// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.Omea.ContactsPlugin
{
	public class PropertyEditor : System.Windows.Forms.UserControl
	{
        private System.Windows.Forms.TextBox _textBox;
        private System.Windows.Forms.Label _textLabel;
        private System.ComponentModel.Container components = null;

		public PropertyEditor()
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
            this._textBox = new System.Windows.Forms.TextBox();
            this._textLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // _textBox
            //
            this._textBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._textBox.Location = new System.Drawing.Point(0, 0);
            this._textBox.Name = "_textBox";
            this._textBox.Size = new System.Drawing.Size(184, 20);
            this._textBox.TabIndex = 0;
            this._textBox.Text = "";
            this._textBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnMouseDown);
            this._textBox.TextChanged += new System.EventHandler(this._textBox_TextChanged);
            //
            // _textLabel
            //
            this._textLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._textLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._textLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._textLabel.Location = new System.Drawing.Point(0, 0);
            this._textLabel.Name = "_textLabel";
            this._textLabel.Size = new System.Drawing.Size(184, 32);
            this._textLabel.TabIndex = 1;
            this._textLabel.Text = "label1";
            this._textLabel.Visible = false;
            //
            // PropertyEditor
            //
            this.Controls.Add(this._textLabel);
            this.Controls.Add(this._textBox);
            this.Name = "PropertyEditor";
            this.Size = new System.Drawing.Size(184, 32);
            this.ResumeLayout(false);

        }
		#endregion

        public override string Text
        {
            get{ return _textBox.Text; }
            set
            {
                _textBox.Text = value;
                if ( value == "" || value == null )
                {
                    _textLabel.Text = "Not Specified";
                    _textLabel.Font = new Font( _textLabel.Font, FontStyle.Regular );
                    _textLabel.ForeColor = SystemColors.GrayText;
                }
                else
                {
                    _textLabel.Text = value;
                    _textLabel.ForeColor = SystemColors.WindowText;
                    _textLabel.Font = new Font( _textLabel.Font, FontStyle.Bold );
                }
            }
        }

        public bool ReadOnly
        {
            get { return _textLabel.Visible; }
            set
            {
                _textBox.Visible = !value;
                _textLabel.Visible = value;
                SetStyle( ControlStyles.Selectable, !value );
            }
        }

        public bool Multiline
        {
            get { return _textBox.Multiline; }
            set
            {
                _textBox.Multiline = value;
            }
        }

        public int PreferredHeight
        {
            get
            {
                using( Graphics g = _textLabel.CreateGraphics() )
                {
                    SizeF sz = g.MeasureString( _textLabel.Text, _textLabel.Font, _textLabel.Width );
                    return (int) sz.Height;
                }
            }
        }

        public delegate void onMouseEventHandler( object sender, MouseEventArgs e );
        public event onMouseEventHandler onMouseListenerEvent;
        public new event EventHandler TextChanged;

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if ( onMouseListenerEvent != null )
            {
                onMouseListenerEvent( this, e );
            }
        }

        private void _textBox_TextChanged( object sender, System.EventArgs e )
        {
            if ( TextChanged != null )
            {
                TextChanged( this, EventArgs.Empty );
            }
        }
	}
}
