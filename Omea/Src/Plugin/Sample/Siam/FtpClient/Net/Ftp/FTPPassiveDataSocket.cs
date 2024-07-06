// edtFTPnet
/*
SPDX-FileCopyrightText: 2004 Enterprise Distributed Technologies Ltd

SPDX-License-Identifier: LGPL-2.1-or-later
*/
//
// Bug fixes, suggestions and comments should posted on
// http://www.enterprisedt.com/forums/index.php
//
// Change Log:
//
// $Log: FTPPassiveDataSocket.cs,v $
// Revision 1.8  2004/11/13 19:04:42  bruceb
// GetStream removed arg
//
// Revision 1.7  2004/11/05 20:00:29  bruceb
// cleaned up namespaces
//
// Revision 1.6  2004/11/04 22:32:26  bruceb
// made many protected methods internal
//
// Revision 1.5  2004/11/04 21:18:14  hans
// *** empty log message ***
//
// Revision 1.4  2004/10/29 14:30:31  bruceb
// BaseSocket changes
//
//

using System;
using System.IO;
using System.Net.Sockets;
using BaseSocket = EnterpriseDT.Net.BaseSocket;

namespace EnterpriseDT.Net.Ftp
{

	/// <summary>  Passive data socket handling class
	///
	/// </summary>
	/// <author>       Bruce Blackshaw
	/// </author>
	/// <version>      $LastChangedRevision$
	/// </version>
	public class FTPPassiveDataSocket : FTPDataSocket
	{
		/// <summary>
		/// Set the TCP timeout on the underlying control socket.
		/// </summary>
		internal override int Timeout
		{
			set
			{
			    SetSocketTimeout(sock, value);
			}
		}

		/// <summary>
		/// Gets the stream of the connection
		/// </summary>
		internal override Stream DataStream
		{
			get
			{
				return sock.GetStream();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sock"> client socket to use
		/// </param>
		internal FTPPassiveDataSocket(BaseSocket sock)
		{
			this.sock = sock;
		}

		/// <summary>
		/// Closes underlying socket
		/// </summary>
		internal override void Close()
		{
			sock.Close();
		}
	}
}
