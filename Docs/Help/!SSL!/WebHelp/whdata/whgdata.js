// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

//	WebHelp 5.10.001
var gIEA = new Array();
function aGE(sName, sDef)
{
	var len = gIEA.length;
	gIEA[len] = new gloEntry(sName, sDef);
}

function gloEntry(sName, sDef)
{
	this.sName = sName;
	this.sDef = sDef;
	this.nNKOff = 0;
}

function window_OnLoad()
{
	if (parent && parent != this) {
		if (parent.putData)
		{
			parent.putData(gIEA);
		}
	}
}

window.onload = window_OnLoad;
