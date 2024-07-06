// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "msgstore.h"
#include "msgstoresPreloaded.h"
#include "EntryID.h"
#include "EMAPIFolder.h"
#include "ETable.h"
#include "EMessage.h"
#include "ESPropValue.h"
#include "Guard.h"
#include "MAPISession.h"

#include "RCPtrDef.h"

#include "FormViewer.h"
#include "FormManager.h"
#include "AddrBook.h"

template RCPtr<MsgStore>;
template RCPtr<MsgStores>;

#ifdef EMAPI_MANAGED
#pragma managed
#endif

MsgStore::MsgStore( LPMDB pMDB, LPMAPISESSION lpMAPISession ) : MAPIProp( pMDB ), _ulConnection( 0 ), _pMDB( pMDB ), _pSession( lpMAPISession )
{
    OutputDebugString( "MsgStore::MsgStore" );
    _sink = NULL;
    if ( pMDB == NULL )
    {
        Guard::ThrowArgumentNullException( "pMDB" );
    }
    if ( lpMAPISession == NULL )
    {
        Guard::ThrowArgumentNullException( "lpMAPISession" );
    }
    _pSession->AddRef();
}

MsgStore::~MsgStore()
{
    try
    {
        Unadvise();
        OutputDebugString( "MsgStore::~MsgStore" );
        UlRelease( _pSession );
    }
    catch(...){}
}
AddrBookSPtr MsgStore::OpenAddressBook() const
{
    return AddrBook::OpenAddressBook( _pSession );
}

void MsgStore::AddRecipient( const EMessageSPtr& eMessage, LPWSTR displayName, LPWSTR email,
                            LPSTR displayNameA, LPSTR emailA, int recType ) const
{
    HRESULT hr = eMessage->AddRecipient( _pSession, displayName, email, displayNameA, emailA, true, recType );
    if ( (int)MAPI_E_BAD_CHARWIDTH == hr )
    {
        eMessage->AddRecipient( _pSession, displayName, email, displayNameA, emailA, false, recType );
    }
}

EMAPIFolderSPtr MsgStore::GetFolderForMessageCreation()
{
    EMAPIFolderSPtr folder = OpenDefaultFolder( PR_IPM_DRAFTS_ENTRYID );
    if ( folder.IsNull() )
    {
        folder = OpenOutbox();
    }
    return folder;
}

EMAPIFolderSPtr MsgStore::OpenDefaultFolder( int tag ) const
{
    EMAPIFolderSPtr indox = GetReceiveFolder( "IPM.Note" );
    if ( !indox.IsNull() )
    {
        ESPropValueSPtr entryID = indox->getSingleProp( tag );//PR_IPM_???_ENTRYID
        if ( !entryID.IsNull() )
        {
            return OpenFolder( entryID );
        }
    }
    return EMAPIFolderSPtr( NULL );
}

void MsgStore::DeleteMessage( const EntryIDSPtr& entryID, bool DeletedItems ) const
{
    EMessageSPtr message = OpenMessage( entryID );
    if ( message.IsNull() )
    {
        return;
    }
    ESPropValueSPtr folderID = message->getSingleProp( (int)PR_PARENT_ENTRYID );
    if ( folderID.IsNull() )
    {
        return;
    }
    message.release();
    EMAPIFolderSPtr folder = OpenFolder( folderID );
    if ( !DeletedItems )
    {
        folder->DeleteMessage( entryID );
    }
}

void MsgStore::DeleteFolder( const EntryIDSPtr& folderID, bool DeletedItems ) const
{
    EMAPIFolderSPtr eFolder = OpenFolder( folderID );
    if ( eFolder.IsNull() )
    {
        return;
    }
    ESPropValueSPtr parentID = eFolder->getSingleProp( (int)PR_PARENT_ENTRYID );
    if ( parentID.IsNull() )
    {
        return;
    }
    eFolder.release();
    EMAPIFolderSPtr folder = OpenFolder( parentID );
    if ( !DeletedItems )
    {
        folder->DeleteFolder( folderID );
    }
}

EMAPIFolderSPtr MsgStore::OpenFolder( const ESPropValueSPtr& folderID ) const
{
    if ( folderID.IsNull() )
    {
        Guard::ThrowArgumentNullException( "folderID" );
    }
    LPMAPIFOLDER pFolder = NULL;
    ULONG ulObjType;
    HRESULT hr = _pMDB->OpenEntry( folderID->GetBinCB(), (LPENTRYID)folderID->GetBinLPBYTE(), 0, (int)TEST_MAPI_MODIFY, &ulObjType, (LPUNKNOWN*)&pFolder);
    if ( hr == S_OK )
    {
        return TypeFactory::CreateEMAPIFolder( pFolder );
    }
    return EMAPIFolderSPtr( NULL );
}

EMAPIFolderSPtr MsgStore::GetReceiveFolder( LPSTR messageClass ) const
{
    ULONG cbInboxEID;
    LPENTRYID lpInboxEID = NULL;
    HRESULT hRes = _pMDB->GetReceiveFolder( messageClass, 0, &cbInboxEID, (LPENTRYID *) &lpInboxEID, NULL );
    if ( hRes == S_OK )
    {
        LPMAPIFOLDER lpFolder = NULL;
        unsigned long ulObjectType = 0;
        hRes = _pMDB->OpenEntry( cbInboxEID, (LPENTRYID)lpInboxEID, NULL, 0, &ulObjectType,
            (LPUNKNOWN*)&lpFolder );
        if ( hRes == S_OK )
        {
            return TypeFactory::CreateEMAPIFolder( lpFolder );
        }
    }
    return EMAPIFolderSPtr( NULL );
}

MsgStoreSPtr MsgStore::OpenMsgStore( LPMAPISESSION pSession, const EntryIDSPtr& entryID )
{
    if ( pSession == NULL )
    {
        Guard::ThrowArgumentNullException( "pSession" );
    }
    if ( entryID.IsNull() )
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }
    LPMDB pMDB = NULL;
    HRESULT hr = pSession->OpenMsgStore( 0, entryID->GetLength(), entryID->getLPENTRYID(), 0, (int)MDB_WRITE, &pMDB );
    Guard::CheckHR( hr, MapiLastError<LPMAPISESSION>(pSession) );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateMsgStore( pMDB, pSession );
    }
    return MsgStoreSPtr( NULL );
}

LPMDB MsgStore::GetRaw() const
{
    return _pMDB;
}

//#define PR_TEST_LINE_SPEED    PROP_TAG( PT_BINARY, 0x662B0102 )

void MsgStore::Advise( MsgStoreAdviseSink* sink )
{
    Unadvise();
    int flags = (int)fnevNewMail | (int)fnevObjectCreated | (int)fnevObjectMoved | (int)fnevObjectDeleted |
        (int)fnevObjectModified | (int)fnevObjectCopied;
    HRESULT hr = Guard::HrThisThreadAdviseSink( sink, &_sink );
    Guard::CheckHR( hr );
    _pMDB->Advise( 0, 0, flags, _sink, &_ulConnection );
    ULONG cProps = 0;
    const SizedSPropTagArray( 1, atProps ) = { 1, 0x662B0102 };

    LPSPropValue pPropValue = 0;
    hr = _pMDB->GetProps( (LPSPropTagArray)&atProps,  0,  &cProps, &pPropValue );
    MAPIBuffer mapiBuffer( hr, pPropValue );
}

void MsgStore::Unadvise()
{
    _pMDB->Unadvise( _ulConnection );
}
ETableSPtr MsgStore::GetReceiveFolderTable() const
{
    LPMAPITABLE table = NULL;
    HRESULT hr = _pMDB->GetReceiveFolderTable( 0, &table );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateETable( table );
    }
    return ETableSPtr( NULL );
}

EMAPIFolderSPtr MsgStore::GetRootFolder() const
{
    const SizedSPropTagArray( 2, atProps ) = { 2, (int)PR_DISPLAY_NAME, (int)PR_IPM_SUBTREE_ENTRYID };
    unsigned long ulCount = 0;
    LPSPropValue pVal = 0;
    HRESULT hr = _pMDB->GetProps( (LPSPropTagArray)&atProps, 0, &ulCount, &pVal );
    MAPIBuffer mapiBuffer( hr, pVal );
    if ( SUCCEEDED( hr ) )
    {
        LPMAPIFOLDER  pFolder   = 0;
        unsigned long ulObjType = 0;
        hr = _pMDB->OpenEntry( pVal[1].Value.bin.cb, (LPENTRYID)pVal[1].Value.bin.lpb, 0, (int)TEST_MAPI_MODIFY, &ulObjType,
            (LPUNKNOWN*)&pFolder);
        if ( SUCCEEDED( hr ) )
        {
            return TypeFactory::CreateEMAPIFolder( pFolder );
        }
    }
    return EMAPIFolderSPtr( NULL );
}

LPUNKNOWN MsgStore::OpenEntry( const EntryIDSPtr& entryID ) const
{
    LPUNKNOWN pMAPIObject = NULL;
    unsigned long ulObjectType = 0;
    HRESULT hr = _pMDB->OpenEntry( entryID->GetLength(), entryID->getLPENTRYID(), NULL, (int)MAPI_MODIFY,
        &ulObjectType, &pMAPIObject );
    if ( hr == S_OK && pMAPIObject != NULL )
    {
        return pMAPIObject;
    }
    return NULL;
}

EMessageSPtr MsgStore::OpenMessage( const EntryIDSPtr& entryID ) const
{
    LPMESSAGE lpMessage = (LPMESSAGE)OpenEntry( entryID );
    if ( NULL != lpMessage )
    {
        return TypeFactory::CreateEMessage( lpMessage );
    }
    return EMessageSPtr( NULL );
}
EMAPIFolderSPtr MsgStore::OpenFolder( const EntryIDSPtr& entryID ) const
{
    LPMAPIFOLDER lpFolder = (LPMAPIFOLDER)OpenEntry( entryID );
    if ( NULL != lpFolder )
    {
        return TypeFactory::CreateEMAPIFolder( lpFolder );
    }
    return EMAPIFolderSPtr( NULL );
}

EMAPIFolderSPtr MsgStore::OpenOutbox() const
{
    ESPropValueSPtr entryID = getSingleProp( (int)PR_IPM_OUTBOX_ENTRYID );
    if ( !entryID.IsNull() )
    {
        LPMAPIFOLDER pOutbox = NULL;
        unsigned long ulObjectType = 0;
        HRESULT hr = _pMDB->OpenEntry( entryID->GetBinCB(), (LPENTRYID)entryID->GetBinLPBYTE(),
            0, (int)TEST_MAPI_MODIFY, &ulObjectType, (LPUNKNOWN*)&pOutbox );

        if ( SUCCEEDED( hr ) )
        {
            return TypeFactory::CreateEMAPIFolder( pOutbox );
        }
    }
    return EMAPIFolderSPtr( NULL );
}

void MsgStore::OpenForm( const EMessageSPtr& message, int verbID )
{
    ESPropValueSPtr propStatus = message->GetStatus();
    if ( propStatus.IsNull() ) return;

    EMAPIFolderSPtr outbox = OpenOutbox();

    MsgStoreSPtr msgStore( this );
    FormViewer* lpMAPIFormViewer =
        new FormViewer( msgStore, _pSession, outbox, message, verbID );

    LPMAPIMESSAGESITE lpMAPIMessageSite = 0;
    LPMAPIVIEWCONTEXT lpMAPIViewContext = 0;

    HRESULT hr = lpMAPIFormViewer->QueryInterface( IID_IMAPIMessageSite, (LPVOID*)&lpMAPIMessageSite );
    hr = lpMAPIFormViewer->QueryInterface( IID_IMAPIViewContext, (LPVOID*)&lpMAPIViewContext );

    FormManagerSPtr frmManager = FormManager::GetFormManager( _pSession );
    MAPIFormSPtr form =
        frmManager->LoadForm( propStatus, lpMAPIMessageSite, lpMAPIViewContext, message );
    PersistMessageSPtr persistMessage = form->GetPersistMessage();
    persistMessage->Save( message );
    //persistMessage->Load( lpMAPIMessageSite, message, propStatus );

    //form->SetViewContext( lpMAPIViewContext );
    lpMAPIFormViewer->SetPersist( persistMessage );

    lpMAPIFormViewer->SetForm( form );
    form->DoVerb( verbID, lpMAPIViewContext );
    UlRelease( lpMAPIViewContext );
    UlRelease( lpMAPIMessageSite );
    UlRelease( lpMAPIFormViewer );
}

bool MsgStore::ActionMessage( const EntryIDSPtr& entryID, int verbID, EMessageSPtr& message, const MsgStoreSPtr& defMsgStore )
{
    if ( message.IsNull() )
    {
        message = OpenMessage( entryID );
    }

    if ( message.IsNull() )
    {
        return false;
    }
    ESPropValueSPtr propStatus = message->GetStatus();
    if ( propStatus.IsNull() )
    {
        return false;
    }
    EMAPIFolderSPtr folder = defMsgStore->GetFolderForMessageCreation();
    if ( folder.IsNull() )
    {
        return false;
    }
    MsgStoreSPtr param( this );
    FormViewer* lpMAPIFormViewer = new FormViewer( param, _pSession, folder, message, verbID );

    LPMAPIMESSAGESITE lpMAPIMessageSite = 0;
    LPMAPIVIEWCONTEXT lpMAPIViewContext = 0;

    HRESULT hr = lpMAPIFormViewer->QueryInterface(IID_IMAPIMessageSite,(LPVOID*)&lpMAPIMessageSite);
	hr = lpMAPIFormViewer->QueryInterface(IID_IMAPIViewContext,(LPVOID*)&lpMAPIViewContext);

    FormManagerSPtr frmManager = FormManager::GetFormManager( _pSession );
    MAPIFormSPtr form =
        frmManager->LoadForm( propStatus, lpMAPIMessageSite, lpMAPIViewContext, message );
    hr = lpMAPIFormViewer->SetForm( form );
    form->DoVerb( verbID, lpMAPIViewContext );
	UlRelease( lpMAPIViewContext );
	UlRelease( lpMAPIMessageSite );
	UlRelease( lpMAPIFormViewer );

    return true;
}
