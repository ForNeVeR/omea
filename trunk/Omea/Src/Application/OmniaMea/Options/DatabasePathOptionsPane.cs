/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.TextIndex;

namespace JetBrains.Omea
{
	/// <summary>
	/// Options pane for specifying the database path.
	/// </summary>
	public class DatabasePathOptionsPane : AbstractOptionsPane
	{
        private FolderBrowserDialog _folderBrowserDialog;
        private GroupBox _groupDbPath;
        private Button _btnBrowseDb;
        private JetTextBox _edtDbPath;
        private GroupBox _groupLogPath;
        private Button _btnBrowseLogs;
        private JetTextBox _edtLogPath;
        private Label _lblLogLocation;
        private Label _lblRestartRequiredForDB, _lblRestartRequiredForLog;
        private Label _lblDatabaseLocation;
        private Label _lblBackupLocation;
        private JetTextBox _backupPath;
        private Button _backupBtn;
        private Button _browseBackupDir;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private Container components = null;
        private CheckBox _enableBackupBox;
        private GroupBox _backupGroup;

        private static bool _isBackuping;

		public DatabasePathOptionsPane()
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
            this._folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this._groupDbPath = new System.Windows.Forms.GroupBox();
            this._lblDatabaseLocation = new System.Windows.Forms.Label();
            this._btnBrowseDb = new System.Windows.Forms.Button();
            this._edtDbPath = new JetBrains.Omea.GUIControls.JetTextBox();
            this._lblRestartRequiredForDB = new System.Windows.Forms.Label();
            this._lblRestartRequiredForLog = new System.Windows.Forms.Label();
            this._groupLogPath = new System.Windows.Forms.GroupBox();
            this._lblLogLocation = new System.Windows.Forms.Label();
            this._btnBrowseLogs = new System.Windows.Forms.Button();
            this._edtLogPath = new JetBrains.Omea.GUIControls.JetTextBox();
            this._backupGroup = new System.Windows.Forms.GroupBox();
            this._backupBtn = new System.Windows.Forms.Button();
            this._lblBackupLocation = new System.Windows.Forms.Label();
            this._browseBackupDir = new System.Windows.Forms.Button();
            this._backupPath = new JetBrains.Omea.GUIControls.JetTextBox();
            this._enableBackupBox = new System.Windows.Forms.CheckBox();
            this._groupDbPath.SuspendLayout();
            this._groupLogPath.SuspendLayout();
            this._backupGroup.SuspendLayout();
            this.SuspendLayout();

            #region Db Path
            // 
            // _groupDbPath
            // 
            this._groupDbPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._groupDbPath.Controls.Add(this._lblDatabaseLocation);
            this._groupDbPath.Controls.Add(this._btnBrowseDb);
            this._groupDbPath.Controls.Add(this._edtDbPath);
		    this._groupDbPath.Controls.Add( _lblRestartRequiredForDB );
            this._groupDbPath.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._groupDbPath.Location = new System.Drawing.Point(0, 0);
            this._groupDbPath.Name = "_groupDbPath";
            this._groupDbPath.Size = new System.Drawing.Size(416, 108);
            this._groupDbPath.TabIndex = 15;
            this._groupDbPath.TabStop = false;
            this._groupDbPath.Text = "Database Path";
            // 
            // _btnBrowseDb
            // 
            this._btnBrowseDb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnBrowseDb.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnBrowseDb.Location = new System.Drawing.Point(332, 21);
            this._btnBrowseDb.Name = "_btnBrowseDb";
            this._btnBrowseDb.TabIndex = 12;
            this._btnBrowseDb.Text = "Browse...";
            this._btnBrowseDb.Click += new System.EventHandler(this._btnBrowseDb_Click);
            // 
            // _edtDbPath
            // 
            this._edtDbPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtDbPath.ContextProvider = null;
            this._edtDbPath.EmptyText = null;
            this._edtDbPath.Location = new System.Drawing.Point(7, 20);
            this._edtDbPath.Name = "_edtDbPath";
            this._edtDbPath.Size = new System.Drawing.Size(317, 21);
            this._edtDbPath.TabIndex = 11;
            this._edtDbPath.Text = "";
            this._edtDbPath.TextChanged += new System.EventHandler(this._edtDbPath_TextChanged);
            // 
            // _lblDatabaseLocation
            // 
            this._lblDatabaseLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblDatabaseLocation.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblDatabaseLocation.Location = new System.Drawing.Point(8, 55);
            this._lblDatabaseLocation.Name = "_lblDatabaseLocation";
            this._lblDatabaseLocation.Size = new System.Drawing.Size(396, 32);
            this._lblDatabaseLocation.TabIndex = 18;
            this._lblDatabaseLocation.Text = "This is where Omea stores its database. You will need at least 500 MB of free" +
                                             " disk storage space at this location.";
            //
            // _lblRestartRequiredForDB
            //
            this._lblRestartRequiredForDB.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this._lblRestartRequiredForDB.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblRestartRequiredForDB.Location = new System.Drawing.Point(8, 85);
            this._lblRestartRequiredForDB.Name = "_lblRestartRequiredForDB";
            this._lblRestartRequiredForDB.Size = new System.Drawing.Size(396, 18);
            this._lblRestartRequiredForDB.TabIndex = 19;
            this._lblRestartRequiredForDB.Text = "Restart Omea for the option to take an effect.";
            this._lblRestartRequiredForDB.ForeColor = Color.LightSlateGray;
            #endregion Db Path

            #region Log Path
            // 
            // _groupLogPath
            // 
            this._groupLogPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._groupLogPath.Controls.Add(this._lblLogLocation);
            this._groupLogPath.Controls.Add(this._btnBrowseLogs);
            this._groupLogPath.Controls.Add(this._edtLogPath);
		    this._groupLogPath.Controls.Add( _lblRestartRequiredForLog );
            this._groupLogPath.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._groupLogPath.Location = new System.Drawing.Point(0, 112);
            this._groupLogPath.Name = "_groupLogPath";
            this._groupLogPath.Size = new System.Drawing.Size(416, 110);
            this._groupLogPath.TabIndex = 16;
            this._groupLogPath.TabStop = false;
            this._groupLogPath.Text = "Log Files Path";
            // 
            // _btnBrowseLogs
            // 
            this._btnBrowseLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnBrowseLogs.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnBrowseLogs.Location = new System.Drawing.Point(332, 21);
            this._btnBrowseLogs.Name = "_btnBrowseLogs";
            this._btnBrowseLogs.TabIndex = 16;
            this._btnBrowseLogs.Text = "Browse...";
            this._btnBrowseLogs.Click += new System.EventHandler(this._btnBrowseLogs_Click);
            // 
            // _edtLogPath
            // 
            this._edtLogPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtLogPath.ContextProvider = null;
            this._edtLogPath.EmptyText = null;
            this._edtLogPath.Location = new System.Drawing.Point(4, 20);
            this._edtLogPath.Name = "_edtLogPath";
            this._edtLogPath.Size = new System.Drawing.Size(320, 21);
            this._edtLogPath.TabIndex = 15;
            this._edtLogPath.Text = "";
            // 
            // _lblLogLocation
            // 
            this._lblLogLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblLogLocation.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblLogLocation.Location = new System.Drawing.Point(8, 53);
            this._lblLogLocation.Name = "_lblLogLocation";
            this._lblLogLocation.Size = new System.Drawing.Size(396, 32);
            this._lblLogLocation.Text = "This is where Omea stores its log files. They are useful to troubleshoot" +
                " problems with the product.";
            //
            // _lblRestartRequiredForLog
            //
            this._lblRestartRequiredForLog.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this._lblRestartRequiredForLog.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblRestartRequiredForLog.Location = new System.Drawing.Point(8, 88);
            this._lblRestartRequiredForLog.Name = "_lblRestartRequiredForDB";
            this._lblRestartRequiredForLog.Size = new System.Drawing.Size(396, 18);
            this._lblRestartRequiredForLog.Text = "Restart Omea for the option to take an effect.";
            this._lblRestartRequiredForLog.ForeColor = Color.LightSlateGray;
            #endregion Log Path

            #region Backup path
            // 
            // _enableBackupBox
            // 
            this._enableBackupBox.Location = new System.Drawing.Point(0, 228);
            this._enableBackupBox.Name = "_enableBackupBox";
            this._enableBackupBox.Size = new System.Drawing.Size(184, 24);
            this._enableBackupBox.TabIndex = 18;
            this._enableBackupBox.Text = "Enable database backup";
            this._enableBackupBox.CheckedChanged += new System.EventHandler(this._enableBackupBox_CheckedChanged);
            // 
            // _backupGroup
            // 
            this._backupGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._backupGroup.Controls.Add(this._backupBtn);
            this._backupGroup.Controls.Add(this._lblBackupLocation);
            this._backupGroup.Controls.Add(this._browseBackupDir);
            this._backupGroup.Controls.Add(this._backupPath);
            this._backupGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._backupGroup.Location = new System.Drawing.Point(8, 252);
            this._backupGroup.Name = "_backupGroup";
            this._backupGroup.Size = new System.Drawing.Size(408, 88);
            this._backupGroup.TabIndex = 17;
            this._backupGroup.TabStop = false;
            this._backupGroup.Text = "Database Backup Path";
            // 
            // _backupBtn
            // 
            this._backupBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._backupBtn.Location = new System.Drawing.Point(324, 53);
            this._backupBtn.Name = "_backupBtn";
            this._backupBtn.TabIndex = 18;
            this._backupBtn.Text = "Backup Now";
            this._backupBtn.Click += new System.EventHandler(this._backupBtn_Click);
            // 
            // _lblBackupLocation
            // 
            this._lblBackupLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblBackupLocation.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblBackupLocation.Location = new System.Drawing.Point(8, 54);
            this._lblBackupLocation.Name = "_lblBackupLocation";
            this._lblBackupLocation.Size = new System.Drawing.Size(308, 32);
            this._lblBackupLocation.TabIndex = 17;
            this._lblBackupLocation.Text = "This is where Omea will automatically store daily backup of its resources database.";
            // 
            // _browseBackupDir
            // 
            this._browseBackupDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._browseBackupDir.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._browseBackupDir.Location = new System.Drawing.Point(324, 22);
            this._browseBackupDir.Name = "_browseBackupDir";
            this._browseBackupDir.TabIndex = 16;
            this._browseBackupDir.Text = "Browse...";
            this._browseBackupDir.Click += new System.EventHandler(this._browseBackupDir_Click);
            // 
            // _backupPath
            // 
            this._backupPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._backupPath.ContextProvider = null;
            this._backupPath.EmptyText = null;
            this._backupPath.Location = new System.Drawing.Point(4, 20);
            this._backupPath.Name = "_backupPath";
            this._backupPath.Size = new System.Drawing.Size(312, 21);
            this._backupPath.TabIndex = 15;
            this._backupPath.Text = "Here is the path to the database backup file";
            this._backupPath.TextChanged += new System.EventHandler(this._backupPath_TextChanged);
            #endregion Backup path

            // 
            // DatabasePathOptionsPane
            // 
            this.Controls.Add(this._enableBackupBox);
            this.Controls.Add(this._backupGroup);
            this.Controls.Add(this._groupLogPath);
            this.Controls.Add(this._groupDbPath);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "DatabasePathOptionsPane";
            this.Size = new System.Drawing.Size(420, 316);
            this._groupDbPath.ResumeLayout(false);
            this._groupLogPath.ResumeLayout(false);
            this._backupGroup.ResumeLayout(false);
            this.ResumeLayout(false);
        }
		#endregion

        public event EventHandler DbPathChanged;

	    public override void ShowPane()
	    {
#if READER
            this.label3.Text = "This is where Omea will store its database. You will need at least 50 MB of free" +
                " disk storage space at this location.";
#endif
            LoadSettings();
	    }
	    
        public static void SetBackupDefaults()
        {
            ISettingStore ini = Core.SettingStore;
            if( ini.ReadString( "ResourceStore", "EnableBackup" ).Length == 0 )
            {
                ini.WriteBool( "ResourceStore", "EnableBackup", true );
                ini.WriteString( "ResourceStore", "BackupPath", GetDefaultBackupPath( OMEnv.WorkDir ) );
            }
        }

        public void LoadSettings()
        {
            _edtDbPath.Text = OMEnv.WorkDir;
            LoadLogPath();
            ISettingStore ini = Core.SettingStore;
            if( ini == null )
            {
                _enableBackupBox.Visible = false;
                _backupGroup.Visible = false;
            }
            else
            {
                _backupPath.Text = ini.ReadString( "ResourceStore", "BackupPath", string.Empty );
                _enableBackupBox.Checked = ini.ReadBool( "ResourceStore", "EnableBackup", false );
                _backupGroup.Enabled = _enableBackupBox.Checked;
            }
        }

	    private void LoadLogPath()
	    {
	        string logPath = RegUtil.LogPath;
	        if ( logPath == null )
	        {
	            string basePath = Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );
	            logPath = IOTools.Combine( IOTools.Combine( basePath, @"JetBrains\Omea" ), "logs" );
	        }
	        _edtLogPath.Text = logPath;
	    }

	    public override void OK()
        {
            if( RegUtil.DatabasePath != _edtDbPath.Text ) 
            {
                RegUtil.DatabasePath = _edtDbPath.Text;
                NeedRestart = true;
            }
            if( RegUtil.LogPath != _edtLogPath.Text ) 
            {
                RegUtil.LogPath = _edtLogPath.Text;
                NeedRestart = true;
            }
	        SaveBackupPath();
        }

	    private void SaveBackupPath()
	    {
	        ISettingStore ini = Core.SettingStore;
            if(ini != null)
            {
                ini.WriteBool( "ResourceStore", "EnableBackup", _enableBackupBox.Checked );
                ini.WriteString( "ResourceStore", "BackupPath", _backupPath.Text );
            }
	    }

	    public override bool IsValid( ref string errorMessage, ref Control controlToSelect )
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo( _edtDbPath.Text );
                if( !Directory.Exists( dirInfo.FullName ) )
                {
                    errorMessage = "Database path doesn't exist";
                    controlToSelect = _edtDbPath;
                    return false;
                }
            }
            catch( Exception e )
            {
                errorMessage = e.Message;
                controlToSelect = _edtDbPath;
                return false;
            }
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo( _edtLogPath.Text );
                if( !Directory.Exists( dirInfo.FullName ) )
                {
                    // We should suggest to create this directory
                    DialogResult res = MessageBox.Show( "Log path '" + dirInfo.FullName + "' doesn't exist.\nCreate?",
                        "Omea Options",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error);
                    if(res == DialogResult.Yes)
                    {
                        Directory.CreateDirectory( dirInfo.FullName  );
                        if( !Directory.Exists( dirInfo.FullName ) )
                        {
                            errorMessage = "Log path '" + dirInfo.FullName + "' can not be created";
                            controlToSelect = _edtLogPath;
                            LoadLogPath();
                            return false;
                        }
                    }
                    else
                    {
                        // Reset to old
                        LoadLogPath();
                    }
                }
            }
            catch( Exception e )
            {
                errorMessage = e.Message;
                controlToSelect = _edtLogPath;
                return false;
            }
            try
            {
                if( _enableBackupBox.Checked )
                {
                    string backupPath = _backupPath.Text;
                    if( backupPath.Length > 0 )
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo( backupPath );
                        if( !Directory.Exists( dirInfo.FullName ) )
                        {
                            errorMessage = "Database backup path doesn't exist";
                            controlToSelect = _backupPath;
                            return false;
                        }
                    }
                }
            }
            catch( Exception e )
            {
                errorMessage = e.Message;
                controlToSelect = _backupPath;
                return false;
            }
            return true;
        }


	    public static AbstractOptionsPane CreatePane()
	    {
	        return new DatabasePathOptionsPane();
	    }

        private void _btnBrowseDb_Click( object sender, EventArgs e )
        {
            _folderBrowserDialog.Description = "Select the path for the database:";
            _folderBrowserDialog.SelectedPath = _edtDbPath.Text;
            if ( _folderBrowserDialog.ShowDialog( this ) == DialogResult.OK )
            {
                _edtDbPath.Text = _folderBrowserDialog.SelectedPath;
            }
            OnDbPathChanged();
        }

        private void _btnBrowseLogs_Click( object sender, EventArgs e )
        {
            _folderBrowserDialog.Description = "Select the path for the log files:";
            _folderBrowserDialog.SelectedPath = _edtLogPath.Text;
            if ( _folderBrowserDialog.ShowDialog( this ) == DialogResult.OK )
            {
                _edtLogPath.Text = _folderBrowserDialog.SelectedPath;
            }
        }

        private void _edtDbPath_TextChanged( object sender, EventArgs e )
        {
            OnDbPathChanged();
        }

	    private void OnDbPathChanged()
	    {
	        if ( DbPathChanged != null )
	        {
	            DbPathChanged( this, EventArgs.Empty );
	        }
	    }

	    public string DbPath
        {
            get { return _edtDbPath.Text; }
        }

	    public override string GetHelpKeyword()
	    {
	        return "/reference/paths.html";
	    }

        private void _backupPath_TextChanged(object sender, EventArgs e)
        {
            AdjustBackupButtonEnability();
        }

	    private void _browseBackupDir_Click(object sender, EventArgs e)
        {
            _folderBrowserDialog.Description = "Select the path for database backup:";
            string backupPath = _backupPath.Text.Trim();
            if( backupPath.Length == 0 )
            {
                backupPath = GetDefaultBackupPath( _edtDbPath.Text );
            }
            _folderBrowserDialog.SelectedPath = backupPath;
            if ( _folderBrowserDialog.ShowDialog( this ) == DialogResult.OK )
            {
                _backupPath.Text = _folderBrowserDialog.SelectedPath;
            }
        }

	    private void _backupBtn_Click(object sender, EventArgs e)
        {
            string backupPath = _backupPath.Text;
            if( backupPath.Length > 0 && Directory.Exists( backupPath ) )
            {
                SaveBackupPath();
                _isBackuping = true;
                AdjustBackupButtonEnability();
                MyPalStorage.BackupDatabase( false );
                Core.ResourceAP.QueueJob( JobPriority.Lowest, new MethodInvoker( MarkFinishedBackup ) );
            }
        }
	    
        private void _enableBackupBox_CheckedChanged(object sender, EventArgs e)
        {
            if( _backupGroup.Enabled = _enableBackupBox.Checked )
            {
                _backupPath.Focus();
                AdjustBackupButtonEnability();
            }
        }

        private void MarkFinishedBackup()
        {
            _isBackuping = false;
            Core.UserInterfaceAP.QueueJob( new MethodInvoker( AdjustBackupButtonEnability ) );
        }

        private void AdjustBackupButtonEnability()
        {
            string backupPath = _backupPath.Text;
            _backupBtn.Enabled = !_isBackuping && backupPath.Length > 0 && Directory.Exists( backupPath );
        }
	    
        private static string GetDefaultBackupPath( string dbPath )
        {
            string backupPath;
            backupPath = IOTools.Combine( dbPath, "backup" );
            if( !Directory.Exists( backupPath ) )
            {
                Directory.CreateDirectory( backupPath );
            }
            return backupPath;
        }
	}
}
