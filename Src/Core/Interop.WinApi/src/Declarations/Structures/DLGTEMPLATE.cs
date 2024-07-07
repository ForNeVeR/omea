// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;

namespace JetBrains.Interop.WinApi
{
	[StructLayout(LayoutKind.Sequential)]
	public struct DLGTEMPLATE
	{
		public WindowStyles style;
		public WindowExStyles dwExtendedStyle;
		public UInt16 cdit;
		public Int16 x;
		public Int16 y;
		public Int16 cx;
		public Int16 cy;
		public UInt16 menu;
		public UInt16 dialog;
		public UInt16 font;
		public DLGITEMTEMPLATE pannel;
	}
}
