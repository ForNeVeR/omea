// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using JetBrains.Omea.Containers;
using JetBrains.Omea.Database;

namespace JetBrains.Omea.ResourceStore
{
	/// <summary>
	/// Result set enumerator which supports uniform exception handling.
	/// </summary>
	internal abstract class SafeRecordEnumeratorBase: IDisposable
	{
        private IResultSet _resultSet;
	    protected IEnumerator _baseEnumerator;
	    protected IRecord _currentRecord;
        private string _operation;
        private bool _ioError = false;

	    protected SafeRecordEnumeratorBase( IResultSet resultSet, string operation )
	    {
            _resultSet = resultSet;
            _operation = operation;
            try
            {
                _baseEnumerator = resultSet.GetEnumerator();
            }
            catch( IOException ex )
            {
                _ioError = true;
                _resultSet.Dispose();
                MyPalStorage.Storage.OnIOErrorDetected( ex );
                _baseEnumerator = null;
            }
	    }

        public bool MoveNext()
        {
            if ( _baseEnumerator == null )
            {
                return false;
            }
            while( true )
            {
                bool result;
                try
                {
                    result = _baseEnumerator.MoveNext();
                }
                catch( IOException ex )
                {
                    _ioError = true;
                    _resultSet.Dispose();
                    MyPalStorage.Storage.OnIOErrorDetected( ex );
                    return false;
                }
                catch( BadIndexesException )
                {
                    MyPalStorage.Storage.OnIndexCorruptionDetected( "Bad indexes found in " + _operation );
                    return false;
                }
                if ( !result )
                {
                    return false;
                }

                try
                {
                    LoadCurrentRecord();
                }
                catch( IOException ex )
                {
                    _ioError = true;
                    _resultSet.Dispose();
                    MyPalStorage.Storage.OnIOErrorDetected( ex );
                    return false;
                }
                catch( BadIndexesException )
                {
                    MyPalStorage.Storage.OnIndexCorruptionDetected( "Bad indexes found in " + _operation );
                    continue;
                }

                return true;
            }
        }

	    protected abstract void LoadCurrentRecord();

	    public void Dispose()
        {
            IDisposable disp = _baseEnumerator as IDisposable;
            if ( disp != null )
            {
                disp.Dispose();
            }
        }

	    public bool IOError
	    {
	        get { return _ioError; }
	    }
	}

    /// <summary>
    /// Enumerator which provides direct access to records.
    /// </summary>
    internal class SafeRecordEnumerator: SafeRecordEnumeratorBase
    {
        public SafeRecordEnumerator( IResultSet resultSet, string operation )
            : base( resultSet, operation )
        {
        }

        protected override void LoadCurrentRecord()
        {
            _currentRecord = (IRecord) _baseEnumerator.Current;
        }

        public IRecord Current
        {
            get { return _currentRecord; }
        }
    }

    /// <summary>
    /// Enumerator which provides access to values in records.
    /// </summary>
    internal class SafeRecordValueEnumerator: SafeRecordEnumeratorBase
    {
        private IRecordEnumerator _recordEnumerator;

        public SafeRecordValueEnumerator( IResultSet resultSet, string operation )
            : base( resultSet, operation )
        {
            _recordEnumerator = _baseEnumerator as IRecordEnumerator;
        }

        protected override void LoadCurrentRecord()
        {
            if ( _recordEnumerator == null )
            {
                _currentRecord = (IRecord) _baseEnumerator.Current;
            }
        }

        public object GetCurrentValue( int column )
        {
            if ( _recordEnumerator != null )
            {
                return _recordEnumerator.GetCurrentRecordValue( column );
            }
            return _currentRecord.GetValue( column );
        }

        public int GetCurrentIntValue( int column )
        {
            if ( _recordEnumerator != null )
            {
                return (int) _recordEnumerator.GetCurrentRecordValue( column );
            }
            return _currentRecord.GetIntValue( column );
        }
    }
}
