// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.DataStructures;

namespace JetBrains.Omea.Containers
{
    /**
     * Class which allows to count distinct instances of objects.
     */

    public class CountedSet: IEnumerable
	{
        public class Entry
        {
            private object _value;
            private int _count;

            internal Entry( object val )
            {
                _value = val;
                _count = 1;
            }

            internal void IncCount()
            {
                _count++;
            }

            public object Value
            {
                get { return _value; }
            }

            public int Count
            {
                get { return _count; }
            }
        }

        private class EntryCountComparer: IComparer
        {
            public int Compare( object x, object y )
            {
                Entry e1 = (Entry) x;
                Entry e2 = (Entry) y;
                return e2.Count - e1.Count;
            }
        }


        private ArrayList _entryList = new ArrayList();
        private HashMap _entryMap = new HashMap();

        public void Add( object val )
        {
            if ( val == null )
                throw new ArgumentNullException( "val" );

            Entry e = (Entry) _entryMap [val];
            if ( e == null )
            {
                e = new Entry( val );
                _entryList.Add( e );
                _entryMap [val] = e;
            }
            else
            {
                e.IncCount();
            }
        }

        public int Count
        {
            get { return _entryList.Count; }
        }

        public void SortByCount()
        {
            _entryList.Sort( new EntryCountComparer() );
        }

        public int this [object val]
        {
            get
            {
                foreach( Entry e in _entryList )
                {
                    if ( e.Value == val )
                        return e.Count;
                }
                return 0;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _entryList.GetEnumerator();
        }
	}
}
