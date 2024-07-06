// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "StdAfx.h"
#include "JetIE.h"

// Static data initialization
DWORD CJetIe::m_dwDdeInstance = 0;

CJetIe::CJetIe(void)
{
}

CJetIe::~CJetIe(void)
{
}

char CJetIe::ToHexChar(char c)
{
	return c < 0x0A ? c + 0x30 : c - 0x0A + 0x41;
}

char CJetIe::FromHexChar(char c)
{
	return (c >= '0' && c <= '9') ? (c - '0') : ((c >= 'A' && c <= 'F') ? (c - 'A' + 0xA) : ((c >= 'a' && c <= 'f') ? (c - 'a' + 0xa) : (0)));
}

CStringA CJetIe::UrlEncode(CStringW sSource)
{
	/*
	DWORD	dwNewLen = 0;
	InternetCanonicalizeUrl( URI, NULL, &dwNewLen, ICU_ENCODE_PERCENT | ICU_NO_META );
	if(dwNewLen == 0)
	return L"";	// TODO: err?

	CTString	strNew;
	strNew.resize(dwNewLen);
	if(!InternetCanonicalizeUrl( URI, &strNew[0], &dwNewLen, ICU_ENCODE_PERCENT | ICU_NO_META ))
	return L"";	// TODO: err?

	return strNew.c_str();*/

	int	nLen = sSource.GetLength();
	CStringA	sRet;

	for(int a = 0; a < nLen; a++)
	{
		// Check if this character needs to be encoded
		if(((sSource[a] >= L'0') && (sSource[a] <= L'9')) || ((sSource[a] >= L'A') && (sSource[a] <= L'Z')) || ((sSource[a] >= L'a') && (sSource[a] <= L'z')))
			sRet.AppendChar((char)sSource[a]);	// Can go as is
		else if(sSource[a] < 0x80)	// An US-ASCII character, encode as one byte
		{
			sRet.AppendChar('%');
			sRet.AppendChar(ToHexChar((sSource[a] & 0x00F0) >> 0x04));
			sRet.AppendChar(ToHexChar((sSource[a] & 0x000F) >> 0x00));
		}
		else	// A UCS-2 minus US-ASCII character, encode as one word
		{
			sRet.AppendChar('%');
			sRet.AppendChar('u');
			sRet.AppendChar(ToHexChar((sSource[a] & 0xF000) >> 0x0C));
			sRet.AppendChar(ToHexChar((sSource[a] & 0x0F00) >> 0x08));
			sRet.AppendChar(ToHexChar((sSource[a] & 0x00F0) >> 0x04));
			sRet.AppendChar(ToHexChar((sSource[a] & 0x000F) >> 0x00));
		}
	}

	return sRet;
}

CStringW CJetIe::UrlDecode(CStringA sSource)
{
	CStringW	sRet;
	int	nLength = sSource.GetLength();

	for(int a = 0; a < nLength; a++)
	{
		if(sSource[a] == '%')	// An encoded char
		{
			if((a + 5 < nLength) && ((sSource[a + 1] == 'u') || (sSource[a + 1] == 'U')))	// Unicode
			{
				sRet.AppendChar(
					((WCHAR)FromHexChar(sSource[a + 2]) << 0x0C) |
					((WCHAR)FromHexChar(sSource[a + 3]) << 0x08) |
					((WCHAR)FromHexChar(sSource[a + 4]) << 0x04) |
					((WCHAR)FromHexChar(sSource[a + 5]) << 0x00)
					);
				a += 5;
			}
			else if(a + 2 < nLength)	// One-byte
			{
				sRet.AppendChar(
					((WCHAR)FromHexChar(sSource[a + 1]) << 0x04) |
					((WCHAR)FromHexChar(sSource[a + 2]) << 0x00)
					);
				a += 2;
			}
			else
				sRet.AppendChar(sSource[a]);	// Something invalid
		}
		else	// A non-encoded char
			sRet.AppendChar(sSource[a]);
	}

	return sRet;
}

CString CJetIe::GetModuleFileName()
{
	CString	sFileName;
	::GetModuleFileName(GetModuleInstanceHandle(), sFileName.GetBuffer(MAX_PATH), MAX_PATH);
	sFileName.ReleaseBuffer();
	return sFileName;
}

HINSTANCE CJetIe::GetModuleInstanceHandle()
{
	return _AtlComModule.m_hInstTypeLib;
}

HWND CJetIe::WindowFromBrowser(SHDocVw::IWebBrowser2Ptr &oBrowser)
{
	try
	{
		SHDocVw::IWebBrowserAppPtr	oApp = oBrowser;

		HWND	hwnd;
		COM_CHECK(oApp, get_HWND((long*)&hwnd));

		return hwnd;
	}
	catch(_com_error e)
	{
		COM_TRACE();
		return NULL;
	}
}

void CJetIe::SerializeResource(LPCTSTR szResType, LPCTSTR szResName, IStreamPtr oStream, bool bRewindStream /*= true*/)
{
	TRACE(L"SerializeResource(name: %s).", ToW(szResName));
	HRSRC	hResInfo = FindResource(GetModuleInstanceHandle(), szResName, szResType);
	if(hResInfo == NULL)
	{
		CStringW	sError;
		sError.Format(L"SerializeResource failed to find resource (%#010X).", GetLastError());
		CJetIeException::Throw(sError);
	}

	HGLOBAL	hResData = LoadResource(GetModuleInstanceHandle(), hResInfo);
	if(hResData == NULL)
	{
		CStringW	sError;
		sError.Format(L"SerializeResource failed to load resource (%#010X).", GetLastError());
		CJetIeException::Throw(sError);
	}

	LPVOID	pResBytes = LockResource(hResData);
	if(pResBytes == NULL)
	{
		CStringW	sError;
		sError.Format(L"SerializeResource failed to lock resource (%#010X).", GetLastError());
		CJetIeException::Throw(sError);
	}

	HRESULT	hRet;

	DWORD	dwWritten;
	hRet = oStream->Write(pResBytes, SizeofResource(GetModuleInstanceHandle(), hResInfo), &dwWritten);
	if(FAILED(hRet))
	{
		CStringW	sError;
		sError.Format(L"SerializeResource failed to write resource (%#010X).", hRet);
		CJetIeException::Throw(sError);
	}

	// Rewind the stream
	if(bRewindStream)
	{
		LARGE_INTEGER	li;
		li.QuadPart = 0;
		COM_CHECK(oStream, Seek(li, STREAM_SEEK_SET, NULL));
	}

	// OK
	TRACE(L"SerializeResource wrote resource (name:\"%s\") OK (size %d)\r\n", ToW(szResName), dwWritten);
}

CString CJetIe::LoadStringT(UINT nID)
{
	DWORD	dwErr = GetLastError();	// Store it here to restore later
	CString	sRet;
	if(sRet.LoadString(nID))
	{
		SetLastError(dwErr);	// Restore the error code
		return sRet;
	}
	else
	{
		TRACE(L"The string resource %d could not be loaded (#%010X).", nID, GetLastError());
		ASSERT(FALSE && "The string resource could not be loaded.");
		SetLastError(dwErr);	// Restore the error code
		return _T("?");
	}
}

CStringW CJetIe::LoadString(UINT nID)
{
	DWORD	dwErr = GetLastError();	// Store it here to restore later
	CStringW	sRet;
	if(sRet.LoadString(nID))
	{
		SetLastError(dwErr);	// Restore the error code
		return sRet;
	}
	else
	{
		TRACE(L"The string resource %d could not be loaded (#%010X).", nID, GetLastError());
		ASSERT(FALSE);
		SetLastError(dwErr);	// Restore the error code
		return L"?";
	}
}

XmlDocument CJetIe::CreateXmlDocument()
{
	XmlDocument	oDoc;
	oDoc.CreateInstance(__uuidof(MSXML2::DOMDocument40));
	if(oDoc != NULL)
	{
		TRACE(L"Created MSXML2::DOMDocument40.");
		return oDoc;
	}
	oDoc.CreateInstance(__uuidof(MSXML2::DOMDocument30));
	if(oDoc != NULL)
	{
		TRACE(L"Created MSXML2::DOMDocument30.");
		return oDoc;
	}
	oDoc.CreateInstance(__uuidof(MSXML2::DOMDocument26));
	if(oDoc != NULL)
	{
		TRACE(L"Created MSXML2::DOMDocument26.");
		return oDoc;
	}
	oDoc.CreateInstance(__uuidof(MSXML2::DOMDocument));
	if(oDoc != NULL)
	{
		TRACE(L"Created MSXML2::DOMDocument.");
		return oDoc;
	}
	oDoc.CreateInstance(L"MSXML.DOMDocument");
	if(oDoc != NULL)
	{
		TRACE(L"Created MSXML.DOMDocument.");
		return oDoc;
	}

	// Oops … No MSXML
	CJetIeException::Throw(L"FATAL ERROR: could not instantiate any version of MSXML.");
	return NULL;	// Dummy return to prevent exceptions due to the implicit throw statement in the above line
}

bool CJetIe::IsWinNT()
{
	// Get the OS version
	OSVERSIONINFO	osvi = {0};
	osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);
	GetVersionEx(&osvi);
	return osvi.dwPlatformId == VER_PLATFORM_WIN32_NT;
}

CString CJetIe::GetSystemErrorT(DWORD dwError /*= GetLastError()*/)
{
	// First, retrieve the message
	LPVOID lpMsgBuf;
	if(!FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,NULL, dwError, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT) /* Default language */, (LPTSTR)&lpMsgBuf, 0, NULL))
		return _T("The operation could not be completed.");

	// Retrieve to a safe container
	CString	sMessage = (LPCTSTR)lpMsgBuf;

	// Cut-out the trailing line break, if present
	sMessage.TrimRight(_T('\n'));
	sMessage.TrimRight(_T('\r'));

	// Free the buffer.
	LocalFree(lpMsgBuf);

	return sMessage;
}

CStringW CJetIe::GetSystemError(DWORD dwError /*= GetLastError()*/)
{
	// First, retrieve the message
	LPVOID lpMsgBuf;
	if(!FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,NULL, dwError, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT) /* Default language */, (LPTSTR)&lpMsgBuf, 0, NULL))
		return L"The operation could not be completed.";

	// Retrieve to a safe container
	CStringW	sMessage = (LPCWSTR)CT2W((LPCTSTR)lpMsgBuf);

	// Cut-out the trailing line break, if present
	sMessage.TrimRight(L'\n');
	sMessage.TrimRight(L'\r');

	// Free the buffer.
	LocalFree(lpMsgBuf);

	return sMessage;
}

void CJetIe::ShowPopupNotification(LPCWSTR sMessage, LPCWSTR sTitle, CPopupNotification::Mood mood /*= CPopupNotification::pmNotify*/)
{
	try
	{
		IPopupNotificationPtr	oPopup(__uuidof(CPopupNotification));
		oPopup->Show((_variant_t)(_bstr_t)sMessage, (_variant_t)(_bstr_t)(sTitle != NULL ? sTitle : L""), (_variant_t)(long)mood, vtMissing, vtMissing);	// TODO: specify the parent window
	}
	catch(_com_error e)
	{
		COM_TRACE();
		TRACE(L"Warning! Could not display the popup notification, have to show the WinAPI MessageBox.");

		DWORD	dwIcon = 0;
		switch(mood)
		{
		case CPopupNotification::pmNotify:
			dwIcon = MB_ICONINFORMATION;
			break;
		case CPopupNotification::pmWarn:
			dwIcon = MB_ICONWARNING;
			break;
		case CPopupNotification::pmStop:
			dwIcon = MB_ICONSTOP;
			break;
		default:
			ASSERT(FALSE);
		}

		MessageBox(NULL, CW2T((LPCWSTR)sMessage), CW2T((LPCWSTR)sTitle), MB_OK | dwIcon);	// TODO: specify the parent window
	}
}

CStringW CJetIe::HashMd5(CStringW sData)
{
	// Create the cryptoprovider, if needed
	CCryptProv	cryptProv;
	CHECK(cryptProv.Initialize(PROV_RSA_FULL, NULL, MS_DEF_PROV, CRYPT_VERIFYCONTEXT));

	// Prepare the UTF-8-encoded data for hashing
	int	nBufSize;
	CHECK_BOOL(nBufSize = WideCharToMultiByte(CP_UTF8, 0, sData, -1, NULL, 0, NULL, NULL));
	CStringA	sMultibyteData;
	CHECK_BOOL(WideCharToMultiByte(CP_UTF8, 0, sData, -1, sMultibyteData.GetBuffer(nBufSize), nBufSize, NULL, NULL));
	sMultibyteData.ReleaseBuffer(-1);	// Recalc the string length to exclude the trailing zero

	// Create and init the hash
	CHashHandle	hash;
	CHECK_BOOL(CryptCreateHash(cryptProv.GetHandle(), CALG_MD5, NULL, NULL, &hash));
	CHECK_BOOL(CryptHashData(hash, (const BYTE*)(LPCSTR)sMultibyteData, sMultibyteData.GetLength(), NULL));

	// Retrieve the hashed value
	DWORD	dwSize = NULL;
	CStringA	szBuf;
	CHECK_BOOL(CryptGetHashParam(hash, HP_HASHVAL, NULL, &dwSize, NULL));
	BYTE	*pBuf = (BYTE*)szBuf.GetBuffer(dwSize);
	CHECK_BOOL(CryptGetHashParam(hash, HP_HASHVAL, pBuf, &dwSize, NULL));

	// Convert to the hex string
	CStringW	sRet;
	wchar_t	szHexChars[] = L"0123456789abcdef";
	for(int a = 0; a < (int)dwSize; a++)
	{
		sRet.AppendChar(szHexChars[(pBuf[a] & 0xF0) >> 4]);
		sRet.AppendChar(szHexChars[(pBuf[a] & 0x0F) >> 0]);
	}
	szBuf.Empty();

	hash.Detach();

	return sRet;
}

CString CJetIe::GetDataFolderPathName(bool bLocal)
{
	CString	sPath;

#if defined(_UNICODE)	// NT
	if(SHGetFolderPath(NULL, (bLocal ? CSIDL_LOCAL_APPDATA : CSIDL_APPDATA) | CSIDL_FLAG_CREATE, NULL, SHGFP_TYPE_CURRENT, sPath.GetBuffer(MAX_PATH + 1)) != S_OK)
	{
		CStringW	sError;
		sError.Format(CJetIe::LoadString(IDS_E_DATAFOLDER), (bLocal ? L"LOCAL" : L""), L"UNICODE", GetSystemError());
		CJetIeException::Throw(sError);
	}
#else	// Non-NT
	if((!SHGetSpecialFolderPath(NULL, sPath.GetBuffer(MAX_PATH + 1), (bLocal ? CSIDL_LOCAL_APPDATA : CSIDL_APPDATA), TRUE)) && (!SHGetSpecialFolderPath(NULL, sPath.GetBuffer(MAX_PATH + 1), CSIDL_APPDATA, TRUE)))	// Fallback to APPDATA, if needed
	{
		CStringW	sError;
		sError.Format(CJetIe::LoadString(IDS_E_DATAFOLDER), (bLocal ? L"LOCAL" : L""), L"ANSI", GetSystemError());
		CJetIeException::Throw(sError);
	}
#endif	// ^?NT
	sPath.ReleaseBuffer();	// Calculate the actual length of the string

	// Try creating all the directories, just in case. If they already exist, nothing bad happens.
	sPath += _T("\\JetBrains");
	CreateDirectory(sPath, NULL);
	sPath += _T("\\") + CJetIe::LoadStringT(IDS_PLUGIN_NAME);
	CreateDirectory(sPath, NULL);

	return sPath;
}

IRawActionManagerPtr CJetIe::GetActionManager()
{
	IRawActionManagerPtr	oActionManager = CActionManager::GetInstance();	// Here some kind of simple caching is implemented
	return oActionManager;
}

HWND CJetIe::WindowFromVariant(_variant_t vtWindow)
{
	// Check if it's a missing parameter
	if(V_IS_MISSING(&vtWindow))	// The optional parameter is missing
		return NULL;

	// Try as an IOleWindow
	try
	{
		// Get a pointer
		IUnknownPtr	oUnk = (IUnknown*)vtWindow;

		// Check if NULL
		if(oUnk == NULL)
			return NULL;

		// IOleWindow approach
		HWND	hwnd;
		CHECK(((IOleWindowPtr)oUnk)->GetWindow(&hwnd));
		return hwnd;	// If we've reached this point without an exception, then the window handle has been gotten successfully
	}
	COM_CATCH_SILENT();

	// Try as an explicit integer representation of the HWND value
	try
	{
		HWND	hwnd = (HWND)(INT_PTR)(long)vtWindow;	// Try converting to a number
		return hwnd;	// If we've reached this point without an exception, then the window handle has been gotten successfully
	}
	COM_CATCH_SILENT();

	CJetIeException::Throw(CJetIe::LoadString(IDS_E_INVALIDVARIANTWINDOW));
	return NULL;	// A dummy return
}

bool CJetIe::BooleanFromVariant(_variant_t vtBoolean, bool bDefault /*= false*/)
{
	// Check if it's a missing parameter
	if(V_IS_MISSING(&vtBoolean))	// The optional parameter is missing
		return bDefault;

	// Try to coerce
	try
	{
		return (bool)vtBoolean;
	}
	COM_CATCH_SILENT();

	CJetIeException::Throw(CJetIe::LoadString(IDS_E_INVALIDBOOLEAN));
	return NULL;	// A dummy return
}

void CJetIe::OpenInNewWindow(CStringW sUrl, SHDocVw::IWebBrowser2Ptr oBrowser /*= NULL*/)
{
	MSHTMLLite::IHTMLDocument2Ptr	oDoc = oBrowser != NULL ? oBrowser->Document : NULL;
	TRACE(L"Opening the URL %s in a new window.", sUrl);

	bool	bUseBackup = true;	// Signals that the backup impl (Shell run) should be used for opening the window if the main scheme fails

	// Open a new window by deriving it from an existing one
	/*	// Commented-out because this attempt gets usually intercepted by the popup blocka'
	if(oDoc != NULL)
	{
		try
		{
			// Derive a new window from the current browser's window
			MSHTMLLite::IHTMLWindow2Ptr	oWindow;
			COM_CHECK(oDoc, get_parentWindow(&oWindow));
			MSHTMLLite::IHTMLWindow2Ptr	oNewWindow;
			COM_CHECK(oWindow, open((_bstr_t)sUrl, (_bstr_t)L"", (_bstr_t)L"", VARIANT_FALSE, &oNewWindow));	// Do not specify the window name so that all the links would open in a new window, not reuse the same

			bUseBackup = false;	// Succeeded!!
		}
		COM_CATCH();
	}
	*/

	// DDE Approach: establich a DDE conversation to an existing instance of the browser, and tell it to open a new window
	// ACHTUNG! This method should be called from a single thread only, the primary UI thread. This is assumed by the implementation.
	if(bUseBackup)
	{
		TRACE(L"Trying to open URL via DDE.");
		try
		{
			// Prepare the DDE command for opening a URL in a new window
			CStringW	sCommand;
			sCommand.Format(L"\"%s\",,0", sUrl);

			// Initialize the DDE, if necessary
			m_dwDdeInstance = 0;	// TODO: implement properly
			//if(m_dwDdeInstance == 0)
				CHECK_ERROR(DdeInitialize(&m_dwDdeInstance, (PFNCALLBACK)DdeCallback, APPCMD_CLIENTONLY, 0u));

			// Connect to the DDE server (Internet Explorer, in our case) and start the conversation
			CDdeStringHandle	dsService(m_dwDdeInstance, L"iexplore");
			CDdeStringHandle	dsTopic(m_dwDdeInstance, L"WWW_OpenURLNewWindow");
			CDdeConversationHandle	conversation(DdeConnect(m_dwDdeInstance, dsService, dsTopic, NULL));
			if((HCONV)conversation == 0)
			{
				DWORD	dwError = DdeGetLastError(m_dwDdeInstance);
				TRACE(L"Could not establish a DDE conversation: %#010X.", dwError);
				CJetIeException::ThrowSystemError();
			}

			// Start a transaction that opens the URL
			DWORD	dwResult;
			CString	sData = ToT(sCommand);
			CHECK_BOOL(DdeClientTransaction((LPBYTE)(LPCTSTR)sData, sData.GetLength() * sizeof(TCHAR), conversation, NULL, 0, XTYP_EXECUTE, TIMEOUT_ASYNC, &dwResult));

			// Succeeded in this method, don't fallback to others
			bUseBackup = false;
		}
		COM_CATCH();
	}

	// Backup scheme: Shell Run
	// No HTML document currently loaded (or it has failed to create a window for us); employ shell run for the default browser
	if(bUseBackup)
	{
		TRACE(L"Trying to open URL via Shell Run.");
		DWORD	dwRet;
		if((dwRet = (DWORD)(DWORD_PTR)ShellExecute(CJetIe::WindowFromBrowser(oBrowser), NULL, CW2T((LPCWSTR)sUrl), NULL, NULL, SW_SHOWNORMAL)) < 32)
			CJetIeException::Throw(CJetIe::LoadString(IDS_E_OPENBROWSERWINDOW) + L' ' + CJetIe::GetSystemError(dwRet));
	}
}

bool CJetIe::GetSelectedText(MSHTMLLite::IHTMLDocument2Ptr oDoc, CStringW *psText, CStringW *psHtml) throw(_com_error)
{
	CStringW	sText;
	CStringW	sHtml;
	bool	bHasSel = false;

	// If the document is NULL, it may be a non-HTML document, report no selection for the documents of such a type
	if(oDoc != NULL)
	{
		// Try getting the selection from the current document
		MSHTMLLite::IHTMLSelectionObjectPtr	oSel;
		COM_CHECK(oDoc, get_selection(&oSel));

		IDispatchPtr	oDispRange;
		MSHTMLLite::IHTMLTxtRangePtr	oRange;
		COM_CHECK(oSel, createRange(&oDispRange));
		oRange = oDispRange;

		BSTR	bstrSelection = 0;

		// Get the HTML selection text
		COM_CHECK(oRange, get_htmlText(&bstrSelection));
		_bstr_t	bsSelectionHtml(bstrSelection, false);
		if((BSTR)bsSelectionHtml != NULL)
			sHtml = (LPCWSTR)bsSelectionHtml;

		// Get the plain selection text
		COM_CHECK(oRange, get_text(&bstrSelection));
		_bstr_t	bsSelectionText(bstrSelection, false);
		if((BSTR)bsSelectionText != NULL)
			sText = (LPCWSTR)bsSelectionText;

		// If both types are non-empty, assume that the selection is nonempty
		bHasSel = (!sHtml.IsEmpty()) && (!sText.IsEmpty());

		// If there was no selection available in the current document, try recursing to its child frames
		if(!bHasSel)
		{
			// Enumerate the child frames of this document
			MSHTMLLite::IHTMLFramesCollection2Ptr	oFrames;
			COM_CHECK(oDoc, get_frames(&oFrames));

			long	nFrames = 0;
			COM_CHECK(oFrames, get_length(&nFrames));

			for(long a = 0; (a < nFrames) && (!bHasSel); a++)	// Enumerate thru the immediate-child frames, stop when selection is found
			{
				// Get the next frame object
				_variant_t	vtIndex(a), vtItem;
				COM_CHECK(oFrames, item(&vtIndex, &vtItem));

				// Get the frame's document object (if available)
				IHTMLWindow2Ptr	oFrameWindow = (IDispatch*)vtItem;
				IHTMLDocument2Ptr	oFrameDoc;
				if(oFrameWindow != NULL)
					COM_CHECK(oFrameWindow, get_document(&oFrameDoc));

				// Query it for the selection recursively
				bHasSel = GetSelectedText(oFrameDoc, &sText, &sHtml);
			}
		}
	}

	// Pass out the return parameters (be them empty or not) and retrun the success/failure flag
	if(psText != NULL)
		*psText = sText;
	if(psHtml != NULL)
		*psHtml = sHtml;
	return bHasSel;
}

CStringW CJetIe::GetDllVersion(__int64 *pMyVersion) throw(_com_error)
{
	try
	{
		CStringW	sVersion;

		// Retrieve the version info block
		DWORD	dwDummy;
		DWORD	dwSize = GetFileVersionInfoSize(CJetIe::GetModuleFileName(), &dwDummy);
		if(!dwSize)
			CJetIeException::ThrowSystemError();
		CStringA	sBufA;
		char	*pBuffer = sBufA.GetBuffer(dwSize);
		GetFileVersionInfo(CJetIe::GetModuleFileName(), NULL, dwSize, pBuffer);	// All the version info in a single block

		// Read the version information from it
		LPTSTR	pValue;
		UINT	dwLen;
		if(!VerQueryValue(pBuffer, _T("\\"), (LPVOID*)&pValue, &dwLen))
			CJetIeException::ThrowSystemError(GetLastError());
		if(dwLen != sizeof(VS_FIXEDFILEINFO))
			CJetIeException::Throw(CJetIe::LoadString(IDS_FAIL));

		VS_FIXEDFILEINFO	*pVI = (VS_FIXEDFILEINFO*)pValue;
		if(pVI->dwSignature != 0xFEEF04BD)	// The magic cookie of this structure
			CJetIeException::ThrowComError(E_UNEXPECTED);
		sVersion.Format(L"%d.%d.%d.%d", HIWORD(pVI->dwProductVersionMS), LOWORD(pVI->dwProductVersionMS), HIWORD(pVI->dwProductVersionLS), LOWORD(pVI->dwProductVersionLS));

		// If specified, fill in the version-as-a-number
		if(pMyVersion != NULL)
			*pMyVersion = (((__int64)pVI->dwProductVersionMS) << 32) || ((__int64)pVI->dwProductVersionMS);

		return sVersion;
	}
	catch(_com_error e)
	{
		CStringW	sError = COM_REASON(e);
		COM_TRACE();
		CJetIeException::Throw(CJetIe::LoadString(IDS_E_GETVERSIONFAILED) + L" " + sError);
		return L"";	// Dummy warning-killa retval
	}
}

HDDEDATA CJetIe::DdeCallback(UINT, UINT, HCONV, HDDEDATA, HDDEDATA, HDDEDATA, HDDEDATA, HDDEDATA)
{
	return 0;
}

void CJetIe::ShowHtmlDialog(HWND hwndParent, CStringW sUrl, CStringW sDialogStyle, _variant_t &vtArguments, bool bNonModal /*= true*/) throw(_com_error)
{
	// Create a URL moniker for the resource we're about to load
	IMonikerPtr	oUrlMoniker;
	_com_util::CheckError(CreateURLMoniker(NULL, sUrl, &oUrlMoniker));

	// Show the dialog, take 1: try to show it non-modally
	try
	{
		// Load MSHTML
		HINSTANCE m_hinstMSHTML = NULL;
		if(m_hinstMSHTML == NULL)
			m_hinstMSHTML = LoadLibrary(_T("MSHTML.DLL"));	// Need to load the MSHTML DLL
		CHECK(m_hinstMSHTML != NULL ? S_OK : E_FAIL);	// Succeeded in loading?

		// Locate the function
		SHOWHTMLDIALOGEXFN* pfnShowHTMLDialogEx;
		pfnShowHTMLDialogEx = (SHOWHTMLDIALOGEXFN*)GetProcAddress(m_hinstMSHTML, ("ShowHTMLDialogEx"));
		CHECK(pfnShowHTMLDialogEx != NULL ? S_OK : E_FAIL);

		// Invoke
		CHECK(pfnShowHTMLDialogEx(hwndParent, oUrlMoniker, (bNonModal ? HTMLDLG_MODELESS : HTMLDLG_MODAL) | HTMLDLG_VERIFY, &vtArguments, (LPWSTR)(LPCWSTR)sDialogStyle, NULL));

		return;	// Shown OK
	}
	catch(_com_error e)
	{
		COM_TRACE();
		TRACE(L"Warning: could not display the modeless HTML dialog for the popup message. Failling back to the modal version.");
	}

	// Show the dialog, take 2: try to show it modally
	_COM_SMARTPTR_TYPEDEF(IHostDialogHelper, __uuidof(IHostDialogHelper));
	IHostDialogHelperPtr	pHDH;
	_com_util::CheckError(CoCreateInstance(CLSID_HostDialogHelper, NULL, CLSCTX_INPROC, IID_IHostDialogHelper, (void**)&pHDH));
	_com_util::CheckError(pHDH->ShowHTMLDialog(hwndParent, oUrlMoniker, &vtArguments, (LPWSTR)(LPCWSTR)sDialogStyle, NULL, NULL));
}
