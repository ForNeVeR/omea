// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "TypeFactory.h"

namespace DBIndex
{
	BTreeKeyBase* TypeFactory::NewKey( int type )
	{
		switch( type )
		{
			case int_Key: return new BTreeKey<int>();
			case int_int_Key: return new BTreeKey< CompoundKey<int,int> >();
			case int_datetime_Key: return new BTreeKey< CompoundKey<int,long> >();
			case int_int_int_Key: return new BTreeKey< CompoundKeyWithValue<int,int,int> >();
			case int_int_datetime_Key: return new BTreeKey< CompoundKeyWithValue<int,int,long> >();
			case int_datetime_int_Key: return new BTreeKey< CompoundKeyWithValue<int,long,int> >();
			case long_Key: return new BTreeKey<long>();
			case datetime_Key: return new BTreeKey<long>();
			case double_Key: return new BTreeKey<double>();
		}
		return 0;
	}

	BTreePageBase* TypeFactory::NewPage( int type )
	{
		switch( type )
		{
			case int_Key: return new BTreePage<int>( 0, 0 );
			case int_int_Key: return new BTreePage< CompoundKey<int,int> >( 0, 0 );
			case int_datetime_Key: return new BTreePage< CompoundKey<int,long> >( 0, 0 );
			case int_int_int_Key: return new BTreePage< CompoundKeyWithValue<int,int,int> >( 0, 0 );
			case int_int_datetime_Key: return new BTreePage< CompoundKeyWithValue<int,int,long> >( 0, 0 );
			case int_datetime_int_Key: return new BTreePage< CompoundKeyWithValue<int,long,int> >( 0, 0 );
			case long_Key: return new BTreePage<long>( 0, 0 );
			case datetime_Key: return new BTreePage<long>( 0, 0 );
			case double_Key: return new BTreePage<double>( 0, 0 );
		}
		return 0;
	}

	BTreeHeaderBase* TypeFactory::NewHeader( int type )
	{
		switch( type )
		{
			case int_Key: return new BTreeHeader<int>();
			case int_int_Key: return new BTreeHeader< CompoundKey<int,int> >();
			case int_datetime_Key: return new BTreeHeader< CompoundKey<int,long> >();
			case int_int_int_Key: return new BTreeHeader< CompoundKeyWithValue<int,int,int> >();
			case int_int_datetime_Key: return new BTreeHeader< CompoundKeyWithValue<int,int,long> >();
			case int_datetime_int_Key: return new BTreeHeader< CompoundKeyWithValue<int,long,int> >();
			case long_Key: return new BTreeHeader<long>();
			case datetime_Key: return new BTreeHeader<long>();
			case double_Key: return new BTreeHeader<double>();
		}
		return 0;
	}

	BTreeHeaderIteratorBase* TypeFactory::NewHeaderIterator( int type )
	{
		switch( type )
		{
			case int_Key: return new BTreeHeaderIterator<int>();
			case int_int_Key: return new BTreeHeaderIterator< CompoundKey<int,int> >();
			case int_datetime_Key: return new BTreeHeaderIterator< CompoundKey<int,long> >();
			case int_int_int_Key: return new BTreeHeaderIterator< CompoundKeyWithValue<int,int,int> >();
			case int_int_datetime_Key: return new BTreeHeaderIterator< CompoundKeyWithValue<int,int,long> >();
			case int_datetime_int_Key: return new BTreeHeaderIterator< CompoundKeyWithValue<int,long,int> >();
			case long_Key: return new BTreeHeaderIterator<long>();
			case datetime_Key: return new BTreeHeaderIterator<long>();
			case double_Key: return new BTreeHeaderIterator<double>();
		}
		return 0;
	}

	IKeyComparer* TypeFactory::NewKeyComparer( int type )
	{
		switch( type )
		{
		    case int_Key: return BTreeKey<int>::GetKeyComparer();
			case int_int_Key: return BTreeKey< CompoundKey<int,int> >::GetKeyComparer();
			case int_datetime_Key: return BTreeKey< CompoundKey<int,long> >::GetKeyComparer();
			case int_int_int_Key: return BTreeKey< CompoundKeyWithValue<int,int,int> >::GetKeyComparer();
			case int_int_datetime_Key: return BTreeKey< CompoundKeyWithValue<int,int,long> >::GetKeyComparer();
			case int_datetime_int_Key: return BTreeKey< CompoundKeyWithValue<int,long,int> >::GetKeyComparer();
			case long_Key: return BTreeKey<long>::GetKeyComparer();
			case datetime_Key: return BTreeKey<long>::GetKeyComparer();
			case double_Key: return BTreeKey<double>::GetKeyComparer();
		}
		return 0;
	}

	void TypeFactory::DeleteKey( BTreeKeyBase* key )
	{
		delete key;
	}

	void TypeFactory::DeletePage( BTreePageBase* page )
	{
		delete page;
	}

	void TypeFactory::DeleteHeader( BTreeHeaderBase* header )
	{
		delete header;
	}

	void TypeFactory::DeleteHeaderIterator( BTreeHeaderIteratorBase* iterator )
	{
		delete iterator;
	}

	void TypeFactory::DeleteKeyComparer( IKeyComparer* comparer )
	{
		delete comparer;
	}
}
