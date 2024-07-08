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

String^ Helper::BinPropToString( const ESPropValueSPtr& prop )
{
    if ( prop.IsNull() ) return nullptr;
    return Helper::EntryIDToHex( prop->GetBinLPBYTE(), prop->GetBinCB() );
}

String^ Helper::EntryIDToHex( const LPBYTE bytes, int count )
{
    if ( count == 0 || bytes == 0 )
    {
        return nullptr;
    }
    CharBufferSPtr EID = TypeFactory::CreateCharBuffer( count * 2 + 1 );
    if ( EID.IsNull() )
    {
        return nullptr;
    }
    HexFromBin( bytes, count, EID->Get() );
    String^ strEntryID = gcnew String( EID->Get() );
    return strEntryID;
}

EntryIDSPtr Helper::HexToEntryID( String^ hex )
{
    if ( hex == nullptr )
    {
        throw gcnew System::ArgumentNullException( "hex" );
    }
    if ( hex->Length == 0 )
    {
        throw gcnew System::ArgumentException( "hex should be with length more then 0" );
    }
    int cb = hex->Length/2;
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
            StringBuilder^ str = gcnew StringBuilder();
            str->Append( "Can't convert hex string to entryid because FBinFromHex returned FALSE: \n" );
            str->Append( hex );
            str->Append( gcnew String( " \n" ) );
            str->Append( gcnew String( ansi->GetChars() ) );
            throw gcnew System::ArgumentException( str->ToString() );
        }
        throw gcnew System::ArgumentException( "Can't convert hex string to entryid because can't allocate buffer" );
    }
    throw gcnew System::ArgumentException( "Can't convert hex string to entryid because length == 0" );
}

EMAPILib::MAPINtf^ Helper::GetNewMailNtf( _NOTIFICATION notification )
{
    String^ entryID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.newmail.lpEntryID, notification.info.newmail.cbEntryID );
    String^ parentID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.newmail.lpParentID, notification.info.newmail.cbParentID );

    return gcnew EMAPILib::MAPINtf( parentID, entryID );
}

EMAPILib::MAPINtf ^Helper::GetMAPINtf( _NOTIFICATION notification )
{
    String ^entryID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpEntryID, notification.info.obj.cbEntryID );
    String ^parentID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpParentID, notification.info.obj.cbParentID );

    return gcnew EMAPILib::MAPINtf( parentID, entryID );
}

EMAPILib::MAPIFullNtf ^Helper::GetMAPIFullNtf( _NOTIFICATION notification )
{
    String ^entryID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpEntryID, notification.info.obj.cbEntryID );
    String ^parentID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpParentID, notification.info.obj.cbParentID );
    String ^oldParentID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpOldParentID, notification.info.obj.cbOldParentID );
    String ^oldEntryID =
        Helper::EntryIDToHex( (LPBYTE)notification.info.obj.lpOldID, notification.info.obj.cbOldID );

    return gcnew EMAPILib::MAPIFullNtf( parentID, entryID, oldParentID, oldEntryID );
}


void Helper::SetGUID( LPGUID lpGUID, System::Guid% gcGUID )
{
    array<unsigned char> ^bytes = gcGUID.ToByteArray();
    for ( int i = 0; i < 16; i++ )
    {
        ((BYTE*)lpGUID)[i] = bytes[i];
    }
}
void Helper::MarshalCopy( byte* bytes, array<unsigned char> ^destination, int startIndex, int count )
{
    Marshal::Copy(static_cast<IntPtr>(bytes), destination, startIndex, count );
}

EMAPILib::Disposable::Disposable()
{
    _disposed = false;
}
EMAPILib::Disposable::!Disposable()
{
    try
    {
        this->~Disposable();
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

EMAPILib::Disposable::~Disposable()
{
    DisposeImpl();
}
void EMAPILib::Disposable::DisposeImpl()
{
    _disposed = true;
}
