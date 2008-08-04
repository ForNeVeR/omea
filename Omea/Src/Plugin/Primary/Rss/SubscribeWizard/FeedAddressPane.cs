/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin.SubscribeWizard
{
	/// <summary>
	/// Summary description for FeedAddressPane.
	/// </summary>
	public class FeedAddressPane : UserControl
	{
        private TextBox     _edtURL;
        private Label       label1;
        private Label       label2;
        private Label       _lblError;
        private Label       _lblProgress;
        private CheckBox    _chkAuthentication;
        private GroupBox    _grpLogin;
        private Label       _lblUserName, _lblPassword;
        private TextBox     _edtUserName, _edtPassword;
        private JetLinkLabel _lnkExistingFeed;
        private ToolTip      _tipForFullFeedPath;
		private IContainer   components = null;

        public event EventHandler NextPage;

        //  Any step pane must be able to control the possibility to move
        //  further (via button Next) depending on the internal state.
        internal SubscribeForm.CanMoveNextDelegate  NextPredicate;

		public FeedAddressPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            label2.Text = "If you don\'t know the address of a feed, you can enter the address of the Web site, and "
                          + Core.ProductName + " will try to discover the feed automatically.";
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
            this._edtURL = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this._lblError = new System.Windows.Forms.Label();
            this._lblProgress = new System.Windows.Forms.Label();
            this._chkAuthentication = new System.Windows.Forms.CheckBox();
            this._grpLogin = new System.Windows.Forms.GroupBox();
            this._edtPassword = new System.Windows.Forms.TextBox();
            this._lblPassword = new System.Windows.Forms.Label();
            this._edtUserName = new System.Windows.Forms.TextBox();
            this._lblUserName = new System.Windows.Forms.Label();
            this._lnkExistingFeed = new JetLinkLabel();
			components = new Container();
			_tipForFullFeedPath = new ToolTip( components );
            this._grpLogin.SuspendLayout();
            this.SuspendLayout();
            // 
            // _edtURL
            // 
            this._edtURL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtURL.Location = new System.Drawing.Point(12, 48);
            this._edtURL.Name = "_edtURL";
            this._edtURL.Size = new System.Drawing.Size(344, 20);
            this._edtURL.TabIndex = 2;
            this._edtURL.Text = "";
            this._edtURL.KeyDown += new System.Windows.Forms.KeyEventHandler(this._edtURL_KeyDown);
            this._edtURL.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this._edtURL_KeyPress);
            this._edtURL.TextChanged +=new EventHandler(_edtURL_TextChanged);
            this._edtURL.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(12, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(372, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Enter the address of an RSS or ATOM feed:";
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(12, 84);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(344, 32);
            this.label2.TabIndex = 3;
            this.label2.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            // 
            // _lblError
            // 
            this._lblError.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblError.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._lblError.Location = new System.Drawing.Point(24, 224);
            this._lblError.Name = "_lblError";
            this._lblError.Size = new System.Drawing.Size(332, 92);
            this._lblError.TabIndex = 4;
            this._lblError.Text = "label3";
            this._lblError.Visible = false;
            this._lblError.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this._lblError.ForeColor = Color.Red;
            // 
            // _lblProgress
            // 
            this._lblProgress.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblProgress.Location = new System.Drawing.Point(24, 324);
            this._lblProgress.Name = "_lblProgress";
            this._lblProgress.Size = new System.Drawing.Size(336, 48);
            this._lblProgress.TabIndex = 5;
            // 
            // _chkAuthentication
            // 
            this._chkAuthentication.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkAuthentication.Location = new System.Drawing.Point(12, 120);
            this._chkAuthentication.Name = "_chkAuthentication";
            this._chkAuthentication.Size = new System.Drawing.Size(344, 16);
            this._chkAuthentication.TabIndex = 6;
            this._chkAuthentication.Text = "The feed requires an HTTP login";
            this._chkAuthentication.CheckedChanged += new System.EventHandler(this._chkAuthentication_CheckedChanged);
            // 
            // _grpLogin
            // 
            this._grpLogin.Controls.Add(this._edtPassword);
            this._grpLogin.Controls.Add(this._lblPassword);
            this._grpLogin.Controls.Add(this._edtUserName);
            this._grpLogin.Controls.Add(this._lblUserName);
            this._grpLogin.Enabled = false;
            this._grpLogin.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._grpLogin.Location = new System.Drawing.Point(32, 140);
            this._grpLogin.Name = "_grpLogin";
            this._grpLogin.Size = new System.Drawing.Size(324, 76);
            this._grpLogin.TabIndex = 7;
            this._grpLogin.TabStop = false;
            this._grpLogin.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            // 
            // _edtPassword
            // 
            this._edtPassword.Location = new System.Drawing.Point(116, 44);
            this._edtPassword.Name = "_edtPassword";
            this._edtPassword.PasswordChar = '*';
            this._edtPassword.Size = new System.Drawing.Size(200, 20);
            this._edtPassword.TabIndex = 3;
            this._edtPassword.Text = "";
            this._edtPassword.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            // 
            // label4
            // 
            this._lblPassword.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblPassword.Location = new System.Drawing.Point(8, 48);
            this._lblPassword.Name = "_lblPassword";
            this._lblPassword.Size = new System.Drawing.Size(100, 16);
            this._lblPassword.TabIndex = 2;
            this._lblPassword.Text = "Password:";
            // 
            // _edtUserName
            // 
            this._edtUserName.Location = new System.Drawing.Point(116, 16);
            this._edtUserName.Name = "_edtUserName";
            this._edtUserName.Size = new System.Drawing.Size(200, 20);
            this._edtUserName.TabIndex = 1;
            this._edtUserName.Text = "";
            this._edtUserName.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            // 
            // label3
            // 
            this._lblUserName.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblUserName.Location = new System.Drawing.Point(8, 20);
            this._lblUserName.Name = "_lblUserName";
            this._lblUserName.Size = new System.Drawing.Size(100, 16);
            this._lblUserName.TabIndex = 0;
            this._lblUserName.Text = "User name:";
            // 
            // _lnkExistingFeed
            // 
            this._lnkExistingFeed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lnkExistingFeed.Click += new EventHandler( HandleExistingFeedClick );
            this._lnkExistingFeed.Location = new System.Drawing.Point(24, 248);
            this._lnkExistingFeed.Name = "_lnkExistingFeed";
            this._lnkExistingFeed.Size = new System.Drawing.Size(328, 16);
            this._lnkExistingFeed.TabIndex = 8;
            this._lnkExistingFeed.Text = "label5";
            this._lnkExistingFeed.Visible = false;
            //
            _tipForFullFeedPath.ShowAlways = true;
            _tipForFullFeedPath.InitialDelay = 0;
            // 
            // FeedAddressPane
            // 
            this.Controls.Add(this._lnkExistingFeed);
            this.Controls.Add(this._grpLogin);
            this.Controls.Add(this._chkAuthentication);
            this.Controls.Add(this._lblProgress);
            this.Controls.Add(this._lblError);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._edtURL);
            this.Controls.Add(this.label1);
            this.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.Name = "FeedAddressPane";
            this.Size = new System.Drawing.Size(384, 396);
            this._grpLogin.ResumeLayout(false);
            this.ResumeLayout(false);

        }
	    #endregion

        #region Key pressing handling
        private void _edtURL_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyData == Keys.Enter )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( OnNextPage ) );
            }
        }

        private void _edtURL_KeyPress( object sender, KeyPressEventArgs e )
        {
            ErrorMessage = "";
            if ( e.KeyChar == '\r' )
            {
                e.Handled = true;
            }
        }

        private void _edtURL_TextChanged(object sender, EventArgs e)
        {
            NextPredicate( !String.IsNullOrEmpty( _edtURL.Text ) );
        }
        #endregion Key pressing handling

	    private void OnNextPage()
	    {
	        if ( NextPage != null )
	        {
	            NextPage( this, EventArgs.Empty );
	        }
	    }

	    public string FeedUrl
        {
            get { return _edtURL.Text.Trim(); }
            set { _edtURL.Text = value; }
        }

        public string ErrorMessage
        {
            get { return ""; }
            set
            {
                if ( value == null )
                {
                    _lblError.Visible = false;
                }
                else
                {
                    _lblError.Text = value;
                    _lblError.Visible = true;
                }
            }
        }

        public void SetExistingFeedLink( IResource existingFeed )
        {
            if ( existingFeed == null )
            {
                _lnkExistingFeed.Visible = false;
                _lnkExistingFeed.Tag = null;
                _tipForFullFeedPath.SetToolTip( _lnkExistingFeed, null );
            }
            else
            {
                _lnkExistingFeed.Visible = true;
                _lnkExistingFeed.Text = existingFeed.DisplayName;
                _lnkExistingFeed.Tag = existingFeed;

                string fullPath = existingFeed.DisplayName;
                IResource parent = existingFeed.GetLinkProp( Core.Props.Parent );
                while( parent != null && parent.Type == Props.RSSFeedGroupResource )
                {
                    fullPath = parent.DisplayName + "/" + fullPath;
                    parent = parent.GetLinkProp( Core.Props.Parent );
                }
                _tipForFullFeedPath.SetToolTip( _lnkExistingFeed, fullPath );
            }
        }

        private void HandleExistingFeedClick( object sender, EventArgs e )
        {
            if ( _lnkExistingFeed.Tag != null )
            {
                FindForm().Close();
                Core.UIManager.DisplayResourceInContext( (IResource) _lnkExistingFeed.Tag );
            }
        }

        public Label ProgressLabel
        {
            get { return _lblProgress; }
        }

        public bool ControlsEnabled
        {
            get { return _edtURL.Enabled; }
            set { _edtURL.Enabled = value; }
        }

        public void UnselectFeedUrl()
        {
            _edtURL.SelectionStart = _edtURL.Text.Length;	        
        }

        private void _chkAuthentication_CheckedChanged( object sender, EventArgs e )
        {
            _grpLogin.Enabled = _chkAuthentication.Checked;
        }

	    public bool RequiresAuthentication
	    {
            get { return _chkAuthentication.Checked; }
	    }

	    public string UserName
	    {
            get { return _edtUserName.Text; }
	    }

	    public string Password
	    {
            get { return _edtPassword.Text; }
        }
    }
}
