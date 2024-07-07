// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once
#include "helpers.h"
#include "typefactory.h"
#include "MAPIPropImpl.h"

namespace EMAPILib
{
    public __gc class AttachImpl : public EMAPILib::IEAttach, public MAPIPropImpl
    {
    private:
        EAttachSPtr* _eAttach;
    public:
        AttachImpl( const EAttachSPtr& eAttach );
        virtual System::Byte ReadToEnd()[];
        virtual void InsertOLEIntoRTF( int hwnd, int pos );
        virtual ~AttachImpl();
        virtual IEMessage* OpenMessage();
        virtual void Dispose();
    };
}
