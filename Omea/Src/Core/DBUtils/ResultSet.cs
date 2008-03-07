/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Threading;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.Database
{
    internal class EmptyResultSet : ICountedResultSet
    {
        private static EmptyEnumerator _emptyEnumerator = new EmptyEnumerator();

        public int Count
        {
            get { return 0; }
        }

        public IRecord this[int index]
        {
            get { throw new ArgumentOutOfRangeException( "there is empty result set" ); }
        }

        public IEnumerator GetEnumerator()
        {
            return _emptyEnumerator;
        }

        public void Dispose()
        {
        }
    }

    internal class GenericResultSet : ICountedResultSet
    {
        private Table _table;
        private IntArrayList _offsets;
        private Record _lastRecord;
        private int _lastIndex = -1;
        private static EmptyEnumerator _emptyEnumerator = new EmptyEnumerator();

        public GenericResultSet( Table table )
        {
            _table = table;
        }
        protected void SetOffsets( IntArrayList offsets )
        {
            _offsets = offsets;
        }
        internal IntArrayList Offsets{ get{ return _offsets; } }

        public int Count { get { return _offsets.Count; } }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if( _offsets.Count == 0 )
            {
                return _emptyEnumerator;
            }
            return new GenericResultSetEnumerator( this );
        }

        public IRecord this[int index]
        {
            get
            {
                if( _lastIndex != index )
                {
                    _lastIndex = index;
                    int offset = _offsets[ index ];
                    if( _lastRecord == null )
                    {
                        _lastRecord = _table.GetRecordImpl( offset );
                    }
                    else
                    {
                        _lastRecord.Init( offset );
                    }
                }
                return _lastRecord;
            }
        }
        #region IDisposable Members

        public void Dispose()
        {
            if ( _offsets != null )
            {
                IntArrayListPool.Dispose( _offsets );
                _offsets = null;
                Monitor.Exit( _table );
            }
        }

        #endregion
    }

    internal class GenericResultSetEnumerator: IEnumerator
    {
        private GenericResultSet _rs;
        private int _cursor = -1;

        internal GenericResultSetEnumerator( GenericResultSet rs )
        {
            _rs = rs;
        }

        void IEnumerator.Reset()
        {
            _cursor = -1;
        }
        bool IEnumerator.MoveNext()
        {
            return ++_cursor < _rs.Count;
        }
        object IEnumerator.Current
        {
            get
            {
                if ( ( _cursor < 0 ) || ( _cursor == _rs.Count ) )
                {
                    throw new InvalidOperationException();
                }
                return _rs[ _cursor ];
            }
        }
    }

    internal class ResultSet : GenericResultSet
    {
        public ResultSet( Table table, IntArrayList offsets ) : base( table )
        {
            SetOffsets( offsets );
        }
    }

    internal class EnumerableResultSet : ICountedResultSet
    {
        private Table       _table;
        private IEnumerable _index;
        private bool        _disposed;

        internal EnumerableResultSet( Table table, IEnumerable index )
        {
            _table = table;
            _index = index;
        }

        #region IResultSet Members

        public IRecord this[ int index ]
        {
            get
            {
                throw new InvalidOperationException( "EnumerableResultSet doesn't support random access." );
            }
        }

        #endregion

        #region ICollection Members

        public int Count
        {
            get
            {
                throw new InvalidOperationException( "EnumerableResultSet doesn't implement ICollection" );
            }
        }

        #endregion

        #region IEnumerable Members

        private class RecordEnumerator: IKeyPairEnumerator
        {
            private Table       _table;
            private IEnumerator _index;
            private KeyPair     _pair;
            private Record      _rec;
            private bool        _reloadRecord;

            public RecordEnumerator( Table table, IEnumerator index )
            {
                _table = table;
                _index = index;
                Reset();
            }
            public void Reset()
            {
                _index.Reset();
                _reloadRecord = true;
            }

            public object Current
            {
                get
                {
                    if( _reloadRecord ) 
                    {
                        _reloadRecord = false;
                        if( _rec == null )
                        {
                            _rec = _table.GetRecordImpl( _pair._offset );
                        }
                        else
                        {
                            _rec.Init( _pair._offset );
                        }
                    }
                    return _rec;
                }
            }

            public IFixedLengthKey GetCurrentKey()
            {
                return _pair._key;
            }

            public int GetCurrentOffset()
            {
                return _pair._offset;
            }

            public bool MoveNext()
            {
                if( _index.MoveNext() )
                {
                    _pair = (KeyPair) _index.Current;
                    _reloadRecord = true;
                    return true;
                }
                return false;
            }

            public void Dispose()
            {
                ((IDisposable) _index).Dispose();
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new RecordEnumerator( _table, _index.GetEnumerator() );
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if ( !_disposed )
            {
                _disposed = true;
                Monitor.Exit( _table );
            }
        }

        #endregion
    }


    internal class Record : IRecord
    {
        private Table _table;
        private int _offset;
        private object[] _fields;
        private object[] _fieldsOld;
        private RecordType _recordType;

        internal Record( Table table, int offset )
        {
            _table = table;
            Init( offset );
        }

        internal Record( Table table, int offset, object[] fields )
        {
            _table = table;
            Init( offset, fields );
        }

        internal void Init( int offset )
        {
            _offset = offset;
            if ( offset != -1 )
            {
                _recordType = RecordType.NormalMarker;
                _fields = _table.ReadRecordFromOffset( _offset );
            }
            else
            {
                _recordType = RecordType.New;
                _fields = _table.ReadRecordFromColumns( );
            }
        }

        internal void Init( int offset, object[] fields )
        {
            _offset = offset;
            _fields = fields;
            _recordType = RecordType.NormalMarker;
        }

        internal void SetFields( object[] fields )
        {
            _fields = fields;
        }

        private void CloneFields()
        {
            _fieldsOld = _fields.Clone() as object[];
        }

        public void Delete()
        {
            object[] testFields;
            if ( _recordType == RecordType.NormalMarker )
            {
                testFields = _fields;
            }
            else if ( _recordType == RecordType.Updated )
            {
                testFields = _fieldsOld;
            }
            else
            {
                throw new AttemptDeleteNotNormalOrNotUpdatedRecordException();
            }
            if ( _table.IsRecordWithBLOB )
            {
                for ( int i = 0; i < testFields.Length; i++ )
                {
                    IBLOB colValue  = testFields[i] as IBLOB;
                    if ( colValue != null )
                    {
                        colValue.Delete();
                    }
                }
            }
            _table.DeleteRecord( _offset, testFields );
            _recordType = RecordType.DeletedMarker;
            _fieldsOld = null;
        }

        public int GetID()
        {
            if ( _fields[0] == null )
            {
                _fields[0] = 0;
            }
            return (int)_fields[0];
        }
        public object GetValue( int index )
        {
            return _fields[index];
        }
        public void SetValue( int index, object value )
        {
            if ( _recordType == RecordType.DeletedMarker ) throw new AttemptWritingToDeletedRecordException();
            if ( _recordType == RecordType.NormalMarker )
            {
                CloneFields();
                _recordType = RecordType.Updated;
            }
            _fields[index] = value;
        }

        public void Commit()
        {
            if ( _recordType == RecordType.Updated || _recordType == RecordType.New )
            {
                _offset = _table.UpdateFields( this, _fieldsOld, _fields, _offset, _recordType );
                _recordType = RecordType.NormalMarker;
                _fieldsOld = null;
                return;
            }
            throw new AttemptCommitNotNewOrNotUpdatedRecordException();
        }
        public string GetStringValue( int index )
        {
            if ( _recordType == RecordType.Deleted ) throw new AttemptReadingDeletedRecordException();
            return (string)_fields[index];
        }
        public int GetIntValue( int index )
        {
            if ( _recordType == RecordType.Deleted ) throw new AttemptReadingDeletedRecordException();
            return (int)_fields[index];
        }
        public double GetDoubleValue( int index )
        {
            if ( _recordType == RecordType.Deleted ) throw new AttemptReadingDeletedRecordException();
            return (double)_fields[index];
        }
        public IBLOB GetBLOBValue( int index )
        {
            if ( _recordType == RecordType.Deleted ) throw new AttemptReadingDeletedRecordException();
            return (IBLOB)_fields[index];
        }
        public DateTime GetDateTimeValue( int index )
        {
            if ( _recordType == RecordType.Deleted ) throw new AttemptReadingDeletedRecordException();
            return (DateTime)_fields[index];
        }

        internal int Offset { get{ return _offset; } }
    }
}
