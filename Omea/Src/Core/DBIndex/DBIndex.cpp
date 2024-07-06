// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include "DBIndex.h"
#include "Enumerator.h"

#define HEADER_SIZE 1024

using namespace System::Diagnostics;
using namespace JetBrains::Omea::Containers;

namespace DBIndex
{
	OmniaMeaBTree::OmniaMeaBTree( String* filename, IFixedLengthKey* factoryKey )
	{
		Trace::Write( "OmeaBTree(" );
		Trace::Write( System::IO::Path::GetFileName( filename ) );
		Trace::WriteLine( "): Creating..." );
		DBIndexHeapObject::CreateHeap();
		_filename = filename;
		_factoryKey = factoryKey->FactoryMethod();
		_pagesCache = BTreePagesCache::Create( 16 );
		_searchForRangeEnumerable = new SearchForRangeEnumerable( this );
		_freeOffsets = new IntArrayList();
		_oneItemList = new ArrayList( 1 );
		_keysInIndex = 0;
		_keyType = unknown_Key;
		_page = 0;
		_freePage = 0;
		_firstKey = 0;
		_lastKey = 0;
		_headerKey = 0;
		_btreeHeader = 0;
		_btreeHeaderIterator = 0;
		_keyComparer = 0;
		_numberOfPages = 0;
        _loadedPages = 0;

		Object* key = factoryKey->Key;
		Type* keyType = key->GetType();
		if( keyType->Equals( __typeof( Int32 ) ) )
		{
			_keyType = int_Key;
		}
		else if( keyType->Equals( __typeof( Int64 ) ) )
		{
			_keyType = long_Key;
		}
		else if( keyType->Equals( __typeof( DateTime ) ) )
		{
			_keyType = datetime_Key;
		}
		else if( keyType->Equals( __typeof( Double ) ) )
		{
			_keyType = double_Key;
		}
		else if( keyType->Equals( __typeof( Compound ) ) )
		{
			Compound* compound = dynamic_cast< Compound* >( key );
			Type* type1 = compound->_key1->GetType();
			Type* type2 = compound->_key2->GetType();
			if( type1->Equals( __typeof( Int32 ) ) )
			{
				if( type2->Equals( __typeof( Int32 ) ) )
				{
					_keyType = int_int_Key;
				}
				else if( type2->Equals( __typeof( DateTime ) ) )
				{
					_keyType = int_datetime_Key;
				}
				else if( type2->Equals( __typeof( Int64 ) ) )
				{
					_keyType = int_long_Key;
				}
			}
			else if( type1->Equals( __typeof( Int64 ) ) )
			{
				if( type2->Equals( __typeof( Int32 ) ) )
				{
					_keyType = long_int_Key;
				}
				else if( type2->Equals( __typeof( Int64 ) ) )
				{
					_keyType = long_long_Key;
				}
			}
		}
		else if( keyType->Equals( __typeof( CompoundAndValue ) ) )
		{
			CompoundAndValue* compound = dynamic_cast< CompoundAndValue* >( key );
			Type* type1 = compound->_key1->GetType();
			Type* type2 = compound->_key2->GetType();
			Type* valueType = compound->_value->GetType();
			if( type1->Equals( __typeof( Int32 ) ) )
			{
				if( type2->Equals( __typeof( Int32 ) ) )
				{
					if( valueType->Equals( __typeof( Int32 ) ) )
					{
						_keyType = int_int_int_Key;
					}
					else if( valueType->Equals( __typeof( DateTime ) ) )
					{
						_keyType = int_int_datetime_Key;
					}
				}
				else if( type2->Equals( __typeof( DateTime ) ) )
				{
					if( valueType->Equals( __typeof( Int32 ) ) )
					{
						_keyType = int_datetime_int_Key;
					}
				}
			}
		}
	}

	void OmniaMeaBTree::Dispose()
	{
		Trace::Write( "OmeaBTree(" );
		Trace::Write( System::IO::Path::GetFileName( _filename ) );
		Trace::WriteLine( "): Disposing..." );
		if( _page )
		{
			TypeFactory::DeletePage( _page );
			_page = 0;
		}
		if( _freePage )
		{
			TypeFactory::DeletePage( _freePage );
			_freePage = 0;
		}
		if( !_firstKey )
		{
			TypeFactory::DeleteKey( _firstKey );
			_firstKey = 0;
		}
		if( !_lastKey )
		{
			TypeFactory::DeleteKey( _lastKey );
			_lastKey = 0;
		}
		if( !_headerKey )
		{
			TypeFactory::DeleteKey( _headerKey );
			_headerKey = 0;
		}
		if( !_btreeHeader )
		{
			TypeFactory::DeleteHeader( _btreeHeader );
			_btreeHeader = 0;
		}
		if( !_btreeHeaderIterator )
		{
			TypeFactory::DeleteHeaderIterator( _btreeHeaderIterator );
			_btreeHeaderIterator = 0;
		}
		if( !_keyComparer )
		{
			TypeFactory::DeleteKeyComparer( _keyComparer );
			_keyComparer = 0;
		}
		BTreePagesCache::Delete( _pagesCache );
		_pagesCache = 0;
	}

	bool OmniaMeaBTree::Open()
	{
		InstantiateTypes();

		_btreeFile = new FileStream( _filename, FileMode::OpenOrCreate, FileAccess::ReadWrite, FileShare::Read, 8 );
		int fileHandle = _btreeFile->Handle.ToInt32();
		_page->SetFileHandle( fileHandle );
		if( _freePage )
		{
			_freePage->SetFileHandle( fileHandle );
		}
		_keysInIndex = 0;

		/**
		 * check whether the btree was successfully closed, and load header if it was
		 */
        byte closed = ( _btreeFile->Length < HEADER_SIZE ) ? 0 : _btreeFile->ReadByte();
		_btreeFile->Position = 0;
		_btreeFile->WriteByte( 0 );
		if( !closed )
		{
			for( int i = 1; i < HEADER_SIZE; ++i )
			{
				_btreeFile->WriteByte( 0 );
			}
		}
		else
		{
			BinaryReader* reader = new BinaryReader( _btreeFile );
			_keysInIndex = reader->ReadInt32();
			int size = reader->ReadInt32();
			_btreeFile->Position = size;
			if( !_btreeHeader->Load( fileHandle ) )
			{
				throw new System::IO::IOException( "Failed to load BTree header" );
			}
			else
			{
				_btreeFile->SetLength( size );
				_numberOfPages = _btreeHeader->Size();
			}
		}

		_btreeFile->Flush();

		return closed != 0;
	}

	void OmniaMeaBTree::Close()
	{
		Flush();
		CloseFile();

		/**
		 * save header and mark as closed
		 */
		try
		{
			_btreeFile = new FileStream( _filename, FileMode::OpenOrCreate, FileAccess::ReadWrite, FileShare::None, 8 );
			if( _btreeFile->get_CanRead() && _btreeFile->get_CanWrite() )
			{
				BinaryWriter* writer = new BinaryWriter( _btreeFile );
				_btreeFile->WriteByte( 0 );
				writer->Write( _keysInIndex );
				writer->Write( (int) _btreeFile->Length );
				_btreeFile->Position = _btreeFile->Length;
				if( _btreeHeader->Save( _btreeFile->Handle.ToInt32() ) )
				{
					_btreeFile->Position = 0;
					_btreeFile->WriteByte( 1 );
				}
				CloseFile();
			}
		}
		catch(...) {}

		_btreeHeader->Clear();
		_freeOffsets->Clear();
		_keysInIndex = 0;
		_numberOfPages = 0;
	}

	void OmniaMeaBTree::CloseFile()
	{
		if( _btreeFile != 0 )
		{
			_btreeFile->Close();
		}
	}

	void OmniaMeaBTree::Clear()
	{
		// if btree is opened and was not disposed
		if( _page )
		{
			_btreeHeader->Clear();
			_freeOffsets->Clear();
			_pagesCache->ClearWithoutSaving();
			_keysInIndex = 0;
			_btreeFile->SetLength( HEADER_SIZE );
			_btreeFile->Position = 0;
			for( int i = 0; i < HEADER_SIZE; ++i )
			{
				_btreeFile->WriteByte( 0 );
			}
			_numberOfPages = 0;
		}
	}

	void OmniaMeaBTree::Flush()
	{
		// if btree was opened and not disposed
		if( _page )
		{
			_pagesCache->Clear( *_btreeHeader );
		}
	}

	void OmniaMeaBTree::GetAllKeys( IntArrayList* offsets )
	{
		const BTreeKeyBase* temp_keys[ MAX_KEYS_IN_PAGE ];

		_btreeHeader->GetMinimumPage( *_btreeHeaderIterator );
		while( !_btreeHeaderIterator->Exhausted() )
		{
			BTreePageBase* page = GetPageByOffset( _btreeHeaderIterator->GetCurrentOffset() );
			unsigned keyCount = page->GetAllKeys( temp_keys );
			if( keyCount > MAX_KEYS_IN_PAGE )
			{
				throw new BadIndexesException( "BTree contains cycles. Possible memory corruption." );
			}
			CopyOffsets( temp_keys, keyCount, offsets );
			_btreeHeaderIterator->MoveNextPage();
		}
	}

    void OmniaMeaBTree::GetAllKeys( ArrayList* keys_offsets )
	{
		const BTreeKeyBase* temp_keys[ MAX_KEYS_IN_PAGE ];

		_btreeHeader->GetMinimumPage( *_btreeHeaderIterator );
		while( !_btreeHeaderIterator->Exhausted() )
		{
			BTreePageBase* page = GetPageByOffset( _btreeHeaderIterator->GetCurrentOffset() );
			unsigned keyCount = page->GetAllKeys( temp_keys );
			if( keyCount > MAX_KEYS_IN_PAGE )
			{
				throw new BadIndexesException( "BTree contains cycles. Possible memory corruption." );
			}
			CopyKeys( temp_keys, keyCount, keys_offsets );
			_btreeHeaderIterator->MoveNextPage();
		}
	}

	IEnumerable* OmniaMeaBTree::GetAllKeys()
	{
		return new GetAllKeysEnumerable( this );
	}

	KeyPair* OmniaMeaBTree::GetMinimum()
	{
		_btreeHeader->GetMinimumPage( *_btreeHeaderIterator );
		if( _btreeHeaderIterator->Exhausted() )
		{
			return 0;
		}
		_oneItemList->Clear();
		BTreePageBase* page = GetPageByOffset( _btreeHeaderIterator->GetCurrentOffset() );
		const BTreeKeyBase* oneKey[ 1 ];
		oneKey[ 0 ] = &page->GetMinimum();
		CopyKeys( oneKey, 1, _oneItemList );
		return dynamic_cast<KeyPair*>( _oneItemList->get_Item( 0 ) );
	}

	KeyPair* OmniaMeaBTree::GetMaximum()
	{
		_btreeHeader->GetMaximumPage( *_btreeHeaderIterator );
		if( _btreeHeaderIterator->Exhausted() )
		{
			return 0;
		}
		_oneItemList->Clear();
		BTreePageBase* page = GetPageByOffset( _btreeHeaderIterator->GetCurrentOffset() );
		const BTreeKeyBase* oneKey[ 1 ];
		oneKey[ 0 ] = &page->GetMaximum();
		CopyKeys( oneKey, 1, _oneItemList );
		return dynamic_cast<KeyPair*>( _oneItemList->get_Item( 0 ) );
	}

	void OmniaMeaBTree::SearchForRange( IFixedLengthKey* beginKey, IFixedLengthKey* endKey, IntArrayList* offsets )
	{
		SetFirstAndLastKeys( beginKey, endKey );

		const BTreeKeyBase& firstKey = *_firstKey;
		const BTreeKeyBase& lastKey = *_lastKey;

		const BTreeKeyBase* temp_keys[ MAX_KEYS_IN_PAGE ];

		_btreeHeader->GetPage( firstKey, *_btreeHeaderIterator );
		while( !_btreeHeaderIterator->Exhausted() )
		{
			_btreeHeaderIterator->GetCurrentKey( *_headerKey );
			if( _keyComparer->Less( lastKey, *_headerKey ) )
			{
				break;
			}
			BTreePageBase* page = GetPageByOffset( _btreeHeaderIterator->GetCurrentOffset() );
			unsigned keyCount = page->SearchForRange( firstKey, lastKey, temp_keys );
			if( keyCount > MAX_KEYS_IN_PAGE )
			{
				throw new BadIndexesException( "BTree contains cycles. Possible memory corruption." );
			}
			CopyOffsets( temp_keys, keyCount, offsets );
			_btreeHeaderIterator->MoveNextPage();
		}
	}

	void OmniaMeaBTree::SearchForRange( IFixedLengthKey* beginKey, IFixedLengthKey* endKey, ArrayList* keys_offsets )
	{
		SetFirstAndLastKeys( beginKey, endKey );

		const BTreeKeyBase& firstKey = *_firstKey;
		const BTreeKeyBase& lastKey = *_lastKey;

		const BTreeKeyBase* temp_keys[ MAX_KEYS_IN_PAGE ];

		_btreeHeader->GetPage( firstKey, *_btreeHeaderIterator );
		while( !_btreeHeaderIterator->Exhausted() )
		{
			_btreeHeaderIterator->GetCurrentKey( *_headerKey );
			if( _keyComparer->Less( lastKey, *_headerKey ) )
			{
				break;
			}
			BTreePageBase* page = GetPageByOffset( _btreeHeaderIterator->GetCurrentOffset() );
			unsigned keyCount = page->SearchForRange( firstKey, lastKey, temp_keys );
			if( keyCount > MAX_KEYS_IN_PAGE )
			{
				throw new BadIndexesException( "BTree contains cycles. Possible memory corruption." );
			}
			CopyKeys( temp_keys, keyCount, keys_offsets );
			_btreeHeaderIterator->MoveNextPage();
		}
	}

	IEnumerable* OmniaMeaBTree::SearchForRange( IFixedLengthKey* beginKey, IFixedLengthKey* endKey )
	{
		dynamic_cast<SearchForRangeEnumerable*>( _searchForRangeEnumerable )->Init( beginKey, endKey );
		return _searchForRangeEnumerable;
	}

    void OmniaMeaBTree::DeleteKey( IFixedLengthKey* akey, int offset )
	{
		SetFirstKey( akey );
		_firstKey->SetOffset( offset );

		const BTreeKeyBase& firstKey = *_firstKey;
		_btreeHeader->GetPage( firstKey, *_btreeHeaderIterator );

		if( !_btreeHeaderIterator->Exhausted() )
		{
			BTreePageBase* page = GetPageByOffset( _btreeHeaderIterator->GetCurrentOffset() );
			if( page->Delete( firstKey ) )
			{
				--_keysInIndex;
				_btreeHeaderIterator->GetCurrentKey( *_headerKey );
				offset = page->GetOffset();
				if( page->GetCount() == 0 )
				{
					_freeOffsets->Add( offset );
					_pagesCache->RemovePage( offset );
					_btreeHeader->DeletePageOffset( *_headerKey );
					--_numberOfPages;
				}
				else
				{
					const BTreeKeyBase& minKey = page->GetMinimum();
					if( _keyComparer->Less( firstKey, minKey ) )
					{
						_btreeHeader->DeletePageOffset( firstKey );
						_btreeHeader->SetPageOffset( minKey, offset );
					}
				}
			}
		}
	}

    void OmniaMeaBTree::InsertKey( IFixedLengthKey* akey, int offset )
	{
		SetFirstKey( akey );
		_firstKey->SetOffset( offset );
		++_keysInIndex;

		const BTreeKeyBase& firstKey = *_firstKey;

		BTreePageBase* page;
		_btreeHeader->GetPage( firstKey, *_btreeHeaderIterator );

		if( _btreeHeaderIterator->Exhausted() )
		{
			page = AllocPage();
			page->Insert( firstKey );
			_btreeHeader->SetPageOffset( firstKey, page->GetOffset() );
		}
		else
		{
			page = GetPageByOffset( _btreeHeaderIterator->GetCurrentOffset() );
			if( !page->IsFull() )
			{
				if( page->IsAlmostFull() )
				{
					if( _keyComparer->Less( page->GetMaximum(), firstKey ) ||
						_keyComparer->Less( firstKey, page->GetMinimum() ) )
					{
						BTreePageBase* newPage = AllocPage();
						newPage->Insert( firstKey );
						_btreeHeader->SetPageOffset( firstKey, newPage->GetOffset() );
						return;
					}
				}
				page->Insert( firstKey );
			}
			else
			{
				BTreePageBase* rightPage = AllocPage();
				page->Split( *rightPage );
				const BTreeKeyBase& minKey = rightPage->GetMinimum();
				_btreeHeader->SetPageOffset( minKey, rightPage->GetOffset() );
				if( _keyComparer->Less( firstKey, minKey ) )
				{
					page->Insert( firstKey );
				}
				else
				{
					rightPage->Insert( firstKey );
					return;
				}
			}
			_btreeHeaderIterator->GetCurrentKey( *_headerKey );
			if( _keyComparer->Less( firstKey, *_headerKey ) )
			{
				_btreeHeader->DeletePageOffset( *_headerKey );
				_btreeHeader->SetPageOffset( firstKey, page->GetOffset() );
			}
		}
	}

    int OmniaMeaBTree::get_MaxCount()
	{
		return MAX_KEYS_IN_PAGE;
	}

    int OmniaMeaBTree::get_Count()
	{
		return _keysInIndex;
	}

	void OmniaMeaBTree::SetCacheSize( int numberOfPages )
	{
		if( numberOfPages < 2 ) // cache size can't be less than 2
		{
			numberOfPages = 2;
		}
		_pagesCache->SetSize( (unsigned) numberOfPages );
	}

    int OmniaMeaBTree::GetCacheSize()
	{
		return _pagesCache->GetSize();
	}

	int OmniaMeaBTree::GetObjectsCount()
	{
		return DBIndexHeapObject::ObjectsCount();
	}

	int OmniaMeaBTree::GetUsedMemory()
	{
		return DBIndexHeapObject::HeapSize();
	}

	///////////////////////////////////////////////////////////////////////////
	// implementation details (private members)
	///////////////////////////////////////////////////////////////////////////

	void OmniaMeaBTree::InstantiateTypes()
	{
		int type = _keyType;
		if( type == unknown_Key )
		{
			throw new System::Exception( "Key type is not supported!" );
		}
		if( !_page )
		{
			_page = TypeFactory::NewPage( type );
		}
		if( !_firstKey )
		{
			_firstKey = TypeFactory::NewKey( type );
		}
		if( !_lastKey )
		{
			_lastKey = TypeFactory::NewKey( type );
		}
		if( !_headerKey )
		{
			_headerKey = TypeFactory::NewKey( type );
		}
		if( !_btreeHeader )
		{
			_btreeHeader = TypeFactory::NewHeader( type );
		}
		if( !_btreeHeaderIterator )
		{
			_btreeHeaderIterator = TypeFactory::NewHeaderIterator( type );
		}
		if( !_keyComparer )
		{
			_keyComparer = TypeFactory::NewKeyComparer( type );
		}
	}

	void OmniaMeaBTree::SetFirstKey( IFixedLengthKey* akey )
	{
		switch( _keyType )
		{
			case int_Key:
			{
				int theKey = *dynamic_cast<Int32*>( akey->Key );
				static_cast< BTreeKey<int>* >( _firstKey )->SetKey( theKey );
				break;
			}
			case int_int_Key:
			{
				Compound* compound = dynamic_cast<Compound*>( akey->Key );
				CompoundKey<int,int> theKey;
				theKey._first = *dynamic_cast<Int32*>( compound->_key1 );
				theKey._second = *dynamic_cast<Int32*>( compound->_key2 );
				static_cast< BTreeKey< CompoundKey<int,int> >* >( _firstKey )->SetKey( theKey );
				break;
			}
			case int_datetime_Key:
			{
				Compound* compound = dynamic_cast<Compound*>( akey->Key );
				CompoundKey<int,long> theKey;
				theKey._first = *dynamic_cast<Int32*>( compound->_key1 );
				theKey._second = dynamic_cast<__box DateTime*>( compound->_key2 )->Ticks;
				static_cast< BTreeKey< CompoundKey<int,long> >* >( _firstKey )->SetKey( theKey );
				break;
			}
			case int_int_int_Key:
			{
				CompoundAndValue* compound = dynamic_cast<CompoundAndValue*>( akey->Key );
				CompoundKeyWithValue<int,int,int> theKey;
				theKey._first = *dynamic_cast<Int32*>( compound->_key1 );
				theKey._second = *dynamic_cast<Int32*>( compound->_key2 );
				int value = *dynamic_cast<Int32*>( compound->_value );
				theKey.SetValue( value );
				static_cast< BTreeKey< CompoundKeyWithValue<int,int,int> >* >( _firstKey )->SetKey( theKey );
				break;
			}
			case int_int_datetime_Key:
			{
				CompoundAndValue* compound = dynamic_cast<CompoundAndValue*>( akey->Key );
				CompoundKeyWithValue<int,int,long> theKey;
				theKey._first = *dynamic_cast<Int32*>( compound->_key1 );
				theKey._second = *dynamic_cast<Int32*>( compound->_key2 );
				long value = dynamic_cast<__box DateTime*>( compound->_value )->Ticks;
				theKey.SetValue( value );
				static_cast< BTreeKey< CompoundKeyWithValue<int,int,long> >* >( _firstKey )->SetKey( theKey );
				break;
			}
			case int_datetime_int_Key:
			{
				CompoundAndValue* compound = dynamic_cast<CompoundAndValue*>( akey->Key );
				CompoundKeyWithValue<int,long,int> theKey;
				theKey._first = *dynamic_cast<Int32*>( compound->_key1 );
				theKey._second = dynamic_cast<__box DateTime*>( compound->_key2 )->Ticks;
				int value = *dynamic_cast<Int32*>( compound->_value );
				theKey.SetValue( value );
				static_cast< BTreeKey< CompoundKeyWithValue<int,long,int> >* >( _firstKey )->SetKey( theKey );
				break;
			}
			case long_Key:
			{
				long theKey = *dynamic_cast<Int64*>( akey->Key );
				static_cast< BTreeKey<long>* >( _firstKey )->SetKey( theKey );
				break;
			}
			case datetime_Key:
			{
				long theKey = dynamic_cast<__box DateTime*>( akey->Key )->Ticks;
				static_cast< BTreeKey<long>* >( _firstKey )->SetKey( theKey );
				break;
			}
			case double_Key:
			{
				double theKey = *dynamic_cast<Double*>( akey->Key );
				static_cast< BTreeKey<double>* >( _firstKey )->SetKey( theKey );
				break;
			}
			default: throw new System::Exception( "Key type is not supported!" );
		}
	}

	void OmniaMeaBTree::SetFirstAndLastKeys( IFixedLengthKey* beginKey, IFixedLengthKey* endKey )
	{
		switch( _keyType )
		{
			case int_Key:
			{
				int firstKey = *dynamic_cast<Int32*>( beginKey->Key );
				int lastKey = *dynamic_cast<Int32*>( endKey->Key );
				static_cast< BTreeKey<int>* >( _firstKey )->SetKey( firstKey );
				static_cast< BTreeKey<int>* >( _lastKey )->SetKey( lastKey );
				break;
			}
			case int_int_Key:
			{
				Compound* compound1 = dynamic_cast<Compound*>( beginKey->Key );
				Compound* compound2 = dynamic_cast<Compound*>( endKey->Key );
				CompoundKey<int,int> firstKey;
				CompoundKey<int,int> lastKey;
				firstKey._first = *dynamic_cast<Int32*>( compound1->_key1 );
				firstKey._second = *dynamic_cast<Int32*>( compound1->_key2 );
				lastKey._first = *dynamic_cast<Int32*>( compound2->_key1 );
				lastKey._second = *dynamic_cast<Int32*>( compound2->_key2 );
				static_cast< BTreeKey< CompoundKey<int,int> >* >( _firstKey )->SetKey( firstKey );
				static_cast< BTreeKey< CompoundKey<int,int> >* >( _lastKey )->SetKey( lastKey );
				break;
			}
			case int_datetime_Key:
			{
				Compound* compound1 = dynamic_cast<Compound*>( beginKey->Key );
				Compound* compound2 = dynamic_cast<Compound*>( endKey->Key );
				CompoundKey<int,long> firstKey;
				CompoundKey<int,long> lastKey;
				firstKey._first = *dynamic_cast<Int32*>( compound1->_key1 );
				firstKey._second = dynamic_cast<__box DateTime*>( compound1->_key2 )->Ticks;
				lastKey._first = *dynamic_cast<Int32*>( compound2->_key1 );
				lastKey._second = dynamic_cast<__box DateTime*>( compound2->_key2 )->Ticks;
				static_cast< BTreeKey< CompoundKey<int,long> >* >( _firstKey )->SetKey( firstKey );
				static_cast< BTreeKey< CompoundKey<int,long> >* >( _lastKey )->SetKey( lastKey );
				break;
			}
			case int_int_int_Key:
			{
				CompoundAndValue* compound1 = dynamic_cast<CompoundAndValue*>( beginKey->Key );
				CompoundAndValue* compound2 = dynamic_cast<CompoundAndValue*>( endKey->Key );
				CompoundKeyWithValue<int,int,int> firstKey;
				CompoundKeyWithValue<int,int,int> lastKey;
				firstKey._first = *dynamic_cast<Int32*>( compound1->_key1 );
				firstKey._second = *dynamic_cast<Int32*>( compound1->_key2 );
				int value = *dynamic_cast<Int32*>( compound1->_value );
				firstKey.SetValue( value );
				lastKey._first = *dynamic_cast<Int32*>( compound2->_key1 );
				lastKey._second = *dynamic_cast<Int32*>( compound2->_key2 );
				value = *dynamic_cast<Int32*>( compound2->_value );
				lastKey.SetValue( value );
				static_cast< BTreeKey< CompoundKeyWithValue<int,int,int> >* >( _firstKey )->SetKey( firstKey );
				static_cast< BTreeKey< CompoundKeyWithValue<int,int,int> >* >( _lastKey )->SetKey( lastKey );
				break;
			}
			case int_int_datetime_Key:
			{
				CompoundAndValue* compound1 = dynamic_cast<CompoundAndValue*>( beginKey->Key );
				CompoundAndValue* compound2 = dynamic_cast<CompoundAndValue*>( endKey->Key );
				CompoundKeyWithValue<int,int,long> firstKey;
				CompoundKeyWithValue<int,int,long> lastKey;
				firstKey._first = *dynamic_cast<Int32*>( compound1->_key1 );
				firstKey._second = *dynamic_cast<Int32*>( compound1->_key2 );
				long value = dynamic_cast<__box DateTime*>( compound1->_value )->Ticks;
				firstKey.SetValue( value );
				lastKey._first = *dynamic_cast<Int32*>( compound2->_key1 );
				lastKey._second = *dynamic_cast<Int32*>( compound2->_key2 );
				value = dynamic_cast<__box DateTime*>( compound2->_value )->Ticks;
				lastKey.SetValue( value );
				static_cast< BTreeKey< CompoundKeyWithValue<int,int,long> >* >( _firstKey )->SetKey( firstKey );
				static_cast< BTreeKey< CompoundKeyWithValue<int,int,long> >* >( _lastKey )->SetKey( lastKey );
				break;
			}
			case int_datetime_int_Key:
			{
				CompoundAndValue* compound1 = dynamic_cast<CompoundAndValue*>( beginKey->Key );
				CompoundAndValue* compound2 = dynamic_cast<CompoundAndValue*>( endKey->Key );
				CompoundKeyWithValue<int,long,int> firstKey;
				CompoundKeyWithValue<int,long,int> lastKey;
				firstKey._first = *dynamic_cast<Int32*>( compound1->_key1 );
				firstKey._second = dynamic_cast<__box DateTime*>( compound1->_key2 )->Ticks;
				int value = *dynamic_cast<Int32*>( compound1->_value );
				firstKey.SetValue( value );
				lastKey._first = *dynamic_cast<Int32*>( compound2->_key1 );
				lastKey._second = dynamic_cast<__box DateTime*>( compound2->_key2 )->Ticks;
				value = *dynamic_cast<Int32*>( compound2->_value );
				lastKey.SetValue( value );
				static_cast< BTreeKey< CompoundKeyWithValue<int,long,int> >* >( _firstKey )->SetKey( firstKey );
				static_cast< BTreeKey< CompoundKeyWithValue<int,long,int> >* >( _lastKey )->SetKey( lastKey );
				break;
			}
			case long_Key:
			{
				long firstKey = *dynamic_cast<Int64*>( beginKey->Key );
				long lastKey = *dynamic_cast<Int64*>( endKey->Key );
				static_cast< BTreeKey<long>* >( _firstKey )->SetKey( firstKey );
				static_cast< BTreeKey<long>* >( _lastKey )->SetKey( lastKey );
				break;
			}
			case datetime_Key:
			{
				long firstKey = dynamic_cast<__box DateTime*>( beginKey->Key )->Ticks;
				long lastKey = dynamic_cast<__box DateTime*>( endKey->Key )->Ticks;
				static_cast< BTreeKey<long>* >( _firstKey )->SetKey( firstKey );
				static_cast< BTreeKey<long>* >( _lastKey )->SetKey( lastKey );
				break;
			}
			case double_Key:
			{
				double firstKey = *dynamic_cast<Double*>( beginKey->Key );
				double lastKey = *dynamic_cast<Double*>( endKey->Key );
				static_cast< BTreeKey<double>* >( _firstKey )->SetKey( firstKey );
				static_cast< BTreeKey<double>* >( _lastKey )->SetKey( lastKey );
				break;
			}
			default: throw new System::Exception( "Key type is not supported!" );
		}
		_firstKey->SetOffset( 0 );
		_lastKey->SetOffset( MAX_OFFSET );
	}

	BTreePageBase* OmniaMeaBTree::GetPageByOffset( int offset )
	{
		BTreePageBase* page = _pagesCache->TryOffset( offset );
		if( page == 0 )
		{
			page = PrepareNewPage( offset );
			LoadPage( page );
		}
		return page;
	}

	BTreePageBase* OmniaMeaBTree::AllocPage()
	{
		++_numberOfPages;

		int newOffset;
		BTreePageBase* page;
		int freeOffsetsCount = _freeOffsets->Count;

		if( freeOffsetsCount == 0 )
		{
			newOffset = (int) _btreeFile->Length;
			page = PrepareNewPage( newOffset );
			page->Clear();
			SavePage( page );
		}
		else
		{
			newOffset = _freeOffsets->get_Item( freeOffsetsCount - 1 );
			_freeOffsets->RemoveAt( freeOffsetsCount - 1 );
			page = PrepareNewPage( newOffset );
			page->Clear();
		}
		return page;
	}

	BTreePageBase* OmniaMeaBTree::PrepareNewPage( int offset )
	{
		BTreePageBase* page = _freePage;
		if( !page )
		{
			page = _page->Clone();
		}
		page->SetOffset( offset );
		_freePage = _pagesCache->CachePage( page );
		return page;
	}

	void OmniaMeaBTree::CopyOffsets( const BTreeKeyBase** temp_keys, unsigned count, IntArrayList* offsets )
	{
		for( unsigned i = 0; i < count; ++i )
		{
			offsets->Add( temp_keys[ i ]->GetOffset() );
		}
	}

	static void CopyLongKeys( const BTreeKeyBase** temp_keys, int count, ArrayList* keys_offsets, IFixedLengthKey* factoryKey )
	{
		for( int i = 0; i < count; ++i )
		{
			const BTreeKeyBase* key = temp_keys[ i ];
			KeyPair* keyPair = new KeyPair();
			keyPair->_offset = key->GetOffset();
			keyPair->_key = factoryKey->FactoryMethod();
			const BTreeKey< long >* cKey =  static_cast< const BTreeKey< long >* >( key );
			Int64 theKey( cKey->GetKey() );
			keyPair->_key->Key = dynamic_cast< System::IComparable* >( __box( theKey ) );
			keys_offsets->Add( keyPair );
		}
	}

	static void CopyIntKeys( const BTreeKeyBase** temp_keys, int count, ArrayList* keys_offsets, IFixedLengthKey* factoryKey )
	{
		for( int i = 0; i < count; ++i )
		{
			const BTreeKeyBase* key = temp_keys[ i ];
			KeyPair* keyPair = new KeyPair();
			keyPair->_offset = key->GetOffset();
			keyPair->_key = factoryKey->FactoryMethod();
			const BTreeKey< int >* cKey =  static_cast< const BTreeKey< int >* >( key );
			Int32 theKey( cKey->GetKey() );
			keyPair->_key->Key = dynamic_cast< System::IComparable* >( __box( theKey ) );
			keys_offsets->Add( keyPair );
		}
	}

	static void CopyIntIntIntKeys( const BTreeKeyBase** temp_keys, int count, ArrayList* keys_offsets, IFixedLengthKey* factoryKey )
	{
		for( int i = 0; i < count; ++i )
		{
			const BTreeKeyBase* key = temp_keys[ i ];
			KeyPair* keyPair = new KeyPair();
			keyPair->_offset = key->GetOffset();
			keyPair->_key = factoryKey->FactoryMethod();
			const BTreeKey< CompoundKeyWithValue<int,int,int> >* cKey =
				static_cast< const BTreeKey< CompoundKeyWithValue<int,int,int> >* >( key );
			Int32 key1( cKey->GetKey()._first );
			Int32 key2( cKey->GetKey()._second );
			Int32 value( cKey->GetKey().GetValue() );
			CompoundAndValue* theKey = dynamic_cast< CompoundAndValue* >( keyPair->_key->Key );
			theKey->_key1 = __box( key1 );
			theKey->_key2 = __box( key2 );
			theKey->_value = __box( value );
			keyPair->_key->Key = dynamic_cast< System::IComparable* >( theKey );
			keys_offsets->Add( keyPair );
		}
	}

	static void CopyIntIntDateTimeKeys( const BTreeKeyBase** temp_keys, int count, ArrayList* keys_offsets, IFixedLengthKey* factoryKey )
	{
		for( int i = 0; i < count; ++i )
		{
			const BTreeKeyBase* key = temp_keys[ i ];
			KeyPair* keyPair = new KeyPair();
			keyPair->_offset = key->GetOffset();
			keyPair->_key = factoryKey->FactoryMethod();
			const BTreeKey< CompoundKeyWithValue<int,int,long> >* cKey =
				static_cast< const BTreeKey< CompoundKeyWithValue<int,int,long> >* >( key );
			Int32 key1( cKey->GetKey()._first );
			Int32 key2( cKey->GetKey()._second );
			DateTime value( cKey->GetKey().GetValue() );
			CompoundAndValue* theKey = dynamic_cast< CompoundAndValue* >( keyPair->_key->Key );
			theKey->_key1 = __box( key1 );
			theKey->_key2 = __box( key2 );
			theKey->_value = __box( value );
			keyPair->_key->Key = dynamic_cast< System::IComparable* >( theKey );
			keys_offsets->Add( keyPair );
		}
	}

	static void CopyIntDateTimeIntKeys( const BTreeKeyBase** temp_keys, int count, ArrayList* keys_offsets, IFixedLengthKey* factoryKey )
	{
		for( int i = 0; i < count; ++i )
		{
			const BTreeKeyBase* key = temp_keys[ i ];
			KeyPair* keyPair = new KeyPair();
			keyPair->_offset = key->GetOffset();
			keyPair->_key = factoryKey->FactoryMethod();
			const BTreeKey< CompoundKeyWithValue<int,long,int> >* cKey =
				static_cast< const BTreeKey< CompoundKeyWithValue<int,long,int> >* >( key );
			Int32 key1( cKey->GetKey()._first );
			DateTime key2( cKey->GetKey()._second );
			Int32 value( cKey->GetKey().GetValue() );
			CompoundAndValue* theKey = dynamic_cast< CompoundAndValue* >( keyPair->_key->Key );
			theKey->_key1 = __box( key1 );
			theKey->_key2 = __box( key2 );
			theKey->_value = __box( value );
			keyPair->_key->Key = dynamic_cast< System::IComparable* >( theKey );
			keys_offsets->Add( keyPair );
		}
	}

	void OmniaMeaBTree::CopyKeys( const BTreeKeyBase** temp_keys, int count, ArrayList* keys_offsets )
	{
		switch( _keyType )
		{
			case long_Key:
			{
				CopyLongKeys( temp_keys, count, keys_offsets, _factoryKey );
				break;
			}
			case int_Key:
			{
				CopyIntKeys( temp_keys, count, keys_offsets, _factoryKey );
				break;
			}
			case int_int_int_Key:
			{
				CopyIntIntIntKeys( temp_keys, count, keys_offsets, _factoryKey );
				break;
			}
			case int_int_datetime_Key:
			{
				CopyIntIntDateTimeKeys( temp_keys, count, keys_offsets, _factoryKey );
				break;
			}
			case int_datetime_int_Key:
			{
				CopyIntDateTimeIntKeys( temp_keys, count, keys_offsets, _factoryKey );
				break;
			}
		}
	}

	void OmniaMeaBTree::LoadPage( BTreePageBase* page )
	{
		int pageHandle = page->GetFileHandle();
		int fileHandle = _btreeFile->Handle.ToInt32();
		if( pageHandle != fileHandle )
		{
			char s[ 100 ];
			sprintf( s, "Page handle(%d) is not equal to btree file handle(%d).", pageHandle, fileHandle );
			throw new System::IO::IOException( s );
		}
		int loadedBytes = page->Load();
		int pageSize = page->GetSize();
		if( loadedBytes != pageSize )
		{
			char s[ 200 ];
			sprintf( s, "BTreePageBase.Load() returned %d, pageSize = %d, last error = %d.", loadedBytes, pageSize, ::GetLastError() );
			throw new System::IO::IOException( s );
		}
        _loadedPages++;
	}

	void OmniaMeaBTree::SavePage( BTreePageBase* page )
	{
		int pageHandle = page->GetFileHandle();
		int fileHandle = _btreeFile->Handle.ToInt32();
		if( pageHandle != fileHandle )
		{
			char s[ 100 ];
			sprintf( s, "Page handle(%d) is not equal to btree file handle(%d).", pageHandle, fileHandle );
			throw new System::IO::IOException( s );
		}
		int savedBytes = page->Save();
		int pageSize = page->GetSize();
		if( savedBytes != pageSize )
		{
			char s[ 200 ];
			sprintf( s, "BTreePageBase.Save() returned %d, pageSize = %d, last error = %d.", savedBytes, pageSize, ::GetLastError() );
			throw new System::IO::IOException( s );
		}
	}

    int OmniaMeaBTree::GetLoadedPages()
    {
        return _loadedPages;
    }

    int OmniaMeaBTree::GetPageSize()
    {
        return _page->GetSize();
    }
}
