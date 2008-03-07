/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// IexploreOmea\AboutDlg.cpp : Implementation of CAboutDlg

#include "stdafx.h"
#include "AboutDlg.h"
#include "..\JetIe.h"

// CAboutDlg

CAboutDlg::CAboutDlg()
{
}

CAboutDlg::~CAboutDlg()
{
}

LRESULT CAboutDlg::OnInitDialog(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	CAxDialogImpl<CAboutDlg>::OnInitDialog(uMsg, wParam, lParam, bHandled);

	// Get the own version
	CStringW	sVersion;
	try
	{
		sVersion = CJetIe::GetDllVersion();
	}
	catch(_com_error e)
	{
		sVersion = COM_REASON_T(e);
		COM_TRACE();
	}
	SetDlgItemText(IDC_VERSION, ToT(sVersion));

	return 1;  // Let the system set the focus
}

LRESULT CAboutDlg::OnClickedOK(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled)
{
	EndDialog(wID);
	return 0;
}

LRESULT CAboutDlg::OnClickedCancel(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled)
{
	EndDialog(wID);
	return 0;
}
