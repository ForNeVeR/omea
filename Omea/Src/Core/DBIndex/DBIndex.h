/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#ifndef _OMEA_DBINDEX_H
#define _OMEA_DBINDEX_H

#include "TypeFactory.h"
#include "BTreePagesCache.h"

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace JetBrains::Omea::Containers;

namespace DBIndex
{
	///////////////////////////////////////////////////////////////////////////
	// OmniaMeaBTree is used from C# code
	///////////////////////////////////////////////////////////////////////////

	public __gc class OmniaMeaBTree : public IBTree
	{
	public:

		OmniaMeaBTree( String* filename, IFixedLengthKey* factoryKey );

		void Dispose();
		bool Open();
        void Close();
		void CloseFile();
        void Clear();
		void Flush();

		void GetAllKeys( IntArrayList* offsets );
        void GetAllKeys( ArrayList* keys_offsets );
		IEnumerable* GetAllKeys();
		KeyPair* GetMinimum();
		KeyPair* GetMaximum();

        void SearchForRange( IFixedLengthKey* beginKey, IFixedLengthKey* endKey, IntArrayList* offsets );
        void SearchForRange( IFixedLengthKey* beginKey, IFixedLengthKey* endKey, ArrayList* keys_offsets );
		IEnumerable* SearchForRange( IFixedLengthKey* beginKey, IFixedLengthKey* endKey );

        void DeleteKey( IFixedLengthKey* akey, int offset );
        void InsertKey( IFixedLengthKey* akey, int offset );

        __property int get_MaxCount();
        __property int get_Count();

        void SetCacheSize( int numberOfPages );
        int GetCacheSize();

        int GetLoadedPages();
        int GetPageSize();

		static int GetObjectsCount();
		static int GetUsedMemory();

	private public:

		void InstantiateTypes();
		void SetFirstKey( IFixedLengthKey* akey );
		void SetFirstAndLastKeys( IFixedLengthKey* beginKey, IFixedLengthKey* endKey );
		BTreePageBase* GetPageByOffset( int offset );
		BTreePageBase* AllocPage();
		BTreePageBase* PrepareNewPage( int offset );
		void CopyOffsets( const BTreeKeyBase** temp_keys, unsigned count, IntArrayList* offsets );
		void CopyKeys( const BTreeKeyBase** temp_keys, int count, ArrayList* keys_offsets  );
		void LoadPage( BTreePageBase* );
		void SavePage( BTreePageBase* );

		String*						_filename;
		IFixedLengthKey*			_factoryKey;
		FileStream*					_btreeFile;
		BTreePageBase*				_page;
		BTreePageBase*				_freePage;
		BTreeKeyBase*				_firstKey;
		BTreeKeyBase*				_lastKey;
		BTreeKeyBase*				_headerKey;
		IKeyComparer*				_keyComparer;
		BTreePagesCache*			_pagesCache;
		BTreeHeaderBase*			_btreeHeader;
		BTreeHeaderIteratorBase*	_btreeHeaderIterator;
		IEnumerable*				_searchForRangeEnumerable;
		IntArrayList*				_freeOffsets;
		ArrayList*					_oneItemList;
		int							_keysInIndex;
		int							_keyType;
		unsigned					_numberOfPages;
        int                         _loadedPages;
	};
}

#endif