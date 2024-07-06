// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using System.IO;
using System.Reflection;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.FiltersManagement
{
    internal class ViewIconProvider : IResourceIconProvider
    {
        readonly Assembly _runner = Assembly.GetExecutingAssembly();
        Icon        _general;
        Icon        _trash, _date, _searchPure, _search;

        public Icon GetResourceIcon( IResource res )
        {
            if( res.HasProp( "IsDeletedResourcesView" ) )
            {
                LoadIcon( ref _trash, "trash.ico" );
                return _trash;
            }
            else
            if( res.HasProp( "TimingView"  ))
            {
                LoadIcon( ref _date, "calendar.ico" );
                return _date;
            }
            else
            if( res.GetPropText( "DeepName" ).Equals( FilterManagerProps.SearchResultsViewName ) )
            {
                LoadIcon( ref _searchPure, "folderfind.ico" );
                return _searchPure;
            }
            else
            {
                IResourceList conditions = Core.FilterRegistry.GetConditionsPlain( res );
                foreach( IResource cond in conditions )
                {
                    if( cond.GetIntProp( Core.FilterRegistry.Props.OpProp ) == (int)ConditionOp.QueryMatch )
                    {
                        LoadIcon( ref _search, "search.ico" );
                        return _search;
                    }
                }
            }
            return GetDefaultIcon( res.Type );
        }

        public Icon GetDefaultIcon( string resType )
        {
            if( _general == null )
            {
                Stream strm = _runner.GetManifestResourceStream("OmniaMea.Icons.view.new.ico");
                _general = new Icon( strm );
            }
            return _general;
        }

        private void LoadIcon( ref Icon icon, string path )
        {
            if( icon == null )
            {
                Stream strm = _runner.GetManifestResourceStream( "OmniaMea.Icons." + path );
                icon = new Icon( strm );
            }
        }
    }
}
