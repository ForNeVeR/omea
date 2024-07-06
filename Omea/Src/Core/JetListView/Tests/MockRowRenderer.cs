// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using SP.Windows;

namespace JetBrains.JetListViewLibrary.Tests
{
	/// <summary>
	/// Summary description for MockRowRenderer.
	/// </summary>
    internal class MockRowRenderer: IRowRenderer
    {
        private ArrayList _drawOperations = new ArrayList();
	    private Point _lastMouseDownPoint;
        private JetListViewNode _lastMouseDownNode;
        private string _searchHighlightText;
        private int _rowHeight;
        private JetListViewColumnCollection _columnCollection;

	    public MockRowRenderer()
	    {
	    }

	    public MockRowRenderer( JetListViewColumnCollection columnCollection )
	    {
	        _columnCollection = columnCollection;
	    }

	    public event EventHandler ScrollRangeChanged;
        public event EventHandler Invalidate;
        public event RowHeightChangedEventHandler RowHeightChanged;
        public event EventHandler AllRowsHeightChanged;
        public event RequestScrollEventHandler RequestScroll;

        public int RowHeight
	    {
	        get { return _rowHeight; }
	        set { _rowHeight = value; }
	    }

	    public int GetRowHeight( JetListViewNode node )
	    {
	        return _rowHeight;
	    }

	    public int AllRowsHeight
	    {
	        get { return _rowHeight; }
	    }

	    public ArrayList DrawOperations
        {
            get { return _drawOperations; }
        }

        public int ScrollOffset
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public Point LastMouseDownPoint
        {
            get { return _lastMouseDownPoint; }
        }

        public JetListViewNode LastMouseDownNode
        {
            get { return _lastMouseDownNode; }
        }

        public void DrawRow( Graphics g, Rectangle rc, JetListViewNode itemNode, RowState state )
        {
            _drawOperations.Add( new DrawRowOperation( rc, itemNode, state ) );
        }

        public MouseHandleResult HandleMouseDown( JetListViewNode node, int x, int y, MouseButtons button, Keys modifiers )
        {
            _lastMouseDownPoint = new Point( x, y );
            _lastMouseDownNode = node;
            return 0;
        }

        public bool HandleMouseUp( JetListViewNode node, int x, int y, MouseButtons button, Keys modifiers )
        {
            return false;
        }

        public bool HandleKeyDown( JetListViewNode node, KeyEventArgs e )
        {
            return false;
        }

	    public void HandleDoubleClick( JetListViewNode node )
	    {
	    }

	    public bool AcceptDoubleClick( JetListViewNode node, int x, int y )
	    {
	        return true;
	    }

	    public void SizeColumnsToContent( HashSet addedNodes, HashSet removedNodes, HashSet changedNodes )
        {
        }

        public void ProcessNodeExpanded( JetListViewNode node )
        {
        }

        public void ProcessNodeCollapsed( JetListViewNode node )
        {
        }

        public string SearchHighlightText
        {
            get { return _searchHighlightText; }
            set { _searchHighlightText = value; }
        }

	    public Header HeaderControl
	    {
	        get { throw new NotImplementedException(); }
	        set { throw new NotImplementedException(); }
	    }

	    public IControlMethodInvoker MethodInvoker
	    {
	        get { throw new NotImplementedException(); }
	        set { throw new NotImplementedException(); }
	    }

	    public JetListViewColumn GetColumnAt( JetListViewNode node, int x, int y )
	    {
	        return null;
	    }

	    public Rectangle GetColumnBounds( JetListViewColumn col, JetListViewNode node )
	    {
	        throw new NotImplementedException();
	    }

	    public JetListViewColumn GetInPlaceEditColumn( JetListViewNode node )
	    {
	        throw new NotImplementedException();
	    }

	    public void UpdateItem( object item )
	    {
	        throw new NotImplementedException();
	    }

	    public bool MatchIncrementalSearch( JetListViewNode node, string text )
	    {
            foreach( JetListViewColumn col in _columnCollection )
            {
                if ( col.MatchIncrementalSearch( node, text ) )
                {
                    return true;
                }
            }
            return false;
	    }

	    public int ScrollRange
	    {
	        get { throw new NotImplementedException(); }
	    }
	    public int VisibleWidth
	    {
	        get { throw new NotImplementedException(); }
	        set { throw new NotImplementedException(); }
	    }
	    public int BorderSize
	    {
	        get { throw new NotImplementedException(); }
	        set { throw new NotImplementedException(); }
	    }
	    public Control OwnerControl
	    {
	        get { throw new NotImplementedException(); }
	        set { throw new NotImplementedException(); }
	    }
	    public IControlPainter ControlPainter
	    {
	        get { throw new NotImplementedException(); }
	        set { throw new NotImplementedException(); }
	    }
	    public bool FullRowSelect
	    {
	        get { throw new NotImplementedException(); }
	        set { throw new NotImplementedException(); }
	    }
    }

    internal class DrawRowOperation
    {
        public Rectangle Rect;
        public JetListViewNode ItemNode;
        public RowState State;

        public DrawRowOperation( Rectangle rect, JetListViewNode itemNode, RowState state )
        {
            Rect = rect;
            ItemNode = itemNode;
            State = state;
        }
    }
}
