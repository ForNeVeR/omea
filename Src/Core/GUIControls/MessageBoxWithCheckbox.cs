// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.GUIControls
{
	public class MessageBoxWithCheckBox : DialogBase
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private Label _text;
        private CheckBox _checkBox;
        private GdiControlPainter _painter = new GdiControlPainter();
        private const int BORDER_X = 40;
        private const int INTERVAL_Y = 16;
        private const int STANDART_WIDTH = 440;
        private const int BUTTON_HEGHT = 23;
        private const int INTERVAL_BETWEEN_BUTTONS = 8;
		private const string BUTTON_YES = "&Yes";
		private const string BUTTON_NO = "&No";
		private readonly int _BUTTON_WIDTH = 75;
        private readonly ButtonClick[] _buttons = null;
        private int _idPressedButton = -1;
        private PictureBox _icon;
        private int _buttonsOffset = 0;

		protected MessageBoxWithCheckBox( string text, string caption, string checkBoxText, bool isChecked, string[] buttons, int[] results, string cancelButton, string acceptButton )
		{
            if ( buttons == null || buttons.Length == 0 )
            {
                throw new ArgumentException( "buttons should have at list ont entry" );
            }
		    Guard.NullArgument( results, "results" );
            if ( results.Length != buttons.Length )
            {
                throw new ArgumentException( "Counts for buttons and results must be equal." );
            }

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
            SuspendLayout();
            this.Text = caption;
            _text.Text = text;
            _checkBox.Text = checkBoxText;
            _checkBox.Checked = isChecked;

            _buttons = new ButtonClick[buttons.Length];
            for( int i = 0; i < buttons.Length; ++i )
            {
                _buttons[i] = new ButtonClick( this, buttons[i], results[i], i );
                if ( cancelButton != null && buttons[i] == cancelButton )
                {
                    CancelButton = _buttons[i]._button;
                }
                if ( acceptButton != null && buttons[i] == acceptButton )
                {
                    AcceptButton = _buttons[i]._button;
                }

                int width = _painter.MeasureText( Graphics.FromHwnd( Handle ), buttons[i], _buttons[i]._button.Font ).Width;
                if ( width + 8 > _BUTTON_WIDTH )
                {
                    _BUTTON_WIDTH = width + 8;
                }
            }
            AdjustContolProperties( this );

            SetTextBox();
            SetCheckBox();
            SetButtons();
            SetDialogBox();
            MoveButtons();
            ResumeLayout( false );
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
                _painter.Dispose();
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
            this._text = new System.Windows.Forms.Label();
            this._checkBox = new System.Windows.Forms.CheckBox();
            this._icon = new System.Windows.Forms.PictureBox();
            this.SuspendLayout();
            //
            // _text
            //
            this._text.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._text.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._text.Location = new System.Drawing.Point(256, 16);
            this._text.Name = "_text";
            this._text.Size = new System.Drawing.Size(168, 20);
            this._text.TabIndex = 0;
            //
            // _checkBox
            //
            this._checkBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._checkBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._checkBox.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._checkBox.Location = new System.Drawing.Point(0, 80);
            this._checkBox.Name = "_checkBox";
            this._checkBox.Size = new System.Drawing.Size(444, 22);
            this._checkBox.TabIndex = 1;
            //
            // _icon
            //
            this._icon.Location = new System.Drawing.Point(12, 8);
            this._icon.Name = "_icon";
            this._icon.Size = new System.Drawing.Size(52, 50);
            this._icon.TabIndex = 2;
            this._icon.TabStop = false;
            this._icon.Visible = false;
            //
            // MessageBoxWithCheckBox
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(438, 128);
            this.Controls.Add(this._icon);
            this.Controls.Add(this._checkBox);
            this.Controls.Add(this._text);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "MessageBoxWithCheckBox";
            this.Text = "MessageBoxWithCheckbox";
            this.ResumeLayout(false);
        }
		#endregion

        public void SetFont( Font font )
        {
            Guard.NullArgument( font, "font" );
            Font = font;
            _checkBox.Font = font;
            _text.Font = font;
        }

        private void MoveButtons()
        {
            Button lastButton = _buttons[ _buttons.Length - 1 ]._button;
            int width = ( lastButton.Size.Width + lastButton.Location.X ) / 2;
            int dlgWidth = Size.Width / 2;
            _buttonsOffset = dlgWidth - width;
            foreach ( ButtonClick button in _buttons )
            {
                button.SetSizeAndLocation();
            }
        }

        private void SetButtons()
        {
            foreach ( ButtonClick button in _buttons )
            {
                button.SetSizeAndLocation();
            }
        }

        private void SetDialogBox()
        {
            int maxW = Math.Max( _text.Width, _checkBox.Right );
            maxW = Math.Max( maxW,  _buttons[ _buttons.Length - 1 ]._button.Right );

            Width = maxW + 2 * BORDER_X;
            Height = _buttons[ 0 ]._button.Location.Y + BUTTON_HEGHT + 43;
        }
        private void SetTextBox()
        {
            Size textSize = _painter.MeasureText( Graphics.FromHwnd( Handle ), _text.Text, _text.Font, STANDART_WIDTH );
            _text.Size = textSize;
            Point location = _text.Location;
            location.X = BORDER_X;
            location.Y = INTERVAL_Y;
            _text.Location = location;
        }

        public int IdPressedButton{ get { return _idPressedButton; } }

        private void SetCheckBox()
        {
            int     currWidth = (int)Graphics.FromHwnd( _checkBox.Handle ).MeasureString( _checkBox.Text, _checkBox.Font ).Width;
            _checkBox.Size = new Size( currWidth + _checkBox.Height, _checkBox.Height );
            Point checkBoxLocation = _checkBox.Location;
            checkBoxLocation.Y = _text.Location.Y + _text.Height + 8;
            int delta = _text.Width - currWidth - 20;
            if( delta < 2 )
            {
                checkBoxLocation.X = BORDER_X;
            }
            else
            {
                checkBoxLocation.X = BORDER_X + delta / 2;
            }
            _checkBox.Location = checkBoxLocation;
        }

        public Result Show( IWin32Window parent )
        {
            ShowDialog( parent );
            return new Result( _idPressedButton, _checkBox.Checked );
        }

        public static Result Show( IWin32Window parent, string text, string caption, string checkBoxText, bool isChecked, string[] buttons, int[] results, string cancelButton, string acceptButton )
        {
            using( MessageBoxWithCheckBox messageBox = new MessageBoxWithCheckBox( text, caption, checkBoxText, isChecked, buttons, results, cancelButton, acceptButton ) )
            {
                if ( parent == null )
                {
                    messageBox.StartPosition = FormStartPosition.CenterScreen;
                }
                return messageBox.Show( parent );
            }
        }
        public static Result ShowYesNo( IWin32Window parent, string text, string caption, string checkBoxText, bool isChecked )
        {
            using( MessageBoxWithCheckBox messageBox = new MessageBoxWithCheckBox( text, caption, checkBoxText, isChecked,
                       new string[]{ BUTTON_YES, BUTTON_NO }, new int[]{ (int)DialogResult.Yes, (int)DialogResult.No }, BUTTON_NO, BUTTON_YES ) )
            {
                if ( parent == null )
                {
                    messageBox.StartPosition = FormStartPosition.CenterScreen;
                }
                return messageBox.Show( parent );
            }
        }
        public class Result
        {
            private readonly int _dlgResult;
            private readonly bool _isChecked;
            public Result( int dlgResult, bool isChecked )
            {
                _dlgResult = dlgResult;
                _isChecked = isChecked;
            }
            public int IdPressedButton{ get { return _dlgResult; } }
            public bool Checked{ get { return _isChecked; } }
        }
        private class ButtonClick
        {
            public readonly Button _button;

            private readonly int _id;
            private readonly int _dlgResult;
            private readonly MessageBoxWithCheckBox _parent;

            private static int _tabIndex = 0;

            public ButtonClick( MessageBoxWithCheckBox parent, string text, int dlgResult, int id )
            {
                _parent = parent;
                _id = id;
                _dlgResult = dlgResult;

                _button = new Button();
                _button.FlatStyle = FlatStyle.System;
                _button.TabIndex = _tabIndex++;
                _button.Text = text;
                _button.Click += _button_Click;

                _parent.Controls.Add( _button );
            }
            public void SetSizeAndLocation()
            {
                _button.Size = new Size( _parent._BUTTON_WIDTH, BUTTON_HEGHT );
                int Y = _parent._checkBox.Location.Y + 36;
                _button.Location = new Point( _id * ( _parent._BUTTON_WIDTH + INTERVAL_BETWEEN_BUTTONS ) + _parent._buttonsOffset, Y );
            }

            private void _button_Click(object sender, EventArgs e)
            {
                _parent._idPressedButton = _dlgResult;
                _parent.Close();
            }
        }
    }
}
