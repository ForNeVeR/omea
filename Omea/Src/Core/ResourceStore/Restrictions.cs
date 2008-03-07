/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.DataStructures;

namespace JetBrains.Omea.ResourceStore
{
    internal abstract class ResourceRestriction
    {
        protected string _resourceType;
        protected int _propId;

        protected ResourceRestriction() {}
        
        protected ResourceRestriction( string resourceType, int propId )
        {
            _resourceType = resourceType;
            _propId = propId;
        }
        
        public abstract void CheckResource( IResource res, IPropertyChangeSet cs );
        public abstract void DeleteFromResourceStore();

        public string ResourceType
        {
            get { return _resourceType; }
        }

        public int PropId
        {
            get { return _propId; }
        }
    }
    
    /** 
     * represents single link restriction
     */
    internal class LinkRestriction: ResourceRestriction
    {
        public LinkRestriction( string fromResourceType,
                                string toResourceType,
                                int linkType,
                                int minCount,
                                int maxCount )
            : base( fromResourceType, linkType )
        {
            _toResourceType = toResourceType;
            _minCount = minCount;
            _maxCount = maxCount;
        }

        public LinkRestriction( IResource lr )
        {
            if( lr.Type != RestrictionResourceType )
                throw new StorageException(
                    "Attempt to load link restriction from an inappropriate resource");

            _resourceType = lr.GetStringProp( ResourceRestrictions.propFromResourceType );
            _toResourceType = lr.GetStringProp( ResourceRestrictions.propToResourceType );
            _propId = lr.GetIntProp( ResourceRestrictions.propLinkType );
            _minCount = lr.GetIntProp( ResourceRestrictions.propMinCount );
            _maxCount = lr.GetIntProp( ResourceRestrictions.propMaxCount );
        }

        public void SaveToResourceStore()
        {
            IResource lr = MyPalStorage.Storage.BeginNewResource( RestrictionResourceType );
            try
            {
                lr.SetProp( ResourceRestrictions.propFromResourceType, _resourceType );
                if( _toResourceType != null )
                    lr.SetProp( ResourceRestrictions.propToResourceType, _toResourceType );
                lr.SetProp( ResourceRestrictions.propLinkType, _propId );
                lr.SetProp( ResourceRestrictions.propMinCount, _minCount );
                lr.SetProp( ResourceRestrictions.propMaxCount, _maxCount );
            }
            finally
            {
                lr.EndUpdate();
            }
        }

        public override void DeleteFromResourceStore()
        {
            IResourceList restList = MyPalStorage.Storage.FindResources( RestrictionResourceType, 
                ResourceRestrictions.propLinkType, _propId );
            foreach( IResource rest in restList )
            {
                if ( rest.GetStringProp( ResourceRestrictions.propFromResourceType ) == _resourceType )
                {
                    rest.Delete();
                }
            }
        }

        #region System.Object overrides

        public override int GetHashCode()
        {
            return _resourceType.GetHashCode() + _propId;
        }

        public override bool Equals( object obj )
        {
            if( !( obj is LinkRestriction ) )
                return false;
            LinkRestriction lr = (LinkRestriction) obj;
            return _resourceType == lr._resourceType && _propId == lr._propId;
        }

        #endregion

        /** 
         * main checking predicate
         * returns true if a resource corresponds to the restriction
         */
        public override void CheckResource( IResource res, IPropertyChangeSet cs )
        {
            if( res.Type != _resourceType )
                return;

            bool hasAdds = false, hasDeletes = false;
            LinkChange[] changes = cs.GetLinkChanges( _propId );
            for( int i=0; i<changes.Length; i++ )
            {
                if ( changes [i].ChangeType == LinkChangeType.Add )
                {
                    hasAdds = true;
                    if ( _toResourceType != null )
                    {
                        IResource target = MyPalStorage.Storage.TryLoadResource( changes [i].TargetId );
                        if ( target != null && target.Type != _toResourceType )
                        {
                            throw new ResourceRestrictionException( "Resource of type " + res.Type +
                                " doesn't correspond to link resource type restriction on property "  +
                                MyPalStorage.Storage.GetPropName( _propId ) + 
                                ": required links to " + _toResourceType + ", found link to " + target.Type );
                        }
                    }
                }
                else if ( changes [i].ChangeType == LinkChangeType.Delete )
                {
                    hasDeletes = true;
                }
            }
            
            int linkCount = res.GetLinkCount( _propId );
             
            /** 
             * at first check counts
             */
            if( ( hasDeletes || changes.Length == 0 ) && linkCount < _minCount )
            {
                throw new ResourceRestrictionException( "Resource of type " + res.Type + 
                    " doesn't correspond to minimum link count restriction on property "  +
                    MyPalStorage.Storage.GetPropName( _propId ) );
            }
            if( hasAdds && _maxCount >= 0 && linkCount > _maxCount )
            {
                throw new ResourceRestrictionException( "Resource of type " + res.Type + 
                    " doesn't correspond to maximum link count restriction on property "  +
                    MyPalStorage.Storage.GetPropName( _propId ) );
            }
        }

        public static string RestrictionResourceType
        {
            get { return "LinkRestriction"; }
        }

        public string ToResourceType
        {
            get { return _toResourceType; }
        }

        public int MinCount
        {
            get { return _minCount; }
        }

        public int MaxCount
        {
            get { return _maxCount; }
        }

        private string      _toResourceType;
        private int         _minCount;
        private int         _maxCount;
    }

    internal class UniqueRestriction: ResourceRestriction
    {
        public UniqueRestriction( string resourceType, int propId )
            : base( resourceType, propId )
        {
        }

        public UniqueRestriction( IResource res )
        {
            _resourceType = res.GetStringProp( ResourceRestrictions.propFromResourceType );
            _propId = res.GetIntProp( ResourceRestrictions.propUniquePropId );
        }

        public void SaveToResourceStore()
        {
            IResource lr = MyPalStorage.Storage.BeginNewResource( RestrictionResourceType );
            try
            {
                lr.SetProp( ResourceRestrictions.propFromResourceType, _resourceType );
                lr.SetProp( ResourceRestrictions.propUniquePropId, _propId );
            }
            finally
            {
                lr.EndUpdate();
            }
        }

        public override void DeleteFromResourceStore()
        {
            IResourceList restList = MyPalStorage.Storage.FindResources( RestrictionResourceType, 
                ResourceRestrictions.propUniquePropId, _propId );
            foreach( IResource rest in restList )
            {
                if ( rest.GetStringProp( ResourceRestrictions.propFromResourceType ) == _resourceType )
                {
                    rest.Delete();
                }
            }
        }

        public override void CheckResource( IResource res, IPropertyChangeSet cs )
        {
            object propValue = res.GetProp( _propId );
            if ( propValue != null )
            {
                IResourceList resList = MyPalStorage.Storage.FindResources( res.Type, _propId, propValue );
                if ( resList.Count > 1 )
                {
                    int dupId = -1;
                    for( int i=0; i<resList.Count; i++ )
                    {
                        if ( resList.ResourceIds [i] != res.Id )
                        {
                            dupId = resList.ResourceIds [i];
                        }
                    }
                    MyPalStorage.Storage.SetRepairRequired();
                    if ( dupId != -1 )
                    {
                        // it's an actual data consistency problem, not a problem with
                        // the indexes
                        throw new ResourceRestrictionException( "Resource of type " + res.Type + 
                            ", ID=" + res.Id + " doesn't correspond to unique restriction on property "  +
                            MyPalStorage.Storage.GetPropName( _propId ) + 
                            ": same value <" + propValue + "> as resource ID=" + dupId );
                    }
                }
            }
        }

        public static string RestrictionResourceType
        {
            get { return "UniqueRestriction"; }
        }

        public override bool Equals( object obj )
        {
            UniqueRestriction rhs = obj as UniqueRestriction;
            if ( rhs == null )
                return false;

            return _propId == rhs._propId && _resourceType == rhs._resourceType;
        }

        public override int GetHashCode()
        {
            return _propId ^ _resourceType.GetHashCode();
        }
    }

    internal class CustomRestriction: ResourceRestriction
    {
        private IResourceRestriction _restriction;
        private string _restrictionClass;

        public CustomRestriction( string resourceType, int propId, IResourceRestriction restriction )
            : base( resourceType, propId )
        {
            _restriction = restriction;
            _restrictionClass = _restriction.GetType().FullName;
        }

        public CustomRestriction( IResource res )
        {
            _resourceType = res.GetStringProp( ResourceRestrictions.propFromResourceType );
            _propId = res.GetIntProp( ResourceRestrictions.propUniquePropId );
            _restrictionClass = res.GetStringProp( ResourceRestrictions.propCustomRestrictionClass );
        }

        public override void CheckResource( IResource res, IPropertyChangeSet cs )
        {
            if ( _restriction == null )
                throw new ResourceRestrictionException( "Custom resource restriction implementation not registered" );
            _restriction.CheckResource( res );
        }

        public static string RestrictionResourceType
        {
            get { return "CustomRestriction"; }
        }

        public string RestrictionClass
        {
            get { return _restrictionClass; }
        }

        internal void SetImplementation( IResourceRestriction restriction )
        {
            _restriction = restriction;
        }

        internal void SaveToResourceStore()
        {
            IResource lr = MyPalStorage.Storage.BeginNewResource( RestrictionResourceType );
            try
            {
                lr.SetProp( ResourceRestrictions.propFromResourceType, _resourceType );
                lr.SetProp( ResourceRestrictions.propUniquePropId, _propId );
                lr.SetProp( ResourceRestrictions.propCustomRestrictionClass, _restrictionClass );
            }
            finally
            {
                lr.EndUpdate();
            }
        }

        public override void DeleteFromResourceStore()
        {
            IResourceList restList = MyPalStorage.Storage.FindResources( RestrictionResourceType, 
                ResourceRestrictions.propUniquePropId, _propId );
            foreach( IResource rest in restList )
            {
                if ( rest.GetStringProp( ResourceRestrictions.propFromResourceType ) == _resourceType )
                {
                    rest.Delete();
                }
            }
        }
    }

    /** 
     * whole space of link restrictions
     */
    public sealed class ResourceRestrictions
    {
        public static void RegisterTypes()
        {
            _store = MyPalStorage.Storage;
            _store.ResourceTypes.Register( LinkRestriction.RestrictionResourceType, String.Empty,
                                         string.Empty, ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            _store.ResourceTypes.Register( UniqueRestriction.RestrictionResourceType, String.Empty,
                string.Empty, ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            _store.ResourceTypes.Register( CustomRestriction.RestrictionResourceType, String.Empty,
                string.Empty, ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );

            _propFromResourceType = _store.PropTypes.Register( "fromResourceType", PropDataType.String, PropTypeFlags.Internal );
            _propToResourceType = _store.PropTypes.Register( "toResourceType", PropDataType.String, PropTypeFlags.Internal );
            _propLinkType = _store.PropTypes.Register( "LinkType", PropDataType.Int, PropTypeFlags.Internal );
            _propUniquePropId = _store.PropTypes.Register( "UniquePropId", PropDataType.Int, PropTypeFlags.Internal );
            _propMinCount = _store.PropTypes.Register( "MinCount", PropDataType.Int, PropTypeFlags.Internal );
            _propMaxCount = _store.PropTypes.Register( "MaxCount", PropDataType.Int, PropTypeFlags.Internal );
            propCustomRestrictionClass = _store.PropTypes.Register( "CustomRestrictionClass", PropDataType.String, 
                PropTypeFlags.Internal );

            _restrictions = new HashMap();
            _customRestrictions = new HashMap();
            
            foreach( IResource res in _store.GetAllResources( LinkRestriction.RestrictionResourceType ) )
            {
                LinkRestriction lr = new LinkRestriction( res );
                if ( lr.ResourceType != null )
                {
                    AddResourceRestriction( lr );
                }
            }

            foreach( IResource res in _store.GetAllResources( UniqueRestriction.RestrictionResourceType ) )
            {
                UniqueRestriction restriction = new UniqueRestriction( res );
                if ( restriction.ResourceType != null )
                {
                    AddResourceRestriction( restriction );
                }
            }

            foreach( IResource res in _store.GetAllResources( CustomRestriction.RestrictionResourceType ) )
            {
                CustomRestriction restriction = new CustomRestriction( res );
                if ( restriction.ResourceType != null )
                {
                    _customRestrictions [restriction.RestrictionClass] = restriction;
                    AddResourceRestriction( restriction );
                }
            }

            _active = true;
        }

        /** 
         * empty default ctor is necessary to be sure that static ctor is executed
         */
        private ResourceRestrictions() {}

        internal static int propFromResourceType
        {
            get { return _propFromResourceType; }
        }
        internal static int propToResourceType
        {
            get { return _propToResourceType; }
        }
        internal static int propLinkType
        {
            get { return _propLinkType; }
        }
        internal static int propMinCount
        {
            get { return _propMinCount; }
        }
        internal static int propMaxCount
        {
            get { return _propMaxCount; }
        }

        internal static int propUniquePropId
        {
            get { return _propUniquePropId; }
        }

        internal static void RegisterLinkRestriction( string fromResourceType,
                                                      int linkType,
                                                      string toResourceType,
                                                      int minCount,
                                                      int maxCount )
        {
            if( !_active )
                return;

            if ( fromResourceType == null )
            {
                throw new ArgumentNullException( "fromResourceType" );
            }
            if ( !MyPalStorage.Storage.ResourceTypes.Exist( fromResourceType ) )
            {
                throw new ArgumentException( "Invalid resource type " + fromResourceType, fromResourceType );
            }
            if ( MyPalStorage.Storage.GetPropDataType( linkType ) != PropDataType.Link )
            {
                throw new Exception( "Link restrictions may only be registered for link properties" );
            }

            LinkRestriction lr = new LinkRestriction(
                fromResourceType, toResourceType, linkType, minCount, maxCount );
            if ( AddResourceRestriction( lr ) )
            {
                lr.SaveToResourceStore();
            }
        }

        internal static void RegisterCustomRestriction( string resourceType, int propId, 
            IResourceRestriction restriction )
        {
            if ( resourceType == null )
            {
                throw new ArgumentNullException( "resourceType" );
            }
            if ( !MyPalStorage.Storage.ResourceTypes.Exist( resourceType ) )
            {
                throw new ArgumentException( "Invalid resource type " + resourceType, resourceType );
            }

            string restrictionClass = restriction.GetType().FullName;
            CustomRestriction existingRestriction = (CustomRestriction) _customRestrictions [restrictionClass];
            if ( existingRestriction != null )
            {
                existingRestriction.SetImplementation( restriction );
            }
            else
            {
                CustomRestriction customRestriction = new CustomRestriction( resourceType, propId, restriction );
                AddResourceRestriction( customRestriction );
                customRestriction.SaveToResourceStore();
                _customRestrictions [restrictionClass] = customRestriction;
            }
        }

        internal static void DeleteCustomRestriction( string resourceType, int propId )
        {
            lock( _restrictions )
            {
                HashSet restrictionSet = (HashSet) _restrictions [resourceType];
                if ( restrictionSet != null )
                {
                    foreach( HashSet.Entry e in restrictionSet )
                    {
                        CustomRestriction restriction = e.Key as CustomRestriction;
                        if ( restriction != null && restriction.PropId == propId )
                        {
                            restrictionSet.Remove( restriction );
                            _customRestrictions.Remove( restriction.RestrictionClass );
                            restriction.DeleteFromResourceStore();
                        }
                    }
                }
            }
        }

        internal static void RegisterRestrictionOnDelete( string resourceType, IResourceRestriction restriction )
        {
            RegisterCustomRestriction( resourceType, ResourceProps.Id, restriction );
        }

        internal static void DeleteRestrictionOnDelete( string resourceType )
        {
            DeleteCustomRestriction( resourceType, ResourceProps.Id );
        }

        internal static void RegisterUniqueRestriction( string resourceType, int propId )
        {
            if ( resourceType == null )
            {
                throw new ArgumentNullException( "resourceType" );
            }
            if ( !MyPalStorage.Storage.ResourceTypes.Exist( resourceType ) )
            {
                throw new ArgumentException( "Invalid resource type " + resourceType, resourceType );
            }

            PropDataType dataType = MyPalStorage.Storage.PropTypes [propId].DataType;
            if  (dataType != PropDataType.Int && dataType != PropDataType.String && dataType != PropDataType.Date)
            {
                throw new StorageException( "Unique restrictions may only be registered for int, string or date properties" );
            }

            UniqueRestriction restriction = new UniqueRestriction( resourceType, propId );
            if ( AddResourceRestriction( restriction ) )
            {
                restriction.SaveToResourceStore();
            }
        }

        internal static void DeleteUniqueRestriction( string resourceType, int propId )
        {
            lock( _restrictions )
            {
                HashSet restrictionSet = (HashSet) _restrictions [resourceType];
                if ( restrictionSet != null )
                {
                    UniqueRestriction restriction = new UniqueRestriction( resourceType, propId );
                    restrictionSet.Remove( restriction );  // this will delete the restriction which is equal to the given one
                    restriction.DeleteFromResourceStore();
                }
            }
        }

        internal static bool UniqueRestrictionExists( string resourceType, int propId )
        {
            Guard.NullArgument( resourceType, "resourceType" );
            lock( _restrictions )
            {
                HashSet restrictionSet = (HashSet) _restrictions [resourceType];
                if ( restrictionSet != null )
                {
                    return restrictionSet.Contains( new UniqueRestriction( resourceType, propId ) );
                }
                return false;
            }
        }

        /// <summary>
        /// Deletes all restrictions on the specified property type.
        /// </summary>
        /// <param name="propId">The ID of the property type.</param>
        internal static void DeletePropRestrictions( int propId )
        {
            lock( _restrictions )
            {
                foreach( HashMap.Entry e in _restrictions )
                {
                    ArrayList restrictionsToDelete = new ArrayList();
                    HashSet restrictionsSet = (HashSet) e.Value;
                    foreach( HashSet.Entry restrictionEntry in restrictionsSet )
                    {
                        ResourceRestriction restriction = (ResourceRestriction) restrictionEntry.Key;
                        if ( restriction.PropId == propId )
                        {
                            restrictionsToDelete.Add( restriction );
                        }
                    }

                    foreach( ResourceRestriction restriction in restrictionsToDelete )
                    {
                        restrictionsSet.Remove( restriction );
                        restriction.DeleteFromResourceStore();
                    }
                }
            }
        }

        private static bool AddResourceRestriction( ResourceRestriction restriction )
        {
            lock( _restrictions )
            {
                HashMap.Entry E = _restrictions.GetEntry( restriction.ResourceType );
                HashSet restrictionsSet = ( E == null ) ?  new HashSet() : (HashSet) E.Value;
                if( !restrictionsSet.Contains( restriction ) )
                {
                    restrictionsSet.Add( restriction );
                    if( E == null )
                        _restrictions[ restriction.ResourceType ] = restrictionsSet;
                    return true;
                }
                return false;
            }
        }

        public static void CheckResource( IResource res, IPropertyChangeSet changeSet )
        {
            if( _active )
            {
                lock( _restrictions )
                {
                    HashSet restrictionsSet = (HashSet) _restrictions[ res.Type ];
                    if( restrictionsSet != null )
                    {
                        foreach( HashSet.Entry E in restrictionsSet )
                        {
                            ResourceRestriction restriction = (ResourceRestriction) E.Key;
                            if ( ( restriction.PropId != ResourceProps.Id && changeSet.IsNewResource ) ||  
                                 changeSet.IsPropertyChanged( restriction.PropId ) )
                            {
                                restriction.CheckResource( res, changeSet );
                            }
                        }
                    }
                }
            }
        }

        internal static void CheckResourceDelete( IResource res )
        {
            CheckResource( res, new SinglePropChangeSet( ResourceProps.Id, res.Id, false, false ) );
        }

        public static int GetMinLinkCountRestriction( string fromResourceType, int linkType )
        {
            LinkRestriction restriction = FindLinkRestriction( fromResourceType, linkType );
            if ( restriction != null )
            {
                return restriction.MinCount;
            }
            return 0;
        }

        public static int GetMaxLinkCountRestriction( string fromResourceType, int linkType )
        {
            LinkRestriction restriction = FindLinkRestriction( fromResourceType, linkType );
            if ( restriction != null )
            {
                return restriction.MaxCount;
            }
            return Int32.MaxValue;
        }

        public static string GetLinkResourceTypeRestriction( string fromResourceType, int linkType )
        {
            LinkRestriction restriction = FindLinkRestriction( fromResourceType, linkType );
            if ( restriction != null )
            {
                return restriction.ToResourceType;
            }
            return null;
        }

        private static LinkRestriction FindLinkRestriction( string fromResourceType, int linkType )
        {
            lock( _restrictions )
            {
                HashSet restrictionsSet = (HashSet) _restrictions[ fromResourceType ];
                if( restrictionsSet != null )
                {
                    foreach( HashSet.Entry E in restrictionsSet )
                    {
                        ResourceRestriction restriction = (ResourceRestriction) E.Key;
                        if ( restriction.PropId == linkType )
                        {
                            LinkRestriction lr = restriction as LinkRestriction;
                            if ( lr != null )
                            {
                                return lr;
                            }
                        }
                    }
                }
                return null;
            }
        }

        private static MyPalStorage     _store;
        private static int              _propFromResourceType;
        private static int              _propToResourceType;
        private static int              _propLinkType;
        private static int              _propMinCount;
        private static int              _propMaxCount;
        internal static int             propCustomRestrictionClass;
        private static HashMap          _restrictions;
        private static HashMap          _customRestrictions;
        private static bool             _active = false;
        private static int              _propUniquePropId;
    }
}