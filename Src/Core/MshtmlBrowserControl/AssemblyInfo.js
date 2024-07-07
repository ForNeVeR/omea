// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

import System.Reflection;
import System.Runtime.CompilerServices;

[assembly: AssemblyTitle("MSHTML Browser Control")]
[assembly: AssemblyDescription("Managed wrapper for the C++ site hosting an MSHTML browser control that provides browsing and editing capabilities.")]

// Strong name (cannot apply as compiler params, only assembly-info syntax is supported)
@if(!@DEBUG)
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile("..\\..\\..\\Lib\\Key.snk")]
[assembly: AssemblyKeyName("")]
@end
