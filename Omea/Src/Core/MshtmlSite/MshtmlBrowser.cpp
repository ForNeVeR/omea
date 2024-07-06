// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetBrains Omea Mshtml Browser Component
//
// Implements the Web browser component wrapping with full-scale customization, including view options and security settings & zones.
// Consists of an unmanaged part (C++ ATL, raw hosting, a composite ActiveX control) and a managed part (JScript.NET, Windows Forms control around the unmanaged ActiveX control plus AbstractWebBrowser proxy-inheritor).
// The unmanaged parts server as a wrapper for the custom interfaces only, and should not carry out any meaningful processing. All the events should be delegated to the managed part for processing.
//
// This file belongs to the unmanaged part and defines the CMshtmlBrowser class.
// This class implements the control part of the composite ActiveX control, that is, enables hosting in the ActiveX containers. Also, it holds all the On… functions that invoke the managed wrapper part. This invocation simulates the OLE Events mechanism with the sole difference that it is capable of using the properties and does not support multicasting (delivering to multiple consumers).
//
// © JetBrains Inc, 2004
// Written by (H) Serge Baltic
//
#include "stdafx.h"
#include "MshtmlBrowser.h"
#include "MshtmlHostWindow.h"
#include ".\mshtmlbrowser.h"

const GUID CMshtmlBrowser::CGID_IWebBrowser = { 0xED016940L,0xBD5B,0x11cf, { 0xBA, 0x4E,0x00,0xC0,0x4F,0xD7,0x08,0x16 } };

// Declare the trace category for verbose CMshtmlBrowser output
//CTraceCategory TRACE_VERBOSE( _T("MshtmlVerboseTrace"), 4 );

// CMshtmlBrowser

CMshtmlBrowser::CMshtmlBrowser()
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	m_bWindowOnly = true;
	m_pSite = NULL;

	// Initialize the stock properties
	m_bEnabled = true;
	m_nBorderStyle = 0;	// No border
	m_bTabStop = true;

	// UserMode init
	m_bUserMode = false;	// In Design Mode by default

	// Security settings
	m_nPermissionSet = PS_Nothing;	// Disallow all by default
	m_nSecurityZone = 0;
}

CMshtmlBrowser::~CMshtmlBrowser()
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
}

_COM_SMARTPTR_TYPEDEF(IAxWinAmbientDispatch, __uuidof(IAxWinAmbientDispatch));
void CMshtmlBrowser::TryInstantiate()
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	if(m_oBrowser != NULL)
		return;

	AtlAxWinInit();
	CComPtr<IUnknown> spUnkContainer;

	// Create the host window for MSHTML (if it does not exist yet)
	if(m_pSite == NULL)
	{
		CComObject<CMshtmlHostWindow>*	theHostWindow;
		CHECK(CComObject<CMshtmlHostWindow>::CreateInstance(&theHostWindow));
		m_pSite = theHostWindow;	// Store it for accessing later
		m_pSite->SetControlPart(this);	// Tell who is the control around the inlying container (for callbacks and settings)
	}
	spUnkContainer = (IUnknown*)(IOleClientSite*)m_pSite;	// Get IUnknown-pointer to the container
	//_com_util::CheckError(CMshtmlHostWindow::_CreatorClass::CreateInstance(NULL, IID_IUnknown, (void**)&spUnkContainer));	// Old creation, as it exists in the internals of ATL
	//theHostWindow->SetAmbientDispatch(this);	// Set the ambient-dispatch to be queried for the ambient container properties through the IDispatch::Invoke
	if(m_oParentCallback != NULL)
		m_pSite->SetAmbientDispatch(m_oParentCallback);

	CComPtr<IAxWinHostWindow> pAxWindow;
	COM_CHECK(spUnkContainer, QueryInterface(IID_IAxWinHostWindow, (void**)&pAxWindow));

	IAxWinAmbientDispatchPtr	oIEHostDisp = (IUnknown*)pAxWindow;

	IUnknownPtr	oIEUnk;
	//COM_CHECK(pAxWindow, CreateControlEx(L"Mozilla.Browser", m_hWnd, NULL, &oIEUnk, IID_NULL, NULL));	// mAzila
	COM_CHECK(pAxWindow, CreateControlEx(L"about:blank", m_hWnd, NULL, &oIEUnk, IID_NULL, NULL));	// MSHTML
	m_oBrowser = oIEUnk;
	//pAxWindow->SetExternalDispatch(static_cast<IMshtmlBrowser*>(this));	// Set by the IDocHostUIHandler
	oIEHostDisp->put_OptionKeyPath(L"Software\\JetBrains\\Omea\\Internet Explorer");

	// Sink the browser's events
	IWebBrowserEventsSinkImpl::DispEventAdvise( m_oBrowser, &DIID_DWebBrowserEvents );
	IWebBrowserEvents2SinkImpl::DispEventAdvise( m_oBrowser, &DIID_DWebBrowserEvents2 );

	// Set the UserMode/DesignMode
	CHECK(GetAmbientUserMode(m_bUserMode));
	IAxWinAmbientDispatchPtr	oAmbientWin = (IUnknown*)spUnkContainer;
	if(oAmbientWin != NULL)
	{
		TRACE(_T("Setting UserMode to %d\n"), m_bUserMode);
		COM_CHECK(oAmbientWin, put_UserMode(m_bUserMode ? VARIANT_TRUE : VARIANT_FALSE));
	}
	else
		TRACE(_T("Cannot set UserMode to %d\n"), m_bUserMode);

	// Signal that the browser has been created and can be used from now on
	OnBrowserCreated();
}

STDMETHODIMP CMshtmlBrowser::get_AmbientDlControl( LONG *pVal )
{
	*pVal = DLCTL_DLIMAGES | DLCTL_VIDEOS | DLCTL_BGSOUNDS;	// No restrictions by default

	// Query the ParentCallback about special settings imposed on the browser, if available
	if(m_oParentCallback != NULL)
	{
		try
		{
			// Declarations for the invocations
			DISPID dispid;
			OLECHAR FAR* szMember;
			DISPPARAMS dispparams = { NULL, NULL, 0, 0 };
			_variant_t	vtRet;

			// ShowImages (also affects the other multimedia stuff)
			try
			{
				szMember = L"ShowImages";
				COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
				COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_PROPERTYGET, &dispparams, &vtRet, NULL, NULL));
				if( (bool)vtRet )
					*pVal &= ~( DLCTL_DLIMAGES | DLCTL_VIDEOS | DLCTL_BGSOUNDS );	// Set these three flags off
			}
			COM_CATCH();

			// Disable scripts and java and some related stuff if we're working in the "Nothing" permission set
			if( m_nPermissionSet == PS_Nothing )
				*pVal |= DLCTL_NO_SCRIPTS | DLCTL_NO_JAVA | DLCTL_NO_DLACTIVEXCTLS | DLCTL_NO_RUNACTIVEXCTLS | DLCTL_NO_FRAMEDOWNLOAD | DLCTL_NO_BEHAVIORS | DLCTL_NO_CLIENTPULL;

			// Silent Mode (no UI pops up during the download)
			try
			{
				szMember = L"SilentMode";
				COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
				COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_PROPERTYGET, &dispparams, &vtRet, NULL, NULL));
				if( (bool)vtRet )
					*pVal |= DLCTL_SILENT;	// Set this flag on
			}
			COM_CATCH();

			// Force Offline Mode ()
			try
			{
				szMember = L"OfflineMode";
				COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
				COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_PROPERTYGET, &dispparams, &vtRet, NULL, NULL));
				if( (bool)vtRet )
					*pVal |= DLCTL_FORCEOFFLINE;	// Set this flag on
			}
			COM_CATCH();
		}
		COM_CATCH();
	}

	TRACE(_T("[OMEA.MSHTML] DLCONTROL"));
	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::Navigate(BSTR URI)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	if(m_oBrowser == NULL)	// Not created yet
		return E_UNEXPECTED;

	try
	{
		TryInstantiate();

		m_oBrowser->Navigate(URI);
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::ShowHtml(IStream* HtmlData)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	TryUIDeactivate();

	try
	{
		TryInstantiate();

		if(m_oBrowser->Document == NULL)	// Not navigated to a page yet
			return E_UNEXPECTED;

		// Feed the browser with data
		COM_CHECK(IPersistStreamInitPtr(m_oBrowser->Document), Load(HtmlData));
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::ShowHtmlText(BSTR HtmlText)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);

	try
	{
		TryInstantiate();

		if(m_oBrowser->Document == NULL)	// Not navigated to a page yet
			return E_UNEXPECTED;

		UINT	nCbStringLen = ocslen(HtmlText) * sizeof(OLECHAR);
		UINT	nCreateSize = nCbStringLen + 2;	// Reserve some place for the BOM (byte-order-mark)
		HGLOBAL hGlobal = GlobalAlloc(GMEM_MOVEABLE, nCreateSize);
		if (hGlobal)
		{
			CComPtr<IStream> spStream;
			BYTE* pBytes = (BYTE*) GlobalLock(hGlobal);
			pBytes[0] = 0xFF;	// Write the BOM, byte 1
			pBytes[1] = 0xFE;	// Write the BOM, byte 2
			memcpy(pBytes + 2, HtmlText, nCbStringLen);
			GlobalUnlock(hGlobal);
			HRESULT hr = CreateStreamOnHGlobal(hGlobal, TRUE, &spStream);
			if(SUCCEEDED(hr))
			{
				// Try the main loading scheme (thru the IPersistInit interface)
				hr = ShowHtml(spStream);
				// If the main scheme has failed, try the backup one
				if(FAILED(hr))
				{
					TRACE(_T("The main content uploading scheme has failed for the browser (%#010X), falling back to simple upload."), hr);
					m_oBrowser->Navigate(L"about:blank");	// Clean up
					while(m_oBrowser->ReadyState < 4)	// Wait until cleanup completes
						Sleep(10);
					IDispatchPtr	oDoc = m_oBrowser->Document;
					DISPID dispid;
					OLECHAR FAR* szMember = L"write";
					COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
					_variant_t avtParams[] = { _variant_t(HtmlText) };
					DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
					COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, NULL, NULL));
				}
			}
			else
			{
				GlobalFree(hGlobal);
				_com_util::CheckError(hr);
			}
		}
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::TranslateAccelerator(MSG *pMsg)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	if(!m_bUserMode)
		return S_FALSE;	// No keyboard processing in Design Mode

	// Check if window deactivation has occured for us or the underlying control, and perform ui-deactivation in this case
	if(pMsg->message == WM_KILLFOCUS)
		UIDeactivate();

	// The underlying control has the first chance to crack at the Windows messages received by this window
	try
	{
		// See if we should invoke the callback (do not make calls for ordinary keys to improve performance)
		/*	// TODO: check if this drops the performance significantly
		bool	bCallback = false;
		if(pMsg->message == WM_KEYDOWN)
		{
			// Special processing for the spacebar key
			if(pMsg->wParam == 0x20)
				bCallback = true;
			// If a modifier key is pressed with an ordinary key
			else if((GetKeyState(VK_CONTROL) & 0x8000) || (GetKeyState(VK_MENU) & 0x8000))
				bCallback = true;
		}
		else if(pMsg->message == WM_SYSKEYDOWN)
			bCallback = true;
		*/

        // Callback!
		//if((bCallback) && (OnBeforeKeyDown((long)pMsg->wParam, !!(GetKeyState(VK_CONTROL) & 0x8000), !!(GetKeyState(VK_MENU) & 0x8000), !!(GetKeyState(VK_SHIFT) & 0x8000))))
		//	return S_OK;	// Handled by us, do not let to go on

		// Call a handler for key-down messages
		if(((pMsg->message == WM_KEYDOWN) || (pMsg->message == WM_SYSKEYDOWN)) && (OnBeforeKeyDown((long)pMsg->wParam, !!(GetKeyState(VK_CONTROL) & 0x8000), !!(GetKeyState(VK_MENU) & 0x8000), !!(GetKeyState(VK_SHIFT) & 0x8000))))
			return S_OK;	// Handled by us, do not let it go on to MSHTML

		// Call a handler for key-up messages
		if(((pMsg->message == WM_KEYUP) || (pMsg->message == WM_SYSKEYUP)) && (OnBeforeKeyUp((long)pMsg->wParam, !!(GetKeyState(VK_CONTROL) & 0x8000), !!(GetKeyState(VK_MENU) & 0x8000), !!(GetKeyState(VK_SHIFT) & 0x8000))))
			return S_OK;

		// Call a handler for char messages
		if(((pMsg->message == WM_CHAR) || (pMsg->message == WM_SYSCHAR)) && (OnBeforeKeyPress((long)pMsg->wParam, !!(GetKeyState(VK_CONTROL) & 0x8000), !!(GetKeyState(VK_MENU) & 0x8000), !!(GetKeyState(VK_SHIFT) & 0x8000))))
			return S_OK;


		/*if( (m_oBrowser != NULL) &&
			(((pMsg->message >= WM_KEYFIRST) && (pMsg->message <= WM_KEYLAST)) ||
			((pMsg->message >= WM_MOUSEFIRST) && (pMsg->message <= WM_MOUSELAST))) )*/
			if(((IOleInPlaceActiveObjectPtr)m_oBrowser)->TranslateAccelerator(pMsg) == S_OK)	// Check if the message has been processed by the control
			{
				TRACE(_T("[OMEA.MSHTML] Message was processed by the browser.\n"));
				return S_OK;
			}
			else
				TRACE(_T("[OMEA.MSHTML] Message was NOT processed by the browser.\n"));
	}
	COM_CATCH();

	// Delegate processing to the parent
	return IOleInPlaceActiveObjectImpl<CMshtmlBrowser>::TranslateAccelerator(pMsg);
}

STDMETHODIMP CMshtmlBrowser::DoVerb( LONG iVerb, /* Value representing verb to be performed */ LPMSG lpmsg, /* Pointer to structure that describes Windows */ /* message */ IOleClientSite *pActiveSite, /* Pointer to active client site */ LONG lindex, /* Reserved */ HWND hwndParent, /* Handle of window containing the object */ LPCRECT lprcPosRect /* Pointer to object's display rectangle */ )
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	TRACE(_T("[MSHTML] Doing Verb %d\r\n"), iVerb);	// TODO: remove
	try
	{
		// Call the base class implementation that applies to this very control
		IOleObjectImpl<CMshtmlBrowser>::DoVerb(iVerb, lpmsg, pActiveSite, lindex, hwndParent, lprcPosRect);

		// Create the inlying control if necessary
		TryInstantiate();

		// Delegate verb processing to the inlying control
		if(m_oBrowser != NULL)
			COM_CHECK(((IOleObjectPtr)m_oBrowser), DoVerb(iVerb, lpmsg, pActiveSite, lindex, hwndParent, lprcPosRect));
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::EnumVerbs( IEnumOLEVERB **ppEnumOleVerb /* Address of output variable that receives the IEnumOleVerb interface pointer*/ )
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	try
	{
		// Create the inlying control if necessary
		TryInstantiate();

		// Delegate operation to the inlying control
		if(m_oBrowser != NULL)
			COM_CHECK(((IOleObjectPtr)m_oBrowser), EnumVerbs(ppEnumOleVerb));
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::SetHostNames( /* [in] */ LPCOLESTR szContainerApp, /* [unique][in] */ LPCOLESTR szContainerObj)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	try
	{
		// Create the inlying control if necessary
		TryInstantiate();

		// Delegate operation to the inlying control
		if(m_oBrowser != NULL)
			COM_CHECK(((IOleObjectPtr)m_oBrowser), SetHostNames(szContainerApp, szContainerObj));
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::InitFromData( /* [unique][in] */ IDataObject *pDataObject, /* [in] */ BOOL fCreation, /* [in] */ DWORD dwReserved)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	try
	{
		// Create the inlying control if necessary
		TryInstantiate();

		// Delegate operation to the inlying control
		if(m_oBrowser != NULL)
			COM_CHECK(((IOleObjectPtr)m_oBrowser), InitFromData(pDataObject, fCreation, dwReserved));
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::GetClipboardData( /* [in] */ DWORD dwReserved, /* [out] */ IDataObject **ppDataObject)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	try
	{
		// Create the inlying control if necessary
		TryInstantiate();

		// Delegate operation to the inlying control
		if(m_oBrowser != NULL)
			COM_CHECK(((IOleObjectPtr)m_oBrowser), GetClipboardData(dwReserved, ppDataObject));
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::Update( void )
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	try
	{
		// Create the inlying control if necessary
		TryInstantiate();

		// Delegate operation to the inlying control
		if(m_oBrowser != NULL)
			COM_CHECK(((IOleObjectPtr)m_oBrowser), Update());
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::IsUpToDate( void)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	try
	{
		// Create the inlying control if necessary
		TryInstantiate();

		// Delegate operation to the inlying control
		if(m_oBrowser != NULL)
			COM_CHECK(((IOleObjectPtr)m_oBrowser), IsUpToDate());
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::SetColorScheme( /* [in] */ LOGPALETTE *pLogpal)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	try
	{
		// Create the inlying control if necessary
		TryInstantiate();

		// Delegate operation to the inlying control
		if(m_oBrowser != NULL)
			COM_CHECK(((IOleObjectPtr)m_oBrowser), SetColorScheme(pLogpal));
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::get_HtmlDocument(IDispatch** pVal)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	*pVal = NULL;
	try
	{
		// Create the inlying control if necessary
		TryInstantiate();

		if(m_oBrowser == NULL)
			return E_UNEXPECTED;

		// Check if this is actually an HTML document
		IHTMLDocument2Ptr	oDoc = m_oBrowser->Document;

		// Get the document pointer
		if(oDoc == NULL)
			return S_OK;	// Returns NULL

		return oDoc->QueryInterface(__uuidof(IDispatch), (void**)pVal);
	}
	COM_CATCH_RETURN();
}

STDMETHODIMP CMshtmlBrowser::ExecDocumentCommand(BSTR Command, VARIANT_BOOL PromptUser)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	try
	{
		// Create the inlying control if necessary
		TryInstantiate();

		// Get the command target pointer
		IOleCommandTargetPtr oCmdTarget;
		if((m_oBrowser != NULL) && (m_oBrowser->Document != NULL))
			oCmdTarget = m_oBrowser->Document;
		else
			return E_UNEXPECTED;

		DWORD	nCmdId = 0;

		// Choose the command id based on the command passed in
		_bstr_t	bsCommand = Command;
		if(bsCommand == _bstr_t(L"ViewSource") )
			nCmdId = HTMLID_VIEWSOURCE;
		else if(bsCommand == _bstr_t(L"Find"))
			nCmdId = HTMLID_FIND;
		else
			return Error(_T("Unknown browser command."));

		TRACE(_T("[OMEA.MSHTML] Execing the browser command\n"));

		// Execute the command, do not prompt user
		COM_CHECK(oCmdTarget, Exec(&CGID_IWebBrowser, nCmdId, (PromptUser != VARIANT_FALSE ? OLECMDEXECOPT_PROMPTUSER : OLECMDEXECOPT_DONTPROMPTUSER), NULL, NULL));
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::get_Browser(IDispatch** pVal)
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	try
	{
		TryInstantiate();	// Try creating the control, if not created yet

		if(m_oBrowser == NULL)	// Should not happen
			return E_UNEXPECTED;

		// Make QI on the browser object and return a reference
		return m_oBrowser->QueryInterface(__uuidof(IDispatch), (void**)pVal);
	}
	COM_CATCH_RETURN();
}

/// Deactivates an active in-place object and discards the object's undo state.
STDMETHODIMP CMshtmlBrowser::InPlaceDeactivate()
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	TRACE(_T("InPlaceDeactivate\n"));
	try
	{
		if(m_oBrowser == NULL)
			return E_FAIL;	// Must be already created if deactivating ;)

		// Disconnect the events sink from the Browser
		IWebBrowserEventsSinkImpl::DispEventUnadvise( m_oBrowser, &DIID_DWebBrowserEvents );
		IWebBrowserEvents2SinkImpl::DispEventUnadvise( m_oBrowser, &DIID_DWebBrowserEvents2 );

		// Delegate processing to the inlying control
		if(m_oBrowser != NULL)
			COM_CHECK(((IOleInPlaceObjectPtr)m_oBrowser), InPlaceDeactivate());

		// Call the base to process deactivation on our composite control
		IOleInPlaceObjectWindowlessImpl<CMshtmlBrowser>::InPlaceDeactivate();
	}
	COM_CATCH_RETURN();

	return S_OK;
}

/// Deactivates and removes the user interface that supports in-place activation.
STDMETHODIMP CMshtmlBrowser::UIDeactivate()
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	TRACE(_T("UIDeactivate\n"));
	try
	{
		if(m_oBrowser == NULL)
			return E_FAIL;	// Must be already created if deactivating ;)

		// Delegate processing to the inlying control
		if(m_oBrowser != NULL)
			COM_CHECK(((IOleInPlaceObjectPtr)m_oBrowser), UIDeactivate());

		// Call the base to process deactivation on our composite control
		IOleInPlaceObjectWindowlessImpl<CMshtmlBrowser>::UIDeactivate();
	}
	COM_CATCH_RETURN();

	return S_OK;
}

/// Reactivates a previously deactivated object, undoing the last state of the object.
STDMETHODIMP CMshtmlBrowser::ReactivateAndUndo()
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	try
	{
		if(m_oBrowser == NULL)
			return E_FAIL;	// Must be already created if deactivating ;)

		// Delegate processing to the inlying control
		if(m_oBrowser != NULL)
			COM_CHECK(((IOleInPlaceObjectPtr)m_oBrowser), ReactivateAndUndo());

		// Call the base to process deactivation on our composite control
		IOleInPlaceObjectWindowlessImpl<CMshtmlBrowser>::ReactivateAndUndo();
	}
	COM_CATCH_RETURN();

	return S_OK;
}

void CMshtmlBrowser::TryUIDeactivate()
{
	//ATLTRACE2(TRACE_VERBOSE, 4, __FUNCTION__);
	ASSERT(m_pSite != NULL);
	if(m_pSite == NULL)
		return;	// Not initialized yet

	// If we're not UI-Active already, then nothing to check as we cannot get more-ui-deactivated than now anyway ;)
	if(!m_pSite->IsUIActive())
		return;

	// The window that currently has focus
	HWND	hWnd = ::GetFocus();

	// Walk up the windows hierarchy to detect whether the focus resides somewhere beneath our window
	while(hWnd != NULL)
	{
		if(hWnd == m_hWnd)
			break;
		hWnd = ::GetParent(hWnd);
	}

	// If we have not met our window during the walk, then someone else has focus
	// Or, maybe, we just could not get who has the focus and had a NULL initially
	// The control should be UI-activated no more
	if(hWnd == NULL)
		UIDeactivate();	// Invoke the deactivation
}

STDMETHODIMP CMshtmlBrowser::get_ParentCallback(IDispatch** pVal)
{
	if(m_oParentCallback == NULL)	// Not set or already released
	{
		*pVal = NULL;
		return S_OK;
	}
	return m_oParentCallback->QueryInterface(__uuidof(IDispatch), (void**)pVal);
}

STDMETHODIMP CMshtmlBrowser::put_ParentCallback(IDispatch* newVal)
{
	if(newVal == NULL)
		return E_POINTER;

	// Store for calling back upon requests
	m_oParentCallback = newVal;

	// Attach for checking for the control site ambient properties
	if(m_pSite != NULL)
		m_pSite->SetAmbientDispatch(newVal);

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::get_PermissionSet(BSTR* pVal)
{
	_bstr_t	bsRet;
	switch(m_nPermissionSet)
	{
	case PS_Auto:
		bsRet = L"Auto";
		break;
	case PS_Nothing:
		bsRet = L"Nothing";
		break;
	case PS_Everything:
		bsRet = L"Everything";
		break;
	case PS_Zone:
		{
			TCHAR	szBuf[0x400];
			StringCchPrintf(szBuf, sizeof(szBuf) / sizeof(*szBuf), _T("#%d"), m_nSecurityZone);
			bsRet = szBuf;
		}
		break;
	default:
		return Error(_T("A fatal internal error has occured: unexpected switch value."));
	}

	*pVal = bsRet.copy();
	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::put_PermissionSet(BSTR newVal)
{
	_bstr_t	bsVal(newVal);

	if(newVal == _bstr_t(L"Auto"))
		m_nPermissionSet = PS_Auto;
	else if(newVal == _bstr_t(L"Nothing"))
		m_nPermissionSet = PS_Nothing;
	else if(newVal == _bstr_t(L"Everything"))
		m_nPermissionSet = PS_Everything;
	else
	{
		/*LPCTSTR	szVal = bsVal;
		if(szVal[0] != _T('#'))
			return E_INVALIDARG;
		scanf(*/

		if(newVal[0] != L'#')
			return E_INVALIDARG;
		m_nSecurityZone = _wtol(newVal + 1);
		m_nPermissionSet = PS_Zone;
	}

	return S_OK;
}


void CMshtmlBrowser::OnStatusTextChange( BSTR text )
{
	if(m_oParentCallback != NULL)
	{
		//TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
			DISPID dispid;
			OLECHAR FAR* szMember = L"OnStatusTextChange";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			_variant_t avtParams[] = { _variant_t(text) };
			DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, NULL, NULL));
		}
		COM_CATCH();
	}
}

void CMshtmlBrowser::OnTitleChange( BSTR text )
{
	if(m_oParentCallback != NULL)
	{
		TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
			DISPID dispid;
			OLECHAR FAR* szMember = L"OnTitleChange";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			_variant_t avtParams[] = { _variant_t(text) };
			DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, NULL, NULL));
		}
		COM_CATCH();
	}
}

void CMshtmlBrowser::OnBeforeNavigate1( BSTR /*URL*/, long /*Flags*/, BSTR /*TargetFrameName*/, VARIANT* /*PostData*/, BSTR /*Headers*/, VARIANT_BOOL *Cancel)
{
	TRACE(_T("[OMEA.MSHTML] OnBeforeNavigate1\n"));
	*Cancel = VARIANT_TRUE;
	return;
}

void CMshtmlBrowser::OnBeforeNavigate2( LPDISPATCH pDisp, VARIANT *url, VARIANT * /*Flags*/, VARIANT *TargetFrameName, VARIANT *PostData, VARIANT *Headers, VARIANT_BOOL *Cancel )
{
	TRACE(_T("[OMEA.MSHTML] OnBeforeNavigate2\n"));

	//*Cancel = VARIANT_TRUE;
	//return;

	if(m_oParentCallback != NULL)
	{
		TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
			// Check if the event applies to the main browser window
			SHDocVw::IWebBrowser2Ptr	oSourceBrowser = pDisp;
			if((SHDocVw::IWebBrowser2*)oSourceBrowser != (SHDocVw::IWebBrowser2*)m_oBrowser)
				return;	// The event was fired by whatever child frame

			// Invoke the callback
			DISPID dispid;
			OLECHAR FAR* szMember = L"OnBeforeNavigate";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			_variant_t avtParams[] = { Headers, PostData, TargetFrameName, url };
			DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
			_variant_t	vtRet;
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, &vtRet, NULL, NULL));

			// Check the return value, if any
			*Cancel = (bool)vtRet ? VARIANT_TRUE : VARIANT_FALSE;	// If the value cannot be coerced to bool, it throws (that means the return value was not a bool, eg empty)
			TRACE(_T("[OMEA.MSHTML] OnBeforeNavigate return value is %d."), (int)(short)*Cancel);
		}
		COM_CATCH();
	}
}

void CMshtmlBrowser::OnNavigateComplete( LPDISPATCH pDisp, VARIANT* URL )
{
	if(m_oParentCallback != NULL)
	{
		TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
			// Check if the event applies to the main browser window
			SHDocVw::IWebBrowser2Ptr	oSourceBrowser = pDisp;
			if((SHDocVw::IWebBrowser2*)oSourceBrowser != (SHDocVw::IWebBrowser2*)m_oBrowser)
				return;	// The event was fired by whatever child frame

			// Invoke the callback
			DISPID dispid;
			OLECHAR FAR* szMember = L"OnNavigateComplete";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			_variant_t avtParams[] = { URL };
			DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, NULL, NULL));
		}
		COM_CATCH();
	}
}

void CMshtmlBrowser::OnNavigateError( IDispatch* pDisp, VARIANT* URL, VARIANT* Frame, VARIANT* StatusCode, VARIANT_BOOL* Cancel )
{
	if(m_oParentCallback != NULL)
	{
		TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
			// Check if the event applies to the main browser window
			SHDocVw::IWebBrowser2Ptr	oSourceBrowser = pDisp;
			if((SHDocVw::IWebBrowser2*)oSourceBrowser != (SHDocVw::IWebBrowser2*)m_oBrowser)
				return;	// The event was fired by whatever child frame

			// Invoke the callback
			DISPID dispid;
			OLECHAR FAR* szMember = L"OnNavigateError";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			_variant_t avtParams[] = { StatusCode, Frame, URL };
			DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
			_variant_t	vtRet;
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, &vtRet, NULL, NULL));

			// Check the return value, if any
			*Cancel = (bool)vtRet ? VARIANT_TRUE : VARIANT_FALSE;	// If the value cannot be coerced to bool, it throws (that means the return value was not a bool, eg empty)
		}
		COM_CATCH();
	}
}

void CMshtmlBrowser::OnNewWindow( BSTR url, long /*Flags*/, BSTR TargetFrameName, VARIANT * PostData, BSTR Headers, VARIANT_BOOL * Processed)
{
	if(m_oParentCallback != NULL)
	{
		TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
			// Invoke the callback
			DISPID dispid;
			OLECHAR FAR* szMember = L"OnNewWindow";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			_variant_t avtParams[] = { Headers, PostData, TargetFrameName, url };
			DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
			_variant_t	vtRet;
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, &vtRet, NULL, NULL));

			// Check the return value, if any
			*Processed = (bool)vtRet ? VARIANT_TRUE : VARIANT_FALSE;	// If the value cannot be coerced to bool, it throws (that means the return value was not a bool, eg empty)
		}
		COM_CATCH();
	}
}

void CMshtmlBrowser::OnProgressChange( long Progress, long ProgressMax )
{
	if(m_oParentCallback != NULL)
	{
		TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
			// Adjust the percentage if it's unavailable
			double	fProgress;
			if(Progress == -1)	// Complete
				fProgress = 1.0;
			else if((Progress == 0) && (ProgressMax == 0))	// Special case, when progress is in range [0..0] :)
				fProgress = 1.0;
			else
				fProgress = (double)Progress / ProgressMax;

			// Invoke the callback
			DISPID dispid;
			OLECHAR FAR* szMember = L"OnProgressChange";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			_variant_t avtParams[] = { fProgress };
			DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, NULL, NULL));
		}
		COM_CATCH();
	}
}

void CMshtmlBrowser::OnDocumentComplete( IDispatch* pDisp, VARIANT* URL )
{
	if(m_oParentCallback != NULL)
	{
		TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
			// Check if the event applies to the main browser window
			SHDocVw::IWebBrowser2Ptr	oSourceBrowser = pDisp;
			if((SHDocVw::IWebBrowser2*)oSourceBrowser != (SHDocVw::IWebBrowser2*)m_oBrowser)
				return;	// The event was fired by whatever child frame

			// Invoke the callback
			DISPID dispid;
			OLECHAR FAR* szMember = L"OnDocumentComplete";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			_variant_t avtParams[] = { URL };
			DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, NULL, NULL));
		}
		COM_CATCH();
	}
}

void CMshtmlBrowser::OnQuit()
{
	// TODO: If you remove the sink unadvising code from the OnDeactivate handler, put it here.
}

void CMshtmlBrowser::OnDownloadComplete()
{
	if(m_oParentCallback != NULL)
	{
		TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
			// Invoke the callback
			DISPID dispid;
			OLECHAR FAR* szMember = L"OnDownloadComplete";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			DISPPARAMS dispparams = {NULL, NULL, 0, 0};
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, NULL, NULL));
		}
		COM_CATCH();
	}
}

bool CMshtmlBrowser::OnContextMenu( DWORD dwID, POINT* pptPosition, IUnknown* pCommandTarget, IDispatch* pDispatchObjectHit )
{
	if(m_oParentCallback != NULL)
	{
		TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
	UIDeactivate();

			// Invoke the callback
			DISPID dispid;
			OLECHAR FAR* szMember = L"OnContextMenu";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			_variant_t avtParams[] = { pDispatchObjectHit, pCommandTarget, (long)pptPosition->y, (long)pptPosition->x, (long)dwID };
			DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
			_variant_t	vtRet;
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, &vtRet, NULL, NULL));

			// Check the return value, if any
			return (bool)vtRet;	// If the value cannot be coerced to bool, it throws (that means the return value was not a bool, eg empty)
		}
		COM_CATCH();
	}

	return false;	// Not handled by default
}

bool CMshtmlBrowser::OnBeforeKeyDown( long code, bool ctrl, bool alt, bool shift )
{
	return OnBeforeKeyAny(L"OnBeforeKeyDown", code, ctrl, alt, shift);
}

bool CMshtmlBrowser::OnBeforeKeyUp( long code, bool ctrl, bool alt, bool shift )
{
	return OnBeforeKeyAny(L"OnBeforeKeyUp", code, ctrl, alt, shift);
}

bool CMshtmlBrowser::OnBeforeKeyPress( long code, bool ctrl, bool alt, bool shift )
{
	return OnBeforeKeyAny(L"OnBeforeKeyPress", code, ctrl, alt, shift);
}

bool CMshtmlBrowser::OnBeforeKeyAny( LPCWSTR szFunctionName, long code, bool ctrl, bool alt, bool shift )
{
	if(m_oParentCallback != NULL)
	{
		TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
			// Invoke the callback
			DISPID dispid;
			OLECHAR FAR* szMember = (LPWSTR)szFunctionName;
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			_variant_t avtParams[] = { shift, alt, ctrl, code };
			DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
			_variant_t	vtRet;
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, &vtRet, NULL, NULL));

			// Check the return value, if any
			return (bool)vtRet;	// If the value cannot be coerced to bool, it throws (that means the return value was not a bool, eg empty)
		}
		COM_CATCH();
	}

	return false;	// Not handled by default
}

bool CMshtmlBrowser::OnUrlAction(LPCWSTR pwszUrl, DWORD dwAction, DWORD dwFlags, BYTE *pPolicy)
{
	if(m_oParentCallback != NULL)
	{
		TRACE((LPCTSTR)(CA2T((LPCSTR)(__FUNCTION__))));
		try
		{
			// Invoke the callback
			DISPID dispid;
			OLECHAR FAR* szMember = L"OnUrlAction";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			_variant_t avtParams[] = { dwFlags, dwAction, (_bstr_t)pwszUrl };
			DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };
			_variant_t	vtRet;
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, &vtRet, NULL, NULL));

			if( ( V_VT(&vtRet) == VT_ERROR ) || ( V_VT(&vtRet) == VT_EMPTY ) )	// VT_ERROR + DISP_E_… means that the parameter is missing
				return false;	// Not handled

			*pPolicy = (BYTE)(long)vtRet;	// Extract the return value, that must be an integer in this case
			return true;	// we reach this point, we have succeeded.
		}
		COM_CATCH();
	}

	return false;	// Not handled by default
}

bool CMshtmlBrowser::OnGetHostInfo( DWORD *pFlags )
{
	if(m_oParentCallback != NULL)
	{
		try
		{
			// Declarations for the invocations
			DISPID dispid;
			OLECHAR FAR* szMember;
			DISPPARAMS dispparams = { NULL, NULL, 0, 0 };
			_variant_t	vtRet;

			szMember = L"AmbientHostUiInfo";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_PROPERTYGET, &dispparams, &vtRet, NULL, NULL));

			if( ( V_VT(&vtRet) == VT_ERROR ) || ( V_VT(&vtRet) == VT_EMPTY ) )	// VT_ERROR + DISP_E_… means that the parameter is missing
				return false;	// Not handled

			*pFlags = (DWORD)(long)vtRet;	// Extract and convert the property value
			return true;	// Succeeded
		}
		COM_CATCH();
	}

	return false;	// Not handled by default
}

void CMshtmlBrowser::OnBrowserCreated()
{
	if(m_oParentCallback != NULL)
	{
		try
		{
			// Declarations for the invocations
			DISPID dispid;
			OLECHAR FAR* szMember;
			DISPPARAMS dispparams = { NULL, NULL, 0, 0 };

			szMember = L"OnBrowserCreated";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, NULL, NULL));
		}
		COM_CATCH();
	}
}

IDispatchPtr CMshtmlBrowser::OnGetExternal()
{
	if(m_oParentCallback != NULL)
	{
		try
		{
			// Declarations for the invocations
			DISPID dispid;
			OLECHAR FAR* szMember;
			DISPPARAMS dispparams = { NULL, NULL, 0, 0 };
			_variant_t	vtRet;

			szMember = L"ExternalObject";
			COM_CHECK(m_oParentCallback, GetIDsOfNames( IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
			COM_CHECK(m_oParentCallback, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_PROPERTYGET, &dispparams, &vtRet, NULL, NULL));

			TRACE(_T("[MSHTML] The ExternalObject VARIANT type is %#010X.\n"), V_VT(&vtRet));
			if(V_VT(&vtRet) == VT_DISPATCH)
				return (IDispatch*)vtRet;	// Take the dispatch pointer directly
			else
				return (IDispatchPtr)(IUnknown*)vtRet;	// Try to extract IUnknown and query it for IDispatch
		}
		COM_CATCH();
	}

	return NULL;	// Not handled by default
}

STDMETHODIMP CMshtmlBrowser::QueryExternal(REFIID riid, void**pp)
{
	if(m_oParentCallback == NULL)
		return E_NOINTERFACE;
	return m_oParentCallback->QueryInterface( riid, pp );
}

STDMETHODIMP CMshtmlBrowser::SettingsChanged(void)
{
	try
	{
		TryInstantiate();	// Try to create the browser, if needed

		COM_CHECK(((IOleControlPtr)m_oBrowser), OnAmbientPropertyChange(DISPID_AMBIENT_DLCONTROL));	// Notify the web browser that its properties have changed.
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::GetShortFilePath(BSTR LongPath, BSTR* ShortPath)
{
	// TODO: Add your implementation code here
    TCHAR sShortPath[MAX_PATH+1];
    if( !GetShortPathName( _bstr_t(LongPath), sShortPath, MAX_PATH ) )
		return AtlHresultFromLastError();	// Has failed for some reason

	*ShortPath = _bstr_t(sShortPath).copy();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::GetHtmlText(BSTR* Html)
{
	try
	{
		TryInstantiate();

		if(m_oBrowser->Document == NULL)	// Not navigated to a page yet
			return E_UNEXPECTED;

		// Create a memory stream
		IStreamPtr	oStream;
		CHECK(CreateStreamOnHGlobal(NULL, TRUE, &oStream));

		// Get the content
		COM_CHECK(IPersistStreamInitPtr(m_oBrowser->Document), Save(oStream, false));

		// Get the size
		STATSTG	statstg;
		ZeroMemory(&statstg, sizeof(statstg));
		COM_CHECK(oStream, Stat(&statstg, STATFLAG_NONAME));

		// Rewind the stream
		LARGE_INTEGER	li;
		li.QuadPart = 0;
		COM_CHECK(oStream, Seek(li, STREAM_SEEK_SET, NULL));

		// Preapre the buffer
		std::basic_string<WCHAR>	sHtml;
		sHtml.resize((int)ceil(statstg.cbSize.QuadPart * 0.5));

		// Read the data
		COM_CHECK(oStream, Read(&sHtml[0], (ULONG)statstg.cbSize.QuadPart, NULL));

		// Create a basic string
		_bstr_t	bsRet = sHtml.c_str();

		// Return
		*Html = bsRet.Detach();
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMshtmlBrowser::ResurrectWebBrowser()
{
	try
	{
		// Invalidate the browser
		m_oBrowser = NULL;

		// Try re-creating
		TryInstantiate();
	}
	COM_CATCH_RETURN();

	return S_OK;
}
