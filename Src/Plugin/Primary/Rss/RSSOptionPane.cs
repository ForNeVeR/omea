// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
    /// <summary>
    /// Options pane for the RSS plugin.
    /// </summary>
    public class RSSOptionPane: AbstractOptionsPane
    {
        private GroupBox _grpUpdateSettings;
        private Label label1;
        private PeriodComboBox _cmbUpdatePeriod;
        private NumericUpDownSettingEditor _udUpdateFrequency;
        private Label label2;
        private PeriodComboBox _cmbStopUpdatePeriod;
        private NumericUpDownSettingEditor _udStopUpdatePeriod;
        private string _oldUpdatePeriod;

        private CookieProviderSelector _cookieProviderSelector;

        private CheckBoxSettingEditor         _chkRememberSelection;
        private CheckBox _chkShowDesktopAlert;
		private CheckBox _checkNewspaperAllowHoverSelection;
		private CheckBox _checkPropagateFavIconToItems;
        private CheckBox _checkUseDetailedURLs;
        private CheckBox _checkShowSummary;
        private bool _wasDesktopAlertChecked;

        private GroupBox _grpFontChars;
        private CheckBox _chkOverrideFont;
        private Label _lblFontFamily;
        private Button _btnChangeFont;
        private TextBox _txtFont;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components = null;

        private NumericUpDownSettingEditor _updateTimeOut;
        private Label label3;
        private GroupBox groupExpRules;
        private Button buttonEditDefExpRule;
        private Label labelDefaultExpRule;
        private Label labelExpRuleForDeleted;
        private Button buttonEditExpRuleForDeleted;
        private Button buttonClearDefault;
        private Button buttonClearDeleted;
        private int _oldUpdateFrequency;

        private string          _currFont;
        private int             _currFontSize;

        private delegate void UpdateDefaultsDelegate( int oldFreq, string oldPeriod );

        public RSSOptionPane()
        {
            InitializeComponent();
        }

        private void  ReadFontCharacteristics()
        {
            _currFont = Core.UIManager.DefaultFontFace;
            _currFontSize = (int)Core.UIManager.DefaultFontSize;

            _chkOverrideFont.Checked = Core.SettingStore.ReadBool( IniKeys.Section, "RSSPostFontOverride", false );
            if( _chkOverrideFont.Checked )
            {
                _currFont = Core.SettingStore.ReadString( IniKeys.Section, "RSSPostFont", Core.UIManager.DefaultFontFace );
                _currFontSize = Core.SettingStore.ReadInt( IniKeys.Section, "RSSPostFontSize", (int)Core.UIManager.DefaultFontSize );
            }
            _txtFont.Text = _currFont + ", " + _currFontSize;
        }
        private void  WriteFontCharacteristics()
        {
            Core.SettingStore.WriteBool( IniKeys.Section, "RSSPostFontOverride", _chkOverrideFont.Checked );
            if( _chkOverrideFont.Checked )
            {
                Core.SettingStore.WriteString( IniKeys.Section, "RSSPostFont", _currFont );
                Core.SettingStore.WriteInt( IniKeys.Section, "RSSPostFontSize", _currFontSize );
            }
        }

        private void  ReadItemFormattingOptions()
        {
            _checkUseDetailedURLs.Checked = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.UseDetailedURLs, false );
            _checkShowSummary.Checked = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.ShowSummary, false );
        }
        private void  WriteItemFormattingOptions()
        {
            Core.SettingStore.WriteBool( IniKeys.Section, IniKeys.UseDetailedURLs, _checkUseDetailedURLs.Checked );
            Core.SettingStore.WriteBool( IniKeys.Section, IniKeys.ShowSummary, _checkShowSummary.Checked );
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
            this._grpUpdateSettings = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this._cmbUpdatePeriod = new JetBrains.Omea.RSSPlugin.PeriodComboBox();
			this._udUpdateFrequency = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
			this.label2 = new System.Windows.Forms.Label();
			this._cmbStopUpdatePeriod = new JetBrains.Omea.RSSPlugin.PeriodComboBox();
			this._udStopUpdatePeriod = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
			this._updateTimeOut = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
			this.label3 = new System.Windows.Forms.Label();

			this._chkShowDesktopAlert = new System.Windows.Forms.CheckBox();
			this._cookieProviderSelector = new JetBrains.Omea.GUIControls.CookieProviderSelector();
			this._checkNewspaperAllowHoverSelection = new System.Windows.Forms.CheckBox();
			this._chkRememberSelection = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
			this._checkPropagateFavIconToItems = new System.Windows.Forms.CheckBox();
            this._checkUseDetailedURLs = new System.Windows.Forms.CheckBox();
            this._checkShowSummary = new System.Windows.Forms.CheckBox();

            this._grpFontChars = new System.Windows.Forms.GroupBox();
            this._chkOverrideFont = new System.Windows.Forms.CheckBox();
			this._lblFontFamily = new System.Windows.Forms.Label();
            this._txtFont = new System.Windows.Forms.TextBox();
            this._btnChangeFont = new System.Windows.Forms.Button();

			this.groupExpRules = new System.Windows.Forms.GroupBox();
			this.labelDefaultExpRule = new System.Windows.Forms.Label();
			this.buttonEditExpRuleForDeleted = new System.Windows.Forms.Button();
			this.buttonClearDefault = new System.Windows.Forms.Button();
			this.labelExpRuleForDeleted = new System.Windows.Forms.Label();
			this.buttonEditDefExpRule = new System.Windows.Forms.Button();
			this.buttonClearDeleted = new System.Windows.Forms.Button();
            this._grpUpdateSettings.SuspendLayout();
			this.groupExpRules.SuspendLayout();
			this.SuspendLayout();

            #region UpdateSettings
			//
			// _grpUpdateSettings
			//
			this._grpUpdateSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._grpUpdateSettings.Controls.Add(label1);
			this._grpUpdateSettings.Controls.Add(_cmbUpdatePeriod);
			this._grpUpdateSettings.Controls.Add(_udUpdateFrequency);
			this._grpUpdateSettings.Controls.Add(label2);
			this._grpUpdateSettings.Controls.Add(_cmbStopUpdatePeriod);
			this._grpUpdateSettings.Controls.Add(_udStopUpdatePeriod);
			this._grpUpdateSettings.Controls.Add(_updateTimeOut);
			this._grpUpdateSettings.Controls.Add(label3);
			this._grpUpdateSettings.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._grpUpdateSettings.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
			this._grpUpdateSettings.Location = new System.Drawing.Point(0, 4);
			this._grpUpdateSettings.Name = "groupExpRules";
			this._grpUpdateSettings.Size = new System.Drawing.Size(464, 100);
			this._grpUpdateSettings.TabIndex = 0;
			this._grpUpdateSettings.TabStop = false;
			this._grpUpdateSettings.Text = "Feed Update Settings";
			//
			// label1
			//
			this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label1.Location = new System.Drawing.Point(8, 18);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(180, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "Default feeds &update interval:";
			//
			// _cmbUpdatePeriod
			//
			this._cmbUpdatePeriod.Changed = false;
			this._cmbUpdatePeriod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._cmbUpdatePeriod.Location = new System.Drawing.Point(288, 14);
			this._cmbUpdatePeriod.Name = "_cmbUpdatePeriod";
			this._cmbUpdatePeriod.Size = new System.Drawing.Size(76, 21);
			this._cmbUpdatePeriod.TabIndex = 2;
			//
			// _udUpdateFrequency
			//
			this._udUpdateFrequency.Changed = true;
			this._udUpdateFrequency.Location = new System.Drawing.Point(244, 14);
			this._udUpdateFrequency.Maximum = 1000;
			this._udUpdateFrequency.Minimum = 1;
			this._udUpdateFrequency.Name = "_udUpdateFrequency";
			this._udUpdateFrequency.Size = new System.Drawing.Size(40, 20);
			this._udUpdateFrequency.TabIndex = 1;
			this._udUpdateFrequency.Text = "1";
			this._udUpdateFrequency.Value = 1;
			//
			// label2
			//
			this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label2.Location = new System.Drawing.Point(8, 38);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(216, 32);
			this.label2.TabIndex = 3;
			this.label2.Text = "Stop updating feeds co&mments if no comments have been received for:";
			//
			// _cmbStopUpdatePeriod
			//
			this._cmbStopUpdatePeriod.Changed = false;
			this._cmbStopUpdatePeriod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._cmbStopUpdatePeriod.Location = new System.Drawing.Point(288, 42);
			this._cmbStopUpdatePeriod.Name = "_cmbStopUpdatePeriod";
			this._cmbStopUpdatePeriod.Size = new System.Drawing.Size(76, 21);
			this._cmbStopUpdatePeriod.TabIndex = 5;
			//
			// _udStopUpdatePeriod
			//
			this._udStopUpdatePeriod.Changed = false;
			this._udStopUpdatePeriod.Location = new System.Drawing.Point(244, 42);
			this._udStopUpdatePeriod.Maximum = 1000;
			this._udStopUpdatePeriod.Minimum = 1;
			this._udStopUpdatePeriod.Name = "_udStopUpdatePeriod";
			this._udStopUpdatePeriod.Size = new System.Drawing.Size(40, 20);
			this._udStopUpdatePeriod.TabIndex = 4;
			//
			// _updateTimeOut
			//
			this._updateTimeOut.Changed = true;
			this._updateTimeOut.Location = new System.Drawing.Point(244, 70);
			this._updateTimeOut.Maximum = 1000;
			this._updateTimeOut.Minimum = 1;
			this._updateTimeOut.Name = "_updateTimeOut";
			this._updateTimeOut.Size = new System.Drawing.Size(40, 20);
			this._updateTimeOut.TabIndex = 7;
			this._updateTimeOut.Text = "1";
			this._updateTimeOut.Value = 1;
			//
			// label3
			//
			this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label3.Location = new System.Drawing.Point(8, 74);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(232, 16);
			this.label3.TabIndex = 6;
			this.label3.Text = "Feeds update &timeout (in seconds):";
            #endregion UpdateSettings

            #region Single settings
			//
			// _cookieProviderSelector
			//
			this._cookieProviderSelector.Location = new System.Drawing.Point(0, 114);
			this._cookieProviderSelector.Name = "_cookieProviderSelector";
			this._cookieProviderSelector.Size = new System.Drawing.Size(324, 24);
			this._cookieProviderSelector.TabIndex = 8;
			//
			// _chkRememberSelection
			//
			this._chkRememberSelection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._chkRememberSelection.Changed = false;
			this._chkRememberSelection.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._chkRememberSelection.InvertValue = false;
			this._chkRememberSelection.Location = new System.Drawing.Point(0, 142);
			this._chkRememberSelection.Name = "_chkRememberSelection";
			this._chkRememberSelection.Size = new System.Drawing.Size(460, 20);
			this._chkRememberSelection.TabIndex = 9;
			this._chkRememberSelection.Text = "&Remember selection in feeds between restarts";
			//
			// _chkShowDesktopAlert
			//
			this._chkShowDesktopAlert.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._chkShowDesktopAlert.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._chkShowDesktopAlert.Location = new System.Drawing.Point(0, 166);
			this._chkShowDesktopAlert.Name = "_chkShowDesktopAlert";
			this._chkShowDesktopAlert.Size = new System.Drawing.Size(460, 20);
			this._chkShowDesktopAlert.TabIndex = 10;
			this._chkShowDesktopAlert.Text = "&Show desktop alert when new items are received";
			//
			// _checkNewspaperAllowHoverSelection
			//
			this._checkNewspaperAllowHoverSelection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._checkNewspaperAllowHoverSelection.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._checkNewspaperAllowHoverSelection.Location = new System.Drawing.Point(0, 190);
			this._checkNewspaperAllowHoverSelection.Name = "_checkNewspaperAllowHoverSelection";
			this._checkNewspaperAllowHoverSelection.Size = new System.Drawing.Size(460, 20);
			this._checkNewspaperAllowHoverSelection.TabIndex = 11;
			this._checkNewspaperAllowHoverSelection.Text = "In Newspaper View, select items when I &point at them";
			//
			// _checkPropagateFavIconToItems
			//
			this._checkPropagateFavIconToItems.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._checkPropagateFavIconToItems.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._checkPropagateFavIconToItems.Location = new System.Drawing.Point(0, 214);
			this._checkPropagateFavIconToItems.Name = "_checkPropagateFavIconToItems";
			this._checkPropagateFavIconToItems.Size = new System.Drawing.Size(460, 20);
			this._checkPropagateFavIconToItems.TabIndex = 12;
			this._checkPropagateFavIconToItems.Text = "If a &feed has a custom icon, use it for feed items also";
			//
			// _checkUseDetailedURLs
			//
			this._checkUseDetailedURLs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._checkUseDetailedURLs.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._checkUseDetailedURLs.Location = new System.Drawing.Point(0, 238);
			this._checkUseDetailedURLs.Name = "_checkUseDetailedURLs";
			this._checkUseDetailedURLs.Size = new System.Drawing.Size(460, 20);
			this._checkUseDetailedURLs.TabIndex = 13;
			this._checkUseDetailedURLs.Text = "Show detailed 'Source' URLs";
            //
            // _checkShowSummary
            //
            this._checkShowSummary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._checkShowSummary.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._checkShowSummary.Location = new System.Drawing.Point(0, 262);
            this._checkShowSummary.Name = "_checkUseDetailedURLs";
            this._checkShowSummary.Size = new System.Drawing.Size(460, 20);
            this._checkShowSummary.TabIndex = 14;
            this._checkShowSummary.Text = "Show Atom post summary";
            #endregion Single settings

            #region Group Font Settings
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
			this._grpFontChars.Location = new System.Drawing.Point(0, 290);
			this._grpFontChars.Name = "groupExpRules";
			this._grpFontChars.Size = new System.Drawing.Size(464, 72);
			this._grpFontChars.TabIndex = 0;
			this._grpFontChars.TabStop = false;
			this._grpFontChars.Text = "RSS/Atom Post Font Settings";
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
            this._txtFont.Text = "";
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


            #region GroupExpRules
            //
			// groupExpRules
			//
			this.groupExpRules.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupExpRules.Controls.Add(this.labelDefaultExpRule);
			this.groupExpRules.Controls.Add(this.buttonEditExpRuleForDeleted);
			this.groupExpRules.Controls.Add(this.buttonClearDefault);
			this.groupExpRules.Controls.Add(this.labelExpRuleForDeleted);
			this.groupExpRules.Controls.Add(this.buttonEditDefExpRule);
			this.groupExpRules.Controls.Add(this.buttonClearDeleted);
			this.groupExpRules.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.groupExpRules.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
			this.groupExpRules.Location = new System.Drawing.Point(0, 523);
			this.groupExpRules.Name = "groupExpRules";
			this.groupExpRules.Size = new System.Drawing.Size(464, 78);
			this.groupExpRules.TabIndex = 13;
			this.groupExpRules.TabStop = false;
			this.groupExpRules.Text = "Autoexpiration Rules";
			//
			// labelDefaultExpRule
			//
			this.labelDefaultExpRule.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.labelDefaultExpRule.Location = new System.Drawing.Point(8, 20);
			this.labelDefaultExpRule.Name = "labelDefaultExpRule";
			this.labelDefaultExpRule.Size = new System.Drawing.Size(140, 16);
			this.labelDefaultExpRule.TabIndex = 0;
			this.labelDefaultExpRule.Text = "Default rule for all Feeds:";
			//
			// buttonEditExpRuleForDeleted
			//
			this.buttonEditExpRuleForDeleted.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonEditExpRuleForDeleted.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonEditExpRuleForDeleted.Location = new System.Drawing.Point(288, 45);
			this.buttonEditExpRuleForDeleted.Name = "buttonEditExpRuleForDeleted";
			this.buttonEditExpRuleForDeleted.TabIndex = 4;
			this.buttonEditExpRuleForDeleted.Text = "E&dit...";
			this.buttonEditExpRuleForDeleted.Click += new System.EventHandler(this.buttonEditExpRuleForDeleted_Click);
			//
			// buttonClearDefault
			//
			this.buttonClearDefault.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClearDefault.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonClearDefault.Location = new System.Drawing.Point(376, 17);
			this.buttonClearDefault.Name = "buttonClearDefault";
			this.buttonClearDefault.TabIndex = 2;
			this.buttonClearDefault.Text = "Clear";
			this.buttonClearDefault.Click += new System.EventHandler(this.buttonClearDefault_Click);
			//
			// labelExpRuleForDeleted
			//
			this.labelExpRuleForDeleted.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.labelExpRuleForDeleted.Location = new System.Drawing.Point(8, 48);
			this.labelExpRuleForDeleted.Name = "labelExpRuleForDeleted";
			this.labelExpRuleForDeleted.Size = new System.Drawing.Size(180, 16);
			this.labelExpRuleForDeleted.TabIndex = 3;
			this.labelExpRuleForDeleted.Text = "Rule for deleted RSS/ATOM Posts:";
			//
			// buttonEditDefExpRule
			//
			this.buttonEditDefExpRule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonEditDefExpRule.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonEditDefExpRule.Location = new System.Drawing.Point(288, 17);
			this.buttonEditDefExpRule.Name = "buttonEditDefExpRule";
			this.buttonEditDefExpRule.TabIndex = 1;
			this.buttonEditDefExpRule.Text = "&Edit...";
			this.buttonEditDefExpRule.Click += new System.EventHandler(this.buttonEditDefExpRule_Click);
			//
			// buttonClearDeleted
			//
			this.buttonClearDeleted.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClearDeleted.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonClearDeleted.Location = new System.Drawing.Point(376, 45);
			this.buttonClearDeleted.Name = "buttonClearDeleted";
			this.buttonClearDeleted.TabIndex = 5;
			this.buttonClearDeleted.Text = "Clear";
			this.buttonClearDeleted.Click += new System.EventHandler(this.buttonClearDeleted_Click);
            #endregion GroupExpRules

            //
			// RSSOptionPane
			//
			this.Controls.Add(this._grpUpdateSettings);
			this.Controls.Add(this._cookieProviderSelector);
            this.Controls.Add(this._grpFontChars);
			this.Controls.Add(this.groupExpRules);
			this.Controls.Add(this._chkShowDesktopAlert);
			this.Controls.Add(this._chkRememberSelection);
			this.Controls.Add(this._checkNewspaperAllowHoverSelection);
			this.Controls.Add(this._checkPropagateFavIconToItems);
            this.Controls.Add(this._checkUseDetailedURLs);
            this.Controls.Add(this._checkShowSummary);
			this.Name = "RSSOptionPane";
			this.Size = new System.Drawing.Size(464, 460);
            this._grpUpdateSettings.ResumeLayout(false);
			this.groupExpRules.ResumeLayout(false);
			this.ResumeLayout(false);

		}
        #endregion

        public override void ShowPane()
        {
            _udUpdateFrequency.SetSetting( Settings.UpdateFrequency );
            _udStopUpdatePeriod.SetSetting( Settings.StopUpdateFrequency );

            _updateTimeOut.SetSetting( Settings.TimeoutInSec );
            _cmbUpdatePeriod.SetSetting( Settings.UpdatePeriod );
            _cmbStopUpdatePeriod.SetSetting( Settings.StopUpdatePeriod );

            _oldUpdatePeriod = Settings.UpdatePeriod;
            _oldUpdateFrequency = Settings.UpdateFrequency;

            _chkRememberSelection.SetSetting( Settings.RememberSelection );
            _chkShowDesktopAlert.Checked = ( FindDesktopAlertRules().Count > 0 );
            _wasDesktopAlertChecked = _chkShowDesktopAlert.Checked;

			_checkNewspaperAllowHoverSelection.Checked = Core.SettingStore.ReadBool( "NewspaperView()", "AllowHoverSelection", true );
			_checkPropagateFavIconToItems.Checked = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.PropagateFavIconToItems, true );
			_checkUseDetailedURLs.Checked = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.UseDetailedURLs, false );

            if ( IsStartupPane )
            {
                _grpFontChars.Visible = false;
                groupExpRules.Visible = false;
            }
            else
            {
                ReadFontCharacteristics();
                ReadItemFormattingOptions();
                _txtFont.Text = _currFont + ", " + _currFontSize;
            }

            IResource resFolderType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "RSSFeed" );
            buttonClearDefault.Enabled = resFolderType.HasProp( "ExpirationRuleLink" );
            IResource resItemType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "RSSItem" );
            buttonClearDeleted.Enabled = resItemType.HasProp( "ExpirationRuleOnDeletedLink" );

            _cookieProviderSelector.Populate( typeof( RSSUnitOfWork ) );
        }

        private static IResourceList FindDesktopAlertRules()
        {
            return Core.ResourceStore.FindResourcesWithProp( "FilterRule", Props.DefaultDesktopAlertRule );
        }

        private bool _isOKProcessing = false;
        public override void OK()
        {
            if ( _isOKProcessing )
            {
                return;
            }
            _isOKProcessing = true;
            SettingSaver.Save( Controls );
            Settings.LoadSettings();

            Core.ResourceAP.RunJob( new UpdateDefaultsDelegate( UpdateDefaults ),
                _oldUpdateFrequency, _oldUpdatePeriod );
            if ( _chkShowDesktopAlert.Checked != _wasDesktopAlertChecked )
            {
                if ( _chkShowDesktopAlert.Checked )
                {
                    Core.ResourceAP.RunJob( new MethodInvoker( CreateDesktopAlertRule ) );
                }
                else
                {
                    Core.ResourceAP.RunJob( new MethodInvoker( DeleteDesktopAlertRule ) );
                }
            }
            CookiesManager.SetUserCookieProviderName( typeof( RSSUnitOfWork ), _cookieProviderSelector.SelectedProfileName );
            _isOKProcessing = false;

            WriteFontCharacteristics();
            WriteItemFormattingOptions();

			Core.SettingStore.WriteBool( "NewspaperView()", "AllowHoverSelection", _checkNewspaperAllowHoverSelection.Checked );
			Core.SettingStore.WriteBool( IniKeys.Section, IniKeys.PropagateFavIconToItems, _checkPropagateFavIconToItems.Checked );
        }

        private void CreateDesktopAlertRule()
        {
            IResource action = Core.ResourceStore.FindUniqueResource( "RuleAction", "Name", "Show desktop alert" );
            if ( action != null )
            {
                IResource rule = Core.FilterRegistry.RegisterRule( StandardEvents.ResourceReceived,
                    "Desktop Alert: Received Feed Items", new string[] { "RSSItem" },
                    new IResource[] {}, new IResource[] {}, new IResource[] { action } );
                rule.SetProp( Props.DefaultDesktopAlertRule, true );
            }
        }

        private void DeleteDesktopAlertRule()
        {
            foreach( IResource res in FindDesktopAlertRules() )
            {
                Core.FilterRegistry.DeleteRule( res );
            }
        }

        private void UpdateDefaults( int oldFreq, string oldPeriod )
        {
            IResourceList allFeeds = Core.ResourceStore.GetAllResources( "RSSFeed" );
            foreach( IResource feed in allFeeds )
            {
                if ( feed.GetIntProp( Props.UpdateFrequency ) == oldFreq &&
                    feed.GetStringProp( Props.UpdatePeriod ) == oldPeriod )
                {
                    feed.BeginUpdate();
                    feed.SetProp( Props.UpdateFrequency, (int)Settings.UpdateFrequency );
                    feed.SetProp( Props.UpdatePeriod, (string)Settings.UpdatePeriod );
                    feed.EndUpdate();
                }
            }
        }

        public override string GetHelpKeyword()
        {
            return "/reference/feeds.html";
        }

        #region Expiration Rules
        private void buttonEditDefExpRule_Click(object sender, EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "RSSFeed" );
            IResource linkedExpRule = resType.GetLinkProp( "ExpirationRuleLink" );

            Core.FilteringFormsManager.ShowExpirationRuleForm( resType, linkedExpRule, false );
            buttonClearDefault.Enabled = resType.HasProp( "ExpirationRuleLink" );
        }

        private void buttonClearDefault_Click(object sender, EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "RSSFeed" );
            IResource rule = resType.GetLinkProp( "ExpirationRuleLink" );

            Core.ExpirationRuleManager.UnregisterRule( rule.GetStringProp( "Name" ) );
            buttonClearDefault.Enabled = resType.HasProp( "ExpirationRuleLink" );
        }

        private void buttonEditExpRuleForDeleted_Click(object sender, EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "RSSItem" );
            IResource linkedExpRule = resType.GetLinkProp( "ExpirationRuleOnDeletedLink" );

            Core.FilteringFormsManager.ShowExpirationRuleForm( resType, linkedExpRule, true );
            buttonClearDeleted.Enabled = resType.HasProp( "ExpirationRuleOnDeletedLink" );
        }

        private void buttonClearDeleted_Click(object sender, EventArgs e)
        {
            IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "RSSItem" );
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
