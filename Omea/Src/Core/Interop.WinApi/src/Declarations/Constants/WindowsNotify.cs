/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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