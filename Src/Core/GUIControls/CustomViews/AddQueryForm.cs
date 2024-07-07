// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Categories;

namespace JetBrains.Omea.GUIControls
{
	public class EnterValueForm : System.Windows.Forms.Form
	{
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.Button cancelButton;
        private string ResultText;

		public EnterValueForm( string dialogTitle, string defaultValue )
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
            this.Text = dialogTitle;
            this.textBox.Text = defaultValue;
            ResultText = "";
            okButton.Enabled = (defaultValue != null) && (defaultValue.Length > 0);
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
            this.textBox = new System.Windows.Forms.TextBox();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // textBox
            //
            this.textBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox.Location = new System.Drawing.Point(8, 8);
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(400, 21);
            this.textBox.TabIndex = 0;
            this.textBox.Text = "";
            this.textBox.TextChanged += new System.EventHandler(this.textBox_TextChanged);
            //
            // okButton
            //
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.okButton.Location = new System.Drawing.Point(248, 36);
            this.okButton.Name = "okButton";
            this.okButton.TabIndex = 1;
            this.okButton.Text = "OK";
            this.okButton.Click += new System.EventHandler(this.OnClick);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelButton.Location = new System.Drawing.Point(332, 36);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            //
            // EnterValueForm
            //
            this.AcceptButton = this.okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(415, 65);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.textBox);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(600, 120);
            this.MinimizeBox = false;
            this.Name = "EnterValueForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Enter Search Query";
            this.ResumeLayout(false);

        }
		#endregion

        public string SearchQueryString
        {
            get{ return ResultText; }
        }

        private void OnClick(object sender, System.EventArgs e)
        {
            ResultText = textBox.Text;
            DialogResult = DialogResult.OK;
        }

        private void textBox_TextChanged(object sender, System.EventArgs e)
        {
            okButton.Enabled = (textBox.Text.Length > 0);
        }
	}
}
