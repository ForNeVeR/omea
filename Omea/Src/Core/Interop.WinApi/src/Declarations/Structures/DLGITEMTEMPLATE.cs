// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;

namespace JetBrains.Interop.WinApi
{
	[StructLayout(LayoutKind.Sequential)]
	public struct DLGITEMTEMPLATE
	{
		public WindowStyles style;
		public WindowExStyles dwExtendedStyle;
		public Int16 x;
		public Int16 y;
		public Int16 cx;
		public Int16 cy;
		public UInt16 id;
		public UInt16 atom;
		public UInt16 _static;
		public UInt16 _r1;
		public UInt16 _r2;
		public UInt16 _r3;
	}
}
