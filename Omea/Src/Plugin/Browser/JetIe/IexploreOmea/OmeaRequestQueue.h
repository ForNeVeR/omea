// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// COmeaRequestQueue — implements queueing the Omea requests and executing them at a later time.
//
// This class is not thread-safe. Creating a new instance is not a costly operation, so you should create a new one whenever needed.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once

#include "../JetIe.h"
#include "OmeaSettingStore.h"

#define OMEA_REQUEST_QUEUE_MUTEX	_T("JetBrains.IexploreOmea.RequestQueue")

class COmeaRequestQueue
{
public:
	/// Ctor.
	COmeaRequestQueue();

	/// Dtor.
	virtual ~COmeaRequestQueue();

// Implementation — fields
protected:
	/// A named mutex that controls access to the queue file.
	static CMutex	m_mutexQueueFile;

	/// A timer that retries the submit attempts.
	/// NULL if the requests are not running at this time.
	/// Protected by the m_mutexStaticLock.
	static UINT_PTR	m_timerSubmitAttempts;

	/// An unnamed mutex that controls access to the static variables of this class, including the timer startup and shutdown.
	static CMutex	m_mutexStaticLock;

	/// A DWORD value that represents the moment we started waiting for Omea startup (in ticks), or NULL, if we're not waiting.
	/// When we're starting Omea, we decrease the polling interval to achieve better response.
	/// After a successful submittal or a timeout, the interval is restored.
	/// Protected by the m_mutexStaticLock.
	static DWORD	m_dwWaitingForOmeaStartup;

	/// A maximum interval of time, in ticks, for which we wait the Omea to start and process our requests.
	/// As it elapses, we fall back the polling interval to its normal value.
	/// Protected by the m_mutexStaticLock.
	static DWORD	m_dwWaitingForOmeaStartupLimit;

// Implementation — operations
protected:
	/// Disable copy constructor.
	COmeaRequestQueue(const COmeaRequestQueue &other);

	/// Disable assignment operator.
	COmeaRequestQueue &operator=(const COmeaRequestQueue &other);

	/// Read states for the queue processing.
	enum QueueReadState { qrMethodName, qrParameters };

// Operations
public:
	/// Tries to enqueue a request, shows the UI notifications if necessary.
	/// If the requests queue is prohibited, drops the request and displays the failure message.
	/// Returns whether a request was actually enqueued.
	static __declspec(nothrow) bool EnqueueRequest(CStringA sMethod, CStringA sData);

	/// Returns whether queueing of the requests is allowed.
	static __declspec(nothrow) bool IsQueueAllowed();

	/// Returns the path-name of the file which is used for storing the request queue.
	static CString GetRequestQueueFileName() throw(_com_error);

	/// If the request queue is enabled and there are requests in the queue, starts the timer which makes the submit attempts.
	/// Returns whether the submit attempts were actually started (note that failure to start mught mean that they're already running).
	static __declspec(nothrow) bool BeginSubmitAttempts();

	/// If the request queue is enabled and there are requests in the queue returns true.
	/// Also erases the queue if needed.
	static __declspec(nothrow) bool CanBeSubmitted();

	/// A function that is called on the queue-submit-attempt timer events.
	static VOID CALLBACK OnQueueTimer(HWND hwnd, UINT uMsg, UINT_PTR idEvent, DWORD dwTime);

	/// Erases all the stored requests and empties the requests queue.
	/// Locks the queue only if required (omits the lock if called from inside another lock).
	static __declspec(nothrow) void EraseRequestQueue(bool bLock);

	/// Turns off the timer that makes the submit attempts periodically.
	static __declspec(nothrow) void AbortSubmitAttempts();

	/// Checks asynchronously for Omea presence and initiates the queue-submitting procedure in the positive case.
	static __declspec(nothrow) void SubmitQueue();

	/// Performs the sync queue submission after Omea presence has been checked by an async request.
	static __declspec(nothrow) void SubmitQueueImpl();
};
