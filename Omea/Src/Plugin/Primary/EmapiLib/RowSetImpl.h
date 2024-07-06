// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once
#include "helpers.h"
#include "typefactory.h"
#using <mscorlib.dll>

namespace EMAPILib
{
    public __gc class RowSetImpl : public IERowSet, public Disposable
    {
    private:
        ELPSRowSetSPtr* _rowSet;
    public:
        RowSetImpl( const ELPSRowSetSPtr& rowSet );
        virtual ~RowSetImpl();

        virtual int GetRowCount();

        virtual String* GetBinProp( int index );
        virtual String* GetStringProp( int index );
        virtual DateTime GetDateTimeProp( int index );
        virtual int GetLongProp( int index );

        virtual String* GetBinProp( int index, int rowNum );
        virtual String* GetStringProp( int index, int rowNum );
        virtual DateTime GetDateTimeProp( int index, int rowNum );
        virtual int GetLongProp( int index, int rowNum );

        virtual String* FindStringProp( int tag );
        virtual String* FindBinProp( int tag );
        virtual int FindLongProp( int tag );
        virtual void Dispose();
    };
}
