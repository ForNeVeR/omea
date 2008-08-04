/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections;
using System.Diagnostics;
using JetBrains.DataStructures;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceStore;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Fills ResourceListView2 with data from a threaded resource list.
	/// </summary>
	public class ConversationDataProvider: ResourceListDataProvider
	{
        private class ConversationNode
        {
            internal readonly IResource Resource;

            internal JetListViewNode LvNode;
            internal ConversationNode Parent;
            internal ArrayList Children;
            internal bool InList;

            public ConversationNode( IResource resource )
            {
                Resource = resource;
            }

            internal void AddChild( ConversationNode node )
            {
                lock( this )
                {
                    if ( Children == null )
                    {
                        Children = new ArrayList();
                    }
                    Debug.Assert( !HasChild( node ) );
                    Children.Add( node );
                }
                node.Parent = this;
            }

            internal bool HasChild( ConversationNode node )
            {
                lock( this )
                {
                    if ( Children != null )
                    {
                        foreach( ConversationNode existingNode in Children )
                        {
                            if ( existingNode.Resource.Id == node.Resource.Id )
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            internal bool HasUnreadReplies()
            {
                lock( this )
                {
                    if ( Children != null )
                    {
                        foreach( ConversationNode node in Children )
                        {
                            if ( node.InList && node.Resource.HasProp( Core.Props.IsUnread ) )
                            {
                                return true;
                            }
                            if ( node.HasUnreadReplies() )
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            public void RemoveChild( ConversationNode node )
            {
                lock( this )
                {
                    if ( Children != null )
                    {
                        Children.Remove( node );
                    }
                }
            }
        }

        private class ConversationNodeComparer: IComparer
        {
            private readonly ResourceComparer _resourceComparer;

            internal ConversationNodeComparer( ResourceComparer comparer )
            {
                _resourceComparer = comparer;
            }
            
            public int Compare( object x, object y )
            {
                ConversationNode lhs = x as ConversationNode;
                ConversationNode rhs = y as ConversationNode;
                if ( lhs == null )
                {
                    return (rhs == null) ? 0 : -1;
                }
                if ( rhs == null )
                {
                    return 1;
                }
                return _resourceComparer.CompareResources( lhs.Resource, rhs.Resource );
            }
        }

        private readonly IResourceThreadingHandler _threadingHandler;
        private readonly ArrayList _conversationRoots = new ArrayList();            // <ConversationNode>
        private readonly IComparer _childComparer;
        private IntHashTable _conversationNodeMap;                         // resource ID -> ConversationNode
	    private JetListViewNode _lastExpandingNode;

	    public ConversationDataProvider( IResourceList resourceList, IResourceThreadingHandler threadingHandler )
            : base( resourceList )
        {
            _threadingHandler = threadingHandler;
            _childComparer = new ResourceComparer( resourceList, new SortSettings( Core.Props.Date, true ), true );
        }

	    protected override void AddResourceNodes()
	    {
            _conversationNodeMap = new IntHashTable();
            foreach( IResource res in _resourceList.ValidResources )
            {
                ConversationNode node = GetConversationNode( res );
                node.InList = true;
            }

            ArrayList topLevelNodes = ArrayListPool.Alloc();
            try 
            {
                foreach( ConversationNode node in _conversationRoots )
                {
                    FillTopLevelNodes( topLevelNodes, node );
                }
                if ( _lastComparer != null )
                {
                    topLevelNodes.Sort( new ConversationNodeComparer( _lastComparer ) );
                }

                foreach( ConversationNode node in topLevelNodes )
                {
                    JetListViewNode lvNode = AddListViewNode( _listView.Nodes, node );
                    if ( node.Children != null || _threadingHandler.CanExpandThread( node.Resource, ThreadExpandReason.Expand ) )
                    {
                        lvNode.HasChildren = true;                     
                    }
                }
            }
            finally
            {
                ArrayListPool.Dispose( topLevelNodes );
            }

            _listView.ChildrenRequested += HandleChildrenRequested;
            _listView.NodeCollection.NodeExpandChanging += HandleExpandChanging;
        }

	    public override void Dispose()
	    {
	        if ( _listView != null )
	        {
                _listView.ChildrenRequested -= HandleChildrenRequested;
                _listView.NodeCollection.NodeExpandChanging -= HandleExpandChanging;
            }
            base.Dispose();
	    }

	    private static JetListViewNode AddListViewNode( ChildNodeCollection nodes, ConversationNode node )
        {
            JetListViewNode lvNode = nodes.Add( node.Resource );
            node.LvNode = lvNode;
            return node.LvNode;
        }

        /// <summary>
        /// Finds or adds a conversation node for the specified resource.
        /// </summary>
        /// <param name="res">The resource to find the node for.</param>
        /// <returns>The conversation node instance.</returns>
        private ConversationNode GetConversationNode( IResource res )
        {
            lock( _conversationNodeMap )
            {
                ConversationNode node = (ConversationNode) _conversationNodeMap [res.Id];
                if ( node != null )
                {
                    return node;
                }

                node = new ConversationNode( res );

                IResource parent = _threadingHandler.GetThreadParent( res );
                if ( parent != null )
                {
                    ConversationNode parentNode = GetConversationNode( parent );
                    parentNode.AddChild( node );
                }
                else
                {
                    _conversationRoots.Add( node );
                }

                _conversationNodeMap [res.Id] = node;
                return node;
            }
        }

        private ConversationNode FindConversationNode( int resId )
        {
            if ( _conversationNodeMap == null )
            {
                return null;
            }
            lock( _conversationNodeMap )
            {
                return (ConversationNode) _conversationNodeMap [resId];
            }
        }

        /// <summary>
        /// Adds the node (if it's present in the list) or some of its child nodes
        /// (if it is not) to the tree of conversations.
        /// </summary>
        /// <param name="topLevelNodes"></param>
        /// <param name="node"></param>
        private static void FillTopLevelNodes( ArrayList topLevelNodes, ConversationNode node )
        {
            lock( node )
            {
                if ( node.InList )
                {
                    topLevelNodes.Add( node );
                }
                else if ( node.Children != null )
                {
                    foreach( ConversationNode child in node.Children )
                    {
                        FillTopLevelNodes( topLevelNodes, child );
                    }
                }
            }
        }

        private void HandleExpandChanging( object sender, JetListViewNodeEventArgs e )
        {
            if ( !e.Node.Expanded )
            {
                _lastExpandingNode = e.Node;
            }
        }

        private void HandleChildrenRequested( object sender, RequestChildrenEventArgs e )
	    {
            if ( _listView == null )
            {
                return;
            }

            IResource res = (IResource) e.Node.Data;
            e.Handled = _threadingHandler.HandleThreadExpand( res, 
                (e.Reason == RequestChildrenReason.Enumerate 
                    ? ThreadExpandReason.Enumerate : ThreadExpandReason.Expand ) );
                    
	        BuildConversation( res, (e.Node == _lastExpandingNode && e.Node.Level == 0 ) );
	    }

	    public void ExpandConversation( IResource res )
	    {
	        BuildConversation( res, true );
	    }

	    private void BuildConversation( IResource res, bool expandNode )
	    {
	        Guard.NullArgument( res, "res" );
            Guard.NullMember( _listView, "_listView" );
            ConversationNode node = FindConversationNode( res.Id );
	        if ( node != null )
            {
                while( node.LvNode == null )
                {
                    if ( node.Parent == null )
                    {
                        return;
                    }
                    node = node.Parent;
                }
                if ( node.LvNode.Nodes.Count == 0 )
                {
                    _listView.NodeCollection.SetItemComparer( node.Resource, _childComparer );
                    AddConversationReplies( node, node.LvNode );
                }
	            if ( expandNode )
	            {
	                node.LvNode.ExpandAll();	            
	            }
	        }
	    }

	    private void AddConversationReplies( ConversationNode convNode, JetListViewNode node )
	    {
            lock( convNode )
            {
                if ( convNode.Children != null )
                {
                    foreach( ConversationNode child in convNode.Children )
                    {
                        AddConversationRecursive( child, node );
                    }
                }
            }
        }

        private void AddConversationRecursive( ConversationNode child, JetListViewNode node )
        {
            if ( child.LvNode != null )
            {
                node = child.LvNode;
            }
            else if ( child.InList )
            {
                node = AddListViewNode( node.Nodes, child );
            }

            AddConversationReplies( child, node );
        }

        protected override void HandleResourceAdded( object sender, ResourceIndexEventArgs e )
	    {
            if ( _disposed )
            {
                return;
            }

            lock( _resourceList )
            {
                ConversationNode newNode = GetConversationNode( e.Resource );
                newNode.InList = true;

                foreach( IResource child in _threadingHandler.GetThreadChildren( e.Resource ) )
                {
                    ConversationNode childNode = FindConversationNode( child.Id );
                    if ( childNode != null && !newNode.HasChild( childNode ) )
                    {
                        newNode.AddChild( childNode );
                    }
                }

                if ( _threadingHandler.GetThreadParent( e.Resource ) == null )
                {
                    OnThreadRootAdded( e.Resource, newNode );
                }
                else
                {
                    // if the parent is not in list, the node should be added as a root (#6376)
                    if ( !OnThreadChildAdded( e.Resource, newNode ) )
                    {
                        OnThreadRootAdded( e.Resource, newNode );
                    }
                }
            }
    

            OnResourceCountChanged();
	    }

	    private void OnThreadRootAdded( IResource resource, ConversationNode node )
	    {
            JetListViewNode lvNode = null;
            if ( node.LvNode == null )
            {
                lvNode = AddListViewNode( _listView.Nodes, node );
            }
            RemoveChildRoots( resource );
            if ( lvNode != null && ( node.Children != null || _threadingHandler.CanExpandThread( node.Resource, ThreadExpandReason.Expand ) ) )
            {
                lvNode.HasChildren = true;                     
            }
	    }

        /// <summary>
        /// If the specified root resource of a conversation was added to the list 
        /// later than its children, the children appeared as "intermediate" roots.
        /// Now that we have the real root, the "intermediate" roots need to be
        /// removed.
        /// </summary>
        /// <param name="res"></param>
        private void RemoveChildRoots( IResource res )
        {
            foreach( IResource reply in _threadingHandler.GetThreadChildren( res ) )
            {
                RemoveChildRoots( reply );
                ConversationNode node = FindConversationNode( reply.Id );
                if ( node != null && node.LvNode != null )
                {
                    RemoveLvNode( node );
                }
            }
        }

	    private bool OnThreadChildAdded( IResource resource, ConversationNode node )
	    {
            ConversationNode parentNode = FindParentInList( resource, true );
            if ( parentNode != null && node.LvNode == null )
            {
                JetListViewNode parentLvNode = parentNode.LvNode;
                if ( parentLvNode.Level == 0 && parentLvNode.Nodes.Count == 0 )
                {
                    // for a root thread which has not yet been expanded, 
                    // only mark that it has children
                    parentLvNode.HasChildren = true;
                }
                else
                {
                    AddListViewNode( parentLvNode.Nodes, node );
                    parentLvNode.Expanded = true;
                }
                return true;
            }
            return false;
        }

        private ConversationNode FindParentInList( IResource resource, bool needLvNode )
        {
            IResource curRes = resource;
            // if some of the resources in a thread are skipped (for example, when
            // we're viewing only unread items and the thread goes unread->read->unread),
            // go up the thread until we find a node that is not skipped
            while( true )
            {
                IResource parentRes = _threadingHandler.GetThreadParent( curRes );
                if ( parentRes == null )
                {
                    return null;
                }
                ConversationNode parentNode = FindConversationNode( parentRes.Id );
                if ( parentNode != null && parentNode.InList && ( !needLvNode || parentNode.LvNode != null ) )
                {
                    return parentNode;
                }
                curRes = parentRes;
            }
        }

	    protected override void HandleResourceChanged( object sender, ResourcePropIndexEventArgs e )
	    {
            if ( _disposed )
            {
                return;
            }

            lock( _resourceList )
            {
                ConversationNode node = FindConversationNode( e.Resource.Id );
                if ( _threadingHandler.IsThreadChanged( e.Resource, e.ChangeSet ) )
                {
                    if ( node != null && node.LvNode != null )
                    {
                        UpdateItemThread( e.Resource, node );
                    }
                }
                else if ( node != null )  // if we use a live snapshot list, it's possible to get 
                    // ResourceChanged notifications for nodes which weren't included 
                    // in predicate GetMatchingResources() output (OM-8711)
                {
                    _listView.UpdateItemSafe( e.Resource );
                    IResource lastCollapsedParent = null;
                    node = node.Parent;
                    while( node != null )
                    {
                        if ( node.LvNode != null && !node.LvNode.Expanded )
                        {
                            lastCollapsedParent = (IResource) node.LvNode.Data;
                        }
                        node = node.Parent;
                    }
                    if ( lastCollapsedParent != null )
                    {
                        _listView.UpdateItemSafe( lastCollapsedParent );
                    }
                }
            }
	    }

	    private void UpdateItemThread( IResource resource, ConversationNode node )
	    {
	        ConversationNode newParentNode = FindParentInList( resource, false );
	        if ( newParentNode != null )
	        {
                if ( node.Parent != null )
	            {
	                node.Parent.RemoveChild( node );
	            }
	            newParentNode.AddChild( node );
	            if ( newParentNode.LvNode == null )
	            {
	                // the new parent belongs to a thread which wasn't expanded
                    _listView.Nodes.Remove( resource );
	                node.LvNode = null;
	            }
	            else
	            {
	                CollapseState oldState = newParentNode.LvNode.CollapseState;
	                node.LvNode.SetParent( newParentNode.LvNode );
	                if ( oldState == CollapseState.NoChildren )
	                {
	                    newParentNode.LvNode.Expanded = false;
	                }
	            }
	        }
	        else
	        {
	            node.LvNode.SetParent( null );
	        }
	    }

	    protected override void HandleResourceDeleting( object sender, ResourceIndexEventArgs e )
	    {
            if ( _disposed || e.Resource == null )
            {
                return;
            }

	        lock( _resourceList )
	        {
                ConversationNode node = FindConversationNode( e.Resource.Id );
                if ( node != null )
                {
                    if ( node.LvNode != null )
                    {
                        lock( _listView.NodeCollection )
                        {
                            for( int i=node.LvNode.Nodes.Count-1; i >= 0; i-- )
                            {
                                node.LvNode.Nodes [i].SetParent( node.LvNode.Parent );
                            }
                            RemoveLvNode( node );
                        }
                    }

                    ConversationNode parent = node.Parent;
                    if ( parent != null )
                    {
                        if ( parent.Children != null && parent.Children.Count == 0 &&
                            !_threadingHandler.CanExpandThread( parent.Resource, ThreadExpandReason.Expand ) )
                        {
                            parent.LvNode.HasChildren = false;
                        }
                        node.Parent.RemoveChild( node );
                    }
                    lock( _conversationNodeMap )
                    {
                        _conversationNodeMap.Remove( e.Resource.Id );                    
                    }
                }
	        }

            OnResourceCountChanged();
	    }

	    private static void RemoveLvNode( ConversationNode node )
	    {
	        if ( node.LvNode != null )
	        {
                JetListViewNode parentLvNode = node.LvNode.Parent;
                if ( parentLvNode != null )
	            {
	                parentLvNode.Nodes.Remove( node.Resource );
	            }
	            node.LvNode = null;
	        }
	    }

        public override bool FindResourceNode( IResource res )
	    {
            if ( !base.FindResourceNode( res ) )
            {
                return false;
            }

            ConversationNode convNode = FindConversationNode( res.Id );
            if ( convNode == null )
            {
                return false;
            }

            if ( convNode.LvNode == null )
            {
                ConversationNode parent = convNode.Parent;
                while( parent != null )
                {
                    if ( parent.LvNode != null && parent.LvNode.Level == 0 )
                    {
                        ExpandConversation( parent.Resource );
                        break;
                    }
                    parent = parent.Parent;
                }
            }

            return true;
	    }

        public IResourceList ExpandSelectedResources( IResourceList selection )
        {
            IntArrayList resourceIds = new IntArrayList();
            resourceIds.AddRange( selection.ResourceIds );
            for( int i=0; i<selection.Count; i++ )
            {
                ConversationNode node = FindConversationNode( selection.ResourceIds [i] );
                if ( node != null && node.LvNode != null && node.LvNode.CollapseState == CollapseState.Collapsed )
                {
                    CollectResourcesRecursive( node, resourceIds );
                }
            }
            return Core.ResourceStore.ListFromIds( resourceIds, false );
        }

        private static void CollectResourcesRecursive( ConversationNode convNode, IntArrayList resourceIds )
        {
            resourceIds.Add( convNode.Resource.Id );
            lock( convNode )
            {
                if ( convNode.Children != null )
                {
                    foreach( ConversationNode childNode in convNode.Children )
                    {
                        CollectResourcesRecursive( childNode, resourceIds );
                    }
                }
            }
        }

	    internal bool ResourceHasUnreadReplies( IResource res )
	    {
	        ConversationNode node = FindConversationNode( res.Id );
            if ( node != null )
            {
                return node.HasUnreadReplies();
            }
            return false;
	    }
	}
}
