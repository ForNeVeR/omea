// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using SP.Windows;

namespace JetBrains.JetListViewLibrary
{
    [Flags]
    public enum RowState
    {
        None = 0,
        ActiveSelected = 1,
        InactiveSelected = 2,
        Focused = 4,
        Disabled = 8,
        DropTarget = 16,
        InPlaceEdit = 32,
        IncSearchMatch = 64
    };

    [Flags]
    public enum MouseHandleResult
    {
        Handled = 1,
        MayInPlaceEdit = 2,
        SuppressFocus = 4,
        FocusOnMouseDown = 8
    }

	/// <summary>
	/// Defines how the visual cues for a drop target are rendered during the drag'n'drop operation over the list view.
	/// </summary>
	[Flags]
    public enum DropTargetRenderMode
	{
		/// <summary>
		/// The drop over the current item is prohibited.
		/// Currently, the drop target cues for this mode are not painted at all.
		/// </summary>
		Restricted = 1,
		/// <summary>
		/// A box drop target over the item.
		/// </summary>
		Over = 2,
		/// <summary>
		/// An insertion mark above the hovered item.
		/// </summary>
		InsertAbove = 4,
		/// <summary>
		/// An insertion mark below the hovered item.
		/// </summary>
		InsertBelow = 8,
		/// <summary>
		/// A synthetic value meaning any insertion drop target. Do not assign to this value, just use it to test for any kind of insertion.
		/// </summary>
		InsertAny = InsertAbove | InsertBelow
	}

    internal interface IRowRenderer
    {
        int GetRowHeight( JetListViewNode node );
        void UpdateItem( object item );
        void DrawRow( Graphics g, Rectangle rc, JetListViewNode itemNode, RowState rowState );
        MouseHandleResult HandleMouseDown( JetListViewNode node, int x, int y, MouseButtons button, Keys modifiers );
        bool HandleMouseUp( JetListViewNode node, int x, int y, MouseButtons button, Keys modifiers );
        bool HandleKeyDown( JetListViewNode node, KeyEventArgs e );
        bool AcceptDoubleClick( JetListViewNode node,int x, int y );
        void HandleDoubleClick( JetListViewNode node );
        void SizeColumnsToContent( HashSet addedNodes, HashSet removedNodes, HashSet changedNodes );
        void ProcessNodeExpanded( JetListViewNode node );
        void ProcessNodeCollapsed( JetListViewNode node );
        Rectangle GetColumnBounds( JetListViewColumn col, JetListViewNode node );
        JetListViewColumn GetInPlaceEditColumn( JetListViewNode node );
        bool MatchIncrementalSearch( JetListViewNode node, string text );

        int ScrollOffset { get; set; }
        int ScrollRange { get; }
        int VisibleWidth { get; set; }
        int BorderSize { get; set; }
        string SearchHighlightText { get; set; }
        Header HeaderControl { get; set; }
        Control OwnerControl { get; set; }
        IControlMethodInvoker MethodInvoker { get; set; }
        IControlPainter ControlPainter { get; set; }
        bool FullRowSelect { get; set; }
        int AllRowsHeight { get; }

        JetListViewColumn GetColumnAt( JetListViewNode node, int x, int y );

        event EventHandler ScrollRangeChanged;
        event EventHandler Invalidate;
        event RowHeightChangedEventHandler RowHeightChanged;
        event EventHandler AllRowsHeightChanged;
        event RequestScrollEventHandler RequestScroll;
    }

    /// <summary>
    /// Draws and handles events for group headers.
    /// </summary>
    internal interface IGroupRenderer
    {
        void DrawGroupHeader( Graphics g, Rectangle rectangle, GroupHeaderNode node, RowState rowState );
        bool HandleMouseDown( GroupHeaderNode node, int x, int y, MouseButtons button, Keys modifiers );
        bool HandleGroupKeyDown( GroupHeaderNode node, KeyEventArgs e );
        bool HandleNodeKeyDown( JetListViewNode viewNode, KeyEventArgs e );
        Color GroupHeaderColor { get; set; }
        int GroupHeaderHeight { get; }
        int VisibleWidth { get; set; }
    }

	/// <summary>
	/// Manages drawing and scrolling of a list of variable-height rows. The specific content
	/// of each row is defined by the IRowRenderer implementation used.
	/// </summary>
	internal class RowListRenderer: IPagingProvider
	{
		private JetListViewNodeCollection _nodeCollection;
        private NodeGroupCollection _groupCollection;
        private IVisibleNodeCollection _visibleNodeCollection;
        private IRowRenderer _rowRenderer;
        private IGroupRenderer _groupRenderer;
        private int _scrollOffset;
        private int _scrollRange;
        private int _borderSize;
        private int _lastRemovedNodeTop;
        private int _lastMovingNodeTop;
        private int _visibleHeight = Int32.MaxValue;
        private SelectionModel _selection;
        private bool _activeSelection = false;
        private bool _controlEnabled = true;
        private JetListViewNode _dropTargetRow;
        private JetListViewNode _inPlaceEditedNode;
        private IViewNode _topNode;
        private int _topNodeOffset;
        private bool _hideSelection;
        private bool _rowDelimiters = false;
        private bool _calculatingScrollRange = false;
        private DropTargetRenderMode _dropTargetRenderMode;

        public RowListRenderer( JetListViewNodeCollection collection, SelectionModel selectionModel )
		{
            _nodeCollection = collection;
            _nodeCollection.VisibleNodeAdded += new JetListViewNodeEventHandler( HandleVisibleNodeAdded );
            _nodeCollection.VisibleNodeRemoving += new JetListViewNodeEventHandler( HandleVisibleNodeRemoving );
            _nodeCollection.VisibleNodeRemoved += new JetListViewNodeEventHandler( HandleVisibleNodeRemoved );
            _nodeCollection.NodeMoving += new JetListViewNodeEventHandler( HandleNodeMoving );
            _nodeCollection.NodeMoved += new JetListViewNodeEventHandler( HandleNodeMoved );
            _nodeCollection.MultipleNodesChanged += new MultipleNodesChangedEventHandler( HandleMultipleNodesChanged );
            _nodeCollection.FilterListChanged += new EventHandler( HandleFilterListChanged );
            _nodeCollection.NodeExpandChanged += new JetListViewNodeEventHandler( HandleNodeExpandChanged );
            _nodeCollection.NodeChanged += new JetListViewNodeEventHandler( HandleNodeChanged );
            _nodeCollection.Sorted += new EventHandler( HandleNodesSorted );
            _visibleNodeCollection = _nodeCollection;

            SelectionModel = selectionModel;
            _topNode = null;
		}

	    public event InvalidateEventHandler Invalidate;
        public event EventHandler ScrollRangeChanged;
        public event RequestScrollEventHandler RequestVerticalScroll;

	    internal IRowRenderer RowRenderer
	    {
	        get { return _rowRenderer; }
	        set
	        {
	            if ( _rowRenderer != value )
	            {
                    bool hadRenderer = (_rowRenderer != null);
                    if ( _rowRenderer != null )
                    {
                        _rowRenderer.RowHeightChanged -= new RowHeightChangedEventHandler( HandleRowHeightChanged );
                        _rowRenderer.AllRowsHeightChanged -= new EventHandler( HandleAllRowsHeightChanged );
                    }
                    _rowRenderer = value;
                    if ( _rowRenderer != null )
                    {
                        _rowRenderer.RowHeightChanged += new RowHeightChangedEventHandler( HandleRowHeightChanged );
                        _rowRenderer.AllRowsHeightChanged += new EventHandler( HandleAllRowsHeightChanged );
                    }
                    if ( hadRenderer )
                    {
                        UpdateScrollRange();
                    }
	            }
	        }
        }

	    internal IGroupRenderer GroupRenderer
	    {
	        get { return _groupRenderer; }
	        set
	        {
	            _groupRenderer = value;
                UpdateScrollRange();
	        }
	    }

	    internal NodeGroupCollection NodeGroupCollection
	    {
	        get { return _groupCollection; }
	        set
	        {
	            if ( _groupCollection != value)
	            {
	                if ( _groupCollection != null )
	                {
	                    _groupCollection.GroupAdded -= new GroupEventHandler( HandleGroupsChanged );
	                    _groupCollection.GroupRemoved -= new GroupEventHandler( HandleGroupsChanged );
	                    _groupCollection.GroupExpandChanged -= new GroupEventHandler( HandleGroupExpandChanged );
	                }
	                _groupCollection = value;
	                if ( _groupCollection != null )
	                {
	                    _groupCollection.GroupAdded += new GroupEventHandler( HandleGroupsChanged );
	                    _groupCollection.GroupRemoved += new GroupEventHandler( HandleGroupsChanged );
	                    _groupCollection.GroupExpandChanged += new GroupEventHandler( HandleGroupExpandChanged );
	                    _visibleNodeCollection = _groupCollection;
	                }
	                else
	                {
	                    _visibleNodeCollection = _nodeCollection;
	                }
                    UpdateTopNode();
	            }
	        }
	    }

	    internal SelectionModel SelectionModel
        {
            get { return _selection; }
            set
            {
                if ( _selection != value )
                {
                    if ( _selection != null )
                    {
                        _selection.SelectionStateChanged -= new ViewNodeStateChangeEventHandler( HandleSelectionStateChanged );
                        _selection.FocusStateChanged -= new ViewNodeStateChangeEventHandler( HandleFocusStateChanged );
                        _selection.PagingProvider = null;
                    }

                    _selection = value;
                    _selection.PagingProvider = this;

                    _selection.SelectionStateChanged += new ViewNodeStateChangeEventHandler( HandleSelectionStateChanged );
                    _selection.FocusStateChanged += new ViewNodeStateChangeEventHandler( HandleFocusStateChanged );
                }
            }
        }

        public int ScrollOffset
	    {
	        get { return _scrollOffset; }
            set
            {
                if ( _scrollOffset != value )
                {
                    int delta = value - _scrollOffset;
                    _scrollOffset = value;
                    AdjustTopNode( delta );
                    /*
                    IViewNode adjustedNode = _topNode;
                    int adjustedOffset = _topNodeOffset;
                    UpdateTopNode();
                    if ( adjustedNode != _topNode || adjustedOffset != _topNodeOffset )
                    {
                        throw new Exception( "AdjustTopNode/UpdateTopNode mismatch" );
                    }
                    */
                }
            }
	    }

        private int GetRowHeight( IViewNode node )
        {
            GroupHeaderNode headerNode = node as GroupHeaderNode;
            if ( headerNode != null )
            {
                return _groupRenderer.GroupHeaderHeight;
            }
            else
            {
                int height = _rowRenderer.GetRowHeight( (JetListViewNode) node );
                if ( _rowDelimiters )
                {
                    height++;
                }
                return height;
            }
        }

        private bool inUpdateTopNode = false;

        private void UpdateTopNode()
        {
            if( !inUpdateTopNode )
            {
                inUpdateTopNode = true;
                try
                {
                    lock( _nodeCollection )
                    {
                        _topNode = null;
                        int delta = _scrollOffset;
                        IVisibleNodeEnumerator enumerator = _visibleNodeCollection.GetFullEnumerator();
                        while( enumerator.MoveNext() )
                        {
                            int height = GetRowHeight( enumerator.CurrentNode );
                            if ( delta < height )
                            {
                                _topNode = enumerator.CurrentNode;
                                _topNodeOffset = delta;
                                break;
                            }
                            delta -= height;
                        }
                    }
                }
                finally
                {
                    inUpdateTopNode = false;
                }
            }
        }

        private void AdjustTopNode( int delta )
        {
            lock( _nodeCollection )
            {
                IViewNode topNode = _topNode;      // avoid race conditions (OM-12038)
                if ( topNode == null || !_visibleNodeCollection.IsNodeVisible( topNode ) )
                {
                    UpdateTopNode();
                    return;
                }
                try
                {
                    IVisibleNodeEnumerator enumerator = _visibleNodeCollection.GetDirectionalEnumerator(  topNode,
                                                                                                          (delta > 0) ? MoveDirection.Down : MoveDirection.Up );

                    if ( _topNodeOffset != 0 )
                    {
                        if ( delta > 0 )
                        {
                            int curNodeDelta = GetRowHeight( topNode ) - _topNodeOffset;
                            if ( delta < curNodeDelta )
                            {
                                _topNodeOffset += delta;
                                return;
                            }
                            delta -= curNodeDelta;
                            enumerator.MoveNext();
                        }
                        else
                        {
                            if ( _topNodeOffset >= -delta )
                            {
                                _topNodeOffset += delta;
                                return;
                            }
                            delta += _topNodeOffset;
                        }
                    }

                    if ( delta < 0 )
                    {
                        enumerator.MoveNext();
                    }

                    int absDelta = Math.Abs( delta );
                    _topNodeOffset = 0;
                    while( enumerator.MoveNext() )
                    {
                        int curRowHeight = GetRowHeight( enumerator.CurrentNode );
                        if ( absDelta < curRowHeight )
                        {
                            if ( delta > 0 || absDelta == 0 )
                            {
                                _topNodeOffset = absDelta;
                            }
                            else
                            {
                                _topNodeOffset = curRowHeight - absDelta;
                            }
                            break;
                        }
                        absDelta -= curRowHeight;
                        if ( delta < 0 && absDelta == 0 )
                        {
                            break;
                        }
                    }
                    _topNode = enumerator.CurrentNode;
                }
                catch( ArgumentOutOfRangeException ex )  // mega-hacky crappy fix for OM-11090
                {
                    Trace.WriteLine( "Exception in UpdateTopNode: " + ex.ToString() );
                    UpdateTopNode();
                }
            }
        }

        public int ScrollRange
        {
            get { return _scrollRange; }
        }

        public int VisibleHeight
        {
            get { return _visibleHeight; }
            set { _visibleHeight = value; }
        }

	    public int BorderSize
	    {
	        get { return _borderSize; }
	        set { _borderSize = value; }
	    }

	    internal bool ActiveSelection
	    {
	        get { return _activeSelection; }
	        set
	        {
	            if ( _activeSelection != value )
	            {
                    _activeSelection = value;
	                InvalidateSelectedNodes();
	            }
	        }
	    }

	    public bool HideSelection
        {
            get { return _hideSelection; }
            set
            {
                if ( _hideSelection != value )
                {
                    _hideSelection = value;
                    if ( !_activeSelection )
                    {
                        InvalidateSelectedNodes();
                    }
                }
            }
        }

	    public bool RowDelimiters
	    {
	        get { return _rowDelimiters; }
	        set
	        {
	            if ( _rowDelimiters != value )
	            {
                    _rowDelimiters = value;
                    UpdateScrollRange();
                    UpdateTopNode();
	            }
	        }
	    }

	    private void InvalidateSelectedNodes()
	    {
            IViewNode[] nodes = _selection.SelectionToArray();
            foreach( IViewNode node in nodes )
            {
                InvalidateRow( node );
            }
	    }

	    public bool ControlEnabled
	    {
	        get { return _controlEnabled; }
	        set { _controlEnabled = value; }
	    }

	    internal JetListViewNode DropTargetRow
	    {
	        get { return _dropTargetRow; }
	    }

	    protected void OnInvalidate( int startY, int endY )
        {
            if ( Invalidate != null )
            {
                Invalidate( this, new InvalidateEventArgs( startY, endY ) );
            }
        }

        private void HandleGroupsChanged( object sender, GroupEventArgs e )
        {
            UpdateTopNode();
            UpdateScrollRange();
            InvalidateBelow( 0 );
        }

        private void HandleGroupExpandChanged( object sender, GroupEventArgs e )
        {
            UpdateScrollRange();
            InvalidateBelow( e.GroupHeader );
        }

        private void HandleVisibleNodeAdded( object sender, JetListViewNodeEventArgs e )
        {
            Guard.NullArgument( e.Node, "e.Node" );
            if ( _topNode == null )  // first node added to the view
            {
                _topNode = e.Node;
            }
            UpdateScrollRange();

            IViewNode topNode = _topNode;
            if ( topNode == null )
            {
                topNode = e.Node;
            }
            if ( _visibleNodeCollection.GetMoveDirection( e.Node, topNode ) == MoveDirection.Down )
            {
                // If we were scrolled so that the top item in the list was visible,
                // add the new item to the visible area. If the top item in the list was
                // not visible, scroll down so that the visible area remains the same and
                // the new item does not appear in it.
                if ( _scrollOffset == 0 )
                {
                    UpdateTopNode();
                }
                else
                {
                    OnRequestVerticalScroll( _scrollOffset + GetRowHeight( e.Node ) );
                }
            }
            InvalidateBelow( e.Node );
        }

        private void HandleNodeExpandChanged( object sender, JetListViewNodeEventArgs e )
        {
            if ( e.Node.Expanded )
            {
                _rowRenderer.ProcessNodeExpanded( e.Node );
            }
            else
            {
                _rowRenderer.ProcessNodeCollapsed( e.Node );
            }
            UpdateTopNode();
            UpdateScrollRange();
            InvalidateBelow( e.Node );
        }

	    private void InvalidateBelow( IViewNode node )
	    {
	        int startY, endY;
	        if ( GetRowBounds( node, out startY, out endY ) )
	        {
                InvalidateBelow( startY );
	        }
	    }

        private void InvalidateBelow( int y )
        {
            OnInvalidate( y, VisibleHeight + _borderSize );
        }

	    private void HandleVisibleNodeRemoving( object sender, JetListViewNodeEventArgs e )
        {
            _lastRemovedNodeTop = GetNodeTop( e.Node );
        }

        private void HandleVisibleNodeRemoved( object sender, JetListViewNodeEventArgs e )
        {
            UpdateTopNode();
            UpdateScrollRange();
            if ( _lastRemovedNodeTop >= 0 )
            {
                InvalidateBelow( _lastRemovedNodeTop );
            }
        }

        private void HandleNodeMoving( object sender, JetListViewNodeEventArgs e )
        {
            _lastMovingNodeTop = GetNodeTop( e.Node );
        }

        private void HandleNodeMoved( object sender, JetListViewNodeEventArgs e )
        {
            UpdateTopNode();
            int movedNodeTop = GetNodeTop( e.Node );
            if ( _lastMovingNodeTop >= 0 || movedNodeTop >= 0 )
            {
                InvalidateBelow( Math.Min( _lastMovingNodeTop, movedNodeTop ) );
            }
        }

        private void HandleMultipleNodesChanged( object sender, MultipleNodesChangedEventArgs e )
        {
            _rowRenderer.SizeColumnsToContent( e.AddedNodes, e.RemovedNodes, e.ChangedNodes );
            UpdateTopNode();
            UpdateScrollRange();
            InvalidateBelow( 0 );
        }

        private void HandleFilterListChanged( object sender, EventArgs e )
        {
            ScrollSelectionInView();
        }

	    private void ScrollSelectionInView()
	    {
	        if ( _selection.FocusNode != null )
	        {
	            ScrollInView( _selection.FocusNode );
	        }
	    }

	    private void HandleNodeChanged( object sender, JetListViewNodeEventArgs e )
        {
            InvalidateRow( e.Node );
        }

        private void HandleRowHeightChanged( object sender, RowHeightChangedEventArgs e )
        {
            if ( JetListViewNodeCollection.IsNodeVisible( e.Row ) )
            {
                SetScrollRange( _scrollRange - e.OldHeight + e.NewHeight );
                InvalidateBelow( 0 );
            }
        }

        private void HandleAllRowsHeightChanged( object sender, EventArgs e )
        {
            UpdateScrollRange();
            UpdateTopNode();
            InvalidateBelow( 0 );
            ScrollSelectionInView();
        }

        internal void UpdateScrollRange()
        {
            if ( _rowRenderer == null || _calculatingScrollRange )
            {
                return;
            }

            int newScrollRange;
            _calculatingScrollRange = true;
            try
            {
                if ( _rowRenderer.AllRowsHeight >= 0 && _groupCollection == null)
                {
                    int rowHeight = _rowRenderer.AllRowsHeight;
                    if ( _rowDelimiters )
                    {
                        rowHeight++;
                    }
                    newScrollRange = rowHeight * _visibleNodeCollection.VisibleNodeCount;
                }
                else
                {
                    newScrollRange = 0;
                    lock( _nodeCollection )
                    {
                        if ( _nodeCollection.Nodes.Count > 0 )
                        {
                            IEnumerator rowEnumerator = _visibleNodeCollection.GetFullEnumerator();
                            while( rowEnumerator.MoveNext() )
                            {
                                newScrollRange += GetRowHeight( (IViewNode) rowEnumerator.Current );
                            }
                        }
                    }
                }
            }
            finally
            {
                _calculatingScrollRange = false;
            }

            SetScrollRange( newScrollRange );
        }

	    private void SetScrollRange( int newScrollRange )
	    {
	        if ( newScrollRange != _scrollRange )
	        {
	            _scrollRange = newScrollRange;
	            if ( ScrollRangeChanged != null )
	            {
	                ScrollRangeChanged( this, EventArgs.Empty );
	            }
	        }
	    }

	    private IEnumerator GetRowEnumerator()
	    {
            return GetRowEnumerator( _topNode );
	    }

        private IEnumerator GetRowEnumerator( IViewNode startNode )
        {
            return _visibleNodeCollection.GetDirectionalEnumerator( startNode, MoveDirection.Down );
        }

	    public void Draw( Graphics g, Rectangle rectangle )
        {
            lock( _nodeCollection )
            {
                IViewNode topNode = _topNode;
                if ( topNode != null )
                {
                    int curY = -_topNodeOffset + _borderSize;
                    IEnumerator enumerator = GetRowEnumerator( topNode );
                    while( enumerator.MoveNext() )
                    {
                        int oldVer = _nodeCollection.Version;
                        GroupHeaderNode groupNode = enumerator.Current as GroupHeaderNode;
                        if ( groupNode != null )
                        {
                            DrawGroupRow( g, rectangle, groupNode, ref curY );
                        }
                        else
                        {
                            JetListViewNode itemNode = (JetListViewNode) enumerator.Current;
                            DrawNodeRow( g, rectangle, itemNode, ref curY );
                        }

                        // some column draw methods may have caused events to be pumped or modified
                        // the collection - if it has changed, abort the draw and start again
                        if ( oldVer != _nodeCollection.Version )
                        {
                            InvalidateBelow( 0 );
                            break;
                        }
                        if ( curY >= rectangle.Bottom )
                        {
                            break;
                        }
                    }
                }
            }
        }

	    private void DrawNodeRow( Graphics g, Rectangle rectangle, JetListViewNode itemNode, ref int curY )
	    {
	        int itemBaseHeight = _rowRenderer.GetRowHeight( itemNode );
	        int itemHeight = itemBaseHeight;
	        if ( _rowDelimiters )
	        {
	            itemHeight++;
	        }
	        if ( curY + itemHeight >= rectangle.Top )
	        {
	            Rectangle rcRow = new Rectangle( rectangle.Left, curY, rectangle.Width, itemBaseHeight );
	            if ( _rowRenderer != null )
	            {
                    _rowRenderer.DrawRow( g, rcRow, itemNode, GetRowState( itemNode ) );
                }
	            if ( _rowDelimiters )
	            {
	                g.DrawLine( SystemPens.Control, rectangle.Left, curY + itemBaseHeight,
	                            rectangle.Width, curY + itemBaseHeight );
	            }

				// Draw an insertion mark if the current row is the insertion drop target (for insertion either above or below)
                if((itemNode == _dropTargetRow) && ((_dropTargetRenderMode & DropTargetRenderMode.InsertAny ) != 0))
                    DrawInsertMark( g, rcRow );
	        }
	        curY += itemHeight;
	    }

	    private void DrawInsertMark( Graphics g, Rectangle rcRow )
	    {
            int markY;
            if ( _dropTargetRenderMode == DropTargetRenderMode.InsertAbove )
            {
                markY = rcRow.Top-1;
            }
            else
            {
                markY = rcRow.Bottom-1;
            }
            g.DrawLine( Pens.DarkGray, rcRow.Left+2, markY+1, rcRow.Right-1, markY+1 );
            g.DrawLine( Pens.DarkGray, rcRow.Left+2, markY-1, rcRow.Left+2, markY+3 );
            g.DrawLine( Pens.DarkGray, rcRow.Right-1, markY-1, rcRow.Right-1, markY+3 );

            g.DrawLine( Pens.BlueViolet, rcRow.Left+1, markY, rcRow.Right-2, markY );
            g.DrawLine( Pens.BlueViolet, rcRow.Left+1, markY-2, rcRow.Left+1, markY+2 );
            g.DrawLine( Pens.BlueViolet, rcRow.Right-2, markY-2, rcRow.Right-2, markY+2 );
        }

	    private void DrawGroupRow( Graphics g, Rectangle rectangle, GroupHeaderNode node, ref int curY )
        {
            Rectangle rcHeader = new Rectangle( rectangle.Left, curY, rectangle.Width, _groupRenderer.GroupHeaderHeight );
            _groupRenderer.DrawGroupHeader( g, rcHeader, node, GetRowState( node ) );
            curY += rcHeader.Height;
        }

	    private RowState GetRowState( IViewNode node )
	    {
	        RowState rowState = RowState.None;
	        if ( !_controlEnabled )
	        {
	            rowState |= RowState.Disabled;
	        }
	        else
	        {
	            if ( _selection.IsNodeSelected( node ) )
	            {
	                if ( _activeSelection )
	                {
	                    rowState |= RowState.ActiveSelected;
	                }
	                else if ( !_hideSelection )
	                {
	                    rowState |= RowState.InactiveSelected;
	                }
	            }
	            if ( _activeSelection && _selection.IsNodeFocused( node ) )
	            {
	                rowState |= RowState.Focused;
	            }
	        }
	        if ( node == _dropTargetRow && _dropTargetRenderMode == DropTargetRenderMode.Over )
	        {
	            rowState |= RowState.DropTarget;
	        }
	        if ( node == _inPlaceEditedNode )
	        {
	            rowState |= RowState.InPlaceEdit;
	        }
	        return rowState;
	    }

	    public JetListViewNode GetRowAt( int y )
	    {
            int deltaY;
            return GetRowAndDelta( y, out deltaY ) as JetListViewNode;
	    }

        public IViewNode GetRowAndDelta( int y, out int deltaY )
        {
            lock( _nodeCollection )
            {
                if ( _topNode != null )
                {
                    int curY = -_topNodeOffset + _borderSize;
                    IEnumerator enumerator = GetRowEnumerator();
                    while( enumerator.MoveNext() )
                    {
                        IViewNode viewNode = (IViewNode) enumerator.Current;
                        int itemHeight = GetRowHeight( viewNode );
                        if ( y >= curY && y < curY + itemHeight )
                        {
                            deltaY = y - curY;
                            return viewNode;
                        }
                        curY += itemHeight;
                        if ( curY >= _visibleHeight + _borderSize )
                        {
                            break;
                        }
                    }
                }
            }
            deltaY = 0;
            return null;
        }

        public bool GetRowBounds( IViewNode node, out int startY, out int endY )
        {
            if ( _rowRenderer != null && _topNode != null )
            {
                lock( _nodeCollection )
                {
                    if ( _topNode != null )
                    {
                        int curY = -_topNodeOffset + _borderSize;
                        IEnumerator enumerator = GetRowEnumerator();
                        while( enumerator.MoveNext() )
                        {
                            IViewNode itemNode = (IViewNode) enumerator.Current;
                            int itemHeight = GetRowHeight( itemNode );
                            if ( itemNode == node )
                            {
                                startY = curY;
                                endY = curY + itemHeight;
                                return endY > 0;
                            }
                            curY += itemHeight;
                            if ( curY > _visibleHeight + _borderSize )
                                break;
                        }
                    }
                }
            }

            startY = -1;
            endY = -1;
            return false;
        }

        private int GetNodeTop( JetListViewNode node )
        {
            int startY, endY;
            if ( GetRowBounds( node, out startY, out endY ) )
            {
                return startY;
            }
            return -1;
        }

	    public MouseHandleResult HandleMouseDown( int x, int y, MouseButtons button, Keys modifiers )
	    {
	        MouseHandleResult rrResult = 0;

            int deltaY;
	        IViewNode row = GetRowAndDelta( y, out deltaY );
            if ( row is JetListViewNode )
            {
                rrResult = _rowRenderer.HandleMouseDown( row as JetListViewNode, x, deltaY, button, modifiers );
                if ( ( rrResult & MouseHandleResult.Handled ) != 0 )
                {
                    return rrResult;
                }
            }
            else if ( row is GroupHeaderNode )
            {
                if ( _groupRenderer.HandleMouseDown( row as GroupHeaderNode, x, deltaY, button, modifiers ) )
                {
                    return MouseHandleResult.Handled;
                }
            }

            if ( button == MouseButtons.Left && row != null )
            {
                MouseHandleResult selResult = _selection.HandleMouseDown( row, modifiers );
                if ( (rrResult & MouseHandleResult.MayInPlaceEdit ) == 0 )
                {
                    selResult = rrResult & ~MouseHandleResult.MayInPlaceEdit;
                }
                return selResult;
            }
            return 0;
        }

        public void HandleMouseUp( int x, int y, MouseButtons button, Keys modifiers )
        {
            int deltaY;
            JetListViewNode row = GetRowAndDelta( y, out deltaY ) as JetListViewNode;
            if ( row != null )
            {
                if ( _rowRenderer.HandleMouseUp( row, x, deltaY, button, modifiers ) )
                    return;
            }
            if ( button == MouseButtons.Left )
            {
                _selection.HandleMouseUp( row, modifiers );
            }
        }

        public int GetWheelScrollDistance( int lines )
        {
            // the wheel scroll can be requested between BeginUpdate() and EndUpdate(),
            // when the node which was _topNode has already been removed from the collection
            // but UpdateTopNode() was not yet called (OM-10633)
            int result = 0;
            lock ( _nodeCollection )
            {
                IViewNode topNode = _topNode;          // avoid race condition (OM-11963)
                if ( topNode != null && _visibleNodeCollection.IsNodeVisible( topNode ) )
                {
                    IVisibleNodeEnumerator enumerator = _visibleNodeCollection.GetDirectionalEnumerator( topNode,
                                                                                                         (lines < 0) ? MoveDirection.Up : MoveDirection.Down );
                    while( enumerator.MoveNext() && lines != 0 )
                    {
                        int rowHeight = GetRowHeight( enumerator.CurrentNode );
                        if ( lines > 0 )
                        {
                            result += rowHeight;
                            lines--;
                        }
                        else
                        {
                            result -= rowHeight;
                            lines++;
                        }
                    }
                }
            }
            return result;
        }

        public bool HandleKeyDown( KeyEventArgs e )
        {
            IViewNode viewNode = _selection.FocusViewNode;
            if ( viewNode is JetListViewNode )
            {
                if ( _rowRenderer.HandleKeyDown( viewNode as JetListViewNode, e ) )
                    return true;
            }
            if ( viewNode is GroupHeaderNode )
            {
                if ( _groupRenderer.HandleGroupKeyDown( viewNode as GroupHeaderNode, e ) )
                    return true;
            }
            if ( viewNode is JetListViewNode && _groupRenderer != null )
            {
                if ( _groupRenderer.HandleNodeKeyDown( viewNode as JetListViewNode, e ) )
                    return true;
            }

            return _selection.HandleKeyDown( e.KeyData );
        }

        public bool AcceptDoubleClick( int x, int y )
        {
            int deltaY;
            JetListViewNode node = GetRowAndDelta( y, out deltaY ) as JetListViewNode;
            if ( node == null )
            {
                return false;
            }
            return _rowRenderer.AcceptDoubleClick( node, x, deltaY );
        }

        public void HandleDoubleClick( int x, int y )
        {
            JetListViewNode node = GetRowAt( y );
            if ( node != null )
            {
                _rowRenderer.HandleDoubleClick( node );
            }
        }

        private void HandleSelectionStateChanged( object sender, ViewNodeStateChangeEventArgs e )
        {
            InvalidateRow( e.ViewNode );
        }

	    private void HandleFocusStateChanged( object sender, ViewNodeStateChangeEventArgs e )
        {
            InvalidateRow( e.ViewNode );
            if ( e.State )
            {
                ScrollInView( e.ViewNode );
            }
        }

        private void HandleNodesSorted( object sender, EventArgs e )
        {
            UpdateTopNode();
            InvalidateBelow( 0 );
            ScrollSelectionInView();
        }

        internal void InvalidateDropTargetRow( IViewNode node )
        {
            int startY, endY;
            if ( GetRowBounds( node, out startY, out endY ) )
            {
                // include possible space for insertion mark
                startY -= 3;
                if ( startY < 0 )
                {
                    startY = 0;
                }
                endY += 3;
                if ( endY > _scrollRange )
                {
                    endY = _scrollRange;
                }
                OnInvalidate( startY, endY );
            }
        }

        internal void InvalidateRow( IViewNode node )
        {
            int startY, endY;
            if ( GetRowBounds( node, out startY, out endY ) )
            {
                OnInvalidate( startY, endY );
            }
        }

        public void ScrollInView( IViewNode node )
	    {
            if ( _rowRenderer == null )
            {
                return;
            }

            int curY = 0;
            IEnumerator enumerator = _visibleNodeCollection.GetFullEnumerator();
            while( enumerator.MoveNext() )
            {
                IViewNode itemNode = (IViewNode) enumerator.Current;
                int itemHeight = GetRowHeight( itemNode );
                if ( itemNode == node )
                {
                    if ( curY < _scrollOffset )
                    {
                        OnRequestVerticalScroll( curY  );
                    }
                    else if ( itemHeight > _visibleHeight && curY >= _scrollOffset + _visibleHeight )
                    {
                        OnRequestVerticalScroll( curY );
                    }
                    else if ( itemHeight < _visibleHeight && curY + itemHeight > _scrollOffset + _visibleHeight )
                    {
                        OnRequestVerticalScroll( curY + itemHeight - _visibleHeight );
                    }
                    return;
                }
                curY += itemHeight;
            }
	    }

	    private void OnRequestVerticalScroll( int y )
	    {
	        if ( RequestVerticalScroll != null )
	        {
	            RequestVerticalScroll( this, new RequestScrollEventArgs( y ) );
	        }
	    }

	    public IViewNode MoveByPage( IViewNode startNode, MoveDirection direction )
	    {
	        lock( _nodeCollection )
	        {
                int movedHeight = 0;
	            IEnumerator enumerator = _visibleNodeCollection.GetDirectionalEnumerator( startNode, direction );
                IViewNode lastNode = null;
                while( enumerator.MoveNext() )
                {
                    int nextHeight = GetRowHeight( (IViewNode) enumerator.Current );
                    if ( movedHeight + nextHeight > VisibleHeight )
                    {
                        break;
                    }
                    movedHeight += nextHeight;
                    lastNode = (IViewNode) enumerator.Current;
                }
	            if ( lastNode != null )
	            {
	                return lastNode;
	            }
                return startNode;
	        }
	    }

		internal void SetDropTarget(JetListViewNode row, DropTargetRenderMode mode)
		{
			bool	bRepaint = ((row == _dropTargetRow) && (mode != _dropTargetRenderMode));	// If the row changes, it will be repainted automatically; if not, repaint if mode changes
			_dropTargetRenderMode = mode;
			SetDropTargetRow( row );
			if(bRepaint)
				InvalidateDropTargetRow( row );
		}

        internal void ClearDropTarget()
        {
            SetDropTargetRow( null );
        }

        private void SetDropTargetRow( JetListViewNode node )
        {
            if ( node != _dropTargetRow )
            {
                if ( _dropTargetRow != null )
                {
                    InvalidateDropTargetRow( _dropTargetRow );
                }
                _dropTargetRow = node;
                if ( _dropTargetRow != null )
                {
                    InvalidateDropTargetRow( _dropTargetRow );
                }
            }
        }

	    internal JetListViewNode EditedNode
	    {
	        get { return _inPlaceEditedNode; }
	        set
	        {
                if (_inPlaceEditedNode != null)
                {
                    InvalidateRow( _inPlaceEditedNode );
                }
                _inPlaceEditedNode = value;
                if (_inPlaceEditedNode != null)
                {
                    InvalidateRow( _inPlaceEditedNode );
                }
	        }
	    }
	}
}
