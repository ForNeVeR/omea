// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// Column class which draws column values and handles column operations through the
    /// ICustomColumn interface.
    /// </summary>
    public class ResourceListViewCustomColumn: ResourcePropsColumn
    {
        private ICustomColumn[] _customColumns;
        private ICustomColumn _defaultCustomColumn;
        private IContextProvider _contextProvider;

        public ResourceListViewCustomColumn( int[] propIds, ICustomColumn[] customColumns )
            : base( propIds )
        {
            _customColumns = customColumns;
            for( int i=0; i<_customColumns.Length; i++ )
            {
                if ( _customColumns [i] != null )
                {
                    _defaultCustomColumn = _customColumns [i];
                    break;
                }
            }
        }

        public IContextProvider ContextProvider
        {
            get { return _contextProvider; }
            set { _contextProvider = value; }
        }

        protected override void DrawHeader( Graphics g, Rectangle bounds )
        {
            if ( _defaultCustomColumn != null )
            {
                _defaultCustomColumn.DrawHeader( g, bounds );
            }
        }

        protected override void DrawItem( Graphics g, Rectangle rc, object item, RowState state, string highlightText )
        {
            if ( Core.State == CoreState.ShuttingDown )
            {
                return;
            }

            if ( HasNonfixedColumnBefore() )
            {
                DrawItemBackground( g, rc, rc, item, state, null );
            }
            IResource res = (IResource) item;
            ICustomColumn col = GetCustomColumn( res );
            if ( col != null )
            {
                col.Draw( res, g, rc );
            }
        }

        public override string GetToolTip( JetListViewNode node, Rectangle rc, ref bool needPlace )
        {
            IResource res = (IResource) node.Data;
            ICustomColumn col = GetCustomColumn( res );
            if ( col != null )
            {
                needPlace = false;
                return col.GetTooltip( res );
            }
            return null;
        }

        protected override string GetItemText( object item )
        {
            return "";
        }

        public override bool MatchIncrementalSearch( JetListViewNode curNode, string text )
        {
            return false;
        }

        protected override MouseHandleResult HandleMouseDown( JetListViewNode node, int x, int y )
        {
            IResource res = (IResource) node.Data;
            ICustomColumn col = GetCustomColumn( res );
            if ( col != null )
            {
                col.MouseClicked( res, new Point( x, y ) );
            }
            return MouseHandleResult.FocusOnMouseDown;
        }

        protected override bool HandleContextMenu( JetListViewNode node, int x, int y )
        {
            if ( _contextProvider != null )
            {
                IResource res = (IResource) node.Data;
                ICustomColumn col = GetCustomColumn( res );
                IActionContext context = _contextProvider.GetContext( ActionContextKind.ContextMenu );
                if ( col.ShowContextMenu( context, OwnerControl, new Point( x, y ) ) )
                {
                    return true;
                }
            }
            return false;
        }

        private ICustomColumn GetCustomColumn( IResource res )
        {
            for( int i=0; i<_propIds.Length; i++ )
            {
                if ( _customColumns [i] != null && res.HasProp( _propIds [i] ) )
                {
                    return _customColumns [i];
                }
            }
            return _defaultCustomColumn;
        }

        private bool HasNonfixedColumnBefore()
        {
            if ( OwnerControl == null || OwnerControl.MultiLineView )
            {
                return true;
            }
            int index = OwnerControl.Columns.IndexOf( this );
            for( int i = index-1; i>=0; i-- )
            {
                if ( !OwnerControl.Columns [i].FixedSize )
                {
                    return true;
                }
            }
            return false;
        }

        public override bool AcceptColumnDoubleClick
        {
            get { return false; }
        }
    }
}
