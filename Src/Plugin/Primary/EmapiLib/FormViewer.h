// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// MyMAPIFormViewer.h: interface for the CMyMAPIFormViewer class.
//
//////////////////////////////////////////////////////////////////////

#pragma once

#include "typefactory.h"
#include "emapi.h"

class FormViewer;

class ViewContext : public IMAPIViewContext, public MyHeapObject
{
private:
    FormViewer* _formViewer;
    LONG _cRef;
public:
    ViewContext( FormViewer* formViewer );
    virtual ~ViewContext();
    STDMETHODIMP QueryInterface ( REFIID riid, LPVOID * ppvObj );
    STDMETHODIMP_(ULONG) AddRef();
    STDMETHODIMP_(ULONG) Release();
    STDMETHODIMP GetLastError( HRESULT hResult, ULONG ulFlags, LPMAPIERROR FAR * lppMAPIError );

    ////////////////////////////////////////////////////////////
    // IMAPIViewContext Functions
    ////////////////////////////////////////////////////////////
    STDMETHODIMP SetAdviseSink( LPMAPIFORMADVISESINK pmvns);
    STDMETHODIMP ActivateNext( ULONG ulDir, LPCRECT prcPosRect);
    STDMETHODIMP GetPrintSetup( ULONG ulFlags, LPFORMPRINTSETUP FAR * lppFormPrintSetup);
    STDMETHODIMP GetSaveStream( ULONG FAR * pulFlags, ULONG FAR * pulFormat, LPSTREAM FAR * ppstm);
    STDMETHODIMP GetViewStatus( LPULONG lpulStatus);
};
class FormViewer : public IMAPIMessageSite, public IMAPIViewAdviseSink, public MyHeapObject
{
public:
    FormViewer( MsgStoreSPtr msgStore, LPMAPISESSION lpMAPISession, EMAPIFolderSPtr folder,
        EMessageSPtr message, int verbID );
    virtual ~FormViewer();
    int _formID;
public:
    STDMETHODIMP QueryInterface ( REFIID riid, LPVOID * ppvObj );
    STDMETHODIMP_(ULONG) AddRef();
    STDMETHODIMP_(ULONG) Release();

    STDMETHODIMP SetForm( MAPIFormSPtr form );
    STDMETHODIMP SetPersist( PersistMessageSPtr persistMessage );

    STDMETHODIMP GetLastError( HRESULT hResult, ULONG ulFlags, LPMAPIERROR FAR * lppMAPIError );

    ////////////////////////////////////////////////////////////
    // IMAPIMessageSite Functions
    ////////////////////////////////////////////////////////////
    STDMETHODIMP GetSession ( LPMAPISESSION FAR * ppSession );
    STDMETHODIMP GetStore ( LPMDB FAR * ppStore );
    STDMETHODIMP GetFolder ( LPMAPIFOLDER FAR * ppFolder );
    STDMETHODIMP GetMessage ( LPMESSAGE FAR * ppmsg );
    STDMETHODIMP GetFormManager ( LPMAPIFORMMGR FAR * ppFormMgr );
    STDMETHODIMP NewMessage ( ULONG fComposeInFolder, LPMAPIFOLDER pFolderFocus, LPPERSISTMESSAGE pPersistMessage,
        LPMESSAGE FAR * ppMessage, LPMAPIMESSAGESITE FAR * ppMessageSite, LPMAPIVIEWCONTEXT FAR * ppViewContext );
    STDMETHODIMP CopyMessage ( LPMAPIFOLDER pFolderDestination );
    STDMETHODIMP MoveMessage ( LPMAPIFOLDER pFolderDestination, LPMAPIVIEWCONTEXT pViewContext, LPCRECT prcPosRect );
    STDMETHODIMP DeleteMessage ( LPMAPIVIEWCONTEXT pViewContext, LPCRECT prcPosRect );
    STDMETHODIMP SaveMessage ();
    STDMETHODIMP SubmitMessage ( ULONG ulFlags );
    STDMETHODIMP GetSiteStatus ( LPULONG lpulStatus );
    ////////////////////////////////////////////////////////////
    // IMAPIViewAdviseSink Functions
    ////////////////////////////////////////////////////////////
    STDMETHODIMP OnShutdown();
    STDMETHODIMP OnNewMessage();
    STDMETHODIMP OnPrint( ULONG dwPageNumber, HRESULT hrStatus);
    STDMETHODIMP OnSubmitted();
    STDMETHODIMP OnSaved();

private :
    STDMETHODIMP CopyMessage( LPMAPIFOLDER pFolderDestination, bool move = false );
private :
    MsgStoreSPtr _msgStore;
    EMAPIFolderSPtr _folder;
    EMessageSPtr _message;
    PersistMessageSPtr _persistMessage;
    MAPIFormSPtr _form;

    bool _listen;
    LONG m_cRef;
    LPMAPIFORMADVISESINK	m_lpMapiFormAdviseSink;
    SBinary					m_MessageEID;
    ULONG					m_pulConnection;
    LPMAPISESSION			m_lpMAPISession;
    int                     _verbID;
    ViewContext* _viewContext;
};
