// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.Nntp
{
	internal class NntpOptionsPane : AbstractOptionsPane
	{
        private System.Windows.Forms.Label _downloadingLabel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private NumericUpDownSettingEditor _articlesNumber;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label4;
        private EncodingComboBox _encodingsBox;
        private System.Windows.Forms.Label _lblPosting;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox _deliverNewsBox;
        private System.Windows.Forms.Label label6;
        private NumericUpDownSettingEditor _deliverNewsPeriod;
        private CheckBoxSettingEditor _markFromMeAsRead;
        private RadioButtonSettingEditor _formatGroupBox;
        private System.Windows.Forms.Label label7;
        private ComboBoxSettingEditor _mimeEncodingBox;
        private CheckBoxSettingEditor _markAsReadOnReplyAndFormard;
        private CheckBoxSettingEditor _markAsReadOnThreadStop;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label _lblEditingPreview;
        private CheckBoxSettingEditor _displayAttachmentsInline;
        private CheckBoxSettingEditor _deliverOnStartBox;
        private CheckBoxSettingEditor _closeOnReply;
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.GroupBox groupExpRules;
        private System.Windows.Forms.Label labelDefaultExpRule;
        private System.Windows.Forms.Label labelExpRuleForDeleted;
        private System.Windows.Forms.Button _buttonDefExpRule;
        private System.Windows.Forms.Button _buttonClearDefExpRule;
        private System.Windows.Forms.Button _buttonExpRuleForDeleted;
        private System.Windows.Forms.Button _buttonClearExpRuleForDeleted;
        private CheckBoxSettingEditor _downloadHeadersOnlyCheckBox;

        private System.Windows.Forms.GroupBox _grpFontChars;
        private System.Windows.Forms.CheckBox _chkOverrideFont;
        private System.Windows.Forms.Label _lblFontFamily;
        private System.Windows.Forms.Button _btnChangeFont;
        private System.Windows.Forms.TextBox _txtFont;

        private string          _currFont;
        private int             _currFontSize;

		private NntpOptionsPane()
		{
			InitializeComponent();
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "NewsGroup" );
            _buttonClearDefExpRule.Enabled = resType.HasProp( "ExpirationRuleLink" );
            _buttonClearExpRuleForDeleted.Enabled = resType.HasProp( "ExpirationRuleOnDeletedLink" );
		}

        private void  ReadFontCharacteristics()
        {
            _currFont = Core.UIManager.DefaultFontFace;
            _currFontSize = (int)Core.UIManager.DefaultFontSize;

            _chkOverrideFont.Checked = Core.SettingStore.ReadBool( "NNTP", "NewsArticleFontOverride", false );
            if( _chkOverrideFont.Checked )
            {
                _currFont = Core.SettingStore.ReadString( "NNTP", "NewsArticleFont", Core.UIManager.DefaultFontFace );
                _currFontSize = Core.SettingStore.ReadInt( "NNTP", "NewsArticleFontSize", (int)Core.UIManager.DefaultFontSize );
            }
            _txtFont.Text = _currFont + ", " + _currFontSize;
        }
        private void  WriteFontCharacteristics()
        {
            Core.SettingStore.WriteBool( "NNTP", "NewsArticleFontOverride", _chkOverrideFont.Checked );
            if( _chkOverrideFont.Checked )
            {
                Core.SettingStore.WriteString( "NNTP", "NewsArticleFont", _currFont );
                Core.SettingStore.WriteInt( "NNTP", "NewsArticleFontSize", _currFontSize );
            }
        }

        internal static AbstractOptionsPane NntpOptionsPaneCreator()
        {
            return new NntpOptionsPane();
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
		private void InitializeComponent()
		{
			this._downloadingLabel = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this._articlesNumber = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label4 = new System.Windows.Forms.Label();
			this._encodingsBox = new JetBrains.Omea.Nntp.EncodingComboBox();
			this._lblPosting = new System.Windows.Forms.Label();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this._deliverNewsBox = new System.Windows.Forms.CheckBox();
			this._deliverNewsPeriod = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
			this.label6 = new System.Windows.Forms.Label();
			this._markFromMeAsRead = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
			this._formatGroupBox = new JetBrains.Omea.GUIControls.RadioButtonSettingEditor();
			this._mimeEncodingBox = new JetBrains.Omea.GUIControls.ComboBoxSettingEditor();
			this.label7 = new System.Windows.Forms.Label();
			this._markAsReadOnReplyAndFormard = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._markAsReadOnThreadStop = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this._lblEditingPreview = new System.Windows.Forms.Label();
			this._displayAttachmentsInline = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
			this._deliverOnStartBox = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
			this._closeOnReply = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
			this._downloadHeadersOnlyCheckBox = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
			this.groupExpRules = new System.Windows.Forms.GroupBox();
			this.labelDefaultExpRule = new System.Windows.Forms.Label();
			this._buttonDefExpRule = new System.Windows.Forms.Button();
			this._buttonClearDefExpRule = new System.Windows.Forms.Button();
			this.labelExpRuleForDeleted = new System.Windows.Forms.Label();
			this._buttonExpRuleForDeleted = new System.Windows.Forms.Button();
			this._buttonClearExpRuleForDeleted = new System.Windows.Forms.Button();

            this._grpFontChars = new System.Windows.Forms.GroupBox();
            this._chkOverrideFont = new System.Windows.Forms.CheckBox();
			this._lblFontFamily = new System.Windows.Forms.Label();
            this._txtFont = new System.Windows.Forms.TextBox();
            this._btnChangeFont = new System.Windows.Forms.Button();

            this._grpFontChars.SuspendLayout();
			this._formatGroupBox.SuspendLayout();
			this.groupExpRules.SuspendLayout();
			this.SuspendLayout();

            #region Downloading
            //
			// _downloadingLabel
			//
			this._downloadingLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._downloadingLabel.Location = new System.Drawing.Point(0, 4);
			this._downloadingLabel.Name = "_downloadingLabel";
			this._downloadingLabel.Size = new System.Drawing.Size(92, 16);
			this._downloadingLabel.TabIndex = 0;
			this._downloadingLabel.Text = "Downloading";
			this._downloadingLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			//
			// groupBox1
			//
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Location = new System.Drawing.Point(96, 4);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(364, 8);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			//
			// label1
			//
			this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label1.Location = new System.Drawing.Point(8, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(152, 16);
			this.label1.TabIndex = 2;
			this.label1.Text = "&Download not more than";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _articlesNumber
			//
			this._articlesNumber.Changed = false;
			this._articlesNumber.Location = new System.Drawing.Point(188, 20);
			this._articlesNumber.Maximum = 9999;
			this._articlesNumber.Minimum = 1;
			this._articlesNumber.Name = "_articlesNumber";
			this._articlesNumber.Size = new System.Drawing.Size(48, 20);
			this._articlesNumber.TabIndex = 3;
			//
			// label2
			//
			this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label2.Location = new System.Drawing.Point(244, 24);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(168, 20);
			this.label2.TabIndex = 4;
			this.label2.Text = "articles from a group at a time";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _deliverNewsBox
			//
			this._deliverNewsBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._deliverNewsBox.Location = new System.Drawing.Point(8, 64);
			this._deliverNewsBox.Name = "_deliverNewsBox";
			this._deliverNewsBox.Size = new System.Drawing.Size(172, 22);
			this._deliverNewsBox.TabIndex = 6;
			this._deliverNewsBox.Text = "Deliver News e&very";
			this._deliverNewsBox.CheckedChanged += new System.EventHandler(this._deliverNewsBox_CheckedChanged);
            //
			// _deliverOnStartBox
			//
			this._deliverOnStartBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._deliverOnStartBox.Changed = false;
			this._deliverOnStartBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._deliverOnStartBox.InvertValue = false;
			this._deliverOnStartBox.Location = new System.Drawing.Point(8, 42);
			this._deliverOnStartBox.Name = "_deliverOnStartBox";
			this._deliverOnStartBox.Size = new System.Drawing.Size(452, 22);
			this._deliverOnStartBox.TabIndex = 5;
			this._deliverOnStartBox.Text = "Deliver News on &startup";
			//
			// _downloadHeadersOnlyCheckBox
			//
			this._downloadHeadersOnlyCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._downloadHeadersOnlyCheckBox.Changed = false;
			this._downloadHeadersOnlyCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._downloadHeadersOnlyCheckBox.InvertValue = false;
			this._downloadHeadersOnlyCheckBox.Location = new System.Drawing.Point(8, 108);
			this._downloadHeadersOnlyCheckBox.Name = "_downloadHeadersOnlyCheckBox";
			this._downloadHeadersOnlyCheckBox.Size = new System.Drawing.Size(452, 24);
			this._downloadHeadersOnlyCheckBox.TabIndex = 10;
			this._downloadHeadersOnlyCheckBox.Text = "On Deliver News, download only article &headers";
			//
			// _deliverNewsPeriod
			//
			this._deliverNewsPeriod.Changed = false;
			this._deliverNewsPeriod.Location = new System.Drawing.Point(188, 64);
			this._deliverNewsPeriod.Maximum = 9999;
			this._deliverNewsPeriod.Minimum = 0;
			this._deliverNewsPeriod.Name = "_deliverNewsPeriod";
			this._deliverNewsPeriod.Size = new System.Drawing.Size(48, 20);
			this._deliverNewsPeriod.TabIndex = 7;
			//
			// label6
			//
			this.label6.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label6.Location = new System.Drawing.Point(244, 68);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(72, 16);
			this.label6.TabIndex = 8;
			this.label6.Text = "minutes";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _markFromMeAsRead
			//
			this._markFromMeAsRead.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._markFromMeAsRead.Changed = false;
			this._markFromMeAsRead.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._markFromMeAsRead.InvertValue = false;
			this._markFromMeAsRead.Location = new System.Drawing.Point(8, 86);
			this._markFromMeAsRead.Name = "_markFromMeAsRead";
			this._markFromMeAsRead.Size = new System.Drawing.Size(452, 22);
			this._markFromMeAsRead.TabIndex = 9;
			this._markFromMeAsRead.Text = "Mark articles from me as &read";
            #endregion Downloading

            #region Editing and Preview
            //
			// _lblEditingPreview
			//
			this._lblEditingPreview.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._lblEditingPreview.Location = new System.Drawing.Point(0, 144);
			this._lblEditingPreview.Name = "_lblEditingPreview";
			this._lblEditingPreview.Size = new System.Drawing.Size(120, 16);
			this._lblEditingPreview.TabIndex = 11;
			this._lblEditingPreview.Text = "Editing and Preview";
			this._lblEditingPreview.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			//
			// groupBox4
			//
			this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox4.Location = new System.Drawing.Point(120, 144);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(340, 8);
			this.groupBox4.TabIndex = 12;
			this.groupBox4.TabStop = false;
			//
			// _displayAttachmentsInline
			//
			this._displayAttachmentsInline.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._displayAttachmentsInline.Changed = false;
			this._displayAttachmentsInline.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._displayAttachmentsInline.InvertValue = false;
			this._displayAttachmentsInline.Location = new System.Drawing.Point(8, 160);
			this._displayAttachmentsInline.Name = "_displayAttachmentsInline";
			this._displayAttachmentsInline.Size = new System.Drawing.Size(452, 22);
			this._displayAttachmentsInline.TabIndex = 13;
			this._displayAttachmentsInline.Text = "Display attachments &inline";
			//
			// _closeOnReply
			//
			this._closeOnReply.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._closeOnReply.Changed = false;
			this._closeOnReply.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._closeOnReply.InvertValue = false;
			this._closeOnReply.Location = new System.Drawing.Point(8, 182);
			this._closeOnReply.Name = "_closeOnReply";
			this._closeOnReply.Size = new System.Drawing.Size(452, 22);
			this._closeOnReply.TabIndex = 14;
			this._closeOnReply.Text = "&Close original message on Reply and Forward";
			//
			// _markAsReadOnReplyAndFormard
			//
			this._markAsReadOnReplyAndFormard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._markAsReadOnReplyAndFormard.Changed = false;
			this._markAsReadOnReplyAndFormard.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._markAsReadOnReplyAndFormard.InvertValue = false;
			this._markAsReadOnReplyAndFormard.Location = new System.Drawing.Point(8, 204);
			this._markAsReadOnReplyAndFormard.Name = "_markAsReadOnReplyAndFormard";
			this._markAsReadOnReplyAndFormard.Size = new System.Drawing.Size(452, 22);
			this._markAsReadOnReplyAndFormard.TabIndex = 15;
			this._markAsReadOnReplyAndFormard.Text = "Mar&k as read on Reply and Forward";

			//
			// _markAsReadOnReplyAndFormard
			//
			this._markAsReadOnThreadStop.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._markAsReadOnThreadStop.Changed = false;
			this._markAsReadOnThreadStop.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._markAsReadOnThreadStop.InvertValue = false;
			this._markAsReadOnThreadStop.Location = new System.Drawing.Point(8, 226);
			this._markAsReadOnThreadStop.Name = "_markAsReadOnThreadStop";
			this._markAsReadOnThreadStop.Size = new System.Drawing.Size(452, 22);
			this._markAsReadOnThreadStop.TabIndex = 16;
			this._markAsReadOnThreadStop.Text = "Mark as read on Stopping the &Thread";

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
			this._grpFontChars.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._grpFontChars.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
			this._grpFontChars.Location = new System.Drawing.Point(8, 252);
			this._grpFontChars.Name = "groupExpRules";
			this._grpFontChars.Size = new System.Drawing.Size(456, 72);
			this._grpFontChars.TabIndex = 0;
			this._grpFontChars.TabStop = false;
			this._grpFontChars.Text = "News Article Font Settings";
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
            #endregion Group Font Attributes
            #endregion Editing and Preview

            #region Intl Settings
			//
			// label3
			//
			this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label3.Location = new System.Drawing.Point(0, 336);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(128, 20);
			this.label3.TabIndex = 17;
			this.label3.Text = "International Settings";
			this.label3.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			//
			// groupBox2
			//
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Location = new System.Drawing.Point(128, 336);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(332, 8);
			this.groupBox2.TabIndex = 18;
			this.groupBox2.TabStop = false;
			//
			// label4
			//
			this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label4.Location = new System.Drawing.Point(8, 360);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(124, 16);
			this.label4.TabIndex = 19;
			this.label4.Text = "Default &encoding:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _encodingsBox
			//
			this._encodingsBox.Changed = false;
			this._encodingsBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._encodingsBox.Location = new System.Drawing.Point(128, 356);
			this._encodingsBox.Name = "_encodingsBox";
			this._encodingsBox.Size = new System.Drawing.Size(144, 21);
			this._encodingsBox.TabIndex = 20;
            #endregion Intl Settings

            #region Posting
			//
			// _lblPosting
			//
			this._lblPosting.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._lblPosting.Location = new System.Drawing.Point(0, 392);
			this._lblPosting.Name = "_lblPosting";
			this._lblPosting.Size = new System.Drawing.Size(64, 16);
			this._lblPosting.TabIndex = 21;
			this._lblPosting.Text = "Posting";
			this._lblPosting.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			//
			// groupBox3
			//
			this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox3.Location = new System.Drawing.Point(68, 392);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(392, 8);
			this.groupBox3.TabIndex = 22;
			this.groupBox3.TabStop = false;
			//
			// _formatGroupBox
			//
			this._formatGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._formatGroupBox.Changed = false;
			this._formatGroupBox.Controls.Add(this._mimeEncodingBox);
			this._formatGroupBox.Controls.Add(this.label7);
			this._formatGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._formatGroupBox.Location = new System.Drawing.Point(8, 412);
			this._formatGroupBox.Name = "_formatGroupBox";
			this._formatGroupBox.Size = new System.Drawing.Size(456, 72);
			this._formatGroupBox.TabIndex = 23;
			this._formatGroupBox.TabStop = false;
			this._formatGroupBox.Text = "Message format";
			//
			// _mimeEncodingBox
			//
			this._mimeEncodingBox.Changed = false;
			this._mimeEncodingBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._mimeEncodingBox.Location = new System.Drawing.Point(252, 44);
			this._mimeEncodingBox.Name = "_mimeEncodingBox";
			this._mimeEncodingBox.Size = new System.Drawing.Size(144, 21);
			this._mimeEncodingBox.TabIndex = 3;
			//
			// label7
			//
			this.label7.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label7.Location = new System.Drawing.Point(120, 48);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(128, 16);
			this.label7.TabIndex = 2;
			this.label7.Text = "Encode te&xt with:";
			this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			#endregion Posting

            #region Group Exp Rules
			//
			// groupExpRules
			//
			this.groupExpRules.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupExpRules.Controls.Add(this.labelDefaultExpRule);
			this.groupExpRules.Controls.Add(this._buttonDefExpRule);
			this.groupExpRules.Controls.Add(this._buttonClearDefExpRule);
			this.groupExpRules.Controls.Add(this.labelExpRuleForDeleted);
			this.groupExpRules.Controls.Add(this._buttonExpRuleForDeleted);
			this.groupExpRules.Controls.Add(this._buttonClearExpRuleForDeleted);
			this.groupExpRules.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.groupExpRules.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
			this.groupExpRules.Location = new System.Drawing.Point(0, 498);
			this.groupExpRules.Name = "groupExpRules";
			this.groupExpRules.Size = new System.Drawing.Size(464, 80);
			this.groupExpRules.TabIndex = 24;
			this.groupExpRules.TabStop = false;
			this.groupExpRules.Text = "Autoexpiration Rules";
			//
			// labelDefaultExpRule
			//
			this.labelDefaultExpRule.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.labelDefaultExpRule.Location = new System.Drawing.Point(8, 23);
			this.labelDefaultExpRule.Name = "labelDefaultExpRule";
			this.labelDefaultExpRule.Size = new System.Drawing.Size(160, 16);
			this.labelDefaultExpRule.TabIndex = 0;
			this.labelDefaultExpRule.Text = "Default rule for all Newsgroups:";
			//
			// _buttonDefExpRule
			//
			this._buttonDefExpRule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonDefExpRule.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._buttonDefExpRule.Location = new System.Drawing.Point(300, 20);
			this._buttonDefExpRule.Name = "_buttonDefExpRule";
			this._buttonDefExpRule.TabIndex = 1;
			this._buttonDefExpRule.Text = "Edit...";
			this._buttonDefExpRule.Click += new System.EventHandler(this.button1_Click);
			//
			// _buttonClearDefExpRule
			//
			this._buttonClearDefExpRule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonClearDefExpRule.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._buttonClearDefExpRule.Location = new System.Drawing.Point(380, 20);
			this._buttonClearDefExpRule.Name = "_buttonClearDefExpRule";
			this._buttonClearDefExpRule.TabIndex = 2;
			this._buttonClearDefExpRule.Text = "Clear";
			this._buttonClearDefExpRule.Click += new System.EventHandler(this.buttonClearDefExpRule_Click);
			//
			// labelExpRuleForDeleted
			//
			this.labelExpRuleForDeleted.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.labelExpRuleForDeleted.Location = new System.Drawing.Point(8, 51);
			this.labelExpRuleForDeleted.Name = "labelExpRuleForDeleted";
			this.labelExpRuleForDeleted.Size = new System.Drawing.Size(160, 16);
			this.labelExpRuleForDeleted.TabIndex = 3;
			this.labelExpRuleForDeleted.Text = "Rule for deleted News Articles:";
			//
			// _buttonExpRuleForDeleted
			//
			this._buttonExpRuleForDeleted.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonExpRuleForDeleted.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._buttonExpRuleForDeleted.Location = new System.Drawing.Point(300, 48);
			this._buttonExpRuleForDeleted.Name = "_buttonExpRuleForDeleted";
			this._buttonExpRuleForDeleted.TabIndex = 4;
			this._buttonExpRuleForDeleted.Text = "Edit...";
			this._buttonExpRuleForDeleted.Click += new System.EventHandler(this._buttonExpRuleForDeleted_Click);
			//
			// _buttonClearExpRuleForDeleted
			//
			this._buttonClearExpRuleForDeleted.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonClearExpRuleForDeleted.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._buttonClearExpRuleForDeleted.Location = new System.Drawing.Point(380, 48);
			this._buttonClearExpRuleForDeleted.Name = "_buttonClearExpRuleForDeleted";
			this._buttonClearExpRuleForDeleted.TabIndex = 5;
			this._buttonClearExpRuleForDeleted.Text = "Clear";
			this._buttonClearExpRuleForDeleted.Click += new System.EventHandler(this._buttonClearExpRuleForDeleted_Click);
            #endregion Group Exp Rules

			//
			// NntpOptionsPane
			//
			this.Controls.Add(this.groupExpRules);
			this.Controls.Add(this._downloadHeadersOnlyCheckBox);
			this.Controls.Add(this._closeOnReply);
			this.Controls.Add(this._deliverOnStartBox);
			this.Controls.Add(this._displayAttachmentsInline);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this._lblEditingPreview);
			this.Controls.Add(this._grpFontChars);
			this.Controls.Add(this._markAsReadOnReplyAndFormard);
			this.Controls.Add(this._markAsReadOnThreadStop);
			this.Controls.Add(this._formatGroupBox);
			this.Controls.Add(this._markFromMeAsRead);
			this.Controls.Add(this.label6);
			this.Controls.Add(this._deliverNewsPeriod);
			this.Controls.Add(this._deliverNewsBox);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this._lblPosting);
			this.Controls.Add(this._encodingsBox);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this._articlesNumber);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this._downloadingLabel);
			this.Name = "NntpOptionsPane";
			this.Size = new System.Drawing.Size(464, 504);
            this._grpFontChars.ResumeLayout(false);
			this._formatGroupBox.ResumeLayout(false);
			this.groupExpRules.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

        public override void ShowPane()
        {
            /**
             * setting "articles per group" value
             */
            _articlesNumber.SetSetting( Settings.ArticlesPerGroup );
            int articlesPerGroup = _articlesNumber.Value;
            if( articlesPerGroup <= 0 )
            {
                articlesPerGroup = 300;
            }
            _articlesNumber.Value = articlesPerGroup;

            /**
             * setting Deliver News on startup
             */
            _deliverOnStartBox.SetSetting( Settings.DeliverOnStartup );

            /**
             * setting deliver news period
             */
            _deliverNewsPeriod.SetSetting( Settings.DeliverNewsPeriod );
            int deliverNewsPeriod = _deliverNewsPeriod.Value;
            if( deliverNewsPeriod < 0 )
            {
                deliverNewsPeriod = 0;
            }
            _deliverNewsPeriod.Value = deliverNewsPeriod;
            _deliverNewsPeriod.Enabled = _deliverNewsBox.Checked = ( deliverNewsPeriod > 0 );

            /**
             * setting mark "from me" option
             */
            _markFromMeAsRead.SetSetting( Settings.MarkFromMeAsRead );

            /**
             * setting download article headers only on Deliver News
             */
            _downloadHeadersOnlyCheckBox.InvertValue = true;
            _downloadHeadersOnlyCheckBox.SetSetting( Settings.DownloadBodiesOnDeliver );

            /**
             * preview and editing setting
             */
            _displayAttachmentsInline.SetSetting( Settings.DisplayAttachmentsInline );
            _closeOnReply.SetSetting( Settings.CloseOnReply );
            _markAsReadOnReplyAndFormard.SetSetting( Settings.MarkAsReadOnReplyAndFormard );
            _markAsReadOnThreadStop.SetSetting( Settings.MarkAsReadOnReplyThreadStop );

            ReadFontCharacteristics();

            _encodingsBox.Sorted = true;
            _encodingsBox.Init( Settings.Charset );

            _formatGroupBox.CheckedChanged += _groupBox_CheckedChanged;
            _formatGroupBox.SetData( new string[]{ "UUEncode", "MIME" }, new string[]{ "&UUEncode", "&MIME" } );
            _formatGroupBox.SetSetting( Settings.Format );

            SetMimeEncodingBox();
        }

	    private void SetMimeEncodingBox()
	    {
	        string[] values = new string[]{ "None", "Quoted-Printable", "Base64" };
	        _mimeEncodingBox.SetData( values, values );
	        _mimeEncodingBox.SetSetting( Settings.EncodeTextWith );
	        if ( _mimeEncodingBox.SelectedIndex == -1 )
	        {
	            _mimeEncodingBox.SetValue( "None" );
	        }
	    }

	    public override void OK()
        {
            if ( !_deliverNewsBox.Checked )
            {
                _deliverNewsPeriod.Value = 0;
                _deliverNewsPeriod.Changed = true;
            }
            SettingSaver.Save( Controls );
            Settings.LoadSettings();
            WriteFontCharacteristics();
        }

        public override void Cancel()
        {
        }

        private void _deliverNewsBox_CheckedChanged(object sender, EventArgs e)
        {
            if( _deliverNewsPeriod.Enabled == _deliverNewsBox.Checked )
            {
                _deliverNewsPeriod.Focus();
            }
        }

        private void _groupBox_CheckedChanged(object sender, EventArgs e)
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

	    public override string GetHelpKeyword()
	    {
	        return "/reference/news.html";
	    }

        #region Expiration Rules
        private void button1_Click(object sender, EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "NewsGroup" );
            IResource linkedExpRule = resType.GetLinkProp("ExpirationRuleLink");

            Core.FilteringFormsManager.ShowExpirationRuleForm( resType, linkedExpRule, false );
            _buttonClearDefExpRule.Enabled = resType.HasProp( "ExpirationRuleLink" );
        }

        private void buttonClearDefExpRule_Click(object sender, EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "NewsGroup" );
            IResource rule = resType.GetLinkProp( "ExpirationRuleLink" );
            Core.ExpirationRuleManager.UnregisterRule( rule.GetStringProp( "Name" ) );
            _buttonClearDefExpRule.Enabled = resType.HasProp( "ExpirationRuleLink" );
        }

        private void _buttonExpRuleForDeleted_Click(object sender, EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "Article" );
            IResource linkedExpRule = resType.GetLinkProp( "ExpirationRuleOnDeletedLink" );

            Core.FilteringFormsManager.ShowExpirationRuleForm( resType, linkedExpRule, true );
            _buttonClearExpRuleForDeleted.Enabled = resType.HasProp( "ExpirationRuleOnDeletedLink" );
        }

        private void _buttonClearExpRuleForDeleted_Click(object sender, EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "Article" );
            IResource rule = resType.GetLinkProp( "ExpirationRuleOnDeletedLink" );

            Core.ExpirationRuleManager.UnregisterRule( rule.GetStringProp( "Name" ) );
            _buttonClearExpRuleForDeleted.Enabled = resType.HasProp( "ExpirationRuleOnDeletedLink" );
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
