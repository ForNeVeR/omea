// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.JetListViewLibrary
{
    /// <summary>
    /// Contains platform-dependent functions for drawing different elements of JetListView.
    /// </summary>
    public interface IControlPainter
    {
        void DrawFocusRect( Graphics g, Rectangle rc );
        int DrawText( Graphics g, string text, Font font, Color color, Rectangle rc, StringFormat format );
        Size MeasureText( string text, Font font );
        Size MeasureText( Graphics g, string text, Font font );
        Size MeasureText( Graphics g, string text, Font font, int maxWidth );
        void DrawCheckBox( Graphics g, Rectangle rc, ButtonState state );
        int GetListViewBorderSize( BorderStyle borderStyle );
        void DrawListViewBorder( Graphics g, Rectangle rc, BorderStyle borderStyle );
        void DrawTreeIcon( Graphics g, Rectangle rc, bool expanded );
        Size GetTreeIconSize( Graphics g, Rectangle rc );
    }

    /// <summary>
	/// Provides an implementation of IControlPainter which uses only standard
	/// System.Drawing and System.Windows.Forms functions for drawing.
	/// </summary>
	public class DefaultControlPainter: IControlPainter
	{
        public void DrawFocusRect( Graphics g, Rectangle rc )
        {
            ControlPaint.DrawFocusRectangle( g, rc );
        }

        public int DrawText( Graphics g, string text, Font font, Color color, Rectangle rc, StringFormat format )
        {
            RectangleF rcf = new RectangleF( rc.Left, rc.Top, rc.Width, rc.Height );
            using( Brush b = new SolidBrush( color ) )
            {
                g.DrawString( text, font, b, rcf, format );
                return MeasureText( g, text, font ).Height;
            }
        }

        public Size MeasureText( string text, Font font )
        {
            using( Graphics g = Graphics.FromHwnd( IntPtr.Zero ) )
            {
                return MeasureText( g, text, font );
            }
        }

        public Size MeasureText( Graphics g, string text, Font font )
        {
            SizeF result = g.MeasureString( text, font );
            return new Size( (int) result.Width, (int) result.Height );
        }

        public Size MeasureText( Graphics g, string text, Font font, int maxWidth )
        {
            SizeF result = g.MeasureString( text, font, new SizeF( maxWidth, Screen.PrimaryScreen.Bounds.Height ) );
            return new Size( (int) result.Width, (int) result.Height );
        }

        public void DrawCheckBox( Graphics g, Rectangle rc, ButtonState state )
        {
            ControlPaint.DrawCheckBox( g, rc, state );
        }

        public void DrawListViewBorder( Graphics g, Rectangle rc, BorderStyle borderStyle )
        {
            if ( borderStyle == BorderStyle.FixedSingle  )
            {
                ControlPaint.DrawBorder3D( g, rc, Border3DStyle.Flat );
            }
            else if ( borderStyle == BorderStyle.Fixed3D )
            {
                ControlPaint.DrawBorder3D( g, rc );
            }
        }

        public int GetListViewBorderSize( BorderStyle borderStyle )
        {
            if ( borderStyle == BorderStyle.None )
                return 0;
            return (borderStyle == BorderStyle.FixedSingle) ? 1 : 2;
        }

        public void DrawTreeIcon( Graphics g, Rectangle rc, bool expanded )
        {
            DoDrawTreeIcon( g, rc, expanded );
        }

        internal static void DoDrawTreeIcon( Graphics g, Rectangle rc, bool expanded )
        {
            int midX = (rc.Left + rc.Right) / 2;
            int midY = (rc.Top + rc.Bottom) / 2;

            g.DrawRectangle( SystemPens.GrayText, new Rectangle( rc.Left, rc.Top, rc.Width-1, rc.Height-1 ) );
            g.DrawLine( SystemPens.WindowText, rc.Left + 2, midY, rc.Right - 3, midY );
            if ( !expanded )
            {
                g.DrawLine( SystemPens.WindowText, midX, rc.Top + 2, midX, rc.Bottom - 3 );
            }
        }

        public Size GetTreeIconSize( Graphics g, Rectangle rc )
        {
            return DoGetTreeIconSize( rc );
        }

        internal static Size DoGetTreeIconSize( Rectangle rc )
        {
            int plusSize = Math.Min( rc.Width, rc.Height ) / 2;
            if ( plusSize % 2 == 0 )
            {
                plusSize++;
            }
            return new Size( plusSize, plusSize );
        }
	}
}
