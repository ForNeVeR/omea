// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    /// <summary>
    /// About box for OmniaMea.
    /// </summary>
    public class AboutBox : Form
    {
        private Button _btnOK;
        private Label _lblProductName;
        private Label label1;
        private Label _lblBuildDate;
        private PictureBox pictureBox1;
        private Button _btnCredits;
        private JetLinkLabel _lblWebPage;
        private JetLinkLabel _lblEmail;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components = null;

        public AboutBox()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            Icon = Core.UIManager.ApplicationIcon;

//            AutoScale = false;
            AutoScaleMode = AutoScaleMode.None;
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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(AboutBox));
            this._btnOK = new System.Windows.Forms.Button();
            this._lblProductName = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this._lblBuildDate = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this._btnCredits = new System.Windows.Forms.Button();
            this._lblWebPage = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this._lblEmail = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this.SuspendLayout();
            //
            // _btnOK
            //
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(232, 308);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 0;
            this._btnOK.Text = "OK";
            //
            // _lblProductName
            //
            this._lblProductName.AutoSize = true;
            this._lblProductName.BackColor = System.Drawing.SystemColors.Control;
            this._lblProductName.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblProductName.ForeColor = System.Drawing.Color.Black;
            this._lblProductName.Location = new System.Drawing.Point(8, 200);
            this._lblProductName.Name = "_lblProductName";
            this._lblProductName.Size = new System.Drawing.Size(120, 17);
            this._lblProductName.TabIndex = 1;
            this._lblProductName.Text = "JetBrains ProductName";
            //
            // label1
            //
            this.label1.BackColor = System.Drawing.SystemColors.Control;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(8, 216);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(280, 16);
            this.label1.TabIndex = 4;
            this.label1.Text = "Copyright (C) 2003-06 JetBrains s.r.o. All rights reserved.";
            //
            // _lblBuildDate
            //
            this._lblBuildDate.BackColor = System.Drawing.SystemColors.Control;
            this._lblBuildDate.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblBuildDate.ForeColor = System.Drawing.Color.Black;
            this._lblBuildDate.Location = new System.Drawing.Point(8, 272);
            this._lblBuildDate.Name = "_lblBuildDate";
            this._lblBuildDate.Size = new System.Drawing.Size(276, 16);
            this._lblBuildDate.TabIndex = 5;
            this._lblBuildDate.Text = "Built on Thursday after the rain";
            //
            // pictureBox1
            //
//            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(400, 200);
            this.pictureBox1.TabIndex = 6;
            this.pictureBox1.TabStop = false;
            //
            // _btnCredits
            //
            this._btnCredits.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCredits.Location = new System.Drawing.Point(316, 308);
            this._btnCredits.Name = "_btnCredits";
            this._btnCredits.TabIndex = 0;
            this._btnCredits.Text = "Credits";
            this._btnCredits.Click += new System.EventHandler(this._btnCredits_Click);
            //
            // _lblWebPage
            //
            this._lblWebPage.Cursor = System.Windows.Forms.Cursors.Hand;
            this._lblWebPage.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(70)), ((System.Byte)(70)), ((System.Byte)(211)));
            this._lblWebPage.Location = new System.Drawing.Point(8, 232);
            this._lblWebPage.Name = "_lblWebPage";
            this._lblWebPage.Size = new System.Drawing.Size(0, 0);
            this._lblWebPage.TabIndex = 7;
            this._lblWebPage.Click += new System.EventHandler(this.OnLinkLabelClick);
            //
            // _lblEmail
            //
            this._lblEmail.Cursor = System.Windows.Forms.Cursors.Hand;
            this._lblEmail.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(70)), ((System.Byte)(70)), ((System.Byte)(211)));
            this._lblEmail.Location = new System.Drawing.Point(8, 248);
            this._lblEmail.Name = "_lblEmail";
            this._lblEmail.Size = new System.Drawing.Size(0, 0);
            this._lblEmail.TabIndex = 8;
            this._lblEmail.Click += new System.EventHandler(this.OnLinkLabelClick);
            //
            // AboutBox
            //
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnOK;
            this.ClientSize = new System.Drawing.Size(400, 340);
            this.Controls.Add(this._lblEmail);
            this.Controls.Add(this._lblWebPage);
            this.Controls.Add(this._lblBuildDate);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._lblProductName);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this._btnCredits);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutBox";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AboutBox";
            this.ResumeLayout(false);
        }

        #endregion

        public void ShowAboutBox( Version version, string buildDate )
        {
            _lblEmail.Text = "mailto:feedback.omea@jetbrains.com";
#if READER
            const string imageName = "OmniaMea.Icons.AboutBoxReader.png";
            pictureBox1.Image = Image.FromStream( Assembly.GetExecutingAssembly().GetManifestResourceStream( imageName ) );
            this._lblWebPage.Text = "http://www.jetbrains.com/omea_reader/";
#else
            const string imageName = "OmniaMea.Icons.about_pro.png";
            pictureBox1.Image = Image.FromStream( Assembly.GetExecutingAssembly().GetManifestResourceStream( imageName ) );
            _lblWebPage.Text = "http://www.jetbrains.com/omea/";
#endif

            string productName = Core.ProductFullName;

            Text = "About " + productName;

            if ( Core.ProductReleaseVersion != null )
            {
                _lblProductName.Text = "JetBrains " + productName + " " +
                    Core.ProductReleaseVersion + " (version " + version + ")";
            }
            else
            {
                _lblProductName.Text = "JetBrains " + productName + " v" + version;
            }

            _lblBuildDate.Text = buildDate;

            ShowDialog( Core.MainWindow );
        }

        private void _btnCredits_Click( object sender, EventArgs e )
        {
            using( CreditsDlg dlg = new CreditsDlg() )
            {
                dlg.ShowDialog( this );
            }
        }

        private void OnLinkLabelClick( object sender, EventArgs e )
        {
			Core.UIManager.OpenInNewBrowserWindow((sender as Control).Text);
        }
	}
}
