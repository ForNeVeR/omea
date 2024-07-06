// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using System.Collections;
using System.Collections.Specialized;
using JetBrains.Omea.Database;
using System.Diagnostics;
using System.Globalization;

namespace JetBrains.Omea.ResourceStore
{
    /**
     * Cached information for a single resource type.
     */

    internal class ResourceTypeItem: IResourceType
    {
        private int                _id;
        private string            _name;
        private string            _displayName;
        private DisplayNameMask   _displayNameTemplate;
        private ResourceTypeFlags _flags;
        private bool              _ownerPluginLoaded;
        private ResourceList      _resourcesOfType;

        internal ResourceTypeItem( int ID, string name, string displayNameTemplate, ResourceTypeFlags flags )
        {
            _id = ID;
            _name = name;
            _displayName = name;
            _displayNameTemplate = new DisplayNameMask( displayNameTemplate, true );
            _flags = flags;
            _ownerPluginLoaded = true;
        }

        public int Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string DisplayName
        {
            get
            {
                if ( _displayName == null || _displayName.Length == 0 )
                {
                    return _name;
                }
                return _displayName;
            }
            set
            {
                (MyPalStorage.Storage.ResourceTypes as ResourceTypeCollection).UpdateResourceType(
                    _name, _displayNameTemplate.ToString(), _flags, value );

                IResource res = MyPalStorage.Storage.FindUniqueResource( "ResourceType",
                    MyPalStorage.Storage.Props.Name, _name );
                res.SetProp( "PropDisplayName", value );
            }
        }

        public string ResourceDisplayNameTemplate
        {
            get { return _displayNameTemplate.ToString(); }
            set
            {
                if ( _displayNameTemplate.ToString() != value )
                {
                    (MyPalStorage.Storage.ResourceTypes as ResourceTypeCollection).UpdateResourceType(
                        _name, value, _flags, _displayName );

                    IResource res = MyPalStorage.Storage.FindUniqueResource( "ResourceType",
                        MyPalStorage.Storage.Props.Name, _name );
                    if ( res == null )
                    {
                        MyPalStorage.Storage.OnIndexCorruptionDetected( "set_ResourceDisplayNameTemplate: Could not find resource for resource type " +
                            _name );
                    }
                    else
                    {
                        res.SetProp( MyPalStorage.Storage.Props.DisplayNameMask, value );
                    }
                }
            }
        }

        internal DisplayNameMask DisplayNameTemplate
        {
            get { return _displayNameTemplate; }
        }

        public ResourceTypeFlags Flags
        {
            get { return _flags; }
            set
            {
                ResourceTypeCollection resTypes = MyPalStorage.Storage.ResourceTypes as ResourceTypeCollection;
                resTypes.UpdateResourceType( _name, _displayNameTemplate.ToString(), value, _displayName );

                IResource res = MyPalStorage.Storage.FindUniqueResource( "ResourceType",
                    MyPalStorage.Storage.Props.Name, _name );
                resTypes.SetResourceTypeFlags( res, value );
            }
        }

        public bool HasFlag( ResourceTypeFlags flag )
        {
            return (_flags & flag) != 0;
        }

        internal void SetFlags( ResourceTypeFlags flags )
        {
            _flags = flags;
        }

        internal void SetDisplayName( string displayName )
        {
            _displayName = displayName;
        }

        internal void SetDisplayNameTemplate( string displayNameTemplate, bool validate )
        {
            _displayNameTemplate = new DisplayNameMask( displayNameTemplate, validate );
        }

        public bool OwnerPluginLoaded
        {
            get { return _ownerPluginLoaded; }
        }

        internal void SetOwnerPluginUnloaded()
        {
            _ownerPluginLoaded = false;
        }

        internal ResourceList ResourcesOfType
        {
            get { return _resourcesOfType; }
            set { _resourcesOfType = value; }
        }
    }

    /**
     * A collection of registered resource types.
     */

    internal class ResourceTypeCollection: IResourceTypeCollection
    {
        private MyPalStorage _storage;
        private ITable       _resourceTypeTable;
        private ArrayList _resourceTypeCache     = new ArrayList();
        private Hashtable _resourceTypeNameCache = CollectionsUtil.CreateCaseInsensitiveHashtable();

        internal struct ResourceTypeFlagMapping
        {
            public ResourceTypeFlags Flag;
            public string Prop;
            public object Value;

            public ResourceTypeFlagMapping( ResourceTypeFlags flag, string prop, object value )
            {
                Flag = flag;
                Prop = prop;
                Value = value;
            }
        }

        internal ResourceTypeFlagMapping[] _flagMap = new ResourceTypeFlagMapping[]
            {   new ResourceTypeFlagMapping( ResourceTypeFlags.Internal, "Internal", 1 ),
                new ResourceTypeFlagMapping( ResourceTypeFlags.ResourceContainer, "ResourceContainer", 1 ),
                new ResourceTypeFlagMapping( ResourceTypeFlags.NoIndex, "NoIndex", 1 ),
                new ResourceTypeFlagMapping( ResourceTypeFlags.CanBeUnread, "CanBeUnread", true ),
                new ResourceTypeFlagMapping( ResourceTypeFlags.FileFormat, "FileFormat", 1 ) };

        internal ResourceTypeCollection( MyPalStorage storage, ITable resourceTypeTable )
        {
            _storage = storage;
            _resourceTypeTable = resourceTypeTable;
        }

        public IEnumerator GetEnumerator()
        {
            return new SkipNullEnumerator( _resourceTypeCache );
        }

        public IResourceType this [int ID]
        {
            get
            {
                if ( ID < 0 || ID >= _resourceTypeCache.Count )
                    throw new StorageException( "Invalid resource type ID " + ID + ": cache count " + _resourceTypeCache.Count );

                ResourceTypeItem item = (ResourceTypeItem) _resourceTypeCache [ID];
                if (item == null)
                {
                    throw new StorageException( "Invalid resource type ID " + ID + ": null element in cache" );
                }
                return item;
            }
        }

        internal IResourceType GetItemSafe( int id )
        {
            if ( id < 0 || id >= _resourceTypeCache.Count )
            {
                MyPalStorage.Storage.OnIndexCorruptionDetected( "Invalid resource type ID " + id + ": cache count " + _resourceTypeCache.Count );
                return null;
            }

            ResourceTypeItem item = (ResourceTypeItem) _resourceTypeCache [id];
            if (item == null)
            {
                MyPalStorage.Storage.OnIndexCorruptionDetected( "Invalid resource type ID " + id + ": cache count " + _resourceTypeCache.Count );
            }
            return item;
        }

        public IResourceType this [string name]
        {
            get
            {
                ResourceTypeItem item = (ResourceTypeItem) _resourceTypeNameCache [name];
                if ( item == null )
                    throw new StorageException( "Invalid resource type name " + name );
                return item;
            }
        }

        public int Count
        {
            get { return _resourceTypeNameCache.Count; }
        }

        /**
         * Registers a new resource type with a default display name, or returns the ID of the
         * existing type if it has already been registered.
         */

        public int Register( string name, string resourceDisplayNameTemplate )
        {
            return Register( name, name, resourceDisplayNameTemplate, ResourceTypeFlags.Normal, null );
        }

        public int Register( string name, string resourceDisplayNameTemplate, ResourceTypeFlags flags )
        {
            return Register( name, name, resourceDisplayNameTemplate, flags, null );
        }

        /**
         * Registers a new resource type, or returns the ID of the existing type if
         * it has already been registered.
         */

        public int Register( string name, string displayName, string resourceDisplayNameTemplate )
        {
            return Register( name, displayName, resourceDisplayNameTemplate, ResourceTypeFlags.Normal, null );
        }

        /**
         * Registers a new resource type, or returns the ID of the existing type if
         * it has already been registered.
         */

        public int Register( string name, string displayName, string resourceDisplayNameTemplate,
            ResourceTypeFlags flags )
        {
            return Register( name, displayName, resourceDisplayNameTemplate, flags, null );
        }

        public int Register( string name, string displayName, string resourceDisplayNameTemplate,
            ResourceTypeFlags flags, IPlugin ownerPlugin )
        {
            if ( resourceDisplayNameTemplate == null )
            {
                resourceDisplayNameTemplate = "";
            }
            // creating the mask instance validates it, and we don't want to get exceptions
            // after some of the data has been created
            new DisplayNameMask( resourceDisplayNameTemplate, true );

            bool newType = false;
            int ID = RegisterResourceTypeInternal( name, resourceDisplayNameTemplate, flags, false, out newType );

            CreateOrUpdateResourceTypeResource( name, displayName, resourceDisplayNameTemplate, flags, ownerPlugin, ID, newType );
            return ID;
        }

        internal void CreateOrUpdateResourceTypeResource( string name, string displayName,
            string resourceDisplayNameTemplate, ResourceTypeFlags flags, IPlugin ownerPlugin,
            int ID, bool newType )
        {
            ResourceTypeFlags oldFlags = ResourceTypeFlags.Normal;

            IResource res;
            if ( newType )
            {
                try
                {
                    res = CreateResourceTypeResource( ID, name, resourceDisplayNameTemplate );
                }
                catch( ResourceRestrictionException ex )
                {
                    MyPalStorage.Storage.OnIndexCorruptionDetected( "ResourceRestrictionException when creating PropType resource: " +
                        ex.Message );
                    return;
                }
            }
            else
            {
                res = _storage.FindUniqueResource( "ResourceType", _storage.Props.Name, name );
                oldFlags = this [name].Flags;
            }

            if ( res == null )
            {
                MyPalStorage.Storage.OnIndexCorruptionDetected( "Could not find ResourceType resource with name " + name );
            }
            else
            {
                SetResourceTypeFlags( res, flags );
                res.SetProp( _storage.Props.PropDisplayName, displayName );

                _storage.SetOwnerPlugin( res, ownerPlugin );
            }

            UpdateResourceTypeCache( ID, resourceDisplayNameTemplate, flags | oldFlags );

            if ( newType )
            {
                _storage.CacheResourceTypePredicate( (ResourceTypeItem) _resourceTypeCache [ID] );
            }
        }

        /// <summary>
        /// Creates a resource entry for the specified resource type.
        /// </summary>
        private IResource CreateResourceTypeResource( int ID, string name, string displayNameMask )
        {
            IResource res = _storage.BeginNewResource( "ResourceType" );
            res.SetProp( _storage.Props.TypeId, ID );
            res.SetProp( _storage.Props.Name, name );
            res.SetProp( _storage.Props.DisplayNameMask, displayNameMask );
            res.EndUpdate();
            return res;
        }

        /**
         * Checks if all resource types in the specified list are registered.
         */

        public bool Exist( params string[] resourceTypeNames )
        {
            foreach( string name in resourceTypeNames )
            {
                if ( _resourceTypeNameCache [name] == null )
                    return false;
            }
            return true;
        }

        /**
         * Loads the resource types to the cache hash table.
         */

        internal void CacheResourceTypes()
        {
            int count = 0;
            IResultSet rs = _resourceTypeTable.CreateResultSet( 0 );
            try
            {
                foreach( IRecord rec in rs )
                {
                    int ID = rec.GetIntValue( 0 );
                    string name = rec.GetStringValue( 1 );

                    AddResourceTypeToCache( ID, name, "", ResourceTypeFlags.Normal );
                    count++;
                }
            }
            finally
            {
                rs.Dispose();
            }
            Debug.WriteLine( "Loaded " + count + " resource types to cache" );
        }

        /// <summary>
        /// Adds a new ResourceTypeItem to the resource type cache.
        /// </summary>
        private void AddResourceTypeToCache( int ID, string name, string displayNameTemplate,
            ResourceTypeFlags flags )
        {
            if ( ID < 0 || ID > 65536 )
                throw new BadIndexesException( "Invalid resource type ID " + ID );

            lock( _resourceTypeCache )
            {
                ResourceTypeItem item = new ResourceTypeItem( ID, name, displayNameTemplate, flags );
                while( _resourceTypeCache.Count < ID )
                {
                    _resourceTypeCache.Add( null );
                }

                if ( _resourceTypeCache.Count == ID )
                {
                    _resourceTypeCache.Add( item );
                }
                else
                {
                    _resourceTypeCache [ID] = item;
                }

                _resourceTypeNameCache [name] = item;
            }
        }

        /**
         * Loads the flags, display name masks and display names of resource types
         * to the cache hash table.
         */

        internal void CacheResourceTypeFlags()
        {
            foreach( ResourceTypeItem item in _resourceTypeCache )
            {
                if ( item != null )
                {
                    IResource res = _storage.FindUniqueResource( "ResourceType", "Name", item.Name );
                    if ( res != null )
                    {
                        item.SetFlags( GetFlagsFromResource( res ) );
                        item.SetDisplayName( res.GetStringProp( "PropDisplayName" ) );
                        item.SetDisplayNameTemplate( res.GetStringProp( "DisplayNameMask" ), false );
                    }
                }
            }
        }

        private ResourceTypeFlags GetFlagsFromResource( IResource res )
        {
            ResourceTypeFlags flags = ResourceTypeFlags.Normal;
            foreach( ResourceTypeFlagMapping flagMapping in _flagMap )
            {
                if ( flagMapping.Value.Equals( res.GetProp( flagMapping.Prop ) ) )
                {
                    flags |= flagMapping.Flag;
                }
            }
            return flags;
        }

        internal void SetResourceTypeFlags( IResource res, ResourceTypeFlags flags )
        {
            foreach( ResourceTypeFlagMapping flagMapping in _flagMap )
            {
                if ( ( flags & flagMapping.Flag ) != 0 )
                {
                    res.SetProp( flagMapping.Prop, flagMapping.Value );
                }
                else
                {
                    res.DeleteProp( flagMapping.Prop );
                }
            }
        }

        /**
         * Adds a record for the specified resource type to the DB.
         */

        internal int RegisterResourceTypeInternal( string name, string displayNameTemplate, ResourceTypeFlags flags,
            bool skipChecks, out bool newType )
        {
            _storage.CheckOwnerThread();
            IRecord rec = _resourceTypeTable.GetRecordByEqual( 1, name );
            if ( rec != null )
            {
                if ( !_resourceTypeNameCache.ContainsKey( name ) )
                {
                    throw new BadIndexesException( "Resource type " + name + " found in ResourceTypes table but missing in name cache" );
                }
                string oldDisplayNameTemplate = rec.GetStringValue( 2 );
                if ( !skipChecks && String.Compare( oldDisplayNameTemplate, displayNameTemplate, true, CultureInfo.InvariantCulture ) != 0 )
                {
                    if ( oldDisplayNameTemplate.Length == 0 )
                    {
                        rec.SetValue( 2, displayNameTemplate );
                        _storage.SafeCommitRecord( rec, "ResourceTypeCollection.RegisterResourceTypeInternal" );
                    }
                    else
                    {
                        throw new StorageException( "Inconsistent display name template for resource type " + name +
                            "\nOld: " + oldDisplayNameTemplate + " New: " + displayNameTemplate );
                    }
                }
                newType = false;
                return rec.GetIntValue( 0 );
            }

            int ID;
            lock( _resourceTypeTable )
            {
                IRecord resourceType = _resourceTypeTable.NewRecord();
                resourceType.SetValue( 1, name );
                resourceType.SetValue( 2, displayNameTemplate );
                _storage.SafeCommitRecord( resourceType, "ResourceTypeCollection.RegisterResourceTypeInternal" );
                ID = resourceType.GetID();
                if ( ID > 65536 )
                {
                    MyPalStorage.Storage.OnIndexCorruptionDetected( "Invalid next ID in property type table" );
                }
            }

            AddResourceTypeToCache( ID, name, displayNameTemplate, flags );

            newType = true;
            return ID;
        }

        /**
         * When a ResourceType resource is changed, updates the DB data for it.
         */

        internal void UpdateResourceTypeFromResource( Resource res )
        {
            UpdateResourceType( res.GetStringProp( "Name" ), res.GetStringProp( "DisplayNameMask" ),
                GetFlagsFromResource( res ), res.GetStringProp( "PropDisplayName" ) );
        }

        internal void UpdateResourceType( string name, string displayNameTemplate, ResourceTypeFlags flags,
            string propDisplayName )
        {
            ICountedResultSet rs = _resourceTypeTable.CreateModifiableResultSet( 1, name );

            try
            {
                if ( rs.Count > 0 )
                {
                    IRecord rec = rs [0];
                    if ( rec.GetStringValue( 2 ) != displayNameTemplate )
                    {
                        rec.SetValue( 2, displayNameTemplate );
                        _storage.SafeCommitRecord( rec, "ResourceTypeCollection.UpdateResourceType" );
                    }

                    int resID = rec.GetIntValue( 0 );
                    UpdateResourceTypeCache( resID, displayNameTemplate, flags );

                    ResourceTypeItem item = (ResourceTypeItem) this [resID];
                    item.SetDisplayName( propDisplayName );
                }
            }
            finally
            {
                rs.Dispose();
            }
        }

        internal void UpdateResourceTypeCache( int resTypeID, string displayNameMask, ResourceTypeFlags flags )
        {
            ResourceTypeItem item = (ResourceTypeItem) _resourceTypeCache [resTypeID];
            Debug.Assert( item != null );
            Debug.Assert( item.Id == resTypeID );
            item.SetDisplayNameTemplate( displayNameMask, true );
            item.SetFlags( flags );
        }

        public void Delete( string name )
        {
            ResourceTypeItem item = (ResourceTypeItem) this [name];
            IResourceList resList = _storage.GetAllResources( name );
            resList.DeleteAll();

            _resourceTypeCache [item.Id] = null;
            _resourceTypeNameCache.Remove( name );

            ICountedResultSet rs = _resourceTypeTable.CreateModifiableResultSet( 0, item.Id );
            try
            {
                _storage.SafeDeleteRecord( rs [0], "ResourceTypes.Delete" );
            }
            finally
            {
                rs.Dispose();
            }

            IResource resourceTypeRes = _storage.FindUniqueResource( "ResourceType", "Name", name );
            Debug.Assert( resourceTypeRes != null );
            resourceTypeRes.Delete();
        }
    }
}
