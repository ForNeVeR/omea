// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// The column which draws item lines and expand/collapse signs in JetListView.
	/// </summary>
	public class TreeStructureColumn: JetListViewColumn
	{
        // the width of the indent column is used as an indent step
        private const int _cColumnWidth = 17;

	    private bool _showLines = true;
        private Pen _linePen;
        private int _indent;  // Width is the width of the last indent step, while Indent is the width of all steps before the last one

        public TreeStructureColumn()
	    {
            Width = _cColumnWidth;

            _linePen = new Pen( SystemColors.GrayText );
            _linePen.DashStyle = DashStyle.Dot;
            _showHeader = false;
        }

	    public override void Dispose()
	    {
	        if ( Owner.OwnerControl != null )
	        {
                Owner.OwnerControl.NodeCollection.NodeAdded -= HandleStructureChanged;
                Owner.OwnerControl.NodeCollection.NodeRemoved -= HandleStructureChanged;
	        }
            base.Dispose();
	    }

	    protected override void OnWidthChanged()
	    {
	        base.OnWidthChanged();
            _indent = Width;
	    }

	    public int Indent
	    {
	        get { return _indent; }
	        set { _indent = value; }
	    }

	    protected override void SetOwner( JetListViewColumnCollection value )
	    {
	        base.SetOwner( value );
            if ( value != null && value.OwnerControl != null )
            {
                value.OwnerControl.NodeCollection.NodeAdded += HandleStructureChanged;
                value.OwnerControl.NodeCollection.NodeRemoved += HandleStructureChanged;
            }
	    }

	    /// <summary>
	    /// Gets or sets a value indicating whether lines are drawn between tree nodes.
	    /// </summary>
        public bool ShowLines
	    {
	        get { return _showLines; }
	        set { _showLines = value; }
	    }

	    public override bool IsIndentColumn()
	    {
	        return true;
	    }

	    public override int GetIndent( JetListViewNode node )
	    {
	        return node.Level * _indent + Width;
	    }

	    protected internal override void DrawNode( Graphics g, Rectangle rc, JetListViewNode node,
            RowState state, string highlightText )
	    {
            if ( Owner == null || OwnerControl == null )
                return;

            Rectangle lastRect = new Rectangle( rc.Right - Width, rc.Top, Width, rc.Height );
            Rectangle iconRect = lastRect;

            int midX = (lastRect.Left + lastRect.Right) / 2;
            int midY = (lastRect.Top + lastRect.Bottom) / 2;
            if ( _showLines && ( midY + OwnerControl.VScrollbar.Value ) % 2 == 1 )
            {
                midY--;
                iconRect.Offset( 0, -1 );
            }
            Size iconSize = OwnerControl.ControlPainter.GetTreeIconSize( g, lastRect );

            if ( node.CollapseState != CollapseState.NoChildren )
            {
                int left = midX - iconSize.Width/2;
                int top = midY - iconSize.Height/2;
                Rectangle rcIcon = new Rectangle( left, top, iconSize.Width, iconSize.Height );
                DrawStructureIcon( g, iconRect, rcIcon, node );
            }
            if ( _showLines )
            {
                int lineTop = rc.Top;
                if ( ( lineTop + OwnerControl.VScrollbar.Value )  % 2 == 1 )
                {
                    lineTop++;
                }

                if ( node.CollapseState != CollapseState.NoChildren )
                {
                    if ( node.Level > 0 || node.PrevFilteredNode != null )
                    {
                        g.DrawLine( _linePen, midX, lineTop, midX, midY - iconSize.Height/2 );
                    }
                    g.DrawLine( _linePen, midX + iconSize.Width/2, midY, lastRect.Right, midY );
                    if ( node.NextFilteredNode != null )
                    {
                        g.DrawLine( _linePen, midX, midY + iconSize.Height/2, midX, lastRect.Bottom );
                    }
                }
                else
                {
                    if ( node.Level > 0 || node.PrevFilteredNode != null )
                    {
                        g.DrawLine( _linePen, midX, lineTop, midX, midY );
                    }
                    if ( node.NextFilteredNode != null )
                    {
                        g.DrawLine( _linePen, midX, midY, midX, lastRect.Bottom );
                    }
                    g.DrawLine( _linePen, midX, midY, lastRect.Right, midY );
                }

                JetListViewNode curParent = node;
                for( int i=node.Level-1; i >= 0; i-- )
                {
                    curParent = curParent.Parent;
                    midX -= Width;
                    if ( curParent.NextFilteredNode != null )
                    {
                        g.DrawLine( _linePen, midX, lineTop, midX, lastRect.Bottom );
                    }
                }
            }
	    }

	    protected virtual void DrawStructureIcon( Graphics g, Rectangle rc, Rectangle rcIcon, JetListViewNode node )
	    {
	        OwnerControl.ControlPainter.DrawTreeIcon( g, rcIcon, ( node.CollapseState == CollapseState.Expanded ) );
	    }

	    protected internal override MouseHandleResult HandleMouseDown( JetListViewNode node, int x, int y )
	    {
            int itemIndent = _indent * node.Level;
            if ( node.HasChildren && x >= itemIndent && x < itemIndent + Width )
            {
                node.Expanded = !node.Expanded;
                return MouseHandleResult.Handled | MouseHandleResult.SuppressFocus;
            }
            return 0;
	    }

        protected internal override bool HandleMouseUp( JetListViewNode node, int x, int y )
	    {
            // selection can be changed not only in mouse down handler, but also in mouse up (OM-8807)
            int itemIndent = _indent * node.Level;
            if ( node.HasChildren && x >= itemIndent && x < itemIndent + Width )
            {
                return true;
            }
            return false;
        }

	    public override bool AcceptColumnDoubleClick
	    {
	        get { return false; }
	    }

	    public override bool HandleDoubleClick( JetListViewNode node )
	    {
	        if ( node.HasChildren )
	        {
	            node.Expanded = !node.Expanded;
                return true;
	        }
            return false;
	    }

	    protected internal override bool HandleKeyDown( JetListViewNode node, KeyEventArgs e )
	    {
	        if ( node.HasChildren )
	        {
                if ( e.KeyData == Keys.Add )
                {
                    node.Expanded = true;
                    return true;
                }
                if ( e.KeyData == Keys.Subtract )
                {
                    node.Expanded = false;
                    return true;
                }
                if ( e.KeyData == Keys.Multiply )
                {
                    node.ExpandAll();
                    return true;
                }
	        }
            if ( e.KeyData == Keys.Left )
            {
                if ( node.HasChildren && node.Expanded )
                {
                    node.Expanded = false;
                    return true;
                }
                else if ( node.Parent != null && node.Parent.Data != null )
                {
                    OwnerControl.Selection.Clear();
                    OwnerControl.Selection.Add( node.Parent.Data );
                    return true;
                }
                return false;
            }
            if ( e.KeyData == Keys.Right )
            {
                if ( node.HasChildren )
                {
                    if ( !node.Expanded )
                    {
                        node.Expanded = true;
                    }
                    else if ( node.ChildCount > 0 )
                    {
                        OwnerControl.Selection.Clear();
                        OwnerControl.Selection.Add( node.GetChildNode( 0 ).Data );
                    }
                }
                return true;
            }
            return false;
	    }

        public override string GetToolTip( JetListViewNode node, Rectangle rc, ref bool needPlace )
        {
            return null;
        }

	    private void HandleStructureChanged( object sender, JetListViewNodeEventArgs e )
	    {
            Owner.OwnerControl.Invalidate();
	    }
	}
}
