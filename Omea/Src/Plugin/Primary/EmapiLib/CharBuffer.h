// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "rcobject.h"
#include "emapi.h"

__nogc class CharBuffer : public RCObject
{
private:
    char* _chars;
    int _length;
public :
    CharBuffer( int length );
    const char* GetRawChars() const;
    void SetLength( int  length );
    int Length() const;
    virtual ~CharBuffer( );
    LPTSTR Get() const;
    void strcopy( LPCSTR source );
private:
    CharBuffer( const CharBuffer& );
    CharBuffer& operator=( const CharBuffer& );
};

