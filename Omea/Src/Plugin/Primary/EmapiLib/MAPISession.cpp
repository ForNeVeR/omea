// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "MAPISession.h"
#include "FormViewer.h"
#include "MsgStore.h"
#include "MsgStoresPreloaded.h"
#include "EMessage.h"
#include "StringStream.h"
#include "typefactory.h"
#include "AddrBook.h"
#include "MailUser.h"
#include "EntryID.h"
#include "FormManager.h"
#include "ESPropValue.h"
#include "CharBuffer.h"
#include "EMAPIFolder.h"
#include "StringConvertion.h"
#include "guard.h"

#pragma unmanaged

#ifdef EMAPI_MANAGED
#pragma managed
#endif

MAPISession::MAPISession()
{
}

LPMAPISESSION MAPISession::GetRaw() const
{
    return _pSession;
}

bool MAPISession::Initialize( bool pickLogonProfile )
{
    MAPIINIT_0 MAPIInit;
    MAPIInit.ulFlags = 0x8;//MAPI_NO_COINIT;
    MAPIInit.ulVersion = (int)MAPI_INIT_VERSION;

    HRESULT hRes = MAPIInitialize( &MAPIInit );

    // http://support.microsoft.com/default.aspx?scid=kb;EN-US;239853
    // Please note the MAPI_NO_COINIT flag was added to MAPI in Exchange 5.5 SP1.
    if ( hRes == (int)MAPI_E_UNKNOWN_FLAGS )
    {
        MAPIInit.ulFlags = 0;
        hRes = MAPIInitialize( &MAPIInit );
    }

    if ( S_OK != hRes )
    {
        ::OutputDebugString( "MAPIInitialize failed: hResult=" );
        Guard::CheckHR( hRes );
        return false;
    }
    hRes = MAPILogonEx( 0, NULL, NULL,
        ( pickLogonProfile ? (int)MAPI_LOGON_UI : (int)MAPI_USE_DEFAULT ) | (int)MAPI_FORCE_DOWNLOAD | (int)MAPI_EXTENDED | (int)MAPI_NEW_SESSION,
        &_pSession );

    if ( hRes != S_OK && !pickLogonProfile )
    {
        //(int)MAPI_ALLOW_OTHERS
        ::OutputDebugString( "MAPILogon with default profile failed: hResult=" );
        hRes = MAPILogonEx( 0, NULL, NULL,
            (int)( MAPI_LOGON_UI | MAPI_FORCE_DOWNLOAD | MAPI_EXTENDED | MAPI_NEW_SESSION ),
            &_pSession );
    }

    if ( S_OK != hRes )
    {
        ::OutputDebugString( "MAPILogon failed: hResult=" );
        Guard::CheckHR( hRes );
        return false;
    }
    return true;
}

void MAPISession::Uninitialize()
{
    OutputDebugString( "MAPISession::Uninitialize()" );
    if ( NULL != _pSession )
    {
        UlRelease(_pSession);
    }
    MAPIUninitialize();
}
MsgStoreSPtr MAPISession::OpenMsgStore( const EntryIDSPtr& entryID ) const
{
    return MsgStore::OpenMsgStore( _pSession, entryID );
}
bool MAPISession::IsStandartReply( const EMessageSPtr& eMessage )
{
    StringStreamSPtr streamComp = eMessage->openStreamProperty( (int)PR_RTF_COMPRESSED );
    if ( !streamComp.IsNull() )
    {
        StringStreamSPtr stream = streamComp->GetWrapCompressedRTFStream();
        if ( !stream.IsNull() )
        {
            stream->Read();
            StringStream::Format format = stream->GetStreamFormat();

            if ( format != StringStream::Format::PlainText )
            {
                return true;
            }
            return false;
        }
    }
    CharBufferSPtr prHTML = eMessage->openStringProperty( (int)0x10130102 );
    if ( !prHTML.IsNull() )
    {
        return true;
    }
    return false;
}
AddrBookSPtr MAPISession::OpenAddressBook() const
{
    return AddrBook::OpenAddressBook( _pSession );
}
void MAPISession::AddRecipient( const EMessageSPtr& eMessage, LPWSTR displayName, LPWSTR email, LPSTR displayNameA, LPSTR emailA, int recType ) const
{
    HRESULT hr = eMessage->AddRecipient( _pSession, displayName, email, displayNameA, emailA, true, recType );
    if ( (int)MAPI_E_BAD_CHARWIDTH == hr )
    {
        eMessage->AddRecipient( _pSession, displayName, email, displayNameA, emailA, false, recType );
    }
}
EMAPIFolderSPtr MAPISession::GetFolderForMessageCreation()
{
    MsgStoreSPtr defaultStore = GetDefaultStore();
    if ( !defaultStore.IsNull() )
    {
        return defaultStore->GetFolderForMessageCreation();
    }
    return EMAPIFolderSPtr( NULL );
}
MsgStoreSPtr MAPISession::GetDefaultStore( )
{
    // if we're already opened and cached the default store, return the cached one
    //if ( _defaultStoreIndex >= 0 )
    //{
        Guard::ThrowNotImplementedException( "GetDefaultStore" );

        //return _msgStores [_defaultStoreIndex];
    //}

    LPMDB pMDB = NULL;
    HRESULT hr = E_FAIL;
	enum { EID, NAME, STORE, NUM_COLS };
	LPMAPITABLE pStoresTbl = NULL;

	HRESULT hRes = _pSession->GetMsgStoresTable( 0, &pStoresTbl );
	if ( SUCCEEDED( hRes ) )
    {
    	SPropValue spv;
	    static SRestriction sres;
    	LPSRowSet pRow = NULL;
	    //set up restriction for the default store
	    sres.rt = (int)RES_PROPERTY;//gonna compare a property
	    sres.res.resProperty.relop = (int)RELOP_EQ;//gonna test equality
	    sres.res.resProperty.ulPropTag = (int)PR_DEFAULT_STORE;//tag to compare
	    sres.res.resProperty.lpProp = &spv;//prop tag to compare against

	    spv.ulPropTag = (int)PR_DEFAULT_STORE;//tag type
	    spv.Value.b   = TRUE;//tag value

        static SizedSPropTagArray( NUM_COLS, sptCols ) =
            { NUM_COLS, (int)PR_ENTRYID, (int)PR_DISPLAY_NAME, (int)PR_DEFAULT_STORE };

	    hRes = HrQueryAllRows( pStoresTbl, (LPSPropTagArray)&sptCols, &sres, NULL, 0, &pRow );

	    if ( SUCCEEDED(hRes) )
        {
            LPSPropValue lpProp = &pRow->aRow[0].lpProps[EID];
	        hRes = _pSession->OpenMsgStore(0, lpProp->Value.bin.cb, (LPENTRYID)lpProp->Value.bin.lpb,
                NULL, (int)MDB_WRITE,  &pMDB );
	        if ( SUCCEEDED(hRes) )
            {
                hr = S_OK;
            }
        	FreeProws(pRow);
        }
    }
    UlRelease( pStoresTbl );
    if ( pMDB != NULL )
    {
        return TypeFactory::CreateMsgStore( pMDB, _pSession );
    }
    else
    {
    	return MsgStoreSPtr( NULL );
    }
}
MAPISession::~MAPISession( )
{
    ::OutputDebugString( "~ESession" );
}
MsgStoresSPtr MAPISession::GetMsgStores()
{
    MsgStoresSPtr _msgStores = MsgStores::Get( _pSession );
    if ( _msgStores->PrepareMsgStoresTable() && _msgStores->QueryAllRows() )
    {
        return _msgStores;
    }
    return MsgStoresSPtr( NULL );
}
bool MAPISession::CompareEntryIDs( const EntryIDSPtr& entryID1, const EntryIDSPtr& entryID2 )
{
	ULONG result = 0;
	HRESULT hr = _pSession->CompareEntryIDs( entryID1->GetLength(), entryID1->getLPENTRYID(),
		entryID2->GetLength(), entryID2->getLPENTRYID(), 0, &result );
	if ( !SUCCEEDED( hr ) )
	{
		return false;
	}

	return result != FALSE;
}
