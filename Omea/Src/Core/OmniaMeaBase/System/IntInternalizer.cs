/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.Containers;

namespace JetBrains.Omea.Base
{
    public class IntInternalizer
    {
        private IntInternalizer() {}

        public static object Intern( int theInt )
        {
            object result;
            // Intern() never blocks current thread.
            // If it fails to enter the lock it just boxes the int.
            if( !_intCacheLock.TryEnter() )
            {
                result = theInt;
            }
            else 
            {
                try
                {
                    result = _intCache.TryKey( theInt );
                    if( result == null )
                    {
                        result = theInt;
                        _intCache.CacheObject( theInt, result );
                    }
                }
                finally
                {
                    _intCacheLock.Exit();
                }
            }
            return result;
        }

        static IntInternalizer()
        {
            _intCacheLock = new SpinWaitLock();
            _intCache = new IntObjectCache( _intCacheSize );
            for( int i = 0; i < _intCacheSize; ++i )
            {
                _intCache.CacheObject( i, i );
            }
        }

        private const int _intCacheSize = 8191;

        private static SpinWaitLock     _intCacheLock;
        private static IntObjectCache   _intCache;
    }
}