/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.Diagnostics;
using System.Threading;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Database
{
    internal enum RecordType
    {
        Normal = 0,
        Deleted = 1,
        Updated = 2,
        New = 3,
        NormalMarker = 'x',
        DeletedMarker = 'y',
        CorruptedMarker = 'ж',
    }

    internal class Table : ITable, ITableDesign
    {
        #region class members
        private Database _database;
        private HashMap _columnsMap = new HashMap();

        private ArrayList _indexes = new ArrayList();
        private ArrayList _compoundIndexes = new ArrayList();
        private ArrayList _compoundIndexesWithValue = new ArrayList();
        private IntHashTable _indexesCompoundMap = new IntHashTable();
        private IntHashTable _indexesCompoundMapWithValue = new IntHashTable();

        private CachedStream _file = null;
        private SafeBinaryReader _reader = null;
        private SafeBinaryWriter _writer = null;

        private int _lastCommited = -1;
     
        private TableStructure _tblStructure = null;
        private Tracer _tracer;
        private Column _idColumn = null;
        private Compound _compound = new Compound( null, null );
        private Compound _compoundSecond = new Compound( null, null );
        private CompoundAndValue _compoundAndValue = new CompoundAndValue( null, null, null );
        private CompoundAndValue _compoundAndValueSecond = new CompoundAndValue( null, null, null );
        private Column[] _columns = new Column[0];
        private object[] _fields = new object[0];
        private string _fileName;
        private bool _canUpdate = true;
        private bool _fixedSize = true;
        private bool _isRecordWithBLOB = false;
        private bool[] _fieldChangeFlag = new bool[0];
        private int _totalCount = -1;
        private int _sortedColumn = -1;
        private static ICountedResultSet _emptyResultSet =  new EmptyResultSet();
        
        private int _loadedRecords = 0, _savedRecords = 0;
        private long _loadedRecordSize = 0, _savedRecordSize = 0;
        private bool _autoFlush = true;

        #endregion

        internal Table( Database database, TableStructure tblStructure )
        {
            _database = database;
            _tblStructure = tblStructure;
            _totalCount = _tblStructure.TotalCount();
            OpenStreams();

            _tracer = new Tracer( "(DBUtils) Table - " + Name );
        }

        public void CheckTableLength()
        {
            if ( _fixedSize )
            {
                long remainder = _file.Length % _recordSize;
                if ( remainder != 0 )
                {
                    _file.Position = _file.Length;
                    byte[] padding = new byte[_recordSize-remainder];
                    _writer.Write( padding );
                    _writer.Flush();
                }
            }
        }
        public bool NeedUpgradeTo22 { get { return _checkedNeedUpgradeTo22 ? false : _database.NeedUpgradeTo22; } }
        private bool _checkedNeedUpgradeTo22 = false;
        private void CheckedNeedUpgradeTo22()
        {
            _checkedNeedUpgradeTo22 = true;
        }

        public bool AutoFlush
        {
            get { return _autoFlush; }
            set { _autoFlush = value; }
        }

        public void FlushData()
        {
            _writer.Flush();
        }

        public long TracePerformanceCounters()
        {
            Trace.WriteLine( Name + " performance counters: " );
            long result = _loadedRecordSize + _savedRecordSize;
            Trace.WriteLine( "    Loaded " + _loadedRecords + " records (" + Utils.SizeToString( _loadedRecordSize ) + ")" );
            Trace.WriteLine( "    Saved " + _savedRecords + " records (" + Utils.SizeToString( _savedRecordSize ) + ")" );

            foreach( IDBIndex dbIndex in _indexes )
            {
                if ( dbIndex != null )
                {
                    result += TraceIndexPerformanceCounters( dbIndex );
                }
            }
            foreach( CompoundIndex compoundIndex in _compoundIndexes )
            {
                if ( compoundIndex != null )
                {
                    result += TraceIndexPerformanceCounters( compoundIndex._dbIndex );
                }
            }
            foreach( CompoundIndexWithValue indexWithValue in _compoundIndexesWithValue )
            {
                if ( indexWithValue != null )
                {
                    result += TraceIndexPerformanceCounters( indexWithValue._dbIndex );
                }
            }
            return result;
        }

        private long TraceIndexPerformanceCounters( IDBIndex dbIndex )
        {
            long loadedBytes = dbIndex.LoadedPages * dbIndex.PageSize;
            Trace.WriteLine( "    " + dbIndex.Name + ": loaded " + dbIndex.LoadedPages + " pages (" + 
                Utils.SizeToString( loadedBytes ) + ")" );
            return loadedBytes;
        }

        #region Working with record
        private int Commit( int offset, object[] oldFields, object[] newFields )
        {
            bool fieldsChanged = false;
            if ( _canUpdate && newFields != null && offset != -1 )
            {
                for ( int i = 0; i < _fieldChangeFlag.Length; ++i )
                {
                    bool changed = !oldFields[i].Equals( newFields[i] );
                    if ( changed )
                    {
                        fieldsChanged = true;
                    }
                    _fieldChangeFlag[i] = changed;
                }
                if ( !fieldsChanged ) return offset;
            }

            _tblStructure.Dirty = true;
            if ( offset == -1 || !_canUpdate )
            {
                if ( _totalCount != -1 )
                {
                    _totalCount++;
                }
                _lastCommited = (int) (_file.Position = _file.Length );
            }
            else
            {
                CheckIfOffsetValid( offset, false );
                _file.Position = offset;
                _lastCommited = offset;
            }

            _writer.Write( (byte)RecordType.NormalMarker );
            int columnCount = _columns.Length;
            for ( int i = 0; i < columnCount; i++ )
            {
                Column column = _columns[i];
                Object value = column.SaveValue( _writer );
                object dbIndex = column.Index;
                if ( dbIndex != null )
                {
                    bool bAddToIndex = true;
                    if ( _canUpdate && offset != -1 && newFields != null )
                    {
                        if ( !fieldsChanged )
                        {
                            continue;
                        }

                        bAddToIndex = _fieldChangeFlag[i];
                    }
                    else if ( !_canUpdate )
                    {
                        string strValue = value as string;
                        if ( strValue != null )
                        {
                            value = DBHelper.GetHashCodeInLowerCase( strValue );
                        }
                    }

                    if ( bAddToIndex )
                    {
                        (dbIndex as IDBIndex).AddIndexEntry( (IComparable)value, _lastCommited );
                    }
                }
            }
            if ( _autoFlush )
            {
                _writer.Flush();
            }
            _savedRecords++;
            _savedRecordSize += _file.Position - _lastCommited;

            for ( int i = 0; i < _compoundIndexes.Count; i++ )
            {
                CompoundIndex compoundIndex = _compoundIndexes[i] as CompoundIndex;
                if ( compoundIndex != null )
                {
                    IDBIndex dbIndex = compoundIndex._dbIndex;
                    Column column1 = compoundIndex._column1;
                    Column column2 = compoundIndex._column2;
                    Object value1 = column1.Value;
                    Object value2 = column2.Value;

                    bool bAddToIndex = true;
                    if ( _canUpdate && offset != -1 && newFields != null )
                    {
                        bAddToIndex = _fieldChangeFlag[compoundIndex._columnIndex1];
                        if ( !bAddToIndex )
                        {
                            bAddToIndex = _fieldChangeFlag[compoundIndex._columnIndex2];
                        }
                    }
                    else if ( !_canUpdate )
                    {
                        string strValue1 = value1 as string;
                        if ( strValue1 != null )
                        {
                            value1 = DBHelper.GetHashCodeInLowerCase( strValue1 );
                        }
                        string strValue2 = value2 as string;
                        if ( strValue2 != null )
                        {
                            value2 = DBHelper.GetHashCodeInLowerCase( strValue2 );
                        }
                    }
                    if ( bAddToIndex )
                    {
                        SetCompoundKey( _compound, value1, value2 );
                        dbIndex.AddIndexEntry( _compound, _lastCommited );
                    }
                }
            }
            for ( int i = 0; i < _compoundIndexesWithValue.Count; i++ )
            {
                CompoundIndexWithValue compoundIndexWithValue = _compoundIndexesWithValue[i] as CompoundIndexWithValue;
                if ( compoundIndexWithValue != null )
                {
                    IDBIndex dbIndex = compoundIndexWithValue._dbIndex;
                    Column column1 = compoundIndexWithValue._column1;
                    Column column2 = compoundIndexWithValue._column2;
                    object value1 = column1.Value;
                    object value2 = column2.Value;
                    object value3 = compoundIndexWithValue._valueColumn.Value;

                    bool bAddToIndex = true;
                    if ( _canUpdate && offset != -1 && newFields != null )
                    {
                        bAddToIndex = _fieldChangeFlag[compoundIndexWithValue._columnIndex1];
                        if ( !bAddToIndex )
                        {
                            bAddToIndex = _fieldChangeFlag[compoundIndexWithValue._columnIndex2];
                        }
                        if ( !bAddToIndex )
                        {
                            bAddToIndex = _fieldChangeFlag[compoundIndexWithValue._fieldIndex];
                        }
                    }
                    else if ( !_canUpdate )
                    {
                        string strValue1 = value1 as string;
                        if ( strValue1 != null )
                        {
                            value1 = DBHelper.GetHashCodeInLowerCase( strValue1 );
                        }
                        string strValue2 = value2 as string;
                        if ( strValue2 != null )
                        {
                            value2 = DBHelper.GetHashCodeInLowerCase( strValue2 );
                        }
                    }
                    if ( bAddToIndex )
                    {
                        SetCompoundKey( _compoundAndValue, value1, value2, value3 );
                        dbIndex.AddIndexEntry( _compoundAndValue, _lastCommited );
                    }
                }
            }
            return _lastCommited;
        }

        public IRecord GetRecord( int offset )
        {
            Record rec = GetRecordImpl( offset );
            rec.SetFields( _fields.Clone() as object[] );
            return rec;
        }
        internal Record GetRecordImpl( int offset )
        {
            CheckIfOffsetValid( offset );
            return new Record( this, offset );
        }

        public int UpdateFields( Record record, object[] oldFields, object[] newFields, int offset, RecordType recordType )
        {
            CheckIfSingleOffsetValid( offset, true );
            if ( recordType == RecordType.Updated )
            {
                if ( _canUpdate )
                {
                    RemoveValuesFromIndex( offset, oldFields, newFields );
                }
                else
                {
                    DeleteRecord( offset, oldFields );
                }
            }
            for ( int i = 0; i < newFields.Length; i++ )
            {
                _fields[i] = newFields[i];
            }
            
            int newOffset;

            if ( _canUpdate )
            {
                newOffset = Commit( offset, oldFields, newFields );
            }
            else
            {
                newOffset = Commit( -1, oldFields, newFields );
            }
            return newOffset;
        }
        public object[] ReadRecordFromColumns( )
        {
            return _fields.Clone() as object[];
        }
        public object[] ReadRecordFromOffsetNoChecking( int offset )
        {
            CheckIfOffsetValid( offset, true );
            _file.Position = offset;

            long startOffset = _file.Position;
            _file.ReadByte();
            LoadRecordToColumns();
            _loadedRecords++;
            _loadedRecordSize += _file.Position - startOffset;
            return _fields.Clone() as object[];
        }

        private RecordType ReadRecord( bool fix )
        {
            long startOffset = _file.Position;
            RecordType recType = (RecordType)_file.ReadByte();
            try
            {
                bool success = LoadRecordToColumns( startOffset, fix );
                _loadedRecords++;
                _loadedRecordSize += _file.Position - startOffset;
                if ( !success )
                {
                    return RecordType.DeletedMarker;
                }
            }
            catch ( EndOfStreamException )
            {
                if ( fix )
                {
                    _file.SetLength( startOffset );
                    Console.WriteLine( "Table was truncated because last record is corrupted" );
                }
                return RecordType.DeletedMarker;
            }
            catch ( NoEndMarkerException )
            {
                return RecordType.DeletedMarker;
            }

            return recType;
        }

        /*
         * Method for regular reading of records. If there is data corruption then BadIndexesException is thrown.
         * */
        private void LoadRecordToColumns( ) 
        {
            for ( int i = 0; i < _columns.Length; ++i )
            {
                try
                {
                    _columns[i].LoadValue( _reader );
                }
                catch ( DateTimeCorruptedException exception )
                {
                    throw new BadIndexesException( "Table is corrupted.", exception );
                }
                catch ( NoEndMarkerException exception )
                {
                    throw new BadIndexesException( "Unexpected end of table. Table or indexes are corrupted.", exception );
                }
                catch ( EndOfStreamException exception )
                {
                    throw new BadIndexesException( "Unexpected end of table. Table or indexes are corrupted.", exception );
                }
                catch ( StringCorruptedException exception )
                {
                    throw new BadIndexesException( "Unexpected end of table. Table or indexes are corrupted.", exception );
                }
            }
        }
        private bool ReadToNearestEndMarker( long columnOffset )
        {
            try
            {
                if ( _file.Length <= columnOffset + 4 )
                {
                    return false;
                }

                _file.Position = columnOffset + 4;
                for ( ;; )
                {
                    byte readByte = (byte)_file.ReadByte();
                    if ( readByte == 0xDE )
                    {
                        if ( _file.Position >= 4 )
                        {
                            _file.Position -= 4;
                            if ( _reader.ReadUInt32() == StringColumn.END_MARKER )
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch ( EndOfStreamException )
            {
                return false;
            }
        }
        private void TryToFixEndMarker( long startRecordOffset, long startColumnOffset )
        {
            if ( _file.Length <= startColumnOffset + 4 )
            {
                return;
            }
            _file.Position = startColumnOffset + 4;
            try
            {
                for ( ;; )
                {
                    byte readByte = (byte)_file.ReadByte();
                    if ( readByte == 0xDE )
                    {
                        if ( _file.Position >= 4 )
                        {
                            _file.Position -= 4;
                                    
                            if ( _reader.ReadUInt32() == StringColumn.END_MARKER )
                            {
                                long endOffset = _file.Position;
                                _file.Position = startColumnOffset;
                                int stringLength = (int)( endOffset - startColumnOffset - 8 );
                                _writer.Write( stringLength );
                                _file.Position = startRecordOffset;
                                _file.ReadByte();
                                LoadRecordToColumns();
                                return;
                            }
                        }
                    }
                }
            }
            catch ( EndOfStreamException )
            {
                Console.WriteLine( "Table was truncated because last records are corrupted" );
                _file.SetLength( startRecordOffset );
            }
        }
        /*
         * Method for reading of records when rebuild and defragment db.
         * */
        private bool LoadRecordToColumns( long startRecordOffset, bool fix )
        {
            int columnsCount = _columns.Length;
            long columnOffset = 0;
            for ( int i = 0; i < columnsCount; ++i )
            {
                try
                {
                    columnOffset = _file.Position;
                    _columns[i].LoadValue( _reader );
                }
                catch ( DateTimeCorruptedException )
                {
                    if ( fix )
                    {
                        _file.Position -= 8;
                        _columns[i].SaveValue( _writer );
                    }
                }
                catch ( StringCorruptedException exception )
                {
                    _tracer.TraceException( exception );
                    if ( fix )
                    {
                        TryToFixEndMarker( startRecordOffset, columnOffset );
                    }
                    else
                    {
                        if ( ReadToNearestEndMarker( columnOffset ) )
                        {
                            i++;
                            for ( ;i < columnsCount; ++i )
                            {
                                _columns[i].LoadValue( _reader );
                            }
                        }
                    }
                    return false;
                }
                catch ( NoEndMarkerException noEndMarker )
                {
                    if ( !fix )
                    {
                        throw new BadIndexesException( "Table or indexes are corrupted: String corrupted no end marker in column = " + 
                            _columns[i].Name + " fix = " + fix, noEndMarker );
                    }
                    long currentOffset = _file.Position;
                    try
                    {
                        _file.ReadByte();
                        try
                        {
                            LoadRecordToColumns();
                        }
                        catch ( Exception )
                        {
                            TryToFixEndMarker( startRecordOffset, columnOffset );
                            return false;
                        }
                        _file.Position = currentOffset - 4;
                        _writer.Write( StringColumn.END_MARKER );
                        i++;
                        for ( ;i < columnsCount; ++i )
                        {
                            _columns[i].LoadValue( _reader );
                        }
                        _file.Position = startRecordOffset;
                        _file.ReadByte();
                        LoadRecordToColumns();
                        return false;
                    }
                    catch ( Exception )
                    {
                        _file.Position = currentOffset;
                        throw noEndMarker;
                    }
                }
                catch ( EndOfStreamException exception )
                {
                    _tracer.Trace( "Unexpected end of table. Table or indexes are corrupted." );
                    _tracer.TraceException( exception );
                    throw exception;
                }
            }
            return true;
        }
        private void CheckIfOffsetValid( int offset )
        {
            CheckIfSingleOffsetValid( offset, false );
        }
        private void CheckIfSingleOffsetValid( int offset, bool minusOneIsExpected )
        {
            if ( minusOneIsExpected && offset == -1 )
            {
                return;
            }
            if ( _fixedSize && offset != -1 )
            {
                if ( offset % _recordSize != 0 )
                {
                    throw new BadIndexesException( "An attempt was made to read from bad offset: " + offset + " When recordSize is " + _recordSize, null );
                }
            }
        }
        private void CheckIfOffsetValid( int offset, bool forReading )
        {
            if ( forReading )
            {
                if ( offset < 0 || _file.Length <= offset  )
                {
                    throw new BadIndexesException( "An attempt was made to move the file pointer before the beginning of the file: offset = " + offset, null );
                }
            }
            else
            {
                if ( offset < 0 || _file.Length < offset  )
                {
                    throw new BadIndexesException( "An attempt was made to move the file pointer before the beginning of the file: offset = " + offset, null );
                }
            }
            CheckIfSingleOffsetValid( offset, !forReading );
        }
        public object[] ReadRecordFromOffset( int offset )
        {
            CheckIfOffsetValid( offset, true );
            _file.Position = offset;

            RecordType recType = (RecordType)_file.ReadByte();
            if ( recType != RecordType.NormalMarker )
            {
                throw new BadIndexesException( "Index points to not 'Normal' record. RecordType was = " + recType + " by offset = " + offset, null );
            }

            LoadRecordToColumns();
            _loadedRecords++;
            _loadedRecordSize += _file.Position - offset;
            return _fields;
        }

        public int NextID()
        {
            lock( this )
            {
                return _tblStructure.NextID();
            }
        }

        public int PeekNextID()
        {
            lock( this )
            {
                return _tblStructure.PeekNextID();
            }
        }

        public IRecord NewRecord( )
        {
            Record record = new Record( this, -1 );
            if ( _idColumn != null )
            {
                int id = NextID();
                _idColumn.Value = id;
                record.SetValue( 0, IntInternalizer.Intern( id ) );
            }
            return record;
        }

        public void DeleteRecord( int offset, object[] oldFields )
        {
            CheckIfOffsetValid( offset, false );

            _file.Position = offset;
            RecordType recType = (RecordType)_file.ReadByte();
            if ( recType != RecordType.NormalMarker )
            {
                throw new BadIndexesException( "Attemption to delete wrong record.", null );
            }

            RemoveValuesFromIndex( offset, oldFields, null );
            _tblStructure.Dirty = true;
            _file.Position = offset;

            _writer.Write( (byte)RecordType.DeletedMarker );
            if ( _autoFlush )
            {
                _writer.Flush();
            }
        }

        private void RemoveValuesFromIndex( int offset, object[] oldFields, object[] newFields )
        {
            bool fieldsChanged = false;
            if ( _canUpdate && newFields != null )
            {
                for ( int i = 0; i < _fieldChangeFlag.Length; i++ )
                {
                    bool changed = !oldFields[i].Equals( newFields[i] );
                    if ( changed )
                    {
                        fieldsChanged = true;
                    }
                    _fieldChangeFlag[i] = changed;
                }
                if ( !fieldsChanged ) return;
            }

            for ( int i = 0; i < _indexes.Count; i++ )
            {
                IDBIndex dbIndex = _indexes[i] as IDBIndex;
                if ( dbIndex != null )
                {
                    object oldValue = oldFields[i];
                    bool bRemoveFromIndex = true;
                    if ( _canUpdate && newFields != null )
                    {
                        bRemoveFromIndex = _fieldChangeFlag[i];
                    }
                    else if ( !_canUpdate )
                    {
                        string strValue = oldValue as string;
                        if ( strValue != null )
                        {
                            oldValue = DBHelper.GetHashCodeInLowerCase( strValue );
                        }
                    }

                    if ( bRemoveFromIndex )
                    {
                        dbIndex.RemoveIndexEntry( (IComparable)oldValue, offset );
                    }
                }
            }
            for ( int i = 0; i < _compoundIndexes.Count; i++ )
            {
                CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexes[i];
                if ( compoundIndex != null )
                {
                    IDBIndex dbIndex = compoundIndex._dbIndex;

                    object oldValue1 = oldFields[compoundIndex._columnIndex1];
                    object oldValue2 = oldFields[ compoundIndex._columnIndex2 ];

                    bool bRemoveFromIndex = true;
                    if ( _canUpdate && newFields != null )
                    {
                        bRemoveFromIndex = _fieldChangeFlag[compoundIndex._columnIndex1];
                        if ( !bRemoveFromIndex )
                        {
                            bRemoveFromIndex = _fieldChangeFlag[compoundIndex._columnIndex2];
                        }
                    }
                    else if ( !_canUpdate )
                    {
                        string strValue1 = oldValue1 as string;
                        if ( strValue1 != null )
                        {
                            oldValue1 = DBHelper.GetHashCodeInLowerCase( strValue1 );
                        }
                        string strValue2 = oldValue2 as string;
                        if ( strValue2 != null )
                        {
                            oldValue2 = DBHelper.GetHashCodeInLowerCase( strValue2 );
                        }
                    }
                    
                    if ( bRemoveFromIndex )
                    {
                        SetCompoundKey( _compound, oldValue1, oldValue2 );
                        dbIndex.RemoveIndexEntry( _compound, offset );
                    }
                }
            }
            for ( int i = 0; i < _compoundIndexesWithValue.Count; i++ )
            {
                CompoundIndexWithValue compoundIndexWithValue = (CompoundIndexWithValue)_compoundIndexesWithValue[i];
                if ( compoundIndexWithValue != null )
                {
                    IDBIndex dbIndex = compoundIndexWithValue._dbIndex;
                    object oldValue1 = oldFields[compoundIndexWithValue._columnIndex1];
                    object oldValue2 = oldFields[compoundIndexWithValue._columnIndex2];
                    object oldValue3 = oldFields[compoundIndexWithValue._fieldIndex];

                    bool bRemoveFromIndex = true;
                    if ( _canUpdate && newFields != null )
                    {
                        bRemoveFromIndex = _fieldChangeFlag[compoundIndexWithValue._columnIndex1];
                        if ( !bRemoveFromIndex )
                        {
                            bRemoveFromIndex = _fieldChangeFlag[compoundIndexWithValue._columnIndex2];
                        }
                        if ( !bRemoveFromIndex )
                        {
                            bRemoveFromIndex = _fieldChangeFlag[compoundIndexWithValue._fieldIndex];
                        }
                    }
                    else if ( !_canUpdate )
                    {
                        string strValue1 = oldValue1 as string;
                        if ( strValue1 != null )
                        {
                            oldValue1 = DBHelper.GetHashCodeInLowerCase( strValue1 );
                        }
                        string strValue2 = oldValue2 as string;
                        if ( strValue2 != null )
                        {
                            oldValue2 = DBHelper.GetHashCodeInLowerCase( strValue2 );
                        }
                    }

                    if ( bRemoveFromIndex )
                    {
                        SetCompoundKey( _compoundAndValue, oldValue1, oldValue2, oldValue3 );
                        dbIndex.RemoveIndexEntry( _compoundAndValue, offset );
                    }
                }
            }
        }

        #endregion

        #region Constructing table ( columns, indexes )

        private void OpenStreams()
        {
            _fileName = 
                DBHelper.GetFullNameForTable( _database.Path, _database.Name, Name );

            _file = DBHelper.PrepareIOFile( this, _fileName, FileMode.OpenOrCreate );
            
            // BinaryWriter has UTF-8 as the default encoding, but it sets
            // throwOnInvalidBytes to true, which we don't want (we can receive incorrect
            // surrogate pairs from high upstream, and there is nothing we can do about that
            // on the DB level)
            // see, for example, #4742
            UTF8Encoding encoding = new UTF8Encoding();
            _reader = new SafeBinaryReader( _file, encoding );
            _writer = new SafeBinaryWriter( _file, encoding );
        }

        private int _recordSize = 1;

        public int AddColumn( Column column )
        {
            if ( column.Name == "Id" )
            {
                _idColumn = column;
            }

            if ( column.Type == ColumnType.BLOB )
            {
                _isRecordWithBLOB = true;
            }

            if ( column.Type == ColumnType.String || _isRecordWithBLOB )
            {
                _canUpdate = false;
                _fixedSize = false;
            }

            if ( column.GetFixedFactory() != null )
            {
                _recordSize += column.GetFixedFactory().KeySize;
            }

            int columnIndex = _columns.Length;
            Column[] newColumns = new Column[ columnIndex + 1 ];
            object[] newFields = new object[ columnIndex + 1 ];
            _fieldChangeFlag = new bool[columnIndex + 1];
            for ( int i = 0; i < columnIndex; ++i )
            {
                newColumns[i] = _columns[i];
                newFields[i] = _fields[i];
            }
            newColumns[columnIndex] = column;
            newFields[columnIndex] = null;
            _columns = newColumns;
            _fields = newFields;

            for ( int i = 0; i < _columns.Length; ++i )
            {
                _columns[i].SetSharedFields( _fields, i );
                _fieldChangeFlag[i] = false;
            }

            _indexes.Add( null );
            _compoundIndexes.Add( null );
            _compoundIndexesWithValue.Add( null );
            //_fieldChangeFlag.Add( false );

            _columnsMap[column.Name] = columnIndex;
            return columnIndex;
        }
        internal void DropIndex( string columnName )
        {
            _tracer.Trace( "Drop index: " + columnName );
            int columnIndex = GetColumnIndexByName( columnName );
            IDBIndex dbIndex = _indexes[columnIndex] as IDBIndex;
            if ( dbIndex != null )
            {
                _indexes[columnIndex] = null;
                dbIndex.Close();
                return;
            }
            throw new IndexDoesNotExistException( columnName );
        }
        internal void DropCompoundIndex( string indexName )
        {
            string[] names = indexName.Split( '#' );
            if ( names.Length == 2 )
            {
                int firstIndex = GetColumnIndexByName( names[0] );
                int secondIndex = GetColumnIndexByName( names[1] );
                CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexes[firstIndex];
                compoundIndex._dbIndex.Close();
                _indexesCompoundMap.Remove( (firstIndex<< 6)+secondIndex );
                _compoundIndexes[firstIndex] = null;
            }
            CheckCountForIndexes();
        }
        internal void DropCompoundIndexWithValue( string indexName )
        {
            string[] names = indexName.Split( '#' );
            if ( names.Length == 2 )
            {
                int firstIndex = GetColumnIndexByName( names[0] );
                int secondIndex = GetColumnIndexByName( names[1] );
                CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexesWithValue[firstIndex];
                compoundIndex._dbIndex.Close();
                _indexesCompoundMapWithValue.Remove( (firstIndex<< 6)+secondIndex );
                _compoundIndexesWithValue[firstIndex] = null;
            }
            CheckCountForIndexes();
        }

        public void AddIndex( string indexName, IDBIndex dbIndex )
        {
            Column column = GetColumnByName( indexName );
            if ( column == null )
            {
                throw new ColumnDoesNotExistException( "Column does not exist", indexName );
            }

            _indexes.Insert( GetColumnIndexByName( indexName ), dbIndex );
            column.SetIndex( dbIndex, GetColumnIndexByName( indexName ) );
        }

        public void AddCompoundIndex( string indexName, IDBIndex dbIndex )
        {
            string[] names = indexName.Split( '#' );
            if ( names.Length == 2 )
            {
                int firstIndex = GetColumnIndexByName( names[0] );
                int secondIndex = GetColumnIndexByName( names[1] );
                _indexesCompoundMap[(firstIndex<< 6)+secondIndex] = dbIndex;
                _compoundIndexes.Insert( GetColumnIndexByName( dbIndex.FirstCompoundName ), 
                    new CompoundIndex( dbIndex, GetColumnByName(names[0]), firstIndex, GetColumnByName(names[1]), secondIndex ) );
            }

        }
        public void AddCompoundIndexWithValue( string indexName, IDBIndex dbIndex, string columnName )
        {
            string[] names = indexName.Split( '#' );
            if ( names.Length == 2 )
            {
                int firstIndex = GetColumnIndexByName( names[0] );
                int secondIndex = GetColumnIndexByName( names[1] );

                CompoundIndexWithValue compoundIndexWithValue = 
                    new CompoundIndexWithValue( dbIndex, GetColumnByName(names[0]), firstIndex, GetColumnByName(names[1]), 
                    secondIndex, GetColumnByName(columnName), GetColumnIndexByName(columnName));
                _indexesCompoundMapWithValue[(firstIndex<< 6)+secondIndex] = compoundIndexWithValue;
                _compoundIndexesWithValue.Insert( firstIndex, compoundIndexWithValue );
            }
        }
		
        public ArrayList GetColumnInfos()
        {
            int columnsCount = _columns.Length;
            ArrayList columnInfos = new ArrayList( columnsCount );
            for( int i = 0; i < columnsCount; i++ )
            {
                Column column = _columns[i];
                columnInfos.Add( new ColumnInfo( column.Name, column.Type ) );
            }
            return columnInfos;
        }

        public Column GetColumn( string name )
        {
            Column column = GetColumnByName(name);
            if ( column != null )
            {
                return column;
            }
            throw new ColumnDoesNotExistException( "Column does not exist", name );
        }
        #endregion
        #region loading and shutting down for table
        private void ShutdownIndexes( )
        {
            for ( int i = 0; i < _indexes.Count; i++ )
            {
                IDBIndex dbIndex = _indexes[i] as IDBIndex;
                if ( dbIndex != null ) dbIndex.Shutdown();
            }
            for ( int i = 0; i < _compoundIndexes.Count; i++ )
            {
                CompoundIndex compoundIndex = _compoundIndexes[i] as CompoundIndex;
                if ( compoundIndex != null )
                {
                    compoundIndex._dbIndex.Shutdown();
                }
            }
            for ( int i = 0; i < _compoundIndexesWithValue.Count; i++ )
            {
                CompoundIndex compoundIndex = _compoundIndexesWithValue[i] as CompoundIndex;
                if ( compoundIndex != null )
                {
                    compoundIndex._dbIndex.Shutdown();
                }
            }
        }
        private void FlushIndexes( )
        {
            for ( int i = 0; i < _indexes.Count; i++ )
            {
                IDBIndex dbIndex = _indexes[i] as IDBIndex;
                if ( dbIndex != null ) dbIndex.Flush();
            }
            for ( int i = 0; i < _compoundIndexes.Count; i++ )
            {
                CompoundIndex compoundIndex = _compoundIndexes[i] as CompoundIndex;
                if ( compoundIndex != null )
                {
                    compoundIndex._dbIndex.Flush();
                }
            }
            for ( int i = 0; i < _compoundIndexesWithValue.Count; i++ )
            {
                CompoundIndex compoundIndex = _compoundIndexesWithValue[i] as CompoundIndex;
                if ( compoundIndex != null )
                {
                    compoundIndex._dbIndex.Flush();
                }
            }
        }

        public void Flush()
        {
            Monitor.Enter( this );
            _tblStructure.SetTotalCount( _totalCount );
            FlushIndexes( );
            _tblStructure.Dirty = false;
            Monitor.Exit( this );
        }

        private void TableShutdown()
        {
            _writer.Close();
            _file.Close();
        }

        internal void Shutdown()
        {
            lock ( this )
            {
                Flush();
                _writer.Flush();
                TableShutdown();
                ShutdownIndexes( );
            }
        }

        #endregion
        #region Creating ResultSets
        public IRecord GetRecordByEqual( int columnIndex, object key )
        {
            if ( key == null ) return null;

            IResultSet resultSet = null;
            IRecord record = null;
            try
            {
                resultSet = CreateResultSet( columnIndex, key, true );
                IEnumerator enumerator = resultSet.GetEnumerator();
                try
                {
                    if ( enumerator.MoveNext() )
                    {
                        record = (IRecord) enumerator.Current;
                    }
                }
                finally
                {
                    IDisposable disposable = enumerator as IDisposable;
                    if ( disposable != null )
                    {
                        disposable.Dispose();
                    }
                }
            }
            finally
            {
                if ( resultSet != null ) resultSet.Dispose();
            }
            return record;
        }

        private void SetCompoundKey( Compound compound, object key1, object key2 )
        {
            compound._key1 = key1 as IComparable;
            compound._key2 = key2 as IComparable;
        }
        private void SetCompoundKey( CompoundAndValue compoundAndValue, object key1, object key2, object valueKey )
        {
            compoundAndValue._key1 = key1 as IComparable;
            compoundAndValue._key2 = key2 as IComparable;
            compoundAndValue._value = valueKey as IComparable;
        }
        public ICountedResultSet EmptyResultSet
        {
            get { return _emptyResultSet; }
        }

        private ArrayList _keys_offsets = new ArrayList();
        void PrepareKeysOffset()
        {
            _keys_offsets = new ArrayList();
        }
        private ArrayList GetKeysOffset()
        {
            return _keys_offsets;
        }
        public ICountedResultSet CreateResultSet(
            int columnIndex1, object key1, int columnIndex2, object key2, bool readOnly )
        {
            int stringColumnIndex = -1;
            string stringParam = null;

            if ( !_canUpdate )
            {
                string strKey1 = key1 as string;
                if ( strKey1 != null )
                {
                    stringParam = strKey1;
                    key1 = DBHelper.GetHashCodeInLowerCase( strKey1 );
                    stringColumnIndex = columnIndex1;
                }
                string strKey2 = key2 as string;
                if ( strKey2 != null )
                {
                    stringParam = strKey2;
                    key2 = DBHelper.GetHashCodeInLowerCase( strKey2 );
                    stringColumnIndex = columnIndex2;
                }
            }
            Monitor.Enter( this );
            try
            {
                object compoundIndexWithValue = _indexesCompoundMapWithValue[(columnIndex1 << 6)+columnIndex2];
                if ( compoundIndexWithValue != null )
                {
                    CompoundIndexWithValue compoundIndexWV = compoundIndexWithValue as CompoundIndexWithValue;
                    IDBIndex dbIndex = compoundIndexWV._dbIndex;
                    FixedLengthKey valueKey = dbIndex.SearchKeyValue();
                    SetCompoundKey( _compoundAndValue, key1, key2, valueKey.MinKey );
                    SetCompoundKey( _compoundAndValueSecond, key1, key2, valueKey.MaxKey );
                    IEnumerable enumerable;
                    if( readOnly )
                    {
                        enumerable = dbIndex.SearchForRange( _compoundAndValue, _compoundAndValueSecond );
                    }
                    else
                    {
                        ArrayList keys_offsets = GetKeysOffset();
                        dbIndex.SearchForRange( keys_offsets, _compoundAndValue, _compoundAndValueSecond );
                        if( keys_offsets.Count == 0 )
                        {
                            Monitor.Exit( this );
                            return _emptyResultSet;
                        }
                        PrepareKeysOffset();
                        enumerable = keys_offsets;
                    }
                    ICountedResultSet retResultSet = new FromIndexWithValueResultSet(
                        this, enumerable, compoundIndexWV._columnIndex1, compoundIndexWV._columnIndex2, compoundIndexWV._fieldIndex );
                    if ( stringColumnIndex > -1 )
                    {
                        retResultSet = new PreLoadedResultSet( retResultSet, stringColumnIndex, stringParam );
                    }
                    return retResultSet;
                }
                object compoundIndex = _indexesCompoundMap[(columnIndex1 << 6)+columnIndex2];
                if ( compoundIndex != null )
                {
                    IDBIndex dbIndex = (IDBIndex)compoundIndex;
                    SetCompoundKey( _compound, key1, key2 );
                    ICountedResultSet retResultSet;
                    if( readOnly )
                    {
                        retResultSet = new EnumerableResultSet( this, dbIndex.SearchForRange( _compound, _compound  ) );
                    }
                    else
                    {
                        IntArrayList offsets = IntArrayListPool.Alloc();
                        dbIndex.SearchForRange( offsets, _compound, _compound );
                        retResultSet = new ResultSet( this, offsets );
                    }
                    if ( stringColumnIndex > -1 )
                    {
                        retResultSet = new PreLoadedResultSet( retResultSet, stringColumnIndex, stringParam );
                    }
                    return retResultSet;
                }
                throw new Exception( "No index" );
            }
            catch ( Exception exception )
            {
                Tracer._TraceException( exception );
                Monitor.Exit( this );
                throw;
            }
        }

        public ICountedResultSet CreateModifiableResultSet( int columnIndex, object key )
        {
            return CreateResultSet( columnIndex, key, false );
        }

        public IResultSet CreateResultSet( int columnIndex, object key )
        {
            return CreateResultSet( columnIndex, key, true );
        }

        private ICountedResultSet CreateResultSet( int columnIndex, object key, bool readOnly )
        {
            int stringColumnhIndex = -1;
            string stringParam = null;
            if ( key == null )
            {
                return _emptyResultSet;
            }
            if ( !_canUpdate )
            {
                string strKey = key as string;
                if ( strKey != null )
                {
                    stringParam = strKey;
                    key = DBHelper.GetHashCodeInLowerCase( strKey );
                    stringColumnhIndex = columnIndex;
                }
            }
            if ( columnIndex < _columns.Length )
            {
                Monitor.Enter( this );
                ICountedResultSet retResultSet = PrepareResultSet( columnIndex, key, readOnly );
                if ( stringColumnhIndex > -1 )
                {
                    retResultSet = new PreLoadedResultSet( retResultSet, stringColumnhIndex, stringParam );
                }
                return retResultSet;
            }
            throw new ColumnDoesNotExistException( "Column does not exist", columnIndex.ToString() );
        }

        public void GetAllOffsets( IntArrayList offsets )
        {
            Monitor.Enter( this );
            try
            {
                for ( int i = 0; i < _indexes.Count; ++i )
                {
                    IDBIndex dbIndex = _indexes[i] as IDBIndex;
                    if ( dbIndex != null )
                    {
                        dbIndex.GetAllOffsets( offsets );
                        return;
                    }
                }
                for ( int i = 0; i < _compoundIndexes.Count; ++i )
                {
                    CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexes[i];
                    if ( compoundIndex != null )
                    {
                        compoundIndex._dbIndex.GetAllOffsets( offsets );
                        return;
                    }
                }
                for ( int i = 0; i < _compoundIndexesWithValue.Count; ++i )
                {
                    CompoundIndexWithValue compoundIndex = (CompoundIndexWithValue)_compoundIndexes[i];
                    if ( compoundIndex != null )
                    {
                        compoundIndex._dbIndex.GetAllOffsets( offsets );
                        return;
                    }
                }
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
                Monitor.Exit( this );
            }
        }

        public ICountedResultSet CreateResultSet( int columnIndex )
        {
            Monitor.Enter( this );
            try
            {
                bool isCompound;
                CompoundIndexWithValue withValue;            
                IDBIndex dbIndex = GetIndex( columnIndex, out isCompound, out withValue );
                if( dbIndex != null )
                {
                    if( withValue == null )
                    {
                        IntArrayList offsets = IntArrayListPool.Alloc();
                        dbIndex.GetAllOffsets( offsets );
                        if ( offsets.Count == 0 )
                        {
                            IntArrayListPool.Dispose( offsets );
                            Monitor.Exit( this );
                            return _emptyResultSet;                        
                        }
                        return new ResultSet( this, offsets );
                    }
                    else
                    {
                        ArrayList keys_offsets = GetKeysOffset();
                        dbIndex.GetAllOffsets( keys_offsets );
                        PrepareKeysOffset();
                        return new FromIndexWithValueResultSet(
                            this, keys_offsets, withValue._columnIndex1, withValue._columnIndex2, withValue._fieldIndex );
                    }
                }
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
                Monitor.Exit( this );
            }
            throw new ColumnHasNoIndexException( "Column '" + columnIndex.ToString() + "' has not index" );//no appropriate index
        }

        public ICountedResultSet CreateResultSetForRange( int firstColumnIndex, object firstKey, 
            int secondColumnIndex, object beginKey, object endKey )
        {
            int stringColumnIndex = -1;
            string stringParam = null;
            if ( !_canUpdate )
            {
                string strFirstKey = firstKey as string;
                if ( strFirstKey != null )
                {
                    stringParam = strFirstKey;
                    firstKey = DBHelper.GetHashCodeInLowerCase( strFirstKey );
                    stringColumnIndex = firstColumnIndex;
                }
            }

            Monitor.Enter( this );
            try
            {
                object objCompoundIndexWithValue = _indexesCompoundMapWithValue[(firstColumnIndex << 6)+secondColumnIndex];

                if ( objCompoundIndexWithValue != null )
                {
                    CompoundIndexWithValue compoundIndexWV = (CompoundIndexWithValue)objCompoundIndexWithValue;
                    IDBIndex compoundIndex = compoundIndexWV._dbIndex;
                    FixedLengthKey valueKey = compoundIndex.SearchKeyValue();
                    SetCompoundKey( _compoundAndValue, firstKey, beginKey, valueKey.MinKey );
                    SetCompoundKey( _compoundAndValueSecond, firstKey, endKey, valueKey.MaxKey );
                    ICountedResultSet retResultSet = new FromIndexWithValueResultSet(
                        this, compoundIndex.SearchForRange( _compoundAndValue, _compoundAndValueSecond ),
                        compoundIndexWV._columnIndex1, compoundIndexWV._columnIndex2, compoundIndexWV._fieldIndex );
                    if ( stringColumnIndex > -1 )
                    {
                        retResultSet = new PreLoadedResultSet( retResultSet, stringColumnIndex, stringParam );
                    }
                    return retResultSet;
                }

                object objCompoundIndex = _indexesCompoundMap[(firstColumnIndex << 6)+secondColumnIndex];
                if ( objCompoundIndex != null )
                {
                    IDBIndex compoundIndex = objCompoundIndex as IDBIndex;
                    SetCompoundKey( _compound, firstKey, beginKey );
                    SetCompoundKey( _compoundSecond, firstKey, endKey );
                    ICountedResultSet retResultSet = new EnumerableResultSet( this, compoundIndex.SearchForRange( _compound, _compoundSecond ) );
                    if ( stringColumnIndex > -1 )
                    {
                        retResultSet = new PreLoadedResultSet( retResultSet, stringColumnIndex, stringParam );
                    }
                    return retResultSet;
                }
                throw new Exception( "No appropriate index" );
            }
            catch ( Exception exception )
            {
                Tracer._TraceException( exception );
                Monitor.Exit( this );
                throw;
            }
        }

        private ICountedResultSet PrepareResultSet( int fieldIndex, object key, bool readOnly )
        {
            bool isCompound;
            CompoundIndexWithValue withValue;
            IDBIndex index = GetIndex( fieldIndex, out isCompound, out withValue );
            if ( withValue == null )
            {
                if ( readOnly )
                {
                    if ( isCompound )
                    {
                        FixedLengthKey sKey = index.SearchKeySecond();
                        SetCompoundKey( _compound, key, sKey.MinKey );
                        SetCompoundKey( _compoundSecond, key, sKey.MaxKey );
                        return new EnumerableResultSet( this, index.SearchForRange( _compound, _compoundSecond ) );
                    }
                    else
                    {
                        IComparable comparableKey = key as IComparable;
                        return new EnumerableResultSet( this, index.SearchForRange( comparableKey, comparableKey ) );
                    }
                }
                IntArrayList offsets = IntArrayListPool.Alloc();
                if ( isCompound )
                {
                    FixedLengthKey sKey = index.SearchKeySecond();
                    SetCompoundKey( _compound, key, sKey.MinKey );
                    SetCompoundKey( _compoundSecond, key, sKey.MaxKey );
                    index.SearchForRange( offsets, _compound, _compoundSecond );
                }
                else
                {
                    index.SearchForRange( offsets, key as IComparable, key as IComparable );
                }
                if ( offsets.Count == 0 )
                {
                    IntArrayListPool.Dispose( offsets );
                    Monitor.Exit( this );
                    return _emptyResultSet;                        
                }
                return new ResultSet( this, offsets );
            }
            else
            {
                FixedLengthKey sKey = index.SearchKeySecond();
                FixedLengthKey valueKey = index.SearchKeyValue();
                SetCompoundKey( _compoundAndValue, key, sKey.MinKey, valueKey.MinKey );
                SetCompoundKey( _compoundAndValueSecond, key, sKey.MaxKey, valueKey.MaxKey );
                IEnumerable enumerable;
                if( readOnly )
                {
                    enumerable = index.SearchForRange( _compoundAndValue, _compoundAndValueSecond );
                }
                else
                {
                    ArrayList keys_offsets = GetKeysOffset();
                    index.SearchForRange( keys_offsets, _compoundAndValue, _compoundAndValueSecond );
                    if( keys_offsets.Count == 0 )
                    {
                        Monitor.Exit( this );
                        return _emptyResultSet;
                    }
                    PrepareKeysOffset();
                    enumerable = keys_offsets;
                }
                return new FromIndexWithValueResultSet( this, enumerable, withValue._columnIndex1, withValue._columnIndex2, withValue._fieldIndex );
            }
        }
        #endregion        

        #region Working with BLOBs
        public IBLOB CreateBLOB( Stream stream )
        {
            return new BLOB( this, stream );
        }

        #endregion
        #region Getters
        public int GetID()
        {
            if ( _idColumn == null ) return 0;
            return (int)_idColumn.Value;
        }

        private Column GetColumnByName( string coloumnName )
        {
            object obj = _columnsMap[coloumnName];
            if ( obj == null ) return null;
            int columnIndex = (int)obj;
            return _columns[columnIndex];
        }
        public int GetColumnIndexByName( string columnName )
        {
            return (int)_columnsMap[columnName];
        }

        public bool IsRecordWithBLOB
        {
            get { return _isRecordWithBLOB; }
        }
        public DatabaseMode Mode { get { return _tblStructure.Mode; } }

        public int Version { get { return _database.Version; } }

        public int Count 
        { 
            get 
            { 
                for ( int i = 0; i < _indexes.Count; i++ )
                {
                    IDBIndex dbIndex = _indexes[i] as IDBIndex;
                    if ( dbIndex != null )
                    {
                        return dbIndex.Count;
                    }
                }

                for ( int i = 0; i < _compoundIndexes.Count;i++ )
                {
                    CompoundIndex compoundIndex = _compoundIndexes[i] as CompoundIndex;
                    if ( compoundIndex != null )
                    {
                        return compoundIndex._dbIndex.Count;
                    }
                }
                for ( int i = 0; i < _compoundIndexesWithValue.Count;i++ )
                {
                    CompoundIndex compoundIndex = _compoundIndexesWithValue[i] as CompoundIndex;
                    if ( compoundIndex != null )
                    {
                        return compoundIndex._dbIndex.Count;
                    }
                }
                throw new Exception( "Cannot access count for tables with no indexes" );
            }
        }

        public string Name { get { return _tblStructure.Name; } }
        public IDatabaseDesign Database { get{ return _database; } }
        public bool Dirty { get { return _tblStructure.Dirty; } }

        public IDBIndex GetIndex( int indexNum, out bool isCompound, out CompoundIndexWithValue withValue )
        {
            IDBIndex dbIndex;
            withValue = null;
            dbIndex = _indexes[indexNum] as IDBIndex;
            if ( dbIndex != null )
            {
                isCompound = false;
                return dbIndex;
            }
            withValue = _compoundIndexesWithValue[indexNum] as CompoundIndexWithValue;
            if ( withValue != null )
            {
                dbIndex = withValue._dbIndex;
                if ( dbIndex != null )
                {
                    isCompound = true;
                    return dbIndex;
                }
            }
            CompoundIndex compoundIndex = _compoundIndexes[indexNum] as CompoundIndex;
            if ( compoundIndex != null )
            {
                isCompound = true;
                return compoundIndex._dbIndex;
            }
            throw new ColumnHasNoIndexException( "Column '" + indexNum.ToString() + "' has not index" );//no appropriate index
        }

        #endregion
        #region Helpers ( rebuilding, defragmentation, dump )

        public int SortedColumn
        {
            get { return _sortedColumn; }
            set { _sortedColumn = value; }
        }

        private void DumpCounts()
        {
            Console.WriteLine( "#############################################" );
            _tracer.Trace( "#############################################" );
            Console.WriteLine( "Common count: " + Count );
            _tracer.Trace( "Common count: " + Count );
            for ( int i = 0; i < _indexes.Count; i++ )
            {
                IDBIndex dbIndex = (IDBIndex)_indexes[i];
                if ( dbIndex != null )
                {
                    Console.WriteLine( dbIndex.Name + " index count: " + dbIndex.Count );
                    _tracer.Trace( dbIndex.Name + " index count: " + dbIndex.Count );
                }
            }
            for ( int i = 0; i < _compoundIndexes.Count; i++ )
            {
                CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexes[i];
                if ( compoundIndex != null )
                {
                    IDBIndex dbIndex = compoundIndex._dbIndex;
                    Console.WriteLine( dbIndex.Name + " index count: " + dbIndex.Count );
                    _tracer.Trace( dbIndex.Name + " index count: " + dbIndex.Count );
                }
            }
            for ( int i = 0; i < _compoundIndexesWithValue.Count; i++ )
            {
                CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexesWithValue[i];
                if ( compoundIndex != null )
                {
                    IDBIndex dbIndex = compoundIndex._dbIndex;
                    Console.WriteLine( dbIndex.Name + " index count: " + dbIndex.Count );
                    _tracer.Trace( dbIndex.Name + " index count: " + dbIndex.Count );
                }
            }
            Console.WriteLine( "#############################################" );
            _tracer.Trace( "#############################################" );
        }

        private void CheckCountForIndexes()
        {
            int count = 0;
            for ( int i = 0; i < _indexes.Count; i++ )
            {
                IDBIndex dbIndex = _indexes[i] as IDBIndex;
                if ( dbIndex != null )
                {
                    if( count == 0 )
                    {
                        count = dbIndex.Count;
                    }
                    else if( dbIndex.Count != count )
                    {
                        DumpCounts();
                        throw new BadIndexesException( "Table: " + Name, null );
                    }
                }
            }
            for ( int i = 0; i < _compoundIndexes.Count; i++ )
            {
                CompoundIndex compoundIndex = _compoundIndexes[i] as CompoundIndex;
                if ( compoundIndex != null)
                {
                    if( count == 0 )
                    {
                        count = compoundIndex._dbIndex.Count;
                    }
                    else if( compoundIndex._dbIndex.Count != count )
                    {
                        DumpCounts();
                        throw new BadIndexesException( "Table: " + Name, null );
                    }
                }
            }
            for ( int i = 0; i < _compoundIndexesWithValue.Count; i++ )
            {
                CompoundIndex compoundIndex = _compoundIndexesWithValue[i] as CompoundIndex;
                if ( compoundIndex != null)
                {
                    if( count == 0 )
                    {
                        count = compoundIndex._dbIndex.Count;
                    }
                    else if( compoundIndex._dbIndex.Count != count )
                    {
                        DumpCounts();
                        throw new BadIndexesException( "Table: " + Name, null );
                    }
                }
            }
            if( !Dirty && count == 0 && !IsEmpty() )
            {
                throw new BadIndexesException( "Table: " + Name, null );
            }
        }

        public void RebuildIndexes()
        {
            TableRebuilder.RebuildIndexes( this, false );
        }

        public void RebuildIndexes( bool resetNextId )
        {
            TableRebuilder.RebuildIndexes( this, resetNextId );
        }

        public void OpenIndexes()
        {
            for ( int i = 0; i < _indexes.Count; ++i )
            {
                IDBIndex dbIndex = (IDBIndex)_indexes[i];
                if ( dbIndex != null )
                {
                    if( !dbIndex.Open() )
                    {
                        if ( !IsEmpty() && Count == 0 )
                        {
                            throw new BadIndexesException();
                        }
                    }
                }
            }
            for ( int i = 0; i < _compoundIndexes.Count; ++i )
            {
                CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexes[i];
                if ( compoundIndex != null )
                {
                    if( !compoundIndex._dbIndex.Open() )
                    {
                        if ( !IsEmpty() && Count == 0 )
                        {
                            throw new BadIndexesException();
                        }
                    }
                }
            }
            for ( int i = 0; i < _compoundIndexesWithValue.Count; ++i )
            {
                CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexesWithValue[i];
                if ( compoundIndex != null )
                {
                    if( !compoundIndex._dbIndex.Open() )
                    {
                        if ( !IsEmpty() && Count == 0 )
                        {
                            throw new BadIndexesException();
                        }
                    }
                }
            }
            CheckCountForIndexes();
        }

        private void EndOfUpgrade()
        {
            for ( int i = 0; i < _columns.Length; ++i )
            {
                StringColumnTo22 stringColumn = _columns[i] as StringColumnTo22;
                if ( stringColumn != null )
                {
                    _columns[i] = stringColumn.AsStringColumn();
                }
            }
            CheckedNeedUpgradeTo22();
        }

        public void Defragment( )
        {
            if ( NeedUpgradeTo22 )
            {
                for ( int i = 0; i < _columns.Length; ++i )
                {
                    StringColumnTo22 stringColumn = _columns[i] as StringColumnTo22;
                    if ( stringColumn != null )
                    {
                        if ( ReadToNearestEndMarker( 0 ) )
                        {
                            EndOfUpgrade();
                        }
                    }
                }
            }
            TableDefragmentator.Defragment( this );
            if ( NeedUpgradeTo22 )
            {
                EndOfUpgrade();
            }
        }

        public void DefragmentIndexes( bool idleMode )
        {
            ArrayList indexes = new ArrayList();

            lock( this )
            {
                for ( int i = 0; i < _indexes.Count; i++ )
                {
                    IDBIndex dbIndex = (IDBIndex)_indexes[i];
                    if ( dbIndex != null )
                    {
                        indexes.Add( dbIndex );
                    }
                }
                for ( int i = 0; i < _compoundIndexes.Count; i++ )
                {
                    CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexes[i];
                    if ( compoundIndex != null )
                    {
                        indexes.Add( compoundIndex._dbIndex );
                    }
                }
                for ( int i = 0; i < _compoundIndexesWithValue.Count; i++ )
                {
                    CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexesWithValue[i];
                    if ( compoundIndex != null )
                    {
                        indexes.Add( compoundIndex._dbIndex );
                    }
                }

                foreach( IDBIndex dbIndex in indexes )
                {
                    if( idleMode && !Core.IsSystemIdle )
                    {
                        break;
                    }
                    dbIndex.Defragment( idleMode );
                }
            }
        }

        private void ClearIndexes()
        {
            for ( int i = 0; i < _indexes.Count; i++ )
            {
                IDBIndex dbIndex = (IDBIndex)_indexes[i];
                if ( dbIndex != null )
                {
                    dbIndex.Clear();
                }
            }
            for ( int i = 0; i < _compoundIndexes.Count; i++ )
            {
                CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexes[i];
                if ( compoundIndex != null )
                {
                    compoundIndex._dbIndex.Clear();
                }
            }
            for ( int i = 0; i < _compoundIndexesWithValue.Count; i++ )
            {
                CompoundIndex compoundIndex = (CompoundIndex)_compoundIndexesWithValue[i];
                if ( compoundIndex != null )
                {
                    compoundIndex._dbIndex.Clear();
                }
            }
        }
        public bool IsEmpty()
        {
            return _file.Length == 0;
        }
        public abstract class RecordEnum
        {
            private Table _table;
            public RecordEnum( Table table )
            {
                _table = table;
            }
            protected abstract void OnRecordFetched( RecordType recType, long size );
            public virtual void EnumerateRecords( bool fix )
            {
                Stream stream = _table._file;
                stream.Position = 0;
                long offset = 0;
                try
                {
                    for ( ;; )
                    {
                        RecordType recType = _table.ReadRecord( fix );
                        long curOffset = stream.Position;
                        OnRecordFetched( recType, curOffset - offset );
                        offset = curOffset;
                    }
                }
                catch ( EndOfStreamException )
                {
                }
            }
        }
        public class RecordCounter : RecordEnum
        {
            private int _normal;
            private int _total;
            public RecordCounter( Table table ) : base( table )
            {
            }
            public static RecordsCounts GetRecordsCounts( Table table )
            {
                lock( table )
                {
                    RecordCounter counter = new RecordCounter( table );
                    counter.EnumerateRecords( false );
                    return counter.RecordsCounts;
                }
            }
            protected override void OnRecordFetched( RecordType recType, long size )
            {
                _total++;
                if ( recType == RecordType.NormalMarker )
                {
                    _normal++;
                }
            }
            public RecordsCounts RecordsCounts{ get { return new RecordsCounts( _normal, _total ); } }
        }
        public class TableRebuilder : RecordEnum
        {
            protected Table _table;
            protected Column[] _columns;
            private int _nextID = 0;
            private int _totalCount = 0;
            private int _currentOffset = 0;
            private long _fileLength;
            private int _lastTickCount;

            public TableRebuilder( Table table ) : base( table )
            {
                _table = table;
                _fileLength = _table._file.Length;
                _columns = _table._columns;
                _nextID = _table._tblStructure.NextID();
                Trace.WriteLine( "Rebuild indexes for: " + _table.Name );
            }
            public virtual void Finish()
            {
                _table._tblStructure.SetNextID( _nextID );
                _table._totalCount = _totalCount;
                _table._tblStructure.SetTotalCount( _totalCount );
            }

            public static void RebuildIndexes( Table table, bool resetNextId )
            {
                lock( table )
                {
                    TableRebuilder rebuilder = new TableRebuilder( table );
                    if ( resetNextId )
                    {
                        rebuilder._nextID = 0;
                    }
                    rebuilder.EnumerateRecords( true );
                    rebuilder.Finish();
                }
            }

            public override void EnumerateRecords( bool fix )
            {
                int multiplier = DBIndex._cacheSizeMultiplier;
                DBIndex._cacheSizeMultiplier = 20;
                try
                {
                    _table.ClearIndexes();
                    base.EnumerateRecords( fix );
                }
                finally
                {
                    DBIndex._cacheSizeMultiplier = multiplier;
                }
            }

            protected virtual int OffsetToWrite { get { return _currentOffset; } }

            protected override void OnRecordFetched( RecordType recType, long size )
            {
                _totalCount++;
                int currentTickCount = Environment.TickCount;
                if ( ( _table.NeedUpgradeTo22 && recType == RecordType.Normal ) || recType == RecordType.NormalMarker )
                {
                    for ( int i = 0; i < _columns.Length; ++i )
                    {
                        Column column = _columns[i];
                        if ( column.IndexNum != -1 )
                        {
                            object value = column.Value;
                            string strValue = value as string;
                            if ( strValue != null )
                            {
                                value = DBHelper.GetHashCodeInLowerCase( strValue );
                            }

                            column.Index.AddIndexEntry( (IComparable)value, OffsetToWrite );
                        }
                    }
                    _table.AddEntryToCompoundIndexes( OffsetToWrite );
                    _table.AddEntryToCompoundIndexesWithValue( OffsetToWrite );
                    int id = _table.GetID();
                    if ( id+1 > _nextID )
                    {
                        _nextID = id+1;
                    }
                }

                _currentOffset += (int)size;

                if ( currentTickCount - _lastTickCount > 300 )
                {
                    if ( _fileLength != 0 )
                    {
                        _table._database.OnProgress( "Rebuilding indexes for '" + _table.Name + "' table...",  OffsetToWrite, (int)_fileLength );
                    }
                    _lastTickCount = currentTickCount;
                }
            }
        }
        public class TableDefragmentator : TableRebuilder
        {
            private SafeBinaryWriter _writerDefragment = null;
            private string _strFullPath = null;
            private CachedStream _fileDefragment = null;
            private int _totalCount = 0;

            public TableDefragmentator( Table table ) : base( table )
            {
                _strFullPath = 
                    DBHelper.GetFullNameForTable( _table._database.Path, _table._database.Name, _table.Name + "_defragment" );
                _fileDefragment = DBHelper.PrepareIOFile( table, _strFullPath, FileMode.Create );
                Encoding encoding = new UTF8Encoding();  // throwOnInvalidBytes=false: see OM-7096
                _writerDefragment = new SafeBinaryWriter( _fileDefragment, encoding );
            }
            public override void Finish()
            {
                _table.TableShutdown();
                Rollback( _table._fileName );
                _table.OpenStreams();
                _table._totalCount = _totalCount;
                _table._tblStructure.SetTotalCount( _totalCount );
            }
            public void Rollback( string fileName )
            {
                CleanUp();
                try
                {
                    File.Copy( _strFullPath, fileName, true );
                }
                catch
                {
                    File.Delete( fileName );
                    File.Move( _strFullPath, fileName );
                    return;
                }
                File.Delete( _strFullPath );
            }
            public void CleanUp()
            {
                if ( _fileDefragment != null ) _fileDefragment.Close();
                if ( _writerDefragment != null ) _writerDefragment.Close();
                _fileDefragment = null;
                _writerDefragment = null;
            }

            public override void EnumerateRecords( bool fix )
            {
                if( _table.NeedUpgradeTo22 || _table.SortedColumn < 0 )
                {
                    try
                    {
                        if ( _table.NeedUpgradeTo22 )
                        {
                            Tracer._Trace( "Need Upgrade To Version 22, TABLE COUNT = " + _table.Count );
                            bool processByOffsets = false;
                            if ( _table.Count != 0 )
                            {
                                ArrayList columns = _table.GetColumnInfos();
                                foreach ( ColumnInfo column in columns )
                                {
                                    if ( column.Type == ColumnType.String )
                                    {
                                        Tracer._Trace( "There is String column '" + column.Name + "'" );
                                        processByOffsets = true;
                                        break;
                                    }
                                }
                            }
                            if ( processByOffsets )
                            {
                                IntArrayList offsets = new IntArrayList();
                                _table.GetAllOffsets( offsets );
                                if( _table.Count == offsets.Count )
                                {
                                    try
                                    {
                                        Tracer._Trace( "Try common enumeration" );
                                        base.EnumerateRecords( fix );
                                        return;
                                    }
                                    catch ( StringCorruptedException exception )
                                    {
                                        Tracer._TraceException( exception );
                                    }
                                    catch ( BadIndexesException exception )
                                    {
                                        Tracer._TraceException( exception );
                                    }
                                    Tracer._Trace( "Try enumeration with offsets from index" );
                                    _table.ClearIndexes();
                                    offsets.Sort();
                                    for( int i = 0; i < offsets.Count; ++i )
                                    {
                                        _table.ReadRecordFromOffsetNoChecking( offsets[ i ] );
                                        OnRecordFetched( RecordType.NormalMarker, 0 );
                                    }
                                    return;
                                }
                            }
                        }
                    }
                    catch ( Exception exception )
                    {
                        Tracer._TraceException( exception );
                    }
                    base.EnumerateRecords( fix );
                }
                else
                {
                    IResultSet rs = _table.CreateResultSet( _table.SortedColumn );
                    IntArrayList offsets = new IntArrayList();
                    using( rs )
                    {
                        foreach( Record rec in rs )
                        {
                            offsets.Add( rec.Offset );
                        }
                    }
                    if( _table.Count != offsets.Count )
                    {
                        throw new InvalidOperationException(
                            "Can't defragment the table, index is corrupted. _table.count = " +
                            _table.Count + ", offsets.Count = " + offsets.Count );
                    }
                    _table.ClearIndexes();
                    for( int i = 0; i < offsets.Count; ++i )
                    {
                        _table.ReadRecordFromOffset( offsets[ i ] );
                        OnRecordFetched( RecordType.NormalMarker, 0 );
                    }
                }
            }

            public static void Defragment( Table table )
            {
                lock( table )
                {
                    TableDefragmentator defragmentator = new TableDefragmentator( table );
                    try
                    {
                        defragmentator.EnumerateRecords( false );
                        defragmentator.Finish();
                    }
                    catch ( Exception exception )
                    {
                        Tracer._TraceException( exception );
                        table.ClearIndexes();
                        throw new DefragmentationFailedException( exception.Message, exception );
                    }
                    finally
                    {
                        defragmentator.CleanUp();
                    }
                }
            }
            protected override int OffsetToWrite { get { return (int)_fileDefragment.Position; } }
            protected override void OnRecordFetched( RecordType recType, long size )
            {
                base.OnRecordFetched( recType, size );
                if ( ( _table.NeedUpgradeTo22 && recType == RecordType.Normal ) || recType == RecordType.NormalMarker )
                {
                    _totalCount++;
                    _writerDefragment.Write( (byte)RecordType.NormalMarker );
                    for ( int i = 0; i < _columns.Length; ++i )
                    {
                        _columns[i].SaveValue( _writerDefragment );
                    }
                }
            }
        }

        public RecordsCounts ComputeWastedSpace( )
        {
            if ( _totalCount != -1 )
            {
                return new RecordsCounts( Count, _totalCount );
            }
            else
            {
                RecordsCounts recordsCounts = RecordCounter.GetRecordsCounts( this );
                _totalCount = recordsCounts.TotalRecordCount;
                _tblStructure.SetTotalCount( _totalCount );
                return recordsCounts;
            }
        }
        public void AddEntryToCompoundIndexesWithValue( int currentOffset )
        {
            for ( int i = 0; i < _compoundIndexesWithValue.Count; i++ )
            {
                CompoundIndexWithValue compoundIndex = _compoundIndexesWithValue[i] as CompoundIndexWithValue;
                if ( compoundIndex != null )
                {
                    IDBIndex dbIndex = compoundIndex._dbIndex;
                    object value1 = compoundIndex._column1.Value;
                    object value2 = compoundIndex._column2.Value;
                    object value3 = compoundIndex._valueColumn.Value;

                    string strValue1 = value1 as string;
                    if ( strValue1 != null )
                    {
                        value1 = DBHelper.GetHashCodeInLowerCase( strValue1 );
                    }
                    string strValue2 = value2 as string;
                    if ( strValue2 != null )
                    {
                        value2 = DBHelper.GetHashCodeInLowerCase( strValue2 );
                    }

                    SetCompoundKey( _compoundAndValue, value1, value2, value3 );

                    dbIndex.AddIndexEntry( _compoundAndValue, currentOffset );
                }
            }
        }
        public void AddEntryToCompoundIndexes( int currentOffset )
        {
            for ( int i = 0; i < _compoundIndexes.Count; i++ )
            {
                CompoundIndex compoundIndex = _compoundIndexes[i] as CompoundIndex;
                if ( compoundIndex != null )
                {
                    IDBIndex dbIndex = compoundIndex._dbIndex;
                    object value1 = compoundIndex._column1.Value;
                    object value2 = compoundIndex._column2.Value;

                    string strValue1 = value1 as string;
                    if ( strValue1 != null )
                    {
                        value1 = DBHelper.GetHashCodeInLowerCase( strValue1 );
                    }
                    string strValue2 = value2 as string;
                    if ( strValue2 != null )
                    {
                        value2 = DBHelper.GetHashCodeInLowerCase( strValue2 );
                    }

                    SetCompoundKey( _compound, value1, value2 );

                    dbIndex.AddIndexEntry( _compound, currentOffset );
                }
            }
        }

        public void Dump()
        {
            Console.WriteLine( "Table = " +  Name );
            _file.Position = 0;

            try
            {
                for ( int i = 0; i < _columns.Length; ++i )
                {
                    Console.Write( _columns[i].Name + "\t\t" );
                }
                Console.WriteLine( string.Empty );
                while ( true )
                {
                    RecordType recType = (RecordType)_file.ReadByte(); //read for RecordType
                    for ( int i = 0; i < _columns.Length; ++i )
                    {
                        Column column = _columns[i];
                        column.LoadValue( _reader );
                        if ( recType == RecordType.NormalMarker )
                        {
                            Console.Write( column.Value.ToString() + "\t\t" );
                        }
                    }
                    if ( recType == RecordType.NormalMarker )
                    {
                        Console.WriteLine( string.Empty );
                    }
                }
            }
            catch ( EndOfStreamException )
            {
                Console.WriteLine( "______________________________________________________________________" );
            }
        }
        #endregion

        public void LowLevelCheck()
        {
            Console.WriteLine( "Low level checking for '" + Name + "'"  );
            bool expected = false;
            try
            {
                _file.Position = 0;
                for ( ;; )
                {
                    expected = true;
                    long recordOffset = _file.Position;
                    _file.ReadByte();
                    expected = false;
                    for ( int i = 0; i < _columns.Length; ++i )
                    {
                        try
                        {
                            _columns[i].LoadValue( _reader );
                        }
                        catch ( StringCorruptedException )
                        {
                            Console.WriteLine( );
                            Console.WriteLine( "Error: String is corrupted. Record offset = " + recordOffset );
                            Console.WriteLine( );
                            return;
                        }
                        catch ( NoEndMarkerException )
                        {
                            Console.WriteLine( );
                            Console.WriteLine( "Error: No end marker for string. Record offset = " + recordOffset );
                            Console.WriteLine( );
                            return;
                        }
                        catch ( DateTimeCorruptedException )
                        {
                            Console.WriteLine( );
                            Console.WriteLine( "Error: DateTime is corrupted. Record offset = " + recordOffset );
                            Console.WriteLine( );
                            return;
                        }
                    }
                }
            }
            catch ( EndOfStreamException )
            {
                if ( !expected )
                {
                    Console.WriteLine( );
                    Console.WriteLine( "Error: Unexpected end of file." );
                    Console.WriteLine( );
                }
            }
        }
    }

    internal class CompoundIndex
    {
        public IDBIndex _dbIndex;
        public Column _column1;
        public Column _column2;
        public int _columnIndex1;
        public int _columnIndex2;

        public CompoundIndex( IDBIndex dbIndex, Column column1, int columnIndex1, Column column2, int columnIndex2 )
        {
            _dbIndex = dbIndex;
            _column1 = column1;
            _column2 = column2;
            _columnIndex1 = columnIndex1;
            _columnIndex2 = columnIndex2;
        }
    }
    internal class CompoundIndexWithValue : CompoundIndex
    {
        public int _fieldIndex;
        public Column _valueColumn;

        public CompoundIndexWithValue( IDBIndex dbIndex, Column column1, int columnIndex1, Column column2, int columnIndex2, Column valueColumn, int fieldIndex ) : 
            base( dbIndex, column1, columnIndex1, column2, columnIndex2 )
        {
            _fieldIndex = fieldIndex;
            _valueColumn = valueColumn;
        }
    }

    internal class SafeBinaryWriter: BinaryWriter
    {
        private Encoder _encoder;
        private byte[] _charBytes;
        private const int HALF_BUFFER_SIZE = 512;
        private const int BUFFER_SIZE = HALF_BUFFER_SIZE*2;

        internal SafeBinaryWriter( Stream baseStream, Encoding encoding )
            : base( baseStream, encoding )
        {
            _encoder = encoding.GetEncoder();
        }
        public void WriteStringSafeWithIntLength( string str )
        {
            if ( _charBytes == null )
            {
                _charBytes = new byte [BUFFER_SIZE];
            }

            int length = str.Length;
            char[] chars = str.ToCharArray( 0, length );
            int byteCount = _encoder.GetByteCount( chars, 0, length, true );
            Write( byteCount );

            int curPos = 0;
            while ( length > 0 )
            {
                int toRead = length < HALF_BUFFER_SIZE ? length : HALF_BUFFER_SIZE;
                length -= toRead;
                byteCount = _encoder.GetByteCount( chars, curPos, toRead, true );
                byte[] buffer = byteCount > BUFFER_SIZE ? new byte[byteCount] : _charBytes;
                int n = _encoder.GetBytes( chars, curPos, toRead, buffer, 0, true );
                Write( buffer, 0, n );
                curPos += toRead;
            }
        }
    }

    /// <summary>
    /// A bugfix for BinaryReader which fixes the "Conversion buffer overflow" issue
    /// http://blogs.jetbrains.com/yole/archives/000035.html
    /// </summary>
    internal class SafeBinaryReader: BinaryReader
    {
        private byte[] _charBytes;
        private char[] _charBuffer;
        private Decoder _decoder;
        
        internal SafeBinaryReader( Stream baseStream, Encoding encoding )
            : base( baseStream, encoding )
        {
            _decoder = encoding.GetDecoder();            
        }

        public string ReadStringSafeWithoutLength( int stringLength )
        {
            if ( stringLength < 0 )
            {
                throw new StringCorruptedException( "string length was negative: " + stringLength );
            }
            if ( _charBytes == null )
            {
                _charBytes = new byte [128];
            }
            if ( _charBuffer == null )
            {
                _charBuffer = new char [256];
            }
            
            int currPos = 0;
            StringBuilder sb = null;
            try
            {
                do
                {
                    int readLength = ((stringLength - currPos) > 128) ? 128 : stringLength - currPos;
                    int n = Read( _charBytes, 0, readLength );
                    if ( n == 0 )
                    {
                        throw new EndOfStreamException();
                    }

                    int charsRead = _decoder.GetChars( _charBytes, 0, n, _charBuffer, 0 );
                    if ( currPos == 0 && n == stringLength )
                        return new string( _charBuffer, 0, charsRead );

                    if ( sb == null )
                    {
                        sb = StringBuilderPool.Alloc();
                    }
                    sb.Append( _charBuffer, 0, charsRead );
                    currPos += n;
                } while( currPos < stringLength );
                return sb.ToString();
            }
            finally
            {
                if( sb != null )
                {
                    StringBuilderPool.Dispose( sb );
                }
            }
        }

        public string ReadStringSafe()
        {
            int stringLength = Read7BitEncodedInt();
            if ( stringLength == 0 )
            {
                return String.Empty;
            }
            return ReadStringSafeWithoutLength( stringLength );
        }
    }
    public class Tests
    {
        public static void ReadRecordFromOffset( ITable table, int offset )
        {
            ( table as Table ).ReadRecordFromOffset( offset );
        }
        public static void DeleteRecord( ITable table, int offset, object[] fields )
        {
            ( table as Table ).DeleteRecord( offset, fields );
        }
    }
}
