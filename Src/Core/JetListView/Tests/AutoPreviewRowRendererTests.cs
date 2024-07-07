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
    public class AutoPreviewRowRendererTests
	{
        private JetListViewNodeCollection _nodeCollection;
        private JetListViewColumnCollection _columnCollection;
        private MockJetListViewColumn _baseColumn;
        private SingleLineRowRenderer _baseRowRenderer;
        private AutoPreviewRowRenderer _rowRenderer;
        private MockJetListViewColumn _previewColumn;
	    private IControlPainter _controlPainter;

	    [SetUp] public void SetUp()
        {
            _nodeCollection = new JetListViewNodeCollection( null );
            _columnCollection = new JetListViewColumnCollection();
            _baseRowRenderer = new SingleLineRowRenderer( _columnCollection, _nodeCollection );
            _baseColumn = new MockJetListViewColumn();
            _baseColumn.Width = 100;
            _columnCollection.Add( _baseColumn );
            _baseRowRenderer.RowHeight = 17;
            _previewColumn = new MockJetListViewColumn();
            _previewColumn.AutoPreviewHeight = 10;
            _rowRenderer = new AutoPreviewRowRenderer( _baseRowRenderer, _previewColumn, _columnCollection );
            _rowRenderer.VisibleWidth = 100;
            _controlPainter = new MockControlPainter();
            _columnCollection.ControlPainter = _controlPainter;
        }

        [Test] public void TestNodeHeight()
        {
            JetListViewNode node = _nodeCollection.Add( "Test" );
            Assert.AreEqual( 27, _rowRenderer.GetRowHeight( node ) );
        }

        [Test] public void TestDrawRow()
        {
            Rectangle rc = new Rectangle( 0, 0, 100, 27 ) ;
            JetListViewNode node = _nodeCollection.Add( "Test" );
            _rowRenderer.DrawRow( null, rc, node, RowState.None );
            Assert.AreEqual( new Rectangle( 0, 17, 100, 10 ), _previewColumn.LastDrawItemRect );
        }

        [Test] public void TestMouseDown()
        {
            JetListViewNode node = _nodeCollection.Add( "Test" );
            _rowRenderer.HandleMouseDown( node, 10, 20, MouseButtons.Left, Keys.None );
            Assert.AreEqual( new Point( 10, 3 ), _previewColumn.LastMouseDownPoint );
        }

	    [Test] public void TestScrollOffset()
	    {
            _rowRenderer.ScrollOffset = 10;
            Rectangle rc = new Rectangle( 0, 0, 100, 27 ) ;
            JetListViewNode node = _nodeCollection.Add( "Test" );
            _rowRenderer.DrawRow( null, rc, node, RowState.None );
            Assert.AreEqual( new Rectangle( -10, 17, 100, 10 ), _previewColumn.LastDrawItemRect );
	    }
	}
}
