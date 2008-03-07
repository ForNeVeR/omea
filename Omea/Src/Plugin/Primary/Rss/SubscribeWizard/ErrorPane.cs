/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Web;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin.SubscribeWizard
{
	/// <summary>
	/// Summary description for ErrorPane.
	/// </summary>
	internal class ErrorPane : System.Windows.Forms.UserControl
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label _lblErrorMessage;
        private System.Windows.Forms.Label _lblExceptionMessage;
        private JetLinkLabel _lblValidate;
        private JetLinkLabel _lnkExistingFeed;
        private string _feedUrl;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ErrorPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

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
            this._lblErrorMessage = new System.Windows.Forms.Label();
            this._lblExceptionMessage = new System.Windows.Forms.Label();
            this._lblValidate = new JetLinkLabel();
            this._lnkExistingFeed = new JetLinkLabel();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(360, 32);
            this.label1.TabIndex = 0;
            this.label1.Text = "Error when subscribing to feed:";
            this.label1.UseMnemonic = false;
            // 
            // _lblErrorMessage
            // 
            this._lblErrorMessage.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblErrorMessage.Location = new System.Drawing.Point(20, 64);
            this._lblErrorMessage.Name = "_lblErrorMessage";
            this._lblErrorMessage.Size = new System.Drawing.Size(352, 76);
            this._lblErrorMessage.TabIndex = 1;
            // 
            // _lblExceptionMessage
            // 
            this._lblExceptionMessage.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblExceptionMessage.Location = new System.Drawing.Point(20, 144);
            this._lblExceptionMessage.Name = "_lblExceptionMessage";
            this._lblExceptionMessage.Size = new System.Drawing.Size(352, 76);
            this._lblExceptionMessage.TabIndex = 2;
            // 
            // _lblValidate
            // 
            this._lblValidate.Location = new System.Drawing.Point(16, 224);
            this._lblValidate.Name = "_lblValidate";
            this._lblValidate.Size = new System.Drawing.Size(100, 16);
            this._lblValidate.TabIndex = 3;
            this._lblValidate.Text = "Validate";
            this._lblValidate.Visible = false;
            this._lblValidate.Click += new EventHandler(_lblValidate_Click);
            //
            // _lblExistingFeed
            //
            this._lnkExistingFeed.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            this._lnkExistingFeed.Location = new Point(20, 88);
            this._lnkExistingFeed.Name = "_lnkExistingFeed";
            this._lnkExistingFeed.Size = new Size(352, 20);
            this._lnkExistingFeed.TabIndex = 4;
            this._lnkExistingFeed.Visible = false;
            this._lnkExistingFeed.Click += new EventHandler( HandleExistingFeedClick );
            // 
            // ErrorPane
            // 
            this.Controls.Add(this._lblValidate);
            this.Controls.Add(this._lblExceptionMessage);
            this.Controls.Add(this._lblErrorMessage);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._lnkExistingFeed);
            this.Name = "ErrorPane";
            this.Size = new System.Drawing.Size(384, 396);
            this.ResumeLayout(false);

        }

	    #endregion

        public string FeedUrl
        {
            set
            {
                label1.Text = "Error when subscribing to feed for " + value + ":";
                _feedUrl = value;
            }
        }

        public string ErrorMessage
        {
            get { return _lblErrorMessage.Text; }
            set { _lblErrorMessage.Text = value; }
        }

        public string ExceptionMessage
        {
            get { return _lblExceptionMessage.Text; }
            set { _lblExceptionMessage.Text = value; }
        }

        public bool ShowValidateLink
        {
            get { return _lblValidate.Visible; }
            set { _lblValidate.Visible = value; }
        }

        private void _lblValidate_Click( object sender, EventArgs e )
        {
			Core.UIManager.OpenInNewBrowserWindow( "http://feedvalidator.org/check.cgi?url=" + HttpUtility.UrlEncode( _feedUrl ) );
        }

        public void SetExistingFeedLink( IResource existingFeed )
        {
            if ( existingFeed == null )
            {
                _lblErrorMessage.Height = 76;
                _lnkExistingFeed.Visible = false;
                _lnkExistingFeed.Tag = null;
            }
            else
            {
                _lblErrorMessage.Height = 20;
                _lnkExistingFeed.Visible = true;
                _lnkExistingFeed.Text = existingFeed.DisplayName;
                _lnkExistingFeed.Tag = existingFeed;
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
    }
}
