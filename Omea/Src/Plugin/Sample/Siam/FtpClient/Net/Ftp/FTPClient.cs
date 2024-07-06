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
// $Log: FTPClient.cs,v $
// Revision 1.14  2004/11/20 22:34:54  bruceb
// abort() added re resume, fixed resume append bug
//
// Revision 1.13  2004/11/15 23:26:40  hans
// *** empty log message ***
//
// Revision 1.12  2004/11/13 22:25:32  hans
// *** empty log message ***
//
// Revision 1.11  2004/11/13 19:14:01  bruceb
// exception restructuring etc
//
// Revision 1.10  2004/11/11 22:14:37  hans
// *** empty log message ***
//
// Revision 1.9  2004/11/06 22:38:13  bruceb
// renamed property
//
// Revision 1.8  2004/11/06 11:10:01  bruceb
// tidied namespaces, changed IOException to SystemException
//
// Revision 1.7  2004/11/05 20:00:13  bruceb
// events added
//
// Revision 1.6  2004/11/04 21:18:11  hans
// *** empty log message ***
//
// Revision 1.5  2004/11/03 21:32:04  bruceb
// fixed getbinary bug
//
// Revision 1.4  2004/10/29 14:30:31  bruceb
// BaseSocket changes
//
//

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;
using System.Globalization;
using System.ComponentModel;
using Level = EnterpriseDT.Util.Debug.Level;
using Logger = EnterpriseDT.Util.Debug.Logger;

namespace EnterpriseDT.Net.Ftp
{
    #region Types

    /// <summary>
	/// Event args for BytesTransferred event
	/// </summary>
    public class BytesTransferredEventArgs : EventArgs
    {
        /// <summary>
    	/// Constructor
		/// <param name="byteCount">
        /// The current count of bytes transferred
		/// </param>
    	/// </summary>
        public BytesTransferredEventArgs(long byteCount)
        {
            this.byteCount = byteCount;
        }

        /// <summary>
    	/// Gets the byte count
    	/// </summary>
        public long ByteCount
        {
            get
            {
                return byteCount;
            }
        }

        private long byteCount;
    }

    /// <summary>
	/// Event args for ReplyReceived and CommandSent events
	/// </summary>
    public class FTPMessageEventArgs : EventArgs
    {
        /// <summary>
    	/// Constructor
		/// <param name="message">
        /// The message sent to or from the remote host
		/// </param>
    	/// </summary>
        public FTPMessageEventArgs(string message)
        {
            this.message = message;
        }

        /// <summary>
    	/// Gets the message
    	/// </summary>
        public string Message
        {
            get
            {
                return message;
            }
        }

        private string message;
    }

    /// <summary>
	/// Delegate used for the BytesTransferred event
	/// </summary>
    public delegate void BytesTransferredHandler(
        object ftpClient,
        BytesTransferredEventArgs bytesTransferred
    );

    /// <summary>
	/// Delegate used for ReplyReceived and CommandSent events
	/// </summary>
    public delegate void FTPMessageHandler(
        object ftpClient,
        FTPMessageEventArgs message
    );


	/// <summary>
	/// Enumerates the connect modes that are possible, active and passiv e
	/// </summary>
	public enum FTPConnectMode
	{
		/// <member>
		/// Represents active - PORT - connect mode
		/// </member>
		ACTIVE = 1,

		/// <member>
		/// Represents passive - PASV - connect mode
		/// </member>
		PASV = 2
	}

	/// <summary>
	/// Enumerates the transfer types possible. We support only the two common types,
	/// ASCII and Image (often called binary).
	/// </summary>
	public enum FTPTransferType
	{
		/// <member>
		/// Represents ASCII transfer type
		/// </member>
		ASCII = 1,

		/// <member>
		/// Represents Image (or binary) transfer type
		/// </member>
		BINARY = 2
	}

	#endregion

	/// <summary>
	/// Supports client-side FTP. Most common
	/// FTP operations are present in this class.
	/// </summary>
	/// <author>       Bruce Blackshaw
	/// </author>
	/// <version>      $LastChangedRevision$
	/// </version>
	public class FTPClient
	{
		/// <summary> Get the version of edtFTPj
		///
		/// </summary>
		/// <returns> int array of {major,middle,minor} version numbers
		/// </returns>
		public static int[] Version
		{
			get
			{
				return version;
			}

		}
		/// <summary> Get the build timestamp
		///
		/// </summary>
		/// <returns> d-MMM-yyyy HH:mm:ss z build timestamp
		/// </returns>
		public static string BuildTimestamp
		{
			get
			{
				return buildTimestamp;
			}
		}

        /// <summary>
		/// Strict checking of return codes. If it is on
		/// (the default), all return codes must exactly match the expected code.
		/// If strict checking is off, only the first digit must match
		/// </summary>
		/// <returns>
		/// true if strict return code checking, false if non-strict.
		/// </returns>
		virtual public bool StrictReturnCodes
		{
			get
			{
				return strictReturnCodes;
			}

			set
			{
				this.strictReturnCodes = value;
				if (control != null)
					control.StrictReturnCodes = value;
			}

		}
		/// <summary>
		/// Set the TCP timeout on the underlying socket.
		/// </summary>
		virtual public int Timeout
		{
		    get
		    {
		        return timeout;
		    }
			set
			{
				this.timeout = value;
				if (control != null)
				    control.Timeout = value;
			}
		}

		/// <summary>
		/// Get/Set the connect mode
		/// </summary>
		virtual public FTPConnectMode ConnectMode
		{
			set
			{
				connectMode = value;
			}
            get
            {
                return connectMode;
            }
		}

		/// <summary>
		/// Get the bytes transferred between each notification of the
		/// BytesTransferred event. Reduce this value to receive more
		/// frequent notifications of transfer progress
		/// </summary>
		public long TransferNotifyInterval
		{
			get
			{
				return monitorInterval;
			}
			set
			{
			    monitorInterval = value;
			}
		}

        /// <summary>
        /// Get/set the size of the buffers used in writing to and reading from
		/// the data sockets
		/// </summary>
		public int TransferBufferSize
		{
			get
			{
				return transferBufferSize;
			}

			set
			{
				transferBufferSize = value;
			}
		}

		/// <summary>
		/// Get/set the name of the remote host.
		/// </summary>
		/// <remarks>
		/// Can only be set if not currently connected.
		/// </remarks>
		public virtual string RemoteHost
		{
			get
			{
				return remoteHost;
			}
			set
			{
			    CheckConnection(false);
			    remoteHost = value;
			}
		}

		/// <summary>
		/// Get/set the delete on failure flag
        /// </summary>
        /// <remarks>
        /// If true, a partially downloaded file is deleted if there
        /// is a failure during the download. For example, the connection
        /// to the FTP server might have failed. If false, the partially
        /// downloaded file remains on the client machine - and the download
        /// may be resumed, if it is a binary transfer. By default this flag is set to true.
		/// </remarks>
		public bool DeleteOnFailure
		{
			get
			{
				return deleteOnFailure;
			}
			set
			{
			    deleteOnFailure = value;
			}
		}

		/// <summary>
		/// Get/set the controlPort. Can only be set
		/// if not currently connected
		/// </summary>
		public int ControlPort
		{
			get
			{
				return controlPort;
			}
			set
			{
			    CheckConnection(false);
			    controlPort = value;
			}
		}

		/// <summary>
		/// Override the chosen file factory with a user created one - meaning
		/// that a specific parser has been selected
		/// </summary>
		public FTPFileFactory FTPFileFactory
		{
			set
			{
				this.fileFactory = value;
			}
		}

		/// <summary>
		/// Gets the latest valid reply from the server
		/// </summary>
		/// <returns>  reply object encapsulating last valid server response
		/// </returns>
		public FTPReply LastValidReply
		{
			get
			{
				return lastValidReply;
			}
		}

        /// <summary>
		/// Get or set the current transfer type
		/// </summary>
		/// <returns>
		/// the current type of the transfer, i.e. BINARY or ASCII
		/// </returns>
		public FTPTransferType TransferType
		{
			get
			{
				return transferType;
			}
			set
			{
				CheckConnection(true);

				// determine the character to send
				string typeStr = ASCII_CHAR;
				if (value.Equals(FTPTransferType.BINARY))
					typeStr = BINARY_CHAR;

				// send the command
				FTPReply reply = control.SendCommand("TYPE " + typeStr);
				lastValidReply = control.ValidateReply(reply, "200");

				// record the type
				transferType = value;
			}
		}

        /// <summary>
    	/// Event for notifying start of a transfer
    	/// </summary>
        public event EventHandler TransferStarted;

        /// <summary>
    	/// Event for notifying start of a transfer
    	/// </summary>
        public event EventHandler TransferComplete;

        /// <summary>
    	/// Event for notifying start of a transfer
    	/// </summary>
        public event BytesTransferredHandler BytesTransferred;

        /// <summary>
    	/// Event for notifying start of a transfer
    	/// </summary>
        public event FTPMessageHandler CommandSent;

        /// <summary>
    	/// Event for notifying start of a transfer
    	/// </summary>
        public event FTPMessageHandler ReplyReceived;

		/// <summary> Default byte interval for transfer monitor</summary>
		private const int DEFAULT_MONITOR_INTERVAL = 4096;

		/// <summary> Default transfer buffer size</summary>
		private const int DEFAULT_BUFFER_SIZE = 4096;

		/// <summary> Major version (substituted by ant)</summary>
		private static string majorVersion = "1";

		/// <summary> Middle version (substituted by ant)</summary>
		//TODO: uncomment
		private static string middleVersion = "0";

		/// <summary> Middle version (substituted by ant)</summary>
		private static string minorVersion = "0";

		/// <summary> Full version</summary>
		private static int[] version;

		/// <summary> Timestamp of build</summary>
		private static string buildTimestamp = "1/1/2000";

		/// <summary>
		/// The char sent to the server to set BINARY
		/// </summary>
		private static string BINARY_CHAR = "I";

		/// <summary>
		/// The char sent to the server to set ASCII
		/// </summary>
		private static string ASCII_CHAR = "A";

		/// <summary>Date format</summary>
		private static readonly string tsFormat = "yyyyMMddHHmmss";

		/// <summary> Logging object</summary>
		private Logger log;

		/// <summary>  Socket responsible for controlling
		/// the connection
		/// </summary>
		internal FTPControlSocket control = null;

		/// <summary>  Socket responsible for transferring
		/// the data
		/// </summary>
		internal FTPDataSocket data = null;

		/// <summary>  Socket timeout for both data and control. In
		/// milliseconds
		/// </summary>
		internal int timeout = 0;

		/// <summary> Use strict return codes if true</summary>
		private bool strictReturnCodes = true;

		/// <summary>  Can be used to cancel a transfer</summary>
		private bool cancelTransfer = false;

		/// <summary> If true, a file transfer is being resumed</summary>
		private bool resume = false;

		/// <summary>If a download to a file fails, delete the partial file</summary>
		private bool deleteOnFailure = true;

		/// <summary> Resume byte marker point</summary>
		private long resumeMarker = 0;

		/// <summary> Bytes transferred in between monitor callbacks</summary>
		private long monitorInterval = DEFAULT_MONITOR_INTERVAL;

		/// <summary> Size of transfer buffers</summary>
		private int transferBufferSize = DEFAULT_BUFFER_SIZE;

		/// <summary> Parses LIST output</summary>
		private FTPFileFactory fileFactory = null;

		/// <summary>  Record of the transfer type - make the default ASCII</summary>
		private FTPTransferType transferType = FTPTransferType.ASCII;

		/// <summary>  Record of the connect mode - make the default PASV (as this was
		/// the original mode supported)
		/// </summary>
		private FTPConnectMode connectMode = FTPConnectMode.PASV;

		/// <summary>
        /// Holds the last valid reply from the server on the control socket
        /// </summary>
		internal FTPReply lastValidReply;

		/// <summary>
        /// Port on which we connect to the FTP server and messages are passed
        /// </summary>
		internal int controlPort = -1;

		/// <summary>
        /// Remote host we are connecting to
        /// </summary>
		internal string remoteHost = null;

		#region Constructors

		/// <summary>
		/// Constructor. Creates the control socket
		/// </summary>
		/// <param name="remoteHost"> the remote hostname
		/// </param>
		public FTPClient(string remoteHost):
            this(remoteHost, FTPControlSocket.CONTROL_PORT, 0)
		{
		}

		/// <summary>
		/// Constructor. Creates the control socket
		/// </summary>
		/// <param name="remoteHost"> the remote hostname
		/// </param>
		/// <param name="controlPort"> port for control stream (-1 for default port)
		/// </param>
		public FTPClient(string remoteHost, int controlPort):
            this(remoteHost, controlPort, 0)
		{
		}

		/// <summary>
		/// Constructor. Creates the control socket
		/// </summary>
		/// <param name="remoteHost"> the remote hostname
		/// </param>
		/// <param name="controlPort"> port for control stream (use -1 for the default port)
		/// </param>
		/// <param name="timeout">      the length of the timeout, in milliseconds
		/// (pass in 0 for no timeout)
		/// </param>
		public FTPClient(string remoteHost, int controlPort, int timeout):
            this(Dns.Resolve(remoteHost).AddressList[0], controlPort, timeout)
		{
		    this.remoteHost = remoteHost;
		}


		/// <summary>  Constructor. Creates the control
		/// socket
		///
		/// </summary>
		/// <param name="remoteAddr"> the address of the
		/// remote host
		/// </param>
		public FTPClient(IPAddress remoteAddr):
            this(remoteAddr, FTPControlSocket.CONTROL_PORT, 0)
		{
		}


		/// <summary>
		/// Constructor. Creates the control
		/// socket. Allows setting of control port (normally
		/// set by default to 21).
		///
		/// </summary>
		/// <param name="remoteAddr"> the address of the
		/// remote host
		/// </param>
		/// <param name="controlPort"> port for control stream
		/// </param>
		public FTPClient(IPAddress remoteAddr, int controlPort):
            this(remoteAddr, controlPort, 0)
		{
		}

		/// <summary>
		/// Constructor. Creates the control
		/// socket. Allows setting of control port (normally
		/// set by default to 21).
		/// </summary>
		/// <param name="remoteAddr">   the address of the
		/// remote host
		/// </param>
		/// <param name="controlPort">  port for control stream (-1 for default port)
		/// </param>
		/// <param name="timeout">       the length of the timeout, in milliseconds
		/// (pass in 0 for no timeout)
		/// </param>
		public FTPClient(IPAddress remoteAddr, int controlPort, int timeout)
		{
			InitBlock();
			remoteHost = remoteAddr.ToString();
			Connect(remoteAddr, controlPort, timeout);
		}

		/// <summary>
        /// Default constructor for use by subclasses. Does not connect
        /// to the remote host
        /// </summary>
		public FTPClient()
		{
			InitBlock();
		}

		#endregion

		/// <summary>
		/// Instance initializer. Sets formatter to GMT.
		/// </summary>
		private void InitBlock()
		{
			log = Logger.GetLogger(typeof(FTPClient));
			transferType = FTPTransferType.ASCII;
			connectMode = FTPConnectMode.PASV;
			controlPort = FTPControlSocket.CONTROL_PORT;
		}

		/// <summary>
        /// Connect to the remote host. Cannot be currently connected. RemoteHost
        /// property must be set
        /// </summary>
		public virtual void Connect()
		{
	        CheckConnection(false);
	        Connect(Dns.Resolve(remoteHost).AddressList[0], controlPort, timeout);
		}

		internal virtual void Connect(IPAddress remoteAddr, int controlPort, int timeout)
		{
			if (controlPort < 0)
            {
                log.Warn("Invalid control port supplied: " + controlPort + " Using default: " +
                    FTPControlSocket.CONTROL_PORT);
				controlPort = FTPControlSocket.CONTROL_PORT;
			}
			this.controlPort = controlPort;
			log.Debug("Connecting to " + remoteAddr.ToString() + ":" + controlPort);
            Initialize(new FTPControlSocket(remoteAddr, controlPort, timeout));
		}

		/// <summary>
		/// Set the control socket explicitly
		/// </summary>
		/// <param name="control">  control socket reference
		/// </param>
		internal void Initialize(FTPControlSocket control)
		{
			this.control = control;

            // set up the event handlers so they call back to this object - and can
            // then be passed on if required
            control.CommandSent += new FTPMessageHandler(CommandSentControl);
            control.ReplyReceived += new FTPMessageHandler(ReplyReceivedControl);
		}


		/// <summary>
		/// Checks if the client has connected to the server and throws an exception if it hasn't.
		/// This is only intended to be used by subclasses
		/// </summary>
		/// <throws>  FTPException Thrown if the client has not connected to the server. </throws>
		internal virtual void CheckConnection(bool shouldBeConnected)
		{
			if (shouldBeConnected && control == null)
				throw new FTPException("The FTP client has not yet connected to the server.  " +
                "The requested action cannot be performed until after a connection has been established.");
			else if (!shouldBeConnected && control != null)
				throw new FTPException("The FTP client has already been connected to the server.  " +
                "The requested action must be performed before a connection is established.");
		}


        internal void CommandSentControl(object client, FTPMessageEventArgs message)
        {
            if (CommandSent != null)
                CommandSent(this, message);
        }


        internal void ReplyReceivedControl(object client, FTPMessageEventArgs message)
        {
            if (ReplyReceived != null)
                ReplyReceived(this, message);
        }


		/// <summary>
		/// Switch Debug of responses on or off
		/// </summary>
		/// <param name="on"> true if you wish to have responses to
		/// the log stream, false otherwise
		/// </param>
		/// <deprecated>  use the Logger class to switch Debugging on and off
		/// </deprecated>
		public void DebugResponses(bool on)
		{
			if (on)
				Logger.CurrentLevel = Level.DEBUG;
			else
				Logger.CurrentLevel = Level.OFF;
		}


		/// <summary>  Cancels the current transfer. Generally called from a separate
		/// thread. Note that this may leave partially written files on the
		/// server or on local disk, and should not be used unless absolutely
		/// necessary. The server is not notified
		/// </summary>
		public virtual void CancelTransfer()
		{
			cancelTransfer = true;
		}

		/// <summary>
		/// Login into an account on the FTP server. This
		/// call completes the entire login process
		/// </summary>
		/// <param name="user">      user name
		/// </param>
		/// <param name="password">  user's password
		/// </param>
		public virtual void Login(string user, string password)
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("USER " + user);

			// we allow for a site with no password - 230 response
			string[] validCodes = new string[]{"230", "331"};
			lastValidReply = control.ValidateReply(reply, validCodes);
			if (lastValidReply.ReplyCode.Equals("230"))
				return ;
			else
			{
				Password(password);
			}
		}

		/// <summary>  Supply the user name to log into an account
		/// on the FTP server. Must be followed by the
		/// password() method - but we allow for
		///
		/// </summary>
		/// <param name="user">      user name
		/// </param>
		public virtual void User(string user)
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("USER " + user);

			// we allow for a site with no password - 230 response
			string[] validCodes = new string[]{"230", "331"};
			lastValidReply = control.ValidateReply(reply, validCodes);
		}


		/// <summary>
		/// Supplies the password for a previously supplied
		/// username to log into the FTP server. Must be
		/// preceeded by the user() method
		/// </summary>
		/// <param name="password">      The password.
		/// </param>
		public virtual void Password(string password)
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("PASS " + password);

			// we allow for a site with no passwords (202)
			string[] validCodes = new string[]{"230", "202"};
			lastValidReply = control.ValidateReply(reply, validCodes);
		}


		/// <summary>  Issue arbitrary ftp commands to the FTP server.
		///
		/// </summary>
		/// <param name="command">    ftp command to be sent to server
		/// </param>
		/// <param name="validCodes"> valid return codes for this command
		///
		/// </param>
		/// <returns>  the text returned by the FTP server
		/// </returns>
		public virtual string Quote(string command, string[] validCodes)
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand(command);

			// allow for no validation to be supplied
			if (validCodes != null && validCodes.Length > 0)
			{
				lastValidReply = control.ValidateReply(reply, validCodes);
				return lastValidReply.ReplyText;
			}
			else
			{
				throw new FTPException("Valid reply code must be supplied");
			}
		}


		/// <summary>
		/// Get the size of a remote file. This is not a standard FTP command, it
		/// is defined in "Extensions to FTP", a draft RFC
		/// (draft-ietf-ftpext-mlst-16.txt)
		/// </summary>
		/// <param name="remoteFile"> name or path of remote file in current directory
		/// </param>
		/// <returns> size of file in bytes
		/// </returns>
		public virtual long Size(string remoteFile)
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("SIZE " + remoteFile);
			lastValidReply = control.ValidateReply(reply, "213");

			// parse the reply string .
			string replyText = lastValidReply.ReplyText;

			// trim off any trailing characters after a space, e.g. webstar
			// responds to SIZE with 213 55564 bytes
			int spacePos = replyText.IndexOf((System.Char) ' ');
			if (spacePos >= 0)
				replyText = replyText.Substring(0, (spacePos) - (0));

			// parse the reply
			try
			{
				return Int64.Parse(replyText);
			}
			catch (FormatException)
			{
				throw new FTPException("Failed to parse reply: " + replyText);
			}
		}

		/// <summary>
		/// Make the next file transfer (put or get) resume. For puts(), the
		/// bytes already transferred are skipped over, while for gets(), if
		/// writing to a file, it is opened in append mode, and only the bytes
		/// required are transferred.
		///
		/// Currently resume is only supported for BINARY transfers (which is
		/// generally what it is most useful for).
		/// </summary>
		/// <throws>  FTPException </throws>
		public virtual void Resume()
		{
			if (transferType.Equals(FTPTransferType.ASCII))
				throw new FTPException("Resume only supported for BINARY transfers");
			resume = true;
		}

		/// <summary>
        /// Cancel the resume. Use this method if something goes wrong
		/// and the server is left in an inconsistent state
		/// </summary>
		/// <throws>  SystemException </throws>
		/// <throws>  FTPException </throws>
		public virtual void CancelResume()
		{
			Restart(0);
			resume = false;
		}

		/// <summary>
		/// Issue the RESTart command to the remote server
		/// </summary>
		/// <param name="size"> the REST param, the mark at which the restart is
		/// performed on the remote file. For STOR, this is retrieved
		/// by SIZE
		/// </param>
		/// <throws>  SystemException </throws>
		/// <throws>  FTPException </throws>
		private void Restart(long size)
		{
			FTPReply reply = control.SendCommand("REST " + size);
			lastValidReply = control.ValidateReply(reply, "350");
		}


		/// <summary>
		/// Put a local file onto the FTP server. It
		/// is placed in the current directory.
		/// </summary>
		/// <param name="localPath">  path of the local file
		/// </param>
		/// <param name="remoteFile"> name of remote file in
		/// current directory
		/// </param>
		public virtual void Put(string localPath, string remoteFile)
		{
			Put(localPath, remoteFile, false);
		}

		/// <summary>
		/// Put a stream of data onto the FTP server. It
		/// is placed in the current directory.
		/// </summary>
		/// <param name="srcStream">  input stream of data to put
		/// </param>
		/// <param name="remoteFile"> name of remote file in
		/// current directory
		/// </param>
		public virtual void Put(Stream srcStream, string remoteFile)
		{
			Put(srcStream, remoteFile, false);
		}


		/// <summary>  Put a local file onto the FTP server. It
		/// is placed in the current directory. Allows appending
		/// if current file exists
		///
		/// </summary>
		/// <param name="localPath">  path of the local file
		/// </param>
		/// <param name="remoteFile"> name of remote file in
		/// current directory
		/// </param>
		/// <param name="append">     true if appending, false otherwise
		/// </param>
		public virtual void Put(string localPath, string remoteFile, bool append)
		{
			// get according to set type
			if (transferType == FTPTransferType.ASCII)
			{
				PutASCII(localPath, remoteFile, append);
			}
			else
			{
				PutBinary(localPath, remoteFile, append);
			}
			ValidateTransfer();
		}

		/// <summary>
		/// Put a stream of data onto the FTP server. It
		/// is placed in the current directory. Allows appending
		/// if current file exists
		/// </summary>
		/// <param name="srcStream">  input stream of data to put
		/// </param>
		/// <param name="remoteFile"> name of remote file in
		/// current directory
		/// </param>
		/// <param name="append">     true if appending, false otherwise
		/// </param>
		public virtual void Put(Stream srcStream, string remoteFile, bool append)
		{
			// get according to set type
			if (transferType == FTPTransferType.ASCII)
			{
				PutASCII(srcStream, remoteFile, append);
			}
			else
			{
				PutBinary(srcStream, remoteFile, append);
			}
			ValidateTransfer();
		}

		/// <summary>
		/// Validate that the Put() or get() was successful.  This method is not
		/// for general use.
		/// </summary>
		public virtual void ValidateTransfer()
		{
			CheckConnection(true);

			// check the control response
			string[] validCodes = new string[]{"225", "226", "250", "426", "450"};
			FTPReply reply = control.ReadReply();

			// permit 426/450 error if we cancelled the transfer, otherwise
			// throw an exception
			string code = reply.ReplyCode;
			if ((code.Equals("426") || code.Equals("450")) && !cancelTransfer)
				throw new FTPException(reply);

			lastValidReply = control.ValidateReply(reply, validCodes);
		}

		/// <summary>
		/// Close the data socket
		/// </summary>
		/// <param name="stream">
		/// stream to close
		/// </param>
		private void CloseDataSocket(Stream stream)
		{
            if (stream != null) {
                try {
                    stream.Close();
                }
                catch (SystemException ex)
    			{
    				log.Warn("Caught exception closing data socket", ex);
    			}
            }

			CloseDataSocket();
		}

		/// <summary>
		/// Close the data socket
		/// </summary>
		private void CloseDataSocket()
		{
			if (data != null)
			{
				try
				{
					data.Close();
					data = null;
				}
				catch (SystemException ex)
				{
					log.Warn("Caught exception closing data socket", ex);
				}
			}
		}

		/// <summary>
		/// Request the server to set up the put
		/// </summary>
		/// <param name="remoteFile"> name of remote file in
		/// current directory
		/// </param>
		/// <param name="append">     true if appending, false otherwise
		/// </param>
		private void InitPut(string remoteFile, bool append)
		{
			CheckConnection(true);

			// reset the cancel flag
			cancelTransfer = false;

			bool close = false;
			data = null;
			try
			{
				// set up data channel
				data = control.CreateDataSocket(connectMode);
				data.Timeout = timeout;

				// if resume is requested, we must obtain the size of the
				// remote file and issue REST
				if (resume)
				{
					if (transferType.Equals(FTPTransferType.ASCII))
						throw new FTPException("Resume only supported for BINARY transfers");
					resumeMarker = Size(remoteFile);
					Restart(resumeMarker);
				}

				// send the command to store
				string cmd = append?"APPE ":"STOR ";
				FTPReply reply = control.SendCommand(cmd + remoteFile);

				// Can get a 125 or a 150
				string[] validCodes = new string[]{"125", "150"};
				lastValidReply = control.ValidateReply(reply, validCodes);
			}
			catch (SystemException ex)
			{
				close = true;
				throw ex;
			}
			catch (FTPException ex)
			{
				close = true;
				throw ex;
			}
			finally
			{
				if (close)
				{
					resume = false;
					CloseDataSocket();
				}
			}
		}


		/// <summary>
		/// Put as ASCII, i.e. read a line at a time and write
		/// inserting the correct FTP separator
		/// </summary>
		/// <param name="localPath">  full path of local file to read from
		/// </param>
		/// <param name="remoteFile"> name of remote file we are writing to
		/// </param>
		/// <param name="append">     true if appending, false otherwise
		/// </param>
		private void PutASCII(string localPath, string remoteFile, bool append)
		{
			// create an inputstream & pass to common method
            Stream srcStream = new FileStream(localPath, FileMode.Open, FileAccess.Read);
			PutASCII(srcStream, remoteFile, append);
		}

		/// <summary>
		/// Put as ASCII, i.e. read a line at a time and write
		/// inserting the correct FTP separator
		/// </summary>
		/// <param name="srcStream">  input stream of data to put
		/// </param>
		/// <param name="remoteFile"> name of remote file we are writing to
		/// </param>
		/// <param name="append">     true if appending, false otherwise
		/// </param>
		private void PutASCII(Stream srcStream, string remoteFile, bool append)
		{
			// need to read line by line ...
			StreamReader input = null;
			StreamWriter output = null;
			SystemException storedEx = null;
			long size = 0;
			try
			{
                input = new StreamReader(srcStream);

				InitPut(remoteFile, append);

				// get an character output stream to write to ... AFTER we
				// have the ok to go ahead AND AFTER we've successfully opened a
				// stream for the local file
				output = new StreamWriter(data.DataStream);

				if (TransferStarted != null)
				    TransferStarted(this, new EventArgs());

				// write \r\n as required by RFC959 after each line
				long monitorCount = 0;
                string line = null;
                while ((line = input.ReadLine()) != null && !cancelTransfer)
                {
                    size += line.Length;
                    monitorCount += line.Length;
                    output.Write(line);
                    output.Write(FTPControlSocket.EOL);

					if (BytesTransferred != null && monitorCount > monitorInterval)
					{
					    BytesTransferred(this, new BytesTransferredEventArgs(size));
						monitorCount = 0;
					}
				}
			}
			catch (SystemException ex)
			{
				storedEx = ex;
			}
			finally
			{
				try
				{
					if (input != null)
						input.Close();
				}
				catch (SystemException ex)
				{
                    log.Warn("Caught exception closing stream", ex);
				}

				try
				{
				    if (output != null)
					   output.Close();
				}
				catch (SystemException ex)
				{
					log.Warn("Caught exception closing data socket", ex);
				}

                // if we did get an exception bail out now
				if (storedEx != null)
				    throw storedEx;


				// notify the final transfer size
				if (BytesTransferred != null)
				    BytesTransferred(this, new BytesTransferredEventArgs(size));
				if (TransferComplete != null)
				    TransferComplete(this, new EventArgs());

			}
		}


		/// <summary>
		/// Put as binary, i.e. read and write raw bytes
		/// </summary>
		/// <param name="localPath">
		/// full path of local file to read from
		/// </param>
		/// <param name="remoteFile">
		/// name of remote file we are writing to
		/// </param>
		/// <param name="append">
		/// true if appending, false otherwise
		/// </param>
		private void PutBinary(string localPath, string remoteFile, bool append)
		{

			// open input stream to read source file ... do this
			// BEFORE opening output stream to server, so if file not
			// found, an exception is thrown
			Stream srcStream = new FileStream(localPath, FileMode.Open, FileAccess.Read);
			PutBinary(srcStream, remoteFile, append);
		}

		/// <summary>
		/// Put as binary, i.e. read and write raw bytes
		/// </summary>
		/// <param name="srcStream">  input stream of data to put
		/// </param>
		/// <param name="remoteFile"> name of remote file we are writing to
		/// </param>
		/// <param name="append">     true if appending, false otherwise
		/// </param>
		private void PutBinary(Stream srcStream, string remoteFile, bool append)
		{
			BufferedStream input = null;
			BinaryWriter output = null;
			SystemException storedEx = null;
			long size = 0;
			try
			{
				input = new BufferedStream(srcStream);

				InitPut(remoteFile, append);

				// get an output stream
				output = new BinaryWriter(data.DataStream);

				// if resuming, we skip over the unwanted bytes
				if (resume)
				{
                    input.Seek(resumeMarker, SeekOrigin.Current);
				}

				byte[] buf = new byte[transferBufferSize];

				if (TransferStarted != null)
				    TransferStarted(this, new EventArgs());

				// read a chunk at a time and write to the data socket
				long monitorCount = 0;
				int count = 0;
				while ((count = input.Read(buf, 0, buf.Length)) > 0 && !cancelTransfer)
				{
                    output.Write(buf, 0, count);
					size += count;
					monitorCount += count;
					if (BytesTransferred != null && monitorCount > monitorInterval)
					{
    				    BytesTransferred(this, new BytesTransferredEventArgs(size));
						monitorCount = 0;
					}
				}
			}
			catch (SystemException ex)
			{
				storedEx = ex;
			}
			finally
			{
				resume = false;
				try
				{
					if (input != null)
						input.Close();
				}
				catch (SystemException ex)
				{
				    log.Warn("Caught exception closing stream", ex);
				}

				try
				{
				    if (output != null)
					   output.Close();
				}
				catch (SystemException ex)
				{
					log.Warn("Caught exception closing data socket", ex);
				}

				// if we did get an exception bail out now
				if (storedEx != null)
				    throw storedEx;

				// notify the final transfer size
				if (BytesTransferred != null)
				    BytesTransferred(this, new BytesTransferredEventArgs(size));
				if (TransferComplete != null)
				    TransferComplete(this, new EventArgs());

				// log bytes transferred
				log.Debug("Transferred " + size + " bytes to remote host");
			}
		}


		/// <summary>
		/// Put data onto the FTP server. It
		/// is placed in the current directory.
		/// </summary>
		/// <param name="bytes">       array of bytes
		/// </param>
		/// <param name="remoteFile"> name of remote file in
		/// current directory
		/// </param>
		public virtual void Put(byte[] bytes, string remoteFile)
		{
			Put(bytes, remoteFile, false);
		}

		/// <summary>
		/// Put data onto the FTP server. It
		/// is placed in the current directory. Allows
		/// appending if current file exists
		/// </summary>
		/// <param name="bytes">       array of bytes
		/// </param>
		/// <param name="remoteFile"> name of remote file in
		/// current directory
		/// </param>
		/// <param name="append">     true if appending, false otherwise
		/// </param>
		public virtual void Put(byte[] bytes, string remoteFile, bool append)
		{
			InitPut(remoteFile, append);

			// get an output stream
			BinaryWriter output = new BinaryWriter(data.DataStream);

			try
			{
				// write array
				output.Write(bytes, 0, bytes.Length);
			}
			finally
			{
                try
                {
                    output.Close();
                }
                catch (SystemException ex)
    			{
    				log.Warn("Caught exception closing data socket", ex);
    			}
			}

			ValidateTransfer();
		}


		/// <summary>
		/// Get data from the FTP server. Uses the currently
		/// set transfer mode.
		/// </summary>
		/// <param name="localPath">  local file to put data in
		/// </param>
		/// <param name="remoteFile"> name of remote file in
		/// current directory
		/// </param>
		public virtual void Get(string localPath, string remoteFile)
		{
			// get according to set type
			if (transferType == FTPTransferType.ASCII)
			{
				GetASCII(localPath, remoteFile);
			}
			else
			{
				GetBinary(localPath, remoteFile);
			}
			ValidateTransfer();
		}

		/// <summary>
		/// Get data from the FTP server, using the currently
		/// set transfer mode.
		/// </summary>
		/// <param name="destStream"> data stream to write data to
		/// </param>
		/// <param name="remoteFile"> name of remote file in
		/// current directory
		/// </param>
		public virtual void Get(Stream destStream, string remoteFile)
		{
			// get according to set type
			if (transferType == FTPTransferType.ASCII)
			{
				GetASCII(destStream, remoteFile);
			}
			else
			{
				GetBinary(destStream, remoteFile);
			}
		    ValidateTransfer();
		}


		/// <summary>
		/// Request to the server that the get is set up
		/// </summary>
		/// <param name="remoteFile"> name of remote file
		/// </param>
		private void InitGet(string remoteFile)
		{
			CheckConnection(true);

			// reset the cancel flag
			cancelTransfer = false;

			bool close = false;
			data = null;
			try
			{
				// set up data channel
				data = control.CreateDataSocket(connectMode);
				data.Timeout = timeout;

				// if resume is requested, we must issue REST
				if (resume)
				{
					if (transferType.Equals(FTPTransferType.ASCII))
						throw new FTPException("Resume only supported for BINARY transfers");
					Restart(resumeMarker);
				}

				// send the retrieve command
				FTPReply reply = control.SendCommand("RETR " + remoteFile);

				// Can get a 125 or a 150
				string[] validCodes1 = new string[]{"125", "150"};
				lastValidReply = control.ValidateReply(reply, validCodes1);
			}
			catch (SystemException ex)
			{
				close = true;
				throw ex;
			}
			catch (FTPException ex)
			{
				close = true;
				throw ex;
			}
			finally
			{
				if (close)
				{
					resume = false;
					CloseDataSocket();
				}
			}
		}


		/// <summary>
		/// Get as ASCII, i.e. read a line at a time and write
		/// using the correct newline separator for the OS
		/// </summary>
		/// <param name="localPath">  full path of local file to write to
		/// </param>
		/// <param name="remoteFile"> name of remote file
		/// </param>
		private void GetASCII(string localPath, string remoteFile)
		{
			// Call InitGet() before creating the FileOutputStream.
			// This will prevent being left with an empty file if a FTPException
			// is thrown by InitGet().
			InitGet(remoteFile);

			SystemException storedEx = null;
			long size = 0;

			// Need to store the local file name so the file can be
			// deleted if necessary.
			FileInfo localFile = new FileInfo(localPath);

			// create the buffered stream for writing
			StreamWriter output = new StreamWriter(localPath);

			// get an character input stream to read data from ... AFTER we
			// have the ok to go ahead AND AFTER we've successfully opened a
			// stream for the local file
			StreamReader input = null;
			try
			{
			    input = new StreamReader(data.DataStream);

    			// If we are in active mode we have to set the timeout of the passive
    			// socket. We can achieve this by setting Timeout again.
    			// If we are in passive mode then we are merely setting the value twice
    			// which does no harm anyway. Doing this simplifies any logic changes.
    			data.Timeout = timeout;

    			if (TransferStarted != null)
    			    TransferStarted(this, new EventArgs());

    			// output a new line after each received newline
    			long monitorCount = 0;
                string line = null;
                while ((line = ReadLine(input)) != null && !cancelTransfer)
                {
                    size += line.Length;
                    monitorCount += line.Length;
                    output.WriteLine(line);

					if (BytesTransferred != null && monitorCount > monitorInterval)
					{
    				    BytesTransferred(this, new BytesTransferredEventArgs(size));
						monitorCount = 0;
					}
				}
                // if asked to transfer, abort
                if (cancelTransfer)
                    Abort();
			}
			catch (SystemException ex)
			{
				storedEx = ex;
			}

		    try
		    {
			    output.Close();
			}
			catch (SystemException ex)
			{
				log.Warn("Caught exception closing output stream", ex);
			}

            try {
                if (input != null)
                    input.Close();
            }
            catch (SystemException ex)
			{
				log.Warn("Caught exception closing data socket", ex);
			}

			// if we failed to write the file, rethrow the exception
			if (storedEx != null)
			{
                // delete the partial file if failure occurred
			    if (deleteOnFailure)
			        localFile.Delete();
				throw storedEx;
			}

            if (BytesTransferred != null)
				BytesTransferred(this, new BytesTransferredEventArgs(size));
			if (TransferComplete != null)
			    TransferComplete(this, new EventArgs());

		}

		/// <summary>
		/// Get as ASCII, i.e. read a line at a time and write
		/// using the correct newline separator for the OS
		/// </summary>
		/// <param name="destStream"> data stream to write data to
		/// </param>
		/// <param name="remoteFile"> name of remote file
		/// </param>
		private void GetASCII(Stream destStream, string remoteFile)
		{
			InitGet(remoteFile);

			// create the buffered stream for writing
            StreamWriter output = new StreamWriter(destStream);

			// get an character input stream to read data from ... AFTER we
			// have the ok to go ahead
            StreamReader input = null;
            SystemException storedEx = null;
			long size = 0;
            try
            {
                input = new StreamReader(data.DataStream);

    			// B. McKeown:
    			// If we are in active mode we have to set the timeout of the passive
    			// socket. We can achieve this by setting Timeout again.
    			// If we are in passive mode then we are merely setting the value twice
    			// which does no harm anyway. Doing this simplifies any logic changes.
    			data.Timeout = timeout;

    			if (TransferStarted != null)
    			    TransferStarted(this, new EventArgs());

    			// output a new line after each received newline
    			long monitorCount = 0;
                string line = null;
                while ((line = ReadLine(input)) != null && !cancelTransfer)
                {
                    size += line.Length;
                    monitorCount += line.Length;
                    output.WriteLine(line);

                    if (BytesTransferred != null && monitorCount > monitorInterval)
					{
					    BytesTransferred(this, new BytesTransferredEventArgs(size));
						monitorCount = 0;
					}
				}
                // if asked to transfer, abort
                if (cancelTransfer)
                    Abort();
			}
			catch (SystemException ex)
			{
				storedEx = ex;
			}

            try {
                output.Close();
            }
            catch (SystemException ex)
			{
				log.Warn("Caught exception closing data socket", ex);
			}

            try {
                if (input != null)
                    input.Close();
            }
            catch (SystemException ex)
			{
				log.Warn("Caught exception closing data socket", ex);
			}

			// if we failed to write the file, rethrow the exception
			if (storedEx != null)
				throw storedEx;

			if (BytesTransferred != null)
				BytesTransferred(this, new BytesTransferredEventArgs(size));
			if (TransferComplete != null)
			    TransferComplete(this, new EventArgs());
		}


		/// <summary>
		/// Get as binary file, i.e. straight transfer of data
		/// </summary>
		/// <param name="localPath">  full path of local file to write to
		/// </param>
		/// <param name="remoteFile"> name of remote file
		/// </param>
		private void GetBinary(string localPath, string remoteFile)
		{
			// B. McKeown: Need to store the local file name so the file can be
			// deleted if necessary.
			FileInfo localFile = new FileInfo(localPath);

			// if resuming, we must find the marker
            if (localFile.Exists && resume)
				resumeMarker = localFile.Length;

			// B.McKeown:
			// Call InitGet() before creating the FileOutputStream.
			// This will prevent being left with an empty file if a FTPException
			// is thrown by InitGet().
			InitGet(remoteFile);

			// create the output stream for writing the file
            FileMode mode = resume ? FileMode.Append : FileMode.Create;
            BinaryWriter output = new BinaryWriter(new FileStream(localPath, mode));

			// get an input stream to read data from ... AFTER we have
			// the ok to go ahead AND AFTER we've successfully opened a
			// stream for the local file
            BinaryReader input = null;
            long size = 0;
            SystemException storedEx = null;
            try
            {
                input = new BinaryReader(data.DataStream);

    			// B. McKeown:
    			// If we are in active mode we have to set the timeout of the passive
    			// socket. We can achieve this by calling setTimeout() again.
    			// If we are in passive mode then we are merely setting the value twice
    			// which does no harm anyway. Doing this simplifies any logic changes.
    			data.Timeout = timeout;

    			if (TransferStarted != null)
    			    TransferStarted(this, new EventArgs());

    			// do the retrieving
    			long monitorCount = 0;
    			byte[] chunk = new byte[transferBufferSize];
    			int count;

    			// read from socket & write to file in chunks
				while ((count = ReadChunk(input, chunk, transferBufferSize)) > 0 && !cancelTransfer)
				{
					output.Write(chunk, 0, count);
					size += count;
					monitorCount += count;

					if (BytesTransferred != null && monitorCount > monitorInterval)
					{
						BytesTransferred(this, new BytesTransferredEventArgs(size));
						monitorCount = 0;
					}
				}
                 // if asked to transfer, abort
                if (cancelTransfer)
                    Abort();
			}
			catch (SystemException ex)
			{
				storedEx = ex;
			}

			resume = false;

            try {
                output.Close();
            }
            catch (SystemException ex)
			{
				log.Warn("Caught exception closing stream", ex);
			}

            try {
                if (input != null)
                    input.Close();
            }
            catch (SystemException ex)
			{
				log.Warn("Caught exception closing data socket", ex);
			}

			// if we failed to write the file, rethrow the exception
			if (storedEx != null)
			{
			    // delete the partial file if failure occurred
			    if (deleteOnFailure)
			       localFile.Delete();
				throw storedEx;
			}

            if (BytesTransferred != null)
				BytesTransferred(this, new BytesTransferredEventArgs(size));
			if (TransferComplete != null)
			    TransferComplete(this, new EventArgs());

			// log bytes transferred
			log.Debug("Transferred " + size + " bytes from remote host");
		}

		/// <summary>
		/// Get as binary file, i.e. straight transfer of data
		/// </summary>
		/// <param name="destStream"> stream to write to
		/// </param>
		/// <param name="remoteFile"> name of remote file
		/// </param>
		private void GetBinary(Stream destStream, string remoteFile)
		{
			InitGet(remoteFile);

			// create the buffered output stream for writing the file
			BufferedStream output = new BufferedStream(destStream);

			// get an input stream to read data from ... AFTER we have
			// the ok to go ahead AND AFTER we've successfully opened a
			// stream for the local file
			BinaryReader input = null;
			long size = 0;
            SystemException storedEx = null;
            try
            {
                input = new BinaryReader(data.DataStream);

    			// B. McKeown:
    			// If we are in active mode we have to set the timeout of the passive
    			// socket. We can achieve this by calling setTimeout() again.
    			// If we are in passive mode then we are merely setting the value twice
    			// which does no harm anyway. Doing this simplifies any logic changes.
    			data.Timeout = timeout;

    			if (TransferStarted != null)
    			    TransferStarted(this, new EventArgs());

    			// do the retrieving
    			long monitorCount = 0;
    			byte[] chunk = new byte[transferBufferSize];
    			int count;

    			// read from socket & write to file in chunks
				while ((count = ReadChunk(input, chunk, transferBufferSize)) > 0 && !cancelTransfer)
				{
					output.Write(chunk, 0, count);
					size += count;
					monitorCount += count;

					if (BytesTransferred != null && monitorCount > monitorInterval)
					{
						BytesTransferred(this, new BytesTransferredEventArgs(size));
						monitorCount = 0;
					}
				}
                // if asked to transfer, abort
                if (cancelTransfer)
                    Abort();
			}
			catch (SystemException ex)
			{
				storedEx = ex;
			}

            try {
                output.Close();
            }
            catch (SystemException ex)
			{
				log.Warn("Caught exception closing stream", ex);
			}

            try {
                if (input != null)
                    input.Close();
            }
            catch (SystemException ex)
			{
				log.Warn("Caught exception closing data socket", ex);
			}

			// if we failed to write to the stream, rethrow the exception
			if (storedEx != null)
				throw storedEx;

            if (BytesTransferred != null)
				BytesTransferred(this, new BytesTransferredEventArgs(size));
			if (TransferComplete != null)
			    TransferComplete(this, new EventArgs());

			// log bytes transferred
			log.Debug("Transferred " + size + " bytes from remote host");
		}

		/// <summary>
		/// Get data from the FTP server.
        /// </summary>
        /// <remarks>
        /// Transfers in whatever mode we are in. Retrieve as a byte array. Note
		/// that we may experience memory limitations as the
		/// entire file must be held in memory at one time.
		/// </remarks>
		/// <param name="remoteFile"> name of remote file in
		/// current directory
		/// </param>
		public virtual byte[] Get(string remoteFile)
		{
			InitGet(remoteFile);

			// get an input stream to read data from
			BinaryReader input = new BinaryReader(data.DataStream);
            long size = 0;
            SystemException storedEx = null;
            MemoryStream temp = null;
            try
            {
    			// B. McKeown:
    			// If we are in active mode we have to set the timeout of the passive
    			// socket. We can achieve this by calling setTimeout() again.
    			// If we are in passive mode then we are merely setting the value twice
    			// which does no harm anyway. Doing this simplifies any logic changes.
    			data.Timeout = timeout;

    			// do the retrieving
    			long monitorCount = 0;
    			byte[] chunk = new byte[transferBufferSize]; // read chunks into
    			temp = new MemoryStream(transferBufferSize); // temp swap buffer
    			int count; // size of chunk read

    			if (TransferStarted != null)
    			    TransferStarted(this, new EventArgs());

    			// read from socket & write to file
    			while ((count = ReadChunk(input, chunk, transferBufferSize)) > 0 && !cancelTransfer)
    			{
    				temp.Write(chunk, 0, count);
    				size += count;
    				monitorCount += count;

    				if (BytesTransferred != null && monitorCount > monitorInterval)
    				{
    					BytesTransferred(this, new BytesTransferredEventArgs(size));
    					monitorCount = 0;
    				}
    			}
                // if asked to transfer, abort
                if (cancelTransfer)
                    Abort();
    		}
			catch (SystemException ex)
			{
				storedEx = ex;
			}

            try
            {
                if (temp != null)
                    temp.Close();
            }
            catch (SystemException ex)
			{
				log.Warn("Caught exception closing stream", ex);
			}

            try
            {
                input.Close();
            }
            catch (SystemException ex)
			{
				log.Warn("Caught exception closing data socket", ex);
			}

	     	// if we failed to write to the stream, rethrow the exception
			if (storedEx != null)
				throw storedEx;

			// notify final transfer size
			if (BytesTransferred != null)
				BytesTransferred(this, new BytesTransferredEventArgs(size));
			if (TransferComplete != null)
			    TransferComplete(this, new EventArgs());

			ValidateTransfer();

			return temp.ToArray();;
		}


		/// <summary>
		/// Run a site-specific command on the
		/// server. Support for commands is dependent
		/// on the server
		/// </summary>
		/// <param name="command">  the site command to run
		/// </param>
		/// <returns> true if command ok, false if
		/// command not implemented
		/// </returns>
		public virtual bool Site(string command)
		{
			CheckConnection(true);

			// send the retrieve command
			FTPReply reply = control.SendCommand("SITE " + command);

			// Can get a 200 (ok) or 202 (not impl). Some
			// FTP servers return 502 (not impl)
			string[] validCodes = new string[]{"200", "202", "502"};
			lastValidReply = control.ValidateReply(reply, validCodes);

			// return true or false? 200 is ok, 202/502 not
			// implemented
			if (reply.ReplyCode.Equals("200"))
				return true;
			else
				return false;
		}


		/// <summary>
		/// List a directory's contents as an array of FTPFile objects.
		/// Should work for Windows and most Unix FTP servers - let us know
		/// about unusual formats (support@enterprisedt.com)
		/// </summary>
		/// <param name="dirname"> name of directory OR filemask
		/// </param>
		/// <returns>  an array of FTPFile objects
		/// </returns>
		public virtual FTPFile[] DirDetails(string dirname)
        {
			// create the factory
			if (fileFactory == null)
				fileFactory = new FTPFileFactory(GetSystem());

			// get the details and parse
			return fileFactory.Parse(Dir(dirname, true));
		}

		/// <summary>
		/// List current directory's contents as an array of strings of
		/// filenames.
		/// </summary>
		/// <returns>  an array of current directory listing strings
		/// </returns>
		public virtual string[] Dir()
		{
			return Dir(null, false);
		}

		/// <summary>
		/// List a directory's contents as an array of strings of filenames.
		/// </summary>
		/// <param name="dirname"> name of directory OR filemask
		/// </param>
		/// <returns>  an array of directory listing strings
		/// </returns>
		public virtual string[] Dir(string dirname)
		{
			return Dir(dirname, false);
		}


		/// <summary>
		/// List a directory's contents as an array of strings. A detailed
		/// listing is available, otherwise just filenames are provided.
		/// The detailed listing varies in details depending on OS and
		/// FTP server. Note that a full listing can be used on a file
		/// name to obtain information about a file
		/// </summary>
		/// <param name="dirname"> name of directory OR filemask
		/// </param>
		/// <param name="full">    true if detailed listing required
		/// false otherwise
		/// </param>
		/// <returns>  an array of directory listing strings
		/// </returns>
		public virtual string[] Dir(string dirname, bool full)
        {
			CheckConnection(true);

			// set up data channel
			data = control.CreateDataSocket(connectMode);
			data.Timeout = timeout;

			// send the retrieve command
			string command = full?"LIST ":"NLST ";
			if (dirname != null)
				command += dirname;

			// some FTP servers bomb out if NLST has whitespace appended
			command = command.Trim();
			FTPReply reply = control.SendCommand(command);

			// check the control response. wu-ftp returns 550 if the
			// directory is empty, so we handle 550 appropriately. Similarly
			// proFTPD returns 450
			string[] validCodes1 = new string[]{"125", "150", "450", "550"};
			lastValidReply = control.ValidateReply(reply, validCodes1);

			// an empty array of files for 450/550
			string[] result = new string[0];

			// a normal reply ... extract the file list
			string replyCode = lastValidReply.ReplyCode;
			if (!replyCode.Equals("450") && !replyCode.Equals("550"))
			{
				// get a character input stream to read data from .
                StreamReader input = new StreamReader(data.DataStream);

				// read a line at a time
				ArrayList lines = new ArrayList(10);
				string line = null;
				while ((line = ReadLine(input)) != null)
				{
					lines.Add(line);
				}
                try {
                    input.Close();
                }
                catch (SystemException ex)
    			{
    				log.Warn("Caught exception closing data socket", ex);
    			}
				CloseDataSocket();

				// check the control response
				string[] validCodes2 = new string[]{"226", "250"};
				reply = control.ReadReply();
				lastValidReply = control.ValidateReply(reply, validCodes2);

				// empty array is default
				if (!(lines.Count == 0))
				{
					result = new string[lines.Count];
					lines.CopyTo(result);
				}
			}
			else
			{
				// 450 or 550 - still need to close data socket
				CloseDataSocket();
			}
			return result;
		}

		/// <summary>
		/// Attempts to read a specified number of bytes from the given
		/// <code>BufferedStream</code> and place it in the given byte-array.
		/// The purpose of this method is to permit subclasses to execute
		/// any additional code necessary when performing this operation.
		/// </summary>
		/// <param name="input">The <code>BinaryReader</code> to read from.
		/// </param>
		/// <param name="chunk">The byte-array to place read bytes in.
		/// </param>
		/// <param name="chunksize">Number of bytes to read.
		/// </param>
		/// <returns> Number of bytes actually read.
		/// </returns>
		/// <throws>  SystemException Thrown if there was an error while reading. </throws>
		internal virtual int ReadChunk(BinaryReader input, byte[] chunk, int chunksize)
		{
			return input.Read(chunk, 0, chunksize);
		}

		/// <summary>
		/// Attempts to read a single character from the given <code>StreamReader</code>.
		/// The purpose of this method is to permit subclasses to execute
		/// any additional code necessary when performing this operation.
		/// </summary>
		/// <param name="input">The <code>StreamReader</code> to read from.
		/// </param>
		/// <returns> The character read.
		/// </returns>
		/// <throws>  SystemException Thrown if there was an error while reading. </throws>
		internal virtual int ReadChar(StreamReader input)
		{
			return input.Read();
		}

		/// <summary>
		/// Attempts to read a single line from the given <code>StreamReader</code>.
		/// The purpose of this method is to permit subclasses to execute
		/// any additional code necessary when performing this operation.
		/// </summary>
		/// <param name="input">The <code>StreamReader</code> to read from.
		/// </param>
		/// <returns> The string read.
		/// </returns>
		/// <throws>
        /// SystemException Thrown if there was an error while reading.
        /// </throws>
		internal virtual string ReadLine(StreamReader input)
		{
			return input.ReadLine();
		}

		/// <summary>
		/// Delete the specified remote file
		/// </summary>
		/// <param name="remoteFile"> name of remote file to
		/// delete
		/// </param>
		public virtual void Delete(string remoteFile)
		{
			CheckConnection(true);
			string[] validCodes = new string[]{"200", "250"};
			FTPReply reply = control.SendCommand("DELE " + remoteFile);
			lastValidReply = control.ValidateReply(reply, validCodes);
		}


		/// <summary>
		/// Rename a file or directory
		/// </summary>
		/// <param name="from"> name of file or directory to rename
		/// </param>
		/// <param name="to">   intended name
		/// </param>
		public virtual void Rename(string from, string to)
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("RNFR " + from);
			lastValidReply = control.ValidateReply(reply, "350");

			reply = control.SendCommand("RNTO " + to);
			lastValidReply = control.ValidateReply(reply, "250");
		}


		/// <summary>
		/// Delete the specified remote working directory
		/// </summary>
		/// <param name="dir"> name of remote directory to
		/// delete
		/// </param>
		public virtual void RmDir(string dir)
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("RMD " + dir);

			// some servers return 200,257, technically incorrect but
			// we cater for it ...
			string[] validCodes = new string[]{"200", "250", "257"};
			lastValidReply = control.ValidateReply(reply, validCodes);
		}


		/// <summary>
		/// Create the specified remote working directory
		/// </summary>
		/// <param name="dir"> name of remote directory to
		/// create
		/// </param>
		public virtual void MkDir(string dir)
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("MKD " + dir);

			// some servers return 200,257, technically incorrect but
			// we cater for it ...
			string[] validCodes = new string[]{"200", "250", "257"};
			lastValidReply = control.ValidateReply(reply, validCodes);
		}


		/// <summary>
		/// Change the remote working directory to that supplied
		/// </summary>
		/// <param name="dir"> name of remote directory to
		/// change to
		/// </param>
		public virtual void ChDir(string dir)
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("CWD " + dir);
			lastValidReply = control.ValidateReply(reply, "250");
		}

		/// <summary>
		/// Get modification time for a remote file
		/// </summary>
		/// <param name="remoteFile">  name of remote file
		/// </param>
		/// <returns>
		/// modification time of file as a date
		/// </returns>
		public virtual DateTime ModTime(string remoteFile)
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("MDTM " + remoteFile);
			lastValidReply = control.ValidateReply(reply, "213");

			// parse the reply string ...
			DateTime ts = DateTime.ParseExact(lastValidReply.ReplyText,
                                              tsFormat,
                                              CultureInfo.CurrentCulture.DateTimeFormat);
            return ts.ToUniversalTime();
		}

		/// <summary>
		/// Get the current remote working directory
		/// </summary>
		/// <returns>   the current working directory
		/// </returns>
		public virtual string Pwd()
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("PWD");
			lastValidReply = control.ValidateReply(reply, "257");

			// get the reply text and extract the dir
			// listed in quotes, if we can find it. Otherwise
			// just return the whole reply string
			string text = lastValidReply.ReplyText;
			int start = text.IndexOf((System.Char) '"');
			int end = text.LastIndexOf((System.Char) '"');
			if (start >= 0 && end > start)
				return text.Substring(start + 1, (end) - (start + 1));
			else
				return text;
		}


		/// <summary>
		/// Get the server supplied features
		/// </summary>
		/// <returns>
		/// string containing server features, or null if no features or not
		/// supported
		/// </returns>
		public virtual string[] Features()
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("FEAT");
			string[] validCodes = new string[]{"211", "500", "502"};
			lastValidReply = control.ValidateReply(reply, validCodes);
			if (lastValidReply.ReplyCode.Equals("211"))
				return lastValidReply.ReplyData;
			else
				throw new FTPException(reply);
		}

		/// <summary>
		/// Get the type of the OS at the server
		/// </summary>
		/// <returns>   the type of server OS
		/// </returns>
		public virtual string GetSystem()
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("SYST");
			lastValidReply = control.ValidateReply(reply, "215");
			return lastValidReply.ReplyText;
		}

		/// <summary>  Get the help text for the specified command
		///
		/// </summary>
		/// <param name="command"> name of the command to get help on
		/// </param>
		/// <returns> help text from the server for the supplied command
		/// </returns>
		public virtual string Help(string command)
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("HELP " + command);
			string[] validCodes = new string[]{"211", "214"};
			lastValidReply = control.ValidateReply(reply, validCodes);
			return lastValidReply.ReplyText;
		}

		/// <summary>
		/// Abort the current action
		/// </summary>
		protected virtual void Abort()
		{
			CheckConnection(true);

			FTPReply reply = control.SendCommand("ABOR");
			string[] validCodes = new string[]{"426", "226"};
			lastValidReply = control.ValidateReply(reply, validCodes);
		}

		/// <summary>
		/// Quit the FTP session
		/// </summary>
		public virtual void Quit()
		{
			CheckConnection(true);

			fileFactory = null;
			try
			{
				FTPReply reply = control.SendCommand("QUIT");
				string[] validCodes = new string[]{"221", "226"};
				lastValidReply = control.ValidateReply(reply, validCodes);
			}
			finally
			{
				// ensure we clean up the connection
				control.Logout();
				control = null;
			}
		}

        /// <summary>
		/// Quit the FTP session immediately by closing the control socket
		/// without sending the QUIT command
		/// </summary>
        public virtual void QuitImmediately()
        {
            CheckConnection(true);

			fileFactory = null;

            control.Logout();
			control = null;
        }


		/// <summary>
        /// Work out the version array
        /// </summary>
		static FTPClient()
		{
			{
				try
				{
					version = new int[3];
					version[0] = Int32.Parse(majorVersion);
					version[1] = Int32.Parse(middleVersion);
					version[2] = Int32.Parse(minorVersion);
				}
				catch (FormatException ex)
				{
					System.Console.Error.WriteLine("Failed to calculate version: " + ex.Message);
				}
			}
		}
	}
}
