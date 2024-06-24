// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

/// A singleton splashscreen window to be displayed by the launcher even before the netfx is started up (which can take much time).
class CSplash
{
public:
	static HRESULT ShowWindow(HINSTANCE hInstance);
	static HWND GetHwnd() { return m_hWnd; }

protected:
	static HRESULT RegisterWndClass();
	static HRESULT CreateSplashWindow();
	static HRESULT RenderSplash();
	static HRESULT PumpMessagesOnce();
	static LRESULT CALLBACK SplashWndProc(HWND, UINT, WPARAM, LPARAM);

protected:
	static HINSTANCE	m_hInstance;
	static HWND	m_hWnd;
};
