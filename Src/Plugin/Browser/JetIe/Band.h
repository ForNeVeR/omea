// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// Band.h : Declaration of the CBand
// CBand is a generic implementation of an Internet Explorer Rebar Band.
// This class works in conjunction with the dynamic class factory which
// creates it from one of the CLSIDs known to the Action Manager. Upon
// creation, this object is parameterized with the GUID of control
// family it should be displaying.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once
#include "CommonResource.h"       // main symbols
#include "JetIe.h"
#include "DynamicClassFactory.h"

// IBand
#ifdef JETIE_OMEA
[
	object,
	uuid("EF07FE2E-8F17-4DFA-9716-4F39023B744C"),
	dual,	helpstring("Internet Explorer Omea Add-on Rebar Band Interface"),
	pointer_default(unique)
]
#endif
#ifdef JETIE_BEELAXY
[
	object,
	uuid("EF07FE2F-8F17-4DFA-9716-4F39023B744C"),
	dual,	helpstring("Internet Explorer Beelaxy Add-on Rebar Band Interface"),
	pointer_default(unique)
]
#endif
__interface IBand : IDispatch
{
};

// CBand

#ifdef JETIE_OMEA
[	// TODO: suppres registration of this coclass
	coclass,
	threading("apartment"),
	vi_progid("IexploreOmea.Band"),
	progid("IexploreOmea.Band.1"),
	uuid("6EDCCE69-14A2-4E9F-826B-DC523B82167E"),
	version(1.0),
	helpstring("Internet Explorer Omea Add-on Rebar Band")
]
#endif
#ifdef JETIE_BEELAXY
[	// TODO: suppres registration of this coclass
	coclass,
	threading("apartment"),
	vi_progid("IexploreBeelaxy.Band"),
	progid("IexploreBeelaxy.Band.1"),
	uuid("6EDCCE6A-14A2-4E9F-826B-DC523B82167E"),
	version(1.0),
	helpstring("Internet Explorer Beelaxy Add-on Rebar Band")
]
#endif
class ATL_NO_VTABLE CBand :
	public IObjectWithSiteImpl<CBand>,
	public CWindowImpl<CBand>,
	public IBand,
	public IDeskBand,
	public IInputObject,
	public IPersistStream
{
public:
	CBand();
	virtual ~CBand();

	DECLARE_PROTECT_FINAL_CONSTRUCT()
	DECLARE_CLASSFACTORY_EX( CDynamicClassFactory );

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

// Window facilities
DECLARE_WND_SUPERCLASS(GetWindowClassName(), TOOLBARCLASSNAME)
/*
// Component categories
BEGIN_CATEGORY_MAP(CBand)
    IMPLEMENTED_CATEGORY(CATID_…)
END_CATEGORY_MAP()
*/

// Message map
BEGIN_MSG_MAP(CBand)
	MESSAGE_HANDLER(WM_CREATE, OnCreate)
	MESSAGE_HANDLER(WM_DESTROY, OnDestroy)
	MESSAGE_HANDLER(WM_COMMAND, OnCommand)
	MESSAGE_HANDLER(WM_NOTIFYFORMAT, OnNotifyFormat)
	MESSAGE_HANDLER(WM_MOUSEMOVE, OnMouseMove)
	MESSAGE_HANDLER(WM_LBUTTONDOWN, OnLButtonDown)
	MESSAGE_HANDLER(WM_LBUTTONUP, OnLButtonUp)
	MESSAGE_HANDLER(WM_TIMER, OnTimer)
	MESSAGE_HANDLER(WM_SETFOCUS, OnSetFocus)
	MESSAGE_HANDLER(WM_KILLFOCUS, OnKillFocus)
	MESSAGE_HANDLER(WM_PAINT, OnPaint)
	MESSAGE_HANDLER(WM_SIZE, OnSizeOrSizing)
	MESSAGE_HANDLER(WM_SIZING, OnSizeOrSizing)
	MESSAGE_HANDLER(m_nUpdateControlsMessage, OnUpdateControls);
	NOTIFY_CODE_HANDLER(TBN_GETINFOTIP, OnGetInfoTip)
	NOTIFY_CODE_HANDLER(TTN_NEEDTEXT, OnNeedText)
	NOTIFY_CODE_HANDLER(TBN_DROPDOWN, OnDropDown)
	MESSAGE_HANDLER(WM_NOTIFY, OnNotify)	// This entry must be the last in the list because it intercepts all the toolbar notification messages that are not caught by the above NOTIFY_CODE_HANDLER — all the notification messages must be trapped in either way not to fall down to the base class which will re-emit them in reaction
	//NOTIFY_RANGE_HANDLER(IDS_CONTROLBASE, IDC_CONTROLLIMIT, )
END_MSG_MAP()

	// Message handlers
	LRESULT OnCreate(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnDestroy(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnCommand(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnNotifyFormat(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnMouseMove(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnLButtonDown(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnLButtonUp(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnNotify(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnTimer(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnKillFocus(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnPaint(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnSizeOrSizing(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnSetFocus(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnUpdateControls(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnGetInfoTip(int idCtrl, LPNMHDR pnmh, BOOL& bHandled);
	LRESULT OnNeedText(int idCtrl, LPNMHDR pnmh, BOOL& bHandled);
	LRESULT OnDropDown(int idCtrl, LPNMHDR pnmh, BOOL& bHandled);

// Operations
public:
	/// Assigns the toolbar GUID when creating this class thru a class factory.
	void SetToolbarGuid(GUID guid);

// Implementation
protected:
	/// Identifier of this tool band object.
	DWORD	m_dwBandId;
	/// The mode this band is curently viewed in. See DBIF_VIEWMODE_… constants.
	DWORD	m_dwViewMode;
	/// Handle to the window which is a parent for our band. Filled by SetSite.
	HWND	m_hwndParent;
	/// Handle to the top-most parent window; (in)direct parent of self and m_hwndParent. Set at the same place as m_hwndParent.
	HWND	m_hwndTopmostParent;

	/// Actions and their attributes for this toolbar. This is a snapshot, not a live list, which allows to bind the buttons directly to the control indices.
	XmlElement	m_xmlControls;
	/// Base id for the toolbar controls. Shift from it represents an index in the m_xmlControls controls list.
	enum
	{
		IDC_CONTROLBASE = 0x2456,	// Base for the toolbar control IDs
		IDC_CONTROLLIMIT = IDC_CONTROLBASE + 0x100 - 1,	// The maximum toolbar control ID
		IDIL_ALL = 0x01,	// ID for the button icons image list
		STATE_VISIBLE = 1,	// Flag that indicates that the action's control should be visible
		STATE_ENABLED = 2,	// Flag that indicates that the action's control should be enabled
		STATE_CHECKED = 4,	// Flag that indicates that the action's control should be drawn as checked/pushed
		timerUpdateUI = 0x364,	// Timer that forces controls state to be updated
		timerUpdateUIInterval = 1000,	// Interval between UI updates
	};

	/// Image list that contains the normal icons for toolbar buttons which have all the three images.
	CImageList	m_ilAllNormal;
	/// Image list that contains the hot (hovered/pressed) icons for toolbar buttons which have all the three images.
	CImageList	m_ilAllHot;
	/// Image list that contains the disabled (greyed) icons for toolbar buttons which have all the three images.
	CImageList	m_ilAllDisabled;

	/// Pointer to the browser object that owns the toolbar and that should be asked for the Web page the actions should be applied to.
	SHDocVw::IWebBrowser2Ptr	m_oBrowser;

	/// The ActionManager that defines behavior for the UI controls hosted by this band.
	IRawActionManagerPtr	m_oActionManager;

	/// GUID under which this toolbar is known to the Registry.
	GUID	m_guidToolbar;

	/// The class name of the CBand window.
	static CString	m_sWindowClassName;

	/// A message which is sent to CBand windows in order to reload the UI controls layout.
	static UINT	m_nUpdateControlsMessage;

	/// Stores the time of last automatic update of UI controls in order to prevent the automatic updates from happening too often.
	/// Automatic updates are those not requested explicitly, ie on mouse hovering.
	DWORD	m_dwLastUpdateControls;

	/// The Chevron icon.
	HICON	m_iconChevron;

	/// Indicates whether the mouse is currently captured, and should be released when mouse pointer exits the window bounds.
	bool	m_bMouseCaptured;

	/// Chevron state that affects the painting, see the enum.
	int	m_nChevronState;

	/// Enumeration for the m_nChevronState variable.
	enum { csNormal, csHovered, csPressed };

// Internal operations
protected:

	/// Adds buttons to the toolbar. Non-zero if successful.
	__declspec(nothrow) BOOL CreateControls();

	/// Loads an icon from the resources of either this application (if specified by "%JETIE%,####", where #### stands for a resource identifier), or another dll/exe (as "Path,####"), or an ico file.
	HICON LoadResourceIcon(CString sOrigin);

	/// Updates the controls state (hidden/disabled/pushed/etc).
	__declspec(nothrow) void UpdateControls();

	/// Updates the state of one selected control (hidden/disabled/pushed/etc).
	void UpdateControl(XmlElement xmlControl, int nIndex);

	/// Prepares a structure that can be used for creating a button control. sCache is a place which will hold the string buffer with the button text until the button gets actually submitted to the toolbar via the special message.
	void PrepareButtonControl(XmlElement xmlAction, TBBUTTON &btn, CString &sCache) throw(_com_error);

	/// Sends a Windows message to all the toolbars (including self) that will cause them to reload the UI controls layout and properties from description files and update them on screen.
	void UpdateAllToolbars();

	/// Enumerates the top-level windows in order to find other CBand windows of the same add-in and send them a message, or start enumeration of their child windows.
	/// lParam is the message to be transmitted (as passed to the EnumWindows function).
	static BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam);

	/// Enumerates the child windows of a particular top-level window in order to find other CBand windows of the same add-in and send them a message.
	/// lParam is the message to be transmitted (as passed to the EnumWindows function).
	static BOOL CALLBACK EnumChildWindowsProc(HWND hwnd, LPARAM lParam);

	/// The UpdateUI timer has ticked. Update the controls state (if we're the active application).
	void OnTimerUpdateUI();

	/// Gets whether the chevron is currently visible on the toolbar.
	__declspec(property(get=GetChevronVisible))	bool ChevronVisible;
	bool GetChevronVisible();

	/// Gets the chevron bounding rectange. Its width is equal to the width of chevron, and height is the full toolbar height.
	__declspec(property(get=GetChevronBounds)) CRect ChevronBounds;
	CRect GetChevronBounds();

	/// Returns the chevron bounds as if it were visible, unlike the ChevronBounds, that hands out an empty rect when the chevron is hidden.
	__declspec(nothrow) CRect GetDefaultChevronBounds();

// Operations
public:
	/// Throws a _com_error exception with an HRESULT and IErrorInfo which resolves to the error text specified. May be used by classes that do not have their own IErrorInfo, but wish to issue meaningful error messages. Also traces the error text to the standard debug output.
	void ThrowError(CStringW sError) throw(_com_error)
	{ TRACE(L"" + sError + L"\n"); _com_issue_errorex(Error(sError), static_cast<IBand*>(this), __uuidof(IBand)); }

	/// Returns class name of the CBand window.
	static CString GetWindowClassName();

// Interface
public:
	// IObjectWithSite (overloaded)
	STDMETHOD(SetSite)(IUnknown* pUnkSite);

	// IDeskBand
	STDMETHOD(GetBandInfo)(DWORD dwBandID, DWORD dwViewMode, DESKBANDINFO* pdbi);

	// IOleWindow
	STDMETHOD(GetWindow)(HWND *phwnd);
	STDMETHOD(ContextSensitiveHelp)(BOOL fEnterMode);

	// IDockingWindow
	STDMETHOD(CloseDW)(DWORD);
	STDMETHOD(ResizeBorderDW)(LPCRECT prcBorder, IUnknown *punkToolbarSite, BOOL);
	STDMETHOD(ShowDW)(BOOL bShow);

	// IInputObject
	STDMETHOD(HasFocusIO)();
	STDMETHOD(TranslateAcceleratorIO)(LPMSG lpMsg);
	STDMETHOD(UIActivateIO)(BOOL fActivate, LPMSG lpMsg);

	// IPersistStream
	// This is only supported to allow the desk band to be dropped on the
	// desktop and to prevent multiple instances of the desk band from showing
	// up in the context menu. This desk band doesn't actually persist any data.
	STDMETHOD(GetClassID)(CLSID *pClassID);
	STDMETHOD(IsDirty)(void);
	STDMETHOD(Load)(LPSTREAM);
	STDMETHOD(Save)(LPSTREAM, BOOL);
	STDMETHOD(GetSizeMax)(ULARGE_INTEGER*);
};
