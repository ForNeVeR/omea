/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
	internal class HeadersViewer : DialogBase
	{
        private System.Windows.Forms.TextBox _headerBox;
        private System.Windows.Forms.Button _closeBtn;
		private System.ComponentModel.Container components = null;

		private HeadersViewer()
		{
			InitializeComponent();
		}

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
            this._headerBox = new System.Windows.Forms.TextBox();
            this._closeBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _headerBox
            // 
            this._headerBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._headerBox.Location = new System.Drawing.Point(8, 9);
            this._headerBox.Multiline = true;
            this._headerBox.Name = "_headerBox";
            this._headerBox.ReadOnly = true;
            this._headerBox.Size = new System.Drawing.Size(616, 396);
            this._headerBox.TabIndex = 0;
            this._headerBox.Text = "";
            // 
            // _closeBtn
            // 
            this._closeBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._closeBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._closeBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._closeBtn.Location = new System.Drawing.Point(550, 414);
            this._closeBtn.Name = "_closeBtn";
            this._closeBtn.Size = new System.Drawing.Size(75, 25);
            this._closeBtn.TabIndex = 1;
            this._closeBtn.Text = "Close";
            this._closeBtn.Click += new System.EventHandler(this._closeBtn_Click);
            // 
            // HeadersViewer
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._closeBtn;
            this.ClientSize = new System.Drawing.Size(632, 446);
            this.Controls.Add(this._closeBtn);
            this.Controls.Add(this._headerBox);
            this.Name = "HeadersViewer";
            this.Text = "";
            this.ResumeLayout(false);

        }
		#endregion

        internal static void ViewHeaders( IResource article )
        {
            HeadersViewer form = new HeadersViewer();
            form.RestoreSettings();
            form._headerBox.Text = article.GetPropText( NntpPlugin._propArticleHeaders );
            form.Text = "Headers for " + article.DisplayName;
            form._headerBox.SelectionLength = 0;
            form.Show();
        }

        private void _closeBtn_Click(object sender, System.EventArgs e)
        {
            Close();
        }
	}
}
