/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

import System;
import System.ComponentModel;

print("Hello!");

try
{
    throw new Win32Exception(0);
}
catch(ex : Exception)
{
    print(ex);
}
