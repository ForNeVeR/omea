// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

/////////////////////////////////////////////////////////////////////////////
// Contains the smart wrappers definitions for some WinAPI entities.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "StdAfx.h"
#include "Wrappers.h"
#include "JetIe.h"

/////////////////////////////////////////////////////////////////////////////
// CLockMutex Class
//
// Serves as a smart pointer for the mutex-controlled section by providing the mutex release over any exit from the section.
//

CLockMutex::CLockMutex(HANDLE hMutex, bool bJustTry /*= false*/)
{
	ASSERT(hMutex != NULL);
	m_hMutex = NULL;

	if(!bJustTry)
	{
		// Take ownership
		if(WaitForSingleObject(hMutex, 30000) == WAIT_TIMEOUT)	// Wait for thirty seconds at maximum
			CJetIeException::Throw(L"Timeout elapsed waiting for the queue lock to be acquired.");
		m_hMutex = hMutex;
	}
	else
	{
		if(WaitForSingleObject(hMutex, 0) == WAIT_TIMEOUT)	// Do not wait, just check/try
			return;	// Failed to lock the mutex
        m_hMutex = hMutex;	// Store only if succeeded to set the lock
	}
}

CLockMutex::~CLockMutex()
{
	if(m_hMutex != NULL)	// May be NULL if lock failed to be set in bJustTry mode
		ReleaseMutex(m_hMutex);	// Release!
	ASSERT((m_hMutex = NULL, true));	// Assign in debug version only
}

CLockMutex::operator HANDLE()
{
	return m_hMutex;
}

/////////////////////////////////////////////////////////////////////////////
// CImageList Class
//
/// Serves as a smart wrapper that automatically frees the contained handle to an image list.
/// THIS CLASS IS NOT THREAD SAFE!!

CImageList::CImageList()
{
	m_hil = NULL;
}

CImageList::~CImageList()
{
	Detach();
}

HIMAGELIST CImageList::Attach(HIMAGELIST hil)
{
	HIMAGELIST	hilRet = Detach();	// Free the old resource, if needed
	m_hil = hil;
	return hilRet;
}

HIMAGELIST CImageList::Detach()
{
	HIMAGELIST	hilRet = m_hil;
	if(m_hil != NULL)
	{
		ImageList_Destroy(m_hil);
		m_hil = NULL;
	}
	return hilRet;
}

CImageList::operator HIMAGELIST()
{
	return m_hil;
}

CImageList::operator LPARAM()
{
	return (LPARAM)m_hil;
}

CMutexLock2::CMutexLock2( CMutex& mtx, bool bInitialLock ) :
	m_mtx( mtx ),
	m_bLocked( false )
{
	if( bInitialLock )
	{
		Lock();
	}
}

CMutexLock2::~CMutexLock2() throw()
{
	if( m_bLocked )
	{
		Unlock();
	}
}

void CMutexLock2::Lock()
{
	DWORD dwResult;

	ATLASSERT( !m_bLocked );
	dwResult = ::WaitForSingleObject( m_mtx, INFINITE );
	if( dwResult == WAIT_ABANDONED )
	{
		ATLTRACE(atlTraceSync, 0, _T("Warning: abandoned mutex 0x%x\n"), (HANDLE)m_mtx);
	}
	m_bLocked = true;
}

bool CMutexLock2::LockWeak(DWORD dwTimeout)
{
	DWORD dwResult;

	ATLASSERT( !m_bLocked );
	dwResult = ::WaitForSingleObject( m_mtx, dwTimeout );
	if( dwResult == WAIT_ABANDONED )
	{
		ATLTRACE(atlTraceSync, 0, _T("Warning: abandoned mutex 0x%x\n"), (HANDLE)m_mtx);
	}
	m_bLocked = dwResult != WAIT_TIMEOUT;
	return m_bLocked;
}

void CMutexLock2::Unlock() throw()
{
	ATLASSERT( m_bLocked );

	m_mtx.Release();
}


/*
/////////////////////////////////////////////////////////////////////////////
// CHandle Class
//
// Serves as a smart wrapper that automatically frees the contained handle
//

CHandle::CHandle()
{
	m_handle = NULL;
}

CHandle::CHandle(HANDLE handle)
{
	m_handle = handle;
}

CHandle::~CHandle()
{
	if(m_handle != NULL)
		CloseHandle(m_handle);
	m_handle = NULL;
}

HANDLE CHandle::Attach(HANDLE handle)
{
	// Release prev value
	if(m_handle != NULL)
		CloseHandle(m_handle);
	m_handle = NULL;

	// Replace invalid handle with NULL ofr uniformity
	if(handle == INVALID_HANDLE_VALUE)
		handle = NULL;

	return (m_handle = handle);	// If NULL, has no effect
}

HANDLE CHandle::Detach()
{
	if(m_handle != NULL)
		CloseHandle(m_handle);
	m_handle = NULL;
}
*/
