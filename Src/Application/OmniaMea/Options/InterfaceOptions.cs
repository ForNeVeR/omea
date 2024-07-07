// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// General options" page of the Options dialog.
    /// </summary>
    public class InterfaceOptions: AbstractOptionsPane
	{
        private const String csHelpKeyword = "/reference/general.html";
        private const int    ciDefaultVisibilityInterval = 4;

        private CheckBox    _chkMarkAsRead;
        private NumericUpDown _udMarkAsReadSeconds;
        private Label       label1;
        private CheckBox    _chkShowContext;
        private CheckBox    _chkUseShortDateFormat;
        private CheckBox    _chkSwitchToTab;
        private CheckBox    _chkMinimizeToTray;
        private CheckBox    _chkTrayIconMode;
        private CheckBox    _chkIdleTextIndex;

        private GroupBox    _grpOpenLinks;
        private RadioButton _radOmeaWindow;
        private RadioButton _radPreviewPane;
        private RadioButton _radBrowserWindow;

        private GroupBox    _grpBalloonUI;
        private Label       labelPeriodOfActivity;
        private NumericUpDown _udPeriod;
        private Label       labelSeconds;
        private Label       labelBackColor;
        private Panel       panelSample;
        private Button      btnChangeBack;

        private GroupBox    _grpFontChars;
        private Label       _lblFontFamily;
        private Button      _btnChangeFont;
        private TextBox     _txtFont;

        private GroupBox    _grpSubjectPrefixed;
        private Label       _labelPrefixesList;
        private TextBox     _editSubjectPrefixes;

        private string      _currFont;
        private int         _currFontSize;

        private readonly Color  DefltBackColor = Color.FromArgb(192, 192, 255);

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public InterfaceOptions()
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
            this._chkMarkAsRead = new System.Windows.Forms.CheckBox();
            this._udMarkAsReadSeconds = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this._chkShowContext = new System.Windows.Forms.CheckBox();
            this._chkUseShortDateFormat = new System.Windows.Forms.CheckBox();
            this._chkSwitchToTab = new System.Windows.Forms.CheckBox();
            this._chkMinimizeToTray = new System.Windows.Forms.CheckBox();
            this._chkTrayIconMode = new System.Windows.Forms.CheckBox();
            this._chkIdleTextIndex = new System.Windows.Forms.CheckBox();
            this._grpSubjectPrefixed = new GroupBox();
            this._labelPrefixesList = new Label();
            this._editSubjectPrefixes = new TextBox();

		    _grpOpenLinks = new GroupBox();
            _radOmeaWindow = new RadioButton();
            _radPreviewPane = new RadioButton();
            _radBrowserWindow = new RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this._udMarkAsReadSeconds)).BeginInit();

		    _grpBalloonUI = new GroupBox();
            labelPeriodOfActivity = new System.Windows.Forms.Label();
            _udPeriod = new System.Windows.Forms.NumericUpDown();
            labelSeconds = new System.Windows.Forms.Label();
            labelBackColor = new System.Windows.Forms.Label();
            panelSample = new System.Windows.Forms.Panel();
            btnChangeBack = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this._udPeriod)).BeginInit();

            this._grpFontChars = new System.Windows.Forms.GroupBox();
			this._lblFontFamily = new System.Windows.Forms.Label();
            this._txtFont = new System.Windows.Forms.TextBox();
            this._btnChangeFont = new System.Windows.Forms.Button();

            this._grpFontChars.SuspendLayout();
		    _grpBalloonUI.SuspendLayout();
            this.SuspendLayout();

            #region Singular options
            //
            // _chkMarkAsRead
            //
            this._chkMarkAsRead.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkMarkAsRead.Location = new System.Drawing.Point(4, 0);
            this._chkMarkAsRead.Name = "_chkMarkAsRead";
            this._chkMarkAsRead.Size = new System.Drawing.Size(228, 16);
            this._chkMarkAsRead.TabIndex = 0;
            this._chkMarkAsRead.Text = "Mark item read after displaying for";
            this._chkMarkAsRead.Click += new System.EventHandler(this._chkMarkAsRead_Click);
            //
            // _udMarkAsReadSeconds
            //
            this._udMarkAsReadSeconds.Location = new System.Drawing.Point(244, 0);
            this._udMarkAsReadSeconds.Maximum = new System.Decimal(new int[] {
                                                                                 300,
                                                                                 0,
                                                                                 0,
                                                                                 0});
            this._udMarkAsReadSeconds.Name = "_udMarkAsReadSeconds";
            this._udMarkAsReadSeconds.Size = new System.Drawing.Size(36, 21);
            this._udMarkAsReadSeconds.TabIndex = 1;
            this._udMarkAsReadSeconds.Value = new System.Decimal(new int[] {
                                                                               1,
                                                                               0,
                                                                               0,
                                                                               0});
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(288, 1);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "second(s)";
            //
            // _chkShowContext
            //
            this._chkShowContext.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkShowContext.Location = new System.Drawing.Point(4, 20);
            this._chkShowContext.Name = "_chkShowContext";
            this._chkShowContext.Size = new System.Drawing.Size(336, 16);
            this._chkShowContext.TabIndex = 3;
            this._chkShowContext.Text = "Show context for search results";
            //
            // _chkUseShortDateFormat
            //
            this._chkUseShortDateFormat.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkUseShortDateFormat.Location = new System.Drawing.Point(4, 40);
            this._chkUseShortDateFormat.Name = "_chkUseShortDateFormat";
            this._chkUseShortDateFormat.Size = new System.Drawing.Size(336, 16);
            this._chkUseShortDateFormat.TabIndex = 4;
            this._chkUseShortDateFormat.Text = "Use short date format";
            //
            // _chkSwitchToTab
            //
            this._chkSwitchToTab.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkSwitchToTab.Location = new System.Drawing.Point(4, 60);
            this._chkSwitchToTab.Name = "_chkSwitchToTab";
            this._chkSwitchToTab.Size = new System.Drawing.Size(350, 16);
            this._chkSwitchToTab.TabIndex = 5;
            this._chkSwitchToTab.Text = "After search switch to tab which owns the results";
            //
            // _chkMinimizeToTray
            //
            this._chkMinimizeToTray.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkMinimizeToTray.Location = new System.Drawing.Point(4, 80);
            this._chkMinimizeToTray.Name = "_chkMinimizeToTray";
            this._chkMinimizeToTray.Size = new System.Drawing.Size(320, 16);
            this._chkMinimizeToTray.TabIndex = 6;
            this._chkMinimizeToTray.Text = "Minimize to the system tray";
            //
            // checkTrayIconMode
            //
            this._chkTrayIconMode.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkTrayIconMode.Location = new System.Drawing.Point(4, 100);
            this._chkTrayIconMode.Name = "_chkTrayIconMode";
            this._chkTrayIconMode.Size = new System.Drawing.Size(340, 16);
            this._chkTrayIconMode.TabIndex = 7;
            this._chkTrayIconMode.Text = "Change tray icon when one resource becomes read";
            this._chkTrayIconMode.CheckedChanged += new System.EventHandler(this.checkTrayIconMode_CheckedChanged);
            //
            // _chkIdleTextIndex
            //
            this._chkIdleTextIndex.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkIdleTextIndex.Location = new System.Drawing.Point(4, 120);
            this._chkIdleTextIndex.Name = "_chkIdleTextIndex";
            this._chkIdleTextIndex.Size = new System.Drawing.Size(340, 16);
            this._chkIdleTextIndex.TabIndex = 8;
            this._chkIdleTextIndex.Text = "Perform text indexing only in idle mode";
            #endregion Singular options

            #region Group Links in Web
            _grpOpenLinks.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
		    _grpOpenLinks.FlatStyle = FlatStyle.System;
		    _grpOpenLinks.Location = new Point(4, 140);
		    _grpOpenLinks.Size = new Size(348, 80);
		    _grpOpenLinks.Text = "Open links to Web pages";

            _radOmeaWindow.Location = new Point( 8, 16 );
            _radOmeaWindow.Size = new Size( 200, 16 );
            _radOmeaWindow.Text = "In Omea window";

            _radPreviewPane.Location = new Point( 8, 36 );
            _radPreviewPane.Size = new Size( 200, 16 );
            _radPreviewPane.Text = "In Omea preview pane";

            _radBrowserWindow.Location = new Point( 8, 56 );
            _radBrowserWindow.Size = new Size( 200, 16 );
            _radBrowserWindow.Text = "In a new browser window";

		    _grpOpenLinks.Controls.Add( _radOmeaWindow );
		    _grpOpenLinks.Controls.Add( _radPreviewPane );
		    _grpOpenLinks.Controls.Add( _radBrowserWindow );
            #endregion Group Links in Web

            #region Notification Balloon UI options.
            //
            // Notification Balloon UI options.
            //
		    _grpBalloonUI.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
		    _grpBalloonUI.FlatStyle = FlatStyle.System;
		    _grpBalloonUI.Location = new Point(4, 230);
		    _grpBalloonUI.Size = new Size(348, 72);
		    _grpBalloonUI.Text = "Notification Balloon";
            //
            // labelPeriodOfActivity
            //
            this.labelPeriodOfActivity.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelPeriodOfActivity.Location = new System.Drawing.Point(8, 18);
            this.labelPeriodOfActivity.Name = "labelPeriodOfActivity";
            this.labelPeriodOfActivity.Size = new System.Drawing.Size(90, 16);
            this.labelPeriodOfActivity.TabStop = false;
            this.labelPeriodOfActivity.Text = "Activity period:";
            //
            // _udPeriod
            //
            this._udPeriod.Location = new System.Drawing.Point(100, 16);
            this._udPeriod.Name = "_udPeriod";
            this._udPeriod.Size = new System.Drawing.Size(36, 21);
            this._udPeriod.TabIndex = 1;
            this._udPeriod.Maximum = new System.Decimal(new int[] { 10, 0, 0, 0});
            this._udPeriod.Minimum = new System.Decimal(new int[] { 1, 0, 0, 0});
            //
            // labelSeconds
            //
            this.labelSeconds.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelSeconds.Location = new System.Drawing.Point(145, 18);
            this.labelSeconds.Name = "labelSeconds";
            this.labelSeconds.Size = new System.Drawing.Size(60, 16);
            this.labelSeconds.TabStop = false;
            this.labelSeconds.Text = "second(s)";
            //
            // labelBackColor
            //
            this.labelBackColor.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelBackColor.Location = new System.Drawing.Point(8, 47);
            this.labelBackColor.Name = "labelBackColor";
            this.labelBackColor.Size = new System.Drawing.Size(90, 16);
            this.labelBackColor.TabStop = false;
            this.labelBackColor.Text = "Background color:";
            //
            // panelSample
            //
            this.panelSample.AutoScroll = false;
            this.panelSample.BackColor = System.Drawing.Color.White;
            this.panelSample.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelSample.Location = new System.Drawing.Point(100, 46);
            this.panelSample.Name = "panelSample";
            this.panelSample.Size = new System.Drawing.Size(40, 20);
            this.panelSample.TabStop = false;
            //
            // buttonChangeForeground
            //
            this.btnChangeBack.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnChangeBack.Location = new System.Drawing.Point(150, 45);
            this.btnChangeBack.Size = new System.Drawing.Size(22, 18);
            this.btnChangeBack.Name = "btnChangeBack";
            this.btnChangeBack.TabIndex = 6;
            this.btnChangeBack.Text = "...";
            this.btnChangeBack.Click += new EventHandler(btnChangeBack_Click);

		    _grpBalloonUI.Controls.Add( labelPeriodOfActivity );
		    _grpBalloonUI.Controls.Add( _udPeriod );
		    _grpBalloonUI.Controls.Add( labelSeconds );
		    _grpBalloonUI.Controls.Add( labelBackColor );
		    _grpBalloonUI.Controls.Add( panelSample );
		    _grpBalloonUI.Controls.Add( btnChangeBack );
            #endregion Notification Balloon UI options.

            #region Group Font Attributes
			//
			// _grpFontChars
			//
			this._grpFontChars.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._grpFontChars.Controls.Add(_lblFontFamily);
			this._grpFontChars.Controls.Add(_txtFont);
			this._grpFontChars.Controls.Add(_btnChangeFont);
			this._grpFontChars.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._grpFontChars.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
			this._grpFontChars.Location = new System.Drawing.Point(4, 312);
            this._grpFontChars.Name = "_grpFontChars";
			this._grpFontChars.Size = new System.Drawing.Size(348, 50);
			this._grpFontChars.TabIndex = 0;
			this._grpFontChars.TabStop = false;
			this._grpFontChars.Text = "Resource Content Font Settings";
			//
			// _lblFontFamily
			//
			this._lblFontFamily.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._lblFontFamily.Location = new System.Drawing.Point(8, 20);
			this._lblFontFamily.Name = "_lblFontFamily";
			this._lblFontFamily.Size = new System.Drawing.Size(40, 20);
			this._lblFontFamily.TabIndex = 2;
			this._lblFontFamily.Text = "F&ont:";
			this._lblFontFamily.UseMnemonic = true;
            //
            // _txtFont
            //
            this._txtFont.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this._txtFont.Location = new System.Drawing.Point(57, 16);
            this._txtFont.Name = "_txtFont";
            this._txtFont.Size = new System.Drawing.Size(130, 20);
            this._txtFont.TabIndex = 3;
            this._txtFont.ReadOnly = true;
            this._txtFont.Text = Core.UIManager.DefaultFontFace + ", " + Core.UIManager.DefaultFontSize;
			//
			// _btnChangeFont
			//
			this._btnChangeFont.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
			this._btnChangeFont.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnChangeFont.Location = new System.Drawing.Point(200, 15);
			this._btnChangeFont.Name = "_btnChangeFont";
			this._btnChangeFont.TabIndex = 4;
			this._btnChangeFont.Text = "&Change...";
			this._btnChangeFont.Click += new EventHandler(_btnChangeFont_Click);
            #endregion Group Font Attributes

            #region Subject Prefixes
            //
            // _grpSubjectPrefixed
            //
            this._grpSubjectPrefixed.Anchor = ( AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this._grpSubjectPrefixed.Controls.Add( _editSubjectPrefixes );
            this._grpSubjectPrefixed.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._grpSubjectPrefixed.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._grpSubjectPrefixed.Location = new System.Drawing.Point(4, 370);
            this._grpSubjectPrefixed.Name = "_grpSubjectPrefixed";
            this._grpSubjectPrefixed.Size = new System.Drawing.Size(348, 50);
            this._grpSubjectPrefixed.TabIndex = 0;
            this._grpSubjectPrefixed.TabStop = false;
            this._grpSubjectPrefixed.Text = "Subject prefixes";
            //
            // _labelPrefixesList
            //
            this._labelPrefixesList.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._labelPrefixesList.Location = new System.Drawing.Point(8, 19);
            this._labelPrefixesList.Name = "_labelPrefixesList";
		    this._labelPrefixesList.Size = new System.Drawing.Size(90, 19);
            this._labelPrefixesList.TabIndex = 8;
            this._labelPrefixesList.Text = "Subject prefixes:";
            //
            //  _editSubjectPrefixes
            //
            this._editSubjectPrefixes.AcceptsReturn = false;
            this._editSubjectPrefixes.Location = new System.Drawing.Point(100, 16);
            this._editSubjectPrefixes.Multiline = false;
            this._editSubjectPrefixes.Name = "_editSubjectPrefixes";
            this._editSubjectPrefixes.Size = new System.Drawing.Size(140, 18);
            this._editSubjectPrefixes.TabIndex = 9;

            _grpSubjectPrefixed.Controls.Add( _labelPrefixesList );
            _grpSubjectPrefixed.Controls.Add( _editSubjectPrefixes );
            #endregion Subject Prefixes

            //
            // InterfaceOptions
            //
            Controls.Add( _chkIdleTextIndex );
            Controls.Add( _chkTrayIconMode );
            Controls.Add( _chkShowContext );
		    Controls.Add( _chkUseShortDateFormat );
            Controls.Add( _chkSwitchToTab );
            Controls.Add( label1 );
            Controls.Add( _udMarkAsReadSeconds );
            Controls.Add( _chkMarkAsRead );
            Controls.Add( _chkMinimizeToTray );
            Controls.Add( _grpOpenLinks );
            Controls.Add( _grpBalloonUI );
            Controls.Add( _grpFontChars );
		    Controls.Add( _grpSubjectPrefixed );

            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "InterfaceOptions";
            this.Size = new System.Drawing.Size(356, 112);
            ((System.ComponentModel.ISupportInitialize)(this._udMarkAsReadSeconds)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._udPeriod)).EndInit();
            this._grpFontChars.ResumeLayout(false);
		    _grpBalloonUI.ResumeLayout(false);
            this.ResumeLayout(false);
        }
		#endregion

        #region Initialization
        public override void ShowPane()
        {
            ISettingStore ini = Core.SettingStore;
            int markReadMS = ini.ReadInt( "Resources", "MarkAsReadTimeOut", 2000 );
            if ( markReadMS == 0 )
            {
                _chkMarkAsRead.Checked = false;
                _udMarkAsReadSeconds.Enabled = false;
            }
            else
            {
                _chkMarkAsRead.Checked = true;
                _udMarkAsReadSeconds.Value = markReadMS / 1000;
            }
            _chkShowContext.Checked = ini.ReadBool( "Resources", "ShowSearchContext", true );
            _chkUseShortDateFormat.Checked = ini.ReadBool("Resources", "UseShortDateFormat", false);
            _chkSwitchToTab.Checked = ini.ReadBool( "Search", "AutoSwitchToResults", true );
            _chkMinimizeToTray.Checked = ini.ReadBool("Resources", "MinimizeToTray", false);
            _chkTrayIconMode.Checked = Core.TrayIconManager.IsOutlookMode;
            _chkIdleTextIndex.Checked = Core.TextIndexManager.IdleIndexingMode;

            if (ini.ReadBool("Resources", "LinksInNewWindow", false))
            {
                _radBrowserWindow.Checked = true;
            }
            else if ( ini.ReadBool( "Resources", "LinksInPreviewPane", false ) )
            {
                _radPreviewPane.Checked = true;
            }
            else
            {
                _radOmeaWindow.Checked = true;
            }

            _udPeriod.Value = Core.SettingStore.ReadInt( "General", "BalloonTimeout", ciDefaultVisibilityInterval );

            ReadBackColor();
            ReadFontCharacteristics();
            ReadPrefixes();
        }

        private void ReadFontCharacteristics()
        {
            _currFont = Core.UIManager.DefaultFontFace;
            _currFontSize = (int)Core.UIManager.DefaultFontSize;
            _txtFont.Text = _currFont + ", " + _currFontSize;
        }

        private void ReadBackColor()
        {
            int  r, g, b;
            r = Core.SettingStore.ReadInt( "General", "BalloonBackgroundR", 192 );
            g = Core.SettingStore.ReadInt( "General", "BalloonBackgroundG", 192 );
            b = Core.SettingStore.ReadInt( "General", "BalloonBackgroundB", 255 );
            try
            {
                panelSample.BackColor = Color.FromArgb(r, g, b);
            }
            catch( Exception )
            {
                panelSample.BackColor = DefltBackColor;
            }
        }

        private void ReadPrefixes()
        {
            string prefixes = Core.SettingStore.ReadString( "General", "SubjectPrefixes",
                                                            SubjectComparer.csDefaultPrefixes );
            _editSubjectPrefixes.Text = prefixes;
        }
        #endregion Initialization

        #region Save
        public override void OK()
        {
            ISettingStore ini = Core.SettingStore;
            int markReadMS;
            if ( !_chkMarkAsRead.Checked )
            {
                markReadMS = 0;
            }
            else
            {
                markReadMS = (int) _udMarkAsReadSeconds.Value * 1000;
                if ( markReadMS == 0 )
                {
                    markReadMS = 1;
                }
            }
            ini.WriteInt ( "Resources", "MarkAsReadTimeOut", markReadMS );
            ini.WriteBool( "Resources", "ShowSearchContext", _chkShowContext.Checked );
            ini.WriteBool( "Resources", "UseShortDateFormat", _chkUseShortDateFormat.Checked );
            ini.WriteBool( "Search", "AutoSwitchToResults", _chkSwitchToTab.Checked );
            ini.WriteBool( "Resources", "LinksInNewWindow", _radBrowserWindow.Checked );
            ini.WriteBool( "Resources", "LinksInPreviewPane", _radPreviewPane.Checked );
            ini.WriteBool( "Resources", "MinimizeToTray", _chkMinimizeToTray.Checked );

            ini.WriteInt ( "General", "BalloonTimeout", (int)_udPeriod.Value );
            ini.WriteInt ( "General", "BalloonBackgroundR", panelSample.BackColor.R );
            ini.WriteInt ( "General", "BalloonBackgroundG", panelSample.BackColor.G );
            ini.WriteInt ( "General", "BalloonBackgroundB", panelSample.BackColor.B );

            if( isValidPrefixes() )
            {
                SubjectComparer.SubjectPrefixes = _editSubjectPrefixes.Text;
                ini.WriteString( SubjectComparer.csIniSection, SubjectComparer.csIniKey, _editSubjectPrefixes.Text );
            }

            Core.UIManager.DefaultFormattingFont = new Font( _currFont, _currFontSize );
            Core.TextIndexManager.IdleIndexingMode = _chkIdleTextIndex.Checked;
        }
        #endregion Save

        private void _chkMarkAsRead_Click(object sender, EventArgs e)
        {
            if (_chkMarkAsRead.Checked)
            {
                _udMarkAsReadSeconds.Enabled = true;
                if (_udMarkAsReadSeconds.Value < 0)
                {
                    _udMarkAsReadSeconds.Value = 0;
                }
            }
            else
            {
                _udMarkAsReadSeconds.Enabled = false;
            }
        }

        private bool isValidPrefixes()
        {
            return !String.IsNullOrEmpty( _editSubjectPrefixes.Text );
        }

        public override string GetHelpKeyword()
        {
            return csHelpKeyword;
        }

        private void checkTrayIconMode_CheckedChanged(object sender, EventArgs e)
        {
            if( _chkTrayIconMode.Checked )
                Core.TrayIconManager.SetOutlookMode();
            else
                Core.TrayIconManager.SetStrictMode();
        }

        private void btnChangeBack_Click(object sender, EventArgs e)
        {
            ColorDialog dlg = new ColorDialog();
            if( dlg.ShowDialog( this ) == DialogResult.OK )
            {
                panelSample.BackColor = dlg.Color;
            }
        }

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
        #endregion Font Group
    }
}
