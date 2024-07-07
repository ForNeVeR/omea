// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;

namespace JetBrains.Omea.Database
{
    internal abstract class Column
    {
        protected string _name = null;
        private IDBIndex _index = null;
        private int _indexNum = -1;
        protected int _version;
        protected Table _table;

        internal Column( Table table, string name, int version )
        {
            _table = table;
            _name = name;
            _version = version;
        }

        protected object[] _fields;
        protected int _fieldIndex;

        public void SetSharedFields( object[] fields, int fieldIndex )
        {
            _fields = fields;
            _fieldIndex = fieldIndex;
        }

        protected object[] Fields { get { return _fields; } }
        protected int FieldIndex { get { return _fieldIndex; } }

        public abstract FixedLengthKey GetFixedFactory();

        public string Name{ get{ return _name; } }

        public int Version{ get{ return _version; } }

        public abstract Object Value { get; set; }

        public abstract object SaveValue( SafeBinaryWriter writer );

        public abstract void LoadValue( SafeBinaryReader reader );

        public abstract ColumnType Type{ get; }

        public void SetIndex( IDBIndex dbIndex, int indexNum )
        {
            _index = dbIndex;
            _indexNum = indexNum;
        }
        public IDBIndex Index
        {
            get { return _index; }
        }
        public int IndexNum
        {
            get { return _indexNum; }
        }
    }

    internal class DateTimeColumn : Column
    {
        internal DateTimeColumn( Table table, string name, int version ) : base( table, name, version )
        {
        }
        public override void LoadValue( SafeBinaryReader reader )
        {
            long ticks = reader.ReadInt64();
            try
            {
                _fields[_fieldIndex] = new DateTime( ticks );
            }
            catch( ArgumentOutOfRangeException exception )
            {
                _fields[_fieldIndex] = DateTime.MinValue;
                Tracer._TraceException( exception );
                throw new DateTimeCorruptedException( "Invalid DateTime ticks value: " + ticks );
            }
        }

        public override object SaveValue( SafeBinaryWriter writer )
        {
            object value = _fields[_fieldIndex];
            if ( value == null )
            {
                value = DateTime.MinValue;
            }
            writer.Write( ((DateTime )value).Ticks );
            return value;
        }
        public override FixedLengthKey GetFixedFactory()
        {
            return new FixedLengthKey_DateTime( DateTime.MinValue );
        }

        public override Object Value
        {
            get
            {
                object value = _fields[_fieldIndex];
                if ( value == null )
                {
                    value = DateTime.MinValue;
                }
                return value;
            }
            set
            {
                if ( value == null ) value = DateTime.MinValue;
                _fields[_fieldIndex] = (DateTime)value;
            }
        }

        public override ColumnType Type{ get{ return ColumnType.DateTime; } }
    }
    internal class StringColumnTo22 : StringColumn
    {
        internal StringColumnTo22( Table table, string name, int version ) : base( table, name, version )
        {
        }
        public override void LoadValue( SafeBinaryReader reader )
        {
            try
            {
                _fields[_fieldIndex] = reader.ReadStringSafe();
            }
            catch ( EndOfStreamException )
            {
                throw new StringCorruptedException( "There is end of stream while reading string" );
            }
        }
        public StringColumn AsStringColumn()
        {
            StringColumn column = new StringColumn( _table, Name, Version );
            column.SetIndex( Index, IndexNum );
            column.SetSharedFields( Fields, FieldIndex );
            return column;
        }
    }

    internal class StringColumn : Column
    {
        public const uint END_MARKER = 0xDEDAB0BA;
        internal StringColumn( Table table, string name, int version ) : base( table, name, version )
        {
        }
        public override Object Value
        {
            get
            {
                if ( _fields[_fieldIndex] == null )
                {
                    _fields[_fieldIndex] = string.Empty;
                }
                return _fields[_fieldIndex];
            }
            set
            {
                if ( value == null ) value = string.Empty;
                _fields[_fieldIndex] = value;
            }
        }

        public override void LoadValue( SafeBinaryReader reader )
        {
            int length = reader.ReadInt32();
            if ( length == 0 )
            {
                _fields[_fieldIndex] = string.Empty;
            }
            else
            {
                try
                {
                    _fields[_fieldIndex] = reader.ReadStringSafeWithoutLength( length );
                }
                catch ( EndOfStreamException )
                {
                    throw new StringCorruptedException();
                }
            }
            if ( reader.ReadUInt32() != END_MARKER )
            {
                throw new NoEndMarkerException( "String property has no end marker" );
            }
        }

        public override object SaveValue( SafeBinaryWriter writer )
        {
            string value = _fields[_fieldIndex] as string;
            if ( value == null )
            {
                value = string.Empty;
            }
            writer.WriteStringSafeWithIntLength( value );
            writer.Write( END_MARKER );
            return value;
        }
        public override FixedLengthKey GetFixedFactory()
        {
            return new FixedLengthKey_Int( 0 );
        }

        public override ColumnType Type{ get{ return ColumnType.String; } }
    }

    internal class DoubleColumn : Column
    {
        internal DoubleColumn( Table table, string name, int version ) :
            base( table, name, version ) {}

        public override Object Value
        {
            get
            {
                if ( _fields[_fieldIndex] == null )
                {
                    _fields[_fieldIndex] = 0.0;
                }
                return _fields[_fieldIndex];
            }
            set
            {
                if ( value == null ) value = 0.0;
                _fields[_fieldIndex] = (double)value;
            }
        }

        public override void LoadValue( SafeBinaryReader reader )
        {
            _fields[_fieldIndex] = reader.ReadDouble();
        }

        public override object SaveValue( SafeBinaryWriter writer )
        {
            object value = _fields[_fieldIndex];
            if ( value == null )
            {
                value = 0.0;
            }
            writer.Write( (double)value );
            return value;
        }
        public override FixedLengthKey GetFixedFactory()
        {
            return new FixedLengthKey_Double( 0.0 );
        }
        public override ColumnType Type{ get{ return ColumnType.Double; } }
    }

    internal class BLOBColumn : Column
    {
        internal BLOBColumn( Table table, string name, int version ) :
            base( table, name, version )
        {
        }

        public override Object Value
        {
            get
            {
                return _fields[_fieldIndex];
            }
            set
            {
                if ( value != null )
                {
                    _fields[_fieldIndex] = value;
                }
            }
        }
        public void DeleteBLOB()
        {
            ((IBLOB)_fields[_fieldIndex]).Delete();
        }

        public override void LoadValue( SafeBinaryReader reader )
        {
            string blobID = reader.ReadString();
            _fields[_fieldIndex] = new BLOB( _table, blobID );
        }

        public override object SaveValue( SafeBinaryWriter writer )
        {
            BLOB value = _fields[_fieldIndex] as BLOB;
            writer.Write( value.ID );
            return value;
        }
        public override FixedLengthKey GetFixedFactory()
        {
            return null;
        }

        public override ColumnType Type{ get{ return ColumnType.BLOB; } }
    }


    internal class IntColumn : Column
    {
        internal IntColumn( Table table, string name, int version ) :
            base( table, name, version ) {}

        public override Object Value
        {
            get
            {
                if ( _fields[_fieldIndex] == null )
                {
                    _fields[_fieldIndex] = 0;
                }
                return _fields[_fieldIndex];
            }
            set
            {
                if ( value == null ) value = 0;
                _fields[_fieldIndex] = (int)value;
            }
        }
        public override void LoadValue( SafeBinaryReader reader )
        {
            _fields[_fieldIndex] = IntInternalizer.Intern( reader.ReadInt32() );
        }

        public override object SaveValue( SafeBinaryWriter writer )
        {
            object value = _fields[_fieldIndex];
            int intValue = 0;
            if ( value != null )
            {
                intValue = (int)value;
            }
            writer.Write( intValue );
            return intValue;
        }
        public override FixedLengthKey GetFixedFactory()
        {
            return new FixedLengthKey_Int( 0 );
        }
        public override ColumnType Type{ get{ return ColumnType.Integer; } }
    }

    internal class BLOB: IBLOB
    {
        private Table _table;
        private BlobFileSystem _bfs;
        private string _id;

        internal BLOB( Table table, string id )
        {
            _table = table;
            _bfs = table.Database.BlobFS;
            _id = id;
        }

        internal BLOB( Table table, Stream source )
        {
            _table = table;
            _bfs = table.Database.BlobFS;
            int handle;
            _bfs.Lock();
            try
            {
                Stream stream = _bfs.AllocFile( out handle ).BaseStream;
                _id = handle.ToString();
                SetStream( source, stream );
            }
            finally
            {
                _bfs.UnLock();
            }
        }

        public void Delete()
        {
            int handle = Handle;
            if( handle > 0 )
            {
                _bfs.Lock();
                try
                {
                    _bfs.DeleteFile( handle );
                }
                finally
                {
                    _bfs.UnLock();
                }
            }
            else
            {
                IOTools.DeleteFile( GetFileName() );
            }
        }

        public void Set( Stream source )
        {
            _bfs.Lock();
            try
            {
                int handle = Handle;
                using( Stream target = _bfs.RewriteFile( handle ).BaseStream )
                {
                    SetStream( source, target );
                }
                _bfs.Flush();
            }
            finally
            {
                _bfs.UnLock();
            }
        }

        private void SetStream( Stream source, Stream target )
        {
            try
            {
                MemoryStream memStream = source as MemoryStream;
                if ( memStream != null )
                {
                    memStream.WriteTo( target );
                }
                else
                {
                    JetMemoryStream jmStream = source as JetMemoryStream;
                    if( jmStream != null )
                    {
                        jmStream.WriteTo( target );
                    }
                    else
                    {
                        try
                        {
                            if ( source.CanSeek )
                            {
                                source.Position = 0;
                            }
                            CopyStream( target, source, _bfs.GetRawBytes() );
                        }
                        finally
                        {
                            source.Close();
                        }
                    }
                }
            }
            finally
            {
                target.Close();
            }
        }

        internal string ID
        {
            get{ return _id; }
        }

        private string GetFileName()
        {
            return Path.Combine( _table.Database.Path, _id + ".blob" );
        }

        public Stream Stream
        {
            get
            {
                int handle = Handle;
                return ( handle > 0 ) ? (Stream) new BlobFSMemoryStream( handle, _bfs ) : File.Open( GetFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite );
            }
        }

        public override string ToString()
        {
            int handle = Handle;
            if( handle == 0 )
            {
                return Utils.StreamToString( File.Open( GetFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite ) );
            }
            _bfs.Lock();
            try
            {
                return Utils.StreamToString( _bfs.GetRawStream( handle ) );
            }
            catch
            {
                _bfs.RewriteFile( handle );
                _bfs.Flush();
                return string.Empty;
            }
            finally
            {
                _bfs.UnLock();
            }
        }

        private class BlobFSMemoryStream : JetMemoryStream
        {
            private int _handle;
            private BlobFileSystem _bfs;
            private bool _dirty;

            public BlobFSMemoryStream( int handle, BlobFileSystem bfs )
                : base( 1024 )
            {
                _handle = handle;
                _bfs = bfs;
                bfs.Lock();
                try
                {
                    CopyStream( this, bfs.GetRawStream( handle ), bfs.GetRawBytes() );
                }
                catch
                {
                    SetLength( 0 );
                    bfs.RewriteFile( handle );
                    bfs.Flush();
                }
                finally
                {
                    bfs.UnLock();
                }
                base.Position = 0;
                _dirty = false;
            }

            public override void Close()
            {
                if( _dirty )
                {
                    _bfs.Lock();
                    int handle = _handle;
                    try
                    {
                        using( Stream stream = _bfs.RewriteFile( handle ).BaseStream )
                        {
                            WriteTo( stream );
                        }
                        _bfs.Flush();
                    }
                    finally
                    {
                        _bfs.UnLock();
                    }
                }
                base.Close();
            }

            public override void WriteByte( byte value )
            {
                _dirty = true;
                base.WriteByte( value );
            }

            public override void Write( byte[] buffer, int offset, int count )
            {
                _dirty = true;
                base.Write( buffer, offset, count );
            }
        }

        private int Handle
        {
            get
            {
                int handle = 0;
                try
                {
                    handle = Int32.Parse( _id );
                }
                catch {}
                return handle;
            }
        }

        private static void CopyStream( Stream target, Stream source, byte[] buf )
        {
            int len;
            while( ( len = source.Read( buf, 0, buf.Length ) ) > 0 )
            {
                target.Write( buf, 0, len );
            }
        }
    }
}
