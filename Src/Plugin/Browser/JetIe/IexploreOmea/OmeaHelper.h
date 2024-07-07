// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// OmeaHelper.h : Declaration of the COmeaHelper
//
// The Internet Explorer helper object that is attached to IE when a window is created.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once
#include "resource.h"       // main symbols


// IOmeaHelper
[
	object,
	uuid("E2D79867-BF7D-4EA1-9243-AF56BA0C0F94"),
	dual,	helpstring("Interner Explorer Omea Add-on Browser Helper Object Interface"),
	pointer_default(unique)
]
__interface IOmeaHelper : IDispatch
{
};


// _IOmeaHelperEvents
[
	dispinterface,
	uuid("53C0C5A0-FC63-49BA-B640-606F1F61CECE"),
	helpstring("Interner Explorer Omea Add-on Browser Helper Object Events Interface")
]
__interface _IOmeaHelperEvents
{
};


// COmeaHelper

[
	coclass,
	threading("apartment"),
	support_error_info("IOmeaHelper"),
	event_source("com"),
	vi_progid("Omea.Helper"),
	progid("Omea.Helper.1"),
	version(1.0),
	uuid("09628AAA-66AD-4FA2-82E2-698185B66463"),
	helpstring("Interner Explorer Omea Add-on Browser Helper Object")
	//registration_script("Res/RegisterHkeyLocalMachine.rgs")
]
class ATL_NO_VTABLE COmeaHelper :
	public IObjectWithSiteImpl<COmeaHelper>,
	public IOmeaHelper
{
public:
	COmeaHelper();
	virtual ~COmeaHelper();

	__event __interface _IOmeaHelperEvents;

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	//DECLARE_REGISTRY_RESOURCEID(IDR_HELPER)

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

	/// Overloads the IObjectWithSite method.
	STDMETHOD(SetSite)(IUnknown* pUnkSite);


// Implementation
protected:
	SHDocVw::IWebBrowser2Ptr	m_oBrowser;

// Interface
public:
};

