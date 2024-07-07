// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "typefactory.h"

class ELPSRowSet : public RCObject
{
private:
    LPSRowSet _lpSRowSet;
public:
    ELPSRowSet( LPSRowSet lpSRowSet );
    virtual ~ELPSRowSet();
    int GetCount() const;
    LPSRowSet GetRaw() const;
    ESPropValueSPtr FindProp( int tag, int rowNum ) const;
    ESPropValueSPtr FindProp( int tag ) const;
    LPSPropValue GetProp( int index ) const;
    LPSPropValue GetProp( int index, int rowNum ) const;
};
class ETable : public RCObject
{
private:
    LPMAPITABLE _lpMAPITable;
public:
    ETable( LPMAPITABLE lpMAPITable );
    virtual ~ETable();
    HRESULT Sort( int tag, bool Asc ) const;
    int GetRowCount() const;
    ELPSRowSetSPtr GetNextRow() const;
    ELPSRowSetSPtr GetNextRows( int count ) const;
    HRESULT SetColumns( LPSPropTagArray tagArray ) const;
    HRESULT SetRestriction( LPSRestriction restriction ) const;
};
