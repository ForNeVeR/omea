// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#ifndef _OMEA_BTREEPAGESCACHE_H
#define _OMEA_BTREEPAGESCACHE_H

#include "DBIndexHeapObject.h"
#include "BTreeHeader.h"
#include "BTreePage.h"

namespace DBIndex
{
	typedef BTreePageBase* PagePtr;

	class BTreePagesCache : public DBIndexHeapObject
	{
		/**
		 * In order to avoid explicit allocation/deallocation in managed code,
		 * constructor and destructor are private.
		 * To create a new instance use static factory Create(), to delete use Delete().
		 */
		BTreePagesCache( unsigned size );
		~BTreePagesCache();

	public:

		static BTreePagesCache* Create( int size );
		static void Delete( BTreePagesCache* );

		void SetSize( unsigned size );
		unsigned GetSize() const;
		double GetHitRate() const;

		// has cache pages?
		bool HasPages() const;
		// returns removed page is any
		PagePtr CachePage( PagePtr page );
		// tries to load from cache a page by offset
		PagePtr TryOffset( int offset );

		void RemovePage( int offset );

		// returns false if header contradicts with cache
		bool Clear( BTreeHeaderBase& );
		void ClearWithoutSaving();

	private:

		PagePtr*	_pages;
		unsigned	_size;
		unsigned	_attempts;
		unsigned	_hits;
	};
}

#endif
