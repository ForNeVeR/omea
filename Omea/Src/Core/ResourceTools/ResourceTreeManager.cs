/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using JetBrains.Omea.OpenAPI;
using JetBrains.DataStructures;

namespace JetBrains.Omea.ResourceTools
{
	/// <summary>
	/// Utility class for working with root nodes of trees for various types of resources.
	/// </summary>
	public class ResourceTreeManager: IResourceTreeManager
	{
        private static IResource _rootOfRoots;

        private static string _resTreeRoot = "ResourceTreeRoot";
        private static int _propRootResType;

        private IResourceStore _store;
        private IntHashTable _sortPropCache = new IntHashTable();
        private HashSet _viewsExclusive = new HashSet();

        private class TreeListenerManager
        {
            private int _parentProp;
            private IResourceList _resources;
            private IntHashTable _listeners = new IntHashTable();   // resource ID -> ArrayList<IResourceListListener>
            private enum Operation { Added, Changed, Deleting };

            public TreeListenerManager( int parentProp )
            {
                _parentProp = parentProp;
                _resources = Core.ResourceStore.FindResourcesWithPropLive( null, _parentProp );
                _resources.ResourceAdded += new ResourceIndexEventHandler( HandleResourceAdded );
                _resources.ResourceChanged += new ResourcePropIndexEventHandler( HandleResourceChanged );
                _resources.ResourceDeleting += new ResourceIndexEventHandler( HandleResourceDeleting );
                _resources.ChangedResourceDeleting += new ResourcePropIndexEventHandler( HandleChangedResourceDeleting );
            }

            public void RegisterListener( IResource parent, IResourceListListener listener )
            {
                lock( _listeners )
                {
                    ArrayList listenerList = (ArrayList) _listeners [parent.Id];
                    if ( listenerList == null )
                    {
                        listenerList = new ArrayList();
                        _listeners [parent.Id] = listenerList;
                    }
                    listenerList.Add( listener );
                }
            }

            public void UnregisterListener( IResource parent, IResourceListListener listener )
            {
                lock( _listeners )
                {
                    ArrayList listenerList = (ArrayList) _listeners [parent.Id];
                    if ( listenerList != null )
                    {
                        listenerList.Remove( listener );
                    }
                }
            }

            private void HandleResourceAdded( object sender, ResourceIndexEventArgs e )
            {
                IResource parent = e.Resource.GetLinkProp( _parentProp );
                NotifyListeners( parent, Operation.Added, e.Resource, null );
            }

            private void HandleResourceChanged( object sender, ResourcePropIndexEventArgs e )
            {
                if ( e.ChangeSet.IsPropertyChanged( _parentProp ) )
                {
                    LinkChange[] changes = e.ChangeSet.GetLinkChanges( _parentProp );
                    for( int i=0; i<changes.Length; i++ )
                    {
                        IResource target = Core.ResourceStore.TryLoadResource( changes [i].TargetId );
                        if ( target != null )
                        {
                            if ( changes [i].ChangeType == LinkChangeType.Add )
                            {
                                NotifyListeners( target, Operation.Added, e.Resource, null );
                            }
                            else
                            {
                                NotifyListeners( target, Operation.Deleting, e.Resource, null );
                            }
                        }
                    }
                }
                else
                {
                    IResource parent = e.Resource.GetLinkProp( _parentProp );
                    NotifyListeners( parent, Operation.Changed, e.Resource, e.ChangeSet );
                }
            }

            private void HandleResourceDeleting( object sender, ResourceIndexEventArgs e )
            {
                IResource parent = e.Resource.GetLinkProp( _parentProp );
                if ( parent != null )
                {
                    NotifyListeners( parent, Operation.Deleting, e.Resource, null );
                }
            }

            private void HandleChangedResourceDeleting( object sender, ResourcePropIndexEventArgs e )
            {
                if ( e.ChangeSet.IsPropertyChanged( _parentProp ) )
                {
                    LinkChange[] changes = e.ChangeSet.GetLinkChanges( _parentProp );
                    for( int i=0; i<changes.Length; i++ )
                    {
                        IResource target = Core.ResourceStore.TryLoadResource( changes [i].TargetId );
                        if ( changes [i].ChangeType == LinkChangeType.Delete && target != null )
                        {
                            NotifyListeners( target, Operation.Deleting, e.Resource, null );
                        }
                    }
                }
            }

            private void NotifyListeners( IResource parent, Operation op, IResource res, IPropertyChangeSet cs )
            {
                lock( _listeners )
                {
                    ArrayList listenerList = (ArrayList) _listeners [parent.Id];
                    if ( listenerList != null )
                    {
                        for( int i=0; i<listenerList.Count; i++ )
                        {
                            IResourceListListener listener = (IResourceListListener) listenerList [i];
                            switch( op )
                            {
                                case Operation.Added:
                                    listener.ResourceAdded( res );
                                    break;

                                case Operation.Changed:
                                    listener.ResourceChanged( res, cs );
                                    break;

                                case Operation.Deleting:
                                    listener.ResourceDeleting( res );
                                    break;
                            }
                        }
                    }
                }
            }
        }
        
        private IntHashTable _treeListenerManagers = new IntHashTable();  // parent prop ID -> TreeListenerManager

        public ResourceTreeManager( IResourceStore store )
        {
            _store = store;
            RegisterTreeProps();

            IResourceList rootList = store.GetAllResources( _resTreeRoot );
            if ( rootList.Count == 1 && !rootList [0].HasProp( "RootResourceType") )
            {
                rootList [0].SetProp( _propRootResType, _resTreeRoot );
            }

            _rootOfRoots = GetRootForType( _resTreeRoot );
            _rootOfRoots.SetProp( "SortPropStr", "RootSortOrder" );
        }
        
        private void RegisterTreeProps()
        {
            _store.ResourceTypes.Register( _resTreeRoot, "", 
                ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex | ResourceTypeFlags.ResourceContainer );

            _store.PropTypes.Register( "SortProp", PropDataType.Int, PropTypeFlags.Internal );
            _store.PropTypes.Register( "SortPropStr", PropDataType.String, PropTypeFlags.Internal );
            _store.PropTypes.Register( "RootSortOrder", PropDataType.Int, PropTypeFlags.Internal );
            _propRootResType = _store.PropTypes.Register( "RootResourceType", PropDataType.String, PropTypeFlags.Internal );

            _store.RegisterUniqueRestriction( "ResourceTreeRoot", _propRootResType );
        }

        public IResource ResourceTreeRoot
        {
            get { return GetRootForType( _resTreeRoot ); }
        }

	    public IResource GetRootForType( string resType )
        {
            IResourceList resList = _store.FindResources( _resTreeRoot, 
                _propRootResType, resType );

            if ( resList.Count > 0 )
            {
                return resList [0];
            }

            ResourceProxy proxy = ResourceProxy.BeginNewResource( _resTreeRoot );
            proxy.SetProp( _propRootResType, resType );
            proxy.EndUpdate();
            return proxy.Resource;
        }

        public void LinkToResourceRoot( IResource res, int index )
        {
			// First, set the user-sort-order, if appropriate, so that there were no jumping of the item after it's added
			if(index == int.MinValue)
				new UserResourceOrder(res, JobPriority.Immediate).Insert( 0, new int[]{res.OriginalId}, false, null );	// To the beginning
			else if(index == int.MaxValue)
				new UserResourceOrder(res, JobPriority.Immediate).Insert( 0, new int[]{res.OriginalId}, true, null );	// To the end
			else if(index < 0)
				throw new ArgumentOutOfRangeException("index", "The index must be a non-negative integer value, int.MinValue, or int.MaxValue.");

			// Insert the resource
			IResource rootResource = GetRootForType( _resTreeRoot );
            if ( res.GetLinkProp( Core.Props.Parent ) != rootResource || res.GetIntProp( "RootSortOrder" ) != index )
            {
                ResourceProxy proxy = new ResourceProxy( res );
                proxy.BeginUpdate();
                if ( rootResource != null )
                {
                    proxy.AddLink( Core.Props.Parent, rootResource );
                }
                proxy.SetProp( "RootSortOrder", index );
                proxy.EndUpdate();
            }
        }

        public void SetResourceNodeSort( IResource node, string sortProps )
        {
            if ( node.GetStringProp( "SortPropStr" ) != sortProps )
            {
                ResourceProxy proxy = new ResourceProxy( node );
                proxy.BeginUpdate();
                proxy.DeleteProp( "SortProp" );
                proxy.SetProp( "SortPropStr", sortProps );
                proxy.EndUpdate();
            }

            lock( _sortPropCache )
            {
                _sortPropCache [node.Id] = sortProps;
            }
        }

        public string GetResourceNodeSort( IResource node )
        {
            IResource res = node;
            while( res != null )
            {
                lock( _sortPropCache )
                {
                    string sort = (string) _sortPropCache [res.Id];
                    if ( sort != null )
                        return sort;

                    sort = node.GetStringProp( "SortPropStr" );
                    if ( sort != null )
                    {
                        _sortPropCache [node.Id] = sort;
                        return sort;
                    }
                }
                res = res.GetLinkProp( Core.Props.Parent );
           }
           return null;
        }

        public void SetViewsExclusive( string resType )
        {
            _viewsExclusive.Add( resType );
        }

        public bool AreViewsExclusive( string resType )
        {
            return _viewsExclusive.Contains( resType );
        }

	    public void RegisterTreeListener( IResource parent, int parentProp, IResourceListListener listener )
	    {
            TreeListenerManager manager;
            lock( _treeListenerManagers )
            {
                manager = (TreeListenerManager) _treeListenerManagers [parentProp];
                if ( manager == null )
                {
                    manager = new TreeListenerManager( parentProp );
                    _treeListenerManagers [parentProp] = manager;
                }
           }
           manager.RegisterListener( parent, listener );
	    }

        public void UnregisterTreeListener( IResource parent, int parentProp, IResourceListListener listener )
        {
            TreeListenerManager manager;
            lock( _treeListenerManagers )
            {
                manager = (TreeListenerManager) _treeListenerManagers [parentProp];
            }
            if ( manager != null )
            {
                manager.UnregisterListener( parent, listener );
            }
        }
	}
}
