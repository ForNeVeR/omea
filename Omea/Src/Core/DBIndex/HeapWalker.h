/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#ifndef _OMEA_HEAP_WALKER_H
#define _OMEA_HEAP_WALKER_H

using namespace System;

namespace DBIndex
{
	class Win32HeapWalker
	{
	public:
		Win32HeapWalker();

		unsigned HeapCount() const;
		unsigned HeapTotalSize() const;
		unsigned DBIndexMemUsage() const;
		void Dump( const char* filename ) const;
	};

	public __gc class Win32Heaps
	{
	public:
		static unsigned HeapCount()
		{
			if( _walker == 0 )
			{
				_walker = new Win32HeapWalker();
			}
			return _walker->HeapCount();
		}
		static unsigned TotalHeapSize()
		{
			if( _walker == 0 )
			{
				_walker = new Win32HeapWalker();
			}
			return _walker->HeapTotalSize();
		}
		static void Dump( String* filename );
	private:
		static Win32HeapWalker* _walker;
	};
}

#endif