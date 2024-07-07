// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.DataStructures;

namespace JetBrains.Omea.Containers
{
    /**
     * Cache of objects identified by keys of artitrary type
     * Each cached object is identified by a key
     */
    public class ObjectCache: IEnumerable
    {
        public const int defaultSize = 8192;
        public const int minSize = 4;
        public const int maxSize = 65535;

        protected struct CacheEntry
        {
            public object       key;
            public object       x;
            public ushort       prev;
            public ushort       next;
            public ushort       hash_next;
        }

        public ObjectCache() : this( defaultSize ) {}
        public ObjectCache( int cacheSize )
        {
            if( cacheSize < minSize )
            {
                cacheSize = minSize;
            }
            else if( cacheSize > maxSize )
            {
                cacheSize = maxSize;
            }
            _top = _back = 0;
            _cache = new CacheEntry[ cacheSize + 1 ];
            _hashTableSize = HashtableParams.AdjustHashtableSize( (uint) cacheSize );
            _hashTable = new ushort[ _hashTableSize ];
            _Attempts = 0;
            _Hits = 0;
            _count = _firstFree = 0;
            _eventArgs = new ObjectCacheEventArgs();
        }

        public void CacheObject( object key, object x )
        {
            ushort index = _firstFree;

            if( _count < (ushort) ( _cache.Length - 1 ) )
            {
                if( index == 0 )
                {
                    index = _count;
                    ++index;
                }
                else
                {
                    _firstFree = _cache[ index ].hash_next;
                }
                if( _count == 0 )
                {
                    _back = index;
                }
            }
            else
            {
                index = _back;
                RemoveEntryFromHashTable( index );
                if( ObjectRemoved != null )
                {
                    _eventArgs.Key = _cache[ index ].key;
                    _eventArgs.Object = _cache[ index ].x;
                    ObjectRemoved( this, _eventArgs );
                }
                _cache[ _back = _cache[ index ].prev ].next = 0;
            }

            _cache[ index ].key = key;
            _cache[ index ].x = x;
            AddEntry2HashTable( index );
            Add2Top( index );
        }

        public object TryKey( object key )
        {
            ++_Attempts;
            ushort index = SearchForCacheEntry( key );
            if( index == 0 )
            {
                return null;
            }
            ++_Hits;
            if( index != _top )
            {
                RemoveEntry( index );
                Add2Top( index );
            }
            return _cache[ index ].x;
        }

        public bool IsCached( object key )
        {
            return SearchForCacheEntry( key ) != 0;
        }

        public void Remove( object key )
        {
            ushort index = SearchForCacheEntry( key );
            if( index != 0 )
            {
                RemoveEntry( index );
                RemoveEntryFromHashTable( index );
                _cache[ index ].hash_next = _firstFree;
                _firstFree = index;
                if( ObjectRemoved != null )
                {
                    _eventArgs.Key = key;
                    _eventArgs.Object = _cache[ index ].x;
                    ObjectRemoved( this, _eventArgs );
                }
                _cache[ index ].key = _cache[ index ].x = null;
            }
        }

        public void RemoveAll()
        {
            ArrayList keys = new ArrayList( Count );
            for( ushort current = _top; current > 0; )
            {
                object key = _cache[ current ].key;
                if( key != null )
                {
                    keys.Add( key );
                }
                current = _cache[ current ].next;
            }
            for( int i = 0; i < keys.Count; ++i )
            {
                Remove( keys[ i ] );
            }
        }

        public int Count
        {
            get{ return _count; }
        }

        public int Size
        {
            get { return _cache.Length - 1; }
        }

        public double HitRate()
        {
            return ( _Attempts > 0 ) ? ( ( double )_Hits / ( double )_Attempts ) : 0;
        }

        private void Add2Top( ushort index )
        {
            _cache[ index ].next = _top;
            _cache[ index ].prev = 0;
            _cache[ _top ].prev = index;
            _top = index;
        }

        private void RemoveEntry( ushort index )
        {
            if( index == _back )
            {
                _back = _cache[ index ].prev;
            }
            else
            {
                _cache[ _cache[ index ].next ].prev = _cache[ index ].prev;
            }
            if( index == _top )
            {
                _top = _cache[ index ].next;
            }
            else
            {
                _cache[ _cache[ index ].prev ].next = _cache[ index ].next;
            }
        }

        private void AddEntry2HashTable( ushort index )
        {
            uint hash_index = ( (uint) _cache[ index ].key.GetHashCode() ) % _hashTableSize;
            _cache[ index ].hash_next = _hashTable[ hash_index ];
            _hashTable[ hash_index ] = index;
            ++_count;
        }

        private void RemoveEntryFromHashTable( ushort index )
        {
            uint hash_index = ( (uint) _cache[ index ].key.GetHashCode() ) % _hashTableSize;
            ushort current = _hashTable[ hash_index ];
            ushort previous = 0;
            ushort next;
            while( current != 0 )
            {
                next = _cache[ current ].hash_next;
                if( current == index )
                {
                    if( previous != 0 )
                    {
                        _cache[ previous ].hash_next = next;
                    }
                    else
                    {
                        _hashTable[ hash_index ] = next;
                    }
                    --_count;
                    break;
                }
                previous = current;
                current = next;
            }
        }

        private ushort SearchForCacheEntry( object key )
        {
            uint index = ( (uint) key.GetHashCode() ) % _hashTableSize;
            ushort current = _hashTable[ index ];
            _cache[ 0 ].key = key;
            while( !key.Equals( _cache[ current ].key ) )
            {
                current = _cache[ current ].hash_next;
            }
            return current;
        }

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return new ObjectCacheEnumerator( this );
        }

        internal class ObjectCacheEnumerator: IEnumerator
        {
            private ObjectCache     _cache;
            private CacheEntry[]    _cacheArray;
            private ushort          _curEntry;

            public ObjectCacheEnumerator( ObjectCache cache )
            {
                _cache = cache;
                _cacheArray = cache._cache;
                Reset();
            }

            #region IEnumerator Members

            public void Reset()
            {
                _curEntry = 0;
                _cacheArray [0].next = _cache._top;
            }

            public object Current
            {
                get { return _cacheArray[ _curEntry ].x; }
            }

            public bool MoveNext()
            {
                _curEntry = _cacheArray[ _curEntry ].next;
                return _curEntry != 0;
            }

            #endregion
        }

        #endregion

        public event ObjectCacheEventHandler ObjectRemoved;

        protected ushort                _top;
        protected ushort                _back;
        protected CacheEntry[]          _cache;
        protected ushort[]              _hashTable;
        protected uint                  _hashTableSize;
        protected ushort                _count;
        protected ushort                _firstFree;

        private long                    _Attempts;
        private long                    _Hits;
        private ObjectCacheEventArgs    _eventArgs;
    }

    /**
     * Cache of objects identified by the Int32 keys
     * Each cached object is identified by a key
     */
    public class IntObjectCache: IEnumerable
    {
        public const int defaultSize = 8192;
        public const int minSize = 4;
        public const int maxSize = 65535;

        protected struct CacheEntry
        {
            public int          key;
            public object       x;
            public ushort       prev;
            public ushort       next;
            public ushort       hash_next;
        }

        public IntObjectCache() : this( defaultSize ) {}
        public IntObjectCache( int cacheSize )
        {
            if( cacheSize < minSize )
            {
                cacheSize = minSize;
            }
            else if( cacheSize > maxSize )
            {
                cacheSize = maxSize;
            }
            _top = _back = 0;
            _cache = new CacheEntry[ cacheSize + 1 ];
            _hashTableSize = HashtableParams.AdjustHashtableSize( (uint) cacheSize );
            _hashTable = new ushort[ _hashTableSize ];
            _Attempts = 0;
            _Hits = 0;
            _count = _firstFree = 0;
            _eventArgs = new IntObjectCacheEventArgs();
        }

        public void CacheObject( int key, object x )
        {
            if( x == null )
            {
                throw new ArgumentNullException( "x", "Please don't cache nulls" );
            }

            ushort index = _firstFree;

            if( _count < (ushort) ( _cache.Length - 1 ) )
            {
                if( index == 0 )
                {
                    index = _count;
                    ++index;
                }
                else
                {
                    _firstFree = _cache[ index ].hash_next;
                }
                if( _count == 0 )
                {
                    _back = index;
                }
            }
            else
            {
                index = _back;
                RemoveEntryFromHashTable( index );
                if( ObjectRemoved != null )
                {
                    _eventArgs.Key = _cache[ index ].key;
                    _eventArgs.Object = _cache[ index ].x;
                    ObjectRemoved( this, _eventArgs );
                }
                _cache[ _back = _cache[ index ].prev ].next = 0;
            }

            _cache[ index ].key = key;
            _cache[ index ].x = x;
            AddEntry2HashTable( index );
            Add2Top( index );
        }

        public object TryKey( int key )
        {
            ++_Attempts;
            ushort index = SearchForCacheEntry( key );
            if( index == 0 )
            {
                return null;
            }
            ++_Hits;
            if( index != _top )
            {
                RemoveEntry( index );
                Add2Top( index );
            }
            return _cache[ index ].x;
        }

        public bool IsCached( int key )
        {
            return SearchForCacheEntry( key ) != 0;
        }

        public void Remove( int key )
        {
            ushort index = SearchForCacheEntry( key );
            if( index != 0 )
            {
                RemoveEntry( index );
                RemoveEntryFromHashTable( index );
                _cache[ index ].hash_next = _firstFree;
                _firstFree = index;
                if( ObjectRemoved != null )
                {
                    _eventArgs.Key = key;
                    _eventArgs.Object = _cache[ index ].x;
                    ObjectRemoved( this, _eventArgs );
                }
                _cache[ index ].x = null;
            }
        }

        public void RemoveAll()
        {
            IntArrayList keys = new IntArrayList( Count );
            for( ushort current = _top; current > 0; )
            {
                if( _cache[ current ].x != null )
                {
                    keys.Add( _cache[ current ].key );
                }
                current = _cache[ current ].next;
            }
            for( int i = 0; i < keys.Count; ++ i )
            {
                Remove( keys[ i ] );
            }
        }

        public int Count
        {
            get{ return _count; }
        }

        public int Size
        {
            get { return _cache.Length - 1; }
        }

        public double HitRate()
        {
            return ( _Attempts > 0 ) ? ( ( double )_Hits / ( double )_Attempts ) : 0;
        }

        private void Add2Top( ushort index )
        {
            _cache[ index ].next = _top;
            _cache[ index ].prev = 0;
            _cache[ _top ].prev = index;
            _top = index;
        }

        private void RemoveEntry( ushort index )
        {
            if( index == _back )
            {
                _back = _cache[ index ].prev;
            }
            else
            {
                _cache[ _cache[ index ].next ].prev = _cache[ index ].prev;
            }
            if( index == _top )
            {
                _top = _cache[ index ].next;
            }
            else
            {
                _cache[ _cache[ index ].prev ].next = _cache[ index ].next;
            }
        }

        private void AddEntry2HashTable( ushort index )
        {
            uint hash_index = ( (uint) _cache[ index ].key ) % _hashTableSize;
            _cache[ index ].hash_next = _hashTable[ hash_index ];
            _hashTable[ hash_index ] = index;
            ++_count;
        }

        private void RemoveEntryFromHashTable( ushort index )
        {
            uint hash_index = ( (uint) _cache[ index ].key ) % _hashTableSize;
            ushort current = _hashTable[ hash_index ];
            ushort previous = 0;
            ushort next;
            while( current != 0 )
            {
                next = _cache[ current ].hash_next;
                if( current == index )
                {
                    if( previous != 0 )
                    {
                        _cache[ previous ].hash_next = next;
                    }
                    else
                    {
                        _hashTable[ hash_index ] = next;
                    }
                    --_count;
                    break;
                }
                previous = current;
                current = next;
            }
        }

        private ushort SearchForCacheEntry( int key )
        {
            uint index = ( (uint) key ) % _hashTableSize;
            ushort current = _hashTable[ index ];
            _cache[ 0 ].key = key;
            while( key != _cache[ current ].key )
            {
                current = _cache[ current ].hash_next;
            }
            return current;
        }

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return new IntObjectCacheEnumerator( this );
        }

        internal class IntObjectCacheEnumerator: IEnumerator
        {
            private IntObjectCache  _cache;
            private CacheEntry[]    _cacheArray;
            private ushort          _curEntry;

            public IntObjectCacheEnumerator( IntObjectCache cache )
            {
                _cache = cache;
                _cacheArray = cache._cache;
                Reset();
            }

            #region IEnumerator Members

            public void Reset()
            {
                _curEntry = 0;
                _cacheArray [0].next = _cache._top;
            }

            public object Current
            {
                get
                {
                    return _cacheArray[ _curEntry ].x;
                }
            }

            public bool MoveNext()
            {
                _curEntry = _cacheArray[ _curEntry ].next;
                return _curEntry != 0;
            }

            #endregion
        }

        #endregion

        public event IntObjectCacheEventHandler ObjectRemoved;

        protected ushort                _top;
        protected ushort                _back;
        protected CacheEntry[]          _cache;
        protected ushort[]              _hashTable;
        protected uint                  _hashTableSize;
        protected ushort                _count;
        protected ushort                _firstFree;

        private long                    _Attempts;
        private long                    _Hits;
        private IntObjectCacheEventArgs _eventArgs;
    }

    public class ObjectCacheEventArgs : EventArgs
    {
        private object _aKey;
        private object _anObject;

        public object Key
        {
            get { return _aKey; }
            set { _aKey = value; }
        }
        public object Object
        {
            get { return _anObject; }
            set { _anObject = value; }
        }
    }

    public delegate void ObjectCacheEventHandler( object sender, ObjectCacheEventArgs e );

    public class IntObjectCacheEventArgs : EventArgs
    {
        private int _aKey;
        private object _anObject;

        public int Key
        {
            get { return _aKey; }
            set { _aKey = value; }
        }
        public object Object
        {
            get { return _anObject; }
            set { _anObject = value; }
        }
    }

    public delegate void IntObjectCacheEventHandler( object sender, IntObjectCacheEventArgs e );
}
