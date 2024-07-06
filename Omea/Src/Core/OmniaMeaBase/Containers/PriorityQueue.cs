// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.Containers
{
    /**
     * default priority queue works with int priority values
     * the more priority, the faster an object is popped
     * NOTE: specific feature of the PriorityQueue is that objects pushed with
     * the same priority are popped in the sequence of pushing
     * The feature is guaranteed by properties of encapsulated RedBlackTree
     */
    public class PriorityQueue: IEnumerable
    {
        public PriorityQueue()
        {
            _tree = new RedBlackTree( new QueueEntryComparer() );
        }

        public void Push( int priority, object obj )
        {
            QueueEntry entry = AllocEntry();
            entry.SetEntry( priority, obj, _tree._null );
            _tree.RB_InsertNode( entry );
        }
        public object Pop()
        {
            QueueEntry node = (QueueEntry) _tree.GetMinimumNode();
            if( node == null )
            {
                return null;
            }
            object result = node.Value;
            _tree.RB_Delete( node );
            _freeNode = node;
            return result;
        }
        public void Remove( QueueEntry entry )
        {
            _tree.RB_Delete( entry );
        }
        public int Count
        {
            get { return _tree.Count; }
        }

        public QueueEntry PopEntry()
        {
            QueueEntry node = (QueueEntry) _tree.GetMinimumNode();
            if( node == null )
            {
                return null;
            }
            _tree.RB_Delete( node );
            return node;
        }

        #region IEnumerable Members

        private class PriorityQueueEnumerator: IEnumerator
        {
            private RedBlackTree    _tree;
            private QueueEntry      _queueEntry;

            internal PriorityQueueEnumerator( RedBlackTree tree )
            {
                _tree = tree;
                Reset();
            }
            #region IEnumerator Members

            public void Reset()
            {
                _queueEntry = null;
            }

            public object Current
            {
                get
                {
                    return _queueEntry;
                }
            }

            public bool MoveNext()
            {
                if( _queueEntry == null )
                {
                    _queueEntry = _tree.GetMinimumNode() as QueueEntry;
                }
                else
                {
                    _queueEntry = _tree.GetSuccessor( _queueEntry ) as QueueEntry;
                }
                return _queueEntry != null;
            }

            #endregion
        }

        public IEnumerator GetEnumerator()
        {
            return new PriorityQueueEnumerator( _tree );
        }

        #endregion

        /// <summary>
        /// Priority queue entry inherits RBNodeBase
        /// </summary>
        public class QueueEntry : RBNodeBase
        {
            private int _priority;
            private object _object;

            public void SetEntry( int priority, object obj, RBNodeBase nullNode )
            {
                _priority = priority;
                _object = obj;
                _right = _left = _parent = nullNode;
            }

            public int CompareTo( object obj )
            {
                QueueEntry entry = (QueueEntry) obj;
                return entry._priority - _priority;
            }

            public override object Key
            {
                get { return IntInternalizer.Intern( _priority ); }
                set { _priority = (int) value; }
            }

            public int    Priority { get { return _priority; } }
            public object Value    { get { return _object; } }
        }

        protected class QueueEntryComparer : IComparer
        {
            #region IComparer Members

            public int Compare( object x, object y )
            {
                return (int) y - (int) x;
            }

            #endregion
        }

        private QueueEntry AllocEntry()
        {
            QueueEntry result = _freeNode;
            if( result == null )
            {
                result = new QueueEntry();
            }
            else
            {
                _freeNode = null;
            }
            return result;
        }

        protected RedBlackTree  _tree;
        protected QueueEntry    _freeNode;
    }

    /**
     * priority queue with DateTime priority values
     * the less priority (date & time), the faster an object is popped
     * uses partially ordered tree instead of red-black tree (less memory),
     * so it doesn't guarantee any order for objects with the same date & time
     */
    public class DateTimePriorityQueue
    {
        public DateTimePriorityQueue()
        {
            _poTree = new ArrayList();
        }

        public void Push( DateTime priority, object obj )
        {
            int i = _poTree.Count;
            QueueEntry newEntry = new QueueEntry( priority, obj );
            _poTree.Add( newEntry );
            while( i > 0 )
            {
                int parent = ( i - 1 ) >> 1;
                QueueEntry E = (QueueEntry) _poTree[ parent ];
                if( E.Priority <= priority )
                {
                    break;
                }
                _poTree[ parent ] = newEntry;
                _poTree[ i ] = E;
                i = parent;
            }
        }
        public object Pop()
        {
            QueueEntry minEntry = PopEntry();
            return ( minEntry == null ) ? null : minEntry.Value;
        }
        public int Count
        {
            get { return _poTree.Count; }
        }

        public QueueEntry PopEntry()
        {
            QueueEntry result = GetMinimumEntry();
            if( result != null )
            {
                int lastIndex = _poTree.Count - 1;
                QueueEntry E = (QueueEntry) _poTree[ lastIndex ];
                _poTree.RemoveAt( lastIndex );
                if( _poTree.Count == 0 )
                {
                    _poTree.TrimToSize();
                }
                else
                {
                    _poTree[ 0 ] = E;
                    int i = 0;
                    int left;
                    while( ( left = ( i << 1 ) + 1 ) < _poTree.Count )
                    {
                        QueueEntry leftEntry = (QueueEntry) _poTree[ left ];
                        if( left + 1 < _poTree.Count )
                        {
                            QueueEntry rightEntry = (QueueEntry) _poTree[ left + 1 ];
                            if( rightEntry.Priority < leftEntry.Priority )
                            {
                                leftEntry = rightEntry;
                                ++left;
                            }
                        }
                        if( E.Priority <= leftEntry.Priority )
                        {
                            break;
                        }
                        _poTree[ left ] = E;
                        _poTree[ i ] = leftEntry;
                        i = left;
                    }
                }
            }
            return result;
        }

        public QueueEntry GetMinimumEntry()
        {
            return ( _poTree.Count == 0 ) ? null : ( (QueueEntry) _poTree[ 0 ] );
        }

        public class QueueEntry
        {
            private DateTime _priority;
            private object _object;
            public QueueEntry( DateTime priority, object obj )
            {
                _priority = priority;
                _object = obj;
            }

            public DateTime Priority { get { return _priority; } }
            public object   Value    { get { return _object; } }
        }

        /**
         * plain table representation of partially ordered tree
         */
        protected internal ArrayList _poTree;
    }
}
