// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

function omeaconnector_visitHomePage()
{
  const newTab = window.opener.getBrowser().addTab("@URL@");
  window.opener.getBrowser().selectedTab = newTab;
  window.close();
}
