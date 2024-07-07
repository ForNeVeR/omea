// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using SP.Windows;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// Draws JetListView rows with a different multi-line column layout in each row.
	/// </summary>
	internal class MultiLineRowRenderer: RowRendererBase
	{
        private IColumnSchemeProvider _columnSchemeProvider;
        private int _visibleWidth;
        private HeaderSection _arrangeByHeaderSection;
        private HeaderSection _sortOrderHeaderSection;
	    private JetListViewColumn _sortColumn;
        private Color _lastTextColor;
        private ItemColorCallback _lastItemColorCallback;
        private ItemFontCallback _lastItemFontCallback;
        private int _topMargin = 0;

	    public MultiLineRowRenderer( JetListViewColumnCollection columnCollection )
            : base( columnCollection )
	    {
	    }

	    public IColumnSchemeProvider ColumnSchemeProvider
	    {
	        get { return _columnSchemeProvider; }
	        set { _columnSchemeProvider = value; }
	    }

	    public int TopMargin
	    {
	        get { return _topMargin; }
	        set { _topMargin = value; }
	    }

	    public override int VisibleWidth
        {
            get { return _visibleWidth; }
            set
            {
                if ( _visibleWidth != value )
                {
                    _visibleWidth = value;
                    if ( _arrangeByHeaderSection != null )
                    {
                        _arrangeByHeaderSection.Width = Math.Max( 50, _visibleWidth - 100 );
                    }
                    OnInvalidate();
                }
            }
        }

        public override void DrawRow( Graphics g, Rectangle rc, JetListViewNode itemNode, RowState state )
        {
            MultiLineColumnScheme scheme = _columnSchemeProvider.GetColumnScheme( itemNode.Data );
            int indent = GetRowIndent( itemNode, scheme );

            Rectangle rcFocus = GetFocusRect( itemNode, rc );

            bool focusRow = false, dropTargetRow = false;
            if ( ( (state & RowState.Focused) != 0 &&
                _searchHighlightText != null && _searchHighlightText.Length > 0) )
            {
                state |= RowState.IncSearchMatch;
            }

            if ( _fullRowSelect )
            {
                FillFullRowSelectBar( g, rcFocus, state );
            }

            ClearRowSelectState( ref state, ref focusRow, ref dropTargetRow );

            foreach( MultiLineColumnSetting setting in scheme.ColumnSettings )
            {
                if ( _fullRowSelect )
                {
                    if ( (state & RowState.IncSearchMatch) != 0 && (state & RowState.ActiveSelected) != 0 )
                    {
                        state &= ~RowState.ActiveSelected;
                        state |= RowState.InactiveSelected;
                    }
                }

                _lastTextColor = setting.TextColor;
                _lastItemColorCallback = setting.Column.ForeColorCallback;
                _lastItemFontCallback = setting.Column.FontCallback;
                if ( ( state & (RowState.ActiveSelected | RowState.InactiveSelected) ) == 0 )
                {
                    setting.Column.ForeColorCallback = new ItemColorCallback( GetColumnForeColor );
                }
                HorizontalAlignment lastAlignment = setting.Column.Alignment;
                int oldMargin = setting.Column.RightMargin;
                setting.Column.Alignment = setting.TextAlign;
                if ( setting.TextAlign == HorizontalAlignment.Right )
                {
                    setting.Column.RightMargin = 5;
                }

                Rectangle rcCol = GetRectangleFromSetting( scheme, setting, indent );
                rcCol.Offset( _borderSize, rc.Top );

                DrawColumnWithHighlight( g, rcCol, itemNode, setting.Column, state );

                setting.Column.ForeColorCallback = _lastItemColorCallback;
                setting.Column.Alignment = lastAlignment;
                setting.Column.RightMargin = oldMargin;
            }

            DrawRowSelectRect( g, rcFocus, dropTargetRow, focusRow );
        }

	    protected internal override Rectangle GetFocusRect( JetListViewNode itemNode, Rectangle rc )
	    {
            MultiLineColumnScheme scheme = _columnSchemeProvider.GetColumnScheme( itemNode.Data );
            int indent = GetRowIndent( itemNode, scheme );
            return new Rectangle( _borderSize + indent, rc.Top, _visibleWidth - _borderSize - indent, rc.Height );
        }

	    private Color GetColumnForeColor( object item )
        {
            if ( _lastItemColorCallback != null && _lastItemFontCallback != null )
            {
                Color callbackColor = _lastItemColorCallback( item );
                FontStyle callbackFont = _lastItemFontCallback( item );
                if ( callbackColor.ToArgb() != SystemColors.ControlText.ToArgb() || callbackFont != FontStyle.Regular )
                {
                    return callbackColor;
                }
            }
            return _lastTextColor;
        }

        private Rectangle GetRectangleFromSetting( MultiLineColumnScheme scheme,
            MultiLineColumnSetting setting, int indent )
	    {
            if ( setting.Column.IsIndentColumn() )
            {
                return new Rectangle( 0, _topMargin, indent, _rowHeight );
            }

            int baseWidth = scheme.BaseWidth;
	        int deltaWidth = _visibleWidth - baseWidth - indent;
	        int startX = setting.StartX + indent;
	        int width = setting.Width;
	        if ( ( setting.Anchor & ColumnAnchor.Left ) == 0 )
	        {
	            startX += deltaWidth;
	        }
	        if ( (setting.Anchor & (ColumnAnchor.Left | ColumnAnchor.Right) ) == (ColumnAnchor.Left | ColumnAnchor.Right)  )
	        {
	            width += deltaWidth;
	        }
	        return new Rectangle( startX, _topMargin + setting.StartRow * _rowHeight,
	                              width, ( setting.EndRow - setting.StartRow + 1 ) * _rowHeight);
	    }

        private int GetRowIndent( JetListViewNode node, MultiLineColumnScheme scheme )
        {
            if ( !node.HasChildren && node.Level == 0 && !scheme.AlignTopLevelItems )
            {
                return 0;
            }
            foreach( JetListViewColumn col in scheme.Columns )
            {
                if ( col.IsIndentColumn() )
                {
                    return col.GetIndent( node );
                }
            }
            return 0;
        }

        public override int GetRowHeight( JetListViewNode node )
	    {
            MultiLineColumnScheme scheme = _columnSchemeProvider.GetColumnScheme( node.Data );
            return scheme.RowCount * _rowHeight + _topMargin;
        }

	    public override int AllRowsHeight
	    {
	        get { return -1; }
	    }

	    public override void UpdateItem( object item )
	    {
            MultiLineColumnScheme scheme = _columnSchemeProvider.GetColumnScheme( item );
            foreach( MultiLineColumnSetting setting in scheme.ColumnSettings )
            {
                setting.Column.UpdateItem( item );
            }
        }

        public override Rectangle GetColumnBounds( JetListViewColumn col, JetListViewNode node )
        {
            MultiLineColumnScheme scheme = _columnSchemeProvider.GetColumnScheme( node.Data );
            foreach( MultiLineColumnSetting setting in scheme.ColumnSettings )
            {
                if ( setting.Column == col )
                {
                    int indent = GetRowIndent( node, scheme );
                    return GetRectangleFromSetting( scheme, setting, indent );
                }
            }
            throw new Exception( "The specified column is not found in the column scheme for the specified node" );
        }

        protected override JetListViewColumn GetColumnAndDelta( JetListViewNode node, int x, int y,
            out int deltaX, out int deltaY )
        {
            MultiLineColumnScheme scheme = _columnSchemeProvider.GetColumnScheme( node.Data );
            foreach( MultiLineColumnSetting setting in scheme.ColumnSettings )
            {
                int indent = GetRowIndent( node, scheme );
                Rectangle rcCol = GetRectangleFromSetting( scheme, setting, indent );
                if ( rcCol.Contains( x, y ) )
                {
                    deltaX = x - rcCol.Left;
                    deltaY = y - rcCol.Top;
                    return setting.Column;
                }
            }
            deltaX = 0;
            deltaY = 0;
            return null;
        }

	    protected override IEnumerable GetColumnEnumerable( JetListViewNode node )
	    {
	        MultiLineColumnScheme scheme = _columnSchemeProvider.GetColumnScheme( node.Data );
            return scheme.Columns;
	    }

	    public override void SizeColumnsToContent( HashSet addedNodes, HashSet removedNodes, HashSet changedNodes )
	    {
	    }

	    public override void ProcessNodeExpanded( JetListViewNode node )
	    {
	    }

	    public override void ProcessNodeCollapsed( JetListViewNode node )
	    {
	    }

	    protected override void HookHeaderControl()
	    {
            _headerControl.Sections.Clear();
            _arrangeByHeaderSection = new HeaderSection( "Arranged By: ", Math.Max( 50, _visibleWidth - 100 ) );
            _headerControl.Sections.Add( _arrangeByHeaderSection );
            _sortOrderHeaderSection = new HeaderSection( "Sort", 100 );
            _headerControl.Sections.Add( _sortOrderHeaderSection );
            UpdateSortColumn( null );

            _headerControl.SectionClick += new HeaderSectionEventHandler( HandleSectionClick );
            _headerControl.BeforeSectionTrack += new HeaderSectionWidthConformableEventHandler( HandleBeforeSectionTrack );
            _headerControl.BeforeSectionDrag += new HeaderSectionOrderConformableEventHandler( HandleBeforeSectionDrag );
        }

	    protected override void UnhookHeaderControl()
	    {
            _headerControl.Sections.Clear();
            _arrangeByHeaderSection = null;
            _sortOrderHeaderSection = null;
            _headerControl.SectionClick -= new HeaderSectionEventHandler( HandleSectionClick );
            _headerControl.BeforeSectionTrack -= new HeaderSectionWidthConformableEventHandler( HandleBeforeSectionTrack );
            _headerControl.BeforeSectionDrag -= new HeaderSectionOrderConformableEventHandler( HandleBeforeSectionDrag );
        }

	    protected override void HandleSortIconChanged( object sender, EventArgs e )
	    {
	        UpdateSortColumn( null );
	    }

	    protected override void HandleColumnRemoved( object sender, ColumnEventArgs e )
	    {
	        base.HandleColumnRemoved( sender, e );
            if ( e.Column.SortIcon != SortIcon.None )
            {
                UpdateSortColumn( e.Column );
            }
	    }

	    private void UpdateSortColumn( JetListViewColumn exceptColumn )
	    {
            bool foundSortColumn = false;
            foreach( JetListViewColumn col in _columnCollection )
            {
                if ( col.SortIcon != SortIcon.None && col != exceptColumn )
                {
                    _sortColumn = col;
                    _arrangeByHeaderSection.Text = "Arranged By: " + col.SortMenuText;
                    _sortOrderHeaderSection.Text = (col.SortIcon == SortIcon.Ascending)
                        ? col.SortMenuAscText
                        : col.SortMenuDescText;
                    foundSortColumn = true;
                    break;
                }
            }
            if ( !foundSortColumn )
            {
                _arrangeByHeaderSection.Text = "Arranged By:";
                _sortOrderHeaderSection.Text = "";
            }
	    }

	    private void HandleSectionClick( object sender, HeaderSectionEventArgs ea )
	    {
            if ( ea.Item == _arrangeByHeaderSection )
            {
                ShowSortContextMenu();
            }
            else if ( ea.Item == _sortOrderHeaderSection )
            {
                OnColumnClick( _sortColumn );
            }
	    }

	    private void ShowSortContextMenu()
	    {
            ContextMenu menu = new ContextMenu();
            foreach( JetListViewColumn col in _columnCollection )
            {
                if ( col.ShowHeader && col.SortMenuText != null && col.SortMenuText.Length > 0 )
                {
                    MenuItem item = menu.MenuItems.Add( col.SortMenuText,
                        new EventHandler( HandleSortContextMenuClick ) );
                    if ( col == _sortColumn )
                    {
                        item.Checked = true;
                    }
                }
            }
            menu.Show( _headerControl, _headerControl.PointToClient( Cursor.Position ) );
	    }

	    private void HandleSortContextMenuClick( object sender, EventArgs e )
	    {
            MenuItem item = (MenuItem) sender;
            foreach( JetListViewColumn col in _columnCollection )
            {
                if ( col.ShowHeader && col.SortMenuText == item.Text )
                {
                    if ( col != _sortColumn )
                    {
                        OnColumnClick( col );
                    }
                }
            }
	    }

	    private void HandleBeforeSectionTrack( object sender, HeaderSectionWidthConformableEventArgs ea )
	    {
            ea.Accepted = false;
	    }

        private void HandleBeforeSectionDrag( object sender, HeaderSectionOrderConformableEventArgs ea )
        {
            ea.Accepted = false;
        }
	}
}
