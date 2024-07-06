// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#ifndef _OMNIAMEA_RCOBJECT_H
#define _OMNIAMEA_RCOBJECT_H

#include "RCPtr.h"

class RCObject : public MyHeapObject
{
public:
    RCObject();
    RCObject( const RCObject& rhs );
    RCObject& operator=( const RCObject& rhs );
    virtual ~RCObject();

    void addRef();
    int removeRef();
    int GetRefCount() const;
private:
    int refCount;
};
#endif
