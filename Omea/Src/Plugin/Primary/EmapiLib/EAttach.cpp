// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "eattach.h"
#include "StringStream.h"
#include "CharBuffer.h"
#include "CharsStorage.h"
#include "guard.h"
#include "Richedit.h"
#include "Richole.h"

#include "RCPtrDef.h"
template RCPtr<EAttach>;

EAttach::EAttach( LPATTACH lpAttach ) : MAPIProp( lpAttach )
{
    if ( lpAttach == NULL )
    {
        Guard::ThrowArgumentNullException( "lpAttach" );
    }
    _lpAttach = lpAttach;
}

EAttach::~EAttach()
{
    _lpAttach = NULL;
}
CharBufferSPtr EAttach::ReadToEnd() const
{
    LPSTREAM pStream = NULL;
    HRESULT hr =
        _lpAttach->OpenProperty( (int)PR_ATTACH_DATA_BIN, (LPIID)&IID_IStream, 0, 0, (LPUNKNOWN *)&pStream );
    if ( hr == S_OK )
    {
        StringStream stream( pStream );
        stream.ReadToEnd();
        CharBufferSPtr buffer = stream.GetBuffer();
        if ( !buffer.IsNull() && buffer->Length() != 0 )
        {
            return buffer;
        }
    }
    return CharBufferSPtr( NULL );
}
LPMESSAGE EAttach::OpenMessage() const
{
    LPMESSAGE pmsgAttach = NULL;
    HRESULT hr = _lpAttach->OpenProperty ( (int)PR_ATTACH_DATA_OBJ, &IID_IMessage, 0L, 0, (LPUNKNOWN*)&pmsgAttach );
    if ( hr == S_OK )
    {
        return pmsgAttach;
    }
    return NULL;
}
template<typename T> class ComPtr
{
public:
    ComPtr()
    {
        pointee = NULL;
    }
    ~ComPtr()
    {
        if ( pointee != NULL )
        {
            pointee->Release();
        }
    }
    T operator->() const
    {
        return pointee;
    }
    T getI() const
    {
        return pointee;
    }

    T* get()
    {
        return &pointee;
    }
    bool IsNull() const
    {
        return pointee == NULL;
    }

private:
    T pointee;
};

void EAttach::InsertOLEIntoRTF( HWND hwnd, int pos ) const
{
    ComPtr<IRichEditOle*> pRichEditOle;
    ::SendMessage( hwnd, EM_GETOLEINTERFACE, 0, (LPARAM)pRichEditOle.get() );
    if ( pRichEditOle.IsNull() )
    {
        Guard::ThrowArgumentNullException( "Cannot get IRichEditOle interface for Rich edit control" );
    }

    ComPtr<IStorage*> pStorage;
    HRESULT hr = _lpAttach->OpenProperty ( (int)PR_ATTACH_DATA_OBJ, &IID_IStorage, 0L, 0, (LPUNKNOWN*)pStorage.get() );
    Guard::CheckHR( hr );
    if ( pStorage.IsNull() )
    {
        Guard::ThrowArgumentNullException( "Cannot open property PR_ATTACH_DATA_OBJ to get IStorage" );
    }

    ComPtr<IOleClientSite*> clientSite;
    hr = pRichEditOle->GetClientSite( clientSite.get() );
    Guard::CheckHR( hr );
    if ( clientSite.IsNull() )
    {
        Guard::ThrowArgumentNullException( "Cannot get IOleClientSite from rich edit control" );
    }
    ComPtr<IOleObject*> ppvObj;
    hr = OleLoad( pStorage.getI(), IID_IOleObject, clientSite.getI(), (LPVOID*)ppvObj.get() );
    Guard::CheckHR( hr );
    if ( ppvObj.IsNull() )
    {
        Guard::ThrowArgumentNullException( "Cannot get IOleObject" );
    }

    CLSID clsid;
	hr = ppvObj->GetUserClassID(&clsid);
    Guard::CheckHR( hr );

    REOBJECT reobject;
	ZeroMemory(&reobject, sizeof(REOBJECT));
	reobject.cbStruct = sizeof(REOBJECT);

    reobject.clsid = clsid;
    reobject.cp = pos;
    reobject.dvaspect = DVASPECT_CONTENT;
    reobject.dwFlags = REO_STATIC | REO_GETMETAFILE;
    reobject.dwUser = 0;

    reobject.poleobj = ppvObj.getI();
    reobject.polesite = clientSite.getI();
    reobject.pstg = pStorage.getI();

    SIZEL sizel;
    sizel.cx = sizel.cy = 0;
    reobject.sizel = sizel;
    hr = pRichEditOle->InsertObject( &reobject );
    Guard::CheckHR( hr );
}
