// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#include "addrbookimpl.h"
#include "etableimpl.h"
#include "rowsetimpl.h"
#using <mscorlib.dll>
#include "addrbook.h"
#include "mailUser.h"
#include "ETable.h"
#include "Guard.h"
#include "entryid.h"
#include "ESPropValue.h"

EMAPILib::ABContainerImpl::ABContainerImpl( const ABContainerSPtr& abCont ) : MAPIPropImpl( abCont.get() )
{
    if ( abCont.IsNull() )
    {
        Guard::ThrowArgumentNullException( "abCont" );
    }
    _abCont = abCont.CloneOnHeap();
}

EMAPILib::ABContainerImpl::~ABContainerImpl()
{
    MAPIPropImpl::DisposeImpl();
    TypeFactory::Delete( _abCont );
    _abCont = NULL;
}
EMAPILib::IERowSet^ EMAPILib::ABContainerImpl::GetRowSet()
{
    CheckDisposed();
    ELPSRowSetSPtr rowset = (*_abCont)->GetRowSet();
    if ( !rowset.IsNull() )
    {
        return gcnew RowSetImpl( rowset );
    }
    return nullptr;
}
EMAPILib::IETable^ EMAPILib::ABContainerImpl::GetTable()
{
    CheckDisposed();
    ETableSPtr table = (*_abCont)->GetTable();
    if ( !table.IsNull() )
    {
        return gcnew ETableImpl( table );
    }
    return nullptr;
}
EMAPILib::IEMailUser^ EMAPILib::ABContainerImpl::CreateMailUser()
{
    CheckDisposed();

    MailUserSPtr mailUser = (*_abCont)->CreateEntry();
    if ( mailUser.IsNull() )
    {
        return nullptr;
    }

    return gcnew MailUserImpl( mailUser );
}

EMAPILib::IEMailUser^ EMAPILib::ABContainerImpl::OpenMailUser( String^ entryID )
{
    CheckDisposed();
    if ( entryID == nullptr )
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }

    EntryIDSPtr entryId = Helper::HexToEntryID( entryID );
    MailUserSPtr mailUser = (*_abCont)->OpenEntry( (LPBYTE)entryId->getLPENTRYID(), entryId->GetLength() );
    if ( mailUser.IsNull() )
    {
        return nullptr;
    }

    return gcnew MailUserImpl( mailUser );
}

EMAPILib::AddrBookImpl::AddrBookImpl( const AddrBookSPtr& addrBook ) : MAPIPropImpl( addrBook.get() )
{
    if ( addrBook.IsNull() )
    {
        Guard::ThrowArgumentNullException( "addrBook" );
    }
    _addrBook = addrBook.CloneOnHeap();
    ELPSRowSetSPtr rows = (*_addrBook)->GetSearchPath();
    _rows = rows.CloneOnHeap();
}

EMAPILib::AddrBookImpl::~AddrBookImpl()
{
    MAPIPropImpl::DisposeImpl();
    TypeFactory::Delete( _addrBook );
    _addrBook = NULL;
    TypeFactory::Delete( _rows );
    _rows = NULL;
}
int EMAPILib::AddrBookImpl::GetCount()
{
    CheckDisposed();
    if ( !(*_rows).IsNull() )
    {
        return (*_rows)->GetCount();
    }
    return 0;
}
EMAPILib::IEABContainer^ EMAPILib::AddrBookImpl::OpenAB( int index )
{
    CheckDisposed();
    if ( !(*_rows).IsNull() )
    {
        ESPropValueSPtr prop = (*_rows)->FindProp( (int)PR_ENTRYID, index );
        if ( prop.IsNull() ) return nullptr;
        ABContainerSPtr ABCont = (*_addrBook)->OpenEntry( prop->GetBinLPBYTE(), prop->GetBinCB() );
        if ( ABCont.IsNull() ) return nullptr;
        return gcnew ABContainerImpl( ABCont );
    }
    return nullptr;
}
String^ EMAPILib::AddrBookImpl::FindBinProp( int index, int tag )
{
    CheckDisposed();
    if ( !(*_rows).IsNull() )
    {
        ESPropValueSPtr prop = (*_rows)->FindProp( tag, index );
        if ( prop.IsNull() ) return nullptr;
        return Helper::BinPropToString( prop );
    }
    return nullptr;
}
EMAPILib::IEABContainer^ EMAPILib::AddrBookImpl::OpenAB( String^ entryId )
{
    CheckDisposed();
    if ( entryId == nullptr || entryId->Length == 0 )
    {
        Guard::ThrowArgumentNullException( "entryId" );
    }
    EntryIDSPtr entryIDSPtr = Helper::HexToEntryID( entryId );
    if ( !entryIDSPtr.IsNull() )
    {
        ABContainerSPtr ABCont = (*_addrBook)->OpenEntry( (LPBYTE)entryIDSPtr->getLPENTRYID(), entryIDSPtr->GetLength() );
        if ( !ABCont.IsNull() )
        {
            return gcnew ABContainerImpl( ABCont );
        }
    }
    return nullptr;
}

EMAPILib::IEMailUser^ EMAPILib::AddrBookImpl::OpenMailUser( String^ entryID )
{
    CheckDisposed();
    if ( entryID == nullptr )
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }

    EntryIDSPtr entryId = Helper::HexToEntryID( entryID );
    MailUserSPtr mailUser = (*_addrBook)->OpenMailUser( (LPBYTE)entryId->getLPENTRYID(), entryId->GetLength() );
    if ( mailUser.IsNull() )
    {
        return nullptr;
    }

    return gcnew MailUserImpl( mailUser );
}
EMAPILib::MailUserImpl::MailUserImpl( const MailUserSPtr& mailUser ) : MAPIPropImpl( mailUser.get() )
{
    if ( mailUser.IsNull() )
    {
        Guard::ThrowArgumentNullException( "mailUser" );
    }
    _mailUser = mailUser.CloneOnHeap();
}

EMAPILib::MailUserImpl::~MailUserImpl()
{
    MAPIPropImpl::DisposeImpl();
    TypeFactory::Delete( _mailUser );
    _mailUser = NULL;
}
