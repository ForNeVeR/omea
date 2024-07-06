// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using PostToConfluence.com.atlassian.confluence;

namespace JetBrains.Omea.SamplePlugins.PostToConfluence
{
	/// <summary>
	/// The dialog for posting the selected resource to Confluence.
	/// </summary>
	public class PostDialog : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Label _lblProgress;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private IResource _resourceToPost;
        private string _textToPost;
        private System.Windows.Forms.Button _btnLogin;
        private System.Windows.Forms.Label _lblLogin;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox _cmbSpaces;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button _btnPost;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.TextBox _edtTitle;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox _edtContent;
        private string _loginToken;
        private long _parentId;
        private bool _asyncOperation = false;
        private RemoteSpaceSummary[] _spaces;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button _btnBrowse;
        private System.Windows.Forms.TextBox _edtParent;
        private System.Windows.Forms.Label _lblParent;
        private System.Windows.Forms.RadioButton _radNewPage;
        private System.Windows.Forms.RadioButton _radBlogPost;
        private ConfluenceSoap _confluence;

		public PostDialog()
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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(PostDialog));
            this._lblLogin = new System.Windows.Forms.Label();
            this._lblProgress = new System.Windows.Forms.Label();
            this._btnLogin = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this._cmbSpaces = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this._btnPost = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._edtTitle = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this._edtContent = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._radBlogPost = new System.Windows.Forms.RadioButton();
            this._radNewPage = new System.Windows.Forms.RadioButton();
            this._btnBrowse = new System.Windows.Forms.Button();
            this._edtParent = new System.Windows.Forms.TextBox();
            this._lblParent = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            //
            // _lblLogin
            //
            this._lblLogin.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblLogin.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblLogin.Location = new System.Drawing.Point(8, 12);
            this._lblLogin.Name = "_lblLogin";
            this._lblLogin.Size = new System.Drawing.Size(332, 28);
            this._lblLogin.TabIndex = 0;
            this._lblLogin.Text = "Not logged in";
            this._lblLogin.UseMnemonic = false;
            //
            // _lblProgress
            //
            this._lblProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblProgress.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblProgress.Location = new System.Drawing.Point(8, 379);
            this._lblProgress.Name = "_lblProgress";
            this._lblProgress.Size = new System.Drawing.Size(252, 16);
            this._lblProgress.TabIndex = 9;
            //
            // _btnLogin
            //
            this._btnLogin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnLogin.Location = new System.Drawing.Point(348, 8);
            this._btnLogin.Name = "_btnLogin";
            this._btnLogin.TabIndex = 1;
            this._btnLogin.Text = "&Login...";
            this._btnLogin.Click += new System.EventHandler(this._btnLogin_Click);
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "&Space:";
            //
            // _cmbSpaces
            //
            this._cmbSpaces.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._cmbSpaces.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbSpaces.Location = new System.Drawing.Point(112, 48);
            this._cmbSpaces.Name = "_cmbSpaces";
            this._cmbSpaces.Size = new System.Drawing.Size(312, 21);
            this._cmbSpaces.TabIndex = 3;
            this._cmbSpaces.SelectedIndexChanged += new System.EventHandler(this._cmbSpaces_SelectedIndexChanged);
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 172);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "&Title:";
            //
            // _btnPost
            //
            this._btnPost.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnPost.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnPost.Location = new System.Drawing.Point(264, 379);
            this._btnPost.Name = "_btnPost";
            this._btnPost.TabIndex = 10;
            this._btnPost.Text = "Post";
            this._btnPost.Click += new System.EventHandler(this._btnPost_Click);
            //
            // _btnCancel
            //
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(348, 379);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 11;
            this._btnCancel.Text = "Cancel";
            this._btnCancel.Click += new System.EventHandler(this._btnCancel_Click);
            //
            // _edtTitle
            //
            this._edtTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtTitle.Location = new System.Drawing.Point(112, 168);
            this._edtTitle.Name = "_edtTitle";
            this._edtTitle.Size = new System.Drawing.Size(312, 21);
            this._edtTitle.TabIndex = 6;
            this._edtTitle.Text = "";
            this._edtTitle.TextChanged += new System.EventHandler(this.HandleControlChanged);
            //
            // label3
            //
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label3.Location = new System.Drawing.Point(8, 196);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 16);
            this.label3.TabIndex = 7;
            this.label3.Text = "&Content:";
            //
            // _edtContent
            //
            this._edtContent.AcceptsReturn = true;
            this._edtContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtContent.AutoSize = false;
            this._edtContent.Location = new System.Drawing.Point(8, 216);
            this._edtContent.Multiline = true;
            this._edtContent.Name = "_edtContent";
            this._edtContent.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._edtContent.Size = new System.Drawing.Size(416, 152);
            this._edtContent.TabIndex = 8;
            this._edtContent.Text = "";
            this._edtContent.TextChanged += new System.EventHandler(this.HandleControlChanged);
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this._radBlogPost);
            this.groupBox1.Controls.Add(this._radNewPage);
            this.groupBox1.Controls.Add(this._btnBrowse);
            this.groupBox1.Controls.Add(this._edtParent);
            this.groupBox1.Controls.Add(this._lblParent);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox1.Location = new System.Drawing.Point(8, 72);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(416, 88);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Post as";
            //
            // _radBlogPost
            //
            this._radBlogPost.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radBlogPost.Location = new System.Drawing.Point(8, 16);
            this._radBlogPost.Name = "_radBlogPost";
            this._radBlogPost.Size = new System.Drawing.Size(104, 20);
            this._radBlogPost.TabIndex = 0;
            this._radBlogPost.Text = "&Blog Post";
            //
            // _radNewPage
            //
            this._radNewPage.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radNewPage.Location = new System.Drawing.Point(8, 36);
            this._radNewPage.Name = "_radNewPage";
            this._radNewPage.Size = new System.Drawing.Size(104, 20);
            this._radNewPage.TabIndex = 1;
            this._radNewPage.Text = "&New Page";
            this._radNewPage.CheckedChanged += new System.EventHandler(this._radNewPage_CheckedChanged);
            //
            // _btnBrowse
            //
            this._btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnBrowse.Enabled = false;
            this._btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnBrowse.Location = new System.Drawing.Point(332, 57);
            this._btnBrowse.Name = "_btnBrowse";
            this._btnBrowse.TabIndex = 4;
            this._btnBrowse.Text = "B&rowse...";
            this._btnBrowse.Click += new System.EventHandler(this._btnBrowse_Click);
            //
            // _edtParent
            //
            this._edtParent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtParent.Enabled = false;
            this._edtParent.Location = new System.Drawing.Point(112, 56);
            this._edtParent.Name = "_edtParent";
            this._edtParent.ReadOnly = true;
            this._edtParent.Size = new System.Drawing.Size(211, 21);
            this._edtParent.TabIndex = 3;
            this._edtParent.Text = "";
            //
            // _lblParent
            //
            this._lblParent.Enabled = false;
            this._lblParent.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblParent.Location = new System.Drawing.Point(32, 60);
            this._lblParent.Name = "_lblParent";
            this._lblParent.Size = new System.Drawing.Size(68, 16);
            this._lblParent.TabIndex = 2;
            this._lblParent.Text = "Parent Page:";
            //
            // PostDialog
            //
            this.AcceptButton = this._btnPost;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(432, 410);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this._edtContent);
            this.Controls.Add(this._edtTitle);
            this.Controls.Add(this.label3);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnPost);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._cmbSpaces);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._btnLogin);
            this.Controls.Add(this._lblProgress);
            this.Controls.Add(this._lblLogin);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PostDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Post to Confluence";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PostDialog_KeyDown);
            this.VisibleChanged += new System.EventHandler(this.PostDialog_VisibleChanged);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        /// <summary>
        /// Posts the selected resource to Confluence.
        /// </summary>
        /// <param name="res">The resource to post to Confluence.</param>
        public static void PostToConfluence( IResource res, string selectedText )
        {
            PostDialog dlg = new PostDialog();
            dlg.ResourceToPost = res;
            dlg.TextToPost = selectedText;
            dlg.Show();
        }

        /// <summary>
        /// Ensures that the login dialog is shown when the posting dialog is visible,
        /// so that progress for a stored login can be displayed in the dialog.
        /// </summary>
        private void PostDialog_VisibleChanged( object sender, System.EventArgs e )
        {
            VisibleChanged -= new EventHandler( PostDialog_VisibleChanged );
            BeginInvoke( new MethodInvoker( DoLogin ) );
        }

	    private void DoLogin()
	    {
            _lblProgress.Text = "Logging in...";
            _lblProgress.Refresh();

            _loginToken = LoginDialog.GetLoginToken();
            if ( _loginToken == null )
            {
                Close();
            }
            else
            {
                UpdateLogin();
            }
	    }

	    private void UpdateLogin()
	    {
	        _lblLogin.Text = "Logged in as " + LoginManager.UserName + " to " +
	            LoginManager.Url;
            CollectResourceText();
            RefreshSpaceList();
	    }

	    private void RefreshSpaceList()
        {
            _lblProgress.Text = "Refreshing space list...";

            _confluence = new ConfluenceSoap();
            _confluence.Url = LoginManager.ServiceUrl;

            _asyncOperation = true;
            _confluence.BegingetSpaces( _loginToken, new AsyncCallback( GetSpacesDone ), null );
            UpdateButtonStatus();
        }

	    private void GetSpacesDone( IAsyncResult ar )
	    {
            _spaces = _confluence.EndgetSpaces( ar );
            BeginInvoke( new MethodInvoker( DoRefreshSpaceList ) );
	    }

        private void DoRefreshSpaceList()
        {
            string lastSpace = Core.SettingStore.ReadString( "PostToConfluence", "LastSpace", "" );
            _cmbSpaces.Items.Clear();
            foreach( RemoteSpaceSummary remoteSpaceSummary in _spaces )
            {
                _cmbSpaces.Items.Add( new SpaceSummary( remoteSpaceSummary.key, remoteSpaceSummary.name ) );
                if ( lastSpace == remoteSpaceSummary.key )
                {
                    _cmbSpaces.SelectedIndex = _cmbSpaces.Items.Count-1;
                }
            }

            if ( Core.SettingStore.ReadBool( "PostToConfluence", "LastNewPage", true ) )
            {
                _radNewPage.Checked = true;
            }
            else
            {
                _radBlogPost.Checked = true;
            }

            _asyncOperation = false;
            UpdateButtonStatus();
            _lblProgress.Text = "";
        }

	    private void CollectResourceText()
        {
            _lblProgress.Text = "Collecting resource text...";
            _lblProgress.Refresh();

            ResourceTextCollector collector = new ResourceTextCollector();
            if ( _resourceToPost != null )
            {
                Core.PluginLoader.InvokeResourceTextProviders( _resourceToPost, collector );
                _edtTitle.Text = collector.Subject.Replace( ":", " " );
            }

            if ( _textToPost != null && TextToPost.Length > 0 )
            {
                _edtContent.Text = _textToPost;
            }
            else
            {
                _edtContent.Text = collector.Body;
            }
        }

        private void UpdateButtonStatus()
        {
            _btnBrowse.Enabled = !_asyncOperation && _cmbSpaces.SelectedItem != null && _radNewPage.Checked;
            _edtParent.Enabled = _lblParent.Enabled = _radNewPage.Checked && _cmbSpaces.SelectedItem != null;

            _btnLogin.Enabled = !_asyncOperation;
            _btnPost.Enabled = !_asyncOperation && _cmbSpaces.SelectedItem != null &&
                _edtTitle.Text.Length > 0 && _edtContent.Text.Length > 0;
        }

        private void _btnPost_Click( object sender, System.EventArgs e )
        {
            bool postSuccess = false;

            _lblProgress.Text = "Posting...";
            _lblProgress.Refresh();

            ConfluenceSoap confluence = new ConfluenceSoap();
            confluence.Url = LoginManager.ServiceUrl;
            // the login token may have expired, so let's login again, just in case
            string loginToken = confluence.login( LoginManager.UserName, LoginManager.Password );

            try
            {
                if ( _radNewPage.Checked )
                {
                    postSuccess = PostPage( confluence, loginToken );
                }
                else
                {
                    PostBlog( confluence, loginToken );
                    postSuccess = true;
                }
            }
            catch( Exception ex )
            {
                _lblProgress.Text = ex.Message;
            }

            Core.SettingStore.WriteString( "PostToConfluence", "LastSpace", (_cmbSpaces.SelectedItem as SpaceSummary).Key );
            Core.SettingStore.WriteBool( "PostToConfluence", "LastNewPage", _radNewPage.Checked );
            if ( postSuccess )
            {
                Close();
            }
            else
            {
                _asyncOperation = false;
                UpdateButtonStatus();
            }
        }

	    private bool PostPage( ConfluenceSoap confluence, string loginToken )
	    {
	        string spaceKey = (_cmbSpaces.SelectedItem as SpaceSummary).Key;
	        string title = _edtTitle.Text;
	        RemotePage page = new RemotePage();

	        RemotePageSummary[] pageSummaries = confluence.getPages( loginToken, spaceKey );
	        foreach( RemotePageSummary pageSummary in pageSummaries )
	        {
	            if ( pageSummary.title == _edtTitle.Text )
	            {
	                DialogResult confirm = MessageBox.Show( this,
	                                                        "A page named '" + title + "' already exists. Do you want to overwrite it?",
	                                                        "Post to Confluence", MessageBoxButtons.YesNo );
	                if ( confirm != DialogResult.Yes )
	                {
	                    _lblProgress.Text = "";
	                    return false;
	                }
	                page = confluence.getPage( loginToken, pageSummary.id );
	                break;
	            }
	        }

	        page.space    = spaceKey;
	        page.title    = title;
	        page.content  = _edtContent.Text;
	        page.parentId = _parentId;

	        confluence.storePage( loginToken, page );
            return true;
	    }

        private void PostBlog( ConfluenceSoap confluence, string loginToken )
        {
            RemoteBlogEntry entry = new RemoteBlogEntry();
            entry.space   = (_cmbSpaces.SelectedItem as SpaceSummary).Key;
            entry.title   = _edtTitle.Text;
            entry.content = _edtContent.Text;

            confluence.storeBlogEntry( loginToken, entry );
        }

        private void HandleControlChanged( object sender, System.EventArgs e )
        {
            UpdateButtonStatus();
        }

        private void _cmbSpaces_SelectedIndexChanged( object sender, System.EventArgs e )
        {
            UpdateButtonStatus();
            _parentId = 0L;
            _edtParent.Text = "";
        }

        private void _btnBrowse_Click( object sender, System.EventArgs e )
        {
            string spaceKey = (_cmbSpaces.SelectedItem as SpaceSummary).Key;
            RemotePageSummary pageSummary = BrowsePagesDialog.BrowseForPage( this, spaceKey );
            if ( pageSummary != null )
            {
                _parentId = pageSummary.id;
                _edtParent.Text = pageSummary.title;
            }
            else
            {
                _parentId = 0L;
                _edtParent.Text = "";
            }
        }

        private void _btnLogin_Click( object sender, System.EventArgs e )
        {
            string token = LoginDialog.GetLoginTokenFromDialog();
            if ( token != null )
            {
                _loginToken = token;
                UpdateLogin();
            }
        }

        private void _radNewPage_CheckedChanged( object sender, System.EventArgs e )
        {
            UpdateButtonStatus();
        }

        private void PostDialog_KeyDown( object sender, System.Windows.Forms.KeyEventArgs e )
        {
            if ( e.KeyData == ( Keys.Control | Keys.Enter ) )
            {
                _asyncOperation = true;
                UpdateButtonStatus();
                _btnPost_Click( sender, e );
            }
        }

        private void _btnCancel_Click(object sender, System.EventArgs e)
        {
            Close();
        }

	    public IResource ResourceToPost
	    {
	        set { _resourceToPost = value; }
	    }

	    public string TextToPost
	    {
	        get { return _textToPost; }
	        set { _textToPost = value; }
	    }

	    private class SpaceSummary
        {
            private string _key;
            private string _name;

            public SpaceSummary( string key, string name )
            {
                _key = key;
                _name = name;
            }

            internal string Key
            {
                get { return _key; }
            }

            public override string ToString()
            {
                return _name;
            }
        }

        private class ResourceTextCollector: IResourceTextConsumer
        {
            private StringBuilder _subjectBuilder = new StringBuilder();
            private StringBuilder _bodyBuilder = new StringBuilder();

            public void AddDocumentHeading( int resourceId, string text )
            {
                _subjectBuilder.Append( text );
            }

            public void AddDocumentFragment( int resourceId, string text )
            {
                _bodyBuilder.Append( text );
            }

            public void AddDocumentFragment( int resourceId, string text, string sectionName )
            {
                if ( sectionName == DocumentSection.BodySection )
                {
                    _bodyBuilder.Append( text );
                }
                else if ( sectionName == DocumentSection.SubjectSection )
                {
                    _subjectBuilder.Append( text );
                }
            }

            public void IncrementOffset( int inc )
            {
            }

            public void RestartOffsetCounting()
            {
            }

            public void RejectResult()
            {
            }

            public TextRequestPurpose Purpose
            {
                get { return TextRequestPurpose.Indexing; }
            }

            internal string Subject
            {
                get { return _subjectBuilder.ToString(); }
            }

            internal string Body
            {
                get { return _bodyBuilder.ToString(); }
            }
        }
    }
}
