// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
    /// <summary>
    /// Summary description for Bloglines.
    /// </summary>
    internal class RssBanditImporter : IFeedImporter, IFeedElementParser
    {
        private const string _progressMessage = "Importing RSS Bandit subscriptions";
        private const string _progressMessageCache = "Importing RSS Bandit cache";
        private const string _rbNS = "http://www.25hoursaday.com/2003/RSSBandit/feeds/";

        private string _subscriptionPath = null;
        private string _flaggedPath = null;
        private string _cachePath = null;

        private bool _parseCache = true;

        private struct FeedInfo
        {
            internal string url;
            internal string cacheFile;
            internal ArrayList readItems;
            internal IResource feed;
        }

        private ArrayList _importedFeeds = null;
        private string    _currentURL    = null;
        private IResource _currentFlag   = null;

        private Hashtable _flagsMap = new Hashtable();
        private IResource _defaultFlag = null;
        private RSSPlugin _plugin = RSSPlugin.GetInstance();

        public RssBanditImporter()
        {
            bool RssBanditFound = false;
            string basePath = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
            string banditPath = Path.Combine( basePath, @"RssBandit" );

            _subscriptionPath = Path.Combine( banditPath, "subscriptions.xml" );
            if( File.Exists( _subscriptionPath ) )
            {
                RssBanditFound = true;
            }
            if( ! RssBanditFound )
            {
                _subscriptionPath = Path.Combine( banditPath, "feedlist.xml" );
                if( File.Exists( _subscriptionPath ) )
                {
                    RssBanditFound = true;
                }
            }
            if( ! RssBanditFound )
            {
                // don't build additional data structures
                return;
            }

            _plugin.RegisterFeedImporter( "RSS Bandit", this );
            // Calculate cache dir
            _cachePath = Path.Combine( banditPath , "Cache" );
            // Try find flagged feed
            _flaggedPath = Path.Combine( banditPath, "flagitems.xml" );
            if( ! File.Exists( _flaggedPath ) )
            {
                // No stream with flags, Ok.
                _flaggedPath = null;
            }
            // Build flags hash
            _defaultFlag    = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", "RedFlag" );
            _flagsMap[ "FollowUp" ] = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", "RedFlag" );
            _flagsMap[ "Review" ]   = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", "YellowFlag" );
            _flagsMap[ "Read" ]     = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", "GreenFlag" );
            _flagsMap[ "Forward" ]  = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", "BlueFlag" );
            _flagsMap[ "Complete" ] = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", "CompletedFlag" );
            _flagsMap[ "Reply" ]    = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", "BlueFlag" );
        }

        /// <summary>
        /// Check if importer needs configuration before import starts.
        /// </summary>
        public bool HasSettings
        {
            get { return false; }
        }

        /// <summary>
        /// Returns creator of options pane.
        /// </summary>
        public OptionsPaneCreator GetSettingsPaneCreator()
        {
            return null;
        }

        /// <summary>
        /// Import subscription
        /// </summary>
        public void DoImport( IResource importRoot, bool addToWorkspace )
        {
            IResource feedRes = null;

            importRoot = _plugin.FindOrCreateGroup( "RssBandit subscriptions", importRoot );

            // We will add info about imported feeds here
            _importedFeeds = new ArrayList();

            ImportUtils.UpdateProgress( 0, _progressMessage );
            // Start to import feeds structure
            XmlDocument feedlist = new XmlDocument();
            try
            {
                feedlist.Load( _subscriptionPath );
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "RssBandit subscrption load failed: '" + ex.Message + "'" );
                RemoveFeedsAndGroupsAction.DeleteFeedGroup( importRoot );
                ImportUtils.ReportError( "RSS Bandit Subscription Import", "Import of RSS Bandit subscription failed:\n" + ex.Message );
                return;
            }

            ImportUtils.FeedUpdateData defaultUpdatePeriod;
            XmlAttribute period = feedlist.SelectSingleNode( "/feeds/@refresh-rate" ) as XmlAttribute;
            if( period != null  )
            {
                defaultUpdatePeriod = ImportUtils.ConvertUpdatePeriod( period.Value, 60000 );
            }
            else
            {
                defaultUpdatePeriod = ImportUtils.ConvertUpdatePeriod( "", 60000 );
            }

            XmlNodeList feeds = feedlist.GetElementsByTagName( "feed" );

            int totalFeeds = Math.Max( feeds.Count, 1 );
            int processedFeeds = 0;
            ImportUtils.UpdateProgress( processedFeeds / totalFeeds, _progressMessage );
            foreach( XmlElement feed in feeds )
            {
                string s = ImportUtils.GetUniqueChildText( feed, "link" );

                if( s == null )
                {
                    continue;
                }
                // May be, we are already subscribed?
                if( Core.ResourceStore.FindUniqueResource( "RSSFeed", "URL", s ) != null )
                {
                    continue;
                }

                FeedInfo info = new FeedInfo();
                info.url = s;

                IResource group = AddCategory( importRoot, feed.GetAttribute( "category" ) );
                // Ok, now we should create feed
                feedRes = Core.ResourceStore.NewResource( "RSSFeed" );
                feedRes.BeginUpdate();
                feedRes.SetProp( "URL", s );

                s = ImportUtils.GetUniqueChildText( feed, "title" );
                ImportUtils.Child2Prop( feed, "title", feedRes, Core.Props.Name, Props.OriginalName );

                ImportUtils.Child2Prop( feed, "etag", feedRes, Props.ETag );

                s = ImportUtils.GetUniqueChildText( feed, "last-retrieved" );
                if( s != null )
                {
                    DateTime dt = DateTime.Parse( s );
                    feedRes.SetProp( "LastUpdateTime",  dt );
                }

                // Peridoically
                ImportUtils.FeedUpdateData upd;
                s = ImportUtils.GetUniqueChildText( feed, "refresh-rate" );
                if( s != null )
                {
                    upd = ImportUtils.ConvertUpdatePeriod( s, 60000 );
                }
                else
                {
                    upd = defaultUpdatePeriod;
                }
                feedRes.SetProp( "UpdatePeriod",    upd.period );
                feedRes.SetProp( "UpdateFrequency", upd.freq );

                // Cached?
                s = ImportUtils.GetUniqueChildText( feed, "cacheurl" );
                if( s != null )
                {
                    info.cacheFile = s;
                }
                else
                {
                    info.cacheFile = null;
                }

                // Login & Password
                ImportUtils.Child2Prop( feed, "auth-user", feedRes, Props.HttpUserName );
                s = ImportUtils.GetUniqueChildText( feed, "auth-password" );
                if( s != null )
                {
                    feedRes.SetProp( Props.HttpPassword, DecryptPassword( s ) );
                }

                // Enclosures
                ImportUtils.Child2Prop( feed, "enclosure-folder", feedRes, Props.EnclosurePath );

                // Try to load "read" list
                XmlElement read = ImportUtils.GetUniqueChild( feed, "stories-recently-viewed" );
                if( read != null )
                {
                    ArrayList list = new ArrayList();
                    foreach( XmlElement story in read.GetElementsByTagName( "story" ) )
                    {
                        list.Add( story.InnerText );
                    }
                    if( list.Count > 0 )
                    {
                        info.readItems = list;
                    }
                    else
                    {
                        info.readItems = null;
                    }
                }
                // Feed is ready
                feedRes.AddLink( Core.Props.Parent, group );
                feedRes.EndUpdate();
                info.feed = feedRes;
                _importedFeeds.Add( info );
                if( addToWorkspace )
                {
                    Core.WorkspaceManager.AddToActiveWorkspace( feedRes );
                }

                processedFeeds += 100;
                ImportUtils.UpdateProgress( processedFeeds / totalFeeds, _progressMessage );
            }
            return;
        }

        /// <summary>
        /// Import cached items, flags, etc.
        /// </summary>
        public void DoImportCache()
        {
            Stream stream = null;
            RSSParser parser = null;

            // Register our item processor
            _plugin.RegisterItemElementParser( FeedType.Rss,  _rbNS, "flag-status", this );
            _plugin.RegisterItemElementParser( FeedType.Atom, _rbNS, "flag-status", this );

            ImportUtils.UpdateProgress( 0, _progressMessageCache );
            int totalFeeds = Math.Max( _importedFeeds.Count, 1 );
            int processedFeeds = 0;

            // Ok, all feeds are created. Try to import all caches and mark read items
            _parseCache = true;
            foreach( FeedInfo fi in _importedFeeds )
            {
                ImportUtils.UpdateProgress( processedFeeds / totalFeeds, _progressMessageCache );
                processedFeeds += 100;
                if( fi.feed.IsDeleted )
                {
                    continue;
                }
                if( null == fi.cacheFile )
                {
                    continue;
                }

                string path = Path.Combine( _cachePath, fi.cacheFile );
                if( ! File.Exists( path ) )
                {
                    continue;
                }
                try
                {
                    // Load feed!
                    parser = new RSSParser( fi.feed );
                    using( stream = new FileStream( path, FileMode.Open, FileAccess.Read ) )
                    {
                        parser.Parse( stream, Encoding.UTF8, true );
                    }
                }
                catch( Exception ex )
                {
                    Trace.WriteLine( "RSS Bandit cache '" + path + "' load failed: '" + ex.Message + "'" );
                }

                if( fi.readItems != null )
                {
                    // And mark as read
                    IResourceList allItems = fi.feed.GetLinksTo( "RSSItem", "From" );
                    foreach( string readOne in fi.readItems )
                    {
                        IResourceList markAsRead = Core.ResourceStore.FindResources( "RSSItem", "GUID", readOne ).Intersect( allItems, true );
                        foreach( IResource item in markAsRead )
                        {
                            item.DeleteProp( Core.Props.IsUnread );
                        }
                    }
                }
            }
            ImportUtils.UpdateProgress( processedFeeds / totalFeeds, _progressMessageCache );

            // And here we should import flags from flags stream
            if( _flaggedPath == null )
            {
                return;
            }

            // Register two additional elements
            _plugin.RegisterItemElementParser( FeedType.Rss,  _rbNS, "feed-url", this );
            _plugin.RegisterItemElementParser( FeedType.Atom, _rbNS, "feed-url", this );
            _parseCache = false;

            IResource pseudoFeed = Core.ResourceStore.NewResourceTransient( "RSSFeed" );
            try
            {
                parser = new RSSParser( pseudoFeed );
                parser.ItemParsed += this.FlaggedItemParsed;
                using( stream = new FileStream( _flaggedPath, FileMode.Open, FileAccess.Read ) )
                {
                    parser.Parse( stream, Encoding.UTF8, true );
                }
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "RssBandit flags load failed: '" + ex.Message + "'" );
            }
            pseudoFeed.Delete();
        }

        /// <summary>
        /// Parses the value for the element.
        /// </summary>
        /// <param name="resource">The resource in which the element data should be stored
        /// (of type RSSFeed for channel element parsers or RSSItem for item element
        /// parsers).</param>
        /// <param name="reader">The reader positioned after the starting tag of the element.</param>
        public void ParseValue( IResource resource, XmlReader reader )
        {
            if( _parseCache )
            {
                if( reader.LocalName == "flag-status" )
                {
                    string flagName = reader.ReadElementString();
                    IResource flag;
                    if( _flagsMap.ContainsKey( flagName ) )
                    {
                        flag = (IResource) _flagsMap[ flagName ];
                    }
                    else
                    {
                        flag = _defaultFlag;
                    }
                    resource.AddLink( "Flag", flag );
                }
            }
            else
            {
                if( reader.LocalName == "flag-status" )
                {
                    string flagName = reader.ReadElementString();
                    if( _flagsMap.ContainsKey( flagName ) )
                    {
                        _currentFlag =  (IResource) _flagsMap[ flagName ];
                    }
                    else
                    {
                        _currentFlag = _defaultFlag;
                    }
                }
                else if( reader.LocalName == "feed-url" )
                {
                    _currentURL = reader.ReadElementString();
                }
            }
        }

        /// <summary>
        /// Checks if a Read() call is needed to move to the next element after
        /// <see cref="ParseValue"/> is completed.
        /// </summary>
        public bool SkipNextRead
        {
            get { return true; }
        }

        private IResource AddCategory( IResource parent, string groupname )
        {
            string[] path = groupname.Split( '\\' );
            for( int i = 0; i < path.Length; ++i )
            {
                parent = _plugin.FindOrCreateGroup( path[i], parent );
            }
            return parent;
        }

        public void FlaggedItemParsed( object sender, ResourceEventArgs e )
        {
            if( _currentURL != null && _currentFlag != null )
            {
                // Search for feed
                IResource feed = null;
                foreach( FeedInfo fi in _importedFeeds )
                {
                    if( fi.url == _currentURL )
                    {
                        feed = fi.feed;
                        break;
                    }
                }
                if( feed != null )
                {
                    // IResource item = null; // Get item by feed and e.resource!
                    // item.AddLink( "Flag", _currentFlag );
                }
            }
            _currentURL = null;
            _currentFlag = null;
            // We don't need this one
            e.Resource.Delete();
        }

        private static byte[] CalcPasswordKey()
        {
            string salt = "NewsComponents.4711";
            byte[] b = Encoding.Unicode.GetBytes(salt);
            int bLen = b.GetLength(0);
            Random r = new Random(1500450271);
            byte[] res = new Byte[500];
            int i = 0;
            for( i = 0; i < bLen && i < 500; ++i )
            {
                res[i] = (byte)( b[i] ^ r.Next( 30, 127 ) );
            }
            while( i < 500 )
            {
                res[ i++ ] = (byte)r.Next( 30, 127 );
            }
            return new MD5CryptoServiceProvider().ComputeHash(res);
        }

        private static string DecryptPassword( string password )
        {
            byte[] base64;
            byte[] bytes;
            string ret;
            TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider();
            des.Key = CalcPasswordKey();
            des.Mode = CipherMode.ECB;
            try
            {
                base64 = Convert.FromBase64String( password );
                bytes = des.CreateDecryptor().TransformFinalBlock(base64, 0, base64.GetLength(0));
                ret = Encoding.Unicode.GetString(bytes);
            }
            catch
            {
                ret = String.Empty;
            }
            return ret;
        }
    }
}
