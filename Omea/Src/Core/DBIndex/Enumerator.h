// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#ifndef _DBINDEX_ENUMERATOR_H
#define _DBINDEX_ENUMERATOR_H

#include "DBIndex.h"

using namespace System;
using namespace System::Collections;
using namespace JetBrains::Omea::Base;

namespace DBIndex
{
	///////////////////////////////////////////////////////////////////////////
	// BTree enumerator for getting all keys
	///////////////////////////////////////////////////////////////////////////

	private __gc class GetAllKeysEnumerator : public IEnumerator, public IDisposable
	{
	public:

		Object* get_Current();
		bool MoveNext();
		void Reset();

		void Dispose();

	private public:

		GetAllKeysEnumerator( OmniaMeaBTree* bTree );

		OmniaMeaBTree*				_bTree;
		BTreeHeaderIteratorBase*	_btreeHeaderIterator;
		KeyPair*					_current;
		short						_currentPageIndex;
		short						_currentPageCount;
		const BTreeKeyBase**		_currentPageKeys;
	};

	private __gc class GetAllKeysEnumerable : public IEnumerable
	{
	public:

		IEnumerator* GetEnumerator()
		{
			return new GetAllKeysEnumerator( _bTree );
		}

	private public:

		GetAllKeysEnumerable( OmniaMeaBTree* bTree )
		{
			_bTree = bTree;
		}

		OmniaMeaBTree* _bTree;
	};


	///////////////////////////////////////////////////////////////////////////
	// BTree enumerator for searching keys
	///////////////////////////////////////////////////////////////////////////

	private __gc class SearchForRangeEnumerator : public IEnumerator, public IDisposable
	{
	public:

		Object* get_Current();
		bool MoveNext();
		void Reset();

		void Dispose();

	private public:

		SearchForRangeEnumerator();
		void Init( OmniaMeaBTree* bTree, IFixedLengthKey* beginKey, IFixedLengthKey* endKey );

		OmniaMeaBTree*				_bTree;
		IFixedLengthKey*			_beginKey;
		IFixedLengthKey*			_endKey;
		BTreeHeaderIteratorBase*	_btreeHeaderIterator;
		KeyPair*					_current;
		short						_currentPageIndex;
		short						_currentPageCount;
		const BTreeKeyBase**		_currentPageKeys;
	};

	private __gc class SearchForRangeEnumerable : public IEnumerable
	{
	public:

		IEnumerator* GetEnumerator();

	private public:

		SearchForRangeEnumerable( OmniaMeaBTree* bTree ) { _bTree = bTree; }
		void Init( IFixedLengthKey* beginKey, IFixedLengthKey* endKey );
		static System::Object* CreateNewEnumerator();

		OmniaMeaBTree*		_bTree;
		IFixedLengthKey*	_beginKey;
		IFixedLengthKey*	_endKey;
		static ObjectPool*  _enumeratorPool;
	};
}

#endif
