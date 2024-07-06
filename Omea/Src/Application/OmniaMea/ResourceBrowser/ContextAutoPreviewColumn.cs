// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using JetBrains.DataStructures;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.RichText;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Column which shows search result contexts in the auto-preview area.
	/// </summary>
	internal class ContextAutoPreviewColumn: JetListViewPreviewColumn
	{
        private IHighlightDataProvider _highlightDataProvider;
        private IntHashSet _contextsRequested;
        private IntHashTable _contextCache;
        private static Pen _barPen = new Pen( Color.DarkGray, 2.0f );

	    public ContextAutoPreviewColumn()
	    {
            VerticalAlignment = StringAlignment.Near;
	    }

	    internal void SetHighlightDataProvider( IHighlightDataProvider provider )
        {
            _contextsRequested = new IntHashSet();
            _contextCache = new IntHashTable();
            _highlightDataProvider = provider;
        }

	    protected override string GetItemText( object item )
	    {
	        IResource res = (IResource) item;
            string context = _highlightDataProvider.GetContext( res );
            if ( context != null )
            {
                return context;
            }
            lock( _contextsRequested )
            {
                if ( !_contextsRequested.Contains( res.Id ) )
                {
                    _contextsRequested.Add( res.Id );
                    _highlightDataProvider.RequestContexts( new int[] { res.Id } );
                }
            }
            return "Calculating context...";
	    }

        protected override void DrawItemText( Graphics g, Rectangle rcText, object item,
                                              Color textColor, RowState state, string highlightText )
        {
            IResource res = (IResource) item;
            string context = _highlightDataProvider.GetContext( res );
            if ( context != null )
            {
                RichText richText = (RichText) _contextCache [res.Id];
                if ( richText == null )
                {
                    richText = new RichText( context,
                        new RichTextParameters( OwnerControl.Font,
                        new TextStyle( FontStyle.Regular, Color.DarkGreen, Color.Transparent ) ) );

                    OffsetData[] data = _highlightDataProvider.GetContextHighlightData( res );
                    if ( data != null )
                    {
                        for( int i = 0; i < data.Length; i++ )
                        {
                            richText.SetStyle( FontStyle.Bold, data [i].Start, data [i].Length );
                        }
                    }
                    _contextCache [res.Id] = richText;
                }

                g.DrawLine( _barPen, rcText.X + 2, rcText.Y + 1, rcText.X + 2, rcText.Y + rcText.Height - 2 );
	            ShiftRectForText( ref rcText );
                RichTextColumn.FormatRowRichText( ref richText, textColor, state, highlightText );
                richText.DrawClipped( g, rcText );
            }
            else
            {
                base.DrawItemText( g, rcText, item, textColor, state, highlightText );
            }
        }

	    protected override Color GetItemForeColor( object item )
	    {
	        return Color.DarkGreen;
	    }

	    public override int GetAutoPreviewHeight( JetListViewNode node )
	    {
	        return base.GetAutoPreviewHeight( node ) + 5;
	    }
	}
}
