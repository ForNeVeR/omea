// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Text;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
    internal class FavoritesTools
    {
        /**
         * returns action flags for group of favorites (IAction.Update methods)
         */
        public static void IActionUpdateWeblinks( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources == null || context.SelectedResources.Count == 0 )
            {
                presentation.Visible = false;
            }
            else
            {
                for( int i = 0; i < context.SelectedResources.Count; ++i )
                {
                    IResource res = context.SelectedResources[ i ];
                    if( res.Type != "Weblink" )
                    {
                        res = res.GetLinkProp( "Source" );
                    }
                    if( res == null || res.Type != "Weblink" || res.GetStringProp( "URL" ) == null )
                    {
                        presentation.Visible = false;
                        break;
                    }
                }
            }
        }

        public enum ActionType
        {
            Create, Delete, Update, Edit
        }

        /**
         * returns action flags for group of weblinks or folders (IAction.Update methods)
         */
        public static void IActionUpdateWeblinksOrFolders(
            IActionContext context, ref ActionPresentation presentation, ActionType type )
        {
            if ( context.SelectedResources == null || context.SelectedResources.Count == 0 )
            {
                presentation.Visible = false;
            }
            else
            {
                IBookmarkService service =
                    (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
                for( int i = 0; i < context.SelectedResources.Count; ++i )
                {
                    IResource res = context.SelectedResources[i];
                    if( res.Type != "Folder" && res.Type != "Weblink" )
                    {
                        res = res.GetLinkProp( "Source" );
                        if( res == null || ( res.Type != "Weblink" && res.Type != "Folder" ) )
                        {
                            presentation.Visible = false;
                            return;
                        }
                    }
                    IBookmarkProfile profile = service.GetOwnerProfile( res );
                    string error = null;
                    if( profile != null )
                    {
                        switch( type )
                        {
                            case ActionType.Create:
                            {
                                if( !profile.CanCreate( null, out error ) )
                                {
                                    presentation.Visible = false;
                                }
                                break;
                            }

                            case ActionType.Update:
                            {
                                if( !profile.CanRename( res, out error ) )
                                {
                                    presentation.Visible = false;
                                }
                                break;
                            }
                            case ActionType.Delete:
                            {
                                if( !profile.CanDelete( res, out error ) )
                                {
                                    presentation.Visible = false;
                                }
                                break;
                            }
                            case ActionType.Edit:
                            {
                                break;
                            }
                        }
                        if( !presentation.Visible && error != null && error.Length > 0 )
                        {
                            presentation.ToolTip = error;
                        }
                    }
                }
            }
        }

        /**
         * Trace if corresponding setting allowes
         */
        public static void TraceIfAllowed( string traceLine )
        {
            if( _trace == null )
            {
                _trace = Core.SettingStore.ReadBool( "Favorites", "Trace", false );
            }
            if( (bool) _trace )
            {
                Trace.WriteLine( traceLine, "Favorites.Plugin" );
            }
        }

        private static object _trace = null;

        /**
         * Replace invalid chars in name of bookmark with dots
         */
        public static string GetSafeBookmarkName( IBookmarkProfile profile, string name )
        {
            if( profile.InvalidNameChars.Length == 0 )
            {
                return name;
            }
            char[] invalidChars = (char[]) profile.InvalidNameChars.Clone();
            Array.Sort( invalidChars );
            StringBuilder builder = new StringBuilder( name.Length );
            for( int i = 0; i < name.Length; ++i )
            {
                if( Array.BinarySearch( invalidChars, name[ i ] ) >= 0 )
                {
                    builder.Append( '.' );
                }
                else
                {
                    builder.Append( name[ i ] );
                }
            }
            return builder.ToString();
        }
    }
}
