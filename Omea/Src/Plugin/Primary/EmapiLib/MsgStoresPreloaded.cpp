/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma unmanaged

#include "msgstorespreloaded.h"
#include "EntryID.h"
#include "EMAPIFolder.h"
#include "ETable.h"
#include "EMessage.h"
#include "ESPropValue.h"
#include "Guard.h"
#include "MsgStore.h"
#include "RCPtrDef.h"

template RCPtr<MsgStores>;

#ifdef EMAPI_MANAGED
#pragma managed
#endif

MsgStores::MsgStores( LPMAPISESSION pSession ) : _pStoresTbl( NULL ), _pRowSet( NULL ), _storeCount( 0 )
{
    if ( pSession == NULL )
    {
        Guard::ThrowArgumentNullException( "pSession" );
    }
    _pSession = pSession;
    _pSession->AddRef();
    ::OutputDebugString( "MsgStoresPreloaded" );
}

MsgStores::~MsgStores()
{
    try
    {
        UlRelease( _pStoresTbl );
    }
    catch(...){}
    try
    {
        UlRelease( _pSession );
    }
    catch(...){}
    ::OutputDebugString( "~MsgStoresPreloaded" );
}
bool MsgStores::PrepareMsgStoresTable()
{
	HRESULT hRes = _pSession->GetMsgStoresTable( 0, &_pStoresTbl );
    if ( hRes != S_OK )
    {
        ::OutputDebugString( "_pSession->GetMsgStoresTable( 0, &_pStoresTbl ) != S_OK" );
    }
	return SUCCEEDED( hRes );
}

void MsgStores::FreeAllRows()
{
    if ( !_pRowSet.IsNull() )
    {
        _pRowSet.release();
    }
    _storeCount = 0;
}

int MsgStores::GetCount() const
{
    return _storeCount;
}

MsgStoresSPtr MsgStores::Get( LPMAPISESSION pSession )
{
    if ( pSession == NULL )
    {
        Guard::ThrowArgumentNullException( "pSession" );
    }
    return TypeFactory::CreateMsgStores( pSession );
}

MsgStoreSPtr MsgStores::OpenStorage( int index ) const
{
    OutputDebugString( "MsgStoresPreloaded::OpenStorage( int index )" );
    LPMDB pMDB = NULL;
    if ( _pRowSet.IsNull() ) 
    {
        return MsgStoreSPtr( NULL );
    }
    LPSPropValue lpProp = _pRowSet->GetProp( EID, index );
    if ( lpProp != NULL )
    {
        OutputDebugString( "MsgStoresPreloaded::OpenStorage( int index ) 1" );
        HRESULT hRes = _pSession->OpenMsgStore( 0, lpProp->Value.bin.cb, (LPENTRYID)lpProp->Value.bin.lpb, NULL, (int)MDB_WRITE, &pMDB );
        if ( hRes == (int)MAPI_E_UNKNOWN_LCID || hRes == (int)MAPI_E_FAILONEPROVIDER || 
            hRes == MAPI_E_UNCONFIGURED || hRes == MAPI_E_NOT_FOUND || hRes == MAPI_E_USER_CANCEL )
        {
            if ( pMDB != NULL )
            {
                pMDB->Release();
            }
            if ( hRes == MAPI_E_USER_CANCEL || hRes == MAPI_E_UNCONFIGURED )
            {
                Guard::ThrowProblemWhenOpenStorage( hRes, lpProp );
            }
            return MsgStoreSPtr( NULL );
        }
        OutputDebugString( "Helper::CheckHR( hRes ); OK" );

        if ( SUCCEEDED( hRes ) && pMDB != NULL && _pSession != NULL )
        {
            return TypeFactory::CreateMsgStore( pMDB, _pSession );
        }
    }
    return MsgStoreSPtr( NULL );
}
LPSPropValue MsgStores::GetStorageID( int index ) const
{
    if ( _pRowSet.IsNull() )
    {
        ::OutputDebugString( "_pRowSet.IsNull()" );
        return NULL;
    }
    LPSPropValue lpProp = _pRowSet->GetProp( EID, index );
    if ( lpProp != NULL )
    {
        if ( lpProp->Value.bin.lpb != NULL )
        {
            return lpProp;
        }
    }
    return NULL;
}
LPSTR MsgStores::GetDisplayName( int index ) const
{
    LPSPropValue lpProp = _pRowSet->GetProp( NAME, index );
    if ( lpProp != NULL )
    {
        if ( lpProp->Value.lpszA != NULL )
        {
            return lpProp->Value.lpszA;
        }
    }
    return NULL;
}

bool MsgStores::IsDefaultStore( int index ) const
{
    LPSPropValue lpProp = _pRowSet->GetProp( 2, index );
    if ( lpProp != NULL )
    {
        return ( lpProp->Value.b == 1 );
    }
    return false;
}

bool MsgStores::QueryAllRows()
{
    FreeAllRows();
    const SizedSPropTagArray( NUM_COLS, sptCols ) = 
    { NUM_COLS, (int)PR_ENTRYID, (int)PR_DISPLAY_NAME, (int)PR_DEFAULT_STORE };

    LPSRowSet lpSRowSet = NULL;

	HRESULT hRes = HrQueryAllRows(
		_pStoresTbl,					//table to query
		(LPSPropTagArray) &sptCols,	//columns to get
		//&sres,						//restriction to use
		NULL,						//restriction to use
		NULL,						//sort order
		0,							//max number of rows
		&lpSRowSet );

    if ( SUCCEEDED( hRes ) )
    {
        _pRowSet = TypeFactory::CreateELPSRowSet( lpSRowSet );
        _storeCount = _pRowSet->GetCount();
    }
	return SUCCEEDED( hRes );
}
