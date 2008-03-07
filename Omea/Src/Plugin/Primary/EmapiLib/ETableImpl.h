/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma once
#include "helpers.h"
#using <mscorlib.dll>

namespace EMAPILib
{
    public __gc class ETableImpl : public EMAPILib::IETable, public EMAPILib::Disposable
    {
    private:
        ETableSPtr* _mapiTable;
    public:
        ETableImpl( const ETableSPtr& mapiTable );
        virtual ~ETableImpl();
        virtual void Sort( int tag, bool Asc );
        virtual int GetRowCount();
        virtual IERowSet* GetNextRow();
        virtual IERowSet* GetNextRows( int count );
        virtual void Dispose();
    };
}