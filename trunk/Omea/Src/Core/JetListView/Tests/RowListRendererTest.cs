/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using NUnit.Framework;

namespace JetBrains.JetListViewLibrary.Tests
{
	[TestFixture] 
    public class RowListRendererTest
	{
        private JetListViewFilterCollection _filterCollection;
        private JetListViewNodeCollection _nodeCollection;
        private RowListRenderer _rowListRenderer;
        private MockRowRenderer _rowRenderer;
        private SelectionModel _selectionModel;
        private int _scrollRangeChanges;
        private ArrayList _rowInvalidates;
        private Rectangle _clientRect;
        private int _lastRequestScroll;
        
        [SetUp] public void SetUp()
        {
            _filterCollection = new JetListViewFilterCollection();
            _nodeCollection = new JetListViewNodeCollection( _filterCollection );
            _selectionModel = new MultipleSelectionModel( _nodeCollection );
            _rowListRenderer = new RowListRenderer( _nodeCollection, _selectionModel );
            _rowListRenderer.ActiveSelection = true;
            _rowRenderer = new MockRowRenderer();
            _rowListRenderer.RowRenderer = _rowRenderer;
            _rowRenderer.RowHeight = 17;
            _scrollRangeChanges = 0;
            _rowInvalidates = new ArrayList();
            _clientRect = new Rectangle( 0, 0, 100, 100 );
            _lastRequestScroll = -1;
        }

        private void VerifyInvalidate( int index, int startY, int endY )
        {
            InvalidateEventArgs e = (InvalidateEventArgs) _rowInvalidates [index];
            Assert.AreEqual( startY, e.StartY );
            Assert.AreEqual( endY, e.EndY );
        }

        [Test] public void SimpleTest()
        {
            JetListViewNode itemNode = _nodeCollection.Add( "Item1", null );
            _rowListRenderer.Draw( null, _clientRect );

            Assert.AreEqual( 1, _rowRenderer.DrawOperations.Count );
            DrawRowOperation op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, 0, 100, 17 ), op.Rect );
            Assert.AreEqual( itemNode, op.ItemNode );
        }

        [Test] public void VerticalScrollRange()
        {
            _rowListRenderer.ScrollRangeChanged += new EventHandler( OnScrollRangeChanged );
            _nodeCollection.Add( "Item1", null );
            Assert.AreEqual( 1, _scrollRangeChanges );
            Assert.AreEqual( 17, _rowListRenderer.ScrollRange );
        }

        [Test] public void ScrollOffsetTest()
        {
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item2", null );
            _rowListRenderer.ScrollOffset = 30;
            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 100 ) ) ;

            Assert.AreEqual( 1, _rowRenderer.DrawOperations.Count );
            DrawRowOperation op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, -13, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item2", op.ItemNode.Data );

            _rowRenderer.DrawOperations.Clear();
            _rowListRenderer.ScrollOffset = 33;
            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 100 ) ) ;

            Assert.AreEqual( 1, _rowRenderer.DrawOperations.Count );
            op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, -16, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item2", op.ItemNode.Data );
        }

        [Test] public void ScrollOffsetExactTest()
        {
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item2", null );
            _rowListRenderer.ScrollOffset = 17;
            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 100 ) ) ;

            Assert.AreEqual( 1, _rowRenderer.DrawOperations.Count );
            DrawRowOperation op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, 0, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item2", op.ItemNode.Data );
        }

        [Test] public void ScrollOffsetExactUpTest()
        {
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item2", null );
            _rowListRenderer.ScrollOffset = 17;
            _rowListRenderer.ScrollOffset = 0;
            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 100 ) ) ;

            Assert.AreEqual( 2, _rowRenderer.DrawOperations.Count );
            DrawRowOperation op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, 0, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item1", op.ItemNode.Data );
            op = (DrawRowOperation) _rowRenderer.DrawOperations [1];
            Assert.AreEqual( new Rectangle( 0, 17, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item2", op.ItemNode.Data );
        }

        [Test] public void ScrollOffsetExactUpTest2()
        {
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item2", null );
            _rowListRenderer.ScrollOffset = 20;
            _rowListRenderer.ScrollOffset = 0;
            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 100 ) ) ;

            Assert.AreEqual( 2, _rowRenderer.DrawOperations.Count );
            DrawRowOperation op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, 0, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item1", op.ItemNode.Data );
            op = (DrawRowOperation) _rowRenderer.DrawOperations [1];
            Assert.AreEqual( new Rectangle( 0, 17, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item2", op.ItemNode.Data );
        }

        [Test] public void ScrollOffsetExactUpTest3()
        {
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item2", null );
            _nodeCollection.Add( "Item3", null );
            _rowListRenderer.ScrollOffset = 34;
            _rowListRenderer.ScrollOffset = 17;
            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 100 ) ) ;

            Assert.AreEqual( 2, _rowRenderer.DrawOperations.Count );
            DrawRowOperation op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, 0, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item2", op.ItemNode.Data );
            op = (DrawRowOperation) _rowRenderer.DrawOperations [1];
            Assert.AreEqual( new Rectangle( 0, 17, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item3", op.ItemNode.Data );
        }

        [Test] public void ScrollOffsetUpTest2()
        {
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item2", null );
            _nodeCollection.Add( "Item3", null );
            _rowListRenderer.ScrollOffset = 34;
            _rowListRenderer.ScrollOffset = 33;
            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 100 ) ) ;

            Assert.AreEqual( 2, _rowRenderer.DrawOperations.Count );
            DrawRowOperation op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, -16, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item2", op.ItemNode.Data );
        }

        [Test] public void ScrollUpTest()
        {
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item2", null );
            _rowListRenderer.ScrollOffset = 30;
            _rowListRenderer.ScrollOffset = 10;
            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 100 ) ) ;

            Assert.AreEqual( 2, _rowRenderer.DrawOperations.Count );
            DrawRowOperation op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, -10, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item1", op.ItemNode.Data );

            _rowRenderer.DrawOperations.Clear();
            _rowListRenderer.ScrollOffset = 5;
            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 100 ) ) ;

            Assert.AreEqual( 2, _rowRenderer.DrawOperations.Count );
            op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, -5, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item1", op.ItemNode.Data );

            _rowRenderer.DrawOperations.Clear();
            _rowListRenderer.ScrollOffset = 0;
            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 100 ) ) ;
            Assert.AreEqual( 2, _rowRenderer.DrawOperations.Count );
            op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, 0, 100, 17 ), op.Rect );
            Assert.AreEqual( "Item1", op.ItemNode.Data );
        }

        [Test] public void GetRowAt()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1", null );
            JetListViewNode item2 = _nodeCollection.Add( "Item2", null );

            Assert.AreEqual( item, _rowListRenderer.GetRowAt( 10 )  );
            Assert.AreEqual( item2, _rowListRenderer.GetRowAt( 20 )  );
            Assert.IsNull( _rowListRenderer.GetRowAt( 40 )  );
        }

        [Test] public void GetRowBounds()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1", null );
            JetListViewNode item2 = _nodeCollection.Add( "Item2", null );

            int startY, endY;
            Assert.IsTrue( _rowListRenderer.GetRowBounds( item, out startY, out endY ) );
            Assert.AreEqual( 0, startY );
            Assert.AreEqual( 17, endY );

            _rowListRenderer.ScrollOffset = 20;
            Assert.IsFalse( _rowListRenderer.GetRowBounds( item, out startY, out endY ) );
            Assert.IsTrue( _rowListRenderer.GetRowBounds( item2, out startY, out endY ) );
            Assert.AreEqual( 14, endY );
        }   

        [Test] public void SelectByClick()
        {
            _nodeCollection.Add( "Item1", null );
            _rowListRenderer.HandleMouseDown( 10, 10, MouseButtons.Left, Keys.None );
            _rowListRenderer.Draw( null, _clientRect ) ;
            Assert.AreEqual( 1, _rowRenderer.DrawOperations.Count );
            DrawRowOperation op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( RowState.ActiveSelected | RowState.Focused, op.State );
        }

        [Test] public void InvalidateOnSelect()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1", null );
            _rowListRenderer.Invalidate += new InvalidateEventHandler( _rowListRenderer_Invalidate );
            _selectionModel.HandleMouseDown( node, Keys.None );
            Assert.IsTrue( _rowInvalidates.Count >= 1 );
            InvalidateEventArgs args = (InvalidateEventArgs) _rowInvalidates [0];
            Assert.AreEqual( 0, args.StartY );
            Assert.AreEqual( 17, args.EndY );
        }

        [Test] public void ClipBottom()
        {
            _nodeCollection.Add( "Item1", null );
            JetListViewNode node2 = _nodeCollection.Add( "Item1", null );
            _rowListRenderer.VisibleHeight = 10;
            int startY, endY;
            Assert.IsFalse( _rowListRenderer.GetRowBounds( node2, out startY, out endY ) );
        }

        [Test] public void RequestScroll()
        {
            _rowListRenderer.RequestVerticalScroll += new RequestScrollEventHandler( _rowListRenderer_OnRequestScroll );
            JetListViewNode item1 = _nodeCollection.Add( "Item1", null );
            JetListViewNode item2 = _nodeCollection.Add( "Item2", null );

            _rowListRenderer.VisibleHeight = 10;
            _rowListRenderer.ScrollInView( item1 );
            Assert.AreEqual( -1, _lastRequestScroll );
            _rowListRenderer.ScrollInView( item2  );
            Assert.AreEqual( 17, _lastRequestScroll );
        }

        [Test] public void RequestScroll_PartialVisible()
        {
            _rowListRenderer.RequestVerticalScroll += new RequestScrollEventHandler( _rowListRenderer_OnRequestScroll );
            JetListViewNode item1 = _nodeCollection.Add( "Item1", null );
            JetListViewNode item2 = _nodeCollection.Add( "Item2", null );

            _rowListRenderer.VisibleHeight = 20;
            _rowListRenderer.ScrollInView( item2  );
            Assert.AreEqual( 14, _lastRequestScroll );
        }

        [Test] public void RequestScrollOnFocusChange()
        {
            _rowListRenderer.RequestVerticalScroll += new RequestScrollEventHandler( _rowListRenderer_OnRequestScroll );
            JetListViewNode item1 = _nodeCollection.Add( "Item1", null );
            JetListViewNode item2 = _nodeCollection.Add( "Item2", null );
            _rowListRenderer.VisibleHeight = 10;

            _selectionModel.HandleKeyDown( Keys.Down );
            Assert.AreEqual( -1, _lastRequestScroll );
            _selectionModel.HandleKeyDown( Keys.Down );
            Assert.AreEqual( 17, _lastRequestScroll );
        }

        [Test] public void PagingProviderTest()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1", null );
            JetListViewNode item2 = _nodeCollection.Add( "Item2", null );
            JetListViewNode item3 = _nodeCollection.Add( "Item3", null );
            JetListViewNode item4 = _nodeCollection.Add( "Item4", null );

            _rowListRenderer.VisibleHeight = 3 * _rowRenderer.RowHeight + 5;
            Assert.AreEqual( item3, _rowListRenderer.MoveByPage( item1, MoveDirection.Down ) );
            Assert.AreEqual( item4, _rowListRenderer.MoveByPage( item3, MoveDirection.Down ) );
        }

        [Test] public void InvalidateInvisibleRow()
        {
            for( int i=0; i<4; i++ )
            {
                _nodeCollection.Add( "Item" + i );
            }

            _rowListRenderer.Invalidate += new InvalidateEventHandler( _rowListRenderer_Invalidate );
            _rowListRenderer.VisibleHeight = 20;
            _rowListRenderer.InvalidateRow( _nodeCollection.Nodes [3] );
            Assert.AreEqual( 0, _rowInvalidates.Count );
        }

        [Test] public void InactiveSelection()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1", null );
            _selectionModel.HandleMouseDown( item1, Keys.None );
            
            _rowListRenderer.Invalidate += new InvalidateEventHandler( _rowListRenderer_Invalidate );
            _rowListRenderer.ActiveSelection = false;
            Assert.AreEqual( 1, _rowInvalidates.Count );

            _rowListRenderer.Draw( null, _clientRect );

            Assert.AreEqual( 1, _rowRenderer.DrawOperations.Count );
            DrawRowOperation op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( new Rectangle( 0, 0, 100, 17 ), op.Rect );
            Assert.AreEqual( RowState.InactiveSelected, op.State );
        }

        [Test] public void HandleMouseDown()
        {
            _nodeCollection.Add( "Item1", null );
            JetListViewNode item2 = _nodeCollection.Add( "Item2", null );

            _rowListRenderer.HandleMouseDown( 10, 30, MouseButtons.Left,  Keys.None );
            Assert.AreEqual( new Point( 10, 13 ), _rowRenderer.LastMouseDownPoint );
            Assert.AreEqual( item2, _rowRenderer.LastMouseDownNode );
        }

        [Test] public void ScrollInView_BorderSize()
        {
            JetListViewNode node1 = _nodeCollection.Add( "Item1", null );
            JetListViewNode node2 = _nodeCollection.Add( "Item2", null );

            _rowListRenderer.BorderSize = 15;
            _rowListRenderer.VisibleHeight = 34;
            _rowListRenderer.RequestVerticalScroll += new RequestScrollEventHandler( _rowListRenderer_OnRequestScroll );

            _rowListRenderer.ScrollInView( node2 );
            Assert.AreEqual( -1, _lastRequestScroll );
        }

        [Test] public void ScrollRangeChangeOnRemove()
        {
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item2", null );

            _rowListRenderer.ScrollRangeChanged += new EventHandler( OnScrollRangeChanged );
            _nodeCollection.Remove( "Item1", null );

            Assert.AreEqual( 1, _scrollRangeChanges );
            Assert.AreEqual( 17, _rowListRenderer.ScrollRange );
        }

        [Test] public void InvalidateOnRemove()
        {
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item2", null );

            _rowListRenderer.VisibleHeight = 40;
            _rowListRenderer.Invalidate += new InvalidateEventHandler( _rowListRenderer_Invalidate );
            _nodeCollection.Remove( "Item1", null );

            Assert.AreEqual( 1, _rowInvalidates.Count );
            VerifyInvalidate( 0, 0, 40 );
        }

	    [Test] public void InvalidateOnMove()
        {
            _nodeCollection.SetItemComparer( null, new Comparer( CultureInfo.CurrentCulture ) );
            MockComparable cmp1 = new MockComparable( "Item1" );
            MockComparable cmp2 = new MockComparable( "Item2" );
            _nodeCollection.Add( cmp1 );
            _nodeCollection.Add( cmp2 );

            _rowListRenderer.Invalidate += new InvalidateEventHandler( _rowListRenderer_Invalidate );
	        cmp1.Value = "Item3";
            _nodeCollection.Update( cmp1 );

            Assert.AreEqual( 1, _rowInvalidates.Count );
        }

        [Test] public void DropTargetInvalidate()
        {
            _nodeCollection.Add( "Item" );

            _rowListRenderer.Invalidate += new InvalidateEventHandler( _rowListRenderer_Invalidate );
            _rowListRenderer.SetDropTarget( _rowListRenderer.GetRowAt( 10 ), DropTargetRenderMode.Over );
            Assert.AreEqual( 1, _rowInvalidates.Count );
            VerifyInvalidate( 0, 0, 17 );

            _rowListRenderer.SetDropTarget( _rowListRenderer.GetRowAt( 12 ), DropTargetRenderMode.Over );
            Assert.AreEqual( 1, _rowInvalidates.Count );
        }

        [Test] public void DropTargetDraw()
        {
            _nodeCollection.Add( "Item" );

            _rowListRenderer.SetDropTarget( _rowListRenderer.GetRowAt( 10 ), DropTargetRenderMode.Over );
            _rowListRenderer.Draw( null, _clientRect );
            DrawRowOperation op = (DrawRowOperation) _rowRenderer.DrawOperations [0];
            Assert.AreEqual( RowState.DropTarget, op.State );
        }

        [Test] public void ScrollRangeChangeOnRefilter()
        {
            _nodeCollection.Add( "!Item1" );
            _nodeCollection.Add( "Item2" );

            _rowListRenderer.ScrollRangeChanged += new EventHandler( OnScrollRangeChanged );
            _filterCollection.Add( new MockFilter() );

            Assert.AreEqual( 1, _scrollRangeChanges );
        }

        [Test] public void ScrollRangeChangeOnExpand()
        {
            JetListViewNode node = _nodeCollection.Add( "Parent" );
            _nodeCollection.Add( "Child", node );

            _rowListRenderer.ScrollRangeChanged += new EventHandler( OnScrollRangeChanged );
            node.Expanded = true;
            Assert.AreEqual( 1, _scrollRangeChanges );
        }

        [Test] public void InvalidateOnChildCountChange()
        {
            JetListViewNode node = _nodeCollection.Add( "Parent" );

            _rowListRenderer.Invalidate += new InvalidateEventHandler( _rowListRenderer_Invalidate );
            _nodeCollection.Add( "Child", node );

            Assert.AreEqual( 1, _rowInvalidates.Count );
        }

        [Test] public void ChangeScrollRangeOnClear()
        {
            _nodeCollection.Add( "Parent" );

            _nodeCollection.Nodes.Clear();
            Assert.AreEqual( 0, _rowListRenderer.ScrollRange );
        }

        [Test] public void ScrollOnFocusLost()  // OM-7869
        {
            JetListViewNode node = _nodeCollection.Add( "Item" );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );
            _rowListRenderer.VisibleHeight = 10;
            
            _selectionModel.SelectAndFocusNode( node2 );
            _rowListRenderer.ScrollOffset = 0;

            _rowListRenderer.RequestVerticalScroll += new RequestScrollEventHandler( _rowListRenderer_OnRequestScroll );
            _selectionModel.Clear();
            Assert.AreEqual( -1, _lastRequestScroll );
        }

        [Test] public void ItemBoundsWithBorder()
        {
            _rowListRenderer.BorderSize = 30;
            _rowListRenderer.VisibleHeight = 20;
            
            JetListViewNode node = _nodeCollection.Add( "Item" );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );

            int startY, endY;
            Assert.IsTrue( _rowListRenderer.GetRowBounds( node2, out startY, out endY ) );
            Assert.AreEqual( 47, startY );
        }

        [Test] public void ScrollOnNewNodeOnTop_PartHidden()
        {
            _nodeCollection.SetItemComparer( null, Comparer.Default );

            _rowListRenderer.RequestVerticalScroll += new RequestScrollEventHandler( _rowListRenderer_OnRequestScroll );
            _nodeCollection.Add( "B" );
            _rowListRenderer.ScrollOffset = 5;
            _nodeCollection.Add( "A" );
            Assert.AreEqual( 22, _lastRequestScroll );
        }

        [Test] public void ScrollOnNewNodeOnTop_NothingHidden()
        {
            _nodeCollection.SetItemComparer( null, Comparer.Default );

            _rowListRenderer.RequestVerticalScroll += new RequestScrollEventHandler( _rowListRenderer_OnRequestScroll );
            _nodeCollection.Add( "B" );
            _nodeCollection.Add( "A" );
            _rowListRenderer.Draw( null, _clientRect ) ;
            Assert.AreEqual( 2, _rowRenderer.DrawOperations.Count );
            Assert.AreEqual( -1, _lastRequestScroll );
        }


	    private void OnScrollRangeChanged( object sender, EventArgs e )
	    {
            _scrollRangeChanges++;
	    }

        private void _rowListRenderer_Invalidate( object sender, InvalidateEventArgs e )
        {
            _rowInvalidates.Add( e );
        }

        private void _rowListRenderer_OnRequestScroll( object sender, RequestScrollEventArgs e )
        {
	        _lastRequestScroll = e.Coord;
        }
    }
}
