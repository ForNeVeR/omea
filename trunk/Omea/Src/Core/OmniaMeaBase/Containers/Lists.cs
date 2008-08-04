/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using JetBrains.Omea.Algorithms;
using System.Text;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.Containers
{
    /** 
     * ArrayList of ints
     */

    public class IntArrayList: ICollection, ICloneable
    {
        private int[]       _items;
        private int         _size;
        private int         _version;
        private const int   InitialCapacity = 2;

        public IntArrayList()
        {
            _items = new int [ InitialCapacity ];
        }

        public IntArrayList( int capacity )
        {
            _items = new int [ AdjustCapacity( capacity ) ];
        }

        public IntArrayList( ICollection collection )
        {
            _items = new int [ AdjustCapacity( collection.Count ) ];
            AddRange( collection );
        }

        private IntArrayList( int[] items, int size )
        {
            _items = items;
            _size = size;
        }

        #region ICollection Members

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return null; }
        }

        public int Count
        {
            get { return _size; }
        }

        public void CopyTo( Array array, int arrayIndex )
        {
            Array.Copy( _items, 0, array, arrayIndex, _size );
        }

        private static int[] _emptyIntArray = new int[ 0 ];

        public int[] ToArray()
        {
            if( _size == 0 )
            {
                return _emptyIntArray;
            }
            int[] result = new int [_size];
            Array.Copy( _items, 0, result, 0, _size );
            return result;
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new IntArrayListEnumerator( this );
        }

        public IntArrayListEnumerator GetEnumerator()
        {
            return new IntArrayListEnumerator( this );
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            IntArrayList result;
            if( _size == 0 )
            {
                result = new IntArrayList();
            }
            else
            {
                result = new IntArrayList( _size );
                result._size = _size;
                Array.Copy( _items, 0, result._items, 0, _size );
            }
            result._version = _version;
            return result;
        }

        #endregion

        public void Add( int value )
        {
            if ( _size == _items.Length )
                EnsureCapacity( _size + 1 );
            _items [_size] = value;
            _size++;
            _version++;
        }

        public void Add( int value, int count )
        {
            EnsureCapacity( _size + count );
            while( --count >= 0 )
            {
                _items[ _size++ ] = value;
            }
            _version++;
        }

        public void AddRange( ICollection c )
        {
            int count = c.Count;
            EnsureCapacity( _size + count );
            c.CopyTo( _items, _size );
            _size += count;
            _version++;
        }

        public void Insert( int index, int value )
        {
            if(( index < 0 ) || ( index > _size ))
                throw new ArgumentOutOfRangeException( "index", index, 
                    "Insertion index is out of bounds: current size = " + _size.ToString() );

            if ( _size == _items.Length )
                EnsureCapacity( _size+1 );
            if ( index < _size )
                Array.Copy( _items, index, _items, index + 1, _size - index );
            _items [index] = value;
            _size++;
            _version++;
        }

        public void Remove( int value )
        {
            int index = IndexOf( value );
            if ( index >= 0 )
            {
                RemoveAt( index );
            }
        }

        public void RemoveRange( int index, int count )  
        {
            if ( index < 0 )
                throw new ArgumentOutOfRangeException( "index" );
            if ( count < 0 )
                throw new ArgumentOutOfRangeException( "count" );
            if ( _size - index < count )
                throw new ArgumentException( "_size - index < count" );
    
            if (count > 0) 
            {
                _size -= count;
                if (index < _size) 
                {
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }
                _version++;
            }
        }

        public int IndexOf( int value )
        {
            int size = _size;
            
            if( ( size & 1 ) == 0)
            {
                for( int i = 0; i < size; ++i )
                {
                    if( _items[ i ] == value ) return i;
                }
            }
            else
            {
                for( int i = size - 1; i >= 0; --i )
                {
                    if( _items[ i ] == value ) return i;
                }
            }
            return -1;
        }

        public void Reverse()
        {
            int last = _size - 1;
            int count = (last + 1) / 2;
            for ( int i = 0; i < count; i++ )
            {
                int temp = _items[i];
                _items[i] = _items[last - i];
                _items[last - i] = temp;
            }
        }

        public void RemoveAt( int index )
        {
            if ( index < 0 || index >= _size ) 
                throw new ArgumentOutOfRangeException( "index" );

            _size--;
            if (index < _size) 
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _version++;
        }

        public  int     Capacity
        {
            get { return _items.Length; }
            set
            {
                if( value <= 0 )
                    throw new ArgumentOutOfRangeException( "value" );
                int[] newData = new int [ AdjustCapacity( value ) ];
                _size = Math.Min( _size, newData.Length );
                Array.Copy( _items, 0, newData, 0, _size );
                _items = newData;
            }
        }

        public  void  Clear()
        {
            _items = new int[ InitialCapacity ];
            _size = 0;
        }

        public void SetSize( int size )
        {
            _size = size;
        }

        public int this[int index]
        {
            get 
            { 
                if (index < 0 || index >= _size) 
                {
                    throw new ArgumentOutOfRangeException( "index", index, 
                        "Specified argument was out of the range of valid values: list size=" + _size );
                }
                return _items [index];
            }
            set
            {
                if (index < 0 || index >= _size) 
                {
                    throw new ArgumentOutOfRangeException( "index", index, 
                        "Specified argument was out of the range of valid values: list size=" + _size );
                }
                _items [index] = value;
                _version++;
            }
        }

        public int Last
        {
            get
            {
                if( _size <= 0 )
                    throw new IndexOutOfRangeException( "Last element is not defined in the empty array" );
                return( _items[ _size - 1 ] );
            }
        }

        public int BinarySearch( int value )
        {
            int lo = 0;
            int hi = _size - 1;
            while (lo <= hi) 
            {
                int i = (lo + hi) >> 1;
                int c = _items[ i ] - value;
                if (c == 0) return i;
                if (c < 0) 
                {
                    lo = i + 1;
                }
                else 
                {
                    hi = i - 1;
                }
            }
            return ~lo;
        }

        public int BinarySearch( int value, IComparer comparer )
        {
            return Array.BinarySearch( _items, 0, _size, value, comparer );
        }

        public void Sort()
        {
            Debug.Assert( _size >= 0 && _size <= _items.Length );
            Array.Sort( _items, 0, _size );
            _version++;
        }

        public void RadixSort()
        {
            Sorts.RadixSort( _items, Count );
            _version++;
        }

        public void Sort( IComparer comparer )
        {
            Array.Sort( _items, 0, _size, comparer );
            _version++;
        }

        private void EnsureCapacity( int requestCapacity )
        {
            int capacity = Capacity;
            if( capacity < requestCapacity )
            {
                do 
                {
                    capacity = AdjustCapacity( ( ( capacity + 1 ) * 13 ) >> 3 ); // phi's rational approximation
                }
                while( capacity < requestCapacity );
                Capacity = capacity;
            }
        }

        private static int AdjustCapacity( int capacity )
        {
            return ( capacity < InitialCapacity ) ? InitialCapacity : capacity;
        }

        public void RemoveDuplicatesSorted()
        {
            if ( _size == 0 )
                return;
            
            int srcIndex  = 1;
            int destIndex = 0;
            while( srcIndex < _size )
            {
                if ( _items [srcIndex] != _items [destIndex] )
                {
                    destIndex++;
                    _items [destIndex] = _items [srcIndex];
                }
                srcIndex++;
            }
            _size = destIndex+1;
            Debug.Assert( _size >= 0 && _size <= _items.Length );
        }

        public static IntArrayList MergeSorted( IntArrayList list1, IntArrayList list2 )
        {
            int count1 = list1.Count;
            int count2 = list2.Count;
            if( count1 == 0 )
            {
                return list2;
            }
            if( count2 == 0 )
            {
                return list1;
            }
            int[] result = new int [ AdjustCapacity( count1 + count2) ];
            int index1 = 0;
            int index2 = 0;
            int destIndex = 0;
            while ( index1 < count1 && index2 < count2 )
            {
                int compareResult = list1._items [index1] - list2._items [index2];
                if ( compareResult == 0 )
                {
                    index1++;
                }
                else if ( compareResult < 0 )
                {
                    result [destIndex++] = list1._items [index1];
                    index1++;
                }
                else
                {
                    result [destIndex++] = list2._items [index2];
                    index2++;
                }
            }
            
            // only one of these for loops will have >0 steps
            for( int i=index1; i < count1; i++ )
            {
                result [destIndex++] = list1._items [i];
            }
            for( int i=index2; i < count2; i++ )
            {
                result [destIndex++] = list2._items [i];
            }

            return new IntArrayList( result, destIndex );
        }

        public static IntArrayList MergeSorted( IntArrayList list1, IntArrayList list2, IComparer comparer )
        {
            int count1 = list1.Count;
            int count2 = list2.Count;
            if( count1 == 0 )
            {
                return list2;
            }
            if( count2 == 0 )
            {
                return list1;
            }
            int[] result = new int [ AdjustCapacity( count1 + count2) ];
            int index1 = 0;
            int index2 = 0;
            int destIndex = 0;
            while ( index1 < count1 && index2 < count2 )
            {
                int compareResult = comparer.Compare( list1._items [index1], list2._items [index2] );
                if ( compareResult == 0 )
                {
                    index1++;
                }
                else if ( compareResult < 0 )
                {
                    result [destIndex++] = list1._items [index1];
                    index1++;
                }
                else
                {
                    result [destIndex++] = list2._items [index2];
                    index2++;
                }
            }
            
            // only one of these for loops will have >0 steps
            for( int i=index1; i < count1; i++ )
            {
                result [destIndex++] = list1._items [i];
            }
            for( int i=index2; i < count2; i++ )
            {
                result [destIndex++] = list2._items [i];
            }

            return new IntArrayList( result, destIndex );
        }

        public void IntersectSorted( IntArrayList list )
        {
            int count1 = _size;
            int count2 = list._size;
            int compareResult;
            int index1 = 0;
            int index2 = 0;
            int destIndex = 0;
            if( count1 > 0 && count2 > 0 )
            {
                compareResult = _items[ 0 ] - list[ 0 ];
                if( compareResult < 0 )
                {
                    compareResult = BinarySearch( list[0] );
                    if( compareResult >= 0 )
                    {
                        index1 = compareResult;
                    }
                    else
                    {
                        index1 = ~compareResult;
                    }
                }
                else if( compareResult > 0 )
                {
                    compareResult = list.BinarySearch( _items[0] );
                    if( compareResult >= 0 )
                    {
                        index2 = compareResult;
                    }
                    else
                    {
                        index2 = ~compareResult;
                    }
                }
                compareResult = _items[ count1 - 1 ] - list[ count2 - 1 ];
                if( compareResult < 0 )
                {
                    compareResult = Array.BinarySearch( list._items, index2, count2 - index2, _items[ count1 - 1 ] );
                    if( compareResult >= 0 )
                    {
                        count2 = compareResult + 1;
                    }
                    else
                    {
                        count2 = ~compareResult;
                    }
                }
                else if( compareResult > 0 )
                {
                    compareResult = Array.BinarySearch( _items, index1, count1 - index1, list[ count2 - 1 ] );
                    if( compareResult >= 0 )
                    {
                        count1 = compareResult + 1;
                    }
                    else
                    {
                        count1 = ~compareResult;
                    }
                }
                while ( index1 < count1 && index2 < count2 )
                {
                    compareResult = _items[ index1 ] - list._items[ index2 ];
                    if ( compareResult == 0 )
                    {
                        int value = _items[ index1++ ];
                        _items[ destIndex++ ] = value;
                        // skip duplicates
                        while( index1 < count1 && _items[ index1 ] == value )
                        {
                            index1++;
                        }
                        do
                        {
                            index2++;
                        }
                        while( index2 < count2 && list._items[ index2 ] == value );
                    }
                    else if ( compareResult < 0 )
                    {
                        index1++;
                    }
                    else
                    {
                        index2++;
                    }
                }
            }
            _size = destIndex;
            _version++;
        }

        /// <summary>
        /// Intersects the specified lists of integers. This method can return one of the lists passed to it
        /// if one of the lists is found to be empty.
        /// </summary>
        /// <param name="list1">The first list to intersect.</param>
        /// <param name="list2">The second list to intersect.</param>
        /// <returns>The intersection result.</returns>
        public static IntArrayList IntersectSorted( IntArrayList list1, IntArrayList list2 )
        {
            if( list1.Count == 0 )
            {
                return list1;
            }
            if( list2.Count == 0 )
            {
                return list2;
            }
            return IntersectSortedNew( list1, list2 );
        }

        /// <summary>
        /// Intersects the specified lists of integers. This method is guaranteed to return a new list instance
        /// and not to reuse any of the lists passed to it.
        /// </summary>
        /// <param name="list1">The first list to intersect.</param>
        /// <param name="list2">The second list to intersect.</param>
        /// <returns>The intersection result.</returns>
        public static IntArrayList IntersectSortedNew(IntArrayList list1, IntArrayList list2)
        {
            int count1 = list1.Count;
            int count2 = list2.Count;
            int index1 = 0;
            int index2 = 0;
            int[] result = new int [ AdjustCapacity( Math.Min( count1, count2 ) )] ;
            int destIndex = 0;
            if( count1 > 0 && count2 > 0 )
            {
                int compareResult = list1[ 0 ] - list2[ 0 ];
                if( compareResult < 0 )
                {
                    compareResult = list1.BinarySearch( list2[ 0 ] );
                    if( compareResult >= 0 )
                    {
                        index1 = compareResult;
                    }
                    else
                    {
                        index1 = ~compareResult;
                    }
                }
                else if( compareResult > 0 )
                {
                    compareResult = list2.BinarySearch( list1[ 0 ] );
                    if( compareResult >= 0 )
                    {
                        index2 = compareResult;
                    }
                    else
                    {
                        index2 = ~compareResult;
                    }
                }
                compareResult = list1[ count1 - 1 ] - list2[ count2 - 1 ];
                if( compareResult < 0 )
                {
                    compareResult = Array.BinarySearch( list2._items, index2, count2 - index2, list1[ count1 - 1 ] );
                    if( compareResult >= 0 )
                    {
                        count2 = compareResult + 1;
                    }
                    else
                    {
                        count2 = ~compareResult;
                    }
                }
                else if( compareResult > 0 )
                {
                    compareResult = Array.BinarySearch( list1._items, index1, count1 - index1, list2[ count2 - 1 ] );
                    if( compareResult >= 0 )
                    {
                        count1 = compareResult + 1;
                    }
                    else
                    {
                        count1 = ~compareResult;
                    }
                }
                while ( index1 < count1 && index2 < count2 )
                {
                    compareResult = list1._items[ index1 ] - list2._items[ index2 ];
                    if ( compareResult == 0 )
                    {
                        int value = list1._items [index1];
                        result [destIndex++] = value;
                        index1++;
                        // skip duplicates
                        while( index1 < count1 && list1._items [index1 ] == value )
                        {
                            index1++;
                        }
                        do
                        {
                            index2++;
                        }
                        while( index2 < count2 && list2._items [index2 ] == value );
                    }
                    else if ( compareResult < 0 )
                    {
                        index1++;
                    }
                    else
                    {
                        index2++;
                    }
                }
            }
            return new IntArrayList( result, destIndex );
        }

        public static IntArrayList IntersectSorted( IntArrayList list1, IntArrayList list2, IComparer comparer )
        {
            int count1 = list1.Count;
            int count2 = list2.Count;
            if( count1 == 0 )
            {
                return list1;
            }
            if( count2 == 0 )
            {
                return list2;
            }
            IntArrayList result = new IntArrayList( Math.Min( count1, count2 ) );
            int index1 = 0;
            int index2 = 0;
            while ( index1 < count1 && index2 < count2 )
            {
                int compareResult = comparer.Compare( list1 [index1], list2  [index2] );
                if ( compareResult == 0 )
                {
                    result.Add( list1 [index1] );
                    index1++;
                }
                else if ( compareResult < 0 )
                {
                    index1++;
                }
                else
                {
                    index2++;
                }
            }
            return result;
        }

        /// <summary>
        /// Unlike MergeSorted and IntersectSorted, MinusSorted is instance method because it
        /// implements the "-=" operation
        /// </summary>
        /// <param name="minus"></param>
        public void MinusSorted( IntArrayList minus )
        {
            int index0 = 0;
            int index1 = 0;
            int index2 = 0;
            int count1 = Count;
            int count2 = minus.Count;
            while( index1 < count1 && index2 < count2 )
            {
                _items[ index0 ] = _items[ index1 ];
                int compareResult = _items[ index1 ] - minus[ index2 ];
                if ( compareResult == 0 )
                {
                    index1++;
                }
                else if ( compareResult < 0 )
                {
                    index0++;
                    index1++;
                }
                else
                {
                    index2++;
                }
            }
            while( index1 < count1 )
            {
                _items[ index0++ ] = _items[ index1++ ];
            }
            _size = index0;
            _version++;
        }

        [Serializable()] public class IntArrayListEnumerator : IEnumerator, ICloneable
        {
            private IntArrayList _list;
            private int _index;
            private int _version;
						    
            internal IntArrayListEnumerator( IntArrayList list ) 
            {
                _list = list;
                _index = -1;
                _version = list._version;
            }

            public Object Clone() 
            {
                return MemberwiseClone();
            }
    
            public virtual bool MoveNext() 
            {
                if ( _version != _list._version ) 
                    throw new InvalidOperationException( "Collection was modified; enumeration operation may not execute." );
                if ( _index != -2 && _index < (_list.Count-1) ) 
                {
                    _index++;
                    return true;
                }
                else 
                    _index = -2;

                return false;
            }
    
            object IEnumerator.Current 
            {
                get 
                {
                    if ( _index == -1 )
                        throw new InvalidOperationException( "Enumeration has not started. Call MoveNext." );
                    else if ( _index == -2 )
                        throw new InvalidOperationException( "Enumeration already finished." );
                    return _list [_index];
                }
            }

            public int Current
            {
                get 
                {
                    if ( _index == -1 )
                        throw new InvalidOperationException( "Enumeration has not started. Call MoveNext." );
                    else if ( _index == -2 )
                        throw new InvalidOperationException( "Enumeration already finished." );
                    return _list [_index];
                }
            }
    
            public virtual void Reset() 
            {
                if (_version != _list._version) 
                    throw new InvalidOperationException( "Collection was modified; enumeration operation may not execute." );
                _index = -1;
            }
        }
    }


    /**
     * ArrayList with per-element Equals(), GetHashCode() and ToString() implementations.
     */

    public class ComparableArrayList: ArrayList
    {
        private const string _toStringSeparator = ",";

        public ComparableArrayList() : base() {}
        public ComparableArrayList( ICollection coll ): base(coll) {}
    	
        public override bool Equals( object obj )
        {
            if ( obj == null || !(obj is ComparableArrayList) )
                return false;

            if ( obj == this )
                return true;

            ComparableArrayList rhs = (ComparableArrayList) obj;
            if ( rhs.Count != Count )
                return false;

            for( int i=0; i<Count; i++ )
            {
                if ( !this [i].Equals( rhs [i] ) )
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for( int i=0; i<Count; i++ )
            {
                hash ^= this [i].GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            StringBuilder builder = StringBuilderPool.Alloc();
            try 
            {
                for( int i=0; i<Count; i++ )
                {
                    if ( i > 0 )
                    {
                        builder.Append( _toStringSeparator );
                    }
                    builder.Append( this [i].ToString() );
                }
                return builder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( builder );
            }
        }
    }
}
