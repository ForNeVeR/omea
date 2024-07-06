// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// OmeaOptionsDialog.cpp : Implementation of COmeaOptionsDialog
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "stdafx.h"
#include "OmeaOptionsDialog.h"

#include "..\JetIe.h"
#include "math.h"
#include "OmeaRequestQueue.h"
#include "AboutDlg.h"

// COmeaOptionsDialog

COmeaOptionsDialog::COmeaOptionsDialog()
{
}

COmeaOptionsDialog::~COmeaOptionsDialog()
{
}

LRESULT COmeaOptionsDialog::OnInitDialog(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
{
	CAxDialogImpl<COmeaOptionsDialog>::OnInitDialog(uMsg, wParam, lParam, bHandled);

	// Initial state
	CheckDlgButton(IDC_ENQUEUE, FALSE);
	CheckDlgButton(IDC_RUNOMEA, FALSE);
	CheckDlgButton(IDC_AUTOSUBMIT, !!m_settings.GetProfileInt(COmeaSettingStore::setAllowSubmitAttempts));
	GetDlgItem(IDC_AUTOSUBMIT).EnableWindow(FALSE);

	// Load the values
	// The Run Omea option is set, and queueing is enabled
	if((m_settings.GetProfileInt(COmeaSettingStore::setAutorunOmea)) && (m_settings.GetProfileInt(COmeaSettingStore::setAllowDeferredRequests)))
		CheckDlgButton(IDC_RUNOMEA, TRUE);
	// The Run Omea option is not set, but queueing is still enabled
	else if((!m_settings.GetProfileInt(COmeaSettingStore::setAutorunOmea)) && (m_settings.GetProfileInt(COmeaSettingStore::setAllowDeferredRequests)))
	{
		CheckDlgButton(IDC_ENQUEUE, TRUE);
		GetDlgItem(IDC_AUTOSUBMIT).EnableWindow(TRUE);
	}

	return 1;  // Let the system set the focus
}

LRESULT COmeaOptionsDialog::OnClickedOK(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled)
{
	// Save the settings
	if(IsDlgButtonChecked(IDC_RUNOMEA))
	{	// Run omea option
		m_settings.WriteProfileInt(COmeaSettingStore::setAllowDeferredRequests, (long)1);
		m_settings.WriteProfileInt(COmeaSettingStore::setAllowSubmitAttempts, (long)1);
		m_settings.WriteProfileInt(COmeaSettingStore::setAutorunOmea, (long)1);
	}
	else if(IsDlgButtonChecked(IDC_ENQUEUE))
	{	// Enqueue option
		m_settings.WriteProfileInt(COmeaSettingStore::setAllowDeferredRequests, (long)1);
		m_settings.WriteProfileInt(COmeaSettingStore::setAllowSubmitAttempts, (long)IsDlgButtonChecked(IDC_AUTOSUBMIT));
		m_settings.WriteProfileInt(COmeaSettingStore::setAutorunOmea, (long)0);
	}
	else
	{	// No option set
		m_settings.WriteProfileInt(COmeaSettingStore::setAllowDeferredRequests, (long)0);
		m_settings.WriteProfileInt(COmeaSettingStore::setAllowSubmitAttempts, (long)0);
		m_settings.WriteProfileInt(COmeaSettingStore::setAutorunOmea, (long)0);
	}

	// Check if the request queue submit attemtps should be started now
	COmeaRequestQueue::BeginSubmitAttempts();

	EndDialog(wID);
	return 0;
}

LRESULT COmeaOptionsDialog::OnClickedCancel(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled)
{
	// Check if the request queue submit attemtps should be started now
	COmeaRequestQueue::BeginSubmitAttempts();

	EndDialog(wID);
	return 0;
}

LRESULT COmeaOptionsDialog::OnClickedEnqueue(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled)
{
	bHandled = TRUE;
	GetDlgItem(IDC_AUTOSUBMIT).EnableWindow(IsDlgButtonChecked(IDC_ENQUEUE));
	return 0;
}

LRESULT COmeaOptionsDialog::OnClickedRunOmea(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled)
{
	bHandled = TRUE;
	GetDlgItem(IDC_AUTOSUBMIT).EnableWindow(IsDlgButtonChecked(IDC_ENQUEUE));
	return 0;
}

LRESULT COmeaOptionsDialog::OnBnClickedAbout(WORD /*wNotifyCode*/, WORD /*wID*/, HWND /*hWndCtl*/, BOOL& /*bHandled*/)
{
	// Show the about box
	CAboutDlg	dlgAbout;
	dlgAbout.DoModal(m_hWnd);

	return 0;
}
