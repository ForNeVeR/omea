// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Containers
{
    /**
     * A list where elements are positioned relative to each other using anchors.
     */

	public class AnchoredList: IEnumerable
	{
        // parallel lists of keys and values
        private ArrayList _keyList = new ArrayList();
        private ArrayList _valueList = new ArrayList();
        private bool _allowDuplicates = false;

	    public bool AllowDuplicates
	    {
	        get { return _allowDuplicates; }
	        set { _allowDuplicates = value; }
	    }

        public int Count
        {
            get { return _valueList.Count; }
        }

        public object this[int index]
        {
            get { return _valueList [index]; }
        }

	    public int Add( string key, object value, ListAnchor anchor )
        {
            if ( !_allowDuplicates && FindByKey( key ) != null )
                return -1;

            int index = FindAnchorIndex( anchor );
            if ( index == -1 )
            {
                Trace.WriteLine( "Couldn't find object to anchor to: " + anchor.RefId );
                index = _keyList.Count;
            }

            _keyList.Insert( index, key );
            _valueList.Insert( index, value );
            return index;
        }

        public string GetKey( int index )
        {
            return (string) _keyList [index];
        }

        public int IndexOf( object value )
        {
            return _valueList.IndexOf( value );
        }

        public void Remove( object value )
        {
            int index = IndexOf( value );
            if ( index >= 0 )
            {
                RemoveAt( index );
            }
        }

        public void RemoveAt( int index )
        {
            _keyList.RemoveAt( index );
            _valueList.RemoveAt( index );
        }

        public object FindByKey( string key )
        {
        	for( int i=0; i<_keyList.Count; i++ )
        	{
        		if ( _keyList [i].Equals( key ) )
                    return _valueList [i];
        	}
            return null;
        }

        public IEnumerator GetEnumerator()
        {
        	return _valueList.GetEnumerator();
        }

        private int FindAnchorIndex( ListAnchor anchor )
        {
        	if ( anchor.AnchorType == AnchorType.First )
        	{
        		return 0;
        	}

            if ( anchor.AnchorType == AnchorType.Last )
            {
            	return _keyList.Count;
            }

            if ( anchor.RefId == null )
                throw new ArgumentException( "RefID must be not null for Before and After anchors" );

            for( int i=0; i<_keyList.Count; i++ )
            {
            	if ( _keyList [i].Equals( anchor.RefId ) )
            	{
            		if ( anchor.AnchorType == AnchorType.Before )
                        return i;
                    return i+1;
            	}
            }
            return -1;
        }
	}
}
