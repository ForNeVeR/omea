/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.IO;
using System.Text;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;
using JetBrains.Omea.HTML;
using JetBrains.Omea.HttpTools;
using JetBrains.DataStructures;

namespace JetBrains.Omea.RSSPlugin
{
    /// <summary>
    /// Discovers the RSS feed for the specified Web page.
    /// </summary>
    internal class RSSDiscover: AbstractJob
    {
        private Uri                _baseUri;
        private RSSDiscoverResults _results;
        private PriorityQueue      _candidateURLs;
        private HashSet            _candidateURLSet;
        private int                _lastPriority;
        private IResource          _lastFeed;
        private RSSUnitOfWork      _lastUnitOfWork;
        private HashMap            _candidateHintTexts;
        private string             _lastCandidateURL;
        private bool               _downloadResults = true;
        
        public event DownloadProgressEventHandler DiscoverProgress;
        public event EventHandler DiscoverDone;

        public RSSDiscoverResults Results
        {
            get { return _results; }
        }

        public void StartDiscover( string url, Stream readStream, string charset )
        {
            OnDiscoverProgress( "Discovering..." );
            
            _baseUri = new Uri( url );
            _results = new RSSDiscoverResults();
            _candidateURLs      = new PriorityQueue();
            _candidateHintTexts = new HashMap();
            _candidateURLSet    = new HashSet();

            using( HTMLParser parser = new HTMLParser( OpenHTMLReader( readStream, charset) ) )
            {
                parser.AddTagHandler( "link", new HTMLParser.TagHandler( OnLinkTag ) );
                parser.AddTagHandler( "a", new HTMLParser.TagHandler( OnATag ) );
                parser.AddTagHandler( "/a", new HTMLParser.TagHandler( OnEndATag ) );
                while( !parser.Finished )
                {
                    string fragment = parser.ReadNextFragment();
                    if ( _lastCandidateURL != null )
                    {
                        _candidateHintTexts [_lastCandidateURL] = fragment.Trim();
                        _lastCandidateURL = null;
                    }
                }
                _lastPriority = -1;
                if ( _downloadResults )
                {
                    ParseNextCandidate();
                }
            }
        }

        internal bool DownloadResults
        {
            set { _downloadResults = value; }
        }

        internal PriorityQueue CandidateURLs
        {
            get { return _candidateURLs; }
        }

        protected override void Execute() {}

        private void OnLinkTag( HTMLParser instance, string tag )
        {
            HashMap attrMap = instance.ParseAttributes( tag );
            if ( (string) attrMap ["rel"] == "alternate" && 
                ((string) attrMap ["type"] == "application/rss+xml" || (string) attrMap ["type"] == "application/atom+xml" ) )
            {
                string href = (string) attrMap ["href"];
                if ( href != null )
                {
                    string url;
                    try
                    {
                        url = new Uri( _baseUri, href ).ToString();
                    }
                    catch( UriFormatException )
                    {
                        return;
                    }
                    if ( !HttpReader.IsSupportedProtocol( url ) )
                    {
                        return;
                    }
                    _candidateURLs.Push( 10, url );
                    _candidateURLSet.Add( url );
                    _candidateHintTexts [url] = attrMap ["title"];
                }
            }
        }

        private void OnATag( HTMLParser instance, string tag )
        {
            if ( instance.InScript )
                return;

            HashMap attrMap = instance.ParseAttributes( tag );
            string href = (string) attrMap ["href"];
            if ( href == null )
                return;

            if ( href.StartsWith( "feed:" ) )
            {
                href = "http:" + href.Substring( 5 );
            }

            Uri hrefUri;
            try
            {
                hrefUri = new Uri( _baseUri, href );
            }
            catch( Exception )
            {
                // sometimes generic exceptions are thrown from Uri constructor (see OM-9323) 
                return;
            }

            string hrefUriString;
            try
            {
/*
                OM-12523.
                System.UriFormatException: Invalid URI: The hostname could not be parsed.                at System.Uri.CreateHostStringHelper(String str, UInt16 idx, UInt16 end, Flags& flags, String& scopeId)
                at System.Uri.CreateHostString()
                at System.Uri.EnsureHostString(Boolean allowDnsOptimization)
                at System.Uri.GetComponentsHelper(UriComponents uriComponents, UriFormat uriFormat)
                at System.Uri.ToString()            
*/
                hrefUriString = hrefUri.ToString();
            }
            catch( System.UriFormatException )
            {
                return;
            }

            if ( !HttpReader.IsSupportedProtocol( hrefUriString ) )
            {
                return;
            }

            bool sameServer = ( String.Compare( _baseUri.Host, hrefUri.Host, true) == 0 );

            int pos = href.LastIndexOf( "." );
            string ext = ( pos < 0 ) ? "" : href.Substring( pos ).ToLower();
            int priority = 0;

            if ( ext == ".rss" || ext == ".rdf" || ext == ".xml" )
            {
                priority = sameServer ? 9 : 7;
            }
            else
            {
                href = href.ToLower();
                if ( href.IndexOf( "rss" ) >= 0 || href.IndexOf( "rdf" ) >= 0 || href.IndexOf( "xml" ) >= 0 )
                {
                    priority = sameServer ? 8 : 6;
                }
            }
            if ( priority != 0 )
            {
                if ( !_candidateURLSet.Contains( hrefUriString ) )
                {
                    _lastCandidateURL = hrefUriString;
                    _candidateURLSet.Add( _lastCandidateURL );
                    _candidateURLs.Push( priority, _lastCandidateURL );
                }
            }
        }

        private void OnEndATag( HTMLParser instance, string tag )
        {
            // don't use fragment text for RSS title if the fragment is after the closing </a> tag
            _lastCandidateURL = null;
        }

        private TextReader OpenHTMLReader( Stream readStream, string charset )
        {
            if ( charset == null )
            {
                readStream.Seek( 0, SeekOrigin.Begin );
                charset = HtmlTools.DetectCharset( new StreamReader( readStream ) );
            }
            readStream.Seek( 0, SeekOrigin.Begin );
            Encoding enc;
            try
            {
                enc = Encoding.GetEncoding( charset );
            }
            catch( Exception )
            {
                enc = Encoding.Default;
            }

            return new StreamReader( readStream, enc );
        }

        private void ParseNextCandidate()
        {
            PriorityQueue.QueueEntry qEntry = _candidateURLs.PopEntry();
            if ( qEntry == null )
            {
                OnDiscoverDone();
                return;
            }
            if ( _lastPriority != -1 && qEntry.Priority != _lastPriority && qEntry.Priority < 9 && _results.Count > 0 )
            {
                OnDiscoverDone();
                return;
            }

            _lastPriority = qEntry.Priority;

            ResourceProxy newFeedProxy = ResourceProxy.BeginNewResource( "RSSFeed" );
            newFeedProxy.SetProp( "Transient", 1 );
            newFeedProxy.SetProp( "URL", (string) qEntry.Value );
            newFeedProxy.EndUpdate();
            _lastFeed = newFeedProxy.Resource;
            
            _lastUnitOfWork = new RSSUnitOfWork( _lastFeed, false, true );
            _lastUnitOfWork.DownloadProgress += new DownloadProgressEventHandler( RSSDownloadProgress );
            _lastUnitOfWork.ParseDone += new EventHandler( RSSParseDone );
            Core.NetworkAP.QueueJob( _lastUnitOfWork );
        }

        private void RSSDownloadProgress( object sender, DownloadProgressEventArgs e )
        {
            RSSUnitOfWork uow = (RSSUnitOfWork) sender;
            OnDiscoverProgress( "Trying " + uow.FeedURL + " (" + e.SizesToString() + ")..." );
        }

        private void RSSParseDone( object sender, EventArgs e )
        {
            if ( _lastUnitOfWork.Status == RSSWorkStatus.Success )
            {
                string name     = _lastFeed.GetStringProp( Core.Props.Name );
                string url      = _lastFeed.GetStringProp( Props.URL );
                string hintText = (string) _candidateHintTexts [url];
                if ( name == null )
                {
                    name = url;
                }
                _results.Add( new RSSDiscoverResult( url, name, hintText ) );
            }
            new ResourceProxy( _lastFeed ).DeleteAsync();
            ParseNextCandidate();
        }

/*
        public void SearchSyndic8()
        {
            OnDiscoverProgress( "Searching Syndic8..." );
            _syndic8Proxy = new Syndic8();
            _syndic8Proxy.KeepAlive = false;
            InvokeAfterWait( new MethodInvoker( Syndic8FindFeeds ), null );
            Core.NetworkAP.QueueJob( JobPriority.Immediate, this );
        }

        private void Syndic8FindFeeds()
        {
            OnDiscoverProgress( "Finding feeds on Syndic8..." );
            _syndic8QueryString = _baseUri.Host.ToLower();
            if ( _syndic8QueryString.StartsWith( "www." ) )
            {
                _syndic8QueryString = _syndic8QueryString.Substring( 4 );
            }
            _syndic8AR = _syndic8Proxy.BeginFindSites( _syndic8QueryString, null, null );
            if ( _syndic8AR.IsCompleted )
            {
                Syndic8GetFeedInfo();
                return;
            }
            InvokeAfterWait( new MethodInvoker( Syndic8GetFeedInfo ), _syndic8AR.AsyncWaitHandle );
        }

        private void Syndic8GetFeedInfo()
        {
            OnDiscoverProgress( "Getting feed info on Syndic8..." );
            int [] feedIDs;
            try
            {
                feedIDs = _syndic8Proxy.EndFindSites( _syndic8AR );
            }
            catch( Exception e )
            {
                Trace.WriteLine( "Syndic8.com EndFindSites exception: " + e.ToString() );
                // ignore Syndic8.com XML-RPC call errors
                feedIDs = new int [0];
            }
            
            if ( feedIDs.Length == 0 )
            {
                OnDiscoverDone();
                return;
            }
            _syndic8AR = _syndic8Proxy.BeginGetFeedInfo( feedIDs, null, null );
            if ( _syndic8AR.IsCompleted )
            {
                Syndic8ProcessFeedInfo();
                return;
            }
            InvokeAfterWait( new MethodInvoker( Syndic8ProcessFeedInfo ), _syndic8AR.AsyncWaitHandle );
        }

        private void Syndic8ProcessFeedInfo()
        {
            object[] feedInfos;
            try
            {
                feedInfos = _syndic8Proxy.EndGetFeedInfo( _syndic8AR );
            }
            catch( Exception e )
            {
                Trace.WriteLine( "Syndic8.com EndGetFeedInfo exception: " + e.ToString() );
                OnDiscoverDone();
                return;
            }
            
            foreach( XmlRpcStruct feedInfo in feedInfos )
            {
                string status = (string) feedInfo ["status"];
                if ( status != "Dead" && status != "Duplicate" && status != "Rejected" )
                {
                    string url = (string) feedInfo ["dataurl"];

                    // when searching, for example, for "cnews.com", Syndic8 returns
                    // results like "abcnews.com"
                    // check that the symbol before the found string is not an
                    // alphanumeric character
                    
                    int pos = url.ToLower().IndexOf( _syndic8QueryString.ToLower() );
                    if ( pos > 0 && Char.IsLetterOrDigit( url, pos-1 ) )
                    {
                        continue;
                    }

                    int priority;
                    if ( (string) feedInfo ["scraped"] == "0" )
                        priority = 5;
                    else
                        priority = 4;

                    if ( HttpReader.IsSupportedProtocol( url ) )
                    {
                        _candidateURLs.Push( priority, url );
                    }
                }
            }
            ParseNextCandidate();
        }
*/

        private void OnDiscoverProgress( string message )
        {
            if ( DiscoverProgress != null )
            {
                DiscoverProgress( this, new DownloadProgressEventArgs( message ) );
            }
        }

        private void OnDiscoverDone()
        {
            if ( DiscoverDone != null )
            {
                DiscoverDone( this, EventArgs.Empty );
            }
        }

        public class RSSDiscoverResults: IEnumerable
        {
            private ArrayList _results = new ArrayList();

            public int Count
            {
                get { return _results.Count; }
            }

            internal void Add( RSSDiscoverResult result )
            {
                _results.Add( result );
            }

            public RSSDiscoverResult this[ int index ]
            {
                get { return (RSSDiscoverResult) _results [index]; }
            }

            public IEnumerator GetEnumerator()
            {
                return _results.GetEnumerator();
            }
        }

        public class RSSDiscoverResult
        {
            private string _url;
            private string _name;
            private string _hintText;
            private IResource _existingFeed;

            internal RSSDiscoverResult( string url, string name, string hintText )
            {
                _url          = url;
                _name         = name;
                _hintText     = hintText;
                _existingFeed = RSSPlugin.GetExistingFeed( url );
            }

            public string URL               { get { return _url; } }
            public string Name              { get { return _name; } }
            public string HintText          { get { return _hintText; } }
            public IResource ExistingFeed   { get { return _existingFeed; } }

            public override string ToString()
            {
                if ( _hintText != null && _hintText.Length > 0 )
                {
                    return _url + " (" + _hintText + ")";
                }
                return _url;
            }
        }
    }
}
