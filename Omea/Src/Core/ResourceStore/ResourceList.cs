// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.Database;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;
using JetBrains.DataStructures;

namespace JetBrains.Omea.ResourceStore
{
    /**
     * An abstract class containing the list of resources. Instances of this
     * class cannot be directly created; use methods of other classes to
     * obtain ResourceLists.
     * All ResourceLists are live (meaning, they reflect the changes in the underlying
     * database).
     */

    public class ResourceList: IResourceList, IUpdateListener
    {
        internal IntArrayList _list = null;
        private ResourceListPredicate _predicate;
        private ResourceComparer _lastComparer = null;
        private ArrayList _propertyProviders = null;
        private BitArray _watchedProperties = null;
        private ResourceIdCollection _idCollection = null;
        private bool _watchDisplayName;
        private bool _isLive;
        private bool _handlersAttached;
        private bool _explicitSort = false;
        private bool _updatePriority = false;
        private ValidResourcesEnumerable _validResourcesEnumerable = null;

        private event ResourceIndexEventHandler ResourceAddedInternal;
        private event ResourceIndexEventHandler ResourceDeletingInternal;
        private event ResourcePropIndexEventHandler ResourceChangedInternal;
        private event ResourcePropIndexEventHandler ChangedResourceDeletingInternal;

        protected internal ResourceList( ResourceListPredicate predicate, bool live )
        {
            _predicate = predicate;
            _isLive = live;
        }

        internal void SetUpdatePriority()
        {
            _updatePriority = true;
        }

        private void SetLive()
        {
            if ( !_handlersAttached )
            {
                _handlersAttached = true;
                MyPalStorage.Storage.AddUpdateListener( this, _updatePriority );
            }
        }

        protected internal ResourceListPredicate Predicate
        {
            get { return _predicate; }
        }

        protected bool IsInstantiated
        {
            get
            {
                return _list != null;
            }
        }

        internal void Instantiate()
        {
            Instantiate( true );
        }

        internal void Instantiate( bool optimize )
        {
            if ( _list == null )
            {
                ResourceListPredicate oldPredicate = _predicate;
                if ( optimize )
                {
                    _predicate = _predicate.Optimize( _isLive );
                }
                if ( MyPalStorage.TraceOperations && !(_predicate is PlainListPredicate ) )
                {
                    if ( !Object.ReferenceEquals( oldPredicate, _predicate ) )
                    {
                        Trace.WriteLine( "Predicate before optimization: " + oldPredicate.ToString() );
                    }
                    Trace.WriteLine( "Instantiating list " + _predicate.ToString() );
                }
                bool predicateSortedById = false;
                _list = _predicate.GetMatchingResources( out predicateSortedById );
                if ( _isLive && !_handlersAttached )
                {
                    SetLive();
                }
                if ( _lastComparer != null )
                {
                    DoSort( _lastComparer, true );
                }
                else if ( predicateSortedById )
                {
                    _lastComparer = new ResourceComparer( this, new SortSettings( ResourceProps.Id, true ), false );
                }
            }
        }

        /**
         * Disconnects the handlers of the resource list and moves it back to predicate state.
         */

        public void Deinstantiate()
        {
            lock( this )
            {
                DetachHandlers();
                _list = null;
            }
        }

        public IResource Find(Predicate<IResource> predicate)
        {
            lock( this )
            {
                foreach( IResource res in this )
                {
                    if ( predicate(res)) return res;
                }
            }
            return null;
        }

        public event ResourceIndexEventHandler ResourceAdded
        {
            add
            {
                if ( _isLive )
                {
                    SetLive();
                }
                ResourceAddedInternal += value;
            }
            remove
            {
                ResourceAddedInternal -= value;
            }
        }

        public event ResourceIndexEventHandler ResourceDeleting
        {
            add
            {
                if ( _isLive )
                {
                    SetLive();
                }
                ResourceDeletingInternal += value;
            }
            remove
            {
                ResourceDeletingInternal -= value;
            }
        }

        public event ResourcePropIndexEventHandler ResourceChanged
        {
            add
            {
                if ( _isLive )
                {
                    SetLive();
                }
                ResourceChangedInternal += value;
            }
            remove
            {
                ResourceChangedInternal -= value;
            }
        }

        public event ResourcePropIndexEventHandler ChangedResourceDeleting
        {
            add
            {
                if ( _isLive )
                {
                    SetLive();
                }
                ChangedResourceDeletingInternal += value;
            }
            remove
            {
                ChangedResourceDeletingInternal -= value;
            }
        }

        protected void Add( IResource res )
        {
            int index = -1;
            lock( this )
            {
                if ( _list != null )
                {
                    index = FindInsertIndex( res );
                    _list.Insert( index, res.Id );
                }
            }
            OnResourceAdded( res, index );
        }

        private void OnResourceAdded( IResource res, int index )
        {
            if ( ResourceAddedInternal != null )
            {
                ResourceAddedInternal( this, new ResourceIndexEventArgs( res, index ) );
            }
        }

        void IUpdateListener.ResourceDeleting( IResource resource )
        {
            if ( !_handlersAttached )
            {
                return;
            }

            if ( _predicate.MatchResource( resource, null ) == PredicateMatch.Match )
            {
                Remove( resource, null );
            }
        }

        protected internal void Remove( IResource res, IPropertyChangeSet cs )
        {
            if ( _list == null )
            {
                RemoveAt( res, -1, cs );
            }
            else
            {
                lock( this )
                {
                    int index = IndexOf( res.Id );
                    if ( index >= 0 )
                    {
                        RemoveAt( res, index, cs );
                    }
                }
            }
        }

        protected void RemoveAt( IResource res, int index, IPropertyChangeSet cs )
        {
            // proceed with delete even if the ResourceDeleting handler throws an exception
            try
            {
                if ( ResourceDeletingInternal != null )
                {
                    ResourceDeletingInternal( this, new ResourceIndexEventArgs( res, index ) );
                }
                if ( cs != null && ChangedResourceDeletingInternal != null )
                {
                    ChangedResourceDeletingInternal( this, new ResourcePropIndexEventArgs( res, index, cs ) );
                }
            }
            finally
            {
                if ( index >= 0 )
                {
                    _list.RemoveAt( index );
                }
            }
        }

        public int IndexOf( IResource res )
        {
            if ( res == null )
            {
                throw new ArgumentNullException( "res" );
            }
            return IndexOf( res.Id );
        }

        public int IndexOf( int resID )
        {
            lock( this )
            {
                Instantiate();
                if ( IsIDSort() )
                {
                    int result = _list.BinarySearch( resID );
                    if ( result < 0 )
                        return -1;
                    return result;
                }
                else
                {
                    return _list.IndexOf( resID );
                }
            }
        }

        public bool Contains( IResource res )
        {
            if ( res == null )
            {
                throw new ArgumentNullException( "res" );
            }
            return _predicate.MatchResource( res, null ) == PredicateMatch.Match;
        }

        /**
         * Tells that the client of the resource list wants to receive notifications on the
         * change of the specified property. (If no watches have been added, changes of any
         * properties are reported to the client.
         */

        public void AddPropertyWatch( int propID )
        {
            if ( propID == ResourceProps.DisplayName )
            {
                if ( _watchedProperties == null )
                {
                    // mark that we have a filter
                    _watchedProperties = new BitArray( 1 );
                }
                _watchDisplayName = true;
                return;
            }

            MyPalStorage.Storage.GetPropDataType( propID );  // this validates the property ID
            if ( _watchedProperties == null )
            {
                _watchedProperties = new BitArray( propID+1 );
            }
            else if ( _watchedProperties.Length < propID+1 )
            {
                _watchedProperties.Length = propID+1;
            }
            _watchedProperties [propID] = true;
        }

        public void AddPropertyWatch( int[] propIds )
        {
            for( int i=0; i<propIds.Length; i++ )
            {
                AddPropertyWatch( propIds [i] );
            }
        }

        /**
         * Checks if the specified property change set intersects the set of watched
         * properties on the resource list.
         */

        private bool ChangesIntersectWatches( IPropertyChangeSet cs )
        {
            if ( _watchedProperties == null || cs == null )
                return true;

            if ( _watchDisplayName && cs.IsDisplayNameAffected )
                return true;

            return ( cs as PropertyChangeSetBase ).Intersects( _watchedProperties );
        }

        void IUpdateListener.ResourceSaved( IResource resource, IPropertyChangeSet changeSet )
        {
            if ( !_handlersAttached )
            {
                return;
            }

            switch( _predicate.MatchResource( resource, changeSet ) )
            {
                case PredicateMatch.Add:
                    Add( resource );
                    break;

                case PredicateMatch.Match:
                    if ( ( ResourceChangedInternal != null || _lastComparer != null ) &&
                        ChangesIntersectWatches( changeSet) )
                    {
                        int index = -1;
                        if ( _list != null )
                        {
                            index = IndexOf( resource );
                        }
                        ProcessResourceChanged( resource, index, changeSet );
                    }
                    break;

                case PredicateMatch.Del:
                    Remove( resource, changeSet );
                    break;
            }
        }

        private void ProcessResourceChanged( IResource res, int index, IPropertyChangeSet changeSet )
        {
            if ( ResourceChangedInternal != null )
            {
                ResourceChangedInternal( this, new ResourcePropIndexEventArgs( res, index, changeSet ) );
            }

            // guard against  race condition OM-9543 (Deinstatiate() may be called while we're processing the change)
            IntArrayList list = _list;
            if ( list != null && _lastComparer != null && index >= 0 )
            {
                // check if the position of the resource in the sorted order is still valid
                int prevCmpResult = 0, nextCmpResult = 0;

                while( index > 0 )
                {
                    IResource prevResource = MyPalStorage.Storage.TryLoadResource( list [index-1] );
                    if ( prevResource == null || prevResource.IsDeleted )
                    {
                        lock( this )
                        {
                            if ( _list == null )
                            {
                                return;
                            }
                            RemoveAt( prevResource, index-1, null );
                            index--;
                        }
                        continue;
                    }
                    prevCmpResult = _lastComparer.CompareResources( prevResource, res );
                    break;
                }

                while( index < list.Count-1 )
                {
                    IResource nextResource = MyPalStorage.Storage.TryLoadResource( list [index+1] );
                    if ( nextResource == null || nextResource.IsDeleted )
                    {
                        lock( this )
                        {
                            if ( _list == null )
                            {
                                return;
                            }
                            RemoveAt( nextResource, index+1, null );
                        }
                        continue;
                    }
                    nextCmpResult = _lastComparer.CompareResources( res, nextResource );
                    break;
                }

                if ( prevCmpResult > 0 || nextCmpResult > 0 )
                {
                    int newIndex;
                    lock( this )
                    {
                        if ( _list == null )
                        {
                            return;
                        }
                        RemoveAt( res, index, null );
                        newIndex = FindInsertIndex( res );
                        _list.Insert( newIndex, res.Id );
                    }
                    OnResourceAdded( res, newIndex );
                }
            }
        }

        public int Count
        {
            get
            {
                Instantiate();
                return _list.Count;
            }
        }

        public IResource this[ int index ]
        {
            get
            {
                Instantiate();
                return MyPalStorage.Storage.LoadResource( _list [index], true, _predicate.GetKnownType() );
            }
        }

        public IResourceIdCollection ResourceIds
        {
            get
            {
                Instantiate();
                if ( _idCollection == null )
                {
                    _idCollection = new ResourceIdCollection( this );
                }
                return _idCollection;
            }
        }

        protected internal IntArrayList ResourceIdArray
        {
            get
            {
                Instantiate();
                return _list;
            }
        }

        public IEnumerable ValidResources
        {
            get
            {
                if ( _validResourcesEnumerable == null )
                {
                    _validResourcesEnumerable = new ValidResourcesEnumerable( this );
                }
                return _validResourcesEnumerable;
            }
        }

        public bool IsLive
        {
            get { return _isLive; }
        }

        public virtual void Dispose()
        {
            if ( _propertyProviders != null )
            {
                foreach( IPropertyProvider provider in _propertyProviders )
                {
                    provider.ResourceChanged -= new PropertyProviderChangeEventHandler( provider_ResourceChanged );
                }
            }
            DetachHandlers();
        }

        private void DetachHandlers()
        {
            if ( _handlersAttached && MyPalStorage.Storage != null )
            {
                _handlersAttached = false;
                MyPalStorage.Storage.RemoveUpdateListener( this, _updatePriority );
            }
        }

        /**
         * Adds a provider for virtual property values to the list.
         */

        public void AttachPropertyProvider( IPropertyProvider provider )
        {
            if ( _propertyProviders == null )
            {
                _propertyProviders = new ArrayList();
            }
            _propertyProviders.Add( provider );
            provider.ResourceChanged += new PropertyProviderChangeEventHandler( provider_ResourceChanged );
        }

        /**
         * Adds all providers from the specified collection, if they are not
         * already added to the provider list.
         */

        private void AttachPropertyProviders( ICollection coll )
        {
            if ( coll != null )
            {
                if ( _propertyProviders == null )
                {
                    _propertyProviders = new ArrayList();
                }

                foreach( IPropertyProvider provider in coll )
                {
                    if ( _propertyProviders.IndexOf( provider ) < 0 )
                    {
                        _propertyProviders.Add( provider );
                        provider.ResourceChanged += new PropertyProviderChangeEventHandler( provider_ResourceChanged );
                    }
                }
            }
        }

        private void provider_ResourceChanged( object sender, PropertyProviderChangeEventArgs e )
        {
            Resource res;
            try
            {
                res = (Resource) MyPalStorage.Storage.LoadResource( e.ResourceId );
            }
            catch( StorageException )
            {
                return;
            }

            MyPalStorage.Storage.OnResourceSaved( res, e.PropId, e.OldValue );
        }

        /**
         * Checks if any of the resources in the list has the property with
         * the specified name.
         */

        public bool HasProp( string propName )
        {
            return HasProp( MyPalStorage.Storage.GetPropId( propName ) );
        }

        /**
         * Checks if any of the resources in the list has the property with
         * the specified ID.
         */

        public virtual bool HasProp( int propID )
        {
            lock( this )
            {
                if ( _propertyProviders != null )
                {
                    for( int i=0; i<_propertyProviders.Count; i++ )
                    {
                        if ( ( _propertyProviders [i] as IPropertyProvider).HasProp( propID ) )
                        {
                            return true;
                        }
                    }
                }

                IPropType propType = MyPalStorage.Storage.PropTypes [propID];
                if ( propType.HasFlag( PropTypeFlags.Virtual ) )
                {
                    return false;
                }

                PropDataType dataType = propType.DataType;
                if ( dataType == PropDataType.LongString || dataType == PropDataType.Blob || dataType == PropDataType.Double )
                {
                    Instantiate();
                    for( int i=0; i<_list.Count; i++)
                    {
                        IResource res = MyPalStorage.Storage.TryLoadResource( _list [i] );
                        if ( res != null && res.HasProp( propID ) )
                            return true;
                    }
                    return false;
                }
            }
            return Intersect( MyPalStorage.Storage.FindResourcesWithProp( null, propID ) ).Count > 0;
        }

        /**
         * Checks if all the resources in the list have the property with the
         * specified name.
         */

        public bool AllHasProp( string propName )
        {
            return AllHasProp( MyPalStorage.Storage.GetPropId( propName ) );
        }

        /**
         * Checks if all the resources in the list have the property with the
         * specified name.
         */

        public bool AllHasProp( int propID )
        {
            lock( this )
            {
                Instantiate();
                if ( _list.Count == 0 )
                    return false;

                for( int i=0; i<_list.Count; i++ )
                {
                    IResource res = MyPalStorage.Storage.LoadResource( _list [i] );
                    if ( !res.HasProp( propID ) )
                        return false;
                }
            }
            return true;
        }

        /**
         * Checks if all resources in the list have the specified type.
         */

        public bool AllResourcesOfType( string type )
        {
            lock( this )
            {
                Instantiate();
                if ( Count == 0 )
                    return false;

                for( int i=0; i<Count; i++ )
                {
                    if ( String.Compare( this [i].Type, type, true, CultureInfo.InvariantCulture ) != 0 )
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /**
         * Returns the array containing all resource types currently contained in
         * the resource list.
         */

        public string[] GetAllTypes()
        {
            lock( this )
            {
                Instantiate();
                if ( _predicate.GetKnownType() >= 0 )
                {
                    return new string[] { MyPalStorage.Storage.ResourceTypes [_predicate.GetKnownType()].Name };
                }
                if( Count > 0 )
                {
                    if ( Count == 1 )
                    {
                        return new string[] { this [0].Type };
                    }

                    HashSet processedTypes = new HashSet();
                    ArrayList resTypeList = ArrayListPool.Alloc();
                    try
                    {
                        foreach( IResource res in ValidResources )
                        {
                            if ( !processedTypes.Contains( res.Type ) )
                            {
                                processedTypes.Add( res.Type );
                                resTypeList.Add( res.Type );
                            }
                        }
                        resTypeList.Sort();
                        return (string[]) resTypeList.ToArray( typeof(string) );
                    }
                    finally
                    {
                        ArrayListPool.Dispose( resTypeList );
                    }
                }
            }
            return emptyStringArray;
        }

        private static string[] emptyStringArray = new string[] {};

        /**
         * Returns the value of the specified property for the resource
         * with the given index.
         */

        public virtual object GetPropValue( IResource res, int propId )
        {
            if ( _propertyProviders != null )
            {
                for( int i=0; i<_propertyProviders.Count; i++ )
                {
                    object val = (_propertyProviders [i] as IPropertyProvider).GetPropValue( res, propId );
                    if ( val != null )
                        return val;
                }
            }
            return res.GetProp( propId );
        }

        /**
         * Returns the text of the specified property for the resource with
         * the given index.
         */

        public string GetPropText( int index, string propName )
        {
            return GetPropText( index, MyPalStorage.Storage.GetPropId( propName ) );
        }

        public string GetPropText( int index, int propId )
        {
            return GetPropText( this [index], propId );
        }

        public string GetPropText( IResource res, int propId )
        {
            if ( MyPalStorage.Storage.GetPropDataType( propId ) == PropDataType.Link )
                return res.GetPropText( propId );

            object propValue = GetPropValue( res, propId );
            if ( propValue == null )
                return "";

            if ( propValue is Double )
            {
                double dblValue = (double) propValue;
                return dblValue.ToString( "N" );
            }
            else
                return propValue.ToString();
        }

        /**
         * Checks if the object at the specified index in the list has the
         * specified property.
         */

        public bool HasProp( int index, string propName )
        {
            return HasProp( index, MyPalStorage.Storage.GetPropId( propName ) );
        }

        public bool HasProp( int index, int propID )
        {
            return GetPropValue( this [index], propID ) != null;
        }

        public bool HasProp( IResource res, int propID )
        {
            return GetPropValue( res, propID ) != null;
        }

        public string SortProps
        {
            get
            {
                if ( _lastComparer == null )
                    return "";
                return _lastComparer.SortSettings.ToString( MyPalStorage.Storage );
            }
        }

        public int[] SortPropIDs
        {
            get
            {
                if ( _lastComparer== null )
                    return null;
                return (int[]) _lastComparer.SortSettings.SortProps.Clone();
            }
        }

        public int[] SortDirections
        {
            get
            {
                if ( _lastComparer == null )
                    return null;
                int[] result = new int [_lastComparer.SortSettings.SortProps.Length];
                for( int i=0; i<result.Length; i++ )
                {
                    result [i] = _lastComparer.SortSettings.SortDirections [i] ? 1 : -1;
                }
                return result;
            }
        }

        public SortSettings SortSettings
        {
            get
            {
                if ( _lastComparer == null )
                {
                    return null;
                }
                return _lastComparer.SortSettings;
            }
        }

        internal IComparer LastComparer
        {
            get { return _lastComparer; }
        }

        public bool SortAscending
        {
            get
            {
                if ( _lastComparer == null )
                    return false;
                return _lastComparer.SortSettings.SortAscending;
            }
        }

        /**
         * Checks if the list is sorted by ID.
         */

        internal bool IsIDSort()
        {
            return _lastComparer != null && _lastComparer.IsIdSort();
        }

        /**
         * Finds an index in the sorted order where the specified resource
         * should be inserted.
         */

        private int FindInsertIndex( IResource res )
        {
            if ( _lastComparer == null )
                return _list.Count;

            int index;
            if ( _lastComparer.IsIdSort() )
            {
                index = _list.BinarySearch( res.Id );
            }
            else
            {
                index = BinarySearchWithComparer( res );
            }

            if ( index < 0 )
                return ~index;
            return index;
        }

        private int BinarySearchWithComparer( IResource res )
        {
            int index = 0;
            bool foundDeleted;
            do
            {
                foundDeleted = false;
                try
                {
                    index = _list.BinarySearch( res.Id, _lastComparer );
                }
                catch( InvalidOperationException e )
                {
                    if ( e.InnerException != null && e.InnerException is ResourceDeletedException )
                    {
                        ResourceDeletedException rde = (ResourceDeletedException) e.InnerException;
                        int removeIndex = _list.IndexOf( rde.ResourceId );
                        IResource deletedResource = MyPalStorage.Storage.LoadResource( rde.ResourceId, true, -1 );
                        RemoveAt( deletedResource, removeIndex, null );
                        foundDeleted = true;
                    }
                    else if ( e.InnerException != null && e.InnerException is InvalidResourceIdException )
                    {
                        InvalidResourceIdException irie = (InvalidResourceIdException) e.InnerException;
                        int removeIndex = _list.IndexOf( irie.ResourceId );
                        RemoveAt( null, removeIndex, null );
                        foundDeleted = true;
                    }
                    else
                        throw;
                }
            } while( foundDeleted );
            return index;
        }

        public IEnumerator GetEnumerator()
        {
            Instantiate();
            return new ResourceListEnumerator( _list.GetEnumerator(), _predicate.GetKnownType() );
        }

        public void Sort( SortSettings sortSettings )
        {
            DoSort( new ResourceComparer( this, sortSettings, false ), false );
        }

        public void Sort( string propNames )
        {
            Guard.NullArgument( propNames, "propNames" );
            SortSettings settings = SortSettings.Parse( MyPalStorage.Storage, propNames );
            Sort( settings );
        }

        public void Sort( string propNames, bool ascending )
        {
            Guard.NullArgument( propNames, "propNames" );
            SortSettings settings = SortSettings.Parse( MyPalStorage.Storage, propNames );
            if ( !ascending )
            {
                settings = settings.Reverse();
            }
            Sort( settings );
        }

        public void Sort( int[] propIDs, bool ascending )
        {
            Sort( propIDs, ascending, false );
        }

        public void Sort( int[] propIDs, bool ascending, bool propsEquivalent )
        {
            SortSettings settings = new SortSettings( propIDs, ascending );
            DoSort( new ResourceComparer( this, settings, propsEquivalent ), false );
        }

        public void Sort( int[] propIDs, bool[] sortDirections )
        {
            if ( propIDs.Length != sortDirections.Length )
            {
                throw new ArgumentException( "Property ID count does not match sort direction count" );
            }

            SortSettings settings = new SortSettings( propIDs, sortDirections );
            DoSort( new ResourceComparer( this, settings, false ), false );
        }

        public void Sort( IResourceComparer customComparer, bool ascending )
        {
            DoSort( new ResourceComparer( this, customComparer, ascending ), false );
        }

        private void DoSort( ResourceComparer comparer, bool instantiating )
        {
            lock( this )
            {
                _lastComparer = comparer;
                if ( _list == null || _list.Count == 0 )
                {
                    return;
                }

                if ( _lastComparer.IsIdSort() )
                {
                    _list.Sort();
                }
                else if ( _explicitSort )
                {
                    // If we are sorting a list which has already been sorted,
                    // use RBTree-based stable sort
                    StableSort();
                }
                else
                {
                    QuickSort( instantiating );
                }

                _explicitSort = true;
            }
        }

        private void QuickSort( bool instantiating )
        {
            // to reduce the overhead on multiple resource cache accesses, sort the
            // IResource ArrayList instead of the actual resource ID list
            IResource[] resList = new IResource [_list.Count];

            int destCount = 0;
            if ( instantiating )
            {
                // Another thread can delete any resource at any time, so the list
                // fill must be delete-proof
                for( int i=0; i<_list.Count; i++ )
                {
                    try
                    {
                        IResource res = MyPalStorage.Storage.LoadResource( _list [i], true,
                            _predicate.GetKnownType() );
                        if ( !res.IsDeleted )
                        {
                            resList [destCount++] = res;
                        }
                    }
                    catch( InvalidResourceIdException )
                    {
                        continue;
                    }
                }
            }
            else
            {
                for( int i=0; i<_list.Count; i++ )
                {
                    resList [i] = MyPalStorage.Storage.LoadResource( _list [i], true,
                        _predicate.GetKnownType() );
                }
                destCount = _list.Count;
            }

            Array.Sort( resList, 0, destCount, _lastComparer );

            if ( instantiating )
            {
                int destIndex = 0;
                for( int i=0; i<destCount; i++ )
                {
                    if ( !resList [i].IsDeleted )
                    {
                        _list [destIndex++] = resList [i].Id;
                    }
                }
                _list.RemoveRange( destIndex, _list.Count - destIndex );
            }
            else
            {
                for( int i=0; i<_list.Count; i++ )
                {
                    _list [i] = resList [i].Id;
                }
            }
        }

        private void StableSort()
        {
            IntArrayList badResourceIds = null;

            RedBlackTree tree = new RedBlackTree( _lastComparer );
            for( int i=0; i<_list.Count; i++ )
            {
                IResource res = MyPalStorage.Storage.TryLoadResource( _list [i] );
                if ( res != null )
                {
                    tree.RB_Insert( res );
                }
                else
                {
                    if ( badResourceIds == null )
                    {
                        badResourceIds = new IntArrayList();
                    }
                    badResourceIds.Add( _list [i] );
                }
            }
            RBNodeBase node = tree.GetMinimumNode();
            int destIndex = 0;
            while( node != null )
            {
                _list [destIndex++] = ((IResource) node.Key).Id;
                node = tree.GetNext( node );
            }
            if ( badResourceIds != null )
            {
                for( int j=0; j<badResourceIds.Count; j++ )
                {
                    _list [destIndex++] = badResourceIds [j];
                }
            }
        }

        public void IndexedSort( int propID )
        {
            lock( this )
            {
                if ( _list == null )
                    Instantiate();

                IntHashSet hashSet = new IntHashSet();
                for( int i=0; i<_list.Count; i++ )
                {
                    hashSet.Add( _list [i] );
                }
                int destIndex = 0;
                IResultSet rs = MyPalStorage.Storage.SelectResourcesWithProp( propID );
                foreach( IRecord rec in rs )
                {
                    int id = rec.GetIntValue( 0 );
                    if ( hashSet.Contains( id ) )
                    {
                        _list [destIndex++] = id;
                    }
                }
            }
        }

        public IResourceList Union( IResourceList other )
        {
            return Union( other, false );
        }

        public IResourceList Union( IResourceList other, bool allowMerge )
        {
            if ( other == null )
                return this;

            ResourceList otherList = (ResourceList) other;

            if ( allowMerge )
            {

                ResourceList destList = this;
                ResourceList srcList = otherList;
                UnionPredicate predicate = null;
                if ( _list == null )
                {
                    predicate = _predicate as UnionPredicate;
                }
                if ( predicate == null && otherList._list == null )
                {
                    srcList = this;
                    destList = otherList;
                    predicate = otherList._predicate as UnionPredicate;
                }
                if ( predicate != null )
                {
                    predicate.AddSource( srcList.Predicate );
                    return MergeListData( destList, srcList );
                }
            }

            UnionPredicate pred = new UnionPredicate( _predicate, otherList._predicate );

            ResourceList result = new ResourceList( pred, _isLive || otherList.IsLive );
            return MergeListData( result, this, otherList );
        }

        public IResourceList Intersect( IResourceList other )
        {
            return Intersect( other, false );
        }

        public IResourceList Intersect( IResourceList other, bool allowMerge )
        {
            if ( other == null )
                return this;

            ResourceList otherList = (ResourceList) other;

            if ( _predicate.Equals( otherList.Predicate) )
            {
                Trace.WriteLine( "Attempt to intersect lists with equal predicates " + _predicate.ToString() );
                return this;
            }

            if ( allowMerge )
            {
                ResourceList destList = this;
                ResourceList srcList = otherList;
                IntersectionPredicate predicate = null;
                if ( _list == null )
                {
                    predicate = _predicate as IntersectionPredicate;
                }
                if ( predicate == null && otherList._list == null )
                {
                    srcList = this;
                    destList = otherList;
                    predicate = otherList._predicate as IntersectionPredicate;
                }
                if ( predicate != null )
                {
                    predicate.AddSource( srcList.Predicate );
                    return MergeListData( destList, srcList );
                }
            }

            IntersectionPredicate pred = new IntersectionPredicate( _predicate, otherList._predicate );

            ResourceList result = new ResourceList( pred, _isLive || otherList.IsLive );
            return MergeListData( result, this, otherList );
        }

        /**
         * Merges the data on liveness, virtual properties, sorting from the source list
         * into the destination list.
         */

        private static ResourceList MergeListData( ResourceList dest, ResourceList source )
        {
            if ( source.IsLive )
                dest._isLive = true;
            dest.AttachPropertyProviders( source._propertyProviders );
            if ( dest.LastComparer == null && source.LastComparer != null )
            {
                dest._lastComparer = source._lastComparer;
            }
            return dest;
        }

        /**
         * Merges the data on liveness, virtual properties, sorting from the two
         * source lists to the dest list.
         */

        private static ResourceList MergeListData( ResourceList dest, ResourceList source1, ResourceList source2 )
        {
            dest.AttachPropertyProviders( source1._propertyProviders );
            dest.AttachPropertyProviders( source2._propertyProviders );
            if ( IsSameSort( source1, source2 ) || source2._lastComparer == null )
            {
                dest._lastComparer = source1._lastComparer;
            }
            else if ( source1._lastComparer == null )
            {
                dest._lastComparer = source2._lastComparer;
            }
            return dest;
        }

        public IResourceList Minus( IResourceList other )
        {
            if ( other == null )
                return this;

            ResourceList otherList = (ResourceList) other;
            MinusPredicate pred = new MinusPredicate(
                _predicate, otherList._predicate );

            ResourceList result = new ResourceList( pred, _isLive || otherList.IsLive );
            result.AttachPropertyProviders( _propertyProviders );
            result.AttachPropertyProviders( otherList._propertyProviders );
            if ( _lastComparer != null )
            {
                result._lastComparer = _lastComparer;
            }
            return result;
        }

        /**
         * Deletes all resources in the list from the DB.
         */

        public void DeleteAll()
        {
            Instantiate();
            for( int i=_list.Count-1; i >= 0; i-- )
            {
                IResource res;
                try
                {
                    res = MyPalStorage.Storage.LoadResource( _list [i] );
                }
                catch( ResourceDeletedException )
                {
                    continue;
                }
                catch( InvalidResourceIdException )
                {
                    continue;
                }

                res.Delete();
            }
        }

        internal static bool IsSameSort( ResourceList resList1, ResourceList resList2 )
        {
            int[] sortProps1 = resList1.SortPropIDs;
            int[] sortProps2 = resList2.SortPropIDs;
            if ( sortProps1 == null || sortProps2 == null )
                return false;

            if ( sortProps1.Length != sortProps2.Length )
                return false;

            for( int i=0; i<sortProps1.Length; i++ )
            {
                if ( sortProps1 [i] != sortProps2 [i] )
                    return false;
                if ( resList1.SortDirections [i] != resList2.SortDirections [i] )
                    return false;
            }
            return true;
        }

        public override string ToString()
        {
            Instantiate();
            StringBuilder sb = new StringBuilder( ListTypeToString() + ": (" );
            foreach( int resID in _list )
            {
                sb.Append( " " );
                sb.Append( resID );
            }
            sb.Append( " )" );
            return sb.ToString();
        }

        public string ListTypeToString()
        {
            return _predicate.ToString();
        }

        void IUpdateListener.Trace()
        {
            Trace.WriteLine( ListTypeToString() );
        }

        int IUpdateListener.GetKnownType()
        {
            return _predicate.GetKnownType();
        }

        internal int GetPredicateKnownType()
        {
            return _predicate.GetKnownType();
        }

        private class ResourceListEnumerator: IEnumerator
        {
            private IntArrayList.IntArrayListEnumerator _baseEnumerator;
            private int _knownType;

            internal ResourceListEnumerator( IntArrayList.IntArrayListEnumerator baseEnumerator, int knownType )
            {
                _baseEnumerator = baseEnumerator;
                _knownType = knownType;
            }

            public void Reset()
            {
                _baseEnumerator.Reset();
            }

            public object Current
            {
                get
                {
                    return MyPalStorage.Storage.LoadResource( _baseEnumerator.Current, true, _knownType );
                }
            }

            public bool MoveNext()
            {
                return _baseEnumerator.MoveNext();
            }
        }

        private class ResourceIdCollection: IResourceIdCollection
        {
            private ResourceList _baseList;

            public ResourceIdCollection( ResourceList baseList )
            {
                _baseList = baseList;
            }

            int ICollection.Count
            {
                get { return _baseList.Count; }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get { return this; }
            }

            void ICollection.CopyTo( Array array, int index )
            {
                _baseList.Instantiate();
                _baseList._list.CopyTo( array, index );
            }

            int IResourceIdCollection.this [int index ]
            {
                get
                {
                    _baseList.Instantiate();
                    return _baseList._list [index];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _baseList._list.GetEnumerator();
            }
        }

        private class ValidResourcesEnumerable: IEnumerable
        {
            private ResourceList _resourceList;

            public ValidResourcesEnumerable( ResourceList resourceList )
            {
                _resourceList = resourceList;
            }

            public IEnumerator GetEnumerator()
            {
                return new ValidResourcesEnumerator( _resourceList );
            }
        }

        private class ValidResourcesEnumerator: IEnumerator
        {
            private ResourceList _resourceList;
            private int _index;
            private IResource _curResource;

            public ValidResourcesEnumerator( ResourceList resourceList )
            {
                _resourceList = resourceList;
                _index = -1;
            }

            public bool MoveNext()
            {
                while( _index < _resourceList.Count-1 )
                {
                    _index++;
                    _curResource = MyPalStorage.Storage.TryLoadResource( _resourceList._list [_index] );
                    if ( _curResource != null )
                    {
                        return true;
                    }
                }
                _curResource = null;
                return false;
            }

            public void Reset()
            {
                _index = -1;
            }

            public object Current
            {
                get
                {
                    if ( _curResource == null )
                    {
                        throw new InvalidOperationException( "The enumerator is not positioned on a valid element" );
                    }
                    return _curResource;
                }
            }
        }
    }
}
