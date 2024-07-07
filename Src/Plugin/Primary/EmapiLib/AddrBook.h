// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once
#include "typefactory.h"
#include "mapiprop.h"

__nogc class ABContainer : public MAPIProp
{
private:
    IABContainer* _lpABContainer;
public:
    ABContainer( IABContainer* lpABContainer );
    virtual ~ABContainer();
    IABContainer* GetRaw() const;
    ETableSPtr GetTable() const;
    MailUserSPtr OpenEntry( LPBYTE entryID, int cb ) const;
    MailUserSPtr ABContainer::CreateEntry() const;
    ELPSRowSetSPtr GetRowSet() const;
};

__nogc class AddrBook : public MAPIProp
{
private:
    LPADRBOOK _lpAdrBook;
public:
    AddrBook( LPADRBOOK lpAdrBook );
    virtual ~AddrBook();
    ELPSRowSetSPtr AddrBook::GetSearchPath() const;
    ABContainerSPtr OpenEntry( LPBYTE entryID, int cb ) const;
    MailUserSPtr OpenMailUser( LPBYTE entryID, int cb ) const;
    static AddrBookSPtr OpenAddressBook( LPMAPISESSION lpSession );
};

