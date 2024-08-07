﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Threading;

namespace JetBrains.Omea.Base
{
    public class MTQueue : IDisposable
    {
        public MTQueue()
        {
            _queue = new Queue();
            _event = new ManualResetEvent( false );
        }

        public void push( object obj )
        {
            lock( _queue )
            {
                _queue.Enqueue( obj );
                _event.Set();
            }
        }

        public object pop_wait( int timeout )
        {
            object result = null;

            if( _event.WaitOne( timeout, false ) )
            {
                lock( _queue )
                {
                    result = _queue.Dequeue();
                    if( _queue.Count == 0 )
                    {
                        _event.Reset();
                    }
                }
            }

            return result;
        }

        #region IDisposable Members

        public void Dispose()
        {
            _event.Close();
        }

        #endregion

        private Queue               _queue;
        private ManualResetEvent    _event;
    }
}
