// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Omea.Base;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// The dialog notifying the user that an update is available.
	/// </summary>
	internal class UpdateNotifyDialog : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Button _btnYes;
        private System.Windows.Forms.Button _btnNo;
        private System.Windows.Forms.CheckBox _chkNoMoreUpdates;
        private System.Windows.Forms.Label _lblNewVersion;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public UpdateNotifyDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
            this._lblNewVersion = new System.Windows.Forms.Label();
            this._btnYes = new System.Windows.Forms.Button();
            this._btnNo = new System.Windows.Forms.Button();
            this._chkNoMoreUpdates = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            //
            // _lblNewVersion
            //
            this._lblNewVersion.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblNewVersion.Location = new System.Drawing.Point(8, 8);
            this._lblNewVersion.Name = "_lblNewVersion";
            this._lblNewVersion.Size = new System.Drawing.Size(400, 35);
            this._lblNewVersion.TabIndex = 0;
            this._lblNewVersion.Text = "<ProductName> version <version> is available. Would you like to download it?";
            //
            // _btnYes
            //
            this._btnYes.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this._btnYes.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnYes.Location = new System.Drawing.Point(120, 50);
            this._btnYes.Name = "_btnYes";
            this._btnYes.TabIndex = 1;
            this._btnYes.Text = "Yes";
            //
            // _btnNo
            //
            this._btnNo.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnNo.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnNo.Location = new System.Drawing.Point(216, 50);
            this._btnNo.Name = "_btnNo";
            this._btnNo.TabIndex = 2;
            this._btnNo.Text = "No";
            //
            // _chkNoMoreUpdates
            //
            this._chkNoMoreUpdates.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkNoMoreUpdates.Location = new System.Drawing.Point(8, 78);
            this._chkNoMoreUpdates.Name = "_chkNoMoreUpdates";
            this._chkNoMoreUpdates.Size = new System.Drawing.Size(384, 20);
            this._chkNoMoreUpdates.TabIndex = 3;
            this._chkNoMoreUpdates.Text = "Don\'t check for updates any more";
            //
            // UpdateNotifyDialog
            //
            this.AcceptButton = this._btnYes;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnNo;
            this.ClientSize = new System.Drawing.Size(410, 110);
            this.Controls.Add(this._chkNoMoreUpdates);
            this.Controls.Add(this._btnNo);
            this.Controls.Add(this._btnYes);
            this.Controls.Add(this._lblNewVersion);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateNotifyDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "UpdateNotifyDialog";
            this.ResumeLayout(false);

        }
		#endregion

        internal static void NotifyNewVersion( string newVersion, string versionUrl )
        {
            UpdateNotifyDialog dlg = new UpdateNotifyDialog();
            dlg.Text = Core.ProductFullName + " Update";
            dlg.Icon = (Core.MainWindow as Form).Icon;
            dlg._lblNewVersion.Text = Core.ProductFullName + " version " + newVersion +
                " is available. Would you like to download it?";
            DialogResult dr = dlg.ShowDialog( Core.MainWindow );
            if ( dr == DialogResult.Yes )
            {
                try
                {
                    if ( versionUrl != null )
                    {
                        Core.UIManager.OpenInNewBrowserWindow( versionUrl );
                    }
                    else
                    {
#if READER
                        Core.UIManager.OpenInNewBrowserWindow( "http://www.jetbrains.com/omea_reader/download/" );
#else
                        Core.UIManager.OpenInNewBrowserWindow( "http://www.jetbrains.com/omea/download/" );
#endif
                    }
                }
                catch( Exception )
                {
                    // ignore
                }
            }
            if ( dlg._chkNoMoreUpdates.Checked )
            {
                Core.SettingStore.WriteBool( "UpdateManager", "CheckForUpdates", false );
                Core.NetworkAP.QueueJobAt( DateTime.Now.AddSeconds( 10 ), new MethodInvoker( UpdateManager.RefuseUpdates ) );
            }
        }
	}

    internal class UpdateManager
    {
        internal static void QueueUpdateCheck()
        {
            bool checkForUpdates = Core.SettingStore.ReadBool( "UpdateManager", "CheckForUpdates", true );
            if ( !checkForUpdates )
            {
                return;
            }

            int updateCheckDays = Core.SettingStore.ReadInt( "UpdateManager", "UpdateCheckDays", 1 );
            DateTime lastUpdateCheck = Core.SettingStore.ReadDate( "UpdateManager", "LastCheckTime", DateTime.Now.AddDays( -1 ) );
            TimeSpan ts = DateTime.Now - lastUpdateCheck;
            if ( ts.TotalDays >= updateCheckDays )
            {
                Core.NetworkAP.QueueJobAt( DateTime.Now.AddSeconds( 15 ),
                    new CheckForUpdatesDelegate( CheckForUpdates ), false );
            }
            else
            {
                Core.NetworkAP.QueueJobAt( lastUpdateCheck.AddDays( updateCheckDays ),
                    new CheckForUpdatesDelegate( CheckForUpdates ), false );
            }
        }

        internal static void CheckForUpdatesNow()
        {
            Core.NetworkAP.QueueJob( JobPriority.Immediate,
                new CheckForUpdatesDelegate( CheckForUpdates ), true );
        }

        private static void CheckForUpdates( bool manualCheck )
        {
            int newBuild;
            string newVersion;
            string versionUrl = null;

            WebClient client = new WebClient();
            try
            {
                client.Headers.Add( "User-Agent", HttpReader.UserAgent );

                byte[] data;
                string url;

#if READER
                url = "http://www.jetbrains.com/omea/reader-update";
#else
                url = "http://www.jetbrains.com/omea/pro-update";
#endif
                url += ".xml";

                data = client.DownloadData( url );
                JetMemoryStream dataStream = new JetMemoryStream( data, true );
                XmlDocument doc = new XmlDocument();
                doc.Load( dataStream );
                XmlNode buildNode = doc.SelectSingleNode( "//omea-update/build" );
                newBuild = Int32.Parse( buildNode.InnerText );
                XmlNode versionNode = doc.SelectSingleNode( "//omea-update/version" );
                newVersion = versionNode.InnerText;
                XmlNode urlNode = doc.SelectSingleNode( "//omea-update/url" );
                if ( urlNode != null )
                {
                    versionUrl = urlNode.InnerText;
                }
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "Error checking for updates: " + ex.Message );
                Core.NetworkAP.QueueJobAt( DateTime.Now.AddHours( 1 ),
                    new CheckForUpdatesDelegate( CheckForUpdates ), false );
                return;
            }

            Core.SettingStore.WriteDate( "UpdateManager", "LastCheckTime", DateTime.Now );
            if ( !manualCheck )
            {
                QueueUpdateCheck();
            }
            if ( newBuild > Assembly.GetExecutingAssembly().GetName().Version.Build )
            {
                Core.UIManager.QueueUIJob( new NotifyNewVersionDelegate( UpdateNotifyDialog.NotifyNewVersion ), newVersion, versionUrl );
            }
            else if ( manualCheck )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( NotifyNoUpdates ) );
            }
        }

        private static void NotifyNoUpdates()
        {
            MessageBox.Show( Core.MainWindow,
                "You are running the latest version of " + Core.ProductFullName,
                Core.ProductFullName + " Update" );
        }

        private delegate void CheckForUpdatesDelegate( bool manualCheck );
        private delegate void NotifyNewVersionDelegate( string newVersion, string versionUrl );

        internal static void RefuseUpdates()
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add( "User-Agent", HttpReader.UserAgent );
#if READER
                client.DownloadData( "http://www.jetbrains.com/omea/omea-reader-update-stop.xml" );
#else
                client.DownloadData( "http://www.jetbrains.com/omea/omea-update-stop.xml" );
#endif
            }
            catch( Exception )
            {
                // ignore
            }
        }
    }

    public class CheckForUpdatesAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            UpdateManager.CheckForUpdatesNow();
        }
    }
}
