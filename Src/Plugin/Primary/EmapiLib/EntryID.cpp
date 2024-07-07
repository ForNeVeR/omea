// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "entryid.h"
#include "RCPtrDef.h"

template RCPtr<EntryID>;

EntryID::EntryID( const LPBYTE bytes, int count )
{
    _lpEntryID = (LPENTRYID)bytes;
    _length = count;
}
int EntryID::GetLength() const
{
    return _length;
}

LPENTRYID EntryID::getLPENTRYID() const
{
    return _lpEntryID;
}

EntryID::operator LPENTRYID() const
{
    return _lpEntryID;
}
EntryID::~EntryID()
{
    try
    {
        MAPIFreeBuffer( _lpEntryID );
    }
    catch(...){}
}
