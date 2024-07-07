// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.IO;
using System.Net;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;
using Microsoft.Win32;

namespace JetBrains.Omea
{
    public class ProxyConfigPane : AbstractOptionsPane
    {
        private System.Windows.Forms.RadioButton _defaultProxyButton;
        private System.Windows.Forms.RadioButton _configureProxyButton;
        private System.Windows.Forms.Label _addressLabel;
        private System.Windows.Forms.TextBox _address;
        private System.Windows.Forms.Label _portLabel;
        private System.Windows.Forms.NumericUpDown _port;
        private System.Windows.Forms.Label _bypassLabel;
        private System.Windows.Forms.Label _helpLabel;
        private System.Windows.Forms.TextBox _bypassList;
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Label _userLabel;
        private System.Windows.Forms.Label _passwordLabel;
        private System.Windows.Forms.TextBox _user;
        private System.Windows.Forms.TextBox _password;
        private System.Windows.Forms.Label _lblDefaultSetting;
        private System.Windows.Forms.CheckBox _bypassLocal;

        public static AbstractOptionsPane ProxyConfigPaneCreator()
        {
            return new ProxyConfigPane();
        }

        public ProxyConfigPane()
        {
            InitializeComponent();
        }

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
            this._defaultProxyButton = new System.Windows.Forms.RadioButton();
            this._configureProxyButton = new System.Windows.Forms.RadioButton();
            this._addressLabel = new System.Windows.Forms.Label();
            this._address = new System.Windows.Forms.TextBox();
            this._portLabel = new System.Windows.Forms.Label();
            this._port = new System.Windows.Forms.NumericUpDown();
            this._bypassLabel = new System.Windows.Forms.Label();
            this._helpLabel = new System.Windows.Forms.Label();
            this._bypassList = new System.Windows.Forms.TextBox();
            this._userLabel = new System.Windows.Forms.Label();
            this._passwordLabel = new System.Windows.Forms.Label();
            this._user = new System.Windows.Forms.TextBox();
            this._password = new System.Windows.Forms.TextBox();
            this._bypassLocal = new System.Windows.Forms.CheckBox();
            this._lblDefaultSetting = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this._port)).BeginInit();
            this.SuspendLayout();
            //
            // _defaultProxyButton
            //
            this._defaultProxyButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._defaultProxyButton.Location = new System.Drawing.Point(0, 0);
            this._defaultProxyButton.Name = "_defaultProxyButton";
            this._defaultProxyButton.Size = new System.Drawing.Size(180, 24);
            this._defaultProxyButton.TabIndex = 0;
            this._defaultProxyButton.Text = "Use default proxy setting";
            this._defaultProxyButton.CheckedChanged += new System.EventHandler(this._defaultProxyButton_CheckedChanged);
            //
            // _configureProxyButton
            //
            this._configureProxyButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._configureProxyButton.Location = new System.Drawing.Point(0, 48);
            this._configureProxyButton.Name = "_configureProxyButton";
            this._configureProxyButton.Size = new System.Drawing.Size(196, 20);
            this._configureProxyButton.TabIndex = 1;
            this._configureProxyButton.Text = "Configure proxy server";
            this._configureProxyButton.CheckedChanged += new System.EventHandler(this._configureProxyButton_CheckedChanged);
            //
            // _addressLabel
            //
            this._addressLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._addressLabel.Location = new System.Drawing.Point(8, 76);
            this._addressLabel.Name = "_addressLabel";
            this._addressLabel.Size = new System.Drawing.Size(64, 20);
            this._addressLabel.TabIndex = 2;
            this._addressLabel.Text = "Address:";
            this._addressLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _address
            //
            this._address.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._address.Location = new System.Drawing.Point(80, 72);
            this._address.Name = "_address";
            this._address.Size = new System.Drawing.Size(288, 21);
            this._address.TabIndex = 2;
            this._address.Text = "";
            //
            // _portLabel
            //
            this._portLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._portLabel.Location = new System.Drawing.Point(8, 100);
            this._portLabel.Name = "_portLabel";
            this._portLabel.Size = new System.Drawing.Size(64, 20);
            this._portLabel.TabIndex = 4;
            this._portLabel.Text = "Port:";
            this._portLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _port
            //
            this._port.Location = new System.Drawing.Point(80, 96);
            this._port.Maximum = new System.Decimal(new int[] {
                                                                  65535,
                                                                  0,
                                                                  0,
                                                                  0});
            this._port.Minimum = new System.Decimal(new int[] {
                                                                  1,
                                                                  0,
                                                                  0,
                                                                  0});
            this._port.Name = "_port";
            this._port.Size = new System.Drawing.Size(56, 21);
            this._port.TabIndex = 3;
            this._port.Value = new System.Decimal(new int[] {
                                                                3128,
                                                                0,
                                                                0,
                                                                0});
            //
            // _bypassLabel
            //
            this._bypassLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._bypassLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._bypassLabel.Location = new System.Drawing.Point(8, 196);
            this._bypassLabel.Name = "_bypassLabel";
            this._bypassLabel.Size = new System.Drawing.Size(360, 20);
            this._bypassLabel.TabIndex = 6;
            this._bypassLabel.Text = "Bypass proxy server for the following addresses:";
            this._bypassLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _helpLabel
            //
            this._helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._helpLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._helpLabel.Location = new System.Drawing.Point(8, 256);
            this._helpLabel.Name = "_helpLabel";
            this._helpLabel.Size = new System.Drawing.Size(360, 23);
            this._helpLabel.TabIndex = 7;
            this._helpLabel.Text = "Use semicolon \';\' to separate entries.";
            this._helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _bypassList
            //
            this._bypassList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._bypassList.Location = new System.Drawing.Point(8, 216);
            this._bypassList.Multiline = true;
            this._bypassList.Name = "_bypassList";
            this._bypassList.Size = new System.Drawing.Size(360, 40);
            this._bypassList.TabIndex = 7;
            this._bypassList.Text = "";
            //
            // _userLabel
            //
            this._userLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._userLabel.Location = new System.Drawing.Point(8, 124);
            this._userLabel.Name = "_userLabel";
            this._userLabel.Size = new System.Drawing.Size(64, 16);
            this._userLabel.TabIndex = 9;
            this._userLabel.Text = "User:";
            this._userLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _passwordLabel
            //
            this._passwordLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._passwordLabel.Location = new System.Drawing.Point(8, 148);
            this._passwordLabel.Name = "_passwordLabel";
            this._passwordLabel.Size = new System.Drawing.Size(68, 20);
            this._passwordLabel.TabIndex = 10;
            this._passwordLabel.Text = "Password:";
            this._passwordLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _user
            //
            this._user.Location = new System.Drawing.Point(80, 120);
            this._user.Name = "_user";
            this._user.TabIndex = 4;
            this._user.Text = "";
            //
            // _password
            //
            this._password.Location = new System.Drawing.Point(80, 144);
            this._password.Name = "_password";
            this._password.PasswordChar = '*';
            this._password.TabIndex = 5;
            this._password.Text = "";
            //
            // _bypassLocal
            //
            this._bypassLocal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._bypassLocal.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._bypassLocal.Location = new System.Drawing.Point(8, 168);
            this._bypassLocal.Name = "_bypassLocal";
            this._bypassLocal.Size = new System.Drawing.Size(360, 24);
            this._bypassLocal.TabIndex = 6;
            this._bypassLocal.Text = "Bypass proxy server for local addresses";
            //
            // _lblDefaultSetting
            //
            this._lblDefaultSetting.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblDefaultSetting.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblDefaultSetting.Location = new System.Drawing.Point(28, 28);
            this._lblDefaultSetting.Name = "_lblDefaultSetting";
            this._lblDefaultSetting.Size = new System.Drawing.Size(344, 16);
            this._lblDefaultSetting.TabIndex = 11;
            this._lblDefaultSetting.Text = "Default setting: No proxy specified";
            //
            // ProxyConfigPane
            //
            this.Controls.Add(this._lblDefaultSetting);
            this.Controls.Add(this._bypassLocal);
            this.Controls.Add(this._password);
            this.Controls.Add(this._user);
            this.Controls.Add(this._passwordLabel);
            this.Controls.Add(this._userLabel);
            this.Controls.Add(this._bypassList);
            this.Controls.Add(this._helpLabel);
            this.Controls.Add(this._bypassLabel);
            this.Controls.Add(this._port);
            this.Controls.Add(this._portLabel);
            this.Controls.Add(this._address);
            this.Controls.Add(this._addressLabel);
            this.Controls.Add(this._configureProxyButton);
            this.Controls.Add(this._defaultProxyButton);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "ProxyConfigPane";
            this.Size = new System.Drawing.Size(376, 304);
            ((System.ComponentModel.ISupportInitialize)(this._port)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        private void _defaultProxyButton_CheckedChanged(object sender, System.EventArgs e)
        {
            _addressLabel.Enabled = _address.Enabled = _portLabel.Enabled = _port.Enabled =
                _userLabel.Enabled = _user.Enabled = _passwordLabel.Enabled = _password.Enabled =
                _bypassLocal.Enabled = _bypassLabel.Enabled = _bypassList.Enabled = _helpLabel.Enabled = false;
        }

        private void _configureProxyButton_CheckedChanged(object sender, System.EventArgs e)
        {
            _addressLabel.Enabled = _address.Enabled = _portLabel.Enabled = _port.Enabled =
                _userLabel.Enabled = _user.Enabled = _passwordLabel.Enabled = _password.Enabled =
                _bypassLocal.Enabled = _bypassLabel.Enabled = _bypassList.Enabled = _helpLabel.Enabled = true;
        }

        public override void ShowPane()
        {
            ISettingStore ini = Core.SettingStore;
            _address.Text = ini.ReadString( "HttpProxy", "Address" );
            int port = ini.ReadInt( "HttpProxy", "Port", 3128 );
            if( port < (int) _port.Minimum )
                port = (int) _port.Minimum;
            else if( port > (int) _port.Maximum )
                port = (int) _port.Maximum;
            _port.Value = port;
            _user.Text = ini.ReadString( "HttpProxy", "User" );
            _password.Text = ini.ReadString( "HttpProxy", "Password" );
            _bypassLocal.Checked = ini.ReadBool( "HttpProxy", "BypassLocal", true );
            _bypassList.Text = ini.ReadString( "HttpProxy", "BypassList" );

            if( _address.Text.Length > 0 )
                _configureProxyButton.Checked = true;
            else
                _defaultProxyButton.Checked = true;

            if ( IsDefaultProxyAutoconfigured() )
            {
                _lblDefaultSetting.Text = "Autoconfigure script not supported, please specify proxy settings manually";
            }
            else if ( WebProxy.GetDefaultProxy().Address != null )
            {
                _lblDefaultSetting.Text = "Default proxy setting: " + WebProxy.GetDefaultProxy().Address;
            }
            else
            {
                _lblDefaultSetting.Text = "Default proxy setting: No proxy specified";
            }
        }

        private static bool IsDefaultProxyAutoconfigured()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey( @"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\Connections" );
            if ( regKey == null )
            {
                return false;
            }

            byte[] defaultConnectionSettings = (byte[]) regKey.GetValue( "DefaultConnectionSettings" );
            regKey.Close();

            if ( defaultConnectionSettings == null || defaultConnectionSettings.Length < 12 )
            {
                return false;
            }

            JetMemoryStream settingStream = new JetMemoryStream( defaultConnectionSettings, true );
            BinaryReader reader = new BinaryReader( settingStream, Encoding.UTF8 );
            int length = reader.ReadInt32();

            bool isAutoProxy = false;
            if ( length >= 60 )
            {
                reader.ReadInt32();  // settings version
                int flags = reader.ReadInt32();
                if ( ( flags & PROXY_TYPE_AUTO_PROXY_URL ) != 0 )
                {
                    isAutoProxy = true;
                }
            }
            return isAutoProxy;
        }

        private const int PROXY_TYPE_AUTO_PROXY_URL = 4;

        public override void OK()
        {
            ISettingStore ini = Core.SettingStore;
            if( _defaultProxyButton.Checked )
            {
                ini.WriteString( "HttpProxy", "Address", string.Empty );
            }
            else
            {
                string address = _address.Text;
                ini.WriteString( "HttpProxy", "Address", address );
                ini.WriteInt( "HttpProxy", "Port", (int) _port.Value );
                ini.WriteString( "HttpProxy", "User", _user.Text );
                ini.WriteString( "HttpProxy", "Password", _password.Text );
                ini.WriteBool( "HttpProxy", "BypassLocal", _bypassLocal.Checked );
                ini.WriteString( "HttpProxy", "BypassList", _bypassList.Text );
            }
            HttpReader.LoadHttpConfig();
        }

        public override string GetHelpKeyword()
        {
            return "/reference/proxy_configuration.html";
        }
    }
}
