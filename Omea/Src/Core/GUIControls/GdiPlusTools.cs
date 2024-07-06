// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Assorted static utility functions for GDI+.
	/// </summary>
	public sealed class GdiPlusTools
	{
        private GdiPlusTools()
        {
        }

        public static GraphicsPath BuildRoundRectPath( Rectangle rc )
        {
            GraphicsPath gp = new GraphicsPath();

            int left = rc.Left;
            int top = rc.Top;
            int right = rc.Right-1;
            int bottom = rc.Bottom-1;
            int radius = 3; // round rect radius
            gp.AddLine( left + radius, top, right - radius, top );
            gp.AddArc( right - 2*radius, top, 2*radius, 2*radius, 270, 90 );
            gp.AddLine( right, top + radius, right, bottom - radius );
            gp.AddArc ( right - 2*radius, bottom - 2*radius, 2*radius, 2*radius, 0, 90 );
            gp.AddLine( right - radius, bottom, left + radius, bottom );
            gp.AddArc( left, bottom - 2*radius, 2*radius, 2*radius, 90, 90 );
            gp.AddLine( left, bottom - radius, left, top + radius );
            gp.AddArc( left, top, 2*radius, 2*radius, 180, 90 );
            gp.CloseFigure();

            return gp;
        }

        public static Color GetColorMult( Color baseColor, float coeff )
        {
            int red   = Math.Min( 255, (int) (baseColor.R * coeff) );
            int green = Math.Min( 255, (int) (baseColor.G * coeff) );
            int blue  = Math.Min( 255, (int) (baseColor.B * coeff) );
            return Color.FromArgb( red, green, blue );
        }
	}
}
