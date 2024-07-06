// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// © JetBrains, Inc, 2005
// Written by (H) Serge Baltic

#include "StdAfx.h"
#include "OmeaSettingStore.h"
#include "..\JetIe.h"
#include "Resource.h"

COmeaSettingStore::COmeaSettingStore() :
	CSettingStore(_T("JetBrains"), _T("IexploreOmea"))
{
	// Fill in the options list

	// Whether the deferred requests are enabled, nonzero to enable (disabling them cleans the queue, prevents from adding new requests to the queue)
	AddEntryDefinition(setAllowDeferredRequests, _T("Deferred Requests"), _T("Enabled"), (long)TRUE);

	// Interval, in seconds, between the submission attempts
	AddEntryDefinition(setDeferredSubmitInterval, _T("Deferred Requests"), _T("Submit Attempts Interval"), (long)60);

	// Interval, in seconds, between the submission attempts, when we're starting Omea and waiting for it to start processing the requests
	AddEntryDefinition(setOmeaStartSubmitInterval, _T("Deferred Requests"), _T("Submit Attempts Interval when Starting Omea"), (long)5);

	// Allows retrying top submit the request queue on timer (if not allowed, will be submitted only on successful request)
	AddEntryDefinition(setAllowSubmitAttempts, _T("Deferred Requests"), _T("Allow Submit Attempts"), (long)1);

	// If a request cannot reach Omea, starts executing the request queue (if possible) and runs Omea
	AddEntryDefinition(setAutorunOmea, _T("Deferred Requests"), _T("Autorun Omea"), (long)0);

	// When we decide to start up Omea and increase the polling frequency, maximum time to hold the frequency at the raised level, in seconds
	AddEntryDefinition(setOmeaStartupTimeLimit, _T("Deferred Requests"), _T("Omea Startup Time Limit"), (long)600);

	// Hostname on which Omea replies to the remoting, typically, localhost
	AddEntryDefinition(setOmeaRemotingHost, _T("Omea Remoting"), _T("Hostname"), L"127.0.0.1");

	// Port on which Omea replies to the remoting, typically, -1, which means that Omea registry settings should be used
	AddEntryDefinition(setOmeaRemotingPort, _T("Omea Remoting"), _T("Port"), (long)-1);

	// Omea remoting formatter
	AddEntryDefinition(setOmeaRemotingFormatter, _T("Omea Remoting"), _T("Formatter"), L"xml");

	// Determines whether the notification balloons should be shown upon successful completion of UI-driving actions
	AddEntryDefinition(setShowSuccessNotifications, _T("User Interface"), _T("Show Success Notification"), (long)0);

#ifdef _DEBUG
	// Determines on each queue-submit attempt a copy of the queue should be made (works in DEBUG builds only).
	AddEntryDefinition(setDebugMakeQueueCopy, _T("Debug"), _T("Make Queue Copy"), (long)1);
#endif
}

COmeaSettingStore::~COmeaSettingStore()
{
}

CString COmeaSettingStore::GetOmeaExecutableFileName()
{
	CString	sSecurityKey;
	DWORD	dwChars;
	CRegKey	rk;
	if(rk.Open(HKEY_CURRENT_USER, _T("Software\\JetBrains\\Omea"), KEY_READ) != ERROR_SUCCESS)
		CJetIeException::Throw(CJetIe::LoadString(IDS_E_OMEA_SECURITY_KEY) + L'\n' + CJetIe::LoadString(IDS_E_OMEA_REGISTRY));
	if(rk.QueryStringValue(_T("ControlRun"), NULL, &dwChars) != ERROR_SUCCESS)
		CJetIeException::Throw(CJetIe::LoadString(IDS_E_OMEA_SECURITY_KEY));
	if(rk.QueryStringValue(_T("ControlRun"), sSecurityKey.GetBuffer(dwChars), &dwChars) != ERROR_SUCCESS)
		CJetIeException::Throw(CJetIe::LoadString(IDS_E_OMEA_SECURITY_KEY));
	rk.Close();
	sSecurityKey.ReleaseBuffer();
	return sSecurityKey;
}

int COmeaSettingStore::GetOmeaRemotingPortNumber()
{
	// First, try to get the port number from plugin settings
	int	nPluginPort = GetProfileInt(COmeaSettingStore::setOmeaRemotingPort);
	if(nPluginPort != -1)	// -1 means that we should lookup Omea settings
		return nPluginPort;

	// Port number from the Omea settings
	DWORD	dwPort = 0xFFFFFFFF;
	CRegKey	rk;
	if(rk.Open(HKEY_CURRENT_USER, _T("Software\\JetBrains\\Omea"), KEY_READ) != ERROR_SUCCESS)
		CJetIeException::Throw(CJetIe::LoadString(IDS_E_OMEA_REGISTRY));
	if(rk.QueryDWORDValue(_T("ControlPort"), dwPort) != ERROR_SUCCESS)	// Not a fatal error
	{
		TRACE(L"Could not read the Omea Remoting port number from the Registry.");
		dwPort = 3566;
	}
	rk.Close();

	return dwPort;
}

CStringA COmeaSettingStore::GetOmeaRemotingSecurityKey()
{
	CString	sSecurityKey;
	DWORD	dwChars;
	CRegKey	rk;
	if(rk.Open(HKEY_CURRENT_USER, _T("Software\\JetBrains\\Omea"), KEY_READ) != ERROR_SUCCESS)
		CJetIeException::Throw(CJetIe::LoadString(IDS_E_OMEA_SECURITY_KEY) + L'\n' + CJetIe::LoadString(IDS_E_OMEA_REGISTRY));
	if(rk.QueryStringValue(_T("ControlProtection"), NULL, &dwChars) != ERROR_SUCCESS)
		CJetIeException::Throw(CJetIe::LoadString(IDS_E_OMEA_SECURITY_KEY));
	if(rk.QueryStringValue(_T("ControlProtection"), sSecurityKey.GetBuffer(dwChars), &dwChars) != ERROR_SUCCESS)
		CJetIeException::Throw(CJetIe::LoadString(IDS_E_OMEA_SECURITY_KEY));
	rk.Close();
	sSecurityKey.ReleaseBuffer();
	return ToA(sSecurityKey);	// Convert to ANSI as it will take part in forming the target URL
}
