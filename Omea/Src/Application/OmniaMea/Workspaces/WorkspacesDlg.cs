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
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.UI.Components.ImageListButton;

namespace JetBrains.Omea.Workspaces
{
	/// <summary>
	/// The dialog for configuring workspaces.
	/// </summary>
	public class WorkspacesDlg : DialogBase
	{
        private ResourceListDataProvider dataProvider;
        private SortSettings listSorting;
        private Panel _workspaceListPane;

		private Splitter splitter1;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		private WorkspaceManager _workspaceManager;

		private IResource _currentWorkspace;

		private WorkspaceTabData _currentTabData;

		private bool _tabsInitialized = false;

		private Label label1;

        private ResourceListView2 _workspaceListView;
        private ImageListButton _btnMoveUp, _btnMoveDown;

		private Button _btnNewWorkspace;
		private Button _btnRename;
		private Button _btnDeleteWorkspace;

		private Panel _workspaceRightPane;

		private Panel _buttonPane;

		private Button _btnClose;

		private ContextMenu _workspaceListContextMenu;

		private MenuItem miRenameWorkspace;

		private TabControl _workspaceTabControl;

		private Label _lblResourcesInWorkspace;

		private Button _btnHelp;


		private Label _colorLabel;

		/// <summary>
		/// A bar that provides for picking the basic workspace colors.
		/// </summary>
		private ColorPickerBar _panelColorPickerBar;

		/// <summary>
		/// A color swatch that displays the color of the currently-selected workspace.
		/// </summary>
		private ColorPreview _panelColorPreview;

		private MenuItem miDeleteWorkspace;

		/// <summary>
		/// Button that calls the system color picker that allows to choose from a larger set of colors.
		/// </summary>
		private Button _btnMoreColors;

		#region Class WorkspaceTabData

		private class WorkspaceTabData
		{
			internal string[] _resTypes;

			internal string[] _otherViewTypes;

			internal int _otherViewLinkProp;

			internal TabPage _tabPage;

			internal IWorkspaceSelector _selector;

			internal Label _lblOther;
			internal ResourceListView2 _lvOther;
            internal IResourceList     _listOther;

			internal Button _btnRemoveOther;

			internal IResource _lastWorkspace;

			internal bool _populated;

			internal void SetWorkspace( IResource workspace )
			{
				if( _lastWorkspace != workspace )
				{
					_lastWorkspace = workspace;
					if( _selector != null )
					{
						_selector.SetWorkspace( workspace );
					}
					if( _lvOther != null )
					{
                        _listOther = Core.ResourceStore.EmptyResourceList;
						if( workspace != null )
						{
							IResourceList otherResources = null;
							if( _otherViewTypes != null )
							{
								foreach( string resType in _otherViewTypes )
								{
									otherResources = Core.WorkspaceManager.GetWorkspaceResourcesLive( workspace, resType ).Union( otherResources );

									IResourceList fragments = Core.WorkspaceManager.GetWorkspaceResourcesLive( workspace, "Fragment" );
									fragments = fragments.Intersect( Core.ResourceStore.FindResourcesLive( "Fragment", "ContentType", resType ), true );
									otherResources = otherResources.Union( fragments );
								}
							}
							if( _otherViewLinkProp > 0 )
							{
								IResourceList linkResources = Core.ResourceStore.FindResourcesWithPropLive( null, _otherViewLinkProp );
								linkResources = linkResources.Intersect( Core.WorkspaceManager.GetWorkspaceResources( workspace, null ), true );
								linkResources = linkResources.Minus( Core.ResourceStore.GetAllResourcesLive( _resTypes[ 0 ] ) );
								otherResources = linkResources.Union( otherResources );
							}
							_listOther = otherResources;
							_btnRemoveOther.Enabled = (otherResources.Count > 0);
                            foreach( IResource res in _listOther )
                                _lvOther.JetListView.Nodes.Add( res );
						}
					}
				}
			}
		}

		#endregion

		public WorkspacesDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			_workspaceListView.AllowDrop = false;
            _workspaceListView.AllowDrag = false;

			RestoreSettings();

			_workspaceManager = Core.WorkspaceManager as WorkspaceManager;
			_workspaceListView.AddIconColumn();
			JetListViewColumn nameColumn = new JetListViewColumn();
			nameColumn.SizeToContent = true;
			_workspaceListView.Columns.Add( nameColumn );

            IResourceList wsps = _workspaceManager.GetAllWorkspaces();

            //  Setup initial sorting
            int propId = Core.ResourceStore.PropTypes[ "Order" ].Id;
            int visSortId = ((WorkspaceManager) (Core.WorkspaceManager)).Props.VisibleOrder;
		    listSorting = new SortSettings( propId, true );
            for( int i = 0; i < wsps.Count; i++ )
            {
                int currOrder = wsps[ i ].GetIntProp( propId );
                string name = wsps[ i ].GetStringProp( Core.Props.Name );
                Trace.WriteLine( currOrder + name );

                int visOrder = wsps[ i ].GetIntProp( visSortId );
                new ResourceProxy( wsps[ i ] ).SetProp( propId, visOrder );
            }
			dataProvider = new ResourceListDataProvider( wsps );
            dataProvider.SetInitialSort( listSorting );

			_workspaceListView.DataProvider = dataProvider;
			if( _workspaceManager.ActiveWorkspace != null )
			{
				_workspaceListView.Selection.Add( _workspaceManager.ActiveWorkspace );
			}
			else if( dataProvider.ResourceList.Count > 0 )
			{
				_workspaceListView.Selection.MoveDown();
			}

            _btnMoveUp.Enabled = false;
            _btnMoveDown.Enabled = (_workspaceListView.Selection.Count > 0) && (wsps.Count > 1);
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
		    Assembly thisOne = Assembly.GetExecutingAssembly();

			this._workspaceListPane = new System.Windows.Forms.Panel();
			this._btnNewWorkspace = new System.Windows.Forms.Button();
			this._btnRename = new System.Windows.Forms.Button();
			this._btnDeleteWorkspace = new System.Windows.Forms.Button();
			this._workspaceListView = new JetBrains.Omea.GUIControls.ResourceListView2();
            this._btnMoveUp = new ImageListButton();
            this._btnMoveDown = new ImageListButton();
			this._workspaceListContextMenu = new System.Windows.Forms.ContextMenu();
			this.miRenameWorkspace = new System.Windows.Forms.MenuItem();
			this.miDeleteWorkspace = new System.Windows.Forms.MenuItem();
			this.label1 = new System.Windows.Forms.Label();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this._workspaceRightPane = new System.Windows.Forms.Panel();
			this._panelColorPreview = new ColorPreview();
			_btnMoreColors = new Button();
			this._panelColorPickerBar = new ColorPickerBar();
			this._colorLabel = new System.Windows.Forms.Label();
			this._lblResourcesInWorkspace = new System.Windows.Forms.Label();
			this._workspaceTabControl = new System.Windows.Forms.TabControl();
			this._buttonPane = new System.Windows.Forms.Panel();
			this._btnClose = new System.Windows.Forms.Button();
			this._btnHelp = new System.Windows.Forms.Button();
			this._workspaceListPane.SuspendLayout();
			this._workspaceRightPane.SuspendLayout();
			this._buttonPane.SuspendLayout();
			this.SuspendLayout();
			// 
			// _workspaceListPane
			// 
			this._workspaceListPane.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this._workspaceListPane.Controls.Add( this._btnMoveUp );
			this._workspaceListPane.Controls.Add( this._btnMoveDown );
			this._workspaceListPane.Controls.Add( this._btnNewWorkspace );
			this._workspaceListPane.Controls.Add( this._btnRename );
			this._workspaceListPane.Controls.Add( this._btnDeleteWorkspace );
			this._workspaceListPane.Controls.Add( this._workspaceListView );
			this._workspaceListPane.Controls.Add( this.label1 );
			this._workspaceListPane.Dock = System.Windows.Forms.DockStyle.Left;
			this._workspaceListPane.Location = new System.Drawing.Point( 0, 0 );
			this._workspaceListPane.Name = "_workspaceListPane";
			this._workspaceListPane.Size = new System.Drawing.Size( 208, 327 );
			this._workspaceListPane.TabIndex = 0;
			// 
			// _btnMoveUp 
			// 
			this._btnMoveUp.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._btnMoveUp.Location = new System.Drawing.Point( 185, 28 );
			this._btnMoveUp.Name = "_btnMoveUp";
			this._btnMoveUp.Size = new System.Drawing.Size( 25, 20 );
			this._btnMoveUp.TabIndex = 2;
			this._btnMoveUp.Enabled = false;
			this._btnMoveUp.Click += new EventHandler(_btnMoveUp_Click);

            Stream stream = thisOne.GetManifestResourceStream( "OmniaMea.Icons.MoveUp.ico" );
            this._btnMoveUp.AddIcon( new Icon( stream ), ImageListButton.ButtonState.Normal );
		    stream = thisOne.GetManifestResourceStream( "OmniaMea.Icons.MoveUpDisabled.ico" );
            this._btnMoveUp.AddIcon( new Icon( stream ), ImageListButton.ButtonState.Disabled );
		    stream = thisOne.GetManifestResourceStream( "OmniaMea.Icons.MoveUpHot.ico" );
            this._btnMoveUp.AddIcon( new Icon( stream ), ImageListButton.ButtonState.Hot );
			// 
			// _btnMoveDown
			// 
			this._btnMoveDown.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._btnMoveDown.Location = new System.Drawing.Point( 185, 50 );
			this._btnMoveDown.Name = "_btnNewWorkspace";
			this._btnMoveDown.Size = new System.Drawing.Size( 25, 20 );
			this._btnMoveDown.TabIndex = 3;
			this._btnMoveDown.Click += new EventHandler(_btnMoveDown_Click);

		    stream = thisOne.GetManifestResourceStream( "OmniaMea.Icons.MoveDown.ico" );
            this._btnMoveDown.AddIcon( new Icon( stream ), ImageListButton.ButtonState.Normal );
		    stream = thisOne.GetManifestResourceStream( "OmniaMea.Icons.MoveDownDisabled.ico" );
            this._btnMoveDown.AddIcon( new Icon( stream ), ImageListButton.ButtonState.Disabled );
		    stream = thisOne.GetManifestResourceStream( "OmniaMea.Icons.MoveDownHot.ico" );
            this._btnMoveDown.AddIcon( new Icon( stream ), ImageListButton.ButtonState.Hot );
			// 
			// _btnNewWorkspace
			// 
			this._btnNewWorkspace.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._btnNewWorkspace.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnNewWorkspace.Location = new System.Drawing.Point( 4, 292 );
			this._btnNewWorkspace.Name = "_btnNewWorkspace";
			this._btnNewWorkspace.Size = new System.Drawing.Size( 60, 23 );
			this._btnNewWorkspace.TabIndex = 4;
			this._btnNewWorkspace.Text = "&New...";
			this._btnNewWorkspace.Click += new System.EventHandler( this._btnNewWorkspace_Click );
			// 
			// _btnRename
			// 
			this._btnRename.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._btnRename.Enabled = false;
			this._btnRename.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnRename.Location = new System.Drawing.Point( 72, 292 );
			this._btnRename.Name = "_btnRename";
			this._btnRename.Size = new System.Drawing.Size( 60, 23 );
			this._btnRename.TabIndex = 5;
			this._btnRename.Text = "&Rename";
			this._btnRename.Click += new System.EventHandler( this._btnRename_Click );
			// 
			// _btnDeleteWorkspace
			// 
			this._btnDeleteWorkspace.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._btnDeleteWorkspace.Enabled = false;
			this._btnDeleteWorkspace.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnDeleteWorkspace.Location = new System.Drawing.Point( 140, 292 );
			this._btnDeleteWorkspace.Name = "_btnDeleteWorkspace";
			this._btnDeleteWorkspace.Size = new System.Drawing.Size( 60, 23 );
			this._btnDeleteWorkspace.TabIndex = 6;
			this._btnDeleteWorkspace.Text = "&Delete";
			this._btnDeleteWorkspace.Click += new System.EventHandler( this._btnDeleteWorkspace_Click );
			// 
			// _workspaceListView
			// 
			this._workspaceListView.AllowColumnReorder = false;
			this._workspaceListView.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._workspaceListView.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this._workspaceListView.ColumnSchemeProvider = null;
			this._workspaceListView.ContextMenu = this._workspaceListContextMenu;
			this._workspaceListView.ContextProvider = this._workspaceListView;
			this._workspaceListView.DataProvider = null;
			this._workspaceListView.FullRowSelect = true;
			this._workspaceListView.HeaderContextMenu = null;
			this._workspaceListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this._workspaceListView.InPlaceEdit = true;
			this._workspaceListView.Location = new System.Drawing.Point( 4, 28 );
			this._workspaceListView.MultiLineView = false;
			this._workspaceListView.Name = "_workspaceListView";
			this._workspaceListView.OpenProperty = -1;
			this._workspaceListView.RowDelimiters = false;
			this._workspaceListView.ShowContextMenu = false;
			this._workspaceListView.Size = new System.Drawing.Size( 175, 256 );
			this._workspaceListView.TabIndex = 1;
			this._workspaceListView.KeyDown += new System.Windows.Forms.KeyEventHandler( this._workspaceListView_KeyDown );
			this._workspaceListView.ActiveResourceChanged += new System.EventHandler( this.HandleSelectedWorkspaceChanged );
			this._workspaceListView.AfterItemEdit += new ResourceItemEditEventHandler( _workspaceListView_AfterLabelEdit );
			// 
			// _workspaceListContextMenu
			// 
			this._workspaceListContextMenu.MenuItems.AddRange( new System.Windows.Forms.MenuItem[]
				{
					this.miRenameWorkspace,
					this.miDeleteWorkspace
				} );
			this._workspaceListContextMenu.Popup += new System.EventHandler( this._workspaceListContextMenu_Popup );
			// 
			// miRenameWorkspace
			// 
			this.miRenameWorkspace.Index = 0;
			this.miRenameWorkspace.Text = "&Rename";
			this.miRenameWorkspace.Click += new System.EventHandler( this.miRenameWorkspace_Click );
			// 
			// miDeleteWorkspace
			// 
			this.miDeleteWorkspace.Index = 1;
			this.miDeleteWorkspace.Text = "&Delete";
			this.miDeleteWorkspace.Click += new System.EventHandler( this.miDeleteWorkspace_Click );
			// 
			// label1
			// 
			this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label1.Font = new System.Drawing.Font( "Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte) (204)) );
			this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
			this.label1.Location = new System.Drawing.Point( 4, 4 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 164, 16 );
			this.label1.TabIndex = 0;
			this.label1.Text = "Workspaces";
			// 
			// splitter1
			// 
			this.splitter1.Location = new System.Drawing.Point( 208, 0 );
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size( 3, 327 );
			this.splitter1.TabIndex = 1;
			this.splitter1.TabStop = false;
			// 
			// _workspaceRightPane
			// 
			this._workspaceRightPane.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this._workspaceRightPane.Controls.Add( this._panelColorPreview );
			this._workspaceRightPane.Controls.Add( this._panelColorPickerBar );
			this._workspaceRightPane.Controls.Add( _btnMoreColors );
			this._workspaceRightPane.Controls.Add( this._colorLabel );
			this._workspaceRightPane.Controls.Add( this._lblResourcesInWorkspace );
			this._workspaceRightPane.Controls.Add( this._workspaceTabControl );
			this._workspaceRightPane.Dock = System.Windows.Forms.DockStyle.Fill;
			this._workspaceRightPane.Location = new System.Drawing.Point( 211, 0 );
			this._workspaceRightPane.Name = "_workspaceRightPane";
			this._workspaceRightPane.Size = new System.Drawing.Size( 389, 327 );
			this._workspaceRightPane.TabIndex = 1;
			// 
			// _panelColorPreview
			// 
			this._panelColorPreview.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._panelColorPreview.Location = new System.Drawing.Point( 68, 292 );
			this._panelColorPreview.Name = "_panelColorPreview";
			this._panelColorPreview.TabIndex = 4;
			this._panelColorPreview.Enabled = false;
			// 
			// _panelColorPickerBar
			// 
			this._panelColorPickerBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			this._panelColorPickerBar.Location = new System.Drawing.Point( 100, 292 );
			this._panelColorPickerBar.Name = "_panelColorPickerBar";
			this._panelColorPickerBar.Size = new System.Drawing.Size( 200, 20 );
			this._panelColorPickerBar.TabIndex = 3;
			this._panelColorPickerBar.Enabled = false;
			_panelColorPickerBar.ColorPicked += new JetBrains.Omea.Workspaces.WorkspacesDlg.ColorPickerBar.ColorPickedEventHandler( OnColorPickerBarColorPicked );
			//
			// _btnMoreColors
			//
			_btnMoreColors.Name = "_btnMoreColors";
			_btnMoreColors.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			_btnMoreColors.Text = "Other…";
			_btnMoreColors.Click += new EventHandler( OnMoreColorsClick );
			_btnMoreColors.Size = new Size( 72, 24 );
			_btnMoreColors.Location = new Point( 310, 292 );
			_btnMoreColors.FlatStyle = FlatStyle.System;
			_btnMoreColors.Enabled = false;
			// 
			// _colorLabel
			// 
			this._colorLabel.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._colorLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._colorLabel.Location = new System.Drawing.Point( 8, 296 );
			this._colorLabel.Name = "_colorLabel";
			this._colorLabel.Size = new System.Drawing.Size( 64, 16 );
			this._colorLabel.TabIndex = 2;
			this._colorLabel.Text = "Color:";
			this._colorLabel.Visible = false;
			// 
			// _lblResourcesInWorkspace
			// 
			this._lblResourcesInWorkspace.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._lblResourcesInWorkspace.Font = new System.Drawing.Font( "Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte) (204)) );
			this._lblResourcesInWorkspace.Location = new System.Drawing.Point( 4, 4 );
			this._lblResourcesInWorkspace.Name = "_lblResourcesInWorkspace";
			this._lblResourcesInWorkspace.Size = new System.Drawing.Size( 404, 16 );
			this._lblResourcesInWorkspace.TabIndex = 1;
			// 
			// _workspaceTabControl
			// 
			this._workspaceTabControl.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._workspaceTabControl.Location = new System.Drawing.Point( 4, 20 );
			this._workspaceTabControl.Name = "_workspaceTabControl";
			this._workspaceTabControl.SelectedIndex = 0;
			this._workspaceTabControl.Size = new System.Drawing.Size( 380, 264 );
			this._workspaceTabControl.TabIndex = 0;
			this._workspaceTabControl.Visible = false;
			this._workspaceTabControl.SizeChanged += new System.EventHandler( this._workspaceTabControl_SizeChanged );
			this._workspaceTabControl.SelectedIndexChanged += new System.EventHandler( this._workspaceTabControl_SelectedIndexChanged );
			// 
			// _buttonPane
			// 
			this._buttonPane.Controls.Add( this._btnClose );
			this._buttonPane.Controls.Add( this._btnHelp );
			this._buttonPane.Dock = System.Windows.Forms.DockStyle.Bottom;
			this._buttonPane.Location = new System.Drawing.Point( 0, 327 );
			this._buttonPane.Name = "_buttonPane";
			this._buttonPane.Size = new System.Drawing.Size( 600, 36 );
			this._buttonPane.TabIndex = 1;
			// 
			// _btnClose
			// 
			this._btnClose.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this._btnClose.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnClose.Location = new System.Drawing.Point( 436, 8 );
			this._btnClose.Name = "_btnClose";
			this._btnClose.TabIndex = 2;
			this._btnClose.Text = "Close";
            this._btnClose.Click += new EventHandler(_btnClose_Click);
			// 
			// _btnHelp
			// 
			this._btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnHelp.Location = new System.Drawing.Point( 520, 8 );
			this._btnHelp.Name = "_btnHelp";
			this._btnHelp.TabIndex = 3;
			this._btnHelp.Text = "Help";
			this._btnHelp.Click += new System.EventHandler( this._btnHelp_Click );
			// 
			// WorkspacesDlg
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size( 5, 14 );
			this.CancelButton = this._btnClose;
			this.ClientSize = new System.Drawing.Size( 600, 363 );
			this.Controls.Add( this._workspaceRightPane );
			this.Controls.Add( this.splitter1 );
			this.Controls.Add( this._workspaceListPane );
			this.Controls.Add( this._buttonPane );
			this.Name = "WorkspacesDlg";
			this.Text = "Workspaces";
			this.VisibleChanged += new System.EventHandler( this.WorkspacesDlg_VisibleChanged );
			this.HelpRequested += new System.Windows.Forms.HelpEventHandler( this.WorkspacesDlg_HelpRequested );
			this._workspaceListPane.ResumeLayout( false );
			this._workspaceRightPane.ResumeLayout( false );
			this._buttonPane.ResumeLayout( false );
			this.ResumeLayout( false );

		}

		#endregion

		/**
         * Sets the selection in the workspace list to the specified workspace.
         */

		public void SelectWorkspace( IResource workspace )
		{
			_workspaceListView.Selection.Clear();
			_workspaceListView.Selection.Add( workspace );
		}

		private void _btnNewWorkspace_Click( object sender, EventArgs e )
		{
			string wsName = Core.UIManager.InputString( "New Workspace",
			                                            "Please enter the workspace name:", "", new ValidateStringDelegate( ValidateDelegate ),
			                                            this, 0, "/organizing/organizing_using_workspaces.html#new" );
			if( wsName != null )
			{
				IResource workspace = _workspaceManager.CreateWorkspace( wsName );
				SelectWorkspace( workspace );
				_workspaceListView.Focus();
			}
		}

		private void ValidateDelegate( string value, ref string validateErrorMessage )
		{
			if( Core.ResourceStore.FindResources( "Workspace", "Name", value ).Count > 0 )
			{
				validateErrorMessage = "A workspace with that name already exists";
			}
		}

		private void HandleSelectedWorkspaceChanged( object sender, EventArgs e )
		{
			IResourceList selection = _workspaceListView.GetSelectedResources();
			if( selection.Count > 0 )
			{
				_currentWorkspace = selection[ 0 ];
				_btnDeleteWorkspace.Enabled = true;
				_btnRename.Enabled = true;

				if( !_tabsInitialized )
				{
					InitializeResourceTypeTabs();
				}
				_lblResourcesInWorkspace.Text = "Resources in Workspace '" + _currentWorkspace.DisplayName + "'";
				_workspaceTabControl.Visible = true;
				_panelColorPickerBar.Enabled = true;
				_panelColorPreview.Enabled = true;
				_btnMoreColors.Enabled = true;
				_colorLabel.Visible = true;
				_panelColorPreview.Invalidate();
			}
			else
			{
				_currentWorkspace = null;
				_btnDeleteWorkspace.Enabled = false;
				_btnRename.Enabled = false;
				_workspaceTabControl.Visible = false;
				_lblResourcesInWorkspace.Text = "";
				_panelColorPickerBar.Enabled = false;
				_panelColorPreview.Enabled = false;
				_btnMoreColors.Enabled = false;
				_colorLabel.Visible = false;
			}

            CheckButtonsState();

			ShowCurrentWorkspaceDetails();
		}

		private void InitializeResourceTypeTabs()
		{
			ArrayList otherTypes = new ArrayList();
			ArrayList tabResTypeList = new ArrayList();
			bool firstTab = true;

			HashSet processedTypes = new HashSet();

			IResourceNodeFilter availTreeFilter = null;
			IResourceNodeFilter inWorkspaceTreeFilter = null;

			for( int i = 1; i < Core.TabManager.Tabs.Count; i++ )
			{
				availTreeFilter = null;
				inWorkspaceTreeFilter = null;
				string tabId = Core.TabManager.Tabs[ i ].Id;
				string[] resTypes = Core.TabManager.Tabs[ i ].GetResourceTypes();
				bool haveContainerType = false;

				otherTypes.Clear();
				tabResTypeList.Clear();
				foreach( string resType in resTypes )
				{
					WorkspaceResourceType wrType = _workspaceManager.GetWorkspaceResourceType( resType );
					if( wrType == WorkspaceResourceType.None )
					{
						otherTypes.Add( resType );
						processedTypes.Add( resType );
					}
					else if( wrType == WorkspaceResourceType.Folder )
					{
						tabResTypeList.Add( resType );
						processedTypes.Add( resType );
					}
					else
					{
						if( !haveContainerType )
						{
							haveContainerType = true;
							tabResTypeList.Add( resType );
							if( availTreeFilter == null )
							{
								availTreeFilter = _workspaceManager.GetAvailSelectorFilter( resType );
								inWorkspaceTreeFilter = _workspaceManager.GetInWorkspaceSelectorFilter( resType );
							}
							processedTypes.Add( resType );
						}
					}
				}
				string[] tabResTypes = (string[]) tabResTypeList.ToArray( typeof( string ) );

				WorkspaceTabData tabData = CreateResourceTypeTab( Core.TabManager.Tabs[ tabId ].Name,
				                                                  tabResTypes );
				if( otherTypes.Count > 0 )
				{
					tabData._otherViewTypes = (string[]) otherTypes.ToArray( typeof( string ) );
				}
				tabData._otherViewLinkProp = Core.TabManager.Tabs[ i ].LinkPropId;

				if( haveContainerType )
				{
					string structurePaneId = Core.LeftSidebar.GetResourceStructurePaneId( tabId );
					if( structurePaneId != null )
					{
						AbstractViewPane viewPane = Core.LeftSidebar.GetPane( tabId, structurePaneId );
						if( viewPane is JetResourceTreePane )
						{
							JetResourceTreePane treePane = viewPane as JetResourceTreePane;
							string rootResType = treePane.RootResource.GetStringProp( "RootResourceType" );
							if( rootResType != null &&
								_workspaceManager.GetWorkspaceResourceType( rootResType ) != WorkspaceResourceType.None )
							{
								tabData._selector = new WorkspaceTreeSelector( tabResTypes,
								                                               treePane.RootResource, treePane.ParentProperty, availTreeFilter,
								                                               inWorkspaceTreeFilter );
							}
						}
					}
					if( tabData._selector == null )
					{
						tabData._selector = new WorkspaceListSelector( tabResTypes );
					}
				}

				if( firstTab )
				{
					firstTab = false;
					PopulateTab( tabData );
					tabData._populated = true;
					_currentTabData = tabData;
				}
			}

			for( int i = 0; i < _workspaceManager.WorkspaceTypeCount; i++ )
			{
				string wsType = _workspaceManager.GetWorkspaceType( i );
				if( !processedTypes.Contains( wsType ) )
				{
					string tabName = _workspaceManager.GetWorkspaceTabName(  wsType );
                    if ( tabName == null )
                    {
                        continue;
                    }
                    ArrayList resTypeList = new ArrayList();
                    resTypeList.Add( wsType );
                    for( int j=i+1; j<_workspaceManager.WorkspaceTypeCount; j++ )
                    {
                        string wsType2 = _workspaceManager.GetWorkspaceType( j );
                        if ( !processedTypes.Contains( wsType2 ) && _workspaceManager.GetWorkspaceTabName( wsType2 ) == tabName )
                        {
                            resTypeList.Add( wsType2 );
                            processedTypes.Add( wsType2 );
                        }
                    }

                    string[] resTypes = (string[]) resTypeList.ToArray( typeof(string) );
					WorkspaceTabData tabData = CreateResourceTypeTab( tabName, resTypes );

					// HACK!!!
					IResourceSelectPane selPane = Core.UIManager.CreateResourceSelectPane( wsType );
					ResourceTreeSelectPane treeSelPane = selPane as ResourceTreeSelectPane;
					if( treeSelPane != null )
					{
						tabData._selector = new WorkspaceTreeSelector( resTypes,
						                                               treeSelPane.GetSelectorRoot( wsType ), Core.Props.Parent,
						                                               _workspaceManager.GetAvailSelectorFilter( wsType ),
						                                               _workspaceManager.GetInWorkspaceSelectorFilter( wsType ) );
					}
					else
					{
						tabData._selector = new WorkspaceListSelector( resTypes );
					}
				}
			}

			_tabsInitialized = true;
		}

		private WorkspaceTabData CreateResourceTypeTab( string name, string[] resTypes )
		{
			WorkspaceTabData tabData = new WorkspaceTabData();
			tabData._resTypes = resTypes;

			TabPage tabPage = new TabPage();
			tabData._tabPage = tabPage;
			tabPage.Text = name;
			tabPage.Tag = tabData;

			_workspaceTabControl.TabPages.Add( tabPage );
			return tabData;
		}

		private void _workspaceTabControl_SelectedIndexChanged( object sender, EventArgs e )
		{
			WorkspaceTabData tabData = (WorkspaceTabData) _workspaceTabControl.SelectedTab.Tag;
			_currentTabData = tabData;
			if( !tabData._populated )
			{
				PopulateTab( tabData );
				tabData._populated = true;
			}
			else
			{
				UpdateControlSizes( tabData );
				tabData.SetWorkspace( _currentWorkspace );
			}
		}

		/// <summary>
		/// When the selected workspace changes, displays all the data related to the current workspace.
		/// </summary>
		private void ShowCurrentWorkspaceDetails()
		{
			if( _currentTabData != null )
				_currentTabData.SetWorkspace( _currentWorkspace );
			_panelColorPreview.Color = new WorkspaceUIManager( _currentWorkspace ).WorkspaceColor;
		}

		private void PopulateTab( WorkspaceTabData tabData )
		{
			TabPage tabPage = tabData._tabPage;

			if( tabData._selector != null )
			{
				tabData._selector.CreateComponents();
			}

			if( tabData._otherViewTypes != null || tabData._otherViewLinkProp > 0 )
			{
				Label lblOther = new Label();
				lblOther.Text = "Other:";
				lblOther.AutoSize = true;
				lblOther.FlatStyle = FlatStyle.System;
				tabData._lblOther = lblOther;

				ResourceListView2 lvOther = new ResourceListView2();
				lvOther.HeaderStyle = ColumnHeaderStyle.None;
                lvOther.AddColumn( ResourceProps.DisplayName ).AutoSize = true;
				lvOther.Tag = tabData;
				lvOther.EmptyText = "No individual resources in this workspace. To add a resource, drop it on the workspace button.";
				lvOther.SelectionChanged += new EventHandler( OnOtherListSelectionChanged );

				tabData._lvOther = lvOther;

				tabData._btnRemoveOther = CreateTabButton( tabData, "Remove", new EventHandler( OnRemoveOtherClick ) );
			}

			tabData.SetWorkspace( _currentWorkspace );
			UpdateControlSizes( tabData );

			if( tabData._selector != null )
			{
				tabPage.Controls.Add( tabData._selector.GetControl() );
			}

			if( tabData._lvOther != null )
			{
				tabPage.Controls.Add( tabData._lblOther );
				tabPage.Controls.Add( tabData._lvOther );
				tabPage.Controls.Add( tabData._btnRemoveOther );
			}
		}

		private static Button CreateTabButton( WorkspaceTabData tabData, string name, EventHandler clickHandler )
		{
			Button btn = new Button();
			btn.Text = name;
			btn.FlatStyle = FlatStyle.System;
			btn.Size = new Size( 72, 24 );
			btn.Click += clickHandler;
			btn.Tag = tabData;
			return btn;
		}

		private void UpdateControlSizes( WorkspaceTabData tabData )
		{
			Size tabSize = new Size( _workspaceTabControl.Width - 12, _workspaceTabControl.Height - 24 );

			int lvOtherHeight = (tabData._lvOther != null) ? 120 : 0;

			Size selectPaneSize = new Size( (tabSize.Width - 100) / 2, tabSize.Height - 8 - lvOtherHeight );

			if( tabData._selector != null )
			{
				tabData._selector.GetControl().Location = new Point( 4, 4 );
				tabData._selector.GetControl().Size = new Size( tabSize.Width - 4, selectPaneSize.Height );
			}
			else
			{
				selectPaneSize = new Size( selectPaneSize.Width, 0 );
			}

			if( tabData._lvOther != null )
			{
				tabData._lblOther.Location = new Point( 4, selectPaneSize.Height + 8 );

				tabData._lvOther.Location = new Point( 64, selectPaneSize.Height + 8 );
				tabData._lvOther.Size = new Size( (int) (tabSize.Width -
					(64 + 80) * Core.ScaleFactor.Width), 112 );

				tabData._btnRemoveOther.Location = new Point( tabData._lvOther.Right + 8, tabData._lvOther.Top );
				tabData._btnRemoveOther.Size = new Size( (int) (72 * Core.ScaleFactor.Width),
				                                         (int) (24 * Core.ScaleFactor.Height) );
			}
		}

		private void _workspaceTabControl_SizeChanged( object sender, EventArgs e )
		{
			if( _currentTabData != null )
			{
				UpdateControlSizes( _currentTabData );
			}
		}

		private void OnOtherListSelectionChanged( object sender, EventArgs e )
		{
			Control senderCtl = (Control) sender;
			WorkspaceTabData tabData = (WorkspaceTabData) senderCtl.Tag;
			tabData._btnRemoveOther.Enabled = tabData._lvOther.GetSelectedResources().Count > 0;
		}

		private void OnRemoveOtherClick( object sender, EventArgs e )
		{
            IResourceList selected = _currentTabData._lvOther.GetSelectedResources();
			_workspaceManager.RemoveResourcesFromWorkspace( _currentWorkspace, selected );

            _currentTabData._listOther = _currentTabData._listOther.Minus( selected );
            foreach( IResource res in selected )
                _currentTabData._lvOther.JetListView.Nodes.Remove( res );

		}

		private void _btnDeleteWorkspace_Click( object sender, EventArgs e )
		{
			CheckDeleteCurrentWorkspace();
		}

		private void _workspaceListView_KeyDown( object sender, KeyEventArgs e )
		{
			if( e.KeyData == Keys.Delete )
			{
				if( _currentWorkspace != null )
				{
					CheckDeleteCurrentWorkspace();
					_workspaceListView.Focus();
				}
			}
		}

		/**
         * Prompts the user to delete the current workspace.
         */

		private void CheckDeleteCurrentWorkspace()
		{
			int selCount = _workspaceListView.Selection.Count;
			if( selCount == 1 )
			{
				DialogResult result = MessageBox.Show( this,
				                                       "Are you sure you want to delete the workspace '" + _currentWorkspace.DisplayName +
				                                       	"'?\nThis operation cannot be undone.",
				                                       "Delete Workspace", MessageBoxButtons.YesNo, MessageBoxIcon.Question );

				if( result == DialogResult.Yes )
				{
					_workspaceManager.DeleteWorkspace( _currentWorkspace );
				}
			}
			else if( selCount > 1 )
			{
				DialogResult result = MessageBox.Show( this,
				                                       "Are you sure you want to delete the " + _workspaceListView.Selection.Count +
				                                       	" selected workspaces?\nThis operation cannot be undone.",
				                                       "Delete Workspace", MessageBoxButtons.YesNo, MessageBoxIcon.Question );

				if( result == DialogResult.Yes )
				{
					IResourceList wsList = _workspaceListView.GetSelectedResources();
					foreach( IResource ws in wsList )
					{
						_workspaceManager.DeleteWorkspace( ws );
					}
				}
			}
		}

		/**
         * In-place rename for workspaces.
         */

		private void _workspaceListView_AfterLabelEdit( object sender, ResourceItemEditEventArgs e )
		{
			if( e.Text.Trim() == "" )
			{
				MessageBox.Show( this, "Please enter the name for the workspace", "Workspaces" );
				e.CancelEdit = true;
				return;
			}

			IResourceList workspaces = Core.ResourceStore.FindResources( "Workspace",
			                                                             Core.Props.Name, e.Text );
			if( workspaces.Count > 0 )
			{
				MessageBox.Show( this,
				                 "A workspace named '" + e.Text + " ' already exists", "Workspaces" );
				e.CancelEdit = true;
				return;
			}

			new ResourceProxy( e.Resource ).SetProp( Core.Props.Name, e.Text );
		}

		private void WorkspacesDlg_VisibleChanged( object sender, EventArgs e )
		{
			_workspaceListView.Select();
		}

		private void miRenameWorkspace_Click( object sender, EventArgs e )
		{
			if( _workspaceListView.Selection.Count == 1 )
			{
				_workspaceListView.EditResourceLabel( (IResource) _workspaceListView.Selection[ 0 ] );
			}
		}

		private void _btnRename_Click( object sender, EventArgs e )
		{
			Core.UIManager.QueueUIJob( new MethodInvoker( RenameSelectedWorkspace ) );
		}

		private void RenameSelectedWorkspace()
		{
			if( _workspaceListView.Selection.Count > 0 )
			{
				_workspaceListView.Select();
				_workspaceListView.EditResourceLabel( (IResource) _workspaceListView.Selection[ 0 ] );
			}
		}

		private void miDeleteWorkspace_Click( object sender, EventArgs e )
		{
			CheckDeleteCurrentWorkspace();
		}

		private void _workspaceListContextMenu_Popup( object sender, EventArgs e )
		{
			miRenameWorkspace.Enabled =
				miDeleteWorkspace.Enabled = (_workspaceListView.Selection.Count > 0);
		}

		private void _btnHelp_Click( object sender, EventArgs e )
		{
			Help.ShowHelp( this, Core.UIManager.HelpFileName, "/reference/manage_workspaces.html" );
		}

		private void WorkspacesDlg_HelpRequested( object sender, HelpEventArgs hlpevent )
		{
			Help.ShowHelp( this, Core.UIManager.HelpFileName, "/reference/manage_workspaces.html" );
		}

		#region Class ColorPreview — A control that previews the selected color.

		/// <summary>
		/// A control that previews the selected color.
		/// </summary>
		internal class ColorPreview : UserControl
		{
			private ToolTip _tooltip;

			private Color _color = SystemColors.Window;

			internal ColorPreview()
			{
				Size = new Size( 20, 20 );
				SetStyle( ControlStyles.AllPaintingInWmPaint
					| ControlStyles.CacheText
					| ControlStyles.Opaque
					| ControlStyles.ResizeRedraw
					| ControlStyles.UserPaint
				          , true );
				SetStyle( ControlStyles.ContainerControl
					| ControlStyles.Selectable
				          , false );
				_tooltip = new ToolTip();
				SetTooltip();
			}

			internal Color Color
			{
				get { return _color; }
				set
				{
					_color = value;
					SetTooltip();
					Invalidate();
				}
			}

			/// <summary>
			/// Updates the tooltip.
			/// </summary>
			private void SetTooltip()
			{
				_tooltip.SetToolTip( this, String.Format( "({0}, {1}, {2})", _color.R, _color.G, _color.B ) );
			}

			protected override void OnLayout( LayoutEventArgs levent )
			{
			}

			protected override void OnPaint( PaintEventArgs e )
			{
				Rectangle client = ClientRectangle;

				// Border-LT
				e.Graphics.FillRectangle( SystemBrushes.ControlDark, Rectangle.FromLTRB( client.Left, client.Top, client.Right, client.Top + 1 ) ); // Top
				e.Graphics.FillRectangle( SystemBrushes.ControlDark, Rectangle.FromLTRB( client.Left, client.Top + 1, client.Left + 1, client.Bottom - 1 ) ); // Left
				// Border-RB
				e.Graphics.FillRectangle( SystemBrushes.ControlLightLight, Rectangle.FromLTRB( client.Left, client.Bottom - 1, client.Right, client.Bottom ) ); // Bottom
				e.Graphics.FillRectangle( SystemBrushes.ControlLightLight, Rectangle.FromLTRB( client.Right - 1, client.Top + 1, client.Right, client.Bottom - 1 ) ); // Right

				// Body
				if(Enabled)
				{
					using( Brush brush = new SolidBrush( _color ) )
						e.Graphics.FillRectangle( brush, Rectangle.FromLTRB( client.Left + 1, client.Top + 1, client.Right - 1, client.Bottom - 1 ) );
				}
				else
					e.Graphics.FillRectangle( SystemBrushes.Control, Rectangle.FromLTRB( client.Left + 1, client.Top + 1, client.Right - 1, client.Bottom - 1 ) );
			}
		}

		#endregion

		#region Class ColorPickerBar — A control that acts as a simple color picker.

		/// <summary>
		/// A control that acts as a simple color picker.
		/// </summary>
		internal class ColorPickerBar : UserControl
		{
			/// <summary>
			/// A bitmap that renders the picker's background.
			/// </summary>
			private Bitmap _bmpPicker;

			internal ColorPickerBar()
			{
				SetStyle( ControlStyles.AllPaintingInWmPaint
					| ControlStyles.CacheText
					| ControlStyles.Opaque
					| ControlStyles.ResizeRedraw
					| ControlStyles.UserPaint
				          , true );
				SetStyle( ControlStyles.ContainerControl
					| ControlStyles.Selectable
				          , false );

				// Prepare the background bitmap (add one pixel at the right and at the bottom to avoid mixing colors with the background color)
				_bmpPicker = new Bitmap( ColorManagement.MaxHLS + 1, 2 );
				for( int a = 0; a < ColorManagement.MaxHLS; a++ )
				{
					_bmpPicker.SetPixel( a, 0, ColorManagement.HLStoRGB( (ushort) a, WorkspaceUIManager.DefaultWorkspaceLuminocity, WorkspaceUIManager.DefaultWorkspaceSaturation ) );
					_bmpPicker.SetPixel( a, 1, ColorManagement.HLStoRGB( (ushort) a, WorkspaceUIManager.DefaultWorkspaceLuminocity, WorkspaceUIManager.DefaultWorkspaceSaturation ) );
				}
				_bmpPicker.SetPixel( ColorManagement.MaxHLS, 0, ColorManagement.HLStoRGB( (ushort) (ColorManagement.MaxHLS - 1), WorkspaceUIManager.DefaultWorkspaceLuminocity, WorkspaceUIManager.DefaultWorkspaceSaturation ) );
				_bmpPicker.SetPixel( ColorManagement.MaxHLS, 1, ColorManagement.HLStoRGB( (ushort) (ColorManagement.MaxHLS - 1), WorkspaceUIManager.DefaultWorkspaceLuminocity, WorkspaceUIManager.DefaultWorkspaceSaturation ) );
			}

			protected override void OnPaint( PaintEventArgs e )
			{
				Rectangle client = ClientRectangle;

				// Border-LT
				e.Graphics.FillRectangle( SystemBrushes.ControlDark, Rectangle.FromLTRB( client.Left, client.Top, client.Right, client.Top + 1 ) ); // Top
				e.Graphics.FillRectangle( SystemBrushes.ControlDark, Rectangle.FromLTRB( client.Left, client.Top + 1, client.Left + 1, client.Bottom - 1 ) ); // Left
				// Border-RB
				e.Graphics.FillRectangle( SystemBrushes.ControlLightLight, Rectangle.FromLTRB( client.Left, client.Bottom - 1, client.Right, client.Bottom ) ); // Bottom
				e.Graphics.FillRectangle( SystemBrushes.ControlLightLight, Rectangle.FromLTRB( client.Right - 1, client.Top + 1, client.Right, client.Bottom - 1 ) ); // Right

				// Body bitmap
				if( Enabled )
					e.Graphics.DrawImage( _bmpPicker, Rectangle.FromLTRB( client.Left + 1, client.Top + 1, client.Right - 1, client.Bottom - 1 ), new Rectangle( 0, 0, ColorManagement.MaxHLS, 1 ), GraphicsUnit.Pixel );
				else
					e.Graphics.FillRectangle( SystemBrushes.Control, Rectangle.FromLTRB( client.Left + 1, client.Top + 1, client.Right - 1, client.Bottom - 1 ) );
			}

			protected override void OnMouseDown( MouseEventArgs e )
			{
				if((Enabled) && ( e.Button == MouseButtons.Left ))
				{
					Capture = true; // Register the mouse button pressing
					AssignUndermouseColor( new Point( e.X, e.Y ) );
					//Cursor = Cursors.Cross;
				}
			}

			protected override void OnMouseMove( MouseEventArgs e )
			{
				// Apply the undermouse color, if the mouse button has been pressed within the control
				if( Capture )
					AssignUndermouseColor( new Point( e.X, e.Y ) );

				Cursor = Capture ? Cursors.Cross : Cursors.Hand;
			}

			protected override void OnMouseUp( MouseEventArgs e )
			{
				if( (Enabled) &&(e.Button == MouseButtons.Left) && (Capture) ) // If drag-selecting
				{
					Capture = false;
					AssignUndermouseColor( new Point( e.X, e.Y ) );
					Cursor = Cursors.Hand;
				}
			}

			/// <summary>
			/// Checks if the mouse is hovering a valid color now; if yes, reports selection of this color.
			/// </summary>
			/// <param name="point">Mouse position in client coordinates.</param>
			private void AssignUndermouseColor( Point point )
			{
				Rectangle picker = ClientRectangle;
				picker.Inflate( -1, -1 ); // Exclude the border

				if( !picker.Contains( point ) )
					return;

				int nHue = (point.X - picker.Left) * ColorManagement.MaxHLS / picker.Width;

				if( ColorPicked != null )
					ColorPicked( this, ColorManagement.HLStoRGB( (ushort) nHue, WorkspaceUIManager.DefaultWorkspaceLuminocity, WorkspaceUIManager.DefaultWorkspaceSaturation ) );
			}

			/// <summary>
			/// A color has been picked by its hue value.
			/// </summary>
			internal event ColorPickedEventHandler ColorPicked;

			internal delegate void ColorPickedEventHandler( ColorPickerBar sender, Color colorNew );
		}

		#endregion

		private void OnColorPickerBarColorPicked( ColorPickerBar sender, Color colorNew )
		{
			_panelColorPreview.Color = colorNew;
			if(_currentWorkspace != null)
				new WorkspaceUIManager( _currentWorkspace ).WorkspaceColor = colorNew;
		}

		private void OnMoreColorsClick( object sender, EventArgs e )
		{
			if(_currentWorkspace == null)
			{	// Cannot change the workspace color if there're no selected workspaces
				MessageBox.Show( this, "Please select an existing workspace from the list, or create a new one using the Add… button.", "Workspaces", MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
				return;
			}

			// Show the color picker
			using( ColorDialog picker = new ColorDialog() )
			{
				WorkspaceUIManager	uim = new WorkspaceUIManager( _currentWorkspace );

				// Take the current color value
				picker.Color = uim.WorkspaceColor;
				picker.CustomColors = new int[]{uim.WorkspaceColor.ToArgb()};

				// Show the picker dialog
				if( picker.ShowDialog( this ) == DialogResult.OK )
				{ // If accepted, apply the new value
					_panelColorPreview.Color = picker.Color;
					uim.WorkspaceColor = picker.Color;
				}
			}
        }

        #region Move Up/Move Down
        private void _btnMoveUp_Click(object sender, EventArgs e)
        {
            IResource currItem = _workspaceListView.GetSelectedResources()[ 0 ];
            IResource prevItem = GetNextItem( currItem, -1 );

            SwapOrders( currItem, prevItem );
            CheckButtonsState();
        }

        private void _btnMoveDown_Click(object sender, EventArgs e)
        {
            IResource currItem = _workspaceListView.GetSelectedResources()[ 0 ];
            IResource nextItem = GetNextItem( currItem, 1 );

            SwapOrders( currItem, nextItem );
            CheckButtonsState();
        }

        private IResource GetNextItem( IResource currItem, int shift )
        {
            IResourceList all = dataProvider.ResourceList;
            all.Sort( listSorting );
            return all[ all.IndexOf( currItem ) + shift ];
        }

        private delegate void VoidDelegate( IResource x, int order1, IResource y, int order2 );
        private void  SwapOrders( IResource x, IResource y )
        {
            _btnMoveUp.Enabled = _btnMoveDown.Enabled = false;
            Core.ResourceAP.RunJob( new VoidDelegate( SetProps ), x, x.GetIntProp( "Order" ),
                                                                  y, y.GetIntProp( "Order" ) );
            _workspaceListView.Selection.Clear();
            _workspaceListView.Selection.Add( x );
        }

        private void SetProps( IResource x, int order1, IResource y, int order2 )
        {
            y.SetProp( "Order", order1 );
            x.SetProp( "Order", order2 );
        }

        private void  CheckButtonsState()
        {
            int index = -1;
            bool validSel = (_workspaceListView.Selection.Count == 1);
            if( validSel )
            {
                IResource currItem = _workspaceListView.GetSelectedResources()[ 0 ];
                IResourceList all = dataProvider.ResourceList;
                all.Sort( listSorting );
                index = all.IndexOf( currItem );
            }

            _btnMoveUp.Enabled   = validSel && (index > 0);
            _btnMoveDown.Enabled = validSel && (index < dataProvider.ResourceList.Count - 1);
        }
        #endregion Move Up/Move Down

        private void _btnClose_Click(object sender, EventArgs e)
        {
            int visSortId = ((WorkspaceManager) (Core.WorkspaceManager)).Props.VisibleOrder;
            IResourceList wsps = _workspaceManager.GetAllWorkspaces();
            wsps.Sort( listSorting );
            for( int i = 0; i < wsps.Count; i++ )
            {
                new ResourceProxy( wsps[ i ] ).SetProp( visSortId, wsps[ i ].GetIntProp( "Order" ));
            }
            DialogResult = DialogResult.OK;
        }
    }

	public interface IWorkspaceSelector
	{
		Control GetControl();

		void CreateComponents();

		void SetWorkspace( IResource workspace );
	}
}