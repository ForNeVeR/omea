// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "emapi.h"
#include "typefactory.h"

class ANSIString : public RCObject
{
private:
    LPSTR _lpStr;
public:
    ANSIString();
    ANSIString( LPSTR str );
    virtual ~ANSIString();
    LPSTR GetChars() const;
    ANSIString& operator=( LPSTR str );
private:
    ANSIString( const ANSIString& );
    ANSIString& operator=( const ANSIString& );
    void Release();
};

class ANSIStrings : public MyHeapObject
{
private:
    ANSIString* _ansiStrings;
    LPSTR* _lpstrs;
public:
    ANSIStrings( int count );
    virtual ~ANSIStrings();
    LPSTR* GetLPSTRs() const;
    LPSTR GetChars( int index ) const;
    void Set( int index, LPSTR str ) const;
private:
    ANSIStrings( const ANSIStrings& );
    ANSIStrings& operator=( const ANSIStrings& );
};

class UNIString : public RCObject
{
private:
    LPWSTR _lpStr;
public:
    UNIString();
    UNIString( LPWSTR str );
    ~UNIString();
    LPWSTR GetChars() const;
    UNIString& operator=( LPWSTR  str );
private:
    UNIString( const UNIString& );
    UNIString& operator=( const UNIString& );
    void Release();
};
