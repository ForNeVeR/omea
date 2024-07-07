// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// CJetIe — Contains the shared services for JetBrains Internet Explorer plugins.
// Static members are safe for multithreaded operations.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once

#include "CommonResource.h"

#include "Wrappers.h"
#include "JetIeException.h"
#include "PopupNotification.h"
#include "ActionManager.h"

class CJetIe
{
public:
	CJetIe();
	~CJetIe();

// Operations
public:
	/// Converts an integer value in range [0..F] to a corresponding hexadecimal character, ['0'..'9', 'A'..'F'].
	static char ToHexChar(char c);

	/// Converts a hexadecimal character ['0'..'9', 'A'..'F', 'a'..'f'] to a corresponding integer value in range of [0..F].
	static char FromHexChar(char c);

	/// Encodes an URI so that it contains only allowed characters for the uri-encoded transfer. Note that you must not encode the already-encoded string because this will require a corresponding number of decodings.
	static CStringA UrlEncode(CStringW sSource);

	/// Reverts the UrlEncode operation by unescaping the url-encoded data into its original form.
	static CStringW UrlDecode(CStringA sSource);

	/// Returns a pathname of the DLL this module was loaded from.
	static CString GetModuleFileName();

	/// Returns an instance handle of the DLL this module was loaded from.
	static HINSTANCE GetModuleInstanceHandle();

	/// Gets the HWND of a Browser window for use in the WinAPI functions.
	static HWND WindowFromBrowser(SHDocVw::IWebBrowser2Ptr &oBrowser);

	/// Loads a Win32 resource into a COM stream.
	static void SerializeResource(LPCTSTR szResType, LPCTSTR szResName, IStreamPtr oStream, bool bRewindStream /*= true*/) throw(_com_error);

	/// Loads a string resource and returns it as a T-string.
	__declspec(nothrow) static CString LoadStringT(UINT nID);

	/// Loads a string resource and returns it as an Unicode string.
	__declspec(nothrow) static CStringW LoadString(UINT nID);

	/// Creates an XML document. First, it attempts to create the DOMDocument.4.0 object (as the highest known to this impl), then 3.0 and so on.
	/// The VersionIndependentProgID cannot be just applied because it won't point to the most recent version due to IE compatibility restrictions. If IE was equipped with MSXML3, it will forever point to 3.0 regardless of what MSXML version you install.
	static XmlDocument CreateXmlDocument() throw(_com_error);

	/// Tells whether the plugin is being executed under Windows NT.
	__declspec(nothrow) static bool IsWinNT();

	/// Converts a system error to a human-readable string representation.
	__declspec(nothrow) static CString GetSystemErrorT(DWORD dwError = GetLastError());

	/// Converts a system error to a human-readable string representation.
	__declspec(nothrow) static CStringW GetSystemError(DWORD dwError = GetLastError());

	/// Displays a popup alert in response to some user's action. Note that this function would not throw a COM exception.
	__declspec(nothrow) static void ShowPopupNotification(LPCWSTR sMessage, LPCWSTR sTitle = NULL, CPopupNotification::Mood mood = CPopupNotification::pmNotify);

	/// Applies an MD5 hash to the string and returns it as a hex string.
	static CStringW HashMd5(CStringW sData) throw(_com_error);

	/// Returns the path-name to the folder that contains the plugin's data files, either a local or roaming, without a trailing backslash. Usually that would be %LOCALAPPDATA%/JetBrains/<PluginName> for local and %APPDATA%/JetBrains/<PluginName> for roaming folder.
	static CString GetDataFolderPathName(bool bLocal) throw(_com_error);

	/// Returns the ActionManager object instance that is suitable for receiving calls from the current thread.
	static IRawActionManagerPtr GetActionManager() throw(_com_error);

	/// Takes in a VARIANT that may contain an IOleWindow object or HWND as an integer (incl. NULL) and returns the extracted value as an HWND (or NULL if the pointer passed in is NULL).
	/// In case if the VARIANT represents a missing value, a NULL is returned.
	static HWND WindowFromVariant(_variant_t vtWindow) throw(_com_error);

	/// Takes in a VARIANT that represents a boolean value, or can be coerced to boolean, or is missing (in which case False value is assumed), and returns it the C++ bool value.
	static bool BooleanFromVariant(_variant_t vtBoolean, bool bDefault = false) throw(_com_error);

	/// Opens the URL specified in a new browser window.
	/// Uses an existing browser object to do it if available, which provides that the window will be opened in the same multibrowser environment, if any.
	/// Warning! This function is totally thread-unsafe.
	static void OpenInNewWindow(CStringW sUrl, SHDocVw::IWebBrowser2Ptr oBrowser = NULL) throw(_com_error);

	/// Retrieves the selected text from a document, be it with frames or not.
	/// Throws an exception on a fatal error.
	/// Returns whether there was selection or not.
	static bool GetSelectedText(MSHTMLLite::IHTMLDocument2Ptr oDoc, CStringW *psText, CStringW *psHtml) throw(_com_error);

	/// Returns the JETIE DLL version in the Major.Minor.Build.Revision decimal format, in a string.
	/// Optionally returns the same in a 64-bit number (each field is considered to be 16bit), where the most significant version component goes to the most significant place in the number.
	static CStringW GetDllVersion(__int64 *pMyVersion = NULL) throw(_com_error);

	/// Shows a trusted HTML dialog, loading the content from a given URL. For the dialog styles available, see the IHTMLWindow*::ShowHtmlDialog in MSDN. The vtArguments value will be available as a window.dialogArguments parameter from the page. The bNonModal parameter is just a wish and not guaranteed to succeed.
	static void ShowHtmlDialog(HWND hwndParent, CStringW sUrl, CStringW sDialogStyle, _variant_t &vtArguments, bool bNonModal = true) throw(_com_error);

// Data
protected:
	/// Handle to the DDE session. Must be zero at startup and nonzero after DDE is successfully initialized.
	static DWORD	m_dwDdeInstance;	// TODO: remove as it seems to be unused

// Implementation
protected:
	/// Dummy function that servers as a DDE callback — we don't really need one as we're client-only, but we need it at registration-time.
	static HDDEDATA CALLBACK DdeCallback(UINT uType, UINT uFmt, HCONV hconv, HDDEDATA hsz1, HDDEDATA hsz2, HDDEDATA hdata, HDDEDATA dwData1, HDDEDATA dwData2);
};
