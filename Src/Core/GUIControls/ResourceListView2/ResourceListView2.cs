// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// The almighty ResourceListView 2.0.
	/// </summary>
	public class ResourceListView2 : UserControl, IContextProvider, ICommandProcessor
	{
		protected JetListView _jetListView = new JetListView();
		protected bool _executeDoubleClickAction = true;
		protected bool _allowDrag = true;
		protected IContextProvider _contextProvider;
		protected IResourceDataProvider _dataProvider;
		protected ExpandedPropManager _expandedPropManager;
		protected InPlaceTextEditor _inPlaceEditor;
		protected IResourceDragDropHandler _rootResourceDropHandler;
		protected bool _selectAddedItems = false;
		protected IResource _emptyDropResource;
		protected Timer _keyNavigationTimer;
		protected bool _keyNavigation;
		private ItemFormatCache _itemFormatCache;
		protected ContextMenu _headerContextMenu;
		protected bool _allowSameViewDrag = true;
		protected bool _showContextMenu = true;
		protected bool _initialFill = false;
		protected bool _inResourceOperation = false;

		public delegate bool LocateMatchCallback( IResource res );

		/// <summary>
		/// The root resource.
		/// In case of a tree, its semantic is evident.
		/// If we have a plain list, then it's a "virtual" resource that handles drag-drops to the empty space and holds the items ordering.
		/// </summary>
		protected IResource _resRoot = null;

		public ResourceListView2()
		{
			_jetListView.Dock = DockStyle.Fill;
			_jetListView.Font = new Font( "Tahoma", 8 );
			_jetListView.ControlPainter = new GdiControlPainter();
			_jetListView.BackColor = SystemColors.Window;
			_jetListView.AllowDrop = true;
			_jetListView.DoubleClick += HandleDoubleClick;
			_jetListView.ItemDrag += HandleItemDrag;
			_jetListView.DragOver += HandleDragOver;
			_jetListView.DragDrop += HandleDragDrop;
			_jetListView.KeyDown += HandleKeyDown;
			_jetListView.KeyUp += HandleKeyUp;
			_jetListView.ContextMenuInvoked += HandleContextMenuInvoked;
			_jetListView.ItemUpdated += HandleItemUpdated;
			_jetListView.ActiveNodeChanged += HandleActiveNodeChanged;
			_jetListView.SelectionStateChanged += HandleSelectionStateChanged;
			_jetListView.NodeCollection.NodeAdded += HandleNodeAdded;
			_jetListView.ControlMethodInvoker = new OmeaMethodInvoker();
			Controls.Add( _jetListView );

			_inPlaceEditor = new InPlaceTextEditor();
			_jetListView.InPlaceEditor = _inPlaceEditor;

			_inPlaceEditor.BeforeItemEdit += HandleBeforeItemEdit;
			_inPlaceEditor.AfterItemEdit += HandleAfterItemEdit;

			_keyNavigationTimer = new Timer();
			_keyNavigationTimer.Interval = 200;
			_keyNavigationTimer.Tick += HandleKeyNavigationTimer;

			_itemFormatCache = new ItemFormatCache();
			if( Core.State != CoreState.Initializing )
			{
				HookFormattingRulesChange();
			}

			Core.ResourceAP.JobStarting += HandleResourceJobStarting;
			Core.ResourceAP.JobFinished += HandleResourceJobFinished;

			_contextProvider = this;
		}

		protected override void Dispose( bool disposing )
		{
			if( _dataProvider != null )
			{
				_dataProvider.Dispose();
				_dataProvider = null;
			}
			if( disposing )
			{
				_keyNavigationTimer.Dispose();
			}
			base.Dispose( disposing );
		}

		public event ResourceDragEventHandler ResourceDragOver;
		public event ResourceDragEventHandler ResourceDrop;
		public event EventHandler KeyNavigationCompleted;
		new public event JetListViewLibrary.HandledEventHandler DoubleClick;
		public event EventHandler ActiveResourceChanged;

		/// <summary>
		/// Occurs when a new resource appears in the list.
		/// </summary>
		public event ResourceEventHandler ResourceAdded;

		/// <summary>
		/// Occurs when the list of selected resources in the resource browser is changed.
		/// </summary>
		public event EventHandler SelectionChanged;

		/// <summary>
		/// Occurs when the user drags the column splitter to change the size of the column.
		/// </summary>
		public event EventHandler ColumnSizeChanged
		{
			add { _jetListView.ColumnResized += value; }
			remove { _jetListView.ColumnResized -= value; }
		}

		/// <summary>
		/// Occurs when the user drags the column header to change the order of the columns.
		/// </summary>
		public event EventHandler ColumnOrderChanged
		{
			add { _jetListView.ColumnOrderChanged += value; }
			remove { _jetListView.ColumnOrderChanged -= value; }
		}

		/// <summary>
		/// Occurs when the user starts in-place editing an item.
		/// </summary>
		public event ResourceItemEditEventHandler BeforeItemEdit;

		/// <summary>
		/// Occurs when the item is edited by the user.
		/// </summary>
		public event ResourceItemEditEventHandler AfterItemEdit;

		public JetListView JetListView
		{
			get { return _jetListView; }
		}

		/// <summary>
		/// Enables or disables executing the double-click action when an item is double-clicked
		/// or Enter is pressed on an item.
		/// </summary>
		[DefaultValue( true )]
		public bool ExecuteDoubleClickAction
		{
			get { return _executeDoubleClickAction; }
			set { _executeDoubleClickAction = value; }
		}

		/// <summary>
		/// The provider which provides the context for keyboard and context menu actions
		/// invoked from the ResourceListView2.
		/// </summary>
		[DefaultValue( null )]
		public IContextProvider ContextProvider
		{
			get { return _contextProvider; }
			set
			{
				if( value == null )
				{
					_contextProvider = this;
				}
				else
				{
					_contextProvider = value;
				}

				foreach( JetListViewColumn col in _jetListView.Columns )
				{
					ResourceListViewCustomColumn customColumn = col as ResourceListViewCustomColumn;
					if( customColumn != null )
					{
						customColumn.ContextProvider = _contextProvider;
					}
				}
			}
		}

		/// <summary>
		/// The <see cref="IResourceDragDropHandler"/> which handles drag & drop on the empty space in the list.
		/// </summary>
		[DefaultValue( null )]
		public IResourceDragDropHandler EmptyDropHandler
		{
			get { return _rootResourceDropHandler; }
			set { _rootResourceDropHandler = value; }
		}

		/// <summary>
		/// Gets or sets the IResourceDataProvider which provides resources to show in the
		/// ResourceListView.
		/// </summary>
		[Browsable( false )]
		[DefaultValue( null )]
		public IResourceDataProvider DataProvider
		{
			get { return _dataProvider; }
			set
			{
				if( _dataProvider != value )
				{
					if( _dataProvider != null )
					{
						_dataProvider.Dispose();
					}
					_dataProvider = value;
					_itemFormatCache.Clear();
					_jetListView.NodeCollection.BeginUpdate();
					try
					{
						_jetListView.Nodes.Clear();
						if( _dataProvider != null )
						{
							_initialFill = true;
							try
							{
								_dataProvider.FillResources( this );
							}
							finally
							{
								_initialFill = false;
							}
						}
					}
					finally
					{
						_jetListView.NodeCollection.EndUpdate();
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the column scheme provider for displaying list view contents
		/// in multiline mode.
		/// </summary>
		[DefaultValue( null )]
		public IColumnSchemeProvider ColumnSchemeProvider
		{
			get { return _jetListView.ColumnSchemeProvider; }
			set { _jetListView.ColumnSchemeProvider = value; }
		}

		/// <summary>
		/// Gets or sets the value indicating whether the list view contents is drawn in
		/// multiline mode.
		/// </summary>
		[DefaultValue( false )]
		public bool MultiLineView
		{
			get { return _jetListView.MultiLineView; }
			set { _jetListView.MultiLineView = value; }
		}

		/// <summary>
		/// Gets or sets the ID of the int property which stores the expanded state of folders in the view.
		/// </summary>
		[Browsable( false )]
		[DefaultValue( -1 )]
		public int OpenProperty
		{
			get
			{
				if( _expandedPropManager != null )
				{
					return _expandedPropManager.PropId;
				}
				return -1;
			}
			set
			{
				if( _expandedPropManager != null )
				{
					_expandedPropManager.Dispose();
				}
				if( value == -1 )
				{
					_expandedPropManager = null;
				}
				else
				{
					_expandedPropManager = new ExpandedPropManager( _jetListView, value );
				}
			}
		}

		[DefaultValue( false )]
		public bool HideSelection
		{
			get { return _jetListView.HideSelection; }
			set { _jetListView.HideSelection = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the selection highlight covers all
		/// columns in the list or just the first column.
		/// </summary>
		public bool FullRowSelect
		{
			get { return _jetListView.FullRowSelect; }
			set { _jetListView.FullRowSelect = value; }
		}

		[DefaultValue( false )]
		public bool SelectAddedItems
		{
			get { return _selectAddedItems; }
			set { _selectAddedItems = value; }
		}

		public ColumnHeaderStyle HeaderStyle
		{
			get { return _jetListView.HeaderStyle; }
			set { _jetListView.HeaderStyle = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the user can drag column headers
		/// to reorder columns in the control.
		/// </summary>
		public bool AllowColumnReorder
		{
			get { return _jetListView.AllowColumnReorder; }
			set { _jetListView.AllowColumnReorder = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the user can drag items in the view.
		/// </summary>
		[DefaultValue( true )]
		public bool AllowDrag
		{
			get { return _allowDrag; }
			set { _allowDrag = value; }
		}

		public bool KeyNavigation
		{
			get { return _keyNavigation; }
		}

		public JetListViewFilterCollection Filters
		{
			get { return _jetListView.Filters; }
		}

		public SelectionModel Selection
		{
			get { return _jetListView.Selection; }
		}

		/// <summary>
		/// Gets or sets the context menu which is shown when a column header is right-clicked.
		/// </summary>
		[DefaultValue( null )]
		public ContextMenu HeaderContextMenu
		{
			get { return _headerContextMenu; }
			set { _headerContextMenu = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether it's allowed to perform drag & drop from a view
		/// to the same view.
		/// </summary>
		[DefaultValue( true )]
		public bool AllowSameViewDrag
		{
			get { return _allowSameViewDrag; }
			set { _allowSameViewDrag = value; }
		}

		/// <summary>
		/// Gets or sets the value indicating whether multiple items can be selected in the list.
		/// </summary>
		[DefaultValue( true )]
		public bool MultiSelect
		{
			get { return _jetListView.MultiSelect; }
			set { _jetListView.MultiSelect = value; }
		}

		/// <summary>
		/// Gets or sets the value indicating whether delimiter lines are drawn between
		/// lines in the list.
		/// </summary>
		[DefaultValue( false )]
		public bool RowDelimiters
		{
			get { return _jetListView.RowDelimiters; }
			set { _jetListView.RowDelimiters = value; }
		}

		/// <summary>
		/// Gets or sets the value indicating whether a context menu is displayed when
		/// a resource is right-clicked in the list.
		/// </summary>
		[DefaultValue( true )]
		public bool ShowContextMenu
		{
			get { return _showContextMenu; }
			set { _showContextMenu = value; }
		}

		public BorderStyle BorderStyle
		{
			get { return _jetListView.BorderStyle; }
			set { _jetListView.BorderStyle = value; }
		}

		/// <summary>
		/// The color used for drawing the JetListView border when BorderStyle.FixedSingle is used.
		/// </summary>
		public Color BorderColor
		{
			get { return _jetListView.BorderColor; }
			set { _jetListView.BorderColor = value; }
		}

		/// <summary>
		/// Gets or sets the background color of unselected group headers in the view.
		/// </summary>
		public Color GroupHeaderColor
		{
			get { return _jetListView.GroupHeaderColor; }
			set { _jetListView.GroupHeaderColor = value; }
		}

		/// <summary>
		/// The text shown in the view when there are no visible items.
		/// </summary>
		[DefaultValue( "There are no items in this view." )]
		[Category( "Appearance" )]
		public string EmptyText
		{
			get { return _jetListView.EmptyText; }
			set { _jetListView.EmptyText = value; }
		}

		/// <summary>
		/// Gets or sets whether resources can be dropped in Insert mode to allow reordering them, when dragging within the same view.
		/// </summary>
		/// <remarks>Same view drag must be allowed by setting <see cref="AllowSameViewDrag"/> to <c>True</c> in order for this function to work.</remarks>
		[DefaultValue( true )]
		[Category( "Behavior" )]
		public bool AllowReorder
		{
			get { return _jetListView.AllowDragInsert; }
			set { _jetListView.AllowDragInsert = value; }
		}

		public ResourceIconColumn AddIconColumn()
		{
			ResourceIconColumn column = new ResourceIconColumn();
			_jetListView.Columns.Add( column );
			return column;
		}

		public CheckBoxColumn AddCheckBoxColumn()
		{
			CheckBoxColumn column = new CheckBoxColumn();
			column.Width = 20;
			_jetListView.Columns.Add( column );
			return column;
		}

		public TreeStructureColumn AddTreeStructureColumn()
		{
			TreeStructureColumn column = new TreeStructureColumn();
			_jetListView.Columns.Add( column );
			return column;
		}

		public ResourceListView2Column AddColumn( int propId )
		{
			return AddColumn( new int[] {propId} );
		}

		public ResourceListView2Column AddColumn( int[] propIds )
		{
			ResourceListView2Column column = new ResourceListView2Column( propIds );
			column.FontCallback = _itemFormatCache.GetItemFont;
			column.ForeColorCallback = _itemFormatCache.GetItemForeColor;
			column.BackColorCallback = _itemFormatCache.GetItemBackColor;
			_jetListView.Columns.Add( column );
			return column;
		}

		public ResourceListViewCustomColumn AddCustomColumn( int[] propIds, ICustomColumn[] customColumns )
		{
			ResourceListViewCustomColumn column = new ResourceListViewCustomColumn( propIds, customColumns );
			column.Width = 20;
			column.ContextProvider = _contextProvider;
			column.BackColorCallback = _itemFormatCache.GetItemBackColor;
			_jetListView.Columns.Add( column );
			return column;
		}

		private void HandleDoubleClick(object sender, JetListViewLibrary.HandledEventArgs e)
		{
			if( DoubleClick != null )
			{
				DoubleClick( this, e );
			}
			if( !e.Handled && Core.State == CoreState.Running && _executeDoubleClickAction )
			{
				IResourceList resList = GetSelectedResources();
				if( resList.Count > 0 )
				{
					bool haveActions = false;
					for( int i = 0; i < resList.Count; i++ )
					{
						IResource res;
						try
						{
							res = resList[ i ];
						}
						catch( StorageException )
						{
							continue;
						}
						if( Core.ActionManager.GetDoubleClickAction( res ) != null )
						{
							haveActions = true;
							break;
						}
					}
					if( haveActions )
					{
						Core.ActionManager.ExecuteDoubleClickAction( resList );
						e.Handled = true;
					}
				}
			}
		}

		private void HandleItemDrag( object sender, ItemDragEventArgs e )
		{
			if( !_allowDrag )
			{
				return;
			}

			DataObject dataObj = new DataObject();
			IResourceList selectedResources = GetSelectedResources();
			dataObj.SetData( typeof(IResourceList), selectedResources );
			dataObj.SetData( typeof(ResourceListView2), this );

			string[] dragResTypes = selectedResources.GetAllTypes();
			if( dragResTypes.Length == 1 )
			{
				IResourceDragDropHandler handler = Core.PluginLoader.GetResourceDragDropHandler( selectedResources[ 0 ] );
				if( handler != null )
				{
					handler.AddResourceDragData( selectedResources, dataObj );
				}
			}

			DoDragDrop( dataObj, DragDropEffects.All | DragDropEffects.Move | DragDropEffects.Link );
		}

		/// <summary>
		/// Handles the DnD <see cref="JetListView.DragOver"/> event from the underlying <see cref="JetListView"/>, and fires the own <see cref="ResourceDragOver"/> event for the external handlers (if none available, invokes the default handler).
		/// </summary>
		private void HandleDragOver( object sender, JetListViewDragEventArgs args )
		{
			try
			{
				IResource targetRes;
				if( !CheckDragDropMode( args, out targetRes ) )
					return; // Operation cancelled

				// Call the outer handler for the drag-over operation
				if( (ResourceDragOver != null) && (args.Data.GetDataPresent( typeof(IResourceList) )) )
				{ // Call the custom handler
					IResourceList dragResources = (IResourceList)args.Data.GetData( typeof(IResourceList) );
					ResourceDragEventArgs resourceDragEventArgs = new ResourceDragEventArgs( targetRes, dragResources );
					ResourceDragOver( this, resourceDragEventArgs );
					args.Effect = resourceDragEventArgs.Effect;
				}
				else
				{ // No custom handler defined, invoke the default handler
					if( (targetRes == RootResource) && (_rootResourceDropHandler != null) ) // Empty/Root special handler
						args.Effect = _rootResourceDropHandler.DragOver( targetRes, args.Data, args.AllowedEffect, args.KeyState );
					else // Normal d'n'd handler
						args.Effect = Core.UIManager.ProcessDragOver( targetRes, args.Data, args.AllowedEffect, args.KeyState, (args.Data.GetData( typeof(ResourceListView2) ) == this) );
				}
			}
			catch( Exception ex )
			{
				Core.ReportException( ex, ExceptionReportFlags.AttachLog );
			}
		}

		/// <summary>
		/// Handles the DnD <see cref="JetListView.DragDrop"/> event from the underlying <see cref="JetListView"/>, and fires the own <see cref="ResourceDrop"/> event for the external handlers (if none available, invokes the default handler).
		/// </summary>
		private void HandleDragDrop( object sender, JetListViewDragEventArgs args )
		{
			try
			{
				////////////
				// Prepare
				JetListViewNode nodeInsertionPoint = args.DropTargetNode; // In tree-insertion-mode, we drop to the parent, and here we store the original target to use as an insertion point
				IResource targetRes;
				if( !CheckDragDropMode( args, out targetRes ) )
					return; // Operation cancelled

				// The resources being dragged
				IResourceList resDragged = args.Data.GetDataPresent( typeof(IResourceList) ) ? (IResourceList)args.Data.GetData( typeof(IResourceList) ) : Core.ResourceStore.EmptyResourceList;
				lock( resDragged ) // Kill the resource list to avoid the changes
					resDragged = Core.ResourceStore.ListFromIds( resDragged.ResourceIds, false );

				////////////
				// Reorder
				// If we were dragging in the insertion mode, change the order
				// Do it beforehand to avoid “jumping” the item when it first gets added to the end and then jumps to the proper place
				if( ((args.DropTargetRenderMode & DropTargetRenderMode.InsertAny) != 0) && (nodeInsertionPoint != null) )
				{
					if( nodeInsertionPoint.Parent != null )
					{
						// Collect IDs of the current set of resources under the node (they may be useful if there's no existing order yet)
						// TODO: lock?
						IntArrayList arOld = new IntArrayList( nodeInsertionPoint.Parent.Nodes.Count );
						foreach( JetListViewNode node in nodeInsertionPoint.Parent.Nodes )
							arOld.Add( ((IResource)node.Data).OriginalId );
						UserResourceOrder uro = new UserResourceOrder(
							nodeInsertionPoint.Parent.Data != null ? (IResource)nodeInsertionPoint.Parent.Data : _resRoot
							, JobPriority.Immediate );
						uro.Insert(
							((IResource)nodeInsertionPoint.Data).OriginalId
							, resDragged.ResourceIds
							, ((args.DropTargetRenderMode & DropTargetRenderMode.InsertBelow) != 0)
							, arOld );
					}
				}

				/////////
				// Drop
				// Invoke the external handler for the drop event
				if( (ResourceDrop != null) && (resDragged.Count > 0) ) // A custom handler is available
					ResourceDrop( this, new ResourceDragEventArgs( targetRes, resDragged ) );
				else
				{ // No custom handler, invoke the default one
					if( (targetRes == RootResource) && (_rootResourceDropHandler != null) ) // Empty/Root special handler
						_rootResourceDropHandler.Drop( targetRes, args.Data, args.AllowedEffect, args.KeyState );
					else // Normal d'n'd handler
						Core.UIManager.ProcessDragDrop( targetRes, args.Data, args.AllowedEffect, args.KeyState );
				}
			}
			catch( Exception ex )
			{
				Core.ReportException( ex, ExceptionReportFlags.AttachLog );
			}
		}

		/// <summary>
		/// Checks if the dragging mode could be switched to Insertion or not, and changes the droptarget to the parent node, if necessary, for the Insertion case.
		/// </summary>
		/// <param name="args">
		/// <para>The original drag'n'drop event arguents coming from the list.</para>
		/// </param>
		/// <param name="resDropTarget">Returns the <see cref="IResource">resource</see> that is either dropped over,
		/// or a parent resource in case of the Insertion mode.
		/// <c>Null</c> for the top-level nodes when the root resource is not defined.</param>
		/// <returns>Whether the drag-drop operation is allowed (<c>True</c>) or not.</returns>
		protected bool CheckDragDropMode( JetListViewDragEventArgs args, out IResource resDropTarget )
		{
			// Dragging within the same control?
			bool sameView = args.Data.GetData( typeof(ResourceListView2) ) == this;

			// Prohibit dragging items within the same control
			if( (!AllowSameViewDrag) && (sameView) )
			{
				args.DropTargetRenderMode = DropTargetRenderMode.Restricted;
				args.Effect = DragDropEffects.None;
				resDropTarget = null;
				return false; // Prohibit
			}

			// Assume the target won't change
			JetListViewNode nodeDropTarget = args.DropTargetNode;

			// Check for special processing for the Insertion mode
			if( args.DropTargetNode != null )
			{ // Over some item
				if( sameView )
				{ // Same view drag, check for possible insertion
					if( (args.DropTargetRenderMode & DropTargetRenderMode.InsertAny) != 0 ) // If it's set to Insert by the list, then reordering is allowed
					{ // In the insertion mode, the drop action applies to the parent of the node over which the cursor is located
						if( args.DropTargetNode.Parent != null ) // Reordering in the tree — switch to parent
							nodeDropTarget = args.DropTargetNode.Parent;
						else // Dragging not in a tree but in a plain list; such scenario is not yet supported
							args.DropTargetRenderMode = DropTargetRenderMode.Over;
					}
				}
				else // Not same view, no insertion allowed
					args.DropTargetRenderMode = DropTargetRenderMode.Over;
			}
			else // Over empty space
				args.DropTargetRenderMode = DropTargetRenderMode.Over;

			// Get the target resource and ensure it's OK
			object dataDropTarget = ((nodeDropTarget == null) || (nodeDropTarget.Data == null)) ? _resRoot : nodeDropTarget.Data;
			resDropTarget = dataDropTarget as IResource;
			if( (resDropTarget == null) != (dataDropTarget == null) ) // If the data type is not IResource
				throw new InvalidOperationException( String.Format( "Invalid data type attached to a resource list/tree view node." ) );

			// If the drop is over the empty space, and the empty-drop-resource (Root) has not been defined
			if( resDropTarget == null )
			{
				args.DropTargetRenderMode = DropTargetRenderMode.Restricted;
				args.Effect = DragDropEffects.None;
				resDropTarget = null;
				return false; // Prohibit
			}

			return true; // Allow
		}

		private void HandleContextMenuInvoked( object sender, MouseEventArgs e )
		{
			if( !IsHandleCreated || !Visible )
			{
				return;
			}

			if( _jetListView.IsColumnHeaderAt( e.X, e.Y ) )
			{
				if( _headerContextMenu != null )
				{
					_headerContextMenu.Show( this, new Point( e.X, e.Y ) );
				}
			}
			else if( _showContextMenu )
			{
				IActionContext actionContext = _contextProvider.GetContext( ActionContextKind.ContextMenu );
				if( _jetListView.GetNodeAt( e.X, e.Y ) == null )
				{
					ActionContext context = actionContext as ActionContext;
					if( context != null )
					{
						context.SetSelectedResources( Core.ResourceStore.EmptyResourceList );
					}
				}
				Core.ActionManager.ShowResourceContextMenu( actionContext,
				                                            this, e.X, e.Y );
			}
		}

		private void HandleKeyDown( object sender, KeyEventArgs e )
		{
			if( Core.State == CoreState.ShuttingDown )
				return;

			OnKeyDown( e );

			if( e.KeyCode == Keys.Up || e.KeyCode == Keys.Down ||
				e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown ||
				e.KeyCode == Keys.Home || e.KeyCode == Keys.End )
			{
				_keyNavigation = true;
				_keyNavigationTimer.Stop();
			}

			if( !e.Handled )
			{
				IActionContext context = _contextProvider.GetContext( ActionContextKind.Keyboard );
				if( Core.ActionManager.ExecuteKeyboardAction( context, e.KeyData ) )
				{
					e.Handled = true;
				}
				else if( e.KeyCode == Keys.Enter && e.Modifiers == 0 && _executeDoubleClickAction )
				{
					Core.ActionManager.ExecuteDoubleClickAction( context.SelectedResources );
				}
			}
		}

		private void HandleKeyUp( object sender, KeyEventArgs e )
		{
			if( _keyNavigation )
			{
				_keyNavigation = false;
				_keyNavigationTimer.Stop();
				_keyNavigationTimer.Start();
			}
		}

		private void HandleKeyNavigationTimer( object sender, EventArgs e )
		{
			_keyNavigationTimer.Stop();
			if( KeyNavigationCompleted != null )
			{
				KeyNavigationCompleted( this, EventArgs.Empty );
			}
		}

		public IActionContext GetContext( ActionContextKind kind )
		{
			if( _contextProvider != null && _contextProvider != this )
			{
				return _contextProvider.GetContext( kind );
			}
			ActionContext context = new ActionContext( kind, null, GetSelectedResources() );
			context.SetOwnerForm( FindForm() );
			return context;
		}

        public IResourceList GetSelectedResources()
        {
            List<int> ids = new List<int> ();
            lock( _jetListView.Selection.SelectionLock )
            {
                foreach( IResource res in _jetListView.Selection )
                {
                    if( !res.IsDeleted )
                    {
                        ids.Add( res.Id );
                    }
                }
            }
            return Core.ResourceStore.ListFromIds( ids, false );
        }

		public JetListViewColumnCollection Columns
		{
			get { return _jetListView.Columns; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the user can edit the texts of resources in the tree.
		/// </summary>
		public bool InPlaceEdit
		{
			get { return _jetListView.InPlaceEditor != null; }
			set
			{
				if( value )
				{
					_jetListView.InPlaceEditor = _inPlaceEditor;
				}
				else
				{
					_jetListView.InPlaceEditor = null;
				}
			}
		}

		/// <summary>
		/// Begins in-place editing of the text of the specified resource.
		/// </summary>
		/// <param name="res">The resource to edit.</param>
		public void EditResourceLabel( IResource res )
		{
			if( res == null )
			{
				throw new ArgumentNullException( "res" );
			}

			JetListViewNode node = _jetListView.NodeCollection.NodeFromItem( res );
			if( node == null )
			{
				if( !_dataProvider.FindResourceNode( res ) )
				{
					throw new ArgumentException( "Resource not found in tree", "res" );
				}
				node = _jetListView.NodeCollection.NodeFromItem( res );
			}

			_jetListView.InPlaceEditNode( node );
		}

		/// <summary>
		/// Checks if the text of the specified resource can be in-place edited.
		/// </summary>
		/// <param name="res">The resource to edit.</param>
		/// <param name="text">The text that will be displayed in the in-place edit box.</param>
		/// <returns>true if the resource can be edited, false otherwise.</returns>
		public bool CanEditResourceLabel( IResource res, ref string text )
		{
			IResourceRenameHandler renameHandler = Core.PluginLoader.GetResourceRenameHandler( res );
			if( renameHandler != null )
			{
				return renameHandler.CanRenameResource( res, ref text );
			}

			IResourceUIHandler uiHandler = Core.PluginLoader.GetResourceUIHandler( res );
			if( uiHandler != null )
			{
				return uiHandler.CanRenameResource( res );
			}

			return false;
		}

		private void HandleBeforeItemEdit( object sender, JetItemEditEventArgs e )
		{
			if( BeforeItemEdit != null )
			{
				ResourceItemEditEventArgs args = new ResourceItemEditEventArgs( e.Text, (IResource)e.Item, e.Column );
				BeforeItemEdit( this, args );
				e.Text = args.Text;
				e.CancelEdit = args.CancelEdit;
			}
			else if( AfterItemEdit == null )
			{
				string text = e.Text;
				if( CanEditResourceLabel( (IResource)e.Item, ref text ) )
				{
					e.Text = text;
				}
				else
				{
					e.CancelEdit = true;
				}
			}
		}

		private void HandleAfterItemEdit( object sender, JetItemEditEventArgs e )
		{
			if( e.Text != null )
			{
				IResource res = (IResource)e.Item;
				if( AfterItemEdit != null )
				{
					ResourceItemEditEventArgs args = new ResourceItemEditEventArgs( e.Text, res, e.Column );
					AfterItemEdit( this, args );
				}
				else if( BeforeItemEdit == null )
				{
					IResourceRenameHandler renameHandler = Core.PluginLoader.GetResourceRenameHandler( res );
					if( renameHandler != null )
					{
						renameHandler.ResourceRenamed( res, e.Text );
					}
					else
					{
						IResourceUIHandler uiHandler = Core.PluginLoader.GetResourceUIHandler( res );
						if( uiHandler != null )
						{
							uiHandler.ResourceRenamed( res, e.Text );
						}
					}
				}
			}
		}

		private void HandleNodeAdded( object sender, JetListViewNodeEventArgs e )
		{
			if( _selectAddedItems && !_initialFill )
			{
				_jetListView.Selection.Clear();
				_jetListView.Selection.Add( e.Node.Data );
			}
			if( ResourceAdded != null )
			{
				ResourceAdded( this, new ResourceEventArgs( (IResource)e.Node.Data ) );
			}
		}

		public IResource ActiveResource
		{
			get
			{
				JetListViewNode node = _jetListView.Selection.ActiveNode;
				if( node != null )
				{
					return (IResource)node.Data;
				}
				return null;
			}
		}

		public JetListViewNode NodeFromItem( IResource item )
		{
			return _jetListView.NodeCollection.NodeFromItem( item );
		}

		public IEnumerator EnumerateItemsForward( JetListViewNode node )
		{
			if( node == null )
			{
				node = _jetListView.Nodes[ 0 ];
			}
			return _jetListView.NodeCollection.EnumerateNodesForward( node );
		}

		public IEnumerator EnumerateItemsBackward( JetListViewNode node )
		{
			if( node == null )
			{
				node = _jetListView.Nodes[ 0 ];
			}
			return _jetListView.NodeCollection.EnumerateNodesBackward( node );
		}

		private void HandleItemUpdated( object sender, ItemEventArgs e )
		{
			_itemFormatCache.InvalidateFormat( e.Item );
		}

		private void HandleActiveNodeChanged( object sender, JetListViewNodeEventArgs e )
		{
			if( Core.UserInterfaceAP.IsOwnerThread )
			{
				OnActiveResourceChanged();
			}
			else
			{
				Core.UserInterfaceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( OnActiveResourceChanged ) );
			}
		}

		private void OnActiveResourceChanged()
		{
			if( ActiveResourceChanged != null )
			{
				ActiveResourceChanged( this, EventArgs.Empty );
			}
		}

		private void HandleSelectionStateChanged( object sender, StateChangeEventArgs e )
		{
			if( SelectionChanged != null )
			{
				SelectionChanged( this, EventArgs.Empty );
			}
		}

        //---------------------------------------------------------------------
        //  This method is just a facade for outsiders in order to hide the
        //  JetListView and SelectionModel as much as possible. By 8.02.07 there
        //  are many other methods which should be implemented to hide the JLV
        //  completely.
        //---------------------------------------------------------------------
        public void SelectSingleItem( IResource res )
        {
            _jetListView.Selection.SelectSingleItem( res );
        }

        public void SelectAll()
		{
			_jetListView.Selection.SelectAll();
		}

		public int VisibleItemCount
		{
			get { return _jetListView.NodeCollection.VisibleItemCount; }
		}

		/// <summary>
		/// Calls the specified delegate for every node in the view.
		/// </summary>
		/// <param name="resourceDelegate">The delegate to call.</param>
		public void ForEachNode( ResourceDelegate resourceDelegate )
		{
			if( _jetListView.Nodes.Count > 0 )
			{
				IEnumerator enumerator = _jetListView.NodeCollection.EnumerateNodesForward( _jetListView.Nodes[ 0 ] );
				while( enumerator.MoveNext() )
				{
					JetListViewNode node = (JetListViewNode)enumerator.Current;
					resourceDelegate( (IResource)node.Data );
				}
			}
		}

		public IResource LocateNextResource( IResource startResource, LocateMatchCallback callback,
                                             bool skipFirst, bool searchBackAlso )
		{
            return LocateNextResourceInDirection( true, startResource, callback, skipFirst, searchBackAlso );
		}

		public IResource LocatePrevResource( IResource startResource, LocateMatchCallback callback,
                                             bool skipFirst, bool searchBackAlso )
		{
            return LocateNextResourceInDirection( false, startResource, callback, skipFirst, searchBackAlso );
		}

		private IResource LocateNextResourceInDirection( bool isForward, IResource start, LocateMatchCallback callback,
                                                         bool skipFirst, bool searchBackAlso )
		{
            IResource result = null;
			JetListViewNode node = NodeFromItem( start );
			if( node != null )
			{
				lock( JetListView.NodeCollection )
				{
					result = SearchEnumerator( isForward, node, null, callback, skipFirst );
					if( result == null && searchBackAlso )
					    result = SearchEnumerator( isForward, null, node, callback, false );
				}
			}
			return result;
		}

		private IResource SearchEnumerator( bool forward, JetListViewNode node, JetListViewNode stopNode,
		                                    LocateMatchCallback callback, bool skipFirst )
		{
			IEnumerator enumerator = forward? EnumerateItemsForward( node ) : EnumerateItemsBackward( node ) ;
			if( skipFirst )
			{
				enumerator.MoveNext();
			}
			while( enumerator.MoveNext() )
			{
				JetListViewNode childNode = (JetListViewNode)enumerator.Current;
				if( childNode == stopNode )
				{
					return null;
				}
				IResource childResource = (IResource)childNode.Data;
				if( callback( childResource ) )
				{
					return childResource;
				}
			}
			return null;
		}

		public void HookFormattingRulesChange()
		{
			_itemFormatCache.HookFormattingRulesChange();
		}

		public bool CanExecuteCommand( string command )
		{
			string text = "";
			switch( command )
			{
			case DisplayPaneCommands.SelectAll:
				return true;

			case DisplayPaneCommands.RenameSelection:
				return Selection.Count == 1 && CanEditResourceLabel( SelectedResource, ref text );

			case DisplayPaneCommands.Copy:
				return Selection.Count > 0;
			}
			return false;
		}

		public void ExecuteCommand( string command )
		{
			switch( command )
			{
			case DisplayPaneCommands.SelectAll:
				SelectAll();
				break;

			case DisplayPaneCommands.RenameSelection:
				if( Selection.Count == 1 )
				{
					EditResourceLabel( SelectedResource );
				}
				break;

			case DisplayPaneCommands.Copy:
				_jetListView.CopySelection();
				break;
			}
		}

		private IResource SelectedResource
		{
            get
            {
                IEnumerator enumerator = Selection.GetEnumerator();
                enumerator.MoveNext();
                return (IResource)enumerator.Current;
            }
		}

		private void HandleResourceJobStarting( object sender, EventArgs e )
		{
			if( !_inResourceOperation )
			{
				_jetListView.NodeCollection.BeginUpdate();
				_inResourceOperation = true;
			}
		}

		private void HandleResourceJobFinished( object sender, EventArgs e )
		{
			if( _inResourceOperation )
			{
				try
				{
					_jetListView.NodeCollection.EndUpdate();
				}
				finally
				{
					_inResourceOperation = false;
				}
			}
		}

		/// <summary>
		/// Gets or sets the resource that is treated as a root for the tree or a list view.
		/// </summary>
		public virtual IResource RootResource
		{
			get { return _resRoot; }
			set
			{
				_resRoot = value; // TODO: empty-drop-handler sync
			}
		}
	}

	/// <summary>
	/// A column which displays a number of resource properties.
	/// </summary>
	public class ResourcePropsColumn : JetListViewColumn
	{
		protected int[] _propIds;
		private IResourceComparer _customComparer;
		private IGroupProvider _groupProvider;

		protected ResourcePropsColumn( int[] propIds )
		{
			_propIds = propIds;
		}

		public int[] PropIds
		{
			get { return _propIds; }
		}

		public IResourceComparer CustomComparer
		{
			get { return _customComparer; }
			set { _customComparer = value; }
		}

		/// <summary>
		/// The provider which is used for grouping data in the column.
		/// </summary>
		public IGroupProvider GroupProvider
		{
			get { return _groupProvider; }
			set { _groupProvider = value; }
		}

		/// <summary>
		/// Compares the specified array of property IDs to the array of property IDs displayed
		/// in the column.
		/// </summary>
		/// <param name="propIds">The property IDs to compare with.</param>
		/// <returns>true if the column shows the specified properties, false if the list of properties
		/// is different.</returns>
		public bool PropIdsEqual( int[] propIds )
		{
			if( propIds.Length != _propIds.Length )
			{
				return false;
			}
			for( int i = 0; i < propIds.Length && i < _propIds.Length; i++ )
			{
				if( propIds[ i ] != _propIds[ i ] )
				{
					return false;
				}
			}
			return true;
		}
	}

	public interface IResourceDataProvider : IDisposable
	{
		void FillResources( ResourceListView2 resourceListView );
		bool FindResourceNode( IResource res );
	}

	public class OmeaMethodInvoker : IControlMethodInvoker
	{
		public void BeginInvoke( Delegate method, params object[] args )
		{
			Core.UIManager.QueueUIJob( method, args );
		}

		public bool InvokeRequired
		{
			get { return !Core.UserInterfaceAP.IsOwnerThread; }
		}
	}

	public class ResourceItemEditEventArgs
	{
	    private readonly IResource _resource;
		private readonly JetListViewColumn _column;

	    public ResourceItemEditEventArgs( string text, IResource resource, JetListViewColumn column )
		{
			Text = text;
			_resource = resource;
			_column = column;
			CancelEdit = false;
		}

	    public string Text { get; set; }

	    public IResource Resource {  get { return _resource; }  }

		public JetListViewColumn Column {  get { return _column; }  }

	    public bool CancelEdit { get; set; }
	}

	public delegate void ResourceItemEditEventHandler( object sender, ResourceItemEditEventArgs e );
}
