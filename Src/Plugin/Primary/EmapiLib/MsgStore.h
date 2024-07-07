// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "typefactory.h"
#include "mapiprop.h"

class MsgStoreAdviseSink;

__nogc class MsgStore : public MAPIProp
{
private:
    LPMDB _pMDB;
    LPMAPISESSION _pSession;
    LPMAPIADVISESINK _sink;
    unsigned long _ulConnection;
public:
    MsgStore( LPMDB pMDB, LPMAPISESSION lpMAPISession );
    virtual ~MsgStore();
    EMessageSPtr OpenMessage( const EntryIDSPtr& entryID ) const;
    void DeleteMessage( const EntryIDSPtr& entryID, bool DeletedItems ) const;

    EMAPIFolderSPtr OpenFolder( const EntryIDSPtr& entryID ) const;
    EMAPIFolderSPtr OpenOutbox() const;
    AddrBookSPtr OpenAddressBook() const;
    LPUNKNOWN OpenEntry( const EntryIDSPtr& entryID ) const;
    LPMDB GetRaw() const;
    EMAPIFolderSPtr GetFolderForMessageCreation();
    EMAPIFolderSPtr GetRootFolder() const;
    ETableSPtr GetReceiveFolderTable() const;

    EMAPIFolderSPtr OpenDefaultFolder( int tag ) const;

    EMAPIFolderSPtr OpenFolder( const ESPropValueSPtr& folderID ) const;
    void DeleteFolder( const EntryIDSPtr& folderID, bool DeletedItems ) const;

    EMAPIFolderSPtr GetReceiveFolder( LPSTR messageClass ) const;
    void Advise( MsgStoreAdviseSink* pSink );
    void Unadvise();
    void AddRecipient( const EMessageSPtr& eMessage, LPWSTR displayName, LPWSTR email,
        LPSTR displayNameA, LPSTR emailA, int recType ) const;
    void OpenForm( const EMessageSPtr& message, int verbID );

    static MsgStoreSPtr OpenMsgStore( LPMAPISESSION pSession, const EntryIDSPtr& entryID );

    bool ActionMessage( const EntryIDSPtr& entryID, int verbID, EMessageSPtr& message, const MsgStoreSPtr& defMsgStore );
};
