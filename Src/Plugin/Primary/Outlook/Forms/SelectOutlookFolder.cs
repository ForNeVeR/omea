// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.OutlookPlugin
{
    public class SelectOutlookFolder : DialogBase
    {
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Label _label1;
        private GUIControls.ResourceTreeView _treeView;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public SelectOutlookFolder( bool copy )
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            _treeView.AddNodeFilter( new OutlookFoldersFilter() );
            _treeView.OpenProperty = PROP.OpenSelectFolder;
            _treeView.ParentProperty = Core.Props.Parent;
            _treeView.RootResource = Core.ResourceTreeManager.GetRootForType(STR.MAPIFolder);
            if ( copy )
            {
                _label1.Text = "Copy the selected items to the folder:";
                Text = "Copy Items";
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

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._btnCancel = new System.Windows.Forms.Button();
            this._btnOK = new System.Windows.Forms.Button();
            this._label1 = new System.Windows.Forms.Label();
            this._treeView = new JetBrains.Omea.GUIControls.ResourceTreeView();
            this.SuspendLayout();
            //
            // _btnCancel
            //
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(272, 60);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(75, 25);
            this._btnCancel.TabIndex = 2;
            this._btnCancel.Text = "Cancel";
            //
            // _btnOK
            //
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(272, 26);
            this._btnOK.Name = "_btnOK";
            this._btnOK.Size = new System.Drawing.Size(75, 25);
            this._btnOK.TabIndex = 1;
            this._btnOK.Text = "OK";
            //
            // _label1
            //
            this._label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._label1.Location = new System.Drawing.Point(8, 9);
            this._label1.Name = "_label1";
            this._label1.Size = new System.Drawing.Size(280, 17);
            this._label1.TabIndex = 8;
            this._label1.Text = "Move the selected items to the folder:";
            //
            // _treeView
            //
            this._treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._treeView.ImageIndex = -1;
            this._treeView.Location = new System.Drawing.Point(8, 26);
            this._treeView.Name = "_treeView";
            this._treeView.NodePainter = null;
            this._treeView.ParentProperty = 0;
            this._treeView.SelectedImageIndex = -1;
            this._treeView.Size = new System.Drawing.Size(256, 284);
            this._treeView.TabIndex = 0;
            this._treeView.ThreeStateCheckboxes = false;
            this._treeView.DoubleClick += new System.EventHandler(this.OnDoubleClick);
            this._treeView.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.OnAfterExpand);
            this._treeView.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.OnAfterCollapse);
            //
            // SelectOutlookFolder
            //
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(352, 318);
            this.Controls.Add(this._treeView);
            this.Controls.Add(this._label1);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.MinimumSize = new System.Drawing.Size(360, 304);
            this.Name = "SelectOutlookFolder";
            this.Text = "Move Items";
            this.Load += new System.EventHandler(this.SelectOutlookFolder_Load);
            this.ResumeLayout(false);

        }
        #endregion

        public PairIDs SelectedFolder
        {
            get
            {
                return PairIDs.Get( _treeView.SelectedResource );
            }
        }

        public IResource SelectedFolderResource
        {
            get
            {
                return _treeView.SelectedResource;
            }
        }

        public void SelectFolder( IResource res )
        {
            _treeView.SelectResourceNode( res );
        }

        public static SelectOutlookFolder GetInstance( bool copy )
        {
            return new SelectOutlookFolder( copy );
        }

        private void SelectOutlookFolder_Load(object sender, System.EventArgs e)
        {
            //_outlookFoldersTreeView.ExpandToLevel( 2 );
        }

        private void OnAfterCollapse(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            ResourceProxy folder = new ResourceProxy( (IResource)e.Node.Tag );
            folder.SetPropAsync( PROP.OpenSelectFolder, 0 );
        }

        private void OnAfterExpand(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            ResourceProxy folder = new ResourceProxy( (IResource)e.Node.Tag );
            folder.SetPropAsync( PROP.OpenSelectFolder, 1 );
        }

        private void OnDoubleClick(object sender, System.EventArgs e)
        {
            TreeNode node = _treeView.SelectedNode;
            if ( node != null )
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
