/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Database;
using JetBrains.DataStructures;

namespace JetBrains.Omea.ResourceStore
{
    /**
     * A resource is any object living in the MyPal database. Resources have 
     * properties (of string, int and date types) and are linked to other resources.
     * Properties are stored in the hashtable which Resource inherits.
     */
	
    public class Resource: IntHashTable, IResource
    {
        internal class PropertyCollection: IPropertyCollection
        {
            private Resource _owner;

            internal PropertyCollection( Resource owner )
            {
                _owner = owner;
            }

            public int Count
            {
                get
                {
                    int count = 0;
                    _owner.Lock();
                    try
                    {
                        foreach( Entry e in _owner )
                        {
                            IntArrayList linkList = e.Value as IntArrayList;
                            if ( linkList == null || linkList.Count > 0 )
                            {
                                count++;
                            }
                        }
                    }
                    finally
                    {
                        _owner.UnLock();
                    }
                    return count;
                }
            }

            public IEnumerator GetEnumerator()
            {
                return new PropertyEnumerator( _owner );
            }
        }

        public class PropertyEnumerator: IEnumerator
        {
            private Resource _owner;
            private IEnumerator _entryEnumerator;

            internal PropertyEnumerator( Resource owner )
            {
                _owner = owner;
                _entryEnumerator = owner.GetEnumerator();
            }

            public void Reset()
            {
                _entryEnumerator.Reset();
            }

            object IEnumerator.Current
            {
                get
                {
                    int propId = ((Entry) _entryEnumerator.Current).Key;
                    return new ResourceProperty( _owner, propId );
                }
            }

            public IResourceProperty Current
            {
                get
                {
                    int propId = ((Entry) _entryEnumerator.Current).Key;
                    return new ResourceProperty( _owner, propId );
                }
            }

            public bool MoveNext()
            {
                while( _entryEnumerator.MoveNext() )
                {
                    IntArrayList list = ((Entry) _entryEnumerator.Current).Value as IntArrayList;
                    if ( list == null || list.Count > 0 )
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal class ResourceProperty: IResourceProperty
        {
            private Resource _owner;
            private int _propId;

            internal ResourceProperty( Resource owner, int propId )
            {
                _owner = owner;
                _propId = propId;
            }

            public string Name
            {
                get { return MyPalStorage.Storage.GetPropName( _propId ); }
            }

            public int PropId 
            {
                get { return _propId; }
            }

            public PropDataType DataType
            {
                get { return MyPalStorage.Storage.GetPropDataType( _propId ); }
            }

            public object Value
            {
                get 
                { 
                    if ( MyPalStorage.Storage.GetPropDataType( _propId ) == PropDataType.Link )
                        return _owner.GetLinkProp( _propId );
                    else
                    {
                        _owner.Lock();
                        try
                        {
                            return _owner.GetPropValue( _propId );
                        }
                        finally
                        {
                            _owner.UnLock();
                        }
                    }
                }
            }
        }

        private int          _ID;
        private short        _typeID;
        private ushort       _propLoadedMask;
        private string       _displayName = null;
        private SpinWaitLock _sync = new SpinWaitLock();

        private const ushort RESOURCE_DELETING_MASK  = 0x8000;
        private const ushort RESOURCE_TRANSIENT_MASK = 0x4000;

        private static object _true = true;

        /**
         * Creates a new or lazy-loaded resource with the specified type.
         */
		
        internal Resource( int ID, int typeID, bool isNew )
        {
            _ID = ID;
            if ( isNew )
            {
                Debug.Assert( typeID >= 0 );
                _typeID         = (short) typeID;
                _propLoadedMask = 0x3FFF;
            }
            else
            {
                if ( typeID >= 0 )
                {
                    _typeID = (short) typeID;
                }
                else
                {
                    _typeID = (short) MyPalStorage.Storage.LoadResourceType( _ID, true );
                }
            }
        }

        /**
         * Returns the ID of the resource, or -1 if the resource has been deleted.
         */
		
        public int Id
        {
            [DebuggerStepThrough] get { return _ID; }
        }

        public int OriginalId
        {
            get
            {
                if ( _ID == -1 )
                {
                    Lock();
                    try
                    {
                        return (int) this[ResourceProps.Id];
                    }
                    finally
                    {
                        UnLock();
                    }
                }

                return _ID;
            }
        }

        /**
         * Returns the type of the resource.
         */

        public string Type
        {
            get { return ((ResourceTypeCollection) MyPalStorage.Storage.ResourceTypes).GetItemSafe( _typeID ).Name; }
        }

        /**
         * Returns the ID of the type of the resource.	
         */

        public int TypeId
        {
            get { return _typeID; }
        }

        /**
         * Returns the display name of the resource (the default text representation
         * that will be shown to users).
         */
		
        public string DisplayName
        {
            get
            {
                if (_displayName == null)
                    CalcDisplayName();
                return _displayName;
            }
            set
            {
                SetProp( "_DisplayName", value );
                _displayName = value;
            }
        }

        public void Lock()
        {
            _sync.Enter();
        }

        public bool TryLock()
        {
            return _sync.TryEnter();
        }

        public void UnLock()
        {
            _sync.Exit();
        }

        internal void SetTransient()
        {
            _propLoadedMask |= RESOURCE_TRANSIENT_MASK;
        }

        public bool IsTransient
        {
            get { return ( _propLoadedMask & RESOURCE_TRANSIENT_MASK ) != 0; }
        }

        public bool IsDeleting
        {
            get { return ( _propLoadedMask & RESOURCE_DELETING_MASK ) != 0; }
        }

        public bool IsDeleted
        {
            get { return _ID == -1; }
        }

        public IResourceStore ResourceStore
        {
            get { return MyPalStorage.Storage; }
        }

        /**
         * Invalidates the display name of the resource, so that it would be
         * recalculated when it is requested again.
         */

        internal void InvalidateDisplayName()
        {
            _displayName = null;
        }

        /**
         * Calculates the display name from the display name mask.
         */

        private void CalcDisplayName()
        {
            string staticDisplayName = GetStringProp( "_DisplayName" );
            if ( staticDisplayName != null )
            {
                _displayName = staticDisplayName;
                return;
            }

            if ( MyPalStorage.Storage == null )
            {
                _displayName = "";
            }
            else
            {
                DisplayNameMask displayNameMask = MyPalStorage.Storage.GetResourceTypeDisplayNameMask( _typeID );
                _displayName = displayNameMask.GetValue( this );
                if ( _displayName == null || _displayName.Length == 0 )
                {
                    _displayName = MyPalStorage.Storage.CalcCustomDisplayName( this );
                }
            }
        }

        public override string ToString()
        {
            string result = DisplayName;
            if ( result.Length == 0 )
            {
                result = "<" + Type + " ID=" + Id + ">";
            }
            return result;
        }

        /**
         * Returns the collection allowing easy enumeration of the properties of the resource.
         */

        public IPropertyCollection Properties
        {
            get 
            { 
                CheckLoadAllProperties();
                return new PropertyCollection( this ); 
            }
        }

        public void ClearProperties()
        {
            if( IsTransient )
            {
                Clear();
                _propLoadedMask = 0x3FFF;
                SetTransient();
            }
        }

        internal bool PropertiesLoaded( PropDataType propType )
        {
            return ( _propLoadedMask & ( 1 << (int) propType ) ) != 0;
        }

        /**
         * Sets a property of the resource to the specified value.	
         */
		
        public void SetProp( string propName, object propValue )
        {
            int propId = MyPalStorage.Storage.GetPropId( propName );
            SetProp( propId, propValue );
        }

        public void SetProp<T>(PropId<T> propId, T value)
        {
            SetProp( propId.Id, value );
        }

        public void SetReverseLinkProp(PropId<IResource> propId, IResource propValue)
        {
            SetProp(-propId.Id, propValue);
        }

        /**
         * Sets a property with the specified ID to the specified value.
         */

        public void SetProp( int propId, object propValue )
        {
            IPropType propType = MyPalStorage.Storage.PropTypes [propId];
            PropDataType dataType = propType.DataType;
            if ( propType.HasFlag( PropTypeFlags.Virtual ) )
            {
                throw new StorageException( "Cannot use SetProp to set virtual properties" );
            }
            if ( propValue != null )
            {
                MyPalStorage.Storage.CheckValueType( propId, dataType, propValue );
            }
            else
            {
                DeleteProp( propId );
                return;
            }

            if ( dataType == PropDataType.Link )
            {
                SetLinkProp( propId, (IResource) propValue );
                return;
            }
            if ( dataType == PropDataType.Date && (DateTime) propValue == DateTime.MinValue )
            {
                DeleteProp( propId );
                return;
            }
            if ( dataType == PropDataType.StringList )
            {
                throw new StorageException( "Please use IResource.GetStringListProp() and methods of IStringList " + 
                    " to change values of string list properties" );
            }
            if ( dataType == PropDataType.Blob && propValue is string )
            {
                propValue = new JetMemoryStream( Encoding.UTF8.GetBytes( (string) propValue ), true );
            }
            
            object oldValue;
            Lock();
            try
            {
                if ( _ID == -1 )
                    throw new ResourceDeletedException();

                CheckLoadProperties( dataType );

                bool newProp = false;
                oldValue = GetPropValue( propId );
                
                if ( dataType == PropDataType.Bool )
                {
                    bool oldBoolValue = (oldValue != null);
                    bool boolValue = (bool) propValue;
                    if ( oldBoolValue == boolValue )
                        return;
                    if( boolValue )
                    {
                        this[ propId ] = _true;
                    }
                    else
                    {
                        Remove( propId );
                    }
                }
                else
                {
                    if( oldValue == null )
                    {
                        newProp = true;
                    }
                    else if( oldValue.Equals( propValue ) )
                    {
                        return;
                    }
                    if( newProp || dataType != PropDataType.Blob || IsTransient )
                    {
                            this[propId] = propValue;
                    }                    
                }

                if( !IsTransient )
                {
                    if ( dataType == PropDataType.Bool )
                    {
                        if ( (bool) propValue )
                        {
                            MyPalStorage.Storage.CreateBoolProperty( this, propId );
                        }
                        else
                        {
                            MyPalStorage.Storage.DeleteBoolProperty( this, propId );
                        }
                    }
                    else
                    {
                        if ( newProp )
                        {
                            MyPalStorage.Storage.CreateProperty( this, propId, propValue );
                            if ( dataType == PropDataType.Blob )
                            {
                                // propValue passed to the function is a stream, and the props hash
                                // must actually contain a blob
                                this[ propId ] = MyPalStorage.Storage.GetBlobProperty( _ID, propId );
                            }
                        }
                        else
                        {
                            MyPalStorage.Storage.UpdateProperty( this, propId, propValue );
                        }
                    }
                }
            }
            finally
            {
                UnLock();
            }
            
            MyPalStorage.Storage.OnResourceSaved( this, propId, oldValue );
        }

        /**
         * Sets a link property with the specified ID to the specified value.
         */

        private void SetLinkProp( int propId, IResource propValue )
        {
            if ( propValue == null )
            {
                DeleteLinks( propId );
            }
            else
            {
                // in order not to break link restrictions, BeginUpdate/EndUpdate wrapper is needed
                BeginUpdate();
                try
                {
                    if ( !DeleteLinksExcept( propId, propValue.Id ) )
                    {
                        AddLink( propId, propValue );
                    }
                }
                finally
                {
                    EndUpdate();
                }
            }
        }

        /**
         * Deletes the property of the resource with the specified name.
         */

        public void DeleteProp( string propName )
        {
            DeleteProp( MyPalStorage.Storage.GetPropId( propName ) );
        }

        /**
         * Deletes the property of the resource with the specified ID.
         */

        public void DeleteProp( int propId )
        {
            object oldValue;
            Lock();
            try
            {
                if ( _ID == -1 )
                    throw new ResourceDeletedException();

                PropDataType propType = MyPalStorage.Storage.GetPropDataType( propId );
                if ( propType == PropDataType.Link )
                {
                    DeleteLinks( propId );
                    return;
                }

                CheckLoadProperties( propType );

                oldValue = GetPropValue( propId );
                if ( oldValue != null )
                {
                    Remove( propId );
                    if ( !IsTransient )
                    {
                        if ( propType == PropDataType.Bool )
                        {
                            MyPalStorage.Storage.DeleteBoolProperty( this, propId );
                        }
                        else
                        {
                            MyPalStorage.Storage.DeleteProperty( this, propId );
                        }
                    }
                }
            }
            finally
            {
                UnLock();
            }
            if ( oldValue != null )
            {
                MyPalStorage.Storage.OnResourceSaved( this, propId, oldValue );
            }
        }

        /**
         * Returns the value of the specified property of any type.
         */

        public object GetProp( string propName )
        {
            return GetProp( MyPalStorage.Storage.GetPropId( propName ) );
        }


        public T GetProp<T>(PropId<T> propId)
        {
            return (T) GetProp(propId.Id);
        }

        public object GetProp( int propId )
        {
            // this also checks if the property ID is valid
            PropDataType propType = MyPalStorage.Storage.GetPropDataType( propId );
            if ( propType == PropDataType.Link )
            {
                return GetLinkProp( propId );
            }

            Lock();
            try
            {
                CheckLoadProperties( propType );
                object val = GetPropValue( propId );
                if ( val == null && propType == PropDataType.Bool )
                    return false;
                
                return val;
            }
            finally
            {
                UnLock();
            }
        }

        /**
         * Returns the value of the specified string property. Returns null if 
         * there is no such property, throws an exception if the property is not 
         * of the string type.
         */

        public string GetStringProp( string propName )
        {
            return GetStringProp( MyPalStorage.Storage.GetPropId( propName ) );
        }

        /**
         * Returns the value of the string property with the specified ID.
         * Returns null if there is no such property, throws an exception 
         * if the property is not of the string type.
         */

        public string GetStringProp( int propId )
        {
            return (string) GetPropObject( propId, PropDataType.String );
        }

        /**
         * Returns the value of the specified integer property. Returns 0 if
         * there is no such property, throws an exception if the property is not 
         * of the integer type.
         */

        public int GetIntProp( string propName )
        {
            return GetIntProp( MyPalStorage.Storage.GetPropId( propName ) );
        }

        /**
         * Returns the value of the integer property with the specified ID. Returns 0
         * there is no such property, throws an exception if the property is not 
         * of the integer type.
         */

        public int GetIntProp( int propId )
        {
            object propValue = GetPropObject( propId, PropDataType.Int );
            if ( propValue == null )
                return 0;

            return (int) propValue;
        }

        /**
         * Returns the value of the date property with the specified name.
         */

        public DateTime GetDateProp( string propName )
        {
            return GetDateProp( MyPalStorage.Storage.GetPropId( propName ) );
        }

        /**
         * Returns the value of the date property with the specified ID.
         */

        public DateTime GetDateProp( int propId )
        {
            object dateValue = GetPropObject( propId, PropDataType.Date );
            if ( dateValue == null )
                return DateTime.MinValue;
            return (DateTime) dateValue;
        }

        /**
         * Returns the value of the double property with the specified name.
         */

        public double GetDoubleProp( string propName )
        {
            return GetDoubleProp( MyPalStorage.Storage.GetPropId( propName ) );
        }

        /**
         * Returns the value of the double property with the specified ID.
         */

        public double GetDoubleProp( int propId )
        {
            return (double) GetPropObject( propId, PropDataType.Double );
        }

        /**
         * Returns the value of the blob property with the specified name.
         */

        public Stream GetBlobProp( string propName )
        {
            return GetBlobProp( MyPalStorage.Storage.GetPropId( propName ) );
        }

        /**
         * Returns the value of the blob property with the specified ID.
         */

        public Stream GetBlobProp( int propId )
        {
            if ( _ID == -1 )
                throw new ResourceDeletedException();
            
            IBLOB blob = (IBLOB) GetPropObject( propId, PropDataType.Blob );
            if ( blob != null )
            {
                try
                {
                    return blob.Stream;
                }
                catch( IOException ex )
                {
                    MyPalStorage.Storage.OnIOErrorDetected( ex );
                }
            }
            return null;            
        }

        /**
         * Returns the value of the string list property with the specified name.
         */

        public IStringList GetStringListProp( string propName )
        {
            return GetStringListProp( MyPalStorage.Storage.GetPropId( propName ) );
        }

        /**
         * Returns the value of the string list property with the specified ID.
         */

        public IStringList GetStringListProp( int propId )
        {
            if ( MyPalStorage.Storage.GetPropDataType( propId ) != PropDataType.StringList )
            {
                throw new StorageException( MyPalStorage.Storage.GetPropName( propId ) + 
                    " is not a StringList property" );
            }

            Lock();
            try
            {
                PropertyStringList stringList = (PropertyStringList) this[ propId ];
                if ( stringList == null )
                {
                    stringList = new PropertyStringList( this, propId, IsTransient, IsTransient );
                    this[ propId ] = stringList;
                }
                return stringList;
            }
            finally
            {
                UnLock();
            }
        }

        /**
         * Returns the first object connected to the current object by the link
         * of the specified type, or null if there is no such object.
         */

        public IResource GetLinkProp( string propName )
        {
            return GetLinkProp( MyPalStorage.Storage.GetPropId( propName ) );
        }

        public IResource GetLinkProp( int propId )
        {
            if ( MyPalStorage.Storage.GetPropDataType( propId ) != PropDataType.Link )
                throw new StorageException( propId + " is not a link property" );

            Lock();
            try
            {
                IntArrayList linkList = GetLinkList( propId );
                if ( linkList != null && linkList.Count > 0 )
                {
                    if ( !IsTransient )
                    {
                        return MyPalStorage.Storage.TryLoadResource( linkList [0] );
                    }
                    
                    // for transient resources, the link list may contain deleted resources,
                    // and no one notifies us about their deletion => skip deleted resources now
                    while( linkList.Count > 0 )
                    {
                        IResource res = MyPalStorage.Storage.TryLoadResource( linkList [0] );
                        if ( res != null )
                        {
                            return res;
                        }
                        linkList.RemoveAt( 0 );
                    }
                }

                return null;
            }
            finally
            {
                UnLock();
            }
        }

        public IResource GetReverseLinkProp(PropId<IResource> propId)
        {
            return GetLinkProp(-propId.Id);
        }

        private class StringPropWeakReference : WeakReference
        {
            private int _offset;

            public StringPropWeakReference( object obj, int offset )
                : base(obj )
            {
                _offset = offset;
            }

            public int Offset
            {
                get { return _offset; }
            }
        }

        /// <summary>
        /// Returns the value of the property with the specified ID, performing the lazy-load
        /// of string properties if required.
        /// </summary>
        /// <param name="propId">The ID of the property to return.</param>
        /// <returns>The property value.</returns>
        internal object GetPropValue( int propId )
        {
            object propValue = this[ propId ];
            PropDataType dataType = MyPalStorage.Storage.PropTypes [propId].DataType;
            if( dataType == PropDataType.String || dataType == PropDataType.LongString )
            {
                if ( propValue is Int32 )
                {
                    int offset = (int) propValue;
                    IRecord rec = MyPalStorage.Storage.LoadPropertyRecord( dataType, offset );
                    propValue = rec.GetStringValue( 2 );
                    if( dataType == PropDataType.LongString )
                    {
                        this[ propId ] = new StringPropWeakReference( propValue, offset );
                    }
                    else
                    {
                        this[ propId ] = propValue;
                    }
                }
                else if( propValue is StringPropWeakReference )
                {
                    StringPropWeakReference weakRef = (StringPropWeakReference) propValue;
                    propValue = weakRef.Target;
                    if( propValue == null )
                    {
                        int offset = weakRef.Offset;
                        IRecord rec = MyPalStorage.Storage.LoadPropertyRecord( PropDataType.LongString, offset );
                        propValue = rec.GetStringValue( 2 );
                        weakRef.Target = propValue;
                    }
                }
            }
            Stream stream = propValue as Stream;
            if( stream != null )
            {
                stream.Position = 0;
            }
            return propValue;
        }

        /**
         * Returns the list of resource links which have the specified type.
         */

        private IntArrayList GetLinkList( int propId )
        {
            IntArrayList result = (IntArrayList) this[ propId ];
            if ( result == null && !PropertiesLoaded( PropDataType.Link ) )
            {
                LoadLinks( propId );
                result = (IntArrayList) this[ propId ];
            }
            if ( result != null && result.Count == 0 )
            {
                return null;
            }
            return result;
        }

        /**
         * Returns the value of the specified property, or null if there is no such
         * property. Throws an exception if the property is not of the specified type.
         */
		
        private object GetPropObject( int propId, PropDataType expectType )
        {
            PropDataType propType = MyPalStorage.Storage.GetPropDataType( propId );
            if ( propType != expectType )
            {
                // for LongString properties, the same GetStringProp() method is used,
                // so expectType will be PropType.String when propType is PropType.LongString,
                // which is not an error
                if ( (propType != PropDataType.LongString) || (expectType != PropDataType.String ) )
                {
                    throw new StorageException( MyPalStorage.Storage.GetPropName( propId ) + 
                        " is not a " + expectType + " property" );
                }
            }

            Lock();
            try
            {
                CheckLoadProperties( propType );
                return GetPropValue( propId );
            }
            finally
            {
                UnLock();
            }
        }

        /**
         * Returns the textual (user-visible) representation of the specified
         * property. Supports link properties, too.
         */
		
        public string GetPropText( string propName )
        {
            return GetPropText( MyPalStorage.Storage.GetPropId( propName ) );
        }

        /**
         * Returns the textual (user-visible) representation of the property
         * with the specified ID.
         */

        public string GetPropText( int propId )
        {
            PropDataType propType = MyPalStorage.Storage.GetPropDataType( propId );
            if ( propType == PropDataType.Link )
                return GetLinkPropText( propId );

            object propValue;
            Lock();
            try
            {
                CheckLoadProperties( propType );
                propValue = GetPropValue( propId );
            }
            finally
            {
                UnLock();
            }

            if ( propValue == null )
            {
                return string.Empty;
            }
            if( propType == PropDataType.Blob )
            {
                IBLOB blob = propValue as IBLOB;
                return ( blob != null ) ? blob.ToString() : Utils.StreamToString( (Stream)propValue );
            }
            return propValue.ToString();
        }

        /**
         * Returns the value of the link property with the specified name (the display
         * names of all objects connected with that link type, separated with commas).
         */

        private string GetLinkPropText( int propId )
        {
            Lock();
            try
            {
                IntArrayList resIDList = GetLinkList( propId );
                if ( resIDList == null || resIDList.Count == 0 )
                    return "";

                string resName0 = MyPalStorage.Storage.LoadResource( resIDList [0] ).DisplayName;
                if ( resIDList.Count == 1 )
                {
                    return resName0;
                }

                StringBuilder result = new StringBuilder( resName0 );
                for( int i=1; i<resIDList.Count; i++ )
                {
                    result.Append( ", " );
                    result.Append( MyPalStorage.Storage.LoadResource( resIDList [i] ).DisplayName );
                }

                return result.ToString();
            }
            finally
            {
                UnLock();
            }
        }

        /**
         * Checks if the resource has the specified property.
         */

        public bool HasProp( string propName )
        {
            return HasProp( MyPalStorage.Storage.GetPropId( propName ) );
        }

        public bool HasProp( int propId )
        {
            PropDataType propType = MyPalStorage.Storage.GetPropDataType( propId );
            if ( propType == PropDataType.Link )
            {
                return HasLinkProp( propId );
            }
            else
            {
                Lock();
                try
                {
                    CheckLoadProperties( propType );
                    bool contains = Contains( propId );
                    if ( propType == PropDataType.StringList && contains )
                    {
                        IStringList stringList = (IStringList) this[ propId ];
                        if ( stringList.Count == 0 )
                            return false;
                    }
                    return contains;
                }
                finally
                {
                    UnLock();
                }
            }
        }

        public bool HasProp<T>(PropId<T> propId)
        {
            return HasProp(propId.Id);
        }

        private bool HasLinkProp( int propId )
        {
            IntArrayList list;
            Lock();
            try
            {
                list = (IntArrayList) this[ propId ];
                if ( list != null )
                {
                    return list.Count > 0;
                }
                if( PropertiesLoaded( PropDataType.Link ) )
                {
                    return false;
                }
            }
            finally
            {
                UnLock();   
            }
            if ( propId > 0 || !MyPalStorage.Storage.IsLinkDirected( propId ) )
            {
                using( IResultSet rs = MyPalStorage.Storage.GetLinksFrom( _ID, propId ) )
                {
                    SafeRecordValueEnumerator enumerator = new SafeRecordValueEnumerator( rs, "Resource.HasLinkProp" );
                    using( enumerator )
                    {
                        if( enumerator.MoveNext() )
                        {
                            return true;
                        }
                    }
                }
            }
            if ( propId < 0 || !MyPalStorage.Storage.IsLinkDirected( propId ) )
            {
                int toPropId = propId;
                if ( propId < 0 )
                {
                    toPropId = -toPropId;
                }
                        
                using( IResultSet rs = MyPalStorage.Storage.GetLinksTo( _ID, toPropId ) )
                {
                    SafeRecordValueEnumerator enumerator = new SafeRecordValueEnumerator( rs, "Resource.HasLinkProp" );
                    using( enumerator )
                    {
                        if( enumerator.MoveNext() )
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /**
         * Ensures that the properties of the resource of the specified type are loaded.
         */

        private void CheckLoadProperties( PropDataType propType )
        {
            Debug.Assert( propType != PropDataType.Link );
            if ( !PropertiesLoaded( propType ) )
            {
                if ( propType == PropDataType.Bool )
                {
                    LoadBoolProperties();
                }
                else if ( propType == PropDataType.StringList )
                {
                    LoadStringListProperties();
                }
                else if ( propType == PropDataType.String || propType == PropDataType.LongString )
                {
                    LoadStringProperties( propType );
                }
                else
                {
                    LoadProperties( propType );
                }
                
                _propLoadedMask = (ushort) (_propLoadedMask | (1 << (int) propType));
            }
        }

        /**
         * Ensures that all properties of the resource are loaded.
         */ 

        private void CheckLoadAllProperties()
        {
            Lock();
            try
            {
                for( PropDataType propType = PropDataType.Int; propType <= PropDataType.StringList; propType++ )
                {
                    if ( propType != PropDataType.Link )
                    {
                        CheckLoadProperties( propType );
                    }
                }
                if ( !PropertiesLoaded( PropDataType.Link ) )
                {
                    LoadLinks();
                }
            }
            finally
            {
                UnLock();
            }
        }

        /**
         * Adds a link of the specified type between the specified resources.
         */

        public void AddLink( string propName, IResource target )
        {
            AddLink( MyPalStorage.Storage.GetPropId( propName ), target );
        }

        /**
         * Adds the link with the specified prop ID between the specified resources.
         */

        public void AddLink( int propId, IResource target )
        {
            if ( target == null )
            {
                throw new ArgumentNullException( "target" );
            }
            
            Resource targetResource = (Resource) target;
            if ( _ID == -1 )
                throw new ResourceDeletedException( "The link source resource has been deleted" );
            if ( targetResource.Id == -1 )
                throw new ResourceDeletedException( "The link target resource has been deleted" );
            if ( MyPalStorage.Storage.GetPropDataType( propId ) != PropDataType.Link )
            {
                throw new StorageException( propId + " is not a link property" );
            }
            if ( !IsTransient && propId < 0 )
            {
                throw new StorageException( "Negative link IDs may not be used with AddLink" );
            }
            if ( targetResource.Id == _ID )
            {
                throw new StorageException( "Cannot link a resource to itself (resource type " + Type + 
                    ", property type " + MyPalStorage.Storage.PropTypes [propId].Name + ")" );
            }

            if ( !IsTransient && targetResource.IsTransient )
            {
                if ( MyPalStorage.Storage.IsLinkDirected( propId ) )
                {
                    target.AddLink( -propId, this );
                }
                else
                {
                    target.AddLink( propId, this );
                }
                return;
            }

            if ( !AddLinkSide( this, propId, targetResource ) )
                return;

            int reversePropId = MyPalStorage.Storage.IsLinkDirected( propId ) ? -propId : propId;

            if ( !IsTransient )
            {
                if ( targetResource.IsDeleting )
                {
                    return;                    
                }
                AddLinkSide( targetResource, reversePropId, this );
                MyPalStorage.Storage.SaveLink( Id, target.Id, propId );
            }

            MyPalStorage.Storage.OnLinkAdded( this, targetResource, propId );
            if ( !IsTransient )
            {
                MyPalStorage.Storage.OnLinkAdded( targetResource, this, reversePropId );
            }
        }

        public void AddLink(PropId<IResource> propId, IResource target)
        {
            AddLink(propId.Id, target);
        }

        /**
         * Adds one side of a resource link between two objects. Returns false
         * if the link already exists or true if the link is new.
         */

        private static bool AddLinkSide( Resource from, int propId, Resource target )
        {
            from.Lock();
            try
            {
                if ( from.IsDeleting )
                    return false;
                
                IntArrayList linkList = from.GetLinkList( propId );
                if ( linkList == null )
                {
                    linkList = new IntArrayList();
                    from[ propId ] = linkList;
                }
                lock( linkList )
                {
                    int index = linkList.BinarySearch( target.Id );
                    if ( index >= 0 )
                        return false;
                    linkList.Insert( ~index, target.Id );
                }
            }
            finally
            {
                from.UnLock();
            }
            
            return true;
        }

        /**
         * Returns the count of links of the specified type.
         */

        public int GetLinkCount( string propName )
        {
            return GetLinkCount( MyPalStorage.Storage.GetPropId( propName ) );
        }

        public int GetLinkCount( int propId )
        {
            Lock();
            try
            {
                IntArrayList linkList = GetLinkList( propId );
                if ( linkList == null )
                    return 0;

                return linkList.Count;
            }
            finally
            {
                UnLock();
            }
        }

        /**
         * Checks if a link with the specified type to the specified resource already
         * exists.
         */

        public bool HasLink( string propName, IResource target )
        {
            return HasLink( MyPalStorage.Storage.GetPropId( propName ), target );
        }

        public bool HasLink( int propId, IResource target )
        {
            if ( target == null )
                throw new ArgumentNullException( "target" );

            VerifyLinkProp( propId );

            Lock();
            try
            {
                IntArrayList linkList = GetLinkList( propId );
                if ( linkList == null )
                    return false;
                return linkList.BinarySearch( target.Id ) >= 0;
            }
            finally
            {
                UnLock();
            }
        }

        /**
         * Deletes a link from the resource to another resource.
         */

        public void DeleteLink( string propName, IResource target )
        {
            DeleteLink( MyPalStorage.Storage.GetPropId( propName ), target );
        }

        public void DeleteLink( int propId, IResource target )
        {
            if ( target == null )
                throw new ArgumentNullException( "target" );
            if ( MyPalStorage.Storage.GetPropDataType( propId ) != PropDataType.Link )
                throw new StorageException( propId + " is not a link property" );

            Resource targetRes = (Resource) target;
            
            if ( _ID == -1 )
                throw new ResourceDeletedException();

            if ( MyPalStorage.Storage.IsLinkDirected( propId ) )
            {
                DeleteLinkSide( this, -propId, targetRes );
                if ( !IsTransient )
                {
                    DeleteLinkSide( targetRes, -propId, this );
                }
            }                           

            DeleteLinkSide( this, propId, targetRes );
            if ( !IsTransient )
            {
                DeleteLinkSide( targetRes, propId, this );
            }
        }

        /**
         * Deletes one side of a resource link between two objects.
         */

        private static void DeleteLinkSide( Resource from, int propId, Resource target )
        {
            bool doDeleteLink = false;
            from.Lock();
            try
            {
                IntArrayList linkList = from.GetLinkList( propId );
                if ( linkList != null )
                {
                    bool doRemoveProp = false;
                    lock( linkList )
                    {
                        // the target resource may have been deleted by user code when the first
                        // link side was being deleted, so, in order to make sure we search for the
                        // correct ID, OriginalId needs to be used (OM-10400)
                        int index = linkList.BinarySearch( target.OriginalId );
                        if ( index >= 0 )
                        {
                            linkList.RemoveAt( index );
                            doDeleteLink = true;
                            if ( linkList.Count == 0 )
                                doRemoveProp = true;
                        }
                    }
                    if ( doRemoveProp )
                    {
                        from.Remove( propId );
                    }
                }
            }
            finally
            {
                from.UnLock();
            }
            if ( doDeleteLink )
            {
                MyPalStorage.Storage.DeleteLink( from, target, propId );
            }
        }

        /**
         * Deletes all links of the specified type for a resource.
         */

        public void DeleteLinks( string propName )
        {
            DeleteLinks( MyPalStorage.Storage.GetPropId( propName ) );
        }

        public void DeleteLinks( int propId )
        {
            if ( MyPalStorage.Storage.GetPropDataType( propId ) != PropDataType.Link )
            {
                throw new StorageException( propId + " is not a link property" );
            }
            DeleteLinksExcept( propId, Int32.MinValue );
        }

        /**
         * Deletes all links of the specified type for a resource except for the link to
         * the specified resource.
         * @return true if the link to the specified resource was found.
         */
        
        private bool DeleteLinksExcept( int propId, int exceptResourceId )
        {
            bool foundExisting = false;

            IntArrayList linksToDelete = null;
            try
            {
                IntArrayList reverseLinksToDelete = null;
                Lock();
                try
                {
                    if ( _ID == -1 )
                        throw new ResourceDeletedException();

                    if ( exceptResourceId != Int32.MinValue && MyPalStorage.Storage.IsLinkDirected( propId ) )
                    {
                        // if there is a link between the same two objects going in the
                        // opposite direction, deletes that link
                        IntArrayList reverseLinks = GetLinkList( -propId );
                        if ( reverseLinks != null )
                        {
                            for( int i=0; i<reverseLinks.Count; i++ )
                            {
                                int linkID = reverseLinks [i];
                                if ( linkID == exceptResourceId )
                                {
                                    if ( reverseLinksToDelete == null )
                                    {
                                        reverseLinksToDelete = new IntArrayList();
                                    }
                                    reverseLinksToDelete.Add( linkID );
                                    break;
                                }
                            }
                        }
                    }

                    IntArrayList oldLinks = GetLinkList( propId );
                    if ( oldLinks != null )
                    {
                        for( int i=oldLinks.Count-1; i >= 0; i-- )
                        {
                            if ( oldLinks [i] == exceptResourceId )
                                foundExisting = true;
                            else
                            {
                                if ( linksToDelete == null )
                                {
                                    linksToDelete = IntArrayListPool.Alloc();
                                }
                                linksToDelete.Add( oldLinks [i] );
                            }
                        }
                    }
                }
                finally
                {
                    UnLock();
                }
                if ( linksToDelete != null )
                {
                    for( int i=0; i<linksToDelete.Count; i++ )
                    {
                        DeleteLink( propId, MyPalStorage.Storage.LoadResource( linksToDelete [i] ) );
                    }
                }
                if ( reverseLinksToDelete != null )
                {
                    for( int i=0; i<reverseLinksToDelete.Count; i++ )
                    {
                        DeleteLink( -propId, MyPalStorage.Storage.LoadResource( reverseLinksToDelete [i] ) );
                    }
                }
                return foundExisting;
            }
            finally
            {
                if( linksToDelete != null )
                {
                    IntArrayListPool.Dispose( linksToDelete );
                }
            }
        }

        /**
         * Returns a list of allo resources of the specified type linked to the resource with
         * the specified link type.
         */

        public IResourceList GetLinksOfType( string resType, string propName )
        {
            return GetLinksOfType( resType, MyPalStorage.Storage.GetPropId( propName ), false, false );
        }
 
        /**
         * Returns a list of all resources of the specified type linked to the resource
         * with a link of the specified ID.
         */

        public IResourceList GetLinksOfType( string resType, int propId )
        {
            return GetLinksOfType( resType, propId, false, false );
        }

        public IResourceList GetLinksOfType(string resType, PropId<IResource> propId)
        {
            return GetLinksOfType(resType, propId.Id);
        }

        public BusinessObjectList<T> GetLinksOfType<T>(ResourceTypeId<T> resType, PropId<IResource> propId) where T : BusinessObject
        {
            return new BusinessObjectList<T>(resType, GetLinksOfType(resType.Name, propId));
        }

        public IResourceList GetLinksOfTypeLive( string resType, string propName )
        {
            return GetLinksOfType( resType, MyPalStorage.Storage.GetPropId( propName ), true, false );
        }

        public IResourceList GetLinksOfTypeLive( string resType, int propId )
        {
            return GetLinksOfType( resType, propId, true, false );
        }

        public IResourceList GetLinksOfTypeLive(string resType, PropId<IResource> propId)
        {
            return GetLinksOfTypeLive(resType, propId.Id);
        }

        public IResourceList GetLinksFrom( string resType, string propName )
        {
            int propId = MyPalStorage.Storage.GetPropId( propName );
            VerifyDirectedLink( propId );
            return GetLinksOfType( resType, propId, false, true );
        }

        public IResourceList GetLinksFrom( string resType, int propId )
        {
            VerifyDirectedLink( propId );
            return GetLinksOfType( resType, propId, false, true );
        }

        public IResourceList GetLinksFrom(string resType, PropId<IResource> propId)
        {
            return GetLinksFrom(resType, propId.Id);
        }


        public BusinessObjectList<T> GetLinksFrom<T>(ResourceTypeId<T> resType, PropId<IResource> propId)
            where T : BusinessObject
        {
            return new BusinessObjectList<T>(resType, GetLinksFrom(resType.Name, propId));
        }

        public IResourceList GetLinksFromLive( string resType, string propName )
        {
            int propId = MyPalStorage.Storage.GetPropId( propName );
            VerifyDirectedLink( propId );
            return GetLinksOfType( resType, propId, true, true );
        }

        public IResourceList GetLinksFromLive( string resType, int propId )
        {
            VerifyDirectedLink( propId );
            return GetLinksOfType( resType, propId, true, true );
        }

        public IResourceList GetLinksTo( string resType, string propName )
        {
            int propId = MyPalStorage.Storage.GetPropId( propName );
            VerifyDirectedLink( propId );
            return GetLinksOfType( resType, -propId, false, true );
        }

        public IResourceList GetLinksTo( string resType, int propId )
        {
            VerifyDirectedLink( propId );
            return GetLinksOfType( resType, -propId, false, true  );
        }

        public IResourceList GetLinksTo(string resType, PropId<IResource> propId)
        {
            return GetLinksTo(resType, propId.Id);
        }

        public BusinessObjectList<T> GetLinksTo<T>(ResourceTypeId<T> resType, PropId<IResource> propId) where T : BusinessObject
        {
            return new BusinessObjectList<T>(resType, GetLinksTo(resType.Name, propId));
        }

        public IResourceList GetLinksToLive( string resType, string propName )
        {
            int propId = MyPalStorage.Storage.GetPropId( propName );
            VerifyDirectedLink( propId );
            return GetLinksOfType( resType, -propId, true, true );
        }

        public IResourceList GetLinksToLive( string resType, int propId )
        {
            VerifyDirectedLink( propId );
            return GetLinksOfType( resType, -propId, true, true  );
        }

        private void VerifyDirectedLink( int propId )
        {
            if ( MyPalStorage.Storage.GetPropDataType( propId ) != PropDataType.Link )
                throw new StorageException( propId + " is not a link property" );

            if ( !MyPalStorage.Storage.IsLinkDirected( propId ) )
                throw new StorageException( propId + " is not a directed link property" );
        }

        private void VerifyLinkProp( int propId )
        {
            int absPropId = propId < 0 ? -propId : propId;
            if ( MyPalStorage.Storage.GetPropDataType( absPropId ) != PropDataType.Link )
                throw new StorageException( propId + " is not a link property" );

            if ( propId < 0 && !MyPalStorage.Storage.IsLinkDirected( propId ) )
                throw new StorageException( propId + " is not a directed link property" );
        }

        private IResourceList GetLinksOfType( string resType, int propId, bool live, bool directed )
        {
            if ( !directed )
            {
                if ( MyPalStorage.Storage.GetPropDataType( propId ) != PropDataType.Link )
                    throw new StorageException( propId + " is not a link property" );
            }

            ResourceLinkPredicate pred = new ResourceLinkPredicate( this, propId, directed );
            ResourceList result;
            string restrictedLinkType = MyPalStorage.Storage.GetLinkResourceTypeRestriction( Type, propId );
            if ( resType != null && restrictedLinkType == resType )
            {
                pred.SetKnownType( MyPalStorage.Storage.ResourceTypes [restrictedLinkType].Id );
                result = new ResourceList( pred, live );
            }
            else
            {
                result = MyPalStorage.Storage.IntersectPredicateWithType( pred, resType, live );
            }

            if ( !live )
            {
                /* we need to support the following pattern:
                    - GetLinksOfType()  (not live)
                    - Delete()
                    - do something with resources that were linked to the deleted resource
                 */

                result.Instantiate();
            }
            return result;
        }

        /**
         * Returns the list of resource IDs that are linked to the current resource
         * with links of specified type and possibly its reverse.
         */

        internal IntArrayList GetListOfLinks( int propId, bool directed, bool needClone )
        {
            Lock();
            try
            {
                IntArrayList linkList = GetLinkList( propId );

                if ( !directed && MyPalStorage.Storage.IsLinkDirected( propId ) )
                {
                    IntArrayList reverseList = GetLinkList( -propId );
                    if ( linkList == null )
                        linkList = reverseList;
                    else if ( reverseList != null )
                    {
                        linkList = IntArrayList.MergeSorted( linkList, reverseList );
                        needClone = false;
                    }
                }
                if ( linkList != null )
                {
                    return needClone ? (IntArrayList) linkList.Clone() : linkList;
                }
                return new IntArrayList();
            }
            finally
            {
                UnLock();
            }
        }
        
        public int[] GetLinkTypeIds()
        {
            return GetLinkTypeIds( true );
        }
        
        
        /**
         * Returns a list of the IDs of distinct link types present in the resource.
         */

        private int[] GetLinkTypeIds( bool reverseLinks )
        {
            Lock();
            try
            {
                if ( !PropertiesLoaded( PropDataType.Link ) )
                {
                    LoadLinks();
                }
                IntArrayList result = IntArrayListPool.Alloc();
                try
                {
                    foreach( Entry propEntry in this )
                    {
                        // do not return reversed link type IDs
                        int propType = propEntry.Key;
                        if ( MyPalStorage.Storage.GetPropDataType( propType ) == PropDataType.Link )
                        {
                            IntArrayList linkList = (IntArrayList) propEntry.Value;
                            if ( linkList.Count > 0 )
                            {
                                if ( reverseLinks && propType < 0 )
                                    propType = -propType;

                                if ( result.IndexOf( propType ) < 0 )
                                    result.Add( propType );
                            }
                        }
                    }
                    return result.ToArray();
                }
                finally
                {
                    IntArrayListPool.Dispose( result );
                }
            }
            finally
            {
                UnLock();
            }
        }

        /**
         * Deletes the resource from the store.
         */

        public void Delete()
        {
            if ( IsDeleting )
                return;

            _propLoadedMask |= RESOURCE_DELETING_MASK;
            if ( !IsTransient )
            {
                MyPalStorage.Storage.CheckEndUpdate( this );
            }
            
            try
            {
                MyPalStorage.Storage.OnResourceDeleting( this );
            }
            finally
            {
                DoDelete();
            }
        }

        /**
         * Performs the actual deletion of the resource from the store.
         */

        private void DoDelete()
        {
            if ( !IsTransient )
            {
                // the resource is already marked as deleting, so it is not possible that
                // new links will appear, and there is no need to hold the resource lock
                // for the entire duration of DeleteAllLinks() (see bug #4117)
                DeleteAllLinks();
            }
            Lock();
            try
            {
                if ( _ID == -1 )
                    return;

                if ( !IsTransient )
                {
                    MyPalStorage.Storage.DeleteResource( this );
                }
                else
                {
                    MyPalStorage.Storage.CleanTransientResource( this );
                }

                int originalId = _ID;
                _ID = -1;
            
                Clear();
                this[ ResourceProps.Id ] = originalId;
            }
            finally
            {
                UnLock();
            }
        }

        /**
         * Deletes all links between this resource and other resources.
         */

        private void DeleteAllLinks()
        {
            int[] linkTypes = GetLinkTypeIds( false );
            for( int i=0; i<linkTypes.Length; i++ )
            {
                int propId = linkTypes [i];
                IntArrayList resIDs;
                Lock();
                try
                {
                    resIDs = GetLinkList( propId );
                    if ( resIDs == null )
                        continue;

                    resIDs = (IntArrayList) resIDs.Clone();
                }
                finally
                {
                    UnLock();
                }

                foreach( int resID in resIDs )
                {
                    Resource target = (Resource) MyPalStorage.Storage.TryLoadResource( resID );
                    if ( target == null )
                    {
                        continue;
                    }
                    DeleteLinkSide( this, propId, target );

                    if ( MyPalStorage.Storage.IsLinkDirected( propId ) )
                        DeleteLinkSide( target, -propId, this );
                    else
                        DeleteLinkSide( target, propId, this );

                    /*
                    if ( cascadeDelete )
                    {
                        Debug.WriteLine( String.Format( "Cascading delete from {0} {1} to {2} {3} through link {4}",
                            this.Id, this.Type, target.Id, target.Type, MyPalStorage.Storage.GetPropName( propId ) ) );
                        cascadeDeletes.Add( target );
                    }
                    */
                }
            }
        }

        /**
         * Begins a deferred update of the resource.
         */

        public void BeginUpdate()
        {
            if ( _ID == -1 )
                throw new ResourceDeletedException();

            MyPalStorage.Storage.BeginUpdateResource( this );
        }

        /**
         * Commits a deferred update of the resource.
         */

        public void EndUpdate()
        {
            if ( _ID == -1 )
                return;
            
            // we need to get a real ID before the OnResourceSaved notification is fired
            if ( IsTransient && MyPalStorage.Storage.GetResourceUpdateCount( this ) == 1 )
            {
                SaveTransientProperties();
            }
            MyPalStorage.Storage.EndUpdateResource( this );
        }

        /**
         * Checks if the resource was changed during a deferred update.
         */

        public bool IsChanged()
        {
            if ( _ID == -1 )
                return false;
            
            return MyPalStorage.Storage.IsResourceChanged( this );
        }

        /**
         * Returns a resource list containing only the current resource.
         */

        public IResourceList ToResourceList()
        {
            if ( IsTransient )
            {
                return new ResourceList( new SingleResourcePredicate( this ), false );
            }
            IntArrayList oneItemList = new IntArrayList();
            oneItemList.Add( _ID );
            return MyPalStorage.Storage.ListFromIds( oneItemList, false );
        }

        /**
         * Returns a live resource list containing only the current resource.
         */

        public IResourceList ToResourceListLive()
        {
            if ( IsTransient )
            {
                return new ResourceList( new SingleResourcePredicate( this ), true );
            }
            IntArrayList oneItemList = new IntArrayList();
            oneItemList.Add( _ID );
            return MyPalStorage.Storage.ListFromIds( oneItemList, true );
        }

        /**
         * Changes the type of a resource.
         */

        public void ChangeType( string newType )
        {
            int oldType = _typeID;
            int newTypeID = MyPalStorage.Storage.ResourceTypes [newType].Id;
            _typeID = (short) newTypeID;
            if ( !IsTransient )
            {
                MyPalStorage.Storage.ChangeResourceType( _ID, _typeID );
                MyPalStorage.Storage.OnResourceSaved( this, ResourceProps.Type, oldType );
            }
        }

        // -- internal implementation ----------------------------------------

        private void LoadProperties( PropDataType propType )
        {
            if ( IsDeleted )
                return;

            IResultSet rs = MyPalStorage.Storage.GetProperties( _ID, propType );
            try
            {
                LoadPropertiesFrom( rs );
            }
            finally
            {
                rs.Dispose();
            }
        }

        private void LoadPropertiesFrom( IResultSet rs )
        {
            PropTypeCollection propTypes = MyPalStorage.Storage.PropTypes as PropTypeCollection;
            using( SafeRecordValueEnumerator enumerator = new SafeRecordValueEnumerator( rs, "Resource.LoadPropertiesFrom()" ) )
            {
                while( enumerator.MoveNext() )
                {
                    int propId = enumerator.GetCurrentIntValue( 1 );
                    object propValue = enumerator.GetCurrentValue( 2 );
                    if ( propTypes.IsValidType( propId ) )
                    {
                        this[ propId] = propValue;
                    }
                    else
                    {
                        MyPalStorage.Storage.OnIndexCorruptionDetected( "Found invalid property ID " + propId + " when loading resource properties" );
                    }
                }
            }
        }

        /// <summary>
        /// Loads string properties with the specified type.
        /// </summary>
        /// <param name="propType">The type of string properties to load.</param>
        private void LoadStringProperties( PropDataType propType )
        {
            if ( IsDeleted )
                return;

            IResultSet rs = MyPalStorage.Storage.GetProperties( _ID, propType );
            try
            {
                PropTypeCollection propTypes = MyPalStorage.Storage.PropTypes as PropTypeCollection;
                using( IKeyPairEnumerator enumerator = (IKeyPairEnumerator) rs.GetEnumerator() )
                {
                    while( true )
                    {
                        try
                        {
                            if ( !enumerator.MoveNext() )
                            {
                                break;
                            }
                        }
                        catch( BadIndexesException )
                        {
                            MyPalStorage.Storage.OnIndexCorruptionDetected( "Resource.LoadStringProperties" );
                            break;
                        }
                        catch( IOException ex )
                        {
                            MyPalStorage.Storage.OnIOErrorDetected( ex );
                            break;
                        }

                        Compound compound = (Compound) enumerator.GetCurrentKey().Key;
                        int propId = (int) compound._key2;
                        int offset = enumerator.GetCurrentOffset();

                        if ( propTypes.IsValidType( propId ) )
                        {
                            this[ propId ] = offset;
                        }
                        else
                        {
                            MyPalStorage.Storage.OnIndexCorruptionDetected( "Found invalid property ID " + propId + " when loading resource properties" );
                        }
                    }
                }
            }
            finally
            {
                rs.Dispose();
            }
        }

        /**
         * Loads all boolean properties of a resource.
         */

        private void LoadBoolProperties()
        {
            using( IResultSet rs = MyPalStorage.Storage.GetBoolProperties( _ID ) )
            {
                PropTypeCollection propTypes = MyPalStorage.Storage.PropTypes as PropTypeCollection;
                using( SafeRecordValueEnumerator enumerator = new SafeRecordValueEnumerator( rs, "Resource.LoadBoolProperties" ) )
                {
                    while( enumerator.MoveNext() )
                    {
                        int value = enumerator.GetCurrentIntValue( 1 );

                        if ( propTypes.IsValidType( value ) && propTypes [value].DataType == PropDataType.Bool )
                        {
                            this[ value ] = _true;
                        }
                        else
                        {
                            MyPalStorage.Storage.OnIndexCorruptionDetected( "Found invalid property ID " + value + " when loading Boolean resource properties" );
                        }
                    }
                }
            }
        }

        /**
         * Loads all string list properties of a resource.
         */

        internal void LoadStringListProperties()
        {
            using( IResultSet rs = MyPalStorage.Storage.GetStringListProperties( _ID ) )
            {
                int lastPropId = -1;

                PropTypeCollection propTypes = MyPalStorage.Storage.PropTypes as PropTypeCollection;
                using( SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "Resource.LoadStringProperties" ) )
                {
                    while( enumerator.MoveNext() )
                    {
                        IRecord rec = enumerator.Current;
                        int propId = rec.GetIntValue( 1 );
                        if ( propId != lastPropId )
                        {
                            if ( propTypes.IsValidType( propId ) && propTypes [propId].DataType == PropDataType.StringList )
                            {
                                PropertyStringList stringList = (PropertyStringList) this[ propId ];
                                if ( stringList == null )
                                {
                                    stringList = new PropertyStringList( this, propId, false, false );
                                    this[ propId ] = stringList;
                                }
                            }
                            else
                            {
                                MyPalStorage.Storage.OnIndexCorruptionDetected( "Found invalid property ID " + propId + " when loading string list properties" );
                            }
                        }
                        lastPropId = propId;
                    }
                }
            }

            _propLoadedMask = (ushort) (_propLoadedMask | (1 << (int) PropDataType.StringList));
        }

        internal void LoadStringListProperties( PropertyStringList stringList, int propId )
        {
            PropTypeCollection propTypes = MyPalStorage.Storage.PropTypes as PropTypeCollection;
            if ( !propTypes.IsValidType( propId ) || propTypes [propId].DataType != PropDataType.StringList )
            {
                MyPalStorage.Storage.OnIndexCorruptionDetected( "Found invalid property ID " + propId + " when loading string list properties" );
            }
            else
            {
                using( IResultSet rs = MyPalStorage.Storage.GetStringListProperties( _ID, propId ) )
                {
                    using( SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "Resource.LoadStringProperties" ) )
                    {
                        while( enumerator.MoveNext() )
                        {
                            IRecord rec = enumerator.Current;
                            stringList.AddValue( rec.GetStringValue( 2 ) );
                        }
                    }
                }
            }
        }

        private void LoadLinks()
        {
            if ( IsDeleted )
                return;

            IntArrayList loadedLinkTypes = IntArrayListPool.Alloc();
            try 
            {
                using( IResultSet rs = MyPalStorage.Storage.GetLinksFrom( _ID ) )
                {
                    LoadLinksFromResultSet( rs, loadedLinkTypes, 1, 1 );
                }
                using( IResultSet rs = MyPalStorage.Storage.GetLinksTo( _ID ) )
                {
                    LoadLinksFromResultSet( rs, loadedLinkTypes, 0, -1 );
                }
                _propLoadedMask = (ushort) (_propLoadedMask | (1 << (int) PropDataType.Link ) );
            }
            finally
            {
                IntArrayListPool.Dispose( loadedLinkTypes );
            }
        }

        private void LoadLinks( int propId )
        {
            if ( IsDeleted )
                return;

            int count = 0;
            if ( propId > 0 || !MyPalStorage.Storage.IsLinkDirected( propId ) )
            {
                using( IResultSet rs = MyPalStorage.Storage.GetLinksFrom( _ID, propId ) )
                {
                    count += LoadLinksFromResultSet( rs, null, 1, 1 );
                }
            }
            if ( propId < 0 || !MyPalStorage.Storage.IsLinkDirected( propId ) )
            {
                int toPropId = propId;
                if ( propId < 0 )
                {
                    toPropId = -toPropId;
                }
                using( IResultSet rs = MyPalStorage.Storage.GetLinksTo( _ID, toPropId ) )
                {
                    count += LoadLinksFromResultSet( rs, null, 0, -1 );
                }
            }
            if ( count == 0 )
            {
                // if we tried to load links once and found nothing, make sure we don't try
                // to load the same links again
                this[ propId ] = new IntArrayList();
            }
        }

        private int LoadLinksFromResultSet( IResultSet resultSet, IntArrayList loadedLinkTypes, 
            int targetPropIndex, int directionModifier )
        {
            PropTypeCollection propTypes = MyPalStorage.Storage.PropTypes as PropTypeCollection;

            int lastPropId = Int32.MaxValue;
            bool skipProp = false;
            IntArrayList lastLinkList = null;
            int count = 0;
            using( resultSet )
            {
                SafeRecordValueEnumerator enumerator = new SafeRecordValueEnumerator( resultSet, "Resource.LoadLinksFromResultSet" );
                using( enumerator )
                {
                    while( enumerator.MoveNext() )
                    {
                        count++;
                        int propId = enumerator.GetCurrentIntValue( 2 );
                        int targetId = enumerator.GetCurrentIntValue( targetPropIndex );

                        int absPropId = propId;
                        if ( propTypes.IsValidType( absPropId ) && MyPalStorage.Storage.IsLinkDirected( propId ) )
                            propId *= directionModifier;

                        if ( propId != lastPropId )
                        {
                            if ( !skipProp && lastLinkList != null )
                            {
                                lastLinkList.Sort();
                            }

                            if ( !propTypes.IsValidType( absPropId ) || propTypes [propId].DataType != PropDataType.Link )
                            {
                                MyPalStorage.Storage.OnIndexCorruptionDetected( "Found invalid property ID " + propId + " when loading resource link properties" );
                                lastLinkList = null;
                                skipProp = true;
                            }
                            else
                            {
                                lastLinkList = (IntArrayList) this[ propId ];
                                if ( lastLinkList != null && loadedLinkTypes != null && loadedLinkTypes.IndexOf( propId ) < 0 )
                                {
                                    skipProp = true;
                                }
                                else
                                {
                                    if ( lastLinkList == null )
                                    {
                                        lastLinkList = new IntArrayList();
                                        this[ propId ] = lastLinkList;
                                    }
                                    skipProp = false;
                                    if ( loadedLinkTypes != null )
                                    {
                                        loadedLinkTypes.Add( propId );
                                    }
                                }
                            }
                        }
                        lastPropId = propId;

                        if ( !skipProp )
                        {
                            lastLinkList.Add( targetId );
                        }
                    }

                    if ( !skipProp && lastLinkList != null )
                    {
                        lastLinkList.Sort();
                    }
                }

                return count;
            }
        }

        private void SaveTransientProperties()
        {
            MyPalStorage.Storage.CommitTransientResource( this );
            _propLoadedMask = (ushort) (_propLoadedMask & 0x3FFF);

            ArrayList addedLinks = ArrayListPool.Alloc();
            try 
            {
                IntArrayList emptyLinkLists = null;
                Lock();
                try
                {
                    foreach( Entry propEntry in this )
                    {
                        int propId = propEntry.Key;
                        IntArrayList linkList = propEntry.Value as IntArrayList;
                        if ( linkList != null )
                        {
                            for( int i=linkList.Count-1; i >= 0; i-- )
                            {
                                if ( MyPalStorage.Storage.IsLinkDirected( propId ) )
                                    propId = -propId;
                            
                                Resource target = (Resource) MyPalStorage.Storage.LoadResource( linkList [i], true, -1 );
                                if ( target.IsDeleting )
                                {
                                    linkList.RemoveAt( i );
                                    continue;
                                }
                            
                                if ( AddLinkSide( target, propId, this ) )
                                {
                                    addedLinks.Add( new Pair( linkList [i], propId ) );
                                }
                            
                                if ( propEntry.Key < 0 )
                                {
                                    MyPalStorage.Storage.SaveLink( linkList [i], _ID, -propEntry.Key );
                                }
                                else
                                {
                                    MyPalStorage.Storage.SaveLink( _ID, linkList [i], propEntry.Key );
                                }
                            }
                            if ( linkList.Count == 0 )
                            {
                                if ( emptyLinkLists == null )
                                {
                                    emptyLinkLists = new IntArrayList();
                                }
                                emptyLinkLists.Add( propEntry.Key );
                            }
                        }
                        else if ( propEntry.Value is Boolean )
                        {
                            MyPalStorage.Storage.CreateBoolProperty( this, propId );
                        }
                        else if ( propEntry.Value is PropertyStringList )
                        {
                            ((PropertyStringList) propEntry.Value).CommitTransient();
                        }
                        else
                        {
                            MyPalStorage.Storage.CreateProperty( this, propId, propEntry.Value );
                            PropDataType dataType = MyPalStorage.Storage.PropTypes [propId].DataType;
                            if ( dataType == PropDataType.Blob )
                            {
                                // propValue passed to the function is a stream, and the props hash
                                // must actually contain a blob
                                propEntry.Value = MyPalStorage.Storage.GetBlobProperty( _ID, propId );
                            }
                        }
                    }

                    if ( emptyLinkLists != null )
                    {
                        for( int i=0; i<emptyLinkLists.Count; i++ )
                        {
                            Remove( emptyLinkLists [i] );
                        }
                    }
                }
                finally
                {
                    UnLock();
                }

                // fire LinkAdded events when we are outside of lock
                for( int i=0; i<addedLinks.Count; i++ )
                {
                    Pair pair = (Pair) addedLinks [i];
                    int resId = (int) pair.First;
                    int propId = (int) pair.Second;

                    Resource target = (Resource) MyPalStorage.Storage.LoadResource( resId );
                    MyPalStorage.Storage.OnLinkAdded( target, this, propId );
                }
            }
            finally
            {
                ArrayListPool.Dispose( addedLinks );
            }
        }

        public override int EstimateMemorySize()
        {
            Lock();
            try
            {
                int result = 8 + 16 +/* SpinWaitLock */ 8;  // object header + common size occupied by resource
                result += base.EstimateMemorySize();
                foreach( Entry e in this )
                {
                    if ( e.Key == ResourceProps.Id )  // fake property for ID before deletion
                        continue;

                    switch( Core.ResourceStore.PropTypes [e.Key].DataType )
                    {
                        case PropDataType.Int: 
                            result += 12; break;

                        case PropDataType.String:
                            if ( e.Value is string )
                            {
                                string str = (string) e.Value;
                                result += 20 + 2 * str.Length;
                            }
                            else
                            {
                                result += 12;
                            }
                            break;

                        case PropDataType.Date:
                            result += 16; break;

                        case PropDataType.Link:
                            IntArrayList list = (IntArrayList) e.Value;
                            result += 16 + list.Capacity * 4;
                            break;

                        case PropDataType.Blob:
                            result += 8; break;   // TODO: get real size

                        case PropDataType.Double:
                            result += 16; break;

                        case PropDataType.LongString:
                            if ( e.Value is string )
                            {
                                string lstr = (string) e.Value;
                                result += 20 + 2 * lstr.Length;
                            }
                            else
                            {
                                result += 12;                                    
                            }
                            break;

                        case PropDataType.Bool: 
                            result += 12; break;

                        case PropDataType.StringList:
                            PropertyStringList strList = (PropertyStringList) e.Value;
                            result += strList.EstimateMemorySize();
                            break;
                    }
                }
                if ( _displayName != null )
                {
                    result += 20 + 2 * _displayName.Length;
                }
                return result;
            }
            finally
            {
                UnLock();
            }
        }
    }
}
