// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Threading;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;

namespace JetBrains.JetListViewLibrary
{
    public interface INodeCollection
    {
        void SetItemComparer( object parentItem, IComparer comparer );

        event JetListViewNodeEventHandler NodeAdded;
        event JetListViewNodeEventHandler NodeRemoving;
        event JetListViewNodeEventHandler NodeRemoved;
        event JetListViewNodeEventHandler NodeExpandChanged;

        /// <summary>
        /// Occurs before a node is expanded or collapsed.
        /// </summary>
        event JetListViewNodeEventHandler NodeExpandChanging;

        /// <summary>
        /// Occurs when the children of the specified node are requested, either during
        /// enumeration or when the user actually expands the node to see them.
        /// </summary>
        event RequestChildrenEventHandler ChildrenRequested;

        bool Contains( object item );

        /// <summary>
        /// Returns true if the collection does not contain any nodes.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Returns a node which contains the specified data item.
        /// </summary>
        /// <param name="item">The data item to find the node for.</param>
        /// <returns>The tree node, or null if no such node is found.</returns>
        /// <remarks>If the specified data item is present in multiple places of the tree,
        /// it is not defined which of the occurences is returned.</remarks>
        JetListViewNode NodeFromItem( object item );

        /// <summary>
        /// Returns the child node of the specified node which contains the specified data item.
        /// </summary>
        /// <param name="parent">The parent node, or null if the top-level node should be returned.</param>
        /// <param name="item">The data item to find the node for.</param>
        /// <returns>The tree node, or null if no such node is found.</returns>
        JetListViewNode NodeFromItem( JetListViewNode parent, object item );

        /// <summary>
        /// Returns the array of all nodes which contain the specified data item.
        /// </summary>
        /// <param name="item">The data item to find the nodes for.</param>
        /// <returns>The array of nodes, or an empty array if the item was not found.</returns>
        JetListViewNode[] NodesFromItem( object item );

        IEnumerator EnumerateNodesForward( JetListViewNode startNode );
        IEnumerator EnumerateNodesBackward( JetListViewNode startNode );

        /// <summary>
        /// Returns the total number of nodes which have not been collapsed or filtered away.
        /// </summary>
        int VisibleItemCount { get; }

        /// <summary>
        /// Returns the topmost node visible in the list.
        /// </summary>
        JetListViewNode FirstVisibleNode { get; }

        /// <summary>
        /// Performs recursive stable sort of all nodes in the collection according to the
        /// comparer specified for each node.
        /// </summary>
        void Sort();

        /// <summary>
        /// Begins a batch update of the node collection.
        /// </summary>
        void BeginUpdate();

        /// <summary>
        /// Ends the batch update of the node collection.
        /// </summary>
        void EndUpdate();
    }

    internal interface IVisibleNodeEnumerator: IEnumerator
    {
        IViewNode CurrentNode { get; }
    }

    internal class EmptyVisibleNodeEnumerator: IVisibleNodeEnumerator
    {
        public IViewNode CurrentNode
        {
            get { return null; }
        }

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
        }

        public object Current
        {
            get { return null; }
        }
    }

    internal interface IVisibleNodeCollection
    {
        IVisibleNodeEnumerator GetFullEnumerator();
        MoveDirection GetMoveDirection( IViewNode startNode, IViewNode endNode );
        IVisibleNodeEnumerator GetDirectionalEnumerator( IViewNode startNode, MoveDirection direction );
        IViewNode GetVisibleParent( IViewNode node );
        bool IsNodeVisible( IViewNode node );

        /// <summary>
        /// Returns the total number of nodes currently visible.
        /// </summary>
        int VisibleNodeCount { get; }

        /// <summary>
        /// Returns the first node visible in the view.
        /// </summary>
        IViewNode FirstVisibleViewNode { get; }

        /// <summary>
        /// Returns the last node visible in the view.
        /// </summary>
        IViewNode LastVisibleViewNode { get; }

        /// <summary>
        /// Ensures that the specified node is visible (expands its parents and the group in which
        /// it may be contained).
        /// </summary>
        /// <param name="node">The node to show.</param>
        void EnsureNodeVisible( IViewNode node );

        event EventHandler NodesCollapsed;

        event ViewNodeEventHandler ViewNodeRemoving;
    }

    /// <summary>
	/// Manages the structure of items visible in a JetListView.
	/// </summary>
	internal class JetListViewNodeCollection: INodeCollection, IVisibleNodeCollection
	{
        private JetListViewNode _rootNode;
        private JetListViewFilterCollection _filters;
        private JetListViewNodeMap _nodeMap = new JetListViewNodeMap();
        private HashMap _comparerMap = new HashMap();
        private VisibleItemsEnumerable _visibleItemsEnumerable;
        private int _batchUpdateCount = 0;
        private bool _havePendingUpdates = false;
        private JetListViewNode _lastUpdatedNode;
        private bool _flatList = true;
        private int _version = 0;

        private HashSet _addedNodes = null;
        private HashSet _removedNodes = null;
        private HashSet _changedNodes = null;
        private bool _fullUpdate = false;
        private bool filtersAccept;

        internal JetListViewNodeCollection( JetListViewFilterCollection filters )
		{
            _rootNode = new JetListViewNode( this, null );
            _rootNode.Expanded = true;
            _visibleItemsEnumerable = new VisibleItemsEnumerable( _rootNode );

            _filters = filters;
            if ( _filters != null )
            {
                _filters.FilterListChanged += new EventHandler( HandleFilterListChanged );
            }
		}

        public event JetListViewNodeEventHandler NodeAdded;
        public event JetListViewNodeEventHandler NodeRemoving;
        public event JetListViewNodeEventHandler NodeRemoved;
        public event JetListViewNodeEventHandler VisibleNodeAdded;
        public event JetListViewNodeEventHandler VisibleNodeRemoving;
        public event JetListViewNodeEventHandler VisibleNodeRemoved;
        public event JetListViewNodeEventHandler NodeMoving;
        public event JetListViewNodeEventHandler NodeMoved;
        public event MultipleNodesChangedEventHandler MultipleNodesChanged;
        public event JetListViewNodeEventHandler NodeExpandChanging;
        public event JetListViewNodeEventHandler NodeExpandChanged;
        public event JetListViewNodeEventHandler NodeChanged;
        public event RequestChildrenEventHandler ChildrenRequested;
        public event EventHandler Sorted;
        public event EventHandler NodesCollapsed;
        public event ViewNodeEventHandler ViewNodeRemoving;
        public event EventHandler FilterListChanged;

        #region Event Caller Methods
        private void OnNodeAdded( JetListViewNode node )
        {
            if ( NodeAdded != null )
            {
                NodeAdded( this, new JetListViewNodeEventArgs( node ) );
            }
        }

        private void OnVisibleNodeAdded( JetListViewNode node )
        {
            _version++;
            if ( _batchUpdateCount > 0 )
            {
                _havePendingUpdates = true;
                if ( !_fullUpdate )
                {
                    if ( _addedNodes == null )
                    {
                        _addedNodes = new HashSet();
                    }
                    _addedNodes.Add( node );
                }
            }
            else if ( VisibleNodeAdded != null )
            {
                VisibleNodeAdded( this, new JetListViewNodeEventArgs( node ) );
            }
        }

        private void OnNodeChanged( JetListViewNode node )
        {
            if ( _batchUpdateCount > 0 )
            {
                _havePendingUpdates = true;
                if ( !_fullUpdate )
                {
                    if ( _changedNodes == null )
                    {
                        _changedNodes = new HashSet();
                    }
                    _changedNodes.Add( node );
                }
            }
            else if ( NodeChanged != null )
            {
                NodeChanged( this, new JetListViewNodeEventArgs( node ) );
            }
        }

        private void OnNodeRemoving( JetListViewNode childNode )
        {
            if ( NodeRemoving != null )
            {
                NodeRemoving( this, new JetListViewNodeEventArgs( childNode ) );
            }
        }

        private void OnNodeRemoved( JetListViewNode childNode )
        {
            if ( NodeRemoved != null )
            {
                NodeRemoved( this, new JetListViewNodeEventArgs( childNode ) );
            }
        }

        private void OnVisibleNodeRemoving( JetListViewNode childNode )
        {
            _version++;
            if ( _batchUpdateCount > 0 )
            {
                _havePendingUpdates = true;
            }
            else if ( VisibleNodeRemoving != null )
            {
                VisibleNodeRemoving( this, new JetListViewNodeEventArgs( childNode ) );
            }

            if ( ViewNodeRemoving != null )
            {
                ViewNodeRemoving( this, new ViewNodeEventArgs( childNode ) );
            }
        }

        private void OnVisibleNodeRemoved( JetListViewNode childNode )
        {
            if ( _batchUpdateCount > 0 )
            {
                _havePendingUpdates = true;
                if ( !_fullUpdate )
                {
                    if ( _removedNodes == null )
                    {
                        _removedNodes = new HashSet();
                    }
                    _removedNodes.Add( childNode );
                }
            }
            else if ( VisibleNodeRemoved != null )
            {
                VisibleNodeRemoved( this, new JetListViewNodeEventArgs( childNode ) );
            }
        }

        private void OnMultipleNodesChanged()
        {
            _version++;
            if ( _batchUpdateCount > 0 )
            {
                _havePendingUpdates = true;
                _fullUpdate = true;
                _addedNodes = null;
                _removedNodes = null;
                _changedNodes = null;
            }
            else if ( MultipleNodesChanged != null )
            {
                MultipleNodesChanged( this,
                    new MultipleNodesChangedEventArgs( _addedNodes, _removedNodes, _changedNodes ) );
            }
        }

        protected internal void OnExpandChanging( JetListViewNode node )
        {
            if ( NodeExpandChanging != null )
            {
                NodeExpandChanging( this, new JetListViewNodeEventArgs( node ) );
            }
        }

        protected internal void OnExpandChanged( JetListViewNode node )
        {
            _version++;
            if ( NodeExpandChanged != null )
            {
                NodeExpandChanged( this, new JetListViewNodeEventArgs( node ) );
            }
            if ( !node.Expanded )
            {
                if ( NodesCollapsed != null )
                {
                    NodesCollapsed( this, EventArgs.Empty );
                }
            }
        }

        internal bool OnChildrenRequested( JetListViewNode node, RequestChildrenReason reason )
        {
            if ( ChildrenRequested != null )
            {
                RequestChildrenEventArgs args = new RequestChildrenEventArgs( node, reason );
                ChildrenRequested( this, args );
                return args.Handled;
            }
            return true;
        }

        internal void OnSorted()
        {
            _version++;
            if ( Sorted != null )
            {
                Sorted( this, EventArgs.Empty );
            }
        }

        internal void OnNodeMoving( JetListViewNode node )
        {
            _version++;
            JetListViewNodeEventArgs args = new JetListViewNodeEventArgs( node );
            if ( NodeMoving != null )
            {
                NodeMoving( this, args );
            }
        }

        internal void OnNodeMoved( JetListViewNode node )
        {
            _version++;
            JetListViewNodeEventArgs args = new JetListViewNodeEventArgs( node );
            if ( NodeMoved != null )
            {
                NodeMoved( this, args );
            }
        }

        #endregion

        public JetListViewNode Root
        {
            get { return _rootNode; }
        }

        internal int Version { get { return _version; } }

        public JetListViewNode Add( object item )
        {
            return Add( item, null );
        }

        internal JetListViewNode Add( object item, JetListViewNode parentNode )
        {
            lock( this )
            {
                _lastUpdatedNode = null;
                if ( parentNode == null )
                    parentNode = _rootNode;

                if ( parentNode != _rootNode )
                {
                    _flatList = false;
                }

                CollapseState oldCollapseState = parentNode.CollapseState;

                JetListViewNode node = new JetListViewNode( this, item );
                parentNode.AddChild( node );
                _nodeMap.Add( item, node );
                if ( _filters != null )
                {
                    filtersAccept = _filters.AcceptNode( node );
                    node.SetFiltersAccept( filtersAccept );
                    if ( !filtersAccept )
                    {
                        parentNode.UpdateUnacceptedChildCount();
                    }
                }
                OnNodeAdded( node );
                if ( IsNodeVisible( node ) )
                {
                    OnVisibleNodeAdded( node );
                }
                if ( parentNode != _rootNode && IsNodeVisible( parentNode ) &&
                    oldCollapseState != parentNode.CollapseState )
                {
                    OnNodeChanged( parentNode );
                }
                return node;
            }
        }

        public void Remove( object item, JetListViewNode parentNode )
        {
            lock( this )
            {
                _lastUpdatedNode = null;
                if ( parentNode == null )
                    parentNode = _rootNode;
                JetListViewNode[] childNodes = _nodeMap.NodesFromItem( item );
                if ( childNodes.Length == 0 )
                {
                    throw new ArgumentException( "Trying to remove item which was not added", "item" );
                }
                int removedNodes = 0;
                for( int i=0; i<childNodes.Length; i++ )
                {
                    if ( childNodes [i].Parent == parentNode )
                    {
                        RemoveNode( parentNode, childNodes [i] );
                        removedNodes++;
                    }
                }
                if ( removedNodes == 0 )
                {
                    throw new ArgumentException( "Trying to remove item from wrong parent" );
                }
            }
        }

        private void RemoveNode( JetListViewNode parentNode, JetListViewNode childNode )
        {
            ClearChildren( childNode );

            CollapseState oldCollapseState = parentNode.CollapseState;

            OnNodeRemoving( childNode );
            if ( IsNodeVisible( childNode ) )
            {
                OnVisibleNodeRemoving( childNode );
            }

            parentNode.RemoveChild( childNode );
            _nodeMap.Remove( childNode.Data, parentNode );

            OnNodeRemoved( childNode );
            if ( IsNodeVisible( childNode ) )
            {
                OnVisibleNodeRemoved( childNode );
            }

            if ( parentNode != _rootNode && IsNodeVisible( parentNode ) && oldCollapseState != parentNode.CollapseState )
            {
                OnNodeChanged( parentNode );
            }
        }

        internal void SetNodeParent( JetListViewNode childNode, JetListViewNode parentNode )
        {
            lock( this )
            {
                if ( parentNode == null )
                {
                    parentNode = _rootNode;
                }
                if ( parentNode != _rootNode )
                {
                    _flatList = false;
                }

                if ( parentNode != childNode.Parent )
                {
                    childNode.Parent.RemoveChild( childNode );
                    parentNode.AddChild( childNode );
                    OnMultipleNodesChanged();
                }
            }
        }

        public void ClearChildren( JetListViewNode node )
        {
            lock( this )
            {
                if ( node == _rootNode )
                {
                    ClearAll();
                }
                else
                {
                    for( int i=node.ChildCount-1; i >= 0; i-- )
                    {
                        RemoveNode( node, node.GetChildNode( i ) );
                    }
                }
            }
        }

        private void ClearAll()
        {
            _flatList = true;
            _rootNode.ClearChildren();
            _nodeMap.Clear();
            lock( _comparerMap )
            {
                _comparerMap.Clear();
            }
            OnMultipleNodesChanged();
        }

        public JetListViewNode NodeFromItem( JetListViewNode parent, object item )
        {
            if ( item == null )
            {
                throw new ArgumentNullException( "item" );
            }
            lock( this )
            {
                if ( parent == null )
                {
                    parent = _rootNode;
                }
                return _nodeMap.NodeFromItem( item, parent );
            }
        }

        private JetListViewNode ParentNodeFromItem( object parent )
        {
            JetListViewNode parentNode = _rootNode;
            if ( parent != null )
            {
                parentNode = NodeFromItem( parent );
                if ( parentNode == null )
                    throw new ArgumentException( "Unknown parent " + parent );
            }
            return parentNode;
        }

        public ChildNodeCollection Nodes
        {
            get { return new ChildNodeCollection( _rootNode ); }
        }

        public IEnumerable VisibleItems
        {
            get { return _visibleItemsEnumerable; }
        }

        private class VisibleItemsEnumerable: IEnumerable
        {
            private JetListViewNode _rootNode;

            public VisibleItemsEnumerable( JetListViewNode rootNode )
            {
                _rootNode = rootNode;
            }

            public IEnumerator GetEnumerator()
            {
                return new NodeEnumerator( _rootNode );
            }
        }

        public IEnumerator EnumerateNodesForward( JetListViewNode startNode )
        {
            Guard.NullArgument( startNode, "startNode" );
            return new NodeEnumerator( startNode.Parent, startNode.Parent.IndexOf( startNode ), false );
        }

        public IVisibleNodeEnumerator EnumerateVisibleNodesForward( JetListViewNode startNode )
        {
            Guard.NullArgument( startNode, "startNode" );
            return new NodeEnumerator( startNode.Parent, startNode.Parent.IndexOf( startNode ), true );
        }

        public IEnumerator EnumerateNodesBackward( JetListViewNode startNode )
        {
            Guard.NullArgument( startNode, "startNode" );
            return new ReverseNodeEnumerator( startNode.Parent, startNode.Parent.IndexOf( startNode ), false );
        }

        public IVisibleNodeEnumerator EnumerateVisibleNodesBackward( JetListViewNode startNode )
        {
            Guard.NullArgument( startNode, "startNode" );
            return new ReverseNodeEnumerator( startNode.Parent, startNode.Parent.IndexOf( startNode ), true );
        }

        internal IVisibleNodeEnumerator GetDirectionalEnumerator( JetListViewNode startNode, MoveDirection direction )
        {
            switch( direction )
            {
                case MoveDirection.Down:
                    return EnumerateVisibleNodesForward( startNode );

                case MoveDirection.Up:
                    return EnumerateVisibleNodesBackward( startNode );

                default:
                    throw new ArgumentException( "Invalid move direction" );
            }
        }

        public int VisibleItemCount
        {
            get
            {
                if ( _flatList && (_filters == null || _filters.Count == 0 ) )
                {
                    return _rootNode.ChildCount;
                }
                return _rootNode.CountVisibleItems();
            }
        }

        public JetListViewNode FirstVisibleNode
        {
            get
            {
                IEnumerator enumerator = VisibleItems.GetEnumerator();
                if ( !enumerator.MoveNext() )
                {
                    return null;
                }
                return (JetListViewNode) enumerator.Current;
            }
        }

        public JetListViewNode LastNode
        {
            get
            {
                lock( this )
                {
                    return _rootNode.GetLastChild( false );
                }
            }
        }

        public JetListViewNode LastVisibleNode
        {
            get
            {
                lock( this )
                {
                    return _rootNode.GetLastChild( true );
                }
            }
        }

        public JetListViewNode NodeFromItem( object item )
        {
            lock( this )
            {
                return _nodeMap.NodeFromItem( item );
            }
        }

        public JetListViewNode[] NodesFromItem( object item )
        {
            lock( this )
            {
                return _nodeMap.NodesFromItem( item );
            }
        }

        public void SetItemComparer( object parentItem, IComparer comparer )
        {
            JetListViewNode node = ParentNodeFromItem( parentItem );
            lock( _comparerMap )
            {
                _comparerMap [node] = new NodeComparer( comparer );
            }
        }

        internal IComparer GetNodeComparer( JetListViewNode node )
        {
            while( node != null )
            {
                IComparer itemComparer;
                lock( _comparerMap )
                {
                    itemComparer = (IComparer) _comparerMap [node];
                }
                if ( itemComparer != null )
                {
                    return itemComparer;
                }
                node = node.Parent;
            }
            return null;
        }

        public void Update( object item )
        {
            lock( this )
            {
                JetListViewNode[] nodes = NodesFromItem( item );
                if ( nodes.Length == 0 )
                    throw new ArgumentException( "Item not found in list", "item" );

                for( int i=0; i<nodes.Length; i++ )
                {
                    JetListViewNode node = nodes [i];
                    Guard.NullLocalVariable( node, "node" );
                    if ( _filters != null )
                    {
                        UpdateFiltersAccept( node );
                    }

                    JetListViewNode parent = node.Parent;
                    Guard.NullLocalVariable( parent, "parent" );
                    bool needMove = false;
                    IComparer comparer = GetNodeComparer( parent );
                    if ( comparer != null )
                    {
                        needMove = parent.IsChildOutOfSortedPosition( node, comparer );
                        if ( needMove )
                        {
                            OnNodeMoving( node );
                            parent.UpdateChildPosition( node, comparer );
                            OnNodeMoved( node );

                            // HACK (see OM-9953 and MultipleSortUpdate test)
                            // we receive updates after the nodes have been changed, so the IsChildInSortedPosition()
                            // check uses new state of the node and may erroneously tell us that the node is
                            // in sorted position when it actually is not
                            if ( _lastUpdatedNode != null && _lastUpdatedNode.Parent == parent &&
                                parent.IsChildOutOfSortedPosition( _lastUpdatedNode, comparer ) )
                            {
                                parent.SortChildren( comparer );
                                OnSorted();
                            }
                        }
                    }
                    if ( !needMove && IsNodeVisible( node ) )
                    {
                        OnNodeChanged( node );
                    }
                    _lastUpdatedNode = node;
                }
            }
        }

        private void UpdateFiltersAccept( JetListViewNode node )
        {
            bool filtersAccept = _filters.AcceptNode( node );
            if ( node.FiltersAccept && !filtersAccept )
            {
                OnVisibleNodeRemoving( node );
                node.SetFiltersAccept( false );
                OnVisibleNodeRemoved( node );
                node.Parent.UpdateUnacceptedChildCount();
            }
            else if ( !node.FiltersAccept && filtersAccept )
            {
                node.SetFiltersAccept( true );
                OnVisibleNodeAdded( node );
                node.Parent.UpdateUnacceptedChildCount();
            }
        }

        internal void MoveChild( JetListViewNode baseNode, JetListViewNode nodeToMove, JetListViewNode afterNode )
        {
            OnNodeMoving( nodeToMove );
            baseNode.MoveChild( nodeToMove, afterNode );
            OnNodeMoved( nodeToMove );
        }

        internal static bool IsNodeVisible( JetListViewNode node )
        {
            if ( !node.FiltersAccept )
            {
                return false;
            }

            JetListViewNode parent = node.Parent;
            while( parent != null )
            {
                if ( parent.CollapseState == CollapseState.Collapsed )
                {
                    return false;
                }
                parent = parent.Parent;
            }
            return true;
        }

        public bool Contains( object item )
        {
            if ( item == null )
            {
                throw new ArgumentNullException( "item" );
            }

            lock( this )
            {
                return _nodeMap.Contains( item );
            }
        }

        private void HandleFilterListChanged( object sender, EventArgs e )
        {
            bool anyChanges = false;
            lock( this )
            {
                anyChanges = UpdateFiltersRecursive( _rootNode );
            }
            if ( anyChanges )
            {
                OnMultipleNodesChanged();
                if ( FilterListChanged != null )
                {
                    FilterListChanged( this, EventArgs.Empty );
                }
            }
        }

        private bool UpdateFiltersRecursive( JetListViewNode node )
        {
            bool anyChanges = false;
            foreach( JetListViewNode child in node.Nodes )
            {
                bool newAccept = _filters.AcceptNode( child );
                if ( newAccept != child.FiltersAccept )
                {
                    child.SetFiltersAccept( newAccept );
                    anyChanges = true;
                }
                if ( UpdateFiltersRecursive( child ) )
                {
                    anyChanges = true;
                }
            }
            if ( anyChanges )
            {
                node.UpdateUnacceptedChildCount();
            }
            return anyChanges;
        }

        public void Sort()
        {
            lock( this )
            {
                SortRecursive( _rootNode );
            }
            OnSorted();
        }

        private void SortRecursive( JetListViewNode node )
        {
            node.SortChildren( GetNodeComparer( node ) );
            for( int i=0; i<node.ChildCount; i++ )
            {
                SortRecursive( node.GetChildNode( i ) );
            }
        }

        private class NodeComparer: IComparer
        {
            private IComparer _baseComparer;

            public NodeComparer( IComparer comparer )
            {
                _baseComparer = comparer;
            }

            public int Compare( object x, object y )
            {
                JetListViewNode node1 = (JetListViewNode) x;
                JetListViewNode node2 = (JetListViewNode) y;
                return _baseComparer.Compare( node1.Data, node2.Data );
            }
        }

        public bool IsEmpty
        {
            get { return !_rootNode.HasChildren; }
        }

        internal void ExpandParents( JetListViewNode node )
        {
            ArrayList nodes = ArrayListPool.Alloc();
            try
            {
                JetListViewNode theParent = node.Parent;
                while( theParent != null && theParent != Root )
                {
                    nodes.Insert( 0, theParent );
                    theParent = theParent.Parent;
                }
                foreach( JetListViewNode n in nodes )
                {
                    n.Expanded = true;
                }
            }
            finally
            {
                ArrayListPool.Dispose( nodes );
            }
        }

        public void BeginUpdate()
        {
            if ( Interlocked.Increment( ref _batchUpdateCount ) == 1 )
            {
                _fullUpdate = false;
            }
        }

        public void EndUpdate()
        {
            int value = Interlocked.Decrement( ref _batchUpdateCount );
            if ( value < 0 )
            {
                return;
                //throw new InvalidOperationException( "EndUpdate() before BeginUpdate()" );
            }
            if ( value == 0 && _havePendingUpdates )
            {
                OnMultipleNodesChanged();
                _havePendingUpdates = false;
            }
        }

        internal JetListViewNode GetVisibleParent( JetListViewNode node )
        {
            JetListViewNode parent = node.Parent;
            JetListViewNode topmostCollapsedParent = null;
            while( parent != null )
            {
                if ( !parent.Expanded )
                {
                    topmostCollapsedParent = parent;
                }
                parent = parent.Parent;
            }
            return topmostCollapsedParent;
        }

        internal int CompareVisibleOrder( JetListViewNode lhs, JetListViewNode rhs )
        {
            JetListViewNode origLhs = lhs;
            JetListViewNode origRhs = rhs;
            if ( lhs == rhs )
            {
                return 0;
            }
            while( lhs.Level > rhs.Level )
            {
                lhs = lhs.Parent;
            }
            while( rhs.Level > lhs.Level )
            {
                rhs = rhs.Parent;
            }
            while( lhs.Parent != rhs.Parent )
            {
                lhs = lhs.Parent;
                rhs = rhs.Parent;
            }
            if ( lhs == rhs )
            {
                return origLhs.Level - origRhs.Level;
            }
            int lhsIndex = lhs.Parent.IndexOf( lhs );
            int rhsIndex = rhs.Parent.IndexOf( rhs );
            return lhsIndex - rhsIndex;
        }

        public MoveDirection GetMoveDirection( IViewNode startNode, IViewNode endNode )
        {
            JetListViewNode startLvNode = (JetListViewNode) startNode;
            JetListViewNode endLvNode = (JetListViewNode) endNode;
            int rc = CompareVisibleOrder( startLvNode, endLvNode );
            if ( rc < 0 )
            {
                return MoveDirection.Down;
            }
            return MoveDirection.Up;
        }

        public void SetExpandedRecursive( bool expanded )
        {
            for( int i=0; i<_rootNode.ChildCount; i++ )
            {
                _rootNode.GetChildNode( i ).SetExpandedRecursive( expanded );
            }
        }

        IVisibleNodeEnumerator IVisibleNodeCollection.GetDirectionalEnumerator( IViewNode startNode, MoveDirection direction )
        {
            if ( startNode is JetListViewNode )
            {
                return GetDirectionalEnumerator( (JetListViewNode) startNode, direction );
            }
            return new EmptyVisibleNodeEnumerator();
        }

        IViewNode IVisibleNodeCollection.GetVisibleParent( IViewNode node )
        {
            JetListViewNode lvNode = node as JetListViewNode;
            if ( lvNode == null )
            {
                return null;
            }
            return GetVisibleParent( lvNode );
        }

        IVisibleNodeEnumerator IVisibleNodeCollection.GetFullEnumerator()
        {
            return (IVisibleNodeEnumerator) VisibleItems.GetEnumerator();
        }

        bool IVisibleNodeCollection.IsNodeVisible( IViewNode node )
        {
            JetListViewNode lvNode = node as JetListViewNode;
            if ( lvNode == null || !Contains( lvNode.Data ) )
            {
                return false;
            }
            while( lvNode != null )
            {
                if ( !lvNode.FiltersAccept )
                {
                    return false;
                }
                lvNode = lvNode.Parent;
            }
            return true;
        }

        void IVisibleNodeCollection.EnsureNodeVisible( IViewNode node )
        {
            ExpandParents( (JetListViewNode) node );
    	}

        int IVisibleNodeCollection.VisibleNodeCount
        {
            get { return VisibleItemCount; }
        }

        IViewNode IVisibleNodeCollection.LastVisibleViewNode
        {
            get { return LastVisibleNode; }
        }

        IViewNode IVisibleNodeCollection.FirstVisibleViewNode
        {
            get { return FirstVisibleNode; }
        }
	}
}
