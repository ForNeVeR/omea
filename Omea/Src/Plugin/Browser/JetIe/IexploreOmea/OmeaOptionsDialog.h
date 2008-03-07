/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// OmeaOptionsDialog.h : Declaration of the COmeaOptionsDialog
//
// The Omea Options dialog that allows to change the general Omea Plugin for Internet Explorer options.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once

#include "Resource.h"       // main symbols

#include "OmeaSettingStore.h"

// COmeaOptionsDialog

class COmeaOptionsDialog : 
	public CAxDialogImpl<COmeaOptionsDialog>
{
public:
	COmeaOptionsDialog();
	~COmeaOptionsDialog();

	enum { IDD = IDD_OMEAOPTIONSDIALOG };

BEGIN_MSG_MAP(COmeaOptionsDialog)
	MESSAGE_HANDLER(WM_INITDIALOG, OnInitDialog)
	COMMAND_HANDLER(IDOK, BN_CLICKED, OnClickedOK)
	COMMAND_HANDLER(IDCANCEL, BN_CLICKED, OnClickedCancel)
	COMMAND_HANDLER(IDC_ENQUEUE, BN_CLICKED, OnClickedEnqueue)
	COMMAND_HANDLER(IDC_RUNOMEA, BN_CLICKED, OnClickedRunOmea)
	COMMAND_HANDLER(IDC_ABOUT, BN_CLICKED, OnBnClickedAbout)
	CHAIN_MSG_MAP(CAxDialogImpl<COmeaOptionsDialog>)
END_MSG_MAP()

// Handler prototypes:
//  LRESULT MessageHandler(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
//  LRESULT CommandHandler(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
//  LRESULT NotifyHandler(int idCtrl, LPNMHDR pnmh, BOOL& bHandled);

	LRESULT OnInitDialog(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled);
	LRESULT OnClickedOK(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
	LRESULT OnClickedCancel(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
	LRESULT OnClickedEnqueue(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);
	LRESULT OnClickedRunOmea(WORD wNotifyCode, WORD wID, HWND hWndCtl, BOOL& bHandled);

// Implementation
protected:
	/// The setting store that is read or altered when working with this dialog.
	COmeaSettingStore	m_settings;
public:
	LRESULT OnBnClickedAbout(WORD /*wNotifyCode*/, WORD /*wID*/, HWND /*hWndCtl*/, BOOL& /*bHandled*/);
};


