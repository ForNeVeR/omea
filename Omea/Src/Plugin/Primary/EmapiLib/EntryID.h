/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
