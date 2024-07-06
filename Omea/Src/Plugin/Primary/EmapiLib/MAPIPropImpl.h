// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once
#include "helpers.h"
#using <mscorlib.dll>
using namespace System;

class MAPIProp;

namespace EMAPILib
{
    public __gc class MAPIPropImpl : public Disposable, public IEMAPIProp
    {
    private:
        MAPIProp* _eMAPIProp;
    public:
        MAPIPropImpl( MAPIProp* eMAPIProp );
        virtual ~MAPIPropImpl();

        String* GetBinProp( int tag );
        ArrayList* GetBinArray( int tag );
        ArrayList* GetStringArray( int tag );
        DateTime GetDateTimeProp( int tag );
        int GetLongProp( int tag );
        int GetLongProp( int tag, bool retError );
        bool GetBoolProp( int tag );
        String* GetStringProp( int tag );
        void SetStringArray( int tag, ArrayList* value );

        int GetIDsFromNames( System::Guid* gcGUID, String* name, int propType );
        int GetIDsFromNames( System::Guid* gcGUID, int lID, int propType );

        void SetDateTimeProp( int tag, DateTime value );
        void SetStringProp( int tag, String* value );
        void SetLongProp( int tag, int value );
        void SetBoolProp( int tag, bool value );
        void WriteStringStreamProp( int tag, String* propValue );
        void SaveChanges();
        void DeleteProp( int tag );
    protected:
        void CopyTo( LPCIID lpInterface, IEMAPIProp* destMAPIObj );
    };
}
