// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "etable.h"
#include "ESPropValue.h"
#include "Guard.h"

#include "RCPtrDef.h"
template RCPtr<ELPSRowSet>;
template RCPtr<ETable>;

#ifdef EMAPI_MANAGED
#pragma managed
#endif

ELPSRowSet::ELPSRowSet( LPSRowSet lpSRowSet )
{
    if ( lpSRowSet == NULL )
    {
        Guard::ThrowArgumentNullException( "lpSRowSet" );
    }
    _lpSRowSet = lpSRowSet;
}
ELPSRowSet::~ELPSRowSet()
{
    try
    {
        FreeProws( _lpSRowSet );
    }
    catch(...){}
}
LPSRowSet ELPSRowSet::GetRaw() const
{
    return _lpSRowSet;
}
int ELPSRowSet::GetCount() const
{
    return _lpSRowSet->cRows;
}
ESPropValueSPtr ELPSRowSet::FindProp( int tag ) const
{
    return FindProp( tag, 0 );
}

ESPropValueSPtr ELPSRowSet::FindProp( int tag, int rowNum ) const
{
    if ( rowNum >= (int)_lpSRowSet->cRows )
    {
        return ESPropValueSPtr( NULL );
    }
    LPSPropValue lpsPropVal = NULL;
    lpsPropVal = PpropFindProp( _lpSRowSet->aRow[rowNum].lpProps, _lpSRowSet->aRow[rowNum].cValues, tag );
    if ( NULL != lpsPropVal )
    {
        if ( lpsPropVal->Value.err != (int)MAPI_E_NOT_FOUND )
        {
            return TypeFactory::CreateESPropValue( lpsPropVal, false );
        }
    }
    return ESPropValueSPtr( NULL );
}

LPSPropValue ELPSRowSet::GetProp( int index ) const
{
    return GetProp( index, 0 );
}

LPSPropValue ELPSRowSet::GetProp( int index, int rowNum ) const
{
    LPSPropValue lpsPropVal = &(_lpSRowSet->aRow[rowNum].lpProps[index]);
    if ( lpsPropVal != NULL && lpsPropVal->Value.err != (int)MAPI_E_NOT_FOUND )
    {
        return lpsPropVal;
    }
    return NULL;
}

ETable::ETable( LPMAPITABLE lpMAPITable )
{
    if ( lpMAPITable == NULL )
    {
        Guard::ThrowArgumentNullException( "lpMAPITable" );
    }
    _lpMAPITable = lpMAPITable;
}

ETable::~ETable(void)
{
    try
    {
        UlRelease( _lpMAPITable );
    }
    catch(...){}
}
HRESULT ETable::Sort( int tag, bool Asc ) const
{
    int sortOrder = (int)TABLE_SORT_DESCEND;
    if ( Asc )
    {
        sortOrder = (int)TABLE_SORT_ASCEND;
    }
    const SizedSSortOrderSet( 1L, SortTable ) = { 1L, 0L, 0L, { tag, sortOrder }};
    HRESULT hr = _lpMAPITable->SortTable( (LPSSortOrderSet)&SortTable, (int)TBL_ASYNC );
    return hr;
}

int ETable::GetRowCount() const
{
    ULONG uRowCnt = 0;
    HRESULT hr = _lpMAPITable->GetRowCount( 0, &uRowCnt );
    if ( hr == S_OK )
    {
        return uRowCnt;
    }
    return 0;
}

ELPSRowSetSPtr ETable::GetNextRow() const
{
    return GetNextRows( 1 );
}

ELPSRowSetSPtr ETable::GetNextRows( int count ) const
{
    LPSRowSet lpSRowSet = NULL;
    HRESULT hr = _lpMAPITable->QueryRows( count, 0, &lpSRowSet );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateELPSRowSet( lpSRowSet );
    }
    return ELPSRowSetSPtr( NULL );
}

HRESULT ETable::SetColumns( LPSPropTagArray tagArray ) const
{
    return _lpMAPITable->SetColumns( tagArray, 0 );
}
HRESULT ETable::SetRestriction( LPSRestriction restriction ) const
{
    return _lpMAPITable->Restrict( restriction, 0 );
}
