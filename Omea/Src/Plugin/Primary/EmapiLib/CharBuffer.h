/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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

