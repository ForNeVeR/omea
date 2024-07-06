// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "StdAfx.h"
#include "DynamicClassFactory.h"

#include "MainToolbarButton.h"
#include "Band.h"
#include "JetIe.h"

CDynamicClassFactory::CDynamicClassFactory()
{
	ASSERT(!(m_clsid.Data1 = 0));	// Just make some DEBUG assignment to ensure we won't work with a hollow CLSID
}

CDynamicClassFactory::~CDynamicClassFactory()
{
	ASSERT( m_clsid.Data1 != 0 );	// Must be non-empty
}

_COM_SMARTPTR_TYPEDEF(IBand, __uuidof(IBand));

STDMETHODIMP CDynamicClassFactory::CreateInstance( LPUNKNOWN pUnkOuter, REFIID riid, void** ppvObj )
{
	ASSERT( m_clsid.Data1 != 0 );	// Must be non-empty

	// Try to instantiate an IE Tools Menu item control or IE Main Toolbar button control
	try
	{
		/*
		// Create the class. We cannot call the base implementation because as this class has not been created from a COM object, it has no creator function assigned
		IOleCommandTargetPtr	oUnk;	// Get a reference to be released automatically
		CHECK(CComCreator< CComObject< CMainToolbarButton > >::CreateInstance(pUnkOuter, __uuidof(IOleCommandTarget), (void**)&oUnk	));
		oUnk->AddRef();	// TODO: unneeded?..

		// Pass the control GUID
		((CMainToolbarButton*)(IOleCommandTarget*)oUnk)->SetControlGuid(m_clsid);

		// Here we add one more reference (released by the caller), and releasing our oUnk won't drop the object
		return oUnk->QueryInterface(riid, (void**)ppvObj);

		CJetIe::GetActionManager()->ControlFromGuid(m_clsid);	// Ensure that the CLSID passed into this function actually identifies a valid control
		CHECK(CComCreator< CComObject< CMainToolbarButton > >::CreateInstance(pUnkOuter, riid, (void**)ppvObj));
		// Assign it the control GUID
		static_cast<CMainToolbarButton*>((IOleCommandTarget*)(IUnknown*)(*ppvObj))->SetControlGuid(m_clsid);
		return S_OK;
		*/

		CJetIe::GetActionManager()->ControlFromGuid(m_clsid);	// Ensure that the CLSID passed into this function actually identifies a valid control

		// Create the class. We cannot call the base implementation because as this class has not been created from a COM object, it has no creator function assigned
		IOleCommandTarget	*pUnk = NULL;
		CHECK(CComCreator< CComObject< CMainToolbarButton > >::CreateInstance(pUnkOuter, __uuidof(IOleCommandTarget), (void**)&pUnk));
		IOleCommandTargetPtr	oUnk = pUnk;	// get a reference and hold it until the function exist, free it in case of a failure

		// Pass the control GUID
		((CMainToolbarButton*)(IOleCommandTarget*)oUnk)->SetControlGuid(m_clsid);

		// Here we add one more reference (released by the caller), and releasing our oUnk won't drop the object
		return oUnk->QueryInterface(riid, (void**)ppvObj);

		// TODO: check what happens if we throw, who frees the instance?
	}
	COM_CATCH();

	// Try to instantiate a custom toolbar
	try
	{
		XmlElement	xmlControlFamily = CJetIe::GetActionManager()->ControlFamilyFromGuid(m_clsid, L"Toolbar");	// Ensure that the CLSID passed into this function actually identifies a valid toolbar

		if((_bstr_t)xmlControlFamily->getAttribute(L"Type") == (_bstr_t)L"Toolbar")
		{
			/*
			// Instantiate
			IBandPtr	oBand;	// Get a reference and hold it until the function exist, free it in case of a failure
			CHECK(CComCreator< CComObject< CBand > >::CreateInstance(pUnkOuter, __uuidof(IBand), (void**)&oBand));
			oBand->AddRef();	// TODO: unneeded?

			// Pass the toolbar GUID
			((CBand*)(IBand*)oBand)->SetToolbarGuid(m_clsid);

			return oBand->QueryInterface(riid, (void**)ppvObj);

			CHECK(CComCreator< CComObject< CBand > >::CreateInstance(pUnkOuter, riid, (void**)ppvObj));
			static_cast<CBand*>((IBand*)(IUnknown*)*ppvObj)->SetToolbarGuid(m_clsid);
			return S_OK;*/

			// Instantiate
			/*CHECK(CComCreator< CComObject< CBand > >::CreateInstance( pUnkOuter, riid, ppvObj ));

			// Pass the toolbar GUID
			static_cast<CBand*>(*ppvObj)->SetToolbarGuid(m_clsid);

			return S_OK;
			*/

			IBand	*pBand = NULL;
			CHECK(CComCreator< CComObject< CBand > >::CreateInstance(pUnkOuter, __uuidof(IBand), (void**)&pBand));
			IBandPtr	oBand = pBand;	// get a reference and hold it until the function exist, free it in case of a failure

			((CBand*)(IBand*)oBand)->SetToolbarGuid(m_clsid);

			return oBand->QueryInterface(riid, (void**)ppvObj);
		}
	}
	COM_CATCH();

	return E_FAIL;
}

void CDynamicClassFactory::SetClsid( CLSID clsid )
{
	ASSERT( m_clsid.Data1 == 0 );	// Must be empty
    m_clsid = clsid;
}
