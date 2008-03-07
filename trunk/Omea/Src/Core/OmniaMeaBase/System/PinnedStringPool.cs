/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Runtime.InteropServices;

namespace JetBrains.Omea.Base
{
    /// <summary>
    /// A pool which returns pinned GC handles for strings and keeps a limited number
    /// of strings pinned.
    /// </summary>
    
    public class PinnedStringPool: IDisposable
	{
		private int _size;
        private GCHandle[] _gcHandles;
        private int _freeIndex;
        private bool _reusing;
        
        /// <summary>
        /// Initializes the pool.
        /// </summary>
        /// <param name="size">
        ///   The maximum number of strings which can be held in the pool at the same time.
        /// </param>
        
        public PinnedStringPool( int size )
		{
            _size = size;
            _gcHandles = new GCHandle[ size ];
		}

        /// <summary>
        /// Converts the data of the specified string to a pinned null-terminated character
        /// array.
        /// </summary>
        /// <param name="str">String which should be pinned</param>
        /// <returns>Address of the pinned character array</returns>
        
        public IntPtr PinString( string str )
        {
            if ( _reusing )
            {
                _gcHandles [_freeIndex].Free();
            }

            char[] strChars = new char [str.Length+1];
            str.CopyTo( 0, strChars, 0, str.Length );
            strChars [str.Length] = '\0';

            _gcHandles [_freeIndex] = GCHandle.Alloc( strChars, GCHandleType.Pinned );
            IntPtr result = _gcHandles [_freeIndex].AddrOfPinnedObject();
            
            _freeIndex++;
            if ( _freeIndex == _gcHandles.Length )
            {
                _freeIndex = 0;
                _reusing = true;
            }

            return result;
        }

	    /// <summary>
	    /// Unpins all handles and clears the array.
	    /// </summary>
        
        public void Dispose()
        {
            // if _reusing is set, all handles have been used at least once
            int lastIndex = _reusing ? _size : _freeIndex;
            for( int i=0; i<lastIndex; i++ )
            {
                _gcHandles [i].Free();
            }
            _reusing = false;
            _freeIndex = 0;
	    }
    }
}
