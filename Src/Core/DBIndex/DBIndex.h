// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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

	public ref class OmniaMeaBTree : public IBTree
	{
	public:

		OmniaMeaBTree( String ^filename, IFixedLengthKey ^factoryKey );

		~OmniaMeaBTree();
		bool Open() override;
        void Close() override;
		void CloseFile();
        void Clear() override;
		void Flush();

		void GetAllKeys( IntArrayList ^offsets ) override;
        void GetAllKeys( ArrayList ^keys_offsets ) override;
		IEnumerable ^GetAllKeys() override;
		KeyPair ^GetMinimum();
		KeyPair ^GetMaximum();

        void SearchForRange( IFixedLengthKey ^beginKey, IFixedLengthKey ^endKey, IntArrayList ^offsets ) override;
        void SearchForRange( IFixedLengthKey ^beginKey, IFixedLengthKey ^endKey, ArrayList ^keys_offsets ) override;
		IEnumerable ^SearchForRange( IFixedLengthKey ^beginKey, IFixedLengthKey ^endKey ) override;

        void DeleteKey( IFixedLengthKey ^akey, int offset ) override;
        void InsertKey( IFixedLengthKey ^akey, int offset ) override;

        property int MaxCount { int get() override; }
        property int Count { int get() override; }

        void SetCacheSize( int numberOfPages ) override;
        int GetCacheSize() override;

        int GetLoadedPages() override;
        int GetPageSize() override;

		static int GetObjectsCount();
		static int GetUsedMemory();

	private public:

		void InstantiateTypes();
		void SetFirstKey( IFixedLengthKey ^akey );
		void SetFirstAndLastKeys( IFixedLengthKey ^beginKey, IFixedLengthKey ^endKey );
		BTreePageBase* GetPageByOffset( int offset );
		BTreePageBase* AllocPage();
		BTreePageBase* PrepareNewPage( int offset );
		void CopyOffsets( const BTreeKeyBase** temp_keys, unsigned count, IntArrayList ^offsets );
		void CopyKeys( const BTreeKeyBase** temp_keys, int count, ArrayList ^keys_offsets  );
		void LoadPage( BTreePageBase* );
		void SavePage( BTreePageBase* );

		String^						_filename;
		IFixedLengthKey^			_factoryKey;
		FileStream^					_btreeFile;
		BTreePageBase*				_page;
		BTreePageBase*				_freePage;
		BTreeKeyBase*				_firstKey;
		BTreeKeyBase*				_lastKey;
		BTreeKeyBase*				_headerKey;
		IKeyComparer*				_keyComparer;
		BTreePagesCache*			_pagesCache;
		BTreeHeaderBase*			_btreeHeader;
		BTreeHeaderIteratorBase*	_btreeHeaderIterator;
		IEnumerable^				_searchForRangeEnumerable;
		IntArrayList^				_freeOffsets;
		ArrayList^					_oneItemList;
		int							_keysInIndex;
		int							_keyType;
		unsigned					_numberOfPages;
        int                         _loadedPages;
	};
}

#endif
