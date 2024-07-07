// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "emapi.h"
#include "rcobject.h"

class EMAPIFolder;
typedef RCPtr<EMAPIFolder> EMAPIFolderSPtr;

class ETable;
typedef RCPtr<ETable> ETableSPtr;

class ELPSRowSet;
typedef RCPtr<ELPSRowSet> ELPSRowSetSPtr;

class PersistMessage;
typedef RCPtr<PersistMessage> PersistMessageSPtr;

class MAPIForm;
typedef RCPtr<MAPIForm> MAPIFormSPtr;

class AddrBook;
typedef RCPtr<AddrBook> AddrBookSPtr;

class ABContainer;
typedef RCPtr<ABContainer> ABContainerSPtr;

class MsgStores;
typedef RCPtr<MsgStores> MsgStoresSPtr;

class MsgStore;
typedef RCPtr<MsgStore> MsgStoreSPtr;

class Messages;
typedef RCPtr<Messages> MessagesSPtr;

class EMAPIFolders;
typedef RCPtr<EMAPIFolders> EMAPIFoldersSPtr;

class EMessage;
typedef RCPtr<EMessage> EMessageSPtr;

class EntryID;
typedef RCPtr<EntryID> EntryIDSPtr;

class MailUser;
typedef RCPtr<MailUser> MailUserSPtr;

class EAttach;
typedef RCPtr<EAttach> EAttachSPtr;

class ESPropValue;
typedef RCPtr<ESPropValue> ESPropValueSPtr;

class CharBuffer;
typedef RCPtr<CharBuffer> CharBufferSPtr;

class CharsStorage;
typedef RCPtr<CharsStorage> CharsStorageSPtr;

class StringStream;
typedef RCPtr<StringStream> StringStreamSPtr;

class FormManager;
typedef RCPtr<FormManager> FormManagerSPtr;

class ANSIString;
typedef RCPtr<ANSIString> ANSIStringSPtr;

class UNIString;
typedef RCPtr<UNIString> UNIStringSPtr;

class MAPISession;

class Chars;
typedef RCPtr<Chars> CharsSPtr;

class PropTagArray;
typedef RCPtr<PropTagArray> PropTagArraySPtr;

class TypeFactory
{
public:
    static PropTagArraySPtr CreatePropTagArray( LPSPropTagArray propTags );
    static UNIStringSPtr CreateUNIString( LPWSTR str );
    static ANSIStringSPtr CreateANSIString( LPSTR str );
    static EMAPIFoldersSPtr CreateEMAPIFolders( LPMAPIFOLDER pFolder, LPMAPITABLE pTable );
    static EMAPIFolderSPtr CreateEMAPIFolder( LPMAPIFOLDER pFolder );
    static ESPropValueSPtr CreateESPropValue( LPSPropValue pVal, bool isFreeNecessary = true );
    static CharBufferSPtr CreateCharBuffer( int length );
    static CharsStorageSPtr CreateCharsStorage();
    static StringStreamSPtr CreateStringStream( LPSTREAM lpStream );
    static EAttachSPtr CreateEAttach( LPATTACH lpAttach );
    static MailUserSPtr CreateMailUser( IMailUser* lpMailUser );
    static EntryIDSPtr CreateEntryID( LPBYTE bytes, int cb );
    static ELPSRowSetSPtr CreateELPSRowSet( LPSRowSet lpSRowSet );
    static ETableSPtr CreateETable( LPMAPITABLE lpMAPITable );
    static ETableSPtr CreateAttachmentsETable( LPMAPITABLE lpMAPITable );
    static EMessageSPtr CreateEMessage( LPMESSAGE lpMessage );
    static MessagesSPtr CreateMessages( LPMAPIFOLDER pFolder );
    static MsgStoreSPtr CreateMsgStore( LPMDB pMDB, LPMAPISESSION lpMAPISession );
    static MsgStoresSPtr CreateMsgStores( LPMAPISESSION pSession );
    static ABContainerSPtr CreateABContainer( IABContainer* lpABContainer );
    static AddrBookSPtr CreateAddrBook( LPADRBOOK lpAdrBook );
    static FormManagerSPtr CreateFormManager( LPMAPIFORMMGR pFormManager );
    static MAPIFormSPtr CreateMAPIForm( LPMAPIFORM pForm );
    static PersistMessageSPtr CreatePersistMessage( LPPERSISTMESSAGE lpPersistMessage );
    static ANSIString* CreateANSIStrings( int count );
    static LPSTR* CreateLPSTRArray( int count );
    static MAPISession* CreateMAPISession();
    static void Delete( RCPtrBase* ptr );
    static void Delete( MAPISession* ptr );
private:
    TypeFactory();
};
