﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma once
#include "typefactory.h"

__nogc class FormManager : public RCObject
{
private:
    LPMAPIFORMMGR _pFormManager;
public:
    FormManager( LPMAPIFORMMGR pFormManager );
    virtual ~FormManager();
    MAPIFormSPtr LoadForm( const ESPropValueSPtr& propStatus, IMAPIMessageSite* messageSite, 
        IMAPIViewContext* viewContext, const EMessageSPtr& message ) const;
    MAPIFormSPtr CreateForm( ) const;
    static FormManagerSPtr GetFormManager( LPMAPISESSION pSession );
};

__nogc class MAPIForm : public RCObject
{
private:
    LPMAPIFORM _pForm;
public:
    MAPIForm( LPMAPIFORM pForm );
    virtual ~MAPIForm();
    void SetViewContext( LPMAPIVIEWCONTEXT lpMAPIViewContext ) const;
    PersistMessageSPtr GetPersistMessage() const;
    void ShutdownForm( int ulSaveOptions ) const;
    void DoVerb( int verbID, LPMAPIVIEWCONTEXT viewContext ) const;
    void Advise( LPMAPIVIEWADVISESINK adiseSink, ULONG* m_pulConnection );
    void Unadvise( ULONG ulConnection );
    LPMAPIFORM GetRaw() const;
};

__nogc class PersistMessage : public RCObject
{
private:
    LPPERSISTMESSAGE _lpPersistMessage;
public:
    PersistMessage( LPPERSISTMESSAGE lpPersistMessage );
    virtual ~PersistMessage();
    MAPIFormSPtr GetMAPIForm() const;
    void Save( const EMessageSPtr& message ) const;
    void Save( ) const;
    void Load( LPMAPIMESSAGESITE lpMAPIMessageSite, const EMessageSPtr& message, const ESPropValueSPtr& propStatus ) const;
    void HandsOffMessage() const;
};