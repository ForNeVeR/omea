/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma unmanaged

#include "typefactory.h"
#include "mapiprop.h"
#include "ESPropValue.h"
#include "StringStream.h"
#include "CharBuffer.h"
#include "CharsStorage.h"
#include "guard.h"

#include "RCPtrDef.h"
template RCPtr<PropTagArray>;

#ifdef EMAPI_MANAGED
#pragma managed
#endif

MAPIProp::MAPIProp( LPMAPIPROP mapiProp )
{
    if ( mapiProp == NULL )
    {
        Guard::ThrowArgumentNullException( "mapiProp" );
    }
    _mapiProp = mapiProp;
}

MAPIProp::~MAPIProp()
{
    if ( _mapiProp != NULL )
    {
        try
        {
            _mapiProp->Release();
        }
        catch (...){}
    }
}
PropTagArraySPtr MAPIProp::getPropList() const
{
    LPSPropTagArray lppPropTagArray = NULL;
    HRESULT hr = _mapiProp->GetPropList( 0, &lppPropTagArray );
    if ( hr == S_OK )
    {
        return TypeFactory::CreatePropTagArray( lppPropTagArray );
    }
    if ( hr == MAPI_W_ERRORS_RETURNED )
    {
        MAPIFreeBuffer( lppPropTagArray );
    }
    return PropTagArraySPtr( NULL );
}
void MAPIProp::CopyTo( LPCIID lpInterface, LPVOID lpDestObj ) const
{
    if ( lpInterface == NULL )
    {
        Guard::ThrowArgumentNullException( "lpInterface" );
    }
    if ( lpDestObj == NULL )
    {
        Guard::ThrowArgumentNullException( "lpDestObj" );
    }
    _mapiProp->CopyTo( 0, NULL, 0, NULL, NULL, lpInterface, lpDestObj, 0, NULL );
}

ESPropValueSPtr MAPIProp::getSingleProp( int tag ) const
{
    return ESPropValue::GetSimpleProp( _mapiProp, tag );
}

void MAPIProp::setSimpleProp( LPSPropValue lpPropValue ) const
{
    ESPropValue::SetSimpleProp( _mapiProp, lpPropValue );
}

void MAPIProp::setStringProp( int tag, LPSTR lpStr ) const
{
    SPropValue propValue;
    propValue.ulPropTag = tag;

    propValue.Value.lpszA = lpStr;
    setSimpleProp( &propValue );
}
void MAPIProp::setStringArray( int tag, LPSTR* lppsz, int count ) const
{
    SLPSTRArray mvArray;
    mvArray.cValues = count;
    mvArray.lppszA = lppsz;
    SPropValue propValue;
    propValue.ulPropTag = tag;
    propValue.Value.MVszA = mvArray;
    setSimpleProp( &propValue );
}

void MAPIProp::deleteSimpleProp( int tag ) const
{
    ESPropValue::DeleteSimpleProp( _mapiProp, tag );
}

StringStreamSPtr MAPIProp::openStreamPropertyToWrite( int tag ) const
{
    LPSTREAM lpStream = NULL;
    HRESULT hr = _mapiProp->OpenProperty( tag, &IID_IStream, (int)(STGM_WRITE | STGM_DIRECT),  
        (int)(MAPI_CREATE | MAPI_MODIFY), (LPUNKNOWN *)&lpStream );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateStringStream( lpStream );
    }
    return StringStreamSPtr( NULL );
}

void MAPIProp::writeStringStreamProp( int tag, LPSTR propValue, int size ) const
{
    StringStreamSPtr stream = openStreamPropertyToWrite( tag );
    if ( stream.IsNull() )
    {
        return;
    }
    stream->Write( propValue, size );
    stream->Commit();
}
StringStreamSPtr MAPIProp::openStreamProperty( int tag ) const
{
    LPSTREAM lpStream = NULL;
	HRESULT hr = _mapiProp->OpenProperty( tag, &IID_IStream, 0, 0, (LPUNKNOWN *)&lpStream );
    if ( hr == S_OK )
    {
        return TypeFactory::CreateStringStream( lpStream );
    }
    return StringStreamSPtr( NULL );
}
CharBufferSPtr MAPIProp::openStringProperty( int tag ) const
{
    return openStringProperty( tag, -1 );
}
CharBufferSPtr MAPIProp::openStringProperty( int tag, int sizeToRead ) const
{
    StringStreamSPtr streamComp = openStreamProperty( tag );
    if ( streamComp.IsNull() )
    {
        return CharBufferSPtr( NULL );
    }
    if ( sizeToRead == -1 )
    {
        streamComp->ReadToEnd();
    }
    else
    {
        streamComp->Read( 255 );
    }
    return streamComp->GetBuffer();
}
void MAPIProp::setDateTimeProp( int tag, ULONGLONG value ) const
{
    _FILETIME ft;
    setFILETIME( &ft, value );

    SPropValue prop;
    prop.ulPropTag = tag;
    prop.Value.ft = ft;
    setSimpleProp( &prop );
}
void MAPIProp::setBoolProp( int tag, BOOL value ) const
{
    SPropValue prop;
    prop.ulPropTag = tag;
    prop.Value.b = (unsigned short)value;
    setSimpleProp( &prop );
}
void MAPIProp::setLongProp( int tag, int value ) const
{
    SPropValue prop;
    prop.ulPropTag = tag;
    prop.Value.l = value;
    setSimpleProp( &prop );
}

void MAPIProp::setFILETIME( _FILETIME* ft, ULONGLONG value ) const
{
    ft->dwHighDateTime = (DWORD)(value >> 32);
    ft->dwLowDateTime = (DWORD)(value & 0x00000000FFFFFFFF);
}
int MAPIProp::getIDsFromNames( LPGUID lpGUID, LPWSTR name, int propType ) const
{
    return ESPropValue::GetIDsFromNames( _mapiProp, lpGUID, name, propType );
}
int MAPIProp::getIDsFromNames( LPGUID lpGUID, int lID, int propType ) const
{
    return ESPropValue::GetIDsFromNames( _mapiProp, lpGUID, lID, propType );
}
HRESULT MAPIProp::SaveChanges( int flags ) const
{
    return _mapiProp->SaveChanges( flags );
}
PropTagArray::PropTagArray( LPSPropTagArray propTags )
{
    _propTags = propTags;
}
PropTagArray::~PropTagArray()
{
    try
    {
        MAPIFreeBuffer( _propTags );
    }
    catch (...){}
}
int PropTagArray::GetCount() const
{
    return _propTags->cValues;
}
int PropTagArray::GetTag( int index ) const
{
    return _propTags->aulPropTag[index];
}
