// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#ifndef _OMEA_DBINDEX_HEAP_OBJECT_H
#define _OMEA_DBINDEX_HEAP_OBJECT_H

#include <windows.h>
#include <memory>

namespace DBIndex
{
	// base class for objects allocated in the DBIndex heap
	class DBIndexHeapObject
	{
	public:
		static void CreateHeap();
		static int HeapSize() { return _heapSize; }
		static int ObjectsCount() { return _objectsCount; }

		void* operator new( size_t size );
		void operator delete( void* object );

	private:
		static HANDLE   _heap;
		static int		_heapSize;
		static int		_objectsCount;
	};

	// STL allocator performs allocation in the DBIndex heap
	template < class T > class DBIndex_allocator
	{
	public:
		typedef size_t		size_type;
		typedef ptrdiff_t	difference_type;
		typedef T*			pointer;
		typedef const T*	const_pointer;
		typedef T&			reference;
		typedef const T&	const_reference;
		typedef T			value_type;

		DBIndex_allocator() {}
		DBIndex_allocator( const DBIndex_allocator& ) {}
		pointer allocate( size_type n, const void * = 0 )
		{
			return (pointer) DBIndexHeapObject::operator new( n * sizeof( T ) );
		}
		void deallocate( void* p, size_type )
		{
			if( p )
			{
				DBIndexHeapObject::operator delete( p );
			}
		}
		pointer address( reference x ) const { return &x; }
		const_pointer address( const_reference x ) const { return &x; }
		DBIndex_allocator<T>& operator=( const DBIndex_allocator& ) { return *this; }
		void construct( pointer p, const T& val )
		{
			new((T*) p) T(val);
		}
		void destroy( pointer p )
		{
			p->~T();
		}
		size_type max_size() const
		{
			return size_type( -1 ) / sizeof( T );
		}
		template <class U> struct rebind
		{
			typedef DBIndex_allocator<U> other;
		};
		template <class U> DBIndex_allocator( const DBIndex_allocator<U>& ) {}
		template <class U> DBIndex_allocator& operator=( const DBIndex_allocator<U>& ) { return *this; }
	};
}

#endif
