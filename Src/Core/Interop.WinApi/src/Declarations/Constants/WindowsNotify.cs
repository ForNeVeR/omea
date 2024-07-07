// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Interop.WinApi
{
	public enum WindowsNotify
	{
		CDN_FIRST = 0 - 601,
		CDN_LAST = 0 - 699,
		CDN_INITDONE = CDN_FIRST - 0x0000,
		CDN_SELCHANGE = CDN_FIRST - 0x0001,
		CDN_FOLDERCHANGE = CDN_FIRST - 0x0002,
		CDN_SHAREVIOLATION = CDN_FIRST - 0x0003,
		CDN_HELP = CDN_FIRST - 0x0004,
		CDN_FILEOK = CDN_FIRST - 0x0005,
		CDN_TYPECHANGE = CDN_FIRST - 0x0006,
		CDN_INCLUDEITEM = CDN_FIRST - 0x0007,
	}
}
