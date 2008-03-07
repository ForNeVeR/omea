/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// JetIeException.h : Declaration of the CJetIeException
// An object that provides for throwing COM exceptions from the classes that are not COM-aware.
// As a detailed COM exception information needs an IErrorInfo-capable object, this is the solution.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once
#include "CommonResource.h"       // main symbols

// IJetIeException
#ifdef JETIE_OMEA
[
	object,
	uuid("47F0977C-B644-4DE5-AD4A-8490CD2DE4A9"),
	helpstring("Internet Explorer Omea Add-on Exception Interface"),
	pointer_default(unique)
]
#endif
#ifdef JETIE_BEELAXY
[
	object,
	uuid("47F0977D-B644-4DE5-AD4A-8490CD2DE4A9"),
	helpstring("Internet Explorer Beelaxy Add-on Exception Interface"),
	pointer_default(unique)
]
#endif
__interface IJetIeException : IUnknown
{
	[id(1), helpstring("Raises the error specified.")]
	HRESULT Raise([in] BSTR ErrorMessage);
};

_COM_SMARTPTR_TYPEDEF(IJetIeException, __uuidof(IJetIeException));

// CJetIeException

#ifdef JETIE_OMEA
[
	coclass,
	threading("apartment"),
	support_error_info("IJetIeException"),
	aggregatable("never"),
	vi_progid("IexploreOmea.JetIeException"),
	progid("IexploreOmea.JetIeException.1"),
	version(1.0),
	uuid("9EDF071B-DCA8-453F-B906-3021289AA35D"),
	helpstring("Internet Explorer Omea Add-on Exception")
]
#endif
#ifdef JETIE_BEELAXY
[
	coclass,
	threading("apartment"),
	support_error_info("IJetIeException"),
	aggregatable("never"),
	vi_progid("IexploreBeelaxy.JetIeException"),
	progid("IexploreBeelaxy.JetIeException.1"),
	version(1.0),
	uuid("9EDF071B-DCA8-453F-B906-3021289AA35D"),
	helpstring("Internet Explorer Beelaxy Add-on Exception")
]
#endif
class ATL_NO_VTABLE CJetIeException : 
	public IJetIeException
{
public:
	CJetIeException()
	{
	}


	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}
	
	void FinalRelease() 
	{
	}

// Operations
public:
	/// Throws an exception.
	static void Throw(CStringW sMessage) throw(_com_error);

	/// Throws an exception derived from the system error, along with an optional comment.
	static void ThrowSystemError(DWORD dwError = GetLastError(), LPCWSTR szComment = NULL) throw(_com_error);

	/// Throws a COM exception based on an HRESULT.
	static void ThrowComError(HRESULT hRes) throw(_com_error);
// Interface
public:
	STDMETHOD(Raise)(BSTR ErrorMessage);
};

