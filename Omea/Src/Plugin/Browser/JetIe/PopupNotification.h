/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// PopupNotification.h : Declaration of the CPopupNotification
// Implements a popup balloon which notifies user of some action success or failure.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once
#include "CommonResource.h"       // main symbols
#include "Wrappers.h"

// IPopupNotification
#ifdef JETIE_OMEA
[
	object,
	uuid("52E5BB44-CA25-492A-A52D-0944CC166AE9"),
	dual,	helpstring("Internet Explorer Omea Add-on Balloon Notification Interface"),
	pointer_default(unique)
]
#endif
#ifdef JETIE_BEELAXY
[
	object,
	uuid("52E5BB45-CA25-492A-A52D-0944CC166AE9"),
	dual,	helpstring("Internet Explorer Beelaxy Add-on Balloon Notification Interface"),
	pointer_default(unique)
]
#endif
__interface IPopupNotification : IDispatch
{
	[id(1), helpstring("Displays a standard popup notification of the JetIe add-in. Text specifies the main text. Title specifies the popup caption; if emptystring or missing, a default caption will be used. The mood defines color, icon and whether the popup of this mood is displayed; it can be a string (Notify, Warn, Stop), a corresponding number (0, 1, 2), or a missing value for Notify. ParentWindow is the parent window for this popup, either an HWND value as long, or an IOleWindow object, or NULL or missing for no parent. The optional timeout parameter defines the time for which the popup will be displayed; if missing, the default for this mood will be used.")]
	HRESULT Show([in] VARIANT Text, [in, optional] VARIANT Title, [in, optional] VARIANT Mood, [in, optional] VARIANT ParentWindow, [in, optional] VARIANT Timeout);
	[id(2), helpstring("method Close")] HRESULT Close();
};

_COM_SMARTPTR_TYPEDEF(IPopupNotification, __uuidof(IPopupNotification));

// _IPopupNotificationEvents
#ifdef JETIE_OMEA
[
	dispinterface,
	uuid("EFCD76EE-171D-41B6-82E0-1A601F4BD34E"),
	helpstring("Internet Explorer Omea Add-on Balloon Notification Events Interface")
]
#endif
#ifdef JETIE_BEELAXY
[
	dispinterface,
	uuid("EFCD76EF-171D-41B6-82E0-1A601F4BD34E"),
	helpstring("Internet Explorer Beelaxy Add-on Balloon Notification Events Interface")
]
#endif
__interface _IPopupNotificationEvents
{
};

#define POPUP_WINDOW_WIDTH	0x100
#define POPUP_WINDOW_HEIGHT	0x20

// CPopupNotification

#ifdef JETIE_OMEA
[
	coclass,
	threading("apartment"),
	support_error_info("IPopupNotification"),
	event_source("com"),
	vi_progid("IexploreOmea.PopupNotification"),
	progid("IexploreOmea.PopupNotification.1"),
	version(1.0),
	uuid("0B312F99-836C-44EB-8F2D-CB23598A20C8"),
	helpstring("Internet Explorer Omea Add-on Balloon Notification Window")
]
#endif
#ifdef JETIE_BEELAXY
[
	coclass,
	threading("apartment"),
	support_error_info("IPopupNotification"),
	event_source("com"),
	vi_progid("IexploreBeelaxy.PopupNotification"),
	progid("IexploreBeelaxy.PopupNotification.1"),
	version(1.0),
	uuid("0B312F9A-836C-44EB-8F2D-CB23598A20C8"),
	helpstring("Internet Explorer Beelaxy Add-on Balloon Notification Window")
]
#endif
class ATL_NO_VTABLE CPopupNotification : 
	public IPopupNotification,
	public IOleWindow,
	public CWindowImpl<CPopupNotification>
{
public:
	CPopupNotification();
	virtual ~CPopupNotification();

	__event __interface _IPopupNotificationEvents;

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}
	
	void FinalRelease() 
	{
	}

// Types
public:
	/// Possible moods, defines the behavior and appearance of the window, should be determined from the seriousness of an issue. pmNotify is just for information, pmWarn means that a non-fatal error has occured, and pmStop indicates a fatal error.
	typedef enum tagMood { pmNotify, pmWarn, pmStop } Mood;

// Data
protected:

	/// Mood of this popup.
	Mood	m_mood;

	/// After this timeout elapses, the popup should be hidden.
	DWORD	m_dwTimeout;

	/// Message text displayed in the main space of the popup.
	CString	m_sText;

	/// The current level of opacity (used for fadeout). -1 means that not fading out currently.
	int	m_nOpacity;

	/// Main icon displayed in the client area of the notification popup. Depends on the mood.
	HICON	m_iconClient;

	/// Whether the window has already autosized to fit the contents.
	bool	m_bAutosizeCompleted;

	/// Constants
	enum
	{
		TITLE_HEIGHT = 10,	// Height of the window title
		TIMER_EXPIRE = 0x100,	// ID of the timer that controls popup expiration
		TIMER_TRANSPARENCY = 0x101,	// ID of the timer that deferred-applies transparency attributes
		TIMEOUT_ANIMATE = 500,	// Animation timeouts
		TIMEOUT_DEFAULT_NOTIFY = 1000,	// Default timeout for the Notify mood
		TIMEOUT_DEFAULT_WARN = 2000,	// Default timeout for the Warn mood
		TIMEOUT_DEFAULT_STOP = 3000,	// Default timeout for the Error mood
		TIMEOUT_TRANSPARENCY = 100,	// Default timeout for the transparency timer
		TRANSPARENCY = 0xC0,
	};

	/// The main background color of the popup notification.
	COLORREF	m_colorBack;

	/// Number of instances of this class. Should not grow …
	static int	m_nInstances;

	/// Specifies whether mouse pointer is currently inside the popup.
	bool	m_bMouseIn;

	/// Rectangle into which the text should be rendered.
	CRect	m_rcText;

	/// Rectangle into which the icon should be drawn.
	CRect	m_rcIcon;

// Implementation
protected:

	/// Either displays a window as semitransparent or shows it completely opaque.
	void SetTransparent(bool bTransparent);

	/// Checks if the mouse has actually entered the window, and, if yes, acts accordingly.
	/// bNonClient defines whether the message is for client or non-client area of the window.
	/// Returns whether processing succeeded.
	bool OnMouseEnter(bool bNonClient);

	/// Creates a font that should be used for displaying the popup text.
	void GetFont(HDC hDC, CFont &font);

	/// Adjusts the given rectangle so that the given text would fit into it.
	void CalcTextRect(CString sText, CRect &rect);

	/// Performs the layouting of the popup notification window (by setting up the part rects), and adjusts the size to fit the message text, if necessary.
	/// Returns the desired window rect that may resize or move as compared to the initial one.
	CRect PerformLayout();

// Window Infrastructure
public:
DECLARE_WND_CLASS(_T("JetIe Popup Notification Window"))

// Message map
BEGIN_MSG_MAP(CPopupNotification)
	MESSAGE_HANDLER(WM_NCCREATE, OnNcCreate)
	MESSAGE_HANDLER(WM_CREATE, OnCreate)
	MESSAGE_HANDLER(WM_DESTROY, OnDestroy)
	MESSAGE_HANDLER(WM_RBUTTONDOWN, OnRButtonDown)
	MESSAGE_HANDLER(WM_MBUTTONDOWN, OnMButtonDown)
	MESSAGE_HANDLER(WM_ERASEBKGND, OnEraseBackground)
	MESSAGE_HANDLER(WM_PAINT, OnPaint)
	MESSAGE_HANDLER(WM_NCPAINT, OnNcPaint)
	MESSAGE_HANDLER(WM_NCCALCSIZE, OnNcCalcSize)
	MESSAGE_HANDLER(WM_NCHITTEST, OnNcHitTest)
	MESSAGE_HANDLER(WM_TIMER, OnTimer)
	MESSAGE_HANDLER(WM_MOUSELEAVE, OnAnyMouseLeave)
	MESSAGE_HANDLER(WM_NCMOUSELEAVE, OnAnyMouseLeave)
	MESSAGE_HANDLER(WM_MOUSEMOVE, OnMouseMove)
	MESSAGE_HANDLER(WM_NCMOUSEMOVE, OnNcMouseMove)
END_MSG_MAP()

	// Message handlers
	LRESULT OnNcCreate(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnCreate(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnDestroy(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnMButtonDown(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnRButtonDown(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnEraseBackground(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnPaint(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnNcPaint(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnNcCalcSize(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnNcHitTest(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnTimer(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnAnyMouseLeave(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnMouseMove(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnNcMouseMove(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);

	virtual void OnFinalMessage(HWND hWnd);
public:
	// IOleWindow
	STDMETHOD(GetWindow)(HWND * phwnd);
	STDMETHOD(ContextSensitiveHelp)(BOOL fEnterMode);

	// IPopupNotification
	STDMETHOD(Show)(VARIANT Text, VARIANT Title, VARIANT Mood, VARIANT ParentWindow, VARIANT Timeout);
	STDMETHOD(Close)(void);
};

