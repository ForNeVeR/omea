/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#ifndef _OMEA_DBINDEXMISC_H
#define _OMEA_DBINDEXMISC_H

using namespace System;

namespace DBIndex
{
	public __gc class HashFunctions
	{
	public:
		static Int32 HashiString32( String* s );
		static Int64 HashiString64( String* s );
	};

	public __gc class InternetCookies
	{
	public:
		static System::String* Get( System::String* url );
		static void Set( System::String* url, System::String* cookies );
	};
}

#endif