// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetBrains Omea Mshtml Browser Component
//
// Implements the Web browser component wrapping with full-scale customization, including view options and security settings & zones.
// Consists of an unmanaged part (C++ ATL, raw hosting, a composite ActiveX control) and a managed part (JScript.NET, Windows Forms control around the unmanaged ActiveX control plus AbstractWebBrowser proxy-inheritor).
// The unmanaged parts server as a wrapper for the custom interfaces only, and should not carry out any meaningful processing. All the events should be delegated to the managed part for processing.
//
// This file belongs to the unmanaged part and declares the CMshtmlBrowser class.
// This class implements the control part of the composite ActiveX control, that is, enables hosting in the ActiveX containers. Also, it holds all the On… functions that invoke the managed wrapper part. This invocation simulates the OLE Events mechanism with the sole difference that it is capable of using the properties and does not support multicasting (delivering to multiple consumers).
//
// © JetBrains Inc, 2004
// Written by (H) Serge Baltic
//
#pragma once
#include "resource.h"       // main symbols

// IMshtmlBrowser
[
	object,
	uuid(C6E6C248-48BD-4A21-A593-4A0C7172D25B),
	dual,
	helpstring("IMshtmlBrowser Interface"),
	pointer_default(unique)
]
__interface IMshtmlBrowser : public IDispatch
{
	// Stock properties
	[propput, id(DISPID_ENABLED)] HRESULT Enabled([in]VARIANT_BOOL vbool);
	[propget, id(DISPID_ENABLED)] HRESULT Enabled([out,retval] VARIANT_BOOL *pbool);
    [propput, id(DISPID_BORDERSTYLE)] HRESULT BorderStyle([in]long style);
    [propget, id(DISPID_BORDERSTYLE)] HRESULT BorderStyle([out, retval]long* pstyle);
	[propput, id(DISPID_TABSTOP)] HRESULT TabStop([in]VARIANT_BOOL vbool);
	[propget, id(DISPID_TABSTOP)] HRESULT TabStop([out,retval] VARIANT_BOOL *pbool);

	// Container's ambient properties for which Invoke is executed on the CMshtmlHostWindow actually, but is relayed to this class for resolution
	[propget, id(DISPID_AMBIENT_DLCONTROL)] HRESULT AmbientDlControl( [out,retval] long *pVal );

	// Normal properties and methods
	[id(1), helpstring("Navigates to the URI specified.")] HRESULT Navigate([in] BSTR URI);
	[id(2), helpstring("Uploads the supplied HTML data into the Web Browser.")] HRESULT ShowHtml([in] IStream* HtmlData);
	[id(3), helpstring("Loads the supplied HTML text into the web browser.")] HRESULT ShowHtmlText([in] BSTR HtmlText);
	[propget, id(4), helpstring("The HTML Document object, if already created.")] HRESULT HtmlDocument([out, retval] IDispatch** pVal);
	[id(5), helpstring("Executes a command on the MSHTML Document's IOleCmdTarget interface. PromptUser defines whether any UI will be shown to user or not.")] HRESULT ExecDocumentCommand([in] BSTR Command, [in] VARIANT_BOOL PromptUser);
	[propget, id(6), helpstring("Encapsulated Microsoft WebBrowser object.")] HRESULT Browser([out, retval] IDispatch** pVal);
	[propget, id(7), helpstring("A parent object that is invoked when whatever MSHTML events occur.")] HRESULT ParentCallback([out, retval] IDispatch** pVal);
	[propput, id(7), helpstring("A parent object that is invoked when whatever MSHTML events occur.")] HRESULT ParentCallback([in] IDispatch* newVal);
	[propget, id(8), helpstring("Controls the set of permissions granted to the content being displayed in the browser. Can be either 'Auto' (let the Internet Security Manager decide based upon the URL), 'Nothing' (prohibit all the actions), 'Everything' (allow all the actions), or '#<n>' (force Internet Security Zone <n>, where <n> is an integer, as returned by IInternetZoneManager).")] HRESULT PermissionSet([out, retval] BSTR* pVal);
	[propput, id(8), helpstring("Controls the set of permissions granted to the content being displayed in the browser. Can be either 'Auto' (let the Internet Security Manager decide based upon the URL), 'Nothing' (prohibit all the actions), 'Everything' (allow all the actions), or '#<n>' (force Internet Security Zone <n>, where <n> is an integer, as returned by IInternetZoneManager).")] HRESULT PermissionSet([in] BSTR newVal);
	[id(9), helpstring("Call this method when certain browser parameters, like whether to display pictures or not, do change, so that the brower could be awakened and fed with this data. Do not call it way too often, but ensure each major settings update is followed by such a call.")] HRESULT SettingsChanged(void);
	[id(10), helpstring("Converts an either file path to its short version.")] HRESULT GetShortFilePath([in] BSTR LongPath, [out,retval] BSTR* ShortPath);
	[id(11), helpstring("Returns the HTML text currently being displayed in the browser, either uploaded directly or downloaded from the Web.")] HRESULT GetHtmlText([out,retval] BSTR* Html);
	[id(12), helpstring("Forces re-creation of the Microsoft Web Browser control in case it disappears from the site represented by this object.")] HRESULT ResurrectWebBrowser();
};


// _IMshtmlBrowserEvents
[
	uuid("C8A2C2D2-70D0-4478-8F3B-DFA03B2017DE"),
	dispinterface,
	helpstring("_IMshtmlBrowserEvents Interface")
]
__interface _IMshtmlBrowserEvents
{
};

// Define the event sink cookies
#define IDE_WebBrowserEvents	1
#define IDE_WebBrowserEvents2	2

// Define types for the event sink implementations
class CMshtmlBrowser;
typedef IDispEventImpl<IDE_WebBrowserEvents, CMshtmlBrowser, &DIID_DWebBrowserEvents, &LIBID_SHDocVw, 1, 1> IWebBrowserEventsSinkImpl;
typedef IDispEventImpl<IDE_WebBrowserEvents2, CMshtmlBrowser, &DIID_DWebBrowserEvents2, &LIBID_SHDocVw, 1, 1> IWebBrowserEvents2SinkImpl;

class CMshtmlHostWindow;

// CMshtmlBrowser
[
	coclass,
	threading("apartment"),
	vi_progid("MshtmlSite.MshtmlBrowser"),
	progid("MshtmlSite.MshtmlBrowser.1"),
	version(1.0),
	uuid("8A2F0DBE-EC1B-4D1D-8712-4259211B41B4"),
	helpstring("JetBrains MSHTML Browser Control for Omea"),
	event_source("com"),
	support_error_info(IMshtmlBrowser),
	registration_script("control.rgs")
]
class ATL_NO_VTABLE CMshtmlBrowser :
	//public IMshtmlBrowser,	// Superceeded by CStockPropImpl
	public IPersistStreamInitImpl<CMshtmlBrowser>,
	public IOleControlImpl<CMshtmlBrowser>,
	public IOleObjectImpl<CMshtmlBrowser>,
	public IOleInPlaceActiveObjectImpl<CMshtmlBrowser>,
	public IViewObjectExImpl<CMshtmlBrowser>,
	public IOleInPlaceObjectWindowlessImpl<CMshtmlBrowser>,
	public IPersistStorageImpl<CMshtmlBrowser>,
	public ISpecifyPropertyPagesImpl<CMshtmlBrowser>,
	public IQuickActivateImpl<CMshtmlBrowser>,
	public IDataObjectImpl<CMshtmlBrowser>,
	public CComControl<CMshtmlBrowser>,
	public CStockPropImpl<CMshtmlBrowser,IMshtmlBrowser>,
	public IWebBrowserEventsSinkImpl,	// Event sink
	public IWebBrowserEvents2SinkImpl	// Event sink
{
public:

	CMshtmlBrowser();
	virtual ~CMshtmlBrowser();

DECLARE_OLEMISC_STATUS(
	OLEMISC_RECOMPOSEONRESIZE |
	OLEMISC_CANTLINKINSIDE |
	OLEMISC_ACTIVATEWHENVISIBLE |
	OLEMISC_SETCLIENTSITEFIRST |
	OLEMISC_ALWAYSRUN |
	OLEMISC_SUPPORTSMULTILEVELUNDO
)

BEGIN_PROP_MAP(CMshtmlBrowser)
	PROP_DATA_ENTRY("_cx", m_sizeExtent.cx, VT_UI4)
	PROP_DATA_ENTRY("_cy", m_sizeExtent.cy, VT_UI4)

	//PROP_ENTRY("Enabled", DISPID_ENABLED, CLSID_NULL)

	// Example entries
	// PROP_ENTRY("Property Description", dispid, clsid)
	// PROP_PAGE(CLSID_StockColorPage)
END_PROP_MAP()


BEGIN_MSG_MAP(CMshtmlBrowser)
	CHAIN_MSG_MAP(CComControl<CMshtmlBrowser>)
	//DEFAULT_REFLECTION_HANDLER()
END_MSG_MAP()
// Handler prototypes:
//  LRESULT MessageHandler(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
//  LRESULT CommandHandler(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
//  LRESULT NotifyHandler(int idCtrl, LPNMHDR pnmh, BOOL& bHandled);

	__event __interface _IMshtmlBrowserEvents;
// IViewObjectEx
	DECLARE_VIEW_STATUS(VIEWSTATUS_SOLIDBKGND | VIEWSTATUS_OPAQUE)

	BEGIN_SINK_MAP(CMshtmlBrowser)
		//SINK_ENTRY_EX(IDE_WebBrowserEvents, DIID_DWebBrowserEvents, DISPID_BEFORENAVIGATE, OnBeforeNavigate1)
		SINK_ENTRY_EX(IDE_WebBrowserEvents2, DIID_DWebBrowserEvents2, DISPID_BEFORENAVIGATE2, OnBeforeNavigate2)
        SINK_ENTRY_EX(IDE_WebBrowserEvents, DIID_DWebBrowserEvents, DISPID_NEWWINDOW, OnNewWindow)
		SINK_ENTRY_EX(IDE_WebBrowserEvents2, DIID_DWebBrowserEvents2, DISPID_STATUSTEXTCHANGE, OnStatusTextChange)
        SINK_ENTRY_EX(IDE_WebBrowserEvents2, DIID_DWebBrowserEvents2, DISPID_TITLECHANGE, OnTitleChange)
		SINK_ENTRY_EX(IDE_WebBrowserEvents2, DIID_DWebBrowserEvents2, DISPID_NAVIGATECOMPLETE2, OnNavigateComplete)
		SINK_ENTRY_EX(IDE_WebBrowserEvents2, DIID_DWebBrowserEvents2, DISPID_DOCUMENTCOMPLETE, OnDocumentComplete)
        SINK_ENTRY_EX(IDE_WebBrowserEvents2, DIID_DWebBrowserEvents2, DISPID_NAVIGATEERROR, OnNavigateError)
        SINK_ENTRY_EX(IDE_WebBrowserEvents2, DIID_DWebBrowserEvents2, DISPID_PROGRESSCHANGE, OnProgressChange)
        SINK_ENTRY_EX(IDE_WebBrowserEvents2, DIID_DWebBrowserEvents2, DISPID_ONQUIT, OnQuit)
        SINK_ENTRY_EX(IDE_WebBrowserEvents2, DIID_DWebBrowserEvents2, DISPID_DOWNLOADCOMPLETE, OnDownloadComplete)
	END_SINK_MAP()

	////////////
	// Functions wired to the WebBrowser events, relaying to the ParentCallback for processing.
	// Some of them do check that the HTMLWindow that fired the event is exactly the same as the main WebBrowser window, so that actions in the inlying frames won't trigger top-level processing.
	void __stdcall OnStatusTextChange( BSTR text );
    void __stdcall OnTitleChange( BSTR text );
	void __stdcall OnBeforeNavigate1( BSTR URL, long Flags, BSTR TargetFrameName, VARIANT* PostData, BSTR Headers, VARIANT_BOOL* Cancel);
	void __stdcall OnBeforeNavigate2( IDispatch* pDisp, VARIANT* URL, VARIANT* Flags, VARIANT* TargetFrameName, VARIANT* PostData, VARIANT* Headers, VARIANT_BOOL* Cancel );
	void __stdcall OnNavigateComplete( LPDISPATCH pDisp, VARIANT* URL );
    void __stdcall OnNavigateError( IDispatch* pDisp, VARIANT* URL, VARIANT* Frame, VARIANT* StatusCode, VARIANT_BOOL* Cancel );
    void __stdcall OnNewWindow( BSTR URL, long Flags, BSTR TargetFrameName, VARIANT* PostData, BSTR Headers, VARIANT_BOOL* Processed );
	void __stdcall OnProgressChange( long Progress, long ProgressMax );
	void __stdcall OnDocumentComplete( IDispatch* pDisp, VARIANT* URL );
	void __stdcall OnQuit();
	void __stdcall OnDownloadComplete();

	//////////////////////////////////////////////////////////////////////////////////////
	// Functions not wired to certain events but also making calls to the ParentCallback

	/// Called when the context menu needs to be displayed.
	bool __stdcall OnContextMenu( DWORD dwID, POINT* pptPosition, IUnknown* pCommandTarget, IDispatch* pDispatchObjectHit );
	/// Invokes the parent callback upon receival of WM_KEYDOWN or WM_SYSKEYDOWN messages to give it first crack at the message processing
	bool __stdcall OnBeforeKeyDown( long code, bool ctrl, bool alt, bool shift );
	/// Invokes the parent callback upon receival of WM_KEYUP or WM_SYSKEYUP messages to give it first crack at the message processing
	bool __stdcall OnBeforeKeyUp( long code, bool ctrl, bool alt, bool shift );
	/// Invokes the parent callback upon receival of WM_CHAR or WM_SYSCHAR messages to give it first crack at the message processing
	bool __stdcall OnBeforeKeyPress( long code, bool ctrl, bool alt, bool shift );
	/// Implements the very parent callback invocation procedure for any of the OnBeforeKeyDown, OnBeforeKeyUp, OnBeforeKeyPress functions.
	bool __stdcall OnBeforeKeyAny( LPCWSTR szFunctionName, long code, bool ctrl, bool alt, bool shift );
	/// Queries for an action for the URL security policy. Returns whether the query has been processed, otherwise, the default security manager should be invoked.
	bool __stdcall OnUrlAction( LPCWSTR pwszUrl, DWORD dwAction, DWORD dwFlags, BYTE *pFlags );
	/// Requests the host's UI capabilities and requirements, eg whether to draw the 3D-border, allow text selection, apply system themes, and so on. Returns whether the host has specified the flags.
	bool __stdcall OnGetHostInfo( DWORD *pPolicy );
	/// Callbacks the wrapper that the underlying browser control has been created and can be used. Note: this call may occur while processing some of the incoming calls, so do not originate any more calls from the handler, instead, schedulle the processing thru via the message pump.
	void __stdcall OnBrowserCreated();
	/// Retrieves the external object that should be returned from the window.external property, or Null if it's not available.
	IDispatchPtr __stdcall OnGetExternal();


// IMshtmlBrowser
public:
		HRESULT OnDrawAdvanced(ATL_DRAWINFO& di)
		{
			/*
		RECT& rc = *(RECT*)di.prcBounds;
		// Set Clip region to the rectangle specified by di.prcBounds
		HRGN hRgnOld = NULL;
		if (GetClipRgn(di.hdcDraw, hRgnOld) != 1)
			hRgnOld = NULL;
		bool bSelectOldRgn = false;

		HRGN hRgnNew = CreateRectRgn(rc.left, rc.top, rc.right, rc.bottom);

		if (hRgnNew != NULL)
		{
			bSelectOldRgn = (SelectClipRgn(di.hdcDraw, hRgnNew) != ERROR);
		}

		Rectangle(di.hdcDraw, rc.left, rc.top, rc.right, rc.bottom);
		SetTextAlign(di.hdcDraw, TA_CENTER|TA_BASELINE);
		LPCTSTR pszText = _T("ATL 7.0 : MshtmlBrowser, yow!");
		TextOut(di.hdcDraw,
			(rc.left + rc.right) / 2,
			(rc.top + rc.bottom) / 2,
			pszText,
			lstrlen(pszText));

		if (bSelectOldRgn)
			SelectClipRgn(di.hdcDraw, hRgnOld);
			*/

		return S_OK;
	}


	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}
// Overrides
public:
	friend class CMshtmlHostWindow;

	/// Processes menu accelerator-key messages from the container's message queue. This method should only be used for objects created by a DLL object application.
	STDMETHOD(TranslateAccelerator)(MSG *pMsg);

	//STDMETHOD(QueryInterface)( REFIID iid, void** ppvObject );

	// IOleObject methods
	// The IOleObject interface is the principal means by which an embedded object provides basic functionality to, and communicates with, its container.
	STDMETHOD(DoVerb)( LONG iVerb, /* Value representing verb to be performed */ LPMSG lpmsg, /* Pointer to structure that describes Windows */ /* message */ IOleClientSite *pActiveSite, /* Pointer to active client site */ LONG lindex, /* Reserved */ HWND hwndParent, /* Handle of window containing the object */ LPCRECT lprcPosRect /* Pointer to object's display rectangle */ );
	STDMETHOD(EnumVerbs)( IEnumOLEVERB **ppEnumOleVerb /* Address of output variable that receives the IEnumOleVerb interface pointer*/ );
	STDMETHOD(SetHostNames)( /* [in] */ LPCOLESTR szContainerApp, /* [unique][in] */ LPCOLESTR szContainerObj);
	STDMETHOD(InitFromData)( /* [unique][in] */ IDataObject *pDataObject, /* [in] */ BOOL fCreation, /* [in] */ DWORD dwReserved);
	STDMETHOD(GetClipboardData)( /* [in] */ DWORD dwReserved, /* [out] */ IDataObject **ppDataObject);
	STDMETHOD(Update)();
	STDMETHOD(IsUpToDate)();
	STDMETHOD(SetColorScheme)( /* [in] */ LOGPALETTE *pLogpal);

	// IOleInPlaceObject methods
	// The IOleInPlaceObject interface manages the activation and deactivation of in-place objects, and determines how much of the in-place object should be visible.
	STDMETHOD(InPlaceDeactivate)();	// Deactivates an active in-place object and discards the object's undo state.
	STDMETHOD(UIDeactivate)();	// Deactivates and removes the user interface that supports in-place activation.
	// We do not forward the SetObjectRects call, handled by our container. // Indicates how much of the in-place object is visible.
	STDMETHOD(ReactivateAndUndo)();	// Reactivates a previously deactivated object, undoing the last state of the object.

	// Stock properties processed by the CStockPropImpl
	BOOL	m_bEnabled;
	LONG	m_nBorderStyle;
	BOOL	m_bTabStop;

	// Additional constants

	// (taken from MSIE2 source code)
	//Define the command group GUID for the WebBrowser control
	//It is undocumented, and supposed to be changed in the feature
	// Needed for the MSHTML's IOleCommandTarget interface calls
	static const GUID CGID_IWebBrowser;

	// Commands for the MSHTML's IOleCommandTarget interface
	enum { HTMLID_FIND = 1, HTMLID_VIEWSOURCE = 2, HTMLID_OPTIONS = 3 };

	/// Queries the external object (the parent control) for a specific interface.
	STDMETHOD(QueryExternal)(REFIID riid, void**pp);

protected:

// Implementation
protected:
	/// The WebBrowser Control interface pointer
	SHDocVw::IWebBrowser2Ptr	m_oBrowser;

	/// Creates an instance of the WebBrowser control inside our container, or does nothing if it has already been created (which is not considered to be a failure). This function throws an exception of type _com_error on failure (browser already created is not a failure; failures include COM errors from called functions, etc). This function is called every so often to ensure that the object gets created as soon as needed.
	void TryInstantiate();

	/// The WebBrowser control will never UI-Deactivate itself unless destroyed. Thus let's check at any chance if we're still having focus. If not, invoke UI-Deactivation (as it's then activated correctly on any chance). This function never throws an exception and has a supplimentary action, nothing will fail if it does not succeed. This function is called every so often to encure that the control is UI-Deactivated when it loses focus.
	void TryUIDeactivate();

	/// Contains the UserMode container's ambient property value
	BOOL	m_bUserMode;

	/// Pointer to the class instance implementing the site part of this composite control
	/// It is created when the control gets activated
	CMshtmlHostWindow	*m_pSite;

	/// The parent callback object that is invoked when MSHTML events occur. It's not necessary to fire events in this case because that will be an overdesign as there is always no more than one consumer of those events. This pointer holds a reference that must be released upon inplace-deactivation of the control.
	IDispatchPtr	m_oParentCallback;

	/// Controls the set of permissions granted to the control being displayed in the browser, used by the Internet Security Manager.
	/// PS_Auto means that the Internet Security Manager should make a decision based upon the URL being displayed.
	/// PS_Nothing means "Nothing" permission set (prohibit everything). This is the default value.
	/// PS_Everything means "Everything" permission set (allow everything).
	/// PS_Zone means "A security zone is set, see m_nSecurityZone".
	int	m_nPermissionSet;
	enum { PS_Auto, PS_Nothing, PS_Everything, PS_Zone };

	/// The Internet Security Zone which will be assigned to the content displayed in the browser, see IInternetZoneManager.
	/// Taken into account only if m_nPermissionSet is set to PS_Zone.
	DWORD	m_nSecurityZone;

public:
	STDMETHOD(get_AmbientDlControl)( long *pVal );
	STDMETHOD(Navigate)(BSTR URI);
	STDMETHOD(ShowHtml)(IStream* HtmlData);
	STDMETHOD(ShowHtmlText)(BSTR HtmlText);
	STDMETHOD(get_HtmlDocument)(IDispatch** pVal);
	STDMETHOD(ExecDocumentCommand)(BSTR Command, VARIANT_BOOL PromptUser);
	STDMETHOD(get_Browser)(IDispatch** pVal);
	STDMETHOD(get_ParentCallback)(IDispatch** pVal);
	STDMETHOD(put_ParentCallback)(IDispatch* newVal);
	STDMETHOD(get_PermissionSet)(BSTR* pVal);
	STDMETHOD(put_PermissionSet)(BSTR newVal);
	STDMETHOD(SettingsChanged)(void);
	STDMETHOD(GetShortFilePath)(BSTR LongPath, BSTR* ShortPath);
	STDMETHOD(GetHtmlText)(BSTR* Html);
	STDMETHOD(ResurrectWebBrowser)();
};
