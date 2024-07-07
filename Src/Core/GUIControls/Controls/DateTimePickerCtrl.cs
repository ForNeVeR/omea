// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	public class DateTimePickerCtrl : UserControl
	{
        private System.ComponentModel.IContainer components;

        private DateTime _currentDateTime = DateTime.MinValue;
        private System.Windows.Forms.Button _btnClear;
        private System.Windows.Forms.Label _textBox;
        private System.Windows.Forms.DateTimePicker _dateTimePicker;
        private System.Windows.Forms.ToolTip _toolTip;
        private System.Windows.Forms.TextBox _fakeTextBox;
        private System.Windows.Forms.ComboBox _comboTime;

        private bool _showClearButton = true;
        private bool _autoShowTimeOnActivation = true;
        private bool _timeParsingOk = true;
        public event ValidStateEventHandler ValidStateChanged;

		public DateTimePickerCtrl()
		{
			InitializeComponent();
            DateTime time = DateTime.MinValue;
            time = time.AddHours( 8 );
            for( int i = 0; i < 25; i++ )
            {
                _comboTime.Items.Add( time.ToShortTimeString() );
                time = time.AddMinutes( 30 );
            }
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

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(DateTimePickerCtrl));
            this._btnClear = new System.Windows.Forms.Button();
            this._textBox = new System.Windows.Forms.Label();
            this._dateTimePicker = new System.Windows.Forms.DateTimePicker();
            this._toolTip = new System.Windows.Forms.ToolTip(this.components);
            this._fakeTextBox = new System.Windows.Forms.TextBox();
            this._comboTime = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            //
            // _textBox
            //
            this._textBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._textBox.Location = new System.Drawing.Point(2, 2);
            this._textBox.Name = "_textBox";
            this._textBox.Size = new System.Drawing.Size(80, 16);
            this._textBox.TabIndex = 4;
            this._toolTip.SetToolTip(this._textBox, "Click to edit date and time");
            this._textBox.Click += new System.EventHandler(this._textBox_Click);
            this._textBox.BackColor = SystemColors.Menu;
            //
            // _dateTimePicker
            //
            this._dateTimePicker.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._dateTimePicker.CustomFormat = "MMM dd, yyyy";
            this._dateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this._dateTimePicker.Location = new System.Drawing.Point(0, 0);
            this._dateTimePicker.Name = "_dateTimePicker";
            this._dateTimePicker.Size = new System.Drawing.Size(80, 20);
            this._dateTimePicker.TabIndex = 1;
            this._dateTimePicker.Enter += new System.EventHandler(this._dateTimePicker_Enter);
            this._dateTimePicker.KeyDown += new System.Windows.Forms.KeyEventHandler(this._dateTimePicker_KeyDown);
            this._dateTimePicker.ValueChanged += new EventHandler(_dateTimePicker_ValueChanged);
            this._toolTip.SetToolTip(this._dateTimePicker, "Click to edit date and time");
            //
            // _comboTime
            //
            this._comboTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this._comboTime.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._comboTime.Location = new System.Drawing.Point(72, 0);
            this._comboTime.Name = "_comboTime";
            this._comboTime.Size = new System.Drawing.Size(70, 21);
            this._comboTime.TabIndex = 2;
            this._comboTime.Enabled = false;
            this._comboTime.MaxDropDownItems = 8;
            this._comboTime.TextChanged += new EventHandler(_comboTime_TextChanged);
            //
            // _btnClear
            //
            this._btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnClear.Image = ((System.Drawing.Image)(resources.GetObject("_btnClear.Image")));
            this._btnClear.Location = new System.Drawing.Point(233, 0);
            this._btnClear.Name = "_btnClear";
            this._btnClear.Size = new System.Drawing.Size(21, 21);
            this._btnClear.TabIndex = 3;
            this._toolTip.SetToolTip(this._btnClear, "Clear date and time");
            this._btnClear.Visible = false;
            this._btnClear.Click += new System.EventHandler(this._btnClear_Click);
            //
            // _fakeTextBox
            //
            this._fakeTextBox.Location = new System.Drawing.Point(0, 28);
            this._fakeTextBox.Name = "_fakeTextBox";
            this._fakeTextBox.ReadOnly = true;
            this._fakeTextBox.TabIndex = 4;
            this._fakeTextBox.TabStop = false;
            this._fakeTextBox.Text = "";
            //
            // DateTimePickerCtrl
            //
            this.Controls.Add(this._fakeTextBox);
            this.Controls.Add(this._textBox);
            this.Controls.Add(this._dateTimePicker);
            this.Controls.Add(this._btnClear);
            this.Controls.Add(this._comboTime);
            this.VisibleChanged += new EventHandler(OnFormVisibleChanged);
            this.EnabledChanged += new EventHandler(ControlEnabledChanged);
            this.Name = "DateTimePickerCtrl";
            this.Size = new System.Drawing.Size(254, 28);
            this.ResumeLayout(false);
        }
		#endregion

        [DefaultValue(false)]
        public bool ShowClearButton
        {
            get { return _showClearButton;  }
            set { _showClearButton = value; _btnClear.Visible = value; }
        }

        public bool AutoSetTime
        {
            get { return _autoShowTimeOnActivation;  }
            set { _autoShowTimeOnActivation = value; }
        }

        public DateTime CurrentDateTime
        {
            get
            {
                string timeStr = _comboTime.Text;
                DateTime time = new DateTime();
                if( timeStr.Length > 0 )
                    time = DateTime.Parse( timeStr );

                _currentDateTime = _currentDateTime.Date.AddHours( time.Hour ).AddMinutes( time.Minute );
                return _currentDateTime;
            }
            set
            {
                _currentDateTime = value.Date.AddHours( value.Hour ).AddMinutes( value.Minute );
                SetControlText();
                bool enabled = value > DateTime.MinValue;
                if( !enabled )
                {
                    _fakeTextBox.Focus();
                }
                _btnClear.Enabled = enabled;
            }
        }

        private void OnFormVisibleChanged( object sender, EventArgs e )
        {
            if( Visible )
            {
                if( ShowClearButton )
                {
                    _btnClear.Left = Width - _btnClear.Width;
                    _comboTime.Left = Width - _comboTime.Width - _btnClear.Width - 5;
                    _dateTimePicker.Width = Width - _btnClear.Width - _comboTime.Width - 10;
                }
                else
                {
                    _comboTime.Left = Width - _btnClear.Width - 5;
                    _dateTimePicker.Width = Width - _comboTime.Width - 10;
                }
                _textBox.Width = _dateTimePicker.Width - 22;
            }
        }

        private void _btnClear_Click(object sender, System.EventArgs e)
        {
            Core.UserInterfaceAP.QueueJob( new MethodInvoker( ResetDateTime ) );
            _btnClear.Focus();
        }

	    private void _dateTimePicker_Enter(object sender, System.EventArgs e)
        {
            StartEditDateTime();
        }

        private void _textBox_Click(object sender, System.EventArgs e)
        {
            StartEditDateTime();
        }

        private void _dateTimePicker_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if( e.KeyCode == Keys.Space && !e.Alt && !e.Control && !e.Shift )
            {
                e.Handled = true;
                ResetDateTime();
            }
            else
            if( e.KeyCode == Keys.Tab && !e.Alt && !e.Control && !e.Shift )
            {
                e.Handled = true;
                _comboTime.Focus();
            }
        }

        private void SetControlText()
        {
            if ( _currentDateTime > DateTime.MinValue )
            {
                _comboTime.Enabled = true;
                _textBox.Text = _currentDateTime.ToString( "MMM dd, yyyy" );
                if( _autoShowTimeOnActivation )
                {
                    _comboTime.Text = _currentDateTime.ToShortTimeString();
                }
            }
            else
            {
                _textBox.Text = "None";
            }
        }

	    private void StartEditDateTime()
        {
            _textBox.Visible = false;
            _dateTimePicker.Focus();
            if( _currentDateTime <= _dateTimePicker.MinDate || _currentDateTime >= _dateTimePicker.MaxDate )
            {
                CurrentDateTime = DateTime.Now;
            }
            _dateTimePicker.Value = _currentDateTime;
            _btnClear.Enabled = true;
        }

        private void ResetDateTime()
        {
            CurrentDateTime = DateTime.MinValue;
            _textBox.Text = "None";
            _textBox.Visible = true;
            _comboTime.Text = string.Empty;
            _comboTime.Enabled = false;
            _fakeTextBox.Focus();
        }

        private void ControlEnabledChanged(object sender, EventArgs e)
        {
            _textBox.BackColor = Enabled ? SystemColors.Menu : SystemColors.Control;
        }

        private void _comboTime_TextChanged(object sender, EventArgs e)
        {
            bool   status = true;
            string timeStr = _comboTime.Text;
            if( timeStr.Length > 0 )
            {
                try
                {
                    DateTime.Parse( timeStr );
                }
                catch( Exception )
                {
                    status = false;
                }
            }

            if( status != _timeParsingOk && ValidStateChanged != null )
            {
                ValidStateChanged( this, new ValidStateEventArgs( status ));
                _timeParsingOk = status;
            }
        }

        private void _dateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            _currentDateTime = _dateTimePicker.Value;
            Trace.WriteLine( "DateTimePicker -- current date is " + _dateTimePicker.Value );
        }
    }
}
