/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.HttpTools;

namespace JetBrains.Omea.Favorites
{
    /** 
     * favorite OUW is implemented as decorator over HttpReader job
     */
    internal class FavoriteJob : AbstractNamedJob
    {
        public FavoriteJob( IResource webLink, string URL )
        {
            _webLink = webLink;
            _statusWriter = Core.UIManager.GetStatusWriter( this, StatusPane.Network );
            _reader = new HttpReader( URL );
            Timeout = Core.SettingStore.ReadInt(
                "Favorites", "DownloadTimeout", 60000 ); // 1 minute in milliseconds
            OnTimeout += new MethodInvoker( _reader_OnTimeout );
            string ifNoneMatch = _webLink.GetPropText( FavoritesPlugin._propETag );
            if( ifNoneMatch.Length > 0 )
            {
                _reader.IfNoneMatch = ifNoneMatch;
            }
            _reader.IfModifiedSince = LastModified();
            _reader.CookieProvider = CookiesManager.GetUserCookieProvider( typeof( FavoriteJob ) );
            _lastState = _reader.CurrentState;
        }

        public override int GetHashCode()
        {
            return _webLink.Id;
        }

        public override bool Equals( object obj )
        {
            if( obj is FavoriteJob )
            {
                FavoriteJob job = (FavoriteJob) obj;
                return _webLink.Id == job._webLink.Id;
            }
            return false;
        }

        public override string Name
        {
            get { return "Downloading " + _reader.URL; }
            set {}
        }

        protected override void Execute()
        {
            UpdateStatus();
            DecoratedHttpReaderPredicate();
            if( NextWaitHandle == null )
            {
                Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddSeconds( 1 ), new MethodInvoker( _statusWriter.ClearStatus ) );
                new ResourceProxy( _webLink ).SetPropAsync( FavoritesPlugin._propLastUpdated, DateTime.Now );
            }
        }

        private void UpdateStatus()
        {
            int bytes = ( _reader.ReadStream != null ) ? (int) _reader.ReadStream.Length : 0;
            if( _weblinkName == null )
            {
                _weblinkName = _webLink.GetPropText( Core.Props.Name );
            }
            Utils.UpdateHttpStatus( _statusWriter, _weblinkName, bytes );
        }

        private void DecoratedHttpReaderPredicate()
        {
            MethodInvoker method = _reader.NextMethod;
            _reader.InvokeAfterWait( null, null ); // clear readers methods
            if( _reader.Type == HttpReader.URLType.Undefined || method == null )
            {
                SetLastError( "Protocol is not supported." );
                InvokeAfterWait( null, null );
                return;
            }
            /**
             * perform HttpReader work
             */
            method();
            /**
             * if there occured an http error save it and stop download
             */
            if( _reader.CurrentState == HttpReader.State.Error )
            {
                SetLastError( _reader.LastException.Message );
                InvokeAfterWait( null, null );
                return;
            }
            /**
             * if reading is performed the first time for the job, check whether
             * the content was changed or not from the last download attempt
             * stop download and set error if necessary
             */
            if( _lastState == HttpReader.State.Connecting &&
               ( _reader.CurrentState == HttpReader.State.Done || _reader.CurrentState == HttpReader.State.Reading ) )
            {
                HttpWebResponse response = _reader.WebResponse;
                if( _webLink.HasProp( "LastModified" ) && GetLastModified( response ) <= LastModified() )
                {
                    FavoritesTools.TraceIfAllowed( _reader.URL + " not modified, download stopped" );
                    InvokeAfterWait( null, null );
                    return;
                }
            }
            /**
             * if download has finished, process response
             */
            if( _reader.CurrentState == HttpReader.State.Done )
            {
                FavoritesTools.TraceIfAllowed( "_reader.CurrentState == HttpReader.State.Done" );
                if( _reader.Type ==  HttpReader.URLType.File )
                {
                    Core.ResourceAP.QueueJob( new MethodInvoker( ProcessFile ) );
                }
                else if( _reader.Type == HttpReader.URLType.Web )
                {
                    Core.ResourceAP.QueueJob( new MethodInvoker( ProcessWebLink ) );
                }
            }
            _lastState = _reader.CurrentState;
            InvokeAfterWait( NextMethod, _reader.NextWaitHandle );
        }

        private void ProcessFile()
        {
            if( _webLink.IsDeleted ) // if favorite resource was deleted then do nothing 
            {
                return;
            }
            FileInfo fileInfo = _reader.fileInfo;
            Stream readStream = _reader.ReadStream;
            using( readStream )
            {
                DateTime lastWriteTime = IOTools.GetLastWriteTime( fileInfo );
                if( !_webLink.HasProp( "LastModified" ) || lastWriteTime > LastModified() ||
                    _webLink.HasProp( Core.Props.LastError ) )
                {
                    bool isShowed = BookmarkService.BookmarkSynchronizationFrequency( _webLink ) > 0;
                    IResource formatFile = _webLink.GetLinkProp( "Source" );
                    _webLink.BeginUpdate();
                    if( formatFile != null )
                    {
                        formatFile.BeginUpdate();
                    }
                    else
                    {
                        string resourceType = Core.FileResourceManager.GetResourceTypeByExtension( fileInfo.Extension );
                        if( resourceType == null )
                        {
                            resourceType = "UnknownFile";
                        }
                        formatFile = Core.ResourceStore.BeginNewResource( resourceType );
                        _webLink.AddLink( "Source", formatFile );
                    }
                    if( _webLink.HasProp( Core.Props.LastError ) )
                    {
                        SetLastError( null );
                    }
                    _webLink.SetProp( "LastModified", lastWriteTime );
                    formatFile.SetProp( Core.Props.Size, (int) readStream.Length );
                    formatFile.SetProp( FavoritesPlugin._propContent, readStream );
                    if( isShowed )
                    {
                        formatFile.SetProp( Core.Props.Date, lastWriteTime );
                        formatFile.SetProp( FavoritesPlugin._propIsUnread, true );
                        Core.FilterManager.ExecRules( StandardEvents.ResourceReceived, _webLink );
                    }
                    if( formatFile.Type != "UnknownFile" )
                    {
                        Core.TextIndexManager.QueryIndexing( formatFile.Id );
                    }
                    _webLink.EndUpdate();
                    formatFile.EndUpdate();
                }
            }
        }

        private void ProcessWebLink()
        {
            Stream readStream = _reader.ReadStream;
            if( readStream == null || _webLink.IsDeleted ) // if favorite resource was deleted then do nothing 
            {
                return;
            }
            int propContent = FavoritesPlugin._propContent;
            HttpWebResponse response = _reader.WebResponse;
            // check whether http stream differs from earlier saved one
            bool differs = true;
            ///////////////////////////////////////////////////////////////////////////////
            /// the following if statement is a workaround over invalid processing by
            /// HttpReader the Not-Modified-Since header. .NET 1.1 was throwing the
            /// exception if content not modified, .NET 1.1 SP1 just returns empty stream
            ///////////////////////////////////////////////////////////////////////////////
            if( readStream.Length == 0 && !_webLink.HasProp( Core.Props.LastError ) )
            {
                return;
            }

            IResource formatFile = _webLink.GetLinkProp( "Source" );
            if( formatFile != null && formatFile.HasProp( propContent ) )
            {
                Stream savedStream = formatFile.GetBlobProp( propContent );
                using( savedStream )
                {
                    if( savedStream.Length == readStream.Length )
                    {
                        differs = false;
                        for( int i = 0; i < readStream.Length ; ++i )
                        {
                            if( ( byte ) savedStream.ReadByte() != ( byte ) readStream.ReadByte() )
                            {
                                readStream.Position = 0;
                                differs = true;
                                break;
                            }
                        }
                    }
                }
            }
            if( differs )
            {
                DateTime lastModified = GetLastModified( response );
                bool isShowed = BookmarkService.BookmarkSynchronizationFrequency( _webLink ) > 0;
                // content changed, so set properties and proceed with indexing

                string resourceType = "UnknownFile";
                if( !String.IsNullOrEmpty( response.ContentType ))
                {
                    resourceType = (Core.FileResourceManager as FileResourceManager).GetResourceTypeByContentType( response.ContentType );
                    if( resourceType == null )
                    {
                        resourceType = "UnknownFile";
                    }
                }
                _webLink.BeginUpdate();
                if( formatFile != null )
                {
                    formatFile.BeginUpdate();
                    if( resourceType != "UnknownFile" && formatFile.Type != resourceType )
                    {
                        formatFile.ChangeType( resourceType );
                    }
                }
                else
                {
                    formatFile = Core.ResourceStore.BeginNewResource( resourceType );
                    _webLink.AddLink( "Source", formatFile );
                }
                if( _webLink.HasProp( Core.Props.LastError ) )
                {
                    SetLastError( null );
                }
                _webLink.SetProp( "LastModified", lastModified );
                string redirectedUrl = _reader.RedirectUrl;
                if( redirectedUrl != null && redirectedUrl.Length > 0 )
                {
                    _webLink.SetProp( FavoritesPlugin._propURL, redirectedUrl );
                }
                if( _reader.ETag.Length > 0 )
                {
                    _webLink.SetProp( FavoritesPlugin._propETag, _reader.ETag );
                }                        
                // try to get charset from content-type or from content itself
                string charset = _reader.CharacterSet;
                if ( charset != null )
                {
                    _webLink.SetProp( Core.FileResourceManager.PropCharset, charset );
                }
                formatFile.SetProp( Core.Props.Size, (int) readStream.Length );
                formatFile.SetProp( propContent, readStream );
                if( isShowed )
                {
                    formatFile.SetProp( Core.Props.Date, lastModified );
                    formatFile.SetProp( FavoritesPlugin._propIsUnread, true );
                    Core.FilterManager.ExecRules( StandardEvents.ResourceReceived, _webLink );
                }
                if( formatFile.Type != "UnknownFile" )
                {
                    Core.TextIndexManager.QueryIndexing( formatFile.Id );
                }
                _webLink.EndUpdate();
                formatFile.EndUpdate();
            }
        }

        private static DateTime GetLastModified( HttpWebResponse response )
        {
            // Some servers send an invalid date in the Last-Modified header:
            // http://earful.bitako.com/index.php?a=rdf
            DateTime lastModified = DateTime.Now;
            try
            {
                lastModified = response.LastModified;
            }
            catch {}
            return lastModified;
        }

        private void _reader_OnTimeout()
        {
            _statusWriter.ClearStatus();
            SetLastError( "Timeout occurred reading remote site." );
        }

        private delegate void SetLastErrorDelegate( string errorMessage );

        private void SetLastError( string errorMessage )
        {
            if( Core.ResourceStore.IsOwnerThread() )
            {
                if( !_webLink.IsDeleted )
                {
                    if( errorMessage != null && errorMessage.Length > 0 )
                    {
                        _webLink.SetProp( Core.Props.LastError, errorMessage );
                    }
                    else
                    {
                        _webLink.DeleteProp( Core.Props.LastError );
                    }
                }
            }
            else
            {
                Core.ResourceAP.QueueJob(
                    JobPriority.AboveNormal, new SetLastErrorDelegate( SetLastError ), errorMessage );
            }
        }


        private DateTime LastModified()
        {
            DateTime result;
            try
            {
                result = _webLink.GetDateProp( "LastModified" );
            }
            catch
            {
                result = DateTime.MinValue;
            }
            return result;
        }

        private IStatusWriter       _statusWriter;
        private IResource           _webLink;
        private HttpReader          _reader;
        private HttpReader.State    _lastState;
        private string              _weblinkName;
    }
}