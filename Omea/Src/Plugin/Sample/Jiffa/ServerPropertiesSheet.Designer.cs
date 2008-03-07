/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Omea.Jiffa
{
	partial class ServerPropertiesSheet
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
			this._propsheet = new System.Windows.Forms.PropertyGrid();
			this.SuspendLayout();
			// 
			// _propsheet
			// 
			this._propsheet.Dock = System.Windows.Forms.DockStyle.Fill;
			this._propsheet.Location = new System.Drawing.Point(0, 0);
			this._propsheet.Name = "_propsheet";
			this._propsheet.Size = new System.Drawing.Size(499, 292);
			this._propsheet.TabIndex = 0;
			// 
			// ServerPropertiesSheet
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(499, 292);
			this.Controls.Add(this._propsheet);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "ServerPropertiesSheet";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "ServerPropertiesSheet";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PropertyGrid _propsheet;
	}
}