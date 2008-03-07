/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Web;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Charsets;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.MIME;
using JetBrains.Omea.Net;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Components.Separator;

namespace JetBrains.Omea.Nntp
{
	public class EditMessageForm: DialogBase, IContextProvider, ICommandProcessor
	{
	    private const string _DefaultCharset = "utf-8";
        private const string _CommonMessage = "This message cannot be encoded with the charset you have selected, “{0}”. Some of the characters will be lost.\n";

		private JetTextBox _subjectBox;
		private IContainer components;
		private Label _subjectLabel;
		private StatusBar _statusBar;
		private OpenFileDialog _openAttachDialog;
		private ToolTip _fullNameToolTip;
		private Panel _headerPanel;
		private Label _fromlabel;
		private Splitter _bodyListSplitter;
		private Panel _bodyPanel;
		private ContextMenu _attachListMenu;
		private MenuItem _removeAttachMenuItem;
		private GradientToolbar _toolbar;
		private ResourceListLinkLabel _groupsLabel;
		private JetTextBox _fromTextLabel;
		private ComboBox _fromComboBox;
		private Label _serverLabel;
		private ResourceComboBox _serverComboBox;

		private ResourceListView2 _attachList;
        private ResourceListView2Column nameCol, sizeCol;
        private static IResourceList _attaches = Core.ResourceStore.EmptyResourceList;

		private AbstractWebBrowser _browser;

        #region Menu
        private MenuStrip mainMenu1;
		private ToolStripMenuItem _fileMenuItem;
		private ToolStripMenuItem _newMenuItem;
		private ToolStripSeparator _menuSeparator1;
		private ToolStripMenuItem _closeMenuItem;
		private ToolStripMenuItem _formatMenuItem;
		private ToolStripMenuItem _saveMenuItem;
		private ToolStripSeparator _menuSeparator4;
		private ToolStripMenuItem _encodingMenuItem;
		private ToolStripMenuItem _postMenuItem;
		private ToolStripMenuItem _undoMenuItem;
		private ToolStripMenuItem _redoMenuItem;
		private ToolStripSeparator _menuSeparator5;
		private ToolStripMenuItem _cutMenuItem;
		private ToolStripMenuItem _copyMenuItem;
		private ToolStripMenuItem _pasteMenuItem;
		private ToolStripMenuItem _editMenuItem;
		private ToolStripMenuItem _saveAndCloseMenuItem;
		private ToolStripMenuItem _menuitemReply2Sender;
		private ToolStripMenuItem _menuitemReply2Herd;
		private ToolStripMenuItem _menuitemForward;
		private ToolStripMenuItem _menuitemDeleteArticle;
		private ToolStripMenuItem _menuitemNewsgroups;
		private ToolStripMenuItem _menuitemAttachFile;
		private ToolStripMenuItem _menuitemNext;
		private ToolStripMenuItem _menuitemPrevious;
		private ToolStripMenuItem _menuActions;
        #endregion Menu

        private JetLinkLabel _newsgroupsLabel;

		private IResourceList _groups;
        private string _references;
        private bool _articleChanged;
        private Separator _toolbarSeparator;
        private IResource _draftArticle;
        private IResourceList _liveArticle;
        private readonly MethodInvoker _saveArticleDelegate;
        private static readonly IntHashTable _openArticles = new IntHashTable();

		/// <summary>
		/// <c>True</c> while the form is busy (eg sending the article) and cannot execute most of the actions.
		/// </summary>
		protected bool _isBusy = false;

		/// <summary>
		/// Binds toolbar controls to particular actions; also populates toolbar with buttons.
		/// </summary>
		protected ToolbarActionManager	_toolbarActionManager;

		/// <summary>
		/// Security Context in which the HTML bodies are displayed, derived from the <see cref="WebSecurityContext.Restricted"/>, forces links to be opened in a new window.
		/// </summary>
		protected WebSecurityContext _browserContext;

		/// <summary>
		/// A map of keyboard shortcuts to the corresponding actions.
		/// </summary>
		protected IntHashTable	_hashKeyboardActions;

		/// <summary>
		/// Identifier of the HTML TEXTAREA that servers as a text editor control.
		/// </summary>
		protected readonly static string _sPlainTextEditorId = "PlainTextEditor";

		/// <summary>
		/// Identifies the role in which the current EditMessageForm performs.
		/// </summary>
        private enum FormStateVerb
        {
			/// <summary>
			/// The form is editing a news article.
			/// </summary>
            Edit,

			/// <summary>
			/// The form is viewing a news article in read-only mode.
			/// </summary>
            View
        }

        private FormStateVerb _whatWeAreDoing;

		/// <summary>
		/// Actions manager for the menu items.
		/// </summary>
		protected MenuActionsManager _menuactionsmanager;

		/// <summary>
		/// The charset that is currently selected for the article.
		/// Initialized by the default charset of the news server, or Omea's default charset if not overridden.
		/// </summary>
		protected CharsetsEnum.Charset _charset = CharsetsEnum.GetDefaultBodyCharset();

		/// <summary>
		/// If <c>True</c>, warnings are not displayed in case the selected charset cannot handle all the characters in the message correctly.
		/// Used to suppress repetitive messages for the same article on every save, if the user choses to ignore the problem.
		/// </summary>
		protected bool _bSuppressUnfitEncodingWarning = false;

		private EditMessageForm()
		{
			InitializeComponent();
            Icon = Core.UIManager.ApplicationIcon;
            _attaches = Core.ResourceStore.EmptyResourceList;
            _fromTextLabel.Width = _fromComboBox.Width - SystemInformation.VerticalScrollBarWidth - 1;

            _newsgroupsLabel.Text = "Newsgroups:";

            _saveArticleDelegate = SaveArticle;
            Core.UIManager.MainWindowClosing += EditMessageForm_Closing;

            IResourceList servers = Core.ResourceStore.GetAllResources( NntpPlugin._newsServer );

            FillFromCombo( servers );
            FillServersCombo( servers );

            FillEncodingMenu();

			_attachList.ShowContextMenu = false;

			// Create and initialize the browser object
            _bodyPanel.Controls.Remove( _bodyListSplitter );
            _bodyPanel.Controls.Remove( _attachList );

            InitializeBrowserControl();

            _bodyPanel.Controls.Add( _browser );
            _bodyPanel.Controls.Add( _bodyListSplitter );
            _bodyPanel.Controls.Add( _attachList );

            InitializeMenuToolBarManagers();

            InitializeColumns();

            InitializeFormSize();
		}

        private void FillFromCombo( IResourceList servers )
        {
            _fromComboBox.BeginUpdate();
            try
            {
                foreach( IResource server in servers.ValidResources )
                {
                    string displayName = server.GetPropText( NntpPlugin._propUserDisplayName );
                    string email = server.GetPropText( NntpPlugin._propEmailAddress );
                    if( email.Length > 0 )
                    {
                        if( displayName.Length > 0 )
                        {
                            email = displayName + " <" + email + '>';
                        }
                        if( _fromComboBox.Items.IndexOf( email ) < 0 )
                        {
                            _fromComboBox.Items.Add( email );
                        }
                    }
                }
            }
            finally
            {
                _fromComboBox.EndUpdate();
            }
        }

        private void FillServersCombo( IResourceList servers )
        {
            _serverComboBox.BeginUpdate();
            try
            {
                ComboBox.ObjectCollection items = _serverComboBox.Items;
                items.Clear();
                foreach( IResource server in servers.ValidResources )
                {
                    items.Add( server );
                }
                if( items.Count > 0 )
                {
                    _serverComboBox.SelectedItem = servers[ 0 ];
                }
            }
            finally
            {
                _serverComboBox.EndUpdate();
            }
        }

        private void InitializeBrowserControl()
        {
			_browser = Core.WebBrowser.NewInstance();
			_browser.Dock = DockStyle.Fill;
			_browser.Name = "_browser";
			_browser.TabIndex = 1;
			_browser.Visible = true;
			_browser.KeyDown += _browser_KeyDown;
            _browser.AllowDrop = false;
            _browser.DragOver += EditMessageForm_DragOver;
            _browser.DragDrop += EditMessageForm_DragDrop;
			_browser.ContextProvider = this;	// Selected resource information and so on
			_browser.BeforeNavigate += OnBeforeBrowserNavigate;

			_browserContext = WebSecurityContext.Restricted;
			_browserContext.AllowInPlaceNavigation = false;
        }

        private void FillEncodingMenu()
        {
			//////////////////
			// Encoding Menu
			// Get the list of available charsets
			ArrayList charsets = new ArrayList();
			foreach( CharsetsEnum.Charset charset in new CharsetsEnum( CharsetFlags.NntpCharset ) )
				charsets.Add( charset );

			// Sort by family, then by description
			charsets.Sort();

			// Find out where to put the separators
			// (between two families, at least one of whom has more than one member)
			int nFamilyStart = 0;
			int nCurrentFamily = -1;
			IntHashSet hashSeparators = new IntHashSet(); // Indexes of charsets in the array BEFORE which separators should be inserted
			for( int a = 0; a < charsets.Count; a++ )
			{
				CharsetsEnum.Charset charset = (CharsetsEnum.Charset)charsets[ a ];
				// Family ends here?
				if( charset.FamilyCodepage != nCurrentFamily )
				{
					// If it was a large enough family, surround it with separators
					if( a - nFamilyStart > 1 ) // Number of charsets in the family
					{
						if( nFamilyStart > 0 )
							hashSeparators.Add( nFamilyStart );
						hashSeparators.Add( a );
					}

					// Seed the new family
					nCurrentFamily = charset.FamilyCodepage;
					nFamilyStart = a;
				}
			}

			// Add to the menu
			for( int a = 0; a < charsets.Count; a++ )
			{
				// Add an optional separator
				if( hashSeparators.Contains( a ) )
					_encodingMenuItem.DropDownItems.Add( new ToolStripSeparator() );

				// Add the charset menu item
				CharsetsEnum.Charset charset = (CharsetsEnum.Charset)charsets[ a ];
				ToolStripMenuItem mi = new EncodingMenuItem( charset );
				mi.Click += OnEncodingMenuItemClicked;
				_encodingMenuItem.DropDownItems.Add( mi );
			}
        }

        private void InitializeMenuToolBarManagers()
        {
			/////////////////////////////////////////
			// Create some actions for later use
			IAction	actionSave = new MethodInvokerAction( OnSaveArticle, OnUpdateSaveArticle );
			IAction	actionSaveAndClose = new MethodInvokerAction( OnSaveAndClose, OnUpdateSaveArticle );
			IAction actionSend = new MethodInvokerAction( DoPost, OnUpdateDoPost );
			IAction actionClose = new MethodInvokerAction( AsyncClose, null );
			IAction actionNewsgroups = new MethodInvokerAction( SelectNewsgroups, OnUpdateSelectNewsgroups );
			IAction actionAttachFile = new MethodInvokerAction( InsertAttachment, OnUpdateInsertAttachment );
			IAction actionUndo = new CommandProcessorAction( "Undo" );
			IAction actionRedo = new CommandProcessorAction( "Redo" );
			IAction actionCut = new CommandProcessorAction( DisplayPaneCommands.Cut );
			IAction actionCopy = new CommandProcessorAction( DisplayPaneCommands.Copy );
			IAction actionPaste = new CommandProcessorAction( DisplayPaneCommands.Paste );
			IAction	actionReply2Sender = new Reply2Sender(this);
			IAction	actionReplyToHerd = new ReplyAction(this);
			IAction	actionForward = new ForwardArticle( this );
			IAction	actionDeleteArticle = new MethodInvokerAction(DeleteArticle, OnUpdateDelete);
			IAction actionNextArticle = new MethodInvokerAction(DisplayNextArticle, OnUpdateDisplayNextArticle);
			IAction actionPreviousArticle = new MethodInvokerAction(DisplayPreviousArticle, OnUpdateDisplayPreviousArticle);

			///////////////////////////////
			// Initialize the toolbar and populate it with buttons
			_toolbarActionManager = new ToolbarActionManager(_toolbar);
			_toolbarActionManager.ContextProvider = this;

			string	sGroup;

			///////////////////
			sGroup = "File";
			_toolbarActionManager.RegisterActionGroup( sGroup, ListAnchor.Last );
			_toolbarActionManager.RegisterAction(actionSend, sGroup, ListAnchor.Last, LoadIcon("Send"), "&Send", "Send Article", null, null );
			_toolbarActionManager.RegisterAction(actionSave, sGroup, ListAnchor.Last, LoadIcon("Save"), "Save", "Save Article in Drafts", null, null );

			///////////////////
			sGroup = "Edit";
			_toolbarActionManager.RegisterActionGroup( sGroup, ListAnchor.Last );

			_toolbarActionManager.RegisterAction(actionCut,sGroup, ListAnchor.Last, LoadIcon("Cut"), "Cut", null, null, null );
			_toolbarActionManager.RegisterAction(actionCopy,sGroup, ListAnchor.Last, LoadIcon("Copy"), "Copy", null, null, null );
			_toolbarActionManager.RegisterAction(actionPaste,sGroup, ListAnchor.Last, LoadIcon("Paste"), "Paste", null, null, null );

			///////////////////
			sGroup = "Actions";
			_toolbarActionManager.RegisterActionGroup( sGroup, ListAnchor.Last );

			_toolbarActionManager.RegisterAction( actionReplyToHerd, sGroup, ListAnchor.Last, LoadIcon("ReplyToHerd"), "Reply", null, null, null );
			_toolbarActionManager.RegisterAction( actionReply2Sender, sGroup, ListAnchor.Last, LoadIcon("Reply"), "Reply to Sender", null, null, null );
			_toolbarActionManager.RegisterAction( actionForward, sGroup, ListAnchor.Last, LoadIcon("MailForward"), "Forward", "Forward Article", null, null );

			_toolbarActionManager.RegisterAction( actionDeleteArticle, sGroup, ListAnchor.Last, LoadIcon("Delete"), null, "Delete", "Delete Article", null );
			_toolbarActionManager.RegisterAction(actionAttachFile, sGroup, ListAnchor.Last, LoadIcon("Attach"), "&Insert Attachment…", "Insert Attachment", null, null );
			_toolbarActionManager.RegisterAction(actionNewsgroups, sGroup, ListAnchor.Last, LoadIcon("EditMessageForm.PickNewsgroups"), "Newsgroups…", "Select Newsgroups", null, null );

			///////////////////
			sGroup = "View";
			_toolbarActionManager.RegisterActionGroup( sGroup, ListAnchor.Last );

			_toolbarActionManager.RegisterAction( actionNextArticle, sGroup, ListAnchor.Last, LoadIcon("Next"), null, "Next Article", null, null );
			_toolbarActionManager.RegisterAction(actionPreviousArticle, sGroup, ListAnchor.Last, LoadIcon("Previous"), null, "Previous Article", null, null );
			/////////

			////////////////////////////////////
			// Register the Menu Items actions
			_menuactionsmanager = new MenuActionsManager( this, mainMenu1 );

            // File
			_menuactionsmanager.Add( _postMenuItem, actionSend );
			_menuactionsmanager.Add( _saveMenuItem, actionSave );
			_menuactionsmanager.Add( _saveAndCloseMenuItem, actionSaveAndClose );
			// Edit
			_menuactionsmanager.Add( _undoMenuItem, actionUndo );
			_menuactionsmanager.Add( _redoMenuItem, actionRedo );
			_menuactionsmanager.Add( _cutMenuItem, actionCut );
			_menuactionsmanager.Add( _copyMenuItem, actionCopy );
			_menuactionsmanager.Add( _pasteMenuItem, actionPaste );
			_menuactionsmanager.Add( _menuitemNewsgroups, actionNewsgroups );
			_menuactionsmanager.Add( _menuitemAttachFile, actionAttachFile );
			// Actions
			_menuactionsmanager.Add( _menuitemReply2Sender, actionReply2Sender );
			_menuactionsmanager.Add( _menuitemReply2Herd, actionReplyToHerd );
			_menuactionsmanager.Add( _menuitemForward, actionForward );
			_menuactionsmanager.Add( _menuitemDeleteArticle, actionDeleteArticle );
			_menuactionsmanager.Add( _menuitemNext, actionNextArticle );
			_menuactionsmanager.Add( _menuitemPrevious, actionPreviousArticle );

			////////////////////////////////////////
			// Initialize the keyboard actions
			_hashKeyboardActions = new IntHashTable();
			_hashKeyboardActions[(int) (Keys.Control | Keys.Enter)] = actionSend;
			_hashKeyboardActions[(int) (Keys.Alt | Keys.S)] = actionSend;
			_hashKeyboardActions[(int) Keys.Escape] = actionClose;
			_hashKeyboardActions[(int) (Keys.Control | Keys.S)] = actionSave;
			_hashKeyboardActions[(int) (Keys.Alt | Keys.I)] = actionAttachFile;
        }

        private void  InitializeColumns()
        {
            _attachList.AllowColumnReorder = false;
            _attachList.Columns.Add( new ResourceIconColumn() );
            nameCol = _attachList.AddColumn( Core.Props.Name );
            sizeCol = _attachList.AddColumn( Core.Props.Size );
            nameCol.ShowHeader = true;
//            nameCol.FixedSize = false;
            nameCol.AutoSize = true;
            nameCol.Width = 100;
            nameCol.Text = "Name";
            sizeCol.ShowHeader = sizeCol.FixedSize = true;
            sizeCol.Width = 56;
            sizeCol.Text = "Size";
        }

        private void InitializeFormSize()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Width = ( bounds.Width * 3 ) >> 2;
            Height = ( bounds.Height * 3 ) >> 2;
            Left = bounds.Width >> 3;
            Top = bounds.Height >> 3;
        }

		private static void OnBeforeBrowserNavigate(object sender, BeforeNavigateEventArgs args)
		{
			args.Cancel = true;	// Prevent from the unwanted navigation
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
                Core.UIManager.MainWindowClosing -= EditMessageForm_Closing;
				_toolbarActionManager.Dispose();
				_encodingMenuItem.Dispose();
/*
				foreach(MenuItem mi in _encodingMenuItem.MenuItems)	// TODO: I think this can freely be removed as it is not needed after the wire-up scheme has been changed
					mi.Click -= OnEncodingMenuItemClicked;
*/
				if(components != null)
				{
					components.Dispose();
				}

				if(_menuactionsmanager != null)
				{
					_menuactionsmanager.Dispose();
					_menuactionsmanager = null;
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		private void InitializeComponent()
		{
			this.components = new Container();
			ResourceManager resources = new ResourceManager( typeof(EditMessageForm) );
			_subjectBox = new JetTextBox();
			_groupsLabel = new ResourceListLinkLabel();
			_fromlabel = new Label();
			_subjectLabel = new Label();
			_statusBar = new StatusBar();
			_openAttachDialog = new OpenFileDialog();
			_attachList = new ResourceListView2();
			_attachListMenu = new ContextMenu();
			_removeAttachMenuItem = new MenuItem();
			_fullNameToolTip = new ToolTip( this.components );
			_headerPanel = new Panel();
			_newsgroupsLabel = new JetLinkLabel();
			_serverComboBox = new ResourceComboBox();
			_serverLabel = new Label();
			_toolbarSeparator = new Separator();
			_fromTextLabel = new JetTextBox();
			_fromComboBox = new ComboBox();
			_bodyListSplitter = new Splitter();
			_bodyPanel = new Panel();
			_toolbar = new GradientToolbar();

			mainMenu1 = new MenuStrip();
			_fileMenuItem = new ToolStripMenuItem();
			_newMenuItem = new ToolStripMenuItem();
			_postMenuItem = new ToolStripMenuItem();
			_menuSeparator1 = new ToolStripSeparator();
			_saveMenuItem = new ToolStripMenuItem();
			_saveAndCloseMenuItem = new ToolStripMenuItem();
			_menuSeparator4 = new ToolStripSeparator();
			_closeMenuItem = new ToolStripMenuItem();
			_editMenuItem = new ToolStripMenuItem();
			_undoMenuItem = new ToolStripMenuItem();
			_redoMenuItem = new ToolStripMenuItem();
			_menuSeparator5 = new ToolStripSeparator();
			_cutMenuItem = new ToolStripMenuItem();
			_copyMenuItem = new ToolStripMenuItem();
			_pasteMenuItem = new ToolStripMenuItem();
			_formatMenuItem = new ToolStripMenuItem();
			_encodingMenuItem = new ToolStripMenuItem();
			_menuitemReply2Sender = new ToolStripMenuItem();
			_menuitemReply2Herd = new ToolStripMenuItem();
			_menuitemForward = new ToolStripMenuItem();
			_menuitemDeleteArticle = new ToolStripMenuItem();
			_menuitemNewsgroups = new ToolStripMenuItem();
			_menuitemAttachFile = new ToolStripMenuItem();
			_menuitemNext = new ToolStripMenuItem();
			_menuitemPrevious = new ToolStripMenuItem();
			_menuActions = new ToolStripMenuItem();
			_headerPanel.SuspendLayout();
			_bodyPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// _subjectBox
            // 
            this._subjectBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._subjectBox.ContextProvider = null;
            this._subjectBox.EmptyText = null;
            this._subjectBox.Location = new System.Drawing.Point(100, 92);
            this._subjectBox.Name = "_subjectBox";
            this._subjectBox.Size = new System.Drawing.Size(688, 21);
            this._subjectBox.TabIndex = 4;
            this._subjectBox.Text = "";
            this._subjectBox.TextChanged += new System.EventHandler(this._subjectBox_TextChanged);
            // 
            // _groupsLabel
            // 
            this._groupsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._groupsLabel.AutoScroll = true;
            this._groupsLabel.Location = new System.Drawing.Point(100, 66);
            this._groupsLabel.Name = "_groupsLabel";
            this._groupsLabel.ResourceList = null;
            this._groupsLabel.Size = new System.Drawing.Size(684, 20);
            this._groupsLabel.TabIndex = 3;
            // 
            // _fromlabel
            // 
            this._fromlabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._fromlabel.Location = new System.Drawing.Point(8, 10);
            this._fromlabel.Name = "_fromlabel";
            this._fromlabel.Size = new System.Drawing.Size(90, 17);
            this._fromlabel.TabIndex = 6;
            this._fromlabel.Text = "From:";
            this._fromlabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _subjectLabel
            // 
            this._subjectLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._subjectLabel.Location = new System.Drawing.Point(8, 94);
            this._subjectLabel.Name = "_subjectLabel";
            this._subjectLabel.Size = new System.Drawing.Size(90, 17);
            this._subjectLabel.TabIndex = 10;
            this._subjectLabel.Text = "Subject:";
            this._subjectLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _statusBar
            // 
            this._statusBar.AllowDrop = true;
            this._statusBar.Location = new System.Drawing.Point(0, 544);
            this._statusBar.Name = "_statusBar";
            this._statusBar.Size = new System.Drawing.Size(792, 22);
            this._statusBar.TabIndex = 11;
            this._statusBar.DragDrop += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragDrop);
            this._statusBar.DragOver += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragOver);
            // 
            // _openAttachDialog
            // 
            this._openAttachDialog.Multiselect = true;
            // 
            // _attachList
            // 
            this._attachList.AllowDrop = true;
            this._attachList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._attachList.ContextMenu = this._attachListMenu;
            this._attachList.Dock = System.Windows.Forms.DockStyle.Right;
            this._attachList.FullRowSelect = true;
            this._attachList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this._attachList.HideSelection = false;
            this._attachList.Location = new System.Drawing.Point(628, 0);
            this._attachList.Name = "_attachList";
            this._attachList.Size = new System.Drawing.Size(160, 394);
            this._attachList.TabIndex = 3;
            this._attachList.SelectionChanged += new System.EventHandler(this._attachListView_SelectedIndexChanged);
            this._attachList.Resize += new System.EventHandler(this._attachListView_Resize);
            this._attachList.DragOver += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragOver);
            this._attachList.DragDrop += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragDrop);
            this._attachList.KeyDown += new System.Windows.Forms.KeyEventHandler(this._attachListView_KeyDown);
            // 
            // _attachListMenu
            // 
            this._attachListMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                            this._removeAttachMenuItem});
            // 
            // _removeAttachMenuItem
            // 
            this._removeAttachMenuItem.Index = 0;
            this._removeAttachMenuItem.Text = "Remove";
            this._removeAttachMenuItem.Click += new System.EventHandler(this._removeAttachMenuItem_Click);
            // 
            // _headerPanel
            // 
            this._headerPanel.AllowDrop = true;
            this._headerPanel.Controls.Add(this._newsgroupsLabel);
            this._headerPanel.Controls.Add(this._serverComboBox);
            this._headerPanel.Controls.Add(this._serverLabel);
            this._headerPanel.Controls.Add(this._toolbarSeparator);
            this._headerPanel.Controls.Add(this._fromTextLabel);
            this._headerPanel.Controls.Add(this._fromComboBox);
            this._headerPanel.Controls.Add(this._subjectLabel);
            this._headerPanel.Controls.Add(this._subjectBox);
            this._headerPanel.Controls.Add(this._fromlabel);
            this._headerPanel.Controls.Add(this._groupsLabel);
            this._headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._headerPanel.Location = new System.Drawing.Point(0, 26);
            this._headerPanel.Name = "_headerPanel";
            this._headerPanel.Size = new System.Drawing.Size(792, 120);
            this._headerPanel.TabIndex = 0;
            this._headerPanel.DragDrop += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragDrop);
            this._headerPanel.DragOver += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragOver);
            // 
            // _newsgroupsLabel
            // 
            this._newsgroupsLabel.AutoSize = false;
            this._newsgroupsLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this._newsgroupsLabel.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(70)), ((System.Byte)(70)), ((System.Byte)(211)));
            this._newsgroupsLabel.Location = new System.Drawing.Point(8, 66);
            this._newsgroupsLabel.Name = "_newsgroupsLabel";
            this._newsgroupsLabel.Size = new System.Drawing.Size(90, 17);
            this._newsgroupsLabel.TabIndex = 0;
            this._newsgroupsLabel.TabStop = false;
            this._newsgroupsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._newsgroupsLabel.Click += new System.EventHandler(this._newsgroupsLabel_Click);
            // 
            // _serverComboBox
            // 
            this._serverComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._serverComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this._serverComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._serverComboBox.Location = new System.Drawing.Point(100, 38);
            this._serverComboBox.Name = "_serverComboBox";
            this._serverComboBox.Size = new System.Drawing.Size(688, 22);
            this._serverComboBox.TabIndex = 2;
            this._serverComboBox.SelectedIndexChanged += new System.EventHandler(this._serverComboBox_SelectedIndexChanged);
            // 
            // _serverLabel
            // 
            this._serverLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._serverLabel.Location = new System.Drawing.Point(8, 38);
            this._serverLabel.Name = "_serverLabel";
            this._serverLabel.Size = new System.Drawing.Size(90, 17);
            this._serverLabel.TabIndex = 13;
            this._serverLabel.Text = "News Server:";
            this._serverLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _toolbarSeparator
            // 
            this._toolbarSeparator.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._toolbarSeparator.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._toolbarSeparator.Location = new System.Drawing.Point(0, 0);
            this._toolbarSeparator.Name = "_toolbarSeparator";
            this._toolbarSeparator.Size = new System.Drawing.Size(792, 2);
            this._toolbarSeparator.TabIndex = 12;
            // 
            // _fromTextLabel
            // 
            this._fromTextLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._fromTextLabel.Location = new System.Drawing.Point(100, 8);
            this._fromTextLabel.Name = "_fromTextLabel";
            this._fromTextLabel.ReadOnly = true;
            this._fromTextLabel.Size = new System.Drawing.Size(671, 21);
            this._fromTextLabel.TabIndex = 0;
            this._fromTextLabel.Text = "";
            this._fromTextLabel.Enter += new System.EventHandler(this._fromTextLabel_Enter);
            // 
            // _fromComboBox
            // 
            this._fromComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._fromComboBox.Location = new System.Drawing.Point(100, 8);
            this._fromComboBox.Name = "_fromComboBox";
            this._fromComboBox.Size = new System.Drawing.Size(688, 21);
            this._fromComboBox.TabIndex = 1;
            this._fromComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this._fromBox_KeyDown);
            this._fromComboBox.DropDown += new System.EventHandler(this._fromBox_DropDown);
            this._fromComboBox.Leave += new System.EventHandler(this._fromBox_Leave);
            this._fromComboBox.Enter += new System.EventHandler(this._fromBox_DropDown);
            // 
            // _bodyListSplitter
            // 
            this._bodyListSplitter.Dock = System.Windows.Forms.DockStyle.Right;
            this._bodyListSplitter.Location = new System.Drawing.Point(624, 0);
            this._bodyListSplitter.Name = "_bodyListSplitter";
            this._bodyListSplitter.Size = new System.Drawing.Size(4, 394);
            this._bodyListSplitter.TabIndex = 13;
            this._bodyListSplitter.TabStop = false;
            // 
            // _bodyPanel
            // 
            this._bodyPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._bodyPanel.Controls.Add(this._bodyListSplitter);
            this._bodyPanel.Controls.Add(this._attachList);
            this._bodyPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._bodyPanel.Location = new System.Drawing.Point(0, 146);
            this._bodyPanel.Name = "_bodyPanel";
            this._bodyPanel.Size = new System.Drawing.Size(792, 398);
            this._bodyPanel.TabIndex = 1;
            this._bodyPanel.DragDrop += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragDrop);
            this._bodyPanel.DragOver += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragOver);
            // 
            // _toolbar
            // 
            this._toolbar.AllowDrop = true;
            this._toolbar.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
            this._toolbar.Divider = false;
            this._toolbar.DropDownArrows = true;
            this._toolbar.GradientEndColor = System.Drawing.SystemColors.Control;
            this._toolbar.GradientStartColor = System.Drawing.SystemColors.ControlLightLight;
            this._toolbar.Location = new System.Drawing.Point(0, 0);
            this._toolbar.Name = "_toolbar";
            this._toolbar.ShowToolTips = true;
            this._toolbar.Size = new System.Drawing.Size(792, 26);
            this._toolbar.TabIndex = 11;
            this._toolbar.TextAlign = System.Windows.Forms.ToolBarTextAlign.Right;
            this._toolbar.DragDrop += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragDrop);
            this._toolbar.DragOver += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragOver);
            // 
            // mainMenu1
            // 
            mainMenu1.Items.AddRange( new ToolStripItem[]{ _fileMenuItem, _editMenuItem, _menuActions, _formatMenuItem});
            // 
            // _fileMenuItem
            // 
            _fileMenuItem.DropDownItems.AddRange( new ToolStripItem[] { _newMenuItem, _postMenuItem,
                                                                        _menuSeparator1, _saveMenuItem, _saveAndCloseMenuItem,
                                                                        _menuSeparator4, _closeMenuItem});
            this._fileMenuItem.Text = "&File";
            // 
            // _newMenuItem
            // 
            this._newMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            this._newMenuItem.Text = "&New";
            this._newMenuItem.Click += new System.EventHandler(this._newMenuItem_Click);
            // 
            // _postMenuItem
            // 
            this._postMenuItem.Text = "&Send Article";
            this._postMenuItem.ShortcutKeys = Keys.Control | Keys.Enter;
            // 
            // _saveMenuItem
            // 
            this._saveMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            this._saveMenuItem.Text = "Sa&ve";
            // 
            // _saveAndCloseMenuItem
            // 
            this._saveAndCloseMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            this._saveAndCloseMenuItem.Text = "Save and C&lose";
            // 
            // _closeMenuItem
            // 
            this._closeMenuItem.Text = "&Close";
            this._closeMenuItem.Click += new System.EventHandler(this._closeMenuItem_Click);
            // 
            // _editMenuItem
            // 
            this._editMenuItem.DropDownItems.AddRange(new ToolStripItem[] { _undoMenuItem, _redoMenuItem,
																			_menuSeparator5,
                                                                            _cutMenuItem, _copyMenuItem, _pasteMenuItem,
                                                                            _menuitemAttachFile, _menuitemNewsgroups});
            this._editMenuItem.Text = "&Edit";
            this._editMenuItem.DropDownItemClicked += new ToolStripItemClickedEventHandler(_editMenuItem_DropDownItemClicked);
			// 
			// _undoMenuItem
			// 
			this._undoMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
			this._undoMenuItem.Text = "&Undo";
			// 
			// _redoMenuItem
			// 
			this._redoMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
			this._redoMenuItem.Text = "&Redo";
            // 
            // _cutMenuItem
            // 
            this._cutMenuItem.ShortcutKeys = Keys.Control | Keys.X;
            this._cutMenuItem.Text = "Cu&t";
            // 
            // _copyMenuItem
            // 
            this._copyMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            this._copyMenuItem.Text = "&Copy";
            // 
            // _pasteMenuItem
            // 
            this._pasteMenuItem.ShortcutKeys = Keys.Control | Keys.V;
            this._pasteMenuItem.Text = "&Paste";
            // 
            // _formatMenuItem
            // 
            this._formatMenuItem.DropDownItems.AddRange( new ToolStripItem[] { _encodingMenuItem } );
            this._formatMenuItem.Text = "F&ormat";
            // 
            // _encodingMenuItem
            // 
            this._encodingMenuItem.Text = "&Encoding";

            _menuitemReply2Sender.Text = "Reply to &Sender";
			_menuitemReply2Herd.Text = "&Reply";
			_menuitemForward.Text = "&Forward";
			_menuitemDeleteArticle.Text = "&Delete";
			_menuitemNewsgroups.Text = "&Newsgroups …";
			_menuitemAttachFile.Text = "&Insert Attachment…";
			_menuitemNext.Text = "&Next Article";
			_menuitemPrevious.Text = "&Previous Article";

			_menuActions.Text = "&Actions";
            _menuActions.DropDownItems.AddRange( new ToolStripItem[]{ _menuitemReply2Herd, _menuitemReply2Sender, _menuitemForward,
                                                                      _menuitemDeleteArticle, _menuitemNext, _menuitemPrevious} );
			// 
            // EditMessageForm
            // 
            this.AllowDrop = true;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(792, 566);
            this.Controls.Add(this._bodyPanel);
            this.Controls.Add(this._headerPanel);
            this.Controls.Add(this._statusBar);
            this.Controls.Add(this._toolbar);
            this.Controls.Add( this.mainMenu1 );

            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new System.Drawing.Size(200, 136);
            this.Name = "EditMessageForm";
            this.ShowInTaskbar = true;
            this.Text = "New Article";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.EditMessageForm_KeyDown);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.EditMessageForm_Closing);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragOver);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.EditMessageForm_DragDrop);
            this._headerPanel.ResumeLayout(false);
            this._bodyPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        void _editMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            _undoMenuItem.Enabled = _cutMenuItem.Enabled = _copyMenuItem.Enabled = _pasteMenuItem.Enabled =
                _whatWeAreDoing == FormStateVerb.Edit && (ActiveControl is TextBox);	// Checking for a TextBox is valid, as JetTextBox extends it
        }
        #endregion

        public static void EditAndPostMessage( IResourceList groups,
                                               string subject, string message, string references,
                                               bool newArticle )
        {
            EditMessageForm theForm = new EditMessageForm();
            theForm.RestoreSettings();
            theForm._whatWeAreDoing = FormStateVerb.Edit;

            ServerResource server = null;
            IContact myself = Core.ContactManager.MySelf;
            string myselfDisplayName = Core.ContactManager.GetFullName( myself.Resource );
            string from = myselfDisplayName;
            string email = myself.DefaultEmailAddress;
            if( email != null && email.Length > 0 )
            {
                from += " <" + email + '>';
            }
            if( groups == null || groups.Count == 0 )
            {
                theForm._groups = Core.ResourceStore.EmptyResourceList;
                theForm.UpdateStatusBarText();
            }
            else
            {
                IResource serverRes = new NewsgroupResource( groups[ 0 ] ).Server;
                if( serverRes != null )
                {
                    server = new ServerResource( serverRes );
                    theForm._serverComboBox.SelectedItem = serverRes;
                    string userDisplayName = server.UserDisplayName;
                    if( userDisplayName.Length == 0 )
                    {
                        userDisplayName = myselfDisplayName;
                    }
                    string userEmailAddress = server.UserEmailAddress;
                    if( userEmailAddress.Length > 0 )
                    {
                        from = userDisplayName + " <" + userEmailAddress + ">";
                    }
                    else
                    {
                        from = userDisplayName;
                    }
                }
                theForm._groups = groups;
            }
            if( server == null )
            {
                server = new ServerResource( (IResource) theForm._serverComboBox.SelectedItem );
            }
            theForm._fromComboBox.Text = theForm._fromTextLabel.Text = from;
            theForm._subjectBox.Text = subject;
            if( newArticle && server.UseSignature )
            {
                message += "\r\n";
                message += server.MailSignature;
            }

        	// Upload text to the edit control
			theForm._browser.SecurityContext = WebSecurityContext.Internet;	// No malicious content expected, but some small helper scripts are
			theForm._browser.Html = FormatBodyForTextarea( message );
            //theForm._bodyBox.Select( 0, 0 );	// TODO: drop or reimplement for the textarea (no problem, but is it needed?)

            theForm._references = references;
            theForm.ArticleChanged = false;
			// Grant input focus either to the subject (for the newly-created article) or to the body edit (for replies)
        	if( newArticle )
        	{
        		theForm._subjectBox.Select();
        	}
        	else
        	{
        		theForm._browser.Select();
        	}
        	theForm.SetCharset( server.Charset );
            theForm.UpdateToolbarButtons();
            theForm.UpdateAttachListVisibility();
            theForm.Show();
            theForm.SetNewsgroups();
        }

		/// <summary>
		/// Formats text of the message body into an HTML page so that it could be loaded into a text area.
		/// </summary>
		/// <param name="message">Original message text.</param>
		/// <returns>The HTML page for editing the text.</returns>
		public static string FormatBodyForTextarea( string message )
		{
			StringWriter	content = new StringWriter();
			content.WriteLine( "<html>" );
			content.WriteLine( "<body style=\"padding: 0px; margin: 0px; overflow: hidden;\" onfocus=\"document.getElementById('{0}').focus();\">", _sPlainTextEditorId );

			// The Textarea Element
			content.Write( "<textarea id=\"{0}\" ", _sPlainTextEditorId);
			//content.Write("style=\"width: 100%; height: 100%; border: none; font-family: Verdana; color: black; overflow-y: hidden;\" ");
			content.Write("style=\"width: 100%; height: 100%; border: none; font-family: Verdana; color: black; overflow-y: expression(this.scrollHeight > this.clientHeight ? 'scroll' : 'hidden');\" ");
			content.Write("tabindex=\"1\" ");
			content.WriteLine(">");

			// Message Text
			content.WriteLine( HttpUtility.HtmlEncode(message) );
			content.WriteLine("</textarea>");

			return content.ToString();
		}

		public static void EditAndPostMessage( IResource draftArticle )
        {
            EditMessageForm theForm = (EditMessageForm) _openArticles[ draftArticle.Id ];
            if( theForm != null )
            {
                theForm.Activate();
            }
            else
            {
                theForm = new EditMessageForm();
                _openArticles[ draftArticle.Id ] = theForm;
                theForm.RestoreSettings();
                theForm._whatWeAreDoing = FormStateVerb.Edit;
                theForm._draftArticle = draftArticle;
                IResourceList groups = draftArticle.GetLinksFrom( NntpPlugin._newsGroup, NntpPlugin._propTo );
                if( groups == null || groups.Count == 0 )
                {
                    theForm._groups = Core.ResourceStore.EmptyResourceList;
                }
                else
                {
                    IResource serverRes = new NewsgroupResource( groups[ 0 ] ).Server;
                    if( serverRes != null )
                    {
                        theForm._serverComboBox.SelectedItem = serverRes;
                    }
                    theForm._groups = groups;
                }
                theForm._fromComboBox.Text =
                    theForm._fromTextLabel.Text = NewsContactHelper.RestoreFromField( draftArticle );
                theForm._subjectBox.Text = draftArticle.GetPropText( Core.Props.Subject );
				theForm._browser.SecurityContext = WebSecurityContext.Internet;	// No malicious content expected, but some small helper scripts are
				theForm._browser.Html = FormatBodyForTextarea(draftArticle.GetPropText( Core.Props.LongBody ));
				// theForm._bodyBox.Select( 0, 0 );	// TODO: drop or impl
                theForm._references = draftArticle.GetPropText( NntpPlugin._propReferenceId );
                theForm.ArticleChanged = false;

                AddAttachmentResources( theForm, draftArticle );

				// Select either subject (if empty) or the body (if we've already composed the subject and now're doing the body)
                if( theForm._subjectBox.Text.Length > 0 )
                {
                    theForm._browser.Select();
                }
                else
                {
                    theForm._subjectBox.Select();
                }
                theForm.SetCharset( draftArticle.GetPropText( Core.FileResourceManager.PropCharset ) );
                theForm.UpdateToolbarButtons();
                theForm.UpdateAttachListVisibility();
                theForm.SetListenerForArticle( draftArticle );
                theForm.Show();
                theForm.SetNewsgroups();
                theForm.ArticleChanged = true;
            }
        }

        private static void  AddAttachmentResources( EditMessageForm form, IResource article )
        {
            _attaches = article.GetLinksOfTypeLive( null, NntpPlugin._propAttachment );
            foreach( IResource res in _attaches )
                form._attachList.JetListView.Nodes.Add( res );
        }

        public static void OpenMessageInSeparateWindow( IResource article )
        {
            EditMessageForm theForm = (EditMessageForm) _openArticles[ article.Id ];
            if( theForm != null )
            {
                theForm.Activate();
            }
            else
            {
                theForm = new EditMessageForm();
                theForm.RestoreSettings();
                theForm._whatWeAreDoing = FormStateVerb.View;

                //  Do not trace the event since the article is RO.
                theForm._subjectBox.TextChanged -= theForm._subjectBox_TextChanged;

                theForm.OpenArticle(article);
				theForm.Show();
				if( article.HasProp( NntpPlugin._propIsUnread ) )
                {
                    new ResourceProxy( article ).SetProp( NntpPlugin._propIsUnread, false );
                }
            }
        }

        private void OpenArticle( IResource article )
        {
            #region Preconditions
            if( article == null )
                throw new ArgumentNullException( "article", "EditMessageForm.OpenArticle -- Input resource can not be NULL." );
            #endregion Preconditions

            _openArticles[ article.Id ] = this;
            _groups = article.GetLinksFrom( NntpPlugin._newsGroup, NntpPlugin._propTo );
            if( _groups.Count > 0 )
            {
                IResource serverRes = new NewsgroupResource( _groups[ 0 ] ).Server;
                if( serverRes != null )
                {
                    _serverComboBox.Enabled = true;
                    _serverComboBox.SelectedItem = serverRes;
                }
            }
            SetArticleReadonly( true );
            _browser.Visible = true;
        	_toolbar.Visible = true;
            _fromComboBox.Visible = false;
            _fromTextLabel.Width = _fromComboBox.Width;
            _subjectBox.BackColor = SystemColors.Window;
            IResourceBrowser rBrowser = Core.ResourceBrowser;
            if( rBrowser.VisibleResources.IndexOf( article ) >=0 )
            {
                rBrowser.SelectResource( article );
            }
            _fromTextLabel.Text = NewsContactHelper.RestoreFromField( article );
            SetNewsgroups();
            _subjectBox.Text = article.GetPropText( Core.Props.Subject );
			_browser.SecurityContext = _browserContext;
			WordPtr[]	words = new WordPtr[0];
            try
            {
                _browser.Html = ArticlePreviewPane.ArticleBody2Html( article, ref words );
            }
            catch( Exception e )
            {
                _browser.Html = e.Message;
            }
			_browser.Show();
        	_browser.Select();

            AddAttachmentResources( this, article );

            _attachList.ContextMenu = null;
            _attachList.ShowContextMenu = true;

            SetCharset( article.GetPropText( Core.FileResourceManager.PropCharset ) );
            UpdateToolbarButtons();
            UpdateAttachListVisibility();
            /**
             * closing window if article is deleted
             */
            SetListenerForArticle( article );
            if( article.HasProp( NntpPlugin._propHasNoBody ) )
            {
                RefreshArticleAction.RefreshArticleImpl( article.ToResourceList() );
            }
        }

        private void SetListenerForArticle( IResource article )
        {
            if( _liveArticle != null )
            {
                IResource oldArticle = GetListenedArticle();
                if ( oldArticle != null )
                {
                    if( oldArticle == article )
                    {
                        return;
                    }
                    _openArticles.Remove( oldArticle.Id );
                }
                _liveArticle.Dispose();
            }
            _liveArticle = article.ToResourceListLive();
            _liveArticle = _liveArticle.Minus( Core.ResourceStore.FindResourcesWithProp( null, Core.Props.IsDeleted ) );
            _liveArticle.ResourceDeleting += EditMessageForm_ResourceDeleting;
            _liveArticle.ResourceChanged += _liveArticle_ResourceChanged;
        }

        private IResource GetListenedArticle()
        {
            IResource result = null;
            if( _liveArticle != null )
            {
                try
                {
                    lock( _liveArticle )
                    {
                        if( _liveArticle.Count > 0 )
                        {
                            result = _liveArticle[ 0 ];
                        }
                    }
                }
                catch( InvalidResourceIdException )
                {
                    result = null;
                }
            }
            return result;
        }

        private static IResource GetNextArticle( IResource article )
        {
            IResource result = article;
            while( result != null )
            {
                result = Core.ResourceBrowser.GetResourceBelow( result );
                if( result == null || result.Type == NntpPlugin._newsArticle )
                {
                    break;
                }
            }
            return result;
        }

        private static IResource GetPrevArticle( IResource article )
        {
            IResource result = article;
            while( result != null )
            {
                result = Core.ResourceBrowser.GetResourceAbove( result );
                if( result == null || result.Type == NntpPlugin._newsArticle )
                {
                    break;
                }
            }
            return result;
        }

        private void _liveArticle_ResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            IPropertyChangeSet changeSet = e.ChangeSet;
            IResource article = e.Resource;
            if( article != _draftArticle )
            {
                if( changeSet.IsPropertyChanged( Core.Props.LongBody ) ||
                    changeSet.IsPropertyChanged( NntpPlugin._propHtmlContent ) ||
                    changeSet.IsPropertyChanged( Core.Props.Subject ) ||
                    changeSet.IsPropertyChanged( Core.ContactManager.Props.LinkFrom ) ||
                    changeSet.IsPropertyChanged( Core.ResourceStore.PropTypes[ "NoFormat" ].Id ) )
                {
                    Core.UIManager.QueueUIJob( new ResourceDelegate( OpenArticle ), e.Resource );
                }
            }
            else
            {
                if( changeSet.IsPropertyChanged( Core.Props.Parent ) &&
                    NewsFolders.IsInFolder( article, NewsFolders.SentItems ) )
                {
                    AsyncClose( null );
                }
            }
        }

	    private void _subjectBox_TextChanged(object sender, EventArgs e)
        {
	        string subject = _subjectBox.Text;
	        Text = ( subject.Length == 0 ) ? "New Article" : subject;

            ArticleChanged = true;
            _statusBar.Text = string.Empty;
            UpdateToolbarButtons();
        }

        private void EditMessageForm_KeyDown(object sender, KeyEventArgs e)
        {
            KeyboardHandler( e );
        }

	    private void EditMessageForm_Closing(object sender, CancelEventArgs e)
        {
            if( ArticleChanged )
            {
                DialogResult result = MessageBox.Show( this, 
                    "Article has been modified. Save it in Drafts folder?",
                    "Save Article", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question );
                if ( result == DialogResult.Cancel )
                {
                    e.Cancel = true;
                    return;
                }
                else if ( result == DialogResult.Yes )
                {
                    SaveArticle();
                }
                else if( result == DialogResult.No )
                {
                    if( _draftArticle != null )
                    {
                        new ResourceProxy( _draftArticle ).Delete();
                        _draftArticle = null;
                    }
                }
            }
            Core.UIManager.MainWindowClosing -= EditMessageForm_Closing;
            ArticleChanged = false;
            if( _liveArticle != null )
            {
                IResource article = GetListenedArticle();
                if( article != null && !article.IsDeleting )
                {
                    _openArticles.Remove( article.Id );
                }
                _liveArticle.Dispose();
                _liveArticle = null;
            }
            if( _draftArticle != null )
            {
                _openArticles.Remove( _draftArticle.Id );
            }
        }

        private void _attachListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            IResourceList selectedList = _attachList.GetSelectedResources();
            if( selectedList.Count > 0 )
            {
                IResource selected = selectedList[ 0 ];
                _fullNameToolTip.SetToolTip( _attachList,
                    Path.Combine( selected.GetPropText( NntpPlugin._propDirectory ),
                                  selected.GetPropText( Core.Props.Name ) ) );
            }
        }

        private void _attachListView_Resize(object sender, EventArgs e)
        {
            if( _attachList.Width > 4 )
            {
                sizeCol.Width = _attachList.Width / 3 + 1;
                nameCol.Width = _attachList.Width - sizeCol.Width - 4;
            }
        }

        private void _removeAttachMenuItem_Click(object sender, EventArgs e)
        {
            RemoveSelectedAttachments();
        }

	    private void _fromTextLabel_Enter(object sender, EventArgs e)
        {
            if( _fromComboBox.Visible )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( MakeFromTextInvisible ) );
            }
        }

	    private void _fromBox_DropDown(object sender, EventArgs e)
        {
            _fromTextLabel.Visible = false;
        }

        private void _fromBox_Leave(object sender, EventArgs e)
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( UpdateFromText ) );
        }

	    private void _fromBox_KeyDown(object sender, KeyEventArgs e)
        {
            KeyboardHandler( e );
        }

        private void _attachListView_KeyDown(object sender, KeyEventArgs e)
        {
            if( e.KeyCode == Keys.Delete && !e.Alt && !e.Control && !e.Shift )
            {
                RemoveSelectedAttachments();
            }
            else
            {
                KeyboardHandler( e );
            }
        }

        private void _browser_KeyDown( object sender, KeyEventArgs e )
        {
        	// Check if this key could be processed by keyboard handler or it should always be processed by the browser itself
        	if( !JetTextBox.IsEditorKey( e.KeyData ) )
        		KeyboardHandler( e );
        }

		private void _serverComboBox_SelectedIndexChanged( object sender, EventArgs e )
        {
            if( _groups != null )
            {
                IResource server = (IResource) _serverComboBox.SelectedItem;
                if( server == null )
                {
                    _groupsLabel.ResourceList = _groups = Core.ResourceStore.EmptyResourceList;
                }
                else
                {
                    _groupsLabel.ResourceList = _groups = _groups.Intersect(
                        new ServerResource( server ).Groups, true );
                }
                UpdateToolbarButtons();
            }
        }

        private void _newsgroupsLabel_Click(object sender, EventArgs e)
        {
            if( !_isBusy )
            {
                SelectNewsgroups(GetContext( ActionContextKind.Toolbar ));
            }
        }
        
        private void _newMenuItem_Click(object sender, EventArgs e)
        {
            EditAndPostMessage( _groups, string.Empty, string.Empty, string.Empty, true );
        }

        private void OnSaveAndClose(IActionContext context )
        {
            SaveArticle();
            AsyncClose( null );
        }

        private void _closeMenuItem_Click(object sender, EventArgs e)
        {
            AsyncClose( null );
        }

       private void OnEncodingMenuItemClicked(object sender, EventArgs e)
        {
			// Get the menu item that has been clicked (selected)
            EncodingMenuItem miClicked = sender as EncodingMenuItem;
			if(miClicked == null)
				throw new InvalidOperationException("An encoding menu item event has come from a wrong object.");

		   // Switch the charset
		   _charset = miClicked.Charset;

			// Check the clicked item, uncheck all the others
		   OnUpdateSelectedCharset();

			// Update the article encoding according to the new selection
            IResource article = GetListenedArticle();
            if( article != null )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate,
                    new ReencodeArticleDelegate( ReencodeArticle ), article );
            }
        }

        private void EditMessageForm_DragOver( object sender, DragEventArgs e )
        {
            e.Effect = DragDropEffects.None;
            IResourceList resources = e.Data.GetData( typeof( IResourceList ) ) as IResourceList;
            if( resources != null )
            {
                foreach( IResource res in resources )
                {
                    if( File.Exists( IOTools.Combine( res.GetPropText( NntpPlugin._propDirectory ),
                        res.GetPropText( Core.Props.Name ) ) ) )
                    {
                        e.Effect = DragDropEffects.Link;
                    }
                }
            }
            else if( e.Data.GetDataPresent( DataFormats.FileDrop, false ) )
            {
                e.Effect = DragDropEffects.Link;
            }
        }

        private void EditMessageForm_DragDrop(object sender, DragEventArgs e)
        {
            IResourceList resources = e.Data.GetData( typeof( IResourceList ) ) as IResourceList;
            if( resources != null )
            {
                foreach( IResource res in resources )
                {
                    if( File.Exists( IOTools.Combine( res.GetPropText( NntpPlugin._propDirectory ),
                        res.GetPropText( Core.Props.Name ) ) ) )
                    {
                        AddAttachment( res );
                    }
                }
                UpdateAttachListVisibility();
            }
            else
            {
                string[] filenames = (string[]) e.Data.GetData( DataFormats.FileDrop );
                AddAttachments( filenames );
            }
        }

        private void AddAttachments( string[] filenames )
        {
            foreach( string filename in filenames )
            {
                FileInfo fi = IOTools.GetFileInfo( filename );
                if( fi != null )
                {
                    IResource attachment = Core.ResourceStore.NewResourceTransient( NntpPlugin._unknownFileResourceType );
                    attachment.SetProp( Core.Props.Name, fi.Name );
                    attachment.SetProp( Core.Props.Size, (int) fi.Length );
                    attachment.SetProp( NntpPlugin._propDirectory, fi.DirectoryName );

                    AddAttachment( attachment );
                }
            }
            UpdateAttachListVisibility();
        }

        private void  AddAttachment( IResource res )
        {
            _attaches = _attaches.Union( res.ToResourceListLive() );
            _attachList.JetListView.Nodes.Add( res );

            if( _draftArticle != null )
            {
                ResourceProxy proxy = new ResourceProxy( res );
                proxy.AsyncPriority = JobPriority.Immediate;
                proxy.AddLink( NntpPlugin._propAttachment, _draftArticle );
            }
            ArticleChanged = true;
        }

        private delegate void ReencodeArticleDelegate( IResource article );
        private void ReencodeArticle( IResource article )
        {
            string oldCharset = article.GetPropText( Core.FileResourceManager.PropCharset );
            if( oldCharset.Length > 0 )
            {
                Encoding newEncoding = MIMEParser.GetEncodingExceptionSafe( _charset.Name );
                Encoding oldEncoding = MIMEParser.GetEncodingExceptionSafe( oldCharset );
                article.SetProp( Core.Props.LongBody,
                    newEncoding.GetString( oldEncoding.GetBytes( article.GetPropText( Core.Props.LongBody ) ) ) );
                if( article.HasProp( NntpPlugin._propHtmlContent ) )
                {
                    article.SetProp( NntpPlugin._propHtmlContent,
                        newEncoding.GetString( oldEncoding.GetBytes( article.GetPropText( NntpPlugin._propHtmlContent ) ) ) );
                }
            }
            else
            {
                article.SetProp( Core.Props.LongBody,
                    MIMEParser.TranslateRawStringInCharset( _charset.Name, article.GetPropText( Core.Props.LongBody ) ) );
                if( article.HasProp( NntpPlugin._propHtmlContent ) )
                {
                    article.SetProp( NntpPlugin._propHtmlContent,
                        MIMEParser.TranslateRawStringInCharset( _charset.Name, article.GetPropText( NntpPlugin._propHtmlContent ) ) );
                }
            }
            article.SetProp( Core.FileResourceManager.PropCharset, _charset.Name );
            // correct from for a specific charset
            string from = article.GetPropText( NntpPlugin._propRawFrom );
            IContact sender;
            NewsArticleParser.ParseFrom( article, NewsArticleParser.TranslateHeader( _charset.Name, from ), out sender );
            // correct subject for a specific charset
            string subject = article.GetPropText( NntpPlugin._propRawSubject );
            if( subject.Length > 0 )
            {
                article.SetProp( Core.Props.Subject, NewsArticleParser.TranslateHeader( _charset.Name, subject ) );
            }
        }

        private void DisplayNextArticle( IActionContext context )
        {
            IResource article = GetListenedArticle();
            if( article != null )
            {
                //  Fix OM-13062:
                //  When user opens prelast article in the list and clicks on the
                //  "Next" toolbar button too fast then next resource is null;
                IResource next = GetNextArticle( article );
                if( next != null )
                    OpenArticle( next );
            }
        }

        private void DisplayPreviousArticle( IActionContext context )
        {
            IResource article = GetListenedArticle();
            if( article != null )
            {
                IResource prev = GetPrevArticle( article );
                if( prev != null )
                    OpenArticle( prev );
            }
        }

        private void DeleteArticle( IActionContext context )
        {
            IResource article = GetListenedArticle();
            if( article != null )
            {
                IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( article.Type );
                if( deleter != null )
                {
                    Core.ResourceAP.QueueJob(
                        JobPriority.AboveNormal, new ResourceDelegate( deleter.DeleteResource ), article );
                }
            }
        }

        private void DoPost( IActionContext context )
        {
			if( _toolbar.Enabled && !_isBusy )
            {
                if( _subjectBox.Text.Length == 0 )
                {
                    string newSubject = Core.UIManager.InputString( "Send Message",
                        "You did not specify a subject for this message. If you would like to provide one," +
                        " please type it now.",
                        "(no subject)",
                        null,
                        this,
                        InputStringFlags.AllowEmpty );
                
                    if ( newSubject == null )
                    {
                        SetArticleReadonly( false );
                        return;
                    }
                    _subjectBox.Text = newSubject;
                }
            	_toolbar.Enabled = false;
                SetArticleReadonly( true );
				_isBusy = true;
                _attachList.Enabled = false;
                ArticleChanged = true;
                SaveArticle();
                if( _draftArticle == null )
                {
                    SetArticleReadonly( false );
                	_toolbar.Enabled = true;
                    _attachList.Enabled = true;
					_isBusy = false;
                }
                else
                {
                    _statusBar.Text = "Posting article...";
                    IResource server = (IResource) _serverComboBox.SelectedItem;
                    if( server != null && server.HasProp( NntpPlugin._propPutInOutbox ) )
                    {
                        NewsFolders.PlaceResourceToFolder( _draftArticle, NewsFolders.Outbox );
                        AsyncClose( null );
                    }
                    else
                    {
                        NntpClientHelper.PostArticle( _draftArticle, PostingFinished, true );
                    }
                }
            }
        }

        private void PostingFinished( AsciiProtocolUnit unit )
        {
            NntpPostArticleUnit postUnit = (NntpPostArticleUnit) unit;
            if( unit != null )
            {
                string error = postUnit.Error;
                if( error == null )
                {
                    ArticleChanged = false;
                    AsyncClose( null );
                }
                else
                {
                    string errorMessage = "Posting failed. ";
                    if( error.Length > 0 )
                    {
                        errorMessage += error;
                    }
                    _statusBar.Text = errorMessage;
                    SetArticleReadonly( false );
                	_toolbar.Enabled = true;
                    _isBusy = false;
                    _attachList.Enabled = true;
                }
            }
        }

        private void InsertAttachment( IActionContext context )
        {
            if( _openAttachDialog.InitialDirectory.Length == 0 )
            {
                _openAttachDialog.InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.Personal );
            }
            if( _openAttachDialog.ShowDialog( this ) == DialogResult.OK )
            {
                string[] filenames = _openAttachDialog.FileNames;
                AddAttachments( filenames );
            }
        }

	    private void RemoveSelectedAttachments()
        {
            IResourceList selected = _attachList.GetSelectedResources();
            if( selected.Count > 0 )
            {
                foreach( IResource res in selected )
                {
                    _attachList.JetListView.Nodes.Remove( res );
                }
                _attaches = _attaches.Minus( selected );

                ArticleChanged = true;
                UpdateAttachListVisibility();
            }
        }

        private void SelectNewsgroups( IActionContext context )
        {
            IResource server = (IResource) _serverComboBox.SelectedItem;
            if( server != null )
            {
                IResourceList selectedGroups = server.ToResourceList();
                selectedGroups = Core.UIManager.SelectResourcesFromList( this,
                    selectedGroups.Union( new ServerResource( server ).AllSubNodes ), "Select Newsgroups", _groups );
                if( selectedGroups != null && selectedGroups.Count > 0 )
                {
                    if( selectedGroups.Count == _groups.Count &&
                        selectedGroups.Union( _groups ).Count == _groups.Count )
                    {
                        return;
                    }
                    _groups = selectedGroups;
                    SetNewsgroups();
                    UpdateToolbarButtons();
                    UpdateStatusBarText();
                }
            }
        }

	    private void SetNewsgroups()
	    {
	        _groupsLabel.ResourceList = _groups;
	    }

		/// <summary>
		/// Sets the message's charset to a new value.
		/// Does not change the current charset if the charset name is invalid.
		/// The default is system's default body charset.
		/// </summary>
        private void SetCharset( string charset )
        {
            Guard.NullArgument( charset, "charset" );
        	CharsetsEnum.Charset charsetNew = CharsetsEnum.TryGetCharset( charset );
        	Debug.Assert(charsetNew != null, "The charset name passed into the EMF.SetCharset does not resolve to a valid charset.");
			if(charsetNew != null)
			{
				_charset = charsetNew;
				OnUpdateSelectedCharset();
			}
        }

	    private void UpdateToolbarButtons()
        {
			// Toolbar buttons
			_toolbarActionManager.UpdateToolbarActions();

			// Miscellanous
            if( _whatWeAreDoing == FormStateVerb.View )
                _newsgroupsLabel.ClickableLink = false;
        }

        private void UpdateStatusBarText()
        {
            if( _groups.Count == 0 )
            {
                _statusBar.Text = "Please select newsgroup(s)...";
                _statusBar.ForeColor = Color.Red;
            }
            else
            {
                _statusBar.Text = string.Empty;
                _statusBar.ForeColor = Color.Black;
            }
        }

        private void UpdateAttachListVisibility()
        {
            SuspendLayout();
            _bodyListSplitter.Visible = _attachList.Visible = _attaches.Count > 0;

            int splitterIndex = _bodyPanel.Controls.GetChildIndex( _bodyListSplitter );
            int attachIndex = _bodyPanel.Controls.GetChildIndex( _attachList );
            if( attachIndex < splitterIndex )
            {
                _bodyPanel.Controls.SetChildIndex( _attachList, splitterIndex );
                _bodyPanel.Controls.SetChildIndex( _bodyListSplitter, attachIndex );
            }
            ResumeLayout();
        }

        private void EditMessageForm_ResourceDeleting( object sender, ResourceIndexEventArgs e )
        {
            AsyncClose( null );
        }

        private void UpdateFromText()
        {
            if( _fromTextLabel.Text != _fromComboBox.Text )
            {
                _fromTextLabel.Text = _fromComboBox.Text;
                ArticleChanged = true;
            }
            _fromTextLabel.Visible = true;
        }

        private void MakeFromTextInvisible()
        {
            _fromTextLabel.Visible = false;
        }

        private void KeyboardHandler( KeyEventArgs e )
        {
            IntHashTable.Entry entry = _hashKeyboardActions.GetEntry( (int) e.KeyData );
            if( entry != null )
            {
                IAction action = (IAction) entry.Value;
                IActionContext	context = GetContext( ActionContextKind.Keyboard );

                ActionPresentation	presentation = new ActionPresentation();
                presentation.Reset();
                action.Update( context, ref presentation );

                if(presentation.Enabled)
                {
                    action.Execute( context );
                    e.Handled = true;
                    return;
                }
            }

			if( (!JetTextBox.IsEditorKey(e.KeyData)) && (Core.ActionManager.ExecuteKeyboardAction(GetContext( ActionContextKind.Keyboard ), e.KeyData)))
				e.Handled = true;
			return;
        }

	    private void AsyncClose( IActionContext context )
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( Close ) );
        }

        private string CreateNntpBody( ServerResource server )
        {
            CheckUnfitEncoding();

            StringBuilder articleBuilder = StringBuilderPool.Alloc();
            try 
            {
                string from = _fromTextLabel.Text;
                string subject = _subjectBox.Text;
                /**
                 * encode headers if necessary
                 */
                if( MIMEParser.Has8BitCharacters( from ) )
                {
                    int i = from.IndexOf( "@" );
                    while( i > 0 && from[ --i ] != ' ' );

                    if( i <= 0 )
                    {
                        from = MIMEParser.CreateQuotedPrintableMIMEString( _charset.Name, from );
                    }
                    else
                    {
                        from = MIMEParser.CreateQuotedPrintableMIMEString( _charset.Name, from.Substring( 0, i ) ) +
                            from.Substring( i );
                    }
                }
                if( MIMEParser.Has8BitCharacters( subject ) )
                {
                    subject = MIMEParser.CreateBase64MIMEString( _charset.Name, subject );
                }

                articleBuilder.Append( "From: " );
                articleBuilder.Append( from );
                articleBuilder.Append( "\r\nSubject: " );
                articleBuilder.Append( subject );
                articleBuilder.Append( "\r\nNewsgroups: " );
                bool first = true;
                foreach( IResource group in _groups.ValidResources )
                {
                    if( !first )
                    {
                        articleBuilder.Append( ',' );
                    }
                    articleBuilder.Append( new NewsgroupResource( group ).Name );
                    first = false;
                }
                if( _references.Length > 0 )
                {
                    articleBuilder.Append( "\r\nReferences: " );
                    articleBuilder.Append( ParseTools.UnescapeCaseSensitiveString( _references ) );
                }
                string body = RetrieveFromTextarea();
                string content_type = "text/plain";
                if( _charset.Name.Length > 0 )
                {
                    content_type += "; charset=";
                    content_type += _charset.Name;
                }
                string lowerMsgFormat = server.MailFormat.ToLower();
                IResourceList attachList = _attaches;
                lock( attachList )
                {
                    if( attachList.Count == 0 )
                    {
                        if( lowerMsgFormat == "mime" )
                        {
                            articleBuilder.Append( "\r\nMIME-Version: 1.0\r\nContent-Transfer-Encoding: 8bit" );
                        }
                        content_type += "; format=flowed";
                        body = MultiPartBodyBuilder.BuildPlainTextFlowedBody( body, _charset.Name );
                    }
                    else
                    {
                        ArrayList attachments = new ArrayList( attachList.Count );
                        foreach( IResource attachment in attachList.ValidResources )
                        {
                            attachments.Add(
                                Path.Combine( attachment.GetPropText( NntpPlugin._propDirectory ),
                                attachment.GetPropText( Core.Props.Name ) ) );
                        }
                        string[] attachmentsArray = (string[]) attachments.ToArray( typeof( string ) );
                        string bodyEncoding = server.MIMETextEncoding;
                        if( lowerMsgFormat != "mime" )
                        {
                            body = MultiPartBodyBuilder.BuildBodyWithUUEncodedInsertions( body, _charset.Name, attachmentsArray );
                            content_type = string.Empty;
                        }
                        else
                        {
                            string boundary;
                            body = MultiPartBodyBuilder.BuildMIMEBody(
                                body, _charset.Name, bodyEncoding, out boundary, attachmentsArray );
                            content_type = "multipart/mixed; boundary=\"" + boundary + "\"";
                            articleBuilder.Append( "\r\nMIME-Version: 1.0\r\nContent-Transfer-Encoding: " );
                            if( bodyEncoding.ToLower() == "quoted-printable" || bodyEncoding.ToLower() == "base64" )
                            {
                                articleBuilder.Append( bodyEncoding );
                            }
                            else
                            {
                                articleBuilder.Append( "8bit" );
                            }
                        }
                    }
                }
                if( content_type.Length > 0 )
                {
                    articleBuilder.Append( "\r\nContent-Type: " );
                    articleBuilder.Append( content_type );
                }
                articleBuilder.Append( "\r\nX-Newsreader: " );
                articleBuilder.Append( "JetBrains " );
                articleBuilder.Append( Core.ProductFullName );
                articleBuilder.Append( ' ' );
                articleBuilder.Append( Core.ProductVersion );
                articleBuilder.Append( "\r\n\r\n" );
                articleBuilder.Append( body );
                if( Settings.ExtraFooterLF )
                {
                    articleBuilder.Append( "\r\n" );
                }
                articleBuilder.Append( "\r\n." );
                return articleBuilder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( articleBuilder );
            }
        }

		/// <summary>
		/// Checks whether the encoding currently selected in the news message editor fits to the text being edited,
		/// ie whether all the symbols can be represented by that encoding.
		/// If not, displays a message box that suggests switching to another encoding.
		/// TODO: check not only the body, but also the From, Subject and any other encoded field.
		/// </summary>
		private void CheckUnfitEncoding()
		{
			// Check whether the body can be encoded using the specified charset
			if(!_bSuppressUnfitEncodingWarning)
			{
				string body = RetrieveFromTextarea();
				string	sSourceChar = null, sDestChar = null;
				Encoding encoding = MIMEParser.GetEncodingExceptionSafe(_charset.Name);

				// Encode forth and back, and see if the result is different
				string bodyAfterEncoding = encoding.GetString( encoding.GetBytes( body ));
				if(bodyAfterEncoding != body)
				{
					// If the body length has not changed, check what symbols are failing
					if(bodyAfterEncoding.Length == body.Length)
					{
						for(int a = 0; a < body.Length; a++)
						{
							if(body[a] != bodyAfterEncoding[a])
							{
								sSourceChar = body[a].ToString(  );
								sDestChar = bodyAfterEncoding[a].ToString(  );
								break;
							}
						}
					}

					CharsetsEnum.Charset charsetUtf8 = CharsetsEnum.TryGetCharset( _DefaultCharset );
					DialogResult choice;
				    string title = Core.ProductName + " – Save News Article";
					if((sSourceChar == null) || (sDestChar == null))
                        choice = MessageBox.Show(String.Format(_CommonMessage + "\nWould you like to use the “{1}” charset instead, which will save all the characters?", _charset.Description, charsetUtf8.Description), title, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
					else
                        choice = MessageBox.Show(String.Format(_CommonMessage + "For example, “{2}” will become “{3}”.\n\nWould you like to use the “{1}” charset instead, which will save all the characters?", _charset.Description, charsetUtf8.Description, sSourceChar, sDestChar), title, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);

					if(choice == DialogResult.Yes)
					{
						_charset = charsetUtf8;
						OnUpdateSelectedCharset();
					}
					else
						_bSuppressUnfitEncodingWarning = true;
				}
			}
		}

		/// <summary>
		/// Retrieves the edited body text as plain text from the editing text area and returns it.
		/// </summary>
		/// <returns>Plain text of the message body.</returns>
		/// <remarks>If the browser has not been initialized yet, a <c>Null</c> value is returned.</remarks>
		protected string RetrieveFromTextarea()
		{
			IHtmlDomDocument doc = _browser.HtmlDocument;
			if(doc == null)
				return "";
			IHtmlDomElement textarea = doc.GetElementById( _sPlainTextEditorId );
			if(textarea == null)
				return "";
			return textarea.InnerText;
		}

		private void SaveArticle()
        {
            IResource serverRes = (IResource) _serverComboBox.SelectedItem;
            if( ArticleChanged && serverRes != null )
            {
                ArticleChanged = false;
                ServerResource server = new ServerResource( serverRes );
                string nntpBody = CreateNntpBody( server );
                _draftArticle = NewsFolders.PlaceArticle( _draftArticle, NewsFolders.Drafts, _groups,
                                                          _fromTextLabel.Text, _subjectBox.Text, RetrieveFromTextarea(),
                                                          _charset.Name, _references, nntpBody, _attaches );
                _openArticles[ _draftArticle.Id ] = this;
                SetListenerForArticle( _draftArticle );
            }
        }

        private bool ArticleChanged
        {
            get
            {
                if( !Visible )	// Form not visible => do not check the article body
                    return false;

                if( _articleChanged )
                    return true;

                if( _whatWeAreDoing == FormStateVerb.View )
                    return _articleChanged;

                string text = RetrieveFromTextarea();
				if( text == null )	// The text is not ready yet, which means that it's probably not dirty )
					return false;
                return ( _draftArticle != null ) && ( _draftArticle.GetPropText( Core.Props.LongBody ) != text );
            }
            set
            {
                bool lastValue = ArticleChanged;
                _articleChanged = value;
                IAsyncProcessor ap = Core.UserInterfaceAP;
                if( !value )
                {
                    if( lastValue )
                    {
                        ap.CancelTimedJobs( _saveArticleDelegate );
                    }
                }
                else
                {
                    ap.QueueJobAt( DateTime.Now.AddSeconds( 30 ), _saveArticleDelegate );
                }
                Core.UIManager.QueueUIJob( new SetSaveButtonEnabledDelegate( SetSaveButtonEnabled ), value );
            }
        }

        private delegate void SetSaveButtonEnabledDelegate( bool enabled );

        private void SetSaveButtonEnabled( bool enabled )
        {
            _saveAndCloseMenuItem.Enabled = _saveMenuItem.Enabled = enabled;
        }

        private void SetArticleReadonly( bool readOnly )
        {
            //_bodyBox.ReadOnly = readOnly;	// TODO: implement
            _subjectBox.ReadOnly = readOnly;
            _serverComboBox.Enabled = !readOnly;
        }

		/// <summary>
		/// Returns the action context for the current state of the control.
		/// </summary>
		/// <param name="kind">The kind of action which is invoked (keyboard, menu and so on).</param>
		/// <returns>The action context for the specified kind and the current state.</returns>
		public IActionContext GetContext( ActionContextKind kind )
		{
			ActionContext context = new ActionContext( kind, this, _liveArticle );
			context.SetCommandProcessor( this );
			context.SetOwnerForm( this );
			return context;
		}

		/// <summary>
		/// Checks if the command with the specified ID can be executed in the current state
		/// of the control.
		/// </summary>
		/// <param name="command">The ID of the command.</param>
		/// <returns>true if the ID of the command is known to the control and can be
		/// executed; false otherwise.</returns>
		public bool CanExecuteCommand( string command )
		{
			// Check if the focused control can process the action
			Control	active = ActiveControl;
			if((active == null) || (!(active.Focused)))
				active = _browser;	// Currently, we do not mark browser as being active if it gets activated by a click (as opposed to tab sequence activation), so we do a little hack to avoid directing actions to the subject box if actually the browser is active
			if((active is ICommandProcessor) && ((active as ICommandProcessor).CanExecuteCommand(command)))
				return true;

			return false;	// Cannot process myself
		}

		/// <summary>
		/// Executes the command with the specified ID.
		/// </summary>
		/// <param name="command">ID of the command to execute.</param>
		public void ExecuteCommand( string command )
		{
			// Check if the focused control can process the action
			Control	active = ActiveControl;
			if((active == null) || (!(active.Focused)))
				active = _browser;	// Currently, we do not mark browser as being active if it gets activated by a click (as opposed to tab sequence activation), so we do a little hack to avoid directing actions to the subject box if actually the browser is active
			if((active is ICommandProcessor) && ((active as ICommandProcessor).CanExecuteCommand(command)))
			{
				(active as ICommandProcessor).ExecuteCommand( command );
				return;
			}

			return;	// Cannot process myself
		}

		/// <summary>
		/// Loads an icon from the resources stream.
		/// </summary>
		/// <param name="name">Short name of the icon (without namespace prefixs and the extension).</param>
		/// <returns>Icon object.</returns>
		private static Icon LoadIcon( string name )
		{
			Stream stream;
			return (((stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "NntpPlugin.Icons." + name + ".ico" )) != null) ? new Icon( stream ) : null);
		}

		private void OnUpdateDelete( IActionContext context, ref ActionPresentation presentation )
		{
			presentation.Visible = (_whatWeAreDoing == FormStateVerb.View);
			presentation.Enabled = !_isBusy; // Disable if busy
		}

		private void OnUpdateDisplayNextArticle( IActionContext context, ref ActionPresentation presentation )
		{
			presentation.Visible = (_whatWeAreDoing == FormStateVerb.View);
            presentation.Enabled = presentation.Visible && !_isBusy && (GetNextArticle( GetListenedArticle() ) != null);
		}

		private void OnUpdateDisplayPreviousArticle( IActionContext context, ref ActionPresentation presentation )
		{
            presentation.Visible = (_whatWeAreDoing == FormStateVerb.View);
            presentation.Enabled = presentation.Visible && !_isBusy && (GetPrevArticle( GetListenedArticle() ) != null);
		}

		private void OnSaveArticle( IActionContext context )
		{
			SaveArticle();
		}

		private void OnUpdateSaveArticle( IActionContext context, ref ActionPresentation presentation )
		{
			presentation.Visible = (_whatWeAreDoing == FormStateVerb.Edit);
			presentation.Enabled = !_isBusy;	// Disable if busy
		}

		private void OnUpdateDoPost( IActionContext context, ref ActionPresentation presentation )
		{
            try
            {
                presentation.Visible = (_whatWeAreDoing == FormStateVerb.Edit);
                presentation.Enabled = presentation.Visible && (_groups != null) && (_groups.Count > 0) && !_isBusy;

			    // Do not enable the Send button until the control is finally created
			    IHtmlDomDocument htmlDocument;	// Foolproof variable to avoid async modification
			    if((_browser == null) || ((htmlDocument = _browser.HtmlDocument) == null) || (htmlDocument.GetElementById( _sPlainTextEditorId ) == null))
				    presentation.Enabled = false;
            }
            catch( NullReferenceException e )
            {
                Core.ReportBackgroundException( new  NullReferenceException( "NRE in OnUpdateDoPost", e ));
                presentation.Enabled = false;
            }
		}

		private void OnUpdateInsertAttachment( IActionContext context, ref ActionPresentation presentation )
		{
			presentation.Visible = _whatWeAreDoing == FormStateVerb.Edit;
			presentation.Enabled = !_isBusy;	// Disable if busy
		}

		private void OnUpdateSelectNewsgroups( IActionContext context, ref ActionPresentation presentation )
		{
            try
            {
                presentation.Visible = (_whatWeAreDoing == FormStateVerb.Edit);
                if( presentation.Visible )
                {
			        IResource server = (IResource) _serverComboBox.SelectedItem;
			        presentation.Enabled = (server != null) && (new ServerResource( server ).Groups.Count > 0) && !_isBusy; // Disable if busy
                }
            }
            catch( NullReferenceException e )
            {
                throw new ApplicationException( "NRE in OnUpdateSelectNewsgroups", e );
            }
		}

		/// <summary>
		/// Updates the radio-check on the Encoding submenu items so that only the currently-selected charset was checked.
		/// </summary>
		protected void OnUpdateSelectedCharset()
		{
		    foreach( ToolStripItem mi in _encodingMenuItem.DropDownItems )
			{
			    EncodingMenuItem emi;
			    if((emi = mi as EncodingMenuItem) == null)
					continue;
				emi.Checked = emi.Charset == _charset;
			}
		}

		#region MenuActionsManager Class — Binds Omea actions to the menu items, and performs presentation state updates

		/// <summary>
		/// Updates presentation for the subscribed menu items.
		/// </summary>
		public class MenuActionsManager : IDisposable
		{
			/// <summary>
			/// Maps menu items to the corresponding actions.
			/// </summary>
			protected Hashtable _hashMenuActions;

			/// <summary>
			/// The context provider.
			/// </summary>
			protected IContextProvider _contextprovider;

			/// <summary>
			/// The top level menu items listened for the <see cref="MenuItem.Popup"/> event.
			/// </summary>
			protected ToolStripMenuItem[]	_miTopLevel;

			public MenuActionsManager( IContextProvider contextprovider, ToolStrip menu )
			{
				//Core.UIManager.EnterIdle += new EventHandler( OnEnterIdle );
				_contextprovider = contextprovider;
				_hashMenuActions = new Hashtable();

				// Attach to the top-level menu-item popups
				_miTopLevel = new ToolStripMenuItem[ menu.Items.Count ];
				menu.Items.CopyTo( _miTopLevel, 0 );
				foreach( ToolStripMenuItem mi in _miTopLevel )
					mi.Click += OnTopLevelMenuItemPopup;
			}

			public void Add( ToolStripMenuItem mi, IAction action )
			{
				_hashMenuActions.Add( mi, action );
				mi.Click += OnMenuItemClicked;
			}

			#region IDisposable Members
			public void Dispose()
			{
				//if( ICore.Instance != null )
				//	Core.UIManager.EnterIdle -= new EventHandler( OnEnterIdle );

				// Unsubscribe
				if( _hashMenuActions != null )
				{
					foreach( ToolStripMenuItem mi in _hashMenuActions.Keys )
						mi.Click -= OnMenuItemClicked;
				}

				// Unsubscribe from the top-level items
				if(_miTopLevel != null)
				{
					foreach( ToolStripMenuItem mi in _miTopLevel )
						mi.Click -= OnTopLevelMenuItemPopup;
					_miTopLevel = null;
				}

				// Detach
				_hashMenuActions = null;
			}
			#endregion

			protected void OnEnterIdle( object sender, EventArgs e )
			{
				if( Core.ResourceStore != null )
                {
				    try {  UpdateActions();  }
				    catch( Exception ex )
				    {
					    Core.ReportBackgroundException( ex );
				    }
                }
			}

			/// <summary>
			/// Update all actions that represent the menu. Executes when a menu opens.
			/// </summary>
			protected void UpdateActions()
			{
				ActionPresentation presentation = new ActionPresentation();
				IActionContext context = _contextprovider.GetContext( ActionContextKind.MainMenu );

				// Update the actions
				foreach( ToolStripMenuItem mi in _hashMenuActions.Keys )
				{
					presentation.Reset();

					IAction action = (IAction)_hashMenuActions[ mi ];
					action.Update( context, ref presentation );

					mi.Visible = presentation.Visible;
					mi.Enabled = presentation.Enabled;
					if( presentation.Text != null )
						mi.Text = presentation.Text;
				}

				// Now for each of the top-level menus hide the double-separators
				foreach( ToolStripMenuItem miTopLevel in _miTopLevel )
				{
					bool	bPrevSep = true;	// Allows to hide to conscequent separators that might appear if all the items between them get hidden; setting it initially to True avoids showing a separator at the first position in the menu
					ToolStripItem miLastVisible = null;	// The last item that is visible (check for a sep)

					foreach( ToolStripItem mi in miTopLevel.DropDownItems )
					{
						if( mi is ToolStripSeparator )
						{
							if( bPrevSep )	// Two or more separators in a row, suppress the second one
								mi.Visible = false;
							else
							{	// This is a first separator after a normal item, show it (if it's not the first visible item in the menu)
								mi.Visible = bPrevSep = true;
								miLastVisible = mi;
							}
						}
						else
						{	// A normal item
							if( mi.Visible )
							{
								bPrevSep = false;
								miLastVisible = mi;
							}
						}
					}

					// Don't allow a separator to be the last visible item in the menu
					if( ( miLastVisible != null ) && ( miLastVisible is ToolStripSeparator ) )
						miLastVisible.Visible = false;
				}
			}

			private void OnMenuItemClicked( object sender, EventArgs e )
			{
				ToolStripMenuItem miSender = (ToolStripMenuItem)sender;

				((IAction)_hashMenuActions[ miSender ]).Execute( _contextprovider.GetContext( ActionContextKind.MainMenu ) );
			}

			private void OnTopLevelMenuItemPopup(object sender, EventArgs e)
			{
				UpdateActions();
			}
		}
		#endregion

		#region EncodingMenuItem Class — MenuItems that have a charset associated to them.

		internal class EncodingMenuItem : ToolStripMenuItem
		{
			/// <summary>
			/// The charset this item serves.
			/// </summary>
			protected CharsetsEnum.Charset _charset;

			internal EncodingMenuItem( CharsetsEnum.Charset charset )
				: base( charset.Description ) // Send UI text to the menu item base
			{
				Guard.NullArgument( charset, "charset" );
				_charset = charset;
//				RadioCheck = true;
                CheckOnClick = true;
			}

			/// <summary>
			/// Gets the charset this item serves.
			/// </summary>
			internal CharsetsEnum.Charset Charset
			{
				get { return _charset; }
			}
		}

		#endregion
	}
}
