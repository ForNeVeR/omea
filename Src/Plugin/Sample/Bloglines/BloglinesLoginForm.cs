// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace JetBrains.Omea.SamplePlugins.BloglinesPlugin
{
	/// <summary>
	/// Summary description for BloglinesLoginForm.
	/// </summary>
	internal class ImportBloglinesSubscription : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxLogin;
		private System.Windows.Forms.TextBox textBoxPassword;
		private System.Windows.Forms.CheckBox checkBoxPreview;
		private System.Windows.Forms.Button buttonImport;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private string _login = null;
		private string _passw = null;
		private System.Windows.Forms.Button buttonCancel;
		private bool _preview = false;

		internal string Login { get { return _login; } }
		internal string Password { get { return _passw; } }
		internal bool NeedPreview { get { return _preview; } }

		public ImportBloglinesSubscription(string login, string passwd, bool needPreview)
		{
			_login = login;
			_passw = passwd;
			_preview = needPreview;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			textBoxLogin.Text = _login;
			textBoxPassword.Text = _passw;
			checkBoxPreview.Checked = _preview;
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
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxLogin = new System.Windows.Forms.TextBox();
			this.textBoxPassword = new System.Windows.Forms.TextBox();
			this.checkBoxPreview = new System.Windows.Forms.CheckBox();
			this.buttonImport = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// label1
			//
			this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(88, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "Bloglines &Login:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			//
			// label2
			//
			this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label2.Location = new System.Drawing.Point(24, 32);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(72, 16);
			this.label2.TabIndex = 2;
			this.label2.Text = "&Password:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			//
			// textBoxLogin
			//
			this.textBoxLogin.Location = new System.Drawing.Point(104, 8);
			this.textBoxLogin.Name = "textBoxLogin";
			this.textBoxLogin.Size = new System.Drawing.Size(264, 20);
			this.textBoxLogin.TabIndex = 1;
			this.textBoxLogin.Text = "";
			//
			// textBoxPassword
			//
			this.textBoxPassword.Location = new System.Drawing.Point(104, 32);
			this.textBoxPassword.Name = "textBoxPassword";
			this.textBoxPassword.PasswordChar = '*';
			this.textBoxPassword.Size = new System.Drawing.Size(264, 20);
			this.textBoxPassword.TabIndex = 3;
			this.textBoxPassword.Text = "";
			//
			// checkBoxPreview
			//
			this.checkBoxPreview.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.checkBoxPreview.Location = new System.Drawing.Point(104, 56);
			this.checkBoxPreview.Name = "checkBoxPreview";
			this.checkBoxPreview.Size = new System.Drawing.Size(264, 24);
			this.checkBoxPreview.TabIndex = 4;
			this.checkBoxPreview.Text = "Preview &feed list before importing";
			//
			// buttonImport
			//
			this.buttonImport.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonImport.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonImport.Location = new System.Drawing.Point(208, 88);
			this.buttonImport.Name = "buttonImport";
			this.buttonImport.TabIndex = 5;
			this.buttonImport.Text = "&Import";
			this.buttonImport.Click += new System.EventHandler(this.buttonImport_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonCancel.Location = new System.Drawing.Point(296, 88);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.TabIndex = 6;
			this.buttonCancel.Text = "&Cancel";
			//
			// ImportBloglinesSubscription
			//
			this.AcceptButton = this.buttonImport;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(378, 119);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonImport);
			this.Controls.Add(this.checkBoxPreview);
			this.Controls.Add(this.textBoxPassword);
			this.Controls.Add(this.textBoxLogin);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Font = new System.Drawing.Font("Tahoma", 8F);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ImportBloglinesSubscription";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Import Bloglines Subscription";
			this.ResumeLayout(false);

		}
		#endregion

		private void buttonImport_Click(object sender, System.EventArgs e)
		{
			_login = textBoxLogin.Text;
			_passw = textBoxPassword.Text;
			_preview = checkBoxPreview.Checked;
		}
	}
}
