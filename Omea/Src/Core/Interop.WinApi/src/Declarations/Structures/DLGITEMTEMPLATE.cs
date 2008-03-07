/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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