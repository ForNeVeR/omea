﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

//	WebHelp 5.10.001
// const strings
var gaProj = new Array();
var gsRoot = "";

function setRoot(sRoot)
{
	gsRoot = sRoot
}

function aPE(sProjPath, sRootPath)
{
	gaProj[gaProj.length] = new tocProjEntry(sProjPath, sRootPath);
}

function tocProjEntry(sProjPath, sRootPath)
{
	if(sProjPath.lastIndexOf("/")!=sProjPath.length-1)
		sProjPath+="/";
	this.sPPath = sProjPath;
	this.sRPath = sRootPath;
}


function window_OnLoad()
{
	if (parent && parent != this && parent.projReady) {
		parent.projReady(gsRoot, gaProj);
	}
}
window.onload = window_OnLoad;
