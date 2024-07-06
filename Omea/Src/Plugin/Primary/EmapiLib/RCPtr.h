// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#ifndef _OMNIAMEA_RCPTR_H
#define _OMNIAMEA_RCPTR_H

#include "emapi.h"

__nogc class RCPtrBase : public MyHeapObject
{
protected:
    RCPtrBase(){}
public:
    virtual ~RCPtrBase() = 0{}
};

template<typename T>
class RCPtr : public RCPtrBase
{
public:
    RCPtr( T* realPtr = NULL );
    RCPtr( const RCPtr<T>& rhs );
    virtual ~RCPtr();
    RCPtr<T>& operator=( const RCPtr<T>& rhs );
    T* operator->() const;
    T* get() const;
    T& operator*() const;
    bool IsNull() const;
    void release();
    int GetRefCount() const;
    RCPtr<T>* CloneOnHeap() const;
private:
    T* pointee;
    void init();
};


#endif
