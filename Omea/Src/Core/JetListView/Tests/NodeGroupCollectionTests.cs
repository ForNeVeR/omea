// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;

namespace JetBrains.JetListViewLibrary.Tests
{
	[TestFixture]
    public class NodeGroupCollectionTests
	{
        private JetListViewNodeCollection _nodeCollection;
        private NodeGroupCollection _groupCollection;
	    private RowListRenderer _rowListRenderer;
	    private MockRowRenderer _rowRenderer;
        private MockGroupProvider _groupProvider;
	    private GroupHeaderNode _lastEventGroup;
	    private int _lastInvalidateStartY;
	    private MultipleSelectionModel _selection;
	    private MockGroupRenderer _groupRenderer;

	    [SetUp] public void SetUp()
        {
            _nodeCollection = new JetListViewNodeCollection( null );
            _groupProvider = new MockGroupProvider();
            _groupCollection = new NodeGroupCollection( _nodeCollection, _groupProvider );

	        _selection = new MultipleSelectionModel( _nodeCollection );
	        _rowListRenderer = new RowListRenderer( _nodeCollection, _selection );
            _rowRenderer = new MockRowRenderer();
            _rowRenderer.RowHeight = 20;
            _rowListRenderer.RowRenderer = _rowRenderer;

            _groupRenderer = new MockGroupRenderer( 10 );
            _rowListRenderer.GroupRenderer = _groupRenderer;
            _rowListRenderer.NodeGroupCollection = _groupCollection;

	        _lastEventGroup = null;
            _lastInvalidateStartY = -1;
        }

        private void VerifyGroupHeader( IEnumerator enumerator, string text, bool expanded )
        {
            Assert.IsTrue( enumerator.MoveNext(), "Expected group header but found end of enumeration" );
            Assert.IsNotNull( enumerator.Current, "Expected group header but found null" );
            GroupHeaderNode headerNode = enumerator.Current as GroupHeaderNode;
            Assert.IsNotNull( headerNode, "Expected group header but found " + enumerator.Current );
            Assert.AreEqual( expanded, headerNode.Expanded );
            Assert.AreEqual( text, headerNode.Text );
        }

        private void VerifyNode( IEnumerator enumerator, object data )
        {
            Assert.IsTrue( enumerator.MoveNext() );
            Assert.IsNotNull( enumerator.Current, "Expected node but found null" );
            JetListViewNode itemNode = enumerator.Current as JetListViewNode;
            Assert.IsNotNull( itemNode, "Expected JetListViewNode but found " + enumerator.Current.ToString() );
            Assert.AreEqual( data, itemNode.Data );
        }

        private void EnumerateGroups()
        {
            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            while( enumerator.MoveNext() ) ;
        }

        [Test] public void SimpleTest()
        {
            _nodeCollection.Add( "Test" );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            VerifyGroupHeader( enumerator, "Test", true );
            VerifyNode( enumerator, "Test" );
            Assert.IsFalse( enumerator.MoveNext() );
        }

        [Test] public void MultipleGroups()
        {
            _nodeCollection.Add( "Test" );
            _nodeCollection.Add( "Test2" );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            VerifyGroupHeader( enumerator, "Test", true );
            VerifyNode( enumerator, "Test" );
            VerifyGroupHeader( enumerator, "Test2", true );
            VerifyNode( enumerator, "Test2" );
            Assert.IsFalse( enumerator.MoveNext() );
        }

        [Test] public void CollapseGroup()
        {
            _nodeCollection.Add( "Test" );
            _nodeCollection.Add( "Test2" );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            GroupHeaderNode node = enumerator.Current as GroupHeaderNode;
            node.Expanded = false;

            enumerator = _groupCollection.GetFullEnumerator();
            VerifyGroupHeader( enumerator, "Test", false );
            VerifyGroupHeader( enumerator, "Test2", true );
            VerifyNode( enumerator, "Test2" );
            Assert.IsFalse( enumerator.MoveNext() );
        }

        [Test] public void EnumerateWithChildren()
        {
            JetListViewNode testNode = _nodeCollection.Add( "Test" );
            _nodeCollection.Add( "TestChild", testNode );
            testNode.Expanded = true;
            _nodeCollection.Add( "Test2" );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            VerifyGroupHeader( enumerator, "Test", true );
            VerifyNode( enumerator, "Test" );
            VerifyNode( enumerator, "TestChild" );
            VerifyGroupHeader( enumerator, "Test2", true );
            VerifyNode( enumerator, "Test2" );
            Assert.IsFalse( enumerator.MoveNext() );
        }

        [Test] public void ChildrenWithSameGroup()
        {
            JetListViewNode testNode = _nodeCollection.Add( "Test" );
            _nodeCollection.Add( "TestChild", testNode );
            _nodeCollection.Add( "Test2" );
            _groupProvider.SetGroup( "Test2", "Test" );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            VerifyGroupHeader( enumerator, "Test", true );
            VerifyNode( enumerator, "Test" );
            VerifyNode( enumerator, "Test2" );
        }

        [Test] public void GroupAdded()
        {
            _groupCollection.GroupAdded += new GroupEventHandler( HandleGroupEvent );
            _nodeCollection.Add( "Test" );
            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            Assert.IsNotNull( _lastEventGroup );
            Assert.AreEqual( "Test", _lastEventGroup.Text );
        }

        [Test] public void GroupExpandChanged()
        {
            _groupCollection.GroupExpandChanged += new GroupEventHandler( HandleGroupEvent );
            _nodeCollection.Add( "Test" );
            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            GroupHeaderNode groupHeaderNode = (GroupHeaderNode) enumerator.Current;
            groupHeaderNode.Expanded = false;
            Assert.AreEqual( groupHeaderNode, _lastEventGroup );
        }

	    [Test] public void RenderGroup()
        {
            _nodeCollection.Add( "Test" );

            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 40 ) );
            Assert.AreEqual( "Test", _groupRenderer.LastGroupText );
            Assert.AreEqual( new Rectangle( 0, 0, 100, 10 ), _groupRenderer.LastGroupRect );

            Assert.AreEqual( 1, _rowRenderer.DrawOperations.Count );
        }

	    [Test] public void GroupScrollRange()
        {
            _nodeCollection.Add( "Test" );

            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 40 ) );

            Assert.AreEqual( 30, _rowListRenderer.ScrollRange );
        }

	    [Test] public void GroupRowAt()
        {
            _nodeCollection.Add( "Test" );

            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 40 ) );

            int deltaY;
            IViewNode viewNode = _rowListRenderer.GetRowAndDelta( 5, out deltaY );
            GroupHeaderNode headerNode = viewNode as GroupHeaderNode;
            Assert.IsNotNull( headerNode );
            Assert.AreEqual( 5, deltaY );

            viewNode = _rowListRenderer.GetRowAndDelta( 15, out deltaY );
            JetListViewNode lvNode = viewNode as JetListViewNode;
            Assert.IsNotNull( lvNode );
            Assert.AreEqual( 5, deltaY );
        }

	    [Test] public void GroupMouseDown()
        {
            _nodeCollection.Add( "Test" );

            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 40 ) );

            _rowListRenderer.HandleMouseDown( 5, 5, MouseButtons.Left, Keys.None );
            Assert.AreEqual( new Point( 5, 5 ), _groupRenderer.LastMouseDownPoint );
        }

        [Test] public void GroupSelection()
        {
            _nodeCollection.Add( "Test" );

            _rowListRenderer.Draw( null, new Rectangle( 0, 0, 100, 40 ) );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            GroupHeaderNode group = (GroupHeaderNode) enumerator.Current;

            _groupRenderer.MouseDownHandled = false;
            _rowListRenderer.HandleMouseDown( 25, 5, MouseButtons.Left, Keys.None );
            Assert.IsTrue( _selection.IsNodeSelected( group ), "Group must be selected" );
        }

        [Test] public void InvalidateOnGroupExpand()
        {
            _nodeCollection.Add( "Test" );

            _rowListRenderer.Invalidate += new InvalidateEventHandler( HandleInvalidate );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            GroupHeaderNode groupHeaderNode = (GroupHeaderNode) enumerator.Current;
            groupHeaderNode.Expanded = false;

            Assert.AreEqual( 0, _lastInvalidateStartY );
        }

        [Test] public void CollapsedGroupScrollRange()
        {
            _nodeCollection.Add( "Test" );

            _rowListRenderer.Invalidate += new InvalidateEventHandler( HandleInvalidate );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            GroupHeaderNode groupHeaderNode = (GroupHeaderNode) enumerator.Current;
            groupHeaderNode.Expanded = false;

            Assert.AreEqual( 10, _rowListRenderer.ScrollRange );
        }

        [Test] public void TestDirectionalEnumeratorFromNode()
        {
            JetListViewNode testNode = _nodeCollection.Add( "Test" );
            IEnumerator enumerator = _groupCollection.GetDirectionalEnumerator( testNode, MoveDirection.Down );
            VerifyNode( enumerator, "Test" );
            Assert.IsFalse( enumerator.MoveNext() );
        }

        [Test] public void TestDirectionalEnumeratorFromGroup()
        {
            _nodeCollection.Add( "Test" );
            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            GroupHeaderNode groupHeaderNode = (GroupHeaderNode) enumerator.Current;

            enumerator = _groupCollection.GetDirectionalEnumerator( groupHeaderNode, MoveDirection.Down );
            VerifyGroupHeader( enumerator, "Test", true );
            VerifyNode( enumerator, "Test" );
            Assert.IsFalse( enumerator.MoveNext() );
        }

        [Test] public void VisibleParent()
        {
            JetListViewNode testNode = _nodeCollection.Add( "Test" );
            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            GroupHeaderNode groupHeaderNode = (GroupHeaderNode) enumerator.Current;
            groupHeaderNode.Expanded = false;

            Assert.AreEqual( groupHeaderNode, _groupCollection.GetVisibleParent( testNode ) );
            Assert.IsFalse( _groupCollection.IsNodeVisible( testNode ) );
        }

        [Test] public void EnumerateUpFromGroup()
        {
            _nodeCollection.Add( "Test" );
            _nodeCollection.Add( "Test2" );
            _groupProvider.SetGroup( "Test2", "Test" );
            _nodeCollection.Add( "Test3" );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();
            while( !(enumerator.Current is GroupHeaderNode ) )
            {
                enumerator.MoveNext();
            }

            enumerator = _groupCollection.GetDirectionalEnumerator( (IViewNode) enumerator.Current, MoveDirection.Up );
            VerifyGroupHeader( enumerator, "Test3", true );
            VerifyNode( enumerator, "Test2" );
            VerifyNode( enumerator, "Test" );
            VerifyGroupHeader( enumerator, "Test", true );
            Assert.IsFalse( enumerator.MoveNext(), "expected end of enumeration" );
        }

        [Test] public void EnumerateUpFromNode()
        {
            _nodeCollection.Add( "Test" );
            _nodeCollection.Add( "Test2" );
            _groupProvider.SetGroup( "Test2", "Test" );
            _nodeCollection.Add( "Test3" );
            JetListViewNode test4Node = _nodeCollection.Add( "Test4" );
            _groupProvider.SetGroup( "Test4", "Test3" );

            IEnumerator enumerator = _groupCollection.GetDirectionalEnumerator( test4Node, MoveDirection.Up );
            VerifyNode( enumerator, "Test4" );
            VerifyNode( enumerator, "Test3" );
            VerifyGroupHeader( enumerator, "Test3", true );
        }

        [Test] public void RowBounds()
        {
            _nodeCollection.Add( "Test" );
            _nodeCollection.Add( "Test2" );
            _groupProvider.SetGroup( "Test2", "Test" );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            GroupHeaderNode groupHeaderNode = (GroupHeaderNode) enumerator.Current;
            enumerator.MoveNext();
            JetListViewNode testNode = (JetListViewNode) enumerator.Current;
            enumerator.MoveNext();
            JetListViewNode test2Node = (JetListViewNode) enumerator.Current;

            int startY, endY;
            Assert.IsTrue( _rowListRenderer.GetRowBounds( groupHeaderNode, out startY, out endY ) );
            Assert.AreEqual( 0, startY );
            Assert.AreEqual( 10, endY );

            Assert.IsTrue( _rowListRenderer.GetRowBounds( testNode, out startY, out endY ) );
            Assert.AreEqual( 10, startY );
            Assert.AreEqual( 30, endY );

            Assert.IsTrue( _rowListRenderer.GetRowBounds( test2Node, out startY, out endY ) );
            Assert.AreEqual( 30, startY );
            Assert.AreEqual( 50, endY );
        }

        [Test] public void TopNodeOverwrite()
        {
            _nodeCollection.Add( "Test" );
            _nodeCollection.Add( "Test2" );
            _groupProvider.SetGroup( "Test2", "Test" );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            GroupHeaderNode groupHeaderNode = (GroupHeaderNode) enumerator.Current;
            enumerator.MoveNext();
            JetListViewNode testNode = (JetListViewNode) enumerator.Current;
            enumerator.MoveNext();
            JetListViewNode test2Node = (JetListViewNode) enumerator.Current;

            enumerator = _groupCollection.GetDirectionalEnumerator( test2Node, MoveDirection.Down );
            // this could overwrite top node of group to test2Node, and subsequent iteration would
            // return incorrect results
            enumerator.MoveNext();

            enumerator = _groupCollection.GetDirectionalEnumerator( groupHeaderNode, MoveDirection.Down );
            VerifyGroupHeader( enumerator, "Test", true );
            VerifyNode( enumerator, "Test" );
        }

        [Test] public void GroupDeleted()
        {
            _nodeCollection.Add( "Test" );

            EnumerateGroups();

            _groupCollection.GroupRemoved += new GroupEventHandler( HandleGroupEvent );

            _nodeCollection.Remove( "Test", null );
            Assert.AreEqual( "Test", _lastEventGroup.Text );
        }

	    [Test] public void GroupDeletedOnChange()
        {
            _nodeCollection.Add( "Test" );

            IEnumerator enumerator = _groupCollection.GetFullEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();

            _groupCollection.GroupRemoved += new GroupEventHandler( HandleGroupEvent );

            _groupProvider.SetGroup( "Test", "Rest" );
            _nodeCollection.Update( "Test" );
            Assert.AreEqual( "Test", _lastEventGroup.Text );
        }

        [Test] public void MoveSelectionOnGroupDelete()
        {
            JetListViewNode node = _nodeCollection.Add( "Test" );
            JetListViewNode node2 = _nodeCollection.Add( "Test2" );
            EnumerateGroups();
            _selection.HandleMouseDown( node2, Keys.None );
            Assert.IsTrue( _selection.IsNodeSelected( node2 ) );
            _nodeCollection.Remove( "Test2", null );
            Assert.IsTrue( _selection.IsNodeSelected( node ) );
        }

	    private void HandleGroupEvent( object sender, GroupEventArgs e )
	    {
	        _lastEventGroup = e.GroupHeader;
	    }

        private void HandleInvalidate( object sender, InvalidateEventArgs e )
        {
            _lastInvalidateStartY = e.StartY;
        }

        private class MockGroupProvider: IGroupProvider
        {
            private Hashtable _groups = new Hashtable();

            public string GetGroupName( object item )
            {
                string group = (string) _groups [item];
                if ( group != null )
                {
                    return group;
                }
                return item.ToString();
            }

	        public void SetGroup( object item, string groupName )
	        {
	            _groups [item] = groupName;
	        }
        }

	    private class MockGroupRenderer: IGroupRenderer
        {
            private int _rowHeight;
            private string _lastGroupText;
            private Rectangle _lastGroupRect;
            private Point _lastMouseDownPoint;
	        private bool _mouseDownHandled = true;

	        public MockGroupRenderer( int rowHeight )
            {
                _rowHeight = rowHeight;
            }

            public string LastGroupText
            {
                get { return _lastGroupText; }
            }

            public Rectangle LastGroupRect
            {
                get { return _lastGroupRect; }
            }

            public Point LastMouseDownPoint
            {
                get { return _lastMouseDownPoint; }
            }

            public void DrawGroupHeader( Graphics g, Rectangle rectangle, GroupHeaderNode node, RowState rowState )
            {
                _lastGroupText = node.Text;
                _lastGroupRect = rectangle;
            }

            public bool HandleMouseDown( GroupHeaderNode node, int x, int y, MouseButtons button, Keys modifiers )
            {
                _lastMouseDownPoint = new Point( x, y );
                return _mouseDownHandled;
            }

	        public bool HandleGroupKeyDown( GroupHeaderNode node, KeyEventArgs e )
	        {
	            throw new NotImplementedException();
	        }

	        public bool HandleNodeKeyDown( JetListViewNode viewNode, KeyEventArgs e )
	        {
	            throw new NotImplementedException();
	        }

	        public int GroupHeaderHeight
            {
                get { return _rowHeight; }
            }

	        public int VisibleWidth
	        {
	            get { throw new NotImplementedException(); }
	            set { throw new NotImplementedException(); }
	        }

	        public Color GroupHeaderColor
	        {
	            get { throw new NotImplementedException(); }
	            set { throw new NotImplementedException(); }
	        }

	        public bool MouseDownHandled
	        {
	            get { return _mouseDownHandled; }
	            set { _mouseDownHandled = value; }
	        }
        }
	}
}
