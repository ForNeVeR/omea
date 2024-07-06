// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Globalization;
using NUnit.Framework;

namespace JetBrains.JetListViewLibrary.Tests
{
	[TestFixture]
    public class NodeCollectionTests
	{
        private JetListViewFilterCollection _filters;
        private JetListViewNodeCollection _nodeCollection;
        private ArrayList _eventNodes;
        private int _eventCount;

        [SetUp] public void SetUp()
        {
            _filters = new JetListViewFilterCollection();
            _nodeCollection = new JetListViewNodeCollection( _filters );
            _eventNodes = new ArrayList();
            _eventCount = 0;
        }

        private JetListViewNode FetchNext( IEnumerator enumerator )
        {
            if ( !enumerator.MoveNext() )
                return null;

            return (JetListViewNode) enumerator.Current;
        }

        private void VerifyItems( IEnumerator enumerator, params object[] items )
        {
            for( int i=0; i<items.Length; i++ )
            {
                Assert.IsTrue( enumerator.MoveNext(), "Not enough items in enumerator: {0} expected, {1} found",
                    items.Length, i );
                JetListViewNode node = (JetListViewNode) enumerator.Current;
                Assert.AreEqual( items [i], node.Data, "Mismatch in item {0}", i );
            }
            Assert.IsFalse( enumerator.MoveNext(), "Too many items in enumerator: {0} expected", items.Length );
        }

        [Test] public void Add()
        {
            _nodeCollection.Add( "Item1", null );

            IEnumerator enumerator = _nodeCollection.VisibleItems.GetEnumerator();
            Assert.AreEqual( "Item1", FetchNext( enumerator ).Data );
            Assert.IsFalse( enumerator.MoveNext() );
        }

        [Test] public void AddTwo()
        {
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item2", null );

            IEnumerator enumerator = _nodeCollection.VisibleItems.GetEnumerator();
            VerifyItems( enumerator, "Item1", "Item2" );
        }

        [Test] public void AddThree()
        {
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item2", null );
            _nodeCollection.Add( "Item3", null );

            IEnumerator enumerator = _nodeCollection.VisibleItems.GetEnumerator();
            Assert.AreEqual( "Item1", FetchNext( enumerator ).Data );
            Assert.AreEqual( "Item2", FetchNext( enumerator ).Data );
            Assert.AreEqual( "Item3", FetchNext( enumerator ).Data );
            Assert.IsFalse( enumerator.MoveNext() );
        }

        [Test] public void AddChild()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item11", node );
            _nodeCollection.Add( "Item2", null );

            IEnumerator enumerator = _nodeCollection.VisibleItems.GetEnumerator();
            JetListViewNode itemData = FetchNext( enumerator );
            Assert.AreEqual( "Item1", itemData.Data );
            Assert.AreEqual( CollapseState.Collapsed, itemData.CollapseState );
            itemData = FetchNext( enumerator );
            Assert.AreEqual( "Item2", itemData.Data );
            Assert.AreEqual( CollapseState.NoChildren, itemData.CollapseState );
            Assert.IsFalse( enumerator.MoveNext() );
        }

        [Test] public void ExpandNode()
        {
            JetListViewNode node1 = _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item11", node1 );
            _nodeCollection.Add( "Item2", null );

            node1.Expanded = true;

            IEnumerator enumerator = _nodeCollection.VisibleItems.GetEnumerator();
            JetListViewNode itemData = FetchNext( enumerator );
            Assert.AreEqual( "Item1", itemData.Data );
            Assert.AreEqual( CollapseState.Expanded, itemData.CollapseState );

            itemData = FetchNext( enumerator );
            Assert.AreEqual( "Item11", itemData.Data );
            Assert.AreEqual( CollapseState.NoChildren, itemData.CollapseState );

            itemData = FetchNext( enumerator );
            Assert.AreEqual( "Item2", itemData.Data );
            Assert.AreEqual( CollapseState.NoChildren, itemData.CollapseState );
            Assert.IsFalse( enumerator.MoveNext() );
        }

        [Test] public void NodeAddedEvent()
        {
            _nodeCollection.NodeAdded += new JetListViewNodeEventHandler( HandleNodeEvent );
            JetListViewNode node = _nodeCollection.Add( "Item1", null );
            Assert.AreEqual( 1, _eventNodes.Count );
            Assert.AreEqual( node, _eventNodes [0] );
        }

        [Test] public void VisibleNodeCount()
        {
            Assert.AreEqual( 0, _nodeCollection.VisibleItemCount );
            _nodeCollection.Add( "Item1", null );
            Assert.AreEqual( 1, _nodeCollection.VisibleItemCount );
        }

        [Test] public void EnumerateItems()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item11", node );
            _nodeCollection.Add( "Item2", null );

            IEnumerator enumerator = _nodeCollection.EnumerateNodesForward( node );
            VerifyItems( enumerator, "Item1", "Item11", "Item2" );
        }

        [Test] public void EnumerateFromMiddle()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1", null );
            JetListViewNode node2 = _nodeCollection.Add( "Item11", node );
            _nodeCollection.Add( "Item2", null );

            IEnumerator enumerator = _nodeCollection.EnumerateVisibleNodesForward( node2 );
            VerifyItems( enumerator, "Item11", "Item2" );
        }

        [Test] public void EnumerateBackward()
        {
            JetListViewNode node1 = _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item11", node1 );
            JetListViewNode node3 = _nodeCollection.Add( "Item2", null );

            IEnumerator enumerator = _nodeCollection.EnumerateVisibleNodesBackward( node3 );
            VerifyItems( enumerator, "Item2", "Item1" );

            enumerator = _nodeCollection.EnumerateNodesBackward( node3 );
            VerifyItems( enumerator, "Item2", "Item11", "Item1" );
        }

        [Test] public void LastItem()
        {
            JetListViewNode node1 = _nodeCollection.Add( "Item1", null );
            JetListViewNode node3 = _nodeCollection.Add( "Item2", null );
            JetListViewNode node22 = _nodeCollection.Add( "Item22", node3 );

            Assert.AreEqual( node22, _nodeCollection.LastNode );
        }

        [Test] public void NodeIndexer()
        {
            JetListViewNode node1 = _nodeCollection.Add( "Item1", null );
            JetListViewNode node11 = _nodeCollection.Add( "Item11", node1 );

            Assert.AreEqual( node1, _nodeCollection.Nodes [0] );
            Assert.AreEqual( node11, _nodeCollection.Nodes [0].Nodes [0] );
        }

        [Test] public void RemoveItem()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1", null );
            _nodeCollection.Remove( "Item1", null );
            Assert.AreEqual( 0, _nodeCollection.Nodes.Count );
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void RemoveNotAdded()
        {
            _nodeCollection.Remove( "Item1", null );
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void RemoveWrongParent()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1", null );
            _nodeCollection.Add( "Item11", node );
            _nodeCollection.Remove( "Item11", null );
        }

        [Test] public void RemoveItemEvent()
        {
            _nodeCollection.NodeRemoved += new JetListViewNodeEventHandler( HandleNodeEvent );
            _nodeCollection.Add( "Item1", null );
            _nodeCollection.Remove( "Item1", null );
            Assert.AreEqual( 1, _eventNodes.Count );
        }

        [Test] public void NodeComparer()
        {
            _nodeCollection.SetItemComparer( null, new Comparer( CultureInfo.CurrentCulture ) );
            _nodeCollection.Add( "Item2" );
            _nodeCollection.Add( "Item1" );

            Assert.AreEqual( "Item1", _nodeCollection.Nodes [0].Data );
            Assert.AreEqual( "Item2", _nodeCollection.Nodes [1].Data );
        }

        [Test] public void RefreshNode()
        {
            _nodeCollection.SetItemComparer( null, new Comparer( CultureInfo.CurrentCulture ) );
            MockComparable cmp1 = new MockComparable( "Item1" );
            MockComparable cmp2 = new MockComparable( "Item2" );
            _nodeCollection.Add( cmp1 );
            _nodeCollection.Add( cmp2 );

            cmp1.Value = "Item3";
            _nodeCollection.Update( cmp1 );
            Assert.AreEqual( cmp2, _nodeCollection.Nodes [0].Data );
            Assert.AreEqual( cmp1, _nodeCollection.Nodes [1].Data );
        }

        [Test] public void NodeFilter()
        {
            _filters.Add( new MockFilter() );
            _nodeCollection.Add( "!Item1" );
            _nodeCollection.Add( "Item2" );

            IEnumerator enumerator = _nodeCollection.VisibleItems.GetEnumerator();
            VerifyItems( enumerator, "Item2" );

            Assert.AreEqual( 1, _nodeCollection.VisibleItemCount );
        }

        [Test] public void FilterListChange()
        {
            _nodeCollection.Add( "!Item1" );
            _nodeCollection.Add( "Item2" );

            MockFilter mockFilter = new MockFilter();
            _filters.Add( mockFilter );
            Assert.AreEqual( 1, _nodeCollection.VisibleItemCount );
            _filters.Remove( mockFilter );
            Assert.AreEqual( 2, _nodeCollection.VisibleItemCount );
        }

        [Test] public void FilterUpdate()
        {
            MockFilter mockFilter = new MockFilter();
            _filters.Add( mockFilter );

            _nodeCollection.Add( "!Item1" );
            _nodeCollection.Add( "?Item2" );

            VerifyItems( _nodeCollection.VisibleItems.GetEnumerator(), "?Item2" );

            mockFilter.SetFilterString( "?" );
            VerifyItems( _nodeCollection.VisibleItems.GetEnumerator(), "!Item1" );
        }

        [Test] public void FilterRecursive()
        {
            MockFilter mockFilter = new MockFilter();
            _filters.Add( mockFilter );

            JetListViewNode node = _nodeCollection.Add( "!Item1" );
            _nodeCollection.Add( "?Item2", node );
            node.Expanded = true;

            VerifyItems( _nodeCollection.VisibleItems.GetEnumerator() /* empty list */ );
        }

        [Test] public void FilterUpdateItem()
        {
            MockFilter mockFilter = new MockFilter();
            _filters.Add( mockFilter );

            MockComparable cmp = new MockComparable( "?Item2" );
            cmp.SimpleToString = true;
            _nodeCollection.Add( cmp );
            VerifyItems( _nodeCollection.VisibleItems.GetEnumerator(), cmp );

            cmp.Value = "!Item1";
            _nodeCollection.Update( cmp );
            VerifyItems( _nodeCollection.VisibleItems.GetEnumerator() /* empty list */ );
        }

        [Test] public void NodeLevel()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node2 = _nodeCollection.Add( "Item11", node );

            Assert.AreEqual( 0, node.Level );
            Assert.AreEqual( 1, node2.Level );
        }

        [Test] public void VirtualChildren()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            node.HasChildren = true;
            Assert.AreEqual( CollapseState.Collapsed, node.CollapseState );
        }

        [Test] public void EnumerateChildren()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item2" );
            JetListViewNode childNode = _nodeCollection.Add( "Item11", node );
            JetListViewNode subChildNode = _nodeCollection.Add( "Item111", childNode );
            childNode.Expanded = true;

            IEnumerator enumerator = node.EnumerateChildrenRecursive();
            VerifyItems( enumerator, "Item11", "Item111" );
        }

        [Test] public void EnumerateSkipCollapsed()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item11", node );
            _nodeCollection.Add( "Item2" );

            IEnumerator enumerator = _nodeCollection.EnumerateVisibleNodesForward( node );
            VerifyItems( enumerator, "Item1", "Item2" );
        }

        [Test] public void ChangeParentOnRemoveChild()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item2", node );

            _nodeCollection.NodeChanged += new JetListViewNodeEventHandler( HandleNodeEvent );
            _nodeCollection.Remove( "Item2", node );

            Assert.AreEqual( 1, _eventNodes.Count );
        }

        [Test] public void EqualNodesInComparer()
        {
            _nodeCollection.SetItemComparer( null, new Comparer( CultureInfo.CurrentCulture ) );
            _nodeCollection.Add( "Item" );
            _nodeCollection.Add( "Item" );
        }

        [Test] public void EqualNodesInComparerUpdate()
        {
            _nodeCollection.SetItemComparer( null, new Comparer( CultureInfo.CurrentCulture ) );
            MockComparable cmp1 = new MockComparable( "Best" );
            MockComparable cmp2 = new MockComparable( "Rest" );
            MockComparable cmp3 = new MockComparable( "Test" );

            _nodeCollection.Add( cmp1 );
            _nodeCollection.Add( cmp2 );
            _nodeCollection.Add( cmp3 );

            cmp3.Value = "Best";
            _nodeCollection.Update( cmp3 );   // OM-7898
            Assert.AreEqual( cmp2, _nodeCollection.Nodes [2].Data );
        }

        [Test] public void MultiMap()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );
            string childNode = "Child";
            _nodeCollection.Add( childNode, node );
            JetListViewNode child2 = _nodeCollection.Add( childNode, node2 );

            _nodeCollection.Remove( childNode, node );
            Assert.AreEqual( child2, _nodeCollection.NodeFromItem( childNode ) );
        }

        [Test] public void UpdateMultiMap()
        {
            _nodeCollection.SetItemComparer( null, new Comparer( CultureInfo.CurrentCulture ) );
            MockComparable cmp1 = new MockComparable( "Item1" );
            MockComparable cmp2 = new MockComparable( "Item2" );
            JetListViewNode node1 = _nodeCollection.Add( cmp1 );
            JetListViewNode node2 = _nodeCollection.Add( cmp2 );

            _nodeCollection.Add( new MockComparable( "Item3" ), node1 );
            _nodeCollection.Add( new MockComparable( "Item4" ), node2 );

            MockComparable theComparable = new MockComparable( "Item5" );
            _nodeCollection.Add( theComparable, node1 );
            _nodeCollection.Add( theComparable, node2 );

            theComparable.Value = "Item0";
            _nodeCollection.Update( theComparable );
            Assert.AreEqual( theComparable, _nodeCollection.Nodes [0].Nodes [0].Data );
            Assert.AreEqual( theComparable, _nodeCollection.Nodes [1].Nodes [0].Data );
        }

        [Test] public void Clear()
        {
            _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item2" );

            _nodeCollection.Root.Nodes.Clear();
            Assert.AreEqual( 0, _nodeCollection.Root.Nodes.Count );
            Assert.IsNull( _nodeCollection.NodeFromItem( "Item1" ) );
        }

        [Test] public void ClearPart()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode child1 = node.Nodes.Add( "Item11" );
            JetListViewNode child2 = child1.Nodes.Add( "Item111" );

            _nodeCollection.NodeRemoved += new JetListViewNodeEventHandler( HandleNodeEvent );
            node.Nodes.Clear();

            Assert.IsTrue( _eventNodes.Contains( child1 ) );
            Assert.IsTrue( _eventNodes.Contains( child2 ) );
        }

        [Test] public void RequestChildrenWhenEnumerating()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            node.HasChildren = true;
            _nodeCollection.ChildrenRequested += new RequestChildrenEventHandler( HandleChildrenRequested );

            IEnumerator enumerator = _nodeCollection.EnumerateNodesForward( node );
            VerifyItems( enumerator, "Item1" );
            Assert.AreEqual( 1, _eventNodes.Count );
        }

        [Test] public void InvisibleNodeAdded()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node2 = _nodeCollection.Add( "Item2", node );
            node.Expanded = false;

            _nodeCollection.VisibleNodeAdded += new JetListViewNodeEventHandler( HandleNodeEvent );
            _nodeCollection.Add( "Item3", node );
            Assert.AreEqual( 0, _eventNodes.Count );
        }

        [Test] public void ChangeNodeParent()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node11 = _nodeCollection.Add( "Item11", node );
            JetListViewNode node111 = _nodeCollection.Add( "Item111", node11 );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );

            node.Expanded = true;
            node11.Expanded = true;

            node11.SetParent( node2 );

        }

        [Test] public void BatchUpdate()
        {
            _nodeCollection.VisibleNodeAdded += new JetListViewNodeEventHandler( HandleNodeEvent );
            _nodeCollection.MultipleNodesChanged += new MultipleNodesChangedEventHandler( HandleMultipleNodesChanged );
            _nodeCollection.BeginUpdate();
            _nodeCollection.Add( "Item1" );
            _nodeCollection.EndUpdate();

            Assert.AreEqual( 0, _eventNodes.Count );
            Assert.AreEqual( 1, _eventCount );
        }

	    [Test] public void MultipleSortUpdate()   // OM-9953
        {
            MockComparable[] cmp = new MockComparable [5];
            for( int i=0; i<cmp.Length; i++ )
            {
                cmp [i] = new MockComparable( "Item" );
                _nodeCollection.Add( cmp [i] );
            }

            _nodeCollection.SetItemComparer( null, new Comparer( CultureInfo.CurrentCulture ) );

            cmp [1].Value = "Jtem";
            cmp [2].Value = "Jtem";
            cmp [3].Value = "Jtem";

            _nodeCollection.Update( cmp [1] );
            _nodeCollection.Update( cmp [2] );
            _nodeCollection.Update( cmp [3] );

            Assert.AreEqual( "Item", (_nodeCollection.Nodes [0].Data as MockComparable).Value );
            Assert.AreEqual( "Item", (_nodeCollection.Nodes [1].Data as MockComparable).Value  );
            Assert.AreEqual( "Jtem", (_nodeCollection.Nodes [2].Data as MockComparable).Value );
            Assert.AreEqual( "Jtem", (_nodeCollection.Nodes [3].Data as MockComparable).Value );
            Assert.AreEqual( "Jtem", (_nodeCollection.Nodes [4].Data as MockComparable).Value );
        }

	    private void HandleNodeEvent( object sender, JetListViewNodeEventArgs e )
	    {
	        _eventNodes.Add( e.Node );
	    }

	    private void HandleMultipleNodesChanged( object sender, MultipleNodesChangedEventArgs e )
        {
            _eventCount++;
        }

        private void HandleChildrenRequested( object sender, RequestChildrenEventArgs e )
	    {
            _eventNodes.Add( e.Node );
	    }
	}

    internal class MockComparable: IComparable
    {
        private string _value;
        private bool _simpleToString = false;

        public MockComparable( string value )
        {
            _value = value;
        }

        public bool SimpleToString
        {
            get { return _simpleToString; }
            set { _simpleToString = value; }
        }

        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public int CompareTo( object obj )
        {
            return _value.CompareTo( ((MockComparable) obj)._value );
        }

        public override string ToString()
        {
            if ( _simpleToString )
            {
                return _value;
            }
            return "MockComparable:" + _value;
        }
    }

    internal class MockFilter: IJetListViewNodeFilter
    {
        private string _filterString = "!";

        public event EventHandler FilterChanged;

        internal void SetFilterString( string s )
        {
            _filterString = s;
            if ( FilterChanged != null )
            {
                FilterChanged( this, EventArgs.Empty );
            }
        }

        public bool AcceptNode( JetListViewNode node )
        {
            return !node.Data.ToString().StartsWith( _filterString );
        }
    }
}
