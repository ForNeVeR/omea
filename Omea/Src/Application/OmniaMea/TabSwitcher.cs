// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.CommandBar;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
	/// <summary>
	/// The control that manages switching between resource type tabs.
	/// </summary>
	/// <remarks>
	/// The <see cref="ICommandBar"/> and <see cref="ICommandBarSite"/> interface implementations are just forwarders
	/// between the site of this command bar and the underlying <see cref="TabBar"/> control
	/// that rendsers the tabs.
	/// </remarks>
	internal class TabSwitcher : UserControl, ITabManager, ICommandBar, ICommandBarSite
	{
		#region TabFilter Internal Class

		internal class TabFilter : IResourceTypeTab
		{
			private TabSwitcher _owner;

			private string _tabID;

			private string[] _resTypes;

			private int _linkPropId;

			internal TabFilter( TabSwitcher owner, string tabID, string[] resTypes, int linkPropID )
			{
				_owner = owner;
				_tabID = tabID;
				_resTypes = resTypes;
				_linkPropId = linkPropID;
			}

			public string Id
			{
				get { return _tabID; }
			}

			public string Name
			{
				get { return _owner.GetTabName( _tabID ); }
			}

			public string[] GetResourceTypes()
			{
				return _resTypes;
			}

			public int LinkPropId
			{
				get { return _linkPropId; }
			}

			internal void SetLinkPropID( int linkPropID )
			{
				_linkPropId = linkPropID;
			}

			internal void AddResourceTypes( string[] resTypes )
			{
				if( _resTypes == null )
					_resTypes = resTypes;
				else
				{
					ArrayList newTypes = new ArrayList( _resTypes );
					foreach( string resType in resTypes )
					{
						if( !newTypes.Contains( resType ) )
						{
							newTypes.Add( resType );
						}
					}
					_resTypes = (string[]) newTypes.ToArray( typeof( string ) );
				}
			}

			/// <summary>
			/// Checks if the specified resource type is encountered in the source type list of a tab.
			/// </summary>
			internal bool ContainsResourceType( string type )
			{
				if( _resTypes != null )
				{
					for( int i = 0; i < _resTypes.Length; i++ )
					{
						if( String.Compare( _resTypes[ i ], type, true ) == 0 )
							return true;
					}
				}
				return false;
			}

			public IResourceList GetFilterList( bool live )
			{
				return GetFilterList( live, true );
			}

			public IResourceList GetFilterList( bool live, bool includeFragments )
			{
				if( _resTypes == null )
					return null;

				IResourceList list = live
					? Core.ResourceStore.GetAllResourcesLive( _resTypes )
					: Core.ResourceStore.GetAllResources( _resTypes );
				IResourceList fragmentList = null;
				SelectionType selType = live ? SelectionType.Live : SelectionType.Normal;

				if( includeFragments )
				{
					for( int i = 0; i < _resTypes.Length; i++ )
					{
                        IResourceList foundList = Core.ResourceStore.FindResources( selType, null,
                            "ContentType", _resTypes[ i ]) ;
                        if ( fragmentList == null )
                        {
                            fragmentList = foundList;
                        }
                        else
                        {
                            fragmentList = fragmentList.Union( foundList, true );
                        }
					}
                    if ( fragmentList != null )
                    {
                        fragmentList = fragmentList.Intersect( Core.ResourceStore.GetAllResources( "Fragment" ), true );
                    }
                    list = list.Union( fragmentList );
				}

				if( _linkPropId != -1 )
				{
					IResourceList fileResourceTypes = Core.ResourceStore.GetAllResources( ResourceTypeHelper.GetFileResourceTypes() );
					fileResourceTypes = fileResourceTypes.Intersect(
                        Core.ResourceStore.FindResourcesWithProp( selType, null, _linkPropId ), true );
					list = list.Union( fileResourceTypes, true );

					if( includeFragments )
					{
						list = list.Union( Core.ResourceStore.FindResources( "Fragment", "ContentLinks",
                            Core.ResourceStore.PropTypes[ _linkPropId ].Name ), true );
					}
				}
				return list;
			}
		}

		#endregion

		#region TabState Internal Class

		internal class TabState
		{
			private SidebarState _sidebarState;

			internal SidebarState SidebarState
			{
				get { return _sidebarState; }
				set { _sidebarState = value; }
			}
		}

		#endregion

		#region ResourceTypeTabCollection Internal Class

		internal class ResourceTypeTabCollection : IResourceTypeTabCollection
		{
			private readonly TabBar _tabBar;

			public ResourceTypeTabCollection( TabBar tabBar )
			{
				_tabBar = tabBar;
			}

			public int Count
			{
				get { return _tabBar.TabCount; }
			}

			public IResourceTypeTab this[ int index ]
			{
				get { return (IResourceTypeTab) _tabBar.GetTabTag( index ); }
			}

			public IResourceTypeTab this[ string tabId ]
			{
				get
				{
					for( int i = 0; i < _tabBar.TabCount; i++ )
					{
						TabFilter tabFilter = (TabFilter) _tabBar.GetTabTag( i );
						if( tabFilter.Id == tabId )
						{
							return tabFilter;
						}
					}
					throw new ArgumentException( "Tab '" + tabId + "' not found ", "tabId" );
				}
			}
		}

		#endregion

		#region Data

		/// <summary>
		/// Maintains and displays the tabs list.
		/// </summary>
		private TabBar _tabBar;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		private Hashtable _resourceTabOrder = new Hashtable();

		private TabFilter _curTabFilter;

		private TabState _curTabState = null;

		private Hashtable _tabStates = new Hashtable(); // WorkspaceTabState -> TabState
		private IntHashTableOfInt _workspaceTabs = new IntHashTableOfInt(); // workspace ID -> tab index
		private string _defaultPaneCaption;

		private Hashtable _defaultSelectedResources = new Hashtable(); // tab name -> IResource

		private SidebarSwitcher _querySidebar;

		private ResourceBrowser _resourceBrowser;

		private bool _startupComplete;

		private bool _selectingLastTab;

		public event EventHandler TabChanging;

		public event EventHandler TabChanged;

		private IResource _defaultViewResource = null;

		private UnreadManager _unreadManager;

		private UnreadState _curUnreadState;

		private IResource _activeWorkspace;

		private IResourceList _activeWorkspaceFilterList;

		private IResourceList _activeWorkspaceWatchList;

		private ResourceTypeTabCollection _tabCollection;

		/// <summary>
		/// Command bar site.
		/// </summary>
		private ICommandBarSite _site;

		#endregion

		#region Construction

		public TabSwitcher()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			SetStyle( ControlStyles.AllPaintingInWmPaint
				| ControlStyles.CacheText
				| ControlStyles.UserPaint
				| ControlStyles.Opaque
			          , true );

			//tabPage1.Tag = new TabFilter( "All", null, -1 );
			_tabBar.AddTab( "All Resources", new TabFilter( this, "All", null, -1 ) );

			_tabCollection = new ResourceTypeTabCollection( _tabBar );

            Core.StateChanged += CoreStateChanged;
		}

	    private void CoreStateChanged( object sender, EventArgs e )
	    {
            if( Core.State == CoreState.Running )
            {
                Core.StateChanged -= CoreStateChanged;
                if( _tabBar.TabCount < 3 )
                {
                    Dispose();
                }
            }
	    }

	    /// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this._tabBar = new TabBar();
			this._tabBar.SuspendLayout();
			this.SuspendLayout();
			//
			// _tabControl
			//
			this._tabBar.Dock = System.Windows.Forms.DockStyle.Fill;
            //  Workaround fix for OMs:12240, 13178, 13119, 13100, 13066, 12265, etc.
            //  This code should be substed when a better solution will be found
            try
            {
    			this._tabBar.Font = new System.Drawing.Font( "Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte) (204)) );
            }
            catch( System.ArgumentException )
		    {
    			this._tabBar.Font = new System.Drawing.Font( "Tahoma", 9.0F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte) (204)) );
		    }
			this._tabBar.Location = new System.Drawing.Point( 0, 0 );
			this._tabBar.Name = "_tabBar";
			this._tabBar.Size = new System.Drawing.Size( 150, 150 );
			this._tabBar.TabIndex = 0;
			this._tabBar.SelectedIndexChanged += new System.EventHandler( this.OnSelectedTabChange );
			//
			// TabSwitcher
			//
			this.Controls.Add( this._tabBar );
			this.Name = "TabSwitcher";
			this._tabBar.ResumeLayout( false );
			this.ResumeLayout( false );
		}

		#endregion

		#region Attributes

		public IResourceTypeTabCollection Tabs
		{
			get { return _tabCollection; }
		}

		[Browsable( false )]
		public UnreadManager UnreadManager
		{
			get { return _unreadManager; }
			set { _unreadManager = value; }
		}

		/// <summary>
		/// Whether the initial tab filling process is complete.
		/// </summary>
		public bool StartupComplete
		{
			get { return _startupComplete; }
			set { _startupComplete = value; }
		}

		/// <summary>
		/// The resource displayed by default in new tabs if there is no saved resource.
		/// </summary>
		[DefaultValue( null ), Browsable( false )]
		public IResource DefaultViewResource
		{
			get { return _defaultViewResource; }
			set { _defaultViewResource = value; }
		}

		[DefaultValue( null )]
		public SidebarSwitcher QuerySidebar
		{
			get { return _querySidebar; }
			set { _querySidebar = value; }
		}

		[DefaultValue( null )]
		public ResourceBrowser ResourceBrowser
		{
			get { return _resourceBrowser; }
			set { _resourceBrowser = value; }
		}

		/// <summary>
		/// Caption of the default view pane.
		/// </summary>
		[DefaultValue( null )]
		public string DefaultPaneCaption
		{
			get { return _defaultPaneCaption; }
			set { _defaultPaneCaption = value; }
		}

		[DefaultValue( null )]
		public ColorScheme ColorScheme
		{
			get { return _tabBar.ColorScheme; }
			set { _tabBar.ColorScheme = value; }
		}

		[Browsable( false )]
		public string CurrentTabId
		{
			get
			{
				TabFilter filter = (TabFilter) _tabBar.SelectedTabTag;
				return filter.Id;
			}
			set { SelectTab( value ); }
		}

		[Browsable( false )]
		public IResourceTypeTab CurrentTab
		{
			get { return Tabs[ CurrentTabId ]; }
		}

		internal Rectangle ActiveTabRect
		{
			get
			{
				if( _tabBar.SelectedIndex < 0 )
				{
					return new Rectangle();
				}
				return _tabBar.GetTabRect( _tabBar.SelectedIndex );
			}
		}

		#endregion

		#region Operations

		/// <summary>
		/// Returns the list of resource types displayed in the specified tab, or null if
		/// all resources are displayed.
		/// </summary>
		public string[] GetTabResourceTypes( string tabId )
		{
			int tabIndex = FindTabByID( tabId );
			if( tabIndex < 0 )
				throw new ArgumentException( "Invalid tab ID " + tabId, "tabId" );

			TabFilter tabFilter = (TabFilter) _tabBar.GetTabTag( tabIndex );
			return tabFilter.GetResourceTypes();
		}

		/// <summary>
		/// Returns the link type filtering the specified tab, or null if there is no
		/// link type filter.
		/// </summary>
		public int GetTabLinkPropId( string tabId )
		{
			int tabIndex = FindTabByID( tabId );
			if( tabIndex < 0 )
				throw new ArgumentException( "Invalid tab ID " + tabId, "tabId" );

			TabFilter tabFilter = (TabFilter) _tabBar.GetTabTag( tabIndex );
			return tabFilter.LinkPropId;
		}

		public bool ActivateTab( string tabId )
		{
			// NOTE: While the setter of CurrentTabId runs, Windows messages are processed,
			// so a different tab switch can occur. Thus, the check below is not senseless
			// and must not be removed.
			CurrentTabId = tabId;
			return CurrentTabId == tabId;
		}

		public void RegisterResourceTypeTab( string tabID, string tabName, string resType, int order )
		{
			RegisterResourceTypeTab( tabID, tabName, new string[] {resType}, -1, order );
		}

		public void RegisterResourceTypeTab( string tabID, string tabName, string[] resTypes, int order )
		{
			RegisterResourceTypeTab( tabID, tabName, resTypes, -1, order );
		}

		public void RegisterResourceTypeTab( string tabID, string tabName, string[] resTypes,
		                                     int linkPropID, int order )
		{
			int tabIndex = FindTabByID( tabID );
			if( tabIndex >= 0 )
			{
				TabFilter filter = (TabFilter) _tabBar.GetTabTag( tabIndex );
				if( resTypes != null )
				{
					filter.AddResourceTypes( resTypes );
				}
				if( linkPropID != -1 )
				{
					filter.SetLinkPropID( linkPropID );
				}

				return;
			}

			AddTabWithFilter( tabName, new TabFilter( this, tabID, resTypes, linkPropID ), order );
			_querySidebar.RegisterTab( tabID, resTypes, linkPropID );
		}

		/// <summary>
		/// Specifies that the specified resource should be selected in the specified
		/// tab when it is shown for the first time.
		/// </summary>
		public void SetDefaultSelectedResource( string tabID, IResource res )
		{
			_defaultSelectedResources[ tabID ] = res;
		}

		/// <summary>
		/// Selects the tab at the specified index.
		/// </summary>
		public void SelectTab( int index )
		{
			if( _tabBar.TabCount > index )
			{
				_tabBar.SelectedIndex = index;
			}
		}

		/// <summary>
		/// Selects a tab with the specified name.
		/// </summary>
		public void SelectTab( string tabId )
		{
			if(tabId == null)
				throw new ArgumentNullException("tabId");
			int tabIndex = FindTabByID( tabId );
			if( tabIndex != -1 )
			{
				SelectTab( tabIndex );
			}
			else
			{
				throw new ArgumentException( String.Format("Invalid tab ID \"{0}\".", tabId), "tabId" );
			}
		}

		/// <summary>
		/// Selects the tab where the resources of the specified type are displayed.
		/// </summary>
		public void SelectResourceTypeTab( string resType )
		{
			string tabId = FindResourceTypeTab( resType );
			if( tabId != null )
			{
				SelectTab( tabId );
			}
			else
			{
				SelectTab( 0 );
			}
		}

		/// <summary>
		/// Selects the tab where the resources with the specified link are displayed.
		/// </summary>
		/// <param name="linkPropId"></param>
		public void SelectLinkPropTab( int linkPropId )
		{
			string tabId = FindLinkPropTab( linkPropId );
			if( tabId != null )
			{
				SelectTab( tabId );
			}
			else
			{
				SelectTab( 0 );
			}
		}

		/// <summary>
		/// Returns the ID of the tab at the specified index.
		/// </summary>
		public string GetTabId( int index )
		{
			TabFilter filter = (TabFilter) _tabBar.GetTabTag( index );
			return filter.Id;
		}

		/// <summary>
		/// Returns the name of the tab with the specified ID.
		/// </summary>
		public string GetTabName( string tabID )
		{
			int tabIndex = FindTabByID( tabID );
			if( tabIndex == -1 )
				throw new Exception( "Invalid tab ID " + tabID );
			return _tabBar.GetTabText( tabIndex );
		}

		/// <summary>
		/// Finds the tab where the resources of the specified type are displayed.
		/// </summary>
		public string FindResourceTypeTab( string type )
		{
			for( int i = 0; i < _tabBar.TabCount; i++ )
			{
				TabFilter filter = (TabFilter) _tabBar.GetTabTag( i );
				if( filter != null && filter.ContainsResourceType( type ) )
				{
					return filter.Id;
				}
			}
			return null;
		}

		/// <summary>
		/// Finds the tab where resources with the specified property are displayed.
		/// </summary>
		public string FindLinkPropTab( int linkPropId )
		{
			for( int i = 0; i < _tabBar.TabCount; i++ )
			{
				TabFilter filter = (TabFilter) _tabBar.GetTabTag( i );
				if( filter.LinkPropId == linkPropId )
				{
					return filter.Id;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns the index of a tab with the specified name.
		/// </summary>
		public int FindTabByID( string id )
		{
			for( int i = 0; i < _tabBar.TabCount; i++ )
			{
				TabFilter filter = (TabFilter) _tabBar.GetTabTag( i );
				if( filter.Id == id )
				{
					return i;
				}
			}
			return -1;
		}

		public IResourceList GetCurrentTabFilterList()
		{
			return Tabs[ CurrentTabId ].GetFilterList( true );
		}

		public int GetResourceTabOrder( string tabID )
		{
			if( tabID == null || !_resourceTabOrder.Contains( tabID ) )
				return -1;

			return (int) _resourceTabOrder[ tabID ];
		}

		public string GetResourceTab( IResource res )
		{
			string resType = res.Type;
			if( resType == "Fragment" )
			{
				resType = res.GetStringProp( "ContentType" );
			}
			// TODO: optimize
			for( int i = 1; i < _tabBar.TabCount; i++ )
			{
				TabFilter tabFilter = (TabFilter) _tabBar.GetTabTag( i );
				if( tabFilter.ContainsResourceType( resType ) )
				{
					return tabFilter.Id;
				}
				if( tabFilter.LinkPropId >= 0 && res.HasProp( tabFilter.LinkPropId ) )
				{
					return tabFilter.Id;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns the state key for the current state of the switcher.
		/// </summary>
		public WorkspaceTabState GetCurStateKey()
		{
			TabFilter filter = (TabFilter) _tabBar.SelectedTabTag;
			return new WorkspaceTabState( filter.Id, (_activeWorkspace == null) ? 0 : _activeWorkspace.Id );
		}

		/// <summary>
		/// Registers actions for switching between tabs.
		/// </summary>
		public void RegisterTabSwitchActions()
		{
			for( int i = 0; i < _tabBar.TabCount; i++ )
			{
				GoTabAction action = new GoTabAction( this, i );
				Core.ActionManager.RegisterMainMenuAction( action, ActionGroups.GO_TAB_ACTIONS, ListAnchor.Last,
				                                           _tabBar.GetTabText( i ), null, null, null );
				if ( i < 9 )
				{
                    Core.ActionManager.RegisterKeyboardAction( action, (Keys.D1 + i) | Keys.Alt, null,
                                                               new IActionStateFilter[] { new MainWindowFilter() } );
				}
			}
		}

		/// <summary>
		/// Returns the index of the tab that should be selected in the specified workspace.
		/// </summary>
		public int GetWorkspaceTab( int workspaceID )
		{
			int tabIndex = _workspaceTabs[ workspaceID ];
			if( tabIndex == _workspaceTabs.MissingKeyValue )
			{
				tabIndex = Core.SettingStore.ReadInt( "WorkspaceTabs", workspaceID.ToString(), 1 );
			}
			return tabIndex;
		}

		/// <summary>
		/// Selects the last tab which was active when the program was closed.
		/// </summary>
		public void SelectLastTab()
		{
			Core.WorkspaceManager.WorkspaceChanged += new EventHandler( OnActiveWorkspaceChanged );

			int activeWorkspaceID = Core.SettingStore.ReadInt( "WorkspaceTabs", "ActiveWorkspace", 0 );
			IResource res = null;
			if( activeWorkspaceID != 0 )
			{
				try
				{
					res = Core.ResourceStore.LoadResource( activeWorkspaceID );
					if( res.Type != "Workspace" )
					{
						res = null;
					}
				}
				catch( StorageException )
				{
					res = null;
				}
			}
			if( res != null )
			{
				_selectingLastTab = true;
				Core.WorkspaceManager.ActiveWorkspace = res;
				_selectingLastTab = false;
			}
			else
			{
				int tab = GetWorkspaceTab( 0 );
				if( tab == _tabBar.SelectedIndex || tab >= _tabBar.TabCount )
				{
					UpdateSelectedTab();
				}
				else
				{
					SelectTab( tab );
				}
			}
		}

		/// <summary>
		/// Saves the pane switcher state of all tabs to the INI file.
		/// </summary>
		public void SaveTabStates()
		{
			if( _curTabState != null )
			{
				_curTabState.SidebarState = _querySidebar.CurrentState;
			}

			int activeWorkspaceID = (_activeWorkspace == null) ? 0 : _activeWorkspace.Id;
			_workspaceTabs[ activeWorkspaceID ] = _tabBar.SelectedIndex;
			Core.SettingStore.WriteInt( "WorkspaceTabs", "ActiveWorkspace", activeWorkspaceID );

			foreach( IntHashTableOfInt.Entry e in _workspaceTabs )
			{
				Core.SettingStore.WriteInt( "WorkspaceTabs", e.Key.ToString(), e.Value );
			}

			foreach( DictionaryEntry de in _tabStates )
			{
				WorkspaceTabState stateKey = (WorkspaceTabState) de.Key;
				TabState state = (TabState) de.Value;

				if( state.SidebarState != null )
				{
					state.SidebarState.SaveToIni( stateKey.GetIniString() );
				}
			}
		}

		/// <summary>
		/// Loads the pane switcher state for all workspaces and all tabs currently present
		/// in the UI and puts data in the _tabStates hash.
		/// </summary>
		public void RestoreTabStates()
		{
			RestoreTabStatesForWorkspace( 0 );
			foreach( IResource res in Core.WorkspaceManager.GetAllWorkspaces() )
			{
				RestoreTabStatesForWorkspace( res.Id );
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Updates the tabs and sidebar when the active workspace is changed.
		/// </summary>
		private void OnActiveWorkspaceChanged( object sender, EventArgs e )
		{
            if ( Core.UserInterfaceAP.IsOwnerThread )
            {
                UpdateActiveWorkspace();
            }
            else
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( UpdateActiveWorkspace ) );
            }
		}

	    private void UpdateActiveWorkspace()
	    {
	        int oldWorkspaceID = (_activeWorkspace == null) ? 0 : _activeWorkspace.Id;
	        if( !_selectingLastTab )
	        {
	            _workspaceTabs[ oldWorkspaceID ] = _tabBar.SelectedIndex;
	        }
	        if( _activeWorkspaceWatchList != null )
	        {
	            _activeWorkspaceWatchList.Dispose();
	            _activeWorkspaceWatchList.ResourceChanged -= OnActiveWorkspaceChanged;
	            _activeWorkspaceWatchList = null;
	        }

	        _activeWorkspace = Core.WorkspaceManager.ActiveWorkspace;

	        int newWorkspaceID = (_activeWorkspace == null) ? 0 : _activeWorkspace.Id;
	        if( _activeWorkspace != null )
	        {
	            _activeWorkspaceFilterList = Core.WorkspaceManager.GetFilterList( _activeWorkspace );
	            _activeWorkspaceWatchList = _activeWorkspace.ToResourceListLive();
	            _activeWorkspaceWatchList.ResourceChanged += OnActiveWorkspaceChanged;
	        }
	        else
	        {
	            _activeWorkspaceFilterList = null;
	        }

	        int newSelectedIndex = GetWorkspaceTab( newWorkspaceID );
	        if( newSelectedIndex != _tabBar.SelectedIndex &&
	            newSelectedIndex >= 0 && newSelectedIndex < _tabBar.TabCount )
	        {
	            _tabBar.SelectedIndex = newSelectedIndex;
	        }
	        else
	        {
	            UpdateSelectedTab();
	        }
	    }

	    /// <summary>
		/// The main function for switching resource type tabs and workspaces.
		/// </summary>
		private void UpdateSelectedTab()
		{
			if( Core.State == CoreState.ShuttingDown )
			{
				return;
			}

			using( new WaitCursorDisplayer() )
			{
				if( TabChanging != null )
				{
					TabChanging( this, EventArgs.Empty );
				}

                _resourceBrowser.UnhookResourceList( null );

				Trace.WriteLine( "--- Start tab switch ---" );
				if( _curTabState != null )
				{
					_curTabState.SidebarState = _querySidebar.CurrentState;
				}

				TabFilter tabFilter = (TabFilter) _tabBar.SelectedTabTag;

				string tabText = _tabBar.GetTabText( _tabBar.SelectedIndex );
				string tabName = (_tabBar.SelectedIndex > 0) ? tabText : null;
				string[] tabResourceTypes = tabFilter.GetResourceTypes();
				string tabId = tabFilter.Id;

				Trace.WriteLine( "Setting default resource browser columns" );
				_resourceBrowser.SetCaptionPrefix( tabName, false );
				_resourceBrowser.DefaultColumns = (tabResourceTypes == null)
					? null
					: (Core.DisplayColumnManager as DisplayColumnManager).GetColumnsForTypes( tabResourceTypes );

				Trace.WriteLine( "Initializing tab state" );
				bool newState = false;
				WorkspaceTabState tabStateKey = GetCurStateKey();
				TabState state = (TabState) _tabStates[ tabStateKey ];
				if( state == null )
				{
					newState = true;
					state = new TabState();
					_tabStates[ tabStateKey ] = state;
				}

				//_resourceBrowser.ResourceListView.Filters.BeginUpdate();

				Trace.WriteLine( "Updating ResourceListView filters" );

				_curTabFilter = tabFilter;

				UpdateResourceBrowserFilterList();
				_resourceBrowser.UpdatePerTabSettings();

                Trace.WriteLine( "Showing panes in QuerySidebar" );
				_querySidebar.ShowPanesForTab( tabId, state.SidebarState );

                ResourceTreePaneBase defaultViewPane = _querySidebar.DefaultViewPane as ResourceTreePaneBase;
                if ( defaultViewPane != null )
                {
                    defaultViewPane.UnreadState = _curUnreadState;
                }
				JetResourceTreePane structurePane = _querySidebar.ResourceStructurePane as JetResourceTreePane;
				if( structurePane != null )
				{
					structurePane.UnreadState = _curUnreadState;
				}

				Trace.WriteLine( "Firing TabSwitch handler" );
				if( TabChanged != null )
				{
					TabChanged( this, EventArgs.Empty );
				}

				if( newState )
				{
					Trace.WriteLine( "Showing columns for resource list" );
					if( _resourceBrowser.DefaultColumns != null )
					{
						_resourceBrowser.ShowListViewColumns( _resourceBrowser.DefaultColumns );
					}
					else
					{
						_resourceBrowser.ShowColumnsForResourceList();
					}

					Trace.WriteLine( "Selecting default view" );
//  Debug
                    try
                    {
    					SelectDefaultView( tabId );
                    }
                    catch ( NullReferenceException )
                    {
                        throw new ApplicationException( "Illegal name of the View [" + tabId + "] while switching to [" + tabText + "]");
                    }
//  endDebug
				}
				else
				{
					Trace.WriteLine( "Selecting default view" );
					if( state.SidebarState != null &&
						state.SidebarState.SelectedResource == null )
					{
//  Debug
                        try
                        {
    					    SelectDefaultView( tabId );
                        }
                        catch ( NullReferenceException )
                        {
                            throw new ApplicationException( "Illegal name of the View [" + tabId + "] while switching to [" + tabText + "]");
                        }
//  endDebug
					}
				}
				Trace.WriteLine( "Ending filters update" );
				//_resourceBrowser.ResourceListView.Filters.EndUpdate();
				_curTabState = state;
				Trace.WriteLine( "--- Done tab switch ---" );
			}
		}

		private void AddTabWithFilter( string tabName, TabFilter filter, int order )
		{
			int insertIndex = _tabBar.TabCount;
			if( _tabBar.TabCount > 0 )
			{
				// All Resources is always the first tab
				for( int i = 1; i < _tabBar.TabCount; i++ )
				{
					string oldTabID = GetTabId( i );
					int oldOrder = 0;
					if( _resourceTabOrder.Contains( oldTabID ) )
					{
						oldOrder = (int) _resourceTabOrder[ oldTabID ];
					}
					if( oldOrder > order )
					{
						insertIndex = i;
						break;
					}
				}
			}
			_tabBar.InsertTab( insertIndex, tabName, filter );
			_resourceTabOrder[ filter.Id ] = order;
		}

		/// <summary>
		/// When a tab is switched, updates the selected tab.
		/// </summary>
		private void OnSelectedTabChange( object sender, EventArgs e )
		{
			if( !_startupComplete )
				return;

			UpdateSelectedTab();
		}

		/// <summary>
		/// Update the filter list based on the active tab and workspace.
		/// </summary>
		private void UpdateResourceBrowserFilterList()
		{
			IResourceList tabFilterList = null;
			if( _curTabFilter != null )
			{
				tabFilterList = _curTabFilter.GetFilterList( true );
			}
			if( _activeWorkspaceFilterList != null )
			{
				Trace.WriteLine( "Intersecting tab filter list with workspace filter list" );
                tabFilterList = _activeWorkspaceFilterList.Intersect( tabFilterList );
			}
			_resourceBrowser.SetFilterResourceList( tabFilterList );
			_curUnreadState = _unreadManager.SetUnreadState( CurrentTabId, _activeWorkspace );
		}

		/// <summary>
		/// When the active workspace is changed, updates the filter list.
		/// </summary>
		private void OnActiveWorkspaceChanged( object sender, ResourcePropIndexEventArgs e )
		{
		    if ( Core.UserInterfaceAP.IsOwnerThread )
		    {
                ProcessActiveWorkspaceChange();
		    }
            else
		    {
		        Core.UIManager.QueueUIJob( new MethodInvoker( ProcessActiveWorkspaceChange ) );
		    }
		}

	    private void ProcessActiveWorkspaceChange()
	    {
	        _activeWorkspaceFilterList = Core.WorkspaceManager.GetFilterList( _activeWorkspace );
	        UpdateResourceBrowserFilterList();
	    }

	    /// <summary>
		/// Loads the pane switcher state for all tabs in the specified workspace.
		/// </summary>
		private void RestoreTabStatesForWorkspace( int workspaceID )
		{
			string workspaceText = (workspaceID == 0) ? "" : workspaceID + ".";
			for( int i = 0; i < _tabBar.TabCount; i++ )
			{
				TabFilter filter = (TabFilter) _tabBar.GetTabTag( i );
				SidebarState state = SidebarState.RestoreFromIni( "TabState." + workspaceText + filter.Id );
				if( state != null )
				{
					TabState tabState = new TabState();
					tabState.SidebarState = state;

					WorkspaceTabState stateKey = new WorkspaceTabState( filter.Id, workspaceID );
					_tabStates[ stateKey ] = tabState;
				}
			}
		}

		/// <summary>
		/// Selects the default resource for the specified tab.
		/// </summary>
		private void SelectDefaultView( string tabID )
		{
			AbstractViewPane defaultPane = _querySidebar.DefaultViewPane as AbstractViewPane;

			IResource res = (IResource) _defaultSelectedResources[ tabID ];
			if( res != null )
			{
				string paneID = _querySidebar.GetResourceStructurePaneId( tabID );
				if( paneID != null )
				{
					AbstractViewPane viewPane = _querySidebar.GetPane( paneID );
					if( viewPane != null && viewPane.SelectResource( res, false ) )
					{
						viewPane.Select();
						return;
					}
				}

				if( defaultPane.SelectResource( res, false ) )
				{
					_querySidebar.Select();
					return;
				}
			}

			if( _defaultViewResource != null )
			{
				if( defaultPane.SelectResource( _defaultViewResource, false ) )
				{
					defaultPane.Select();
					return;
				}
			}

			Core.ResourceBrowser.DisplayResourceList( null, Core.ResourceStore.EmptyResourceList,
			                                          "", null );
		}

		/// <summary>
		/// Suppress painting as the <see cref="TabBar"/> control does it all and fills the whole surface.
		/// </summary>
		protected override void OnPaint( PaintEventArgs e )
		{
		}

		#endregion

		#region ICommandBar Interface Members

		public void SetSite( ICommandBarSite site )
		{
			_site = site;
			_tabBar.SetSite( this );
		}

		public Size MinSize
		{
			get { return _tabBar.MinSize; }
		}

		public Size MaxSize
		{
			get { return _tabBar.MaxSize; }
		}

		public Size OptimalSize
		{
			get { return _tabBar.OptimalSize; }
		}

		public Size Integral
		{
			get { return _tabBar.Integral; }
		}

		#endregion

		#region ICommandBarSite Interface Members

		public bool RequestMove( ICommandBar sender, Size offset )
		{
			return _site.RequestMove( this, offset );
		}

		public bool RequestSize( ICommandBar sender, Size difference )
		{
			return _site.RequestSize( this, difference );
		}

		public bool PerformLayout( ICommandBar sender )
		{
			return _site.PerformLayout( this );
		}

		#endregion
	}

	#region GoTabAction Class — Action to activate the specified tab.

	/// <summary>
	/// Action to activate the specified tab.
	/// </summary>
	internal class GoTabAction : SimpleAction
	{
		private TabSwitcher _tabSwitcher;

		private int _index;

		internal GoTabAction( TabSwitcher tabSwitcher, int index )
		{
			_tabSwitcher = tabSwitcher;
			_index = index;
		}

		public override void Execute( IActionContext context )
		{
			_tabSwitcher.SelectTab( _index );
		}
	}

	#endregion
}
