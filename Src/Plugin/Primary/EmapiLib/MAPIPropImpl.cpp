// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include "mapipropimpl.h"
#include "mapiprop.h"
#include "guard.h"
#include "espropvalue.h"
#include "StringConvertion.h"
#include "temp.h"
#using <mscorlib.dll>

EMAPILib::MAPIPropImpl::MAPIPropImpl( MAPIProp* eMAPIProp ) : _eMAPIProp( eMAPIProp )
{
    if ( eMAPIProp == NULL )
    {
        Guard::ThrowArgumentNullException( "eMAPIProp" );
    }
}

EMAPILib::MAPIPropImpl::~MAPIPropImpl()
{
}
ArrayList^ EMAPILib::MAPIPropImpl::GetBinArray( int tag )
{
    ESPropValueSPtr prop = _eMAPIProp->getSingleProp( tag );
    if ( !prop.IsNull() )
    {
        SBinaryArray binArray = prop->GetMVbin();
        ArrayList^ list = gcnew ArrayList( (int)binArray.cValues );
        for ( int i = 0; i < (int)binArray.cValues; ++i )
        {
            SBinary bin = binArray.lpbin[i];
            String ^str = Helper::EntryIDToHex( bin.lpb, bin.cb );
            list->Add( str );
        }
        return list;
    }
    return nullptr;
}

ArrayList ^EMAPILib::MAPIPropImpl::GetStringArray( int tag )
{
    ESPropValueSPtr prop = _eMAPIProp->getSingleProp( tag );
    if ( !prop.IsNull() )
    {
        SLPSTRArray strArray = prop->GetMVszA();
        ArrayList ^list = gcnew ArrayList( (int)strArray.cValues );
        for ( int i = 0; i < (int)strArray.cValues; ++i )
        {
            LPSTR str = strArray.lppszA[i];
            list->Add( gcnew String( str ) );
        }
        return list;
    }
    return nullptr;
}
void EMAPILib::MAPIPropImpl::SetStringArray( int tag, ArrayList ^value )
{
    CheckDisposed();
    if ( value != nullptr && value->Count > 0 )
    {
        int count = value->Count;
        ANSIStrings ansiStrings( count );
        for ( int i = 0; i < count; ++i )
        {
            String ^strValue = dynamic_cast<String^>( value[i] );
            ansiStrings.Set( i, Temp::GetLPSTR( strValue ) );
        }
        _eMAPIProp->setStringArray( tag, ansiStrings.GetLPSTRs(), count );
    }
    else
    {
        _eMAPIProp->deleteSimpleProp( tag );
    }
}

String ^EMAPILib::MAPIPropImpl::GetBinProp( int tag )
{
    CheckDisposed();
    return Helper::BinPropToString( _eMAPIProp->getSingleProp( tag ) );
}

void EMAPILib::MAPIPropImpl::DeleteProp( int tag )
{
    CheckDisposed();
    _eMAPIProp->deleteSimpleProp( tag );
}

void EMAPILib::MAPIPropImpl::CopyTo( LPCIID lpInterface, IEMAPIProp ^destMAPIObj )
{
    CheckDisposed();
    MAPIPropImpl ^destMAPIObjImpl = dynamic_cast<MAPIPropImpl^>(destMAPIObj);
    _eMAPIProp->CopyTo( lpInterface, destMAPIObjImpl->_eMAPIProp );
}

DateTime EMAPILib::MAPIPropImpl::GetDateTimeProp( int tag )
{
    CheckDisposed();
    try
    {
        ESPropValueSPtr prop = _eMAPIProp->getSingleProp( tag );
        if ( !prop.IsNull() )
        {
            _FILETIME ft = prop->GetFILETIME();
            ULONGLONG dt = (((ULONGLONG) ft.dwHighDateTime) << 32) + ft.dwLowDateTime;
            return DateTime::FromFileTime( dt );
        }
    }
    catch( System::Exception ^exc )
    {
        System::Diagnostics::Debug::WriteLine( exc->Message );
        System::Diagnostics::Debug::WriteLine( exc->StackTrace );
    }
    return DateTime::MinValue;
}
int EMAPILib::MAPIPropImpl::GetLongProp( int tag )
{
    CheckDisposed();
    return GetLongProp( tag, false );
}
int EMAPILib::MAPIPropImpl::GetLongProp( int tag, bool retError )
{
    CheckDisposed();
    ESPropValueSPtr prop = _eMAPIProp->getSingleProp( tag );
    if ( !prop.IsNull() )
    {
        return prop->GetLong();
    }
    if ( retError )
    {
        return -9999;
    }
    return 0;
}

bool EMAPILib::MAPIPropImpl::GetBoolProp( int tag )
{
    CheckDisposed();
    ESPropValueSPtr prop = _eMAPIProp->getSingleProp( tag );
    if ( !prop.IsNull() )
    {
        return prop->GetBool();
    }
    return false;
}

String ^EMAPILib::MAPIPropImpl::GetStringProp( int tag )
{
    CheckDisposed();
    ESPropValueSPtr prop = _eMAPIProp->getSingleProp( tag );
    if ( !prop.IsNull() && prop->GetLPSTR() != NULL )
    {
        return gcnew String( prop->GetLPSTR() );
    }
    return nullptr;
}

void EMAPILib::MAPIPropImpl::SetLongProp( int tag, int value )
{
    CheckDisposed();
    _eMAPIProp->setLongProp( tag, value );
}

void EMAPILib::MAPIPropImpl::SetStringProp( int tag, String ^value )
{
    CheckDisposed();
    if ( value != nullptr )
    {
        _eMAPIProp->setStringProp( tag, Temp::GetANSIString( value )->GetChars() );
    }
    else
    {
        _eMAPIProp->setStringProp( tag, "" );
    }
}

void EMAPILib::MAPIPropImpl::SetBoolProp( int tag, bool value )
{
    CheckDisposed();
    _eMAPIProp->setBoolProp( tag, value );
}
void EMAPILib::MAPIPropImpl::SetDateTimeProp( int tag, DateTime value )
{
    CheckDisposed();
    if ( value == DateTime::MinValue )
    {
        _eMAPIProp->deleteSimpleProp( tag );
        return;
    }
    _eMAPIProp->setDateTimeProp( tag, value.ToFileTime() );
}

void EMAPILib::MAPIPropImpl::WriteStringStreamProp( int tag, String ^propValue )
{
    CheckDisposed();
    int count = 0;
    if ( propValue != nullptr )
    {
        count = propValue->Length;
    }
    _eMAPIProp->writeStringStreamProp( tag, Temp::GetANSIString( propValue )->GetChars(), count );
}

void EMAPILib::MAPIPropImpl::SaveChanges()
{
    CheckDisposed();
    _eMAPIProp->SaveChanges( (int)KEEP_OPEN_READWRITE );
}

int EMAPILib::MAPIPropImpl::GetIDsFromNames( System::Guid% gcGUID, String ^name, int propType )
{
    CheckDisposed();
    GUID guid;
    Helper::SetGUID( &guid, gcGUID );
    return _eMAPIProp->getIDsFromNames( &guid, Temp::GetUNIString( name )->GetChars(), propType );
}

int EMAPILib::MAPIPropImpl::GetIDsFromNames( System::Guid% gcGUID, int lID, int propType )
{
    CheckDisposed();
    GUID guid;
    Helper::SetGUID( &guid, gcGUID );
    return _eMAPIProp->getIDsFromNames( &guid, lID, propType );
}
