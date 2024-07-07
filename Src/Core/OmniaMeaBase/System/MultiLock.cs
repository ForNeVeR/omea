// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Threading;

namespace JetBrains.Omea.Base
{
    /// <summary>
    /// Deadlock-safe multiple-object lock.
    /// </summary>
    public class MultiLock: IDisposable
	{
        private object _obj1, _obj2;

		public MultiLock( object obj1, object obj2 )
		{
            if ( obj2 == null )
            {
                _obj1 = obj1;
            }
            else if ( obj1 == null )
            {
                _obj1 = obj2;
            }
            else if ( obj1.GetHashCode() < obj2.GetHashCode() )
            {
                _obj1 = obj1;
                _obj2 = obj2;
            }
            else
            {
                _obj2 = obj1;
                _obj1 = obj2;
            }
            if ( _obj1 != null )
            {
                Monitor.Enter( _obj1 );
            }
            if ( _obj2 != null )
            {
                Monitor.Enter( _obj2 );
            }
		}

        public void Dispose()
        {
            if ( _obj2 != null )
            {
                Monitor.Exit( _obj2 );
            }
            if ( _obj1 != null )
            {
                Monitor.Exit( _obj1 );
            }
        }
	}
}
