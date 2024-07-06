// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
