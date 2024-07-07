// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GUIControls.Controls;
using JetBrains.DataStructures;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Base;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
    internal class ResourceBrowser : UserControl, IResourceBrowser
    {
        private const int   _cSpaceMargin = 4;
        private const int   _cDefaultLinksPaneWidth = 150;

        private IContainer components = null;

        private Panel _bodyPane;
        private LinksPane _linksPane;
        private CustomStylePanel _lowerPaneBackground;
        private ResourceListView2 _listView;
        private Panel             _lowerPane;
        private JetSplitter _listAndContentSplitter;
        private IDisplayPane _displayPane = null;

        private string _lastDisplayedType = null;
        private IResourceDisplayer _lastPlugin = null;
        private IResourceList _resourceList;
        private IResourceList _origResourceList;
        private IResourceList _origFilterResourceList;
        private IResourceList _singleResourceList;
        private IResourceList _filterResourceList;
        private IResourceList _ownerResourceList;
        private IResourceList _customProperties;
        private IResource _lastDisplayedResource;
        private int _lastDisplayedResourceCount = 0;
        private IResource _delegateOrigResource;
        private IResource _ownerResource;
        private IHighlightDataProvider _highlightProvider;
        private bool _suppressContexts;

        private string _caption;
        private string _captionPrefix;
        private string _captionTemplate;
        private Panel _captionPanel;
        private Label _captionLabel;
        private readonly Font _captionFont;

        int                         _tmrMarkAsRead_TimeOut = 0;
        private bool                _viewAnnotations = false;
        private AnnotationForm      _annotationForm;

        BrowserPanesVisibilityMode _mode = BrowserPanesVisibilityMode.Both;
        private ColumnDescriptor[] _defaultColumns;
        private ResourceListState _listState;

        private BrowseStack _browseStack;
        private DisplayColumnManager _columnManager;
        private Timer _tmrMarkAsRead;
        private JetLinkLabel _statusLineLabel;
        private EventHandler _statusLineClickHandler;
        private readonly ArrayList _webModeHiddenControls = new ArrayList();
        private string    _webModeSavedCaption;
        private bool      _resourceListVisible;
        private bool      _statusLineVisible;
        private ContextMenu _headerContextMenu;
        private MenuItem miConfigureColumns;
        private MenuItem miShowItemsInGroups;
        private SeeAlsoBar _seeAlsoBar;
        private string    _urlBarText;
        private LinksBar _linksBar;
        private Splitter _linksPaneSplitter;

        private bool        _webPageMode;
        private int         _SavedWidth;
        private DockStyle   _savedDock;

        private Panel       _toolBarPanel;
        private ToolStrip   _toolBar;
        private ToolStrip   _urlBarToolbar;             // saved value for switching to Web mode
        private Panel       _webAddressPanel;
        private JetTextBox  _edtURL;
        private Button      _btnGoURL;
        private readonly ToolbarActionManager _toolBarActionManager;
        private readonly ToolbarActionManager _urlBarActionManager;
        private readonly ToolBarRenderer _toolbarRenderer;

        private readonly HashMap _linksPaneFilters = new HashMap();  // resource type -> ILinksPaneFilter

        private ColorScheme _colorScheme;
        private bool _linksPaneWidthLoaded;

        private int _updateCount;
        private bool _urlBarShown = false;

        private Hashtable _displayForwarders = new Hashtable();  // resource type -> ResourceDisplayForwarderDelegate

        private bool _bodyPaneFocused = false;

        private IStatusWriter _itemCountWriter;
        private ResourceListDataProvider _dataProvider;

        public event EventHandler ContentChanged;

        private readonly PerTabBrowserSettings _perTabBrowserSettings = new PerTabBrowserSettings();
        private readonly DefaultAutoPreviewColumn _autoPreviewColumn;
        private readonly ContextAutoPreviewColumn _contextPreviewColumn;

        private HiddenColumnState _hiddenColumnState;

        private NewspaperViewer _newspaperViewer;
        private Panel _listViewPanel;
        private int _listViewWidth;    // width of list view in vertical layout
        private int _listViewHeight;   // height of list view in horizontal layout
        private bool _verticalLayout;
        private bool _groupItems;
        private int _displayResourceListReenter;
        private bool _displayResourceListReenterAllowed;
        private bool _inMarkResourceRead = false;

        private IResource _transientContainer;

		public delegate IResource GetNextResourceDelegate( IResource start, ResourceListView2.LocateMatchCallback callback,
                                                           bool skipFirst, bool lookAlsoBackward );
		public delegate bool GetNextViewDelegate( AbstractViewPane pane, IResource currentView );

        public ResourceBrowser()
        {
            _captionFont = new Font( "Tahoma", 12.0F, FontStyle.Bold, GraphicsUnit.Point, ((System.Byte)(204)));

            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            _toolBarActionManager = new ToolbarActionManager( _toolBar );
            _toolBarActionManager.ContextProvider = this;

            _listView.ActiveResourceChanged += HandleActiveResourceChanged;
            _listView.SelectionChanged += HandleResourceSelectionChanged;
            _listView.KeyNavigationCompleted += HandleKeyNavigationCompleted;
            _listView.ContextProvider = this;
            _listView.AllowColumnReorder = true;
            _listView.JetListView.AutoToolTips = false;
            _listView.AllowSameViewDrag = false;

            _listAndContentSplitter.ControlToCollapse = _listView;

            _browseStack = new BrowseStack( this );

            SetCaptionLabelInactive();

            _urlBarActionManager = new ToolbarActionManager( _urlBarToolbar );
            _urlBarActionManager.ContextProvider = this;
            _toolbarRenderer = new ToolBarRenderer( _colorScheme, Color.White, SystemColors.ControlDark );

            _autoPreviewColumn = new DefaultAutoPreviewColumn();
            _contextPreviewColumn = new ContextAutoPreviewColumn();

            if ( Core.State == CoreState.Initializing )
            {
                Core.StateChanged += OnCoreStateChanged;
            }
            else
            {
                InitializeResourceBrowser();
            }
        }

        private void OnCoreStateChanged( object sender, EventArgs e )
        {
            if ( Core.State == CoreState.StartingPlugins )
            {
                Core.StateChanged -= OnCoreStateChanged;
                InitializeResourceBrowser();
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                _browseStack.Dispose();
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Panes Visibility
        public BrowserPanesVisibilityMode BrowserPanesMode
        {
            get {  return _mode;  }
            set
            {
                if( _mode != value )
                {
                    if( _mode == BrowserPanesVisibilityMode.ListOnly )
                        ShowContentPane();
                    if( _mode == BrowserPanesVisibilityMode.ContentOnly )
                        ResourceListExpanded = true;

                    if( value == BrowserPanesVisibilityMode.ListOnly )
                        HideContentPane();
                    if( value == BrowserPanesVisibilityMode.ContentOnly )
                        ResourceListExpanded = false;

                    _mode = value;
                }
            }
        }

        private void HideContentPane()
        {
            _SavedWidth = _listView.Width;
            _savedDock = _listView.Dock;

            _lowerPaneBackground.Visible = _listAndContentSplitter.Visible = false;
            _listView.Dock = DockStyle.Fill;
        }

        private void ShowContentPane()
        {
            _listView.Dock = _savedDock;
            _listView.Width = _SavedWidth;

            _lowerPaneBackground.Visible = _listAndContentSplitter.Visible = true;
        }
        #endregion Panes Visibility

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ResourceBrowser));
            this._listAndContentSplitter = new JetBrains.Omea.GUIControls.JetSplitter();
            this._lowerPaneBackground = new CustomStylePanel();
            this._lowerPane = new System.Windows.Forms.Panel();
            this._bodyPane = new System.Windows.Forms.Panel();
            this._linksPaneSplitter = new Splitter();
            this._linksPane = new LinksPane();
            this._captionPanel = new System.Windows.Forms.Panel();
            this._captionLabel = new System.Windows.Forms.Label();
            this._seeAlsoBar = new SeeAlsoBar();
            this._tmrMarkAsRead = new System.Windows.Forms.Timer(this.components);
            this._statusLineLabel = new JetLinkLabel();
            this._listView = new JetBrains.Omea.GUIControls.ResourceListView2();
            this._headerContextMenu = new System.Windows.Forms.ContextMenu();
            this.miConfigureColumns = new System.Windows.Forms.MenuItem();
            this.miShowItemsInGroups = new System.Windows.Forms.MenuItem();
            this._webAddressPanel = new System.Windows.Forms.Panel();
            this._urlBarToolbar = new ToolStrip();
            this._edtURL = new JetTextBox();
            this._btnGoURL = new Button();
            this._linksBar = new LinksBar();
            this._toolBar = new ToolStrip();
            this._toolBarPanel = new System.Windows.Forms.Panel();
            this._listViewPanel = new Panel();
            this._lowerPane.SuspendLayout();
            this._captionPanel.SuspendLayout();
            this._webAddressPanel.SuspendLayout();
            this._toolBarPanel.SuspendLayout();
            this._listViewPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // listViewPanel
            //
            this._listViewPanel.Name = "_listViewPanel";
            this._listViewPanel.Dock = DockStyle.Fill;
            this._listViewPanel.Controls.Add(this._lowerPaneBackground);
            this._listViewPanel.Controls.Add(this._listAndContentSplitter);
            this._listViewPanel.Controls.Add(this._listView);
            //
            // _listAndContentSplitter
            //
            this._listAndContentSplitter.ControlToCollapse = null;
            this._listAndContentSplitter.Dock = System.Windows.Forms.DockStyle.Top;
            this._listAndContentSplitter.FillGradient = false;
            this._listAndContentSplitter.FillCenterRect = false;
            this._listAndContentSplitter.Location = new System.Drawing.Point(0, 227);
            this._listAndContentSplitter.Name = "_listAndContentSplitter";
            this._listAndContentSplitter.Size = new System.Drawing.Size(600, 5);
            this._listAndContentSplitter.TabIndex = 1;
            this._listAndContentSplitter.TabStop = false;
            //
            // _lowerPaneBackground
            //
            this._lowerPaneBackground.BackColor = SystemColors.AppWorkspace;
            this._lowerPaneBackground.BorderStyle = BorderStyle.FixedSingle;
            this._lowerPaneBackground.Controls.Add(this._lowerPane);
            this._lowerPaneBackground.Controls.Add(this._linksBar);
            this._lowerPaneBackground.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lowerPaneBackground.Location = new System.Drawing.Point(0, 252);
            this._lowerPaneBackground.Name = "_lowerPaneBackground";
            this._lowerPaneBackground.ResizeRedraw = false;
            this._lowerPaneBackground.Size = new System.Drawing.Size(600, 180);
            this._lowerPaneBackground.TabIndex = 3;
            //
            // _lowerPane
            //
            this._lowerPane.BackColor = SystemColors.Control;
            this._lowerPane.Controls.Add(this._bodyPane);
            this._lowerPane.Controls.Add(this._linksPaneSplitter);
            this._lowerPane.Controls.Add(this._linksPane);
            this._lowerPane.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lowerPane.Location = new System.Drawing.Point(0, 252);
            this._lowerPane.Name = "_lowerPane";
            this._lowerPane.Size = new System.Drawing.Size(600, 180);
            this._lowerPane.TabIndex = 3;
            //
            // _bodyPane
            //
            this._bodyPane.BackColor = SystemColors.Window;
            this._bodyPane.Dock = System.Windows.Forms.DockStyle.Fill;
            this._bodyPane.Location = new System.Drawing.Point(0, 0);
            this._bodyPane.Name = "_bodyPane";
            this._bodyPane.Size = new System.Drawing.Size(445, 180);
            this._bodyPane.TabIndex = 0;
            this._bodyPane.Enter += new System.EventHandler(this._bodyPane_Enter);
            this._bodyPane.Paint += new System.Windows.Forms.PaintEventHandler(this._bodyPane_OnPaint);
            this._bodyPane.Leave += new System.EventHandler(this._bodyPane_Leave);
            //
            // _linksPaneSplitter
            //
            this._linksPaneSplitter.Dock = System.Windows.Forms.DockStyle.Right;
            this._linksPaneSplitter.Location = new System.Drawing.Point(445, 0);
            this._linksPaneSplitter.Name = "_linksPaneSplitter";
            this._linksPaneSplitter.Size = new System.Drawing.Size(3, 180);
            this._linksPaneSplitter.TabIndex = 1;
            this._linksPaneSplitter.TabStop = false;
            this._linksPaneSplitter.Visible = false;
            //
            // _linksPane
            //
            this._linksPane.ColorScheme = null;
            this._linksPane.Dock = System.Windows.Forms.DockStyle.Right;
            this._linksPane.Location = new System.Drawing.Point(450, 0);
            this._linksPane.Name = "_linksPane";
            this._linksPane.Size = new System.Drawing.Size(150, 180);
            this._linksPane.TabIndex = 2;
            this._linksPane.Visible = false;
            //
            // _captionPanel
            //
            this._captionPanel.BackColor = System.Drawing.SystemColors.ControlDark;
            this._captionPanel.Controls.Add(this._seeAlsoBar);
            this._captionPanel.Controls.Add(this._captionLabel);
            this._captionPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._captionPanel.Location = new System.Drawing.Point(0, 0);
            this._captionPanel.Name = "_captionPanel";
            this._captionPanel.Size = new System.Drawing.Size(600, 28);
            this._captionPanel.TabIndex = 3;
            this._captionPanel.Click += new System.EventHandler(this._captionLabel_Click);
            this._captionPanel.Paint += new PaintEventHandler( HandleCaptionPanelPaint );
			_captionPanel.Layout += new LayoutEventHandler(OnLayoutCaptionPanel);
            //
            // _captionLabel
            //
            this._captionLabel.AutoSize = false;
            this._captionLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._captionLabel.Font = _captionFont;
            this._captionLabel.ForeColor = System.Drawing.SystemColors.Window;
            this._captionLabel.Location = new System.Drawing.Point(8, 3);
            this._captionLabel.Name = "_captionLabel";
            this._captionLabel.Size = new System.Drawing.Size(580, 20);
            this._captionLabel.TabIndex = 0;
            this._captionLabel.UseMnemonic = false;
            this._captionLabel.Click += new System.EventHandler(this._captionLabel_Click);
            this._captionLabel.SizeChanged += new System.EventHandler(this._captionLabel_SizeChanged);
            this._captionLabel.TextChanged += new System.EventHandler(this._captionLabel_TextChanged);
            //
            // _seeAlsoBar
            //
            this._seeAlsoBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._seeAlsoBar.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._seeAlsoBar.Name = "_seeAlsoBar";
            this._seeAlsoBar.TabIndex = 0;
            this._seeAlsoBar.Visible = false;
            this._seeAlsoBar.Click += new System.EventHandler(this._captionLabel_Click);
            this._seeAlsoBar.SeeAlsoLinkClicked += new SeeAlsoEventHandler(this._seeAlsoBar_SeeAlsoLinkClicked);
            //
            // _tmrMarkAsRead
            //
            this._tmrMarkAsRead.Interval = 2000;
            this._tmrMarkAsRead.Tick += new System.EventHandler(this._tmrMarkAsRead_Tick);
            //
            // _statusLineLabel
            //
            this._statusLineLabel.AutoSize = false;
            this._statusLineLabel.BackColor = SystemColors.Info;
            this._statusLineLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this._statusLineLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._statusLineLabel.Location = new System.Drawing.Point(0, 46);
            this._statusLineLabel.Name = "_statusLineLabel";
            this._statusLineLabel.Size = new System.Drawing.Size(600, 21);
            this._statusLineLabel.TabIndex = 4;
            this._statusLineLabel.TabStop = true;
            this._statusLineLabel.Text = "linkLabel1";
            this._statusLineLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._statusLineLabel.Visible = false;
            this._statusLineLabel.Click += new EventHandler(this._statusLineLabel_LinkClicked);
            this._statusLineLabel.Paint += new PaintEventHandler( HandleStatusLineLabelPaint );
            //
            // _listView
            //
            this._listView.AllowDrop = true;
            this._listView.BorderStyle = BorderStyle.FixedSingle;
            this._listView.HeaderStyle = ColumnHeaderStyle.Clickable;
            this._listView.Dock = System.Windows.Forms.DockStyle.Top;
            this._listView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._listView.FullRowSelect = true;
            this._listView.HeaderContextMenu = this._headerContextMenu;
            this._listView.HideSelection = false;
            this._listView.InPlaceEdit = true;
            this._listView.Location = new System.Drawing.Point(0, 96);
            this._listView.Name = "_listView";
            this._listView.Size = new System.Drawing.Size(600, 131);
            this._listView.TabIndex = 2;
            this._listView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnListViewKeyDown);
            this._listView.ColumnSizeChanged += new System.EventHandler(this._listView_ColumnSizeChanged);
            this._listView.ColumnOrderChanged += new System.EventHandler(this._listView_ColumnOrderChanged);
//            this._listView.VisibleChanged += new System.EventHandler(this.OnListViewVisibleChanged);
            //
            // _headerContextMenu
            //
            this._headerContextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                               this.miConfigureColumns,
                                                                                               this.miShowItemsInGroups});
            this._headerContextMenu.Popup += new EventHandler( HandleHeaderContextMenuPopup );
            //
            // miConfigureColumns
            //
            this.miConfigureColumns.Index = 0;
            this.miConfigureColumns.Text = "&Configure Columns...";
            this.miConfigureColumns.Click += new System.EventHandler(this.miConfigureColumns_Click);
            //
            // miConfigureColumns
            //
            this.miShowItemsInGroups.Index = 1;
            this.miShowItemsInGroups.Text = "&Show Items in Groups";
            this.miShowItemsInGroups.Click += new System.EventHandler(this.miShowItemsInGroups_Click);
            //
            // _webAddressPanel
            //
            this._webAddressPanel.Controls.Add(this._btnGoURL);
            this._webAddressPanel.Controls.Add(this._edtURL);
            this._webAddressPanel.Controls.Add(this._urlBarToolbar);
            this._webAddressPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._webAddressPanel.Location = new System.Drawing.Point(0, 67);
            this._webAddressPanel.Name = "_webAddressPanel";
            this._webAddressPanel.Size = new System.Drawing.Size(600, 25);
            this._webAddressPanel.TabIndex = 5;
            this._webAddressPanel.Visible = false;
            this._webAddressPanel.Paint += new System.Windows.Forms.PaintEventHandler(this._webAddressPanel_Paint);
            //
            // _urlBarToolbar
            //
            this._urlBarToolbar.AutoSize = false;
            this._urlBarToolbar.Dock = System.Windows.Forms.DockStyle.Left;
            this._urlBarToolbar.Location = new System.Drawing.Point(0, 0);
            this._urlBarToolbar.Name = "_urlBarToolbar";
            this._urlBarToolbar.Size = new System.Drawing.Size(40, 29);
            this._urlBarToolbar.TabIndex = 2;
            this._urlBarToolbar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this._urlBarToolbar.Renderer = _toolbarRenderer;
            //
            // _edtURL
            //
            this._edtURL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtURL.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._edtURL.Location = new System.Drawing.Point(44, 2);
            this._edtURL.Name = "_edtURL";
            this._edtURL.Size = new System.Drawing.Size(500, 21);
            this._edtURL.TabIndex = 1;
            this._edtURL.Text = "";
            this._edtURL.KeyDown += new System.Windows.Forms.KeyEventHandler(this._edtURL_KeyDown);
            this._edtURL.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this._edtURL_KeyPress);
            //
            // _btnGoURL
            //
            _btnGoURL.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right) ;
            _btnGoURL.Location = new System.Drawing.Point(544, 2);
            _btnGoURL.Size = new System.Drawing.Size( 27, 21 );
            _btnGoURL.TabIndex = 2;
            _btnGoURL.Text = "Go";
            _btnGoURL.Click += new EventHandler(URL_ButtonPress);
            _btnGoURL.FlatStyle = FlatStyle.System;
            //
            // _linksBar
            //
            this._linksBar.ColorScheme = null;
            this._linksBar.Dock = System.Windows.Forms.DockStyle.Top;
            this._linksBar.LinksPaneExpanded = false;
            this._linksBar.Location = new System.Drawing.Point(0, 232);
            this._linksBar.Name = "_linksBar";
            this._linksBar.Size = new System.Drawing.Size(600, 20);
            this._linksBar.TabIndex = 7;
            this._linksBar.LinksPaneExpandChanged += new System.EventHandler(this._linksBar_LinksPaneExpandChanged);
            //
            // _toolBar
            //
            this._toolBar.AutoSize = false;
            this._toolBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this._toolBar.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._toolBar.Location = new System.Drawing.Point(244, 0);
            this._toolBar.Name = "_toolBar";
            this._toolBar.Size = new System.Drawing.Size(356, 29);
            this._toolBar.TabIndex = 1;
            this._toolBar.Renderer = _toolbarRenderer;
			//
            // _toolBarPanel
            //
            this._toolBarPanel.Controls.Add(this._toolBar);
            this._toolBarPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._toolBarPanel.Location = new System.Drawing.Point(0, 18);
            this._toolBarPanel.Name = "_toolBarPanel";
            this._toolBarPanel.Size = new System.Drawing.Size(600, 24);
            this._toolBarPanel.TabIndex = 8;
            //
            // ResourceBrowser
            //
            this.Controls.Add(this._listViewPanel);
            this.Controls.Add(this._webAddressPanel);
            this.Controls.Add(this._statusLineLabel);
            this.Controls.Add(this._toolBarPanel);
            this.Controls.Add(this._captionPanel);
            this.Name = "ResourceBrowser";
            this.Size = new System.Drawing.Size(600, 432);
            this.Enter += new System.EventHandler(this.ResourceBrowser_Enter);
            this.Leave += new System.EventHandler(this.ResourceBrowser_Leave);
            this._lowerPane.ResumeLayout(false);
            this._captionPanel.ResumeLayout(false);
            this._webAddressPanel.ResumeLayout(false);
            this._toolBarPanel.ResumeLayout(false);
            this._listViewPanel.ResumeLayout( false );
            this.ResumeLayout(false);

        }

        #endregion

        private void InitializeResourceBrowser()
        {
            _columnManager = Core.DisplayColumnManager as DisplayColumnManager;
            RestoreLayoutSettings();
            UpdateSettings();
            Core.UIManager.AddOptionsChangesListener( "Omea", "General", OnInterfaceOptionsChanged );

            _customProperties = Core.ResourceStore.FindResourcesLive( "PropType", "Custom", 1 );
            _customProperties.ResourceDeleting += OnCustomPropertyDeleting;

            _itemCountWriter = Core.UIManager.GetStatusWriter( this, StatusPane.ResourceBrowser );
        }

        internal void AttachToWebBrowser()
        {
            Core.WebBrowser.ContextProvider = Core.ResourceBrowser;
            Core.WebBrowser.BeforeNavigate += HandleBeforeNavigate;
            Core.WebBrowser.BeforeShowHtml += HandleBeforeShowHtml;
            Core.WebBrowser.TitleChanged += OnWebBrowserTitleChanged;
        }

        private void HandleBeforeNavigate( object sender, BeforeNavigateEventArgs args )
        {
            const string prefix = "omea://";
            if( args.Uri.StartsWith( prefix ) && ( args.Uri.Length > prefix.Length ))
            {
                args.Cancel = true;

                string str = args.Uri.Substring( prefix.Length );
                if( str.EndsWith( "/" ))
                    str = str.Substring( 0, str.Length - 1 );
                try
                {
                    int id = Int32.Parse( str );
                    IResource res = Core.ResourceStore.TryLoadResource( id );
                    if( res != null )
                    {
                        Core.UIManager.DisplayResourceInContext( res );
                    }
                }
                catch( FormatException )
                {}
            }
            else
            if ( args.Cause == BrowserNavigationCause.ReturnToOriginal )
            {
                WebPageMode = false;
            }
            else
            {
                if ( ( args.Inplace ) && ( args.Cause == BrowserNavigationCause.FollowLink ) &&
                    !Core.SettingStore.ReadBool( "Resources", "LinksInPreviewPane", false ) )
                {
                    WebPageMode = true;
                }
                _edtURL.Text = args.Uri;
            }
        }

        private void HandleBeforeShowHtml( object sender, BeforeShowHtmlEventArgs args )
        {
            if ( WebPageMode )
            {
                WebPageMode = false;
            }
        }

#if DEBUG
        internal ResourceListView2 ListView
        {
            get { return _listView; }
        }
#endif

        public ToolbarActionManager ToolBarActionManager
        {
            get { return _toolBarActionManager; }
        }

        public BrowseStack BrowseStack
        {
            get { return _browseStack; }
            set
            {
                if ( _browseStack != value )
                {
                    if ( value == null )
                        throw new ArgumentNullException( "value" );

                    _browseStack = value;
                }
            }
        }

        public string CaptionPrefix
        {
            get { return _captionPrefix; }
        }

        public ColumnDescriptor[] DefaultColumns
        {
            get { return _defaultColumns; }
            set { _defaultColumns = value; }
        }

        /// <summary>
        /// The resource list which is intersected with any resource list displayed in the browser.
        /// </summary>
        public IResourceList FilterResourceList
        {
            get { return _filterResourceList; }
        }

        internal void SetFilterResourceList( IResourceList resourceList )
        {
            _filterResourceList = resourceList;
        }

        /// <summary>
        /// The filter resource list which was used when the last resource list was displayed.
        /// </summary>
        public IResourceList LastFilterResourceList
        {
            get { return _origFilterResourceList; }
        }

        public IResource OwnerResource
        {
            get { return _ownerResource; }
        }

        public IResource DisplayedResource
        {
            get
            {
                if ( _singleResourceList == null || _singleResourceList.Count == 0 )
                {
                    return null;
                }
                return _singleResourceList [0];
            }
        }

        /**
         * Sets the prefix string displayed before the resource list caption.
         * @param update If true, redraws the caption immediately.
         */

        public void SetCaptionPrefix( string captionPrefix, bool update )
        {
            if ( _captionPrefix != captionPrefix )
            {
                _captionPrefix = captionPrefix;
                if ( update )
                    UpdateCaption();
            }
        }

        /**
         * Saves the layout settings (links pane width and listview height) to the INI file.
         */

        public void SaveLayoutSettings()
        {
            if ( _listView.MultiLineView )
            {
                _listViewWidth = _listView.Width;
            }
            else
            {
                _listViewHeight = _listView.Height;
            }
            Core.SettingStore.WriteInt( "ResourceBrowser", "LinksPaneWidth", _linksPane.Width );
            Core.SettingStore.WriteBool( "ResourceBrowser", "LinksPaneExpanded", _linksBar.LinksPaneExpanded );
            Core.SettingStore.WriteInt( "ResourceBrowser", "ListViewWidth", _listViewWidth );
            Core.SettingStore.WriteInt( "ResourceBrowser", "ListViewHeight", _listViewHeight );
        }

        /**
         * Restores the layout settings (links pane width and listview height) from the INI file.
         */

        public void RestoreLayoutSettings()
        {
            _linksBar.LinksPaneExpanded = Core.SettingStore.ReadBool( "ResourceBrowser", "LinksPaneExpanded", false );

            _listViewWidth = (int) (Core.SettingStore.ReadInt( "ResourceBrowser", "ListViewWidth", 300 ) / Core.ScaleFactor.Width);
            _listViewHeight = Core.SettingStore.ReadInt( "ResourceBrowser", "ListViewHeight", 150 );
            if ( _listViewHeight < 30 )
            {
                _listViewHeight = 30;
            }
            else if ( _listViewHeight > Height - 120 )
            {
                _listViewHeight = Height - 120;
            }

            if ( _listViewWidth == 0 )
            {
                _listViewWidth = 300;
            }
            else if ( _listViewWidth < 30 )
            {
                _listViewWidth = 30;
            }
            else if ( _listViewWidth > Width - 120 )
            {
                _listViewWidth = Width - 120;
            }

            if ( _listView.MultiLineView )
            {
                _listView.Width = _listViewWidth;
            }
            else
            {
                _listView.Height = (int) (_listViewHeight / Core.ScaleFactor.Height);
            }
        }

        /**
         * Ensures that the resource contents pane does not fall out of OmniaMea window bounds.
         */

        protected override void OnLayout( LayoutEventArgs levent )
        {
            base.OnLayout( levent );
            Form frm = FindForm();
            if ( frm != null && frm.WindowState != FormWindowState.Minimized )
            {
                int contentsHeight = Height - _listView.Height;
                if ( Height > 120 && contentsHeight < 120 && _listView.Height > 50 )
                {
                    _listView.Height = Height - 120;
                }
            }
        }

        /**
         * Checks if the specified display pane action is currently enabled.
         */

        public bool CanExecuteCommand( string action )
        {
            if ( ResourceListFocused && _listView.CanExecuteCommand( action ) )
            {
                return true;
            }

            if ( _displayPane != null && _displayPane.CanExecuteCommand( action ) )
            {
                return true;
            }

            return false;
        }

        /**
         * Executes the specified display pane action.
         */

        public void ExecuteCommand( string action )
        {
            if ( ResourceListFocused && _listView.CanExecuteCommand( action ) )
            {
                _listView.ExecuteCommand( action );
            }
            else if ( _displayPane != null && _displayPane.CanExecuteCommand( action ) )
            {
            	_displayPane.ExecuteCommand( action );
            }
        }

        public void GoBack()
        {
            if ( WebPageMode && Core.WebBrowser.CanExecuteCommand( DisplayPaneCommands.Back ) )
            {
                Core.WebBrowser.ExecuteCommand( DisplayPaneCommands.Back );
            }
            else if ( _displayPane != null && _displayPane.CanExecuteCommand( DisplayPaneCommands.Back ) )
            {
                _displayPane.ExecuteCommand( DisplayPaneCommands.Back );
            }
            else
            {
                _browseStack.GoBack();
            }
        }

        public bool CanBack()
        {
            if ( WebPageMode && Core.WebBrowser.CanExecuteCommand( DisplayPaneCommands.Back ) )
            {
                return true;
            }
            if ( _displayPane != null && _displayPane.CanExecuteCommand( DisplayPaneCommands.Back ) )
            {
                return true;
            }
            return _browseStack.CanBack();
        }

        public void GoForward()
        {
            if ( WebPageMode && Core.WebBrowser.CanExecuteCommand( DisplayPaneCommands.Forward ) )
            {
                Core.WebBrowser.ExecuteCommand( DisplayPaneCommands.Forward );
            }
            else if ( _displayPane != null && _displayPane.CanExecuteCommand( DisplayPaneCommands.Forward ) )
            {
                _displayPane.ExecuteCommand( DisplayPaneCommands.Forward );
            }
            else
            {
                _browseStack.GoForward();
            }
        }

        public bool CanForward()
        {
            if ( WebPageMode && Core.WebBrowser.CanExecuteCommand( DisplayPaneCommands.Forward ) )
            {
                return true;
            }

            if ( _displayPane != null && _displayPane.CanExecuteCommand( DisplayPaneCommands.Forward ) )
            {
                return true;
            }
            return _browseStack.CanForward();
        }

        private void OnContentChanged()
        {
            if ( ContentChanged != null )
            {
                ContentChanged( this, EventArgs.Empty );
            }
        }

        public void BeginUpdate()
        {
            _updateCount++;
            if ( _updateCount == 1 )
            {
                _urlBarShown = false;
            }
        }

        public void EndUpdate()
        {
            #region Preconditions
            if ( _updateCount <= 0 )
            {
                throw new InvalidOperationException( "EndUpdate() called without BeginUpdate()" );
            }
            #endregion Preconditions

            _updateCount--;
            if ( _updateCount == 0 )
            {
                if ( _urlBarShown )
                {
                    _toolBarPanel.Visible = false;
                }
                else
                {
                    CancelWebMode();
                }
            }
        }

        public void RegisterResourceDisplayForwarder( string resType, ResourceDisplayForwarderCallback forwarder )
        {
            _displayForwarders [resType] = forwarder;
        }

        public void DisplayResource( IResource res )
        {
            DisplayResource( res, true );
        }

        /**
         * Displays the specified resource in the resource browser.
         */

        public void DisplayResource( IResource res, bool backOnDelete )
        {
            #region Preconditions
            if ( res == null )
                throw new ArgumentNullException( "res" );
            #endregion Preconditions

            // If we are in the "List-Only" mode, switch back.
            if( _mode == BrowserPanesVisibilityMode.ListOnly )
            {
                ShowContentPane();
            }

            // need to check for count - the resource in singleResourceList may have been
            // deleted, and the list is live
            if ( backOnDelete && _singleResourceList != null && _singleResourceList.Count > 0 &&
                _singleResourceList.ResourceIds [0] == res.Id )
            {
                _lastDisplayedResource = null;
                DisplayResourceData( res );
            }
            else
            {
                _browseStack.Push( new ResourceBrowseState( res, backOnDelete ) );
                DoShowResource( res, backOnDelete );
            }
        }

        internal void DoShowResource( IResource res, bool backOnDelete )
        {
            Guard.NullArgument( res, "res" );
            UnhookResourceList( null );
            HideStatusLine();
            CancelWebMode();
            _seeAlsoBar.Visible = false;
            _listView.Visible = false;
            _listAndContentSplitter.Visible = false;
            _lowerPaneBackground.Visible = true;
            _origResourceList = null;
            _highlightProvider = null;
            _captionLabel.Text = res.DisplayName;
            if ( _itemCountWriter != null )
            {
                _itemCountWriter.ClearStatus();
            }
            if ( _updateCount == 0 )
            {
                _toolBarPanel.Visible = true;
            }
            _ownerResource = null;
            OnContentChanged();
            if ( backOnDelete )
            {
                HookSingleResourceList( res );
            }
            DisplayResourceData( res );
        }

        /**
         * Displays the specified resource list in the resource browser.
         */

        public void DisplayResourceList( IResource ownerResource, IResourceList resources,
                                         string caption, ColumnDescriptor[] columns )
        {
            DisplayResourceList( ownerResource, resources, caption, columns, null, null );
        }

        /**
         * Displays the specified resource list in the resource browser and
         * selects the specified resource.
         */

        public void DisplayResourceList( IResource ownerResource, IResourceList resources, string caption,
                                         ColumnDescriptor[] columns, IResource selectedResource )
        {
            DisplayResourceList( ownerResource, resources, caption, columns, selectedResource, null );
        }

        /**
         * Displays the specified resource list without applying the tab filter.
         */

        public void DisplayUnfilteredResourceList( IResource ownerResource, IResourceList resources,
                                                   string caption, ColumnDescriptor[] columns )
        {
            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            options.Caption = caption;
            options.Columns = columns;
            options.TabFilter = false;
            DisplayResourceList( ownerResource, resources, options );
        }

        /**
         * Displays the specified resource list with the specified highlight data provider.
         */

        public void DisplayResourceList( IResource ownerResource, IResourceList resources, string caption,
            ColumnDescriptor[] columns, IResource selectedResource, IHighlightDataProvider highlightProvider )
        {
            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            options.Caption = caption;
            options.Columns = columns;
            options.SelectedResource = selectedResource;
            options.HighlightDataProvider = highlightProvider;
            DisplayResourceList( ownerResource, resources, options );
        }

        public void DisplayResourceList( IResource ownerResource, IResourceList resources,
                                         ResourceListDisplayOptions options )
        {
            #region Preconditions
            if ( resources == null )
                throw new ArgumentNullException( "resources" );
            #endregion Preconditions

            bool isSameList;
            if ( options.ThreadingHandler != null )
            {
                isSameList = (_dataProvider is ConversationDataProvider) &&
                             ((_dataProvider as ConversationDataProvider).ResourceList == resources );
            }
            else
            {
                isSameList = ( !(_dataProvider is ConversationDataProvider) &&
                    resources == _origResourceList && _filterResourceList == _origFilterResourceList &&
                    !_webPageMode );
            }
            bool wasNewspaper = (_newspaperViewer != null && _newspaperViewer.Visible );

            if ( isSameList && !wasNewspaper && !options.ShowNewspaper )
            {
                DisplaySelectedResource();
                return;
            }

            _browseStack.Push( new ResourceListBrowseState( ownerResource, resources, options ) );
            DoShowResources( ownerResource, resources, options );
        }

        public void DisplayConfigurableResourceList( IResource ownerResource, IResourceList resList,
                                                     ResourceListDisplayOptions options )
        {
            options = new ResourceListDisplayOptions( options );
            if ( ownerResource != null )
            {
                if ( ownerResource.HasProp( Core.Props.DisplayUnread ) )
                {
                    resList = resList.Intersect( Core.ResourceStore.FindResourcesWithProp(
                        SelectionType.LiveSnapshot, null, Core.Props.IsUnread ), true );
                    options.Caption = "Unread Items in " + options.Caption;
                }
                if ( ownerResource.HasProp( Core.Props.DisplayThreaded ) && options.ThreadingHandler == null )
                {
                    options.ThreadingHandler = Core.PluginLoader.CompositeThreadingHandler;
                }
                if ( ownerResource.HasProp( Core.Props.DisplayNewspaper ) && ActiveTabHasNewspaperProviders() )
                {
                    options.ShowNewspaper = true;
                }
            }
            DisplayResourceList( ownerResource, resList, options );
        }

        private static bool ActiveTabHasNewspaperProviders()
        {
            string[] resTypes = Core.TabManager.CurrentTab.GetResourceTypes();
            if ( resTypes == null )
            {
                return false;
            }
            for( int i=0; i<resTypes.Length; i++ )
            {
                if ( Core.PluginLoader.GetNewspaperProvider( resTypes [i] ) != null )
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * Displays the specified resource list in threaded mode.
         */

        public void DisplayThreadedResourceList( IResource ownerResource, IResourceList resources,
                                                 string caption, string sortProp, int replyProp,
                                                 ColumnDescriptor[] columns, IResource selectedResource )
        {
            #region Preconditions
            if ( resources == null )
                throw new ArgumentNullException( "resources" );
            #endregion Preconditions

            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            options.Caption = caption;
            if ( sortProp != null )
            {
                options.SortSettings = SortSettings.Parse( Core.ResourceStore, sortProp );
            }
            options.ThreadingHandler = new DefaultThreadingHandler( replyProp );
            options.Columns = columns;
            options.SelectedResource = selectedResource;

            DisplayResourceList( ownerResource, resources, options );
        }

        private void AttachDataProvider()
        {
            #region Preconditions
            if ( _listView.DataProvider != null )
            {
                throw new InvalidOperationException( "Old data provider was not detached before attaching new data provider" );
            }
            #endregion Preconditions

            _dataProvider.ResourceCountChanged += HandleResourceCountChanged;
            _dataProvider.SortChanged += HandleSortChanged;
            _listView.DataProvider = _dataProvider;
        }

        /**
         * Adds the conversation containing the specified resource to the listview.
         */

        public void DisplayConversation( IResource res )
        {
            IResourceList convResourceList = ConversationBuilder.UnrollConversation( res );
            convResourceList = convResourceList.Minus( Core.ResourceStore.FindResourcesWithProp( null, Core.Props.IsDeleted ) );
            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            options.Caption = "Conversation on " + res.DisplayName;
            options.ThreadingHandler = Core.PluginLoader.CompositeThreadingHandler;

            DisplayResourceList( null, convResourceList, options );
            ExpandConversation( res );
            SelectResource( res );
        }

        private delegate void DoShowResourcesDelegate( IResource ownerResource, IResourceList resources,
            ResourceListDisplayOptions options );

        internal void DoShowResources( IResource ownerResource, IResourceList resources,
                                       ResourceListDisplayOptions options )
        {
            if ( !_displayResourceListReenterAllowed && _displayResourceListReenter > 0 )
            {
                // the reentering can happen, for example, because of synchronous ResourceProxy.Delete()
                // call in UnhookResourceList() (OM-11699)
                Core.UserInterfaceAP.QueueJob( new DoShowResourcesDelegate( DoShowResources ),
                                               ownerResource, resources, options );
                return;
            }
            _listView.JetListView.SuspendLayout();
            _displayResourceListReenter++;
            try
            {
                EndDisplayLastResource();
                UnhookResourceList( ownerResource );
                HideStatusLine();
                _seeAlsoBar.Visible = false;
                CancelWebMode();

                _origFilterResourceList = options.TabFilter ? _filterResourceList : null;
                _ownerResource = ownerResource;

                OnContentChanged();

                if ( options.TransientContainerParent != null && options.TransientContainerPaneId != null )
                {
                    _displayResourceListReenterAllowed = true;
                    try
                    {
                        if ( Core.LeftSidebar.ActivePaneId != options.TransientContainerPaneId )
                        {
                            Core.LeftSidebar.ActivateViewPane( options.TransientContainerPaneId );
                            UnhookResourceList( ownerResource );
                        }

                        ResourceProxy proxy = ResourceProxy.BeginNewResource( "TransientContainer" );
                        proxy.SetProp( Core.Props.Name, options.Caption );
                        proxy.AddLink( Core.Props.Parent, options.TransientContainerParent );
                        proxy.EndUpdate();
                        _transientContainer = proxy.Resource;
                        Core.LeftSidebar.GetPane( options.TransientContainerPaneId ).SelectResource( _transientContainer, true );
                        _ownerResource = _transientContainer;
                        _seeAlsoBar.Visible = false;     // it may have been shown because of view selection done at ActivateViewPane()
                    }
                    finally
                    {
                        _displayResourceListReenterAllowed = false;
                        UnhookResourceList( _transientContainer );
                    }
                }

                IResourceList listValid = resources;
                _origResourceList = resources;  // store the list before filtering intersections
                if ( _ownerResource != null && !_ownerResource.HasProp( Core.Props.ShowDeletedItems ) )
                {
                    resources = resources.Minus( Core.ResourceStore.FindResourcesWithProp( null, Core.Props.IsDeleted ) );
                    listValid = resources;
                }
                if ( options.TabFilter && _filterResourceList != null )
                {
                    resources = resources.Intersect( _filterResourceList );
                }

                if ( options.ShowNewspaper )
                {
                    DoShowNewspaper( ownerResource, resources, options );
                }
                else
                {
                    ShowRegularResourceList( ownerResource, resources, listValid, options );
                }
            }
            finally
            {
                _listView.JetListView.ResumeLayout();
                _displayResourceListReenter--;
            }

            if ( options.StatusLine != null )
            {
                AddStatusLine( options.StatusLine, options.StatusLineClickHandler );
            }
        }

        private void CheckPanesVisibility()
        {
            _listAndContentSplitter.Visible = !(_mode == BrowserPanesVisibilityMode.ContentOnly) &&
                                !(_mode == BrowserPanesVisibilityMode.ListOnly);
            _listViewPanel.Visible = _listView.Visible = (_mode != BrowserPanesVisibilityMode.ContentOnly);
            _lowerPaneBackground.Visible = !(_mode == BrowserPanesVisibilityMode.ListOnly);
        }

        private void ShowRegularResourceList( IResource ownerResource, IResourceList resources,
                                              IResourceList seeAlsoList, ResourceListDisplayOptions options )
        {
            ColumnDescriptor[] columns = CheckGetDefaultColumns( options.Columns, resources );

            _resourceList = resources;
            _highlightProvider = options.HighlightDataProvider;
            _suppressContexts = options.SuppressContexts;
            if ( _updateCount == 0 )
            {
                _toolBarPanel.Visible = true;
            }
            _caption = options.Caption;
            _captionTemplate = options.CaptionTemplate;
            if ( _captionTemplate != null && ownerResource != null )
            {
                _ownerResourceList = ownerResource.ToResourceListLive();
                _ownerResourceList.ResourceChanged += HandleOwnerResourceChanged;
            }
            columns = _columnManager.CreateTypeColumn( columns );

            CheckPanesVisibility();

            if ( options.ThreadingHandler == null )
            {
                if ( _dataProvider != null && !(_dataProvider is ConversationDataProvider) &&
                    _dataProvider.ResourceList == resources )
                {
                    DisplaySelectedResource();
                    return;
                }
                _dataProvider = new ResourceListDataProvider( resources );
            }
            else
            {
                ConversationDataProvider conversationDataProvider = new ConversationDataProvider( resources, options.ThreadingHandler );
                _dataProvider = conversationDataProvider;
            }

            _listState = _columnManager.GetListViewState( ownerResource, resources, columns, options.DefaultGroupItems );
            _groupItems = _listState.GroupItems;
            ColumnDescriptor[] stateColumns = _listState.Columns;
            _columnManager.RestoreCustomComparers( columns, ref stateColumns );
            ShowListViewColumns( _listState.Columns );

            SortSettings sortSettings = _listState.SortSettings;
            if ( sortSettings == null || sortSettings.SortProps.Length == 0 )
            {
                if ( options.SortSettings != null )
                {
                    sortSettings = options.SortSettings;
                }
                else
                {
                    sortSettings = resources.SortSettings;
                }
            }

            _dataProvider.SetInitialSort( sortSettings );

            if ( _hiddenColumnState != null && _hiddenColumnState.HiddenColumnCount > 0 )
            {
                _listView.JetListView.ItemUpdated += HandleItemUpdated;
            }

            if ( _verticalLayout )
            {
                _listView.ColumnSchemeProvider = new ResourceColumnSchemeProvider( _columnManager, _listView );
            }

            if ( options.EmptyText != null )
            {
                _listView.EmptyText = options.EmptyText;
            }
            else
            {
                _listView.EmptyText = JetListView.DefaultEmptyText;
            }

            AttachDataProvider();
            SetInitialSelection( options.SelectedResource );

            UpdateCaption();
            UpdateAutoPreviewColumn();

            if ( _listView.ActiveResource == null )
            {
                DisplayEmptyLowerPane();
            }

            if ( options.SeeAlsoBar )
            {
                ShowSeeAlsoBar( seeAlsoList );
            }
        }

        private void HandleOwnerResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( UpdateCaption ) );
        }

        /**
         * When the count of resources in a conversation is changed, updates the caption.
         */

        private void HandleResourceCountChanged( object sender, EventArgs e )
        {
            Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddMilliseconds( 100 ), new MethodInvoker( UpdateCaption ) );
            if ( !Core.UserInterfaceAP.IsOwnerThread )
            {
                if ( _resourceList != null && _resourceList.Count > 0 && _listState != null && _listState.OwnerResource == null &&
                    _listState.KeyTypes != null && (_listState.KeyTypes.Length == 0 || _listState.KeyTypes [0].Length == 0 ) )
                {
                    Core.UIManager.QueueUIJob( new ResourceListDelegate( LoadStateForFirstResource ), _resourceList );
                }
            }
        }

        private void LoadStateForFirstResource( IResourceList resourceList )
        {
            if ( _resourceList != resourceList )
            {
                // the resource list has changed while the event was queued
                return;
            }

            ColumnDescriptor[] columns = CheckGetDefaultColumns( null, _resourceList );
            _listState = _columnManager.GetListViewState( _ownerResource, _resourceList, columns, true );
            _groupItems = _listState.GroupItems;

            ColumnDescriptor[] stateColumns = _listState.Columns;
            _columnManager.RestoreCustomComparers( columns, ref stateColumns );
            ShowListViewColumns( _listState.Columns );
            _dataProvider.ApplySortSettings( _listView.JetListView, _listState.SortSettings );
        }

        private void SetInitialSelection( IResource selectedResource )
        {
            if ( selectedResource != null )
            {
                // _dataProvider.ResourceList may be a live snapshot resource list,
                // so checking it with FindResource() and actually evaluating it
                // can have different results
                if ( _dataProvider.FindResourceNode( selectedResource ) )
                {
                    _listView.Selection.Clear();
                    _listView.Selection.AddIfPresent( selectedResource );
                }
            }
        }

        private ColumnDescriptor[] CheckGetDefaultColumns( ColumnDescriptor[] columns, IResourceList resources )
        {
            if ( columns == null )
            {
                if ( _defaultColumns == null )
                {
                    return _columnManager.GetDefaultColumns( resources );
                }
                return _defaultColumns;
            }
            return columns;
        }

        /**
         * Shows the columns in the specified list in the list view.
         */

        public void ShowListViewColumns( ColumnDescriptor[] columns )
        {
            #region Preconditions
            if ( columns == null )
                throw new ArgumentNullException( "columns" );
            #endregion Preconditions

            if ( _dataProvider != null )
            {
                columns = _columnManager.CreateTypeColumn( columns );
                if ( _verticalLayout )
                {
                    _hiddenColumnState = null;
                }
                else
                {
                    _hiddenColumnState = new HiddenColumnState();
                    columns = _hiddenColumnState.HideEmptyColumns( columns, _dataProvider.ResourceList );
                }
                _columnManager.ShowListViewColumns( _listView, columns, _dataProvider, _groupItems );
            }
        }

        public void ShowColumnsForResourceList()
        {
            if ( _resourceList != null )
            {
                ShowListViewColumns( _columnManager.GetDefaultColumns( _resourceList ) );
            }
        }

        /// <summary>
        /// If necessary, saves the state (columns and sorting) of the resource browser. Also
        /// deinitializes the resource browser.
        /// </summary>
        public void Shutdown()
        {
            SaveLayoutSettings();
            CheckSaveListState( false );
            _listState = null;
            UnhookResourceList( null );
        }

        internal void UnhookResourceList( IResource newOwnerResource )
        {
            if ( NewspaperVisible )
            {
                SuspendLayout();
                try
                {
                    HideNewspaper();
                    _listViewPanel.Visible = true;
                }
                finally
                {
                    ResumeLayout();
                }
            }
            if ( _ownerResourceList != null )
            {
                _ownerResourceList.Dispose();
                _ownerResourceList.ResourceChanged -= HandleOwnerResourceChanged;
                _ownerResourceList = null;
            }
            if ( _resourceList != null )
            {
                _resourceList.Deinstantiate();
                _resourceList = null;
            }
            if ( _singleResourceList != null )
            {
                _singleResourceList.ResourceDeleting -= OnResourceDeleting;
                _singleResourceList.Dispose();
                _singleResourceList = null;
            }
            if ( _dataProvider != null )
            {
                _dataProvider.ResourceCountChanged -= HandleResourceCountChanged;
                _dataProvider.SortChanged -= HandleSortChanged;
                _dataProvider.Dispose();
                _dataProvider = null;
                _listView.DataProvider = null;
                _listView.JetListView.GroupProvider = null;
            }
            if ( _transientContainer != null && _transientContainer != newOwnerResource )
            {
                new ResourceProxy( _transientContainer ).Delete();
                _transientContainer = null;
            }
            _listView.JetListView.ItemUpdated -= HandleItemUpdated;
            _listState = null;
            HideAnnotationForm();
        }

        private void HideNewspaper()
        {
            _newspaperViewer.HideNewspaper();
            _newspaperViewer.Visible = false;
        }

        private void HookSingleResourceList( IResource res )
        {
            _singleResourceList = res.ToResourceListLive();
            _singleResourceList.ResourceDeleting += OnResourceDeleting;
        }

        private void HandleItemUpdated( object sender, ItemEventArgs e )
        {
            IResource res = (IResource) e.Item;
            Core.UserInterfaceAP.QueueJob( new ResourceDelegate( CheckUpdateHiddenColumns ), res );
        }

        private void CheckUpdateHiddenColumns( IResource res )
        {
            if ( _hiddenColumnState != null && _listState != null && _dataProvider != null &&
                _hiddenColumnState.HiddenColumnsChanged( res, _dataProvider.ResourceList ) )
            {
                ShowListViewColumns( _listState.Columns );
            }
        }

        private void OnResourceDeleting( object sender, ResourceIndexEventArgs e )
        {
            if ( InvokeRequired )
            {
                Core.UIManager.QueueUIJob( new ResourceIndexEventHandler( OnResourceDeleting ), new object[] { sender, e } );
            }
            else
            {
                _browseStack.DropTop();
            }
        }

        private void UpdateCaption()
        {
            if ( Core.State == CoreState.ShuttingDown )
            {
                return;
            }

            if ( _captionTemplate != null && _ownerResource != null )
            {
                _caption = _captionTemplate.Replace( "%OWNER%", _ownerResource.DisplayName );
            }

            string caption = (_captionPrefix == null) ? _caption : _captionPrefix + " | " + _caption;

            int itemCount = -1;
            if (_itemCountWriter != null)
            {
                if ( NewspaperVisible )
                {
                    itemCount = _newspaperViewer.ItemsInViewCount;
                }
                else if ( _dataProvider != null )
                {
                    if( _dataProvider.ResourceList != null )
                        itemCount = _dataProvider.ResourceList.Count;
                }
                if ( itemCount >= 0 )
                {
                    _itemCountWriter.ShowStatus( itemCount + " Items" );
                }
            }

            if (itemCount >= 0)
            {
                caption += " (" + ((itemCount == 0) ? "no" : itemCount.ToString()) + " items)";
            }

            _captionLabel.Text = caption;
        }

        /**
         * Sets the focus to the resource list.
         */

        public void FocusResourceList()
        {
            _listView.Focus();
            if ( _listView.Selection.Count == 0 && _listView.JetListView.Nodes.Count > 0 )
            {
                _listView.Selection.Add( _listView.JetListView.Nodes [0].Data );
            }
        }

        public IResourceList SelectedResources
        {
            get
            {
                if ( _webPageMode )
                    return Core.ResourceStore.EmptyResourceList;

                if ( _listView.Visible )
                    return _listView.GetSelectedResources();

                if ( NewspaperVisible )
					return _newspaperViewer.SelectedResources;

                if ( _lastDisplayedResource != null )
                    return _lastDisplayedResource.ToResourceList();

                return Core.ResourceStore.EmptyResourceList;
            }
        }

        public IResourceList SelectedResourcesExpanded
        {
            get
            {
                IResourceList selection = SelectedResources;
                ConversationDataProvider provider = _dataProvider as ConversationDataProvider;
                if ( provider != null )
                {
                    return provider.ExpandSelectedResources( selection );
                }

                return selection;
            }
        }

        public bool ResourceListVisible
        {
            get { return _listView.Visible; }
        }

        public bool ResourceListSplitterVisible
        {
            get { return _listAndContentSplitter.Visible; }
        }

        public bool NewspaperVisible
        {
            get { return _newspaperViewer != null &&
                      _newspaperViewer.State != NewspaperViewer.NewspaperState.Deactivated; }
        }

        public IActionContext GetContext( ActionContextKind kind )
        {
            ActionContext context = new ActionContext( kind, this, SelectedResources );
            context.SetCommandProcessor( this );
            context.SetListOwner( _ownerResource );
            context.SetSelectedResourcesExpanded( SelectedResourcesExpanded );
            context.SetOwnerForm( FindForm() );
            string url = Core.WebBrowser.CurrentUrl;
            if ( !string.IsNullOrEmpty( url ) && url != "about:blank" )
            {
            	context.SetCurrentUrl( url );
                context.SetCurrentPageTitle( Core.WebBrowser.Title );
            }
            if ( NewspaperVisible )
            {
                string plainText, html;
                _newspaperViewer.GetSelectedText( out plainText, out html );
                context.SetSelectedText( html, plainText, TextFormat.Html );
            }
            else if ( _displayPane != null )
            {
                TextFormat fmt = TextFormat.PlainText;
                string selText = _displayPane.GetSelectedText( ref fmt );
                string selPlainText;
                if ( fmt == TextFormat.PlainText )
                {
                    selPlainText = selText;
                }
                else
                {
                    selPlainText = _displayPane.GetSelectedPlainText();
                }
                if ( !string.IsNullOrEmpty( selText ) )
                {
                    context.SetSelectedText( selText, selPlainText, fmt );
                }
            }
        	return context;
        }

        /**
         * Returns the list of resources currently visible in the resource browser.
         */

        public IResourceList VisibleResources
        {
            get
            {
                if ( NewspaperVisible )
                {
                    return _newspaperViewer.NewspaperResources;
                }
                if ( _dataProvider != null )
                {
                    return _dataProvider.ResourceList;
                }
                return Core.ResourceStore.EmptyResourceList;
            }
        }

        /**
         * Selects the specified resource in the list view.
         * @return true if the resource was found in the list, false otherwise
         */

        public bool SelectResource( IResource res )
        {
            if ( NewspaperVisible )
            {
                return _newspaperViewer.SelectResource( res );
            }

/*
            if ( !_listView.Visible )
                return false;
*/

            if ( _dataProvider == null || !_dataProvider.FindResourceNode( res ) )
            {
                return false;
            }

            // in case of a live snapshot resource list, it is possible that FindResourceNode()
            // (Contains()) will return true, but the item will not actually be present in the list (OM-8538)
            if ( _listView.JetListView.NodeCollection.Contains( res ) )
            {
                _listView.Selection.Clear();
                return _listView.Selection.AddIfPresent( res );
            }
            return false;
        }

        public void ExpandConversation( IResource res )
        {
            Guard.NullArgument( res, "res" );
            ConversationDataProvider dataProvider = _dataProvider as ConversationDataProvider;
            if ( dataProvider != null )
            {
                dataProvider.ExpandConversation( res );
            }
        }

        /**
         * Begins label editing for the specified resource in the list view.
         */

        public void EditResourceLabel( IResource res )
        {
            _listView.EditResourceLabel( res );
        }

        private void HandleActiveResourceChanged( object sender, EventArgs e )
        {
            if ( _dataProvider != null && !_listView.KeyNavigation )
            {
                if ( Core.UserInterfaceAP.IsOwnerThread )
                {
                    DisplaySelectedResource();
                }
                else
                {
                    Core.UIManager.QueueUIJob( new MethodInvoker( DisplaySelectedResource ) );
                }
            }
        }

        private void DisplaySelectedResource()
        {
            if ( Core.State != CoreState.ShuttingDown )
            {
                if( _mode != BrowserPanesVisibilityMode.ListOnly )
                {
                    IResource res = _listView.ActiveResource;
                    if ( res != null )
                    {
                        DisplayResourceData( res );
                    }
                    else
                    {
                        DisplayEmptyLowerPane();
                    }
                }
            }
        }

        private void HandleKeyNavigationCompleted( object sender, EventArgs e )
        {
            DisplaySelectedResource();
        }

        private void HandleResourceSelectionChanged( object sender, EventArgs e )
        {
            if ( _linksBar.LinksPaneExpanded )
            {
                Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddSeconds( 0.1 ), new
                    MethodInvoker( DoUpdateLinksPane ) );
            }
        }

        private void DoUpdateLinksPane()
        {
            if ( _listView.Selection.Count > 1 || _listView.JetListView.NodeCollection.IsEmpty )
            {
                _linksPane.DisplayLinks( _listView.GetSelectedResources(), null );
                _lastDisplayedResourceCount = _listView.Selection.Count;
            }
        }

        private void DisplayEmptyLowerPane()
        {
            CancelWebMode();
            EndDisplayLastResource();
            DisposeDisplayPane();

            // this could cause LV focusing and display of a resource => check if the pane is still actually empty
            if ( _listView.ActiveResource == null )
            {
                if ( _linksBar.LinksPaneExpanded )
                {
                    _linksPane.DisplayLinks( null, null );
                }
                _linksBar.DisplayLinks( null, null );

                _lowerPane.Visible = false;
                _linksBar.Visible = false;
            }
        }

        public void RedisplaySelectedResource()
        {
            if ( InvokeRequired )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( RedisplaySelectedResource ) );
            }
            else
            {
                IResource res = _lastDisplayedResource;
                _lastDisplayedResource = null;
                if ( _listView.Visible )
                {
                    DisplaySelectedResource();
                }
                else if ( res != null )
                {
                    DoShowResource( res, (_singleResourceList != null) );
                }
            }
        }

        private void CancelWebMode()
        {
            if ( _updateCount == 0 )
            {
                _urlBarText = null;
                _webAddressPanel.Visible = false;
                WebPageMode = false;
            }
        }

        /**
         * Shows the specified resource in the links and preview panes.
         */

        private void DisplayResourceData( IResource res )
        {
            _lowerPane.Visible = true;
            _linksBar.Visible = true;
            _delegateOrigResource = null;
            if ( _lastDisplayedResource == res && _lastDisplayedResourceCount == 1 )
            {
                return;
            }

            _lastDisplayedResourceCount = 1;
            CancelWebMode();
            _tmrMarkAsRead.Stop();

            IResource displayRes = res;
            ResourceDisplayForwarderCallback forwarder = (ResourceDisplayForwarderCallback) _displayForwarders [res.Type];
            if ( forwarder != null )
            {
                _delegateOrigResource = res;
                displayRes = forwarder( res );
            }

            string resType = displayRes.Type;

            EndDisplayLastResource();

            if ( _lastDisplayedType == null || resType != _lastDisplayedType ||
                ( _displayPane != null && _displayPane.GetControl().Parent != _bodyPane ) )
            {
                DisposeDisplayPane();
                if ( resType != "" )
                {
                    _lastPlugin = Core.PluginLoader.GetResourceDisplayer( resType );

                    if ( _lastPlugin != null )
                    {
                        try
                        {
                            _displayPane = _lastPlugin.CreateDisplayPane( resType );
                        }
                        catch( Exception e )
                        {
                            _displayPane = new LabelDisplayPane( "Error displaying resource: " + e.Message );
                            _lastPlugin = null;
                        }

                        if ( _displayPane != null )
                        {
                            try
                            {
                                ShowDisplayPaneControl( _displayPane.GetControl() );
                            }
                            catch( COMException ex )
                            {
                                _displayPane.DisposePane();
                                _lastPlugin = null;

                                _displayPane = new LabelDisplayPane( "Error creating ActiveX control for resource: " + ex.Message );
                                ShowDisplayPaneControl( _displayPane.GetControl() );
                            }
                        }
                    }
                }
                _lastDisplayedType = resType;
                _bodyPane.Invalidate();
            }

            if ( resType != "" && _lastPlugin != null && _displayPane != null )
            {
                DisplayResourceInPlugin( displayRes );
            }

            if ( !res.IsDeleting )
            {
                res.Lock();
                try
                {
                    ILinksPaneFilter filter = (ILinksPaneFilter) _linksPaneFilters [res.Type];
                    _linksBar.DisplayLinks( res, filter );
                    if ( _linksBar.LinksPaneExpanded )
                    {
                        _linksPane.DisplayLinks( res.ToResourceList(), filter );
                    }

                    if ( res.HasProp( Core.Props.IsUnread ) || displayRes.HasProp( Core.Props.IsUnread ) )
                    {
                        if ( _tmrMarkAsRead_TimeOut == 1 )
                        {
                            MarkLastResourceRead( true );
                        }
                        else if ( _tmrMarkAsRead_TimeOut > 0 )
                        {
                            _tmrMarkAsRead.Interval = _tmrMarkAsRead_TimeOut;
                            _tmrMarkAsRead.Start();
                        }
                    }
                }
                finally
                {
                    res.UnLock();
                }

                if ( _viewAnnotations && res.HasProp( Core.Props.Annotation ))
                {
                    GetAnnotationForm().ShowAnnotation( res, _viewAnnotations );
                }
                else
                {
                    HideAnnotationForm();
                }
            }
        }

        private void ShowDisplayPaneControl( Control ctl )
        {
            ctl.Dock = DockStyle.None;
            ctl.Size = new Size( _bodyPane.ClientRectangle.Width - _cSpaceMargin * 2 - 1, _bodyPane.ClientRectangle.Height - _cSpaceMargin * 2 - 1 );
            ctl.Location = new Point( _cSpaceMargin, _cSpaceMargin + 1 );
            ctl.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            while( _bodyPane.Controls.Count > 0 )
            {
                Trace.WriteLine( "Removing stale control " + _bodyPane.Controls [0] + " from body pane" );
                _bodyPane.Controls.Remove( _bodyPane.Controls [0] );
            }
            Trace.WriteLine( "Showing display pane control " + ctl );
            _bodyPane.Controls.Add( ctl );
        }

        /**
         * Notifies the plugin that the resource which was previously displayed
         * is no longer displayed.
         */

        private void EndDisplayLastResource()
        {
            if ( _displayPane != null && _lastDisplayedResource != null )
            {
                _displayPane.EndDisplayResource( _lastDisplayedResource );
                _lastDisplayedResource = null;
                HideAnnotationForm();
            }
        }

        /// <summary>
        /// Displays the resource in the currently active plugin.
        /// </summary>
        private void DisplayResourceInPlugin( IResource res )
        {
            _lastDisplayedResource = res;

            if ( _highlightProvider != null && _displayPane is IDisplayPane2 )
            {
                WordPtr[] words;
                if ( _highlightProvider.GetHighlightData( res, out words ) )
                {
                    IDisplayPane2 displayPane2 = (IDisplayPane2) _displayPane;
                    displayPane2.DisplayResource( res, words );
                }
                else
                {
                    _displayPane.DisplayResource( res );
                }
            }
            else
            {
                IDisplayPane displayPane = _displayPane;
                displayPane.DisplayResource( res );
                if ( _highlightProvider != null )
                {
                    WordPtr[] words;
                    // DisplayResource() may have caused message pumping which lead to disposing of the
                    // display pane (OM-8392)
                    if ( _highlightProvider.GetHighlightData( res, out words ) && _displayPane == displayPane )
                    {
                        _displayPane.HighlightWords( words );
                    }
                }
            }
        }

        /// <summary>
        /// If a resource display pane exists, destroys it.
        /// </summary>
        public void DisposeDisplayPane()
        {
            Guard.OwnerThread( Core.UserInterfaceAP );
            IDisplayPane displayPane = _displayPane;                  // OM-10971
            if ( displayPane != null )
            {
                _bodyPane.Controls.Remove( displayPane.GetControl() );
                try
                {
                    displayPane.DisposePane();
                }
                catch( Exception e )
                {
                    Core.ReportException( e, false );
                }
                _displayPane = null;
                _lastPlugin = null;
                _lastDisplayedType = null;
            }
        }

        /**
         * Reads the setting for the "mark as read" timeout from the INI file.
         */

        private void UpdateSettings()
        {
            ISettingStore ini = Core.SettingStore;
            _tmrMarkAsRead_TimeOut = ini.ReadInt( "Resources", "MarkAsReadTimeOut", 2000 );
            BrowseStack.MaxBrowseStackSize = ini.ReadInt( "Resources", "MaxBrowseStackSize", 10 );
        }

        private void OnInterfaceOptionsChanged( object sender, EventArgs e )
        {
            UpdateSettings();
        }

        /**
         * After an unread resource is viewed for a certain interval (2 seconds),
         * marks it as read.
         */

        private void _tmrMarkAsRead_Tick( object sender, EventArgs e )
        {
            _tmrMarkAsRead.Stop();
            if ( Core.State != CoreState.ShuttingDown )
            {
                MarkLastResourceRead( true );
            }
        }

        /**
         * Marks the last displayed resource as read.
         */

        private void MarkLastResourceRead( bool async )
        {
            // if DelegateDisplayResource() was used, set the unread flag for both
            // the resource which was displayed originally and the resource to which
            // the display was delegated (#1538)
            if ( _lastDisplayedResource != null )
            {
                MarkResourceRead( _lastDisplayedResource, async );
            }

            if ( _delegateOrigResource != null && _delegateOrigResource != _lastDisplayedResource )
            {
                MarkResourceRead( _delegateOrigResource, async );
            }
        }

        private void MarkResourceRead( IResource res, bool async )
        {
            if( async )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate,
                    "Marking resource read by timer", new ResourceDelegate( MarkResourceRead ), res );
            }
            else
            {
                _inMarkResourceRead = true;
                try
                {
                    Core.ResourceAP.RunUniqueJob(
                        "Marking resource read by timer", new ResourceDelegate( MarkResourceRead ), res );
                }
                finally
                {
                    _inMarkResourceRead = false;
                }
            }
        }

        private static void MarkResourceRead( IResource res )
        {
            if ( !res.IsDeleted )
            {
                res.SetProp( Core.Props.IsUnread, false );
            }
        }

        private void OnListViewKeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyData == Keys.Space )
            {
                if ( _displayPane != null && _displayPane.CanExecuteCommand( DisplayPaneCommands.PageDown ) )
                {
                    _displayPane.ExecuteCommand( DisplayPaneCommands.PageDown );
                }
                else if ( !_inMarkResourceRead )
                {
                    GotoNextUnread();
                }
                e.Handled = true;
            }
        }

        /**
         * Shows the column configuration dialog for the currently displayed
         * resource list.
         */

        public void ConfigureColumns()
        {
            if ( _dataProvider != null )
            {
                IResourceList resList = _dataProvider.ResourceList;
                using( ConfigureColumnsDialog dlg = new ConfigureColumnsDialog() )
                {
                    _listState = dlg.ConfigureColumns( _listState, resList, _ownerResource );
                    // the contents of the resource browser may have been changed programmatically (OM-8718)
                    if ( _dataProvider != null && _dataProvider.ResourceList == resList )
                    {
                        ShowListViewColumns( _listState.Columns );
                        _dataProvider.UpdateSortColumn();
                    }
                }
            }
        }

        private void HandleHeaderContextMenuPopup( object sender, EventArgs e )
        {
            miShowItemsInGroups.Checked = _groupItems;
        }

        private void miConfigureColumns_Click( object sender, EventArgs e )
        {
            ConfigureColumns();
            CheckSaveListState( false );
        }

        public void miShowItemsInGroups_Click( object sender, EventArgs e )
        {
            GroupItems = !GroupItems;
        }

        private void _listView_ColumnSizeChanged( object sender, EventArgs e )
        {
            CheckSaveListState( true );
        }

        private void _listView_ColumnOrderChanged( object sender, EventArgs e )
        {
            AdjustSpecialColumnPositions();
            CheckSaveListState( true );
        }

        /// <summary>
        /// Ensures that the special columns (tree structure and icon) are in their correct place
        /// in the column list (before the first non-fixed-size column).
        /// </summary>
        private void AdjustSpecialColumnPositions()
        {
            int firstNonFixedColumnIndex = -1;
            int iconColumnIndex = -1;
            int treeStructureColumnIndex = -1;
            JetListViewColumn iconColumn = null;
            JetListViewColumn treeStructureColumn = null;

            for( int i=0; i<_listView.Columns.Count; i++ )
            {
                JetListViewColumn col = _listView.Columns [i];
                if ( firstNonFixedColumnIndex == -1 && !col.FixedSize && !col.IsIndentColumn() )
                {
                    firstNonFixedColumnIndex = i;
                }
                else if ( col is ResourceIconColumn )
                {
                    iconColumnIndex = i;
                    iconColumn = col;
                }
                else if ( col.IsIndentColumn() )
                {
                    treeStructureColumnIndex = i;
                    treeStructureColumn = col;
                }
            }

            if ( iconColumnIndex == firstNonFixedColumnIndex -1 &&
                (treeStructureColumnIndex == -1 || treeStructureColumnIndex == firstNonFixedColumnIndex-2 ) )
            {
                return;
            }

            if ( iconColumnIndex < firstNonFixedColumnIndex )
            {
                firstNonFixedColumnIndex--;
            }
            _listView.Columns.Remove( iconColumn );
            _listView.Columns.Insert( firstNonFixedColumnIndex, iconColumn );
            if ( treeStructureColumn != null )
            {
                if ( treeStructureColumnIndex < firstNonFixedColumnIndex )
                {
                    firstNonFixedColumnIndex--;
                }
                _listView.Columns.Remove( treeStructureColumn );
                _listView.Columns.Insert( firstNonFixedColumnIndex, treeStructureColumn );
            }
        }

        /**
         * When a custom property displayed in a column is deleted, remove the column.
         */

        private void OnCustomPropertyDeleting( object sender, ResourceIndexEventArgs e )
        {
            int propId = e.Resource.GetIntProp( "ID" );

            foreach( JetListViewColumn col in _listView.Columns )
            {
                ResourcePropsColumn rpCol = col as ResourcePropsColumn;
                if ( rpCol != null && rpCol.PropIds.Length == 1 && rpCol.PropIds [0] == propId )
                {
                    _listView.Columns.Remove( rpCol );
                    CheckSaveListState( false );
                    break;
                }
            }
        }

        private void HandleSortChanged( object sender, EventArgs e )
        {
            CheckSaveListState( true );
        }

        private void CheckSaveListState( bool async )
        {
            if ( _listState != null && _dataProvider != null )
            {
                _listState.GroupItems = _groupItems;
                _columnManager.SaveListViewState( _listView, _dataProvider, _listState, async );
            }
        }

        #region GoTo navigation support
        public bool GotoNext()
        {
            ResourceListView2.LocateMatchCallback matcher = MatchAnyResource;
            GetNextResourceDelegate mover = _listView.LocateNextResource;
            GetNextViewDelegate viewMover = GoNextView;

            // - Move down in the list
            // - Match any resource in the current list
            // - Select next view in the tree if current list ends
            // - When a view is changed, select its first item in list
            // - Do not try to change direction in the case of no match in primary direction
            return MoveToNextItem( mover, matcher, viewMover, true, false );
        }

        public bool GotoNextUnread()
		{
            ResourceListView2.LocateMatchCallback matcher = MatchUnreadResource;
            GetNextResourceDelegate mover = _listView.LocateNextResource;
            GetNextViewDelegate viewMover = GoNextUnreadView;

            // - Move down in the list
            // - Match only unread resource in the current list
            // - Select next view in the tree if current list ends
            // - When a view is changed, select its first item in list
            // - Try to change direction in the case of no match in primary direction
            return MoveToNextItem( mover, matcher, viewMover, true, true );
		}

        public bool GotoPrev()
        {
            ResourceListView2.LocateMatchCallback matcher = MatchAnyResource;
            GetNextResourceDelegate mover = _listView.LocatePrevResource;
            GetNextViewDelegate viewMover = GoPrevView;

            // - Move up in the list
            // - Match any resource in the current list
            // - Select previous view in the tree if current list ends
            // - When a view is changed, select its last item in list
            // - Do not try to change direction in the case of no match in primary direction
            return MoveToNextItem( mover, matcher, viewMover, false, false );
        }

        public bool GotoPrevUnread()
		{
            ResourceListView2.LocateMatchCallback matcher = MatchUnreadResource;
            GetNextResourceDelegate mover = _listView.LocatePrevResource;
            GetNextViewDelegate viewMover = GoPrevView;

            // - Move up in the list
            // - Match only unread resource in the current list
            // - Select previous view in the tree if current list ends
            // - When a view is changed, select its last item in list
            // - Try to change direction in the case of no match in primary direction
            return MoveToNextItem( mover, matcher, viewMover, false, true );
		}

        private bool MoveToNextItem( GetNextResourceDelegate inListMover,
                                     ResourceListView2.LocateMatchCallback matcher,
                                     GetNextViewDelegate viewMover,
                                     bool selectFirstOnViewChange, bool lookBackwardAlso )
        {
            bool skipFirst = true;

            IResource startResource = _listView.ActiveResource;
            if ( startResource == null && _listView.VisibleItemCount > 0 )
            {
                startResource = (IResource) _listView.JetListView.Nodes[ 0 ].Data;
                skipFirst = false;
            }

            IResource res = null;
            if ( startResource != null )
            {
                res = inListMover( startResource, matcher, skipFirst, lookBackwardAlso );
            }

            if ( res == null && _ownerResource != null )
            {
                VerticalSidebar sidebar = (Core.LeftSidebar as SidebarSwitcher).ActiveSidebar;
                AbstractViewPane viewPane = sidebar.GetPane( sidebar.ActivePaneId );

                if ( viewMover( viewPane, _ownerResource ) )
                {
                    if ( _listView.VisibleItemCount > 0 )
                    {
                        int startIndex = selectFirstOnViewChange ? 0 : _listView.JetListView.Nodes.Count - 1;
                        IResource start = (IResource) _listView.JetListView.Nodes[ startIndex ].Data;

                        res = inListMover( start, matcher, false, lookBackwardAlso );
                    }
                }
            }
            if ( res != null )
            {
                IResourceThreadingHandler handler = Core.PluginLoader.CompositeThreadingHandler;

                //  Check that new resource is not located inside some collapsed
                //  thread and try to expand it from its own parent.
                //--- LX, fix start
                if( !IsJLVNodeVisible( res, _listView ) )
                {
                    IResource parent = handler.GetThreadParent( res );
                    while( parent != null && !IsJLVNodeVisible( parent, _listView ) )
                    {
                        parent = handler.GetThreadParent( parent );
                    }
                    if( parent != null )
                    {
                        ExpandConversation( parent );
                    }
                }

                //---  fix end
                //  After the fix above I don't know whether the subsequent code
                //  fragment is nesessary at all (it's necessary to review the logic)
                //  TODO: check and remove
                IResource nodeParent = handler.GetThreadParent( res );
                if( nodeParent != null && handler.CanExpandThread( nodeParent, ThreadExpandReason.Enumerate ))
                {
                    ExpandConversation( nodeParent );
                }

                _listView.JetListView.ScrollThreadInView( res );

                //--Fix OM-12700:
                //  When the focus isn't on the list and in the preview pane,
                //  using space  bar selects the current item and the first
                //  item on the list(and the preview pane shows the first item's
                //  preview) which isn't the correct behavior.
                //  Comment (LloiX):
                //  JetListView.OnGotFocus is activated between clearing the
                //  selection and setting the new one. Thus first workout focus
                //  switching, set illegal model selection then clear all at once
                //  and set the new correct one.

                if( !Core.ResourceBrowser.ResourceListFocused )
                {
                    Core.ResourceBrowser.FocusResourceList();
                }
                //--End of Fix.

                _listView.SelectSingleItem( res );
                return true;
            }

            return false;
        }

        private static bool MatchUnreadResource( IResource res )
        {
            return res.HasProp( Core.Props.IsUnread );
        }

        private static bool MatchAnyResource( IResource res )
        {
            return true;
        }

        private static bool GoNextUnreadView( AbstractViewPane pane, IResource view )
        {
            return (pane != null && pane.GotoNextUnreadView( view ));
        }

        private static bool GoNextView( AbstractViewPane pane, IResource viewCurrent )
        {
            return (pane != null && pane.GotoNextView( viewCurrent ));
        }

        private static bool GoPrevView( AbstractViewPane pane, IResource viewCurrent )
        {
            return (pane != null && pane.GotoPrevView( viewCurrent ));
        }

        public bool CanGotoNextUnread()
        {
            if ( _inMarkResourceRead )
            {
                return false;
            }
            return NewspaperVisible || (_dataProvider != null && _dataProvider.ResourceList.Count > 0);
        }
        #endregion GoTo navigation support

        #region StatusLine support
        /**
         * Adds the status line above the column headers, which is optionally
         * displayed as a link.
         */

        public void AddStatusLine( string text, EventHandler clickHandler )
        {
            SetStatusLine( text, clickHandler );
        }

        private void SetStatusLine( string text, EventHandler clickHandler )
        {
            _statusLineLabel.Visible = true;
            _statusLineLabel.Text = text;
            /*
            if ( _statusLineLabel.Top < _toolBar.Top )
            {
                Controls.SetChildIndex( _statusLineLabel, Controls.IndexOf( _toolBar )  );
            }
            */

            if ( clickHandler != null )
            {
                _statusLineLabel.ClickableLink = true;
                _statusLineClickHandler = clickHandler;
            }
            else
            {
                _statusLineLabel.ClickableLink = false;
            }
        }

        /**
         * Hides the clickable line of text in the resource list.
         */

        public void HideStatusLine()
        {
            _statusLineLabel.Visible = false;
            _statusLineClickHandler = null;
        }

        private void _statusLineLabel_LinkClicked( object sender, EventArgs e )
        {
            if ( _statusLineClickHandler != null )
            {
                _statusLineClickHandler( sender, EventArgs.Empty );
            }
        }
        #endregion StatusLine support

        #region Web mode support
        public bool WebPageMode
        {
            get { return _webPageMode; }
            set
            {
                if ( _webPageMode != value )
                {
                    SuspendLayout();
                    try
                    {
                        _webPageMode = value;
                        _linksBar.Visible = !_webPageMode;
                        _linksPane.Visible = !_webPageMode && _linksBar.LinksPaneExpanded;
                        _toolBarPanel.Visible = !_webPageMode;
                        _linksPaneSplitter.Visible = _linksPane.Visible;
                        if ( _webPageMode )
                        {
                            _resourceListVisible = _listView.Visible;
                            _statusLineVisible   = _statusLineLabel.Visible;
                            _listView.Visible = false;
                            _listAndContentSplitter.Visible = false;
                            _statusLineLabel.Visible = false;
                            _webModeHiddenControls.Clear();
                            if ( _displayPane != null && _displayPane.GetControl() != Core.WebBrowser )
                            {
                                foreach( Control ctl in _displayPane.GetControl().Controls )
                                {
                                    if ( ctl.Visible && ctl != Core.WebBrowser )
                                    {
                                        ctl.Visible = false;
                                        _webModeHiddenControls.Add( ctl );
                                    }
                                }
                            }
                            Core.WebBrowser.Dock = DockStyle.Fill;
                            _webAddressPanel.Visible = true;
                            //Controls.SetChildIndex( _webAddressPanel, Controls.IndexOf( _toolBar ) - 1 );
                            _webModeSavedCaption = _captionLabel.Text;
                        }
                        else
                        {
                            foreach( Control ctl in _webModeHiddenControls )
                            {
                                ctl.Visible = true;
                            }
                            _webModeHiddenControls.Clear();
                            if ( Core.WebBrowser.Parent == this )
                            {
                                Controls.Remove( Core.WebBrowser );
                            }
                            if ( _urlBarText == null )
                            {
                                _webAddressPanel.Visible = false;
                            }
                            else
                            {
                                ShowUrlBar( _urlBarText );
                            }
                            _captionLabel.Text       = _webModeSavedCaption;
                            _listView.Visible        = _resourceListVisible;
                            _statusLineLabel.Visible = _statusLineVisible;
                            _listAndContentSplitter.Visible        = true;
                        }
                    }
                    finally
                    {
                        ResumeLayout();
                    }
                }
            }
        }

        private void OnWebBrowserTitleChanged( object sender, EventArgs e )
        {
            // we don't detach the event handler, so we need the IsDisposed check (OM-12078)
            if ( _webPageMode && !_captionLabel.IsDisposed )
            {
                _captionLabel.Text = Core.WebBrowser.Title;
            }
        }

        /**
         * If a resource is displayed in a full-page view, shows the URL bar and displays in
         * it the specified URL.
         */

        public void ShowUrlBar( string url )
        {
            Core.WebBrowser.CurrentUrl = url;

            if ( !_webPageMode )
            {
                _urlBarText = url;
            }

            _webAddressPanel.Visible = true;
            _edtURL.Text = url;
            if ( _updateCount == 0 )
            {
                _toolBarPanel.Visible = false;
            }
            _urlBarShown = true;
        }

        internal string UrlBarText
        {
            get
            {
                return !_webAddressPanel.Visible ? null : _edtURL.Text;
            }
        }

        private void _edtURL_KeyDown(object sender, KeyEventArgs e)
        {
            if ( (e.KeyCode == Keys.Enter) && (!e.Alt) && (_edtURL.Text != "") )
            {
                if ( e.Control )
                {
                    _edtURL.Text = "http://www."+ _edtURL.Text;
                    if ( !_edtURL.Text.EndsWith( ".com" ) )
                    {
                        _edtURL.Text = _edtURL.Text + ".com";
                    }
                }
                Core.WebBrowser.NavigateInPlace( _edtURL.Text );
                e.Handled = true;
            }
        }

        public void _edtURL_KeyPress( object sender, KeyPressEventArgs e )
        {
            if ( e.KeyChar == '\r' || e.KeyChar == '\n' )
            {
                e.Handled = true;
            }
        }

        private void URL_ButtonPress( object sender, EventArgs e )
        {
            Core.WebBrowser.NavigateInPlace( _edtURL.Text );
        }
        #endregion

        #region SeeAlsoBar support

        internal delegate void SeeAlsoDelegate( IResource host, IResourceList list, string[] types, int prop );

        public void  ShowSeeAlsoBar( IResourceList resList )
        {
            ShowSeeAlsoBar( resList, false );
        }
        public void  ShowSeeAlsoBar( IResourceList resList, bool needToPrepare )
        {
            string[] resTypes = Core.TabManager.CurrentTab.GetResourceTypes();
            int propId = Core.TabManager.CurrentTab.LinkPropId;
            if( needToPrepare )
            {
                resList = resList.Minus( Core.ResourceStore.FindResourcesWithProp( null, Core.Props.IsDeleted ) );
            }
            // "ShowLinks" also makes the bar visible, if needed.
            //  Generally running also bar as separate UI job must fasten switching
            //  between e.g. Wsps.
            Core.UserInterfaceAP.QueueJob( new SeeAlsoDelegate( _seeAlsoBar.ShowLinks ),
                                           _ownerResource, resList, resTypes, propId );
        }

        /**
         * When a see-also link is clicked, takes the resource currently selected in the
         * default pane, switches to the tab matching the clicked type and selects the resource
         * there. (Note: this is a bit hackish...)
         */

        private void _seeAlsoBar_SeeAlsoLinkClicked( object sender, SeeAlsoEventArgs e )
        {
            string activePaneId = Core.LeftSidebar.ActivePaneId;
            AbstractViewPane activePane = Core.LeftSidebar.GetPane( activePaneId );
            IResource res = activePane.SelectedResource;

            Core.UIManager.BeginUpdateSidebar();
            if ( e.MainWorkspace )
            {
                Core.WorkspaceManager.ActiveWorkspace = null;
            }

            if ( e.TabId == "" )
            {
                Core.TabManager.CurrentTabId = Core.TabManager.Tabs [0].Id;
            }
            else
            {
                Core.TabManager.CurrentTabId = e.TabId;
            }

            activePane = Core.LeftSidebar.GetPane( activePaneId );
            if ( activePane != null )
            {
                Core.LeftSidebar.ActivateViewPane( activePaneId );
            }
            Core.UIManager.EndUpdateSidebar();

            if ( activePane != null )
            {
                activePane.SelectResource( res, false );
            }
            else
            {
                (Core.LeftSidebar as SidebarSwitcher).ActiveSidebar.ForceSelectResource( res );
            }
        }

        #endregion

        private void ResourceBrowser_Enter( object sender, EventArgs e )
        {
            SetCaptionLabelActive();
        }

        private void SetCaptionLabelActive()
        {
            _captionLabel.BackColor = GUIControls.ColorScheme.GetColor( _colorScheme, "PaneCaption.Active",
                                                            SystemColors.ActiveCaption );
            _captionPanel.BackColor = _captionLabel.BackColor;
            _captionLabel.ForeColor = GUIControls.ColorScheme.GetColor( _colorScheme, "PaneCaption.ActiveText",
                                                            SystemColors.ActiveCaptionText );
			_seeAlsoBar.Undercolor = GUIControls.ColorScheme.GetColor( _colorScheme, "PaneCaption.Active",
                                                            SystemColors.ActiveCaption );
            _seeAlsoBar.Active = true;
        }

        private void ResourceBrowser_Leave(object sender, EventArgs e)
        {
            SetCaptionLabelInactive();
        }

        private void SetCaptionLabelInactive()
        {
            _captionLabel.BackColor = ColorScheme.GetColor( _colorScheme, "PaneCaption.Inactive", SystemColors.InactiveCaption );
            _captionPanel.BackColor = _captionLabel.BackColor;
            _captionLabel.ForeColor = ColorScheme.GetColor( _colorScheme, "PaneCaption.InactiveText", SystemColors.InactiveCaptionText );
			_seeAlsoBar.Undercolor = ColorScheme.GetColor( _colorScheme, "PaneCaption.Inactive", SystemColors.ActiveCaption );
            _seeAlsoBar.Active = false;
        }

        private void _captionLabel_Click( object sender, EventArgs e )
        {
            if ( _listView.Visible )
            {
                _listView.Focus();
            }
            else if ( _displayPane != null )
            {
                _displayPane.GetControl().Focus();
            }

        }

        private void _linksBar_LinksPaneExpandChanged( object sender, EventArgs e )
        {
            UpdateLinksPaneVisibility();
        }

        private void UpdateLinksPaneVisibility()
        {
            SuspendLayout();
            if ( !_linksPaneWidthLoaded )
            {
                int linksPaneWidth = Core.SettingStore.ReadInt( "ResourceBrowser", "LinksPaneWidth", _cDefaultLinksPaneWidth );
                _linksPane.Width = (int) (Math.Min( linksPaneWidth, _lowerPane.Width - 30 ) / Core.ScaleFactor.Width);
                _linksPaneWidthLoaded = true;
            }
            bool linksPaneVisible = _linksBar.LinksPaneExpanded && !_linksBar.VerticalViewMode;
            _linksPane.Visible = linksPaneVisible;
            _linksPaneSplitter.Visible = linksPaneVisible;
            if ( linksPaneVisible )
            {
                if ( _dataProvider != null )
                {
                    ILinksPaneFilter filter = null;
                    IResourceList selection = _listView.GetSelectedResources();
                    if ( selection != null && selection.Count == 1 )
                    {
                        if ( selection.ResourceIds [0] == -1 )
                        {
                            selection = null;
                        }
                        else
                        {
                            filter = (ILinksPaneFilter) _linksPaneFilters [selection [0].Type];
                        }
                    }
                    _linksPane.DisplayLinks( selection, filter );
                }
                else if ( _lastDisplayedResource != null )
                {
                    if ( _lastDisplayedResource.IsDeleted )
                    {
                        _linksPane.DisplayLinks( null, null );
                    }
                    else
                    {
                        ILinksPaneFilter filter = (ILinksPaneFilter) _linksPaneFilters [_lastDisplayedResource.Type];
                        _linksPane.DisplayLinks( _lastDisplayedResource.ToResourceList(), filter );
                    }
                }
            }
            ResumeLayout();
        }

        public bool LinksPaneExpanded
        {
            get { return _linksBar.LinksPaneExpanded; }
            set { _linksBar.LinksPaneExpanded = value; }
        }

        public bool ResourceListExpanded
        {
            get { return !_listAndContentSplitter.Collapsed; }
            set { _listAndContentSplitter.Collapsed = !value; _listAndContentSplitter.Visible = value; }
        }

        #region UrlBar Mgmt
        public void RegisterUrlBarActionGroup( string groupId, ListAnchor anchor )
        {
            _urlBarActionManager.RegisterActionGroup( groupId, anchor );
        }

        public void RegisterUrlBarAction( IAction action, string groupId, ListAnchor anchor,
            Icon icon, string text, string toolTip, IActionStateFilter[] filters )
        {
            _urlBarActionManager.RegisterAction( action, groupId, anchor,
                icon, text, toolTip, null, filters );
            UpdateUrlBarSize();
        }

        public void RegisterUrlBarAction( IAction action, string groupId, ListAnchor anchor,
            Image icon, string text, string toolTip, IActionStateFilter[] filters )
        {
            _urlBarActionManager.RegisterAction( action, groupId, anchor,
                icon, text, toolTip, null, filters );
            UpdateUrlBarSize();
        }

        public void UnregisterUrlBarAction( IAction action )
        {
            _urlBarActionManager.UnregisterAction( action );
            UpdateUrlBarSize();
        }

        private void UpdateUrlBarSize()
        {
            _urlBarToolbar.Width = _urlBarActionManager.GetPreferredWidth() + 4;
            _edtURL.Left = _urlBarToolbar.Width;
            _edtURL.Width = _webAddressPanel.Width - _edtURL.Left - 32;
            _btnGoURL.Left = _webAddressPanel.Width - 30;
        }
        #endregion UrlBar Mgmt

        #region Annotations
        public bool  ViewAnnotations
        {
            get{  return _viewAnnotations;   }
            set
            {
                _viewAnnotations = value;
                if( !_viewAnnotations )
                {
                    HideAnnotationForm();
                }
                else
                if( SelectedResources.Count == 1 &&
                    SelectedResources[ 0 ].HasProp( Core.Props.Annotation ))
                {
                    GetAnnotationForm().ShowAnnotation( SelectedResources[ 0 ], true );
                }
            }
        }

        public void EditAnnotation( IResource res )
        {
            GetAnnotationForm().ShowAnnotation( res, _viewAnnotations && !NewspaperVisible, true );
        }

        internal AnnotationForm GetAnnotationForm()
        {
            if ( _annotationForm == null || _annotationForm.IsDisposed )
            {
                _annotationForm = new AnnotationForm();
                _annotationForm.Owner = (Form) Core.MainWindow;
            }
            return _annotationForm;
        }

        internal void HideAnnotationForm()
        {
            if ( _annotationForm != null )
            {
                _annotationForm.Hide();
            }
        }

        internal Rectangle DisplayPanePosition
        {
            get { return _lowerPane.RectangleToScreen( _bodyPane.Bounds ); }
        }

        #endregion

        private void _bodyPane_Enter( object sender, EventArgs e )
        {
            _bodyPaneFocused = true;
            _bodyPane.Invalidate();
        }

        private void _bodyPane_Leave( object sender, EventArgs e )
        {
            _bodyPaneFocused = false;
            _bodyPane.Invalidate();
        }

        /**
         * Draws the background of the toolbar panel and the web address panel.
         */

        private void _webAddressPanel_Paint( object sender, PaintEventArgs e )
        {
            if ( _webAddressPanel.Width > 0 && _webAddressPanel.Height > 0 )
            {
                Rectangle rc = _webAddressPanel.ClientRectangle;
                GradientRenderer.Paint( e.Graphics, rc, Color.White, SystemColors.ControlDark, LinearGradientMode.Vertical );
                Pen borderPen = ColorScheme.GetPen( _colorScheme, "PaneCaption.Border", SystemPens.Control );
                e.Graphics.DrawLine( borderPen, rc.Right-1, 0, rc.Right-1, rc.Bottom-1 );
            }
        }

        public void RegisterLinksPaneFilter( string resourceType, ILinksPaneFilter filter )
        {
            #region Preconditions
            if ( !Core.ResourceStore.ResourceTypes.Exist( resourceType ) )
            {
                throw new ArgumentException( "Invalid resource type '" + resourceType + "'", "resourceType" );
            }
            #endregion Preconditions

            ILinksPaneFilter oldFilter = (ILinksPaneFilter) _linksPaneFilters [resourceType];
            if ( oldFilter != null )
            {
                CompositeLinksPaneFilter compositeFilter = oldFilter as CompositeLinksPaneFilter;
                if ( compositeFilter != null )
                {
                    compositeFilter.AddBaseFilter( filter );
                }
                else
                {
                    _linksPaneFilters [resourceType] = new CompositeLinksPaneFilter( oldFilter, filter );
                }
            }
            else
            {
                _linksPaneFilters [resourceType] = filter;
            }
        }

        public void RegisterLinksGroup( string groupId, int[] propTypes, ListAnchor anchor )
        {
            LinksPaneBase.RegisterLinksGroup( groupId, propTypes, anchor );
        }

        private void _captionLabel_TextChanged(object sender, EventArgs e)
        {
            _captionLabel.Width = _captionLabel.PreferredWidth + 12;
        }

        protected void _captionLabel_SizeChanged(object sender, EventArgs e)
        {
        	_captionPanel.PerformLayout((Control) sender, "Size");
        }

    	public bool ResourceListFocused
        {
            get { return _listView.ContainsFocus; }
        }

        public void SelectAll()
        {
            _listView.SelectAll();
        }

        public void HookFormattingRulesChange()
        {
            _listView.HookFormattingRulesChange();
        }

        public AutoPreviewMode AutoPreviewMode
        {
            get { return _perTabBrowserSettings.CurTabAutoPreviewMode; }
            set
            {
                _perTabBrowserSettings.CurTabAutoPreviewMode = value;
                UpdateAutoPreviewColumn();
            }
        }

        private void UpdateAutoPreviewColumn()
        {
            if ( _highlightProvider != null && !_suppressContexts &&
                Core.SettingStore.ReadBool( "Resources", "ShowSearchContext", true ) )
            {
                _contextPreviewColumn.SetHighlightDataProvider( _highlightProvider );
                _listView.JetListView.AutoPreviewColumn = _contextPreviewColumn;
            }
            else
            {
                AutoPreviewMode mode = _perTabBrowserSettings.CurTabAutoPreviewMode;
                if ( mode != AutoPreviewMode.Off )
                {
                    _autoPreviewColumn.AutoPreviewMode = mode;
                    _listView.JetListView.AutoPreviewColumn = _autoPreviewColumn;
                }
                else
                {
                    _listView.JetListView.AutoPreviewColumn = null;
                }
            }
            UpdateRowDelimiters();
        }

        /// <summary>
        /// Shows or hides row delimiters depending on the state of the view.
        /// </summary>
        private void UpdateRowDelimiters()
        {
            _listView.RowDelimiters = _listView.MultiLineView || _listView.JetListView.AutoPreviewColumn != null;
        }

        /// <summary>
        /// Gets or sets the value indicating whether the list view contents is drawn in
        /// multiline mode.
        /// </summary>
        public bool VerticalLayout
        {
            get { return _verticalLayout; }
            set
            {
                if ( _verticalLayout != value )
                {
                    _verticalLayout = value;

                    BrowserPanesMode = BrowserPanesVisibilityMode.Both;

                    if ( _listState != null )
                    {
                        ShowListViewColumns( _listState.Columns );
                    }
                    if ( _verticalLayout )
                    {
                        _listView.ColumnSchemeProvider = new ResourceColumnSchemeProvider( _columnManager, _listView );
                    }
                    _listView.MultiLineView = _verticalLayout;
                    UpdateRowDelimiters();
                    _listViewPanel.SuspendLayout();
                    if ( _listView.MultiLineView )
                    {
                        _listViewHeight = _listView.Height;
                        _listView.Dock = DockStyle.Left;
                        _listAndContentSplitter.Dock = DockStyle.Left;
                        _listView.Width = _listViewWidth;
                        _lowerPane.BackColor = SystemColors.Window;
                    }
                    else
                    {
                        _listViewWidth = _listView.Width;
                        _listView.Dock = DockStyle.Top;
                        _listAndContentSplitter.Dock = DockStyle.Top;
                        _listView.Height = _listViewHeight;
                        _lowerPane.BackColor = SystemColors.Control;
                        _listViewPanel.DockPadding.All = 0;
                        LinksPaneExpanded = false;
                        foreach( JetListViewColumn col in _listView.Columns )
                        {
                            if ( col is TreeStructureColumn )
                            {
                                (col as TreeStructureColumn ).Indent = col.Width;
                            }
                        }
                    }
                    _linksBar.VerticalViewMode = _listView.MultiLineView;
                    _listViewPanel.ResumeLayout();
                    UpdateLinksPaneVisibility();
                    _bodyPane.Invalidate();
                    _perTabBrowserSettings.VerticalLayout = value;
                }
            }
        }

        private static bool IsJLVNodeVisible( IResource res, ResourceListView2 listView )
        {
            bool isVisible = false;
            if( res != null )
            {
                JetListViewNode node = listView.JetListView.NodeCollection.NodeFromItem( res );
                isVisible = (node != null) && JetListView.IsNodeVisible( node );
            }
            return isVisible;
        }

        internal void UpdatePerTabSettings()
        {
            VerticalLayout = _perTabBrowserSettings.VerticalLayout;
            UpdateAutoPreviewColumn();
        }

        private void DoShowNewspaper( IResource ownerResource, IResourceList resources,
                                      ResourceListDisplayOptions options )
        {
            _lastDisplayedType = null;
            ColumnDescriptor[] columns = CheckGetDefaultColumns( options.Columns, resources );
            ResourceListState state = _columnManager.GetListViewState( ownerResource, resources, columns,
                options.DefaultGroupItems );
            if ( state.SortSettings != null )
            {
                resources.Sort( state.SortSettings );
            }

            SuspendLayout();
            _listViewPanel.Visible = false;
            _toolBarPanel.Visible = true;
            if ( _newspaperViewer == null )
            {
                _newspaperViewer = new NewspaperViewer();
                _newspaperViewer.NavigateAway += HandleNewspaperNavigateAway;
                _newspaperViewer.ItemsInViewCountChanged += HandleNewspaperCountChanged;
                _newspaperViewer.JumpOut += HandleNewspaperJumpOut;
                _newspaperViewer.Dock = DockStyle.Fill;
                _newspaperViewer.ContextProvider = this;
                Controls.Add( _newspaperViewer );
            }
            Controls.SetChildIndex( _newspaperViewer, 0 );
            ResumeLayout();

            _ownerResource = ownerResource;
            _caption = options.Caption;
            _captionTemplate = options.CaptionTemplate;
            if ( _captionTemplate != null && ownerResource != null )
            {
                _ownerResourceList = ownerResource.ToResourceListLive();
                _ownerResourceList.ResourceChanged += HandleOwnerResourceChanged;
            }
            UpdateCaption();
            _newspaperViewer.Visible = true;
            _newspaperViewer.ShowNewspaper( ownerResource, resources, options );

            if ( options.SeeAlsoBar )
            {
                ShowSeeAlsoBar( _origResourceList );
            }
        }

        private void HandleNewspaperCountChanged( object sender, EventArgs e )
        {
            UpdateCaption();
        }

        private void HandleNewspaperNavigateAway( object sender, NewspaperViewer.NavigateAwayEventArgs args )
        {
            SuspendLayout();
            try
            {
                HideNewspaper();
                WebPageMode = true;

                AbstractBrowseState browseState = _browseStack.Peek( 0 );
                _browseStack.Push( browseState );

                _listViewPanel.Visible = true;
                _lowerPane.Visible = true;
                Controls.SetChildIndex( _listViewPanel, 0 );
                ShowDisplayPaneControl( Core.WebBrowser );
                Core.WebBrowser.Navigate( args.Uri );
            }
            finally
            {
                ResumeLayout();
            }
        }

        private static void HandleNewspaperJumpOut( object sender, EventArgs e )
        {
            VerticalSidebar sidebar = (Core.LeftSidebar as SidebarSwitcher).ActiveSidebar;
            if ( sidebar != null )
            {
                sidebar.FocusActivePane();
            }
        }

        public bool GroupItems
        {
            get { return _groupItems; }
            set
            {
                if ( _groupItems != value )
                {
                    _groupItems = value;
                    if ( _listState != null )
                    {
                        ShowListViewColumns( _listState.Columns );
                        if ( _dataProvider != null )
                        {
                            _dataProvider.UpdateSortColumn();
                        }
                        CheckSaveListState( true );
                    }
                }
            }
        }

        public void SetAllGroupsExpanded( bool expanded )
        {
            _listView.JetListView.SetAllGroupsExpanded( expanded );
        }

        public void SetAllThreadsExpanded( bool expanded )
        {
            _listView.JetListView.SetAllThreadsExpanded( expanded );
        }

        public bool IsThreaded
        {
            get { return _dataProvider is ConversationDataProvider; }
        }

        /// <summary>
		/// Repositions the see-also bar according to the parent panel and caption label size.
		/// It's located on the caption panel, to the right of the caption label, and occupies the free space.
		/// Gaps:
		/// <code>
		///
		///                             1px upper border (borders handled by the underlying control)
		///                                                          |
		/// [caption] — 12px gap — [#######################SEE#ALSO#BAR###############################] — 1px border
		///                                                          |
		///                                                     3px spacing
		///                                                          |
		///                                                   1px lower border
		/// </code>
		/// </summary>
		private void OnLayoutCaptionPanel( object sender, LayoutEventArgs e )
    	{
			_seeAlsoBar.Location = new Point(_captionLabel.Right + 12, 1);	// 1 is a spacing for upper border
			_seeAlsoBar.Size = new Size(_captionPanel.Width - 1 - _seeAlsoBar.Left, _captionPanel.Height - 1 - 1 - 3);	// 1s for borders, 3 for spacing below the seealsobar
		}

        internal void PopFocus()
        {
            if ( _displayPane != null && _displayPane.GetControl().ContainsFocus )
            {
                FocusResourceList();
            }
            else if ( _listView.ContainsFocus )
            {
                VerticalSidebar sidebar = (Core.LeftSidebar as SidebarSwitcher).ActiveSidebar;
                if ( sidebar != null )
                {
                    sidebar.FocusActivePane();
                }
            }
        }

        public IResource GetResourceAbove( IResource res )
        {
            return GetResourceRelative( res, false );
        }

        public IResource GetResourceBelow( IResource res )
        {
            return GetResourceRelative( res, true );
        }

        private IResource GetResourceRelative( IResource res, bool forward )
        {
            JetListViewNode node = _listView.JetListView.NodeCollection.NodeFromItem( res );
            if ( node == null )
            {
                return null;
            }
            IEnumerator enumerator;
            if ( forward )
            {
                enumerator = _listView.JetListView.NodeCollection.EnumerateNodesForward( node );
            }
            else
            {
                enumerator = _listView.JetListView.NodeCollection.EnumerateNodesBackward( node );
            }
            enumerator.MoveNext();
            if ( !enumerator.MoveNext() )
            {
                return null;
            }
            JetListViewNode lvNode = (JetListViewNode) enumerator.Current;
            return (IResource) lvNode.Data;
        }

        public void SetDefaultViewSettings( string tabId, AutoPreviewMode autoPreviewMode, bool verticalLayout )
        {
            _perTabBrowserSettings.SetDefaultsForTab( tabId, autoPreviewMode, verticalLayout );
        }

        /// <summary>
        /// Retrieve a list of column descriptors for visible columns corresponding
        /// the current resource list.
        /// </summary>
        /// <returns>An array of ColumnDescriptor objects describing displayed
        /// columns for actual resource list.</returns>
        public ColumnDescriptor[] GetDisplayedColumns()
        {
            return ((DisplayColumnManager)Core.DisplayColumnManager).ColumnDescriptorsFromList( _listView );
        }

        #region Paint
        private void _bodyPane_OnPaint( object sender, PaintEventArgs e )
        {
            if ( _bodyPaneFocused )
            {
                using( Pen borderPen = new Pen( Color.FromArgb( 135, 131, 164 ) ) )
                {
                    e.Graphics.DrawRectangle( borderPen,
                        new Rectangle( 3, 3, _bodyPane.ClientRectangle.Width - 7, _bodyPane.ClientRectangle.Height - 7 ) );
                    e.Graphics.DrawRectangle( borderPen,
                        new Rectangle( 2, 2, _bodyPane.ClientRectangle.Width - 5, _bodyPane.ClientRectangle.Height - 5 ) );
                    e.Graphics.DrawRectangle( borderPen,
                        new Rectangle( 1, 1, _bodyPane.ClientRectangle.Width - 3, _bodyPane.ClientRectangle.Height - 3 ) );
                    e.Graphics.DrawRectangle( borderPen,
                        new Rectangle( 0, 0, _bodyPane.ClientRectangle.Width - 1, _bodyPane.ClientRectangle.Height - 1 ) );
                    e.Graphics.DrawLine( borderPen, 4, 4, _bodyPane.ClientRectangle.Width - 4, 4 );
                }
            }
        }

        private void HandleCaptionPanelPaint( object sender, PaintEventArgs e )
        {
            Pen borderPen = ColorScheme.GetPen( _colorScheme, "PaneCaption.Border", SystemPens.Control );
            e.Graphics.DrawRectangle( borderPen, 0, 0,
                _captionPanel.ClientRectangle.Width - 1, _captionPanel.ClientRectangle.Height - 1 );
        }

        private void HandleStatusLineLabelPaint( object sender, PaintEventArgs e )
        {
            Pen borderPen = ColorScheme.GetPen( _colorScheme, "PaneCaption.Border", SystemPens.Control );
            e.Graphics.DrawLine( borderPen, 0, 0, 0, _statusLineLabel.Height - 1 );
            e.Graphics.DrawLine( borderPen, _statusLineLabel.Width - 1, 0, _statusLineLabel.Width - 1,_statusLineLabel.Height - 1 );
        }

        [DefaultValue(null)]
        public ColorScheme ColorScheme
        {
            get { return _colorScheme; }
            set
            {
                _colorScheme = value;
                _linksBar.ColorScheme = value;
                _linksPane.ColorScheme = value;
                _seeAlsoBar.ColorScheme = value;
                //_linksPaneSplitter.ColorScheme = value;
                _listAndContentSplitter.ColorScheme = value;

                SetToolbarColors( _toolBar );
                SetToolbarColors( _urlBarToolbar );

                Color borderColor = ColorScheme.GetColor( _colorScheme, "PaneCaption.Border", Color.Black );
                _lowerPaneBackground.BorderColor = borderColor;
                _listView.BorderColor = borderColor;
                _listView.GroupHeaderColor = ColorScheme.GetColor( _colorScheme, "ResourceList.GroupHeader", SystemColors.Control );

                if ( ContainsFocus )
                    SetCaptionLabelActive();
                else
                    SetCaptionLabelInactive();
            }
        }

        private void SetToolbarColors( ToolStrip toolBar )
        {
//            Color start = ColorScheme.GetStartColor( _colorScheme, "Toolbar.Background", Color.White );
//            Color end = ColorScheme.GetEndColor( _colorScheme, "Toolbar.Background", SystemColors.ControlDark );
//            toolBar.Renderer = new ToolBarRenderer( _colorScheme, start, end );
//            toolBar.Renderer = new GradientRenderer( /*_colorScheme, */start, end );
        }
        #endregion Paint

        private class ToolBarRenderer : GradientRenderer
        {
            private readonly ColorScheme _colorScheme;

            public ToolBarRenderer( ColorScheme scheme, Color startColor, Color endColor )
                : base( startColor, endColor )
            {
                _colorScheme = scheme;
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                base.OnRenderToolStripBackground( e );
//                Pen borderPen = ColorScheme.GetPen( _colorScheme, "PaneCaption.Border", SystemPens.Control );
                Pen borderPen = ColorScheme.GetPen( _colorScheme, "PaneCaption.Border", SystemPens.Control );
//                Color color = ColorScheme.GetColor( _colorScheme, "PaneCaption.Border", Color.Black );
                borderPen = Pens.Black;
                e.Graphics.DrawLine( borderPen, 0, 0, e.AffectedBounds.Right - 2, e.AffectedBounds.Bottom - 1 );

                e.Graphics.DrawLine( borderPen, 0, 0, 0, e.AffectedBounds.Bottom - 1 );
                e.Graphics.DrawLine( borderPen, 1, 0, 1, e.AffectedBounds.Bottom - 1 );
                e.Graphics.DrawLine( borderPen, e.AffectedBounds.Right - 2, 0, e.AffectedBounds.Right - 2, e.AffectedBounds.Bottom - 1 );
            }
        }
    }
}
