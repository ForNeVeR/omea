// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

///////////////////////////////
// COM Error Handling Helpers
// This file is commonly included into the StdAfx.h precompiled header and provides the custom COM helpers.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

/// Checks an HRESULT for a COM error and, if positive, throws an extended COM exception that is capable of providing the additional error information thru its IErrorInfo, if the failing object has such capabilities.
inline HRESULT ComCheckError(IUnknown *pInterface, const IID &iid, HRESULT hr)
{
    if(FAILED(hr))
        _com_issue_errorex(hr, pInterface, iid);
	return hr;
}

/// Checks if the returned HRESULT represents an error, and throws an exception if so, along with the clarifying IErrorInfo-implementing object.
/// The first parameter is an object (an interface pointer or a smart pointer), and the second is the function call (after the "->", without the object itself).
#define COM_CHECK(OBJ, FUNC)	ComCheckError((OBJ), __uuidof(OBJ), (OBJ)->FUNC)

/// Checks if the returned HRESULT represents an error, and throws an exception if so, along with the clarifying IErrorInfo-implementing object.
/// The first parameter is an object (an interface pointer or a smart pointer), and the second is the valid expression returning an HRESULT.
#define COM_CHECK2(o, hr)	ComCheckError((o), __uuidof(o), (hr))

/// Checks if the HRESULT is an error HRESULT and throws a simple COM error (without the IErrorInfo support) if so.
#define CHECK(x) _com_util::CheckError(x)

/// Checks the boolean return value. FALSE indicates a failure, in this case the last system error is converted into an HRESULT and thrown as a simple COM error.
#define CHECK_BOOL(x)	if(!(x)) CHECK(AtlHresultFromLastError())

/// Outputs the com error represented by e variable to the standard debug.
#define COM_TRACE()		TRACE(_T("[MSHTML] COM Error %#010X in %s: %s (%s:%s:%d)\r\n"), (DWORD)e.Error(), (LPCTSTR)e.Source(), (LPCTSTR)e.Description(), __FILE__, __FUNCTION__, __LINE__)

/// Checks if the passed in variant represents a missing value. Use in place of a boolean expression.
#define V_IS_MISSING(pvt)	((V_VT(pvt) == VT_ERROR) && (V_ERROR(pvt) == DISP_E_PARAMNOTFOUND))

/// Injects a catch block for the COM error, traces it to the debug output, and returns as a HRESULT.
/// If there's IErrorInfo attached to the error, returns the HRESULT corresponding to the current COM object's IErrorInfo so that the same description would be provided. Otherwise, returns just an HRESULT.
#define COM_CATCH_RETURN()\
	catch(_com_error e)\
	{\
		COM_TRACE();\
		return (LPCTSTR)e.Description() != NULL ? Error((LPCTSTR)e.Description()) : E_FAIL;\
	}

/// Injects a catch block for the COM error, traces it to the debug output, and always returns just a plain HRESULT.
/// This is suitable for the objects that are not COM objects or have no own IErrorInfo, that is, cannot return extended errors.
#define COM_CATCH_RETURN_RAW()\
	catch(_com_error e)\
	{\
		COM_TRACE();\
		return e.Error();\
	}

/// Injects a catch block for the COM error and traces it to the debug output.
#define COM_CATCH()\
	catch(_com_error e)\
	{\
		COM_TRACE();\
	}

/// Injects a catch block for the COM error, traces it to the debug output, and assigns the caught HRESULT to the local variable named hResult.
#define COM_CATCH_ASSIGN_hResult()\
	catch(_com_error e)\
	{\
		COM_TRACE();\
		hResult = (LPCTSTR)e.Description() != NULL ? Error((LPCTSTR)e.Description()) : E_FAIL;\
	}

/// Injects a catch block for the COM error that does nothing, just silently traps a potential exception.
#define COM_CATCH_SILENT()\
	catch(_com_error /*e*/)\
	{\
	}

/// Extracts the error message from a COM exception by employing the IErrorInfo, if available, and extracting the error string from it, or, if missing, converting the standard HRESULT to an error message.
/// Note that just calling the Description member would give us NULL if there's no IErrorInfo, and, vice versa, ErrorMessage would not tell us about the IErrorInfo-encapsulated error message.
#define COM_REASON(e)	((e).Description().length() != 0 ? (LPCTSTR)(e).Description() : (e).ErrorMessage())
