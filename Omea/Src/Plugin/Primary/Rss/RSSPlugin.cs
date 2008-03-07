/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.RSSPlugin.SubscribeWizard;

namespace JetBrains.Omea.RSSPlugin
{
    [PluginDescription("JetBrains Inc.", "Support for RSS/Atom subscriptions.")]
    public class RSSPlugin : IPlugin, IResourceDisplayer, IResourceUIHandler,
                             IResourceTextProvider, IRssService
    {
        private ResourceTreePaneBase _rssTreePane;
        private IResource _feedRoot;
        private IResource _lastSelectedFeed;
        private IResource _lastDisplayedFeed;
        private bool _lastDisplayUnread;
        private bool _lastDisplayThreaded;
        private bool _lastDisplayNewspaper;
        private IResourceList _lastSelectedFeedList;
        private IResourceList _lastDisplayedGroupWatcher;
        private GroupUnreadCountDecorator _groupUnreadCountDecorator;

        public static readonly string   _NetworkUnavailable = "Network is unavailable";

        private static RSSPlugin            _thePlugin;

        private static FeedUnreadsFilter _feedsPaneUnreadFilter = new FeedUnreadsFilter();
        private static ErrorFeedFilter _feedsPaneErrorFilter = new ErrorFeedFilter();
        private static PlaneListProvider   _feedsPlaneListProvider = new PlaneListProvider();
        private static String              _savedOrder;

        private static FeedUpdateQueue _updateQueue = new FeedUpdateQueue();
        private BlogExtensionManager _blogExtensionManager;
        private CommentThreadingHandler _commentThreadingHandler;

        private IntHashTableOfInt _selectionMap = new IntHashTableOfInt(); // feed ID -> item ID

        private ImportManager _importManager = null;
        Hashtable _feedImporters = new Hashtable();

		protected IStatusWriter _statuswriter = null;

        public event ResourceEventHandler FeedUpdated;
		public event EventHandler UpdateAllStarted;
		public event EventHandler UpdateAllFinished;

        private static MethodInvoker _scheduleUpdateDelegate = ScheduleUpdate;

		/// <summary>
		/// <c>True</c> if the “Update All Feeds” action is currently in progress.
		/// Is reset to <c>False</c> automatically when all the pending updates finish.
		/// Does not set to <c>True</c> if a schedulled update or a manual update of a single feed occurs.
		/// </summary>
		public bool	_isUpdatingAll = false;

        public RSSPlugin()
        {
            _thePlugin = this;
			_statuswriter = Core.UIManager.GetStatusWriter( this, StatusPane.UI );
        }

        public static RSSPlugin GetInstance()
        {
            return _thePlugin;
        }
        static private void CreateSearchEngine( string name, string url )
        {
            Guard.NullArgument( name, "name" );
            Guard.NullArgument( url, "url" );
            IResource engine = Core.ResourceStore.FindUniqueResource( Props.RSSSearchEngineResource, Core.Props.Name, name );
            if ( engine == null )
            {
                engine = Core.ResourceStore.BeginNewResource( Props.RSSSearchEngineResource );
                engine.SetProp( Core.Props.Name, name );
            }
            else
            {
                engine.BeginUpdate();
            }
            engine.SetProp( Props.URL, url );
            engine.EndUpdate();
        }

        static private void CreateSearchEngines()
        {
            CreateSearchEngine( "Google News", "http://news.google.com/news?output=rss&scoring=d&ie=UTF-8&q=" );
            CreateSearchEngine( "MSN Search", "http://search.msn.com:80/results.aspx?format=rss&FORM=R0RE&q=" );
            CreateSearchEngine( "Yahoo! Search", "http://api.search.yahoo.com/WebSearchService/rss/webSearch.xml?adult_ok=1&query=" );
            CreateSearchEngine( "Feedster", "http://feedster.com/search.php?sort=date&ie=UTF-8&hl=&content=full&type=rss&limit=15&q=" );
            CreateSearchEngine( "Blogdigger", "http://www.blogdigger.com/rss.jsp?sortby=date&q=" );
            CreateSearchEngine( "Google Blog Search", "http://blogsearch.google.com/blogsearch_feeds?hl=en&btnG=Search+Blogs&scoring=d&num=20&output=rss&ie=UTF-8&q=" );
            CreateSearchEngine( "BlogPulse", "http://www.blogpulse.com/rss?sort=date&operator=and&query=" );
            CreateSearchEngine( "blogs.yandex.ru", "http://blogs.yandex.ru/search.rss?how=tm&rd=2&charset=UTF-8&no_group=1&text=" );
            CreateSearchEngine( "IceRocket Blog Search", "http://www.icerocket.com/search?tab=blog&rss=1&q=" );
            CreateSearchEngine( "Sphere", "http://www.sphere.com/rss?datedrop=0&sortby=date&histdays=120&q=" );
        }

        public void Register()
        {
            Props.Register( this );
            Core.ResourceStore.RegisterUniqueRestriction( Props.RSSSearchEngineResource, Core.Props.Name );
            CreateSearchEngines();

            IUIManager uiMgr = Core.UIManager;
            uiMgr.RegisterOptionsGroup( "Internet", "The Internet options enable you to control how [product name] works with several types of online content." );
            uiMgr.RegisterOptionsPane( "Internet", "Feeds", CreateRSSOptionsPane,
                                       "The Feeds options enable you to control when and how posts to RSS and Atom feeds are downloaded (and subsequently indexed)." );
            uiMgr.RegisterOptionsPane( "Internet", "Feeds Enclosures", CreateRSSEnclosureOptionsPane,
                                       "The Feeds Enclosures options enable you to control when and where downloads RSS enclosures." );

            InitRootFeedGroup();
            Core.TabManager.RegisterResourceTypeTab( "Feeds", "Feeds",
                                                     new string[] {"RSSItem", "RSSFeed", "RSSFeedGroup"}, 4 );


            _rssTreePane = new JetResourceTreePane();
            _rssTreePane.RootResourceType = "RSSFeed";
            Core.LeftSidebar.RegisterResourceStructurePane( "Feeds", "Feeds", "Feeds",
                                                            LoadIconFromAssembly( "RSSfeeds.ico" ), _rssTreePane );
            _rssTreePane.WorkspaceFilterTypes = new string[] {"RSSFeed", "RSSFeedGroup"};
            _groupUnreadCountDecorator = new GroupUnreadCountDecorator();
            _rssTreePane.AddNodeDecorator( _groupUnreadCountDecorator );
            _rssTreePane.AddNodeDecorator( new FeedActivenessDecorator() );
            _rssTreePane.AddNodeDecorator( new TotalCountDecorator( "RSSFeed", Props.RSSItem, Core.Props.Parent ) );
            _rssTreePane.ToolTipCallback = HandleToolTipCallback;

            _feedsPaneUnreadFilter = new FeedUnreadsFilter();
            _feedsPaneUnreadFilter.Hide = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.FilterUnreadFeeds, false );
            _rssTreePane.AddNodeFilter( _feedsPaneUnreadFilter );
            _feedsPaneErrorFilter = new ErrorFeedFilter();
            _feedsPaneErrorFilter.Hide = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.FilterErrorFeeds, false );
            _rssTreePane.AddNodeFilter( _feedsPaneErrorFilter );

            Core.LeftSidebar.RegisterViewPaneShortcut( "Feeds", Keys.Control | Keys.Alt | Keys.F );

            Core.PluginLoader.RegisterViewsConstructor( new RSSUgrade1ViewsConstructor() );
            Core.PluginLoader.RegisterViewsConstructor( new RSSDataUpgrade() );
            Core.PluginLoader.RegisterViewsConstructor( new RSSDataUpgrade2() );
            Core.PluginLoader.RegisterViewsConstructor( new RSSViewsConstructor() );
            Core.PluginLoader.RegisterViewsConstructor( new RSSUgrade2ViewsConstructor() );
            Core.PluginLoader.RegisterViewsConstructor( new RSSUgrade3ViewsConstructor() );

            //-----------------------------------------------------------------
            //  Register Search Extensions to narrow the list of results using
            //  simple phrases in search queries:
            //  - two synonyms for restricting the resource type to feed articles;
            //  - restriction to those posts which are comments to others;
            //  - restriction to those posts which have enclosures.
            //-----------------------------------------------------------------
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "feeds", "RSSItem" );
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "rss", "RSSItem" );
            IResource cond = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "DeepName", RSSViewsConstructor.PostIsACommentDeep );
            if( cond != null )
                Core.SearchQueryExtensions.RegisterSingleTokenRestriction( "in", "comments", cond );
            cond = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "DeepName", RSSViewsConstructor.PostHasEnclosuredDeep );
            if( cond != null )
                Core.SearchQueryExtensions.RegisterSingleTokenRestriction( "in", "enclosured", cond );

            //-----------------------------------------------------------------
            uiMgr.RegisterResourceLocationLink( "RSSItem", -Props.RSSItem, "RSSFeed" );
            uiMgr.RegisterResourceLocationLink( "RSSFeedGroup", 0, "RSSFeedGroup" );
            uiMgr.RegisterResourceSelectPane( "RSSFeed", typeof (ResourceTreeSelectPane) );

            IWorkspaceManager workspaceMgr = Core.WorkspaceManager;
            workspaceMgr.RegisterWorkspaceType( "RSSFeed", new int[] {Props.RSSItem},
                                                WorkspaceResourceType.Container );
            workspaceMgr.RegisterWorkspaceFolderType( "RSSFeedGroup", "RSSFeed", new int[] {Props.RSSItem} );
            workspaceMgr.RegisterWorkspaceType( "RSSItem",
                new int[] { -Props.ItemComment }, WorkspaceResourceType.None );

            Core.PluginLoader.RegisterResourceTextProvider( "RSSItem", this );
            Core.PluginLoader.RegisterResourceDisplayer( "RSSItem", this );
            Core.PluginLoader.RegisterResourceUIHandler( "RSSFeed", this );
            Core.PluginLoader.RegisterResourceUIHandler( "RSSFeedGroup", this );
            Core.PluginLoader.RegisterNewspaperProvider( "RSSItem", new RssNewspaperProvider() );

			// Drag'n'drop
            RSSDragDropHandler dragDropHandler = new RSSDragDropHandler();
			Core.PluginLoader.RegisterResourceDragDropHandler( Core.ResourceTreeManager.ResourceTreeRoot.Type, dragDropHandler );
			Core.PluginLoader.RegisterResourceDragDropHandler( "RSSFeed", new DragDropLinkAdapter( dragDropHandler ) );
			Core.PluginLoader.RegisterResourceDragDropHandler( "RSSFeedGroup", new DragDropLinkAdapter( dragDropHandler ) );

            RSSRenameHandler renameHandler = new RSSRenameHandler();
            Core.PluginLoader.RegisterResourceRenameHandler( "RSSFeed", renameHandler );
            Core.PluginLoader.RegisterResourceRenameHandler( "RSSFeedGroup", renameHandler );

            Core.PluginLoader.RegisterPluginService( this );

            // Register default importers
            new FeedDemonImporter();
            new RssBanditImporter();
            new SharpReaderImporter();
            new BloglinesImporter();
            new OPMLImporter();
            
            // After service
            _importManager = new ImportManager( null, _feedImporters );
            if( Core.ProductFullName.EndsWith( "Reader" ) )
            {
                uiMgr.RegisterWizardPane( ImportManager.ImportPaneName, _importManager.GetImportWizardPane, -1 );
            }

            Core.ResourceBrowser.RegisterLinksPaneFilter( "RSSItem", new ItemRecipientsFilter() );
            Core.ResourceBrowser.RegisterLinksPaneFilter( "RSSItem", new WeblogFromFilter() );

            _blogExtensionManager = new BlogExtensionManager();
            _blogExtensionManager.LoadExtensions();

            _updateQueue.FeedUpdated += _updateQueue_OnFeedUpdated;
			_updateQueue.QueueGotEmpty += OnFeedUpdateQueueGotEmpty;

            Core.RemoteControllerManager.AddRemoteCall( "RSSPlugin.SubscribeToFeed.1",
                                                        new RemoteSubscribeToFeedDelegate( RemoteSubscribeToFeed ) );

            Core.PluginLoader.RegisterResourceDeleter( "RSSItem", new RSSItemDeleter() );

            //  Register Link Id which serves as an anchor for tracing the events
            //  when new article is created and linked to its folder.
            int linkId = Core.ResourceStore.GetPropId( "RSSItem" );
            Core.ExpirationRuleManager.RegisterResourceType( -linkId, "RSSFeed", "RSSItem" );

            Core.ProtocolHandlerManager.RegisterProtocolHandler( "feed", "feed aggregator", RemoteSubscribeToFeed );
            RSSFeedIconProvider rssFeedIconProvider = new RSSFeedIconProvider();
            Core.ResourceIconManager.RegisterResourceIconProvider( "RSSFeed", rssFeedIconProvider );
            Core.ResourceIconManager.RegisterOverlayIconProvider( "RSSFeed", rssFeedIconProvider );
            Core.ResourceIconManager.RegisterOverlayIconProvider( "RSSFeedGroup", rssFeedIconProvider );
			Core.ResourceIconManager.RegisterResourceIconProvider( "RSSItem", new RSSItemIconProvider(rssFeedIconProvider) );

            _commentThreadingHandler = new CommentThreadingHandler();
            Core.PluginLoader.RegisterResourceThreadingHandler( "RSSItem", _commentThreadingHandler );
        	EnclosureDownloadStateColumn.Register();
			Core.DisplayColumnManager.RegisterPropertyToTextCallback( Props.EnclosureDownloadedSize, OnPropertyToSize );
			Core.DisplayColumnManager.RegisterPropertyToTextCallback( Props.EnclosureSize, OnPropertyToSize );

            Core.ResourceBrowser.RegisterLinksGroup( "Addresses", new int[] { Props.RSSItem }, ListAnchor.First );

            Core.ResourceBrowser.SetDefaultViewSettings( "Feeds", AutoPreviewMode.Off, true );
        }

		/// <summary>
		/// Converts an integer property to a string containing its size representation.
		/// </summary>
    	public static string OnPropertyToSize( IResource res, int propId )
    	{
    		return Utils.SizeToString( res.GetIntProp( propId ) );
    	}

    	private static AbstractOptionsPane CreateRSSOptionsPane()
        {
            return new RSSOptionPane();
        }
        private static AbstractOptionsPane CreateRSSEnclosureOptionsPane()
        {
            return new RSSEnclosureOptionPane();
        }

        private static void UpdateSubjectAndBodyCRC()
        {
            if ( Core.ResourceStore.FindResourcesWithProp( "RSSItem", Props.RssLongBodyCRC ).Count != 0 )
            {
                return;
            }
            IResourceList list = Core.ResourceStore.GetAllResources( "RSSItem" );
            foreach ( IResource item in list )
            {
                int crc = Utils.GetHashCodeInLowerCase( item.GetPropText( Core.Props.Subject ), item.GetPropText( Core.Props.LongBody ) );
                item.SetProp( Props.RssLongBodyCRC, crc );
            }
        }

        public void Startup()
        {
            UpdateSubjectAndBodyCRC();
            foreach ( IResource feedGroup in Core.ResourceStore.GetAllResources( "RSSFeedGroup" ) )
            {
                if ( !feedGroup.HasProp( Core.Props.Parent ) || feedGroup.GetLinkProp( Core.Props.Parent ) == feedGroup )
                {
                    feedGroup.AddLink( Core.Props.Parent, _feedRoot );
                }
            }

            if ( !Core.SettingStore.ReadBool( IniKeys.Section, "DefaultSubscriptionCreated", false ) )
            {
                Core.SettingStore.WriteBool( IniKeys.Section, "DefaultSubscriptionCreated", true );
                FindOrCreateFeed( "Omea News", "http://jetbrains.com/omearss.xml" );
                FindOrCreateFeed( "Omea Tips and Tricks", "http://blogs.jetbrains.com/omea/wp-rss2.php" );
                FindOrCreateFeed( "JetBrains News", "http://jetbrains.com/rss.xml" );
                
            }

            string newSummaryStyle = Core.SettingStore.ReadString( IniKeys.Section, "SummaryStyle", string.Empty );
            if( newSummaryStyle.Length > 0 )
            {
                RSSItemView.SummaryStyle = newSummaryStyle;
            }

            // No preview possible, do all work by hands
            _importManager.DoImport( _feedRoot, true );
            _importManager.DoImportCache();

            Core.StateChanged += Core_StateChanged;

            PerformStructureCorrections();
            _groupUnreadCountDecorator.UpdateGroupUnreadCount( false );
        }

        private void FindOrCreateFeed(string name, string url )
        {
            if( Core.ResourceStore.FindResources( null, Props.URL, url ).Count == 0 ) 
            {
                IResource feed = CreateFeed( name, url, null );
                if( feed != null )
                {
                    QueueFeedUpdate( feed );
                }
            }
        }

        private void  PerformStructureCorrections()
        {
            IResourceList allFeeds = Core.ResourceStore.GetAllResources( "RSSFeed" );
            foreach ( IResource feed in allFeeds )
            {
                IResource commentOwnerItem = feed.GetLinkProp( Props.ItemCommentFeed );
                if ( !feed.HasProp( Core.Props.Parent ) && commentOwnerItem == null )
                {
                    feed.AddLink( Core.Props.Parent, _feedRoot );
                }

                IResource parentFeed = feed.GetLinkProp( Props.FeedComment2Feed );

                if ( commentOwnerItem != null && parentFeed == null )
                {
                    IResource pFeed = commentOwnerItem.GetLinkProp( -Props.RSSItem );
                    if ( pFeed != null )
                    {
                        pFeed.SetProp( Props.FeedComment2Feed, feed );
                    }
                }

                // delete leftover transient feeds 
                if ( feed.GetIntProp( Props.Transient ) == 1 )
                {
                    new ResourceProxy( feed ).DeleteAsync();
                }
                else
                {
                    if ( feed.GetStringProp( Props.UpdateStatus ) == "(updating)" )
                    {
                        new ResourceProxy( feed ).DeleteProp( Props.UpdateStatus );
                    }

                    UpgradeDeletedItems( feed );
                }
            }

            if( Core.ResourceStore.PropTypes.Exist( "DeletedItems" ) )
            {
                int propId = Core.ResourceStore.PropTypes[ "DeletedItems" ].Id;
                Core.ResourceStore.PropTypes.Delete( propId );
            }
        }

        private static void Core_StateChanged( object sender, EventArgs e )
        {
            if ( Core.State == CoreState.Running )
            {
                Utils.NetworkConnectedStateChanged += NetworkConnectedStateChanged;
                Core.NetworkAP.QueueJobAt( DateTime.Now.AddSeconds( 5 ), _scheduleUpdateDelegate );
                EnclosureDownloadManager.DownloadNextEnclosure();

                bool treeFormat = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.ShowPlaneList, false );
                UpdateSortFilter( treeFormat );
            }
        }

        private static void NetworkConnectedStateChanged()
        {
            if( Utils.IsNetworkConnectedLight() )
            {
                Core.NetworkAP.QueueJob( _scheduleUpdateDelegate );
            }
        }

        private static void ScheduleUpdate()
        {
            foreach ( IResource feed in Core.ResourceStore.GetAllResources( "RSSFeed" ) )
            {
                _updateQueue.ScheduleFeedUpdate( feed );
            }
        }

        private static void UpgradeDeletedItems( IResource feed )
        {
            if ( Core.ResourceStore.PropTypes.Exist( "DeletedItems" ) && feed.HasProp( "DeletedItems" ) )
            {
                string deletedItems = feed.GetStringProp( "DeletedItems" );
                IStringList deletedItemList = feed.GetStringListProp( Props.DeletedItemHashList );
                foreach ( string itemHash in deletedItems.Split( ';' ) )
                {
                    deletedItemList.Add( itemHash );
                }
                feed.DeleteProp( "DeletedItems" );
            }
        }

        private void InitRootFeedGroup()
        {
            _feedRoot = Core.ResourceTreeManager.GetRootForType( "RSSFeed" );
            Core.ResourceTreeManager.SetResourceNodeSort( _feedRoot, "Type- Name" ); // groups above feeds 
            _feedRoot.DisplayName = "All Feeds";
        }

        internal static void UpdateUnreadPaneFilter( bool hide )
        {
            _feedsPaneUnreadFilter.Hide = hide;
            RSSTreePane.UpdateNodeFilter( true );
        } 

        internal static void UpdateErrorPaneFilter( bool hide )
        {
            _feedsPaneErrorFilter.Hide = hide;
            RSSTreePane.UpdateNodeFilter( true );
        } 

        internal static void UpdateSortFilter( bool show )
        {
            if( show )
            {
                Core.UIManager.RunWithProgressWindow( "Sorting feeds", new MethodInvoker( SortFeeds ) );
            }
            else
            {
                IResource root = ((ResourceTreePaneBase)RSSTreePane).RootResource;
                ResourceTreeDataProvider provider = ((ResourceTreePaneBase)RSSTreePane).DataProvider;
                provider.ResourceChildProvider = null;
                new ResourceProxy( root ).SetProp( Core.Props.UserResourceOrder, _savedOrder );
                provider.RebuildTree();
            }
        }

        private static void SortFeeds()
        {
            Core.ProgressWindow.UpdateProgress( 0, "Sorting feeds", null );
            IResource root = ((ResourceTreePaneBase)RSSTreePane).RootResource;
            ResourceTreeDataProvider provider = ((ResourceTreePaneBase)RSSTreePane).DataProvider;

            provider.ResourceChildProvider = _feedsPlaneListProvider;
            _savedOrder = root.GetStringProp( Core.Props.UserResourceOrder );

            IResourceList feeds = _feedsPlaneListProvider.GetChildResources( root );
            feeds.Sort( new LastPostComparer(), true );
            UserResourceOrder uro = new UserResourceOrder( root );
            uro.WriteSortOrder( feeds.ResourceIds );
            provider.RebuildTree();
        }

        private class PlaneListProvider : IJetResourceChildProvider
        {
            public IResourceList GetChildResources( IResource parent )
            {
                IResourceList allFeeds = null;
                if( parent.Id == ((ResourceTreePaneBase)RSSTreePane).RootResource.Id )
                {
                    allFeeds = Core.ResourceStore.GetAllResourcesLive( Props.RSSFeedResource );
                    allFeeds = allFeeds.Minus( Core.ResourceStore.FindResourcesWithPropLive( Props.RSSFeedResource, Props.FeedComment2Feed ) );
                }
                return allFeeds;
            }
        }

	    private class LastPostComparer : IResourceComparer
	    {
            private readonly Hashtable hash = new Hashtable();

		    #region IResourceComparer Members
		    public int CompareResources( IResource r1, IResource r2 )
		    {
                DateTime r1Time, r2Time;
                if( hash.Contains( r1.Id ))
                    r1Time = (DateTime)hash[ r1.Id ];
                else
                {
                    r1Time = CalcTime( r1 );
                    hash[ r1.Id ] = r1Time;
                }

                if( hash.Contains( r2.Id ))
                    r2Time = (DateTime)hash[ r2.Id ];
                else
                {
                    r2Time = CalcTime( r2 );
                    hash[ r2.Id ] = r2Time;
                }

			    return r1Time.CompareTo( r2Time );
		    }
		    #endregion

            internal static DateTime CalcTime( IResource feed )
            {
                DateTime time = DateTime.MinValue;
                if( feed.Type == Props.RSSFeedResource )
                {
                    IResourceList posts = feed.GetLinksOfType( Props.RSSItemResource, Props.RSSItem );
                    posts.Sort( new int[] { Core.Props.Date }, false );
                    if( posts.Count > 0 )
                        time = posts[ 0 ].GetDateProp( Core.Props.Date );
                }
                return time;
            }
	    }

        internal static IResource RootFeedGroup { get { return _thePlugin._feedRoot; } }

        internal static IResourceTreePane   RSSTreePane { get { return _thePlugin._rssTreePane; } }

        internal static BlogExtensionManager ExtensionManager { get { return _thePlugin._blogExtensionManager; } }

        internal Hashtable FeedImporters { get { return _feedImporters; } }

        /**
         * Creates a feed group with the specified parent and name.
         */

        internal static IResource CreateFeedGroup( IResource parent, string name )
        {
            ResourceProxy proxy = ResourceProxy.BeginNewResource( "RSSFeedGroup" );
            proxy.SetProp( Core.Props.Name, name );
            proxy.AddLink( Core.Props.Parent, parent );
            proxy.EndUpdate();

            Core.ResourceTreeManager.SetResourceNodeSort( proxy.Resource, "Type- Name" ); // groups above feeds
            Core.WorkspaceManager.AddToActiveWorkspaceRecursive( proxy.Resource );
            return proxy.Resource;
        }

        public IResourceList GetAllFeeds()
        {
            // transient feeds do not have the Parent property
            return Core.ResourceStore.FindResourcesWithProp( "RSSFeed", Core.Props.Parent );
        }

        public static IResource GetExistingFeed( string url )
        {
            foreach ( IResource res in Core.ResourceStore.FindResources( "RSSFeed", Props.URL, url ) )
            {
                if ( res.HasProp( Core.Props.Parent ) )
                {
                    return res;
                }
            }
            return null;
        }

        bool IResourceTextProvider.ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            if ( res.Type == "RSSItem" )
            {
                string title = res.GetPropText( Core.Props.Subject );
                if ( title.Length > 0 )
                {
                    consumer.AddDocumentHeading( res.Id, title );
                }

                string body = res.GetPropText( Core.Props.LongBody );
                if ( body.Length > 0 )
                {
                    consumer.RestartOffsetCounting();
                    HtmlIndexer.IndexHtml( res, body, consumer, null );
                }

                IResourceList parent = res.GetLinksTo( "RSSFeed", "RSSItem" );
                if ( parent.Count == 1 )
                {
                    string author = "";
                    if ( parent[ 0 ].HasProp( Props.Author ) )
                    {
                        author += parent[ 0 ].GetStringProp( Props.Author ) + " ";
                    }
                    if ( parent[ 0 ].HasProp( Core.Props.Name ) )
                    {
                        author += parent[ 0 ].GetStringProp( Core.Props.Name );
                    }
                    if ( author.Length > 0 )
                    {
                        consumer.AddDocumentFragment( res.Id, author, DocumentSection.SourceSection );
                    }
                }
            }
            return true;
        }

        public void Shutdown()
        {}

        #region IResourceDisplayer Members

        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            if ( resourceType == "RSSItem" )
            {
                return new RSSItemView();
            }

            return null;
        }

        #endregion

        internal static Icon LoadIconFromAssembly( string name )
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream( "RSSPlugin.Icons." + name );
            if ( stream != null )
            {
                return new Icon( stream );
            }
            return null;
        }

        public void ResourceNodeSelected( IResource res )
        {
            if ( res.Type == "RSSFeed" )
            {
                DisplayFeedItemList( res );
            }
            else if ( res.Type == "RSSFeedGroup" )
            {
                DisplayGroupItemList( res );
            }
        }

        private void DisplayFeedItemList( IResource res )
        {
            bool displayUnread = res.HasProp( Core.Props.DisplayUnread );
            bool displayNewspaper = res.HasProp( Core.Props.DisplayNewspaper );
            if ( res != _lastSelectedFeed || displayUnread != _lastDisplayUnread ||
                displayNewspaper != _lastDisplayNewspaper )
            {
                _lastSelectedFeed = res;
                _lastDisplayUnread = displayUnread;
                bool haveComments = false;
                _lastSelectedFeedList = ItemsInFeed( res, !displayNewspaper, ref haveComments );
                _lastDisplayThreaded = haveComments;
                _lastDisplayNewspaper = displayNewspaper;

                if ( displayUnread )
                {
                    _lastSelectedFeedList = GetUnreadResources( _lastSelectedFeedList );
                }
            }

            string captionTemplate = "%OWNER%";
            if ( displayUnread )
            {
                captionTemplate = "Unread Posts in " + captionTemplate;
            }
            DisplayRSSItemList( res, _lastSelectedFeedList, captionTemplate, _lastDisplayThreaded );
            _lastDisplayedFeed = res;
            Core.ResourceBrowser.ContentChanged += HandleBrowserContentChanged;
        }

        private IResource GetRememberedSelection( IResource res )
        {
            IResource selResource = null;
            if ( Settings.RememberSelection )
            {
                selResource = res.GetLinkProp( Props.SelectedRSSItem );
            }
            else if ( _selectionMap.ContainsKey( res.Id ) )
            {
                int selResourceId = _selectionMap[ res.Id ];
                selResource = Core.ResourceStore.TryLoadResource( selResourceId );
            }
            return selResource;
        }

        private void HandleBrowserContentChanged( object sender, EventArgs e )
        {
            Core.ResourceBrowser.ContentChanged -= HandleBrowserContentChanged;
            if ( _lastDisplayedGroupWatcher != null )
            {
                _lastDisplayedGroupWatcher.ResourceChanged -= HandleDisplayedFeedChanged;
                _lastDisplayedGroupWatcher.Dispose();
                _lastDisplayedGroupWatcher = null;
            }
            if ( _lastDisplayedFeed != null && _lastDisplayedFeed.HasProp( Props.MarkReadOnLeave ) )
            {
                MarkAsReadAction.DoMarkAsRead( _lastDisplayedFeed.ToResourceList() );
            }
        }

        private void HandleDisplayedFeedChanged( object sender, ResourcePropIndexEventArgs e )
        {
            if ( e.ChangeSet.IsPropertyChanged( -Core.Props.Parent ) )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( RedisplaySelectedFeed ) );
            }
        }

        private void DisplayGroupItemList( IResource res )
        {
            bool displayUnread = res.HasProp( Core.Props.DisplayUnread );
            bool displayThreaded = false;
            IResourceList itemList = ItemsInGroupRecursive( res, !res.HasProp( Core.Props.DisplayNewspaper ),
                                                            ref displayThreaded );
            if ( itemList != null )
            {
                if ( displayUnread )
                {
                    itemList = GetUnreadResources( itemList );
                }

                string captionTemplate = "%OWNER%";
                if ( displayUnread )
                {
                    captionTemplate = "Unread Posts in " + captionTemplate;
                }

                DisplayRSSItemList( res, itemList, captionTemplate, displayThreaded );

                _lastDisplayedGroupWatcher = res.ToResourceListLive();
                _lastDisplayedGroupWatcher.ResourceChanged += HandleDisplayedFeedChanged;
                Core.ResourceBrowser.ContentChanged += HandleBrowserContentChanged;
            }
            else
            {
                Core.ResourceBrowser.DisplayResourceList( null, Core.ResourceStore.EmptyResourceList,
                                                          res.GetPropText( "Name" ), null, null );
            }
        }

        public static IResourceList ItemsInFeed( IResource res, bool includeComments, ref bool haveComments )
        {
            IResourceList result = res.GetLinksOfTypeLive( "RSSItem", Props.RSSItem );
            if ( includeComments )
            {
                // to avoid scanning all items, enable threading if only first item has comment URL
                if ( result.Count > 0 && result[ 0 ].GetPropText( Props.CommentRSS ).Length > 0 )
                {
                    haveComments = true;
                }

                IResourceList comments = res.GetLinksOfTypeLive( "RSSItem", Props.FeedComment );
                if ( comments.Count > 0 || haveComments )
                {
                    result = result.Union( comments );
                    haveComments = true;
                }
                else
                {
                    comments.Dispose();
                }
            }
            return result;
        }

        public static IResourceList ItemsInGroupRecursive( IResource res, bool includeComments,
                                                           ref bool haveComments )
        {
            IResourceList feedsInGroup = res.GetLinksTo( "RSSFeed", Core.Props.Parent );
            IResourceList itemList = null;
            foreach ( IResource feed in feedsInGroup )
            {
                itemList = ItemsInFeed( feed, includeComments, ref haveComments ).Union( itemList );
            }
            foreach ( IResource childGroup in res.GetLinksTo( "RSSFeedGroup", Core.Props.Parent ) )
            {
                IResourceList childList = ItemsInGroupRecursive( childGroup, includeComments, ref haveComments );
                if ( itemList == null )
                {
                    itemList = childList;
                }
                else
                {
                    itemList = itemList.Union( childList );
                }
            }
            if ( itemList == null )
            {
                return Core.ResourceStore.EmptyResourceList;
            }
            return itemList;
        }

        private static IResourceList GetUnreadResources( IResourceList resList )
        {
            return resList.Intersect(
                Core.ResourceStore.FindResourcesWithProp( SelectionType.LiveSnapshot, null, Core.Props.IsUnread ), true );
        }

        private void DisplayRSSItemList( IResource ownerResource, IResourceList itemList, string captionTemplate,
                                         bool displayThreaded )
        {
            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            options.CaptionTemplate = captionTemplate;
            options.SelectedResource = GetRememberedSelection( ownerResource );
            if ( displayThreaded )
            {
                options.ThreadingHandler = _commentThreadingHandler;
            }
            options.SortSettings = new SortSettings( Core.Props.Date, false );
            options.ShowNewspaper = ownerResource.HasProp( Core.Props.DisplayNewspaper );
            if ( ownerResource.HasProp( Core.Props.LastError ) )
            {
                options.StatusLine = ownerResource.GetStringProp( Core.Props.LastError );
            }

            if ( ownerResource.HasProp( Props.RSSSearchPhrase ) )
            {
                options.HighlightDataProvider = new HighlightDataProvider( ownerResource.GetPropText( Props.RSSSearchPhrase ) );
                options.SuppressContexts = true;
            }
            Core.ResourceBrowser.DisplayResourceList( ownerResource, itemList, options );
        }

        public bool CanRenameResource( IResource res )
        {
            // obsolete
            return false;
        }

        public bool ResourceRenamed( IResource res, string newName )
        {
            // obsolete
            return false;
        }

        public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
        {
            // obsolete
        }

        public bool CanDropResources( IResource targetResource, IResourceList dragResources )
        {
            // obsolete
            return false;
        }

        internal void RememberSelection( IResource feed, IResource item )
        {
            _selectionMap[ feed.Id ] = item.Id;
            if ( Settings.RememberSelection )
            {
                new ResourceProxy( feed ).SetPropAsync( Props.SelectedRSSItem, item );
            }
        }

        /**
         * Initiates an immediate update of the selected feed.
         */

        public void QueueFeedUpdate( IResource feed, JobPriority jobPriority )
        {
            _updateQueue.QueueFeedUpdate( feed, jobPriority );
        }
        public void QueueFeedUpdate( IResource feed )
        {
            QueueFeedUpdate( feed, JobPriority.Normal );
        }

        /**
         * Initiates an update of the selected feed at a time determined by its
         * last update time and update frequency.
         */

        public void ScheduleFeedUpdate( IResource feed )
        {
            _updateQueue.ScheduleFeedUpdate( feed );
        }

        private void _updateQueue_OnFeedUpdated( object sender, ResourceEventArgs e )
        {
            Core.UIManager.QueueUIJob( new ResourceDelegate( ProcessFeedUpdated ), e.Resource );
            if ( FeedUpdated != null )
            {
                FeedUpdated( this, new ResourceEventArgs( e.Resource ) );
            }
        }

        private void ProcessFeedUpdated( IResource res )
        {
            if ( Core.ResourceBrowser.OwnerResource == res )
            {
                if ( !Core.ResourceBrowser.IsThreaded && !Core.ResourceBrowser.NewspaperVisible )
                {
                    IResourceList result = res.GetLinksOfTypeLive( "RSSItem", Props.RSSItem );
                    if ( result.Count > 0 && result[ 0 ].GetPropText( Props.CommentRSS ).Length > 0 )
                    {
                        // update selection to ensure comments are shown (OM-10527)
                        RedisplaySelectedFeed();
                    }
                }
                string lastError = res.GetStringProp( Core.Props.LastError );
                if ( lastError == null )
                {
                    Core.ResourceBrowser.HideStatusLine();
                }
                else
                {
                    Core.ResourceBrowser.AddStatusLine( lastError, null );
                }
            }
        }

        private void RedisplaySelectedFeed()
        {
            _lastSelectedFeed = null;
            _rssTreePane.UpdateSelection();
        }

        public static int UpdatePeriodToIndex( string updatePeriod )
        {
            for ( int i = 0; i < _updatePeriods.Length; i++ )
            {
                if ( String.Compare( _updatePeriods[ i ], updatePeriod, true, CultureInfo.InvariantCulture ) == 0 )
                {
                    return i;
                }
            }
            return 1;
        }

        public static string IndexToUpdatePeriod( int index )
        {
            return _updatePeriods[ index ];
        }

        private static readonly string[] _updatePeriods = new string[] {"minutely", "hourly", "daily", "weekly"};

        public static bool IsFeedsOrGroups( IResourceList resources )
        {
            string[] allTypes = resources.GetAllTypes();
            return ( allTypes.Length == 1 && ( allTypes[ 0 ] == "RSSFeed" || allTypes[ 0 ] == "RSSFeedGroup" ) ) ||
                ( allTypes.Length == 2 && allTypes[ 0 ] == "RSSFeed" && allTypes[ 1 ] == "RSSFeedGroup" );
        }

        public static bool IsFeedOrGroup( IResource resource )
        {
            return resource != null && ( resource.Type == "RSSFeed" || resource.Type == "RSSFeedGroup" );
        }

        internal static bool HasComments( IResource rssItem )
        {
            if ( rssItem.HasProp( Props.CommentCount ) &&
                rssItem.GetIntProp( Props.CommentCount ) == 0 )
            {
                return false;
            }
            string commentRss = rssItem.GetPropText( Props.CommentRSS );
            if ( commentRss.Length == 0 )
            {
                return false;
            }
            return true;
        }

        public IResource FindOrCreateGroup( string name, IResource parent )
        {
            if ( parent == null )
            {
                parent = _feedRoot;
            }
            foreach ( IResource group in parent.GetLinksTo( "RSSFeedGroup", Core.Props.Parent ) )
            {
                if ( String.Compare( group.DisplayName, name, true ) == 0 )
                {
                    return group;
                }
            }

            ResourceProxy groupProxy = ResourceProxy.BeginNewResource( "RSSFeedGroup" );
            groupProxy.SetProp( Core.Props.Name, name );
            groupProxy.AddLink( Core.Props.Parent, parent );
            groupProxy.EndUpdate();
            return groupProxy.Resource;
        }

        public IResource CreateFeed( string name, string url, IResource parent )
        {
            return CreateFeed( name, url, parent, null, null );
        }

        public IResource CreateFeed( string name, string url, IResource parent, string httpLogin, string httpPassword )
        {
            ResourceProxy newFeedProxy = ResourceProxy.BeginNewResource( "RSSFeed" );
            newFeedProxy.SetProp( Core.Props.Name, name );
            newFeedProxy.SetProp( Props.URL, url );
            if ( parent != null )
            {
                newFeedProxy.AddLink( Core.Props.Parent, parent );
            }
            else
            {
                newFeedProxy.AddLink( Core.Props.Parent, _feedRoot );
            }
            if ( httpLogin != null )
            {
                newFeedProxy.SetProp( Props.HttpUserName, httpLogin );
                newFeedProxy.SetProp( Props.HttpPassword, httpPassword );
            }
            newFeedProxy.EndUpdate();
            return newFeedProxy.Resource;
        }

        public void ImportOpmlStream( Stream importStream, IResource importRoot,
                                      string importFileName, bool importPreview )
        {
            if ( importStream == null )
            {
                throw new ArgumentNullException( "importStream" );
            }
            if ( importRoot == null )
            {
                importRoot = RootFeedGroup;
            }

            ImportFeedsOperation importOperation = new ImportFeedsOperation( importStream, importRoot, importFileName, importPreview );
            if ( Core.ResourceStore.IsOwnerThread() )
            {
                importOperation.ExecuteOperation();
            }
            else
            {
                importOperation.Enqueue();
            }
        }

        public void ExportOpmlFile( IResource exportRoot, string exportFileName )
        {
            ExportOpmlFileImpl( exportRoot, exportFileName );
        }

        private static void ExportOpmlFileImpl( IResource exportRoot, string exportFileName )
        {
            if ( exportRoot == null )
            {
                exportRoot = RootFeedGroup;
            }
            OPMLProcessor.Export( exportRoot, exportFileName );
        }

        public void ShowAddFeedWizard( string defaultUrl, IResource defaultGroup )
        {
            SubscribeForm form = new SubscribeForm();
            form.ShowAddFeedWizard( defaultUrl, defaultGroup );
            form.Activate();
        }

        public void RegisterChannelElementParser( FeedType feedType, string xmlNameSpace, string elementName,
                                                  IFeedElementParser parser )
        {
            RSSParser.RegisterChannelElementParser( feedType, xmlNameSpace, elementName, parser );
        }

        public void RegisterItemElementParser( FeedType feedType, string xmlNameSpace, string elementName,
                                               IFeedElementParser parser )
        {
            RSSParser.RegisterItemElementParser( feedType, xmlNameSpace, elementName, parser );
        }

        /// <summary>
        /// Register new subscription importer. Importer will be available to user in startup wizard
        /// and options pane.
        /// </summary>
        /// <param name="name">the name of importer, will be shown to user.</param>
        /// <param name="importer">The importer instance.</param>
        public void RegisterFeedImporter( string name, IFeedImporter importer )
        {
            _feedImporters[ name ] = importer;
        }

        public void RemoteSubscribeToFeed( string url )
        {
            if ( url.StartsWith( "//" ) )
            {
                url = url.Substring( 2 );
            }
            Core.UIManager.QueueUIJob( new ShowSubscribeWizardDelegate( ShowAddFeedWizard ),
                                       url, RootFeedGroup );
        }

        private delegate void ShowSubscribeWizardDelegate( string url, IResource defaultGroup );

        private delegate void RemoteSubscribeToFeedDelegate( string url );

        private static string HandleToolTipCallback( IResource res )
        {
            if( Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.ShowPlaneList, false ) )
            {
                DateTime time = LastPostComparer.CalcTime( res );
                return "Last updated: " + time.ToShortDateString();
            }
            else
            {
                return res.GetStringProp( Core.Props.LastError );
            }
        }

		/// <summary>
		/// The queue of feeds-to-update has gotten empty, and all the pending updates are thru.
		/// Exit the “Is Updating All” state.
		/// </summary>
		protected void OnFeedUpdateQueueGotEmpty(object sender, EventArgs e)
		{
			bool	bWasUpdatingAll;
			lock( this )
			{
				bWasUpdatingAll = _isUpdatingAll;
				_isUpdatingAll = false;
			}

			// Show the message and fire an event if the queue's gotten empty after updating all the feeds
			if(bWasUpdatingAll)
			{
				_statuswriter.ShowStatus( "Finished updating all feeds.", 10 );
				if( UpdateAllFinished != null )
				{
					try
					{
						UpdateAllFinished( this, EventArgs.Empty );
					}
					catch( Exception ex )

					{
						Core.ReportException( ex, ExceptionReportFlags.AttachLog );
					}
				}
			}
		}

		/// <summary>
		/// Fires the event that notifies that an update-all action has started.
		/// </summary>
		protected void FireUpdateAllStarted()
		{
			if( UpdateAllStarted != null )
			{
				try
				{
					UpdateAllStarted( this, EventArgs.Empty );
				}
				catch( Exception ex )

				{
					Core.ReportException( ex, ExceptionReportFlags.AttachLog );
				}
			}
		}

		/// <summary>
		/// Tries to set the <see cref="IsDoingUpdateAll" /> flag and reports whether it was successful.
		/// Succeeds only if update-all is not already running.
		/// </summary>
		/// <returns></returns>
		public bool TrySetIsDoingUpdateAll()
		{
			lock(this)
			{
				if(_isUpdatingAll)
				{
					_statuswriter.ShowStatus( "“Update All Feeds” is already running.", 5 );
					return false;
				}
				Core.UserInterfaceAP.QueueJob( "Started Updating All Feeds.", new MethodInvoker(FireUpdateAllStarted) );
				_statuswriter.ShowStatus( "Updating all feeds…", 1 );
				return _isUpdatingAll = true;
			}
		}

		public delegate void StringDelegate(string param);

		/// <summary>
		/// Gets whether the “Update All Feeds” action is currently updating any of the feeds.
		/// </summary>
		public bool IsDoingUpdateAll
		{
			get { return _isUpdatingAll;}
		}

    	/// <summary><seealso cref="IsDoingUpdateAll"/><seealso cref="UpdateAllStarted"/><seealso cref="UpdateAllFinished"/>
    	/// Attempts to start updating all the feeds.
    	/// </summary>
    	/// <returns>Whether an update-all was initiated successfully.</returns>
    	/// <remarks><para>The attempt succeeds if an “update all feeds” process is not currently running. In this case the function returns <c>True</c>.</para>
    	/// <para>Otherwise, all the feeds are not queued for update again and <c>False</c> is returned.</para></remarks>
    	public bool UpdateAll()
    	{
			// Check if we're allowed to update-all at the moment
			// (we're not if an update-all is already in progress)
			if(!TrySetIsDoingUpdateAll())
				return false;	// Cannot

			// Get the list of feeds and queue the update
			IResourceList	resFeeds = GetAllFeeds().Minus( Core.ResourceStore.FindResourcesWithProp( null, Props.IsPaused ));
			foreach(IResource res in resFeeds)
				QueueFeedUpdate( res);

			// TODO: should we update feeds if the “Update Every” is not checked?..
			// res.GetIntProp( Props.UpdateFrequency ) >= 0
    		
			return true;
    	}

        internal static void SaveSubscription()
        {
            string dbPath = RegUtil.DatabasePath;
            if( dbPath == null )
                dbPath = Application.StartupPath;

            string fileName = Path.Combine( dbPath, ".Subscription.opml.sav" );
            ExportOpmlFileImpl( RootFeedGroup, fileName );
        }
    }

    /// <summary>
    /// Links pane filter which hides the From link if it has the same value as the Weblog link.
    /// </summary>
    internal class WeblogFromFilter : ILinksPaneFilter
    {
        public bool AcceptLinkType( IResource displayedResource, int propId, ref string displayName )
        {
            return true;
        }

        public bool AcceptLink( IResource displayedResource, int propId, IResource targetResource,
                                ref string linkTooltip )
        {
            if ( propId == Core.ContactManager.Props.LinkFrom )
            {
                IResource weblog = displayedResource.GetLinkProp( -Props.RSSItem );
                if ( weblog == targetResource )
                {
                    return false;
                }
            }
            return true;
        }

        public bool AcceptAction( IResource displayedResource, IAction action )
        {
            return true;
        }
    }

    internal class RSSFeedIconProvider : IResourceIconProvider, IOverlayIconProvider
    {
    	private static Icon[] _updating;
    	private static Icon[] _error;
    	private static Icon[] _paused;
    	private static Icon _default;
    	private readonly FavIconManager _favIconManager;

    	public RSSFeedIconProvider()
    	{
    		_favIconManager = (FavIconManager)Core.GetComponentImplementation( typeof(FavIconManager) );
    	}

    	public Icon GetResourceIcon( IResource resource )
    	{
    		// Try to get the feed's favicon
    		Icon favicon = TryGetResourceIcon( resource );
    		if( favicon != null )
    			return favicon;

    		// No favicon available, return resource-type's default icon
    		if( _default == null )
    		{ // Delay-load the default icon
    			_default = RSSPlugin.LoadIconFromAssembly( "RSSFeed.ico" );
    		}
    		return _default;
    	}

    	/// <summary>
    	/// Almost the same as <see cref="IResourceIconProvider.GetResourceIcon"/>, but returns <c>Null</c> in case a customized favicon
    	/// is not available for this feed (in which case the <see cref="GetResourceIcon"/> returns the default resource type icon).
    	/// </summary>
    	public Icon TryGetResourceIcon( IResource resource )
    	{
    		string feedUrl = resource.GetStringProp( Props.URL );
    		return Utils.IsValidString( feedUrl ) ? _favIconManager.GetResourceFavIcon( feedUrl ) : null;
    	}

    	public Icon GetDefaultIcon( string resType )
    	{
    		return null;
    	}

    	#region IOverlayIconProvider Members

    	public Icon[] GetOverlayIcons( IResource resource )
    	{
    		string updateStatus = resource.GetStringProp( Props.UpdateStatus );
    		if( updateStatus == "(updating)" )
    		{
    			if( _updating == null )
    			{
    				_updating = new Icon[1];
    				_updating[ 0 ] = RSSPlugin.LoadIconFromAssembly( "updating.ico" );
    			}
    			return _updating;
    		}
    		if( updateStatus == "(error)" )
    		{
    			if( _error == null )
    			{
    				_error = new Icon[1];
    				_error[ 0 ] = RSSPlugin.LoadIconFromAssembly( "error.ico" );
    			}
    			return _error;
    		}
    		if( resource.HasProp( Props.IsPaused ) )
    		{
    			if( _paused == null )
    			{
    				_paused = new Icon[1];
    				_paused[ 0 ] = RSSPlugin.LoadIconFromAssembly( "RSSFeedPaused.ico" );
    			}
    			return _paused;
    		}
    		return null;
    	}

    	#endregion
    }

	#region RSSItemIconProvider Class — Provides the Feed Item icons

	/// <summary>
	/// Provides an icon for the RSS Items, so that the icon corresponds to the icon of the parent feed.
	/// </summary>
	internal class RSSItemIconProvider : IResourceIconProvider
	{
		/// <summary>
		/// An icon provider that gives an icon for the RSS Feed resources.
		/// </summary>
		private readonly RSSFeedIconProvider _feedprovider = null;

		/// <summary>
		/// A default icon for the RSS items.
		/// </summary>
		private Icon _iconDefault = null;

		/// <summary>
		/// A default icon for the unread RSS items.
		/// </summary>
		private Icon _iconDefaultUnread = null;

		/// <summary>
		/// Specifies whether the feed item should display the same favicon as the feed does.
		/// This variable caches the corresponding value from Omea Settings.
		/// </summary>
		private bool _bUseFeedIcon = true;

		/// <summary>
		/// Constructs the object.
		/// </summary>
		/// <param name="feedprovider">Icon provider for the feed, which has a favicon as a resource icon.
		/// If the corresponding option is enabled, the feed icon is propagated to the feed item icon.</param>
		public RSSItemIconProvider( RSSFeedIconProvider feedprovider )
		{
			_feedprovider = feedprovider;

			// Read the settings and listen to changes in them
			Core.UIManager.AddOptionsChangesListener( "Internet", "Feeds", OnSettingsChanged );
			OnSettingsChanged( null, EventArgs.Empty );
		}

		#region IResourceIconProvider Members

		public Icon GetResourceIcon( IResource resItem )
		{
			if(_bUseFeedIcon)
			{	// Return the feed's favicon for the feed item (if available)
				IResourceList parents = resItem.GetLinksTo( "RSSFeed", "RSSItem" );
				lock(parents)
				{
					if( parents.Count != 0 )
					{
						IResource feed = parents[ 0 ];
						Icon	favicon = _feedprovider.TryGetResourceIcon( feed );
						if(favicon != null)
							return favicon;
					}
					else
						Trace.WriteLine( "Warning: parentless RSS item enountered, cannot get feed icon for it." );
				}
			}

			// Don't use the feed's favicon (either if option disabled or if the feed does not have a favicon)
			return resItem.HasProp( Core.Props.IsUnread ) ? DefaultUnread : Default;
		}

		public Icon GetDefaultIcon( string resType )
		{
			return Default;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the default feed item icon (for the read resource).
		/// </summary>
		public Icon Default
		{
			get
			{
                if (_iconDefault == null)
                    _iconDefault = RSSPlugin.LoadIconFromAssembly( "RSSItem.ico" );
				return _iconDefault;
			}
		}

		/// <summary>
		/// Gets the default unread feed item icon.
		/// </summary>
		public Icon DefaultUnread
		{
			get
			{
				if( _iconDefaultUnread == null )
					_iconDefaultUnread = RSSPlugin.LoadIconFromAssembly( "RSSItemUnread.ico" );
				return _iconDefaultUnread;
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Settings have changed, re-query for them.
		/// </summary>
		protected void OnSettingsChanged( object sender, EventArgs args )
		{
			_bUseFeedIcon = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.PropagateFavIconToItems, _bUseFeedIcon );
		}

		#endregion
	}

	#endregion

	internal class RSSItemDeleter : DefaultResourceDeleter
    {
        public override void DeleteResource( IResource res )
        {
            base.DeleteResource( res );
            IResourceList comments = res.GetLinksTo( "RSSItem", Props.ItemComment );
            foreach( IResource comment in comments.ValidResources )
            {
                DeleteResource( comment );
            }
        }

        public override void DeleteResourcePermanent( IResource res )
        {
            IResourceList feedList = res.GetLinksTo( null, "RSSItem" );
            if ( feedList.Count > 0 )
            {
                IResource feed = feedList[ 0 ];
                IStringList delItemList = feed.GetStringListProp( Props.DeletedItemHashList );
                delItemList.Add( RSSParser.GetRSSItemMD5( res ) );
            }

            IResource commentFeed = res.GetLinkProp( -Props.ItemCommentFeed );
            if ( commentFeed != null )
            {
                RemoveFeedsAndGroupsAction.DeleteFeedsAndGroups( commentFeed.ToResourceList() );
            }
            res.Delete();
        }
    }

    public class WebPost
    {
        private static void AddItem( XmlTextWriter xmlWriter, string tag, string value )
        {
            xmlWriter.WriteStartElement( tag );
            xmlWriter.WriteString( value );
            xmlWriter.WriteEndElement();
        }
        public static void PostNewComment( string url, string title, string author, string link, string body )
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create( url );
            req.Method = "POST";
            req.ContentType = "text/xml";
            req.UserAgent = HttpReader.UserAgent;

            StringBuilder xml = new StringBuilder();
            XmlTextWriter xmlWriter = new XmlTextWriter( new StringWriter( xml ) );
            try
            {
                xmlWriter.WriteStartElement( "item" );
                AddItem( xmlWriter, "title", title );
                AddItem( xmlWriter, "author", author );
                AddItem( xmlWriter, "link", link );
                AddItem( xmlWriter, "description", body );
                xmlWriter.WriteEndElement();
                xmlWriter.Flush();
            }
            finally
            {
                xmlWriter.Close();
            }

            byte[] utf8Bytes = Encoding.UTF8.GetBytes( xml.ToString() );
            req.ContentLength = utf8Bytes.Length;
            try
            {
                Stream stream = req.GetRequestStream();
                stream.Write( utf8Bytes, 0, utf8Bytes.Length );
                stream.Close();
            }
            catch ( WebException )
            {}
        }
    }

    internal class FeedUnreadsFilter : IResourceNodeFilter
    {
        private bool        _hidden = false;
        private Hashtable   _feedStati = new Hashtable();

        public bool  Hide
        {
            set
            {
                _hidden = value;
                _feedStati.Clear();
            }
        }
        public bool  AcceptNode( IResource node, int level )
        {
            bool  accept = true;
            if( _hidden )
            {
                if( _feedStati.ContainsKey( node.Id ))
                {
                    accept = (bool) _feedStati[ node.Id ];
                }
                else
                    if( node.Type == Props.RSSFeedResource )
                {
                    IResourceList unreads = Core.ResourceStore.FindResourcesWithProp( Props.RSSItemResource, Core.Props.IsUnread );
                    accept = node.GetLinksOfType( Props.RSSItemResource, Props.RSSItem ).Intersect( unreads ).Count > 0;
                    _feedStati[ node.Id ] = accept;
                }
                else
                {
                    IResourceList children = node.GetLinksTo( null, Core.Props.Parent );
                    foreach( IResource child in children )
                        accept = accept || AcceptNode( child, level + 1 );
                }
            }
            return accept;
        }
    }

    internal class ErrorFeedFilter : IResourceNodeFilter
    {
        private bool        _hidden = false;
        private Hashtable   _feedStati = new Hashtable();

        public bool  Hide
        {
            set
            {
                _hidden = value;
                _feedStati.Clear();
            }
        }
        public bool  AcceptNode( IResource node, int level )
        {
            bool  accept = true;
            if( _hidden )
            {
                if( _feedStati.ContainsKey( node.Id ))
                {
                    accept = (bool) _feedStati[ node.Id ];
                }
                else
                if( node.Type == Props.RSSFeedResource )
                {
                    _feedStati[ node.Id ] = accept = node.HasProp( Core.Props.LastError );
                }
                else
                {
                    IResourceList children = node.GetLinksTo( null, Core.Props.Parent );
                    foreach( IResource child in children )
                        accept = accept || AcceptNode( child, level + 1 );
                }
            }
            return accept;
        }
    }

    internal class RSSRenameHandler : IResourceRenameHandler
    {
        public bool CanRenameResource( IResource res, ref string editText )
        {
            if ( res.Type == Props.RSSFeedResource || res.Type == Props.RSSFeedGroupResource )
            {
                editText = res.GetPropText( Core.Props.Name );
                return true;
            }
            return false;
        }

        public bool ResourceRenamed( IResource res, string newName )
        {
            if ( newName == "" )
            {
                MessageBox.Show( Core.MainWindow, "Please specify a name." );
                return false;
            }

            ResourceProxy proxy = new ResourceProxy( res );
            proxy.AsyncPriority = JobPriority.Immediate;
            proxy.SetPropAsync( Core.Props.Name, newName );
            return true;
        }
    }

    internal class EnclosureDownloadStateColumn : ImageListColumn
    {
    	/// <summary>
    	/// Singleton instance.
    	/// </summary>
    	private static EnclosureDownloadStateColumn _instance = null; // Do not initialize here, so that .cctor won't create an object
		/// <summary>
		/// Gets the singleton instance.
		/// </summary>
    	public static EnclosureDownloadStateColumn Instance
    	{
    		get
    		{
    			if( _instance == null )
    				_instance = new EnclosureDownloadStateColumn();
    			return _instance;
    		}
    	}

    	private EnclosureDownloadStateColumn()
    		: base( Props.EnclosureDownloadingState )
    	{
    	}

    	public static void Register()
    	{
    		EnclosureDownloadStateColumn instance = Instance;

    		// Add enclosure state icons
    		for( int a = (int)EnclosureDownloadState.MinValue; a < (int)EnclosureDownloadState.MaxValue; a++ )
    			instance.AddIconValue( EnclosureDownloadManager.GetEnclosureStateIcon( (EnclosureDownloadState)a ), a );

    		instance.SetHeaderIcon( RSSPlugin.LoadIconFromAssembly( "downloadColumn.ico" ) );
    		instance.ShowTooltips = true;
    		Core.DisplayColumnManager.RegisterCustomColumn( Props.EnclosureDownloadingState, instance );

    		IResourceList list =
    			Core.ResourceStore.FindResources( "RSSItem", Props.EnclosureSize, -1 );
    		foreach( IResource resource in list.ValidResources )
    		{
    			resource.SetProp( Props.EnclosureSize, 0 );
    		}
    	}

    	public override string GetTooltip( IResource res )
    	{
    		if( res.HasProp( Props.EnclosureDownloadingState ) )
    		{
    			switch( res.GetIntProp( Props.EnclosureDownloadingState ) )
    			{
    			case DownloadState.NotDownloaded:
    				return "Not Downloaded";
    			case DownloadState.Planned:
    				return "Planned For Downloading";
    			case DownloadState.Completed:
    				return "Download Completed";
    			case DownloadState.Failed:
    				string tooltip = "Download Failed";
    				if( res.HasProp( Props.EnclosureFailureReason ) )
    				{
    					tooltip += ": " + res.GetPropText( Props.EnclosureFailureReason );
    				}
    				return tooltip;
    			case DownloadState.InProgress:
    				float size = res.GetIntProp( Props.EnclosureSize );
    				float downloaded = res.GetIntProp( Props.EnclosureDownloadedSize );
    				if( size > 0.0 && downloaded > 0.0 )
    				{
    					int percent = (int)((100.0 * downloaded) / size);
    					if( percent > 100 )
    					{
    						percent = 100;
    					}
    					return "Downloaded " + percent + "%";
    				}
    				return "Download In Progress";
    			}
    		}
    		return string.Empty;
    	}

    	public override void MouseClicked( IResource res, Point pt )
    	{
    		if( res.HasProp( Props.EnclosureDownloadingState ) )
    		{
    			switch( res.GetIntProp( Props.EnclosureDownloadingState ) )
    			{
    			case DownloadState.Planned:
    				new ResourceProxy( res ).SetProp( Props.EnclosureDownloadingState, DownloadState.NotDownloaded );
    				break;
    			case DownloadState.NotDownloaded:
    				EnclosureDownloadManager.PlanToDownload( res );
    				break;
    			case DownloadState.Failed:
    				EnclosureDownloadManager.PlanToDownload( res );
    				break;
    			}
    		}
    	}
    }

	internal class CommentThreadingHandler : DefaultThreadingHandler
    {
        public CommentThreadingHandler() :
            base( Props.ItemComment )
        {}

        public override bool CanExpandThread( IResource res, ThreadExpandReason reason )
        {
            if ( reason == ThreadExpandReason.Enumerate )
            {
                return base.CanExpandThread( res, reason );
            }
            return base.CanExpandThread( res, reason ) || RSSPlugin.HasComments( res );
        }

        public override bool HandleThreadExpand( IResource res, ThreadExpandReason reason )
        {
            if ( reason == ThreadExpandReason.Expand )
            {
                // if feed update was started during last run of Omea, but never completed,
                // we need to retry it now
                if ( RSSPlugin.HasComments( res ) &&
                    ( !res.HasProp( -Props.ItemCommentFeed ) || ReadCommentsAction.FindDownloadingCommentsItem( res ) != null ) )
                {
                    ReadCommentsAction.DownloadComments( res );
                }
                return true;
            }
            return false;
        }
    }

    public class HighlightDataProvider : IHighlightDataProvider
    {
        private readonly WordPtr[] _words;

        public HighlightDataProvider( string phrase )
        {
            Guard.EmptyStringArgument( phrase, "phrase" );
            string[] strWords = phrase.Split( ' ' );
            if ( strWords != null && strWords.Length > 0 )
            {
                _words = new WordPtr[strWords.Length];
                for ( int i = 0; i < strWords.Length; ++i )
                {
                    _words[i].Section = DocumentSection.BodySection;
                    _words[i].StartOffset = 0;
                    _words[i].Original = strWords[i];
                    _words[i].Text = strWords[i];
                }
            }
        }

        public bool GetHighlightData( IResource res, out WordPtr[] words )
        {
            Guard.NullArgument( res, "res" );
            words = _words;
            return _words != null;
        }

        public void RequestContexts( int[] resourceIDs )
        {
        }

        public string GetContext( IResource res )
        {
            return null;
        }

        public OffsetData[] GetContextHighlightData( IResource res )
        {
            return null;
        }
    }

    public class DateIndexComparer: IResourceComparer
    {
        public int CompareResources( IResource r1, IResource r2 )
        {
            int result = r1.GetDateProp( Core.Props.Date ).CompareTo( r2.GetDateProp( Core.Props.Date ) );
            if ( result == 0 )
            {
                // assume the items in the feed go in the same order as in main page =>
                // newest items on top => smaller feed index means later date
                result = r2.GetIntProp( Props.IndexInFeed ) - r1.GetIntProp( Props.IndexInFeed );
            }
            return result;
        }
    }
}
 