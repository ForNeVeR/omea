// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include "folderimpl.h"
#include "messageimpl.h"
#include "etableimpl.h"
#using <mscorlib.dll>
#include "EntryID.h"
#include "ETable.h"
#include "EMAPIFolder.h"
#include "EMessage.h"
#include "Messages.h"
#include "Guard.h"
#include "StringConvertion.h"
#include "temp.h"

EMAPILib::FolderImpl::FolderImpl( const EMAPIFolderSPtr& eFolder ) : MAPIPropImpl( eFolder.get() )
{
    if ( eFolder.IsNull() )
    {
        Guard::ThrowArgumentNullException( "eFolder" );
    }
    _eFolder = eFolder.CloneOnHeap();
}
EMAPILib::FolderImpl::!FolderImpl()
{
    _eFolder = NULL;
}
void EMAPILib::FolderImpl::Empty()
{
    CheckDisposed();
    (*_eFolder)->Empty();
}
void EMAPILib::FolderImpl::SetReadFlags( String ^entryID, bool unread )
{
    CheckDisposed();
    EntryIDSPtr entry;
    if ( entryID != nullptr )
    {
        entry = Helper::HexToEntryID( entryID );
    }
    (*_eFolder)->SetReadFlags( entry, unread );
}
void EMAPILib::FolderImpl::CopyTo( IEFolder ^destFolder )
{
    CheckDisposed( );
    MAPIPropImpl::CopyTo( &IID_IMAPIFolder, destFolder );
}
void EMAPILib::FolderImpl::SetMessageStatus( String ^entryID, int newStatus, int newStatusMask )
{
    CheckDisposed();
    if ( entryID == nullptr )
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }
    EntryIDSPtr entry = Helper::HexToEntryID( entryID );
    (*_eFolder)->SetMessageStatus( entry, newStatus, newStatusMask );
}
int EMAPILib::FolderImpl::GetMessageStatus( String ^entryID )
{
    CheckDisposed();
    if ( entryID == nullptr )
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }
    EntryIDSPtr entry = Helper::HexToEntryID( entryID );
    return (*_eFolder)->GetMessageStatus( entry );
}

EMAPILib::FolderImpl::~FolderImpl()
{
    Disposable::DisposeImpl();
    TypeFactory::Delete( _eFolder );
    _eFolder = NULL;
}
String^ EMAPILib::FolderImpl::GetFolderID()
{
    CheckDisposed();
    return GetBinProp( (int)PR_PARENT_ENTRYID );
}
EMAPILib::IEFolder^ EMAPILib::FolderImpl::CreateSubFolder( String ^name )
{
    CheckDisposed();
    if ( name == nullptr )
    {
        Guard::ThrowArgumentNullException( "name" );
    }
    EMAPIFolderSPtr folder = (*_eFolder)->CreateSubFolder( Temp::GetANSIString( name )->GetChars() );
    if ( !folder.IsNull() )
    {
        return gcnew FolderImpl( folder );
    }
    return nullptr;
}

SizedSPropTagArray(3, _Props3);

EMAPILib::IETable ^EMAPILib::FolderImpl::GetEnumTableForOwnEmail( )
{
    CheckDisposed();
    ETableSPtr table = (*_eFolder)->GetTable();
    if ( !table.IsNull() )
    {
        decltype(_Props3) atProps =
        { 3, (int)PR_SENDER_EMAIL_ADDRESS, (int)PR_SENDER_NAME, (int)PR_MESSAGE_DELIVERY_TIME };
        table->SetColumns( (LPSPropTagArray)&atProps );
        return gcnew ETableImpl( table );
    }
    return nullptr;
}
int EMAPILib::FolderImpl::GetTag()
{
    System::Guid set1( "{00062008-0000-0000-C000-000000000046}" );
    return GetIDsFromNames( &set1, 0x8578, PT_LONG );
}

SizedSPropTagArray(8, _Props8);

EMAPILib::IETable ^EMAPILib::FolderImpl::GetEnumTable( DateTime dt )
{
    CheckDisposed();
    ETableSPtr table = (*_eFolder)->GetTable();
    if ( !table.IsNull() )
    {

        static int tag = GetTag();

        const decltype(_Props8) atProps =
        { 8, (int)PR_ENTRYID, (int)0x66700102, (int)PR_LAST_MODIFICATION_TIME, (int)PR_MESSAGE_CLASS,
            (int)PR_MESSAGE_FLAGS, (int)PR_MESSAGE_DELIVERY_TIME, tag, PR_BODY };

        /*
        const SizedSPropTagArray( 5, atProps ) =
        { 5, (int)PR_ENTRYID, (int)PR_TRANSPORT_MESSAGE_HEADERS, (int)PR_MESSAGE_DELIVERY_TIME, 0x801D0003, 0x80240003 };
        */
        HRESULT hr = table->SetColumns( (LPSPropTagArray)&atProps );
        if ( hr == S_OK )
        {
            if ( dt != DateTime::MinValue )
            {
                FILETIME ft;
                Guard::SetFILETIME( &ft, dt.ToFileTime() );

                SPropValue prop;
                prop.ulPropTag = (int)PR_MESSAGE_DELIVERY_TIME;
                prop.Value.ft = ft;

                LPSRestriction pRest;
                hr = MAPIAllocateBuffer( sizeof(SRestriction), (LPVOID *)&pRest );
                MAPIBuffer mapiBuffer( hr, pRest );
                if ( hr == S_OK )
                {
                    pRest->rt = (int)RES_PROPERTY;
                    pRest->res.resProperty.relop = (int)RELOP_GE;
                    pRest->res.resProperty.ulPropTag = (int)PR_MESSAGE_DELIVERY_TIME;
                    pRest->res.resProperty.lpProp = &prop;
                    table->SetRestriction( pRest );
                }
            }
            return gcnew ETableImpl( table );
        }
    }
    return nullptr;
}
EMAPILib::IETable^ EMAPILib::FolderImpl::GetEnumTableForRecordKey( String ^recordKey )
{
    CheckDisposed();
    if ( recordKey == nullptr ) return nullptr;
    ETableSPtr table = (*_eFolder)->GetTable();
    if ( !table.IsNull() )
    {
        const decltype(_Props3) atProps =
            { 3, (int)PR_ENTRYID, (int)0x66700102, (int)PR_RECORD_KEY };
        table->SetColumns( (LPSPropTagArray)&atProps );
        EntryIDSPtr entry = Helper::HexToEntryID( recordKey );
        if ( entry.IsNull() ) return nullptr;

        SPropValue prop;
        prop.ulPropTag = (int)PR_RECORD_KEY;
        prop.Value.bin.cb = entry->GetLength();
        prop.Value.bin.lpb = (LPBYTE)entry->getLPENTRYID();

        LPSRestriction pRest;
        HRESULT hr = MAPIAllocateBuffer( sizeof(SRestriction), (LPVOID *)&pRest );
        Guard::CheckHR( hr );
        MAPIBuffer mapiBuffer( hr, pRest );
        pRest->rt = (int)RES_PROPERTY;
        pRest->res.resProperty.relop = (int)RELOP_EQ;
        pRest->res.resProperty.ulPropTag = (int)PR_RECORD_KEY;
        pRest->res.resProperty.lpProp = &prop;
        table->SetRestriction( pRest );
        return gcnew ETableImpl( table );
    }
    return nullptr;
}

EMAPILib::IEFolders ^EMAPILib::FolderImpl::GetFolders()
{
    CheckDisposed();
    EMAPIFoldersSPtr folders = (*_eFolder)->GetFolders();
    if ( !folders.IsNull() )
    {
        return gcnew FoldersImpl( folders );
    }
    return nullptr;
}
EMAPILib::IEMessages ^EMAPILib::FolderImpl::GetMessages()
{
    CheckDisposed();
    MessagesSPtr messages = (*_eFolder)->GetMessages();
    if ( !messages.IsNull() )
    {
        return gcnew EMAPILib::MessagesImpl( messages );
    }
    return nullptr;
}
EMAPILib::IEMessage ^EMAPILib::FolderImpl::OpenMessage( String ^entryID )
{
    CheckDisposed();
    if ( entryID == nullptr )
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }
    if ( entryID->Length == 0 )
        throw gcnew System::ArgumentException( "entryID shold not be empty" );
    EntryIDSPtr entry = Helper::HexToEntryID( entryID );
    EMessageSPtr message = (*_eFolder)->OpenMessage( entry );
    if ( !message.IsNull() )
    {
        return gcnew MessageImpl( message );
    }
    return nullptr;
}

EMAPILib::IEMessage ^EMAPILib::FolderImpl::CreateMessage( String ^messageClass )
{
    CheckDisposed();
    EMessageSPtr message = (*_eFolder)->CreateMessage();
    if ( !message.IsNull() )
    {
        message->setStringProp( (int)PR_MESSAGE_CLASS, Temp::GetANSIString( messageClass )->GetChars() );
        return gcnew MessageImpl( message );
    }
    return nullptr;
}
void EMAPILib::FolderImpl::MoveFolder( String ^entryID, IEFolder ^destFolder )
{
    CheckDisposed();
    CopyFolder( entryID, destFolder, (int)FOLDER_MOVE );
}
void EMAPILib::FolderImpl::CopyFolder( String ^entryID, IEFolder ^destFolder )
{
    CheckDisposed();
    CopyFolder( entryID, destFolder, 0 );
}
void EMAPILib::FolderImpl::CopyFolder( String ^entryID, IEFolder ^destFolder, int flags )
{
    CheckDisposed();
    FolderImpl^ destFolderImpl = dynamic_cast<FolderImpl^>(destFolder);
    EntryIDSPtr entry = Helper::HexToEntryID( entryID );
    (*_eFolder)->CopyFolder( entry, *(destFolderImpl->_eFolder), flags );
}

void EMAPILib::FolderImpl::MoveMessage( String ^entryID, IEFolder ^destFolder )
{
    CheckDisposed();
    CopyMessage( entryID, destFolder, (int)MESSAGE_MOVE );
}
void EMAPILib::FolderImpl::CopyMessage( String ^entryID, IEFolder ^destFolder )
{
    CheckDisposed();
    CopyMessage( entryID, destFolder, 0 );
}
void EMAPILib::FolderImpl::CopyMessage( String ^entryID, IEFolder ^destFolder, int flags )
{
    CheckDisposed();
    FolderImpl^ destFolderImpl = dynamic_cast<FolderImpl^>(destFolder);
    if ( destFolderImpl == nullptr )
    {
        return;
    }
    EntryIDSPtr entry = Helper::HexToEntryID( entryID );
    if ( !entry.IsNull() )
    {
        (*_eFolder)->CopyMessage( entry, *(destFolderImpl->_eFolder ), (int)flags );
    }
}

EMAPILib::FoldersImpl::FoldersImpl( const EMAPIFoldersSPtr& eFolders )
{
    if ( eFolders.IsNull() )
    {
        Guard::ThrowArgumentNullException( "eFolders" );
    }
    _eFolders = eFolders.CloneOnHeap();
}

EMAPILib::FoldersImpl::~FoldersImpl()
{
    Disposable::DisposeImpl();
    TypeFactory::Delete( _eFolders );
    _eFolders = NULL;
}
int EMAPILib::FoldersImpl::GetCount()
{
    CheckDisposed();
    return (*_eFolders)->GetCount();
}
String^ EMAPILib::FoldersImpl::GetEntryId( int rowNum )
{
    CheckDisposed();
    LPSPropValue lpProp = (*_eFolders)->GetProp( 1, rowNum );
    if ( lpProp != NULL )
    {
        return Helper::EntryIDToHex( lpProp->Value.bin.lpb, lpProp->Value.bin.cb );
    }
    return nullptr;
}

EMAPILib::IEFolder ^EMAPILib::FoldersImpl::OpenFolder( int rowNum )
{
    CheckDisposed();
    EMAPIFolderSPtr folder = (*_eFolders)->GetFolder( rowNum );
    if ( !folder.IsNull() )
    {
        return gcnew FolderImpl( folder );
    }
    return nullptr;
}
