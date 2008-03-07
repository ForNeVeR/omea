/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Runtime.InteropServices;

namespace JetBrains.Interop.WinApi
{
	[StructLayout(LayoutKind.Sequential)]
	public struct HDITEM
	{
		public HeaderItemMask mask;
		public int cxy;
		public IntPtr pszText;
		public IntPtr hbm;
		public int cchTextMax;
		public HeaderItemFlag fmt;
		public IntPtr lParam;
		public int iImage;
		public int iOrder;
		public uint type;
		public IntPtr pvFilter;
	}
}