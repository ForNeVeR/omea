// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;

namespace JetBrains.JetListViewLibrary.Tests
{
    internal class MockJetListViewColumn : JetListViewPreviewColumn
    {
        private Rectangle _lastDrawItemRect;
        private Point _lastMouseDownPoint = new Point( -1, -1 );
        private int _autoPreviewHeight;
        private object _lastUpdatedItem;

        public int AutoPreviewHeight
        {
            get { return _autoPreviewHeight; }
            set { _autoPreviewHeight = value; }
        }

        protected internal override void DrawItem( Graphics g, Rectangle rc, object item,
                                                   RowState state, string highlightText )
        {
            _lastDrawItemRect = rc;
        }

        public Rectangle LastDrawItemRect
        {
            get { return _lastDrawItemRect; }
        }

        protected internal override MouseHandleResult HandleMouseDown( JetListViewNode node, int x, int y )
        {
            _lastMouseDownPoint = new Point( x, y );
            return 0;
        }

        public Point LastMouseDownPoint
        {
            get { return _lastMouseDownPoint; }
        }

        public override int GetAutoPreviewHeight( JetListViewNode node )
        {
            return _autoPreviewHeight;
        }

        protected internal override void UpdateItem( object item )
        {
            _lastUpdatedItem = item;
        }

        public object LastUpdatedItem
        {
            get { return _lastUpdatedItem; }
        }
    }
}
