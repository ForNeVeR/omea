/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.ExceptionReport;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Dialog for showing and submitting exceptions that occurred during background processing.
	/// </summary>
	public class BackgroundExceptionDlg : System.Windows.Forms.Form
	{
        private System.Windows.Forms.ListView _lvExceptions;
        private System.Windows.Forms.TextBox _edtDetails;
        private System.Windows.Forms.Button _btnSubmit;
        private System.Windows.Forms.Button _btnClear;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.ColumnHeader columnHeader1;

        private ArrayList _backgroundExceptions;
        private System.Windows.Forms.Label _lblStatus;
        private string _excDescription;

		public BackgroundExceptionDlg()
		{
			InitializeComponent();
            this.Icon = Core.UIManager.ApplicationIcon;
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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(BackgroundExceptionDlg));
            this._lvExceptions = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this._edtDetails = new System.Windows.Forms.TextBox();
            this._btnSubmit = new System.Windows.Forms.Button();
            this._btnClear = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this._lblStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _lvExceptions
            // 
            this._lvExceptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lvExceptions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                            this.columnHeader1});
            this._lvExceptions.FullRowSelect = true;
            this._lvExceptions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._lvExceptions.HideSelection = false;
            this._lvExceptions.Location = new System.Drawing.Point(4, 20);
            this._lvExceptions.Name = "_lvExceptions";
            this._lvExceptions.Size = new System.Drawing.Size(428, 112);
            this._lvExceptions.TabIndex = 0;
            this._lvExceptions.View = System.Windows.Forms.View.Details;
            this._lvExceptions.SelectedIndexChanged += new System.EventHandler(this._lvExceptions_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = 300;
            // 
            // _edtDetails
            // 
            this._edtDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtDetails.Location = new System.Drawing.Point(4, 156);
            this._edtDetails.Multiline = true;
            this._edtDetails.Name = "_edtDetails";
            this._edtDetails.ReadOnly = true;
            this._edtDetails.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this._edtDetails.Size = new System.Drawing.Size(432, 84);
            this._edtDetails.TabIndex = 1;
            this._edtDetails.Text = "";
            // 
            // _btnSubmit
            // 
            this._btnSubmit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnSubmit.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnSubmit.Location = new System.Drawing.Point(276, 248);
            this._btnSubmit.Name = "_btnSubmit";
            this._btnSubmit.TabIndex = 2;
            this._btnSubmit.Text = "Submit";
            this._btnSubmit.Click += new System.EventHandler(this._btnSubmit_Click);
            // 
            // _btnClear
            // 
            this._btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnClear.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnClear.Location = new System.Drawing.Point(360, 248);
            this._btnClear.Name = "_btnClear";
            this._btnClear.TabIndex = 3;
            this._btnClear.Text = "Clear";
            this._btnClear.Click += new System.EventHandler(this._btnClear_Click);
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 16);
            this.label1.TabIndex = 5;
            this.label1.Text = "Error List:";
            // 
            // label4
            // 
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Location = new System.Drawing.Point(4, 136);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 16);
            this.label4.TabIndex = 6;
            this.label4.Text = "Technical Details:";
            // 
            // _lblStatus
            // 
            this._lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._lblStatus.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblStatus.Location = new System.Drawing.Point(4, 248);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(224, 16);
            this._lblStatus.TabIndex = 7;
            // 
            // BackgroundExceptionDlg
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(440, 275);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._btnClear);
            this.Controls.Add(this._btnSubmit);
            this.Controls.Add(this._edtDetails);
            this.Controls.Add(this._lvExceptions);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "BackgroundExceptionDlg";
            this.ShowInTaskbar = false;
            this.Text = "Error Report";
            this.ResumeLayout(false);

        }
		#endregion

        public void ShowBackgroundExceptionDialog( ArrayList backgroundExceptions, string excDescription )
        {
            _excDescription = excDescription;
            _backgroundExceptions = backgroundExceptions;
            _lvExceptions.Columns [0].Width = _lvExceptions.Width - 8;
            foreach( Exception e in backgroundExceptions )
            {
                ListViewItem lvItem = _lvExceptions.Items.Add( e.Message );
                lvItem.Tag = e;
            }
            if ( _lvExceptions.Items.Count > 0 )
            {
                _lvExceptions.Items [0].Selected = true;
            }
            ShowDialog( Core.MainWindow );
        }

        private void _lvExceptions_SelectedIndexChanged( object sender, System.EventArgs e )
        {
            UpdateSelectedException();        
        }

	    private void UpdateSelectedException()
	    {
            if ( _lvExceptions.SelectedItems.Count > 0 )
            {
                Exception selException = (Exception) _lvExceptions.SelectedItems [0].Tag;
                _edtDetails.Text = selException.ToString();
                _btnSubmit.Enabled = true;
            }
            else
            {
                _edtDetails.Text = "";
                _btnSubmit.Enabled = false;
            }
	    }

        private void _btnClear_Click(object sender, System.EventArgs e)
        {
            _backgroundExceptions.Clear();
            DialogResult = DialogResult.OK;
        }

        private void _btnSubmit_Click( object sender, System.EventArgs e )
        {
            string userName = Core.SettingStore.ReadString( "ErrorReport", "UserName" );
            string password = Core.SettingStore.ReadString( "ErrorReport", "Password" );
            if ( userName.Length == 0 || password.Length == 0 )
            {
                userName = "om_anonymous";
                password = "guest";
            }

            SubmissionResult result = null;

            IExceptionSubmitter submitter = new RPCExceptionSubmitter();
            submitter.SubmitProgress += new SubmitProgressEventHandler( OnSubmitProgress );
            foreach( ListViewItem lvItem in _lvExceptions.SelectedItems )
            {
                Exception ex = (Exception) lvItem.Tag;
                try
                {
                    result = submitter.SubmitException( ex, _excDescription, userName, password, 
                                                        Assembly.GetExecutingAssembly().GetName().Version.Build, WebProxy.GetDefaultProxy() );
                }
                catch( Exception ex1 )
                {
                    MessageBox.Show( this, "Failed to submit exception: " + ex1.Message, Core.ProductFullName );
                    continue;
                }
                _backgroundExceptions.Remove( ex );
            }
            for( int i=_lvExceptions.SelectedItems.Count-1; i >= 0; i-- )
            {
                _lvExceptions.Items.Remove( _lvExceptions.SelectedItems [i] );
            }

            if ( result != null )
            {
                ExceptionReportForm.ShowSubmissionResult( this, result, "OM" );
            }
            if ( _backgroundExceptions.Count == 0 )
            {
                DialogResult = DialogResult.OK;
            }
        }

	    private void OnSubmitProgress( object sender, SubmitProgressEventArgs e )
	    {
	        _lblStatus.Text = e.Message;
            Application.DoEvents();
	    }
	}
}
