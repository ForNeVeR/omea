// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// The column which draws the resource icon.
    /// </summary>
    public class ResourceIconColumn: ResourcePropsColumn
    {
        public ResourceIconColumn() : base( new int[] { ResourceProps.Type } )
        {
            Width = 18;
            FixedSize = true;
            MouseDown += OnMouseClick;
        }

        protected override void DrawItem( Graphics g, Rectangle rc, object item,
                                          RowState state, string highlightText )
        {
            IResource res = item as IResource;
            if ( res != null )
            {
                DrawResourceIcon( g, res, rc, state );
            }
        }

        internal void DrawResourceIcon( Graphics g, IResource res, Rectangle rc, RowState state )
        {
            int midPointX = rc.Left + Width / 2;
            int midPointY = (rc.Top + rc.Bottom) / 2;
            int index = Core.ResourceIconManager.GetIconIndex( res );
            DrawSingleIcon( state, g, index, rc, midPointX, midPointY );
            int[] overlayIcons = Core.ResourceIconManager.GetOverlayIconIndices( res );
            for( int i=0; i<overlayIcons.Length; i++ )
            {
                DrawSingleIcon( state, g, overlayIcons [i], rc, midPointX, midPointY );
            }
        }

        private static void DrawSingleIcon( RowState state, Graphics g, int index,
                                            Rectangle rcCol, int midPointX, int midPointY )
        {
            if ( index >= Core.ResourceIconManager.ImageList.Images.Count )
            {
                // possible on shutdown (OM-8972)
                return;
            }

            RectangleF rcClip = g.ClipBounds;
            rcClip.Intersect( new RectangleF( rcCol.Left, rcCol.Top, rcCol.Width, rcCol.Height ) );
            IntPtr hdc = g.GetHdc();
            try
            {
                IntPtr clipRgn = Win32Declarations.CreateRectRgn( 0, 0, 0, 0 );
                if ( Win32Declarations.GetClipRgn( hdc, clipRgn ) != 1 )
                {
                    Win32Declarations.DeleteObject( clipRgn );
                    clipRgn = IntPtr.Zero;
                }
                Win32Declarations.IntersectClipRect( hdc, (int) rcClip.Left, (int) rcClip.Top,
                    (int) rcClip.Right, (int) rcClip.Bottom );

                int ildState = ( ( state & RowState.ActiveSelected ) != 0 )
                    ? Win32Declarations.ILD_SELECTED
                    : Win32Declarations.ILD_NORMAL;

                Win32Declarations.ImageList_Draw( Core.ResourceIconManager.ImageList.Handle,
                                                    index, hdc, midPointX - 8, midPointY - 8, ildState );

                Win32Declarations.SelectClipRgn( hdc, clipRgn );
                Win32Declarations.DeleteObject( clipRgn );
            }
            finally
            {
                g.ReleaseHdc( hdc );
            }
        }

        protected override string GetItemText( object item )
        {
            return "";
        }

        public override string GetToolTip( JetListViewNode node, Rectangle rc, ref bool needPlace )
        {
            return null;
        }

        private void OnMouseClick(object sender, ItemMouseEventArgs e)
        {
            IResource res = (IResource) e.Item;
            IResourceType type = Core.ResourceStore.ResourceTypes[ res.Type ];
            if( type.HasFlag( ResourceTypeFlags.CanBeUnread ) )
            {
                bool unreadState = res.HasProp( Core.Props.IsUnread );
                SetResourceUnreadState( res, unreadState );

                //  If there is a command to propagate the reading status over
                //  the whole thread (conversation) we need to set exactly the
                //  same flag on all resources, not just toggle their flags
                //  forward.
                if( (Control.ModifierKeys & Keys.Control) > 0 )
                {
                    PropagateUnreadState2Thread( res, unreadState );
                }
                e.Handled = false;
            }
        }

        private delegate void AssignStatusDelegate( bool status, IResourceList list );
        public void  PropagateUnreadState2Thread( IResource res, bool state )
        {
            IResourceList subTree = ConversationBuilder.UnrollConversationFromCurrent( res );
            subTree = subTree.Minus( res.ToResourceList() );
            Core.ResourceAP.QueueJob( new AssignStatusDelegate( SetListUnreadState ), state, subTree );
        }

        private static void SetListUnreadState( bool state, IResourceList list )
        {
            foreach( IResource res in list )
                SetResourceUnreadState( res, state );
        }

        private static void  SetResourceUnreadState( IResource res, bool state )
        {
            if( state )
                new ResourceProxy( res ).DeleteProp( Core.Props.IsUnread );
            else
                new ResourceProxy( res ).SetProp( Core.Props.IsUnread, true );
        }
    }
}
