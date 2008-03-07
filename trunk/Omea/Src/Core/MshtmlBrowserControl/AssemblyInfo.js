/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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