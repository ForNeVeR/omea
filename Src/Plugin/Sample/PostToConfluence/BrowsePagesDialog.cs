// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Windows.Forms;
using PostToConfluence.com.atlassian.confluence;

namespace JetBrains.Omea.SamplePlugins.PostToConfluence
{
	/// <summary>
	/// Dialog for selecting a page in a Confluence space
	/// </summary>
	public class BrowsePagesDialog : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label _lblProgress;
        private System.Windows.Forms.TreeView _tvPages;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private ConfluenceSoap _confluence;
        private string _spaceKey;
        private RemotePageSummary[] _summaries;

		public BrowsePagesDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(BrowsePagesDialog));
            this._tvPages = new System.Windows.Forms.TreeView();
            this._btnOK = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this._lblProgress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // _tvPages
            //
            this._tvPages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._tvPages.ImageIndex = -1;
            this._tvPages.Location = new System.Drawing.Point(4, 4);
            this._tvPages.Name = "_tvPages";
            this._tvPages.SelectedImageIndex = -1;
            this._tvPages.Size = new System.Drawing.Size(284, 228);
            this._tvPages.Sorted = true;
            this._tvPages.TabIndex = 0;
            //
            // _btnOK
            //
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.Enabled = false;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(124, 240);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 1;
            this._btnOK.Text = "OK";
            //
            // button1
            //
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button1.Location = new System.Drawing.Point(208, 240);
            this.button1.Name = "button1";
            this.button1.TabIndex = 2;
            this.button1.Text = "Cancel";
            //
            // _lblProgress
            //
            this._lblProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._lblProgress.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblProgress.Location = new System.Drawing.Point(8, 240);
            this._lblProgress.Name = "_lblProgress";
            this._lblProgress.Size = new System.Drawing.Size(108, 16);
            this._lblProgress.TabIndex = 3;
            this._lblProgress.Text = "Loading pages...";
            this._lblProgress.Visible = false;
            //
            // BrowsePagesDialog
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(292, 271);
            this.Controls.Add(this._lblProgress);
            this.Controls.Add(this.button1);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._tvPages);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BrowsePagesDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "BrowsePagesDialog";
            this.VisibleChanged += new System.EventHandler(this.BrowsePagesDialog_VisibleChanged);
            this.ResumeLayout(false);

        }

	    #endregion

	    public string SpaceKey
	    {
	        get { return _spaceKey; }
	        set
            {
                _spaceKey = value;
                Text = "Browse Pages in Space " + _spaceKey;
            }
	    }

	    private void BrowsePagesDialog_VisibleChanged( object sender, System.EventArgs e )
        {
            VisibleChanged -= new EventHandler( BrowsePagesDialog_VisibleChanged );
            BeginInvoke( new MethodInvoker( LoadPages ) );
        }

        private void LoadPages()
        {
            _lblProgress.Visible = true;
            _lblProgress.Refresh();
            _confluence = LoginManager.GetConfluenceService();
            _confluence.Beginlogin( LoginManager.UserName, LoginManager.Password,
                new AsyncCallback( LoginDone ), null );
        }

	    private void LoginDone( IAsyncResult ar )
	    {
            string loginToken = _confluence.Endlogin( ar );
            _confluence.BegingetPages( loginToken, _spaceKey, new AsyncCallback( GetPagesDone ), null );
	    }

	    private void GetPagesDone( IAsyncResult ar )
	    {
	        _summaries = _confluence.EndgetPages( ar );
            BeginInvoke( new MethodInvoker( FillPages ) );
	    }

        private void FillPages()
        {
            PageNode rootNode = BuildPageTree( _summaries );
            foreach( PageNode pageNode in rootNode.Children )
            {
                TreeNode node = _tvPages.Nodes.Add( pageNode.Page.title );
                node.Tag = pageNode.Page;
                FillChildNodes( pageNode, node );
            }
            _lblProgress.Visible = false;
            _btnOK.Enabled = true;
        }

        private void FillChildNodes( PageNode pageNode, TreeNode node )
        {
            foreach( PageNode childPageNode in pageNode.Children )
            {
                TreeNode childNode = node.Nodes.Add( childPageNode.Page.title );
                childNode.Tag = childPageNode.Page;
                FillChildNodes( childPageNode, childNode );
            }
        }

	    private static PageNode BuildPageTree( RemotePageSummary[] summaries )
	    {
	        Hashtable nodes = new Hashtable();
	        PageNode rootNode = new PageNode( null );
	        nodes [0L] = rootNode;

	        foreach( RemotePageSummary summary in summaries )
	        {
	            PageNode parentNode = (PageNode) nodes [summary.parentId];
	            if ( parentNode == null )
	            {
	                parentNode = new PageNode( null );
	                nodes [summary.parentId] = parentNode;
	            }

	            PageNode thisNode = (PageNode) nodes [summary.id];
	            if ( thisNode == null )
	            {
	                thisNode = new PageNode( summary );
	                nodes [summary.id] = thisNode;
	            }
	            else
	            {
	                thisNode.Page = summary;
	            }

	            parentNode.AddChild( thisNode );
	        }
            return rootNode;
	    }

	    public RemotePageSummary SelectedPageSummary
        {
            get
            {
                if ( _tvPages.SelectedNode != null )
                {
                    return (RemotePageSummary) _tvPages.SelectedNode.Tag;
                }
                return null;
            }
        }

        public static RemotePageSummary BrowseForPage( IWin32Window owner, string spaceKey )
        {
            BrowsePagesDialog dlg = new BrowsePagesDialog();
            using( dlg )
            {
                dlg.SpaceKey = spaceKey;
                DialogResult dr = dlg.ShowDialog( owner );
                if ( dr == DialogResult.Cancel )
                {
                    return null;
                }
                return dlg.SelectedPageSummary;
            }
        }

        internal class PageNode
        {
            RemotePageSummary _page;
            ArrayList _children = new ArrayList();

            public PageNode( RemotePageSummary page )
            {
                _page = page;
            }

            public RemotePageSummary Page
            {
                get { return _page; }
                set { _page = value; }
            }

            public void AddChild( PageNode node )
            {
                _children.Add( node );
            }

            public IEnumerable Children
            {
                get { return _children; }
            }
        }

	}
}
