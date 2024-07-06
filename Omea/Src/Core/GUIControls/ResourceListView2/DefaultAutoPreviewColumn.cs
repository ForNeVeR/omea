// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using JetBrains.DataStructures;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// The column which draws the preview text of a resource in the auto-preview area.
	/// </summary>
	public class DefaultAutoPreviewColumn: JetListViewPreviewColumn
	{
        private int _lineCount = 3;
        private IntHashTableOfInt _previewHeights = new IntHashTableOfInt();
        private int _defaultPreviewHeight;
        private AutoPreviewMode _autoPreviewMode;
        private static Pen _barPen = new Pen( Color.DarkGray, 2.0f );

	    public DefaultAutoPreviewColumn()
	    {
            _noWrap = false;
            _previewHeights.MissingKeyValue = Int32.MaxValue;
            _autoPreviewMode = AutoPreviewMode.AllItems;
	    }

	    public override void Dispose()
	    {
	        if ( OwnerControl != null )
	        {
	            OwnerControl.FontChanged -= new EventHandler( HandleOwnerFontChanged );
	        }
            base.Dispose();
	    }

	    public AutoPreviewMode AutoPreviewMode
	    {
	        get { return _autoPreviewMode; }
	        set
	        {
	            if ( _autoPreviewMode != value )
	            {
                    _autoPreviewMode = value;
                    lock( _previewHeights )
                    {
                        _previewHeights.Clear();
                    }
                    OnAllRowsHeightChanged();
	            }
	        }
	    }

	    protected override void SetOwner( JetListViewColumnCollection value )
	    {
	        base.SetOwner( value );
            if ( OwnerControl != null )
            {
                UpdateDefaultHeight();
                OwnerControl.FontChanged += new EventHandler( HandleOwnerFontChanged );
            }
	    }

        private void HandleOwnerFontChanged( object sender, EventArgs e )
        {
            UpdateDefaultHeight();
        }

        private void UpdateDefaultHeight()
        {
            _defaultPreviewHeight = ((int) OwnerControl.Font.GetHeight() + 1 ) * _lineCount + 2;
        }

        protected override Color GetItemForeColor( object item )
	    {
	        return Color.Blue;
	    }

	    protected override string GetItemText( object item )
	    {
            IResource res = (IResource) item;
            if ( _autoPreviewMode == AutoPreviewMode.UnreadItems && !res.HasProp( Core.Props.IsUnread ) )
            {
                return "";
            }
	        string previewText = Core.MessageFormatter.GetPreviewText( res, _lineCount );
            if ( previewText.Length == 0 )
            {
                return "<no preview available>";
            }

            return previewText;
	    }

	    protected override void DrawItemText( Graphics g, Rectangle rcText, object item, Color textColor,
                                              RowState state, string highlightText )
	    {
            IResource res = (IResource) item;
            string text = GetItemText( item );
            if ( text == "" )
            {
                SetCachedPreviewHeight( res, 0 );
                return;
            }

            StringFormat fmt = GetColumnStringFormat();
            Font itemFont = GetItemFont( item );

            int oldHeight = GetAutoPreviewHeight( res.Id );
            g.DrawLine( _barPen, rcText.X + 2, rcText.Y + 1, rcText.X + 2, rcText.Y + rcText.Height - 2 );
	        ShiftRectForText( ref rcText );

            int height = Owner.ControlPainter.DrawText( g, text, itemFont, textColor, rcText, fmt ) + 2;
	        if ( height != oldHeight && height < _defaultPreviewHeight )
            {
                ChangeItemHeight( res, oldHeight, height );
            }
	    }

	    private void ChangeItemHeight( IResource res, int oldHeight, int height )
	    {
	        SetCachedPreviewHeight( res, height );
	        JetListViewNode[] nodes = OwnerControl.NodeCollection.NodesFromItem( res );
	        for( int i = 0; i < nodes.Length; i++ )
	        {
	            OnRowHeightChanged( nodes[ i ], oldHeight, height );
	        }
	    }

	    private void SetCachedPreviewHeight( IResource res, int height )
	    {
	        lock( _previewHeights )
	        {
	            _previewHeights[ res.Id ] = height;
	        }
	    }

	    public override int GetAutoPreviewHeight( JetListViewNode node )
	    {
	        IResource res = (IResource) node.Data;
            if ( _autoPreviewMode == AutoPreviewMode.UnreadItems && !res.HasProp( Core.Props.IsUnread ) )
            {
                return 0;
            }
            return GetAutoPreviewHeight( res.Id );
	    }

        private int GetAutoPreviewHeight( int resId )
        {
            lock( _previewHeights )
            {
                int h = _previewHeights[ resId ];
                return (h == _previewHeights.MissingKeyValue) ? _defaultPreviewHeight : h;
            }
        }

	    protected override void UpdateItem( object item )
	    {
            if ( _autoPreviewMode == AutoPreviewMode.UnreadItems )
            {
                IResource res = (IResource) item;
                bool isUnread = res.HasProp( Core.Props.IsUnread );
                lock( _previewHeights )
                {
                    int cachedHeight = _previewHeights[ res.Id ];
                    if ( isUnread && cachedHeight == 0 )
                    {
                        _previewHeights.Remove( res.Id );
                        ChangeItemHeight( res, 0, _defaultPreviewHeight );
                    }
                    if ( !isUnread && cachedHeight != 0 )
                    {
                        int oldHeight = (cachedHeight == _previewHeights.MissingKeyValue)
                            ? _defaultPreviewHeight : cachedHeight;
                        ChangeItemHeight( res, oldHeight, 0 );
                    }
                }
            }
	    }
	}
}
