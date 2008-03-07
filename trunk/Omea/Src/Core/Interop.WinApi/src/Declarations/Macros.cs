/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Hosts the scattered WinAPI macros, in the form of functions.
	/// </summary>
	public static class Macros
	{
		public static ushort LOWORD(uint l)
		{
			return (ushort)(l & 0xffff);
		}

		public static ushort HIWORD(uint l)
		{
			return (ushort)(l >> 16);
		}

		public static ushort LOWORD(IntPtr l)
		{
			return unchecked((ushort)((long)l & 0xFFFF));
		}

		public static ushort HIWORD(IntPtr l)
		{
			return unchecked((ushort)(((long)l >> 16) & 0xFFFF));
		}

		/// <summary>
		/// Gets a signed x-coordinate packed into an LPARAM, usually in Windows messgaes.
		/// To create a point from an LPARAM, use casting thru the <see cref="POINT"/> class.
		/// </summary>
		public static int GET_X_LPARAM(IntPtr lParam)
		{
			return unchecked((short)LOWORD(lParam));
		}

		/// <summary>
		/// Gets a signed y-coordinate packed into an LPARAM, usually in Windows messgaes.
		/// To create a point from an LPARAM, use casting thru the <see cref="POINT"/> class.
		/// </summary>
		public static int GET_Y_LPARAM(IntPtr lParam)
		{
			return unchecked((short)HIWORD(lParam));
		}
		
	}
}