// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using System.Drawing;
using System.IO;
using System.Reflection;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Custom column "Categories".
	/// </summary>
	internal class CategoriesColumn: LinkIconManagerColumn
	{
		public CategoriesColumn()
               : base( Core.ResourceStore.GetPropId( "Category" ) )  {}

	    public override string GetTooltip( IResource res )
	    {
            string  tooltip = string.Empty;
            IResourceList categories = res.GetLinksOfType( "Category", "Category" );
            foreach( IResource cat in categories )
            {
                tooltip += cat.DisplayName + "; ";
            }
            if( tooltip.Length > 0 )
                tooltip = tooltip.Substring( 0, tooltip.Length - 2 );
            return tooltip;
	    }
	}

    internal class CategoryIconProvider : IResourceIconProvider
    {
        private Hashtable   _changedIcons = new Hashtable();
        private Icon        _default;

        public CategoryIconProvider()
        {
            IResourceList categories = Core.ResourceStore.GetAllResources( "Category" );
            foreach( IResource cat in categories )
            {
                Stream strm = cat.GetBlobProp( "IconBlob" );
                if( strm != null )
                {
                    Icon icon = new Icon( strm );
                    _changedIcons[ cat.Id ] = icon;
                }
            }
        }

        public Icon GetResourceIcon( IResource res )
        {
            object icon = _changedIcons[ res.Id ];
            if( icon != null )
            {
                return (Icon) icon;
            }
            else
            if ( _default == null )
            {
               _default = MainFrame.LoadIconFromAssembly( "categories.ico" );
            }
            return _default;
        }

        public Icon GetDefaultIcon( string resType )
        {
            return null;
        }

        public void  UpdateIcon( IResource category )
        {
            Stream strm = category.GetBlobProp( "IconBlob" );
            if( strm != null )
            {
                Icon icon = new Icon( strm );
                _changedIcons[ category.Id ] = icon;
            }
            else
            if( _changedIcons.ContainsKey( category.Id ) )
            {
                _changedIcons.Remove( category.Id );
            }
        }
    }
}
