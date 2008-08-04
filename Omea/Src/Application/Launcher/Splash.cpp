#include "StdAfx.h"
#include "Splash.h"
#include "resource.h"

#define WND_CLASS_NAME _T("OmeaLauncherSplash")
#define WINDOW_NAME _T("Starting JetBrains Omea")

HINSTANCE CSplash::m_hInstance = NULL;
HWND CSplash::m_hWnd = NULL;

HRESULT CSplash::ShowWindow(HINSTANCE hInstance)
{
	m_hInstance = hInstance;
	HRESULT hRet;

	if(FAILED(hRet = RegisterWndClass()))
		return hRet;
	if(FAILED(hRet = CreateSplashWindow()))
		return hRet;	
	if(FAILED(hRet = RenderSplash()))
		return hRet;	
	if(FAILED(hRet = PumpMessagesOnce()))
		return hRet;	

	return S_OK;
}

HRESULT CSplash::RegisterWndClass()
{
	WNDCLASSEX wc;
	ZeroMemory(&wc, sizeof(wc));

	wc.cbSize = sizeof(wc);
	wc.hbrBackground = NULL;
	wc.hCursor = LoadCursor(NULL, IDC_WAIT);
	wc.hIcon = LoadIcon(m_hInstance, MAKEINTRESOURCE(IDI_LAUNCHER));
	wc.hIconSm = LoadIcon(m_hInstance, MAKEINTRESOURCE(IDI_LAUNCHER));
	wc.hInstance = m_hInstance;
	wc.lpfnWndProc = (WNDPROC)SplashWndProc;
	wc.lpszClassName = WND_CLASS_NAME;
	wc.style = CS_DROPSHADOW;

	if(!RegisterClassEx(&wc))
		return E_FAIL;

	return S_OK;
}

HRESULT CSplash::CreateSplashWindow()
{
	// Window handle
	m_hWnd = CreateWindowEx(WS_EX_LAYERED, WND_CLASS_NAME, WINDOW_NAME, WS_POPUP, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, NULL, NULL, m_hInstance, NULL);

	if(m_hWnd == NULL)
		return E_FAIL;

	return S_OK;
}

HRESULT CSplash::RenderSplash()
{
	HBITMAP hBmpSplash = NULL;
	HDC hdcScreen = NULL;
	HDC hdcMem = NULL;
	HGDIOBJ hPrevObject = NULL;

	__try
	{
		// Load bitmap
		hBmpSplash = LoadBitmap(m_hInstance, MAKEINTRESOURCE(IDB_SPLASH));
		if(hBmpSplash == NULL)
		{
			OutputDebugString(_T("Could not load the splash screen bitmap.\n"));
			return E_NOINTERFACE;
		}

		hdcScreen = CreateDC(_T("DISPLAY"), NULL, NULL, NULL);

		// Get bitmap dimensions
		BITMAPINFO bmpInfo;
		ZeroMemory(&bmpInfo, sizeof(bmpInfo));
		bmpInfo.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
		bmpInfo.bmiHeader.biCompression = BI_RGB;
		if(!GetDIBits(hdcScreen, hBmpSplash, 0, 0, NULL, &bmpInfo, DIB_RGB_COLORS))
		{
			OutputDebugString(_T("Could not read the splash screen bitmap dimensions.\n"));
			return E_ABORT;
		}
		int nBmpWidth = bmpInfo.bmiHeader.biWidth;
		int nBmpHeight = bmpInfo.bmiHeader.biHeight >= 0 ? bmpInfo.bmiHeader.biHeight : -bmpInfo.bmiHeader.biHeight;

		// Get screen dimensions
		int nScreenWidth = GetSystemMetrics(SM_CXSCREEN);
		int nScreenHeight = GetSystemMetrics(SM_CYSCREEN);

		// Set window dimensions
		POINT ptLocation = {((nScreenWidth - nBmpWidth) / 2), ((nScreenHeight - nBmpHeight) / 2)};
		SIZE size = {nBmpWidth, nBmpHeight};
		POINT ptEmpty = {0,0};


		/*
		// Fill the BITMAPINFO structure
		BITMAPINFO bmpInfo;
		bmpInfo.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
		bmpInfo.bmiHeader.biWidth = 400;
		bmpInfo.bmiHeader.biHeight = -300;
		bmpInfo.bmiHeader.biPlanes = 1;
		bmpInfo.bmiHeader.biBitCount = 32;
		bmpInfo.bmiHeader.biCompression = BI_RGB;

		DWORD *pBMPBuffer;
		hBmpSplash = CreateDIBSection(hdcScreen, &bmpInfo, DIB_RGB_COLORS, (void**)&pBMPBuffer, NULL, 0);
		for(int a = 0; a < bmpInfo.bmiHeader.biWidth * bmpInfo.bmiHeader.biHeight; a++)
		pBMPBuffer[a] = 0xFF000080;
		*/

		hdcMem = CreateCompatibleDC(hdcScreen);
		hPrevObject = SelectObject(hdcMem, hBmpSplash);	// Note: must not select before doing GetDIBits

		// Render the splash
		BLENDFUNCTION blend;
		blend.BlendOp = AC_SRC_OVER;
		blend.BlendFlags = 0;
		blend.SourceConstantAlpha = 229;
		blend.AlphaFormat = AC_SRC_ALPHA;

		if(!UpdateLayeredWindow(m_hWnd, NULL, &ptLocation, &size, hdcMem, &ptEmpty, NULL, &blend, ULW_ALPHA))
		{
			OutputDebugString(_T("Could not render the splash screen.\n"));
			return E_ABORT;
		}

		// Show the window
		SetWindowPos(m_hWnd, HWND_TOP, ptLocation.x, ptLocation.y, size.cx, size.cy, SWP_FRAMECHANGED | SWP_SHOWWINDOW);

		return S_OK;
	}
	__finally
	{
		SelectObject(hdcMem, hPrevObject);
		DeleteDC(hdcMem);
		DeleteDC(hdcScreen);
		DeleteObject(hBmpSplash);
	}
}

HRESULT CSplash::PumpMessagesOnce()
{
	MSG msg;
	while(PeekMessage(&msg, NULL, 0, 0, PM_REMOVE))
	{
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}

	return S_OK;
}

LRESULT CALLBACK CSplash::SplashWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	return DefWindowProc(hWnd, msg, wParam, lParam);
}
