// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "espropvalue.h"
#include "RCPtrDef.h"
#include "Guard.h"

template RCPtr<ESPropValue>;

void ESPropValue::SetSimpleProp( LPMAPIPROP lpProp, LPSPropValue lpPropValue )
{
    if ( lpProp == NULL )
    {
        Guard::ThrowArgumentNullException( "ESPropValue::SetSimpleProp: lpProp parameter must be not NULL" );
    }
    HRESULT hr = lpProp->SetProps( 1, lpPropValue, NULL );
    if ( hr == S_OK )
    {
    }
}

void ESPropValue::DeleteSimpleProp( LPMAPIPROP lpProp, int tag )
{
    if ( lpProp == NULL )
    {
        Guard::ThrowArgumentNullException( "ESPropValue::DeleteSimpleProp: lpProp parameter must be not NULL" );
    }
    const SizedSPropTagArray( 1, atProps ) = { 1, tag };
    HRESULT hr = lpProp->DeleteProps( (LPSPropTagArray)&atProps, NULL );
    if ( hr == S_OK )
    {
    }
}

ESPropValueSPtr ESPropValue::GetSimpleProp( LPMAPIPROP lpProp, int tag )
{
    if ( lpProp == NULL )
    {
        Guard::ThrowArgumentNullException( "ESPropValue::GetSimpleProp: lpProp parameter must be not NULL" );
    }
    const SizedSPropTagArray( 1, atProps ) = { 1, tag };
    unsigned long ulTmp = 0;
    LPSPropValue pVal = 0;

    Guard::BeginReadProp( tag );
    HRESULT hr = lpProp->GetProps( (LPSPropTagArray)&atProps, 0, &ulTmp, &pVal );
    Guard::EndReadProp();
    //TODO: it is necessary to process MAPI_E_NOT_ENOUGH_MEMORY through OpenProperty
    if ( hr == S_OK && pVal->Value.err != (int)MAPI_E_NOT_FOUND &&
        pVal->Value.err != (int)MAPI_E_NOT_ENOUGH_MEMORY && pVal->Value.err != (int)MAPI_E_BAD_CHARWIDTH && pVal != NULL )
    {
        return TypeFactory::CreateESPropValue( pVal );
    }
    else
    {
        MAPIBuffer mapiBuffer( hr, pVal );
        if ( hr == S_OK && pVal->Value.err != (int)MAPI_E_NOT_ENOUGH_MEMORY )
        {
            OutputDebugString( "GetSimpleProp: MAPI_E_NOT_ENOUGH_MEMORY" );
        }
        if ( hr == S_OK && pVal->Value.err != (int)MAPI_E_BAD_CHARWIDTH )
        {
            OutputDebugString( "GetSimpleProp: MAPI_E_BAD_CHARWIDTH" );
        }
    }
    return ESPropValueSPtr( NULL );
}
ESPropValue::ESPropValue( LPSPropValue pProp, bool isFreeNecessary ) : _isFreeNecessary( isFreeNecessary )
{
    if ( pProp == NULL )
    {
        Guard::ThrowArgumentNullException( "ESPropValue::ESPropValue: pProp parameter must be not NULL" );
    }
    _pProp = pProp;
}
ESPropValue::~ESPropValue()
{
    try
    {
        if ( _isFreeNecessary )
        {
            MAPIFreeBuffer( _pProp );
        }
    }
    catch(...){}
}
LPSTR ESPropValue::GetLPSTR( int index )
{
    return _pProp[index].Value.lpszA;
}
int ESPropValue::GetLong( int index )
{
    return _pProp[index].Value.l;
}
bool ESPropValue::GetBool( int index )
{
    return ( _pProp[index].Value.b == 1 );
}

int ESPropValue::GetBinCB( int index )
{
    return _pProp[index].Value.bin.cb;
}

SBinaryArray ESPropValue::GetMVbin( int index )
{
    return _pProp[index].Value.MVbin;
}

SLPSTRArray ESPropValue::GetMVszA( int index )
{
    return _pProp[index].Value.MVszA;
}

LPBYTE ESPropValue::GetBinLPBYTE( int index )
{
    return _pProp[index].Value.bin.lpb;
}
_FILETIME ESPropValue::GetFILETIME( int index )
{
    return _pProp[index].Value.ft;
}

int ESPropValue::GetIDs( LPMAPIPROP lpProp, LPMAPINAMEID lpNmid, int propType )
{
    LPSPropTagArray lpNamedPropTags = NULL;
    HRESULT hr = lpProp->GetIDsFromNames( 1, &lpNmid, (int)MAPI_CREATE, &lpNamedPropTags );
    MAPIBuffer mapiBuffer( hr, lpNamedPropTags );

    if ( hr == S_OK )
    {
        return PROP_TAG( propType, PROP_ID( lpNamedPropTags->aulPropTag[0] ));
    }
    return 0;
}

int ESPropValue::GetIDsFromNames( LPMAPIPROP lpProp, LPGUID lpGUID, LPWSTR name, int propType )
{
    if ( lpProp == NULL )
    {
        Guard::ThrowArgumentNullException( "ESPropValue::GetIDsFromNames: lpProp parameter must be not NULL" );
    }
    MAPINAMEID NamedID;
    LPMAPINAMEID lpNmid = &NamedID;
    NamedID.lpguid = lpGUID;
    NamedID.ulKind = MNID_STRING;
    NamedID.Kind.lpwstrName = name;
    return GetIDs( lpProp, lpNmid, propType );
}
int ESPropValue::GetIDsFromNames( LPMAPIPROP lpProp, LPGUID lpGUID, int lID, int propType )
{
    if ( lpProp == NULL )
    {
        Guard::ThrowArgumentNullException( "ESPropValue::GetIDsFromNames: lpProp parameter must be not NULL" );
    }
    MAPINAMEID NamedID;
    LPMAPINAMEID lpNmid = &NamedID;
    NamedID.lpguid = lpGUID;
    NamedID.ulKind = MNID_ID;
    NamedID.Kind.lID = lID;
    return GetIDs( lpProp, lpNmid, propType );
}
