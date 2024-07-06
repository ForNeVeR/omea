// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Summary description for PostCommentForm.
	/// </summary>
	public class PostCommentForm : DialogBase
	{
        private System.Windows.Forms.Button _btnSend;
        private System.Windows.Forms.Button _btnClose;
        private System.Windows.Forms.TextBox _body;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox _cmbFrom;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox _txtRssFeed;
        private System.Windows.Forms.TextBox _txtSubject;
        private System.Windows.Forms.TextBox _txtName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox _txtLink;
        private System.Windows.Forms.TextBox _txtOnPost;
        private System.Windows.Forms.Label label5;

        private readonly string _url;

        /// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public PostCommentForm( IResource item )
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
            _url = item.GetStringProp( Props.WfwComment );
            SetToField( item );
            _txtSubject.Text = "Re: " + item.DisplayName;
            Text = _txtSubject.Text;
		    IResourceList emailList = Core.ContactManager.MySelf.Resource.GetLinksOfType( "EmailAccount", "EmailAcct" );
            foreach ( IResource address in emailList )
            {
                _cmbFrom.Items.Add( address );
            }
            string name = Settings.SendFrom;
            if ( string.IsNullOrEmpty( name ) )
            {
                name = Core.ContactManager.MySelf.Resource.DisplayName;
            }
            _txtName.Text = name;
            string sendEmail = Settings.SendEmail;
            if ( string.IsNullOrEmpty( sendEmail ) )
            {
                sendEmail = Core.ContactManager.MySelf.DefaultEmailAddress;
            }
            _cmbFrom.Text = sendEmail;
            string sendHomePage = Settings.SendHomePage;
            if ( sendHomePage == null )
            {
                sendHomePage = Core.ContactManager.MySelf.HomePage;
            }
            _txtLink.Text = sendHomePage;
            RestoreSettings();
		}

        private void SetToField( IResource item )
        {

            _txtOnPost.Text = item.DisplayName;
            IResource feed = item.GetLinkProp( -Props.RSSItem );
            if ( feed != null )
            {
                _txtRssFeed.Text += feed.DisplayName;
            }
        }

        public static void CreateNewComment( IResource item )
        {
            PostCommentForm form = new PostCommentForm( item );
            form.Show();
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
            this._btnSend = new System.Windows.Forms.Button();
            this._btnClose = new System.Windows.Forms.Button();
            this._body = new System.Windows.Forms.TextBox();
            this._cmbFrom = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this._txtSubject = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this._txtRssFeed = new System.Windows.Forms.TextBox();
            this._txtName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this._txtLink = new System.Windows.Forms.TextBox();
            this._txtOnPost = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // _btnSend
            //
            this._btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnSend.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnSend.Location = new System.Drawing.Point(312, 236);
            this._btnSend.Name = "_btnSend";
            this._btnSend.TabIndex = 3;
            this._btnSend.Text = "Send";
            this._btnSend.Click += new System.EventHandler(this.OnClick);
            //
            // _btnClose
            //
            this._btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnClose.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnClose.Location = new System.Drawing.Point(400, 236);
            this._btnClose.Name = "_btnClose";
            this._btnClose.TabIndex = 4;
            this._btnClose.Text = "Close";
            this._btnClose.Click += new System.EventHandler(this.OnClose);
            //
            // _body
            //
            this._body.AcceptsReturn = true;
            this._body.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._body.Location = new System.Drawing.Point(8, 120);
            this._body.Multiline = true;
            this._body.Name = "_body";
            this._body.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this._body.Size = new System.Drawing.Size(468, 80);
            this._body.TabIndex = 0;
            this._body.Text = "";
            //
            // _cmbFrom
            //
            this._cmbFrom.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._cmbFrom.Location = new System.Drawing.Point(272, 8);
            this._cmbFrom.Name = "_cmbFrom";
            this._cmbFrom.Size = new System.Drawing.Size(204, 21);
            this._cmbFrom.TabIndex = 7;
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 16);
            this.label1.TabIndex = 5;
            this.label1.Text = "&From:";
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 96);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 16);
            this.label2.TabIndex = 12;
            this.label2.Text = "&Subject:";
            //
            // _txtSubject
            //
            this._txtSubject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._txtSubject.Location = new System.Drawing.Point(80, 92);
            this._txtSubject.Name = "_txtSubject";
            this._txtSubject.Size = new System.Drawing.Size(396, 21);
            this._txtSubject.TabIndex = 13;
            this._txtSubject.Text = "";
            this._txtSubject.TextChanged += new System.EventHandler(this.OnTextChanged);
            //
            // label3
            //
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(8, 40);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 16);
            this.label3.TabIndex = 8;
            this.label3.Text = "&To feed:";
            //
            // _txtRssFeed
            //
            this._txtRssFeed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._txtRssFeed.Location = new System.Drawing.Point(80, 36);
            this._txtRssFeed.Name = "_txtRssFeed";
            this._txtRssFeed.ReadOnly = true;
            this._txtRssFeed.Size = new System.Drawing.Size(396, 21);
            this._txtRssFeed.TabIndex = 9;
            this._txtRssFeed.Text = "";
            //
            // _txtName
            //
            this._txtName.Location = new System.Drawing.Point(80, 8);
            this._txtName.Name = "_txtName";
            this._txtName.Size = new System.Drawing.Size(188, 21);
            this._txtName.TabIndex = 6;
            this._txtName.Text = "";
            //
            // label4
            //
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Location = new System.Drawing.Point(12, 212);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(68, 16);
            this.label4.TabIndex = 1;
            this.label4.Text = "&My web page:";
            //
            // _txtLink
            //
            this._txtLink.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._txtLink.Location = new System.Drawing.Point(80, 208);
            this._txtLink.Name = "_txtLink";
            this._txtLink.Size = new System.Drawing.Size(396, 21);
            this._txtLink.TabIndex = 2;
            this._txtLink.Text = "";
            //
            // _txtOnPost
            //
            this._txtOnPost.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._txtOnPost.Location = new System.Drawing.Point(80, 64);
            this._txtOnPost.Name = "_txtOnPost";
            this._txtOnPost.ReadOnly = true;
            this._txtOnPost.Size = new System.Drawing.Size(396, 21);
            this._txtOnPost.TabIndex = 11;
            this._txtOnPost.Text = "";
            //
            // label5
            //
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label5.Location = new System.Drawing.Point(8, 68);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(48, 16);
            this.label5.TabIndex = 10;
            this.label5.Text = "&On post:";
            //
            // PostCommentForm
            //
            this.AcceptButton = this._btnSend;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnClose;
            this.ClientSize = new System.Drawing.Size(484, 266);
            this.Controls.Add(this._txtOnPost);
            this.Controls.Add(this._txtLink);
            this.Controls.Add(this._txtName);
            this.Controls.Add(this._txtRssFeed);
            this.Controls.Add(this._txtSubject);
            this.Controls.Add(this._body);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._cmbFrom);
            this.Controls.Add(this._btnClose);
            this.Controls.Add(this._btnSend);
            this.MinimumSize = new System.Drawing.Size(492, 236);
            this.Name = "PostCommentForm";
            this.ShowInTaskbar = true;
            this.Text = "Post New Comment";
            this.ResumeLayout(false);

        }
		#endregion

        private void OnClick(object sender, System.EventArgs e)
        {
            string author = _txtName.Text + "<" + _cmbFrom.Text + ">";

            Settings.SendHomePage.Save( _txtLink.Text );
            Settings.SendEmail.Save( _cmbFrom.Text );
            Settings.SendFrom.Save( _txtName.Text );

            WebPost.PostNewComment( _url, _txtSubject.Text, author, _txtLink.Text, _body.Text );
            Close();
        }

        private void OnClose(object sender, System.EventArgs e)
        {
            Close();
        }

        private void OnTextChanged(object sender, System.EventArgs e)
        {
            Text = _txtSubject.Text;
        }
	}
}
