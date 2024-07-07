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
}
void EMAPILib::ABContainerImpl::Dispose()
{
    MAPIPropImpl::DisposeImpl();
    TypeFactory::Delete( _abCont );
    _abCont = NULL;
}
EMAPILib::IERowSet* EMAPILib::ABContainerImpl::GetRowSet()
{
    CheckDisposed();
    ELPSRowSetSPtr rowset = (*_abCont)->GetRowSet();
    if ( !rowset.IsNull() )
    {
        return new RowSetImpl( rowset );
    }
    return NULL;
}
EMAPILib::IETable* EMAPILib::ABContainerImpl::GetTable()
{
    CheckDisposed();
    ETableSPtr table = (*_abCont)->GetTable();
    if ( !table.IsNull() )
    {
        return new ETableImpl( table );
    }
    return NULL;
}
EMAPILib::IEMailUser* EMAPILib::ABContainerImpl::CreateMailUser()
{
    CheckDisposed();

    MailUserSPtr mailUser = (*_abCont)->CreateEntry();
    if ( mailUser.IsNull() )
    {
        return NULL;
    }

    return new MailUserImpl( mailUser );
}

EMAPILib::IEMailUser* EMAPILib::ABContainerImpl::OpenMailUser( String* entryID )
{
    CheckDisposed();
    if ( entryID == NULL )
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }

    EntryIDSPtr entryId = Helper::HexToEntryID( entryID );
    MailUserSPtr mailUser = (*_abCont)->OpenEntry( (LPBYTE)entryId->getLPENTRYID(), entryId->GetLength() );
    if ( mailUser.IsNull() )
    {
        return NULL;
    }

    return new MailUserImpl( mailUser );
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
}
void EMAPILib::AddrBookImpl::Dispose()
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
EMAPILib::IEABContainer* EMAPILib::AddrBookImpl::OpenAB( int index )
{
    CheckDisposed();
    if ( !(*_rows).IsNull() )
    {
        ESPropValueSPtr prop = (*_rows)->FindProp( (int)PR_ENTRYID, index );
        if ( prop.IsNull() ) return NULL;
        ABContainerSPtr ABCont = (*_addrBook)->OpenEntry( prop->GetBinLPBYTE(), prop->GetBinCB() );
        if ( ABCont.IsNull() ) return NULL;
        return new ABContainerImpl( ABCont );
    }
    return NULL;
}
String* EMAPILib::AddrBookImpl::FindBinProp( int index, int tag )
{
    CheckDisposed();
    if ( !(*_rows).IsNull() )
    {
        ESPropValueSPtr prop = (*_rows)->FindProp( tag, index );
        if ( prop.IsNull() ) return NULL;
        return Helper::BinPropToString( prop );
    }
    return NULL;
}
EMAPILib::IEABContainer* EMAPILib::AddrBookImpl::OpenAB( String* entryId )
{
    CheckDisposed();
    if ( entryId == NULL || entryId->Length == 0 )
    {
        Guard::ThrowArgumentNullException( "entryId" );
    }
    EntryIDSPtr entryIDSPtr = Helper::HexToEntryID( entryId );
    if ( !entryIDSPtr.IsNull() )
    {
        ABContainerSPtr ABCont = (*_addrBook)->OpenEntry( (LPBYTE)entryIDSPtr->getLPENTRYID(), entryIDSPtr->GetLength() );
        if ( !ABCont.IsNull() )
        {
            return new ABContainerImpl( ABCont );
        }
    }
    return NULL;
}

EMAPILib::IEMailUser* EMAPILib::AddrBookImpl::OpenMailUser( String* entryID )
{
    CheckDisposed();
    if ( entryID == NULL )
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }

    EntryIDSPtr entryId = Helper::HexToEntryID( entryID );
    MailUserSPtr mailUser = (*_addrBook)->OpenMailUser( (LPBYTE)entryId->getLPENTRYID(), entryId->GetLength() );
    if ( mailUser.IsNull() )
    {
        return NULL;
    }

    return new MailUserImpl( mailUser );
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
}
void EMAPILib::MailUserImpl::Dispose()
{
    MAPIPropImpl::DisposeImpl();
    TypeFactory::Delete( _mailUser );
    _mailUser = NULL;
}
