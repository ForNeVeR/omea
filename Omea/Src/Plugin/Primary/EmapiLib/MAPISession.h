/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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

