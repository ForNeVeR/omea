// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    public class OutlookOptionsPane : JetBrains.Omea.OpenAPI.AbstractOptionsPane
    {
        private System.Windows.Forms.Panel panel1;
        private CheckBoxSettingEditor _deliverOnStartup;
        private System.Windows.Forms.Label label2;
        private NumericUpDownSettingEditor _sendReceiveTimeout;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox4;
        private CheckBoxSettingEditor _showEmbedPics;
        private CheckBoxSettingEditor _scheduleDeliver;
        private RadioButtonSettingEditor _syncMode;
        private CheckBoxSettingEditor _markAsReadOnReply;
        private CheckBoxSettingEditor _setCategoryFromContactWhenEmailArrived;
        private CheckBoxSettingEditor _setCategoryFromContactWhenEmailSent;

        private System.Windows.Forms.GroupBox _grpExpRules;
        private System.Windows.Forms.Button buttonEditDefExpRule;
        private System.Windows.Forms.Label labelDefaultExpRule;
        private System.Windows.Forms.Label labelExpRuleForDeleted;
        private System.Windows.Forms.Button buttonEditExpRuleForDeleted;
        private System.Windows.Forms.Button buttonClearDefault;
        private System.Windows.Forms.Button buttonClearDeleted;

        private System.Windows.Forms.GroupBox _grpFontChars;
        private System.Windows.Forms.CheckBox _chkOverrideFont;
        private System.Windows.Forms.Label _lblFontFamily;
        private System.Windows.Forms.Button _btnChangeFont;
        private System.Windows.Forms.TextBox _txtFont;
        private System.Windows.Forms.Label _lblNote;

        private string          _currFont;
        private int             _currFontSize;

        public OutlookOptionsPane()
        {
            InitializeComponent();
        }

        private void  ReadFontCharacteristics()
        {
            _currFont = Core.UIManager.DefaultFontFace;
            _currFontSize = (int)Core.UIManager.DefaultFontSize;

            _chkOverrideFont.Checked = Core.SettingStore.ReadBool( "Outlook", "MailFontOverride", false );
            if( _chkOverrideFont.Checked )
            {
                _currFont = Core.SettingStore.ReadString( "Outlook", "MailFont", Core.UIManager.DefaultFontFace );
                _currFontSize = Core.SettingStore.ReadInt( "Outlook", "MailFontSize", (int)Core.UIManager.DefaultFontSize );
            }
            _txtFont.Text = _currFont + ", " + _currFontSize;
        }
        private void  WriteFontCharacteristics()
        {
            Core.SettingStore.WriteBool( "Outlook", "MailFontOverride", _chkOverrideFont.Checked );
            if( _chkOverrideFont.Checked )
            {
                Core.SettingStore.WriteString( "Outlook", "MailFont", _currFont );
                Core.SettingStore.WriteInt( "Outlook", "MailFontSize", _currFontSize );
            }
        }

        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if (components != null)
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
            this.panel1 = new System.Windows.Forms.Panel();
            this._syncMode = new JetBrains.Omea.GUIControls.RadioButtonSettingEditor();
            this._scheduleDeliver = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this._deliverOnStartup = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this.label2 = new System.Windows.Forms.Label();
            this._sendReceiveTimeout = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
            this._showEmbedPics = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this._markAsReadOnReply = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._setCategoryFromContactWhenEmailArrived = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._setCategoryFromContactWhenEmailSent = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();

            this._grpExpRules = new System.Windows.Forms.GroupBox();
            this.labelDefaultExpRule = new System.Windows.Forms.Label();
            this.buttonEditExpRuleForDeleted = new System.Windows.Forms.Button();
            this.buttonClearDefault = new System.Windows.Forms.Button();
            this.labelExpRuleForDeleted = new System.Windows.Forms.Label();
            this.buttonEditDefExpRule = new System.Windows.Forms.Button();
            this.buttonClearDeleted = new System.Windows.Forms.Button();

            this._grpFontChars = new System.Windows.Forms.GroupBox();
            this._chkOverrideFont = new System.Windows.Forms.CheckBox();
			this._lblFontFamily = new System.Windows.Forms.Label();
            this._txtFont = new System.Windows.Forms.TextBox();
            this._btnChangeFont = new System.Windows.Forms.Button();
            this._lblNote = new System.Windows.Forms.Label();

            this._grpFontChars.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this._grpExpRules.SuspendLayout();
            this.SuspendLayout();
            //
            // panel1
            //
            this.panel1.Controls.Add(this._syncMode);
            this.panel1.Controls.Add(this._scheduleDeliver);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this._deliverOnStartup);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this._sendReceiveTimeout);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(404, 172);
            this.panel1.TabIndex = 0;
            //
            // _syncMode
            //
            this._syncMode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._syncMode.Changed = false;
            this._syncMode.Location = new System.Drawing.Point(0, 80);
            this._syncMode.Name = "_syncMode";
            this._syncMode.Size = new System.Drawing.Size(404, 88);
            this._syncMode.TabIndex = 15;
            this._syncMode.TabStop = false;
            this._syncMode.Text = "Synchronize Outlook folders on startup";
            //
            // _scheduleDeliver
            //
            this._scheduleDeliver.Changed = false;
            this._scheduleDeliver.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._scheduleDeliver.InvertValue = false;
            this._scheduleDeliver.Location = new System.Drawing.Point(0, 56);
            this._scheduleDeliver.Name = "_scheduleDeliver";
            this._scheduleDeliver.Size = new System.Drawing.Size(228, 20);
            this._scheduleDeliver.TabIndex = 13;
            this._scheduleDeliver.Text = "&Schedule a send/receive every:";
            this._scheduleDeliver.Click += new System.EventHandler(this._scheduleDeliver_Click);
            //
            // label3
            //
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(0, 8);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(88, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "Send/Receive";
            //
            // groupBox2
            //
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Location = new System.Drawing.Point(88, 8);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(312, 8);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            //
            // _deliverOnStartup
            //
            this._deliverOnStartup.Changed = false;
            this._deliverOnStartup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._deliverOnStartup.InvertValue = false;
            this._deliverOnStartup.Location = new System.Drawing.Point(0, 28);
            this._deliverOnStartup.Name = "_deliverOnStartup";
            this._deliverOnStartup.Size = new System.Drawing.Size(396, 24);
            this._deliverOnStartup.TabIndex = 2;
            this._deliverOnStartup.Text = "Send/&receive mail on Omea startup.";
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(284, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(68, 16);
            this.label2.TabIndex = 8;
            this.label2.Text = "minute(s)";
            //
            // _sendReceiveTimeout
            //
            this._sendReceiveTimeout.Changed = false;
            this._sendReceiveTimeout.Location = new System.Drawing.Point(236, 52);
            this._sendReceiveTimeout.Maximum = 1000;
            this._sendReceiveTimeout.Minimum = 1;
            this._sendReceiveTimeout.Name = "_sendReceiveTimeout";
            this._sendReceiveTimeout.Size = new System.Drawing.Size(44, 20);
            this._sendReceiveTimeout.TabIndex = 7;
            //
            // _showEmbedPics
            //
            this._showEmbedPics.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._showEmbedPics.Changed = false;
            this._showEmbedPics.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._showEmbedPics.InvertValue = false;
            this._showEmbedPics.Location = new System.Drawing.Point(0, 24);
            this._showEmbedPics.Name = "_showEmbedPics";
            this._showEmbedPics.Size = new System.Drawing.Size(400, 20);
            this._showEmbedPics.TabIndex = 12;
            this._showEmbedPics.Text = "Show &embedded pictures";
            //
            // groupBox4
            //
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Location = new System.Drawing.Point(60, 4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(344, 8);
            this.groupBox4.TabIndex = 11;
            this.groupBox4.TabStop = false;
            //
            // label6
            //
            this.label6.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label6.Location = new System.Drawing.Point(0, 4);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(56, 16);
            this.label6.TabIndex = 10;
            this.label6.Text = "Security";
            //
            // groupBox3
            //
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Location = new System.Drawing.Point(60, 48);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(344, 8);
            this.groupBox3.TabIndex = 10;
            this.groupBox3.TabStop = false;
            //
            // _markAsReadOnReply
            //
            this._markAsReadOnReply.Changed = false;
            this._markAsReadOnReply.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._markAsReadOnReply.InvertValue = false;
            this._markAsReadOnReply.Location = new System.Drawing.Point(0, 68);
            this._markAsReadOnReply.Name = "_markAsReadOnReply";
            this._markAsReadOnReply.Size = new System.Drawing.Size(300, 24);
            this._markAsReadOnReply.TabIndex = 4;
            this._markAsReadOnReply.Text = "&Mark messages as read on reply and forward";
            //
            // _setCategoryFromContactWhenEmailArrived
            //
            this._setCategoryFromContactWhenEmailArrived.Changed = false;
            this._setCategoryFromContactWhenEmailArrived.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._setCategoryFromContactWhenEmailArrived.InvertValue = false;
            this._setCategoryFromContactWhenEmailArrived.Location = new System.Drawing.Point(0, 88);
            this._setCategoryFromContactWhenEmailArrived.Name = "_setCategoryFromContactWhenEmailArrived";
            this._setCategoryFromContactWhenEmailArrived.Size = new System.Drawing.Size(300, 24);
            this._setCategoryFromContactWhenEmailArrived.TabIndex = 4;
            this._setCategoryFromContactWhenEmailArrived.Text = "Assign sender's categories to received mail";
            //
            // _setCategoryFromContactWhenEmailSent
            //
            this._setCategoryFromContactWhenEmailSent.Changed = false;
            this._setCategoryFromContactWhenEmailSent.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._setCategoryFromContactWhenEmailSent.InvertValue = false;
            this._setCategoryFromContactWhenEmailSent.Location = new System.Drawing.Point(0, 108);
            this._setCategoryFromContactWhenEmailSent.Name = "_setCategoryFromContactWhenEmailArrived";
            this._setCategoryFromContactWhenEmailSent.Size = new System.Drawing.Size(300, 24);
            this._setCategoryFromContactWhenEmailSent.TabIndex = 4;
            this._setCategoryFromContactWhenEmailSent.Text = "Assign recipients' categories to sent mail";
            //
            // panel2
            //
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.groupBox3);
            this.panel2.Controls.Add(this._markAsReadOnReply);
            this.panel2.Controls.Add(this._setCategoryFromContactWhenEmailArrived);
            this.panel2.Controls.Add(this._setCategoryFromContactWhenEmailSent);
            this.panel2.Controls.Add(this.groupBox4);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Controls.Add(this._showEmbedPics);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 172);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(404, 140);
            this.panel2.TabIndex = 11;
            //
            // label4
            //
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Location = new System.Drawing.Point(0, 48);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 16);
            this.label4.TabIndex = 0;
            this.label4.Text = "Other";

            #region Group Font Attributes
			//
			// _grpFontChars
			//
			this._grpFontChars.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._grpFontChars.Controls.Add(_chkOverrideFont);
			this._grpFontChars.Controls.Add(_lblFontFamily);
			this._grpFontChars.Controls.Add(_txtFont);
			this._grpFontChars.Controls.Add(_btnChangeFont);
			this._grpFontChars.Controls.Add(_lblNote);
			this._grpFontChars.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._grpFontChars.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
			this._grpFontChars.Location = new System.Drawing.Point(0, 315);
			this._grpFontChars.Name = "_grpExpRules";
			this._grpFontChars.Size = new System.Drawing.Size(404, 97);
			this._grpFontChars.TabIndex = 0;
			this._grpFontChars.TabStop = false;
			this._grpFontChars.Text = "Mail Font Settings";
			//
			// _chkOverride
			//
			this._chkOverrideFont.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._chkOverrideFont.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._chkOverrideFont.Location = new System.Drawing.Point(8, 16);
			this._chkOverrideFont.Name = "_chkOverrideFont";
			this._chkOverrideFont.Size = new System.Drawing.Size(260, 20);
			this._chkOverrideFont.TabIndex = 1;
			this._chkOverrideFont.Text = "Override common settings";
            this._chkOverrideFont.CheckedChanged += new EventHandler(_chkOverrideFont_CheckedChanged);
			//
			// _lblFontFamily
			//
			this._lblFontFamily.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._lblFontFamily.Location = new System.Drawing.Point(16, 45);
			this._lblFontFamily.Name = "_lblFontFamily";
			this._lblFontFamily.Size = new System.Drawing.Size(40, 20);
			this._lblFontFamily.TabIndex = 2;
			this._lblFontFamily.Text = "F&ont:";
			this._lblFontFamily.UseMnemonic = true;
            //
            // _txtFont
            //
            this._txtFont.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this._txtFont.Location = new System.Drawing.Point(65, 41);
            this._txtFont.Name = "_txtFont";
            this._txtFont.Size = new System.Drawing.Size(130, 20);
            this._txtFont.TabIndex = 3;
            this._txtFont.ReadOnly = true;
            this._txtFont.Enabled = false;
			//
			// _btnChangeFont
			//
			this._btnChangeFont.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
			this._btnChangeFont.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnChangeFont.Location = new System.Drawing.Point(210, 40);
			this._btnChangeFont.Name = "_btnChangeFont";
			this._btnChangeFont.TabIndex = 4;
			this._btnChangeFont.Text = "&Change...";
			this._btnChangeFont.Enabled = false;
			this._btnChangeFont.Click += new EventHandler(_btnChangeFont_Click);
			//
			// _lblNote
			//
			this._lblNote.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._lblNote.Location = new System.Drawing.Point(16, 72);
			this._lblNote.Name = "_lblNote";
			this._lblNote.Size = new System.Drawing.Size(300, 20);
			this._lblNote.TabIndex = 2;
			this._lblNote.Text = "Take(s) effect only for plain text-formatted mails";
			this._lblNote.ForeColor = SystemColors.GrayText;
            #endregion Group Font Attributes

            #region Group Exp Rules
            //
            // groupExpRules
            //
            this._grpExpRules.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._grpExpRules.Controls.Add(this.labelDefaultExpRule);
            this._grpExpRules.Controls.Add(this.buttonEditExpRuleForDeleted);
            this._grpExpRules.Controls.Add(this.buttonClearDefault);
            this._grpExpRules.Controls.Add(this.labelExpRuleForDeleted);
            this._grpExpRules.Controls.Add(this.buttonEditDefExpRule);
            this._grpExpRules.Controls.Add(this.buttonClearDeleted);
            this._grpExpRules.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._grpExpRules.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._grpExpRules.Location = new System.Drawing.Point(0, 425);
            this._grpExpRules.Name = "_grpExpRules";
            this._grpExpRules.Size = new System.Drawing.Size(404, 80);
            this._grpExpRules.TabIndex = 13;
            this._grpExpRules.TabStop = false;
            this._grpExpRules.Text = "Autoexpiration Rules";
            //
            // labelDefaultExpRule
            //
            this.labelDefaultExpRule.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelDefaultExpRule.Location = new System.Drawing.Point(8, 23);
            this.labelDefaultExpRule.Name = "labelDefaultExpRule";
            this.labelDefaultExpRule.Size = new System.Drawing.Size(175, 16);
            this.labelDefaultExpRule.TabIndex = 0;
            this.labelDefaultExpRule.Text = "Default rule for all Outlook folders:";
            //
            // buttonEditExpRuleForDeleted
            //
            this.buttonEditExpRuleForDeleted.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonEditExpRuleForDeleted.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonEditExpRuleForDeleted.Location = new System.Drawing.Point(220, 48);
            this.buttonEditExpRuleForDeleted.Name = "buttonEditExpRuleForDeleted";
            this.buttonEditExpRuleForDeleted.TabIndex = 4;
            this.buttonEditExpRuleForDeleted.Text = "E&dit...";
            this.buttonEditExpRuleForDeleted.Click += new System.EventHandler(this.buttonEditExpRuleForDeleted_Click);
            //
            // buttonClearDefault
            //
            this.buttonClearDefault.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClearDefault.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonClearDefault.Location = new System.Drawing.Point(310, 20);
            this.buttonClearDefault.Name = "buttonClearDefault";
            this.buttonClearDefault.TabIndex = 2;
            this.buttonClearDefault.Text = "Clear";
            this.buttonClearDefault.Click += new System.EventHandler(this.buttonClearDefault_Click);
            //
            // labelExpRuleForDeleted
            //
            this.labelExpRuleForDeleted.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelExpRuleForDeleted.Location = new System.Drawing.Point(8, 51);
            this.labelExpRuleForDeleted.Name = "labelExpRuleForDeleted";
            this.labelExpRuleForDeleted.Size = new System.Drawing.Size(180, 16);
            this.labelExpRuleForDeleted.TabIndex = 3;
            this.labelExpRuleForDeleted.Text = "Rule for deleted Emails:";
            //
            // buttonEditDefExpRule
            //
            this.buttonEditDefExpRule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonEditDefExpRule.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonEditDefExpRule.Location = new System.Drawing.Point(220, 20);
            this.buttonEditDefExpRule.Name = "buttonEditDefExpRule";
            this.buttonEditDefExpRule.TabIndex = 1;
            this.buttonEditDefExpRule.Text = "&Edit...";
            this.buttonEditDefExpRule.Click += new System.EventHandler(this.buttonEditDefExpRule_Click);
            //
            // buttonClearDeleted
            //
            this.buttonClearDeleted.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClearDeleted.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonClearDeleted.Location = new System.Drawing.Point(310, 48);
            this.buttonClearDeleted.Name = "buttonClearDeleted";
            this.buttonClearDeleted.TabIndex = 5;
            this.buttonClearDeleted.Text = "Clear";
            this.buttonClearDeleted.Click += new System.EventHandler(this.buttonClearDeleted_Click);
            #endregion Group Exp Rules

            //
            // OutlookOptionsPane
            //
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(352, 184);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this._grpFontChars);
            this.Controls.Add(this._grpExpRules);
            this.Name = "OutlookOptionsPane";
            this.Size = new System.Drawing.Size(404, 344);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this._grpFontChars.ResumeLayout(false);
            this._grpExpRules.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        #endregion

        internal static AbstractOptionsPane OutlookOptionsPaneCreator()
        {
            return new OutlookOptionsPane();
        }
        public override void ShowPane()
        {
            panel2.Visible = !IsStartupPane;
            _syncMode.Visible = !IsStartupPane;
            _syncMode.SetData( new string[]{ MailSyncMode.All, MailSyncMode.Fresh, MailSyncMode.None },
                new string[]{ "All folders", "Only folders with new e-mail", "None" } );
            _syncMode.SetSetting( Settings.SyncMode );
            _showEmbedPics.SetSetting( Settings.ShowEmbedPics );
            _deliverOnStartup.SetSetting( Settings.DeliverOnStartup );
            _markAsReadOnReply.SetSetting( Settings.MarkAsReadOnReply );
            _setCategoryFromContactWhenEmailArrived.SetSetting( Settings.SetCategoryFromContactWhenEmailArrived );
            _setCategoryFromContactWhenEmailSent.SetSetting( Settings.SetCategoryFromContactWhenEmailSent );
            _scheduleDeliver.SetSetting( Settings.ScheduleDeliver );
            _sendReceiveTimeout.SetSetting( Settings.SendReceiveTimeout );
            _sendReceiveTimeout.Enabled = _scheduleDeliver.Checked;

            ReadFontCharacteristics();

            _grpFontChars.Visible = !IsStartupPane;
            _grpExpRules.Visible = !IsStartupPane;
            IResource resFolderType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", STR.MAPIFolder );
            buttonClearDefault.Enabled = resFolderType.HasProp( "ExpirationRuleLink" );
            IResource resItemType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", STR.Email );
            buttonClearDeleted.Enabled = resItemType.HasProp( "ExpirationRuleOnDeletedLink" );
        }

        public override void OK()
        {
            SettingSaver.Save( Controls );
            Settings.LoadSettings();
            if ( Settings.OutlookFolders != null )
            {
                Settings.OutlookFolders.UpdateNodeFilter( true );
            }
            WriteFontCharacteristics();
        }

        private void _scheduleDeliver_Click(object sender, System.EventArgs e)
        {
            _sendReceiveTimeout.Enabled = _scheduleDeliver.Checked;
        }

        public override string GetHelpKeyword()
        {
            return "/reference/outlook_general.html";
        }

        #region Expiration Rules
        private void buttonEditDefExpRule_Click(object sender, System.EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", STR.MAPIFolder );
            IResource linkedExpRule = resType.GetLinkProp( "ExpirationRuleLink" );

            Core.FilteringFormsManager.ShowExpirationRuleForm( resType, linkedExpRule, false );
            buttonClearDefault.Enabled = resType.HasProp( "ExpirationRuleLink" );
        }

        private void buttonClearDefault_Click(object sender, System.EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", STR.MAPIFolder );
            IResource rule = resType.GetLinkProp( "ExpirationRuleLink" );

            Core.ExpirationRuleManager.UnregisterRule( rule.GetStringProp( "Name" ) );
            buttonClearDefault.Enabled = resType.HasProp( "ExpirationRuleLink" );
        }

        private void buttonEditExpRuleForDeleted_Click(object sender, System.EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", STR.Email );
            IResource linkedExpRule = resType.GetLinkProp( "ExpirationRuleOnDeletedLink" );

            Core.FilteringFormsManager.ShowExpirationRuleForm( resType, linkedExpRule, true );
            buttonClearDeleted.Enabled = resType.HasProp( "ExpirationRuleOnDeletedLink" );
        }

        private void buttonClearDeleted_Click(object sender, System.EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", STR.Email );
            IResource rule = resType.GetLinkProp( "ExpirationRuleOnDeletedLink" );

            Core.ExpirationRuleManager.UnregisterRule( rule.GetStringProp( "Name" ) );
            buttonClearDeleted.Enabled = resType.HasProp( "ExpirationRuleOnDeletedLink" );
        }
        #endregion Expiration Rules

        #region Font Group
        private void _btnChangeFont_Click(object sender, EventArgs e)
        {
            FontDialog form = new FontDialog();
            form.ShowEffects = form.ShowColor = false;
            form.FontMustExist = true;
            form.MinSize = 6;
            form.Font = new Font( _currFont, _currFontSize );
            if( form.ShowDialog() == DialogResult.OK )
            {
                _currFont = form.Font.Name;
                _currFontSize = (int)(form.Font.Size + 0.5f);
                _txtFont.Text = _currFont + ", " + _currFontSize;
            }
        }

        private void _chkOverrideFont_CheckedChanged(object sender, EventArgs e)
        {
            _txtFont.Enabled = _btnChangeFont.Enabled = _chkOverrideFont.Checked;
        }
        #endregion Font Group
    }
}

