// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once
#include <vector>
#include "typefactory.h"

class CharsStorage : public RCObject
{
private:
    int _count;
    std::vector<CharBufferSPtr> _buffers;
public:
    CharsStorage();
    virtual ~CharsStorage();
    void Add( const CharBufferSPtr& buffer );
    int Count() const;
    CharBufferSPtr Concatenate() const;
    const char* GetBuffer( int index ) const;
};
