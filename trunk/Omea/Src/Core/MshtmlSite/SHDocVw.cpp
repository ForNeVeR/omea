/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// Created by Microsoft (R) C/C++ Compiler Version 13.10.3077 (830a7814).
//
// //mypal/omniamea/mshtmlbrowser/mshtmlsite/debug/shdocvw.tli
//
// Wrapper implementations for Win32 type library EAB22AC0-30C1-11CF-A7EB-0000C05BAE0B
// compiler-generated file created 12/07/04 at 15:53:24 - DO NOT EDIT!

#include "StdAfx.h"
#pragma once

namespace SHDocVw
{
//
// interface IWebBrowser wrapper method implementations
//

HRESULT IWebBrowser::GoBack ( ) {
    HRESULT _hr = raw_GoBack();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IWebBrowser::GoForward ( ) {
    HRESULT _hr = raw_GoForward();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IWebBrowser::GoHome ( ) {
    HRESULT _hr = raw_GoHome();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IWebBrowser::GoSearch ( ) {
    HRESULT _hr = raw_GoSearch();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IWebBrowser::Navigate ( _bstr_t URL, VARIANT * Flags, VARIANT * TargetFrameName, VARIANT * PostData, VARIANT * Headers ) {
    HRESULT _hr = raw_Navigate(URL, Flags, TargetFrameName, PostData, Headers);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IWebBrowser::Refresh ( ) {
    HRESULT _hr = raw_Refresh();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IWebBrowser::Refresh2 ( VARIANT * Level ) {
    HRESULT _hr = raw_Refresh2(Level);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IWebBrowser::Stop ( ) {
    HRESULT _hr = raw_Stop();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

IDispatchPtr IWebBrowser::GetApplication ( ) {
    IDispatch * _result = 0;
    HRESULT _hr = get_Application(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return IDispatchPtr(_result, false);
}

IDispatchPtr IWebBrowser::GetParent ( ) {
    IDispatch * _result = 0;
    HRESULT _hr = get_Parent(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return IDispatchPtr(_result, false);
}

IDispatchPtr IWebBrowser::GetContainer ( ) {
    IDispatch * _result = 0;
    HRESULT _hr = get_Container(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return IDispatchPtr(_result, false);
}

IDispatchPtr IWebBrowser::GetDocument ( ) {
    IDispatch * _result = 0;
    HRESULT _hr = get_Document(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return IDispatchPtr(_result, false);
}

VARIANT_BOOL IWebBrowser::GetTopLevelContainer ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_TopLevelContainer(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

_bstr_t IWebBrowser::GetType ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_Type(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

long IWebBrowser::GetLeft ( ) {
    long _result = 0;
    HRESULT _hr = get_Left(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowser::PutLeft ( long pl ) {
    HRESULT _hr = put_Left(pl);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

long IWebBrowser::GetTop ( ) {
    long _result = 0;
    HRESULT _hr = get_Top(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowser::PutTop ( long pl ) {
    HRESULT _hr = put_Top(pl);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

long IWebBrowser::GetWidth ( ) {
    long _result = 0;
    HRESULT _hr = get_Width(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowser::PutWidth ( long pl ) {
    HRESULT _hr = put_Width(pl);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

long IWebBrowser::GetHeight ( ) {
    long _result = 0;
    HRESULT _hr = get_Height(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowser::PutHeight ( long pl ) {
    HRESULT _hr = put_Height(pl);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

_bstr_t IWebBrowser::GetLocationName ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_LocationName(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

_bstr_t IWebBrowser::GetLocationURL ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_LocationURL(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

VARIANT_BOOL IWebBrowser::GetBusy ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_Busy(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

//
// dispinterface DWebBrowserEvents wrapper method implementations
//

HRESULT DWebBrowserEvents::BeforeNavigate ( _bstr_t URL, long Flags, _bstr_t TargetFrameName, VARIANT * PostData, _bstr_t Headers, VARIANT_BOOL * Cancel ) {
    return _com_dispatch_method(this, 0x64, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008\x0003\x0008\x400c\x0008\x400b", (BSTR)URL, Flags, (BSTR)TargetFrameName, PostData, (BSTR)Headers, Cancel);
}

HRESULT DWebBrowserEvents::NavigateComplete ( _bstr_t URL ) {
    return _com_dispatch_method(this, 0x65, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008", (BSTR)URL);
}

HRESULT DWebBrowserEvents::StatusTextChange ( _bstr_t Text ) {
    return _com_dispatch_method(this, 0x66, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008", (BSTR)Text);
}

HRESULT DWebBrowserEvents::ProgressChange ( long Progress, long ProgressMax ) {
    return _com_dispatch_method(this, 0x6c, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003\x0003", Progress, ProgressMax);
}

HRESULT DWebBrowserEvents::DownloadComplete ( ) {
    return _com_dispatch_method(this, 0x68, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

HRESULT DWebBrowserEvents::CommandStateChange ( long Command, VARIANT_BOOL Enable ) {
    return _com_dispatch_method(this, 0x69, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003\x000b", Command, Enable);
}

HRESULT DWebBrowserEvents::DownloadBegin ( ) {
    return _com_dispatch_method(this, 0x6a, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

HRESULT DWebBrowserEvents::NewWindow ( _bstr_t URL, long Flags, _bstr_t TargetFrameName, VARIANT * PostData, _bstr_t Headers, VARIANT_BOOL * Processed ) {
    return _com_dispatch_method(this, 0x6b, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008\x0003\x0008\x400c\x0008\x400b", (BSTR)URL, Flags, (BSTR)TargetFrameName, PostData, (BSTR)Headers, Processed);
}

HRESULT DWebBrowserEvents::TitleChange ( _bstr_t Text ) {
    return _com_dispatch_method(this, 0x71, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008", (BSTR)Text);
}

HRESULT DWebBrowserEvents::FrameBeforeNavigate ( _bstr_t URL, long Flags, _bstr_t TargetFrameName, VARIANT * PostData, _bstr_t Headers, VARIANT_BOOL * Cancel ) {
    return _com_dispatch_method(this, 0xc8, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008\x0003\x0008\x400c\x0008\x400b", (BSTR)URL, Flags, (BSTR)TargetFrameName, PostData, (BSTR)Headers, Cancel);
}

HRESULT DWebBrowserEvents::FrameNavigateComplete ( _bstr_t URL ) {
    return _com_dispatch_method(this, 0xc9, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008", (BSTR)URL);
}

HRESULT DWebBrowserEvents::FrameNewWindow ( _bstr_t URL, long Flags, _bstr_t TargetFrameName, VARIANT * PostData, _bstr_t Headers, VARIANT_BOOL * Processed ) {
    return _com_dispatch_method(this, 0xcc, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008\x0003\x0008\x400c\x0008\x400b", (BSTR)URL, Flags, (BSTR)TargetFrameName, PostData, (BSTR)Headers, Processed);
}

HRESULT DWebBrowserEvents::Quit ( VARIANT_BOOL * Cancel ) {
    return _com_dispatch_method(this, 0x67, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x400b", Cancel);
}

HRESULT DWebBrowserEvents::WindowMove ( ) {
    return _com_dispatch_method(this, 0x6d, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

HRESULT DWebBrowserEvents::WindowResize ( ) {
    return _com_dispatch_method(this, 0x6e, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

HRESULT DWebBrowserEvents::WindowActivate ( ) {
    return _com_dispatch_method(this, 0x6f, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

HRESULT DWebBrowserEvents::PropertyChange ( _bstr_t Property ) {
    return _com_dispatch_method(this, 0x70, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008", (BSTR)Property);
}

//
// interface IWebBrowserApp wrapper method implementations
//

HRESULT IWebBrowserApp::Quit ( ) {
    HRESULT _hr = raw_Quit();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IWebBrowserApp::ClientToWindow ( int * pcx, int * pcy ) {
    HRESULT _hr = raw_ClientToWindow(pcx, pcy);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IWebBrowserApp::PutProperty ( _bstr_t Property, const _variant_t & vtValue ) {
    HRESULT _hr = raw_PutProperty(Property, vtValue);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

_variant_t IWebBrowserApp::GetProperty ( _bstr_t Property ) {
    VARIANT _result;
    VariantInit(&_result);
    HRESULT _hr = raw_GetProperty(Property, &_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _variant_t(_result, false);
}

_bstr_t IWebBrowserApp::GetName ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_Name(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

long IWebBrowserApp::GetHWND ( ) {
    long _result = 0;
    HRESULT _hr = get_HWND(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

_bstr_t IWebBrowserApp::GetFullName ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_FullName(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

_bstr_t IWebBrowserApp::GetPath ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_Path(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

VARIANT_BOOL IWebBrowserApp::GetVisible ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_Visible(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowserApp::PutVisible ( VARIANT_BOOL pBool ) {
    HRESULT _hr = put_Visible(pBool);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

VARIANT_BOOL IWebBrowserApp::GetStatusBar ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_StatusBar(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowserApp::PutStatusBar ( VARIANT_BOOL pBool ) {
    HRESULT _hr = put_StatusBar(pBool);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

_bstr_t IWebBrowserApp::GetStatusText ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_StatusText(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

void IWebBrowserApp::PutStatusText ( _bstr_t StatusText ) {
    HRESULT _hr = put_StatusText(StatusText);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

int IWebBrowserApp::GetToolBar ( ) {
    int _result = 0;
    HRESULT _hr = get_ToolBar(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowserApp::PutToolBar ( int Value ) {
    HRESULT _hr = put_ToolBar(Value);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

VARIANT_BOOL IWebBrowserApp::GetMenuBar ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_MenuBar(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowserApp::PutMenuBar ( VARIANT_BOOL Value ) {
    HRESULT _hr = put_MenuBar(Value);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

VARIANT_BOOL IWebBrowserApp::GetFullScreen ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_FullScreen(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowserApp::PutFullScreen ( VARIANT_BOOL pbFullScreen ) {
    HRESULT _hr = put_FullScreen(pbFullScreen);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

//
// interface IWebBrowser2 wrapper method implementations
//

HRESULT IWebBrowser2::Navigate2 ( VARIANT * URL, VARIANT * Flags, VARIANT * TargetFrameName, VARIANT * PostData, VARIANT * Headers ) {
    HRESULT _hr = raw_Navigate2(URL, Flags, TargetFrameName, PostData, Headers);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

enum OLECMDF IWebBrowser2::QueryStatusWB ( enum OLECMDID cmdID ) {
    enum OLECMDF _result;
    HRESULT _hr = raw_QueryStatusWB(cmdID, &_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

HRESULT IWebBrowser2::ExecWB ( enum OLECMDID cmdID, enum OLECMDEXECOPT cmdexecopt, VARIANT * pvaIn, VARIANT * pvaOut ) {
    HRESULT _hr = raw_ExecWB(cmdID, cmdexecopt, pvaIn, pvaOut);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IWebBrowser2::ShowBrowserBar ( VARIANT * pvaClsid, VARIANT * pvarShow, VARIANT * pvarSize ) {
    HRESULT _hr = raw_ShowBrowserBar(pvaClsid, pvarShow, pvarSize);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

enum tagREADYSTATE IWebBrowser2::GetReadyState ( ) {
    enum tagREADYSTATE _result;
    HRESULT _hr = get_ReadyState(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

VARIANT_BOOL IWebBrowser2::GetOffline ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_Offline(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowser2::PutOffline ( VARIANT_BOOL pbOffline ) {
    HRESULT _hr = put_Offline(pbOffline);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

VARIANT_BOOL IWebBrowser2::GetSilent ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_Silent(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowser2::PutSilent ( VARIANT_BOOL pbSilent ) {
    HRESULT _hr = put_Silent(pbSilent);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

VARIANT_BOOL IWebBrowser2::GetRegisterAsBrowser ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_RegisterAsBrowser(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowser2::PutRegisterAsBrowser ( VARIANT_BOOL pbRegister ) {
    HRESULT _hr = put_RegisterAsBrowser(pbRegister);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

VARIANT_BOOL IWebBrowser2::GetRegisterAsDropTarget ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_RegisterAsDropTarget(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowser2::PutRegisterAsDropTarget ( VARIANT_BOOL pbRegister ) {
    HRESULT _hr = put_RegisterAsDropTarget(pbRegister);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

VARIANT_BOOL IWebBrowser2::GetTheaterMode ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_TheaterMode(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowser2::PutTheaterMode ( VARIANT_BOOL pbRegister ) {
    HRESULT _hr = put_TheaterMode(pbRegister);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

VARIANT_BOOL IWebBrowser2::GetAddressBar ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_AddressBar(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowser2::PutAddressBar ( VARIANT_BOOL Value ) {
    HRESULT _hr = put_AddressBar(Value);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

VARIANT_BOOL IWebBrowser2::GetResizable ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_Resizable(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IWebBrowser2::PutResizable ( VARIANT_BOOL Value ) {
    HRESULT _hr = put_Resizable(Value);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

//
// dispinterface DWebBrowserEvents2 wrapper method implementations
//

HRESULT DWebBrowserEvents2::StatusTextChange ( _bstr_t Text ) {
    return _com_dispatch_method(this, 0x66, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008", (BSTR)Text);
}

HRESULT DWebBrowserEvents2::ProgressChange ( long Progress, long ProgressMax ) {
    return _com_dispatch_method(this, 0x6c, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003\x0003", Progress, ProgressMax);
}

HRESULT DWebBrowserEvents2::CommandStateChange ( long Command, VARIANT_BOOL Enable ) {
    return _com_dispatch_method(this, 0x69, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003\x000b", Command, Enable);
}

HRESULT DWebBrowserEvents2::DownloadBegin ( ) {
    return _com_dispatch_method(this, 0x6a, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

HRESULT DWebBrowserEvents2::DownloadComplete ( ) {
    return _com_dispatch_method(this, 0x68, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

HRESULT DWebBrowserEvents2::TitleChange ( _bstr_t Text ) {
    return _com_dispatch_method(this, 0x71, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008", (BSTR)Text);
}

HRESULT DWebBrowserEvents2::PropertyChange ( _bstr_t szProperty ) {
    return _com_dispatch_method(this, 0x70, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0008", (BSTR)szProperty);
}

HRESULT DWebBrowserEvents2::BeforeNavigate2 ( IDispatch * pDisp, VARIANT * URL, VARIANT * Flags, VARIANT * TargetFrameName, VARIANT * PostData, VARIANT * Headers, VARIANT_BOOL * Cancel ) {
    return _com_dispatch_method(this, 0xfa, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0009\x400c\x400c\x400c\x400c\x400c\x400b", pDisp, URL, Flags, TargetFrameName, PostData, Headers, Cancel);
}

HRESULT DWebBrowserEvents2::NewWindow2 ( IDispatch * * ppDisp, VARIANT_BOOL * Cancel ) {
    return _com_dispatch_method(this, 0xfb, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x4009\x400b", ppDisp, Cancel);
}

HRESULT DWebBrowserEvents2::NavigateComplete2 ( IDispatch * pDisp, VARIANT * URL ) {
    return _com_dispatch_method(this, 0xfc, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0009\x400c", pDisp, URL);
}

HRESULT DWebBrowserEvents2::DocumentComplete ( IDispatch * pDisp, VARIANT * URL ) {
    return _com_dispatch_method(this, 0x103, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0009\x400c", pDisp, URL);
}

HRESULT DWebBrowserEvents2::OnQuit ( ) {
    return _com_dispatch_method(this, 0xfd, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

HRESULT DWebBrowserEvents2::OnVisible ( VARIANT_BOOL Visible ) {
    return _com_dispatch_method(this, 0xfe, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x000b", Visible);
}

HRESULT DWebBrowserEvents2::OnToolBar ( VARIANT_BOOL ToolBar ) {
    return _com_dispatch_method(this, 0xff, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x000b", ToolBar);
}

HRESULT DWebBrowserEvents2::OnMenuBar ( VARIANT_BOOL MenuBar ) {
    return _com_dispatch_method(this, 0x100, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x000b", MenuBar);
}

HRESULT DWebBrowserEvents2::OnStatusBar ( VARIANT_BOOL StatusBar ) {
    return _com_dispatch_method(this, 0x101, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x000b", StatusBar);
}

HRESULT DWebBrowserEvents2::OnFullScreen ( VARIANT_BOOL FullScreen ) {
    return _com_dispatch_method(this, 0x102, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x000b", FullScreen);
}

HRESULT DWebBrowserEvents2::OnTheaterMode ( VARIANT_BOOL TheaterMode ) {
    return _com_dispatch_method(this, 0x104, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x000b", TheaterMode);
}

HRESULT DWebBrowserEvents2::WindowSetResizable ( VARIANT_BOOL Resizable ) {
    return _com_dispatch_method(this, 0x106, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x000b", Resizable);
}

HRESULT DWebBrowserEvents2::WindowSetLeft ( long Left ) {
    return _com_dispatch_method(this, 0x108, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003", Left);
}

HRESULT DWebBrowserEvents2::WindowSetTop ( long Top ) {
    return _com_dispatch_method(this, 0x109, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003", Top);
}

HRESULT DWebBrowserEvents2::WindowSetWidth ( long Width ) {
    return _com_dispatch_method(this, 0x10a, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003", Width);
}

HRESULT DWebBrowserEvents2::WindowSetHeight ( long Height ) {
    return _com_dispatch_method(this, 0x10b, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003", Height);
}

HRESULT DWebBrowserEvents2::WindowClosing ( VARIANT_BOOL IsChildWindow, VARIANT_BOOL * Cancel ) {
    return _com_dispatch_method(this, 0x107, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x000b\x400b", IsChildWindow, Cancel);
}

HRESULT DWebBrowserEvents2::ClientToHostWindow ( long * CX, long * CY ) {
    return _com_dispatch_method(this, 0x10c, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x4003\x4003", CX, CY);
}

HRESULT DWebBrowserEvents2::SetSecureLockIcon ( long SecureLockIcon ) {
    return _com_dispatch_method(this, 0x10d, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003", SecureLockIcon);
}

HRESULT DWebBrowserEvents2::FileDownload ( VARIANT_BOOL * Cancel ) {
    return _com_dispatch_method(this, 0x10e, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x400b", Cancel);
}

HRESULT DWebBrowserEvents2::NavigateError ( IDispatch * pDisp, VARIANT * URL, VARIANT * Frame, VARIANT * StatusCode, VARIANT_BOOL * Cancel ) {
    return _com_dispatch_method(this, 0x10f, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0009\x400c\x400c\x400c\x400b", pDisp, URL, Frame, StatusCode, Cancel);
}

HRESULT DWebBrowserEvents2::PrintTemplateInstantiation ( IDispatch * pDisp ) {
    return _com_dispatch_method(this, 0xe1, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0009", pDisp);
}

HRESULT DWebBrowserEvents2::PrintTemplateTeardown ( IDispatch * pDisp ) {
    return _com_dispatch_method(this, 0xe2, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0009", pDisp);
}

HRESULT DWebBrowserEvents2::UpdatePageStatus ( IDispatch * pDisp, VARIANT * nPage, VARIANT * fDone ) {
    return _com_dispatch_method(this, 0xe3, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0009\x400c\x400c", pDisp, nPage, fDone);
}

HRESULT DWebBrowserEvents2::PrivacyImpactedStateChange ( VARIANT_BOOL bImpacted ) {
    return _com_dispatch_method(this, 0x110, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x000b", bImpacted);
}

HRESULT DWebBrowserEvents2::NewWindow3 ( IDispatch * * ppDisp, VARIANT_BOOL * Cancel, unsigned long dwFlags, _bstr_t bstrUrlContext, _bstr_t bstrUrl ) {
    return _com_dispatch_method(this, 0x111, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x4009\x400b\x0003\x0008\x0008", ppDisp, Cancel, dwFlags, (BSTR)bstrUrlContext, (BSTR)bstrUrl);
}

//
// dispinterface DShellWindowsEvents wrapper method implementations
//

HRESULT DShellWindowsEvents::WindowRegistered ( long lCookie ) {
    return _com_dispatch_method(this, 0xc8, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003", lCookie);
}

HRESULT DShellWindowsEvents::WindowRevoked ( long lCookie ) {
    return _com_dispatch_method(this, 0xc9, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003", lCookie);
}

//
// interface IShellWindows wrapper method implementations
//

long IShellWindows::GetCount ( ) {
    long _result = 0;
    HRESULT _hr = get_Count(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

IDispatchPtr IShellWindows::Item ( const _variant_t & index ) {
    IDispatch * _result = 0;
    HRESULT _hr = raw_Item(index, &_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return IDispatchPtr(_result, false);
}

IUnknownPtr IShellWindows::_NewEnum ( ) {
    IUnknown * _result = 0;
    HRESULT _hr = raw__NewEnum(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return IUnknownPtr(_result, false);
}

HRESULT IShellWindows::Register ( IDispatch * pid, long HWND, int swClass, long * plCookie ) {
    HRESULT _hr = raw_Register(pid, HWND, swClass, plCookie);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellWindows::RegisterPending ( long lThreadId, VARIANT * pvarloc, VARIANT * pvarlocRoot, int swClass, long * plCookie ) {
    HRESULT _hr = raw_RegisterPending(lThreadId, pvarloc, pvarlocRoot, swClass, plCookie);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellWindows::Revoke ( long lCookie ) {
    HRESULT _hr = raw_Revoke(lCookie);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellWindows::OnNavigate ( long lCookie, VARIANT * pvarloc ) {
    HRESULT _hr = raw_OnNavigate(lCookie, pvarloc);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellWindows::OnActivated ( long lCookie, VARIANT_BOOL fActive ) {
    HRESULT _hr = raw_OnActivated(lCookie, fActive);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

IDispatchPtr IShellWindows::FindWindowSW ( VARIANT * pvarloc, VARIANT * pvarlocRoot, int swClass, long * pHWND, int swfwOptions ) {
    IDispatch * _result = 0;
    HRESULT _hr = raw_FindWindowSW(pvarloc, pvarlocRoot, swClass, pHWND, swfwOptions, &_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return IDispatchPtr(_result, false);
}

HRESULT IShellWindows::OnCreated ( long lCookie, IUnknown * punk ) {
    HRESULT _hr = raw_OnCreated(lCookie, punk);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellWindows::ProcessAttachDetach ( VARIANT_BOOL fAttach ) {
    HRESULT _hr = raw_ProcessAttachDetach(fAttach);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

//
// interface IShellUIHelper wrapper method implementations
//

HRESULT IShellUIHelper::ResetFirstBootMode ( ) {
    HRESULT _hr = raw_ResetFirstBootMode();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellUIHelper::ResetSafeMode ( ) {
    HRESULT _hr = raw_ResetSafeMode();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellUIHelper::RefreshOfflineDesktop ( ) {
    HRESULT _hr = raw_RefreshOfflineDesktop();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellUIHelper::AddFavorite ( _bstr_t URL, VARIANT * Title ) {
    HRESULT _hr = raw_AddFavorite(URL, Title);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellUIHelper::AddChannel ( _bstr_t URL ) {
    HRESULT _hr = raw_AddChannel(URL);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellUIHelper::AddDesktopComponent ( _bstr_t URL, _bstr_t Type, VARIANT * Left, VARIANT * Top, VARIANT * Width, VARIANT * Height ) {
    HRESULT _hr = raw_AddDesktopComponent(URL, Type, Left, Top, Width, Height);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

VARIANT_BOOL IShellUIHelper::IsSubscribed ( _bstr_t URL ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = raw_IsSubscribed(URL, &_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

HRESULT IShellUIHelper::NavigateAndFind ( _bstr_t URL, _bstr_t strQuery, VARIANT * varTargetFrame ) {
    HRESULT _hr = raw_NavigateAndFind(URL, strQuery, varTargetFrame);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellUIHelper::ImportExportFavorites ( VARIANT_BOOL fImport, _bstr_t strImpExpPath ) {
    HRESULT _hr = raw_ImportExportFavorites(fImport, strImpExpPath);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellUIHelper::AutoCompleteSaveForm ( VARIANT * Form ) {
    HRESULT _hr = raw_AutoCompleteSaveForm(Form);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellUIHelper::AutoScan ( _bstr_t strSearch, _bstr_t strFailureUrl, VARIANT * pvarTargetFrame ) {
    HRESULT _hr = raw_AutoScan(strSearch, strFailureUrl, pvarTargetFrame);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellUIHelper::AutoCompleteAttach ( VARIANT * Reserved ) {
    HRESULT _hr = raw_AutoCompleteAttach(Reserved);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

_variant_t IShellUIHelper::ShowBrowserUI ( _bstr_t bstrName, VARIANT * pvarIn ) {
    VARIANT _result;
    VariantInit(&_result);
    HRESULT _hr = raw_ShowBrowserUI(bstrName, pvarIn, &_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _variant_t(_result, false);
}

//
// dispinterface DShellNameSpaceEvents wrapper method implementations
//

HRESULT DShellNameSpaceEvents::FavoritesSelectionChange ( long cItems, long hItem, _bstr_t strName, _bstr_t strUrl, long cVisits, _bstr_t strDate, long fAvailableOffline ) {
    return _com_dispatch_method(this, 0x1, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003\x0003\x0008\x0008\x0003\x0008\x0003", cItems, hItem, (BSTR)strName, (BSTR)strUrl, cVisits, (BSTR)strDate, fAvailableOffline);
}

HRESULT DShellNameSpaceEvents::SelectionChange ( ) {
    return _com_dispatch_method(this, 0x2, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

HRESULT DShellNameSpaceEvents::DoubleClick ( ) {
    return _com_dispatch_method(this, 0x3, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

HRESULT DShellNameSpaceEvents::Initialized ( ) {
    return _com_dispatch_method(this, 0x4, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

//
// interface IShellFavoritesNameSpace wrapper method implementations
//

HRESULT IShellFavoritesNameSpace::MoveSelectionUp ( ) {
    HRESULT _hr = raw_MoveSelectionUp();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellFavoritesNameSpace::MoveSelectionDown ( ) {
    HRESULT _hr = raw_MoveSelectionDown();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellFavoritesNameSpace::ResetSort ( ) {
    HRESULT _hr = raw_ResetSort();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellFavoritesNameSpace::NewFolder ( ) {
    HRESULT _hr = raw_NewFolder();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellFavoritesNameSpace::Synchronize ( ) {
    HRESULT _hr = raw_Synchronize();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellFavoritesNameSpace::Import ( ) {
    HRESULT _hr = raw_Import();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellFavoritesNameSpace::Export ( ) {
    HRESULT _hr = raw_Export();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellFavoritesNameSpace::InvokeContextMenuCommand ( _bstr_t strCommand ) {
    HRESULT _hr = raw_InvokeContextMenuCommand(strCommand);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellFavoritesNameSpace::MoveSelectionTo ( ) {
    HRESULT _hr = raw_MoveSelectionTo();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

VARIANT_BOOL IShellFavoritesNameSpace::GetSubscriptionsEnabled ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_SubscriptionsEnabled(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

VARIANT_BOOL IShellFavoritesNameSpace::CreateSubscriptionForSelection ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = raw_CreateSubscriptionForSelection(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

VARIANT_BOOL IShellFavoritesNameSpace::DeleteSubscriptionForSelection ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = raw_DeleteSubscriptionForSelection(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

HRESULT IShellFavoritesNameSpace::SetRoot ( _bstr_t bstrFullPath ) {
    HRESULT _hr = raw_SetRoot(bstrFullPath);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

//
// interface IShellNameSpace wrapper method implementations
//

long IShellNameSpace::GetEnumOptions ( ) {
    long _result = 0;
    HRESULT _hr = get_EnumOptions(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IShellNameSpace::PutEnumOptions ( long pgrfEnumFlags ) {
    HRESULT _hr = put_EnumOptions(pgrfEnumFlags);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

IDispatchPtr IShellNameSpace::GetSelectedItem ( ) {
    IDispatch * _result = 0;
    HRESULT _hr = get_SelectedItem(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return IDispatchPtr(_result, false);
}

void IShellNameSpace::PutSelectedItem ( IDispatch * pItem ) {
    HRESULT _hr = put_SelectedItem(pItem);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

_variant_t IShellNameSpace::GetRoot ( ) {
    VARIANT _result;
    VariantInit(&_result);
    HRESULT _hr = get_Root(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _variant_t(_result, false);
}

void IShellNameSpace::PutRoot ( const _variant_t & pvar ) {
    HRESULT _hr = put_Root(pvar);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

int IShellNameSpace::GetDepth ( ) {
    int _result = 0;
    HRESULT _hr = get_Depth(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IShellNameSpace::PutDepth ( int piDepth ) {
    HRESULT _hr = put_Depth(piDepth);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

unsigned int IShellNameSpace::GetMode ( ) {
    unsigned int _result = 0;
    HRESULT _hr = get_Mode(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IShellNameSpace::PutMode ( unsigned int puMode ) {
    HRESULT _hr = put_Mode(puMode);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

unsigned long IShellNameSpace::GetFlags ( ) {
    unsigned long _result = 0;
    HRESULT _hr = get_Flags(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void IShellNameSpace::PutFlags ( unsigned long pdwFlags ) {
    HRESULT _hr = put_Flags(pdwFlags);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

void IShellNameSpace::PutTVFlags ( unsigned long dwFlags ) {
    HRESULT _hr = put_TVFlags(dwFlags);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

unsigned long IShellNameSpace::GetTVFlags ( ) {
    unsigned long _result = 0;
    HRESULT _hr = get_TVFlags(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

_bstr_t IShellNameSpace::GetColumns ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_Columns(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

void IShellNameSpace::PutColumns ( _bstr_t bstrColumns ) {
    HRESULT _hr = put_Columns(bstrColumns);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

int IShellNameSpace::GetCountViewTypes ( ) {
    int _result = 0;
    HRESULT _hr = get_CountViewTypes(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

HRESULT IShellNameSpace::SetViewType ( int iType ) {
    HRESULT _hr = raw_SetViewType(iType);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

IDispatchPtr IShellNameSpace::SelectedItems ( ) {
    IDispatch * _result = 0;
    HRESULT _hr = raw_SelectedItems(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return IDispatchPtr(_result, false);
}

HRESULT IShellNameSpace::Expand ( const _variant_t & var, int iDepth ) {
    HRESULT _hr = raw_Expand(var, iDepth);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IShellNameSpace::UnselectAll ( ) {
    HRESULT _hr = raw_UnselectAll();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

//
// interface IScriptErrorList wrapper method implementations
//

HRESULT IScriptErrorList::advanceError ( ) {
    HRESULT _hr = raw_advanceError();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT IScriptErrorList::retreatError ( ) {
    HRESULT _hr = raw_retreatError();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

long IScriptErrorList::canAdvanceError ( ) {
    long _result = 0;
    HRESULT _hr = raw_canAdvanceError(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

long IScriptErrorList::canRetreatError ( ) {
    long _result = 0;
    HRESULT _hr = raw_canRetreatError(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

long IScriptErrorList::getErrorLine ( ) {
    long _result = 0;
    HRESULT _hr = raw_getErrorLine(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

long IScriptErrorList::getErrorChar ( ) {
    long _result = 0;
    HRESULT _hr = raw_getErrorChar(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

long IScriptErrorList::getErrorCode ( ) {
    long _result = 0;
    HRESULT _hr = raw_getErrorCode(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

_bstr_t IScriptErrorList::getErrorMsg ( ) {
    BSTR _result = 0;
    HRESULT _hr = raw_getErrorMsg(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

_bstr_t IScriptErrorList::getErrorUrl ( ) {
    BSTR _result = 0;
    HRESULT _hr = raw_getErrorUrl(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

long IScriptErrorList::getAlwaysShowLockState ( ) {
    long _result = 0;
    HRESULT _hr = raw_getAlwaysShowLockState(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

long IScriptErrorList::getDetailsPaneOpen ( ) {
    long _result = 0;
    HRESULT _hr = raw_getDetailsPaneOpen(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

HRESULT IScriptErrorList::setDetailsPaneOpen ( long fDetailsPaneOpen ) {
    HRESULT _hr = raw_setDetailsPaneOpen(fDetailsPaneOpen);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

long IScriptErrorList::getPerErrorDisplay ( ) {
    long _result = 0;
    HRESULT _hr = raw_getPerErrorDisplay(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

HRESULT IScriptErrorList::setPerErrorDisplay ( long fPerErrorDisplay ) {
    HRESULT _hr = raw_setPerErrorDisplay(fPerErrorDisplay);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

//
// interface ISearch wrapper method implementations
//

_bstr_t ISearch::GetTitle ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_Title(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

_bstr_t ISearch::GetId ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_Id(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

_bstr_t ISearch::GetURL ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_URL(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

//
// interface ISearches wrapper method implementations
//

long ISearches::GetCount ( ) {
    long _result = 0;
    HRESULT _hr = get_Count(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

_bstr_t ISearches::GetDefault ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_Default(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

ISearchPtr ISearches::Item ( const _variant_t & index ) {
    struct ISearch * _result = 0;
    HRESULT _hr = raw_Item(index, &_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return ISearchPtr(_result, false);
}

IUnknownPtr ISearches::_NewEnum ( ) {
    IUnknown * _result = 0;
    HRESULT _hr = raw__NewEnum(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return IUnknownPtr(_result, false);
}

//
// interface ISearchAssistantOC wrapper method implementations
//

HRESULT ISearchAssistantOC::AddNextMenuItem ( _bstr_t bstrText, long idItem ) {
    HRESULT _hr = raw_AddNextMenuItem(bstrText, idItem);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT ISearchAssistantOC::SetDefaultSearchUrl ( _bstr_t bstrUrl ) {
    HRESULT _hr = raw_SetDefaultSearchUrl(bstrUrl);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT ISearchAssistantOC::NavigateToDefaultSearch ( ) {
    HRESULT _hr = raw_NavigateToDefaultSearch();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

VARIANT_BOOL ISearchAssistantOC::IsRestricted ( _bstr_t bstrGuid ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = raw_IsRestricted(bstrGuid, &_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

VARIANT_BOOL ISearchAssistantOC::GetShellFeaturesEnabled ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_ShellFeaturesEnabled(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

VARIANT_BOOL ISearchAssistantOC::GetSearchAssistantDefault ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_SearchAssistantDefault(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

ISearchesPtr ISearchAssistantOC::GetSearches ( ) {
    struct ISearches * _result = 0;
    HRESULT _hr = get_Searches(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return ISearchesPtr(_result, false);
}

VARIANT_BOOL ISearchAssistantOC::GetInWebFolder ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_InWebFolder(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

HRESULT ISearchAssistantOC::PutProperty ( VARIANT_BOOL bPerLocale, _bstr_t bstrName, _bstr_t bstrValue ) {
    HRESULT _hr = raw_PutProperty(bPerLocale, bstrName, bstrValue);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

_bstr_t ISearchAssistantOC::GetProperty ( VARIANT_BOOL bPerLocale, _bstr_t bstrName ) {
    BSTR _result = 0;
    HRESULT _hr = raw_GetProperty(bPerLocale, bstrName, &_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

void ISearchAssistantOC::PutEventHandled ( VARIANT_BOOL _arg1 ) {
    HRESULT _hr = put_EventHandled(_arg1);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

HRESULT ISearchAssistantOC::ResetNextMenu ( ) {
    HRESULT _hr = raw_ResetNextMenu();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT ISearchAssistantOC::FindOnWeb ( ) {
    HRESULT _hr = raw_FindOnWeb();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT ISearchAssistantOC::FindFilesOrFolders ( ) {
    HRESULT _hr = raw_FindFilesOrFolders();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT ISearchAssistantOC::FindComputer ( ) {
    HRESULT _hr = raw_FindComputer();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT ISearchAssistantOC::FindPrinter ( ) {
    HRESULT _hr = raw_FindPrinter();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT ISearchAssistantOC::FindPeople ( ) {
    HRESULT _hr = raw_FindPeople();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

_bstr_t ISearchAssistantOC::GetSearchAssistantURL ( VARIANT_BOOL bSubstitute, VARIANT_BOOL bCustomize ) {
    BSTR _result = 0;
    HRESULT _hr = raw_GetSearchAssistantURL(bSubstitute, bCustomize, &_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

HRESULT ISearchAssistantOC::NotifySearchSettingsChanged ( ) {
    HRESULT _hr = raw_NotifySearchSettingsChanged();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

void ISearchAssistantOC::PutASProvider ( _bstr_t pProvider ) {
    HRESULT _hr = put_ASProvider(pProvider);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

_bstr_t ISearchAssistantOC::GetASProvider ( ) {
    BSTR _result = 0;
    HRESULT _hr = get_ASProvider(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

void ISearchAssistantOC::PutASSetting ( int pSetting ) {
    HRESULT _hr = put_ASSetting(pSetting);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

int ISearchAssistantOC::GetASSetting ( ) {
    int _result = 0;
    HRESULT _hr = get_ASSetting(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

HRESULT ISearchAssistantOC::NETDetectNextNavigate ( ) {
    HRESULT _hr = raw_NETDetectNextNavigate();
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

HRESULT ISearchAssistantOC::PutFindText ( _bstr_t FindText ) {
    HRESULT _hr = raw_PutFindText(FindText);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _hr;
}

int ISearchAssistantOC::GetVersion ( ) {
    int _result = 0;
    HRESULT _hr = get_Version(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

_bstr_t ISearchAssistantOC::EncodeString ( _bstr_t bstrValue, _bstr_t bstrCharSet, VARIANT_BOOL bUseUTF8 ) {
    BSTR _result = 0;
    HRESULT _hr = raw_EncodeString(bstrValue, bstrCharSet, bUseUTF8, &_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _bstr_t(_result, false);
}

//
// interface ISearchAssistantOC2 wrapper method implementations
//

VARIANT_BOOL ISearchAssistantOC2::GetShowFindPrinter ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_ShowFindPrinter(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

//
// interface ISearchAssistantOC3 wrapper method implementations
//

VARIANT_BOOL ISearchAssistantOC3::GetSearchCompanionAvailable ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_SearchCompanionAvailable(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

void ISearchAssistantOC3::PutUseSearchCompanion ( VARIANT_BOOL pbUseSC ) {
    HRESULT _hr = put_UseSearchCompanion(pbUseSC);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
}

VARIANT_BOOL ISearchAssistantOC3::GetUseSearchCompanion ( ) {
    VARIANT_BOOL _result = 0;
    HRESULT _hr = get_UseSearchCompanion(&_result);
    if (FAILED(_hr)) _com_issue_errorex(_hr, this, __uuidof(this));
    return _result;
}

//
// dispinterface _SearchAssistantEvents wrapper method implementations
//

HRESULT _SearchAssistantEvents::OnNextMenuSelect ( long idItem ) {
    return _com_dispatch_method(this, 0x1, DISPATCH_METHOD, VT_EMPTY, NULL, 
        L"\x0003", idItem);
}

HRESULT _SearchAssistantEvents::OnNewSearch ( ) {
    return _com_dispatch_method(this, 0x2, DISPATCH_METHOD, VT_EMPTY, NULL, NULL);
}

}