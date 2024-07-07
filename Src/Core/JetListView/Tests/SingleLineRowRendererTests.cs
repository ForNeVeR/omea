// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;

namespace JetBrains.JetListViewLibrary.Tests
{
	[TestFixture]
    public class SingleLineRowRendererTests
	{
        private JetListViewNodeCollection _nodeCollection;
        private JetListViewColumnCollection _columnCollection;
        private SingleLineRowRenderer _rowRenderer;
        private int _scrollRangeChanges;
        private MockControlPainter _controlPainter;

        [SetUp] public void SetUp()
        {
            _nodeCollection = new JetListViewNodeCollection( null );
            _columnCollection = new JetListViewColumnCollection();
            _rowRenderer = new SingleLineRowRenderer( _columnCollection, _nodeCollection );
            _rowRenderer.MinColumnWidth = 5;
            _controlPainter = new MockControlPainter();
            _columnCollection.ControlPainter = _controlPainter;
            _scrollRangeChanges = 0;
        }

        private void SetupSizeToContentColumn()
        {
            JetListViewColumn col = new JetListViewColumn();
            col.Width = 0;
            col.LeftMargin = col.RightMargin = 0;
            col.SizeToContent = true;
            _columnCollection.Add( col );
        }

        private void SetupTreeStructureColumn()
        {
            TreeStructureColumn treeStructureCol = new TreeStructureColumn();
            treeStructureCol.Width = 20;
            _columnCollection.Add( treeStructureCol );
        }

        [Test] public void ScrollRangeChanged()
        {
            SetupSizeToContentColumn();
            _controlPainter.SetExpectedSize( "Test", new Size( 10, 10 ) );
            _rowRenderer.ScrollRangeChanged += new EventHandler( HandleScrollRangeChanged );
            _nodeCollection.Add( "Test" );
            Assert.AreEqual( 1, _scrollRangeChanges );
            Assert.AreEqual( 10, _rowRenderer.ScrollRange );
        }

	    [Test] public void SizeToContentAfterAdd()
        {
            SetupSizeToContentColumn();
            _controlPainter.SetExpectedSize( "Test", new Size( 10, 10 ) );
            _rowRenderer.ScrollRangeChanged += new EventHandler( HandleScrollRangeChanged );
            _nodeCollection.Add( "Test" );
            Assert.AreEqual( 1, _scrollRangeChanges );
        }

        [Test] public void NoShrinkWhenAddingShorterItem()
        {
            SetupSizeToContentColumn();
            _controlPainter.SetExpectedSize( "LongTest", new Size( 20, 10 ) );
            _controlPainter.SetExpectedSize( "Test", new Size( 10, 10 ) );
            _rowRenderer.ScrollRangeChanged += new EventHandler( HandleScrollRangeChanged );
            _nodeCollection.Add( "LongTest" );
            _nodeCollection.Add( "Test" );
            Assert.AreEqual( 1, _scrollRangeChanges );
            Assert.AreEqual( 20, _rowRenderer.ScrollRange );
        }

        [Test] public void SizeToContentAfterRemove()
        {
            SetupSizeToContentColumn();
            _controlPainter.SetExpectedSize( "LongTest", new Size( 20, 10 ) );
            _controlPainter.SetExpectedSize( "Test", new Size( 10, 10 ) );
            _nodeCollection.Add( "LongTest" );
            _nodeCollection.Add( "Test" );

            _rowRenderer.ScrollRangeChanged += new EventHandler( HandleScrollRangeChanged );
            _nodeCollection.Remove( "LongTest", null );
            Assert.AreEqual( 1, _scrollRangeChanges );
            Assert.AreEqual( 10, _rowRenderer.ScrollRange );
        }

        [Test] public void ScrollOffsetTest()
        {
            MockJetListViewColumn col = new MockJetListViewColumn();
            col.Width = 20;
            _columnCollection.Add( col );
            JetListViewNode node = _nodeCollection.Add( "Test" );

            _rowRenderer.ScrollOffset = 10;
            _rowRenderer.DrawRow( null, new Rectangle( 0, 0, 20, 20 ), node, RowState.None );
            Assert.AreEqual( new Rectangle( -10, 0, 20, 20 ), col.LastDrawItemRect );
        }

        [Test] public void MouseDownTest()
        {
            MockJetListViewColumn col = new MockJetListViewColumn();
            col.Width = 20;
            _columnCollection.Add( col );

            MockJetListViewColumn col2 = new MockJetListViewColumn();
            col2.Width = 20;
            _columnCollection.Add( col2 );

            JetListViewNode node = _nodeCollection.Add( "Test" );

            _rowRenderer.HandleMouseDown( node, 30, 5, MouseButtons.Left, Keys.None );
            Assert.AreEqual( new Point( 10, 5 ), col2.LastMouseDownPoint );
        }

        [Test] public void DrawIndented()
        {
            SetupTreeStructureColumn();
            MockJetListViewColumn col = new MockJetListViewColumn();
            col.Width = 100;
            _columnCollection.Add( col );

            JetListViewNode node = _nodeCollection.Add( "Test" );
            JetListViewNode child = _nodeCollection.Add( "Child", node );

            _rowRenderer.DrawRow( null, new Rectangle( 0, 0, 100, 20 ), node, RowState.None );
            Assert.AreEqual( new Rectangle( 20, 0, 80, 20 ), col.LastDrawItemRect );

            _rowRenderer.DrawRow( null, new Rectangle( 0, 0, 100, 20 ), child, RowState.None );
            Assert.AreEqual( new Rectangle( 40, 0, 60, 20 ), col.LastDrawItemRect );
        }

        [Test] public void DrawIndentedMultiColumn()
        {
            SetupTreeStructureColumn();
            MockJetListViewColumn col = new MockJetListViewColumn();
            col.Width = 100;
            _columnCollection.Add( col );

            MockJetListViewColumn col2 = new MockJetListViewColumn();
            col2.Width = 100;
            _columnCollection.Add( col2 );

            JetListViewNode node = _nodeCollection.Add( "Test" );
            JetListViewNode child = _nodeCollection.Add( "Child", node );

            _rowRenderer.DrawRow( null, new Rectangle( 0, 0, 200, 20 ), node, RowState.None );
            Assert.AreEqual( new Rectangle( 20, 0, 80, 20 ), col.LastDrawItemRect );
            Assert.AreEqual( new Rectangle( 100, 0, 100, 20 ), col2.LastDrawItemRect );
        }

	    [Test] public void DesiredWidthIndented()
        {
            SetupTreeStructureColumn();
            SetupSizeToContentColumn();

            _controlPainter.SetExpectedSize( "Test", new Size( 10, 10 ) );
	        JetListViewNode node = _nodeCollection.Add( "Test" );
            _nodeCollection.Add( "Test", node );
            node.Expanded = true;
            _rowRenderer.SizeColumnsToContent( null, null, null );
            Assert.AreEqual( 50, _rowRenderer.ScrollRange );
        }

        [Test] public void DesiredWidth_NodeAdded()
        {
            SetupTreeStructureColumn();
            SetupSizeToContentColumn();

            _controlPainter.SetExpectedSize( "Test", new Size( 10, 10 ) );
            JetListViewNode node = _nodeCollection.Add( "Test" );
            JetListViewNode child = _nodeCollection.Add( "Test", node );
            node.Expanded = true;
            _rowRenderer.ProcessNodeAdded( child );
            Assert.AreEqual( 50, _rowRenderer.ScrollRange );
        }

        [Test] public void DesiredWidth_NodeExpanded()
        {
            SetupTreeStructureColumn();
            SetupSizeToContentColumn();

            _controlPainter.SetExpectedSize( "Test", new Size( 10, 10 ) );
            JetListViewNode node = _nodeCollection.Add( "Test" );
            JetListViewNode child = _nodeCollection.Add( "Test", node );
            node.Expanded = true;
            _rowRenderer.ProcessNodeExpanded( node );
            Assert.AreEqual( 50, _rowRenderer.ScrollRange );
        }

        [Test] public void DesiredWidth_NodeCollapsed()
        {
            SetupTreeStructureColumn();
            SetupSizeToContentColumn();

            _controlPainter.SetExpectedSize( "Test", new Size( 10, 10 ) );
            JetListViewNode node = _nodeCollection.Add( "Test" );
            JetListViewNode child = _nodeCollection.Add( "Test", node );
            node.Expanded = true;
            _rowRenderer.ProcessNodeExpanded( node );
            node.Expanded = false;
            _rowRenderer.ProcessNodeCollapsed( node );
            Assert.AreEqual( 30, _rowRenderer.ScrollRange );
        }

        [Test] public void DesiredWidthMulticolumn()
        {
            SetupTreeStructureColumn();
            SetupSizeToContentColumn();
            SetupSizeToContentColumn();

            _controlPainter.SetExpectedSize( "Test", new Size( 10, 10 ) );
            _nodeCollection.Add( "Test" );
            _rowRenderer.SizeColumnsToContent( null, null, null );
            Assert.AreEqual( 40, _rowRenderer.ScrollRange );  // 20 (tree structure) + 2*10 (size to content)
        }

        [Test] public void MouseDownIndented()
        {
            SetupTreeStructureColumn();
            MockJetListViewColumn col = new MockJetListViewColumn();
            col.Width = 100;
            _columnCollection.Add( col );

            JetListViewNode node = _nodeCollection.Add( "Test" );
            JetListViewNode child = _nodeCollection.Add( "Child", node );

            _rowRenderer.HandleMouseDown( child, 30, 10, MouseButtons.Left, Keys.None );
            Assert.AreEqual( new Point( -1, -1 ), col.LastMouseDownPoint );
            _rowRenderer.HandleMouseDown( node, 30, 10, MouseButtons.Left, Keys.None );
            Assert.AreEqual( new Point( 10, 10 ), col.LastMouseDownPoint );
        }

        [Test] public void IndentWithFixedSize()
        {
            SetupTreeStructureColumn();

            MockJetListViewColumn col = new MockJetListViewColumn();
            col.Width = 20;
            col.FixedSize = true;
            _columnCollection.Add( col );

            JetListViewNode node = _nodeCollection.Add( "Test" );
            JetListViewNode child = _nodeCollection.Add( "Child", node );
            _rowRenderer.DrawRow( null, new Rectangle( 0, 0, 100, 20 ), node, RowState.None );
            Assert.AreEqual( new Rectangle( 20, 0, 20, 20 ), col.LastDrawItemRect );
            _rowRenderer.DrawRow( null, new Rectangle( 0, 0, 100, 20 ), child, RowState.None );
            Assert.AreEqual( new Rectangle( 40, 0, 20, 20 ), col.LastDrawItemRect );
        }

        [Test] public void CompressIndent()  // OM-8539
        {
            SetupTreeStructureColumn();
            MockJetListViewColumn col = new MockJetListViewColumn();
            col.Width = 20;
            col.FixedSize = true;
            _columnCollection.Add( col );

            MockJetListViewColumn col2 = new MockJetListViewColumn();
            col2.Width = 30;
            _columnCollection.Add( col2 );

            MockJetListViewColumn col3 = new MockJetListViewColumn();
            col3.Width = 30;
            _columnCollection.Add( col3 );

            JetListViewNode node = _nodeCollection.Add( "Test" );
            _rowRenderer.DrawRow( null, new Rectangle( 0, 0, 100, 20 ), node, RowState.None );
            Assert.AreEqual( new Rectangle( 20, 0, 20, 20 ), col.LastDrawItemRect );

            JetListViewNode child = _nodeCollection.Add( "Child", node );
            _rowRenderer.DrawRow( null, new Rectangle( 0, 0, 100, 20 ), child, RowState.None );
            Assert.AreEqual( new Rectangle( 40, 0, 10, 20 ), col.LastDrawItemRect );
            Assert.AreEqual( new Rectangle( 50, 0, 30, 20 ), col3.LastDrawItemRect );
        }

        [Test] public void SizeToContentOnUpdate()
        {
            SetupSizeToContentColumn();
            _controlPainter.SetExpectedSize( "MockComparable:LongTest", new Size( 20, 10 ) );
            _controlPainter.SetExpectedSize( "MockComparable:Test", new Size( 10, 10 ) );

            MockComparable comparable = new MockComparable( "Test" );
            _nodeCollection.Add( comparable );
            Assert.AreEqual( 10, _rowRenderer.ScrollRange );

            _rowRenderer.ScrollRangeChanged += new EventHandler( HandleScrollRangeChanged );

            comparable.Value = "LongTest";
            _nodeCollection.Update( comparable );
            Assert.AreEqual( 20, _rowRenderer.ScrollRange );
        }

        [Test] public void ScrollRangeChangeOnManualResize()
        {
            MockJetListViewColumn col = new MockJetListViewColumn();
            col.Width = 20;
            col.FixedSize = true;
            _columnCollection.Add( col );

            _rowRenderer.ScrollRangeChanged += new EventHandler( HandleScrollRangeChanged );

            col.Width = 30;
            Assert.AreEqual( 1, _scrollRangeChanges );
        }

        [Test] public void AutoSize()
        {
            _rowRenderer.VisibleWidth = 10;

            MockJetListViewColumn col = new MockJetListViewColumn();
            col.AutoSize = true;
            _columnCollection.Add( col );
            Assert.AreEqual( 10, col.Width );

            _rowRenderer.VisibleWidth = 30;
            Assert.AreEqual( 30, col.Width );
        }

        [Test] public void AutoSizeMultiple()  // OM-8700
        {
            _rowRenderer.VisibleWidth = 30;

            MockJetListViewColumn col = new MockJetListViewColumn();
            col.AutoSize = true;
            _columnCollection.Add( col );

            MockJetListViewColumn col2 = new MockJetListViewColumn();
            col2.AutoSize = true;
            _columnCollection.Add( col2 );

            Assert.AreEqual( 15, col.Width );
            Assert.AreEqual( 15, col2.Width );

            _rowRenderer.VisibleWidth = 40;
            Assert.AreEqual( 20, col.Width );
            Assert.AreEqual( 20, col2.Width );
        }

        [Test] public void AutoSizeNegative()  // OM-8701
        {
            _rowRenderer.VisibleWidth = 30;

            MockJetListViewColumn col = new MockJetListViewColumn();
            col.Width = 20;
            _columnCollection.Add( col );

            MockJetListViewColumn col2 = new MockJetListViewColumn();
            col2.AutoSize = true;
            _columnCollection.Add( col2 );

            _rowRenderer.VisibleWidth = 10;
            Assert.AreEqual( 10, col2.Width );
        }

        [Test] public void AutoSizeMinWidth()
        {
            _rowRenderer.VisibleWidth = 30;

            _columnCollection.BeginUpdate();
            MockJetListViewColumn col = new MockJetListViewColumn();
            col.AutoSize = true;
            col.AutoSizeMinWidth = 40;
            col.Width = 40;
            _columnCollection.Add( col );

            MockJetListViewColumn col2 = new MockJetListViewColumn();
            col2.Width = 10;
            _columnCollection.Add( col2 );
            _columnCollection.EndUpdate();

            Assert.AreEqual( 40, col.Width );
            Assert.AreEqual( 10, col2.Width );

            _rowRenderer.VisibleWidth = 60;
            Assert.AreEqual( 50, col.Width );
            Assert.AreEqual( 10, col2.Width );

            _rowRenderer.VisibleWidth = 30;
            Assert.AreEqual( 40, col.Width );
            Assert.AreEqual( 10, col2.Width );
        }

        [Test] public void AutoSizeMinWidthMultiple()
        {
            _rowRenderer.VisibleWidth = 30;

            _columnCollection.BeginUpdate();
            MockJetListViewColumn col = new MockJetListViewColumn();
            col.AutoSize = true;
            col.AutoSizeMinWidth = 40;
            col.Width = 40;
            _columnCollection.Add( col );

            MockJetListViewColumn col2 = new MockJetListViewColumn();
            col2.Width = 10;
            col2.AutoSize = true;
            _columnCollection.Add( col2 );
            _columnCollection.EndUpdate();

            Assert.AreEqual( 40, col.Width );
            Assert.AreEqual( 10, col2.Width );

            _rowRenderer.VisibleWidth = 60;
            Assert.AreEqual( 40, col.Width );
            Assert.AreEqual( 20, col2.Width );

            _rowRenderer.VisibleWidth = 100;
            Assert.AreEqual( 50, col.Width );
            Assert.AreEqual( 50, col2.Width );
        }

        [Test] public void TotalWidthWithIndentColumn()
        {
            SetupTreeStructureColumn();

            MockJetListViewColumn col2 = new MockJetListViewColumn();
            col2.FixedSize = true;
            col2.Width = 20;
            _columnCollection.Add( col2 );

            SetupSizeToContentColumn();
            _controlPainter.SetExpectedSize( "Test", new Size( 10, 10 ) );
            _rowRenderer.ScrollRangeChanged += new EventHandler( HandleScrollRangeChanged );
            _nodeCollection.Add( "Test" );
            Assert.AreEqual( 1, _scrollRangeChanges );
            Assert.AreEqual( 50, _rowRenderer.ScrollRange );
        }

	    private void HandleScrollRangeChanged( object sender, EventArgs e )
	    {
            _scrollRangeChanges++;
	    }
	}
}
