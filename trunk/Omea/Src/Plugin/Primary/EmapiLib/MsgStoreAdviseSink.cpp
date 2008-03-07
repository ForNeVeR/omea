/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#include "msgstoreadvisesink.h"
#include "guard.h"

void MsgStoreAdviseSink::SetListener( EMAPILib::IMAPIListener* listener )
{
    _listener = listener;
}

void MsgStoreAdviseSink::OnNotifyImpl( ULONG cNotif, LPNOTIFICATION pNotifications )
{
    if ( _listener == NULL ) return;
    for ( int i = 0; i < (int)cNotif; i++ )
    {
        _NOTIFICATION ntf = pNotifications[i];
        switch ( ntf.ulEventType )
        {
            case (int)fnevCriticalError:
                OutputDebugString( "fnevCriticalError" );
                break;
            case (int)fnevNewMail:
                OutputDebugString( "fnevNewMail" );
                _listener->OnNewMail( Helper::GetNewMailNtf( ntf ) );
                break;

            case (int)fnevObjectCreated:
                if ( ntf.info.obj.ulObjType == (int)MAPI_FOLDER )
                {
                    _listener->OnFolderAdd( Helper::GetMAPINtf( ntf ) );
                }
                else if ( ntf.info.obj.ulObjType == (int)MAPI_MESSAGE )
                {
                    _listener->OnMailAdd( Helper::GetMAPINtf( ntf ) );
                }
                break;
            case (int)fnevObjectMoved:
                if ( ntf.info.obj.ulObjType == (int)MAPI_FOLDER )
                {
                    _listener->OnFolderMove( Helper::GetMAPIFullNtf( ntf ) );
                }
                else if ( ntf.info.obj.ulObjType == (int)MAPI_MESSAGE )
                {
                    _listener->OnMailMove( Helper::GetMAPIFullNtf( ntf ) );
                }
                break;
            case (int)fnevObjectDeleted:
                if ( ntf.info.obj.ulObjType == (int)MAPI_FOLDER )
                {
                    _listener->OnFolderDelete( Helper::GetMAPINtf( ntf ) );
                }
                else if ( ntf.info.obj.ulObjType == (int)MAPI_MESSAGE )
                {
                    _listener->OnMailDelete( Helper::GetMAPINtf( ntf ) );
                }
                break;
            case (int)fnevObjectModified:
                if ( ntf.info.obj.ulObjType == (int)MAPI_FOLDER )
                {
                    _listener->OnFolderModify( Helper::GetMAPIFullNtf( ntf ) );
                }
                else if ( ntf.info.obj.ulObjType == (int)MAPI_MESSAGE )
                {
                    _listener->OnMailModify( Helper::GetMAPIFullNtf( ntf ) );
                }
                break;
            case (int)fnevObjectCopied:
                if ( ntf.info.obj.ulObjType == (int)MAPI_FOLDER )
                {
                    _listener->OnFolderCopy( Helper::GetMAPIFullNtf( ntf ) );
                }
                else if ( ntf.info.obj.ulObjType == (int)MAPI_MESSAGE )
                {
                    _listener->OnMailCopy( Helper::GetMAPIFullNtf( ntf ) );
                }
                break;
            default:
                break;
        }
    }
}

#pragma unmanaged

STDMETHODIMP_(ULONG) MsgStoreAdviseSink::OnNotify( ULONG cNotif, LPNOTIFICATION pNotifications )
{
    if ( pNotifications != NULL )
    {
        try
        {
            OnNotifyImpl( cNotif, pNotifications );
        }
        catch (...)
        {
            OutputDebugString( "Exception was caught in MsgStoreAdviseSink::OnNotify" );
        }
    }
    return S_OK;
}

MsgStoreAdviseSink::MsgStoreAdviseSink( ) : _cRef( 1 )
{
    OutputDebugString( "MsgStoreAdviseSink" );
}

MsgStoreAdviseSink::~MsgStoreAdviseSink()
{
    OutputDebugString( "~MsgStoreAdviseSink" );
}

STDMETHODIMP MsgStoreAdviseSink::QueryInterface( REFIID riid, LPVOID *ppvObj )
{
    *ppvObj = 0;
    if ( riid == IID_IMAPIAdviseSink || riid == IID_IUnknown )
    {
        *ppvObj = (LPVOID)this;
        AddRef();
        return S_OK;
    }
    return E_NOINTERFACE;
};

STDMETHODIMP_(ULONG) MsgStoreAdviseSink::AddRef()
{
    InterlockedIncrement( &_cRef );
    return _cRef;
};

STDMETHODIMP_(ULONG) MsgStoreAdviseSink::Release()
{
    InterlockedDecrement( &_cRef );
    int ulRet = _cRef;
    if ( ulRet == 0 ) 
    {
        delete this;
    }
    return ulRet;
};

MsgStoreAdviseSink* MsgStoreAdviseSink::Create()
{
    return new MsgStoreAdviseSink();
};
