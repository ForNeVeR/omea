/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// JetRpcClient.cpp : Implementation of CJetRpcClient
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "stdafx.h"
#include "JetRpcClient.h"

// CJetRpcClient

#ifdef _TRACE
int CJetRpcClient::m_nInstances = 0;
volatile LONG CJetRpcClient::m_nBstrOnTheRun = 0;
#endif

CJetRpcClient::CJetRpcClient()
{
#ifdef _TRACE
	TRACE(L"CJetRpcClient::CJetRpcClient (+%d)", ++m_nInstances);
#endif
	m_state = stateIdle;
	m_marshalling.dwTag = 0;
	m_bTerminationCallbackInvoked = false;
	m_bAsync = true;
}

CJetRpcClient::~CJetRpcClient()
{
	ASSERT(m_state == stateIdle);	// Must not be killed in action
	ASSERT(!IsWindow());
	if(IsWindow())
		DestroyWindow();
	ASSERT(m_marshalling.dwTag == 0);
	// TODO: close the handles, just in case
#ifdef _TRACE
	TRACE(L"CJetRpcClient::~CJetRpcClient (-%d) bstrs(%d)", --m_nInstances, m_nBstrOnTheRun);
#endif
}

void CJetRpcClient::InvokeServer(bool bAsync)
{
	// Check if we're permitted to run a request at this time
	if(m_state != stateIdle)
		ThrowError(CJetIe::LoadString(IDS_E_REQUEST_ALREADY_RUNNING));

	m_state = stateIdle;	// Will be changed by the FSM to the value appropriate
	m_bAsync = bAsync;

	// Call the FSM which will either complete the request synchronously or initiate the async operation
	RunFSM();

	ASSERT((m_bAsync) || (m_state == stateIdle));	// Check that, if sync, the operation has completed

	// Report the error, if there was one; do in sync case only because the only place we can set an error to this string is OnMarshalError, and in async case it has already reported the error to the caller; don't do it twice
	if((!m_bAsync) && (!m_sLastError.IsEmpty()))
		ThrowError(m_sLastError);
}

bool CJetRpcClient::BeginRequest()
{
	// Initialize WinInet
	if(m_hInet == NULL)	// This object has not been used yet
	{
		m_hInet.Attach(InternetOpenA("JetIe", INTERNET_OPEN_TYPE_PRECONFIG, NULL, NULL, (m_bAsync ? INTERNET_FLAG_ASYNC : 0)));	// Optional asynchronous mode
		if(m_hInet == NULL)
			ThrowError(CJetIe::LoadString(IDS_E_WININET_SESSION) + L'\n' + CJetIe::GetSystemError());
	}

	// Establish the connection
	if(m_hConnection == NULL)	// The second request could be run on the same connection
	{
		m_hConnection.Attach(InternetConnectA(m_hInet, OnGetHostName(), OnGetPort(), "", "", INTERNET_SERVICE_HTTP, (m_bAsync ? INTERNET_FLAG_ASYNC : 0), (DWORD_PTR)&m_marshalling));
		if(m_hConnection == NULL)	// TODO: check whether this function may return the ERROR_IO_PENDING code
			ThrowError(CJetIe::LoadString(IDS_E_WININET_CONNECT) + L'\n' + CJetIe::GetSystemError());

		if(!IsWindow())
		{
			// Try to create the marshalling window
			HWND	hwndParent = CJetIe::IsWinNT() ? HWND_MESSAGE : NULL;	// Parent of this window, use HWND_MESSAGE under WinNT to create a message-only window
			if(Create(hwndParent, NULL, _T("JetRpcClient Marshalling Window"), WS_POPUP) == NULL)
				ThrowError(CJetIe::GetSystemError());
		}

		// Register for the callback
		if(InternetSetStatusCallbackA(m_hConnection, InternetStatusCallback) == INTERNET_INVALID_STATUS_CALLBACK)
			ThrowError(CJetIe::LoadString(IDS_E_WININET_SESSION) + L'\n' + CJetIe::GetSystemError());
	}

	// Open the request
	LPCSTR	szMediaFormats[] = { "text/xml", NULL };
	m_hRequest.Attach(HttpOpenRequestA(m_hConnection, "POST", OnGetObjectName(), NULL, NULL, szMediaFormats, HTTP_OPEN_REQUEST_FLAGS | (m_bAsync ? INTERNET_FLAG_ASYNC : 0), (DWORD_PTR)&m_marshalling));
	if(m_hRequest == NULL)
	{
		InternetSetStatusCallbackA(m_hConnection, NULL);	// Cancel the callback on failure
		ThrowError(CJetIe::LoadString(IDS_E_WININET_OPENREQ) + L'\n' + CJetIe::GetSystemError());
	}

	// Add headers specifying the content-type
	CStringA	sHeaders = "Content-Type: " + OnGetContentType();
	if(!HttpAddRequestHeadersA(m_hRequest, sHeaders, -1, HTTP_ADDREQ_FLAG_ADD | HTTP_ADDREQ_FLAG_REPLACE))
	{
		DWORD	dwErr = GetLastError();	// Is non-null as there was not a success
		if(dwErr != ERROR_IO_PENDING)	// Pending-error means that the request was stared OK, otherwise, indicates a failure
			ThrowError(CJetIe::LoadString(IDS_E_WININET_SENDREQ) + L'\n' + CJetIe::GetSystemError(dwErr));
		return false;	// Wait async
	}
	// TODO: is this function capable of the asynchronous operation?

	// Request initated, now prepare the data, send it, and execute the request
	return true;
}

void CJetRpcClient::ValidateXmlResponse()
{
	// Check the response for simple parsing errors
	if(m_xmlResponse->parseError->errorCode != 0)
	{
		CStringW	sError;
		sError.Format(L"%s\n%s (line %d, char %d)", CJetIe::LoadString(IDS_E_XML_INVALID), (LPCWSTR)m_xmlResponse->parseError->reason, m_xmlResponse->parseError->line, m_xmlResponse->parseError->linepos);
		ThrowError(sError);
	}

	// Invoke the per-protocol handler
	OnValidateResponse(m_xmlResponse);
}

bool CJetRpcClient::SendData()
{
	// Measure the stream size
	STATSTG	statstg = {0};
	COM_CHECK(m_streamTransmit, Stat(&statstg, STATFLAG_NONAME));

	// Read the data
	int	nBufSize = (int)statstg.cbSize.QuadPart;
	char	*pBuf = m_sBufA.GetBuffer(nBufSize);	// TODO: here we assume that all the content fits within HTTP_BUFFER_SIZE bytes. Overcome this assumption later …
	DWORD	dwRead;
	COM_CHECK(m_streamTransmit, Read(pBuf, nBufSize, &dwRead));
	m_streamTransmit = NULL;	// Release the stream buffers

	// Prepare the buffers
	INTERNET_BUFFERSA	ibIn;
	ZeroMemory(&ibIn, sizeof(ibIn));
	ibIn.dwStructSize = sizeof(ibIn);
	ibIn.Next = NULL;
	ibIn.lpcszHeader = NULL;
	ibIn.dwHeadersLength = 0;
	ibIn.dwHeadersTotal = 0;
	ibIn.lpvBuffer = pBuf;
	ibIn.dwBufferLength = dwRead;
	ibIn.dwBufferTotal = dwRead;

	// Start transmitting the request
	if(!HttpSendRequestExA(m_hRequest, &ibIn, NULL, (m_bAsync ? INTERNET_FLAG_ASYNC : 0), (DWORD_PTR)&m_marshalling))	// If the method has not succeeded
	{
		DWORD	dwErr = GetLastError();	// Is non-null as there was not a success
		if(dwErr != ERROR_IO_PENDING)	// Pending-error means that the request was stared OK, otherwise, indicates a failure
			ThrowError(CJetIe::LoadString(IDS_E_WININET_SENDREQ) + L'\n' + CJetIe::GetSystemError(dwErr));	

		return false;	// Wait for async completion
	}

	return true;	// Go on sync
}

bool CJetRpcClient::ReadData()
{
	bool	bFirst = (m_streamReceive == NULL);

	// Is this the first call? Prepare structures, if yes
	if(bFirst)
	{
		// Init the stream
		ASSERT(m_xmlResponse == NULL);	// Must be cleaned up after the prev run
		CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);	// TODO: workaround
		m_xmlResponse = CJetIe::CreateXmlDocument();
		m_streamReceive = m_xmlResponse;	// Take a stream that will load the XML file async while downloading

		// Prepare the buffers
		ZeroMemory(&m_ibOut, sizeof(m_ibOut));
		m_ibOut.dwStructSize = sizeof(m_ibOut);
	}

	for(int a = 0; a < 0x1000; a++)	// An infinite loop with some kind of a watchdog. This loop executes as long as there is data ready for immediate reading, otherwise, it escapes and lets WinINet prepare some more of data
	{
		if(!bFirst)	// If we already have some of the information read, either thru the sync or async operation
		{
			if(m_ibOut.dwBufferLength == 0)	// No data was received on the last step
				return false;	// Completed, the stream contains the full data received from the server

			// Process the newly-gotten chunk
			DWORD	dwWritten;
			CHECK(m_streamReceive->Write(m_ibOut.lpvBuffer, m_ibOut.dwBufferLength, &dwWritten));
			if(dwWritten != m_ibOut.dwBufferLength)
				ThrowError(CJetIe::LoadString(IDS_E_WININET_READRESP));
		}
		bFirst = false;

		// Prepare the buffers for the next iteration
		m_ibOut.lpvBuffer = m_sBufA.GetBuffer(HTTP_BUFFER_SIZE);
		m_ibOut.dwBufferLength = HTTP_BUFFER_SIZE;

		// Initiate the asynchronous read operation	
		// Note: here we use InternetReadFileExA instead of T because the W version always returns the "Not implemented" error, and this is the only code it contains. Such a shit :)
		if(!InternetReadFileExA(m_hRequest, &m_ibOut, (m_bAsync ? INTERNET_FLAG_ASYNC : 0), (DWORD_PTR)&m_marshalling))	// If the method has failed to complete immediately
		{
			DWORD	dwErr = GetLastError();	// Is non-null as there was not a success
			if(dwErr != ERROR_IO_PENDING)	// Pending-error means that the request was stared OK, otherwise, indicates a failure
				ThrowError(CJetIe::LoadString(IDS_E_WININET_READRESP) + L'\n' + CJetIe::GetSystemError(dwErr));

			return true;	// Request pending. Wait for it asynchronously
		}
	}

	return false;	// Have read all the data synchronously — good luck …
}

bool CJetRpcClient::EndRequest()
{
	if(!HttpEndRequestA(m_hRequest, NULL, (m_bAsync ? INTERNET_FLAG_ASYNC : 0), (DWORD_PTR)&m_marshalling))	// If the method has failed to complete immediately
	{
		DWORD	dwErr = GetLastError();	// Is non-null as there was not a success
		if(dwErr != ERROR_IO_PENDING)	// Pending-error means that the request was stared OK, otherwise, indicates a failure
			ThrowError(CJetIe::LoadString(IDS_E_WININET_SENDREQ) + L'\n' + CJetIe::GetSystemError(dwErr));				

		return false;	// Request pending. Wait for it asynchronously
	}
	return true;	// Happened to be sync; execute one more step immediately
}

void CJetRpcClient::PrepareDataToSend()
{
	m_streamTransmit = OnPrepareDataToSend();
	if(m_streamTransmit == NULL)
		ThrowError(CJetIe::LoadString(IDS_E_NODATATOSEND));
}

/// Some additional services for displaying the internet status constants as human-readable text; included in the Debug version only.
#ifdef _TRACE
typedef struct tagStatus
{
	DWORD	dwStatus;
	LPCWSTR	sStatus;
	LPCWSTR	sDescription;
}InternetStatus;

CStringW	LookupInternetStatus(DWORD dwStatus)
{
	static InternetStatus	statuses[] = 
	{
		{ INTERNET_STATUS_CLOSING_CONNECTION, L"INTERNET_STATUS_CLOSING_CONNECTION", L"Closing the connection to the server. The lpvStatusInformation parameter is NULL." },
		{ INTERNET_STATUS_CONNECTED_TO_SERVER, L"INTERNET_STATUS_CONNECTED_TO_SERVER", L"Successfully connected to the socket address (SOCKADDR) pointed to by lpvStatusInformation." },
		{ INTERNET_STATUS_CONNECTING_TO_SERVER, L"INTERNET_STATUS_CONNECTING_TO_SERVER", L"Connecting to the socket address (SOCKADDR) pointed to by lpvStatusInformation." },
		{ INTERNET_STATUS_CONNECTION_CLOSED, L"INTERNET_STATUS_CONNECTION_CLOSED", L"Successfully closed the connection to the server. The lpvStatusInformation parameter is NULL." },
		{ INTERNET_STATUS_CTL_RESPONSE_RECEIVED, L"INTERNET_STATUS_CTL_RESPONSE_RECEIVED", L"Not implemented." },
		{ INTERNET_STATUS_DETECTING_PROXY, L"INTERNET_STATUS_DETECTING_PROXY", L"Notifies the client application that a proxy has been detected." },
		{ INTERNET_STATUS_HANDLE_CLOSING, L"INTERNET_STATUS_HANDLE_CLOSING", L"This handle value has been terminated." },
		{ INTERNET_STATUS_HANDLE_CREATED, L"INTERNET_STATUS_HANDLE_CREATED", L"Used by InternetConnect to indicate it has created the new handle. This lets the application call InternetCloseHandle from another thread, if the connect is taking too long. The lpvStatusInformation parameter contains the address of an INTERNET_ASYNC_RESULT structure." },
		{ INTERNET_STATUS_INTERMEDIATE_RESPONSE, L"INTERNET_STATUS_INTERMEDIATE_RESPONSE", L"Received an intermediate (100 level) status code message from the server." },
		{ INTERNET_STATUS_NAME_RESOLVED, L"INTERNET_STATUS_NAME_RESOLVED", L"Successfully found the IP address of the name contained in lpvStatusInformation." },
		{ INTERNET_STATUS_PREFETCH, L"INTERNET_STATUS_PREFETCH", L"Not implemented." },
		{ INTERNET_STATUS_RECEIVING_RESPONSE, L"INTERNET_STATUS_RECEIVING_RESPONSE", L"Waiting for the server to respond to a request. The lpvStatusInformation parameter is NULL." },
		{ INTERNET_STATUS_REDIRECT, L"INTERNET_STATUS_REDIRECT", L"An HTTP request is about to automatically redirect the request. The lpvStatusInformation parameter points to the new URL. At this point, the application can read any data returned by the server with the redirect response and can query the response headers. It can also cancel the operation by closing the handle. This callback is not made if the original request specified INTERNET_FLAG_NO_AUTO_REDIRECT." },
		{ INTERNET_STATUS_REQUEST_COMPLETE, L"INTERNET_STATUS_REQUEST_COMPLETE", L"An asynchronous operation has been completed. The lpvStatusInformation parameter contains the address of an INTERNET_ASYNC_RESULT structure." },
		{ INTERNET_STATUS_REQUEST_SENT, L"INTERNET_STATUS_REQUEST_SENT", L"Successfully sent the information request to the server. The lpvStatusInformation parameter points to a DWORD value that contains the number of bytes sent." },
		{ INTERNET_STATUS_RESOLVING_NAME, L"INTERNET_STATUS_RESOLVING_NAME", L"Looking up the IP address of the name contained in lpvStatusInformation." },
		{ INTERNET_STATUS_RESPONSE_RECEIVED, L"INTERNET_STATUS_RESPONSE_RECEIVED", L"Successfully received a response from the server. The lpvStatusInformation parameter points to a DWORD value that contains the number of bytes received." },
		{ INTERNET_STATUS_SENDING_REQUEST, L"INTERNET_STATUS_SENDING_REQUEST", L"Sending the information request to the server. The lpvStatusInformation parameter is NULL." },
		{ INTERNET_STATUS_STATE_CHANGE, L"INTERNET_STATUS_STATE_CHANGE", L"Moved between a secure (HTTPS) and a nonsecure (HTTP) site. The user must be informed of this change; otherwise, the user is at risk of disclosing sensitive information involuntarily. When this flag is set, the lpvStatusInformation parameter points to a status DWORD that contains additonal flags." },
		{ NULL, NULL, NULL }
	};

	CStringW	sRet;
	int	a;
	for(a = 0; (a < 0x1000) && (statuses[a].sStatus != NULL); a++)
	{
		if(statuses[a].dwStatus == dwStatus)
		{
			sRet.Format(L"%s (%#010X) — %s", statuses[a].sStatus, statuses[a].dwStatus, statuses[a].sDescription);
			return sRet;
		}
	}

	sRet.Format(L"UNKNOWN (%#010X) — UNKNOWN", statuses[a].dwStatus);
	return sRet;
}

CStringW	LookupInternetState(DWORD dwStatus)
{
	static InternetStatus	statuses[] = 
	{
		{ INTERNET_STATE_CONNECTED, L"INTERNET_STATE_CONNECTED", L"Connected state (mutually exclusive with disconnected state)." },
		{ INTERNET_STATE_DISCONNECTED, L"INTERNET_STATE_DISCONNECTED", L"Disconnected state. No network connection could be established." },
		{ INTERNET_STATE_DISCONNECTED_BY_USER, L"INTERNET_STATE_DISCONNECTED_BY_USER", L"Disconnected by user request." },
		{ INTERNET_STATE_IDLE, L"INTERNET_STATE_IDLE", L"No network requests are being made by Windows Internet." },
		{ INTERNET_STATE_BUSY, L"INTERNET_STATE_BUSY", L"Network requests are being made by Windows Internet." },
		{ INTERNET_STATUS_USER_INPUT_REQUIRED, L"INTERNET_STATUS_USER_INPUT_REQUIRED", L"The request requires user input to be completed." },
		{ NULL, NULL, NULL }
	};

	CStringW	sRet;
	int	a;
	for(a = 0; (a < 0x1000) && (statuses[a].sStatus != NULL); a++)
	{
		if(statuses[a].dwStatus == dwStatus)
		{
			sRet.Format(L"%s (%#010X) — %s", statuses[a].sStatus, statuses[a].dwStatus, statuses[a].sDescription);
			return sRet;
		}
	}

	sRet.Format(L"UNKNOWN (%#010X) — UNKNOWN", statuses[a].dwStatus);
	return sRet;
}

/// Retuns the display-name of an FSM step.
CStringW LookupFsmState(int nState)
{
	static LPCWSTR states[] = 
	{
		L"stateIdle",
		L"stateInitializing",
		L"stateReadyToSend",
		L"stateTransmitting",
		L"stateReadyToReceive",
		L"stateReceiving",
		L"stateParsingReply",
		L"stateCompleted",
		L"stateCloseRequest",
		L"stateShutdown",
		L"stateNULL"
	};
	if((nState >= 0) && (nState < sizeof(states) / sizeof(*states)))
		return states[nState];
	return L"<Unknown FSM Step>";
}
#endif

void CALLBACK CJetRpcClient::InternetStatusCallback(HINTERNET hInternet, DWORD_PTR dwContext, DWORD dwInternetStatus, LPVOID lpvStatusInformation, DWORD dwStatusInformationLength)
{
	// Diagnotic trace just to see whatsappenin
#ifdef _TRACE
	TRACE(L"InternetStatusCallback: %s.", LookupInternetStatus(dwInternetStatus));
	if(dwInternetStatus == INTERNET_STATUS_STATE_CHANGE)
		TRACE(L"InternetStatusCallback: • state has changed to %s.", LookupInternetState(*(DWORD*)lpvStatusInformation));
#endif	// _TRACE

	// Extract the passed context data
	CMarshallingData *pMD = (CMarshallingData*)dwContext;
	// Check if this is the second call informing that the handle is being closed (all the deinit has occured in response to the first call)
	if(IsBadReadPtr(pMD, sizeof(CMarshallingData)))	// Some deinit has already invalidated the context, don't use it!
		return;	// We cannot issue an error in this case, so just return …
	if((dwInternetStatus == INTERNET_STATUS_HANDLE_CLOSING) && (pMD->dwTag != MARSHALLING_DATA_TAG))
		return;	// Just have nothing to do
	HWND	hwndMarshal = pMD->hwnd;
	bool	bAsync = pMD->bAsync;

	// Errors generated by this block should be marshalled to be handled correctly
	try
	{
		// What is the notification we have received in this callback?
		switch(dwInternetStatus)
		{
		case INTERNET_STATUS_REQUEST_COMPLETE:	// This indicates that an asynchronous operation has completed; check which exactly it was, whether it has succeeded, and what to do then
			{
				// First, check for errors
				INTERNET_ASYNC_RESULT	*pResult = (INTERNET_ASYNC_RESULT*)lpvStatusInformation;
				if(pResult->dwError != ERROR_SUCCESS)
				{
					// Notify the object on the main apartment about the error; then, just quit the callback — the request object's handler should initiate the shutdown sequence
					CoInitialize(NULL);	// Just in case, if it's another thread without an initialized apartment
					CJetIeException::Throw(CJetIe::LoadString(IDS_E_WININET_SENDREQ) + L'\n' + CJetIe::GetSystemError());	// Do not use the instance pointer from another thread. Instead, throw the exception using JetIeException
				}

				// OK, this is a notification about a successful completion of some async operation
				// Marshal the notification to our home apartment
				if(bAsync)
				{
					if(!::PostMessage(hwndMarshal, WM_JETRPCCLIENT_MARSHAL_REQUESTCOMPLETE, 0, 0))
						ASSERT(FALSE && "Marshalling has failed");
				}
				else
					::SendMessage(hwndMarshal, WM_JETRPCCLIENT_MARSHAL_REQUESTCOMPLETE, 0, 0);
			}
			break;
		case INTERNET_STATUS_HANDLE_CLOSING:
			if(bAsync)
			{
				if(!::PostMessage(hwndMarshal, WM_JETRPCCLIENT_MARSHAL_HANDLECLOSING, 0, (LONG)(INT_PTR)hInternet))
					ASSERT(FALSE && "Marshalling has failed");
			}
			else
				::SendMessage(hwndMarshal, WM_JETRPCCLIENT_MARSHAL_HANDLECLOSING, 0, (LONG)(INT_PTR)hInternet);
			break;
		case INTERNET_STATUS_HANDLE_CREATED:
			if(bAsync)
			{
				if(!::PostMessage(hwndMarshal, WM_JETRPCCLIENT_MARSHAL_HANDLECREATED, 0, (LONG)(INT_PTR)hInternet))
					ASSERT(FALSE && "Marshalling has failed");
			}
			else
				::SendMessage(hwndMarshal, WM_JETRPCCLIENT_MARSHAL_HANDLECREATED, 0, (LONG)(INT_PTR)hInternet);
			break;
		}
	}
	catch(_com_error e)
	{
		// Try marshalling the exception information to the calling object
		try
		{
			CStringW	sErrorMessage = COM_REASON(e);
			COM_TRACE();
			ReportError(NULL, hwndMarshal, sErrorMessage, bAsync);	// TODO: check if we're trying to jump from another thread here
		}
		catch(_com_error e)
		{
			// Whoa … everything is quite bad. Fatal error again (the callee has failed to initiate the graceful shutdown)

			// Deinitialize this instance
			InternetCloseHandle(hInternet);			
			
			ASSERT(FALSE);
			// TODO: write implementation similar to one at the first catch of this func
		}
	}
}

bool CJetRpcClient::FSM()
{
#ifdef _TRACE
	TRACE(L"Request FSM is stepping from the %s=%d state.", LookupFsmState(m_state), m_state);	// Report the state
#endif

	// FSM logics
	switch(m_state)
	{
	case stateIdle:
		// Clear the errors left from the previous run
		m_sLastError.Empty();
		m_bTerminationCallbackInvoked = false;
		m_state = stateInitializing;
		m_xmlResponse = NULL;
		m_streamTransmit = NULL;
		m_streamReceive = NULL;
		return true;

	case stateInitializing:
		m_state = stateReadyToSend;
		return BeginRequest();	// Start the request. Either sync or async

	case stateReadyToSend:
		PrepareDataToSend();
		m_state = stateTransmitting;
		return SendData();

	case stateTransmitting:
		m_state = stateReadyToReceive;	// The next state
		return EndRequest();

	case stateReadyToReceive:
		m_state = stateReceiving;
		return true;	// Go to receiving, exec now

	case stateReceiving:
		if(ReadData())	// Request data for reading or process the results of the last request, returns true if wants to be called again
			return false;	// Go async

		// If we're here, then we're thru with reading the data
		m_state = stateParsingReply;
		return true;	// Jump to the state

	case stateParsingReply:
		m_streamReceive = NULL;	// Finish with the stream. Now we have the XML object loaded and will work with it
		ValidateXmlResponse();

		// Finally, the data was received OK!
		TRACE(L"Server reply has been received async:\n%s", (LPCWSTR)(_bstr_t)m_xmlResponse->xml);

		// Switch to the Completed state
		m_state = stateCompleted;
		return true;	// Do it now

	case stateCompleted:

		// Invoke the external callback
		if(!m_bTerminationCallbackInvoked)	// Call back no more than once
		{
			m_bTerminationCallbackInvoked = true;
			OnComplete(m_xmlResponse);
		}

		// Run one more request?
		if(OnWhetherInvokeAgain())
		{
			m_state = stateIdle;
			return true;
		}

		// Cleanup, as no more requests needed
		m_state = OnWhetherCloseConnection() ? stateShutdown : stateCloseRequest;
		return true;

	case stateCloseRequest:
		// Cleanup
		ASSERT(m_streamReceive == NULL);
		m_streamReceive = NULL;
		ASSERT(m_streamTransmit == NULL);
		m_streamTransmit = NULL;

		// Shutdown the request
		m_hRequest.Detach();
		m_state = stateIdle;

		ASSERT(FALSE && "(H) This branch was not used by the moment of implementation and has not been tested yet for correct workflow following this point. Please take care.");

		return false;

	case stateShutdown:
		// Cleanup
		ASSERT(m_streamReceive == NULL);
		m_streamReceive = NULL;
		ASSERT(m_streamTransmit == NULL);
		m_streamTransmit = NULL;

		// Shutdown the request
		m_hRequest.Detach();
		// Close the connection
		m_hConnection.Detach();
		return false;	// Wait for the notification on that the connection has been closed. There will switch to Idle

	default:	// A state that does not suppose asynchronous operations completion
		ASSERT(FALSE);
		ThrowError(CJetIe::LoadString(IDS_INCONSISTENCY));
	}
	ASSERT(FALSE);	// Kinda lost branch
	return true;	// Hmm … nothing returned explicitly. Call once more, as there's a watchdog outta there anyway
}

void CJetRpcClient::RunFSM()
{
	try
	{
		// Call the FSM handler function and keep calling it again al long a required
		int	a;
		for(a = 0; (a < 0x1000) && (FSM()); a++)
			;
		if(!(a < 0x1000))	// Check the watchdog
			ThrowError(CJetIe::LoadString(IDS_INCONSISTENCY));
	}
	catch(_com_error e)	// Trap errors of the FSM and register them
	{
		CStringW	sErrorMessage = COM_REASON(e);
		COM_TRACE();
		ReportError(this, NULL, sErrorMessage, false);	// Can do it sync because it's the same thread
	}
}

void CJetRpcClient::ReportError(CJetRpcClient *pInstance, HWND hwndMarshal, CStringW sErrorMessage, bool bAsync)
{
	// Prepare the data for marshlling
#ifdef _TRACE
	InterlockedIncrement(&m_nBstrOnTheRun);
#endif
	_bstr_t	bsErrorMessage = (LPCWSTR)sErrorMessage;

	if(pInstance != NULL)
		hwndMarshal = pInstance->m_hWnd;	// Use the instance window for marshalling, if known
	else
		ASSERT((hwndMarshal != NULL) && "No instance is specified for error message marshalling, but the marshalling window handle is NULL.");

	// Check if there is a window that can be used for marshalling
	if(::IsWindow(hwndMarshal))
	{
		// Marshal using the window
		if(bAsync)
		{
			if(!::PostMessage(hwndMarshal, WM_JETRPCCLIENT_MARSHAL_ERROR, 0, (LONG)(INT_PTR)bsErrorMessage.Detach()))	// Async
				ASSERT(FALSE && "Marshalling has failed");
		}
		else
			::SendMessage(hwndMarshal, WM_JETRPCCLIENT_MARSHAL_ERROR, 0, (LONG)(INT_PTR)bsErrorMessage.Detach());	// Sync
	}
	else	// No window. Either it has not been created yet, or there was a problem on or before creating of the window
	{
		// We're still working sync in this case, even though the mode may be set to async
		BOOL	bDummy;
		if(pInstance != NULL)
			pInstance->OnMarshalError(WM_JETRPCCLIENT_MARSHAL_ERROR, 0, (LONG)(INT_PTR)bsErrorMessage.Detach(), bDummy);
		else
			ASSERT(FALSE && "Some error needs to be marshalled, but there is no window to marshal to and the instance is not known at the same time.");
	}
}

LRESULT CJetRpcClient::OnMarshalError(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = TRUE;

	try
	{
		// Extract the error message
		_bstr_t	bsErrorMessage((BSTR)(INT_PTR)lParam, false);
#ifdef _TRACE
		InterlockedDecrement(&m_nBstrOnTheRun);
#endif
		TRACE(L"An error has occured while processing the request. %s", (LPCWSTR)bsErrorMessage);

		// Save the error message for later use
		if(m_sLastError.IsEmpty())	// Do not overwrite the original error message with subsequent errors
			m_sLastError = (LPCTSTR)bsErrorMessage;

		// Inform the external callbacks, if any, of the failure
		if(!m_bTerminationCallbackInvoked)
		{
			m_bTerminationCallbackInvoked = true;	// And call no more
			OnError((LPCWSTR)bsErrorMessage);
		}
	}
	catch(_com_error e)
	{
		// An error while reporting an error — wo we can do nothing more
		// Just try to shut down gently
		COM_TRACE();
		ASSERT(FALSE && "A failure handler has in turn reported a failure. Can do nothing about that.");
	}

	// Cancel further processing (these will cause global request deinit by the callback)
	m_hRequest.Detach();
	m_hConnection.Detach();

	if(!IsWindow())	// No window, and the callback most likely has not been set => noone will invoke the OnHandleClosing callback, do some cleanup right here
		m_state = stateIdle;

	return 0;
}

LRESULT CJetRpcClient::OnMarshalRequestComplete(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = TRUE;
	try
	{
		// Invoke the next pack of FSM steps
		RunFSM();
	}
	catch(_com_error e)
	{
		CStringW sErrorMessage = COM_REASON(e);
		COM_TRACE();
		ReportError(this, NULL, sErrorMessage, false);	// Do it sync, on the same thread
	}
	return 0;
}

LRESULT CJetRpcClient::OnMarshalHandleCreated(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = TRUE;
	return 0;
}

LRESULT CJetRpcClient::OnMarshalHandleClosing(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = TRUE;

	// Monitor closing of the connection only. There may be more than one request per connection, do not monitor closing of request handles
	// Note that if the connection has actually been closed, we have to look at the previous value!
	if(((m_hConnection != NULL) && ((HINTERNET)(DWORD_PTR)lParam == m_hConnection)) || ((HINTERNET)(DWORD_PTR)lParam == m_hConnection.Previous))
	{
		try
		{
			// Detach the callback function
			InternetSetStatusCallbackA(m_hConnection, NULL);

			// Set state to idle
			m_state = stateIdle;

			// Deinitialize marshalling
			DestroyWindow();

			// TODO: close the handles, just in case
		}
		COM_CATCH();
	}

	return 0;	// Don't report any errors as that's gonna make no good
}

LRESULT CJetRpcClient::OnCreateWindow(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = TRUE;
	TRACE(L"JetRpcClient Marshalling window created.");

	// Lock the object lifetime while this window exists (and request runs).
	OnLockObject();

	// Initialize the marshalling data.
	ASSERT(m_marshalling.dwTag == 0);
	m_marshalling.dwTag = MARSHALLING_DATA_TAG;
	m_marshalling.hwnd = m_hWnd;
	m_marshalling.bAsync = m_bAsync;

	return 0;
}

void CJetRpcClient::OnFinalMessage(HWND hWnd)
{
	TRACE(L"JetRpcClient Marshalling window destroyed.");

	// Deinit the marshalling data.
	ASSERT(m_marshalling.dwTag == MARSHALLING_DATA_TAG);
	m_marshalling.dwTag = 0;
	m_marshalling.hwnd = NULL;
	
	// Unlock the object lifetime
	OnUnlockObject();
}

bool CJetRpcClient::IsAsync()
{
	return m_bAsync;
}

XmlDocument CJetRpcClient::GetResponse()
{
	return m_xmlResponse;
}
