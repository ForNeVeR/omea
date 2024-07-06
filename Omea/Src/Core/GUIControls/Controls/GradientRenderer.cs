// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GUIControls.Controls
{
    public class GradientRenderer : ToolStripProfessionalRenderer
    {
        private readonly Color  _start, _end;

        public GradientRenderer( Color startColor, Color endColor )
        {
            _start = startColor;
            _end = endColor;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            base.OnRenderToolStripBackground( e );
            Rectangle rect = e.AffectedBounds;
            Paint( e.Graphics, rect, _start, _end, LinearGradientMode.Vertical );
        }

        public static void Paint( Graphics gr, Rectangle rect, Color start, Color end, LinearGradientMode mode )
        {
            using( Brush b = new LinearGradientBrush( rect, start, end, mode ) )
            {
                gr.FillRectangle( b, rect );
            }
        }
    }
}
