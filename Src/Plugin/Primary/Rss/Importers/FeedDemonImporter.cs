// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using JetBrains.Omea.HTML;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Summary description for Bloglines.
	/// </summary>
	internal class FeedDemonImporter : IFeedImporter, IFeedElementParser
	{
	    private const string _progressMessage = "Importing FeedDemon subscriptions";
        private const string _progressMessageCache = "Importing FeedDemon cache";
        private const string _fdNS = "http://www.bradsoft.com/feeddemon/xmlns/1.0/";

        private string _channelsPath = null;
        private string _groupsPath = null;
        private IResource _flag = null;
        private ArrayList _readItems = null;
	    private Hashtable _name2url = new Hashtable();

	    public FeedDemonImporter()
		{
            bool FeedDemonFound = true;
            string basePath = Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );
            string daemonPath = Path.Combine( basePath, @"Bradsoft.com\FeedDemon\1.0" );

		    _channelsPath = Path.Combine( daemonPath, "Channels" );
		    _groupsPath = Path.Combine( daemonPath, "Groups" );

            FeedDemonFound = Directory.Exists( _channelsPath ) && Directory.Exists( _groupsPath );

            if( ! FeedDemonFound )
            {
                // don't build additional data structures
                return;
            }

            RSSPlugin.GetInstance().RegisterFeedImporter( "FeedDemon", this );
            _flag = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", "RedFlag" );
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
            RSSPlugin plugin = RSSPlugin.GetInstance();

            importRoot = plugin.FindOrCreateGroup( "FeedDemon subscriptions", importRoot );

            // Count full count of resources
	        string[] allFiles = Directory.GetFiles( _groupsPath, "*.opml" );

	        int totalFiles = Math.Max( allFiles.Length, 1 );
            int processedFiles = 0;

            ImportUtils.UpdateProgress( processedFiles / totalFiles, _progressMessage );
            foreach( string file in allFiles )
            {
                IResource group = null;
                string name = Path.GetFileNameWithoutExtension( file );
                group = plugin.FindOrCreateGroup( name, importRoot );

                try
                {
                    Hashtable ns = new Hashtable();
                    Stream stream = new FileStream( file, FileMode.Open, FileAccess.Read );

                    // Fix bugs in OPML
                    ns[ "fd" ] = _fdNS;
                    OPMLProcessor.Import( new StreamReader(stream), group, addToWorkspace, ns );
                }
                catch( Exception ex )
                {
                    RemoveFeedsAndGroupsAction.DeleteFeedGroup( group );
                    ImportUtils.ReportError( "FeedDemon Subscription Import", "Import of FeedDemon group '" + name + "' failed:\n" + ex.Message );
                }

                processedFiles += 100;
                ImportUtils.UpdateProgress( processedFiles / totalFiles, _progressMessage );
            }

	        // Read summary.xml
            string summary = Path.Combine( _channelsPath, "summary.xml" );
            if( File.Exists( summary ) )
            {
                try
                {
                    XmlDocument xdoc = new XmlDocument();
                    xdoc.Load( summary );
                    foreach( XmlElement channel in xdoc.GetElementsByTagName( "channel" ) )
                    {
                        string title = null;
                        string url = null;
                        XmlNodeList l = null;

                        l = channel.GetElementsByTagName( "title" );
                        if( l.Count < 1 )
                        {
                            continue;
                        }
                        title = l[0].InnerText;

                        l = channel.GetElementsByTagName( "newsFeed" );
                        if( l.Count < 1 )
                        {
                            continue;
                        }
                        url = l[0].InnerText;
                        _name2url.Add( title, url );
                    }
                }
                catch( Exception ex )
                {
                    Trace.WriteLine( "FeedDemon subscrption load failed: '" + ex.Message + "'" );
                }
            }
            return;
	    }

	    /// <summary>
	    /// Import cached items, flags, etc.
	    /// </summary>
	    public void DoImportCache()
	    {
            RSSPlugin plugin = RSSPlugin.GetInstance();

            _readItems = new ArrayList();
            // Register us for special tags
            plugin.RegisterItemElementParser( FeedType.Rss, _fdNS, "state", this );
            plugin.RegisterItemElementParser( FeedType.Atom, _fdNS, "state", this );

	        string[] allFiles = Directory.GetFiles( _channelsPath, "*.rss" );

            int totalFiles = Math.Max( allFiles.Length, 1 );
            int processedFiles = 0;

            foreach( string file in allFiles )
            {
                ImportUtils.UpdateProgress( processedFiles / totalFiles, _progressMessageCache );
                processedFiles += 100;

                IResource feed = null;
                string name = HtmlTools.SafeHtmlDecode( Path.GetFileNameWithoutExtension( file ) );
                if( _name2url.ContainsKey( name ) )
                {
                    IResourceList feeds = Core.ResourceStore.FindResources( "RSSFeed", Props.URL, _name2url[ name ] );
                    if( feeds.Count > 0 )
                    {
                        feed = feeds[0];
                    }
                }
                if( feed == null )
                {
                    IResourceList feeds = Core.ResourceStore.FindResources( "RSSFeed", Core.Props.Name, name );
                    if( feeds.Count > 0 )
                    {
                        feed = feeds[0];
                    }
                }
                // Not found (import of this feed was canceled?)
                if( feed == null )
                {
                    continue;
                }
                _readItems.Clear();
                using( Stream rss = new FileStream( file, FileMode.Open ) )
                {
                    try
                    {
                        RSSParser parser = new RSSParser( feed );
                        parser.Parse( rss, Encoding.UTF8, true );
                    }
                    catch( Exception ex )
                    {
                        Trace.WriteLine( "FeedDemon cache '" + file + "' load failed: '" + ex.Message + "'" );
                    }
                }
                foreach( IResource r in _readItems )
                {
                    if( ! r.IsDeleted )
                    {
                        r.DeleteProp( Core.Props.IsUnread );
                    }
                }
            }
            ImportUtils.UpdateProgress( processedFiles / totalFiles, _progressMessageCache );
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
            string a = null;
            a = reader.GetAttribute( "read" );
            if( a != null && a != "0" )
            {
                _readItems.Add( resource );
            }
            a = reader.GetAttribute( "flagged" );
            if( a != null && a != "0" )
            {
                resource.AddLink( "Flag", _flag );
            }
        }

	    /// <summary>
	    /// Checks if a Read() call is needed to move to the next element after
	    /// <see cref="ParseValue"/> is completed.
	    /// </summary>
	    public bool SkipNextRead
	    {
	        get { return false; }
	    }
	}
}
