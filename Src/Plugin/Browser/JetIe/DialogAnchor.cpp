// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// CDialogAnchor — Implements anchoring of UI controls on the WinAPI dialogs.
//
// This helps with implementing the resizeable dialogs
// that maintain the relative layout of the conrols when resizing,
// according to the anchoring information provided.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic
//
#include "StdAfx.h"
#include ".\dialoganchor.h"

CDialogAnchor::CDialogAnchor(void)
{
	m_hwndDialog = NULL;
}

CDialogAnchor::~CDialogAnchor(void)
{
}

void CDialogAnchor::Attach(HWND hwndDialog)
{
	ASSERT(hwndDialog != NULL);

	// Store
	m_hwndDialog = hwndDialog;

	// Get the bounds
	CRect	rc;
	GetWindowRect(m_hwndDialog, &rc);
	m_sizeDialog = rc.Size();

	// The client rect
	GetClientRect(m_hwndDialog, &rc);
	m_sizeClient = rc.Size();
}

void CDialogAnchor::AddControl(HWND hwndControl, Anchor anchor)
{
	ASSERT(m_hwndDialog != NULL);
	if(m_hwndDialog == NULL)
		return;

	// Add the anchoring information (this will also save the initial sizing data)
	m_controls.push_back(CAnchoredControl(hwndControl, m_hwndDialog, anchor, m_sizeClient));
}

void CDialogAnchor::PerformLayout() const
{
	// Measure the dialog's client rect
	CRect	client;
	GetClientRect(m_hwndDialog, &client);
	CSize	sizeClient = client.Size();

	// Layout all the controls
	std::vector<CAnchoredControl>::const_iterator cit;
	for(cit = m_controls.begin(); cit != m_controls.end(); ++cit)
		cit->PerformLayout(m_hwndDialog, sizeClient);
}


/////////////////////////////////////////////////////////////////////////////
// CAnchoredControl Definitions

CAnchoredControl::CAnchoredControl(HWND hwnd, HWND hwndDialog, Anchor anchor, CSize sizeClient)
{
	ASSERT(hwnd != NULL);
	ASSERT(hwndDialog != NULL);

	// Store
	m_hWnd = hwnd;
	m_anchor = anchor;

	// Gather the layouting info
	CRect	rcBounds;	// Bounds of the control
	GetWindowRect(m_hWnd, &rcBounds);

	// Convert to the dialog's client coordinates
	CPoint	pt(0, 0);
	ClientToScreen(hwndDialog, &pt);
	rcBounds.OffsetRect(-pt.x, -pt.y);

	// Store the distances
	m_rcDistances = CRect(rcBounds.left, rcBounds.top, sizeClient.cx - rcBounds.right, sizeClient.cy - rcBounds.bottom);

	// Store the dimensions
	m_sizeDimensions = rcBounds.Size();
}

CAnchoredControl::CAnchoredControl(const CAnchoredControl &other)
{
	*this = other;	// Use operator=
}

CAnchoredControl &CAnchoredControl::operator=(const CAnchoredControl &other)
{
	m_hWnd = other.m_hWnd;
	m_rcDistances = other.m_rcDistances;
	m_anchor = other.m_anchor;
	m_sizeDimensions = other.m_sizeDimensions;

	return *this;
}

CAnchoredControl::~CAnchoredControl()
{
}

void CAnchoredControl::PerformLayout(HWND hwndDialog, CSize sizeClient) const
{
	ASSERT(m_hWnd != NULL);

	// Get the current control bounds
	CRect	rcBounds;	// Bounds of the control
	GetWindowRect(m_hWnd, &rcBounds);

	// Convert to the dialog's client coordinates
	CPoint	ptClientToScreen(0, 0);
	ClientToScreen(hwndDialog, &ptClientToScreen);
	rcBounds.OffsetRect(-ptClientToScreen.x, -ptClientToScreen.y);

	////////////////////
	// Apply anchoring, checking the anchoring flags

	// Horizontal anchoring
	if((m_anchor & anchorLeft) && (m_anchor & anchorRight))	// A sizing horizontal anchoring
	{
		rcBounds.left = m_rcDistances.left;
		rcBounds.right = sizeClient.cx - m_rcDistances.right;
	}
	else if(m_anchor & anchorLeft)	// A non-sizing left-anchoring
	{
		rcBounds.left = m_rcDistances.left;
		rcBounds.right = rcBounds.left + m_sizeDimensions.cx;
	}
	else if(m_anchor & anchorRight)	// A non-sizing right-anchoring
	{
		rcBounds.right = sizeClient.cx - m_rcDistances.right;
		rcBounds.left = rcBounds.right - m_sizeDimensions.cx;
	}

	// Vertical anchoring
	if((m_anchor & anchorTop) && (m_anchor & anchorBottom))
	{
		rcBounds.top = m_rcDistances.top;
		rcBounds.bottom = sizeClient.cy - m_rcDistances.bottom;
	}
	else if(m_anchor & anchorTop)	// A non-sizing top-anchoring
	{
		rcBounds.top = m_rcDistances.top;
		rcBounds.bottom = rcBounds.top + m_sizeDimensions.cy;
	}
	else if(m_anchor & anchorBottom)	// A non-sizing bottom-anchoring
	{
		rcBounds.bottom = sizeClient.cy - m_rcDistances.bottom;
		rcBounds.top = rcBounds.bottom - m_sizeDimensions.cy;
	}

	// Apply the new size to the window
	SetWindowPos(m_hWnd, NULL, rcBounds.left, rcBounds.top, rcBounds.Width(), rcBounds.Height(), SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOZORDER);	// Apply the new position
	InvalidateRect(m_hWnd, NULL, TRUE);
}
