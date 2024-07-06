// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;
using JetBrains.Omea.TextIndex;

namespace JetBrains.Omea
{
	/// <summary>
	/// Summary description for DiskSpaceExhaustedForm.
	/// </summary>
	public class DiskSpaceExhaustedForm : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Button _closeButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label _diskSpaceLabel;
        private System.Windows.Forms.Timer _timer;
        private System.Windows.Forms.Label _alertText;
        private System.ComponentModel.IContainer components;

        private const int   BytesUnit = 1024 * 1024;
        private const int   SpaceMarginInMB = 10;

		public DiskSpaceExhaustedForm()
		{
			InitializeComponent();
            this.Icon = Core.UIManager.ApplicationIcon;
            _alertText.Text = _alertText.Text.Replace( "[product name]", Core.ProductName );
		    UpdateDiskSpaceLabel();
		}

        public static void StartMonitoring()
        {
            Core.ResourceAP.QueueJobAt( DateTime.Now.AddMinutes( 1 ), new MethodInvoker( CheckFreeSpace ) );
        }

	    private void UpdateDiskSpaceLabel()
	    {
	        ulong userFree = IOTools.DiskFreeSpaceForUserDB( OMEnv.WorkDir );
	        NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalDigits = 1;
            double megaBytes = (double) userFree / BytesUnit;
            _diskSpaceLabel.Text = megaBytes.ToString( "N", nfi ) + "M";
	    }

        private static void CheckFreeSpace()
        {
            ulong diskFreeSpace;
            try
            {
                diskFreeSpace = IOTools.DiskFreeSpaceForUserDB( OMEnv.WorkDir );
            }
            catch( IOException )
            {
                StartMonitoring();
                return;
            }

            if( diskFreeSpace > SpaceMarginInMB * BytesUnit )
            {
                StartMonitoring();
            }
            else
            {
                DiskSpaceExhaustedForm form = new DiskSpaceExhaustedForm();
                form.ShowDialog( Core.MainWindow );
            }
        }

        /// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
                _timer.Dispose();
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
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(DiskSpaceExhaustedForm));
            this._alertText = new System.Windows.Forms.Label();
            this._closeButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this._diskSpaceLabel = new System.Windows.Forms.Label();
            this._timer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            //
            // _alertText
            //
            this._alertText.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._alertText.Location = new System.Drawing.Point(8, 8);
            this._alertText.Name = "_alertText";
            this._alertText.Size = new System.Drawing.Size(308, 80);
            this._alertText.TabIndex = 0;
            this._alertText.Text = @"[product name] has encountered dangerously low free hard disk space and should be closed in order to avoid data corruption in Resource Store and Text Index. Please close all open [product name] windows, finish [product name], and clean drive, where [product name] database is placed.";
            //
            // _closeButton
            //
            this._closeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._closeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._closeButton.Location = new System.Drawing.Point(126, 128);
            this._closeButton.Name = "_closeButton";
            this._closeButton.TabIndex = 1;
            this._closeButton.Text = "OK";
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 96);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(128, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Current Free Space:";
            //
            // _diskSpaceLabel
            //
            this._diskSpaceLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._diskSpaceLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._diskSpaceLabel.Location = new System.Drawing.Point(136, 96);
            this._diskSpaceLabel.Name = "_diskSpaceLabel";
            this._diskSpaceLabel.Size = new System.Drawing.Size(180, 20);
            this._diskSpaceLabel.TabIndex = 3;
            this._diskSpaceLabel.Text = "100 KB";
            //
            // _timer
            //
            this._timer.Enabled = true;
            this._timer.Interval = 1000;
            this._timer.Tick += new System.EventHandler(this._timer_Tick);
            //
            // DiskSpaceExhaustedForm
            //
            this.AcceptButton = this._closeButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(318, 160);
            this.Controls.Add(this._diskSpaceLabel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._closeButton);
            this.Controls.Add(this._alertText);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(328, 200);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(328, 196);
            this.Name = "DiskSpaceExhaustedForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Warning: Low Free Disk Space";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.DiskSpaceExhaustedForm_Closing);
            this.ResumeLayout(false);

        }
		#endregion

        private void _timer_Tick(object sender, System.EventArgs e)
        {
            UpdateDiskSpaceLabel();
        }

        private void DiskSpaceExhaustedForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StartMonitoring();
        }
	}
}
