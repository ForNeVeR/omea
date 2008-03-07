/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma once

#include "helpers.h"
#include "emapi.h"
#using <mscorlib.dll>
#include <vcclr.h>
using namespace System;

class  MsgStoreAdviseSink : public IMAPIAdviseSink
{
private :
    LONG _cRef;
    int _storeID_idx;
    gcroot<EMAPILib::IMAPIListener*> _listener;
public :
    MsgStoreAdviseSink();
    void SetListener( EMAPILib::IMAPIListener* listener );
    static MsgStoreAdviseSink* Create();

    void OnNotifyImpl( ULONG cNotif, LPNOTIFICATION pNotifications );
    virtual ~MsgStoreAdviseSink();

    STDMETHODIMP QueryInterface( REFIID riid, LPVOID *ppvObj );
    STDMETHODIMP_(ULONG) AddRef();
    STDMETHODIMP_(ULONG) Release();
    MAPI_IMAPIADVISESINK_METHODS(IMPL);
};
