/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// MainToolbarButton.cpp : Implementation of CMainToolbarButton
//
// A generic handle for Internet Explorer main toolbar button or Tools menu item actions.
// This class is instantiated by a dynamic class factory based on the information provided by the ActionManager.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "StdAfx.h"
#include "MainToolbarButton.h"

#include "JetIe.h"

// CMainToolbarButton
CMainToolbarButton::CMainToolbarButton()
{
	ASSERT(!(m_guidControl.Data1 = 0));

	m_oActionManager = CJetIe::GetActionManager();
}

CMainToolbarButton::~CMainToolbarButton()
{
	ASSERT(m_guidControl.Data1 != 0);	// Should have been assigned on a normal flow
}

void CMainToolbarButton::SetControlGuid(GUID &guidControl)
{
	ASSERT(m_guidControl.Data1 == 0);	// Must be unassigned by this time
	m_guidControl = guidControl;
}

/////////////////////////////////////////////////////////////////////////////
// IOleCommandTarget
STDMETHODIMP CMainToolbarButton::QueryStatus( const GUID *pguidCmdGroup, ULONG cCmds, OLECMD *prgCmds, OLECMDTEXT *pCmdText )
{
	ASSERT(m_guidControl.Data1 != 0);
	if(m_guidControl.Data1 == 0)
		return E_FAIL;

	ASSERT(m_oBrowser != NULL);
	if(m_oBrowser == NULL)
		return E_FAIL;

	try
	{
		// Mark all the commands as enabled
		bool	bReturnedText = false;	// This flag controls that we return the textual information only for the first command we support in the list, as required by the IOleCommandTarget::QueryStatus
		_bstr_t	bsCommandText;
		for(int a = 0; a < (int)cCmds; a++)
		{
			prgCmds->cmdf = OLECMDF_SUPPORTED | OLECMDF_ENABLED;

			// TODO: implement querying the command status
				
				/*m_action->GetFlags(m_oBrowser);	// Request the enabled/disabled etc state depending
			

			// Should we provide the additional command info? Prepare the text string
			if((!bReturnedText) && (pCmdText != NULL))
			{
				bReturnedText = true;
				try
				{
					if(pCmdText->cmdtextf == OLECMDTEXTF_NAME)	// Command Name
						bsCommandText = m_action->GetName(m_oBrowser);
					else if(pCmdText->cmdtextf == OLECMDTEXTF_STATUS)	// Status Bar Text
						bsCommandText = m_action->GetDescription(m_oBrowser);
					else
						bsCommandText = L"";
				}
				catch(_com_error e)
				{
					COM_TRACE();
					bsCommandText = L"";
				}

				// Supply the string
				StringCchCopyW(pCmdText->rgwz, pCmdText->cwBuf, (BSTR)bsCommandText);	// Copy characters (even if the function fails, the copy operation should succeed)
				pCmdText->cwActual = bsCommandText.length();
			}
			*/
		}
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CMainToolbarButton::Exec( const GUID *pguidCmdGroup, DWORD nCmdID, DWORD nCmdExecOpt, VARIANTARG *pvaIn, VARIANTARG *pvaOut )
{
	TRACE(L"Exec for command %d, option %d", nCmdID, nCmdExecOpt);

	ASSERT(m_guidControl.Data1 != 0);
	if(m_guidControl.Data1 == 0)
		return E_FAIL;

	ASSERT(m_oBrowser != NULL);
	if(m_oBrowser == NULL)
		return E_FAIL;

	try
	{
		// Get the control
		XmlElement	xmlControl = m_oActionManager->ControlFromGuid(m_guidControl);

		// Execute its action
		m_oActionManager->Execute2(xmlControl, (_variant_t)(IDispatch*)(IDispatchPtr)m_oBrowser);
	}
	catch(_com_error e)
	{
		CStringW	sErr = COM_REASON(e);
		COM_TRACE();
		CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_FAIL) + L'\n' + sErr, NULL, CPopupNotification::pmStop);
	}

	return S_OK;
}

STDMETHODIMP CMainToolbarButton::SetSite(IUnknown* pUnkSite)
{
	IObjectWithSiteImpl<CMainToolbarButton>::SetSite(pUnkSite);

	try
	{
		if(pUnkSite == NULL)	// Shutdown
			m_oBrowser = NULL;
		else	// Startup
		{
			// To retrieve the top-level IWebBrowser2 reference, get IServiceProvider from the client site and perform a QueryService for IID_IServiceProvider under the service SID_STopLevelBrowser (defined in Shlguid.h). From this second IServiceProvider, perform a QueryService for IID_IWebBrowser2 in the SID_SWebBrowserApp service.

			// The best place to perform this work is in the SetClientSite() method of IOleObject.

			IServiceProviderPtr	oSiteServiceProvider = (IUnknown*)m_spUnkSite;	// Site's Service Provider
			IServiceProviderPtr	oTopLevelWebBrowserServiceProvider;	// Service Provider of the Web browser object for the top-level frame window
			oSiteServiceProvider->QueryService(SID_STopLevelBrowser, IID_IServiceProvider, reinterpret_cast<void **>(&oTopLevelWebBrowserServiceProvider));	// Get it
			oTopLevelWebBrowserServiceProvider->QueryService(SID_SWebBrowserApp, IID_IWebBrowser2, reinterpret_cast<void **>(&m_oBrowser));	// Get the Web browser object interface of the top-level frame window
		}
	}
	COM_CATCH();

	return S_OK;
}
