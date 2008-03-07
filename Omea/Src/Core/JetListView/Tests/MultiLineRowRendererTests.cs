/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;

namespace JetBrains.JetListViewLibrary.Tests
{
	[TestFixture]
    public class MultiLineRowRendererTests
	{
        private JetListViewColumnCollection _columnCollection;
        private JetListViewNodeCollection _nodeCollection;
        private MultiLineRowRenderer _renderer;
        private MockColumnSchemeProvider _schemeProvider;
        private MockJetListViewColumn _column1;
        private MockJetListViewColumn _column2;
	    private JetListViewNode _testNode;
        private MultiLineColumnScheme _defaultScheme;

	    [SetUp] public void SetUp()
        {
            _schemeProvider = new MockColumnSchemeProvider();
            _defaultScheme = _schemeProvider.DefaultScheme;
            _columnCollection = new JetListViewColumnCollection();
            _renderer = new MultiLineRowRenderer( _columnCollection );
            _renderer.ColumnSchemeProvider = _schemeProvider;
            _renderer.VisibleWidth = 100;
            _renderer.RowHeight = 20;

            _nodeCollection = new JetListViewNodeCollection( null );
            _testNode = _nodeCollection.Add( "Test" );

	        _column1 = new MockJetListViewColumn();
            _column2 = new MockJetListViewColumn();
        }

        [Test] public void TestDraw()
        {
            _defaultScheme.AddColumn( _column1, 0, 0, 0, 100, 0, SystemColors.ControlText, HorizontalAlignment.Left );

            Rectangle rcPaint = new Rectangle( 0, 0, 100, 20 );
            _renderer.DrawRow( null, rcPaint, _testNode, RowState.None );
            Assert.AreEqual( rcPaint, _column1.LastDrawItemRect );
        }

        [Test] public void TestDrawStretch()
        {
            _defaultScheme.AddColumn( _column1, 0, 0, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );

            _renderer.VisibleWidth = 120;
            Rectangle rcPaint = new Rectangle( 0, 50, 120, 20 );
            _renderer.DrawRow( null, rcPaint, _testNode, RowState.None );
            Assert.AreEqual( rcPaint, _column1.LastDrawItemRect );
        }

        [Test] public void TwoColumns()
        {
            _defaultScheme.AddColumn( _column1, 0, 0, 0, 80, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );
            _defaultScheme.AddColumn( _column2, 0, 0, 80, 20, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );

            _renderer.VisibleWidth = 100;
            _renderer.DrawRow( null, new Rectangle( 0, 0, 100, 20 ), _testNode, RowState.None );
            Assert.AreEqual( new Rectangle( 0, 0, 80, 20 ), _column1.LastDrawItemRect );
            Assert.AreEqual( new Rectangle( 80, 0, 20, 20 ), _column2.LastDrawItemRect );
        }

        [Test] public void TwoRows()
        {
            _defaultScheme.AddColumn( _column1, 0, 0, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );
            _defaultScheme.AddColumn( _column2, 1, 1, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );

            _renderer.DrawRow( null, new Rectangle( 0, 0, 100, 50 ), _testNode, RowState.None );
            Assert.AreEqual( new Rectangle( 0, 0, 100, 20 ), _column1.LastDrawItemRect );
            Assert.AreEqual( new Rectangle( 0, 20, 100, 20 ), _column2.LastDrawItemRect );
        }

        [Test] public void RightAnchor()
        {
            _defaultScheme.AddColumn( _column1, 0, 0, 0, 80, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );
            _defaultScheme.AddColumn( _column2, 0, 0, 80, 20, ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );

            _renderer.VisibleWidth = 120;
            _renderer.DrawRow( null, new Rectangle( 0, 0, 120, 20 ), _testNode, RowState.None );
            Assert.AreEqual( new Rectangle( 0, 0, 100, 20 ), _column1.LastDrawItemRect );
            Assert.AreEqual( new Rectangle( 100, 0, 20, 20 ), _column2.LastDrawItemRect );
        }

        [Test] public void RowHeight()
        {
            _defaultScheme.AddColumn( _column1, 0, 0, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );
            _defaultScheme.AddColumn( _column2, 1, 1, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );

            Assert.AreEqual( 40, _renderer.GetRowHeight( _testNode ) );            
        }

        [Test] public void UpdateItem()
        {
            _defaultScheme.AddColumn( _column1, 0, 0, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );
            _defaultScheme.AddColumn( _column2, 1, 1, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );

            _renderer.UpdateItem( "Test" );
            Assert.AreEqual( "Test", _column1.LastUpdatedItem );
            Assert.AreEqual( "Test", _column2.LastUpdatedItem );
        }

        [Test] public void ColumnBounds()
        {
            _defaultScheme.AddColumn( _column1, 0, 0, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );
            _defaultScheme.AddColumn( _column2, 1, 1, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );

            _renderer.VisibleWidth = 120;
            Assert.AreEqual( new Rectangle( 0, 20, 120, 20 ), _renderer.GetColumnBounds( _column2, _testNode ) );
        }

        [Test] public void ColumnAt()
        {
            _defaultScheme.AddColumn( _column1, 0, 0, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );
            _defaultScheme.AddColumn( _column2, 1, 1, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );

            _renderer.VisibleWidth = 120;
            Assert.AreEqual( _column1, _renderer.GetColumnAt( _testNode, 110, 10 ) );
            Assert.AreEqual( _column2, _renderer.GetColumnAt( _testNode, 110, 30 ) );
        }

        [Test] public void HandleMouseDown()
        {
            _defaultScheme.AddColumn( _column1, 0, 0, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );
            _defaultScheme.AddColumn( _column2, 1, 1, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );

            _renderer.VisibleWidth = 120;
            _renderer.HandleMouseDown( _testNode, 110, 30, MouseButtons.Left, Keys.None );
            Assert.AreEqual( new Point( 110, 10 ), _column2.LastMouseDownPoint );
        }

        [Test] public void IndentColumn()
        {
            TreeStructureColumn column = new TreeStructureColumn();
            column.Width = 20;
            _defaultScheme.AddColumn( column, 0, 0, 0, 0, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );
            _defaultScheme.AddColumn( _column1, 0, 0, 0, 100, ColumnAnchor.Left | ColumnAnchor.Right, 
                SystemColors.ControlText, HorizontalAlignment.Left );
            _defaultScheme.AlignTopLevelItems = true;
            
            _renderer.DrawRow( null, new Rectangle( 0, 0, 100, 20 ), _testNode, RowState.None );
            Assert.AreEqual( new Rectangle( 20, 0, 80, 20 ), _column1.LastDrawItemRect );
            
            JetListViewNode childNode = _nodeCollection.Add( "Child", _testNode );

            _renderer.DrawRow( null, new Rectangle( 0, 0, 100, 20 ), childNode, RowState.None );
            Assert.AreEqual( new Rectangle( 40, 0, 60, 20 ), _column1.LastDrawItemRect );
        }
    }
}
