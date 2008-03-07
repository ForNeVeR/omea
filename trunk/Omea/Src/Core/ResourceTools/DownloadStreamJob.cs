/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
    public delegate void ReadyDelegate( bool ready );
    public class DownloadStreamJob : AbstractNamedJob
    {
        protected HttpReader _reader;
        private ReadyDelegate _readyCallback;
        private bool _successfull = false;
        private Exception _lastException;
        protected string _url;

        protected DownloadStreamJob( string url, ReadyDelegate readyCallback, bool initReader )
        {
            Init( url, readyCallback, initReader );
        }
        public DownloadStreamJob( string url )
        {
            Init( url, null, true );
        }
        public DownloadStreamJob( string url, ReadyDelegate readyCallback )
        {
            Init( url, readyCallback, true );
        }

        public int GetDownloadedSize()
        {
            return HttpReader.GetDownloadedSize();
        }
        public int GetLength()
        {
            return HttpReader.GetLength();
        }

        public bool Successfull { get { return _successfull; } }
        public HttpReader HttpReader { get { return _reader; } }

        protected void Init( string url, ReadyDelegate readyCallback, bool initReader )
        {
            Guard.NullArgument( url, "url" );
            _url = url;
            _readyCallback = readyCallback;
            if ( initReader )
            {
                _reader = new HttpReader( _url );
            }
        }

        protected virtual void StoreStream( Stream stream )
        {
        }

        protected virtual void Ready( )
        {
            if ( _readyCallback != null )
            {
                _readyCallback( _successfull );
            }
        }

        protected override void Execute()
        {
            MethodInvoker method = _reader.NextMethod;
            _reader.InvokeAfterWait( null, null ); // clear readers methods
            method();
            if( _reader.NextWaitHandle != null )
            {
                InvokeAfterWait( new MethodInvoker( Execute ), _reader.NextWaitHandle );
            }
            else
            {
                _successfull = _reader.LastException == null;
                if ( _successfull )
                {
                    Stream stream = _reader.ReadStream;
                    if ( stream != null )
                    {
                        try
                        {
                            StoreStream( stream );
                        }
                        catch ( Exception exception )
                        {
                            _lastException = exception;
                            Tracer._TraceException( exception );
                            Core.ReportBackgroundException( exception );
                            _successfull = false;
                        }
                    }
                }
                Ready();
            }
        }

        public Exception LastException 
        { 
            get
            {
                if ( _lastException != null )
                {
                    return _lastException;
                }
                return _reader.LastException;
            } 
        }

        public string Url { get { return _reader.URL; } }

        public override string Name
        {
            get
            {
                return "Download " + _reader.URL;
            }
            set
            {
            }
        }
    }
    public class DownloadResourceBlobJob : DownloadStreamJob
    {
        IResource _resource;
        int _propId;

        public DownloadResourceBlobJob( IResource resource, int propId, string url ) : base ( url, null )
        {
            Guard.NullArgument( resource, "resource" );
            _resource = resource;
            _propId = propId;
        }
        public DownloadResourceBlobJob( IResource resource, int propId, string url, ReadyDelegate readyCallback ) : base ( url, readyCallback )
        {
            _resource = resource;
            _propId = propId;
        }
        protected override void StoreStream( Stream stream )
        {
            new ResourceProxy( _resource ).SetPropAsync( _propId, stream );
        }
    }

    public class DownloadFileJob : DownloadStreamJob
    {
        private FileStream _file;

        public DownloadFileJob( string url, FileStream fileStream, int startPosition ) : base( url, null, false )
        {
            Guard.NullArgument( fileStream, "fileStream" );
            _file = fileStream;
            Init( url, null, false );
            _reader = new HttpReaderToFile( _url, _file, startPosition );
        }
        public DownloadFileJob( string url, FileStream fileStream, ReadyDelegate readyCallback, int startPosition ) : base( url, readyCallback )
        {
            Guard.NullArgument( fileStream, "fileStream" );
            _file = fileStream;
            Init( url, readyCallback, false );
            _reader = new HttpReaderToFile( _url, _file, startPosition );
        }
        protected override void StoreStream( Stream stream )
        {
            _file.Close();
        }
        public FileStream FileStream { get { return _file; } }
        protected override void Execute()
        {
            _file.Flush();
            base.Execute();
        }
    }
}
