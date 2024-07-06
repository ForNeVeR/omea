// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "rcobject.h"

#ifdef EMAPI_MANAGED
#pragma managed
#endif

RCObject::RCObject() : refCount( 0 )
{
}

RCObject::RCObject( const RCObject& ) : refCount( 0 )
{
}

RCObject& RCObject::operator=( const RCObject& )
{
    return *this;
}
RCObject::~RCObject()
{
}

void RCObject::addRef()
{
    ::InterlockedIncrement( (PLONG)&refCount );
}

int RCObject::removeRef()
{
    int rCount = ::InterlockedDecrement( (PLONG)&refCount );
    if ( rCount == 0 )
    {
        delete this;
    }
    return rCount;
}
int RCObject::GetRefCount() const
{
    return refCount;
}
