/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
