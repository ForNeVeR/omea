// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Summary description for ShowBlobAsPicture.
	/// </summary>
	public class ShowBlobAsPicture : System.Windows.Forms.Form
	{
        private System.Windows.Forms.PictureBox _image;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ShowBlobAsPicture( Stream stream )
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
            _image.Image = Image.FromStream( stream );
            _image.SizeMode = PictureBoxSizeMode.AutoSize;
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
            this._image = new System.Windows.Forms.PictureBox();
            this.SuspendLayout();
            //
            // _image
            //
            this._image.Dock = System.Windows.Forms.DockStyle.Fill;
            this._image.Location = new System.Drawing.Point(0, 0);
            this._image.Name = "_image";
            this._image.Size = new System.Drawing.Size(444, 294);
            this._image.TabIndex = 0;
            this._image.TabStop = false;
            //
            // ShowBlobAsPicture
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(444, 294);
            this.Controls.Add(this._image);
            this.Name = "ShowBlobAsPicture";
            this.Text = "ShowBlobAsPicture";
            this.ResumeLayout(false);

        }
		#endregion
	}
}
