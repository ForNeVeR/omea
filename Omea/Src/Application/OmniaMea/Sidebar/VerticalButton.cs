/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.UI.Interop;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    /// <summary>
    /// A clickable vertical button.
    /// </summary>
    internal class VerticalButton: Control
	{
        private IContainer components;

        private int _angle;
        private IntPtr _fontHandle;
        private bool _pressed;
        private bool _pressing;
        private bool _hot;
        private Icon _icon;
        private Timer _dragOverTimer;
        private bool _customDrawFailed = false;
        private GraphicsPath _borderPath;
        private double _heightMultiplier = 1.0;
        private ColorScheme _colorScheme;

		public VerticalButton()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            SetStyle( ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.SupportsTransparentBackColor | ControlStyles.DoubleBuffer | 
                ControlStyles.ResizeRedraw, true );
            SetStyle( ControlStyles.Selectable, false );

		    CreateVerticalFont();
            CreateBorderPath();
		}

	    /// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
                if ( _fontHandle != IntPtr.Zero )
				{
                    Win32Declarations.DeleteObject( _fontHandle );
                    _fontHandle = IntPtr.Zero;
				}
                if ( _borderPath != null )
                {
                    _borderPath.Dispose();
                }
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
            this.components = new Container();
            this._dragOverTimer = new Timer(this.components);
            // 
            // _dragOverTimer
            // 
            this._dragOverTimer.Interval = 500;
            this._dragOverTimer.Tick += new EventHandler(this._dragOverTimer_Tick);
            // 
            // VerticalButton
            // 
            this.AllowDrop = true;
            this.Name = "VerticalButton";

        }
		#endregion

        public int Angle
        {
            get { return _angle; }
            set 
            { 
                _angle = value; 
                CreateVerticalFont();
            }
        }

        public Icon Icon
        {
            get { return _icon; }
            set { _icon = value; }
        }

        public ColorScheme ColorScheme
        {
            get { return _colorScheme; }
            set
            {
                if ( _colorScheme != value )
                {
                    _colorScheme = value;
                    Invalidate();
                }
            }
        }

        public bool Pressed
        {
            get { return _pressed; }
            set 
            { 
                if ( _pressed != value )
                {
                    _pressed = value; 
                    Invalidate();
                    if ( PressedChanged != null )
                    {
                        PressedChanged( this, EventArgs.Empty );
                    }
                }
            }
        }

        public int PreferredHeight
        {
            get
            {
                using( Graphics g = CreateGraphics() )
                {
                    IntPtr hdc = g.GetHdc();
                    SIZE sz = new SIZE();
                    Win32Declarations.GetTextExtentPoint32( hdc, Text, Text.Length, ref sz );
                    g.ReleaseHdc( hdc );
                    
                    return sz.cx + 26;  // 10 px - top space, 10 px - bottom space, 16 px - icon
                }
                
            }
        }

	    public double HeightMultiplier
	    {
	        get { return _heightMultiplier; }
	        set { _heightMultiplier = value; }
	    }

	    public event EventHandler PressedChanged;

        private void CreateVerticalFont()
        {
            if ( _fontHandle != IntPtr.Zero )
            {
                Win32Declarations.DeleteObject( _fontHandle );
                _fontHandle = IntPtr.Zero;
            }

            int logPixY;
            using( Graphics g = CreateGraphics() )
            {
                IntPtr hdc = g.GetHdc();
                logPixY = Win32Declarations.GetDeviceCaps( hdc, Win32Declarations.LOGPIXELSY );
                g.ReleaseHdc( hdc );
            }
            
            _fontHandle = Win32Declarations.CreateFont( (int) (-Font.SizeInPoints * logPixY / 72),
                0, _angle * 10, 0, Win32Declarations.FW_NORMAL, 0, 0, 0, 0, 0, 0, 0, 0, Font.Name );
        }

        private void CreateBorderPath()
        {
            if ( _borderPath != null )
            {
                _borderPath.Dispose();                
            }
            _borderPath = GdiPlusTools.BuildRoundRectPath( ClientRectangle );
        }

        protected override void OnFontChanged( EventArgs e )
        {
            base.OnFontChanged( e );
            CreateVerticalFont();
        }

	    protected override void OnSizeChanged( EventArgs e )
	    {
	        base.OnSizeChanged( e );
            CreateBorderPath();
	    }

	    protected override void OnPaint( PaintEventArgs e )
        {
            base.OnPaint( e );
            if ( _customDrawFailed )
                return;

            try
            {
                int left = ClientRectangle.Left;
                int top = ClientRectangle.Top;
                int right = ClientRectangle.Right;
                int bottom = ClientRectangle.Bottom;

                bool drawPressed = _pressed || ( _pressing && _hot);

                string backBrushId = drawPressed
                    ? "Sidebar.Button.BackgroundPressed"
                    : "Sidebar.Button.Background";
                
                Brush backBrush = GUIControls.ColorScheme.GetBrush( _colorScheme, backBrushId, ClientRectangle,
                    SystemBrushes.Control );
                e.Graphics.FillPath( backBrush, _borderPath );

                Pen borderPen = GUIControls.ColorScheme.GetPen( _colorScheme, "Sidebar.Button.Border", 
                    SystemPens.ControlDark );
                e.Graphics.DrawPath( borderPen, _borderPath );

                if ( _hot )
                {
                    Pen hotPen = GUIControls.ColorScheme.GetPen( _colorScheme, "Sidebar.Button.BorderHot", 
                        Pens.Blue );
                    e.Graphics.DrawLine( hotPen, left+1, top+2, left+1, bottom-3 );
                    e.Graphics.DrawLine( hotPen, left+2, top+1, right-3, top+1 );
                    e.Graphics.DrawLine( hotPen, right-2, top+2, right-2, bottom-3 );
                    e.Graphics.DrawLine( hotPen, left+2, bottom-2, right-3, bottom-2 );
                    e.Graphics.DrawRectangle( hotPen, left+2, top+2, ClientRectangle.Width-5, ClientRectangle.Height-5 );
                }
                else
                {
                    string leftPenId = drawPressed ? "Sidebar.Button.BorderDarkPressed" : "Sidebar.Button.BorderLight";
                    string rightPenId = drawPressed ? "Sidebar.Button.BorderLight" : "Sidebar.Button.BorderDark";
                    
                    Pen leftPen = GUIControls.ColorScheme.GetPen( _colorScheme, leftPenId, Pens.White );
                    e.Graphics.DrawLine( leftPen, left+1, top+2, left+1, bottom-3 );
                    e.Graphics.DrawLine( leftPen, left+2, top+1, right-3, top+1 );

                    Pen rightPen = GUIControls.ColorScheme.GetPen( _colorScheme, rightPenId, SystemPens.ControlDark );
                    e.Graphics.DrawLine( rightPen, right-2, top+2, right-2, bottom-3 );
                    e.Graphics.DrawLine( rightPen, left+2, bottom-2, right-3, bottom-2 );
                }

                IntPtr hdc = e.Graphics.GetHdc();
                IntPtr oldFont = Win32Declarations.SelectObject( hdc, _fontHandle );
                BackgroundMode oldMode = Win32Declarations.SetBkMode( hdc, BackgroundMode.TRANSPARENT );

                int delta = drawPressed ? 1 : 0;
                int topSpace = 6;
                int iconAreaHeight = 20;

                int maxTextHeight = ClientRectangle.Height - topSpace - iconAreaHeight - 2;
                SIZE sz = new SIZE();
                Win32Declarations.GetTextExtentPoint32( hdc, Text, Text.Length, ref sz );

                string textToDraw = Text;
                if ( sz.cx > maxTextHeight )
                {
                    // calculate how many characters fit if we leave space for the ellipsis
                    SIZE szClip = new SIZE();
                    int charsFit;
                    Win32Declarations.GetTextExtentExPoint( hdc, Text, Text.Length, maxTextHeight-10,
                        out charsFit, IntPtr.Zero, out szClip );
                    if ( charsFit > 0 )
                    {
                        textToDraw = Text.Substring( 0, charsFit ) + "...";
                    }
                    else
                    {
                        textToDraw = Text.Substring( 0, 1 );
                    }

                    Win32Declarations.GetTextExtentPoint32( hdc, textToDraw, textToDraw.Length, ref sz );
                }
                                
                if ( _angle == 270 )
                {
                    Win32Declarations.TextOut( hdc, ClientRectangle.Right - 2 + delta, ClientRectangle.Top + topSpace + delta, 
                        textToDraw, textToDraw.Length );
                }
                else if ( _angle == 90 )
                {
                    Win32Declarations.TextOut( hdc, ClientRectangle.Left + 2 + delta, ClientRectangle.Top + topSpace + sz.cx + delta, 
                        textToDraw, textToDraw.Length );
                }

                Win32Declarations.SetBkMode( hdc, oldMode );
                Win32Declarations.SelectObject( hdc, oldFont );
                e.Graphics.ReleaseHdc( hdc );

                if ( _icon != null )
                {
                    int iconX = (ClientRectangle.Width - 16) / 2;
                    e.Graphics.DrawIcon( _icon, ClientRectangle.Left + iconX, ClientRectangle.Bottom - iconAreaHeight + delta );
                }
            }
            catch( Exception ex )
            {
                Core.ReportBackgroundException( ex );
                _customDrawFailed = true;
            }
        }

        protected override void OnMouseDown( MouseEventArgs e )
        {
            base.OnMouseDown( e );
            if ( e.Button == MouseButtons.Left )
            {
                _pressing = true;
                Capture = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp( MouseEventArgs e )
        {
            base.OnMouseUp( e );
            if ( e.Button == MouseButtons.Left )
            {
                _pressing = false;
                Capture = false;
                if ( ClientRectangle.Contains( e.X, e.Y ) )
                {
                    Pressed = !Pressed;
                    _hot = true;
                }
                else
                {
                    Invalidate();
                }
            }
        }

        protected override void OnMouseMove( MouseEventArgs e )
        {
            base.OnMouseMove( e );
            if ( _pressing )
            {
                bool oldHot = _hot;
                _hot = ClientRectangle.Contains( e.X, e.Y );
                if ( oldHot != _hot )
                {
                    Invalidate();
                }
            }
        }

        protected override void OnMouseEnter( EventArgs e )
        {
            base.OnMouseEnter( e );
            _hot = true;
            Invalidate();
        }

        protected override void OnMouseLeave( EventArgs e )
        {
            base.OnMouseLeave( e );
            _hot = false;
            Invalidate();
        }

        protected override void OnDragEnter( DragEventArgs drgevent )
        {
            base.OnDragEnter( drgevent );
            _dragOverTimer.Start();
        }
        
        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave( e );
            _dragOverTimer.Stop();
        }

        private void _dragOverTimer_Tick( object sender, EventArgs e )
        {
            _dragOverTimer.Stop();
            Pressed = true;
        }
    }
}
