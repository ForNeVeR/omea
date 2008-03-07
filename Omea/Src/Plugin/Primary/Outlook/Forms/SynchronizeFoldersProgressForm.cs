/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
	/// <summary>
	/// Summary description for SynchronizeFoldersProgressForm.
	/// </summary>
	public class SynchronizeFoldersProgressForm : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Label label1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public SynchronizeFoldersProgressForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
            this.Icon = Core.UIManager.ApplicationIcon;

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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(SynchronizeFoldersProgressForm));
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label1.Location = new System.Drawing.Point(66, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(160, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Synchronizing folder structure...";
            // 
            // SynchronizeFoldersProgressForm
            // 
            this.AutoScale = false;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(292, 41);
            this.ControlBox = false;
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "SynchronizeFoldersProgressForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Outlook Plugin";
            this.VisibleChanged += new System.EventHandler(this.SynchronizeFoldersProgressForm_VisibleChanged);
            this.ResumeLayout(false);

        }
		#endregion

        private void SynchronizeFoldersProgressForm_VisibleChanged(object sender, System.EventArgs e)
        {
            VisibleChanged -= new EventHandler( SynchronizeFoldersProgressForm_VisibleChanged );
            Core.UIManager.QueueUIJob( new MethodInvoker( DoSynchronize ) );
        }

	    private void DoSynchronize()
	    {
            OutlookSession.ProcessJobs();
            OutlookSession.OutlookProcessor.RunSynchronizeFolderAndAddressBooks();
            DialogResult = DialogResult.OK;
        }
	}
}
