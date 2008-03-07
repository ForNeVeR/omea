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
    public __gc class ABContainerImpl : public IEABContainer, public MAPIPropImpl
    {
    private:
        ABContainerSPtr* _abCont;
    public:
        ABContainerImpl( const ABContainerSPtr& abCont );
        virtual ~ABContainerImpl();
        virtual IETable* GetTable();
        virtual IERowSet* GetRowSet();
        virtual IEMailUser* OpenMailUser( String* entryID );
        virtual IEMailUser* CreateMailUser();
        virtual void Dispose();
    };

    public __gc class AddrBookImpl : public IEAddrBook, public MAPIPropImpl
    {
    private:
        AddrBookSPtr* _addrBook;
        ELPSRowSetSPtr* _rows;
    public:
        AddrBookImpl( const AddrBookSPtr& addrBook );
        virtual ~AddrBookImpl();
        virtual int GetCount();
        virtual IEABContainer* OpenAB( int index );
        virtual IEABContainer* OpenAB( String* entryId );
        virtual String* FindBinProp( int index, int tag );
        virtual IEMailUser* OpenMailUser( String* entryID );
        virtual void Dispose();
    };
    public __gc class MailUserImpl : public IEMailUser, public MAPIPropImpl
    {
    private:
        MailUserSPtr* _mailUser;
    public:
        MailUserImpl( const MailUserSPtr& mailUser );
        virtual ~MailUserImpl();
        virtual void Dispose();
    };
}
