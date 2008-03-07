/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.OpenAPI;
using System.Collections;
using System.Text;

namespace JetBrains.Omea.ResourceStore
{
	/**
     * The list of values of a string list property.
     */
    
    internal class PropertyStringList: IStringList
	{
        private Resource _resource;
        private int _propID;
        private ArrayList _values = new ArrayList();
        private bool _instantiated;
        private bool _transient;

	    public PropertyStringList( Resource res, int propID, bool instantiated, bool transient )
	    {
	        _resource = res;
            _propID = propID;
            _instantiated = instantiated;
            _transient = transient;
	    }

	    public int Count
        {
            get 
            { 
                Instantiate();
                lock( this )
                {
                    return _values.Count; 
                }
            }
        }

        public string this [int index]
        {
            get 
            { 
                Instantiate();

                lock( this )
                {
                    return (string) _values [index]; 
                }
            }
        }

        internal bool IsInstantiated
        {
            get { return _instantiated; }
        }

        internal void SetInstantiated()
        {
            _instantiated = true;
        }

        public void Dispose()
        {
            _resource.Lock();
            try
            {
                if ( _instantiated )
                {
                    lock( this )
                    {
                        _values.Clear();
                    }
                    _instantiated = false;
                }
            }
            finally
            {
                _resource.UnLock();
            }
        }

        private void Instantiate()
        {
            if ( !_instantiated )
            {
                _resource.Lock();
                try
                {
                    if ( !_instantiated )
                    {
                        _resource.LoadStringListProperties( this, _propID );
                        _instantiated = true;
                    }
                }
                finally
                {
                    _resource.UnLock();
                }
            }
        }

        public void Add( string str )
        {
            if ( _instantiated )
            {
                lock( this )
                {
                    _values.Add( str );
                }
            }
            if ( !_transient )
            {
                MyPalStorage.Storage.CreateProperty( _resource, _propID, str );
                MyPalStorage.Storage.OnResourceSaved( _resource, _propID, null );
            }
        }

        public void Remove( string str )
        {
            int index;
            Instantiate();
            lock( this )
            {
                index = _values.IndexOf( str );
                if ( index >= 0 )
                {
                    _values.RemoveAt( index );
                }
            }
            if ( !_transient && index >= 0 )
            {
                MyPalStorage.Storage.DeleteProperty( _resource, _propID, index );
                MyPalStorage.Storage.OnResourceSaved( _resource, _propID, null );
            }
        }

        public void RemoveAt( int index )
        {
            if ( _instantiated )
            {
                lock( this )
                {
                    _values.RemoveAt( index );
                }
            }
            if ( !_transient )
            {
                MyPalStorage.Storage.DeleteProperty( _resource, _propID, index );
                MyPalStorage.Storage.OnResourceSaved( _resource, _propID, null );
            }
        }

        public void CommitTransient()
        {
            lock( this )
            {
                foreach( string str in _values )
                {
                    MyPalStorage.Storage.CreateProperty( _resource, _propID, str );
                }
            }
            _transient = false;
        }

        public void Clear()
        {
            if ( _instantiated && _values.Count > 0 )
            {
                lock( this )
                {
                    _values.Clear();
                }
            }
            if ( !_transient )
            {
                MyPalStorage.Storage.DeleteProperty( _resource, _propID );
                MyPalStorage.Storage.OnResourceSaved( _resource, _propID, null );
            }
        }

        public int IndexOf( string str )
        {                            
            Instantiate();
            lock( this )
            {
                return _values.IndexOf( str );
            }
        }

        internal int IndexOfInsensitive( string str )
        {
            Instantiate();
            lock( this )
            {
                for( int i=0; i<_values.Count; i++ )
                {
                    if ( String.Compare( (string) _values [i], str, true ) == 0 )
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        internal void AddValue( string str )
        {
            lock( this )
            {
                _values.Add( str );
            }
        }

        public override string ToString()
        {
            Instantiate();
            lock( this )
            {
                if ( _values.Count == 0 )
                    return "";

                if ( _values.Count == 1 )
                    return (string) _values [0];

                StringBuilder builder = new StringBuilder( (string) _values [0] );
                for( int i=1; i<_values.Count; i++ )
                {
                    builder.Append( ", " );
                    builder.Append( _values [i] );
                }
                return builder.ToString();
            }
        }

        public IEnumerator GetEnumerator()
        {
            Instantiate();
            return _values.GetEnumerator();
        }

        public int EstimateMemorySize()
        {
            lock( this )
            {
                int result = 24 + _values.Capacity * 4;
                foreach( string str in _values )
                {
                    result += 20 + 2 * str.Length;
                }
                return result;
            }
        }
	}
}
