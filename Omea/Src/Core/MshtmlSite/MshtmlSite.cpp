// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetBrains Omea Mshtml Browser Component
//
// Implements the Web browser component wrapping with full-scale customization, including view options and security settings & zones.
// Consists of an unmanaged part (C++ ATL, raw hosting, a composite ActiveX control) and a managed part (JScript.NET, Windows Forms control around the unmanaged ActiveX control plus AbstractWebBrowser proxy-inheritor).
// The unmanaged parts server as a wrapper for the custom interfaces only, and should not carry out any meaningful processing. All the events should be delegated to the managed part for processing.
//
// This file belongs to the unmanaged part and implements the DLL Exports and other DLL-global stuff.
//
// © JetBrains Inc, 2004
// Written by (H) Serge Baltic
//
#include "stdafx.h"
#include "resource.h"

// The module attribute causes DllMain, DllRegisterServer and DllUnregisterServer to be automatically implemented for you
[ module(dll, uuid = "{05765213-D08D-4A03-8E16-215CCBE5F01A}",
		 name = "MshtmlSite",
		 helpstring = "MshtmlSite 1.0 Type Library",
		 resource_name = "IDR_MSHTMLSITE") ]
class CMshtmlSiteModule
{
public:
// Override CAtlDllModuleT members
};

// rem "$(FrameworkSDKDir)/bin/AXImp.exe" "$(TargetPath)" /out:"$(TargetDir)$(TargetName).AxInterop$(TargetExt)"
