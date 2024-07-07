// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Contacts;
using System.Diagnostics;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.ContactsPlugin
{
    /**
     * Contact view block for editing Birthday and Homepage.
     */

    internal class DetailsBlock : AbstractContactViewBlock
	{
        private System.Windows.Forms.Label labelBirthday;
        private System.Windows.Forms.Label labelHomePage;
        private System.Windows.Forms.Label labelBirthdayDate;
        private PropertyEditor _homePage;
        private DatePickerCtrl _birthday;
        private JetLinkLabel _homePageLinkLabel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private IResource _contact;
        private const string HTTP = "http:";

		public DetailsBlock()
		{
			InitializeComponent();
		}

        public static AbstractContactViewBlock CreateBlock()
        {
            return new DetailsBlock();
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
            this._homePageLinkLabel = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this.labelBirthday = new System.Windows.Forms.Label();
            this.labelHomePage = new System.Windows.Forms.Label();
            this._homePage = new JetBrains.Omea.ContactsPlugin.PropertyEditor();
            this._birthday = new JetBrains.Omea.GUIControls.DatePickerCtrl();
            this.labelBirthdayDate = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // _homePageLinkLabel
            //
            this._homePageLinkLabel.AllowDrop = true;
            this._homePageLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._homePageLinkLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this._homePageLinkLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._homePageLinkLabel.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(70)), ((System.Byte)(70)), ((System.Byte)(211)));
            this._homePageLinkLabel.Location = new System.Drawing.Point(92, 28);
            this._homePageLinkLabel.Name = "_homePageLinkLabel";
            this._homePageLinkLabel.Size = new System.Drawing.Size(96, 16);
            this._homePageLinkLabel.TabIndex = 24;
            this._homePageLinkLabel.Click += new System.EventHandler(this._homePageLinkLabel_Click);
            //
            // label10
            //
            this.labelBirthday.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelBirthday.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.labelBirthday.Location = new System.Drawing.Point(4, 4);
            this.labelBirthday.Name = "labelBirthday";
            this.labelBirthday.Size = new System.Drawing.Size(64, 16);
            this.labelBirthday.TabIndex = 23;
            this.labelBirthday.Text = "Birthday:";
            //
            // label9
            //
            this.labelHomePage.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelHomePage.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.labelHomePage.Location = new System.Drawing.Point(4, 28);
            this.labelHomePage.Name = "labelHomePage";
            this.labelHomePage.Size = new System.Drawing.Size(72, 16);
            this.labelHomePage.TabIndex = 21;
            this.labelHomePage.Text = "Home page:";
            //
            // _homePage
            //
            this._homePage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._homePage.ForeColor = System.Drawing.SystemColors.HotTrack;
            this._homePage.Location = new System.Drawing.Point(92, 28);
            this._homePage.Multiline = false;
            this._homePage.Name = "_homePage";
            this._homePage.ReadOnly = false;
            this._homePage.Size = new System.Drawing.Size(104, 24);
            this._homePage.TabIndex = 7;
            //
            // _birthday
            //
            this._birthday.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._birthday.CurrentDate = new System.DateTime(((long)(0)));
            this._birthday.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._birthday.Location = new System.Drawing.Point(92, 0);
            this._birthday.Name = "_birthday";
            this._birthday.Size = new System.Drawing.Size(104, 24);
            this._birthday.TabIndex = 5;
            //
            // _lblBirthday
            //
            this.labelBirthdayDate.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelBirthdayDate.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.labelBirthdayDate.Location = new System.Drawing.Point(92, 4);
            this.labelBirthdayDate.Name = "labelBirthdayDate";
            this.labelBirthdayDate.Size = new System.Drawing.Size(100, 16);
            this.labelBirthdayDate.TabIndex = 25;
            this.labelBirthdayDate.Text = "label1";
            this.labelBirthdayDate.Visible = false;
            //
            // DetailsBlock
            //
            this.Controls.Add(this.labelBirthdayDate);
            this.Controls.Add(this._homePageLinkLabel);
            this.Controls.Add(this.labelBirthday);
            this.Controls.Add(this.labelHomePage);
            this.Controls.Add(this._homePage);
            this.Controls.Add(this._birthday);
            this.Name = "DetailsBlock";
            if( Core.ScaleFactor.Height == 1.0 )
                this.Size = new System.Drawing.Size(200, 52);
            else
                this.Size = new System.Drawing.Size(200, (int)(52 * Core.ScaleFactor.Height) );
            this.ResumeLayout(false);

        }
		#endregion

        public override void EditResource( IResource res )
        {
            _contact = res;
        	string homepage = _contact.GetPropText( ContactManager._propHomePage );
            if ( Controls.IndexOf( _homePageLinkLabel ) >= 0 )
            {
                Controls.Remove( _homePageLinkLabel );
            }
            if ( Controls.IndexOf( _homePage ) < 0 )
            {
                Controls.Add( _homePage );
            }
            _homePage.Text = homepage;

            DateTime birthDate = _contact.GetDateProp( ContactManager._propBirthday );
            _birthday.CurrentDate = birthDate;
        }

        public override void Save()
        {
            _contact.SetProp( ContactManager._propHomePage, _homePage.Text );
            if ( _birthday.CurrentDate != DateTime.MinValue )
            {
                _contact.SetProp( ContactManager._propBirthday, _birthday.CurrentDate );
            }
        }

        public override bool IsChanged()
        {
            return _homePage.Text != _contact.GetPropText( ContactManager._propHomePage ) ||
                   _birthday.CurrentDate != _contact.GetDateProp( ContactManager._propBirthday );
        }

        public override bool OwnsProperty( int propId )
        {
            return propId == ContactManager._propHomePage ||
                   propId == ContactManager._propBirthday;
        }

        private void _homePageLinkLabel_Click( object sender, EventArgs e )
        {
            string text = _homePageLinkLabel.Text;
            if ( text == null ) return;

            text = text.Trim();
            if ( text.Length > 0 )
            {
                if ( !text.StartsWith( HTTP ) )
                {
                    text = HTTP + "//" + text;
                }
                try
                {
                    Process process = new Process();
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.FileName = text;
                    process.Start();
                }
                catch ( Exception )
                {
                }
            }
        }

        public override string  HtmlContent( IResource contact )
        {
            string result = ObligatoryTag( contact, "Birthday:", ContactManager._propBirthday );
            result += ObligatoryTag( contact, "Home Page:", ContactManager._propHomePage );
            return result;
        }
    }
}
