/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using JetBrains.Omea.Containers;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// A selection model implementation which allows selecting only a single item.
	/// </summary>
	internal class SingleSelectionModel: SelectionModel
	{
        private IViewNode _selectedNode;
        private IEnumerable _selectedNodesEnumerable;
        private EmptyEnumerator _emptyEnumerator = new EmptyEnumerator();

	    public SingleSelectionModel( JetListViewNodeCollection nodeCollection )
            : base( nodeCollection )
        {
            _selectedNodesEnumerable = new SelectedNodesEnumerable( this );
	    }

	    public override int Count
	    {
	        get { return (_selectedNode == null) ? 0 : 1; }
	    }

	    internal override IEnumerable SelectedNodes
	    {
	        get { return _selectedNodesEnumerable; }
	    }

	    public override IEnumerator GetEnumerator()
	    {
            JetListViewNode selectedLvNode = _selectedNode as JetListViewNode;
            if ( selectedLvNode == null )
            {
                return _emptyEnumerator;
            }
	        return new SingleItemEnumerator( selectedLvNode.Data );
	    }

	    public override object SelectionLock
	    {
	        get { return this; }
	    }

	    internal override void ClearSelection()
	    {
            if ( _selectedNode != null )
            {
                IViewNode oldSelectedNode = _selectedNode;
                _selectedNode = null;
                OnSelectionStateChanged( oldSelectedNode, false );
            }
	    }

	    internal override void SelectNode( IViewNode node )
	    {
            if ( node != _selectedNode )
            {
                IViewNode oldSelectedNode = _selectedNode;
                _selectedNode = node;
                if ( oldSelectedNode != null )
                {
                    OnSelectionStateChanged( oldSelectedNode, false );
                }
                if ( _selectedNode != null )
                {
                    OnSelectionStateChanged( _selectedNode, true );
                }
            }
	    }

	    internal override bool UnselectNode( IViewNode node )
	    {
	        if ( _selectedNode == node )
	        {
	            _selectedNode = null;
                OnSelectionStateChanged( node, false );
                return true;
	        }
            return false;
	    }

	    internal override bool IsNodeSelected( IViewNode node )
	    {
	        return node == _selectedNode;
	    }

	    internal override IViewNode[] SelectionToArray()
	    {
	        return new IViewNode[] { _selectedNode };
        }

        private class SelectedNodesEnumerable : IEnumerable
        {
            private readonly SingleSelectionModel _selectionModel;

            public SelectedNodesEnumerable( SingleSelectionModel selectionModel )
            {
                _selectionModel = selectionModel;
            }

            public IEnumerator GetEnumerator()
            {
                if ( _selectionModel._selectedNode == null )
                {
                    return _selectionModel._emptyEnumerator;
                }
                return new SingleItemEnumerator( _selectionModel._selectedNode );
            }
        }
    }

    internal class SingleItemEnumerator : IEnumerator
    {
        private readonly object _data;
        private bool _movedToData;

        public SingleItemEnumerator( object data )
        {
            _data = data;
        }

        public bool MoveNext()
        {
            if ( !_movedToData )
            {
                _movedToData = true;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _movedToData = false;
        }

        public object Current
        {
            get 
            {
                if ( !_movedToData )
                {
                    throw new InvalidOperationException( "Calling IEnumerator.Current before MoveNext" );
                }
                return _data;
            }
        }
    }
}
