// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.InstantMessaging.Miranda
{
    /// <summary>
    /// Options pane for the Miranda plugin.
    /// </summary>
    public class MirandaOptionsPane: AbstractOptionsPane
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox _lbxProfiles;
        private System.Windows.Forms.CheckBox _chkCreateCategories;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton _radSyncImmediate;
        private System.Windows.Forms.RadioButton _radSyncStartup;
        private System.Windows.Forms.CheckBox _chkLatestOnTop;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown _udConversationPeriod;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public MirandaOptionsPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
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
            this._lbxProfiles = new System.Windows.Forms.ListBox();
            this._chkCreateCategories = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._radSyncStartup = new System.Windows.Forms.RadioButton();
            this._radSyncImmediate = new System.Windows.Forms.RadioButton();
            this._chkLatestOnTop = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this._udConversationPeriod = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._udConversationPeriod)).BeginInit();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(212, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Miranda &profile to index:";
            //
            // _lbxProfiles
            //
            this._lbxProfiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lbxProfiles.Location = new System.Drawing.Point(0, 24);
            this._lbxProfiles.Name = "_lbxProfiles";
            this._lbxProfiles.Size = new System.Drawing.Size(392, 95);
            this._lbxProfiles.TabIndex = 1;
            //
            // _chkCreateCategories
            //
            this._chkCreateCategories.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkCreateCategories.Location = new System.Drawing.Point(0, 212);
            this._chkCreateCategories.Name = "_chkCreateCategories";
            this._chkCreateCategories.Size = new System.Drawing.Size(320, 16);
            this._chkCreateCategories.TabIndex = 6;
            this._chkCreateCategories.Text = "Create &categories from contact groups";
            //
            // groupBox1
            //
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this._radSyncStartup);
            this.groupBox1.Controls.Add(this._radSyncImmediate);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox1.Location = new System.Drawing.Point(0, 124);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(392, 60);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Synchronize database";
            //
            // _radSyncStartup
            //
            this._radSyncStartup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radSyncStartup.Location = new System.Drawing.Point(8, 36);
            this._radSyncStartup.Name = "_radSyncStartup";
            this._radSyncStartup.Size = new System.Drawing.Size(128, 20);
            this._radSyncStartup.TabIndex = 1;
            this._radSyncStartup.Text = "&On startup";
            //
            // _radSyncImmediate
            //
            this._radSyncImmediate.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radSyncImmediate.Location = new System.Drawing.Point(8, 16);
            this._radSyncImmediate.Name = "_radSyncImmediate";
            this._radSyncImmediate.Size = new System.Drawing.Size(128, 20);
            this._radSyncImmediate.TabIndex = 0;
            this._radSyncImmediate.Text = "&Immediately";
            //
            // _chkLatestOnTop
            //
            this._chkLatestOnTop.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkLatestOnTop.Location = new System.Drawing.Point(0, 232);
            this._chkLatestOnTop.Name = "_chkLatestOnTop";
            this._chkLatestOnTop.Size = new System.Drawing.Size(320, 16);
            this._chkLatestOnTop.TabIndex = 7;
            this._chkLatestOnTop.Text = "Show &latest messages in conversations on top";
            //
            // label4
            //
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Location = new System.Drawing.Point(324, 192);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 16);
            this.label4.TabIndex = 5;
            this.label4.Text = "minutes";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _udConversationPeriod
            //
            this._udConversationPeriod.Location = new System.Drawing.Point(264, 188);
            this._udConversationPeriod.Maximum = new System.Decimal(new int[] {
                                                                                  14400,
                                                                                  0,
                                                                                  0,
                                                                                  0});
            this._udConversationPeriod.Minimum = new System.Decimal(new int[] {
                                                                                  1,
                                                                                  0,
                                                                                  0,
                                                                                  0});
            this._udConversationPeriod.Name = "_udConversationPeriod";
            this._udConversationPeriod.Size = new System.Drawing.Size(56, 20);
            this._udConversationPeriod.TabIndex = 4;
            this._udConversationPeriod.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._udConversationPeriod.Value = new System.Decimal(new int[] {
                                                                                120,
                                                                                0,
                                                                                0,
                                                                                0});
            //
            // label5
            //
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label5.Location = new System.Drawing.Point(4, 192);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(256, 16);
            this.label5.TabIndex = 3;
            this.label5.Text = "&Maximum time span between messages:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // MirandaOptionsPane
            //
            this.Controls.Add(this.label4);
            this.Controls.Add(this._udConversationPeriod);
            this.Controls.Add(this.label5);
            this.Controls.Add(this._chkLatestOnTop);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this._chkCreateCategories);
            this.Controls.Add(this._lbxProfiles);
            this.Controls.Add(this.label1);
            this.Name = "MirandaOptionsPane";
            this.Size = new System.Drawing.Size(408, 256);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._udConversationPeriod)).EndInit();
            this.ResumeLayout(false);

        }
		#endregion

        public override void ShowPane()
        {
            ISettingStore ini = Core.SettingStore;

            string curProfile = ini.ReadString( "Miranda", "ProfileToIndex" );
            _lbxProfiles.Items.Add( "<none>" );
            foreach( string profile in ProfileManager.GetProfileList() )
            {
                _lbxProfiles.Items.Add( profile );
                if ( curProfile == profile )
                {
                    _lbxProfiles.SelectedIndex = _lbxProfiles.Items.Count-1;
                }
            }
            if ( _lbxProfiles.SelectedIndex < 0 )
            {
                // if we're running in Startup Wizard, select the first profile by default;
                // otherwise, the user selected None and we must keep the selection
                if ( IsStartupPane && _lbxProfiles.Items.Count > 1 )
                {
                    _lbxProfiles.SelectedIndex = 1;
                }
                else
                {
                    _lbxProfiles.SelectedIndex = 0;
                }
            }
            _chkCreateCategories.Checked = IniSettings.CreateCategories;
            _chkLatestOnTop.Checked = IniSettings.LatestOnTop;

            if ( IniSettings.SyncImmediate )
            {
                _radSyncImmediate.Checked = true;
            }
            else
            {
                _radSyncStartup.Checked = true;
            }

            _udConversationPeriod.Value = IniSettings.ConversationPeriod / 60;

            if ( IsStartupPane )
            {
                groupBox1.Visible = false;
                _chkLatestOnTop.Visible = false;
                label4.Visible = false;
                label5.Visible = false;
                _udConversationPeriod.Visible = false;
                _chkCreateCategories.Top = _lbxProfiles.Bottom + 8;
            }
        }

        public override void OK()
        {
            ISettingStore ini = Core.SettingStore;

            string profile = (_lbxProfiles.SelectedIndex <= 0) ? "" : (string) _lbxProfiles.SelectedItem;
            ini.WriteString( "Miranda", "ProfileToIndex", profile );
            IniSettings.CreateCategories = _chkCreateCategories.Checked;
            IniSettings.LatestOnTop = _chkLatestOnTop.Checked;
            IniSettings.SyncImmediate = _radSyncImmediate.Checked;
            IniSettings.ConversationPeriod = (int) _udConversationPeriod.Value * 60;
        }

        public override string GetHelpKeyword()
        {
            return "/reference/miranda.html";
        }
	}
}
