// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetBrains Omea Mshtml Browser Component
//
// Implements the Web browser component wrapping with full-scale customization, including view options and security settings & zones.
// Consists of an unmanaged part (C++ ATL, raw hosting, a composite ActiveX control) and a managed part (JScript.NET, Windows Forms control around the unmanaged ActiveX control plus AbstractWebBrowser proxy-inheritor).
// The unmanaged parts server as a wrapper for the custom interfaces only, and should not carry out any meaningful processing. All the events should be delegated to the managed part for processing.
//
// This file belongs to the unmanaged part and serves as a precompiled header which is an include file for standard system include files, or project specific include files that are used frequently, but are changed infrequently. Also contains some project-specific declarations.
//
// © JetBrains Inc, 2004
// Written by (H) Serge Baltic
//
#pragma once

#ifndef STRICT
#define STRICT
#endif

// Modify the following defines if you have to target a platform prior to the ones specified below.
// Refer to MSDN for the latest info on corresponding values for different platforms.
#ifndef WINVER				// Allow use of features specific to Windows 95 and Windows NT 4 or later.
#define WINVER 0x0510		// Change this to the appropriate value to target Windows 98 and Windows 2000 or later.
#endif

#ifndef _WIN32_WINNT		// Allow use of features specific to Windows NT 4 or later.
#define _WIN32_WINNT 0x0510	// Change this to the appropriate value to target Windows 2000 or later.
#endif

#ifndef _WIN32_WINDOWS		// Allow use of features specific to Windows 98 or later.
#define _WIN32_WINDOWS 0x0510 // Change this to the appropriate value to target Windows Me or later.
#endif

#ifndef _WIN32_IE			// Allow use of features specific to IE 4.0 or later.
#define _WIN32_IE 0x0600	// Change this to the appropriate value to target IE 5.0 or later.
#endif

#define _ATL_APARTMENT_THREADED
#define _ATL_NO_AUTOMATIC_NAMESPACE

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit

// turns off ATL's hiding of some common and often safely ignored warning messages
#define _ATL_ALL_WARNINGS

#pragma warning(disable: 4995)

#include <atlbase.h>
#include <atlcom.h>
#include <atlwin.h>
#include <atltypes.h>
#include <atlctl.h>
#include <atlhost.h>
#include <atltrace.h>
//DECLARE_TRACE_CATEGORY( TRACE_VERBOSE )

#include <comdef.h>	// Basic COM declarations
#include <comutil.h>	// Compiler COM Support Utilities (_bstr_t, etc)
#include <docobj.h>	// OLE Document related stuff
#include <exdispid.h>	// DISPIDs for the MSHTML/WebBrowser events
#include <strsafe.h>	// Safe string operations
#include <math.h>	// Maths
#include <string>	// std::basic_string and so on
#include <atlsimpstr.h>	// String services
#include <atlstr.h>	// String services

// Custom debug points to the stock implementation by default
#if(defined(_DEBUG))
	#define TRACE ATLTRACE
#else	//(defined(_DEBUG))
	#if(defined(_TRACE))
		void DirectTrace(LPCTSTR pstrFormat, ...);
		#define TRACE DirectTrace
	#else	// (defined(_TRACE))
		#define TRACE	__noop
	#endif	// (defined(_TRACE))
#endif	//(defined(_DEBUG))

#define ASSERT	ATLASSERT

// Import the needed underlaying interfaces
//#pragma warning(disable: 4192)
//#import "libid:EAB22AC0-30C1-11CF-A7EB-0000C05BAE0B"	// Web browser control (IWebBrowser2 and so on)
//#import "libid:3050F1C5-98B5-11CF-BB82-00AA00BDCE0B" raw_interfaces_only rename_namespace("MSHTMLLite")	// MSHTML Object Model (lite version, without smart wrappers)
//#pragma warning(default: 4192)

#include "SHDocVw.h"
#include <MsHtmdid.h>	// WebBrowser declarations
//_COM_SMARTPTR_TYPEDEF(IWebBrowser2, __uuidof(IWebBrowser2));

////////////////////////
// Custom COM Error Handling Helpers
#include "StdAfxComHelper.h"

using namespace ATL;
