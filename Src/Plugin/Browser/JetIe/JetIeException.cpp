// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetIeException.cpp : Implementation of CJetIeException
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "StdAfx.h"
#include "JetIeException.h"

#include "JetIe.h"

// CJetIeException

void CJetIeException::Throw(CStringW sMessage)
{
	CStringW	sDisplayMessage(sMessage);
#ifdef _TRACE
	sDisplayMessage.Replace(L'\n', L' ');
	sDisplayMessage.Replace(L'\r', L' ');
	TRACE(L"Exception: " + sDisplayMessage + L'\n');
#endif	// _TRACE
	IJetIeExceptionPtr	oException(__uuidof(CJetIeException));
	_com_issue_errorex(oException->Raise((BSTR)(LPCWSTR)sDisplayMessage), (IJetIeException*)(oException), __uuidof(IJetIeException));
}

void CJetIeException::ThrowSystemError(DWORD dwError /*= GetLastError()*/, LPCWSTR szComment /*= NULL*/)
{
	// Throw the formatted error
	if(szComment == NULL)
		Throw(CJetIe::GetSystemError(dwError));
	else
		Throw((CStringW)szComment + L"\n" + CJetIe::GetSystemError(dwError));
}

void CJetIeException::ThrowComError(HRESULT hRes) throw(_com_error)
{
#ifdef _TRACE
	CStringW	sTrace;
	sTrace.Format(L"[JETIE] Exception: HRESULT=%#010X.\n", hRes);
	TRACE(sTrace);
#endif
	IJetIeExceptionPtr	oException(__uuidof(CJetIeException));
	_com_issue_errorex(hRes, (IJetIeException*)(oException), __uuidof(IJetIeException));
}

STDMETHODIMP CJetIeException::Raise(BSTR ErrorMessage)
{
	return Error(ErrorMessage, __uuidof(IJetIeException));
}
