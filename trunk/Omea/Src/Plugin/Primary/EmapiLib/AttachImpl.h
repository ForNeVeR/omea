/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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