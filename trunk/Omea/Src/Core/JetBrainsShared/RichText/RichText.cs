/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using JetBrains.UI.Interop;

namespace JetBrains.UI.RichText
{

    #region RichTextParameters structure

    /// <summary>
    /// Contains parameters for a whole rich text block
    /// </summary>
    public class RichTextParameters
    {
        /// <summary>
        /// Font to use
        /// </summary>
        private Font myFont;

        /// <summary>
        /// Default text style
        /// </summary>
        private TextStyle myStyle;

        /// <summary>
        /// Gets or sets used font
        /// </summary>
        public Font Font
        {
            get { return myFont; }
            set { myFont = value; }
        }

        /// <summary>
        /// Gets or sets default text style
        /// </summary>
        public TextStyle Style
        {
            get { return myStyle; }
            set { myStyle = value; }
        }

        /// <summary>
        /// Creates new rich text parameters
        /// </summary>
        public RichTextParameters()
        {
        }

        /// <summary>
        /// Creates new rich text parameters
        /// </summary>
        /// <param name="font">Font to use</param>
        public RichTextParameters( Font font )
        {
            myFont = font;
            myStyle = TextStyle.DefaultStyle;
        }

        /// <summary>
        /// Creates new rich text parameters
        /// </summary>
        /// <param name="font">Font to use</param>
        /// <param name="style">Default text style</param>
        public RichTextParameters( Font font, TextStyle style )
        {
            myFont = font;
            myStyle = style;
        }
    }

    #endregion

    /// <summary>
    /// Represents a formatted text block (i.e., actually, sequence of <see cref="RichString"/> instances).
    /// </summary>
    public class RichText : ICloneable
    {
        private class TextRangeDataRecord
        {
            private int myStartOffset;
            private int myEndOffset;
            private object myObject;

            public int StartOffset
            {
                get { return myStartOffset; }
            }

            public int EndOffset
            {
                get { return myEndOffset; }
            }

            public object Object
            {
                get { return myObject; }
            }

            public TextRangeDataRecord( int startOffset, int endOffset, object @object )
            {
                myStartOffset = startOffset;
                myEndOffset = endOffset;
                myObject = @object;
            }
        }

        #region Fields

        /// <summary>
        /// Text formatting options
        /// </summary>
        private RichTextParameters myParameters;

        /// <summary>
        /// Parts of the text
        /// </summary>
        private ArrayList myParts = new ArrayList( 1 );

        /// <summary>
        /// String contents
        /// </summary>
        private string myString = "";

        /// <summary>
        /// Keeps user data attached to ranges
        /// </summary>
        private ArrayList myData = new ArrayList();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets used parameters
        /// </summary>
        public RichTextParameters Parameters
        {
            get { return myParameters; }
            set { myParameters = value; }
        }

        #endregion

        #region Size cache

        private SizeF mySize = SizeF.Empty;
        private bool mySizeIsValid = false;

        #endregion

        #region User data

        public void PutUserData( int startOffset, int endOffset, object data )
        {
            myData.Add( new TextRangeDataRecord( startOffset, endOffset, data ) );
        }

        public object[] GetUserData( int startOffset, int endOffset )
        {
            ArrayList data = new ArrayList();

            foreach( TextRangeDataRecord textRangeDataRecord in myData )
            {
                if (textRangeDataRecord.StartOffset <= startOffset && textRangeDataRecord.EndOffset >= endOffset)
                    data.Add( textRangeDataRecord.Object );
            }

            return (object[]) data.ToArray( typeof (object) );
        }

        #endregion

        /// <summary>
        /// Creates a new rich text block
        /// </summary>
        /// <param name="s">Text content</param>
        /// <param name="parameters">Parameters to use</param>
        public RichText( string s, RichTextParameters parameters )
        {
            myParameters = parameters;

            if (s != null && s.Length > 0)
            {
                myString = s;
                myParts.Add( new RichString( 0, s.Length, parameters.Style, this ) );
            }
        }

        private RichText( string s, RichTextParameters parameters, IList parts )
        {
            myParameters = parameters;
            myString = s;
            myParts = new ArrayList( parts.Count );
            for (int i = 0; i < parts.Count; i++)
                myParts.Add( ((RichString) parts[ i ]).Clone() );
        }

        /// <summary>
        /// Adds a part with a custom style to the text
        /// </summary>
        /// <param name="s">The part to add</param>
        /// <param name="style">The style of the part to add</param>
        public void Append( string s, TextStyle style )
        {
            myParts.Add( new RichString( myString.Length, s.Length, style, this ) );
            myString += s;

            mySizeIsValid = false;
        }

        /// <summary>
        /// Adds part to the text
        /// </summary>
        /// <param name="s">The part to add</param>
        public void Append( string s )
        {
            myParts.Add( new RichString( myString.Length, s.Length, myParameters.Style, this ) );
            myString += s;

            mySizeIsValid = false;
        }

        /// <summary>
        /// Appends one rich text to another
        /// </summary>
        /// <param name="richText">The rich text to append</param>
        public void Append( RichText richText )
        {
            foreach( RichString part in richText.myParts )
                myParts.Add( part );

            myString += richText.myString;
            mySizeIsValid = false;
        }

        /// <summary>
        /// Gets plain string representation of rich text
        /// </summary>
        /// <returns>Plain string representation of rich text</returns>
        public override string ToString()
        {
            string s = "";

            for (int i = 0; i < myParts.Count; i++)
            {
                if (i > 0)
                    s += "|";

                s += ((RichString) myParts[ i ]).PartText;
            }

            return s;
        }

        /// <summary>
        /// Gets the underlying text
        /// </summary>
        public string Text
        {
            get { return myString; }
        }

        /// <summary>
        /// Gets total length of the text in characters
        /// </summary>
        public int Length
        {
            get { return myString.Length; }
        }

        public int GetCharByOffset( int x, IntPtr hDC )
        {
            if (x < 0)
                return -1;

            int currentX = 0;
            int currentChar = 0;

            foreach( RichString part in myParts )
            {
                SizeF size = part.GetSize( hDC, myParameters );

                if (currentX + (int) size.Width > x)
                    return currentChar + part.GetSymbolByOffset( x - currentX, myParameters, hDC );

                currentX += (int) size.Width;
                currentChar += part.Length;
            }

            return -1;
        }

        /// <summary>
        /// Gets size of the text in the given graphics
        /// </summary>
        /// <param name="hdc">The device context to calculate size in</param>
        /// <returns>Size of the string when drawn in a given graphics</returns>
        /// <exception cref="ArgumentNullException"><i>g</i> is null.</exception>
        public SizeF GetSize( IntPtr hdc )
        {
            if (!mySizeIsValid)
            {
                float width = 0, height = 0;

                foreach( RichString s in myParts )
                {
                    SizeF size = s.GetSize( hdc, myParameters );

                    width += size.Width;
                    height = Math.Max( height, size.Height );
                }

                mySize = new SizeF( width, height );
                mySizeIsValid = true;
            }

            return mySize;
        }

        /// <summary>
        /// Get the size on the screen HDC
        /// </summary>
        /// <returns></returns>
        public Size GetSize()
        {
            IntPtr hDC = Win32Declarations.GetDC( IntPtr.Zero );
            try
            {
                return GetSize( hDC ).ToSize();
            }
            finally
            {
                Win32Declarations.ReleaseDC( IntPtr.Zero, hDC );
            }
        }

        /// <summary>
        /// Draws the formatted string on a given device contect (HDC)
        /// </summary>
        /// <param name="hdc">The device context to draw the string in.</param>
        /// <param name="rect">The rectangle where the string is drawn.</param>
        /// <exception cref="ArgumentNullException"><i>g</i> is null</exception>
        public void Draw( IntPtr hdc, Rectangle rect )
        {
            foreach( RichString s in myParts )
            {
                int stringWidth = s.Draw( hdc, rect, myParameters );
                rect = new Rectangle( rect.Left + stringWidth, rect.Top, rect.Width - stringWidth, rect.Height );
            }
        }

        /// <summary>
        /// Draws the formatted string on a given graphics
        /// </summary>
        /// <param graphics device context to draw the string in.</param>
        /// <param name="rect">The rectangle where the string is drawn.</param>
        /// <exception cref="ArgumentNullException"><i>g</i> is null</exception>
        public void Draw( Graphics graphics, Rectangle rect )
        {
            IntPtr hdc = graphics.GetHdc();
            try
            {
                Draw( hdc, rect );
            }
            finally
            {
                graphics.ReleaseHdc( hdc );
            }
        }

        /// <summary>
        /// Draws the formatted string on a given graphics, clipping the text by the
        /// ClipBounds set on the Graphics.
        /// </summary>
        /// <param graphics device context to draw the string in.</param>
        /// <param name="rect">The rectangle where the string is drawn.</param>
        /// <exception cref="ArgumentNullException"><i>g</i> is null</exception>
        public void DrawClipped( Graphics graphics, Rectangle rect )
        {
            RectangleF rcClip = graphics.ClipBounds;
            IntPtr hdc = graphics.GetHdc();
            try
            {
                IntPtr clipRgn = Win32Declarations.CreateRectRgn( 0, 0, 0, 0 );
                if (Win32Declarations.GetClipRgn( hdc, clipRgn ) != 1)
                {
                    Win32Declarations.DeleteObject( clipRgn );
                    clipRgn = IntPtr.Zero;
                }
                Win32Declarations.IntersectClipRect( hdc, (int) rcClip.Left, (int) rcClip.Top,
                                                     (int) rcClip.Right, (int) rcClip.Bottom );

                Draw( hdc, rect );

                Win32Declarations.SelectClipRgn( hdc, clipRgn );
                Win32Declarations.DeleteObject( clipRgn );
            }
            finally
            {
                graphics.ReleaseHdc( hdc );
            }
        }

        /// <summary>
        /// Sets new colors for the whole text
        /// </summary>
        /// <param name="foreColor">Foreground color to set</param>
        /// <param name="backColor">Background color to set</param>
        public void SetColors( Color foreColor, Color backColor )
        {
            foreach( RichString s in myParts )
            {
                TextStyle style = s.Style;

                style.BackgroundColor = backColor;
                style.ForegroundColor = foreColor;
                style.EffectColor = foreColor;

                s.Style = style;
            }

            mySizeIsValid = false;
        }

        public void SetColors( Color foreColor, Color backColor, int startOffset, int length )
        {
            foreach( RichString s in GetStringsInRange( startOffset, length ) )
            {
                TextStyle style = s.Style;

                style.BackgroundColor = backColor;
                style.ForegroundColor = foreColor;
                style.EffectColor = foreColor;

                s.Style = style;
            }

            mySizeIsValid = false;
        }

        /// <summary>
        /// Sets new style to a specified part of the text
        /// </summary>
        /// <param name="style">The style to set</param>
        /// <param name="startOffset">Start offset of the block</param>
        /// <param name="length">Block length</param>
        /// <exception cref="ArgumentOutOfRangeException"><i>startOffset</i> is invalid in current string</exception>
        /// <exception cref="ArgumentOutOfRangeException"><i>length</i> is invalid starting with specified <i>startOffset</i></exception>
        public void SetStyle( TextStyle style, int startOffset, int length )
        {
            foreach( RichString s in GetStringsInRange( startOffset, length ) )
            {
                s.Style = style;
            }

            mySizeIsValid = false;
        }

        /// <summary>
        /// Sets new font style to a specified part of the text
        /// </summary>
        /// <param name="style">The font style to set</param>
        /// <param name="startOffset">Start offset of the block</param>
        /// <param name="length">Block length</param>
        /// <exception cref="ArgumentOutOfRangeException"><i>startOffset</i> is invalid in current string</exception>
        /// <exception cref="ArgumentOutOfRangeException"><i>length</i> is invalid starting with specified <i>startOffset</i></exception>
        public void SetStyle( FontStyle style, int startOffset, int length )
        {
            foreach( RichString s in GetStringsInRange( startOffset, length ) )
            {
                s.Style = new TextStyle( style, s.Style.ForegroundColor, s.Style.BackgroundColor, s.Style.Effect, s.Style.EffectColor );
            }

            mySizeIsValid = false;
        }

        /// <summary>
        /// Sets new effect to a specified part of the text
        /// </summary>
        /// <param name="effect">The effect style to set</param>
        /// <param name="effectColor">The effect color to set</param>
        /// <param name="startOffset">Start offset of the block</param>
        /// <param name="length">Block length</param>
        /// <exception cref="ArgumentOutOfRangeException"><i>startOffset</i> is invalid in current string</exception>
        /// <exception cref="ArgumentOutOfRangeException"><i>length</i> is invalid starting with specified <i>startOffset</i></exception>
        public void SetStyle( TextStyle.EffectStyle effect, Color effectColor, int startOffset, int length )
        {
            foreach( RichString s in GetStringsInRange( startOffset, length ) )
            {
                s.Style = new TextStyle( s.Style.FontStyle, s.Style.ForegroundColor, s.Style.BackgroundColor, effect, effectColor );
            }

            mySizeIsValid = false;
        }

        /// <summary>
        /// Splits rich text at the specified offset
        /// </summary>
        /// <param name="offset">The offset to split the text at</param>
        /// <returns>Array of the result parts</returns>
        public RichText[] Split( int offset )
        {
            ArrayList firstPart = new ArrayList();
            ArrayList secondPart = new ArrayList();

            foreach( RichString s in myParts )
            {
                if (s.Offset + s.Length <= offset)
                    firstPart.Add( s );
                else if (s.Offset >= offset)
                    secondPart.Add( s );
                else
                {
                    RichString[] parts = BreakString( s, offset - s.Offset, false );

                    firstPart.Add( parts[ 0 ] );
                    secondPart.Add( parts[ 1 ] );
                }
            }

            return new RichText[]
                {
                    CreateRichTextFromParts( (RichString[]) firstPart.ToArray( typeof (RichString) ) ),
                    CreateRichTextFromParts( (RichString[]) secondPart.ToArray( typeof (RichString) ) )
                };
        }

        #region Private methods

        private RichText CreateRichTextFromParts( RichString[] parts )
        {
            if (parts.Length == 0)
                return new RichText( "", myParameters );

            string text = "";
            int startOffset = parts[ 0 ].Offset;

            foreach( RichString s in parts )
                text += Text.Substring( s.Offset, s.Length );

            RichText result = new RichText( text, myParameters );

            foreach( RichString s in parts )
                result.SetStyle( s.Style, s.Offset - startOffset, s.Length );

            return result;
        }

        /// <summary>
        /// Gets array of parts which lie in the specified range
        /// </summary>
        private RichString[] GetStringsInRange( int startOffset, int length )
        {
            if (startOffset < 0)
                startOffset = 0;

            if (startOffset + length > myString.Length)
                length = myString.Length - startOffset;

            ArrayList strings = new ArrayList();

            int offset = startOffset;
            int endOffset = startOffset + length;

            while (offset < endOffset)
            {
                RichString s = GetPartByOffset( offset );

                Debug.Assert( s != null, "We've fixed the offsets so we should always find the corresponding string" );

                int startingOffset = GetStartingOffset( s );
                int localStartingOffset = offset - startingOffset;

                if (localStartingOffset > 0)
                {
                    s = BreakString( s, localStartingOffset, true )[ 1 ];
                    localStartingOffset = 0;
                }

                int localEndingOffset = localStartingOffset + length;

                if (localEndingOffset < s.Length)
                {
                    s = BreakString( s, localEndingOffset, true )[ 0 ];
                    localEndingOffset = s.Length;
                }

                strings.Add( s );

                offset += s.Length;
            }

            return (RichString[]) strings.ToArray( typeof (RichString) );
        }

        /// <summary>
        /// Gets starting offset of a given part in global coordinates
        /// </summary>
        private int GetStartingOffset( RichString part )
        {
            int currentOffset = 0;

            foreach( RichString s in myParts )
            {
                if (s == part)
                    return currentOffset;

                currentOffset += s.Length;
            }

            throw new ArgumentException( "There's no such part in this text" );
        }

        /// <summary>
        /// Breaks a part into two parts at given offset
        /// </summary>
        private RichString[] BreakString( RichString s, int offset, bool insertParts )
        {
            if (offset >= s.Length)
                return null;

            RichString[] parts = new RichString[2];

            parts[ 0 ] = new RichString( s.Offset, offset, s.Style, this );
            parts[ 1 ] = new RichString( s.Offset + offset, s.Length - offset, s.Style, this );

            if (insertParts)
            {
                int index = myParts.IndexOf( s );
                myParts.Remove( s );
                myParts.Insert( index, parts[ 0 ] );
                myParts.Insert( index + 1, parts[ 1 ] );
            }

            return parts;
        }

        /// <summary>
        /// Gets part which contains the specified offset
        /// </summary>
        private RichString GetPartByOffset( int offset )
        {
            int currentOffset = 0;

            foreach( RichString s in myParts )
            {
                currentOffset += s.Length;

                if (currentOffset > offset)
                    return s;
            }

            return null;
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            RichText clone = new RichText( myString, myParameters, myParts );
            return clone;
        }

        public int GetCharByOffset( Point point, IntPtr hdc )
        {
            SizeF size = GetSize( hdc );

            if (point.Y < 0 || point.Y > size.Height)
                return -1;

            return GetCharByOffset( point.X, hdc );
        }

        #endregion    
    }
}