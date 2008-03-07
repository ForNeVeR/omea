// edtFTPnet
// 
// Copyright (C) 2004 Enterprise Distributed Technologies Ltd
// 
// www.enterprisedt.com
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
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