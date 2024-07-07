// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// MyMAPIFormViewer.cpp: implementation of the CMyMAPIFormViewer class.
//
//////////////////////////////////////////////////////////////////////
#pragma unmanaged

#include "typefactory.h"
#include "FormViewer.h"
#include <exchform.h>
#include "EMAPIFolder.h"
#include "EntryID.h"
#include "ESPropValue.h"
#include "EMessage.h"
#include "MsgStore.h"
#include "FormManager.h"
#include "guard.h"

ViewContext::ViewContext( FormViewer* formViewer )
{
    OutputDebugString("ViewContext::ViewContext\n");
    _formViewer = formViewer;
    _cRef = 0;
}
ViewContext::~ViewContext()
{
    OutputDebugString("ViewContext::~ViewContext\n");
}
STDMETHODIMP ViewContext::GetLastError(HRESULT /*hResult*/, ULONG /*ulFlags*/, LPMAPIERROR FAR * /*lppMAPIError*/)
{
    OutputDebugString("ViewContext::GetLastError\n");
    return S_OK;
}

STDMETHODIMP_(ULONG) ViewContext::AddRef()
{
    OutputDebugString("ViewContext::AddRef\n");
    InterlockedIncrement( &_cRef );
    return _cRef;
}

STDMETHODIMP_(ULONG) ViewContext::Release()
{
    OutputDebugString("ViewContext::Release\n");
    LONG lCount = InterlockedDecrement( &_cRef );
    if ( !lCount )  delete this;
    return lCount;
}
STDMETHODIMP ViewContext::QueryInterface ( REFIID riid, LPVOID * ppvObj )
{
    *ppvObj = 0;
    if (riid == IID_IMAPIViewContext )
    {
        OutputDebugString("ViewContext::QueryInterface IMAPIViewContext\n");
        *ppvObj = (IMAPIViewContext *)this;
        AddRef();
        return S_OK;
    }
    else
    {
        return _formViewer->QueryInterface( riid, ppvObj );
    }
}


///////////////////////////////////////////////////////////////////////////////
// IMAPIViewContext implementation
///////////////////////////////////////////////////////////////////////////////

STDMETHODIMP ViewContext::SetAdviseSink( LPMAPIFORMADVISESINK pmvns )
{
    OutputDebugString("ViewContext::SetAdviseSink\n");
    if ( pmvns != NULL )
    {
        //UlRelease( pmvns );
    }
    return S_OK;
/*
    HRESULT hRes = S_OK;
    //UlRelease(m_lpMapiFormAdviseSink);
    if ( pmvns )
    {
        UlRelease( m_lpMapiFormAdviseSink );
        m_lpMapiFormAdviseSink = pmvns;
        //m_lpMapiFormAdviseSink->AddRef();
    }
    else
    {
        m_lpMapiFormAdviseSink = NULL;
    }
    return hRes;
    */
}

STDMETHODIMP ViewContext::ActivateNext(ULONG /*ulDir*/,
                                      LPCRECT /*prcPosRect*/)
{
    OutputDebugString("ViewContext::ActivateNext\n");
    return S_OK;
}

STDMETHODIMP ViewContext::GetPrintSetup(ULONG /*ulFlags*/,
                                       LPFORMPRINTSETUP FAR * /*lppFormPrintSetup*/)
{
    OutputDebugString("ViewContext::GetPrintSetup\n");
    return S_OK;
}

STDMETHODIMP ViewContext::GetSaveStream(ULONG FAR * /*pulFlags*/,
                                       ULONG FAR * /*pulFormat*/,
                                       LPSTREAM FAR * /*ppstm*/)
{
    OutputDebugString("ViewContext::GetSaveStream\n");
    return S_OK;
}

STDMETHODIMP ViewContext::GetViewStatus( LPULONG lpulStatus )
{
    OutputDebugString("ViewContext::GetViewStatus\n");
    *lpulStatus = 0;
    *lpulStatus |= VCSTATUS_INTERACTIVE;
    return S_OK;
}

///////////////////////////////////////////////////////////////////////////////
// End IMAPIViewContext implementation
///////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////
// Construction/Destruction
//////////////////////////////////////////////////////////////////////

FormViewer::FormViewer( MsgStoreSPtr msgStore, LPMAPISESSION lpMAPISession, EMAPIFolderSPtr folder,
                       EMessageSPtr message, int verbID ) :
        _msgStore( msgStore ), _folder( folder ), _message( message ), _persistMessage( NULL ), _form( NULL )
{
    _viewContext = new ViewContext( this );
    _viewContext->AddRef();
    _formID = Guard::RegisterForm();
    _listen = false;
    _verbID = verbID;
    OutputDebugString("CMyMAPIFormViewer::CMyMAPIFormViewer\n");

    m_lpMAPISession = lpMAPISession;
    m_lpMAPISession->AddRef();

    m_lpMapiFormAdviseSink = NULL;
    m_pulConnection = 0;
    m_cRef = 1;
}

FormViewer::~FormViewer()
{
    try
    {
        OutputDebugString("CMyMAPIFormViewer::~CMyMAPIFormViewer\n");
        UlRelease( m_lpMAPISession );
        ULONG viewContextRefCount = _viewContext->Release();
        if ( viewContextRefCount != 0 )
        {
            OutputDebugString("ERROR: viewContextRefCount > 0\n");
        }
        OutputDebugString("CMyMAPIFormViewer::~CMyMAPIFormViewer2\n");
    }
    catch (...){}
}

STDMETHODIMP FormViewer::QueryInterface ( REFIID riid, LPVOID * ppvObj )
{
    *ppvObj = 0;
    if ( riid == IID_IMAPIMessageSite )
    {
        OutputDebugString("CMyMAPIFormViewer::QueryInterface IMAPIMessageSite\n");
        *ppvObj = (IMAPIMessageSite *)this;
        AddRef();
        return S_OK;
    }
    if ( riid == IID_IMAPIViewContext )
    {
        OutputDebugString("CMyMAPIFormViewer::QueryInterface IMAPIViewContext\n");
        *ppvObj = (IMAPIViewContext *)_viewContext;
        _viewContext->AddRef();
        return S_OK;
    }
    if ( riid == IID_IMAPIViewAdviseSink )
    {
        OutputDebugString("CMyMAPIFormViewer::QueryInterface IMAPIViewAdviseSink\n");
        *ppvObj = (IMAPIViewAdviseSink *)this;
        AddRef();
        return S_OK;
    }
    if ( riid == IID_IUnknown )
    {
        OutputDebugString("CMyMAPIFormViewer::QueryInterface IMAPIMessageSite\n");
        *ppvObj = (LPUNKNOWN)((IMAPIMessageSite *)this);
        AddRef();
        return S_OK;
    }
    OutputDebugString("CMyMAPIFormViewer::QueryInterface E_NOINTERFACE\n");

    return E_NOINTERFACE;
}

STDMETHODIMP_(ULONG) FormViewer::AddRef()
{
    OutputDebugString("CMyMAPIFormViewer::AddRef\n");
    InterlockedIncrement(&m_cRef);
    return m_cRef;
}

STDMETHODIMP_(ULONG) FormViewer::Release()
{
    OutputDebugString("CMyMAPIFormViewer::Release\n");
    LONG lCount = InterlockedDecrement(&m_cRef);
    if (!lCount)
    {
        delete this;
        /*
        _msgStore.release();
        _folder.release();
        _message.release();
        _persistMessage.release();
        _form.release();
        */
        OutputDebugString("CMyMAPIFormViewer::Destructor should be\n");
    }
    return lCount;
}

///////////////////////////////////////////////////////////////////////////////
// IMAPIMessageSite implementation
///////////////////////////////////////////////////////////////////////////////
STDMETHODIMP FormViewer::GetSession (LPMAPISESSION FAR * ppSession)
{
    OutputDebugString("CMyMAPIFormViewer::GetSession\n");
    *ppSession = m_lpMAPISession;
    (*ppSession)->AddRef();
    return S_OK;
}

STDMETHODIMP FormViewer::GetStore ( LPMDB FAR * ppStore )
{
    OutputDebugString("CMyMAPIFormViewer::GetStore\n");

    *ppStore = _msgStore->GetRaw();
    (*ppStore)->AddRef();
    return S_OK;
}

STDMETHODIMP FormViewer::GetFolder( LPMAPIFOLDER FAR * ppFolder )
{
    OutputDebugString("CMyMAPIFormViewer::GetFolder\n");
    *ppFolder = _folder->GetRaw();
    (*ppFolder)->AddRef();
    return S_OK;
}

STDMETHODIMP FormViewer::GetMessage( LPMESSAGE FAR * ppmsg )
{
    OutputDebugString("CMyMAPIFormViewer::GetMessage\n");
    HRESULT hRes = S_OK;
    if ( !_message.IsNull() )
    {
        *ppmsg = _message->GetRaw();
        (*ppmsg)->AddRef();
    }
    else
    {
        *ppmsg = NULL;
        hRes = S_FALSE;
    }
    return hRes;
}

STDMETHODIMP FormViewer::GetFormManager( LPMAPIFORMMGR FAR * ppFormMgr )
{
    OutputDebugString("CMyMAPIFormViewer::GetFormManager\n");
    return MAPIOpenFormMgr( m_lpMAPISession, ppFormMgr );
}

STDMETHODIMP FormViewer::NewMessage(ULONG /*fComposeInFolder*/, LPMAPIFOLDER /*pFolderFocus*/, LPPERSISTMESSAGE pPersistMessage,
                                            LPMESSAGE FAR * ppMessage, LPMAPIMESSAGESITE FAR * ppMessageSite,
                                            LPMAPIVIEWCONTEXT FAR * ppViewContext)
{
    OutputDebugString("CMyMAPIFormViewer::NewMessage\n");

    FormViewer *lpMyMAPIFormViewer = NULL;

    HRESULT hRes = S_OK;
    EMessageSPtr newMessage = _folder->CreateMessage( );

    *ppMessage = newMessage->GetRaw();
    (*ppMessage)->AddRef();

    _form->Unadvise( m_pulConnection );
    _form.release();
    lpMyMAPIFormViewer = new FormViewer( _msgStore, m_lpMAPISession, _folder, newMessage, _verbID );
    PersistMessageSPtr persistMessage = TypeFactory::CreatePersistMessage( pPersistMessage );
    lpMyMAPIFormViewer->SetPersist( persistMessage );

    hRes = lpMyMAPIFormViewer->QueryInterface( IID_IMAPIMessageSite, (LPVOID*)ppMessageSite );
    if ( ppViewContext != NULL )
    {
        hRes = lpMyMAPIFormViewer->QueryInterface( IID_IMAPIViewContext, (LPVOID*)ppViewContext );
    }
    else
    {
        OutputDebugString("FormViewer::NewMessage ppViewContext == NULL\n");
    }

    return hRes;
}

STDMETHODIMP FormViewer::CopyMessage( LPMAPIFOLDER pFolderDestination, bool move )
{
    ESPropValueSPtr entryID = _message->getSingleProp( (int)PR_ENTRYID );
    if ( entryID.IsNull() )
    {
        return E_FAIL;
    }
    if ( pFolderDestination == NULL )
    {
        return E_FAIL;
    }
    EMAPIFolderSPtr folder = TypeFactory::CreateEMAPIFolder( pFolderDestination );
    if ( folder.IsNull() )
    {
        return E_FAIL;
    }
    ESPropValueSPtr folderID = folder->getSingleProp( (int)PR_ENTRYID );
    if ( folderID.IsNull() )
    {
        return E_FAIL;
    }
    if ( move )
    {
        Guard::MoveMessage( entryID, folderID );
    }
    else
    {
        Guard::CopyMessage( entryID, folderID );
    }
    return S_OK;
}

STDMETHODIMP FormViewer::CopyMessage( LPMAPIFOLDER pFolderDestination )
{
    OutputDebugString("CMyMAPIFormViewer::CopyMessage\n");
    return CopyMessage( pFolderDestination, false );
}

STDMETHODIMP FormViewer::MoveMessage( LPMAPIFOLDER pFolderDestination, LPMAPIVIEWCONTEXT /*pViewContext*/, LPCRECT /*prcPosRect*/ )
{
    OutputDebugString("CMyMAPIFormViewer::MoveMessage\n");
    return CopyMessage( pFolderDestination, true );
}

STDMETHODIMP FormViewer::DeleteMessage( LPMAPIVIEWCONTEXT /*pViewContext*/, LPCRECT /*prcPosRect*/)
{
    OutputDebugString("CMyMAPIFormViewer::DeleteMessage\n");
    HRESULT hRes = S_OK;
    _persistMessage->Save();
    _persistMessage->HandsOffMessage();
    _persistMessage.release();
    //hRes = m_lpPersistMessage->Save( NULL, TRUE );
    ESPropValueSPtr entryID = _message->getSingleProp( (int)PR_ENTRYID );
    _message.release();// can't delete an open message
    if ( !entryID.IsNull() )
    {
        OutputDebugString("_folder->DeleteMessage( entryID )\n");
        Guard::DeleteMessage( entryID );
    }
    else
    {
        OutputDebugString("entryID.IsNull()");
    }
    return hRes;
}

STDMETHODIMP FormViewer::SaveMessage()
{
    OutputDebugString("CMyMAPIFormViewer::SaveMessage\n");
    HRESULT hRes = S_OK;

    _message->RTFSyncRTF();
    _persistMessage->Save( _message );
    if ( _verbID != (int)EXCHIVERB_REPLYTOSENDER && _verbID != (int)EXCHIVERB_REPLYTOALL && _verbID != (int)EXCHIVERB_FORWARD )
    {
        _message->SetUnRead( true );
    }
    _message->SaveChanges( (int)KEEP_OPEN_READWRITE );
    return hRes;
}

STDMETHODIMP FormViewer::SubmitMessage ( ULONG /*ulFlags*/)
{
    OutputDebugString("CMyMAPIFormViewer::SubmitMessage\n");
    HRESULT hRes = S_OK;
    _persistMessage->Save( _message );
    _persistMessage->HandsOffMessage();
    _message->RTFSyncRTF();
    _message->Submit();
    return hRes;
}

STDMETHODIMP FormViewer::GetSiteStatus (LPULONG lpulStatus)
{
    OutputDebugString("CMyMAPIFormViewer::GetSiteStatus\n");
    *lpulStatus = (int)(VCSTATUS_NEW_MESSAGE | VCSTATUS_SAVE | VCSTATUS_SUBMIT | VCSTATUS_DELETE |
        VCSTATUS_MOVE | VCSTATUS_COPY | VCSTATUS_DELETE_IS_MOVE );
    return S_OK;
}

STDMETHODIMP FormViewer::GetLastError(HRESULT /*hResult*/, ULONG /*ulFlags*/, LPMAPIERROR FAR * /*lppMAPIError*/)
{
    OutputDebugString("CMyMAPIFormViewer::GetLastError\n");
    return S_OK;
}

///////////////////////////////////////////////////////////////////////////////
// End IMAPIMessageSite implementation
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// IMAPIViewAdviseSink implementation
///////////////////////////////////////////////////////////////////////////////

//Assuming we've advised on this form, we need to Unadvise it now, or it will never unload
STDMETHODIMP FormViewer::OnShutdown()
{
    try
    {
        Guard::UnregisterForm( _formID );
    }
    catch (...){}
    OutputDebugString("CMyMAPIFormViewer::OnShutdown\n");
    _form->Unadvise( m_pulConnection );
    _form.release();
    return S_OK;
}

STDMETHODIMP FormViewer::OnNewMessage()
{
    OutputDebugString("CMyMAPIFormViewer::OnNewMessage\n");
    return S_OK;
}

STDMETHODIMP FormViewer::OnPrint( ULONG /*dwPageNumber*/, HRESULT /*hrStatus*/)
{
    OutputDebugString("CMyMAPIFormViewer::OnPrint\n");
    return S_OK;
}

STDMETHODIMP FormViewer::OnSubmitted()
{
    OutputDebugString("CMyMAPIFormViewer::OnSubmitted\n");
    return S_OK;
}

STDMETHODIMP FormViewer::OnSaved()
{
    OutputDebugString("CMyMAPIFormViewer::OnSaved\n");
    return S_OK;
}

///////////////////////////////////////////////////////////////////////////////
// End IMAPIViewAdviseSink implementation
///////////////////////////////////////////////////////////////////////////////

//set the m_lpPersistMessage pointer and get an advise to play with
STDMETHODIMP FormViewer::SetForm( MAPIFormSPtr form )
{
    _form = form;
    OutputDebugString("CMyMAPIFormViewer::SetForm\n");
    _persistMessage = _form->GetPersistMessage();
    _form->Advise( (LPMAPIVIEWADVISESINK)this, &m_pulConnection );
    return S_OK;
}

STDMETHODIMP FormViewer::SetPersist( PersistMessageSPtr persistMessage )
{
    OutputDebugString("CMyMAPIFormViewer::SetPersist\n");
    _persistMessage = persistMessage;
    _form = _persistMessage->GetMAPIForm();
    //_form->SetViewContext( (LPMAPIVIEWCONTEXT)this );
    _form->Advise( (LPMAPIVIEWADVISESINK)this, &m_pulConnection );
    return S_OK;
}
