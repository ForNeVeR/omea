// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using JetBrains.DataStructures;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Base
{
    public class BlobFileSystem : IDisposable
    {
        public BlobFileSystem( String filename, int cacheSize, int minClusterSize )
        {
            _stream = new ClusteredCachedStream( filename, cacheSize, minClusterSize );
        }

        public BlobFileSystem( String filename, ICachingStrategy strategy, int minClusterSize )
        {
            _stream = new ClusteredCachedStream( filename, strategy, minClusterSize );
        }

        public void Dispose()
        {
            _stream.Shutdown();
        }

        public void Flush()
        {
            _stream.Flush();
        }

        public void Lock()
        {
            _lock.Enter();
        }

        public void UnLock()
        {
            _lock.Exit();
        }

        public long Length
        {
            get { return _stream.Length; }
        }

        public bool ManualFlush
        {
            get { return _stream.ManualFlush; }
            set { _stream.ManualFlush = value; }
        }

        public enum FragmentationStrategy
        {
            Quadratic, // default
            Exponential
        }

        public FragmentationStrategy CurrentFragmentationStrategy
        {
            get { return _stream.CurrentFragmentationStrategy; }
            set { _stream.CurrentFragmentationStrategy = value; }
        }

        // default is 31
        public int ClusterCacheSize
        {
            get { return _stream.ClusterCacheSize; }
            set { _stream.ClusterCacheSize = value; }
        }

        public BinaryReader GetFileReader( int handle )
        {
            _stream.CurrentClusterHandle = handle;
            return new BinaryReader( _stream );
        }

        public BinaryWriter AllocFile( out int handle )
        {
            ClusteredCachedStream.Cluster cluster = _stream.AllocCluster( null );
            handle = cluster.Handle;
            _stream.CurrentClusterHandle = handle;
            return new BinaryWriter( _stream );
        }

        public BinaryWriter AppendFile( int handle, ref int lastClusterHandle )
        {
            _stream.CurrentClusterHandle = ( IsValidHandle( lastClusterHandle ) ) ? lastClusterHandle : handle;
            _stream.SkipFile();
            lastClusterHandle = _stream.CurrentClusterHandle;
            return new BinaryWriter( _stream );
        }

        public BinaryWriter RewriteFile( int handle )
        {
            _stream.DeleteFile( handle );
            int h;
            BinaryWriter result = AllocFile( out h );
            if( h != handle )
            {
                throw new InvalidOperationException( "BlobFileSystem.AllocFile() didn't reuse deleted handles" );
            }
            return result;
        }

        public Stream GetRawStream( int handle )
        {
            _stream.CurrentClusterHandle = handle;
            return _stream;
        }

        public void DeleteFile( int handle )
        {
            _stream.DeleteFile( handle );
            _stream.Flush();
        }

        public bool IsValidHandle( int handle )
        {
            return _stream.IsValidHandle( handle );
        }

        /// <summary>
        /// Resturns list of handles of all available files.
        /// </summary>
        /// <returns></returns>
        public IntArrayList GetAllFiles( bool idle )
        {
            int handle;
            IntHashSet deletedFiles = new IntHashSet();
            handle = _stream.GetFirstFreeFileHandle();
            while( IsValidHandle( handle ) && Core.State != CoreState.ShuttingDown &&
                ( !idle || Core.IsSystemIdle ) )
            {
                deletedFiles.Add( handle );
                ClusteredCachedStream.Cluster cluster = _stream.GetCluster( handle );
                handle = _stream.OffsetToHandle( cluster.NextOffset );
            }
            IntArrayList result = new IntArrayList();
            for( long off = ClusteredCachedStream.BLOB_FILE_SYSTEM_HEADER_SIZE; off < _stream.Length; )
            {
                if( Core.State == CoreState.ShuttingDown || ( idle && !Core.IsSystemIdle ) )
                {
                    break;
                }
                handle = _stream.OffsetToHandle( off );
                ClusteredCachedStream.Cluster cluster = _stream.GetCluster( handle );
                if( cluster.PrevOffset == ClusteredCachedStream.NOT_SET && !deletedFiles.Contains( handle ) )
                {
                    result.Add( handle );
                }
                off += cluster.Length;
                off += ClusteredCachedStream.CLUSTER_HEADER_SIZE;
            }
            return result;
        }

        public byte[] GetRawBytes()
        {
            return _stream._rawBytes;
        }

        public void Repair()
        {
            _stream.Repair();
        }

        #region implementation details

        private class ClusteredCachedStream : Stream, IDisposable
        {
            public const int BLOB_FILE_SYSTEM_VERSION = 1;
            public const int NOT_SET = 0;
            public const int CLUSTER_HEADER_SIZE = 12;
            public const int BLOB_FILE_SYSTEM_HEADER_SIZE = 256;
            public const int CLUSTER_CACHE_SIZE = 31;
            public const String FS_HEADER = "JetBrains Omea Blob File System";

            public ClusteredCachedStream( String filename, int cacheSize, int minClusterSize )
            {
                _stream = new CachedStream( GetFileStream( filename ), cacheSize );
                _minClusterSize = minClusterSize;
                Init();
            }

            public ClusteredCachedStream( String filename, ICachingStrategy strategy, int minClusterSize )
            {
                _stream = new CachedStream( GetFileStream( filename ), strategy );
                _minClusterSize = minClusterSize;
                Init();
            }

            public int MinClusterSize
            {
                get { return _minClusterSize; }
            }

            public int ClusterCacheSize
            {
                get { return _clusterCache.Size; }
                set
                {
                    if( _clusterCache.Size != value )
                    {
                        Flush();
                        _clusterCache.ObjectRemoved -= new IntObjectCacheEventHandler( _clusterCache_ObjectRemoved );
                        _clusterCache = new IntObjectCache( value );
                        _clusterCache.ObjectRemoved += new IntObjectCacheEventHandler( _clusterCache_ObjectRemoved );
                    }
                }
            }

            public Cluster GetCluster( int handle )
            {
                Cluster result = _clusterCache.TryKey( handle ) as Cluster;
                if( result == null )
                {
                    long offset = HandleToOffset( handle );
                    if( _freeCluster == null )
                    {
                        result = new Cluster( this, offset );
                    }
                    else
                    {
                        result = _freeCluster;
                        result.Offset = offset;
                        result.LoadHeader();
                    }
                    CacheCluster( result );
                }
                return result;
            }

            public int OffsetToHandle( long offset )
            {
                return (int) ( offset / _minClusterSize );
            }

            public long HandleToOffset( int handle )
            {
                return ( (long) handle ) * _minClusterSize;
            }

            /// <summary>
            /// Handle of current cluster.
            /// </summary>
            public int CurrentClusterHandle
            {
                get { return _currentClusterHandle; }
                set
                {
                    Cluster cluster = GetCluster( _currentClusterHandle = value );
                    cluster.Reset();
                    _stream.Position = cluster.PositionInRawFile;
                }
            }

            public Cluster AllocCluster( Cluster prev  )
            {
                Cluster result;
                if( prev == null )
                {
                    long free = GetFreeOffset();
                    if( free == _stream.Length )
                    {
                        result = AppendCluster( NOT_SET, _minClusterSize );
                    }
                    else
                    {
                        result = GetCluster( OffsetToHandle( free ) );
                        CleanFile( result );
                    }
                }
                else
                {
                    int clusterSize = _minClusterSize;
                    long free = _stream.Length;
                    long prevOffset = prev.Offset;
                    int prevLength = prev.Length + CLUSTER_HEADER_SIZE;
                    if( _fragmentationStrategy == FragmentationStrategy.Exponential )
                    {
                        clusterSize = prevLength;
                    }
                    // try to extend previous cluster if it is the last one in the file
                    if( free == prevOffset + prevLength && prevLength + clusterSize <= _maxClusterSize )
                    {
                        prev.IncLength( clusterSize );
                        _stream.Position = free;
                        AllocClusterSpace( clusterSize );
                        return prev;
                    }
                    prev.NextOffset = free;
                    clusterSize += prevLength;
                    if( clusterSize > _maxClusterSize )
                    {
                        clusterSize = _maxClusterSize;
                    }
                    result = AppendCluster( prevOffset, clusterSize );
                }
                return result;
            }

            public override void Flush()
            {
                if( _dirtyClusters.Count > 0 )
                {
                    IntArrayList handles = IntArrayListPool.Alloc();
                    try
                    {
                        foreach( IntHashSet.Entry e in _dirtyClusters )
                        {
                            handles.Add( e.Key );
                        }
                        for( int i = 0; i < handles.Count; ++i )
                        {
                            ( (Cluster) _clusterCache.TryKey( handles[ i ] ) ).SaveHeader();
                        }
                    }
                    finally
                    {
                        IntArrayListPool.Dispose( handles );
                    }
                    _stream.Flush();
                }
            }

            public bool ManualFlush
            {
                get { return _manualFlush; }
                set { _manualFlush = value; }
            }

            public FragmentationStrategy CurrentFragmentationStrategy
            {
                get { return _fragmentationStrategy; }
                set { _fragmentationStrategy = value; }
            }

            public override void Close()
            {
                if( !_manualFlush )
                {
                    Flush();
                }
            }

            public void Dispose()
            {
                Close();
            }

            public void Shutdown()
            {
                if( _stream != null )
                {
                    Flush();
                    _stream.Close();
                    _stream = null;
                }
            }

            public override int Read( byte[] buffer, int offset, int count )
            {
                if( count == 1 )
                {
                    int b = ReadByte();
                    if( b < 0 )
                    {
                        return 0;
                    }
                    buffer[ offset ] = (byte) b;
                    return 1;
                }
                int savedOffset = offset;
                while( count > 0 )
                {
                    Cluster cluster = PrepareClusterForReading();
                    // end of stream
                    if( cluster == null )
                    {
                        break;
                    }
                    int readBytes = cluster.Size - cluster.Position;
                    if( readBytes > count )
                    {
                        readBytes = count;
                    }
                    _stream.Read( buffer, offset, readBytes );
                    offset += readBytes;
                    count -= readBytes;
                    cluster.IncPosition( readBytes );
                }
                return offset - savedOffset;
            }

            public override int ReadByte()
            {
                Cluster cluster = PrepareClusterForReading();
                if( cluster == null )
                {
                    return -1;
                }
                int readByte = _stream.ReadByte();
                cluster.IncPosition( 1 );
                return readByte;
            }

            public override void Write( byte[] buffer, int offset, int count )
            {
                if( count == 1 )
                {
                    WriteByte( buffer[ offset ] );
                }
                else
                {
                    while( count > 0 )
                    {
                        Cluster cluster = PrepareClusterForWriting();
                        int writeBytes = cluster.Length - cluster.Position;
                        if( count < writeBytes )
                        {
                            writeBytes = count;
                        }
                        _stream.Write( buffer, offset, writeBytes );
                        count -= writeBytes;
                        offset += writeBytes;
                        cluster.IncPosition( writeBytes );
                    }
                }
            }

            public override void WriteByte( byte value )
            {
                Cluster cluster = PrepareClusterForWriting();
                _stream.WriteByte( value );
                cluster.IncPosition( 1 );
            }

            public void SkipFile()
            {
                Cluster cluster = GetCluster( _currentClusterHandle );
                while( cluster.NextOffset != NOT_SET )
                {
                    cluster = SetCurrentCluster( cluster.NextOffset );
                }
                cluster.IncPosition( cluster.Size );
                _stream.Position = cluster.PositionInRawFile;
            }

            public override long Seek( long offset, SeekOrigin origin )
            {
                throw new NotImplementedException();
            }

            public override void SetLength( long value )
            {
                throw new NotImplementedException();
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override long Length
            {
                get { return _stream.Length; }
            }

            public override long Position
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public void DeleteFile( int handle )
            {
                Cluster cluster = GetCluster( handle );
                cluster.PrevOffset = HandleToOffset( GetFirstFreeFileHandle() );
                _firstFreeFileHandle = handle;
                CleanFile( cluster );
                SaveHeader();
                Flush();
            }

            public void CleanFile( Cluster cluster )
            {
                long prevOffset = NOT_SET;
                for( ; ; )
                {
                    cluster.Size = 0;
                    if( prevOffset != NOT_SET && cluster.PrevOffset != prevOffset )
                    {
                        cluster.PrevOffset = prevOffset;
                    }
                    long nextOffset = cluster.NextOffset;
                    if( nextOffset == NOT_SET )
                    {
                        break;
                    }
                    prevOffset = cluster.Offset;
                    int handle = OffsetToHandle( nextOffset );
                    if( IsValidHandle( handle ) )
                    {
                        cluster = GetCluster( handle );
                    }
                    else
                    {
                        cluster.NextOffset = NOT_SET;
                        break;
                    }
                }
            }

            public bool IsValidHandle( int handle )
            {
                return handle > 0 && _stream != null && handle < ( Length / MinClusterSize );
            }

            public int GetFirstFreeFileHandle()
            {
                int handle = _firstFreeFileHandle;
                return IsValidHandle( handle ) ? handle : NOT_SET;
            }

            public void Repair()
            {
                if (Length % _minClusterSize != 0)
                {
                    int paddingLength = (int) (_minClusterSize - Length % _minClusterSize);
                    byte[] padding = new byte[paddingLength];
                    _stream.Seek(0, SeekOrigin.End);
                    _stream.Write(padding, 0, paddingLength);
                }
            }

            private static FileStream GetFileStream( string filename )
            {
                return new FileStream( filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 256 );
            }

            private void Init()
            {
                try
                {
                    if( _minClusterSize != 16 && _minClusterSize != 32 && _minClusterSize != 64 && _minClusterSize != 128 && _minClusterSize != 256 )
                    {
                        throw new ArgumentException( "Minimum cluster size can only be 16, 32, 64, 128 or 256" );
                    }
                    _maxClusterSize = 0x10000 - _minClusterSize;
                    _fragmentationStrategy = FragmentationStrategy.Quadratic;
                    _reader = new BinaryReader( _stream );
                    _writer = new BinaryWriter( _stream );
                    _clusterCache = new IntObjectCache( CLUSTER_CACHE_SIZE );
                    _dirtyClusters = new IntHashSet();
                    _clusterCache.ObjectRemoved += new IntObjectCacheEventHandler( _clusterCache_ObjectRemoved );
                    _firstFreeFileHandle = NOT_SET;
                    _rawBytes = new byte[ _minClusterSize - CLUSTER_HEADER_SIZE ];
                    if( _stream.Length == 0 )
                    {
                        SaveHeader();
                        _stream.SetLength( BLOB_FILE_SYSTEM_HEADER_SIZE );
                    }
                    else
                    {
                        if( _stream.Length < BLOB_FILE_SYSTEM_HEADER_SIZE || !_reader.ReadString().Equals( FS_HEADER ) )
                        {
                            throw new IOException( "Invalid file system header" );
                        }
                        if( _reader.ReadInt32() != BLOB_FILE_SYSTEM_VERSION )
                        {
                            throw new IOException( "Invalid file system version" );
                        }
                        _firstFreeFileHandle = _reader.ReadInt32();
                    }
                }
                catch( Exception e )
                {
                    Shutdown();
                    throw e;
                }
            }

            private void SaveHeader()
            {
                _stream.Position = 0;
                _writer.Write( FS_HEADER );
                _writer.Write( BLOB_FILE_SYSTEM_VERSION );
                _writer.Write( GetFirstFreeFileHandle() );
            }

            private long GetFreeOffset()
            {
                // that means we are allocating first cluster for a file been allocated
                // thus, we can reuse earlier deleted chain of clusters (deleted file)
                int freeHandle = _firstFreeFileHandle;
                if( freeHandle != NOT_SET && IsValidHandle( freeHandle ) )
                {
                    Cluster free = GetCluster( freeHandle );
                    _firstFreeFileHandle = OffsetToHandle( free.PrevOffset );
                    free.PrevOffset = NOT_SET;
                    SaveHeader();
                    Flush();
                    return free.Offset;
                }
                return _stream.Length;
            }

            private Cluster AppendCluster( long prevOffset, int clusterSize )
            {
                long offset = _stream.Length;
                Cluster result = _freeCluster;
                if( result == null )
                {
                    result = new Cluster( this, offset, prevOffset, NOT_SET, 0, clusterSize );
                }
                else
                {
                    result.Init( this, offset, prevOffset, NOT_SET, 0, clusterSize );
                    result.SaveHeader();
                }
                AllocClusterSpace( clusterSize - CLUSTER_HEADER_SIZE );
                CacheCluster( result );
                return result;
            }

            private void _clusterCache_ObjectRemoved( object sender, IntObjectCacheEventArgs e )
            {
                _freeCluster = (Cluster)e.Object;
                _freeCluster.SaveHeader();
            }

            private Cluster SetCurrentCluster( long offset )
            {
                return GetCluster( CurrentClusterHandle = OffsetToHandle( offset ) );
            }

            private void AllocClusterSpace( int clusterSize )
            {
                if( _rawBytes.Length < clusterSize )
                {
                    _rawBytes = new byte[ clusterSize ];
                }
                _stream.Write( _rawBytes, 0, clusterSize );
            }

            private void CacheCluster( Cluster cluster )
            {
                _clusterCache.CacheObject( cluster.Handle, cluster );
            }

            private Cluster PrepareClusterForReading()
            {
                Cluster result = GetCluster( _currentClusterHandle );
                while( result.Position >= result.Size )
                {
                    if( result.Size == 0 )
                    {
                        return null;
                    }
                    long next = result.NextOffset;
                    if( next == NOT_SET )
                    {
                        return null;
                    }
                    result = SetCurrentCluster( next );
                }
                return result;
            }

            private Cluster PrepareClusterForWriting()
            {
                Cluster result = GetCluster( _currentClusterHandle );
                if( result.Position >= result.Length )
                {
                    long next = result.NextOffset;
                    if( next != NOT_SET )
                    {
                        result = SetCurrentCluster( next );
                    }
                    else
                    {
                        Cluster newCluster = AllocCluster( result );
                        // new cluster was atucally allocated and linked with the
                        // _currentCluster, else myCurrentCluster was just extended
                        if( newCluster == result )
                        {
                            _stream.Position =  newCluster.PositionInRawFile;
                        }
                        else
                        {
                            CurrentClusterHandle = newCluster.Handle;
                            result = newCluster;
                        }
                    }
                }
                return result;
            }

            private CachedStream BaseStream
            {
                get { return _stream; }
            }

            private BinaryReader BaseReader
            {
                get { return _reader; }
            }

            private BinaryWriter BaseWriter
            {
                get { return _writer; }
            }

            internal class Cluster
            {
                public Cluster( ClusteredCachedStream stream, long offset )
                {
                    Init( stream, offset);
                    LoadHeader();
                }

                public Cluster( ClusteredCachedStream stream, long offset, long prevOffset, long nextOffset, int size, int length )
                {
                    Init( stream, offset, prevOffset, nextOffset, size, length );
                    SaveHeader();
                }

                public void Init( ClusteredCachedStream stream, long offset  )
                {
                    if( offset % stream._minClusterSize != 0 )
                    {
                        throw new IOException( "Badly aligned cluster offset: offset=" + offset +
                            ", min cluster size=" + stream._minClusterSize );
                    }
                    _stream = stream;
                    _offset = offset;
                }

                public void Init( ClusteredCachedStream stream, long offset, long prevOffset, long nextOffset, int size, int length )
                {
                    if( offset % stream._minClusterSize != 0 )
                    {
                        throw new IOException( "Badly aligned cluster offset: offset=" + offset +
                            ", min cluster size=" + stream._minClusterSize );
                    }
                    _stream = stream;
                    _offset = offset;
                    _prevOffset = prevOffset;
                    NextOffset = nextOffset;
                    _size = size;
                    _length = length - CLUSTER_HEADER_SIZE;
                }

                public void LoadHeader()
                {
                    _stream.BaseStream.Position = _offset;
                    BinaryReader reader = _stream.BaseReader;
                    _prevOffset = _stream.HandleToOffset( reader.ReadInt32() );
                    _nextOffset = _stream.HandleToOffset( reader.ReadInt32() );
                    _size = reader.ReadUInt16();
                    _length = reader.ReadUInt16();
                    SetDirty( false );
                }

                public void SaveHeader()
                {
                    if( _isDirty )
                    {
                        _stream.BaseStream.Position = _offset;
                        BinaryWriter writer = _stream.BaseWriter;
                        writer.Write( _stream.OffsetToHandle( _prevOffset ) );
                        writer.Write( _stream.OffsetToHandle( _nextOffset ) );
                        writer.Write( (ushort)_size );
                        writer.Write( (ushort)_length );
                        SetDirty( false );
                    }
                    Reset();
                }

                public void Reset()
                {
                    _position = 0;
                }

                public long Offset
                {
                    get { return _offset; }
                    set { _offset = value; }
                }

                public int Handle
                {
                    get { return _stream.OffsetToHandle( _offset ); }
                }

                public int Size
                {
                    get { return _size; }
                    set
                    {
                        _size = value;
                        SetDirty( true );
                    }
                }

                public int Length
                {
                    get { return _length; }
                }

                public void IncLength( int addend )
                {
                    if( _length + addend > _stream._maxClusterSize - CLUSTER_HEADER_SIZE )
                    {
                        throw new IOException( "Too long cluster" );
                    }
                    _length += addend;
                    SetDirty( true );
                }

                public long NextOffset
                {
                    get { return _nextOffset; }
                    set
                    {
                        _nextOffset = value;
                        SetDirty( true );
                    }
                }

                public long PrevOffset
                {
                    get { return _prevOffset; }
                    set
                    {
                        _prevOffset = value;
                        SetDirty( true );
                    }
                }

                public int Position
                {
                    get { return _position; }
                }

                public long PositionInRawFile
                {
                    get { return _offset + _position + CLUSTER_HEADER_SIZE; }
                }

                public void IncPosition( int addend )
                {
                    if( ( _position += addend ) > _length )
                    {
                        throw new IOException( "Position is out of cluster bounds" );
                    }
                    if( _position > _size )
                    {
                        Size = _position;
                    }
                }

                private void SetDirty( bool dirty )
                {
                    if( _isDirty != dirty )
                    {
                        if( _isDirty = dirty )
                        {
                            _stream._dirtyClusters.Add( Handle );
                        }
                        else
                        {
                            _stream._dirtyClusters.Remove( Handle );
                        }
                    }
                }

                private ClusteredCachedStream   _stream;
                private long                    _offset;
                private long                    _prevOffset;
                private long                    _nextOffset;
                private int                     _size;
                private int                     _length;
                private int                     _position;
                private bool                    _isDirty;
            }

            private CachedStream            _stream;
            private BinaryReader            _reader;
            private BinaryWriter            _writer;
            private int                     _currentClusterHandle;
            private Cluster                 _freeCluster;
            private IntObjectCache          _clusterCache;
            private IntHashSet              _dirtyClusters;
            private int                     _firstFreeFileHandle;
            private bool                    _manualFlush;
            private FragmentationStrategy   _fragmentationStrategy;
            private int                     _minClusterSize;
            private int                     _maxClusterSize;

            public byte[]                   _rawBytes;
        }

        private ClusteredCachedStream   _stream;
        private SpinWaitLock            _lock = new SpinWaitLock();

        #endregion
    }
}
