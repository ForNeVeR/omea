// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once
#include "helpers.h"
#include "typefactory.h"
#include "mapiPropImpl.h"
#using <mscorlib.dll>
using namespace System;

namespace EMAPILib
{
public __gc class MessageImpl : public EMAPILib::IEMessage, public MAPIPropImpl
{
    private:
        EMessageSPtr* _eMessage;
    public:
        MessageImpl( const EMessageSPtr& eMessage );
        virtual ~MessageImpl();
        virtual EMAPILib::MessageBody* GetRawBodyAsRTF();
        virtual String* GetPlainBody();
        virtual String* GetPlainBody( int sizeToRead );
        virtual bool IsUnread();
        virtual void SetUnRead( bool unread );
        virtual void CopyTo( IEMessage* destMessage );

        virtual IETable* GetRecipients();
        virtual IETable* GetAttachments();

        virtual IEAttach* OpenAttach( int num );
        virtual void SaveToMSG( String* path );

        virtual void Dispose();
    };
public __gc class MessagesImpl : public EMAPILib::IEMessages, public Disposable
{
    private:
        MessagesSPtr* _eMessages;
    public:
        MessagesImpl( const MessagesSPtr& eMessages );
        virtual ~MessagesImpl();
        virtual int GetCount();
        virtual IEMessage* OpenMessage( int index );
        virtual void Dispose();
    };
}
