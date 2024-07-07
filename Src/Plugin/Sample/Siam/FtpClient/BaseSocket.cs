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
// $Log: BaseSocket.cs,v $
// Revision 1.2  2004/11/13 19:03:49  bruceb
// GetStream() changed, added comments
//
// Revision 1.1  2004/10/29 14:30:10  bruceb
// first cut
//
//

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace EnterpriseDT.Net
{
	/// <summary>
	/// Socket abstraction that simplifies socket code
	/// </summary>
	/// <author>
    /// Hans Andersen
	/// </author>
	/// <version>
    /// $LastChangedRevision$
	/// </version>
	public abstract class BaseSocket
	{
	    /// <summary>
	    /// Creates a new Socket for a newly created connection
	    /// </summary>
		public abstract BaseSocket Accept();

	    /// <summary>
	    /// Associates a Socket with a local endpoint.
	    /// </summary>
		public abstract void Bind(EndPoint localEP);

	    /// <summary>
	    /// Closes the Socket connection and releases all associated resources.
	    /// </summary>
		public abstract void Close();

	    /// <summary>
	    /// Establishes a connection to a remote endpoint
	    /// </summary>
		public abstract void Connect(EndPoint remoteEP);

	    /// <summary>
	    /// Places socket in a listening state.
	    /// </summary>
		public abstract void Listen(int backlog);

	    /// <summary>
	    /// Get the stream associated with the socket.
	    /// </summary>
	    /// <remarks>
	    /// The stream returned owns the socket, so closing the
	    /// stream will close the socket
	    /// </remarks>
		public abstract Stream GetStream();

	    /// <summary>
	    /// Receives data from a bound Socket.
	    /// </summary>
		public abstract int Receive(byte[] buffer);

	    /// <summary>
	    /// Sends data to a connected Socket.
	    /// </summary>
		public abstract int Send(byte[] buffer);

	    /// <summary>
	    /// Sets a Socket option.
	    /// </summary>
		public abstract void SetSocketOption(
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            int optionValue);

	    /// <summary>
	    /// Gets the local endpoint.
	    /// </summary>
		public abstract EndPoint LocalEndPoint {get;}
	}

	/// <summary>
	/// Standard implementation of BaseSocket
	/// </summary>
	/// <author>
    /// Hans Andersen
	/// </author>
	/// <version>
    /// $LastChangedRevision$
	/// </version>
	public class StandardSocket : BaseSocket
	{
	    /// <summary>
	    /// The real socket this class is wrapping
	    /// </summary>
		private Socket socket;

	    /// <summary>
	    /// Initializes a new instance of the StandardSocket class
	    /// </summary>
 		public StandardSocket(
			AddressFamily addressFamily,
			SocketType socketType,
			ProtocolType protocolType
			)
		{
			socket = new Socket(addressFamily, socketType, protocolType);
		}

	    /// <summary>
	    /// Initializes a new instance of the StandardSocket class
	    /// </summary>
		protected StandardSocket(Socket socket)
		{
			this.socket = socket;
		}

	    /// <summary>
	    /// Creates a new Socket for a newly created connection
	    /// </summary>
		public override BaseSocket Accept()
		{
			return new StandardSocket(socket.Accept());
		}

	    /// <summary>
	    /// Associates a Socket with a local endpoint.
	    /// </summary>
		public override void Bind(EndPoint localEP)
		{
			socket.Bind(localEP);
		}

	    /// <summary>
	    /// Closes the Socket connection and releases all associated resources.
	    /// </summary>
		public override void Close()
		{
			socket.Close();
		}

	    /// <summary>
	    /// Establishes a connection to a remote endpoint
	    /// </summary>
		public override void Connect(EndPoint remoteEP)
		{
			socket.Connect(remoteEP);
		}

	    /// <summary>
	    /// Places socket in a listening state.
	    /// </summary>
		public override void Listen(int backlog)
		{
			socket.Listen(backlog);
		}

	    /// <summary>
	    /// Get the stream associated with the socket.
	    /// </summary>
		public override Stream GetStream()
		{
			return new NetworkStream(socket, true);
		}

	    /// <summary>
	    /// Receives data from a bound Socket.
	    /// </summary>
		public override int Receive(byte[] buffer)
		{
			return socket.Receive(buffer);
		}

	    /// <summary>
	    /// Sends data to a connected Socket.
	    /// </summary>
		public override int Send(byte[] buffer)
		{
			return socket.Send(buffer);
		}

	    /// <summary>
	    /// Sets a Socket option.
	    /// </summary>
		public override void SetSocketOption(
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            int optionValue)
		{
			socket.SetSocketOption(optionLevel, optionName, optionValue);
		}

	    /// <summary>
	    /// Gets the local endpoint.
	    /// </summary>
		public override EndPoint LocalEndPoint
		{
			get
			{
				return socket.LocalEndPoint;
			}
		}
	}
}
