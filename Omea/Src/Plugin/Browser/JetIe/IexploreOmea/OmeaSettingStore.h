// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// Omea Setting Store — extends the JetIe SettingStore,
// and populates it with Omea-specific options definitions.
//
// The general use consists of creating a new instance of this class when needed.
// Do not call RewriteSettings too often because it's the only costly and non-thread-safe operation.
//
// © JetBrains, Inc, 2005
// Written by (H) Serge Baltic

#pragma once
#include "..\SettingStore.h"

class COmeaSettingStore :
	public CSettingStore
{
// Ctor/dtor
public:
	COmeaSettingStore();
	virtual ~COmeaSettingStore();

public:
	/// Settings IDs list, see the place where they're registered (ctor?) for comments.
	enum
	{
		setAllowDeferredRequests,
		setDeferredSubmitInterval,
		setOmeaStartSubmitInterval,
		setAllowSubmitAttempts,
		setAutorunOmea,
		setOmeaStartupTimeLimit,
		setOmeaRemotingHost,
		setOmeaRemotingPort,
		setOmeaRemotingFormatter,
		setShowSuccessNotifications

#ifdef _DEBUG
		, setDebugMakeQueueCopy
#endif
	};

// Operations
public:
	/// Returns the filename of Omea executable that should be run to process the failed requests.
	CString GetOmeaExecutableFileName() throw(_com_error);

	/// Number of port on which Omea listens to the remote requests.
	int GetOmeaRemotingPortNumber() throw(_com_error);

	/// Security key that must be present in remote requests to Omea.
	CStringA GetOmeaRemotingSecurityKey() throw(_com_error);
};
