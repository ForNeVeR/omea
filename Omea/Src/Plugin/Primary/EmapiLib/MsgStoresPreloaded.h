/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma once

#include "typefactory.h"

__nogc class MsgStores : public RCObject
{
private:
    enum { EID, NAME, STORE, NUM_COLS };
    LPMAPISESSION _pSession;
	LPMAPITABLE _pStoresTbl;
    ELPSRowSetSPtr _pRowSet;
    int _storeCount;
public:
    MsgStores( LPMAPISESSION pSession );
    virtual ~MsgStores();
    bool PrepareMsgStoresTable();
    bool QueryAllRows();
    int GetCount() const;
    MsgStoreSPtr OpenStorage( int index ) const;
    LPSPropValue GetStorageID( int index ) const;
    LPSTR GetDisplayName( int index ) const;
    bool IsDefaultStore( int index ) const;
    static MsgStoresSPtr Get( LPMAPISESSION pSession );
private:
    void FreeAllRows();
};
