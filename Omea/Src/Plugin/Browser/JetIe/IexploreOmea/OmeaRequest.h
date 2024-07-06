// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// OmeaRequest.h : Declaration of the COmeaRequest
// Implements the Omea Remoting client.
// Also serves as the UI actions execution handler.
//
// One instance of this class should be used for making only one request.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once
#include "resource.h"       // main symbols

#include "..\JetRpcClient.h"
#include "OmeaSettingStore.h"
#include "OmeaApplication.h"
#include "OmeaRequestQueue.h"

// IOmeaRequest
[
	object,
	uuid("0801C705-F0CE-434E-85B0-330FF1A3C8C4"),
	dual,	helpstring("Omea RPC Request Interface"),
	pointer_default(unique)
]
__interface IOmeaRequest : IDispatch
{
	[id(1), helpstring("Invokes subscription to a feed in Omea.")]
	HRESULT SubscribeToFeed([in] BSTR URI);

	[id(2), helpstring("Creates a clipping in Omea. Setting Silent parameter to True means that no UI should be displayed for the clipping creation. The default is False.")]
	HRESULT CreateClipping([in] BSTR Subject, [in] BSTR Text, [in] BSTR SourceURI, [in, optional] VARIANT Silent);

	[id(3), helpstring("Opens the annotation/categorization UI in Omea for the given URI.")]
	HRESULT Annotate([in] BSTR URI, [in] BSTR Title);

	[id(4), propget, helpstring("Tells whether the following requests will be run in async or sync fashion.")]
	HRESULT Async([out, retval] VARIANT_BOOL *pVal);
	[id(4), propput, helpstring("Tells whether the following requests will be run in async or sync fashion.")]
	HRESULT Async([in] VARIANT_BOOL newVal);

	[propget, id(5), helpstring("Parent Omea Application that is in charge of this request object.")]
	HRESULT Application([out, retval] IApplication **pVal);
	[propput, id(5), helpstring("Parent Omea Application that is in charge of this request object.")]
	HRESULT Application([in] IApplication *newVal);

	[propget, id(6), helpstring("Enables or disables sotring the particular requests in the request queue in case they cannot reach Omea.")]
	HRESULT AllowQueueing([out, retval] VARIANT_BOOL *pVal);
	[propput, id(6), helpstring("Enables or disables sotring the particular requests in the request queue in case they cannot reach Omea.")]
	HRESULT AllowQueueing([in] VARIANT_BOOL newVal);

	[id(7), helpstring("Checks whether Omea is running and ready for processing the requests, and, if yes, submits the request queue.")]
	HRESULT SubmitRequestQueue();

	[id(8), helpstring("Submits a generic Omea request. Parameters MUST be url-encoded.")]
	HRESULT SubmitRequest([in] BSTR MethodName, [in] BSTR Parameters);


	// UI Action handlers
	[id(128), hidden, helpstring("Handler for the Syndicate UI Action execution.")]
	HRESULT Action_Syndicate_Exec([in, optional] VARIANT WebBrowser);

	[id(129), hidden, helpstring("Handler for the Clip UI Action execution.")]
	HRESULT Action_Clip_Exec([in] VARIANT WebBrowser);

	[id(130), hidden, helpstring("Handler for the OmeaOptions UI Action execution.")]
	HRESULT Action_OmeaOptions_Exec([in] VARIANT WebBrowser);

	[id(131), hidden, helpstring("Handler for the ClipSilent UI Action execution.")]
	HRESULT Action_ClipSilent_Exec([in] VARIANT WebBrowser);

	[id(132), hidden, helpstring("Handler for the Annotate UI Action execution.")]
	HRESULT Action_Annotate_Exec([in] IDispatch* Browser);
};

_COM_SMARTPTR_TYPEDEF(IOmeaRequest, __uuidof(IOmeaRequest));

// _IOmeaRequestEvents
[
	dispinterface,
	uuid("F108658F-4CE8-42B5-94BF-17D70203B6DE"),
	helpstring("Omea RPC Request Events Interface")
]
__interface _IOmeaRequestEvents
{
};

// COmeaRequest

[
	coclass,
	threading("apartment"),
	support_error_info("IOmeaRequest"),
	event_source("com"),
	vi_progid("Omea.Request"),
	progid("Omea.Request.1"),
	version(1.0),
	uuid("348584DD-2CDE-4A2C-99AF-E303BF608D5E"),
	helpstring("Omea RPC Request")
]
class ATL_NO_VTABLE COmeaRequest :
	public IOmeaRequest,
	public CJetRpcClient
{
public:
	COmeaRequest();
	virtual ~COmeaRequest();


	__event __interface _IOmeaRequestEvents;

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

// Implementation
protected:
	/// The setting store.
	COmeaSettingStore	m_settings;

	/// Method to be remotely called in Omea.
	CStringA	m_sOmeaMethod;

	/// Data which should be sent to Omea. As it must be url-encoded, there's no use in a Unicode string, and finally it will be sent out as ANSI.
	CStringA	m_sOmeaData;

	/// Error code, reported by Omea.
	int	m_nErrorCode;

	// Type definitions for delegates that are invoked when a request completes or fails.
	typedef void (COmeaRequest::*DelegateOnComplete)(XmlDocument xmlResponse);
	typedef void (COmeaRequest::*DelegateOnError)(CStringW sErrorMessage);

	/// Handler that should be called when a request completes successfully.
	DelegateOnComplete	m_pfnOnComplete;

	/// Handler that should be called when a request fails due to an error.
	DelegateOnError	m_pfnOnError;

	/// A field that identifies whether the new requests should be started in an async fashion. True by default.
	/// Note that the inherited m_bAsync and GetAsync members indicate the actual state of the currently-running requests and are unspecified until the request starts, while this field rules the future request mode.
	bool	m_bRunAsyncRequests;

	/// Pointer to the "parent" Omea application object.
	/// Should be accessed thru get_Application, as this property getter creates the default object if it is not set.
	IApplicationPtr	m_oApplication;

	/// Determines whether queueing of requests is allowed.
	/// If we're trying to resubmit a failed request, this MUST be set to FALSE to prevent the same request from falling into the queue again and again.
	bool	m_bAllowQueueing;

// Internal operations
protected:

	/// Fills the involved internal fields and initiates the asynchronous request.
	void Start(CStringA sOmeaMethod, CStringA sOmeaData, DelegateOnComplete pfnOnComplete, DelegateOnError pfnOnError) throw(_com_error);

	/// Implementation of the clipping action, either silent or not.
	STDMETHOD(ClipActionExecImpl)(VARIANT WebBrowser, BOOL bSilent);

// Overloads for the request infrastructure (see CJetRpcClient for the comments)
protected:
	virtual CStringA OnGetHostName();
	virtual int OnGetPort();
	virtual CStringA OnGetObjectName();
	virtual CStringA OnGetContentType();
	virtual void OnLockObject();
	virtual void OnUnlockObject();
	virtual void OnComplete(XmlDocument xmlResponse);
	virtual void OnError(CStringW sErrorMessage);
	virtual IStreamPtr OnPrepareDataToSend();
	virtual void OnValidateResponse(XmlDocument xmlResponse);
	virtual bool OnWhetherCloseConnection();
	virtual bool OnWhetherInvokeAgain();

	virtual void ThrowError(CStringW sError) throw(_com_error);

// Callbacks for the completed/failed requests
protected:
	void OnFailEnqueue(CStringW sErrorMessage);	// A queueable request has failed. Check if the failure reason prevents from queueing and whether user has disabled queueing.
	void OnFailDontQueue(CStringW sErrorMessage);	// A non-queueable request has failed. Report failure to the user.
	void OnFailNop(CStringW sErrorMessage);	// Does nothing on error.
	void OnSyndicateComplete(XmlDocument xmlResponse);	// Subscribed to feed OK.
	void OnClipComplete(XmlDocument xmlResponse);	// Created a clipping and opened it for editing.
	void OnClipSilentComplete(XmlDocument xmlResponse);	// Created a clipping.
	void OnAnnotateComplete(XmlDocument xmlResponse);	// Opened for annotation in Omea.
	void OnPingOmeaComplete(XmlDocument xmlResponse);	// A ping request indicated presence and readiness of Omea. Can submit the requests queue.
	void OnCompleteNop(XmlDocument xmlResponse);	// Does nothing on completion.

// Interface
public:
	// UI Actions
	STDMETHOD(Action_Syndicate_Exec)(VARIANT WebBrowser);
	STDMETHOD(Action_Clip_Exec)(VARIANT WebBrowser);
	STDMETHOD(Action_OmeaOptions_Exec)(VARIANT WebBrowser);
	STDMETHOD(Action_ClipSilent_Exec)(VARIANT WebBrowser);
	STDMETHOD(Action_Annotate_Exec)(IDispatch* Browser);

	// Methods of this object
	STDMETHOD(SubscribeToFeed)(BSTR URI);
	STDMETHOD(CreateClipping)(BSTR Subject, BSTR Text, BSTR SourceURI, VARIANT Silent);
	STDMETHOD(Annotate)(BSTR URI, BSTR Title);
	STDMETHOD(get_Async)(VARIANT_BOOL *pVal);
	STDMETHOD(put_Async)(VARIANT_BOOL newVal);
	STDMETHOD(get_Application)(IApplication **pVal);
	STDMETHOD(put_Application)(IApplication *newVal);
	STDMETHOD(get_AllowQueueing)(VARIANT_BOOL *pVal);
	STDMETHOD(put_AllowQueueing)(VARIANT_BOOL newVal);
	STDMETHOD(SubmitRequestQueue)();
	STDMETHOD(SubmitRequest)(BSTR MethodName, BSTR Parameters);
};
