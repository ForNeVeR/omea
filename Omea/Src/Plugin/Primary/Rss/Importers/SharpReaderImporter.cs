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
    internal class SharpReaderImporter : IFeedImporter
    {
        private const string _progressMessage = "Importing SharpReader subscriptions";
        private const string _progressMessageCache = "Importing SharpReader cache";

        private static readonly string[] _flagsIDs = new string[]
            {
                "CompletedFlag", // 0 - BLACK !!
                "RedFlag",       // 1 - RED
                "GreenFlag",     // 2 - GREEN
                "PurpleFlag",    // 3 - PURPLE
                "BlueFlag",      // 4 - BLUE
                "RedFlag",       // 5 - PINK !!
                "RedFlag",       // 6 - DARK RED !!
                "BlueFlag",      // 7 - DARK BLUE !!
                "PurpleFlag",    // 8 - DARK PURPLE !!
                "OrangeFlag"     // 9 - ORANGE
        };

        private string _subscriptionPath = null;
        private string _cachePath = null;

        private struct FeedInfo
        {
            internal string url;
            internal string cacheFile;
            internal IResource feed;
        }

        private ArrayList _importedFeeds = null;

        private Hashtable _flagsMap = new Hashtable();
        private IResource _defaultFlag = null;
        private RSSPlugin _plugin = RSSPlugin.GetInstance();

        public SharpReaderImporter()
        {
            bool SharpReaderFound = false;
            string basePath = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
            string sharpPath = Path.Combine( basePath, @"SharpReader" );

            _subscriptionPath = Path.Combine( sharpPath, "subscriptions.xml" );
            if( File.Exists( _subscriptionPath ) )
            {
                SharpReaderFound = true;
            }

            if( ! SharpReaderFound )
            {
                // don't build additional data structures
                return;
            }

            _plugin.RegisterFeedImporter( "SharpReader", this );
            // Calculate cache dir
            _cachePath = Path.Combine( sharpPath , "Cache" );

            // Build flags hash
            _defaultFlag    = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", "RedFlag" );
            for( int i = 0; i < _flagsIDs.Length; ++i )
            {
                _flagsMap[ i.ToString() ] = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", _flagsIDs[i] );
            }
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
            importRoot = _plugin.FindOrCreateGroup( "SharpReader subscriptions", importRoot );
            // We will add info about imported feeds here
            _importedFeeds = new ArrayList();

            ImportUtils.UpdateProgress( 0, _progressMessage );
            // Start to import feeds structure
            XmlDocument feedlist = new XmlDocument();
            try
            {
                feedlist.Load( _subscriptionPath );
            }
            catch(Exception ex)
            {
                Trace.WriteLine( "SharpReader subscrption load failed: '" + ex.Message + "'" );
                RemoveFeedsAndGroupsAction.DeleteFeedGroup( importRoot );
                ImportUtils.ReportError( "SharpReader Subscription Import", "Import of SharpReader subscription failed:\n" + ex.Message );
                return;
            }

            ImportUtils.FeedUpdateData defaultUpdatePeriod;
            XmlAttribute period = feedlist.SelectSingleNode( "/feeds/@refreshMinutes" ) as XmlAttribute;
            if( period != null  )
            {
                defaultUpdatePeriod = ImportUtils.ConvertUpdatePeriod( period.Value, 1 );
            }
            else
            {
                defaultUpdatePeriod = ImportUtils.ConvertUpdatePeriod( "", 1 );
            }

            ImportUtils.UpdateProgress( 10, _progressMessage );
            XmlElement root = (XmlElement)feedlist.SelectSingleNode( "/feeds" );
            ImportGroup( root, importRoot, defaultUpdatePeriod, addToWorkspace );
            ImportUtils.UpdateProgress( 100, _progressMessage );
            return;
        }

        /// <summary>
        /// Import cached items, flags, etc.
        /// </summary>
        public void DoImportCache()
        {
            int totalFeeds = Math.Max( _importedFeeds.Count, 1 );
            int processedFeeds = 0;
            // Import cache
            foreach( FeedInfo fi in _importedFeeds )
            {
                ImportUtils.UpdateProgress( processedFeeds / totalFeeds, _progressMessageCache );
                processedFeeds += 100;
                if( fi.feed.IsDeleted )
                {
                    continue;
                }

                string path = Path.Combine( _cachePath, fi.cacheFile );
                if( ! File.Exists( path ) )
                {
                    continue;
                }
                XmlDocument feed = new XmlDocument();
                try
                {
                    feed.Load( path );
                }
                catch(Exception ex)
                {
                    Trace.WriteLine( "SharpReader cache '" + path + "' load failed: '" + ex.Message + "'" );
                    continue;
                }


                // Load info from feed
                string s = null;
                ImportUtils.InnerText2Prop( feed.SelectSingleNode( "/rss/Description" ) as XmlElement, fi.feed, Props.Description );
                ImportUtils.InnerText2Prop( feed.SelectSingleNode( "/rss/WebPageUrl" )  as XmlElement, fi.feed, Props.HomePage );

                ImportUtils.InnerText2Prop( feed.SelectSingleNode( "/rss/Image/Title" ) as XmlElement, fi.feed, Props.ImageTitle );
                ImportUtils.InnerText2Prop( feed.SelectSingleNode( "/rss/Image/Url" )   as XmlElement, fi.feed, Props.ImageURL );
                ImportUtils.InnerText2Prop( feed.SelectSingleNode( "/rss/Image/Link" )  as XmlElement, fi.feed, Props.ImageLink );

                // Load items from feed
                FeedAuthorParser authParser = new FeedAuthorParser();
                foreach( XmlElement item in feed.SelectNodes( "/rss/Items" ) )
                {
                    IResource feedItem = Core.ResourceStore.BeginNewResource( "RSSItem" );

                    ImportUtils.Child2Prop( item, "Title",            feedItem, Core.Props.Subject );
                    ImportUtils.Child2Prop( item, "Description",      feedItem, Core.Props.LongBody );
                    ImportUtils.Child2Prop( item, "Link",             feedItem, Props.Link );
                    ImportUtils.Child2Prop( item, "Guid",             feedItem, Props.GUID );
                    feedItem.SetProp( Core.Props.LongBodyIsHTML, true );
                    ImportUtils.Child2Prop( item, "CommentsUrl",      feedItem, Props.CommentURL );
                    ImportUtils.Child2Prop( item, "CommentsRss",      feedItem, Props.CommentRSS );
                    ImportUtils.Child2Prop( item, "Subject",          feedItem, Props.RSSCategory );
                    s = ImportUtils.GetUniqueChildText( item, "CommentCount" );
                    if( s != null )
                    {
                        try
                        {
                            feedItem.SetProp( Props.CommentCount,  Int32.Parse( s ) );
                        }
                        catch( FormatException )
                        {
                            Trace.WriteLine( "SharpReader cache: invalid comment-count" );
                        }
                        catch( OverflowException )
                        {
                            Trace.WriteLine( "SharpReader cache: invalid comment-count" );
                        }
                    }

                    // Date
                    s = ImportUtils.GetUniqueChildText( item, "PubDate" );
                    if( s != null )
                    {
                        DateTime dt = DateTime.Parse( s );
                        feedItem.SetProp( Core.Props.Date, dt );
                    }

                    // Read/unread
                    s = ImportUtils.GetUniqueChildText( item, "IsRead" );
                    if( s == null || s != "true" )
                    {
                        feedItem.SetProp( Core.Props.IsUnread, true );
                    }

                    // Flag
                    s = ImportUtils.GetUniqueChildText( item, "Flag" );
                    if( s != null )
                    {
                        IResource flag = null;
                        if( _flagsMap.ContainsKey( s ) )
                        {
                            flag = (IResource) _flagsMap[ s ];
                        }
                        else
                        {
                            flag = _defaultFlag;
                        }
                        feedItem.AddLink( "Flag", flag );
                    }
                    // Author
                    s = ImportUtils.GetUniqueChildText( item, "Author" );
                    if( s != null )
                    {
                        authParser.ParseAuthorString( feedItem, s );
                    }
                    else
                    {
                        feedItem.AddLink( Core.ContactManager.Props.LinkFrom, fi.feed );
                    }

                    feedItem.EndUpdate();
                    fi.feed.AddLink( Props.RSSItem, feedItem );
                }
            }
            ImportUtils.UpdateProgress( processedFeeds / totalFeeds, _progressMessageCache );
        }

        private void ImportGroup( XmlElement root, IResource rootGroup, ImportUtils.FeedUpdateData defaultUpdatePeriod, bool addToWorkspace )
        {

            // Import all groups
            XmlNodeList l = root.SelectNodes( "RssFeedsCategory" );
            if( l != null )
            {
                foreach( XmlElement group in l )
                {
                    string name = group.GetAttribute( "name" );
                    if( name == null )
                    {
                        name = "Unknown group";
                    }
                    IResource iGroup = _plugin.FindOrCreateGroup( name, rootGroup );
                    ImportGroup( group, iGroup, defaultUpdatePeriod, addToWorkspace );
                }
            }
            // Import all elements
            IResource feedRes = null;

            l = root.SelectNodes( "RssFeed" );
            if( l != null )
            {
                foreach( XmlElement feed in l )
                {
                    string s = feed.GetAttribute( "url" );

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

                    // Ok, now we should create feed
                    feedRes = Core.ResourceStore.NewResource( "RSSFeed" );
                    feedRes.BeginUpdate();

                    feedRes.SetProp( "URL", s );

                    ImportUtils.Attrib2Prop( feed, "name",         feedRes, Core.Props.Name, Props.OriginalName );
                    ImportUtils.Attrib2Prop( feed, "etag",         feedRes, Props.ETag );
                    ImportUtils.Attrib2Prop( feed, "authUserName", feedRes, Props.HttpUserName );
                    s = feed.GetAttribute( "authPassword" );
                    if( s != null )
                    {
                        string sharpReaderPassword = "VeRyToPsEcReTpAsSwOrDHuShHuShDoN'tTeLlAnYoNe";
                        MD5CryptoServiceProvider MD5 = new MD5CryptoServiceProvider();
                        TripleDESCryptoServiceProvider DES = new TripleDESCryptoServiceProvider();
                        DES.Key = MD5.ComputeHash( Encoding.ASCII.GetBytes( sharpReaderPassword ) );
                        DES.Mode = CipherMode.ECB;

                        byte[] DESed   = Convert.FromBase64String( s );
                        byte[] DeDESed = DES.CreateDecryptor().TransformFinalBlock( DESed, 0, DESed.Length);
                        // Huuray!
                        feedRes.SetProp( Props.HttpPassword, Encoding.Unicode.GetString( DeDESed ) );
                    }

                    s = feed.GetAttribute( "lastRefresh" );
                    if( s != null )
                    {
                        DateTime dt = DateTime.Parse( s );
                        feedRes.SetProp( Props.LastUpdateTime,  dt );
                    }

                    // Peridoically
                    ImportUtils.FeedUpdateData upd;
                    s = feed.GetAttribute( "refreshMinutes" );
                    if( s != null )
                    {
                        upd = ImportUtils.ConvertUpdatePeriod( s, 1 );
                    }
                    else
                    {
                        upd = defaultUpdatePeriod;
                    }
                    feedRes.SetProp( Props.UpdatePeriod,    upd.period );
                    feedRes.SetProp( Props.UpdateFrequency, upd.freq );

                    // Cached?
                    info.cacheFile = GetCacheNameByURL( info.url );

                    // Feed is ready
                    feedRes.AddLink( Core.Props.Parent, rootGroup );
                    feedRes.EndUpdate();

                    info.feed = feedRes;
                    _importedFeeds.Add( info );
                    if( addToWorkspace )
                    {
                        Core.WorkspaceManager.AddToActiveWorkspace( feedRes );
                    }
                }
            }
        }

        private string GetCacheNameByURL( string url )
        {
            string name = url;
            int pos = name.IndexOf("://");
            if( pos >= 0 )
            {
                name = name.Substring(  pos + 3 );
            }
            name = name.Replace('/', '-').Replace('\\', '-').Replace(':', '-')
                .Replace('?', '-').Replace('*', '-').Replace('"', '-')
                .Replace('<', '-').Replace('>', '-').Replace('|', '-');
            if( ! name.EndsWith(".xml") && ! name.EndsWith(".XML") )
            {
                name = name + ".xml";
            }
            if( name.Length > 128 )
            {
                name = md5_hex( url.ToLower() );
            }
            return name;
        }

        private readonly static char[] _hexDigits = new char[ 16 ] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
        private string md5_hex( string s )
        {
            byte[] md5 = new MD5CryptoServiceProvider().ComputeHash( Encoding.ASCII.GetBytes( s ) );
            StringBuilder res = new StringBuilder( md5.Length * 2 );
            for( int i = 0; i < md5.Length; ++i )
            {
                res.Append( _hexDigits[ md5[i] >> 4   ] );
                res.Append( _hexDigits[ md5[i] & 0x0F ] );
            }
            return res.ToString();
        }
    }
}
