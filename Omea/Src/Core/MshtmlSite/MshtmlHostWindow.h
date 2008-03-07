/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// JetBrains Omea Mshtml Browser Component
//
// Implements the Web browser component wrapping with full-scale customization, including view options and security settings & zones.
// Consists of an unmanaged part (C++ ATL, raw hosting, a composite ActiveX control) and a managed part (JScript.NET, Windows Forms control around the unmanaged ActiveX control plus AbstractWebBrowser proxy-inheritor).
// The unmanaged parts server as a wrapper for the custom interfaces only, and should not carry out any meaningful processing. All the events should be delegated to the managed part for processing.
//
// This file belongs to the unmanaged part and declares the CMshtmlHostWindow class.
// This class implements the container part of the composite ActiveX control, that is, enables hosting of the WebBrowser control in it. Also, it implements the additional interfaces that are queried by MSHTML on its Client Site (which this class implements as well). A Service Provider implemented by this class also exposes the security management interfaces that are chained as a part of the URL Moniker infrastructure. The base code for this class has been mostly derived from the ATL implementation of some ax host window.
//
// © JetBrains Inc, 2004
// Written by (H) Serge Baltic
//
#pragma once
#include "AtlHost.h"

class CMshtmlBrowser;	// Class implementing the control part of this composite control

/// The MSHTML Host Window Class.
/// Supercedes the Atl Host Window class and provides for overriding all the necessary security settings and customization parameters.
///
/// Note that we do not host the MSHTML server directly as it is an ActiveDocument which involves additional troubles for us. Also, pure MSHTML does not support in-place links navigation and some other features implemented in its default wrapper called WebBrowser Control. That one is an ActiveX Control and we can host it as a usual control.
class ATL_NO_VTABLE CMshtmlHostWindow : 
		public CComCoClass<CMshtmlHostWindow, &CLSID_NULL>,	// No coclass specified
		public CComObjectRootEx<CComSingleThreadModel>,
		public CWindowImpl<CMshtmlHostWindow>,
		public IAxWinHostWindowLic,
		public IOleClientSite,
		public IOleInPlaceSiteWindowless,
		public IOleControlSite,
		public IOleContainer,
		public IObjectWithSiteImpl<CMshtmlHostWindow>,
		public IServiceProvider,
		public IAdviseSink,
		//public IDocHostUIHandler,	// IE Customization Interface (included in IDocHostUIHandler2)
		public IDocHostUIHandler2,	// IE Customization Interface
		public IDocHostShowUI,	// IE Customization Interface
		public IInternetSecurityManager,	// IE Security Interface
		public IDispatchImpl<IAxWinAmbientDispatchEx, &__uuidof(IAxWinAmbientDispatchEx), &CAtlModule::m_libid, 0xFFFF, 0xFFFF>
{
protected:
	CMshtmlHostWindow();

	/// Pointer to the class implementing the control part of this composite
	/// Must be explicitly set immediately after creation of this class in the caller by invoking the SetControlPart function
	/// Does not hold the lock on the parent control
	CMshtmlBrowser	*m_pControl;

public:
	friend class CMshtmlBrowser;

	~CMshtmlHostWindow();

	/// See m_pControl variable
	void SetControlPart(CMshtmlBrowser *pControl) { m_pControl = pControl; }

	/// Checks whether the inlaying control is currently in the UI-Active state
	bool IsUIActive() { return m_bUIActive; }

public:
	void FinalRelease()
	{
		ReleaseAll();
	}

	virtual void OnFinalMessage(HWND /*hWnd*/)
	{
		GetControllingUnknown()->Release();
	}

	DECLARE_NO_REGISTRY()
	DECLARE_POLY_AGGREGATABLE(CMshtmlHostWindow)
	DECLARE_GET_CONTROLLING_UNKNOWN()

	BEGIN_COM_MAP(CMshtmlHostWindow)
		COM_INTERFACE_ENTRY2(IDispatch, IAxWinAmbientDispatchEx)
		COM_INTERFACE_ENTRY(IAxWinHostWindowLic)
		COM_INTERFACE_ENTRY(IAxWinAmbientDispatchEx)
		COM_INTERFACE_ENTRY(IAxWinHostWindow)
		COM_INTERFACE_ENTRY(IOleClientSite)
		COM_INTERFACE_ENTRY(IOleInPlaceSiteWindowless)
		COM_INTERFACE_ENTRY(IOleInPlaceSiteEx)
		COM_INTERFACE_ENTRY(IOleInPlaceSite)
		COM_INTERFACE_ENTRY(IOleWindow)
		COM_INTERFACE_ENTRY(IOleControlSite)
		COM_INTERFACE_ENTRY(IOleContainer)
		COM_INTERFACE_ENTRY(IObjectWithSite)
		COM_INTERFACE_ENTRY(IServiceProvider)
		COM_INTERFACE_ENTRY(IAxWinAmbientDispatch)
		COM_INTERFACE_ENTRY(IDocHostUIHandler)
		COM_INTERFACE_ENTRY(IDocHostUIHandler2)
		COM_INTERFACE_ENTRY(IDocHostShowUI)
		COM_INTERFACE_ENTRY(IInternetSecurityManager)
		COM_INTERFACE_ENTRY(IAdviseSink)
	END_COM_MAP()

	static CWndClassInfo& GetWndClassInfo()
	{
		static CWndClassInfo wc =
		{
			{ sizeof(WNDCLASSEX), 0, StartWindowProc,
			  0, 0, 0, 0, 0, (HBRUSH)(COLOR_WINDOW + 1), 0, _T(ATLAXWIN_CLASS), 0 },
			NULL, NULL, IDC_ARROW, TRUE, 0, _T("")
		};
		return wc;
	}

	BEGIN_MSG_MAP(CMshtmlHostWindow)
		//MESSAGE_HANDLER(WM_ERASEBKGND, OnEraseBackground)
		//MESSAGE_HANDLER(WM_PAINT, OnPaint)
		MESSAGE_HANDLER(WM_SIZE, OnSize)
		MESSAGE_HANDLER(WM_MOUSEACTIVATE, OnMouseActivate)
		MESSAGE_HANDLER(WM_SETFOCUS, OnSetFocus)
		MESSAGE_HANDLER(WM_KILLFOCUS, OnKillFocus)
		if (m_bWindowless)
		{
			// Mouse messages handled when a windowless control has captured the cursor
			// or if the cursor is over the control
			DWORD dwHitResult = m_bCapture ? HITRESULT_HIT : HITRESULT_OUTSIDE;
			if (dwHitResult == HITRESULT_OUTSIDE && m_spViewObject != NULL)
			{
				POINT ptMouse = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
				m_spViewObject->QueryHitPoint(DVASPECT_CONTENT, &m_rcPos, ptMouse, 0, &dwHitResult);
			}
			if (dwHitResult == HITRESULT_HIT)
			{
				MESSAGE_HANDLER(WM_MOUSEMOVE, OnWindowlessMouseMessage)
				MESSAGE_HANDLER(WM_SETCURSOR, OnWindowlessMouseMessage)
				MESSAGE_HANDLER(WM_LBUTTONUP, OnWindowlessMouseMessage)
				MESSAGE_HANDLER(WM_RBUTTONUP, OnWindowlessMouseMessage)
				MESSAGE_HANDLER(WM_MBUTTONUP, OnWindowlessMouseMessage)
				MESSAGE_HANDLER(WM_LBUTTONDOWN, OnWindowlessMouseMessage)
				MESSAGE_HANDLER(WM_RBUTTONDOWN, OnWindowlessMouseMessage)
				MESSAGE_HANDLER(WM_MBUTTONDOWN, OnWindowlessMouseMessage)
				MESSAGE_HANDLER(WM_LBUTTONDBLCLK, OnWindowlessMouseMessage)
				MESSAGE_HANDLER(WM_RBUTTONDBLCLK, OnWindowlessMouseMessage)
				MESSAGE_HANDLER(WM_MBUTTONDBLCLK, OnWindowlessMouseMessage)
			}
		}
		if (m_bWindowless & m_bHaveFocus)
		{
			// Keyboard messages handled only when a windowless control has the focus
			MESSAGE_HANDLER(WM_KEYDOWN, OnWindowMessage)
			MESSAGE_HANDLER(WM_KEYUP, OnWindowMessage)
			MESSAGE_HANDLER(WM_CHAR, OnWindowMessage)
			MESSAGE_HANDLER(WM_DEADCHAR, OnWindowMessage)
			MESSAGE_HANDLER(WM_SYSKEYDOWN, OnWindowMessage)
			MESSAGE_HANDLER(WM_SYSKEYUP, OnWindowMessage)
			MESSAGE_HANDLER(WM_SYSDEADCHAR, OnWindowMessage)
			MESSAGE_HANDLER(WM_HELP, OnWindowMessage)
			MESSAGE_HANDLER(WM_CANCELMODE, OnWindowMessage)
			MESSAGE_HANDLER(WM_IME_CHAR, OnWindowMessage)
			MESSAGE_HANDLER(WM_MBUTTONDBLCLK, OnWindowMessage)
			MESSAGE_RANGE_HANDLER(WM_IME_SETCONTEXT, WM_IME_KEYUP, OnWindowMessage)
		}
		MESSAGE_HANDLER(WM_DESTROY, OnDestroy)
		if (m_bMessageReflect)
		{
			bHandled = TRUE;
			lResult = ReflectNotifications(uMsg, wParam, lParam, bHandled);
			if(bHandled)
				return TRUE;
		}
		MESSAGE_HANDLER(WM_ATLGETHOST, OnGetUnknown)
		MESSAGE_HANDLER(WM_ATLGETCONTROL, OnGetControl)
		MESSAGE_HANDLER(WM_FORWARDMSG, OnForwardMsg)
	END_MSG_MAP()

	LRESULT OnForwardMsg(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM lParam, BOOL& /*bHandled*/)
	{
		ATLASSERT(lParam != 0);
		LPMSG lpMsg = (LPMSG)lParam;
		CComQIPtr<IOleInPlaceActiveObject, &__uuidof(IOleInPlaceActiveObject)> spInPlaceActiveObject(m_spUnknown);
		if(spInPlaceActiveObject)
		{
			if(spInPlaceActiveObject->TranslateAccelerator(lpMsg) == S_OK)
				return 1;
		}
		return 0;
	}

	LRESULT OnGetUnknown(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM /*lParam*/, BOOL& /*bHandled*/)
	{
		IUnknown* pUnk = GetControllingUnknown();
		pUnk->AddRef();
		return (LRESULT)pUnk;
	}
	LRESULT OnGetControl(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM /*lParam*/, BOOL& /*bHandled*/)
	{
		IUnknown* pUnk = m_spUnknown;
		if (pUnk)
			pUnk->AddRef();
		return (LRESULT)pUnk;
	}

	void ReleaseAll()
	{
		if (m_bReleaseAll)
			return;
		m_bReleaseAll = TRUE;

		if (m_spViewObject != NULL)
			m_spViewObject->SetAdvise(DVASPECT_CONTENT, 0, NULL);

		if(m_dwAdviseSink != 0xCDCDCDCD)
		{
			AtlUnadvise(m_spUnknown, m_iidSink, m_dwAdviseSink);
			m_dwAdviseSink = 0xCDCDCDCD;
		}

		if (m_spOleObject)
		{
			m_spOleObject->Unadvise(m_dwOleObject);
			m_spOleObject->Close(OLECLOSE_NOSAVE);
			m_spOleObject->SetClientSite(NULL);
		}

		if (m_spUnknown != NULL)
		{
			CComPtr<IObjectWithSite> spSite;
			m_spUnknown->QueryInterface(__uuidof(IObjectWithSite), (void**)&spSite);
			if (spSite != NULL)
				spSite->SetSite(NULL);
		}

		m_spViewObject.Release();
		m_dwViewObjectType = 0;

		m_spInPlaceObjectWindowless.Release();
		m_spOleObject.Release();
		m_spUnknown.Release();

		m_spInPlaceUIWindow.Release();
		m_spInPlaceFrame.Release();

		m_bInPlaceActive = FALSE;
		m_bWindowless = FALSE;
		m_bInPlaceActive = FALSE;
		m_bUIActive = FALSE;
		m_bCapture = FALSE;
		m_bReleaseAll = FALSE;

		if (m_hAccel != NULL)
		{
			DestroyAcceleratorTable(m_hAccel);
			m_hAccel = NULL;
		}
	}


// window message handlers
	LRESULT OnEraseBackground(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM /*lParam*/, BOOL& bHandled)
	{
		if (m_spViewObject == NULL)
			bHandled = false;

		return 1;
	}

	LRESULT OnMouseActivate(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM /*lParam*/, BOOL& bHandled)
	{
		bHandled = FALSE;
		if (m_dwMiscStatus & OLEMISC_NOUIACTIVATE)
		{
			if (m_spOleObject != NULL && !m_bInPlaceActive)
			{
				CComPtr<IOleClientSite> spClientSite;
				GetControllingUnknown()->QueryInterface(__uuidof(IOleClientSite), (void**)&spClientSite);
				if (spClientSite != NULL)
					m_spOleObject->DoVerb(OLEIVERB_INPLACEACTIVATE, NULL, spClientSite, 0, m_hWnd, &m_rcPos);
			}
		}
		else
		{
			BOOL b;
			OnSetFocus(0, 0, 0, b);
		}
		return 0;
	}
	LRESULT OnSetFocus(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM /*lParam*/, BOOL& bHandled)
	{
		m_bHaveFocus = TRUE;
		if (!m_bReleaseAll)
		{
			if (m_spOleObject != NULL && !m_bUIActive)
			{
				CComPtr<IOleClientSite> spClientSite;
				GetControllingUnknown()->QueryInterface(__uuidof(IOleClientSite), (void**)&spClientSite);
				if (spClientSite != NULL)
					m_spOleObject->DoVerb(OLEIVERB_UIACTIVATE, NULL, spClientSite, 0, m_hWnd, &m_rcPos);
			}
			if (m_bWindowless)
				::SetFocus(m_hWnd);
			else if(!IsChild(::GetFocus()))
				::SetFocus(::GetWindow(m_hWnd, GW_CHILD));
		}
		bHandled = FALSE;
		return 0;
	}
	LRESULT OnKillFocus(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM /*lParam*/, BOOL& bHandled)
	{
		m_bHaveFocus = FALSE;
		bHandled = FALSE;
		return 0;
	}
	LRESULT OnSize(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM lParam, BOOL& bHandled)
	{
		int nWidth = GET_X_LPARAM(lParam);  // width of client area
		int nHeight = GET_Y_LPARAM(lParam); // height of client area

		m_rcPos.right = m_rcPos.left + nWidth;
		m_rcPos.bottom = m_rcPos.top + nHeight;
		m_pxSize.cx = m_rcPos.right - m_rcPos.left;
		m_pxSize.cy = m_rcPos.bottom - m_rcPos.top;
		AtlPixelToHiMetric(&m_pxSize, &m_hmSize);

		if (m_spOleObject)
			m_spOleObject->SetExtent(DVASPECT_CONTENT, &m_hmSize);
		if (m_spInPlaceObjectWindowless)
			m_spInPlaceObjectWindowless->SetObjectRects(&m_rcPos, &m_rcPos);
		if (m_bWindowless)
			InvalidateRect(NULL, TRUE);
		bHandled = FALSE;
		return 0;
	}
	LRESULT OnDestroy(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
	{
		GetControllingUnknown()->AddRef();
		DefWindowProc(uMsg, wParam, lParam);
		ReleaseAll();
		bHandled = FALSE;
		return 0;
	}
	LRESULT OnWindowMessage(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
	{
		LRESULT lRes = 0;
		HRESULT hr = S_FALSE;
		if (m_bInPlaceActive && m_bWindowless && m_spInPlaceObjectWindowless)
			hr = m_spInPlaceObjectWindowless->OnWindowMessage(uMsg, wParam, lParam, &lRes);
		if (hr == S_FALSE)
			bHandled = FALSE;
		return lRes;
	}
	LRESULT OnWindowlessMouseMessage(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
	{
		LRESULT lRes = 0;
		if (m_bInPlaceActive && m_bWindowless && m_spInPlaceObjectWindowless)
			m_spInPlaceObjectWindowless->OnWindowMessage(uMsg, wParam, lParam, &lRes);
		bHandled = FALSE;
		return lRes;
	}
	LRESULT OnPaint(UINT /*uMsg*/, WPARAM /*wParam*/, LPARAM /*lParam*/, BOOL& bHandled)
	{
		if (m_spViewObject == NULL)
		{
			PAINTSTRUCT ps;
			HDC hdc = ::BeginPaint(m_hWnd, &ps);
			if (hdc == NULL)
				return 0;
			RECT rcClient;
			GetClientRect(&rcClient);
			HBRUSH hbrBack = CreateSolidBrush(m_clrBackground);
			if (hbrBack != NULL)
			{
				FillRect(hdc, &rcClient, hbrBack);
				DeleteObject(hbrBack);
			}
			::EndPaint(m_hWnd, &ps);
			return 1;
		}
		if (m_spViewObject && m_bWindowless)
		{
			PAINTSTRUCT ps;
			HDC hdc = ::BeginPaint(m_hWnd, &ps);

			if (hdc == NULL)
				return 0;

			RECT rcClient;
			GetClientRect(&rcClient);

			HBITMAP hBitmap = CreateCompatibleBitmap(hdc, rcClient.right - rcClient.left, rcClient.bottom - rcClient.top);
			if (hBitmap != NULL)
			{
				HDC hdcCompatible = ::CreateCompatibleDC(hdc);
				if (hdcCompatible != NULL)
				{
					HBITMAP hBitmapOld = (HBITMAP)SelectObject(hdcCompatible, hBitmap); 
					if (hBitmapOld != NULL)
					{
						HBRUSH hbrBack = CreateSolidBrush(m_clrBackground);
						if (hbrBack != NULL)
						{
							FillRect(hdcCompatible, &rcClient, hbrBack);
							DeleteObject(hbrBack);

							m_spViewObject->Draw(DVASPECT_CONTENT, -1, NULL, NULL, NULL, hdcCompatible, (RECTL*)&m_rcPos, (RECTL*)&m_rcPos, NULL, NULL); 

							::BitBlt(hdc, 0, 0, rcClient.right, rcClient.bottom,  hdcCompatible, 0, 0, SRCCOPY);
						}
						::SelectObject(hdcCompatible, hBitmapOld); 
					}
					::DeleteDC(hdcCompatible);
				}
				::DeleteObject(hBitmap);
			}
			::EndPaint(m_hWnd, &ps);
		}
		else
		{
			bHandled = FALSE;
			return 0;
		}
		return 1;
	}

// IAxWinHostWindow
	STDMETHOD(CreateControl)(LPCOLESTR lpTricsData, HWND hWnd, IStream* pStream)
	{
		CComPtr<IUnknown> p;
		return CreateControlLicEx(lpTricsData, hWnd, pStream, &p, IID_NULL, NULL, NULL);
	}
	STDMETHOD(CreateControlEx)(LPCOLESTR lpszTricsData, HWND hWnd, IStream* pStream, IUnknown** ppUnk, REFIID iidAdvise, IUnknown* punkSink)
	{
		return CreateControlLicEx(lpszTricsData, hWnd, pStream, ppUnk, iidAdvise, punkSink, NULL);
	}
	STDMETHOD(AttachControl)(IUnknown* pUnkControl, HWND hWnd)
	{
		HRESULT hr = S_FALSE;

		ReleaseAll();

		bool bReleaseWindowOnFailure = false; // Used to keep track of whether we subclass the window

		if ((m_hWnd != NULL) && (m_hWnd != hWnd)) // Don't release the window if it's the same as the one we already subclass/own
		{
			RedrawWindow(NULL, NULL, RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASE | RDW_INTERNALPAINT | RDW_FRAME);
			ReleaseWindow();
		}

		if (::IsWindow(hWnd))
		{
			if (m_hWnd != hWnd) // Don't need to subclass the window if we already own it
			{
				SubclassWindow(hWnd);
				bReleaseWindowOnFailure = true;
			}

			hr = ActivateAx(pUnkControl, true, NULL);

			if (FAILED(hr))
			{
				ReleaseAll();

				if (m_hWnd != NULL)
				{
					RedrawWindow(NULL, NULL, RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASE | RDW_INTERNALPAINT | RDW_FRAME);
					if (bReleaseWindowOnFailure) // We subclassed the window in an attempt to create this control, so we unsubclass on failure
						ReleaseWindow();
				}
			}
		}
		return hr;
	}
	STDMETHOD(QueryControl)(REFIID riid, void** ppvObject)
	{
		HRESULT hr = E_POINTER;
		if (ppvObject)
		{
			if (m_spUnknown)
			{
				hr = m_spUnknown->QueryInterface(riid, ppvObject);
			}
			else
			{
				*ppvObject = NULL;
				hr = OLE_E_NOCONNECTION;
			}
		}
		return hr;
	}
	STDMETHOD(SetExternalDispatch)(IDispatch* pDisp)
	{
		m_spExternalDispatch = pDisp;
		return S_OK;
	}
	STDMETHOD(SetExternalUIHandler)(IDocHostUIHandlerDispatch* pUIHandler)
	{
		m_spIDocHostUIHandlerDispatch = pUIHandler;
		return S_OK;
	}

	STDMETHOD(CreateControlLic)(LPCOLESTR lpTricsData, HWND hWnd, IStream* pStream, BSTR bstrLic)
	{
		CComPtr<IUnknown> p;
		return CreateControlLicEx(lpTricsData, hWnd, pStream, &p, IID_NULL, NULL, bstrLic);
	}
	STDMETHOD(CreateControlLicEx)(LPCOLESTR lpszTricsData, HWND hWnd, IStream* pStream, IUnknown** ppUnk, REFIID iidAdvise, IUnknown* punkSink, BSTR bstrLic)
	{
		ATLASSERT(ppUnk != NULL);
		if (ppUnk == NULL)
			return E_POINTER;
		*ppUnk = NULL;
		HRESULT hr = S_FALSE;
		bool bReleaseWindowOnFailure = false; // Used to keep track of whether we subclass the window

		ReleaseAll();

		if ((m_hWnd != NULL) && (m_hWnd != hWnd)) // Don't release the window if it's the same as the one we already subclass/own
		{
			RedrawWindow(NULL, NULL, RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASE | RDW_INTERNALPAINT | RDW_FRAME);
			ReleaseWindow();
		}

		if (::IsWindow(hWnd))
		{
			USES_CONVERSION;
			if (m_hWnd != hWnd) // Don't need to subclass the window if we already own it
			{
				SubclassWindow(hWnd);
				bReleaseWindowOnFailure = true;
			}
			if (m_clrBackground == NULL)
			{
				if (IsParentDialog())
					m_clrBackground = GetSysColor(COLOR_BTNFACE);
				else
					m_clrBackground = GetSysColor(COLOR_WINDOW);
			}

			bool bWasHTML = false;

			hr = CreateNormalizedObject(lpszTricsData, __uuidof(IUnknown), (void**)ppUnk, bWasHTML, bstrLic);

			if (SUCCEEDED(hr))
				hr = ActivateAx(*ppUnk, false, pStream);

			// Try to hook up any sink the user might have given us.
			m_iidSink = iidAdvise;
			if(SUCCEEDED(hr) && *ppUnk && punkSink)
				AtlAdvise(*ppUnk, punkSink, m_iidSink, &m_dwAdviseSink);

			if (SUCCEEDED(hr) && bWasHTML && *ppUnk != NULL)
			{
				if ((GetStyle() & (WS_VSCROLL | WS_HSCROLL)) == 0)
					m_dwDocHostFlags |= DOCHOSTUIFLAG_SCROLL_NO;
				else
				{
					DWORD dwStyle = GetStyle();
					SetWindowLong(GWL_STYLE, dwStyle & ~(WS_VSCROLL | WS_HSCROLL));
					SetWindowPos(NULL, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOZORDER | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_DRAWFRAME);
				}

				CComPtr<IUnknown> spUnk(*ppUnk);
				// Is it just plain HTML?
				USES_CONVERSION;
				if ((lpszTricsData[0] == OLECHAR('M') || lpszTricsData[0] == OLECHAR('m')) &&
					(lpszTricsData[1] == OLECHAR('S') || lpszTricsData[1] == OLECHAR('s')) &&
					(lpszTricsData[2] == OLECHAR('H') || lpszTricsData[2] == OLECHAR('h')) &&
					(lpszTricsData[3] == OLECHAR('T') || lpszTricsData[3] == OLECHAR('t')) &&
					(lpszTricsData[4] == OLECHAR('M') || lpszTricsData[4] == OLECHAR('m')) &&
					(lpszTricsData[5] == OLECHAR('L') || lpszTricsData[5] == OLECHAR('l')) &&
					(lpszTricsData[6] == OLECHAR(':')))
				{
					// Just HTML: load the HTML data into the document

					UINT nCreateSize = (ocslen(lpszTricsData) - 7) * sizeof(OLECHAR);
					HGLOBAL hGlobal = GlobalAlloc(GHND, nCreateSize);
					if (hGlobal)
					{
						CComPtr<IStream> spStream;
						BYTE* pBytes = (BYTE*) GlobalLock(hGlobal);
						memcpy(pBytes, lpszTricsData + 7, nCreateSize);
						GlobalUnlock(hGlobal);
						hr = CreateStreamOnHGlobal(hGlobal, TRUE, &spStream);
						if (SUCCEEDED(hr))
						{
							CComPtr<IPersistStreamInit> spPSI;
							hr = spUnk->QueryInterface(__uuidof(IPersistStreamInit), (void**)&spPSI);
							if (SUCCEEDED(hr))
								hr = spPSI->Load(spStream);
						}
					}
					else
						hr = E_OUTOFMEMORY;
				}
				else
				{
					CComPtr<IWebBrowser2> spBrowser;
					spUnk->QueryInterface(__uuidof(IWebBrowser2), (void**)&spBrowser);
					if (spBrowser)
					{
						CComVariant ve;
						CComVariant vurl(lpszTricsData);
						spBrowser->put_Visible(ATL_VARIANT_TRUE);
						spBrowser->Navigate2(&vurl, &ve, &ve, &ve, &ve);
					}
				}

			}
			if (FAILED(hr) || m_spUnknown == NULL)
			{
				// We don't have a control or something failed so release
				ReleaseAll();

				if (m_hWnd != NULL)
				{
					RedrawWindow(NULL, NULL, RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASE | RDW_INTERNALPAINT | RDW_FRAME);
					if (FAILED(hr) && bReleaseWindowOnFailure) // We subclassed the window in an attempt to create this control, so we unsubclass on failure
						ReleaseWindow();
				}
			}
		}
		return hr;
	}

	/////////////////////////////////////////////////////////////////////////////
	// IDocHostUIHandler
	virtual HRESULT STDMETHODCALLTYPE ShowContextMenu(/* [in] */ DWORD dwID, /* [in] */ POINT *ppt, /* [in] */ IUnknown *pcmdtReserved, /* [in] */ IDispatch *pdispReserved);        
	virtual HRESULT STDMETHODCALLTYPE GetHostInfo(/* [out][in] */ DOCHOSTUIINFO *pInfo);        
	virtual HRESULT STDMETHODCALLTYPE ShowUI(/* [in] */ DWORD dwID, /* [in] */ IOleInPlaceActiveObject *pActiveObject, /* [in] */ IOleCommandTarget *pCommandTarget, /* [in] */ IOleInPlaceFrame *pFrame, /* [in] */ IOleInPlaceUIWindow *pDoc);        
	virtual HRESULT STDMETHODCALLTYPE HideUI( void);        
	virtual HRESULT STDMETHODCALLTYPE UpdateUI( void);        
	virtual HRESULT STDMETHODCALLTYPE EnableModeless(/* [in] */ BOOL fEnable);        
	virtual HRESULT STDMETHODCALLTYPE OnDocWindowActivate(/* [in] */ BOOL fActivate);        
	virtual HRESULT STDMETHODCALLTYPE OnFrameWindowActivate(/* [in] */ BOOL fActivate);        
	virtual HRESULT STDMETHODCALLTYPE ResizeBorder(/* [in] */ LPCRECT prcBorder, /* [in] */ IOleInPlaceUIWindow *pUIWindow, /* [in] */ BOOL fRameWindow);        
	virtual HRESULT STDMETHODCALLTYPE TranslateAccelerator(/* [in] */ LPMSG lpMsg, /* [in] */ const GUID *pguidCmdGroup, /* [in] */ DWORD nCmdID);        
	virtual HRESULT STDMETHODCALLTYPE GetOptionKeyPath(/* [out] */ LPOLESTR *pchKey, /* [in] */ DWORD dw);        
	virtual HRESULT STDMETHODCALLTYPE GetDropTarget(/* [in] */ IDropTarget *pDropTarget, /* [out] */ IDropTarget **ppDropTarget);        
	virtual HRESULT STDMETHODCALLTYPE GetExternal(/* [out] */ IDispatch **ppDispatch);        
	virtual HRESULT STDMETHODCALLTYPE TranslateUrl(/* [in] */ DWORD dwTranslate, /* [in] */ OLECHAR *pchURLIn, /* [out] */ OLECHAR **ppchURLOut);        
	virtual HRESULT STDMETHODCALLTYPE FilterDataObject(/* [in] */ IDataObject *pDO, /* [out] */ IDataObject **ppDORet);

	/////////////////////////////////////////////////////////////////////////////
	// IDocHostUIHandler2
	virtual HRESULT STDMETHODCALLTYPE GetOverrideKeyPath(/* [out] */ LPOLESTR *pchKey, /* [in] */ DWORD dw);

	/////////////////////////////////////////////////////////////////////////////
	// IDocHostShowUI
	virtual HRESULT STDMETHODCALLTYPE ShowMessage(/* [in] */ HWND hwnd, /* [in] */ LPOLESTR lpstrText, /* [in] */ LPOLESTR lpstrCaption, /* [in] */ DWORD dwType, /* [in] */ LPOLESTR lpstrHelpFile, /* [in] */ DWORD dwHelpContext, /* [out] */ LRESULT *plResult);        
	virtual HRESULT STDMETHODCALLTYPE ShowHelp(/* [in] */ HWND hwnd, /* [in] */ LPOLESTR pszHelpFile, /* [in] */ UINT uCommand, /* [in] */ DWORD dwData, /* [in] */ POINT ptMouse, /* [out] */ IDispatch *pDispatchObjectHit);

	/////////////////////////////////////////////////////////////////////////////
	// IInternetSecurityManager
    STDMETHOD(SetSecuritySite)(/* [unique][in] */ IInternetSecurityMgrSite *pSite);
    STDMETHOD(GetSecuritySite)(/* [out] */ IInternetSecurityMgrSite **ppSite);
    STDMETHOD(MapUrlToZone)(/* [in] */ LPCWSTR pwszUrl, /* [out] */ DWORD *pdwZone, /* [in] */ DWORD dwFlags);
    STDMETHOD(GetSecurityId)(/* [in] */ LPCWSTR pwszUrl, /* [size_is][out] */ BYTE *pbSecurityId, /* [out][in] */ DWORD *pcbSecurityId, /* [in] */ DWORD_PTR dwReserved);
    STDMETHOD(ProcessUrlAction)(/* [in] */ LPCWSTR pwszUrl, /* [in] */ DWORD dwAction, /* [size_is][out] */ BYTE *pPolicy, /* [in] */ DWORD cbPolicy, /* [in] */ BYTE *pContext, /* [in] */ DWORD cbContext, /* [in] */ DWORD dwFlags, /* [in] */ DWORD dwReserved);
    STDMETHOD(QueryCustomPolicy)(/* [in] */ LPCWSTR pwszUrl, /* [in] */ REFGUID guidKey, /* [size_is][size_is][out] */ BYTE **ppPolicy, /* [out] */ DWORD *pcbPolicy, /* [in] */ BYTE *pContext, /* [in] */ DWORD cbContext, /* [in] */ DWORD dwReserved);
    STDMETHOD(SetZoneMapping)(/* [in] */ DWORD dwZone, /* [in] */ LPCWSTR lpszPattern, /* [in] */ DWORD dwFlags);
    STDMETHOD(GetZoneMappings)(/* [in] */ DWORD dwZone, /* [out] */ IEnumString **ppenumString, /* [in] */ DWORD dwFlags);

	HRESULT FireAmbientPropertyChange(DISPID dispChanged)
	{
		HRESULT hr = S_OK;
		CComQIPtr<IOleControl, &__uuidof(IOleControl)> spOleControl(m_spUnknown);
		if (spOleControl != NULL)
			hr = spOleControl->OnAmbientPropertyChange(dispChanged);
		return hr;
	}

// IAxWinAmbientDispatch

	CComPtr<IDispatch> m_spAmbientDispatch;

	STDMETHOD(Invoke)(DISPID dispIdMember, REFIID riid, LCID lcid, WORD wFlags, DISPPARAMS *pDispParams,
			VARIANT *pVarResult, EXCEPINFO *pExcepInfo, UINT *puArgErr)
	{
		HRESULT hr = IDispatchImpl<IAxWinAmbientDispatchEx, &__uuidof(IAxWinAmbientDispatchEx), &CAtlModule::m_libid, 0xFFFF, 0xFFFF>::Invoke
			(dispIdMember, riid, lcid, wFlags, pDispParams, pVarResult, pExcepInfo, puArgErr);
		if ((hr == DISP_E_MEMBERNOTFOUND || hr == TYPE_E_ELEMENTNOTFOUND) && m_spAmbientDispatch != NULL)
		{
			hr = m_spAmbientDispatch->Invoke(dispIdMember, riid, lcid, wFlags, pDispParams, pVarResult, pExcepInfo, puArgErr);
			if (SUCCEEDED(hr) && (wFlags & DISPATCH_PROPERTYPUT) != 0)
			{
				hr = FireAmbientPropertyChange(dispIdMember);
			}
		}
		return hr;
	}

	STDMETHOD(put_AllowWindowlessActivation)(VARIANT_BOOL bAllowWindowless)
	{
		m_bCanWindowlessActivate = bAllowWindowless;
		return S_OK;
	}
	STDMETHOD(get_AllowWindowlessActivation)(VARIANT_BOOL* pbAllowWindowless)
	{
		ATLASSERT(pbAllowWindowless != NULL);
		if (pbAllowWindowless == NULL)
			return E_POINTER;

		*pbAllowWindowless = m_bCanWindowlessActivate ? ATL_VARIANT_TRUE : ATL_VARIANT_FALSE;
		return S_OK;
	}
	STDMETHOD(put_BackColor)(OLE_COLOR clrBackground)
	{
		m_clrBackground = clrBackground;
		FireAmbientPropertyChange(DISPID_AMBIENT_BACKCOLOR);
		InvalidateRect(0, FALSE);
		return S_OK;
	}
	STDMETHOD(get_BackColor)(OLE_COLOR* pclrBackground)
	{
		ATLASSERT(pclrBackground != NULL);
		if (pclrBackground == NULL)
			return E_POINTER;

		*pclrBackground = m_clrBackground;
		return S_OK;
	}
	STDMETHOD(put_ForeColor)(OLE_COLOR clrForeground)
	{
		m_clrForeground = clrForeground;
		FireAmbientPropertyChange(DISPID_AMBIENT_FORECOLOR);
		return S_OK;
	}
	STDMETHOD(get_ForeColor)(OLE_COLOR* pclrForeground)
	{
		ATLASSERT(pclrForeground != NULL);
		if (pclrForeground == NULL)
			return E_POINTER;

		*pclrForeground = m_clrForeground;
		return S_OK;
	}
	STDMETHOD(put_LocaleID)(LCID lcidLocaleID)
	{
		m_lcidLocaleID = lcidLocaleID;
		FireAmbientPropertyChange(DISPID_AMBIENT_LOCALEID);
		return S_OK;
	}
	STDMETHOD(get_LocaleID)(LCID* plcidLocaleID)
	{
		ATLASSERT(plcidLocaleID != NULL);
		if (plcidLocaleID == NULL)
			return E_POINTER;

		*plcidLocaleID = m_lcidLocaleID;
		return S_OK;
	}
	STDMETHOD(put_UserMode)(VARIANT_BOOL bUserMode)
	{
		m_bUserMode = bUserMode;
		FireAmbientPropertyChange(DISPID_AMBIENT_USERMODE);
		return S_OK;
	}
	STDMETHOD(get_UserMode)(VARIANT_BOOL* pbUserMode)
	{
		ATLASSERT(pbUserMode != NULL);
		if (pbUserMode == NULL)
			return E_POINTER;

		*pbUserMode = m_bUserMode ? ATL_VARIANT_TRUE : ATL_VARIANT_FALSE;
		return S_OK;
	}
	STDMETHOD(put_DisplayAsDefault)(VARIANT_BOOL bDisplayAsDefault)
	{
		m_bDisplayAsDefault = bDisplayAsDefault;
		FireAmbientPropertyChange(DISPID_AMBIENT_DISPLAYASDEFAULT);
		return S_OK;
	}
	STDMETHOD(get_DisplayAsDefault)(VARIANT_BOOL* pbDisplayAsDefault)
	{
		ATLASSERT(pbDisplayAsDefault != NULL);
		if (pbDisplayAsDefault == NULL)
			return E_POINTER;

		*pbDisplayAsDefault = m_bDisplayAsDefault ? ATL_VARIANT_TRUE : ATL_VARIANT_FALSE;
		return S_OK;
	}
	STDMETHOD(put_Font)(IFontDisp* pFont)
	{
		m_spFont = pFont;
		FireAmbientPropertyChange(DISPID_AMBIENT_FONT);
		return S_OK;
	}
	STDMETHOD(get_Font)(IFontDisp** pFont)
	{
		ATLASSERT(pFont != NULL);
		if (pFont == NULL)
			return E_POINTER;
		*pFont = NULL;

		if (m_spFont == NULL)
		{
			USES_CONVERSION;
			HFONT hSystemFont = (HFONT) GetStockObject(DEFAULT_GUI_FONT);
			if (hSystemFont == NULL)
				hSystemFont = (HFONT) GetStockObject(SYSTEM_FONT);
			if (hSystemFont == NULL)
				return AtlHresultFromLastError();
			LOGFONT logfont;
			GetObject(hSystemFont, sizeof(logfont), &logfont);
			FONTDESC fd;
			fd.cbSizeofstruct = sizeof(FONTDESC);
			fd.lpstrName = T2OLE(logfont.lfFaceName);
			fd.sWeight = (short)logfont.lfWeight;
			fd.sCharset = logfont.lfCharSet;
			fd.fItalic = logfont.lfItalic;
			fd.fUnderline = logfont.lfUnderline;
			fd.fStrikethrough = logfont.lfStrikeOut;

			long lfHeight = logfont.lfHeight;
			if (lfHeight < 0)
				lfHeight = -lfHeight;

			int ppi;
			HDC hdc;
			if (m_hWnd)
			{
				hdc = ::GetDC(m_hWnd);
				if (hdc == NULL)
					return AtlHresultFromLastError();
				ppi = GetDeviceCaps(hdc, LOGPIXELSY);
				::ReleaseDC(m_hWnd, hdc);
			}
			else
			{
				hdc = ::GetDC(GetDesktopWindow());
				if (hdc == NULL)
					return AtlHresultFromLastError();
				ppi = GetDeviceCaps(hdc, LOGPIXELSY);
				::ReleaseDC(GetDesktopWindow(), hdc);
			}
			fd.cySize.Lo = lfHeight * 720000 / ppi;
			fd.cySize.Hi = 0;

			OleCreateFontIndirect(&fd, __uuidof(IFontDisp), (void**) &m_spFont);
		}

		return m_spFont.CopyTo(pFont);
	}
	STDMETHOD(put_MessageReflect)(VARIANT_BOOL bMessageReflect)
	{
		m_bMessageReflect = bMessageReflect;
		FireAmbientPropertyChange(DISPID_AMBIENT_MESSAGEREFLECT);
		return S_OK;
	}
	STDMETHOD(get_MessageReflect)(VARIANT_BOOL* pbMessageReflect)
	{

		ATLASSERT(pbMessageReflect != NULL);
		if (pbMessageReflect == NULL)
			return E_POINTER;

		*pbMessageReflect = m_bMessageReflect ? ATL_VARIANT_TRUE : ATL_VARIANT_FALSE;
		return S_OK;
	}
	STDMETHOD(get_ShowGrabHandles)(VARIANT_BOOL* pbShowGrabHandles)
	{
		*pbShowGrabHandles = ATL_VARIANT_FALSE;
		return S_OK;
	}
	STDMETHOD(get_ShowHatching)(VARIANT_BOOL* pbShowHatching)
	{
		ATLASSERT(pbShowHatching != NULL);
		if (pbShowHatching == NULL)
			return E_POINTER;

		*pbShowHatching = ATL_VARIANT_FALSE;
		return S_OK;
	}
	STDMETHOD(put_DocHostFlags)(DWORD dwDocHostFlags)
	{
		m_dwDocHostFlags = dwDocHostFlags;
		FireAmbientPropertyChange(DISPID_UNKNOWN);
		return S_OK;
	}
	STDMETHOD(get_DocHostFlags)(DWORD* pdwDocHostFlags)
	{
		ATLASSERT(pdwDocHostFlags != NULL);
		if (pdwDocHostFlags == NULL)
			return E_POINTER;

		*pdwDocHostFlags = m_dwDocHostFlags;
		return S_OK;
	}
	STDMETHOD(put_DocHostDoubleClickFlags)(DWORD dwDocHostDoubleClickFlags)
	{
		m_dwDocHostDoubleClickFlags = dwDocHostDoubleClickFlags;
		return S_OK;
	}
	STDMETHOD(get_DocHostDoubleClickFlags)(DWORD* pdwDocHostDoubleClickFlags)
	{
		ATLASSERT(pdwDocHostDoubleClickFlags != NULL);
		if (pdwDocHostDoubleClickFlags == NULL)
			return E_POINTER;

		*pdwDocHostDoubleClickFlags = m_dwDocHostDoubleClickFlags;
		return S_OK;
	}
	STDMETHOD(put_AllowContextMenu)(VARIANT_BOOL bAllowContextMenu)
	{
		m_bAllowContextMenu = bAllowContextMenu;
		return S_OK;
	}
	STDMETHOD(get_AllowContextMenu)(VARIANT_BOOL* pbAllowContextMenu)
	{
		ATLASSERT(pbAllowContextMenu != NULL);
		if (pbAllowContextMenu == NULL)
			return E_POINTER;

		*pbAllowContextMenu = m_bAllowContextMenu ? ATL_VARIANT_TRUE : ATL_VARIANT_FALSE;
		return S_OK;
	}
	STDMETHOD(put_AllowShowUI)(VARIANT_BOOL bAllowShowUI)
	{
		m_bAllowShowUI = bAllowShowUI;
		return S_OK;
	}
	STDMETHOD(get_AllowShowUI)(VARIANT_BOOL* pbAllowShowUI)
	{
		ATLASSERT(pbAllowShowUI != NULL);
		if (pbAllowShowUI == NULL)
			return E_POINTER;

		*pbAllowShowUI = m_bAllowShowUI ? ATL_VARIANT_TRUE : ATL_VARIANT_FALSE;
		return S_OK;
	}
	STDMETHOD(put_OptionKeyPath)(BSTR bstrOptionKeyPath)
	{
		m_bstrOptionKeyPath = bstrOptionKeyPath;;
		return S_OK;
	}
	STDMETHOD(get_OptionKeyPath)(BSTR* pbstrOptionKeyPath)
	{
		ATLASSERT(pbstrOptionKeyPath != NULL);
		if (pbstrOptionKeyPath == NULL)
			return E_POINTER;

		*pbstrOptionKeyPath = m_bstrOptionKeyPath;
		return S_OK;
	}

	STDMETHOD(SetAmbientDispatch)(IDispatch* pDispatch)
	{
		m_spAmbientDispatch = pDispatch;
		return S_OK;
	}

// IObjectWithSite
	STDMETHOD(SetSite)(IUnknown* pUnkSite)
	{
		HRESULT hr = IObjectWithSiteImpl<CMshtmlHostWindow>::SetSite(pUnkSite);

		if (SUCCEEDED(hr) && m_spUnkSite)
		{
			// Look for "outer" IServiceProvider
			hr = m_spUnkSite->QueryInterface(__uuidof(IServiceProvider), (void**)&m_spServices);
			ATLASSERT( !hr && "No ServiceProvider!" );
		}

		if (pUnkSite == NULL)
			m_spServices.Release();

		return hr;
	}

// IOleClientSite
	STDMETHOD(SaveObject)()
	{
		ATLTRACENOTIMPL(_T("IOleClientSite::SaveObject"));
	}
	STDMETHOD(GetMoniker)(DWORD /*dwAssign*/, DWORD /*dwWhichMoniker*/, IMoniker** /*ppmk*/)
	{
		ATLTRACENOTIMPL(_T("IOleClientSite::GetMoniker"));
	}
	STDMETHOD(GetContainer)(IOleContainer** ppContainer)
	{
		ATLTRACE2(atlTraceHosting, 2, _T("IOleClientSite::GetContainer\n"));
		ATLASSERT(ppContainer != NULL);

		HRESULT hr = E_POINTER;
		if (ppContainer)
		{
			hr = E_NOTIMPL;
			(*ppContainer) = NULL;
			if (m_spUnkSite)
				hr = m_spUnkSite->QueryInterface(__uuidof(IOleContainer), (void**)ppContainer);
			if (FAILED(hr))
				hr = QueryInterface(__uuidof(IOleContainer), (void**)ppContainer);
		}
		return hr;
	}
	STDMETHOD(ShowObject)()
	{
		ATLTRACE2(atlTraceHosting, 2, _T("IOleClientSite::ShowObject\r\n"));

		HDC hdc = CWindowImpl<CMshtmlHostWindow>::GetDC();
		if (hdc == NULL)
			return E_FAIL;
		if (m_spViewObject)
			m_spViewObject->Draw(DVASPECT_CONTENT, -1, NULL, NULL, NULL, hdc, (RECTL*)&m_rcPos, (RECTL*)&m_rcPos, NULL, NULL); 
		CWindowImpl<CMshtmlHostWindow>::ReleaseDC(hdc);
		return S_OK;
	}
	STDMETHOD(OnShowWindow)(BOOL /*fShow*/)
	{
		ATLTRACENOTIMPL(_T("IOleClientSite::OnShowWindow"));
	}
	STDMETHOD(RequestNewObjectLayout)()
	{
		ATLTRACENOTIMPL(_T("IOleClientSite::RequestNewObjectLayout"));
	}

// IOleInPlaceSite
	STDMETHOD(GetWindow)(HWND* phwnd)
	{
		*phwnd = m_hWnd;
		return S_OK;
	}
	STDMETHOD(ContextSensitiveHelp)(BOOL /*fEnterMode*/)
	{
		ATLTRACENOTIMPL(_T("IOleInPlaceSite::ContextSensitiveHelp"));
	}
	STDMETHOD(CanInPlaceActivate)()
	{
		return S_OK;
	}
	STDMETHOD(OnInPlaceActivate)()
	{
		// should only be called once the first time control is inplace-activated
		ATLASSERT(m_bInPlaceActive == FALSE);
		ATLASSERT(m_spInPlaceObjectWindowless == NULL);

		m_bInPlaceActive = TRUE;
		OleLockRunning(m_spOleObject, TRUE, FALSE);
		m_bWindowless = FALSE;
		m_spOleObject->QueryInterface(__uuidof(IOleInPlaceObject), (void**) &m_spInPlaceObjectWindowless);
		return S_OK;
	}
	STDMETHOD(OnUIActivate)()
	{
		ATLTRACE2(atlTraceHosting, 2, _T("IOleInPlaceSite::OnUIActivate\n"));
		m_bUIActive = TRUE;
		return S_OK;
	}
	STDMETHOD(GetWindowContext)(IOleInPlaceFrame** ppFrame, IOleInPlaceUIWindow** ppDoc, LPRECT lprcPosRect, LPRECT lprcClipRect, LPOLEINPLACEFRAMEINFO pFrameInfo)
	{
		if (ppFrame != NULL)
			*ppFrame = NULL;
		if (ppDoc != NULL)
			*ppDoc = NULL;
		if (ppFrame == NULL || ppDoc == NULL || lprcPosRect == NULL || lprcClipRect == NULL)
		{
			ATLASSERT(false);
			return E_POINTER;
		}

		if (!m_spInPlaceFrame)
		{
			CComObject<CAxFrameWindow>* pFrameWindow;
			CComObject<CAxFrameWindow>::CreateInstance(&pFrameWindow);
			pFrameWindow->QueryInterface(__uuidof(IOleInPlaceFrame), (void**) &m_spInPlaceFrame);
			ATLASSERT(m_spInPlaceFrame);
		}
		if (!m_spInPlaceUIWindow)
		{
			CComObject<CAxUIWindow>* pUIWindow;
			CComObject<CAxUIWindow>::CreateInstance(&pUIWindow);
			pUIWindow->QueryInterface(__uuidof(IOleInPlaceUIWindow), (void**) &m_spInPlaceUIWindow);
			ATLASSERT(m_spInPlaceUIWindow);
		}
		m_spInPlaceFrame.CopyTo(ppFrame);
		m_spInPlaceUIWindow.CopyTo(ppDoc);
		GetClientRect(lprcPosRect);
		GetClientRect(lprcClipRect);

		if (m_hAccel == NULL)
		{
			ACCEL ac = { 0,0,0 };
			m_hAccel = CreateAcceleratorTable(&ac, 1);
		}
		pFrameInfo->cb = sizeof(OLEINPLACEFRAMEINFO);
		pFrameInfo->fMDIApp = m_bMDIApp;
		pFrameInfo->hwndFrame = GetParent();
		pFrameInfo->haccel = m_hAccel;
		pFrameInfo->cAccelEntries = (m_hAccel != NULL) ? 1 : 0;

		return S_OK;
	}
	STDMETHOD(Scroll)(SIZE /*scrollExtant*/)
	{
		ATLTRACENOTIMPL(_T("IOleInPlaceSite::Scroll"));
	}
	STDMETHOD(OnUIDeactivate)(BOOL /*fUndoable*/)
	{
		ATLTRACE2(atlTraceHosting, 2, _T("IOleInPlaceSite::OnUIDeactivate\n"));
		m_bUIActive = FALSE;
		return S_OK;
	}
	STDMETHOD(OnInPlaceDeactivate)()
	{
		m_bInPlaceActive = FALSE;
		m_spInPlaceObjectWindowless.Release();
		return S_OK;
	}
	STDMETHOD(DiscardUndoState)()
	{
		ATLTRACENOTIMPL(_T("IOleInPlaceSite::DiscardUndoState"));
	}
	STDMETHOD(DeactivateAndUndo)()
	{
		ATLTRACENOTIMPL(_T("IOleInPlaceSite::DeactivateAndUndo"));
	}
	STDMETHOD(OnPosRectChange)(LPCRECT /*lprcPosRect*/)
	{
		ATLTRACENOTIMPL(_T("IOleInPlaceSite::OnPosRectChange"));
	}

// IOleInPlaceSiteEx
	STDMETHOD(OnInPlaceActivateEx)(BOOL* /*pfNoRedraw*/, DWORD dwFlags)
	{
		// should only be called once the first time control is inplace-activated
		ATLASSERT(m_bInPlaceActive == FALSE);
		ATLASSERT(m_spInPlaceObjectWindowless == NULL);

		m_bInPlaceActive = TRUE;
		OleLockRunning(m_spOleObject, TRUE, FALSE);
		HRESULT hr = E_FAIL;
		if (dwFlags & ACTIVATE_WINDOWLESS)
		{
			m_bWindowless = TRUE;
			hr = m_spOleObject->QueryInterface(__uuidof(IOleInPlaceObjectWindowless), (void**) &m_spInPlaceObjectWindowless);
		}
		if (FAILED(hr))
		{
			m_bWindowless = FALSE;
			hr = m_spOleObject->QueryInterface(__uuidof(IOleInPlaceObject), (void**) &m_spInPlaceObjectWindowless);
		}
		if (m_spInPlaceObjectWindowless)
			m_spInPlaceObjectWindowless->SetObjectRects(&m_rcPos, &m_rcPos);
		return S_OK;
	}
	STDMETHOD(OnInPlaceDeactivateEx)(BOOL /*fNoRedraw*/)
	{
		m_bInPlaceActive = FALSE;
		m_spInPlaceObjectWindowless.Release();
		return S_OK;
	}
	STDMETHOD(RequestUIActivate)()
	{
		return S_OK;
	}

// IOleInPlaceSiteWindowless
	HDC m_hDCScreen;
	bool m_bDCReleased;

	STDMETHOD(CanWindowlessActivate)()
	{
		return m_bCanWindowlessActivate ? S_OK : S_FALSE;
	}
	STDMETHOD(GetCapture)()
	{
		return m_bCapture ? S_OK : S_FALSE;
	}
	STDMETHOD(SetCapture)(BOOL fCapture)
	{
		if (fCapture)
		{
			CWindow::SetCapture();
			m_bCapture = TRUE;
		}
		else
		{
			ReleaseCapture();
			m_bCapture = FALSE;
		}
		return S_OK;
	}
	STDMETHOD(GetFocus)()
	{
		return m_bHaveFocus ? S_OK : S_FALSE;
	}
	STDMETHOD(SetFocus)(BOOL fGotFocus)
	{
		m_bHaveFocus = fGotFocus;
		return S_OK;
	}
	STDMETHOD(GetDC)(LPCRECT /*pRect*/, DWORD grfFlags, HDC* phDC)
	{
		if (phDC == NULL)
			return E_POINTER;
		if (!m_bDCReleased)
			return E_FAIL;

		*phDC = CWindowImpl<CMshtmlHostWindow>::GetDC();
		if (*phDC == NULL)
			return E_FAIL;

		m_bDCReleased = false;

		if (grfFlags & OLEDC_NODRAW)
			return S_OK;

		RECT rect;
		GetClientRect(&rect);
		if (grfFlags & OLEDC_OFFSCREEN)
		{
			HDC hDCOffscreen = CreateCompatibleDC(*phDC);
			if (hDCOffscreen != NULL)
			{
				HBITMAP hBitmap = CreateCompatibleBitmap(*phDC, rect.right - rect.left, rect.bottom - rect.top);
				if (hBitmap == NULL)
					DeleteDC(hDCOffscreen);
				else
				{
					HGDIOBJ hOldBitmap = SelectObject(hDCOffscreen, hBitmap);
					if (hOldBitmap == NULL)
					{
						DeleteObject(hBitmap);
						DeleteDC(hDCOffscreen);
					}
					else
					{
						DeleteObject(hOldBitmap);
						m_hDCScreen = *phDC;
						*phDC = hDCOffscreen;
					}
				}
			}
		}

		if (grfFlags & OLEDC_PAINTBKGND)
			::FillRect(*phDC, &rect, (HBRUSH) (COLOR_WINDOW+1));
		return S_OK;
	}
	STDMETHOD(ReleaseDC)(HDC hDC)
	{
		m_bDCReleased = true;
		if (m_hDCScreen != NULL)
		{
			RECT rect;
			GetClientRect(&rect);
			// Offscreen DC has to be copied to screen DC before releasing the screen dc;
			BitBlt(m_hDCScreen, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, hDC, 0, 0, SRCCOPY);
			DeleteDC(hDC);
			hDC = m_hDCScreen;
		}

		CWindowImpl<CMshtmlHostWindow>::ReleaseDC(hDC);
		return S_OK;
	}
	STDMETHOD(InvalidateRect)(LPCRECT pRect, BOOL fErase)
	{
		CWindowImpl<CMshtmlHostWindow>::InvalidateRect(pRect, fErase);
		return S_OK;
	}
	STDMETHOD(InvalidateRgn)(HRGN hRGN, BOOL fErase)
	{
		CWindowImpl<CMshtmlHostWindow>::InvalidateRgn(hRGN, fErase);
		return S_OK;
	}
	STDMETHOD(ScrollRect)(INT /*dx*/, INT /*dy*/, LPCRECT /*pRectScroll*/, LPCRECT /*pRectClip*/)
	{
		return S_OK;
	}
	STDMETHOD(AdjustRect)(LPRECT /*prc*/)
	{
		return S_OK;
	}
	STDMETHOD(OnDefWindowMessage)(UINT msg, WPARAM wParam, LPARAM lParam, LRESULT* plResult)
	{
		*plResult = DefWindowProc(msg, wParam, lParam);
		return S_OK;
	}

// IOleControlSite
	STDMETHOD(OnControlInfoChanged)()
	{
		return S_OK;
	}
	STDMETHOD(LockInPlaceActive)(BOOL /*fLock*/)
	{
		return S_OK;
	}
	STDMETHOD(GetExtendedControl)(IDispatch** ppDisp)
	{
		if (ppDisp == NULL)
			return E_POINTER;
		return m_spOleObject.QueryInterface(ppDisp);
	}
	STDMETHOD(TransformCoords)(POINTL* /*pPtlHimetric*/, POINTF* /*pPtfContainer*/, DWORD /*dwFlags*/)
	{
		ATLTRACENOTIMPL(_T("CMshtmlHostWindow::TransformCoords"));
	}
	STDMETHOD(TranslateAccelerator)(LPMSG /*lpMsg*/, DWORD /*grfModifiers*/)
	{
		return S_FALSE;
	}
	STDMETHOD(OnFocus)(BOOL fGotFocus)
	{
		m_bHaveFocus = fGotFocus;
		return S_OK;
	}
	STDMETHOD(ShowPropertyFrame)()
	{
		ATLTRACENOTIMPL(_T("CMshtmlHostWindow::ShowPropertyFrame"));
	}

// IAdviseSink
	STDMETHOD_(void, OnDataChange)(FORMATETC* /*pFormatetc*/, STGMEDIUM* /*pStgmed*/)
	{
	}
	STDMETHOD_(void, OnViewChange)(DWORD /*dwAspect*/, LONG /*lindex*/)
	{
	}
	STDMETHOD_(void, OnRename)(IMoniker* /*pmk*/)
	{
	}
	STDMETHOD_(void, OnSave)()
	{
	}
	STDMETHOD_(void, OnClose)()
	{
	}

// IOleContainer
	STDMETHOD(ParseDisplayName)(IBindCtx* /*pbc*/, LPOLESTR /*pszDisplayName*/, ULONG* /*pchEaten*/, IMoniker** /*ppmkOut*/)
	{
		ATLTRACENOTIMPL(_T("CMshtmlHostWindow::ParseDisplayName"));
	}
	STDMETHOD(EnumObjects)(DWORD /*grfFlags*/, IEnumUnknown** ppenum)
	{
		if (ppenum == NULL)
			return E_POINTER;
		*ppenum = NULL;
		typedef CComObject<CComEnum<IEnumUnknown, &__uuidof(IEnumUnknown), IUnknown*, _CopyInterface<IUnknown> > > enumunk;
		enumunk* p = NULL;
		ATLTRY(p = new enumunk);
		if(p == NULL)
			return E_OUTOFMEMORY;
		IUnknown* pTemp = m_spUnknown;
		// There is always only one object.
		HRESULT hRes = p->Init(reinterpret_cast<IUnknown**>(&pTemp), reinterpret_cast<IUnknown**>(&pTemp + 1), GetControllingUnknown(), AtlFlagCopy);
		if (SUCCEEDED(hRes))
			hRes = p->QueryInterface(__uuidof(IEnumUnknown), (void**)ppenum);
		if (FAILED(hRes))
			delete p;
		return hRes;
	}
	STDMETHOD(LockContainer)(BOOL fLock)
	{
		m_bLocked = fLock;
		return S_OK;
	}

	HRESULT ActivateAx(IUnknown* pUnkControl, bool bInited, IStream* pStream)
	{
		if (pUnkControl == NULL)
			return S_OK;

		m_spUnknown = pUnkControl;

		HRESULT hr = S_OK;
		pUnkControl->QueryInterface(__uuidof(IOleObject), (void**)&m_spOleObject);
		if (m_spOleObject)
		{
			m_spOleObject->GetMiscStatus(DVASPECT_CONTENT, &m_dwMiscStatus);
			if (m_dwMiscStatus & OLEMISC_SETCLIENTSITEFIRST)
			{
				CComQIPtr<IOleClientSite> spClientSite(GetControllingUnknown());
				m_spOleObject->SetClientSite(spClientSite);
			}

			if (!bInited) // If user hasn't initialized the control, initialize/load using IPersistStreamInit or IPersistStream
			{
				CComQIPtr<IPersistStreamInit> spPSI(m_spOleObject);
				if (spPSI)
				{
					if (pStream)
						hr = spPSI->Load(pStream);
					else
						hr = spPSI->InitNew();
				}
				else if (pStream)
				{
					CComQIPtr<IPersistStream> spPS(m_spOleObject);
					if (spPS)
						hr = spPS->Load(pStream);
				}

				if (FAILED(hr)) // If the initialization of the control failed...
				{
					// Clean up and return
					if (m_dwMiscStatus & OLEMISC_SETCLIENTSITEFIRST)
						m_spOleObject->SetClientSite(NULL);

					m_dwMiscStatus = 0;
					m_spOleObject.Release();
					m_spUnknown.Release();

					return hr;
				}
			}

			if (0 == (m_dwMiscStatus & OLEMISC_SETCLIENTSITEFIRST))
			{
				CComQIPtr<IOleClientSite> spClientSite(GetControllingUnknown());
				m_spOleObject->SetClientSite(spClientSite);
			}

			m_dwViewObjectType = 0;
			hr = m_spOleObject->QueryInterface(__uuidof(IViewObjectEx), (void**) &m_spViewObject);
			if (FAILED(hr))
			{
				hr = m_spOleObject->QueryInterface(__uuidof(IViewObject2), (void**) &m_spViewObject);
				if (SUCCEEDED(hr))
					m_dwViewObjectType = 3;
			} else
				m_dwViewObjectType = 7;

			if (FAILED(hr))
			{
				hr = m_spOleObject->QueryInterface(__uuidof(IViewObject), (void**) &m_spViewObject);
				if (SUCCEEDED(hr))
					m_dwViewObjectType = 1;
			}
			CComQIPtr<IAdviseSink> spAdviseSink(GetControllingUnknown());
			m_spOleObject->Advise(spAdviseSink, &m_dwOleObject);
			if (m_spViewObject)
				m_spViewObject->SetAdvise(DVASPECT_CONTENT, 0, spAdviseSink);
			m_spOleObject->SetHostNames(OLESTR("AXWIN"), NULL);

			if ((m_dwMiscStatus & OLEMISC_INVISIBLEATRUNTIME) == 0)
			{
				GetClientRect(&m_rcPos);
				m_pxSize.cx = m_rcPos.right - m_rcPos.left;
				m_pxSize.cy = m_rcPos.bottom - m_rcPos.top;
				AtlPixelToHiMetric(&m_pxSize, &m_hmSize);
				m_spOleObject->SetExtent(DVASPECT_CONTENT, &m_hmSize);
				m_spOleObject->GetExtent(DVASPECT_CONTENT, &m_hmSize);
				AtlHiMetricToPixel(&m_hmSize, &m_pxSize);
				m_rcPos.right = m_rcPos.left + m_pxSize.cx;
				m_rcPos.bottom = m_rcPos.top + m_pxSize.cy;

				CComQIPtr<IOleClientSite> spClientSite(GetControllingUnknown());
				hr = m_spOleObject->DoVerb(OLEIVERB_INPLACEACTIVATE, NULL, spClientSite, 0, m_hWnd, &m_rcPos);
				RedrawWindow(NULL, NULL, RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASE | RDW_INTERNALPAINT | RDW_FRAME);
			}
		}
		CComPtr<IObjectWithSite> spSite;
		pUnkControl->QueryInterface(__uuidof(IObjectWithSite), (void**)&spSite);
		if (spSite != NULL)
			spSite->SetSite(GetControllingUnknown());

		return hr;
	}


// pointers
	CComPtr<IUnknown> m_spUnknown;
	CComPtr<IOleObject> m_spOleObject;
	CComPtr<IOleInPlaceFrame> m_spInPlaceFrame;
	CComPtr<IOleInPlaceUIWindow> m_spInPlaceUIWindow;
	CComPtr<IViewObjectEx> m_spViewObject;
	CComPtr<IOleInPlaceObjectWindowless> m_spInPlaceObjectWindowless;
	CComPtr<IDispatch> m_spExternalDispatch;
	CComPtr<IDocHostUIHandlerDispatch> m_spIDocHostUIHandlerDispatch;
	IID m_iidSink;
	DWORD m_dwViewObjectType;
	DWORD m_dwAdviseSink;

// state
	unsigned long m_bInPlaceActive:1;
	unsigned long m_bUIActive:1;
	unsigned long m_bMDIApp:1;
	unsigned long m_bWindowless:1;
	unsigned long m_bCapture:1;
	unsigned long m_bHaveFocus:1;
	unsigned long m_bReleaseAll:1;
	unsigned long m_bLocked:1;

	DWORD m_dwOleObject;
	DWORD m_dwMiscStatus;
	SIZEL m_hmSize;
	SIZEL m_pxSize;
	RECT m_rcPos;

	// Accelerator table
	HACCEL m_hAccel;

	// Ambient property storage
	unsigned long m_bCanWindowlessActivate:1;
	unsigned long m_bUserMode:1;
	unsigned long m_bDisplayAsDefault:1;
	unsigned long m_bMessageReflect:1;
	unsigned long m_bSubclassed:1;
	unsigned long m_bAllowContextMenu:1;
	unsigned long m_bAllowShowUI:1;
	OLE_COLOR m_clrBackground;
	OLE_COLOR m_clrForeground;
	LCID m_lcidLocaleID;
	CComPtr<IFontDisp> m_spFont;
	CComPtr<IServiceProvider>  m_spServices;
	DWORD m_dwDocHostFlags;
	DWORD m_dwDocHostDoubleClickFlags;
	CComBSTR m_bstrOptionKeyPath;

	void SubclassWindow(HWND hWnd)
	{
		m_bSubclassed = CWindowImpl<CMshtmlHostWindow>::SubclassWindow(hWnd);
	}

	void ReleaseWindow()
	{
		if (m_bSubclassed)
		{
			if(UnsubclassWindow(TRUE) != NULL)
				m_bSubclassed = FALSE;
		}
		else
			DestroyWindow();
	}

	// Reflection
	LRESULT ReflectNotifications(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
	{
		HWND hWndChild = NULL;

		switch(uMsg)
		{
		case WM_COMMAND:
			if(lParam != NULL)	// not from a menu
				hWndChild = (HWND)lParam;
			break;
		case WM_NOTIFY:
			hWndChild = ((LPNMHDR)lParam)->hwndFrom;
			break;
		case WM_PARENTNOTIFY:
			DefWindowProc();
			switch(LOWORD(wParam))
			{
			case WM_CREATE:
			case WM_DESTROY:
				hWndChild = (HWND)lParam;
				break;
			default:
				hWndChild = GetDlgItem(HIWORD(wParam));
				break;
			}
			break;
		case WM_DRAWITEM:
			{
				DRAWITEMSTRUCT* pdis = ((LPDRAWITEMSTRUCT)lParam);
				if (pdis->CtlType != ODT_MENU)	// not from a menu
					hWndChild = pdis->hwndItem;
				else							// Status bar control sends this message with type set to ODT_MENU
					if (::IsWindow(pdis->hwndItem))
						hWndChild = pdis->hwndItem;
			}
			break;
		case WM_MEASUREITEM:
			{
				MEASUREITEMSTRUCT* pmis = ((LPMEASUREITEMSTRUCT)lParam);
				if(pmis->CtlType != ODT_MENU)	// not from a menu
					hWndChild = GetDlgItem(pmis->CtlID);
			}
			break;
		case WM_COMPAREITEM:
				// Sent only by combo or list box
				hWndChild = GetDlgItem(((LPCOMPAREITEMSTRUCT)lParam)->CtlID);
			break;
		case WM_DELETEITEM:
				// Sent only by combo or list box
				hWndChild = GetDlgItem(((LPDELETEITEMSTRUCT)lParam)->CtlID);
			break;
		case WM_VKEYTOITEM:
		case WM_CHARTOITEM:
		case WM_HSCROLL:
		case WM_VSCROLL:
			hWndChild = (HWND)lParam;
			break;
		case WM_CTLCOLORBTN:
		case WM_CTLCOLORDLG:
		case WM_CTLCOLOREDIT:
		case WM_CTLCOLORLISTBOX:
		case WM_CTLCOLORMSGBOX:
		case WM_CTLCOLORSCROLLBAR:
		case WM_CTLCOLORSTATIC:
			hWndChild = (HWND)lParam;
			break;
		default:
			break;
		}

		if(hWndChild == NULL)
		{
			bHandled = FALSE;
			return 1;
		}

		if (m_bWindowless)
		{
			LRESULT lRes = 0;
			if (m_bInPlaceActive && m_spInPlaceObjectWindowless)
				m_spInPlaceObjectWindowless->OnWindowMessage(OCM__BASE + uMsg, wParam, lParam, &lRes);
			return lRes;
		}

		ATLASSERT(::IsWindow(hWndChild));
		return ::SendMessage(hWndChild, OCM__BASE + uMsg, wParam, lParam);
	}

	STDMETHOD(QueryService)( REFGUID rsid, REFIID riid, void** ppvObj) 
	{
		ATLASSERT(ppvObj != NULL);
		if (ppvObj == NULL)
			return E_POINTER;
		*ppvObj = NULL;

		HRESULT hr = E_NOINTERFACE;
		// Try for service on this object

		if(::InlineIsEqualGUID(rsid, SID_SInternetSecurityManager))
		{
			HRESULT	hr = QueryInterface(riid, ppvObj);
			TRACE(_T("INET_E_DEFAULT_ACTION: SecurityManager requested, hr = %#010X.\r\n"), hr);
			return hr;
		}

		// No services currently

		// If that failed try to find the service on the outer object
		if (FAILED(hr) && m_spServices)
			hr = m_spServices->QueryService(rsid, riid, ppvObj);

		return hr;
	}
};