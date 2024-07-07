// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;

namespace JetBrains.DataStructures
{
	public class HashtableParams
    {
        public const uint maxBucketsPerIndex = 2;

	    static HashtableParams()
	    {
            Array.Sort( TableSizes );
	    }

	    // table sizes should be prime & enough far from powers of 2
        public static readonly uint[] TableSizes =
        {
            5,11,23,47,97,197,397,797,1597,3203,6421,12853,25717,51437,102877,205759,
            411527,823117,1646237,3292489,6584983,13169977,26339969,52679969,105359939,
            210719881,421439783,842879579,1685759167,
            433,877,1759,3527,7057,14143,28289,56591,113189,226379,452759,905551,1811107,
            3622219,7244441,14488931,28977863,57955739,115911563,231823147,463646329,927292699,
            1854585413,
            953,1907,3821,7643,15287,30577,61169,122347,244703,489407,978821,1957651,3915341,
            7830701,15661423,31322867,62645741,125291483,250582987,501165979,1002331963,
            2004663929,
            1039,2081,4177,8363,16729,33461,66923,133853,267713,535481,1070981,2141977,4283963,
            8567929,17135863,34271747,68543509,137087021,274174111,548348231,1096696463,
            31,67,137,277,557,1117,2237,4481,8963,17929,35863,71741,143483,286973,573953,
            1147921,2295859,4591721,9183457,18366923,36733847,73467739,146935499,293871013,
            587742049,1175484103,
            599,1201,2411,4831,9677,19373,38747,77509,155027,310081,620171,1240361,2480729,
            4961459,9922933,19845871,39691759,79383533,158767069,317534141,635068283,1270136683,
            311,631,1277,2557,5119,10243,20507,41017,82037,164089,328213,656429,1312867,
            2625761,5251529,10503061,21006137,42012281,84024581,168049163,336098327,672196673,
            1344393353,
            3,7,17,37,79,163,331,673,1361,2729,5471,10949,21911,43853,87719,175447,350899,
            701819,1403641,2807303,5614657,11229331,22458671,44917381,89834777,179669557,
            359339171,718678369,1437356741,
            43,89,179,359,719,1439,2879,5779,11579,23159,46327,92657,185323,370661,741337,
            1482707,2965421,5930887,11861791,23723597,47447201,94894427,189788857,379577741,
            759155483,1518310967,
            379,761,1523,3049,6101,12203,24407,48817,97649,195311,390647,781301,1562611,
            3125257,6250537,12501169,25002389,50004791,100009607,200019221,400038451,800076929,
            1600153859
        };

        public static uint AdjustHashtableSize( uint desiredSize )
        {
            int lo = 0;
            int hi = TableSizes.Length - 1;
            while( lo <= hi )
            {
                int i = ( lo + hi ) >> 1;
                int c = (int)TableSizes[ i ] - (int)desiredSize;
                if( c == 0 )
                {
                    return TableSizes[ i ];
                }
                if( c < 0 )
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }
            return TableSizes[ lo ];
        }
    }

	/// <summary>
	/// Map of hashable objects to arbitrary objects.
	/// </summary>
    public class HashMap : IEnumerable
    {
        public HashMap() : this( 0 ) {}

        public HashMap( int initialSize )
        {
            _count = 0;
            ReHash( _initialSize = HashtableParams.AdjustHashtableSize( (uint) initialSize ) );
        }

        public int Count
        {
            get { return (int) _count; }
        }

        public bool Contains( object key )
        {
            if( _count == 0 ) return false;
            uint tableIndex, prevIndex;
            return SearchCollisions( key, out tableIndex, out prevIndex ) != 0;
        }

        public object this [ object key ]
        {
            get
            {
                if( _count == 0 ) return null;
                uint tableIndex, prevIndex;
                uint index = SearchCollisions( key, out tableIndex, out prevIndex );
                return( index == 0 ) ? null : _buckets[ index ].value;
            }
            set
            {
                Add( key, value );
            }
        }

        public virtual void Add( object key, object value )
        {
            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            if( index != 0 )
            {
                _buckets[ index ].value = value;
            }
            else
            {
                uint i = _firstFree;
                if( i != 0 )
                {
                    _firstFree = _buckets[ i ].next;
                }
                else
                {
                    i = _count + 1;
                    if( i == _buckets.Length )
                    {
                        ReHash( ( ( _size * 13 ) - 7 ) >> 3 );
                        tableIndex = ( (uint) key.GetHashCode() ) % _size;
                    }
                }
                _buckets[ i ].key = key;
                _buckets[ i ].value = value;
                _buckets[ i ].next = _hashTable[ tableIndex ];
                _hashTable[ tableIndex ] = i;
                ++_count;
#if DEBUG
                ++_version;
#endif
            }
        }
        public void Remove( object key )
        {
            if( _count == 0 ) return;
            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            if( index != 0 )
            {
                if( prevIndex != 0 )
                {
                    _buckets[ prevIndex ].next = _buckets[ index ].next;
                }
                else
                {
                    _hashTable[ tableIndex ] = _buckets[ index ].next;
                }
                _buckets[ index ].key = null;
                _buckets[ index ].value = null;
                _buckets[ index ].next = _firstFree;
                _firstFree = index;
                if( --_count == 0 )
                {
                    Clear();
                }
#if DEBUG
                ++_version;
#endif
            }
        }

        public void Clear()
        {
            if( _initialSize != _size )
            {
                _count = 0;
                ReHash( 0 );
            }
            else
            {
                if( _count > 0 )
                {
                    _count = 0;
                    _firstFree = 0;
                    Array.Clear( _hashTable, 0, _hashTable.Length );
                    Array.Clear( _buckets, 0, _buckets.Length );
                }
            }
        }

        public Entry GetEntry( object key )
        {
            if ( key == null )
            {
                throw new ArgumentNullException( "key" );
            }
            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            return ( index == 0 ) ? null : new Entry( _buckets, index );
        }

		/// <summary>
		/// Proxy object around a bucket representing key-value pair.
		/// </summary>
        public class Entry
        {
            internal Entry( bucket[] buckets, uint index )
            {
                _buckets = buckets;
                _index = index;
            }
            public object Key
            {
                get { return _buckets[ _index ].key; }
                set { _buckets[ _index ].key = value; }
            }
            public object Value
            {
                get { return _buckets[ _index ].value; }
                set { _buckets[ _index ].value = value; }
            }
            internal uint       _index;
            internal bucket[]   _buckets;
        }

        #region IEnumerable Members

        protected internal class HashMapEnumerator : IEnumerator
        {
            public HashMapEnumerator( HashMap map )
            {
                _entry = new Entry( map._buckets, 0 );
                _count = map._count;
                _theMap = map;
                Reset();
            }

            public object Current
            {
                get
                {
#if DEBUG
                    if( _counted == 0 )
                    {
                        throw new InvalidOperationException(
                            "Enumerator.Current called without calling MoveNext()." );
                    }
                    if( _savedVersion != _theMap._version )
                    {
                        throw new InvalidOperationException(
                            "Collection modified while enumeration." );
                    }
#endif
                    return _entry;
                }
            }
            public virtual bool MoveNext()
            {
                if( _counted == _count )
                {
                    return false;
                }
                while( _entry._buckets[ ++_entry._index ].key == null );
                ++_counted;
                return true;
            }
            public void Reset()
            {
                _entry._index = _counted = 0;
#if DEBUG
                _savedVersion = _theMap._version;
#endif
            }

            private uint    _count;
            private uint    _counted;
            private Entry   _entry;
            private HashMap _theMap;
#if DEBUG
            private int     _savedVersion;
#endif
        }

        public IEnumerator GetEnumerator()
        {
            return new HashMapEnumerator( this );
        }

        #endregion

        #region implementation details

        protected internal void ReHash( uint desiredSize )
        {
            if( desiredSize < _initialSize )
            {
                desiredSize = _initialSize;
            }
            uint size = HashtableParams.AdjustHashtableSize( desiredSize );
            if( size != _size )
            {
                _firstFree = 0;
                bucket[] oldBuckets = _buckets;
                _hashTable = new uint[ size ];
                _buckets = new bucket[ size * HashtableParams.maxBucketsPerIndex ];
                if( _count > 0 )
                {
                    for( uint i = 1, j = 0; i < oldBuckets.Length; ++i )
                    {
                        object key = oldBuckets[ i ].key;
                        if( key != null )
                        {
                            ++j;
                            _buckets[ j ].key = key;
                            _buckets[ j ].value = oldBuckets[ i ].value;
                            uint hashValue = ( (uint) key.GetHashCode() ) % size;
                            _buckets[ j ].next = _hashTable[ hashValue ];
                            _hashTable[ hashValue ] = j;
                        }
                    }
                }
#if DEBUG
                ++_version;
#endif
                _size = size;
            }
        }

		/// <summary>
		/// Returns index of bucket where key is found or zero if not found.
		/// </summary>
        protected internal uint SearchCollisions( object key, out uint tableIndex, out uint prevIndex )
        {
            prevIndex = 0;
            uint bucketIndex = _hashTable[ tableIndex = ( (uint) key.GetHashCode() ) % _size ];
            if( bucketIndex > 0 && !key.Equals( _buckets[ bucketIndex ].key ) )
            {
#if DEBUG
                int savedVersion = _version;
#endif
                _buckets[ 0 ].key = key;
                do
                {
#if DEBUG
                    if( savedVersion != _version || !key.Equals( _buckets[ 0 ].key ) )
                    {
                        throw new InvalidOperationException( "Non-serialized usage detected!" );
                    }
#endif
                    bucketIndex = _buckets[ ( prevIndex = bucketIndex ) ].next;
                }
                while( !key.Equals( _buckets[ bucketIndex ].key ) );
                _buckets[ 0 ].key = null;
            }
            return bucketIndex;
        }

        protected internal struct bucket
        {
            public object	key;
            public object	value;
            public uint     next;
        }

        protected internal uint     _count;
        protected internal uint     _size;
        protected internal uint     _initialSize;
        protected internal uint     _firstFree;
        protected internal uint[]   _hashTable;
        protected internal bucket[] _buckets;
#if DEBUG
        protected internal int      _version;
#endif
        #endregion
    }

	/// <summary>
	/// Set of hashable objects
	/// In contrast to HashMap, in keys aren't mapped to values
	/// Signatures of some public methods differ from the HashMap's ones
	/// Indexer is not supported
	/// </summary>
    public class HashSet : IEnumerable
    {
        public HashSet() : this( 0 ) {}

		public HashSet( int initialSize )
        {
            _count = 0;
		    ReHash( _initialSize = HashtableParams.AdjustHashtableSize( (uint) initialSize ) );
        }

        public HashSet( HashSet other ) : this( 0 )
        {
            foreach( HashSet.Entry e in other )
            {
                Add( e.Key );
            }
        }

        public HashSet( ICollection collection ) : this( 0 )
        {
            foreach( object item in collection )
            {
                Add( item );
            }
        }

        public int Count
        {
            get { return (int) _count; }
        }

        public bool Contains( object key )
        {
            if( _count == 0 ) return false;

            if ( key == null )
                throw new ArgumentNullException( "key" );

            uint tableIndex, prevIndex;
            return SearchCollisions( key, out tableIndex, out prevIndex ) != 0;
        }

        public virtual void Add( object key )
        {
            if ( key == null )
                throw new ArgumentNullException( "key" );

            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            if( index == 0 )
            {
                uint i = _firstFree;
                if( i != 0 )
                {
                    _firstFree = _buckets[ i ].next;
                }
                else
                {
                    i = _count + 1;
                    if( i == _buckets.Length )
                    {
                        ReHash( ( ( _size * 13 ) - 7 ) >> 3 );
                        tableIndex = ( (uint) key.GetHashCode() ) % _size;
                    }
                }
                _buckets[ i ].key = key;
                _buckets[ i ].next = _hashTable[ tableIndex ];
                _hashTable[ tableIndex ] = i;
                ++_count;
#if DEBUG
                ++_version;
#endif
            }
        }
        public void Remove( object key )
        {
            if ( key == null )
                throw new ArgumentNullException( "key" );

            if( _count == 0 ) return;

            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            if( index != 0 )
            {
                if( prevIndex != 0 )
                {
                    _buckets[ prevIndex ].next = _buckets[ index ].next;
                }
                else
                {
                    _hashTable[ tableIndex ] = _buckets[ index ].next;
                }
                _buckets[ index ].key = null;
                _buckets[ index ].next = _firstFree;
                _firstFree = index;
                if( --_count == 0 )
                {
                    Clear();
                }
#if DEBUG
                ++_version;
#endif
            }
        }

        public object GetKey( object key )
        {
            if ( key == null )
                throw new ArgumentNullException( "key" );

            if( _count == 0 ) return null;

            uint tableIndex, prevIndex;
            uint bucketIndex = SearchCollisions( key, out tableIndex, out prevIndex );
            return ( bucketIndex == 0 ) ? null : _buckets[ bucketIndex ].key;
        }

        public void Clear()
        {
            if( _initialSize != _size )
            {
                _count = 0;
                ReHash( 0 );
            }
            else
            {
                if( _count > 0 )
                {
                    _count = 0;
                    _firstFree = 0;
                    Array.Clear( _hashTable, 0, _hashTable.Length );
                    Array.Clear( _buckets, 0, _buckets.Length );
                }
            }
        }

		/// <summary>
		/// Proxy object around a bucket representing key-value pair.
		/// </summary>
        public class Entry
        {
            internal Entry( bucket[] buckets, uint index )
            {
                _buckets = buckets;
                _index = index;
            }
            public object Key
            {
                get { return _buckets[ _index ].key; }
                set { _buckets[ _index ].key = value; }
            }
            internal uint       _index;
            internal bucket[]   _buckets;
        }

        #region IEnumerable Members

        protected internal class HashSetEnumerator : IEnumerator
        {
            public HashSetEnumerator( HashSet set )
            {
                _entry = new Entry( set._buckets, 0 );
                _count = set._count;
                _theSet = set;
                Reset();
            }

            public object Current
            {
                get
                {
#if DEBUG
                    if( _counted == 0 )
                    {
                        throw new InvalidOperationException(
                            "Enumerator.Current called without calling MoveNext()." );
                    }
                    if( _savedVersion != _theSet._version )
                    {
                        throw new InvalidOperationException(
                            "Collection modified while enumeration." );
                    }
#endif
                    return _entry;
                }
            }
            public virtual bool MoveNext()
            {
                if( _counted == _count )
                {
                    return false;
                }
                while( _entry._buckets[ ++_entry._index ].key == null );
                ++_counted;
                return true;
            }
            public void Reset()
            {
                _entry._index = _counted = 0;
#if DEBUG
                _savedVersion = _theSet._version;
#endif
            }

            private uint    _count;
            private uint    _counted;
            private Entry   _entry;
            private HashSet _theSet;
#if DEBUG
            private int     _savedVersion;
#endif
        }

        public IEnumerator GetEnumerator()
        {
            return new HashSetEnumerator( this );
        }

        #endregion

        #region implementation details

        protected internal void ReHash( uint desiredSize )
        {
            if( desiredSize < _initialSize )
            {
                desiredSize = _initialSize;
            }
            uint size = HashtableParams.AdjustHashtableSize( desiredSize );
            if( size != _size )
            {
                _firstFree = 0;
                bucket[] oldBuckets = _buckets;
                _hashTable = new uint[ size ];
                _buckets = new bucket[ size * HashtableParams.maxBucketsPerIndex ];
                if( _count > 0 )
                {
                    for( uint i = 1, j = 0; i < oldBuckets.Length; ++i )
                    {
                        object key = oldBuckets[ i ].key;
                        if( key != null )
                        {
                            ++j;
                            _buckets[ j ].key = key;
                            uint hashValue = ( (uint) key.GetHashCode() ) % size;
                            _buckets[ j ].next = _hashTable[ hashValue ];
                            _hashTable[ hashValue ] = j;
                        }
                    }
                }
#if DEBUG
                ++_version;
#endif
                _size = size;
            }
        }

		/// <summary>
		/// Returns index of bucket where key is found or zero if not found.
		/// </summary>
        protected internal uint SearchCollisions( object key, out uint tableIndex, out uint prevIndex )
        {
            prevIndex = 0;
            uint bucketIndex = _hashTable[ tableIndex = ( (uint) key.GetHashCode() ) % _size ];
            if( bucketIndex > 0 && !key.Equals( _buckets[ bucketIndex ].key ) )
            {
#if DEBUG
                int savedVersion = _version;
#endif
                _buckets[ 0 ].key = key;
                do
                {
#if DEBUG
                    if( savedVersion != _version || !key.Equals( _buckets[ 0 ].key ) )
                    {
                        throw new InvalidOperationException( "Non-serialized usage detected!" );
                    }
#endif
                    bucketIndex = _buckets[ ( prevIndex = bucketIndex ) ].next;
                }
                while( !key.Equals( _buckets[ bucketIndex ].key ) );
                _buckets[ 0 ].key = null;
            }
            return bucketIndex;
        }

        protected internal struct bucket
        {
            public object	key;
            public uint     next;
        }

        protected internal uint     _count;
        protected internal uint     _size;
        protected internal uint     _initialSize;
        protected internal uint     _firstFree;
        protected internal uint[]   _hashTable;
        protected internal bucket[] _buckets;
#if DEBUG
        protected internal int      _version;
#endif
        #endregion
    }

	/// <summary>
	/// IntHashSet is similar to HashSet.
	/// Never add Int32.MaxValue to a set!!!
	/// </summary>
    public class IntHashSet : IEnumerable
    {
        public IntHashSet() : this( 0 ) {}

        public IntHashSet( int initialSize )
        {
            _count = 0;
            ReHash( _initialSize = HashtableParams.AdjustHashtableSize( (uint) initialSize ) );
        }

        public int Count
        {
            get { return (int) _count; }
        }

        public bool Contains( int key )
        {
            if( _count == 0 ) return false;
            uint tableIndex, prevIndex;
            return SearchCollisions( key, out tableIndex, out prevIndex ) != 0;
        }

        public virtual void Add( int key )
        {
            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            if( index == 0 )
            {
                uint i = _firstFree;
                if( i != 0 )
                {
                    _firstFree = _buckets[ i ].next;
                }
                else
                {
                    i = _count + 1;
                    if( i == _buckets.Length )
                    {
                        ReHash( ( ( _size * 13 ) - 7 ) >> 3 );
                        tableIndex = ( (uint) key.GetHashCode() ) % _size;
                    }
                }
                _buckets[ i ].key = key;
                _buckets[ i ].next = _hashTable[ tableIndex ];
                _hashTable[ tableIndex ] = i;
                ++_count;
#if DEBUG
                ++_version;
#endif
            }
        }
        public void Remove( int key )
        {
            if( _count == 0 ) return;
            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            if( index != 0 )
            {
                if( prevIndex != 0 )
                {
                    _buckets[ prevIndex ].next = _buckets[ index ].next;
                }
                else
                {
                    _hashTable[ tableIndex ] = _buckets[ index ].next;
                }
                _buckets[ index ].key = Int32.MaxValue;
                _buckets[ index ].next = _firstFree;
                _firstFree = index;
                if( --_count == 0 )
                {
                    Clear();
                }
#if DEBUG
                ++_version;
#endif
            }
        }

        public void Clear()
        {
            if( _initialSize != _size )
            {
                _count = 0;
                ReHash( 0 );
            }
            else
            {
                if( _count > 0 )
                {
                    _count = 0;
                    _firstFree = 0;
                    Array.Clear( _hashTable, 0, _hashTable.Length );
                    Array.Clear( _buckets, 0, _buckets.Length );
                }
            }
        }

		/// <summary>
		/// Proxy object around a bucket representing key-value pair.
		/// </summary>
        public class Entry
        {
            internal Entry( bucket[] buckets, uint index )
            {
                _buckets = buckets;
                _index = index;
            }
            public int Key
            {
                get { return _buckets[ _index ].key; }
                set { _buckets[ _index ].key = value; }
            }
            internal uint       _index;
            internal bucket[]   _buckets;
        }

		/// <summary>
		/// Compares the bucket indices for the two given keys.
		/// For a hash-set that sustained only add operations without the remove ones, this comparer corresponds to the order in which the values were added to the hash set.
		/// </summary>
		public int BucketComparer(int key1, int key2)
		{
			uint dummy1, dummy2;
			uint value1 = SearchCollisions(key1, out dummy1, out dummy2);
			uint value2 = SearchCollisions(key2, out dummy1, out dummy2);
			return (int)value1 - (int)value2;
		}

        #region IEnumerable Members

        protected internal class IntHashSetEnumerator : IEnumerator
        {
            public IntHashSetEnumerator( IntHashSet set )
            {
                _entry = new Entry( set._buckets, 0 );
                _count = set._count;
                _theSet = set;
                Reset();
            }

            public object Current
            {
                get
                {
#if DEBUG
                    if( _counted == 0 )
                    {
                        throw new InvalidOperationException(
                            "Enumerator.Current called without calling MoveNext()." );
                    }
                    if( _savedVersion != _theSet._version )
                    {
                        throw new InvalidOperationException(
                            "Collection modified while enumeration." );
                    }
#endif
                    return _entry;
                }
            }
            public virtual bool MoveNext()
            {
                if( _counted == _count )
                {
                    return false;
                }
                while( _entry._buckets[ ++_entry._index ].key == Int32.MaxValue );
                ++_counted;
                return true;
            }
            public void Reset()
            {
                _entry._index = _counted = 0;
#if DEBUG
                _savedVersion = _theSet._version;
#endif
            }

            private uint        _count;
            private uint        _counted;
            private Entry       _entry;
            private IntHashSet  _theSet;
#if DEBUG
            private int     _savedVersion;
#endif
        }

        public IEnumerator GetEnumerator()
        {
            return new IntHashSetEnumerator( this );
        }

        #endregion

        #region implementation details

        protected internal void ReHash( uint desiredSize )
        {
            if( desiredSize < _initialSize )
            {
                desiredSize = _initialSize;
            }
            uint size = HashtableParams.AdjustHashtableSize( desiredSize );
            if( size != _size )
            {
                _firstFree = 0;
                bucket[] oldBuckets = _buckets;
                _hashTable = new uint[ size ];
                _buckets = new bucket[ size * HashtableParams.maxBucketsPerIndex ];
                if( _count > 0 )
                {
                    for( uint i = 1, j = 0; i < oldBuckets.Length; ++i )
                    {
                        int key = oldBuckets[ i ].key;
                        if( key != Int32.MaxValue )
                        {
                            ++j;
                            _buckets[ j ].key = key;
                            uint hashValue = ( (uint) key ) % size;
                            _buckets[ j ].next = _hashTable[ hashValue ];
                            _hashTable[ hashValue ] = j;
                        }
                    }
                }
#if DEBUG
                ++_version;
#endif
                _size = size;
            }
        }

		/// <summary>
		/// Returns index of bucket where key is found or zero if not found.
		/// </summary>
        protected internal uint SearchCollisions( int key, out uint tableIndex, out uint prevIndex )
        {
            prevIndex = 0;
            uint bucketIndex = _hashTable[ tableIndex = ( (uint) key ) % _size ];
            if( bucketIndex > 0 && key != _buckets[ bucketIndex ].key )
            {
#if DEBUG
                int savedVersion = _version;
#endif
                _buckets[ 0 ].key = key;
                do
                {
#if DEBUG
                    if( savedVersion != _version || key != _buckets[ 0 ].key )
                    {
                        throw new InvalidOperationException( "Non-serialized usage detected!" );
                    }
#endif
                    bucketIndex = _buckets[ ( prevIndex = bucketIndex ) ].next;
                }
                while( key != _buckets[ bucketIndex ].key );
            }
            return bucketIndex;
        }

        protected internal struct bucket
        {
            public int  key;
            public uint next;
        }

        protected internal uint     _count;
        protected internal uint     _size;
        protected internal uint     _initialSize;
        protected internal uint     _firstFree;
        protected internal uint[]   _hashTable;
        protected internal bucket[] _buckets;
#if DEBUG
        protected internal int      _version;
#endif
        #endregion
    }

	/// <summary>
	/// IntHashTable implements non-synchronized hashtable of pairs: int(key), object.
	/// !!! Do not use Int32.MaxValue as a value for key.
	/// </summary>
    public class IntHashTable : IEnumerable
    {
        public IntHashTable() : this( 0 ) {}

        public IntHashTable( int initialSize )
        {
            _count = 0;
            ReHash( _initialSize = HashtableParams.AdjustHashtableSize( (uint) initialSize ) );
        }

        public int Count
        {
            get { return (int) _count; }
        }

        public bool Contains( int key )
        {
            if( _count == 0 ) return false;
            uint tableIndex, prevIndex;
            return SearchCollisions( key, out tableIndex, out prevIndex ) != 0;
        }

        public bool ContainsKey( int key )
        {
            uint tableIndex, prevIndex;
            return SearchCollisions( key, out tableIndex, out prevIndex ) != 0;
        }

        public virtual object this [ int key ]
        {
            get
            {
                if( _count == 0 ) return null;
                uint tableIndex, prevIndex;
                uint index = SearchCollisions( key, out tableIndex, out prevIndex );
                return( index == 0 ) ? null : _buckets[ index ].value;
            }
            set
            {
                Add( key, value );
            }
        }

        public virtual void Add( int key, object value )
        {
            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            if( index != 0 )
            {
                _buckets[ index ].value = value;
            }
            else
            {
                uint i = _firstFree;
                if( i != 0 )
                {
                    _firstFree = _buckets[ i ].next;
                }
                else
                {
                    i = _count + 1;
                    if( i == _buckets.Length )
                    {
                        ReHash( (uint) ( ( ( _hashTable.Length * 13 ) - 7 ) >> 3 ) );
                        tableIndex = ( (uint) key ) % ( (uint) _hashTable.Length );
                        i = _count + 1;
                    }
                }
                _buckets[ i ].key = key;
                _buckets[ i ].value = value;
                _buckets[ i ].next = _hashTable[ tableIndex ];
                _hashTable[ tableIndex ] = i;
                ++_count;
#if DEBUG
                ++_version;
#endif
            }
        }
        public void Remove( int key )
        {
            if( _count == 0 ) return;
            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            if( index != 0 )
            {
                if( prevIndex != 0 )
                {
                    _buckets[ prevIndex ].next = _buckets[ index ].next;
                }
                else
                {
                    _hashTable[ tableIndex ] = _buckets[ index ].next;
                }
                _buckets[ index ].key = Int32.MaxValue;
                _buckets[ index ].value = null;
                _buckets[ index ].next = _firstFree;
                _firstFree = index;
                if( --_count == 0 )
                {
                    Clear();
                }
#if DEBUG
                ++_version;
#endif
            }
        }

        public void Clear()
        {
            if( _initialSize != _hashTable.Length )
            {
                _count = 0;
                ReHash( 0 );
            }
            else
            {
                if( _count > 0 )
                {
                    _count = 0;
                    _firstFree = 0;
                    Array.Clear( _hashTable, 0, _hashTable.Length );
                    Array.Clear( _buckets, 0, _buckets.Length );
                }
            }
        }

        public Entry GetEntry( int key )
        {
            if( _count == 0 ) return null;
            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            return ( index == 0 ) ? null : new Entry( _buckets, index );
        }

		/// <summary>
		/// Proxy object around a bucket representing key-value pair.
		/// </summary>
        public class Entry
        {
            internal Entry( bucket[] buckets, uint index )
            {
                _buckets = buckets;
                _index = index;
            }
            public int Key
            {
                get { return _buckets[ _index ].key; }
                set { _buckets[ _index ].key = value; }
            }
            public object Value
            {
                [DebuggerStepThrough] get { return _buckets[ _index ].value; }
                set { _buckets[ _index ].value = value; }
            }
            internal uint       _index;
            internal bucket[]   _buckets;
        }

        #region IEnumerable Members

        protected internal class IntHashTableEnumerator : IEnumerator
        {
            public IntHashTableEnumerator( IntHashTable table )
            {
                _entry = new Entry( table._buckets, 0 );
                _count = table._count;
                _table = table;
                Reset();
            }

            public object Current
            {
                get
                {
#if DEBUG
                    if( _counted == 0 )
                    {
                        throw new InvalidOperationException(
                            "Enumerator.Current called without calling MoveNext()." );
                    }
                    if( _savedVersion != _table._version )
                    {
                        throw new InvalidOperationException(
                            "Collection modified while enumeration." );
                    }
#endif
                    return _entry;
                }
            }
            public virtual bool MoveNext()
            {
                if( _counted == _count )
                {
                    return false;
                }
                while( _entry._buckets[ ++_entry._index ].key == Int32.MaxValue );
                ++_counted;
                return true;
            }
            public void Reset()
            {
                _entry._index = _counted = 0;
#if DEBUG
                _savedVersion = _table._version;
#endif
            }

            private uint            _count;
            private uint            _counted;
            private Entry           _entry;
            protected IntHashTable  _table;
#if DEBUG
            private int             _savedVersion;
#endif
        }

        public virtual IEnumerator GetEnumerator()
        {
            return new IntHashTableEnumerator( this );
        }

        /// <summary>
        /// Returns the size of the memory occupied by the hashtable structures. Does not
        /// include the memory occupied by the objects stored in the hashtable.
        /// </summary>
        /// <returns>The size of the hashtable structures in bytes.</returns>
        public virtual int EstimateMemorySize()
        {
            return 8 + 12 + 4 * _hashTable.Length + 12 * _buckets.Length;
        }

        #endregion

        #region implementation details

        protected internal virtual void ReHash( uint desiredSize )
        {
            if( desiredSize < _initialSize )
            {
                desiredSize = _initialSize;
            }
            uint size = HashtableParams.AdjustHashtableSize( desiredSize );
            if( _hashTable == null || size != _hashTable.Length )
            {
                _firstFree = 0;
                bucket[] oldBuckets = _buckets;
                _hashTable = new uint[ size ];
                _buckets = new bucket[ size * HashtableParams.maxBucketsPerIndex ];
                if( _count > 0 )
                {
                    for( uint i = 1, j = 0; i < oldBuckets.Length; ++i )
                    {
                        int key = oldBuckets[ i ].key;
                        if( key != Int32.MaxValue )
                        {
                            ++j;
                            _buckets[ j ].key = key;
                            _buckets[ j ].value = oldBuckets[ i ].value;
                            uint hashValue = ( (uint) key ) % size;
                            _buckets[ j ].next = _hashTable[ hashValue ];
                            _hashTable[ hashValue ] = j;
                        }
                    }
                }
#if DEBUG
                ++_version;
#endif
            }
        }

		/// <summary>
		/// Returns index of bucket where key is found or zero if not found.
		/// </summary>
        protected internal uint SearchCollisions( int key, out uint tableIndex, out uint prevIndex )
        {
            prevIndex = 0;
            uint bucketIndex = _hashTable[ tableIndex = ( (uint) key ) % ( (uint) _hashTable.Length ) ];
            if( bucketIndex > 0 && key != _buckets[ bucketIndex ].key )
            {
#if DEBUG
                int savedVersion = _version;
#endif
                _buckets[ 0 ].key = key;
                do
                {
#if DEBUG
                    if( savedVersion != _version || key != _buckets[ 0 ].key )
                    {
                        throw new InvalidOperationException( "Non-serialized usage detected!" );
                    }
#endif
                    bucketIndex = _buckets[ ( prevIndex = bucketIndex ) ].next;
                }
                while( key != _buckets[ bucketIndex ].key );
            }
            return bucketIndex;
        }

        protected internal struct bucket
        {
            public int      key;
            public object	value;
            public uint     next;
        }

        protected internal uint     _count;
        //protected internal uint     _size;
        protected internal uint     _initialSize;
        protected internal uint     _firstFree;
        protected internal uint[]   _hashTable;
        protected internal bucket[] _buckets;
#if DEBUG
        protected internal int      _version;
#endif
        #endregion
    }

	/// <summary>
	/// IntHashTableOfInt implements non-synchronized hashtable of pairs: int(key), int.
	/// </summary>
    public class IntHashTableOfInt : IEnumerable
    {
        public IntHashTableOfInt() : this( 0 ) {}

        public IntHashTableOfInt( int initialSize )
        {
            _count = 0;
            ReHash( _initialSize = HashtableParams.AdjustHashtableSize( (uint) initialSize ) );
        }

		/// <summary>
		/// The value which is returned if the key is not found in the hash.
		/// </summary>
        public int MissingKeyValue
        {
            get { return _missingKeyValue; }
            set { _missingKeyValue = value; }
        }

        public int Count
        {
            get { return (int) _count; }
        }

        public bool Contains( int key )
        {
            if( _count == 0 ) return false;
            uint tableIndex, prevIndex;
            return SearchCollisions( key, out tableIndex, out prevIndex ) != 0;
        }

        public bool ContainsKey( int key )
        {
            return Contains( key );
        }

        public virtual int this [ int key ]
        {
            get
            {
                if( _count == 0 ) return _missingKeyValue;
                uint tableIndex, prevIndex;
                uint index = SearchCollisions( key, out tableIndex, out prevIndex );
                return( index == 0 ) ? _missingKeyValue : _buckets[ index ].value;
            }
            set
            {
                Add( key, value );
            }
        }

        public virtual void Add( int key, int value )
        {
            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            if( index != 0 )
            {
                _buckets[ index ].value = value;
            }
            else
            {
                uint i = _firstFree;
                if( i != 0 )
                {
                    _firstFree = _buckets[ i ].next;
                }
                else
                {
                    i = _count + 1;
                    if( i == _buckets.Length )
                    {
                        ReHash( ( ( _size * 13 ) - 7 ) >> 3 );
                        tableIndex = ( (uint) key.GetHashCode() ) % _size;
                    }
                }
                _buckets[ i ].key = key;
                _buckets[ i ].value = value;
                _buckets[ i ].next = _hashTable[ tableIndex ];
                _hashTable[ tableIndex ] = i;
                ++_count;
#if DEBUG
                ++_version;
#endif
            }
        }

        public void Remove( int key )
        {
            if( _count == 0 ) return;
            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            if( index != 0 )
            {
                if( prevIndex != 0 )
                {
                    _buckets[ prevIndex ].next = _buckets[ index ].next;
                }
                else
                {
                    _hashTable[ tableIndex ] = _buckets[ index ].next;
                }
                _buckets[ index ].key = _missingKeyValue;
                _buckets[ index ].next = _firstFree;
                _firstFree = index;
                if( --_count == 0 )
                {
                    Clear();
                }
#if DEBUG
                ++_version;
#endif
            }
        }

        public void Clear()
        {
            if( _initialSize != _size )
            {
                _count = 0;
                ReHash( 0 );
            }
            else
            {
                if( _count > 0 )
                {
                    _count = 0;
                    _firstFree = 0;
                    Array.Clear( _hashTable, 0, _hashTable.Length );
                    Array.Clear( _buckets, 0, _buckets.Length );
                }
            }
        }

        public Entry GetEntry( int key )
        {
            uint tableIndex, prevIndex;
            uint index = SearchCollisions( key, out tableIndex, out prevIndex );
            return ( index == 0 ) ? null : new Entry( _buckets, index );
        }

		/// <summary>
		/// Proxy object around a bucket representing key-value pair.
		/// </summary>
        public class Entry
        {
            internal Entry( bucket[] buckets, uint index )
            {
                _buckets = buckets;
                _index = index;
            }
            public int Key
            {
                get { return _buckets[ _index ].key; }
                set { _buckets[ _index ].key = value; }
            }
            public int Value
            {
                get { return _buckets[ _index ].value; }
                set { _buckets[ _index ].value = value; }
            }
            internal uint       _index;
            internal bucket[]   _buckets;
        }

        #region IEnumerable Members

        protected internal class IntHashTableOfIntEnumerator : IEnumerator
        {
            public IntHashTableOfIntEnumerator( IntHashTableOfInt table, int missingKeyValue )
            {
                _entry = new Entry( table._buckets, 0 );
                _count = table._count;
                _missingKeyValue = missingKeyValue;
                _table = table;
                Reset();
            }

            public object Current
            {
                get
                {
#if DEBUG
                    if( _counted == 0 )
                    {
                        throw new InvalidOperationException(
                            "Enumerator.Current called without calling MoveNext()." );
                    }
                    if( _savedVersion != _table._version )
                    {
                        throw new InvalidOperationException(
                            "Collection modified while enumeration." );
                    }
#endif
                    return _entry;
                }
            }
            public virtual bool MoveNext()
            {
                if( _counted == _count )
                {
                    return false;
                }
                while( _entry._buckets[ ++_entry._index ].key == _missingKeyValue );
                ++_counted;
                return true;
            }
            public void Reset()
            {
                _entry._index = _counted = 0;
#if DEBUG
                _savedVersion = _table._version;
#endif
            }

            private uint                _count;
            private uint                _counted;
            private int                 _missingKeyValue;
            private Entry               _entry;
            private IntHashTableOfInt   _table;
#if DEBUG
            private int                 _savedVersion;
#endif
        }

        public virtual IEnumerator GetEnumerator()
        {
            return new IntHashTableOfIntEnumerator( this, _missingKeyValue );
        }

        #endregion

        #region implementation details

        protected internal void ReHash( uint desiredSize )
        {
            if( desiredSize < _initialSize )
            {
                desiredSize = _initialSize;
            }
            uint size = HashtableParams.AdjustHashtableSize( desiredSize );
            if( size != _size )
            {
                _firstFree = 0;
                bucket[] oldBuckets = _buckets;
                _hashTable = new uint[ size ];
                _buckets = new bucket[ size * HashtableParams.maxBucketsPerIndex ];
                if( _count > 0 )
                {
                    for( uint i = 1, j = 0; i < oldBuckets.Length; ++i )
                    {
                        int key = oldBuckets[ i ].key;
                        if( key != _missingKeyValue )
                        {
                            ++j;
                            _buckets[ j ].key = key;
                            _buckets[ j ].value = oldBuckets[ i ].value;
                            uint hashValue = ( (uint) key ) % size;
                            _buckets[ j ].next = _hashTable[ hashValue ];
                            _hashTable[ hashValue ] = j;
                        }
                    }
                }
#if DEBUG
                ++_version;
#endif
                _size = size;
            }
        }

		/// <summary>
		/// Returns index of bucket where key is found or zero if not found.
		/// </summary>
		protected internal uint SearchCollisions( int key, out uint tableIndex, out uint prevIndex )
        {
            prevIndex = 0;
            uint bucketIndex = _hashTable[ tableIndex = ( (uint) key ) % _size ];
            if( bucketIndex > 0 && key != _buckets[ bucketIndex ].key )
            {
#if DEBUG
                int savedVersion = _version;
#endif
                _buckets[ 0 ].key = key;
                do
                {
#if DEBUG
                    if( savedVersion != _version || key != _buckets[ 0 ].key )
                    {
                        throw new InvalidOperationException( "Non-serialized usage detected!" );
                    }
#endif
                    bucketIndex = _buckets[ ( prevIndex = bucketIndex ) ].next;
                }
                while( key != _buckets[ bucketIndex ].key );
            }
            return bucketIndex;
        }

        protected internal struct bucket
        {
            public int  key;
            public int  value;
            public uint next;
        }

        protected internal uint     _count;
        protected internal uint     _size;
        protected internal uint     _initialSize;
        protected internal uint     _firstFree;
        protected internal int      _missingKeyValue = Int32.MaxValue;
        protected internal uint[]   _hashTable;
        protected internal bucket[] _buckets;
#if DEBUG
        protected internal int      _version;
#endif
        #endregion
    }

	/// <summary>
	/// IntWeakHashTable implements non-synchronized hashtable of pairs:
	/// key = int, value = weak reference to object
	/// </summary>
    public class IntWeakHashTable : IntHashTable
    {
        public IntWeakHashTable()  : base() {}
        public IntWeakHashTable( int initialSize ) : base( initialSize ) {}

        public override void Add( int key, object value )
        {
            base.Add( key, new WeakReference( value ) );
        }
        public override object this [int  key]
        {
            get
            {
                object value = base[ key ];
                return ( value == null ) ? null : ( (WeakReference)value ).Target;
            }
            set
            {
                Add( key, value );
            }
        }

        // enumerator for self-cleaned hashtable should check whether the current object is alive
        protected internal class IntWeakHashTableEnumerator : IntHashTableEnumerator
        {
            public IntWeakHashTableEnumerator( IntWeakHashTable table )
                : base( table ) {}

            public override bool MoveNext()
            {
                IntWeakHashTable table = (IntWeakHashTable) _table;
                while( base.MoveNext() )
                {
                    Entry current = (Entry) Current;
                    WeakReference wr = current.Value as WeakReference;
                    if( wr != null && wr.IsAlive )
                    {
                        return true;
                    }
                    else
                    {
                        if( table.ValueDead != null )
                        {
                            table.ValueDead( current.Key );
                        }
                    }
                }
                return false;
            }
        }

        public override IEnumerator GetEnumerator()
        {
            return new IntWeakHashTableEnumerator( this );
        }

        public delegate void ValueDeadDelegate( int id );

        public event ValueDeadDelegate ValueDead;

        public void Compact()
        {
            ArrayList buckets = new ArrayList();
            foreach( Entry e in this )
            {
                buckets.Add( _buckets[ e._index ] );
            }
            Clear();
            foreach( bucket b in buckets )
            {
                base.Add( b.key, b.value );
            }
        }

        protected internal override void ReHash( uint desiredSize )
        {
            if( desiredSize < _initialSize )
            {
                desiredSize = _initialSize;
            }
            uint size = HashtableParams.AdjustHashtableSize( desiredSize );
            if( _hashTable == null || size != _hashTable.Length )
            {
                _firstFree = 0;
                bucket[] oldBuckets = _buckets;
                if( _count > 0 )
                {
                    uint count = 0;
                    for( int i = 1; i < oldBuckets.Length; ++i )
                    {
                        int key = oldBuckets[ i ].key;
                        WeakReference value = oldBuckets[ i ].value as WeakReference;
                        if( key != Int32.MaxValue && value != null && value.IsAlive )
                        {
                            oldBuckets[ ++count ].key = oldBuckets[ i ].key;
                            oldBuckets[ count ].value = oldBuckets[ i ].value;
                            oldBuckets[ count ].next = 0;
                        }
                    }
                    if( count < _hashTable.Length / 2 )
                    {
                        _count = count;
                        size = (uint) _hashTable.Length;
                        Array.Clear( _hashTable, 0, _hashTable.Length );
                        for( uint i = 1; i < count + 1; ++i )
                        {
                            int key = _buckets[ i ].key;
                            uint hashValue = ( (uint) key ) % size;
                            _buckets[ i ].next = _hashTable[ hashValue ];
                            _hashTable[ hashValue ] = i;
                        }
#if DEBUG
                        ++_version;
#endif
                        return;
                    }
                }
                _hashTable = new uint[ size ];
                _buckets = new bucket[ size * HashtableParams.maxBucketsPerIndex ];
                if( _count > 0 )
                {
                    for( uint i = 1, j = 0; i < oldBuckets.Length; ++i )
                    {
                        int key = oldBuckets[ i ].key;
                        if( key != Int32.MaxValue )
                        {
                            ++j;
                            _buckets[ j ].key = key;
                            _buckets[ j ].value = oldBuckets[ i ].value;
                            uint hashValue = ( (uint) key ) % size;
                            _buckets[ j ].next = _hashTable[ hashValue ];
                            _hashTable[ hashValue ] = j;
                        }
                    }
                }
#if DEBUG
                ++_version;
#endif
            }
        }
    }
}
