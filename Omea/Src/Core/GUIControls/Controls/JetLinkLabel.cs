// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using JetBrains.Interop.WinApi;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Interop;
using JetBrains.UI.RichText;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// A link label which draws its text through the Windows API, draws highlighting
    /// on mouse over and executes the link click action only on single click.
    /// </summary>
    public class JetLinkLabel : UserControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private static readonly FontCache _fontCache = new FontCache();
        private static readonly Color _linkColor = Color.FromArgb( 70, 70, 211 );
        private Font _underlineFont;
        private bool _underline;
        private bool _autoSize = false;
        private bool _wordWrap;
        private bool _clickableLink = true;
        private bool _useMnemonic = false;
        private bool _endEllipsis = false;
        private string _postfixText = "";     // the text which is drawn after the link
        private ContentAlignment _textAlign = ContentAlignment.TopLeft;

		public JetLinkLabel()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			SetStyle( ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw | ControlStyles.CacheText, true );
            SetStyle( ControlStyles.StandardClick | ControlStyles.Selectable, false );
            ForeColor = _linkColor;
            Cursor = Cursors.Hand;
            _underlineFont = new Font( Font, FontStyle.Underline );
            _autoSize = true;
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
			components = new System.ComponentModel.Container();
		}
		#endregion

        protected override void ScaleCore( float dx, float dy )
        {
            base.ScaleCore( dx, dy );
            if ( _underlineFont != null )
            {
                _underlineFont.Dispose();
            }
            _underlineFont = new Font( Font, FontStyle.Underline );
            if ( _autoSize )
            {
                Size = PreferredSize;
            }
        }

        [DefaultValue(true)]
        public bool AutoSize
	    {
	        get { return _autoSize; }
	        set { _autoSize = value; }
	    }

        [DefaultValue(false)]
        public bool WordWrap
        {
            get { return _wordWrap; }
            set
            {
                if ( _wordWrap != value )
                {
                    _wordWrap = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue(true)]
        public bool ClickableLink
        {
            get { return _clickableLink; }
            set
            {
                _clickableLink = value;
                ForeColor = _clickableLink ? _linkColor : SystemColors.ControlText;
                Cursor = _clickableLink ? Cursors.Hand : Cursors.Default;
            }
        }

        [DefaultValue(false)]
        public bool UseMnemonic
        {
            get { return _useMnemonic; }
            set
            {
                if ( _useMnemonic != value )
                {
                    _useMnemonic = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue("")]
        public string PostfixText
        {
            get { return _postfixText; }
            set
            {
                if ( _postfixText != value )
                {
                    _postfixText = value;
                    if ( _autoSize )
                    {
                        Size = PreferredSize;
                    }
                    Invalidate();
                }
            }
        }

        [DefaultValue(ContentAlignment.TopLeft)]
        public ContentAlignment TextAlign
        {
            get { return _textAlign; }
            set
            {
                if ( _textAlign != value )
                {
                    _textAlign = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue(false)]
        public bool EndEllipsis
        {
            get { return _endEllipsis; }
            set
            {
                if ( _endEllipsis != value )
                {
                    _endEllipsis = value;
                    Invalidate();
                }
            }
        }

        protected override void OnFontChanged( EventArgs e )
        {
            base.OnFontChanged( e );
            _underlineFont = new Font( Font, FontStyle.Underline );
            if ( IsHandleCreated && _autoSize )
            {
                Size = PreferredSize;
            }
        }

        protected override void OnHandleCreated( EventArgs e )
        {
            base.OnHandleCreated( e );
            if ( _autoSize )
            {
                Size = PreferredSize;
            }
        }

        protected override void OnTextChanged( EventArgs e )
        {
            base.OnTextChanged( e );
            if ( IsHandleCreated && !IsDisposed )
            {
                if ( _autoSize )
                {
                    Size = PreferredSize;
                }
                Invalidate();
            }
        }

		/// <summary>
		/// Size of the link label text. Note: it is recalculated using the device context every time you request it.
		/// </summary>
		public Size PreferredSize
    	{
    		get
    		{
    		    Size prefSize = GetTextSize( this, Text + _postfixText, Font, GetTextFormatFlags() );
                if ( Core.ScaleFactor.Height >= 1.01f )
                {
                    prefSize.Height += 1;
                }
    		    return prefSize;
    		}
    	}

		#region Generic Helper Functions

    	/// <summary>
    	/// Calculates the rectangle needed for rendering the text string.
    	/// </summary>
    	/// <param name="control">Control to which the text will be rendered.
    	/// It's used for getting the device context and setting the initial text bounds
    	/// (the latter is ignored in the single-line case).</param>
    	/// <param name="text">Text to be rendered.</param>
    	/// <param name="font">Font in which the text will be rendered.</param>
    	/// <param name="bounds">Initial size that should be expanded to fit the text.</param>
    	/// <param name="dtf">Additional parameters that control rendering of the text.</param>
    	/// <returns>Size of the text's bounding rectangle.</returns>
    	public static Size GetTextSize( Control control, string text, Font font, Size bounds, DrawTextFormatFlags dtf )
    	{
    		using( Graphics g = control.CreateGraphics() )
    		{
    			IntPtr hdc = g.GetHdc();
    			try
    			{
    				IntPtr hFont = _fontCache.GetHFont( font );
    				IntPtr oldFont = Win32Declarations.SelectObject( hdc, hFont );
    				RECT rc = new RECT( 0, 0, bounds.Width, bounds.Height );

    				Win32Declarations.DrawText( hdc, text, text.Length, ref rc,
    				                            dtf | DrawTextFormatFlags.DT_CALCRECT );

    				int height = rc.bottom - rc.top;
                    //height += 1;
                    Size sz = new Size( rc.right - rc.left, height );

    				Win32Declarations.SelectObject( hdc, oldFont );

    				return sz;
    			}
    			finally
    			{
    				g.ReleaseHdc( hdc );
    			}
    		}
    	}

		/// <summary>
		/// Calculates the rectangle needed for rendering the text string.
		/// </summary>
		/// <param name="control">Control to which the text will be rendered.
		/// It's used for getting the device context and setting the initial text bounds
		/// (the latter is ignored in the single-line case).</param>
		/// <param name="text">Text to be rendered.</param>
		/// <param name="font">Font in which the text will be rendered.</param>
		/// <param name="dtf">Additional parameters that control rendering of the text</param>
		/// <returns>Size of the text's bounding rectangle.</returns>
		/// <remarks>The initial size is the control size.</remarks>
		public static Size GetTextSize( Control control, string text, Font font, DrawTextFormatFlags dtf )
		{
            return GetTextSize( control, text, font, new Size( Int32.MaxValue, control.Height ), dtf );
		}

    	/// <summary>
    	/// Calculates the rectangle needed for rendering the text string.
    	/// </summary>
    	/// <param name="control">Control to which the text will be rendered.
    	/// It's used for getting the device context and setting the initial text bounds
    	/// (the latter is ignored in the single-line case).</param>
    	/// <param name="text">Text to be rendered.</param>
    	/// <param name="font">Font in which the text will be rendered.</param>
    	/// <returns>Size of the text's bounding rectangle.</returns>
    	/// <remarks>Uses <see cref="DrawTextFormatFlags.DT_NOPREFIX"/> and <see cref="DrawTextFormatFlags.DT_SINGLELINE"/>
    	/// as the text rendering flags for calling the main overload.</remarks>
    	public static Size GetTextSize( Control control, string text, Font font )
    	{
    		return GetTextSize(control, text, font, new Size(control.Width, control.Height), DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE);
    	}

    	/// <summary>
    	/// Renders some text to a graphics device.
    	/// </summary>
    	/// <param name="graphics">Graphics device to render the text into.</param>
    	/// <param name="text">Text to render.</param>
    	/// <param name="rect">Bounding rectangle for the text to fit into.</param>
    	/// <param name="font">Font in which the text is rendered.</param>
    	/// <param name="color">Text color.</param>
    	/// <param name="dtf">Formatting flags.</param>
    	public static void DrawText(Graphics graphics, string text, Rectangle rect, Font font, Color color, DrawTextFormatFlags dtf)
    	{
    		IntPtr hdc = graphics.GetHdc();
    		try
    		{
    			// Font
    			IntPtr hFont = _fontCache.GetHFont( font );
    			IntPtr oldFont = Win32Declarations.SelectObject( hdc, hFont );

    			// Bounding rectangle
    			RECT rc = new RECT(rect.Left,  rect.Top, rect.Right, rect.Bottom);

    			// Color
    			int textColor = Win32Declarations.ColorToRGB( color );
    			int oldColor = Win32Declarations.SetTextColor( hdc, textColor );
    			BackgroundMode oldMode = Win32Declarations.SetBkMode( hdc, BackgroundMode.TRANSPARENT );

    			// Render the text
    			Win32Declarations.DrawText( hdc, text, text.Length, ref rc, dtf );

    			// Do deinit
    			Win32Declarations.SetBkMode( hdc, oldMode );
    			Win32Declarations.SetTextColor( hdc, oldColor );
    			Win32Declarations.SelectObject( hdc, oldFont );
    		}
    		finally
    		{
    			graphics.ReleaseHdc( hdc );
    		}
    	}

    	#endregion

		/// <summary>
		/// Width of the link label text. Note: it is recalculated using the device context every time you request it.
		/// </summary>
    	public int PreferredWidth
        {
            get { return PreferredSize.Width; }
        }

        private DrawTextFormatFlags GetTextFormatFlags()
        {
            DrawTextFormatFlags flags = 0;
            if ( !_useMnemonic )
            {
                flags |= DrawTextFormatFlags.DT_NOPREFIX;
            }
            if ( _wordWrap )
            {
                flags |= DrawTextFormatFlags.DT_WORDBREAK;
            }
            if ( _endEllipsis )
            {
                flags |= DrawTextFormatFlags.DT_END_ELLIPSIS | DrawTextFormatFlags.DT_SINGLELINE;
            }
            if ( _textAlign != ContentAlignment.TopLeft )
            {
                flags |= DrawTextFormatFlags.DT_SINGLELINE;
            }
            switch( _textAlign )
            {
                case ContentAlignment.TopCenter:
                    flags |= DrawTextFormatFlags.DT_CENTER; break;
                case ContentAlignment.TopRight:
                    flags |= DrawTextFormatFlags.DT_RIGHT; break;
                case ContentAlignment.MiddleLeft:
                    flags |= DrawTextFormatFlags.DT_VCENTER; break;
                case ContentAlignment.MiddleCenter:
                    flags |= DrawTextFormatFlags.DT_CENTER | DrawTextFormatFlags.DT_VCENTER; break;
                case ContentAlignment.MiddleRight:
                    flags |= DrawTextFormatFlags.DT_VCENTER | DrawTextFormatFlags.DT_RIGHT; break;
                case ContentAlignment.BottomLeft:
                    flags |= DrawTextFormatFlags.DT_BOTTOM; break;
                case ContentAlignment.BottomCenter:
                    flags |= DrawTextFormatFlags.DT_BOTTOM | DrawTextFormatFlags.DT_CENTER; break;
                case ContentAlignment.BottomRight:
                    flags |= DrawTextFormatFlags.DT_BOTTOM | DrawTextFormatFlags.DT_RIGHT; break;
            }
            return flags;
        }

        protected override void OnPaint( PaintEventArgs e )
        {
            // Paint background
			if(BackColor != Color.Transparent)
			{
				using(Brush brush = new SolidBrush(BackColor))
					e.Graphics.FillRectangle( brush, ClientRectangle );
			}

            base.OnPaint( e );

			// Paint foreground
			IntPtr hdc = e.Graphics.GetHdc();
            try
            {
				IntPtr hFont = _fontCache.GetHFont( _underline ? _underlineFont : Font );
                IntPtr oldFont = Win32Declarations.SelectObject( hdc, hFont );

                RECT rc = new RECT( 0, 0, Bounds.Width, Bounds.Height );
                int textColor = Enabled
                    ? Win32Declarations.ColorToRGB( ForeColor )
                    : Win32Declarations.ColorToRGB( SystemColors.GrayText );

                int oldColor = Win32Declarations.SetTextColor( hdc, textColor );
                BackgroundMode oldMode = Win32Declarations.SetBkMode( hdc, BackgroundMode.TRANSPARENT );

                int postfixLeft = 0;
                if ( _postfixText.Length > 0 )
                {
                    Win32Declarations.DrawText( hdc, Text, Text.Length, ref rc,
                        GetTextFormatFlags() | DrawTextFormatFlags.DT_CALCRECT );
                    postfixLeft = rc.right;
                }
                Win32Declarations.DrawText( hdc, Text, Text.Length, ref rc, GetTextFormatFlags() );

                if ( _postfixText.Length > 0 )
                {
                    Win32Declarations.SetTextColor( hdc, ColorTranslator.ToWin32( Color.Black ) );
                    if ( _underline )
                    {
                        Win32Declarations.SelectObject( hdc, _fontCache.GetHFont( Font ) );
                    }
                    rc.left = postfixLeft;
                    rc.right = Bounds.Width;
                    Win32Declarations.DrawText( hdc, _postfixText, _postfixText.Length, ref rc, GetTextFormatFlags() );
                }

                Win32Declarations.SetBkMode( hdc, oldMode );
                Win32Declarations.SetTextColor( hdc, oldColor );
                Win32Declarations.SelectObject( hdc, oldFont );
            }
            finally
            {
                e.Graphics.ReleaseHdc( hdc );
            }
        }

        protected override void OnMouseDown( MouseEventArgs e )
        {
            // UserControl.OnMouseDown sets focus to the control, which is wrong if
            // the link is not clickable
            if ( _clickableLink )
            {
                base.OnMouseDown (e);
            }
        }

        protected override void OnMouseEnter( EventArgs e )
        {
            base.OnMouseEnter( e );
            if ( Enabled && ClickableLink )
            {
                _underline = true;
                Invalidate();
            }
        }

        protected override void OnMouseLeave( EventArgs e )
        {
            base.OnMouseLeave( e );
            if ( Enabled && ClickableLink )
            {
                _underline = false;
                Invalidate();
            }
        }

        protected override void OnMouseUp( MouseEventArgs e )
        {
            base.OnMouseUp( e );
            if ( Enabled && ClickableLink && e.Button == MouseButtons.Left && ClientRectangle.Contains( e.X, e.Y ) )
            {
                OnClick( EventArgs.Empty );
            }
        }
	}
}
