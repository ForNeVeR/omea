/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// Launcher.cpp : Defines the entry point for the application.
//

#include "stdafx.h"
#include "Launcher.h"

int APIENTRY _tWinMain(HINSTANCE hInstance,
					   HINSTANCE /*hPrevInstance*/,
					   LPTSTR    lpCmdLine,
					   int       nCmdShow)
{
	return CLauncher::Run(hInstance, lpCmdLine, nCmdShow);
}
