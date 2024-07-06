// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
