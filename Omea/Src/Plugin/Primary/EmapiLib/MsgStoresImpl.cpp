// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include "msgstoresimpl.h"
#include "folderimpl.h"
#include "messageimpl.h"
#using <mscorlib.dll>
#include "EntryID.h"
#include "ETable.h"
#include "ESPropValue.h"
#include "EMAPIFolder.h"
#include "EMessage.h"
#include "MsgStore.h"
#include "MsgStoresPreloaded.h"
#include "guard.h"
#include "temp.h"
#include "MsgStoreAdviseSink.h"
#include "StringConvertion.h"
#include "StringStream.h"
#include "CharBuffer.h"
#include "FormManager.h"
#include "FormViewer.h"
#include "AddrBook.h"
#include "MailUser.h"
#include "emapilib.h"


EMAPILib::MsgStoreImpl::MsgStoreImpl( const MsgStoreSPtr& eMsgStore ) : MAPIPropImpl( eMsgStore.get() )
{
    _eMsgStore = eMsgStore.CloneOnHeap();
    _pSink = NULL;
}
EMAPILib::MsgStoreImpl::~MsgStoreImpl()
{
}
void EMAPILib::MsgStoreImpl::Dispose()
{
    MAPIPropImpl::DisposeImpl();
    TypeFactory::Delete( _eMsgStore );
    _eMsgStore = NULL;
}
void EMAPILib::MsgStoreImpl::Advise( IMAPIListener* sink )
{
    CheckDisposed();
    if ( _pSink != NULL )
    {
        Unadvise();
    }
    _pSink = MsgStoreAdviseSink::Create();
    (*_eMsgStore)->Advise( _pSink );
    _pSink->SetListener( sink );
}
void EMAPILib::MsgStoreImpl::Unadvise()
{
    CheckDisposed();
    if ( _pSink != NULL )
    {
        _pSink->Release();
    }
    (*_eMsgStore)->Unadvise();
}

bool EMAPILib::MsgStoreImpl::CreateNewMessage( String* subject, String* body, MailBodyFormat bodyFormat, ArrayList* recipients,
    ArrayList* attachments, int codePage )
{
    Debug::WriteLine( "MsgStoreImpl::CreateNewMessage" );

    EMAPIFolderSPtr folder = (*_eMsgStore)->GetFolderForMessageCreation();
    if ( folder.IsNull() )
    {
        throw new System::ApplicationException( "Cannot get folder for message creation" );
        return false;
    }
    EMessageSPtr message = folder->CreateMessage();

    if ( message.IsNull() )
    {
        Debug::WriteLine( "MsgStoreImpl::CreateNewMessage cannot CreateMessage" );
        return false;
    }

    message->setStringProp( (int)PR_MESSAGE_CLASS, "IPM.Note" );

    if ( subject != NULL )
    {
        //Debug::WriteLine( subject );
        message->setStringProp( (int)PR_SUBJECT, Temp::GetANSIString( subject )->GetChars() );
    }
    if ( body != NULL )
    {
        //Debug::WriteLine( body );
        if ( EMAPILib::MailBodyFormat::PlainText == bodyFormat )
        {
            message->writeStringStreamProp( (int)PR_BODY,
                Temp::GetANSIString( body )->GetChars(), body->get_Length() );
        }
        else
        {
            ESPropValueSPtr maskProp = message->getSingleProp( (int)PR_STORE_SUPPORT_MASK );
            int mask = maskProp->GetLong();
            bool bSupportRTF = ( mask & (int)STORE_RTF_OK ) == 1;
            bSupportRTF = true;
            if ( bSupportRTF )
            {
                StringStreamSPtr streamComp = message->openStreamPropertyToWrite( (int)PR_RTF_COMPRESSED );
                if ( !streamComp.IsNull() )
                {
                    OutputDebugString( "Compressed RTF is open" );
                    StringStreamSPtr stream =
                        streamComp->GetWrapCompressedRTFStream( (int)MAPI_MODIFY  | ( (int)STORE_UNCOMPRESSED_RTF & mask ) );
                    if ( !stream.IsNull() )
                    {
                        OutputDebugString( "Compressed RTF is wrapped" );
                        CharBufferSPtr buffer = stream->Html2Rtf( Temp::GetANSIString( body )->GetChars() );
                        stream->Write( buffer->GetRawChars(), buffer->Length() );
                        stream->Commit();
                        streamComp->Commit();
                        message->RTFSyncRTF();
                        message->setLongProp( (int)PR_MSG_EDITOR_FORMAT, (int)EDITOR_FORMAT_HTML );
                        message->setLongProp( (int)PR_INET_MAIL_OVERRIDE_FORMAT, (int)ENCODING_PREFERENCE | (int)ENCODING_MIME | (int)BODY_ENCODING_TEXT_AND_HTML );

                        if ( codePage != 0 )
                        {
                            message->setLongProp( (int)0x3FDE0003, codePage );
                        }
                        //message->writeStringStreamProp( (int)PR_BODY, "BEBE", 4 );
                    }
                }
            }
            else
            {
                OutputDebugString( "Try to write to PR_HTML" );
                StringStreamSPtr streamComp = message->openStreamPropertyToWrite( (int)PR_BODY_HTML ); //PR_BODY_HTML
                if ( !streamComp.IsNull() )
                {
                    OutputDebugString( "PR_HTML is opened" );
                    streamComp->Write( Temp::GetANSIString( body )->GetChars(), body->get_Length() );
                    streamComp->Commit();
                }
                message->setLongProp( (int)PR_MSG_EDITOR_FORMAT, (int)EDITOR_FORMAT_HTML );
                message->setLongProp( (int)PR_INET_MAIL_OVERRIDE_FORMAT, (int)ENCODING_PREFERENCE | (int)ENCODING_MIME | (int)BODY_ENCODING_TEXT_AND_HTML );
            }
        }
    }
    Debug::WriteLine( "OpenForm" );
    if ( recipients != NULL )
    {
        Temp::AddRecipients( message, (*_eMsgStore), recipients, MAPI_TO );
    }
    if ( attachments != NULL )
    {
        Temp::AttachFiles( message, attachments );
    }
    (*_eMsgStore)->OpenForm( message, (int)EXCHIVERB_OPEN );
    return true;
}
bool EMAPILib::MsgStoreImpl::IsStandartReply( const EMessageSPtr& eMessage )
{
    StringStreamSPtr streamComp = eMessage->openStreamProperty( (int)PR_RTF_COMPRESSED );
    if ( !streamComp.IsNull() )
    {
        StringStreamSPtr stream = streamComp->GetWrapCompressedRTFStream();
        if ( !stream.IsNull() )
        {
            stream->Read();
            StringStream::Format format = stream->GetStreamFormat();

            if ( format != StringStream::Format::PlainText )
            {
                return true;
            }
            return false;
        }
    }
    CharBufferSPtr prHTML = eMessage->openStringProperty( (int)0x10130102 );
    if ( !prHTML.IsNull() )
    {
        return true;
    }
    return false;
}
bool EMAPILib::MsgStoreImpl::ReplyMessage( String* strEntryID, IEMsgStore* defaultMsgStore )
{
    return ReplyMessageImpl( strEntryID, (int)EXCHIVERB_REPLYTOSENDER, defaultMsgStore );
}
bool EMAPILib::MsgStoreImpl::ReplyAllMessage( String* entryID, IEMsgStore* defaultMsgStore )
{
    return ReplyMessageImpl( entryID, (int)EXCHIVERB_REPLYTOALL, defaultMsgStore );
}

bool EMAPILib::MsgStoreImpl::ReplyMessageImpl( String* strEntryID, int verbID, IEMsgStore* defaultMsgStore )
{
    CheckDisposed();
    MsgStoreImpl* defMsgStoreImpl = dynamic_cast<MsgStoreImpl*>( defaultMsgStore );

    MsgStoreSPtr defMsgStore = (*defMsgStoreImpl->_eMsgStore);

    EntryIDSPtr entryID = Helper::HexToEntryID( strEntryID );
    EMessageSPtr eMessage = (*_eMsgStore)->OpenMessage( entryID );
    if ( eMessage.IsNull() ) return false;

    if ( IsStandartReply( eMessage ) )
    {
        EMessageSPtr msg;
        return (*_eMsgStore)->ActionMessage( entryID, verbID, msg, defMsgStore );
    }

    EMAPIFolderSPtr folder = defMsgStore->GetFolderForMessageCreation();
    if ( folder.IsNull() ) return false;

    EMessageSPtr newMessage = folder->CreateMessage();
    if ( newMessage.IsNull() ) return false;

    CharBufferSPtr body = eMessage->openStringProperty( (int)PR_BODY );
    String* str = String::Empty;
    if ( !body.IsNull() )
    {
        str = new String( body->GetRawChars() );
    }

    EMAPILib::IQuoting* quoter = EMAPILib::EMAPISession::GetQuoter();
    if ( quoter != NULL )
    {
        String* body = quoter->QuoteReply( str );
        newMessage->writeStringStreamProp( (int)PR_BODY, Temp::GetANSIString( body )->GetChars(), body->get_Length() );
        ESPropValueSPtr propSubj = eMessage->getSingleProp( (int)PR_NORMALIZED_SUBJECT );

        String* subject = String::Empty;
        if ( !propSubj.IsNull() && propSubj->GetLPSTR() != NULL )
        {
            subject = new String( propSubj->GetLPSTR() );
        }
        String* reSubject = "RE: ";
        if ( !subject->StartsWith( reSubject ) )
        {
            subject = String::Concat( reSubject, subject );
        }
        newMessage->setStringProp( (int)PR_SUBJECT, Temp::GetANSIString( subject )->GetChars() );
    }

    newMessage->SetConversation( eMessage );

    ESPropValueSPtr propEmail = eMessage->getSingleProp( (int)PR_SENT_REPRESENTING_EMAIL_ADDRESS );
    LPSTR email = NULL;
    if ( !propEmail.IsNull() )
    {
        email = propEmail->GetLPSTR();
    }
    ESPropValueSPtr propName = eMessage->getSingleProp( (int)PR_SENT_REPRESENTING_NAME );
    LPSTR name = NULL;
    if ( !propName.IsNull() )
    {
        name = propName->GetLPSTR();
    }
    if ( name != NULL || email != NULL )
    {
        ArrayList* replyToRecipients = new ArrayList();
        replyToRecipients->Add( new EMAPILib::RecipInfo( name, email ) );
        Temp::AddRecipients( newMessage, defMsgStore, replyToRecipients, MAPI_TO );
    }

    if ( (int)EXCHIVERB_REPLYTOALL == verbID )
    {
        ETableSPtr table = eMessage->GetRecipientsTable();
        if ( !table.IsNull() )
        {
            for ( int i = 0; i < table->GetRowCount(); ++i )
            {
                ELPSRowSetSPtr row = table->GetNextRow();
                ESPropValueSPtr prop = row->FindProp( PR_RECIPIENT_TYPE );
                if ( !prop.IsNull() )
                {
                    newMessage->AddRecipient( row, MAPI_TO );
                }
            }
        }
    }

    newMessage->setLongProp( (int)PR_MSG_EDITOR_FORMAT, (int)EDITOR_FORMAT_PLAINTEXT );
    return (*_eMsgStore)->ActionMessage( entryID, (int)EXCHIVERB_OPEN, newMessage, defMsgStore );
    //return (*_eMsgStore)->ActionMessage( Helper::HexToEntryID( strEntryID ), (int)EXCHIVERB_REPLYTOSENDER );
}

bool EMAPILib::MsgStoreImpl::DisplayMessage( String* entryID, IEMsgStore* defaultMsgStore )
{
    CheckDisposed();
    MsgStoreImpl* defMsgStoreImpl = dynamic_cast<MsgStoreImpl*>( defaultMsgStore );
    MsgStoreSPtr defMsgStore = (*defMsgStoreImpl->_eMsgStore);

    EMessageSPtr msg;
    return (*_eMsgStore)->ActionMessage( Helper::HexToEntryID( entryID ), (int)EXCHIVERB_OPEN, msg, defMsgStore );
}
bool EMAPILib::MsgStoreImpl::ForwardMessage( String* entryID, IEMsgStore* defaultMsgStore )
{
    CheckDisposed();
    MsgStoreImpl* defMsgStoreImpl = dynamic_cast<MsgStoreImpl*>( defaultMsgStore );
    MsgStoreSPtr defMsgStore = (*defMsgStoreImpl->_eMsgStore);
    EMessageSPtr msg;
    return (*_eMsgStore)->ActionMessage( Helper::HexToEntryID( entryID ), (int)EXCHIVERB_FORWARD, msg, defMsgStore );
}

void EMAPILib::MsgStoreImpl::DeleteMessage( String* entryID, bool DeletedItems )
{
    CheckDisposed();
    (*_eMsgStore)->DeleteMessage( Helper::HexToEntryID( entryID ), DeletedItems );
}

void EMAPILib::MsgStoreImpl::DeleteFolder( String* entryID, bool DeletedItems )
{
    CheckDisposed();
    (*_eMsgStore)->DeleteFolder( Helper::HexToEntryID( entryID ), DeletedItems );
}

EMAPILib::IEFolder* EMAPILib::MsgStoreImpl::GetRootFolder()
{
    CheckDisposed();
    EMAPIFolderSPtr folder = (*_eMsgStore)->GetRootFolder();
    if ( !folder.IsNull() )
    {
        return new FolderImpl( folder );
    }
    return NULL;
}
EMAPILib::IEMessage* EMAPILib::MsgStoreImpl::OpenMessage( String* entryID )
{
    CheckDisposed();
    if ( entryID != NULL && entryID->get_Length() > 0  )
    {
        EMessageSPtr message = (*_eMsgStore)->OpenMessage( Helper::HexToEntryID( entryID ) );
        if ( !message.IsNull() )
        {
            return new MessageImpl( message );
        }
    }
    return NULL;
}
EMAPILib::IEFolder* EMAPILib::MsgStoreImpl::OpenFolder( String* entryID )
{
    CheckDisposed();
    if ( entryID != NULL && entryID->get_Length() > 0  )
    {
        EMAPIFolderSPtr folder = (*_eMsgStore)->OpenFolder( Helper::HexToEntryID( entryID ) );
        if ( !folder.IsNull() )
        {
            return new FolderImpl( folder );
        }
    }
    return NULL;
}
EMAPILib::IEFolder* EMAPILib::MsgStoreImpl::OpenDraftsFolder()
{
    CheckDisposed();
    EMAPIFolderSPtr folder = (*_eMsgStore)->OpenDefaultFolder( PR_IPM_DRAFTS_ENTRYID );
    if ( !folder.IsNull() )
    {
        return new FolderImpl( folder );
    }
    return NULL;
}

EMAPILib::IEFolder* EMAPILib::MsgStoreImpl::OpenTasksFolder()
{
    CheckDisposed();
    EMAPIFolderSPtr folder = (*_eMsgStore)->OpenDefaultFolder( PR_IPM_TASK_ENTRYID );
    if ( !folder.IsNull() )
    {
        return new FolderImpl( folder );
    }
    return NULL;
}

String* EMAPILib::MsgStoreImpl::GetDefaultTaskFolderID()
{
    CheckDisposed();
    ETableSPtr table = (*_eMsgStore)->GetReceiveFolderTable();
    if ( table.IsNull() || table->GetRowCount() == 0 ) return NULL;
    ELPSRowSetSPtr row = table->GetNextRow( );
    if ( row.IsNull() ) return NULL;
    ESPropValueSPtr entryID = row->FindProp( (int)PR_ENTRYID );
    if ( entryID.IsNull() ) return NULL;
    EMAPIFolderSPtr folder = (*_eMsgStore)->OpenFolder( entryID );
    if ( folder.IsNull() ) return NULL;
    ESPropValueSPtr taskEntryID = folder->getSingleProp( (int)0x36D40102 );//PR_IPM_TASK_ENTRYID
    if ( !taskEntryID.IsNull() && taskEntryID->GetBinCB() > 0 )
    {
        return Helper::EntryIDToHex( taskEntryID->GetBinLPBYTE(), taskEntryID->GetBinCB() );
    }
    return NULL;
}
EMAPILib::MAPIIDs* EMAPILib::MsgStoreImpl::GetInboxIDs()
{
    CheckDisposed();
    ESPropValueSPtr prop = (*_eMsgStore)->getSingleProp( (int)PR_ENTRYID );
    String* storeID = Helper::BinPropToString( prop );
    if ( storeID == NULL )
    {
        return NULL;
    }
    EMAPIFolderSPtr folder = (*_eMsgStore)->GetReceiveFolder( "IPM.Note" );
    if ( folder.IsNull() )
    {
        return NULL;
    }
    ESPropValueSPtr propEntryID = folder->getSingleProp( (int)PR_ENTRYID );
    if ( propEntryID.IsNull() )
    {
        return NULL;
    }
    String* entryID = Helper::EntryIDToHex( propEntryID->GetBinLPBYTE(), propEntryID->GetBinCB() );
    if ( entryID == NULL )
    {
        return NULL;
    }
    return new EMAPILib::MAPIIDs( storeID, entryID );
}
EMAPILib::MsgStoresImpl::MsgStoresImpl( const MsgStoresSPtr& eMsgStores )
{
    if ( eMsgStores.IsNull() )
    {
        Guard::ThrowArgumentNullException( "eMsgStores" );
    }
    _eMsgStores = eMsgStores.CloneOnHeap();
}
EMAPILib::MsgStoresImpl::~MsgStoresImpl()
{
}
void EMAPILib::MsgStoresImpl::Dispose()
{
    Disposable::DisposeImpl();
    TypeFactory::Delete( _eMsgStores );
    _eMsgStores = NULL;
}
EMAPILib::IEMsgStore* EMAPILib::MsgStoresImpl::GetMsgStore( int index )
{
    CheckDisposed();
    if ( index >= GetCount() )
    {
        Guard::ThrowArgumentOutOfRangeException( "index" );
    }
    MsgStoreSPtr msgStore = (*_eMsgStores)->OpenStorage( index );
    if ( !msgStore.IsNull() )
    {
        return new MsgStoreImpl( msgStore );
    }
    return NULL;
}

int EMAPILib::MsgStoresImpl::GetCount()
{
    CheckDisposed();
    return (*_eMsgStores)->GetCount();
}
String* EMAPILib::MsgStoresImpl::GetStorageID( int index )
{
    CheckDisposed();
    Debug::WriteLine( "EMAPILib::MsgStoresImpl::GetStorageID" );
    Debug::WriteLine( __box( index ) );
    Debug::WriteLine( __box( GetCount() ) );
    if ( index >= GetCount() )
    {
        Debug::WriteLine( "ERROR: index >= GetCount" );
        throw new System::ApplicationException( "index >= GetCount" );
    }
    LPSPropValue lpProp = (*_eMsgStores)->GetStorageID( index );
    if ( lpProp != NULL )
    {
        return Helper::EntryIDToHex( lpProp->Value.bin.lpb, lpProp->Value.bin.cb );
    }
    return NULL;
}
String* EMAPILib::MsgStoresImpl::GetDisplayName( int index )
{
    CheckDisposed();
    return new String( (*_eMsgStores)->GetDisplayName( index ) );
}
bool EMAPILib::MsgStoresImpl::IsDefaultStore( int index )
{
    CheckDisposed();
    return (*_eMsgStores)->IsDefaultStore( index );
}
