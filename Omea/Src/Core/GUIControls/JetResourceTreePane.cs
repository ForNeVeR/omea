/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// A resource tree pane implementation that provides workspace filtering as used by
	/// resource structure panes.
	/// </summary>
	public class JetResourceTreePane: ResourceTreePaneBase
	{
        private ResourceTreeTypeFilter _treeTypeFilter;
	    private IResource _lastActiveWorkspace = null;

        public override void SetActiveWorkspace( IResource workspace )
	    {
            if ( _lastActiveWorkspace == workspace )
            {
                return;
            }
            _lastActiveWorkspace = workspace;

            _resourceTree.Filters.Remove( _treeTypeFilter );
            if ( workspace == null )
            {
                JetWorkspaceResourcesProvider provider = _dataProvider.ResourceChildProvider as JetWorkspaceResourcesProvider;
                if ( provider != null )
                {
                    provider.Dispose();
                }
                _dataProvider.ResourceChildProvider = null;
            }
            else
            {
                _dataProvider.ResourceChildProvider = new JetWorkspaceResourcesProvider( _dataProvider, workspace, 
                    _workspaceFilterTypes, _rootResource );
                bool otherViewVisible = (Core.WorkspaceManager as WorkspaceManager).HasResourcesOutsideContainers( workspace );
                if ( _treeTypeFilter == null )
                {
                    _treeTypeFilter = new ResourceTreeTypeFilter( _workspaceFilterTypes, otherViewVisible );
                }
                else
                {
                    _treeTypeFilter.OtherViewVisible = otherViewVisible;
                }
                    
                _resourceTree.Filters.Add( _treeTypeFilter );
            }
            _dataProvider.RebuildTree();
            AsyncUpdateSelection();
        }
	}

    internal class JetWorkspaceResourcesProvider: IJetResourceChildProvider, IDisposable
    {
        private ResourceTreeDataProvider _dataProvider;
        private IResource _workspace;
        private IResourceList _workspaceContentsList;
        private string[] _workspaceFilterTypes;
        private WorkspaceManager _workspaceManager;
        private IResource _treeRoot;

        public JetWorkspaceResourcesProvider( ResourceTreeDataProvider dataProvider, IResource workspace, 
            string[] workspaceFilterTypes, IResource treeRoot )
        {
            _workspaceManager = Core.WorkspaceManager as WorkspaceManager;
            _workspace = workspace;
            _dataProvider = dataProvider;
            _workspaceFilterTypes = workspaceFilterTypes;
            _treeRoot = treeRoot;

            _workspaceContentsList = _workspace.GetLinksToLive( null, _workspaceManager.Props.InWorkspace );
            _workspaceContentsList = _workspaceContentsList.Union( _workspace.GetLinksToLive( null, _workspaceManager.Props.InWorkspaceRecursive ) );
            _workspaceContentsList = _workspaceContentsList.Union( Core.ResourceStore.FindResourcesWithProp( null, _workspaceManager.Props.VisibleInAllWorkspaces ) );
            _workspaceContentsList.Sort( new SortSettings( Core.Props.Name, true ) );

            _workspaceContentsList.ResourceAdded += OnWorkspaceContentsChanged;
            _workspaceContentsList.ResourceDeleting += OnWorkspaceContentsChanged;
        }

        public void Dispose()
        {
            _workspaceContentsList.ResourceAdded -= OnWorkspaceContentsChanged;
            _workspaceContentsList.ResourceDeleting -= OnWorkspaceContentsChanged;
            _workspaceContentsList.Dispose();
        }

        private void OnWorkspaceContentsChanged( object sender, ResourceIndexEventArgs e )
        {
            if ( Array.IndexOf( _workspaceFilterTypes, e.Resource.Type ) >= 0 )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( UpdateWorkspaceTree ) );
            }
        }

        private void UpdateWorkspaceTree()
        {
            _dataProvider.RebuildTree();
        }

        public IResourceList GetChildResources( IResource parent )
        {
            IResourceList result = null;
            if ( parent == _treeRoot )
            {
                return _workspaceContentsList;
            }
            WorkspaceResourceType wrType = Core.WorkspaceManager.GetWorkspaceResourceType( parent.Type );
            if ( wrType == WorkspaceResourceType.Container && parent.HasLink( _workspaceManager.Props.InWorkspace, _workspace ) )
            {
                // if it's a container that is not linked recursively - do not show children
                return Core.ResourceStore.EmptyResourceList;
            }
            
            result = parent.GetLinksToLive( null, _dataProvider.ParentProperty );
            result = result.Minus( _workspace.GetLinksToLive( null, _workspaceManager.Props.ExcludeFromWorkspace ) );
            string sortProps = Core.ResourceTreeManager.GetResourceNodeSort( parent );
            if ( sortProps != null )
            {
                result.Sort( sortProps );
            }
            return result;
        }
    }

    /**
     * The filter which shows only resources of the specified types in the tree.
     */

    internal class ResourceTreeTypeFilter: IJetListViewNodeFilter
    {
        private string[] _resTypes;
        private bool _otherViewVisible;

        internal ResourceTreeTypeFilter( string[] resTypes, bool otherViewVisible )
        {
            _resTypes = resTypes;
            _otherViewVisible = otherViewVisible;
        }

        internal bool OtherViewVisible
        {
            get { return _otherViewVisible; }
            set { _otherViewVisible = value; }
        }

        public bool AcceptNode( JetListViewNode node )
        {
            if ( node.Level == 0 )
            {
                IResource res = (IResource) node.Data;
                return (Array.IndexOf( _resTypes, res.Type ) >= 0 || 
                    ( res.Type == "WorkspaceOtherView" && _otherViewVisible ));
            }
            return true;
        }

        public event EventHandler FilterChanged;
    }
}
