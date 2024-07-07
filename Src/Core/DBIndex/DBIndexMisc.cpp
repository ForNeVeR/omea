// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include <wchar.h>
#include <windows.h>
#include <Wininet.h>
#include <stdio.h>
#include "DBIndexMisc.h"

using namespace System::Runtime::InteropServices;

namespace DBIndex
{
	typedef unsigned __int32 uint32;
	typedef unsigned __int64 uint64;

	static uint32 HashiString32Impl( LPCWSTR s );
	static uint64 HashiString64Impl( LPCWSTR s );

	Int32 HashFunctions::HashiString32( String* s )
	{
		IntPtr ptr = Marshal::StringToHGlobalUni( s );
		Int32 result = HashiString32Impl( (LPCWSTR) ptr.ToPointer() );
		Marshal::FreeCoTaskMem( ptr );
		return result;
	}

	Int64 HashFunctions::HashiString64( String* s )
	{
		IntPtr ptr = Marshal::StringToHGlobalUni( s );
		Int64 result = HashiString64Impl( (LPCWSTR) ptr.ToPointer() );
		Marshal::FreeCoTaskMem( ptr );
		return result;
	}

	System::String* InternetCookies::Get( System::String* url )
	{
		IntPtr ptr = Marshal::StringToHGlobalAnsi( url );
		char data[ 256 ];
		DWORD dwSize = sizeof( data ) - 1;
		LPSTR lpszData = data;
		try
		{
			LPCSTR szUrl = (LPCSTR) ptr.ToPointer();
			while( !::InternetGetCookie( szUrl, NULL, lpszData, &dwSize ) )
			{
				DWORD err = ::GetLastError();
				if( err == ERROR_INSUFFICIENT_BUFFER )
				{
					lpszData = new char[ dwSize ];
					continue;
				}
				return NULL;
			}
			return new System::String( lpszData );
		}
		__finally
		{
			if( lpszData != data )
			{
				delete lpszData;
			}
			Marshal::FreeCoTaskMem( ptr );
		}
	}

	void InternetCookies::Set( System::String* url, System::String* cookies )
	{
		IntPtr ansiUrl = Marshal::StringToHGlobalAnsi( url );
		IntPtr ansiCookies = Marshal::StringToHGlobalAnsi( cookies );
		try
		{
			LPCSTR lpszUrl = (LPCSTR) ansiUrl.ToPointer();
			LPCSTR lpszData = (LPCSTR) ansiCookies.ToPointer();
			::InternetSetCookie( lpszUrl, NULL, lpszData );
		}
		__finally
		{
			Marshal::FreeCoTaskMem( ansiUrl );
			Marshal::FreeCoTaskMem( ansiCookies );
		}
	}

#pragma unmanaged

	uint64 HashiString64Impl( LPCWSTR s )
	{
		uint64 result = 5381;
		int c;
		unsigned char len = 0xff;

		while( ( c = *s++ ) != 0 )
		{
			result = ( ( result << 5 ) + result ) ^ (int) CharLowerW( (LPWSTR) c );
			++len;
		}
		return ( result & 0xffffffffffffff ) | ( ( (uint64) len ) << 56 );
	}

	uint32 HashiString32Impl( LPCWSTR s )
	{
		uint32 result = 5381;
		int c;
		int sum = 0;
		unsigned short len = 0;

		while( ( c = *s++ ) != 0 )
		{
			c = (int) CharUpperW( (LPWSTR) c );
			sum += c;
			result = ( ( result << 5 ) + result ) ^ c;
			++len;
		}
		if( len > 1 )
		{
			sum /= len;
		}
		return ( result & 0xffffff ) | ( ( ( sum >> 3 ) & 0xff ) << 24 );
	}
}
