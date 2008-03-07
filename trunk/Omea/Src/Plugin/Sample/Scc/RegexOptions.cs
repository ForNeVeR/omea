/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
	/// <summary>
	/// Summary description for RegexOptions.
	/// </summary>
	public class RegexOptions : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _edtHighlightAsLink;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox _edtLinkPointsTo;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public RegexOptions()
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
            this._btnCancel = new System.Windows.Forms.Button();
            this._btnOK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this._edtHighlightAsLink = new System.Windows.Forms.TextBox();
            this._edtLinkPointsTo = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _btnCancel
            // 
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(276, 64);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 3;
            this._btnCancel.Text = "Cancel";
            // 
            // _btnOK
            // 
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(192, 64);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 2;
            this._btnOK.Text = "OK";
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 20);
            this.label1.TabIndex = 4;
            this.label1.Text = "Highlight as link:";
            // 
            // _edtHighlightAsLink
            // 
            this._edtHighlightAsLink.Location = new System.Drawing.Point(112, 4);
            this._edtHighlightAsLink.Name = "_edtHighlightAsLink";
            this._edtHighlightAsLink.Size = new System.Drawing.Size(240, 21);
            this._edtHighlightAsLink.TabIndex = 5;
            this._edtHighlightAsLink.Text = "";
            // 
            // _edtLinkPointsTo
            // 
            this._edtLinkPointsTo.Location = new System.Drawing.Point(112, 32);
            this._edtLinkPointsTo.Name = "_edtLinkPointsTo";
            this._edtLinkPointsTo.Size = new System.Drawing.Size(240, 21);
            this._edtLinkPointsTo.TabIndex = 7;
            this._edtLinkPointsTo.Text = "";
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 20);
            this.label2.TabIndex = 6;
            this.label2.Text = "Link points to:";
            // 
            // RegexOptions
            // 
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(360, 95);
            this.Controls.Add(this._edtLinkPointsTo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._edtHighlightAsLink);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RegexOptions";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Description Link Regex";
            this.ResumeLayout(false);

        }
		#endregion
	    
	    public string RegexMatch
	    {
	        get { return _edtHighlightAsLink.Text; }
	        set { _edtHighlightAsLink.Text = value; }
	    }
	    
	    public string RegexReplace
	    {
	        get { return _edtLinkPointsTo.Text; }
	        set { _edtLinkPointsTo.Text = value; }
	    }
	}
}
