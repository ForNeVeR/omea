// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.RSSPlugin.SubscribeWizard;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Summary description for ExportFeedsFrom.
	/// </summary>
	public class SelectFeedFolderForm : DialogBase
	{
        private ResourceTreeView            _treeFeeds;
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button _btnCancel;
        private IResource                   _folderSelected = null;
        private IResourceList               _sourceSelected;

        private System.ComponentModel.Container components = null;

		public SelectFeedFolderForm( IResourceList sourceFeeds )
		{
			InitializeComponent();
            _sourceSelected = sourceFeeds;
		}

        public IResource SelectedFolder { get { return _folderSelected; } }

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
            this._treeFeeds = new ResourceTreeView();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // _treeFeeds
            //
            this._treeFeeds.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._treeFeeds.Location = new System.Drawing.Point(8, 8);
            this._treeFeeds.Size = new System.Drawing.Size(284, 422);
            this._treeFeeds.TabIndex = 1;
            this._treeFeeds.Name = "_treeFeeds";

            this._treeFeeds.DoubleBuffer = false;
            this._treeFeeds.ExecuteDoubleClickAction = false;
            this._treeFeeds.HideSelection = false;
            this._treeFeeds.ImageIndex = -1;
            this._treeFeeds.LabelEdit = false;
            this._treeFeeds.NodePainter = null;
            this._treeFeeds.ResourceChildProvider = null;
            this._treeFeeds.SelectedImageIndex = -1;
            this._treeFeeds.SelectedNodes = new System.Windows.Forms.TreeNode[0];
            this._treeFeeds.ShowRootResource = true;
            this._treeFeeds.ThreeStateCheckboxes = false;
            this._treeFeeds.FullRowSelect = false;
            this._treeFeeds.CheckBoxes = false;
            this._treeFeeds.MultiSelect = false;
            this._treeFeeds.ShowContextMenu = false;
            this._treeFeeds.DoubleClick += new System.EventHandler(this._btnOK_Click);
            this._treeFeeds.RootResource = RSSPlugin.RootFeedGroup;
            this._treeFeeds.ParentProperty = Core.Props.Parent;
            this._treeFeeds.AddNodeFilter( new TitleGroupPane.FeedGroupNodeFilter() );
            this._treeFeeds.TreeCreated += new EventHandler( OnTreeCreated );
            this._treeFeeds.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.OnFolderSelected);
            //
            // _btnOK
            //
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(132, 440);
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
            this._btnCancel.Location = new System.Drawing.Point(217, 440);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 6;
            this._btnCancel.Text = "Cancel";

            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.AcceptButton = this._btnOK;
            this.CancelButton = this._btnCancel;
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._treeFeeds);
            this.Name = "SelectFeedFolderForm";
            this.ClientSize = new System.Drawing.Size(300, 470);
			this.Text = "Select Destination Feed Folder";
            this.ResumeLayout(false);
		}
		#endregion

        private void OnTreeCreated( object sender, EventArgs e )
        {
            _treeFeeds.ExpandAll();
        }

        private void OnFolderSelected(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            IResource dest = _treeFeeds.SelectedResource;
            _btnOK.Enabled = (dest != null);
            if( _btnOK.Enabled )
            {
                foreach( IResource res in _sourceSelected )
                {
                    if( res.Type == Props.RSSFeedGroupResource && res.Id == dest.Id )
                        _btnOK.Enabled = false;
                }
            }
        }

        private void _btnOK_Click(object sender, System.EventArgs e)
        {
            _folderSelected = _treeFeeds.SelectedResource;
        }
    }
}
