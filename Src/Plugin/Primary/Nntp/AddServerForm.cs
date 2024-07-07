// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.Charsets;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
	internal class EditServerForm : DialogBase
	{
        private static int _selectedTab = 0;
        private System.Windows.Forms.Label _serverLabel;
        private System.Windows.Forms.Label _portLabel;
        internal System.Windows.Forms.TextBox _serverName;
        internal NumericUpDownSettingEditor _portValue;
        private System.Windows.Forms.Button _cancelBtn;
        private System.Windows.Forms.Button _okBtn;
        private System.Windows.Forms.Label _userNameLabel;
        internal System.Windows.Forms.CheckBox _authorizedAccessBox;
        private System.Windows.Forms.Label _passwordLabel;
        internal StringSettingEditor _userNameBox;
        internal StringSettingEditor _passwordBox;
        private System.Windows.Forms.Label _displayNameLabel;
        private System.Windows.Forms.Label _emailLabel;
        internal StringSettingEditor _displayNameBox;
        internal StringSettingEditor _emailBox;
        private System.Windows.Forms.Label _displayAsLabel;
        internal System.Windows.Forms.TextBox _displayAsTextBox;
        private System.Windows.Forms.Label _errorLabel;
        private System.Windows.Forms.Button _helpButton;
        private System.Windows.Forms.TabControl _optionTabs;
        private System.Windows.Forms.CheckBox _abbreviateCheckBox;
        private NumericUpDownSettingEditor _abbreviateLevel;
        private CheckBoxSettingEditor _deliverOnStartupCheckBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private NumericUpDownSettingEditor _deliverFreqBox;
        private NumericUpDownSettingEditor _articlesCountBox;
        private System.Windows.Forms.CheckBox _scheduledDeliverCheckBox;
        private CheckBoxSettingEditor _downloadHeadersCheckBox;
        private CheckBoxSettingEditor _downloadBodyOnSelectionCheckBox;
        private CheckBoxSettingEditor _markFromMeAsRead;
        private System.Windows.Forms.TabPage _generalPage;
        private System.Windows.Forms.TabPage _securityPage;
        private System.Windows.Forms.TabPage _postingPage;
        private System.Windows.Forms.TabPage _localSettingsPage;
        private EncodingComboBox _encodingsBox;
        private System.Windows.Forms.Label label4;
        private CheckBoxSettingEditor _putInOutbox;
        private RadioButtonSettingEditor _formatGroupBox;
        private ComboBoxSettingEditor _mimeEncodingBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TabPage _downloadPage;
        private System.Windows.Forms.TabPage _signaturesPage;
        private CheckBox                _chkSigSettingsOverrided;
        private CheckBoxSettingEditor   _useSignature;
        private StringSettingEditor     _signatureBox;
        private RadioButtonSettingEditor _grpSignatureInReplies;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private CheckBoxSettingEditor _enableSSLCheckBox;
        private IResourceList _servers;
        private IResource _server;
        private const string MULTIPLE_SELECTION = "<Multiple Items Selected>";

		private EditServerForm()
		{
			InitializeComponent();
            RestoreSettings();

            this.Icon = Core.UIManager.ApplicationIcon;

            _okBtn.Enabled = false;
            _servers = Core.ResourceStore.EmptyResourceList;
            _optionTabs.SelectedIndex = _selectedTab;
		}

        protected override void ScaleCore( float dx, float dy )
        {
            base.ScaleCore( dx, dy );
            if( Environment.Version.Major < 2 )
            {
                MinimumSize = new Size(
                    (int) ( (float) MinimumSize.Width * dx ), (int) ( (float) MinimumSize.Height * dy ) );
                MaximumSize = new Size(
                    (int) ( (float) MaximumSize.Width * dx ), (int) ( (float) MaximumSize.Height * dy ) );
            }
        }

        public IResourceList Servers
        {
            get { return _servers; }
        }

        public static EditServerForm CreateNewServerPropertiesForm( string name, int port )
        {
            IResource tempServer = Core.ResourceStore.NewResourceTransient( NntpPlugin._newsServer );
            tempServer.SetProp( Core.Props.DisplayThreaded, true );
            ServerResource server = new ServerResource( tempServer );
            server.Name = name;
            server.Port = port;
            EditServerForm form = CreateServerPropertiesForm( tempServer.ToResourceList() );
            form.Text = "Add News Server";
            form._optionTabs.SelectedIndex = 0;
            return form;
        }

        private void Init( IResourceList servers )
        {
            Guard.NullArgument( servers, "servers" );
            if ( servers.Count == 0 )
            {
                throw new ArgumentException( "'servers' resource list must contain at least one server." );
            }
            _servers = servers;
            _server = servers[0];
            MultiSettings multiSettings = new MultiSettings( _servers );

            ServerResource serverResource = new ServerResource( _server );
            if( _servers.Count > 1 )
            {
                Text = "News Servers Properties";
                _serverName.Text = MULTIPLE_SELECTION;
                _serverName.ReadOnly = true;
                _displayAsTextBox.Text = MULTIPLE_SELECTION;
                _displayAsTextBox.ReadOnly = true;
            }
            else
            {
                Text = serverResource.DisplayName + ": News Server Properties";
                _serverName.Text = serverResource.Name;
                _displayAsTextBox.Text = serverResource.DisplayName;
            }

            Setting port = multiSettings.Port;
            _portValue.SetSetting( port );

            _displayNameBox.SetSetting( multiSettings.UserDisplayName );
            _emailBox.SetSetting( multiSettings.UserEmailAddress );

            _abbreviateLevel.Minimum = 0;
            _abbreviateLevel.SetSetting( multiSettings.AbbreviateLevel );

            if( _abbreviateLevel.Determinated && _abbreviateLevel.Value == 0 )
            {
                _abbreviateLevel.Enabled = _abbreviateCheckBox.Checked = false;
            }
            else
            {
                _abbreviateLevel.Enabled = _abbreviateCheckBox.Checked = true;
            }
            _abbreviateLevel.Minimum = 1;

            _userNameBox.SetSetting( multiSettings.LoginName );
            _passwordBox.SetSetting( multiSettings.Password );

            if ( _userNameBox.Determinated && _userNameBox.Text.Length == 0 )
            {
                _authorizedAccessBox.Checked = false;
            }
            else
            {
                _authorizedAccessBox.Checked = true;
            }
            _enableSSLCheckBox.SetSetting( multiSettings.Ssl3Enabled );

            _articlesCountBox.SetSetting( multiSettings.ArticlesPerGroup );

            _deliverOnStartupCheckBox.SetSetting( multiSettings.DeliverOnStartup );

            _deliverFreqBox.Minimum = 0;
            _deliverFreqBox.SetSetting( multiSettings.DeliverNewsPeriod );
            _scheduledDeliverCheckBox.Checked = !_deliverFreqBox.Determinated || _deliverFreqBox.Value > 0;
            if( _deliverFreqBox.Determinated && _deliverFreqBox.Value == 0 )
            {
                _deliverFreqBox.Value = (int)_deliverFreqBox.Setting.Default;
                if ( _scheduledDeliverCheckBox.Checked )
                {
                    _deliverFreqBox.Changed = true;
                }
            }
            _deliverFreqBox.Minimum = 1;

            _markFromMeAsRead.SetSetting( multiSettings.MarkFromMeAsRead );
            _downloadHeadersCheckBox.InvertValue = true;
            _downloadHeadersCheckBox.SetSetting( multiSettings.DownloadBodiesOnDeliver );

            _downloadBodyOnSelectionCheckBox.SetSetting( multiSettings.DownloadBodyOnSelection );

            _encodingsBox.Sorted = true;
            _encodingsBox.Init( multiSettings.Charset );

            _formatGroupBox.CheckedChanged+=new RadioButtonSettingEditor.CheckedChangedHandler( _groupBox_CheckedChanged );
            _formatGroupBox.SetData( new string[]{ "UUEncode", "MIME" }, new string[]{ "UUEncode", "MIME" } );
            _formatGroupBox.SetSetting( multiSettings.Format );

            string[] mimeEncodings = new string[]{ "None", "Quoted-Printable", "Base64" };
            _mimeEncodingBox.SetData( mimeEncodings, mimeEncodings );
            _mimeEncodingBox.SetSetting( multiSettings.EncodeTextWith );
            _putInOutbox.SetSetting( multiSettings.PutInOutbox );

            _useSignature.SetSetting( multiSettings.UseSignature );
            _signatureBox.SetSetting( multiSettings.Signature );
            _grpSignatureInReplies.SetData( new object[]{ 0, 1, 2 }, new string[]{ "None", "Before quoted text", "After quoted text" } );
            _grpSignatureInReplies.SetSetting( multiSettings.SignatureInReplies );

            _chkSigSettingsOverrided.ThreeState = (_servers.Count > 1);
            if( _servers.Count > 1 )
            {
                if( multiSettings.UseSignature.Different ||
                    multiSettings.Signature.Different ||
                    multiSettings.SignatureInReplies.Different )
                {
                    _chkSigSettingsOverrided.CheckState = CheckState.Indeterminate;
                }
            }
            else
            {
                _chkSigSettingsOverrided.Checked = serverResource.OverrideSig;
                if( !_chkSigSettingsOverrided.Checked )
                {
                    _useSignature.Enabled = _signatureBox.Enabled = _grpSignatureInReplies.Enabled = false;
                }
            }

            if( _chkSigSettingsOverrided.Checked && !_useSignature.Checked )
            {
                _signatureBox.Enabled = _grpSignatureInReplies.Enabled = false;
            }

            UpdateOKButton();

            _serverName.TextChanged += new System.EventHandler(this._serverName_TextChanged);
            _emailBox.TextChanged += new System.EventHandler(this._emailBox_TextChanged);
            _displayAsTextBox.TextChanged += new System.EventHandler(this._displayAsTextBox_TextChanged);
        }

        public static EditServerForm CreateServerPropertiesForm( IResourceList servers )
        {
            EditServerForm form = new EditServerForm();
            form.Init( servers );
            return form;
        }

        protected override void Dispose( bool disposing )
		{
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(EditServerForm));
            this._serverLabel = new System.Windows.Forms.Label();
            this._portLabel = new System.Windows.Forms.Label();
            this._serverName = new System.Windows.Forms.TextBox();
            this._portValue = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
            this._cancelBtn = new System.Windows.Forms.Button();
            this._okBtn = new System.Windows.Forms.Button();
            this._authorizedAccessBox = new System.Windows.Forms.CheckBox();
            this._userNameLabel = new System.Windows.Forms.Label();
            this._passwordLabel = new System.Windows.Forms.Label();
            this._userNameBox = new JetBrains.Omea.GUIControls.StringSettingEditor();
            this._passwordBox = new JetBrains.Omea.GUIControls.StringSettingEditor();
            this._displayNameLabel = new System.Windows.Forms.Label();
            this._emailLabel = new System.Windows.Forms.Label();
            this._displayNameBox = new JetBrains.Omea.GUIControls.StringSettingEditor();
            this._emailBox = new JetBrains.Omea.GUIControls.StringSettingEditor();
            this._displayAsLabel = new System.Windows.Forms.Label();
            this._displayAsTextBox = new System.Windows.Forms.TextBox();
            this._errorLabel = new System.Windows.Forms.Label();
            this._helpButton = new System.Windows.Forms.Button();
            this._optionTabs = new System.Windows.Forms.TabControl();
            this._generalPage = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._abbreviateLevel = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
            this._abbreviateCheckBox = new System.Windows.Forms.CheckBox();
            this._securityPage = new System.Windows.Forms.TabPage();
            this._enableSSLCheckBox = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this._downloadPage = new System.Windows.Forms.TabPage();
            this._markFromMeAsRead = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._downloadHeadersCheckBox = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._downloadBodyOnSelectionCheckBox = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this.label3 = new System.Windows.Forms.Label();
            this._deliverFreqBox = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
            this.label2 = new System.Windows.Forms.Label();
            this._articlesCountBox = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
            this.label1 = new System.Windows.Forms.Label();
            this._scheduledDeliverCheckBox = new System.Windows.Forms.CheckBox();
            this._deliverOnStartupCheckBox = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._localSettingsPage = new System.Windows.Forms.TabPage();
            this._encodingsBox = new JetBrains.Omea.Nntp.EncodingComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this._postingPage = new System.Windows.Forms.TabPage();
            this._putInOutbox = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._formatGroupBox = new JetBrains.Omea.GUIControls.RadioButtonSettingEditor();
            this._mimeEncodingBox = new JetBrains.Omea.GUIControls.ComboBoxSettingEditor();
            this.label7 = new System.Windows.Forms.Label();
            this._signaturesPage = new System.Windows.Forms.TabPage();
            this._chkSigSettingsOverrided = new System.Windows.Forms.CheckBox();
            this._grpSignatureInReplies = new JetBrains.Omea.GUIControls.RadioButtonSettingEditor();
            this._useSignature = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._signatureBox = new JetBrains.Omea.GUIControls.StringSettingEditor();
            this._optionTabs.SuspendLayout();
            this._generalPage.SuspendLayout();
            this._securityPage.SuspendLayout();
            this._downloadPage.SuspendLayout();
            this._localSettingsPage.SuspendLayout();
            this._postingPage.SuspendLayout();
            this._formatGroupBox.SuspendLayout();
            this._signaturesPage.SuspendLayout();
            this.SuspendLayout();
            //
            // _serverLabel
            //
            this._serverLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._serverLabel.Location = new System.Drawing.Point(16, 16);
            this._serverLabel.Name = "_serverLabel";
            this._serverLabel.Size = new System.Drawing.Size(56, 20);
            this._serverLabel.TabIndex = 0;
            this._serverLabel.Text = "&Server:";
            this._serverLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _portLabel
            //
            this._portLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._portLabel.Location = new System.Drawing.Point(16, 44);
            this._portLabel.Name = "_portLabel";
            this._portLabel.Size = new System.Drawing.Size(56, 20);
            this._portLabel.TabIndex = 1;
            this._portLabel.Text = "&Port:";
            this._portLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _serverName
            //
            this._serverName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._serverName.Location = new System.Drawing.Point(72, 14);
            this._serverName.Name = "_serverName";
            this._serverName.Size = new System.Drawing.Size(336, 21);
            this._serverName.TabIndex = 0;
            this._serverName.Text = "";
            this._serverName.KeyDown += new System.Windows.Forms.KeyEventHandler(this._serverName_KeyDown);
            //
            // _portValue
            //
            this._portValue.Changed = false;
            this._portValue.Location = new System.Drawing.Point(72, 42);
            this._portValue.Maximum = 65535;
            this._portValue.Minimum = 1;
            this._portValue.Name = "_portValue";
            this._portValue.Size = new System.Drawing.Size(56, 21);
            this._portValue.TabIndex = 1;
            //
            // _cancelBtn
            //
            this._cancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelBtn.Location = new System.Drawing.Point(276, 301);
            this._cancelBtn.Name = "_cancelBtn";
            this._cancelBtn.TabIndex = 2;
            this._cancelBtn.Text = "Cancel";
            //
            // _okBtn
            //
            this._okBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okBtn.Location = new System.Drawing.Point(192, 301);
            this._okBtn.Name = "_okBtn";
            this._okBtn.TabIndex = 1;
            this._okBtn.Text = "OK";
            this._okBtn.Click += new System.EventHandler(this._okBtn_Click);
            //
            // _authorizedAccessBox
            //
            this._authorizedAccessBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._authorizedAccessBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._authorizedAccessBox.Location = new System.Drawing.Point(16, 16);
            this._authorizedAccessBox.Name = "_authorizedAccessBox";
            this._authorizedAccessBox.Size = new System.Drawing.Size(396, 24);
            this._authorizedAccessBox.TabIndex = 0;
            this._authorizedAccessBox.Text = "&Authentication required";
            this._authorizedAccessBox.CheckedChanged += new System.EventHandler(this._authorizedAccessBox_CheckedChanged);
            //
            // _userNameLabel
            //
            this._userNameLabel.Enabled = false;
            this._userNameLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._userNameLabel.Location = new System.Drawing.Point(24, 48);
            this._userNameLabel.Name = "_userNameLabel";
            this._userNameLabel.Size = new System.Drawing.Size(88, 23);
            this._userNameLabel.TabIndex = 5;
            this._userNameLabel.Text = "&User name:";
            this._userNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _passwordLabel
            //
            this._passwordLabel.Enabled = false;
            this._passwordLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._passwordLabel.Location = new System.Drawing.Point(24, 76);
            this._passwordLabel.Name = "_passwordLabel";
            this._passwordLabel.Size = new System.Drawing.Size(88, 23);
            this._passwordLabel.TabIndex = 6;
            this._passwordLabel.Text = "&Password:";
            this._passwordLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _userNameBox
            //
            this._userNameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._userNameBox.Changed = false;
            this._userNameBox.Enabled = false;
            this._userNameBox.Location = new System.Drawing.Point(116, 46);
            this._userNameBox.Name = "_userNameBox";
            this._userNameBox.Size = new System.Drawing.Size(288, 21);
            this._userNameBox.TabIndex = 1;
            this._userNameBox.Text = "";
            //
            // _passwordBox
            //
            this._passwordBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._passwordBox.Changed = false;
            this._passwordBox.Enabled = false;
            this._passwordBox.Location = new System.Drawing.Point(116, 74);
            this._passwordBox.Name = "_passwordBox";
            this._passwordBox.PasswordChar = '*';
            this._passwordBox.Size = new System.Drawing.Size(288, 21);
            this._passwordBox.TabIndex = 2;
            this._passwordBox.Text = "";
            //
            // _displayNameLabel
            //
            this._displayNameLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._displayNameLabel.Location = new System.Drawing.Point(16, 124);
            this._displayNameLabel.Name = "_displayNameLabel";
            this._displayNameLabel.Size = new System.Drawing.Size(88, 23);
            this._displayNameLabel.TabIndex = 7;
            this._displayNameLabel.Text = "Your &name:";
            this._displayNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _emailLabel
            //
            this._emailLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._emailLabel.Location = new System.Drawing.Point(16, 152);
            this._emailLabel.Name = "_emailLabel";
            this._emailLabel.Size = new System.Drawing.Size(88, 23);
            this._emailLabel.TabIndex = 8;
            this._emailLabel.Text = "&E-mail address:";
            this._emailLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _displayNameBox
            //
            this._displayNameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._displayNameBox.Changed = false;
            this._displayNameBox.Location = new System.Drawing.Point(108, 120);
            this._displayNameBox.Name = "_displayNameBox";
            this._displayNameBox.Size = new System.Drawing.Size(300, 21);
            this._displayNameBox.TabIndex = 3;
            this._displayNameBox.Text = "";
            //
            // _emailBox
            //
            this._emailBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._emailBox.Changed = false;
            this._emailBox.Location = new System.Drawing.Point(108, 148);
            this._emailBox.Name = "_emailBox";
            this._emailBox.Size = new System.Drawing.Size(300, 21);
            this._emailBox.TabIndex = 4;
            this._emailBox.Text = "";
            //
            // _displayAsLabel
            //
            this._displayAsLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._displayAsLabel.Location = new System.Drawing.Point(16, 72);
            this._displayAsLabel.Name = "_displayAsLabel";
            this._displayAsLabel.Size = new System.Drawing.Size(88, 23);
            this._displayAsLabel.TabIndex = 9;
            this._displayAsLabel.Text = "&Display as:";
            this._displayAsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _displayAsTextBox
            //
            this._displayAsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._displayAsTextBox.Location = new System.Drawing.Point(108, 68);
            this._displayAsTextBox.Name = "_displayAsTextBox";
            this._displayAsTextBox.Size = new System.Drawing.Size(300, 21);
            this._displayAsTextBox.TabIndex = 2;
            this._displayAsTextBox.Text = "";
            //
            // _errorLabel
            //
            this._errorLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right | AnchorStyles.Top)));
            this._errorLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._errorLabel.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(192)), ((System.Byte)(0)), ((System.Byte)(0)));
            this._errorLabel.Location = new System.Drawing.Point(12, 276);
            this._errorLabel.Name = "_errorLabel";
            this._errorLabel.Size = new System.Drawing.Size(424, 20);
            this._errorLabel.TabIndex = 10;
            //
            // _helpButton
            //
            this._helpButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._helpButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._helpButton.Location = new System.Drawing.Point(360, 300);
            this._helpButton.Name = "_helpButton";
            this._helpButton.TabIndex = 3;
            this._helpButton.Text = "&Help";
            this._helpButton.Click += new System.EventHandler(this._helpButton_Click);
            //
            // _optionTabs
            //
            this._optionTabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._optionTabs.Controls.Add(this._generalPage);
            this._optionTabs.Controls.Add(this._securityPage);
            this._optionTabs.Controls.Add(this._downloadPage);
            this._optionTabs.Controls.Add(this._localSettingsPage);
            this._optionTabs.Controls.Add(this._postingPage);
            this._optionTabs.Controls.Add(this._signaturesPage);
            this._optionTabs.Location = new System.Drawing.Point(8, 8);
            this._optionTabs.Name = "_optionTabs";
            this._optionTabs.SelectedIndex = 0;
            this._optionTabs.Size = new System.Drawing.Size(428, 264);
            this._optionTabs.TabIndex = 0;
            //
            // _generalPage
            //
            this._generalPage.Controls.Add(this.groupBox2);
            this._generalPage.Controls.Add(this.groupBox1);
            this._generalPage.Controls.Add(this._abbreviateLevel);
            this._generalPage.Controls.Add(this._abbreviateCheckBox);
            this._generalPage.Controls.Add(this._serverLabel);
            this._generalPage.Controls.Add(this._serverName);
            this._generalPage.Controls.Add(this._portLabel);
            this._generalPage.Controls.Add(this._portValue);
            this._generalPage.Controls.Add(this._displayAsLabel);
            this._generalPage.Controls.Add(this._displayAsTextBox);
            this._generalPage.Controls.Add(this._displayNameLabel);
            this._generalPage.Controls.Add(this._displayNameBox);
            this._generalPage.Controls.Add(this._emailLabel);
            this._generalPage.Controls.Add(this._emailBox);
            this._generalPage.Location = new System.Drawing.Point(4, 22);
            this._generalPage.Name = "_generalPage";
            this._generalPage.Size = new System.Drawing.Size(420, 238);
            this._generalPage.TabIndex = 0;
            this._generalPage.Text = "General";
            //
            // groupBox2
            //
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Location = new System.Drawing.Point(16, 180);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(392, 8);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            //
            // groupBox1
            //
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Location = new System.Drawing.Point(16, 100);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(392, 8);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            //
            // _abbreviateLevel
            //
            this._abbreviateLevel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._abbreviateLevel.Changed = false;
            this._abbreviateLevel.Enabled = false;
            this._abbreviateLevel.Location = new System.Drawing.Point(360, 200);
            this._abbreviateLevel.Maximum = 10;
            this._abbreviateLevel.Minimum = 1;
            this._abbreviateLevel.Name = "_abbreviateLevel";
            this._abbreviateLevel.Size = new System.Drawing.Size(48, 21);
            this._abbreviateLevel.TabIndex = 6;
            //
            // _abbreviateCheckBox
            //
            this._abbreviateCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._abbreviateCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._abbreviateCheckBox.Location = new System.Drawing.Point(16, 200);
            this._abbreviateCheckBox.Name = "_abbreviateCheckBox";
            this._abbreviateCheckBox.Size = new System.Drawing.Size(340, 24);
            this._abbreviateCheckBox.TabIndex = 5;
            this._abbreviateCheckBox.Text = "A&bbreviate newsgroup names up to level:";
            this._abbreviateCheckBox.CheckedChanged += new System.EventHandler(this._abbreviateCheckBox_CheckedChanged);
            //
            // _securityPage
            //
            this._securityPage.Controls.Add(this._enableSSLCheckBox);
            this._securityPage.Controls.Add(this.groupBox3);
            this._securityPage.Controls.Add(this._authorizedAccessBox);
            this._securityPage.Controls.Add(this._userNameLabel);
            this._securityPage.Controls.Add(this._userNameBox);
            this._securityPage.Controls.Add(this._passwordLabel);
            this._securityPage.Controls.Add(this._passwordBox);
            this._securityPage.Location = new System.Drawing.Point(4, 22);
            this._securityPage.Name = "_securityPage";
            this._securityPage.Size = new System.Drawing.Size(420, 238);
            this._securityPage.TabIndex = 1;
            this._securityPage.Text = "Security";
            //
            // _enableSSLCheckBox
            //
            this._enableSSLCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._enableSSLCheckBox.Changed = false;
            this._enableSSLCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._enableSSLCheckBox.InvertValue = false;
            this._enableSSLCheckBox.Location = new System.Drawing.Point(16, 124);
            this._enableSSLCheckBox.Name = "_enableSSLCheckBox";
            this._enableSSLCheckBox.Size = new System.Drawing.Size(392, 24);
            this._enableSSLCheckBox.TabIndex = 12;
            this._enableSSLCheckBox.Text = "This server requires &secure connection (SSL)";
            this._enableSSLCheckBox.CheckedChanged += new System.EventHandler(this._enableSSLCheckBox_CheckedChanged);
            //
            // groupBox3
            //
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Location = new System.Drawing.Point(16, 104);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(392, 8);
            this.groupBox3.TabIndex = 11;
            this.groupBox3.TabStop = false;
            //
            // _downloadPage
            //
            this._downloadPage.Controls.Add(this._markFromMeAsRead);
            this._downloadPage.Controls.Add(this._downloadHeadersCheckBox);
            this._downloadPage.Controls.Add(this._downloadBodyOnSelectionCheckBox);
            this._downloadPage.Controls.Add(this.label3);
            this._downloadPage.Controls.Add(this._deliverFreqBox);
            this._downloadPage.Controls.Add(this.label2);
            this._downloadPage.Controls.Add(this._articlesCountBox);
            this._downloadPage.Controls.Add(this.label1);
            this._downloadPage.Controls.Add(this._scheduledDeliverCheckBox);
            this._downloadPage.Controls.Add(this._deliverOnStartupCheckBox);
            this._downloadPage.Location = new System.Drawing.Point(4, 22);
            this._downloadPage.Name = "_downloadPage";
            this._downloadPage.Size = new System.Drawing.Size(420, 238);
            this._downloadPage.TabIndex = 2;
            this._downloadPage.Text = "Download";
            //
            // _markFromMeAsRead
            //
            this._markFromMeAsRead.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._markFromMeAsRead.Changed = false;
            this._markFromMeAsRead.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._markFromMeAsRead.InvertValue = false;
            this._markFromMeAsRead.Location = new System.Drawing.Point(16, 96);
            this._markFromMeAsRead.Name = "_markFromMeAsRead";
            this._markFromMeAsRead.Size = new System.Drawing.Size(396, 24);
            this._markFromMeAsRead.TabIndex = 4;
            this._markFromMeAsRead.Text = "Mark articles from &me as read";
            //
            // _downloadHeadersCheckBox
            //
            this._downloadHeadersCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._downloadHeadersCheckBox.Changed = false;
            this._downloadHeadersCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._downloadHeadersCheckBox.InvertValue = false;
            this._downloadHeadersCheckBox.Location = new System.Drawing.Point(16, 124);
            this._downloadHeadersCheckBox.Name = "_downloadHeadersCheckBox";
            this._downloadHeadersCheckBox.Size = new System.Drawing.Size(396, 24);
            this._downloadHeadersCheckBox.TabIndex = 5;
            this._downloadHeadersCheckBox.Text = "On Deliver News, download only article &headers";
            this._downloadHeadersCheckBox.CheckedChanged += new EventHandler( _downloadHeadersCheckBox_CheckedChanged );
            //
            // _downloadBodyOnSelectionCheckBox
            //
            this._downloadBodyOnSelectionCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._downloadBodyOnSelectionCheckBox.Changed = false;
            this._downloadBodyOnSelectionCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._downloadBodyOnSelectionCheckBox.InvertValue = false;
            this._downloadBodyOnSelectionCheckBox.Location = new System.Drawing.Point(28, 148);
            this._downloadBodyOnSelectionCheckBox.Name = "_downloadBodyOnSelectionCheckBox";
            this._downloadBodyOnSelectionCheckBox.Size = new System.Drawing.Size(396, 24);
            this._downloadBodyOnSelectionCheckBox.TabIndex = 5;
            this._downloadBodyOnSelectionCheckBox.Enabled = false;
            this._downloadBodyOnSelectionCheckBox.Text = "Automatically download article when viewing in the &Preview Pane";
            //
            // label3
            //
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(232, 72);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(180, 23);
            this.label3.TabIndex = 6;
            this.label3.Text = "minutes";
            //
            // _deliverFreqBox
            //
            this._deliverFreqBox.Changed = false;
            this._deliverFreqBox.Location = new System.Drawing.Point(180, 68);
            this._deliverFreqBox.Maximum = 9999;
            this._deliverFreqBox.Minimum = 0;
            this._deliverFreqBox.Name = "_deliverFreqBox";
            this._deliverFreqBox.Size = new System.Drawing.Size(48, 21);
            this._deliverFreqBox.TabIndex = 3;
            //
            // label2
            //
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(232, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(180, 23);
            this.label2.TabIndex = 4;
            this.label2.Text = "articles from group at a time";
            //
            // _articlesCountBox
            //
            this._articlesCountBox.Changed = false;
            this._articlesCountBox.Location = new System.Drawing.Point(180, 12);
            this._articlesCountBox.Maximum = 9999;
            this._articlesCountBox.Minimum = 0;
            this._articlesCountBox.Name = "_articlesCountBox";
            this._articlesCountBox.Size = new System.Drawing.Size(48, 21);
            this._articlesCountBox.TabIndex = 0;
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(16, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(160, 23);
            this.label1.TabIndex = 2;
            this.label1.Text = "Download &not more than";
            //
            // _scheduledDeliverCheckBox
            //
            this._scheduledDeliverCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._scheduledDeliverCheckBox.Location = new System.Drawing.Point(16, 68);
            this._scheduledDeliverCheckBox.Name = "_scheduledDeliverCheckBox";
            this._scheduledDeliverCheckBox.Size = new System.Drawing.Size(152, 24);
            this._scheduledDeliverCheckBox.TabIndex = 2;
            this._scheduledDeliverCheckBox.Text = "Deliver News &every";
            //
            // _deliverOnStartupCheckBox
            //
            this._deliverOnStartupCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._deliverOnStartupCheckBox.Changed = false;
            this._deliverOnStartupCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._deliverOnStartupCheckBox.InvertValue = false;
            this._deliverOnStartupCheckBox.Location = new System.Drawing.Point(16, 40);
            this._deliverOnStartupCheckBox.Name = "_deliverOnStartupCheckBox";
            this._deliverOnStartupCheckBox.Size = new System.Drawing.Size(396, 24);
            this._deliverOnStartupCheckBox.TabIndex = 1;
            this._deliverOnStartupCheckBox.Text = "Deliver News on &startup";
            //
            // _localSettingsPage
            //
            this._localSettingsPage.Controls.Add(this._encodingsBox);
            this._localSettingsPage.Controls.Add(this.label4);
            this._localSettingsPage.Location = new System.Drawing.Point(4, 22);
            this._localSettingsPage.Name = "_localSettingsPage";
            this._localSettingsPage.Size = new System.Drawing.Size(420, 238);
            this._localSettingsPage.TabIndex = 4;
            this._localSettingsPage.Text = "International Settings";
            //
            // _encodingsBox
            //
            this._encodingsBox.Changed = false;
            this._encodingsBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._encodingsBox.Location = new System.Drawing.Point(140, 12);
            this._encodingsBox.Name = "_encodingsBox";
            this._encodingsBox.Size = new System.Drawing.Size(200, 21);
            this._encodingsBox.TabIndex = 11;
            //
            // label4
            //
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Location = new System.Drawing.Point(16, 16);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(124, 16);
            this.label4.TabIndex = 10;
            this.label4.Text = "Default &encoding:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _postingPage
            //
            this._postingPage.Controls.Add(this._formatGroupBox);
            this._postingPage.Controls.Add(this._putInOutbox);
            this._postingPage.Location = new System.Drawing.Point(4, 22);
            this._postingPage.Name = "_postingPage";
            this._postingPage.Size = new System.Drawing.Size(420, 238);
            this._postingPage.TabIndex = 3;
            this._postingPage.Text = "Posting";
            //
            // _formatGroupBox
            //
            this._formatGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._formatGroupBox.Changed = false;
            this._formatGroupBox.Controls.Add(this._mimeEncodingBox);
            this._formatGroupBox.Controls.Add(this.label7);
            this._formatGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._formatGroupBox.Location = new System.Drawing.Point(16, 16);
            this._formatGroupBox.Name = "_formatGroupBox";
            this._formatGroupBox.Size = new System.Drawing.Size(392, 72);
            this._formatGroupBox.TabIndex = 12;
            this._formatGroupBox.TabStop = false;
            this._formatGroupBox.Text = "Message &format";
            //
            // _putInOutbox
            //
            this._putInOutbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._putInOutbox.Changed = false;
            this._putInOutbox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._putInOutbox.InvertValue = false;
            this._putInOutbox.Location = new System.Drawing.Point(16, 96);
            this._putInOutbox.Name = "_putInOutbox";
            this._putInOutbox.Size = new System.Drawing.Size(392, 22);
            this._putInOutbox.TabIndex = 14;
            this._putInOutbox.Text = "On Send, put articles in Outbox rather than post immediatelly";
            //
            // _mimeEncodingBox
            //
            this._mimeEncodingBox.Changed = false;
            this._mimeEncodingBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._mimeEncodingBox.Location = new System.Drawing.Point(240, 44);
            this._mimeEncodingBox.Name = "_mimeEncodingBox";
            this._mimeEncodingBox.Size = new System.Drawing.Size(140, 21);
            this._mimeEncodingBox.TabIndex = 2;
            //
            // label7
            //
            this.label7.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label7.Location = new System.Drawing.Point(108, 48);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(128, 16);
            this.label7.TabIndex = 2;
            this.label7.Text = "&Encode text with:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _signaturesPage
            //
            this._signaturesPage.Controls.Add(this._grpSignatureInReplies);
            this._signaturesPage.Controls.Add(this._signatureBox);
            this._signaturesPage.Controls.Add(this._useSignature);
            this._signaturesPage.Controls.Add(this._chkSigSettingsOverrided);
            this._signaturesPage.Location = new System.Drawing.Point(4, 22);
            this._signaturesPage.Name = "_signaturesPage";
            this._signaturesPage.Size = new System.Drawing.Size(420, 238);
            this._signaturesPage.TabIndex = 5;
            this._signaturesPage.Text = "Signatures";
            //
            // _grpSignatureInReplies
            //
            this._chkSigSettingsOverrided.ThreeState = true;
            this._chkSigSettingsOverrided.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkSigSettingsOverrided.Location = new System.Drawing.Point(8, 8);
            this._chkSigSettingsOverrided.Name = "_chkSigSettingsOverrided";
            this._chkSigSettingsOverrided.Size = new System.Drawing.Size(280, 20);
            this._chkSigSettingsOverrided.TabIndex = 21;
            this._chkSigSettingsOverrided.Text = "Override &general settings";
            this._chkSigSettingsOverrided.CheckStateChanged += new EventHandler(_chkSigSettingsOverrided_CheckStateChanged);
            this._chkSigSettingsOverrided.CheckedChanged += new EventHandler(_chkSigSettingsOverrided_CheckedChanged);
            //
            // _useSignature
            //
            this._useSignature.Changed = true;
            this._useSignature.Checked = true;
            this._useSignature.CheckState = System.Windows.Forms.CheckState.Checked;
            this._useSignature.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._useSignature.InvertValue = false;
            this._useSignature.Location = new System.Drawing.Point(16, 28);
            this._useSignature.Name = "_useSignature";
            this._useSignature.Size = new System.Drawing.Size(280, 20);
            this._useSignature.TabIndex = 22;
            this._useSignature.Text = "Include signature in &outgoing messages";
            this._useSignature.CheckedChanged += new System.EventHandler(this._useSignature_CheckedChanged);
            //
            // _signatureBox
            //
            this._signatureBox.AcceptsReturn = true;
            this._signatureBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._signatureBox.Changed = false;
            this._signatureBox.Location = new System.Drawing.Point(24, 50);
            this._signatureBox.Multiline = true;
            this._signatureBox.Name = "_signatureBox";
            this._signatureBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._signatureBox.Size = new System.Drawing.Size(380, 78);
            this._signatureBox.TabIndex = 23;
            this._signatureBox.Text = "";
            //
            // _grpSignatureInReplies
            //
            this._grpSignatureInReplies.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._grpSignatureInReplies.Changed = false;
            this._grpSignatureInReplies.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._grpSignatureInReplies.Location = new System.Drawing.Point(24, 135);
            this._grpSignatureInReplies.Name = "_grpSignatureInReplies";
            this._grpSignatureInReplies.Size = new System.Drawing.Size(380, 96);
            this._grpSignatureInReplies.TabIndex = 23;
            this._grpSignatureInReplies.TabStop = false;
            this._grpSignatureInReplies.Text = "Signature in &Replies";
            //
            // AddServerForm
            //
            this.AcceptButton = this._okBtn;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._cancelBtn;
            this.ClientSize = new System.Drawing.Size(444, 334);
            this.Controls.Add(this._optionTabs);
            this.Controls.Add(this._helpButton);
            this.Controls.Add(this._errorLabel);
            this.Controls.Add(this._okBtn);
            this.Controls.Add(this._cancelBtn);
            this.MinimumSize = new System.Drawing.Size(452, 368);
            this.MaximumSize = new System.Drawing.Size(640, 384);
            this.Name = "AddServerForm";
            this.Text = "Add News Server";
            this.Closed += new System.EventHandler(this.AddServerForm_Closed);
            this.Activated += new System.EventHandler(this.AddServerForm_Activated);
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.AddServerForm_HelpRequested);
            this._optionTabs.ResumeLayout(false);
            this._generalPage.ResumeLayout(false);
            this._securityPage.ResumeLayout(false);
            this._downloadPage.ResumeLayout(false);
            this._localSettingsPage.ResumeLayout(false);
            this._postingPage.ResumeLayout(false);
            this._formatGroupBox.ResumeLayout(false);
            this._signaturesPage.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        private void AddServerForm_Activated(object sender, System.EventArgs e)
        {
            _serverName.Focus();
        }

        private void _serverName_TextChanged(object sender, System.EventArgs e)
        {
            UpdateOKButton();
        }

        private void _serverName_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if( e.Handled || _servers.Count > 1 )
            {
                return;
            }
            if( _displayAsTextBox.Text.Length == 0 || _displayAsTextBox.Text == _serverName.Text )
            {
                Core.UserInterfaceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( CopyDisplayName ) );
            }
        }

        private void _displayAsTextBox_TextChanged(object sender, System.EventArgs e)
        {
            UpdateOKButton();
        }

        private void _emailBox_TextChanged(object sender, System.EventArgs e)
        {
            UpdateOKButton();
        }

	    private void _authorizedAccessBox_CheckedChanged(object sender, System.EventArgs e)
        {
            bool enabled = !_userNameLabel.Enabled;
            _userNameLabel.Enabled = enabled;
            _passwordLabel.Enabled = enabled;
            _userNameBox.Enabled = enabled;
            _passwordBox.Enabled = enabled;
            if( enabled )
            {
                _userNameBox.Focus();
            }
        }

        private void _okBtn_Click(object sender, System.EventArgs e)
        {
            _okBtn.Enabled = false;
            AsyncProcessor uiProc = (AsyncProcessor) Core.UserInterfaceAP;
            if( uiProc.OutstandingJobs > 0 )
            {
                uiProc.DoJobs();
            }
            Core.ResourceAP.RunUniqueJob( new MethodInvoker( UpdateServerResource ) );
        }

	    private void _abbreviateCheckBox_CheckedChanged(object sender, System.EventArgs e)
        {
            if( _abbreviateLevel.Enabled = _abbreviateCheckBox.Checked )
            {
                if ( _abbreviateLevel.Determinated && _abbreviateLevel.Value == 0 )
                {
                    _abbreviateLevel.Minimum = 1;
                    _abbreviateLevel.Value = 1;
                    _abbreviateLevel.Changed = true;
                }
                _abbreviateLevel.Focus();
            }
        }

        private void _enableSSLCheckBox_CheckedChanged(object sender, System.EventArgs e)
        {
            if( _enableSSLCheckBox.Checked )
            {
                if( _portValue.Determinated && _portValue.Value == 119 )
                {
                    _portValue.Value = 563;
                    _portValue.Changed = true;
                }
            }
            else
            {
                if( _portValue.Determinated && _portValue.Value == 563 )
                {
                    _portValue.Value = 119;
                    _portValue.Changed = true;
                }
            }
        }
        private void _groupBox_CheckedChanged(object sender, System.EventArgs e)
        {
            string value = _formatGroupBox.GetValue() as string;
            if ( value != null )
            {
                _mimeEncodingBox.Enabled = value.Equals( "MIME" );
                if ( value.Equals( "MIME" ) )
                {
                    _mimeEncodingBox.Focus();
                }
            }
        }
        private void _useSignature_CheckedChanged(object sender, System.EventArgs e)
        {
            if( _signatureBox.Enabled = _grpSignatureInReplies.Enabled = _useSignature.Checked )
            {
                _signatureBox.Focus();
            }
        }

        private void _downloadHeadersCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _downloadBodyOnSelectionCheckBox.Enabled = _downloadHeadersCheckBox.Checked;
        }

        private void _helpButton_Click( object sender, System.EventArgs e )
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "/reference/news_server_properties.html" );
        }

        private void AddServerForm_HelpRequested( object sender, HelpEventArgs e )
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "/reference/news_server_properties.html" );
        }

        private void AddServerForm_Closed(object sender, System.EventArgs e)
        {
            _selectedTab = _optionTabs.SelectedIndex;
        }

        private void CopyDisplayName()
        {
            if( _servers.Count > 1 )
            {
                return;
            }
            _displayAsTextBox.Text = _serverName.Text;
            UpdateOKButton();
        }

        private void UpdateOKButton()
        {
            _errorLabel.Text = string.Empty;
            _okBtn.Enabled = true;
            if( _servers.Count == 1 && _serverName.Text.Length == 0 )
            {
                _errorLabel.Text = "Please enter the address of the server";
                _okBtn.Enabled = false;
            }
            else if( _emailBox.Determinated && _emailBox.Text.Length == 0 )
            {
                _errorLabel.Text = "Please enter the e-mail address for posting messages";
                _okBtn.Enabled = false;
            }
            else if( _servers.Count == 1 && _displayAsTextBox.Text.Length == 0 )
            {
                _errorLabel.Text = "Server's display name is not set";
                _okBtn.Enabled = false;
            }
            else
            {
                try
                {
                    if( _emailBox.Determinated )
                    {
                        new Uri( "mailto:" + _emailBox.Text );
                    }
                }
                catch( Exception exc )
                {
                    string text = exc.Message;
                    if( text.StartsWith( "Invalid URI: " ) )
                    {
                        text = text.Remove( 0, "Invalid URI: ".Length );
                    }
                    _errorLabel.Text = "Bad e-mail address: " + text;
                    _okBtn.Enabled = false;
                    return;
                }
                IResourceList servers = Core.ResourceStore.GetAllResources( NntpPlugin._newsServer );
                foreach( IResource server in servers )
                {
                    if( server != _server && server.DisplayName == _displayAsLabel.Text )
                    {
                        _errorLabel.Text = "News server with specified display name already exists";
                        _okBtn.Enabled = false;
                        return;
                    }
                }
                if( _servers.Count == 1 )
                {
                    string lastError = _servers[ 0 ].GetPropText( Core.Props.LastError );
                    if( lastError.Length > 0 )
                    {
                        _errorLabel.Text = "Connection to server fails due to reason: " + lastError;
                    }
                }
            }
        }

        private void UpdateServerResource()
        {
            bool needEndUpdate = false;
            try
            {
                if( !_server.IsTransient ) // if it's not new server
                {
                    needEndUpdate = true;
                    foreach ( IResource server in _servers )
                    {
                        server.BeginUpdate();
                    }
                }
                else
                {
                    Core.WorkspaceManager.AddToActiveWorkspaceRecursive( _server );
                    NewsFolders.AddToRoot( _server );
                }
                if ( !_abbreviateCheckBox.Checked )
                {
                    _abbreviateLevel.Minimum = 0;
                    _abbreviateLevel.Value = 0;
                    _abbreviateLevel.Changed = true;
                }
                if ( !_scheduledDeliverCheckBox.Checked )
                {
                    _deliverFreqBox.Minimum = -1;
                    _deliverFreqBox.Value = -1;
                    _deliverFreqBox.Changed = true;
                }
                if( !_authorizedAccessBox.Checked )
                {
                    _userNameBox.Text = string.Empty;
                    _userNameBox.Changed = true;
                    _passwordBox.Text = string.Empty;
                    _passwordBox.Changed = true;
                }

                SettingSaver.Save( Controls );
                if( _servers.Count == 1 )
                {
                    ServerResource server = new ServerResource( _server );
                    server.Name = _serverName.Text.Trim();
                    server.DisplayName = _displayAsTextBox.Text.Trim();

                    server.OverrideSig = _chkSigSettingsOverrided.Checked;
                    if( _chkSigSettingsOverrided.Checked )
                    {
                        server.UseSignature = _useSignature.Checked;
                        server.MailSignature = _signatureBox.Text;
                        server.ReplySignaturePosition = (SignaturePosition)_grpSignatureInReplies.Setting.Value;
                    }
                }
            }
            finally
            {
                if( needEndUpdate )
                {
                    foreach( IResource server in _servers )
                    {
                        server.EndUpdate();
                    }
                }
                else
                {
                    _server.EndUpdate();
                }
            }

            /**
             * add user identity info to myself contact
             */
            IContactManager cm = Core.ContactManager;
            IContact self = cm.MySelf;
            self.AddAccount( cm.FindOrCreateEmailAccount( _emailBox.Text ) );
            if( Core.ContactManager.GetFullName( self.Resource ).Length == 0 )
            {
                self.UpdateNameFields( _displayNameBox.Text );
            }
            Settings.LoadSettings();
        }

        private void _chkSigSettingsOverrided_CheckStateChanged(object sender, EventArgs e)
        {
        }

        private void _chkSigSettingsOverrided_CheckedChanged(object sender, EventArgs e)
        {
            _useSignature.Enabled = _signatureBox.Enabled =
            _grpSignatureInReplies.Enabled = _chkSigSettingsOverrided.Checked;
        }
    }
}
