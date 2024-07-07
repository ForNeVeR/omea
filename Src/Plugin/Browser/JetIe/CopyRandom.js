// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

import System;
import System.IO;
import System.Diagnostics;

try
{
	if(Environment.GetCommandLineArgs().Length < 2)
		throw new Exception("Usage: exe <source>");

	var	fi : FileInfo = new FileInfo(Environment.GetCommandLineArgs()[1]);

	var	sNewName : System.String = fi.Directory + "\\" + fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length) + "." + DateTime.Now.ToString("s").Replace(':', '-') + fi.Extension;
	Console.WriteLine("copy \"{0}\" \"{1}\"", fi.FullName, sNewName);
	fi.CopyTo(sNewName);

	Console.WriteLine("regsvr32 /s /c \"{0}\"", sNewName);
	//Process.Start("regsvr32", "\"" + sNewName + "\"");
	Process.Start("regsvr32", "/s /c \"" + sNewName + "\"").WaitForExit();
}
catch(ex : Exception)
{
	Console.WriteLine(ex);
}
