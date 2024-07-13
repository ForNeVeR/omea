// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
// SPDX-FileCopyrightText: 2024 Friedrich von Never
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.HttpTools
{
    public class HttpReader : AbstractJob
    {
        static HttpReader()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        /**
         * size of byte buffer for async reading of http stream
         */
        public const int BufferSize = 0x4000;

        /**
         * state of http reader
         * is set (and may asked) after execution of each async operation
         */
        public enum State
        {
            NotStarted,
            Connecting,
            Reading,
            Done,
            Error
        }

        /**
         * type of URL
         */
        public enum URLType
        {
            Undefined,
            File,
            Web
        }

    	public static string UserAgent
    	{
    		get
    		{
    			return _userAgent;
    		}
    		set
    		{
    			_userAgent = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(value));	// Suppress non-ASCII characters
    		}
    	}

        public static IWebProxy DefaultProxy
        {
            get
            {
                if ( _defaultProxy == null )
                {
                    _defaultProxy = GlobalProxySelection.GetEmptyWebProxy();
                }
                return _defaultProxy;
            }
        }

        public ICookieProvider CookieProvider
        {
            get { return _cookieProvider; }
            set { _cookieProvider = value; }
        }

        /**
         * sets proxy for all webrequests
         * opens current user's SSL certificates store
         */
        public static void LoadHttpConfig()
        {
            Trace.WriteLine( "Loading proxy settings" );
            try
            {
                _defaultProxy = LoadDefaultProxy();
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "Error loading default proxy settings: " + ex );
            }
            GlobalProxySelection.Select = DefaultProxy;

            X509Store store = new X509Store(StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                _certificates = store.Certificates;
                Trace.WriteLine("Number of current user's certificates: " + _certificates.Count, "HttpReader");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Cannot open certificate store: " + ex);
            }
            Trace.WriteLine( "Default connection limit: " + ServicePointManager.DefaultConnectionLimit );
            Trace.WriteLine( "Max service point idle time: " + ServicePointManager.MaxServicePointIdleTime );
            Trace.WriteLine( "Max service points: " + ServicePointManager.MaxServicePoints );
        }

        private static WebProxy LoadDefaultProxy()
        {
            WebProxy proxy;
            ISettingStore ini = Core.SettingStore;
            string address = ini.ReadString( "HttpProxy", "Address" );
            bool haveProxy = false;
            if( address.Length == 0 )
            {
                proxy = WebProxy.GetDefaultProxy();
                proxy.Credentials = CredentialCache.DefaultCredentials;
                if ( proxy.Address != null )
                {
                    Trace.WriteLine( "Using proxy settings from IE: address " + proxy.Address +
                                     ", bypass on local addresses " + proxy.BypassProxyOnLocal );
                    haveProxy = true;
                }
                else
                {
                    Trace.WriteLine( "No proxy configured in IE" );
                }
            }
            else
            {
                do
                {
                    int port = ini.ReadInt( "HttpProxy", "Port", 3128 );
                    try
                    {
                        proxy = new WebProxy( address, port );
                    }
                    catch
                    {
                        proxy = WebProxy.GetDefaultProxy();
                    }
                    string user = ini.ReadString( "HttpProxy", "User" );
                    if( user.Length == 0 )
                    {
                        proxy.Credentials = CredentialCache.DefaultCredentials;
                    }
                    else
                    {
                        string password = ini.ReadString( "HttpProxy", "Password" );
                        string[] userParts = user.Split( '\\', '/' );
                        if( userParts.Length == 2 )
                        {
                            proxy.Credentials = new NetworkCredential( userParts[ 1 ], password, userParts[ 0 ] );
                        }
                        else
                        {
                            proxy.Credentials = new NetworkCredential( user, password );
                        }
                    }
                    proxy.BypassProxyOnLocal = ini.ReadBool( "HttpProxy", "BypassLocal", true );

                    string bypassList = ini.ReadString( "HttpProxy", "BypassList" );
                    if ( bypassList.Trim().Length > 0 )
                    {
                        try
                        {
                            proxy.BypassList = bypassList.Split( ';' );
                        }
                        catch{}
                    }

                    if ( proxy.Address != null )
                    {
                        Trace.WriteLine( "Using proxy settings specified by user: address " + proxy.Address +
                            ", bypass on local addresses " + proxy.BypassProxyOnLocal );
                        haveProxy = true;
                    }
                    else
                    {
                        Trace.WriteLine( "No proxy configured by user" );
                    }
                } while( false );
            }

            if ( haveProxy )
            {
                ServicePointManager.DefaultConnectionLimit = 5;
            }

            return proxy;
        }

        /// <summary>
        /// Requests an URL.
        /// </summary>
        /// <param name="url">URL to web resource.</param>
        public HttpReader( string url )
        {
            Guard.EmptyStringArgument( url, "URL" );

            _urlType = SupportedProtocol( url );
            _state = State.NotStarted;
            _url = url;
            _certfId = -1;
            _ifModifiedSince = DateTime.MinValue;
            if( ICore.Instance != null && Core.SettingStore.ReadBool( "HttpReader", "Trace", false ) )
            {
                _tracer = new Tracer( "HttpReader" );
            }
        }

        /// <summary>
        /// Requests an URL sending specified request stream using the POST method.
        /// </summary>
        /// <param name="url">URL to web resource.</param>
        /// <param name="requestStream">Stream to send with request.</param>
        public HttpReader( string url, JetMemoryStream requestStream, string requestContentType )
            : this( url )
        {
            if( _urlType != URLType.Web )
            {
                throw new InvalidOperationException( "Only web URL can be requested using POST method" );
            }
            _requestStream = requestStream;
            _requestContentType = requestContentType;
        }

        public static bool IsSupportedProtocol( string url )
        {
            Guard.NullArgument( url, "url" );
            return SupportedProtocol( url ) != URLType.Undefined;
        }

        public static URLType SupportedProtocol( string url )
        {
            Guard.NullArgument( url, "url" );

            URLType urlType = URLType.Undefined;
            if( Utils.StartsWith( url, "http:", true ) || Utils.StartsWith( url, "https:", true ))
               urlType = URLType.Web;
            else
            if( Utils.StartsWith( url, "file:", true ) )
               urlType = URLType.File;

            return urlType;
        }

        /**
         * read-only properties
         * can be asked for specialized controlling of downloading, say,
         * when HttpReader switches from one state to another
         */
        public State CurrentState { get { return _state; } }
        public string URL { get { return _url; } }
        public URLType Type { get { return _urlType; } }
        public X509Certificate X509Certificate { get { return _X509Certificate; } }
        public HttpWebResponse WebResponse { get { return _response; } }
        public Stream ReadStream { get { return _readStream; } }
        public FileInfo fileInfo { get { return _fileInfo; } }
        public string ETag { get { return _ETag; } }
        public string IfNoneMatch { set { _ifNoneMatch = value; } }
        public DateTime IfModifiedSince { set { _ifModifiedSince = value; } }
        public Exception LastException { get { return _lastException; } }

        public ICredentials Credentials
        {
            get { return _credentials; }
            set { _credentials = value; }
        }

        public string AcceptInstanceManipulation
        {
            get { return _acceptInstanceManipulation; }
            set { _acceptInstanceManipulation = value; }
        }

        public string AcceptEncoding
        {
            get { return _acceptEncoding; }
            set { _acceptEncoding = value; }
        }

        public string RedirectUrl
        {
            get { return _redirectUrl; }
        }

        public string CharacterSet
        {
            get
            {
                if ( _response == null )
                {
                    return null;
                }

                int i = _response.ContentType.IndexOf( "charset=" );
                if( i > 0 )
                {
                    i += 8;
                    string charset = _response.ContentType;
                    int separator = charset.IndexOfAny( new char[] { ';', ',', ' ' }, i );
                    charset = ( separator > 0 ) ? charset.Substring( i, separator - i ) : charset.Substring( i );
                    charset = charset.Trim();
                    if( charset.Length > 0 )
                    {
                        return charset.Replace( '_', '-' ).Trim( '"' );
                    }
                }
                return null;
            }
        }

        #region implementation details

        protected override void Execute()
        {
            if( _urlType == URLType.File )
            {
                ProcessFileURL();
            }
            else
            if( _urlType == URLType.Web )
            {
                if( _requestStream != null )
                {
                    StartRequestWebURL();
                }
                else
                {
                    StartProcessingWebURL();
                }
            }
        }

        private void ProcessFileURL()
        {
            try
            {
                Uri uri = new Uri( _url );
                string localPath = uri.LocalPath.Replace( '|', ':' );
                _fileInfo = new FileInfo( localPath );
                TraceString( "Reading " + localPath );
                _readStream = _fileInfo.OpenRead();
                _state = State.Done;
            }
            catch( Exception e )
            {
                _lastException = e;
                TraceException( e );
                _state = State.Error;
            }
        }

        private void StartRequestWebURL()
        {
            _state = State.Connecting;
            try
            {
                PrepareRequest();
                _request.Method = "POST";
                _request.ContentType = _requestContentType;
                _request.ContentLength = _requestStream.Length;
                _asyncResult = _request.BeginGetRequestStream( null, null );
                InvokeAfterWait( WriteRequestStream, _asyncResult.AsyncWaitHandle );
            }
            catch( Exception e )
            {
                _lastException = e;
                TraceException( e );
                _state = State.Error;
            }
        }

        private void WriteRequestStream()
        {
            try
            {
                Stream requestStream = _request.EndGetRequestStream( _asyncResult );
                _requestStream.WriteTo( requestStream );
                requestStream.Close();
                CloseAsyncWaitHandle();
                _asyncResult = _request.BeginGetResponse( null, null );
                InvokeAfterWait( ProcessWebResponse, _asyncResult.AsyncWaitHandle );
            }
            catch( Exception e )
            {
                _lastException = e;
                TraceException( e );
                _state = State.Error;
            }
        }

        private void StartProcessingWebURL()
        {
            _state = State.Connecting;
            try
            {
                PrepareRequest();
                int wrkTh, complTh;
                ThreadPool.GetAvailableThreads( out wrkTh, out complTh );

                _asyncResult = _request.BeginGetResponse( null, null );
                InvokeAfterWait( ProcessWebResponse, _asyncResult.AsyncWaitHandle );
            }
            catch( Exception e )
            {
                _lastException = e;
                TraceException( e );
                _state = State.Error;
            }
        }

        protected virtual void PrepareRequest()
        {
            if( _certfId < 0 || _certificates == null )
            {
                _X509Certificate = null;
                TraceString( "Requesting " + _url );
            }
            else
            {
                _X509Certificate = _certificates[ _certfId ];
                TraceString( "Requesting " + _url + " using certificate " + _X509Certificate.GetName() );
            }
            _request = ( HttpWebRequest ) WebRequest.Create( _url );
            _request.Proxy = DefaultProxy;

            Uri requestUri = new Uri( _url );
            Uri proxyUri =  _request.Proxy.GetProxy( requestUri );
            if ( proxyUri != null && !proxyUri.Equals( requestUri ) )
            {
                TraceString( "Using proxy " + proxyUri );
            }
            else if ( _request.Proxy.IsBypassed( requestUri ) )
            {
                TraceString( "Bypassing proxy" );
            }
            else
            {
                TraceString( "Using no proxy" );
            }
            if( _tracer != null )
            {
                TraceString( String.Format( "Service point connection limit: {0}, current connections: {1}",
                    _request.ServicePoint.ConnectionLimit, _request.ServicePoint.CurrentConnections ) );
            }

            _request.AllowAutoRedirect = false;
            _request.UserAgent = _userAgent;
            if( _X509Certificate != null )
            {
                _request.ClientCertificates.Clear();
                _request.ClientCertificates.Add( _X509Certificate );
            }
            if( _cookieProvider != null )
            {
                string cookies = _cookieProvider.GetCookies( requestUri.AbsoluteUri );
                if( cookies != null )
                {
                    _request.CookieContainer = new CookieContainer();

                    //  fix OM-14781 - do not interrupt processing of the feed if the
                    //  cookies format is illegal - continue as if there is all OK.
                    try {  _request.CookieContainer.SetCookies( requestUri, cookies );  }
                    catch( CookieException ) {  /*  May be we can just ignore them?  */ }
                }
            }
            if( _ifNoneMatch != null )
            {
                _request.Headers[ "If-None-Match" ] = _ifNoneMatch;
            }
            if( _ifModifiedSince != DateTime.MinValue )
            {
                _request.IfModifiedSince = _ifModifiedSince;
            }
            if( _credentials != null )
            {
                _request.Credentials = _credentials;
            }
            else
            {
                _request.Credentials = CredentialCache.DefaultCredentials;
            }

            if( _acceptInstanceManipulation != null )
            {
                _request.Headers[ "A-IM" ] = _acceptInstanceManipulation;
            }

            // to avoid problems with 'deflate' encoding support (some servers send
            // the zlib header in the deflated stream and some don't), tell that we only
            // support gzip encoding
            if ( _acceptEncoding != null )
            {
                _request.Headers ["Accept-Encoding"] = _acceptEncoding;
            }

            _request.Pipelined = false;
        }

        private static byte[] _lastBuffer = null;

        protected virtual byte[] CreateBuffer()
        {
            byte[] result = _lastBuffer ?? new byte[ BufferSize ];
            _lastBuffer = null;
            return result;
        }

        protected virtual Stream CreateReadStream()
        {
            return new JetMemoryStream( BufferSize );
        }

        private void ProcessWebResponse()
        {
            try
            {
                _response = ( HttpWebResponse ) _request.EndGetResponse( _asyncResult );

                string responseUrl = _response.ResponseUri.AbsoluteUri;
                WebHeaderCollection headers = _response.Headers;
                TraceString( "Reading " + responseUrl + " ContentType=" + _response.ContentType );

                if ( _response.StatusCode == HttpStatusCode.Moved ||
                    _response.StatusCode == HttpStatusCode.Redirect ||
                    _response.StatusCode == HttpStatusCode.RedirectKeepVerb )
                {
                    _redirectCount++;
                    if ( _redirectCount > _maximumRedirectCount )
                    {
                        throw new Exception( "Exceeded maximum redirect count" );
                    }
                    Uri baseUri = new Uri( _url );
                    _url = new Uri( baseUri, headers ["Location"] ).ToString();
                    if( _response.StatusCode == HttpStatusCode.Moved ||
                        _response.StatusCode == HttpStatusCode.Redirect )
                    {
                        _redirectUrl = _url;
                    }
                    CloseAsyncWaitHandle();
                    _response.Close();
                    StartProcessingWebURL();
                    return;
                }

                _state = State.Reading;
                _ETag = headers[ "ETag" ];
                if( _ETag == null )
                {
                    _ETag = string.Empty;
                }
                if( _cookieProvider != null )
                {
                    string cookie = headers[ "Set-Cookie" ];
                    if( cookie != null )
                    {
                        _cookieProvider.SetCookies( responseUrl, cookie );
                    }
                }
                _responseStream = _response.GetResponseStream();
                _responseBytes = CreateBuffer();
                _readStream = CreateReadStream();
                CloseAsyncWaitHandle();
                _asyncResult = _responseStream.BeginRead( _responseBytes, 0, BufferSize, null, null );
                InvokeAfterWait( ReadWebStream, _asyncResult.AsyncWaitHandle );
                return;
            }
            catch( WebException e )
            {
                TraceString( "Error loading " + _url + ", WebExceptionStatus = " + e.Status );
                TraceException( e );
                if( e.Status == WebExceptionStatus.SecureChannelFailure &&
                    _certificates != null && ++_certfId < _certificates.Count )
                {
                    if ( e.Response != null )
                    {
                        e.Response.Close();
                    }
                    CloseAsyncWaitHandle();
                    StartProcessingWebURL();
                    return;
                }

                _response = ( HttpWebResponse ) e.Response;
                _state = ( _response == null || _response.StatusCode != HttpStatusCode.NotModified ) ?
                    State.Error : State.Done;
                _lastException = e;
            }
            catch( Exception e )
            {
                _lastException = e;
                TraceException( e );
                _state = State.Error;
            }
            CloseAsyncWaitHandle();
            if( _response != null )
            {
                try
                {
                    _response.Close();
                }
                catch( Exception ex )
                {
                    TraceException( ex );
                }
            }
        }

        public int GetDownloadedSize()
        {
            if ( _readStream == null )
            {
                return 0;
            }
            return (int)_readStream.Position;
        }
        public int GetLength()
        {
            if ( _response == null )
            {
                return 0;
            }
            return (int)_response.ContentLength;
        }

        private void ReadWebStream()
        {
            try
            {
                int readBytes = _responseStream.EndRead( _asyncResult );
                // if at least one byte is read we are to continue
                if( readBytes > 0 )
                {
                    _readStream.Write( _responseBytes, 0, readBytes );
                    CloseAsyncWaitHandle();
                    _asyncResult = _responseStream.BeginRead( _responseBytes, 0, BufferSize, null, null );
                    InvokeAfterWait( ReadWebStream, _asyncResult.AsyncWaitHandle );
                    return;
                }

                string contentEncoding = _response.ContentEncoding;
                if ( contentEncoding != null )
                {
                    DecodeReadStream( contentEncoding );
                }
                _state = State.Done;
                _readStream.Position = 0;
            }
            catch ( Exception e )
            {
                _lastException = e;
                TraceException( e );
                _state = State.Error;
            }
            _lastBuffer = _responseBytes;
            _responseBytes = null;
            CloseAsyncWaitHandle();
            _response.Close();
        }

        private void CloseAsyncWaitHandle()
        {
            _asyncResult.AsyncWaitHandle.Close();
        }

        private void DecodeReadStream( string encoding )
        {
            try
            {
                _readStream.Position = 0;
                Stream encodedStream;
                if ( encoding == "gzip" )
                {
                    encodedStream = new GZipInputStream( _readStream );
                }
                else if ( encoding == "deflate" )
                {
                    encodedStream = new InflaterInputStream( _readStream );
                }
                else
                {
                    return;
                }

                JetMemoryStream decodedStream = new JetMemoryStream( BufferSize );
                while( true )
                {
                    int bytesRead = encodedStream.Read( _responseBytes, 0, BufferSize );
                    if ( bytesRead == 0 )
                        break;

                    decodedStream.Write( _responseBytes, 0, bytesRead );
                }

                encodedStream.Close();
                _readStream.Close();

                _readStream = decodedStream;
            }
            catch( Exception ex )
            {
                throw new HttpDecompressException( "Decompression failed", ex );
            }
        }

        protected HttpWebRequest Request { get { return _request; } }

        protected void TraceString( string str )
        {
            if( _tracer != null )
            {
                _tracer.Trace( str );
            }
        }

        protected void TraceException( Exception e )
        {
            if( _tracer != null )
            {
                _tracer.TraceException( e );
            }
        }

        private const int                           _maximumRedirectCount = 10;
        private static X509CertificateCollection    _certificates;
        private static string                       _userAgent = String.Empty;
        private State                               _state;
        private string                              _url;
        private readonly URLType                    _urlType;
        private int                                 _certfId;
        private X509Certificate                     _X509Certificate;
        private HttpWebRequest                      _request;
        private readonly JetMemoryStream            _requestStream;
        private readonly string                     _requestContentType;
        private HttpWebResponse                     _response;
        private Stream                              _responseStream;
        private IAsyncResult                        _asyncResult;
        private byte[]                              _responseBytes;
        private Stream                              _readStream;
        private FileInfo                            _fileInfo;
        private string                              _ETag = string.Empty;
        private string                              _ifNoneMatch;
        private DateTime                            _ifModifiedSince;
        private Exception                           _lastException;
        private int                                 _redirectCount = 0;
        private string                              _redirectUrl;
        private ICredentials                        _credentials = null;
        private string                              _acceptInstanceManipulation = null;
        private ICookieProvider                     _cookieProvider = null;
        private string                              _acceptEncoding = "gzip";
        private readonly Tracer                     _tracer;

        private static IWebProxy                    _defaultProxy;

        #endregion
    }

    public class HttpReaderToFile : HttpReader
    {
        private readonly FileStream _file;
        private readonly int _startPosition = 0;
        public HttpReaderToFile( string URL, FileStream fileStream ) : base ( URL )
        {
            Guard.NullArgument( fileStream, "fileStream" );
            _file = fileStream;
        }
        public HttpReaderToFile( string URL, FileStream fileStream, int startPosition ) : base ( URL )
        {
            Guard.NullArgument( fileStream, "fileStream" );
            _file = fileStream;
            _startPosition = startPosition;
        }
        protected override Stream CreateReadStream()
        {
            return _file;
        }
        protected override void PrepareRequest()
        {
            base.PrepareRequest();
            Request.AddRange( _startPosition );
        }
    }

    public class HttpDecompressException: Exception
    {
        public HttpDecompressException( string message, Exception innerException )
            : base( message, innerException )
        {
        }
    }
}
