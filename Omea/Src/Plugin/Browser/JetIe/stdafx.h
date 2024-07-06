// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once
#pragma warning(disable: 4995)	// "name was marked as #pragma deprecated" — suppress for the standard headers

#ifndef STRICT
#define STRICT
#endif

// Modify the following defines if you have to target a platform prior to the ones specified below.
// Refer to MSDN for the latest info on corresponding values for different platforms.
#ifndef WINVER				// Allow use of features specific to Windows 95 and Windows NT 4 or later.
#define WINVER 0x0501		// Change this to the appropriate value to target Windows 98 and Windows 2000 or later.
#endif

#ifndef _WIN32_WINNT		// Allow use of features specific to Windows NT 4 or later.
#define _WIN32_WINNT 0x0501	// Change this to the appropriate value to target Windows 2000 or later.
#endif

#ifndef _WIN32_WINDOWS		// Allow use of features specific to Windows 98 or later.
#define _WIN32_WINDOWS 0x0410 // Change this to the appropriate value to target Windows Me or later.
#endif

#ifndef _WIN32_IE			// Allow use of features specific to IE 4.0 or later.
#define _WIN32_IE 0x0600	// Change this to the appropriate value to target IE 5.0 or later.
#endif

#define _ATL_APARTMENT_THREADED
#define _ATL_NO_AUTOMATIC_NAMESPACE

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit

// turns off ATL's hiding of some common and often safely ignored warning messages
#define _ATL_ALL_WARNINGS

#include <atlbase.h>
#include <atlcom.h>
#include <atlwin.h>
#include <atltypes.h>
#include <atlctl.h>
#include <atlhost.h>

//DECLARE_TRACE_CATEGORY( TRACE_VERBOSE )

#include <comdef.h>	// Basic COM declarations
#include <comutil.h>	// Compiler COM Support Utilities (_bstr_t, etc)
#include <docobj.h>	// OLE Document related stuff
#include <exdispid.h>	// DISPIDs for the MSHTML/WebBrowser events
#include <strsafe.h>	// Safe string operations
#include <WinINet.h>	// WinINet functions support
#include <Shlguid.h>	// Service Identifiers and other useful crap
#include <statreg.h>	// ATL Registry Scripts support for registering/unregistering the components
#include <shlobj.h>	// Shell objects support (like getting path to shell folders)
#include <atlcomtime.h>	// ATL Date/Time routunes
#include <atlsync.h>	// ATL synchronization primitives
#include <atlcrypt.h>	// ATL support for cryptography
#include <CommCtrl.h>	// Windows Common Controls
#include <map>	// STL
#include <vector>	// STL
#include <list>	// STL
#include <atlsimpstr.h>	// String services
#include <atlstr.h>	// String services

// Custom debug points to the stock implementation by default
/*
#if(defined(_DEBUG))
	#define TRACE_T ATLTRACE
#else	//(defined(_DEBUG))
	#if(defined(_TRACE))
		void DirectTrace(LPCTSTR pstrFormat, ...);
		#define TRACE_T DirectTrace
	#else	// (defined(_TRACE))
		#define TRACE_T	__noop
	#endif	// (defined(_TRACE))
#endif	//(defined(_DEBUG))
	*/
#if((defined(_DEBUG)) && (!defined(_TRACE)))
#define _TRACE
#endif	// ((defined(_DEBUG)) && (!defined(_TRACE)))

#if(defined(_TRACE))
	void DirectTrace(LPCWSTR pstrFormat, ...);
	#define TRACE DirectTrace
#else	// (defined(_TRACE))
	#define TRACE __noop
#endif	// (defined(_TRACE))

#define ASSERT	ATLASSERT

// Import the needed underlaying interfaces
#pragma warning(disable: 4192)
#import "libid:EAB22AC0-30C1-11CF-A7EB-0000C05BAE0B" rename("FindText", "BrowserFindText")	// Web browser control (IWebBrowser2 and so on)
#import "libid:3050F1C5-98B5-11CF-BB82-00AA00BDCE0B" raw_interfaces_only rename_namespace("MSHTMLLite") rename("TranslateAccelerator", "MshtmlTranslateAccelerator")	// MSHTML Object Model (lite version, without smart wrappers)

#import "libid:F5078F18-C551-11D3-89B9-0000F81FE221"	// Microsoft XML, v4.0
typedef MSXML2::IXMLDOMNodePtr	XmlNode;
typedef MSXML2::IXMLDOMElementPtr	XmlElement;
typedef MSXML2::IXMLDOMDocumentPtr	XmlDocument;
typedef MSXML2::IXMLDOMNodeListPtr	XmlNodeList;

#pragma warning(default: 4192)

#include <MsHtmdid.h>	// WebBrowser declarations
//_COM_SMARTPTR_TYPEDEF(IWebBrowser2, __uuidof(IWebBrowser2));

// Custom COM Error Handling Helpers
#include "StdAfxComHelper.h"

using namespace ATL;
#pragma warning(disable: 4995)	// "name was marked as #pragma deprecated" — reenable for user code
#pragma warning(disable: 4290)	// warning C4290: C++ exception specification ignored except to indicate a function is not __declspec(nothrow)
