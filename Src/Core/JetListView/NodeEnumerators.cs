// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;

namespace JetBrains.JetListViewLibrary
{
    internal abstract class NodeEnumeratorBase: IVisibleNodeEnumerator
    {
        protected JetListViewNode _curNode;
        protected int _curChildIndex = -1;
        private int _minLevel = -1;
        private bool _skipCollapsed;
        private bool _skipFiltered;

        protected NodeEnumeratorBase( bool skipCollapsed, int minLevel )
        {
            _skipCollapsed = skipCollapsed;
            _skipFiltered = true;
            _minLevel = minLevel;
        }

        protected abstract bool MoveNextStep();

        public bool MoveNext()
        {
            while( true )
            {
                if ( !MoveNextStep() )
                    return false;

                if ( !_skipFiltered )
                    return true;

                JetListViewNode curNode = (JetListViewNode) Current;

                if ( _minLevel != -1 && curNode.Level < _minLevel )
                    return false;

                if ( curNode.FiltersAccept )
                    return true;
            }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public object Current
        {
            get { return _curNode [_curChildIndex]; }
        }

        public IViewNode CurrentNode
        {
            get { return (IViewNode) Current; }
        }

        protected bool NeedProcessChildren( JetListViewNode node )
        {
            if ( _skipFiltered && !node.FiltersAccept )
            {
                return false;
            }
            if ( _skipCollapsed && node.CollapseState != CollapseState.Expanded )
            {
                return false;
            }
            node.RequestChildren( RequestChildrenReason.Enumerate );
            return node.ChildCount > 0;
        }
    }

    /// <summary>
    /// Enumerator of visible items in a JetListViewNodeCollection.
    /// </summary>
    internal class NodeEnumerator: NodeEnumeratorBase
    {
        private int _startChildIndex;

        public NodeEnumerator( JetListViewNode startNode )
            : base( true, -1 )
        {
            _curNode = startNode;
            _startChildIndex = -1;
        }

        public NodeEnumerator( JetListViewNode startNode, int minLevel )
            : base( true, minLevel )
        {
            _curNode = startNode;
            _startChildIndex = -1;
        }

        public NodeEnumerator( JetListViewNode startNode, int startChildIndex, bool skipCollapsed )
            : base( skipCollapsed, -1 )
        {
            _curNode = startNode;
            _startChildIndex = startChildIndex;
        }

        protected override bool MoveNextStep()
        {
            if ( _startChildIndex >= 0 )
            {
                _curChildIndex = _startChildIndex;
                _startChildIndex = -1;
                return true;
            }

            if ( _curChildIndex >= 0 && NeedProcessChildren( _curNode [_curChildIndex] ) )
            {
                _curNode = _curNode [_curChildIndex];
                _curChildIndex = -1;
            }

            _curChildIndex++;
            while( _curChildIndex == _curNode.ChildCount )
            {
                if ( _curNode.Parent == null )
                {
                    // backtrack to keep enumerator positioned on the last item
                    _curChildIndex--;
                    return false;
                }
                _curChildIndex = _curNode.Parent.IndexOf( _curNode ) + 1;
                _curNode = _curNode.Parent;
            }
            return true;
        }

    }

    /// <summary>
    /// Enumerator of visible items in reverse order in a JetListViewNodeCollection.
    /// </summary>
    internal class ReverseNodeEnumerator: NodeEnumeratorBase, IEnumerator
    {
        private int _startChildIndex;

        public ReverseNodeEnumerator( JetListViewNode startNode, int startChildIndex, bool skipCollapsed )
            : base( skipCollapsed, -1 )
        {
            _curNode = startNode;
            _startChildIndex = startChildIndex;
        }

        protected override bool MoveNextStep()
        {
            if ( _startChildIndex >= 0 )
            {
                _curChildIndex = _startChildIndex;
                _startChildIndex = -1;
                return true;
            }

            if ( _curChildIndex == 0 )
            {
                if ( _curNode.Parent == null )
                    return false;
                int index = _curNode.Parent.IndexOf( _curNode );
                if ( index < 0 )
                    return false;
                SetCurNode( _curNode.Parent, index );
            }
            else
            {
                _curChildIndex--;
                while( NeedProcessChildren( _curNode [_curChildIndex] ) )
                {
                    SetCurNode( _curNode [_curChildIndex], _curNode [_curChildIndex].ChildCount-1 );
                }
            }
            return true;
        }

        private void SetCurNode( JetListViewNode node, int curIndex )
        {
            _curNode = node;
            _curChildIndex = curIndex;
        }
    }
}
