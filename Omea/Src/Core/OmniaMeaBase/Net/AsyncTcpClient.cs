// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Diagnostics;
using Org.Mentalis.Security;
using Org.Mentalis.Security.Ssl;
using Org.Mentalis.Security.Ssl.Shared;

namespace JetBrains.Omea.Net
{
    /**
     * abstract asynchronous tcp client implemented as job
     * async processing implemented using Omea execution model (network processor used)
     * to create specific client, override virtual callback methods
     */

    public class AsyncTcpClient : AbstractNamedJob
    {
        public enum ClientState
        {
            NotStarted,
            Resolving,
            Connecting,
            Idle,
            Sending,
            Receiving,
            Disconnected,
            Failed
        };

        public enum ClientMode
        {
            Text,
            Binary
        };

        public const int _defaultReceiveBufferSize = 0x4000;
        public const int _defaultTimeout = 60000; // milliseconds

        public AsyncTcpClient( string server, int port )
            : this( server, port, _defaultReceiveBufferSize, _defaultTimeout, ClientMode.Text ) {}

        public AsyncTcpClient( string server, int port, int receiveBufferSize )
            : this( server, port, receiveBufferSize, _defaultTimeout, ClientMode.Text ) {}

        public AsyncTcpClient( string server, int port, int receiveBufferSize, int timeout )
            : this( server, port, receiveBufferSize, timeout, ClientMode.Text ) {}

        public AsyncTcpClient( string server, int port, ClientMode mode )
            : this( server, port, _defaultReceiveBufferSize, _defaultTimeout, mode ) {}

        public AsyncTcpClient( string server, int port, int receiveBufferSize, int timeout, ClientMode mode )
            : base()
        {
            _tracer = new Tracer( "TcpClient[" + server + ':' + port.ToString() + ']' );
            _server = server;
            _port = port;
            _receiveBufferSize = receiveBufferSize;
            _readStream = new JetMemoryStream( _defaultReceiveBufferSize );
            _currentLineBuilder = new StringBuilder();
            State = ClientState.NotStarted;
            _mode = mode;
            Timeout = timeout;
            OnTimeout += new MethodInvoker( OnOperationFailed );
            _sendMethod = new MethodInvoker( BeginSend );
            _receiveMethod = new MethodInvoker( BeginReceive );
            _endSendMethod = new MethodInvoker( EndSend );
            _endReceiveMethod = new MethodInvoker( EndReceive );
        }

        public void Connect()
        {
            State = ClientState.Resolving;
            InvokeAfterWait( new MethodInvoker( BeginResolve ), null );

            /**
             * The following pretty pass is programmed taking into consideration the
             * fact that there may occur clients with overriden GetHashcode() & Equals().
             * This fact leads to the possibility that network processor has in its
             * queue unfinished operations with the same server (say, the rest of
             * disconnecting protocol), so me MUST check whether this connecting UOW
             * is really queued. If not, there could be several workarounds. We just
             * try to connect once more in the small period of time without counting
             * the number of unsuccessful attempts.
             *
             * !!! please do not change the priority
             */
            if( !Core.NetworkAP.QueueJob( JobPriority.BelowNormal, this ) )
            {
                Core.NetworkAP.QueueJobAt( DateTime.Now.AddSeconds( 1 ), new MethodInvoker( Connect ) );
            }
        }

        public void Disconnect()
        {
            InvokeAfterWait( new MethodInvoker( BeginDisconnect ), null );
            // !!! please do not change the priority
            Core.NetworkAP.QueueJob( JobPriority.AboveNormal, this );
        }

        public void Send( string line )
        {
            Trace( line );
            // emulate 8-bit encoding
            byte[] bytes = new byte[ line.Length ];
            for( int i = 0; i < line.Length; ++i )
            {
                bytes[ i ] = (byte) line[ i ];
            }
            Send( bytes );
        }

        public void SendLine( string line )
        {
            Send( line + "\r\n" );
        }

        public void Send( byte[] bytes )
        {
            if( State == ClientState.Idle )
            {
                State = ClientState.Sending;
                _bytesToSend = bytes;
                InvokeAfterWait( _sendMethod, null );
                Core.NetworkAP.QueueJob( this );
            }
        }

        public void Send( byte[] bytes, int startIndex, int count )
        {
            if( State == ClientState.Idle )
            {
                State = ClientState.Sending;
                _bytesToSend = new byte[ count ];
                Array.Copy( bytes, startIndex, _bytesToSend, 0, count );
                InvokeAfterWait( _sendMethod, null );
                Core.NetworkAP.QueueJob( this );
            }
        }

        public void Receive()
        {
            if( State == ClientState.Idle )
            {
                State = ClientState.Receiving;
                InvokeAfterWait( _receiveMethod, null );
                Core.NetworkAP.QueueJob( this );
            }
        }

        public void SkipReceivedStream()
        {
            _readStream.SetLength( 0 );
        }

        #region properties

        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public bool TraceProtocol
        {
            get { return _traceProtocol; }
            set { _traceProtocol = value; }
        }

        public ClientState State
        {
            get { return _state; }
            set
            {
                // when disconnected nothing more can be done
                if( _state != ClientState.Disconnected )
                {
                    if( _traceProtocol )
                    {
                        Trace( "State = " + ( _state = value ).ToString() );
                    }
                    _state = value;
                }
            }
        }

        public ClientMode Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        public bool SSL3Enabled
        {
            get { return _ssl3Enabled; }
            set { _ssl3Enabled = value; }
        }

        public int ReceiveBufferSize
        {
            get { return _receiveBufferSize; }
            set { _receiveBufferSize = value; }
        }

        public Exception LastSocketException
        {
            get { return _lastSocketException; }
        }

        #endregion

        #region INamedUnitOfWork Members

        public override string Name
        {
            get { return "TCP connection to " + _server + ':' + _port; }
        }

        #endregion

        #region callbacks

        /**
         * callback must return true, if result is accepted and processing can be continued
         * otherwise client forces Disconnect()
         */
        protected virtual void OnResolveFailed() {}
        protected virtual void OnConnectFailed() {}
        protected virtual void OnAfterConnect() {}
        protected virtual void OnBeforeDisconnect() {}
        protected virtual void OnAfterSend() {}
        protected virtual void OnLineReceived( string line ) {}
        protected virtual void OnStreamReceived( Stream stream ) {}
        protected virtual void OnAfterReceive() {}
        protected virtual void OnOperationFailed() {}

        #endregion

        #region implementation details

        protected void Trace( string str )
        {
            if( _traceProtocol )
            {
                _tracer.Trace( str );
            }
        }

        private void BeginResolve()
        {
            if( State != ClientState.Disconnected )
            {
                try
                {
                    _currentAsyncResult = Dns.BeginResolve( _server, null, null );
                    InvokeAfterWait( new MethodInvoker( EndResolve ), _currentAsyncResult.AsyncWaitHandle );
                }
                catch( Exception e )
                {
                    if( !HandleException( e ) )
                    {
                        throw new AsyncTcpClientException( this, e );
                    }
                    OnResolveFailed();
                    return;
                }
            }
        }

        private void EndResolve()
        {
            if( State != ClientState.Disconnected )
            {
                try
                {
                    _hostEntry = Dns.EndResolve( _currentAsyncResult );
                }
                catch( Exception e )
                {
                    if( !HandleException( e ) )
                    {
                        throw new AsyncTcpClientException( this, e );
                    }
                    OnResolveFailed();
                    return;
                }

                try
                {
                    State = ClientState.Connecting;
                    _socket = new SecureSocket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
                    if( _ssl3Enabled )
                    {
                        _socket.ChangeSecurityProtocol( new SecurityOptions( SecureProtocol.Ssl3 ) );
                    }
                    _socket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.SendTimeout, Timeout );
                    _socket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, Timeout );
                    _endPoint = new IPEndPoint( _hostEntry.AddressList[ 0 ], _port );
                    if( _traceProtocol )
                    {
                        Trace( "Connecting to "  + _endPoint.Address.ToString() + ':' +  _endPoint.Port.ToString() );
                    }
                    CloseAsyncWaitHandle();
                    _currentAsyncResult = _socket.BeginConnect( _endPoint, null, null );
                    InvokeAfterWait( new MethodInvoker( EndConnect ), _currentAsyncResult.AsyncWaitHandle );
                }
                catch( Exception e )
                {
                    if( !HandleException( e ) )
                    {
                        throw new AsyncTcpClientException( this, e );
                    }
                    OnConnectFailed();
                    return;
                }
            }
        }

        private void EndConnect()
        {
            if( State != ClientState.Disconnected )
            {
                try
                {
                    _socket.EndConnect( _currentAsyncResult );
                }
                catch( Exception e )
                {
                    if( !HandleException( e ) )
                    {
                        throw new AsyncTcpClientException( this, e );
                    }
                    OnConnectFailed();
                    return;
                }
                State = ClientState.Idle;
                OnAfterConnect();
            }
        }

        private void BeginDisconnect()
        {
            if( State != ClientState.Disconnected )
            {
                OnBeforeDisconnect();
                Core.NetworkAP.QueueJob( JobPriority.BelowNormal, new MethodInvoker( EndDisconnect ) );
            }
        }

        private void EndDisconnect()
        {
            if( State != ClientState.Disconnected )
            {
                State = ClientState.Disconnected;
                // socket is not created when resolving failed
                if( _socket != null )
                {
                    try
                    {
                        _socket.Shutdown( SocketShutdown.Both );
                        _socket.Close();
                    }
                    catch( Exception e )
                    {
                        if( !HandleException( e ) )
                        {
                            throw new AsyncTcpClientException( this, e );
                        }
                        return;
                    }
                }
            }
        }

        private void BeginSend()
        {
            /**
             * check socket for null in order to avoid sending on disconnecting
             * if disconnect was initiated without connect, i.e. if socket was not created
             */
            if( State != ClientState.Disconnected && _socket != null )
            {
                try
                {
                    _currentAsyncResult = _socket.BeginSend(
                        _bytesToSend, 0, _bytesToSend.Length, SocketFlags.None, null, null );
                    InvokeAfterWait( _endSendMethod, _currentAsyncResult.AsyncWaitHandle );
                }
                catch( Exception e )
                {
                    if( !HandleException( e ) )
                    {
                        throw new AsyncTcpClientException( this, e );
                    }
                    OnOperationFailed();
                    return;
                }
            }
        }

        private void EndSend()
        {
            if( State != ClientState.Disconnected )
            {
                try
                {
                    if( ( _socket.EndSend( _currentAsyncResult ) == _bytesToSend.Length ) )
                    {
                        State = ClientState.Idle;
                    }
                    else
                    {
                        State = ClientState.Failed;
                        OnOperationFailed();
                    }
                }
                catch( Exception e )
                {
                    if( !HandleException( e ) )
                    {
                        throw new AsyncTcpClientException( this, e );
                    }
                    OnOperationFailed();
                    return;
                }
                CloseAsyncWaitHandle();
                OnAfterSend();
            }
        }

        private void BeginReceive()
        {
            if( State != ClientState.Disconnected )
            {
                // create new buffer if it was not created or buffer size changed
                if( _readBytes == null || _readBytes.Length != _receiveBufferSize )
                {
                    _readBytes = new byte[ _receiveBufferSize ];
                }
                try
                {
                    _currentAsyncResult = _socket.BeginReceive(
                        _readBytes, 0, _readBytes.Length, SocketFlags.None, null, null );
                    InvokeAfterWait( _endReceiveMethod, _currentAsyncResult.AsyncWaitHandle );
                }
                catch( Exception e )
                {
                    if( !HandleException( e ) )
                    {
                        throw new AsyncTcpClientException( this, e );
                    }
                    OnOperationFailed();
                    return;
                }
            }
        }

        private void EndReceive()
        {
            if( State != ClientState.Disconnected )
            {
                try
                {
                    int numberOfBytes = _socket.EndReceive( _currentAsyncResult );
                    if( numberOfBytes > 0 )
                    {
                        _readStream.Write( _readBytes, 0, numberOfBytes );
                        if( _socket.Available > 0 )
                        {
                            CloseAsyncWaitHandle();
                            _currentAsyncResult = _socket.BeginReceive(
                                _readBytes, 0, _readBytes.Length, SocketFlags.None, null, null );
                            InvokeAfterWait( _endReceiveMethod, _currentAsyncResult.AsyncWaitHandle );
                            return;
                        }
                    }
                }
                catch( Exception e )
                {
                    if( !HandleException( e ) )
                    {
                        throw new AsyncTcpClientException( this, e );
                    }
                    OnOperationFailed();
                    return;
                }

                State = ClientState.Idle;

                _readStream.Position = 0;

                /**
                 * in binary mode, just process memory stream as is
                 * in text mode, build list of lines and process them successively
                 */
                try
                {
                    if( _mode == ClientMode.Binary )
                    {
                        OnStreamReceived( _readStream );
                    }
                    else
                    {
                        int aChar;
                        while( ( aChar = _readStream.ReadByte() ) != -1 )
                        {
                            _currentLineBuilder.Append( (char)aChar );
                            if( aChar == '\n' )
                            {
                                string line = _currentLineBuilder.ToString();
                                try
                                {
                                    Trace( line );
                                    OnLineReceived( line );
                                }
                                finally
                                {
                                    _currentLineBuilder.Length = 0;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    SkipReceivedStream();
                }
                CloseAsyncWaitHandle();

                OnAfterReceive();
            }
        }

        private void CloseAsyncWaitHandle()
        {
            _currentAsyncResult.AsyncWaitHandle.Close();
        }

        protected override void Execute()
        {
        }

        private bool HandleException( Exception e )
        {
            e = Utils.GetMostInnerException( e );
            _tracer.TraceException( e );
            bool handled = false;
            if( e is SocketException || e is SslException || e is SecurityException ||
                e is InvalidOperationException ||  // workaround over #6498
                e is ArgumentOutOfRangeException || // workaround over OM-10281
                e is ApplicationException ) // workaround over OM-11252
            {
                _lastSocketException = e;
                handled = true;
            }
            if( e is NullReferenceException ) // workaround over OM-8256 and similar exceptions
            {
                handled = true;
            }
            return handled;
        }

        private Tracer              _tracer;
        private string              _server;
        private int                 _port;
        private bool                _traceProtocol;
        private ClientState         _state;
        private ClientMode          _mode;
        private bool                _ssl3Enabled;
        private IAsyncResult        _currentAsyncResult;
        private IPHostEntry         _hostEntry;
        private IPEndPoint          _endPoint;
        private SecureSocket        _socket;
        private byte[]              _bytesToSend;
        private byte[]              _readBytes;
        private int                 _receiveBufferSize;
        private JetMemoryStream     _readStream;
        private StringBuilder       _currentLineBuilder;
        private Exception           _lastSocketException;
        private MethodInvoker       _sendMethod;
        private MethodInvoker       _receiveMethod;
        private MethodInvoker       _endSendMethod;
        private MethodInvoker       _endReceiveMethod;

        #endregion
    }

    public class AsyncTcpClientException : Exception
    {
        public AsyncTcpClientException( AsyncTcpClient client, Exception e )
            : base( "Tcp client [" + client.Server + ":" + client.Port + "] stopped due to exception ", e ) {}
    }
}
