// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "StringStream.h"
#include "CharsStorage.h"
#include "AddrBook.h"
#include "ESPropValue.h"
#include "ETable.h"
#include "MailUser.h"
#include "Guard.h"

#include "RCPtrDef.h"
template RCPtr<ABContainer>;
template RCPtr<AddrBook>;

ABContainer::ABContainer( IABContainer* lpABContainer ) : MAPIProp( lpABContainer )
{
    if ( lpABContainer == NULL )
    {
        Guard::ThrowArgumentNullException( "lpABContainer" );
    }
    _lpABContainer = lpABContainer;
}

ABContainer::~ABContainer()
{
    _lpABContainer = NULL;
}

IABContainer* ABContainer::GetRaw() const
{
    return _lpABContainer;
}
MailUserSPtr ABContainer::OpenEntry( LPBYTE entryID, int cb ) const
{
    IMailUser* lpMailUser = NULL;
    ULONG lObjType = 0;
    HRESULT hr = _lpABContainer->OpenEntry( cb, (LPENTRYID)entryID,
        &IID_IMailUser, 0, &lObjType, (LPUNKNOWN*)&lpMailUser );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateMailUser( lpMailUser );
    }
    Guard::CheckHR( hr );
    return MailUserSPtr( NULL );
}
MailUserSPtr ABContainer::CreateEntry() const
{
    SizedSPropTagArray(2, TypeColumns) = {2, {PR_ADDRTYPE, PR_ENTRYID}};
    LPMAPITABLE lpTPLTable = NULL;
    HRESULT hr = _lpABContainer->OpenProperty( PR_CREATE_TEMPLATES,
                    (LPIID) &IID_IMAPITable, 0, 0, (LPUNKNOWN *)&lpTPLTable );

    Guard::CheckHR( hr );

    ETableSPtr table = TypeFactory::CreateETable( lpTPLTable );
    if ( table.IsNull() )
    {
        return MailUserSPtr( NULL );
    }
    hr = lpTPLTable->SetColumns((LPSPropTagArray)&TypeColumns, 0);
    Guard::CheckHR( hr );

    SRestriction srDisplayType;
    SPropValue spv;
    srDisplayType.rt = RES_PROPERTY;
    srDisplayType.res.resProperty.relop = RELOP_EQ;
    srDisplayType.res.resProperty.ulPropTag = PR_DISPLAY_TYPE;
    srDisplayType.res.resProperty.lpProp = &spv;
    spv.ulPropTag = PR_DISPLAY_TYPE;
    spv.Value.l = DT_MAILUSER;

    hr = lpTPLTable->Restrict( &srDisplayType, 0 );

    Guard::CheckHR( hr );

    ULONG ulCount;
    hr = lpTPLTable->GetRowCount( 0, &ulCount );

    Guard::CheckHR( hr );

    if ( ulCount == 1 )
    {
        ELPSRowSetSPtr prop = table->GetNextRow();
        if ( !prop.IsNull() )
        {
            LPMAPIPROP lpNewEntry = NULL;
            LPSPropValue lpProp = prop->GetProp( 1, 0 );
            if ( lpProp != NULL )
            {
                hr = _lpABContainer->CreateEntry( lpProp->Value.bin.cb, (ENTRYID*)lpProp->Value.bin.lpb,
                            CREATE_CHECK_DUP_LOOSE, &lpNewEntry );

                Guard::CheckHR( hr );
                if ( hr == S_OK && lpNewEntry != NULL )
                {
                    return TypeFactory::CreateMailUser( (IMailUser*)lpNewEntry );
                }
            }
        }
    }
    return MailUserSPtr( NULL );
}

ELPSRowSetSPtr ABContainer::GetRowSet() const
{
    LPMAPITABLE lpTable = NULL;
    HRESULT hr = _lpABContainer->GetContentsTable( 0, &lpTable );
    if ( hr == S_OK )
    {
        const SizedSPropTagArray( 4, atProps ) =
        { 4, (int)PR_ENTRYID,
            (int)PR_DISPLAY_NAME,
            (int)0x3003001E, //email address
            (int)PR_DISPLAY_TYPE};
        LPSRowSet rowSet = NULL;
        hr = HrQueryAllRows( lpTable, (LPSPropTagArray)&atProps, NULL, NULL, 0, &rowSet );
        lpTable->Release();
        if ( hr == S_OK )
        {
            return TypeFactory::CreateELPSRowSet( rowSet );
        }
    }
    return ELPSRowSetSPtr( NULL );
}

ETableSPtr ABContainer::GetTable() const
{
    LPMAPITABLE lpTable = NULL;
    HRESULT hr = _lpABContainer->GetContentsTable( 0, &lpTable );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateETable( lpTable );
    }
    return ETableSPtr( NULL );
}

AddrBook::AddrBook( LPADRBOOK lpAdrBook ) : MAPIProp( lpAdrBook )
{
    if ( lpAdrBook == NULL )
    {
        Guard::ThrowArgumentNullException( "lpAdrBook" );
    }
    _lpAdrBook = lpAdrBook;
}

AddrBook::~AddrBook()
{
    _lpAdrBook = NULL;
}

ELPSRowSetSPtr AddrBook::GetSearchPath() const
{
    LPSRowSet lpSRowSet = NULL;
    HRESULT hr = _lpAdrBook->GetSearchPath( 0, &lpSRowSet );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateELPSRowSet( lpSRowSet );
    }
    return ELPSRowSetSPtr( NULL );
}
ABContainerSPtr AddrBook::OpenEntry( LPBYTE entryID, int cb ) const
{
    IABContainer* lpABContainer = NULL;
    ULONG lObjType = 0;
    HRESULT hr = _lpAdrBook->OpenEntry( cb, (LPENTRYID)entryID,
        &IID_IABContainer, 0, &lObjType, (LPUNKNOWN*)&lpABContainer );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateABContainer( lpABContainer );
    }
    return ABContainerSPtr( NULL );
}

MailUserSPtr AddrBook::OpenMailUser( LPBYTE entryID, int cb ) const
{
    IMailUser* lpMailUser = NULL;
    ULONG lObjType = 0;
    HRESULT hr = _lpAdrBook->OpenEntry( cb, (LPENTRYID)entryID,
        &IID_IMailUser, 0, &lObjType, (LPUNKNOWN*)&lpMailUser );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateMailUser( lpMailUser );
    }
    return MailUserSPtr( NULL );
}
AddrBookSPtr AddrBook::OpenAddressBook( LPMAPISESSION lpSession )
{
    LPADRBOOK lpAdrBook = NULL;
    HRESULT hr = lpSession->OpenAddressBook( 0, NULL, (int)AB_NO_DIALOG, &lpAdrBook );
    if ( hr == S_OK || hr == MAPI_W_ERRORS_RETURNED )
    {
        if ( lpAdrBook != NULL )
        {
            return TypeFactory::CreateAddrBook( lpAdrBook );
        }
    }
    return AddrBookSPtr( NULL );
}
