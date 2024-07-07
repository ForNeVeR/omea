// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// stdafx.cpp : source file that includes just the standard includes
// %ProjectName%.pch will be the pre-compiled header
// stdafx.obj will contain the pre-compiled type information

#include "stdafx.h"

#pragma comment(lib, "WinINet.lib")
#pragma comment(lib, "comctl32.lib")
#pragma comment(lib, "version.lib")

#if(defined(_TRACE))

#define TRACE_PREFIX L"[JETIE] "
#define TRACE_PREFIX_LENGTH	(sizeof(TRACE_PREFIX) / sizeof(*TRACE_PREFIX) - 1)	// Without the trailing zero

void DirectTrace(LPCWSTR pstrFormat, ...)
{
	// Format the string to incorporate the passed-in arguments
	va_list args;
	va_start(args, pstrFormat);
	WCHAR	szFormat[0x1000];
	bool	bUnfit = StringCchVPrintfW(szFormat, sizeof(szFormat) / sizeof(*szFormat), pstrFormat, args) == STRSAFE_E_INSUFFICIENT_BUFFER;
	va_end(args);

	// Decorate the string by adding the app tag and trailing CRLF
	WCHAR	szDecorated[0x1000] = TRACE_PREFIX;	// Start from the prefix
	int	nDecorated = TRACE_PREFIX_LENGTH;	// Position in the decorated string
	bool	bNewLine = false;
	for(int a = 0; (szFormat[a]) && (nDecorated < sizeof(szDecorated) / sizeof(*szDecorated)); a++)
	{
		if((szFormat[a] == L'\n') || (szFormat[a] == L'\r'))
			bNewLine = true;
		else
		{
			// If the line is over, append the trailing CRLF and submit the line
			if(bNewLine)
			{
				// Trail
				if(nDecorated < sizeof(szDecorated) / sizeof(*szDecorated))
					szDecorated[nDecorated++] = L'\r';
				if(nDecorated < sizeof(szDecorated) / sizeof(*szDecorated))
					szDecorated[nDecorated++] = L'\n';
				szDecorated[nDecorated < sizeof(szDecorated) / sizeof(*szDecorated) ? nDecorated : sizeof(szDecorated) / sizeof(*szDecorated) - 1] = 0;	// Ensure there's a terminator

				bUnfit |= nDecorated >= sizeof(szDecorated) / sizeof(*szDecorated);	// Has it fit the buffer?

				// Send
#ifdef _UNICODE
				OutputDebugStringW(szDecorated);
#else
				char	szAnsi[0x1000] = "";
				WideCharToMultiByte(CP_ACP, 0, szDecorated, nDecorated, szAnsi, sizeof(szAnsi) / sizeof(*szAnsi), NULL, NULL);
				OutputDebugStringA(szAnsi);
#endif

				// Reset
				StringCchCopyW(szDecorated, sizeof(szDecorated) / sizeof(*szDecorated), TRACE_PREFIX);
				nDecorated = TRACE_PREFIX_LENGTH;
			}

			// Append the normal char
			if(nDecorated < sizeof(szDecorated) / sizeof(*szDecorated))
				szDecorated[nDecorated++] = szFormat[a];

			bNewLine = false;
		}
	}

	// Send the last line, if any
	if(nDecorated > TRACE_PREFIX_LENGTH)
	{
		// Trail
		if(nDecorated < sizeof(szDecorated) / sizeof(*szDecorated))
			szDecorated[nDecorated++] = L'\r';
		if(nDecorated < sizeof(szDecorated) / sizeof(*szDecorated))
			szDecorated[nDecorated++] = L'\n';
		szDecorated[nDecorated < sizeof(szDecorated) / sizeof(*szDecorated) ? nDecorated : sizeof(szDecorated) / sizeof(*szDecorated) - 1] = 0;	// Ensure there's a terminator

		bUnfit |= nDecorated >= sizeof(szDecorated) / sizeof(*szDecorated);	// Has it fit the buffer?

		// Send
#ifdef _UNICODE
		OutputDebugStringW(szDecorated);
#else
		char	szAnsi[0x1000] = "";
		WideCharToMultiByte(CP_ACP, 0, szDecorated, nDecorated, szAnsi, sizeof(szAnsi) / sizeof(*szAnsi), NULL, NULL);
		OutputDebugStringA(szAnsi);
#endif
	}

	// Warn if some info was dropped
	if(bUnfit)
		OutputDebugString(_T("[JETIE] The previous trace did not fit the formatting buffer.\r\n"));
}
#endif	//(defined(_TRACE))
