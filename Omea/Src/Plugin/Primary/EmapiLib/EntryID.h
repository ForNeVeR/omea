// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "rcobject.h"
#include "emapi.h"

class EntryID : public RCObject
{
private:
    LPENTRYID _lpEntryID;
    int _length;
public :
    EntryID( const LPBYTE bytes, int count );
    int GetLength() const;
    LPENTRYID getLPENTRYID() const;
    operator LPENTRYID() const;
    virtual ~EntryID();
private:
    EntryID( const EntryID& );
    EntryID& operator=( const EntryID& );
};
