// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// PopupNotification.cpp : Implementation of CPopupNotification
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "stdafx.h"
#include "PopupNotification.h"
#include "JetIe.h"
#include "JetIeException.h"

// CPopupNotification

int CPopupNotification::m_nInstances = 0;

CPopupNotification::CPopupNotification()
{
	TRACE(L"CPopupNotification::CPopupNotification (+%d)", m_nInstances++);
	m_nOpacity = -1;
	m_mood = pmNotify;
	m_bMouseIn = false;
	m_bAutosizeCompleted = false;
}

CPopupNotification::~CPopupNotification()
{
	TRACE(L"CPopupNotification::~CPopupNotification (-%d)", --m_nInstances);
}

STDMETHODIMP CPopupNotification::GetWindow(HWND * phwnd)
{
	if(phwnd == NULL)
		return E_POINTER;
	if(!::IsWindow(m_hWnd))
		return Error(_T("The window has not been created yet or has already been destroyed."));
	*phwnd = m_hWnd;
	return S_OK;
}

STDMETHODIMP CPopupNotification::ContextSensitiveHelp(BOOL fEnterMode)
{
	return S_FALSE;	// Do not handle, do not raise error
}

STDMETHODIMP CPopupNotification::Show(VARIANT Text, VARIANT Title, VARIANT Mood, VARIANT ParentWindow, VARIANT Timeout)
{
	try
	{
		// Check conditions
		if(::IsWindow(m_hWnd))
			return Error(_T("The Show command cannot be applied at this time: the popup is already visible. Wait until it expires or invoke Close."));

		/////////////////////////
		// Parse the parameters

		// Parse the Text parameter
		m_sText = (LPCTSTR)(_bstr_t)(_variant_t)Text;

		// Parse the Title optional parameter
		CString	sTitle;
		if(V_IS_MISSING(&Title))	// The optional parameter is missing
			sTitle = CJetIe::LoadString(IDS_TITLE);	// Missing parameter, use default
		else if(((_bstr_t)Title).length() == 0)	// Some empty value, or NULL
			sTitle = CJetIe::LoadString(IDS_TITLE);	// Use default
		else	// Try as a string
			sTitle = (LPCTSTR)(_bstr_t)Title;	// Take the explicit value

		// Parse the Mood optional parameter
		if(V_IS_MISSING(&Mood))	// The optional parameter is missing
			m_mood = pmNotify;
		else	// Parse the parameter
		{
			try
			{
				m_mood = (CPopupNotification::Mood)(long)(_variant_t)Mood;	// Try converting to a number
				if((m_mood < 0) || (m_mood >= 3))	// Out of range
					return Error(_T("The value of the Mood parameter is invalid. It should be either \"Notify\", \"Warn\", \"Stop\", 0, 1, 2, or missing."));
			}
			catch(_com_error)
			{
				// Convertion to a number failed, try a string
				try
				{
					_bstr_t	bsMood = (_variant_t)Mood;
					if(bsMood == (_bstr_t)L"Notify")
						m_mood = pmNotify;
					else if(bsMood == (_bstr_t)L"Warn")
						m_mood = pmWarn;
					else if(bsMood == (_bstr_t)L"Stop")
						m_mood = pmStop;
					else
						return Error(_T("The value of the Mood parameter is invalid. It should be either \"Notify\", \"Warn\", \"Stop\", 0, 1, 2, or missing."));	// Unknown value
				}
				catch(_com_error)
				{
					// Cannot understand the value
					return Error(_T("The value of the Mood parameter is invalid. It should be either \"Notify\", \"Warn\", \"Stop\", 0, 1, 2, or missing."));
				}
			}
		}

		// Parse the ParentWindow optional parameter
		HWND	hwndParent = CJetIe::WindowFromVariant(ParentWindow);

		// Parse the Timeout optional parameter
		m_dwTimeout = 0;
		if(V_IS_MISSING(&Timeout))	// The optional parameter is missing
			m_dwTimeout = 0;	// Choose default
		else
		{
			try
			{
				m_dwTimeout = (long)(_variant_t)Timeout;
				if(m_dwTimeout < 0)
					_com_issue_error(E_FAIL);
			}
			catch(_com_error)
			{
				// Cannot understand the value
				return Error(_T("The value of the Timeout parameter is invalid. It should be a positive integer, or 0 if defaults should be used."));
			}
		}
		if(m_dwTimeout == 0)	// Take the default
		{
			if(m_mood == pmNotify)
				m_dwTimeout = TIMEOUT_DEFAULT_NOTIFY;
			else if(m_mood == pmWarn)
				m_dwTimeout = TIMEOUT_DEFAULT_WARN;
			else
				m_dwTimeout = TIMEOUT_DEFAULT_STOP;
		}

		// Choose the background color, icon and other mood-dependent parameters
		switch(m_mood)
		{
		case pmNotify:
			m_colorBack = RGB(0xC0, 0xFF, 0xD0);
			m_iconClient = LoadIcon(CJetIe::GetModuleInstanceHandle(), MAKEINTRESOURCE(IDI_NOTIFY));
			break;
		case pmWarn:
			m_colorBack = RGB(0xFF, 0xFF, 0xC0);
			m_iconClient = LoadIcon(CJetIe::GetModuleInstanceHandle(), MAKEINTRESOURCE(IDI_WARN));
			break;
		case pmStop:
			m_colorBack = RGB(0xFF, 0xD0, 0xC0);
			m_iconClient = LoadIcon(CJetIe::GetModuleInstanceHandle(), MAKEINTRESOURCE(IDI_STOP));
			break;
		default:
			return Error(_T("An internal error has occured. The mood value is illegal."));
		}

		////////////////////////////////////////////
		// Still everything is OK, show the window

		// Obtain parameters
		POINT	pt = {0, 0};
		GetCursorPos(&pt);
		pt.x += 10;
		pt.y += 10;
		RECT	rc = {pt.x, pt.y, pt.x + POPUP_WINDOW_WIDTH, pt.y + POPUP_WINDOW_HEIGHT};
		// TODO: fit into screen, of the current monitor

		// Initiate window creation
		if(Create(hwndParent, rc, sTitle, WS_POPUP, WS_EX_NOACTIVATE) == NULL)
			CJetIeException::ThrowSystemError();
	}
	COM_CATCH_RETURN();

	return S_OK;
}

STDMETHODIMP CPopupNotification::Close(void)
{
	TRACE(L"CPopupNotification::Close");
	if(!::IsWindow(m_hWnd))
		return Error(_T("The Close command cannot be applied at this time because the popup window is not visible."));

	try
	{
		if(!DestroyWindow())
			CJetIeException::ThrowSystemError();
	}
	COM_CATCH_RETURN();

	return S_OK;
}

LRESULT CPopupNotification::OnNcCreate(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	TRACE(L"OnNcCreate");
	bHandled = TRUE;
	return TRUE;
}

LRESULT CPopupNotification::OnCreate(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	TRACE(L"OnCreate");
	AddRef();	// Keep an UI Reference
	bHandled = TRUE;

	// Perform layouting of the window (adjust the size, internal layout, and position (if needed to fit in screen).
	CRect	rcWindow = PerformLayout();

	// Set transparency in the UNICODE build (for Windows NT versions)
#ifdef _UNICODE
	TRACE(L"Setting up the layered window …");
	SetWindowLong(GWL_EXSTYLE, GetWindowLong(GWL_EXSTYLE) | WS_EX_LAYERED);
	SetLayeredWindowAttributes(m_hWnd, NULL, TRANSPARENCY, LWA_ALPHA);	// TODO: return the GetLastError-unleashed
#endif
	SetWindowPos(HWND_TOPMOST, rcWindow, SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE );	// Ensure topmost, apply the above-set parameters, resize & move to ensure visible, and show the window

	// Show the window

	/*
	TRACE(L"Before AnimateWindow");
	TRACE(L"AnimateWindow result: %d, %#010X.", AnimateWindow(m_hWnd, 2000, AW_BLEND), GetLastError());
	TRACE(L"After AnimateWindow");
	*/

	// Check if mouse pointer is currently inside the popup, and show opaque if needed
	POINT	ptMouse;
	GetCursorPos(&ptMouse);
	if(PtInRect(&rcWindow, ptMouse))
	{
		m_bMouseIn = true;
		SetTransparent(false);
	}
	else
		m_bMouseIn = false;

	m_nOpacity = -1;	// Ensure not fading out currently

	// Start couting the expiration timeout
	SetTimer(TIMER_EXPIRE, m_dwTimeout, NULL);

	return 0;
}

LRESULT CPopupNotification::OnDestroy(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	TRACE(L"OnDestroy");
	//DefWindowProc();	// Allow to detach from the window
	//Detach();
	//Release();	// Release the UI reference. This causes the object to be destroyed …
	bHandled = TRUE;
	return 0;
}

LRESULT CPopupNotification::OnMButtonDown(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	TRACE(L"OnMButtonDown");
	bHandled = TRUE;
	DestroyWindow();	// Closes on the middle-click
	return 0;
}

LRESULT CPopupNotification::OnRButtonDown(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	TRACE(L"OnRButtonDown");
	bHandled = TRUE;
	DestroyWindow();	// Closes on the right-click
	return 0;
}

LRESULT CPopupNotification::OnEraseBackground(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	TRACE(L"OnEraseBackground");

	// Obtain device context
	HDC	hDC = (HDC)wParam;
	SaveDC(hDC);

	// Background
	CBrush	brushBack(CreateSolidBrush(m_colorBack));
	RECT	rc;
	GetClientRect(&rc);
	FillRect(hDC, &rc, brushBack);

	// Undo changes to settings
	RestoreDC(hDC, -1);

	bHandled = TRUE;
	return 0;
}

LRESULT CPopupNotification::OnPaint(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	TRACE(L"OnPaint");
	PAINTSTRUCT	ps;
	HDC	hDC = BeginPaint(&ps);
	SaveDC(hDC);

	CRect	rcClient;
	GetClientRect(&rcClient);

	// Set the font
	CFont	fontText;
	GetFont(hDC, fontText);
	SelectObject(hDC, fontText);

	// Set the text parameters
	SetBkMode(hDC, TRANSPARENT);
	SetTextColor(hDC, 0x00000000);

	// Draw the message text
	DrawText(hDC, m_sText, -1, &m_rcText, DT_CENTER | DT_EXPANDTABS | DT_NOPREFIX | DT_WORD_ELLIPSIS | DT_WORDBREAK);

	// Icon
	DrawIconEx(hDC, m_rcIcon.left, m_rcIcon.top, m_iconClient, 0, 0, NULL, NULL, DI_IMAGE | DI_MASK);

	RestoreDC(hDC, -1);
	EndPaint(&ps);
	bHandled = TRUE;
	return 0;
}

LRESULT CPopupNotification::OnNcPaint(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	TRACE(L"OnNcPaint");
    HDC hDC = GetDCEx((HRGN)wParam, DCX_WINDOW | DCX_INTERSECTRGN);
	SaveDC(hDC);

	// Title background
	CBrush	brushTitle(CreateSolidBrush(RGB(0x80, 0x00, 0xFF)));
	RECT	rc;
	GetWindowRect(&rc);
	rc.bottom = rc.bottom - rc.top >= TITLE_HEIGHT ? rc.top + TITLE_HEIGHT : rc.bottom;
	FillRect(hDC, &rc, brushTitle);
	DeleteObject(brushTitle);

	RestoreDC(hDC, -1);
    ReleaseDC(hDC);

	bHandled = TRUE;
	return 0;
}

LRESULT CPopupNotification::OnNcCalcSize(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	TRACE(L"OnNcCalcSize");
	if(wParam)	// wParam is True
	{
		// If wParam is TRUE, it specifies that the application should indicate which part of the client area contains valid information. The system copies the valid information to the specified area within the new client area.
		bHandled = TRUE;
		return TRUE;	// The client area is preserved and aligned in the top-left corner of the new area
	}

	// wParam is false. Return the client area size
	LPRECT	pRC = (LPRECT)lParam;	// [in] the window rect; [out] the new client rect
	if(pRC->bottom - pRC->top < TITLE_HEIGHT)
		pRC->top += TITLE_HEIGHT;
	bHandled = TRUE;

	return 0;
}

LRESULT CPopupNotification::OnNcHitTest(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	//TRACE(L"OnNcHitTest");
	bHandled = TRUE;

	POINT	pt = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
	RECT	rc;
	GetWindowRect(&rc);

	if(!PtInRect(&rc, pt))
		return HTNOWHERE;

	if(pt.y - rc.top < TITLE_HEIGHT)
		return HTCAPTION;

	return HTCLIENT;
}

LRESULT CPopupNotification::OnTimer(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	if(wParam == TIMER_EXPIRE)	// A timeout has expired. It's time to close the popup.
	{
		KillTimer(TIMER_EXPIRE);
		KillTimer(TIMER_TRANSPARENCY);
		//AnimateWindow(m_hWnd, TIMEOUT_ANIMATE, AW_BLEND | AW_HIDE);
		//SetTimer(TIMER_DESTROY, TIMEOUT_ANIMATE);	// Schedulle window destruction when it finally disappears
		DestroyWindow();
	}
	else if(wParam == TIMER_TRANSPARENCY)	// Deferred-apply the transparency value (to avoid flickering when leaving nonclient area and entering the client one)
	{
		KillTimer(TIMER_TRANSPARENCY);	// One-time action
		SetTransparent(!m_bMouseIn);	// Transparent if mouse is not above this window
	}
	//else if(wParam ==
	bHandled = TRUE;
	return 0;
}

void CPopupNotification::OnFinalMessage(HWND hWnd)
{
	Release();	// Release the UI reference. This causes the object to be destroyed …
}

LRESULT CPopupNotification::OnAnyMouseLeave(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	if(m_bMouseIn)
	{
		m_bMouseIn = false;

		// Display as semitransparent
		KillTimer(TIMER_TRANSPARENCY);	// Cancel prev one, if there were any
		SetTimer(TIMER_TRANSPARENCY, TIMEOUT_TRANSPARENCY, NULL);

		// Restart counting the timeout
		SetTimer(TIMER_EXPIRE, m_dwTimeout, NULL);
	}
	bHandled = TRUE;
	return 0;
}

void CPopupNotification::SetTransparent(bool bTransparent)
{
#ifdef _UNICODE
	SetLayeredWindowAttributes(m_hWnd, NULL, (bTransparent ? TRANSPARENCY : 0xFF), LWA_ALPHA);
	SetWindowPos(0, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);	// Apply the changes
#endif
}

LRESULT CPopupNotification::OnMouseMove(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	OnMouseEnter(false);
	bHandled = FALSE;	// Do not interfere
	return 0;
}

LRESULT CPopupNotification::OnNcMouseMove(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	OnMouseEnter(true);
	bHandled = FALSE;	// Do not interfere
	return 0;
}

bool CPopupNotification::OnMouseEnter(bool bNonClient)
{
	if(m_bMouseIn)	// Do nothing if mouse pointer is already inside the window
		return false;

	// Register for the mouse-leave event for non-client area
	TRACKMOUSEEVENT	tme;
	ZeroMemory(&tme, sizeof(tme));
	tme.cbSize = sizeof(tme);
	tme.dwFlags = TME_LEAVE | (bNonClient ? TME_NONCLIENT : 0);
	tme.hwndTrack = m_hWnd;
	tme.dwHoverTime = HOVER_DEFAULT;
	if(!_TrackMouseEvent(&tme))
	{
		TRACE(L"Could not track the mouse events: %#010X.", GetLastError());
		false;
	}

	// If successful, account for mouse cursor presence in the window
	m_bMouseIn = true;
	KillTimer(TIMER_TRANSPARENCY);	// Cancel prev one, if there were any
	SetTimer(TIMER_TRANSPARENCY, TIMEOUT_TRANSPARENCY, NULL);	// Schedule update of transparency
	KillTimer(TIMER_EXPIRE);	// Cancel popup expiration

	return true;
}

void CPopupNotification::GetFont(HDC hDC, CFont &font)
{
	int	nFontHeight = -MulDiv(8, GetDeviceCaps(hDC, LOGPIXELSY), 72);	// 8pt converted to pixels

	font.Attach(CreateFont(nFontHeight, 0, 0, 0, 0, FALSE, FALSE, FALSE, ANSI_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_DONTCARE, _T("Tahoma")));
}

void CPopupNotification::CalcTextRect(CString sText, CRect &rect)
{
	// Obtain the display device context to measure the text size on it
	HDC	hDC;
	if((hDC = CreateDC(_T("DISPLAY"), NULL, NULL, NULL)) == NULL)
		return;	// Failure

	CFont	font;
	GetFont(hDC, font);
	SelectObject(hDC, font);
	DrawText(hDC, sText, -1, &rect, DT_CENTER | DT_EXPANDTABS | DT_NOPREFIX | DT_WORDBREAK | DT_CALCRECT);
	DWORD	dwError = GetLastError();
	if(dwError != ERROR_SUCCESS)
		TRACE(L"Could not measure the required text size. %s (%#010X)", CJetIe::GetSystemError(dwError), dwError);

	DeleteDC(hDC);
}

CRect CPopupNotification::PerformLayout()
{
	// Start from the current client rectangle
	CRect	client;
	GetClientRect(&client);

	int	nPadding = 2;	// Universal padding, in pixels
	int	nIconSize = 32;	// Size of the icon

	//////////////////
	// Set the parts

	// Icon
	m_rcIcon.left = client.left + nPadding;
	m_rcIcon.top = client.top + (client.Height() - nIconSize) / 2;	// V-center
	m_rcIcon.right = m_rcIcon.left + nIconSize;
	m_rcIcon.bottom = m_rcIcon.top + nIconSize;

	// Text
	m_rcText.left = m_rcIcon.right + nPadding;
	m_rcText.top = client.top + nPadding;
	m_rcText.right = client.right - nPadding;
	m_rcText.bottom = client.bottom - nPadding;
	CRect	rcTextOriginal = m_rcText;	// Store to compare and detect the changes

	//////////////////////////////
	// Update the Text Part size
	CalcTextRect(m_sText, m_rcText);
	if(m_rcText.Height() < nIconSize)
	{	// V-center the text within the window
		int	nMoveDown = (nIconSize - m_rcText.Height()) / 2;
		m_rcText.top += nMoveDown;
		m_rcText.bottom += nMoveDown;
	}

	/////////////////////////////////////
	// Adjust the Client Rect and Parts
	// according to the new size

	// Difference in each direction
	int	nHorDiff = m_rcText.Width() - rcTextOriginal.Width();
	int	nVerDiff = m_rcText.Height() - rcTextOriginal.Height();
	nVerDiff = nVerDiff >= 0 ? nVerDiff : 0;	// Don't allow to shrink as the icon won't fit in this case

	// Apply to the client rect
	client.right += nHorDiff;
	client.bottom += nVerDiff;

	// Adjust the Icon Part (vertical position may change)
	m_rcIcon.top += nVerDiff / 2;
	m_rcIcon.bottom += nVerDiff / 2;

	// Text Part rectangle has already been adjusted

	///////////////////////
	// Adjust the window size to fit the new client rect and reposition it to fit the screen

	// Adjust the size
	CRect	rcWindow;
	GetWindowRect(&rcWindow);
	rcWindow.right += nHorDiff;
	rcWindow.bottom += nVerDiff;

	// Reposition to fit on the current monitor
	HMONITOR	monitor = MonitorFromWindow(m_hWnd, MONITOR_DEFAULTTONEAREST);
	MONITORINFOEX	mi;
	ZeroMemory(&mi, sizeof(mi));
	mi.cbSize = sizeof(mi);
	if(GetMonitorInfo(monitor, &mi))	// If succeeded
	{
		CRect	rcWorkArea = mi.rcWork;	// Working area of the current monitor (without its taskbar, other bars, etc)
		int	nOffset;

		// Hor-adjust
		if(rcWindow.left < rcWorkArea.left)	// Falls out to the left
			nOffset = rcWorkArea.left - rcWindow.left;	// Move right
		else if(rcWindow.right > rcWorkArea.right)	// Falls out to the right
			nOffset = rcWorkArea.right - rcWindow.right;	// Move left
		else
			nOffset = 0;	// OK
		rcWindow.left += nOffset;
		rcWindow.right += nOffset;

		// Ver-adjust
		if(rcWindow.top < rcWorkArea.top)	// Falls out to the top
			nOffset = rcWorkArea.top - rcWindow.top;	// Move down
		else if(rcWindow.bottom > rcWorkArea.bottom)	// Falls out to the bottom
			nOffset = rcWorkArea.bottom - rcWindow.bottom;	// Move up
		else
			nOffset = 0;	// OK
		rcWindow.top += nOffset;
		rcWindow.bottom += nOffset;
	}

	// Return the new window size and position
	return rcWindow;
}
