/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using System.Threading;
using JetBrains.DataStructures;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.Base
{
    /// <summary>
    /// Cached stream over a random-access stream, to use instead of .NET BufferedStream.
    /// Does high-performance caching of underlying stream.
    /// </summary>
    public class CachedStream : Stream, IDisposable
    {
        public const int _defaultCacheSize = CachedPage._pageSize * 16;

        public CachedStream( Stream stream )
            : this( stream, _defaultCacheSize ) {}

        public CachedStream( Stream stream, int cacheSize )
        {
            if( !stream.CanSeek )
            {
                throw new ArgumentException(
                    "Underlying stream should be a random-access one (stream.CanSeek is true).", "stream" );
            }
            _stream = stream;
            cacheSize &= (int)(CachedPage._outPageBits & 0x7fffffff);
            _length = _stream.Length;
            _position = _stream.Position;
            _isOpen = true;
            _cachingStrategy = new SingleStreamCachingStrategy( cacheSize );
        }

        public CachedStream( Stream stream, ICachingStrategy strategy )
        {
            if( !stream.CanSeek )
            {
                throw new ArgumentException(
                    "Underlying stream should be a random-access one (stream.CanSeek is true).", "stream" );
            }
            _stream = stream;
            _length = _stream.Length;
            _position = _stream.Position;
            _isOpen = true;
            _cachingStrategy = strategy;
        }

        ~CachedStream()
        {
            Dispose();
        }

        public object SyncRoot
        {
            get { return _syncRoot; }
            set { _syncRoot = value; }
        }

        #region IDisposable implementation

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize( this );
        }

        #endregion

        #region Stream implementation

        public override int GetHashCode()
        {
            return _stream.GetHashCode();
        }

        public override bool Equals( object obj )
        {
            CachedStream stream = obj as CachedStream;
            return stream != null && stream._stream.Equals( _stream );
        }

        public override string ToString()
        {
            return _stream.ToString();
        }
 
        public override bool CanRead
        {
            get
            {
                return _stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _stream.CanWrite;
            }
        }

        public override void Close()
        {
            if( _isOpen )
            {
                Flush();
                _stream.Close();
                _isOpen = false;
            }
        }

        public override void Flush()
        {
            _cachingStrategy.Flush( this );
        }

        public override long Length
        {
            get
            {
                return _length;
            }
        }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                if( value < 0 || value > _length )
                {
                    throw new ArgumentOutOfRangeException( "Position = " + value + " _length = " + _length );
                }
                _position = value;
            }
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            switch( origin )
            {
                case SeekOrigin.Begin:
                {
                    if( offset > _length )
                    {
                        throw new InvalidOperationException( "Can't seek beyond the end of stream." );
                    }
                    _position = offset;
                    break;
                }
                case SeekOrigin.End:
                {
                    if( offset > _length )
                    {
                        throw new InvalidOperationException( "Can't seek before the begin of stream." );
                    }
                    
                    _position = _length - offset;
                    break;
                }
                case SeekOrigin.Current:
                {
                    if( offset + _position > _length )
                    {
                        throw new InvalidOperationException( "Can't seek beyond the end of stream." );
                    }
                    if( offset + _position < 0 )
                    {
                        throw new InvalidOperationException( "Can't seek before the begin of stream." );
                    }
                    _position += offset;
                    break;
                }
            }
            return _position;
        }

        public override void SetLength( long value )
        {
            _cachingStrategy.SetLength( this, value );
        }

        public override int ReadByte()
        {
            byte result = this[ _position ];
            ++_position;
            return result;
        }

        public override void WriteByte( byte value )
        {
            this[ _position ] = value;
            ++_position;
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            int savedOffset = offset;
            while( count > 0 )
            {
                if( _position == _length )
                {
                    throw new EndOfStreamException();
                }
                CachedPage page = _cachingStrategy.GetPage( this, _position );
                int pageIndex = (int) _position & CachedPage._inPageBits;
                int readBytes = page.Read( pageIndex, buffer, offset, count );
                _position += readBytes;
                offset += readBytes;
                count -= readBytes;
            }
            return offset - savedOffset;
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            while( count > 0 )
            {
                CachedPage page = _cachingStrategy.GetPage( this, _position );
                int pageIndex = (int) _position & CachedPage._inPageBits;
                int pageBytes = CachedPage._pageSize - pageIndex;
                if( pageBytes > count )
                {
                    pageBytes = count;
                }
                page.Write( buffer, offset, pageIndex, pageBytes );
                count -= pageBytes;
                _position += pageBytes;
                offset += pageBytes;
                if( _position > _length )
                {
                    _length = _position;
                }
            }
        }

        #region async I/O stubs

        public override IAsyncResult BeginRead( byte[] buffer, int offset, int count, AsyncCallback callback, object state )
        {
            return _stream.BeginRead( buffer, offset, count, callback, state );
        }

        public override IAsyncResult BeginWrite( byte[] buffer, int offset, int count, AsyncCallback callback, object state )
        {
            return _stream.BeginWrite( buffer, offset, count, callback, state );
        }

        public override int EndRead( IAsyncResult asyncResult )
        {
            return _stream.EndRead( asyncResult );
        }

        public override void EndWrite( IAsyncResult asyncResult )
        {
            _stream.EndWrite( asyncResult );
        }

        protected override WaitHandle CreateWaitHandle()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region not implemented

        public override System.Runtime.Remoting.ObjRef CreateObjRef( Type requestedType )
        {
            throw new NotImplementedException();
        }

        public override object InitializeLifetimeService()
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
        
        #region implementation details

        internal Stream GetUnderlyingStream()
        {
            return _stream;
        }

        internal void AddDirtyOffset( int offset )
        {
            _cachingStrategy.AddDirtyOffset( this, offset );
        }

        internal void RemoveDirtyOffset( int offset )
        {
            _cachingStrategy.RemoveDirtyOffset( this, offset );
        }

        internal void SetLengthImpl( long length )
        {
            _length = length;
        }

        private byte this[ long index ]
        {
            get
            {
                if( index >= _length )
                {
                    throw new EndOfStreamException();
                }
                CachedPage page = _cachingStrategy.GetPage( this, index );
                return page[ (int) index & CachedPage._inPageBits ];
            }
            set
            {
                CachedPage page = _cachingStrategy.GetPage( this, index );
                page[ (int) index & CachedPage._inPageBits ] = value;
                if( index >= _length )
                {
                    _length = index + 1;
                }
            }
        }

        /// <summary>
        /// default caching strategy for single stream
        /// </summary>
        private class SingleStreamCachingStrategy : ICachingStrategy
        {
            public SingleStreamCachingStrategy( int cacheSize )
            {
                _dirtyPages = new IntHashSet();
                _pagesCache = new IntObjectCache( cacheSize >> CachedPage._pageShiftBits );
                _pagesCache.ObjectRemoved += new IntObjectCacheEventHandler( _pagesCache_ObjectRemoved );
            }

            public CachedPage GetPage( CachedStream owner, long offset )
            {
                offset &= CachedPage._outPageBits;
                if( _lastAccessedPage != null && _lastAccessedPage.Offset == offset )
                {
                    return _lastAccessedPage;
                }
                int shiftedOffset = (int) ( offset >> CachedPage._pageShiftBits );
                CachedPage result = (CachedPage) _pagesCache.TryKey( shiftedOffset );
                if( result == null )
                {
                    long bytes2End = owner.Length - offset;
                    if( bytes2End < 0 )
                    {
                        bytes2End = 0;
                    }
                    if( _freePage == null )
                    {
                        result = new CachedPage( owner, offset );
                    }
                    else
                    {
                        result = _freePage;
                        result.Offset = offset;
                    }
                    result.Size = ( bytes2End >= CachedPage._pageSize ) ? CachedPage._pageSize : (int) bytes2End;
                    _pagesCache.CacheObject( shiftedOffset, result );
                    result.Load();
                }
                _lastAccessedPage = result;
                return result;
            }

            public void SetLength( CachedStream owner, long length )
            {
                IntArrayList obsoletePages = null;
                try
                {
                    if( owner.Length > length )
                    {
                        foreach( CachedPage page in _pagesCache )
                        {
                            if( page.Offset >= length )
                            {
                                if( obsoletePages == null )
                                {
                                    obsoletePages = IntArrayListPool.Alloc();
                                }
                                obsoletePages.Add( (int) ( page.Offset >> CachedPage._pageShiftBits ) );
                                page.ClearDirty();
                                if( _lastAccessedPage != null && _lastAccessedPage.Offset == page.Offset )
                                {
                                    _lastAccessedPage = null;
                                }
                            }
                            else if( page.Offset + page.Size > length )
                            {
                                int newSize = (int) ( length - page.Offset );
                                if( newSize != page.Size ) 
                                {
                                    page.Size = newSize;
                                    page.SetDirty();
                                }
                            }
                        }
                        if( obsoletePages != null )
                        {
                            for( int i = 0; i < obsoletePages.Count; ++i  )
                            {
                                _pagesCache.Remove( obsoletePages[ i ] );
                            }
                        }
                    }
                    owner.SetLengthImpl( length );
                }
                finally
                {
                    owner.GetUnderlyingStream().SetLength( length );
                    if( obsoletePages != null )
                    {
                        IntArrayListPool.Dispose( obsoletePages );
                    }
                }
            }

            public void Flush( CachedStream owner )
            {
                if( _dirtyPages.Count > 0 )
                {
                    if( _dirtyPages.Count == 1 )
                    {
                        foreach( IntHashSet.Entry e in _dirtyPages )
                        {
                            ( (CachedPage) _pagesCache.TryKey( e.Key ) ).Save();
                        }
                    }
                    else
                    {
                        IntArrayList pageOffsets = IntArrayListPool.Alloc();
                        try
                        {
                            foreach( IntHashSet.Entry e in _dirtyPages )
                            {
                                pageOffsets.Add( e.Key );
                            }
                            pageOffsets.Sort();
                            for( int i = 0; i < pageOffsets.Count; ++i )
                            {
                                ( (CachedPage) _pagesCache.TryKey( pageOffsets[ i ] ) ).Save();
                            }
                        }
                        finally
                        {
                            IntArrayListPool.Dispose( pageOffsets );
                        }
                    }
                }
                if( owner.GetUnderlyingStream().CanWrite )
                {
                    owner.GetUnderlyingStream().Flush();
                }
            }

            public void AddDirtyOffset( CachedStream owner, int offset )
            {
                _dirtyPages.Add( offset );
            }

            public void RemoveDirtyOffset( CachedStream owner, int offset )
            {
                _dirtyPages.Remove( offset );
            }

            private void _pagesCache_ObjectRemoved(object sender, IntObjectCacheEventArgs e)
            {
                _freePage = (CachedPage) e.Object;
                _freePage.Save();
            }

            private IntObjectCache  _pagesCache;
            private IntHashSet      _dirtyPages;
            private CachedPage      _freePage;
            private CachedPage      _lastAccessedPage;
        }

        private Stream              _stream;
        private long                _length;
        private long                _position;
        private bool                _isOpen;
        private object              _syncRoot;
        private ICachingStrategy    _cachingStrategy;

        #endregion
    }

    public interface ICachingStrategy
    {
        CachedPage GetPage( CachedStream owner, long offset );
        void SetLength( CachedStream owner, long length );
        void Flush( CachedStream owner );
        void AddDirtyOffset( CachedStream owner, int offset );
        void RemoveDirtyOffset( CachedStream owner, int offset );
    }

    public class SharedCachingStrategy: ICachingStrategy
    {
        public SharedCachingStrategy( int cacheSize )
        {
            cacheSize &= (int)(CachedPage._outPageBits & 0x7fffffff);
            _dirtyPages = new HashSet();
            _pagesCache = new ObjectCache( cacheSize >> CachedPage._pageShiftBits );
            _pagesCache.ObjectRemoved += new ObjectCacheEventHandler( _pagesCache_ObjectRemoved );
            _searchKey = new CachedPageKey( null, 0 );
        }

        public CachedPage GetPage( CachedStream owner, long offset )
        {
            offset &= CachedPage._outPageBits;
            if( _lastAccessedPage != null && Object.ReferenceEquals( _lastAccessedPage.Owner, owner )
                && _lastAccessedPage.Offset == offset )
            {
                return _lastAccessedPage;
            }
            _searchKey._owner = owner;
            _searchKey._offset = (int) ( offset >> CachedPage._pageShiftBits );
            CachedPage result = (CachedPage) _pagesCache.TryKey( _searchKey );
            if( result == null )
            {
                long bytes2End = owner.Length - offset;
                if( bytes2End < 0 )
                {
                    bytes2End = 0;
                }
                if( _freePage == null )
                {
                    result = new CachedPage( owner, offset );
                }
                else
                {
                    result = _freePage;
                    result.Owner = owner;
                    result.Offset = offset;
                    _freePage = null;
                }
                result.Size = ( bytes2End >= CachedPage._pageSize ) ? CachedPage._pageSize : (int) bytes2End;
                _pagesCache.CacheObject( _searchKey.Clone(), result );
                result.Load();
            }
            _lastAccessedPage = result;
            return result;
        }

        public void SetLength( CachedStream owner, long length )
        {
            IntArrayList obsoleteOffsets = null;
            try
            {
                if( owner.Length > length )
                {
                    foreach( CachedPage page in _pagesCache )
                    {
                        if( Object.ReferenceEquals( page.Owner, owner ) )
                        {
                            if( page.Offset >= length )
                            {
                                if( obsoleteOffsets == null )
                                {
                                    obsoleteOffsets = IntArrayListPool.Alloc();
                                }
                                obsoleteOffsets.Add( (int) ( page.Offset >> CachedPage._pageShiftBits ) );
                                page.ClearDirty();
                                if( _lastAccessedPage != null && _lastAccessedPage.Offset == page.Offset )
                                {
                                    _lastAccessedPage = null;
                                }
                            }
                            else if( page.Offset + page.Size > length )
                            {
                                int newSize = (int) ( length - page.Offset );
                                if( newSize != page.Size ) {
                                    page.Size = newSize;
                                    page.SetDirty();
                                }
                            }
                        }
                    }
                    if( obsoleteOffsets != null )
                    {
                        _searchKey._owner = owner;
                        for( int i = 0; i < obsoleteOffsets.Count; ++i  )
                        {
                            _searchKey._offset = obsoleteOffsets[ i ];
                            _pagesCache.Remove( _searchKey );
                        }
                    }
                }
                owner.SetLengthImpl( length );
            }
            finally
            {
                owner.GetUnderlyingStream().SetLength( length );
                if( obsoleteOffsets != null )
                {
                    IntArrayListPool.Dispose( obsoleteOffsets );
                }
            }
        }

        public void Flush( CachedStream owner )
        {
            if( _dirtyPages.Count > 0 )
            {
                if( _dirtyPages.Count == 1 )
                {
                    foreach( HashSet.Entry e in _dirtyPages )
                    {
                        CachedPageKey key = (CachedPageKey)e.Key;
                        if( Object.ReferenceEquals( key._owner, owner ) )
                        {
                            ( (CachedPage) _pagesCache.TryKey( key ) ).Save();
                        }
                    }
                }
                else
                {
                    IntArrayList pageOffsets = IntArrayListPool.Alloc();
                    try
                    {
                        foreach( HashSet.Entry e in _dirtyPages )
                        {
                            CachedPageKey key = (CachedPageKey)e.Key;
                            if( Object.ReferenceEquals( key._owner, owner ) )
                            {
                                pageOffsets.Add( key._offset );
                            }
                        }
                        pageOffsets.Sort();
                        _searchKey._owner = owner;
                        for( int i = 0; i < pageOffsets.Count; ++i )
                        {
                            _searchKey._offset = pageOffsets[ i ];
                            ( (CachedPage) _pagesCache.TryKey( _searchKey ) ).Save();
                        }
                    }
                    finally
                    {
                        IntArrayListPool.Dispose( pageOffsets );
                    }
                }
            }
            if( owner.GetUnderlyingStream().CanWrite )
            {
                owner.GetUnderlyingStream().Flush();
            }
        }

        public void AddDirtyOffset( CachedStream owner, int offset )
        {
            _dirtyPages.Add( new CachedPageKey( owner, offset ) );
        }

        public void RemoveDirtyOffset( CachedStream owner, int offset )
        {
            _searchKey._owner = owner;
            _searchKey._offset = offset ;
            _dirtyPages.Remove( _searchKey );
        }

        private void _pagesCache_ObjectRemoved(object sender, ObjectCacheEventArgs e)
        {
            _freePage = (CachedPage) e.Object;
            _freePage.Save();
        }

        private class CachedPageKey : ICloneable
        {
            public CachedStream _owner;
            public int _offset;

            public CachedPageKey( CachedStream owner, int offset ) 
            {
                _owner = owner;
                _offset = offset;
            }

            public override bool Equals( object o ) 
            {
                CachedPageKey right = (CachedPageKey)o;
                return Object.ReferenceEquals( right._owner, _owner ) && right._offset == _offset;
            }

            public override int GetHashCode() 
            {
                return _owner.GetHashCode() + _offset;
            }

            public object Clone()
            {
                return new CachedPageKey( _owner, _offset );
            }
        }

        private ObjectCache     _pagesCache;
        private HashSet         _dirtyPages;
        private CachedPage      _freePage;
        private CachedPage      _lastAccessedPage;
        private CachedPageKey   _searchKey;
    }

    public class CachedPage
    {
        public CachedPage( CachedStream owner, long offset )
        {
            _owner = owner;
            _page = new byte[ _pageSize ];
            _offset = offset;
        }

        public CachedStream Owner
        {
            get { return _owner; }
            set { _owner = value; }
        }

        public long Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public int Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public bool IsDirty
        {
            get { return _isDirty; }
        }

        public void Load()
        {
            if( _size > 0 )
            {
                _owner.GetUnderlyingStream().Position = _offset;
                _owner.GetUnderlyingStream().Read( _page, 0, _size );
            }
            _isDirty = false;
        }

        public void Save( )
        {
            if( _isDirty )
            {
                ClearDirty();
                if( _size > 0 )
                {
                    Stream stream = _owner.GetUnderlyingStream();
                    if( stream.Length < _offset )
                    {
                        stream.SetLength( _offset );
                    }
                    stream.Position = _offset;
                    stream.Write( _page, 0, _size );
                }
            }
        }

        public byte this[ int index ]
        {
            get 
            {
                if( index >= _size )
                {
                    throw new InvalidOperationException( "byte this[ int index ], get: index >= _size" );
                }
                return _page[ index ];
            }
            set
            {
                _page[ index ] = value;
                if( _size <= index )
                {
                    _size = index + 1;
                }
                SetDirty();
            }
        }

        public int Read( int sourceIndex, byte[] buffer, int destIndex, int count )
        {
            int bytesRead = count;
            if( sourceIndex + bytesRead > _size )
            {
                bytesRead = _size - sourceIndex;
            }
            Array.Copy( _page, sourceIndex, buffer, destIndex, bytesRead );
            return bytesRead;
        }

        public void Write( byte[] buffer, int sourceIndex, int destIndex, int count )
        {
            Array.Copy( buffer, sourceIndex, _page, destIndex, count );
            if( _size < destIndex + count )
            {
                _size = destIndex + count;
            }
            SetDirty();
        }

        public void SetDirty()
        {
            if( !_isDirty )
            {
                _isDirty = true;
                _owner.AddDirtyOffset( (int) ( _offset >> _pageShiftBits ) );
            }
        }

        public void ClearDirty()
        {
            _isDirty = false;
            _owner.RemoveDirtyOffset( (int) ( _offset >> _pageShiftBits ) );
        }

        public const int    _pageSize = 0x2000;
        public const int    _inPageBits = 0x1fff;
        public const long   _outPageBits = 0x7fffffffffffe000;
        public const int    _pageShiftBits = 13;

        private CachedStream        _owner;
        private long                _offset;
        private int                 _size;
        private byte[]              _page;
        private bool                _isDirty;
    }    
}