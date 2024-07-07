// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml;

using System35;

using DBIndex;
using Ini;

using JetBrains.Annotations;
using JetBrains.Interop.WinApi;
using JetBrains.Interop.WinApi.Wrappers;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.Categories;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.Containers;
using JetBrains.Omea.Database;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.CommandBar;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Plugins;
using JetBrains.Omea.RemoteControl;
using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.TextIndex;
using JetBrains.UI.Hooks;
using JetBrains.UI.Interop;
using Microsoft.Win32;
using PicoContainer.Defaults;

using Application=System.Windows.Forms.Application;
using ContextMenu=System.Windows.Forms.ContextMenu;
using HorizontalAlignment=System.Windows.Forms.HorizontalAlignment;
using Image=System.Drawing.Image;
using Label=System.Windows.Forms.Label;
using MenuItem=System.Windows.Forms.MenuItem;
using MessageBox=System.Windows.Forms.MessageBox;
using Panel=System.Windows.Forms.Panel;
using Point=System.Drawing.Point;
using Size=System.Drawing.Size;
using SystemColors=System.Drawing.SystemColors;
using Timer=System.Windows.Forms.Timer;
using ToolTip=System.Windows.Forms.ToolTip;

namespace JetBrains.Omea
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class MainFrame : Form
    {
        private const string EAPVersionName = "Grenache";

        private MenuStrip _mainMenu;
        private ToolStripMenuItem _menuTools;
        private ToolStripMenuItem _menuView;
        private ToolStripMenuItem _menuFile;
        private ToolStripMenuItem _menuEdit;
        private ToolStripMenuItem _menuActions;
        private ToolStripMenuItem _menuGo;
        private ToolStripMenuItem _menuHelp;
        private ToolStripMenuItem _menuSearch;
        private ToolStripMenuItem _menuWSpaces;

        private IContainer components;
        private ContextMenuStrip _contextMenu;
        private readonly IniFile _iniFile;
        private AsyncProcessor _resourceJobProcessor;
        private AsyncProcessor _networkJobProcessor;
        private ImageList _pluginImages8;
        private ImageList _pluginImages32;
        private int _pluginImageBitDepth;

        private bool                _noTextIndex = false;
        private bool                _textIndexExists = false;
        private TextIndexManager    _textIndexManager;

        private static CustomExceptionHandler _excHandler;
        private static readonly ProtocolHandlerManager _protocolHandlerManager = new ProtocolHandlerManager();
        private ImageList _toolbarImages32;
        private ImageList _toolbarImages8;

        private Panel _statusBarPanel;
        private StatusBar _statusBar;
        private StatusBarPanel _itemStatusPanel;
        private StatusBarPanel _uiStatusPanel;
        private StatusBarPanel _netStatusPanel;
        private StatusBarPanel _backgroundExceptionPanel;
        private StatusBarPanel _indicatorStatusPanel;
        private readonly StatusPaneManager _itemStatusManager;
        private readonly StatusPaneManager _uiStatusManager;
        private readonly StatusPaneManager _netStatusManager;
        internal Panel _indicatorsPanel;
        private Label _memUsageLabel;
        internal ImageList _statesImageList;
        internal ToolTip _toolTip;

        private Panel _contentAndRightSidebarPane;
        private JetSplitter _rightSidebarSplitter;
        private VerticalSidebar _rightSidebar;
        private Panel _contentPane;
        private SidebarSwitcher _querySidebar;
        private JetSplitter _leftSidebarSplitter;
        private Panel _resourceBrowserBorder;
        private ResourceBrowser _resourceBrowser;
        private Panel _fillerPanel;

		private ResourceTabsRow	_panelResourceTabsRow;

        private SizeF _scaleFactor;

        internal PluginEnvironment _theEnvironment;

        private WorkspaceButtonsRow _panelWorkspaceButtonsRow;

        private WorkspaceManager _workspaceManager;
        private ActionManager _actionManager;
        private ResourceIconManager _resourceIconManager;

        private FlagColumn _flagColumn;

        internal WheelMessageFilter _messageFilter;

        private int _idleLastMessage = -1;

        private readonly ArrayList _backgroundExceptionList = new ArrayList();

        private ColorScheme _colorScheme;

        private readonly TrayIconManager  _trayIconManager;
        private NotifyIcon      _notifyIcon;
        private ContextMenu     _notifyIconContextMenu;
        private MenuItem        _miOpenOmniaMea;
        private MenuItem        _miSeparator;
        private MenuItem        _miExitOmniaMea;

        private CorrespondentCtrl   _correspondentCtrl;

        private bool _minimized;
        private bool _needMaximize = false;
        private FormWindowState _oldWindowState = FormWindowState.Normal;

        private bool _cancelStart = false;

        internal static UIAsyncProcessor _uiAsyncProcessor;
        private readonly UIManager _uiManager;
        private readonly RemoteControlServer _rcServer;
        internal static bool _skipPlugins = false;
        internal static bool _skipWizard = false;
        private bool _detailedProgress;
        private Timer _uiUpdateTimer;
        private bool _forceClose;
        private bool _formSettingsRestored = false;
        private bool _restoringFromTray = false;
        private static bool _forceBlobsUpdate = false;

		/// <summary>
		/// A handle to the unmanaged pre-splash screen window, if one was shown by the unmanaged Launcher stage. Should be discarded by the managed splash.
		/// </summary>
		private static IntPtr _hwndUnmanagedPreSplash;

        internal static bool RunningTests
        {
            get { return _excHandler == null; }
        }

        public MainFrame()
        {
            try
            {
                LogManager.InitializeLog();
                CookiesManager.RegisterCookieProvider( new InternetExplorerCookieProvider() );

                _theEnvironment = new PluginEnvironment();
                _uiAsyncProcessor = new UIAsyncProcessor( this );
                _theEnvironment.SetUserInterfaceAP( _uiAsyncProcessor );
                _uiAsyncProcessor.ExceptionHandler = ReportAsyncException;

                _theEnvironment.SetProtocolHandlerManager( _protocolHandlerManager );
                _theEnvironment.SetState( CoreState.Initializing );

                _theEnvironment.SetMainWindow( this );
                _theEnvironment.SetExcHandler( _excHandler );

                Icon appIcon;
#if !READER
                appIcon = LoadIconFromAssembly( "App.ico" );
#else
                appIcon = LoadIconFromAssembly( "AppReader.ico" );
#endif
                _uiManager = new UIManager( appIcon );
                _theEnvironment.RegisterComponentInstance( _uiManager );

                string workDir = FindWorkingDirectory();
                if ( workDir == null )
                {
                    _cancelStart = true;
                    return;
                }

                _iniFile = new IniFile( Path.Combine( workDir, "OmniaMea.ini" ) );
                _theEnvironment.RegisterComponentInstance( _iniFile );
                LogManager.InitializeUsageLog();

                OMEnv.WorkDir = workDir;
                OMEnv.DataDir = Path.Combine( Application.StartupPath, "data" );
                MyPalStorage.DBPath = Path.Combine( workDir, "db" );

                //  Enable backup by default.
                _iniFile.WriteBool( "ResourceStore", "EnableBackup", true );
                _iniFile.WriteString( "ResourceStore", "BackupPath", Path.Combine( workDir, "backup" ) );

                ArrayList absentFiles = new ArrayList();
                if ( !OMEnv.IsDictionaryPresent( absentFiles ) )
                {
                    string files = string.Join( ", ", (string[]) absentFiles.ToArray( typeof( string )));
                    MessageBox.Show( "Cannot find dictionary file(s): " + files + ". Please reinstall the product.",
                                     "Startup Error", MessageBoxButtons.OK,  MessageBoxIcon.Exclamation );
                    _cancelStart = true;
                    return;
                }

                string dbPath = MyPalStorage.DBPath;
                if (!Directory.Exists( dbPath ) )
                    Directory.CreateDirectory( dbPath );
                DatabasePathOptionsPane.SetBackupDefaults();

                InitializeAsyncProcessors();

                //
                // Required for Windows Form Designer support
                //
                InitializeComponent();

                _panelWorkspaceButtonsRow.WorkspaceButtonsManager.WorkspaceChanged += _workspaceBar_WorkspaceChanged;

                if( _iniFile.ReadBool( "MainFrame", "ShowMemoryUsage", false ) )
                {
                    _uiManager.MemUsageLabel = _memUsageLabel;
                }
                else
                {
                    _indicatorsPanel.Controls.Remove( _memUsageLabel );
                    _indicatorsPanel.Width -= _memUsageLabel.Width;
                    _indicatorsPanel.Left += _memUsageLabel.Width;
                }

                Core.UIManager.RegisterIndicatorLight( "Resource Store", _resourceJobProcessor, 5,
                    LoadIconFromAssembly( "resourcestore_idle.ico" ),
                    LoadIconFromAssembly( "resourcestore_busy.ico" ),
                    LoadIconFromAssembly( "resourcestore_stuck.ico" ) );
                Core.UIManager.RegisterIndicatorLight( "Network", _networkJobProcessor, 60,
                    LoadIconFromAssembly( "network_idle.ico" ),
                    LoadIconFromAssembly( "network_busy.ico" ),
                    LoadIconFromAssembly( "network_stuck.ico" ) );

                TracePlatformVersion();
                InitPluginImageBitDepth();
                if ( _pluginImageBitDepth == 8 )
                {
#if !READER
                    _notifyIcon.Icon = LoadIconFromAssembly( "trayicon8.ico" );
#else
                    _notifyIcon.Icon = LoadIconFromAssembly( "trayiconReader8.ico" );
#endif
                }
                else
                {
#if !READER
                    _notifyIcon.Icon = LoadIconFromAssembly( "trayicon.ico" );
#else
                    _notifyIcon.Icon = LoadIconFromAssembly( "trayiconReader.ico" );
#endif
                }

                _panelResourceTabsRow.BackColor = Color.FromArgb( 206, 204, 187 );

                _rightSidebar = new VerticalSidebar();
                _rightSidebar.Dock = DockStyle.Right;
                _rightSidebar.Side = SidebarSide.Right;
                _rightSidebar.Visible = false;
//                _rightSidebar.ExpandedChanged += OnRightSidebarExpand;
                _rightSidebar.PaneAdded += OnRightSidebarPaneAdded;
                _rightSidebar.SizeChanged += HandleRightSidebarSizeChanged;
//                _rightSidebar.BackgroundColorSchemeKey = "TopBar.Background";
//                _rightSidebar.BackgroundFillHeight = 0;
//                _rightSidebar.CollapsedWidth = 34;
//                _rightSidebar.PaintSidebarBackground += HandlePaintRightSidebarBackground;
                _contentAndRightSidebarPane.Controls.Add( _rightSidebar );

                _leftSidebarSplitter.ControlToCollapse = _querySidebar;
                _leftSidebarSplitter.PaintSplitterBackground += HandlePaintSplitterBackground;
                _leftSidebarSplitter.FillCenterRect = false;
                _leftSidebarSplitter.FillGradient = false;

                _rightSidebarSplitter.ControlToCollapse = _rightSidebar;
                _rightSidebarSplitter.PaintSplitterBackground += HandlePaintSplitterBackground;
                _rightSidebarSplitter.FillGradient = false;

                // Some of the Resource Type Tabs initialization
                _panelResourceTabsRow.ResourceTypeTabs.QuerySidebar = _querySidebar;
                _panelResourceTabsRow.ResourceTypeTabs.ResourceBrowser = _resourceBrowser;
                _panelResourceTabsRow.ResourceTypeTabs.UnreadManager = null;
                _panelResourceTabsRow.ResourceTypeTabs.TabChanged += _resourceTypeTabs_TabSwitch;
                _panelResourceTabsRow.ResourceTypeTabs.StartupComplete = false;

                _theEnvironment.RegisterComponentInstance( _resourceBrowser );
                _theEnvironment.RegisterComponentInstance( _panelResourceTabsRow.ResourceTypeTabs );
                _theEnvironment.RegisterComponentInstance( _querySidebar );
                _theEnvironment.RegisterComponentInstance( _rightSidebar );

                _itemStatusManager = new StatusPaneManager( _statusBar, _itemStatusPanel, "" );
                _uiStatusManager = new StatusPaneManager( _statusBar, _uiStatusPanel, "" );
                _netStatusManager = new StatusPaneManager( _statusBar, _netStatusPanel, "" );

                _uiStatusManager.UpdateStatusText( false );

                _trayIconManager = new TrayIconManager( _notifyIcon );
                _theEnvironment.RegisterComponentInstance( _trayIconManager );
//                _contactMgr = new ContactManager( Core.ResourceStore );
//                _theEnvironment.RegisterComponentInstance( _contactMgr );

                _messageFilter = new WheelMessageFilter();
                Application.AddMessageFilter( _messageFilter );

                Version verProduct = Assembly.GetExecutingAssembly().GetName().Version;

                if ( Core.ProductReleaseVersion != null )
                {
                    Text = "JetBrains " + Core.ProductFullName + " " + Core.ProductReleaseVersion;
                }
                else
                {
                    Text = "JetBrains " + Core.ProductFullName + " “" + EAPVersionName + "” v" + verProduct;
                }

                _theEnvironment.SetProductVersion( verProduct );
                Trace.WriteLine( "Current build is " + verProduct );

                HttpReader.LoadHttpConfig();

#if READER
            HttpReader.UserAgent = Text + " (http://www.jetbrains.com/omea/reader/)";
#else
                HttpReader.UserAgent = Text + " (http://www.jetbrains.com/omea/)";
#endif

                try
                {
                    Thread.CurrentThread.Name = "UI Thread";
                }
                catch( InvalidOperationException )
                {
                    // TestDriven.NET sets the main thread name, so in order to enable running tests
                    // under it, we ignore errors when setting the current thread name
                }

                InitResourceIconManager();
                CalcScaleFactor();

                if ( XPThemes.IsThemed )
                {
                    string colorScheme = XPThemes.ColorSchemeName;
                    Trace.WriteLine( "XP color scheme name is " + colorScheme );
                    if ( colorScheme == "Metallic" )
                    {
                        LoadColorScheme( "MetallicColorScheme.xml" );
                    }
                    else if ( colorScheme == "HomeStead" )
                    {
                        LoadColorScheme( "HomeSteadColorScheme.xml" );
                    }
                    else
                    {
                        LoadColorScheme( "DefaultColorScheme.xml" );
                    }
                }
                else
                {
                    Trace.WriteLine( "Not running under Windows XP, or no color scheme is active" );
                    LoadColorScheme( "DefaultColorScheme.xml" );
                }

                if ( _excHandler != null )
                {
                    _excHandler.OwnerControl = this;
                }
            }
            catch( Exception ex )
            {
                _excHandler.ReportException( ex, ExceptionReportFlags.Fatal );
                _cancelStart = true;
                return;
            }

            try
            {
                AbstractWebBrowser wb = Core.WebBrowser;
                wb.CreateControl();
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "Error initializing embedded browser: " + ex );
                MessageBox.Show( "Error initializing embedded browser: " + ex.Message +
                    "\r\nPlease reinstall " + Core.ProductFullName + ".", Core.ProductFullName );
                _cancelStart = true;
                return;
            }

            try
            {
                _rcServer = new RemoteControlServer();
                _theEnvironment.RegisterComponentInstance( _rcServer );
                if(_rcServer.IsEnabled)
                {
                    _rcServer.Start();
                    _rcServer.StoreProtectionKey();
                    _rcServer.AddRemoteCall( "MainFrame.Restore", new MethodInvoker( RestoreFromTray ) );
                }
            }
            catch( Exception ex )
            {
                if ( RunningTests )
                {
                    throw;
                }

                MessageBox.Show(
                    "Error initializing remote control server at random port: " + ex.Message + ".\n" +
                    "Browser plugins will not work.",
                    Core.ProductFullName );
            }

            if ( !RunningTests )
            {
                try
                {
                    OnFirstShow();
                }
                catch( Exception e )
                {
                    _excHandler.ReportException( e, ExceptionReportFlags.Fatal );
                }
            }
            else
            {
                OnFirstShow();
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MainFrame));
            this._mainMenu = new System.Windows.Forms.MenuStrip();
            this._menuFile = new ToolStripMenuItem ();
            this._menuEdit = new ToolStripMenuItem ();
            this._menuView = new ToolStripMenuItem ();
            this._menuSearch = new ToolStripMenuItem ();
            this._menuGo = new ToolStripMenuItem ();
            this._menuTools = new ToolStripMenuItem ();
            this._menuActions = new ToolStripMenuItem ();
            this._menuWSpaces = new ToolStripMenuItem ();
            this._menuHelp = new ToolStripMenuItem ();
            this._toolbarImages8 = new System.Windows.Forms.ImageList(this.components);
            this._contextMenu = new System.Windows.Forms.ContextMenuStrip();
            this._pluginImages8 = new System.Windows.Forms.ImageList(this.components);
            this._pluginImages32 = new System.Windows.Forms.ImageList(this.components);
            this._toolbarImages32 = new System.Windows.Forms.ImageList(this.components);
            this._statusBarPanel = new System.Windows.Forms.Panel();
            this._statusBar = new System.Windows.Forms.StatusBar();
            this._itemStatusPanel = new System.Windows.Forms.StatusBarPanel();
            this._uiStatusPanel = new System.Windows.Forms.StatusBarPanel();
            this._netStatusPanel = new System.Windows.Forms.StatusBarPanel();
            this._backgroundExceptionPanel = new System.Windows.Forms.StatusBarPanel();
            this._indicatorStatusPanel = new  System.Windows.Forms.StatusBarPanel();
            this._indicatorsPanel = new System.Windows.Forms.Panel();
            this._memUsageLabel = new System.Windows.Forms.Label();
            this._statesImageList = new System.Windows.Forms.ImageList(this.components);
            this._toolTip = new System.Windows.Forms.ToolTip(this.components);
            this._querySidebar = new SidebarSwitcher();
            this._resourceBrowser = new ResourceBrowser();
            this._contentPane = new System.Windows.Forms.Panel();
            this._contentAndRightSidebarPane = new Panel();
            this._resourceBrowserBorder = new System.Windows.Forms.Panel();
            this._leftSidebarSplitter = new JetBrains.Omea.GUIControls.JetSplitter();
            this._rightSidebarSplitter = new JetBrains.Omea.GUIControls.JetSplitter();
            this._panelWorkspaceButtonsRow = new WorkspaceButtonsRow();
            this._fillerPanel = new System.Windows.Forms.Panel();
            this._panelResourceTabsRow = new ResourceTabsRow();
            this._notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this._notifyIconContextMenu = new System.Windows.Forms.ContextMenu();
            this._miOpenOmniaMea = new System.Windows.Forms.MenuItem();
            this._miSeparator = new System.Windows.Forms.MenuItem();
            this._miExitOmniaMea = new System.Windows.Forms.MenuItem();
            this._uiUpdateTimer = new System.Windows.Forms.Timer(this.components);
            this._statusBarPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._itemStatusPanel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._uiStatusPanel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._netStatusPanel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._backgroundExceptionPanel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._indicatorStatusPanel)).BeginInit();
            this._indicatorsPanel.SuspendLayout();
            this._contentPane.SuspendLayout();
            this._resourceBrowserBorder.SuspendLayout();
            this._panelWorkspaceButtonsRow.SuspendLayout();
            this._panelResourceTabsRow.SuspendLayout();
            this.SuspendLayout();

            #region MenuStrip
            //
            // _mainMenu
            //
            _mainMenu.Name = "Main Menu";
            _mainMenu.Text = "Main Menu";
            _mainMenu.Dock = DockStyle.Top;
            _mainMenu.Height = 24;
            _mainMenu.TabIndex = 1;
            this._mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {  this._menuFile, this._menuEdit, this._menuView,
                                                                                      this._menuSearch, this._menuGo, this._menuTools,
                                                                                      this._menuActions, this._menuWSpaces, this._menuHelp});
            this._menuFile.Text = "&File";
            this._menuEdit.Text = "&Edit";
            this._menuView.Text = "&View";
            this._menuSearch.Text = "&Search";
            this._menuGo.Text = "&Go";
            this._menuTools.Text = "&Tools";
            this._menuActions.Text = "&Actions";
            this._menuWSpaces.Text = "&Workspaces";
            this._menuHelp.Text = "&Help";

            #endregion MenuStrip

            //
            // _toolbarImages8
            //
            this._toolbarImages8.ImageSize = new System.Drawing.Size(16, 16);
            this._toolbarImages8.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_toolbarImages8.ImageStream")));
            this._toolbarImages8.TransparentColor = System.Drawing.Color.Transparent;
            //
            // _pluginImages8
            //
            this._pluginImages8.ImageSize = new System.Drawing.Size(16, 16);
            this._pluginImages8.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_pluginImages8.ImageStream")));
            this._pluginImages8.TransparentColor = System.Drawing.Color.Transparent;
            //
            // _pluginImages32
            //
            this._pluginImages32.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this._pluginImages32.ImageSize = new System.Drawing.Size(16, 16);
            this._pluginImages32.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_pluginImages32.ImageStream")));
            this._pluginImages32.TransparentColor = System.Drawing.Color.Transparent;
            //
            // _toolbarImages32
            //
            this._toolbarImages32.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this._toolbarImages32.ImageSize = new System.Drawing.Size(16, 16);
            this._toolbarImages32.TransparentColor = System.Drawing.Color.Transparent;

            #region StatusBar
            //
            // _statusBarPanel
            //
            this._statusBarPanel.Controls.Add(this._statusBar);
            this._statusBarPanel.Name = "_statusBarPanel";
            this._statusBarPanel.Size = new System.Drawing.Size(940, 22);	// Defines the size
            this._statusBarPanel.TabIndex = 5;
			this._statusBarPanel.Dock = DockStyle.Bottom;

            //
            // _statusBar
            //
            this._statusBar.Font = new System.Drawing.Font("Tahoma", 8F);
            this._statusBar.Location = new System.Drawing.Point(0, 0);
            this._statusBar.Name = "_statusBar";
            this._statusBar.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] { this._itemStatusPanel,
                                                                                        this._uiStatusPanel,
                                                                                        this._netStatusPanel,
                                                                                        this._backgroundExceptionPanel,
                                                                                        this._indicatorStatusPanel});
            this._statusBar.ShowPanels = true;
            this._statusBar.Controls.Add(this._indicatorsPanel);
            this._statusBar.Size = new System.Drawing.Size(820, 22);
            this._statusBar.SizingGrip = false;
            this._statusBar.TabIndex = 2;
            this._statusBar.PanelClick += new System.Windows.Forms.StatusBarPanelClickEventHandler(this._statusBar_PanelClick);
            //
            // _itemStatusPanel
            //
            this._itemStatusPanel.Width = 70;
            //
            // _uiStatusPanel
            //
            this._uiStatusPanel.Width = 400;
            //
            // _netStatusPanel
            //
            this._netStatusPanel.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
            this._netStatusPanel.Width = 200;
            //
            // _backgroundExceptionPanel
            //
            this._backgroundExceptionPanel.Width = 20;
            //
            // _indicatorStatusPanel
            //
            this._indicatorStatusPanel.Width = 130;
            //
            // _indicatorsPanel
            //
            this._indicatorsPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this._indicatorsPanel.BorderStyle = BorderStyle.None;
            this._indicatorsPanel.Controls.Add(this._memUsageLabel);
            this._indicatorsPanel.Location = new System.Drawing.Point(690, 2);
            this._indicatorsPanel.Name = "_indicatorsPanel";
            this._indicatorsPanel.Size = new System.Drawing.Size(124, 18);
            this._indicatorsPanel.TabIndex = 0;
            this._indicatorsPanel.Resize +=new EventHandler(_indicatorsPanel_Resize);
            //
            // _memUsageLabel
            //
            this._memUsageLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._memUsageLabel.Dock = DockStyle.None;
            this._memUsageLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            this._memUsageLabel.Location = new System.Drawing.Point(0, 2);
            this._memUsageLabel.Name = "_memUsageLabel";
            this._memUsageLabel.Size = new System.Drawing.Size(120, 14);
            this._memUsageLabel.AutoSize = false;
            this._memUsageLabel.TabIndex = 3;
            this._memUsageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._memUsageLabel.FlatStyle = FlatStyle.System;
            //
            // _statesImageList
            //
            this._statesImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this._statesImageList.ImageSize = new System.Drawing.Size(16, 16);
            this._statesImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_statesImageList.ImageStream")));
            this._statesImageList.TransparentColor = System.Drawing.Color.Transparent;

            #endregion StatusBar

            //
            // _querySidebar
            //
            this._querySidebar.ColorScheme = null;
            this._querySidebar.DefaultPaneIcon = null;
            this._querySidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this._querySidebar.Expanded = false;
            this._querySidebar.ExpandedWidth = 0;
            this._querySidebar.Location = new System.Drawing.Point(0, 0);
            this._querySidebar.Name = "_querySidebar";
            this._querySidebar.Size = new System.Drawing.Size(200, 406);
            this._querySidebar.TabIndex = 3;
            this._querySidebar.ExpandedChanged += new System.EventHandler(this._querySidebar_ExpandedChanged);
            //
            // _resourceBrowser
            //
            this._resourceBrowser.DefaultColumns = null;
            this._resourceBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this._resourceBrowser.LinksPaneExpanded = false;
            this._resourceBrowser.Location = new System.Drawing.Point(0, 0);
            this._resourceBrowser.Name = "_resourceBrowser";
            this._resourceBrowser.Size = new System.Drawing.Size(728, 404);
            this._resourceBrowser.TabIndex = 6;
            this._resourceBrowser.ViewAnnotations = false;
            this._resourceBrowser.WebPageMode = false;
            //
            // _contentAndRightSidebarPane
            //
            this._contentAndRightSidebarPane.Controls.Add(this._contentPane);
//            this._contentAndRightSidebarPane.Controls.Add(this._fillerPanel);
            this._contentAndRightSidebarPane.Controls.Add(this._rightSidebarSplitter);
            this._contentAndRightSidebarPane.Name = "_contentAndRightSidebarPane";
			_contentAndRightSidebarPane.Dock = DockStyle.Fill;
            //
            // _contentPane
            //
            this._contentPane.Controls.Add(this._resourceBrowserBorder);
            this._contentPane.Controls.Add(this._leftSidebarSplitter);
            this._contentPane.Controls.Add(this._querySidebar);
            this._contentPane.Dock = DockStyle.Fill;
            this._contentPane.Location = new System.Drawing.Point(0, 4);
            this._contentPane.Name = "_contentPane";
            this._contentPane.Size = new System.Drawing.Size(940, 402);
            this._contentPane.TabIndex = 7;
            //
            // _resourceBrowserBorder
            //
            this._resourceBrowserBorder.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._resourceBrowserBorder.Controls.Add(this._resourceBrowser);
            this._resourceBrowserBorder.Dock = System.Windows.Forms.DockStyle.Fill;
            this._resourceBrowserBorder.Location = new System.Drawing.Point(205, 0);
            this._resourceBrowserBorder.Name = "_resourceBrowserBorder";
            this._resourceBrowserBorder.Size = new System.Drawing.Size(730, 406);
            this._resourceBrowserBorder.TabIndex = 17;
            //
            // _splitter
            //
            this._leftSidebarSplitter.CollapsedChanged += new EventHandler( HandleLeftSidebarCollapsedChanged );
            this._leftSidebarSplitter.Location = new System.Drawing.Point(200, 0);
            this._leftSidebarSplitter.Name = "_leftSidebarSplitter";
            this._leftSidebarSplitter.Size = new System.Drawing.Size(5, 406);
            this._leftSidebarSplitter.TabIndex = 8;
            this._leftSidebarSplitter.TabStop = false;
            //
            // _rightSideSplitter
            //
            this._rightSidebarSplitter.Dock = System.Windows.Forms.DockStyle.Right;
            this._rightSidebarSplitter.Location = new System.Drawing.Point(935, 0);
            this._rightSidebarSplitter.Name = "_rightSidebarSplitter";
            this._rightSidebarSplitter.FillCenterRect = false;
            this._rightSidebarSplitter.Size = new System.Drawing.Size(5, 406);
            this._rightSidebarSplitter.TabIndex = 0;
            this._rightSidebarSplitter.TabStop = false;
            this._rightSidebarSplitter.Visible = false;
            this._rightSidebarSplitter.VisibleChanged += new EventHandler( HandleRightSidebarCollapsedChanged );
            //
            // _topBarPane
            //
            _panelWorkspaceButtonsRow.Name = "_panelWorkspaceButtonsRow";
			_panelWorkspaceButtonsRow.TabIndex = 2;
			_panelWorkspaceButtonsRow.Dock = DockStyle.Top;
/*
            //
            // _fillerPanel
            //
            this._fillerPanel.Dock = DockStyle.Top;
            this._fillerPanel.Location = new System.Drawing.Point(0, 70);
            this._fillerPanel.Name = "_fillerPanel";
            this._fillerPanel.Paint += new PaintEventHandler( HandlePaintFillerPanel );
            this._fillerPanel.Size = new System.Drawing.Size(940, 4);
            this._fillerPanel.TabIndex = 16;
*/
            //
            // _panelResourceTabsBar
            //
        	_panelResourceTabsRow.Name = "_panelResourceTabsRow";
        	_panelResourceTabsRow.TabIndex = 3;
			_panelResourceTabsRow.Dock = DockStyle.Top;

            #region TaskBar Icon
            //
            // _notifyIcon
            //
            this._notifyIcon.ContextMenu = this._notifyIconContextMenu;
            this._notifyIcon.Text = "JetBrains Omea";
            this._notifyIcon.Visible = true;
            this._notifyIcon.DoubleClick += new System.EventHandler(this._notifyIcon_DoubleClick);
            //
            // _notifyIconContextMenu
            //
            this._notifyIconContextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                                   this._miOpenOmniaMea,
                                                                                                   this._miSeparator,
                                                                                                   this._miExitOmniaMea});
            //
            // _miOpenOmniaMea
            //
            this._miOpenOmniaMea.DefaultItem = true;
            this._miOpenOmniaMea.Index = 0;
            this._miOpenOmniaMea.Text = "Open Omea";
            this._miOpenOmniaMea.Click += new System.EventHandler(this.miOpenOmniaMea_Click);
            //
            // _miSeparator
            //
            this._miSeparator.Index = 1;
            this._miSeparator.Text = "-";
            //
            // _miExitOmniaMea
            //
            this._miExitOmniaMea.Enabled = false;
            this._miExitOmniaMea.Index = 2;
            this._miExitOmniaMea.Text = "Exit Omea";
            this._miExitOmniaMea.Click += new System.EventHandler(this.miExitOmniaMea_Click);
            #endregion TaskBar Icon

            //
            // _uiUpdateTimer
            //
            this._uiUpdateTimer.Enabled = true;
            this._uiUpdateTimer.Interval = 500;
            this._uiUpdateTimer.Tick += new System.EventHandler(this._uiUpdateTimer_Tick);
            //
            // MainFrame
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(940, 501);

            this.Controls.Add(this._contentAndRightSidebarPane);
			this.Controls.Add(this._panelResourceTabsRow);
			this.Controls.Add(this._panelWorkspaceButtonsRow);
            this.Controls.Add( _mainMenu );
            this.Controls.Add(this._statusBarPanel);
#if !READER
            this.Icon = LoadIconFromAssembly( "App.ico" );
#else
            this.Icon = LoadIconFromAssembly( "AppReader.ico" );
#endif
            this.KeyPreview = true;
            this.MainMenuStrip = _mainMenu;
            this.Name = "MainFrame";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Omnia Mea";
            this.Resize += new System.EventHandler(this.MainFrame_Resize);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.OnClosing);
            this.Closed += new System.EventHandler(this.OnClosed);
            this._statusBarPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._itemStatusPanel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._uiStatusPanel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._netStatusPanel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._backgroundExceptionPanel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._indicatorStatusPanel)).EndInit();
            this._indicatorsPanel.ResumeLayout(false);
            this._contentPane.ResumeLayout(false);
            this._resourceBrowserBorder.ResumeLayout(false);
            this._panelWorkspaceButtonsRow.ResumeLayout(false);
            this._panelResourceTabsRow.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        internal class UIAsyncProcessor : AsyncProcessor
        {
            private delegate void ExecuteJobDelegate( AbstractJob job );

            private readonly MainFrame _mainFrame;
            private readonly Thread _startupThread;
            private readonly ExecuteJobDelegate _executeJobMethod;
            private int _reenteringJobsCount;

            public UIAsyncProcessor( MainFrame mainFrame )
                : base( false )
            {
                _startupThread = Thread.CurrentThread;
                _reenteringJobsCount = 0;
                _mainFrame = mainFrame;
                ProcessMessages = true;
                _executeJobMethod = base.ExecuteJob;
                QueueGotEmpty += UIAsyncProcessor_QueueGotEmpty;
            }

            public override void EmployCurrentThread()
            {
                QueueJob( JobPriority.Immediate, new MethodInvoker( VeryFirstUIJob ) );
                QueueJobAt( DateTime.Now.AddSeconds( 30 ), new MethodInvoker( DeleteOldLogs ) );
                base.EmployCurrentThread();
            }

            public bool AreThereObsoleteTimedJobs
            {
                get
                {
                    DateTimePriorityQueue.QueueEntry entry = _timedJobs.GetMinimumEntry();
                    return entry != null && entry.Priority <= DateTime.Now;
                }
            }

            protected override bool PushJob( AbstractJob job, JobPriority priority )
            {
                if( _isThreadStarted != 0 )
                {
                    return base.PushJob( job, priority );
                }

                _mainFrame.BeginInvoke( _executeJobMethod, new object[] { job } );
                return true;
            }

            public override bool IsOwnerThread
            {
                get
                {
                    if ( _isThreadStarted == 0 )
                    {
                        return _startupThread == Thread.CurrentThread;
                    }
                    return base.IsOwnerThread;
                }
            }

            public override void DoJobs()
            {
                ProcessMessages = ++_reenteringJobsCount < 2;
                try
                {
                    base.DoJobs();
                }
                finally
                {
                    --_reenteringJobsCount;
                }
                if( IsIdle )
                {
                    _mainFrame.OnAppIdle( this, EventArgs.Empty );
                }
            }

            internal bool IsIdle
            {
                get { return !_finished && OutstandingJobs == 0 && !AreThereObsoleteTimedJobs; }
            }

            internal void Awake()
            {
                _awakeningEvent.Set();
            }

            private void UIAsyncProcessor_QueueGotEmpty( object sender, EventArgs e )
            {
                if( !Finished && ProcessMessages )
                {
					unsafe
					{
						User32Dll.MsgWaitForMultipleObjectsEx(0, null, 200, (uint)QueueStatusFlags.QS_ALLINPUT, (uint)MsgWaitForMultipleObjectsFlags.MWMO_INPUTAVAILABLE );
					}
				}
            }

            private void VeryFirstUIJob()
            {
                Reenterable = false;
                _mainFrame.Visible = true;
            }

            private static void DeleteOldLogs()
            {
                int maxLogAge = Core.SettingStore.ReadInt( "MainFrame", "MaxLogAge", 5 );
                if ( maxLogAge > 0 )
                {
                    LogManager.DeleteOldLogs( maxLogAge );
                }
            }
        }

        private int _timerTicks = 0;

        private void _uiUpdateTimer_Tick( object sender, EventArgs e )
        {
            ++_timerTicks;
            if( _uiAsyncProcessor.IsIdle )
            {
                OnAppIdle( this, EventArgs.Empty );
            }
            else
            {
                int startCycle = Environment.TickCount;
                while( !_uiAsyncProcessor.Finished &&
                    ( _uiAsyncProcessor.OutstandingJobs > 0 || _uiAsyncProcessor.AreThereObsoleteTimedJobs ) )
                {
                    _uiAsyncProcessor.DoJobs();
                    if( Environment.TickCount - 200 > startCycle )
                    {
                        break;
                    }
                }
            }
            if( _uiManager != null )
            {
                _uiManager.UpdateLights();
            }
            // ping ui asyncprocessor on exit
            if( Core.State == CoreState.ShuttingDown )
            {
                _uiAsyncProcessor.Awake();
            }
            if( ( _timerTicks & 63 ) == 0 )
            {
                GC.Collect();
            }
        }

        internal class InternetExplorerCookieProvider: ICookieProvider
        {
            public string Name
            {
                get { return CookiesManager.InternetExplorerCookieProviderName; }
            }

            public string GetCookies( string url )
            {
                return InternetCookies.Get( url );
            }

            public void SetCookies( string url, string cookies )
            {
                InternetCookies.Set( url, cookies );
            }
        }

        internal static bool _restartFlag = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            string startDirectory = Environment.CurrentDirectory;
            string[] args = Environment.GetCommandLineArgs();
            for( int i = 0; i < args.Length; i++ )
            {
                string s = args [i].Substring( 1 );
                if ( s == "leakdiag" )
                {
                    MessageBox.Show( "Starting Omea" );
                }
                if( s == "forceblobsupdate" )
                {
                    _forceBlobsUpdate = true;
                }
            }

            Mutex omniaMeaMux = null;
            try
            {
                string openurl = null;
                for( int i = 0; i < args.Length-1; i++ )
                {
                    if ( args [i].Substring( 1 ) == "openurl" )
                    {
                        if ( i+1 < args.Length )
                        {
                            openurl = args[i+1];
                            if ( openurl != null )
                            {
                                _protocolHandlerManager.SetOpenURL( openurl );
                            }
                        }
                        break;
                    }
                }

                bool omniaMeaIsNotRun;
                omniaMeaMux = new Mutex( true, "OmniaMeaMutex", out omniaMeaIsNotRun );
                if( !omniaMeaIsNotRun )
                {
                    try
                    {
                        if ( openurl != null )
                        {
                            _protocolHandlerManager.RemoteInvoke( RemoteControlServer.configuredPort, RemoteControlServer.protectionKey );
                        }
                        else
                        {
                            new RemoteControlClient( RemoteControlServer.configuredPort,
                                RemoteControlServer.protectionKey ).SendRequest( "MainFrame.Restore" );
                        }
                    }
                    catch( Exception )
                    {
                        // ignore
                    }
                    return;
                }

                _excHandler = new CustomExceptionHandler();
                Application.ThreadException += _excHandler.OnThreadException;

                MainFrame mainFrame = new MainFrame();
                if( !mainFrame._cancelStart )
                {
                    _uiAsyncProcessor.EmployCurrentThread();
                    _uiAsyncProcessor.Dispose();
                    Trace.WriteLine( "Done closing." );
                }
            }
            finally
            {
                try
                {
                    MyPalStorage.CloseDatabase();
                    FileResourceManager.ClearTrashDirectory();
                }
                catch( Exception ex )
                {
                    Trace.WriteLine( "Database close failed: " + ex );
                }
                if( omniaMeaMux != null )
                    omniaMeaMux.Close();
            }
            if( _restartFlag )
            {
                StringBuilder argsBuilder = new StringBuilder();
                for( int i = 1; i < args.Length; ++i )
                {
                    argsBuilder.Append( args[ i ] );
                }
                Environment.CurrentDirectory = startDirectory;
                Process.Start( args[ 0 ], argsBuilder.ToString() );
            }
        }

    	/// <summary>
    	/// An entry point for the Launcher, when it starts the application.
    	/// </summary>
    	/// <param name="hWndSplash">If an unmanaged pre-splash was shown, a handle to its window. Can be <c>Null</c>.</param>
    	public static void Launch(IntPtr hWndSplash)
    	{
    		try
    		{
    			_hwndUnmanagedPreSplash = hWndSplash;
    			Main();
    		}
    		catch(Exception ex)
    		{
    			MessageBox.Show(new Win32Window(hWndSplash), "JetBrains Omea failed to start." + Environment.NewLine + Environment.NewLine + ex.Message, "Omea", MessageBoxButtons.OK, MessageBoxIcon.Error);
    		}
    	}

        private static string FindWorkingDirectory()
        {
			string workDir = null;

			// Pass 1: Explicitly specified on the command-line
            string[] args = Environment.GetCommandLineArgs();
            for( int i = 0; i < args.Length - 1; i++ )
            {
                if ( args[ i ].Substring( 1 ) == "workdir" )
                {
                    try
                    {
                        workDir = Path.GetFullPath( args[ i + 1 ] );
                    }
                    catch( ArgumentException ex )
                    {
                        MessageBox.Show( "Working directory specified incorrectly:\n" +
                                         args [ i + 1 ] + "\n" + ex.Message, "Omea" );
                        return null;
                    }
                }
            }
			if(workDir != null)
				return workDir;	// Note: not written to the Registry

			//////////////////////
			// Pass 2: default workdir specified in the Registry

			// Ensure Registry key
            if ( !RegUtil.CreateOmeaKey() )
            {
            	ShowErrorMessage_RepairInstallation("Failed to open or create Omea registry key in the Current User Registry.");
            	return null;
            }

			// Read from the Registry
        	workDir = RegUtil.DatabasePath;
        	if(workDir != null)
        	{
        		try
        		{
					return workDir = Path.GetFullPath(workDir);
        		}
        		catch(Exception ex)
        		{
        			ShowErrorMessage("The Registry setting for Omea Database Path is corrupt. Default database folder will be used.");
        		}
        	}

        	// Pass 3: neither cmdline nor Registry default are set, create and write the default path
        	string defltFolder = Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );
        	workDir = Path.Combine(Path.Combine( defltFolder, "JetBrains" ), "Omea");

        	// Save default paths
        	RegUtil.DatabasePath = workDir;
        	RegUtil.LogPath = Path.Combine( workDir, "logs" );

        	return workDir;
        }

    	/// <summary>
    	/// Reports an error in Omea configuration.
    	/// Recommends repairing the installation.
    	/// </summary>	// TODO: use for error messages
    	private static void ShowErrorMessage_RepairInstallation([NotNull] string reason)
    	{
    		if(reason == null)
    			throw new ArgumentNullException("reason");
    		ShowErrorMessage(string.Format("{0}\n\nIt is recommended that you repair Omea installation.", reason));
    	}

		/// <summary>
		/// Reports an error thru a standard message box.
		/// </summary>
    	private static void ShowErrorMessage(string error)
    	{
    		MessageBox.Show(error, "Omea – Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    	}

    	/// <summary>
        /// Under Windows XP or later, sets OmniaMea to use 32-bit icons. Otherwise,
        /// uses 8-bit icons.
        /// </summary>
        private void InitResourceIconManager()
        {
            if ( _pluginImageBitDepth == 32 )
            {
                AddIconFromAssembly( "back.ico", _toolbarImages32 );
                AddIconFromAssembly( "forward.ico", _toolbarImages32 );

                _resourceIconManager = new ResourceIconManager( _pluginImages32 );
            }
            else
            {
                _resourceIconManager = new ResourceIconManager( _pluginImages8 );
            }
            _theEnvironment.RegisterComponentInstance( _resourceIconManager );

            Icon propIcon = LoadIconFromAssembly( "property.ico" );
            Icon linkIcon = LoadIconFromAssembly( "link.ico" );
            _resourceIconManager.SetDefaultPropIcons( propIcon, linkIcon );
            _resourceIconManager.RegisterResourceIconProvider( "ResourceType",
                new ResourceTypeIconProvider( linkIcon ) );
            _resourceIconManager.RegisterResourceIconProvider( "PropType",
                new PropTypeIconProvider( _resourceIconManager, propIcon ) );

            IResourceIconProvider viewIconProvider = new ViewIconProvider();
            _resourceIconManager.RegisterResourceIconProvider( FilterManagerProps.ViewResName, viewIconProvider );
        }

        private static void  TracePlatformVersion()
        {
            string version = PlatformVersions;
            Trace.WriteLine( ".Net Platforms installed: " + version );
            Trace.WriteLine( ".Net Platform active: " + Environment.Version );
        }
        public static string  PlatformVersions
        {
            get
            {
                string    versionString = string.Empty;
                try
                {
                    RegistryKey regKey = Registry.LocalMachine.OpenSubKey( @"Software\Microsoft\.NetFramework\Policy" );
                    string[] subkeyNames = regKey.GetSubKeyNames();
                    foreach( string name in subkeyNames )
                    {
                        if( name.Length > 2 && name[ 0 ] == 'v' && Char.IsDigit( name[ 1 ] ) )
                            versionString += name + "|";
                    }

                    if( versionString.Length > 0 )
                    {
                        versionString = versionString.Substring( 0, versionString.Length - 1 );
                    }
                }
                catch( Exception )
                {}

                return versionString;
            }
        }

        private void InitPluginImageBitDepth()
        {
            _pluginImageBitDepth = 8;
            OperatingSystem ver = Environment.OSVersion;
            if ( ver.Platform == PlatformID.Win32NT )
            {
                if ( ver.Version.Major > 5 || (ver.Version.Major == 5 && ver.Version.Minor >= 1) )
                {
                    Trace.WriteLine( "Version.Major=" + ver.Version.Major +
                        ", Version.Minor=" + ver.Version.Minor + ", switching to 32-bit icons" );
                    _pluginImageBitDepth = 32;
                }

                IntPtr ptr1 = Win32Declarations.GetDC( IntPtr.Zero );
                int bitDepth = Win32Declarations.GetDeviceCaps( ptr1, Win32Declarations.BITSPIXEL );
                bitDepth *= Win32Declarations.GetDeviceCaps( ptr1, Win32Declarations.PLANES );
                Win32Declarations.ReleaseDC( IntPtr.Zero, ptr1 );
                Trace.WriteLine( "Screen bit depth is " + bitDepth );

                if ( ver.Version.Major == 5 && ver.Version.Minor == 0 && bitDepth == 32 )
                {
                    // set private static member Icon.bitDepth to 24 so that .NET won't
                    // try to load 32-bit icons which it does not support under Windows 2000
                    FieldInfo fi = typeof(Icon).GetField( "bitDepth", BindingFlags.NonPublic | BindingFlags.Static );
                    fi.SetValue( null, 24 );
                }
            }
        }

        private static int AddIconFromAssembly( string iconName, ImageList imgList )
        {
            Icon icon = LoadIconFromAssembly( iconName );
            if ( icon != null )
            {
                imgList.Images.Add( icon );
                return imgList.Images.Count - 1;
            }
            return -1;
        }

        internal static Icon LoadIconFromAssembly( string iconName )
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "OmniaMea.Icons." + iconName );
            if( stream != null )
            {
                return new Icon( stream );
            }
            Trace.WriteLine( "Failed to load icon " + iconName );
            return null;
        }

        private void LoadColorScheme( string schemeName )
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "OmniaMea.Icons." + schemeName );
            _colorScheme = new ColorScheme( Assembly.GetExecutingAssembly(), "OmniaMea.Icons.",
                _resourceIconManager.IconColorDepth );
            _colorScheme.Load( stream );

            _resourceBrowser.ColorScheme   = _colorScheme;
            _querySidebar.ColorScheme      = _colorScheme;
            _rightSidebar.ColorScheme      = _colorScheme;
        	_panelResourceTabsRow.ResourceTypeTabs.ColorScheme  = _colorScheme;
            _panelWorkspaceButtonsRow.ShortcutBar.ColorScheme       = _colorScheme;
        	_panelResourceTabsRow.SearchBar.ColorScheme        = _colorScheme;
            _leftSidebarSplitter.ColorScheme          = _colorScheme;
            _rightSidebarSplitter.ColorScheme = _colorScheme;

        	_panelWorkspaceButtonsRow.BackColor = _colorScheme.GetColor( "TopBar.TopBackground" );
        	_panelResourceTabsRow.BackColor = _colorScheme.GetColor( "TopBar.Background" );
        }

		/// <summary>
		/// After the form showing is complete, starts the index building
		/// process (if needed), and then initializes the controls in the view.
		/// </summary>
        private void OnFirstShow()
        {
            _textIndexExists = File.Exists( OMEnv.TermIndexFileName );

            _actionManager = new ActionManager( _mainMenu, _contextMenu, _resourceBrowser );
            _theEnvironment.RegisterComponentInstance( _actionManager );

#if !READER
            Core.UIManager.RegisterWizardPane( "Indexing period", CreateIndexPeriodPane, -1 );
#endif

            Core.UIManager.RegisterWizardPane( "User Information", MySelfPane.MySelfPaneCreator, 100 );

            _detailedProgress = RunningTests || _iniFile.ReadBool( "Omea", "DetailedProgress", false );

            if ( _excHandler != null )
            {
                RunWithSplashScreen( new MethodInvoker( OnStartupProgressExc ) );
            }
            else
            {
                _theEnvironment.SetProgressWindow( new MockProgressWindow( true ) );
                OnStartupProgress();
                _theEnvironment.SetProgressWindow( null );
            }

            if ( _querySidebar.ActiveSidebar != null )
            {
                _querySidebar.ActiveSidebar.FocusActivePane();
            }
        }

        protected override void OnVisibleChanged( EventArgs e )
        {
            base.OnVisibleChanged( e );
            if ( Core.State == CoreState.StartingPlugins )
            {
                Trace.WriteLine( "Switching to CoreState.Running" );
                _theEnvironment.SetState( CoreState.Running );
                _protocolHandlerManager.InvokeOpenUrl();
                _protocolHandlerManager.CheckProtocols( this );
                Trace.WriteLine( "Omea startup complete" );
            }
            if ( _needMaximize )
            {
                WindowState = FormWindowState.Maximized;
            }
        }

        internal void OnAppIdle( object sender, EventArgs e )
        {
            if ( _messageFilter == null || _uiManager == null )
            {
                return;
            }
            if ( _idleLastMessage != _messageFilter.LastMessageIndex )
            {
                _idleLastMessage = _messageFilter.LastMessageIndex;
                _uiManager.OnEnterIdle();
            }
        }

        public void RunWithProgressWindow( [NotNull] string progressTitle, [NotNull] Action action )
        {
            RunWithProgressWindow( progressTitle, false, action );
        }

    	private void RunWithProgressWindow([NotNull] string progressTitle, bool canMinimize, [NotNull] Action action)
    	{
    		var progressWindow = new ProgressWindow(canMinimize);
    		_theEnvironment.SetProgressWindow(progressWindow);
    		try
    		{
    			progressWindow.Text = progressTitle;
    			var job = new ActionJob(progressTitle, action);
    			progressWindow.Tag = job;

    			progressWindow.OnFirstShow += delegate { action(); };
    			progressWindow.ShowDialog(this);
    		}
    		finally
    		{
    			_theEnvironment.SetProgressWindow(null);
    		}
    	}

    	private void RunWithSplashScreen(Delegate method)
    	{
    		var splashScreen = new SplashScreen(_hwndUnmanagedPreSplash);
    		_theEnvironment.SetProgressWindow(splashScreen);
    		var uow = new DelegateJob(method, new object[] {});
    		splashScreen.OnFirstShow += delegate { uow.NextMethod(); };

    		try
    		{
    			splashScreen.ShowDialog(this);
    		}
    		finally
    		{
    			_theEnvironment.SetProgressWindow(null);
    		}
    	}

        private static AbstractOptionsPane CreateIndexPeriodPane()
        {
            return new IndexPeriodPane();
        }

		/// <summary>
		/// On first run, prompts the user if he wants to use quick indexing.
		/// On subsequent runs, if quick indexing was used, prompts if the user
		/// wants to do full indexing now.
		/// </summary>
        private void CheckQuickIndexing()
        {
            if ( _textIndexExists )
            {
                bool idleIndexing = _iniFile.ReadBool( "Startup", "IdleIndexing", true );
                if ( idleIndexing )
                    return;

                DateTime indexStartDate = _iniFile.ReadDate( "Startup", "IndexStartDate", DateTime.MinValue );
                if ( indexStartDate.Year != DateTime.MinValue.Year )
                {
                    foreach( string arg in Environment.GetCommandLineArgs() )
                    {
                        if ( arg.ToLower() == "/quickindex" || arg.ToLower() == "-quickindex" )
                        {
                            return;
                        }
                    }

                    if( Core.SettingStore.ReadBool( "Startup", "AskFullIndexing", true ) )
                    {
                        MessageBoxWithCheckBox.Result dr = MessageBoxWithCheckBox.ShowYesNo( this,
                            "The last time you ran " + ProductName + ", you only indexed the data since " +
                            indexStartDate.ToShortDateString() + ". Would you like to perform a full indexing now?",
                            ProductName, "Never &ask again", false );
                        if ( dr.IdPressedButton == (int)DialogResult.Yes )
                        {
                            _iniFile.WriteDate( "Startup", "IndexStartDate", DateTime.MinValue );
                        }
                        Core.SettingStore.WriteBool( "Startup", "AskFullIndexing", !dr.Checked );
                    }
                }
            }
        }

        private void OnStartupProgressExc()
        {
            try
            {
                OnStartupProgress();
            }
            catch( PluginLoader.CancelStartupException )
            {
                _cancelStart = true;
            }
            catch( Exception ex )
            {
                while( ex is AsyncProcessorException || ex is TargetInvocationException ||
                    ex is PicoInvocationTargetInitializationException )
                {
                    ex = ex.InnerException;
                }
                if ( !(ex is PluginLoader.CancelStartupException ) )
                {
                    _excHandler.ReportException( ex, ExceptionReportFlags.Fatal );
                }
                _cancelStart = true;
            }
        }

        private void OnStartupProgress()
        {
            bool forceWizard = false;
            bool unreadManagerEnabled = true;
            foreach( string arg in Environment.GetCommandLineArgs() )
            {
                string strippedArg = arg.Substring( 1 );
                if ( Utils.StartsWith( strippedArg, "notext", true ) )
                {
                    _noTextIndex = true;
                }
                else if ( Utils.StartsWith( strippedArg, "wizard", true ) )
                {
                    forceWizard = true;
                }
                else if( Utils.StartsWith( strippedArg, "nounread", true ) )
                {
                    unreadManagerEnabled = false;
                }

            }

            CheckQuickIndexing();

            bool dbOpened = false;
            while( !dbOpened )
            {
                dbOpened = true;
                try
                {
                    if ( !InitializeResourceStore() )
                    {
                        if( !CheckBackup( "Failed to open or repair databse" ))
                        {
                            return;
                        }
                        dbOpened = false;
                    }
                }
                catch( EndOfStreamException )
                {
                    if( !CheckBackup( "Database structure corruption detected" ) )
                    {
                        return;
                    }
                    dbOpened = false;
                }
                catch( ColumnAlreadyExistsException )
                {
                    if( !CheckBackup( "Database structure corruption detected" ) )
                    {
                        return;
                    }
                    dbOpened = false;
                }
                catch( IndexAlreadyExistsException )
                {
                    if( !CheckBackup( "Database structure corruption detected" ) )
                    {
                        return;
                    }
                    dbOpened = false;
                }
                catch( DataCorruptedException )
                {
                    if( !CheckBackup( "Fatal data corruption detected" ) )
                    {
                        return;
                    }
                    dbOpened = false;
                }
                catch( TableDoesNotExistException ex )
                {
                    if( !CheckBackup("Database structure corruption detected (table " + ex.ParamName + " is missing)" ) )
                    {
                        return;
                    }
                    dbOpened = false;
                }
                catch( MyPalStorage.IndexRebuildException ex)
                {
                    if( !CheckBackup( "Failed to rebuild database indexes: " + ex.Message ) )
                    {
                        return;
                    }
                    dbOpened = false;
                }
                catch( IOException ex )
                {
                    if( !CheckBackup( "Error opening database: " + ex.Message ) )
                    {
                        return;
                    }
                    dbOpened = false;
                }
                catch( CannotGetVersionInfoException ex )
                {
                    if( !CheckBackup( "Error opening database: " + ex.Message ) )
                    {
                        return;
                    }
                    dbOpened = false;
                }
                catch( BackwardIncompatibility )
                {
                    if( !CheckBackup( "Error opening database. Possible reason is attempt to open database created by a later version of Omea or database corruption" ) )
                    {
                        return;
                    }
                    dbOpened = false;
                }
            }

            _actionManager.RegisterCoreActions( _noTextIndex );
#if USAGE_LOG
            _networkJobProcessor.QueueJobAt( DateTime.Now.AddMinutes( 10 ), new MethodInvoker( LogManager.SubmitUsageLog ) );
#endif

            (Core.UnreadManager as UnreadManager).Enabled = unreadManagerEnabled;
        	_panelResourceTabsRow.ResourceTypeTabs.UnreadManager = Core.UnreadManager as UnreadManager;

            _protocolHandlerManager.RegisterResources();

            InitializeOptionsDialog();

            // make sure it's loaded now, while the resource store write access
            // is still allowed from the UI thread
            //IFileResourceManager manager = Core.FileResourceManager;

            if ( !_noTextIndex )
            {
                _textIndexManager = new TextIndexManager();
                _theEnvironment.RegisterComponentInstance( _textIndexManager );

                _textIndexManager.SetExceptionHandler( ReportAsyncException );
                _textIndexManager.IdlePeriod = _theEnvironment.IdlePeriod * 60000;
                MyPalStorage.Storage.TextIndexManager = _textIndexManager;
            }
            else
            {
                _theEnvironment.RegisterComponentInstance( new MockTextIndexManager() );
            }

            //  QuerySidebar must be initialized after the TextIndexManager since
            //  some Views decorators use Core.TextIndexManager.IndexLoaded event for
            //  their proper logic.
            InitializeQuerySidebar();

            PluginInterfaces pluginLoader = Core.PluginLoader as PluginInterfaces;
            pluginLoader.RegisterXmlConfigurationHandler( "actions", _actionManager.LoadXmlConfiguration );
            pluginLoader.RegisterXmlConfigurationHandler( "display-columns", ColumnConfigurationLoader.LoadXmlConfiguration );
            pluginLoader.RegisterXmlConfigurationHandler( "resource-icons", LoadXmlConfiguration_ResourceIcons );
            pluginLoader.LoadXmlConfiguration( Assembly.GetExecutingAssembly() );

            CategoryUIHandler categoryUIHandler = new CategoryUIHandler( Core.CategoryManager as CategoryManager );
            pluginLoader.RegisterResourceUIHandler( "Category", categoryUIHandler );
            pluginLoader.RegisterResourceUIHandler( "ResourceTreeRoot", new CategoryRootUIHandler(  ) );
			pluginLoader.RegisterResourceDragDropHandler( "Category", categoryUIHandler );
			pluginLoader.RegisterResourceDragDropHandler( "ResourceTreeRoot", categoryUIHandler );

            _resourceBrowser.HookFormattingRulesChange();

            InitializeCustomColumns();

            Core.UIManager.RegisterResourceSelectPane( "Category", typeof(CategorySelectPane) );
            pluginLoader.RegisterResourceDisplayer( "Fragment", new FragmentDisplayer() );
            pluginLoader.RegisterResourceTextProvider( "Fragment", new FragmentTextProvider() );
            Core.ResourceIconManager.RegisterResourceLargeIcon( "Fragment", LoadIconFromAssembly( "FragmentLarge.ico" ) );
            Core.PluginLoader.RegisterResourceDeleter( "Fragment", new DefaultResourceDeleter() );
            Core.FilterEngine.RegisterRuleApplicableResourceType( "Fragment" );
			ClippingNewspaperProvider.Register();
			Core.ResourceStore.PropTypes.Register("Url", PropDataType.String);

            //  Necessary to init CM before plugins' startup.
            ContactManager cm = (ContactManager)Core.ContactManager;

            Core.PluginLoader.RegisterPluginService( new SimpleMapiEmailService() );

            Core.RemoteControllerManager.AddRemoteCall( "Omea.CreateClipping.1",
                new CreateFragmentDelegate( CreateFragmentAction.CreateHtmlFragment ) );
            Core.RemoteControllerManager.AddRemoteCall( "Omea.CreateClippingSilent.1",
                new CreateFragmentDelegate( CreateFragmentAction.CreateHtmlFragmentSilent ) );

            _protocolHandlerManager.AddRemoteCall();

            Core.PluginLoader.RegisterResourceUIHandler( "TransientContainer", new TransientContainerUIHandler() );

            _theEnvironment.SetState( CoreState.StartingPlugins );
            if ( !_skipPlugins )
            {
                  pluginLoader.LoadPlugins();
            }
            if (_protocolHandlerManager.Registrations > 0 )
            {
                Core.UIManager.RegisterOptionsPane( "Omea", "Default Application", ProtocolHandlerOptionsPane.Creator,
                    "The Default Application options allow to configure the default applications for processing URLs." );
            }

            InitCachedPredicates();

            _trayIconManager.RegisterTypes();

            //  Invocation of views initializers is possible only when all
            //  plugins have been loaded and registered their interfaces,
            //  properties and resource types.
            ViewsInitializer.InvokeViewsConstructors();
            _trayIconManager.Initialize();
            cm.Initialize();

            //  Initialization of unread counters must follow the complete
            //  initialization process of ViewsInitializers since thery
            //  register runtime executor for CustomConditions and RuleActions.
            Core.UnreadManager.RegisterUnreadCountProvider( FilterManagerProps.ViewResName, new ViewUnreadCountProvider() );

            StartupWizard.RegisterTypes();

            UpdateBlobProperties();
            UpdateLongBodyProperties();

            _resourceJobProcessor.StartThread();
            _networkJobProcessor.ThreadPriority = ThreadPriority.BelowNormal;
            _networkJobProcessor.StartThread();

            _resourceJobProcessor.JobFinished += OnResourceJobFinished;

            DiskSpaceExhaustedForm.StartMonitoring();

            if ( !_skipWizard )
            {
                DialogResult result = UIManager.RunWizard( forceWizard );
                if( result == DialogResult.OK )
                {
                    SplashScreen splashScreen = Core.ProgressWindow as SplashScreen;
                    if ( splashScreen != null )
                    {
                        splashScreen.ResetElapsedTime();
                    }
                }
                else if( result != DialogResult.None )
                {
                    _iniFile.WriteBool( "MainForm", "StartupWizardCancelled", true );
                    _cancelStart = true;
                    Close();
                    return;
                }
            }

            LogManager.InitUsageLog();

            if ( Core.ProgressWindow != null )
            {
                Core.ProgressWindow.UpdateProgress( 0, "Starting plugins...", null );
            }
            pluginLoader.StartupPlugins();

            _workspaceManager = Core.WorkspaceManager as WorkspaceManager;
            _workspaceManager.RegisterWorkspaceType( "Category",
                new[] { (Core.CategoryManager as CategoryManager).PropCategory }, WorkspaceResourceType.Filter  );
            _workspaceManager.SetWorkspaceTabName( "Category", "Categories" );
            _workspaceManager.RegisterWorkspaceType( "ResourceTreeRoot", new int[] {}, WorkspaceResourceType.Folder );
            _workspaceManager.SetWorkspaceTabName( "ResourceTreeRoot", "Categories" );
            _workspaceManager.RegisterWorkspaceSelectorFilter( "Category", new CategoryNodeFilter(), null );
            Core.ResourceAP.RunJob( new MethodInvoker( _workspaceManager.CheckRebuildWorkspaceLinks ) );

            FlagColumn.FillImagesAndActions();

            // register the component after the plugins have started up, to make sure
            // all the plugins will be able to override it
            _actionManager.RegisterActionComponent( new IEBrowserAction( "Refresh" ), "Refresh", null, null );
            _actionManager.RegisterActionComponent( new MarkAllAsReadAction(), "MarkAllRead", null, null );
            _actionManager.RegisterActionComponent( new SendCurrentUrlAction(), "SendByMail", null, null );
            _actionManager.RegisterMainMenuAction( new OpenUrlAction( "mailto:feedback.omea@jetbrains.com"), "HelpWebsiteActions",
                                                   ListAnchor.First, "Send Questions or Feedback by E-mail", null, null, null );

#if READER
            _actionManager.RegisterMainMenuAction( new OpenUrlAction( "http://www.jetbrains.com/omea/"),
                "HelpWebsiteActions", ListAnchor.First, "Upgrade to Omea Pro", null, null );
            _actionManager.RegisterMainMenuAction( new OpenUrlAction( "http://www.jetbrains.com/omea_reader/"),
                "HelpWebsiteActions", ListAnchor.First, "Product Web Page", null, null );

#else
            _actionManager.RegisterMainMenuAction( new OpenUrlAction( "http://www.jetbrains.com/omea/"), "HelpWebsiteActions",
                                                   ListAnchor.First, "Product Web Page", null, null, null );
#endif

            if ( Core.ProgressWindow != null )
            {
                Core.ProgressWindow.UpdateProgress( 0, "Initializing view…", null );
            }

			// Init the Avalon application engine
			Dispatcher.CurrentDispatcher.UnhandledException += (sender, args) => { args.Handled = true; Core.ReportException(args.Exception, false); };
			AvalonOperationCrisp.Execute();	// Ensure the fonts are not affected by reduced antialiasing
			new System.Windows.Application();	// Singleton Avalon application
			System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;	// Don't kill app after popup Avalon windows close

            _rightSidebar.PopulateViewPanes();
            _rightSidebar.CurrentState = SidebarState.RestoreFromIni( "RightSidebar" );
        	_panelResourceTabsRow.ResourceTypeTabs.RestoreTabStates();
        	_panelResourceTabsRow.SearchBar.Visible = !_noTextIndex;

            if (ObjectStore.ReadInt("Columns", "Version", 0) < 4)
			{
				Core.ResourceAP.RunJob( "DeleteAllStates", ResourceListState.DeleteAllStates );
				ObjectStore.WriteInt( "Columns", "Version", 4 );
			}

        	_panelResourceTabsRow.ResourceTypeTabs.DefaultViewResource =
                MyPalStorage.Storage.FindUniqueResource( FilterManagerProps.ViewResName, "Name", "Today" );

            // select first tab ("Email" by default)
        	_panelResourceTabsRow.ResourceTypeTabs.StartupComplete = true;
        	_panelResourceTabsRow.ResourceTypeTabs.RegisterTabSwitchActions();
            if ( _excHandler != null )
            {
            	_panelResourceTabsRow.ResourceTypeTabs.SelectLastTab();
            }

            Trace.WriteLine( "Initializing criteria..." );
            (Core.FilterEngine as FilterEngine).InitializeCriteria();

            Trace.WriteLine( "Setting unread state..." );
            ViewsCategoriesPane defaultTreePane = _querySidebar.DefaultViewPane as ViewsCategoriesPane;
            if ( defaultTreePane != null )
            {
                defaultTreePane.UnreadState = (Core.UnreadManager as UnreadManager).CurrentUnreadState;
            }

            ResourceBrowser resourceBrowser = Core.ResourceBrowser as ResourceBrowser;
            if ( resourceBrowser.BrowseStack.Count > 0 )
            {
                AbstractBrowseState state = resourceBrowser.BrowseStack.Peek( 0 );
                resourceBrowser.BrowseStack.Clear();  // OM-10094, OM-10139
                resourceBrowser.BrowseStack.Push( state );
            }

            Trace.WriteLine( "Rebuilding shortcut bar..." );
            _panelWorkspaceButtonsRow.ShortcutBar.RebuildShortcutBar();

            UpdateManager.QueueUpdateCheck();

			_panelResourceTabsRow.SearchBar.EnableControls( false );
			if( _textIndexManager != null )
            {
            	_panelResourceTabsRow.SearchBar.SetText( "Loading index..." );
                _textIndexManager.ThreadStarted += AsyncProcessor_ThreadStarted;
            }

            _miExitOmniaMea.Enabled = true;
        }

    	private void LoadXmlConfiguration_ResourceIcons(Assembly asmPlugin, XmlElement xmlElement)
    	{
    		var iconProvider = new ConfigurableIconProvider(asmPlugin, xmlElement);
    		Core.ResourceIconManager.RegisterResourceIconProvider(iconProvider.ResourceTypes, iconProvider);
    		foreach(string sResType in iconProvider.ResourceTypes)
    			Core.ResourceIconManager.RegisterOverlayIconProvider(sResType, iconProvider);
    	}

    	private bool CheckBackup( string error )
        {
            DateTime time = MyPalStorage.BackupTime();
            if( time == DateTime.MinValue )
            {
                MessageBox.Show( this, error + '.', "Database is Lost", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
            else
            {
                if( MessageBox.Show( this, error + ".\r\n\r\n" +
                    "Last backup of your database was made at " + time +
                    ". Would you like to restore the database from backup?",
                    "Restore Database", MessageBoxButtons.YesNo, MessageBoxIcon.Error ) == DialogResult.Yes )
                {
                    try
                    {
                        MyPalStorage.RestoreFromBackup();
                        return true;
                    }
                    catch( Exception ex )
                    {
                        MessageBox.Show( this, "Restoring database failed: " + ex.Message, "Database is Lost", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    }
                }
            }
            Close();
            _cancelStart = true;
            return false;
        }

        private static void OnResourceJobFinished( object sender, EventArgs e )
        {
            MyPalStorage.Storage.FlushTables();
        }

        private void AsyncProcessor_ThreadStarted( object sender, EventArgs e )
        {
            _textIndexManager.ThreadStarted -= AsyncProcessor_ThreadStarted;
            PopulateSearchCtrl();
        }

        private void PopulateSearchCtrl()
        {
            if( Core.UserInterfaceAP.IsOwnerThread )
            {
            	_panelResourceTabsRow.SearchBar.EnableControls( true );
            	_panelResourceTabsRow.SearchBar.Populate();
            }
            else
            {
                Core.UserInterfaceAP.QueueJob( new MethodInvoker( PopulateSearchCtrl ) );
            }
        }

        private static bool CheckOlderBuildDB()
        {
            int creatorBuild = ParseBuild( MyPalStorage.DBCreatorBuild );
            int curBuild = Core.ProductVersion.Build;
            if ( creatorBuild > 0 && curBuild > 0 )
            {
                if ( curBuild < creatorBuild )
                {
                    DialogResult dr = MessageBox.Show(
                        "You are trying to run " + Core.ProductFullName + " build " + curBuild +
                        " on the database created by a newer version of the product, build " + creatorBuild +
                        ".\r\nThis is NOT SUPPORTED and can lead to problems including DATA CORRUPTION.\r\nDo you wish to continue?",
                        Core.ProductFullName, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation );
                    return dr == DialogResult.No;
                }
            }
            return false;
        }

        private static void InitCachedPredicates()
        {
            MyPalStorage store = (MyPalStorage) Core.ResourceStore;
            store.CachePredicate( store.FindResourcesWithPropLive( null, Core.Props.IsUnread ) );
            store.CachePredicate( store.FindResourcesWithPropLive( null, Core.Props.IsDeleted ) );
        }

        private static int ParseBuild( string build )
        {
            if ( build == null )
            {
                return 0;
            }

            int pos = build.IndexOf( '.' );
            if ( pos > 0 )
            {
                build = build.Substring( 0, pos );
            }
            try
            {
                return Int32.Parse( build );
            }
            catch( FormatException )
            {
                return 0;
            }
        }

        private void InitializeAsyncProcessors()
        {
            int idlePeriod = _theEnvironment.IdlePeriod * 60000;

            _resourceJobProcessor = new AsyncProcessor( false );
            _resourceJobProcessor.ThreadName = "Resource AsyncProcessor";
            _resourceJobProcessor.ExceptionHandler = ReportAsyncException;
            _resourceJobProcessor.IdlePeriod = idlePeriod;
            _resourceJobProcessor.ThreadStarted += SetResourceOwnerThread;
            _networkJobProcessor = new AsyncProcessor( false );
            _networkJobProcessor.ThreadName = "Network AsyncProcessor";
            _networkJobProcessor.ExceptionHandler = ReportAsyncException;
            _networkJobProcessor.IdlePeriod = idlePeriod;

            _theEnvironment.SetAsyncProcessors( _resourceJobProcessor, _networkJobProcessor );
        }

        private void SetResourceOwnerThread( object sender, EventArgs e )
        {
            MyPalStorage.Storage.OwnerThread = _resourceJobProcessor.Thread;
        }

        private bool InitializeResourceStore()
        {
            MyPalStorage.ResourceCacheSize = _iniFile.ReadInt( "ResourceStore", "ResourceCacheSize", 4096 );
            Database.DBIndex._cacheSizeMultiplier =
                _iniFile.ReadInt( "ResourceStore", "DBindexCacheSizeMultiplier", 2 );
            MyPalStorage.TraceOperations = _iniFile.ReadBool( "ResourceStore", "TraceOperations", false );
            MyPalStorage.SetProgressWindow( Core.ProgressWindow );

            bool fullRepairRequired = _iniFile.ReadBool( "ResourceStore", "FullRepairRequired", false );
            if ( !MyPalStorage.DatabaseExists() )
            {
                MyPalStorage.CreateDatabase();
                if ( _excHandler != null )
                {
                    _excHandler.NewDatabase = true;
                }
            }
            else
            {
                if ( CheckOlderBuildDB() )
                {
                    return false;
                }
                if ( fullRepairRequired )
                {
                    if ( !CheckRunFullRepair() )
                    {
                        return false;
                    }
                    _iniFile.WriteBool( "ResourceStore", "FullRepairRequired", false );
                }
            }

            try
            {
                MyPalStorage.OpenDatabase();
            }
            catch( BadIndexesException )
            {
                if ( !CheckRunFullRepair() )
                {
                    return false;
                }
                MyPalStorage.OpenDatabase();
            }

            _theEnvironment.RegisterComponentInstance( MyPalStorage.Storage );
            if ( _iniFile.ReadBool( "ResourceStore", "RepairRequired", false ) && !fullRepairRequired )
            {
                ResourceStoreRepair repair = new ResourceStoreRepair( null );
                repair.RepairProgress += Repair_OnRepairProgress;
                repair.FixErrors = true;
                repair.RepairRestrictions();
                _iniFile.WriteBool( "ResourceStore", "RepairRequired", false );
            }

            if ( _excHandler != null )
            {
                if ( !_excHandler.NewDatabase )
                {
                    _excHandler.DBCreatorBuild = MyPalStorage.Storage.BuildNumber;
                    _excHandler.IndexesRebuilt = MyPalStorage.Storage.IndexesRebuilt;
                }
            }

            MyPalStorage.Storage.BuildNumber = Core.ProductVersion.Build + "." + Core.ProductVersion.Revision;
            MyPalStorage.Storage.IndexCorruptionDetected += HandleIndexCorruptionDetected;
            MyPalStorage.Storage.IOErrorDetected += HandleResourceStoreIOError;
            MyPalStorage.QueueDefragmentTableIndexes();
            MyPalStorage.QueueIdleDatabaseBackup();
            InitializeCoreResources();
            return true;
        }

        private static bool CheckRunFullRepair()
        {
            Exception ex = MyPalStorage.RunFullRepair( Repair_OnRepairProgress );
            if ( ex != null && MyPalStorage.BackupTime() == DateTime.MinValue )
            {
                MessageBox.Show( "Fatal error when repairing the database: " + ex +
                    "\nPlease email feedback.omea@jetbrains.com to get help with repairing the database.",
                                 Core.ProductFullName, MessageBoxButtons.OK, MessageBoxIcon.Error );
                return false;
            }
            return true;
        }

        private static void Repair_OnRepairProgress( object sender, RepairProgressEventArgs e )
        {
            if ( Core.ProgressWindow != null )
            {
                Core.ProgressWindow.UpdateProgress( 0, e.Message, "" );
            }
        }

        private void HandleIndexCorruptionDetected( object sender, EventArgs e )
        {
            const string title = "Database Index Corruption";
            string message = Core.ProductFullName + " has detected a corruption of the database indexes. " +
                Core.ProductFullName + " will now shutdown. Please restart " + Core.ProductFullName +
                " to get the indexes rebuilt.";

            if ( _excHandler != null )
            {
                _excHandler.IgnoreAllExceptions();
            }

            CheckReportResourceStoreError( title, message );
        }

        private void HandleResourceStoreIOError( object sender, ThreadExceptionEventArgs e )
        {
            const string title = "Database Reading or Writing Error";
            string message = Core.ProductFullName + " has detected an error when reading or writing the database:\r\n" +
                e.Exception.Message +
                "\r\n" + Core.ProductFullName +
                " will now shutdown. ";

            if ( e.Exception is UnauthorizedAccessException )
            {
                message += "Please ensure that no other programs are accessing the folder containing the database and restart " +
                    Core.ProductFullName + ".";
            }
            else
            {
                message += "Please ensure that the disk containing the database is connected and working properly and restart " +
                    Core.ProductFullName + ".";
            }

            if ( _excHandler != null )
            {
                _excHandler.IgnoreAllExceptions();
            }

            CheckReportResourceStoreError( title, message );
        }

        private void CheckReportResourceStoreError( string title, string message )
        {
            SaveRepairRequired();
            if ( Core.UserInterfaceAP.IsOwnerThread )
            {
                ReportResourceStoreError( title, message, true );
            }
            else
            {
                Core.UserInterfaceAP.QueueJob( JobPriority.Immediate,
                    new ReportErrorDelegate( ReportResourceStoreError ), title, message, false );
                if ( Core.State == CoreState.Initializing || Core.State == CoreState.StartingPlugins )
                {
                    throw new PluginLoader.CancelStartupException();
                }
            }
        }

        private delegate void ReportErrorDelegate( string title, string message, bool arg );

        private void ReportResourceStoreError( string title, string message, bool uiThread )
        {
            CoreState state = Core.State;
            MessageBox.Show( this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Stop );
            Close();
            if ( uiThread && ( state == CoreState.Initializing || state == CoreState.StartingPlugins ) )
            {
                throw new PluginLoader.CancelStartupException();
            }
        }

        private void InitializeCoreResources()
        {
            MyPalStorage store = MyPalStorage.Storage;
#pragma warning disable 168
            // make sure core props are initialized while we have access to resource store
            ICoreProps props = Core.Props;
#pragma warning restore 168
            store.PropTypes.Register( "NoFormat", PropDataType.Bool, PropTypeFlags.Internal );
            store.PropTypes.Register( "Directory", PropDataType.String );
            store.PropTypes.Register( "IsClippingFakeProp", PropDataType.Bool, PropTypeFlags.Internal );
            int propFragment = store.PropTypes.Register( "Fragment", PropDataType.Link, PropTypeFlags.DirectedLink );
            store.PropTypes.RegisterDisplayName( propFragment, "Clipping Source", "Clipping" );

            Core.CategoryManager.RootCategory.SetProp( "CategoryExpanded", 1 );

            store.ResourceTypes.Register( "Fragment", "Clipping", "Subject", ResourceTypeFlags.Normal );
            UnknownFileResource.Register();
            ShortcutProps.Register();

            NotifyMeDlg.RegisterTypes();

            store.ResourceTypes.Register( "Folder", "Name",
                ResourceTypeFlags.Internal | ResourceTypeFlags.ResourceContainer | ResourceTypeFlags.NoIndex );
			store.ResourceTypes.Register( "TransientContainer", "Name",
				ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );

            store.PropTypes.Register( "AnnotationLastModifiedDate", PropDataType.Date );

            // the check is done so that the user can delete the Linked To property, and it won't
            // get re-registered
            bool linkedToRegistered = _iniFile.ReadBool( "Resources", "LinkedToRegistered", false );
            if ( !linkedToRegistered )
            {
                _iniFile.WriteBool( "Resources", "LinkedToRegistered", true );
                store.PropTypes.Register( "Linked To", PropDataType.Link );

                IResource res = store.FindUniqueResource( "PropType", "Name", "Linked To" );
                res.SetProp( "Custom", 1 );
            }

            store.ResourceTypes.Register( "DeletedResourcePlaceholder", "",
                ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );
            if ( store.GetAllResources( "DeletedResourcePlaceholder" ).Count == 0 )
            {
                store.NewResource( "DeletedResourcePlaceholder" );
            }
        }

        private static void UpdateBlobProperties()
        {
            if( _forceBlobsUpdate || ObjectStore.ReadBool( "Startup", "NeedUpdateBlobProperties", true ) )
            {
                string[] blobFiles = Directory.GetFiles( MyPalStorage.DBPath, "*.blob" );
                if( blobFiles.Length > 0 )
                {
                    IResourceStore store = Core.ResourceStore;
                    IProgressWindow pw = Core.ProgressWindow;
                    ArrayList allResourceTypes = new ArrayList();
                    foreach( IResourceType type in store.ResourceTypes )
                    {
                        allResourceTypes.Add( type.Name );
                    }

                    string[] allResourceTypeNames = (string[]) allResourceTypes.ToArray( typeof( String ) );
                    IResourceList allResources = store.GetAllResources( allResourceTypeNames );
                    int count = allResources.Count;
                    int i = 0;
                    JetMemoryStream tempStream = new JetMemoryStream();
                    byte[] buffer = new byte[ 4096 ];
                    ArrayList blobProps = new ArrayList();

                    foreach( IResource res in allResources.ValidResources )
                    {
                        if( pw != null && ( i++ & 0xff ) == 0 )
                        {
                            pw.UpdateProgress( i * 100 / count, "Maintaining database: updating binary large objects...", null );
                        }
                        blobProps.Clear();
                        foreach( IResourceProperty prop in res.Properties )
                        {
                            if( prop.DataType == PropDataType.Blob )
                            {
                                // save in the list since enumerated collection can't be changed
                                blobProps.Add( prop );
                            }
                        }
                        foreach( IResourceProperty prop in blobProps )
                        {
                            int id = prop.PropId;
                            //
                            // This strange (at the first look) code just rewrites blob properties
                            // Newly set properties should be written into the blob file system
                            //
                            Stream stream = res.GetBlobProp( id );
                            tempStream.SetLength( 0 );
                            int readBytes;
                            while( ( readBytes = stream.Read( buffer, 0, buffer.Length ) ) > 0 )
                            {
                                tempStream.Write( buffer, 0, readBytes );
                            }
                            res.DeleteProp( id );
                            res.SetProp( id, tempStream );
                        }
                    }

                    foreach( string blobFile in blobFiles )
                    {
                        IOTools.DeleteFile( blobFile );
                    }

                    ObjectStore.WriteBool( "Startup", "NeedUpdateBlobProperties", false );
                }
            }
        }

        private static void UpdateLongBodyProperties()
        {
            if( Core.ResourceStore.PropTypes.Exist( "LongBody" ) )
            {
                IResourceStore store = Core.ResourceStore;
                IProgressWindow pw = Core.ProgressWindow;
                int obsoleteId = Core.ResourceStore.PropTypes[ "LongBody" ].Id;
                ArrayList allResourceTypes = new ArrayList();
                foreach( IResourceType type in store.ResourceTypes )
                {
                    allResourceTypes.Add( type.Name );
                }

                string[] allResourceTypeNames = (string[]) allResourceTypes.ToArray( typeof( String ) );
                IResourceList allResources = store.GetAllResources( allResourceTypeNames );
                int count = allResources.Count;
                int i = 0;
                foreach( IResource res in allResources.ValidResources )
                {
                    if( pw != null && ( i++ & 0xff ) == 0 )
                    {
                        pw.UpdateProgress( i * 100 / count, "Maintaining database: updating messages/posts contents...", null );
                    }
                    if( res.HasProp( obsoleteId ) )
                    {
                        res.SetProp( Core.Props.LongBody, res.GetPropText( obsoleteId ) );
                    }
                }
                Core.ResourceStore.PropTypes.Delete( obsoleteId );
            }
        }

        static private ResourceFlag AddResourceFlag( string flagID, string name, string iconName )
        {
            return new ResourceFlag( flagID, name, "OmniaMea", iconName );
        }
        static private ResourceFlag AddResourceFlag( string flagID, string name, string iconName, ResourceFlag nextState )
        {
            ResourceFlag flag = AddResourceFlag( flagID, name, iconName );
            flag.NextStateFlag = nextState;
            return flag;
        }

        private void InitializeCustomColumns()
        {
            DisplayColumnManager colMgr = _theEnvironment.DisplayColumnManager as DisplayColumnManager;

            //  Annotation Column and Icon Provider

            colMgr.RegisterDisplayColumn( null, 10000, new ColumnDescriptor( "Annotation", 20, ColumnDescriptorFlags.FixedSize ) );
            colMgr.RegisterAllTypesMultiLineColumn( Core.Props.Annotation, 1, 1, -20, 20,
                MultiLineColumnFlags.AnchorRight, SystemColors.ControlText, HorizontalAlignment.Left );

            int propAnnotation = MyPalStorage.Storage.GetPropId( "Annotation" );
            ImageListColumn annotColumn = new AnnotationColumn( propAnnotation );
            annotColumn.SetAnyValueIcon( LoadIconFromAssembly( "annotation.ico" ) );
            annotColumn.SetHeaderIcon( LoadIconFromAssembly( "annotation2.ico" ) );
            annotColumn.SetNoValueIcon( LoadIconFromAssembly( "AnnotationNoProp.ico" ) );
            colMgr.RegisterCustomColumn( propAnnotation, annotColumn );

            //  Flag Column

            ResourceFlag.RegisterTypes();
            ResourceFlag completedFlag = AddResourceFlag( "CompletedFlag", "Completed Flag", "OmniaMea.Icons.CompletedFlag.ico" );
            ResourceFlag.DefaultFlag = AddResourceFlag( "RedFlag", "Red Flag", "OmniaMea.Icons.RedFlag.ico", completedFlag );
            AddResourceFlag( "PurpleFlag", "Purple Flag", "OmniaMea.Icons.PurpleFlag.ico", completedFlag );
            AddResourceFlag( "OrangeFlag", "Orange Flag", "OmniaMea.Icons.OrangeFlag.ico", completedFlag );
            AddResourceFlag( "GreenFlag", "Green Flag", "OmniaMea.Icons.GreenFlag.ico", completedFlag );
            AddResourceFlag( "YellowFlag", "Yellow Flag", "OmniaMea.Icons.YellowFlag.ico", completedFlag );
            AddResourceFlag( "BlueFlag", "Blue Flag", "OmniaMea.Icons.BlueFlag.ico", completedFlag );

            ColumnDescriptor flagColDesc = new ColumnDescriptor( "Flag", 20, ColumnDescriptorFlags.FixedSize );
            flagColDesc.CustomComparer = new FlagComparer();
            colMgr.RegisterDisplayColumn( null, 9000, flagColDesc );

            IResourceIconProvider flagIconProvider = new FlagIconProvider();
            _resourceIconManager.RegisterResourceIconProvider( "Flag", flagIconProvider );
            _flagColumn = new FlagColumn( flagIconProvider );
            colMgr.RegisterCustomColumn( ResourceFlag.PropFlag, _flagColumn );

            colMgr.RegisterAllTypesMultiLineColumn( ResourceFlag.PropFlag, 1, 1, -40, 20,
                MultiLineColumnFlags.AnchorRight, SystemColors.ControlText, HorizontalAlignment.Left );

            //  Category Icon Provider and Column

            int  catLinkId = MyPalStorage.Storage.GetPropId( "Category" );

            IResourceIconProvider categoryIconProvider = new CategoryIconProvider();
            _resourceIconManager.RegisterResourceIconProvider( "Category", categoryIconProvider );

            LinkIconManagerColumn categories = new CategoriesColumn();
            categories.SetHeaderIcon( LoadIconFromAssembly( "categoriesBW.ico" ) );
            categories.ShowTooltips = true;
            colMgr.RegisterDisplayColumn( null, 8000, new ColumnDescriptor( "Category", 20, ColumnDescriptorFlags.FixedSize ) );
            colMgr.RegisterCustomColumn( catLinkId, categories );
            colMgr.RegisterAllTypesMultiLineColumn( catLinkId, 1, 1, -60, 20,
                MultiLineColumnFlags.AnchorRight, SystemColors.ControlText, HorizontalAlignment.Left );

            ColumnFormatter.GetInstance().RegisterFormatters();
        }

        private static void ReportAsyncException( Exception e )
        {
            if ( e is AsyncProcessorException )
            {
                Trace.WriteLine( "AsyncProcessorException: " + e );
                Core.ReportException( e.InnerException, 0 );
            }
            else
            {
                Core.ReportException( e, 0 );
            }
        }

        internal void ForceClose()
        {
            _forceClose = true;
            _cancelStart = true;
            Close();
            _excHandler.IgnoreAllExceptions();
        }

        private void OnClosing( object sender, CancelEventArgs e )
        {
            Debug.WriteLine( "MainFrame closing" );
            if ( !_forceClose )
            {
                e.Cancel = _uiManager.IsCancelMainWindowClosing();
            }
        }

        private void OnClosed( object sender, EventArgs e )
        {
            if ( _excHandler != null )
            {
                try
                {
                    if ( Core.State != CoreState.ShuttingDown )
                    {
						Trace.WriteLine( "--- Closing ---" );

                        _miExitOmniaMea.Enabled = false;
                        RunWithProgressWindow( "Closing", false, OnShutdownProgress);
						LogManager.CloseUsageLog();
					}
                }
                catch ( Exception exc )
                {
                    Trace.WriteLine( exc.Message );
                }
            }
            else
            {
                TestShutdown();
            }
        }

        internal void TestShutdown()
        {
            _theEnvironment.SetProgressWindow( new MockProgressWindow( true ) );
            OnShutdownProgress();
            _theEnvironment.SetProgressWindow( null );
        }

        private bool _shutdownStarted = false;

        private void OnShutdownProgress()
        {
            if( !_shutdownStarted )
            {
                _shutdownStarted = true;
                try
                {
                    _theEnvironment.SetState( CoreState.ShuttingDown );
                    Core.ProgressWindow.UpdateProgress( 0, "Closing...", null );

                    if( _rcServer != null )
                    {
                        _rcServer.Stop();
                    }

                    if ( _actionManager != null )
                    {
                        _actionManager.EndUpdateActions();
                    }

                    //_resourceJobProcessor.RunJob( new MethodInvoker( _panelWorkspaceButtonsRow.WorkspaceButtonsManager.SaveVisibilityList ) );

                    if ( _detailedProgress )
                    {
                        Core.ProgressWindow.UpdateProgress( 0, "Stopping TextIndexManager...", null );
                    }
                    if ( _textIndexManager != null )
                    {
                        _textIndexManager.Dispose();
                    }
                    if ( _networkJobProcessor != null )
                    {
                        _networkJobProcessor.QueueEndOfWork();
                    }

                    _resourceBrowser.DisposeDisplayPane();

                    SaveFormSettings();
                	_panelResourceTabsRow.ResourceTypeTabs.SaveTabStates();
                    _rightSidebar.CurrentState.SaveToIni( "RightSidebar" );
                    _resourceBrowser.Shutdown();

					((PluginInterfaces)Core.PluginLoader).ShutdownPlugins(_detailedProgress);

                    if ( _detailedProgress )
                    {
                        Core.ProgressWindow.UpdateProgress( 0, "Stopping network AsyncProcessor...", null );
                    }

                    if ( _networkJobProcessor != null )
                    {
                        _networkJobProcessor.Dispose();
                    }

                    // wait until resource operations invoked by Dispose() of plugins are finished
                    if ( _detailedProgress )
                    {
                        Core.ProgressWindow.UpdateProgress( 0, "Stopping resource AsyncProcessor...", null );
                    }
                    if ( _resourceJobProcessor != null )
                    {
                        _resourceJobProcessor.QueueEndOfWork();
                        _resourceJobProcessor.ThreadFinished += _resourceUOWProcessor_ThreadFinished;
                        _resourceJobProcessor.WaitUntilFinished();
                        _resourceJobProcessor.Dispose();
                    }

                    if ( _detailedProgress )
                    {
                        Trace.WriteLine( "Closing text index environment..." );
                    }
                    OMEnv.Cleanup();
                }
                catch( Exception ex )
                {
                    _excHandler.ReportException( ex, 0 );
                }
                _uiAsyncProcessor.QueueEndOfWork();
            }
        }

        private void _resourceUOWProcessor_ThreadFinished( object sender, EventArgs e )
        {
            MyPalStorage.Storage.DefragmentDatabase( Core.ProgressWindow );
            if ( _detailedProgress )
            {
                Core.ProgressWindow.UpdateProgress( 0, "Closing database...", null );
            }
            SaveRepairRequired();
            MyPalStorage.CloseDatabase();
        }

        private void SaveRepairRequired()
        {
            _iniFile.WriteBool( "ResourceStore", "RepairRequired", MyPalStorage.Storage.RepairRequired );
            _iniFile.WriteBool( "ResourceStore", "FullRepairRequired", MyPalStorage.Storage.FullRepairRequired );
        }

        private void SaveFormSettings()
        {
            bool maximized = (WindowState == FormWindowState.Maximized);
            _iniFile.WriteBool( "MainForm", "Maximized", maximized );
            if ( ClientSize.Width > 0 && ClientSize.Height > 0 )
            {
                int x = Location.X,  y = Location.Y;
                // ensure window maximizes in proper screen
                if ( maximized )
                {
                    x += 8;
                    y += 8;
                }

                _iniFile.WriteInt( "MainForm", "X", x);
                _iniFile.WriteInt( "MainForm", "Y", y );
                _iniFile.WriteInt( "MainForm", "Width", Width );
                _iniFile.WriteInt( "MainForm", "Height", Height );
                _iniFile.WriteInt( "MainForm", "RightSidePaneWidth", _rightSidebar.ExpandedWidth );
                _iniFile.WriteInt( "MainForm", "QuerySidebarWidth", _querySidebar.ExpandedWidth );
            }
            _iniFile.WriteBool( "MainForm", "ShortcutBarVisible", ShortcutBarVisible );
            _iniFile.WriteBool( "MainForm", "WorkspaceBarVisible", WorkspaceButtonsVisible );

			// Save the components' settings
			_panelResourceTabsRow.SerializeSettings( true );
			_panelWorkspaceButtonsRow.SerializeSettings( true );
        }

		/// <summary>
		/// Calculates the factor by which the saved size values should be divided
		/// to get correct results when the form has been auto-scaled.
		/// </summary>
        private void CalcScaleFactor()
        {
            Size baseSize = AutoScaleBaseSize;
            SizeF curSizeF = GetAutoScaleSize( Font );
            Size curSize = new Size( (int) curSizeF.Width, (int) curSizeF.Height );

            _scaleFactor = new SizeF(
                AdjustScale( (float) curSize.Width / (float) baseSize.Width ),
                AdjustScale( (float) curSize.Height / (float) baseSize.Height ) );
            _theEnvironment.SetScaleFactor( _scaleFactor );

            Debug.WriteLine( "ScaleFactor.Width=" + _scaleFactor.Width );
            Debug.WriteLine( "ScaleFactor.Height=" + _scaleFactor.Height );
        }

		/// <summary>
		/// Copy of a private method in System.Windows.Forms.Form.
		/// </summary>
        private static float AdjustScale( float scale )
        {
            if (scale < 0.92)
            {
                return (scale + 0.08f);
            }
            if (scale < 1)
            {
                return 1;
            }
            if (scale > 1.01)
            {
                return (scale + 0.08f);
            }
            return scale;
        }

        private void RestoreFormSettings()
        {
            if ( _iniFile == null )
            {
                return;
            }
            using( new LayoutSuspender( this ) )
        	{
                int x = _iniFile.ReadInt( "MainForm", "X", -1 );
        		int y = _iniFile.ReadInt( "MainForm", "Y", -1 );
        		if( x > 0 && y > 0 )
        		{
        			Location = new Point( x, y );
        		}
        		int width = _iniFile.ReadInt( "MainForm", "Width", -1 );
        		int height = _iniFile.ReadInt( "MainForm", "Height", -1 );
        		if( width > 0 && height > 0 )
        		{
        			Size = new Size(
        				(int) (width / _scaleFactor.Width),
        				(int) (height / _scaleFactor.Height) );
        		}
                else
        		{
        		    ClientSize = new Size( Screen.PrimaryScreen.WorkingArea.Width * 19 / 20,
                        Screen.PrimaryScreen.WorkingArea.Height * 9 / 10 );
        		}

        		bool maximized = _iniFile.ReadBool( "MainForm", "Maximized", false );
        		if( maximized && WindowState == FormWindowState.Normal )
        		{
      				_needMaximize = true;
        		}

        		int querySidebarWidth = _iniFile.ReadInt( "MainForm", "QuerySidebarWidth", 240 );
        		if( querySidebarWidth > 0 )
        		{
        			_querySidebar.ExpandedWidth = (int) (querySidebarWidth / _scaleFactor.Width);
        		}
        		int rightSidePanelWidth = _iniFile.ReadInt( "MainForm", "RightSidePaneWidth", 200 );
        		if( rightSidePanelWidth > 0 )
        		{
        			int maxWidth = Width - _querySidebar.Width - 10;
        			if( rightSidePanelWidth > maxWidth )
        			{
        				rightSidePanelWidth = maxWidth;
        			}
        			_rightSidebar.ExpandedWidth = (int) (rightSidePanelWidth / _scaleFactor.Width);
        		}

        		LeftSidebarExpanded = _iniFile.ReadBool( "MainForm", "LeftSidebarExpanded", true );

        		// Load the components' settings
        		_panelResourceTabsRow.SerializeSettings( false );
        		_panelWorkspaceButtonsRow.SerializeSettings( false );
        	}
        }

    	private void HandleLeftSidebarCollapsedChanged( object sender, EventArgs e )
        {
            _iniFile.WriteBool( "MainForm", "LeftSidebarExpanded", LeftSidebarExpanded );
        }

        private void HandleRightSidebarCollapsedChanged( object sender, EventArgs e )
        {
            _iniFile.WriteBool( "MainForm", "RightSidebarExpanded", RightSidebarExpanded );
        }

        //
        // -- IUIManager implementation --------------------------------------
        //

		/// <summary>
		/// Initializes the panes of the QuerySidebar.
		/// </summary>
        private void InitializeQuerySidebar()
		{
            Image img = Utils.TryGetEmbeddedResourceImageFromAssembly( Assembly.GetExecutingAssembly(), "OmniaMea.Icons.Views24.png" );

			// Note: do not do SuspendLayout here, or the side bar won't restore the pane sizes properly
			_querySidebar.DefaultPaneIcon = img;
			_correspondentCtrl = new CorrespondentCtrl();
			_correspondentCtrl.IniSection = "DefaultCorrespondentCtrl";
			_querySidebar.RegisterTab( "All", null, -1 );
			_querySidebar.RegisterActivateAction( null, StandardViewPanes.ViewsCategories, "Views and Categories" );
			_querySidebar.RegisterViewPaneShortcut( StandardViewPanes.ViewsCategories, Keys.Control | Keys.Alt | Keys.V );

            img = Utils.TryGetEmbeddedResourceImageFromAssembly( Assembly.GetExecutingAssembly(), "OmniaMea.Icons.Correspondents24.png" );
			_querySidebar.RegisterViewPane( StandardViewPanes.Correspondents, "All", "Correspondents", img, _correspondentCtrl );
			_querySidebar.RegisterViewPaneShortcut( StandardViewPanes.Correspondents, Keys.Control | Keys.Alt | Keys.C );

			// Register a drag'n'drop handler for the Views'n'Cats resource-tree-root
			Core.PluginLoader.RegisterResourceDragDropHandler( Core.ResourceTreeManager.ResourceTreeRoot.Type, new ViewsCategoriesRootDragDropHandler() );
		}

    	/// <summary>
		/// Initializes the panes of the Options Dialog.
		/// </summary>
        private static void InitializeOptionsDialog()
        {
            Core.UIManager.RegisterOptionsGroup( "Omea", "This option group contains several pages of settings that apply globally to your [product name] installation." );
            Core.UIManager.RegisterOptionsPane( "Omea", "Plugins (Old)", PluginsConfigPane.PluginsConfigPaneCreator,
                "The Plugins options activate and deactivate plugins that support various features and types of resources." );
            Core.UIManager.RegisterOptionsPane( "Omea", "Plugins", ()=>new OmeaPluginsPage(),
                "The Plugins options activate and deactivate plugins that support various features and types of resources." );
            Core.UIManager.RegisterOptionsPane( "Omea", "Paths", DatabasePathOptionsPane.CreatePane,
                "The Paths options specify the locations where the Omea database and logs are stored." );
            Core.UIManager.RegisterOptionsPane( "Omea", "General", CreateInterfaceOptionsPane,
                "The General options control certain global behaviors." );
            Core.UIManager.RegisterOptionsPane( "Omea", "User Information", MySelfPane.MySelfPaneCreator,
                "The User Information options enable you to specify your first name, last name and list of e-mail addresses." );
            Core.UIManager.RegisterOptionsPane( "Omea", "Proxy Configuration", ProxyConfigPane.ProxyConfigPaneCreator,
                "The Proxy Configuration options enable you to configure a proxy server if one is required by your Internet service provider in order for you to access online content." );
            Core.UIManager.RegisterOptionsPane( "Omea", "Mail Format",  MailFormatPane.MailFormatPaneCreator,
                "The Mail Format options allow to configure the formatting of messages and replies created in Omea." );
            Core.UIManager.RegisterOptionsPane( "Omea", "Delete Confirmations", DeletersPane.DeletersPaneCreator,
                "The Delete Confirmations options enable you to configure per resource type how confirmations appear when resources are moved to the Deleted Resources and deleted permanently." );
		}

        private static AbstractOptionsPane CreateInterfaceOptionsPane()
        {
            return new InterfaceOptions();
        }

		/// <summary>
		/// Shows the Advanced Search dialog.
		/// </summary>
        public void ShowAdvancedSearchDialog()
        {
        	_panelResourceTabsRow.SearchBar.ShowAdvancedSearchDialog();
        }

        public void FocusSearchBox()
        {
        	_panelResourceTabsRow.SearchBar.FocusSearchBox();
        }

        public bool LeftSidebarExpanded
        {
            get { return !_leftSidebarSplitter.Collapsed; }
            set { _leftSidebarSplitter.Collapsed = !value; }
        }

        public bool RightSidebarExpanded
        {
//            get { return _rightSidebar.PanesCount > 0 && !_rightSidebarSplitter.Collapsed; }
            get { return _rightSidebar.PanesCount > 0 && _rightSidebarSplitter.Visible; }
            set
            {
                if ( _rightSidebar.PanesCount > 0 )
                {
                    _rightSidebar.Visible = value;
                    _rightSidebarSplitter.Visible = value;
//                    _rightSidebarSplitter.Collapsed = !value;
                }
            }
        }

		/// <summary>
		/// Gets or sets whether the shortcut bar is visible or not on the workspace buttons row.
		/// </summary>
		public bool ShortcutBarVisible
        {
            get { return _panelWorkspaceButtonsRow.ShortcutBarVisible; }
            set
            {
                _panelWorkspaceButtonsRow.ShortcutBarVisible = value;
            	PerformLayout();
            }
        }

		/// <summary>
		/// Gets or sets whether the workspace buttons are visible or not on the workspace buttons row.
		/// </summary>
		public bool WorkspaceButtonsVisible
        {
            get { return _panelWorkspaceButtonsRow.WorkspaceButtonsVisible; }
            set
            {
				_panelWorkspaceButtonsRow.WorkspaceButtonsVisible = value;
                _panelWorkspaceButtonsRow.Visible = value;
            	PerformLayout();
            }
        }

        //
        // -- Tabs support ---------------------------------------------------
        //

        private void OnRightSidebarPaneAdded( object sender, EventArgs e )
        {
            if ( _rightSidebar.PanesCount == 1 )
            {
                RightSidebarExpanded = _iniFile.ReadBool( "MainForm", "RightSidebarExpanded", true );
            }
        }

/*
        private void OnRightSidebarExpand( object sender, EventArgs e )
        {
            _rightSidebarSplitter.Visible = _rightSidebar.Expanded;
        }

*/
        private void HandleRightSidebarSizeChanged( object sender, EventArgs e )
        {
            _fillerPanel.Invalidate();
        }

        private void _querySidebar_ExpandedChanged( object sender, EventArgs e )
        {
            _leftSidebarSplitter.Visible = _querySidebar.Expanded;
        }

		/// <summary>
		/// When a tab is switched, updates the available actions and the unread counters.
		/// </summary>
        private void _resourceTypeTabs_TabSwitch( object sender, EventArgs e )
        {
            _fillerPanel.Invalidate();
        }

        private static void _workspaceBar_WorkspaceChanged( object sender, ResourceEventArgs e )
        {
            string wrsp = ((e.Resource == null) ? "Default" : e.Resource.Id.ToString());
            LogManager.WriteToUsageLog( "[Switch] *Workspace* to [" + wrsp + "]" );
        }

        public IStatusWriter GetStatusWriter( object owner, StatusPane pane )
        {
            switch( pane )
            {
                case StatusPane.UI: return _uiStatusManager.GetStatusWriter( owner );
                case StatusPane.ResourceBrowser: return _itemStatusManager.GetStatusWriter( owner );
                default: return _netStatusManager.GetStatusWriter( owner );
            }
        }

        private void HandlePaintSplitterBackground( object sender, PaintEventArgs e )
        {
            JetSplitter splitter = (JetSplitter) sender;
            Rectangle rcFill = new Rectangle( 0, 0, splitter.ClientRectangle.Width, 150 );
            Brush backBrush = ColorScheme.GetBrush( _colorScheme, "Sidebar.Background",
                rcFill, SystemBrushes.Control );
            e.Graphics.FillRectangle( backBrush, rcFill );
            if ( splitter == _rightSidebarSplitter )
            {
                Pen borderPen = ColorScheme.GetPen( _colorScheme, "PaneCaption.Border", Pens.Black );
                e.Graphics.DrawLine( borderPen, 0, 0, splitter.ClientRectangle.Width-1, 0 );
            }
        }

        private void HandlePaintFillerPanel( object sender, PaintEventArgs e )
        {
            int fillRight = ClientRectangle.Width-1;
            using( Brush brush = new SolidBrush( ColorScheme.GetColor( _colorScheme, "Sidebar.Background", Color.Blue ) ) )
            {
                e.Graphics.FillRectangle( brush, 0, 0, fillRight, _fillerPanel.Height );
            }

            Pen borderPen = ColorScheme.GetPen( _colorScheme, "PaneCaption.Border", Pens.Black );
            Rectangle rcActiveTab = _panelResourceTabsRow.ResourceTypeTabs.ActiveTabRect;
            e.Graphics.DrawLine( borderPen, 0, 0, rcActiveTab.Left+2, 0 );
            e.Graphics.DrawLine( borderPen, rcActiveTab.Right+1, 0, fillRight, 0 );
        }

/*
        private void HandlePaintRightSidebarBackground( object sender, PaintEventArgs e )
        {
            if ( !_rightSidebar.Expanded )
            {
                Pen borderPen = ColorScheme.GetPen( _colorScheme, "PaneCaption.Border", Pens.Black );
                e.Graphics.DrawLine( borderPen, 0, 0, 3, 0 );
                e.Graphics.DrawLine( borderPen, 3, 0, 3, _rightSidebar.Height );
                Rectangle rcFill = new Rectangle( 0, 1, 3, 150 );
                Brush backBrush = ColorScheme.GetBrush( _colorScheme, "Sidebar.Background",
                    rcFill, SystemBrushes.Control );
                e.Graphics.FillRectangle( backBrush, rcFill );
                e.Graphics.FillRectangle( SystemBrushes.Control, 0, 150, 3, _rightSidebar.Height-150 );
            }
        }
*/

        public void ReportBackgroundException( Exception ex )
        {
            Trace.WriteLine( "Background exception: " + ex );

            if ( ex is AsyncProcessorException )
                ex = ex.InnerException;
            if ( ex is TargetInvocationException )
                ex = ex.InnerException;

            // ignore duplicate exceptions
            foreach( Exception oldEx in _backgroundExceptionList )
            {
                if ( oldEx.ToString() == ex.ToString() )
                {
                    return;
                }
            }
            _backgroundExceptionList.Add( ex );
            if ( _backgroundExceptionList.Count == 1 )
            {
                _backgroundExceptionPanel.Icon = LoadIconFromAssembly( "BackgroundException.ico" );
                //_backgroundExceptionPanel.ToolTipText = "Exception occurred. Double-click to submit error report";
            }
        }

        private void _statusBar_PanelClick( object sender, StatusBarPanelClickEventArgs e )
        {
            if ( e.StatusBarPanel == _backgroundExceptionPanel && e.Clicks == 2 && _backgroundExceptionList.Count > 0 )
            {
                using( BackgroundExceptionDlg dlg = new BackgroundExceptionDlg() )
                {
                    dlg.ShowBackgroundExceptionDialog( _backgroundExceptionList, _excHandler.GetExceptionDescription() );
                }
                if ( _backgroundExceptionList.Count == 0 )
                {
                    _backgroundExceptionPanel.Icon = null;
                    //_backgroundExceptionPanel.ToolTipText = "";
                }
            }
        }

        private void miExitOmniaMea_Click( object sender,EventArgs e )
        {
            Close();
        }

        private void miOpenOmniaMea_Click(object sender, EventArgs e)
        {
            RestoreFromTray();
        }

        public void RestoreFromTray()
        {
            if ( Core.State == CoreState.Running )
            {
                _restoringFromTray = true;
                try
                {
                    Visible = true;
                    Trace.WriteLine( "Restoring from tray to window state " + _oldWindowState );
                    WindowState = _oldWindowState;
                    Activate();
                }
                finally
                {
                    _restoringFromTray = false;
                }
            }
        }

        private void _notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void MainFrame_Resize(object sender, EventArgs e)
        {
            if ( WindowState == FormWindowState.Minimized )
            {
                if ( _iniFile.ReadBool( "Resources", "MinimizeToTray", false ) )
                {
                    Visible = false;
                    _minimized = true;
                    return;
                }
            }

            if ( _minimized )
            {
                Visible = true;
                _minimized = false;
            }
        }

        protected override void WndProc( ref Message m )
        {
            base.WndProc( ref m );
            if ( m.Msg == Win32Declarations.WM_EXITMENULOOP )
            {
                _uiManager.OnExitMenuLoop();
            }
            else if ( m.Msg == Win32Declarations.WM_CREATE )
            {
                // restore settings only after the form settings from startup info have been processed (OM-7647)
                if ( !_formSettingsRestored )
                {
                    _formSettingsRestored = true;
                    try
                    {
                        RestoreFormSettings();
                    }
                    catch( Exception ex )
                    {
                        Trace.WriteLine( "Exception when restoring form settings: " + ex );
                    }
                }
            }
            else if ( m.Msg == Win32Declarations.WM_SIZE )
            {
                // OnResize() isn't always called when a window is minimized/maximized
                if ( WindowState != FormWindowState.Minimized && !_minimized && !_restoringFromTray )
                {
                    _oldWindowState = WindowState;
                }
            }
        }

		bool _paintLogged = false;

		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint( e );
			if( ! _paintLogged  )
            {
				_paintLogged = true;
                Trace.WriteLine( "First paint done" );

                if ( _textIndexManager != null )
                {
                    _textIndexManager.StartIndexingThread();
                }
			}
		}

        private void _indicatorsPanel_Resize(object sender, EventArgs e)
        {
            _indicatorStatusPanel.Width = _indicatorsPanel.Width + 6;
        }
    }

	#region Class ResourceTabProvider — Intermediate API between UnreadManager and TabManager.

	/// <summary>
	/// Intermediate API between UnreadManager and TabManager.
	/// </summary>
	internal class ResourceTabProvider : IResourceTabProvider
	{
		public string GetDefaultTab()
		{
			return Core.TabManager.Tabs [0].Id;
		}

		public string GetResourceTab( IResource res )
		{
			return Core.TabManager.GetResourceTab( res );
		}

		public IResourceList GetTabFilterList( string tabId )
		{
			TabSwitcher.TabFilter tabFilter = (TabSwitcher.TabFilter) Core.TabManager.Tabs [tabId];
			return tabFilter.GetFilterList( false, false );
		}
	}

	#endregion

	#region class WheelMessageFilter — Forwards mouse wheel messages to the window under cursor instead of the focused window.

	/// <summary>
	/// Forwards mouse wheel messages to the window under cursor instead of the focused window.
	/// </summary>
	internal class WheelMessageFilter: IMessageFilter
	{
		private int _lastMessageIndex = 0;
		private int _lastMouseMovePos = -1;

		public int LastMessageIndex
		{
			get { return _lastMessageIndex; }
		}

		public void IncLastMessage()
		{
			_lastMessageIndex++;
		}

		public bool PreFilterMessage( ref Message m )
		{
			if ( ( m.Msg >= Win32Declarations.WM_KEYFIRST && m.Msg <= Win32Declarations.WM_KEYLAST ) ||
				( m.Msg >= Win32Declarations.WM_MOUSEFIRST && m.Msg <= Win32Declarations.WM_MOUSELAST ) )
			{
				if ( m.Msg == Win32Declarations.WM_MOUSEMOVE )
				{
					int mousePos = (int) m.LParam;
					if ( mousePos != _lastMouseMovePos )
					{
						_lastMouseMovePos = mousePos;
						_lastMessageIndex++;
					}
				}
				else
				{
					_lastMessageIndex++;
				}
			}

			if ( m.Msg == Win32Declarations.WM_MOUSEWHEEL )
			{
				POINT ptapi = new POINT( m.LParam.ToInt32() );
				IntPtr pWnd = WindowsAPI.WindowFromPoint( ptapi );
				if ( pWnd.ToInt32() != 0 )
				{
					User32Dll.Helpers.SendMessageW( pWnd, WindowsMessages.WM_MOUSEWHEEL, m.WParam, m.LParam );
					return true;
				}
			}

			if ( m.Msg == Win32Declarations.WM_APPCOMMAND )
			{
				int appCommand = ( m.LParam.ToInt32() >> 16 ) & ~0xF000;

				Trace.WriteLine( "AppCommand received: " + appCommand );
                if ( Core.State == CoreState.Running )
                {
				    switch( appCommand )
				    {
				    case Win32Declarations.APPCOMMAND_BROWSER_BACKWARD:
					    Core.ResourceBrowser.GoBack(); return true;

				    case Win32Declarations.APPCOMMAND_BROWSER_FORWARD:
					    Core.ResourceBrowser.GoForward(); return true;
				    }
                }
			}
			return false;
		}

	}

	#endregion

	delegate void UpdateStatusTextDelegate( bool doEvents );
}
