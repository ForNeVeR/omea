// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Windows.Forms;
using JetBrains.UI.Interop;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// Manages incremental search in a JetListView.
	/// </summary>
	internal class IncrementalSearcher
	{
        private JetListViewNodeCollection _nodeCollection;
        private RowListRenderer _rowListRenderer;
        private IRowRenderer _rowRenderer;
        private SelectionModel _selection;
        private string _incSearchBuffer = "";
        private bool _incSearching;

	    public IncrementalSearcher( JetListViewNodeCollection nodeCollection,
            RowListRenderer rowListRenderer, SelectionModel selection )
	    {
	        _nodeCollection = nodeCollection;
	        _rowListRenderer = rowListRenderer;
            SelectionModel = selection;
        }

	    public IRowRenderer RowRenderer
	    {
	        get { return _rowRenderer; }
	        set { _rowRenderer = value; }
	    }

	    public SelectionModel SelectionModel
	    {
	        get { return _selection; }
	        set
	        {
	            if ( _selection != null )
	            {
                    _selection.FocusStateChanged -= new ViewNodeStateChangeEventHandler( HandleFocusStateChanged );
	            }
                _selection = value;
                _selection.FocusStateChanged += new ViewNodeStateChangeEventHandler( HandleFocusStateChanged );
	        }
	    }

	    public bool IncrementalSearch( string text )
        {
            if ( _nodeCollection.Nodes.Count == 0 )
            {
                return false;
            }

            JetListViewNode startNode = _selection.ActiveNode;
            if ( startNode == null )
            {
                startNode = _nodeCollection.Nodes [0];
            }

            lock( _nodeCollection )
            {
                if ( SearchEnumerator( text, _nodeCollection.EnumerateVisibleNodesForward( startNode ), null ) )
                {
                    return true;
                }

                if ( startNode != _nodeCollection.Nodes [0] )
                {
                    return SearchEnumerator( text,
                        _nodeCollection.EnumerateVisibleNodesForward( _nodeCollection.Nodes [0] ), startNode );
                }
            }
            return false;
        }

        private bool SearchEnumerator( string text, IEnumerator searchEnumerator,
            JetListViewNode stopNode )
        {
            while( searchEnumerator.MoveNext() )
            {
                JetListViewNode curNode = (JetListViewNode) searchEnumerator.Current;
                if ( curNode == stopNode )
                {
                    return false;
                }

                if ( _rowRenderer.MatchIncrementalSearch( curNode, text ) )
                {
                    _incSearching = true;
                    _rowRenderer.SearchHighlightText = text;
                    _nodeCollection.ExpandParents( curNode );
                    _rowListRenderer.InvalidateRow( curNode );
                    _selection.SelectAndFocusNode( curNode );
                    _incSearching = false;
                    return true;
                }
            }
            return false;
        }

        public void ClearIncrementalSearch()
        {
            _incSearchBuffer = "";
            if ( _rowRenderer != null && _rowRenderer.SearchHighlightText != null )
            {
                _rowRenderer.SearchHighlightText = null;
                if ( _selection.ActiveNode != null )
                {
                    _rowListRenderer.InvalidateRow( _selection.ActiveNode );
                }
            }
        }

        public bool IncrementalSearchNext( string text, MoveDirection dir )
        {
            JetListViewNode curNode = _selection.ActiveNode;
            IEnumerator enumerator = (dir == MoveDirection.Down )
                ? _nodeCollection.EnumerateNodesForward( curNode )
                : _nodeCollection.EnumerateNodesBackward( curNode );
            enumerator.MoveNext();

            if ( SearchEnumerator( text, enumerator, null ) )
            {
                return true;
            }

            JetListViewNode startNode = (dir == MoveDirection.Down)
                ? _nodeCollection.Nodes [0]
                : _nodeCollection.LastNode;

            if ( curNode == startNode )
            {
                return false;
            }

            enumerator = _nodeCollection.GetDirectionalEnumerator( startNode, dir );
            return SearchEnumerator( text, enumerator, curNode );
        }

	    public bool HandleKeyDown( Keys keyData )
	    {
            if ( keyData == Keys.Space && _incSearchBuffer.Length > 0 )
            {
                return true;  // Space should not toggle selection if we're in inc. search
            }

            else if ( keyData == Keys.Escape && _incSearchBuffer.Length > 0 )
            {
                ClearIncrementalSearch();
            }
            else if ( ( keyData == Keys.F3 || keyData == ( Keys.Shift | Keys.F3 ) ) &&
                _incSearchBuffer.Length > 0 )
            {
                if ( !IncrementalSearchNext( _incSearchBuffer,
                    (keyData == Keys.F3) ? MoveDirection.Down : MoveDirection.Up ) )
                {
                    Win32Declarations.MessageBeep( -1 );
                }
            }
            else if ( keyData == Keys.Back && _incSearchBuffer.Length > 0 )
            {
                if ( _incSearchBuffer.Length > 1 )
                {
                    _incSearchBuffer = _incSearchBuffer.Substring( 0, _incSearchBuffer.Length - 1 );
                    IncrementalSearch( _incSearchBuffer );
                }
                else
                {
                    ClearIncrementalSearch();
                }
            }
            else
            {
                return false;
            }

            return true;
        }

	    public void HandleKeyPress( char keyChar )
	    {
            if ( Char.IsLetterOrDigit( keyChar ) || Char.IsPunctuation( keyChar ) ||
                ( _incSearchBuffer.Length > 0 && Char.IsWhiteSpace( keyChar ) ) )
            {
                if ( IncrementalSearch( _incSearchBuffer + keyChar ) )
                {
                    _incSearchBuffer = _incSearchBuffer + keyChar;
                }
                else
                {
                    Win32Declarations.MessageBeep( -1 );
                }
            }
        }

        private void HandleFocusStateChanged( object sender, ViewNodeStateChangeEventArgs e )
        {
            if ( !_incSearching )
            {
                ClearIncrementalSearch();
            }
        }
	}
}
