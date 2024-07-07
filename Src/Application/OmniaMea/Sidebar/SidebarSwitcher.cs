// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
    /// Control which manages creating sidebars for each tab and showing/hiding them
    /// when tabs are switched.
	/// </summary>
    internal class SidebarSwitcher : UserControl, IContextProvider, ISidebarSwitcher
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

        private readonly Hashtable _tabSidebars = new Hashtable();             // tab ID -> VerticalSidebar
        private readonly HashSet _populatedSidebars = new HashSet();
        private readonly Hashtable _resourceStructurePanes = new Hashtable();  // tab ID -> pane ID of resource structure pane
        private VerticalSidebar _activeSidebar;
        private VerticalSidebar _newActiveSidebar;
        private bool _expanded;
        private int _expandedWidth;
        private Image _defaultPaneImage;
        private string _currentTabID;
        private ColorScheme _colorScheme;
        private readonly Hashtable _paneActivateActions = new Hashtable();    // AbstractViewPane -> IAction

		public SidebarSwitcher()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			SetStyle( ControlStyles.Selectable, false );
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
			components = new Container();
		}
		#endregion

        public event EventHandler ExpandedChanged;

        public bool Expanded
        {
            get { return _expanded; }
            set
            {
                _expanded = value;
                if ( ExpandedChanged != null )
                {
                    ExpandedChanged( this, EventArgs.Empty );
                }
            }
        }

	    public Image DefaultPaneIcon
	    {
	        get { return _defaultPaneImage; }
	        set { _defaultPaneImage = value; }
	    }

	    public int ExpandedWidth
        {
            get { return _expandedWidth; }
            set
            {
                _expandedWidth = value;
                if ( Expanded )
                {
                    Width = _expandedWidth;
                }
            }
        }

        public ColorScheme ColorScheme
        {
            get { return _colorScheme; }
            set
            {
                _colorScheme = value;
                foreach( VerticalSidebar ctl in Controls )
                {
                    ctl.ColorScheme = _colorScheme;
                }
            }
        }

        protected override void OnSizeChanged( EventArgs e )
        {
            base.OnSizeChanged( e );
            VerticalSidebar sidebar = _newActiveSidebar ?? _activeSidebar;
            if ( sidebar != null && sidebar.Expanded )
            {
                ExpandedWidth = Width;
            }
        }

        /**
         * Registers a tab and creates a sidebar for it.
         */

	    public void RegisterTab( string tabId, string[] resourceTypes, int linkType )
        {
            VerticalSidebar sidebar = new VerticalSidebar();
            sidebar.Dock = DockStyle.Fill;
            sidebar.Side = SidebarSide.Left;
            sidebar.ColorScheme = _colorScheme;
            sidebar.Visible = false;
            Controls.Add( sidebar );
			sidebar.Size = ClientSize;
            _tabSidebars [tabId] = sidebar;

            CreateDefaultPane( tabId, resourceTypes, linkType );
        }

        /**
         * Creates a default view pane for the specified tab ID.
         */

        private void CreateDefaultPane( string tabId, string[] tabResourceTypes, int tabLinkType )
        {
            ViewsCategoriesPane pane = new ViewsCategoriesPane();
            pane.RootResource = Core.ResourceTreeManager.ResourceTreeRoot;

            ContentTypeFilter filter = new ContentTypeFilter();
            if ( tabResourceTypes != null )
            {
                string[] filterTypes = new string [tabResourceTypes.Length+1];
                filterTypes [0] = "WorkspaceOtherView";
                Array.Copy( tabResourceTypes, 0, filterTypes, 1, tabResourceTypes.Length );
                filter.SetFilter( filterTypes, tabLinkType );
            }
            else
            {
                filter.SetFilter( tabResourceTypes, tabLinkType );
            }
            pane.AddContentTypes( tabResourceTypes );
            pane.AddNodeFilter( filter );
            pane.ShowWorkspaceOtherView = true;

            AddPane( tabId, StandardViewPanes.ViewsCategories, pane, "Views and Categories", _defaultPaneImage );
        }

        /**
         * Registers an activate action for the specified viewpane.
         */

	    public void RegisterActivateAction( string tabID, string paneID, string caption )
        {
            if ( _paneActivateActions.ContainsKey( paneID ) )
            {
                return;
            }

            IAction activateAction = new ActivateViewPaneAction( tabID, paneID );
            // order of actions matches the order of tabs
            Core.ActionManager.RegisterMainMenuAction( activateAction, ActionGroups.VIEW_VIEWPANE_ACTIONS,
                                                       ListAnchor.Last, caption, null, null, null );
            _paneActivateActions [paneID] = activateAction;
        }

        /**
         * Registers a view pane that will be shown in the sidebar.
         */

        public void RegisterViewPane( string paneID, string tabID, string caption, Image icon, AbstractViewPane viewPane )
        {
            RegisterActivateAction( tabID, paneID, caption );
            AddPane( tabID, paneID, viewPane, caption, icon );
        }

        /**
         * Register a view pane that displays a portion of the resource tree.
         */

        public IResourceTreePane RegisterTreeViewPane( string paneID, string tabName, string caption, Image icon, IResource rootResource )
        {
            JetResourceTreePane pane = new JetResourceTreePane();
            pane.RootResource = rootResource;
            RegisterViewPane( paneID, tabName, caption, icon, pane );
            return pane;
        }

        /**
         * Registers a resource pane that will display the resource structure (and will
         * be combined with the "Views and Categories" pane) on the specified resource type tab.
         */

        public void RegisterResourceStructurePane( string paneId, string tabId, string caption,
                                                   Image icon, AbstractViewPane viewPane )
        {
            RegisterActivateAction( tabId, paneId, caption );
            AddPane( tabId, paneId, viewPane, caption, icon );
            RegisterResourceStructurePane( tabId, paneId );

            if ( viewPane is JetResourceTreePane )
            {
                ViewsCategoriesPane viewsCategoriesPane = (ViewsCategoriesPane) GetPane( tabId, StandardViewPanes.ViewsCategories );
                viewsCategoriesPane.ShowWorkspaceOtherView = false;
            }
        }

        /**
         * Registers a resource structure pane based on a ResourceTreeView.
         */

        public IResourceTreePane RegisterResourceStructureTreePane( string paneId, string tabId, string caption,
                                                                    Image icon, IResource rootResource )
        {
            JetResourceTreePane pane = new JetResourceTreePane();
            pane.RootResource = rootResource;
            pane.RootResourceType = null;
            RegisterResourceStructurePane( paneId, tabId, caption, icon, pane );

            ViewsCategoriesPane viewsCategoriesPane = (ViewsCategoriesPane) GetPane( tabId, StandardViewPanes.ViewsCategories );
            viewsCategoriesPane.ShowWorkspaceOtherView = false;

            return pane;
        }

        /**
         * Registers a resource structure pane which has the root of the specified type
         * as the root of the ResourceTreeView.
         */

        public IResourceTreePane RegisterResourceStructureTreePane( string paneId, string tabId, string caption,
                                                                    Image icon, string rootResType )
        {
            JetResourceTreePane treePane = (JetResourceTreePane) RegisterResourceStructureTreePane( paneId, tabId, caption, icon,
                                                                 Core.ResourceTreeManager.GetRootForType( rootResType ) );
            treePane.RootResourceType = rootResType;
            return treePane;
        }

        /**
         * Sets the keyboard activation shortcut for the specified pane.
         */

        public void RegisterViewPaneShortcut( string paneID, Keys shortcut )
        {
            IAction action = (IAction) _paneActivateActions [paneID];
            if ( action != null )
            {
                Core.ActionManager.RegisterKeyboardAction( action, shortcut, null, null );
            }
        }

        /**
         * Registers a pane to be shown in the sidebar for the specified tab.
         */

        public void AddPane( string tabID, string paneID, AbstractViewPane viewPane, string caption, Image icon )
        {
            VerticalSidebar sidebar = GetSidebar( tabID );
            sidebar.RegisterPane( viewPane, paneID, caption, icon );
        }

        /**
         * Registers the specified pane as the resource structure pane for the
         * specified tab.
         */

        public void RegisterResourceStructurePane( string tabId, string paneId )
        {
            _resourceStructurePanes [tabId] = paneId;
        }

        /**
         * Returns the ID of the resource structure pane registered for the specified tab.
         */

        public string GetResourceStructurePaneId( string tabID )
        {
            return (string) _resourceStructurePanes [tabID];
        }

        /**
         * Returns the default pane for the current tab.
         */

        public IResourceTreePane DefaultViewPane
        {
            get
            {
                if ( _activeSidebar == null )
                    return null;

                return (IResourceTreePane) _activeSidebar.GetPane( StandardViewPanes.ViewsCategories );
            }
        }

        /**
         * Returns the resource structure pane for the current tab.
         */

        public AbstractViewPane ResourceStructurePane
        {
            get
            {
                string paneID = GetResourceStructurePaneId( _currentTabID );
                if ( paneID == null )
                    return null;

                return _activeSidebar.GetPane( paneID );
            }
        }

        /**
         * Returns the pane with the specified ID from the current sidebar.
         */

        public AbstractViewPane GetPane( string paneID )
        {
            if ( _activeSidebar == null )
            {
                return null;
            }
            return _activeSidebar.GetPane( paneID );
        }

        public AbstractViewPane GetPane( string tabId, string paneId )
        {
            return GetSidebar( tabId ).GetPane( paneId );
        }

        internal SidebarState CurrentState
        {
            get { return _activeSidebar.CurrentState; }
            set { _activeSidebar.CurrentState = value; }
        }

        public VerticalSidebar ActiveSidebar
        {
            get { return _activeSidebar; }
        }

        /**
         * Returns the ID of the currently active view pane.
         */

        public string ActivePaneId
        {
            get
            {
                if ( _activeSidebar == null )
                {
                    return null;
                }
                return _activeSidebar.ActivePaneId;
            }
        }

        /**
         * Ensures that the view pane with the specified ID is visible and sets the focus to it.
         */

        public void ActivateViewPane( string paneId )
        {
            if ( _activeSidebar != null )
            {
                if ( !_activeSidebar.ContainsPane( paneId ) )
                {
                    throw new ArgumentException( "Pane ID " + paneId + " not found in sidebar of tab " + GetSidebarTab( _activeSidebar ) );
                }
                _activeSidebar.ActivatePane( paneId );
            }
        }

	    public AbstractViewPane ActivateViewPane( string tabId, string paneId )
	    {
	        VerticalSidebar sidebar = _activeSidebar;
            if ( sidebar == null || sidebar != _tabSidebars [tabId] )
            {
                return null;
            }

            AbstractViewPane pane = sidebar.GetPane( paneId );
            if ( pane == null )
            {
                return null;
            }

            sidebar.ActivatePane( paneId );
            if ( _activeSidebar != sidebar )
            {
                // one more check for tab switch caused by message processing during activation
                return null;
            }
            return pane;
        }

	    /**
         * Returns the sidebar instance for the specified tab.
         */

        private VerticalSidebar GetSidebar( string tabId )
        {
            VerticalSidebar sidebar = (VerticalSidebar) _tabSidebars [tabId];
            if ( sidebar == null )
            {
                throw new ArgumentException( "Invalid tab ID " + tabId, "tabId" );
            }
            return sidebar;
        }

        /**
         * Returns the tab ID for the specified sidebar instance.
         */

        private string GetSidebarTab( VerticalSidebar sidebar )
        {
            foreach( DictionaryEntry de in _tabSidebars )
            {
                if ( de.Value == sidebar )
                {
                    return (string) de.Key;
                }
            }
            return "<unknown>";
        }

        /// <summary>
        /// Shows the sidebar for the specified tab.
        /// </summary>
        public void ShowPanesForTab( string tabId, SidebarState state )
        {
            _currentTabID = tabId;
            Core.UIManager.BeginUpdateSidebar();

            VerticalSidebar sidebar = GetSidebar( tabId );
            if ( sidebar != _activeSidebar )
            {
                _newActiveSidebar = sidebar;
                if ( !_populatedSidebars.Contains( tabId ) )
                {
                    _populatedSidebars.Add( tabId );
                    sidebar.PopulateViewPanes();

                    if ( state == null )
                    {
                        sidebar.BeginUpdate();
                        AbstractViewPane pane = sidebar.GetPane( StandardViewPanes.ViewsCategories );
                        if ( pane != null )
                        {
                            sidebar.ActivatePane( StandardViewPanes.ViewsCategories );
                        }

                        string structurePaneID = (string) _resourceStructurePanes [tabId];
                        if ( structurePaneID != null )
                        {
                            sidebar.ActivatePane( structurePaneID );
                        }
                        sidebar.EndUpdate();
                    }
                }
                sidebar.UpdateActiveWorkspace();
                if ( state != null )
                {
                    sidebar.CurrentState = state;
                }

                if ( _activeSidebar != null )
                {
                    _activeSidebar.ExpandedChanged -= OnActiveSidebarExpandedChanged;
                }
                sidebar.ExpandedChanged += OnActiveSidebarExpandedChanged;
                Expanded = sidebar.Expanded;
//                Width = Expanded ? ExpandedWidth : sidebar.CollapsedWidth;
                Width = Expanded ? ExpandedWidth : 0;
            }
            else
            {
                sidebar.UpdateActiveWorkspace();
                if ( state != null )
                {
                    sidebar.CurrentState = state;
                }
            }

            if ( sidebar != _activeSidebar )
            {
                sidebar.Visible = true;
                if ( _activeSidebar != null )
                {
                    _activeSidebar.Visible = false;
                }
                _activeSidebar = sidebar;
                if ( ContainsFocus )
                {
                    _activeSidebar.FocusActivePane();
                }
            }

            Core.UIManager.EndUpdateSidebar();
            _newActiveSidebar = null;
        }

        /**
         * Adjusts the width of the sidebar switcher when the active sidebar is
         * expanded or collapsed.
         */

        private void OnActiveSidebarExpandedChanged( object sender, EventArgs e )
        {
            if ( !_activeSidebar.Expanded )
            {
                ExpandedWidth = Width;
            }
            Expanded = _activeSidebar.Expanded;
//            Width = Expanded ? ExpandedWidth : _activeSidebar.CollapsedWidth;
            Width = Expanded ? ExpandedWidth : 0;
        }

	    public IActionContext GetContext( ActionContextKind kind )
	    {
	        if ( _activeSidebar != null )
	        {
	            return _activeSidebar.GetContext( kind );
	        }
            return null;
	    }
	}

    /// <summary>
    /// Action to activate the specified view pane.
    /// </summary>
    public class ActivateViewPaneAction: SimpleAction
    {
        private readonly string _tabId;
        private readonly string _paneId;

        internal ActivateViewPaneAction( string tabID, string paneID )
        {
            _tabId = tabID;
            _paneId = paneID;
        }

        public override void Execute( IActionContext context )
        {
            Core.UIManager.LeftSidebarExpanded = true;
            if ( Core.LeftSidebar.GetPane( _paneId ) == null && _tabId != null )
            {
                if ( !Core.TabManager.ActivateTab( _tabId ) )
                {
                    return;
                }
            }
            if ( _tabId != null )
            {
                Core.LeftSidebar.ActivateViewPane( _tabId, _paneId );
            }
            else
            {
                Core.LeftSidebar.ActivateViewPane( _paneId );
            }
        }
    }
}
