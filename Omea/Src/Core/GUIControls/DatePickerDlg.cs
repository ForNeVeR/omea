// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Omea.GUIControls
{
	public class DatePickerDlg : System.Windows.Forms.Form
	{
		private System.Windows.Forms.MonthCalendar _monthCalendar;
		private System.Windows.Forms.Button _btnOK;
		private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Label label1;
		private System.ComponentModel.Container components = null;

		public DatePickerDlg()
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
            this._monthCalendar = new System.Windows.Forms.MonthCalendar();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // _monthCalendar
            //
            this._monthCalendar.Location = new System.Drawing.Point(0, 32);
            this._monthCalendar.MaxSelectionCount = 1;
            this._monthCalendar.Name = "_monthCalendar";
            this._monthCalendar.ShowToday = false;
            this._monthCalendar.TabIndex = 1;
            this._monthCalendar.DateChanged += new System.Windows.Forms.DateRangeEventHandler(this._monthCalendar_DateChanged);
            //
            // _btnOK
            //
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(16, 192);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 2;
            this._btnOK.Text = "OK";
            //
            // _btnCancel
            //
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(104, 192);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 3;
            this._btnCancel.Text = "Cancel";
            //
            // label1
            //
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(192, 32);
            this.label1.TabIndex = 4;
            this.label1.Text = "To change month or year click on it and choose appropriate value.";
            //
            // DatePickerDlg
            //
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(192, 224);
            this.ControlBox = false;
            this.Controls.Add(this.label1);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._monthCalendar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "DatePickerDlg";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select date";
            this.ResumeLayout(false);

        }
		#endregion

        public DateTime Date
        {
            get
            {
                return _monthCalendar.SelectionStart;
            }
            set
            {
                _monthCalendar.SetDate( value );
            }
        }
		private void _monthCalendar_DateChanged(object sender, System.Windows.Forms.DateRangeEventArgs e)
		{
			//Close();
		}
	}
}
