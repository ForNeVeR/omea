// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using SP.Windows;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// Decorator around a base row renderer which adds an auto-preview row to it.
	/// </summary>
	internal class AutoPreviewRowRenderer: IRowRenderer
	{
	    private readonly RowRendererBase _baseRowRenderer;
	    private readonly JetListViewPreviewColumn _previewColumn;

	    public AutoPreviewRowRenderer( RowRendererBase baseRowRenderer, JetListViewPreviewColumn previewColumn,
            JetListViewColumnCollection columnCollection )
	    {
	        _baseRowRenderer = baseRowRenderer;
            _baseRowRenderer.ScrollRangeChanged += new EventHandler( HandleBaseScrollRangeChanged );
            _baseRowRenderer.Invalidate += new EventHandler( HandleBaseInvalidate );
            _baseRowRenderer.RequestScroll += new RequestScrollEventHandler( HandleBaseRequestScroll );
	        _previewColumn = previewColumn;
            _previewColumn.Owner = columnCollection;
            _previewColumn.RowHeightChanged += new RowHeightChangedEventHandler( HandlePreviewRowHeightChanged );
            _previewColumn.AllRowsHeightChanged += new EventHandler( HandlePreviewAllRowsHeightChanged );
	    }

	    public int GetRowHeight( JetListViewNode node )
	    {
	        return _baseRowRenderer.GetRowHeight( node ) +
                _previewColumn.GetAutoPreviewHeight( node );
	    }

	    public int AllRowsHeight
	    {
	        get { return -1; }
	    }

	    public void DrawRow( Graphics g, Rectangle rc, JetListViewNode itemNode, RowState rowState )
	    {
            bool focusRow = false;
            bool dropTargetRow = false;
            if ( ( rowState & RowState.Focused) != 0 && _baseRowRenderer.SearchHighlightText != null &&
                _baseRowRenderer.SearchHighlightText.Length > 0 )
            {
                rowState |= RowState.IncSearchMatch;
            }

            if ( _baseRowRenderer.FullRowSelect )
            {
                focusRow = ((rowState & RowState.Focused ) != 0);
                dropTargetRow = ((rowState & RowState.DropTarget ) != 0);
                rowState &= ~(RowState.Focused | RowState.DropTarget);
            }

	        int baseHeight = _baseRowRenderer.GetRowHeight( itemNode );
	        Rectangle rcBase = new Rectangle( rc.Left, rc.Top, rc.Width, baseHeight );
            _baseRowRenderer.DrawRow( g, rcBase, itemNode, rowState );
            int indent = _baseRowRenderer.GetAutoPreviewIndent( itemNode );
            int previewWidth = _baseRowRenderer.ScrollRange - _baseRowRenderer.ScrollOffset;
            if ( previewWidth == 0 )
            {
                previewWidth = _baseRowRenderer.VisibleWidth;
            }

            Rectangle rcPreview = new Rectangle( indent, rc.Top + baseHeight,
                previewWidth + _baseRowRenderer.BorderSize - indent, rc.Height - baseHeight );
            Rectangle rcFocus = _baseRowRenderer.GetFocusRect( itemNode, rc );

            if ( rcPreview.Height > 0 )
            {
                if ( _baseRowRenderer.FullRowSelect )
                {
                    if ( (rowState & RowState.IncSearchMatch) != 0 &&
                        ((rowState & RowState.ActiveSelected) != 0) )
                    {
                        rowState &= ~RowState.ActiveSelected;
                        rowState |= RowState.InactiveSelected;
                    }
                }
                // in multiline view, the rectangle which must be painted with the auto-preview
                // background color is wider than the rectangle containing the column text
                Rectangle rcPreviewBackground = new Rectangle( rcFocus.Left, rcPreview.Top, rcFocus.Width, rcPreview.Height );
                _previewColumn.DrawItemBackground( g, rcPreviewBackground, rcPreviewBackground, itemNode, rowState, null );
                _previewColumn.DrawNode( g, rcPreview, itemNode, rowState, null );
            }

            if ( dropTargetRow || focusRow )
            {
                if ( dropTargetRow )
                {
                    JetListViewColumn.DrawDropTarget( g, rcFocus );
                }
                else if ( focusRow )
                {
                    _baseRowRenderer.ControlPainter.DrawFocusRect( g, rcFocus );
                }
            }
	    }

        public MouseHandleResult HandleMouseDown( JetListViewNode node, int x, int y, MouseButtons button, Keys modifiers )
	    {
            int baseHeight = _baseRowRenderer.GetRowHeight( node );
            if ( y < baseHeight )
            {
                return _baseRowRenderer.HandleMouseDown( node, x, y, button, modifiers );
            }
            return _previewColumn.HandleMouseDown( node, x, y - baseHeight );
	    }

	    public bool HandleMouseUp( JetListViewNode node, int x, int y, MouseButtons button, Keys modifiers )
	    {
            int baseHeight = _baseRowRenderer.GetRowHeight( node );
            if ( y < baseHeight )
            {
                return _baseRowRenderer.HandleMouseUp( node, x, y, button, modifiers );
            }
            return _previewColumn.HandleMouseUp( node, x, y - baseHeight );
        }

	    public bool HandleKeyDown( JetListViewNode node, KeyEventArgs e )
	    {
	        return _baseRowRenderer.HandleKeyDown( node, e );
	    }

	    public bool AcceptDoubleClick( JetListViewNode node, int x, int y )
	    {
            int baseHeight = _baseRowRenderer.GetRowHeight( node );
            if ( y < baseHeight )
            {
                return _baseRowRenderer.AcceptDoubleClick( node, x, y );
            }
            return _previewColumn.AcceptColumnDoubleClick;
        }

	    public void HandleDoubleClick( JetListViewNode node )
	    {
	        _baseRowRenderer.HandleDoubleClick( node );
	    }

	    public void SizeColumnsToContent( HashSet addedNodes, HashSet removedNodes, HashSet changedNodes )
	    {
	        _baseRowRenderer.SizeColumnsToContent( addedNodes, removedNodes, changedNodes );
	    }

	    public void ProcessNodeExpanded( JetListViewNode node )
	    {
	        _baseRowRenderer.ProcessNodeExpanded( node );
	    }

	    public void ProcessNodeCollapsed( JetListViewNode node )
	    {
	        _baseRowRenderer.ProcessNodeCollapsed( node );
	    }

	    public int ScrollOffset
	    {
	        get { return _baseRowRenderer.ScrollOffset; }
	        set { _baseRowRenderer.ScrollOffset = value; }
	    }

	    public string SearchHighlightText
	    {
	        get { return _baseRowRenderer.SearchHighlightText; }
	        set { _baseRowRenderer.SearchHighlightText = value; }
	    }

	    public Header HeaderControl
	    {
	        get { return _baseRowRenderer.HeaderControl; }
	        set { _baseRowRenderer.HeaderControl = value; }
	    }

	    public JetListViewColumn GetColumnAt( JetListViewNode node, int x, int y )
	    {
            int baseHeight = _baseRowRenderer.GetRowHeight( node );
            if ( y < baseHeight )
            {
                return _baseRowRenderer.GetColumnAt( node, x, y );
            }
            return _previewColumn;
        }

	    public Rectangle GetColumnBounds( JetListViewColumn col, JetListViewNode node )
	    {
	        if ( col == _previewColumn )
	        {
                int startX = _baseRowRenderer.BorderSize - _baseRowRenderer.ScrollOffset;
	            int endX = _baseRowRenderer.ScrollRange - _baseRowRenderer.BorderSize;
                return new Rectangle( startX, _baseRowRenderer.GetRowHeight( node ),
                    endX - startX, _previewColumn.GetAutoPreviewHeight( node ) );
	        }
            return _baseRowRenderer.GetColumnBounds( col, node );
	    }

	    public JetListViewColumn GetInPlaceEditColumn( JetListViewNode node )
	    {
	        return _baseRowRenderer.GetInPlaceEditColumn( node );
	    }

	    public bool MatchIncrementalSearch( JetListViewNode node, string text )
	    {
	        return _baseRowRenderer.MatchIncrementalSearch( node, text );
	    }

	    public void UpdateItem( object item )
	    {
	        _baseRowRenderer.UpdateItem( item );
            _previewColumn.UpdateItem( item );
	    }

	    public int ScrollRange
	    {
	        get { return _baseRowRenderer.ScrollRange; }
	    }

	    public int VisibleWidth
	    {
	        get { return _baseRowRenderer.VisibleWidth; }
	        set { _baseRowRenderer.VisibleWidth = value; }
	    }

	    public int BorderSize
	    {
	        get { return _baseRowRenderer.BorderSize; }
	        set { _baseRowRenderer.BorderSize = value; }
	    }

	    public Control OwnerControl
	    {
	        get { return _baseRowRenderer.OwnerControl; }
	        set { _baseRowRenderer.OwnerControl = value; }
	    }

        public IControlMethodInvoker MethodInvoker
	    {
	        get { return _baseRowRenderer.MethodInvoker; }
	        set { _baseRowRenderer.MethodInvoker = value; }
	    }

	    public IControlPainter ControlPainter
	    {
	        get { return _baseRowRenderer.ControlPainter; }
	        set { _baseRowRenderer.ControlPainter = value; }
	    }

	    public bool FullRowSelect
	    {
	        get { return _baseRowRenderer.FullRowSelect; }
	        set { _baseRowRenderer.FullRowSelect = value; }
	    }

	    public event EventHandler ScrollRangeChanged;
	    public event EventHandler Invalidate;
        public event RowHeightChangedEventHandler RowHeightChanged;
        public event EventHandler AllRowsHeightChanged;
        public event RequestScrollEventHandler RequestScroll;

	    private void HandleBaseScrollRangeChanged( object sender, EventArgs e )
	    {
            if ( ScrollRangeChanged != null )
            {
                ScrollRangeChanged( this, EventArgs.Empty );
            }
	    }

	    private void HandleBaseInvalidate( object sender, EventArgs e )
	    {
	        if ( Invalidate != null )
	        {
	            Invalidate( this, EventArgs.Empty );
	        }
	    }

        private void HandleBaseRequestScroll( object sender, RequestScrollEventArgs e )
        {
            if ( RequestScroll != null )
            {
                RequestScroll( this, e );
            }
        }

        private void HandlePreviewRowHeightChanged( object sender, RowHeightChangedEventArgs e )
	    {
            if ( RowHeightChanged != null )
            {
                int baseHeight = _baseRowRenderer.GetRowHeight( e.Row );
                RowHeightChanged( this, new RowHeightChangedEventArgs( e.Row, e.OldHeight + baseHeight,
                    e.NewHeight + baseHeight ) );
            }
	    }

        private void HandlePreviewAllRowsHeightChanged( object sender, EventArgs e )
        {
            if ( AllRowsHeightChanged != null )
            {
                AllRowsHeightChanged( this, EventArgs.Empty );
            }
        }
    }
}
