/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections;
using System.Windows.Forms;

namespace JetBrains.Omea.Net
{
    public delegate void AsciiProtocolUnitDelegate( AsciiProtocolUnit unit );

    public abstract class AsciiProtocolUnit
    {
        public event AsciiProtocolUnitDelegate Finished;

        protected internal abstract void Start( AsciiTcpConnection connection );

        protected static void StartUnit( AsciiProtocolUnit unit, AsciiTcpConnection connection )
        {
            unit.Start( connection );
        }

        protected virtual void FireFinished()
        {
            if( Finished != null )
            {
                try
                {
                    Finished( this );
                }
                finally
                {
                    Finished = null;
                }
            }
        }

        protected internal static void FireFinished( AsciiProtocolUnit unit )
        {
            unit.FireFinished();
        }
    }

    /// <summary>
    /// Connects to server
    /// </summary>
    public class AsciiConnectUnit: AsciiProtocolUnit
    {
        protected internal override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            connection.AfterConnect += new MethodInvoker( connection_AfterConnect );
            connection.ResolveFailed += new MethodInvoker( FireFinished );
            connection.ConnectFailed += new MethodInvoker( FireFinished );
            connection.Connect();
        }

        public bool Connected
        {
            get { return _connected; }
        }

        protected override void FireFinished()
        {
            _connection.AfterConnect -= new MethodInvoker( connection_AfterConnect );
            _connection.ResolveFailed -= new MethodInvoker( FireFinished );
            _connection.ConnectFailed -= new MethodInvoker( FireFinished );
            base.FireFinished();
        }

        private void connection_AfterConnect()
        {
            _connected = true;
            FireFinished();
        }

        private bool                _connected;
        private AsciiTcpConnection  _connection;
    }

    /// <summary>
    /// Connects to server and receives its prompt
    /// </summary>
    public class AsciiConnectAndGetServerPromptUnit: AsciiProtocolUnit
    {
        protected internal override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            AsciiConnectUnit connectUnit = new AsciiConnectUnit();
            connectUnit.Finished += new AsciiProtocolUnitDelegate( connectUnit_Finished );
            StartUnit( connectUnit, connection );
        }

        public bool Connected
        {
            get { return _connected; }
        }

        /// <summary>
        /// returns null on error, otherwise the first line of server response after connect
        /// </summary>
        public string ServerPrompt
        {
            get { return _serverPrompt; }
        }

        protected override void FireFinished()
        {
            if( _connected )
            {
                _connection.AfterReceive -= new MethodInvoker( FireFinished );
                _connection.LineReceived -= new LineDelegate( _connection_LineReceived );
                _connection.OperationFailed -= new MethodInvoker( FireFinished );
            }
            base.FireFinished();
        }

        private void connectUnit_Finished( AsciiProtocolUnit unit )
        {
            AsciiConnectUnit connectUnit = (AsciiConnectUnit) unit;
            if( !( _connected = connectUnit.Connected ) )
            {
                FireFinished();
            }
            else
            {
                _connection.AfterReceive += new MethodInvoker( FireFinished );
                _connection.LineReceived += new LineDelegate( _connection_LineReceived );
                _connection.OperationFailed += new MethodInvoker( FireFinished );
                _connection.SkipReceivedStream();
                _connection.Receive();
            }
        }

        private void _connection_LineReceived( string line )
        {
            if( _serverPrompt == null )
            {
                _serverPrompt = line;
            }
        }

        private bool                _connected;
        private string              _serverPrompt;
        private AsciiTcpConnection  _connection;
    }

    /// <summary>
    /// Send a line to server
    /// </summary>
    public class AsciiSendLineUnit: AsciiProtocolUnit
    {
        public AsciiSendLineUnit( string line )
        {
            _line = line;
        }

        protected internal override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            connection.AfterSend += new MethodInvoker( connection_AfterSend );
            connection.OperationFailed += new MethodInvoker( FireFinished );
            connection.SendLine( _line );
        }

        public bool LineSent
        {
            get { return _lineSent; }
        }

        protected override void FireFinished()
        {
            _connection.AfterSend -= new MethodInvoker( connection_AfterSend );
            _connection.OperationFailed -= new MethodInvoker( FireFinished );
            base.FireFinished();
        }

        private void connection_AfterSend()
        {
            _lineSent = true;
            FireFinished();
        }

        private string              _line;
        private bool                _lineSent;
        private AsciiTcpConnection  _connection;
    }

    /// <summary>
    /// Sends line to server, gets line as response
    /// </summary>
    public class AsciiSendLineGetLineUnit: AsciiProtocolUnit
    {
        public AsciiSendLineGetLineUnit( string line )
        {
            _line = line;
        }

        /// <summary>
        /// Was line successfully sent?
        /// </summary>
        public bool LineSent
        {
            get { return _lineSent; }
        }

        /// <summary>
        /// returns null on error, otherwise response as line
        /// </summary>
        public string ResponseLine
        {
            get { return _responseLine; }
        }

        protected internal override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            AsciiSendLineUnit sendUnit = new AsciiSendLineUnit( _line );
            sendUnit.Finished += new AsciiProtocolUnitDelegate( sendUnit_Finished );
            StartUnit( sendUnit, connection );
        }

        protected override void FireFinished()
        {
            if( _lineSent )
            {
                _connection.AfterReceive -= new MethodInvoker( FireFinished );
                _connection.LineReceived -= new LineDelegate( _connection_LineReceived );
                _connection.OperationFailed -= new MethodInvoker( FireFinished );
                
            }
            base.FireFinished();
        }

        private void sendUnit_Finished( AsciiProtocolUnit unit  )
        {
            AsciiSendLineUnit sendUnit = (AsciiSendLineUnit) unit;
            if( !( _lineSent = sendUnit.LineSent ) )
            {
                FireFinished();
            }
            else
            {
                _connection.AfterReceive += new MethodInvoker( FireFinished );
                _connection.LineReceived += new LineDelegate( _connection_LineReceived );
                _connection.OperationFailed += new MethodInvoker( FireFinished );
                _connection.SkipReceivedStream();
                _connection.Receive();
            }
        }

        private void _connection_LineReceived( string line )
        {
            if( _responseLine == null )
            {
                _responseLine = line;
            }
        }

        private string              _line;
        private bool                _lineSent;
        private string              _responseLine;
        private AsciiTcpConnection  _connection;
    }

    /// <summary>
    /// Sends line to server, gets response as line array
    /// if a response line starts with terminator then unit is finished
    /// </summary>
    public class AsciiSendLineGetLineArrayUnit: AsciiProtocolUnit
    {
        public AsciiSendLineGetLineArrayUnit( string line, string terminator )
        {
            _line = line;
            _terminator = terminator;
        }

        /// <summary>
        /// Was line successfully sent?
        /// </summary>
        public bool LineSent
        {
            get { return _lineSent; }
        }

        /// <summary>
        /// returns null on error, otherwise array of response lines
        /// </summary>
        public string[] ResponseLines
        {
            get { return ( _responseLines == null ) ? null : (string[]) _responseLines.ToArray( typeof( string ) ); }
        }

        protected internal override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            AsciiSendLineUnit sendUnit = new AsciiSendLineUnit( _line );
            sendUnit.Finished += new AsciiProtocolUnitDelegate( sendUnit_Finished );
            StartUnit( sendUnit, connection );
        }

        protected override void FireFinished()
        {
            if( _lineSent )
            {
                _connection.AfterReceive -= new MethodInvoker( _connection_AfterReceive );
                _connection.LineReceived -= new LineDelegate( _connection_LineReceived );
                _connection.OperationFailed -= new MethodInvoker( FireFinished );
            }
            base.FireFinished();
        }

        private void sendUnit_Finished( AsciiProtocolUnit unit  )
        {
            AsciiSendLineUnit sendUnit = (AsciiSendLineUnit) unit;
            if( !( _lineSent = sendUnit.LineSent ) )
            {
                FireFinished();
            }
            else
            {
                _connection.AfterReceive += new MethodInvoker( _connection_AfterReceive );
                _connection.LineReceived += new LineDelegate( _connection_LineReceived );
                _connection.OperationFailed += new MethodInvoker( FireFinished );
                _connection.SkipReceivedStream();
                _connection.Receive();
            }
        }

        private void _connection_AfterReceive()
        {
            _connection.Receive();
        }

        private void _connection_LineReceived( string line )
        {
            if( _responseLines == null )
            {
                _responseLines = new ArrayList();
            }
            if( line.StartsWith( _terminator ) )
            {
                FireFinished();
            }
            else
            {
                _responseLines.Add( line );
            }
        }

        private string              _line;
        private string              _terminator;
        private bool                _lineSent;
        private ArrayList           _responseLines;
        private AsciiTcpConnection  _connection;
    }

    /// <summary>
    /// Sends line to server, invokes method for each line of response
    /// if a response line starts with terminator then unit is finished
    /// </summary>
    public class AsciiSendLineAndApplyMethodUnit: AsciiProtocolUnit
    {
        public AsciiSendLineAndApplyMethodUnit( string line, string terminator, LineDelegate method )
        {
            _line = line;
            _terminator = terminator;
            _method = method;
        }

        /// <summary>
        /// Was line successfully sent?
        /// </summary>
        public bool LineSent
        {
            get { return _lineSent; }
        }

        protected internal override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            AsciiSendLineUnit sendUnit = new AsciiSendLineUnit( _line );
            sendUnit.Finished += new AsciiProtocolUnitDelegate( sendUnit_Finished );
            StartUnit( sendUnit, connection );
        }

        protected override void FireFinished()
        {
            if( _lineSent )
            {
                _connection.AfterReceive -= new MethodInvoker( _connection_AfterReceive );
                _connection.LineReceived -= new LineDelegate( _connection_LineReceived );
                _connection.OperationFailed -= new MethodInvoker( FireFinished );
                
            }
            base.FireFinished();
        }

        private void sendUnit_Finished( AsciiProtocolUnit unit  )
        {
            AsciiSendLineUnit sendUnit = (AsciiSendLineUnit) unit;
            if( !( _lineSent = sendUnit.LineSent ) )
            {
                FireFinished();
            }
            else
            {
                _connection.AfterReceive += new MethodInvoker( _connection_AfterReceive );
                _connection.LineReceived += new LineDelegate( _connection_LineReceived );
                _connection.OperationFailed += new MethodInvoker( FireFinished );
                _connection.SkipReceivedStream();
                _connection.Receive();
            }
        }

        private void _connection_AfterReceive()
        {
            _connection.Receive();
        }

        private void _connection_LineReceived( string line )
        {
            if( line.StartsWith( _terminator ) )
            {
                FireFinished();
            }
            else
            {
                _method( line );
            }
        }

        private string              _line;
        private string              _terminator;
        private LineDelegate        _method;
        private bool                _lineSent;
        private AsciiTcpConnection  _connection;
    }
}