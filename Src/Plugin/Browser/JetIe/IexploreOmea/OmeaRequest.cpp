// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// OmeaRequest.cpp : Implementation of COmeaRequest
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "StdAfx.h"
#include "OmeaRequest.h"

// COmeaRequest

COmeaRequest::COmeaRequest()
{
	m_nErrorCode = 0;
	m_pfnOnComplete = NULL;
	m_pfnOnError = NULL;

	m_bRunAsyncRequests = true;
	m_bAllowQueueing = true;
}

COmeaRequest::~COmeaRequest()
{
}

void COmeaRequest::Start(CStringA sOmeaMethod, CStringA sOmeaData, DelegateOnComplete pfnOnComplete, DelegateOnError pfnOnError)
{
	// Initialize the fields
	m_sOmeaMethod = sOmeaMethod;
	m_sOmeaData = sOmeaData;
	m_pfnOnComplete = pfnOnComplete;
	m_pfnOnError = pfnOnError;

	// Some cleanup
	m_nErrorCode = 0;

	// Start the request in asynchronous mode (parameters will be gotten via callbacks)
	InvokeServer(m_bRunAsyncRequests);
}

CStringA COmeaRequest::OnGetHostName()
{
	return m_settings.GetProfileStringA(COmeaSettingStore::setOmeaRemotingHost);
}

int COmeaRequest::OnGetPort()
{
	return m_settings.GetOmeaRemotingPortNumber();
}

CStringA COmeaRequest::OnGetObjectName()
{
	// Collect
	CStringA	sObjectName;
	sObjectName.Format("/%s/%s/%s", m_settings.GetOmeaRemotingSecurityKey(), m_settings.GetProfileStringA(COmeaSettingStore::setOmeaRemotingFormatter), m_sOmeaMethod);

	return sObjectName;
}

CStringA COmeaRequest::OnGetContentType()
{
	return "application/x-www-form-urlencoded";
}

void COmeaRequest::OnLockObject()
{
	AddRef();
}

void COmeaRequest::OnUnlockObject()
{
	Release();	// TODO: ensure it won't release prematurely
}

void COmeaRequest::OnComplete(XmlDocument xmlResponse)
{
	ASSERT((m_pfnOnComplete != NULL) && "No callback defined");
	if(m_pfnOnComplete != NULL)
		(this->*m_pfnOnComplete)(xmlResponse);
}

void COmeaRequest::OnError(CStringW sErrorMessage)
{
	ASSERT((m_pfnOnError != NULL) && "No callback defined");
	if(m_pfnOnError != NULL)
		(this->*m_pfnOnError)(sErrorMessage);
}

IStreamPtr COmeaRequest::OnPrepareDataToSend()
{
	IStreamPtr	oStream;

	// Create
	CHECK(CreateStreamOnHGlobal(NULL, TRUE, &oStream));

	// Populate with data
	ULONG	nWritten;
	COM_CHECK(oStream, Write((LPCSTR)m_sOmeaData, m_sOmeaData.GetLength(), &nWritten));
	CHECK((int)nWritten == m_sOmeaData.GetLength() ? S_OK : E_FAIL);

	// Rewind to the beginning of data
	LARGE_INTEGER	li;
	li.QuadPart = 0;
	oStream->Seek(li, STREAM_SEEK_SET, NULL);

	return oStream;
}

void COmeaRequest::OnValidateResponse(XmlDocument xmlResponse)
{
	XmlElement	xmlResult = xmlResponse->selectSingleNode(L"/result");

	if(xmlResult == NULL)
		ThrowError(CJetIe::LoadString(IDS_OMEA_INVALID_FORMAT));
	if((_bstr_t)xmlResult->getAttribute(L"status") != (_bstr_t)L"ok")
	{
		TRACE(L"Omea has reported an error in its response.");
		if((_bstr_t)xmlResult->getAttribute(L"status") == (_bstr_t)L"exception")
		{
			// Save the exception code as an error code
			TRACE(L"Omea has reported an error in its response, and the error has a status.");
			m_nErrorCode = (int)(_variant_t)xmlResponse->selectSingleNode(L"/result/struct[@name='exception']/int[@name='code']")->text;
			ThrowError(CJetIe::LoadString(IDS_OMEA_EXCEPTION) + L'\n' + (LPCWSTR)(_bstr_t)xmlResponse->selectSingleNode(L"/result/struct[@name='exception']/string[@name='message']")->text);
		}
		ThrowError(CJetIe::LoadString(IDS_OMEA_EXCEPTION));
	}
}

bool COmeaRequest::OnWhetherCloseConnection()
{
	return true;	// Always close the connection
}

bool COmeaRequest::OnWhetherInvokeAgain()
{
	return false;	// No double-requests
}

void COmeaRequest::ThrowError(CStringW sError)
{
	TRACE(L"" + sError + L'\n');
	_com_issue_errorex(Error(sError), static_cast<IOmeaRequest*>(this), __uuidof(IOmeaRequest));
}

STDMETHODIMP COmeaRequest::SubscribeToFeed(BSTR URI)
{
	try
	{
		Start("RSSPlugin.SubscribeToFeed.1", (CStringA)"url=" + CJetIe::UrlEncode((LPCWSTR)(_bstr_t)URI), &COmeaRequest::OnSyndicateComplete, &COmeaRequest::OnFailEnqueue);
	}
	COM_CATCH_RETURN();

	return S_OK;
}

void COmeaRequest::OnSyndicateComplete(XmlDocument xmlResponse)
{
	// Show success notification, if allowed
	if(m_settings.GetProfileInt(COmeaSettingStore::setShowSuccessNotifications))
		CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_SYNDICATE_OK));

	// As a request has succeeded, check if there's something else that could be submitted just now
	COmeaRequestQueue::SubmitQueue();
}

void COmeaRequest::OnFailEnqueue(CStringW sErrorMessage)
{
	// TODO: determine the cause of request failure, check the error code obtained from Omea

	// Try to queue the request; if prohibited, a popup will be shown
	COmeaRequestQueue::EnqueueRequest(m_sOmeaMethod, m_sOmeaData);
}

void COmeaRequest::OnFailDontQueue(CStringW sErrorMessage)
{
	CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_FAIL_OMEA) + L'\n' + sErrorMessage, NULL, CPopupNotification::pmStop);
}

STDMETHODIMP COmeaRequest::CreateClipping(BSTR Subject, BSTR Text, BSTR SourceURI, VARIANT Silent)
{
	// Extract the value of the Silent parameter
	bool	bSilent = false;
	if(V_IS_MISSING(&Silent))	// The optional parameter is missing
		bSilent = false;
	else	// Convert to Boolean and take the value
	{
		try
		{
			bSilent = (bool)(_variant_t)Silent;
		}
		catch(_com_error e)
		{
			COM_TRACE();
			return Error(CJetIe::LoadString(IDS_E_INVALIDARG_SILENT));
		}
	}

	// Prepare the request data
	CStringA	sRequest = "";
	sRequest += "subject=" + CJetIe::UrlEncode(Subject) + "&";
	sRequest += "text=" + CJetIe::UrlEncode(Text) + "&";
	sRequest += "sourceUrl=" + CJetIe::UrlEncode(SourceURI);

	try
	{
		if(bSilent)
			Start("Omea.CreateClippingSilent.1", sRequest, &COmeaRequest::OnClipSilentComplete, &COmeaRequest::OnFailEnqueue);
		else
			Start("Omea.CreateClipping.1", sRequest, &COmeaRequest::OnClipComplete, &COmeaRequest::OnFailEnqueue);
	}
	COM_CATCH_RETURN();

	return S_OK;
}

void COmeaRequest::OnClipComplete(XmlDocument xmlResponse)
{
	// Show success notification, if allowed
	if(m_settings.GetProfileInt(COmeaSettingStore::setShowSuccessNotifications))
		CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_CLIP_OK));

	// As a request has succeeded, check if there's something else that could be submitted just now
	COmeaRequestQueue::SubmitQueue();
}

void COmeaRequest::OnClipSilentComplete(XmlDocument xmlResponse)
{
	// Show success notification always
	CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_CLIP_OK));

	// As a request has succeeded, check if there's something else that could be submitted just now
	COmeaRequestQueue::SubmitQueue();
}

STDMETHODIMP COmeaRequest::Annotate(BSTR URI, BSTR Title)
{
	try
	{
		Start("Favorites.AnnotateWeblink.1", (CStringA)"url=" + CJetIe::UrlEncode((LPCWSTR)(_bstr_t)URI) + "&title=" + CJetIe::UrlEncode((LPCWSTR)(_bstr_t)Title), &COmeaRequest::OnAnnotateComplete, &COmeaRequest::OnFailEnqueue);
	}
	COM_CATCH_RETURN();

	return S_OK;
}

void COmeaRequest::OnAnnotateComplete(XmlDocument xmlResponse)
{
	// Show success notification, if allowed
	if(m_settings.GetProfileInt(COmeaSettingStore::setShowSuccessNotifications))
		CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_ANNOTATION_OK));

	// As a request has succeeded, check if there's something else that could be submitted just now
	COmeaRequestQueue::SubmitQueue();
}

STDMETHODIMP COmeaRequest::Action_Syndicate_Exec(VARIANT WebBrowser)
{
	_bstr_t	bsUri;

	// Take 1: if an element with focus is a link, then its target should be used for subscription, instead of the page url
	try
	{
		SHDocVw::IWebBrowser2Ptr	oBrowser = (_variant_t)WebBrowser;
		BSTR	bstr;

		MSHTMLLite::IHTMLDocument2Ptr	oDoc = oBrowser->Document;
		CHECK(oDoc != NULL ? S_OK : E_FAIL);

		MSHTMLLite::IHTMLElementPtr	oActive;
		COM_CHECK(oDoc, get_activeElement(&oActive));
		CHECK(oActive != NULL ? S_OK : E_FAIL);

		bstr = NULL;
		COM_CHECK(oActive, get_tagName(&bstr));
		_bstr_t	bsTagName(bstr, false);	// Take ownership and free on falling off scope

		if((bsTagName == (_bstr_t)L"A") || (bsTagName == (_bstr_t)L"a"))	// An anchor
		{
			_variant_t	vtHref;
			COM_CHECK(oActive, getAttribute(L"href", 0, &vtHref));
			bsUri = vtHref;
		}

		TRACE(L"Url for subscription was taken from the target of the active element on the page: \"%s\".\n", (LPCWSTR)bsUri);
	}
	COM_CATCH_SILENT();

	// Take 2: use page's URL for subscription
	if(bsUri.length() == 0)	// Was not filled on the prev step
	{
		try
		{
			SHDocVw::IWebBrowser2Ptr	oBrowser = (_variant_t)WebBrowser;
			bsUri = oBrowser->LocationURL;
			TRACE(L"Url for subscription was taken from the page address: \"%s\".\n", (LPCWSTR)bsUri);
		}
		COM_CATCH();
	}

	// If no URL was obtained, show an error message and exit
	if(bsUri.length() == 0)
	{
		TRACE(L"no URI has been obtained to be used as a page address.");
		CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_SYNDICATE_NO_URI), NULL, CPopupNotification::pmStop);
		return S_OK;	// Don't throw to the action executioner
	}

	// Now try to use the obtained URI
	try
	{
		COM_CHECK((IOmeaRequest*)this, SubscribeToFeed(bsUri));
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP COmeaRequest::Action_Clip_Exec(VARIANT WebBrowser)
{
	return ClipActionExecImpl(WebBrowser, FALSE);
}

STDMETHODIMP COmeaRequest::Action_ClipSilent_Exec(VARIANT WebBrowser)
{
	return ClipActionExecImpl(WebBrowser, TRUE);
}

STDMETHODIMP COmeaRequest::ClipActionExecImpl(VARIANT WebBrowser, BOOL bSilent)
{
	try
	{
		SHDocVw::IWebBrowser2Ptr	oBrowser = (_variant_t)WebBrowser;

		// Retrieve the selected text
		MSHTMLLite::IHTMLDocument2Ptr	oDoc = oBrowser->Document;
		if(oDoc == NULL)
		{	// Not an HTML document currently opened in the window
			CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_CLIP_NO_TEXT), NULL, CPopupNotification::pmStop);
			return S_FALSE;
		}	// TODO: show a message that cannot apply to non-HTML … or try to do something?..

		// First, try to get the selection, if none available, test the whole document
		CStringW	sSelection;
		if(!CJetIe::GetSelectedText(oDoc, NULL, &sSelection))	// Nothing has been selected, take the whole document
		{
			BSTR	bstrSelection = 0;
			MSHTMLLite::IHTMLElementPtr	oElem;
			COM_CHECK(oDoc, get_body(&oElem));
			MSHTMLLite::IHTMLBodyElementPtr	oBody = oElem;
			MSHTMLLite::IHTMLTxtRangePtr	oRange;
			COM_CHECK(oBody, createTextRange((MSHTMLLite::IHTMLTxtRange**)&oRange));
			COM_CHECK(oRange, get_htmlText(&bstrSelection));
			_bstr_t bsSelection(bstrSelection, false);

			if((BSTR)bsSelection == NULL)	// No text in the whole document
			{
				CJetIe::ShowPopupNotification(CJetIe::LoadString(IDS_CLIP_NO_TEXT), NULL, CPopupNotification::pmStop);
				return S_FALSE;
			}
			else
				sSelection = (LPCWSTR)bsSelection;
		}

		// Issue the request
		COM_CHECK((IOmeaRequest*)this, CreateClipping(oBrowser->LocationName, (_bstr_t)(LPCWSTR)sSelection, oBrowser->LocationURL, (_variant_t)(!!bSilent)));	// TODO: decode the URL before passing it
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP COmeaRequest::Action_OmeaOptions_Exec(VARIANT WebBrowser)
{
	HWND	hwndParent = NULL;

	try
	{
		SHDocVw::IWebBrowser2Ptr	oBrowser;
		try { oBrowser = (_variant_t)WebBrowser; } COM_CATCH_SILENT();	// NULL if unavailable

		hwndParent = oBrowser != NULL ? CJetIe::WindowFromBrowser(oBrowser) : NULL;	// If executed from a browser, take its window as a parent for the dialog

		// Call Application for the dialog
		IApplicationPtr	oApp;
		COM_CHECK((IOmeaRequest*)this, get_Application(&oApp));
		COM_CHECK(oApp, ShowOptionsDialog((_variant_t)(long)(INT_PTR)hwndParent));
	}
	catch(_com_error e)
	{
		CString	sErr = COM_REASON_T(e);
		COM_TRACE();
		::MessageBox(hwndParent, CJetIe::LoadStringT(IDS_FAIL) + _T("\n") + sErr, CJetIe::LoadStringT(IDS_TITLE), MB_OK | MB_ICONSTOP);
	}

	return S_OK;
}

STDMETHODIMP COmeaRequest::Action_Annotate_Exec(IDispatch* Browser)
{
	try
	{
		SHDocVw::IWebBrowser2Ptr	oBrowser = Browser;
		COM_CHECK((IOmeaRequest*)this, Annotate(oBrowser->LocationURL, oBrowser->LocationName));
	}
	COM_CATCH();

	return S_OK;
}

STDMETHODIMP COmeaRequest::get_Async(VARIANT_BOOL *pVal)
{
	if(pVal == NULL)
		return E_POINTER;
	*pVal = m_bRunAsyncRequests ? VARIANT_TRUE : VARIANT_FALSE;
	return S_OK;
}

STDMETHODIMP COmeaRequest::put_Async(VARIANT_BOOL newVal)
{
	m_bRunAsyncRequests = newVal != VARIANT_FALSE;
	return S_OK;
}

STDMETHODIMP COmeaRequest::get_Application(IApplication **pVal)
{
	// If this request was not explicitly invoked by a client and the client object has not been set, create a new one
	if(m_oApplication == NULL)
		m_oApplication.CreateInstance(__uuidof(COmeaApplication));
	if(m_oApplication == NULL)
	{
		ASSERT(FALSE && "Could not create the Omea Application object.");
		*pVal = NULL;
		return E_FAIL;
	}
	return m_oApplication->QueryInterface(__uuidof(IApplication), (void**)pVal);
}

STDMETHODIMP COmeaRequest::put_Application(IApplication *newVal)
{
	if(newVal == NULL)
		return E_POINTER;
	m_oApplication = newVal;

	return S_OK;
}

STDMETHODIMP COmeaRequest::get_AllowQueueing(VARIANT_BOOL *pVal)
{
	if(pVal == NULL)
		return E_POINTER;
	*pVal = m_bAllowQueueing ? VARIANT_TRUE : VARIANT_FALSE;
	return S_OK;
}

STDMETHODIMP COmeaRequest::put_AllowQueueing(VARIANT_BOOL newVal)
{
	m_bAllowQueueing = newVal != VARIANT_FALSE;
	return S_OK;
}

STDMETHODIMP COmeaRequest::SubmitRequestQueue()
{
	try
	{
		// Ping Omea
		Start("System.ListAllMethods", "", &COmeaRequest::OnPingOmeaComplete, &COmeaRequest::OnFailNop);
	}
	COM_CATCH_RETURN();

	return S_OK;
}

void COmeaRequest::OnPingOmeaComplete(XmlDocument xmlResponse)
{
	xmlResponse = NULL;	// Don't need it, release the reference

	// Try submitting the queue
	COmeaRequestQueue::SubmitQueueImpl();
}

STDMETHODIMP COmeaRequest::SubmitRequest(BSTR MethodName, BSTR Parameters)
{
	try
	{
		Start((LPCSTR)(_bstr_t)MethodName, (LPCSTR)(_bstr_t)Parameters, &COmeaRequest::OnCompleteNop, &COmeaRequest::OnFailNop);
	}
	COM_CATCH_RETURN();

	return S_OK;
}

void COmeaRequest::OnCompleteNop(XmlDocument xmlResponse)
{
	// Do nothing (NOP)
}

void COmeaRequest::OnFailNop(CStringW sErrorMessage)
{
	// Do nothing (NOP)
}
