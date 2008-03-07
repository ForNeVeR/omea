/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "StdAfx.h"
#include "OmeaRequestQueue.h"

#include "OmeaRequest.h"

// Init the static variables
UINT_PTR	COmeaRequestQueue::m_timerSubmitAttempts = NULL;
CMutex	COmeaRequestQueue::m_mutexStaticLock(NULL, FALSE, NULL);	// Create an unnamed mutex
CMutex	COmeaRequestQueue::m_mutexQueueFile(NULL, FALSE, OMEA_REQUEST_QUEUE_MUTEX);	// Create a machine-global named mutex
DWORD	COmeaRequestQueue::m_dwWaitingForOmeaStartup = NULL;
DWORD	COmeaRequestQueue::m_dwWaitingForOmeaStartupLimit = NULL;

COmeaRequestQueue::COmeaRequestQueue(void)
{
}

COmeaRequestQueue::~COmeaRequestQueue(void)
{
}

bool COmeaRequestQueue::EnqueueRequest(CStringA sMethod, CStringA sData)
{
	// Is the queueing allowed?
	if(!IsQueueAllowed())
	{
		TRACE(L"An attempt to queue a request has been forcefully rejected due to deferred requests being disabled.");
		CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_NO_OMEA_QUEUE), NULL, CPopupNotification::pmStop);
		return false;
	}

	try
	{	// Close the file when falling off this scope
		CMutexLock	lock(m_mutexQueueFile);	// Lock the queue

		// Simple validness check
		if((!sMethod.GetLength()) || (!sData.GetLength()))
			CHECK(E_INVALIDARG);

		{	// Close the file when falling off this scope
			CHandle	hFile(CreateFile(GetRequestQueueFileName(), GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_NOT_CONTENT_INDEXED | FILE_FLAG_SEQUENTIAL_SCAN, NULL));
			if( hFile == INVALID_HANDLE_VALUE )
				CJetIeException::Throw(CJetIe::LoadString(IDS_E_OMEAQUEUEFILE) + L' ' + CJetIe::GetSystemError());
			SetFilePointer(hFile, 0, NULL, FILE_END);	// Ensure we append not overwrite

			DWORD	dwWritten;
			WriteFile(hFile, (LPCSTR)sMethod, sMethod.GetLength() * sizeof(char), &dwWritten, NULL);
			WriteFile(hFile, " ", 1 * sizeof(char), &dwWritten, NULL);
			WriteFile(hFile, (LPCSTR)sData, sData.GetLength() * sizeof(char), &dwWritten, NULL);
			WriteFile(hFile, "\r\n", 2 * sizeof(char), &dwWritten, NULL);
		}
	}
	catch(_com_error e)
	{	// If something has failed, report the failure
		CStringW	sError = COM_REASON(e);
		COM_TRACE();
		CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_E_QUEUEINGFAILED) + L'\n' + sError, NULL, CPopupNotification::pmStop);
		return false;
	}

	// Check if Omea should be started to process the request
	COmeaSettingStore	settings;
	if(settings.GetProfileInt(COmeaSettingStore::setAutorunOmea))
	{
		CString	sFileName = settings.GetOmeaExecutableFileName();
		HINSTANCE	hinstRet = ShellExecute(NULL, NULL, sFileName, NULL, NULL, SW_SHOWDEFAULT);
		if(hinstRet < (HINSTANCE)(INT_PTR)32)	// Something has failed if below 32
			CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_REQUESTQUEUED_OMEARUNFAILED), NULL, CPopupNotification::pmStop);
		else	// Omea started OK
		{
			CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_REQUESTQUEUED_OMEARUNOK), NULL, CPopupNotification::pmWarn);

			// Stop processing the requests queue so that it were restarted (see below) at a faster rate
			{
				CMutexLock	lock(m_mutexStaticLock);	// Protect m_dwWaitingForOmeaStartup
				m_dwWaitingForOmeaStartup = GetTickCount();	// Mark the state as waiting for Omea
			}
			AbortSubmitAttempts();
		}
	}
	else	// Omea should not be run, notify of enqueueing
		CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_REQUEST_QUEUED), NULL, CPopupNotification::pmWarn);

	// Start processing the queue, if any
	BeginSubmitAttempts();

	return true;
}

bool COmeaRequestQueue::IsQueueAllowed()
{
	try
	{
		return !!COmeaSettingStore().GetProfileInt(COmeaSettingStore::setAllowDeferredRequests);
	}
	COM_CATCH();
	return false;
}

CString COmeaRequestQueue::GetRequestQueueFileName()
{
	return CJetIe::GetDataFolderPathName(true) + _T("\\RequestQueue.txt");
}

bool COmeaRequestQueue::BeginSubmitAttempts()
{
	COmeaSettingStore	settings;

	// Not runing yet?
	if(m_timerSubmitAttempts != NULL)	// No interlocking needed here
	{
		TRACE(L"The queue-submit attempts were not started because their timer is already running.");
		return false;	// Submit attempts already running
	}

	// Allowed to submit by timer?
	if(!settings.GetProfileInt(COmeaSettingStore::setAllowSubmitAttempts))
	{
		TRACE(L"The queue-submit attempts were not started because autosubmit is not allowed.");
		return false;
	}

	// Check if the requests queue can be submitted at this time
	if(!CanBeSubmitted())
		return false;

	// Start it!
	{
		CMutexLock	lock(m_mutexStaticLock);
		if(m_timerSubmitAttempts != NULL)	// Race check: whether another thread has started the timer while we were checking for presence of the queue
			return false;

		// Choose the retry attempts interval; it depends on whether we're pinging in idle more or are waiting for Omea to start
		int	nIntervalSec;
		if(m_dwWaitingForOmeaStartup == NULL)
		{	// Normal mode
			nIntervalSec = settings.GetProfileInt(COmeaSettingStore::setDeferredSubmitInterval);
			TRACE(L"Starting the queue-submit attempts in Normal mode, interval is %d.", nIntervalSec);
		}
		else
		{	// Omea-waiting-for mode
			nIntervalSec = settings.GetProfileInt(COmeaSettingStore::setOmeaStartSubmitInterval);
			m_dwWaitingForOmeaStartupLimit = settings.GetProfileInt(COmeaSettingStore::setOmeaStartupTimeLimit) * 1000;	// Load the Limit setting from the Registry, and convert to ticks
			TRACE(L"Starting the queue-submit attempts in OmeaStart mode, interval is %d.", nIntervalSec);
		}
		m_timerSubmitAttempts = SetTimer(NULL, NULL, nIntervalSec * 1000, OnQueueTimer);
		TRACE(L"Started the queue-submit attempts.");
		return true;
	}
}

VOID CALLBACK COmeaRequestQueue::OnQueueTimer(HWND hwnd, UINT uMsg, UINT_PTR idEvent, DWORD dwTime)
{
	// Check if the queue has gotten empty or any other condition prevents from submitting it
	if(!CanBeSubmitted())
	{
		AbortSubmitAttempts();
		return;
	}

	// Try to submit the queue
	SubmitQueue();
}

void COmeaRequestQueue::AbortSubmitAttempts()
{
	CMutexLock	lock(m_mutexStaticLock);	// Unlock when falling off the scope
	if(m_timerSubmitAttempts != NULL)
	{
		KillTimer(NULL, m_timerSubmitAttempts);
		TRACE(L"Request queue submit attempts have been aborted.");
	}
	m_timerSubmitAttempts = NULL;
}

void COmeaRequestQueue::EraseRequestQueue(bool bLock)
{
	{
		CMutexLock	lock(m_mutexQueueFile, false);	// Unlock when falling off scope; lock only if required
		if(bLock)
			lock.Lock();

		{
			// Change the file to a zero-sized empty one
			CHandle	hFileCleanup(CreateFile(GetRequestQueueFileName(), GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_NOT_CONTENT_INDEXED | FILE_FLAG_SEQUENTIAL_SCAN, NULL));
			SetEndOfFile(hFileCleanup);	// Truncate to the null size
		}
	}
}

__declspec(nothrow) bool COmeaRequestQueue::CanBeSubmitted()
{
	COmeaSettingStore	settings;

	// Check if the submit attempts are allowed
	if(!settings.GetProfileInt(COmeaSettingStore::setAllowSubmitAttempts))
	{
		TRACE(L"The queue-submit attempts could not be started because the auto-submit attempts are not allowed according to the user settings.");
		return false;
	}

	// Check if the deferred requests are allowed
	if(!settings.GetProfileInt(COmeaSettingStore::setAllowDeferredRequests))
	{
		TRACE(L"The queue-submit attempts could not be started because the deferred requests are not allowed according to the user settings.");
		EraseRequestQueue(true);	// This call should just clean up the requests queue, as the requests are disabled and there is something in the queue
		return false;
	}

	// Check if there is a queue file and that it has a non-zero size (ie if there are any requests on the queue)
	{
		CMutexLock	lock(m_mutexQueueFile);	// Lock the queue, unlock on falling off this scope

		WIN32_FIND_DATA	fd;
		HANDLE	hFind = FindFirstFile(GetRequestQueueFileName(), &fd);	// Auto-close the search when falling off the scope
		if(hFind == INVALID_HANDLE_VALUE)
		{
			TRACE(L"The queue-submit attempts were not started because the queue file cannot be found.");
			return false;	// The file does not exists or no rights to access the file, do not attempt to read it
		}
		FindClose(hFind);

        if((fd.nFileSizeHigh == 0) && (fd.nFileSizeLow == 0))
		{
			TRACE(L"The queue-submit attempts were not started because the queue file is zero-sized.");
			return false;	// The queue is zero-sized, do not attempt to read it
		}
	}

	return true;	// Passed
}

void COmeaRequestQueue::SubmitQueue()
{
	try
	{
		if(!CanBeSubmitted())
			return;	// Don't ping if there's nothing to do

		// Check if we should exit the Omea-ping mode now
		{
			CMutexLock lock(m_mutexStaticLock);
			if((m_dwWaitingForOmeaStartup != NULL) && (m_dwWaitingForOmeaStartupLimit != NULL) && (GetTickCount() - m_dwWaitingForOmeaStartup >= m_dwWaitingForOmeaStartupLimit))
			{
				m_dwWaitingForOmeaStartup = NULL;	// Waiting for Omea no more
				TRACE(L"Timeout has expired waiting for Omea to start, switching to the Normal queue mode.");

				// Restart the queue processing with a new polling interval
				AbortSubmitAttempts();
				BeginSubmitAttempts();
			}
		}

		IOmeaRequestPtr	oRequest(__uuidof(COmeaRequest));
		COM_CHECK(oRequest, SubmitRequestQueue());
	}
	catch(_com_error e)
	{
		TRACE(L"Could not submit the request queue. ");
		COM_TRACE();
	}
}

void COmeaRequestQueue::SubmitQueueImpl()
{
	if(!CanBeSubmitted())
		return;	// Something has happened to the queue

	bool	bFailed = false;	// Helps to exit the while-file-not-thru loop. On failure, we should see if some of the requests have somehow completed, and remove them from the queue

	{	// Lock scope
		CMutexLock2	lock(m_mutexQueueFile, false);
		if(!lock.LockWeak(100))	// Wait for 100 ms (in case someone's checking or writing the queue under a lock and not submiting it)
		{
			TRACE(L"Could not start processing the requests queue: queue-lock could not be obtained.");
			return;	// The queue is locked already. Maybe someone's already trying to submit it. Do not wait
		}
		TRACE(L"Started processing the requests queue.");

		DWORD	dwDonePosition = 0;	// Position in the file after the last request that had been sent successfully and before the first request that has failed or not been processed at all. The CR-LF disposition against this pointer is unspecified
		DWORD	dwPrevDonePosition = 0;	// Previous value of the dwDonePosition value. After a request has been processed and its dwDonePosition has been shifted, it points directly before the last request, thus providing for erasing this request from the queue

		// Debug action: make a copy of the queue we're currently processing
#ifdef _DEBUG
		COmeaSettingStore	settings;
		if(settings.GetProfileInt(COmeaSettingStore::setDebugMakeQueueCopy))
		{
			CString	sQueueCopy = COleDateTime::GetCurrentTime().Format();
			sQueueCopy.Replace(_T(':'), _T('-'));
			sQueueCopy = CJetIe::GetDataFolderPathName(TRUE) + _T("\\RequestsQueueCopy.") + sQueueCopy + _T(".txt");
			TRACE(L"Queue copy file name: %s.", ToW(sQueueCopy));
			CopyFile(GetRequestQueueFileName(), sQueueCopy, FALSE);
		}
#endif

		{	// Close the file when falling off this scope
			CHandle	hFile(CreateFile(GetRequestQueueFileName(), GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_NOT_CONTENT_INDEXED | FILE_FLAG_SEQUENTIAL_SCAN, NULL));
			if( hFile == INVALID_HANDLE_VALUE )
			{
				TRACE(L"Cannot open the queue file for read-writing. %s.", CJetIe::GetSystemError());
				return;
			}

			int	nState = qrMethodName;
			CStringA	sMethodName;
			CStringA	sParameters;

			char	ch;
			DWORD	dwRead;
			while((!bFailed) && (ReadFile(hFile, &ch, sizeof(char), &dwRead, NULL)) && (dwRead != 0))
			{
				switch(nState)
				{
				case qrMethodName:
					if((ch == '\r') || (ch == '\n'))	// Skip line breaks from the prev line
						continue;
					if(ch == ' ')	// Space delimits parameters from method name
						nState = qrParameters;
					else
						sMethodName.AppendChar(ch);
					break;

				case qrParameters:
					if((ch == '\r') || (ch == '\n'))	// Line breaks end the parameter data
					{
						if(sMethodName.GetLength() > 0)	// Do not submit if something's empty
						{
							try
							{
								// Try to submit the request
								IOmeaRequestPtr	oRequest(__uuidof(COmeaRequest));
								COM_CHECK(oRequest, put_Async(VARIANT_FALSE));	// Must execute sync
								COM_CHECK(oRequest, put_AllowQueueing(VARIANT_FALSE));	// Do not enqueue once more on failure
								COM_CHECK(oRequest, SubmitRequest((_bstr_t)(LPCSTR)sMethodName, (_bstr_t)(LPCSTR)sParameters));

								// Sending a request has succeeded if we reached this point
								// Get the current position in file which is between the processed and unprocessed requests
								dwPrevDonePosition = dwDonePosition;	// Save the prev position, which is a position before the current (succeeded) request
								dwDonePosition = SetFilePointer(hFile, 0, NULL, FILE_CURRENT);	// This is the position after the request, thus they both define the requests range
								ASSERT(!(dwDonePosition % sizeof(char)) && "A non-integer number of chars in the request detected.");

								// Erase the processed request body to avoid any re-posting of it
								SetFilePointer(hFile, dwPrevDonePosition, NULL, FILE_BEGIN);	// Position before this request
								DWORD	dwWritten = 0;
								int	nChars = (int)((dwDonePosition - dwPrevDonePosition) / sizeof(char));
								for(int a = 0; a < nChars; a++)
									WriteFile(hFile, "\r", sizeof(char), &dwWritten, NULL);
								ASSERT(SetFilePointer(hFile, 0, NULL, FILE_CURRENT) == dwDonePosition);	// Check that we've come to the same position
							}
							catch(_com_error e)
							{	
								COM_TRACE();
								// It means that we still could not submit the request due to connectivity problems. Stop processing the queue and mark the done requests, if any, as processed
								// Mark the processing as Failed and break reading the file
								bFailed = true;
							}
						}
						else
						{	// Something was empty, do not send, but advance the success pointer
							dwDonePosition = SetFilePointer(hFile, 0, NULL, FILE_CURRENT);
						}

						// Reset the strings
						sMethodName.Empty();
						sParameters.Empty();

						// Switch to the request-method-name reading mode
						nState = qrMethodName;
					}
					else	// Collect the characters
						sParameters.AppendChar(ch);
				}
			}

			// If the queue has totally succeeded, shrink the queue file to a zero size (which means that the queue is empty)
			if(!bFailed)
			{
				SetFilePointer(hFile, 0, NULL, FILE_BEGIN);	// Jump to the beginning
				SetEndOfFile(hFile);	// Make file end at the beginning
			}
		}	// Queue file gets closed at this point

		// Show a success notification
		if(!bFailed)
			CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_QUEUE_SUBMITTED_OK), NULL, CPopupNotification::pmNotify);

		// Maybe, there were some failed requests but some have succeeded. Erase those that were successful and leave the incompleted ones. This can be done by moving the data up the file, but, to do it simplier, we'll just replace the successful requests with CR characters so that they were ignored at the next attempt. The situation when some requests succeed while others fail mean that Omea was suddenly shut down (as we consider only "Omea not reachable" failures here), which is not likely to happen often, and the empty space in the queue will not be kept long

	}	// Unlock the queue

	TRACE(L"The requests queue has %sbeen processed.", (bFailed ? L"not " : L""));

	// Stop the submission timer on success
	if(!CanBeSubmitted())
		AbortSubmitAttempts();
}
