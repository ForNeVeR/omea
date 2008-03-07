/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#include "etableimpl.h"
#include "rowsetimpl.h"
#using <mscorlib.dll>
#include "ETable.h"
#include "guard.h"

EMAPILib::ETableImpl::ETableImpl( const ETableSPtr& mapiTable )
{
    if ( mapiTable.IsNull() )
    {
        Guard::ThrowArgumentNullException( "mapiTable" );
    }
    _mapiTable = mapiTable.CloneOnHeap();
}

EMAPILib::ETableImpl::~ETableImpl()
{
}
void EMAPILib::ETableImpl::Dispose()
{
    Disposable::DisposeImpl();
    TypeFactory::Delete( _mapiTable );
    _mapiTable = NULL;
}
void EMAPILib::ETableImpl::Sort( int tag, bool Asc )
{
    CheckDisposed();
    (*_mapiTable)->Sort( tag, Asc );
}

int EMAPILib::ETableImpl::GetRowCount()
{
    CheckDisposed();
    return (*_mapiTable)->GetRowCount();
}
EMAPILib::IERowSet* EMAPILib::ETableImpl::GetNextRow()
{
    CheckDisposed();
    return GetNextRows( 1 );
}
EMAPILib::IERowSet* EMAPILib::ETableImpl::GetNextRows( int count )
{
    CheckDisposed();
    ELPSRowSetSPtr rowSet = (*_mapiTable)->GetNextRows( count );
    if ( !rowSet.IsNull() && rowSet->GetCount() > 0 )
    {
        return new EMAPILib::RowSetImpl( rowSet );
    }
    return NULL;
}
