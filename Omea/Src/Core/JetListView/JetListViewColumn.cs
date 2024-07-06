// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using JetBrains.UI.RichText;

namespace JetBrains.JetListViewLibrary
{
    public enum SortIcon { None = -1, Ascending = 0, Descending = 1 };

	/// <summary>
	/// A column in JetListView.
	/// </summary>
    public class JetListViewColumn: IDisposable
    {
        /// <summary>
        ///
        /// </summary>
        public const int MinimumColumnWidth = 0;

        private JetListViewColumnCollection _owner = null;
        private int _width = MinimumColumnWidth;
        private int _autoSizeMinWidth = 0;

        private bool _autoSize = false;
        private bool _sizeToContent = false;
        private bool _handleAllClicks = false;
        private int _leftMargin = 2;
        private int _rightMargin = 2;
        private bool _fixedSize = false;
        /// <summary>
        ///
        /// </summary>
        private HorizontalAlignment _alignment = HorizontalAlignment.Left;
        private StringAlignment _verticalAlignment = StringAlignment.Center;

        private string _text;
        private string _sortMenuText;
        private string _sortMenuAscText = "Ascending";
        private string _sortMenuDescText = "Descending";
        private SortIcon _sortIcon = SortIcon.None;

        #region Callback variables

        private ItemColorCallback _foreColorCallback;
        private ItemColorCallback _backColorCallback;
        private ItemFontCallback _fontCallback;
        private ItemTextCallback _itemTextCallback;
        private ItemCursorCallback _itemCursorCallback;
	    protected ItemTextCallback _itemToolTipCallback;
	    protected bool _showHeader;
        protected bool _noWrap = true;

        private static FontCache _fontCache = new FontCache();

	    #endregion Callback variables

        public JetListViewColumn()
        {
            _showHeader = true;
        }

	    public virtual void Dispose()
	    {
	    }

	    public event EventHandler AutoSizeChanged;
        public event EventHandler SizeToContentChanged;
        public event EventHandler WidthChanged;
        public event EventHandler SortIconChanged;
        public event ItemMouseEventHandler MouseDown;

        /// <summary>
        /// Occurs when the text of the column is changed.
        /// </summary>
        public event EventHandler TextChanged;

        protected internal JetListViewColumnCollection Owner
	    {
	        get { return _owner; }
	        set
	        {
	            SetOwner( value );
	        }
	    }

	    protected virtual void SetOwner( JetListViewColumnCollection value )
	    {
	        _owner = value;
	    }

	    public JetListView OwnerControl
        {
            get { return _owner.OwnerControl; }
        }

	    #region Callback properties

        /// <summary>
        /// The callback called to get the foreground color of an item.
        /// </summary>
        public ItemColorCallback ForeColorCallback
        {
            get { return _foreColorCallback; }
            set { _foreColorCallback = value; }
        }

        /// <summary>
        /// The callback called to get the background color of an item.
        /// </summary>
        public ItemColorCallback BackColorCallback
        {
            get { return _backColorCallback; }
            set { _backColorCallback = value; }
        }

        /// <summary>
        /// The callback called to get the font of an item.
        /// </summary>
        public ItemFontCallback FontCallback
        {
            get { return _fontCallback; }
            set { _fontCallback = value; }
        }

	    /// <summary>
	    /// The callback called to get the text of an item.
	    /// </summary>
        public ItemTextCallback ItemTextCallback
	    {
	        get { return _itemTextCallback; }
	        set { _itemTextCallback = value; }
	    }

	    /// <summary>
	    /// The callback called to get the tooltip of an item.
	    /// </summary>
        public ItemTextCallback ItemToolTipCallback
	    {
	        get { return _itemToolTipCallback; }
	        set { _itemToolTipCallback = value; }
	    }

	    /// <summary>
	    /// The callback called to get the cursor of an item.
	    /// </summary>
        public ItemCursorCallback CursorCallback
	    {
	        get { return _itemCursorCallback; }
	        set { _itemCursorCallback = value; }
	    }

	    #endregion Callback properties

	    public string Text
	    {
	        get { return _text; }
	        set
	        {
	            if ( _text != value )
	            {
                    _text = value;
                    OnTextChanged();
	            }
	        }
	    }

	    public string SortMenuText
	    {
	        get
	        {
	            if ( _sortMenuText == null )
	            {
	                return _text;
	            }
                return _sortMenuText;
	        }
            set { _sortMenuText = value; }
	    }

	    public string SortMenuAscText
	    {
	        get { return _sortMenuAscText; }
	        set { _sortMenuAscText = value; }
	    }

	    public string SortMenuDescText
	    {
	        get { return _sortMenuDescText; }
	        set { _sortMenuDescText = value; }
	    }

	    /// <summary>
        /// Width of the column, in pixels.
        /// </summary>
        public int Width
        {
            get { return _width; }
            set
            {
                if ( value != _width )
                {
                    if (value >= MinimumColumnWidth)
                    {
                        _width = value;
                        OnWidthChanged();
                    }
                    else
                        throw new InvalidOperationException("Column width cannot be smaller than minimal value set in JetListViewColumn class");
                }
            }
        }

	    /// <summary>
	    /// The minimum width to which the column can be autosized.
	    /// </summary>
        public int AutoSizeMinWidth
	    {
	        get { return _autoSizeMinWidth; }
	        set { _autoSizeMinWidth = value; }
	    }

	    public SortIcon SortIcon
	    {
	        get { return _sortIcon; }
	        set
	        {
	            if ( _sortIcon != value )
	            {
                    _sortIcon = value;
                    OnSortIconChanged();
	            }
	        }
	    }

	    /// <summary>
        ///
        /// </summary>
        public bool AutoSize
        {
            get
            {
                return _autoSize;
            }
            set
            {
                if ( _autoSize != value )
                {
                    _autoSize = value;
                    if (_autoSize)
                        _fixedSize = false;

                    if ( AutoSizeChanged != null )
                    {
                        AutoSizeChanged( this, EventArgs.Empty );
                    }
                }
            }
        }

        public bool SizeToContent
        {
            get { return _sizeToContent; }
            set
            {
                if ( _sizeToContent != value )
                {
                    _sizeToContent = value;

                    if( SizeToContentChanged != null )
                    {
                        SizeToContentChanged( this, EventArgs.Empty );
                    }
                }
            }
        }

	    public int LeftMargin
	    {
	        get { return _leftMargin; }
	        set { _leftMargin = value; }
	    }

	    public int RightMargin
	    {
	        get { return _rightMargin; }
	        set { _rightMargin = value; }
	    }

	    public bool HandleAllClicks
	    {
	        get { return _handleAllClicks; }
	        set { _handleAllClicks = value; }
	    }

        /// <summary>
        /// If true, the column gets its separate header. If false, the header of the column
        /// is merged with the header of the column that follows it.
        /// </summary>
        public bool ShowHeader
        {
            get { return _showHeader; }
            set { _showHeader = value; }
        }

        /// <summary>
        ///
        /// </summary>
        public bool FixedSize
        {
            get
            {
                return _fixedSize;
            }
            set
            {
                _fixedSize = value;
                if (_fixedSize)
                    _autoSize = false;
            }
        }

        /// <summary>
        /// The alignment of text in the column.
        /// </summary>
        public HorizontalAlignment Alignment
        {
            get { return _alignment; }
            set { _alignment = value; }
        }

	    /// <summary>
	    /// The vertical alignment of text in the column.
	    /// </summary>
	    [DefaultValue(StringAlignment.Center)]
        public StringAlignment VerticalAlignment
	    {
	        get { return _verticalAlignment; }
	        set { _verticalAlignment = value; }
	    }

        /// <summary>
        /// Returns the difference between the common width of the column and its width for
        /// a specific node. Applies to fixed-size columns only.
        /// </summary>
        /// <param name="node">The node for which the difference is returned.</param>
        /// <returns>The difference.</returns>
        protected internal virtual int GetWidthDelta( JetListViewNode node )
        {
            return 0;
        }

        protected virtual Font GetItemFont( object item )
        {
            if ( FontCallback != null )
            {
                FontStyle fontStyle = FontCallback( item );
                if ( fontStyle != FontStyle.Regular )
                {
                    return _fontCache.GetFont( _owner.Font, fontStyle );
                }
            }
            return _owner.Font;
        }

        protected virtual Color GetItemForeColor( object item )
        {
            if ( ForeColorCallback != null )
            {
                return ForeColorCallback( item );
            }
            return SystemColors.WindowText;
        }

        protected virtual Color GetItemBackColor( object item )
        {
            if ( BackColorCallback != null )
            {
                return BackColorCallback( item );
            }
            return SystemColors.Window;
        }

        public virtual Cursor GetItemCursor( object item )
        {
            if ( CursorCallback != null )
            {
                return CursorCallback( item );
            }
            return null;
        }

        protected internal virtual void DrawHeader( Graphics g, Rectangle bounds )
        {
        }

        protected internal virtual void DrawNode( Graphics g, Rectangle rc, JetListViewNode node,
                                                  RowState state, string highlightText )
        {
            DrawItem( g, rc, node.Data, state, highlightText );
        }

        protected internal virtual void DrawItem( Graphics g, Rectangle rc, object item,
                                                  RowState state, string highlightText )
        {
            #region Preconditions
            if( item == null )
            {
                throw new ArgumentNullException( "JetListViewColumn -- Source object is NULL." );
            }
            #endregion Preconditions

            Rectangle rcText = new Rectangle( rc.Left + _leftMargin, rc.Top,
                rc.Width - _leftMargin - _rightMargin, rc.Height );

            Rectangle rcFocus = rc;
            if ( _alignment == HorizontalAlignment.Left &&
                ( ( state & (RowState.ActiveSelected | RowState.InactiveSelected | RowState.Focused | RowState.DropTarget) ) != 0 ||
                highlightText != null ) )
            {
                int textWidth = GetDesiredWidth( item );
                if ( textWidth < Width )
                {
                    rcFocus.Width = textWidth;
                }
            }

            Color textColor = DrawItemBackground( g, rc, rcFocus, item, state, highlightText );

            DrawItemText( g, rcText, item, textColor, state, highlightText );

            if ( ( state & RowState.DropTarget ) != 0 )
            {
                DrawDropTarget( g, rcFocus );
            }
            else if ( ( state & RowState.Focused ) != 0 )
            {
                _owner.ControlPainter.DrawFocusRect( g, rcFocus );
            }
        }

	    internal static void DrawDropTarget( Graphics g, Rectangle rcFocus )
	    {
	        rcFocus.Width--;
	        rcFocus.Height -= 2;
	        rcFocus.Offset( 1, 1 );
	        g.DrawRectangle( Pens.DarkGray, rcFocus );
	        rcFocus.Offset( -1, -1 );
	        g.DrawRectangle( Pens.BlueViolet, rcFocus );
	    }

	    protected internal Color DrawItemBackground( Graphics g, Rectangle rc, Rectangle rcFocus,
            object item, RowState state, string highlightText )
	    {
	        Color textColor = GetItemForeColor( item );
	        if ( ( state & RowState.Disabled ) != 0 )
	        {
	            textColor = SystemColors.GrayText;
	        }
	        else if ( highlightText != null )
	        {
                Rectangle rcHlFocus = rcFocus;
                if ( _alignment == HorizontalAlignment.Right )
                {
                    rcHlFocus.Offset( -_rightMargin, 0 );
                }
                int deltaWidth = (_alignment == HorizontalAlignment.Left) ? _leftMargin : 0;
                Rectangle rcHighlight, rcRest;
                BuildHighlightRects( g, item, highlightText, rcHlFocus, deltaWidth, out rcHighlight, out rcRest );
	            g.FillRectangle( SystemBrushes.Highlight, rcHighlight );
	            g.FillRectangle( SystemBrushes.Control, rcRest );
	        }
	        else if ( ( state & RowState.ActiveSelected ) != 0  )
	        {
	            g.FillRectangle( SystemBrushes.Highlight, rcFocus );
	            textColor = SystemColors.HighlightText;
	        }
	        else if ( ( state & RowState.InactiveSelected ) != 0 )
	        {
	            g.FillRectangle( SystemBrushes.Control, rcFocus );
	        }
	        else
	        {
	            Color color = GetItemBackColor( item );
	            if ( color != SystemColors.Window )
	            {
	                using( Brush b = new SolidBrush( color ) )
	                {
	                    g.FillRectangle( b, rc );
	                }
	            }
	        }
	        return textColor;
	    }

        private void BuildHighlightRects( Graphics g, object item, string highlightText, Rectangle rc,
            int deltaWidth, out Rectangle rcHighlight, out Rectangle rcRest )
        {
            int hlWidth = GetHighlightWidth( g, item, highlightText ) + deltaWidth;
            if ( _alignment == HorizontalAlignment.Right )
            {
                int itemWidth = GetTextWidth( item );
                rcHighlight = new Rectangle( rc.Right - itemWidth, rc.Top, hlWidth, rc.Height );
                rcRest = new Rectangle( rc.Right - itemWidth + hlWidth, rc.Top, itemWidth - hlWidth, rc.Height );
            }
            else
            {
                rcHighlight = new Rectangle( rc.Left, rc.Top, hlWidth, rc.Height );
                rcRest = new Rectangle( rc.Left + hlWidth, rc.Top, rc.Width - hlWidth, rc.Height );
            }
        }

        public virtual int GetDesiredWidth( object item )
        {
            return GetTextWidth( item ) + _leftMargin + _rightMargin;
        }

        protected virtual int GetHighlightWidth( Graphics g, object item, string hlText )
        {
            // the casing of hlText and actual text found in the item may be different, and
            // we need to measure the actual text
            string itemText = GetItemText( item );
            int index = itemText.ToLower().IndexOf( hlText.ToLower() );
            if ( index != 0 )
            {
                return 0;
            }
            string itemHlText = itemText.Substring( index, hlText.Length );
            return _owner.ControlPainter.MeasureText( g, itemHlText, GetItemFont( item ) ).Width;
        }

        protected internal virtual void DrawItemText( Graphics g, Rectangle rcText,
            object item, Color textColor, RowState state, string highlightText )
        {
            string text = GetItemText( item, rcText.Width );
            StringFormat fmt = GetColumnStringFormat();
            Font itemFont = GetItemFont( item );

            if ( highlightText != null && highlightText.Length <= text.Length )
            {
                Rectangle rcHighlight, rcRest;
                BuildHighlightRects( g,item, highlightText, rcText, 0, out rcHighlight, out rcRest );

                _owner.ControlPainter.DrawText( g, text.Substring( 0, highlightText.Length ),
                    itemFont, SystemColors.HighlightText, rcHighlight, fmt );
                _owner.ControlPainter.DrawText( g, text.Substring( highlightText.Length ),
                    itemFont, textColor, rcRest, fmt );
            }
            else
            {
                _owner.ControlPainter.DrawText( g, text, itemFont, textColor, rcText, fmt );
            }
        }

	    protected StringFormat GetColumnStringFormat()
	    {
	        StringFormat fmt = new StringFormat( StringFormat.GenericDefault );
	        if ( _noWrap )
	        {
	            fmt.FormatFlags |= StringFormatFlags.NoWrap;
	        }
	        else
	        {
	            fmt.FormatFlags &= ~StringFormatFlags.NoWrap;
	        }
	        fmt.Alignment     = GetHorizontalAlignment( Alignment );
	        fmt.LineAlignment = _verticalAlignment;
	        fmt.Trimming      = StringTrimming.EllipsisCharacter;
	        fmt.HotkeyPrefix  = HotkeyPrefix.None;
	        return fmt;
	    }

	    private StringAlignment GetHorizontalAlignment( HorizontalAlignment alignment )
        {
            switch( alignment )
            {
                case HorizontalAlignment.Center: return StringAlignment.Center;
                case HorizontalAlignment.Right:  return StringAlignment.Far;
                default:                         return StringAlignment.Near;
            }
        }

        protected virtual int GetTextWidth( object item )
        {
            return GetTextWidth( item, Width );
        }

        protected virtual int GetTextWidth( object item, int baseWidth )
        {
            Font itemFont = GetItemFont( item );
            return _owner.ControlPainter.MeasureText( GetItemText( item, baseWidth ), itemFont ).Width;
        }

        protected internal virtual string GetItemText( object item )
	    {
            if ( _itemTextCallback != null )
            {
                return _itemTextCallback( item );
            }
	        return item.ToString();
	    }

        protected internal virtual string GetItemText( object item, int width )
        {
            return GetItemText( item );
        }

        protected internal virtual MouseHandleResult HandleMouseDown( JetListViewNode node, int x, int y )
        {
            MouseHandleResult result = 0;
            if ( MouseDown != null )
            {
                ItemMouseEventArgs args = new ItemMouseEventArgs( node.Data, x, y );
                MouseDown( this, args );
                if( args.Handled )
                {
                    result |= MouseHandleResult.Handled;
                }
            }
            return result;
        }

        protected internal virtual bool HandleMouseUp( JetListViewNode node, int x, int y )
        {
            return false;
        }

        protected internal virtual bool HandleKeyDown( JetListViewNode node, KeyEventArgs e )
        {
            return false;
        }

        protected internal virtual bool HandleContextMenu( JetListViewNode node, int x, int y )
        {
            return false;
        }

        protected internal virtual void HandleDragHover( JetListViewNode node )
        {
            node.Expanded = true;
        }

        public virtual bool IsIndentColumn()
        {
            return false;
        }

        public virtual int GetIndent( JetListViewNode node )
        {
            return 0;
        }

        protected internal virtual void UpdateItem( object item )
        {
        }

	    public virtual bool MatchIncrementalSearch( JetListViewNode node, string text )
	    {
	        string matchText = GetItemText( node.Data ).ToLower();
	        return matchText.StartsWith( text.ToLower() );
	    }

	    public virtual string GetToolTip( JetListViewNode node, Rectangle rc, ref bool needPlace )
	    {
            string toolTip = null;
            if ( _itemToolTipCallback != null )
            {
                toolTip = _itemToolTipCallback( node.Data );
            }
            if ( OwnerControl != null && OwnerControl.AutoToolTips && GetTextWidth( node.Data, rc.Width ) > rc.Width )
            {
                if ( toolTip == null || toolTip.Length == 0 )
                {
                    toolTip = GetItemText( node.Data, rc.Width );
                }
                else
                {
                    toolTip = GetItemText( node.Data, rc.Width ) + " (" + toolTip + ")";
                }
            }
            else
            {
                needPlace = false;
            }
            return toolTip;
	    }

        public virtual bool AcceptColumnDoubleClick
        {
            get { return true; }
        }

        public virtual bool HandleDoubleClick( JetListViewNode node )
	    {
	        return false;
	    }

	    protected virtual void OnWidthChanged()
	    {
	        if ( WidthChanged != null )
	        {
	            WidthChanged( this, EventArgs.Empty );
	        }
	    }

	    private void OnSortIconChanged()
	    {
	        if ( SortIconChanged != null )
	        {
                SortIconChanged( this, EventArgs.Empty );
	        }
	    }

	    private void OnTextChanged()
	    {
            if ( TextChanged != null )
            {
                TextChanged( this, EventArgs.Empty );
            }
	    }

	    public override string ToString()
	    {
	        return GetType().Name + " Text='" + Text + "' Width=" + Width;
	    }
    }
}
