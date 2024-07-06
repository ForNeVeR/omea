// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace JetBrains.Omea.GUIControls
{
	public class DatePickerCtrl : System.Windows.Forms.UserControl
	{
        private System.Windows.Forms.TextBox _textBox;
        private System.Windows.Forms.Button _btnDate;
        private DateTime _currentDate = DateTime.MinValue;
        private System.Windows.Forms.Button _btnClear;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.ToolTip _toolTip;
        private bool _showClearButton;

        public event EventHandler ValueChanged;

		public DatePickerCtrl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
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

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(DatePickerCtrl));
            this._textBox = new System.Windows.Forms.TextBox();
            this._btnDate = new System.Windows.Forms.Button();
            this._btnClear = new System.Windows.Forms.Button();
            this._toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // _textBox
            //
            this._textBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._textBox.Location = new System.Drawing.Point(0, 0);
            this._textBox.Name = "_textBox";
            this._textBox.ReadOnly = true;
            this._textBox.Size = new System.Drawing.Size(196, 20);
            this._textBox.TabIndex = 0;
            this._textBox.Text = "";
            //
            // _btnDate
            //
            this._btnDate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnDate.Image = ((System.Drawing.Image)(resources.GetObject("_btnDate.Image")));
            this._btnDate.Location = new System.Drawing.Point(200, 0);
            this._btnDate.Name = "_btnDate";
            this._btnDate.Size = new System.Drawing.Size(23, 23);
            this._btnDate.TabIndex = 1;
            this._toolTip.SetToolTip(this._btnDate, "Select Date");
            this._btnDate.Click += new System.EventHandler(this._btnDate_Click);
            //
            // _btnClear
            //
            this._btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnClear.Image = ((System.Drawing.Image)(resources.GetObject("_btnClear.Image")));
            this._btnClear.Location = new System.Drawing.Point(200, 0);
            this._btnClear.Name = "_btnClear";
            this._btnClear.Size = new System.Drawing.Size(23, 23);
            this._btnClear.TabIndex = 2;
            this._toolTip.SetToolTip(this._btnClear, "Clear Date");
            this._btnClear.Visible = false;
            this._btnClear.Click += new System.EventHandler(this._btnClear_Click);
            //
            // DatePickerCtrl
            //
            this.Controls.Add(this._btnClear);
            this.Controls.Add(this._btnDate);
            this.Controls.Add(this._textBox);
            this.Name = "DatePickerCtrl";
            this.Size = new System.Drawing.Size(224, 28);
            this.ResumeLayout(false);

        }
		#endregion

        public DateTime CurrentDate
        {
            get
            {
                return _currentDate;
            }
            set
            {
                _currentDate = value;
                SetControlText();
            }
        }

        [DefaultValue(false)]
        public bool ShowClearButton
        {
            get { return _showClearButton; }
            set
            {
                _showClearButton = value;
                if ( _showClearButton )
                {
                    _btnClear.Visible = true;
                    _btnDate.Left = _btnClear.Left - _btnDate.Width;
                }
                else
                {
                    _btnClear.Visible = false;
                    _btnDate.Left = _btnClear.Left;
                }
                _textBox.Width = _btnDate.Left;
            }
        }

        private void _btnDate_Click(object sender, System.EventArgs e)
        {
            DatePickerDlg dlg = new DatePickerDlg();
            using( dlg )
            {
                if ( _currentDate != DateTime.MinValue )
                {
                    dlg.Date = _currentDate;
                }

                if ( dlg.ShowDialog() == DialogResult.OK )
                {
                    _currentDate = dlg.Date;
                    SetControlText();
                    OnValueChanged();
                }
            }
        }

	    protected virtual void OnValueChanged()
	    {
	        if ( ValueChanged != null )
	        {
	            ValueChanged( this, EventArgs.Empty );
	        }
	    }

	    private void SetControlText()
        {
            if ( _currentDate != DateTime.MinValue )
            {
                _textBox.Text = _currentDate.ToShortDateString();
            }
            else
            {
                _textBox.Text = string.Empty;
            }
        }

        private void _btnClear_Click( object sender, System.EventArgs e )
        {
            CurrentDate = DateTime.MinValue;
            OnValueChanged();
        }
	}
}
