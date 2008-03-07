/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
