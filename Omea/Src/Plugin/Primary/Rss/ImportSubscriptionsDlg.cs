/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Dialog for selecting the source path of a subscription list to import.
	/// </summary>
	public class ImportSubscriptionsDlg : DialogBase
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _edtOPMLName;
        private System.Windows.Forms.Button _btnBrowse;
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.OpenFileDialog _dlgOpenFile;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Label _lblDownloading;
        private System.Windows.Forms.CheckBox _chkPreviewImportedFeeds;
        private Stream _importStream;

		public ImportSubscriptionsDlg()
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
            this.label1 = new System.Windows.Forms.Label();
            this._edtOPMLName = new System.Windows.Forms.TextBox();
            this._btnBrowse = new System.Windows.Forms.Button();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._dlgOpenFile = new System.Windows.Forms.OpenFileDialog();
            this._lblDownloading = new System.Windows.Forms.Label();
            this._chkPreviewImportedFeeds = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(420, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Enter the file name or URL of the subscription list in OPML format:";
            // 
            // _edtOPMLName
            // 
            this._edtOPMLName.Location = new System.Drawing.Point(8, 28);
            this._edtOPMLName.Name = "_edtOPMLName";
            this._edtOPMLName.Size = new System.Drawing.Size(336, 21);
            this._edtOPMLName.TabIndex = 1;
            this._edtOPMLName.Text = "";
            this._edtOPMLName.TextChanged += new System.EventHandler(this._edtOPMLName_TextChanged);
            // 
            // _btnBrowse
            // 
            this._btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnBrowse.Location = new System.Drawing.Point(352, 28);
            this._btnBrowse.Name = "_btnBrowse";
            this._btnBrowse.TabIndex = 2;
            this._btnBrowse.Text = "Browse...";
            this._btnBrowse.Click += new System.EventHandler(this._btnBrowse_Click);
            // 
            // _btnOK
            // 
            this._btnOK.Enabled = false;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(268, 88);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 5;
            this._btnOK.Text = "OK";
            this._btnOK.Click += new System.EventHandler(this._btnOK_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(352, 88);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 6;
            this._btnCancel.Text = "Cancel";
            // 
            // _dlgOpenFile
            // 
            this._dlgOpenFile.Filter = "OPML files (*.opml)|*.opml|All files (*.*)|*.*";
            this._dlgOpenFile.Title = "Select Subscription List File";
            // 
            // _lblDownloading
            // 
            this._lblDownloading.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblDownloading.Location = new System.Drawing.Point(12, 88);
            this._lblDownloading.Name = "_lblDownloading";
            this._lblDownloading.Size = new System.Drawing.Size(184, 16);
            this._lblDownloading.TabIndex = 4;
            this._lblDownloading.Text = "Downloading...";
            this._lblDownloading.Visible = false;
            // 
            // _chkPreviewImportedFeeds
            // 
            this._chkPreviewImportedFeeds.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkPreviewImportedFeeds.Location = new System.Drawing.Point(8, 60);
            this._chkPreviewImportedFeeds.Name = "_chkPreviewImportedFeeds";
            this._chkPreviewImportedFeeds.Size = new System.Drawing.Size(264, 20);
            this._chkPreviewImportedFeeds.TabIndex = 3;
            this._chkPreviewImportedFeeds.Text = "Preview feed list before importing";
            // 
            // ImportSubscriptionsDlg
            // 
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(436, 117);
            this.Controls.Add(this._chkPreviewImportedFeeds);
            this.Controls.Add(this._lblDownloading);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._btnBrowse);
            this.Controls.Add(this._edtOPMLName);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "ImportSubscriptionsDlg";
            this.Text = "Import Feed Subscriptions";
            this.ResumeLayout(false);

        }

	    #endregion

        private void _btnBrowse_Click( object sender, System.EventArgs e )
        {
            if ( _dlgOpenFile.ShowDialog( this ) == DialogResult.OK )
            {
                _edtOPMLName.Text = _dlgOpenFile.FileName;
            }
        }

        private void _btnOK_Click(object sender, System.EventArgs e)
        {
            _edtOPMLName.Enabled = false;
            _btnOK.Enabled = false;
            
            try
            {
                if ( File.Exists( FileName ) )
                {
                    _importStream = new FileStream( FileName, FileMode.Open, FileAccess.Read, FileShare.Read );
                }
                else
                {
                    _lblDownloading.Visible = true;
                    Application.DoEvents();
                    WebClient client = new WebClient();
                    byte[] data = client.DownloadData( FileName );
                    _importStream = new JetMemoryStream( data, true );
                    _lblDownloading.Visible = false;
                }
            }
            catch( Exception ex )
            {
                MessageBox.Show( this,
                    "Error loading subscription list: " + ex.Message,
                    "Import Subscription List" );

                _edtOPMLName.Enabled = true;
                _btnOK.Enabled = true;
                _lblDownloading.Visible = false;
                return;
            }
            DialogResult = DialogResult.OK;
        }

        private void _edtOPMLName_TextChanged( object sender, System.EventArgs e ) 
        {
            _btnOK.Enabled = (_edtOPMLName.Text.Length > 0);
        }

        public string FileName
        {
            get { return _edtOPMLName.Text; }
        }

	    public Stream ImportStream
	    {
	        get { return _importStream; }
	    }

	    public bool ImportPreview
	    {
            get { return _chkPreviewImportedFeeds.Checked; }
	    }
	}
}
