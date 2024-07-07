// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include "attachimpl.h"
#include "messageimpl.h"
#using <mscorlib.dll>

#include "CharBuffer.h"
#include "EAttach.h"
#include "EMessage.h"

EMAPILib::AttachImpl::AttachImpl( const EAttachSPtr& eAttach ) : MAPIPropImpl( eAttach.get() )
{
    _eAttach = eAttach.CloneOnHeap();
}

EMAPILib::AttachImpl::~AttachImpl()
{
    MAPIPropImpl::DisposeImpl();
    TypeFactory::Delete( _eAttach );
    _eAttach = NULL;
}
array<System::Byte> ^EMAPILib::AttachImpl::ReadToEnd()
{
    CharBufferSPtr buffer = (*_eAttach)->ReadToEnd();
    if ( buffer.IsNull() ) return gcnew array<unsigned char>(0);

    int count = buffer->Length() - 1;
    auto destination = gcnew array<unsigned char>(count);
    Helper::MarshalCopy( (byte*)buffer->GetRawChars(), destination, 0, count );
    return destination;
}
EMAPILib::IEMessage^ EMAPILib::AttachImpl::OpenMessage()
{
    LPMESSAGE lpMessage = (*_eAttach)->OpenMessage();
    if ( lpMessage == NULL ) return nullptr;
    EMessageSPtr message = TypeFactory::CreateEMessage( lpMessage );
    return gcnew EMAPILib::MessageImpl( message );
}
void EMAPILib::AttachImpl::InsertOLEIntoRTF( int hwnd, int pos )
{
    (*_eAttach)->InsertOLEIntoRTF( (HWND)hwnd, pos );
}
