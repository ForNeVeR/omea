/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

function omeaconnector_visitHomePage()
{
  const newTab = window.opener.getBrowser().addTab("@URL@");
  window.opener.getBrowser().selectedTab = newTab;
  window.close();
}
