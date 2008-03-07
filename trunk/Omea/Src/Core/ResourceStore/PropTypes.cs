/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using System.Collections.Specialized;
using System.Collections;
using JetBrains.Omea.Database;
using System.Diagnostics;

namespace JetBrains.Omea.ResourceStore
{
    /**
     * Cached information for a single property type.
     */

    internal class PropTypeItem: IPropType
    {
        private int _id;
        private string _name;
        private PropDataType _type;
        private PropTypeFlags _flags;
        private string _displayName;
        private string _reverseDisplayName;
        private bool _ownerPluginLoaded;

        internal PropTypeItem( int ID, string name, PropDataType type, PropTypeFlags flags )
        {
            _id     = ID;
            _name   = name;
            _type   = type;
            _flags  = flags;
            _displayName = null;
            _reverseDisplayName = null;
            _ownerPluginLoaded = true;        
        }

        public int Id   
        { 
            get { return _id; } 
        }

        public string Name 
        { 
            get  { return _name; } 
        }

        public PropDataType DataType 
        { 
            get { return _type; } 
        }

        public string DisplayName
        {
            get { return _displayName; }
        }

        public string ReverseDisplayName
        {
            get { return _reverseDisplayName; }
        }

        public PropTypeFlags Flags
        {
            get { return _flags; }
            set
            {
                if ( _flags != value )
                {
                    (MyPalStorage.Storage.PropTypes as PropTypeCollection).UpdatePropTypeFlags( _id, value );
                }
            }
        }

        public bool OwnerPluginLoaded
        {
            get { return _ownerPluginLoaded; }
        }

        public bool HasFlag( PropTypeFlags flag )
        {
            return (_flags & flag) != 0;
        }

        internal void SetDisplayNames( string displayName, string reverseDisplayName )
        {
            _displayName = displayName;
            _reverseDisplayName = reverseDisplayName;
        }
        
        internal void SetDataType( PropDataType dataType )
        {
            _type = dataType;
        }

        internal void SetFlags( PropTypeFlags flags )
        {
            _flags = flags;
        }

        internal void SetOwnerPluginUnloaded()
        {
            _ownerPluginLoaded = false;
        }
    }

    /**
     * Collection of property types.
     */
    
    internal class PropTypeCollection: IPropTypeCollection
    {
        private MyPalStorage   _storage;
        private ITable         _propTypeTable;
        private ArrayList      _propTypeCache         = new ArrayList();
        private PropTypeItem[] _pseudoProps           = new PropTypeItem[ 3 ];
        private Hashtable      _propTypeNameCache     = CollectionsUtil.CreateCaseInsensitiveHashtable();
        private bool           _propTypesCached;

        internal PropTypeCollection( MyPalStorage storage, ITable propTypeTable )
        {
            _storage = storage;
            _propTypeTable = propTypeTable;

            _pseudoProps [0] = new PropTypeItem( ResourceProps.Id,
                "", PropDataType.Int, PropTypeFlags.Normal );
            _pseudoProps [1] = new PropTypeItem( ResourceProps.DisplayName,
                "DisplayName", PropDataType.String, PropTypeFlags.Normal );
            _pseudoProps [1].SetDisplayNames( "Name", null );
            _pseudoProps [2] = new PropTypeItem( ResourceProps.Type,
                "Type", PropDataType.String, PropTypeFlags.Normal );
        }

        public IEnumerator GetEnumerator()
        {
            return new SkipNullEnumerator( _propTypeCache );
        }

        public IPropType this [int propID]
        {
            get
            {
                if ( propID < 0 )
                {
                    PropTypeItem revItem = FindPropTypeItem( -propID );
                    if ( revItem.HasFlag( PropTypeFlags.DirectedLink ) )
                    {
                        return revItem;
                    }
                    throw new StorageException( "Invalid property type ID " + propID );
                }
                if ( propID >= ResourceProps.Id )
                {
                    return _pseudoProps [propID - ResourceProps.Id];
                }
                
                return FindPropTypeItem( propID );
            }
        }

        public IPropType this [string propName]
        {
            get
            {
                IPropType propType = (IPropType) _propTypeNameCache [propName];
                if ( propType == null )
                {
                    throw new StorageException( "Invalid property type name " + propName );
                }
                return propType;
            }
        }

        public int Count
        {
            get { return _propTypeNameCache.Count; }
        }

        public int Register( string name, PropDataType propType )
        {
            return Register( name, propType, PropTypeFlags.Normal, null );
        }
		
        public int Register( string name, PropDataType propType, PropTypeFlags flags )
        {
            return Register( name, propType, flags, null );
        }

        public int Register( string name, PropDataType dataType, PropTypeFlags flags, IPlugin ownerPlugin )
        {
            bool newPropType = false;
            int ID = RegisterPropTypeInternal( name, dataType, flags, false, out newPropType );

            CreateOrUpdatePropTypeResource( name, dataType, flags, ownerPlugin, ID, newPropType );

            return ID;
        }

        internal void CreateOrUpdatePropTypeResource( string name, PropDataType dataType, PropTypeFlags flags, IPlugin ownerPlugin, int ID, bool newPropType )
        {
            if ( newPropType )
            {    
                IResource res;
                try
                {
                    res = CreatePropTypeResource( ID, name, dataType, flags );
                }
                catch( ResourceRestrictionException ex )  // OM-9471
                {
                    MyPalStorage.Storage.OnIndexCorruptionDetected( "ResourceRestrictionException when creating PropType resource: " + 
                        ex.Message );
                    return;
                }
                _storage.SetOwnerPlugin( res, ownerPlugin );
            }
            else
            {
                IResource res = _storage.FindUniqueResource( "PropType", "Name", name );

                if ( res != null )
                {
                     res.SetProp( "Flags", (int) this [name].Flags );  // ensure OR'ed flags are applied correctly
                    _storage.SetOwnerPlugin( res, ownerPlugin );                
                }
                else
                {
                    MyPalStorage.Storage.OnIndexCorruptionDetected( "Could not find PropType resource for property type " + name );
                }
            }
        }

        /**
         * Sets the display name for a property or a non-directed link.
         */

        public void RegisterDisplayName( int propID, string displayName )
        {
            if ( this [propID].DataType == PropDataType.Link && this [propID].HasFlag( PropTypeFlags.DirectedLink ) )
            {
                throw new StorageException( "Both Source and Target display names must be specified for directed links" );
            }

            RegisterPropDisplayNameInternal( propID, displayName, null );
        }

        /**
         * Sets the display name for a directed link,
         */

        public void RegisterDisplayName( int propID, string fromDisplayName, string toDisplayName )
        {
            if ( this [propID].DataType != PropDataType.Link || !this[propID].HasFlag( PropTypeFlags.DirectedLink ) )
            {
                throw new StorageException( "Both Source and Target display names can only be specified for directed links" );
            }
            RegisterPropDisplayNameInternal( propID, fromDisplayName, toDisplayName );
        }

        /**
         * Checks if all property types in the specified list are registered.
         */

        public bool Exist( params string[] propTypeNames )
        {
            foreach( string name in propTypeNames )
            {
                if ( _propTypeNameCache [name] == null ) return false;
            }
            return true;
        }

        private PropTypeItem FindPropTypeItem( int propID )
        {
            if ( propID < 0 || propID >= _propTypeCache.Count )
                throw new StorageException( "Invalid property type ID " + propID );
                
            PropTypeItem item = (PropTypeItem) _propTypeCache [propID];
            if (item == null)
                throw new StorageException( "Invalid property type ID " + propID );
            return item;
        }

        /**
         * Loads the property types to the cache hash table.
         */

        internal void CachePropTypes()
        {
            IResultSet rs = _propTypeTable.CreateResultSet( 0 );
            try
            {                                       
                foreach( IRecord rec in rs )
                {
                    int ID      = rec.GetIntValue( 0 );
                    string name = rec.GetStringValue( 1 );
                    AddPropTypeToCache( ID, name, (PropDataType) rec.GetIntValue( 2 ),
                        (PropTypeFlags) rec.GetIntValue( 3 ) );
                }
            }
            finally
            {
                rs.Dispose();
            }
            _propTypesCached = true;
        }

        /// <summary>
        /// Adds a property type to the cache.
        /// </summary>
        internal void AddPropTypeToCache( int ID, string name, PropDataType propType, PropTypeFlags flags )
        {
            if ( ID < 0 || ID > 65536 )
                throw new BadIndexesException( "Invalid property type ID " + ID );
            
            lock( _propTypeCache )
            {
                PropTypeItem propTypeItem = new PropTypeItem( ID, name, propType, flags );
                while( _propTypeCache.Count < ID )
                    _propTypeCache.Add( null );

                if ( _propTypeCache.Count == ID )
                {
                    _propTypeCache.Add( propTypeItem );
                }
                else
                {
                    _propTypeCache [ID] = propTypeItem;
                }
                
                _propTypeNameCache [name] = propTypeItem;
            }
        }

        internal void CachePropDisplayNames()
        {
            // now that the cache is filled, we can use the properties to load
            // display names
            foreach( PropTypeItem item in _propTypeCache )
            {
                if ( item != null )
                {
                    IResource propTypeRes = _storage.FindUniqueResource( "PropType", "ID", item.Id );
                    if ( propTypeRes != null )
                    {
                        item.SetDisplayNames( propTypeRes.GetStringProp( "PropDisplayName" ),
                            propTypeRes.GetStringProp( "ReverseDisplayName" ) );
                    }
                }
            }
        }

        /**
         * Adds a record for the specified prop type to the DB.
         */

        internal int RegisterPropTypeInternal( string name, PropDataType propType, PropTypeFlags flags, 
            bool forceType, out bool newPropType )
        {
            _storage.CheckOwnerThread();
            IRecord rec = _propTypeTable.GetRecordByEqual( 1, name );
            if ( rec != null )
            {
                if ( !_propTypeNameCache.ContainsKey( name ) )
                {
                    throw new BadIndexesException( "Property type " + name + " found in PropTypes table but missing in name cache" );
                }

                bool recordChanged = false;
                if ( rec.GetIntValue( 2 ) != (int) propType ) 
                {
                    if ( forceType )
                    {
                        rec.SetValue( 2, IntInternalizer.Intern( (int) propType ) );
                        ((PropTypeItem) this[name]).SetDataType( propType );
                        recordChanged = true;
                    }
                    else
                    {
                        throw new StorageException( "Inconsistent registration for property type " + name +
                            ": old type " + (PropDataType) rec.GetIntValue( 2 ) + ", new type " + propType );
                    }
                }
                int propId = rec.GetIntValue( 0 );
                PropTypeFlags newFlags = flags | this [propId].Flags;
                if ( rec.GetIntValue( 3 ) != (int) newFlags )
                {
                    rec.SetValue( 3, (int) newFlags );
                    recordChanged = true;
                }
                if ( recordChanged )
                {
                    rec.Commit();
                }

                newPropType = false;
                PropTypeItem propTypeItem = (PropTypeItem) _propTypeCache [propId];
                propTypeItem.SetFlags( newFlags );
                return propId;
            }

            if ( (flags & ( PropTypeFlags.DirectedLink | PropTypeFlags.CountUnread )) != 0 &&
                propType != PropDataType.Link )
            {
                throw new StorageException( "DirectedLink and CountUnread flags can be used only on Link properties" );
            }

            int ID;
            lock( _propTypeTable )
            {
                IRecord propertyType = _propTypeTable.NewRecord();
                propertyType.SetValue( 1, name );
                propertyType.SetValue( 2, IntInternalizer.Intern( (int) propType ) );
                propertyType.SetValue( 3, (int) flags );
                _storage.SafeCommitRecord( propertyType, "PropTypeCollection.RegisterPropTypeInternal" );
                ID = propertyType.GetID();
                if ( ID > 65536 )
                {
                    MyPalStorage.Storage.OnIndexCorruptionDetected( "Invalid next ID in property type table" );
                }
            }

            AddPropTypeToCache( ID, name, propType, flags );
            
            newPropType = true;
            return ID;
        }

        /// <summary>
        /// Creates a resource describing the property type.
        /// </summary>
        private IResource CreatePropTypeResource( int ID, string name, PropDataType propType, PropTypeFlags flags )
        {
            IResource res = _storage.BeginNewResource( "PropType" );
            res.SetProp( _storage.Props.Name, name );
            res.SetProp( _storage.Props.TypeId, ID );
            res.SetProp( _storage.Props.DataType, (int) propType );
            res.SetProp( _storage.Props.Flags, (int) flags );
            res.EndUpdate();
            return res;
        }

        internal void   UpdatePropTypeFlags( int propId, PropTypeFlags flags )
        {
            PropTypeItem propType = (PropTypeItem) _propTypeCache [propId];
            propType.SetFlags( flags );

            IResource res = _storage.FindUniqueResource( "PropType", "ID", propId );
            if ( res != null )
            {
                res.SetProp( "Flags", (int) flags );

                UpdatePropTypeRecord( propType.Name, res );
            }
            else
            {
                MyPalStorage.Storage.OnIndexCorruptionDetected( "Could not find PropType resource with ID " + propId );
            }
        }

        /**
         * When a PropType resource is changed, updates the DB data for it.
         */

        internal void UpdatePropType( Resource res )
        {
            string propName = res.GetStringProp( "Name" );
            if ( propName == null )
            {
                MyPalStorage.Storage.OnIndexCorruptionDetected( "UpdatePropType: Name property missing on PropType resource" );
                return;
            }

            int propId = this [propName].Id;
            
            UpdatePropTypeRecord( propName, res );

            PropTypeItem propTypeItem = FindPropTypeItem( propId );
            Debug.Assert( propTypeItem.Name == propName );
            propTypeItem.SetFlags( (PropTypeFlags) res.GetIntProp( "Flags" ) );
        }

        private void UpdatePropTypeRecord( string propName, IResource res )
        {
            IResultSet rs = _propTypeTable.CreateModifiableResultSet( 1, propName );

            try
            {
                IEnumerator enumerator = rs.GetEnumerator();
                using( (IDisposable) enumerator )
                {
                    if ( !enumerator.MoveNext() )
                        throw new StorageException( "Cannot find the property type to be updated" );

                    IRecord rec = (IRecord) enumerator.Current;
                    if ( rec.GetIntValue( 2 ) != res.GetIntProp( "DataType" ) )
                    {
                        throw new StorageException( "Invalid attempt to change data type of property " + propName + 
                            " from " + (PropDataType) rec.GetIntValue( 2 ) + 
                            " to " + (PropDataType) res.GetIntProp( "DataType" ) );
                    }

                    rec.SetValue( 3, res.GetIntProp( "Flags" ) );
                    _storage.SafeCommitRecord( rec, "PropTypeCollection.UpdatePropTypeRecord" );
                }
            }
            finally
            {
                rs.Dispose();
            }
        }

        /**
         * Unregisters the specified property type and deletes all properties
         * of that type.
         */

        public void Delete( int id )
        {
            PropTypeItem item = FindPropTypeItem( id );
            if ( item.DataType == PropDataType.Link )
            {
                _storage.DeleteLinksOfType( id );
            }
            else
            {
                _storage.DeletePropsOfType( id );
            }
            ResourceRestrictions.DeletePropRestrictions( id );

            IResultSet rs = _propTypeTable.CreateModifiableResultSet( 0, id );
            try
            {
                SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "PropTypes.Delete" );
                using( enumerator )
                {
                    if ( !enumerator.MoveNext() )
                    {
                        MyPalStorage.Storage.OnIndexCorruptionDetected( "PropTypeCollection.Delete: Attempt to delete non-existing property type " + id );
                    }
                    else
                    {
                        IRecord rec = enumerator.Current;
                        _storage.SafeDeleteRecord( rec, "PropTypes.Delete" );
                    }
                }
            }
            finally
            {
                rs.Dispose();
            }
            
            IResource propTypeRes = _storage.FindUniqueResource( "PropType", "ID", id );
            Debug.Assert( propTypeRes != null );
            propTypeRes.Delete();
        }

        internal void RegisterPropDisplayNameInternal( int propID, string fromDisplayName, string toDisplayName )
        {
            PropTypeItem propTypeItem = (PropTypeItem) _propTypeCache [propID];
            
            IResource propTypeRes = _storage.FindUniqueResource( "PropType", "ID", propID );
            if ( propTypeRes == null )
            {
                MyPalStorage.Storage.OnIndexCorruptionDetected( "Property type not found in RegisterPropDisplayNameInternal" );
                return;
            }
            propTypeRes.SetProp( "PropDisplayName", fromDisplayName );
            if ( toDisplayName == null )
            {
                propTypeRes.DeleteProp( "ReverseDisplayName" );
            }
            else
            {
                propTypeRes.SetProp( "ReverseDisplayName", toDisplayName );
            }
            
            propTypeItem.SetDisplayNames( fromDisplayName, toDisplayName );
        }

        /**
         * Returns the display name for a property or link.
         */

        public string GetPropDisplayName( int propId )
        {
            bool reverse = false;
            bool directedLink = false;

            PropTypeItem ptItem = (PropTypeItem) this [propId];
            if ( ptItem.DataType == PropDataType.Link && ptItem.HasFlag( PropTypeFlags.DirectedLink ) )
            {
                directedLink = true;
                if ( propId < 0 )
                {
                    propId = -propId;
                    reverse = true;
                }
            }

            string result;
            if ( reverse )
            {
                result = ptItem.ReverseDisplayName;
                if ( result == null )
                {
                    result = ptItem.Name + " Target";
                }
            }
            else
            {
                result = ptItem.DisplayName;
                if ( result == null )
                {
                    if ( directedLink )
                    {
                        result = ptItem.Name + " Source";
                    }
                    else
                    {
                        result = ptItem.Name;
                    }
                }
            }
            return result;
        }

        internal bool IsValidType( int propId )
        {
            if ( propId < 0 )
            {
                return false;
            }
            if ( _propTypesCached )
            {
                if ( propId >= _propTypeCache.Count || _propTypeCache [propId] == null )
                {
                    return false;
                }
            }
            return true;
        }
    }

    internal class SkipNullEnumerator: IEnumerator
    {
        private IEnumerator _baseEnumerator;

        internal SkipNullEnumerator( IEnumerable baseEnumerable )
        {
            _baseEnumerator = baseEnumerable.GetEnumerator();
        }

        public void Reset()
        {
            _baseEnumerator.Reset();                
        }

        public object Current
        {
            get
            {
                return _baseEnumerator.Current;
            }
        }

        public bool MoveNext()
        {
            while( true )
            {
                if ( !_baseEnumerator.MoveNext() )
                    return false;

                if ( _baseEnumerator.Current != null )
                    return true;
            }
        }
    }
}
