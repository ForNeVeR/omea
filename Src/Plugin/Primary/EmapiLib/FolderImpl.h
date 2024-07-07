// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once
#include "helpers.h"
#include "typefactory.h"
#using <mscorlib.dll>
#include "MAPIPropImpl.h"

namespace EMAPILib
{
    public ref class FolderImpl : public EMAPILib::IEFolder, public MAPIPropImpl
    {
    private:
        EMAPIFolderSPtr* _eFolder;
        void CopyMessage( String^ entryID, IEFolder^ destFolder, int flags );
        void CopyFolder( String^ entryID, IEFolder^ destFolder, int flags );
    public:
        FolderImpl( const EMAPIFolderSPtr& eFolder );
        !FolderImpl();
        virtual IETable^ GetEnumTable( DateTime dt );
        virtual IETable^ GetEnumTableForOwnEmail();
        virtual IETable^ GetEnumTableForRecordKey( String^ recordKey );
        virtual IEFolders^ GetFolders();
        virtual IEMessages^ GetMessages();
        virtual IEFolder^ CreateSubFolder( String^ name );
        virtual void MoveFolder( String^ entryID, IEFolder^ destFolder );
        virtual void CopyFolder( String^ entryID, IEFolder^ destFolder );
        virtual void MoveMessage( String^ entryID, IEFolder^ destFolder );
        virtual void CopyMessage( String^ entryID, IEFolder^ destFolder );
        virtual IEMessage^ CreateMessage( String^ messageClass );
        virtual IEMessage^ OpenMessage( String^ entryID );
        virtual String^ GetFolderID();
        virtual ~FolderImpl();
        virtual void SetMessageStatus( String^ entryID, int newStatus, int newStatusMask );
        virtual int GetMessageStatus( String^ entryID );
        virtual void SetReadFlags( String^ entryID, bool unread );
        virtual void Empty();
        virtual void CopyTo( IEFolder^ destFolder );

    private:
        virtual int GetTag( );
    };
    public ref class FoldersImpl : public EMAPILib::IEFolders, public Disposable
    {
    private:
        EMAPIFoldersSPtr* _eFolders;
    public:
        FoldersImpl( const EMAPIFoldersSPtr& eFolder );
        virtual int GetCount();
        virtual IEFolder^ OpenFolder( int rowNum );
        virtual String^ GetEntryId( int rowNum );
        virtual ~FoldersImpl();
    };
}
