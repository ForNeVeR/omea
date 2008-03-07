/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
