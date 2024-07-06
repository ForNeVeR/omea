// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// Band.cpp : Implementation of CBand
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "StdAfx.h"
#include "Band.h"

// Initialize static variables
CString	CBand::m_sWindowClassName = CBand::GetWindowClassName();
UINT	CBand::m_nUpdateControlsMessage = RegisterWindowMessage(_T("WM_JetIe_") + CJetIe::LoadStringT(IDS_PLUGIN_NAME) + _T("_BandUpdateControls"));

#define CHEVRON_WIDTH	0x10

// CBand
CBand::CBand()
{
	m_dwBandId = NULL;
	m_dwViewMode = DBIF_VIEWMODE_NORMAL;
	m_hwndParent = NULL;
	m_hwndTopmostParent = NULL;
	m_bMouseCaptured = false;
	m_nChevronState = csNormal;

	ASSERT(!(m_guidToolbar.Data1 = 0));

	m_oActionManager = CJetIe::GetActionManager();

	m_dwLastUpdateControls = GetTickCount();

	// Load the icons
	m_iconChevron = LoadIcon(CJetIe::GetModuleInstanceHandle(), MAKEINTRESOURCE(IDI_CHEVRON));
}

CBand::~CBand()
{
	ASSERT(m_guidToolbar.Data1 != 0);
}

STDMETHODIMP CBand::SetSite(IUnknown* pUnkSite)
{
	IObjectWithSiteImpl<CBand>::SetSite(pUnkSite);

	if(pUnkSite == NULL)
	{
		m_oBrowser = NULL;
		return S_OK;	// We're detaching, not attaching
	}

	try
	{
		// To retrieve the top-level IWebBrowser2 reference, get IServiceProvider from the client site and perform a QueryService for IID_IServiceProvider under the service SID_STopLevelBrowser (defined in Shlguid.h). From this second IServiceProvider, perform a QueryService for IID_IWebBrowser2 in the SID_SWebBrowserApp service.
		// The best place to perform this work is in the SetClientSite() method of IOleObject.
		IServiceProviderPtr	oSiteServiceProvider = (IUnknown*)m_spUnkSite;	// Site's Service Provider
		IServiceProviderPtr	oTopLevelWebBrowserServiceProvider;	// Service Provider of the Web browser object for the top-level frame window
		oSiteServiceProvider->QueryService(SID_STopLevelBrowser, IID_IServiceProvider, reinterpret_cast<void **>(&oTopLevelWebBrowserServiceProvider));	// Get it
		oTopLevelWebBrowserServiceProvider->QueryService(SID_SWebBrowserApp, IID_IWebBrowser2, reinterpret_cast<void **>(&m_oBrowser));	// Get the Web browser object interface of the top-level frame window

		// Get the parent window
		IOleWindowPtr	oWindow = pUnkSite;
		COM_CHECK(oWindow, GetWindow(&m_hwndParent));

		// Get the topmost parent window
		m_hwndTopmostParent = m_hwndParent;
		while(::GetParent(m_hwndTopmostParent) != NULL)
			m_hwndTopmostParent = ::GetParent(m_hwndTopmostParent);

		// Now create our window (if not created yet)
		TRACE(L"Creating the band window.");
		if(!::IsWindow(m_hWnd))
		{
			CRect	rc;
			::GetClientRect(m_hwndParent, &rc);
			//TRACE(L"Assigning window style to %#010X.", WS_CHILD | WS_CLIPSIBLINGS | TBSTYLE_ALTDRAG | TBSTYLE_FLAT | TBSTYLE_LIST | TBSTYLE_TRANSPARENT | TBSTYLE_REGISTERDROP | TBSTYLE_TOOLTIPS | TBSTYLE_WRAPABLE | CCS_ADJUSTABLE | CCS_NODIVIDER | CCS_NOPARENTALIGN | CCS_NORESIZE);
			//if(Create(m_hwndParent, &rc, _T("JetIe Rebar Band"), WS_CHILD | WS_CLIPSIBLINGS | TBSTYLE_ALTDRAG | TBSTYLE_FLAT | TBSTYLE_LIST | TBSTYLE_TRANSPARENT | TBSTYLE_REGISTERDROP | TBSTYLE_TOOLTIPS | TBSTYLE_WRAPABLE | CCS_ADJUSTABLE | CCS_NODIVIDER | CCS_NOPARENTALIGN | CCS_NORESIZE) == NULL)

			/*HWND	hwnd = CreateWindow(TOOLBARCLASSNAME, _T("JetIe Rebar Band"), 0x5600994D, rc.left, rc.top, rc.Width(), rc.Height(), m_hwndParent, NULL, CJetIe::GetModuleInstanceHandle(), NULL);
			ASSERT(hwnd != NULL);
			SubclassWindow(hwnd);
			BOOL	bDummy;
			OnCreate(WM_CREATE, 0, 0, bDummy);*/

			//if(Create(m_hwndParent, &rc, _T("JetIe Rebar Band"), 0x5600994D) == NULL)
			if(Create(m_hwndParent, &rc, _T("JetIe Rebar Band"),
				WS_CHILDWINDOW | WS_CLIPSIBLINGS | WS_CLIPCHILDREN | /*TBSTYLE_TRANSPARENT | TBSTYLE_LIST | */TBSTYLE_FLAT | TBSTYLE_TOOLTIPS | CCS_NODIVIDER | CCS_NOPARENTALIGN | CCS_NORESIZE | CCS_TOP/*,
				WS_EX_LEFT | WS_EX_LTRREADING | WS_EX_RIGHTSCROLLBAR | WS_EX_TOOLWINDOW*/) == NULL)

			{
				TRACE(L"The band window could not be created.");
				return E_FAIL;
			}
		}
	}
	COM_CATCH();

	return S_OK;
}

STDMETHODIMP CBand::GetBandInfo(DWORD dwBandID, DWORD dwViewMode, DESKBANDINFO* pdbi)
{
	m_dwBandId = dwBandID;
	m_dwViewMode = dwViewMode;
	pdbi->dwMask |= DBIM_ACTUAL;

	// Calculate the toolbar height
	int	nIconHeight = GetSystemMetrics(SM_CYSMICON);
	int	nToolbarHeight = nIconHeight + 3 * 2;	// Add the gaps

	if(pdbi->dwMask & DBIM_MINSIZE)
	{
		pdbi->ptMinSize.x = 100;
		pdbi->ptMinSize.y = nToolbarHeight;
	}

	if(pdbi->dwMask & DBIM_MAXSIZE)
	{
		pdbi->ptMaxSize.x = -1;	// Unlimited
		pdbi->ptMaxSize.y = -1;
	}

	if(pdbi->dwMask & DBIM_INTEGRAL)
	{
		pdbi->ptIntegral.x = 1;
		pdbi->ptIntegral.y = nToolbarHeight;
	}

	if(pdbi->dwMask & DBIM_ACTUAL)
	{
		pdbi->ptActual.x = 1024;	// TODO: calculate what we actually need
		pdbi->ptActual.y = nToolbarHeight;
	}

	if(pdbi->dwMask & DBIM_TITLE)
	{
		CStringW	sTitle;
		// Get the toolbar title if it should be shown, otherwise, leave it blank
		if((m_xmlControls != NULL) && (m_xmlControls->selectSingleNode(L"@ShowTitle") != NULL) && ((long)m_xmlControls->getAttribute(L"ShowTitle")))
			sTitle = (LPCWSTR)(_bstr_t)m_xmlControls->getAttribute(L"Title");
		StringCchCopyW(pdbi->wszTitle, 0x10, (LPCWSTR)sTitle);	// Copy the title
	}

	if(pdbi->dwMask & DBIM_MODEFLAGS)
	{
		pdbi->dwModeFlags = DBIMF_NORMAL;
		pdbi->dwModeFlags |= DBIMF_VARIABLEHEIGHT;
	}

	if(pdbi->dwMask & DBIM_BKCOLOR)
	{
		//Use the default background color by removing this flag.
		pdbi->dwMask &= ~DBIM_BKCOLOR;
	}

	return S_OK;
}

STDMETHODIMP CBand::HasFocusIO()
{
	HWND	hwnd = ::GetFocus();
	while(hwnd != NULL)
	{
		if(hwnd == m_hWnd)	// We're an (indirect) parent of the focused window
			break;
		hwnd = ::GetParent(hwnd);
	}
	if(hwnd == NULL)	// Reached the topmost window, did not find ourselves
		return S_FALSE;

	return S_OK;	// We're above the focus
}

STDMETHODIMP CBand::TranslateAcceleratorIO(LPMSG lpMsg)
{
	return S_FALSE;
}

STDMETHODIMP CBand::UIActivateIO(BOOL fActivate, LPMSG lpMsg)
{
	if(fActivate)
		SetFocus();
	return S_OK;
}

STDMETHODIMP CBand::GetClassID(CLSID *pClassID)
{
	*pClassID = __uuidof(CBand);
	return S_OK;
}

STDMETHODIMP CBand::IsDirty()
{
	return S_FALSE;
}

STDMETHODIMP CBand::Load(LPSTREAM)
{
	return S_OK;
}

STDMETHODIMP CBand::Save(LPSTREAM, BOOL)
{
	return S_OK;
}

STDMETHODIMP CBand::GetSizeMax(ULARGE_INTEGER*)
{
	return E_NOTIMPL;
}

STDMETHODIMP CBand::GetWindow(HWND *phwnd)
{
	if(m_hWnd == NULL)	// Not created yet
		return E_UNEXPECTED;
	*phwnd = m_hWnd;
	return S_OK;
}

STDMETHODIMP CBand::ContextSensitiveHelp(BOOL fEnterMode)
{
	// TODO: implement the context-sensitive help mode
	return E_NOTIMPL;
}

STDMETHODIMP CBand::CloseDW(DWORD)
{
	TRACE(L"The band docking window is being closed.");
	ASSERT(m_hWnd != NULL);	// Must still exist

	// Destroy the window
	if(m_hWnd != NULL)	// This may happen if the window failed to be created
		DestroyWindow();

	return S_OK;
}

STDMETHODIMP CBand::ResizeBorderDW(LPCRECT prcBorder, IUnknown *punkToolbarSite, BOOL)
{
	// This method is never called for the band objects. He-heh :)
	TRACE(L"Warning! An unexpected thus unhandled call to IDockingWindow::ResizeBorderDW.");
	return E_NOTIMPL;
}

STDMETHODIMP CBand::ShowDW(BOOL bShow)
{
	// Note: we do not call the IDockingWindowSite::SetBorderSpaceDW because it's not needed for the tool bands.
	if(!::IsWindow(m_hWnd))
		return E_UNEXPECTED;

	ShowWindow(bShow ? SW_SHOW : SW_HIDE);

	return S_OK;
}

LRESULT CBand::OnCreate(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	TRACE(L"OnCreateEnter: The window style is %#010X.", GetWindowLong(GWL_STYLE));
	if(DefWindowProc() == -1)	// Allow the default handler to execute
		return -1;
	bHandled = TRUE;	// Handled by both us and the default handler
	TRACE(L"OnCreateCalledSuper: The window style is %#010X.", GetWindowLong(GWL_STYLE));

	// Initialize the toolbar
	TRACE(L"OnCreate: Initializing the toolbar.");
	SendMessage(TB_BUTTONSTRUCTSIZE, (WPARAM)(int)sizeof(TBBUTTON), 0);

	// Set the window that will receive the toolbar notification messages
	SendMessage(TB_SETPARENT, (WPARAM)m_hWnd, 0);

	// Update the window style
	SetWindowLong(GWL_STYLE, 0x5600994D);
	SetWindowLong(GWL_EXSTYLE, 0x00000080);
	//SetWindowPos(0, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_NOZORDER);

	// Set the toolbar's extended style
	SendMessage(TB_SETEXTENDEDSTYLE, 0, (LPARAM)(TBSTYLE_EX_DRAWDDARROWS | TBSTYLE_EX_HIDECLIPPEDBUTTONS | TBSTYLE_EX_MIXEDBUTTONS | TBSTYLE_EX_DOUBLEBUFFER));
	SendMessage(TB_SETWINDOWTHEME, 0, (LPARAM)(LPCWSTR)L"TOOLBAR");
	SetWindowPos(0, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOZORDER);

	if(!CreateControls())
	{
		TRACE(L"The toolbar has failed to self-initialize. Destroying the window.");
		return -1;	// Suicide
	}

	// Start the updater timer
	SetTimer(timerUpdateUI, timerUpdateUIInterval, NULL);

	TRACE(L"OnCreateExit: The window style is %#010X.", GetWindowLong(GWL_STYLE));
	return 0;
}

BOOL CBand::CreateControls()
{
	try
	{
		// Remove the old toolbar controls (if any)
		for(int a = 0; (a < 0x1000) && (SendMessage(TB_DELETEBUTTON, 0, 0)); a++)
			;	// Delete the first button while there are more buttons

		TBBUTTON	btn;
		ZeroMemory(&btn, sizeof(btn));

		// Load or reload the XML control layout data
		// Obtain a copy of this toolbar's controls
		XmlElement	xmlControlsLive = m_oActionManager->ControlFamilyFromGuid(m_guidToolbar, L"Toolbar");
		XmlDocument	xmlCopyDoc = CJetIe::CreateXmlDocument();
		xmlCopyDoc->loadXML(xmlControlsLive->xml);	// Create a copy starting from that element
		m_xmlControls = xmlCopyDoc->selectSingleNode(L"/Controls");

		XmlNodeList	xmlControls = m_xmlControls->selectNodes(L"*");	// All the nodes
		XmlElement	xmlControl;	// Each control on the toolbar
		XmlElement	xmlAction;	// Action that corresponds to the current control
		int	nIndex = 0;
		CString	sButtonText;	// Holds the buffer while the button is being assigned

		// Create the image list and attach to the toolbar
		m_ilAllNormal.Attach(ImageList_Create(16, 16, ILC_COLOR32 | ILC_MASK, xmlControls->length, xmlControls->length));	// Create or re-create each one
		m_ilAllHot.Attach(ImageList_Create(16, 16, ILC_COLOR32 | ILC_MASK, xmlControls->length, xmlControls->length));
		m_ilAllDisabled.Attach(ImageList_Create(16, 16, ILC_COLOR32 | ILC_MASK, xmlControls->length, xmlControls->length));

		// Enable multiple image lists
		if(SendMessage(CCM_GETVERSION, 0, 0) <= 5)	// Determine the current CommonControls Version. Raise if below 5
			SendMessage(CCM_SETVERSION, (WPARAM)(DWORD)5, 0);
		SendMessage(TB_SETIMAGELIST, IDIL_ALL, (LPARAM)m_ilAllNormal);	// Submit
		SendMessage(TB_SETHOTIMAGELIST, IDIL_ALL, (LPARAM)m_ilAllHot);
		SendMessage(TB_SETDISABLEDIMAGELIST, IDIL_ALL, (LPARAM)m_ilAllDisabled);

		// Enumerate and create all the controls
		while((xmlControl = xmlControls->nextNode()) != NULL)
		{
			try	// Per-control failures do not cause the global toolbar failures
			{
				// Preinit
				btn.iBitmap = I_IMAGENONE;	// TODO: control's setting should take precedence, if present
				btn.idCommand = IDC_CONTROLBASE + nIndex;
				btn.fsState = TBSTATE_HIDDEN | TBSTATE_INDETERMINATE;	// Disabled and hidden by default
				btn.dwData = nIndex;	// Index in the controls list

				// Further processing depends on the control type
				if(xmlControl->baseName == (_bstr_t)L"Control")	// Just an ordinary control
				{
					// Get the corresponding action
					xmlAction = m_oActionManager->GetAction2(xmlControl);

					// Fill in the btn structure that will create the button
					PrepareButtonControl(xmlAction, btn, sButtonText);

					// Actually add the button onto a toolbar
					if(!SendMessage(TB_INSERTBUTTON, (WPARAM)-1, (LPARAM)&btn))
						ThrowError(CJetIe::GetSystemError());
				}	// "Control"
				else if(xmlControl->baseName == (_bstr_t)L"DropDownButton")	// A drop-down button with a drop-arrow and default action, both clickable:  [button|v]
				{
					// Get an entry id of the default control dropping out of this button, retrieve that control using this entry-id, and then take its action
					xmlAction = m_oActionManager->GetAction2(m_oActionManager->ControlFromEntryID(xmlControl->getAttribute(L"Default"), xmlControl, false));

					// Fill in the btn structure that will create the button
					PrepareButtonControl(xmlAction, btn, sButtonText);

					// Update to turn it into a dropdown button
					btn.fsStyle |= BTNS_DROPDOWN;
					if(!SendMessage(TB_INSERTBUTTON, (WPARAM)-1, (LPARAM)&btn))
						ThrowError(CJetIe::GetSystemError());
				}
				else if(xmlControl->baseName == (_bstr_t)L"Separator")	// Add a separator to the toolbar
				{
					btn.fsStyle = BTNS_SEP;
					if(!SendMessage(TB_INSERTBUTTON, (WPARAM)-1, (LPARAM)&btn))
						ThrowError(CJetIe::GetSystemError());
				}
				else	// Yet another control type
				{
					ASSERT(FALSE);
					ThrowError(CJetIe::LoadString(IDS_E_CONTROLTYPE));
				}

				// Increase the button index only in case the button has succeeded in being created
				nIndex++;
			}
			catch(_com_error e)
			{
				COM_TRACE();
				TRACE(L"Failed to create a toolbar button for %s.", (LPCWSTR)(_bstr_t)xmlControl->xml);
			}
		}
	}
	catch(_com_error e)
	{
		COM_TRACE();

		// TODO: report the error
		return FALSE;	// Indicates failure
	}

	// Enable/disable, as needed
	UpdateControls();

	return TRUE;	// More or less successful
}


LRESULT CBand::OnDestroy(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	DefWindowProc();	// Let the subclass make its cleanup
	return 0;
}

HICON CBand::LoadResourceIcon(CString sOrigin)
{
	if(sOrigin.Left(8) == _T("%JETIE%,"))	// Denotes an icon in this DLL's resources
		return LoadIcon(CJetIe::GetModuleInstanceHandle(), MAKEINTRESOURCE(_tstoi(sOrigin.Right(sOrigin.GetLength() - 8))));
		//return (HICON)LoadImage(CJetIe::GetModuleInstanceHandle(), MAKEINTRESOURCE(_tstoi(sOrigin.Right(sOrigin.GetLength() - 8))), IMAGE_ICON, 16, 16, LR_CREATEDIBSECTION | LR_SHARED);
	else	// Icon in a file or other module's resourcess
		return (HICON)LoadImage(NULL, sOrigin, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_SHARED);
}

void CBand::UpdateControls()
{
	//TRACE(L"Updating the toolbar controls state.");
	try
	{
		XmlNodeList	xmlControls = m_xmlControls->selectNodes(L"*");
		XmlElement	xmlControl;	// Each control on the toolbar

		// Enumerate and poll all the controls
		for(int nIndex = 0; ((xmlControl = xmlControls->nextNode()) != NULL); nIndex++)
			UpdateControl(xmlControl, nIndex);	// If some control fails, that should not prevent from processing the rest of controls
	}
	COM_CATCH();

	m_dwLastUpdateControls = GetTickCount();
}

void CBand::UpdateControl(XmlElement xmlControl, int nIndex)
{
	try
	{
		///////////////////////////////////
		// Query for the new control style

		// Variables that describe the control state and should be gotten from the control callbacks
		DWORD	dwOleCmdF;
		CStringW	sTitle;

		if(xmlControl->baseName == (_bstr_t)L"Control")	// A control
		{
			m_oActionManager->QueryStatus((_bstr_t)xmlControl->getAttribute(L"Action"), (_variant_t)(IDispatch*)m_oBrowser, &dwOleCmdF, &sTitle, NULL, NULL, true);
			if(!m_oActionManager->HasText((_bstr_t)xmlControl->getAttribute(L"Action")))
				sTitle.Empty();	// Do not submit text to the button if it's not allowed for this control
		}
		else if(xmlControl->baseName == (_bstr_t)L"DropDownButton")	// A drop-down with button
			m_oActionManager->QueryStatus((_bstr_t)m_oActionManager->ControlFromEntryID(xmlControl->getAttribute(L"Default"), xmlControl, false)->getAttribute(L"Action"), (_variant_t)(IDispatch*)m_oBrowser, &dwOleCmdF, &sTitle, NULL, NULL, true);
		else if(xmlControl->baseName == (_bstr_t)L"Separator")	// A separator
			dwOleCmdF = OLECMDF_ENABLED | OLECMDF_SUPPORTED;	// Display it
		else	// Unknown control type
		{
			ASSERT(FALSE);
			ThrowError(CJetIe::LoadString(IDS_E_CONTROLTYPE));
		}

		///////////////////////////////
		// Apply the new control state
		bool	bChanged = false;	// ORs the changes to the button state, submit the new state only if there's a change really

        // Request the old state to merge with
		// Also it's used to test whether there were any changes to the button state actually, and if no, the setter is not invoked (as it causes flickering)
		TBBUTTONINFO	tbi = {0};
		ZeroMemory(&tbi, sizeof(tbi));
		tbi.cbSize = sizeof(tbi);
		tbi.dwMask = TBIF_STATE | TBIF_TEXT | TBIF_BYINDEX;
		CString	sOldButtonTitle;
		int	nBufferLen = 0x400;
		tbi.pszText = sOldButtonTitle.GetBuffer(nBufferLen);
		tbi.cchText = nBufferLen;
		SendMessage(TB_GETBUTTONINFO, (WPARAM)nIndex, (LPARAM)&tbi);
		sOldButtonTitle.ReleaseBuffer();

		// Do the merge
		BYTE	fsStateOld = tbi.fsState;
		tbi.fsState = 0;
		if(!(dwOleCmdF & OLECMDF_SUPPORTED))
			tbi.fsState |= TBSTATE_HIDDEN;
		if(dwOleCmdF & OLECMDF_ENABLED)
			tbi.fsState |= TBSTATE_ENABLED;
		if(dwOleCmdF & OLECMDF_LATCHED)
			tbi.fsState |= TBSTATE_CHECKED;
		tbi.fsState |= fsStateOld & TBSTATE_PRESSED;	// Maintain the pressed-by-mouse button state
		bChanged = bChanged || (tbi.fsState != fsStateOld);	// Test for the change

		// Provide title, if such info is available
		CString	sTitleT;
		if(!sTitle.IsEmpty())
		{
			sTitleT = (LPCTSTR)CW2T((LPCWSTR)sTitle);
			tbi.pszText = (LPTSTR)(LPCTSTR)sTitleT;
			tbi.dwMask |= TBIF_TEXT;
		}
		bChanged = bChanged || (sTitleT != sOldButtonTitle);	// Test for the change

		// Apply the new state
		if(bChanged)
			SendMessage(TB_SETBUTTONINFO, (WPARAM)nIndex, (LPARAM)&tbi);
	}
	COM_CATCH();
}

LRESULT CBand::OnCommand(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	TRACE(L"OnCommand!");
	bHandled = FALSE;	// Will be set to Handled by an individual handler

	if((HWND)lParam == m_hWnd)	// Messages from the toolbar we're handling
	{
		if((LOWORD(wParam) >= IDC_CONTROLBASE) && (LOWORD(wParam) <= IDC_CONTROLLIMIT))	// A message from the toolbar button
		{
			// TODO: parse the notification code
			bHandled = TRUE;
			try
			{
				// Get the control's element
				long	nIndex = LOWORD(wParam) - IDC_CONTROLBASE;
				XmlElement	xmlControl = m_xmlControls->selectNodes(L"*")->item[nIndex];

				// React depending on the control type
				if(xmlControl->baseName == (_bstr_t)L"Control")	// TODO: check the base type of the action
					m_oActionManager->Execute2(xmlControl, (_variant_t)(IDispatch*)(IDispatchPtr)m_oBrowser);	// Execute the action associated with the control
				else if(xmlControl->baseName == (_bstr_t)L"DropDownButton")	// A button part of the drop-down button has been clicked
					m_oActionManager->Execute2(m_oActionManager->ControlFromEntryID(xmlControl->getAttribute(L"Default"), xmlControl, false), (_variant_t)(IDispatch*)(IDispatchPtr)m_oBrowser);	// Execute an action associated with the active button of this dropdown
				else
					TRACE(L"Received a command for an unknown control type (index=%d, type=%s).", nIndex, (LPCWSTR)xmlControl->baseName);

				UpdateControl(xmlControl, nIndex);	// Update the control state and appearance
			}
			catch(_com_error e)
			{
				CStringW	sError = COM_REASON(e);
				COM_TRACE();
				CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_E_CANNOTEXECUTEACTION) + L'\n' + sError, NULL, CPopupNotification::pmStop);
			}
		}
	}

	return 0;
}

LRESULT CBand::OnNotify(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	//TRACE(L"OnNotify (%#010X)/(%#010X,a%#010X,w%#010X,%#010X)!", ((NMHDR*)lParam)->code, TBN_GETINFOTIP, TBN_GETINFOTIPA, TBN_GETINFOTIPW, NM_HOVER);
	bHandled = TRUE;	// Prevent the base class (commctl toolbar 32) from processing any of the notification messages because it will re-emit them to our class and get into an almost infinite loop
	return 0;
}

LRESULT CBand::OnGetInfoTip(int idCtrl, LPNMHDR pnmh, BOOL& bHandled)
{
	bHandled = FALSE;
	TRACE(L"OnNotify::GetInfoTip!");
	try
	{
		NMTBGETINFOTIP	*pTip = (NMTBGETINFOTIP*)pnmh;

		// Select action of the control with an index appropriate
		XmlElement	xmlControl = m_xmlControls->selectNodes(L"*")->item[(long)pTip->lParam];
		_bstr_t	bsAction = m_oActionManager->GetAction2(xmlControl)->getAttribute(L"ID");
		CStringW	sInfoTip;
		m_oActionManager->QueryStatus(bsAction, (_variant_t)(IDispatch*)m_oBrowser, NULL, NULL, &sInfoTip, NULL, true);

		// Submit
		StringCchCopy(pTip->pszText, pTip->cchTextMax, CW2T((LPCWSTR)sInfoTip));
		bHandled = TRUE;
	}
	COM_CATCH();

	return 0;
}

LRESULT CBand::OnNeedText(int idCtrl, LPNMHDR pnmh, BOOL& bHandled)
{
	bHandled = FALSE;
	//TRACE(L"OnNotify::TtnNeedText!");

	try
	{
		NMTTDISPINFO	*pDI = (NMTTDISPINFO*)pnmh;

		// Select action of the control with an index appropriate
		//TRACE(L"ONT-00");
		//TRACE(L"ONT-00-0 Index: %d.", (long)(pDI->hdr.idFrom - IDC_CONTROLBASE));
		//TRACE(L"ONT-00-1 Controls count: %d.", m_xmlControls->selectNodes(L"*")->length);
		XmlElement	xmlControl = m_xmlControls->selectNodes(L"*")->item[(long)(pDI->hdr.idFrom - IDC_CONTROLBASE)];
        _bstr_t	bsAction = m_oActionManager->GetAction2(xmlControl)->getAttribute(L"ID");
		//TRACE(L"ONT-01");
		CStringW	sInfoTip;
		m_oActionManager->QueryStatus(bsAction, (_variant_t)(IDispatch*)m_oBrowser, NULL, NULL, &sInfoTip, NULL, true);
		//TRACE(L"ONT-02");

		// Submit
		pDI->hinst = NULL;
		pDI->lpszText = NULL;
		StringCchCopy(pDI->szText, 80, CW2T((LPCWSTR)sInfoTip));
		bHandled = TRUE;
	}
	COM_CATCH();

	return 0;
}

LRESULT CBand::OnNotifyFormat(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = TRUE;
	if(lParam == NF_QUERY)
#ifdef _UNICODE
		return NFR_UNICODE;
#else
		return NFR_ANSI;
#endif

	return 0;	// An unknown command
}

void CBand::SetToolbarGuid(GUID guid)
{
	ASSERT(m_guidToolbar.Data1 == 0);
	m_guidToolbar = guid;
}

LRESULT CBand::OnDropDown(int idCtrl, LPNMHDR pnmh, BOOL& bHandled)
{
	NMTOOLBAR	*pNM = (NMTOOLBAR*)pnmh;

	if((pNM->iItem >= IDC_CONTROLBASE) && (pNM->iItem <= IDC_CONTROLLIMIT))	// A message from the toolbar button
	{
		bHandled = TRUE;
		try
		{
			// Get the control's element
			long	nIndex = pNM->iItem - IDC_CONTROLBASE;
			XmlElement	xmlControl = m_xmlControls->selectNodes(L"*")->item[nIndex];

			// React depending on the control type
			if(xmlControl->baseName == (_bstr_t)L"DropDownButton")	// An arrow part of the drop-down button has been clicked
			{
				// Button location in screen coordinates (as needed to show the context menu)
				CRect	rcButtonScreen = pNM->rcButton;
				ClientToScreen(&rcButtonScreen);

				// Show the context menu and achieve the result
				XmlElement xmlClicked = m_oActionManager->ShowPopupMenu(xmlControl, m_hWnd, CPoint(rcButtonScreen.left, rcButtonScreen.bottom), (IDispatch*)m_oBrowser);

				///////////
				// Switch the button's default control (if allowed for the button)
				bool	bSwitch = ((xmlControl->selectSingleNode(L"@Switch") != NULL) && ((long)xmlControl->getAttribute(L"Switch")));

				// If switching is allowed, there was some user's choice, the choice is an immediate child to the drop-down button, and it's not the currently-default item
				if((bSwitch) && (xmlClicked != NULL) && (xmlControl->selectSingleNode((_bstr_t)L"*[@EntryID='" + (_bstr_t)xmlClicked->getAttribute(L"EntryID") + L"']") != NULL) && (xmlControl->getAttribute(L"Default") != xmlClicked->getAttribute(L"EntryID")))
				{	// Set the executed action as a default
					// Refresh the ActionManager contents
					m_oActionManager->Load();

					// Do the necessary updates
					XmlElement	xmlControlsLive = m_oActionManager->ControlFamilyFromGuid(m_guidToolbar, L"Toolbar");
					xmlControlsLive->xml;
					XmlElement	xmlControlNew = m_oActionManager->ControlFromEntryID(xmlControl->getAttribute(L"EntryID"), xmlControlsLive, true);
					xmlControlNew->setAttribute(L"Default", xmlClicked->getAttribute(L"EntryID"));

					// Persist the changes
					m_oActionManager->Save();

					// Force updating of all the toolbars
					UpdateAllToolbars();
				}
			}
			else
				ThrowError(CJetIe::LoadString(IDS_E_CONTROLTYPE));

			UpdateControl(xmlControl, nIndex);	// Update the control state and appearance
		}
		catch(_com_error e)
		{
			CStringW	sError = COM_REASON(e);
			COM_TRACE();
			CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_E_CANNOTEXECUTEACTION) + L'\n' + sError, NULL, CPopupNotification::pmStop);
		}
	}

	return 0;
}

void CBand::PrepareButtonControl(XmlElement xmlAction, TBBUTTON &btn, CString &sCache)
{
	// Note that some general init is already done outside this function, uniformly for all the controls

	int	nIconIndex;
	HICON	hNormalIcon;

	// Control appearance (image/text)
	if(xmlAction->selectSingleNode(L"Style") == NULL)	// Style not specified, "image only" for the toolbar by default
	{
		btn.fsStyle = 0;
		btn.iBitmap = I_IMAGECALLBACK;	// This is a temporary value, means that an image should be loaded later
	}
	else	// TODO: local settings in the Control element should take precedence, if present
	{
		// Show image?
		if((long)(xmlAction->selectSingleNode(L"Style/@Image")->nodeValue))
			btn.iBitmap = I_IMAGECALLBACK;	// This is a temporary value, means that an image should be loaded later
		else
			btn.iBitmap = I_IMAGENONE;	// Image should not be loaded

		// Show text?
		if((long)(xmlAction->selectSingleNode(L"Style/@Text")->nodeValue))
			btn.fsStyle = BTNS_AUTOSIZE	| BTNS_SHOWTEXT;
		else
			btn.fsStyle = BTNS_AUTOSIZE;
	}

	// Load image, if needed
	if(btn.iBitmap == I_IMAGECALLBACK)	// Our flag, means need-to-load
	{
		// TODO: fill the imagelit, load the icon appropriate, control's setting should take precedence, if present
		if(xmlAction->selectSingleNode(L"Image") == NULL)	// No icon specified for this action
			btn.iBitmap = I_IMAGENONE;
		else
		{
			// Add the normal image (it must exist anyway)
			nIconIndex = ImageList_AddIcon(m_ilAllNormal, hNormalIcon = LoadResourceIcon((LPCTSTR)(_bstr_t)xmlAction->selectSingleNode(L"Image/@Normal")->nodeValue));	// Load and add to the image list
			btn.iBitmap = MAKELONG(nIconIndex, IDIL_ALL);	// Assign to the button

			// Add the hot image
			if(xmlAction->selectSingleNode(L"Image/@Hot") != NULL)
				ImageList_AddIcon(m_ilAllHot, LoadResourceIcon((LPCTSTR)(_bstr_t)xmlAction->selectSingleNode(L"Image/@Hot")->nodeValue));
			else
				ImageList_AddIcon(m_ilAllHot, hNormalIcon);	// If not specified, use a normal icon for that

			// Add the disabled image
			if(xmlAction->selectSingleNode(L"Image/@Disabled") != NULL)
				ImageList_AddIcon(m_ilAllDisabled, LoadResourceIcon((LPCTSTR)(_bstr_t)xmlAction->selectSingleNode(L"Image/@Disabled")->nodeValue));
			else
				ImageList_AddIcon(m_ilAllDisabled, hNormalIcon);	// If not specified, use a normal icon for that
		}
	}
	if((long)(xmlAction->selectSingleNode(L"Style/@Text")->nodeValue))
	{
		sCache = (LPCTSTR)(_bstr_t)xmlAction->selectSingleNode(L"Title/@Text")->nodeValue;	// Hold the buffer until we submit it
		btn.iString = (INT_PTR)(LPCTSTR)sCache;
	}
	else
		btn.iString = NULL;
}

void CBand::UpdateAllToolbars()
{
	TRACE(L"CBand is starting issuing an OnUpdateControls message.");
	// Find all the windows
	EnumWindows(EnumWindowsProc, m_nUpdateControlsMessage);
}

BOOL CALLBACK CBand::EnumWindowsProc(HWND hwnd, LPARAM lParam)
{
	// Send a message, if this is a suitable window
	CString	sClassName;
	if((GetClassName(hwnd, sClassName.GetBuffer(0x100), 0x100)) && (sClassName == m_sWindowClassName))	// Succeeded in getting the window class name and it conicides with the desired band window class name
	{
		TRACE(L"CBand is issuing an OnUpdateControls message to HWND %#010X.", (DWORD)(INT_PTR)hwnd);
		::PostMessage(hwnd, (UINT)lParam, 0, 0);
	}

	// Recurse to the child windows
	EnumChildWindows(hwnd, EnumChildWindowsProc, lParam);

	// Go on with enumeration
	return TRUE;
}

BOOL CALLBACK CBand::EnumChildWindowsProc(HWND hwnd, LPARAM lParam)
{
	// Send a message, if this is a suitable window
	CString	sClassName;
	if((GetClassName(hwnd, sClassName.GetBuffer(0x100), 0x100)) && (sClassName == m_sWindowClassName))	// Succeeded in getting the window class name and it conicides with the desired band window class name
	{
		TRACE(L"CBand is issuing an OnUpdateControls message to HWND %#010X.", (DWORD)(INT_PTR)hwnd);
		::PostMessage(hwnd, (UINT)lParam, 0, 0);
	}

	// We do not recurse to the child windows because they are already about to be handled by the parent search

	// Go on with enumeration
	return TRUE;
}

CString CBand::GetWindowClassName()
{
	return _T("JetIe.") + CJetIe::LoadStringT(IDS_PLUGIN_NAME) + _T(".RebarBand");
}

LRESULT CBand::OnUpdateControls(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = TRUE;

	TRACE(L"CBand has received an OnUpdateControls message via HWND %#010X.", m_hWnd);

	// First, reload the possible changes to the Action Manager
	m_oActionManager->Load();

	// Then, re-create the toolbar controls according to the new scheme
	CreateControls();

	return 0;
}

LRESULT CBand::OnMouseMove(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = FALSE;	// Allow base impl to process the mouse message

	// Update the controls on mouse-enter
	DWORD	dwTimeLimit = (::IsChild(m_hwndTopmostParent, ::GetFocus())) ? 500 : 5000;	// Minimum time between two subsequent updates, 500 ms for active window and 5000 for inactive
	if(GetTickCount() - m_dwLastUpdateControls >= dwTimeLimit)
		UpdateControls();	// m_dwLastUpdateControls will be updated within this function

	CPoint	pt(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));

	// Monitor the chevron state
	BOOL	bMouseOnChevron = ChevronBounds.PtInRect(pt);	// This also checks for the visibility
	if((bMouseOnChevron) && (m_nChevronState == csNormal))
	{
		m_nChevronState = csHovered;
		InvalidateRect(ChevronBounds, false);
	}
	else if((!bMouseOnChevron) && (m_nChevronState == csHovered))
	{
		m_nChevronState = csNormal;
		InvalidateRect(ChevronBounds, false);
	}
	bHandled = bMouseOnChevron;

	// Capture or uncapture the mouse
	// NOTE: capture causes the base button-pressing algorithm to fail …
	/*
	if(m_bMouseCaptured)
	{
		CRect	rc;
		GetClientRect(&rc);
		if(!rc.PtInRect(pt))
		{
			m_bMouseCaptured = false;
			ReleaseCapture();
		}
	}
	else
	{
		m_bMouseCaptured = true;
		SetCapture();
	}
	*/

	return 0;
}

LRESULT CBand::OnTimer(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = TRUE;	// Assume handled by default

	// TODO: improve?..
	// If the mouse is no more over the chevron, remove the hover state cues
	if(m_nChevronState == csHovered)
	{
		CPoint	pt;
		GetCursorPos(&pt);
		if(!ChevronBounds.PtInRect(pt))
		{
			m_nChevronState = csNormal;
			InvalidateRect(ChevronBounds, FALSE);
		}
	}

	// Dispatch the timer-handling
	switch(wParam)
	{
	case timerUpdateUI:
		OnTimerUpdateUI();
		break;
	default:	// No handler …
		bHandled = FALSE;
	}

	return 0;
}

void CBand::OnTimerUpdateUI()
{
	// Check if the current window has focus in one of its descendant childs
	if(::IsChild(m_hwndTopmostParent, ::GetFocus()))
		UpdateControls();
}

#ifndef IInputObjectSitePtr
_COM_SMARTPTR_TYPEDEF(IInputObjectSite, IID_IInputObjectSite);
#endif

LRESULT CBand::OnKillFocus(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = TRUE;

	// Notify the site about the focus loss
	IInputObjectSitePtr	oSite = (IUnknown*)m_spUnkSite;
	if(oSite != NULL)
		oSite->OnFocusChangeIS((IBand*)this, FALSE);

	return S_OK;
}

LRESULT CBand::OnSetFocus(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = TRUE;

	// Notify the site about the focus gain
	IInputObjectSitePtr	oSite = (IUnknown*)m_spUnkSite;
	if(oSite != NULL)
		oSite->OnFocusChangeIS((IBand*)this, TRUE);

	return S_OK;
}

bool CBand::GetChevronVisible()
{
	// Check if the last button fits within the window's client rectangle.
	CRect	rcClient;
	GetClientRect(&rcClient);

	CRect	rcLastButton;
	if(SendMessage(TB_GETITEMRECT, SendMessage(TB_BUTTONCOUNT, NULL, NULL) - 1, (LPARAM)(LPRECT)&rcLastButton))
		return !((rcClient.PtInRect(CPoint(rcLastButton.left, rcLastButton.top))) && (rcClient.PtInRect(CPoint(rcLastButton.right - 1, rcLastButton.bottom - 1))));

	return true;
}

LRESULT CBand::OnPaint(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	DefWindowProc();

	//PAINTSTRUCT	ps;

	////////////
	// Chevron
	if(ChevronVisible)
	{
		HDC	hdc = GetDC();//BeginPaint(&ps);

		CRect	rcChevron = ChevronBounds;

		// Background
		FillRect(hdc, &rcChevron, (HBRUSH)GetSysColorBrush(COLOR_BTNFACE));

		// Chevron Icon
		DrawIconEx(hdc, rcChevron.left + (rcChevron.Width() - 8) / 2, rcChevron.top + (rcChevron.Height() - 16) / 2, m_iconChevron, 8, 16, NULL, NULL, DI_IMAGE | DI_MASK);

		// Special decoration?
		HBRUSH	brushTopLeft = NULL, brushBottomRight = NULL;
		switch(m_nChevronState)
		{
		case csNormal:
			break;
		case csHovered:
			brushTopLeft = GetSysColorBrush(COLOR_3DHIGHLIGHT);
			brushBottomRight = GetSysColorBrush(COLOR_3DSHADOW);
			break;
		case csPressed:
			brushTopLeft = GetSysColorBrush(COLOR_3DSHADOW);
			brushBottomRight = GetSysColorBrush(COLOR_3DHIGHLIGHT);
			break;
		}
		if((brushTopLeft != 0) || (brushBottomRight != 0))
		{
			CRect	rc;
			rc.SetRect(rcChevron.left, rcChevron.top, rcChevron.right - 1, rcChevron.top + 1);
			FillRect(hdc, &rc, brushTopLeft);
			rc.SetRect(rcChevron.left, rcChevron.top, rcChevron.left + 1, rcChevron.bottom - 1);
			FillRect(hdc, &rc, brushTopLeft);
			rc.SetRect(rcChevron.right - 1, rcChevron.top, rcChevron.right, rcChevron.bottom);
			FillRect(hdc, &rc, brushBottomRight);
			rc.SetRect(rcChevron.left, rcChevron.bottom - 1, rcChevron.right, rcChevron.bottom);
			FillRect(hdc, &rc, brushBottomRight);
		}

		ReleaseDC(hdc);//EndPaint(&ps);
	}

	bHandled = TRUE;	// Called the underlying handler manually
	return 0;
}

CRect CBand::GetChevronBounds()
{
	if(!ChevronVisible)
		return CRect(0, 0, 0, 0);

	return GetDefaultChevronBounds();
}

CRect CBand::GetDefaultChevronBounds()
{
	CRect	rc;
	GetClientRect(&rc);
	rc.left = rc.right - CHEVRON_WIDTH >= 0 ? rc.right - CHEVRON_WIDTH : 0;

	return rc;
}

LRESULT CBand::OnLButtonDown(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = FALSE;

	CRect	rcChevron = ChevronBounds;
	if(rcChevron.PtInRect(CPoint(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam))))	// This also checks for the visibility
	{
		bHandled = TRUE;

		// Paint the chevron as pressed
		m_nChevronState = csPressed;
		InvalidateRect(ChevronBounds, false);

		// Prepare the rects to check if a particular button is visible or not
		CRect	rcButton, rcClient;
		GetClientRect(&rcClient);

		// Filter out those controls that should be displayed under the chevron (those that do not fit into the view)
		XmlElement	xmlTempRoot = m_xmlControls->ownerDocument->createElement(L"Controls");
		XmlNodeList	xmlControls = m_xmlControls->selectNodes(L"*");
		XmlElement	xmlControl;
		for(int nIndex = 0; ((xmlControl = xmlControls->nextNode()) != NULL); nIndex++)
		{
			// Get the rectangle for each button and check whether it fits or not, if not (or on rect-failure), add it to the chevron
			if((!SendMessage(TB_GETITEMRECT, nIndex, (LPARAM)(LPRECT)&rcButton)) || (!((rcClient.PtInRect(CPoint(rcButton.left, rcButton.top))) && (rcClient.PtInRect(CPoint(rcButton.right - 1, rcButton.bottom - 1))))))
				xmlTempRoot->appendChild(xmlControl->cloneNode(true));
		}

		// Chevron location in screen coordinates (as needed to show the context menu)
		CRect	rcChevronScreen = rcChevron;
		ClientToScreen(&rcChevronScreen);

		// Show the popup menu for the chevron
		m_oActionManager->ShowPopupMenu(xmlTempRoot, m_hWnd, CPoint(rcChevronScreen.left, rcChevronScreen.bottom), (IDispatch*)m_oBrowser);

		// Paint the chevron normally
		m_nChevronState = csNormal;
		InvalidateRect(ChevronBounds, false);
	}
	return 0;
}

LRESULT CBand::OnLButtonUp(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	bHandled = FALSE;

	// Catch all the mouse actions over the chevron, although there's no special action for that
	CRect	rcChevron = ChevronBounds;
	if(rcChevron.PtInRect(CPoint(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam))))	// This also checks for chevron visibility
		bHandled = TRUE;

	return 0;
}

LRESULT CBand::OnSizeOrSizing(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	// Force repaint of the chevron, as it's not handled by the toolbar's repainting code

	//CRect	rcChevron = GetDefaultChevronBounds();
	//rcChevron.left -= CHEVRON_WIDTH;
	//InvalidateRect(&rcChevron, false);
	InvalidateRect(NULL);

	bHandled = FALSE;	// Let toolbar play with it
	return 0;
}
