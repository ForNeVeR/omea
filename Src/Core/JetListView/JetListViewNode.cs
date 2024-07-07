// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;

namespace JetBrains.JetListViewLibrary
{
    public enum CollapseState { NoChildren, Expanded, Collapsed };
    public enum RequestChildrenReason { Expand, Enumerate };

    /// <summary>
    /// Marker interface for a row displayed in JetListView (either a node or a group header).
    /// </summary>
    public interface IViewNode
    {
    }

    /// <summary>
    /// A single node in JetListView.
    /// </summary>
    public class JetListViewNode: IViewNode
    {
        private JetListViewNodeCollection _owner;
        private object _data;
        private JetListViewNode _parent;
        private ArrayList _children;
        private int _unacceptedChildCount;
        private byte _flags;

        internal JetListViewNode( JetListViewNodeCollection owner, object data )
        {
            _owner = owner;
            _data = data;
            SetFiltersAccept( true );
        }

        public object Data
        {
            get { return _data; }
        }

        public JetListViewNode Parent
        {
            get { return _parent; }
        }

        public int Level
        {
            get
            {
                int result = 0;
                JetListViewNode theParent = _parent;
                while( theParent.Parent != null )
                {
                    result++;
                    theParent = theParent.Parent;
                }
                return result;
            }
        }

        internal JetListViewNodeCollection Owner
        {
            get { return _owner; }
        }

        public bool Expanded
        {
            get { return ( _flags & 2 ) != 0; }
            set
            {
                if ( Expanded != value )
                {
                    _owner.OnExpandChanging( this );
                    if ( value )
                    {
                        RequestChildren( RequestChildrenReason.Expand );
                        _flags |= 2;
                    }
                    else
                    {
                        _flags &= 0xfd;
                    }
                    _owner.OnExpandChanged( this );
                }
            }
        }

        internal void RequestChildren( RequestChildrenReason reason )
        {
            if ( ChildCount == 0 && ( _flags & 8 ) == 0  )
            {
                _flags |= 8;
                if ( _owner.OnChildrenRequested( this, reason ) )
                {
                    _flags &= 0xfb;
                }
                else
                {
                    _flags &= 0xf7;
                }
            }
        }

        /// <summary>
        /// Gets the node which precedes the current node in the list.
        /// </summary>
        public JetListViewNode PrevNode
        {
            get
            {
                int index = _parent._children.IndexOf( this );
                if ( index > 0 )
                {
                    return (JetListViewNode) _parent._children [index-1];
                }
                return null;
            }
        }

        public JetListViewNode PrevFilteredNode
        {
            get
            {
                int index = _parent._children.IndexOf( this );
                while ( index > 0 )
                {
                    index--;
                    JetListViewNode child = (JetListViewNode) _parent._children [index];
                    if ( child.FiltersAccept )
                    {
                        return child;
                    }
                }
                return null;
            }
        }

        public JetListViewNode NextNode
        {
            get
            {
                int index = _parent._children.IndexOf( this );
                if ( index < _parent._children.Count-1 )
                {
                    return (JetListViewNode) _parent._children [index+1];
                }
                return null;
            }
        }

        public JetListViewNode NextFilteredNode
        {
            get
            {
                int index = _parent._children.IndexOf( this );
                while ( index < _parent._children.Count-1 )
                {
                    index++;
                    JetListViewNode child = (JetListViewNode) _parent._children [index];
                    if ( child.FiltersAccept )
                    {
                        return child;
                    }
                }
                return null;
            }

        }

        public bool FiltersAccept
        {
            get { return ( _flags & 1 ) != 0; }
        }

        internal void SetFiltersAccept( bool value )
        {
            if( value )
            {
                _flags |= 1;
            }
            else
            {
                _flags &= 0xfe;
            }
        }

        internal void AddChild( JetListViewNode itemNode )
        {
            itemNode._parent = this;
            if ( _children == null )
            {
                _children = new ArrayList();
            }

            IComparer comparer = _owner.GetNodeComparer( this );
            lock( _children )
            {
                if ( comparer == null )
                {
                    _children.Add( itemNode );
                }
                else
                {
                    int index = _children.BinarySearch( itemNode, comparer );
                    if ( index < 0 )
                        index = ~index;
                    _children.Insert( index, itemNode );
                }
            }

            _flags &= 0xfb;
            if ( !itemNode.FiltersAccept )
            {
                _unacceptedChildCount++;
            }
        }

        internal void RemoveChild( JetListViewNode node )
        {
            if ( _children != null )
            {
                lock( _children )
                {
                    int index = _children.IndexOf( node );
                    if ( index >= 0 )
                    {
                        _children.RemoveAt( index );
                        if ( !node.FiltersAccept )
                        {
                            _unacceptedChildCount--;
                        }
                    }
                }
            }
        }

        public int IndexOf( JetListViewNode itemNode )
        {
            if ( _children == null )
            {
                return -1;
            }
            return _children.IndexOf( itemNode );
        }

        internal void ClearChildren()
        {
            _children = null;
            _unacceptedChildCount = 0;
        }

        public ChildNodeCollection Nodes
        {
            get { return new ChildNodeCollection( this ); }
        }

        internal int ChildCount
        {
            get
            {
                if ( _children == null )
                {
                    return 0;
                }
                return _children.Count;
            }
        }

        public bool HasChildren
        {
            get { return ( ChildCount > 0 && _unacceptedChildCount < ChildCount) ||
                      ( _flags & 4 ) != 0 ; }
            set
            {
                if ( ChildCount == 0 )
                {
                    if( value )
                    {
                        _flags |= 4;
                    }
                    else
                    {
                        _flags &= 0xfb;
                    }
                    if ( value )
                    {
                        _flags &= 0xf7;
                    }
                }
            }
        }

        private static ArrayList emptyList = new ArrayList();

        internal IEnumerator GetChildEnumerator()
        {
            if ( _children == null )
            {
                return emptyList.GetEnumerator();
            }
            return _children.GetEnumerator();
        }

        public CollapseState CollapseState
        {
            get
            {
                if ( !HasChildren )
                    return CollapseState.NoChildren;
                return Expanded ? CollapseState.Expanded : CollapseState.Collapsed;
            }
        }

        public JetListViewNode this [int index ]
        {
            get
            {
                if ( index < 0 || index >= _children.Count )
                {
                    throw new ArgumentOutOfRangeException( "index", index, "Index was out of range (count=" + _children.Count + ")" );
                }
                return (JetListViewNode) _children [index];
            }
        }

        internal int CountVisibleItems()
        {
            int result = 0;
            if ( _children != null )
            {
                lock( _children )
                {
                    foreach( JetListViewNode child in _children )
                    {
                        if ( child.FiltersAccept )
                        {
                            result++;
                            if ( child.CollapseState == CollapseState.Expanded )
                            {
                                result += child.CountVisibleItems();
                            }
                        }
                    }
                }
            }
            return result;
        }

        public JetListViewNode GetLastChild( bool needExpanded )
        {
            if ( ChildCount == 0 )
            {
                return null;
            }
            JetListViewNode node = null;
            for( int i=_children.Count-1; i >= 0; i-- )
            {
                if ( ((JetListViewNode) _children [i]).FiltersAccept )
                {
                    node = (JetListViewNode) _children [i];
                    break;
                }
            }
            if ( node != null && node.ChildCount > 0 )
            {
                if ( needExpanded && node.CollapseState == CollapseState.Collapsed )
                {
                    return node;
                }
                JetListViewNode nodeChild = node.GetLastChild( needExpanded );
                if ( nodeChild != null )
                {
                    return nodeChild;
                }
            }
            return node;
        }

        internal JetListViewNode GetChildNode( int index )
        {
            return (JetListViewNode) _children [index];
        }

        internal bool IsChildOutOfSortedPosition( JetListViewNode node, IComparer comparer )
        {
            int index = _children.IndexOf( node );
            Debug.Assert( index >= 0 );

            int prevCmpResult = 0, nextCmpResult = 0;
            if ( index > 0 )
            {
                prevCmpResult = comparer.Compare( _children [index-1], node );
            }
            if ( index < _children.Count-1 )
            {
                nextCmpResult = comparer.Compare( node, _children [index+1] );
            }

            return prevCmpResult > 0 || nextCmpResult > 0;
        }

        internal void UpdateChildPosition( JetListViewNode node, IComparer comparer )
        {
            int index = _children.IndexOf( node );
            Debug.Assert( index >= 0 );

            _children.RemoveAt( index );
            index = _children.BinarySearch( node, comparer );
            if ( index < 0 )
            {
                index = ~index;
            }
            _children.Insert( index, node );
        }

        internal void MoveChild( JetListViewNode nodeToMove, JetListViewNode afterNode )
        {
            _children.Remove( nodeToMove );
            if ( afterNode == null )
            {
                _children.Insert( 0, nodeToMove );
            }
            else
            {
                int index = _children.IndexOf( afterNode );
                Debug.Assert( index >= 0 );
                _children.Insert( index+1, nodeToMove );
            }
        }

        internal IEnumerator EnumerateChildrenRecursive()
        {
            return new NodeEnumerator( this, Level+1 );
        }

        public void ExpandAll()
        {
            if ( HasChildren )
            {
                Expanded = true;
                if ( _children != null )
                {
                    lock( _children )
                    {
                        foreach( JetListViewNode node in _children )
                        {
                            node.ExpandAll();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Performs stable sort of children using the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer used for sorting.</param>
        internal void SortChildren( IComparer comparer )
        {
            if ( _children != null && _children.Count > 0 )
            {
                lock( _children )
                {
                    RedBlackTree tree = new RedBlackTree( comparer );
                    for( int i=0; i<_children.Count; i++ )
                    {
                        tree.RB_Insert( _children [i] );
                    }
                    RBNodeBase node = tree.GetMinimumNode();
                    for( int i=0; i<_children.Count; i++ )
                    {
                        _children [i] = node.Key;
                        node = tree.GetNext( node );
                    }
                }
            }
        }

        public void SetParent( JetListViewNode newParent )
        {
            _owner.SetNodeParent( this, newParent );
        }

        public override string ToString()
        {
            return "JetListViewNode(Data=" + _data.ToString() + ")";
        }

        public void SetExpandedRecursive( bool expanded )
        {
            if ( HasChildren )
            {
                Expanded = expanded;
                if ( _children != null )
                {
                    foreach( JetListViewNode child in _children )
                    {
                        child.SetExpandedRecursive( expanded );
                    }
                }
            }
        }

        internal void UpdateUnacceptedChildCount()
        {
            _unacceptedChildCount = 0;
            if ( _children != null )
            {
                lock( _children )
                {
                    foreach( JetListViewNode child in _children )
                    {
                        if ( !child.FiltersAccept )
                        {
                            _unacceptedChildCount++;
                        }
                    }
                }
            }
        }
    }

    internal enum MoveDirection { Up, Down };

    public class ChildNodeCollection: IEnumerable
    {
        private JetListViewNode _baseNode;

        public ChildNodeCollection( JetListViewNode baseNode )
        {
            _baseNode = baseNode;
        }

        public JetListViewNode this [int index]
        {
            get
            {
                return _baseNode.GetChildNode( index );
            }
        }

        public int Count
        {
            get { return _baseNode.ChildCount; }
        }

        public JetListViewNode Add( object data )
        {
            if ( data == null )
            {
                throw new ArgumentNullException( "data" );
            }

            return _baseNode.Owner.Add( data, _baseNode );
        }

        public void Remove( object data )
        {
            Guard.NullArgument( data, "data" );
            _baseNode.Owner.Remove( data, _baseNode );
        }

        /// <summary>
        /// Moves the specified node in the collection of children of the specified node.
        /// </summary>
        /// <param name="nodeToMove">The node to move.</param>
        /// <param name="afterNode">The node after which the moved node is inserted, or null if
        /// the moved node is inserted at the beginning of the list.</param>
        public void Move( JetListViewNode nodeToMove, JetListViewNode afterNode )
        {
            Guard.NullArgument( nodeToMove, "nodeToMove" );
            if ( nodeToMove.Parent != _baseNode )
            {
                throw new InvalidOperationException( "Node to move is a child of a different node" );
            }
            if ( afterNode != null && afterNode.Parent != _baseNode)
            {
                throw new InvalidOperationException( "Node to insert after is a child of a different node" );
            }
            _baseNode.Owner.MoveChild( _baseNode, nodeToMove, afterNode );
        }

        public void AddRange( object[] items )
        {
            for( int i=0; i<items.Length; i++ )
            {
                Add( items [i] );
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _baseNode.GetChildEnumerator();
        }

        public void Clear()
        {
            _baseNode.Owner.ClearChildren( _baseNode );
        }

        public bool Contains( object item )
        {
            if ( item == null )
            {
                throw new ArgumentNullException( "item" );
            }

            return _baseNode.Owner.NodeFromItem( _baseNode, item ) != null;
        }
    }
}
