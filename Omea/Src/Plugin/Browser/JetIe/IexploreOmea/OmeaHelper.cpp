// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// OmeaHelper.cpp : Implementation of COmeaHelper
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "stdafx.h"
#include "OmeaHelper.h"

#include "..\JetIe.h"
#include "OmeaRequestQueue.h"

// COmeaHelper

COmeaHelper::COmeaHelper()
{
}

COmeaHelper::~COmeaHelper()
{
}

STDMETHODIMP COmeaHelper::SetSite(IUnknown* pUnkSite)
{
	// Store the pointer
	IObjectWithSiteImpl<COmeaHelper>::SetSite(pUnkSite);
	try
	{
		m_oBrowser = pUnkSite;
		TRACE(L"Omea Helper object site assigned to %#010X.", (DWORD)(INT_PTR)pUnkSite);
}
	COM_CATCH();

	// Invoke the following section only if this is startup, not shutdown
	if(m_oBrowser != NULL)
	{
		TRACE(L"Omea Helper object created, checking if the requests queue has to be processed.");

		// Update settings in the Registry
		COmeaSettingStore	settings;
		settings.RewriteSettings();

		// Submit requests from the queue, if any
		COmeaRequestQueue::BeginSubmitAttempts();

		// Remember the browser's top-level window for later use
		if(pUnkSite != NULL)	// Setting, not removing
		try
		{
			// Get the browser client window
			HWND	hwnd = CJetIe::WindowFromBrowser(m_oBrowser);

			// Go up to the top-level window
			while(GetParent(hwnd) != NULL)
				hwnd = GetParent(hwnd);

			// Store the top-level window
			//CJetIe::GetPlugin()->AppWindow = hwnd;	// TODO!
		}
		COM_CATCH();
	}
	else
		TRACE(L"Omea Helper object disconnected from site.");

	return S_OK;
}

// TODO: stop the queue timer when unloading!!!
// TODO: check what happens to the queue if one method is not found
