// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using PostToConfluence.com.atlassian.confluence;

namespace JetBrains.Omea.SamplePlugins.PostToConfluence
{
	/// <summary>
	/// The dialog for entering the Confluence URL and login information
	/// </summary>
	public class LoginDialog : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox _edtUrl;
        private System.Windows.Forms.TextBox _edtUserName;
        private System.Windows.Forms.TextBox _edtPassword;
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button _btnCancel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Label _lblProgress;
        private string _loginToken = null;

		public LoginDialog()
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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(LoginDialog));
            this.label1 = new System.Windows.Forms.Label();
            this._edtUrl = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this._edtUserName = new System.Windows.Forms.TextBox();
            this._edtPassword = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._lblProgress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(172, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Address of the Confluence site:";
            //
            // _edtUrl
            //
            this._edtUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtUrl.Location = new System.Drawing.Point(8, 28);
            this._edtUrl.Name = "_edtUrl";
            this._edtUrl.Size = new System.Drawing.Size(394, 21);
            this._edtUrl.TabIndex = 1;
            this._edtUrl.Text = "";
            this._edtUrl.TextChanged += new System.EventHandler(this._edtUserName_TextChanged);
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 60);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "User Name:";
            //
            // _edtUserName
            //
            this._edtUserName.Location = new System.Drawing.Point(116, 56);
            this._edtUserName.Name = "_edtUserName";
            this._edtUserName.Size = new System.Drawing.Size(140, 21);
            this._edtUserName.TabIndex = 3;
            this._edtUserName.Text = "";
            this._edtUserName.TextChanged += new System.EventHandler(this._edtUserName_TextChanged);
            //
            // _edtPassword
            //
            this._edtPassword.Location = new System.Drawing.Point(116, 80);
            this._edtPassword.Name = "_edtPassword";
            this._edtPassword.PasswordChar = '*';
            this._edtPassword.Size = new System.Drawing.Size(140, 21);
            this._edtPassword.TabIndex = 5;
            this._edtPassword.Text = "";
            this._edtPassword.TextChanged += new System.EventHandler(this._edtUserName_TextChanged);
            //
            // label3
            //
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(8, 84);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 16);
            this.label3.TabIndex = 4;
            this.label3.Text = "Password:";
            //
            // _btnOK
            //
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(244, 136);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 6;
            this._btnOK.Text = "OK";
            this._btnOK.Click += new System.EventHandler(this._btnOK_Click);
            //
            // _btnCancel
            //
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(328, 136);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 7;
            this._btnCancel.Text = "Cancel";
            //
            // _lblProgress
            //
            this._lblProgress.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblProgress.Location = new System.Drawing.Point(4, 112);
            this._lblProgress.Name = "_lblProgress";
            this._lblProgress.Size = new System.Drawing.Size(396, 17);
            this._lblProgress.TabIndex = 8;
            this._lblProgress.Text = "Logging in...";
            this._lblProgress.Visible = false;
            //
            // LoginDialog
            //
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(412, 167);
            this.Controls.Add(this._lblProgress);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._edtPassword);
            this.Controls.Add(this.label3);
            this.Controls.Add(this._edtUserName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._edtUrl);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(800, 196);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(100, 176);
            this.Name = "LoginDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Login to Confluence";
            this.ResumeLayout(false);

        }
		#endregion

        public string LoginToken
        {
            get { return _loginToken; }
        }

        private void ShowSettings()
        {
            _edtUrl.Text = LoginManager.Url;
            _edtUserName.Text = LoginManager.UserName;
            _edtPassword.Text = LoginManager.Password;
            UpdateButtonStatus();
        }

	    private void UpdateButtonStatus()
	    {
	        _btnOK.Enabled = _edtUrl.Text.Length > 0 && _edtUserName.Text.Length > 0 &&
                _edtPassword.Text.Length > 0;
	    }

        private void _edtUserName_TextChanged(object sender, System.EventArgs e)
        {
            UpdateButtonStatus();
        }

        private void _btnOK_Click( object sender, System.EventArgs e )
        {
            ConfluenceSoap confluence = new ConfluenceSoap();
            try
            {
                _lblProgress.Visible = true;
                _lblProgress.Refresh();

                if ( _edtUrl.Text.IndexOf( "://") < 0 )
                {
                    _edtUrl.Text = "http://" + _edtUrl.Text;
                }

                confluence.Url = _edtUrl.Text + LoginManager.ServicePath;

                _loginToken = confluence.login( _edtUserName.Text, _edtPassword.Text );
            }
            catch( Exception ex )
            {
                _lblProgress.Text = ex.Message;
                return;
            }
            LoginManager.Url = _edtUrl.Text;
            LoginManager.UserName = _edtUserName.Text;
            LoginManager.Password = _edtPassword.Text;
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Returns the login token for Confluence Remote API calls. If there is no
        /// stored login information, or the stored login information is not valid,
        /// </summary>
        /// <returns>The login token or null if the login was cancelled by the user</returns>
        public static string GetLoginToken()
        {
            string url = LoginManager.Url;
            string userName = LoginManager.UserName;
            string password = LoginManager.Password;

            // try stored login information
            if ( url.Length > 0 && userName.Length > 0 && password.Length > 0 )
            {
                ConfluenceSoap confluence = new ConfluenceSoap();
                confluence.Url = LoginManager.ServiceUrl;
                try
                {
                    return confluence.login( userName, password );
                }
                catch( Exception )
                {
                    // ignore
                }
            }

            return GetLoginTokenFromDialog();
        }

        /// <summary>
        /// Shows the login dialog to the user, verifies the login information entered by the user
        /// and returns the login token for the Confluence service.
        /// </summary>
        /// <returns>The login token or null if the login was cancelled by the user.</returns>
        public static string GetLoginTokenFromDialog()
        {
            LoginDialog dlg = new LoginDialog();
            using( dlg )
            {
                dlg.ShowSettings();

                if( dlg.ShowDialog( Core.MainWindow ) != DialogResult.OK )
                {
                    return null;
                }

                return dlg.LoginToken;
            }
        }
	}

    /// <summary>
    /// The class which manages storing the service URL and login settings.
    /// </summary>
    internal class LoginManager
    {
        internal const string ServicePath = "/rpc/soap/confluenceservice-v1";

        internal static string Url
        {
            get { return Core.SettingStore.ReadString( "PostToConfluence", "URL", "" ); }
            set { Core.SettingStore.WriteString( "PostToConfluence", "URL", value ); }
        }

        internal static string ServiceUrl
        {
            get { return Url + ServicePath; }
        }

        internal static string UserName
        {
            get { return Core.SettingStore.ReadString( "PostToConfluence", "UserName", "" ); }
            set { Core.SettingStore.WriteString( "PostToConfluence", "UserName", value ); }
        }

        public static string Password
        {
            get { return Core.SettingStore.ReadString( "PostToConfluence", "Password", "" ); }
            set { Core.SettingStore.WriteString( "PostToConfluence", "Password", value ); }
        }

        public static ConfluenceSoap GetConfluenceService()
        {
            ConfluenceSoap confluence = new ConfluenceSoap();
            confluence.Url = ServiceUrl;
            return confluence;
        }
    }
}
