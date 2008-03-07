/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using JetBrains.DataStructures;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Fills ResourceListView2 with data from a tree of resources, given the root
	/// resource and the ID of the parent property.
	/// </summary>
	public class ResourceTreeDataProvider: IResourceDataProvider
	{
		#region ResourceTreeDataNode Class — A class that's created for each tree node and listens for its changes

		/// <summary>
		/// A class that's created for each tree node and listens for its changes.
		/// </summary>
		private class ResourceTreeDataNode: IDisposable, IResourceListListener
		{
			private ResourceTreeDataProvider _owner;
			private JetListView _listView;
			private IResource _parentResource;
			private int _parentProp;
			private IResourceList _childResources;
			private bool _handlersAttached = false;

			/// <summary>
			/// Creates a node displaying the children of the specified resource.
			/// </summary>
			/// <param name="owner">The data provider to which the node belongs.</param>
			/// <param name="listView">The list view displaying the nodes.</param>
			/// <param name="parentResource">The node whose children are displayed.</param>
			/// <param name="parentProp">The ID of the link property between the node and its children,
			/// or 0 if a custom list of children is used.</param>
			/// <param name="childResources">The list of child resources.</param>
			public ResourceTreeDataNode( ResourceTreeDataProvider owner, JetListView listView, 
			                             IResource parentResource, int parentProp, IResourceList childResources )
			{
				_listView = listView;
				_owner = owner;
				_parentResource = parentResource;
				_parentProp = parentProp;
				_childResources = childResources;
			}

			internal void AttachHandlers()
			{
				if ( !_handlersAttached )
				{
					_handlersAttached = true;
					if ( _parentProp != 0 )
					{
						Core.ResourceTreeManager.RegisterTreeListener( _parentResource, _parentProp, this );
						_childResources.Dispose();
					}
					else
					{
						_childResources.ResourceAdded += new ResourceIndexEventHandler( HandleResourceAdded );
						_childResources.ResourceChanged += new ResourcePropIndexEventHandler( HandleResourceChanged );
						_childResources.ResourceDeleting += new ResourceIndexEventHandler( HandleResourceDeleting );
					}
				}
			}

			public void Dispose()
			{
				if ( _handlersAttached )
				{
					if ( _parentProp != 0 )
					{
						Core.ResourceTreeManager.UnregisterTreeListener( _parentResource, _parentProp, this );
					}
					else
					{
						_childResources.ResourceAdded -= new ResourceIndexEventHandler( HandleResourceAdded );
						_childResources.ResourceChanged -= new ResourcePropIndexEventHandler( HandleResourceChanged ) ;
						_childResources.ResourceDeleting -= new ResourceIndexEventHandler( HandleResourceDeleting );
					}
				}
			}

			public void ResourceAdded( IResource res )
			{
				_owner.AddResource( _parentResource, res );
			}

			public void ResourceDeleting( IResource res )
			{
				_owner.RemoveResource( _parentResource, res );
			}

			public void ResourceChanged( IResource res, IPropertyChangeSet cs )
			{
				Core.UIManager.QueueUIJob( new ResourceDelegate( DoUpdateResource ), res );
				if(cs.IsPropertyChanged( Core.Props.UserResourceOrder ))
					Core.UserInterfaceAP.QueueJob( "Rearrange Children", new RearrangeChildrenDelegate(_owner.RearrangeChildren), res );
			}

			private void HandleResourceAdded( object sender, ResourceIndexEventArgs e )
			{
				ResourceAdded( e.Resource );
			}

			private void HandleResourceChanged( object sender, ResourcePropIndexEventArgs e )
			{
				ResourceChanged( e.Resource, e.ChangeSet );
			}

			private void DoUpdateResource( IResource res )
			{
				if ( Core.State != CoreState.ShuttingDown )
				{
					// the resource change is queued asynchronously, so the item may
					// have been removed from the list
					_listView.UpdateItemSafe( res );


				}
			}

			private void HandleResourceDeleting( object sender, ResourceIndexEventArgs e )
			{
				ResourceDeleting( e.Resource );
			}

			internal IResourceList ChildResources
			{
				get { return _childResources; }
			}
		}

		#endregion

		private IResource _rootResource;
        private int _parentProp;
        private JetListView _listView;
		/// <summary>
		/// Maps Resource IDs of tree non-leaf nodes to the <see cref="ResourceTreeDataNode"/> instances.
		/// </summary>
        private IntHashTable _dataNodes = new IntHashTable();
        private IJetResourceChildProvider _resourceChildProvider;

		/// <summary>
		/// A resource list that consists of a single root resource and provides for listening to the changes in it, particularily in the user-sort-order property.
		/// </summary>
		private IResourceList	_listRoot = null;

	    public ResourceTreeDataProvider()
	    {
	    }

	    public ResourceTreeDataProvider( IResource rootResource, int parentProp )
	    {
	        if ( rootResource == null )
                throw new ArgumentNullException( "rootResource" );
            if ( Core.ResourceStore.PropTypes [parentProp].DataType != PropDataType.Link )
                throw new ArgumentException( "parentProp is not a link property", "parentProp" );

	    	SetRootResourceImpl( rootResource);
	        _parentProp = parentProp;
	    }

		/// <summary>
		/// Assigns a new root resource, and also catches up with the resource list listening to its changes.
		/// </summary>
		protected void SetRootResourceImpl( IResource value)
		{
			if(value == _rootResource)
				return;	// No change
			if(value == null)
				throw new ArgumentNullException("value");

			if(_listRoot != null)
			{
				_listRoot.ResourceChanged -= new ResourcePropIndexEventHandler(OnRootResourceChanged);
				_listRoot = null;
			}

			_rootResource = value;

			_listRoot = value.ToResourceListLive();
			_listRoot.ResourceChanged += new ResourcePropIndexEventHandler(OnRootResourceChanged);
			
		}

		private void OnRootResourceChanged( object sender, ResourcePropIndexEventArgs e )
		{
			// If the sorting-order has changed, reapply the sorting
			if(e.ChangeSet.IsPropertyChanged( Core.Props.UserResourceOrder ))
				Core.UserInterfaceAP.QueueJob( "Rearrange Children", new RearrangeChildrenDelegate(RearrangeChildren), e.Resource );
		}

		public IJetResourceChildProvider ResourceChildProvider
	    {
	        get { return _resourceChildProvider; }
	        set { _resourceChildProvider = value; }
	    }

        public int ParentProperty
        {
            get { return _parentProp; }
        }

	    public void FillResources( ResourceListView2 listView )
	    {
            _listView = listView.JetListView;
            _listView.ChildrenRequested += new RequestChildrenEventHandler( HandleChildrenRequested );

			// Set the proper root
			listView.RootResource = _rootResource;

            ExpandResource( _listView.Root, _rootResource );
	    }
        
		/// <summary>
		/// For an existing tree node (incl. the originaly-present root), sets up the sorting for that node,
		/// and expands it by adding all the "child" resources as child nodes.
		/// </summary>
        private void ExpandResource( JetListViewNode parentNode, IResource parentResource )
        {
			//////////////////
			// Apply sorting
			UserOrderSortSettings settings = null;

			// Get the per-property sort settings as a string (written to each node)
            string nodeSort = Core.ResourceTreeManager.GetResourceNodeSort( parentResource );
            if(nodeSort != null)	// Apply, if available
            	settings = UserOrderSortSettings.Parse( Core.ResourceStore, nodeSort );
			else
				settings = new UserOrderSortSettings();

			// Apply the user-order sorting (it's writter as a property on the parent with an ordered list of child resources)
            settings.SetUserOrder( parentResource );

			// Submit the sorting order to the node
            _listView.NodeCollection.SetItemComparer(
            	parentNode.Data
            	, new ResourceComparer( null, new UserOrderResourceComparer( settings ), true ) );

			/////////////////
			// Get Children
        	ResourceTreeDataNode dataNode;
            lock( _dataNodes )
            {
                dataNode = (ResourceTreeDataNode) _dataNodes [parentResource.Id];
                if ( dataNode == null )
                {
                    IResourceList childResources = null;
                    int parentProp = _parentProp;
                    if ( _resourceChildProvider != null )
                    {
                        childResources = _resourceChildProvider.GetChildResources( parentResource );
                    }
                    if ( childResources == null )
                    {
                        childResources = parentResource.GetLinksToLive( null, _parentProp );
                    }
                    else
                    {
                        parentProp = 0;
                    }

                    dataNode = new ResourceTreeDataNode( this, _listView, parentResource, parentProp, childResources );
                    _dataNodes [parentResource.Id] = dataNode;
                }
            }

			////////////////////
			// Submit Children
            lock( dataNode.ChildResources )
            {
				// Add 'em!
                foreach( IResource res in dataNode.ChildResources.ValidResources )
                {
                    AddNode( parentNode, res );	// This also calls an ExpandResource for that node
                }
				// Listen for changes to the children resources of the node (not to the node resource itself)
                dataNode.AttachHandlers();
            }
        }

	    private void AddNode( JetListViewNode parentNode, IResource child )
	    {
	        JetListViewNode node = parentNode.Nodes.Add( child );
            if ( IsResourceContainer( child ) )
            {
                if ( child.GetLinkCount( -_parentProp ) > 0 )
                {
                    node.HasChildren = true;
                }
                else
                {
                    // we have no children now, but may get some later
                    ExpandResource( node, child );
                }
            }
	    }

        private  void AddResource( IResource parentResource, IResource resource )
        {
            JetListViewNode[] nodes = _listView.NodeCollection.NodesFromItem( parentResource );
            for( int i=0; i<nodes.Length; i++ )
            {
                AddNode( nodes [i], resource );
            }
            if ( parentResource == _rootResource )
            {
                AddNode( _listView.Root, resource );
            }
        }

        private void RemoveNode( JetListViewNode parentNode, IResource resource )
        {
            if ( parentNode.Nodes.Contains( resource ) )
            {
                parentNode.Nodes.Remove( resource );
            }
            if ( !_listView.NodeCollection.Contains( resource ) )
            {
                ResourceTreeDataNode dataNode = (ResourceTreeDataNode) _dataNodes [resource.Id];
                if ( dataNode != null )
                {
                    dataNode.Dispose();
                    lock( _dataNodes )
                    {
                        _dataNodes.Remove( resource.Id );
                    }
                }
            }
        }

        private void RemoveResource( IResource parentResource, IResource resource )
        {
            JetListViewNode[] nodes = _listView.NodeCollection.NodesFromItem( parentResource );
            for( int i=0; i<nodes.Length; i++ )
            {
                RemoveNode( nodes [i], resource );
            }
            if ( parentResource == _rootResource )
            {
                RemoveNode( _listView.Root, resource );
            }
        }

		/// <summary>
		/// Checks if the specified resource is a resource container.
		/// </summary>
        private bool IsResourceContainer( IResource child )
        {
            return Core.ResourceStore.ResourceTypes [child.Type].HasFlag( ResourceTypeFlags.ResourceContainer );
        }

        private void HandleChildrenRequested( object sender, RequestChildrenEventArgs e )
        {
            IResource res = (IResource) e.Node.Data;
            if ( e.Node.Nodes.Count == 0 )
            {
                ExpandResource( e.Node, res );
                if ( e.Node.Nodes.Count == 0 )
                {
                    e.Node.HasChildren = false;
                }
            }
        }

        public void Dispose()
	    {
            DisposeDataNodes();

			if(_listRoot != null)
			{
				_listRoot.ResourceChanged -= new ResourcePropIndexEventHandler(OnRootResourceChanged);
				_listRoot = null;
			}
	    }

	    private void DisposeDataNodes()
	    {
	        lock( _dataNodes )
	        {
	            foreach( IntHashTable.Entry e in _dataNodes )
	            {
	                ResourceTreeDataNode node = (ResourceTreeDataNode) e.Value;
	                node.Dispose();
	            }
	            _dataNodes.Clear();
	        }
	    }

	    public JetListView ListView
        {
            get { return _listView; }
        }

	    public bool SelectResource( IResource res )
	    {
            if ( res == null )
            {
                return true;
            }

            JetListViewNode node = _listView.NodeCollection.NodeFromItem( res );
            if ( node == null )
            {
                if ( !FindResourceNode( res ) )
                {
                    return false;
                }
                node = _listView.NodeCollection.NodeFromItem( res );
            }
            if ( node != null && !node.FiltersAccept )
            {
                return false;
            }

            if ( _listView.Selection.Count == 1 && _listView.Selection [0] == res )
            {
                return true;
            }
            _listView.Selection.SelectSingleItem( res );
            return true;
        }

	    public bool FindResourceNode( IResource res )
	    {
            ArrayList parentStack = new ArrayList();
            IResource parent = res;
            do
            {
                parentStack.Insert( 0, parent );
                parent = parent.GetLinkProp( _parentProp );
                if ( parent == null )
                    return false;
            } while( parent != _rootResource );
    
            JetListViewNode node = null;
            foreach( IResource parentRes in parentStack )
            {
                node = _listView.NodeCollection.NodeFromItem( node, parentRes );
                if ( node == null )
                    break;

                if ( parentRes == res )
                {
                    return true;
                }
                    
                node.Expanded = true;
            }
            return false;
	    }

	    public void RebuildTree()
	    {
            if ( _listView != null )
            {
                DisposeDataNodes();
                _listView.Nodes.Clear();
                ExpandResource( _listView.Root, _rootResource );
            }
	    }

	    public void SetRootResource( IResource resource, int parentProperty )
	    {
	        if ( _rootResource != resource || _parentProp != parentProperty )
	        {
	        	SetRootResourceImpl(resource);
                _parentProp = parentProperty;
                if ( _listView != null )
                {
                    RebuildTree();
                }
	        }
	    }

		/// <summary>
		/// When the user-resource-order property changes on a node, reapplies the sorting.
		/// </summary>
		protected void RearrangeChildren(IResource res)
		{
			_listView.NodeCollection.Sort();
		}
		/// <summary>
		/// A delegate type for the <see cref="RearrangeChildren"/> function.
		/// </summary>
		protected delegate void RearrangeChildrenDelegate(IResource res);

	}

    public interface IJetResourceChildProvider
    {
        IResourceList GetChildResources( IResource parent );
    }
}
