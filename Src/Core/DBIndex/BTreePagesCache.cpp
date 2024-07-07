// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#define USE_ASSEMBLY_LANG

#include "BTreePagesCache.h"

namespace DBIndex
{
	static PagePtr* AllocPagesArray( unsigned size )
	{
		PagePtr* result = (PagePtr*) DBIndexHeapObject::operator new( sizeof( PagePtr ) * size );
		for( unsigned i = 0; i < size; ++i )
		{
			result[ i ] = 0;
		}
		return result;
	}

	BTreePagesCache::BTreePagesCache( unsigned size )
		: _size( size ), _attempts( 1 ), _hits( 1 )
	{
		_pages = AllocPagesArray( size );
	}

	BTreePagesCache::~BTreePagesCache()
	{
		ClearWithoutSaving();
		DBIndexHeapObject::operator delete( _pages );
	}

	BTreePagesCache* BTreePagesCache::Create( int size )
	{
		return new BTreePagesCache( size );
	}

	void BTreePagesCache::Delete( BTreePagesCache* cache )
	{
		delete cache;
	}

	void BTreePagesCache::SetSize( unsigned size )
	{
		PagePtr* pages = AllocPagesArray( size );
		PagePtr* oldPages = _pages;
		unsigned i = 0;
		PagePtr page;
		for( ; i < _size && i < size; ++i )
		{
			page = oldPages[ i ];
			if( page == 0 )
			{
				i = _size;
				break;
			}
			pages[ i ] = page;
		}
		for( ; i < _size; ++i )
		{
			page = oldPages[ i ];
			if( page == 0 )
			{
				break;
			}
			page->Save();
			delete page;
		}
		DBIndexHeapObject::operator delete( oldPages );
		_pages = pages;
		_size = size;
	}
	unsigned BTreePagesCache::GetSize() const
	{
		return _size;
	}

	double BTreePagesCache::GetHitRate() const
	{
		return (double) _hits / (double) _attempts;
	}

	bool BTreePagesCache::HasPages() const
	{
		return _pages[ 0 ] != 0;
	}

		// returns removed page is any
	PagePtr BTreePagesCache::CachePage( PagePtr page )
	{
		PagePtr* pages = _pages;
		unsigned s = _size;
		PagePtr removedPage = pages[ s - 1 ];
		if( removedPage )
		{
			removedPage->Save();
		}
#ifdef USE_ASSEMBLY_LANG
		__asm
		{
			push	ebx
			push	ecx
			mov		ecx, s
			mov		ebx, pages
			dec		ecx
			shl		ecx, 2
			add		ebx, ecx
			shr		ecx, 2
__loop:
			sub		ebx, 4
			mov		eax, [ebx]
			mov		[ebx+4], eax
			loop	__loop
			mov		eax, page
			mov		[ebx], eax
			pop		ecx
			pop		ebx
			mov		eax, removedPage
		}
#else
		for( int i = s - 2; i >= 0; --i )
		{
			pages[ i + 1 ] = pages[ i ];
		}
		pages[ 0 ] = page;
		return removedPage;
#endif
	}

	// tries to load from cache a page by offset
	PagePtr BTreePagesCache::TryOffset( int offset )
	{
		++_attempts;
		PagePtr page;
		PagePtr* pages = _pages;
		unsigned i = 0, size = _size;
		for( ; ; ++i )
		{
			if( i == size || !( page = pages[ i ] ) )
			{
				return 0;
			}
			if( page->GetOffset() == offset )
			{
				++_hits;
				break;
			}
		}
#ifdef USE_ASSEMBLY_LANG
		__asm
		{
			cmp		i, 0
			jz		__no_loop
			push	ebx
			push	ecx
			mov		ecx, i
			mov		ebx, pages
			shl		ecx, 2
			add		ebx, ecx
			shr		ecx, 2
__loop:
			sub		ebx, 4
			mov		eax, [ebx]
			mov		[ebx+4], eax
			loop	__loop
			mov		eax, page
			mov		[ebx], eax
			pop		ecx
			pop		ebx
			jmp		__exit
__no_loop:
			mov		eax, page
__exit:
		}
#else
		for( ; i != 0; --i )
		{
			pages[ i ] = pages[ i - 1 ];
		}
		return ( pages[ 0 ] = page );
#endif
	}

	void BTreePagesCache::RemovePage( int offset )
	{
		PagePtr page;
		PagePtr* pages = _pages;
		unsigned i = 0, size = _size;
		for( ; i < size; ++i )
		{
			page = pages[ i ];
			if( page == 0 )
			{
				return;
			}
			if( page->GetOffset() == offset )
			{
				break;
			}
		}
		for( ; i < size - 1; ++i )
		{
			if( ( pages[ i ] = pages[ i + 1 ] ) == 0 )
			{
				break;
			}
			pages[ i + 1 ] = 0;
		}
		page->Save();
		delete page;
	}

	bool BTreePagesCache::Clear( BTreeHeaderBase& header )
	{
		PagePtr page, last;
		PagePtr* pages = _pages;
		unsigned size;

		for( size = 0; size < _size && pages[ size ] != 0; ++size );
		if( size )
		{
			// sort pages by offset
			bool continueSort = true;
			while( continueSort )
			{
				continueSort = false;
				last = pages[ 0 ];
				int lastOffset = last->GetOffset();
				for( unsigned i = 1; i < size; ++i )
				{
					page = pages[ i ];
					int offset = page->GetOffset();
					if( ( offset < lastOffset && *last < *page ) ||
						( offset > lastOffset && *page < *last ) )
					{
						page->SetOffset( lastOffset );
						last->SetOffset( offset );
						header.SetPageOffset( page->GetMinimum(), lastOffset );
						header.SetPageOffset( last->GetMinimum(), offset );
						continueSort = true;
					}
					else
					{
						lastOffset = offset;
					}
					last = page;
				}
			}
		}
		for( unsigned i = 0; i < size; ++i )
		{
			page = pages[ i ];
			pages[ i ] = 0;
			page->Save();
			delete page;
		}
		return true;
	}

	void BTreePagesCache::ClearWithoutSaving()
	{
		PagePtr page;
		PagePtr* pages = _pages;

		for( unsigned i = 0; i < _size; ++i )
		{
			page = pages[ i ];
			if( page == 0 )
			{
				break;
			}
			pages[ i ] = 0;
			delete page;
		}
	}
}
