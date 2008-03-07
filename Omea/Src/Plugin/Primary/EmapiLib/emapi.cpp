/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma unmanaged

#include "emapi.h"
#include <lmerr.h>
#include <stdio.h>
#include "guard.h"

#define RTN_OK 0
#define RTN_USAGE 1
#define RTN_ERROR 13

HANDLE MyHeapObject::_myHeap = NULL;
int MyHeapObject::_objectsCount = 0;
int MyHeapObject::_heapSize = 0;

void MyHeapObject::CreateHeap()
{
    if ( _myHeap == NULL )
    {
        _myHeap = ::HeapCreate( HEAP_GENERATE_EXCEPTIONS, 0, 0 );
    }
}

void* MyHeapObject::operator new ( size_t size )
{
	size += 4;
	char* result = (char*)::HeapAlloc( _myHeap, HEAP_GENERATE_EXCEPTIONS, size );
	*( (int*)result ) = (int) size;
	::InterlockedIncrement( (LPLONG)&_objectsCount );
	::InterlockedExchangeAdd( (LPLONG)&_heapSize, (int) size );
	return (void*)( result + 4 );
}

void MyHeapObject::operator delete ( void* ptr )
{
	int size = -*( (int*) ( (char*)ptr - 4 ) );
	::HeapFree( _myHeap, 0, (void*) ( (char*)ptr - 4 ) );
	::InterlockedDecrement( (LPLONG)&_objectsCount );
	::InterlockedExchangeAdd( (LPLONG)&_heapSize, size );
}

LPSTR LoadErrorText( DWORD dwLastError )
{
    LPSTR MessageBuffer = NULL;

    HMODULE hModule = NULL; // default to system source
    DWORD dwBufferLength;

    DWORD dwFormatFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM;

    //
    // If dwLastError is in the network range, 
    //  load the message source.
    //

    if(dwLastError >= NERR_BASE && dwLastError <= MAX_NERR) 
    {
        hModule = LoadLibraryEx( TEXT("netmsg.dll"), NULL, LOAD_LIBRARY_AS_DATAFILE );

        if(hModule != NULL)
        {
            dwFormatFlags |= FORMAT_MESSAGE_FROM_HMODULE;
        }
    }

    //
    // Call FormatMessage() to allow for message 
    //  text to be acquired from the system 
    //  or from the supplied module handle.
    //

    dwBufferLength = FormatMessageA( 
        dwFormatFlags,
        hModule, // module to get message from (NULL == system)
        dwLastError,
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), // default language
        (LPSTR) &MessageBuffer,
        0,
        NULL
        );
    if ( dwBufferLength )
    {
        //DWORD dwBytesWritten;
    }

    //
    // If we loaded a message source, unload it.
    //
    if(hModule != NULL)
    {
        FreeLibrary(hModule);
    }
    return MessageBuffer;
}
static int windowsCount = 0;
BOOL CALLBACK EnumWindowsProc( HWND hwnd, LPARAM /*lParam*/ )
{
    LONG style = GetWindowLong( hwnd, GWL_STYLE );

    if ( ( style & WS_VISIBLE ) != 0 )
    {
        char lpClassName[50];

        int ret = GetClassName( hwnd, lpClassName, 49 );
        if ( ret > 0 )
        {
            lpClassName[ret] = 0;
            if ( strcmp( "rctrl_renwnd32", lpClassName ) == 0 )
            {
                char title[50];
                int count = GetWindowText( hwnd, title, 49 );
                if ( count > 0 )
                {
                    title[count] = 0;
                    if ( strstr( title, "Microsoft Outlook" ) != NULL )
                    {
                        windowsCount--;
                    }
                }
                windowsCount++;
            }
        }
    }
    return TRUE;
}

int CountOutlookWindows()
{
    windowsCount = 0;
    EnumWindows( EnumWindowsProc, NULL );
    return windowsCount;
}

MAPIBuffer::MAPIBuffer( HRESULT hr, LPVOID buffer ) : _buffer( NULL )
{
    if ( hr == S_OK || hr == MAPI_W_ERRORS_RETURNED )
    {
        if ( buffer == NULL )
        {
            Guard::ThrowArgumentNullException( "MAPI buffer cannot be NULL" );
        }
        _buffer = buffer;
    }
}
MAPIBuffer::~MAPIBuffer()
{
    if ( _buffer != NULL )
    {
        try
        {
            MAPIFreeBuffer( _buffer );
        }
        catch (...){}
    }
}

