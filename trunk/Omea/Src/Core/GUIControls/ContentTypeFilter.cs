/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /**
     * Resource tree filter which checks if the nodes have the content type
     * matching the specified resource types.
     */

    public class ContentTypeFilter: IResourceNodeFilter
    {
        private string[] _resTypes;
        private int      _linkType;
        private bool     _viewsExclusive;

        public void SetFilter( string[] resTypes, int linkType )
        {
            _resTypes = resTypes;
            _linkType = linkType;
            if ( resTypes != null )
            {
                for( int i=0; i<_resTypes.Length; i++ )
                {
                    if ( Core.ResourceTreeManager.AreViewsExclusive( resTypes [i] ) )
                    {
                        _viewsExclusive = true;
                    }
                }
            }
        }

        /**
         * If the filter is exclusive, accepts only nodes with ContentType matching
         * one of the selected resource types.
         * Otherwise, accepts nodes with null OR matching ContentType.
         */

        public bool AcceptNode( IResource res, int level )
        {
            string contentType = null;
            int[] contentLinks = null;

            //-----------------------------------------------------------------
            //  Check whether a resource is exclusively ascribed to any
            //  particular workspace(s).
            //-----------------------------------------------------------------
            if( !IsWorkspaceFit( res ))
                return false;

            //-----------------------------------------------------------------
            //  Views (and folders?) which are marked with special sign are
            //  visible everywhere.
            //-----------------------------------------------------------------
            if( res.Type == FilterManagerProps.ViewResName )
            {
                if( Core.FilterRegistry.IsVisibleInAllTabs( res ))
                    return true;
            }
                
            //-----------------------------------------------------------------
            //  Content type for each ViewFolder is an aggregation of content
            //  types of all of its views. For other resources (e.g. views) their
            //  content type is stored directly as property.
            //-----------------------------------------------------------------
            bool  containsAllTypeViews = false;
            if( res.Type == FilterManagerProps.ViewFolderResName)
            {
                IResourceList views = res.GetLinksOfType( FilterManagerProps.ViewResName, "Parent" );

                //  If a folder contains at lease one view - define a folder
                //  visibility from its views, otherwise, try to deduce its
                //  visibility from the possible "contentType" param stored
                //  directly in the folder (e.g. when folder is first created
                //  in the "exclusive" tab.
                if( views.Count > 0 )
                {
                    //  If at least one view has mark "Show in all tabs" (e.g. views
                    //  Flagged, Annotated) then the folder has to be seen everywhere.
                    foreach( IResource view in views )
                    {
                        if( IsWorkspaceFit( view ) && 
                            Core.FilterRegistry.IsVisibleInAllTabs( view ) )
                        {
                            containsAllTypeViews = true;
                            break;
                        }
                    }

                    if( !containsAllTypeViews )
                    {
                        foreach( IResource view in views )
                        {
                            if( IsWorkspaceFit( view ) )
                            {
                                string  ct = view.GetStringProp( "ContentType" );

                                //  If at least one view has "null" content type,
                                //  then the folder has to be seen everywhere.
                                if( ct == null )
                                {
                                    contentType = null;
                                    break;
                                }

                                if( contentType != null )
                                    contentType += '|';
                                contentType += ct;
                            }
                        }
                    }
                }
                else
                {
                    contentType = res.GetStringProp( "ContentType" );
                }
            }
            else
            {
                contentType = res.GetStringProp( "ContentType" );
                contentLinks = ParseContentLinks( res );
            }

            //-----------------------------------------------------------------
            if( containsAllTypeViews )
            {
                return true;
            }

            if ( contentType == null && contentLinks == null )
            {
                return ( res.Type == "ResourceTreeRoot" ) ||
                       ( res.Type == "Category" ) || !_viewsExclusive;
            }

            if ( _resTypes == null )
            {
                return (contentType == null) || CanBeShownInAllResources( contentType );
            }

            if ( contentType != null && MatchContentType( contentType ) )
            {
                return true;
            }

            if ( contentLinks != null && Array.IndexOf( contentLinks, _linkType ) >= 0 )
            {
                return true;
            }

            return false;
        }

        private static int[] ParseContentLinks( IResource res )
        {
            string contentLinks = res.GetStringProp( "ContentLinks" );
            if ( contentLinks == null )
            {
                return null;
            }

            string[] linkNames = contentLinks.Split( '|' );
            int[] linkIds = new int [linkNames.Length];
            for( int i=0; i<linkNames.Length; i++ )
            {
                linkIds [i] = Core.ResourceStore.GetPropId( linkNames [i] );
            }
            return linkIds;
        }

        private bool MatchContentType( string contentType )
        {
            //  No delimiter => 0-th element == source string.
            string[] contentTypes = contentType.Split( '|' );
            for( int i=0; i<contentTypes.Length; i++ )
            {
                for( int j=0; j<_resTypes.Length; j++ )
                {
                    if ( contentTypes [i] == _resTypes [j] ) 
                        return true;
                }
            }
            return false;
        }

        private static bool CanBeShownInAllResources( string contentType )
        {
            string[] contentTypes = contentType.Split( '|' );
            bool     anyTypeNonExclusive = false;

            foreach( string ct in contentTypes )
            {
                if( Core.ResourceStore.ResourceTypes.Exist( ct ) &&
                    Core.ResourceStore.ResourceTypes [ct].OwnerPluginLoaded )
                {
                    anyTypeNonExclusive = anyTypeNonExclusive || 
                                         !Core.ResourceTreeManager.AreViewsExclusive( ct );
                }
            }
            return anyTypeNonExclusive;
        }

        private static bool IsWorkspaceFit( IResource res )
        {
            bool result = true;

            IResourceList inWsps = res.GetLinksOfType( null, "InWorkspace" );
            if( inWsps.Count > 0 )
            {
                IResource currWsp = Core.WorkspaceManager.ActiveWorkspace;
                result = (currWsp == null ) || (inWsps.IndexOf( currWsp ) != -1);
            }
            return result;
        }
    }
}
