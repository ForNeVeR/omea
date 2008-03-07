﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// Created by Microsoft (R) C/C++ Compiler Version 13.10.3077 (830a7814).
//
// //mypal/omniamea/mshtmlbrowser/mshtmlsite/debug/shdocvw.tlh
//
// C++ source equivalent of Win32 type library EAB22AC0-30C1-11CF-A7EB-0000C05BAE0B
// compiler-generated file created 12/07/04 at 15:53:24 - DO NOT EDIT!

#pragma once
#pragma pack(push, 8)

#include <comdef.h>

namespace SHDocVw {

//
// Forward references and typedefs
//

struct __declspec(uuid("eab22ac0-30c1-11cf-a7eb-0000c05bae0b"))
/* LIBID */ __SHDocVw;
struct __declspec(uuid("eab22ac1-30c1-11cf-a7eb-0000c05bae0b"))
/* dual interface */ IWebBrowser;
struct __declspec(uuid("eab22ac2-30c1-11cf-a7eb-0000c05bae0b"))
/* dispinterface */ DWebBrowserEvents;
enum CommandStateChangeConstants;
struct __declspec(uuid("0002df05-0000-0000-c000-000000000046"))
/* dual interface */ IWebBrowserApp;
struct __declspec(uuid("d30c1661-cdaf-11d0-8a3e-00c04fc9e26e"))
/* dual interface */ IWebBrowser2;
enum SecureLockIconConstants;
struct __declspec(uuid("34a715a0-6587-11d0-924a-0020afc7ac4d"))
/* dispinterface */ DWebBrowserEvents2;
struct /* coclass */ WebBrowser_V1;
struct /* coclass */ WebBrowser;
struct /* coclass */ InternetExplorer;
struct /* coclass */ ShellBrowserWindow;
enum ShellWindowTypeConstants;
enum ShellWindowFindWindowOptions;
struct __declspec(uuid("fe4106e0-399a-11d0-a48c-00a0c90a8f39"))
/* dispinterface */ DShellWindowsEvents;
struct __declspec(uuid("85cb6900-4d95-11cf-960c-0080c7f4ee85"))
/* dual interface */ IShellWindows;
struct /* coclass */ ShellWindows;
struct __declspec(uuid("729fe2f8-1ea8-11d1-8f85-00c04fc2fbe1"))
/* dual interface */ IShellUIHelper;
struct /* coclass */ ShellUIHelper;
struct __declspec(uuid("55136806-b2de-11d1-b9f2-00a0c98bc547"))
/* dispinterface */ DShellNameSpaceEvents;
struct __declspec(uuid("55136804-b2de-11d1-b9f2-00a0c98bc547"))
/* dual interface */ IShellFavoritesNameSpace;
struct __declspec(uuid("e572d3c9-37be-4ae2-825d-d521763e3108"))
/* dual interface */ IShellNameSpace;
struct /* coclass */ ShellNameSpace;
struct __declspec(uuid("f3470f24-15fd-11d2-bb2e-00805ff7efca"))
/* dual interface */ IScriptErrorList;
struct /* coclass */ CScriptErrorList;
struct __declspec(uuid("ba9239a4-3dd5-11d2-bf8b-00c04fb93661"))
/* dual interface */ ISearch;
struct __declspec(uuid("47c922a2-3dd5-11d2-bf8b-00c04fb93661"))
/* dual interface */ ISearches;
struct __declspec(uuid("72423e8f-8011-11d2-be79-00a0c9a83da1"))
/* dual interface */ ISearchAssistantOC;
struct __declspec(uuid("72423e8f-8011-11d2-be79-00a0c9a83da2"))
/* dual interface */ ISearchAssistantOC2;
struct __declspec(uuid("72423e8f-8011-11d2-be79-00a0c9a83da3"))
/* dual interface */ ISearchAssistantOC3;
struct __declspec(uuid("1611fdda-445b-11d2-85de-00c04fa35c89"))
/* dispinterface */ _SearchAssistantEvents;
struct /* coclass */ SearchAssistantOC;

//
// Smart pointer typedef declarations
//

_COM_SMARTPTR_TYPEDEF(IWebBrowser, __uuidof(IWebBrowser));
_COM_SMARTPTR_TYPEDEF(DWebBrowserEvents, __uuidof(DWebBrowserEvents));
_COM_SMARTPTR_TYPEDEF(IWebBrowserApp, __uuidof(IWebBrowserApp));
_COM_SMARTPTR_TYPEDEF(IWebBrowser2, __uuidof(IWebBrowser2));
_COM_SMARTPTR_TYPEDEF(DWebBrowserEvents2, __uuidof(DWebBrowserEvents2));
_COM_SMARTPTR_TYPEDEF(DShellWindowsEvents, __uuidof(DShellWindowsEvents));
_COM_SMARTPTR_TYPEDEF(IShellWindows, __uuidof(IShellWindows));
_COM_SMARTPTR_TYPEDEF(IShellUIHelper, __uuidof(IShellUIHelper));
_COM_SMARTPTR_TYPEDEF(DShellNameSpaceEvents, __uuidof(DShellNameSpaceEvents));
_COM_SMARTPTR_TYPEDEF(IShellFavoritesNameSpace, __uuidof(IShellFavoritesNameSpace));
_COM_SMARTPTR_TYPEDEF(IShellNameSpace, __uuidof(IShellNameSpace));
_COM_SMARTPTR_TYPEDEF(IScriptErrorList, __uuidof(IScriptErrorList));
_COM_SMARTPTR_TYPEDEF(ISearch, __uuidof(ISearch));
_COM_SMARTPTR_TYPEDEF(ISearches, __uuidof(ISearches));
_COM_SMARTPTR_TYPEDEF(ISearchAssistantOC, __uuidof(ISearchAssistantOC));
_COM_SMARTPTR_TYPEDEF(ISearchAssistantOC2, __uuidof(ISearchAssistantOC2));
_COM_SMARTPTR_TYPEDEF(ISearchAssistantOC3, __uuidof(ISearchAssistantOC3));
_COM_SMARTPTR_TYPEDEF(_SearchAssistantEvents, __uuidof(_SearchAssistantEvents));

//
// Type library items
//

struct __declspec(uuid("eab22ac1-30c1-11cf-a7eb-0000c05bae0b"))
IWebBrowser : IDispatch
{
    //
    // Property data
    //

    __declspec(property(get=GetApplication))
    IDispatchPtr Application;
    __declspec(property(get=GetParent))
    IDispatchPtr Parent;
    __declspec(property(get=GetContainer))
    IDispatchPtr Container;
    __declspec(property(get=GetDocument))
    IDispatchPtr Document;
    __declspec(property(get=GetTopLevelContainer))
    VARIANT_BOOL TopLevelContainer;
    __declspec(property(get=GetType))
    _bstr_t Type;
    __declspec(property(get=GetLeft,put=PutLeft))
    long Left;
    __declspec(property(get=GetTop,put=PutTop))
    long Top;
    __declspec(property(get=GetWidth,put=PutWidth))
    long Width;
    __declspec(property(get=GetHeight,put=PutHeight))
    long Height;
    __declspec(property(get=GetLocationName))
    _bstr_t LocationName;
    __declspec(property(get=GetLocationURL))
    _bstr_t LocationURL;
    __declspec(property(get=GetBusy))
    VARIANT_BOOL Busy;

    //
    // Wrapper methods for error-handling
    //

    HRESULT GoBack ( );
    HRESULT GoForward ( );
    HRESULT GoHome ( );
    HRESULT GoSearch ( );
    HRESULT Navigate (
        _bstr_t URL,
        VARIANT * Flags = &vtMissing,
        VARIANT * TargetFrameName = &vtMissing,
        VARIANT * PostData = &vtMissing,
        VARIANT * Headers = &vtMissing );
    HRESULT Refresh ( );
    HRESULT Refresh2 (
        VARIANT * Level = &vtMissing );
    HRESULT Stop ( );
    IDispatchPtr GetApplication ( );
    IDispatchPtr GetParent ( );
    IDispatchPtr GetContainer ( );
    IDispatchPtr GetDocument ( );
    VARIANT_BOOL GetTopLevelContainer ( );
    _bstr_t GetType ( );
    long GetLeft ( );
    void PutLeft (
        long pl );
    long GetTop ( );
    void PutTop (
        long pl );
    long GetWidth ( );
    void PutWidth (
        long pl );
    long GetHeight ( );
    void PutHeight (
        long pl );
    _bstr_t GetLocationName ( );
    _bstr_t GetLocationURL ( );
    VARIANT_BOOL GetBusy ( );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall raw_GoBack ( ) = 0;
      virtual HRESULT __stdcall raw_GoForward ( ) = 0;
      virtual HRESULT __stdcall raw_GoHome ( ) = 0;
      virtual HRESULT __stdcall raw_GoSearch ( ) = 0;
      virtual HRESULT __stdcall raw_Navigate (
        /*[in]*/ BSTR URL,
        /*[in]*/ VARIANT * Flags = &vtMissing,
        /*[in]*/ VARIANT * TargetFrameName = &vtMissing,
        /*[in]*/ VARIANT * PostData = &vtMissing,
        /*[in]*/ VARIANT * Headers = &vtMissing ) = 0;
      virtual HRESULT __stdcall raw_Refresh ( ) = 0;
      virtual HRESULT __stdcall raw_Refresh2 (
        /*[in]*/ VARIANT * Level = &vtMissing ) = 0;
      virtual HRESULT __stdcall raw_Stop ( ) = 0;
      virtual HRESULT __stdcall get_Application (
        /*[out,retval]*/ IDispatch * * ppDisp ) = 0;
      virtual HRESULT __stdcall get_Parent (
        /*[out,retval]*/ IDispatch * * ppDisp ) = 0;
      virtual HRESULT __stdcall get_Container (
        /*[out,retval]*/ IDispatch * * ppDisp ) = 0;
      virtual HRESULT __stdcall get_Document (
        /*[out,retval]*/ IDispatch * * ppDisp ) = 0;
      virtual HRESULT __stdcall get_TopLevelContainer (
        /*[out,retval]*/ VARIANT_BOOL * pBool ) = 0;
      virtual HRESULT __stdcall get_Type (
        /*[out,retval]*/ BSTR * Type ) = 0;
      virtual HRESULT __stdcall get_Left (
        /*[out,retval]*/ long * pl ) = 0;
      virtual HRESULT __stdcall put_Left (
        /*[in]*/ long pl ) = 0;
      virtual HRESULT __stdcall get_Top (
        /*[out,retval]*/ long * pl ) = 0;
      virtual HRESULT __stdcall put_Top (
        /*[in]*/ long pl ) = 0;
      virtual HRESULT __stdcall get_Width (
        /*[out,retval]*/ long * pl ) = 0;
      virtual HRESULT __stdcall put_Width (
        /*[in]*/ long pl ) = 0;
      virtual HRESULT __stdcall get_Height (
        /*[out,retval]*/ long * pl ) = 0;
      virtual HRESULT __stdcall put_Height (
        /*[in]*/ long pl ) = 0;
      virtual HRESULT __stdcall get_LocationName (
        /*[out,retval]*/ BSTR * LocationName ) = 0;
      virtual HRESULT __stdcall get_LocationURL (
        /*[out,retval]*/ BSTR * LocationURL ) = 0;
      virtual HRESULT __stdcall get_Busy (
        /*[out,retval]*/ VARIANT_BOOL * pBool ) = 0;
};

struct __declspec(uuid("eab22ac2-30c1-11cf-a7eb-0000c05bae0b"))
DWebBrowserEvents : IDispatch
{
    //
    // Wrapper methods for error-handling
    //

    // Methods:
    HRESULT BeforeNavigate (
        _bstr_t URL,
        long Flags,
        _bstr_t TargetFrameName,
        VARIANT * PostData,
        _bstr_t Headers,
        VARIANT_BOOL * Cancel );
    HRESULT NavigateComplete (
        _bstr_t URL );
    HRESULT StatusTextChange (
        _bstr_t Text );
    HRESULT ProgressChange (
        long Progress,
        long ProgressMax );
    HRESULT DownloadComplete ( );
    HRESULT CommandStateChange (
        long Command,
        VARIANT_BOOL Enable );
    HRESULT DownloadBegin ( );
    HRESULT NewWindow (
        _bstr_t URL,
        long Flags,
        _bstr_t TargetFrameName,
        VARIANT * PostData,
        _bstr_t Headers,
        VARIANT_BOOL * Processed );
    HRESULT TitleChange (
        _bstr_t Text );
    HRESULT FrameBeforeNavigate (
        _bstr_t URL,
        long Flags,
        _bstr_t TargetFrameName,
        VARIANT * PostData,
        _bstr_t Headers,
        VARIANT_BOOL * Cancel );
    HRESULT FrameNavigateComplete (
        _bstr_t URL );
    HRESULT FrameNewWindow (
        _bstr_t URL,
        long Flags,
        _bstr_t TargetFrameName,
        VARIANT * PostData,
        _bstr_t Headers,
        VARIANT_BOOL * Processed );
    HRESULT Quit (
        VARIANT_BOOL * Cancel );
    HRESULT WindowMove ( );
    HRESULT WindowResize ( );
    HRESULT WindowActivate ( );
    HRESULT PropertyChange (
        _bstr_t Property );
};

enum __declspec(uuid("34a226e0-df30-11cf-89a9-00a0c9054129"))
CommandStateChangeConstants
{
    CSC_UPDATECOMMANDS = -1,
    CSC_NAVIGATEFORWARD = 1,
    CSC_NAVIGATEBACK = 2
};

struct __declspec(uuid("0002df05-0000-0000-c000-000000000046"))
IWebBrowserApp : IWebBrowser
{
    //
    // Property data
    //

    __declspec(property(get=GetName))
    _bstr_t Name;
    __declspec(property(get=GetFullName))
    _bstr_t FullName;
    __declspec(property(get=GetPath))
    _bstr_t Path;
    __declspec(property(get=GetVisible,put=PutVisible))
    VARIANT_BOOL Visible;
    __declspec(property(get=GetStatusBar,put=PutStatusBar))
    VARIANT_BOOL StatusBar;
    __declspec(property(get=GetStatusText,put=PutStatusText))
    _bstr_t StatusText;
    __declspec(property(get=GetToolBar,put=PutToolBar))
    int ToolBar;
    __declspec(property(get=GetMenuBar,put=PutMenuBar))
    VARIANT_BOOL MenuBar;
    __declspec(property(get=GetFullScreen,put=PutFullScreen))
    VARIANT_BOOL FullScreen;
    __declspec(property(get=GetHWND))
    long HWND;

    //
    // Wrapper methods for error-handling
    //

    HRESULT Quit ( );
    HRESULT ClientToWindow (
        int * pcx,
        int * pcy );
    HRESULT PutProperty (
        _bstr_t Property,
        const _variant_t & vtValue );
    _variant_t GetProperty (
        _bstr_t Property );
    _bstr_t GetName ( );
    long GetHWND ( );
    _bstr_t GetFullName ( );
    _bstr_t GetPath ( );
    VARIANT_BOOL GetVisible ( );
    void PutVisible (
        VARIANT_BOOL pBool );
    VARIANT_BOOL GetStatusBar ( );
    void PutStatusBar (
        VARIANT_BOOL pBool );
    _bstr_t GetStatusText ( );
    void PutStatusText (
        _bstr_t StatusText );
    int GetToolBar ( );
    void PutToolBar (
        int Value );
    VARIANT_BOOL GetMenuBar ( );
    void PutMenuBar (
        VARIANT_BOOL Value );
    VARIANT_BOOL GetFullScreen ( );
    void PutFullScreen (
        VARIANT_BOOL pbFullScreen );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall raw_Quit ( ) = 0;
      virtual HRESULT __stdcall raw_ClientToWindow (
        /*[in,out]*/ int * pcx,
        /*[in,out]*/ int * pcy ) = 0;
      virtual HRESULT __stdcall raw_PutProperty (
        /*[in]*/ BSTR Property,
        /*[in]*/ VARIANT vtValue ) = 0;
      virtual HRESULT __stdcall raw_GetProperty (
        /*[in]*/ BSTR Property,
        /*[out,retval]*/ VARIANT * pvtValue ) = 0;
      virtual HRESULT __stdcall get_Name (
        /*[out,retval]*/ BSTR * Name ) = 0;
      virtual HRESULT __stdcall get_HWND (
        /*[out,retval]*/ long * pHWND ) = 0;
      virtual HRESULT __stdcall get_FullName (
        /*[out,retval]*/ BSTR * FullName ) = 0;
      virtual HRESULT __stdcall get_Path (
        /*[out,retval]*/ BSTR * Path ) = 0;
      virtual HRESULT __stdcall get_Visible (
        /*[out,retval]*/ VARIANT_BOOL * pBool ) = 0;
      virtual HRESULT __stdcall put_Visible (
        /*[in]*/ VARIANT_BOOL pBool ) = 0;
      virtual HRESULT __stdcall get_StatusBar (
        /*[out,retval]*/ VARIANT_BOOL * pBool ) = 0;
      virtual HRESULT __stdcall put_StatusBar (
        /*[in]*/ VARIANT_BOOL pBool ) = 0;
      virtual HRESULT __stdcall get_StatusText (
        /*[out,retval]*/ BSTR * StatusText ) = 0;
      virtual HRESULT __stdcall put_StatusText (
        /*[in]*/ BSTR StatusText ) = 0;
      virtual HRESULT __stdcall get_ToolBar (
        /*[out,retval]*/ int * Value ) = 0;
      virtual HRESULT __stdcall put_ToolBar (
        /*[in]*/ int Value ) = 0;
      virtual HRESULT __stdcall get_MenuBar (
        /*[out,retval]*/ VARIANT_BOOL * Value ) = 0;
      virtual HRESULT __stdcall put_MenuBar (
        /*[in]*/ VARIANT_BOOL Value ) = 0;
      virtual HRESULT __stdcall get_FullScreen (
        /*[out,retval]*/ VARIANT_BOOL * pbFullScreen ) = 0;
      virtual HRESULT __stdcall put_FullScreen (
        /*[in]*/ VARIANT_BOOL pbFullScreen ) = 0;
};

struct __declspec(uuid("d30c1661-cdaf-11d0-8a3e-00c04fc9e26e"))
IWebBrowser2 : IWebBrowserApp
{
    //
    // Property data
    //

    __declspec(property(get=GetOffline,put=PutOffline))
    VARIANT_BOOL Offline;
    __declspec(property(get=GetSilent,put=PutSilent))
    VARIANT_BOOL Silent;
    __declspec(property(get=GetRegisterAsBrowser,put=PutRegisterAsBrowser))
    VARIANT_BOOL RegisterAsBrowser;
    __declspec(property(get=GetRegisterAsDropTarget,put=PutRegisterAsDropTarget))
    VARIANT_BOOL RegisterAsDropTarget;
    __declspec(property(get=GetTheaterMode,put=PutTheaterMode))
    VARIANT_BOOL TheaterMode;
    __declspec(property(get=GetAddressBar,put=PutAddressBar))
    VARIANT_BOOL AddressBar;
    __declspec(property(get=GetResizable,put=PutResizable))
    VARIANT_BOOL Resizable;
    __declspec(property(get=GetReadyState))
    enum tagREADYSTATE ReadyState;

    //
    // Wrapper methods for error-handling
    //

    HRESULT Navigate2 (
        VARIANT * URL,
        VARIANT * Flags = &vtMissing,
        VARIANT * TargetFrameName = &vtMissing,
        VARIANT * PostData = &vtMissing,
        VARIANT * Headers = &vtMissing );
    enum OLECMDF QueryStatusWB (
        enum OLECMDID cmdID );
    HRESULT ExecWB (
        enum OLECMDID cmdID,
        enum OLECMDEXECOPT cmdexecopt,
        VARIANT * pvaIn,
        VARIANT * pvaOut );
    HRESULT ShowBrowserBar (
        VARIANT * pvaClsid,
        VARIANT * pvarShow = &vtMissing,
        VARIANT * pvarSize = &vtMissing );
    enum tagREADYSTATE GetReadyState ( );
    VARIANT_BOOL GetOffline ( );
    void PutOffline (
        VARIANT_BOOL pbOffline );
    VARIANT_BOOL GetSilent ( );
    void PutSilent (
        VARIANT_BOOL pbSilent );
    VARIANT_BOOL GetRegisterAsBrowser ( );
    void PutRegisterAsBrowser (
        VARIANT_BOOL pbRegister );
    VARIANT_BOOL GetRegisterAsDropTarget ( );
    void PutRegisterAsDropTarget (
        VARIANT_BOOL pbRegister );
    VARIANT_BOOL GetTheaterMode ( );
    void PutTheaterMode (
        VARIANT_BOOL pbRegister );
    VARIANT_BOOL GetAddressBar ( );
    void PutAddressBar (
        VARIANT_BOOL Value );
    VARIANT_BOOL GetResizable ( );
    void PutResizable (
        VARIANT_BOOL Value );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall raw_Navigate2 (
        /*[in]*/ VARIANT * URL,
        /*[in]*/ VARIANT * Flags = &vtMissing,
        /*[in]*/ VARIANT * TargetFrameName = &vtMissing,
        /*[in]*/ VARIANT * PostData = &vtMissing,
        /*[in]*/ VARIANT * Headers = &vtMissing ) = 0;
      virtual HRESULT __stdcall raw_QueryStatusWB (
        /*[in]*/ enum OLECMDID cmdID,
        /*[out,retval]*/ enum OLECMDF * pcmdf ) = 0;
      virtual HRESULT __stdcall raw_ExecWB (
        /*[in]*/ enum OLECMDID cmdID,
        /*[in]*/ enum OLECMDEXECOPT cmdexecopt,
        /*[in]*/ VARIANT * pvaIn,
        /*[in,out]*/ VARIANT * pvaOut ) = 0;
      virtual HRESULT __stdcall raw_ShowBrowserBar (
        /*[in]*/ VARIANT * pvaClsid,
        /*[in]*/ VARIANT * pvarShow = &vtMissing,
        /*[in]*/ VARIANT * pvarSize = &vtMissing ) = 0;
      virtual HRESULT __stdcall get_ReadyState (
        /*[out,retval]*/ enum tagREADYSTATE * plReadyState ) = 0;
      virtual HRESULT __stdcall get_Offline (
        /*[out,retval]*/ VARIANT_BOOL * pbOffline ) = 0;
      virtual HRESULT __stdcall put_Offline (
        /*[in]*/ VARIANT_BOOL pbOffline ) = 0;
      virtual HRESULT __stdcall get_Silent (
        /*[out,retval]*/ VARIANT_BOOL * pbSilent ) = 0;
      virtual HRESULT __stdcall put_Silent (
        /*[in]*/ VARIANT_BOOL pbSilent ) = 0;
      virtual HRESULT __stdcall get_RegisterAsBrowser (
        /*[out,retval]*/ VARIANT_BOOL * pbRegister ) = 0;
      virtual HRESULT __stdcall put_RegisterAsBrowser (
        /*[in]*/ VARIANT_BOOL pbRegister ) = 0;
      virtual HRESULT __stdcall get_RegisterAsDropTarget (
        /*[out,retval]*/ VARIANT_BOOL * pbRegister ) = 0;
      virtual HRESULT __stdcall put_RegisterAsDropTarget (
        /*[in]*/ VARIANT_BOOL pbRegister ) = 0;
      virtual HRESULT __stdcall get_TheaterMode (
        /*[out,retval]*/ VARIANT_BOOL * pbRegister ) = 0;
      virtual HRESULT __stdcall put_TheaterMode (
        /*[in]*/ VARIANT_BOOL pbRegister ) = 0;
      virtual HRESULT __stdcall get_AddressBar (
        /*[out,retval]*/ VARIANT_BOOL * Value ) = 0;
      virtual HRESULT __stdcall put_AddressBar (
        /*[in]*/ VARIANT_BOOL Value ) = 0;
      virtual HRESULT __stdcall get_Resizable (
        /*[out,retval]*/ VARIANT_BOOL * Value ) = 0;
      virtual HRESULT __stdcall put_Resizable (
        /*[in]*/ VARIANT_BOOL Value ) = 0;
};

enum __declspec(uuid("65507be0-91a8-11d3-a845-009027220e6d"))
SecureLockIconConstants
{
    secureLockIconUnsecure = 0,
    secureLockIconMixed = 1,
    secureLockIconSecureUnknownBits = 2,
    secureLockIconSecure40Bit = 3,
    secureLockIconSecure56Bit = 4,
    secureLockIconSecureFortezza = 5,
    secureLockIconSecure128Bit = 6
};

struct __declspec(uuid("34a715a0-6587-11d0-924a-0020afc7ac4d"))
DWebBrowserEvents2 : IDispatch
{
    //
    // Wrapper methods for error-handling
    //

    // Methods:
    HRESULT StatusTextChange (
        _bstr_t Text );
    HRESULT ProgressChange (
        long Progress,
        long ProgressMax );
    HRESULT CommandStateChange (
        long Command,
        VARIANT_BOOL Enable );
    HRESULT DownloadBegin ( );
    HRESULT DownloadComplete ( );
    HRESULT TitleChange (
        _bstr_t Text );
    HRESULT PropertyChange (
        _bstr_t szProperty );
    HRESULT BeforeNavigate2 (
        IDispatch * pDisp,
        VARIANT * URL,
        VARIANT * Flags,
        VARIANT * TargetFrameName,
        VARIANT * PostData,
        VARIANT * Headers,
        VARIANT_BOOL * Cancel );
    HRESULT NewWindow2 (
        IDispatch * * ppDisp,
        VARIANT_BOOL * Cancel );
    HRESULT NavigateComplete2 (
        IDispatch * pDisp,
        VARIANT * URL );
    HRESULT DocumentComplete (
        IDispatch * pDisp,
        VARIANT * URL );
    HRESULT OnQuit ( );
    HRESULT OnVisible (
        VARIANT_BOOL Visible );
    HRESULT OnToolBar (
        VARIANT_BOOL ToolBar );
    HRESULT OnMenuBar (
        VARIANT_BOOL MenuBar );
    HRESULT OnStatusBar (
        VARIANT_BOOL StatusBar );
    HRESULT OnFullScreen (
        VARIANT_BOOL FullScreen );
    HRESULT OnTheaterMode (
        VARIANT_BOOL TheaterMode );
    HRESULT WindowSetResizable (
        VARIANT_BOOL Resizable );
    HRESULT WindowSetLeft (
        long Left );
    HRESULT WindowSetTop (
        long Top );
    HRESULT WindowSetWidth (
        long Width );
    HRESULT WindowSetHeight (
        long Height );
    HRESULT WindowClosing (
        VARIANT_BOOL IsChildWindow,
        VARIANT_BOOL * Cancel );
    HRESULT ClientToHostWindow (
        long * CX,
        long * CY );
    HRESULT SetSecureLockIcon (
        long SecureLockIcon );
    HRESULT FileDownload (
        VARIANT_BOOL * Cancel );
    HRESULT NavigateError (
        IDispatch * pDisp,
        VARIANT * URL,
        VARIANT * Frame,
        VARIANT * StatusCode,
        VARIANT_BOOL * Cancel );
    HRESULT PrintTemplateInstantiation (
        IDispatch * pDisp );
    HRESULT PrintTemplateTeardown (
        IDispatch * pDisp );
    HRESULT UpdatePageStatus (
        IDispatch * pDisp,
        VARIANT * nPage,
        VARIANT * fDone );
    HRESULT PrivacyImpactedStateChange (
        VARIANT_BOOL bImpacted );
    HRESULT NewWindow3 (
        IDispatch * * ppDisp,
        VARIANT_BOOL * Cancel,
        unsigned long dwFlags,
        _bstr_t bstrUrlContext,
        _bstr_t bstrUrl );
};

struct __declspec(uuid("eab22ac3-30c1-11cf-a7eb-0000c05bae0b"))
WebBrowser_V1;
    // interface IWebBrowser2
    // [ default ] interface IWebBrowser
    // [ source ] dispinterface DWebBrowserEvents2
    // [ default, source ] dispinterface DWebBrowserEvents

struct __declspec(uuid("8856f961-340a-11d0-a96b-00c04fd705a2"))
WebBrowser;
    // [ default ] interface IWebBrowser2
    // interface IWebBrowser
    // [ default, source ] dispinterface DWebBrowserEvents2
    // [ source ] dispinterface DWebBrowserEvents

struct __declspec(uuid("0002df01-0000-0000-c000-000000000046"))
InternetExplorer;
    // [ default ] interface IWebBrowser2
    // interface IWebBrowserApp
    // [ default, source ] dispinterface DWebBrowserEvents2
    // [ source ] dispinterface DWebBrowserEvents

struct __declspec(uuid("c08afd90-f2a1-11d1-8455-00a0c91f3880"))
ShellBrowserWindow;
    // [ default ] interface IWebBrowser2
    // interface IWebBrowserApp
    // [ default, source ] dispinterface DWebBrowserEvents2
    // [ source ] dispinterface DWebBrowserEvents

enum __declspec(uuid("f41e6981-28e5-11d0-82b4-00a0c90c29c5"))
ShellWindowTypeConstants
{
    SWC_EXPLORER = 0,
    SWC_BROWSER = 1,
    SWC_3RDPARTY = 2,
    SWC_CALLBACK = 4
};

enum __declspec(uuid("7716a370-38ca-11d0-a48b-00a0c90a8f39"))
ShellWindowFindWindowOptions
{
    SWFO_NEEDDISPATCH = 1,
    SWFO_INCLUDEPENDING = 2,
    SWFO_COOKIEPASSED = 4
};

struct __declspec(uuid("fe4106e0-399a-11d0-a48c-00a0c90a8f39"))
DShellWindowsEvents : IDispatch
{
    //
    // Wrapper methods for error-handling
    //

    // Methods:
    HRESULT WindowRegistered (
        long lCookie );
    HRESULT WindowRevoked (
        long lCookie );
};

struct __declspec(uuid("85cb6900-4d95-11cf-960c-0080c7f4ee85"))
IShellWindows : IDispatch
{
    //
    // Property data
    //

    __declspec(property(get=GetCount))
    long Count;

    //
    // Wrapper methods for error-handling
    //

    long GetCount ( );
    IDispatchPtr Item (
        const _variant_t & index = vtMissing );
    IUnknownPtr _NewEnum ( );
    HRESULT Register (
        IDispatch * pid,
        long HWND,
        int swClass,
        long * plCookie );
    HRESULT RegisterPending (
        long lThreadId,
        VARIANT * pvarloc,
        VARIANT * pvarlocRoot,
        int swClass,
        long * plCookie );
    HRESULT Revoke (
        long lCookie );
    HRESULT OnNavigate (
        long lCookie,
        VARIANT * pvarloc );
    HRESULT OnActivated (
        long lCookie,
        VARIANT_BOOL fActive );
    IDispatchPtr FindWindowSW (
        VARIANT * pvarloc,
        VARIANT * pvarlocRoot,
        int swClass,
        long * pHWND,
        int swfwOptions );
    HRESULT OnCreated (
        long lCookie,
        IUnknown * punk );
    HRESULT ProcessAttachDetach (
        VARIANT_BOOL fAttach );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall get_Count (
        /*[out,retval]*/ long * Count ) = 0;
      virtual HRESULT __stdcall raw_Item (
        /*[in]*/ VARIANT index,
        /*[out,retval]*/ IDispatch * * Folder ) = 0;
      virtual HRESULT __stdcall raw__NewEnum (
        /*[out,retval]*/ IUnknown * * ppunk ) = 0;
      virtual HRESULT __stdcall raw_Register (
        /*[in]*/ IDispatch * pid,
        /*[in]*/ long HWND,
        /*[in]*/ int swClass,
        /*[out]*/ long * plCookie ) = 0;
      virtual HRESULT __stdcall raw_RegisterPending (
        /*[in]*/ long lThreadId,
        /*[in]*/ VARIANT * pvarloc,
        /*[in]*/ VARIANT * pvarlocRoot,
        /*[in]*/ int swClass,
        /*[out]*/ long * plCookie ) = 0;
      virtual HRESULT __stdcall raw_Revoke (
        /*[in]*/ long lCookie ) = 0;
      virtual HRESULT __stdcall raw_OnNavigate (
        /*[in]*/ long lCookie,
        /*[in]*/ VARIANT * pvarloc ) = 0;
      virtual HRESULT __stdcall raw_OnActivated (
        /*[in]*/ long lCookie,
        /*[in]*/ VARIANT_BOOL fActive ) = 0;
      virtual HRESULT __stdcall raw_FindWindowSW (
        /*[in]*/ VARIANT * pvarloc,
        /*[in]*/ VARIANT * pvarlocRoot,
        /*[in]*/ int swClass,
        /*[out]*/ long * pHWND,
        /*[in]*/ int swfwOptions,
        /*[out,retval]*/ IDispatch * * ppdispOut ) = 0;
      virtual HRESULT __stdcall raw_OnCreated (
        /*[in]*/ long lCookie,
        /*[in]*/ IUnknown * punk ) = 0;
      virtual HRESULT __stdcall raw_ProcessAttachDetach (
        /*[in]*/ VARIANT_BOOL fAttach ) = 0;
};

struct __declspec(uuid("9ba05972-f6a8-11cf-a442-00a0c90a8f39"))
ShellWindows;
    // [ default ] interface IShellWindows
    // [ default, source ] dispinterface DShellWindowsEvents

struct __declspec(uuid("729fe2f8-1ea8-11d1-8f85-00c04fc2fbe1"))
IShellUIHelper : IDispatch
{
    //
    // Wrapper methods for error-handling
    //

    HRESULT ResetFirstBootMode ( );
    HRESULT ResetSafeMode ( );
    HRESULT RefreshOfflineDesktop ( );
    HRESULT AddFavorite (
        _bstr_t URL,
        VARIANT * Title = &vtMissing );
    HRESULT AddChannel (
        _bstr_t URL );
    HRESULT AddDesktopComponent (
        _bstr_t URL,
        _bstr_t Type,
        VARIANT * Left = &vtMissing,
        VARIANT * Top = &vtMissing,
        VARIANT * Width = &vtMissing,
        VARIANT * Height = &vtMissing );
    VARIANT_BOOL IsSubscribed (
        _bstr_t URL );
    HRESULT NavigateAndFind (
        _bstr_t URL,
        _bstr_t strQuery,
        VARIANT * varTargetFrame );
    HRESULT ImportExportFavorites (
        VARIANT_BOOL fImport,
        _bstr_t strImpExpPath );
    HRESULT AutoCompleteSaveForm (
        VARIANT * Form = &vtMissing );
    HRESULT AutoScan (
        _bstr_t strSearch,
        _bstr_t strFailureUrl,
        VARIANT * pvarTargetFrame = &vtMissing );
    HRESULT AutoCompleteAttach (
        VARIANT * Reserved = &vtMissing );
    _variant_t ShowBrowserUI (
        _bstr_t bstrName,
        VARIANT * pvarIn );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall raw_ResetFirstBootMode ( ) = 0;
      virtual HRESULT __stdcall raw_ResetSafeMode ( ) = 0;
      virtual HRESULT __stdcall raw_RefreshOfflineDesktop ( ) = 0;
      virtual HRESULT __stdcall raw_AddFavorite (
        /*[in]*/ BSTR URL,
        /*[in]*/ VARIANT * Title = &vtMissing ) = 0;
      virtual HRESULT __stdcall raw_AddChannel (
        /*[in]*/ BSTR URL ) = 0;
      virtual HRESULT __stdcall raw_AddDesktopComponent (
        /*[in]*/ BSTR URL,
        /*[in]*/ BSTR Type,
        /*[in]*/ VARIANT * Left = &vtMissing,
        /*[in]*/ VARIANT * Top = &vtMissing,
        /*[in]*/ VARIANT * Width = &vtMissing,
        /*[in]*/ VARIANT * Height = &vtMissing ) = 0;
      virtual HRESULT __stdcall raw_IsSubscribed (
        /*[in]*/ BSTR URL,
        /*[out,retval]*/ VARIANT_BOOL * pBool ) = 0;
      virtual HRESULT __stdcall raw_NavigateAndFind (
        /*[in]*/ BSTR URL,
        /*[in]*/ BSTR strQuery,
        /*[in]*/ VARIANT * varTargetFrame ) = 0;
      virtual HRESULT __stdcall raw_ImportExportFavorites (
        /*[in]*/ VARIANT_BOOL fImport,
        /*[in]*/ BSTR strImpExpPath ) = 0;
      virtual HRESULT __stdcall raw_AutoCompleteSaveForm (
        /*[in]*/ VARIANT * Form = &vtMissing ) = 0;
      virtual HRESULT __stdcall raw_AutoScan (
        /*[in]*/ BSTR strSearch,
        /*[in]*/ BSTR strFailureUrl,
        /*[in]*/ VARIANT * pvarTargetFrame = &vtMissing ) = 0;
      virtual HRESULT __stdcall raw_AutoCompleteAttach (
        /*[in]*/ VARIANT * Reserved = &vtMissing ) = 0;
      virtual HRESULT __stdcall raw_ShowBrowserUI (
        /*[in]*/ BSTR bstrName,
        /*[in]*/ VARIANT * pvarIn,
        /*[out,retval]*/ VARIANT * pvarOut ) = 0;
};

struct __declspec(uuid("64ab4bb7-111e-11d1-8f79-00c04fc2fbe1"))
ShellUIHelper;
    // [ default ] interface IShellUIHelper

struct __declspec(uuid("55136806-b2de-11d1-b9f2-00a0c98bc547"))
DShellNameSpaceEvents : IDispatch
{
    //
    // Wrapper methods for error-handling
    //

    // Methods:
    HRESULT FavoritesSelectionChange (
        long cItems,
        long hItem,
        _bstr_t strName,
        _bstr_t strUrl,
        long cVisits,
        _bstr_t strDate,
        long fAvailableOffline );
    HRESULT SelectionChange ( );
    HRESULT DoubleClick ( );
    HRESULT Initialized ( );
};

struct __declspec(uuid("55136804-b2de-11d1-b9f2-00a0c98bc547"))
IShellFavoritesNameSpace : IDispatch
{
    //
    // Property data
    //

    __declspec(property(get=GetSubscriptionsEnabled))
    VARIANT_BOOL SubscriptionsEnabled;

    //
    // Wrapper methods for error-handling
    //

    HRESULT MoveSelectionUp ( );
    HRESULT MoveSelectionDown ( );
    HRESULT ResetSort ( );
    HRESULT NewFolder ( );
    HRESULT Synchronize ( );
    HRESULT Import ( );
    HRESULT Export ( );
    HRESULT InvokeContextMenuCommand (
        _bstr_t strCommand );
    HRESULT MoveSelectionTo ( );
    VARIANT_BOOL GetSubscriptionsEnabled ( );
    VARIANT_BOOL CreateSubscriptionForSelection ( );
    VARIANT_BOOL DeleteSubscriptionForSelection ( );
    HRESULT SetRoot (
        _bstr_t bstrFullPath );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall raw_MoveSelectionUp ( ) = 0;
      virtual HRESULT __stdcall raw_MoveSelectionDown ( ) = 0;
      virtual HRESULT __stdcall raw_ResetSort ( ) = 0;
      virtual HRESULT __stdcall raw_NewFolder ( ) = 0;
      virtual HRESULT __stdcall raw_Synchronize ( ) = 0;
      virtual HRESULT __stdcall raw_Import ( ) = 0;
      virtual HRESULT __stdcall raw_Export ( ) = 0;
      virtual HRESULT __stdcall raw_InvokeContextMenuCommand (
        /*[in]*/ BSTR strCommand ) = 0;
      virtual HRESULT __stdcall raw_MoveSelectionTo ( ) = 0;
      virtual HRESULT __stdcall get_SubscriptionsEnabled (
        /*[out,retval]*/ VARIANT_BOOL * pBool ) = 0;
      virtual HRESULT __stdcall raw_CreateSubscriptionForSelection (
        /*[out,retval]*/ VARIANT_BOOL * pBool ) = 0;
      virtual HRESULT __stdcall raw_DeleteSubscriptionForSelection (
        /*[out,retval]*/ VARIANT_BOOL * pBool ) = 0;
      virtual HRESULT __stdcall raw_SetRoot (
        /*[in]*/ BSTR bstrFullPath ) = 0;
};

struct __declspec(uuid("e572d3c9-37be-4ae2-825d-d521763e3108"))
IShellNameSpace : IShellFavoritesNameSpace
{
    //
    // Property data
    //

    __declspec(property(get=GetEnumOptions,put=PutEnumOptions))
    long EnumOptions;
    __declspec(property(get=GetSelectedItem,put=PutSelectedItem))
    IDispatchPtr SelectedItem;
    __declspec(property(get=GetRoot,put=PutRoot))
    _variant_t Root;
    __declspec(property(get=GetDepth,put=PutDepth))
    int Depth;
    __declspec(property(get=GetMode,put=PutMode))
    unsigned int Mode;
    __declspec(property(get=GetFlags,put=PutFlags))
    unsigned long Flags;
    __declspec(property(get=GetTVFlags,put=PutTVFlags))
    unsigned long TVFlags;
    __declspec(property(get=GetColumns,put=PutColumns))
    _bstr_t Columns;
    __declspec(property(get=GetCountViewTypes))
    int CountViewTypes;

    //
    // Wrapper methods for error-handling
    //

    long GetEnumOptions ( );
    void PutEnumOptions (
        long pgrfEnumFlags );
    IDispatchPtr GetSelectedItem ( );
    void PutSelectedItem (
        IDispatch * pItem );
    _variant_t GetRoot ( );
    void PutRoot (
        const _variant_t & pvar );
    int GetDepth ( );
    void PutDepth (
        int piDepth );
    unsigned int GetMode ( );
    void PutMode (
        unsigned int puMode );
    unsigned long GetFlags ( );
    void PutFlags (
        unsigned long pdwFlags );
    void PutTVFlags (
        unsigned long dwFlags );
    unsigned long GetTVFlags ( );
    _bstr_t GetColumns ( );
    void PutColumns (
        _bstr_t bstrColumns );
    int GetCountViewTypes ( );
    HRESULT SetViewType (
        int iType );
    IDispatchPtr SelectedItems ( );
    HRESULT Expand (
        const _variant_t & var,
        int iDepth );
    HRESULT UnselectAll ( );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall get_EnumOptions (
        /*[out,retval]*/ long * pgrfEnumFlags ) = 0;
      virtual HRESULT __stdcall put_EnumOptions (
        /*[in]*/ long pgrfEnumFlags ) = 0;
      virtual HRESULT __stdcall get_SelectedItem (
        /*[out,retval]*/ IDispatch * * pItem ) = 0;
      virtual HRESULT __stdcall put_SelectedItem (
        /*[in]*/ IDispatch * pItem ) = 0;
      virtual HRESULT __stdcall get_Root (
        /*[out,retval]*/ VARIANT * pvar ) = 0;
      virtual HRESULT __stdcall put_Root (
        /*[in]*/ VARIANT pvar ) = 0;
      virtual HRESULT __stdcall get_Depth (
        /*[out,retval]*/ int * piDepth ) = 0;
      virtual HRESULT __stdcall put_Depth (
        /*[in]*/ int piDepth ) = 0;
      virtual HRESULT __stdcall get_Mode (
        /*[out,retval]*/ unsigned int * puMode ) = 0;
      virtual HRESULT __stdcall put_Mode (
        /*[in]*/ unsigned int puMode ) = 0;
      virtual HRESULT __stdcall get_Flags (
        /*[out,retval]*/ unsigned long * pdwFlags ) = 0;
      virtual HRESULT __stdcall put_Flags (
        /*[in]*/ unsigned long pdwFlags ) = 0;
      virtual HRESULT __stdcall put_TVFlags (
        /*[in]*/ unsigned long dwFlags ) = 0;
      virtual HRESULT __stdcall get_TVFlags (
        /*[out,retval]*/ unsigned long * dwFlags ) = 0;
      virtual HRESULT __stdcall get_Columns (
        /*[out,retval]*/ BSTR * bstrColumns ) = 0;
      virtual HRESULT __stdcall put_Columns (
        /*[in]*/ BSTR bstrColumns ) = 0;
      virtual HRESULT __stdcall get_CountViewTypes (
        /*[out,retval]*/ int * piTypes ) = 0;
      virtual HRESULT __stdcall raw_SetViewType (
        /*[in]*/ int iType ) = 0;
      virtual HRESULT __stdcall raw_SelectedItems (
        /*[out,retval]*/ IDispatch * * ppid ) = 0;
      virtual HRESULT __stdcall raw_Expand (
        /*[in]*/ VARIANT var,
        int iDepth ) = 0;
      virtual HRESULT __stdcall raw_UnselectAll ( ) = 0;
};

struct __declspec(uuid("55136805-b2de-11d1-b9f2-00a0c98bc547"))
ShellNameSpace;
    // [ default ] interface IShellNameSpace
    // [ default, source ] dispinterface DShellNameSpaceEvents

struct __declspec(uuid("f3470f24-15fd-11d2-bb2e-00805ff7efca"))
IScriptErrorList : IDispatch
{
    //
    // Wrapper methods for error-handling
    //

    HRESULT advanceError ( );
    HRESULT retreatError ( );
    long canAdvanceError ( );
    long canRetreatError ( );
    long getErrorLine ( );
    long getErrorChar ( );
    long getErrorCode ( );
    _bstr_t getErrorMsg ( );
    _bstr_t getErrorUrl ( );
    long getAlwaysShowLockState ( );
    long getDetailsPaneOpen ( );
    HRESULT setDetailsPaneOpen (
        long fDetailsPaneOpen );
    long getPerErrorDisplay ( );
    HRESULT setPerErrorDisplay (
        long fPerErrorDisplay );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall raw_advanceError ( ) = 0;
      virtual HRESULT __stdcall raw_retreatError ( ) = 0;
      virtual HRESULT __stdcall raw_canAdvanceError (
        /*[out,retval]*/ long * pfCanAdvance ) = 0;
      virtual HRESULT __stdcall raw_canRetreatError (
        /*[out,retval]*/ long * pfCanRetreat ) = 0;
      virtual HRESULT __stdcall raw_getErrorLine (
        /*[out,retval]*/ long * plLine ) = 0;
      virtual HRESULT __stdcall raw_getErrorChar (
        /*[out,retval]*/ long * plChar ) = 0;
      virtual HRESULT __stdcall raw_getErrorCode (
        /*[out,retval]*/ long * plCode ) = 0;
      virtual HRESULT __stdcall raw_getErrorMsg (
        /*[out,retval]*/ BSTR * pstr ) = 0;
      virtual HRESULT __stdcall raw_getErrorUrl (
        /*[out,retval]*/ BSTR * pstr ) = 0;
      virtual HRESULT __stdcall raw_getAlwaysShowLockState (
        /*[out,retval]*/ long * pfAlwaysShowLocked ) = 0;
      virtual HRESULT __stdcall raw_getDetailsPaneOpen (
        /*[out,retval]*/ long * pfDetailsPaneOpen ) = 0;
      virtual HRESULT __stdcall raw_setDetailsPaneOpen (
        long fDetailsPaneOpen ) = 0;
      virtual HRESULT __stdcall raw_getPerErrorDisplay (
        /*[out,retval]*/ long * pfPerErrorDisplay ) = 0;
      virtual HRESULT __stdcall raw_setPerErrorDisplay (
        long fPerErrorDisplay ) = 0;
};

struct __declspec(uuid("efd01300-160f-11d2-bb2e-00805ff7efca"))
CScriptErrorList;
    // [ default ] interface IScriptErrorList

struct __declspec(uuid("ba9239a4-3dd5-11d2-bf8b-00c04fb93661"))
ISearch : IDispatch
{
    //
    // Property data
    //

    __declspec(property(get=GetTitle))
    _bstr_t Title;
    __declspec(property(get=GetId))
    _bstr_t Id;
    __declspec(property(get=GetURL))
    _bstr_t URL;

    //
    // Wrapper methods for error-handling
    //

    _bstr_t GetTitle ( );
    _bstr_t GetId ( );
    _bstr_t GetURL ( );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall get_Title (
        /*[out,retval]*/ BSTR * pbstrTitle ) = 0;
      virtual HRESULT __stdcall get_Id (
        /*[out,retval]*/ BSTR * pbstrId ) = 0;
      virtual HRESULT __stdcall get_URL (
        /*[out,retval]*/ BSTR * pbstrUrl ) = 0;
};

struct __declspec(uuid("47c922a2-3dd5-11d2-bf8b-00c04fb93661"))
ISearches : IDispatch
{
    //
    // Property data
    //

    __declspec(property(get=GetCount))
    long Count;
    __declspec(property(get=GetDefault))
    _bstr_t Default;

    //
    // Wrapper methods for error-handling
    //

    long GetCount ( );
    _bstr_t GetDefault ( );
    ISearchPtr Item (
        const _variant_t & index = vtMissing );
    IUnknownPtr _NewEnum ( );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall get_Count (
        /*[out,retval]*/ long * plCount ) = 0;
      virtual HRESULT __stdcall get_Default (
        /*[out,retval]*/ BSTR * pbstrDefault ) = 0;
      virtual HRESULT __stdcall raw_Item (
        /*[in]*/ VARIANT index,
        /*[out,retval]*/ struct ISearch * * ppid ) = 0;
      virtual HRESULT __stdcall raw__NewEnum (
        /*[out,retval]*/ IUnknown * * ppunk ) = 0;
};

struct __declspec(uuid("72423e8f-8011-11d2-be79-00a0c9a83da1"))
ISearchAssistantOC : IDispatch
{
    //
    // Property data
    //

    __declspec(property(get=GetShellFeaturesEnabled))
    VARIANT_BOOL ShellFeaturesEnabled;
    __declspec(property(get=GetSearchAssistantDefault))
    VARIANT_BOOL SearchAssistantDefault;
    __declspec(property(get=GetSearches))
    ISearchesPtr Searches;
    __declspec(property(get=GetInWebFolder))
    VARIANT_BOOL InWebFolder;
    __declspec(property(put=PutEventHandled))
    VARIANT_BOOL EventHandled;
    __declspec(property(get=GetASProvider,put=PutASProvider))
    _bstr_t ASProvider;
    __declspec(property(get=GetASSetting,put=PutASSetting))
    int ASSetting;
    __declspec(property(get=GetVersion))
    int Version;

    //
    // Wrapper methods for error-handling
    //

    HRESULT AddNextMenuItem (
        _bstr_t bstrText,
        long idItem );
    HRESULT SetDefaultSearchUrl (
        _bstr_t bstrUrl );
    HRESULT NavigateToDefaultSearch ( );
    VARIANT_BOOL IsRestricted (
        _bstr_t bstrGuid );
    VARIANT_BOOL GetShellFeaturesEnabled ( );
    VARIANT_BOOL GetSearchAssistantDefault ( );
    ISearchesPtr GetSearches ( );
    VARIANT_BOOL GetInWebFolder ( );
    HRESULT PutProperty (
        VARIANT_BOOL bPerLocale,
        _bstr_t bstrName,
        _bstr_t bstrValue );
    _bstr_t GetProperty (
        VARIANT_BOOL bPerLocale,
        _bstr_t bstrName );
    void PutEventHandled (
        VARIANT_BOOL _arg1 );
    HRESULT ResetNextMenu ( );
    HRESULT FindOnWeb ( );
    HRESULT FindFilesOrFolders ( );
    HRESULT FindComputer ( );
    HRESULT FindPrinter ( );
    HRESULT FindPeople ( );
    _bstr_t GetSearchAssistantURL (
        VARIANT_BOOL bSubstitute,
        VARIANT_BOOL bCustomize );
    HRESULT NotifySearchSettingsChanged ( );
    void PutASProvider (
        _bstr_t pProvider );
    _bstr_t GetASProvider ( );
    void PutASSetting (
        int pSetting );
    int GetASSetting ( );
    HRESULT NETDetectNextNavigate ( );
    HRESULT PutFindText (
        _bstr_t FindText );
    int GetVersion ( );
    _bstr_t EncodeString (
        _bstr_t bstrValue,
        _bstr_t bstrCharSet,
        VARIANT_BOOL bUseUTF8 );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall raw_AddNextMenuItem (
        /*[in]*/ BSTR bstrText,
        /*[in]*/ long idItem ) = 0;
      virtual HRESULT __stdcall raw_SetDefaultSearchUrl (
        /*[in]*/ BSTR bstrUrl ) = 0;
      virtual HRESULT __stdcall raw_NavigateToDefaultSearch ( ) = 0;
      virtual HRESULT __stdcall raw_IsRestricted (
        /*[in]*/ BSTR bstrGuid,
        /*[out,retval]*/ VARIANT_BOOL * pVal ) = 0;
      virtual HRESULT __stdcall get_ShellFeaturesEnabled (
        /*[out,retval]*/ VARIANT_BOOL * pVal ) = 0;
      virtual HRESULT __stdcall get_SearchAssistantDefault (
        /*[out,retval]*/ VARIANT_BOOL * pVal ) = 0;
      virtual HRESULT __stdcall get_Searches (
        /*[out,retval]*/ struct ISearches * * ppid ) = 0;
      virtual HRESULT __stdcall get_InWebFolder (
        /*[out,retval]*/ VARIANT_BOOL * pVal ) = 0;
      virtual HRESULT __stdcall raw_PutProperty (
        /*[in]*/ VARIANT_BOOL bPerLocale,
        /*[in]*/ BSTR bstrName,
        /*[in]*/ BSTR bstrValue ) = 0;
      virtual HRESULT __stdcall raw_GetProperty (
        /*[in]*/ VARIANT_BOOL bPerLocale,
        /*[in]*/ BSTR bstrName,
        /*[out,retval]*/ BSTR * pbstrValue ) = 0;
      virtual HRESULT __stdcall put_EventHandled (
        /*[in]*/ VARIANT_BOOL _arg1 ) = 0;
      virtual HRESULT __stdcall raw_ResetNextMenu ( ) = 0;
      virtual HRESULT __stdcall raw_FindOnWeb ( ) = 0;
      virtual HRESULT __stdcall raw_FindFilesOrFolders ( ) = 0;
      virtual HRESULT __stdcall raw_FindComputer ( ) = 0;
      virtual HRESULT __stdcall raw_FindPrinter ( ) = 0;
      virtual HRESULT __stdcall raw_FindPeople ( ) = 0;
      virtual HRESULT __stdcall raw_GetSearchAssistantURL (
        /*[in]*/ VARIANT_BOOL bSubstitute,
        /*[in]*/ VARIANT_BOOL bCustomize,
        /*[out,retval]*/ BSTR * pbstrValue ) = 0;
      virtual HRESULT __stdcall raw_NotifySearchSettingsChanged ( ) = 0;
      virtual HRESULT __stdcall put_ASProvider (
        /*[in]*/ BSTR pProvider ) = 0;
      virtual HRESULT __stdcall get_ASProvider (
        /*[out,retval]*/ BSTR * pProvider ) = 0;
      virtual HRESULT __stdcall put_ASSetting (
        /*[in]*/ int pSetting ) = 0;
      virtual HRESULT __stdcall get_ASSetting (
        /*[out,retval]*/ int * pSetting ) = 0;
      virtual HRESULT __stdcall raw_NETDetectNextNavigate ( ) = 0;
      virtual HRESULT __stdcall raw_PutFindText (
        /*[in]*/ BSTR FindText ) = 0;
      virtual HRESULT __stdcall get_Version (
        /*[out,retval]*/ int * pVersion ) = 0;
      virtual HRESULT __stdcall raw_EncodeString (
        /*[in]*/ BSTR bstrValue,
        /*[in]*/ BSTR bstrCharSet,
        /*[in]*/ VARIANT_BOOL bUseUTF8,
        /*[out,retval]*/ BSTR * pbstrResult ) = 0;
};

struct __declspec(uuid("72423e8f-8011-11d2-be79-00a0c9a83da2"))
ISearchAssistantOC2 : ISearchAssistantOC
{
    //
    // Property data
    //

    __declspec(property(get=GetShowFindPrinter))
    VARIANT_BOOL ShowFindPrinter;

    //
    // Wrapper methods for error-handling
    //

    VARIANT_BOOL GetShowFindPrinter ( );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall get_ShowFindPrinter (
        /*[out,retval]*/ VARIANT_BOOL * pbShowFindPrinter ) = 0;
};

struct __declspec(uuid("72423e8f-8011-11d2-be79-00a0c9a83da3"))
ISearchAssistantOC3 : ISearchAssistantOC2
{
    //
    // Property data
    //

    __declspec(property(get=GetSearchCompanionAvailable))
    VARIANT_BOOL SearchCompanionAvailable;
    __declspec(property(get=GetUseSearchCompanion,put=PutUseSearchCompanion))
    VARIANT_BOOL UseSearchCompanion;

    //
    // Wrapper methods for error-handling
    //

    VARIANT_BOOL GetSearchCompanionAvailable ( );
    void PutUseSearchCompanion (
        VARIANT_BOOL pbUseSC );
    VARIANT_BOOL GetUseSearchCompanion ( );

    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall get_SearchCompanionAvailable (
        /*[out,retval]*/ VARIANT_BOOL * pbAvailable ) = 0;
      virtual HRESULT __stdcall put_UseSearchCompanion (
        /*[in]*/ VARIANT_BOOL pbUseSC ) = 0;
      virtual HRESULT __stdcall get_UseSearchCompanion (
        /*[out,retval]*/ VARIANT_BOOL * pbUseSC ) = 0;
};

struct __declspec(uuid("1611fdda-445b-11d2-85de-00c04fa35c89"))
_SearchAssistantEvents : IDispatch
{
    //
    // Wrapper methods for error-handling
    //

    // Methods:
    HRESULT OnNextMenuSelect (
        long idItem );
    HRESULT OnNewSearch ( );
};

struct __declspec(uuid("b45ff030-4447-11d2-85de-00c04fa35c89"))
SearchAssistantOC;
    // [ default ] interface ISearchAssistantOC3
    // [ default, source ] dispinterface _SearchAssistantEvents

} // namespace SHDocVw

#pragma pack(pop)
