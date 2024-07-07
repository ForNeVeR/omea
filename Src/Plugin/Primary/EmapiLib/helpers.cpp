// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include "CharsStorage.h"
#define long __int32
#include "typefactory.h"

#include "helpers.h"
using namespace System::Diagnostics;
using namespace System::Text;
using namespace System::Runtime::InteropServices;

#include "CharBuffer.h"
#include "ESPropValue.h"
#include "EntryID.h"
#include "StringConvertion.h"
#include "temp.h"
#include "guard.h"

String* Helper::BinPropToString( const ESPropValueSPtr& prop )
{
    if ( prop.IsNull() ) return NULL;
    return Helper::EntryIDToHex( prop->GetBinLPBYTE(), prop->GetBinCB() );
}

String* Helper::EntryIDToHex( const LPBYTE bytes, int count )
{
    if ( count == 0 || bytes == 0 )
    {
        return NULL;
    }
    CharBufferSPtr EID = TypeFactory::CreateCharBuffer( count * 2 + 1 );
    if ( EID.IsNull() )
    {
        return NULL;
    }
    HexFromBin( bytes, count, EID->Get() );
    String* strEntryID = new String( EID->Get() );
    return strEntryID;
}

EntryIDSPtr Helper::HexToEntryID( String* hex )
{
    if ( hex == NULL )
    {
        throw new System::ArgumentNullException( "hex" );
    }
    if ( hex->get_Length() == 0 )
    {
        throw new System::ArgumentException( "hex should be with length more then 0" );
    }
    int cb = hex->get_Length()/2;
    if ( cb != 0 )
    {
        LPBYTE bytes = NULL;
        HRESULT hr = MAPIAllocateBuffer( cb, (void **)&bytes );
        if ( hr == S_OK )
        {
            ANSIStringSPtr ansi = Temp::GetANSIString( hex );
            if ( FBinFromHex( ansi->GetChars(), bytes ) == TRUE )
            {
                return TypeFactory::CreateEntryID( bytes, cb );
            }
            MAPIFreeBuffer( bytes );
            StringBuilder* str = new StringBuilder();
            str->Append( "Can't convert hex string to entryid because FBinFromHex returned FALSE: \n" );
            str->Append( hex );
            str->Append( new String( " \n" ) );
            str->Append( new String( ansi->GetChars() ) );
            throw new System::ArgumentException( str->ToString() );
        }
        throw new System::ArgumentException( "Can't convert hex string to entryid because can't allocate buffer" );
    }
    throw new System::ArgumentException( "Can't convert hex string to entryid because length == 0" );
}

EMAPILib::MAPINtf* Helper::GetNewMailNtf( _NOTIFICATION notification )
{
    String* entryID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.newmail.lpEntryID, notification.info.newmail.cbEntryID );
    String* parentID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.newmail.lpParentID, notification.info.newmail.cbParentID );

    return new EMAPILib::MAPINtf( parentID, entryID );
}

EMAPILib::MAPINtf* Helper::GetMAPINtf( _NOTIFICATION notification )
{
    String* entryID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpEntryID, notification.info.obj.cbEntryID );
    String* parentID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpParentID, notification.info.obj.cbParentID );

    return new EMAPILib::MAPINtf( parentID, entryID );
}

EMAPILib::MAPIFullNtf* Helper::GetMAPIFullNtf( _NOTIFICATION notification )
{
    String* entryID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpEntryID, notification.info.obj.cbEntryID );
    String* parentID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpParentID, notification.info.obj.cbParentID );
    String* oldParentID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpOldParentID, notification.info.obj.cbOldParentID );
    String* oldEntryID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpOldID, notification.info.obj.cbOldID );

    return new EMAPILib::MAPIFullNtf( parentID, entryID, oldParentID, oldEntryID );
}


void Helper::SetGUID( LPGUID lpGUID, System::Guid* gcGUID )
{
    unsigned char bytes __gc[]  = gcGUID->ToByteArray();
    for ( int i = 0; i < 16; i++ )
    {
        ((BYTE*)lpGUID)[i] = bytes[i];
    }
}
void Helper::MarshalCopy( byte* bytes, unsigned char destination __gc[], int startIndex, int count )
{
    Marshal::Copy( bytes, destination, startIndex, count );
}

EMAPILib::Disposable::Disposable()
{
    _disposed = false;
}
EMAPILib::Disposable::~Disposable()
{
    try
    {
        Dispose();
    }
    catch(...){}
}

void EMAPILib::Disposable::CheckDisposed()
{
    if ( _disposed )
    {
        Guard::ThrowObjectDisposedException( "Object was disposed" );
    }
}

void EMAPILib::Disposable::Dispose()
{
    DisposeImpl();
}
void EMAPILib::Disposable::DisposeImpl()
{
    _disposed = true;
}
