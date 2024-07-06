// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include "EMAPILib.h"
#include "MsgStoresImpl.h"
#include "ESPropValue.h"
#include "EntryID.h"
#include "MsgStore.h"
#include "EMessage.h"
#include "AddrBookImpl.h"
#include "Temp.h"
#include "MessageImpl.h"
#include "FolderImpl.h"
#include "EMAPIFolder.h"
#include "AddrBook.h"
#include "EntryID.h"
#include "CharBuffer.h"
#include "StringConvertion.h"
#include "MailUser.h"
#include "FormViewer.h"
#include "FormManager.h"
#include "typefactory.h"
#include "MAPISession.h"
#include "StringStream.h"
#include "emapi.h"
#include "guard.h"
using namespace System::Runtime::InteropServices;

EMAPILib::EMAPISession::EMAPISession( int /*fake*/ )
{
    MyHeapObject::CreateHeap();
    _pMAPISession = TypeFactory::CreateMAPISession();
}
EMAPILib::EMAPISession::~EMAPISession(  )
{
    OutputDebugString( "~EMAPISession" );
    try
    {
        TypeFactory::Delete( _pMAPISession );
    }
    catch(...){}
}

bool EMAPILib::EMAPISession::CanClose()
{
    int count = CountOutlookWindows();
    System::Diagnostics::Debug::WriteLine( __box( count ) );
    return ( CountOutlookWindows() == 0 );
}

bool EMAPILib::EMAPISession::Initialize( bool pickLogonProfile, ILibManager* libManager )
{
    bool bRet = _pMAPISession->Initialize( pickLogonProfile );
    if ( !bRet )
    {
        OutputDebugString( "Initialize FAILED" );
        return false;
    }
    _libManager = libManager;
    return bRet;
}

void EMAPILib::EMAPISession::BeginReadProp( int prop_id )
{
    _libManager->BeginReadProp( prop_id );
}
void EMAPILib::EMAPISession::EndReadProp()
{
    _libManager->EndReadProp();
}

void EMAPILib::EMAPISession::CheckDependencies()
{
    HMODULE hModule = LoadLibraryEx( "MAPI32.dll", NULL, 0 );
    DWORD lastError = GetLastError();
    if ( hModule != NULL )
    {
        FreeLibrary( hModule );
    }
    if ( hModule == NULL && lastError )
    {
        Int32 int32 = lastError;
        String* strError = "Error code: ";
        strError = String::Concat( strError, int32.ToString() );
        LPSTR message = LoadErrorText( lastError );
        if ( message != NULL )
        {
            strError = String::Concat( strError, ". Message: " );
            strError = String::Concat( strError, message );
            LocalFree( message );
        }
        throw new System::Exception( strError );
    }
}
void EMAPILib::EMAPISession::AddRecipients( System::Object* mapiObject, ArrayList* recipients )
{
    if ( mapiObject == NULL )
    {
        Guard::ThrowArgumentNullException( "mapiObject" );
    }
    if ( recipients == NULL )
    {
        Guard::ThrowArgumentNullException( "recipients" );
    }
    IntPtr ptrMapiObject = Marshal::GetIUnknownForObject( mapiObject );
    if ( (int)ptrMapiObject == 0 )
    {
        Guard::ThrowArgumentNullException( "ptrMapiObject is NULL" );
    }
    else
    {
        IMessage* message = NULL;
        try
        {
            IUnknown* unk = static_cast<IUnknown*>(static_cast<void*>( ptrMapiObject ));
            if ( unk == NULL )
            {
                Guard::ThrowArgumentNullException( "Cannot get IUnknown from MAPIOBJECT" );
            }
            HRESULT hr = unk->QueryInterface( IID_IMessage, (void**)&message );
            Guard::CheckHR( hr );
            EMessageSPtr msg = TypeFactory::CreateEMessage( message );
            if ( msg.IsNull() )
            {
                Guard::ThrowArgumentNullException( "Cannot create EMessage for IMessage" );
            }
            for ( int i = 0; i < recipients->Count; i++ )
            {
                EMAPILib::RecipInfo* recipInfo = (dynamic_cast<EMAPILib::RecipInfo*>( recipients->get_Item( i ) ));
                if ( recipInfo != NULL )
                {
                    _pMAPISession->AddRecipient( msg,
                        Temp::GetUNIString( recipInfo->get_DisplayName() )->GetChars(),
                        Temp::GetUNIString( recipInfo->get_Email() )->GetChars(),
                        Temp::GetANSIString( recipInfo->get_DisplayName() )->GetChars(),
                        Temp::GetANSIString( recipInfo->get_Email() )->GetChars(),
                        MAPI_TO );
                }
            }
        }
        __finally
        {
            if ( (int)ptrMapiObject != 0 )
            {
                Marshal::Release( ptrMapiObject );
            }
        }
    }
}

void EMAPILib::EMAPISession::DeleteMessage( const ESPropValueSPtr& entryID )
{
    if ( _libManager != NULL )
    {
        _libManager->DeleteMessage( Helper::EntryIDToHex( entryID->GetBinLPBYTE(), entryID->GetBinCB() ) );
    }
}
void EMAPILib::EMAPISession::MoveMessage( const ESPropValueSPtr& entryID, const ESPropValueSPtr& folderID )
{
    if ( _libManager != NULL )
    {
        _libManager->MoveMessage( Helper::EntryIDToHex( entryID->GetBinLPBYTE(), entryID->GetBinCB() ),
            Helper::EntryIDToHex( folderID->GetBinLPBYTE(), folderID->GetBinCB() ));
    }
}
void EMAPILib::EMAPISession::CopyMessage( const ESPropValueSPtr& entryID, const ESPropValueSPtr& folderID )
{
    if ( _libManager != NULL )
    {
        _libManager->CopyMessage( Helper::EntryIDToHex( entryID->GetBinLPBYTE(), entryID->GetBinCB() ),
            Helper::EntryIDToHex( folderID->GetBinLPBYTE(), folderID->GetBinCB() ));
    }
}

int EMAPILib::EMAPISession::RegisterForm()
{
    if ( _libManager != NULL )
    {
        return _libManager->RegisterForm();
    }
    return -1;
}
void EMAPILib::EMAPISession::UnregisterForm( int formID )
{
    if ( _libManager != NULL )
    {
        _libManager->UnregisterForm( formID );
    }
}

void EMAPILib::EMAPISession::Uninitialize()
{
    _pMAPISession->Uninitialize();
}

EMAPILib::IEAddrBook* EMAPILib::EMAPISession::OpenAddrBook()
{
    AddrBookSPtr addrBook = _pMAPISession->OpenAddressBook();
    if ( addrBook.IsNull() ) return NULL;
    return new EMAPILib::AddrBookImpl( addrBook );
}

void EMAPILib::EMAPISession::SetQuoter( IQuoting* quoter )
{
    _quoter = quoter;
}
EMAPILib::IQuoting* EMAPILib::EMAPISession::GetQuoter( )
{
    return _quoter;
}
EMAPILib::IEMsgStores* EMAPILib::EMAPISession::GetMsgStores( )
{
    OutputDebugString( "GetMsgStores" );
    MsgStoresSPtr msgStores = _pMAPISession->GetMsgStores();
    return new EMAPILib::MsgStoresImpl( msgStores );
}
EMAPILib::IEMsgStore* EMAPILib::EMAPISession::OpenMsgStore( String* entryID )
{
    MsgStoreSPtr msgStore = _pMAPISession->OpenMsgStore( Helper::HexToEntryID( entryID ) );
    if ( !msgStore.IsNull() )
    {
        return new EMAPILib::MsgStoreImpl( msgStore );
    }
    return NULL;
}

bool EMAPILib::EMAPISession::CompareEntryIDs( String* entryID1, String *entryID2 )
{
	return _pMAPISession->CompareEntryIDs( Helper::HexToEntryID( entryID1 ), Helper::HexToEntryID( entryID2 ) );
}

int EMAPILib::EMAPISession::ObjectsCount()
{
	return MyHeapObject::ObjectsCount();
}

int EMAPILib::EMAPISession::HeapSize()
{
	return MyHeapObject::HeapSize();
}

EMAPILib::IEMessage* EMAPILib::EMAPISession::LoadFromMSG( String* path )
{
    if ( path == NULL )
    {
        Guard::ThrowArgumentNullException( "path" );
    }
    EMessageSPtr message = EMessage::LoadFromMSG( Temp::GetANSIString( path )->GetChars() );
    if ( !message.IsNull() )
    {
        return new MessageImpl( message );
    }
    return NULL;
}
