// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "emapi.h"
#include "typefactory.h"
#define TEST_MAPI_MODIFY MAPI_MODIFY
//MAPI_MODIFY

class MsgStoreAdviseSink;

class MAPILastErrorBase
{
public:
    virtual ~MAPILastErrorBase(){};
    virtual HRESULT GetLastError( LPMAPIERROR* mapiError ) const = 0;
};

template<typename T> class MapiLastError : public MAPILastErrorBase
{
    T _t;
public:
    MapiLastError( T t ) : _t( t )
    {
    }
    virtual HRESULT GetLastError( LPMAPIERROR* mapiError ) const
    {
        return _t->GetLastError( (int)MAPI_E_EXTENDED_ERROR, 0, mapiError );
    }
};

class Guard
{
private:
    Guard();
public:
    static void FreeCoTaskMem( LPSTR str );
    static void FreeCoTaskMem( LPWSTR str );
    static void SetFILETIME( _FILETIME* ft, ULONGLONG value );
    static HRESULT CopySBinary( LPSBinary psbDest,const LPSBinary psbSrc, LPVOID pParent);
    static int GetRealCodePage( const CharsStorageSPtr& buffer );
    static void CheckHR( HRESULT hr, const MAPILastErrorBase& mapiProp );
    static void CheckHR( HRESULT hr );
    static void ThrowArgumentNullException( LPSTR message );
    static void ThrowObjectDisposedException( LPSTR message );
    static void ThrowArgumentOutOfRangeException( LPSTR message );
    static void ThrowProblemWhenOpenStorage( HRESULT hr, LPSPropValue lpProp );
    static void ThrowNotImplementedException( LPSTR message );
    static int RegisterForm();
    static void UnregisterForm( int formID );
    static void BeginReadProp( int prop_id );
    static void EndReadProp();
    static void DeleteMessage( const ESPropValueSPtr& entryID );
    static void MoveMessage( const ESPropValueSPtr& entryID, const ESPropValueSPtr& folderID );
    static void CopyMessage( const ESPropValueSPtr& entryID, const ESPropValueSPtr& folderID );
    static HRESULT HrThisThreadAdviseSink( MsgStoreAdviseSink* sink, LPMAPIADVISESINK* lppAdvSink );
};
