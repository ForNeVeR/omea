// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
