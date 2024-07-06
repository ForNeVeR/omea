// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// The default implementation of the class which renders and handles events for group
	/// header rows.
	/// </summary>
	internal class DefaultGroupRenderer: IGroupRenderer
	{
        private NodeGroupCollection _groupCollection;
        private IControlPainter _controlPainter = new DefaultControlPainter();
        private Font _headerFont = new Font( "Tahoma", 8, FontStyle.Bold );
        private int _visibleWidth;
        private Color _groupHeaderColor = SystemColors.Control;

	    public DefaultGroupRenderer( NodeGroupCollection groupCollection )
	    {
	        _groupCollection = groupCollection;
	    }

	    public IControlPainter ControlPainter
	    {
	        get { return _controlPainter; }
	        set { _controlPainter = value; }
	    }

	    public int VisibleWidth
	    {
	        get { return _visibleWidth; }
	        set { _visibleWidth = value; }
	    }

	    public Color GroupHeaderColor
	    {
	        get { return _groupHeaderColor; }
	        set { _groupHeaderColor = value; }
	    }

	    public void DrawGroupHeader( Graphics g, Rectangle rc, GroupHeaderNode node, RowState rowState )
	    {
            Rectangle rcFill = rc;
            rcFill.Height--;
            Color textColor;
            if ( ( rowState & RowState.ActiveSelected ) != 0 )
            {
                g.FillRectangle( SystemBrushes.Highlight, rcFill );
                textColor = SystemColors.HighlightText;
            }
            else
            {
                using( Brush b = new SolidBrush( _groupHeaderColor ) )
                {
                    g.FillRectangle( b, rcFill );
                }
                textColor = SystemColors.ControlText;
            }

            Rectangle rcIcon = new Rectangle( 0, rc.Top, GroupHeaderHeight, GroupHeaderHeight );
            rcIcon.Inflate( -2, -2 );
            _controlPainter.DrawTreeIcon( g, rcIcon, node.Expanded );

            Rectangle rcText = new Rectangle( 20, rc.Top, _visibleWidth-20, rc.Height );

            StringFormat fmt = new StringFormat();
            fmt.FormatFlags = StringFormatFlags.NoWrap;
            fmt.LineAlignment = StringAlignment.Center;
            _controlPainter.DrawText( g, node.Text, _headerFont, textColor, rcText, fmt );

            if ( ( rowState & RowState.Focused ) != 0 )
            {
                _controlPainter.DrawFocusRect( g, new Rectangle( 0, rc.Top, _visibleWidth, rc.Height ) );
            }
	    }

	    public bool HandleMouseDown( GroupHeaderNode node, int x, int y, MouseButtons button, Keys modifiers )
	    {
            Rectangle rcIcon = new Rectangle( 0, 0, GroupHeaderHeight, GroupHeaderHeight );
            rcIcon.Inflate( -2, -2 );
            if ( rcIcon.Contains( x, y ) )
            {
                node.Expanded = !node.Expanded;
                return true;
            }
            return false;
	    }

	    public bool HandleGroupKeyDown( GroupHeaderNode node, KeyEventArgs e )
	    {
	        if ( e.KeyCode == Keys.Add || e.KeyCode == Keys.Right )
	        {
	            node.Expanded = true;
                return true;
	        }
            if ( e.KeyCode == Keys.Subtract || e.KeyCode == Keys.Left )
            {
                node.Expanded = false;
                return true;
            }
            return false;
        }

	    public bool HandleNodeKeyDown( JetListViewNode viewNode, KeyEventArgs e )
	    {
            if ( e.KeyCode == Keys.Subtract || e.KeyCode == Keys.Left )
            {
                _groupCollection.GetNodeGroupHeader( viewNode ).Expanded = false;
                return true;
            }
            return false;
        }

	    public int GroupHeaderHeight
	    {
	        get { return 17; }
	    }
	}
}
