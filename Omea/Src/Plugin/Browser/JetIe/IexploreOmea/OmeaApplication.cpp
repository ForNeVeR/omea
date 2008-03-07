/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// OmeaApplication.cpp : Implementation of COmeaApplication
//
// © JetBrains, Inc, 2005
// Written by (H) Serge Baltic

#include "stdafx.h"
#include "OmeaApplication.h"

#include "..\JetIe.h"
#include "OmeaOptionsDialog.h"
#include "OmeaRequest.h"

// COmeaApplication
COmeaApplication::COmeaApplication()
{
	TRACE(L"OmeaApplication ctor");
}

COmeaApplication::~COmeaApplication()
{
	TRACE(L"OmeaApplication dtor");
}

STDMETHODIMP COmeaApplication::SubscribeToFeed(BSTR URI)
{
	try
	{
		// Delegate processing to the request object
		IOmeaRequestPtr	oRequest(__uuidof(COmeaRequest));
		COM_CHECK(oRequest, put_Application(this));
		COM_CHECK(oRequest, put_Async(VARIANT_FALSE));	// Run sync
		COM_CHECK(oRequest, SubscribeToFeed(URI));
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP COmeaApplication::CreateClipping(BSTR Subject, BSTR Text, BSTR SourceURI, VARIANT Silent)
{
	try
	{
		// Delegate processing to the request object
		IOmeaRequestPtr	oRequest(__uuidof(COmeaRequest));
		COM_CHECK(oRequest, put_Application(this));
		COM_CHECK(oRequest, put_Async(VARIANT_FALSE));	// Run sync
		COM_CHECK(oRequest, CreateClipping(Subject, Text, SourceURI, Silent));
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP COmeaApplication::ShowOptionsDialog(VARIANT ParentWindow)
{
	try
	{
		// Show the WinAPI dialog
		COmeaOptionsDialog	dlgOptions;
		dlgOptions.DoModal(CJetIe::WindowFromVariant(ParentWindow));
	}
	COM_CATCH_RETURN();

	return S_OK;
}
