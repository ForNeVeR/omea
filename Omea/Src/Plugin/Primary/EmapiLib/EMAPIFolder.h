// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "typefactory.h"
#include "mapiprop.h"

class EMAPIFolder : public MAPIProp
{
private:
    LPMAPIFOLDER _lpFolder;
public:
    EMAPIFolder( LPMAPIFOLDER lpFolder );
    virtual ~EMAPIFolder();

    EMAPIFoldersSPtr GetFolders() const;
    LPMAPIFOLDER GetRaw() const;
    ETableSPtr GetTable() const;
    MessagesSPtr GetMessages() const;
    EMessageSPtr CreateMessage() const;
    EMessageSPtr OpenMessage( const EntryIDSPtr& entry ) const;

    void Empty( ) const;

    EMAPIFolderSPtr CreateSubFolder( LPSTR folderName ) const;
    void CopyFolder( const EntryIDSPtr& entry, const EMAPIFolderSPtr& destFolder, int flags ) const;
    void CopyMessage( const EntryIDSPtr& entry, const EMAPIFolderSPtr& destFolder, int flags ) const;
    void CopyMessage( const EntryIDSPtr& entry, LPVOID pFolderDestination, int flags ) const;
    void CopyMessage( SBinary* pEntryId, LPVOID pFolderDestination, int flags ) const;
    void CopyMessage( const ESPropValueSPtr& entry, LPVOID pFolderDestination, int flags ) const;
    void DeleteMessage( SBinary* pEntryId ) const;
    void DeleteMessage( const EntryIDSPtr& entry ) const;
    void DeleteMessage( const ESPropValueSPtr& entry ) const;
    void DeleteFolder( const EntryIDSPtr& entry ) const;
    int GetMailCount() const;
    void SetMessageStatus( const EntryIDSPtr& msgEntryID, int newStatus, int newStatusMask ) const;
    int GetMessageStatus( const EntryIDSPtr& msgEntryID ) const;
    void SetReadFlags( const EntryIDSPtr& entry, bool unread ) const;
};

class EMAPIFolders : public RCObject
{
private:
    LPMAPIFOLDER _lpFolder;
    LPMAPITABLE _pTable;
    int _count;
    LPSRowSet _pRows;
public:
    EMAPIFolders( LPMAPIFOLDER lpFolder, LPMAPITABLE pTable );
    int GetCount() const;
    EMAPIFolderSPtr GetFolder( int rowNum ) const;
    LPSPropValue GetProp( int index, int rowNum ) const;
    virtual ~EMAPIFolders();
};
