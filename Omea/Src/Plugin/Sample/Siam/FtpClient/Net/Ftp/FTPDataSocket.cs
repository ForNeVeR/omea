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
// $Log: FTPDataSocket.cs,v $
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

using System.IO;
using System.Net.Sockets;
using EnterpriseDT.Net;

namespace EnterpriseDT.Net.Ftp
{

	/// <summary>  Interface for data socket classes, whether active or passive
	///
	/// </summary>
	/// <author>       Bruce Blackshaw
	/// </author>
	/// <version>      $LastChangedRevision$
	/// </version>
	abstract public class FTPDataSocket
	{
		/// <summary>
		/// Get/Set the TCP timeout on the underlying control socket.
		/// </summary>
		virtual internal int Timeout
		{
			set
			{
                timeout = value;
			    SetSocketTimeout(sock, value);
			}

            get
            {
                return timeout;
            }
		}

        /// <summary>
		/// Returns the local port to which this socket is bound.
		/// </summary>
		internal int LocalPort
		{
			get
			{
				return ((System.Net.IPEndPoint) sock.LocalEndPoint).Port;
			}
		}

        /// <summary>
        /// The underlying socket
        /// </summary>
		internal BaseSocket sock = null;

        /// <summary>
		/// The timeout for the sockets
		/// </summary>
		internal int timeout = 0;

		/// <summary>
		/// Get the appropriate stream for reading or writing to
		/// </summary>
		/// <returns>
		/// input or output stream for underlying socket.
		/// </returns>
		abstract internal Stream DataStream
		{
			get;
        }

		/// <summary>
		/// Helper method to set a socket's timeout value
		/// </summary>
		/// <param name="sock">socket to set timeout for
		/// </param>
		/// <param name="timeout">timeout value to set
		/// </param>
		internal void SetSocketTimeout(BaseSocket sock, int timeout)
		{
			if (timeout > 0)
			{
				sock.SetSocketOption(SocketOptionLevel.Socket,
					SocketOptionName.ReceiveTimeout, timeout);
				sock.SetSocketOption(SocketOptionLevel.Socket,
					SocketOptionName.SendTimeout, timeout);
			}
		}

		/// <summary>  Closes underlying socket(s)</summary>
		abstract internal void Close();
	}
}
