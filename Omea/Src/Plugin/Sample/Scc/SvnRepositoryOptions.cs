/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
	/// <summary>
	/// Form for editing Subversion repository options.
	/// </summary>
	public class SvnRepositoryOptions : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _edtName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox _edtUrl;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.TextBox _edtUserName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox _edtPassword;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public SvnRepositoryOptions()
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
            this._edtName = new System.Windows.Forms.TextBox();
            this._edtUrl = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this._btnCancel = new System.Windows.Forms.Button();
            this._btnOK = new System.Windows.Forms.Button();
            this._edtUserName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this._edtPassword = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(4, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name:";
            // 
            // _edtName
            // 
            this._edtName.Location = new System.Drawing.Point(112, 4);
            this._edtName.Name = "_edtName";
            this._edtName.Size = new System.Drawing.Size(168, 21);
            this._edtName.TabIndex = 0;
            this._edtName.Text = "textBox1";
            // 
            // _edtUrl
            // 
            this._edtUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtUrl.Location = new System.Drawing.Point(112, 28);
            this._edtUrl.Name = "_edtUrl";
            this._edtUrl.Size = new System.Drawing.Size(320, 21);
            this._edtUrl.TabIndex = 1;
            this._edtUrl.Text = "textBox2";
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(4, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Repository URL:";
            // 
            // _btnCancel
            // 
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(356, 116);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 5;
            this._btnCancel.Text = "Cancel";
            // 
            // _btnOK
            // 
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(272, 116);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 4;
            this._btnOK.Text = "OK";
            // 
            // _edtUserName
            // 
            this._edtUserName.Location = new System.Drawing.Point(112, 56);
            this._edtUserName.Name = "_edtUserName";
            this._edtUserName.Size = new System.Drawing.Size(168, 21);
            this._edtUserName.TabIndex = 2;
            this._edtUserName.Text = "";
            // 
            // label3
            // 
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(4, 60);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 20);
            this.label3.TabIndex = 6;
            this.label3.Text = "User name:";
            // 
            // _edtPassword
            // 
            this._edtPassword.Location = new System.Drawing.Point(112, 84);
            this._edtPassword.Name = "_edtPassword";
            this._edtPassword.PasswordChar = '*';
            this._edtPassword.Size = new System.Drawing.Size(168, 21);
            this._edtPassword.TabIndex = 3;
            this._edtPassword.Text = "";
            // 
            // label4
            // 
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Location = new System.Drawing.Point(4, 88);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 20);
            this.label4.TabIndex = 8;
            this.label4.Text = "Password:";
            // 
            // SvnRepositoryOptions
            // 
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(440, 147);
            this.Controls.Add(this._edtPassword);
            this.Controls.Add(this._edtUserName);
            this.Controls.Add(this._edtUrl);
            this.Controls.Add(this._edtName);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SvnRepositoryOptions";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Subversion Repository Options";
            this.ResumeLayout(false);

        }
		#endregion

	    public DialogResult EditRepository( IWin32Window ownerWindow, IResource repository )
	    {
	        _edtName.Text = repository.GetPropText( Core.Props.Name );
	        _edtUrl.Text = repository.GetPropText( Props.RepositoryUrl );
            _edtUserName.Text = repository.GetPropText( Props.UserName );
            _edtPassword.Text = repository.GetPropText( Props.Password );
	        DialogResult dr = ShowDialog( ownerWindow );
	        if ( dr == DialogResult.OK )
	        {
	            ResourceProxy proxy = new ResourceProxy( repository );
	            proxy.BeginUpdate();
	            proxy.SetProp( Core.Props.Name, _edtName.Text );
	            proxy.SetProp( Props.RepositoryUrl, _edtUrl.Text );
	            proxy.SetProp( Props.UserName, _edtUserName.Text );
	            proxy.SetProp( Props.Password, _edtPassword.Text );
	            proxy.EndUpdate();
	        }
	        return dr;
	    }
	}
}
