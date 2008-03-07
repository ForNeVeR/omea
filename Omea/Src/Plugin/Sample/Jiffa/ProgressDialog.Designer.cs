/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Omea.Jiffa
{
	partial class ProgressDialog
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this._labelStatus = new System.Windows.Forms.Label();
			this._progress = new System.Windows.Forms.ProgressBar();
			this._btnCancel = new System.Windows.Forms.Button();
			this._image = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this._image)).BeginInit();
			this.SuspendLayout();
			// 
			// _labelStatus
			// 
			this._labelStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._labelStatus.Location = new System.Drawing.Point(13, 33);
			this._labelStatus.Name = "_labelStatus";
			this._labelStatus.Size = new System.Drawing.Size(348, 65);
			this._labelStatus.TabIndex = 0;
			this._labelStatus.Text = "Initializing";
			this._labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _progress
			// 
			this._progress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._progress.Location = new System.Drawing.Point(13, 101);
			this._progress.Name = "_progress";
			this._progress.Size = new System.Drawing.Size(277, 13);
			this._progress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this._progress.TabIndex = 1;
			// 
			// _btnCancel
			// 
			this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnCancel.Enabled = false;
			this._btnCancel.Location = new System.Drawing.Point(298, 99);
			this._btnCancel.Name = "_btnCancel";
			this._btnCancel.Size = new System.Drawing.Size(63, 23);
			this._btnCancel.TabIndex = 2;
			this._btnCancel.Text = "Cancel";
			this._btnCancel.UseVisualStyleBackColor = true;
			// 
			// pictureBox1
			// 
			this._image.InitialImage = null;
			this._image.Location = new System.Drawing.Point(13, 14);
			this._image.Name = "_image";
			this._image.Size = new System.Drawing.Size(16, 16);
			this._image.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this._image.TabIndex = 3;
			this._image.TabStop = false;
			// 
			// ProgressDialog
			// 
			this.AcceptButton = this._btnCancel;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(374, 135);
			this.ControlBox = false;
			this.Controls.Add(this._image);
			this.Controls.Add(this._btnCancel);
			this.Controls.Add(this._progress);
			this.Controls.Add(this._labelStatus);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ProgressDialog";
			this.Padding = new System.Windows.Forms.Padding(10);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "The Operation Is in Progress…";
			((System.ComponentModel.ISupportInitialize)(this._image)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label _labelStatus;
		private System.Windows.Forms.ProgressBar _progress;
		private System.Windows.Forms.Button _btnCancel;
		private System.Windows.Forms.PictureBox _image;
	}
}