/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.RSSPlugin
{
    /// <summary>
    /// Options pane for the RSS plugin.
    /// </summary>
    public class RSSEnclosureOptionPane: AbstractOptionsPane
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private BrowseForFolderControl  _browseForFolderControl;
        private CheckBoxSettingEditor   _chkDownloadPeriod;
        private TimeUpDownEditor        _edtStartDownload, _edtFinishDownload;
        private System.Windows.Forms.Label label5;
        private CheckBoxSettingEditor   _chkShowDesktopAlertDownloadComplete;
        private CheckBoxSettingEditor   _chkShowDesktopAlertDownloadFailed;
        private CheckBoxSettingEditor   _chkCreateSubfolderForEveryFeed;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private int _oldUpdateFrequency;
        private string _oldUpdatePeriod;

        private const string  cHelpTopic = "/reference/feeds_enclosures.htm";

        public RSSEnclosureOptionPane()
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
            this.label5 = new System.Windows.Forms.Label();
            this._edtFinishDownload = new JetBrains.Omea.GUIControls.TimeUpDownEditor();
            this._edtStartDownload = new JetBrains.Omea.GUIControls.TimeUpDownEditor();
            this._browseForFolderControl = new JetBrains.Omea.GUIControls.BrowseForFolderControl();
            this._chkDownloadPeriod = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._chkShowDesktopAlertDownloadComplete = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._chkShowDesktopAlertDownloadFailed = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._chkCreateSubfolderForEveryFeed = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this.button1 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.SuspendLayout();
            // 
            // _chkDownloadPeriod
            // 
            this._chkDownloadPeriod.Changed = false;
            this._chkDownloadPeriod.InvertValue = false;
            this._chkDownloadPeriod.Location = new System.Drawing.Point(0, 132);
            this._chkDownloadPeriod.Name = "_chkDownloadPeriod";
            this._chkDownloadPeriod.Size = new System.Drawing.Size(116, 20);
            this._chkDownloadPeriod.TabIndex = 7;
            this._chkDownloadPeriod.Text = "Download fro&m";
            this._chkDownloadPeriod.CheckedChanged+=new EventHandler(_chkDownloadPeriod_CheckedChanged);
            // 
            // _edtStartDownload
            // 
            this._edtStartDownload.Location = new System.Drawing.Point(120, 132);
            this._edtStartDownload.Name = "_edtStartDownload";
            this._edtStartDownload.Size = new System.Drawing.Size(70, 20);
            this._edtStartDownload.TabIndex = 8;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(190, 134);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(16, 20);
            this.label5.TabIndex = 9;
            this.label5.Text = "&to";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _edtFinishDownload
            // 
            this._edtFinishDownload.Location = new System.Drawing.Point(206, 132);
            this._edtFinishDownload.Name = "_edtFinishDownload";
            this._edtFinishDownload.Size = new System.Drawing.Size(70, 20);
            this._edtFinishDownload.TabIndex = 10;
            // 
            // _browseForFolderControl
            // 
            this._browseForFolderControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._browseForFolderControl.Changed = true;
            this._browseForFolderControl.Location = new System.Drawing.Point(0, 104);
            this._browseForFolderControl.Name = "_browseForFolderControl";
            this._browseForFolderControl.SelectedPath = "";
            this._browseForFolderControl.Size = new System.Drawing.Size(392, 24);
            this._browseForFolderControl.TabIndex = 6;
            // 
            // _chkShowDesktopAlertDownloadComplete
            // 
            this._chkShowDesktopAlertDownloadComplete.Changed = false;
            this._chkShowDesktopAlertDownloadComplete.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkShowDesktopAlertDownloadComplete.InvertValue = false;
            this._chkShowDesktopAlertDownloadComplete.Location = new System.Drawing.Point(0, 20);
            this._chkShowDesktopAlertDownloadComplete.Name = "_chkShowDesktopAlertDownloadComplete";
            this._chkShowDesktopAlertDownloadComplete.Size = new System.Drawing.Size(372, 20);
            this._chkShowDesktopAlertDownloadComplete.TabIndex = 2;
            this._chkShowDesktopAlertDownloadComplete.Text = "Show desktop alert when enclosure downloading &completes";
            // 
            // _chkShowDesktopAlertDownloadFailed
            // 
            this._chkShowDesktopAlertDownloadFailed.Changed = false;
            this._chkShowDesktopAlertDownloadFailed.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkShowDesktopAlertDownloadFailed.InvertValue = false;
            this._chkShowDesktopAlertDownloadFailed.Location = new System.Drawing.Point(0, 40);
            this._chkShowDesktopAlertDownloadFailed.Name = "_chkShowDesktopAlertDownloadFailed";
            this._chkShowDesktopAlertDownloadFailed.Size = new System.Drawing.Size(372, 20);
            this._chkShowDesktopAlertDownloadFailed.TabIndex = 3;
            this._chkShowDesktopAlertDownloadFailed.Text = "Show desktop alert when enclosure downloading &fails";
            // 
            // _chkCreateSubfolderForEveryFeed
            // 
            this._chkCreateSubfolderForEveryFeed.Changed = false;
            this._chkCreateSubfolderForEveryFeed.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkCreateSubfolderForEveryFeed.InvertValue = false;
            this._chkCreateSubfolderForEveryFeed.Location = new System.Drawing.Point(0, 60);
            this._chkCreateSubfolderForEveryFeed.Name = "_chkShowDesktopAlertDownloadFailed";
            this._chkCreateSubfolderForEveryFeed.Size = new System.Drawing.Size(372, 20);
            this._chkCreateSubfolderForEveryFeed.TabIndex = 3;
            this._chkCreateSubfolderForEveryFeed.Text = "Create subfolder for every feed.";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(0, 0);
            this.button1.Name = "button1";
            this.button1.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(0, 4);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(104, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "Show desktop alert";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Location = new System.Drawing.Point(108, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(296, 8);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(0, 84);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(244, 16);
            this.label1.TabIndex = 4;
            this.label1.Text = "&Destination folder for downloaded feed enclosures";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Location = new System.Drawing.Point(248, 84);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(156, 8);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            // 
            // RSSEnclosureOptionPane
            // 
            this.Controls.Add(this.label3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this._chkShowDesktopAlertDownloadComplete);
            this.Controls.Add(this._chkShowDesktopAlertDownloadFailed);
            this.Controls.Add(this._chkCreateSubfolderForEveryFeed);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this._edtFinishDownload);
            this.Controls.Add(this._edtStartDownload);
            this.Controls.Add(this._browseForFolderControl);
            this.Controls.Add(this._chkDownloadPeriod);
            this.Name = "RSSEnclosureOptionPane";
            this.Size = new System.Drawing.Size(408, 140);
            this.ResumeLayout(false);

        }
        #endregion

        private int GetLocaleITime()
        {
            try
            {
                byte iTime;
                Win32Declarations.GetLocaleInfo( Win32Declarations.LOCALE_USER_DEFAULT, Win32Declarations.LOCALE_ITIME, out iTime, 1 );
                return Int32.Parse( ((char)iTime).ToString() );
            }
            catch
            {
                return 0;
            }
        }
        private int GetLocaleITimeMarkPosn()
        {
            try
            {
                byte iTime;
                Win32Declarations.GetLocaleInfo( Win32Declarations.LOCALE_USER_DEFAULT, Win32Declarations.LOCALE_ITIMEMARKPOSN, out iTime, 1 );
                return Int32.Parse( ((char)iTime).ToString() );
            }
            catch
            {
                return 0;
            }
        }
        public override void ShowPane()
        {
            _chkDownloadPeriod.SetSetting( Settings.UseEclosureDownloadPeriod );

            int iTime = GetLocaleITime();
            int iTimeMarkPos = GetLocaleITimeMarkPosn();
            _edtStartDownload.SetLocaleInfo( iTime, iTimeMarkPos );
            _edtFinishDownload.SetLocaleInfo( iTime, iTimeMarkPos );

            _edtStartDownload.Value = Settings.EnclosureDownloadStartHour;
            _edtFinishDownload.Value = Settings.EnclosureDownloadFinishHour;
            bool  validTime = _edtStartDownload.TimeParseable() && _edtFinishDownload.TimeParseable();

            if( !_edtStartDownload.TimeParseable() )   _edtStartDownload.Value = Settings.cInitialTimeStamp;
            if( !_edtFinishDownload.TimeParseable() )  _edtFinishDownload.Value = Settings.cInitialTimeStamp;

            //  The setting is enabled only if the time set in the controls is a valid
            //  parseable time of day.
            _chkDownloadPeriod.Checked = _chkDownloadPeriod.Checked && validTime;
            _edtStartDownload.Enabled = _edtFinishDownload.Enabled = _chkDownloadPeriod.Checked;
                
            _chkShowDesktopAlertDownloadComplete.SetSetting( Settings.ShowDesktopAlertWhenEncosureDownloadingComplete );
            _chkShowDesktopAlertDownloadFailed.SetSetting( Settings.ShowDesktopAlertWhenEncosureDownloadingFailed );
            _chkCreateSubfolderForEveryFeed.SetSetting( Settings.CreateSubfolderForEveryFeed );
            _browseForFolderControl.SetSetting( Settings.EnclosurePath );
            _browseForFolderControl.Description = "Select a folder for storing the downloaded feed enclosures:";

            _oldUpdatePeriod = Settings.UpdatePeriod;
            _oldUpdateFrequency = Settings.UpdateFrequency;
        }

        public override void OK()
        {
            _chkDownloadPeriod.Checked = _chkDownloadPeriod.Checked && 
                                         _edtStartDownload.TimeParseable() && _edtFinishDownload.TimeParseable();

            Settings.EnclosureDownloadStartHour.Save( _edtStartDownload.Value );
            Settings.EnclosureDownloadFinishHour.Save( _edtFinishDownload.Value );
            Settings.EnclosurePath.Save( _browseForFolderControl.SelectedPath );
            SettingSaver.Save( Controls );
            Settings.LoadSettings();

            Core.ResourceAP.QueueJob( new UpdateDefaultsDelegate( UpdateDefaults ), _oldUpdateFrequency, _oldUpdatePeriod );
            EnclosureDownloadManager.DownloadNextEnclosure();
        }

        private void UpdateDefaults( int oldFreq, string oldPeriod )
        {
            IResourceList allFeeds = Core.ResourceStore.GetAllResources( "RSSFeed" );
            foreach( IResource feed in allFeeds )
            {
                if( feed.GetIntProp( Props.UpdateFrequency ) == oldFreq && 
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
            return cHelpTopic;
        }

        private delegate void UpdateDefaultsDelegate( int oldFreq, string oldPeriod );

        private void _chkDownloadPeriod_CheckedChanged(object sender, EventArgs e)
        {
            _edtStartDownload.Enabled = _edtFinishDownload.Enabled = _chkDownloadPeriod.Checked;
        }
    }
}
