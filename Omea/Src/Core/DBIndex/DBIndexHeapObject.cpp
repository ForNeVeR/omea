// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

//#define CRT_ALLOCATIONS

#include <windows.h>
#include "DBIndexHeapObject.h"

namespace DBIndex
{
	HANDLE   DBIndexHeapObject::_heap = NULL;
	int      DBIndexHeapObject::_heapSize = 0;
	int      DBIndexHeapObject::_objectsCount = 0;

#ifndef CRT_ALLOCATIONS

static __forceinline __int8 InterlockedExchange(volatile __int8* pTarget, __int8 c)
{
  __asm {
         mov     ebx, pTarget
         mov     al, c
    lock xchg    byte ptr [ebx], al
  }
}

#define STATIC_HEAP_SIZE 16384
#define STATIC_HEAP_LONG_MULTIPLIER 4

		// each heap entry consists of 4 longs (32 bytes)
		static __int64 _staticHeap[STATIC_HEAP_SIZE * STATIC_HEAP_LONG_MULTIPLIER];
		static __int8  _staticHeapStates[STATIC_HEAP_SIZE];
		static int	   _staticHeapObjects;
#endif

	void DBIndexHeapObject::CreateHeap()
	{
#ifndef CRT_ALLOCATIONS
		if( _heap == NULL )
		{
			_heap = ::HeapCreate( HEAP_GENERATE_EXCEPTIONS, 0, 0 );
			::ZeroMemory( _staticHeapStates, sizeof( _staticHeapStates ) );
			_staticHeapObjects = 0;
		}
#endif
	}

	void* DBIndexHeapObject::operator new( size_t size )
	{
#ifdef CRT_ALLOCATIONS
		return malloc( size );
#else
		CreateHeap();
		int* result = 0;
		if( size <= sizeof( __int64 ) * STATIC_HEAP_LONG_MULTIPLIER  &&
			_staticHeapObjects < STATIC_HEAP_SIZE * 15 / 16  )
		{
			int index = ( _objectsCount + _heapSize + _staticHeapObjects ) % STATIC_HEAP_SIZE;
			for( int i = 0; i < 16; ++i )
			{
				if( DBIndex::InterlockedExchange( &_staticHeapStates[ index ], 1 ) == 0 )
				{
					result = (int*) &_staticHeap[ index * STATIC_HEAP_LONG_MULTIPLIER ];
					size = sizeof( __int64 ) * STATIC_HEAP_LONG_MULTIPLIER;
					::InterlockedIncrement( (LPLONG)&_staticHeapObjects );
					break;
				}
				index = ( 2717 * index + 3141 ) % STATIC_HEAP_SIZE;
			}
		}
		if( !result )
		{
			size += 4;
			result = (int*)::HeapAlloc( _heap, 0, size );
			*result = size;
			++result;
		}
		::InterlockedExchangeAdd( (LPLONG)&_heapSize, size );
		::InterlockedIncrement( (LPLONG)&_objectsCount );
		return result;
#endif
	}

	void DBIndexHeapObject::operator delete( void* object )
	{
#ifdef CRT_ALLOCATIONS
		free( object );
#else
		CreateHeap();
		int* ptr = (int*)object;
		int size = -((int)sizeof( __int64 ) * STATIC_HEAP_LONG_MULTIPLIER);
		if( ptr >= (int*)_staticHeap && ptr <= (int*)&_staticHeap[ (STATIC_HEAP_SIZE - 1) * STATIC_HEAP_LONG_MULTIPLIER ] )
		{
			int index = (((__int64*)ptr ) - (__int64*)_staticHeap ) / STATIC_HEAP_LONG_MULTIPLIER;
			DBIndex::InterlockedExchange( &_staticHeapStates[ index ], 0 );
			::InterlockedDecrement( (LPLONG)&_staticHeapObjects );
		}
		else
		{
			--ptr;
			size = -*ptr;
			::HeapFree( _heap, 0, ptr );
		}
		::InterlockedExchangeAdd( (LPLONG)&_heapSize, size );
		::InterlockedDecrement( (LPLONG)&_objectsCount );
#endif
	}
}
