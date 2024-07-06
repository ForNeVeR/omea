// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "CharsStorage.h"
#include "typefactory.h"
#include "CharBuffer.h"
#include "RCPtrDef.h"
template RCPtr<CharsStorage>;

CharsStorage::CharsStorage() : _count( 0 )
{
}

CharsStorage::~CharsStorage()
{
}

void CharsStorage::Add( const CharBufferSPtr& buffer )
{
    _count += buffer->Length();
    _buffers.push_back( buffer );
}
const char* CharsStorage::GetBuffer( int index ) const
{
    return _buffers[index]->GetRawChars();
}

int CharsStorage::Count() const
{
    return _count + 1;
}
CharBufferSPtr CharsStorage::Concatenate() const
{
    int count = _count + 1;
    CharBufferSPtr buffer = TypeFactory::CreateCharBuffer( count );
    char* buf = buffer->Get();
    int offset = 0;
    for ( unsigned int i = 0; i < _buffers.size(); i++ )
    {
        int size = _buffers[i]->Length();
        if ( size > 0 )
        {
            memcpy( buf + offset, _buffers[i]->GetRawChars(), size );
            offset += size;
        }
    }
    buf[count-1] = 0;
    return buffer;
}
