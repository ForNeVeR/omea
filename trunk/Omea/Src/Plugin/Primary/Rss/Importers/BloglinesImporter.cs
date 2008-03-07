/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Summary description for BloglinesImporter.
	/// </summary>
	internal class BloglinesImporterPane : AbstractOptionsPane
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox _txtLogin;
        private System.Windows.Forms.TextBox _txtPasswd;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        BloglinesImporter _importer = null;

		public BloglinesImporterPane( BloglinesImporter importer )
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            _importer = importer;
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
            this.label2 = new System.Windows.Forms.Label();
            this._txtLogin = new System.Windows.Forms.TextBox();
            this._txtPasswd = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(120, 23);
            this.label1.TabIndex = 0;
            this.label1.Text = "Bloglines login:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 23);
            this.label2.TabIndex = 1;
            this.label2.Text = "Bloglines password:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _txtLogin
            // 
            this._txtLogin.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._txtLogin.Location = new System.Drawing.Point(136, 8);
            this._txtLogin.Name = "_txtLogin";
            this._txtLogin.Size = new System.Drawing.Size(232, 24);
            this._txtLogin.TabIndex = 2;
            this._txtLogin.Text = "";
            // 
            // _txtPasswd
            // 
            this._txtPasswd.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._txtPasswd.Location = new System.Drawing.Point(136, 32);
            this._txtPasswd.Name = "_txtPasswd";
            this._txtPasswd.PasswordChar = '*';
            this._txtPasswd.Size = new System.Drawing.Size(232, 24);
            this._txtPasswd.TabIndex = 3;
            this._txtPasswd.Text = "";
            // 
            // BloglinesImporterPane
            // 
            this.Controls.Add(this._txtPasswd);
            this.Controls.Add(this._txtLogin);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "BloglinesImporterPane";
            this.Size = new System.Drawing.Size(376, 64);
            this.ResumeLayout(false);

        }
		#endregion

        public override void LeavePane()
        {
            _importer.Login = _txtLogin.Text;
            _importer.Password = _txtPasswd.Text;
        }
	}

    internal class BloglinesImporter : IFeedImporter
    {
        private const string _progressMessage = "Importing Bloglines subscriptions";
        private const string _ImportURL = "http://rpc.bloglines.com/listsubs";

        private string _login = "";
        private string _password = "";

        internal string Login
        {
            get { return _login; }
            set { _login = value; }
        }

        internal string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        internal BloglinesImporter()
        {
            RSSPlugin.GetInstance().RegisterFeedImporter( "Bloglines", this );
        }

        #region IFeedImporter implementation
        /// <summary>
        /// Check if importer needs configuration before import starts.
        /// </summary>
        public bool HasSettings
        {
            get { return true; }
        }

        /// <summary>
        /// Returns creator of options pane.
        /// </summary>
        public OptionsPaneCreator GetSettingsPaneCreator()
        {
            return new OptionsPaneCreator( this.CreateOptionPane );
        }

        /// <summary>
        /// Import subscription
        /// </summary>
        public void DoImport( IResource importRoot, bool addToWorkspace )
        {
            if( _login.Length == 0 || _password.Length == 0 )
            {
                return;
            }
            RSSPlugin plugin = RSSPlugin.GetInstance();
            string authInfo = Convert.ToBase64String( Encoding.ASCII.GetBytes( _login + ":" + _password ) );

            ImportUtils.UpdateProgress( 0, _progressMessage );

            importRoot = plugin.FindOrCreateGroup( "Bloglines Subscriptions", importRoot );

            ImportUtils.UpdateProgress( 10, _progressMessage );

            WebClient client = new WebClient();
            client.Headers.Add( "Authorization", "basic " + authInfo );
            
            ImportUtils.UpdateProgress( 20, _progressMessage );

            try
            {
                Stream stream = client.OpenRead( _ImportURL );
                ImportUtils.UpdateProgress( 30, _progressMessage );
                OPMLProcessor.Import( new StreamReader(stream), importRoot, addToWorkspace );
                ImportUtils.UpdateProgress( 90, _progressMessage );
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "BlogLines subscrption load failed: '" + ex.Message + "'" );
                RemoveFeedsAndGroupsAction.DeleteFeedGroup( importRoot );

                string message = "Import of BlogLines subscription failed:\n" + ex.Message;
                if( ex is WebException )
                {
                    WebException e = (WebException)ex;
                    if( e.Status == WebExceptionStatus.ProtocolError &&
                        ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.Unauthorized )
                    {
                        message = "Import of BlogLines subscription failed:\nInvalid login or password.";
                    }
                }
                ImportUtils.ReportError( "BlogLines Subscription Import", message );
            }
            ImportUtils.UpdateProgress( 100, _progressMessage );
            return;
        }

        /// <summary>
        /// Import cached items, flags, etc.
        /// </summary>
        public void DoImportCache()
        {
            // Do nothing for bloglines
        }

        #endregion

        private AbstractOptionsPane CreateOptionPane()
        {
            return new BloglinesImporterPane( this );
        }

    }
}
