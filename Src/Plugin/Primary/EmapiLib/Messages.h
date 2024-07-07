// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "typefactory.h"

class Messages : public RCObject
{
private:
    LPMAPIFOLDER _lpFolder;
    LPMAPITABLE _pTable;
    int _count;
    LPSRowSet _pRows;
public:
    Messages( LPMAPIFOLDER lpFolder );
    virtual ~Messages();
    EMessageSPtr GetMessage( int index ) const;
    int GetCount() const;
};
