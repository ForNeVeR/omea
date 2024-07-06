// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "typefactory.h"
#include "emapi.h"

__nogc class MAPISession : public MyHeapObject
{
    private:
        LPMAPISESSION _pSession; //MAPI Session Pointer
    public:
        MAPISession();
        LPMAPISESSION GetRaw() const;
        bool Initialize( bool pickLogonProfile );
        void Uninitialize();

        MsgStoreSPtr OpenMsgStore( const EntryIDSPtr& entryID ) const;
        MsgStoresSPtr GetMsgStores();
        MsgStoreSPtr GetDefaultStore();
        EMAPIFolderSPtr GetFolderForMessageCreation();

		bool CompareEntryIDs( const EntryIDSPtr& entryID1, const EntryIDSPtr& entryID2 );
        bool IsStandartReply( const EMessageSPtr& eMessage );
        AddrBookSPtr OpenAddressBook() const;
        void AddRecipient( const EMessageSPtr& eMessage, LPWSTR displayName, LPWSTR email,
            LPSTR displayNameA, LPSTR emailA, int recType ) const;

        virtual ~MAPISession();
};

