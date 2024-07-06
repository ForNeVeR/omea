// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

/////////////////////////////////////////////////////////////////////////////
// Contains the smart wrappers declarations for some WinAPI entities.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once

/////////////////////////////////////////////////////////////////////////////
// CLockMutex Class
//
/// Serves as a smart pointer for the mutex-controlled section by providing the mutex release over any exit from the section.
class CLockMutex
{
public:
	CLockMutex(HANDLE hMutex, bool bJustTry = false);	// Creates the instance and obtains ownership of the mutex object. If bJustTry is true, does not wait for the mutex if it's already owned by someone, just returns. If failed to lock the mutex, then converting to HANDLE returns NULL.
	~CLockMutex();	// Releases the mutex.
	operator void *();	// Extracts the locked mutex, or, if JustTry and failed to lock, a NULL.
protected:
	HANDLE	m_hMutex;	// Handle to the controlled mutex.
	CLockMutex(const CLockMutex &other);	// A protected copy constructor.
	CLockMutex &operator=(const CLockMutex &other);	// A protected assignment operator.
};

/////////////////////////////////////////////////////////////////////////////
// CImageList Class
//
/// Serves as a smart wrapper that automatically frees the contained handle to an image list.
/// THIS CLASS IS NOT THREAD SAFE!!
class CImageList
{
public:
	CImageList();
	~CImageList();
	HIMAGELIST Attach(HIMAGELIST hil);	// Attaches to another list, disposes of the old one, if needed. Returns the previous value.
	HIMAGELIST Detach();	// Resigns ownership over the encapsulated image list.
	operator HIMAGELIST();	// Extracts the contained image list.
	operator LPARAM();	// Extracts the contained image list as an LPARAM value.
protected:
	HIMAGELIST	m_hil;	// The encapsulated image list.
};

// Smart wrapper for the GDI Objects
template<class T> class CGdiObject
{
public:
	CGdiObject() { m_handle = NULL; }
	CGdiObject(T handle)	{ m_handle = handle; }
	~CGdiObject()	{ Detach(); }
	void Attach(T handle)	{ Detach(); m_handle = handle; }
	void Detach()	{ if(m_handle != NULL) { if(!DeleteObject(m_handle)) TRACE(L"Warning: could not delete a handle (%#010X)!", GetLastError()); } }
	operator T() { return m_handle; }
protected:
	T	m_handle;
};

typedef CGdiObject<HBRUSH>	CBrush;
typedef CGdiObject<HFONT>	CFont;
typedef CGdiObject<HPEN>	CPen;

template<class T, BOOL (__stdcall *Release)(T handle)> class CBoolReleasedHandle
{
public:
	CBoolReleasedHandle()	{ m_handle = NULL; m_handlePrevious = NULL;	}
	CBoolReleasedHandle(T handle)	{	m_handle = handle; m_handlePrevious = NULL;	}
	~CBoolReleasedHandle()	{	Detach();	}
	operator T()	{	return m_handle;	}
	void Detach()	{	if(m_handle != NULL) { m_handlePrevious = m_handle; if(!Release(m_handle)) TRACE(L"Warning: could not close the handle (%#010X)!", GetLastError()); m_handle = NULL; }	}
	void Attach(T handle)	{ Detach(); m_handle = handle; }
	T *operator&()	{	return &m_handle;	}
	T GetPrevious()	{	return m_handlePrevious;	}
	__declspec(property(get=GetPrevious)) T Previous;
protected:
	T	m_handle;
	T	m_handlePrevious;
private:
	CBoolReleasedHandle(const CBoolReleasedHandle&);
	CBoolReleasedHandle &operator=(const CBoolReleasedHandle&);
};
// Derived types
typedef CBoolReleasedHandle<HINTERNET, InternetCloseHandle>	CInternetHandle;
//typedef CBoolReleasedHandle<HCRYPTPROV, CryptReleaseContext>	CCryptoProviderHandle;
typedef CBoolReleasedHandle<HCRYPTHASH, CryptDestroyHash>	CHashHandle;
typedef CBoolReleasedHandle<HMENU, DestroyMenu>	CPopupMenuHandle;
typedef CBoolReleasedHandle<HCONV, DdeDisconnect>	CDdeConversationHandle;
typedef CBoolReleasedHandle<DWORD, DdeUninitialize>	CDdeDllInstanceHandle;

/// A mutex that extends the standard ATL mutex and allows to make weak locking attempts: locks the mutex if it's free, and reports a failure without waiting if it's already lcoked.
/// Mostly copypasted from ATL.
class CMutexLock2
{
public:
	CMutexLock2( CMutex& mtx, bool bInitialLock = true );
	~CMutexLock2() throw();

	void Lock();
	bool LockWeak(DWORD dwTimeout);	// Just returns (without waiting) if a mutex is already locked.
	void Unlock() throw();

// Implementation
private:
	CMutex& m_mtx;
	bool m_bLocked;

// Private to prevent accidental use
	CMutexLock2( const CMutexLock2& ) throw();
	CMutexLock2& operator=( const CMutexLock2& ) throw();
};

// Codepage conversion routines
inline CStringW ToW(CStringW s)	{ return s; }
inline CStringW ToW(CStringA s)	{ return (LPCWSTR)CA2W((LPCSTR)s); }
inline CStringA ToA(CStringW s)	{ return (LPCSTR)CW2A((LPCWSTR)s); }
inline CStringA ToA(CStringA s)	{ return s; }

#ifdef _UNICODE
#define ToT ToW
#else
#define ToT ToA
#endif

// A smart wrapper for the DDE string handle. Throws COM errors on problems.
#ifdef _UNICODE
#define CP_EITHER CP_WINUNICODE
#else
#define CP_EITHER CP_WINANSI
#endif
class CDdeStringHandle
{
// Data
protected:
	HSZ	m_handle;	// Handle to the string we're serving
	DWORD	m_dwDdeInstance;	// Handle to the current session of DDE usage
public:
	CDdeStringHandle(DWORD dwDdeInstance, LPCWSTR sz)	{ m_dwDdeInstance = dwDdeInstance; m_handle = DdeCreateStringHandle(dwDdeInstance, ToT(sz), CP_EITHER); if(m_handle == 0) { TRACE(L"Could not create the DDE String Handle: %#010X.", DdeGetLastError(m_dwDdeInstance)); _com_raise_error(E_FAIL); } }
	operator HSZ()	{ return m_handle; }
	~CDdeStringHandle()	{ if((m_handle != 0) && (m_dwDdeInstance != 0)) { if(!DdeFreeStringHandle(m_dwDdeInstance, m_handle)) ASSERT(FALSE && "Could not free a DDE string handle."); } m_handle = 0; }
private:	// Prohibit cloning
	CDdeStringHandle(const CDdeStringHandle &);
	CDdeStringHandle &operator=(const CDdeStringHandle &);
};

/// This class frees an array of HMENU handles. It does not provide any interface to the array, just stores the item during the object lifetime and disposes of the resources upon destruction.
class CPopupMenuHandleArray
{
public:
	CPopupMenuHandleArray() {}
	~CPopupMenuHandleArray() { for(std::vector<HMENU>::iterator it = m_handles.begin(); it != m_handles.end(); ++it) DestroyMenu(*it); m_handles.erase(m_handles.begin(), m_handles.end()); }
	void Add(HMENU handle) { m_handles.push_back(handle); }
protected:
	std::vector<HMENU>	m_handles;
private:
	CPopupMenuHandleArray(const CPopupMenuHandleArray&);
	CPopupMenuHandleArray &operator=(const CPopupMenuHandleArray&);
};

/*
/////////////////////////////////////////////////////////////////////////////
// CHandle Class
//
/// Serves as a smart wrapper that automatically frees the contained handle.
/// The policy of this class prevents from storing the INVALID_HANDLE_VALUE, instead, it's replaced with NULL and is treated equally.
/// THIS CLASS IS NOT THREAD SAFE!!
class CHandle
{
public:
	// Initializes a wrapper around an invalid handle.
	CHandle();

	// Initializes the wrapper and attaches to the handle specified.
	CHandle(HANDLE handle);

	// Destroys the wrapper and releases the handle, if there is any under ownership of this wrapper object.
	virtual ~CHandle();

	// Extracts the contained handle.
	void operator HANDLE();

	// Attaches a handle and assumes taking ownership over it. Detaches from the previous handle, if any. The returned value is the new handle value, or NULL, if not a valid handle. Attaching to NULL results in a correct detachment of the current value (including closing the handle).
	HANDLE Attach(HANDLE handle);

	// Detaches from a handle without releasing it. Returns the previous handle.
	HANDLE Detach();

protected:
	/// Handle to the contained resource
	HANDLE	m_handle;

	CHandle(const CHandle &);	// Do not allow duplication
	CHandle &operator=(const CHandle &);	// Do not allow assignment
};
*/
