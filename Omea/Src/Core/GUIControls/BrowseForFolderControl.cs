/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Windows.Forms;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Summary description for BrowseForFolderControl.
	/// </summary>
	public class BrowseForFolderControl : System.Windows.Forms.UserControl, ISettingControl
	{
        private StringSettingEditor _pathBox;
        private System.Windows.Forms.Button _btnBrowse;
        private System.Windows.Forms.FolderBrowserDialog _folderBrowser;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public BrowseForFolderControl()
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
			this._pathBox = new JetBrains.Omea.GUIControls.StringSettingEditor();
			this._btnBrowse = new System.Windows.Forms.Button();
			this._folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
			this.SuspendLayout();
			// 
			// _pathBox
			// 
			this._pathBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._pathBox.Changed = false;
			this._pathBox.Location = new System.Drawing.Point(0, 1);
			this._pathBox.Name = "_pathBox";
			this._pathBox.Size = new System.Drawing.Size(160, 20);
			this._pathBox.TabIndex = 0;
			this._pathBox.Text = "";
			// 
			// _btnBrowse
			// 
			this._btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnBrowse.Location = new System.Drawing.Point(165, 0);
			this._btnBrowse.Name = "_btnBrowse";
			this._btnBrowse.TabIndex = 1;
			this._btnBrowse.Text = "&Browse...";
			this._btnBrowse.Click += new System.EventHandler(this.OnBrowse);
			// 
			// BrowseForFolderControl
			// 
			this.Controls.Add(this._btnBrowse);
			this.Controls.Add(this._pathBox);
			this.Name = "BrowseForFolderControl";
			this.Size = new System.Drawing.Size(240, 28);
			this.ResumeLayout(false);

		}
		#endregion

        public string SelectedPath
        {
            get { return _pathBox.Text; }
            set { _pathBox.Text = value; }
        }

        private void OnBrowse(object sender, System.EventArgs e)
        {
            _folderBrowser.ShowNewFolderButton = true;
            _folderBrowser.SelectedPath = _pathBox.Text;
            if ( _folderBrowser.ShowDialog() == DialogResult.OK )
            {
                _pathBox.Changed = true;
                _pathBox.Text = _folderBrowser.SelectedPath; 
            }
        }

		/// <summary>
		/// Gets or sets the description that appears above the folders tree in the dialog.
		/// </summary>
		public string Description
		{
			get { return _folderBrowser.Description; }
			set { _folderBrowser.Description = value; }
		}

		#region ISettingControl Members

        public void SetSetting(JetBrains.Omea.Base.Setting setting)
        {
            _pathBox.SetSetting( setting );
        }

        public void Reset()
        {
            _pathBox.Reset();
        }

        public void SaveSetting()
        {
            _pathBox.SaveSetting();
        }

        public JetBrains.Omea.Base.Setting Setting
        {
            get
            {
                return _pathBox.Setting;
            }
        }

        public bool Changed
        {
            get
            {
                return _pathBox.Changed;
            }
            set
            {
                _pathBox.Changed = true;
            }
        }

        public void SetValue(object value)
        {
            _pathBox.SetValue( value );
        }

        #endregion
    }
}
