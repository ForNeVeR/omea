// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// MainToolbarButton.h : Declaration of the CMainToolbarButton
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once
#include "CommonResource.h"       // main symbols
#include "DynamicClassFactory.h"
#include "ActionManager.h"

// CMainToolbarButton
// TODO: suppres registration of this coclass
#ifdef JETIE_OMEA
[
	coclass,
	threading("apartment"),
	vi_progid("IexploreOmea.MainToolbarButton"),
	progid("IexploreOmea.MainToolbarButton.1"),
	version(1.0),
	uuid("4130B262-A577-44c9-A8D2-D59EA8824C40"),
	helpstring("Supports the generic Omea Internet Explorer Main Toolbar button command target object.")
]
#endif
#ifdef JETIE_BEELAXY
[
	coclass,
	threading("apartment"),
	vi_progid("IexploreBeelaxy.MainToolbarButton"),
	progid("IexploreBeelaxy.MainToolbarButton.1"),
	version(1.0),
	uuid("4130B262-A577-44c9-A8D2-D59EA8824C40"),
	helpstring("Supports the generic Beelaxy Internet Explorer Main Toolbar button command target object.")
]
#endif
class ATL_NO_VTABLE CMainToolbarButton :
	public IObjectWithSiteImpl<CMainToolbarButton>,
	public IOleCommandTarget
{
public:
	CMainToolbarButton();
	virtual ~CMainToolbarButton();

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	DECLARE_CLASSFACTORY_EX( CDynamicClassFactory );

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

protected:
	/// Holds the reference to the Web browser object representing the top-level window of the Web page to which this button applies.
	SHDocVw::IWebBrowser2Ptr	m_oBrowser;

	/// GUID of the UI Control that this control represents.
	GUID	m_guidControl;

	/// The action manager which controls the actions implemented by this button or menu item.
	IRawActionManagerPtr	m_oActionManager;

public:
	/// Assigns the UI Control's GUID value.
	void SetControlGuid(GUID &guidControl);

	// IOleCommandTarget
	STDMETHOD(QueryStatus)( const GUID *pguidCmdGroup, ULONG cCmds, OLECMD *prgCmds, OLECMDTEXT *pCmdText );
	STDMETHOD(Exec)( const GUID *pguidCmdGroup, DWORD nCmdID, DWORD nCmdExecOpt, VARIANTARG *pvaIn, VARIANTARG *pvaOut );

	// IObjectWithSite overloads
	STDMETHOD(SetSite)(IUnknown* pUnkSite);	// Extracts the WebBrowser object pointer from the client site
};
