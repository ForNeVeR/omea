/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
