// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea
{
	/// <summary>
	/// The background of the vertical sidebar.
	/// </summary>
	internal class SidebarBackground: Panel
	{
        private ColorScheme _colorScheme;
        private string _colorSchemeKey = "Sidebar.Background";
        private int _fillHeight = 150;

        public SidebarBackground()
        {
            SetStyle( ControlStyles.ResizeRedraw, true );
            SetStyle( ControlStyles.Opaque, false );
        }

        public event PaintEventHandler PaintSidebarBackground;

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

	    public string ColorSchemeKey
	    {
	        get { return _colorSchemeKey; }
	        set { _colorSchemeKey = value; }
	    }

	    public int FillHeight
	    {
	        get { return _fillHeight; }
	        set { _fillHeight = value; }
	    }

	    protected override void OnPaintBackground( PaintEventArgs pevent )
        {
            base.OnPaintBackground( pevent );
            Rectangle rcFill;
            if ( _fillHeight > 0 )
            {
                rcFill = new Rectangle( 0, 0, ClientRectangle.Width, 150 );
            }
            else
            {
                rcFill = ClientRectangle;
            }
            Brush backBrush = ColorScheme.GetBrush( _colorScheme, _colorSchemeKey,
                rcFill, SystemBrushes.Control );
            pevent.Graphics.FillRectangle( backBrush, rcFill );

            if ( PaintSidebarBackground != null )
            {
                PaintSidebarBackground( this, pevent );
            }
        }
	}
}

