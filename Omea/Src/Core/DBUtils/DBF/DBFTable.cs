/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.IO;
using System.Text;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.Database.DBF
{
    public enum FieldType
    {
        AnsiString,
        UnicodeString,
        Numeric,
        Logical,
        Memo,
        UnicodeMemo,
        Blob,
        Date
    }       

    public struct FieldDescriptor
    {
        public string       _name;      // name
        public FieldType    _type;      // filed type
        public byte         _length;    // length of filed
    }

    /**
     * DBF format table
     */
    public class DBFTable : IDisposable
    {
        public DBFTable( string filename, bool readOnly, ICachingStrategy strategy )
        {
            _filename = filename;
            _stream = new CachedStream(
                ( readOnly ) ? IOTools.OpenRead( filename ) : IOTools.Open( filename ), strategy );
            _recordArray = null;
            CreateBinaryReader();
            LoadStructure();
        }

        public void Dispose()
        {
            if( _stream != null )
            {
                IOTools.CloseStream( _stream );
                _stream = null;
            }
            if( _memoStream != null )
            {
                IOTools.CloseStream( _memoStream );
                _memoStream = null;
            }
        }

        public bool CreateNew()
        {
            if( IsOk )
            {
                IOTools.CloseStream( _stream );
            }
            IOTools.DeleteFile( _filename );
            _stream = IOTools.CreateFile( _filename );
            CreateBinaryReader();
            return IsOk;
        }

        public string FileName
        {
            get { return _filename; }
        }

        public bool IsOk
        {
            get { return _stream != null; }
        }

        public int RecordCount
        {
            get { return _recordCount; }
        }

        public short RecordSize
        {
            get { return _recordSize; }
        }

        public int FieldCount
        {
            get { return _fieldCount; }
        }

        public FieldDescriptor[] Fields
        {
            get { return _fields; }
        }

        public ArrayList this[ int recordNumber ]
        {
            get { return GetRecord( recordNumber ); }
        }

        public ArrayList GetRecord( int recordNumber )
        {
            if( !ReadRawRecord( recordNumber ) || _rawRecord[ 0 ] == 0x2a )
            {
                return null;
            }

            ArrayList result = ( _recordArray == null ) ? new ArrayList() : _recordArray;
            result.Clear();

            int offset = 1;

            foreach( FieldDescriptor d in Fields )
            {
                object fieldValue = null;
                if( d._length > 0 )
                {
                    switch( d._type )
                    {
                        case FieldType.AnsiString:
                        {
                            fieldValue = Encoding.ASCII.GetString( _rawRecord, offset, d._length ).Trim( ' ', '\0' );
                            break;
                        }
                        case FieldType.UnicodeString:
                        {
                            fieldValue = Encoding.Unicode.GetString( _rawRecord, offset, d._length ).Trim( ' ', '\0' );
                            //offset += d._length; // extra shift for two-bytes chars field
                            break;
                        }
                        case FieldType.Logical:
                        {
                            bool value = _rawRecord[ offset ] == 'Y' || _rawRecord[ offset ] == 'y' ||
                                _rawRecord[ offset ] == 'T' || _rawRecord[ offset ] == 't';
                            fieldValue = value;
                            break;
                        }
                        case FieldType.Memo:
                        case FieldType.UnicodeMemo:
                        case FieldType.Blob:
                        {
                            int index = 0;
                            int shift = 0;
                            for( int i = 0; i < d._length; ++i )
                            {
                                int addend = _rawRecord[ offset + i ];
                                index += ( addend << shift );
                                shift += 8;
                            }
                            _memoStream.Position = index * _memoBlockSize;
                            // skip signature
                            _memoStream.ReadByte(); _memoStream.ReadByte(); _memoStream.ReadByte(); _memoStream.ReadByte(); 
                            int memoSize = 0;
                            for( int i = 0; i < 4; ++i )
                            {
                                memoSize <<= 8;
                                memoSize += _memoStream.ReadByte();
                            }
                            if( _memoBytes == null || _memoBytes.Length < memoSize )
                            {
                                _memoBytes = new byte[ memoSize ];
                            }
                            _memoStream.Read( _memoBytes,  0, memoSize );
                            switch( d._type )
                            {
                                case FieldType.UnicodeMemo:
                                {
                                    fieldValue = Encoding.Unicode.GetString( _memoBytes, 0, memoSize ).Trim( ' ', '\0' );
                                    break;
                                }
                                case FieldType.Memo:
                                {
                                    fieldValue = Encoding.ASCII.GetString( _memoBytes, 0, memoSize ).Trim( ' ', '\0' );
                                    break;
                                }
                                case FieldType.Blob:
                                {
                                    fieldValue = _memoBytes;
                                    for( int i = memoSize; i < _memoBytes.Length; ++i )
                                    {
                                        _memoBytes[ i ] = 0;
                                    }
                                    break;
                                }
                            }
                            break;
                        }
                        case FieldType.Numeric:
                        {
                            long number = 0;
                            int shift = 0;
                            for( int i = 0; i < d._length && i < 8; ++i )
                            {
                                long addend = _rawRecord[ offset + i ];
                                number += ( addend << shift );
                                shift += 8;
                            }
                            fieldValue = number;
                            break;
                        }
                    }
                    offset += d._length;
                }
                result.Add( fieldValue );
            }

            return result;
        }

        public void SetRecordArray( ArrayList array )
        {
            _recordArray = array;
        }

        private bool LoadStructure()
        {
            if( _stream == null )
            {
                return false;
            }

            /**
             * open memo file
             */
            string memoFileName = Path.Combine( Path.GetDirectoryName( _filename ),
                Path.GetFileNameWithoutExtension( _filename ) ) + ".fpt";
            _memoStream = IOTools.OpenRead( memoFileName );
            if( _memoStream != null )
            {
                BinaryReader memoReader = new BinaryReader( _memoStream );
                memoReader.ReadInt32(); // skip first free block
                memoReader.ReadInt16(); // skip two unused bytes
                _memoBlockSize = (ushort) ( ( memoReader.ReadByte() << 8 ) + memoReader.ReadByte() );
            }

            /**
             * check version of DBF file
             * 3 - ordinary
             * 48 -- ICQ2003b database
             * 0x83 - clipper-made
             * 0x8b - other
             */
            byte dbfVersion = _reader.ReadByte();
            if( dbfVersion != 3 && dbfVersion!= 48 && dbfVersion != 0x83 && dbfVersion != 0x08b )
            {
                Dispose();
                return false;
            }

            /**
             * skip last update date
             */
            _reader.ReadByte();
            _reader.ReadByte();
            _reader.ReadByte();

            _recordCount = _reader.ReadInt32();
            _headerSize = _reader.ReadInt16();
            _recordSize = _reader.ReadInt16();
            _rawRecord = new byte[ _recordSize ];
            _fieldCount = (short)( ( _headerSize - 33 ) >> 5 );

            /**
             * read field info
             */
            _fields = new FieldDescriptor[ _fieldCount ];
            for( short i = 0; i < _fieldCount; ++i )
            {
                _stream.Position = 32 + ( i * 32 );
                // field name
                string name = string.Empty;
                for( int j = 0; j < 11; ++j )
                {
                    char c = _reader.ReadChar();
                    if( c != '\0' )
                    {
                        name = name + c;
                    }
                }
                // field type
                _fields[ i ]._name = name;
                switch( _reader.ReadChar() )
                {
                    case 'C':
                    {
                        _fields[ i ]._type = FieldType.AnsiString;
                        break;
                    }
                    case 'W':
                    {
                        _fields[ i ]._type = FieldType.UnicodeString;
                        break;
                    }
                    case 'N':
                    {
                        _fields[ i ]._type = FieldType.Numeric;
                        break;
                    }
                    case 'L':
                    {
                        _fields[ i ]._type = FieldType.Logical;
                        break;
                    }
                    case 'M':
                    {
                        _fields[ i ]._type = FieldType.UnicodeMemo;
                        break;
                    }
                    case 'D':
                    {
                        _fields[ i ]._type = FieldType.Date;
                        break;
                    }
                }
                // skip one int
                _reader.ReadInt32();
                // field length
                _fields[ i ]._length = _reader.ReadByte();
            }

            return true;
        }

        private void CreateBinaryReader()
        {
            if( _stream != null )
            {
                _reader = new BinaryReader( _stream );
            }
        }

        private bool ReadRawRecord( int recordNumber )
        {
            _stream.Position = _headerSize + recordNumber * _recordSize;
            int recordSize = _rawRecord.Length;
            return _stream.Read( _rawRecord, 0, recordSize ) == recordSize;
        }

        private string              _filename;
        private Stream              _stream;
        private BinaryReader        _reader;
        private FieldDescriptor[]   _fields;
        private byte[]              _rawRecord;
        private int                 _recordCount;
        private short               _headerSize;
        private short               _recordSize;
        private short               _fieldCount;
        private FileStream          _memoStream;
        private ushort              _memoBlockSize;
        private ArrayList           _recordArray;
        private byte[]              _memoBytes;
    }
}