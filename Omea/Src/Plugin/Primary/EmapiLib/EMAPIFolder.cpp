/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma unmanaged

#include "EMAPIFolder.h"
#include "EntryID.h"
#include "Messages.h"
#include "ETable.h"
#include "EMessage.h"
#include "ESPropValue.h"
#include "Guard.h"

#include "RCPtrDef.h"

template RCPtr<EMAPIFolders>;
template RCPtr<EMAPIFolder>;

EMAPIFolder::EMAPIFolder( LPMAPIFOLDER lpFolder ) : MAPIProp( lpFolder )
{
    if ( lpFolder == NULL )
    {
        Guard::ThrowArgumentNullException( "lpFolder" );
    }
    _lpFolder = lpFolder;
}

EMAPIFolder::~EMAPIFolder()
{
    _lpFolder = NULL;
}
void EMAPIFolder::Empty( ) const
{
    HRESULT hr = _lpFolder->EmptyFolder( 0, NULL, 0 );
    if ( hr != MAPI_W_PARTIAL_COMPLETION )
    {
        Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
    }
}

void EMAPIFolder::SetReadFlags( const EntryIDSPtr& entry, bool unread ) const
{
    SBinary binary;
    ENTRYLIST eidMsg;
    LPENTRYLIST lpEIDMsg = NULL;

    if ( !entry.IsNull() )
    {
        binary.cb = entry->GetLength();
        binary.lpb = (LPBYTE)entry->getLPENTRYID();

        eidMsg.cValues = 1;
        eidMsg.lpbin   = &binary;
        lpEIDMsg = &eidMsg;
    }

    HRESULT hr = _lpFolder->SetReadFlags( lpEIDMsg, NULL, NULL, unread ? CLEAR_READ_FLAG : 0 );
    if ( hr != MAPI_W_PARTIAL_COMPLETION )
    {
        Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
    }
}
EMAPIFolderSPtr EMAPIFolder::CreateSubFolder( LPSTR folderName ) const
{
    LPMAPIFOLDER lpFolder = NULL;
    HRESULT hr = 
        _lpFolder->CreateFolder( (int)FOLDER_GENERIC, folderName, "Created by Omea", NULL, (int)OPEN_IF_EXISTS, &lpFolder );
    Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateEMAPIFolder( lpFolder );
    }
    return EMAPIFolderSPtr( NULL );
}
void EMAPIFolder::SetMessageStatus( const EntryIDSPtr& msgEntryID, int newStatus, int newStatusMask ) const
{
    ULONG oldStatus = 0;
    HRESULT hr = _lpFolder->SetMessageStatus( msgEntryID->GetLength(), msgEntryID->getLPENTRYID(), newStatus, newStatusMask, &oldStatus );
    Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
}

int EMAPIFolder::GetMessageStatus( const EntryIDSPtr& msgEntryID ) const
{
    ULONG status = 0;
    HRESULT hr = _lpFolder->GetMessageStatus( msgEntryID->GetLength(), msgEntryID->getLPENTRYID(), 0, &status );
    Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
    return status;
}
MessagesSPtr EMAPIFolder::GetMessages() const
{
    return TypeFactory::CreateMessages( _lpFolder );
}
ETableSPtr EMAPIFolder::GetTable() const
{
    LPMAPITABLE lpTable = NULL;
    HRESULT hr = _lpFolder->GetContentsTable( 0, &lpTable );
    if ( hr == S_OK ) 
    {
        return TypeFactory::CreateETable( lpTable );
    }
    return ETableSPtr( NULL );
}
int EMAPIFolder::GetMailCount() const
{
    ESPropValueSPtr prop = getSingleProp( (int)PR_CONTENT_COUNT );
    if ( !prop.IsNull() )
    {
        return prop->GetLong();
    }
    return 0;
}

LPMAPIFOLDER EMAPIFolder::GetRaw() const
{
    return _lpFolder;
}

void EMAPIFolder::DeleteMessage( SBinary* pEntryId ) const
{
    ENTRYLIST eidMsg;
    eidMsg.cValues = 1;
    eidMsg.lpbin   = pEntryId;
    HRESULT hr = _lpFolder->DeleteMessages( &eidMsg, NULL, NULL, 0  );
    Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
}

void EMAPIFolder::DeleteMessage( const EntryIDSPtr& entry ) const
{
    SBinary binary;
    binary.cb = entry->GetLength();
    binary.lpb = (LPBYTE)entry->getLPENTRYID();
    DeleteMessage( &binary );
}

void EMAPIFolder::DeleteMessage( const ESPropValueSPtr& entry ) const
{
    SBinary binary;
    binary.cb = entry->GetBinCB();
    binary.lpb = (LPBYTE)entry->GetBinLPBYTE();
    DeleteMessage( &binary );
}

void EMAPIFolder::DeleteFolder( const EntryIDSPtr& entry ) const
{
    HRESULT hr = _lpFolder->DeleteFolder( entry->GetLength(), entry->getLPENTRYID(), 
        0, NULL, (int)( DEL_FOLDERS | DEL_MESSAGES ) );
    Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
}
EMessageSPtr EMAPIFolder::OpenMessage( const EntryIDSPtr& entryID ) const
{
    if ( entryID.IsNull() || entryID->getLPENTRYID() == NULL || entryID->GetLength() == 0 )
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }
    LPMESSAGE lpMessage = NULL;
    ULONG objectType = 0;
    HRESULT hr = 
        _lpFolder->OpenEntry( entryID->GetLength(), entryID->getLPENTRYID(), 
            NULL, (int)TEST_MAPI_MODIFY, &objectType, (LPUNKNOWN*)&lpMessage );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateEMessage( lpMessage );
    }
    if ( hr != (int)MAPI_E_NOT_FOUND && hr != (int)MAPI_E_INVALID_ENTRYID )
    {
        Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
    }
    return EMessageSPtr( NULL );
}

void EMAPIFolder::CopyMessage( SBinary* pEntryId, LPVOID pFolderDestination, int flags ) const
{
    ENTRYLIST eidMsg;
    eidMsg.cValues = 1;
    eidMsg.lpbin   = pEntryId;
    HRESULT hr = _lpFolder->CopyMessages( &eidMsg, &IID_IMAPIFolder, pFolderDestination, NULL, NULL, 
        flags );
    Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
}
void EMAPIFolder::CopyMessage( const EntryIDSPtr& entry, const EMAPIFolderSPtr& destFolder, int flags ) const
{
    CopyMessage( entry, destFolder->_lpFolder, flags );
}
void EMAPIFolder::CopyMessage( const ESPropValueSPtr& entry, LPVOID pFolderDestination, int flags ) const
{
    SBinary binary;
    binary.cb = entry->GetBinCB();
    binary.lpb = entry->GetBinLPBYTE();
    CopyMessage( &binary, pFolderDestination, flags );
}

void EMAPIFolder::CopyMessage( const EntryIDSPtr& entry, LPVOID pFolderDestination, int flags ) const
{
    SBinary binary;
    binary.cb = entry->GetLength();
    binary.lpb = (LPBYTE)entry->getLPENTRYID();
    CopyMessage( &binary, pFolderDestination, flags );
}
EMessageSPtr EMAPIFolder::CreateMessage() const
{
    LPMESSAGE lpMessage = NULL;
    HRESULT hr = _lpFolder->CreateMessage( NULL, 0, &lpMessage );
    if ( SUCCEEDED( hr ) )
    {
        return TypeFactory::CreateEMessage( lpMessage );
    }
    Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
    return EMessageSPtr( NULL );
}

EMAPIFoldersSPtr EMAPIFolder::GetFolders() const
{
    LPMAPITABLE pTable = NULL;
    HRESULT hr = _lpFolder->GetHierarchyTable( 0, &pTable );
    if ( hr == S_OK )
    {
/*
        //IMAP and HotMail MAPIFolders must be extra addreffed 
        //There should be investigation why
        ESPropValueSPtr prop = getSingleProp( (int)PR_CONTAINER_CLASS );
        if ( !prop.IsNull() && prop->GetLPSTR() != NULL )
        {
            LPSTR containerClass = prop->GetLPSTR();
            if ( strcmp( containerClass, "IPF.Imap" ) == 0 || strcmp( containerClass, "IPF.Dav" ) == 0 )
            {
                _lpFolder->AddRef();
            }
        }
*/
        return TypeFactory::CreateEMAPIFolders( _lpFolder, pTable );
    }
    //0x8004DF0A - error code was thrown I can't find what does it mean
    //it is attempt to get folders for deleted info store
    if ( hr != 0x8004DF0A )
    {
        Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
    }
    return EMAPIFoldersSPtr( NULL );
}
void EMAPIFolder::CopyFolder( const EntryIDSPtr& entry, const EMAPIFolderSPtr& destFolder, int flags ) const
{
    HRESULT hr = 
        _lpFolder->CopyFolder( entry->GetLength(), entry->getLPENTRYID(), 
            &IID_IMAPIFolder, (LPVOID)destFolder->GetRaw(), NULL, 0, NULL, flags );
    Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
}

EMAPIFolders::EMAPIFolders( LPMAPIFOLDER lpFolder, LPMAPITABLE pTable ) : _count( 0 ), _pRows( NULL )
{
    if ( lpFolder == NULL )
    {
        Guard::ThrowArgumentNullException( "lpFolder" );
    }
    if ( pTable == NULL )
    {
        Guard::ThrowArgumentNullException( "pTable" );
    }
    _lpFolder = lpFolder;
    _lpFolder->AddRef();
    _pTable = pTable;
    const SizedSPropTagArray( 2, atProps ) = { 2, (int)PR_DISPLAY_NAME, (int)PR_ENTRYID };

    HRESULT hr = HrQueryAllRows( _pTable, (LPSPropTagArray)&atProps, 0, 0, 0, &_pRows );
    if ( hr == S_OK )
    {
        _count = _pRows->cRows;
    }
}

EMAPIFolders::~EMAPIFolders()
{
    try
    {
        if ( _pRows != NULL )
        {
            FreeProws( _pRows );
        }
        if ( _pTable != NULL )
        {
            UlRelease( _pTable );
        }
        if ( _lpFolder != NULL )
        {
            UlRelease( _lpFolder );
        }
    }
    catch(...){}
}
int EMAPIFolders::GetCount() const
{
    return _count;
}

LPSPropValue EMAPIFolders::GetProp( int index, int rowNum ) const
{
    LPSPropValue lpsPropVal = &(_pRows->aRow[rowNum].lpProps[index]);
    if ( lpsPropVal != NULL && lpsPropVal->Value.err != (int)MAPI_E_NOT_FOUND )
    {
        return lpsPropVal;
    }
    return NULL;
}

EMAPIFolderSPtr EMAPIFolders::GetFolder( int rowNum ) const
{
    LPSPropValue pVal = _pRows->aRow[rowNum].lpProps;
    LPMAPIFOLDER lpMAPIFolder = NULL;

    ULONG objectType = 0;
    HRESULT hr = _lpFolder->OpenEntry( pVal[1].Value.bin.cb, (LPENTRYID)pVal[1].Value.bin.lpb, 
        0, 0, &objectType, (LPUNKNOWN*)&lpMAPIFolder );
    if ( SUCCEEDED( hr )&& lpMAPIFolder != NULL )
    {
        return TypeFactory::CreateEMAPIFolder( lpMAPIFolder );
    }
    if ( hr != (int)MAPI_E_INVALID_ENTRYID )
    {
        Guard::CheckHR( hr, MapiLastError<LPMAPIPROP>(_lpFolder) );
    }
    return EMAPIFolderSPtr( NULL );
}
