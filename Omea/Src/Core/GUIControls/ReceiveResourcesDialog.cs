/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.UI.Components.CustomTreeView;

namespace JetBrains.Omea.GUIControls
{
    public class ReceiveResourcesDialog : DialogBase
    {
        private string _fileName;
        private ICore _core;
        private IResource _mail;
        private IResourceIconManager _resourceIconManager;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Button _btnSend;
        private CustomTreeView _resourceTreeView;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public ReceiveResourcesDialog( string fileName, IResource mail )
        {
            if ( fileName == null ) throw new System.ArgumentNullException( "fileName" );
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            
            _mail = mail;
            _resourceTreeView.ThreeStateCheckboxes = true;
            _resourceTreeView.AfterThreeStateCheck += new JetBrains.UI.Components.CustomTreeView.ThreeStateCheckEventHandler(AfterThreeStateCheck);
            _btnSend.Click+=new EventHandler( OnSend );
            _core = ICore.Instance;
            _fileName = fileName;
            _resourceIconManager = _core.ResourceIconManager;
            _resourceTreeView.ImageList = _core.ResourceIconManager.ImageList;
            PopulateTreeView();
        }

        private int GetIcon( IResource resource )
        {
            return _resourceIconManager.GetIconIndex( resource );
        }
        private int GetDefaultIcon( string resourceType )
        {
            return _resourceIconManager.GetDefaultIconIndex( resourceType );
       }

        private void AddLinks( TreeNode treeNode, ResourceUnpack resourceUnpack )
        {
            foreach ( LinkUnpack linkNode in resourceUnpack.Links )
            {
                int iconIndex = GetDefaultIcon( "ResourceType" );
                TreeNode treeSubNode = 
                    new TreeNode( linkNode.DisplayName, iconIndex, iconIndex );
                treeSubNode.Tag = linkNode;
                treeNode.Nodes.Add( treeSubNode );
                AddLinkedResources( treeSubNode, linkNode );
            }
        }
        private string GetTypeDisplayName( string type )
        {
            return Core.ResourceStore.ResourceTypes[type].DisplayName;
        }

        private void AddLinkedResources( TreeNode treeNode, LinkUnpack selectedResource )
        {
            foreach ( ResourceUnpack resourceNode in selectedResource.Resources )
            {
                int iconIndex = GetIcon( resourceNode.Resource );
                TreeNode treeSubNode = new TreeNode( GetTypeDisplayName( resourceNode.Resource.Type ) + ": " + resourceNode.Resource.DisplayName, iconIndex, iconIndex );
                treeSubNode.Tag = resourceNode;
                treeNode.Nodes.Add( treeSubNode );
                _resourceTreeView.SetNodeCheckState( treeSubNode, NodeCheckState.Checked );
            }
        }

        private void PopulateTreeView()
        {
            ResourceDeserializer resSer = new ResourceDeserializer( _fileName );
            foreach ( ResourceUnpack resourceNode in resSer.GetSelectedResources() )
            {
                int iconIndex = GetIcon( resourceNode.Resource );
                TreeNode node = new TreeNode( GetTypeDisplayName( resourceNode.Resource.Type ) + ": " + resourceNode.Resource.DisplayName, iconIndex, iconIndex );
                node.Tag = resourceNode;
                _resourceTreeView.Nodes.Add( node );
                AddLinks( node, resourceNode );
                _resourceTreeView.SetNodeCheckState( node, NodeCheckState.Checked );
                node.Expand();
            }
        }
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
            this._btnSend = new System.Windows.Forms.Button();
            this._resourceTreeView = new JetBrains.UI.Components.CustomTreeView.CustomTreeView();
            this.SuspendLayout();
            // 
            // _btnCancel
            // 
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(352, 304);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(75, 27);
            this._btnCancel.TabIndex = 7;
            this._btnCancel.Text = "Cancel";
            // 
            // _btnSend
            // 
            this._btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnSend.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnSend.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnSend.Location = new System.Drawing.Point(268, 304);
            this._btnSend.Name = "_btnSend";
            this._btnSend.Size = new System.Drawing.Size(75, 27);
            this._btnSend.TabIndex = 6;
            this._btnSend.Text = "Receive";
            // 
            // _resourceTreeView
            // 
            this._resourceTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._resourceTreeView.ImageIndex = -1;
            this._resourceTreeView.Location = new System.Drawing.Point(8, 8);
            this._resourceTreeView.Name = "_resourceTreeView";
            this._resourceTreeView.NodePainter = null;
            this._resourceTreeView.SelectedImageIndex = -1;
            this._resourceTreeView.Size = new System.Drawing.Size(420, 288);
            this._resourceTreeView.TabIndex = 8;
            this._resourceTreeView.ThreeStateCheckboxes = false;
            // 
            // ReceiveResourcesDialog
            // 
            this.AcceptButton = this._btnSend;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(440, 342);
            this.Controls.Add(this._resourceTreeView);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnSend);
            this.Name = "ReceiveResourcesDialog";
            this.Text = "Receive Resources";
            this.ResumeLayout(false);

        }
        #endregion

        private void AfterThreeStateCheck(object sender, JetBrains.UI.Components.CustomTreeView.ThreeStateCheckEventArgs e)
        {
            foreach ( TreeNode node in _resourceTreeView.Nodes )
            {
                if ( _resourceTreeView.GetNodeCheckState( node ) == NodeCheckState.Checked )
                {
                    _btnSend.Enabled = true;
                    return;
                }
            }
            _btnSend.Enabled = false;
        }

        private delegate void DelegateReceiveResoources();
        private void ReceiveResoources()
        {
            foreach ( TreeNode treeNode in _resourceTreeView.Nodes )
            {
                NodeCheckState currState = _resourceTreeView.GetNodeCheckState( treeNode );
                if ( currState == NodeCheckState.Checked )
                {
                    ResourceUnpack resourceUnpack = (ResourceUnpack)treeNode.Tag;
                    resourceUnpack.AcceptReceiving();
                    _mail.AddLink( "ResourceAttachment", resourceUnpack.Resource );
                    foreach ( TreeNode linkTreeNode in treeNode.Nodes )
                    {
                        foreach ( TreeNode linkedResourceNode in linkTreeNode.Nodes )
                        {
                            currState = _resourceTreeView.GetNodeCheckState( linkedResourceNode );
                            if ( currState == NodeCheckState.Checked )
                            {
                                resourceUnpack = (ResourceUnpack)linkedResourceNode.Tag;
                                resourceUnpack.AcceptReceiving();
                            }
                        }
                    }
                }
            }
        }
        private void OnSend(object sender, System.EventArgs e)
        {
            ICore.Instance.ResourceAP.RunJob( new DelegateReceiveResoources( ReceiveResoources ) );
        }
    }
}
