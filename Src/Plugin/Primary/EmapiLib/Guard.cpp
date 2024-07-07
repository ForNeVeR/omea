// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include "CharsStorage.h"
#define long __int32
#using <mscorlib.dll>
#include "guard.h"
#include "emapilib.h"
#include "MsgStoreAdviseSink.h"
using namespace System::Runtime::InteropServices;
using namespace System;

void Guard::BeginReadProp( int prop_id )
{
    EMAPILib::EMAPISession::BeginReadProp( prop_id );
}
void Guard::EndReadProp()
{
    EMAPILib::EMAPISession::EndReadProp();
}

int Guard::RegisterForm()
{
    return EMAPILib::EMAPISession::RegisterForm();
}

void Guard::UnregisterForm( int formID )
{
    EMAPILib::EMAPISession::UnregisterForm( formID );
}

void Guard::FreeCoTaskMem( LPSTR str )
{
    Marshal::FreeCoTaskMem(static_cast<IntPtr>(static_cast<void*>( str )));
}

void Guard::FreeCoTaskMem( LPWSTR str )
{
    Marshal::FreeCoTaskMem(static_cast<IntPtr>(static_cast<void*>( str )));
}

HRESULT Guard::CopySBinary(LPSBinary psbDest,const LPSBinary psbSrc, LPVOID pParent)
{
    if ( psbDest == NULL || psbSrc == NULL ) return E_FAIL;
    HRESULT hRes = S_OK;

    psbDest->cb = psbSrc->cb;

    if ( psbSrc->cb )
    {
        if ( pParent != NULL )
        {
            hRes = MAPIAllocateMore( psbSrc->cb, pParent, (LPVOID*)&psbDest->lpb );
        }
        else
        {
            hRes = MAPIAllocateBuffer(psbSrc->cb, (LPVOID*)&psbDest->lpb );
        }
        if ( !FAILED( hRes ) )
        {
            CopyMemory(psbDest->lpb,psbSrc->lpb,psbSrc -> cb);
        }
    }

    return hRes;
}

int Guard::GetRealCodePage( const CharsStorageSPtr& buffer )
{
    int result = 0;
    try
    {
        const char* buf = buffer->GetBuffer( 0 );

        System::String^ str = gcnew System::String( buf );
        int index = str->IndexOf( "\\ansicpg" );
        if ( index != -1 )
        {
            int endIndex = str->IndexOf( "\\", index + 1 );
            if ( endIndex != -1 )
            {
                index = index + 8;
                System::String ^substring = str->Substring( index, endIndex - index );
                //System::Diagnostics::Debug::WriteLine( substring );
                result = System::Convert::ToInt32( substring, 10 ) ;
            }
        }
    }
    catch(...)
    {
        System::Diagnostics::Debug::WriteLine( "Exception was thrown when parsing codepage" );
    }
    return result;
}

void Guard::CheckHR( HRESULT hr, const MAPILastErrorBase& mapiProp )
{
    if ( hr == (int)MAPI_E_EXTENDED_ERROR )
    {
        LPMAPIERROR pMAPIError = NULL;
        HRESULT hRes = mapiProp.GetLastError( &pMAPIError );
        MAPIBuffer mapiBuffer( hRes, pMAPIError );
        if ( hRes != S_OK )
        {
            String^ component = gcnew String( pMAPIError->lpszComponent );
            String^ error = gcnew String( pMAPIError->lpszError );
            String^ errorMessage = component->Concat( component, " : " );
            errorMessage = errorMessage->Concat( errorMessage, error );
            throw gcnew System::Runtime::InteropServices::COMException( errorMessage, (int)MAPI_E_EXTENDED_ERROR );
        }
        throw gcnew System::Runtime::InteropServices::COMException( "No message for exception", (int)MAPI_E_EXTENDED_ERROR );
    }
    CheckHR( hr );
}

void Guard::ThrowProblemWhenOpenStorage( HRESULT hr, LPSPropValue lpProp )
{
    ESPropValueSPtr prop = TypeFactory::CreateESPropValue( lpProp );
    if ( !prop.IsNull() )
    {
        String ^storeId = Helper::BinPropToString( prop );
        switch ( hr )
        {
        case MAPI_E_USER_CANCEL:
            throw gcnew EMAPILib::CancelledByUser( storeId );
        case MAPI_E_UNCONFIGURED:
            throw gcnew EMAPILib::StorageUnconfigured( storeId );
        }
        throw gcnew EMAPILib::ProblemWhenOpenStorage( storeId );
    }

    throw gcnew EMAPILib::ProblemWhenOpenStorage( nullptr );
}

void Guard::ThrowNotImplementedException( LPSTR message )
{
    throw gcnew System::NotImplementedException( gcnew String( message ) );
}
void Guard::CheckHR( HRESULT hr )
{
    Marshal::ThrowExceptionForHR( hr );
}
void Guard::ThrowArgumentNullException( LPSTR message )
{
    throw gcnew System::ArgumentNullException( gcnew String( message ) );
}
void Guard::ThrowObjectDisposedException( LPSTR message )
{
    throw gcnew System::ObjectDisposedException( gcnew String( message ) );
}
void Guard::ThrowArgumentOutOfRangeException( LPSTR message )
{
    throw gcnew System::ArgumentOutOfRangeException( gcnew String( message ) );
}
void Guard::SetFILETIME( _FILETIME* ft, ULONGLONG value )
{
    ft->dwHighDateTime = (DWORD)(value >> 32);
    ft->dwLowDateTime = (DWORD)(value & 0x00000000FFFFFFFF);
}
void Guard::DeleteMessage( const ESPropValueSPtr& entryID )
{
    EMAPILib::EMAPISession::DeleteMessage( entryID );
}
void Guard::MoveMessage( const ESPropValueSPtr& entryID, const ESPropValueSPtr& folderID )
{
    EMAPILib::EMAPISession::MoveMessage( entryID, folderID );
}
void Guard::CopyMessage( const ESPropValueSPtr& entryID, const ESPropValueSPtr& folderID )
{
    EMAPILib::EMAPISession::CopyMessage( entryID, folderID );
}

HRESULT Guard::HrThisThreadAdviseSink( MsgStoreAdviseSink* sink, LPMAPIADVISESINK* lppAdvSink )
{
    return ::HrThisThreadAdviseSink( sink, lppAdvSink );
}
