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
    public ref class MAPIPropImpl : public Disposable, public IEMAPIProp
    {
    private:
        MAPIProp* _eMAPIProp;
    public:
        MAPIPropImpl( MAPIProp* eMAPIProp );
        virtual ~MAPIPropImpl();

        virtual String^ GetBinProp( int tag );
        virtual ArrayList^ GetBinArray( int tag );
        virtual ArrayList^ GetStringArray( int tag );
        virtual DateTime GetDateTimeProp( int tag );
        virtual int GetLongProp( int tag );
        virtual int GetLongProp( int tag, bool retError );
        virtual bool GetBoolProp( int tag );
        virtual String^ GetStringProp( int tag );
        virtual void SetStringArray( int tag, ArrayList^ value );

        virtual int GetIDsFromNames( System::Guid% gcGUID, String^ name, int propType );
        virtual int GetIDsFromNames( System::Guid% gcGUID, int lID, int propType );

        virtual void SetDateTimeProp( int tag, DateTime value );
        virtual void SetStringProp( int tag, String^ value );
        virtual void SetLongProp( int tag, int value );
        virtual void SetBoolProp( int tag, bool value );
        virtual void WriteStringStreamProp( int tag, String^ propValue );
        virtual void SaveChanges();
        virtual void DeleteProp( int tag );
    protected:
        virtual void CopyTo( LPCIID lpInterface, IEMAPIProp^ destMAPIObj );
    };
}
