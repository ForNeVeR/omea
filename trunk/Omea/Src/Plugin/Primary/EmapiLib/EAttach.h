/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma once

#include "typefactory.h"
#include "mapiprop.h"

class EAttach : public MAPIProp
{
private:
    LPATTACH _lpAttach;
public:
    EAttach( LPATTACH lpAttach );
    virtual ~EAttach();
    CharBufferSPtr ReadToEnd() const;
    LPMESSAGE OpenMessage() const;
    void InsertOLEIntoRTF( HWND hwnd, int pos ) const;
};
