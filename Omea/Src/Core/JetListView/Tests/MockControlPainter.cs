// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.JetListViewLibrary.Tests
{
    internal class MockControlPainter : IControlPainter
    {
        private Hashtable _expectedSizes = new Hashtable();

        public void SetExpectedSize( string text, Size size )
        {
            _expectedSizes[ text ] = size;
        }

        public void DrawFocusRect( Graphics g, Rectangle rc )
        {
        }

        public int DrawText( Graphics g, string text, Font font, Color color, Rectangle rc, StringFormat format )
        {
            return 0;
        }

        public Size MeasureText( string text, Font font )
        {
            return MeasureText( null, text, font );
        }

        public Size MeasureText( Graphics g, string text, Font font )
        {
            if (_expectedSizes.ContainsKey( text ))
                return (Size) _expectedSizes[ text ];
            return new Size( 0, 0 );
        }

        public void DrawCheckBox( Graphics g, Rectangle rc, ButtonState state )
        {
        }

        public int GetListViewBorderSize( BorderStyle borderStyle )
        {
            return 0;
        }

        public void DrawListViewBorder( Graphics g, Rectangle rc, BorderStyle borderStyle )
        {
        }

        public void DrawTreeIcon( Graphics g, Rectangle rc, bool expanded )
        {
        }

        public Size GetTreeIconSize( Graphics g, Rectangle rc )
        {
            return new Size( 0, 0 );
        }

        public Size MeasureText( Graphics g, string text, Font font, int maxWidth )
        {
            return new Size( 0, 0 );
        }
    }
}
