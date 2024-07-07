// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "typefactory.h"
#include "mapiprop.h"

class EMessage : public MAPIProp
{
private:
    LPMESSAGE _lpMessage;
public:
    EMessage( LPMESSAGE lpMessage );
    virtual ~EMessage();
    LPMESSAGE GetRaw() const;

    ESPropValueSPtr GetStatus() const;
    void Submit() const;

    void AttachFile( LPSTR path, LPSTR fileName ) const;
    void AddRecipient( ELPSRowSetSPtr row, int recType ) const;
    HRESULT AddRecipient( LPMAPISESSION pSession, LPWSTR displayName, LPWSTR email,
        LPSTR displayNameA, LPSTR emailA, bool unicode, int recType ) const;
    void SetConversation( const EMessageSPtr& parent ) const;

    void CopyTo( const EMessageSPtr& destMessage ) const;

    //properties
    int GetInternetCPID() const;

    void RTFSyncBody() const;
    void RTFSyncRTF() const;

    bool Unread() const;
    void SetUnRead( bool unread ) const;
    ETableSPtr GetRecipientsTable() const;
    ETableSPtr GetAttachmentTable() const;

    EAttachSPtr OpenAttach( int attachmentNum ) const;
    HRESULT SaveToMSG( LPSTR szPath );
    static EMessageSPtr LoadFromMSG( LPSTR szPath );
};

