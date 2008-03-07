/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.Workspaces
{
	/// <summary>
	/// The list-based selector of resources visible in a workspace.
	/// </summary>
    internal class WorkspaceListSelector : System.Windows.Forms.UserControl, IWorkspaceSelector
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private ResourceListView2 _lvAvailable;
        private ResourceListView2 _lvInWorkspace;
        private IResourceList       _listInWsp, _listAvailable;
        private JetTextBox          _edtFind;
        private ResourceNameJetFilter _nameJetFilter;
        private Button _btnAdd;
        private Button _btnRemove;
        private IResource _currentWorkspace;
        private string[] _resourceTypes;
        private Label _lblAvailable;
        private Label _lblInWorkspace;
        private Label _lblProcessing;
        private int _pendingOperations;

        public WorkspaceListSelector( string[] resTypes )
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            _resourceTypes = resTypes;
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
            if ( workspace != null )
            {
                _listInWsp = Core.WorkspaceManager.GetWorkspaceResourcesLive( workspace, _resourceTypes[ 0 ] );
                _listInWsp.Sort( new SortSettings( ResourceProps.DisplayName, true ) );
            }
            else
            {
                _listInWsp = Core.ResourceStore.EmptyResourceList;                            
            }
            _lvInWorkspace.DataProvider = new ResourceListDataProvider( _listInWsp );

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

            _btnAdd = CreateTabButton( "Add", new EventHandler( OnAddListClick ) );
            _btnRemove = CreateTabButton( "Remove", new EventHandler( OnRemoveListClick ) );

            //
            // _lvAvailable
            //
            _lvAvailable = new ResourceListView2();
            _lvAvailable.Columns.Add( new ResourceIconColumn() );
            _lvAvailable.AddColumn( ResourceProps.DisplayName ).AutoSize = true;

            IResourceList tabResources = Core.ResourceStore.GetAllResourcesLive( _resourceTypes [0] );
            tabResources.Sort( new int[] { ResourceProps.DisplayName }, true );
            _listAvailable = tabResources;
            _lvAvailable.DataProvider = new ResourceListDataProvider( _listAvailable );

            _nameJetFilter = new ResourceNameJetFilter( "" );
            _lvAvailable.Filters.Add( _nameJetFilter );
            _lvAvailable.HeaderStyle = ColumnHeaderStyle.None;
            _lvAvailable.ShowContextMenu = false;
            _lvAvailable.ExecuteDoubleClickAction = false;
            _lvAvailable.AllowDrop = false;
            _lvAvailable.DoubleClick += new HandledEventHandler( OnAddListClick );
            _lvAvailable.SelectionChanged += new EventHandler( OnListSelectionChanged );

            WorkspaceManager workspaceManager = Core.WorkspaceManager as WorkspaceManager;
            IResourceNodeFilter filter = workspaceManager.GetAvailSelectorFilter( _resourceTypes [0] );
            if ( filter != null )
            {
                _lvAvailable.Filters.Add( new TreeFilterJetWrapper( filter ) );
            }

            _edtFind = new JetTextBox();
            _edtFind.EmptyText = "<type a name to find>";
            _edtFind.IncrementalSearchUpdated += new EventHandler( OnListIncSearch );
            
            //
            // _lvInWorkspace
            //
            _lvInWorkspace = new ResourceListView2();
            _lvInWorkspace.Columns.Add( new ResourceIconColumn() );
            _lvInWorkspace.AddColumn( ResourceProps.DisplayName ).AutoSize = true;

            _lvInWorkspace.HeaderStyle = ColumnHeaderStyle.None;
            _lvInWorkspace.ShowContextMenu = false;
            _lvInWorkspace.ExecuteDoubleClickAction = false;
            _lvInWorkspace.AllowDrop = false;
            _lvInWorkspace.DoubleClick += new HandledEventHandler( OnRemoveListClick );
            _lvInWorkspace.SelectionChanged += new EventHandler( OnListSelectionChanged );

            UpdateListSelection();

            Controls.AddRange( new Control[] { _lblAvailable, _edtFind, _lvAvailable, 
                                                 _btnAdd, _btnRemove, _lblProcessing,
                                                 _lblInWorkspace, _lvInWorkspace } );
        }

        private static Button CreateTabButton( string name, EventHandler clickHandler )
        {
            Button btn = new Button();
            btn.Text = name;
            btn.FlatStyle = FlatStyle.System;
            btn.Size = new Size( 72, 24 );
            btn.Click += clickHandler;
            return btn;
        }

        private void BeginWorkspaceOperation()
        {
            _lblProcessing.Visible = true;
            _btnAdd.Enabled = false;
            _btnRemove.Enabled = false;
            _pendingOperations++;
        }

        private void OnAddListClick( object sender, HandledEventArgs e )
        {
            OnAddListClick( sender, (EventArgs) e );
        }
        private void OnAddListClick( object sender, EventArgs e )
        {
            BeginWorkspaceOperation();
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( DoAdd ),
                                      _lvAvailable.GetSelectedResources() );
        }

        private void OnRemoveListClick( object sender, HandledEventArgs e )
        {
            OnRemoveListClick( sender, (EventArgs) e );
        }
        private void OnRemoveListClick( object sender, EventArgs e )
        {
            BeginWorkspaceOperation();
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( DoRemove ),
                                      _lvInWorkspace.GetSelectedResources() );
        }

        private void DoAdd( IResourceList list )
        {
            Core.WorkspaceManager.AddResourcesToWorkspace( _currentWorkspace, list );
            Core.UserInterfaceAP.QueueJob( new MethodInvoker( ProcessPendingOperations ) );
        }
        private void DoRemove( IResourceList list )
        {
            Core.WorkspaceManager.RemoveResourcesFromWorkspace( _currentWorkspace, list );
            Core.UserInterfaceAP.QueueJob( new MethodInvoker( ProcessPendingOperations ) );
        }

        private void ProcessPendingOperations()
        {
            if ( _pendingOperations > 0 )
            {
                _pendingOperations--;
                if ( _pendingOperations == 0 )
                {
                    _lblProcessing.Visible = false;
                    UpdateListSelection();
                }
            }
        }

        private void OnListIncSearch( object sender, EventArgs e )
        {
            _nameJetFilter.FilterString = _edtFind.Text;
        }

        private void OnListSelectionChanged( object sender, EventArgs e )
        {
            UpdateListSelection();
        }

        private void UpdateListSelection()
        {
            _btnAdd.Enabled = _lvAvailable.GetSelectedResources().Count > 0;
            _btnRemove.Enabled = _lvInWorkspace.GetSelectedResources().Count > 0;
        }

        protected override void OnSizeChanged( EventArgs e )
        {
            base.OnSizeChanged( e );

            int middleSpaceX = (int) (48 * Core.ScaleFactor.Width);
            int listWidth = Width / 2 - middleSpaceX;

            _lblAvailable.Location = new Point( 0, 0 );
            _lblInWorkspace.Location = new Point( Width / 2 + middleSpaceX, 0 );

            _edtFind.Location = new Point( 0, 20 );
            _edtFind.Size = new Size( listWidth, 20 );

            _lvAvailable.Location = new Point( 0, 44 );
            _lvAvailable.Size = new Size( listWidth, Height - 44 );

            _lvInWorkspace.Location = new Point( Width / 2 + middleSpaceX, 20 );
            _lvInWorkspace.Size = new Size( listWidth, Height - 20 );
 
            Size btnSize = new Size( (int) (72 * Core.ScaleFactor.Width),
                (int) (24 * Core.ScaleFactor.Height) );
            int btnX = (int) (Width / 2 - (36 * Core.ScaleFactor.Width ));
            _btnAdd.Size = btnSize;
            _btnAdd.Location = new Point( btnX, 24 );
            _btnRemove.Location = new Point( btnX, 56 );
            _btnRemove.Size = btnSize;

            _lblProcessing.Location = new Point( btnX, 88 );
        }

        private class TreeFilterJetWrapper: IJetListViewNodeFilter
        {
            private IResourceNodeFilter _filter;
            public event EventHandler FilterChanged;

            public TreeFilterJetWrapper( IResourceNodeFilter filter )
            {
                _filter = filter;
            }

            public bool AcceptNode( JetListViewNode node )
            {
                IResource res = (IResource)node.Data;
                return _filter.AcceptNode( res, 0 );
            }
        }
    }
}
