// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.UI.RichText;

namespace JetBrains.Omea.Favorites
{
    [PluginDescription("Web Browser Favorites", "JetBrains Inc.", "Support for Mozilla bookmarks and Internet Explorer Favorites.", PluginDescriptionFormat.PlainText, "Icons/FavoritesPluginIcon.png")]
    public class FavoritesPlugin : IPlugin, IStreamProvider, IResourceTextProvider, IResourceDisplayer,
                                   IDisplayPane2, IResourceUIHandler, IResourceIconProvider
    {
        #region IPlugin Members

        public void Register()
        {
            _bookmarkService = new BookmarkService();

            RegisterTypes();

            IUIManager uiMgr = Core.UIManager;
            uiMgr.RegisterWizardPane( "Import Bookmarks", ImportBookmarksOptionsPane.StartupWizardPaneCreator, 1 );
            uiMgr.RegisterOptionsGroup( "Internet", "The Internet options enable you to control how [product name] works with several types of online content." );
            OptionsPaneCreator favoritesPaneCreator = FavoritesOptionsPane.FavoritesOptionsPaneCreator;
            uiMgr.RegisterOptionsPane( "Internet", "Favorites", favoritesPaneCreator,
                "The Favorites options enable you to control how your Internet Explorer Favorites are imported and synchronized with Internet Explorer." );
            ImportBookmarksOptionsPane.AddPane( favoritesPaneCreator );

            if( OperaBookmarkProfile.OperaBookmarksPath().Length > 0  )
            {
                OptionsPaneCreator operaPaneCreator = OperaOptionsPane.CreatePane;
                uiMgr.RegisterOptionsPane( "Internet", "Opera Bookmarks", operaPaneCreator,
                    "The Opera Bookmarks options enable you to control how your Opera Bookmarks are imported." );
                ImportBookmarksOptionsPane.AddPane( operaPaneCreator );
            }

            OptionsPaneCreator downloadOptionsPaneCreator = DownloadOptionsPane.DownloadOptionsPaneCreator;
            uiMgr.RegisterOptionsPane( "Internet", "Web Pages", downloadOptionsPaneCreator,
                "The Web Pages options enable you to control how your bookmarked Web pages are downloaded." );
            IPropTypeCollection propTypes = Core.ResourceStore.PropTypes;
            int sourceId = propTypes[ "Source" ].Id;
            propTypes.RegisterDisplayName( sourceId, "Web Bookmark" );
            Core.TabManager.RegisterResourceTypeTab( "Web", "Web", new[] { "Weblink", "Folder" }, sourceId, 4 );
            uiMgr.RegisterResourceLocationLink( "Weblink", _propParent, "Folder" );
            uiMgr.RegisterResourceLocationLink( "Folder", _propParent, "Folder" );
            uiMgr.RegisterResourceSelectPane( "Weblink", typeof(ResourceTreeSelectPane) );
            Core.WorkspaceManager.RegisterWorkspaceType( "Weblink",
                new[] { Core.ResourceStore.PropTypes[ "Source" ].Id }, WorkspaceResourceType.Container );
            Core.WorkspaceManager.RegisterWorkspaceFolderType( "Folder", "Weblink", new[] { _propParent } );
            IPluginLoader loader = Core.PluginLoader;
            loader.RegisterResourceDisplayer( "Weblink", this );
            loader.RegisterStreamProvider( "Weblink", this );
            loader.RegisterResourceUIHandler( "Folder", this );
            loader.RegisterResourceUIHandler( "Weblink", this );
            loader.RegisterResourceTextProvider( null, this );
            _favIconManager = (FavIconManager) Core.GetComponentImplementation( typeof( FavIconManager ) );

            Image img = Utils.TryGetEmbeddedResourceImageFromAssembly( Assembly.GetExecutingAssembly(), "Favorites.Icons.Favorites24.png" );
            _favoritesTreePane = Core.LeftSidebar.RegisterResourceStructureTreePane( "Favorites", "Web", "Bookmarks", img, "Weblink" );
            _favoritesTreePane.WorkspaceFilterTypes = new[] { "Weblink", "Folder" };
            _favoritesTreePane.EnableDropOnEmpty( this );
            JetResourceTreePane realPane = (JetResourceTreePane)_favoritesTreePane;
            realPane.AddNodeDecorator( new WeblinkNodeDecorator() );
            realPane.AddNodeFilter( new BookmarkProfileFilter() );
            _favoritesTreePane.ToolTipCallback = DisplayWeblinkError;
            Core.LeftSidebar.RegisterViewPaneShortcut( "Favorites", Keys.Control | Keys.Alt | Keys.B );

            Core.ResourceIconManager.RegisterResourceIconProvider( "Weblink", this );
            Core.ResourceIconManager.RegisterPropTypeIcon( Core.ResourceStore.PropTypes[ "Source" ].Id, LoadIconFromAssembly( "favorites1.ico" ) );
            _emptyWeblinkIcon = LoadIconFromAssembly( "weblink_empty.ico" );
            _errorWeblinkIcon = LoadIconFromAssembly( "weblink_error.ico" );
            WebLinksPaneFilter filter = new WebLinksPaneFilter();
            Core.ResourceBrowser.RegisterLinksPaneFilter( "Weblink", filter );
            Core.ResourceBrowser.RegisterLinksPaneFilter( "Folder", filter );
            Core.ResourceBrowser.RegisterResourceDisplayForwarder( "Weblink", ForwardWeblinkDisplay );
            Core.FilterEngine.RegisterRuleApplicableResourceType( "Weblink" );

            Core.RemoteControllerManager.AddRemoteCall( "Favorites.ExportMozillaBookmarkChanges.1",
                new RemoteExportMozillaBookmarkChangesDelegate( RemoteExportMozillaBookmarkChanges ) );
            Core.RemoteControllerManager.AddRemoteCall( "Favorites.SetMozillaBookmarkId.1",
                new RemoteSetMozillaBookmarkIdDelegate( RemoteSetMozillaBookmarkId ) );
            Core.RemoteControllerManager.AddRemoteCall( "Favorites.RefreshMozillaBookmarks.1",
                new RemoteRefreshBookmarksDelegate( RemoteRefreshBookmarks ) );
            Core.RemoteControllerManager.AddRemoteCall( "Favorites.AnnotateWeblink.1",
                new RemoteAnnotateWeblinkDelegate( RemoteAnnotateWeblink ) );

            Core.DisplayColumnManager.RegisterDisplayColumn( "Weblink", 0,
                new ColumnDescriptor( new[] { "Name", "DisplayName" }, 300, ColumnDescriptorFlags.AutoSize ) );
            Core.DisplayColumnManager.RegisterDisplayColumn( "Weblink", 1, new ColumnDescriptor( "LastUpdated", 120 ) );

            /**
             * bookmark profiles
             */
            Core.PluginLoader.RegisterPluginService( _bookmarkService );
            _favoritesProfile = new IEFavoritesBookmarkProfile( _bookmarkService );
            _bookmarkService.RegisterProfile( _favoritesProfile );
            if( OperaBookmarkProfile.OperaBookmarksPath().Length > 0  )
            {
                _operaProfile = new OperaBookmarkProfile( _bookmarkService );
                _bookmarkService.RegisterProfile( _operaProfile );
            }
            if( MozillaProfiles.PresentOnComputer )
            {
                OptionsPaneCreator mozillaPaneCreator = MozillaOptionsPane.MozillaOptionsPaneCreator;
                uiMgr.RegisterOptionsPane( "Internet", "Mozilla Bookmarks", mozillaPaneCreator,
                    "The Mozilla Bookmarks options enable you to select Mozilla or Firefox profile which bookmarks are imported." );
                ImportBookmarksOptionsPane.AddPane( mozillaPaneCreator );
                RegisterMozillaProfiles( MozillaProfiles.GetMozillaProfiles() );
                RegisterMozillaProfiles( MozillaProfiles.GetFirefoxProfiles() );
                RegisterMozillaProfiles( MozillaProfiles.GetFirefox09Profiles() );
                RegisterMozillaProfiles( MozillaProfiles.GetAbsoluteFirefoxProfiles() );
                MozillaBookmarkProfile.SetImportPropertiesOfProfiles();
            }
        }

        public void Startup()
        {
            Core.StateChanged += Core_StateChanged;

            _favorites = Core.ResourceStore.GetAllResourcesLive( "Weblink" );
            _favorites.ResourceChanged += WeblinkOrFolderChanged;
            _folders = Core.ResourceStore.GetAllResourcesLive( "Folder" );
            _folders.ResourceChanged += WeblinkOrFolderChanged;
        }

        public void Shutdown()
        {
            IBookmarkProfile[] profiles = _bookmarkService.Profiles;
            foreach( IBookmarkProfile profile in profiles )
            {
                _bookmarkService.DeRegisterProfile( profile );
            }
        }

        #endregion

        #region IStreamProvider Members

        public Stream GetResourceStream( IResource resource )
        {
            return resource.GetBlobProp( _propContent );
        }

        #endregion

        #region IResourceTextProvider Members

        public bool ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            IResource source = res.GetLinkProp( "Source" );
            if( source != null && source.Type == "Weblink" )
            {
                string name = source.GetPropText( Core.Props.Name );
                if( name.Length > 0 )
                {
                    consumer.AddDocumentHeading( res.Id, name );
                }
            }
            return true;
        }

        #endregion

        #region IResourceDisplayer Members

        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            return ( resourceType == "Weblink" ) ? this : null;
        }

        #endregion

        #region IDisplayPane Members

        Control IDisplayPane.GetControl()
        {
            return Core.WebBrowser;
        }

        private IResource ForwardWeblinkDisplay( IResource resource )
        {
            if( Core.State != CoreState.Running )
            {
                return resource;
            }
            try
            {
                string url = resource.GetPropText( _propURL );
                if( BookmarkService.DownloadMethod == 2 )
                {
                    if( _lastDisplayedWeblink != resource )
                    {
                        BookmarkService.ImmediateQueueWeblink( resource, url );
                    }
                    return resource;
                }
                Core.ResourceBrowser.ShowUrlBar( url );
                IResource source = resource.GetLinkProp( "Source" );
                if ( source != null )
                {
                    return source;
                }
                return resource;
            }
            finally
            {
                _lastDisplayedWeblink = resource;
            }
        }

        public void DisplayResource( IResource resource )
        {
            string url = resource.GetPropText( _propURL );
            if ( BookmarkService.DownloadMethod == 2 )
            {
                if( Core.WebBrowser.CurrentUrl != url )
                {
                    Core.WebBrowser.NavigateInPlace( url );
                    Core.ResourceBrowser.ShowUrlBar( url );
                }
            }
            else
            {
                IResource source = resource.GetLinkProp( "Source" );
                if ( source == null )
                {
                    string lastError = resource.GetPropText( Core.Props.LastError );
                    if( lastError.Length == 0 )
                    {
                        if( url.Length > 0 )
                        {
                            Core.WebBrowser.ShowHtml( "<html><body>Downloading...</body></html>", WebSecurityContext.Trusted, null );
                            BookmarkService.ImmediateQueueWeblink( resource, url );
                        }
                    }
                    else
                    {
                        string	err = lastError.Replace( "&", "&amp;" ).Replace( "\"", "&quot;" ).Replace( "<", "&lt;" ).Replace( ">", "&gt;" );
                        Core.WebBrowser.ShowHtml(
                            "<html><body><b>Bookmark could not be downloaded</b><br><br>" +
                            err + "</body></html>", WebSecurityContext.Trusted, null );
                    }
                }
            }
        }

        public void DisplayResource( IResource resource, WordPtr[] wordsToHighlight )
        {
            DisplayResource( resource );
            if( wordsToHighlight != null )
            {
                HighlightWords( wordsToHighlight );
            }
        }

        public void EndDisplayResource( IResource resource )
        {
            _lastDisplayedWeblink = null;
        }

        public void DisposePane()
        {
        }

        public void HighlightWords( WordPtr[] words )
        {
        }

        bool ICommandProcessor.CanExecuteCommand( string action )
        {
            return Core.WebBrowser.CanExecuteCommand( action );
        }

        void ICommandProcessor.ExecuteCommand( string action )
        {
            Core.WebBrowser.ExecuteCommand( action );
        }

        #endregion

        public string GetSelectedText( ref TextFormat format )
        {
            format = TextFormat.Html;
            return Core.WebBrowser.SelectedHtml;
        }

        public string GetSelectedPlainText()
        {
            return Core.WebBrowser.SelectedText;
        }

        public Icon GetResourceIcon( IResource resource )
        {
            if( resource.Type == "Weblink" )
            {
                if( resource.HasProp( Core.Props.LastError ) )
                {
                    return _errorWeblinkIcon;
                }

                IResource source = resource.GetLinkProp( "Source" );
                if( source == null )
                {
                    return _emptyWeblinkIcon;
                }

                string url = resource.GetStringProp( _propURL );
                if( url != null )
                {
                    Icon icon = _favIconManager.GetResourceFavIcon( url );
                    if ( icon != null )
                    {
                        return icon;
                    }
                    _favIconManager.DownloadFavIcon( url );
                }
                string favIconUrl = resource.GetStringProp( _propFaviconUrl );
                if( favIconUrl != null )
                {
                    if( url != null )
                    {
                        try { favIconUrl = new Uri( new Uri( url ), favIconUrl ).AbsoluteUri; }
                        catch {}
                    }
                    Icon icon = _favIconManager.GetFavIcon( favIconUrl );
                    if ( icon != null )
                    {
                        return icon;
                    }
                    _favIconManager.DownloadFavIcon( favIconUrl );
                }

                IResourceIconProvider provider = Core.ResourceIconManager.GetResourceIconProvider( source.Type );
                if ( provider != null )
                {
                    return provider.GetResourceIcon( source );
                }
            }
            return null;
        }

        public Icon GetDefaultIcon( string resType )
        {
            return _emptyWeblinkIcon;
        }

        #region IResourceUIHandler Members

        public bool CanDropResources( IResource targetResource, IResourceList dragResources )
        {
            if( dragResources.Count > 0 )
            {
                if( targetResource != _bookmarkService.BookmarksRoot )
                {
                    if( targetResource.Type != "Folder" )
                    {
                        return false;
                    }
                    IBookmarkProfile targetProfile = _bookmarkService.GetOwnerProfile( targetResource );
                    foreach( IResource dragRes in dragResources )
                    {
                        string error;
                        IBookmarkProfile sourceProfile = _bookmarkService.GetOwnerProfile( dragRes );
                        if( sourceProfile == targetProfile )
                        {
                            if( targetProfile != null && !targetProfile.CanMove( dragRes, targetResource, out error ) )
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if( sourceProfile != null && !sourceProfile.CanDelete( dragRes, out error ) )
                            {
                                return false;
                            }
                            if( targetProfile != null && !targetProfile.CanCreate( dragRes, out error ) )
                            {
                                return false;
                            }
                        }
                    }
                    IResource temp = targetResource;
                    do
                    {
                        if( dragResources.IndexOf( temp ) >= 0 )
                        {
                            return false;
                        }
                        temp = BookmarkService.GetParent( temp );
                    } while( temp != null );
                }
                string[] types = dragResources.GetAllTypes();
                if( types.Length < 3 )
                {
                    return ( types[ 0 ] == "Weblink" || types[ 0 ] == "Folder" ) &&
                        ( types.Length == 1 || types[ 1 ] == "Weblink" || types[ 1 ] == "Folder" );
                }
            }
            return false;
        }

        public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
        {
            IBookmarkProfile targetProfile = _bookmarkService.GetOwnerProfile( targetResource );

            foreach( IResource dropRes in droppedResources )
            {
                IResource oldParent = BookmarkService.GetParent( dropRes );
                IBookmarkProfile sourceProfile = _bookmarkService.GetOwnerProfile( dropRes );
                if( sourceProfile == targetProfile )
                {
                    if( targetProfile != null )
                    {
                        targetProfile.Move( dropRes, targetResource, oldParent );
                    }
                    new ResourceProxy( dropRes ).SetProp( _propParent, targetResource );
                }
                else
                {
                    if( sourceProfile != null )
                    {
                        sourceProfile.Delete( dropRes );
                    }
                    new ResourceProxy( dropRes ).SetProp( _propParent, targetResource );
                    if( targetProfile != null )
                    {
                        targetProfile.Create( dropRes );
                    }
                }
                // the resource may have been moved from a parent which belonged to a workspace
                // to the top level (#5066)
                if ( dropRes.Type == "Weblink" )
                {
                    Core.WorkspaceManager.AddToActiveWorkspace( dropRes );
                }
                else
                {
                    Core.WorkspaceManager.AddToActiveWorkspaceRecursive( dropRes );
                }
            }
        }

        public bool CanRenameResource( IResource res )
        {
            IBookmarkProfile profile = _bookmarkService.GetOwnerProfile( res );
            string error;
            return ( res.Type == "Weblink" || res.Type == "Folder" ) &&
                   ( profile == null || profile.CanRename( res, out error ) );
        }

        public bool ResourceRenamed( IResource res, string newName )
        {
            string oldName = res.GetStringProp( Core.Props.Name );
            if( oldName == null || newName == null || oldName == newName )
            {
                return false;
            }
            if ( newName.Length == 0 || newName == "New Folder" )
            {
                MessageBox.Show( Core.MainWindow,
                    "Please specify a name.", "Rename", MessageBoxButtons.OK, MessageBoxIcon.Stop );
                return false;
            }
            // check duplicates on the same level for some cases
            IBookmarkProfile profile = _bookmarkService.GetOwnerProfile( res );
            IResource parent = res.GetLinkProp( _propParent );
            if( parent != null && ( profile == _favoritesProfile || res.Type == "Folder" ) &&
                BookmarkService.HasSubNodeWithName( parent, newName ) )
            {
                MessageBox.Show( Core.MainWindow,
                    "The name is already used, please specify another", "Rename", MessageBoxButtons.OK, MessageBoxIcon.Stop );
                return false;
            }
            if( profile != null )
            {
                profile.Rename( res, newName );
            }
            new ResourceProxy( res ).SetPropAsync( Core.Props.Name, newName );
            return true;
        }

        public void ResourceNodeSelected( IResource res )
        {
            if( res.Type == "Weblink" )
            {
                Core.ResourceBrowser.DisplayResource( res, false );
            }
            else if( res.Type == "Folder" )
            {
                IResourceList favorites = res.GetLinksToLive( "Weblink", _propParent );
                Core.ResourceBrowser.DisplayResourceList( res, favorites, "Bookmarks in " + res.DisplayName, null );
            }
        }

        #endregion

        #region Remote Handling

        private static IResourceList FindOrCreateWeblinksByUrl( string url, string title )
        {
            IResourceList weblinks = Core.ResourceStore.FindResources( "Weblink", _propURL, url.Trim() );
            if( weblinks.Count > 0 )
            {
                return weblinks;
            }
            int id = Core.SettingStore.ReadInt( "Favorites", "CatAnnRoot", _bookmarkService.BookmarksRoot.Id );
            IResource annRoot = Core.ResourceStore.TryLoadResource( id ) ?? _bookmarkService.BookmarksRoot;
        	return BookmarkService.FindOrCreateBookmark( annRoot, title, url, true ).ToResourceList();
        }

        private delegate void RemoteAnnotateWeblinkDelegate( string url, string title );

        public static void RemoteAnnotateWeblink( string url, string title )
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.QueueJob("Remote Annotate Web Link", new RemoteAnnotateWeblinkDelegate( RemoteAnnotateWeblink ), url, title );
            }
            else
            {
                IResourceList weblinks = FindOrCreateWeblinksByUrl( url, title );
                if( weblinks.Count > 0 )
                {
                    Core.UserInterfaceAP.QueueJob("Activate Main Window",  new MethodInvoker( ( (Form) Core.MainWindow).Activate ) );
                    foreach( IResource weblink in weblinks )
                    {
                        Core.UserInterfaceAP.QueueJob("Edit Annotation",  new ResourceDelegate( RemoteAnnotateForm.EditAnnotation ), weblink );
                    }
                }
            }
        }

        public delegate BookmarkChange[] RemoteExportMozillaBookmarkChangesDelegate( string profilePath );

        private static readonly HashMap _profilePath2Name = new HashMap( 1 );

        private static string ProfilePath2Name( string profilePath )
        {
            string profileName = (string) _profilePath2Name[ profilePath ];
            if( profileName == null )
            {
                MozillaProfile profile = new MozillaProfile( profilePath );
                profileName = profile.Name ?? string.Empty;
                string lname = profileName.ToLower();
                if( lname.IndexOf( "mozilla" ) < 0 && lname.IndexOf( "firefox" ) < 0 && lname.IndexOf( "phoenix" ) < 0 )
                {
                    profile = new MozillaProfile( IOTools.GetFullName( IOTools.GetParent( profilePath ) ) );
                    profileName = profile.Name;
                    if( string.IsNullOrEmpty( profileName ) )
                    {
                        Trace.WriteLine( "ProfilePath2Name( " + profilePath + " ) : not found" );
                        return null;
                    }
                }
                _profilePath2Name[ profilePath ] = profileName;
            }
            Trace.WriteLine( "ProfilePath2Name( " + profilePath + " ) : " + profileName );
            return profileName;
        }

        public static BookmarkChange[] RemoteExportMozillaBookmarkChanges( string profilePath )
        {
            string profileName = ProfilePath2Name( profilePath );
            if( profileName == null )
            {
                return null;
            }
            return (BookmarkChange[]) Core.ResourceAP.RunUniqueJob("ParseChangesLog",
                new ParseChangesLogDelegate( ParseChangesLog ), profileName );
        }

        private delegate BookmarkChange[] ParseChangesLogDelegate( string profileName );

        private static BookmarkChange[] ParseChangesLog( string profileName )
        {
            IBookmarkService service =
                (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
            IResource profileRoot = service.GetProfileRoot(
                BookmarkService.NormalizeProfileName( "Mozilla/" + profileName ) );
            if( profileRoot != null )
            {
                Trace.WriteLine( "ParseChangesLog( " + profileName + " ) : profileRoot != null" );
                MozillaBookmarkProfile profile =
                    service.GetOwnerProfile( profileRoot ) as MozillaBookmarkProfile;
                if( profile != null )
                {
                    Trace.WriteLine( "ParseChangesLog( " + profileName + " ) : profile != null" );
                    profile.IsActive = true;
                    IStringList log = profileRoot.GetStringListProp( _propChangesLog );
                    if( log.Count > 0 )
                    {
                        Trace.WriteLine( "ParseChangesLog( " + profileName + " ) : log.Count > 0" );
                        BookmarkChange[] result = new BookmarkChange[ log.Count ];
                        for( int i = 0; i < result.Length; ++i )
                        {
                            string[] changeFields = log[ i ].Split( '\x01' );
                            result[ i ].type = Int32.Parse( changeFields[ 0 ] );
                            result[ i ].id = Int32.Parse( changeFields[ 1 ] );
                            result[ i ].rdfid = changeFields[ 2 ];
                            result[ i ].oldparent = changeFields[ 3 ];
                            result[ i ].oldparent_id = Int32.Parse( changeFields[ 4 ] );
                            result[ i ].parent = changeFields[ 5 ];
                            result[ i ].parent_id = Int32.Parse( changeFields[ 6 ] );
                            result[ i ].name = changeFields[ 7 ];
                            result[ i ].url = changeFields[ 8 ];
                        }
                        profileRoot.DeleteProp( _propChangesLog );
                        return result;
                    }
                }
            }
            return null;
        }

        public delegate void RemoteSetMozillaBookmarkIdDelegate( int idres, string rdfid );

        public void RemoteSetMozillaBookmarkId( int idres, string rdfid )
        {
            IResource res = Core.ResourceStore.TryLoadResource( idres );
            if( res != null )
            {
                ResourceProxy proxy = new ResourceProxy( res );
                proxy.SetPropAsync( _propBookmarkId, rdfid );
            }
        }

        public delegate void RemoteRefreshBookmarksDelegate( string profilePath );

        public void RemoteRefreshBookmarks( string profilePath )
        {
            string profileName = ProfilePath2Name( profilePath );
            if( profileName != null )
            {
                IBookmarkService service =
                    (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
                IResource profileRoot = service.GetProfileRoot(
                    BookmarkService.NormalizeProfileName( "Mozilla/" + profileName ) );
                if( profileRoot != null )
                {
                    MozillaBookmarkProfile profile =
                        service.GetOwnerProfile( profileRoot ) as MozillaBookmarkProfile;
                    if( profile != null )
                    {
                        profile.IsActive = true;
                        Core.ResourceAP.QueueJob(
                            "Refreshing bookmarks for " + profileName, new MethodInvoker( profile.StartImport ) );
                    }
                }
            }
        }

        #endregion

        #region implementation details

        private static void Core_StateChanged( object sender, EventArgs e )
        {
            if( Core.State == CoreState.Running )
            {
                StartImportProfiles();
                _bookmarkService.SynchronizeBookmarks();
            }
        }

        private static void RegisterMozillaProfiles( IEnumerable profiles )
        {
            foreach( MozillaProfile profile in profiles )
            {
                MozillaBookmarkProfile prf = new MozillaBookmarkProfile( profile.Name, _bookmarkService );
                _bookmarkService.RegisterProfile( prf );
                CookiesManager.RegisterCookieProvider( prf );
            }
        }

        private static void StartImportProfiles()
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.QueueJob( "Importing bookmark profiles", new MethodInvoker( StartImportProfiles ) );
            }
            else
            {
                foreach( IBookmarkProfile profile in _bookmarkService.Profiles )
                {
                    profile.StartImport();
                }
            }
        }

        internal static Icon LoadIconFromAssembly( string name )
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream( "Favorites.Icons." + name );
            return ( stream != null ) ? new Icon( stream ) : null;
        }

        private void RegisterTypes()
        {
            IResourceTypeCollection resTypes = Core.ResourceStore.ResourceTypes;
            resTypes.Register( "Weblink", "Web Bookmark", "Name", ResourceTypeFlags.NoIndex, this );
            resTypes[ "Weblink" ].DisplayName = "Web Bookmark";
            resTypes.Register( "Folder", "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            IPropTypeCollection propTypes = Core.ResourceStore.PropTypes;
            _propURL = propTypes.Register( "URL", PropDataType.String );
            _propParent = propTypes.Register( "Parent", PropDataType.Link, PropTypeFlags.DirectedLink );
            _propETag = propTypes.Register( "ETag", PropDataType.String, PropTypeFlags.Internal );
            _propLastUpdated = propTypes.Register( "LastUpdated", PropDataType.Date, PropTypeFlags.Internal );
            _propUpdateFreq = propTypes.Register( "UpdateFreq", PropDataType.Int, PropTypeFlags.Internal );
            _propIsUnread = propTypes.Register( "IsUnread", PropDataType.Bool );
            _propContent = propTypes.Register( "Content", PropDataType.Blob, PropTypeFlags.Internal );
            _propBookmarkId = propTypes.Register( "BookmarkId", PropDataType.String, PropTypeFlags.Internal );
            _propChangesLog = propTypes.Register( "ChangesLog", PropDataType.StringList, PropTypeFlags.Internal );
            _propFaviconUrl = propTypes.Register( "FaviconUrl", PropDataType.String, PropTypeFlags.Internal );
            _propInvisible = propTypes.Register( "Invisible", PropDataType.Bool, PropTypeFlags.Internal );
            if( propTypes.Exist( "Path" ) )
            {
                propTypes.Delete( propTypes[ "Path" ].Id );
            }
            if( propTypes.Exist( "IsIEFavoritesRoot" ) )
            {
                IResource ieRoot = Core.ResourceStore.FindUniqueResource( "Folder", "IsIEFavoritesRoot", true );
                if( ieRoot != null )
                {
                    ieRoot.SetProp( Core.Props.Name, ieRoot.DisplayName );
                    ieRoot.DisplayName = "";
                }
                propTypes.Delete( propTypes[ "IsIEFavoritesRoot" ].Id );
            }
            if( propTypes.Exist( "IsMozillaRoot" ) )
            {
                IResource mozillaRoot = Core.ResourceStore.FindUniqueResource( "Folder", "IsMozillaRoot", true );
                if( mozillaRoot != null )
                {
                    _bookmarkService.DeleteFolder( mozillaRoot );
                }
                propTypes.Delete( propTypes[ "IsMozillaRoot" ].Id );
            }
        }

        private void WeblinkOrFolderChanged( object sender, ResourcePropIndexEventArgs e )
        {
            IResource webLink = e.Resource;

            IPropertyChangeSet set = e.ChangeSet;
            int propLastModified = Core.ResourceStore.PropTypes[ "LastModified" ].Id;
            if( BookmarkService.DownloadMethod != 2 && webLink == _lastDisplayedWeblink &&
                ( ( set.IsPropertyChanged( propLastModified ) && webLink.HasProp( "Source" ) )
                || ( set.IsPropertyChanged( Core.Props.LastError ) && webLink.HasProp( Core.Props.LastError ) ) ) )
            {
                // if the displayed web link has changed, redisplay it
                IResourceBrowser browser = Core.ResourceBrowser;
                if( ( webLink == _favoritesTreePane.SelectedNode && Core.TabManager.CurrentTabId == "Web" ) ||
                    ( browser.SelectedResources.Count == 1 && webLink == browser.SelectedResources[ 0 ] ) )
                {
                    Core.UserInterfaceAP.QueueJobAt(DateTime.Now.AddSeconds( 1 ), "RedisplaySelectedResource", browser.RedisplaySelectedResource );
                }
            }

            string URL = webLink.GetPropText( _propURL );
            if( URL.Length > 0 )
            {
                if( set.IsPropertyChanged( _propLastUpdated ) || set.IsPropertyChanged( _propUpdateFreq ) )
                {
                    BookmarkService.QueueWeblink( webLink, URL, BookmarkService.BookmarkSynchronizationTime( webLink ) );
                }
                if( set.IsPropertyChanged( _propURL ) )
                {
                    BookmarkService.ImmediateQueueWeblink( webLink, URL );
                }
            }
            if( set.IsPropertyChanged( Core.PropIds.Name ) || set.IsPropertyChanged( _propURL ) )
            {
                IBookmarkProfile profile = _bookmarkService.GetOwnerProfile( webLink );
                string error;
                if( profile != null && profile.CanCreate( webLink, out error ) )
                {
                    profile.Create( webLink );
                }
            }
        }

        private static string DisplayWeblinkError( IResource res )
        {
            return res.GetPropText( Core.Props.LastError );
        }

        internal static int                         _propURL;
        internal static int                         _propParent;
        internal static int                         _propETag;
        internal static int                         _propLastUpdated;
        internal static int                         _propUpdateFreq;
        internal static int                         _propIsUnread;
        internal static int                         _propContent;
        internal static int                         _propBookmarkId;
        internal static int                         _propChangesLog;
        internal static int                         _propFaviconUrl;
        internal static int                         _propInvisible;
        internal static IResourceTreePane           _favoritesTreePane;
        internal static IEFavoritesBookmarkProfile  _favoritesProfile;
        internal static OperaBookmarkProfile        _operaProfile;
        internal static BookmarkService             _bookmarkService;
        private IResource                           _lastDisplayedWeblink;
        private IResourceList                       _favorites;
        private IResourceList                       _folders;
        private Icon                                _emptyWeblinkIcon;
        private Icon                                _errorWeblinkIcon;

    	private FavIconManager                      _favIconManager;

        #endregion
	}

    internal class WeblinkNodeDecorator : IResourceNodeDecorator
    {
        public event ResourceEventHandler DecorationChanged;

        public WeblinkNodeDecorator()
        {
            IResourceStore store = Core.ResourceStore;
            _unreadWeblinks = store.FindResourcesWithPropLive( null, "Source" );
            _unreadWeblinks = _unreadWeblinks.Minus( store.GetAllResourcesLive( "Weblink" ) );
            _unreadWeblinks = _unreadWeblinks.Intersect( store.FindResourcesWithPropLive( null, "IsUnread" ), true );
            _unreadWeblinks.ResourceAdded += FireDecorationChanged;
            _unreadWeblinks.ResourceDeleting += FireDecorationChanged;
        }

        public bool DecorateNode( IResource res, RichText nodeText )
        {
            if( res.Type == "Weblink" )
            {
                IResource source = res.GetLinkProp( "Source" );
                if( source != null && source.HasProp( Core.Props.IsUnread ) )
                {
                    nodeText.SetStyle( FontStyle.Bold, 0, nodeText.Text.Length );
                }
                return true;
            }
            return false;
        }

        public string DecorationKey
        {
            get { return UnreadNodeDecorator.Key; }
        }

        private void FireDecorationChanged( object sender, ResourceIndexEventArgs e )
        {
            if( DecorationChanged != null )
            {
                IResource source = e.Resource.GetLinkProp( "Source" );
                if( source != null && source.Type == "Weblink" )
                {
                    DecorationChanged( this, new ResourceEventArgs( source ) );
                }
            }
        }

        private readonly IResourceList _unreadWeblinks;
    }

    internal class BookmarkProfileFilter : IResourceNodeFilter
    {
        bool IResourceNodeFilter.AcceptNode( IResource res, int level )
        {
            if( res.HasProp( FavoritesPlugin._propInvisible ) )
            {
                return false;
            }
            IBookmarkService service =
                (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
            if( service != null )
            {
                IBookmarkProfile profile = service.GetOwnerProfile( res );
                if( profile is FakeRestrictProfile )
                {
                    return BookmarkService.SubNodes( "Folder", res ).Minus(
                        Core.ResourceStore.FindResourcesWithProp( null, FavoritesPlugin._propInvisible ) ).Count > 0;
                }
            }
            return true;
        }
    }
}
