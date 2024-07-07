// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
    public class OpenFavoriteAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            for( int i = 0; i < context.SelectedResources.Count; ++i )
            {
                IResource res = context.SelectedResources[ i ];
                if ( String.Compare( res.Type, "WebLink", true ) != 0 )
                {
                    res = res.GetLinkProp( "Source" );
                }

                string url = res.GetPropText( "URL" );
                OpenUrl( url );
            }
        }

        public static void OpenUrl( string url )
        {
            if ( url.IndexOf( "://" ) == -1 )
            {
                url = "http://" + url;
            }
            try
            {
                Process.Start( url );
            }
            catch( Exception e )
            {
                MessageBox.Show( "Error opening Web link: "  + e.Message, "Bookmarks", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                FavoritesTools.IActionUpdateWeblinks( context, ref presentation );
            }
        }
    }

    public class RefreshFavoriteAction : IAction
    {
        public void Execute( IActionContext context )
        {
            for( int i = 0; i < context.SelectedResources.Count; ++i )
            {
                IResource res = context.SelectedResources[ i ];
                if( res.Type != "Weblink" )
                {
                    res = res.GetLinkProp( "Source" );
                }
                if( res != null && res.Type == "Weblink" )
                {
                    BookmarkService.ImmediateQueueWeblink( res, res.GetPropText( "URL" ) );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            FavoritesTools.IActionUpdateWeblinks( context, ref presentation );
        }
    }

    public class DeleteFavoriteAction : IAction
    {
        public void Execute( IActionContext context )
        {
            IResourceList resources = context.SelectedResources;
            if( resources.Count > 1 )
            {
                if( MessageBox.Show( Core.MainWindow,
                    "Are you sure you want to delete selected weblinks and/or folders?", "Delete Bookmarks",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question ) == DialogResult.No )
                {
                    return;
                }
            }
            for( int i = 0; i < resources.Count; ++i )
            {
                IResource res = context.SelectedResources[ i ];
                if( res.Type != "Folder" && res.Type != "Weblink" )
                {
                    res = res.GetLinkProp( "Source" );
                }
                if( res != null && ( res.Type == "Folder" || res.Type == "Weblink" ) )
                {
                    if( resources.Count == 1 )
                    {
                        if( res.Type != "Folder" || res.DisplayName != "New Folder" )
                        {
                            if( MessageBox.Show( Core.MainWindow,
                                "Are you sure you want to delete '" + res.DisplayName + "'?", "Delete Bookmark",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question ) == DialogResult.No )
                            {
                                return;
                            }
                        }
                    }
                    IBookmarkService bookmarkService =
                        (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
                    IBookmarkProfile profile = bookmarkService.GetOwnerProfile( res );
                    string error = null;
                    if( profile != null && profile.CanDelete( res, out error ) )
                    {
                        profile.Delete( res );
                    }
                    if( res.Type == "Folder" )
                    {
                        bookmarkService.DeleteFolder( res );
                    }
                    else
                    {
                        bookmarkService.DeleteBookmark( res );
                    }
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            FavoritesTools.IActionUpdateWeblinksOrFolders(
                context, ref presentation, FavoritesTools.ActionType.Delete );
        }
    }

    public class EditFavoritesPropertiesAction : IAction
    {
        public void Execute( IActionContext context )
        {
            if( context.SelectedResources.Count == 1 && context.SelectedResources[ 0 ].Type != "Folder" )
            {
                AddFavoriteForm.EditFavorite( context.SelectedResources[ 0 ] );
            }
            else
            {
                FavoritesPropertiesForm.EditFavoritesProperties( context.SelectedResources );
            }
        }
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            FavoritesTools.IActionUpdateWeblinksOrFolders(
                context, ref presentation, FavoritesTools.ActionType.Edit );
            if( presentation.Visible &&
                context.SelectedResources.Count == 1 && context.SelectedResources[ 0 ].Type == "Folder" )
            {
                IResourceList recursiveFavorites = Core.ResourceStore.EmptyResourceList;
                FavoritesPropertiesForm.RecursivelyUpdateResourceList(
                    ref recursiveFavorites, context.SelectedResources[ 0 ], true );
                presentation.Visible = recursiveFavorites.Count > 0;
            }
        }
    }

    /**
     * Action for adding a new favorite
     */
    public class AddFavoriteAction : IAction
    {
        public void Execute( IActionContext context )
        {
            IResourceList resources = context.SelectedResources;
            IResource folder = BookmarkService.GetBookmarksRoot();
            IResource selected = folder;
            string url = context.CurrentUrl;
            bool isWeblinkSelected = false;
            if( resources.Count > 0 )
            {
                selected = resources[ 0 ];
            }
            else
            {
                if( FavoritesPlugin._favoritesTreePane.SelectedNode != null )
                {
                    selected = FavoritesPlugin._favoritesTreePane.SelectedNode;
                }
            }
            if( selected.Type == "Folder" )
            {
                folder = selected;
            }
            else if( selected.Type == "Weblink" )
            {
                folder = BookmarkService.GetParent( selected );
                isWeblinkSelected = url != null && selected.GetPropText( FavoritesPlugin._propURL ) == url;
            }
            else
            {
                selected = selected.GetLinkProp( "Source" );
                if( selected != null && selected.Type == "Weblink" )
                {
                    folder = BookmarkService.GetParent( selected );
                    isWeblinkSelected = url != null && selected.GetPropText( FavoritesPlugin._propURL ) == url;
                }
            }

            using( AddFavoriteForm frm = new AddFavoriteForm( folder ) )
            {
                if( !isWeblinkSelected || Core.TabManager.CurrentTabId != "Web" )
                {
                    if( url != null )
                    {
                        frm.SetURL( url );
                    }
                    if( context.CurrentPageTitle != null )
                    {
                        frm._nameBox.Text = context.CurrentPageTitle;
                    }
                }
                frm.ShowDialog( Core.MainWindow );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if( context.Kind == ActionContextKind.Keyboard && Core.TabManager.CurrentTabId != "Web" )
            {
                presentation.Visible = false;
                return;
            }

            if( context.Kind == ActionContextKind.MainMenu || context.Kind == ActionContextKind.Keyboard ||
               ( context.CurrentUrl != null && context.CurrentUrl.Length > 0 ) )
            {
                return;
            }
            if( context.Kind == ActionContextKind.ContextMenu && context.Instance != FavoritesPlugin._favoritesTreePane )
            {
                presentation.Visible = false;
                return;
            }

            int count = context.SelectedResources.Count;
            if( presentation.Visible = count < 2 )
            {
                if( count == 1 )
                {
                    FavoritesTools.IActionUpdateWeblinksOrFolders(
                        context, ref presentation, FavoritesTools.ActionType.Create );
                }
            }
            presentation.Enabled = presentation.Visible;
            presentation.Visible = true;
        }
    }

    /**
     * Action for adding a new folder
     */
    public class AddFolderAction : IAction
    {
        public void Execute( IActionContext context )
        {
            if( context.SelectedResources.Count == 0 )
            {
                _parentFolder = BookmarkService.GetBookmarksRoot();
            }
            else
            {
                _parentFolder = context.SelectedResources[ 0 ];
                if( _parentFolder.Type == "Weblink" )
                {
                    _parentFolder = BookmarkService.GetParent( _parentFolder );
                }
            }
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( NewFolder ) );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if( context.Instance != FavoritesPlugin._favoritesTreePane )
            {
                presentation.Visible = false;
            }
            else
            {
                int count = context.SelectedResources.Count;
                if( presentation.Visible = count < 2 )
                {
                    if( count == 1 )
                    {
                        FavoritesTools.IActionUpdateWeblinksOrFolders(
                            context, ref presentation, FavoritesTools.ActionType.Create );
                    }
                }
                presentation.Enabled = presentation.Visible;
                presentation.Visible = true;
            }
        }

        private IResource _parentFolder;

        private void NewFolder()
        {
            IBookmarkService bookmarkService =
                (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
            IResource newFolder = bookmarkService.FindOrCreateFolder( _parentFolder, "New Folder" );
            IBookmarkProfile profile = bookmarkService.GetOwnerProfile( newFolder );
            string error = null;
            if( profile != null && profile.CanCreate( newFolder, out error ) )
            {
                profile.Create( newFolder );
            }
            Core.WorkspaceManager.AddToActiveWorkspaceRecursive( newFolder );
            Core.UIManager.QueueUIJob( new ResourceDelegate( FavoritesPlugin._favoritesTreePane.SelectResource ), newFolder );
            Core.UIManager.QueueUIJob( new ResourceDelegate( FavoritesPlugin._favoritesTreePane.EditResourceLabel ), newFolder );
        }
    }

    /**
     * Displays the contents of a favorite folder when the link to it is clicked.
     */
    public class FolderLinkClickAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Core.TabManager.CurrentTabId = "Web";
            Core.LeftSidebar.ActivateViewPane( "Favorites" );
            FavoritesPlugin._favoritesTreePane.SelectResource( context.SelectedResources [0] );
        }
    }

    /**
     * Send bookmark by email
     */
    public class SendBookmarkByEmail : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IEmailService service = (IEmailService) Core.PluginLoader.GetPluginService( typeof( IEmailService ) );
            IResource weblink = context.SelectedResources[ 0 ];
            string body = weblink.GetPropText( FavoritesPlugin._propURL );
            if( weblink.HasProp( "Annotation" ) )
            {
                body += "\r\n\r\n";
                body += weblink.GetPropText( "Annotation" );
            }
            service.CreateEmail( weblink.DisplayName, body, EmailBodyFormat.PlainText, (EmailRecipient[]) null, null, true );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            presentation.Visible = presentation.Visible &&
                Core.PluginLoader.GetPluginService( typeof( IEmailService ) ) != null;
        }
    }

    /**
     * Mark bookmark as read/unread
     */
    public class MarkAsReadAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            Core.ResourceAP.QueueJob(
                JobPriority.Immediate, new ResourceListDelegate( MarkAsRead ), context.SelectedResources );
        }

        private static void MarkAsRead( IResourceList resources )
        {
            for( int i = 0; i < resources.Count; ++i )
            {
                IResource res = resources[ i ];
                if ( String.Compare( res.Type, "WebLink", true ) == 0 )
                {
                    res = res.GetLinkProp( "Source" );
                }
                if( res != null )
                {
                    res.SetProp( Core.Props.IsUnread, !res.HasProp( Core.Props.IsUnread ) );
                }
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                FavoritesTools.IActionUpdateWeblinks( context, ref presentation );
                if( presentation.Visible )
                {
                    string text = "Mark as Unread";
                    foreach( IResource res in context.SelectedResources.ValidResources )
                    {
                        IResource source = res;
                        if( source.Type == "Weblink" )
                        {
                            source = source.GetLinkProp( "Source" );
                        }
                        if( source != null && source.HasProp( Core.Props.IsUnread ) )
                        {
                            text = "Mark as Read";
                            break;
                        }
                    }
                    presentation.Text = text;
                }
            }
        }
    }

    /**
     * Action for annotation and categorization of a weblink
     */
    public class AnnotateAndCategorizeWeblinkAction: IAction
    {
        public void Execute( IActionContext context )
        {
            string url = context.CurrentUrl;
            FavoritesPlugin.RemoteAnnotateWeblink( url, context.CurrentPageTitle );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            string url = context.CurrentUrl;
            presentation.Visible = url != null && url.Length > 0;
        }
    }

    /**
     * Hides the Parent link for top-level favorites and folders.
     */
    internal class WebLinksPaneFilter: ILinksPaneFilter
    {
        public bool AcceptLinkType( IResource displayedResource, int propId, ref string displayName )
        {
            return true;
        }

        public bool AcceptLink( IResource displayedResource, int propId, IResource targetResource,
            ref string linkTooltip )
        {
            if ( propId == FavoritesPlugin._propParent && targetResource == BookmarkService.GetBookmarksRoot() )
            {
                return false;
            }
            return true;
        }

        public bool AcceptAction(IResource displayedResource, IAction action)
        {
            return true;
        }
    }
}
