// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "typefactory.h"
#include "emapiFolder.h"
#include "MsgStore.h"
#include "MsgStoresPreloaded.h"
#include "AddrBook.h"
#include "FormManager.h"
#include "MailUser.h"
#include "EntryID.h"
#include "EMessage.h"
#include "Messages.h"
#include "ETable.h"
#include "CharsStorage.h"
#include "CharBuffer.h"
#include "EAttach.h"
#include "StringStream.h"
#include "ESPropValue.h"
#include "StringConvertion.h"
#include "MAPISession.h"

#ifdef EMAPI_MANAGED
#pragma managed
#endif

PropTagArraySPtr TypeFactory::CreatePropTagArray( LPSPropTagArray propTags )
{
    return PropTagArraySPtr( new PropTagArray( propTags ) );
}

EMAPIFolderSPtr TypeFactory::CreateEMAPIFolder( LPMAPIFOLDER pFolder )
{
    if ( pFolder == NULL )
    {
        return EMAPIFolderSPtr( NULL );
    }
    return EMAPIFolderSPtr( new EMAPIFolder( pFolder ) );
}
EMAPIFoldersSPtr TypeFactory::CreateEMAPIFolders( LPMAPIFOLDER pFolder, LPMAPITABLE pTable )
{
    if ( pFolder != NULL )
    {
        if ( pTable != NULL )
        {
            return EMAPIFoldersSPtr( new EMAPIFolders( pFolder, pTable ) );
        }
    }
    return EMAPIFoldersSPtr( NULL );
}

ESPropValueSPtr TypeFactory::CreateESPropValue( LPSPropValue pVal, bool isFreeNecessary )
{
    if ( pVal == NULL )
    {
        return ESPropValueSPtr( NULL );
    }
    return ESPropValueSPtr( new ESPropValue( pVal, isFreeNecessary ) );
}
CharBufferSPtr TypeFactory::CreateCharBuffer( int length )
{
    return CharBufferSPtr( new CharBuffer( length ) );
}
CharsStorageSPtr TypeFactory::CreateCharsStorage()
{
    return CharsStorageSPtr( new CharsStorage() );
}

StringStreamSPtr TypeFactory::CreateStringStream( LPSTREAM lpStream )
{
    if ( lpStream == NULL )
    {
        return StringStreamSPtr( NULL );
    }
    return StringStreamSPtr( new StringStream( lpStream ) );
}

EAttachSPtr TypeFactory::CreateEAttach( LPATTACH lpAttach )
{
    if ( lpAttach == NULL )
    {
        return EAttachSPtr( NULL );
    }
    return EAttachSPtr( new EAttach( lpAttach ) );
}
MailUserSPtr TypeFactory::CreateMailUser( IMailUser* lpMailUser )
{
    if ( lpMailUser == NULL )
    {
        return MailUserSPtr( NULL );
    }
    return MailUserSPtr( new MailUser( lpMailUser ) );
}
EntryIDSPtr TypeFactory::CreateEntryID( LPBYTE bytes, int cb )
{
    return EntryIDSPtr( new EntryID( bytes, cb ) );
}
ELPSRowSetSPtr TypeFactory::CreateELPSRowSet( LPSRowSet lpSRowSet )
{
    if ( lpSRowSet == NULL )
    {
        return ELPSRowSetSPtr( NULL );
    }
    return ELPSRowSetSPtr( new ELPSRowSet( lpSRowSet ) );
}
ETableSPtr TypeFactory::CreateETable( LPMAPITABLE lpMAPITable )
{
    if ( lpMAPITable == NULL )
    {
        return ETableSPtr( NULL );
    }
    return ETableSPtr( new ETable( lpMAPITable ) );
}
ETableSPtr TypeFactory::CreateAttachmentsETable( LPMAPITABLE lpMAPITable )
{
    if ( lpMAPITable == NULL )
    {
        return ETableSPtr( NULL );
    }

    ETable* pTable = new ETable( lpMAPITable );
    const SizedSPropTagArray( 6, atProps ) =
        { 6, (int)PR_ATTACH_SIZE, (int)PR_ATTACH_METHOD, (int)PR_ATTACH_LONG_FILENAME,
                (int)PR_ATTACH_FILENAME, (int)PR_DISPLAY_NAME, (int)PR_ATTACH_NUM };
    pTable->SetColumns( (LPSPropTagArray)&atProps );
    return ETableSPtr( pTable );
}
EMessageSPtr TypeFactory::CreateEMessage( LPMESSAGE lpMessage )
{
    if ( lpMessage == NULL )
    {
        return EMessageSPtr( NULL );
    }
    return EMessageSPtr( new EMessage( lpMessage ) );
}

MessagesSPtr TypeFactory::CreateMessages( LPMAPIFOLDER pFolder )
{
    if ( pFolder == NULL )
    {
        return MessagesSPtr( NULL );
    }
    return MessagesSPtr( new Messages( pFolder ) );
}
MsgStoreSPtr TypeFactory::CreateMsgStore( LPMDB pMDB, LPMAPISESSION lpMAPISession )
{
    if ( pMDB == NULL || lpMAPISession == NULL )
    {
        return MsgStoreSPtr( NULL );
    }
    return MsgStoreSPtr( new MsgStore( pMDB, lpMAPISession ) );
}
MsgStoresSPtr TypeFactory::CreateMsgStores( LPMAPISESSION pSession )
{
    if ( pSession == NULL )
    {
        return MsgStoresSPtr( NULL );
    }
    return MsgStoresSPtr( new MsgStores( pSession ) );
}
ABContainerSPtr TypeFactory::CreateABContainer( IABContainer* lpABContainer )
{
    if ( lpABContainer == NULL )
    {
        return ABContainerSPtr( NULL );
    }
    return ABContainerSPtr( new ABContainer( lpABContainer ) );
}

AddrBookSPtr TypeFactory::CreateAddrBook( LPADRBOOK lpAdrBook )
{
    if ( lpAdrBook == NULL )
    {
        return AddrBookSPtr( NULL );
    }
    return AddrBookSPtr( new AddrBook( lpAdrBook ) );
}
FormManagerSPtr TypeFactory::CreateFormManager( LPMAPIFORMMGR pFormManager )
{
    if ( pFormManager == NULL )
    {
        return FormManagerSPtr( NULL );
    }
    return FormManagerSPtr( new FormManager( pFormManager ) );
}
MAPIFormSPtr TypeFactory::CreateMAPIForm( LPMAPIFORM pForm )
{
    if ( pForm == NULL )
    {
        return MAPIFormSPtr( NULL );
    }
    return MAPIFormSPtr( new MAPIForm( pForm ) );
}
PersistMessageSPtr TypeFactory::CreatePersistMessage( LPPERSISTMESSAGE lpPersistMessage )
{
    if ( lpPersistMessage == NULL )
    {
        return PersistMessageSPtr( NULL );
    }
    return PersistMessageSPtr( new PersistMessage( lpPersistMessage ) );
}

ANSIString* TypeFactory::CreateANSIStrings( int count )
{
    return new ANSIString[count];
}
ANSIStringSPtr TypeFactory::CreateANSIString( LPSTR str )
{
    return ANSIStringSPtr( new ANSIString( str ) );
}
UNIStringSPtr TypeFactory::CreateUNIString( LPWSTR str )
{
    return UNIStringSPtr( new UNIString( str ) );
}
LPSTR* TypeFactory::CreateLPSTRArray( int count )
{
    return new LPSTR[count];
}
MAPISession* TypeFactory::CreateMAPISession()
{
    return new MAPISession();
}
void TypeFactory::Delete( MAPISession* ptr )
{
    if ( ptr != NULL )
    {
        delete ptr;
    }
}

void TypeFactory::Delete( RCPtrBase* ptr )
{
    if ( ptr != NULL )
    {
        delete ptr;
    }
}

