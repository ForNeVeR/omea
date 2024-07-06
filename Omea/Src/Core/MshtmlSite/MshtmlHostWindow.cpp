// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetBrains Omea Mshtml Browser Component
//
// Implements the Web browser component wrapping with full-scale customization, including view options and security settings & zones.
// Consists of an unmanaged part (C++ ATL, raw hosting, a composite ActiveX control) and a managed part (JScript.NET, Windows Forms control around the unmanaged ActiveX control plus AbstractWebBrowser proxy-inheritor).
// The unmanaged parts server as a wrapper for the custom interfaces only, and should not carry out any meaningful processing. All the events should be delegated to the managed part for processing.
//
// This file belongs to the unmanaged part and defines the CMshtmlHostWindow class.
// This class implements the container part of the composite ActiveX control, that is, enables hosting of the WebBrowser control in it. Also, it implements the additional interfaces that are queried by MSHTML on its Client Site (which this class implements as well). A Service Provider implemented by this class also exposes the security management interfaces that are chained as a part of the URL Moniker infrastructure. The base code for this class has been mostly derived from the ATL implementation of some ax host window.
//
// © JetBrains Inc, 2004
// Written by (H) Serge Baltic
//
#include "stdafx.h"
#include "MshtmlHostWindow.h"

#include "MshtmlBrowser.h"

CMshtmlHostWindow::CMshtmlHostWindow()
{
	m_bInPlaceActive = FALSE;
	m_bUIActive = FALSE;
	m_bMDIApp = FALSE;
	m_bWindowless = FALSE;
	m_bCapture = FALSE;
	m_bHaveFocus = FALSE;

	// Initialize ambient properties
	m_bCanWindowlessActivate = TRUE;
	m_bUserMode = TRUE;
	m_bDisplayAsDefault = FALSE;
	m_clrBackground = NULL;
	m_clrForeground = GetSysColor(COLOR_WINDOWTEXT);
	m_lcidLocaleID = LOCALE_USER_DEFAULT;
	m_bMessageReflect = true;

	m_bReleaseAll = FALSE;

	m_bSubclassed = FALSE;

	m_dwAdviseSink = 0xCDCDCDCD;
	m_dwDocHostFlags = DOCHOSTUIFLAG_NO3DBORDER;
	m_dwDocHostDoubleClickFlags = DOCHOSTUIDBLCLK_DEFAULT;
	m_bAllowContextMenu = true;
	m_bAllowShowUI = false;
	m_hDCScreen = NULL;
	m_bDCReleased = true;

	m_hAccel = NULL;
}

CMshtmlHostWindow::~CMshtmlHostWindow()
{
}

/////////////////////////////////////////////////////////////////////////////
// IDocHostUIHandler

// MSHTML requests to display its context menu
STDMETHODIMP CMshtmlHostWindow::ShowContextMenu(DWORD dwID, POINT* pptPosition, IUnknown* pCommandTarget, IDispatch* pDispatchObjectHit)
{
	if(m_pControl == NULL)
		return S_FALSE;	// Allow IE to operate
	return m_pControl->OnContextMenu( dwID, pptPosition, pCommandTarget, pDispatchObjectHit ) ? S_OK : S_FALSE;	// Either suppress or show
}
// Called at initialisation to find UI styles from container
STDMETHODIMP CMshtmlHostWindow::GetHostInfo(DOCHOSTUIINFO* pInfo)
{
	TRACE(_T("[OMEA.MSHTML] GetHostInfo call has occured\n"));
	if (pInfo == NULL)
		return E_POINTER;

	pInfo->cbSize = sizeof(DOCHOSTUIINFO);
	pInfo->pchHostCss = NULL;
	pInfo->pchHostNS = NULL;
	pInfo->dwDoubleClick = DOCHOSTUIDBLCLK_DEFAULT;	// Perform the default action.

	// Host UI Flags: try requesting from the host, if fails, use defaults
	if((m_pControl == NULL) || (!m_pControl->OnGetHostInfo(&pInfo->dwFlags)))
	{	// Getting has failed, using defaults
		pInfo->dwFlags = 0
			// | DOCHOSTUIFLAG_DIALOG	// MSHTML does not enable selection of the text in the form.
			| DOCHOSTUIFLAG_FLAT_SCROLLBAR	// MSHTML uses flat scroll bars for any user interface (UI) it displays.
			| DOCHOSTUIFLAG_ENABLE_FORMS_AUTOCOMPLETE	// Internet Explorer 5 or later. This flag enables the AutoComplete feature for forms in the hosted browser. The Intelliforms feature is only turned on if the user has previously enabled it. If the user has turned the AutoComplete feature off for forms, it is off whether this flag is specified or not.
			| DOCHOSTUIFLAG_ENABLE_INPLACE_NAVIGATION	// Internet Explorer 5 or later. This flag enables the host to specify that navigation should happen in place. This means that applications hosting MSHTML directly can specify that navigation happen in the application's window. For instance, if this flag is set, you can click a link in HTML mail and navigate in the mail instead of opening a new Internet Explorer window.
			| DOCHOSTUIFLAG_IME_ENABLE_RECONVERSION	// Internet Explorer 5 or later. During initialization, the host can set this flag to enable Input Method Editor (IME) reconversion, allowing computer users to employ IME reconversion while browsing Web pages. An input method editor is a program that allows users to enter complex characters and symbols, such as Japanese Kanji characters, using a standard keyboard. For more information, see the International Features reference in the Base Services section of the Microsoft Platform Software Development Kit (SDK).
			| DOCHOSTUIFLAG_THEME	// Internet Explorer 6 or later. Specifies that the hosted browser should use themes for pages it displays.
			| DOCHOSTUIFLAG_NO3DBORDER	// MSHTML does not use 3-D borders on any frames or framesets. To turn the border off on only the outer frameset use DOCHOSTUIFLAG_NO3DOUTERBORDER
			;
	}

	TRACE(_T("[OMEA.MSHTML] Returning host UI info: %#010X\n"), pInfo->dwFlags);

	return S_OK;
}
// Allows the host to replace the IE4/MSHTML menus and toolbars.
STDMETHODIMP CMshtmlHostWindow::ShowUI(DWORD dwID, IOleInPlaceActiveObject* pActiveObject, IOleCommandTarget* pCommandTarget, IOleInPlaceFrame* pFrame, IOleInPlaceUIWindow* pDoc)
{
	HRESULT hr = m_bAllowShowUI ? S_FALSE : S_OK;
	if (m_spIDocHostUIHandlerDispatch != NULL)
		m_spIDocHostUIHandlerDispatch->ShowUI(
		dwID,
		pActiveObject,
		pCommandTarget,
		pFrame,
		pDoc,
		&hr);
	return hr;
}
// Called when IE4/MSHTML removes its menus and toolbars.
STDMETHODIMP CMshtmlHostWindow::HideUI()
{
	HRESULT hr = S_OK;
	if (m_spIDocHostUIHandlerDispatch != NULL)
		hr = m_spIDocHostUIHandlerDispatch->HideUI();
	return hr;
}
// Notifies the host that the command state has changed.
STDMETHODIMP CMshtmlHostWindow::UpdateUI()
{
	HRESULT hr = S_OK;
	if (m_spIDocHostUIHandlerDispatch != NULL)
		hr = m_spIDocHostUIHandlerDispatch->UpdateUI();
	return hr;
}
// Called from the IE4/MSHTML implementation of IOleInPlaceActiveObject::EnableModeless
STDMETHODIMP CMshtmlHostWindow::EnableModeless(BOOL fEnable)
{
	HRESULT hr = S_OK;
	if (m_spIDocHostUIHandlerDispatch != NULL)
		hr = m_spIDocHostUIHandlerDispatch->EnableModeless(fEnable ? ATL_VARIANT_TRUE : ATL_VARIANT_FALSE);
	return hr;
}
// Called from the IE4/MSHTML implementation of IOleInPlaceActiveObject::OnDocWindowActivate
STDMETHODIMP CMshtmlHostWindow::OnDocWindowActivate(BOOL fActivate)
{
	HRESULT hr = S_OK;
	if (m_spIDocHostUIHandlerDispatch != NULL)
		hr = m_spIDocHostUIHandlerDispatch->OnDocWindowActivate(fActivate ? ATL_VARIANT_TRUE : ATL_VARIANT_FALSE);
	return hr;
}
// Called from the IE4/MSHTML implementation of IOleInPlaceActiveObject::OnFrameWindowActivate.
STDMETHODIMP CMshtmlHostWindow::OnFrameWindowActivate(BOOL fActivate)
{
	HRESULT hr = S_OK;
	if (m_spIDocHostUIHandlerDispatch != NULL)
		hr = m_spIDocHostUIHandlerDispatch->OnFrameWindowActivate(fActivate ? ATL_VARIANT_TRUE : ATL_VARIANT_FALSE);
	return hr;
}
// Called from the IE4/MSHTML implementation of IOleInPlaceActiveObject::ResizeBorder.
STDMETHODIMP CMshtmlHostWindow::ResizeBorder(LPCRECT prcBorder, IOleInPlaceUIWindow* pUIWindow, BOOL fFrameWindow)
{
	HRESULT hr = S_OK;
	if (m_spIDocHostUIHandlerDispatch != NULL)
		hr = m_spIDocHostUIHandlerDispatch->ResizeBorder(
		prcBorder->left,
		prcBorder->top,
		prcBorder->right,
		prcBorder->bottom,
		pUIWindow,
		fFrameWindow ? ATL_VARIANT_TRUE : ATL_VARIANT_FALSE);
	return hr;
}
// Called by IE4/MSHTML when IOleInPlaceActiveObject::TranslateAccelerator or IOleControlSite::TranslateAccelerator is called.
STDMETHODIMP CMshtmlHostWindow::TranslateAccelerator(LPMSG lpMsg, const GUID* pguidCmdGroup, DWORD nCmdID)
{
	HRESULT hr = S_FALSE;
	if (m_spIDocHostUIHandlerDispatch != NULL)
		m_spIDocHostUIHandlerDispatch->TranslateAccelerator(
		(DWORD_PTR) lpMsg->hwnd,
		lpMsg->message,
		lpMsg->wParam,
		lpMsg->lParam,
		CComBSTR(*pguidCmdGroup),
		nCmdID,
		&hr);
	return hr;
}
// Returns the registry key under which IE4/MSHTML stores user preferences.
// Returns S_OK if successful, or S_FALSE otherwise. If S_FALSE, IE4/MSHTML will default to its own user options.

STDMETHODIMP CMshtmlHostWindow::GetOptionKeyPath(LPOLESTR* pchKey, DWORD dwReserved)
{
	HRESULT hr = S_FALSE;
	if (pchKey == NULL)
		return E_POINTER;
	*pchKey = NULL;
	if (m_spIDocHostUIHandlerDispatch != NULL)
	{
		hr = m_spIDocHostUIHandlerDispatch->GetOptionKeyPath(pchKey, dwReserved);
		if (FAILED(hr) || *pchKey == NULL)
			hr = S_FALSE;
	}
	else
	{
		if (m_bstrOptionKeyPath.m_str != NULL)
		{
			UINT nByteLength = m_bstrOptionKeyPath.ByteLength();
			LPOLESTR pStr = (LPOLESTR)CoTaskMemAlloc(nByteLength + sizeof(OLECHAR));
			if (pStr == NULL)
				return E_OUTOFMEMORY;
			ocscpy(pStr, m_bstrOptionKeyPath.m_str);
			*pchKey = pStr;
			hr = S_OK;
		}
	}
	return hr;
}

// Called by IE4/MSHTML when it is being used as a drop target to allow the host to supply an alternative IDropTarget
STDMETHODIMP CMshtmlHostWindow::GetDropTarget(IDropTarget* pDropTarget, IDropTarget** ppDropTarget)
{
	ATLASSERT(ppDropTarget != NULL);
	if (ppDropTarget == NULL)
		return E_POINTER;
	*ppDropTarget = NULL;

	HRESULT hr = E_NOTIMPL;
	if (m_spIDocHostUIHandlerDispatch != NULL)
	{
		CComPtr<IUnknown> spUnk;
		hr = m_spIDocHostUIHandlerDispatch->GetDropTarget(pDropTarget, &spUnk);
		if (spUnk)
			hr = spUnk->QueryInterface(__uuidof(IDropTarget), (void**)ppDropTarget);
		if (FAILED(hr) || *ppDropTarget == NULL)
			hr = S_FALSE;
	}
	return hr;
}
// Called by IE4/MSHTML to obtain the host's IDispatch interface
STDMETHODIMP CMshtmlHostWindow::GetExternal(IDispatch** ppDispatch)
{
	ATLASSERT(ppDispatch != NULL);
	if (ppDispatch == NULL)
		return E_POINTER;
	*ppDispatch = NULL;

	if(m_pControl == NULL)
		return E_NOINTERFACE;

	IDispatchPtr	dispRet = m_pControl->OnGetExternal();	// Query the wrapper object for whatever external object it provides
	TRACE(_T("[OMEA.MSHTML] The ExternalObject LPDISPATCH representation is %#010X.\n"), (DWORD_PTR)(IDispatch*)dispRet);
	return dispRet != NULL ? dispRet->QueryInterface( __uuidof(IDispatch), (void**)ppDispatch ) : S_FALSE;	// If not available, return S_FALSE/NULL
}
// Called by IE4/MSHTML to allow the host an opportunity to modify the URL to be loaded
STDMETHODIMP CMshtmlHostWindow::TranslateUrl(DWORD dwTranslate, OLECHAR* pchURLIn, OLECHAR** ppchURLOut)
{
	TRACE(_T("[OMEA.MSHTML] Translating URL %s"), pchURLIn);
	ATLASSERT(ppchURLOut != NULL);
	if (ppchURLOut == NULL)
		return E_POINTER;
	*ppchURLOut = NULL;

	HRESULT hr = S_FALSE;
	if (m_spIDocHostUIHandlerDispatch != NULL)
	{
		CComBSTR bstrURLOut;
		hr = m_spIDocHostUIHandlerDispatch->TranslateUrl(dwTranslate, CComBSTR(pchURLIn), &bstrURLOut);
		if (SUCCEEDED(hr) && bstrURLOut.m_str != NULL)
		{
			UINT nLen = (bstrURLOut.Length() + 1) * 2;
			*ppchURLOut = (OLECHAR*) CoTaskMemAlloc(nLen);
			if (*ppchURLOut == NULL)
				return E_OUTOFMEMORY;
			memcpy(*ppchURLOut, bstrURLOut.m_str, nLen);
		}
		else
			hr = S_FALSE;
	}
	return hr;
}
// Called on the host by IE4/MSHTML to allow the host to replace IE4/MSHTML's data object.
// This allows the host to block certain clipboard formats or support additional clipboard formats.
STDMETHODIMP CMshtmlHostWindow::FilterDataObject(IDataObject* pDO, IDataObject** ppDORet)
{
	ATLASSERT(ppDORet != NULL);
	if (ppDORet == NULL)
		return E_POINTER;
	*ppDORet = NULL;

	HRESULT hr = S_FALSE;
	if (m_spIDocHostUIHandlerDispatch != NULL)
	{
		CComPtr<IUnknown> spUnk;
		hr = m_spIDocHostUIHandlerDispatch->FilterDataObject(pDO, &spUnk);
		if (spUnk)
			hr = QueryInterface(__uuidof(IDataObject), (void**)ppDORet);
		if (FAILED(hr) || *ppDORet == NULL)
			hr = S_FALSE;
	}
	return hr;
}

//^ IDocHostUIHandler
/////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////
// IDocHostUIHandler2

STDMETHODIMP CMshtmlHostWindow::GetOverrideKeyPath(LPOLESTR * /*pchKey*/, DWORD /*dw*/)
{
	return E_NOTIMPL; // TODO: Implement;
}

//^ IDocHostUIHandler2
/////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////
// IDocHostShowUI

STDMETHODIMP CMshtmlHostWindow::ShowMessage(HWND hwnd, LPOLESTR lpstrText, LPOLESTR lpstrCaption, DWORD dwType, LPOLESTR /*lpstrHelpFile*/, DWORD /*dwHelpContext*/, LRESULT *plResult)
{
	//return S_OK; // Host displayed its user interface (UI). MSHTML does not display its message box.
	//return E_NOTIMPL;
	*plResult = ::MessageBox(hwnd, (_bstr_t)lpstrText, _bstr_t(L"Omea"), dwType);	// TODO: Throw the Error Event
	TRACE(_T("IE MessageBox entitled “%s” says “%s” with buttons %#010X.\r\n"), (LPCTSTR)(_bstr_t)lpstrCaption, (LPCTSTR)(_bstr_t)lpstrText, dwType);
	return S_OK;
}

STDMETHODIMP CMshtmlHostWindow::ShowHelp(HWND /*hwnd*/, LPOLESTR /*pszHelpFile*/, UINT /*uCommand*/, DWORD /*dwData*/, POINT /*ptMouse*/, IDispatch * /*pDispatchObjectHit*/)
{
	return E_NOTIMPL; // TODO: Implement;
}

//^ IDocHostShowUI
/////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////
// IInternetSecurityManager

STDMETHODIMP CMshtmlHostWindow::SetSecuritySite(IInternetSecurityMgrSite * /*pSite*/)
{
	/*TRACE("return INET_E_DEFAULT_ACTION\r\n");*/ return INET_E_DEFAULT_ACTION;
}

STDMETHODIMP CMshtmlHostWindow::GetSecuritySite(IInternetSecurityMgrSite ** /*ppSite*/)
{
	TRACE(_T("CMshtmlHostWindow::GetSecuritySite\n"));
	/*TRACE("return INET_E_DEFAULT_ACTION\r\n");*/ return INET_E_DEFAULT_ACTION;
}

STDMETHODIMP CMshtmlHostWindow::MapUrlToZone(LPCWSTR /*pwszUrl*/, DWORD * /*pdwZone*/, DWORD /*dwFlags*/)
{
	TRACE(_T("[OMEA.MSHTML.SECACCESS]\n"));
	// Determine which Internet Security Zone to display the content in
	//if(_bstr_t(pwszUrl) == _bstr_t(L"about:blank"))
	//TRACE(_T("SecurityManager::MapUrlToZone, %s\n"), pwszUrl);
	/*TRACE("return INET_E_DEFAULT_ACTION\r\n");*/ return INET_E_DEFAULT_ACTION;
	//CoInternetCreateSecuritymanager(nil, SecManager, 0);
//CoInternetCreateZoneManager(nil, ZoneManager, 0);

	// TODO: check how the Zone is passed in here and furtherly processed
	/*
	ASSERT(m_pControl != NULL);
	if(m_pControl == NULL)
		return INET_E_DEFAULT_ACTION;	// Relay to default manager

	if(m_pControl->m_nPermissionSet == CMshtmlBrowser::PS_Zone)
	{
		*pdwZone = m_pControl->m_nSecurityZone;	// Assign to the zone value
		return S_OK;	// Overridden
	}

	return INET_E_DEFAULT_ACTION;	// Relay to default manager
	*/
}

STDMETHODIMP CMshtmlHostWindow::GetSecurityId(LPCWSTR /*pwszUrl*/, BYTE * /*pbSecurityId*/, DWORD * /*pcbSecurityId*/, DWORD_PTR /*dwReserved*/)
{
	return INET_E_DEFAULT_ACTION;	// Relay to default manager
}

STDMETHODIMP CMshtmlHostWindow::ProcessUrlAction(LPCWSTR pwszUrl, DWORD dwAction, BYTE *pPolicy, DWORD /*cbPolicy*/, BYTE * /*pContext*/, DWORD /*cbContext*/, DWORD dwFlags, DWORD /*dwReserved*/)
{
	ASSERT(m_pControl != NULL);
	if(m_pControl != NULL)
	{
		if(m_pControl->OnUrlAction(pwszUrl, dwAction, dwFlags, pPolicy))	// Succeeded?
			return S_OK;
	}

	// No special security override, return the default
	return INET_E_DEFAULT_ACTION;	// Relay to the default security manager

	/*
	TRACE(_T("[OMEA.MSHTML.SECACCESS]\n"));
	TRACE(_T("[OMEA.MSHTML] Processing the URL action for %s"), pwszUrl);

	// Check the security settings
	ASSERT(m_pControl != NULL);
	if(m_pControl == NULL)
		return INET_E_DEFAULT_ACTION;	// Relay to the default security manager

	// Disallow all?
	if(m_pControl->m_nPermissionSet == CMshtmlBrowser::PS_Nothing)
	{
		if((dwAction >= URLACTION_ACTIVEX_MIN) && (dwAction <= URLACTION_ACTIVEX_MAX))
			*pPolicy = URLPOLICY_DISALLOW;
		else if(dwAction == URLACTION_CROSS_DOMAIN_DATA)
			*pPolicy = URLPOLICY_DISALLOW;
		else if((dwAction >= URLACTION_DOWNLOAD_MIN) && (dwAction <= URLACTION_DOWNLOAD_MAX))
			*pPolicy = URLPOLICY_DISALLOW;
		else if((dwAction >= URLACTION_HTML_MIN) && (dwAction <= URLACTION_HTML_MAX))
			*pPolicy = URLPOLICY_DISALLOW;
		else if((dwAction >= URLACTION_SCRIPT_MIN) && (dwAction <= URLACTION_SCRIPT_MAX))
			*pPolicy = URLPOLICY_DISALLOW;
		else
			return INET_E_DEFAULT_ACTION;
	}
	// Allow all?
	else if(m_pControl->m_nPermissionSet == CMshtmlBrowser::PS_Everything)
	{
		if((dwAction >= URLACTION_ACTIVEX_MIN) && (dwAction <= URLACTION_ACTIVEX_MAX))
			*pPolicy = URLPOLICY_ALLOW;
		else if(dwAction == URLACTION_CROSS_DOMAIN_DATA)
			*pPolicy = URLPOLICY_ALLOW;
		else if((dwAction >= URLACTION_DOWNLOAD_MIN) && (dwAction <= URLACTION_DOWNLOAD_MAX))
			*pPolicy = URLPOLICY_ALLOW;
		else if((dwAction >= URLACTION_HTML_MIN) && (dwAction <= URLACTION_HTML_MAX))
			*pPolicy = URLPOLICY_ALLOW;
		else if((dwAction >= URLACTION_SCRIPT_MIN) && (dwAction <= URLACTION_SCRIPT_MAX))
			*pPolicy = URLPOLICY_ALLOW;
		else
			return INET_E_DEFAULT_ACTION;
	}

	TRACE(_T("return INET_E_DEFAULT_ACTION: CMshtmlHostWindow::ProcessUrlAction\r\n"));

	// No special security override, return the default
	return INET_E_DEFAULT_ACTION;	// Relay to the default security manager
	*/
}

STDMETHODIMP CMshtmlHostWindow::QueryCustomPolicy(LPCWSTR pwszUrl, REFGUID /*guidKey*/, BYTE ** /*ppPolicy*/, DWORD * /*pcbPolicy*/, BYTE * /*pContext*/, DWORD /*cbContext*/, DWORD /*dwReserved*/)
{
	TRACE(_T("SecurityManager::QueryCustomPolicy, %s\n"), pwszUrl);
	/*TRACE("return INET_E_DEFAULT_ACTION\r\n");*/ return INET_E_DEFAULT_ACTION;
}

STDMETHODIMP CMshtmlHostWindow::SetZoneMapping(DWORD /*dwZone*/, LPCWSTR lpszPattern, DWORD /*dwFlags*/)
{
	TRACE(_T("SecurityManager::SetZoneMapping, %s\n"), lpszPattern);
	/*TRACE("return INET_E_DEFAULT_ACTION\r\n");*/ return INET_E_DEFAULT_ACTION;
}

STDMETHODIMP CMshtmlHostWindow::GetZoneMappings(DWORD /*dwZone*/, IEnumString ** /*ppenumString*/, DWORD /*dwFlags*/)
{
	TRACE(_T("SecurityManager::GetZoneMappings\n"));
	/*TRACE("return INET_E_DEFAULT_ACTION\r\n");*/ return INET_E_DEFAULT_ACTION;
}

//^ IInternetSecurityManager
//\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\//
