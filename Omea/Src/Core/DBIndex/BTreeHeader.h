/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#ifndef _OMEA_BTREEHEADER_H
#define _OMEA_BTREEHEADER_H

#include <map>
#include "DBIndexHeapObject.h"
#include "BTreeKey.h"

using namespace std;

namespace DBIndex
{
	class BTreeHeaderIteratorBase : public DBIndexHeapObject
	{
	public:

		virtual bool Exhausted() const = 0;
		virtual bool MoveNextPage() = 0;
		virtual void GetCurrentKey( BTreeKeyBase& key ) const = 0; // return value should be deleted on one's own!
		virtual int GetCurrentOffset() const = 0;
		virtual BTreeHeaderIteratorBase& operator =( const BTreeHeaderIteratorBase& ) = 0;
	};

	class BTreeHeaderBase : public DBIndexHeapObject
	{
	public:

		virtual void GetPage( const BTreeKeyBase& key, BTreeHeaderIteratorBase& ) const = 0;
		virtual void GetMinimumPage( BTreeHeaderIteratorBase& ) const = 0;
		virtual void GetMaximumPage( BTreeHeaderIteratorBase& ) const = 0;
		virtual void SetPageOffset( const BTreeKeyBase& key, int offset ) = 0;
		virtual void DeletePageOffset( const BTreeKeyBase& key ) = 0;
		virtual void Clear() = 0;
		virtual bool Load( int fileHandle ) = 0;
		virtual bool Save( int fileHandle ) const = 0;
		virtual unsigned Size() const = 0;
	};

	template< class Key > class BTreeHeaderIterator : public BTreeHeaderIteratorBase
	{
		typedef BTreeKey< Key > KeyType;
		typedef BTreeHeaderKey< Key > HeaderKeyType;
		typedef map< HeaderKeyType, int, less< HeaderKeyType >,
			DBIndex_allocator< pair< HeaderKeyType, int > > > headerType;

	public:

		BTreeHeaderIterator() {}
		BTreeHeaderIterator( typename headerType::const_iterator it, typename headerType::const_iterator end )
			: _it( it ), _end( end ) {}

		virtual bool Exhausted() const
		{
			return _it == _end;
		}
		virtual bool MoveNextPage()
		{
			if( !Exhausted() )
			{
				++_it;
			}
			return !Exhausted();
		}
		virtual void GetCurrentKey( BTreeKeyBase& key ) const
		{
			KeyType& outKey = static_cast< KeyType& >( key );
			outKey.SetKey( _it->first.GetKey() );
			outKey.SetOffset( _it->first.GetOffset() );
		}
		virtual int GetCurrentOffset() const
		{
			return _it->second;
		}

		virtual BTreeHeaderIteratorBase& operator =( const BTreeHeaderIteratorBase& it )
		{
			const BTreeHeaderIterator< Key >& i = static_cast< const BTreeHeaderIterator< Key >& >( it );
			_it = i._it;
			_end = i._end;
			return *this;
		}

	private:

		typename headerType::const_iterator _it;
		typename headerType::const_iterator _end;
	};

	template< class Key > class BTreeHeader : public BTreeHeaderBase
	{
		typedef BTreeKey< Key > KeyType;
		typedef BTreeHeaderKey< Key > HeaderKeyType;
		typedef pair< HeaderKeyType, int > HeaderKeyOffset;
		typedef map< HeaderKeyType, int, less< HeaderKeyType >, DBIndex_allocator< HeaderKeyOffset > > headerType;
		typedef BTreeHeaderIterator< Key > headerIteratorType;

	public:

		virtual void GetPage( const BTreeKeyBase& key, BTreeHeaderIteratorBase& it ) const
		{
			const KeyType& realKey = static_cast<const KeyType&>( key );
			HeaderKeyType headerKey( realKey.GetKey(), realKey.GetOffset() );
			headerType::const_iterator i = _header.upper_bound( headerKey );
			if( i != _header.begin() )
			{
				--i;
			}
			it = headerIteratorType( i, _header.end() );
		}
		virtual void GetMinimumPage( BTreeHeaderIteratorBase& it ) const
		{
			it = headerIteratorType( _header.begin(), _header.end() );
		}
		virtual void GetMaximumPage( BTreeHeaderIteratorBase& it ) const
		{
			headerType::const_iterator begin = _header.begin();
			headerType::const_iterator end = _header.end();
			for( unsigned i = 1; i < _header.size(); ++i )
			{
				++begin;
			}
			it = headerIteratorType( begin, end );
		}
		virtual void SetPageOffset( const BTreeKeyBase& key, int offset )
		{
			const KeyType& realKey = static_cast<const KeyType&>( key );
			HeaderKeyType headerKey( realKey.GetKey(), realKey.GetOffset() );
			_header[ headerKey ] = offset;
		}
		virtual void DeletePageOffset( const BTreeKeyBase& key )
		{
			const KeyType& realKey = static_cast<const KeyType&>( key );
			HeaderKeyType headerKey( realKey.GetKey(), realKey.GetOffset() );
			_header.erase( headerKey );
		}
		virtual void Clear()
		{
			_header.clear();
		}

		virtual bool Load( int fileHandle )
		{
			HANDLE file = (HANDLE) fileHandle;
			int size = ::GetFileSize( file, NULL ) - ::SetFilePointer( file, 0, NULL, FILE_CURRENT );
			if( size > 0 )
			{
				DWORD rawSize = size;
				HeaderKeyOffset* keys = (HeaderKeyOffset*) DBIndexHeapObject::operator new( rawSize );
				DWORD read;
				::ReadFile( file, (LPVOID) keys, rawSize, &read, NULL );
				size /= sizeof( HeaderKeyOffset );
				for( int i = 0; i < size; ++i )
				{
					_header[ keys[ i ].first ] = keys[ i ].second;
				}
				DBIndexHeapObject::operator delete( keys );
				return rawSize == read;
			}
			return true;
		}

		virtual bool Save( int fileHandle ) const
		{
			int size = _header.size();
			if( size )
			{
				int rawSize = sizeof( HeaderKeyOffset ) * size;
				HeaderKeyOffset* keys = (HeaderKeyOffset*) DBIndexHeapObject::operator new( rawSize );
				int i = 0;
				for( headerType::const_iterator it = _header.begin(); it != _header.end(); ++it, ++i )
				{
					keys[ i ] = *it;
				}
				DWORD written = 0;
				::WriteFile( (HANDLE) fileHandle, (LPCVOID) keys, size * sizeof( HeaderKeyOffset ), &written, NULL );
				DBIndexHeapObject::operator delete( keys );
				return written == rawSize;
			}
			return true;
		}

		virtual unsigned Size() const
		{
			return _header.size();
		}

	private:

		headerType	_header;
	};
}

#endif
