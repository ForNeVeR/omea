// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using SP.Windows;

namespace JetBrains.JetListViewLibrary
{
	internal class SingleLineRowRenderer: RowRendererBase
	{
        private JetListViewNodeCollection _nodeCollection;
        private int _sizeToContentColumnCount = 0;
        private int _autoSizeColumnCount = 0;
        private int _visibleWidth;
	    private Hashtable _headerMap = new Hashtable();
	    private JetListViewColumn _trackColumn;
        private bool _processingHeaderOperation;
        private int _internalChange = 0;
        private int _minColWidth = 40;
        private HashMap _sizeToContentItemWidths = new HashMap();  // JLVColumn -> IntHashTable<object, width>

	    public SingleLineRowRenderer( JetListViewColumnCollection columnCollection,
            JetListViewNodeCollection nodeCollection ) : base( columnCollection )
	    {
	        _columnCollection = columnCollection;
            _nodeCollection = nodeCollection;
            _columnCollection.BatchUpdateStarted += HandleBatchUpdateStarted;
            _columnCollection.BatchUpdated += HandleBatchUpdated;
            _nodeCollection.VisibleNodeAdded += HandleNodeAdded;
            _nodeCollection.VisibleNodeRemoved += HandleNodeRemoved;
            _nodeCollection.NodeChanged += HandleNodeChanged;
            _nodeCollection.NodeMoved += HandleNodeChanged;
            CountAutoSizeColumns();
	    }

	    public override void Dispose()
	    {
            _columnCollection.BatchUpdateStarted -= HandleBatchUpdateStarted;
            _columnCollection.BatchUpdated -= HandleBatchUpdated;

            base.Dispose();
	    }

	    /// <summary>
	    /// Gets or sets the minimum possible width of an autosize column.
	    /// </summary>
        public int MinColumnWidth
	    {
	        get { return _minColWidth; }
	        set { _minColWidth = value; }
	    }

	    public override int VisibleWidth
	    {
	        get { return _visibleWidth; }
	        set
	        {
                int delta = value - _visibleWidth;
                if ( delta != 0 )
                {
                    _visibleWidth = value;
                    AllocateAutoSizeWidth();
                    UpdateScrollRange();
                    UpdateHeaderControl();
                    if ( _autoSizeColumnCount > 0 )
                    {
                        // the column width has changed, so we need to redraw the column contents
                        OnInvalidate();
                    }
                }
	        }
	    }

	    protected override void HookHeaderControl()
	    {
	        _headerControl.BeforeSectionTrack += HandleBeforeSectionTrack;
	        _headerControl.SectionTracking += HandleSectionTracking;
	        _headerControl.AfterSectionTrack += HandleAfterSectionTrack;
	        _headerControl.SectionClick += HandleSectionClick;
	        _headerControl.BeforeSectionDrag += HandleBeforeSectionDrag;
	        _headerControl.AfterSectionDrag += HandleAfterSectionDrag;
	        _headerControl.CustomDrawSection += HandleCustomDrawSection;
            _headerControl.DividerDblClick += HandleDividerDblClick;
            UpdateHeaderControl();
	    }

	    protected override void UnhookHeaderControl()
	    {
	        _headerControl.BeforeSectionTrack -= HandleBeforeSectionTrack;
	        _headerControl.SectionTracking -= HandleSectionTracking;
	        _headerControl.AfterSectionTrack -= HandleAfterSectionTrack;
	        _headerControl.SectionClick -= HandleSectionClick;
	        _headerControl.BeforeSectionDrag -= HandleBeforeSectionDrag;
	        _headerControl.AfterSectionDrag -= HandleAfterSectionDrag;
	        _headerControl.CustomDrawSection -= HandleCustomDrawSection;
            _headerControl.DividerDblClick -= HandleDividerDblClick;
            _headerControl.Sections.Clear();
            _headerMap.Clear();
	    }

	    protected override void HandleColumnAdded( object sender, ColumnEventArgs e )
	    {
            base.HandleColumnAdded( sender, e );
            if ( e.Column.SizeToContent )
            {
                _sizeToContentColumnCount++;
            }
            else if ( e.Column.AutoSize )
            {
                _autoSizeColumnCount++;
            }
            if ( !_columnCollection.BatchUpdating )
            {
                ProcessColumnUpdate( false );
            }
	    }

	    protected override void HandleColumnRemoved( object sender, ColumnEventArgs e )
        {
            base.HandleColumnRemoved( sender, e );
            HeaderSection section = (HeaderSection) _headerMap [e.Column];
            if ( section != null )
            {
                _headerMap.Remove( e.Column );
                _headerControl.Sections.Remove( section );
            }

	        if ( e.Column.SizeToContent )
            {
                lock( _sizeToContentItemWidths )
                {
                    _sizeToContentItemWidths.Remove( e.Column );
                }
                _sizeToContentColumnCount--;
            }
            if ( e.Column.AutoSize )
            {
                _autoSizeColumnCount--;
            }
            if ( !_columnCollection.BatchUpdating )
            {
                ProcessColumnUpdate( false );
            }
        }

	    protected override void HookColumn( JetListViewColumn col )
	    {
	        base.HookColumn( col );
            col.SizeToContentChanged += HandleSizeToContentChanged;
            col.AutoSizeChanged += HandleAutoSizeChanged;
            col.WidthChanged += HandleColumnWidthChanged;
            col.TextChanged += HandleColumnTextChanged;
	    }

	    protected override void UnhookColumn( JetListViewColumn col )
	    {
            base.UnhookColumn( col );
	        col.SizeToContentChanged -= HandleSizeToContentChanged;
	        col.AutoSizeChanged -= HandleAutoSizeChanged;
	        col.WidthChanged -= HandleColumnWidthChanged;
	        col.TextChanged -= HandleColumnTextChanged;
	    }

	    private void ProcessColumnUpdate( bool notifyResize )
        {
            AllocateAutoSizeWidth();
            UpdateScrollRange();
            UpdateHeaderControl();
            if ( notifyResize )
            {
                OnColumnResized();
            }
        }

        private void HandleSizeToContentChanged( object sender, EventArgs e )
	    {
            JetListViewColumn col = (JetListViewColumn) sender;
            if ( col.SizeToContent )
            {
                _sizeToContentColumnCount++;
            }
            else
            {
                lock( _sizeToContentItemWidths )
                {
                    _sizeToContentItemWidths.Remove( col );
                }
                _sizeToContentColumnCount--;
            }
	    }

        private void HandleAutoSizeChanged( object sender, EventArgs e )
        {
            JetListViewColumn col = (JetListViewColumn) sender;
            if ( col.AutoSize )
            {
                _autoSizeColumnCount++;
            }
            else
            {
                _autoSizeColumnCount--;
            }
            AllocateAutoSizeWidth();
        }

        private void CountAutoSizeColumns()
        {
            _autoSizeColumnCount = 0;
            foreach( JetListViewColumn col in _columnCollection )
            {
                if ( col.AutoSize )
                {
                    _autoSizeColumnCount++;
                }
            }
        }

        private void HandleNodeAdded( object sender, JetListViewNodeEventArgs e )
        {
	        ProcessNodeAdded( e.Node );
        }

	    internal void ProcessNodeAdded( JetListViewNode node )
	    {
            RecalcColumnWidth( RecalcOnNodeAdded, node );
	    }

	    private void RecalcOnNodeAdded( JetListViewColumn col, JetListViewColumn indentCol, int fixedWidth,
            JetListViewNode paramNode )
	    {
            int desiredWidth = GetDesiredWidthIndented( col, indentCol, paramNode, fixedWidth );
            col.Width = Math.Max( col.Width, desiredWidth );
	    }

	    private void HandleNodeRemoved( object sender, JetListViewNodeEventArgs e )
        {
            RecalcColumnWidth( RecalcAll, e.Node );
        }

        private void RecalcAll( JetListViewColumn col, JetListViewColumn indentCol, int fixedWidth, JetListViewNode paramNode )
        {
            if ( paramNode != null && GetDesiredWidthIndented( col, indentCol, paramNode, fixedWidth ) != col.Width )
            {
                return;
            }

            int maxWidth = 0;
            lock( _nodeCollection )
            {
                foreach( JetListViewNode node in _nodeCollection.VisibleItems )
                {
                    int scrollRange = GetDesiredWidthIndented( col, indentCol, node, fixedWidth );
                    if ( scrollRange > maxWidth )
                        maxWidth = scrollRange;
                }
            }
            col.Width = maxWidth;
        }

        private void HandleNodeChanged( object sender, JetListViewNodeEventArgs e )
        {
            RecalcColumnWidth( RecalcOnNodeChanged, e.Node );
        }

	    private void RecalcOnNodeChanged( JetListViewColumn col, JetListViewColumn indentCol, int fixedWidth, JetListViewNode paramNode )
	    {
            int oldWidth = -1;
            lock( _sizeToContentItemWidths )
            {
                HashMap widths = (HashMap) _sizeToContentItemWidths [col];
                if ( widths != null && widths.Contains( paramNode.Data ) )
                {
                    oldWidth = (int) widths [paramNode.Data];
                }
            }
            if ( oldWidth == col.Width )
            {
                RecalcAll( col, indentCol, fixedWidth, null );
            }
            else
            {
                int desiredWidth = GetDesiredWidthIndented( col, indentCol, paramNode, fixedWidth );
                col.Width = Math.Max( col.Width, desiredWidth );
            }
	    }

	    public override void SizeColumnsToContent( HashSet addedNodes, HashSet removedNodes, HashSet changedNodes )
	    {
            if ( _sizeToContentColumnCount == 0 )
            {
                return;
            }
            if ( (addedNodes != null || removedNodes != null || changedNodes != null) &&
                (removedNodes == null || removedNodes.Count == 0 ) )
            {
                if ( addedNodes != null )
                {
                    foreach( HashSet.Entry e in addedNodes )
                    {
                        RecalcColumnWidth( RecalcOnNodeAdded, (JetListViewNode) e.Key );
                    }
                }
                if ( changedNodes != null )
                {
                    foreach( HashSet.Entry e in changedNodes )
                    {
                        RecalcColumnWidth( RecalcOnNodeChanged, (JetListViewNode) e.Key );
                    }
                }
            }
            else
            {
                RecalcColumnWidth( RecalcAll, null );
            }
        }

        public override void ProcessNodeExpanded( JetListViewNode node )
        {
            RecalcColumnWidth( RecalcOnExpand, node );
        }

	    private void RecalcOnExpand( JetListViewColumn col, JetListViewColumn indentCol, int fixedWidth, JetListViewNode paramNode )
	    {
            int maxWidth = col.Width;
            IEnumerator enumerator = paramNode.EnumerateChildrenRecursive();
            while( enumerator.MoveNext() )
            {
                JetListViewNode node = (JetListViewNode) enumerator.Current;
                int desiredWidth = GetDesiredWidthIndented( col, indentCol, node, fixedWidth );
                if ( desiredWidth > maxWidth )
                    maxWidth = desiredWidth;
            }
            col.Width = maxWidth;
	    }

        public override void ProcessNodeCollapsed( JetListViewNode node )
        {
            RecalcColumnWidth( RecalcOnCollapse, node  );
        }

	    private void RecalcOnCollapse( JetListViewColumn col, JetListViewColumn indentCol, int fixedWidth, JetListViewNode paramNode )
	    {
            IEnumerator enumerator = paramNode.EnumerateChildrenRecursive();
            while( enumerator.MoveNext() )
            {
                JetListViewNode node = (JetListViewNode) enumerator.Current;
                int desiredWidth = GetDesiredWidthIndented( col, indentCol, node, fixedWidth );
                if ( col.Width == desiredWidth )
                {
                    RecalcAll( col, indentCol, fixedWidth, null );
                    break;
                }
            }
        }

	    private void RecalcColumnWidth( ProcessSizeToContentColumnDelegate callback, JetListViewNode paramNode )
        {
            if ( _sizeToContentColumnCount > 0 )
            {
                int fixedWidth = 0;
                JetListViewColumn indentCol = null;
                for( int i=0; i<_columnCollection.Count; i++ )
                {
                    JetListViewColumn col = _columnCollection [i];
                    if ( col.IsIndentColumn() )
                    {
                        indentCol = col;
                    }
                    if ( col.FixedSize && indentCol != null )
                    {
                        fixedWidth += col.Width;
                    }
                    if ( col.SizeToContent )
                    {
                        callback( col, indentCol, fixedWidth, paramNode );
                    }
                    if ( !col.FixedSize && !col.IsIndentColumn() )
                    {
                        indentCol = null;
                        fixedWidth = 0;
                    }
                }
                UpdateScrollRange();
            }
        }

	    private int GetDesiredWidthIndented( JetListViewColumn col, JetListViewColumn indentCol,
            JetListViewNode node, int fixedWidth )
	    {
	        int result = col.GetDesiredWidth( node.Data ) + fixedWidth;
            if ( indentCol != null )
            {
                result += indentCol.GetIndent( node );
            }
            lock( _sizeToContentItemWidths )
            {
                HashMap widths = (HashMap) _sizeToContentItemWidths [col];
                if ( widths == null )
                {
                    widths = new HashMap();
                    _sizeToContentItemWidths [col] = widths;
                }
                widths [node.Data] = result;
            }
            return result;
	    }

	    private void UpdateScrollRange()
        {
	        SetScrollRange( GetTotalColumnWidth() );
        }

	    private int GetTotalColumnWidth()
	    {
	        int result = 0;
            bool isIndent = false;
	        foreach( JetListViewColumn col in _columnCollection )
	        {
                if ( col.IsIndentColumn() )
                {
                    isIndent = true;
                }
                if ( !col.FixedSize && !col.IsIndentColumn() )
                {
                    isIndent = false;
                }
                if ( !isIndent )
                {
                    result += col.Width;
                }
	        }
	        return result;
	    }

        private void AllocateAutoSizeWidth()
        {
            if ( _autoSizeColumnCount > 0 )
            {
                bool widthChanged = false;
                int availWidth = VisibleWidth;
                foreach( JetListViewColumn col in _columnCollection )
                {
                    if ( !col.IsIndentColumn() && !col.AutoSize )
                    {
                        availWidth -= col.Width;
                    }
                }
                if ( availWidth > 0 )
                {
                    int remainingColumns = _autoSizeColumnCount;
                    foreach( JetListViewColumn col in _columnCollection )
                    {
                        if ( col.AutoSize )
                        {
                            int colWidth = availWidth / remainingColumns;
                            int minWidth = col.AutoSizeMinWidth;
                            if ( minWidth < _minColWidth )
                            {
                                minWidth = _minColWidth;
                            }
                            if ( colWidth < minWidth )
                            {
                                colWidth = minWidth;
                            }
                            if ( colWidth != col.Width )
                            {
                                widthChanged = true;
                            }
                            SetColumnWidth( col, colWidth );
                            remainingColumns--;
                            availWidth -= colWidth;
                            if ( remainingColumns == 0 || availWidth < 0 )
                            {
                                break;
                            }
                        }
                    }
                }
                if ( widthChanged )
                {
                    OnInvalidate();
                }
            }
        }

	    private void SetColumnWidth( JetListViewColumn col, int colWidth )
	    {
            _internalChange++;
            try
            {
                col.Width = colWidth;
            }
            finally
            {
                _internalChange--;
            }
	    }

	    protected override void ScrollColumnInView( JetListViewColumn col, JetListViewNode node )
	    {
	        Rectangle rc = GetColumnBounds( col, node );
            if ( rc.Left < 0 )
            {
                OnRequestScroll( _scrollOffset + rc.Left + _borderSize );
            }
            else if ( rc.Right > _visibleWidth )
            {
                OnRequestScroll( _scrollOffset + rc.Right - _visibleWidth );
            }
	    }

	    public override void UpdateItem( object item )
	    {
            for( int i=0; i<_columnCollection.Count; i++ )
            {
                _columnCollection [i].UpdateItem( item );
            }
        }

	    public override void DrawRow( Graphics g, Rectangle rc, JetListViewNode itemNode, RowState state )
	    {
            int curX = -_scrollOffset + _borderSize;

            Rectangle rcFocus = Rectangle.Empty;
            bool focusRow = false, dropTargetRow = false;

            if ( ( (state & RowState.Focused) != 0 &&
                _searchHighlightText != null && _searchHighlightText.Length > 0) )
            {
                state |= RowState.IncSearchMatch;
            }

            if ( _fullRowSelect )
            {
                rcFocus = GetFocusRect( itemNode, rc );
                FillFullRowSelectBar( g, rcFocus, state );
            }

            ColumnWidthEnumerator colEnumerator = new ColumnWidthEnumerator( _columnCollection, itemNode );
            bool selectionDrawn = false, firstValueColumnFound = false;
            while( colEnumerator.MoveNext() )
            {
                if ( IsValueColumn( colEnumerator.Current ) )
                {
                    firstValueColumnFound = true;
                }
                ClearRowSelectState( ref state, ref focusRow, ref dropTargetRow );
                if ( _fullRowSelect )
                {
                    if ( (state & RowState.IncSearchMatch) != 0 && firstValueColumnFound &&
                        ((state & RowState.ActiveSelected) != 0) )
                    {
                        state &= ~RowState.ActiveSelected;
                        state |= RowState.InactiveSelected;
                    }
                }
                else
                {
                    if ( selectionDrawn )
                    {
                        state &= ~(RowState.InactiveSelected | RowState.ActiveSelected | RowState.Focused);
                    }
                }

                Rectangle rcCol = new Rectangle( curX, rc.Top, colEnumerator.CurrentWidth, rc.Height );
                JetListViewColumn col = colEnumerator.Current;

                DrawColumnWithHighlight( g, rcCol, itemNode, col, state );

                if ( !selectionDrawn && firstValueColumnFound )
                {
                    selectionDrawn = true;
                }

                curX += rcCol.Width;
            }

	        DrawRowSelectRect( g, rcFocus, dropTargetRow, focusRow );
	    }

	    protected internal override Rectangle GetFocusRect( JetListViewNode itemNode, Rectangle rc )
	    {
	        Rectangle rcFocus;
	        int curX = -_scrollOffset + _borderSize;
	        int selWidth = GetTotalColumnWidth();
            bool inIndent = false;
	        ColumnWidthEnumerator fixedColEnumerator = new ColumnWidthEnumerator( _columnCollection, itemNode );
	        while( fixedColEnumerator.MoveNext() )
	        {
	            if ( IsValueColumn( fixedColEnumerator.Current ) )
	            {
	                break;
	            }
                if ( fixedColEnumerator.Current.IsIndentColumn() )
                {
                    inIndent = true;
                }
	            curX += fixedColEnumerator.CurrentWidth;
                if ( !inIndent || !fixedColEnumerator.Current.FixedSize )
                {
                    selWidth -= fixedColEnumerator.CurrentWidth;
                }
	        }

	        rcFocus = new Rectangle( curX, rc.Top, selWidth, rc.Height );
	        return rcFocus;
	    }

	    public override int GetRowHeight( JetListViewNode node )
	    {
	        return _rowHeight;
	    }

	    public override int AllRowsHeight
	    {
	        get { return _rowHeight; }
	    }

	    protected override JetListViewColumn GetColumnAndDelta( JetListViewNode node, int x, int y, out int deltaX, out int deltaY )
        {
	        Guard.NullArgument( node, "node" );
            deltaY = y;
            int curX = -_scrollOffset + _borderSize;
            ColumnWidthEnumerator enumerator = new ColumnWidthEnumerator( _columnCollection, node );
            while( enumerator.MoveNext() )
            {
                if ( x >= curX && x < curX + enumerator.CurrentWidth )
                {
                    deltaX = x - curX;
                    return enumerator.Current;
                }
                curX += enumerator.CurrentWidth;
            }
            deltaX = 0;
            return null;
        }

	    protected override IEnumerable GetColumnEnumerable( JetListViewNode node )
	    {
	        return _columnCollection;
	    }

	    public override Rectangle GetColumnBounds( JetListViewColumn col, JetListViewNode node )
        {
            int curX = -_scrollOffset + _borderSize;
            ColumnWidthEnumerator enumerator = new ColumnWidthEnumerator( _columnCollection, node );
            while( enumerator.MoveNext() )
            {
                if ( enumerator.Current == col )
                {
                    return new Rectangle( curX, 0, enumerator.CurrentWidth, _rowHeight );
                }

                curX += enumerator.CurrentWidth;
            }
            throw new ArgumentException( "Column not found in list", "col" );
        }

        private void HandleColumnWidthChanged( object sender, EventArgs e )
        {
            if ( !_columnCollection.BatchUpdating && _internalChange == 0 )
            {
                if ( !_processingHeaderOperation )
                {
                    AllocateAutoSizeWidth();
                }
                UpdateScrollRange();
                UpdateHeaderControl();
            }
        }

        private void HandleColumnTextChanged( object sender, EventArgs e )
        {
            if ( !_columnCollection.BatchUpdating )
            {
                UpdateHeaderControl();
            }
        }

        protected override void HandleSortIconChanged( object sender, EventArgs e )
        {
            JetListViewColumn col = (JetListViewColumn) sender;
            HeaderSection section = (HeaderSection) _headerMap [col];
            if ( section != null )
            {
                SetHeaderSortIcon( section, col );
            }
        }

	    private static void SetHeaderSortIcon( HeaderSection section, JetListViewColumn col )
	    {
	        section.ImageIndex = (int) col.SortIcon;
	        section.ImageAlign = LeftRightAlignment.Right;
	    }

	    private void UpdateHeaderControl()
        {
            if ( _headerControl == null )
            {
                return;
            }

            int accumulatedWidth = 0;
            int colIndex = 0;
            HashSet usedHeaderSections = new HashSet();
            foreach( JetListViewColumn col in _columnCollection )
            {
                if ( col.ShowHeader )
                {
                    HeaderSection section = (HeaderSection) _headerMap [col];
                    if ( section == null )
                    {
                        section = new HeaderSection( col.Text, col.Width + accumulatedWidth );
                        _headerControl.Sections.Insert( colIndex, section );
                        _headerMap [col] = section;
                    }
                    else
                    {
                        section.Width = col.Width + accumulatedWidth;
                        section.Text = col.Text;
                        if ( section.Index != colIndex )
                        {
                            _headerControl.Sections.Remove( section );
                            _headerControl.Sections.Insert( colIndex, section );
                        }
                    }
                    SetHeaderSortIcon( section, col );
                    usedHeaderSections.Add( section );
                    colIndex++;
                    accumulatedWidth = 0;
                }
                else if ( !col.IsIndentColumn() )
                {
                    accumulatedWidth += col.Width;
                }
            }

            for( int i=_headerControl.Sections.Count-1; i >= 0; i-- )
            {
                if ( !usedHeaderSections.Contains( _headerControl.Sections [i] ) )
                {
                    _headerControl.Sections.RemoveAt( i );
                }
            }
        }

        private void HandleBeforeSectionTrack( object sender, HeaderSectionWidthConformableEventArgs ea )
        {
            JetListViewColumn col = ColumnFromHeaderSection( ea.Item );
            if ( col.FixedSize || col.SizeToContent || _processingHeaderOperation )
            {
                ea.Accepted = false;
            }
            else
            {
                _processingHeaderOperation = true;
                _trackColumn = col;
                OnInvalidate();
            }
        }

        private void HandleSectionTracking( object sender, HeaderSectionWidthConformableEventArgs ea )
        {
            int trackWidth = AdjustTrackWidth( ea.Width );
            int delta = ea.Width - trackWidth;
            if ( trackWidth >= 10 )
            {
                _trackColumn.Width = trackWidth;
            }
            else
            {
                _trackColumn.Width = 10;
                ea.Item.Width = 10 + delta;
            }
            OnInvalidate();
        }

        private void HandleAfterSectionTrack( object sender, HeaderSectionWidthEventArgs ea )
        {
            if ( _trackColumn != null )
            {
                int trackWidth = AdjustTrackWidth( ea.Width );
                int delta = ea.Width - trackWidth;
                if ( trackWidth >= 10 )
                {
                    _trackColumn.Width = trackWidth;
                    if ( _trackColumn.AutoSize )
                    {
                        _trackColumn.AutoSizeMinWidth = trackWidth;
                    }
                }
                else
                {
                    _trackColumn.Width = 10;
                    _methodInvoker.BeginInvoke( new SetMinWidthDelegate( SetMinWidth ), ea.Item, 10 + delta );
                }
                _trackColumn = null;
            }
            _processingHeaderOperation = false;
            OnInvalidate();
            _methodInvoker.BeginInvoke( new ProcessColumnUpdateDelegate( ProcessColumnUpdate ), true );
        }

	    private delegate void SetMinWidthDelegate( HeaderSection section, int width );
	    private delegate void ProcessColumnUpdateDelegate( bool notifyResize );

        private static void SetMinWidth( HeaderSection section, int width )
        {
            section.Width = width;
        }

        private int AdjustTrackWidth( int colWidth )
	    {
	        // subtract width of preceding columns which have been merged in the same header
	        int index = _columnCollection.IndexOf( _trackColumn );
	        for( int i=index-1; i >= 0; i-- )
	        {
	            if ( _columnCollection [i].ShowHeader )
	                break;

	            if ( !_columnCollection [i].IsIndentColumn() )
	            {
	                colWidth -= _columnCollection [i].Width;
	            }
	        }
	        return colWidth;
	    }

	    private void HandleSectionClick( object sender, HeaderSectionEventArgs ea )
        {
            JetListViewColumn col = ColumnFromHeaderSection( ea.Item );
            if ( !_processingHeaderOperation )
            {
                if ( ea.Button == MouseButtons.Left )
                {
                    _processingHeaderOperation = true;
                    OnColumnClick( col );
                    _processingHeaderOperation = false;
                }
            }
        }

        private void HandleBeforeSectionDrag( object sender, HeaderSectionOrderConformableEventArgs ea )
        {
            if ( _processingHeaderOperation )
            {
                ea.Accepted = false;
            }
            else
            {
                _processingHeaderOperation = true;
            }
        }

        private void HandleAfterSectionDrag( object sender, HeaderSectionOrderConformableEventArgs ea )
        {
            JetListViewColumn col = ColumnFromHeaderSection( ea.Item );
            int targetIndex = ea.Order;
            if ( targetIndex == ea.Item.Index )
            {
                _processingHeaderOperation = false;
                ea.Accepted = false;
                return;
            }

            targetIndex = AdjustTargetIndex( targetIndex );

            _columnCollection.Move( col, targetIndex );

            OnColumnOrderChanged();
            UpdateHeaderControl();
            OnInvalidate();

            ea.Accepted = false;
            _processingHeaderOperation = false;
        }

        private void HandleDividerDblClick( object sender, HeaderSectionEventArgs ea )
        {
            JetListViewColumn col = ColumnFromHeaderSection( ea.Item );
            if ( col.FixedSize )
            {
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                int fixedWidth = 0;
                JetListViewColumn indentCol = null;
                for( int i=0; i<_columnCollection.Count; i++ )
                {
                    JetListViewColumn aCol = _columnCollection [i];
                    if ( aCol.IsIndentColumn() )
                    {
                        indentCol = aCol;
                    }
                    if ( aCol.FixedSize && indentCol != null )
                    {
                        fixedWidth += col.Width;
                    }
                    if ( aCol == col )
                    {
                        RecalcAll( aCol, indentCol, fixedWidth, null );
                        OnInvalidate();
                        break;
                    }
                    if ( !aCol.FixedSize && !aCol.IsIndentColumn() )
                    {
                        indentCol = null;
                        fixedWidth = 0;
                    }
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private int AdjustTargetIndex( int targetIndex )
	    {
	        int columnIndex = 0, headerIndex = 0;
            foreach( JetListViewColumn aCol in _columnCollection )
	        {
                if ( headerIndex == targetIndex )
                {
                    break;
                }
                columnIndex++;
                if ( aCol.ShowHeader )
                {
                    headerIndex++;
                }
	        }
            return columnIndex;
	    }

        private void HandleCustomDrawSection( object sender, HeaderCustomDrawEventArgs ea )
        {
            JetListViewColumn col = ColumnFromHeaderSection( ea.Item );
            if ( col != null )
            {
                using( Graphics g = Graphics.FromHdc( ea.Hdc ) )
                {
                    col.DrawHeader( g, ea.Bounds );
                }
            }
        }

        private JetListViewColumn ColumnFromHeaderSection( HeaderSection item )
        {
            foreach( DictionaryEntry de in _headerMap )
            {
                if ( de.Value == item )
                {
                    return (JetListViewColumn) de.Key;
                }
            }
            return null;
        }

        private void HandleBatchUpdateStarted( object sender, EventArgs e )
        {
            if ( _headerControl != null )
            {
                _headerControl.BeginUpdate();
            }
        }

        private void HandleBatchUpdated( object sender, EventArgs e )
        {
            AllocateAutoSizeWidth();
            UpdateScrollRange();
            if ( _headerControl != null )
            {
                UpdateHeaderControl();
                _headerControl.EndUpdate();
                _headerControl.Invalidate();
            }
            OnInvalidate();
        }

        private class ColumnWidthEnumerator
        {
            private JetListViewColumnCollection _columns;
            private int _colIndex;
            private JetListViewNode _node;
            private int _itemIndent = 0;
            private int _availIndent = Int32.MaxValue;
            private int _curWidth;

            internal ColumnWidthEnumerator( JetListViewColumnCollection columns, JetListViewNode node )
            {
                _node = node;
                _columns = columns;
                _colIndex = -1;
            }

            internal bool MoveNext()
            {
                if ( _colIndex >= _columns.Count-1 )
                    return false;
                _colIndex++;

                JetListViewColumn curColumn = Current;
                if ( curColumn.IsIndentColumn() )
                {
                    _availIndent = GetAvailableIndent( _colIndex );
                    int indent = curColumn.GetIndent( _node );
                    _curWidth = Math.Min( _availIndent, indent );
                    _availIndent -= _curWidth;
                    _itemIndent = _curWidth;
                }
                else if ( !curColumn.FixedSize )
                {
                    _curWidth = curColumn.Width - _itemIndent;
                    _itemIndent = 0;
                    _availIndent = Int32.MaxValue;
                }
                else
                {
                    int delta = curColumn.GetWidthDelta( _node );
                    _curWidth = curColumn.Width + delta;
                    if ( _curWidth > _availIndent )
                    {
                        _itemIndent -= _curWidth - _availIndent;
                        _curWidth = _availIndent;
                    }
                    _availIndent -= _curWidth;
                    _itemIndent -= delta;
                }
                return true;
            }

            private int GetAvailableIndent( int colIndex )
            {
                int result = 0;
                for( int i=colIndex+1; i < _columns.Count; i++ )
                {
                    result += _columns [i].Width;
                    if ( !_columns [i].FixedSize )
                    {
                        return result;
                    }
                }
                return Int32.MaxValue;
            }

            internal JetListViewColumn Current
            {
                get { return _columns [_colIndex]; }
            }

            internal int CurrentWidth
            {
                get { return _curWidth; }
            }
        }

        private delegate void ProcessSizeToContentColumnDelegate( JetListViewColumn col, JetListViewColumn indentCol, int fixedWidth,
                                                                  JetListViewNode paramNode );
	}
}
