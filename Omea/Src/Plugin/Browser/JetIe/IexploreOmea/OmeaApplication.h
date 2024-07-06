// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// OmeaApplication.h : Declaration of the COmeaApplication
// A CJetRpcClient-derived class that implements requests to Omea,
// as well as some additional logics for queueing the requests,
// and serves as a target for UI action handlers.
//
// Note that this object should be used for making one request only.
// Create a new object for each request.
//
// © JetBrains, Inc, 2005
// Written by (H) Serge Baltic

#pragma once
#include "resource.h"       // main symbols


// IApplication
[
	object,
	uuid("BC3BC397-B29E-498C-BFB4-17E8E7D0FA43"),
	dual,	helpstring("IApplication Interface"),
	pointer_default(unique)
]
__interface IApplication : IDispatch
{
	[id(1), helpstring("Invokes subscription to a feed in Omea.")]
	HRESULT SubscribeToFeed([in] BSTR URI);

	[id(2), helpstring("Creates a clipping in Omea. Setting Silent parameter to True means that no UI should be displayed for the clipping creation. The default is False.")]
	HRESULT CreateClipping([in] BSTR Subject, [in] BSTR Text, [in] BSTR SourceURI, [in, optional] VARIANT Silent);

	[id(3), helpstring("Shows the UI for editing the IexploreOmea plugin options.")]
	HRESULT ShowOptionsDialog(VARIANT ParentWindow);
};

_COM_SMARTPTR_TYPEDEF(IApplication, __uuidof(IApplication));

// _IApplicationEvents
[
	dispinterface,
	uuid("CD7B82DF-47C5-4C01-A1EF-159184707A76"),
	helpstring("_IApplicationEvents Interface")
]
__interface _IApplicationEvents
{
};


// COmeaApplication

[
	coclass,
	threading("apartment"),
	support_error_info("IApplication"),
	event_source("com"),
	vi_progid("Omea.Application"),
	progid("Omea.Application.1"),
	version(1.0),
	uuid("D0BE79E9-DDCA-4CA8-AFE2-CC856B6A3D0D"),
	helpstring("Omea Application Class")
]
class ATL_NO_VTABLE COmeaApplication :
	public IApplication
{
public:
	COmeaApplication();
	virtual ~COmeaApplication();

	__event __interface _IApplicationEvents;

	DECLARE_PROTECT_FINAL_CONSTRUCT()

// Component categories
BEGIN_CATEGORY_MAP(COmeaApplication)
    IMPLEMENTED_CATEGORY(CATID_SafeForInitializing)
END_CATEGORY_MAP()

	HRESULT FinalConstruct()
	{
		TRACE(L"Application FinalConstruct");
		return S_OK;
	}

	void FinalRelease()
	{
		TRACE(L"Application FinalRelease");
	}

// Implementation
protected:

// Interface
public:
	STDMETHOD(SubscribeToFeed)(BSTR URI);
	STDMETHOD(CreateClipping)(BSTR Subject, BSTR Text, BSTR SourceURI, VARIANT Silent);
	STDMETHOD(ShowOptionsDialog)(VARIANT ParentWindow);
};
