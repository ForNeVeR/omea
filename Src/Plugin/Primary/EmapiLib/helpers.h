// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "typefactory.h"

#using <mscorlib.dll>
using namespace System;
using namespace System::Collections;
using namespace System::Diagnostics;

namespace EMAPILib
{
    public ref class MAPIIDs
    {
    private:
        String^ _entryID;
        String^ _storeID;
    public:
        MAPIIDs( String^ storeID, String^ entryID )
        {
            _storeID = storeID;
            _entryID = entryID;
        }
        property String^ StoreID { String^ get() { return _storeID; } }
        property String^ EntryID { String^ get() { return _entryID; } }
    };
    public enum class MailBodyFormat
    {
        PlainText,
        PlainTextInRTF,
        RTF,
        HTML,
    };
    public ref class MessageBody
    {
        private:
            String^ _text;
            MailBodyFormat _format;
            int _codePage;
        public:
            MessageBody( String^ text, MailBodyFormat format, int codePage )
            {
                _text = text;
                _format = format;
                _codePage = codePage;
            }
            property String^ text { String^ get() { return _text; } }
            property MailBodyFormat Format { MailBodyFormat get() { return _format; } }
            property int CodePage { int get() { return _codePage; } }
    };

    public ref class Disposable : public System::IDisposable
    {
    private:
        bool _disposed;
    protected:
        void CheckDisposed();
    public:
        Disposable();
        !Disposable();
        virtual ~Disposable();
        virtual void DisposeImpl();
    };
    public interface class IEMAPIProp : public System::IDisposable
    {
    public:
        virtual ArrayList^ GetBinArray( int tag );
        virtual ArrayList^ GetStringArray( int tag );
        virtual String^ GetBinProp( int tag );
        virtual DateTime GetDateTimeProp( int tag );
        virtual int GetLongProp( int tag );
        virtual int GetLongProp( int tag, bool retError );
        virtual bool GetBoolProp( int tag );
        virtual String^ GetStringProp( int tag );
        virtual void SetStringArray( int tag, ArrayList^ value );

        virtual int GetIDsFromNames( System::Guid% gcGUID, String^ name, int propType );
        virtual int GetIDsFromNames( System::Guid% gcGUID, int lID, int propType );

        virtual void SetDateTimeProp( int tag, DateTime value );
        virtual void SetStringProp( int tag, String^ value );
        virtual void SetLongProp( int tag, int value );
        virtual void SetBoolProp( int tag, bool value );
        virtual void WriteStringStreamProp( int tag, String^ propValue );
        virtual void SaveChanges();

        virtual void DeleteProp( int tag );
    };
    public interface class IERowSet : public System::IDisposable
    {
    public:
        virtual int GetRowCount();

        virtual String^ GetBinProp( int index );
        virtual String^ GetStringProp( int index );
        virtual DateTime GetDateTimeProp( int index );
        virtual int GetLongProp( int index );

        virtual String^ GetBinProp( int index, int rowNum );
        virtual String^ GetStringProp( int index, int rowNum );
        virtual DateTime GetDateTimeProp( int index, int rowNum );
        virtual int GetLongProp( int index, int rowNum );

        virtual String^ FindStringProp( int tag );
        virtual String^ FindBinProp( int tag );
        virtual int FindLongProp( int tag );
    };
    public interface class IETable : public System::IDisposable
    {
    public:
        virtual void Sort( int tag, bool Asc );
        virtual int GetRowCount();
        virtual IERowSet^ GetNextRow();
        virtual IERowSet^ GetNextRows( int count );
    };
    public interface class IEMailUser : public IEMAPIProp
    {
    public:
    };
    public interface class IEABContainer : public IEMAPIProp
    {
    public:
        virtual IETable^ GetTable();
        virtual IERowSet^ GetRowSet();
        virtual IEMailUser^ OpenMailUser( String^ entryID );
        virtual IEMailUser^ CreateMailUser();
    };

    public interface class IEAddrBook : public IEMAPIProp
    {
    public:
        virtual int GetCount();
        virtual IEABContainer^ OpenAB( int index );
        virtual IEABContainer^ OpenAB( String^ entryId );
        virtual String^ FindBinProp( int index, int tag );
        virtual IEMailUser^ OpenMailUser( String^ entryID );
    };

    interface class IEMessage;
    public interface class IEAttach : public IEMAPIProp
    {
    public:
        virtual array<System::Byte>^ ReadToEnd();
        virtual IEMessage^ OpenMessage();
        virtual void InsertOLEIntoRTF( int hwnd, int pos );
    };
    public interface class IEMessage : public IEMAPIProp
    {
    public:
        virtual MessageBody^ GetRawBodyAsRTF();
        virtual String^ GetPlainBody();
        virtual String^ GetPlainBody( int sizeToRead );

        virtual void CopyTo( IEMessage^ destMessage );

        virtual bool IsUnread();
        virtual void SetUnRead( bool unread );

        virtual IEAttach^ OpenAttach( int num );

        virtual IETable^ GetRecipients();
        virtual IETable^ GetAttachments();

        virtual void SaveToMSG( String^ path );
        virtual void SaveChanges();
    };
    public interface class IEMessages : public System::IDisposable
    {
    public:
        virtual int GetCount();
        virtual IEMessage^ OpenMessage( int index );
    };

    interface class IEFolders;
    public interface class IEFolder : public IEMAPIProp
    {
    public:
        virtual IETable^ GetEnumTable( DateTime dt );
        virtual IETable^ GetEnumTableForOwnEmail();
        virtual IETable^ GetEnumTableForRecordKey( String^ recordKey );
        virtual IEFolders^ GetFolders();
        virtual IEMessages^ GetMessages();
        virtual IEFolder^ CreateSubFolder( String^ name );
        virtual IEMessage^ CreateMessage( String^ messageClass );
        virtual IEMessage^ OpenMessage( String^ entryID );
        virtual void MoveFolder( String^ entryID, IEFolder^ destFolder );
        virtual void MoveMessage( String^ entryID, IEFolder^ destFolder );
        virtual void CopyMessage( String^ entryID, IEFolder^ destFolder );
        virtual String^ GetFolderID();
        virtual void SetMessageStatus( String^ entryID, int newStatus, int newStatusMask );
        virtual int GetMessageStatus( String^ entryID );
        virtual void SetReadFlags( String^ entryID, bool unread );
        virtual void Empty();
        virtual void CopyTo( IEFolder^ destFolder );
    };
    public interface class IEFolders : public System::IDisposable
    {
    public:
        virtual int GetCount();
        virtual IEFolder^ OpenFolder( int rowNum );
        virtual String^ GetEntryId( int rowNum );
    };

    public ref class MAPINtf
    {
        private:
            String^ _parentID;
            String^ _entryID;
        public:
            MAPINtf( String^ parentID, String^ entryID )
            {
                _parentID = parentID;
                _entryID = entryID;
            }
            property String^ ParentID { String^ get() { return _parentID; } }
            property String^ EntryID { String^ get() { return _entryID; } }
    };
    public ref class MAPIFullNtf : public MAPINtf
    {
        private:
            String^ _oldParentID;
            String^ _oldEntryID;
        public:
            MAPIFullNtf( String^ parentID, String^ entryID, String^ oldParentID, String^ oldEntryID ) :
              MAPINtf( parentID, entryID )
            {
                _oldParentID = oldParentID;
                _oldEntryID = oldEntryID;
            }
            property String^ OldEntryID { String^ get() { return _oldEntryID; } }
            property String^ OldParentID { String^ get() { return _oldParentID; } }
    };

    public interface class IMAPIListener
    {
        void OnNewMail( MAPINtf^ ntf );
        void OnMailAdd( MAPINtf^ ntf );
        void OnFolderAdd( MAPINtf^ ntf );
        void OnMailDelete( MAPINtf^ ntf );
        void OnFolderDelete( MAPINtf^ ntf );
        void OnMailModify( MAPIFullNtf^ ntf );
        void OnFolderModify( MAPINtf^ ntf );
        void OnMailMove( MAPIFullNtf^ ntf );
        void OnFolderMove( MAPIFullNtf^ ntf );
        void OnMailCopy( MAPIFullNtf^ ntf );
        void OnFolderCopy( MAPIFullNtf^ ntf );
    };

    public interface class IEMsgStore : public IEMAPIProp
    {
    public:
        virtual IEFolder^ GetRootFolder();
        virtual IEMessage^ OpenMessage( String^ entryID );
        virtual IEFolder^ OpenFolder( String^ entryID );
        virtual void DeleteFolder( String^ entryID, bool DeletedItems );
        virtual void DeleteMessage( String^ entryID, bool DeletedItems );
        virtual IEFolder^ OpenDraftsFolder();
        virtual IEFolder^ OpenTasksFolder();
        virtual String^ GetDefaultTaskFolderID();
        virtual MAPIIDs^ GetInboxIDs();
        virtual void Advise( IMAPIListener^ sink );
        virtual void Unadvise();

        virtual bool CreateNewMessage( String^ subject, String^ body, MailBodyFormat bodyFormat, ArrayList^ recipients,
            ArrayList^ attachments, int codePage );
        virtual bool DisplayMessage( String^ entryID, IEMsgStore^ defaultMsgStore );
        virtual bool ForwardMessage( String^ entryID, IEMsgStore^ defaultMsgStore );
        virtual bool ReplyMessage( String^ strEntryID, IEMsgStore^ defaultMsgStore );
        virtual bool ReplyAllMessage( String^ entryID, IEMsgStore^ defaultMsgStore );
    };
    public interface class IEMsgStores : public System::IDisposable
    {
    public:
        virtual int GetCount();
        virtual IEMsgStore^ GetMsgStore( int index );
        virtual String^ GetStorageID( int index );
        virtual String^ GetDisplayName( int index );
        virtual bool IsDefaultStore( int index );
    };

    public interface class IQuoting
    {
        String^ QuoteReply( String^ body );
    };

    public interface class ILibManager
    {
        int RegisterForm();
        void UnregisterForm( int id );
        void DeleteMessage( String^ entryID );
        void MoveMessage( String^ entryID, String^ folderID );
        void CopyMessage( String^ entryID, String^ folderID );
        void BeginReadProp( int prop_id );
        void EndReadProp();
    };

    public ref class RecipInfo
    {
        private:
            String^ _displayName;
            String^ _email;
        public:
            RecipInfo( String^ displayName, String^ email )
            {
                _displayName = displayName;
                _email = email;
            }
            property String^ DisplayName { String^ get() { return _displayName; } }
            property String^ Email
            {
                String^ get() { return _email; }
                void set( String^ value ) { _email = value; }
            }
    };
    public ref class AttachInfo
    {
        private:
            String^ _path;
            String^ _fileName;
        public:
            AttachInfo( String^ path, String^ fileName )
            {
                _path = path;
                _fileName = fileName;
            }
            property String^ Path { String^ get() { return _path; } }
            property String^ FileName { String^ get() { return _fileName; } }
    };
    public ref class ProblemWhenOpenStorage : public Exception
    {
        private:
            String^ _storeId;

        public:
            ProblemWhenOpenStorage( String^ storeId )
            {
                _storeId = storeId;
            }
            property String^ StoreId { String^ get() { return _storeId; } }
    };
    public ref class CancelledByUser : public ProblemWhenOpenStorage
    {
        public:
            CancelledByUser( String^ storeId ) : ProblemWhenOpenStorage( storeId )
            {
            }
    };
    public ref class StorageUnconfigured : public ProblemWhenOpenStorage
    {
        public:
            StorageUnconfigured( String^ storeId ) : ProblemWhenOpenStorage( storeId )
            {
            }
    };
}

class Helper
{
public:
    static String^ EntryIDToHex( LPBYTE bytes, int count );
    static EntryIDSPtr HexToEntryID( String^ hex );
    static EMAPILib::MAPINtf^ GetNewMailNtf( _NOTIFICATION notification );
    static EMAPILib::MAPINtf^ GetMAPINtf( _NOTIFICATION notification );
    static EMAPILib::MAPIFullNtf^ GetMAPIFullNtf( _NOTIFICATION notification );
    static void SetGUID( LPGUID lpGUID, System::Guid% gcGUID );
    static void MarshalCopy( byte* bytes, array<unsigned char> ^destination, int startIndex, int count );
    static String^ BinPropToString( const ESPropValueSPtr& prop );
};
