// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using JetBrains.UI.Interop;

namespace JetBrains.UI.RichText
{
    /// <summary>
    /// Cache for fonts.
    /// </summary>
    public class FontCache : IDisposable
    {
        private ArrayList _fontList = new ArrayList();
        private ArrayList _fontFamilies = new ArrayList();
        private ArrayList _fontHandleList = new ArrayList();

        public FontCache()
        {
        }

        public void Dispose()
        {
            foreach( IntPtr fontHandle in _fontHandleList )
            {
                Win32Declarations.DeleteObject( fontHandle );
            }
            _fontHandleList.Clear();
            _fontList.Clear();
            _fontFamilies.Clear();
        }

        public Font GetFont( Font baseFont, FontStyle style )
        {
            lock( _fontList )
            {
                for (int i = 0; i < _fontList.Count; i++)
                {
                    Font font = (Font) _fontList[ i ];

                    if ( _fontFamilies[ i ].Equals( baseFont.FontFamily ) && font.Style == style
                        && Math.Abs( font.Size - baseFont.Size ) < 0.26)
                        return font;
                }

                try
                {
                    Font newFont = new Font( baseFont, style );
                    _fontList.Add( newFont );
                    _fontFamilies.Add( newFont.FontFamily );
                    _fontHandleList.Add( newFont.ToHfont() );
                    return newFont;
                }
                catch( ArgumentException /*e*/ ) // If font doesn't support specified style
                {
                    return baseFont;
                }
            }
        }

        public IntPtr GetHFont( Font font )
        {
            lock( _fontList )
            {
                for (int i = 0; i < _fontList.Count; i++)
                {
                    Font aFont = (Font) _fontList[ i ];

                    if ( _fontFamilies[ i ].Equals( font.FontFamily ) && aFont.Style == font.Style && Math.Abs( aFont.Size - font.Size ) < 0.26)
                        return (IntPtr) _fontHandleList[ i ];
                }

                IntPtr hFont = font.ToHfont();

                _fontList.Add( font );
                _fontFamilies.Add( font.FontFamily );
                _fontHandleList.Add( hFont );
                return hFont;
            }
        }

        public IntPtr GetHFont( Font font, FontStyle fontStyle )
        {
            lock( _fontList )
            {
                for (int i = 0; i < _fontList.Count; i++)
                {
                    Font aFont = (Font) _fontList[ i ];

                    if ( _fontFamilies[ i ].Equals( font.FontFamily ) && aFont.Style == fontStyle && Math.Abs( aFont.Size - font.Size ) < 0.26)
                        return (IntPtr) _fontHandleList[ i ];
                }

                font = new Font( font, fontStyle );
                IntPtr hFont = font.ToHfont();

                _fontList.Add( font );
                _fontFamilies.Add( font.FontFamily );
                _fontHandleList.Add( hFont );

                return hFont;
            }
        }
    }
}
