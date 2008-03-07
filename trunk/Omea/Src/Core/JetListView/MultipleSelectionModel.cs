/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections;
using JetBrains.DataStructures;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// A selection model implementation which allows selecting multiple items.
	/// </summary>
	internal class MultipleSelectionModel: SelectionModel
	{
        private HashSet _selectedNodes;
        private IEnumerable _selectedNodesEnumerable;

        internal MultipleSelectionModel( JetListViewNodeCollection nodeCollection )
            : base( nodeCollection )
		{
            _selectedNodes = new HashSet();
            _selectedNodesEnumerable = new SelectedNodesEnumerable( this );
        }

	    public override int Count
	    {
	        get { return _selectedNodes.Count; }
	    }

	    public override object SelectionLock
	    {
	        get { return _selectedNodes; }
	    }

	    internal override IEnumerable SelectedNodes
	    {
	        get { return _selectedNodesEnumerable; }
	    }
	    
	    private static IEnumerator _emptyEnumerator = new ArrayList( 1 ).GetEnumerator();

	    public override IEnumerator GetEnumerator()
	    {
            return ( _selectedNodes.Count == 0 ) ? _emptyEnumerator : new SelectedItemEnumerator( _selectedNodes.GetEnumerator() );
        }

	    internal override void ClearSelection()
	    {
            IViewNode[] clearedNodes;
            lock( _selectedNodes )
            {
                clearedNodes = SelectionToArray();
                _selectedNodes.Clear();
            }
            foreach( IViewNode node in clearedNodes )
            {
                OnSelectionStateChanged( node, false );
            }
        }

	    internal override void SelectNode( IViewNode node )
	    {
            lock( _selectedNodes )
            {
                if ( _selectedNodes.Contains( node ) )
                {
                    return;
                }
                _selectedNodes.Add( node );
            }
            OnSelectionStateChanged( node, true );
        }

	    internal override bool UnselectNode( IViewNode node )
	    {
            lock( _selectedNodes )
            {
                if ( !_selectedNodes.Contains( node ) )
                {
                    return false;
                }
                _selectedNodes.Remove( node );
            }
            OnSelectionStateChanged( node, false );
            return true;
        }

	    internal override bool IsNodeSelected( IViewNode node )
	    {
            lock( _selectedNodes )
            {
                return _selectedNodes.Contains( node );
            }
        }

	    internal override IViewNode[] SelectionToArray()
	    {
            IViewNode[] selNodes;
            lock( _selectedNodes )
            {
                selNodes = new IViewNode[ _selectedNodes.Count ];
                int i=0;
                foreach( HashSet.Entry entry in _selectedNodes )
                {
                    selNodes [i++] = (IViewNode) entry.Key;
                }
            }
            return selNodes;
        }

        private class SelectedNodesEnumerable: IEnumerable
        {
            private MultipleSelectionModel _selection;

            public SelectedNodesEnumerable( MultipleSelectionModel selection )
            {
                _selection = selection;
            }

            public IEnumerator GetEnumerator()
            {
                return new SelectedNodeEnumerator( _selection._selectedNodes.GetEnumerator() );
            }
        }

        private class SelectedNodeEnumerator: IEnumerator
        {
            private IEnumerator _baseEnumerator;

            public SelectedNodeEnumerator( IEnumerator baseEnumerator )
            {
                _baseEnumerator = baseEnumerator;
            }

            public bool MoveNext()
            {
                return _baseEnumerator.MoveNext();
            }

            public void Reset()
            {
                _baseEnumerator.Reset();
            }

            public object Current
            {
                get
                {
                    HashSet.Entry entry = (HashSet.Entry) _baseEnumerator.Current;
                    return entry.Key;
                }
            }
        }

        private class SelectedItemEnumerator: IEnumerator
        {
            private IEnumerator _baseEnumerator;

            public SelectedItemEnumerator( IEnumerator baseEnumerator )
            {
                _baseEnumerator = baseEnumerator;                
            }

            public bool MoveNext()
            {
                while( _baseEnumerator.MoveNext() )
                {
                    HashSet.Entry entry = (HashSet.Entry) _baseEnumerator.Current;
                    if ( entry.Key is JetListViewNode )
                    {
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                _baseEnumerator.Reset();
            }

            public object Current
            {
                get
                {
                    HashSet.Entry entry = (HashSet.Entry) _baseEnumerator.Current;
                    JetListViewNode node = (JetListViewNode) entry.Key;
                    return node.Data;
                }
            }
        }
	}
}
