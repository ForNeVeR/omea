// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.PicoCore
{
	/// <summary>
	/// Summary description for MockResourceStore.
	/// </summary>
	public class MockResourceStore: IResourceStore
	{
        private ArrayList _allResources = new ArrayList();

	    public int GetPropId( string name )
	    {
	        return _propTypes.GetPropId( name );
	    }

	    public IResource NewResource( string type )
	    {
	        MockResource resource = new MockResource( this, type );
            _allResources.Add( resource );
	        return resource;
	    }

	    public IResource BeginNewResource( string type )
	    {
	        return NewResource( type );
	    }

	    public IResource NewResourceTransient( string type )
	    {
	        throw new NotImplementedException();
	    }

	    public IResource LoadResource( int id )
	    {
	        throw new NotImplementedException();
	    }

	    public IResource TryLoadResource( int id )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResources( string resType, int propId, object propValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResources( string resType, string propName, object propValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResources<T>(string resType, PropId<T> propId, T propValue)
	    {
	        throw new NotImplementedException();
	    }

	    public BusinessObjectList<T> FindResources<T, V>(ResourceTypeId<T> resType, PropId<V> propId, V propValue)
	        where T : BusinessObject
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesLive( string resType, int propId, object propValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesLive( string resType, string propName, object propValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesLive<T>(string resType, PropId<T> propName, T propValue)
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResources( SelectionType selectionType, string resType, int propId, object propValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResources( SelectionType selectionType, string resType, string propName, object propValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesInRange( string resType, int propId, object minValue, object maxValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesInRange( string resType, string propName, object minValue, object maxValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesInRangeLive( string resType, int propId, object minValue, object maxValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesInRangeLive( string resType, string propName, object minValue, object maxValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesInRange( SelectionType selectionType, string resType, int propId, object minValue, object maxValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesInRange( SelectionType selectionType, string resType, string propName, object minValue, object maxValue )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesWithProp( string resType, int propId )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesWithProp( string resType, string propName )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesWithProp<T>(string resType, PropId<T> propId)
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesWithPropLive( string resType, int propId )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesWithPropLive( string resType, string propName )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesWithPropLive<T>(string resType, PropId<T> propId)
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesWithProp( SelectionType selectionType, string resType, int propId )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList FindResourcesWithProp( SelectionType selectionType, string resType, string propName )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList GetAllResources( string resType )
	    {
	        throw new NotImplementedException();
	    }

	    public BusinessObjectList<T> GetAllResources<T>(ResourceTypeId<T> resType) where T : BusinessObject
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList GetAllResourcesLive( string resType )
	    {
	        return new AllResourcesList( _allResources, resType );
	    }

	    public IResourceList GetAllResources( string[] resTypes )
	    {
	        throw new NotImplementedException();
	    }

	    public IResourceList GetAllResourcesLive( string[] resTypes )
	    {
	        throw new NotImplementedException();
	    }

		public IResourceList ListFromIds( int[] resourceIds, bool live )
		{
			throw new NotImplementedException();
		}

		public IResourceList ListFromIds( ICollection resourceIds, bool live )
		{
			throw new NotImplementedException();
		}

	    public IResource FindUniqueResource( string resType, int propId, object propValue )
	    {
	        foreach( IResource res in _allResources )
	        {
	            if ( Object.Equals( propValue, res.GetProp( propId ) ) )
	            {
                    return res;
	            }
	        }
            return null;
	    }

	    public IResource FindUniqueResource( string resType, string propName, object propValue )
	    {
	        throw new NotImplementedException();
	    }

	    public bool IsOwnerThread()
	    {
	        throw new NotImplementedException();
	    }

	    public void RegisterLinkRestriction( string fromResourceType, int linkType, string toResourceType, int minCount, int maxCount )
	    {
	        throw new NotImplementedException();
	    }

	    public void RegisterLinkRestriction(string fromResourceType, PropId<IResource> linkType, string toResourceType,
	                                        int minCount, int maxCount)
	    {
	        throw new NotImplementedException();
	    }

	    public int GetMinLinkCountRestriction( string fromResourceType, int linkType )
	    {
	        throw new NotImplementedException();
	    }

	    public int GetMaxLinkCountRestriction( string fromResourceType, int linkType )
	    {
	        throw new NotImplementedException();
	    }

	    public string GetLinkResourceTypeRestriction( string fromResourceType, int linkType )
	    {
	        throw new NotImplementedException();
	    }

	    public void RegisterUniqueRestriction( string resourceType, int propId )
	    {
	        throw new NotImplementedException();
	    }

	    public void DeleteUniqueRestriction( string resourceType, int propId )
	    {
	        throw new NotImplementedException();
	    }

	    public void RegisterCustomRestriction( string resourceType, int propId, IResourceRestriction restriction )
	    {
	        throw new NotImplementedException();
	    }

	    public void DeleteCustomRestriction( string resourceType, int propId )
	    {
	        throw new NotImplementedException();
	    }

	    public void RegisterRestrictionOnDelete( string resourceType, IResourceRestriction restriction )
	    {
	        throw new NotImplementedException();
	    }

	    public void DeleteRestrictionOnDelete( string resourceType )
	    {
	        throw new NotImplementedException();
	    }

	    public void RegisterDisplayNameProvider( IDisplayNameProvider provider )
	    {
	        throw new NotImplementedException();
	    }

	    public IPropTypeCollection PropTypes
	    {
	        get { return _propTypes; }
	    }
	    public IResourceTypeCollection ResourceTypes
	    {
	        get { return _resTypes; }
	    }
	    public IResourceList EmptyResourceList
	    {
	        get { return new MockResourceList(); }
	    }
	    public event ResourcePropEventHandler ResourceSaved;
	    public event LinkEventHandler LinkAdded;
	    public event LinkEventHandler LinkDeleted;

        private MockPropTypeCollection _propTypes = new MockPropTypeCollection();
        private MockResourceTypeCollection _resTypes = new MockResourceTypeCollection();
	}

    internal class MockPropTypeCollection: IPropTypeCollection
    {
        private Hashtable _propTypes = new Hashtable();
        private Hashtable _idToPropType = new Hashtable();
        private Hashtable _propTypeToId = new Hashtable();

        public bool Exist( params string[] propNames )
        {
            if ( propNames.Length != 1 )
                throw new NotImplementedException();
            return _propTypes.ContainsKey( propNames [0] );
        }

        public int Register( string name, PropDataType dataType )
        {
            return Register( name, dataType, PropTypeFlags.Normal );
        }


        public PropId<T> Register<T>(string name, PropDataTypeGeneric<T> dataType)
        {
            int id = Register(name, dataType.Type);
            return new PropId<T>(id);
        }

        public int Register( string name, PropDataType dataType, PropTypeFlags flags )
        {
            _propTypes [name] = dataType;
            int id = _propTypes.Count;
            _idToPropType [id] = name;
            _propTypeToId [name] = id;
            return id;
        }

        public int Register( string name, PropDataType dataType, PropTypeFlags flags, IPlugin ownerPlugin )
        {
            throw new NotImplementedException();
        }

        public void RegisterDisplayName( int propId, string displayName )
        {
            throw new NotImplementedException();
        }

        public void RegisterDisplayName( int propId, string fromDisplayName, string toDisplayName )
        {
            throw new NotImplementedException();
        }

        public void RegisterDisplayName(PropId<IResource> propId, string fromDisplayName, string toDisplayName)
        {
            throw new NotImplementedException();
        }


        public PropId<T> Register<T>(string name, PropDataTypeGeneric<T> dataType, PropTypeFlags flags,
                                     IPlugin ownerPlugin)
        {
            throw new NotImplementedException();
        }

        public PropId<T> Register<T>(string name, PropDataTypeGeneric<T> dataType, PropTypeFlags flags)
        {
            throw new NotImplementedException();
        }

        public void RegisterDisplayName<T>(PropId<T> propId, string displayName)
        {
            throw new NotImplementedException();
        }

        public void Delete( int id )
        {
            throw new NotImplementedException();
        }

        public string GetPropDisplayName( int propId )
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public IPropType this[ int id ]
        {
            get { throw new NotImplementedException(); }
        }

        public IPropType this[ string name ]
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int GetPropId( string name )
        {
            return (int) _propTypeToId [name];
        }

        public string GetPropName( int id )
        {
            return (string) _idToPropType [id];
        }
    }

    internal class MockResourceTypeCollection: IResourceTypeCollection
    {
        private Hashtable _resTypes = new Hashtable();

        public bool Exist( params string[] resourceTypeNames )
        {
            if ( resourceTypeNames.Length != 1 )
                throw new NotImplementedException();
            return _resTypes.ContainsKey( resourceTypeNames [0] );
        }

        public int Register( string name, string resourceDisplayNameTemplate )
        {
            _resTypes [name] = resourceDisplayNameTemplate;
            return -1;
        }

        public int Register( string name, string resourceDisplayNameTemplate, ResourceTypeFlags flags )
        {
            throw new NotImplementedException();
        }

        public int Register( string name, string displayName, string resourceDisplayNameTemplate )
        {
            throw new NotImplementedException();
        }

        public int Register( string name, string displayName, string resourceDisplayNameTemplate, ResourceTypeFlags flags )
        {
            throw new NotImplementedException();
        }

        public int Register( string name, string displayName, string resourceDisplayNameTemplate, ResourceTypeFlags flags, IPlugin ownerPlugin )
        {
            throw new NotImplementedException();
        }

        public void Delete( string name )
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public IResourceType this[ int id ]
        {
            get { throw new NotImplementedException(); }
        }

        public IResourceType this[ string name ]
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    internal class MockResourceList: IResourceList
    {
        protected ArrayList _resources = new ArrayList();

        internal MockResourceList() { }

        internal MockResourceList( IResource res )
        {
            _resources.Add( res );
        }

        internal MockResourceList( IResourceList lhs, IResourceList rhs )
        {
            foreach( IResource res in lhs )
            {
                _resources.Add( res );
            }
            foreach( IResource res in rhs )
            {
                if ( !_resources.Contains( res ) )
                {
                    _resources.Add( res );
                }
            }
        }

        public void Dispose( bool disposeBaseLists )
        {
            throw new NotImplementedException();
        }

        public int IndexOf( IResource res )
        {
            throw new NotImplementedException();
        }

        public int IndexOf( int resId )
        {
            throw new NotImplementedException();
        }

        public bool Contains( IResource res )
        {
            throw new NotImplementedException();
        }

        public IResourceList Union( IResourceList other )
        {
            return new MockResourceList( this, other );
        }

        public IResourceList Union( IResourceList other, bool allowMerge )
        {
            throw new NotImplementedException();
        }

        public IResourceList Intersect( IResourceList other )
        {
            throw new NotImplementedException();
        }

        public IResourceList Intersect( IResourceList other, bool allowMerge )
        {
            throw new NotImplementedException();
        }

        public IResourceList Minus( IResourceList other )
        {
            throw new NotImplementedException();
        }

        public bool HasProp( string propName )
        {
            throw new NotImplementedException();
        }

        public bool HasProp( int propId )
        {
            throw new NotImplementedException();
        }

        public bool AllResourcesOfType( string type )
        {
            throw new NotImplementedException();
        }

        public string[] GetAllTypes()
        {
            throw new NotImplementedException();
        }

        public void AttachPropertyProvider( IPropertyProvider provider )
        {
            throw new NotImplementedException();
        }

        public string GetPropText( int index, string propName )
        {
            throw new NotImplementedException();
        }

        public string GetPropText( int index, int propId )
        {
            throw new NotImplementedException();
        }

        public bool HasProp( int index, string propName )
        {
            throw new NotImplementedException();
        }

        public bool HasProp( int index, int propId )
        {
            throw new NotImplementedException();
        }

        public void Sort( string propNames )
        {
            throw new NotImplementedException();
        }

        public void Sort( string propNames, bool ascending )
        {
            throw new NotImplementedException();
        }

        public void Sort( int[] propIds, bool ascending )
        {
            throw new NotImplementedException();
        }

        public void Sort( int[] propIds, bool ascending, bool propsEquivalent )
        {
            throw new NotImplementedException();
        }

        public void Sort( int[] propIds, bool[] sortDirections )
        {
            throw new NotImplementedException();
        }

        public void Sort( IResourceComparer customComparer, bool ascending )
        {
            throw new NotImplementedException();
        }

        public void DeleteAll()
        {
            throw new NotImplementedException();
        }

        public void AddPropertyWatch( int propId )
        {
            throw new NotImplementedException();
        }

        public void AddPropertyWatch( int[] propIds )
        {
            throw new NotImplementedException();
        }

        public void Deinstantiate()
        {
            throw new NotImplementedException();
        }

        public IResource Find(Predicate<IResource> predicate)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _resources.Count; }
        }
        public IResourceIdCollection ResourceIds
        {
            get { throw new NotImplementedException(); }
        }
        public string SortProps
        {
            get { throw new NotImplementedException(); }
        }
        public int[] SortPropIDs
        {
            get { throw new NotImplementedException(); }
        }
        public int[] SortDirections
        {
            get { throw new NotImplementedException(); }
        }
        public bool SortAscending
        {
            get { throw new NotImplementedException(); }
        }

        public IResource this[ int index ]
        {
            get { return (IResource) _resources [index]; }
        }

        public IEnumerable ValidResources
        {
            get { throw new NotImplementedException(); }
        }

        public SortSettings SortSettings
        {
            get { throw new NotImplementedException(); }
        }

        public void Sort( SortSettings sortSettings )
        {
            throw new NotImplementedException();
        }

        public event ResourceIndexEventHandler ResourceAdded;
        public event ResourceIndexEventHandler ResourceDeleting;
        public event ResourcePropIndexEventHandler ResourceChanged;
        public event ResourcePropIndexEventHandler ChangedResourceDeleting;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            return _resources.GetEnumerator();
        }

        public object GetPropValue( IResource res, int propId )
        {
            throw new NotImplementedException();
        }

        public string GetPropText( IResource res, int propId )
        {
            throw new NotImplementedException();
        }

        public bool HasProp( IResource res, int propId )
        {
            throw new NotImplementedException();
        }
    }

    internal class AllResourcesList : MockResourceList
    {
        public AllResourcesList( ArrayList resources, string type )
        {
            foreach( IResource res in resources )
            {
                if ( res.Type == type )
                {
                    _resources.Add( res );
                }
            }
        }
    }

    public class MockResource : IResource
    {
        private MockResourceStore _store;
        private Hashtable _properties = new Hashtable();
        private string _type;

        public MockResource( MockResourceStore store, string type )
        {
            _store = store;
            _type = type;
        }

        public void Lock()
        {
        }

        public bool TryLock()
        {
            return true;
        }

        public void UnLock()
        {
        }

        public void ClearProperties()
        {
        }

        public void SetProp( string propName, object propValue )
        {
            _properties [_store.GetPropId( propName )] = propValue;
        }

        public void SetProp( int propId, object propValue )
        {
            _properties [propId] = propValue;
        }

        public void SetProp<T>(PropId<T> propId, T value)
        {
            throw new NotImplementedException();
        }

        public void SetReverseLinkProp(PropId<IResource> propId, IResource propValue)
        {
            throw new NotImplementedException();
        }

        public void DeleteProp( string propName )
        {
            throw new NotImplementedException();
        }

        public void DeleteProp( int propId )
        {
            throw new NotImplementedException();
        }

        public void AddLink( string propName, IResource target )
        {
            AddLink( _store.GetPropId( propName ), target );
        }

        public void AddLink( int propId, IResource target )
        {
            AddLinkSide( propId, this, (MockResource) target );
            AddLinkSide( propId, (MockResource) target, this );
        }

        public void AddLink(PropId<IResource> propId, IResource target)
        {
            AddLink(propId.Id, target);
        }

        private static void AddLinkSide( int propId, MockResource source, MockResource target )
        {
            ArrayList linkList = (ArrayList) source._properties [propId];
            if ( linkList == null )
            {
                linkList = new ArrayList();
                source._properties [propId] = linkList;
            }
            if (!linkList.Contains( target ) )
            {
                linkList.Add( target );
            }
        }

        public void DeleteLink( string propName, IResource target )
        {
            throw new NotImplementedException();
        }

        public void DeleteLink( int propId, IResource target )
        {
            throw new NotImplementedException();
        }

        public void DeleteLinks( string propName )
        {
            throw new NotImplementedException();
        }

        public void DeleteLinks( int propId )
        {
            throw new NotImplementedException();
        }

        public object GetProp( int propId )
        {
            return _properties [propId];
        }

        public object GetProp( string propName )
        {
            return _properties [_store.GetPropId( propName )];
        }

        public T GetProp<T>(PropId<T> propId)
        {
            return (T) GetProp(propId.Id);
        }

        public string GetStringProp( int propId )
        {
            throw new NotImplementedException();
        }

        public string GetStringProp( string propName )
        {
            return (string) _properties [_store.GetPropId( propName )];
        }

        public int GetIntProp( int propId )
        {
            throw new NotImplementedException();
        }

        public int GetIntProp( string propName )
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateProp( int propId )
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateProp( string propName )
        {
            throw new NotImplementedException();
        }

        public double GetDoubleProp( int propId )
        {
            throw new NotImplementedException();
        }

        public double GetDoubleProp( string propName )
        {
            throw new NotImplementedException();
        }

        public Stream GetBlobProp( int propId )
        {
            throw new NotImplementedException();
        }

        public Stream GetBlobProp( string propName )
        {
            throw new NotImplementedException();
        }

        public IStringList GetStringListProp( int propId )
        {
            throw new NotImplementedException();
        }

        public IStringList GetStringListProp( string propName )
        {
            throw new NotImplementedException();
        }

        public IResource GetLinkProp( string propName )
        {
            throw new NotImplementedException();
        }

        public IResource GetLinkProp( int propId )
        {
            throw new NotImplementedException();
        }

        public IResource GetReverseLinkProp(PropId<IResource> propId)
        {
            throw new NotImplementedException();
        }

        public string GetPropText( string propName )
        {
            throw new NotImplementedException();
        }

        public string GetPropText( int propId )
        {
            throw new NotImplementedException();
        }

        public int GetLinkCount( string propName )
        {
            throw new NotImplementedException();
        }

        public int GetLinkCount( int propId )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksOfType( string resType, string propName )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksOfType( string resType, int propId )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksOfType(string resType, PropId<IResource> propId)
        {
            throw new NotImplementedException();
        }

        public BusinessObjectList<T> GetLinksOfType<T>(ResourceTypeId<T> resType, PropId<IResource> propId) where T : BusinessObject
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksOfTypeLive( string resType, string propName )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksOfTypeLive( string resType, int propId )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksOfTypeLive(string resType, PropId<IResource> propId)
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksFrom( string resType, string propName )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksFrom( string resType, int propId )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksFrom(string resType, PropId<IResource> propId)
        {
            throw new NotImplementedException();
        }

        public BusinessObjectList<T> GetLinksFrom<T>(ResourceTypeId<T> resType, PropId<IResource> propId)
            where T : BusinessObject
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksFromLive( string resType, string propName )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksFromLive( string resType, int propId )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksTo( string resType, string propName )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksTo( string resType, int propId )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksTo(string resType, PropId<IResource> propId)
        {
            throw new NotImplementedException();
        }

        public BusinessObjectList<T> GetLinksTo<T>(ResourceTypeId<T> resType, PropId<IResource> propId)
            where T : BusinessObject
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksToLive( string resType, string propName )
        {
            throw new NotImplementedException();
        }

        public IResourceList GetLinksToLive( string resType, int propId )
        {
            throw new NotImplementedException();
        }

        public int[] GetLinkTypeIds()
        {
            throw new NotImplementedException();
        }

        public bool HasProp( string propName )
        {
            throw new NotImplementedException();
        }

        public bool HasProp( int propId )
        {
            throw new NotImplementedException();
        }

        public bool HasProp<T>(PropId<T> propId)
        {
            throw new NotImplementedException();
        }

        public bool HasLink( string propName, IResource target )
        {
            throw new NotImplementedException();
        }

        public bool HasLink( int propId, IResource target )
        {
            throw new NotImplementedException();
        }

        public void ChangeType( string newType )
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public void BeginUpdate()
        {
            throw new NotImplementedException();
        }

        public void EndUpdate()
        {
        }

        public bool IsChanged()
        {
            throw new NotImplementedException();
        }

        public IResourceList ToResourceList()
        {
            return new MockResourceList( this );
        }

        public IResourceList ToResourceListLive()
        {
            throw new NotImplementedException();
        }

        public int Id
        {
            get { throw new NotImplementedException(); }
        }
        public int OriginalId
        {
            get { throw new NotImplementedException(); }
        }

        public string Type
        {
            get { return _type;}
        }

        public int TypeId
        {
            get { throw new NotImplementedException(); }
        }
        public string DisplayName
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        public IPropertyCollection Properties
        {
            get { throw new NotImplementedException(); }
        }
        public bool IsDeleting
        {
            get { throw new NotImplementedException(); }
        }
        public bool IsDeleted
        {
            get { throw new NotImplementedException(); }
        }
        public bool IsTransient
        {
            get { throw new NotImplementedException(); }
        }
    }
}
