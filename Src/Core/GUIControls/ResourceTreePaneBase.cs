// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using GUIControls.Controls;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Implementation of IResourceTreePane based on JetListView. Does not provide any workspace
	/// filtering behavior.
	/// </summary>
	public class ResourceTreePaneBase: AbstractViewPane, IResourceTreePane, IContextProvider, ICommandProcessor, IColorSchemeable
	{
        private ToolStrip _toolBar;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

	    protected ResourceListView2 _resourceTree;
        private readonly ToolbarActionManager _toolbarManager;
        private readonly UnreadNodeDecorator _unreadDecorator;
        private readonly RichTextColumn _textColumn;
	    protected ResourceTreeDataProvider _dataProvider;
	    protected int _parentProperty = -1;
	    protected IResource _rootResource;
        private string _rootResourceType;
	    protected string[] _workspaceFilterTypes;
        private ResourceToolTipCallback _resourceToolTipCallback;
        private bool _populated = false;
        private ColorScheme _colorScheme;

	    public ResourceTreePaneBase()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

		    _resourceTree = new ResourceListView2();
		    _resourceTree.Dock = DockStyle.Fill;
            _resourceTree.BorderStyle = BorderStyle.None;
            _resourceTree.ContextProvider = this;

	        TreeStructureColumn treeStructureColumn = new TreeStructureColumn();
            treeStructureColumn.Width = 20;
	        _resourceTree.Columns.Add( treeStructureColumn );
            _resourceTree.Columns.Add( new ResourceIconColumn() );

            _resourceTree.JetListView.KeyDown += HandleResourceTreeKeyDown;
            _resourceTree.JetListView.ActiveNodeChanged += HandleActiveNodeChanged;
            _resourceTree.KeyNavigationCompleted += HandleKeyNavigationCompleted;

            _textColumn = new RichTextColumn();
            _textColumn.SizeToContent = true;
            _textColumn.ItemToolTipCallback = HandleToolTipCallback;
            _resourceTree.Columns.Add( _textColumn );

            Controls.Add( _resourceTree );
            Controls.SetChildIndex( _resourceTree, 0 );

            _toolbarManager = new ToolbarActionManager( _toolBar );
            _toolbarManager.ContextProvider = this;

            _dataProvider = new ResourceTreeDataProvider();

            SetStyle( ControlStyles.Selectable, false );

            UnreadManager unreadManager = (UnreadManager) Core.UnreadManager;
            if ( unreadManager.Enabled )
            {
                _unreadDecorator = new UnreadNodeDecorator();
                _textColumn.AddNodeDecorator( _unreadDecorator );
            }
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
            this._toolBar = new ToolStrip();
            this.SuspendLayout();
            //
            // _toolBar
            //
            this._toolBar.Location = new System.Drawing.Point(0, 0);
            this._toolBar.Name = "_toolBar";
            this._toolBar.Size = new System.Drawing.Size(150, 32);
            this._toolBar.TabIndex = 2;
            this._toolBar.Renderer = new GradientRenderer( Color.White, SystemColors.ControlDark );
            //
            // JetResourceTreePane
            //
            this.Controls.Add(this._toolBar);
            this.Name = "JetResourceTreePane";
            this.ResumeLayout(false);

        }
		#endregion

        public ResourceListView2 ResourceTree
        {
            get { return _resourceTree; }
        }

        public IResource RootResource
        {
        	get { return _rootResource; }
        	set { _rootResource = value; }
        }

        public ResourceTreeDataProvider DataProvider
        {
            get { return _dataProvider; }
        }

		/// <summary>
        /// Specifies that the root of the pane is the root resource registered for the
        /// specified resource type, and that when the pane is in workspace mode, it should
        /// show only resources of that type connected to the workspace.
        /// </summary>
        [DefaultValue(null)]
        public string RootResourceType
        {
            get { return _rootResourceType; }
            set
            {
                _rootResourceType = value;
                if ( _rootResourceType != null )
                {
                    _rootResource = Core.ResourceTreeManager.GetRootForType( _rootResourceType );
                    _workspaceFilterTypes = new[] { _rootResourceType };
                }
            }
        }

        [DefaultValue(-1)]
        public int ParentProperty
        {
            get { return _parentProperty; }
            set { _parentProperty = value; }
        }

        /**
         * State of unread counters used by the unread decorator.
         */

        public UnreadState UnreadState
        {
            get { return _unreadDecorator.UnreadState; }
            set
            {
                if ( _unreadDecorator != null )
                {
                    _unreadDecorator.UnreadState = value;
                    _textColumn.InvalidateRichText();
                }
            }
        }

        public override void Populate()
	    {
            _populated = true;
//            if ( _toolBar.Buttons.Count == 0 )
            if ( _toolBar.Items.Count == 0 )
            {
                _toolBar.Visible = false;
            }

            if ( _unreadDecorator != null )
            {
                _unreadDecorator.UnreadState = (Core.UnreadManager as UnreadManager).CurrentUnreadState;
            }
            if ( _parentProperty == -1 )
            {
                _parentProperty = Core.Props.Parent;
            }
            if ( RootResource == null )
            {
                RootResource = Core.ResourceTreeManager.ResourceTreeRoot;
            }

            _resourceTree.OpenProperty = Core.Props.Open;
            _dataProvider.SetRootResource( _rootResource, _parentProperty );
            _resourceTree.DataProvider = _dataProvider;
        }

	    public void RegisterToolbarAction( IAction action, Icon icon, string text, string tooltip, IActionStateFilter[] filters )
	    {
            _toolBar.Visible = true;
            _toolbarManager.RegisterAction( action, null, ListAnchor.Last, icon, text, tooltip, null, filters );
        }

	    public void RegisterToolbarAction( IAction action, Image icon, string text, string tooltip, IActionStateFilter[] filters )
	    {
            _toolBar.Visible = true;
            _toolbarManager.RegisterAction( action, null, ListAnchor.Last, icon, text, tooltip, null, filters );
        }

	    public void AddNodeFilter( IResourceNodeFilter nodeFilter )
	    {
	        _resourceTree.Filters.Add( new ResourceNodeFilterAdapter( nodeFilter ) );
	    }

	    public void UpdateNodeFilter( bool keepSelection )
	    {
            _resourceTree.Filters.Update();
	    }

	    public void SelectResource( IResource res )
	    {
            if ( !_populated )
            {
                return;
            }
	        _dataProvider.SelectResource( res );
            UpdateSelection();
	    }

	    public override bool SelectResource( IResource resource, bool highlightOnly )
	    {
            if ( !_populated )
            {
                return false;
            }
            bool result = _dataProvider.SelectResource( resource );
            if ( result && !highlightOnly )
            {
                UpdateSelection();
            }
            return result;
	    }

	    public void EditResourceLabel( IResource res )
	    {
	        if ( !_populated )
	        {
	            return;
	        }
            _resourceTree.EditResourceLabel( res );
	    }

	    public void ExpandParents( IResource res )
	    {
            // do nothing if the pane has not been populated
            if ( _parentProperty != -1 )
            {
                _dataProvider.FindResourceNode( res );
            }
	    }

	    public void EnableDropOnEmpty( IResourceUIHandler emptyDropHandler )
	    {
	        _resourceTree.EmptyDropHandler = new DragDropHandlerAdapter( emptyDropHandler );
	    }

	    public void EnableDropOnEmpty( IResourceDragDropHandler emptyDropHandler )
	    {
	        _resourceTree.EmptyDropHandler = emptyDropHandler;
	    }

	    public override IResource SelectedResource
	    {
	        get { return SelectedNode; }
	    }

	    public IResource SelectedNode
	    {
	        get
	        {
	            JetListViewNode node = _resourceTree.Selection.ActiveNode;
                if ( node == null )
                {
                    return null;
                }
                return (IResource) node.Data;
	        }
	    }

	    public override bool ShowSelection
	    {
	        get { return !_resourceTree.HideSelection; }
	        set { _resourceTree.HideSelection = !value; }
	    }

	    [DefaultValue( false )]
	    public bool SelectAddedItems
	    {
	        get { return _resourceTree.SelectAddedItems; }
	        set { _resourceTree.SelectAddedItems = value; }
	    }

        public string[] WorkspaceFilterTypes
        {
            get { return _workspaceFilterTypes; }
            set { _workspaceFilterTypes = value; }
        }

	    public ResourceToolTipCallback ToolTipCallback
	    {
	        get { return _resourceToolTipCallback; }
	        set { _resourceToolTipCallback = value; }
	    }

	    public IActionContext GetContext( ActionContextKind kind )
	    {
	        ActionContext context = new ActionContext( kind, this, _resourceTree.GetSelectedResources() );
            context.SetCommandProcessor( this );
	        return context;
	    }

	    public void AddNodeDecorator( IResourceNodeDecorator decorator )
	    {
	        _textColumn.AddNodeDecorator( decorator );
	    }

        /// <summary>
        /// Method is supposed to control the order of decorators activation in
        /// more flexible manner, due to the fixed (previously) position of
        /// <c>UnreadNodeDecorator</c> handler.
        /// </summary>
	    public void InsertNodeDecorator( IResourceNodeDecorator decorator, int pos )
        {
            _textColumn.InsertNodeDecorator( decorator, pos );
	    }

        private void HandleActiveNodeChanged( object sender, JetListViewNodeEventArgs e )
        {
            AsyncUpdateSelection();
        }

	    protected override void OnEnter( EventArgs e )
	    {
	        base.OnEnter( e );
            AsyncUpdateSelection();
        }

	    public override void AsyncUpdateSelection()
	    {
            Core.UIManager.QueueUIJob( new MethodInvoker( LazyUpdateSelection ) );
        }

	    private void LazyUpdateSelection()
        {
            if ( Core.LeftSidebar.GetPane( Core.LeftSidebar.ActivePaneId ) != this )
            {
                return;
            }
            if ( Core.ResourceBrowser.OwnerResource != _resourceTree.ActiveResource ||
                Core.ResourceBrowser.LastFilterResourceList != Core.ResourceBrowser.FilterResourceList ||
                Core.ResourceBrowser.WebPageMode )
            {
                UpdateSelection();
            }
        }

	    public override void UpdateSelection()
        {
            IResource res = _resourceTree.ActiveResource;
            if ( res != null && !res.IsDeleted && Core.State != CoreState.ShuttingDown &&
                !_resourceTree.KeyNavigation )
            {
                IResourceUIHandler treeHandler = Core.PluginLoader.GetResourceUIHandler( res );
                if ( treeHandler != null )
                {
                    treeHandler.ResourceNodeSelected( res );
                }
                else
                {
                    Core.ResourceBrowser.DisplayResourceList( null, Core.ResourceStore.EmptyResourceList,
                        res.DisplayName, null );
                }
            }
            else if ( res == null && _resourceTree.VisibleItemCount == 0 )
            {
                Core.ResourceBrowser.DisplayResourceList( null, Core.ResourceStore.EmptyResourceList,
                    "No resource selected", null );
            }
            Core.UserInterfaceAP.CancelJobs( new MethodInvoker( LazyUpdateSelection ) );
        }

        private void HandleKeyNavigationCompleted( object sender, EventArgs e )
        {
            UpdateSelection();
        }

        #region Goto Prev/Next Simple/Unread
        public override bool GotoPrevView( IResource view )
	    {
            IResource res = _resourceTree.LocatePrevResource( view, MatchAny, true, true );
            if ( res != null )
            {
                // make sure synchronous selection update is performed
                ExpandParents( res );
                SelectResource( res, false );
                return true;
            }
            return false;
	    }

        public override bool GotoPrevUnreadView( IResource view )
	    {
            IResource res = _resourceTree.LocatePrevResource( view, MatchUnreadCount, true, true );
            if ( res != null )
            {
                // make sure synchronous selection update is performed
                ExpandParents( res );
                SelectResource( res, false );
                return true;
            }
            return false;
	    }

        public override bool GotoNextView( IResource view )
	    {
            IResource res = _resourceTree.LocateNextResource( view, MatchAny, true, true );
            if ( res != null )
            {
                // make sure synchronous selection update is performed
                ExpandParents( res );
                SelectResource( res, false );
                return true;
            }
            return false;
	    }

        public override bool GotoNextUnreadView( IResource view )
	    {
            IResource res = _resourceTree.LocateNextResource( view, MatchUnreadCount, true, true );
            if ( res != null )
            {
                // make sure synchronous selection update is performed
                ExpandParents( res );
                SelectResource( res, false );
                return true;
            }
            return false;
	    }

        private static bool MatchUnreadCount( IResource res )
        {
            return Core.UnreadManager.GetUnreadCount( res ) > 0;
        }

        private static bool MatchAny( IResource res )
        {
            return (res.GetLinksTo( null, Core.Props.Parent ).Count == 0);
        }
        #endregion Goto Prev/Next Simple/Unread

        private void HandleResourceTreeKeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyCode == Keys.Space )
            {
                IResource res = SelectedResource;
                if ( Core.ResourceBrowser.GotoNextUnread() || GotoNextUnreadView( res ) )
                {
                    e.Handled = true;
                    Core.ResourceBrowser.FocusResourceList();
                }
            }
        }

	    private string HandleToolTipCallback( object item )
	    {
            if ( _resourceToolTipCallback != null )
            {
                IResource res = (IResource) item;
                return _resourceToolTipCallback( res );
            }
            return null;
	    }

	    public bool CanExecuteCommand( string command )
	    {
	        return _resourceTree.CanExecuteCommand( command );
	    }

	    public void ExecuteCommand( string command )
	    {
            _resourceTree.ExecuteCommand( command );
	    }

	    public ColorScheme ColorScheme
	    {
	        get { return _colorScheme; }
	        set
	        {
	            _colorScheme = value;
/*
                _toolBar.GradientStartColor = ColorScheme.GetStartColor( _colorScheme, "Toolbar.Background", Color.White );
                _toolBar.GradientEndColor = ColorScheme.GetEndColor( _colorScheme, "Toolbar.Background", SystemColors.ControlDark );
*/
            }
	    }
	}

    internal class DragDropHandlerAdapter : IResourceDragDropHandler
    {
        private readonly IResourceUIHandler _handler;

        public DragDropHandlerAdapter( IResourceUIHandler handler )
        {
            _handler = handler;
        }

        public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
        {
        }

        public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect,
                                         int keyState )
        {
            IResourceList resList = (IResourceList) data.GetData( typeof(IResourceList) );
            if ( resList != null && resList.Count > 0 && !resList.Contains( targetResource ) )
            {
                if ( _handler.CanDropResources( targetResource, resList ) )
                {
                    return DragDropEffects.Link;
                }
            }
            return DragDropEffects.None;
        }

        public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            IResourceList resList = (IResourceList) data.GetData( typeof(IResourceList) );
            if ( resList != null && resList.Count > 0 && !resList.Contains( targetResource ) )
            {
                _handler.ResourcesDropped( targetResource, resList );
            }
        }
    }

    internal class ResourceNodeFilterAdapter: IJetListViewNodeFilter
    {
        private IResourceNodeFilter _filter;

        public ResourceNodeFilterAdapter( IResourceNodeFilter filter )
        {
            _filter = filter;
        }

        public bool AcceptNode( JetListViewNode node )
        {
            return _filter.AcceptNode( (IResource) node.Data, node.Level );
        }

        public event EventHandler FilterChanged;
    }
}
