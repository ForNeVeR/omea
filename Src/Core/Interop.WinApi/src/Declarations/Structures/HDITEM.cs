// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
