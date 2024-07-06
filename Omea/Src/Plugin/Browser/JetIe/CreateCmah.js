// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// Generates a series of the CMAH html files. A design-time tool, should not be included into compilation
// The CMAH files enable for handling the Internet Explorer context menu commands dynamically and are included into the DLL resources.
// Upon being clicked, the context menu item cannot pass any parameters to the handler, which is an HTML page (the res: protocol seems to have no support for query/hash URI parameters), so there's a set of HTML pages, and each context menu item associates with its own HTML page. The only duty of that page is to pass the appropriate Menu ID to the action manager that converts the latter to the ActionID using a map between the menu items and actions, and then executes the action in question.

import System;
import System.IO;
import System.Text;

//print("CreateContextMenuActionHandlers.exe <Prefix for the ActionManager Dispatch Object with ExecuteContextMenuAction Method ProgId>");
//var sObject = Environment.GetCommandLineArgs()[1];

print("CreateContextMenuActionHandlers.exe");
for(var sProject in (String[](["IexploreOmea", "IexploreBeelaxy"])).GetEnumerator())
{
	print("Processing the ContextMenuActionHandler sequence for the " + sProject + " project.");
	CreateCmah(sProject, "./" + sProject + "/Res");
}
print("Done OK");

/// This function generates a set of CMAH files for a particular project, sObjectName is the first part of a ProgID for the ActionManager and PopupNotification objects for that project.
function CreateCmah(sObjectName, sPath)
{
	for(var a : int = 0; a < 16; a++)
	{
		var	sw = new StreamWriter(sPath + "/IDR_CMAH_" + (a + 1000) + ".html");
		sw.WriteLine("<script language=\"jscript\">");

		sw.WriteLine("try { new ActiveXObject(\"" + sObjectName + ".ActionManager\").ExecuteContextMenuAction(" + (a + 1000) + ", external.menuArguments.document); }");
		sw.WriteLine("catch(e)");
		sw.WriteLine("{ try { new ActiveXObject(\"" + sObjectName + ".PopupNotification\").Show(\"The operation could not be completed.\\n\" + e.message, \"\", \"Stop\"); } catch(e){} }");
		sw.WriteLine("</script>");
		sw.Close();
	}
}
