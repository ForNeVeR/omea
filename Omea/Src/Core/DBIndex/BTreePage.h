/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#ifndef _OMEA_BTREEPAGE_H
#define _OMEA_BTREEPAGE_H

#include <memory.h>
#ifdef _MSC_VER
#include <windows.h>
#else
#include <io.h>
#endif
#include "DBIndexHeapObject.h"
#include "BTreeKey.h"

///////////////////////////////////////////////////////////////////////////////
// maximum number of keys in a page is equal to 2^10 - 2
// page size is actually greater on two keys: the first (zero-based) is null
// object necessary for red-black tree fixup operations, and the second used
// for storing extra info, such as count of keys and free list first index
///////////////////////////////////////////////////////////////////////////////

#define MAX_KEYS_IN_PAGE		1022

///////////////////////////////////////////////////////////////////////////////
// if a page is of size not less than defined below, then we consider it as
// "almost full page"
// attempt of insertion to an almost full page of a key which is greater or
// equal to page's maximum leads to allocation a new page where the key
// actually will be inserted
// this feature allows to minimize splitting when key data is partially ordered
///////////////////////////////////////////////////////////////////////////////

#define ALMOST_FULL_PAGE_SIZE	( MAX_KEYS_IN_PAGE - 64 )

///////////////////////////////////////////////////////////////////////////////
// magic number used for checking pages integrity
// hex digits of the pi number
///////////////////////////////////////////////////////////////////////////////

#define BTREE_PAGE_MAGIC_NUMBER 0xbb40e609


namespace DBIndex
{
	///////////////////////////////////////////////////////////////////////////
	// base abstract class for all BTree pages
	///////////////////////////////////////////////////////////////////////////

	class BTreePageBase : public DBIndexHeapObject
	{
	public:

		virtual BTreePageBase* Clone() const = 0;

		virtual int Load() = 0;
		virtual int Save() = 0;
		virtual void Clear() = 0;
		virtual int GetSize() const = 0;
		__forceinline void SetFileHandle( int fh ) { _fileHandle = fh; }
		__forceinline int GetFileHandle() const { return _fileHandle; }
		__forceinline int GetOffset() const { return _fileOffset; }
		__forceinline void SetOffset( int offset )
		{
			if( _fileOffset != offset )
			{
				_fileOffset = offset;
				_dirty = true;
			}
		}

		virtual bool operator<( BTreePageBase& page ) = 0;

		virtual unsigned SearchForRange( const BTreeKeyBase& first, const BTreeKeyBase& last, const BTreeKeyBase* keys[] ) const = 0;
		virtual unsigned GetAllKeys( const BTreeKeyBase* keys[] ) const = 0;

		virtual void Insert( const BTreeKeyBase& ) = 0;
		virtual bool Delete( const BTreeKeyBase& ) = 0;
		virtual const BTreeKeyBase& GetMinimum() = 0;
		virtual const BTreeKeyBase& GetMaximum() = 0;
		virtual const BTreeKeyBase& GetSuccessor( const BTreeKeyBase& ) const = 0;

		virtual void Split( BTreePageBase& ) = 0;
		virtual void Merge( const BTreePageBase& ) = 0;

		virtual int GetCount() const = 0;
		virtual void SetCount( int count ) = 0;
		virtual int IncCount() = 0;
		virtual int DecCount() = 0;
		__forceinline bool IsFull() const { return GetCount() == MAX_KEYS_IN_PAGE; }
		__forceinline bool IsAlmostFull() const { return GetCount() >= ALMOST_FULL_PAGE_SIZE; }
		
	protected:

		BTreePageBase( int fileHandle, int offset )
			: _magickNumber( BTREE_PAGE_MAGIC_NUMBER ), _fileHandle( fileHandle ), _fileOffset( offset ), _dirty( true ) {}

		unsigned		_magickNumber;
		int				_fileHandle;
		int				_fileOffset;
		bool			_dirty;
	};

	///////////////////////////////////////////////////////////////////////////
	// template class for pages with specified key
	///////////////////////////////////////////////////////////////////////////

	template< class Key > class BTreePage : public BTreePageBase
	{
	public:

		BTreePage( int fileHandle, int offset  )
			: BTreePageBase( fileHandle, offset )
		{
			ClearImpl();
		}

		virtual BTreePageBase* Clone() const
		{
			return new BTreePage< Key >( _fileHandle, _fileOffset );
		}

		virtual int Load()
		{
			DWORD read;
#ifdef _MSC_VER
			DWORD pageSize = (DWORD) GetSize();
			::SetFilePointer( (HANDLE) _fileHandle, (LONG) _fileOffset, NULL, FILE_BEGIN );
			::ReadFile( (HANDLE) _fileHandle, (LPVOID) &_tree, pageSize, &read, NULL );
			_dirty = ( pageSize != read );
#else
			_lseek( _fileHandle, _fileOffset, SEEK_SET );
			int pageSize = GetSize();
			read = _read( _fileHandle, (void*)&_tree, pageSize );
			_dirty = read != pageSize;
#endif
			_minimumIndex = _maximumIndex = 0;

			// check page integrity
			if( !_dirty )
			{
				unsigned rootIndex = GetRootIndex();
				_dirty = ( rootIndex >> 10 ) != ( BTREE_PAGE_MAGIC_NUMBER >> 10 );
				if( !_dirty )
				{
					// clear integrity marker
					SetRootIndex( rootIndex ^ BTREE_PAGE_MAGIC_NUMBER );
				}
			}
			return read;
		}
		virtual int Save()
		{
			DWORD pageSize = (DWORD) GetSize();
			if( _dirty )
			{
				// set integrity marker
				unsigned rootIndex = GetRootIndex();
				SetRootIndex( rootIndex ^ BTREE_PAGE_MAGIC_NUMBER );
				DWORD written;
#ifdef _MSC_VER
				::SetFilePointer( (HANDLE) _fileHandle, (LONG) _fileOffset, NULL, FILE_BEGIN );
				::WriteFile( (HANDLE) _fileHandle, (LPCVOID) &_tree, pageSize, &written, NULL );
				_dirty = written != pageSize;
#else
				_lseek( _fileHandle, _fileOffset, SEEK_SET );
				written = _write( _fileHandle, (const void*) &_tree, pageSize );
				_dirty = false;
#endif
				// clear integrity marker
				SetRootIndex( rootIndex );
				return written;
			}
			return pageSize;
		}

		virtual void Clear()
		{
			ClearImpl();
		}

		virtual int GetSize() const
		{
			return sizeof( _tree );
		}

		virtual bool operator<( BTreePageBase& page )
		{
			const KeyType& rightKey = static_cast< const KeyType& >( page.GetMinimum() );
			const KeyType& thisKey = static_cast< const KeyType& >( GetMinimum() );
			return thisKey < rightKey;
		}

		virtual unsigned SearchForRange( const BTreeKeyBase& first, const BTreeKeyBase& last, const BTreeKeyBase* keys[] ) const
		{
			const KeyType& realFirst = static_cast< const KeyType& >( first );
			const KeyType& realLast = static_cast< const KeyType& >( last );

			unsigned next = 0;
			unsigned index = GetRootIndex();
			while( index )
			{
				const KeyType& key = _tree[ index ];
				if( key < realFirst )
				{
					index = key.GetRight();
				}
				else
				{
					next = index;
					index = key.GetLeft();
				}
			}
			unsigned count = 0;
			unsigned totalCount = GetCount();
			while( next )
			{
				const KeyType& key = _tree[ next ];
				if( realLast < key )
				{
					break;
				}
				if( count == totalCount )
				{
					return MAX_KEYS_IN_PAGE + 1;
				}
				keys[ count++ ] = &key;
				next = GetSuccessor( key, next );
			}
			return count;
		}
		virtual unsigned GetAllKeys( const BTreeKeyBase* keys[] ) const
		{
			unsigned index = ( _minimumIndex > 0 ) ? _minimumIndex : GetMinimum( GetRootIndex() );
			unsigned count = 0;
			unsigned totalCount = GetCount();
			while( index )
			{
				if( count == totalCount )
				{
					return MAX_KEYS_IN_PAGE + 1;
				}
				const KeyType& key = _tree[ index ];
				keys[ count++ ] = &key;
				index = GetSuccessor( key, index );
			}
			return count;
		}

		virtual void Insert( const BTreeKeyBase& key )
		{
			const KeyType& realKey = static_cast< const KeyType& >( key );
			unsigned index = GetFirstFree(); // at first check free list
			if( index )
			{
				SetFirstFree( _tree[ index ].GetRight() );
			}
			else
			{
				index = GetCount() + 2;
			}
			KeyType& allocatedKey = _tree[ index ];
			allocatedKey = realKey;
			allocatedKey.SetRight( 0 );
			allocatedKey.SetLeft( 0 );
			if( GetCount() == 0 )
			{
				allocatedKey.SetColor( BLACK );
				allocatedKey.SetParent( 0 );
				SetRootIndex( index );
			}
			else
			{
				unsigned pIndex = GetRootIndex();
				for( ; ; )
				{
					KeyType& current = _tree[ pIndex ];
					bool isLess = realKey < current;
					unsigned childIndex = isLess ? current.GetLeft() : current.GetRight();
					if( !childIndex )
					{
						allocatedKey.SetParent( pIndex );
						if( isLess )
						{
							current.SetLeft( index );
						}
						else
						{
							current.SetRight( index );
						}
						break;
					}
					pIndex = childIndex;
				}
				allocatedKey.SetColor( RED );
				while( index != GetRootIndex() )
				{
					pIndex = _tree[ index ].GetParent();
					KeyType& parent = _tree[ pIndex ];
					if( parent.GetColor() == BLACK )
					{
						break;
					}
					unsigned ppIndex = parent.GetParent();
					KeyType& parentParent = _tree[ ppIndex ];
					unsigned ppLeftIndex = parentParent.GetLeft();
					unsigned ppRightIndex = parentParent.GetRight();
					if( ppLeftIndex == pIndex )
					{
						KeyType& y = _tree[ ppRightIndex ];
						if( y.GetColor() == RED )
						{
							parent.SetColor( BLACK );
							y.SetColor( BLACK );
							parentParent.SetColor( RED );
							index = ppIndex;
						}
						else if( parent.GetRight() == index )
						{
							index = pIndex;
							LeftRotate( index );
						}
						else
						{
							parent.SetColor( BLACK );
							parentParent.SetColor( RED );
							RightRotate( ppIndex );
						}
					}
					else
					{
						KeyType& y = _tree[ ppLeftIndex ];
						if( y.GetColor() == RED )
						{
							parent.SetColor( BLACK );
							y.SetColor( BLACK );
							parentParent.SetColor( RED );
							index = ppIndex;
						}
						else if( parent.GetLeft() == index )
						{
							index = pIndex;
							RightRotate( index );
						}
						else
						{
							parent.SetColor( BLACK );
							parentParent.SetColor( RED );
							LeftRotate( ppIndex );
						}
					}
				}
				_tree[ GetRootIndex() ].SetColor( BLACK ); // root's color is always Black
			}
			IncCount();
			_dirty = true;
			_minimumIndex = _maximumIndex = 0;
		}
		virtual bool Delete( const BTreeKeyBase& key )
		{
			if( GetCount() > 0 )
			{
				const KeyType& realKey = static_cast< const KeyType& >( key );
				unsigned index = GetRootIndex();
				do
				{
					KeyType& current = _tree[ index ];
					if( realKey < current )
					{
						index = current.GetLeft();
					}
					else if( current < realKey )
					{
						index = current.GetRight();
					}
					else
					{
						break;
					}
				}
				while( index );
				if( index )
				{
					Delete( index );
					_dirty = true;
					_minimumIndex = _maximumIndex = 0;
					return true;
				}
			}
			return false;
		}

		virtual const BTreeKeyBase& GetMinimum()
		{
			if( _minimumIndex == 0 )
			{
				_minimumIndex = GetMinimum( GetRootIndex() );
			}
			return _tree[ _minimumIndex ];
		}

		virtual const BTreeKeyBase& GetMaximum()
		{
			if( _maximumIndex == 0 )
			{
				_maximumIndex = GetMaximum( GetRootIndex() );
			}
			return _tree[ _maximumIndex ];
		}

		virtual const BTreeKeyBase& GetSuccessor( const BTreeKeyBase& key ) const
		{
			const KeyType* realKey = static_cast< const KeyType* >( &key );
			unsigned index = ( (char*)realKey - (char*)&_tree ) / sizeof( KeyType );
			return _tree[ GetSuccessor( *realKey, index ) ];
		}

		// !!! Only full page can be splitted !!!
		virtual void Split( BTreePageBase& rightPage )
		{
			KeyType root = _tree[ GetRootIndex() ];

			// copy keys
			KeyType	treeCopy[ MAX_KEYS_IN_PAGE ];
			memcpy( &treeCopy, &_tree[ 2 ], sizeof( treeCopy ) );

			Clear();
			for( int i = 0; i < MAX_KEYS_IN_PAGE; ++i )
			{
				const KeyType& current = treeCopy[ i ];
				if( root < current )
				{
					rightPage.Insert( current );
				}
				else
				{
					Insert( current );
				}
			}
		}
		virtual void Merge( const BTreePageBase& rightPage )
		{
			if( rightPage.GetCount() > 0 )
			{
				const thisType& page = static_cast< const thisType& >( rightPage );
				unsigned index = page.GetMinimum( page.GetRootIndex() );
				while( index )
				{
					const KeyType& key = page._tree[ index ];
					Insert( key );
					index = page.GetSuccessor( key, index );
				}
			}
		}

		///////////////////////////////////////////////////////////////////////
		// first key (with index 1) doesn't actually stores a key, but is
		// used for saving extra info, such as count of keys in the page and
		// the index of the first free key
		// freed keys are represented with linked list of indexes
		///////////////////////////////////////////////////////////////////////

		virtual int GetCount() const { return _tree[ 1 ].GetParent(); }
		virtual void SetCount( int count ) { _tree[ 1 ].SetParent( count ); }
		virtual int IncCount()
		{
			int result = _tree[ 1 ].GetParent() + 1;
			_tree[ 1 ].SetParent( result );
			return result;
		}
		virtual int DecCount()
		{
			int result = _tree[ 1 ].GetParent() - 1;
			_tree[ 1 ].SetParent( result );
			return result;
		}

	private:

		typedef BTreeKey< Key > KeyType;
		typedef BTreePage< Key > thisType;

		void ClearImpl()
		{
			// it is enough to clear only 2 header keys
			memset( (char*) &_tree, 0, 2 * sizeof( KeyType ) );
			_dirty = true;
			_minimumIndex = _maximumIndex = 0;
		}

		// subtree minimum
		unsigned GetMinimum( unsigned index ) const
		{
			unsigned left;
			while( ( left = _tree[ index ].GetLeft() ) )
			{
				index = left;
			}
			return index;
		}

		// subtree maximum
		unsigned GetMaximum( unsigned index ) const
		{
			unsigned right;
			while( ( right = _tree[ index ].GetRight() ) )
			{
				index = right;
			}
			return index;
		}

		unsigned GetSuccessor( const KeyType& key, unsigned index ) const
		{
			unsigned right = key.GetRight();
			if( right )
			{
				return GetMinimum( right );
			}
			unsigned parent = key.GetParent();
			while( parent )
			{
				const KeyType& parentKey = _tree[ parent ];
				if( parentKey.GetRight() != index )
				{
					break;
				}
				index = parent;
				parent = parentKey.GetParent();
			}
			return parent;
		}

		void Delete( unsigned index )
		{
			KeyType& z = _tree[ index ];
			unsigned i = ( !z.GetRight() || !z.GetLeft() ) ? index : GetSuccessor( z, index );
			KeyType& y = _tree[ i ];
			unsigned j = y.GetLeft();
			j = ( j ) ? j : y.GetRight();
			KeyType& x = _tree[ j ];
			unsigned parent = y.GetParent();
			x.SetParent( parent );
			if( !parent )
			{
				SetRootIndex( j );
			}
			else
			{
				_tree[ parent ].SetLeftOrRight( j, i );
			}
			if( i != index )
			{
				z = y;
			}
			if( y.GetColor() == BLACK )
			{
				DeleteFixup( j );
			}
			FreeIndex( i );
			if( DecCount() == 0 )
			{
				SetRootIndex( 0 );
			}
		}

		void LeftRotate( unsigned index )
		{
			KeyType& x = _tree[ index ];
			unsigned right = x.GetRight();
			KeyType& y = _tree[ right ];
			unsigned left = y.GetLeft();
			x.SetRight( left );
			if( left )
			{
				_tree[ left ].SetParent( index );
			}
			unsigned parent = x.GetParent();
			y.SetParent( parent );
			if( !parent )
			{
				SetRootIndex( right );
			}
			else
			{
				_tree[ parent ].SetLeftOrRight( right, index );
			}
			y.SetLeft( index );
			x.SetParent( right );
		}

		void RightRotate( unsigned index )
		{
			KeyType& x = _tree[ index ];
			unsigned left = x.GetLeft();
			KeyType& y = _tree[ left ];
			unsigned right = y.GetRight();
			x.SetLeft( right );
			if( right )
			{
				_tree[ right ].SetParent( index );
			}
			unsigned parent = x.GetParent();
			y.SetParent( parent );
			if( !parent )
			{
				SetRootIndex( left );
			}
			else
			{
				_tree[ parent ].SetRightOrLeft( left, index );
			}
			y.SetRight( index );
			x.SetParent( left );
		}

		void DeleteFixup( unsigned index )
		{
			unsigned parent, w, left, right;

			while( index != GetRootIndex() )
			{
				KeyType& x = _tree[ index ];
				if( x.GetColor() == RED )
				{
					break;
				}
				parent = x.GetParent();
				KeyType& p = _tree[ parent ];
				if( p.GetLeft() == index )
				{
					w = p.GetRight();
					if( _tree[ w ].GetColor() == RED )
					{
						_tree[ w ].SetColor( BLACK );
						p.SetColor( RED );
						LeftRotate( parent );
						w = p.GetRight();
					}
					left = _tree[ w ].GetLeft();
					right = _tree[ w ].GetRight();
					if( _tree[ left ].GetColor() == BLACK && _tree[ right ].GetColor() == BLACK )
					{
						_tree[ w ].SetColor( RED );
						index = parent;
					}
					else if( _tree[ right ].GetColor() == BLACK )
					{
						_tree[ left ].SetColor( BLACK );
						_tree[ w ].SetColor( RED );
						RightRotate( w );
						w = p.GetRight();
					}
					else
					{
						_tree[ w ].SetColor( p.GetColor() );
						p.SetColor( BLACK );
						_tree[ right ].SetColor( BLACK );
						LeftRotate( parent );
						index = GetRootIndex();
						break;
					}
				}
				else
				{
					w = p.GetLeft();
					if( _tree[ w ].GetColor() == RED )
					{
						_tree[ w ].SetColor( BLACK );
						p.SetColor( RED );
						RightRotate( parent );
						w = p.GetLeft();
					}
					left = _tree[ w ].GetLeft();
					right = _tree[ w ].GetRight();
					if( _tree[ left ].GetColor() == BLACK && _tree[ right ].GetColor() == BLACK )
					{
						_tree[ w ].SetColor( RED );
						index = parent;
					}
					else if( _tree[ left ].GetColor() == BLACK )
					{
						_tree[ right ].SetColor( BLACK );
						_tree[ w ].SetColor( RED );
						LeftRotate( w );
						w = p.GetLeft();
					}
					else
					{
						_tree[ w ].SetColor( p.GetColor() );
						p.SetColor( BLACK );
						_tree[ left ].SetColor( BLACK );
						RightRotate( parent );
						index = GetRootIndex();
						break;
					}
				}
			}
			_tree[ index ].SetColor( BLACK );
		}

		__forceinline unsigned GetRootIndex() const { return _tree[ 1 ].GetOffset(); }
		__forceinline void SetRootIndex( unsigned index ) { _tree[ 1 ].SetOffset( index ); }
		__forceinline unsigned GetFirstFree() const { return _tree[ 1 ].GetRight(); }
		__forceinline void SetFirstFree( unsigned index ) { _tree[ 1 ].SetRight( index ); }
		void FreeIndex( unsigned index )
		{
			unsigned fEmpty = GetFirstFree();
			SetFirstFree( index );
			_tree[ index ].SetRight( fEmpty );
		}

		KeyType		_tree[ MAX_KEYS_IN_PAGE + 2 ];
		unsigned	_minimumIndex;
		unsigned	_maximumIndex;
	};
}

#endif