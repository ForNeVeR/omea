// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include "mapisession.h"
#include "messageimpl.h"
#include "ETableImpl.h"
#include "AttachImpl.h"
#using <mscorlib.dll>
#include "temp.h"
#include "CharBuffer.h"
#include "EAttach.h"
#include "ESPropValue.h"
#include "ETable.h"
#include "Messages.h"
#include "EMessage.h"
#include "StringConvertion.h"
#include "guard.h"

EMAPILib::MessageImpl::MessageImpl( const EMessageSPtr& eMessage ) : MAPIPropImpl( eMessage.get() )
{
    if ( eMessage.IsNull() )
    {
        Guard::ThrowArgumentNullException( "eMessage" );
    }
    _eMessage = eMessage.CloneOnHeap();
}
EMAPILib::MessageImpl::~MessageImpl()
{
}
void EMAPILib::MessageImpl::Dispose()
{
    MAPIPropImpl::DisposeImpl();
    TypeFactory::Delete( _eMessage );
    _eMessage = NULL;
}
void EMAPILib::MessageImpl::SaveToMSG( String* path )
{
    if ( path == NULL )
    {
        Guard::ThrowArgumentNullException( "path" );
    }
    CheckDisposed();
    HRESULT hr = (*_eMessage)->SaveToMSG( Temp::GetANSIString( path )->GetChars() );
    Guard::CheckHR( hr );
}

EMAPILib::IEAttach* EMAPILib::MessageImpl::OpenAttach( int num )
{
    CheckDisposed();
    EAttachSPtr attach = (*_eMessage)->OpenAttach( num );
    if ( !attach.IsNull() )
    {
        return new AttachImpl( attach );
    }
    return NULL;
}
void EMAPILib::MessageImpl::CopyTo( IEMessage* destMessage )
{
    CheckDisposed();
    MessageImpl* destMessageImpl = dynamic_cast<MessageImpl*>(destMessage);
    (*_eMessage)->CopyTo( *(destMessageImpl->_eMessage ) );
}
EMAPILib::MessageBody* EMAPILib::MessageImpl::GetRawBodyAsRTF()
{
    CheckDisposed();
    return Temp::GetRawBodyAsRTF( *_eMessage );
}

String* EMAPILib::MessageImpl::GetPlainBody()
{
    return GetPlainBody( -1 );
}
String* EMAPILib::MessageImpl::GetPlainBody( int sizeToRead )
{
    CheckDisposed();
    CharBufferSPtr buffer = (*_eMessage)->openStringProperty( (int)PR_BODY, sizeToRead );
    if ( buffer.IsNull() ) return NULL;
    return buffer->GetRawChars();
}
void EMAPILib::MessageImpl::SetUnRead( bool unread )
{
    CheckDisposed();
    (*_eMessage)->SetUnRead( unread );
}
bool EMAPILib::MessageImpl::IsUnread()
{
    CheckDisposed();
    return (*_eMessage)->Unread();
}
EMAPILib::IETable* EMAPILib::MessageImpl::GetRecipients()
{
    CheckDisposed();
    ETableSPtr recipients = (*_eMessage)->GetRecipientsTable();
    if ( !recipients.IsNull() )
    {
        return new EMAPILib::ETableImpl( recipients );
    }
    return NULL;
}
EMAPILib::IETable* EMAPILib::MessageImpl::GetAttachments()
{
    CheckDisposed();
    ETableSPtr table = (*_eMessage)->GetAttachmentTable();
    if ( !table.IsNull() )
    {
        return new EMAPILib::ETableImpl( table );
    }
    return NULL;
}
EMAPILib::MessagesImpl::MessagesImpl( const MessagesSPtr& eMessages )
{
    if ( eMessages.IsNull() )
    {
        Guard::ThrowArgumentNullException( "eMessages" );
    }
    _eMessages = eMessages.CloneOnHeap();
}
EMAPILib::MessagesImpl::~MessagesImpl()
{
}
void EMAPILib::MessagesImpl::Dispose()
{
    Disposable::DisposeImpl();
    TypeFactory::Delete( _eMessages );
    _eMessages = NULL;
}
int EMAPILib::MessagesImpl::GetCount()
{
    CheckDisposed();
    return (*_eMessages)->GetCount();
}
EMAPILib::IEMessage* EMAPILib::MessagesImpl::OpenMessage( int index )
{
    CheckDisposed();
    EMessageSPtr message = (*_eMessages)->GetMessage( index );
    if ( !message.IsNull() )
    {
        return new MessageImpl( message );
    }
    return NULL;
}
