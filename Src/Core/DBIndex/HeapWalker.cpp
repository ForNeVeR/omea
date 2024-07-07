// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include <windows.h>
#include <stdio.h>
#include "HeapWalker.h"
#include "BTreePage.h"

using namespace System::Runtime::InteropServices;

namespace DBIndex
{

void Win32Heaps::Dump( String ^filename )
{
	if( _walker == 0 )
	{
		_walker = new Win32HeapWalker();
	}
	IntPtr ptr = Marshal::StringToHGlobalAnsi( filename );
	_walker->Dump( (const char*) ptr.ToPointer() );
	Marshal::FreeCoTaskMem( ptr );
}

#pragma unmanaged

#define OM_MAX_HEAPS 256

	static HANDLE heaps[ OM_MAX_HEAPS ];
	static DWORD heapCount = OM_MAX_HEAPS;

	Win32HeapWalker::Win32HeapWalker()
	{
		if( heapCount == OM_MAX_HEAPS )
		{
			heapCount = ::GetProcessHeaps( heapCount, heaps );
			char msg[ 100 ];
			sprintf_s( msg, 100, "Number of Win32 heaps: %u", heapCount );
			::OutputDebugString( msg );
			if( heapCount > OM_MAX_HEAPS )
			{
				::DebugBreak();
			}
		}
	}

	unsigned Win32HeapWalker::HeapCount() const
	{
		return heapCount;
	}

	unsigned Win32HeapWalker::HeapTotalSize() const
	{
		unsigned size = 0;

		for( DWORD i = 0; i < heapCount; ++i )
		{
			HANDLE heap = heaps[ i ];
			if( ::HeapValidate( heap, 0, NULL ) )
			{
				::HeapLock( heap );
				PROCESS_HEAP_ENTRY heapEntry;
				heapEntry.lpData = NULL;
				while( ::HeapWalk( heap, &heapEntry ) )
				{
					if( heapEntry.wFlags & PROCESS_HEAP_ENTRY_BUSY )
					{
						size += heapEntry.cbData;
						size += heapEntry.cbOverhead;
					}
				}
				::HeapUnlock( heap );
			}
		}

		return size;
	}

	unsigned Win32HeapWalker::DBIndexMemUsage() const
	{
		unsigned result = 0;

		for( DWORD i = 0; i < heapCount; ++i )
		{
			HANDLE heap = heaps[ i ];
			if( !::HeapValidate( heap, 0, NULL ) )
			{
				char errorMsg[ 100 ];
				sprintf_s( errorMsg, 100, "Heap(%08x) is invalid!", heap );
				::OutputDebugString( errorMsg );
			}
			else
			{
				::HeapLock( heap );
				PROCESS_HEAP_ENTRY heapEntry;
				heapEntry.lpData = NULL;
				while( ::HeapWalk( heap, &heapEntry ) )
				{
					// check whether heap entry is OmniaMeaBTree page
					if( ( heapEntry.wFlags & PROCESS_HEAP_ENTRY_BUSY ) &&
						( heapEntry.cbData > 12 * 1024 ) )
					{
						unsigned* ptr = (unsigned*) heapEntry.lpData;
						// skip pointer to vtbl
						if( ptr[ 1 ] == BTREE_PAGE_MAGIC_NUMBER )
						{
							result += heapEntry.cbData;
						}
					}
				}
				::HeapUnlock( heap );
			}
		}
		return result;
	}

	static unsigned size_counts[ 200000 ];

	void Win32HeapWalker::Dump( const char* filename ) const
	{
		FILE* dump = fopen( filename, "wb" );

		for( DWORD i = 0; i < heapCount; ++i )
		{
			HANDLE heap = heaps[ i ];
			if( !::HeapValidate( heap, 0, NULL ) )
			{
				fprintf( dump, "Heap(%08x) is invalid!\n", heap );
			}
			else
			{
				memset( size_counts, 0, sizeof( size_counts ) );
				unsigned size = 0;
				fprintf( dump, "\t      size  overhead\n" );
				::HeapLock( heap );
				PROCESS_HEAP_ENTRY heapEntry;
				heapEntry.lpData = NULL;
				while( ::HeapWalk( heap, &heapEntry ) )
				{
					if( heapEntry.wFlags & PROCESS_HEAP_ENTRY_BUSY )
					{
						size += heapEntry.cbData;
						size += heapEntry.cbOverhead;
						char str[ 129 ];
						DWORD i = 0;
						for( ; i < heapEntry.cbData && i < 64; ++i )
						{
							sprintf_s( &str[ i << 1 ], 129, "%02x", ((byte*)heapEntry.lpData)[ i ] );
						}
						str[ i << 1 ] = '\0';
						fprintf( dump, "\t%10d%10d %s\n", heapEntry.cbData, heapEntry.cbOverhead, str );
						if( heapEntry.cbData < 200000 )
						{
							++size_counts[ heapEntry.cbData ];
						}
					}
				}
				::HeapUnlock( heap );
				fprintf( dump, "Total size of memory allocated in heap(%08x): %d\n", heap, size );
				fprintf( dump, "Dump counts of blocks by size (less than 200000)\n" );
				fprintf( dump, "      size     count\n");
				for( int i = 0; i < 200000; ++i )
				{
					if( size_counts[ i ] )
					{
						fprintf( dump, "%10d%10d\n", i, size_counts[ i ] );
					}
				}
			}
			fprintf( dump, "\n" );
		}

		fclose( dump );
	}
}
