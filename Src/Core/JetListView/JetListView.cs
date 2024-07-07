// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.UI.Interop;
using SP.Windows;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// Summary description for JetListView2.
	/// </summary>
	public class JetListView: Control
	{
	    private JetListViewFilterCollection _filters;
        private JetListViewNodeCollection _items;
        private JetListViewColumnCollection _columns;
        private RowListRenderer _rowListRenderer;
        private RowRendererBase _baseRowRenderer;
        private IRowRenderer _rowRenderer;
        private HScrollBar _hScrollbar;
        private VScrollBar _vScrollbar;
        private SelectionModel _selectionModel;
        private IControlPainter _controlPainter;
        private BorderStyle _borderStyle = BorderStyle.None;
        private Timer _dragScrollTimer;
        private Timer _dragExpandTimer;
        private Timer _inPlaceEditTimer;
        private bool _dragScrollUp;
        private IControlMethodInvoker _invoker;
        private IInPlaceEditor _inPlaceEditor;
        private IncrementalSearcher _incSearcher;
        private ItemToolTip _itemToolTip;
        private JetListViewNode _dragNode;
        private Rectangle _dragRect;
        private bool _startInPlaceEditTimer;
        private Header _header;
        private System.Windows.Forms.ImageList _headerImgList;
        private System.ComponentModel.IContainer components;
	    private ColumnHeaderStyle _headerStyle = ColumnHeaderStyle.None;
        private int _pendingScroll = -1;
        private string _emptyText = DefaultEmptyText;
        private bool _autoToolTips = true;
        private bool _fullRowSelect = false;
        private JetListViewPreviewColumn _autoPreviewColumn;
	    private bool _lastKeyDownHandled;
        private IColumnSchemeProvider _columnSchemeProvider;
	    private IGroupProvider _groupProvider;
        private SizeF _scaleFactor = new SizeF( 1.0f, 1.0f );
        private Color _borderColor = Color.Black;
        private Color _groupHeaderColor = SystemColors.Control;
        private bool _captureFocus = false;
        private bool _focusLeft = false;
        private JetListViewColumn _dragOverColumn;
		private bool _allowDragInsert = false;
		private bool _autoRestrictDropTarget = true;

        private const int _headerHeight = 18;

        public const string DefaultEmptyText = "There are no items in this view.";

	    public JetListView()
	    {
            _filters = new JetListViewFilterCollection();
	        _items = new JetListViewNodeCollection( _filters );
            _columns = new JetListViewColumnCollection();
            _columns.OwnerControl = this;
            SetSelectionModel( new MultipleSelectionModel( _items ) );
            _rowListRenderer = new RowListRenderer( _items, _selectionModel );
            SetBaseRowRenderer( new SingleLineRowRenderer( _columns, _items ) );
            _baseRowRenderer.RowHeight = 17;
            _incSearcher = new IncrementalSearcher( _items, _rowListRenderer, _selectionModel );

            _items.ChildrenRequested += HandleChildrenRequested;

            _rowListRenderer.ScrollRangeChanged += HandleScrollRangeChanged;
            _rowListRenderer.Invalidate += HandleInvalidateRows;
            _rowListRenderer.RequestVerticalScroll += HandleRequestVerticalScroll;

	        InitializeComponent();

            _header = new Header();
            _header.Height = _headerHeight;
            _header.Visible = false;
            //_header.FullDragSections = true;
            _header.ImageList = _headerImgList;
            Controls.Add( _header );

            _hScrollbar = new HScrollBar();
            _hScrollbar.ValueChanged += HandleHorizontalScroll ;
            _hScrollbar.SmallChange = 10;
	        _hScrollbar.TabStop = false;
            Controls.Add( _hScrollbar );

	        _vScrollbar = new VScrollBar();
	        _vScrollbar.ValueChanged += HandleVerticalScroll;
	        _vScrollbar.SmallChange = _baseRowRenderer.RowHeight;
	        _vScrollbar.TabStop = false;
            Controls.Add( _vScrollbar );

            SetStyle( ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                      ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true );

            _controlPainter = new DefaultControlPainter();
            _columns.ControlPainter = _controlPainter;
            _columns.Font = Font;

            BackColor = SystemColors.Window;
            BorderStyle = BorderStyle.Fixed3D;

            _dragScrollTimer = new Timer();
            _dragScrollTimer.Interval = 150;
            _dragScrollTimer.Tick += HandleDragScroll;

            _dragExpandTimer = new Timer();
            _dragExpandTimer.Interval = 1500;
            _dragExpandTimer.Tick += HandleDragExpand;

            _inPlaceEditTimer = new Timer();
            _inPlaceEditTimer.Interval = SystemInformation.DoubleClickTime + 10;
            _inPlaceEditTimer.Tick += HandleInPlaceEditTimer;

            _invoker = new ControlMethodInvoker( this );

            SetRowRenderer( _baseRowRenderer );

            _itemToolTip = new ItemToolTip( this );
	    }

	    protected override void Dispose( bool disposing )
	    {
	        if ( disposing )
	        {
                _columns.Dispose();
                _dragScrollTimer.Dispose();
                _dragExpandTimer.Dispose();
                _inPlaceEditTimer.Dispose();
                _itemToolTip.Dispose();
                _header.Dispose();
	        }
            base.Dispose( disposing );
	    }

        private void InitializeComponent()
        {
            components = new Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager( typeof(JetListView) );
            _headerImgList = new ImageList( components );
            //
            // _headerImgList
            //
            _headerImgList.ImageSize = new Size(14, 14);
            _headerImgList.ImageStream = ((ImageListStreamer)( resources.GetObject( "_headerImgList.ImageStream" ) ) );
            _headerImgList.TransparentColor = Color.Transparent;
            //
            // JetListView
            //
            Name = "JetListView";
        }

	    /// <summary>
	    /// Occurs when the user starts dragging an item from the list.
	    /// </summary>
        public event ItemDragEventHandler ItemDrag;

        /// <summary>
        /// Occurs when an object is dragged over the control's bounds.
        /// </summary>
        public new event JetListViewDragEventHandler DragOver;

        /// <summary>
        /// Occurs when a drag-and-drop operation is completed.
        /// </summary>
        public new event JetListViewDragEventHandler DragDrop;

        /// <summary>
        /// Fired when a context menu is invoked on a list node.
        /// </summary>
        public event MouseEventHandler ContextMenuInvoked;

        /// <summary>
        /// Occurs when a list node with virtual children is expanded.
        /// </summary>
        public event RequestChildrenEventHandler ChildrenRequested;

        /// <summary>
        /// Occurs when a list node is double-clicked. Allows to cancel the internal
        /// double-click processing of the control.
        /// </summary>
        public new event HandledEventHandler DoubleClick;

        /// <summary>
        /// Occurs when the UpdateItem() method is called to update an item in the control.
        /// </summary>
        public event ItemEventHandler ItemUpdated;

        /// <summary>
        /// Occurs when the user drags the column splitter to change the size of the column.
        /// </summary>
        public event EventHandler ColumnResized;

        /// <summary>
        /// Occurs when the user drags the column header to change the order of the columns.
        /// </summary>
        public event EventHandler ColumnOrderChanged;

        /// <summary>
        /// Occurs when the header of a column is clicked.
        /// </summary>
        public event ColumnEventHandler ColumnClick;

        public event StateChangeEventHandler SelectionStateChanged;
        public event StateChangeEventHandler FocusStateChanged;
        public event JetListViewNodeEventHandler ActiveNodeChanged;

	    protected override void ScaleCore( float dx, float dy )
	    {
	        base.ScaleCore( dx, dy );
            _scaleFactor = new SizeF( dx, dy );
            _vScrollbar.Width = SystemInformation.VerticalScrollBarWidth;
            _hScrollbar.Height = SystemInformation.HorizontalScrollBarHeight;
            UpdateBorderSize();
	    }

	    private void SetBaseRowRenderer( RowRendererBase rowRenderer )
        {
            if ( _baseRowRenderer != null )
            {
                _baseRowRenderer.ColumnClick -= ForwardColumnClick;
                _baseRowRenderer.ColumnOrderChanged -= ForwardColumnOrderChanged;
                _baseRowRenderer.ColumnResized -= ForwardColumnResized;
                _baseRowRenderer.Dispose();
            }
            _baseRowRenderer = rowRenderer;
            _baseRowRenderer.ColumnClick += ForwardColumnClick;
            _baseRowRenderer.ColumnOrderChanged += ForwardColumnOrderChanged;
            _baseRowRenderer.ColumnResized += ForwardColumnResized;
        }

        private void SetRowRenderer( IRowRenderer rowRenderer )
        {
            if ( _rowRenderer != null )
            {
                _rowRenderer.RequestScroll -= HandleRequestHorizontalScroll;
                _rowRenderer.ScrollRangeChanged -= HandleScrollRangeChanged;
                _rowRenderer.Invalidate -= HandleInvalidateAll;
                _rowRenderer.HeaderControl = null;
            }
            _rowRenderer = rowRenderer;
            _rowRenderer.RequestScroll += HandleRequestHorizontalScroll;
            _rowRenderer.ScrollRangeChanged += HandleScrollRangeChanged;
            _rowRenderer.Invalidate += HandleInvalidateAll;
            _rowRenderer.OwnerControl   = this;
            _rowRenderer.HeaderControl  = _header;
            _rowRenderer.MethodInvoker  = _invoker;
            _rowRenderer.ControlPainter = _controlPainter;
            _rowRenderer.FullRowSelect  = _fullRowSelect;
            _rowRenderer.ScrollOffset   = _hScrollbar.Value;

            _rowListRenderer.RowRenderer = _rowRenderer;
            _incSearcher.RowRenderer = _rowRenderer;

            UpdateBorderSize();
        }

	    private void UpdateRowRenderer()
        {
            _baseRowRenderer.RowHeight = 17;
            if ( _autoPreviewColumn == null )
            {
                SetRowRenderer( _baseRowRenderer );
            }
            else
            {
                AutoPreviewRowRenderer renderer = new AutoPreviewRowRenderer( _baseRowRenderer,
                    _autoPreviewColumn, _columns );
                SetRowRenderer( renderer );
            }
            if ( _selectionModel.FocusNode != null )
            {
                _rowListRenderer.ScrollInView( _selectionModel.FocusNode );
            }
        }

        private void HandleHorizontalScroll( object sender, EventArgs e )
	    {
	        CloseInPlaceEdit();
	        _rowRenderer.ScrollOffset = _hScrollbar.Value;
            if ( _headerStyle != ColumnHeaderStyle.None )
            {
                _header.Left = _controlPainter.GetListViewBorderSize( _borderStyle ) -_hScrollbar.Value;
            }
            Invalidate();
	    }

	    private void CloseInPlaceEdit()
	    {
            _inPlaceEditTimer.Stop();
            if ( _inPlaceEditor != null )
	        {
	            _inPlaceEditor.CloseEdit( true );
	        }
	    }

	    private void HandleVerticalScroll( object sender, EventArgs e )
	    {
            CloseInPlaceEdit();
            if ( _rowListRenderer.ScrollOffset != _vScrollbar.Value )
            {
                _rowListRenderer.ScrollOffset = _vScrollbar.Value;
                Invalidate();
            }
	    }

        private void HandleInvalidateRows( object sender, InvalidateEventArgs e )
        {
            Invalidate( new Rectangle( 0, e.StartY, ClientRectangle.Width, e.EndY - e.StartY ) );
        }

        private void HandleRequestHorizontalScroll( object sender, RequestScrollEventArgs e )
        {
            Trace.Assert( e.Coord >= 0 );
            if ( e.Coord <= _hScrollbar.Maximum - _hScrollbar.LargeChange + 1 )
            {
                _hScrollbar.Value = e.Coord;
            }
        }

        private void HandleRequestVerticalScroll( object sender, RequestScrollEventArgs e )
        {
            Trace.Assert( e.Coord >= 0 );
            if ( e.Coord <= _vScrollbar.Maximum - _vScrollbar.LargeChange + 1 )
            {
                _pendingScroll = -1;
                _vScrollbar.Value = e.Coord;
            }
            else
            {
                _pendingScroll = e.Coord;
            }
        }

        private void HandleInvalidateAll( object sender, EventArgs e )
        {
            Invalidate();
        }

        public ChildNodeCollection Nodes
        {
            get { return _items.Nodes; }
        }

        public INodeCollection NodeCollection
        {
            get { return _items; }
        }

        public JetListViewNode Root
        {
            get { return _items.Root; }
        }

	    public JetListViewColumnCollection Columns
	    {
	        get { return _columns; }
	    }

        public SelectionModel Selection
        {
            get { return _selectionModel; }
        }

	    public JetListViewFilterCollection Filters
	    {
	        get { return _filters; }
	    }

#if DEBUG
        public Header Header
        {
            get { return _header; }
        }
#endif

	    public IControlPainter ControlPainter
	    {
	        get { return _controlPainter; }
	        set
	        {
                if ( _controlPainter != value )
                {
                    _controlPainter = value;
                    _columns.ControlPainter = value;
                    _rowRenderer.ControlPainter = value;
                    UpdateBorderSize();
                }
	        }
	    }

	    public IControlMethodInvoker ControlMethodInvoker
	    {
	        get { return _invoker; }
	        set
	        {
	            _invoker = value;
                _rowRenderer.MethodInvoker = _invoker;
	        }
	    }

	    public IInPlaceEditor InPlaceEditor
	    {
	        get { return _inPlaceEditor; }
	        set { _inPlaceEditor = value; }
	    }

	    public BorderStyle BorderStyle
	    {
	        get { return _borderStyle; }
	        set
	        {
	            if ( _borderStyle != value )
	            {
	                _borderStyle = value;
	                UpdateBorderSize();
	            }
	        }
	    }

	    /// <summary>
	    /// The color used for drawing the JetListView border when BorderStyle.FixedSingle is used.
	    /// </summary>
        public Color BorderColor
	    {
	        get { return _borderColor; }
	        set
	        {
	            if ( _borderColor != value )
	            {
                    _borderColor = value;
                    Invalidate();
	            }
	        }
	    }

	    public ColumnHeaderStyle HeaderStyle
        {
            get { return _headerStyle; }
            set
            {
                if ( _headerStyle != value )
                {
                    _headerStyle = value;
                    if ( IsHandleCreated )
                    {
                        UpdateHeaderStyle();
                    }
                }
            }
        }

	    [DefaultValue(false)]
        public bool HideSelection
        {
            get { return _rowListRenderer.HideSelection; }
            set { _rowListRenderer.HideSelection = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the selection highlight covers all
        /// columns in the list or just the first column.
        /// </summary>
        [DefaultValue(false)]
        public bool FullRowSelect
        {
            get { return _fullRowSelect; }
            set
            {
                _fullRowSelect = value;
                _rowRenderer.FullRowSelect = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user can drag column headers
        /// to reorder columns in the control.
        /// </summary>
        public bool AllowColumnReorder
        {
            get { return _header.AllowDragSections; }
            set { _header.AllowDragSections = value; }
        }

        /// <summary>
        /// The text shown in the view when there are no visible items.
        /// </summary>
        [DefaultValue("There are no items in this view.")]
        public string EmptyText
        {
            get { return _emptyText; }
            set { _emptyText = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether tooltips are automatically shown
        /// for columns where the displayed text is wider than the column in which
        /// it is displayed.
        /// </summary>
        [DefaultValue(true)]
        public bool AutoToolTips
	    {
	        get { return _autoToolTips; }
	        set { _autoToolTips = value; }
	    }

        /// <summary>
        /// Gets or sets the column used to draw the auto-preview text. If null, no
        /// auto-preview is drawn.
        /// </summary>
        [DefaultValue(null)]
        public JetListViewPreviewColumn AutoPreviewColumn
        {
            get { return _autoPreviewColumn; }
            set
            {
                if ( _autoPreviewColumn != value )
                {
                    _autoPreviewColumn = value;
                    UpdateRowRenderer();
                }
            }
        }

	    /// <summary>
        /// Gets or sets the value indicating whether the list view contents is drawn in
        /// multiline mode.
        /// </summary>
        public bool MultiLineView
        {
            get { return _baseRowRenderer is MultiLineRowRenderer; }
            set
            {
                if ( value != MultiLineView )
                {
                    if ( value )
                    {
                        MultiLineRowRenderer multiLineRenderer = new MultiLineRowRenderer( _columns );
                        multiLineRenderer.TopMargin = 2;
                        SetBaseRowRenderer( multiLineRenderer );
                        multiLineRenderer.ColumnSchemeProvider = _columnSchemeProvider;
                    }
                    else
                    {
                        SetBaseRowRenderer( new SingleLineRowRenderer( _columns, _items ) );
                    }
                    UpdateRowRenderer();
                }
            }
        }

        /// <summary>
        /// Gets or sets the column scheme which is used for all items in multiline mode.
        /// </summary>
        [Browsable(false)]
        public MultiLineColumnScheme ColumnScheme
        {
            get
            {
                if ( _columnSchemeProvider is StaticColumnSchemeProvider )
                {
                    return _columnSchemeProvider.GetColumnScheme( null );
                }
                return null;
            }
            set
            {
                if ( value != null )
                {
                    ColumnSchemeProvider = new StaticColumnSchemeProvider( value );
                }
                else
                {
                    ColumnSchemeProvider = null;
                }
            }
        }

	    /// <summary>
	    /// Gets or sets the column scheme provider for displaying list view contents
	    /// in multiline mode.
	    /// </summary>
        public IColumnSchemeProvider ColumnSchemeProvider
	    {
	        get { return _columnSchemeProvider; }
	        set
	        {
	            _columnSchemeProvider = value;
                if ( _baseRowRenderer is MultiLineRowRenderer )
                {
                    (_baseRowRenderer as MultiLineRowRenderer).ColumnSchemeProvider = value;
                }
	        }
	    }

        /// <summary>
        /// Gets or sets the group provider which defines separation of items shown in the list
        /// into groups.
        /// </summary>
        public IGroupProvider GroupProvider
        {
            get { return _groupProvider; }
            set
            {
                if ( !Equals( _groupProvider, value ) )
                {
                    if ( _rowListRenderer.NodeGroupCollection != null )
                    {
                        _rowListRenderer.NodeGroupCollection.Dispose();
                    }

                    _groupProvider = value;
                    if ( _groupProvider != null )
                    {
                        NodeGroupCollection nodeGroupCollection = new NodeGroupCollection( _items, _groupProvider );
                        DefaultGroupRenderer groupRenderer = new DefaultGroupRenderer( nodeGroupCollection );
                        groupRenderer.ControlPainter = _controlPainter;
                        groupRenderer.GroupHeaderColor = _groupHeaderColor;
                        groupRenderer.VisibleWidth = _rowRenderer.VisibleWidth;
                        _rowListRenderer.GroupRenderer = groupRenderer;
                        _rowListRenderer.NodeGroupCollection = nodeGroupCollection;
                    }
                    else
                    {
                        _rowListRenderer.NodeGroupCollection = null;
                    }
                    UpdateVisibleNodeCollection();
                    _rowListRenderer.UpdateScrollRange();
                    Invalidate();
                }
            }
        }

		/// <summary>
		/// Gets or sets whether the Insert mode is allowed when dragging over the list view.
		/// If yes, the drop target visual cue is drawn as an insert mark automatically when the drop-point is between the items.
		/// </summary>
		[DefaultValue(false), Category("Behavior")]
		public bool AllowDragInsert
		{
			get { return _allowDragInsert; }
			set { _allowDragInsert = value; }
		}

		/// <summary>
		/// Gets or sets whether the drop target visual cues are painted as <see cref="DropTargetRenderMode.Restricted"/> automatically
		/// when the <see cref="JetListViewDragEventArgs.Effect"/> is set to <see cref="DragDropEffects.None"/> (when dropping here is not possible).
		/// </summary>
		[DefaultValue(true), Category("Appearance")]
		public bool AutoRestrictDropTarget
		{
			get { return _autoRestrictDropTarget; }
			set { _autoRestrictDropTarget = value; }
		}

	    private void UpdateVisibleNodeCollection()
	    {
            if ( _rowListRenderer != null )
            {
                if ( _rowListRenderer.NodeGroupCollection != null )
                {
                    _selectionModel.VisibleNodeCollection = _rowListRenderer.NodeGroupCollection;
                }
                else
                {
                    _selectionModel.VisibleNodeCollection = _items;
                }
            }
	    }

	    /// <summary>
        /// Gets or sets the value indicating whether multiple items can be selected in the list.
        /// </summary>
        [DefaultValue(true)]
        public bool MultiSelect
        {
            get { return _selectionModel is MultipleSelectionModel; }
            set
            {
                if ( MultiSelect != value )
                {
                    if ( value )
                    {
                        SetSelectionModel( new MultipleSelectionModel( _items ) );
                    }
                    else
                    {
                        SetSelectionModel( new SingleSelectionModel( _items ) );
                    }
                }
            }
        }

	    /// <summary>
	    /// Gets or sets the value indicating whether delimiter lines are drawn between
	    /// lines in the list.
	    /// </summary>
        public bool RowDelimiters
	    {
	        get { return _rowListRenderer.RowDelimiters; }
	        set { _rowListRenderer.RowDelimiters = value; }
	    }

	    /// <summary>
	    /// Gets or sets the background color of unselected group headers in the view.
	    /// </summary>
        public Color GroupHeaderColor
	    {
	        get { return _groupHeaderColor; }
	        set
	        {
	            if ( _groupHeaderColor != value )
	            {
                    _groupHeaderColor = value;
                    if ( _rowListRenderer.GroupRenderer != null )
                    {
                        _rowListRenderer.GroupRenderer.GroupHeaderColor = value;
                        Invalidate();
                    }
	            }
	        }
	    }

	    private void SetSelectionModel( SelectionModel model )
        {
            if ( _selectionModel != null )
            {
                _selectionModel.ActiveNodeChanged -= ForwardActiveNodeChanged;
                _selectionModel.FocusStateChanged -= ForwardFocusStateChanged;
                _selectionModel.SelectionStateChanged -= ForwardSelectionStateChanged;
            }
            _selectionModel = model;
            if ( _rowListRenderer != null )
            {
                _rowListRenderer.SelectionModel = _selectionModel;
            }
            if ( _incSearcher != null )
            {
                _incSearcher.SelectionModel = _selectionModel;
            }
            _selectionModel.ActiveNodeChanged += ForwardActiveNodeChanged;
            _selectionModel.FocusStateChanged += ForwardFocusStateChanged;
            _selectionModel.SelectionStateChanged += ForwardSelectionStateChanged;
            UpdateVisibleNodeCollection();
            Invalidate();
        }

	    private void UpdateBorderSize()
	    {
	        PerformLayout();
            int borderSize = _controlPainter.GetListViewBorderSize( _borderStyle );
            if ( _rowRenderer != null )
            {
                _rowRenderer.BorderSize = borderSize;
            }

            if ( _headerStyle != ColumnHeaderStyle.None )
            {
                borderSize += (int) (_headerHeight * _scaleFactor.Height);
            }
            _rowListRenderer.BorderSize = borderSize;
	        Invalidate();
	    }

	    internal HScrollBar HScrollbar
	    {
	        get { return _hScrollbar; }
	    }

	    internal VScrollBar VScrollbar
	    {
	        get { return _vScrollbar; }
	    }

        private void UpdateHeaderStyle()
        {
            _header.Visible = (_headerStyle != ColumnHeaderStyle.None);
            _header.Clickable = (_headerStyle == ColumnHeaderStyle.Clickable);
            UpdateBorderSize();
        }

        protected override void OnHandleCreated( EventArgs e )
	    {
            base.OnHandleCreated( e );
            _itemToolTip.CreateHandle();
            if ( _headerStyle != ColumnHeaderStyle.None )
            {
                UpdateHeaderStyle();
            }
	    }

	    protected override void OnFontChanged( EventArgs e )
	    {
	        base.OnFontChanged( e );
            _columns.Font = Font;
	    }

	    protected override void OnEnabledChanged( EventArgs e )
	    {
	        base.OnEnabledChanged( e );
            _rowListRenderer.ControlEnabled = Enabled;
            Invalidate();
	    }

	    protected override void OnPaint( PaintEventArgs e )
	    {
	        base.OnPaint( e );

            if ( _borderStyle == BorderStyle.FixedSingle )
            {
                using( Pen borderPen = new Pen ( _borderColor ) )
                {
                    e.Graphics.DrawRectangle( borderPen, 0, 0, Width-1, Height-1 );
                }
            }
            else
            {
                _controlPainter.DrawListViewBorder( e.Graphics, ClientRectangle, _borderStyle );
            }

            if ( _hScrollbar.Visible && _vScrollbar.Visible )
            {
                Rectangle rc = RectangleInsideBorders();
                rc = new Rectangle( rc.Right - _vScrollbar.Width, rc.Bottom - _hScrollbar.Height,
                    _vScrollbar.Width, _hScrollbar.Height );
                e.Graphics.FillRectangle( SystemBrushes.Control, rc );
            }

	        Rectangle rcClip = InternalClientRect();
	        Rectangle rcPaint = e.ClipRectangle;
            rcPaint.Intersect( rcClip );
            e.Graphics.SetClip( rcPaint );

            if ( _items.IsEmpty )
            {
                Rectangle rc = rcClip;
                rc.Offset( 0, 20 );
                StringFormat fmt = StringFormat.GenericDefault;
                fmt.Alignment = StringAlignment.Center;
                fmt.FormatFlags |= StringFormatFlags.NoWrap;
                _controlPainter.DrawText( e.Graphics, _emptyText, Font, SystemColors.WindowText,
                    rc, fmt );
            }
            else
            {
                _rowListRenderer.Draw( e.Graphics, rcPaint );
            }
	    }

	    internal Rectangle InternalClientRect()
	    {
	        Rectangle rcClip = RectangleInsideBorders();
	        if ( _vScrollbar.Visible )
	        {
	            rcClip.Width -= _vScrollbar.Width;
	        }
	        if ( _hScrollbar.Visible )
	        {
	            rcClip.Height -= _hScrollbar.Height;
	        }
	        return rcClip;
	    }

	    private Rectangle RectangleInsideBorders()
	    {
	        Rectangle rcClip = ClientRectangle;
	        int borderSize = _controlPainter.GetListViewBorderSize( _borderStyle );
	        rcClip.Inflate( -borderSize, -borderSize );
	        if ( _headerStyle != ColumnHeaderStyle.None )
	        {
	            rcClip.Offset( 0, (int) (_headerHeight * _scaleFactor.Height) );
	            rcClip.Height -= (int) (_headerHeight * _scaleFactor.Height);
	        }
	        return rcClip;
	    }

	    protected override void OnLayout( LayoutEventArgs levent )
	    {
	        base.OnLayout( levent );
	        UpdateScrollbars();
	    }

	    protected override void OnMouseDown( MouseEventArgs e )
	    {
            _startInPlaceEditTimer = false;
            _inPlaceEditTimer.Stop();
            CloseInPlaceEdit();
	        bool wasFocused = ContainsFocus;
            base.OnMouseDown( e );

            // Mouse down outside of internal client rect?
            if ( !InternalClientRect().Contains( e.X, e.Y ) )
            {
                return;
            }

            MouseHandleResult result = _rowListRenderer.HandleMouseDown( e.X, e.Y, e.Button, ModifierKeys );

            //  Mouse down on disposed control?
            if ( IsDisposed )  // OM-11129 - subscribe wizard closes form in its mouse down handler
            {
                return;
            }

            if ( e.Button == MouseButtons.Left && e.Clicks == 1 )
            {
                //  MouseDown: column said FocusOnMouseDown, capturing focus...
                if ( ( result & MouseHandleResult.FocusOnMouseDown ) != 0 )
                {
                    Focus();
                    _captureFocus = false;
                }
                //  MouseDown: capturing mouse, expecting mouse up...
                else if ( ( result & MouseHandleResult.SuppressFocus ) == 0 )
                {
                    Capture = true;
                    _captureFocus = true;
                }
            }

            if ( wasFocused && ( result & MouseHandleResult.MayInPlaceEdit) != 0 )
            {
                _startInPlaceEditTimer = true;
            }

	        JetListViewNode node = _rowListRenderer.GetRowAt( e.Y );
            if ( node != null )
            {
                Size dragSize = SystemInformation.DragSize;

                _dragRect = new Rectangle( new Point( e.X - (dragSize.Width /2),
                    e.Y - (dragSize.Height /2)), dragSize );
                _dragNode = node;
            }
            else
            {
                _dragNode = null;
            }

            if ( e.Clicks == 2 && e.Button == MouseButtons.Left )
            {
                _inPlaceEditTimer.Stop();
                _startInPlaceEditTimer = false;
                ProcessDoubleClick( e );
            }
	    }

        private void ProcessDoubleClick( MouseEventArgs e )
        {
            HandledEventArgs args = new HandledEventArgs();
            if ( _rowListRenderer.AcceptDoubleClick( e.X, e.Y ) )
            {
                if ( DoubleClick != null )
                {
                    DoubleClick( this, args );
                }
                if ( !args.Handled )
                {
                    _rowListRenderer.HandleDoubleClick( e.X, e.Y );
                }
            }
        }

        protected virtual void OnItemDrag( MouseButtons button, object item )
	    {
	        if ( ItemDrag != null )
	        {
	            ItemDrag( this, new ItemDragEventArgs( button, item ) );
	        }
	    }

	    protected override void OnMouseMove( MouseEventArgs e )
        {
            base.OnMouseMove( e );
            if ( _dragNode != null && !_dragRect.Contains( e.X, e.Y ) )
            {
                JetListViewNode theDragNode = _dragNode;
                _dragNode = null;
                _inPlaceEditTimer.Stop();
                CloseInPlaceEdit();
                IViewNode[] savedSelection = CheckSaveSelection( theDragNode );
                OnItemDrag( e.Button, theDragNode );
                CheckRestoreSelection( savedSelection );
            }
            _itemToolTip.UpdateToolTip( new Point( e.X, e.Y ) );
            UpdateCursor();
        }

	    protected override void OnMouseUp( MouseEventArgs e )
	    {
	        base.OnMouseUp( e );
            if ( _captureFocus )
            {
                Focus();
                _captureFocus = false;
            }

            _dragNode = null;
            if ( _startInPlaceEditTimer )
            {
                _inPlaceEditTimer.Start();
            }
            if ( !InternalClientRect().Contains( e.X, e.Y ) )
            {
                return;
            }
            _rowListRenderer.HandleMouseUp( e.X, e.Y, e.Button, ModifierKeys );
	    }

	    protected override bool IsInputChar( char charCode )
	    {
	        return true;
	    }

	    protected override void OnLeave( EventArgs e )
	    {
	        _focusLeft = true;
            base.OnLeave( e );
	    }

	    protected override void OnKeyDown( KeyEventArgs e )
	    {
            if ( _incSearcher.HandleKeyDown( e.KeyData ) )
            {
                return;
            }

            _focusLeft = false;
            base.OnKeyDown( e );
            if ( e.Handled && !ContainsFocus && Form.ActiveForm == FindForm() && !_focusLeft )  // OM-7838
            {
                Focus();
            }
            if ( !e.Handled )
            {
                _lastKeyDownHandled = _rowListRenderer.HandleKeyDown( e );

                if ( e.KeyCode == Keys.F2 && _selectionModel.ActiveNode != null )
                {
                    InPlaceEditNode( _selectionModel.ActiveNode );
                }
            }
            else
            {
                _lastKeyDownHandled = true;
            }
	    }

        protected override void OnKeyPress( KeyPressEventArgs e )
	    {
	        base.OnKeyPress( e );
            if ( !_lastKeyDownHandled )
            {
                _incSearcher.HandleKeyPress( e.KeyChar );
            }
            else
            {
                e.Handled = true;
            }
	    }

        protected override void OnMouseWheel( MouseEventArgs e )
	    {
	        base.OnMouseWheel( e );
            if ( !_vScrollbar.Visible )
                return;

            int lines = -e.Delta * SystemInformation.MouseWheelScrollLines / 120;
            VerticalScrollClamped( _vScrollbar.Value + _rowListRenderer.GetWheelScrollDistance( lines ) );
	    }

        private void VerticalScrollClamped( int pos )
        {
            if ( pos < 0 )
            {
                _vScrollbar.Value = 0;
            }
            else if ( pos > _vScrollbar.Maximum - _vScrollbar.LargeChange + 1 )
            {
                _vScrollbar.Value = _vScrollbar.Maximum - _vScrollbar.LargeChange + 1;
            }
            else
            {
                _vScrollbar.Value = pos;
            }
        }

	    protected override void OnGotFocus( EventArgs e )
	    {
	        base.OnGotFocus( e );
            if ( _selectionModel.Count == 0 && _items.VisibleItemCount > 0 )
            {
                Trace.WriteLine( "JLV selecting top item from OnGotFocus" );
                _selectionModel.MoveDown();
            }
            _rowListRenderer.ActiveSelection = true;
        }

	    protected override void OnLostFocus( EventArgs e )
	    {
	        base.OnLostFocus( e );
            _rowListRenderer.ActiveSelection = false;
            _incSearcher.ClearIncrementalSearch();
            _inPlaceEditTimer.Stop();
        }

        #region Drag'n'Drop
        protected override void OnDragOver( DragEventArgs drgevent )
	    {
            Point pnt = PointToClient( new Point( drgevent.X, drgevent.Y ) );
            if ( !InternalClientRect().Contains( pnt ) )
            {
                drgevent.Effect = DragDropEffects.None;
                return;
            }

            int scrollBottom = ClientSize.Height - 7;
            if ( _hScrollbar.Visible )
            {
                scrollBottom -= _hScrollbar.Height;
            }

            if ( pnt.Y < 7 )
            {
                _dragScrollUp = true;
                _dragScrollTimer.Enabled = true;
            }
            else if ( pnt.Y > scrollBottom )
            {
                _dragScrollUp = false;
                _dragScrollTimer.Enabled = true;
            }
            else
            {
                _dragScrollTimer.Enabled = false;
            }

			// Query external handlers of their view on the drag'n'drop process
            JetListViewDragEventArgs args = FillDragEventArgs(drgevent, pnt);
            if ( DragOver != null )
            {
                DragOver( this, args );
            }

			// If the drop-target rendering mode is set to generic insertion, determine the proper type (above or below)
			if(args.DropTargetRenderMode == DropTargetRenderMode.InsertAny)
				args.DropTargetRenderMode = args.LocalY < args.RowHeight / 2 ? DropTargetRenderMode.InsertAbove : DropTargetRenderMode.InsertBelow;

			// Hide the drop-target if drop is prohibited here
			if((AutoRestrictDropTarget) && (args.Effect == DragDropEffects.None))
				args.DropTargetRenderMode = DropTargetRenderMode.Restricted;

            JetListViewNode oldDropTargetRow = _rowListRenderer.DropTargetRow;
            JetListViewColumn oldDragOverColumn = _dragOverColumn;
            _rowListRenderer.SetDropTarget( args.DropTargetNode, args.DropTargetRenderMode );
            if ( _rowListRenderer.DropTargetRow == null )
            {
                _dragOverColumn = null;
            }
            else
            {
                _dragOverColumn = _rowRenderer.GetColumnAt( _rowListRenderer.DropTargetRow, pnt.X, args.LocalY );
            }

            if ( oldDropTargetRow != _rowListRenderer.DropTargetRow || _dragOverColumn != oldDragOverColumn )
            {
                _dragExpandTimer.Stop();
                if ( _rowListRenderer.DropTargetRow != null && _dragOverColumn != null )
                {
                    _dragExpandTimer.Enabled = true;
                }
            }

            drgevent.Effect = args.Effect;
	        base.OnDragOver( drgevent );
	    }

		/// <summary>
		/// Prepares the drag event args given the current dragging point in client coordinates.
		/// </summary>
		internal JetListViewDragEventArgs FillDragEventArgs(DragEventArgs dea, Point pt)
		{
			// Get the droptarget node, and check if the drop should be Over or Insert
			int localY;
			int nRowTop = 0;
			int nRowBottom = 0;
			JetListViewNode nodeTarget = _rowListRenderer.GetRowAndDelta( pt.Y, out localY ) as JetListViewNode;
			DropTargetRenderMode renderTarget = DropTargetRenderMode.Restricted;
			if( nodeTarget != null )
			{ // The upper and lower quarter are for Insert, and the middle area of half a row height is for Over
				_rowListRenderer.GetRowBounds( nodeTarget, out nRowTop, out nRowBottom );
				renderTarget = (localY < (nRowBottom - nRowTop) / 4) ? DropTargetRenderMode.InsertAbove : ((localY >= (nRowBottom - nRowTop) * 3 / 4) ? DropTargetRenderMode.InsertBelow : DropTargetRenderMode.Over);
			}

			return new JetListViewDragEventArgs( dea, nodeTarget, renderTarget, localY, nRowBottom - nRowTop );
		}

		protected override void OnDragLeave( EventArgs e )
	    {
	        EndDragOver();
	        base.OnDragLeave( e );
	    }

	    protected override void OnDragDrop( DragEventArgs drgevent )
	    {
            Point pnt = PointToClient( new Point( drgevent.X, drgevent.Y ) );
            EndDragOver();
            JetListViewDragEventArgs dragEventArgs = FillDragEventArgs( drgevent, pnt );
            if ( DragDrop != null )
            {
                DragDrop( this, dragEventArgs );
            }
            base.OnDragDrop( drgevent );
	    }

        private void EndDragOver()
        {
            _dragScrollTimer.Enabled = false;
            _dragExpandTimer.Enabled = false;
            _rowListRenderer.ClearDropTarget();
        }

        private void HandleDragScroll( object sender, EventArgs e )
        {
            int newValue = _dragScrollUp
                ? _vScrollbar.Value - _vScrollbar.SmallChange
                : _vScrollbar.Value + _vScrollbar.SmallChange;
            VerticalScrollClamped( newValue );
        }

        private void HandleDragExpand( object sender, EventArgs e )
        {
            _dragExpandTimer.Stop();
            if ( _dragOverColumn != null && _rowListRenderer.DropTargetRow != null )
            {
                _dragOverColumn.HandleDragHover( _rowListRenderer.DropTargetRow );
            }
        }
        #endregion Drag'n'Drop

        protected override bool IsInputKey( Keys keyData )
	    {
            return keyData != Keys.Tab && keyData != (Keys.Shift | Keys.Tab) && keyData != Keys.Escape;
	    }

        private void HandleScrollRangeChanged( object sender, EventArgs e )
        {
            if ( _invoker.InvokeRequired )
            {
                _invoker.BeginInvoke( new MethodInvoker( PerformLayout ) );
            }
            else
            {
                PerformLayout();
            }
        }

	    private void UpdateScrollbars()
	    {
            if ( _rowRenderer == null || _rowListRenderer == null || _vScrollbar == null || _hScrollbar == null )
                return;
            if ( _controlPainter == null )
                return;

            SuspendLayout();
	        try
	        {
	            int hScrollRange = _rowRenderer.ScrollRange;
	            int vScrollRange = _rowListRenderer.ScrollRange;

	            int borderSize = _controlPainter.GetListViewBorderSize( _borderStyle );
	            int clientWidth = Width - 2 * borderSize;
	            int clientHeight = Height - 2 * borderSize;

	            if ( _headerStyle != ColumnHeaderStyle.None )
	            {
	                clientHeight -= (int) (_headerHeight * _scaleFactor.Height);
	            }

	            int availWidth = clientWidth;
	            int availHeight = clientHeight;
	            if ( availWidth < _vScrollbar.Width || availHeight < _hScrollbar.Height )
	                return;

	            if ( availWidth < hScrollRange )
	            {
	                availHeight = clientHeight - _hScrollbar.Height;
	            }
	            if ( availHeight < vScrollRange )
	            {
	                availWidth = clientWidth - _vScrollbar.Width;
	            }
	            // repeat the calculation because availWidth may have been reduced by previous calc
	            if ( availWidth < hScrollRange )
	            {
	                availHeight = clientHeight - _hScrollbar.Height;
	            }

	            if ( vScrollRange > availHeight )
	            {
	                int rowCount = availHeight / _baseRowRenderer.RowHeight;
	                if ( rowCount < 1 )
	                {
	                    rowCount = 1;
	                }
	                int vLargeChange = rowCount * _baseRowRenderer.RowHeight;
	                _vScrollbar.Maximum = vScrollRange - availHeight + vLargeChange;
	                _vScrollbar.LargeChange = vLargeChange;

	                if ( _headerStyle != ColumnHeaderStyle.None )
	                {
	                    _vScrollbar.Top = (int) (_headerHeight * _scaleFactor.Height) + borderSize;
	                }
	                else
	                {
	                    _vScrollbar.Top = borderSize;
	                }
	                _vScrollbar.Left = Width - (borderSize+1) - _vScrollbar.Width;
	                _vScrollbar.Height = clientHeight - ( hScrollRange > availWidth ? _hScrollbar.Height : 0 );

	                if ( _vScrollbar.Value > _vScrollbar.Maximum - _vScrollbar.LargeChange + 1 )
	                {
	                    _vScrollbar.Value = _vScrollbar.Maximum - _vScrollbar.LargeChange + 1;
	                }

	                _vScrollbar.Visible = true;
	            }
	            else
	            {
	                _vScrollbar.Value = 0;
	                _vScrollbar.Visible = false;
	            }

	            if ( hScrollRange > availWidth )
	            {
	                int hLargeChange = Math.Max( 3, availWidth - 10 );
	                _hScrollbar.Maximum = hScrollRange - availWidth + hLargeChange;
	                _hScrollbar.LargeChange = hLargeChange;

	                _hScrollbar.Left = borderSize;
	                _hScrollbar.Top = Height - (borderSize+1) - _hScrollbar.Height;
	                _hScrollbar.Width = clientWidth - ( _vScrollbar.Visible ? _vScrollbar.Width : 0 );

	                if ( _hScrollbar.Value > _hScrollbar.Maximum - _hScrollbar.LargeChange + 1 )
	                {
	                    _hScrollbar.Value = _hScrollbar.Maximum - _hScrollbar.LargeChange + 1;
	                }

	                _hScrollbar.Visible = true;
	            }
	            else
	            {
	                _hScrollbar.Value = 0;
	                _hScrollbar.Visible = false;
	            }

	            _rowListRenderer.VisibleHeight = availHeight;
	            _rowRenderer.VisibleWidth = availWidth;
	            if ( _rowListRenderer.GroupRenderer != null )
	            {
	                _rowListRenderer.GroupRenderer.VisibleWidth = availWidth;
	            }

	            if ( _pendingScroll >= 0 && _pendingScroll <= _vScrollbar.Maximum - _vScrollbar.LargeChange + 1 )
	            {
	                _vScrollbar.Value = _pendingScroll;
	            }
	            _pendingScroll = -1;

	            if ( _headerStyle != ColumnHeaderStyle.None )
	            {
	                _header.Location = new Point( borderSize - _hScrollbar.Value, borderSize );
	                int needHeaderSize = hScrollRange;
	                if ( _vScrollbar.Visible )
	                {
	                    needHeaderSize += _vScrollbar.Width;
	                }

	                _header.Width = Math.Max( Width - 2 * borderSize, needHeaderSize );
	            }
	        }
	        finally
	        {
	            ResumeLayout();
	        }
	    }

	    /// <summary>
	    /// Invalidates the nodes displaying the specified item.
	    /// </summary>
	    /// <param name="item">The item to invalidate.</param>
        public void InvalidateItem( object item )
	    {
	        JetListViewNode[] nodes = _items.NodesFromItem( item );
            if ( nodes.Length == 0 )
            {
                throw new ArgumentException( "Item not displayed in view" );
            }

            for( int i=0; i<nodes.Length; i++ )
            {
                _rowListRenderer.InvalidateRow( nodes [i] );
            }
	    }

        /// <summary>
        /// Updates the display of the specified item in the list.
        /// </summary>
        /// <param name="item">The item to update.</param>
        /// <remarks>If the item is shown in a sorted list, updates the position of the item
        /// in the list. Also, if the view is filtered, updates the filtered status of the item.
        /// Also invalidates the item.</remarks>
        public void UpdateItem( object item )
        {
            lock( _items )
            {
                DoUpdateItem( item );
            }
            OnItemUpdated( item );
        }

	    private void DoUpdateItem( object item )
	    {
	        _rowRenderer.UpdateItem( item );
	        _items.Update( item );
	        InvalidateItem( item );
	    }

	    /// <summary>
        /// Updates the display of the specified item in the list if it is present in the list.
        /// </summary>
        /// <param name="item">The item to update.</param>
        public void UpdateItemSafe( object item )
        {
	        Guard.NullArgument( item, "item" );
            lock( _items )
            {
                if ( !_items.Contains( item ) )
                {
                    return;
                }
                DoUpdateItem( item );
            }
            OnItemUpdated( item );
        }

        private void OnItemUpdated( object item )
        {
            if ( ItemUpdated != null )
            {
                ItemUpdated( this, new ItemEventArgs( item ) );
            }
        }

        public JetListViewNode GetNodeAt( int x, int y )
        {
            return _rowListRenderer.GetRowAt( y );
        }

        public JetListViewNode GetNodeAt( Point pnt )
        {
            return _rowListRenderer.GetRowAt( pnt.Y );
        }

        public JetListViewColumn GetColumnAt( Point pnt )
        {
            int deltaY;
            JetListViewNode node = _rowListRenderer.GetRowAndDelta( pnt.Y, out deltaY ) as JetListViewNode;
            if ( node == null )
            {
                return null;
            }
            return _rowRenderer.GetColumnAt( node, pnt.X, deltaY );
        }

        /// <summary>
        /// Returns the bounds of the cell displaying the specified column value for the specified
        /// node.
        /// </summary>
        /// <param name="node">The node for which the bounds are retrieved.</param>
        /// <param name="col">The column for which the bounds are retrieved.</param>
        /// <returns>The bounds rectangle.</returns>
        public Rectangle GetItemBounds( JetListViewNode node, JetListViewColumn col )
        {
            int startY, endY;
            Rectangle rcColBounds = _rowRenderer.GetColumnBounds( col, node );
            _rowListRenderer.GetRowBounds( node, out startY, out endY );

            int startX = rcColBounds.Left + col.LeftMargin;
            int endX = rcColBounds.Right;

            Rectangle rcClient = InternalClientRect();
            endX -= col.RightMargin;
            if ( endX > rcClient.Right )
            {
                endX = rcClient.Right;
            }

            return new Rectangle( rcColBounds.Left + col.LeftMargin, startY + rcColBounds.Top,
                endX-startX, rcColBounds.Height );
        }

	    protected override void WndProc( ref Message m )
	    {
            if ( m.Msg == Win32Declarations.WM_SYSCHAR )
            {
                if ( _lastKeyDownHandled )
                {
                    return;
                }
            }

            base.WndProc( ref m );

            if ( m.Msg == Win32Declarations.WM_CONTEXTMENU )
            {
                HandleContextMenu( m );
            }
            else if ( m.Msg == Win32Declarations.WM_NOTIFY )
            {
                NMHDR nmhdr = (NMHDR) m.GetLParam( typeof(NMHDR) );
                if ( nmhdr.hwndFrom == _itemToolTip.Handle )
                {
                    _itemToolTip.HandleWMNotify( ref m );
                }
            }
        }

        private void UpdateCursor()
        {
            if ( IsDisposed )
            {
                return;
            }
            Point pnt = PointToClient( Cursor.Position );
            if ( InternalClientRect().Contains( pnt.X, pnt.Y ) )
            {
                int deltaY;
                JetListViewNode node = _rowListRenderer.GetRowAndDelta( pnt.Y, out deltaY ) as JetListViewNode;
                if ( node != null )
                {
                    JetListViewColumn col = _rowRenderer.GetColumnAt( node, pnt.X, deltaY );
                    if ( col != null )
                    {
                        Cursor = col.GetItemCursor( node.Data );
                    }
                }
            }
        }

        private void HandleContextMenu( Message m )
	    {
            _itemToolTip.Hide();
            IViewNode[] savedSelection = null;

            MouseButtons button = 0;
            Point pnt = new Point( m.LParam.ToInt32() );
            if ( pnt.X == -1 && pnt.Y == -1 )
            {
                pnt = GetSelectedItemMenuLocation();
            }
            else
            {
                pnt = PointToClient( pnt );
                button = MouseButtons.Right;

                if ( InternalClientRect().Contains( pnt.X, pnt.Y ) )
                {
                    int deltaY;
                    JetListViewNode node = _rowListRenderer.GetRowAndDelta( pnt.Y, out deltaY ) as JetListViewNode;
                    if ( node != null )
                    {
                        savedSelection = CheckSaveSelection( node );

                        JetListViewColumn col = _rowRenderer.GetColumnAt( node, pnt.X, deltaY );
                        if ( col != null && col.HandleContextMenu( node, pnt.X, pnt.Y ) )
                        {
                            CheckRestoreSelection( savedSelection );
                            return;
                        }
                    }
                }
            }

            if ( ContextMenuInvoked != null )
            {
                ContextMenuInvoked( this, new MouseEventArgs( button, 1, pnt.X, pnt.Y, 0 ) );
            }

	        CheckRestoreSelection( savedSelection );
	    }

	    /// <summary>
	    /// If the specified node is not contained in the selection, selects it and
	    /// returns the array containing old selection.
	    /// </summary>
        private IViewNode[] CheckSaveSelection( JetListViewNode node )
	    {
	        IViewNode[] savedSelection = null;
	        if ( !_selectionModel.IsNodeSelected( node ) )
	        {
	            savedSelection = _selectionModel.SelectionToArray();
	            _selectionModel.ClearSelection();
	            _selectionModel.SelectNode( node );
	        }
	        return savedSelection;
	    }

        /// <summary>
        /// If the specified array is not null, restores the selection to the list
        /// of nodes in the specified array.
        /// </summary>
        private void CheckRestoreSelection( IViewNode[] savedSelection )
        {
            if ( savedSelection != null )
            {
                _selectionModel.ClearSelection();
                for( int i=0; i<savedSelection.Length; i++ )
                {
                    _selectionModel.SelectNode( savedSelection [i] );
                }
            }
        }

        private Point GetSelectedItemMenuLocation()
	    {
	        Point pnt;
	        pnt = new Point( 0, 0 );
	        if ( Selection.Count > 0 )
	        {
	            JetListViewNode node = Selection.ActiveNode;
	            int startY, endY;
	            if ( node == null || !_rowListRenderer.GetRowBounds( node, out startY, out endY ) )
	            {
	                pnt = new Point( 0, 0 );
	            }
	            else
	            {
	                pnt = new Point( 0, startY );
	            }
	        }

	        pnt.X += 4;
	        pnt.Y += 4;
	        return pnt;
	    }

	    private void HandleChildrenRequested( object sender, RequestChildrenEventArgs e )
        {
            OnChildrenRequested( e );
        }

	    protected virtual void OnChildrenRequested( RequestChildrenEventArgs e )
	    {
	        if ( ChildrenRequested != null )
	        {
	            ChildrenRequested( this, e );
	        }
	    }

        private void HandleInPlaceEditTimer( object sender, EventArgs e )
        {
            _inPlaceEditTimer.Stop();
            if ( _selectionModel.ActiveNode != null )
            {
                InPlaceEditNode( _selectionModel.ActiveNode );
            }
        }

        public void InPlaceEditNode( JetListViewNode node )
        {
            CloseInPlaceEdit();
            if ( _inPlaceEditor != null )
            {
                JetListViewColumn col = _rowRenderer.GetInPlaceEditColumn( node );
                if ( col == null )
                    return;

                _items.ExpandParents( node );
                _selectionModel.SelectAndFocusNode( node );

                _inPlaceEditor.BeginEdit( this, col, node );
            }
        }

        public static bool IsNodeVisible( JetListViewNode node )
        {
            JetListViewNode parent = node.Parent;
            while( parent != null )
            {
                if ( parent.CollapseState == CollapseState.Collapsed )
                    return false;

                parent = parent.Parent;
            }
            return true;
        }

	    public void ScrollInView( JetListViewNode node )
	    {
            _rowListRenderer.ScrollInView( node );
	    }

        /// <summary>
        /// Scrolls in view the thread containing the specified resource.
        /// </summary>
        /// <param name="res">The resource to scroll in view.</param>
        public void ScrollThreadInView( object res )
        {
            JetListViewNode node = _items.NodeFromItem( res );
            if ( node != null )
            {
                int startY, endY;
                if ( _rowListRenderer.GetRowBounds( node, out startY, out endY ) )
                {
                    return;
                }
                JetListViewNode parent = node;
                while( parent.Parent != null && parent.Parent != _items.Root )
                {
                    parent = parent.Parent;
                }
                if ( parent != node )
                {
                    _rowListRenderer.ScrollInView( parent );
                }
                _rowListRenderer.ScrollInView( node );
            }
        }

        public void SetAllGroupsExpanded( bool expanded )
        {
            if ( _rowListRenderer.NodeGroupCollection != null )
            {
                _rowListRenderer.NodeGroupCollection.SetAllGroupsExpanded( expanded );
            }
        }

        public void SetAllThreadsExpanded( bool expanded )
        {
            _items.SetExpandedRecursive( expanded );
        }

        public void CopySelection()
        {
            StringBuilder selBuilder = new StringBuilder();
            foreach( IViewNode viewNode in _selectionModel.SelectedNodes )
            {
                JetListViewNode node = viewNode as JetListViewNode;
                if ( node == null )
                {
                    // skip group header nodes (OM-14696)
                    continue;
                }

                bool first = true;
                foreach( JetListViewColumn col in _columns )
                {
                    if ( col.IsIndentColumn() )
                    {
                        continue;
                    }
                    if ( first )
                    {
                        first = false;
                    }
                    else
                    {
                        selBuilder.Append( '\t' );
                    }
                    selBuilder.Append( col.GetItemText( node.Data ) );
                }
                selBuilder.Append( "\r\n" );
            }
            if ( _selectionModel.Count == 1 )
            {
                Clipboard.SetDataObject( selBuilder.ToString().Trim() );
            }
            else
            {
                Clipboard.SetDataObject( selBuilder.ToString() );
            }
        }

        internal void SetEditedNode( JetListViewNode node )
        {
            _rowListRenderer.EditedNode = node;
        }

	    public bool IsColumnHeaderAt( int x, int y )
	    {
            if ( _headerStyle != ColumnHeaderStyle.None )
            {
                return _header.Bounds.Contains( x, y );
            }
            return false;
	    }

	    private void ForwardActiveNodeChanged( object sender, ViewNodeEventArgs e )
	    {
            JetListViewNode lvNode = e.ViewNode as JetListViewNode;
            if ( ActiveNodeChanged != null )
            {
                ActiveNodeChanged( this, new JetListViewNodeEventArgs( lvNode ) );
            }
	    }

	    private void ForwardFocusStateChanged( object sender, ViewNodeStateChangeEventArgs e )
	    {
            JetListViewNode lvNode = e.ViewNode as JetListViewNode;
            if ( lvNode != null )
            {
                if ( FocusStateChanged != null )
                {
                    FocusStateChanged( this, new StateChangeEventArgs( lvNode, e.State ) );
                }
            }
	    }

	    private void ForwardSelectionStateChanged( object sender, ViewNodeStateChangeEventArgs e )
	    {
            JetListViewNode lvNode = e.ViewNode as JetListViewNode;
            if ( lvNode != null )
            {
                if ( SelectionStateChanged != null )
                {
                    SelectionStateChanged( this, new StateChangeEventArgs( lvNode, e.State ) );
                }
            }
	    }

	    private void ForwardColumnClick( object sender, ColumnEventArgs e )
	    {
	        if ( ColumnClick != null )
	        {
	            ColumnClick( this, e );
	        }
	    }

	    private void ForwardColumnOrderChanged( object sender, EventArgs e )
	    {
	        if ( ColumnOrderChanged != null )
	        {
	            ColumnOrderChanged( this, e );
	        }
	    }

	    private void ForwardColumnResized( object sender, EventArgs e )
	    {
	        if ( ColumnResized != null )
	        {
	            ColumnResized( this, e );
	        }
	    }

        private class StaticColumnSchemeProvider: IColumnSchemeProvider
        {
            private readonly MultiLineColumnScheme _scheme;

            public StaticColumnSchemeProvider( MultiLineColumnScheme scheme )
            {
                _scheme = scheme;
            }

            public MultiLineColumnScheme GetColumnScheme( object item )
            {
                return _scheme;
            }
        }
	}
}
