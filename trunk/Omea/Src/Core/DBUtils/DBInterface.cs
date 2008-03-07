/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.Database
{
    public enum DatabaseMode
    {
        Create = 0,
        Open = 1
    };

    public enum ColumnType
    {
        String = 0,
        DateTime = 1,
        Integer = 2,
        Double = 3,
        BLOB = 4
    }
    public interface IDatabase
    {
        ITable GetTable( string name );
        string Name{ get; }
        string Path{ get; }
        int Version{ get; }
        void Shutdown();
        void Flush();
        void RepairBlobFileSystem();
    }

    internal interface IDatabaseDesign
    {
        ITableDesign CreateTable( string name, TableStructure tblStructure );
        BlobFileSystem BlobFS { get; }
        string Name{ get; }
        string Path{ get; }
        int Version{ get; }
        void Shutdown();
        void Flush();
    }

    public interface ITable
    {
        string Name { get; }
        ICountedResultSet CreateModifiableResultSet( int columnIndex, object key );
        IResultSet CreateResultSet( int columnIndex, object key );
        ICountedResultSet CreateResultSetForRange( int firstColumnIndex, object firstKey, int secondColumnIndex, object beginKey, object endKey );
        ICountedResultSet CreateResultSet( int columnIndex );
        ICountedResultSet CreateResultSet( int columnIndex1, object key1, int columnIndex2, object key2, bool readOnly );
        ICountedResultSet EmptyResultSet{ get; }
        IRecord GetRecord( int offset );
        IRecord GetRecordByEqual( int columnIndex, object key );
        int NextID();
        int PeekNextID();
        ArrayList GetColumnInfos();
        int Count{ get; }
        IRecord NewRecord();
        IBLOB CreateBLOB( Stream stream );

        RecordsCounts ComputeWastedSpace( );
        void RebuildIndexes();
        void RebuildIndexes( bool resetNextId );
        int SortedColumn { get; set; }
        void Defragment();
        void DefragmentIndexes( bool idleMode );
        bool Dirty { get; }
        bool IsEmpty();
        DatabaseMode Mode { get; }

        long TracePerformanceCounters();
        bool AutoFlush { get; set; }
        void FlushData();
    }

    internal interface ITableDesign
    {
        int AddColumn( Column column );
        Column GetColumn( string name );
        void AddIndex( string indexName, IDBIndex dbIndex );
        void AddCompoundIndex( string indexName, IDBIndex dbIndex );
        void AddCompoundIndexWithValue( string indexName, IDBIndex dbIndex, string columnName );
        ArrayList GetColumnInfos();
        string Name { get; }
        IDatabaseDesign Database { get; }
        int Version{ get; }
        DatabaseMode Mode { get; }
    }
    
    public interface IResultSet : IEnumerable, IDisposable
    {
    }

    public interface ICountedResultSet: IResultSet
    {
        int Count { get; }

        IRecord this[int index]
        {
            get;
        }
    }

    public interface IRecordEnumerator: IEnumerator, IDisposable
    {
        IComparable GetCurrentRecordValue( int index );
    }

    public interface IKeyPairEnumerator: IEnumerator, IDisposable
    {
        IFixedLengthKey GetCurrentKey();
        int GetCurrentOffset();
    }

    public interface IBLOB
    {
        void Delete();
        Stream Stream
        {
            get;
        }
        void Set( Stream source );
    }

    public interface IRecord
    {
        object GetValue( int index );
        void SetValue( int index, object value );
        string GetStringValue( int index );
        int GetIntValue( int index );
        IBLOB GetBLOBValue( int index );
        double GetDoubleValue( int index );
        DateTime GetDateTimeValue( int index );

        int GetID();
        void Commit();
        void Delete();
    }

    public class ColumnInfo
    {
        private string _name;
        private ColumnType _type;

        public ColumnInfo( string name, ColumnType type )
        {
            _name = name;
            _type = type;
        }

        public string Name{ get{ return _name; } }
        public ColumnType Type{ get{ return _type; } }
    }

    public class RecordsCounts
    {
        private int _normalRecordCount;
        private int _totalRecordCount;
        public RecordsCounts( int normalRecordCount, int totalRecordCount )
        {
            _normalRecordCount = normalRecordCount;
            _totalRecordCount = totalRecordCount;
        }
        public int NormalRecordCount { get { return _normalRecordCount; } }
        public int TotalRecordCount{ get { return _totalRecordCount; } }
    }

    [Serializable]
    public class CannotGetVersionInfoException : Exception
    {
        public CannotGetVersionInfoException( string message, Exception innerException ) : base( message, innerException ) {}

        protected CannotGetVersionInfoException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    [Serializable]
    public class BackwardIncompatibility : Exception
    {
        public BackwardIncompatibility( string message ) : base( message )
        {
        }

        protected BackwardIncompatibility( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }

    [Serializable]
    public class IndexIsCorruptedException : Exception
    {
        public IndexIsCorruptedException() : base() {}
        public IndexIsCorruptedException( string message ) : base( message )
        {
        }

        protected IndexIsCorruptedException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }

    [Serializable]
    public class IndexAlreadyExistsException : Exception
    {
        public IndexAlreadyExistsException() : base() { }
        public IndexAlreadyExistsException( string message ) : base( message ) { }

        protected IndexAlreadyExistsException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }

    [Serializable]
    public class IndexDoesNotExistException : Exception
    {
        public IndexDoesNotExistException() : base() { }
        public IndexDoesNotExistException( string message ) : base( message )
        {
        }

        protected IndexDoesNotExistException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }

    [Serializable]
    public class TableAlreadyExistsException : System.ArgumentException
    {
        public TableAlreadyExistsException(): base() { }
        public TableAlreadyExistsException( string message, string tableName ) : 
            base( message, tableName )
        {
        }

        protected TableAlreadyExistsException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    
    [Serializable]
    public class TableDoesNotExistException : System.ArgumentException
    {
        public TableDoesNotExistException() : base() { }
        public TableDoesNotExistException( string message, string tableName ) : 
            base( message, tableName )
        {
        }
        protected TableDoesNotExistException( SerializationInfo info, StreamingContext context )
            : base( info, context ) { }
    }
    
    [Serializable]
    public class ColumnAlreadyExistsException : System.ArgumentException
    {
        public ColumnAlreadyExistsException(): base() { }
        public ColumnAlreadyExistsException( string message, string columnName ) : 
            base( message, columnName )
        {
        }

        protected ColumnAlreadyExistsException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    
    [Serializable]
    public class ColumnDoesNotExistException : System.ArgumentException
    {
        public ColumnDoesNotExistException() : base() { }
        public ColumnDoesNotExistException( string message, string columnName ) : 
            base( message, columnName )
        {
        }

        protected ColumnDoesNotExistException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    
    [Serializable]
    public class ColumnHasNoIndexException : System.Exception
    {
        public ColumnHasNoIndexException() : base() { }
        public ColumnHasNoIndexException( string message ) : 
            base( message)
        {
        }

        protected ColumnHasNoIndexException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    
    [Serializable]
    public class RebuildIndexesNeededException : System.Exception
    {
        public RebuildIndexesNeededException() : base() { }
        public RebuildIndexesNeededException( string message ) : 
            base( message)
        {
        }

        protected RebuildIndexesNeededException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    
    [Serializable]
    public class DefragmentationFailedException : System.Exception
    {
        public DefragmentationFailedException( System.Exception innerException ) : base( "", innerException ) { }
        public DefragmentationFailedException( string message, System.Exception innerException ) : 
            base( message, innerException )
        {
        }

        protected DefragmentationFailedException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    
    [Serializable]
    public class AttemptReadingDeletedRecordException : System.Exception
    {
        public AttemptReadingDeletedRecordException( string message ) : base( message)
        {
        }
        public AttemptReadingDeletedRecordException( )
        {
        }

        protected AttemptReadingDeletedRecordException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }

    [Serializable]
    public class AttemptWritingToDeletedRecordException : System.Exception
    {
        public AttemptWritingToDeletedRecordException( )
        {
        }

        protected AttemptWritingToDeletedRecordException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    [Serializable]
    public class AttemptCommitNotNewOrNotUpdatedRecordException : System.Exception
    {
        public AttemptCommitNotNewOrNotUpdatedRecordException( )
        {
        }

        protected AttemptCommitNotNewOrNotUpdatedRecordException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    [Serializable]
    public class AttemptDeleteNotNormalOrNotUpdatedRecordException : System.Exception
    {
        public AttemptDeleteNotNormalOrNotUpdatedRecordException( )
        {
        }

        protected AttemptDeleteNotNormalOrNotUpdatedRecordException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    
    [Serializable]
    public class DataCorruptedException: System.Exception
    {
        public DataCorruptedException() : base() { }
        public DataCorruptedException( string message )
            : base( message )
        {
        }

        protected DataCorruptedException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    [Serializable]
    public class DateTimeCorruptedException: DataCorruptedException
    {
        public DateTimeCorruptedException() : base() { }
        public DateTimeCorruptedException( string message )
            : base( message )
        {
        }

        protected DateTimeCorruptedException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    [Serializable]
    public class NoEndMarkerException: DataCorruptedException
    {
        public NoEndMarkerException() : base() { }
        public NoEndMarkerException( string message )
            : base( message )
        {
        }

        protected NoEndMarkerException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
    [Serializable]
    public class StringCorruptedException: DataCorruptedException
    {
        public StringCorruptedException() : base() { }
        public StringCorruptedException( string message )
            : base( message )
        {
        }

        protected StringCorruptedException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
}
