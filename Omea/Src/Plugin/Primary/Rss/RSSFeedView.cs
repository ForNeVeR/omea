// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// The form displaying the properties of a feed.
	/// </summary>
    public class RSSFeedView: DialogBase
	{
        private const string MULTIPLE_SELECTION = "<Multiple Items Selected>";
	    private const string HELP_KEY = "/reference/feed_properties_dialog.html";

        private static int _selectedTab = 0;
        private Label       _lblAddress;
        private JetTextBox  _edtAddress;
        private Label       _lblTitle;
        private JetTextBox  _edtTitle;
        private Label       label3;
        private JetLinkLabel _lblHomepage;
        private JetTextBox   _edtDescription;
		private System.ComponentModel.Container components = null;
        private PeriodComboBox  _cmbUpdatePeriod;
        private NumericUpDownSettingEditor  _udUpdateFrequency;
        private Label       label5;
        private JetLinkLabel _lblAuthor;
        private Label       label6;
        private Label       _lblLastUpdated;
        private Button      _btnSave;
        private Button      _btnCancel;
        private GroupBox    _grpLogin;
        private StringSettingEditor         _edtPassword;
        private Label       _lblPassword;
        private StringSettingEditor         _edtUserName;
        private Label       _lblUserName;
        private CheckBox    _chkAuthentication;
        private CheckBox    _chkUpdate;
        private Button      _btnHelp;
        private PictureBox  _image;
        private GroupBox    _grpEnclosure;
        private BrowseForFolderControl _browseForFolderControl;
        private GroupBox    _grpDescription;

        private JetTextBox  _edtAnnotation;
        private Panel       _panelCategories;
        private CategoriesSelector _selector;

        private TabControl _tabs;
        private TabPage     _tabFeedInfo;
        private TabPage     _tabSettings;
        private TabPage     _tabAnnotation;

        private CheckBoxSettingEditor _chkMarkReadOnLeave;
        private CheckBoxSettingEditor _chkAutoFollowLink;
        private CheckBoxSettingEditor _chkAutoUpdateComments;
        private CheckBoxSettingEditor _chkAllowEqualPosts;
        private CheckBoxSettingEditor _chkAutoDownloadEncls;

        private IResource           _feed;
        private IResourceList       _feeds;
        private bool                _needUpdate = false;

		public RSSFeedView()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
            RestoreSettings();
            _tabs.SelectedIndex = _selectedTab;
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
            this._lblAddress = new System.Windows.Forms.Label();
            this._edtAddress = new JetTextBox();
            this._lblTitle = new System.Windows.Forms.Label();
            this._edtTitle = new JetTextBox();
            this._btnSave = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this._lblHomepage = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this._grpDescription = new System.Windows.Forms.GroupBox();
            this._edtDescription = new JetTextBox();
            this._udUpdateFrequency = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
            this._cmbUpdatePeriod = new JetBrains.Omea.RSSPlugin.PeriodComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this._lblAuthor = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this.label6 = new System.Windows.Forms.Label();
            this._lblLastUpdated = new System.Windows.Forms.Label();
            this._btnCancel = new System.Windows.Forms.Button();
            this._grpLogin = new System.Windows.Forms.GroupBox();
            this._edtPassword = new JetBrains.Omea.GUIControls.StringSettingEditor();
            this._lblPassword = new System.Windows.Forms.Label();
            this._edtUserName = new JetBrains.Omea.GUIControls.StringSettingEditor();
            this._lblUserName = new System.Windows.Forms.Label();
            this._chkAuthentication = new System.Windows.Forms.CheckBox();
            this._chkUpdate = new System.Windows.Forms.CheckBox();
            this._btnHelp = new System.Windows.Forms.Button();
            this._image = new System.Windows.Forms.PictureBox();
            this._grpEnclosure = new System.Windows.Forms.GroupBox();
            this._browseForFolderControl = new JetBrains.Omea.GUIControls.BrowseForFolderControl();

            _chkMarkReadOnLeave = new CheckBoxSettingEditor();
            _chkAutoFollowLink = new CheckBoxSettingEditor();
            _chkAutoUpdateComments = new CheckBoxSettingEditor();
            _chkAllowEqualPosts = new CheckBoxSettingEditor();
            _chkAutoDownloadEncls = new CheckBoxSettingEditor();

            _edtAnnotation = new JetTextBox();
            _panelCategories = new Panel();
            _selector = new CategoriesSelector();

            this._tabs = new System.Windows.Forms.TabControl();
            this._tabFeedInfo = new System.Windows.Forms.TabPage();
            this._tabSettings = new System.Windows.Forms.TabPage();
            this._tabAnnotation = new System.Windows.Forms.TabPage();
            this._grpDescription.SuspendLayout();
            this._grpLogin.SuspendLayout();
            this._grpEnclosure.SuspendLayout();
            this._tabs.SuspendLayout();
            this._tabFeedInfo.SuspendLayout();
            this._tabSettings.SuspendLayout();
            this._tabAnnotation.SuspendLayout();
            this.SuspendLayout();
            //
            // label1
            //
            this._lblAddress.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblAddress.Location = new System.Drawing.Point(8, 8);
            this._lblAddress.Name = "_lblAddress";
            this._lblAddress.Size = new System.Drawing.Size(56, 17);
            this._lblAddress.TabIndex = 0;
            this._lblAddress.Text = "&Address:";
            //
            // _edtAddress
            //
            this._edtAddress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtAddress.Location = new System.Drawing.Point(84, 4);
            this._edtAddress.Name = "_edtAddress";
            this._edtAddress.Size = new System.Drawing.Size(316, 21);
            this._edtAddress.TabIndex = 1;
            this._edtAddress.Text = "";
            this._edtAddress.TextChanged += new System.EventHandler(this._edtAddress_TextChanged);
            //
            // label2
            //
            this._lblTitle.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblTitle.Location = new System.Drawing.Point(8, 32);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Size = new System.Drawing.Size(56, 17);
            this._lblTitle.TabIndex = 2;
            this._lblTitle.Text = "&Title:";
            //
            // _edtTitle
            //
            this._edtTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtTitle.Location = new System.Drawing.Point(84, 28);
            this._edtTitle.Name = "_edtTitle";
            this._edtTitle.Size = new System.Drawing.Size(316, 21);
            this._edtTitle.TabIndex = 3;
            this._edtTitle.Text = "";
            //
            // label3
            //
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(8, 56);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 17);
            this.label3.TabIndex = 4;
            this.label3.Text = "Homepage:";
            //
            // _lblHomepage
            //
            this._lblHomepage.Cursor = System.Windows.Forms.Cursors.Hand;
            this._lblHomepage.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(70)), ((System.Byte)(70)), ((System.Byte)(211)));
            this._lblHomepage.Location = new System.Drawing.Point(84, 56);
            this._lblHomepage.Name = "_lblHomepage";
            this._lblHomepage.Size = new System.Drawing.Size(0, 0);
            this._lblHomepage.TabIndex = 5;
            this._lblHomepage.Click += new System.EventHandler(this._lblHomepage_LinkClicked);
            //
            // label5
            //
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label5.Location = new System.Drawing.Point(8, 80);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(68, 18);
            this.label5.TabStop = false;
            this.label5.Text = "Author:";
            //
            // _lblAuthor
            //
            this._lblAuthor.Cursor = System.Windows.Forms.Cursors.Hand;
            this._lblAuthor.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(70)), ((System.Byte)(70)), ((System.Byte)(211)));
            this._lblAuthor.Location = new System.Drawing.Point(84, 80);
            this._lblAuthor.Name = "_lblAuthor";
            this._lblAuthor.Size = new System.Drawing.Size(0, 0);
            this._lblAuthor.TabStop = false;
            this._lblAuthor.Click += new System.EventHandler(this._lblAuthor_LinkClicked);
            //
            // label6
            //
            this.label6.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label6.Location = new System.Drawing.Point(8, 104);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(68, 18);
            this.label6.TabIndex = 13;
            this.label6.Text = "Last updated:";
            //
            // _lblLastUpdated
            //
            this._lblLastUpdated.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblLastUpdated.Location = new System.Drawing.Point(84, 104);
            this._lblLastUpdated.Name = "_lblLastUpdated";
            this._lblLastUpdated.Size = new System.Drawing.Size(316, 34);
            this._lblLastUpdated.TabIndex = 8;
            this._lblLastUpdated.Text = "label7";
            this._lblLastUpdated.UseMnemonic = false;
            //
            // _grpDescription
            //
            this._grpDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._grpDescription.Controls.Add(this._edtDescription);
            this._grpDescription.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._grpDescription.Location = new System.Drawing.Point(4, 146);
            this._grpDescription.Name = "_grpDescription";
            this._grpDescription.Size = new System.Drawing.Size(396, 110);
            this._grpDescription.TabIndex = 6;
            this._grpDescription.TabStop = false;
            this._grpDescription.Text = "&Description";
            //
            // _edtDescription
            //
            this._edtDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtDescription.Location = new System.Drawing.Point(8, 24);
            this._edtDescription.Multiline = true;
            this._edtDescription.Name = "_edtDescription";
            this._edtDescription.ReadOnly = true;
            this._edtDescription.Size = new System.Drawing.Size(380, 74);
            this._edtDescription.TabIndex = 0;
            this._edtDescription.Text = "";
            //
            // _chkAuthentication
            //
            this._chkAuthentication.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkAuthentication.Location = new System.Drawing.Point(8, 8);
            this._chkAuthentication.Name = "_chkAuthentication";
            this._chkAuthentication.Size = new System.Drawing.Size(264, 16);
            this._chkAuthentication.TabIndex = 1;
            this._chkAuthentication.Text = "The feed requires an HTTP &login";
            this._chkAuthentication.CheckedChanged += new System.EventHandler(this._chkAuthentication_CheckedChanged);
            //
            // _grpLogin
            //
            this._grpLogin.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._grpLogin.Controls.Add(this._edtPassword);
            this._grpLogin.Controls.Add(this._lblPassword);
            this._grpLogin.Controls.Add(this._edtUserName);
            this._grpLogin.Controls.Add(this._lblUserName);
            this._grpLogin.Enabled = false;
            this._grpLogin.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._grpLogin.Location = new System.Drawing.Point(32, 28);
            this._grpLogin.Name = "_grpLogin";
            this._grpLogin.Size = new System.Drawing.Size(368, 72);
            this._grpLogin.TabIndex = 2;
            this._grpLogin.TabStop = false;
            //
            // label8
            //
            this._lblUserName.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblUserName.Location = new System.Drawing.Point(8, 20);
            this._lblUserName.Name = "_lblUserName";
            this._lblUserName.Size = new System.Drawing.Size(64, 16);
            this._lblUserName.TabIndex = 0;
            this._lblUserName.Text = "&User name:";
            //
            // _edtUserName
            //
            this._edtUserName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtUserName.Changed = false;
            this._edtUserName.Location = new System.Drawing.Point(72, 20);
            this._edtUserName.Name = "_edtUserName";
            this._edtUserName.Size = new System.Drawing.Size(288, 21);
            this._edtUserName.TabIndex = 1;
            this._edtUserName.Text = "";
            //
            // label7
            //
            this._lblPassword.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblPassword.Location = new System.Drawing.Point(8, 48);
            this._lblPassword.Name = "_lblPassword";
            this._lblPassword.Size = new System.Drawing.Size(60, 16);
            this._lblPassword.TabIndex = 2;
            this._lblPassword.Text = "&Password:";
            //
            // _edtPassword
            //
            this._edtPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtPassword.Changed = false;
            this._edtPassword.Location = new System.Drawing.Point(72, 44);
            this._edtPassword.Name = "_edtPassword";
            this._edtPassword.PasswordChar = '*';
            this._edtPassword.Size = new System.Drawing.Size(288, 21);
            this._edtPassword.TabIndex = 3;
            this._edtPassword.Text = "";
            //
            // _chkUpdate
            //
            this._chkUpdate.Checked = true;
            this._chkUpdate.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkUpdate.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkUpdate.Location = new System.Drawing.Point(8, 112);
            this._chkUpdate.Name = "_chkUpdate";
            this._chkUpdate.Size = new System.Drawing.Size(88, 16);
            this._chkUpdate.TabIndex = 5;
            this._chkUpdate.Text = "Update &every";
            this._chkUpdate.CheckedChanged += new System.EventHandler(this._chkUpdate_CheckedChanged);
            //
            // _udUpdateFrequency
            //
            this._udUpdateFrequency.Changed = true;
            this._udUpdateFrequency.Location = new System.Drawing.Point(104, 108);
            this._udUpdateFrequency.Maximum = 1000;
            this._udUpdateFrequency.Minimum = 1;
            this._udUpdateFrequency.Name = "_udUpdateFrequency";
            this._udUpdateFrequency.Size = new System.Drawing.Size(40, 21);
            this._udUpdateFrequency.TabIndex = 6;
            this._udUpdateFrequency.Text = "1";
            this._udUpdateFrequency.Value = 1;
            //
            // _cmbUpdatePeriod
            //
            this._cmbUpdatePeriod.Changed = false;
            this._cmbUpdatePeriod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbUpdatePeriod.Location = new System.Drawing.Point(152, 108);
            this._cmbUpdatePeriod.Name = "_cmbUpdatePeriod";
            this._cmbUpdatePeriod.Size = new System.Drawing.Size(76, 21);
            this._cmbUpdatePeriod.TabIndex = 7;
            //
            // _image
            //
            this._image.Location = new System.Drawing.Point(8, 132);
            this._image.Name = "_image";
            this._image.Size = new System.Drawing.Size(144, 4);
            this._image.TabIndex = 15;
            this._image.TabStop = false;
            //
            // _grpEnclosure
            //
            this._grpEnclosure.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._grpEnclosure.Controls.Add(this._browseForFolderControl);
            this._grpEnclosure.Location = new System.Drawing.Point(4, 136);
            this._grpEnclosure.Name = "_grpEnclosure";
            this._grpEnclosure.Size = new System.Drawing.Size(396, 52);
            this._grpEnclosure.TabIndex = 16;
            this._grpEnclosure.TabStop = false;
            this._grpEnclosure.Text = "&Destination folder for downloaded feed enclosures";
            //
            // _browseForFolderControl
            //
            this._browseForFolderControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._browseForFolderControl.Changed = true;
            this._browseForFolderControl.Location = new System.Drawing.Point(4, 24);
            this._browseForFolderControl.Name = "_browseForFolderControl";
            this._browseForFolderControl.SelectedPath = "";
            this._browseForFolderControl.Size = new System.Drawing.Size(380, 25);
            this._browseForFolderControl.TabIndex = 0;

            this._chkMarkReadOnLeave.Location = new Point(8, 196);
            this._chkMarkReadOnLeave.Size = new Size(392, 16);
            this._chkMarkReadOnLeave.Text = "&Mark all items read when leaving the feed";
            this._chkMarkReadOnLeave.TabIndex = 17;

            this._chkAutoFollowLink.Location = new Point(8, 216);
            this._chkAutoFollowLink.Size = new Size(392, 16);
            this._chkAutoFollowLink.Text = "&Go to the item link when an item is selected";
            this._chkAutoFollowLink.TabIndex = 18;

            this._chkAutoUpdateComments.Location = new Point(8, 236);
            this._chkAutoUpdateComments.Size = new Size(220, 16);
            this._chkAutoUpdateComments.Text = "&Auto update feed comments";
            this._chkAutoUpdateComments.TabIndex = 19;

            this._chkAllowEqualPosts.Location = new Point(8, 256);
            this._chkAllowEqualPosts.Size = new Size(220, 16);
            this._chkAllowEqualPosts.Text = "Accept &identical posts";
            this._chkAllowEqualPosts.TabIndex = 20;

            this._chkAutoDownloadEncls.Location = new Point(8, 276);
            this._chkAutoDownloadEncls.Size = new Size(220, 16);
            this._chkAutoDownloadEncls.Text = "Autodownload Enclosures";
            this._chkAutoDownloadEncls.TabIndex = 21;
            //
            // _edtAnnotation
            //
            this._edtAnnotation.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtAnnotation.Location = new System.Drawing.Point(8, 8);
            this._edtAnnotation.Multiline = true;
            this._edtAnnotation.Name = "_edtAnnotation";
            this._edtAnnotation.ReadOnly = false;
            this._edtAnnotation.Size = new System.Drawing.Size(386, 200);
            this._edtAnnotation.TabIndex = 0;
            this._edtAnnotation.Text = "";
            //
            // _panelCategories
            //
            _panelCategories.Controls.Add( _selector );
            _panelCategories.Size = new Size(386, 40);
            _panelCategories.Dock = DockStyle.Bottom;
            _panelCategories.Name = "_panelCategories";
            _panelCategories.TabIndex = 2;
            //
            // _selector
            //
            _selector.Dock = DockStyle.Fill;
            _selector.Name = "_selector";
            _selector.TabIndex = 3;
            //
            // _btnSave
            //
            this._btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnSave.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnSave.Location = new System.Drawing.Point(164, 303);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Size = new System.Drawing.Size(75, 25);
            this._btnSave.TabIndex = 10;
            this._btnSave.Text = "Save";
            this._btnSave.Click += new System.EventHandler(this.OnSaveFeed);
            //
            // _btnCancel
            //
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(252, 303);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(75, 26);
            this._btnCancel.TabIndex = 11;
            this._btnCancel.Text = "Cancel";
            //
            // _btnHelp
            //
            this._btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnHelp.Location = new System.Drawing.Point(340, 303);
            this._btnHelp.Name = "_btnHelp";
            this._btnHelp.Size = new System.Drawing.Size(75, 26);
            this._btnHelp.TabIndex = 14;
            this._btnHelp.Text = "Help";
            this._btnHelp.Click += new System.EventHandler(this._btnHelp_Click);

            //
            // _tabs
            //
            this._tabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._tabs.Controls.Add(this._tabFeedInfo);
            this._tabs.Controls.Add(this._tabSettings);
            this._tabs.Controls.Add(this._tabAnnotation);
            this._tabs.Location = new System.Drawing.Point(8, 8);
            this._tabs.Name = "_tabs";
            this._tabs.SelectedIndex = 0;
            this._tabs.Size = new System.Drawing.Size(412, 288);
            this._tabs.TabIndex = 17;
            //
            // _tabFeedInfo
            //
            this._tabFeedInfo.Controls.Add(this._lblAddress);
            this._tabFeedInfo.Controls.Add(this._lblTitle);
            this._tabFeedInfo.Controls.Add(this._edtAddress);
            this._tabFeedInfo.Controls.Add(this._edtTitle);
            this._tabFeedInfo.Controls.Add(this.label5);
            this._tabFeedInfo.Controls.Add(this._lblAuthor);
            this._tabFeedInfo.Controls.Add(this._image);
            this._tabFeedInfo.Controls.Add(this._lblHomepage);
            this._tabFeedInfo.Controls.Add(this.label3);
            this._tabFeedInfo.Controls.Add(this.label6);
            this._tabFeedInfo.Controls.Add(this._lblLastUpdated);
            this._tabFeedInfo.Controls.Add(this._grpDescription);
            this._tabFeedInfo.Location = new System.Drawing.Point(4, 22);
            this._tabFeedInfo.Name = "_tabFeedInfo";
            this._tabFeedInfo.Size = new System.Drawing.Size(404, 262);
            this._tabFeedInfo.TabIndex = 0;
            this._tabFeedInfo.Text = "Feed Information";
            //
            // _tabSettings
            //
            this._tabSettings.Controls.Add(this._udUpdateFrequency);
            this._tabSettings.Controls.Add(this._chkUpdate);
            this._tabSettings.Controls.Add(this._cmbUpdatePeriod);
            this._tabSettings.Controls.Add(this._chkAuthentication);
            this._tabSettings.Controls.Add(this._grpLogin);
            this._tabSettings.Controls.Add(this._grpEnclosure);
            this._tabSettings.Controls.Add(this._chkMarkReadOnLeave);
            this._tabSettings.Controls.Add(this._chkAutoFollowLink);
            this._tabSettings.Controls.Add(this._chkAutoUpdateComments);
            this._tabSettings.Controls.Add(this._chkAllowEqualPosts);
            this._tabSettings.Controls.Add(this._chkAutoDownloadEncls);

            this._tabSettings.Location = new System.Drawing.Point(4, 22);
            this._tabSettings.Name = "_tabSettings";
            this._tabSettings.Size = new System.Drawing.Size(404, 262);
            this._tabSettings.TabIndex = 1;
            this._tabSettings.Text = "Settings";
            //
            // _tabAnnotation
            //
            _tabAnnotation.Controls.Add( _edtAnnotation );
		    _tabAnnotation.Controls.Add( _panelCategories );

            _tabAnnotation.Location = new System.Drawing.Point(4, 22);
            _tabAnnotation.Name = "_tabAnnotation";
            _tabAnnotation.Size = new System.Drawing.Size(404, 262);
            _tabAnnotation.TabIndex = 2;
            _tabAnnotation.Text = "Annotation";
            //
            // RSSFeedView
            //
            this.AcceptButton = this._btnSave;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(424, 338);
            this.Controls.Add(this._tabs);
            this.Controls.Add(this._btnHelp);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnSave);
            this.MinimumSize = new System.Drawing.Size(432, 372);
            this.Name = "RSSFeedView";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Feed Properties";
            this.Closed += new System.EventHandler(this.OnClosed);
            this._grpDescription.ResumeLayout(false);
            this._grpLogin.ResumeLayout(false);
            this._grpEnclosure.ResumeLayout(false);
            this._tabs.ResumeLayout(false);
            this._tabFeedInfo.ResumeLayout(false);
            this._tabSettings.ResumeLayout(false);
            this._tabAnnotation.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        public void DisplayRSSFeeds( IResourceList feeds )
        {
            Guard.NullArgument( feeds, "feeds" );
            if ( feeds.Count == 0 )
            {
                throw new ArgumentException( "feeds should have at least one feed" );
            }
            RestoreSettings();
            _feeds = feeds;
            _feed =  feeds[ 0 ];
            _selector.Resource = feeds[ 0 ];

            SettingArray enclosurePath  = SettingArray.FromResourceList( _feeds, Props.EnclosurePath, Settings.EnclosurePath, true );
            _browseForFolderControl.SetSetting( enclosurePath );
            _cmbUpdatePeriod.SetSetting( SettingArray.FromResourceList( _feeds, Props.UpdatePeriod, "daily", true ) );
            _udUpdateFrequency.SetSetting( SettingArray.FromResourceList( _feeds, Props.UpdateFrequency, Settings.UpdateFrequency ) );

            if ( !_udUpdateFrequency.Setting.Defined || _udUpdateFrequency.Setting.Value == null || (int)_udUpdateFrequency.Setting.Value == -1 )
            {
                _chkUpdate.Checked = false;
                _udUpdateFrequency.Value = Settings.UpdateFrequency;
            }

            _edtUserName.SetSetting( SettingArray.FromResourceList( _feeds, Props.HttpUserName, null, true ) );
            _edtPassword.SetSetting( SettingArray.FromResourceList( _feeds, Props.HttpPassword, null, true ) );

            if ( _edtUserName.Setting.Different || _edtUserName.Setting.Defined ||
                _edtPassword.Setting.Different || _edtPassword.Setting.Defined )
            {
                _chkAuthentication.Checked = true;
            }

            _chkMarkReadOnLeave.SetSetting( SettingArray.FromResourceList( _feeds, Props.MarkReadOnLeave, false ) );
            _chkAutoFollowLink.SetSetting( SettingArray.FromResourceList( _feeds, Props.AutoFollowLink, false ) );
            _chkAutoUpdateComments.SetSetting( SettingArray.FromResourceList( _feeds, Props.AutoUpdateComments, false ) );
            _chkAllowEqualPosts.SetSetting( SettingArray.FromResourceList( _feeds, Props.AllowEqualPosts, false ) );
            _chkAutoDownloadEncls.SetSetting( SettingArray.FromResourceList( _feeds, Props.AutoDownloadEnclosure, false ) );

            if ( _feeds.Count > 1 )
            {
                _edtAddress.Text        = MULTIPLE_SELECTION;
                _edtAddress.ReadOnly    = true;
                _edtTitle.Text          = MULTIPLE_SELECTION;
                _edtTitle.ReadOnly      = true;
                _lblHomepage.Text       = MULTIPLE_SELECTION;
                _lblHomepage.ClickableLink = false;
                _lblAuthor.Text = MULTIPLE_SELECTION;
                _lblAuthor.ClickableLink = false;
                _edtDescription.Text    = MULTIPLE_SELECTION;
                _lblLastUpdated.Text    = MULTIPLE_SELECTION;
                _edtAnnotation.Text = MULTIPLE_SELECTION;
            }
            else
            {
                //  Fix OM-12232 - broken feeds some times return empty string
                //  for an URL. Just ignore such image specifications.
                //  Test feed (for 16.02.06) was http://www.metalfan.ro/forum/rss.php
                if ( !_feed.HasProp( Props.ImageContent ) && _feed.HasProp( Props.ImageURL ) &&
                     !string.IsNullOrEmpty( _feed.GetStringProp( Props.ImageURL )))
                {
                    Core.NetworkAP.QueueJob( JobPriority.AboveNormal,
                        new DownloadResourceBlobJob( _feed, Props.ImageContent, _feed.GetStringProp( Props.ImageURL ),
                        ImageContentDownloaded ) );
                }
                SetImage();
                InitSingleFeedSelection( _feed );
            }
            UpdateControls();
        }

        private void ImageContentDownloaded( bool ready )
        {
            if ( !IsDisposed )
            {
                try
                {
                    SetImageAfterDownload();
                }
                catch{}
            }
        }

        private void SetImageAfterDownload()
        {
            Size size = Size;
            size.Height += _image.Size.Height;
            Size = size;
            SetImage();
        }

        private void SetImage()
        {
            //  workaround of OM-12936, do not process broken blobs (?)
            try
            {
                Stream stream = _feed.GetBlobProp( Props.ImageContent );
                if ( stream == null ) return;
                _image.Image = Image.FromStream( stream );
                _image.SizeMode = PictureBoxSizeMode.AutoSize;
                AdjustGroupBox( );
            }
            catch( ArgumentException )
            {
                //  Nothing to do, just ignore.
            }
        }

        private void AdjustGroupBox( )
        {
            Point location = _grpDescription.Location;
            location.Y += _image.Size.Height;
            _grpDescription.Location = location;
            Size groupSize = _grpDescription.Size;
            groupSize.Height -= _image.Size.Height;
            _grpDescription.Size = groupSize;
            MinimumSize = new Size( MinimumSize.Width, location.Y + 180 );
        }

        private void InitSingleFeedSelection(IResource feed)
        {
            _edtAddress.Text= feed.GetStringProp( Props.URL );
            _edtTitle.Text  = feed.GetStringProp( Core.Props.Name );
            string origName = feed.GetStringProp( Props.OriginalName );
            if ( !string.IsNullOrEmpty( origName ) )
            {
                Text = origName;
            }

            _lblHomepage.Text = feed.GetStringProp( Props.HomePage );
            _edtAnnotation.Text = feed.GetPropText( Core.Props.Annotation );

            if ( feed.GetPropText( Props.Author ).Length > 0 )
            {
                _lblAuthor.Text = feed.GetStringProp( Props.Author );

                IResource res = feed.GetLinkProp( Props.AuthorEmail );
                if ( res != null )
                {
                    _lblAuthor.ClickableLink = true;
                    _lblAuthor.Tag = res.GetStringProp( Core.ContactManager.Props.EmailAddress );
                }
                else
                {
                    _lblAuthor.ClickableLink = false;
                }
            }
            else
            {
                IResourceList contactLinks = feed.GetLinksTo( null, Props.Weblog );
                if ( contactLinks.Count > 0 )
                {
                    IResource contact = contactLinks [0];
                    IResource email = contact.GetLinkProp( Core.ContactManager.Props.LinkEmailAcct );
                    if ( email == null )
                    {
                        _lblAuthor.ClickableLink = false;
                        _lblAuthor.Text = contact.DisplayName;
                    }
                    else
                    {
                        _lblAuthor.Text = contact.DisplayName + " <" + email.DisplayName + ">";
                        _lblAuthor.ClickableLink = true;
                        _lblAuthor.Tag = email.DisplayName;
                    }
                }
                else
                {
                    _lblAuthor.Text = "Not Specified";
                    _lblAuthor.ClickableLink = false;
                    _lblAuthor.ForeColor = SystemColors.GrayText;
                }
            }
            _edtDescription.Text = feed.GetStringProp( Props.Description );

            DateTime lastUpdateTime = _feed.GetDateProp( Props.LastUpdateTime );
            if ( lastUpdateTime == DateTime.MinValue )
            {
                _lblLastUpdated.Text = "never";
            }
            else
            {
                _lblLastUpdated.Text = lastUpdateTime.ToString();

                string lastError = _feed.GetStringProp( Core.Props.LastError );
                if ( lastError != null )
                {
                    _lblLastUpdated.Text += ", error: " + lastError.Replace( "\r\n", " " );
                }
            }

        }

        private void _lblHomepage_LinkClicked( object sender, EventArgs e )
        {
			Core.UIManager.OpenInNewBrowserWindow(_lblHomepage.Text);
        }

        private void _lblAuthor_LinkClicked(object sender, EventArgs e)
        {
            JetLinkLabel senderLabel = sender as JetLinkLabel;
            if ( senderLabel.ClickableLink )
            {
                try
                {
                    Process.Start( "mailto:" + (string) senderLabel.Tag );
                }
                catch( Exception ex )
                {
                    MessageBox.Show( this, "Error creating message to " + (string) senderLabel.Tag + ": " + ex.Message,
                        "Feed Properties", MessageBoxButtons.OK );
                }
            }
        }

        private void OnSaveFeed( object sender, EventArgs e )
        {
            Core.ResourceAP.RunUniqueJob( new MethodInvoker( OnSaveFeedImpl ) );
        }

        private void OnSaveFeedImpl()
        {
            foreach ( IResource feed in _feeds )
            {
                feed.BeginUpdate();
            }
            if ( !_chkAuthentication.Checked )
            {
                _edtUserName.Text = string.Empty;
                _edtPassword.Text = string.Empty;
            }

            _needUpdate = _needUpdate || _udUpdateFrequency.Changed || _cmbUpdatePeriod.Changed;

            if ( !_chkUpdate.Checked )
            {
                _cmbUpdatePeriod.SetValue( "daily" );
                _cmbUpdatePeriod.Changed = true;
                _udUpdateFrequency.Minimum = -1;
                _udUpdateFrequency.SetValue( -1 );
                _udUpdateFrequency.Changed = true;
            }

            SettingSaver.Save( Controls );
            if ( _feeds.Count == 1 )
            {
                _feed.SetProp( Props.URL, _edtAddress.Text );
                _feed.SetProp( Core.Props.Name, _edtTitle.Text );
                _feed.SetProp( Core.Props.Annotation, _edtAnnotation.Text );
            }

            foreach ( IResource feed in _feeds )
            {
                feed.EndUpdate();
            }

            if ( _needUpdate )
            {
                foreach ( IResource feed in _feeds )
                {
                    RSSPlugin.GetInstance().QueueFeedUpdate( feed );
                }
            }
        }
        private void _chkAuthentication_CheckedChanged( object sender, EventArgs e )
        {
            _grpLogin.Enabled = _chkAuthentication.Checked;
        }

        private void _chkUpdate_CheckedChanged( object sender, EventArgs e )
        {
            _needUpdate = _chkUpdate.Checked;
            _udUpdateFrequency.Enabled = _chkUpdate.Checked;
            _cmbUpdatePeriod.Enabled = _chkUpdate.Checked;
            _cmbUpdatePeriod.Changed = true;
            _udUpdateFrequency.Changed = true;
        }

        private void _btnHelp_Click( object sender, EventArgs e )
        {
            Help.ShowHelp(this, Core.UIManager.HelpFileName, HELP_KEY);
        }

        private void _edtAddress_TextChanged( object sender, EventArgs e )
        {
            UpdateControls();
        }

        private void UpdateControls()
        {
            _btnSave.Enabled = _edtAddress.Text.Length > 0;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _selectedTab = _tabs.SelectedIndex;
        }
	}
}
