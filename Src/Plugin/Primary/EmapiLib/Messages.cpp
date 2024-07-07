// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "Messages.h"
#include "EMessage.h"
#include "guard.h"
#include "RCPtrDef.h"

template RCPtr<Messages>;

Messages::Messages( LPMAPIFOLDER lpFolder ) : _count( 0 )
{
    _lpFolder = lpFolder;
    _lpFolder->AddRef();
    _pTable = NULL;
    _pRows = NULL;

    HRESULT hr = _lpFolder->GetContentsTable( 0, &_pTable );
    //Guard::CheckHR( hr ); //can be access denied
    if ( SUCCEEDED(hr) )
    {

        const SizedSPropTagArray( 5, atProp ) = { 5, (int)PR_ENTRYID, (int)PR_SUBJECT, (int)PR_SENT_REPRESENTING_NAME, (int)PR_MESSAGE_DELIVERY_TIME, (int)PR_TRANSPORT_MESSAGE_HEADERS };

        hr = HrQueryAllRows( _pTable, (SPropTagArray*)&atProp, 0, 0, 0, &_pRows );
        //Guard::CheckHR( hr ); //can be access denied
        if ( SUCCEEDED( hr ) )
        {
            _count = _pRows->cRows;
        }
        else
        {
            _count = 0;
        }
    }
}

Messages::~Messages()
{
    if ( _pRows != NULL )
    {
        try
        {
            FreeProws( _pRows );
        }
        catch(...){}
    }
    if ( _pTable != NULL )
    {
        try
        {
            UlRelease( _pTable );
        }
        catch(...){}
    }
    try
    {
        UlRelease( _lpFolder );
    }
    catch(...){}
}
EMessageSPtr Messages::GetMessage( int index ) const
{
    LPSPropValue pVal = _pRows->aRow[index].lpProps;
    LPMESSAGE lpMessage = NULL;
    unsigned long ulObjectType = 0;
    HRESULT hr = _lpFolder->OpenEntry( pVal[0].Value.bin.cb, (LPENTRYID)pVal[0].Value.bin.lpb,
        0, (int)TEST_MAPI_MODIFY, &ulObjectType, (LPUNKNOWN*)&lpMessage );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateEMessage( lpMessage );
    }
    return EMessageSPtr( NULL );
}

int Messages::GetCount() const
{
    return _count;
}
