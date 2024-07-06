// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using Microsoft.Win32;
using OmniaMea.RemoteControl;

namespace JetBrains.Omea.RemoteControl
{
    public class RemoteControlServer : IDisposable, IRemoteControlManager
    {
        private const string _csRemoteControlRegKey = @"SOFTWARE\JetBrains\Omea";
        private const string _regPortVal            = "ControlPort";
		private const string _regPortASVal          = "ControlPortAsString";
		private const string _regSecureVal          = "ControlProtection";
		private const string _regRunPath            = "ControlRun";
		private const int    _basePort              = 3566;
        private const int    _portDivider           = 97; // Prime number

        private const string _threadName = "RemoteControl server";

        private static Type _typeString = Type.GetType( "System.String" );
        private static Type _typeInt    = Type.GetType( "System.Int32" );
        private static Type _typeBool   = Type.GetType( "System.Boolean" );
        private static Type _typeVoid   = Type.GetType( "System.Void" );

        Hashtable _formatters = null;
        CreateFormatter _defaultFormatter;

        private int _port = 0;
        private bool _storePort = false;
		private static string _protectionKey = null;
        private Socket _listenSocket = null;
        private Hashtable _calls = null;
        private Thread _mainThread = null;
        private WaitCallback _onAccept = null;

        private IntPtr _stop;

		public static string protectionKey
		{
			get
			{
				if( _protectionKey == null )
				{
					// Read from registry, constructor was never called
					return (string)RegUtil.GetValue( Registry.CurrentUser, _csRemoteControlRegKey, _regSecureVal, "" );
				}
				return _protectionKey;
			}
		}

        public static int configuredPort
        {
            get
            {
                int rndPort = generateRandomPort();
				int port = 0;
                try
                {
                    if ( !RegUtil.IsKeyExists( Registry.CurrentUser, _csRemoteControlRegKey ) )
                    {
                        RegUtil.CreateSubKey( Registry.CurrentUser, _csRemoteControlRegKey );
                        RegUtil.SetValue( Registry.CurrentUser, _csRemoteControlRegKey, _regPortVal, rndPort );
					}
					port = (int)RegUtil.GetValue( Registry.CurrentUser, _csRemoteControlRegKey, _regPortVal, 0 );
					if( 0 == port )
					{
						RegUtil.SetValue( Registry.CurrentUser, _csRemoteControlRegKey, _regPortVal, rndPort );
						port = rndPort;
					}
                }
                catch
                {
					try
					{
						RegUtil.CreateSubKey( Registry.CurrentUser, _csRemoteControlRegKey );
						RegUtil.SetValue( Registry.CurrentUser, _csRemoteControlRegKey, _regPortVal, rndPort );
					}
					catch {}
					port = rndPort;
                }
				RegUtil.SetValue( Registry.CurrentUser, _csRemoteControlRegKey, _regPortASVal, port.ToString() );
				return port;
			}
        }

        internal static int generateRandomPort()
        {
            int port = _basePort + (int)(DateTime.Now.Ticks % _portDivider);
            return port;
        }

        internal RemoteControlServer() : this( 0 )
        {
        }

        internal RemoteControlServer( int port ) // For debugging we will use special port, not random one
        {
			_protectionKey = System.Guid.NewGuid().ToString();

            _port = port;
            // Remote control is disabled
            if ( -1 == _port )
            {
                return;
            }

            _formatters = new Hashtable();
            _formatters.Add( "xml", new CreateFormatter(XmlOutputFormatter.CreateFormatter) );
            _defaultFormatter = new CreateFormatter(XmlOutputFormatter.CreateFormatter);


            _calls = new Hashtable();
            AddRemoteCall( "System.ListAllMethods", new ListAllMethodsDelegate(listAllMethods) );
            _onAccept = new WaitCallback(onAccept);
        }

        internal void Start()
        {
            if( -1 == _port )
            {
                return;
            }
	        try
            {
                IPAddress addr = IPAddress.Loopback;
                _listenSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
#if(DEBUG)	// Check an option that provides for listening on all the network interfaces, not only loopback (DEBUG builds only)
                if(!Core.SettingStore.ReadBool( "RemoteControl", "LoopbackInterfaceOnly", true ))
                {
                    addr = IPAddress.Any;
                }
#endif
                if( _port != 0 )
                {
                    _listenSocket.Bind( new IPEndPoint( addr, _port )  );
                }
                else
                {
                    _storePort = true;
                    while(true)
                    {
                        _port = generateRandomPort();
                        try
                        {
                            _listenSocket.Bind( new IPEndPoint( IPAddress.Loopback, _port )  );
                            break;
                        }
                        catch( SocketException ex )
                        {
                            if(ex.ErrorCode != 10048) // WSAEADDRINUSE
                                return;
                        }
                    }
                }
                _listenSocket.Listen( (int)SocketOptionName.MaxConnections );

                _stop = Winsock.WSACreateEvent();
                Winsock.WSAResetEvent( _stop );

                _mainThread = new Thread( new ThreadStart(Listener) );
                _mainThread.Name = _threadName;
                _mainThread.Start();
                _mainThread.Priority = ThreadPriority.BelowNormal;
            }
            catch
            {
                Winsock.WSASetEvent(_stop);
                _port = -1;
                throw;
            }
            return;
        }

		internal void StoreProtectionKey()
		{
			if ( !RegUtil.IsKeyExists( Registry.CurrentUser, _csRemoteControlRegKey ) )
			{
				RegUtil.CreateSubKey( Registry.CurrentUser, _csRemoteControlRegKey );
			}
			RegUtil.SetValue( Registry.CurrentUser, _csRemoteControlRegKey, _regSecureVal, protectionKey );
			RegUtil.SetValue( Registry.CurrentUser, _csRemoteControlRegKey, _regRunPath, Application.ExecutablePath);
            if( _storePort )
            {
                RegUtil.SetValue( Registry.CurrentUser, _csRemoteControlRegKey, _regPortVal,   _port );
                RegUtil.SetValue( Registry.CurrentUser, _csRemoteControlRegKey, _regPortASVal, _port.ToString() );
            }
		}

        internal bool IsEnabled { get { return -1 != _port; } }

        internal void Stop()
        {
            if( Winsock.WSAWaitForMultipleEvents( 1, new IntPtr[] { _stop }, 1/*true*/, 0, 0/*false*/ ) == Winsock.WSA_WAIT_EVENT_0 )
            {
                Trace.WriteLine( "[RCS] Second call to Stop()." );
                return;
            }
            Winsock.WSASetEvent(_stop);
            Trace.WriteLine( "[RCS] Stop event has been fired." );

            try
            {
                Trace.WriteLine( "[RCS] Waiting for server thread..." );
                if( !Application.MessageLoop )
                {
                    _mainThread.Join();
                }
                else
                {
                    while( _mainThread.IsAlive && !_mainThread.Join( 100 ) )
                    {
                        Application.DoEvents();
                    }
                }
                Trace.WriteLine( "[RCS] Server thread has finished." );
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "[RCS] Server thread waiting exception: " + ex.Message );
            }
            finally
            {
                _mainThread = null;
            }
            Winsock.WSACloseEvent( _stop );
        }


        #region HTTP Server methods

        private void Listener()
        {
            IntPtr socketEvent = Winsock.WSACreateEvent();
            IntPtr[] allEvents = { _stop, socketEvent };
            IntPtr handle = _listenSocket.Handle;
            Winsock.WSAEventSelect( handle, socketEvent, Winsock.FD_ACCEPT );
            while( true )
            {
                try
                {
                    uint res = Winsock.WSAWaitForMultipleEvents( 2, allEvents, 0/*false*/, Winsock.WSA_INFINITE, 1/*true*/ );
                    if( res == Winsock.WSA_WAIT_EVENT_0 )
                    {
                        Trace.WriteLine( "[RCS] Event 0 (STOP) signalled" );
                        break;
                    }
                    else if( res == Winsock.WSA_WAIT_EVENT_0 + 1 )
                    {
                        Winsock.WSAResetEvent( socketEvent );
                        Trace.WriteLine( "[RCS] Event 1 (ACCEPT) signalled" );

                        Socket cs = _listenSocket.Accept();
                        Winsock.WSAEventSelect( cs.Handle, socketEvent, 0 );
                        cs.Blocking = true;

                        Trace.WriteLine( "[RCS] Client accepted at socket " + cs.Handle.ToString() );
                        ThreadPool.QueueUserWorkItem( _onAccept, cs );
                    }
                    else
                    {
                        Trace.WriteLine( "[RCS] Event " + res.ToString() + " (?) signalled" );
                    }
                }
                catch( Exception ex )
                {
                    Trace.WriteLine( "[RCS] Accept exception: " + ex.Message );
                    // If not sopped: sleep and try again
                    if( Winsock.WSAWaitForMultipleEvents( 1, new IntPtr[] { _stop }, 1/*true*/, 0, 0/*false*/ ) != Winsock.WSA_WAIT_EVENT_0 )
                    {
                        Trace.WriteLine( "[RCS] It is not stop!" );
                        Thread.Sleep( 100 );
                    }
                    else
                    {
                        Trace.WriteLine( "[RCS] It is stop, exiting." );
                        // Stopped: exit
                        break;
                    }
                }
            }
            try
            {
                Trace.WriteLine( "[RCS] Closing main socket " + handle.ToString() + " ..." );
                _listenSocket.Close();
                Trace.WriteLine( "[RCS] Main socket " + handle.ToString() + " closed." );
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "[RCS] Main socket closing exception: " + ex.Message );
            }
            finally
            {
                _listenSocket = null;
            }
            Winsock.WSACloseEvent( socketEvent );
        }

        private void onAccept(Object oSocket)
        {
            Socket s = (Socket)oSocket;
            Trace.WriteLine( "[RCS] Start serve client on socket " + s.Handle.ToString() );
            using( Stream socketStream = new NetworkStream( s, false ) )
            {
                ProcessRequest( socketStream );
            }
            Trace.WriteLine( "[RCS] Client served on socket " + s.Handle.ToString() );
            try
            {
                Trace.WriteLine( "[RCS] Shutting down client scoket " + s.Handle.ToString() + " ..." );
                s.Shutdown( SocketShutdown.Both );
                Trace.WriteLine( "[RCS] Client socket " + s.Handle.ToString() + " shutdowned." );
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "[RCS] Client socket shutdown exception: " + ex.Message );
            }
            try
            {
                Trace.WriteLine( "[RCS] Closing client socket " + s.Handle.ToString() + " ..." );
                s.Close();
                Trace.WriteLine( "[RCS] Client socket " + s.Handle.ToString() + " closed." );
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "[RCS] Client socket closing exception: " + ex.Message );
            }
        }

        private void ProcessRequest( Stream socketStream )
        {
            CallRequest req = new CallRequest();
            try
            {
                req.Read( socketStream );
            }
            catch(CallRequest.RequestException ex)
            {
                FormatError( "HTTP/1.0", _defaultFormatter(), socketStream, ex.Code, ex.Message );
                return;
            }
            catch(Exception ex)
            {
                FormatError( "HTTP/1.0", _defaultFormatter(), socketStream, 408, ex.Message );
                return;
            }

            // Check our method
            if(Core.State != CoreState.Running)
            {
                FormatError( req.Version, _defaultFormatter(), socketStream, 403, "Not ready" );
                return;
            }

            // Check secure string
            int keyEnd = req.URL.IndexOf( '/', 1 );
            if( keyEnd < 0 )
            {
                FormatError( req.Version, _defaultFormatter(), socketStream, 404, "Secure key not found" );
                return;
            }
            string key = req.URL.Substring( 1, keyEnd - 1 );
            if( key != _protectionKey )
            {
                FormatError( req.Version, _defaultFormatter(), socketStream, 403, "Invalid secure key" );
                return;
            }

            // Try to find formatter
            int frmEnd = req.URL.IndexOf( '/', keyEnd + 1 );
            if(frmEnd < 0)
            {
                FormatError( req.Version, _defaultFormatter(), socketStream, 404, "Output format not found" );
                return;
            }
            string formatName = req.URL.Substring( keyEnd + 1, frmEnd - keyEnd - 1 );
            // Try to find method.
            if( !_formatters.ContainsKey( formatName ) )
            {
                FormatError( req.Version, _defaultFormatter(), socketStream, 404, "Output format not found" );
                return;
            }
            IOutputFormatter formatter = (_formatters[formatName] as CreateFormatter)();

            string method = req.URL.Substring( frmEnd + 1 );
            if( !_calls.ContainsKey( method ) )
            {
                FormatError( req.Version, formatter, socketStream, 404, "Method not found" );
                return;
            }

            // Get method and check parameters
            Delegate md;
            lock(_calls)
            {
                md = _calls[method] as Delegate;
            }
            object result;
            try
            {
                result = CallMethod( md, req );
                FormatResult( req.Version, formatter, socketStream, result );
            }
            catch(Exception ex)
            {
                FormatError( req.Version, formatter, socketStream, 500, ex.Message );
            }
        }

        private void FormatResult( string httpVer, IOutputFormatter formatter, Stream stream, object result )
        {
            byte[] xml;
            string response = "";

			formatter.startResult();
			formatter.addValue( "retval", result );
            xml = formatter.finishOutput();

            response += httpVer + " 200 OK\r\n";
            response += GenerateCommonHeaders( formatter.getContentType(), xml.Length );
            response += "\r\n";

            byte[] rb = Encoding.UTF8.GetBytes( response );
            stream.Write( rb, 0, rb.Length );
			stream.Write( xml, 0, xml.Length );
        }

        private void FormatError( string httpVer, IOutputFormatter formatter, Stream stream, int code, string message )
        {
			byte[] xml;
			string response = "";

			formatter.startException( code, message );
			xml = formatter.finishOutput();

            response += httpVer + " " + code.ToString() + " " + message  +"\r\n";
            response += GenerateCommonHeaders( formatter.getContentType(), xml.Length );
            response += "\r\n";

            byte[] rb = Encoding.UTF8.GetBytes( response );
            stream.Write( rb, 0, rb.Length );
			stream.Write( xml, 0, xml.Length );
		}

        private object DemarhasllObject( string name, Type objectType, Hashtable pool )
        {
            object val = pool.ContainsKey( name ) ? pool[ name ] : null;
            if( _typeString == objectType )
            {
                if( val != null )
                {
                    return val as string;
                }
                else
                {
                    return "";
                }
            }
            else if( _typeInt == objectType )
            {
                try
                {
                    if( val != null )
                    {
                        return Int32.Parse( val as string );
                    }
                    else
                        throw new Exception();
                }
                catch
                {
                    throw new Exception("Invalid integer value");
                }
            }
            else if( _typeBool == objectType )
            {
                if( val != null )
                {
                    return "0" !=  val as string;
                }
                else
                {
                    return false;
                }
            }
            else if( objectType.IsValueType && !objectType.IsPrimitive )
            {
                MemberInfo[] fields = objectType.GetMembers();
                object retval = Activator.CreateInstance( objectType );
                foreach ( MemberInfo mi in fields )
                {
                    if( mi.MemberType != MemberTypes.Field )
                        continue;
                    FieldInfo field = mi as FieldInfo;
                    object fVal = DemarhasllObject( name + "." + field.Name,  field.FieldType, pool );
                    field.SetValue( retval, fVal );
                }
                return retval;
            }
            return null;
        }

        private object CallMethod( Delegate method, CallRequest request )
        {
            MethodInfo mi = method.Method;
            ParameterInfo[] pis = mi.GetParameters();
            object[] args = new object[ pis.Length ];

            // Enumerate all parameters and fill out objects array
            for( int i = 0; i < pis.Length; ++i )
            {
                args[i] = DemarhasllObject( pis[i].Name, pis[i].ParameterType, request.Parameters );
            }
            // Arguments are ready
            return method.DynamicInvoke( args );
        }

        private string GenerateCommonHeaders(string contentType, int contentLength)
        {
            string h = "";
            h += "Host: 127.0.0.1:" + _port + "\r\n";
            h += "Server: " + Core.ProductFullName + " Remote/" + Core.ProductVersion + "\r\n";
            h += "Date: " + DateTime.UtcNow.ToString( "r" ) + "\r\n";
            h += "Content-Length: " + contentLength + "\r\n";
            h += "Content-Type: " + contentType + "\r\n";
            return h;
        }

        #endregion

        #region IDisposable methods

        public void Dispose()
        {
            Stop();
        }

        #endregion

        #region IRemoteControlManager methods

        public bool AddRemoteCall(string rcName, Delegate method)
        {
            // check delegate
            foreach( ParameterInfo pi in method.Method.GetParameters() )
            {
                if( !CheckArgType( pi.ParameterType ) )
                    throw new ArgumentException( "Invalid type of delegate argument '" + pi.Name + "'" );
            }
            if( ! CheckResType( method.Method.ReturnType ) )
                throw new ArgumentException( "Invalid return type" );
            // No remote control.
            if ( ! IsEnabled )
            {
                return true;
            }
            lock(_calls)
            {
                if( _calls.ContainsKey( rcName ) )
                    return false;
                _calls.Add( rcName, method );
            }
            return true;
        }

        private bool CheckArgType( Type type )
        {
            if( type == _typeString || type == _typeInt || type == _typeBool )
                return true;

            if( type.IsValueType && !type.IsPrimitive )
            {
                MemberInfo[] fields = type.GetMembers();
                foreach ( MemberInfo mi in fields )
                {
                    if( mi.MemberType != MemberTypes.Field )
                        continue;
                    FieldInfo field = mi as FieldInfo;
                    // Field is of invalid type
                    if( ! CheckArgType( field.FieldType ) )
                        return false;
                }
                return true;
            }
            return false;
        }

        private bool CheckResType( Type type )
        {
            if( CheckArgType( type ) )
                return true;
            if( type == _typeVoid )
                return true;
            if( type.IsArray && type.HasElementType )
                return CheckResType( type.GetElementType() );
            return false;
        }

        #endregion

        #region RC Exported methods

        internal delegate string[] ListAllMethodsDelegate();
        internal string[] listAllMethods()
        {
            string[] result = new string[ _calls.Keys.Count ];
            lock(_calls)
            {
                int i = 0;
                foreach ( string name in _calls.Keys )
                {
                    result[ i++ ]  = name;
                }
            }
            return result;
        }

        #endregion

    }
}
