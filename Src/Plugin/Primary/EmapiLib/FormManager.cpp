// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "formmanager.h"
#include "ESPropValue.h"
#include "EMessage.h"
#include "Guard.h"

#include "RCPtrDef.h"

template RCPtr<FormManager>;
template RCPtr<MAPIForm>;
template RCPtr<PersistMessage>;

FormManager::FormManager( LPMAPIFORMMGR pFormManager )
{
    _pFormManager = pFormManager;
}

FormManager::~FormManager()
{
    try
    {
        UlRelease( _pFormManager );
    }
    catch(...){}
}

FormManagerSPtr FormManager::GetFormManager( LPMAPISESSION pSession )
{
    LPMAPIFORMMGR pFormManager = NULL;
    MAPIOpenFormMgr( pSession, &pFormManager );
    return TypeFactory::CreateFormManager( pFormManager );
}

MAPIFormSPtr FormManager::LoadForm( const ESPropValueSPtr& propStatus, IMAPIMessageSite* messageSite,
                      IMAPIViewContext* viewContext, const EMessageSPtr& message ) const
{
    LPMAPIFORM pForm = NULL;
    HRESULT hr = _pFormManager->LoadForm( 0, (int)MAPI_DIALOG, propStatus->GetLPSTR(3),
        propStatus->GetLong(0), propStatus->GetLong(1), NULL, messageSite,
            message->GetRaw(), viewContext, IID_IMAPIForm, (LPVOID*)&pForm );

    Guard::CheckHR( hr );
    return TypeFactory::CreateMAPIForm( pForm );
}
MAPIFormSPtr FormManager::CreateForm() const
{
    LPMAPIFORMINFO lpMAPIFormInfo = NULL;

    HRESULT hr = _pFormManager->ResolveMessageClass( "IPM.Note", NULL, NULL, &lpMAPIFormInfo );
    Guard::CheckHR( hr );

    LPMAPIFORM pForm = NULL;
    hr = _pFormManager->CreateForm( 0, (int)MAPI_DIALOG, lpMAPIFormInfo, IID_IMAPIForm, (LPVOID *) &pForm );
    UlRelease(lpMAPIFormInfo);
    Guard::CheckHR( hr );
    return TypeFactory::CreateMAPIForm( pForm );
}

MAPIForm::MAPIForm( LPMAPIFORM pForm )
{
    _pForm = pForm;
}

MAPIForm::~MAPIForm( )
{
    try
    {
        UlRelease( _pForm );
    }
    catch(...){}
}

void MAPIForm::ShutdownForm( int saveOptions ) const
{
    _pForm->ShutdownForm( saveOptions );
}

void MAPIForm::SetViewContext( LPMAPIVIEWCONTEXT lpMAPIViewContext ) const
{
    HRESULT hr = _pForm->SetViewContext( lpMAPIViewContext );
    Guard::CheckHR( hr );
}

void MAPIForm::DoVerb( int verbID, LPMAPIVIEWCONTEXT viewContext ) const
{
    HRESULT hr = _pForm->DoVerb( verbID, viewContext, NULL, NULL );
    Guard::CheckHR( hr );
}
PersistMessageSPtr MAPIForm::GetPersistMessage() const
{
    LPPERSISTMESSAGE lpPersistMessage = NULL;
    HRESULT hRes = _pForm->QueryInterface( IID_IPersistMessage, (LPVOID*)&lpPersistMessage );
    Guard::CheckHR( hRes );
    return TypeFactory::CreatePersistMessage( lpPersistMessage );
}

void MAPIForm::Advise( LPMAPIVIEWADVISESINK adiseSink, ULONG* pulConnection )
{
    _pForm->Advise( adiseSink, pulConnection );
}
void MAPIForm::Unadvise( ULONG ulConnection )
{
    _pForm->Unadvise( ulConnection );
}
LPMAPIFORM MAPIForm::GetRaw() const
{
    return _pForm;
}

PersistMessage::PersistMessage( LPPERSISTMESSAGE lpPersistMessage )
{
    _lpPersistMessage = lpPersistMessage;
}
PersistMessage::~PersistMessage( )
{
    try
    {
        UlRelease( _lpPersistMessage );
    }
    catch(...){}
}

void PersistMessage::Save( const EMessageSPtr& message ) const
{
    HRESULT hr = _lpPersistMessage->Save( message->GetRaw(), TRUE );
    hr = hr;
}
void PersistMessage::Save() const
{
    HRESULT hr = _lpPersistMessage->Save( NULL, TRUE );
    hr = hr;
}
void PersistMessage::Load( LPMAPIMESSAGESITE lpMAPIMessageSite, const EMessageSPtr& message, const ESPropValueSPtr& propStatus ) const
{
    HRESULT hRes = _lpPersistMessage->Load( lpMAPIMessageSite, message->GetRaw(), propStatus->GetLong(0), propStatus->GetLong(1) );
    Guard::CheckHR( hRes );
}
void PersistMessage::HandsOffMessage() const
{
    HRESULT hr = _lpPersistMessage->HandsOffMessage();
    hr = hr;
}
MAPIFormSPtr PersistMessage::GetMAPIForm() const
{
    LPMAPIFORM lpMapiForm = NULL;
    HRESULT hRes = _lpPersistMessage->QueryInterface( IID_IMAPIForm, (LPVOID *)&lpMapiForm );
    Guard::CheckHR( hRes );
    return TypeFactory::CreateMAPIForm( lpMapiForm );
}
