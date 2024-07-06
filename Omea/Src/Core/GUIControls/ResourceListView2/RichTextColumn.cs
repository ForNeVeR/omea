// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Threading;
using JetBrains.DataStructures;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.RichText;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// A column which draws rich text in JetListView.
	/// </summary>
	public class RichTextColumn: JetListViewColumn
	{
        private ArrayList _decorators = new ArrayList();
        private IntHashTable _richTextCache = new IntHashTable();
        private HashSet _decorationChangedNodes = new HashSet();
        private LocalDataStoreSlot _itemBeingDecoratedSlot;
        private ReaderWriterLock _decoratorLock = new ReaderWriterLock();

	    public RichTextColumn()
	    {
            Core.ResourceAP.JobFinished += HandleResourceJobFinished;
            _itemBeingDecoratedSlot = Thread.AllocateDataSlot();
	    }

	    public override void Dispose()
	    {
            Core.ResourceAP.JobFinished -= HandleResourceJobFinished;

            _decoratorLock.AcquireWriterLock( -1 );
            try
            {
                foreach( IResourceNodeDecorator decorator in _decorators )
                {
                    decorator.DecorationChanged -= HandleDecorationChanged;
                }
                _decorators.Clear();
            }
            finally
            {
                _decoratorLock.ReleaseWriterLock();
            }

	        base.Dispose();
	    }

	    public void AddNodeDecorator( IResourceNodeDecorator decorator )
        {
            _decoratorLock.AcquireWriterLock( -1 );
            try
            {
                _decorators.Add( decorator );
            }
            finally
            {
                _decoratorLock.ReleaseWriterLock();
            }

            decorator.DecorationChanged += HandleDecorationChanged;
        }

        public void InsertNodeDecorator( IResourceNodeDecorator decorator, int pos )
        {
            _decoratorLock.AcquireWriterLock( -1 );
            try
            {
                _decorators.Insert( pos, decorator );
            }
            finally
            {
                _decoratorLock.ReleaseWriterLock();
            }

            decorator.DecorationChanged += HandleDecorationChanged;
        }

	    private void HandleDecorationChanged( object sender, ResourceEventArgs e )
	    {
            object itemBeingDecorated = Thread.GetData( _itemBeingDecoratedSlot );
            if ( itemBeingDecorated != null && (int) itemBeingDecorated == e.Resource.Id )
            {
                return;
            }

            if ( Core.ResourceStore.IsOwnerThread() )
            {
                _decorationChangedNodes.Add( e.Resource );
            }
            else
            {
                UpdateDecoration( e.Resource );
            }
	    }

	    private void UpdateDecoration( IResource res )
	    {
            OwnerControl.UpdateItemSafe( res );
	    }

	    protected override void DrawItemText( Graphics g, Rectangle rcText, object item,
            Color textColor, RowState state, string highlightText )
	    {
	        IResource res = (IResource) item;
            RichText text = GetRichText( res );

	        FormatRowRichText( ref text, textColor, state, highlightText );

	        text.DrawClipped( g, rcText );
	    }

	    public static void FormatRowRichText( ref RichText text, Color textColor, RowState state,
            string highlightText )
	    {
	        if ( textColor != SystemColors.WindowText || highlightText != null ||
	            (state & RowState.InactiveSelected) != 0 )
	        {
	            text = (RichText) text.Clone();
	            if ( textColor != SystemColors.WindowText || (state & RowState.InactiveSelected) != 0 )
	            {
	                text.SetColors( textColor, Color.Transparent );
	            }
	            if ( highlightText != null )
	            {
	                text.SetColors( textColor, Color.Transparent );
	                text.SetColors( SystemColors.HighlightText, Color.Transparent, 0, highlightText.Length );
	            }
	        }
	    }

	    protected override int GetTextWidth( object item )
	    {
            IResource res = (IResource) item;
            RichText text = GetRichText( res );
            return text.GetSize().Width;
        }

	    protected override int GetHighlightWidth( Graphics g, object item, string highlightText )
	    {
	        RichText text = GetRichText( (IResource) item );
            RichText[] strings = text.Split( highlightText.Length );
            return strings [0].GetSize().Width;
	    }

	    private RichText GetRichText( IResource res )
	    {
            lock( _richTextCache )
            {
                RichText richText = (RichText) _richTextCache [res.Id];
                if ( richText != null )
                {
                    return richText;
                }
            }

            TextStyle defaultStyle = new TextStyle( FontStyle.Regular,
                SystemColors.WindowText, Color.Transparent );
            RichTextParameters defaultParams = new RichTextParameters( OwnerControl.Font,
                defaultStyle );

            if ( Core.State == CoreState.ShuttingDown )
            {
                return new RichText( "", defaultParams );
            }

            Thread.SetData( _itemBeingDecoratedSlot, res.Id );
            RichText newRichText = new RichText( res.DisplayName, defaultParams );

            _decoratorLock.AcquireReaderLock( -1 );
            try
            {
                HashSet processedDecorators = new HashSet();
                for( int i=_decorators.Count-1; i >= 0; i-- )
                {
                    IResourceNodeDecorator decorator = (IResourceNodeDecorator) _decorators [i];
                    string key = decorator.DecorationKey;
                    if ( key != null && processedDecorators.Contains( key ) )
                    {
                        continue;
                    }
                    if ( decorator.DecorateNode( res, newRichText ) )
                    {
                        if ( key != null )
                        {
                            processedDecorators.Add( key );
                        }
                    }
                }
            }
            finally
            {
                _decoratorLock.ReleaseReaderLock();
            }

            Thread.SetData( _itemBeingDecoratedSlot, -1 );
            lock( _richTextCache )
            {
                _richTextCache [res.Id] = newRichText;
            }
            return newRichText;
	    }

	    protected override void UpdateItem( object item )
	    {
            lock( _richTextCache )
            {
                IResource res = (IResource) item;
                _richTextCache.Remove( res.Id );
            }
        }

	    public override string GetToolTip( JetListViewNode node, Rectangle rc, ref bool needPlace )
	    {
            IResource res = (IResource) node.Data;
            string toolTip = null;
            if ( _itemToolTipCallback != null )
            {
                toolTip = _itemToolTipCallback( node.Data );
            }

            RichText richText = null;
            if ( OwnerControl != null && OwnerControl.AutoToolTips )
            {
                richText = GetRichText( res );
            }
            if ( richText != null && richText.GetSize().Width > rc.Width )
            {
                if ( toolTip == null || toolTip.Length == 0 )
                {
                    toolTip = richText.Text;
                }
                else
                {
                    toolTip = richText.Text + " (" + toolTip + ")";
                }
            }
            else
            {
                needPlace = false;
            }
            return toolTip;
	    }

        public void InvalidateRichText()
        {
            lock( _richTextCache )
            {
                _richTextCache.Clear();
            }
        }

	    private void HandleResourceJobFinished( object sender, EventArgs e )
	    {
            if( _decorationChangedNodes.Count > 0 )
            {
                foreach( HashSet.Entry entry in _decorationChangedNodes )
                {
                    IResource res = (IResource) entry.Key;
                    UpdateDecoration( res );
                }
                _decorationChangedNodes.Clear();
            }
	    }
	}
}
