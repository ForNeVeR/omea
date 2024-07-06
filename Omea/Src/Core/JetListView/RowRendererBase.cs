// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using SP.Windows;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// Shared code between single-line and multiline renderers.
	/// </summary>
	internal abstract class RowRendererBase: IRowRenderer, IDisposable
	{
        protected JetListViewColumnCollection _columnCollection;
        protected int _borderSize;
	    protected int _scrollOffset;
	    protected string _searchHighlightText;
        private Control _ownerControl;
	    protected IControlPainter _controlPainter;
	    protected IControlMethodInvoker _methodInvoker;
	    protected bool _fullRowSelect = false;
	    private int _scrollRange;
	    protected int _rowHeight;
	    protected Header _headerControl;

	    protected RowRendererBase( JetListViewColumnCollection columnCollection )
	    {
	        _columnCollection = columnCollection;

            _columnCollection.ColumnAdded += new ColumnEventHandler( HandleColumnAdded );
            _columnCollection.ColumnRemoved += new ColumnEventHandler( HandleColumnRemoved );

            foreach( JetListViewColumn col in _columnCollection )
            {
                HookColumn( col );
            }
        }

	    public virtual void Dispose()
	    {
            _columnCollection.ColumnAdded -= new ColumnEventHandler( HandleColumnAdded );
            _columnCollection.ColumnRemoved -= new ColumnEventHandler( HandleColumnRemoved );

            foreach( JetListViewColumn col in _columnCollection )
            {
                UnhookColumn( col );
            }
        }

	    public event EventHandler ScrollRangeChanged;
        public event EventHandler Invalidate;
        public event RowHeightChangedEventHandler RowHeightChanged;
        public event EventHandler AllRowsHeightChanged;
        public event RequestScrollEventHandler RequestScroll;

        public event ColumnEventHandler ColumnClick;

        /// <summary>
        /// Occurs when the user drags the column splitter to change the size of the column.
        /// </summary>
        public event EventHandler ColumnResized;

        /// <summary>
        /// Occurs when the user drags the column header to change the order of the columns.
        /// </summary>
        public event EventHandler ColumnOrderChanged;

        /// <summary>
        /// The height of the line in the view.
        /// </summary>
        public int RowHeight
        {
            get { return _rowHeight; }
            set { _rowHeight = value; }
        }

        public int ScrollOffset
        {
            get { return _scrollOffset; }
            set { _scrollOffset = value; }
        }

        public int BorderSize
        {
            get { return _borderSize; }
            set { _borderSize = value; }
        }

        public string SearchHighlightText
        {
            get { return _searchHighlightText; }
            set { _searchHighlightText = value; }
        }

        public IControlMethodInvoker MethodInvoker
        {
            get { return _methodInvoker; }
            set { _methodInvoker = value; }
        }

        public Control OwnerControl
        {
            get { return _ownerControl; }
            set { _ownerControl = value; }
        }

        public IControlPainter ControlPainter
        {
            get { return _controlPainter; }
            set { _controlPainter = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the selection highlight covers all
        /// columns in the list or just the first column.
        /// </summary>
        public bool FullRowSelect
        {
            get { return _fullRowSelect; }
            set { _fullRowSelect = value; }
        }

        public int ScrollRange
        {
            get { return _scrollRange; }
        }

        public Header HeaderControl
        {
            get { return _headerControl; }
            set
            {
                if ( _headerControl != value )
                {
                    if ( _headerControl != null )
                    {
                        UnhookHeaderControl();
                    }
                    _headerControl = value;
                    if ( _headerControl != null )
                    {
                        HookHeaderControl();
                    }
                }
            }
        }

        public MouseHandleResult HandleMouseDown( JetListViewNode node, int x, int y, MouseButtons button, Keys modifiers )
        {
            MouseHandleResult result = 0;
            if ( button == MouseButtons.Left )
            {
                int deltaX, deltaY;
                JetListViewColumn col = GetClickColumn( node, x, y, out deltaX, out deltaY );
                if ( col != null )
                {
                    result = col.HandleMouseDown( node, deltaX, deltaY );
                    if ( col == GetInPlaceEditColumn( node ) )
                    {
                        result |= MouseHandleResult.MayInPlaceEdit;
                    }
                }
            }
            return result;
        }

        public bool HandleMouseUp( JetListViewNode node, int x, int y, MouseButtons button, Keys modifiers )
        {
            if ( button == MouseButtons.Left )
            {
                int deltaX, deltaY;
                JetListViewColumn col = GetClickColumn( node, x, y, out deltaX, out deltaY );
                if ( col != null )
                {
                    return col.HandleMouseUp( node, deltaX, deltaY );
                }
            }
            return false;
        }

	    public bool HandleKeyDown( JetListViewNode node, KeyEventArgs e )
	    {
            foreach( JetListViewColumn col in GetColumnEnumerable( node ) )
            {
                if ( col.HandleKeyDown( node, e ) )
                {
                    return true;
                }
            }
            return false;
        }

        public bool AcceptDoubleClick( JetListViewNode node, int x, int y )
        {
            JetListViewColumn col = GetColumnAt( node, x, y );
            if ( col == null )
            {
                return false;
            }
            return col.AcceptColumnDoubleClick;
        }

        public void HandleDoubleClick( JetListViewNode node )
        {
            foreach( JetListViewColumn col in GetColumnEnumerable( node ) )
            {
                if ( col.HandleDoubleClick( node ) )
                {
                    break;
                }
            }
        }

        protected JetListViewColumn GetClickColumn( JetListViewNode node, int x, int y,
            out int deltaX, out int deltaY )
        {
            foreach( JetListViewColumn col in GetColumnEnumerable( node ) )
            {
                if ( col.HandleAllClicks )
                {
                    Rectangle rc = GetColumnBounds( col, node );
                    deltaX = x - rc.Left;
                    deltaY = y - rc.Top;
                    return col;
                }
            }

            return GetColumnAndDelta( node, x, y, out deltaX, out deltaY );
        }

        public JetListViewColumn GetColumnAt( JetListViewNode node, int x, int y )
        {
            int deltaX, deltaY;
            return GetColumnAndDelta( node, x, y, out deltaX, out deltaY );
        }

        public JetListViewColumn GetInPlaceEditColumn( JetListViewNode node )
        {
            foreach( JetListViewColumn col in GetColumnEnumerable( node ) )
            {
                if ( IsValueColumn( col ) )
                {
                    return col;
                }
            }
            return null;
        }

        protected bool IsValueColumn( JetListViewColumn col )
        {
            return !col.FixedSize && !col.IsIndentColumn();
        }

        protected void OnInvalidate()
        {
            if ( Invalidate != null )
            {
                Invalidate( this, EventArgs.Empty );
            }
        }

        protected void SetScrollRange( int width )
        {
            if ( _scrollRange != width )
            {
                _scrollRange = width;
                if ( ScrollRangeChanged != null )
                {
                    ScrollRangeChanged( this, EventArgs.Empty );
                }
            }
        }

        protected virtual void HandleColumnAdded( object sender, ColumnEventArgs e )
        {
            HookColumn( e.Column );
        }

	    protected virtual void HandleColumnRemoved( object sender, ColumnEventArgs e )
        {
            UnhookColumn( e.Column );
        }

        protected virtual void HookColumn( JetListViewColumn col )
        {
            col.SortIconChanged += new EventHandler( HandleSortIconChanged );
        }

        protected virtual void UnhookColumn( JetListViewColumn col )
        {
            col.SortIconChanged -= new EventHandler( HandleSortIconChanged );
        }

        protected void OnColumnClick( JetListViewColumn col )
        {
            if ( ColumnClick != null )
            {
                ColumnClick( this, new ColumnEventArgs( col ) );
            }
        }

        protected void OnColumnResized()
        {
            if ( ColumnResized != null )
            {
                ColumnResized( this, EventArgs.Empty );
            }
        }

        protected void OnColumnOrderChanged()
        {
            if ( ColumnOrderChanged != null )
            {
                ColumnOrderChanged( this, EventArgs.Empty );
            }
        }

        protected void OnRequestScroll( int coord )
        {
            if ( RequestScroll != null )
            {
                RequestScroll( this, new RequestScrollEventArgs( coord ) );
            }
        }

        public abstract int GetRowHeight( JetListViewNode node );
        public abstract int AllRowsHeight { get; }
	    public abstract void UpdateItem( object item );
	    public abstract void DrawRow( Graphics g, Rectangle rc, JetListViewNode itemNode, RowState rowState );
	    public abstract void SizeColumnsToContent( HashSet addedNodes, HashSet removedNodes, HashSet changedNodes );
	    public abstract void ProcessNodeExpanded( JetListViewNode node );
	    public abstract void ProcessNodeCollapsed( JetListViewNode node );
	    public abstract Rectangle GetColumnBounds( JetListViewColumn col, JetListViewNode node );

        public abstract int VisibleWidth { get; set; }

        protected abstract JetListViewColumn GetColumnAndDelta( JetListViewNode node, int x, int y, out int deltaX, out int deltaY );
        protected abstract IEnumerable GetColumnEnumerable( JetListViewNode node );
        protected abstract void HandleSortIconChanged( object sender, EventArgs e );

	    protected virtual void UnhookHeaderControl()
	    {
	    }

        protected virtual void HookHeaderControl()
        {
        }

        protected virtual void ScrollColumnInView( JetListViewColumn col, JetListViewNode node )
        {
        }

        protected internal abstract Rectangle GetFocusRect( JetListViewNode itemNode, Rectangle rc );

        protected void FillFullRowSelectBar( Graphics g, Rectangle rcFocus, RowState state )
        {
            Brush brush = null;
            if ( (state & RowState.IncSearchMatch) != 0 )
            {
                brush = SystemBrushes.Control;
            }
            else if ( (state & RowState.ActiveSelected) != 0 )
            {
                brush = SystemBrushes.Highlight;
            }
            else if ( (state & RowState.InactiveSelected) != 0 )
            {
                brush = SystemBrushes.Control;
            }
            if ( brush != null )
            {
                g.FillRectangle( brush, rcFocus );
            }
        }

        protected void ClearRowSelectState( ref RowState state, ref bool focusRow, ref bool dropTargetRow )
        {
            if ( _fullRowSelect )
            {
                if ( (state & RowState.Focused ) != 0 )
                {
                    state &= ~RowState.Focused;
                    focusRow = true;
                }
                if ( (state & RowState.DropTarget) != 0 )
                {
                    state &= ~RowState.DropTarget;
                    dropTargetRow = true;
                }
            }
        }

        protected void DrawRowSelectRect( Graphics g, Rectangle rcFocus, bool dropTargetRow, bool focusRow )
        {
            if ( _fullRowSelect )
            {
                if ( dropTargetRow )
                {
                    JetListViewColumn.DrawDropTarget( g, rcFocus );
                }
                else if ( focusRow )
                {
                    _controlPainter.DrawFocusRect( g, rcFocus );
                }
            }
        }

        protected void DrawColumnWithHighlight( Graphics g, Rectangle rcCol, JetListViewNode itemNode,
            JetListViewColumn col, RowState state )
        {
            if ( (state & RowState.InPlaceEdit ) == 0 || col != GetInPlaceEditColumn( itemNode ) )
            {
                string highlightText = null;
                if ( (state & RowState.IncSearchMatch) != 0 &&
                    col.MatchIncrementalSearch( itemNode, _searchHighlightText ) )
                {
                    highlightText = _searchHighlightText;
                }

                col.DrawNode( g, rcCol, itemNode, state, highlightText );
            }
        }

	    public bool MatchIncrementalSearch( JetListViewNode node, string text )
	    {
	        foreach( JetListViewColumn col in GetColumnEnumerable( node ) )
	        {
	            if ( col.MatchIncrementalSearch( node, text ) )
	            {
                    ScrollColumnInView( col, node );
	                return true;
	            }
	        }
            return false;
        }

	    internal int GetAutoPreviewIndent( JetListViewNode node )
        {
            foreach( JetListViewColumn col in GetColumnEnumerable( node ) )
            {
                if ( !col.IsIndentColumn() && !col.FixedSize )
                {
                    return GetColumnBounds( col, node ).Left;
                }
            }
            return 0;
        }
    }
}
