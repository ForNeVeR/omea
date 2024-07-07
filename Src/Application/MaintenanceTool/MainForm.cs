// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Ini;
using JetBrains.Omea.Base;
using JetBrains.Omea.Database;
using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.TextIndex;

namespace JetBrains.Omea.Maintenance
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
	{
        private System.Windows.Forms.ImageList _imageList;
        private System.Windows.Forms.StatusBar _statusBar;
        private System.Windows.Forms.TabControl _tabs;
        private System.Windows.Forms.TabPage _dbPage;
        private System.Windows.Forms.TabPage _textIndexPage;
        private System.Windows.Forms.ToolBar _toolBar;
        private System.Windows.Forms.ToolBarButton _diagnoseTablesButton;
        private System.Windows.Forms.ToolBarButton _refreshDbButton;
        private System.Windows.Forms.TabPage _performancePage;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.Panel _resultsPanel;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _resultsBox;
        private System.Windows.Forms.TextBox _pathBox;
        private System.Windows.Forms.ListView _tablesListView;
        private System.Windows.Forms.ColumnHeader _nameHeader;
        private System.Windows.Forms.ColumnHeader _recordsHeader;
        private System.Windows.Forms.ColumnHeader _fragmentationHeader;
        private System.Windows.Forms.ColumnHeader _occupiedSpaceHeader;
        private System.Windows.Forms.TrackBar _resourceCacheSizeTrackBar;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;

        private DatabaseProxy _dbProxy;
        private System.Windows.Forms.ToolBarButton _rebuildIndexesButton;
        private IniFile _ini;
        private string _lastRebuiltTable;

		public MainForm()
		{
			InitializeComponent();
            _pathBox.Text = MyPalStorage.DBPath;
            _dbProxy = new DatabaseProxy( MyPalStorage.DBPath );
            _dbProxy.Populate( _tablesListView );
            _ini = new IniFile( Path.Combine( OMEnv.WorkDir, "OmniaMea.ini" ) );
            _resourceCacheSizeTrackBar.Value = ( _ini.ReadInt( "ResourceStore", "ResourceCacheSize", 2048 ) + 1 ) >> 10;
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
                if( _dbProxy != null )
                {
                    _dbProxy.Dispose();
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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MainForm));
            this._imageList = new System.Windows.Forms.ImageList(this.components);
            this._statusBar = new System.Windows.Forms.StatusBar();
            this._tabs = new System.Windows.Forms.TabControl();
            this._dbPage = new System.Windows.Forms.TabPage();
            this._tablesListView = new System.Windows.Forms.ListView();
            this._nameHeader = new System.Windows.Forms.ColumnHeader();
            this._recordsHeader = new System.Windows.Forms.ColumnHeader();
            this._fragmentationHeader = new System.Windows.Forms.ColumnHeader();
            this._occupiedSpaceHeader = new System.Windows.Forms.ColumnHeader();
            this._pathBox = new System.Windows.Forms.TextBox();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this._resultsPanel = new System.Windows.Forms.Panel();
            this._resultsBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this._toolBar = new System.Windows.Forms.ToolBar();
            this._refreshDbButton = new System.Windows.Forms.ToolBarButton();
            this._diagnoseTablesButton = new System.Windows.Forms.ToolBarButton();
            this._rebuildIndexesButton = new System.Windows.Forms.ToolBarButton();
            this._textIndexPage = new System.Windows.Forms.TabPage();
            this._performancePage = new System.Windows.Forms.TabPage();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this._resourceCacheSizeTrackBar = new System.Windows.Forms.TrackBar();
            this._tabs.SuspendLayout();
            this._dbPage.SuspendLayout();
            this._resultsPanel.SuspendLayout();
            this._performancePage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._resourceCacheSizeTrackBar)).BeginInit();
            this.SuspendLayout();
            //
            // _imageList
            //
            this._imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this._imageList.ImageSize = new System.Drawing.Size(16, 16);
            this._imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_imageList.ImageStream")));
            this._imageList.TransparentColor = System.Drawing.Color.Transparent;
            //
            // _statusBar
            //
            this._statusBar.Location = new System.Drawing.Point(0, 328);
            this._statusBar.Name = "_statusBar";
            this._statusBar.Size = new System.Drawing.Size(616, 22);
            this._statusBar.TabIndex = 1;
            //
            // _tabs
            //
            this._tabs.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this._tabs.Controls.Add(this._dbPage);
            this._tabs.Controls.Add(this._textIndexPage);
            this._tabs.Controls.Add(this._performancePage);
            this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tabs.ImageList = this._imageList;
            this._tabs.Location = new System.Drawing.Point(0, 0);
            this._tabs.Multiline = true;
            this._tabs.Name = "_tabs";
            this._tabs.SelectedIndex = 0;
            this._tabs.ShowToolTips = true;
            this._tabs.Size = new System.Drawing.Size(616, 328);
            this._tabs.TabIndex = 2;
            //
            // _dbPage
            //
            this._dbPage.Controls.Add(this._tablesListView);
            this._dbPage.Controls.Add(this._pathBox);
            this._dbPage.Controls.Add(this.splitter1);
            this._dbPage.Controls.Add(this._resultsPanel);
            this._dbPage.Controls.Add(this._toolBar);
            this._dbPage.ImageIndex = 0;
            this._dbPage.Location = new System.Drawing.Point(4, 4);
            this._dbPage.Name = "_dbPage";
            this._dbPage.Size = new System.Drawing.Size(608, 301);
            this._dbPage.TabIndex = 0;
            this._dbPage.Text = "Resource Store";
            this._dbPage.ToolTipText = "Manage database of resources";
            //
            // _tablesListView
            //
            this._tablesListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                              this._nameHeader,
                                                                                              this._recordsHeader,
                                                                                              this._fragmentationHeader,
                                                                                              this._occupiedSpaceHeader});
            this._tablesListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tablesListView.FullRowSelect = true;
            this._tablesListView.Location = new System.Drawing.Point(0, 41);
            this._tablesListView.Name = "_tablesListView";
            this._tablesListView.Size = new System.Drawing.Size(405, 260);
            this._tablesListView.TabIndex = 6;
            this._tablesListView.View = System.Windows.Forms.View.Details;
            //
            // _nameHeader
            //
            this._nameHeader.Text = "Table";
            this._nameHeader.Width = 100;
            //
            // _recordsHeader
            //
            this._recordsHeader.Text = "Record Count";
            this._recordsHeader.Width = 100;
            //
            // _fragmentationHeader
            //
            this._fragmentationHeader.Text = "Fragmentation";
            this._fragmentationHeader.Width = 100;
            //
            // _occupiedSpaceHeader
            //
            this._occupiedSpaceHeader.Text = "Occupied Space";
            this._occupiedSpaceHeader.Width = 100;
            //
            // _pathBox
            //
            this._pathBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._pathBox.Dock = System.Windows.Forms.DockStyle.Top;
            this._pathBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._pathBox.Location = new System.Drawing.Point(0, 28);
            this._pathBox.Name = "_pathBox";
            this._pathBox.ReadOnly = true;
            this._pathBox.Size = new System.Drawing.Size(405, 13);
            this._pathBox.TabIndex = 5;
            this._pathBox.Text = "";
            //
            // splitter1
            //
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Right;
            this.splitter1.Location = new System.Drawing.Point(405, 28);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 273);
            this.splitter1.TabIndex = 4;
            this.splitter1.TabStop = false;
            //
            // _resultsPanel
            //
            this._resultsPanel.Controls.Add(this._resultsBox);
            this._resultsPanel.Controls.Add(this.label1);
            this._resultsPanel.Dock = System.Windows.Forms.DockStyle.Right;
            this._resultsPanel.Location = new System.Drawing.Point(408, 28);
            this._resultsPanel.Name = "_resultsPanel";
            this._resultsPanel.Size = new System.Drawing.Size(200, 273);
            this._resultsPanel.TabIndex = 3;
            //
            // _resultsBox
            //
            this._resultsBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._resultsBox.Location = new System.Drawing.Point(0, 23);
            this._resultsBox.Multiline = true;
            this._resultsBox.Name = "_resultsBox";
            this._resultsBox.ReadOnly = true;
            this._resultsBox.Size = new System.Drawing.Size(200, 250);
            this._resultsBox.TabIndex = 6;
            this._resultsBox.Text = "";
            //
            // label1
            //
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(200, 23);
            this.label1.TabIndex = 5;
            this.label1.Text = "Results";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // _toolBar
            //
            this._toolBar.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
                                                                                        this._refreshDbButton,
                                                                                        this._diagnoseTablesButton,
                                                                                        this._rebuildIndexesButton});
            this._toolBar.ButtonSize = new System.Drawing.Size(120, 22);
            this._toolBar.DropDownArrows = true;
            this._toolBar.Location = new System.Drawing.Point(0, 0);
            this._toolBar.Name = "_toolBar";
            this._toolBar.ShowToolTips = true;
            this._toolBar.Size = new System.Drawing.Size(608, 28);
            this._toolBar.TabIndex = 1;
            this._toolBar.TextAlign = System.Windows.Forms.ToolBarTextAlign.Right;
            this._toolBar.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this._toolBar_ButtonClick);
            //
            // _refreshDbButton
            //
            this._refreshDbButton.Text = "Refresh";
            //
            // _diagnoseTablesButton
            //
            this._diagnoseTablesButton.Text = "Diagnose";
            this._diagnoseTablesButton.ToolTipText = "Diagnose selected tables";
            //
            // _rebuildIndexesButton
            //
            this._rebuildIndexesButton.Text = "Rebuild Indexes";
            //
            // _textIndexPage
            //
            this._textIndexPage.ImageIndex = 1;
            this._textIndexPage.Location = new System.Drawing.Point(4, 4);
            this._textIndexPage.Name = "_textIndexPage";
            this._textIndexPage.Size = new System.Drawing.Size(608, 301);
            this._textIndexPage.TabIndex = 1;
            this._textIndexPage.Text = "Text Index";
            this._textIndexPage.ToolTipText = "Manage text index";
            this._textIndexPage.Visible = false;
            //
            // _performancePage
            //
            this._performancePage.Controls.Add(this.label4);
            this._performancePage.Controls.Add(this.label3);
            this._performancePage.Controls.Add(this.label2);
            this._performancePage.Controls.Add(this._resourceCacheSizeTrackBar);
            this._performancePage.Location = new System.Drawing.Point(4, 4);
            this._performancePage.Name = "_performancePage";
            this._performancePage.Size = new System.Drawing.Size(608, 301);
            this._performancePage.TabIndex = 2;
            this._performancePage.Text = "Performance";
            this._performancePage.ToolTipText = "Tune performance settings";
            //
            // label4
            //
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.Location = new System.Drawing.Point(528, 40);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(92, 23);
            this.label4.TabIndex = 3;
            this.label4.Text = "More speed";
            //
            // label3
            //
            this.label3.Location = new System.Drawing.Point(168, 40);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(112, 23);
            this.label3.TabIndex = 2;
            this.label3.Text = "Less memory";
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(152, 23);
            this.label2.TabIndex = 1;
            this.label2.Text = "Resources cache size:";
            //
            // _resourceCacheSizeTrackBar
            //
            this._resourceCacheSizeTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._resourceCacheSizeTrackBar.Location = new System.Drawing.Point(160, 4);
            this._resourceCacheSizeTrackBar.Maximum = 64;
            this._resourceCacheSizeTrackBar.Name = "_resourceCacheSizeTrackBar";
            this._resourceCacheSizeTrackBar.Size = new System.Drawing.Size(436, 45);
            this._resourceCacheSizeTrackBar.TabIndex = 0;
            this._resourceCacheSizeTrackBar.ValueChanged += new System.EventHandler(this._resourceCacheSizeTrackBar_ValueChanged);
            //
            // MainForm
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(616, 350);
            this.Controls.Add(this._tabs);
            this.Controls.Add(this._statusBar);
            this.Font = new System.Drawing.Font("Tahoma", 8F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Activated += new System.EventHandler(this.MainForm_Activated);
            this._tabs.ResumeLayout(false);
            this._dbPage.ResumeLayout(false);
            this._resultsPanel.ResumeLayout(false);
            this._performancePage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._resourceCacheSizeTrackBar)).EndInit();
            this.ResumeLayout(false);

        }
		#endregion

        internal static Icon LoadIconFromAssembly( string name )
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream( name );
            return ( stream != null ) ? new Icon( stream ) : null;
        }

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
            OMEnv.WorkDir = RegUtil.DatabasePath;
            MyPalStorage.DBPath = Path.Combine( OMEnv.WorkDir, "db" );
            bool omniaMeaIsNotRun;
            Mutex omniaMeaMux = new Mutex( true, "OmniaMeaMutex", out omniaMeaIsNotRun );
            try
            {
                if ( !omniaMeaIsNotRun )
                {
                    string prompt;
#if READER
                    prompt = "Omea Reader is currently running. Please close it and come back to Maintenance Tool.";
#else
                    prompt = "Omea Pro is currently running. Please close it and come back to Maintenance Tool.";
#endif
                    WaitHandleForm form = new WaitHandleForm( omniaMeaMux, prompt );
                    if( form.ShowDialog() != DialogResult.OK )
                    {
                        return;
                    }
                }
                Application.Run(new MainForm());
            }
            finally
            {
                omniaMeaMux.Close();
            }
		}

        private void MainForm_Activated(object sender, System.EventArgs e)
        {
#if READER
           Text = "JetBrains Omea Reader Maintenance Tool";
#else
           Text = "JetBrains Omea Pro Maintenance Tool";
#endif
        }

        private void _toolBar_ButtonClick(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e)
        {
            _toolBar.Enabled = false;
            try
            {
                if( e.Button == _refreshDbButton )
                {
                    _statusBar.Text = "Refreshing view...";
                    _dbProxy.Populate( _tablesListView );
                }
                else if( e.Button == _diagnoseTablesButton )
                {
                    _resultsBox.Text = "";
                    _statusBar.Text = "Diagnosing database...";
                    _dbProxy.Diagnose( new RepairProgressEventHandler( Repair_RepairProgress ) );
                }
                else if ( e.Button == _rebuildIndexesButton )
                {
                    _resultsBox.Text = "";
                    _statusBar.Text = "Rebuilding indexes...";
                    _lastRebuiltTable = null;
                    _dbProxy.RebuildIndexes( new DBStructure.ProgressEventHandler( RebuildIndexes_Progress ) );
                }
            }
            finally
            {
                _statusBar.Text = "";
                _toolBar.Enabled = true;
            }
        }

        private void Repair_RepairProgress( object sender, RepairProgressEventArgs e )
        {
            _resultsBox.Text += e.Message;
            _resultsBox.Text += "\r\n";
            Application.DoEvents();
        }

        private void RebuildIndexes_Progress( string progress, int tableNum, int tableCount )
        {
            if ( progress != _lastRebuiltTable )
            {
                _lastRebuiltTable = progress;
                _resultsBox.Text += progress;
                _resultsBox.Text += "\r\n";
                Application.DoEvents();
            }
        }

        private void _resourceCacheSizeTrackBar_ValueChanged(object sender, System.EventArgs e)
        {
            int cacheSize = _resourceCacheSizeTrackBar.Value << 10;
            if( cacheSize == ( 1 << 16 ) )
            {
                --cacheSize;
            }
            _ini.WriteInt( "ResourceStore", "ResourceCacheSize", cacheSize );
        }
	}
}
