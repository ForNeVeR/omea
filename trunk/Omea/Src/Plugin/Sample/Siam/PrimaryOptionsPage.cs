/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.Siam
{
	/// <summary>
	/// TODO: notify changes
	/// </summary>
	public class PrimaryOptionsPage : AbstractOptionsPane
	{
		private NumericUpDown _spinMonitoredItems;

		private Label _label1;

		private TextBox _txtFileName;

		private TextBox _txtURL;

		private RadioButton _radioSyncFile;

		private RadioButton _radioSyncHttp;

		private RadioButton _radioSyncNone;

		private CheckBox _checkSyncOnStartup;

		private CheckBox _checkSyncOnShutdown;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton _radioSyncFtp;
		private System.Windows.Forms.TextBox _txtFtpUri;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox _txtUsername;
		private System.Windows.Forms.TextBox _txtPassword;

		private IContainer components = null;

		public PrimaryOptionsPage()
		{
			// This call is required by the Windows Form Designer.
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
				if( components != null )
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this._label1 = new System.Windows.Forms.Label();
			this._spinMonitoredItems = new System.Windows.Forms.NumericUpDown();
			this._txtFileName = new System.Windows.Forms.TextBox();
			this._txtURL = new System.Windows.Forms.TextBox();
			this._radioSyncFile = new System.Windows.Forms.RadioButton();
			this._radioSyncHttp = new System.Windows.Forms.RadioButton();
			this._radioSyncNone = new System.Windows.Forms.RadioButton();
			this._checkSyncOnStartup = new System.Windows.Forms.CheckBox();
			this._checkSyncOnShutdown = new System.Windows.Forms.CheckBox();
			this.label2 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this._radioSyncFtp = new System.Windows.Forms.RadioButton();
			this._txtFtpUri = new System.Windows.Forms.TextBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this._txtUsername = new System.Windows.Forms.TextBox();
			this._txtPassword = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this._spinMonitoredItems)).BeginInit();
			this.SuspendLayout();
			// 
			// _label1
			// 
			this._label1.Location = new System.Drawing.Point(40, 216);
			this._label1.Name = "_label1";
			this._label1.Size = new System.Drawing.Size(160, 24);
			this._label1.TabIndex = 0;
			this._label1.Text = "&Items to include per feed:";
			this._label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _spinMonitoredItems
			// 
			this._spinMonitoredItems.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._spinMonitoredItems.Location = new System.Drawing.Point(216, 216);
			this._spinMonitoredItems.Maximum = new System.Decimal(new int[] {
																				1000,
																				0,
																				0,
																				0});
			this._spinMonitoredItems.Minimum = new System.Decimal(new int[] {
																				1,
																				0,
																				0,
																				0});
			this._spinMonitoredItems.Name = "_spinMonitoredItems";
			this._spinMonitoredItems.Size = new System.Drawing.Size(128, 20);
			this._spinMonitoredItems.TabIndex = 1;
			this._spinMonitoredItems.ThousandsSeparator = true;
			this._spinMonitoredItems.Value = new System.Decimal(new int[] {
																			  1,
																			  0,
																			  0,
																			  0});
			// 
			// _txtFileName
			// 
			this._txtFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._txtFileName.Location = new System.Drawing.Point(216, 72);
			this._txtFileName.Name = "_txtFileName";
			this._txtFileName.Size = new System.Drawing.Size(128, 20);
			this._txtFileName.TabIndex = 3;
			this._txtFileName.Text = "";
			// 
			// _txtURL
			// 
			this._txtURL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._txtURL.Location = new System.Drawing.Point(216, 104);
			this._txtURL.Name = "_txtURL";
			this._txtURL.Size = new System.Drawing.Size(128, 20);
			this._txtURL.TabIndex = 3;
			this._txtURL.Text = "";
			// 
			// _radioSyncFile
			// 
			this._radioSyncFile.Location = new System.Drawing.Point(40, 72);
			this._radioSyncFile.Name = "_radioSyncFile";
			this._radioSyncFile.Size = new System.Drawing.Size(176, 24);
			this._radioSyncFile.TabIndex = 4;
			this._radioSyncFile.Text = "Synchronize with &File:";
			this._radioSyncFile.CheckedChanged += new System.EventHandler(this.OnSyncSourceChanged);
			// 
			// _radioSyncHttp
			// 
			this._radioSyncHttp.Location = new System.Drawing.Point(40, 104);
			this._radioSyncHttp.Name = "_radioSyncHttp";
			this._radioSyncHttp.Size = new System.Drawing.Size(176, 24);
			this._radioSyncHttp.TabIndex = 4;
			this._radioSyncHttp.Text = "Synchronize via &HTTP, URL:";
			this._radioSyncHttp.CheckedChanged += new System.EventHandler(this.OnSyncSourceChanged);
			// 
			// _radioSyncNone
			// 
			this._radioSyncNone.Location = new System.Drawing.Point(40, 40);
			this._radioSyncNone.Name = "_radioSyncNone";
			this._radioSyncNone.Size = new System.Drawing.Size(176, 24);
			this._radioSyncNone.TabIndex = 4;
			this._radioSyncNone.Text = "&Do not synchronize feeds";
			this._radioSyncNone.CheckedChanged += new System.EventHandler(this.OnSyncSourceChanged);
			// 
			// _checkSyncOnStartup
			// 
			this._checkSyncOnStartup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._checkSyncOnStartup.Location = new System.Drawing.Point(40, 248);
			this._checkSyncOnStartup.Name = "_checkSyncOnStartup";
			this._checkSyncOnStartup.Size = new System.Drawing.Size(304, 24);
			this._checkSyncOnStartup.TabIndex = 5;
			this._checkSyncOnStartup.Text = "Automatically &synchronize-in on startup";
			// 
			// _checkSyncOnShutdown
			// 
			this._checkSyncOnShutdown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._checkSyncOnShutdown.Location = new System.Drawing.Point(40, 280);
			this._checkSyncOnShutdown.Name = "_checkSyncOnShutdown";
			this._checkSyncOnShutdown.Size = new System.Drawing.Size(304, 24);
			this._checkSyncOnShutdown.TabIndex = 5;
			this._checkSyncOnShutdown.Text = "Automatically synchronize-out on shut&down";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 8);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(48, 16);
			this.label2.TabIndex = 7;
			this.label2.Text = "General";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Location = new System.Drawing.Point(64, 16);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(288, 2);
			this.groupBox1.TabIndex = 8;
			this.groupBox1.TabStop = false;
			// 
			// _radioSyncFtp
			// 
			this._radioSyncFtp.Location = new System.Drawing.Point(40, 136);
			this._radioSyncFtp.Name = "_radioSyncFtp";
			this._radioSyncFtp.Size = new System.Drawing.Size(176, 24);
			this._radioSyncFtp.TabIndex = 4;
			this._radioSyncFtp.Text = "Synchronize via FT&P, URL:";
			this._radioSyncFtp.CheckedChanged += new System.EventHandler(this.OnSyncSourceChanged);
			// 
			// _txtFtpUri
			// 
			this._txtFtpUri.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._txtFtpUri.Location = new System.Drawing.Point(216, 136);
			this._txtFtpUri.Name = "_txtFtpUri";
			this._txtFtpUri.Size = new System.Drawing.Size(128, 20);
			this._txtFtpUri.TabIndex = 3;
			this._txtFtpUri.Text = "";
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Location = new System.Drawing.Point(104, 192);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(248, 3);
			this.groupBox2.TabIndex = 8;
			this.groupBox2.TabStop = false;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(8, 184);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(88, 16);
			this.label3.TabIndex = 7;
			this.label3.Text = "Synchronization";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// groupBox3
			// 
			this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox3.Location = new System.Drawing.Point(96, 336);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(256, 3);
			this.groupBox3.TabIndex = 8;
			this.groupBox3.TabStop = false;
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(8, 328);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(80, 16);
			this.label4.TabIndex = 7;
			this.label4.Text = "Authentication";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(40, 360);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(160, 24);
			this.label1.TabIndex = 0;
			this.label1.Text = "&User name:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(40, 392);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(160, 24);
			this.label5.TabIndex = 0;
			this.label5.Text = "&Password:";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _txtUsername
			// 
			this._txtUsername.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._txtUsername.Location = new System.Drawing.Point(216, 360);
			this._txtUsername.Name = "_txtUsername";
			this._txtUsername.Size = new System.Drawing.Size(128, 20);
			this._txtUsername.TabIndex = 3;
			this._txtUsername.Text = "";
			// 
			// _txtPassword
			// 
			this._txtPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._txtPassword.Location = new System.Drawing.Point(216, 384);
			this._txtPassword.Name = "_txtPassword";
			this._txtPassword.Size = new System.Drawing.Size(128, 20);
			this._txtPassword.TabIndex = 3;
			this._txtPassword.Text = "";
			// 
			// PrimaryOptionsPage
			// 
			this.AutoScroll = true;
			this.AutoScrollMinSize = new System.Drawing.Size(360, 424);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.label2);
			this.Controls.Add(this._checkSyncOnStartup);
			this.Controls.Add(this._radioSyncFile);
			this.Controls.Add(this._spinMonitoredItems);
			this.Controls.Add(this._label1);
			this.Controls.Add(this._txtFileName);
			this.Controls.Add(this._txtURL);
			this.Controls.Add(this._radioSyncHttp);
			this.Controls.Add(this._radioSyncNone);
			this.Controls.Add(this._checkSyncOnShutdown);
			this.Controls.Add(this._radioSyncFtp);
			this.Controls.Add(this._txtFtpUri);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.label5);
			this.Controls.Add(this._txtUsername);
			this.Controls.Add(this._txtPassword);
			this.Name = "PrimaryOptionsPage";
			this.Size = new System.Drawing.Size(360, 424);
			this.Load += new System.EventHandler(this.OnLoad);
			((System.ComponentModel.ISupportInitialize)(this._spinMonitoredItems)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		public static AbstractOptionsPane CreateInstance()
		{
			return new PrimaryOptionsPage();
		}

		private void OnLoad( object sender, EventArgs e )
		{
		}

		public override void ShowPane()
		{
			// Init simple values
			_spinMonitoredItems.Value = Core.SettingStore.ReadInt( Str.Name, Str.Option.MonitoredItems, Str.Option.MonitoredItems_Default );
			_spinMonitoredItems.Value = _spinMonitoredItems.Value > 0 ? _spinMonitoredItems.Value : Str.Option.MonitoredItems_Default;
			_txtFileName.Text = Core.SettingStore.ReadString( Str.Name, Str.Option.FileName );
			_txtURL.Text = Core.SettingStore.ReadString( Str.Name, Str.Option.Url );
			_txtFtpUri.Text = Core.SettingStore.ReadString( Str.Name, Str.Option.FtpUri, Str.Option.FtpUri_Default);
			_checkSyncOnStartup.Checked = Core.SettingStore.ReadBool( Str.Name, Str.Option.SyncOnStartup, Str.Option.SyncOnStartup_Default );
			_checkSyncOnShutdown.Checked = Core.SettingStore.ReadBool( Str.Name, Str.Option.SyncOnShutdown, Str.Option.SyncOnShutdown_Default );
			_txtUsername.Text = Core.SettingStore.ReadString( Str.Name, Str.Option.Username,  Str.Option.Username_Default );
			_txtPassword.Text = Core.SettingStore.ReadString( Str.Name, Str.Option.Password,  Str.Option.Password_Default );

			// Init radiobutton
			switch( Core.SettingStore.ReadString( Str.Name, Str.Option.Source, Str.Option.Source_None ) )
			{
			case Str.Option.Source_None:
				_radioSyncNone.Checked = true;
				break;

			case Str.Option.Source_File:
				_radioSyncFile.Checked = true;
				break;

			case Str.Option.Source_Http:
				_radioSyncHttp.Checked = true;
				break;

			case Str.Option.Source_Ftp:
				_radioSyncFtp.Checked = true;
				break;

			default: // Set to None if corrupted
				Core.SettingStore.WriteString( Str.Name, Str.Option.Source, Str.Option.Source_None );
				_radioSyncNone.Checked = true;
				break;
			}
		}

		public override void OK()
		{
			// Get simple values
			Core.SettingStore.WriteInt( Str.Name, Str.Option.MonitoredItems, (int) _spinMonitoredItems.Value );
			Core.SettingStore.WriteString( Str.Name, Str.Option.FileName, _txtFileName.Text );
			Core.SettingStore.WriteString( Str.Name, Str.Option.Url, _txtURL.Text );
			Core.SettingStore.WriteString( Str.Name, Str.Option.FtpUri, _txtFtpUri.Text );
			Core.SettingStore.WriteBool( Str.Name, Str.Option.SyncOnStartup, _checkSyncOnStartup.Checked );
			Core.SettingStore.WriteBool( Str.Name, Str.Option.SyncOnShutdown, _checkSyncOnShutdown.Checked );
			Core.SettingStore.WriteString(Str.Name, Str.Option.Username,  _txtUsername.Text);
			Core.SettingStore.WriteString(Str.Name, Str.Option.Password,  _txtPassword.Text );

			// Get radiobutton
			if( _radioSyncFile.Checked )
				Core.SettingStore.WriteString( Str.Name, Str.Option.Source, Str.Option.Source_File );
			else if( _radioSyncHttp.Checked )
				Core.SettingStore.WriteString( Str.Name, Str.Option.Source, Str.Option.Source_Http );
			else if(_radioSyncFtp.Checked)
				Core.SettingStore.WriteString( Str.Name, Str.Option.Source, Str.Option.Source_Ftp );
			else
                Core.SettingStore.WriteString( Str.Name, Str.Option.Source, Str.Option.Source_None );
		}

		/// <summary>
		/// The sync source has changed, update the editboxes
		/// </summary>
		private void OnSyncSourceChanged(object sender, System.EventArgs e)
		{
			_txtFileName.Enabled = _radioSyncFile.Checked;
			_txtURL.Enabled = _radioSyncHttp.Checked;

			_txtFtpUri.Enabled = _txtUsername.Enabled = _txtPassword.Enabled = _radioSyncFtp.Checked;
		}

		
	}
}