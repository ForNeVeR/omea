// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "charbuffer.h"
#include "RCPtrDef.h"

template RCPtr<CharBuffer>;

CharBuffer::CharBuffer( int length )
{
    _chars = (char*)MyHeapObject::operator new( sizeof(char)*length );
    _length = length;
}
const char* CharBuffer::GetRawChars() const
{
    return _chars;
}
void CharBuffer::SetLength( int  length )
{
    _length = length;
}
int CharBuffer::Length() const
{
    return _length;
}
CharBuffer::~CharBuffer( )
{
    try
    {
        MyHeapObject::operator delete( _chars );
    }
    catch (...)
    {}
}
LPTSTR CharBuffer::Get() const
{
    return _chars;
}
void CharBuffer::strcopy( LPCSTR source )
{
    strcpy( _chars, source );
}
