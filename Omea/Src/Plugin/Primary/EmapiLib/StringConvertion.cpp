/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma unmanaged 

#include "stringconvertion.h"
#include "guard.h"
#include "RCPtrDef.h"
template RCPtr<ANSIString>;
template RCPtr<UNIString>;

ANSIString::ANSIString() : _lpStr( NULL )
{
}

ANSIString::ANSIString( LPSTR str ) : _lpStr( NULL )
{
    operator=( str );
}

ANSIString::~ANSIString()
{
    try
    {
        Release();
    }
    catch(...){}
}

void ANSIString::Release()
{
    if ( _lpStr != NULL )
    {
        Guard::FreeCoTaskMem( _lpStr );
        _lpStr = NULL;
    }
}

LPSTR ANSIString::GetChars() const
{
    return _lpStr;
}

ANSIString& ANSIString::operator=( LPSTR str )
{
    Release();
    _lpStr = str;
    return *this;
}

ANSIStrings::ANSIStrings( int count )
{
    _ansiStrings = new ANSIString[count];
    _lpstrs = TypeFactory::CreateLPSTRArray( count );
}
ANSIStrings::~ANSIStrings()
{
    try
    {
        delete[] _ansiStrings;
    }
    catch(...){}
    try
    {
        delete[] _lpstrs;
    }
    catch(...){}
}
LPSTR* ANSIStrings::GetLPSTRs() const
{
    return _lpstrs;
}
LPSTR ANSIStrings::GetChars( int index ) const
{
    return _ansiStrings[index].GetChars();
}
void ANSIStrings::Set( int index, LPSTR str ) const
{
    _ansiStrings[index].operator =( str );
    _lpstrs[index] = _ansiStrings[index].GetChars();
}
UNIString::UNIString() : _lpStr( NULL )
{
}

UNIString::UNIString( LPWSTR str ) : _lpStr( NULL )
{
    operator=( str );
}

UNIString::~UNIString()
{
    try
    {
        Release();
    }
    catch(...){}
}

void UNIString::Release()
{
    if ( _lpStr != NULL )
    {
        Guard::FreeCoTaskMem( _lpStr );
        _lpStr = NULL;
    }
}

LPWSTR UNIString::GetChars() const
{
    return _lpStr;
}

UNIString& UNIString::operator=( LPWSTR  str )
{
    Release();
    _lpStr = str;
    return *this;
}


