// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;

namespace JetBrains.JetListViewLibrary
{
    /// <summary>
    /// Provides data for events which have a JetListViewNode argument.
    /// </summary>
    public class JetListViewNodeEventArgs: EventArgs
    {
        private JetListViewNode _node;

        public JetListViewNodeEventArgs( JetListViewNode node )
        {
            _node = node;
        }

        public JetListViewNode Node
        {
            get { return _node; }
        }
    }

    public delegate void JetListViewNodeEventHandler( object sender, JetListViewNodeEventArgs e );

    /// <summary>
    /// Provides data for the RequestChildren event.
    /// </summary>
    public class RequestChildrenEventArgs: JetListViewNodeEventArgs
    {
        private RequestChildrenReason _reason;
        private bool _handled = true;

        public RequestChildrenEventArgs( JetListViewNode node, RequestChildrenReason reason )
            : base( node )
        {
            _reason = reason;
        }

        public RequestChildrenReason Reason
        {
            get { return _reason; }
        }

        public bool Handled
        {
            get { return _handled; }
            set { _handled = value; }
        }
    }

    public delegate void RequestChildrenEventHandler( object sender, RequestChildrenEventArgs e );

    internal class RequestScrollEventArgs: EventArgs
    {
        private int _coord;

        public RequestScrollEventArgs( int coord )
        {
            _coord = coord;
        }

        public int Coord
        {
            get { return _coord; }
        }
    }

    internal delegate void RequestScrollEventHandler( object sender, RequestScrollEventArgs e );

    internal class InvalidateEventArgs: EventArgs
    {
        private int _startY;
        private int _endY;

        public InvalidateEventArgs( int startY, int endY )
        {
            _startY = startY;
            _endY = endY;
        }

        public int StartY
        {
            get { return _startY; }
        }
        public int EndY
        {
            get { return _endY; }
        }
    }

    internal delegate void InvalidateEventHandler( object sender, InvalidateEventArgs e );

    public class RowHeightChangedEventArgs: EventArgs
    {
        private JetListViewNode _row;
        private int _oldHeight;
        private int _newHeight;

        public RowHeightChangedEventArgs( JetListViewNode row, int oldHeight, int newHeight )
        {
            _row = row;
            _oldHeight = oldHeight;
            _newHeight = newHeight;
        }

        public JetListViewNode Row
        {
            get { return _row; }
        }

        public int OldHeight
        {
            get { return _oldHeight; }
        }

        public int NewHeight
        {
            get { return _newHeight; }
        }
    }

    public delegate void RowHeightChangedEventHandler( object sender, RowHeightChangedEventArgs e );

    public class ColumnEventArgs
    {
        private JetListViewColumn _column;

        public ColumnEventArgs( JetListViewColumn column )
        {
            _column = column;
        }

        public JetListViewColumn Column
        {
            get { return _column; }
        }
    }

    public delegate void ColumnEventHandler( object sender, ColumnEventArgs e );

    /// <summary>
    /// Provides data for an event related to a group header.
    /// </summary>
    internal class GroupEventArgs: EventArgs
    {
        private GroupHeaderNode _groupHeader;

        public GroupEventArgs( GroupHeaderNode header )
        {
            _groupHeader = header;
        }

        public GroupHeaderNode GroupHeader
        {
            get { return _groupHeader; }
        }
    }

    internal delegate void GroupEventHandler( object sender, GroupEventArgs e );

    public class HandledEventArgs: EventArgs
    {
        private bool _handled = false;

        public bool Handled
        {
            get { return _handled; }
            set { _handled = value; }
        }
    }

    public delegate void HandledEventHandler( object sender, HandledEventArgs e );

    public class ItemEventArgs: EventArgs
    {
        private object _item;

        public ItemEventArgs( object item )
        {
            _item = item;
        }

        public object Item
        {
            get { return _item; }
        }
    }

    public delegate void ItemEventHandler( object sender, ItemEventArgs e );

    internal class ViewNodeEventArgs: EventArgs
    {
        private IViewNode _viewNode;

        [DebuggerStepThrough]
            public ViewNodeEventArgs( IViewNode viewNode )
        {
            _viewNode = viewNode;
        }

        public IViewNode ViewNode
        {
            [DebuggerStepThrough] get { return _viewNode; }
        }
    }

    internal delegate void ViewNodeEventHandler( object sender, ViewNodeEventArgs e );

    internal class ViewNodeStateChangeEventArgs: ViewNodeEventArgs
    {
        private bool _state;

        public ViewNodeStateChangeEventArgs( IViewNode viewNode, bool state )
            : base( viewNode )
        {
            _state = state;
        }

        public bool State
        {
            get { return _state; }
        }
    }

    internal delegate void ViewNodeStateChangeEventHandler( object sender, ViewNodeStateChangeEventArgs e );

    public class StateChangeEventArgs: EventArgs
    {
        private JetListViewNode _node;
        private bool _state;

        public StateChangeEventArgs( JetListViewNode node, bool state )
        {
            _node = node;
            _state = state;
        }

        public JetListViewNode Node
        {
            get { return _node; }
        }

        public bool State
        {
            get { return _state; }
        }
    }

    public delegate void StateChangeEventHandler( object sender, StateChangeEventArgs e );

    /// <summary>
    /// Provides data for drag and drop events in JetListView.
    /// </summary>
    public class JetListViewDragEventArgs: DragEventArgs
    {
    	private JetListViewNode _dropTargetNode;
    	private DropTargetRenderMode _dropTargetRenderMode;
    	private readonly int _localY;
    	private readonly int _rowHeight;

    	public JetListViewDragEventArgs( DragEventArgs e, JetListViewNode dropTargetNode, DropTargetRenderMode dropTargetRenderMode, int localY, int rowHeight )
    		: base( e.Data, e.KeyState, e.X, e.Y, e.AllowedEffect, e.Effect )
    	{
    		_dropTargetNode = dropTargetNode;
    		_dropTargetRenderMode = dropTargetRenderMode;
    		_localY = localY;
    		_rowHeight = rowHeight;
    	}

    	/// <summary>
    	/// Returns the node over which the mouse is currently located.
    	/// </summary>
    	public JetListViewNode DropTargetNode
    	{
    		get { return _dropTargetNode; }
    	}

    	/// <summary>
    	/// Gets the suggested drop target mode, which is <see cref="DropTargetRenderMode.Over"/> by default, but may be <see cref="DropTargetRenderMode.Restricted"/> when dropping over empty space or <see cref="DropTargetRenderMode.InsertAbove"/>/<see cref="DropTargetRenderMode.InsertBelow"/> if dropping in between the elements and insert mode is allowed by the <see cref="JetListView.AllowDragInsert"/> property.
    	/// Sets the desired droptarget visual cue rendering mode, which may be overrided by <see cref="DropTargetRenderMode.Restricted"/> in case the <see cref="Effect"/> property is set to <see cref="DragDropEffects.None"/> and <see cref="JetListView.AutoRestrictDropTarget"/> is turned on.
    	/// If this property is set to <see cref="DropTargetRenderMode.InsertAny"/>, then the caller resolves it to either <see cref="DropTargetRenderMode.InsertAbove"/> or <see cref="DropTargetRenderMode.InsertBelow"/>, depending on the vertical position of the mouse pointer (<see cref="LocalY"/>) within the row.
    	/// </summary>
    	public DropTargetRenderMode DropTargetRenderMode
    	{
    		get { return _dropTargetRenderMode; }
    		set { _dropTargetRenderMode = value; }
    	}

    	/// <summary>
    	/// Gets the y-coordinate of the mouse pointer, relative to the hovered row represented by <see cref="DropTargetNode"/>.
    	/// </summary>
    	public int LocalY
    	{
    		get { return _localY; }
    	}

    	/// <summary>
    	/// Gets the height of the row over which the dragging is performed.
    	/// If there is no such row (eg empty space drop), returns <c>0</c>.
    	/// </summary>
    	public int RowHeight
    	{
    		get { return _rowHeight; }
    	}
    }

	public delegate void JetListViewDragEventHandler( object sender, JetListViewDragEventArgs e );

    /// <summary>
    /// Provides data for the MultipleNodesChanged event handler.
    /// </summary>
    public class MultipleNodesChangedEventArgs: EventArgs
    {
        private HashSet _addedNodes;
        private HashSet _removedNodes;
        private HashSet _changedNodes;

        public MultipleNodesChangedEventArgs( HashSet addedNodes, HashSet removedNodes, HashSet changedNodes )
        {
            _addedNodes = addedNodes;
            _removedNodes = removedNodes;
            _changedNodes = changedNodes;
        }

        public HashSet AddedNodes
        {
            get { return _addedNodes; }
        }

        public HashSet RemovedNodes
        {
            get { return _removedNodes; }
        }

        public HashSet ChangedNodes
        {
            get { return _changedNodes; }
        }
    }

    public delegate void MultipleNodesChangedEventHandler( object sender, MultipleNodesChangedEventArgs e );

    public class ItemMouseEventArgs: EventArgs
    {
        private object _item;
        private int _x;
        private int _y;
        private bool _handled = false;

        public ItemMouseEventArgs( object item, int x, int y )
        {
            _item = item;
            _x = x;
            _y = y;
        }

        public object Item
        {
            get { return _item; }
        }

        public int X
        {
            get { return _x; }
        }

        public int Y
        {
            get { return _y; }
        }

        public bool Handled
        {
            get { return _handled; }
            set { _handled = value; }
        }
    }

    public delegate void ItemMouseEventHandler( object sender, ItemMouseEventArgs e );

    public delegate string ItemTextCallback( object item );
    public delegate FontStyle ItemFontCallback( object item );
    public delegate Color ItemColorCallback( object item );
    public delegate Cursor ItemCursorCallback( object item );
}
