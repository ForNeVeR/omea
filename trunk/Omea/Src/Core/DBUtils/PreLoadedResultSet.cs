/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Threading;
using JetBrains.Omea.Containers;
using System.Globalization;

namespace JetBrains.Omea.Database
{
    internal class PreLoadedResultSet: ICountedResultSet
    {
        private readonly IResultSet _resultSet;
        private readonly int _indexStringColumn;
        private readonly string _value;
        private static CompareInfo _comparer = CultureInfo.CurrentCulture.CompareInfo;

        public PreLoadedResultSet( IResultSet resultSet, int indexStringColumn, string value )
        {
            _resultSet = resultSet;
            _indexStringColumn = indexStringColumn;
            _value = value;
        }

        public int Count
        {
            get
            {
                int count = 0;
                foreach( IRecord rec in this )
                {
                    ++count;
                }
                return count;
            }
        }

        public IRecord this[ int index ]
        {
            get
            {
                int aIndex = -1;
                foreach( IRecord rec in this )
                {
                    if ( ++aIndex == index )
                    {
                        return rec;
                    }
                }
                throw new IndexOutOfRangeException();
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new VerifyStringEnumerator( _resultSet.GetEnumerator(), _indexStringColumn, _value );
        }

        private class VerifyStringEnumerator: IEnumerator, IDisposable
        {
            private readonly IEnumerator _enumerator;
            private readonly int _indexStringColumn;
            private readonly string _value;

            public VerifyStringEnumerator( IEnumerator enumerator, int indexStringColumn, string value )
            {
                _enumerator = enumerator;
                _indexStringColumn = indexStringColumn;
                _value = value;
            }

            public bool MoveNext()
            {
                while( _enumerator.MoveNext() )
                {
                    IRecord record = (IRecord) _enumerator.Current;
                    string strValue = record.GetStringValue( _indexStringColumn );
                    if( _comparer.Compare( strValue, _value, 
                        CompareOptions.StringSort | CompareOptions.IgnoreCase ) == 0 )
                    {
                        return true;                        
                    }
                }
                return false;
            }

            public void Reset()
            {
                _enumerator.Reset();
            }

            public object Current
            {
                get { return _enumerator.Current; }
            }

            public void Dispose()
            {
                IDisposable disposable = _enumerator as IDisposable;
                if ( disposable != null )
                {
                    disposable.Dispose();
                }
            }
        }

        public void Dispose()
        {
            _resultSet.Dispose();
        }
    }

    internal class FromIndexWithValueResultSet : ICountedResultSet
    {
        private Table _table;
        private IEnumerable _enumerable;
        private int _indexField1, _indexField2, _indexValue;
        private bool _disposed = false;

        public FromIndexWithValueResultSet(
            Table table, IEnumerable enumerable, int indexField1, int indexField2, int indexValue )
        {
            _table = table;
            _enumerable = enumerable;
            _indexField1 = indexField1;
            _indexField2 = indexField2;
            _indexValue = indexValue;
        }

        public int Count 
        {
            get
            {
                int count = 0;
                foreach( IRecord rec in this )
                {
                    ++count;
                }
                return count;
            }
        }

        public IRecord this[ int index ]
        {
            get
            {
                int aIndex = -1;
                foreach( IRecord rec in this )
                {
                    if( ++aIndex == index )
                    {
                        return rec;
                    }
                }
                throw new IndexOutOfRangeException();
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new FromIndexWithValueEnumerator(
                _table, _enumerable.GetEnumerator(), _indexField1, _indexField2, _indexValue );
        }

        private class FromIndexWithValueEnumerator : IRecordEnumerator
        {
            private Table _table;
            private IEnumerator _enumerator;
            private int _indexField1, _indexField2, _indexValue;
            private KeyPair _keyPair;
            private Record _record;
            private object[] _fields;

            public FromIndexWithValueEnumerator(
                Table table, IEnumerator enumerator, int indexField1, int indexField2, int indexValue )
            {
                _table = table;
                _enumerator = enumerator;
                _indexField1 = indexField1;
                _indexField2 = indexField2;
                _indexValue = indexValue;
            }
            #region IEnumerator Members

            public void Reset()
            {
                _enumerator.Reset();
            }

            public object Current
            {
                get
                {
                    FixedLengthKey_CompoundWithValue keyWithValue = _keyPair._key as FixedLengthKey_CompoundWithValue;
                    CompoundAndValue compoundAndValue = keyWithValue.Key as CompoundAndValue;
                    if( _fields == null )
                    {
                        _fields = new object[ 3 ];
                    }
                    _fields[ _indexField1 ] = compoundAndValue._key1;
                    _fields[ _indexField2 ] = compoundAndValue._key2;
                    _fields[ _indexValue ] = compoundAndValue._value;
                    if( _record == null )
                    {
                        _record = new Record( _table, _keyPair._offset, _fields );
                    }
                    else
                    {
                        _record.Init( _keyPair._offset, _fields );
                    }
                    return _record;
                }
            }

            public IComparable GetCurrentRecordValue( int index )
            {
                FixedLengthKey_CompoundWithValue keyWithValue = _keyPair._key as FixedLengthKey_CompoundWithValue;
                CompoundAndValue compoundAndValue = keyWithValue.Key as CompoundAndValue;

                if ( index == _indexField1 )
                {
                    return compoundAndValue._key1;
                }
                if ( index == _indexField2 )
                {
                    return compoundAndValue._key2; 
                }
                if ( index == _indexValue )
                {
                    return compoundAndValue._value;
                }

                throw new IndexOutOfRangeException( "Index out of range; value=" + index );
            }

            public bool MoveNext()
            {
                if( _enumerator.MoveNext() )
                {
                    _keyPair = (KeyPair) _enumerator.Current;
                    return true;
                }
                return false;
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                IDisposable disposable = _enumerator as IDisposable;
                if( disposable != null )
                {
                    disposable.Dispose();
                }
            }

            #endregion
        }
        
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
}
