/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma once

#include "resource.h"
#include "Splash.h"

#define OMNIAMEA_EXE _T("OmniaMea.exe")

#define NETFX_DOWNLOAD_URI _T("http://www.microsoft.com/downloads/details.aspx?FamilyID=333325FD-AE52-4E35-B531-508D977D32A6&displaylang=en")

/// Holds the static worker functions.
class CLauncher
{
public:
	/// Entry point.
	static int Run(HINSTANCE hInstance, LPTSTR /*lpCmdLine*/, int /*nCmdShow*/)
	{
		try
		{
			// Instant splash-screen (does not require pumping)
			CSplash::ShowWindow(hInstance);	// Ignore failures

			// Preconditions
			EnsureWinNt();
			EnsureNetfx();

			// Location
			TCHAR szOmeaFolder[MAX_PATH];
			TCHAR szOmeaFilename[MAX_PATH];
			GetOmeaExecutablePath(szOmeaFolder, sizeof(szOmeaFolder) / sizeof(*szOmeaFolder), szOmeaFilename, sizeof(szOmeaFilename) / sizeof(*szOmeaFilename));

			//RegisterComServers(szOmeaFolder); // NOTE: obsoleting the COM registration now…

			//LaunchOmea(szOmeaFolder, szOmeaFilename, lpCmdLine, nCmdShow);	// External-process run
			LaunchOmeaManaged(szOmeaFolder, CSplash::GetHwnd());	// CLR hosting run
		}
		catch(int n)
		{
			return n ? n : E_FAIL;	// Failure exit code
		}
		catch(unsigned int n)
		{
			return n ? n : E_FAIL;	// Failure exit code
		}
		return 0;	// Success
	}

private:

	/// Checks whether Netfx is installed.
	static void EnsureNetfx()
	{
		if(!EnsureNetfx_Check())
		{
			if(MessageBox(NULL, _T("To run JetBrains Omea, you must have the .NET Framework 3.0 installed on your computer.\n\nWould you like to open the Mirosoft website with a free download of the .NET Framework?"), _T("JetBrains Omea – Prerequisites"), MB_YESNO | MB_ICONSTOP) == IDYES)
				ShellExecute(NULL, NULL, NETFX_DOWNLOAD_URI, NULL, NULL, SW_SHOWDEFAULT);
			throw (int)E_NOINTERFACE;
		}
	}

	/// Performs the actual netfx check, returns a boolean success flag silently.
	static bool EnsureNetfx_Check()
	{
		// This is the function pointer defintion for the shim API GetCorVersion (mscoree.dll).
		// It has existed in mscoree.dll since v1.0, and will display the version of the runtime that is currently
		// loaded into the process. If a CLR is not loaded into the process, it will load the latest version.
		typedef HRESULT (STDAPICALLTYPE *GetCORVersionDelegate)(LPWSTR szBuffer, DWORD cchBuffer, DWORD* dwLength);

		// Load mscoree that must be present on any netfx-eqipped machine (even 1.0)
		HMODULE hMscoree = LoadLibrary("mscoree.dll");
		if(!hMscoree)
			return false;

		// Might be missing in placeholder mscorees that can be present even without netfx
		GetCORVersionDelegate GetCORVersion = (GetCORVersionDelegate)GetProcAddress(hMscoree, "GetCORVersion");
		if(!GetCORVersion)
			return false;

		// Suppress netfx diag dialogs
		SetErrorMode(SEM_FAILCRITICALERRORS);

		// Get the latest version
		WCHAR szVersion[0x100];
		DWORD nCchVersion = sizeof(szVersion) / sizeof(*szVersion);
		if(FAILED(GetCORVersion(szVersion, nCchVersion, &nCchVersion)))
			return false;

		// Make sure the version is above 2
		// Must be something like “v1.0.3705”
		if(szVersion[0] != L'v')
			return false;
		int nVersion = 0;
		for(int a = 1; (szVersion[a] >= 0x30) && (szVersion[a] < 0x40) && (a < 0x10); a++)	// First number
			nVersion = nVersion * 10 + szVersion[a] - 0x30;
		if(nVersion < 2)
			return false;

		// TODO: check for netfx above CLR2 (like netfx3)

		return true;
	}

	/// Prohibits running under Win98 and alike, ensures up-to-date WinNT version.
	static void EnsureWinNt()
	{
		OSVERSIONINFO verInfo;
		verInfo.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);
		GetVersionEx( &verInfo );
		if ( verInfo.dwPlatformId != VER_PLATFORM_WIN32_NT )
		{
			MessageBox( NULL,
				"Omea currently requires Windows 2000/XP/2003/Vista and does not run under Windows 95/98/Me.",
				"Omea", MB_OK | MB_ICONSTOP );
			throw (int)E_ABORT;
		}

		if ( verInfo.dwMajorVersion < 5 )
		{
			MessageBox( NULL,
				"Omea currently requires Windows 2000/XP/2003/Vista and does not run under Windows NT 4 or earlier.",
				"Omea", MB_OK | MB_ICONSTOP );
			throw (int)E_ABORT;
		}
	}

	/// Gets the Omea executable path to be run by the launcher.
	static void GetOmeaExecutablePath(LPTSTR szFolder, int cchFolder, LPTSTR szFilename, int cchFilename)
	{
		// Note: we do not check for the Registry as Omea should be runnable from any folder; we look in the same folder as Launcher's executable
		if((cchFolder < MAX_PATH) || (cchFilename < MAX_PATH))
			throw (int)E_FAIL;

		// Get pathname of the Launcher executable
		GetModuleFileName(NULL, szFilename, cchFilename);

		// Folder
		PathRemoveFileSpec(szFilename);
		StringCchCopy(szFolder, cchFolder, szFilename);

		// File
		PathAppend(szFilename, OMNIAMEA_EXE);
	}

	/// Re-registers whatever COM servers we have, in case of a corruption.
	static void RegisterComServers(LPCTSTR szBasePath)
	{
		typedef HRESULT (__stdcall *REGSVRPROC)();
		LPCTSTR szComServers[] = 
		{
			_T("MshtmlSiteW.dll"),
			_T("IexploreOmeaW.dll")
		};

		// Default to the current folder if not specified
		if(szBasePath[0] == 0)
			szBasePath = _T(".");
		TCHAR	szDll[MAX_PATH];
		TCHAR	szMessage[0x400];

		// Register each of the servers
		for(int a = 0; a < sizeof(szComServers) / sizeof(*szComServers); a++)
		{
			// Load the component's libary
			StringCchPrintf(szDll, MAX_PATH, _T("%s\\%s"), szBasePath, szComServers[a]);
			HMODULE	hDll = LoadLibrary(szDll);
			if(hDll == NULL)
			{
				StringCchPrintf(szMessage, sizeof(szMessage) / sizeof(*szMessage), _T("Omea Launcher::RegisterComServers: Could not load the \"%s\" COM server DLL.\n"), szDll);
				OutputDebugString(szMessage);
				continue;
			}

			// Invoke the self-registration
			REGSVRPROC	pRegistrar = (REGSVRPROC)GetProcAddress(hDll, _T("DllRegisterServer"));
			if(pRegistrar != NULL)
			{
				HRESULT	hRet = pRegistrar();
				if(SUCCEEDED(hRet))
					StringCchPrintf(szMessage, sizeof(szMessage) / sizeof(*szMessage), _T("Omea Launcher::RegisterComServers: \"%s\" COM server's self-registration routine has succeeded.\n"), szDll);
				else
					StringCchPrintf(szMessage, sizeof(szMessage) / sizeof(*szMessage), _T("Omea Launcher::RegisterComServers: \"%s\" COM server's self-registration routine has reported a failure code %#010X.\n"), szDll, hRet);
			}
			else
				StringCchPrintf(szMessage, sizeof(szMessage) / sizeof(*szMessage), _T("Omea Launcher::RegisterComServers: Could not import the \"%s\" COM server's self-registration routine.\n"), szDll);

			OutputDebugString(szMessage);

			// Unload the dll
			FreeLibrary(hDll);
		}
	}

	/// Launches the Omea Executable.
	static void LaunchOmea(LPCTSTR szOmeaFolder, LPCTSTR szOmeaFilename, LPCTSTR lpCmdLine, int nCmdShow)
	{
		// Launch Omea!
		INT_PTR	ret = (INT_PTR)ShellExecute(NULL, NULL, szOmeaFilename, lpCmdLine, szOmeaFolder, nCmdShow);
		if(ret <= 32)
		{	// Indicates failure
			// Format and display the error message
			TCHAR	szReason[0x1000] = _T("");

			// Retrieve the system message
			FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, (DWORD)ret, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), szReason, sizeof(szReason) / sizeof(*szReason), NULL);

			// Format the message
			TCHAR	szErrorMessage[0x1000];
			StringCchPrintf(szErrorMessage, sizeof(szErrorMessage) / sizeof(*szErrorMessage), _T("Could not start Omea appliation, “%s”.\n%s"), szOmeaFilename, szReason);

			// Display the error
			MessageBox( NULL, szErrorMessage, _T("Omea"), MB_OK | MB_ICONEXCLAMATION );

			throw (int)ret;	// Report failure
		}
	}

	/// Launches the Omea application in the same process by starting up the .NET Framework and executing the managed code.
	static void LaunchOmeaManaged(LPCTSTR szOmeaFolder, HWND hwndInitialSplash)
	{
		/// Loads a managed assembly by its name, invokes a static member of a managed type.
		/// LParam is an optional pointer to an IntPtr parameter.
		typedef HRESULT (__cdecl *InvokeStaticFromAssemblyNameDelegate)(LPWSTR szAssemblyName, LPWSTR szTypeName, LPWSTR szMemberName, LPARAM *pLParam);

		// Dll
		TCHAR szBootDll[MAX_PATH];
		StringCchCopy(szBootDll, sizeof(szBootDll) / sizeof(*szBootDll), szOmeaFolder);
		PathAppend(szBootDll, _T("NetfxBootstrap.dll"));
		HMODULE hBoot = LoadLibrary(szBootDll);
		if(hBoot == NULL)
		{
			TCHAR szMessage[0x400];
			StringCchPrintf(szMessage, sizeof(szMessage) / sizeof(*szMessage), _T("Could not start the .NET Framework to run Omea. The required module is missing (%#010X)."), GetLastError());
			MessageBox(hwndInitialSplash, szMessage, _T("JetBrains Omea"), MB_OK | MB_ICONSTOP);
			throw (int)E_NOINTERFACE;
		}

		// Method
		InvokeStaticFromAssemblyNameDelegate InvokeStaticFromAssemblyName = (InvokeStaticFromAssemblyNameDelegate)GetProcAddress(hBoot, _T("InvokeStaticFromAssemblyName"));
		if(InvokeStaticFromAssemblyName == NULL)
		{
			TCHAR szMessage[0x400];
			StringCchPrintf(szMessage, sizeof(szMessage) / sizeof(*szMessage), _T("Could not start the .NET Framework to run Omea. The entry point could not be found (%#010X)."), GetLastError());
			MessageBox(hwndInitialSplash, szMessage, _T("JetBrains Omea"), MB_OK | MB_ICONSTOP);
			throw (int)E_NOINTERFACE;
		}

		// Apartment State
		CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);

		// Invoke
		HRESULT hRet = InvokeStaticFromAssemblyName(L"OmniaMea", L"JetBrains.Omea.MainFrame", L"Launch", (LPARAM*)&hwndInitialSplash);
		if(FAILED(hRet))
		{
			TCHAR szMessage[0x400];
			StringCchPrintf(szMessage, sizeof(szMessage) / sizeof(*szMessage), _T("Could not start the .NET Framework to run Omea. The operation has failed (%#010X)."), hRet);
			MessageBox(hwndInitialSplash, szMessage, _T("JetBrains Omea"), MB_OK | MB_ICONSTOP);
			throw (int)hRet;
		}
	}
};
