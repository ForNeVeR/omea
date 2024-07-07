// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.IO;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Summary description for ShowBlobAsText.
	/// </summary>
	public class ShowBlobAsText : System.Windows.Forms.Form
	{
        private System.Windows.Forms.RichTextBox _richTextBox;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ShowBlobAsText( Stream stream )
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
            _richTextBox.Text = Utils.StreamToString( stream );
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
            this._richTextBox = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            //
            // _richTextBox
            //
            this._richTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._richTextBox.Location = new System.Drawing.Point(0, 0);
            this._richTextBox.Name = "_richTextBox";
            this._richTextBox.Size = new System.Drawing.Size(416, 338);
            this._richTextBox.TabIndex = 0;
            this._richTextBox.Text = "";
            //
            // ShowBlobAsText
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(416, 338);
            this.Controls.Add(this._richTextBox);
            this.Name = "ShowBlobAsText";
            this.Text = "ShowBlobAsText";
            this.ResumeLayout(false);

        }
		#endregion
	}
}
