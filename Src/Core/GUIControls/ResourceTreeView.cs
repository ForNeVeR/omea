// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.Containers;
using JetBrains.UI.Components.CustomTreeView;
using JetBrains.UI.Components.RichTextTreeView;
using JetBrains.UI.RichText;
using JetBrains.UI.Components.TreeSearchWindow;
using JetBrains.UI.Interop;
using JetBrains.DataStructures;
using TreeViewItemFlags = JetBrains.UI.Interop.TreeViewItemFlags;
using TVITEM = JetBrains.UI.Interop.TVITEM;

namespace JetBrains.Omea.GUIControls
{
	/**
     * A tree view displaying a resource in each of its nodes.
     */

    public class ResourceTreeView: CustomTreeView
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private IResource           _rootResource;
        private int                 _parentProperty    = -1;
        private int                 _openProperty      = -1;
        private int                 _checkedProperty   = -1;
        private int                 _checkedSetValue   = 1;
        private int                 _checkedUnsetValue = 0;
        private ArrayList           _nodeDecorators = new ArrayList();
        private ArrayList           _nodeFilters = new ArrayList();
        private RichTextNodePainter _nodePainter;
        private SearchWindow        _searchWindow;
        private bool                _showContextMenu = true;
        private object              _menuContextInstance;
        private IResource           _resourceToSelect = null;
        private TreeNode            _dropHighlightNode = null;
        private bool                _delaySaveChecked = false;
        private bool                _dropOnEmpty = false;
        private bool                _selectAddedItems = false;
        private bool                _uniqueResources = true;
        private HashSet             _expandingNodes = new HashSet();
        private Timer               _dragScrollTimer;
        private bool                _dragScrollUp;
        private Timer               _expandTimer;
        private bool                _executeDoubleClickAction = true;
        private ArrayList           _postponedUpdateNodeData = new ArrayList();
        private IntArrayList        _postponedRemoveNodeData = new IntArrayList();
        private bool                _showRootResource = false;
        private bool                _expandAllRequested = false;
        private IResourceChildProvider _resourceChildProvider;
        private int                 _clicksAfterFocus = 0;
        private bool                _lastKeyDownHandled = false;
        private long                _lastTicksOfProcessingPendingUpdates = 0;
        private bool                _inProcessPendingUpdates = false;

        private class ResourceNodeData: IDisposable
        {
            private ResourceTreeView       _tree;
            private TreeNode               _node;
            private IResourceList          _childResources;
            private ResourceListEventQueue _eventQueue;
            private IntArrayList           _postponedResourceAdds = new IntArrayList();
            private bool                   _removingFromTree;

            internal ResourceNodeData( ResourceTreeView tree, TreeNode node, IResourceList children )
            {
                _tree = tree;
                _node = node;
                _childResources = children;
                _eventQueue = new ResourceListEventQueue();
                _eventQueue.Attach( _childResources );
            }

            public void Dispose()
            {
                _eventQueue.Detach();
                _childResources.Dispose();
            }

            internal IResourceList ChildResources
            {
                get { return _childResources; }
            }

            internal bool RemovingFromTree
            {
                get { return _removingFromTree; }
                set { _removingFromTree = value; }
            }

            internal void ProcessPendingUpdates()
            {
                if ( _tree.IsDisposed || _removingFromTree || Core.State == CoreState.ShuttingDown )
                {
                    return;
                }
                TreeNode lastNewNode = null;
                bool nodesRemoved = false;

                while( true )
                {
                    if ( ! _eventQueue.BeginProcessEvents() )
                        break;

                    ResourceListEvent ev = _eventQueue.GetNextEvent();
                    if ( ev == null )
                    {
                        _eventQueue.EndProcessEvents();
                        break;
                    }

                    switch( ev.EventType )
                    {
                        case EventType.Add:
                            lastNewNode = ProcessNodeAdd( ev.ResourceID, true );
                            _eventQueue.EndProcessEvents();
                            break;

                        case EventType.Change:
                            _eventQueue.EndProcessEvents();
                            ProcessNodeChange( ev );
                            break;

                        case EventType.Remove:
                            // Removing a node can cause a selection change, and a lot of user
                            // code to be called, so in order to avoid deadlocks, we need to
                            // release the resource list lock before we process the event
                            _eventQueue.EndProcessEvents();
                            ProcessNodeRemove( ev.ResourceID );
                            nodesRemoved = true;
                            break;

                        default:
                            _eventQueue.EndProcessEvents();
                            break;
                    }
                }

                // set the selected node outside of resource list lock
                if ( lastNewNode != null && _tree.SelectAddedItems )
                {
                    _tree.SelectedNode = lastNewNode;
                }
                if ( nodesRemoved && _tree.DoubleBuffer )
                {
                    _tree.RestartGarbageCleanupTimer();
                }
            }

            /**
             * Processes pending adds of resources. The method is called after all the
             * remove events have been processed, so the check for existing nodes
             * will work correctly.
             */

            internal void ProcessPostponedUpdates()
            {
                foreach( int resourceID in _postponedResourceAdds )
                {
                    ProcessNodeAdd( resourceID, false );
                }
                _postponedResourceAdds.Clear();
            }

            private TreeNode ProcessNodeAdd( int resourceId, bool mayPostpone )
            {
                if ( _tree.GetResourceNode( _node, resourceId ) != null )
                {
                    // we may have a pending event to remove this resource
                    if ( mayPostpone )
                    {
                        _postponedResourceAdds.Add( resourceId );
                        _tree.AddPostponedUpdateNodeData( this );
                    }
                    return null;
                }

                IResource res;

                // if the tree is filtered, we can't use the list index as the insert position
                // => find the previous visible node and use its index + 1
                int listIndex = _childResources.IndexOf( resourceId );
                int insertIndex = 0;
                while( listIndex > 0 )
                {
                    listIndex--;
                    res = _childResources [listIndex];
                    TreeNode node = _tree.GetResourceNode( _node, res.Id );
                    if ( node != null )
                    {
                        insertIndex = node.Index+1;
                        break;
                    }
                }

                try
                {
                    res = Core.ResourceStore.LoadResource( resourceId );
                }
                catch( StorageException )
                {
                    return null;
                }

                TreeNode newNode = _tree.AddResourceNode( _node, insertIndex, res );
                return newNode;
            }

            private void ProcessNodeChange( ResourceListEvent ev )
            {
                IResource res = Core.ResourceStore.LoadResource( ev.ResourceID );
                if ( CheckRefreshFilter( res ) )
                    return;

                TreeNode node = _tree.GetResourceNode( _node, ev.ResourceID );
                if ( node != null )
                {
                    int imgIndex = Core.ResourceIconManager.GetIconIndex( res );
                    if ( imgIndex != node.ImageIndex )
                    {
                        node.ImageIndex = imgIndex;
                        node.SelectedImageIndex = imgIndex;
                    }

                    if ( ev.ChangeSet.IsDisplayNameAffected || _tree._autoUpdateDecorators || _tree.MultiSelect )
                    {
                        _tree.UpdateNodeRichText( node, false );
                    }

                    if ( _tree.IsHandleCreated && node.Nodes.Count == 0 &&
                        res.GetLinksTo( null, _tree.ParentProperty ).Count > 0 )
                    {
                        _tree.SetNodeChildCount( node, 1 );
                    }
                }
            }

            internal bool CheckRefreshFilter( IResource res )
            {
                TreeNode node = _tree.GetResourceNode( _node, res.Id );

                if ( node == null )
                {
                    // the node may pass the filters after the change
                    if ( _tree.FiltersAccept( res, _node ) )
                    {
                        ProcessNodeAdd( res.Id, true );
                        return true;
                    }
                }

                if ( node != null && node.TreeView != null )
                {
                    if ( !_tree.FiltersAccept( res, _node ) )
                    {
                        ProcessNodeRemove( res.Id );
                        return true;
                    }
                }
                return false;
            }

            private void ProcessNodeRemove( int resourceID )
            {
                TreeNode node = _tree.GetResourceNode( _node, resourceID );
                // the node may have been removed by recursive remove and then added
                // to another child, and in this case we don't need to remove it again
                // - that's why we check the parent
                if ( node != null && node.TreeView != null && node.Parent == _node )
                {
                    ResourceNodeData childNodeData = null;
                    if ( node.Nodes.Count > 0 )
                    {
                        childNodeData = (ResourceNodeData) _tree._nodeData [resourceID];
                        if ( childNodeData != null )
                        {
                            childNodeData.RemoveAllChildren();
                            childNodeData.RemovingFromTree = true;
                        }
                    }

                    TreeNode parent = node.Parent;
                    try
                    {
                        node.Remove();
                    }
                    catch( NullReferenceException )  // #4930
                    {
                        Trace.WriteLine( "Null reference exception when removing node from tree" );
                    }

                    _tree.RemoveResourceNode( resourceID );
                    if ( _tree.IsHandleCreated && parent != null && parent.Nodes.Count == 0 )
                    {
                        _tree.SetNodeChildCount( parent, 0 );
                    }
                }
            }

            private void RemoveAllChildren()
            {
                for( int i=_node.Nodes.Count-1; i >= 0; i-- )
                {
                    TreeNode node = _node.Nodes [i];
                    IResource res = (IResource) node.Tag;
                    ProcessNodeRemove( res.OriginalId );
                }
            }
        }

        private IntHashTable _nodeData = new IntHashTable();           // resource ID -> ResourceNodeData
        private IntHashTable _resourceToNodeMap    = new IntHashTable();  // resource ID -> TreeNode
        private IntHashTable _resourceToNodeMapNew = new IntHashTable();  // resource ID -> TreeNode
        private HashSet _decorationChangedNodes = new HashSet();
        internal bool _autoUpdateDecorators = false;

        private Timer _garbageRemoveTimer;

        public event EventHandler TreeCreated;
        public event EventHandler TreeUpdated;
        public event EventHandler MouseActivate;

		public ResourceTreeView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            _dragScrollTimer = new Timer( components );
            _dragScrollTimer.Interval = 250;
            _dragScrollTimer.Tick += _dragScrollTimer_Tick;

            _expandTimer = new Timer( components );
            _expandTimer.Interval = 250;
            _expandTimer.Tick += _expandTimer_Tick;

			_nodePainter = new RichTextNodePainter();

            _searchWindow = new SearchWindow();
            Controls.Add( _searchWindow );
            _searchWindow.Location = new Point( 4, 4 );

            _garbageRemoveTimer = new Timer( components );
            _garbageRemoveTimer.Interval = 300;
            _garbageRemoveTimer.Tick += _garbageRemoveTimer_OnTick;

            _searchWindow.TreeView = this;

            if ( ICore.Instance != null )
            {
                if ( Core.State == CoreState.Initializing )
                {
                    Core.StateChanged += OnCoreStateChanged;
                }
                else
                {
                    Initialize();
                }
            }
		}

        private void OnCoreStateChanged( object sender, EventArgs e )
	    {
	        if ( Core.State != CoreState.Initializing )
	        {
	            Core.StateChanged -= OnCoreStateChanged;
                Initialize();
	        }
	    }

	    /// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
                ImageList = null;
                if ( ICore.Instance != null )
                {
                    Core.ResourceAP.JobFinished -= OnResourceOperationFinished;
                    Core.ResourceAP.QueueGotEmpty -= ResourceAP_QueueGotEmpty;
                    Core.UIManager.ExitMenuLoop -= UiManager_OnExitMenuLoop;
                }
                ClearNodeData();

                if( components != null )
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion


        private void Initialize()
        {
            Core.ResourceAP.JobFinished += OnResourceOperationFinished;
            Core.ResourceAP.QueueGotEmpty += ResourceAP_QueueGotEmpty;
            ImageList = Core.ResourceIconManager.ImageList;
            Core.UIManager.ExitMenuLoop += UiManager_OnExitMenuLoop;
        }

        private void UiManager_OnExitMenuLoop( object sender, EventArgs e )
        {
            // when double-buffering is used, and a menu item is selected over the tree view,
            // garbage remains on the place of the cursor after the menu is closed
            _garbageRemoveTimer.Start();
        }

        private void _garbageRemoveTimer_OnTick( object sender, EventArgs e )
        {
            _garbageRemoveTimer.Stop();
            Invalidate();
        }

        public IResourceChildProvider ResourceChildProvider
	    {
	        get { return _resourceChildProvider; }
	        set { _resourceChildProvider = value; }
	    }

	    [Browsable(false), DefaultValue(null)]
        public IResource RootResource
        {
            get { return _rootResource; }
            set
            {
                if ( _rootResource != value )
                {
                    _rootResource = value;
                    if ( IsHandleCreated && _parentProperty != -1 )
                    {
                        RecreateTree();
                    }
                }
            }
        }

        [DefaultValue(-1)]
        public int ParentProperty
        {
            get { return _parentProperty; }
            set
            {
                if ( _parentProperty != value )
                {
                    _parentProperty = value;
                    if ( IsHandleCreated && _rootResource != null )
                    {
                        RecreateTree();
                    }
                }
            }
        }

	    public bool ShowRootResource
	    {
	        get { return _showRootResource; }
	        set
	        {
	            if ( _showRootResource != value )
	            {
                    _showRootResource = value;
                    if ( IsHandleCreated && _rootResource != null && _parentProperty >= 0 )
                    {
                        RecreateTree();
                    }
	            }
	        }
	    }

	    [DefaultValue(-1)]
        public int OpenProperty
        {
            get { return _openProperty; }
            set { _openProperty = value; }
        }

        [DefaultValue(-1)]
        public int CheckedProperty
        {
            get { return _checkedProperty; }
            set { _checkedProperty = value; }
        }

        [DefaultValue(1)]
        public int CheckedSetValue
        {
            get { return _checkedSetValue; }
            set { _checkedSetValue = value; }
        }

        [DefaultValue(0)]
        public int CheckedUnsetValue
        {
            get { return _checkedUnsetValue; }
            set { _checkedUnsetValue = value; }
        }

        /**
         * If DelaySaveChecked is set, the Checked property of the nodes is set not
         * immediately after a checkbox has been toggled, but by an explicit call to
         * SaveCheckedState().
         */

        [DefaultValue(false)]
        public bool DelaySaveChecked
        {
            get { return _delaySaveChecked; }
            set { _delaySaveChecked = value; }
        }

        [DefaultValue(true)]
        public bool ShowContextMenu
        {
            get { return _showContextMenu; }
            set { _showContextMenu = value; }
        }

        [Browsable(false), DefaultValue(null)]
        public IResource SelectedResource
        {
            get
            {
                if ( SelectedNode == null )
                    return null;
                return (IResource) SelectedNode.Tag;
            }
            set
            {
                SelectResourceNode( value );
            }
        }

        [Browsable(false), DefaultValue(null)]
        public IResourceList SelectedResources
        {
            get
            {
                TreeNode[] nodes = SelectedNodes;
                IntArrayList selectedResourceIds = new IntArrayList( nodes.Length );
                for( int i=0; i<nodes.Length; i++ )
                {
                    IResource res = (IResource) nodes [i].Tag;
                    if ( !res.IsDeleted )
                    {
                        selectedResourceIds.Add( res.Id );
                    }
                }
                return Core.ResourceStore.ListFromIds( selectedResourceIds, false );
            }
        }

        [DefaultValue(null)]
        public object MenuContext
        {
            get { return _menuContextInstance; }
            set { _menuContextInstance = value; }
        }

        [Browsable(false)]
        public SearchWindow SearchWindow
        {
            get { return _searchWindow; }
        }

        /**
         * Allows dropping items on the empty space in the resource list.
         */

        [DefaultValue(false)]
        public bool DropOnEmpty
        {
            get { return _dropOnEmpty; }
            set { _dropOnEmpty = value; }
        }

        /**
         * Turns on automatic selection of new resources added to the tree.
         */

        [DefaultValue(false)]
        public bool SelectAddedItems
        {
            get { return _selectAddedItems; }
            set { _selectAddedItems = value; }
        }

        /**
         * Enables or disables executing the double-click action when an item is double-clicked
         * or Enter is pressed on an item.
         */

        [DefaultValue(true)]
        public bool ExecuteDoubleClickAction
        {
            get { return _executeDoubleClickAction; }
            set { _executeDoubleClickAction = value; }
        }

	    /// <summary>
	    /// Whether a resource can be displayed in multiple places in the tree view.
	    /// </summary>
        [DefaultValue(true)]
        public bool UniqueResources
	    {
	        get { return _uniqueResources; }
	        set { _uniqueResources = value; }
	    }

        public new void ExpandAll()
        {
            if ( IsHandleCreated )
            {
                base.ExpandAll();
            }
            else
            {
                _expandAllRequested = true;
            }
        }

	    public event ResourceDragEventHandler ResourceDragOver;
        public event ResourceDragEventHandler ResourceDrop;
        public event TreeViewEventHandler ResourceAdded;


        /**
         * Sets the root resource and parent property ID in a single call,
         * and does not update the tree.
         */

        public void SetRootResource( IResource root, int parentProp )
        {
            _rootResource = root;
            _parentProperty = parentProp;
        }

        public void AddNodeDecorator( IResourceNodeDecorator decorator )
        {
            AddNodeDecorator( decorator, false );
        }

        public void AddNodeDecorator( IResourceNodeDecorator decorator, bool autoUpdate )
        {
            _nodeDecorators.Add( decorator );
            decorator.DecorationChanged += OnDecorationChanged;
            // only when the first decorator is added, the RichTextNodePainter is enabled
            NodePainter = _nodePainter;
            if ( autoUpdate )
            {
                _autoUpdateDecorators = true;
            }
        }

        protected override void OnMultiSelectChanged()
        {
            base.OnMultiSelectChanged();
            if ( MultiSelect )
            {
                NodePainter = _nodePainter;
            }
        }

        #region Filters
        public void AddNodeFilter( IResourceNodeFilter filter )
        {
            if ( filter == null )
                throw new ArgumentNullException( "filter" );
            _nodeFilters.Add( filter );
        }

        /**
         * Removes the specified filter from the tree. Returns true if the filter
         * was actually removed.
         */

        public bool RemoveNodeFilter( IResourceNodeFilter filter )
        {
            int index = _nodeFilters.IndexOf( filter );
            if ( index >= 0 )
            {
                _nodeFilters.RemoveAt( index );
                return true;
            }
            return false;
        }

        public IResourceNodeFilter[] GetNodeFilters()
        {
            return (IResourceNodeFilter[]) _nodeFilters.ToArray( typeof(IResourceNodeFilter) );
        }

        /**
         * Removes all filters from the tree.
         */

        public void ClearNodeFilters()
        {
            _nodeFilters.Clear();
        }

        /**
         * After the filtering conditions have changed, clears and refills the tree.
         * @param keepSelection If false, the selection will not be set to the node
         * which was previously selected.
         */

        public void UpdateNodeFilter( bool keepSelection )
        {
            // if the handle has not yet been created, the tree was not yet built,
            // so when the handle is created, it will be built with the correct filter
            if ( IsHandleCreated )
            {
                if ( keepSelection )
                    _resourceToSelect = SelectedResource;
                else
                    _resourceToSelect = null;
                RecreateTree();
            }
        }
        #endregion Filters

        public void ClearSearchWindow()
        {
            _searchWindow.CancelSearch();
        }

        /**
         * Ensures that the children of the specified node are all created.
         */

        public void ForceCreateChildren( TreeNode node )
        {
            IResource res = (IResource) node.Tag;
            if ( node.Nodes.Count == 0 )
            {
                ExpandNode( node, res, true );
            }
            else
            {
                foreach( TreeNode childNode in node.Nodes )
                {
                    ForceCreateChildren( childNode );
                }
            }
        }

        protected override void OnHandleCreated( EventArgs e )
        {
            base.OnHandleCreated( e );
            if ( _parentProperty != -1 && _rootResource != null )
            {
                ProcessHandleCreated();
            }
        }

        private void ProcessHandleCreated()
        {
            if ( _inProcessPendingUpdates )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( ProcessHandleCreated ) );
                return;
            }

            if ( IsDisposed )  // OM-10748
            {
                return;
            }
            RecreateTree();
            if ( _expandAllRequested )
            {
                _expandAllRequested = false;
                ExpandAll();
            }
        }

        private void ClearNodeData()
        {
            foreach( IntHashTable.Entry nodeData in _nodeData )
            {
                ((ResourceNodeData) nodeData.Value).Dispose();
            }
            _nodeData.Clear();
            _resourceToNodeMap.Clear();
            _resourceToNodeMapNew.Clear();
        }

        private void RecreateTree()
        {
            ClearNodeData();
            BeginUpdate();
            try
            {
                while( Nodes.Count > 0 )
                {
                    try
                    {
                        Nodes [Nodes.Count-1].Remove();
                    }
                    catch( NullReferenceException ex )
                    {
                        Trace.WriteLine( "Error removing node from tree: " + ex.ToString() );
                    }
                }

                if ( _rootResource != null && _parentProperty >= 0 )
                {
                    TreeNode rootNode = null;
                    if ( _showRootResource )
                    {
                        rootNode = AddResourceNode( null, 0, _rootResource );
                    }
                    ExpandNode( rootNode, _rootResource, false );
                    if ( _resourceToSelect != null )
                    {
                        SelectResourceNode( _resourceToSelect );
                        _resourceToSelect = null;
                    }
                }
                FlushNodeMap();
            }
            finally
            {
                EndUpdate();
            }
            if ( TreeCreated != null )
            {
                TreeCreated( this, EventArgs.Empty );
            }
        }

        private void ExpandNode( TreeNode parentNode, IResource parent, bool forceCreateChildren )
        {
            if ( parentNode != null )
            {
                _expandingNodes.Add( parentNode );
            }

            IResourceList children = CreateResourceNodeData( parent, parentNode );

            bool hasChildren = false;
            for( int i=0; i<children.Count; i++ )
            {
                //  Workaround of OM-12881 and related.
                try
                {
                    IResource child = children [i];
                    if ( GetResourceNode( parentNode, child.Id ) != null )
                    {
                        continue;
                    }

                    TreeNode childNode = AddResourceNode( parentNode, -1, child );
                    if ( childNode != null )
                    {
                        hasChildren = true;
                        if ( _openProperty != -1 && child.GetIntProp( _openProperty ) > 0 )
                        {
                            ExpandNode( childNode, child, forceCreateChildren );
                            childNode.Expand();
                        }
                        else if ( forceCreateChildren )
                        {
                            ExpandNode( childNode, child, true );
                        }
                    }
                }
                catch( OpenAPI.InvalidResourceIdException )
                {
                    // Nothing to do, just ignore.
                }
            }
            if ( !hasChildren && parentNode != null )
            {
                SetNodeChildCount( parentNode, 0 );
            }
            if ( parentNode != null )
            {
                _expandingNodes.Remove( parentNode );
            }
        }

        private IResourceList CreateResourceNodeData( IResource parent, TreeNode parentNode )
        {
            ResourceNodeData existingNodeData = (ResourceNodeData) _nodeData [parent.Id];
            if ( existingNodeData != null )
            {
                return existingNodeData.ChildResources;
            }

            IResourceList children = null;
            if ( _resourceChildProvider != null )
            {
                children = _resourceChildProvider.GetChildResources( this, parent );
            }

            if ( children == null )
            {
                children = parent.GetLinksToLive( null, _parentProperty );

                string nodeSort = Core.ResourceTreeManager.GetResourceNodeSort( parent );
                if ( nodeSort != null )
                {
                    children.Sort( nodeSort );
                }
            }

            ResourceNodeData parentData = new ResourceNodeData( this, parentNode, children );
            _nodeData [parent.Id] = parentData;
            return children;
        }

        private TreeNode AddResourceNode( TreeNode parentNode, int index, IResource child )
        {
            if ( !FiltersAccept( child, parentNode ) )
            {
                return null;
            }

            int iconIndex = -1;
            if ( ICore.Instance != null )
            {
                iconIndex = Core.ResourceIconManager.GetIconIndex( child );
            }
            TreeNode node = new TreeNode( "", iconIndex, iconIndex );
            node.Tag = child;
            if ( parentNode == null )
            {
                if ( index < 0 )
                {
                    Nodes.Add( node );
                }
                else
                {
                    Nodes.Insert( index, node );
                }
            }
            else
            {
                bool firstChild = ( parentNode.Nodes.Count == 0 );
                if ( index < 0 )
                {
                    parentNode.Nodes.Add( node );
                }
                else
                {
                    parentNode.Nodes.Insert( index, node );
                }
                if ( firstChild )
                {
                    SetNodeChildCount( parentNode, 1 );
                }
            }

            if ( _checkedProperty >= 0 && child.GetIntProp( _checkedProperty ) == _checkedSetValue )
            {
                node.Checked = true;
            }

            if ( IsResourceContainer( child ) )
            {
                if ( child.GetLinksTo( null, _parentProperty ).Count > 0 )
                {
                    SetNodeChildCount( node, 1 );
                }
                else
                {
                    // we have no children now, but may get some later
                    CreateResourceNodeData( child, node );
                }
            }
            UpdateNodeRichText( node, true );
            if ( ResourceAdded != null )
            {
                ResourceAdded( this, new TreeViewEventArgs( node ) );
            }
            _resourceToNodeMapNew [child.Id] = node;
            return node;
        }

        /**
         * Returns the node for the specified resource.
         */

        internal TreeNode GetResourceNode( TreeNode parent, int resourceID )
        {
            if ( UniqueResources )
            {
                TreeNode node = (TreeNode) _resourceToNodeMap [resourceID];
                if ( node == null )
                {
                    node = (TreeNode) _resourceToNodeMapNew [resourceID];
                }
                return node;
            }

            if ( parent != null )
            {
                foreach( TreeNode node in parent.Nodes )
                {
                    IResource res = (IResource) node.Tag;
                    if ( res.OriginalId == resourceID )
                        return node;
                }
            }
            else
            {
                foreach( TreeNode node in Nodes )
                {
                    IResource res = (IResource) node.Tag;
                    if ( res.OriginalId == resourceID )
                        return node;
                }
            }
            return null;
        }

        /**
         * Removes the resource node for the specified resource from the map.
         */

        internal void RemoveResourceNode( int resourceID )
        {
            _resourceToNodeMap.Remove( resourceID );
            _postponedRemoveNodeData.Add( resourceID );
        }

        /**
         * Gets the level of the node (0 if node=null, 1 if node is a top-level node, and so on)
         */

        private static int GetNodeLevel( TreeNode node )
        {
            int level = 0;
            while( node != null )
            {
                level++;
                node = node.Parent;
            }
            return level;
        }

        /**
         * Checks if the specified node, with the specified parent node, matches the
         * tree filter conditions.
         */

        public bool FiltersAccept( IResource res, TreeNode parentNode )
        {
            int level = GetNodeLevel( parentNode );
            foreach( IResourceNodeFilter filter in _nodeFilters )
            {
                if ( !filter.AcceptNode( res, level ) )
                {
                    return false;
                }
            }
            return true;
        }

        /**
         * Notifies the tree that because of a change in filtering conditions the specified
         * node matches or no longer matches the filtering conditions.
         */

        public void RefreshFilterForNode( IResource res )
        {
            if ( InvokeRequired && IsHandleCreated )
            {
                Core.UIManager.QueueUIJob( new ResourceDelegate( RefreshFilterForNode ), new object[] { res } );
            }
            else
            {
                IResource parent = res.GetLinkProp( _parentProperty );
                if ( parent != null )
                {
                    ResourceNodeData parentData = (ResourceNodeData) _nodeData [parent.Id];
                    if ( parentData != null )
                    {
                        parentData.CheckRefreshFilter( res );
                    }
                }
            }
        }

        /**
         * Shows or hides the [+] sign on a tree node.
         */

        private void SetNodeChildCount( TreeNode node, int count )
        {
            if ( node.TreeView != null )
            {
                TVITEM item = new TVITEM();
                item.mask = TreeViewItemFlags.CHILDREN | TreeViewItemFlags.HANDLE;
                item.hItem = node.Handle;
                item.cChildren = count;
                Win32Declarations.SendMessage( Handle, TreeViewMessage.TVM_SETITEMA, 0, ref item );
            }
        }

        public int GetNodeChildCount( TreeNode node )
        {
            TVITEM item = new TVITEM();
            item.mask = TreeViewItemFlags.CHILDREN | TreeViewItemFlags.HANDLE;
            item.hItem = node.Handle;
            Win32Declarations.SendMessage( Handle, TreeViewMessage.TVM_GETITEMA, 0, ref item );
            return item.cChildren;
        }

        /**
         * Updates the decorated rich text for all nodes.
         */

        public void RefreshNodeRichText()
        {
            if ( _nodeDecorators.Count > 0 )
            {
                RefreshRichTextRecursive( Nodes );
            }
        }

        /**
         * Updates the rich text for nodes in the specified collection and their children.
         */

        private void RefreshRichTextRecursive( TreeNodeCollection nodes )
        {
            if ( nodes != null )
            {
                foreach( TreeNode node in nodes )
                {
                    UpdateNodeRichText( node, false );
                    RefreshRichTextRecursive( node.Nodes );
                }
            }
        }

        internal void UpdateNodeRichText( TreeNode node, bool newNode )
        {
            Debug.Assert( node != null );
            IResource res = (IResource) node.Tag;
            if ( _nodeDecorators.Count > 0 || MultiSelect )
            {
                RichText text = new RichText( res.DisplayName, new RichTextParameters( this.Font ) );
                foreach( IResourceNodeDecorator dec in _nodeDecorators )
                {
                    dec.DecorateNode( res, text );
                }
                _nodePainter.Add( node, text );
            }
            if ( newNode || node.Text != res.DisplayName )
            {
                node.Text = res.DisplayName;
            }
        }

        /**
         * Checks if the specified resource is a resource container.
         */

        private bool IsResourceContainer( IResource child )
        {
            return ICore.Instance.ResourceStore.ResourceTypes [child.Type].HasFlag( ResourceTypeFlags.ResourceContainer );
        }

        protected override void OnBeforeExpand( TreeViewCancelEventArgs e )
        {
            if ( e.Node.Nodes.Count == 0 )
            {
                CreateChildNodes( e.Node );
            }
            base.OnBeforeExpand( e );
        }

        internal void CreateChildNodes( TreeNode node )
        {
            IResource res = (IResource) node.Tag;
            ExpandNode( node, res, false );
            if ( node.Nodes.Count == 0 )
            {
                SetNodeChildCount( node, 0 );
            }
            FlushNodeMap();
        }

        protected override void OnAfterExpand( TreeViewEventArgs e )
        {
            base.OnAfterExpand( e );
            if ( _openProperty != -1 )
            {
                IResource res = (IResource) e.Node.Tag;
                new ResourceProxy( res ).SetPropAsync( _openProperty, 1 );
            }
        }

        protected override void OnAfterCollapse( TreeViewEventArgs e )
        {
            base.OnAfterCollapse( e );
            if ( _openProperty != -1 )
            {
                IResource res = (IResource) e.Node.Tag;
                new ResourceProxy( res ).SetPropAsync( _openProperty, 0 );
            }
        }

        protected override void OnAfterCheck( TreeViewEventArgs e )
        {
            base.OnAfterCheck( e );
            if ( _checkedProperty != -1 && !_delaySaveChecked )
            {
                IResource res = (IResource) e.Node.Tag;
                new ResourceProxy( res ).SetPropAsync( _checkedProperty,
                    e.Node.Checked ? _checkedSetValue : _checkedUnsetValue );
            }
        }

        private void OnResourceOperationFinished( object sender, EventArgs e )
        {
            if ( !IsHandleCreated )
                return;
            if( _lastTicksOfProcessingPendingUpdates + 2000000 < DateTime.Now.Ticks )
            {
                ForceProcessingPendingUpdates();
            }
        }

        private void ResourceAP_QueueGotEmpty( object sender, EventArgs e )
        {
            ForceProcessingPendingUpdates();
        }

        private void ForceProcessingPendingUpdates()
        {
            _lastTicksOfProcessingPendingUpdates = DateTime.Now.Ticks;
            Core.UIManager.QueueUIJob( new MethodInvoker( ProcessPendingUpdates ), new object[] {} );
        }

        /**
         * When the decoration of an unread node changes, either queues the change
         * to be processed at the end of the resource operation, or processes it
         * immediately if it was not invoked from the resource thread.
         */

        private void OnDecorationChanged( object sender, ResourceEventArgs e )
        {
            if ( Core.State == CoreState.ShuttingDown )
            {
                return;
            }

            if ( e.Resource != null )
            {
                if ( Core.ResourceStore.IsOwnerThread() )
                {
                    lock( _decorationChangedNodes )
                    {
                        _decorationChangedNodes.Add( e.Resource );
                    }
                }
                else if ( InvokeRequired )
                {
                    Core.UIManager.QueueUIJob( new ResourceEventHandler( OnDecorationChanged ), new object[] { sender, e } );
                }
                else
                {
                    // TODO: doesn't work for non-unique resources
                    TreeNode node = GetResourceNode( null, e.Resource.Id );
                    if ( node != null )
                    {
                        UpdateNodeRichText( node, false );
                    }
                }
            }
        }

        /**
         * Processes the changes in resource tree nodes that have been accumulated by operations
         * running in the resource thread.
         */

        public void ProcessPendingUpdates()
        {
            if ( Core.State == CoreState.ShuttingDown )
            {
                return;
            }

            // prevent reentering
            if ( _inProcessPendingUpdates )
                return;

            _inProcessPendingUpdates = true;
            try
            {
                lock( _decorationChangedNodes )
                {
                    foreach (HashSet.Entry entry in _decorationChangedNodes)
                    {
                        IResource res = (IResource) entry.Key;
                        TreeNode node = GetResourceNode( null, res.Id );
                        if ( node != null )
                        {
                            UpdateNodeRichText( node, false );
                        }
                    }
                    _decorationChangedNodes.Clear();
                }

                IResource oldSelectedResource = null;
                if ( SelectedNodes.Length == 1 )
                {
                    oldSelectedResource = SelectedResource;
                }

                // ResourceNodeData.ProcessPendingUpdates() can modify the hashtable,
                // so we cannot use foreach enumeration
                ArrayList dataToUpdate = ArrayListPool.Alloc();
                try
                {
                    foreach( IntHashTable.Entry entry in _nodeData )
                    {
                        dataToUpdate.Add( entry.Value );
                    }
                    foreach( ResourceNodeData resourceNodeData in dataToUpdate )
                    {
                        resourceNodeData.ProcessPendingUpdates();
                    }
                }
                finally
                {
                    ArrayListPool.Dispose( dataToUpdate );
                }

                foreach ( ResourceNodeData resourceNodeData in _postponedUpdateNodeData )
                {
                    resourceNodeData.ProcessPostponedUpdates();
                }
                _postponedUpdateNodeData.Clear();
                foreach( int resourceId in _postponedRemoveNodeData )
                {
                    _nodeData.Remove( resourceId );
                }
                _postponedRemoveNodeData.Clear();
                FlushNodeMap();

                if ( oldSelectedResource != null && !SelectAddedItems )
                {
                    TreeNode selNode = FindResourceNode( oldSelectedResource );
                    if ( selNode != null && !selNode.IsSelected )
                    {
                        SelectedNode = selNode;
                        if ( MultiSelect )
                        {
                            SelectedNodes = new TreeNode[] { selNode };
                        }
                    }
                }

                if ( TreeUpdated != null )
                {
                    TreeUpdated( this, EventArgs.Empty );
                }
            }
            finally
            {
                _inProcessPendingUpdates = false;
            }
        }

        private void AddPostponedUpdateNodeData( ResourceNodeData nodeData )
        {
            _postponedUpdateNodeData.Add( nodeData );
        }

        /**
         * Moves all entries from the "added nodes" map to the real node map.
         */

        private void FlushNodeMap()
        {
            foreach( IntHashTable.Entry e in _resourceToNodeMapNew )
            {
                _resourceToNodeMap [e.Key] = e.Value;
            }
            _resourceToNodeMapNew.Clear();
        }

        /**
         * Correctly selects the node when the right mouse button is clicked.
         */

        protected override void OnMouseDown( MouseEventArgs e )
        {
            base.OnMouseDown( e );
            if ( e.Button == MouseButtons.Right )
            {
                TreeNode node = GetNodeAt( e.X, e.Y );
                if ( MultiSelect )
                {
                    if ( node == null || !node.IsSelected )
                    {
                        SelectedNode = node;
                        SelectedNodes = new TreeNode[] { node };
                    }
                }
                else
                {
                    SelectedNode = node;
                }
            }
        }

        /**
         * Handles the WM_CONTEXTMENU message to show the context menu for the node.
         */

        protected override void WndProc( ref Message m )
        {
            if ( m.Msg == Win32Declarations.WM_LBUTTONDOWN )
            {
                if ( !ContainsFocus )
                {
                    if ( MouseActivate != null )
                    {
                        MouseActivate( this, EventArgs.Empty );
                    }
                    _clicksAfterFocus = 0;
                }
                else
                {
                    _clicksAfterFocus++;
                }
            }

            base.WndProc( ref m );
            if ( m.Msg == Win32Declarations.WM_CONTEXTMENU && ICore.Instance != null &&
                _showContextMenu && Visible && IsHandleCreated )
            {
                Point selectedNodeTop = new Point( 0, 0 );
                if ( SelectedNode != null )
                {
                    selectedNodeTop = SelectedNode.Bounds.Location;
                }
                Point pnt = new Point( m.LParam.ToInt32() );
                if ( pnt.X == -1 && pnt.Y == -1 )
                {
                    pnt = selectedNodeTop;
                    pnt.X += 4;
                    pnt.Y += 4;
                }
                else
                {
                    pnt = PointToClient( pnt );
                }
                ActionContext context = GetActionContext( ActionContextKind.ContextMenu );
                Core.ActionManager.ShowResourceContextMenu( context, this, pnt.X, pnt.Y );
            }
            else if ( m.Msg == Win32Declarations.WM_EXITMENULOOP )
            {
                _garbageRemoveTimer.Start();
            }
        }

        /**
         * Returns the node tagged with the specified resource. If needed, expands the
         * tree branches leading to the node.
         */

        public TreeNode FindResourceNode( IResource res )
        {
            TreeNode node;
            if ( UniqueResources )
            {
                node = GetResourceNode( null, res.Id );
                if ( node != null )
                {
                    return node;
                }
            }

            return FindResourceNodeExpanded( res );
        }

        internal TreeNode FindResourceNodeExpanded( IResource res )
        {
            TreeNode node;
            if ( Nodes.Count == 0 )
                return null;

            ArrayList parentStack = ArrayListPool.Alloc();
            try
            {
                IResource parent = res;
                do
                {
                    parentStack.Insert( 0, parent );
                    parent = parent.GetLinkProp( _parentProperty );
                    if ( parent == null )
                        return null;
                } while( parent != _rootResource );

                node = Nodes [0];
                foreach( IResource parentRes in parentStack )
                {
                    node = GetResourceNode( node, parentRes.Id );
                    if ( node == null )
                        break;

                    if ( parentRes == res )
                        return node;

                    node.Expand();
                }
            }
            finally
            {
                ArrayListPool.Dispose( parentStack );
            }
            return null;
        }

        /**
         * Selects the node tagged with the specified resource. Returns true if
         * the node was found in the tree, false otherwise.
         */

        public bool SelectResourceNode( IResource res )
        {
            if ( !IsHandleCreated )
            {
                _resourceToSelect = res;
                return true;
            }

            if ( MultiSelect )
            {
                SelectedNodes = new TreeNode[] {};   // clear the selection before it's changed programmatically
            }

            if ( res == null )
            {
                SelectedNode = null;
                return true;
            }

            TreeNode node = FindResourceNode( res );
            if ( node != null )
            {
                SelectedNode = node;
                SelectedNodes = new TreeNode[] { node };
                return true;
            }
            return false;
        }

        /**
         * Selects all the resources in the specified list.
         */

        public void SelectResourceNodes( IResourceList resList )
        {
            if ( !MultiSelect )
            {
                SelectResourceNode( resList != null && resList.Count > 0 ? resList [0] : null );
                return;
            }

            SelectedNodes = new TreeNode[] {};   // clear the selection before it's changed programmatically

            if ( resList == null )
            {
                SelectedNode = null;
                return;
            }

            ArrayList treeNodes = ArrayListPool.Alloc();
            try
            {
                foreach( IResource res in resList )
                {
                    TreeNode node = FindResourceNode( res );
                    if ( node != null )
                    {
                        treeNodes.Add( node );
                    }
                }
                SelectedNodes = (TreeNode[]) treeNodes.ToArray( typeof (TreeNode) );
            }
            finally
            {
                ArrayListPool.Dispose( treeNodes );
            }
        }

        /**
         * Saves the Checked state for all nodes.
         */

        public void SaveCheckedState()
        {
            if ( _checkedProperty < 0 )
            {
                throw new InvalidOperationException( "CheckedProperty needs to be set before calling SaveCheckedState()" );
            }
            SaveCheckedStateRecursive( Nodes );
        }

        private void SaveCheckedStateRecursive( TreeNodeCollection nodes )
        {
            foreach( TreeNode node in nodes )
            {
                IResource res = (IResource) node.Tag;
                int checkedValue = node.Checked ? _checkedSetValue : _checkedUnsetValue;
                if ( res.GetIntProp( _checkedProperty ) != checkedValue )
                {
                    new ResourceProxy( res ).SetPropAsync( _checkedProperty, checkedValue );
                }
                if ( node.Nodes.Count > 0 )
                {
                    SaveCheckedStateRecursive( node.Nodes );
                }
            }
        }

        /**
         * When items are dragged, stores the ResourceList of the selected node
         * to the drag object.
         */

        protected override void OnItemDrag( ItemDragEventArgs e )
        {
            IResourceList resList;
            TreeNode dragNode = (TreeNode) e.Item;
            if ( dragNode.IsSelected )
            {
                resList = SelectedResources;
            }
            else
            {
                resList = ((IResource) dragNode.Tag).ToResourceList();
                if ( MultiSelect )
                {
                    SelectedNodes = new TreeNode[] { dragNode };
                }
            }

            if ( resList.Count > 0 )
            {
                Invalidate();  // to ensure selection is redrawn correctly
                DataObject dataObj = new DataObject();
                dataObj.SetData( typeof(IResourceList), resList );
                DoDragDrop( dataObj, DragDropEffects.Link | DragDropEffects.Move);
            }
        }

        protected override void OnDragOver( DragEventArgs drgevent )
        {
            base.OnDragOver( drgevent );

            Point pnt = PointToClient( new Point( drgevent.X, drgevent.Y ) );
            if ( pnt.Y < 7 )
            {
                _dragScrollUp = true;
                _dragScrollTimer.Enabled = true;
            }
            else if ( ClientSize.Height - pnt.Y < 7 )
            {
                _dragScrollUp = false;
                _dragScrollTimer.Enabled = true;
            }
            else
            {
                _dragScrollTimer.Enabled = false;
            }

            TreeNode node = GetDragNodeAt( drgevent.X, drgevent.Y );
            IResourceList droppedResources = (IResourceList) drgevent.Data.GetData( typeof(IResourceList) );
            if ( ( node != null || _dropOnEmpty ) && droppedResources != null )
            {
                if ( ResourceDragOver != null )
                {
                    IResource target = (node == null) ? null : (IResource) node.Tag;
                    ResourceDragEventArgs args = new ResourceDragEventArgs( target, droppedResources );
                    ResourceDragOver( this, args );
                    drgevent.Effect = args.Effect;
                }
            }
            else
            {
                drgevent.Effect = DragDropEffects.None;
            }
            if ( _dropHighlightNode != node )
            {
                _dropHighlightNode = node;
                SetDropHighlightNode( node );

                if ( node != null && !node.IsExpanded )
                {
                    _expandTimer.Stop();
                    _expandTimer.Start();
                }
            }
        }

        protected override void OnDragLeave( EventArgs e )
        {
            base.OnDragLeave( e );
            _dragScrollTimer.Enabled = false;
            RemoveDropHighlight();
        }

        protected override void OnDragDrop( DragEventArgs drgevent )
        {
            base.OnDragDrop( drgevent );
            _dragScrollTimer.Enabled = false;
            RemoveDropHighlight();

            TreeNode node = GetDragNodeAt( drgevent.X, drgevent.Y );
            IResourceList droppedResources = (IResourceList) drgevent.Data.GetData( typeof(IResourceList) );
            if ( ( node != null || _dropOnEmpty ) && droppedResources != null )
            {
                try
                {
                    if ( ResourceDrop != null )
                    {
                        IResource target = (node == null) ? null : (IResource) node.Tag;
                        ResourceDrop( this, new ResourceDragEventArgs( target, droppedResources ) );
                    }
                }
                catch( Exception ex )
                {
                    Core.ReportException( ex, false );
                }
            }
        }

        private void RemoveDropHighlight()
        {
            if ( _dropHighlightNode != null )
            {
                _dropHighlightNode = null;
                SetDropHighlightNode( null );
            }
        }

        private TreeNode GetDragNodeAt( int X, int Y )
        {
            Point pnt = PointToClient( new Point( X, Y ) );
            if ( NodePainter == null )
                return GetNodeAt( pnt.X, pnt.Y );
            else
                return NodePainter.GetNodeAt( this, pnt );
        }

        private void _dragScrollTimer_Tick( object sender, EventArgs e )
        {
            if ( _dragScrollUp )
            {
                Win32Declarations.SendMessage( Handle, Win32Declarations.WM_VSCROLL,
                    new IntPtr( Win32Declarations.SB_LINEUP ), IntPtr.Zero );
            }
            else
            {
                Win32Declarations.SendMessage( Handle, Win32Declarations.WM_VSCROLL,
                    new IntPtr( Win32Declarations.SB_LINEDOWN ), IntPtr.Zero );
            }
        }

        private void _expandTimer_Tick( object sender, EventArgs e )
        {
            if ( _dropHighlightNode != null )
            {
                _dropHighlightNode.Expand();
            }
            _expandTimer.Stop();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (SearchWindow.Visible)
                return true;
            else
                return base.IsInputKey(keyData);
        }

        /**
         * When a key is pressed, executes the keyboard action through ActionManager.
         * When F2 is pressed, begins the rename of the selected node.
         */

        protected override void OnKeyDown( KeyEventArgs e )
        {
            base.OnKeyDown( e );
            _lastKeyDownHandled = false;
            if ( !e.Handled && ICore.Instance != null )
            {
                ActionContext context = GetActionContext( ActionContextKind.Keyboard );
                _lastKeyDownHandled = true;
                // because of DoEvents() calls, we could get to OnKeyPress() before returning
                // from ExecuteKeyboardAction()
                if ( Core.ActionManager.ExecuteKeyboardAction( context, e.KeyData ) )
                {
                    e.Handled = true;
                }
                else if ( e.KeyCode == Keys.Enter && e.Modifiers == 0 && _executeDoubleClickAction )
                {
                    Core.ActionManager.ExecuteDoubleClickAction( context.SelectedResources );
                }
                else
                {
                    _lastKeyDownHandled = false;
                }
            }

            if ( !e.Handled && e.KeyData == Keys.F2 )
            {
                if ( SelectedNode != null && LabelEdit )
                {
                    _clicksAfterFocus++;
                    SelectedNode.BeginEdit();
                    _lastKeyDownHandled = true;
                }
            }
        }

        protected override void OnKeyPress( KeyPressEventArgs e )
        {
            base.OnKeyPress( e );
            if ( _lastKeyDownHandled )
            {
                e.Handled = true;
            }
        }

        private ActionContext GetActionContext(ActionContextKind kind)
        {
            IResourceList selResList = null;
            if( Core.State != CoreState.ShuttingDown )
            {
                if( MultiSelect )
                {
                    selResList = SelectedResources;
                }
                else
                if( SelectedResource != null && !SelectedResource.IsDeleted )
                {
                    selResList = SelectedResource.ToResourceList();
                }
            }

            ActionContext context = new ActionContext( kind, _menuContextInstance, selResList );
            context.SetOwnerForm( FindForm() );
            return context;
        }

        protected override void OnBeforeLabelEdit( NodeLabelEditEventArgs e )
        {
            // cancel label edit if it's too early to activate
            if ( _clicksAfterFocus == 0 )
            {
                e.CancelEdit = true;
                return;
            }

            base.OnBeforeLabelEdit( e );
        }

        /**
         * Wheh a tree node is double-clicked, executes the double-click action for it.
         */

        protected override void OnDoubleClick( EventArgs e )
        {
            base.OnDoubleClick( e );
            if ( ICore.Instance != null && SelectedResource != null && _executeDoubleClickAction )
            {
                Core.ActionManager.ExecuteDoubleClickAction( SelectedResource.ToResourceList() );
            }
        }

	    public void EditResourceLabel( IResource res )
	    {
            ProcessPendingUpdates();
            TreeNode node = FindResourceNode( res );
            if ( node != null )
            {
                SelectedNodes = new TreeNode[] { node };
                _clicksAfterFocus++;
                node.BeginEdit();
            }
        }

        internal void RestartGarbageCleanupTimer()
        {
            _garbageRemoveTimer.Stop();
            _garbageRemoveTimer.Start();
        }
	}

    public delegate void TreeNodeDelegate( TreeNode node );

    public interface IResourceNodeDecorator
    {
        event ResourceEventHandler DecorationChanged;
        string DecorationKey { get; }
        bool DecorateNode( IResource res, RichText nodeText );
    }

    public interface IResourceChildProvider
    {
        IResourceList GetChildResources( ResourceTreeView resourceTree, IResource parent );
    }
}
