/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
