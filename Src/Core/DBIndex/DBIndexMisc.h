// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
