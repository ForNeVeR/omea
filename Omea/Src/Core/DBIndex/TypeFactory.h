/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#ifndef _OMEA_TYPEFACTORY_H
#define _OMEA_TYPEFACTORY_H

#include "BTreeKey.h"
#include "BTreePage.h"
#include "BTreeHeader.h"


namespace DBIndex
{
// substitute 'long' with the __int64 microsoft specific keyword
#ifdef _MSC_VER
#define long __int64
#endif

	enum {	unknown_Key,
		    int_Key,
			long_Key,
			datetime_Key,
			double_Key,
			int_int_Key,
			int_long_Key,
			int_datetime_Key,
			long_int_Key,
			long_long_Key,
			int_int_int_Key,
			int_int_datetime_Key,
			int_datetime_int_Key
	};

	class TypeFactory
	{
	public:

		static BTreeKeyBase*			NewKey( int type );
		static BTreePageBase*			NewPage( int type );
		static BTreeHeaderBase*			NewHeader( int type );
		static BTreeHeaderIteratorBase*	NewHeaderIterator( int type );
		static IKeyComparer*			NewKeyComparer( int type );

		static void	DeleteKey( BTreeKeyBase* );
		static void	DeletePage( BTreePageBase* );
		static void	DeleteHeader( BTreeHeaderBase* );
		static void	DeleteHeaderIterator( BTreeHeaderIteratorBase* );
		static void	DeleteKeyComparer( IKeyComparer* );
	};
}

#endif