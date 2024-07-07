// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "emessage.h"
#include "ESPropValue.h"
#include "ETable.h"
#include "EAttach.h"
#include "guard.h"

#include "RCPtrDef.h"
#include <imessage.h>
// {00020D0B-0000-0000-C000-000000000046}
DEFINE_GUID( CLSID_MailMessage, 0x00020D0B, 0x0000, 0x0000, 0xC0, 0x00, 0x0, 0x00, 0x0, 0x00, 0x00, 0x46 );

template RCPtr<EMessage>;

#ifdef EMAPI_MANAGED
#pragma managed
#endif

EMessage::EMessage( LPMESSAGE lpMessage ) : MAPIProp( lpMessage )
{
    if ( lpMessage == NULL )
    {
        Guard::ThrowArgumentNullException( "lpMessage" );
    }
    _lpMessage = lpMessage;
}

EMessage::~EMessage()
{
    _lpMessage = NULL;
}

void EMessage::CopyTo( const EMessageSPtr& destMessage ) const
{
    MAPIProp::CopyTo( &IID_IMessage, destMessage->_lpMessage );
}

LPMESSAGE EMessage::GetRaw() const
{
    return _lpMessage;
}

int EMessage::GetInternetCPID() const
{
    ESPropValueSPtr prop = getSingleProp( (int)0x3FDE0003 );
    if ( !prop.IsNull() )
    {
        return prop->GetLong();
    }
    return 0;
}

void EMessage::SetConversation( const EMessageSPtr& parent ) const
{
    ESPropValueSPtr prop = parent->getSingleProp( (int)PR_CONVERSATION_INDEX );
    if ( !prop.IsNull() )
    {
        SPropValue pChildConvIndex[1];
        pChildConvIndex[0].ulPropTag = (int)PR_CONVERSATION_INDEX;

        HRESULT hr = ScCreateConversationIndex( prop->GetBinCB(), prop->GetBinLPBYTE(),
            &pChildConvIndex[0].Value.bin.cb, &pChildConvIndex[0].Value.bin.lpb );

        hr = _lpMessage->SetProps( 1, pChildConvIndex, NULL);
    }
}

void EMessage::RTFSyncBody() const
{
    ESPropValueSPtr rtfSync = getSingleProp( (int)PR_RTF_IN_SYNC );
    if ( !rtfSync.IsNull() )
    {
        if ( !rtfSync->GetBool() )
        {
            BOOL fUpdated = FALSE;
            ::RTFSync( _lpMessage, (int)RTF_SYNC_BODY_CHANGED, &fUpdated );
        }
    }
}

void EMessage::RTFSyncRTF() const
{
    BOOL fUpdated = FALSE;
    ::RTFSync( _lpMessage, (int)RTF_SYNC_RTF_CHANGED, &fUpdated );
}
#define PR_RECIPIENT_FLAGS 0x5FFD0003
#define PR_SEND_INTERNET_ENCODING 0x3A710003
#define PR_RECIPIENT_ENTRYID 0x5FF70102
#define PR_RECIPIENT_TRACKSTATUS 0x5FFF0003
#define PR_RECIPIENT_DISPLAY_NAME 0x5FF6001E
void EMessage::AddRecipient( ELPSRowSetSPtr row, int /*recType*/ ) const
{
    HRESULT hr = _lpMessage->ModifyRecipients( (int)MODRECIP_ADD, (LPADRLIST)row.get()->GetRaw() );
    Guard::CheckHR( hr );
}
HRESULT EMessage::AddRecipient( LPMAPISESSION pSession, LPWSTR displayName, LPWSTR email,
                               LPSTR displayNameA, LPSTR emailA, bool unicode, int recType ) const
{
    HRESULT hRes   = S_OK;      // Status code of MAPI calls
    LPADRLIST pAdrList   = NULL;  // ModifyRecips takes LPADRLIST
    LPADRBOOK lpAddrBook = NULL;

    enum { NAME, ADDR, EMAIL, RECIP, EID, RICH_INFO, RECIPIENT_TYPE, FORMAT, RECIPIENT_ENTRYID, TRACKSTATUS, OBJTYPE, DISPLAY_TYPE, RECIPIENT_DISPLAY_NAME, NUM_RECIP_PROPS = 13 };

    // Allocate memory for new SRowSet structure.
    hRes = MAPIAllocateBuffer( CbNewSRowSet(1), (LPVOID*) &pAdrList);
    if (FAILED(hRes)) goto Quit;

    // Zero out allocated memory.
    ZeroMemory( pAdrList, CbNewSRowSet(1) );

    // Allocate memory for SPropValue structure that indicates what
    // recipient properties will be set. NUM_RECIP_PROPS == 5.
    hRes = MAPIAllocateBuffer( NUM_RECIP_PROPS * sizeof(SPropValue), (LPVOID*) &(pAdrList->aEntries[0].rgPropVals));
    if (FAILED(hRes)) goto Quit;

    // Zero out allocated memory.
    ZeroMemory(pAdrList -> aEntries[0].rgPropVals, NUM_RECIP_PROPS * sizeof(SPropValue) );

    // Setup the One Time recipient by indicating how many
    // recipients and how many properties will be set on each // recipient.

    pAdrList->cEntries = 1;   // How many recipients.
    // How many properties per recipient
    pAdrList->aEntries[0].cValues = NUM_RECIP_PROPS;

    if ( unicode )
    {
        // Set the SPropValue members == the desired values.
        pAdrList->aEntries[0].rgPropVals[NAME].ulPropTag = (int)PR_DISPLAY_NAME_W;
        pAdrList->aEntries[0].rgPropVals[NAME].Value.lpszW = displayName;

        pAdrList->aEntries[0].rgPropVals[RECIPIENT_DISPLAY_NAME].ulPropTag = (int)PR_RECIPIENT_DISPLAY_NAME;
        pAdrList->aEntries[0].rgPropVals[RECIPIENT_DISPLAY_NAME].Value.lpszW = displayName;

        pAdrList->aEntries[0].rgPropVals[ADDR].ulPropTag = (int)PR_ADDRTYPE_W;

        pAdrList->aEntries[0].rgPropVals[EMAIL].ulPropTag = (int)PR_EMAIL_ADDRESS_W;
        pAdrList->aEntries[0].rgPropVals[EMAIL].Value.lpszW = email;

        if ( email != NULL && wcscspn( email, L"/" ) == 0 )
        {
            pAdrList->aEntries[0].rgPropVals[ADDR].Value.lpszW = L"EX";
        }
        else
        {
            pAdrList->aEntries[0].rgPropVals[ADDR].Value.lpszW = L"SMTP";
        }
    }
    else
    {
        // Set the SPropValue members == the desired values.
        pAdrList->aEntries[0].rgPropVals[NAME].ulPropTag = (int)PR_DISPLAY_NAME_A;
        pAdrList->aEntries[0].rgPropVals[NAME].Value.lpszA = displayNameA;

        pAdrList->aEntries[0].rgPropVals[RECIPIENT_DISPLAY_NAME].ulPropTag = (int)PR_RECIPIENT_DISPLAY_NAME;
        pAdrList->aEntries[0].rgPropVals[RECIPIENT_DISPLAY_NAME].Value.lpszA = displayNameA;

        pAdrList->aEntries[0].rgPropVals[ADDR].ulPropTag = (int)PR_ADDRTYPE_A;

        pAdrList->aEntries[0].rgPropVals[EMAIL].ulPropTag = (int)PR_EMAIL_ADDRESS_A;
        pAdrList->aEntries[0].rgPropVals[EMAIL].Value.lpszA = emailA;

        if ( email != NULL && wcscspn( email, L"/" ) == 0 )
        {
            pAdrList->aEntries[0].rgPropVals[ADDR].Value.lpszA = "EX";
        }
        else
        {
            pAdrList->aEntries[0].rgPropVals[ADDR].Value.lpszA = "SMTP";
        }
    }

    pAdrList->aEntries[0].rgPropVals[RECIP].ulPropTag = (int)PR_RECIPIENT_TYPE;
    pAdrList->aEntries[0].rgPropVals[RECIP].Value.l = recType;

    pAdrList->aEntries[0].rgPropVals[EID].ulPropTag = (int)PR_ENTRYID;
    pAdrList->aEntries[0].rgPropVals[RECIPIENT_ENTRYID].ulPropTag = (int)PR_RECIPIENT_ENTRYID;

    pAdrList->aEntries[0].rgPropVals[RICH_INFO].ulPropTag = (int)PR_SEND_RICH_INFO;
    pAdrList->aEntries[0].rgPropVals[RICH_INFO].Value.b = 0;

    pAdrList->aEntries[0].rgPropVals[RECIPIENT_TYPE].ulPropTag = (int)PR_RECIPIENT_FLAGS;
    pAdrList->aEntries[0].rgPropVals[RECIPIENT_TYPE].Value.l = 1;

    pAdrList->aEntries[0].rgPropVals[FORMAT].ulPropTag = (int)PR_SEND_INTERNET_ENCODING;
    pAdrList->aEntries[0].rgPropVals[FORMAT].Value.l = 393216;

    pAdrList->aEntries[0].rgPropVals[TRACKSTATUS].ulPropTag = (int)PR_RECIPIENT_TRACKSTATUS;
    pAdrList->aEntries[0].rgPropVals[TRACKSTATUS].Value.l = 0;

    pAdrList->aEntries[0].rgPropVals[OBJTYPE].ulPropTag = (int)PR_OBJECT_TYPE;
    pAdrList->aEntries[0].rgPropVals[OBJTYPE].Value.l = 6;

    pAdrList->aEntries[0].rgPropVals[DISPLAY_TYPE].ulPropTag = (int)PR_DISPLAY_TYPE;
    pAdrList->aEntries[0].rgPropVals[DISPLAY_TYPE].Value.l = 0;


    hRes = pSession->OpenAddressBook(0, NULL, (int)AB_NO_DIALOG, &lpAddrBook);
    if (FAILED(hRes)) goto Quit;
    // Create the One-off address and get an EID for it.
    hRes = lpAddrBook->CreateOneOff(
        pAdrList->aEntries[0].rgPropVals[NAME].Value.lpszA,
        pAdrList->aEntries[0].rgPropVals[ADDR].Value.lpszA,
        pAdrList->aEntries[0].rgPropVals[EMAIL].Value.lpszA,
        MAPI_SEND_NO_RICH_INFO | MAPI_UNICODE,
        &pAdrList->aEntries[0].rgPropVals[EID].Value.bin.cb,
        (LPENTRYID*)
        (&pAdrList->aEntries[0].rgPropVals[EID].Value.bin.lpb));
    if (FAILED(hRes)) goto Quit;

    int cb = pAdrList->aEntries[0].rgPropVals[EID].Value.bin.cb;
    hRes = lpAddrBook->ResolveName( 0L, 0L, NULL, pAdrList );
    if (FAILED(hRes)) goto Quit;

    cb = pAdrList->aEntries[0].rgPropVals[EID].Value.bin.cb;

    pAdrList->aEntries[0].rgPropVals[RECIPIENT_ENTRYID].ulPropTag = (int)PR_RECIPIENT_ENTRYID;
    pAdrList->aEntries[0].rgPropVals[RECIPIENT_ENTRYID].Value.bin.cb = cb;

    if ( cb > 24 )
    {
        LPBYTE byte = pAdrList->aEntries[0].rgPropVals[EID].Value.bin.lpb + 22;
        *byte = 0x07;
        byte = pAdrList->aEntries[0].rgPropVals[EID].Value.bin.lpb + 23;
        *byte = 0x80;
    }
    pAdrList->aEntries[0].rgPropVals[RECIPIENT_ENTRYID].Value.bin.lpb = NULL;
    hRes = MAPIAllocateBuffer( cb, (void **)&pAdrList->aEntries[0].rgPropVals[RECIPIENT_ENTRYID].Value.bin.lpb );
    if (FAILED(hRes)) goto Quit;

    CopyMemory( pAdrList->aEntries[0].rgPropVals[RECIPIENT_ENTRYID].Value.bin.lpb,
                pAdrList->aEntries[0].rgPropVals[EID].Value.bin.lpb, cb );

    pAdrList->aEntries[0].rgPropVals[RICH_INFO].ulPropTag = (int)PR_SEND_RICH_INFO;
    pAdrList->aEntries[0].rgPropVals[RICH_INFO].Value.b = 0;

    // If everything goes right, add the new recipient to the
    // message object passed into us.
    hRes = _lpMessage->ModifyRecipients( (int)MODRECIP_ADD, pAdrList );
    if (FAILED(hRes)) goto Quit;

Quit:
    // Always release any newly created objects and
    // allocated memory.
    FreePadrlist( pAdrList );
    UlRelease( lpAddrBook );
    return hRes;
}
void EMessage::AttachFile( LPSTR path, LPSTR fileName ) const
{
    LPSTREAM pStrmSrc = NULL;

    enum { FILENAME, METHOD, RENDERING, NUM_ATT_PROPS };
    SPropValue spvAttach[NUM_ATT_PROPS];

    HRESULT hRes = OpenStreamOnFile(MAPIAllocateBuffer, MAPIFreeBuffer, STGM_READ, path, NULL, &pStrmSrc );

    if ( !FAILED( hRes ))
    {
        ULONG ulAttNum;
        LPATTACH pAtt = NULL;
        hRes = _lpMessage->CreateAttach( NULL, 0, &ulAttNum, &pAtt );
        if ( !FAILED( hRes ) )
        {
            LPSTREAM pStrmDest = NULL;
            hRes = pAtt->OpenProperty( (int)PR_ATTACH_DATA_BIN, (LPIID)&IID_IStream, 0, (int)(MAPI_MODIFY | MAPI_CREATE), (LPUNKNOWN *)&pStrmDest );
            if ( !FAILED( hRes ) )
            {
                STATSTG StatInfo;
                pStrmSrc->Stat( &StatInfo, STATFLAG_NONAME );
                hRes = pStrmSrc->CopyTo( pStrmDest, StatInfo.cbSize, NULL, NULL );
                if ( !FAILED( hRes ) )
                {
                    spvAttach[FILENAME].ulPropTag = (int)PR_ATTACH_FILENAME;
                    spvAttach[FILENAME].Value.lpszA = fileName;

                    spvAttach[METHOD].ulPropTag = (int)PR_ATTACH_METHOD;
                    spvAttach[METHOD].Value.l = (int)ATTACH_BY_VALUE;

                    spvAttach[RENDERING].ulPropTag = (int)PR_RENDERING_POSITION;
                    spvAttach[RENDERING].Value.l = -1;

                    hRes = pAtt->SetProps( (int)NUM_ATT_PROPS, (LPSPropValue)&spvAttach, NULL );
                    if ( !FAILED( hRes ) )
                    {
                        pAtt->SaveChanges( 0 );
                    }
                }
            }

            if ( pStrmDest )
                pStrmDest->Release();
        }
        if ( pAtt )
            pAtt->Release();
    }
    if ( pStrmSrc )
        pStrmSrc->Release();
}


void EMessage::Submit() const
{
    HRESULT hr = _lpMessage->SubmitMessage( 0 );
    hr = hr;
}
ESPropValueSPtr EMessage::GetStatus() const
{
    const SizedSPropTagArray( 4, atProps ) =
    { 4, (int)PR_MSG_STATUS, (int)PR_MESSAGE_FLAGS, (int)PR_ACCESS_LEVEL, (int)PR_MESSAGE_CLASS };
    unsigned long ulTmp = 0;
    LPSPropValue pVal  = 0;
    HRESULT hr = _lpMessage->GetProps( (LPSPropTagArray)&atProps, 0, &ulTmp, &pVal );
    if ( SUCCEEDED( hr ) )
    {
        return TypeFactory::CreateESPropValue( pVal );
    }
    return ESPropValueSPtr( NULL );
}

void EMessage::SetUnRead( bool unread ) const
{
    if ( unread )
    {
        _lpMessage->SetReadFlag( (int)CLEAR_READ_FLAG );
    }
    else
    {
        _lpMessage->SetReadFlag( 0 );
    }
}

bool EMessage::Unread() const
{
    ESPropValueSPtr prop = getSingleProp( (int)PR_MESSAGE_FLAGS );
    if ( !prop.IsNull() )
    {
        int value = prop->GetLong();
        return ( ( value & 1 ) != 1 );
    }
    return false;
}
ETableSPtr EMessage::GetRecipientsTable() const
{
    LPMAPITABLE lpRecips = NULL;
    HRESULT hr = _lpMessage->GetRecipientTable( 0, &lpRecips );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateETable( lpRecips );
    }
    return ETableSPtr( NULL );
}

ETableSPtr EMessage::GetAttachmentTable() const
{
    LPMAPITABLE lpAttachments = NULL;
    HRESULT hr = _lpMessage->GetAttachmentTable( 0, &lpAttachments );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateAttachmentsETable( lpAttachments );
    }
    return ETableSPtr( NULL );
}
EAttachSPtr EMessage::OpenAttach( int attachmentNum ) const
{
    LPATTACH lpAttach = NULL;
    HRESULT hr = _lpMessage->OpenAttach( attachmentNum, &IID_IAttachment, 0, &lpAttach );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateEAttach( lpAttach );
    }
    return EAttachSPtr( NULL );
}
HRESULT EMessage::SaveToMSG( LPSTR szPath )
{
    LPMALLOC pMalloc = MAPIGetDefaultMalloc();
    if ( pMalloc == NULL ) return E_FAIL;

    LPWSTR lpWideCharStr = NULL;
    ULONG cbStrSize = MultiByteToWideChar( CP_ACP, MB_PRECOMPOSED, szPath, -1, lpWideCharStr, 0);

    HRESULT hr = MAPIAllocateBuffer( cbStrSize * sizeof(WCHAR), (LPVOID *)&lpWideCharStr );
    Guard::CheckHR( hr );
    MAPIBuffer mapiBuffer( hr, lpWideCharStr );

    MultiByteToWideChar( CP_ACP, MB_PRECOMPOSED, szPath, -1, lpWideCharStr, cbStrSize );

    LPSTORAGE pStorage = NULL;
    HRESULT hRes = ::StgCreateDocfile(lpWideCharStr, STGM_READWRITE | STGM_TRANSACTED | STGM_CREATE, 0, &pStorage );

    if ( hRes == S_OK )
    {
        LPMSGSESS pMsgSession =  NULL;
        hRes = ::OpenIMsgSession( pMalloc, 0, &pMsgSession );

        if ( hRes == S_OK )
        {
            LPMESSAGE pIMsg = NULL;
            hRes = ::OpenIMsgOnIStg( pMsgSession, MAPIAllocateBuffer, MAPIAllocateMore, MAPIFreeBuffer, pMalloc,
                            NULL, pStorage, NULL, 0, 0, &pIMsg );

            if ( hRes == S_OK )
            {
                hRes = WriteClassStg( pStorage, CLSID_MailMessage );

                if ( hRes == S_OK )
                {
                    SizedSPropTagArray ( 7, excludeTags );
                    excludeTags.cValues = 7;
                    excludeTags.aulPropTag[0] = PR_ACCESS;
                    excludeTags.aulPropTag[1] = PR_BODY;
                    excludeTags.aulPropTag[2] = PR_RTF_SYNC_BODY_COUNT;
                    excludeTags.aulPropTag[3] = PR_RTF_SYNC_BODY_CRC;
                    excludeTags.aulPropTag[4] = PR_RTF_SYNC_BODY_TAG;
                    excludeTags.aulPropTag[5] = PR_RTF_SYNC_PREFIX_COUNT;
                    excludeTags.aulPropTag[6] = PR_RTF_SYNC_TRAILING_COUNT;

                    hRes = _lpMessage->CopyTo( 0, NULL, (LPSPropTagArray)&excludeTags, NULL, NULL, (LPIID)&IID_IMessage,
                                pIMsg, 0, NULL );

                    if ( hRes == S_OK )
                    {
                        pIMsg->SaveChanges( (int)KEEP_OPEN_READWRITE );
                        hRes = pStorage->Commit( STGC_DEFAULT );
                    }
                }

                pIMsg->Release();
            }

            CloseIMsgSession( pMsgSession );
        }
        pStorage->Release();
    }
    return hRes;
}
#define STGM_DIRECT_SWMR 0x00400000L

EMessageSPtr EMessage::LoadFromMSG( LPSTR szPath )
{
    LPMALLOC pMalloc = MAPIGetDefaultMalloc();
    if ( pMalloc == NULL ) return EMessageSPtr( NULL );

    LPWSTR lpWideCharStr = NULL;
    ULONG cbStrSize = MultiByteToWideChar( CP_ACP, MB_PRECOMPOSED, szPath, -1, lpWideCharStr, 0);
    HRESULT hr = MAPIAllocateBuffer( cbStrSize * sizeof(WCHAR), (LPVOID *)&lpWideCharStr );
    Guard::CheckHR( hr );
    MAPIBuffer mapiBuffer( hr, lpWideCharStr );

    MultiByteToWideChar( CP_ACP, MB_PRECOMPOSED, szPath, -1, lpWideCharStr, cbStrSize );

    LPSTORAGE pStorage = NULL;
    HRESULT hRes = ::StgOpenStorage( lpWideCharStr, NULL, STGM_DIRECT_SWMR | STGM_READ | STGM_SHARE_DENY_NONE, NULL, 0, &pStorage );

    if ( hRes == S_OK )
    {
        LPMSGSESS pMsgSession =  NULL;
        hRes = ::OpenIMsgSession( pMalloc, 0, &pMsgSession );

        if ( hRes == S_OK )
        {
            LPMESSAGE pIMsg = NULL;
            hRes = ::OpenIMsgOnIStg( pMsgSession, MAPIAllocateBuffer, MAPIAllocateMore, MAPIFreeBuffer, pMalloc,
                            NULL, pStorage, NULL, 0, 0, &pIMsg );

            if ( hRes == S_OK )
            {
                return TypeFactory::CreateEMessage( pIMsg );
            }

            CloseIMsgSession( pMsgSession );
        }
        pStorage->Release();
    }
    return EMessageSPtr( NULL );
}
