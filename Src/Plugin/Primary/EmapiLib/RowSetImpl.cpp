// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include "rowsetimpl.h"
#using <mscorlib.dll>
#include "ESPropValue.h"
#include "ETable.h"
#include "guard.h"

EMAPILib::RowSetImpl::RowSetImpl( const ELPSRowSetSPtr& rowSet )
{
    if ( rowSet.IsNull() )
    {
        Guard::ThrowArgumentNullException( "rowSet" );
    }
    _rowSet = rowSet.CloneOnHeap();
}
EMAPILib::RowSetImpl::~RowSetImpl()
{
}
void EMAPILib::RowSetImpl::Dispose()
{
    Disposable::DisposeImpl();
    TypeFactory::Delete( _rowSet );
    _rowSet = NULL;
}
int EMAPILib::RowSetImpl::GetRowCount( )
{
    return (*_rowSet)->GetCount();
}

String* EMAPILib::RowSetImpl::FindBinProp( int tag )
{
    CheckDisposed();
    ESPropValueSPtr prop = (*_rowSet)->FindProp( tag );
    if ( !prop.IsNull() && prop->GetBinLPBYTE() != NULL && prop->GetBinCB() != 0 )
    {
        return Helper::EntryIDToHex( prop->GetBinLPBYTE(), prop->GetBinCB() );
    }
    return NULL;
}

String* EMAPILib::RowSetImpl::GetBinProp( int index )
{
    CheckDisposed();
    return GetBinProp( index, 0 );
}
String* EMAPILib::RowSetImpl::GetBinProp( int index, int rowNum )
{
    CheckDisposed();
    LPSPropValue lpsPropVal = (*_rowSet)->GetProp( index, rowNum );
    if ( lpsPropVal != NULL )
    {
        String* ret = NULL;
        if ( lpsPropVal->Value.bin.lpb != NULL )
        {
            ret = Helper::EntryIDToHex( lpsPropVal->Value.bin.lpb, lpsPropVal->Value.bin.cb );
        }
        return ret;
    }
    return NULL;
}
String* EMAPILib::RowSetImpl::GetStringProp( int index )
{
    CheckDisposed();
    return GetStringProp( index, 0 );
}
String* EMAPILib::RowSetImpl::GetStringProp( int index, int rowNum )
{
    CheckDisposed();
    LPSPropValue lpsPropVal = (*_rowSet)->GetProp( index, rowNum );
	String* ret = NULL;
    if( lpsPropVal != NULL && lpsPropVal->Value.err != (int)MAPI_E_NOT_FOUND && lpsPropVal->Value.lpszA != NULL )
    {
        ret = new String( lpsPropVal->Value.lpszA );
	}
	return ret;
}
DateTime EMAPILib::RowSetImpl::GetDateTimeProp( int index )
{
    CheckDisposed();
    return GetDateTimeProp( index, 0 );
}
DateTime EMAPILib::RowSetImpl::GetDateTimeProp( int index, int rowNum )
{
    CheckDisposed();
    LPSPropValue lpsPropVal = (*_rowSet)->GetProp( index, rowNum );
    if ( lpsPropVal != NULL )
    {
        _FILETIME ft = lpsPropVal->Value.ft;
        ULONGLONG dt = (((ULONGLONG) ft.dwHighDateTime) << 32) + ft.dwLowDateTime;
        return DateTime::FromFileTime( dt );
    }
    return DateTime::MinValue;
}
int EMAPILib::RowSetImpl::GetLongProp( int index )
{
    CheckDisposed();
    return GetLongProp( index, 0 );
}
int EMAPILib::RowSetImpl::GetLongProp( int index, int rowNum )
{
    CheckDisposed();
    LPSPropValue lpsPropVal = (*_rowSet)->GetProp( index, rowNum );
    if ( lpsPropVal != NULL )
    {
        int ret = lpsPropVal->Value.l;
        return ret;
    }
    return 0;
}

String* EMAPILib::RowSetImpl::FindStringProp( int tag )
{
    CheckDisposed();
    ESPropValueSPtr prop = (*_rowSet)->FindProp( tag );
    if ( !prop.IsNull() && prop->GetLPSTR() != NULL )
    {
        return new String( prop->GetLPSTR() );
    }
    return NULL;
}
int EMAPILib::RowSetImpl::FindLongProp( int tag )
{
    CheckDisposed();
    ESPropValueSPtr prop = (*_rowSet)->FindProp( tag );
    if ( !prop.IsNull() )
    {
        return prop->GetLong();
    }
    return 0;
}
