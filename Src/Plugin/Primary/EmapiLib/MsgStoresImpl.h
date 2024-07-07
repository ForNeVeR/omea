// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once
#include "helpers.h"
#include "typefactory.h"
#include "MAPIPropImpl.h"
#using <mscorlib.dll>

class MsgStoreAdviseSink;

namespace EMAPILib
{
    public __gc class MsgStoreImpl : public EMAPILib::IEMsgStore, public MAPIPropImpl
    {
    private:
        MsgStoreSPtr* _eMsgStore;
        MsgStoreAdviseSink* _pSink;
        IMAPIListener* _sink;
    public:
        MsgStoreImpl( const MsgStoreSPtr& eMsgStore );
        virtual ~MsgStoreImpl();
        virtual IEFolder* GetRootFolder();
        virtual IEMessage* OpenMessage( String* entryID );
        virtual void DeleteMessage( String* entryID, bool DeletedItems );
        virtual IEFolder* OpenFolder( String* entryID );
        virtual void DeleteFolder( String* entryID, bool DeletedItems );
        virtual String* GetDefaultTaskFolderID();
        virtual IEFolder* OpenDraftsFolder();
        virtual IEFolder* OpenTasksFolder();
        virtual MAPIIDs* GetInboxIDs();
        virtual void Dispose();

        virtual void Advise( IMAPIListener* sink );
        virtual void Unadvise();

        virtual bool CreateNewMessage( String* subject, String* body, MailBodyFormat bodyFormat, ArrayList* recipients,
            ArrayList* attachments, int codePage );

        virtual bool DisplayMessage( String* entryID, IEMsgStore* defaultMsgStore );
        virtual bool ForwardMessage( String* entryID, IEMsgStore* defaultMsgStore );
        virtual bool ReplyMessage( String* strEntryID, IEMsgStore* defaultMsgStore );
        virtual bool ReplyAllMessage( String* entryID, IEMsgStore* defaultMsgStore );
    private:
        bool IsStandartReply( const EMessageSPtr& eMessage );
        bool ReplyMessageImpl( String* strEntryID, int verbID, IEMsgStore* defaultMsgStore );
    };

    public __gc class MsgStoresImpl : public EMAPILib::IEMsgStores, public Disposable
    {
    private:
        MsgStoresSPtr* _eMsgStores;
    public:
        MsgStoresImpl( const MsgStoresSPtr& eMsgStores );
        virtual ~MsgStoresImpl();
        virtual int GetCount();
        virtual EMAPILib::IEMsgStore* GetMsgStore( int index );
        virtual String* GetStorageID( int index );
        virtual String* GetDisplayName( int index );
        virtual bool IsDefaultStore( int index );
        virtual void Dispose();
    };
}
