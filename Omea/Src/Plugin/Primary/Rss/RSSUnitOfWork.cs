/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.HTML;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.RSSPlugin
{
    public enum RSSWorkStatus 
    { 
        NotStarted, InProgress, Success, NotModified, HTTPError, XMLError, FoundHTML, FoundXML,
        FeedDeleted
    };

    public class RSSUnitOfWork: AbstractNamedJob
    {
        private readonly IStatusWriter  _statusWriter;
        private readonly IResource      _feed;
        private readonly HttpReader     _httpReader;
        private readonly bool           _parseItems;
        private readonly bool           _acceptHtmlIfXmlError;
        private Exception               _lastException;
        private RSSWorkStatus           _status;
        private int                     _attempts = 0;
        private string                  _feedName;

        public event DownloadProgressEventHandler DownloadProgress;
        public event EventHandler ParseDone;

        public RSSUnitOfWork( IResource feed, bool parseItems, bool acceptHtmlIfXmlError )
        {
            Trace.WriteLineIf( Settings.Trace, "Starting update of feed " + feed.DisplayName );
            _feed        = feed;
            _parseItems  = parseItems;
            _acceptHtmlIfXmlError = acceptHtmlIfXmlError;
            _statusWriter = Core.UIManager.GetStatusWriter( this, StatusPane.Network );

            string feedUrl = feed.GetStringProp( Props.URL );
            if ( !HttpReader.IsSupportedProtocol( feedUrl ) )
            {
                throw new ArgumentException( "Unsupported feed protocol: " + feedUrl );
            }
            if ( parseItems )
            {
                FavIconManager favIconManager = (FavIconManager) Core.GetComponentImplementation( typeof(FavIconManager) );
                favIconManager.DownloadFavIcon( feedUrl );
            }
            _httpReader  = new HttpReader( feedUrl );

            string etag = feed.GetPropText( Props.ETag );
            if ( etag.Length > 0 )
            {
                _httpReader.IfNoneMatch = etag;
            }

            string httpUserName = feed.GetStringProp( Props.HttpUserName );
            string httpPassword = feed.GetStringProp( Props.HttpPassword );
            if ( httpUserName != null && httpPassword != null )
            {
                _httpReader.Credentials = new NetworkCredential( httpUserName, httpPassword );
            }

            _httpReader.AcceptInstanceManipulation = "feed";

            if ( _feed.HasProp( Props.DisableCompression ) || Settings.DisableCompression )
            {
                _httpReader.AcceptEncoding = null;
            }

            _httpReader.CookieProvider = CookiesManager.GetUserCookieProvider( typeof( RSSUnitOfWork ) );

            _status = RSSWorkStatus.NotStarted;

            Timeout = Settings.TimeoutInSec * 1000;
            OnTimeout += RSSUnitOfWork_OnTimeout;
        }

        public int Attempts { get { return _attempts; } set { _attempts = value; } } 
        public override string Name
        {
            get { return "Downloading " + _httpReader.URL; }
        }

        public Exception LastException
        {
            get { return _lastException; }
        }

        public RSSWorkStatus Status
        {
            get { return _status; }
        }

        public HttpStatusCode HttpStatus
        {
            get
            {
                if ( _httpReader.WebResponse == null )
                {
                    return HttpStatusCode.OK;
                }
                return _httpReader.WebResponse.StatusCode;
            }
        }

        public Stream ReadStream
        {
            get { return _httpReader.ReadStream; }
        }

        public string CharacterSet
        {
            get { return _httpReader.CharacterSet; }
        }

        public IResource Feed
        {
            get { return _feed; }
        }

        public string FeedURL
        {
            get { return _httpReader.URL; }
        }

        public override int GetHashCode()
        {
            return _feed.Id;
        }

        public override bool Equals( object obj )
        {
            if( obj is RSSUnitOfWork )
            {
                RSSUnitOfWork job = (RSSUnitOfWork) obj;
                return _feed.Id == job._feed.Id;
            }
            return false;
        }

        protected override void Execute()
        {
            if ( _status == RSSWorkStatus.NotStarted )
            {
                ResourceProxy proxy = new ResourceProxy( _feed );
                // the immediate priority is required to make sure that the resource job to set
                // (updating) status is executed before the parse job, which also has
                // immediate priority
                proxy.AsyncPriority = JobPriority.Immediate;
                proxy.SetPropAsync( Props.UpdateStatus, "(updating)" );
                _status = RSSWorkStatus.InProgress;
            }
            RSSLoadDelegate();
        }

        private void RSSLoadDelegate()
        {
            UpdateStatus();
            MethodInvoker method = _httpReader.NextMethod;
            Debug.Assert( method != null );
            method();
            WaitHandle httpHandle = _httpReader.NextWaitHandle;

            if ( _httpReader.CurrentState == HttpReader.State.Reading )
            {
                if ( DownloadProgress != null )
                {
                    long currentSize = _httpReader.ReadStream.Length;
                    long totalSize   = _httpReader.WebResponse.ContentLength; 
                    DownloadProgress( this, new DownloadProgressEventArgs( currentSize, totalSize ) );
                }
            }
            else if ( _httpReader.CurrentState == HttpReader.State.Done )
            {
                if ( _httpReader.WebResponse != null &&
                    ( _httpReader.WebResponse.StatusCode == HttpStatusCode.NotModified ||
                      _httpReader.WebResponse.StatusCode == HttpStatusCode.PreconditionFailed ))
                {
                    OnParseDone( RSSWorkStatus.NotModified );
                }
                else
                {
                    InvokeAfterWait( RSSParseDelegate, null );
                    Core.ResourceAP.QueueJob( this );
                }
                return;
            }
            else if ( _httpReader.CurrentState == HttpReader.State.Error )
            {
                _lastException = _httpReader.LastException;
                OnParseDone( RSSWorkStatus.HTTPError );
                return;
            }
            InvokeAfterWait( RSSLoadDelegate, httpHandle );
        }

        private void UpdateStatus()
        {
            int bytes = ( _httpReader.ReadStream != null ) ? (int) _httpReader.ReadStream.Length : 0;
            if( _feedName == null )
            {
                _feedName = _feed.GetPropText( Core.Props.Name );
            }
            Utils.UpdateHttpStatus( _statusWriter, _feedName, bytes );
        }

        private void RSSParseDelegate()
        {
            if ( _feed.IsDeleted )
            {
                OnParseDone( RSSWorkStatus.FeedDeleted );
                return;
            }

            Stream feedStream = _httpReader.ReadStream;
            _feed.BeginUpdate();
            try
            {

                if ( HttpStatus == HttpStatusCode.Moved && _httpReader.RedirectUrl != null )
                {
                    _feed.SetProp( Props.URL, _httpReader.RedirectUrl );
                }

                byte[] streamStartBytes = new byte[256];
                int cBytes = feedStream.Read( streamStartBytes, 0, 256 );
                string streamStart = Encoding.Default.GetString( streamStartBytes, 0, cBytes );
                feedStream.Position = 0;

                Encoding encoding = null;
                string charset = _httpReader.CharacterSet;
                if ( charset != null )
                {
                    try
                    {
                        encoding = Encoding.GetEncoding( charset );
                    }
                    catch( Exception )
                    {
                        Trace.WriteLine( "Unknown encoding in HTTP for RSS feed: " + charset );
                        encoding = null;
                    }
                }
                
                TraceUrlsUnderSpy( _feed, feedStream, encoding );

                RSSParser parser = new RSSParser( _feed );
                try
                {
                    parser.Parse( feedStream, encoding, _parseItems );
                }
                catch( XmlException e )
                {
                    feedStream.Position = 0;
                    if ( _acceptHtmlIfXmlError &&
                        ( ( _httpReader.WebResponse != null && _httpReader.WebResponse.ContentType.StartsWith( "text/html" ) ) 
                        || HtmlTools.IsHTML( streamStart ) ) )
                    {
                        OnParseDone( RSSWorkStatus.FoundHTML );
                    }
                    else
                    {
                        _lastException = e;
                        OnParseDone( RSSWorkStatus.XMLError );
                    }
                    return;
                }
            
                if ( parser.FoundChannel )
                {
                    if ( _parseItems && _httpReader.ETag != null && _httpReader.ETag.Length > 0 )
                    {
                        _feed.SetProp( Props.ETag, _httpReader.ETag );
                    }
                    else
                    {
                        _feed.DeleteProp( Props.ETag );
                    }
                    OnParseDone( RSSWorkStatus.Success );
                }
                else if ( HtmlTools.IsHTML( streamStart )  )
                {
                    OnParseDone( RSSWorkStatus.FoundHTML );
                }
                else
                {
                    OnParseDone( RSSWorkStatus.FoundXML );
                }
            }
            finally
            {
                _feed.EndUpdate();
                if( RSSParser._nextItem != null )
                {
                    RSSParser._nextItem.ClearProperties();
                }
            }
            Core.NetworkAP.QueueJob( _cleanJob );
        }

        private static void TraceUrlsUnderSpy( IResource _feed, Stream feedStream, Encoding encoding )
        {
            string tracedUrls = Settings.TracedUrls;
            string feedHost = new Uri( _feed.GetPropText( Props.URL ) ).Host;
            if( tracedUrls != null && tracedUrls.Length > 0 && tracedUrls.IndexOf( feedHost ) >= 0 )
            {
                Encoding e = encoding ?? Encoding.UTF8;

                try
                {
                    string httpStream = new StreamReader( feedStream, e ).ReadToEnd();
                    Trace.WriteLine( _feed.GetPropText( Props.URL ) );
                    Trace.WriteLine( httpStream );
                }
                finally
                {
                    feedStream.Position = 0;
                }
            }
        }

        private static int _lastGCTicks = 0;
        private static MethodInvoker _cleanJob = CleanMemory;

        private static void CleanMemory()
        {
            if( Environment.TickCount - _lastGCTicks > 2000 ) 
            {
                GC.Collect();
                _lastGCTicks = Environment.TickCount;
            }
        }

        private void OnParseDone( RSSWorkStatus status )
        {
            Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddSeconds( 1 ), new MethodInvoker( _statusWriter.ClearStatus ) );
            Trace.WriteLineIf( Settings.Trace, "Finished parse of " + _httpReader.URL + " with status " + status );
            _status = status;
            if ( ParseDone != null )
            {
                ParseDone( this, EventArgs.Empty );
            }
        }

        private void RSSUnitOfWork_OnTimeout()
        {
            _lastException = new Exception( "Timeout when downloading feed." );
            OnParseDone( RSSWorkStatus.HTTPError );
        }
    }

    internal class MultipleFeedsJob : ReenteringEnumeratorJob
    {
        private int                 _currentFeed = 0;
        private readonly string[]   _feedUrls, _feedNames;
        private readonly string     _query, _searchPhrase;
        private readonly ArrayList  _feedProxies;
        private readonly ArrayList  _failedFeeds;
        private RSSWorkStatus       _currStatus;
        private Exception           _lastException;

        public event DownloadProgressEventHandler DownloadTitleProgress;
        public event DownloadProgressEventHandler DownloadProgress;
        public event EventHandler ParseDone;

        internal MultipleFeedsJob( string[] urls, string[] names, string query, string searchPhrase )
        {
            _feedUrls = urls;
            _feedNames = names;
            _query = query;
            _searchPhrase = searchPhrase;
            _feedProxies = new ArrayList();
            _failedFeeds = new ArrayList();
        }

        public override string Name          { get { return "Creating Search Feed(s)"; }
        }
        public RSSWorkStatus   Status        { get { return _currStatus;               } }
        public ArrayList       Feeds         { get { return _feedProxies;              } }
        public Exception       LastException { get { return _lastException; } }
        public ArrayList       FailedFeeds   { get { return _failedFeeds;   } }

        public override AbstractJob GetNextJob()
        {
            RSSUnitOfWork nextJob = null;

            if( _currentFeed != _feedUrls.Length )
            {
                string status = "Processing: " + _feedNames[ _currentFeed ];
                Trace.WriteLine( "MultipleFeedsJob -- Processing: " + _feedNames[ _currentFeed ] );
                if( DownloadTitleProgress != null )
                    DownloadTitleProgress( this, new DownloadProgressEventArgs( status ));

                ResourceProxy proxy = ResourceProxy.BeginNewResource( "RSSFeed" );
                proxy.SetProp( Props.Transient, 1 );
                proxy.SetProp( Props.URL, _feedUrls[ _currentFeed ] + _query );
                proxy.SetProp( Props.RSSSearchPhrase, _searchPhrase );
                proxy.SetProp( Core.Props.Name, _feedNames[ _currentFeed++ ] + ": \"" + _query + "\"" );
                proxy.EndUpdate();
                _feedProxies.Add( proxy );

                nextJob = new RSSUnitOfWork( proxy.Resource, false, true );
                nextJob.DownloadProgress += DownloadProgress;
                nextJob.ParseDone += JobParseDone;
            }
            return nextJob;
        }

        public override void EnumerationStarting()
        {
            _currStatus = RSSWorkStatus.Success;
        }
        public override void EnumerationFinished()
        {
            //  In the current scheme we allow some of the feeds to fail
            //  (by different reasons). In any case we select those feeds that
            //  succeed so that user can proceed further.

            foreach( IResource res in _failedFeeds )
            {
                for( int i = 0; i < _feedProxies.Count; i++ )
                {
                    if( ((ResourceProxy) _feedProxies[ i ]).Resource.Id == res.Id )
                    {
                        _feedProxies.RemoveAt( i );
                        break;
                    }
                }
            }

            if( ParseDone != null )
            {
                ParseDone( this, null );
            }
        }

        private void  JobParseDone(object sender, EventArgs e)
        {
            RSSUnitOfWork job = (RSSUnitOfWork) sender;
            if( job.Status != RSSWorkStatus.Success )
            {
                Trace.WriteLine( "MultipleFeedsJob -- JobParseDone failed with code " + _currStatus );

                _failedFeeds.Add( job.Feed );
                _lastException = job.LastException;
            }

            //  Update status if only previous iterations were successful.
            //  Otherwise keep the fact that an error has already occured.
            //  This is necessary in the case when some is parsed correctly
            //  after the errorneous one.
            if( _currStatus == RSSWorkStatus.Success )
            {
                _currStatus = job.Status;
            }
        }
    }

    public class DownloadProgressEventArgs
    {
        private readonly long _currentSize;
        private readonly long _totalSize;
        private readonly string _message;

        internal DownloadProgressEventArgs( long currentSize, long totalSize )
        {
            _currentSize = currentSize;
            _totalSize = totalSize;

            _message = "Downloading (" + SizesToString() + ")...";
        }

        internal DownloadProgressEventArgs( string message )
        {
            _message = message;
        }

        internal string SizesToString()
        {
            string strSize = CurrentSize / 1024 + "K";
            if ( TotalSize > 0 )
            {
                strSize += "/" + TotalSize / 1024 + "K";
            }
            return strSize;
        }

        public long CurrentSize { get { return _currentSize; } }
        public long TotalSize   { get { return _totalSize; } }
        public string Message   { get { return _message; } }
    }

    public delegate void DownloadProgressEventHandler( object sender, DownloadProgressEventArgs e );
}
