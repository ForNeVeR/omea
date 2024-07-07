// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once
#include "typefactory.h"

class FormManager : public RCObject
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

class MAPIForm : public RCObject
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

class PersistMessage : public RCObject
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
