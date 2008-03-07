/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Windows.Forms;
using JetBrains.DataStructures;
using NUnit.Framework;

namespace JetBrains.JetListViewLibrary.Tests
{
	[TestFixture]
    public class SelectionModelTests
	{
        private JetListViewNodeCollection _nodeCollection;
        private JetListViewFilterCollection _filters;
        private SelectionModel _model;
        private ArrayList _selectionStateChanges;
	    private int _activeNodeChanges;

	    [SetUp] public void SetUp()
        {
            _filters = new JetListViewFilterCollection();
            _nodeCollection = new JetListViewNodeCollection( _filters );
            _model = new MultipleSelectionModel( _nodeCollection );
            _selectionStateChanges = new ArrayList();
            _activeNodeChanges = 0;
        }

        private void AddNodes( int count )
        {
            for( int i=1; i<=count; i++ )
            {
                _nodeCollection.Add( "Item" + i );
            }
        }

        [Test] public void MouseSelect()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1", null );
            Assert.IsFalse( _model.IsNodeSelected( item ) );
            _model.HandleMouseDown( item, Keys.None );
            Assert.IsTrue( _model.IsNodeSelected( item ) );
            Assert.IsTrue( _model.IsNodeFocused( item ) );
        }

        [Test] public void SelectionChangeEvent()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1", null );
            _model.SelectionStateChanged += new ViewNodeStateChangeEventHandler( OnSelectionStateChanged );

            _model.HandleMouseDown( item, Keys.None );
            Assert.AreEqual( 1, _selectionStateChanges.Count );
            ViewNodeStateChangeEventArgs e = (ViewNodeStateChangeEventArgs) _selectionStateChanges [0];
            Assert.AreEqual( item, e.ViewNode );
            Assert.IsTrue( e.State );
        }

        [Test] public void NoSelectionChangeOnClickSelected()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1", null );
            _model.HandleMouseDown( item, Keys.None );
            _model.SelectionStateChanged += new ViewNodeStateChangeEventHandler( OnSelectionStateChanged );
            _model.HandleMouseDown( item, Keys.None );
            Assert.AreEqual( 0, _selectionStateChanges.Count );
        }

        [Test] public void MouseCtrlSelect()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );
            _model.HandleMouseDown( item, Keys.None );
            _model.HandleMouseDown( item2, Keys.Control );
            _model.HandleMouseUp( item2, Keys.Control );

            Assert.IsTrue( _model.IsNodeSelected( item ) );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            Assert.IsFalse( _model.IsNodeFocused( item ) );
            Assert.IsTrue( _model.IsNodeFocused( item2 ) );

            _model.HandleMouseDown( item2, Keys.Control );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            _model.HandleMouseUp( item2, Keys.Control );
            Assert.IsFalse( _model.IsNodeSelected( item2 ) );
        }

        [Test] public void MouseShiftSelectForward()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );
            JetListViewNode item3 = _nodeCollection.Add( "Item3" );
            
            _model.HandleMouseDown( item, Keys.None );
            _model.HandleMouseDown( item3, Keys.Shift );

            Assert.IsTrue( _model.IsNodeSelected( item ) );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            Assert.IsTrue( _model.IsNodeSelected( item3 ) );
            Assert.IsTrue( _model.IsNodeFocused( item3 ) );
        }

        [Test] public void MouseShiftUnselectForward()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );
            JetListViewNode item3 = _nodeCollection.Add( "Item3" );
            
            _model.HandleMouseDown( item, Keys.None );
            _model.HandleMouseDown( item3, Keys.Shift );
            _model.HandleMouseDown( item2, Keys.Shift );

            Assert.IsTrue( _model.IsNodeSelected( item ) );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            Assert.IsFalse( _model.IsNodeSelected( item3 ) );
        }

        [Test] public void MouseShiftSelectBackward()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );
            JetListViewNode item3 = _nodeCollection.Add( "Item3" );
            
            _model.HandleMouseDown( item3, Keys.None );
            _model.HandleMouseDown( item, Keys.Shift );

            Assert.IsTrue( _model.IsNodeSelected( item ) );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            Assert.IsTrue( _model.IsNodeSelected( item3 ) );
        }

        [Test] public void MouseCtrlShiftSelect()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );
            JetListViewNode item3 = _nodeCollection.Add( "Item3" );
            JetListViewNode item4 = _nodeCollection.Add( "Item4" );

            _model.HandleMouseDown( item, Keys.None );
            _model.HandleMouseDown( item2, Keys.Control );
            _model.HandleMouseDown( item4, Keys.Control | Keys.Shift );

            Assert.IsTrue( _model.IsNodeSelected( item ) );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            Assert.IsTrue( _model.IsNodeSelected( item3 ) );
            Assert.IsTrue( _model.IsNodeSelected( item4 ) );
        }

        [Test] public void FocusChangeEvent()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );
            
            _model.HandleMouseDown( item, Keys.None );
            
            _model.FocusStateChanged += new ViewNodeStateChangeEventHandler( OnSelectionStateChanged );

            _model.HandleMouseDown( item2, Keys.Control );
            Assert.AreEqual( 2, _selectionStateChanges.Count );
            ViewNodeStateChangeEventArgs[] args = (ViewNodeStateChangeEventArgs[]) _selectionStateChanges.ToArray( typeof (ViewNodeStateChangeEventArgs) );
            Assert.IsTrue( args [0].ViewNode.Equals( item ) || args [1].ViewNode.Equals( item ) );
            Assert.IsTrue( args [0].ViewNode.Equals( item2 ) || args [1].ViewNode.Equals( item2 ) );
        }

        [Test] public void ProcessDownKey()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );

            _model.HandleMouseDown( item, Keys.None );
            _model.HandleKeyDown( Keys.Down );
            Assert.IsFalse( _model.IsNodeSelected( item ) );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            Assert.IsTrue( _model.IsNodeFocused( item2 ) );
        }

        [Test] public void DownKeyWithNoSelection()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item2" );

            _model.HandleKeyDown( Keys.Down );

            Assert.IsTrue( _model.IsNodeSelected( item ) );
            Assert.IsTrue( _model.IsNodeFocused( item ) );
        }
        
        [Test] public void ClickWithNoSelection()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1" );
            _model.HandleMouseDown( null, Keys.None );
            Assert.IsFalse( _model.IsNodeFocused( item ) );
        }

        [Test] public void DownKeyOnLastItem()
        {
            _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );

            _model.HandleMouseDown( item2, Keys.None );
            _model.HandleKeyDown( Keys.Down );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            Assert.IsTrue( _model.IsNodeFocused( item2 ) );
        }

        [Test] public void ProcessUpKey()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );

            _model.HandleMouseDown( item2, Keys.None );
            _model.HandleKeyDown( Keys.Up );
            Assert.IsTrue( _model.IsNodeSelected( item1 ) );
            Assert.IsTrue( _model.IsNodeFocused( item1 ) );
        }

        [Test] public void ProcessPgDnKey()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item2" );
            JetListViewNode item3 = _nodeCollection.Add( "Item2" );

            _model.PagingProvider = new MockPagingProvider( item3 );

            _model.HandleMouseDown( item1, Keys.None );
            _model.HandleKeyDown( Keys.PageDown );
            Assert.IsTrue( _model.IsNodeSelected( item3 ) );
            Assert.IsTrue( _model.IsNodeFocused( item3 ) );
        }

        [Test] public void NoSelectionChangeOnKeyUp()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1" );
            _model.HandleKeyDown( Keys.Down );
            _model.SelectionStateChanged += new ViewNodeStateChangeEventHandler( OnSelectionStateChanged );
            _model.HandleKeyDown( Keys.Down );
            Assert.AreEqual( 0, _selectionStateChanges.Count );
        }

        [Test] public void ProcessHomeKey()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item2" );
            JetListViewNode item3 = _nodeCollection.Add( "Item2" );

            _model.HandleMouseDown( item3, Keys.None );
            _model.HandleKeyDown( Keys.Home );
            Assert.IsTrue( _model.IsNodeSelected( item1 ) );
            Assert.IsTrue( _model.IsNodeFocused( item1 ) );
        }

        [Test] public void ProcessHomeKeyWithNoSelection()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item2" );

            _model.HandleKeyDown( Keys.Home );
            Assert.IsTrue( _model.IsNodeSelected( item1 ) );
            Assert.IsTrue( _model.IsNodeFocused( item1 ) );
        }

        [Test] public void ProcessEndKey()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item2" );
            JetListViewNode item3 = _nodeCollection.Add( "Item2" );

            _model.HandleMouseDown( item1, Keys.None );
            _model.HandleKeyDown( Keys.End );
            Assert.IsTrue( _model.IsNodeSelected( item3 ) );
            Assert.IsTrue( _model.IsNodeFocused( item3 ) );
        }

        [Test] public void ProcessEndKeyWithNoSelection()
        {
            _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );

            _model.HandleKeyDown( Keys.End );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            Assert.IsTrue( _model.IsNodeFocused( item2 ) );
        }
        
        [Test] public void ProcessShiftDownKey()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );

            _model.HandleMouseDown( item1, Keys.None );
            _model.HandleKeyDown( Keys.Shift | Keys.Down );
            Assert.IsTrue( _model.IsNodeSelected( item1 ) );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            Assert.IsTrue( _model.IsNodeFocused( item2 ) );
        }

        [Test] public void ShiftUpDeselect()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );

            _model.HandleMouseDown( item1, Keys.None );
            _model.HandleMouseDown( item2, Keys.Shift );
            _model.HandleKeyDown( Keys.Shift | Keys.Up );

            Assert.IsTrue( _model.IsNodeSelected( item1 ) );
            Assert.IsFalse( _model.IsNodeSelected( item2 ) );
            Assert.IsTrue( _model.IsNodeFocused( item1 ) );
        }

        [Test] public void ShiftPgDn()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );
            JetListViewNode item3 = _nodeCollection.Add( "Item3" );

            _model.PagingProvider = new MockPagingProvider( item3 );
            _model.HandleMouseDown( item1, Keys.None );
            _model.HandleKeyDown( Keys.Shift | Keys.PageDown );

            Assert.IsTrue( _model.IsNodeSelected( item1 ) );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            Assert.IsTrue( _model.IsNodeSelected( item3 ) );
            Assert.IsTrue( _model.IsNodeFocused( item3 ) );
        }

        [Test] public void KeyMoveSelectionStart()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1" );
            JetListViewNode item2 = _nodeCollection.Add( "Item2" );
            JetListViewNode item3 = _nodeCollection.Add( "Item3" );

            _model.PagingProvider = new MockPagingProvider( item3 );
            _model.HandleMouseDown( item1, Keys.None );
            _model.HandleKeyDown( Keys.Down );
            _model.HandleKeyDown( Keys.Shift | Keys.PageDown );

            Assert.IsFalse( _model.IsNodeSelected( item1 ) );
            Assert.IsTrue( _model.IsNodeSelected( item2 ) );
            Assert.IsTrue( _model.IsNodeSelected( item3 ) );
            Assert.IsTrue( _model.IsNodeFocused( item3 ) );
        }

        [Test] public void ShiftEnd()
        {
            AddNodes( 3 );

            _model.HandleMouseDown( _nodeCollection.Nodes [0], Keys.None );
            _model.HandleKeyDown( Keys.Shift | Keys.End );
            for( int i=0; i<3; i++ )
            {
                Assert.IsTrue( _model.IsNodeSelected( _nodeCollection.Nodes [i] ) );
            }
        }

        [Test] public void CtrlDown()
        {
            AddNodes( 3 );

            _model.HandleMouseDown( _nodeCollection.Nodes [0], Keys.None );
            _model.HandleKeyDown( Keys.Control | Keys.Down );
            Assert.IsFalse( _model.IsNodeSelected( _nodeCollection.Nodes [1] ) );
            Assert.IsTrue( _model.IsNodeFocused( _nodeCollection.Nodes [1] ) );
        }

        [Test] public void EnumerateSelectedItems()
        {
            AddNodes( 4 );
            _model.HandleMouseDown( _nodeCollection.Nodes [1], Keys.None );
            _model.HandleKeyDown( Keys.Shift | Keys.End );

            HashSet selItems = new HashSet();
            foreach( JetListViewNode node in _model.SelectedNodes )
            {
                selItems.Add( node );
            }

            Assert.AreEqual( 3, selItems.Count );

            for( int i=1; i<4; i++ )
            {
                Assert.IsTrue( _model.IsNodeSelected( _nodeCollection.Nodes [i] ) );
            }
        }

        [Test] public void ClearSelection()
        {
            JetListViewNode item1 = _nodeCollection.Add( "Item1" );
            _model.HandleMouseDown( item1, Keys.None );

            _model.SelectionStateChanged += new ViewNodeStateChangeEventHandler( OnSelectionStateChanged );
            _model.Clear();

            Assert.IsFalse( _model.IsNodeSelected( item1 ) );
            Assert.IsFalse( _model.IsNodeFocused( item1 ) );
            Assert.AreEqual( 1, _selectionStateChanges.Count );
        }

        [Test] public void SpaceSelect()
        {
            JetListViewNode item = _nodeCollection.Add( "Item1" );
            _model.HandleMouseDown( item, Keys.None );

            _model.HandleKeyDown( Keys.Space );
            Assert.IsFalse( _model.IsNodeSelected( item ) );
            Assert.IsTrue( _model.IsNodeFocused( item ) );

            _model.HandleKeyDown( Keys.Space );
            Assert.IsTrue( _model.IsNodeSelected( item ) );
        }

        [Test] public void PublicApi()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            Assert.IsFalse( _model.Contains( "Item1" ) ); 
            _model.Add( "Item1" );
            Assert.AreEqual( 1, _model.Count );
            Assert.IsTrue( _model.Contains( "Item1" ) );
            Assert.IsTrue( _model.IsNodeFocused( node ) );
            _model.Remove( "Item1" );
            Assert.AreEqual( 0, _model.Count );
            Assert.IsFalse( _model.Contains( "Item1" ) );
            Assert.IsFalse( _model.IsNodeFocused( node ) );
        }

        [Test] public void AdjustSelectionOnRemove()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );

            _model.Add( "Item1" );

            _nodeCollection.Remove( "Item1", null );

            Assert.IsFalse( _model.IsNodeSelected( node ) );
            Assert.IsTrue( _model.IsNodeSelected( node2 ) );
        }

        [Test] public void MoveSelectionUpOnRemove()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );

            _model.Add( "Item2" );

            _nodeCollection.Remove( "Item2", null );

            Assert.IsFalse( _model.IsNodeSelected( node2 ) );
            Assert.IsTrue( _model.IsNodeSelected( node ) );
        }

        [Test] public void MoveSelectionToParentOnRemove()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node11 = _nodeCollection.Add( "Item11", node );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );

            _model.Add( "Item11" );

            _nodeCollection.Remove( "Item11", node );

            Assert.IsTrue( _model.IsNodeSelected( node ) );
        }

        [Test] public void MoveSelectionToSameLevelOnRemove()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node11 = _nodeCollection.Add( "Item11", node );
            JetListViewNode node12 = _nodeCollection.Add( "Item12", node );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );

            _model.Add( "Item12" );

            _nodeCollection.Remove( "Item12", node );

            Assert.IsTrue( _model.IsNodeSelected( node11 ) );
        }

        [Test] public void ActiveItem()
        {
            JetListViewNode node1 = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item2" );
            JetListViewNode node3 = _nodeCollection.Add( "Item3" );

            _model.HandleKeyDown( Keys.Down );
            Assert.AreEqual( node1, _model.ActiveNode );
            _model.HandleKeyDown( Keys.Control | Keys.Down );
            Assert.AreEqual( node1, _model.ActiveNode );
            _model.HandleMouseDown( node3, Keys.None );
            Assert.AreEqual( node3, _model.ActiveNode );
        }

        [Test] public void ClearSelectionOnFilter()
        {
            JetListViewNode node1 = _nodeCollection.Add( "!Item1" );
            JetListViewNode node2 = _nodeCollection.Add( "!Item2" );
            JetListViewNode node3 = _nodeCollection.Add( "Item3" );

            _model.HandleMouseDown( node1, Keys.None ); 
            _model.HandleMouseDown( node2, Keys.Shift ); 

            _filters.Add( new MockFilter() );

            Assert.AreEqual( 1, _model.Count );
            Assert.IsTrue( _model.Contains( "Item3" ) );
        }

        [Test] public void KeyDownCollapsed()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item11", node );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );

            _model.HandleMouseDown( node, Keys.None );
            _model.HandleKeyDown( Keys.Down );

            Assert.IsTrue( _model.IsNodeSelected( node2 ) );
        }

        [Test] public void PullUpSelectionOnCollapse()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node2 = _nodeCollection.Add( "Item11", node );
            JetListViewNode node3 = _nodeCollection.Add( "Item111", node2 );
            
            node.Expanded = true;
            node2.Expanded = true;

            _model.HandleMouseDown( node3, Keys.Down );

            node.Expanded = false;
            Assert.IsFalse( _model.IsNodeSelected( node3 ) );
            Assert.IsTrue( _model.IsNodeSelected( node ) );
            Assert.IsTrue( _model.IsNodeFocused( node ) );
        }

        [Test] public void ClearSelectionOnMouseUp()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );
            JetListViewNode node3 = _nodeCollection.Add( "Item3" );

            _model.HandleMouseDown( node, Keys.None );
            _model.HandleMouseUp( node, Keys.None );
            _model.HandleMouseDown( node3, Keys.Shift );
            _model.HandleMouseUp( node3, Keys.Shift );

            _model.HandleMouseDown( node, Keys.None );
            Assert.IsTrue( _model.IsNodeSelected( node3 ) );
            _model.HandleMouseUp( node, Keys.None );
            Assert.IsFalse( _model.IsNodeSelected( node3 ) );
        }

        [Test] public void MultiMapContains()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );

            string child = "Child";
            _nodeCollection.Add( child, node );
            JetListViewNode child2 = _nodeCollection.Add( child, node2 );

            _model.HandleMouseDown( child2, Keys.None );
            Assert.IsTrue( _model.Contains( child ) );
        }

        [Test] public void MultiMapRemove()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );

            string child = "Child";
            _nodeCollection.Add( child, node );
            JetListViewNode child2 = _nodeCollection.Add( child, node2 );

            _model.HandleMouseDown( child2, Keys.None );
            _model.Remove( child );
            Assert.IsFalse( _model.IsNodeSelected( child2 ) );
        }

        [Test] public void ClearSelectionOnClearTree()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            _model.Add( "Item1" );
            _nodeCollection.Nodes.Clear();

            Assert.AreEqual( 0, _model.Count );
            Assert.IsNull( _model.ActiveNode, "ActiveNode must be cleared" );
            Assert.IsNull( _model.FocusNode, "FocusNode must be cleared" );
        }

        [Test] public void SelectAll()
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            _nodeCollection.Add( "Item11", node );
            _nodeCollection.Add( "Item2" );

            _model.SelectAll();

            Assert.IsTrue( _model.Contains( "Item1" ) );
            Assert.IsTrue( _model.Contains( "Item11" ) );
            Assert.IsTrue( _model.Contains( "Item2" ) );
        }

        [Test] public void ActiveNodeChange()  // OM-8687
        {
            JetListViewNode node = _nodeCollection.Add( "Item1" );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );

            _model.HandleMouseDown( node, Keys.None );
            _model.ActiveNodeChanged += new ViewNodeEventHandler( HandleActiveNodeChanged );
            _model.HandleMouseDown( node2, Keys.None );
            Assert.AreEqual( 1, _activeNodeChanges );
        }

        [Test] public void ActiveNodeChangeOnEmpty()  // OM-8518
        {
            JetListViewNode node = _nodeCollection.Add( "Item" );
            _model.HandleMouseDown( node, Keys.None );
            _model.ActiveNodeChanged += new ViewNodeEventHandler( HandleActiveNodeChanged );
            _nodeCollection.Remove( "Item", null );
            Assert.AreEqual( 1, _activeNodeChanges );
            Assert.IsNull( _model.ActiveNode );
        }

        [Test] public void DeleteSelectionStartNode()
        {
            JetListViewNode node = _nodeCollection.Add( "Item" );
            JetListViewNode node2 = _nodeCollection.Add( "Item2" );
            JetListViewNode node3 = _nodeCollection.Add( "Item3" );

            _model.HandleMouseDown( node, Keys.None );
            _model.HandleMouseDown( node2, Keys.Shift );
            _nodeCollection.Remove( "Item", null );
            _model.HandleMouseDown( node3, Keys.Shift );
            Assert.IsTrue( _model.IsNodeSelected( node3 ) );           
        }

	    private class MockPagingProvider : IPagingProvider
	    {
	        private JetListViewNode _destNode;

	        public MockPagingProvider( JetListViewNode node )
	        {
	            _destNode = node;
	        }

	        public IViewNode MoveByPage( IViewNode startNode, MoveDirection direction )
	        {
	            return _destNode;
	        }
	    }

	    private void OnSelectionStateChanged( object sender, ViewNodeStateChangeEventArgs e )
	    {
            _selectionStateChanges.Add( e ); 
	    }

	    private void HandleActiveNodeChanged( object sender, ViewNodeEventArgs e )
	    {
            _activeNodeChanges++;
	    }
	}
}
    