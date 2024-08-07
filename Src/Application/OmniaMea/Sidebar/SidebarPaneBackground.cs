// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea
{
	/// <summary>
	/// Summary description for SidebarPaneBackground.
	/// </summary>
	internal class SidebarPaneBackground: UserControl
	{
        private ColorScheme _colorScheme;

	    public SidebarPaneBackground()
	    {
	        SetStyle( ControlStyles.Selectable, false );
	    }

	    public void SetContents( Control contents )
        {
            Controls.Add( contents );
            contents.Bounds = new Rectangle( 1, 1, Width - 2, Height - 2 );
            contents.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
        }

	    public ColorScheme ColorScheme
	    {
	        get { return _colorScheme; }
	        set { _colorScheme = value; }
	    }

	    protected override void OnPaint( PaintEventArgs e )
	    {
	        base.OnPaint( e );
            Pen pen = ColorScheme.GetPen( _colorScheme, "PaneCaption.Border", Pens.Black );
            e.Graphics.DrawRectangle( pen, 0, 0, ClientRectangle.Width - 1, ClientRectangle.Height - 1 );
	    }
	}
}
