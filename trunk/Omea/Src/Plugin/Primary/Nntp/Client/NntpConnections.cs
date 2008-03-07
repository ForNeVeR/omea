/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.Net;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
    internal delegate void NntpConnectionDelegate( NntpConnection connection );

    internal class NntpConnection : AsciiTcpConnection
    {
        public NntpConnection( ServerResource server )
            : base( server.Name, server.Port )
        {
            _server = server;
            _serverProxy = new ResourceProxy( server.Resource );
            this.Timeout = Timeout = Core.SettingStore.ReadInt( "NNTP", "Timeout", 60 ) * 1000;
            TraceProtocol = Settings.Trace;
            SSL3Enabled = server.SSL3Enabled;
            _state = NntpConnectionState.Connecting;
            System.Diagnostics.Trace.WriteLine( Server + ":" + Port + ": Connecting to server..." );
            _serverProxy.DeletePropAsync( Core.Props.LastError );
            DoConnect();
            if( !_server.Resource.IsDeleted )
            {
                string username = _server.LoginName;
                if( username.Length > 0 )
                {
                    DoAuthenticate( username, _server.Password );
                }
                StartUnit( Int32.MaxValue, new AsciiSendLineGetLineUnit( "mode reader" ) );
            }
        }

        public enum NntpConnectionState
        {
            NotConnected,
            Connecting,
            Connected
        }

        public NntpConnectionState ConnectionState
        {
            get { return _state; }
        }

        public string ServerPrompt
        {
            get { return _serverPrompt; }
        }

        public override void Close()
        {
            _state = NntpConnectionState.NotConnected;
            AbandonStartedUnits();
            if( LastSocketException == null )
            {
                SendLine( "quit" );
            }
        }

        #region connection

        public void DoConnect()
        {
            AsciiConnectAndGetServerPromptUnit connectUnit = new AsciiConnectAndGetServerPromptUnit();
            connectUnit.Finished += new AsciiProtocolUnitDelegate( ConnectFinished );
            StartUnit( Int32.MaxValue, connectUnit );
        }

        private void ConnectFinished( AsciiProtocolUnit unit )
        {
            AsciiConnectAndGetServerPromptUnit connectUnit = (AsciiConnectAndGetServerPromptUnit) unit;
            _serverPrompt = connectUnit.ServerPrompt;
            _state = ( connectUnit.Connected ) ? NntpConnectionState.Connected : NntpConnectionState.NotConnected;
            if( _state == NntpConnectionState.NotConnected )
            {
                SetError( _serverPrompt );
            }
        }

        #endregion

        #region authentication

        public void DoAuthenticate( string username, string password )
        {
            NntpAuthenticateUnit authenticateUnit = new NntpAuthenticateUnit( username, password );
            authenticateUnit.Finished += new AsciiProtocolUnitDelegate( AuthenticateFinished );
            StartUnit( Int32.MaxValue, authenticateUnit );
        }

        private void AuthenticateFinished( AsciiProtocolUnit unit )
        {
            NntpAuthenticateUnit authenticateUnit = (NntpAuthenticateUnit) unit;
            if( !authenticateUnit.Succeeded )
            {
                SetError( authenticateUnit.ResponseLine );
            }
        }

        #endregion

        private void SetError( string error )
        {
            if( error == null || error.Length == 0 )
            {
                if( LastSocketException != null )
                {
                    error = LastSocketException.Message;
                }
            }
            if( error != null && error.Length > 0 )
            {
                _serverProxy.SetPropAsync( Core.Props.LastError, error.TrimEnd( '\r', '\n' ) );
            }
        }

        private ServerResource      _server;
        private ResourceProxy       _serverProxy;
        private NntpConnectionState _state;
        private string              _serverPrompt;
    }
}