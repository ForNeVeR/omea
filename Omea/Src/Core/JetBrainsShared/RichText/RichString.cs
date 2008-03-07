/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Drawing.Drawing2D;

using JetBrains.Interop.WinApi;
using JetBrains.UI.Interop;

namespace JetBrains.UI.RichText
{
    /// <summary>
    /// Represents a formatted string
    /// </summary>
    internal class RichString : IComparable, ICloneable
    {
        #region Constants

        /// <summary>
        /// Wave length for weavy underlining
        /// </summary>
        private const int WAVE_LENGTH = 6;

        #endregion

        private static FontCache ourFontCache = new FontCache();

        #region Fields

        /// <summary>
        /// String style
        /// </summary>
        private TextStyle myStyle;

        /// <summary>
        /// Starting offset of the corresponding string part
        /// </summary>
        private int myOffset;

        /// <summary>
        /// Length of the corresponding string part
        /// </summary>
        private int myLength;

        /// <summary>
        /// The text which the part is belonging to
        /// </summary>
        private RichText myText;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the string style
        /// </summary>
        public TextStyle Style
        {
            get { return myStyle; }
            set { myStyle = value; }
        }

        /// <summary>
        /// Gets or sets the starting offset
        /// </summary>
        public int Offset
        {
            get { return myOffset; }
            set { myOffset = value; }
        }

        /// <summary>
        /// Gets or sets the starting length
        /// </summary>
        public int Length
        {
            get { return myLength; }
            set { myLength = value; }
        }

        /// <summary>
        /// Gets the parent text
        /// </summary>
        public RichText Text
        {
            get { return myText; }
        }

        #endregion

        /// <summary>
        /// Creates a new <see cref="RichString"/> instance.
        /// </summary>
        /// <param name="offset">String part offset</param>    
        /// <param name="length">String part length</param>
        /// <param name="style">The style</param>
        /// <param name="text">The parent text block</param>
        /// <exception cref="ArgumentNullException"><i>text</i> is null.</exception>
        public RichString( int offset, int length, TextStyle style, RichText text )
        {
            myOffset = offset;
            myLength = length;
            myStyle = style;
            myText = text;
        }

        public int GetSymbolByOffset( int x, RichTextParameters parameters, IntPtr hdc )
        {
            if (x < 0)
                return -1;

            Font hFont = GetParametrizedFont( parameters );
            RECT rc = new RECT();
            rc.left = 0;
            rc.top = 0;

            int currentX = 0;

            IntPtr oldFont = Win32Declarations.SelectObject( hdc, ourFontCache.GetHFont( hFont ) );

            try
            {
                for (int i = 0; i < PartText.Length; i++)
                {
                    Win32Declarations.DrawText( hdc, PartText.Substring( i, 1 ), 1, ref rc, DrawTextFormatFlags.DT_CALCRECT | DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_NOCLIP );
                    currentX += rc.right - rc.left;

                    if (currentX > x)
                        return i;
                }

                return -1;
            }
            finally
            {
                Win32Declarations.SelectObject( hdc, oldFont );
            }
        }

        /// <summary>
        /// Draws the formatted string on a given graphics
        /// </summary>
        /// <param name="hdc">The device context to draw the string in.</param>
        /// <param name="parameters">Text formatting parameters</param>
        /// <exception cref="ArgumentNullException"><i>g</i> is null</exception>
        /// <exception cref="ArgumentNullException"><i>font</i> is null</exception>
        public int Draw( IntPtr hdc, Rectangle rect, RichTextParameters parameters )
        {
            Font hFont = GetParametrizedFont( parameters );
            RECT rc = new RECT();
            rc.left = rect.Left;
            rc.top = rect.Top;
            rc.bottom = rect.Bottom;

            RectangleF bounds;

            IntPtr oldFont = Win32Declarations.SelectObject( hdc, ourFontCache.GetHFont( hFont ) );
            int oldColor = Win32Declarations.SetTextColor( hdc, Win32Declarations.ColorToRGB( myStyle.ForegroundColor ) );
            int oldBkColor = Win32Declarations.SetBkColor( hdc, Win32Declarations.ColorToRGB( myStyle.BackgroundColor ) );
            BackgroundMode oldBkMode = Win32Declarations.SetBkMode( hdc, myStyle.BackgroundColor == Color.Transparent ? BackgroundMode.TRANSPARENT : BackgroundMode.OPAQUE );

            Win32Declarations.DrawText( hdc, PartText, PartText.Length, ref rc, DrawTextFormatFlags.DT_CALCRECT | DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_VCENTER | DrawTextFormatFlags.DT_NOCLIP );
            if (rc.bottom > rect.Bottom) rc.bottom = rect.Bottom;
            if (rc.right > rect.Right) rc.right = rect.Right;
            bounds = new RectangleF( rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top );

            Win32Declarations.DrawText( hdc, PartText, PartText.Length, ref rc, DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_VCENTER | DrawTextFormatFlags.DT_NOCLIP );

            Win32Declarations.SetBkMode( hdc, oldBkMode );
            Win32Declarations.SetBkColor( hdc, oldBkColor );
            Win32Declarations.SetTextColor( hdc, oldColor );
            Win32Declarations.SelectObject( hdc, oldFont );

            switch (myStyle.Effect)
            {
                case TextStyle.EffectStyle.StrikeOut:
                    StrikeOut( hdc, bounds );
                    break;
                case TextStyle.EffectStyle.StraightUnderline:
                    UnderlineStraight( hdc, bounds );
                    break;
                case TextStyle.EffectStyle.WeavyUnderline:
                    UnderlineWeavy( hdc, bounds );
                    break;
            }

            return rc.right - rc.left;
        }

        /// <summary>
        /// Gets size of the string in the given graphics
        /// </summary>
        /// <param name="hdc">The device context to calculate size in</param>
        /// <param name="parameters">Formatting parameters to use</param>
        /// <returns>Size of the string when drawn in a given graphics</returns>
        /// <exception cref="ArgumentNullException"><i>g</i> is null.</exception>
        public SizeF GetSize( IntPtr hdc, RichTextParameters parameters )
        {
            Font hFont = GetParametrizedFont( parameters );
            RECT rc = new RECT();
            rc.left = 0;
            rc.top = 0;

            IntPtr oldFont = Win32Declarations.SelectObject( hdc, ourFontCache.GetHFont( hFont ) );
            Win32Declarations.DrawText( hdc, PartText, PartText.Length, ref rc, DrawTextFormatFlags.DT_CALCRECT | DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_NOPREFIX );
            Win32Declarations.SelectObject( hdc, oldFont );

            return new SizeF( rc.right - rc.left, rc.bottom - rc.top );
        }

        /// <summary>
        /// Gets text of the part
        /// </summary>
        internal string PartText
        {
            get
            {
                if ( myOffset == 0 && myLength == myText.Text.Length )
                {
                    return myText.Text;
                }
                return myText.Text.Substring( myOffset, myLength );
            }
        }

        #region Private methods

        /// <summary>
        /// Returns parametrized font
        /// </summary>
        /// <param name="parameters">Formatting parameters to use</param>
        private Font GetParametrizedFont( RichTextParameters parameters )
        {
            return ourFontCache.GetFont( parameters.Font, myStyle.FontStyle );
        }

        /// <summary>
        /// Underlines text with text style color using straight line
        /// </summary>
        private void UnderlineStraight( IntPtr hdc, RectangleF rect )
        {
            using (Graphics g = Graphics.FromHdc( hdc ))
                g.DrawLine( new Pen( new SolidBrush( myStyle.EffectColor ) ), rect.Left, rect.Bottom, rect.Right, rect.Bottom );
        }

        /// <summary>
        /// Underlines text with text style color using weavy line
        /// </summary>
        private void UnderlineWeavy( IntPtr hdc, RectangleF rect )
        {
            using (Graphics g = Graphics.FromHdc( hdc ))
            {
                Region clip = g.Clip;
                g.SetClip( rect );

                Pen pen = new Pen( new SolidBrush( myStyle.EffectColor ) );

                for (float x = rect.Left; x <= rect.Right; x += WAVE_LENGTH)
                {
                    g.DrawLine( pen, x, rect.Bottom, x + WAVE_LENGTH/2, rect.Bottom - 2 );
                    g.DrawLine( pen, x + WAVE_LENGTH/2, rect.Bottom, x + WAVE_LENGTH, rect.Bottom );
                }

                g.SetClip( clip, CombineMode.Replace );
            }
        }

        /// <summary>
        /// Strikes out text with text style color using weavy line
        /// </summary>
        private void StrikeOut( IntPtr hdc, RectangleF rect )
        {
            using (Graphics g = Graphics.FromHdc( hdc ))
                g.DrawLine( new Pen( new SolidBrush( myStyle.EffectColor ) ), rect.Left, rect.Y + rect.Height/2, rect.Right, rect.Y + rect.Height/2 );
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            return new RichString( myOffset, myLength, myStyle, myText );
        }

        #endregion

        #region IComparable Members

        public int CompareTo( object obj )
        {
            if (!(obj is RichString))
                return -1;

            RichString s = (RichString) obj;

            return s.Offset - myOffset;
        }

        #endregion
    }
}