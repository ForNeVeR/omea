/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

//	WebHelp 5.10.001
var gWEA = new Array();
function aWE()
{
	var len = gWEA.length;
	gWEA[len] = new ftsEntry(aWE.arguments);
}

function ftsEntry(fn_arguments) 
{
	if (fn_arguments.length && fn_arguments.length >= 1) 
	{
		this.sItemName = fn_arguments[0];
		this.aTopics = null;
		var nLen = fn_arguments.length;
		if (nLen > 1) 
		{
			this.aTopics = new Array();
			for (var i = 0; i < nLen - 1; i ++ )
			{
				this.aTopics[i] = fn_arguments[i + 1];
			}
		}
	}
}

function window_OnLoad()
{
	if (parent && parent != this) {
		if (parent.putFtsWData) 
		{
			parent.putFtsWData(gWEA);
		}
	}
}

window.onload = window_OnLoad;