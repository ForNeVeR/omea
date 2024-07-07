// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once
#include "helpers.h"
#include "typefactory.h"
#include "MAPIPropImpl.h"

namespace EMAPILib
{
    public ref class ABContainerImpl : public IEABContainer, public MAPIPropImpl
    {
    private:
        ABContainerSPtr* _abCont;
    public:
        ABContainerImpl( const ABContainerSPtr& abCont );
        virtual IETable^ GetTable();
        virtual IERowSet^ GetRowSet();
        virtual IEMailUser^ OpenMailUser( String^ entryID );
        virtual IEMailUser^ CreateMailUser();
        virtual ~ABContainerImpl();
    };

    public ref class AddrBookImpl : public IEAddrBook, public MAPIPropImpl
    {
    private:
        AddrBookSPtr* _addrBook;
        ELPSRowSetSPtr* _rows;
    public:
        AddrBookImpl( const AddrBookSPtr& addrBook );
        virtual int GetCount();
        virtual IEABContainer^ OpenAB( int index );
        virtual IEABContainer^ OpenAB( String^ entryId );
        virtual String^ FindBinProp( int index, int tag );
        virtual IEMailUser^ OpenMailUser( String^ entryID );
        virtual ~AddrBookImpl();
    };
    public ref class MailUserImpl : public IEMailUser, public MAPIPropImpl
    {
    private:
        MailUserSPtr* _mailUser;
    public:
        MailUserImpl( const MailUserSPtr& mailUser );
        virtual ~MailUserImpl();
    };
}
