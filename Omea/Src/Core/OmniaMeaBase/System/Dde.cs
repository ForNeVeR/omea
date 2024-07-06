// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using JetBrains.UI.Interop;
using DWORD = System.UInt32;
using UINT = System.UInt32;
using HDDEDATA = System.IntPtr;
using HCONV = System.IntPtr;
using HSZ = System.IntPtr;
using BOOL = System.Boolean;
using BOOLEAN = System.Byte;
using BYTE = System.Byte;

namespace JetBrains.Omea.Base
{
	/// <summary>
	/// Implements DDE operations for the Omea application.
	/// </summary>
	public class Dde : IDisposable
	{
		#region Data

		/// <summary>
		/// Instance cookie of the DDEML instance.
		/// </summary>
		protected DWORD _instance = 0u;

		/// <summary>
		/// Here a delegate will be stored that serves as a WINAPI callback.
		/// This variable keeps a reference to it alive while it's still needed.
		/// </summary>
		internal PFNCALLBACK _ddecallback = null;

		/// <summary>
		/// When the DDE subsystem is first accessed, and the first check for an owner thread is performed,
		/// the calling thread is written in here. All the subsequent calls must occur on the same thread.
		/// </summary>
		protected static int _threadOwner = 0;

		#endregion

		#region Constants

		/// <summary>
		/// Contains string identifiers for Internet Explorer's service name and topics.
		/// </summary>
		public class InternetExplorer
		{
			/// <summary>
			/// The Microsoft Internet Explorer version 3.0 or higher service identifier (aka application name).
			/// </summary>
			public static readonly string Service = "iexplore";

			/// <summary>
			/// The Microsoft Internet Explorer version below 3.0 service identifier (aka application name).
			/// </summary>
			public static readonly string ServiceOld = "mosaic";

			/// <summary>
			/// Topic for opening a URL.
			/// </summary>
			public static readonly string TopicOpenUrl = "WWW_OpenURL";

			/// <summary>
			/// Topic for opening a URL in a new window.
			/// </summary>
			public static readonly string TopicOpenUrlNewWindow = "WWW_OpenURLNewWindow";

			/// <summary>
			/// A command that should be sent to open a URL in a new window.
			/// "<c>%1</c>" should be replaced with the actual URI.
			/// </summary>
			public static readonly string CommandOpenUrlInNewWindow = "\"%1\",,0";
		}

		#endregion

		#region Construction

		/// <summary>
		/// Initializes the use of DDE.
		/// </summary>
		public Dde()
		{
			if(_instance != 0)
				throw new InvalidOperationException("Must be uninitialized.");

			CheckOwnerThread();

			// Initialize as a client
			_ddecallback = new PFNCALLBACK(DdeCallback);
			DdeError dwError = DdeInitialize(ref _instance, _ddecallback, (uint)AfCmd.APPCMD_CLIENTONLY, 0u); // TODO: callback collected
			if(dwError != DdeError.DMLERR_NO_ERROR)
				throw new DdeException("Failed to initialize DDE.", dwError);
		}

		#endregion

		#region Operations

		/// <summary>
		/// Initiates a conversation.
		/// </summary>
		/// <param name="service">Service (or application name) we're connecting to.</param>
		/// <param name="topic">Topic to request the server for.</param>
		public DdeConversation CreateConversation(string service, string topic)
		{
			CheckOwnerThread();

			return new DdeConversation(this, service, topic);
		}

		#endregion

		#region Interop Callbacks

		/// <summary>
		/// The DDE callback function. As we're a client-only DDE app by now, it should not be called at all. At least, for the meaningful purposes.
		/// </summary>
		protected HDDEDATA DdeCallback(UINT uType, UINT uFmt, HCONV hconv, HDDEDATA hsz1, HDDEDATA hsz2, HDDEDATA hdata, HDDEDATA dwData1, HDDEDATA dwData2)
		{
			return IntPtr.Zero;
		}

		#endregion

		#region Interop Accessors

		[DllImport("User32.dll", CharSet=CharSet.Ansi)]
		internal static extern DdeError DdeInitialize(ref DWORD pidInst, PFNCALLBACK pfnCallback, DWORD afCmd, DWORD ulRes);

		[DllImport("User32.dll", CharSet=CharSet.Ansi)]
		internal static extern BOOL DdeUninitialize(DWORD idInst);

		internal delegate HDDEDATA PFNCALLBACK(UINT uType, UINT uFmt, HCONV hconv, HDDEDATA hsz1, HDDEDATA hsz2, HDDEDATA hdata, HDDEDATA dwData1, HDDEDATA dwData2);

		internal enum AfCmd : uint
		{
			APPCLASS_MONITOR = 0x00000001u,
			APPCLASS_STANDARD = 0x00000000u,
			APPCMD_CLIENTONLY = 0x00000010u,
			APPCMD_FILTERINITS = 0x00000020u
		}

		[DllImport("User32.dll", CharSet=CharSet.Ansi)]
		internal static extern BOOL DdeKeepStringHandle(DWORD idInst, HSZ hsz);

		[DllImport("User32.dll", CharSet=CharSet.Ansi)]
		internal static extern BOOL DdeFreeStringHandle(DWORD idInst, HSZ hsz);

		[DllImport("User32.dll", CharSet=CharSet.Ansi)]
		internal static extern HSZ DdeCreateStringHandle(DWORD idInst, byte[] psz, int iCodePage);

		[DllImport("User32.dll", CharSet=CharSet.Ansi)]
		internal static extern UINT DdeGetLastError(DWORD idInst);

		[DllImport("User32.dll", CharSet=CharSet.Ansi)]
		internal static extern HCONV DdeConnect(DWORD idInst, HSZ hszService, HSZ hszTopic, /*ref CONVCONTEXT pCC*/IntPtr nullpointer);

		[DllImport("User32.dll", CharSet=CharSet.Ansi)]
		internal static extern BOOL DdeDisconnect(HCONV hConv);

		[DllImport("User32.dll", CharSet=CharSet.Ansi)]
		internal static extern HDDEDATA DdeClientTransaction(byte[] pData, DWORD cbData, HCONV hConv, HSZ hszItem, UINT wFmt, DdeTransaction wType, DWORD dwTimeout, ref DWORD pdwResult);

		[StructLayout(LayoutKind.Sequential)]
		internal struct CONVCONTEXT
		{
			public UINT cb;
			public UINT wFlags;
			public UINT wCountryID;
			public int iCodePage;
			public DWORD dwLangID;
			public DWORD dwSecurity;
			public SECURITY_QUALITY_OF_SERVICE qos;
		}

		/// <summary>
		/// Quality Of Service.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		internal struct SECURITY_QUALITY_OF_SERVICE
		{
			public DWORD Length;
			public SECURITY_IMPERSONATION_LEVEL ImpersonationLevel;
			public SECURITY_CONTEXT_TRACKING_MODE ContextTrackingMode;
			public BOOLEAN EffectiveOnly;
		} ;

		/// <summary>
		/// Impersonation Level
		///
		/// Impersonation level is represented by a pair of bits in Windows.
		/// If a new impersonation level is added or lowest value is changed from
		/// 0 to something else, fix the Windows CreateFile call.
		/// </summary>
		internal enum SECURITY_IMPERSONATION_LEVEL : uint
		{
			SecurityAnonymous,
			SecurityIdentification,
			SecurityImpersonation,
			SecurityDelegation
		}

		internal enum SECURITY_CONTEXT_TRACKING_MODE : byte
		{
			SECURITY_STATIC_TRACKING = 0,
			SECURITY_DYNAMIC_TRACKING = 1
		}

		internal enum DdeTransaction : uint
		{
			XTYPF_NOBLOCK = 0x0002, /* CBR_BLOCK will not work */
			XTYPF_NODATA = 0x0004, /* DDE_FDEFERUPD */
			XTYPF_ACKREQ = 0x0008, /* DDE_FACKREQ */

			XCLASS_MASK = 0xFC00,
			XCLASS_BOOL = 0x1000,
			XCLASS_DATA = 0x2000,
			XCLASS_FLAGS = 0x4000,
			XCLASS_NOTIFICATION = 0x8000,

			XTYP_ERROR = (0x0000 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK),
			XTYP_ADVDATA = (0x0010 | XCLASS_FLAGS),
			XTYP_ADVREQ = (0x0020 | XCLASS_DATA | XTYPF_NOBLOCK),
			XTYP_ADVSTART = (0x0030 | XCLASS_BOOL),
			XTYP_ADVSTOP = (0x0040 | XCLASS_NOTIFICATION),
			XTYP_EXECUTE = (0x0050 | XCLASS_FLAGS),
			XTYP_CONNECT = (0x0060 | XCLASS_BOOL | XTYPF_NOBLOCK),
			XTYP_CONNECT_CONFIRM = (0x0070 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK),
			XTYP_XACT_COMPLETE = (0x0080 | XCLASS_NOTIFICATION),
			XTYP_POKE = (0x0090 | XCLASS_FLAGS),
			XTYP_REGISTER = (0x00A0 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK),
			XTYP_REQUEST = (0x00B0 | XCLASS_DATA),
			XTYP_DISCONNECT = (0x00C0 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK),
			XTYP_UNREGISTER = (0x00D0 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK),
			XTYP_WILDCONNECT = (0x00E0 | XCLASS_DATA | XTYPF_NOBLOCK),
			XTYP_MASK = 0x00F0,
			XTYP_SHIFT = 4 /* shift to turn XTYP_ into an index */
		}

		public const uint TIMEOUT_ASYNC = 0xFFFFFFFF;

		#endregion

		#region Implementation

		/// <summary>
		/// Locks a DDE string handle by increasing its reference count so that it won't be dropped from the stringtable.
		/// </summary>
		internal void KeepStringHandle(HSZ handle)
		{
			if(!DdeKeepStringHandle(_instance, handle))
				throw new DdeException("Cannot lock the string handle.", GetLastError());
		}

		/// <summary>
		/// Unlocks a DDE string handle by decreasing its reference count.
		/// </summary>
		internal void FreeStringHandle(HSZ handle)
		{
			if(!DdeFreeStringHandle(_instance, handle))
				throw new DdeException("Cannot release the string handle.", GetLastError());
		}

		internal HSZ CreateStringHandle(string text)
		{
			if(text.Length > 255)
			{
				Trace.WriteLine(String.Format("A string cannot be wrapped to a DDE string because it's too long for that. It's {0} chars while the limit is 255. The original string is \"{1}\", was truncated down to the required length.", text.Length, text), "[DDE]");
				text = text.Substring(0, 255);
			}

			byte[] ps = Encoding.ASCII.GetBytes(text);
			byte[] psz = new byte[ps.Length + 1];
			ps.CopyTo(psz, 0);
			psz[psz.Length - 1] = 0;
			HSZ handle = DdeCreateStringHandle(_instance, psz, Win32Declarations.CP_WINANSI); // TODO: winneutral

			// Error checks
			if(handle == IntPtr.Zero)
				throw new DdeException("Cannot create a DDE handle of the string.", GetLastError());
			DdeError dwErr = GetLastError();
			if(dwErr != DdeError.DMLERR_NO_ERROR)
				throw new DdeException("Cannot create a DDE handle of the string.", dwErr);

			return handle;
		}

		internal DdeError GetLastError()
		{
			return (DdeError)DdeGetLastError(_instance);
		}

		/// <summary>
		/// Provides access to the session instance handle.
		/// </summary>
		internal DWORD Instance
		{
			get { return _instance; }
		}

		/// <summary>
		/// Ensures that the function is being executed on the owning thread (which is the thread on which DDE has been called for the first time).
		/// </summary>
		internal static void CheckOwnerThread()
		{
			if (_threadOwner == 0)	// Invoked for the first time, remember the thread
			{
				_threadOwner = Win32Declarations.GetCurrentThreadId();
				Trace.WriteLine(String.Format("DDE has remembered thread “{0}”#{1} as its owning thread.", Thread.CurrentThread.Name, _threadOwner), "[DDE]");
			}
			else
			{	// There's already an “own thread” recorded, check that it matches the current thread
				if(Win32Declarations.GetCurrentThreadId() != _threadOwner)
					throw new InvalidOperationException(String.Format("DDE functions must be accessed from one thread only, the same one on which they were accessed for the first time (#{0} not “{1}”#{2}).", _threadOwner, Thread.CurrentThread.Name, Win32Declarations.GetCurrentThreadId()));
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if(_instance != 0)
			{
				DdeUninitialize(_instance);
				_instance = 0;
			}
			GC.SuppressFinalize(this);
		}

		~Dde()
		{
			Dispose();
		}

		#endregion
	}

	#region DdeException Class — Implements an exception caused by the DDE interop.

	/// <summary>
	/// Implements an exception caused by the DDE interop.
	/// </summary>
	public class DdeException : Exception
	{
		/// <summary>
		/// The raw DDE Error represented by this exception.
		/// </summary>
		private readonly DdeError _error;

		/// <summary>
		/// Maps DDE error codes to the corresponding human-readable error messages.
		/// </summary>
		private static Hashtable _hashErrorMessages;

		/// <summary>
		/// Creates an exception that consists of a text message and a raw DDE error code.
		/// </summary>
		/// <param name="message">Text message.</param>
		/// <param name="error">Raw error code.</param>
		public DdeException(string message, DdeError error)
			: base(message + " " + ErrorToString(error))
		{
			_error = error;
		}

		/// <summary>
		/// Creates an exception that consists of a raw DDE error code.
		/// </summary>
		/// <param name="error">Raw error code.</param>
		public DdeException(DdeError error)
			: base(ErrorToString(error))
		{
			_error = error;
		}

		static DdeException()
		{
			// Initialize the error strings
			_hashErrorMessages = new Hashtable();
			lock(_hashErrorMessages)
			{
				_hashErrorMessages[DdeError.DMLERR_NO_ERROR] = "The operation completed successfully.";
				_hashErrorMessages[DdeError.DMLERR_ADVACKTIMEOUT] = "A request for a synchronous advise transaction has timed out.";
				_hashErrorMessages[DdeError.DMLERR_BUSY] = "The response to the transaction caused the DDE_FBUSY flag to be set.";
				_hashErrorMessages[DdeError.DMLERR_DATAACKTIMEOUT] = "A request for a synchronous data transaction has timed out.";
				_hashErrorMessages[DdeError.DMLERR_DLL_NOT_INITIALIZED] = "A DDEML function was called without first calling the DdeInitialize function, or an invalid instance identifier was passed to a DDEML function.";
				_hashErrorMessages[DdeError.DMLERR_DLL_USAGE] = "An application initialized as APPCLASS_MONITOR has attempted to perform a Dynamic Data Exchange (DDE) transaction, or an application initialized as APPCMD_CLIENTONLY has attempted to perform server transactions.";
				_hashErrorMessages[DdeError.DMLERR_EXECACKTIMEOUT] = "A request for a synchronous execute transaction has timed out.";
				_hashErrorMessages[DdeError.DMLERR_INVALIDPARAMETER] = "A parameter failed to be validated by the DDEML. Some of the possible causes follow: * The application used a data handle initialized with a different item name handle than was required by the transaction. * The application used a data handle that was initialized with a different clipboard data format than was required by the transaction. * The application used a client-side conversation handle with a server-side function or vice versa. * The application used a freed data handle or string handle. * More than one instance of the application used the same object.";
				_hashErrorMessages[DdeError.DMLERR_LOW_MEMORY] = "A DDEML application has created a prolonged race condition (in which the server application outruns the client), causing large amounts of memory to be consumed.";
				_hashErrorMessages[DdeError.DMLERR_MEMORY_ERROR] = "A memory allocation has failed.";
				_hashErrorMessages[DdeError.DMLERR_NO_CONV_ESTABLISHED] = "A client's attempt to establish a conversation has failed.";
				_hashErrorMessages[DdeError.DMLERR_NOTPROCESSED] = "A transaction has failed.";
				_hashErrorMessages[DdeError.DMLERR_POKEACKTIMEOUT] = "A request for a synchronous poke transaction has timed out.";
				_hashErrorMessages[DdeError.DMLERR_POSTMSG_FAILED] = "An internal call to the PostMessage function has failed.";
				_hashErrorMessages[DdeError.DMLERR_REENTRANCY] = "An application instance with a synchronous transaction already in progress attempted to initiate another synchronous transaction, or the DdeEnableCallback function was called from within a DDEML callback function.";
				_hashErrorMessages[DdeError.DMLERR_SERVER_DIED] = "A server-side transaction was attempted on a conversation terminated by the client, or the server terminated before completing a transaction.";
				_hashErrorMessages[DdeError.DMLERR_SYS_ERROR] = "An internal error has occurred in the DDEML.";
				_hashErrorMessages[DdeError.DMLERR_UNADVACKTIMEOUT] = "A request to end an advise transaction has timed out.";
				_hashErrorMessages[DdeError.DMLERR_UNFOUND_QUEUE_ID] = "An invalid transaction identifier was passed to a DDEML function. Once the application has returned from an XTYP_XACT_COMPLETE callback, the transaction identifier for that callback function is no longer valid.";
			}
		}

		/// <summary>
		/// The raw DDE Error code.
		/// </summary>
		public DdeError Error
		{
			get { return _error; }
		}

		/// <summary>
		/// Provides a human-readable string representation of a DDE raw error code.
		/// </summary>
		/// <param name="error">DDE error.</param>
		/// <returns>Error string.</returns>
		public static string ErrorToString(DdeError error)
		{
			// Return a string message, if available, or a numerical code otherwise
			object message;
			lock(_hashErrorMessages)
				message = _hashErrorMessages[error];
			return message != null ? message.ToString() : String.Format("An unknown DDE error {0} has occured.", (uint)error);
		}

		/// <summary>
		/// Gets whether the raw DDE error code contained in this exception instance indicates a timeout.
		/// </summary>
		public bool Timeout
		{
			get
			{
				return (_error == DdeError.DMLERR_ADVACKTIMEOUT)
					|| (_error == DdeError.DMLERR_DATAACKTIMEOUT)
					|| (_error == DdeError.DMLERR_EXECACKTIMEOUT)
					|| (_error == DdeError.DMLERR_POKEACKTIMEOUT)
					|| (_error == DdeError.DMLERR_UNADVACKTIMEOUT);
			}

		}
	}

	/// <summary>
	/// DDE error codes.
	/// </summary>
	public enum DdeError : uint
	{
		DMLERR_NO_ERROR = 0,
		DMLERR_FIRST = 0x4000,
		DMLERR_ADVACKTIMEOUT = 0x4000,
		DMLERR_BUSY = 0x4001,
		DMLERR_DATAACKTIMEOUT = 0x4002,
		DMLERR_DLL_NOT_INITIALIZED = 0x4003,
		DMLERR_DLL_USAGE = 0x4004,
		DMLERR_EXECACKTIMEOUT = 0x4005,
		DMLERR_INVALIDPARAMETER = 0x4006,
		DMLERR_LOW_MEMORY = 0x4007,
		DMLERR_MEMORY_ERROR = 0x4008,
		DMLERR_NOTPROCESSED = 0x4009,
		DMLERR_NO_CONV_ESTABLISHED = 0x400a,
		DMLERR_POKEACKTIMEOUT = 0x400b,
		DMLERR_POSTMSG_FAILED = 0x400c,
		DMLERR_REENTRANCY = 0x400d,
		DMLERR_SERVER_DIED = 0x400e,
		DMLERR_SYS_ERROR = 0x400f,
		DMLERR_UNADVACKTIMEOUT = 0x4010,
		DMLERR_UNFOUND_QUEUE_ID = 0x4011,
		DMLERR_LAST = 0x4011
	}

	#endregion

	#region Class DdeConversation

	/// <summary>
	/// Implements a single conversation between a DDE client and a DDE server.
	/// </summary>
	public class DdeConversation : IDisposable
	{
		#region Data

		/// <summary>
		/// The DDE manager instance.
		/// </summary>
		protected Dde _dde;

		/// <summary>
		/// Handle to the active conversation.
		/// </summary>
		protected HCONV _handle;

		#endregion

		#region Construction

		/// <summary>
		/// Initiates a conversation.
		/// </summary>
		/// <param name="service">Service (or application name) we're connecting to.</param>
		/// <param name="topic">Topic to request the server for.</param>
		internal DdeConversation(Dde dde, string service, string topic)
		{
			_dde = dde;

			/*
			// Describe the conversation
			Dde.CONVCONTEXT cc;
			cc.cb = (uint)Marshal.SizeOf(typeof(Dde.CONVCONTEXT));
			cc.wFlags = 0u;
			cc.wCountryID = 0u;
			cc.iCodePage = Win32Declarations.CP_WINANSI;
			cc.dwLangID = 0u;
			cc.dwSecurity = 0u;
			cc.qos.Length = (uint)Marshal.SizeOf(typeof(Dde.SECURITY_QUALITY_OF_SERVICE));
			cc.qos.ImpersonationLevel = Dde.SECURITY_IMPERSONATION_LEVEL.SecurityAnonymous;
			cc.qos.ContextTrackingMode = Dde.SECURITY_CONTEXT_TRACKING_MODE.SECURITY_STATIC_TRACKING;
			cc.qos.EffectiveOnly = 1;
			*/

			using(DdeString dsService = new DdeString(_dde, service))
			using(DdeString dsTopic = new DdeString(_dde, topic))
				_handle = Dde.DdeConnect(_dde.Instance, dsService.Handle, dsTopic.Handle, IntPtr.Zero);

			if(_handle == IntPtr.Zero)
				throw new DdeException("Could not start the DDE conversation.", _dde.GetLastError());
		}

		~DdeConversation()
		{
			if(_handle != IntPtr.Zero)
				Dde.DdeDisconnect(_handle);
		}

		#endregion

		#region Operations

		/// <summary>
		/// Initiates an asynchronous DDE transaction.
		/// </summary>
		/// <param name="item">Item name for which the transaction is executed.</param>
		/// <param name="data">Data to be exchanged during the transaction.</param>
		/// <remarks>This function is the same as <see cref="StartTransaction"/> with <c>timeout</c> set to <see cref="Dde.TIMEOUT_ASYNC"/>.</remarks>
		public void StartAsyncTransaction(string item, string data)
		{
			StartTransaction( item, data, Dde.TIMEOUT_ASYNC );
		}

		/// <summary>
		/// Initiates a synchronous DDE transaction, using the timeout value specified.
		/// If the timeout expires, an exception is thrown.
		/// </summary>
		/// <param name="item">Item name for which the transaction is executed.</param>
		/// <param name="data">Data to be exchanged during the transaction.</param>
		/// <param name="timeout">Specifies the maximum length of time, in milliseconds, that the client will wait for a response from the server application in a synchronous transaction.</param>
		public void StartTransaction(string item, string data, DWORD timeout)
		{
			Dde.CheckOwnerThread();

			byte[] arDataBuffer = data != null ? Encoding.ASCII.GetBytes(data) : null;
			DWORD dwResult = 0;
			using(DdeString dsItem = new DdeString(_dde, item))
			{
				if(Dde.DdeClientTransaction(arDataBuffer, (uint)(arDataBuffer != null ? arDataBuffer.Length : 0), _handle, dsItem.Handle, 0, Dde.DdeTransaction.XTYP_EXECUTE, timeout, ref dwResult) == IntPtr.Zero)
					throw new DdeException("Could start a DDE transaction.", _dde.GetLastError());
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if((_handle != IntPtr.Zero) && (_dde != null))
			{
				if(!Dde.DdeDisconnect(_handle))
					throw new DdeException("Could not terminate the conversation.", _dde.GetLastError());
				_handle = IntPtr.Zero;
				_dde = null;
			}
			GC.SuppressFinalize(this);
		}

		#endregion
	}

	#endregion

	#region Class DdeString — Implements a wrapper around the DDE string handle.

	/// <summary>
	/// Implements a wrapper around the DDE string handle.
	/// </summary>
	internal class DdeString : IDisposable
	{
		/// <summary>
		/// The string value of the DDE string (if assigned or already extracted).
		/// <c>Null</c> if the object has been created from a handle and the string has not been retrieved yet.
		/// </summary>
		protected string _string = null;

		/// <summary>
		/// The DDE object instance. It provides a handle to the DDEML instance.
		/// </summary>
		private readonly Dde _dde;

		/// <summary>
		/// DDE handle to the string
		/// </summary>
		protected HSZ _handle = IntPtr.Zero;

		/// <summary>
		/// Determines whether the handle should be released (is owned by this object).
		/// </summary>
		protected bool _release = false;

		/// <summary>
		/// Initializes the object from a string. The handle will be generated upon a request.
		/// <c>Null</c> is a valid value for a string, will cause a <c>Null</c> handle to be returned.
		/// </summary>
		public DdeString(Dde dde, string text)
		{
			_string = text;
			_dde = dde;
		}

		/// <summary>
		/// Initializes the object from a handle, takes ownership over the handle, and extracts a string value as it's needed.
		/// <c>Null</c> is a valid value for the handle, will cause a <c>Null</c> string to be returned.
		/// </summary>
		public DdeString(Dde dde, HSZ handle)
		{
			_dde = dde;
			_handle = handle;

			_dde.KeepStringHandle(handle);
			_release = handle != IntPtr.Zero;
		}

		/// <summary>
		/// Gets the string value of the DDE string.
		/// This value is either assigned by the constructor, or retrieved from the handle.
		/// </summary>
		public string String
		{
			get
			{
				if((_string == null) && (_handle == IntPtr.Zero))
					return null; // A valid Null value
				if(_string == null) // A handle is defined, but the string is Null => it has to be retrieved
					throw new NotImplementedException("Getting a string from the handle is not implemented yet."); // TODO: implement
				return _string;
			}
		}

		/// <summary>
		/// Gets the handle to the DDE string. This is either a handle passed in to the constructor, or a handle created from a string if a string was passed in.
		/// </summary>
		public HSZ Handle
		{
			get
			{
				// Check if handle was not produced from the string yet
				if(_handle == IntPtr.Zero)
				{
					if(_string == null) // It's a valid Null value
						return IntPtr.Zero;

					_handle = _dde.CreateStringHandle(_string);
					_release = true;
				}
				return _handle;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			_string = null;
			// Release the DDE handle?
			if(_release)
			{
				if(_handle == IntPtr.Zero)
					throw new InvalidOperationException("Wanna free the handle, but the handle is null.");

				_dde.FreeStringHandle(_handle);
				_release = false;
				_handle = IntPtr.Zero;
			}
			GC.SuppressFinalize(this);
		}

		~DdeString()
		{
			if((_release) && (_handle != IntPtr.Zero) && (_dde != null))
				_dde.FreeStringHandle(_handle);
		}

		#endregion
	}

	#endregion
}
