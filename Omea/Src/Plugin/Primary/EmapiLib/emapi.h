/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma once

#define INITGUID
#define USES_IID_IMAPIAdviseSink
#define USES_IID_IMAPIMessageSite
#define USES_IID_IMAPIViewContext
#define USES_IID_IMAPIViewAdviseSink
#define USES_IID_IUnknown
#define USES_IID_IMessage
#define USES_IID_IMAPIForm
#define USES_IID_IPersistMessage
#define USES_IID_IMAPIFolder
#define USES_IID_IAttachment
#define USES_IID_IABContainer
#define USES_IID_IMailUser
#define USES_IID_IMsgStore
#define USES_IID_IMAPITable

#include <windows.h>
#include <initguid.h>
#include <mapiguid.h>

#include <MapiX.h>
#include <MapiUtil.h>
#include <mapiform.h>
#include <exchform.h>

#ifndef PR_MSG_EDITOR_FORMAT 
    #define PR_MSG_EDITOR_FORMAT PROP_TAG( PT_LONG, 0x5909 )
    #define EDITOR_FORMAT_DONTKNOW ((int)0) 
    #define EDITOR_FORMAT_PLAINTEXT ((int)1) 
    #define EDITOR_FORMAT_HTML ((int)2) 
    #define EDITOR_FORMAT_RTF ((int)3) 
#endif

#define PR_BODY_HTML PROP_TAG(PT_TSTRING, 0x1013)

#define PR_INET_MAIL_OVERRIDE_FORMAT PROP_TAG( PT_LONG, 0x5902)
#define ENCODING_PREFERENCE 0x00020000
#define BODY_ENCODING_TEXT_AND_HTML 0x00100000
#define ENCODING_MIME 0x00040000

//default folders
const int PR_IPM_DRAFTS_ENTRYID = 0x36D70102;
const int PR_IPM_TASK_ENTRYID = 0x36D40102;

LPSTR LoadErrorText( DWORD dwLastError );

class MyHeapObject
{
private:
    static HANDLE	_myHeap;
	static int		_objectsCount;
	static int		_heapSize;
public:
    static void CreateHeap();
	static int ObjectsCount() { return _objectsCount; }
	static int HeapSize() { return _heapSize; }
    void* operator new ( size_t size );
    void* operator new ( size_t /*size*/, void* ptr ){ return ptr; }
    void operator delete ( void* ptr );
    void operator delete ( void* /*ptr*/, void* /*ptr*/ ){}
};

int CountOutlookWindows();

class MAPIBuffer
{
private:
    LPVOID _buffer;
    MAPIBuffer( const MAPIBuffer& ){}
    MAPIBuffer& operator=( const MAPIBuffer& ){}
public:
    MAPIBuffer( HRESULT hr, LPVOID buffer );
    ~MAPIBuffer();
};

