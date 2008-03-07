/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma once

#include "typefactory.h"

#using <mscorlib.dll>
using namespace System;
using namespace System::Collections;
using namespace System::Diagnostics;

namespace EMAPILib
{
    public __gc class MAPIIDs
    {
    private:
        String* _entryID;
        String* _storeID;
    public:
        MAPIIDs( String* storeID, String* entryID )
        {
            _storeID = storeID;
            _entryID = entryID;
        }
        __property String* get_StoreID() { return _storeID; }
        __property String* get_EntryID() { return _entryID; }
    };
    public __value enum MailBodyFormat 
    {
        PlainText,
        PlainTextInRTF,
        RTF,
        HTML,
    };
    public __gc class MessageBody
    {
        private:
            String* _text;
            MailBodyFormat _format;
            int _codePage;
        public:
            MessageBody( String* text, MailBodyFormat format, int codePage )
            {
                _text = text;
                _format = format;
                _codePage = codePage;
            }
            __property String* get_text() { return _text; }
            __property MailBodyFormat get_Format() { return _format; }
            __property int get_CodePage() { return _codePage; }
    };

    public __gc class Disposable : public System::IDisposable
    {
    private:
        bool _disposed;
    protected:
        void CheckDisposed();
    public:
        Disposable();
        virtual ~Disposable();
        virtual void Dispose();
        virtual void DisposeImpl();
    };
    public __gc __interface IEMAPIProp : public System::IDisposable
    {
    public:
        virtual ArrayList* GetBinArray( int tag );
        virtual ArrayList* GetStringArray( int tag );
        virtual String* GetBinProp( int tag );
        virtual DateTime GetDateTimeProp( int tag );
        virtual int GetLongProp( int tag );
        virtual int GetLongProp( int tag, bool retError );
        virtual bool GetBoolProp( int tag );
        virtual String* GetStringProp( int tag );
        virtual void SetStringArray( int tag, ArrayList* value );

        virtual int GetIDsFromNames( System::Guid* gcGUID, String* name, int propType );
        virtual int GetIDsFromNames( System::Guid* gcGUID, int lID, int propType );

        virtual void SetDateTimeProp( int tag, DateTime value );
        virtual void SetStringProp( int tag, String* value );
        virtual void SetLongProp( int tag, int value );
        virtual void SetBoolProp( int tag, bool value );
        virtual void WriteStringStreamProp( int tag, String* propValue );
        virtual void SaveChanges();

        virtual void DeleteProp( int tag );
    };
    public __gc __interface IERowSet : public System::IDisposable
    {
    public:
        virtual int GetRowCount();

        virtual String* GetBinProp( int index );
        virtual String* GetStringProp( int index );
        virtual DateTime GetDateTimeProp( int index );
        virtual int GetLongProp( int index );

        virtual String* GetBinProp( int index, int rowNum );
        virtual String* GetStringProp( int index, int rowNum );
        virtual DateTime GetDateTimeProp( int index, int rowNum );
        virtual int GetLongProp( int index, int rowNum );

        virtual String* FindStringProp( int tag );
        virtual String* FindBinProp( int tag );
        virtual int FindLongProp( int tag );
    };
    public __gc __interface IETable : public System::IDisposable
    {
    public:
        virtual void Sort( int tag, bool Asc );
        virtual int GetRowCount();
        virtual IERowSet* GetNextRow();
        virtual IERowSet* GetNextRows( int count );
    };
    public __gc __interface IEMailUser : public IEMAPIProp
    {
    public:
    };
    public __gc __interface IEABContainer : public IEMAPIProp
    {
    public:
        virtual IETable* GetTable();
        virtual IERowSet* GetRowSet();
        virtual IEMailUser* OpenMailUser( String* entryID );
        virtual IEMailUser* CreateMailUser();
    };

    public __gc __interface IEAddrBook : public IEMAPIProp
    {
    public:
        virtual int GetCount();
        virtual IEABContainer* OpenAB( int index );
        virtual IEABContainer* OpenAB( String* entryId );
        virtual String* FindBinProp( int index, int tag );
        virtual IEMailUser* OpenMailUser( String* entryID );
    };
    
    public __gc __interface IEMessage;
    public __gc __interface IEAttach : public IEMAPIProp
    {
    public:
        virtual System::Byte ReadToEnd()[];
        virtual IEMessage* OpenMessage();
        virtual void InsertOLEIntoRTF( int hwnd, int pos );
    };
    public __gc __interface IEMessage : public IEMAPIProp
    {
    public:
        virtual MessageBody* GetRawBodyAsRTF();
        virtual String* GetPlainBody();
        virtual String* GetPlainBody( int sizeToRead );

        virtual void CopyTo( IEMessage* destMessage );

        virtual bool IsUnread();
        virtual void SetUnRead( bool unread );

        virtual IEAttach* OpenAttach( int num );

        virtual IETable* GetRecipients();
        virtual IETable* GetAttachments();

        virtual void SaveToMSG( String* path );
        virtual void SaveChanges();
    };
    public __gc __interface IEMessages : public System::IDisposable
    {
    public:
        virtual int GetCount();
        virtual IEMessage* OpenMessage( int index );
    };

    public __gc __interface IEFolders;
    public __gc __interface IEFolder : public IEMAPIProp
    {
    public:
        virtual IETable* GetEnumTable( DateTime dt );
        virtual IETable* GetEnumTableForOwnEmail();
        virtual IETable* GetEnumTableForRecordKey( String* recordKey );
        virtual IEFolders* GetFolders();
        virtual IEMessages* GetMessages();
        virtual IEFolder* CreateSubFolder( String* name );
        virtual IEMessage* CreateMessage( String* messageClass );
        virtual IEMessage* OpenMessage( String* entryID );
        virtual void MoveFolder( String* entryID, IEFolder* destFolder );
        virtual void MoveMessage( String* entryID, IEFolder* destFolder );
        virtual void CopyMessage( String* entryID, IEFolder* destFolder );
        virtual String* GetFolderID();
        virtual void SetMessageStatus( String* entryID, int newStatus, int newStatusMask );
        virtual int GetMessageStatus( String* entryID );
        virtual void SetReadFlags( String* entryID, bool unread );
        virtual void Empty();
        virtual void CopyTo( IEFolder* destFolder );
    };
    public __gc __interface IEFolders : public System::IDisposable
    {
    public:
        virtual int GetCount();
        virtual IEFolder* OpenFolder( int rowNum );
        virtual String* GetEntryId( int rowNum );
    };

    public __gc class MAPINtf
    {
        private:
            String* _parentID;
            String* _entryID;
        public:
            MAPINtf( String* parentID, String* entryID )
            {
                _parentID = parentID;
                _entryID = entryID;
            }
            __property String* get_ParentID() { return _parentID; }
            __property String* get_EntryID() { return _entryID; }
    };
    public __gc class MAPIFullNtf : public MAPINtf
    {
        private:
            String* _oldParentID;
            String* _oldEntryID;
        public:
            MAPIFullNtf( String* parentID, String* entryID, String* oldParentID, String* oldEntryID ) : 
              MAPINtf( parentID, entryID )
            {
                _oldParentID = oldParentID;
                _oldEntryID = oldEntryID;
            }
            __property String* get_OldEntryID() { return _oldEntryID; }
            __property String* get_OldParentID() { return _oldParentID; }
    };

    public __gc __interface IMAPIListener 
    { 
        void OnNewMail( MAPINtf* ntf );
        void OnMailAdd( MAPINtf* ntf );
        void OnFolderAdd( MAPINtf* ntf );
        void OnMailDelete( MAPINtf* ntf );
        void OnFolderDelete( MAPINtf* ntf );
        void OnMailModify( MAPIFullNtf* ntf );
        void OnFolderModify( MAPINtf* ntf );
        void OnMailMove( MAPIFullNtf* ntf );
        void OnFolderMove( MAPIFullNtf* ntf );
        void OnMailCopy( MAPIFullNtf* ntf );
        void OnFolderCopy( MAPIFullNtf* ntf );
    };

    public __gc __interface IEMsgStore : public IEMAPIProp
    {
    public:
        virtual IEFolder* GetRootFolder();
        virtual IEMessage* OpenMessage( String* entryID );
        virtual IEFolder* OpenFolder( String* entryID );
        virtual void DeleteFolder( String* entryID, bool DeletedItems );
        virtual void DeleteMessage( String* entryID, bool DeletedItems );
        virtual IEFolder* OpenDraftsFolder();
        virtual IEFolder* OpenTasksFolder();
        virtual String* GetDefaultTaskFolderID();
        virtual MAPIIDs* GetInboxIDs();
        virtual void Advise( IMAPIListener* sink );
        virtual void Unadvise();

        virtual bool CreateNewMessage( String* subject, String* body, MailBodyFormat bodyFormat, ArrayList* recipients,     
            ArrayList* attachments, int codePage );
        virtual bool DisplayMessage( String* entryID, IEMsgStore* defaultMsgStore );
        virtual bool ForwardMessage( String* entryID, IEMsgStore* defaultMsgStore );
        virtual bool ReplyMessage( String* strEntryID, IEMsgStore* defaultMsgStore );
        virtual bool ReplyAllMessage( String* entryID, IEMsgStore* defaultMsgStore );
    };
    public __gc __interface IEMsgStores : public System::IDisposable
    {
    public:
        virtual int GetCount();
        virtual IEMsgStore* GetMsgStore( int index );
        virtual String* GetStorageID( int index );
        virtual String* GetDisplayName( int index );
        virtual bool IsDefaultStore( int index );
    };

    public __gc __interface IQuoting 
    { 
        String* QuoteReply( String* body ); 
    };

    public __gc __interface ILibManager
    {
        int RegisterForm();
        void UnregisterForm( int id );
        void DeleteMessage( String* entryID );
        void MoveMessage( String* entryID, String* folderID );
        void CopyMessage( String* entryID, String* folderID );
        void BeginReadProp( int prop_id );
        void EndReadProp();
    };

    public __gc class RecipInfo
    {
        private:
            String* _displayName;
            String* _email;
        public:
            RecipInfo( String* displayName, String* email )
            {
                _displayName = displayName;
                _email = email;
            }
            __property String* get_DisplayName() { return _displayName; }
            __property String* get_Email() { return _email; }
            __property void set_Email( String* value ) { _email = value; }
    };
    public __gc class AttachInfo
    {
        private:
            String* _path;
            String* _fileName;
        public:
            AttachInfo( String* path, String* fileName )
            {
                _path = path;
                _fileName = fileName;
            }
            __property String* get_Path() { return _path; }
            __property String* get_FileName() { return _fileName; }
    };
    public __gc class ProblemWhenOpenStorage : public Exception
    {
        private: 
            String* _storeId;

        public:
            ProblemWhenOpenStorage( String* storeId )
            {
                _storeId = storeId;
            }
            __property String* get_StoreId() { return _storeId; }
    };
    public __gc class CancelledByUser : public ProblemWhenOpenStorage
    {
        public:
            CancelledByUser( String* storeId ) : ProblemWhenOpenStorage( storeId )
            {
            }
    };
    public __gc class StorageUnconfigured : public ProblemWhenOpenStorage
    {
        public:
            StorageUnconfigured( String* storeId ) : ProblemWhenOpenStorage( storeId )
            {
            }
    };
}

class Helper
{
public:
    static String* EntryIDToHex( LPBYTE bytes, int count );
    static EntryIDSPtr HexToEntryID( String* hex );
    static EMAPILib::MAPINtf* GetNewMailNtf( _NOTIFICATION notification );
    static EMAPILib::MAPINtf* GetMAPINtf( _NOTIFICATION notification );
    static EMAPILib::MAPIFullNtf* GetMAPIFullNtf( _NOTIFICATION notification );
    static void SetGUID( LPGUID lpGUID, System::Guid* gcGUID );
    static void MarshalCopy( byte* bytes, unsigned char destination __gc[], int startIndex, int count );
    static String* BinPropToString( const ESPropValueSPtr& prop );
};
