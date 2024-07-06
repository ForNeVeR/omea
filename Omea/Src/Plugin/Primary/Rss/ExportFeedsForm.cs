// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.IO;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Containers;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Summary description for ExportFeedsFrom.
	/// </summary>
	public class ExportFeedsForm : DialogBase
	{
        private ResourceTreeView2 _treeFeeds;
        private Button  _btnOK;
        private Button  _btnCancel;
        private TextBox _edtFileName;
        private Label   _lblTitle;
        private Label   _lblDestination;
        private Button  _btnBrowse;
        private Button  _btnSelectAll;
        private Button  _btnUnselectAll;

        private int          _totalFeeds = 0, _checkedFeeds = 0;
        private readonly IntArrayList _listCheckedFeeds = new IntArrayList();

        private System.ComponentModel.Container components = null;

		public ExportFeedsForm()
		{
			InitializeComponent();
            CheckValidState( false );
		}

        public string       FileName { get { return _edtFileName.Text; } }
        public IntArrayList CheckedFeeds { get { return _listCheckedFeeds; } }

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

		#region Windows Form Designer generated code
		private void InitializeComponent()
		{
            this._lblTitle = new System.Windows.Forms.Label();
            this._treeFeeds = new JetBrains.Omea.GUIControls.ResourceTreeView2();
            this._btnSelectAll = new System.Windows.Forms.Button();
            this._btnUnselectAll = new System.Windows.Forms.Button();
            this._lblDestination = new System.Windows.Forms.Label();
            this._edtFileName = new System.Windows.Forms.TextBox();
            this._btnBrowse = new System.Windows.Forms.Button();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // label1
            //
            this._lblTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this._lblTitle.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblTitle.Location = new System.Drawing.Point(8, 8);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Size = new System.Drawing.Size(150, 16);
            this._lblTitle.TabIndex = 0;
            this._lblTitle.Text = "&Choose feeds for export:";
            //
            // _treeFeeds
            //
            this._treeFeeds.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._treeFeeds.AllowColumnReorder = false;
            this._treeFeeds.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._treeFeeds.FullRowSelect = false;
            this._treeFeeds.HeaderContextMenu = null;
            this._treeFeeds.CheckBoxes = true;
            this._treeFeeds.Location = new System.Drawing.Point(8, 30);
            this._treeFeeds.MultiSelect = false;
            this._treeFeeds.Name = "_treeFeeds";
            this._treeFeeds.ShowContextMenu = false;
            this._treeFeeds.Size = new System.Drawing.Size(280, 400);
            this._treeFeeds.TabIndex = 1;
            this._treeFeeds.ResourceAdded += new JetBrains.Omea.OpenAPI.ResourceEventHandler(this.ResourceAdded);
            this._treeFeeds.AfterCheck += new ResourceCheckEventHandler(_treeFeeds_AfterCheck);
            this._treeFeeds.RootResource = RSSPlugin.RootFeedGroup;
            this._treeFeeds.ParentProperty = Core.Props.Parent;
            this._treeFeeds.OpenProperty = Core.Props.Open;
            //
            // _btnSelectAll
            //
            this._btnSelectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnSelectAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnSelectAll.Location = new System.Drawing.Point(295, 30);
            this._btnSelectAll.Size = new System.Drawing.Size(80, 24);
            this._btnSelectAll.Name = "_btnSelectAll";
            this._btnSelectAll.TabIndex = 1;
            this._btnSelectAll.Text = "&Select All";
            this._btnSelectAll.Click += new System.EventHandler(this._btnSelectAll_Click);
            //
            // _btnUnselectAll
            //
            this._btnUnselectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnUnselectAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnUnselectAll.Location = new System.Drawing.Point(295, 64);
            this._btnUnselectAll.Name = "_btnUnselectAll";
            this._btnUnselectAll.Size = new System.Drawing.Size(80, 24);
            this._btnUnselectAll.TabIndex = 2;
            this._btnUnselectAll.Text = "&Unselect All";
            this._btnUnselectAll.Click += new System.EventHandler(this._btnUnselectAll_Click);
            //
            // _lblDestination
            //
            this._lblDestination.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._lblDestination.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblDestination.Location = new System.Drawing.Point(8, 440);
            this._lblDestination.Name = "_lblDestination";
            this._lblDestination.Size = new System.Drawing.Size(80, 16);
            this._lblDestination.TabIndex = 0;
            this._lblDestination.Text = "Save to &file:";
            //
            // _edtFileName
            //
            this._edtFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtFileName.Location = new System.Drawing.Point(90, 438);
            this._edtFileName.Multiline = false;
            this._edtFileName.Name = "_edtFileName";
            this._edtFileName.Size = new System.Drawing.Size(205, 25);
            this._edtFileName.TabIndex = 4;
            this._edtFileName.Text = "";
            this._edtFileName.TextChanged += new System.EventHandler(_edtFileName_TextChanged);
            //
            // _btnBrowse
            //
            this._btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnBrowse.Location = new System.Drawing.Point(305, 438);
            this._btnBrowse.Name = "_btnBrowse";
            this._btnBrowse.TabIndex = 5;
            this._btnBrowse.Text = "&Browse...";
            this._btnBrowse.Click += new System.EventHandler(_btnBrowse_Click);
            //
            // _btnOK
            //
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(220, 470);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 5;
            this._btnOK.Text = "OK";
            this._btnOK.Click += new System.EventHandler(_btnOK_Click);
            //
            // _btnCancel
            //
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(305, 470);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 6;
            this._btnCancel.Text = "Cancel";

            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.Controls.Add(this._btnUnselectAll);
            this.Controls.Add(this._btnSelectAll);
            this.Controls.Add(this._lblTitle);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._treeFeeds);
            this.Controls.Add(this._lblDestination);
            this.Controls.Add(this._edtFileName);
            this.Controls.Add(this._btnBrowse);
            this.Name = "ExportFeedsForm";
            this.Text = "Export Feeds";
            this.ClientSize = new System.Drawing.Size(390, 500);
			this.Text = "Export Feeds";
            this.ResumeLayout(false);
		}
		#endregion

        private void _btnSelectAll_Click( object sender, System.EventArgs e )
        {
            _treeFeeds.ForEachNode( CheckResource );
            _checkedFeeds = _totalFeeds;
            CheckValidState();
        }

        private void _btnUnselectAll_Click( object sender, System.EventArgs e )
        {
            _treeFeeds.ForEachNode( UncheckResource );
            _checkedFeeds = 0;
            CheckValidState( false );
        }

	    private void CheckResource( IResource res )
	    {
            if ( res.Type != "RSSFeedGroup" )
            {
                _treeFeeds.SetNodeCheckState( res, CheckBoxState.Checked );
            }
	    }

	    private void UncheckResource( IResource res )
	    {
            if ( res.Type != "RSSFeedGroup" )
            {
                _treeFeeds.SetNodeCheckState( res, CheckBoxState.Unchecked );
            }
        }

        public void ResourceAdded( object sender, ResourceEventArgs e )
        {
            if ( e.Resource.Type == "RSSFeedGroup" )
            {
                _treeFeeds.SetNodeCheckState( e.Resource, CheckBoxState.Hidden );
            }
            else
            {
                _treeFeeds.SetNodeCheckState( e.Resource, CheckBoxState.Checked );
                _totalFeeds++;
                _checkedFeeds++;
            }
        }

        private void CheckValidState( bool state )
        {
            _btnOK.Enabled = state;
        }
        private void CheckValidState()
        {
            _btnOK.Enabled = !string.IsNullOrEmpty( _edtFileName.Text ) && AnyItemChecked();
        }

        private bool  AnyItemChecked()
        {
            return _checkedFeeds > 0;
        }

        private void _btnBrowse_Click(object sender, System.EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "OPML files (*.opml)|*.opml|All files (*.*)|*.*";
            if ( dlg.ShowDialog() == DialogResult.OK )
            {
                _edtFileName.Text = dlg.FileName;
            }
            CheckValidState();
        }

        private void _treeFeeds_AfterCheck(object sender, ResourceCheckEventArgs e)
        {
            _checkedFeeds += (e.NewState == CheckBoxState.Checked) ? 1 : -1;
            CheckValidState();
        }

        private void _btnOK_Click(object sender, System.EventArgs e)
        {
            string path = _edtFileName.Text;
            try
            {
                //  Check whether OMPL Processor's XMLWriter will manage
                //  to open output stream.
                path = Path.GetFullPath( _edtFileName.Text );
                FileStream strm = new FileStream( _edtFileName.Text, FileMode.Create );
                _treeFeeds.ForEachNode( CollectCheckedResource );
                strm.Close();
            }
            catch( System.ArgumentException )
            {
                MessageBox.Show( "Can not open output file with name: " + path );
                DialogResult = DialogResult.None;
            }
            catch( DirectoryNotFoundException )
            {
                MessageBox.Show( "Can not open output file with name: " + path );
                DialogResult = DialogResult.None;
            }
            catch( IOException )
            {
                MessageBox.Show( "Can not open output file with name: " + path );
                DialogResult = DialogResult.None;
            }
        }

        private void CollectCheckedResource( IResource res )
        {
            if( _treeFeeds.GetNodeCheckState( res ) == CheckBoxState.Checked )
                _listCheckedFeeds.Add( res.Id );
        }

        private void _edtFileName_TextChanged(object sender, System.EventArgs e)
        {
            CheckValidState();
        }
    }
}
