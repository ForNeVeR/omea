// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once
#include "helpers.h"
#using <mscorlib.dll>

namespace EMAPILib
{
    public ref class ETableImpl : public EMAPILib::IETable, public EMAPILib::Disposable
    {
    private:
        ETableSPtr* _mapiTable;
    public:
        ETableImpl( const ETableSPtr& mapiTable );
        virtual void Sort( int tag, bool Asc );
        virtual int GetRowCount();
        virtual IERowSet^ GetNextRow();
        virtual IERowSet^ GetNextRows( int count );
        virtual ~ETableImpl();
    };
}
