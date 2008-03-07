/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin.SubscribeWizard
{
	/// <summary>
	/// Subscribe Wizard page for entering the feed title and selecting the 
	/// group to place the feed.
	/// </summary>
	internal class TitleGroupPane : System.Windows.Forms.UserControl
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _edtTitle;
        private System.Windows.Forms.Label label2;
        private GUIControls.ResourceTreeView _feedGroupTree;
        private System.Windows.Forms.Button _btnNewGroup;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private IResource _lastNewGroup;

        public event EventHandler NextPage;

		public TitleGroupPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            _feedGroupTree.AddNodeFilter( new FeedGroupNodeFilter() );
            _feedGroupTree.ShowRootResource = true;
            _feedGroupTree.RootResource = RSSPlugin.RootFeedGroup;
            _feedGroupTree.ParentProperty = Core.ResourceStore.GetPropId( "Parent" );
            _feedGroupTree.TreeCreated += new EventHandler( OnTreeCreated );
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

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.label1 = new System.Windows.Forms.Label();
            this._edtTitle = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this._feedGroupTree = new JetBrains.Omea.GUIControls.ResourceTreeView();
            this._btnNewGroup = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(12, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(168, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Enter the title for the feed:";
            // 
            // _edtTitle
            // 
            this._edtTitle.Location = new System.Drawing.Point(12, 48);
            this._edtTitle.Name = "_edtTitle";
            this._edtTitle.Size = new System.Drawing.Size(344, 22);
            this._edtTitle.TabIndex = 1;
            this._edtTitle.Text = "";
            this._edtTitle.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(12, 88);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(292, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "Choose the folder where the feed should be placed:";
            // 
            // _feedGroupTree
            // 
            this._feedGroupTree.DoubleBuffer = false;
            this._feedGroupTree.ExecuteDoubleClickAction = false;
            this._feedGroupTree.HideSelection = false;
            this._feedGroupTree.ImageIndex = -1;
            this._feedGroupTree.LabelEdit = true;
            this._feedGroupTree.Location = new System.Drawing.Point(12, 108);
            this._feedGroupTree.MultiSelect = false;
            this._feedGroupTree.Name = "_feedGroupTree";
            this._feedGroupTree.NodePainter = null;
            this._feedGroupTree.ResourceChildProvider = null;
            this._feedGroupTree.SelectedImageIndex = -1;
            this._feedGroupTree.SelectedNodes = new System.Windows.Forms.TreeNode[0];
            this._feedGroupTree.ShowContextMenu = false;
            this._feedGroupTree.ShowRootResource = false;
            this._feedGroupTree.Size = new System.Drawing.Size(344, 248);
            this._feedGroupTree.TabIndex = 3;
            this._feedGroupTree.ThreeStateCheckboxes = false;
            this._feedGroupTree.DoubleClick += new System.EventHandler(this._feedGroupTree_DoubleClick);
            this._feedGroupTree.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this._feedGroupTree_AfterLabelEdit);
            this._feedGroupTree.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            // 
            // _btnNewGroup
            // 
            this._btnNewGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnNewGroup.Location = new System.Drawing.Point(232, 364);
            this._btnNewGroup.Name = "_btnNewGroup";
            this._btnNewGroup.Size = new System.Drawing.Size(124, 23);
            this._btnNewGroup.TabIndex = 4;
            this._btnNewGroup.Text = "New Feed Folder";
            this._btnNewGroup.Click += new System.EventHandler(this._btnNewGroup_Click);
            this._btnNewGroup.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            // 
            // TitleGroupPane
            // 
            this.Controls.Add(this._btnNewGroup);
            this.Controls.Add(this._feedGroupTree);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._edtTitle);
            this.Controls.Add(this.label1);
            this.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            this.Name = "TitleGroupPane";
            this.Size = new System.Drawing.Size(384, 396);
            this.ResumeLayout(false);

        }

        #endregion

        private void OnTreeCreated( object sender, EventArgs e )
        {
            _feedGroupTree.ExpandAll();
        }

	    private void OnNextPage()
	    {
	        if ( NextPage != null )
	        {
	            NextPage( this, EventArgs.Empty );
	        }
	    }

	    public string FeedTitle
	    {
            get { return _edtTitle.Text; }
	        set
	        {
	            _edtTitle.Text = value;
                _edtTitle.Enabled = true;
	        }
	    }

        public void DisableFeedTitle()
        {
            _edtTitle.Text = "<multiple feeds selected>";
            _edtTitle.Enabled = false;
        }

        public IResource SelectedGroup
        {
            get { return _feedGroupTree.SelectedResource; }
            set { _feedGroupTree.SelectedResource = value; }
        }

        private void _btnNewGroup_Click( object sender, System.EventArgs e )
        {
            ResourceProxy proxy = ResourceProxy.BeginNewResource( "RSSFeedGroup" );
            proxy.SetProp( Core.Props.Name, "<new folder>" );
            proxy.SetProp( Core.Props.Parent, SelectedGroup );
            proxy.EndUpdate();
            _lastNewGroup = proxy.Resource;

            _feedGroupTree.EditResourceLabel( _lastNewGroup );
        }

        private void _feedGroupTree_AfterLabelEdit( object sender, System.Windows.Forms.NodeLabelEditEventArgs e )
        {
            if ( e.Label != null )
            {
                if ( e.Label == "" )
                {
                    MessageBox.Show( Core.MainWindow, "Please specify a name." );
                    e.CancelEdit = true;
                }
                else
                {
                    IResource res = (IResource) e.Node.Tag;
                    new ResourceProxy( res ).SetPropAsync( "Name", e.Label );
                }
            }
            else if ( e.Node.Tag == _lastNewGroup )
            {
                new ResourceProxy( _lastNewGroup ).DeleteAsync();
                _lastNewGroup = null;
            }
        }

        private void _feedGroupTree_DoubleClick( object sender, System.EventArgs e )
        {
            OnNextPage();        
        }

        public class FeedGroupNodeFilter : IResourceNodeFilter
        {
            public bool AcceptNode( IResource res, int level )
            {
                return res == RSSPlugin.RootFeedGroup || res.Type == "RSSFeedGroup";
            }
        }
	}
}
