/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;

namespace JetBrains.JetListViewLibrary
{
    internal interface IPagingProvider
    {
        IViewNode MoveByPage( IViewNode startNode, MoveDirection direction );
    }
    
    /// <summary>
	/// Manages the selection state and selection actions of items in JetListView.
	/// </summary>
	public abstract class SelectionModel: IEnumerable
	{
		private JetListViewNodeCollection _nodeCollection;
        private IVisibleNodeCollection _visibleNodeCollection;
        private IViewNode _focusNode;
        private IViewNode _activeNode;
        private IViewNode _selectionStartNode;
        private bool _processedMouseDown;
        private IPagingProvider _pagingProvider;
        private IViewNode _lastRemovedNode;

        internal SelectionModel( JetListViewNodeCollection nodeCollection )
	    {
	        _nodeCollection = nodeCollection;
            _nodeCollection.MultipleNodesChanged += new MultipleNodesChangedEventHandler( HandleMultipleNodesChanged );
            VisibleNodeCollection = _nodeCollection;
	    }

        internal IPagingProvider PagingProvider
        {
            get { return _pagingProvider; }
            set { _pagingProvider = value; }
        }

        internal IVisibleNodeCollection VisibleNodeCollection
        {
            get { return _visibleNodeCollection; }
            set
            {
                if ( _visibleNodeCollection != value )
                {
                    if ( _visibleNodeCollection != null )
                    {
                        _visibleNodeCollection.NodesCollapsed -= new EventHandler( HandleNodesCollapsed );
                        _visibleNodeCollection.ViewNodeRemoving -= new ViewNodeEventHandler( HandleNodeRemoving );
                    }
                    _visibleNodeCollection = value;
                    if ( _visibleNodeCollection != null )
                    {
                        _visibleNodeCollection.NodesCollapsed += new EventHandler( HandleNodesCollapsed );
                        _visibleNodeCollection.ViewNodeRemoving += new ViewNodeEventHandler( HandleNodeRemoving );
                    }
                }
            }
        }

        internal IViewNode FocusViewNode
        {
            get { return _focusNode; }
        }

        internal JetListViewNode FocusNode
        {
            get { return _focusNode as JetListViewNode; }
        }

        public JetListViewNode ActiveNode
        {
            get { return _activeNode as JetListViewNode; }
        }

        public abstract int Count
        {
            get;
        }

        public object this [int index]
        {
            get
            {
                lock( SelectionLock )
                {
                    IEnumerator enumerator = GetEnumerator();
                    for( int i=0; i<=index; i++ )
                    {
                        if ( !enumerator.MoveNext() )
                        {
                            throw new IndexOutOfRangeException( "The selection index " + index + " is out of range; selection count = " + Count );
                        }
                    }
                    return enumerator.Current;
                }
            }
        }

        internal abstract IEnumerable SelectedNodes
        {
            get;
        }

        public abstract IEnumerator GetEnumerator();

        internal MouseHandleResult HandleMouseDown( IViewNode itemNode, Keys modifiers )
	    {
            MouseHandleResult result = 0;
            
            _processedMouseDown = false;
            if ( itemNode == null )
            {
                return result;
            }

            if ( (modifiers & Keys.Shift) != 0 )
            {
                ProcessShiftClick( itemNode, ((modifiers & Keys.Control) != 0) );
            }
            else if ( (modifiers & Keys.Control) != 0 )
            {
                ProcessControlClick( itemNode );
            }
            else
            {
                if ( IsSingleNodeSelected( itemNode ) && FocusNode == itemNode )
                {
                    result |= MouseHandleResult.MayInPlaceEdit;
                }
                ProcessClick( itemNode );
            }

            if ( itemNode != null )
            {
                SetFocusNode( itemNode );
            }
            return result;
	    }

        internal void HandleMouseUp( IViewNode itemNode, Keys modifiers )
        {
            if ( itemNode != null )
            {
                if ( modifiers == Keys.Control )
                {
                    if ( IsNodeSelected( itemNode ) && !_processedMouseDown )
                    {
                        UnselectNode( itemNode );
                    }
                    
                }
                else if ( modifiers == Keys.None )
                {
                    ProcessMouseUp( itemNode );
                }
            }
        }

        private void ProcessClick( IViewNode itemNode )
        {
            if ( !IsNodeSelected( itemNode ) )
            {
                ClearSelection();
                SelectNode( itemNode );
            }
            else
            {
                SetFocusNode( itemNode );
            }
            _selectionStartNode = itemNode;
        }

        private void ProcessMouseUp( IViewNode node )
        {
            if ( !IsSingleNodeSelected( node ) )
            {
                IViewNode[] selNodes = SelectionToArray();
                for( int i=0; i<selNodes.Length; i++ )
                {
                    if ( selNodes [i] != node )
                    {
                        UnselectNode( selNodes [i] );                        
                    }
                }
            }
        }

        private void ProcessControlClick( IViewNode itemNode )
        {
            _selectionStartNode = itemNode;
            if ( !IsNodeSelected( itemNode ) )
            {
                SelectNode( itemNode );
                _processedMouseDown = true;
            }
        }

        private void ProcessShiftClick( IViewNode itemNode, bool ctrlPressed )
        {
            if ( _selectionStartNode == null || _selectionStartNode == itemNode )
            {
                ProcessClick( itemNode );
                return;
            }

            SelectItemRange( _selectionStartNode, itemNode, !ctrlPressed );
        }

        private void SelectItemRange( IViewNode startNode, IViewNode endNode, bool clearSelection )
        {
            HashSet oldSelectedItems = null;
            if ( clearSelection )
            {
                oldSelectedItems = new HashSet( SelectionToArray() );
            }

            lock( _nodeCollection )
            {
                MoveDirection moveDirection = _visibleNodeCollection.GetMoveDirection( startNode, endNode );
                IEnumerator enumerator = _visibleNodeCollection.GetDirectionalEnumerator( startNode, moveDirection );

                do
                {
                    if ( !enumerator.MoveNext() )
                        throw new Exception( "Internal error: enumerator state inconsistency" );
                    SelectNode( (IViewNode) enumerator.Current );
                    if ( clearSelection )
                    {
                        oldSelectedItems.Remove( enumerator.Current );
                    }
                } while( (IViewNode) enumerator.Current != endNode );
            }

            if ( clearSelection )
            {
                foreach( HashSet.Entry e in oldSelectedItems )
                {
                    UnselectNode( (IViewNode) e.Key );
                }
            }
        }

        public void Clear()
        {
            ClearSelection();
            SetFocusNode( null );
            _selectionStartNode = null;
        }

        /// <summary>
        /// Adds the specified item to the selection if it is present in the list of items
        /// in the control.
        /// </summary>
        /// <param name="item">The item to select.</param>
        /// <returns>true if the item was added to selection, false if it is not present in the control.</returns>
        public bool AddIfPresent( object item )
        {
            if ( item == null )
                throw new ArgumentNullException( "item" );

            JetListViewNode node = _nodeCollection.NodeFromItem( item );
            if ( node != null )
            {
                AddSelectedNode( node );
                return true;
            }
            return false;
        }

        public void Add( object item )
        {
            if ( item == null )
                throw new ArgumentNullException( "item" );

            JetListViewNode node = _nodeCollection.NodeFromItem( item );
            if ( node == null )
            {
                throw new ArgumentException( "Item is not displayed in the control", "item" );
            }
            AddSelectedNode( node );
        }

        private void AddSelectedNode( JetListViewNode node )
        {
            _visibleNodeCollection.EnsureNodeVisible( node );
            SelectNode( node );
            if ( _focusNode == null )
            {
                SetFocusNode( node );
            }
            if ( _selectionStartNode == null )
            {
                _selectionStartNode = node;
            }
        }

        public void SelectSingleItem( object item )
        {
            Guard.NullArgument( item, "item" );
            JetListViewNode node = _nodeCollection.NodeFromItem( item );
            if ( node == null )
            {
                throw new ArgumentException( "Item is not displayed in the control", "item" );
            }
            SelectAndFocusNode( node );
        }

        public void Remove( object item )
        {
            JetListViewNode[] nodes = _nodeCollection.NodesFromItem( item );
            if ( nodes.Length == 0 )
            {
                throw new ArgumentException( "Item is not displayed in the control", "item" );
            }
            for( int i=0; i<nodes.Length; i++ )
            {
                if ( _focusNode == nodes [i] )
                {
                    SetFocusNode( null );
                }
                UnselectNode( nodes [i] );
            }
        }

        /// <summary>
        /// Checks if any of the nodes displaying the specified item is selected.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>true if the item is selected, false otherwise.</returns>
        public bool Contains( object item )
        {
            JetListViewNode[] nodes = _nodeCollection.NodesFromItem( item );
            if ( nodes.Length == 0 )
            {
                throw new ArgumentException( "Item is not displayed in the control", "item" );
            }
            for( int i=0; i<nodes.Length; i++ )
            {
                if ( IsNodeSelected( nodes [i] ) )
                    return true;
            }
            return false;
        }

        public abstract object SelectionLock { get; }
        internal abstract void ClearSelection();
        internal abstract void SelectNode( IViewNode node );
        internal abstract bool UnselectNode( IViewNode node );
        internal abstract bool IsNodeSelected( IViewNode node );
        internal abstract IViewNode[] SelectionToArray();

        internal event ViewNodeStateChangeEventHandler SelectionStateChanged;
        internal event ViewNodeStateChangeEventHandler FocusStateChanged;
        internal event ViewNodeEventHandler ActiveNodeChanged;

        public void OnSelectionStateChanged( IViewNode node, bool isSelected )
        {
            if ( SelectionStateChanged != null )
            {
                SelectionStateChanged( this, new ViewNodeStateChangeEventArgs( node, isSelected ) );
            }
        }

        private void OnFocusStateChanged( IViewNode node, bool isFocused )
        {
            if ( FocusStateChanged != null )
            {
                FocusStateChanged( this, new ViewNodeStateChangeEventArgs( node, isFocused ) );
            }
        }

        internal bool IsNodeFocused( IViewNode node )
        {
            return _focusNode == node;
        }

        private void SetFocusNode( IViewNode node )
        {
            if ( node != _focusNode )
            {
                IViewNode oldFocusNode = _focusNode;
                _focusNode = node;
                if ( oldFocusNode != null )
                {
                     OnFocusStateChanged( oldFocusNode, false );
                }
                if ( _focusNode != null )
                {
                    OnFocusStateChanged( _focusNode, true );
                    if ( IsNodeSelected( _focusNode ) )
                    {
                        SetActiveNode( _focusNode );
                    }
                }
                else
                {
                    SetActiveNode( null );
                }
            }
        }

        private void SetActiveNode( IViewNode node )
        {
            _activeNode = node;
            OnActiveNodeChanged( _activeNode );
        }

        private void OnActiveNodeChanged( IViewNode node )
        {
            if ( ActiveNodeChanged != null )
            {
                ActiveNodeChanged( this, new ViewNodeEventArgs( node ) );
            }
        }

        internal bool HandleKeyDown( Keys keys )
        {
            Keys modifiers = Keys.None;
            if ( (keys & Keys.Shift) != 0 )
            {
                modifiers |= Keys.Shift;
                keys &= ~Keys.Shift;
            }
            if ( (keys & Keys.Control) != 0 )
            {
                modifiers |= Keys.Control;
                keys &= ~Keys.Control;
            }

            switch( keys )
            {
                case Keys.Down:
                    MoveSelection( MoveDirection.Down, modifiers );
                    return true;

                case Keys.Up:
                    MoveSelection( MoveDirection.Up, modifiers );
                    return true;

                case Keys.PageDown:
                    MoveSelectionByPage( MoveDirection.Down, modifiers );
                    return true;

                case Keys.PageUp:
                    MoveSelectionByPage( MoveDirection.Up, modifiers );
                    return true;

                case Keys.End:
                    IViewNode lastVisibleNode = _visibleNodeCollection.LastVisibleViewNode;
                    if ( lastVisibleNode != null )
                    {
                        MoveSelectionTo( lastVisibleNode, modifiers & ~Keys.Control );
                    }
                    return true;

                case Keys.Home:
                    IViewNode firstVisibleNode = _visibleNodeCollection.FirstVisibleViewNode;
                    if ( firstVisibleNode != null )
                    {
                        MoveSelectionTo( firstVisibleNode, modifiers & ~Keys.Control );
                    }
                    return true;

                case Keys.Space:
                    ProcessSpaceKey();
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Moves the selection up by a single item.
        /// </summary>
        public void MoveUp()
        {
            MoveSelection( MoveDirection.Up, Keys.None );
        }

        /// <summary>
        /// Moves the selection down by a single item.
        /// </summary>
        public void MoveDown()
        {
            MoveSelection( MoveDirection.Down, Keys.None );            
        }

        private void MoveSelection( MoveDirection direction, Keys modifiers )
        {
            IViewNode destNode;
            lock( _nodeCollection )
            {
                IVisibleNodeEnumerator enumerator;
                if ( _focusNode == null )
                {
                    enumerator = _visibleNodeCollection.GetFullEnumerator();
                    if ( !enumerator.MoveNext() )
                        return;
                }
                else
                {
                    enumerator = _visibleNodeCollection.GetDirectionalEnumerator( _focusNode, direction );
                    enumerator.MoveNext();
                    if ( !enumerator.MoveNext() )
                        return;
                }
            
                destNode = enumerator.CurrentNode;
                
                // skip group headers
                if ( destNode is GroupHeaderNode )
                {
                    if ( enumerator.MoveNext() && enumerator.CurrentNode is JetListViewNode )
                    {
                        destNode = enumerator.CurrentNode;
                    }
                }
                MoveSelectionTo( destNode, modifiers );
            }
        }

        private void MoveSelectionByPage( MoveDirection direction, Keys modifiers )
        {
            if ( _focusNode == null || _pagingProvider == null )
            {
                MoveSelection( direction, Keys.None );
                return;
            }

            IViewNode destNode = _pagingProvider.MoveByPage( _focusNode, direction );
            MoveSelectionTo( destNode, modifiers );
        }

        private void MoveSelectionTo( IViewNode destNode, Keys modifiers )
        {
            if ( (modifiers & Keys.Shift ) != 0 && _selectionStartNode != null )
            {
                SelectItemRange( _selectionStartNode, destNode, true );
                SetFocusNode( destNode );
            }
            else if ( (modifiers & Keys.Control) != 0 )
            {
                SetFocusNode( destNode );
            }
            else
            {
                SelectAndFocusNode( destNode );    
            }
        }

        internal void SelectAndFocusNode( IViewNode destNode )
        {
            if ( !IsSingleNodeSelected( destNode ) )
            {
                ClearSelection();
                SelectNode( destNode );
                SetFocusNode( destNode );
            }
            _selectionStartNode = destNode;
        }

        private bool IsSingleNodeSelected( IViewNode node )
        {
            return Count == 1 && IsNodeSelected( node ) && _focusNode == node;
        }

        private void ProcessSpaceKey()
        {
            if ( _focusNode != null )
            {
                if ( IsNodeSelected( _focusNode ) )
                {
                    UnselectNode( _focusNode );
                }
                else
                {
                    SelectNode( _focusNode );
                }
            }
        }

        private void HandleNodeRemoving( object sender, ViewNodeEventArgs e )
        {
            if ( _selectionStartNode == e.ViewNode )
            {
                _selectionStartNode = null;
            }
            RemoveNodeFromSelection( e.ViewNode );
        }

        private void RemoveNodeFromSelection( IViewNode viewNode )
        {
            if ( ( UnselectNode( viewNode ) && Count == 0 ) || viewNode == _focusNode )
            {
                IViewNode nextNode = null, prevNode = null;
                IVisibleNodeEnumerator enumerator = _visibleNodeCollection.GetDirectionalEnumerator( viewNode, MoveDirection.Down );
                enumerator.MoveNext();
                // make sure we don't move the selection to the node being deleted if we delete
                // the node and its group in one VisibleNodeRemoving operation
                if ( enumerator.MoveNext() && enumerator.CurrentNode != _lastRemovedNode )
                {
                    nextNode = enumerator.CurrentNode;
                }

                enumerator = _visibleNodeCollection.GetDirectionalEnumerator( viewNode, MoveDirection.Up );
                enumerator.MoveNext();
                if ( enumerator.MoveNext() && enumerator.CurrentNode != _lastRemovedNode )
                {
                    prevNode = enumerator.CurrentNode;
                }
            
                if ( nextNode == null )
                {
                    if ( prevNode != null )
                    {
                        SelectAndFocusNode( prevNode );
                    }
                    else
                    {
                        SetFocusNode( null );
                    }
                }
                else if ( prevNode != null && viewNode is JetListViewNode )
                {
                    JetListViewNode prevLvNode = prevNode as JetListViewNode;
                    JetListViewNode nextLvNode = nextNode as JetListViewNode;
                    int removedNodeLevel = (viewNode as JetListViewNode).Level;
                    if ( prevLvNode != null && nextLvNode != null && 
                        prevLvNode.Level == removedNodeLevel && nextLvNode.Level != removedNodeLevel )
                    {
                        SelectAndFocusNode( prevNode );
                    }
                    else if ( prevLvNode != null && nextLvNode != null &&
                        prevLvNode.Level != removedNodeLevel && nextLvNode.Level == removedNodeLevel )
                    {
                        SelectAndFocusNode( nextNode );
                    }
                    else if ( prevNode == (viewNode as JetListViewNode).Parent )
                    {
                        SelectAndFocusNode( prevNode );
                    }
                    else if ( prevLvNode != null && nextLvNode == null )
                    {
                        SelectAndFocusNode( prevNode );
                    }
                    else
                    {
                        SelectAndFocusNode( nextNode );
                    }
                }
                else
                {
                    SelectAndFocusNode( nextNode );
                }
            }
            _lastRemovedNode = viewNode;
        }

        private void HandleMultipleNodesChanged( object sender, MultipleNodesChangedEventArgs e )
        {
            // ensure correct lock ordering - lock node collection before selection
            lock( _nodeCollection )
            {
                lock( SelectionLock )
                {
                    if ( Count > 0 )
                    {
                        if ( _nodeCollection.IsEmpty )
                        {
                            Clear();
                        }
                        else
                        {
                            IViewNode[] selNodes = SelectionToArray();
                            foreach( IViewNode selNode in selNodes )
                            {
                                if ( !_visibleNodeCollection.IsNodeVisible( selNode ) )
                                {
                                    UnselectNode( selNode );
                                }
                            }
                
                            if ( Count == 0 )
                            {
                                IVisibleNodeEnumerator enumerator = _visibleNodeCollection.GetFullEnumerator();
                                if ( enumerator.MoveNext() )
                                {
                                    SelectAndFocusNode( enumerator.CurrentNode );
                                }
                            }
                        }
                    }
                }
            }
        }

        private void HandleNodesCollapsed( object sender, EventArgs e )
        {
            if ( Count > 0 )
            {
                IViewNode[] selNodes = SelectionToArray();
                foreach( IViewNode node in selNodes )
                {
                    MoveSelectionToVisibleParent( node, true );
                }
            }
            if ( _focusNode != null )
            {
                MoveSelectionToVisibleParent( _focusNode, false );
            }
            if ( _selectionStartNode != null )
            {
                _selectionStartNode = _visibleNodeCollection.GetVisibleParent( _selectionStartNode );
            }
        }

        private void MoveSelectionToVisibleParent( IViewNode node, bool select )
        {
            IViewNode visibleParent = _visibleNodeCollection.GetVisibleParent( node );

            if ( visibleParent != null )
            {
                if ( select )
                {
                    UnselectNode( node );
                    SelectNode( visibleParent );
                }
                else
                {
                    SetFocusNode( visibleParent );
                }
            }
        }

        public void SelectAll()
        {
            if ( _nodeCollection.VisibleItemCount > 0 )
            {
                IEnumerator enumerator = _nodeCollection.EnumerateNodesForward( _nodeCollection.Nodes [0] );
                while( enumerator.MoveNext() )
                {
                    SelectNode( (IViewNode) enumerator.Current );
                }
            }
        }
	}
}
