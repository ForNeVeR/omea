// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
    internal sealed class NntpConnectionPool
    {
        /// <summary>
        /// Returns existing or creates the new NNTP connection for a server and keyword.
        /// </summary>
        /// <param name="server">News server resource</param>
        /// <param name="keyword">Keyword serves for distinguishing connections to a single news server. For
        /// example, currently used keywords can be "background" and "foreground".</param>
        /// <returns>Instance of the connection.</returns>
        public static NntpConnection GetConnection( IResource server, string keyword )
        {
            NntpConnection result;
            lock( _connections )
            {
                HashMap keywordMap = (HashMap) _connections[ server.Id ];
                if( keywordMap == null )
                {
                    keywordMap = new HashMap();
                    _connections[ server.Id ] = keywordMap;
                }
                keyword = keyword.ToLower();
                result = (NntpConnection) keywordMap[ keyword ];
                if( result == null || result.ConnectionState == NntpConnection.NntpConnectionState.NotConnected )
                {
                    result = new NntpConnection( new ServerResource( server ) );
                    keywordMap[ keyword ] = result;
                }
            }
            return result;
        }

        public static void CloseConnections( IResource server )
        {
            lock( _connections )
            {
                HashMap keywordMap = (HashMap) _connections[ server.Id ];
                if( keywordMap != null )
                {
                    foreach( HashMap.Entry mapEntry in keywordMap )
                    {
                        ( (NntpConnection) mapEntry.Value ).Close();
                    }
                    _connections.Remove( server.Id );
                }
            }
        }

        public static NntpConnection[] GetAllConnections()
        {
            return (NntpConnection[]) GetConnectionsImpl( false ).ToArray( typeof( NntpConnection ) );
        }

        public static NntpConnection[] GetBusyConnections()
        {
            return (NntpConnection[]) GetConnectionsImpl( true ).ToArray( typeof( NntpConnection ) );
        }

        public static void CloseAll()
        {
            NntpConnection[] connections = GetAllConnections();
            foreach( NntpConnection connection in connections )
            {
                connection.Close();
            }
        }

        private static ArrayList GetConnectionsImpl( bool onlyBusy )
        {
            ArrayList result = new ArrayList();
            lock( _connections )
            {
                foreach( IntHashTable.Entry e in _connections )
                {
                    HashMap map = (HashMap) e.Value;
                    foreach( HashMap.Entry mapEntry in map )
                    {
                        NntpConnection connection = (NntpConnection)mapEntry.Value;
                        if( !onlyBusy || connection.IsBusy )
                        {
                            result.Add( connection );
                        }
                    }
                }
            }
            return result;
        }

        private static IntHashTable _connections = new IntHashTable();
    }
}
