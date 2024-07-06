// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Collections;

namespace JetBrains.Omea.Containers
{
    internal class BTreeNode : IComparable
    {
        private IComparable _minKey;
        private int _minOffset;
        private int _count = 0;
        private long _pageOffset = 0;

        public BTreeNode( IComparable minKey )
        {
            _minKey = minKey;
        }

        public BTreeNode( IComparable minKey, int minOffset, long pageOffset )
        {
            _minKey = minKey;
            _minOffset = minOffset;
            _pageOffset = pageOffset;
        }

        public BTreeNode( IComparable minKey, int minOffset, long pageOffset, int count )
        {
            _minKey = minKey;
            _minOffset = minOffset;
            _pageOffset = pageOffset;
            _count = count;
        }

        public void ChangeMinKey( IComparable minKey, int minOffset )
        {
            _minKey = minKey;
            _minOffset = minOffset;
        }

        public int IncrementCount()
        {
            return ++_count;
        }
        public int DecrementCount()
        {
            return --_count;
        }

        public IComparable MinKey { get { return _minKey; } }
        public int MinOffset { get { return _minOffset; } }
        public void SetNewCount( int newCount )
        {
            _count = newCount;
        }
        public int Count { get { return _count; } }
        public long PageOffset { get { return _pageOffset; } }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if ( _minKey != null )
            {
                int result = _minKey.CompareTo( ((BTreeNode)obj)._minKey );
                if ( result == 0 )
                {
                    result = _minOffset - ((BTreeNode)obj)._minOffset;
                }
                return result;
            }
            else
            {
                return -1;
            }
        }

        #endregion
    }

    public class KeyPair
    {
        public IFixedLengthKey  _key;
        public int              _offset;
        public KeyPair() {}
        public KeyPair( IFixedLengthKey key, int offset )
        {
            _key = key;
            _offset = offset;
        }
    }

    internal class BTreePage
    {
        private int _maxCount = 0;
        private BTreeNode _treeNode = null;

        private FileStream _stream;
        private BinaryReader _reader;
        private BinaryWriter _writer;

        private MemoryStream _memoryStream;
        private BinaryReader _memoryReader;
        private BinaryWriter _memoryWriter;

        private IFixedLengthKey _factoryKey;
        private IFixedLengthKey _curKey;
        private byte[] _bytes = new byte[BTree.PAGE_SIZE];
        private int _keySize;
        private bool _modified;
        private int _rightBound;

        private const int _deferredArrayStartCapacity = 4;
        private const int _deferredArrayMaximumSize = 256;
        private ArrayList _deferredDeletions = new ArrayList( _deferredArrayStartCapacity );
        private ArrayList _deferredInsertions = new ArrayList( _deferredArrayStartCapacity );
        private KeyPair _keyPair = new KeyPair();
        private static KeyPairComparer _keyComparer = new KeyPairComparer();

        private class KeyPairComparer : IComparer
        {
            public int Compare( object x, object y )
            {
                KeyPair pair_x = (KeyPair) x;
                KeyPair pair_y = (KeyPair) y;
                int result = pair_x._key.CompareTo( pair_y._key );
                if( result == 0 )
                {
                    result = pair_x._offset - pair_y._offset;
                }
                return result;
            }
        }

        public BTreePage( BTreeNode treeNode, int maxCount, FileStream stream, IFixedLengthKey factoryKey )
        {
            SetPageData( treeNode, maxCount, factoryKey );
            _stream = stream;
            _reader = new BinaryReader( stream );
            _writer = new BinaryWriter( stream );

            _memoryStream = new MemoryStream( _bytes, 0, BTree.PAGE_SIZE );
            _memoryReader = new BinaryReader( _memoryStream );
            _memoryWriter = new BinaryWriter( _memoryStream );
        }

        public void SetPageData( BTreeNode treeNode, int maxCount, IFixedLengthKey factoryKey )
        {
            _treeNode = treeNode;
            _rightBound = _treeNode.Count;
            _maxCount = maxCount;
            _factoryKey = factoryKey;
            _curKey = _factoryKey.FactoryMethod();
            _keySize = _factoryKey.KeySize + 4;
        }

        private IFixedLengthKey MemoryRead( int index, out int offset )
        {
            SetPosition( index );
            return MemoryReadNext( out offset );
        }
        private void SetPosition( int index )
        {
            _memoryReader.BaseStream.Position = BTree.HEADER_SIZE + _keySize * index;
        }
        private IFixedLengthKey MemoryReadNext( out int offset )
        {
            _curKey.Read( _memoryReader );
            offset = _memoryReader.ReadInt32();
            return _curKey;
        }

        private void MemoryWrite( IFixedLengthKey key, int offset, int index )
        {
            _memoryWriter.BaseStream.Position = BTree.HEADER_SIZE + _keySize * index;
            key.Write( _memoryWriter );
            _memoryWriter.Write( offset );
        }
        public int GetAllKeys( IntArrayList offsets )
        {
            int count = _treeNode.Count;
            if ( count > 0 )
            {
                ProcessDeferredUpdates();
                SetPosition( 0 );
                for( int index = 0; index < count; index++ )
                {
                    int offset;
                    MemoryReadNext( out offset );
                    offsets.Add( offset );
                }
            }
            return count;
        }
        public int GetAllKeys( ArrayList keys_offsets )
        {
            int count = _treeNode.Count;
            if ( count > 0 )
            {
                ProcessDeferredUpdates();
                SetPosition( 0 );
                for( int index = 0; index < count; index++ )
                {
                    int offset;
                    IFixedLengthKey key = MemoryReadNext( out offset );
                    keys_offsets.Add( new KeyPair( key.FactoryMethod(), offset ) );
                }
            }
            return count;
        }
        public void SearchForRange( IFixedLengthKey beginKey, IFixedLengthKey endKey, IntArrayList offsets )
        {
            if ( _treeNode.Count == 0 )
            {
                return;
            }

            int index, offset;
            KeyPair pair;
            IFixedLengthKey deferredKey;
            int deferredInsertionsIndex = ~GetDeferredInsertionIndex( beginKey, -1 );

            IFixedLengthKey foundKey = SearchL( 0, _rightBound, beginKey, -1, out index );
            if ( foundKey != null && index != -1 && index < _rightBound )
            {
                int deferredDeletionsIndex = ~GetDeferredDeletionIndex( beginKey, -1 );
                SetPosition( index );
                for ( ; index < _rightBound; ++index )
                {
                    IFixedLengthKey aKey = MemoryReadNext( out offset );
                    for( ; deferredInsertionsIndex < _deferredInsertions.Count; ++deferredInsertionsIndex )
                    {
                        pair = (KeyPair)_deferredInsertions[ deferredInsertionsIndex ];
                        deferredKey = pair._key;
                        if( deferredKey.CompareTo( aKey ) > 0 )
                        {
                            break;
                        }
                        if( beginKey.CompareTo( deferredKey ) <= 0 )
                        {
                            if( endKey.CompareTo( deferredKey ) < 0 )
                            {
                                break;
                            }
                            offsets.Add( pair._offset );
                        }
                    }
                    if ( endKey.CompareTo( aKey ) < 0 )
                    {
                        break;
                    }
                    bool deleted = false;
                    while( deferredDeletionsIndex < _deferredDeletions.Count )
                    {
                        pair = (KeyPair)_deferredDeletions[ deferredDeletionsIndex ];
                        int compareResult = pair._key.CompareTo( aKey );
                        if( compareResult == 0 )
                        {
                            compareResult = pair._offset - offset;
                        }
                        if( compareResult > 0 )
                        {
                            break;
                        }
                        ++deferredDeletionsIndex;
                        if( compareResult == 0 )
                        {
                            deleted = true;
                            break;
                        }
                    }
                    if( !deleted )
                    {
                        offsets.Add( offset );
                    }
                }
            }
            for( ; deferredInsertionsIndex < _deferredInsertions.Count; ++deferredInsertionsIndex )
            {
                pair = (KeyPair)_deferredInsertions[ deferredInsertionsIndex ];
                deferredKey = pair._key;
                if( beginKey.CompareTo( deferredKey ) <= 0 )
                {
                    if( endKey.CompareTo( deferredKey ) < 0 )
                    {
                        break;
                    }
                    offsets.Add( pair._offset );
                }
            }
        }

        public void SearchForRange( IFixedLengthKey beginKey, IFixedLengthKey endKey, ArrayList keys_offsets )
        {
            if ( _treeNode.Count == 0 )
            {
                return;
            }

            int index, offset;
            KeyPair pair;
            IFixedLengthKey deferredKey;
            int deferredInsertionsIndex = ~GetDeferredInsertionIndex( beginKey, -1 );

            IFixedLengthKey foundKey = SearchL( 0, _rightBound, beginKey, -1, out index );
            if ( foundKey != null && index != -1 && index < _rightBound )
            {
                int deferredDeletionsIndex = ~GetDeferredDeletionIndex( beginKey, -1 );
                SetPosition( index );
                for ( ; index < _rightBound; ++index )
                {
                    IFixedLengthKey aKey = MemoryReadNext( out offset );
                    for( ; deferredInsertionsIndex < _deferredInsertions.Count; ++deferredInsertionsIndex )
                    {
                        pair = (KeyPair)_deferredInsertions[ deferredInsertionsIndex ];
                        deferredKey = pair._key;
                        if( deferredKey.CompareTo( aKey ) > 0 )
                        {
                            break;
                        }
                        if( beginKey.CompareTo( deferredKey ) <= 0 )
                        {
                            if( endKey.CompareTo( deferredKey ) < 0 )
                            {
                                break;
                            }
                            keys_offsets.Add( pair );
                        }
                    }
                    if ( endKey.CompareTo( aKey ) < 0 )
                    {
                        break;
                    }
                    bool deleted = false;
                    while( deferredDeletionsIndex < _deferredDeletions.Count )
                    {
                        pair = (KeyPair)_deferredDeletions[ deferredDeletionsIndex ];
                        int compareResult = pair._key.CompareTo( aKey );
                        if( compareResult == 0 )
                        {
                            compareResult = pair._offset - offset;
                        }
                        if( compareResult > 0 )
                        {
                            break;
                        }
                        ++deferredDeletionsIndex;
                        if( compareResult == 0 )
                        {
                            deleted = true;
                            break;
                        }
                    }
                    if( !deleted )
                    {
                        keys_offsets.Add( new KeyPair( aKey.FactoryMethod(), offset ) );
                    }
                }
            }
            for( ; deferredInsertionsIndex < _deferredInsertions.Count; ++deferredInsertionsIndex )
            {
                pair = (KeyPair)_deferredInsertions[ deferredInsertionsIndex ];
                deferredKey = pair._key;
                if( beginKey.CompareTo( deferredKey ) <= 0 )
                {
                    if( endKey.CompareTo( deferredKey ) < 0 )
                    {
                        break;
                    }
                    keys_offsets.Add( pair );
                }
            }
        }

        private IFixedLengthKey Search( int l, int r, IFixedLengthKey key, int offset, out int index )
        {
            index = -1;
            if ( l > r )
            {
                return null;
            }
            IFixedLengthKey mKey = null;
            while( l < r )
            {
                int m = ( l + r ) >> 1;
                int off;
                mKey = MemoryRead( m, out off );
                int compareResult = key.CompareTo( mKey );
                if ( compareResult == 0 && offset != -1 )
                {
                    compareResult = offset - off;
                }
                index = m;
                if ( compareResult == 0 )
                {
                    return mKey;
                }
                if( compareResult > 0 )
                {
                    l = m + 1;
                }
                else
                {
                    r = m;
                }
            }

            return null;
        }

        private IFixedLengthKey SearchL( int l, int r, IFixedLengthKey key, int offset, out int index )
        {
            index = -1;
            if ( l > r )
            {
                return null;
            }

            index = l;

            IFixedLengthKey mKey = null;
            while( l < r )
            {
                int m = ( l + r ) >> 1;
                int off;
                mKey = MemoryRead( m, out off );
                int compareResult = key.CompareTo( mKey );
                if ( compareResult == 0 && offset != -1 )
                {
                    compareResult = offset - off;
                }
                if( compareResult > 0 )
                {
                    l = m + 1;
                    index = l;
                }
                else
                {
                    r = m;
                    index = r;
                }
            }

            return mKey;
        }

        private IFixedLengthKey SearchR( int l, int r, IFixedLengthKey key, int offset, out int index )
        {
            index = -1;
            if ( l > r )
            {
                return null;
            }

            index = r;

            IFixedLengthKey mKey = null;
            while( l < r )
            {
                int m = ( l + r ) >> 1;
                int off;
                mKey = MemoryRead( m, out off );
                int compareResult = key.CompareTo( mKey );
                if ( compareResult == 0 && offset != -1 )
                {
                    compareResult = offset - off;
                }
                if( compareResult >= 0 )
                {
                    l = m + 1;
                    index = l;
                }
                else
                {
                    r = m;
                    index = m;
                }
            }

            return mKey;
        }

        private void CopyBytes( Array source, int sourceIndex, Array target, int targetIndex, int count )
        {
            long sIndex = _keySize * sourceIndex + BTree.HEADER_SIZE;
            long dIndex = _keySize * targetIndex + BTree.HEADER_SIZE;
            long shiftCount = _keySize * count;
            Array.Copy( source, sIndex, target, dIndex, shiftCount );
        }

        private void ProcessDeferredUpdates()
        {
            int count = _treeNode.Count;
            _rightBound = count;
            if( count == 0 )
            {
                _deferredDeletions.Clear();
                _deferredInsertions.Clear();
                return;
            }

            int deleted = _deferredDeletions.Count;
            int inserted = _deferredInsertions.Count;
            if( deleted > 0 || inserted > 0 )
            {
                int off;
                int rightBound = count + deleted - inserted;
                int i = _maxCount;
                bool isDeleted;
                KeyPair pair;

                while( rightBound > 0 )
                {
                    isDeleted = false;
                    IFixedLengthKey key = MemoryRead( --rightBound, out off );
                    _keyPair._key = key;
                    _keyPair._offset = off;

                    // at first, look through deferred deletions
                    while( deleted > 0 )
                    {
                        pair = (KeyPair)_deferredDeletions[ deleted - 1 ];
                        int compareResult = _keyComparer.Compare( _keyPair, pair );
                        if( compareResult >= 0 )
                        {
                            if( ( isDeleted = compareResult == 0 ) )
                            {
                                --deleted;
                            }
                            break;
                        }
                        --deleted;
                    }

                    // if current key is not deleted, merge it with deferred insertions
                    if( !isDeleted )
                    {
                        while( inserted > 0 )
                        {
                            pair = (KeyPair)_deferredInsertions[ inserted - 1 ];
                            if( _keyComparer.Compare( _keyPair, pair ) > 0 )
                            {
                                MemoryWrite( key, off, --i );
                                break;
                            }
                            else
                            {
                                MemoryWrite( pair._key, pair._offset, --i );
                                --inserted;
                            }
                        }
                        if( inserted == 0 )
                        {
                            MemoryWrite( key, off, --i );
                        }
                    }
                }

                // if some insertions remain, write it before all other key pairs
                while( inserted > 0 )
                {
                    pair = (KeyPair)_deferredInsertions[ --inserted ];
                    MemoryWrite( pair._key, pair._offset, --i );
                }

                if( i > 0 )
                {
                    CopyBytes( _bytes, i, _bytes, 0, count );
                }

                _deferredDeletions.Clear();
                _deferredInsertions.Clear();
            }
        }

        private bool DeferDeletion( IFixedLengthKey key, int offset )
        {
            int index = GetDeferredInsertionIndex( key, offset );
            if( index >= 0 )
            {
                _deferredInsertions.RemoveAt( index );
                return true;
            }
            else
            {
                if( _deferredDeletions.Count == _deferredArrayMaximumSize )
                {
                    ProcessDeferredUpdates();
                }
                index = GetDeferredDeletionIndex( key, offset );
                if( index < 0 )
                {
                    _deferredDeletions.Insert( ~index, new KeyPair( key.FactoryMethod(), offset ) );
                    return true;
                }
            }
            return false;
        }

        private int GetDeferredDeletionIndex( IFixedLengthKey key, int offset )
        {
            _keyPair._key = key;
            _keyPair._offset = offset;
            return _deferredDeletions.BinarySearch( _keyPair, _keyComparer );
        }

        private void DeferInsertion( IFixedLengthKey key, int offset )
        {
            int index = GetDeferredDeletionIndex( key, offset );
            if( index >= 0 )
            {
                _deferredDeletions.RemoveAt( index );
            }
            else
            {
                int insertedCount = _deferredInsertions.Count;
                if( insertedCount == _deferredArrayMaximumSize || _rightBound + insertedCount == _maxCount )
                {
                    ProcessDeferredUpdates();
                }
                index = GetDeferredInsertionIndex( key, offset );
                if( index < 0 )
                {
                    _deferredInsertions.Insert( ~index, new KeyPair( key.FactoryMethod(), offset ) );
                }
            }
        }

        private int GetDeferredInsertionIndex( IFixedLengthKey key, int offset )
        {
            _keyPair._key = key;
            _keyPair._offset = offset;
            return _deferredInsertions.BinarySearch( _keyPair, _keyComparer );
        }

        private void UpdateMinKey( IFixedLengthKey key, int offset )
        {
            int keyCompare = key.CompareTo( _treeNode.MinKey );
            if ( keyCompare < 0 || ( keyCompare == 0 && offset < _treeNode.MinOffset ) )
            {
                _treeNode.ChangeMinKey( key.FactoryMethod(), offset );
            }
        }

        public void DeleteKey( IFixedLengthKey key, int offset )
        {
            if( DeferDeletion( key, offset ) )
            {
                _modified = true;
                _treeNode.DecrementCount();
            }
        }

        public void InsertKey( IFixedLengthKey key, int offset )
        {
            DeferInsertion( key, offset );
            UpdateMinKey( key, offset );
            _modified = true;
            _treeNode.IncrementCount();
        }

        public void Read()
        {
            _reader.BaseStream.Position = _treeNode.PageOffset;
            _reader.Read( _bytes, 0, BTree.PAGE_SIZE );
            _memoryReader.BaseStream.Position = BTree.HEADER_SIZE;
            _rightBound = _treeNode.Count;
            _modified = false;
        }
        public void Write()
        {
            if( _modified )
            {
                _modified = false;
                ProcessDeferredUpdates();
                _memoryWriter.BaseStream.Position = 0;
                _memoryWriter.Write( _treeNode.Count );
                _writer.BaseStream.Position = _treeNode.PageOffset;
                _writer.Write( _bytes, 0, BTree.PAGE_SIZE );
                _writer.Flush();
            }
        }
        public BTreeNode BTreeNode
        {
            get { return _treeNode; }
        }
        public BTreePage Split( IFixedLengthKey key, int offset,  long pageOffset, float splitFactor )
        {
            ProcessDeferredUpdates();

            if( splitFactor >= 1 || splitFactor < 0.1 )
            {
                throw new Exception( "BTreePage.Split: splitFactor = " + splitFactor.ToString() + " is not applicable" );
            }

            int mIndex = (int) (_maxCount * splitFactor);
            int off;
            IFixedLengthKey minKey = MemoryRead( mIndex, out off ).FactoryMethod();
            BTreePage splittedPage =  new BTreePage(
                new BTreeNode( minKey, off, pageOffset, _maxCount - mIndex ), _maxCount, _stream, _factoryKey );

            CopyBytes( _bytes, mIndex, splittedPage._bytes, 0, _maxCount - mIndex );
            _treeNode.SetNewCount( mIndex );
            _rightBound = mIndex;
            _modified = splittedPage._modified = true;

            if ( key.CompareTo( minKey ) < 0 )
            {
                InsertKey( key, offset );
            }
            else
            {
                splittedPage.InsertKey( key, offset );
            }

            return splittedPage;
        }
        public void Merge( BTreePage rightPage )
        {
            int offset;

            ProcessDeferredUpdates();
            rightPage.ProcessDeferredUpdates();
            rightPage.SetPosition( 0 );
            for( int i = 0; i < rightPage._rightBound; ++i )
            {
                IFixedLengthKey key = rightPage.MemoryReadNext( out offset );
                MemoryWrite( key, offset, _rightBound++ );
            }
            _treeNode.SetNewCount( _rightBound );
            rightPage._treeNode.SetNewCount( 0 );
            _modified = rightPage._modified = true;
        }
        public bool Full()
        {
            return _treeNode.Count == _maxCount;
        }
        public bool Empty()
        {
            return _treeNode.Count == 0;
        }
    }
    public interface IFixedLengthKey : IComparable
    {
        IFixedLengthKey FactoryMethod( BinaryReader reader );
        IFixedLengthKey FactoryMethod();
        void Read( BinaryReader reader );
        void Write( BinaryWriter writer );
        IComparable Key
        {
            get; set;
        }
        int KeySize { get; }
        void SetIntKey( int key );
    }

    /// <summary>
    /// classes for compound keys
    /// </summary>
    public class Compound : IComparable
    {
        public IComparable _key1;
        public IComparable _key2;
        public Compound( IComparable key1, IComparable key2 )
        {
            _key1 = key1;
            _key2 = key2;
        }
        public Compound Clone()
        {
            return new Compound( _key1, _key2 );
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            int result = _key1.CompareTo( ((Compound)obj)._key1 );
            if ( result == 0 )
            {
                result = _key2.CompareTo( ((Compound)obj)._key2 );
            }
            return result;
        }

        #endregion
    }
    public class CompoundAndValue : IComparable
    {
        public IComparable _key1;
        public IComparable _key2;
        public IComparable _value;
        public CompoundAndValue( IComparable key1, IComparable key2, IComparable value )
        {
            _key1 = key1;
            _key2 = key2;
            _value = value;
        }
        public CompoundAndValue Clone()
        {
            return new CompoundAndValue( _key1, _key2, _value );
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            int result = _key1.CompareTo( ((CompoundAndValue)obj)._key1 );
            if ( result == 0 )
            {
                result = _key2.CompareTo( ((CompoundAndValue)obj)._key2 );
            }
            /*if ( result == 0 )
            {
                result = _value.CompareTo( ((CompoundAndValue)obj)._value );
            }*/
            return result;
        }

        #endregion
    }

    public class KeySizeException : Exception
    {
        public KeySizeException() : base( "KeySize should be positive integer" )
        {
        }
    }

    /// <summary>
    /// interface to a BTree implementation
    /// is declared as abstract class in order to be able to define static flag
    /// which is used by factory methods of key classes ( IFixedLengthKey FactoryMethod )
    /// also is used in managed C++ DBIndex implementation
    ///
    /// </summary>
    public abstract class IBTree : IDisposable
    {
        public abstract void Dispose();
        public abstract bool Open();
        public abstract void Close();
        public abstract void Clear();

        public abstract void GetAllKeys( IntArrayList offsets );
        public abstract void GetAllKeys( ArrayList keys_offsets );
        public abstract IEnumerable GetAllKeys();

        public abstract void SearchForRange( IFixedLengthKey beginKey, IFixedLengthKey endKey, IntArrayList offsets );
        public abstract void SearchForRange( IFixedLengthKey beginKey, IFixedLengthKey endKey, ArrayList keys_offsets );
        public abstract IEnumerable SearchForRange( IFixedLengthKey beginKey, IFixedLengthKey endKey );

        public abstract void DeleteKey( IFixedLengthKey key, int offset );
        public abstract void InsertKey( IFixedLengthKey key, int offset );

        public abstract int MaxCount { get; }
        public abstract int Count { get; }

        public abstract void SetCacheSize( int numberOfPages );
        public abstract int GetCacheSize();

        public abstract int GetLoadedPages();
        public abstract int GetPageSize();

        public static bool _bUseOldKeys = false;
    }

    [Serializable]
    public class BadIndexesException : Exception
    {
        public BadIndexesException() : base() { }
        public BadIndexesException( string message ) : base( message ) {}
        public BadIndexesException( string message, Exception innerException ) : base( message, innerException ) {}

        protected BadIndexesException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }

    }

    public class BTree : IBTree
    {
        public const int PAGE_SIZE = 0x4000;
        public const int HEADER_SIZE = 1024;
        public const int CACHE_SIZE = 20;
        private string _fileName;

        private FileStream _stream;
        private BinaryReader _reader;
        private BinaryWriter _writer;

        private int _count = 0;
        private int _maxCount = 0;
        private RedBlackTree _rbTree = new RedBlackTree();
        private BTreeNode _searchNode = new BTreeNode( null );
        private IFixedLengthKey _factoryKey;
        private Stack freePages = new Stack();
        private ObjectCache _cache;
        private BTreePage _freeNode;

        public BTree( string fileName, IFixedLengthKey factoryKey )
        {
            _factoryKey = factoryKey;
            _fileName = fileName;
            if ( _factoryKey.KeySize <= 0 )
            {
                throw new KeySizeException();
            }
            _maxCount = (PAGE_SIZE - HEADER_SIZE) / ( _factoryKey.KeySize + 4 );
        }

        #region IBTree implementation

        public override void Dispose()
        {
        }

        public override bool Open()
        {
            _stream = new FileStream( _fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 64 );
            _rbTree = new RedBlackTree();
            _reader = new BinaryReader( _stream );
            _writer = new BinaryWriter( _stream );
            _count = 0;
            _cache = new ObjectCache( CACHE_SIZE );
            _cache.ObjectRemoved += new ObjectCacheEventHandler( _cache_ObjectRemoved );

            freePages.Clear();
            int numPages = (int)(_stream.Length / PAGE_SIZE);
            //Tracer._Trace( "####################### " + _fileName );
            for ( int i = 0; i < numPages; i++ )
            {
                long pageOffset = i * PAGE_SIZE;
                _stream.Position = pageOffset;
                int count = _reader.ReadInt32();
                //Tracer._Trace( "####################### count = " + count.ToString() );

                _stream.Position = pageOffset + HEADER_SIZE;
                IFixedLengthKey key = _factoryKey.FactoryMethod( _reader );
                int offset = _reader.ReadInt32();
                if ( count != 0 )
                {
                    _rbTree.RB_Insert( new BTreeNode( key, offset, pageOffset, count ) );
                }
                else
                {
                    freePages.Push( pageOffset );
                }
                _count += count;
            }
            return true;
        }

        public override void Close()
        {
            _cache.RemoveAll();
            _cache = null;
            _freeNode = null;
            _count = 0;
            _reader.Close();
            _writer.Close();
            _stream.Close();
        }

        public override void Clear()
        {
            Close();
            File.Delete( _fileName );
            Open();
        }

        public override void GetAllKeys( IntArrayList offsets )
        {
            RBNodeBase rbNode = _rbTree.GetMinimumNode();

            while ( rbNode != null )
            {
                BTreePage page = PreparePage( (BTreeNode)rbNode.Key );
                if ( page.GetAllKeys( offsets ) == 0 )
                {
                    break;
                }
                rbNode = _rbTree.GetNext( rbNode );
            }
        }

        public override void GetAllKeys( ArrayList keys_offsets )
        {
            RBNodeBase rbNode = _rbTree.GetMinimumNode();

            while ( rbNode != null )
            {
                BTreePage page = PreparePage( (BTreeNode)rbNode.Key );
                if ( page.GetAllKeys( keys_offsets ) == 0 )
                {
                    break;
                }
                rbNode = _rbTree.GetNext( rbNode );
            }
        }

        public override IEnumerable GetAllKeys()
        {
            throw new NotImplementedException( "Use OmniaMeaBTree." );
        }

        public override void SearchForRange( IFixedLengthKey beginKey, IFixedLengthKey endKey, IntArrayList offsets )
        {
            _searchNode.ChangeMinKey( beginKey, 0 );
            RBNodeBase rbNode = _rbTree.GetMaximumLess( _searchNode );
            if ( rbNode == null )
            {
                rbNode = _rbTree.GetMinimumNode();
            }

            while ( rbNode != null && ((BTreeNode)rbNode.Key).MinKey.CompareTo( endKey ) <= 0 )
            {
                BTreePage page = PreparePage( (BTreeNode)rbNode.Key );
                page.SearchForRange( beginKey, endKey, offsets );
                rbNode = _rbTree.GetNext( rbNode );
            }
        }

        public override void SearchForRange( IFixedLengthKey beginKey, IFixedLengthKey endKey, ArrayList keys_offsets )
        {
            _searchNode.ChangeMinKey( beginKey, 0 );
            RBNodeBase rbNode = _rbTree.GetMaximumLess( _searchNode );
            if ( rbNode == null )
            {
                rbNode = _rbTree.GetMinimumNode();
            }

            while ( rbNode != null && ((BTreeNode)rbNode.Key).MinKey.CompareTo( endKey ) <= 0 )
            {
                BTreePage page = PreparePage( (BTreeNode)rbNode.Key );
                page.SearchForRange( beginKey, endKey, keys_offsets );
                rbNode = _rbTree.GetNext( rbNode );
            }
        }

        public override IEnumerable SearchForRange( IFixedLengthKey beginKey, IFixedLengthKey endKey )
        {
            throw new NotImplementedException( "Use OmniaMeaBTree." );
        }

        public override void DeleteKey( IFixedLengthKey key, int offset )
        {
            RBNodeBase rbNode;
            BTreeNode foundNode = SearchBTreeNode( key, offset, out rbNode );
            if ( foundNode != null )
            {
                BTreePage page = PreparePage( foundNode );
                _count -= foundNode.Count;
                page.DeleteKey( key, offset );
                int pageCount = foundNode.Count;
                _count += pageCount;
                long pageOffset;
                if ( page.Empty() )
                {
                    pageOffset = page.BTreeNode.PageOffset;
                    freePages.Push( pageOffset );
                    _rbTree.RB_Delete( page.BTreeNode );
                    _cache.Remove( pageOffset );
                }
                else if( pageCount < (_maxCount >> 2) && (rbNode = _rbTree.GetNext( rbNode )) != null )
                {
                    BTreeNode rightNode = (BTreeNode) rbNode.Key;
                    if( pageCount + rightNode.Count < (_maxCount >> 1) )
                    {
                        BTreePage rightPage = PreparePage( rightNode );
                        pageOffset = rightPage.BTreeNode.PageOffset;
                        page.Merge( rightPage );
                        freePages.Push( pageOffset );
                        _rbTree.RB_Delete( rightPage.BTreeNode );
                        _cache.Remove( pageOffset );
                    }
                }
            }
        }

        public override void InsertKey( IFixedLengthKey key, int offset )
        {
            _count++;

            RBNodeBase rbNode;
            BTreeNode foundNode = SearchBTreeNode( key, offset, out rbNode );

            if ( foundNode == null )
            {
                if ( _rbTree.Count == 0 )
                {
                    BTreeNode newNode = new BTreeNode( key.FactoryMethod(), offset, GetOffsetForNewPage() );
                    _rbTree.RB_Insert( newNode );
                    BTreePage page = NewPage( newNode );
                    page.InsertKey( key, offset );
                    page.Write();
                    _cache.CacheObject( newNode.PageOffset, page );
                    return;
                }
                else
                {
                    RBNodeBase rbMinNode = _rbTree.GetMinimumNode();
                    foundNode = (BTreeNode)rbMinNode.Key;
                }
            }
            else
            {
                BTreePage page = PreparePage( foundNode );
                if ( page.Full() )
                {
                    float splitFactor = ( _rbTree.GetNext( rbNode ) == null ) ? 0.875f : 0.5f;
                    BTreePage splittedPage = page.Split( key, offset, GetOffsetForNewPage(), splitFactor );
                    _rbTree.RB_Insert( splittedPage.BTreeNode );
                    splittedPage.Write();
                    _cache.CacheObject( splittedPage.BTreeNode.PageOffset, splittedPage );
                }
                else
                {
                    page.InsertKey( key, offset );
                }
            }
        }

        public override int MaxCount { get { return _maxCount; } }

        public override int Count { get { return _count; } }

        public override void SetCacheSize( int numberOfPages ) {}

        public override int GetCacheSize()
        {
            return CACHE_SIZE;
        }

        #endregion

        private long GetOffsetForNewPage()
        {
            if ( freePages.Count > 0 )
            {
                return (long)freePages.Pop();
            }
            return _stream.Length;
        }

        private void _cache_ObjectRemoved( object sender, ObjectCacheEventArgs e )
        {
            _freeNode = (BTreePage) e.Object;
            _freeNode.Write();
        }

        private BTreePage NewPage( BTreeNode treeNode )
        {
            BTreePage result = _freeNode;
            if( result == null )
            {
                result = new BTreePage( treeNode, _maxCount, _stream, _factoryKey );
            }
            else
            {
                result.SetPageData( treeNode, _maxCount, _factoryKey );
                _freeNode = null;
            }
            return result;
        }

        private BTreeNode SearchBTreeNode( IFixedLengthKey key, int offset, out RBNodeBase rbNode )
        {
            _searchNode.ChangeMinKey( key, offset );
            rbNode = _rbTree.GetEqualOrLess( _searchNode );
            if ( rbNode == null )
            {
                rbNode = _rbTree.GetMinimumNode();
            }
            return ( rbNode != null ) ? (BTreeNode)rbNode.Key : null ;
        }

        private BTreePage PreparePage( BTreeNode foundNode )
        {
            BTreePage page = (BTreePage)_cache.TryKey( foundNode.PageOffset );
            if ( page == null )
            {
                page = NewPage( foundNode );
                page.Read();
                _cache.CacheObject( foundNode.PageOffset, page );
            }
            return page;
        }

        public override int GetLoadedPages()
        {
            throw new NotImplementedException();
        }

        public override int GetPageSize()
        {
            throw new NotImplementedException();
        }
    }
}
