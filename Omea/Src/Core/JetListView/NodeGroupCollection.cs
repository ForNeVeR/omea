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
    /// <summary>
    /// Returns the names of groups for JetListView items.
    /// </summary>
    public interface IGroupProvider
    {
        /// <summary>
        /// Returns the group name for the specified item.
        /// </summary>
        /// <param name="item">The item displayed in the view.</param>
        /// <returns>The name of the group in which the item is shown.</returns>
        string GetGroupName( object item );
    }

    /// <summary>
    /// The node which is a header of a group.
    /// </summary>
    internal class GroupHeaderNode: IViewNode
    {
        private readonly NodeGroupCollection _owner;
        private bool _expanded = true;
        private string _text;
        private JetListViewNode _topNode = null;

        public GroupHeaderNode( NodeGroupCollection owner, string text )
        {
            _text = text;
            _owner = owner;
        }

        public bool Expanded
        {
            get { return _expanded; }
            set
            {
                if ( _expanded != value )
                {
                    _expanded = value;
                    _owner.OnGroupExpandChanged( this );
                }
            }
        }

        public string Text
        {
            get { return _text; }
        }

        internal JetListViewNode TopNode
        {
            get { return _topNode; }
            set { _topNode = value; }
        }
    }

	/// <summary>
	/// Collection of grouped items in a JetListView.
	/// </summary>
	internal class NodeGroupCollection: IVisibleNodeCollection, IDisposable
	{
        private JetListViewNodeCollection _nodeCollection;
	    private IGroupProvider _groupProvider;
        private SortedList _groupMap = new SortedList();

	    public NodeGroupCollection( JetListViewNodeCollection nodeCollection, IGroupProvider groupProvider )
	    {
	        _nodeCollection = nodeCollection;
            _groupProvider = groupProvider;
            _nodeCollection.ViewNodeRemoving += HandleViewNodeRemoving;
            _nodeCollection.NodeMoving += HandleNodeMoving;
            _nodeCollection.NodeChanged += HandleNodeChanged;
            _nodeCollection.NodesCollapsed += ForwardNodesCollapsed;
	    }

	    public void Dispose()
	    {
	        _nodeCollection.NodesCollapsed -= ForwardNodesCollapsed;
            _nodeCollection.ViewNodeRemoving -= HandleViewNodeRemoving;
            _nodeCollection.NodeMoving -= HandleNodeMoving;
            _nodeCollection.NodeChanged -= HandleNodeChanged;
        }

	    public event GroupEventHandler GroupAdded;
        public event GroupEventHandler GroupRemoved;
        public event ViewNodeEventHandler ViewNodeRemoving;
        public event GroupEventHandler GroupExpandChanged;
        public event EventHandler NodesCollapsed;

	    public GroupHeaderNode GetGroupHeader( string group )
	    {
            GroupHeaderNode node = (GroupHeaderNode) _groupMap [group];
            if ( node == null )
            {
                node = new GroupHeaderNode( this, group );
                _groupMap [group] = node;
                OnGroupAdded( node );
            }
            return node;
	    }

        internal GroupHeaderNode GetNodeGroupHeader( JetListViewNode node )
        {
            JetListViewNode lvNode = node as JetListViewNode;
            while( lvNode.Level > 0 )
            {
                lvNode = lvNode.Parent;
            }
            string groupName = _groupProvider.GetGroupName( lvNode.Data );
            if ( groupName == null )
            {
                throw new InvalidOperationException( "Group provider returned null group name for object " + lvNode.Data );
            }
            return GetGroupHeader( groupName );
        }

        public int GroupCount
        {
            get { return _groupMap.Count; }
        }

        public IGroupProvider GroupProvider
        {
            get { return _groupProvider; }
        }

        private void OnGroupAdded( GroupHeaderNode header )
        {
            if ( GroupAdded != null )
            {
                GroupAdded( this, new GroupEventArgs( header ) );
            }
        }

        private void OnGroupRemoved( GroupHeaderNode header )
        {
            if ( GroupRemoved != null )
            {
                GroupRemoved( this, new GroupEventArgs( header ) );
            }
        }

        internal void OnGroupExpandChanged( GroupHeaderNode headerNode )
	    {
            if ( GroupExpandChanged != null )
            {
                GroupExpandChanged( this, new GroupEventArgs( headerNode ) );
            }
            if ( !headerNode.Expanded )
            {
                if ( NodesCollapsed != null )
                {
                    NodesCollapsed( this, EventArgs.Empty );
                }
            }
	    }

        private void OnViewNodeRemoving( IViewNode viewNode )
        {
            if ( ViewNodeRemoving != null )
            {
                ViewNodeRemoving( this, new ViewNodeEventArgs( viewNode ) );
            }
        }

        public IVisibleNodeEnumerator GetFullEnumerator()
	    {
            return new GroupedItemEnumerator( this,
                _nodeCollection.VisibleItems.GetEnumerator(), MoveDirection.Down );
	    }

        public IVisibleNodeEnumerator GetDirectionalEnumerator( IViewNode startNode, MoveDirection direction )
        {
            Guard.NullArgument( startNode, "startNode" );
            JetListViewNode startLvNode = startNode as JetListViewNode;
            if ( startLvNode != null )
            {
                return new GroupedItemEnumerator( this,
                    _nodeCollection.GetDirectionalEnumerator( startLvNode, direction ), direction,
                    (direction == MoveDirection.Down) );
            }

            GroupHeaderNode startHeaderNode = startNode as GroupHeaderNode;
            Debug.Assert( startHeaderNode.TopNode != null );
            if ( startHeaderNode.TopNode == null )
            {
                return new GroupedItemEnumerator(this, new EmptyEnumerator(), direction, false );
            }

            GroupedItemEnumerator enumerator = new GroupedItemEnumerator( this,
               _nodeCollection.GetDirectionalEnumerator( startHeaderNode.TopNode, direction ),
                direction, false );
            if ( direction == MoveDirection.Up )
            {
                // move from first node of current group to last node of previous group
                enumerator.MoveNext();
            }
            return enumerator;
        }

	    public MoveDirection GetMoveDirection( IViewNode startNode, IViewNode endNode )
	    {
	        JetListViewNode startLvNode = startNode as JetListViewNode;
	        if ( startLvNode == null )
	        {
	            GroupHeaderNode startHeaderNode = (GroupHeaderNode) startNode;
	            startLvNode = startHeaderNode.TopNode;
	        }

	        JetListViewNode endLvNode = endNode as JetListViewNode;
	        if ( endLvNode == null )
	        {
	            GroupHeaderNode endHeaderNode = (GroupHeaderNode) endNode;
	            endLvNode = endHeaderNode.TopNode;
	        }

	        int orderDiff = _nodeCollection.CompareVisibleOrder( startLvNode, endLvNode );
	        if ( orderDiff == 0 && startNode is GroupHeaderNode && !(endNode is GroupHeaderNode) )
	        {
	            return MoveDirection.Down;
	        }
	        return (orderDiff < 0 )
	            ? MoveDirection.Down
	            : MoveDirection.Up;
	    }

	    public IViewNode GetVisibleParent( IViewNode node )
	    {
            if ( node is GroupHeaderNode )
            {
                return node;
            }
	        JetListViewNode lvNode = node as JetListViewNode;
	        GroupHeaderNode headerNode = GetNodeGroupHeader( lvNode );
            if ( !headerNode.Expanded )
            {
                return headerNode;
            }
            return _nodeCollection.GetVisibleParent( lvNode );
	    }

	    public bool IsNodeVisible( IViewNode node )
	    {
            if ( node is GroupHeaderNode )
            {
                return true;
            }
            JetListViewNode lvNode = node as JetListViewNode;
            if ( !JetListViewNodeCollection.IsNodeVisible( lvNode ) )
            {
                return false;
            }
            return GetNodeGroupHeader( lvNode ).Expanded;
	    }

	    public void EnsureNodeVisible( IViewNode node )
	    {
	        JetListViewNode lvNode = node as JetListViewNode;
            if ( lvNode != null )
            {
                _nodeCollection.ExpandParents( lvNode );
                GroupHeaderNode group = GetNodeGroupHeader( lvNode );
                group.Expanded = true;
            }
	    }

	    private void ForwardNodesCollapsed( object sender, EventArgs e )
        {
            if ( NodesCollapsed != null )
            {
                NodesCollapsed( this, e );
            }
        }

        private void HandleViewNodeRemoving( object sender, ViewNodeEventArgs e )
        {
            OnViewNodeRemoving( e.ViewNode );
            ProcessViewNodeRemoving( (JetListViewNode) e.ViewNode );
        }

        private void HandleNodeMoving( object sender, JetListViewNodeEventArgs e )
        {
            ProcessViewNodeRemoving( e.Node );
        }

	    private void ProcessViewNodeRemoving( JetListViewNode node )
	    {
	        if ( node.Level == 0 )
	        {
	            string nodeGroup = _groupProvider.GetGroupName( node.Data );
	            GroupHeaderNode nodeGroupHeader = (GroupHeaderNode) _groupMap [nodeGroup];
	            if ( nodeGroupHeader != null )
	            {
	                bool haveSameGroup = false;
	                int index = _nodeCollection.Root.IndexOf( node );
	                if ( index > 0 )
	                {
	                    JetListViewNode prevNode = _nodeCollection.Root.Nodes [index-1];
	                    if ( _groupProvider.GetGroupName( prevNode.Data ) == nodeGroup )
	                    {
	                        haveSameGroup = true;
	                    }
	                }
	                if ( !haveSameGroup && index < _nodeCollection.Root.ChildCount-1 )
	                {
	                    JetListViewNode nextNode = _nodeCollection.Root.Nodes [index+1];
	                    if ( _groupProvider.GetGroupName( nextNode.Data ) == nodeGroup )
	                    {
	                        haveSameGroup = true;
	                    }
	                }

	                if ( !haveSameGroup )
	                {
	                    RemoveGroup( nodeGroupHeader );
	                }
	            }
	        }
	    }

	    private void HandleNodeChanged( object sender, JetListViewNodeEventArgs e )
        {
            if ( e.Node.Level == 0 )
            {
                GroupHeaderNode groupHeader = GetNodeGroupHeader( e.Node );
                int headerIndex = _groupMap.IndexOfValue( groupHeader );
                if ( headerIndex > 0 )
                {
                    GroupHeaderNode prevGroup = (GroupHeaderNode) _groupMap.GetByIndex( headerIndex-1 );
                    if ( prevGroup.TopNode == e.Node )
                    {
                        RemoveGroup( prevGroup );
                        return;
                    }
                }
                if ( headerIndex < _groupMap.Count-1 )
                {
                    GroupHeaderNode nextGroup = (GroupHeaderNode) _groupMap.GetByIndex( headerIndex+1 );
                    if ( nextGroup.TopNode == e.Node )
                    {
                        RemoveGroup( nextGroup );
                        return;
                    }
                }
            }
        }

        private void RemoveGroup( GroupHeaderNode nodeGroupHeader )
        {
            OnViewNodeRemoving( nodeGroupHeader );
            _groupMap.Remove( nodeGroupHeader.Text );
            OnGroupRemoved( nodeGroupHeader );
        }

	    public void SetAllGroupsExpanded( bool expanded )
	    {
	        foreach( GroupHeaderNode node in _groupMap.Values )
	        {
	            node.Expanded = expanded;
	        }
	    }

	    public IViewNode LastVisibleViewNode
	    {
            get { return GetNodeOrCollapsedHeader( _nodeCollection.LastVisibleNode ); }
	    }

	    public IViewNode FirstVisibleViewNode
	    {
	        get { return GetNodeOrCollapsedHeader( _nodeCollection.FirstVisibleNode ); }
	    }

        private IViewNode GetNodeOrCollapsedHeader( JetListViewNode viewNode )
        {
            if ( viewNode != null )
            {
                GroupHeaderNode headerNode = GetNodeGroupHeader( viewNode );
                if ( !headerNode.Expanded )
                {
                    return headerNode;
                }
            }
            return viewNode;
        }

	    public int VisibleNodeCount
	    {
	        get
	        {
	            int count = 0;
                IEnumerator enumerator = GetFullEnumerator();
                while( enumerator.MoveNext() )
                {
                    count++;
                }
                return count;
	        }
	    }
	}

    internal class GroupedItemEnumerator: IVisibleNodeEnumerator
    {
        private readonly NodeGroupCollection _groupCollection;
        private GroupHeaderNode _curHeaderNode;
        private IEnumerator _baseEnumerator;
        private bool _skipFirstGroupHeader = false;
        private bool _onHeaderNode = false;
        private bool _lastHeaderNode = false;
        private MoveDirection _moveDirection;

        public GroupedItemEnumerator( NodeGroupCollection groupCollection, IEnumerator baseEnumerator,
            MoveDirection moveDirection )
        {
            _groupCollection = groupCollection;
            _baseEnumerator = baseEnumerator;
            _moveDirection = moveDirection;
        }

        public GroupedItemEnumerator( NodeGroupCollection groupCollection, IEnumerator baseEnumerator,
            MoveDirection moveDirection, bool skipFirstGroupHeader )
        {
            _groupCollection = groupCollection;
            _baseEnumerator = baseEnumerator;
            _moveDirection = moveDirection;
            _skipFirstGroupHeader = skipFirstGroupHeader;
        }

        public bool MoveNext()
        {
            if ( _onHeaderNode )
            {
                if ( _moveDirection == MoveDirection.Up )
                {
                    if ( _lastHeaderNode )
                    {
                        return false;
                    }
                    JetListViewNode groupNode = (JetListViewNode) _baseEnumerator.Current;
                    string newGroup = _groupCollection.GroupProvider.GetGroupName( groupNode.Data );
                    if ( newGroup == null )
                    {
                        throw new InvalidOperationException( "Group provider returned null group name for object " + groupNode.Data );
                    }
                    _curHeaderNode = _groupCollection.GetGroupHeader( newGroup );
                }
                if ( _curHeaderNode.Expanded )
                {
                    _onHeaderNode = false;
                    return true;
                }

                if ( !SkipCollapsedGroup() )
                {
                    if ( _moveDirection == MoveDirection.Up )
                    {
                        _lastHeaderNode = true;
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                if ( !_baseEnumerator.MoveNext() )
                {
                    if ( _moveDirection == MoveDirection.Up )
                    {
                        _onHeaderNode = true;
                        _lastHeaderNode = true;
                        return true;
                    }
                    return false;
                }
            }

            JetListViewNode curNode = (JetListViewNode) _baseEnumerator.Current;
            GroupHeaderNode newHeaderNode = _groupCollection.GetNodeGroupHeader( curNode );
            if ( newHeaderNode != _curHeaderNode )
            {
                if ( _curHeaderNode == null && _moveDirection == MoveDirection.Up )
                {
                    _curHeaderNode = newHeaderNode;
                }
                else
                {
                    if ( _moveDirection == MoveDirection.Down )
                    {
                        _curHeaderNode = newHeaderNode;
                        // if we're enumerating from middle of a group, we can't overwrite topNode
                        if ( !_skipFirstGroupHeader || _curHeaderNode.TopNode == null )
                        {
                            _curHeaderNode.TopNode = curNode;
                        }
                    }
                    if ( _skipFirstGroupHeader )
                    {
                        _skipFirstGroupHeader = false;
                    }
                    else
                    {
                        _onHeaderNode = true;
                    }
                }
            }

            return true;
        }

        public void Reset()
        {
            _baseEnumerator.Reset();
        }

        public object Current
        {
            get
            {
                if ( _onHeaderNode )
                {
                    return _curHeaderNode;
                }
                return _baseEnumerator.Current;
            }
        }

        public IViewNode CurrentNode
        {
            get { return (IViewNode) Current; }
        }

        private bool SkipCollapsedGroup()
        {
            while( _baseEnumerator.MoveNext() )
            {
                JetListViewNode groupNode = (JetListViewNode) _baseEnumerator.Current;
                if ( groupNode.Level == 0 )
                {
                    string groupName = _groupCollection.GroupProvider.GetGroupName( groupNode.Data );
                    if ( groupName != _curHeaderNode.Text )
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
