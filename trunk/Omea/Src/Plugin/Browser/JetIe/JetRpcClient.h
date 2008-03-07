/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// JetRpcClient.h : Declaration of the CJetRpcClient
// This is a base class for JetBrains remote procedure calls client services.
//
// This class is not intended for use for multiple simultaneous requests,
// though it may perform sequential requests.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once
#include "CommonResource.h"       // main symbols

#include "JetIe.h"
#pragma warning(disable: 4290)	// warning C4290: C++ exception specification ignored except to indicate a function is not __declspec(nothrow)

#define HTTP_OPEN_REQUEST_FLAGS	INTERNET_FLAG_HYPERLINK | INTERNET_FLAG_IGNORE_CERT_CN_INVALID | INTERNET_FLAG_IGNORE_CERT_DATE_INVALID | INTERNET_FLAG_NO_CACHE_WRITE | INTERNET_FLAG_NO_COOKIES | INTERNET_FLAG_NO_UI | INTERNET_FLAG_PRAGMA_NOCACHE | INTERNET_FLAG_RELOAD

#define HTTP_BUFFER_SIZE	0x400
// TODO: play with smaaaall buffer sizes to test

#define MARSHALLING_DATA_TAG 0x2394239

// Marshalling messages
#define WM_JETRPCCLIENT_MARSHAL_BASE	(WM_USER + 239)	// Base message
#define WM_JETRPCCLIENT_MARSHAL_ERROR	(WM_JETRPCCLIENT_MARSHAL_BASE + 0)
#define WM_JETRPCCLIENT_MARSHAL_REQUESTCOMPLETE	(WM_JETRPCCLIENT_MARSHAL_BASE + 1)
#define WM_JETRPCCLIENT_MARSHAL_HANDLECREATED	(WM_JETRPCCLIENT_MARSHAL_BASE + 2)
#define WM_JETRPCCLIENT_MARSHAL_HANDLECLOSING	(WM_JETRPCCLIENT_MARSHAL_BASE + 3)

// CJetRpc

class CJetRpcClient : 
	public CWindowImpl<CJetRpcClient>
{
// Ctor/dtor
public:
	CJetRpcClient();
	virtual ~CJetRpcClient();

// Implementation
protected:

	/// Handle to the WinINet library returned by the InternetOpen function.
	/// TODO: share between instances.
	CInternetHandle	m_hInet;

	/// Handle to the open connection.
	/// TODO: share between instances.
	CInternetHandle	m_hConnection;

	/// Handle to the open request.
	CInternetHandle	m_hRequest;

	/// The data being transmitted.
	/// The data is written in here by PrepareDataToSend. It's not necessary to rewind the stream.
	IStreamPtr	m_streamTransmit;

	/// The data being received.
	/// NULL value indicates that a receival has not been started. Must be NULLed as soon as possible after processing the results.
	IStreamPtr	m_streamReceive;

	/// The server response in the form of an XML document.
	/// Into the input stream of this document the data is uploaded (via m_streamReceive).
	XmlDocument	m_xmlResponse;

	/// The Finite State Machine possible states.
	typedef enum tagStates
	{
		stateIdle,	// Doing nothing. When called in this state, FSM prepares for the run.
		stateInitializing,	// Starting the async or sync operation. The connection is opened, the request handle is created, and the headers are added to it
		stateReadyToSend,	// Preparing the outbound data
		stateTransmitting,	// Transmitting the request data
		stateReadyToReceive,	// The request is being completed (terminated)
		stateReceiving,	// Receiving the reply data asynchronously
		stateParsingReply,	// Processing the data that has come from the server
		stateCompleted,	// A request has been completed successfully; calls the external callbacks, if any
		stateCloseRequest,	// If a connection should not be closed but kept for one more request, closes the request and switches to the idle state, keeping the connection alive. Warning: the object stays locked!
		stateShutdown,	// The async request has completed or failed and is now shutting down
		stateNULL
	} States;

	/// The Finite State Machine current state.
	States	m_state;

	/// A byte buffer used for transmissions.
	CStringA	m_sBufA;

	/// Internet buffers for reading from the stream.
	/// Always use the ANSI version as we're feeding it into InternetReadFileExA.
	INTERNET_BUFFERSA	m_ibOut;

	/// A flag indicating whether the current request is running in the asynchronous manner.
	bool	m_bAsync;

	/// Contains the marshalling data which allows to call the object on its own thread, either sync or async, from any other thread.
	struct CMarshallingData
	{
		DWORD	dwTag;	// Just a tag that helps to ensure that this data structures is actually the thing.
		HWND	hwnd;	// Handle to the window which should be sent a message.
		bool	bAsync;	// Determines whether to do requests sync or async. When working in sync mode, all the requests MUST be sync. In async mode, async calls will prevent from deadlocks.
	}m_marshalling;

#ifdef _TRACE
	/// Number of instances of this class. Should not grow …
	/// Needed for test purposes.
	static int	m_nInstances;

	/// Number of BSTR strings being sent via PostMessage marshalling but not yet received by the callee.
	static volatile LONG	m_nBstrOnTheRun;
#endif

	/// Error message for the most recent error. Starting a new request resets this indicator.
	/// The only place which is in charge of setting this value is the InternetStatusCallback_Error function.
	CStringW	m_sLastError;

	/// Stores whether a callback (either OnError or OnComplete) has already been invoked, and prevents from invoking it once more or calling the other callback more than once per a request.
	bool	m_bTerminationCallbackInvoked;

// Internal Operations
private:

	/// Status callback function for the WinINet notifications, static version. Invokes the instance version.
	static void CALLBACK InternetStatusCallback(HINTERNET hInternet, DWORD_PTR dwContext, DWORD dwInternetStatus, LPVOID lpvStatusInformation, DWORD dwStatusInformationLength);

	/// Performs basic checks on whether the response is a valid XML document.
	/// If yes, a derived handler is invoke to validate the per-protocol validness.
	void ValidateXmlResponse() throw(_com_error);

	/// Transmits the stream of data asynchronously or synchronously.
	/// Returns: True if completed sync, False if should wait for the async completion event.
	bool SendData() throw(_com_error);

	/// Receives a portion of data asynchornously or synchronously.
	/// Better to say, it requests a chunk of data and then waits for it to come asynchronously.
	/// Returns whether it has to be executed more. If False, then no read operation was actually requested, should switch to the next state and start processing the gotten data.
	bool ReadData() throw(_com_error);

	/// Terminates the request asynchronously or synchronously. Returns whether the async operation has managed to complete in an instant and the event should not be waited for, or, that is, just sync processing should go on. If False, then you should wait for the event.
	bool EndRequest() throw(_com_error);

	/// Writes the data to be transmitted into the outbound stream.
	/// Calls the inherited handler to populate it with the actual data.
	void PrepareDataToSend() throw(_com_error);

	/// Executes the next Finite State Machine step, returns whether one more step is required to be executed immediately, synchronously (for example, the state has changed and one more iteration should occur right now for the new state).
	bool FSM() throw(_com_error);

	/// Executes the FSM steps as long as they require execution of one more step in sync mode. When completed or async wait is needed, returns.
	void RunFSM() throw(_com_error);

	/// Initiates the request for the async mode, or just executes it in the sync mode.
	/// True return value indicates sync continuation, and false stands for async.
	bool BeginRequest() throw(_com_error);

	/// Invokes the error-handling function, marshalling the requests into the main thread, either asynchronously or synchronously.
	/// pInstance specifies the class instance, if known, on which the request should be processed. If non-null, it's used as a source of window handle. If null, hwndMarshal should refer to a valid window which would be given a notification message
	static void ReportError(CJetRpcClient *pInstance, HWND hwndMarshal, CStringW sErrorMessage, bool bAsync);

// Operations
public:
	/// Throws a _com_error exception with an HRESULT and IErrorInfo which resolves to the error text specified. May be used by classes that do not have their own IErrorInfo, but wish to issue meaningful error messages. Also traces the error text to the standard debug output.
	void virtual ThrowError(CStringW sError) throw(_com_error) = 0;

	/// Returns the XML document repsesenting the remote server response, or NULL if there were none yet.
	XmlDocument GetResponse();

// Window Infrastructure
public:
DECLARE_WND_CLASS(_T("JetRpcClient Marshalling Window"))

// Message map
BEGIN_MSG_MAP(CJetRpcClient)
	MESSAGE_HANDLER(WM_CREATE, OnCreateWindow)

	MESSAGE_HANDLER(WM_JETRPCCLIENT_MARSHAL_ERROR, OnMarshalError)
	MESSAGE_HANDLER(WM_JETRPCCLIENT_MARSHAL_REQUESTCOMPLETE, OnMarshalRequestComplete)
	MESSAGE_HANDLER(WM_JETRPCCLIENT_MARSHAL_HANDLECREATED, OnMarshalHandleCreated)
	MESSAGE_HANDLER(WM_JETRPCCLIENT_MARSHAL_HANDLECLOSING, OnMarshalHandleClosing)
END_MSG_MAP()

	// Message handlers
	LRESULT OnCreateWindow(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnMarshalError(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnMarshalRequestComplete(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnMarshalHandleCreated(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnMarshalHandleClosing(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);

	/// Fires when the last message for this window is received and allows to release the reference to this object and so on.
	virtual void OnFinalMessage(HWND hWnd);

// Overloads
protected:
	/// Returns the host name to which the request should be issued.
	virtual CStringA OnGetHostName() = 0;

	/// Port number on the host to which the request should be issued.
	virtual int OnGetPort() = 0;

	/// HTTP object name to be used in the request.
	virtual CStringA OnGetObjectName() = 0;

	/// Value for the Content-type header which will be sent to the server.
	virtual CStringA OnGetContentType() = 0;

	/// Locks the object lifetime while the request is running (typically, calls AddRef).
	virtual void OnLockObject() = 0;

	/// Unocks the object lifetime as the request has finished running (typically, calls Release).
	virtual void OnUnlockObject() = 0;

	/// Fires when the request completes successfully.
	virtual void OnComplete(XmlDocument xmlResponse) = 0;

	/// Fires when the request terminates unexpectedly due to an error.
	virtual void OnError(CStringW sErrorMessage) = 0;

	/// Fires when the data to be sent to the server is being requested.
	virtual IStreamPtr OnPrepareDataToSend() = 0;

	/// Validates the XML response against the per-protocol rules.
	/// Throws an exception if the response appears to be invalid or reportes an error.
	virtual void OnValidateResponse(XmlDocument xmlResponse) = 0;

	/// Asks whether a connection can be closed or other requests are going to be performed thru the same connection. Return True to close the connection, or False to keep it.
	virtual bool OnWhetherCloseConnection() = 0;

	/// Asks whether to invoke one more request immediately, in the same sync/async mode. Does not call OnBeforeCloseConnection in case of a positive answer as it's clear that the connection will be needed for one more request.
	/// All the request callbacks will be queried once again for each new request.
	virtual bool OnWhetherInvokeAgain() = 0;

/// Interface
public:
	/// Invokes the remote server in either synchronous or asynchronous fashion, thus starting the request.
	void InvokeServer(bool bAsync) throw(_com_error);

	/// Tells whether the request is sync or async.
	/// If the request is not running, this value is nearly useless due to being unspecified.
	bool IsAsync();
};

