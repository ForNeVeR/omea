/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using NUnit.Framework;

namespace JetBrains.JetListViewLibrary.Tests
{
	[TestFixture]
    public class IncrementalSearchTests
	{
        private JetListViewNodeCollection _nodeCollection;
        private JetListViewColumnCollection _columnCollection;
        private RowListRenderer _rowListRenderer;
        private MockRowRenderer _rowRenderer;
        private SelectionModel _selectionModel;
        private IncrementalSearcher _incSearcher;

        [SetUp] public void SetUp()
        {
            _nodeCollection = new JetListViewNodeCollection( null );
            _columnCollection = new JetListViewColumnCollection();
            _selectionModel = new MultipleSelectionModel( _nodeCollection );
            _rowListRenderer = new RowListRenderer( _nodeCollection, _selectionModel );
            _rowRenderer = new MockRowRenderer( _columnCollection );

            _incSearcher = new IncrementalSearcher( _nodeCollection, _rowListRenderer, _selectionModel );
            _incSearcher.RowRenderer = _rowRenderer;
        }

        [Test] public void IncrementalSearch()
        {
            JetListViewNode node = _nodeCollection.Add( "Test" );
            JetListViewNode node2 = _nodeCollection.Add( "Rest" );

            JetListViewColumn col = new JetListViewColumn();
            _columnCollection.Add( col );
            _incSearcher.IncrementalSearch( "R" );
            Assert.IsTrue( _selectionModel.IsNodeSelected( node2 ) );
            Assert.AreEqual( "R", _rowRenderer.SearchHighlightText );
        }

        [Test] public void IncrementalSearchUp()
        {
            JetListViewNode node = _nodeCollection.Add( "Test" );
            JetListViewNode node2 = _nodeCollection.Add( "Trest" );
            JetListViewNode node3 = _nodeCollection.Add( "Rest" );

            _selectionModel.SelectAndFocusNode( node3 );

            JetListViewColumn col = new JetListViewColumn();
            _columnCollection.Add( col );
            _incSearcher.IncrementalSearch( "T" );
            Assert.IsTrue( _selectionModel.IsNodeSelected( node ) );
        }

    }
}
