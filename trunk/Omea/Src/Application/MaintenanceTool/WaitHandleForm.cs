/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Threading;

namespace JetBrains.Omea.Maintenance
{
	/// <summary>
	/// Summary description for WaitHandleForm.
	/// </summary>
	internal class WaitHandleForm : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Label _prompt;
        private System.Windows.Forms.Button _cancelBtn;
        private System.Windows.Forms.Button _okButton;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.Timer _timer;

        private WaitHandle _h;

		public WaitHandleForm( WaitHandle h, string prompt )
		{
			InitializeComponent();
            _prompt.Text = prompt;
            _h = h;
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
            this.components = new System.ComponentModel.Container();
            this._prompt = new System.Windows.Forms.Label();
            this._cancelBtn = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this._timer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // _prompt
            // 
            this._prompt.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._prompt.Location = new System.Drawing.Point(12, 16);
            this._prompt.Name = "_prompt";
            this._prompt.Size = new System.Drawing.Size(292, 36);
            this._prompt.TabIndex = 0;
            // 
            // _cancelBtn
            // 
            this._cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelBtn.Location = new System.Drawing.Point(120, 68);
            this._cancelBtn.Name = "_cancelBtn";
            this._cancelBtn.TabIndex = 1;
            this._cancelBtn.Text = "Cancel";
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Location = new System.Drawing.Point(12, 60);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(96, 32);
            this._okButton.TabIndex = 2;
            this._okButton.Text = "FAKE OK";
            this._okButton.Visible = false;
            // 
            // _timer
            // 
            this._timer.Enabled = true;
            this._timer.Tick += new System.EventHandler(this._timer_Tick);
            // 
            // WaitHandleForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(312, 102);
            this.ControlBox = false;
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._cancelBtn);
            this.Controls.Add(this._prompt);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(320, 136);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(320, 136);
            this.Name = "WaitHandleForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Waiting...";
            this.ResumeLayout(false);

        }
		#endregion

        private void _timer_Tick(object sender, System.EventArgs e)
        {
            if( _h.WaitOne( 0, false ) )
            {
                _okButton.Width = 0;
                _okButton.Visible = true;
                _okButton.PerformClick();
            }
        }
	}
}