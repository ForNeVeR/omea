/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#ifndef _OMEA_BTREEKEY_H
#define _OMEA_BTREEKEY_H

#include "DBIndexHeapObject.h"

namespace DBIndex
{

//#define OMEA_USE_ASSEMBLY

// red-black tree colors
#define RED		1
#define BLACK	0

// maximum value of record offset
#define MAX_OFFSET	0x7fffffff

//#define __forceinline

	///////////////////////////////////////////////////////////////////////////
	// base class for BTree keys placed in BTree pages
	// encapsulates offset, RBTree relatives and RBTree color
	///////////////////////////////////////////////////////////////////////////

	class BTreeKeyBase : public DBIndexHeapObject
	{
	public:

		BTreeKeyBase()
			: _offset( 0 ), _parent( 0 ), _left( 0 ), _right( 0 ), _color( 0 ) {}

		__forceinline void SetOffset( int offset ) { _offset = offset; }
		__forceinline int GetOffset() const { return _offset; }

		// RBTree relatives and color
		__forceinline void SetParent( unsigned parent ) { _parent = parent; }
		__forceinline unsigned GetParent() const { return _parent; }
		__forceinline void SetLeft( unsigned left ) { _left = left; }
		__forceinline unsigned GetLeft() const { return _left; }
		__forceinline void SetRight( unsigned right ) { _right = right; }
		__forceinline unsigned GetRight() const { return _right; }
		__forceinline void SetColor( unsigned color ) { _color = color; }
		__forceinline unsigned GetColor() const { return _color; }
		__forceinline void SetLeftOrRight( unsigned newChild, unsigned oldLeft )
		{
			if( _left == oldLeft )
			{
				_left = newChild;
			}
			else
			{
				_right = newChild;
			}
		}
		__forceinline void SetRightOrLeft( unsigned newChild, unsigned oldRight )
		{
			if( _right == oldRight )
			{
				_right = newChild;
			}
			else
			{
				_left = newChild;
			}
		}

	protected:

		int				_offset;
		///////////////////////////////////////////////////////////////////////
		// use int, not bool, otherwise it wouldn't be packed correctly
		// values: 1 - Red, 0 - Black
		///////////////////////////////////////////////////////////////////////
		unsigned		_color : 1;
		unsigned		_parent : 10;
		unsigned		_left : 10;
		unsigned		_right : 10;
	};

	///////////////////////////////////////////////////////////////////////////
	// The following class grants polymorphic way to compare BTree keys as
	// instances of the BTreeKey template class. Each implementation of
	// the template just should implement its own comparer
	///////////////////////////////////////////////////////////////////////////

	class IKeyComparer : public DBIndexHeapObject
	{
	public:
		virtual bool Equals( const BTreeKeyBase&, const BTreeKeyBase& ) const = 0;
		virtual bool Less( const BTreeKeyBase&, const BTreeKeyBase& ) const = 0;
	};

	template< class Key > class KeyComparer : public IKeyComparer
	{
		virtual bool Equals( const BTreeKeyBase& l, const BTreeKeyBase& r ) const
		{
			const BTreeKey< Key >& keyL = static_cast< const BTreeKey< Key >& >( l );
			const BTreeKey< Key >& keyR = static_cast< const BTreeKey< Key >& >( r );
			return keyL == keyR;
		}
		virtual bool Less( const BTreeKeyBase& l, const BTreeKeyBase& r ) const
		{
			const BTreeKey< Key >& keyL = static_cast< const BTreeKey< Key >& >( l );
			const BTreeKey< Key >& keyR = static_cast< const BTreeKey< Key >& >( r );
			return keyL < keyR;
		}
	};

	///////////////////////////////////////////////////////////////////////////
	// template class for BTree key with data placed in BTree pages
	// parameter class should implement the == and the < operators
	///////////////////////////////////////////////////////////////////////////

	template< class Key > class BTreeKey : public BTreeKeyBase
	{
	public:

		BTreeKey() : BTreeKeyBase(), _key() {}

		__forceinline const Key& GetKey() const { return _key; }
		__forceinline void SetKey( const Key& key )
		{
			_key = key;
		}
		static IKeyComparer* GetKeyComparer() { return new KeyComparer< Key >(); }

		__forceinline BTreeKey< Key >& operator= ( const BTreeKey< Key >& key )
		{
			_key = key._key;
			_offset = key._offset;
			return *this;
		}
		__forceinline bool operator==( const BTreeKey< Key >& key ) const
		{
			return _offset == key._offset && _key == key._key;
		}
		__forceinline bool operator<( const BTreeKey< Key >& key ) const
		{
			return ( _key < key._key ) || ( _key == key._key && _offset < key._offset );
		}

	private:

		Key		_key;
	};

	///////////////////////////////////////////////////////////////////////////
	// template class for BTree keys with data placed in memory (BTree header)
	// BTree header is supposed to be implemented by std::map
	///////////////////////////////////////////////////////////////////////////

	template< class Key > class BTreeHeaderKey : public DBIndexHeapObject
	{
	public:

		BTreeHeaderKey()
			: _key(), _offset( 0 ) {}
		BTreeHeaderKey( const BTreeHeaderKey& right )
			: _key( right._key ), _offset( right._offset ) {}
		BTreeHeaderKey( const Key& key, int offset )
			: _key( key ), _offset( offset ) {}

		__forceinline bool operator==( const BTreeHeaderKey< Key >& key ) const
		{
			return _offset == key._offset && _key == key._key;
		}
		__forceinline bool operator<( const BTreeHeaderKey< Key> & key ) const
		{
			return ( _key < key._key ) || ( _key == key._key && _offset < key._offset );
		}

		__forceinline const Key& GetKey() const { return _key; }
		__forceinline int GetOffset() const { return _offset; }

	private:

		Key		_key;
		int		_offset;
	};

	///////////////////////////////////////////////////////////////////////////
	// template class for compound key
	///////////////////////////////////////////////////////////////////////////

	template < class Key1, class Key2 > class CompoundKey : public DBIndexHeapObject
	{
		typedef CompoundKey< Key1, Key2 > keyType;
	public:

		__forceinline bool operator ==( const keyType& right ) const
		{
			return _first == right._first && _second == right._second;
		}
		__forceinline bool operator <( const keyType& right ) const
		{
			return ( _first < right._first ) || ( _first == right._first && _second < right._second );
		}

		__forceinline keyType& operator =( const keyType& right )
		{
			_first = right._first;
			_second = right._second;
			return *this;
		}

		Key1	_first;
		Key2	_second;
	};

	///////////////////////////////////////////////////////////////////////////
	// template class for compound key with value
	///////////////////////////////////////////////////////////////////////////

	template < class Key1, class Key2, class Value > class CompoundKeyWithValue : public CompoundKey< Key1, Key2 >
	{
		Value	_value;
		typedef CompoundKeyWithValue< Key1, Key2, Value > keyType;
		typedef CompoundKey< Key1, Key2 > baseType;
	public:

		CompoundKeyWithValue() : baseType(), _value() {}

		__forceinline keyType& operator =( const keyType& right )
		{
			baseType::operator=( right );
			_value = right._value;
			return *this;
		}

		__forceinline bool operator ==( const keyType& right ) const
		{
			return _first == right._first && _second == right._second && _value == right._value;
		}

		__forceinline bool operator <( const keyType& right ) const
		{
			if( _first < right._first )
			{
				return true;
			}
			if( _first == right._first )
			{
				if( _second < right._second )
				{
					return true;
				}
				if( _second == right._second )
				{
					return _value < right._value;
				}
			}
			return false;
		}

		__forceinline void SetValue( const Value& value ) { _value = value; }
		__forceinline const Value& GetValue() const { return _value; }
	};
}

#endif