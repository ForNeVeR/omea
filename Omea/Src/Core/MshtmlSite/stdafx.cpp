/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// stdafx.cpp : source file that includes just the standard includes
// MshtmlSite.pch will be the pre-compiled header
// stdafx.obj will contain the pre-compiled type information

#include "stdafx.h"

#if((defined(_TRACE)) && (!defined(_DEBUG)))
void DirectTrace(LPCTSTR pstrFormat, ...)
{
   CString str;

   // format and write the data you were given
   va_list args;
   va_start(args, pstrFormat);

   str.FormatV(pstrFormat, args);
   va_end(args);

   OutputDebugString(str);
   return;
}
#endif	//((defined(_TRACE)) && (!defined(_DEBUG)))
