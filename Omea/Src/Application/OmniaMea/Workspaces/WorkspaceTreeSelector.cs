/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.Workspaces
{
    /// <summary>
    /// The tree-based selector of resources visible in a workspace.
    /// </summary>
    internal class WorkspaceTreeSelector: UserControl, IResourceChildProvider, IWorkspaceSelector
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private string[] _resourceTypes;
        private Button _btnAdd;
        private Button _btnAddWithChildren;
        private Button _btnRemove;
        private Label _lblAvailable;
        private Label _lblInWorkspace;
        private Label _lblProcessing;
        private ResourceTreeView _tvAvailable;
        private ResourceTreeView _tvInWorkspace;
        private IResource _rootResource;
        private int _parentProperty;
        private IResourceNodeFilter _availTreeFilter;
        private IResourceNodeFilter _inWorkspaceTreeFilter;
        private WorkspaceManager _workspaceManager;
        private IResource _currentWorkspace;
        private bool _operationInProgress;
        private bool _needRebuildTree;

        public WorkspaceTreeSelector( string[] resTypes, IResource rootResource, int parentProperty,
            IResourceNodeFilter availTreeFilter, IResourceNodeFilter inWorkspaceTreeFilter )
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            _resourceTypes = resTypes;
            _rootResource = rootResource;
            _parentProperty = parentProperty;
            _availTreeFilter = availTreeFilter;
            _inWorkspaceTreeFilter = inWorkspaceTreeFilter;
            _workspaceManager = Core.WorkspaceManager as WorkspaceManager;
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }
        #endregion

        public Control GetControl()
        {
            return this;
        }

        public void SetWorkspace( IResource workspace )
        {
            _currentWorkspace = workspace;
            _tvInWorkspace.RootResource = workspace;
            _tvInWorkspace.ExpandAll();

            if ( workspace != null )
            {
                _lblInWorkspace.Text = "In Workspace '" + workspace.DisplayName + "':";
            }
            else
            {
                _lblInWorkspace.Text = "In Workspace:";
            }
        }

        public void CreateComponents()
        {
            _lblAvailable = new Label();
            _lblAvailable.Text = "Available:";
            _lblAvailable.FlatStyle = FlatStyle.System;
            _lblAvailable.AutoSize = true;

            _lblInWorkspace = new Label();
            _lblInWorkspace.FlatStyle = FlatStyle.System;
            _lblInWorkspace.AutoSize = true;

            _lblProcessing = new Label();
            _lblProcessing.FlatStyle = FlatStyle.System;
            _lblProcessing.AutoSize = true;
            _lblProcessing.Visible = false;
            _lblProcessing.Text = "Processing...";

            _btnAdd = CreateTabButton( "Add", OnAddTreeClick );
            _btnAddWithChildren = CreateTabButton( "Add Subtree", OnAddTreeRecursiveClick );
            _btnRemove = CreateTabButton( "Remove", OnRemoveTreeClick );

            _tvAvailable = new ResourceTreeView();
            _tvAvailable.AddNodeFilter( new HideAllWorkspacesResourcesFilter() );
            if ( _availTreeFilter != null )
            {
                _tvAvailable.AddNodeFilter( _availTreeFilter );
            }
            _tvAvailable.RootResource = _rootResource;
            if ( _parentProperty > 0 )
            {
                _tvAvailable.ParentProperty = _parentProperty;
            }
            else
            {
                _tvAvailable.ParentProperty = Core.Props.Parent;
            }
            
            _tvAvailable.MultiSelect = true;
            _tvAvailable.HideSelection = false;
            _tvAvailable.ExecuteDoubleClickAction = false;
            _tvAvailable.ShowContextMenu = false;
            _tvAvailable.AllowDrop = false;
            _tvAvailable.DoubleClick += OnAvailableTreeDoubleClick;
            _tvAvailable.AfterSelect += OnAvailableTreeSelectionChanged;
            _tvAvailable.TreeUpdated += OnTreeUpdated;

            _tvInWorkspace = new ResourceTreeView();
            _tvInWorkspace.ResourceChildProvider = this;
            _tvInWorkspace.MultiSelect = true;
            _tvInWorkspace.HideSelection = false;
            _tvInWorkspace.ParentProperty = _workspaceManager.GetRecurseLinkPropId( _resourceTypes [0] );
            _tvInWorkspace.ExecuteDoubleClickAction = false;
            _tvInWorkspace.ShowContextMenu = false;
            _tvInWorkspace.AllowDrop = false;
            _tvInWorkspace.DoubleClick += OnRemoveTreeClick;
            _tvInWorkspace.AfterSelect += OnWorkspaceTreeSelectionChanged;
            _tvInWorkspace.TreeUpdated += OnTreeUpdated;
            if ( _inWorkspaceTreeFilter != null )
            {
                _tvInWorkspace.AddNodeFilter( _inWorkspaceTreeFilter );
            }

            UpdateAvailableTreeSelection();
            UpdateWorkspaceTreeSelection();

            Controls.AddRange( new Control[] { _lblAvailable, _tvAvailable, 
                                                 _btnAdd, _btnAddWithChildren, _btnRemove, _lblProcessing,
                                                 _lblInWorkspace, _tvInWorkspace } );
        }

        private void OnAvailableTreeSelectionChanged( object sender, TreeViewEventArgs e )
        {
            UpdateAvailableTreeSelection();
        }

        private void UpdateAvailableTreeSelection()
        {
            bool allFolders = true;
            foreach ( IResource selResource in _tvAvailable.SelectedResources )
            {
                WorkspaceResourceType wrType = _workspaceManager.GetWorkspaceResourceType( selResource.Type );
                if (wrType != WorkspaceResourceType.Folder )
                {
                    allFolders = false;
                }
            }
            _btnAdd.Enabled = !allFolders;
            _btnAddWithChildren.Enabled = (_tvAvailable.SelectedResources.Count > 0);
        }

        private void OnAvailableTreeDoubleClick( object sender, EventArgs e )
        {
            if ( _btnAdd.Enabled )
            {
                BeginWorkspaceOperation();
                Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( DoAdd ),
                    _tvAvailable.SelectedResources );
            }
            else if ( _btnAddWithChildren.Enabled )
            {
                BeginWorkspaceOperation();
                Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( DoAddRecursive ),
                    _tvAvailable.SelectedResources );
            }
        }

        private void BeginWorkspaceOperation()
        {
            _lblProcessing.Visible = true;
            _btnAdd.Enabled = false;
            _btnAddWithChildren.Enabled = false;
            _btnRemove.Enabled = false;
            _operationInProgress = true;
        }

        private void OnAddTreeClick( object sender, EventArgs e )
        {
            BeginWorkspaceOperation();
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( DoAdd ),
                _tvAvailable.SelectedResources );                               
        }

        private void DoAdd( IResourceList resList )
        {
            ArrayList skippedResources = new ArrayList();
            foreach( IResource res in resList )
            {
                if ( _workspaceManager.IsInWorkspaceRecursive( _currentWorkspace, res ) )
                {
                    skippedResources.Add( res );
                }
                else
                {
                    _workspaceManager.AddResourceToWorkspace( _currentWorkspace, res );
                }
            }
            Core.UIManager.QueueUIJob( new UpdateDelegate( UpdateTree ), resList, skippedResources );
        }

        private void OnAddTreeRecursiveClick( object sender, EventArgs e )
        {
            BeginWorkspaceOperation();
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( DoAddRecursive ),
                _tvAvailable.SelectedResources );
        }

        private void DoAddRecursive( IResourceList selResourceList )
        {
            ArrayList skippedResources = new ArrayList();

            // if any of the resources were added non-recursively, we need to do a full
            // tree rebuild in order for second-level children to be updated correctly (#5554)
            foreach( IResource res in selResourceList )
            {
                if ( _workspaceManager.IsInWorkspaceRecursive( _currentWorkspace, res ) )
                {
                    skippedResources.Add( res );
                }
                else
                {
                    if ( res.HasLink( "InWorkspace", _currentWorkspace ) )
                    {
                        _needRebuildTree = true;
                    }
                    _workspaceManager.AddResourceToWorkspaceRecursive( _currentWorkspace, res );
                }
            }

            Core.UIManager.QueueUIJob( new UpdateDelegate( UpdateTree ), selResourceList, skippedResources );
        }

        private void UpdateTree( IResourceList selResourceList, ArrayList skippedResources )
        {
            if ( _tvInWorkspace.IsDisposed )
            {
                return;
            }

            if ( _needRebuildTree )
            {
                _tvInWorkspace.UpdateNodeFilter( true );
            }
            _tvInWorkspace.ExpandAll();
            _tvInWorkspace.SelectResourceNodes( selResourceList );
            AddToWorkspaceAction.ReportSkippedResources( FindForm(), skippedResources );
        }

        private delegate void UpdateDelegate( IResourceList selResourceList, ArrayList skippedResources );

        private void OnTreeUpdated( object sender, EventArgs e )
        {
            if ( _operationInProgress )
            {
                _lblProcessing.Visible = false;
                UpdateAvailableTreeSelection();
                UpdateWorkspaceTreeSelection();
            }
        }

        private void OnWorkspaceTreeSelectionChanged( object sender, TreeViewEventArgs e )
        {
            UpdateWorkspaceTreeSelection();
        }

        private void UpdateWorkspaceTreeSelection()
        {
            _btnRemove.Enabled = _tvInWorkspace.SelectedResources.Count > 0;
        }

        private void OnRemoveTreeClick( object sender, EventArgs e )
        {
            BeginWorkspaceOperation();
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( DoRemove ),
                _tvAvailable.SelectedResources );
        }

        private void DoRemove( IResourceList selResourceList )
        {
            _workspaceManager.RemoveResourcesFromWorkspace( _currentWorkspace, 
                _tvInWorkspace.SelectedResources );
        }

        public IResourceList GetChildResources( ResourceTreeView resourceTree, IResource parent )
        {
            IResourceList result = null;
            if ( parent.Type == "Workspace" )
            {
                result = parent.GetLinksToLive( null, "InWorkspace" ).Union( 
                         parent.GetLinksToLive( null, "InWorkspaceRecursive" ) );
                result.Sort( new SortSettings( Core.Props.Name, true ) );
            }
            else
            {
                // if any of our parents has been added recursively, return our children
                // as the child list
                IResource nextParent = parent;
                while( nextParent != null )
                {
                    if ( nextParent.HasLink( "InWorkspaceRecursive", _currentWorkspace ) )
                    {
                        IResourceList excludedResources = _currentWorkspace.GetLinksOfTypeLive( null, "ExcludeFromWorkspace" );
                        result = parent.GetLinksToLive( null, _tvInWorkspace.ParentProperty ).Minus( excludedResources );
                        string sortProps = Core.ResourceTreeManager.GetResourceNodeSort( parent );
                        if ( sortProps != null )
                        {
                            result.Sort( sortProps );
                        }
                        break;
                    }
                    nextParent = nextParent.GetLinkProp( _tvInWorkspace.ParentProperty );
                }
            }
            
            if ( result != null )
            {
                IResourceList filterList = null;
                foreach( string resType in _resourceTypes )
                {
                    if ( _workspaceManager.GetWorkspaceResourceType( resType ) != WorkspaceResourceType.None )
                    {
                        filterList = Core.ResourceStore.GetAllResourcesLive( resType ).Union( filterList, true );
                    }
                }
                result = result.Intersect( filterList, true );
                return result;
            }
            return Core.ResourceStore.EmptyResourceList;
        }

        private static Button CreateTabButton( string name, EventHandler clickHandler )
        {
            Button btnAdd = new Button();
            btnAdd.Text = name;
            btnAdd.FlatStyle = FlatStyle.System;
            btnAdd.Size = new Size( 72, 24 );
            btnAdd.Click += clickHandler;
            return btnAdd;
        }

        protected override void OnSizeChanged( EventArgs e )
        {
            base.OnSizeChanged( e );

            int middleSpaceX = (int) (48 * Core.ScaleFactor.Width);
            
            _lblAvailable.Location = new Point( 0, 0 );
            _lblInWorkspace.Location = new Point( Width / 2 + middleSpaceX, 0 );

            _tvAvailable.Location = new Point( 0, 20 );
            _tvAvailable.Size = new Size( Width / 2 - middleSpaceX, Height-20 );

            _tvInWorkspace.Location = new Point( Width / 2 + middleSpaceX, 20 );
            _tvInWorkspace.Size = _tvAvailable.Size;
 
            Size btnSize = new Size( (int) (72 * Core.ScaleFactor.Width),
                (int) (24 * Core.ScaleFactor.Height) );
            
            int btnX = (int) (Width / 2 - (36 * Core.ScaleFactor.Width ));
            _btnAdd.Location = new Point( btnX, 24 );
            _btnAdd.Size = btnSize;
            _btnAddWithChildren.Location = new Point( btnX, 56 );
            _btnAddWithChildren.Size = btnSize;
            _btnRemove.Location = new Point( btnX, 88 );
            _btnRemove.Size = btnSize;

            _lblProcessing.Location = new Point( btnX, 120 );
        }

        private class HideAllWorkspacesResourcesFilter: IResourceNodeFilter
        {
            public bool AcceptNode( IResource res, int level )
            {
                return !res.HasProp( "VisibleInAllWorkspaces" );
            }
        }
    }
}