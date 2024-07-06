// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.Charsets;
using JetBrains.Omea.Net;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.ResourceTools;
using Microsoft.Win32;

namespace JetBrains.Omea.Nntp
{
    [PluginDescriptionAttribute("NNTP Newsgroups", "JetBrains Inc.", "Support for Usenet news infrastructure - news servers connection, newsgroups subscribtions, news downloading.", PluginDescriptionFormat.PlainText, "Icons/NntpPluginIcon.png")]
    public class NntpPlugin : IPlugin, IResourceDisplayer, IStreamProvider,
                              IResourceUIHandler, IResourceDragDropHandler
    {
        #region IPlugin Members

        public void Register()
        {
            try
            {
                _plugin = this;
                Settings.LoadSettings();
                RegisterTypes();
            }
            catch( Exception e )
            {
                Core.ReportBackgroundException( e );
                Core.ActionManager.DisableXmlActionConfiguration( Assembly.GetExecutingAssembly() );
                return;
            }

            foreach( IResource server in Core.ResourceStore.GetAllResources( _newsServer ).ValidResources )
            {
                NewsFolders.AddToRoot( server );
            }

            IUIManager uiMgr = Core.UIManager;

            uiMgr.RegisterOptionsGroup( "Internet", "The Internet options enable you to control how [product name] works with several types of online content." );
            uiMgr.RegisterOptionsPane( "Internet", "Newsgroups", NntpOptionsPane.NntpOptionsPaneCreator,
                                       "The Newsgroups options enable you to control the downloading of newsgroup articles, " +
                                       "the default encoding and message format used for newsgroups, and the marking of downloaded messages." );

            Core.TabManager.RegisterResourceTypeTab( "News", "News",
                new[] { _newsArticle, _newsGroup, _newsServer, _newsFolder, _newsLocalArticle }, _propAttachment, 3 );

            Image img = Utils.TryGetEmbeddedResourceImageFromAssembly( Assembly.GetExecutingAssembly(), "NntpPlugin.Icons.Newsgroups24.png" );
            _newsgroupsTreePane = Core.LeftSidebar.RegisterResourceStructureTreePane( "Newsgroups", "News", "Newsgroups", img, _newsGroup );
            _newsgroupsTreePane.WorkspaceFilterTypes = new[] { _newsServer, _newsGroup, _newsFolder };
            _newsgroupsTreePane.ToolTipCallback = DisplayGroupOrServerError;
            Core.LeftSidebar.RegisterViewPaneShortcut( "Newsgroups", Keys.Control | Keys.Alt | Keys.N );

            CorrespondentCtrl correspondentPane = new CorrespondentCtrl();
            correspondentPane.IniSection = "NNTP";

            img = Utils.TryGetEmbeddedResourceImageFromAssembly( Assembly.GetExecutingAssembly(), "NntpPlugin.Icons.Correspondents24.png" );
            Core.LeftSidebar.RegisterViewPane( "Correspondents", "News", "Correspondents", img, correspondentPane );

            Core.ResourceTreeManager.SetResourceNodeSort( NewsFolders.Root, "NewsSortOrder VisibleOrder DisplayName" );
            uiMgr.RegisterResourceSelectPane( _newsGroup, typeof( NewgroupsSelectPane ) );

            uiMgr.RegisterResourceLocationLink( _newsArticle, _propTo, _newsGroup );
            uiMgr.RegisterResourceLocationLink( _newsGroup, Core.Props.Parent, _newsServer );
            uiMgr.RegisterResourceLocationLink( _newsFolder, 0, _newsFolder );
            uiMgr.RegisterDisplayInContextHandler( _newsGroup, new DisplayNewsgroupInContextHandler() );

            IWorkspaceManager workspaceMgr = Core.WorkspaceManager;
            workspaceMgr.RegisterWorkspaceType( _newsGroup, new[] { -_propTo }, WorkspaceResourceType.Container );
            workspaceMgr.RegisterWorkspaceFolderType( _newsServer, _newsGroup, new[] { -Core.Props.Parent } );
            workspaceMgr.RegisterWorkspaceFolderType( _newsFolder, _newsGroup, new[] { -Core.Props.Parent } );
            workspaceMgr.RegisterWorkspaceType( _newsArticle, new[] { -_propAttachment }, WorkspaceResourceType.None );
            workspaceMgr.RegisterWorkspaceSelectorFilter( _newsGroup, new NewsWorkspaceSelectorFilter() );

            IPluginLoader pluginLoader = Core.PluginLoader;
            IResourceTextProvider articleTextProvider = new ArticleTextProvider();
            pluginLoader.RegisterResourceTextProvider( _newsArticle, articleTextProvider );
            pluginLoader.RegisterResourceTextProvider( _newsLocalArticle, articleTextProvider );

            IResourceRenameHandler renameHandler = new NntpRenameHandler();
            pluginLoader.RegisterStreamProvider(_newsArticle, this);
            pluginLoader.RegisterStreamProvider( _newsLocalArticle, this );
            pluginLoader.RegisterResourceDisplayer( _newsArticle, this );
            pluginLoader.RegisterResourceDisplayer( _newsLocalArticle, this );
            pluginLoader.RegisterResourceUIHandler( _newsGroup, this );
            pluginLoader.RegisterResourceUIHandler( _newsFolder, this );
            pluginLoader.RegisterResourceUIHandler( _newsServer, this );
            pluginLoader.RegisterResourceRenameHandler( _newsGroup, renameHandler );
            pluginLoader.RegisterResourceRenameHandler( _newsFolder, renameHandler );
            pluginLoader.RegisterResourceRenameHandler( _newsServer, renameHandler );
			pluginLoader.RegisterResourceDragDropHandler( _newsGroup, this );
			pluginLoader.RegisterResourceDragDropHandler( _newsFolder, this );
			pluginLoader.RegisterResourceDragDropHandler( _newsServer, this );
			pluginLoader.RegisterResourceDragDropHandler( NewsFolders.Root.Type, this );

            pluginLoader.RegisterDefaultThreadingHandler( _newsArticle, Core.Props.Reply );
            pluginLoader.RegisterDefaultThreadingHandler( _newsLocalArticle, Core.Props.Reply );

            IResourceBrowser resourceBrowser = Core.ResourceBrowser;
            resourceBrowser.RegisterLinksPaneFilter( _newsArticle, new ItemRecipientsFilter() );
            resourceBrowser.RegisterLinksPaneFilter( _newsLocalArticle, new ItemRecipientsFilter() );
            resourceBrowser.RegisterLinksPaneFilter( _newsArticle, new NewsAttachmentFilter() );
            resourceBrowser.RegisterLinksPaneFilter( _newsLocalArticle, new NewsAttachmentFilter() );
            resourceBrowser.RegisterLinksPaneFilter( _newsArticle, new ArticleNewsgroupsFilter() );
            resourceBrowser.RegisterLinksPaneFilter( _newsLocalArticle, new ArticleNewsgroupsFilter() );

            resourceBrowser.ContentChanged += resourceBrowser_ContentChanged;

            IResourceList folders = Core.ResourceStore.GetAllResources( _newsFolder );
            foreach( IResource folder in folders )
            {
                if ( NewsFolders.IsDefaultFolder( folder ) )
                {
                    folder.SetProp( "VisibleInAllWorkspaces", true );
                }
            }

            IResourceIconManager iconManager = Core.ResourceIconManager;
            NewsgroupIconProvider iconProvider = new NewsgroupIconProvider();
            iconManager.RegisterPropTypeIcon( _propAttachment, LoadNewsIcon( "news_attachment.ico" ) );
            iconManager.RegisterResourceIconProvider( _newsGroup, iconProvider );
            iconManager.RegisterOverlayIconProvider( _newsGroup, iconProvider );
            iconManager.RegisterOverlayIconProvider( _newsArticle, new ArticleOverlayIconProvider() );

            pluginLoader.RegisterResourceDeleter( _newsArticle, new NewsArticleDeleter() );
            pluginLoader.RegisterResourceDeleter( _newsLocalArticle, new NewsArticleDeleter() );

            Core.ProtocolHandlerManager.RegisterProtocolHandler( "news", "newsgroup client",
                                                                 RemoteHandleNews, MakeDefaultHandler );
            Core.ProtocolHandlerManager.RegisterProtocolHandler( "snews", "newsgroup client", RemoteHandleSnews );

            //-----------------------------------------------------------------
            //  Register classes which [upgrade] and initialize plugin-dependent
            //  conditions, templates, notification and tray icon rules.
            //  Naturally, upgrade initializer must be called first.
            //-----------------------------------------------------------------
            Core.PluginLoader.RegisterViewsConstructor( new NewsUpgrade1ViewsConstructor() );
            Core.PluginLoader.RegisterViewsConstructor( new NewsViewsConstructor() );

            //-----------------------------------------------------------------
            //  Register Search Extensions to narrow the list of results using
            //  simple phrases in search queries: for restricting the resource
            //  type to news articles.
            //-----------------------------------------------------------------
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "news", _newsArticle );

            //  Register Link Id which serves as an anchor for tracing the events
            //  when new article is created and linked to its folder.
            Core.ExpirationRuleManager.RegisterResourceType( _propTo, _newsGroup, _newsArticle );

            Core.DisplayColumnManager.SetAlignTopLevelItems( _newsArticle, true );
            Core.DisplayColumnManager.SetAlignTopLevelItems( _newsLocalArticle, true );

            try
            {
                _newsProtocolsKey = Registry.LocalMachine.CreateSubKey(
                    "Software\\Clients\\News\\" + Core.ProductFullName + "\\Protocols" );
            }
            catch
            {
                _newsProtocolsKey = null;
            }

            Core.ResourceBrowser.SetDefaultViewSettings( "News", AutoPreviewMode.Off, true );
        }

        public void Startup()
        {
            RegisterDisplayColumns();

            MyPalStorage store = MyPalStorage.Storage;
            store.CachePredicate( store.FindResourcesWithPropLive( null, _propHasNoBody ) );
            string charset = Settings.Charset;
            if( String.IsNullOrEmpty( charset ) )
                Settings.Charset.Save( CharsetsEnum.GetDefaultBodyCharset().Name );

            _productType = ( Core.ProductFullName.EndsWith( "Reader" ) ) ? ProductType.Reader : ProductType.Pro;
            CheckGroups();

            //-----------------------------------------------------------------
            //  Decorators should be present here since some of them use live lists
            //  constructed by means e.g. FilterRegistry and local views constructor
            //  helper which might not be initialized on registering phase.
            //-----------------------------------------------------------------
            ((JetResourceTreePane)_newsgroupsTreePane).AddNodeDecorator( new NewsNodeDecorator() );
            ((JetResourceTreePane)_newsgroupsTreePane).AddNodeDecorator( new TotalCountDecorator( _newsGroup, _propTo, Core.Props.Parent ) );
            ((JetResourceTreePane)_newsgroupsTreePane).InsertNodeDecorator( new WatchedArticlesDecorator(), 0 );

            Core.StateChanged += Core_StateChanged;
        }

        public void Shutdown()
        {
            NntpConnectionPool.CloseAll();
            Settings.FirstStart.Save( false );
        }
        #endregion

        #region IResourceDisplayer Members

        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            if( resourceType == _newsArticle || resourceType == _newsLocalArticle )
            {
                if( _previewPane == null )
                {
                    _previewPane = new ArticlePreviewPane();
                }
                return _previewPane;
            }
            return null;
        }

        #endregion

        #region IStreamProvider methods

        public Stream GetResourceStream( IResource res )
        {
            return res.GetBlobProp( _propContent );
        }

        #endregion

        #region IResourceUIHandler Members

        public void ResourceNodeSelected( IResource res )
        {
            if( res.Type == _newsGroup )
            {
                NntpClientHelper.DownloadHeadersFromGroup( res );
            }
            if( res.Type == _newsFolder )
            {
                IResourceList groups = new NewsTreeNode( res ).Groups;
                foreach( IResource group in groups )
                {
                    NntpClientHelper.DownloadHeadersFromGroup( group );
                }
            }
            IResourceList articles = CollectArticles( res, true );
            string caption;
            if( !res.HasProp( Core.Props.DisplayUnread ) )
            {
                caption = "Articles in " + res.DisplayName;
            }
            else
            {
                caption = "Unread Articles in " + res.DisplayName;
                articles = articles.Intersect(
                    Core.ResourceStore.FindResourcesWithProp( SelectionType.LiveSnapshot, null, Core.Props.IsUnread ), true );
            }
            IResource lastSelectedArticle = null;
            int lastSelectedID = res.GetIntProp( _propLastSelectedArticle );
            if( lastSelectedID > 0 )
            {
                lastSelectedArticle = Core.ResourceStore.TryLoadResource( lastSelectedID );
            }

            if( !res.HasProp( Core.Props.DisplayThreaded ) || NewsFolders.IsDefaultFolder( res ) )
            {
                Core.ResourceBrowser.DisplayResourceList(
                    res, articles, caption, null, lastSelectedArticle );
            }
            else
            {
                Core.ResourceBrowser.DisplayThreadedResourceList(
                    res, articles, caption, "Date From",
                    Core.Props.Reply, null, lastSelectedArticle );
            }
        }

        public bool CanRenameResource( IResource res )
        {
            return false;
        }

        public bool ResourceRenamed( IResource res, string newName )
        {
            return false;
        }

        public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
        {
			return;
        }

        public bool CanDropResources( IResource targetResource, IResourceList dragResources )
        {
			return false;
        }

        #endregion

        #region news: protocol handling

        private delegate void StringResourceDelegate( string str, IResource res );

        public void RemoteHandleNews( string url )
        {
            Core.UIManager.QueueUIJob( new HandleNewsURLDelegate( HandleNewsURL ), url, false );
        }

        public void RemoteHandleSnews( string url )
        {
            Core.UIManager.QueueUIJob( new HandleNewsURLDelegate( HandleNewsURL ), url, true );
        }

        private static void MakeDefaultHandler()
        {
            try
            {
                if( _newsProtocolsKey != null )
                {
                    RegistryKey key;
                    using( key = Registry.LocalMachine.OpenSubKey( "Software\\Clients\\News", true ) )
                    {
                        key.SetValue( null, Core.ProductFullName );
                    }
                    using( key = Registry.LocalMachine.OpenSubKey( "Software\\Clients\\News\\" + Core.ProductFullName, true ) )
                    {
                        key.SetValue( null, Core.ProductFullName );
                    }
                    ArrayList keys = new ArrayList( 3 );
                    key =_newsProtocolsKey.CreateSubKey( "news" );
                    key.SetValue( null, "URL:News Protocol" );
                    keys.Add( key );
                    key = _newsProtocolsKey.CreateSubKey( "nntp" );
                    key.SetValue( null, "URL:Nntp Protocol" );
                    keys.Add( key );
                    key =_newsProtocolsKey.CreateSubKey( "snews" );
                    key.SetValue( null, "URL:Snews Protocol" );
                    keys.Add( key );
                    for( int i = 0; i < keys.Count; ++i )
                    {
                        using( key = (RegistryKey) keys[ i ] )
                        {
                            key.SetValue( "EditFlags", 2 );
                            key.SetValue( "URL Protocol", "" );
                            RegistryKey subKey = key.CreateSubKey( "shell\\open\\command" );
                            if( subKey != null )
                            {
                                subKey.SetValue( null, '"' + Application.ExecutablePath + "\" -openurl \"%1\"" );
                                subKey.Close();
                            }
                            subKey = key.CreateSubKey( "DefaultIcon" );
                            if( subKey != null )
                            {
                                subKey.SetValue( null, Application.ExecutablePath + ",1" );
                                subKey.Close();
                            }
                        }
                    }
                }
            }
            catch {}
        }

        private delegate void HandleNewsURLDelegate( string url, bool secure );

        private static void HandleNewsURL( string url, bool secure )
        {
            // unescape url
            int i;
            while( ( i = url.IndexOf( '%' ) ) >= 0 )
            {
                int j = i;
                char c = Uri.HexUnescape( url, ref j );
                url = url.Remove( i, j - i ).Insert( i, new String( c, 1 ) );
            }

            ((Form) Core.MainWindow).Activate();
            if( url.StartsWith( "//" ) )
            {
                url = url.Substring( 2 );
                int slash = url.IndexOf( '/' );
                if( slash < 0 )
                {
                    HandleServer( url, true, secure );
                }
                else
                {
                    string serverName = url.Substring( 0, slash );
                    if( slash >= url.Length - 1 )
                    {
                        HandleServer( serverName, false, secure );
                    }
                    else
                    {
                        string rest = url.Substring( slash + 1 );
                        if( rest.IndexOf( "@" ) >= 0 && TryHandleArticle( ref rest ) )
                        {
                            return;
                        }
                        IResource serverRes = HandleServer( serverName, false, secure );
                        if( rest.IndexOf( "@" ) >= 0 )
                        {
                            HandleArticle( rest, serverRes );
                        }
                        else
                        {
                            HandleGroup( rest, serverRes );
                        }
                    }
                }
            }
            else
            {
                if( url.IndexOf( '@' ) >= 0 )
                {
                    HandleArticle( url, null );
                }
                else
                {
                    HandleGroup( url, null );
                }
            }
        }

        private static IResource HandleServer( string name, bool navigate, bool secure )
        {
            int port = secure ? 563 : 119;
            int portSplitter = name.IndexOf( ':' );
            if( portSplitter >= 0 )
            {
                try
                {
                    port = Int32.Parse( name.Substring( portSplitter + 1 ) );
                }
                catch {}
                name = name.Substring( 0, portSplitter );
            }
            IResource server = null;
            IResourceList servers = Core.ResourceStore.FindResources( _newsServer, Core.Props.Name, name );
            if( servers.Count > 0 )
            {
                servers.Sort( new[] { ResourceProps.Id }, false );
                server = servers[ 0 ];
            }
            else
            {
                EditServerForm form = EditServerForm.CreateNewServerPropertiesForm( name, port );
                if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
                {
                    server = form.Servers[0];
                    NntpClientHelper.DeliverNewsFromServer( server );
                    navigate = true;
                }
            }
            if( server != null && navigate )
            {
                HandleResourceSelection( server );
            }
            return server;
        }

        private static IResource HandleGroup( string url, IResource server )
        {
            IResourceList groups = Core.ResourceStore.FindResources( _newsGroup, Core.Props.Name, url );
            IResource group = null;
            if( groups.Count > 0 )
            {
                if( server == null )
                {
                    groups.Sort( new[] { ResourceProps.Id }, false );
                    group = groups[ 0 ];
                }
                else
                {
                    foreach( IResource res in groups )
                    {
                        if( new NewsgroupResource( res ).Server == server )
                        {
                            group = res;
                            break;
                        }
                    }
                }
            }
            else
            {
                IResourceList servers = Core.ResourceStore.FindResources( _newsServer, _propNewsgroupList, url );
                servers.Sort( "LastUpdated", false );
                if( servers.Count > 0 )
                {
                    Core.ResourceAP.RunUniqueJob( new Subscribe2GroupDelegate( Subscribe2Group ), url, servers[ 0 ] );
                    groups = Core.ResourceStore.FindResources( _newsGroup, Core.Props.Name, url );
                    group = groups[ 0 ];
                }
            }
            if( group != null )
            {
                HandleResourceSelection( group );
            }
            else
            {
                if( server == null )
                {
                    if( Core.State == CoreState.Running )
                    {
                        MessageBox.Show( Core.MainWindow, "No such group found: " + url,
                            "Article not found", MessageBoxButtons.OK, MessageBoxIcon.Information );
                    }
                }
                else
                {
                    NntpConnection connection =
                        NntpConnectionPool.GetConnection( server, "protocol handler connection" );
                    NntpDownloadGroupsUnit downloadGroups = new NntpDownloadGroupsUnit( server, false, JobPriority.Immediate );
                    downloadGroups.Finished += downloadGroups_Finished;
                    _lastNavigatedGroup = url;
                    connection.StartUnit( 0, downloadGroups );
                }
            }
            return group;
        }

        private static void HandleGroupMisc()
        {
            HandleGroup( _lastNavigatedGroup, null );
        }

        private static void downloadGroups_Finished( AsciiProtocolUnit unit )
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( HandleGroupMisc ) );
        }

        private static void HandleResourceSelection( IResource resource )
        {
            ITabManager tabManager = Core.TabManager;
            if( tabManager.CurrentTabId != "News" )
            {
                tabManager.SelectResourceTypeTab( _newsGroup );
            }
            _newsgroupsTreePane.SelectResource( resource );
        }

        private static bool TryHandleArticle( ref string articleId )
        {
            if( !articleId.StartsWith( "<" ) )
            {
                articleId = '<' + articleId + '>';
            }
            articleId = ParseTools.EscapeCaseSensitiveString( articleId );
            IResource article = Core.ResourceStore.FindUniqueResource( _newsArticle, _propArticleId, articleId );
            if( article != null )
            {
                Core.UIManager.DisplayResourceInContext( article );
                return true;
            }
            return false;
        }

        private static void HandleArticle( string articleId, IResource server )
        {
            if( !TryHandleArticle( ref articleId ) )
            {
                if( server == null )
                {
                    if( Core.State == CoreState.Running )
                    {
                        MessageBox.Show( Core.MainWindow, "No such article found: " +
                            ParseTools.UnescapeCaseSensitiveString( articleId ),
                            "Article not found", MessageBoxButtons.OK, MessageBoxIcon.Information );
                    }
                }
                else
                {
                    NntpConnection connection =
                        NntpConnectionPool.GetConnection( server, "protocol handler connection" );
                    IResource article = Core.ResourceStore.NewResourceTransient( _newsArticle );
                    article.SetProp( _propArticleId, articleId );
                    article.AddLink( _propTo, server );
                    NntpDownloadArticleUnit downloadUnit =
                        new NntpDownloadArticleUnit( article, null, JobPriority.Immediate, false );
                    downloadUnit.Finished += downloadUnit_Finished;
                    connection.StartUnit( 0, downloadUnit );
                }
            }
        }

        private static void downloadUnit_Finished( AsciiProtocolUnit unit )
        {
            NntpDownloadArticleUnit downloadUnit = (NntpDownloadArticleUnit) unit;
            Core.UIManager.QueueUIJob(
                new StringResourceDelegate( HandleArticle ),
                downloadUnit.Article.GetPropText( _propArticleId ), null );
        }

        #endregion

        #region IResourceDragDropHandler implementation

        /// <summary>
        /// Called to supply data in additional formats when the specified resources are being dragged.
        /// </summary>
        /// <param name="dragResources">The dragged resources.</param>
        /// <param name="dataObject">The drag data object.</param>
        public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
        {
            if(!dataObject.GetDataPresent( typeof(string) ))
            {
                StringBuilder sb = StringBuilderPool.Alloc();
                try
                {
                    foreach( IResource resource in dragResources )
                    {
                        if(sb.Length != 0)
                            sb.Append( ", " );
                        string text = resource.DisplayName;
                        if(text.IndexOf( ' ' )>  0)
                        {
                            sb.Append( "\"" );
                            sb.Append( text );
                            sb.Append( "\"" );
                        }
                        else
                            sb.Append( text );
                    }
                    dataObject.SetData( sb.ToString(  ) );
                }
                finally
                {
                    StringBuilderPool.Dispose( sb );
                }
            }
        }

        /// <summary>
        /// Called to return the drop effect when the specified data object is dragged over the
        /// specified resource.
        /// </summary>
        /// <param name="targetResource">The resource over which the drag happens.</param>
        /// <param name="data">The <see cref="IDataObject"/> containing the dragged data.</param>
        /// <param name="allowedEffect">The drag-and-drop operations which are allowed by the
        /// originator (or source) of the drag event.</param>
        /// <param name="keyState">The current state of the SHIFT, CTRL, and ALT keys,
        /// as well as the state of the mouse buttons.</param>
        /// <returns>The target drop effect.</returns>
        public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            if( data.GetDataPresent( typeof(IResourceList) ) ) // Dragging resources over
            {
                // The resources we're dragging
                IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );
                IResource root = NewsFolders.Root;

                // Restrict the allowed target res-types
                if( targetResource.Type == _newsFolder || targetResource.Type == _newsServer )
                {
                    // Restrict dragged res-types
                    string[] types = dragResources.GetAllTypes();
                    if( types.Length >= 3 )
                        return DragDropEffects.None;
                    foreach( string type in types )
                    {
                        if( type != _newsFolder && type != _newsGroup )
                            return DragDropEffects.None;
                    }

                    // Get the news-server of the target resource
                    IResource targetServer = targetResource;
                    while( targetServer != null && targetServer.Type != _newsServer )
                    {
                        if( dragResources.IndexOf( targetServer ) >= 0 )
                            return DragDropEffects.None;
                        targetServer = new NewsTreeNode( targetServer ).Parent;
                    }
                    // If there's no target server, drop's prohibited (eg Drafts)
                    // If there is one, check that all the dragged things belong to it (dragging to another server's not allowed)
                    if( targetServer != null )
                    {
                        bool result = true;
                        foreach( IResource dragResource in dragResources )
                        {
                            IResource server = dragResource;
                            while( server != null && server.Type != _newsServer )
                            {
                                server = new NewsTreeNode( server ).Parent;
                            }
                            if( server == null || targetServer != server )
                            {
                                result = false;
                                break;
                            }
                        }
                        return result ? DragDropEffects.Move : DragDropEffects.None;
                    }
                }
                else
                if( targetResource == root )
                {
                    // Dragging into empty space / tree root: allow only those that are already direct children of the root
                    bool bAllUnderRoot = true;
                    foreach( IResource res in dragResources )
                    {
                        IResourceList parents = res.GetLinksFrom( root.Type, Core.Props.Parent );
                        if(( parents.Count != 1) || ( parents[ 0 ] != root ) )
                        {
                            bAllUnderRoot = false;
                            break;
                        }
                    }
                    return bAllUnderRoot ? DragDropEffects.Move : DragDropEffects.None;
                }
            }
            return DragDropEffects.None;
        }

        private delegate void NewsResourcesDroppedDelegate( IResource targetResource, IResourceList droppedResources );

        /// <summary>
        /// Called to handle the drop of the specified data object on the specified resource.
        /// </summary>
        /// <param name="targetResource">The drop target resource.</param>
        /// <param name="data">The <see cref="IDataObject"/> containing the dragged data.</param>
        /// <param name="allowedEffect">The drag-and-drop operations which are allowed by the
        /// originator (or source) of the drag event.</param>
        /// <param name="keyState">The current state of the SHIFT, CTRL, and ALT keys,
        /// as well as the state of the mouse buttons.</param>
        public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            if(targetResource == NewsFolders.Root)
                return;
            if(data.GetDataPresent( typeof(IResourceList) ))
            {
                // The resources we're dragging
                IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );

                Core.ResourceAP.QueueJob( JobPriority.Immediate, "NNTP Article Dropped",
                    new NewsResourcesDroppedDelegate( ResourcesDroppedImpl ), targetResource, dragResources );
            }
        }

        private static void ResourcesDroppedImpl( IResource targetResource, IResourceList droppedResources )
        {
            foreach( IResource dropped in droppedResources )
            {
                new NewsTreeNode( dropped ).Parent = targetResource;
            }
        }

        #endregion

        #region implementation details

        public static bool IsNntpType( string type )
        {
            return type == _newsArticle || type == _newsLocalArticle;
        }

        private void RegisterTypes()
        {
            IResourceStore store = Core.ResourceStore;
            IResourceTypeCollection resTypes = store.ResourceTypes;
            resTypes.Register( _newsServer, "Name",
                ResourceTypeFlags.ResourceContainer | ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );
            resTypes.Register( _newsGroup, "Newsgroup", "Name", ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal, this );
            resTypes.Register( _newsArticle, "News Article", "Subject", ResourceTypeFlags.CanBeUnread, this );
            resTypes.Register( _newsLocalArticle, "Local News Article", "Subject", ResourceTypeFlags.Normal, this );
            resTypes[ _newsLocalArticle ].Flags = ResourceTypeFlags.Normal;
            resTypes.Register( _newsFolder, "News Folder", "Name",
                ResourceTypeFlags.ResourceContainer | ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal, this );

            IPropTypeCollection propTypes = store.PropTypes;
            _propPort = propTypes.Register( "Port", PropDataType.Int, PropTypeFlags.Internal );
            _propLastUpdated = propTypes.Register( "LastUpdated", PropDataType.Date, PropTypeFlags.Internal );

            _propTo = propTypes.Register( "Newsgroups", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.CountUnread );
            propTypes.RegisterDisplayName( _propTo, "Newsgroups", "Articles" );
            _propRawSubject = propTypes.Register( "RawSubject", PropDataType.LongString, PropTypeFlags.Internal );
            _propRawFrom = propTypes.Register( "RawFrom", PropDataType.LongString, PropTypeFlags.Internal );
            _propHtmlContent = propTypes.Register( "HtmlContent", PropDataType.Blob, PropTypeFlags.Internal );
            _propHasNoBody = propTypes.Register( "HasNoBody", PropDataType.Bool, PropTypeFlags.Internal );
            _propArticleId = propTypes.Register( "ArticleId", PropDataType.String, PropTypeFlags.Internal );
            _propReferenceId = propTypes.Register( "ReferenceId", PropDataType.String, PropTypeFlags.Internal );
            _propReply = propTypes.Register( "Reply", PropDataType.Link, PropTypeFlags.DirectedLink );
            propTypes.RegisterDisplayName( _propReply, "Reply To", "Replies" );
            _propIsUnread = propTypes.Register( "IsUnread", PropDataType.Bool );
            _propAttachment = propTypes.Register( "NewsAttachment", PropDataType.Link, PropTypeFlags.SourceLink | PropTypeFlags.DirectedLink, this );
            propTypes.RegisterDisplayName( _propAttachment, "News Article", "News Attachment" );
            _propEmbeddedContent = propTypes.Register( "EmbeddedContent",
                PropDataType.Link, PropTypeFlags.SourceLink | PropTypeFlags.DirectedLink | PropTypeFlags.Internal );
            _propUsername = propTypes.Register( "Username", PropDataType.String, PropTypeFlags.Internal );
            _propPassword = propTypes.Register( "Password", PropDataType.String, PropTypeFlags.Internal );
            _propSsl3Enabled = propTypes.Register( "Ssl3Enabled", PropDataType.Bool, PropTypeFlags.Internal );
            _propUserDisplayName = propTypes.Register( "UserDisplayName", PropDataType.String, PropTypeFlags.Internal );
            _propEmailAddress = propTypes.Register( "EmailAddress", PropDataType.String );
            _propNewsgroupList = propTypes.Register( "NewsgroupList", PropDataType.StringList, PropTypeFlags.Internal );

            bool need2UpdateSubscribed = !propTypes.Exist( "SubscribedNewsgroupList" );

            _propSubscribedNewsgroupList = propTypes.Register( "SubscribedNewsgroupList", PropDataType.StringList, PropTypeFlags.Internal );
            _propNewNewsgroupList = propTypes.Register( "NewNewsgroupList", PropDataType.StringList, PropTypeFlags.Internal );
            if( propTypes.Exist( "NewsgroupsHeader" ) )
            {
                propTypes.Delete( propTypes[ "NewsgroupsHeader" ].Id );
            }
            _propLastSelectedArticle = propTypes.Register( "LastSelectedArticle", PropDataType.Int, PropTypeFlags.Internal );
            _propInlineAttachment = propTypes.Register( "InlineAttachment", PropDataType.Bool, PropTypeFlags.Internal );
            _propDeletedList = propTypes.Register( "DeletedList", PropDataType.StringList, PropTypeFlags.Internal );
            _propDirectory = propTypes.Register( "Directory", PropDataType.String );
            CommonProps.Register();
            _propNntpText = propTypes.Register( "NntpText", PropDataType.LongString, PropTypeFlags.Internal );
            _propContent = propTypes.Register( "Content", PropDataType.Blob, PropTypeFlags.Internal );
            _propIsIgnoredThread = propTypes.Register( "IsIgnoredThread", PropDataType.Bool, PropTypeFlags.Internal );
            _propIsSelfThread = propTypes.Register( "IsSelfThread", PropDataType.Bool, PropTypeFlags.Internal );
            _propThreadVisibilityToggleDate = propTypes.Register( "ThreadVisibilityToggleDate", PropDataType.Date, PropTypeFlags.Internal );
            _propArticleHeaders = propTypes.Register( "MessageHeaders", PropDataType.Blob, PropTypeFlags.Internal );
            _propFollowupTo = propTypes.Register( "Followup-To", PropDataType.LongString, PropTypeFlags.Internal );
            _propNoMoreHeaders = propTypes.Register( "NoMoreHeaders", PropDataType.Bool, PropTypeFlags.Internal );
            _propFirstArticle = propTypes.Register( "FirstArticle", PropDataType.Int, PropTypeFlags.Internal );
            _propLastArticle = propTypes.Register( "LastArticle", PropDataType.Int, PropTypeFlags.Internal );
            _propArticleNumbers = propTypes.Register( "ArticleNumbers", PropDataType.LongString, PropTypeFlags.Internal );
            _propLastArticleDate = propTypes.Register( "ThreadLastArticleDate", PropDataType.Date, PropTypeFlags.Normal, this );
            propTypes.RegisterDisplayName( _propLastArticleDate, "Thread Update" );

            _propAbbreviateLevel = propTypes.Register( "AbbreviateLevel", PropDataType.Int, PropTypeFlags.Internal );
            if( propTypes.Exist( "DeliverOnStartup" ) && propTypes[ "DeliverOnStartup" ].DataType == PropDataType.Bool )
            {
                propTypes.Delete( propTypes[ "DeliverOnStartup" ].Id );
            }
            _propCountToDownloadAtTime = propTypes.Register( "CountToDownloadAtTime", PropDataType.Int, PropTypeFlags.Internal );
            _propDeliverOnStartup = propTypes.Register( "DeliverOnStartup", PropDataType.Int, PropTypeFlags.Internal );
            _propDeliverFreq = propTypes.Register( "DeliverFreq", PropDataType.Int, PropTypeFlags.Internal );
            _propMarkFromMeAsRead = propTypes.Register( "MarkFromMeAsRead", PropDataType.Int, PropTypeFlags.Internal );
            if( propTypes.Exist( "DownloadBodiesOnDeliver" ) && propTypes[ "DownloadBodiesOnDeliver" ].DataType == PropDataType.Bool )
            {
                propTypes.Delete( propTypes[ "DownloadBodiesOnDeliver" ].Id );
            }
            _propDownloadBodiesOnDeliver = propTypes.Register( "DownloadBodiesOnDeliver", PropDataType.Int, PropTypeFlags.Internal );
            _propDownloadBodyOnSelection = propTypes.Register( "DownloadBodyOnSelection", PropDataType.Int, PropTypeFlags.Internal );
            if( propTypes.Exist( "ForceDownload" ) )
            {
                propTypes.Delete( propTypes[ "ForceDownload" ].Id );
            }
            _propMailFormat = propTypes.Register( "MailFormat", PropDataType.LongString, PropTypeFlags.Internal );
            _propMIMETextEncoding = propTypes.Register( "MIMETextEncoding", PropDataType.LongString, PropTypeFlags.Internal );
            if( propTypes.Exist( "Allow8BitHeaders" ) )
            {
                propTypes.Delete( propTypes[ "Allow8BitHeaders" ].Id );
            }
            _propPutInOutbox = propTypes.Register( "PutInOutbox", PropDataType.Bool, PropTypeFlags.Internal );
            _propOverrideSigSettings = propTypes.Register( "OverrideSigSettings", PropDataType.Bool, PropTypeFlags.Internal );
            _propUseSignature = propTypes.Register( "UseSignature", PropDataType.Int, PropTypeFlags.Internal );
            _propMailSignature = propTypes.Register( "MailSignature", PropDataType.LongString, PropTypeFlags.Internal );
            _propReplySignaturePosition = propTypes.Register( "ReplySignaturePosition", PropDataType.Int, PropTypeFlags.Internal );
            _propNewsSortOrder = propTypes.Register( "NewsSortOrder", PropDataType.Int, PropTypeFlags.Internal );
            _propMarkReadOnExit = propTypes.Register( "MarkReadOnExit", PropDataType.Bool, PropTypeFlags.Internal );
            _propMarkReadOnLeave = propTypes.Register( "MarkReadOnLeave", PropDataType.Bool, PropTypeFlags.Internal );

            store.RegisterLinkRestriction( _newsArticle, Core.ContactManager.Props.LinkFrom, "Contact", 0, 1 );
            store.RegisterLinkRestriction( _newsLocalArticle, Core.ContactManager.Props.LinkFrom, "Contact", 0, 1 );
            store.RegisterLinkRestriction( _newsGroup, _propTo, _newsArticle, 0, Int32.MaxValue );
            store.RegisterUniqueRestriction( _newsArticle, _propArticleId );

            RemoveHasBodyProperty();
            if( need2UpdateSubscribed )
            {
                UpdateSubscribedGroups();
            }
            MoveDeletedListFromGroupsToServer();
            UpdateDisplayThreaded();
            UpdateNewsSortOrder();
            UpdateRepliesToMyPostsLogic();
            UpdateArticleHeaders();
            UpdateArticleHtmlBodies();
        }

        private static void RegisterDisplayColumns()
        {
            ImageListColumn attachmentColumn = new ImageListColumn( -_propAttachment );
            attachmentColumn.SetHeaderIcon( LoadNewsIcon( "AttachmentHeader.ico" ) );
            attachmentColumn.SetAnyValueIcon( LoadNewsIcon( "AttachmentColumn.ico" ) );
            Core.DisplayColumnManager.RegisterCustomColumn( -_propAttachment, attachmentColumn );
        }

        private static void RemoveHasBodyProperty()
        {
            IResourceStore store = Core.ResourceStore;
            if( store.PropTypes.Exist( "HasBody" ) )
            {
                IResourceList emptyArticles = store.GetAllResources( _newsArticle ).Minus( store.FindResourcesWithProp( null, "HasBody" ) );
                foreach( IResource artcile in emptyArticles )
                {
                    artcile.SetProp( _propHasNoBody, true );
                }
                store.PropTypes.Delete( store.PropTypes[ "HasBody" ].Id );
            }
        }

        private static void UpdateSubscribedGroups()
        {
            IResourceStore store = Core.ResourceStore;

            if( store.PropTypes.Exist( "RfcViolatingServer" ) )
            {
                store.PropTypes.Delete( store.PropTypes[ "RfcViolatingServer" ].Id );
            }
            if( store.PropTypes.Exist( "XoverGroup" ) )
            {
                store.PropTypes.Delete( store.PropTypes[ "XoverGroup" ].Id );
            }
            if( store.PropTypes.Exist( "ArticleNumber" ) )
            {
                store.PropTypes.Delete( store.PropTypes[ "ArticleNumber" ].Id );
            }

            IResourceList groups;
            IResourceList servers;
            groups = store.GetAllResources( _newsGroup );
            foreach( IResource group in groups )
            {
                servers = group.GetLinksFrom( _newsServer, Core.Props.Parent );
                if( servers.Count == 0 )
                {
                    UnsubscribeAction.Unsubscribe( group, true );
                }
                else
                {
                    string groupName = group.GetPropText( Core.Props.Name );
                    new NewsTreeNode( group ).Parent = servers[ 0 ];
                    IResourceList articles = group.GetLinksTo( null, _propTo );
                    for( int i = 1; i < servers.Count; ++i )
                    {
                        IResource newGroup = Core.ResourceStore.BeginNewResource( _newsGroup );
                        newGroup.SetProp( Core.Props.Name, groupName );
                        new NewsTreeNode( newGroup ).Parent = servers[ i ];
                        newGroup.EndUpdate();
                        foreach( IResource article in articles )
                        {
                            article.AddLink( _propTo, newGroup );
                        }
                    }
                }
            }
            servers = store.GetAllResources( _newsServer );
            foreach( IResource server in servers )
            {
                IStringList subscriptions = server.GetStringListProp( _propSubscribedNewsgroupList );
                subscriptions.Clear();
                groups = new ServerResource( server ).Groups;
                foreach( IResource group in groups )
                {
                    string groupName = group.GetPropText( Core.Props.Name );
                    if( subscriptions.IndexOf( groupName ) < 0 )
                    {
                        subscriptions.Add( groupName );
                    }
                }
                subscriptions.Dispose();
            }
        }

        private static void MoveDeletedListFromGroupsToServer()
        {
            IResourceList groups = Core.ResourceStore.FindResourcesWithProp( _newsGroup, _propDeletedList );
            if( groups.Count > 0 )
            {
                foreach( IResource group in groups )
                {
                    IResource server = new NewsgroupResource( group ).Server;
                    if( server != null )
                    {
                        IStringList serverIds = server.GetStringListProp( _propDeletedList );
                        IStringList groupIds = group.GetStringListProp( _propDeletedList );
                        foreach( string id in groupIds )
                        {
                            if( serverIds.IndexOf( id ) < 0 )
                            {
                                serverIds.Add( id );
                            }
                        }
                        group.DeleteProp( _propDeletedList );
                        serverIds.Dispose();
                    }
                }
            }
        }

        private static void UpdateDisplayThreaded()
        {
            IResourceStore store = Core.ResourceStore;
            IPropTypeCollection types = store.PropTypes;
            if( types.Exist( "DisplayPlainGroup" ) )
            {
                IResourceList threadedResources =
                    store.GetAllResources( new[] { _newsGroup, _newsFolder, _newsServer } ).Minus(
                    store.FindResourcesWithProp( null, "DisplayPlainGroup" ) );
                foreach( IResource threaded in threadedResources )
                {
                    threaded.SetProp( Core.Props.DisplayThreaded, true );
                }
                types.Delete( types[ "DisplayPlainGroup" ].Id );
            }
        }

        private static void UpdateNewsSortOrder()
        {
            if( !ObjectStore.ReadBool( "NNTP", "UpdateNewsSortOrder3", false ) )
            {
                foreach( IResource folder in Core.ResourceStore.GetAllResources( _newsFolder ) )
                {
                    if( NewsFolders.IsDefaultFolder( folder ) )
                    {
                        folder.SetProp( _propNewsSortOrder, Int32.MaxValue );
                    }
                    else
                    {
                        folder.DeleteProp( _propNewsSortOrder );
                    }
                }

                foreach( IResource server in Core.ResourceStore.GetAllResources( _newsServer ) )
                {
                    ServerResource serverResource = new ServerResource( server );
                    foreach( IResource group in serverResource.Groups )
                    {
                        if( new NewsgroupResource( group ).IsSubscribed )
                        {
                            group.SetProp( _propNewsSortOrder, 0 );
                        }
                        else
                        {
                            group.SetProp( _propNewsSortOrder, 1 );
                        }
                    }
                }
                ObjectStore.WriteBool( "NNTP", "UpdateNewsSortOrder3", true );
            }
        }

        private static void UpdateRepliesToMyPostsLogic()
        {
            if( !ObjectStore.ReadBool( "NNTP", "UpdateRepliesToMyPosts", false ) )
            {
                if( Core.ProgressWindow != null )
                {
                    Core.ProgressWindow.UpdateProgress( 0, "Upgrading News Articles database", null );
                }

                IntHashSet CollectedIds = new IntHashSet();
                IntHashSet resultSet = new IntHashSet();

                IResourceList allMyHeads =
                    Core.ContactManager.MySelf.Resource.GetLinksOfType( _newsArticle, "From" ).Intersect(
                    Core.ResourceStore.FindResourcesWithProp( _newsArticle, -Core.Props.Reply ), true );

                foreach( IResource res in allMyHeads )
                    CollectedIds.Add( res.Id );

                ProcessResources( resultSet, CollectedIds );

                if( Core.ProgressWindow != null )
                {
                    Core.ProgressWindow.UpdateProgress( 0, "", null );
                }
                ObjectStore.WriteBool( "NNTP", "UpdateRepliesToMyPosts", true );
            }
        }

        private static void UpdateArticleHeaders()
        {
            IProgressWindow pw = Core.ProgressWindow;
            if( Core.ResourceStore.PropTypes.Exist( "ArticleHeaders" ) )
            {
                int obsoleteId = Core.ResourceStore.PropTypes[ "ArticleHeaders" ].Id;
                IResourceList articles = Core.ResourceStore.GetAllResources( _newsArticle );
                int percent = 0, lastPercent = -1, i = 0;
                int count = articles.Count;
                foreach( IResource article in articles )
                {
                    if( pw != null && lastPercent != percent )
                    {
                        lastPercent = percent;
                        pw.UpdateProgress( percent, "Updating news articles' headers...", null );
                    }
                    if( article.HasProp( obsoleteId ) )
                    {
                        article.SetProp( _propArticleHeaders, article.GetPropText( obsoleteId ) );
                    }
                    percent = ++i * 100 / count;
                }
                Core.ResourceStore.PropTypes.Delete( obsoleteId );
            }
        }

        private static void UpdateArticleHtmlBodies()
        {
            IProgressWindow pw = Core.ProgressWindow;
            if( Core.ResourceStore.PropTypes.Exist( "HtmlBody" ) )
            {
                int obsoleteId = Core.ResourceStore.PropTypes[ "HtmlBody" ].Id;
                IResourceList articles = Core.ResourceStore.GetAllResources( _newsArticle );
                int percent = 0, lastPercent = -1, i = 0;
                int count = articles.Count;
                foreach( IResource article in articles )
                {
                    if( pw != null && lastPercent != percent )
                    {
                        lastPercent = percent;
                        pw.UpdateProgress( percent, "Updating news articles' HTML content...", null );
                    }
                    if( article.HasProp( obsoleteId ) )
                    {
                        article.SetProp( _propHtmlContent, article.GetPropText( obsoleteId ) );
                    }
                    percent = ++i * 100 / count;
                }
                Core.ResourceStore.PropTypes.Delete( obsoleteId );
            }
        }

        private static void  ProcessResources( IntHashSet result, IntHashSet source )
        {
            IntHashSet temp = new IntHashSet();
            foreach( IntHashSet.Entry e in source )
            {
                IResource res = Core.ResourceStore.LoadResource( e.Key );
                res.SetProp( _propIsSelfThread, true );

                IResourceList children = res.GetLinksTo( _newsArticle, Core.Props.Reply );
                for( int i = 0; i < children.Count; i++ )
                {
                    int child = children[ i ].Id;
                    if( !source.Contains( child ) && !result.Contains( child ) )
                        temp.Add( child );
                }
            }

            foreach( IntHashSet.Entry e in temp )
                result.Add( e.Key );

            if( temp.Count > 0 )
                ProcessResources( result, temp );
        }

        private void resourceBrowser_ContentChanged( object sender, EventArgs e )
        {
            IResource selected = _lastSelected;
            if( selected != null && !selected.IsDeleted && selected != Core.ResourceBrowser.OwnerResource && selected.HasProp( _propMarkReadOnLeave ) &&
                ( selected.Type == _newsGroup || selected.Type == _newsFolder || selected.Type == _newsServer ) )
            {
                MarkAllAsReadAction.MarkGroupsRead( selected.ToResourceList() );
            }
            _lastSelected = Core.ResourceBrowser.OwnerResource;
        }

        private static void Core_StateChanged( object sender, EventArgs e )
        {
            if( Core.State == CoreState.Running )
            {
                IResourceList servers = Core.ResourceStore.GetAllResources( _newsServer );
                foreach( IResource server in servers )
                {
                    ServerResource serverResource = new ServerResource( server );
                    if( serverResource.DeliverOnStartup )
                    {
                        NntpClientHelper.DeliverNewsFromServer( server );
                    }
                    else
                    {
                        int freq = serverResource.DeliverFreq;
                        if( freq > 0 )
                        {
                            Core.NetworkAP.QueueJobAt(
                                serverResource.LastUpdateTime.AddMinutes( freq ), "Deliver News",
                                new ResourceDelegate( NntpClientHelper.DeliverNewsFromServer ), server );
                        }
                    }
                }
            }
            else if( Core.State == CoreState.ShuttingDown )
            {
                MarkAllAsReadAction.MarkGroupsRead(
                    Core.ResourceStore.FindResourcesWithProp( null, _propMarkReadOnExit ) );
            }
        }

        /**
         * collect articles for a view
         */
        internal static IResourceList CollectArticles( IResource res, bool filterLocalArticles )
        {
            IResourceList articles;
            if( res.Type == _newsGroup )
            {
                articles = res.GetLinksToLive( null, _propTo );
            }
            else if( NewsFolders.IsDefaultFolder( res ) )
            {
                articles = res.GetLinksToLive( null, _propTo );
                filterLocalArticles = false; // forcedly don't minus local articles from default folder's view
            }
            else
            {
                IResourceList groups = new NewsTreeNode( res ).Groups;
                articles = Core.ResourceStore.EmptyResourceList;
                foreach( IResource group in groups )
                {
                    articles = articles.Union( CollectArticles( group, false ), true );
                }
            }
            if( filterLocalArticles )
            {
                articles = articles.Minus( Core.ResourceStore.GetAllResources( _newsLocalArticle ) );
            }

            return articles;
        }


        internal delegate bool Subscribe2GroupDelegate( string group, IResource server );
        /**
         * return true if group was actually subscribed
         */
        internal static bool Subscribe2Group( string group, IResource server )
        {
            IResourceStore store = Core.ResourceStore;
            IResource groupRes = null;
            IResourceList groups = store.FindResources( _newsGroup, Core.Props.Name, group );
            foreach( IResource groupResource in groups )
            {
                if( new NewsgroupResource( groupResource ).Server == server )
                {
                    groupRes = groupResource;
                    groupRes.BeginUpdate();
                    break;
                }
            }
            if( groupRes == null )
            {
                IResourceList fakeGroups =  Core.ResourceStore.FindResources(
                    _newsGroup, Core.Props.Name, group );
                foreach( IResource fakeGroup in fakeGroups )
                {
                    if( !fakeGroup.HasProp( Core.Props.Parent ) )
                    {
                        groupRes = fakeGroup;
                        groupRes.BeginUpdate();
                        break;
                    }
                }
                if( groupRes == null )
                {
                    groupRes = store.BeginNewResource( _newsGroup );
                }
                new NewsTreeNode( groupRes ).Parent = server;
                groupRes.SetProp( Core.Props.DisplayThreaded, server.GetProp( Core.Props.DisplayThreaded ) );
            }
            bool subscriptionPerformed;
            try
            {
                ServerResource serverResource = new ServerResource( server );
                subscriptionPerformed = !( new NewsgroupResource( groupRes ).IsSubscribed );
                serverResource.SubscribeToGroup( group );
                groupRes.SetProp( Core.Props.Name, group );
                new NewsgroupResource( groupRes ).InvalidateDisplayName( serverResource.AbbreviateLevel );
                IResourceList categories = Core.CategoryManager.GetResourceCategories( server );
                foreach( IResource category in categories )
                {
                    Core.CategoryManager.AddResourceCategory( groupRes, category );
                }
                if( subscriptionPerformed )
                {
                    Core.WorkspaceManager.AddToActiveWorkspace( groupRes );
                    _newsgroupsTreePane.ExpandParents( groupRes );
                }
            }
            finally
            {
                groupRes.EndUpdate();
            }
            if( subscriptionPerformed )
            {
                NntpClientHelper.DownloadHeadersFromGroup( groupRes );
            }
            CheckGroups();
            return subscriptionPerformed;
        }

        internal static void DeliverNews( bool invokedByUser )
        {
            IUIManager uiMgr = Core.UIManager;
            IResourceList servers = Core.ResourceStore.GetAllResources( _newsServer );
            if( servers.Count > 0 )
            {
                IResource outbox = NewsFolders.Outbox;
                IResourceList localArticles = outbox.GetLinksTo( _newsLocalArticle, _propTo ).Minus(
                    Core.ResourceStore.FindResourcesWithProp( null, Core.Props.IsDeleted ) );
                if( localArticles.Count > 0 )
                {
                    uiMgr.GetStatusWriter( typeof( NntpPlugin ), StatusPane.Network ).ShowStatus( "Posting articles from Outbox" );
                    foreach( IResource article in localArticles )
                    {
                        NntpClientHelper.PostArticle( article, null, false );
                    }
                }
                uiMgr.GetStatusWriter( typeof( NntpPlugin ), StatusPane.Network ).ShowStatus( "Delivering news..." );
                _deliverNewsUnitCount = 0;
                IResource preferableGroup =
                    ( Core.TabManager.CurrentTab.Id == "News" ) ? _newsgroupsTreePane.SelectedNode : null;
                IResource preferableServer =
                    ( preferableGroup != null && preferableGroup.Type == _newsGroup ) ?
                        new NewsgroupResource( preferableGroup ).Server : null;
                if( preferableServer != null )
                {
                    NntpClientHelper.DeliverNewsFromServer( preferableServer, preferableGroup,
                        invokedByUser, deliverNewsFromServer_Finished );
                }
                foreach( IResource server in servers )
                {
                    if( preferableServer != server )
                    {
                        NntpClientHelper.DeliverNewsFromServer( server, preferableGroup,
                            invokedByUser, deliverNewsFromServer_Finished );
                    }
                }
            }
        }

        private static void deliverNewsFromServer_Finished( AsciiProtocolUnit unit )
        {
            if( _deliverNewsUnitCount == 0 )
            {
                Core.UIManager.GetStatusWriter( typeof( NntpPlugin ), StatusPane.Network ).ClearStatus();
            }
        }

        internal static Icon LoadNewsIcon( string iconName )
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "NntpPlugin.Icons." + iconName );
            if( stream != null )
            {
                return new Icon( stream );
            }
            return null;
        }

        internal static void CheckGroups()
        {
            _areThereGroups = Core.ResourceStore.GetAllResources( _newsGroup ).Count > 0;
        }

        private static string DisplayGroupOrServerError( IResource res )
        {
            return res.GetPropText( Core.Props.LastError );
        }

        #region Icon Providers
        /**
         * icon provider for newsgroup resources
         */
        private class NewsgroupIconProvider: IResourceIconProvider, IOverlayIconProvider
        {
            private Icon _groupMainIcon;
            private Icon _groupUnsubscribedIcon;
            private Icon _groupErrorIcon;

            private readonly Icon[]  _pausedSign = new Icon[ 1 ];

            public NewsgroupIconProvider()
            {
                _pausedSign[ 0 ] = LoadNewsIcon( "pause.ico" );
            }

            private Icon GroupMainIcon
            {
                get
                {
                    if( _groupMainIcon == null )
                    {
                        _groupMainIcon = LoadNewsIcon( "newsgroups.ico" );
                    }
                    return _groupMainIcon;
                }
            }

            private Icon GroupUnsubscribedIcon
            {
                get
                {
                    if( _groupUnsubscribedIcon == null )
                    {
                        _groupUnsubscribedIcon = LoadNewsIcon( "unsubscribed_newsgroups.ico" );
                    }
                    return _groupUnsubscribedIcon;
                }
            }

            private Icon GroupErrorIcon
            {
                get
                {
                    if( _groupErrorIcon == null )
                    {
                        _groupErrorIcon = LoadNewsIcon( "error_newsgroups.ico" );
                    }
                    return _groupErrorIcon;
                }
            }

            public Icon GetResourceIcon( IResource resource )
            {
                if( resource.Type == _newsGroup )
                {
                    if( resource.HasProp( Core.Props.LastError ) )
                    {
                        return GroupErrorIcon;
                    }
                    return ( new NewsgroupResource( resource ).IsSubscribed ) ? GroupMainIcon : GroupUnsubscribedIcon;
                }
                return null;
            }

            public Icon GetDefaultIcon( string resType )
            {
                if( resType == _newsGroup )
                {
                    return GroupMainIcon;
                }
                return null;
            }

            public Icon[] GetOverlayIcons( IResource res )
            {
                int order = res.GetIntProp( _propNewsSortOrder );
                return (order == 1) ? _pausedSign : null;
            }
        }

        /// <summary>
        /// Decorate a news article with "Paused" sign if it is the head of
        /// the thread which was paused for updates.
        /// </summary>
        private class ArticleOverlayIconProvider: IOverlayIconProvider
        {
            private readonly Icon[]  _pausedSign = new Icon[ 1 ];

            public ArticleOverlayIconProvider()
            {
                _pausedSign[ 0 ] = LoadNewsIcon( "pause.ico" );
            }

            public Icon[] GetOverlayIcons( IResource res )
            {
                return (res.HasProp( _propIsIgnoredThread) ? _pausedSign : null);
            }
        }
        #endregion Icon Providers

        private class NewsWorkspaceSelectorFilter: IResourceNodeFilter
        {
            public bool AcceptNode( IResource res, int level )
            {
                return !NewsFolders.IsDefaultFolder( res );
            }
        }

        private class NewsArticleDeleter: DefaultResourceDeleter
        {
            public override bool CanIgnoreRecyclebin()
            {
                return false;
            }

            public override void DeleteResourcePermanent( IResource article )
            {
                string id = article.GetPropText( _propArticleId );
                if( id.Length > 0 )
                {
                    IResourceList groups = article.GetLinksOfType( _newsGroup, _propTo );
                    HashSet servers = new HashSet();
                    foreach( IResource group in groups )
                    {
                        IResource server = new NewsgroupResource( group ).Server;
                        if( server != null )
                        {
                            servers.Add( server );
                        }
                    }
                    foreach( HashSet.Entry e in servers )
                    {
                        IResource server = (IResource) e.Key;
                        if( !Core.ResourceStore.FindResources(
                            null, _propDeletedList, id ).Contains(  server ) )
                        {
                            server.GetStringListProp( _propDeletedList ).Add( id );
                        }
                    }
                }
                IContactManager contactManager = Core.ContactManager;
                IResource from = article.GetLinkProp( contactManager.Props.LinkFrom );
                article.GetLinksOfType( null, _propAttachment ).DeleteAll();
                article.Delete();
                if( from != null )
                {
                    contactManager.DeleteUnusedContacts( from.ToResourceList() );
                }
            }
        }

        internal const string               _newsServer = "NewsServer";
        internal const string               _newsGroup = "NewsGroup";
        internal const string               _newsArticle = "Article";
        internal const string               _newsFolder = "NewsFolder";
        internal const string               _newsLocalArticle = "LocalArticle";
        internal const string               _unknownFileResourceType = "UnknownFile";
        internal const string               _eapGroupName = "jetbrains.omniamea.eap";
        internal const string               _eapAnnouncementGroupName = "jetbrains.omniamea.eap.announcements";
        internal static NntpPlugin          _plugin;
        internal static int                 _propPort;
        internal static int                 _propLastUpdated;
        internal static int                 _propTo;
        internal static int                 _propRawSubject;
        internal static int                 _propRawFrom;
        internal static int                 _propHtmlContent;
        internal static int                 _propHasNoBody;
        internal static int                 _propArticleId;
        internal static int                 _propReferenceId;
        internal static int                 _propReply;
        internal static int                 _propIsUnread;
        internal static int                 _propAttachment;
        internal static int                 _propEmbeddedContent;
        internal static int                 _propUsername;
        internal static int                 _propPassword;
        internal static int                 _propSsl3Enabled;
        internal static int                 _propUserDisplayName;
        internal static int                 _propEmailAddress;
        internal static int                 _propNewsgroupList;
        internal static int                 _propSubscribedNewsgroupList;
        internal static int                 _propNewNewsgroupList;
        internal static int                 _propLastSelectedArticle;
        internal static int                 _propLastArticleDate;
        internal static int                 _propInlineAttachment;
        internal static int                 _propDeletedList;
        internal static int                 _propDirectory;
        internal static int                 _propNntpText;
        internal static int                 _propContent;
        internal static int                 _propIsIgnoredThread;
        internal static int                 _propIsSelfThread;
        internal static int                 _propThreadVisibilityToggleDate;
        internal static int                 _propArticleHeaders;
        internal static int                 _propFollowupTo;
        internal static int                 _propNoMoreHeaders;
        internal static int                 _propFirstArticle;
        internal static int                 _propLastArticle;
        internal static int                 _propArticleNumbers;
        internal static int                 _propAbbreviateLevel;
        internal static int                 _propCountToDownloadAtTime;
        internal static int                 _propDeliverOnStartup;
        internal static int                 _propDeliverFreq;
        internal static int                 _propMarkFromMeAsRead;
        internal static int                 _propDownloadBodiesOnDeliver;
        internal static int                 _propDownloadBodyOnSelection;
        internal static int                 _propMailFormat;
        internal static int                 _propMIMETextEncoding;
        internal static int                 _propPutInOutbox;
        internal static int                 _propOverrideSigSettings;
        internal static int                 _propUseSignature;
        internal static int                 _propMailSignature;
        internal static int                 _propReplySignaturePosition;
        internal static int                 _propNewsSortOrder;
        internal static int                 _propMarkReadOnExit;
        internal static int                 _propMarkReadOnLeave;
        internal static ProductType         _productType;
        internal static IResourceTreePane   _newsgroupsTreePane;
        internal static bool                _areThereGroups;
        internal static int                 _deliverNewsUnitCount;
        internal static ArticlePreviewPane  _previewPane;
        private IResource                   _lastSelected;
        private static string               _lastNavigatedGroup;
        private static RegistryKey          _newsProtocolsKey;
        public static readonly string       _networkUnavailable = "Network is unavailable";

        #endregion
    }
}
