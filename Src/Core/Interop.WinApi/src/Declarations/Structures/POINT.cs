// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Runtime.InteropServices;

using JetBrains.Util;

namespace JetBrains.Interop.WinApi
{
	[StructLayout(LayoutKind.Sequential)]
	[NoReorder]
	public struct POINT
	{
		public POINT(Point p)
		{
			x = p.X;
			y = p.Y;
		}

		public POINT(Int32 X, Int32 Y)
		{
			x = X;
			y = Y;
		}

		public POINT(Int32 dw)
		{
			x = dw & 0xFFFF;
			y = (dw >> 16) & 0xFFFF;
		}

		/// <summary>
		/// Creates a new point, unpacking its signed coordinates from an LPARAM, using the <see cref="Macros.GET_X_LPARAM"/> and <see cref="Macros.GET_Y_LPARAM"/> functions.
		/// </summary>
		public POINT(IntPtr lParam)
		{
			x = Macros.GET_X_LPARAM(lParam);
			y = Macros.GET_Y_LPARAM(lParam);
		}

		public Int32 x;

		public Int32 y;

		public static readonly POINT Empty = new POINT(0, 0);

		public override bool Equals(object obj)
		{
			if(!(obj is POINT))
				return false;
			var point = (POINT)obj;
			return x == point.x && y == point.y;
		}

		public override int GetHashCode()
		{
			return x + 29 * y;
		}

		///<summary>
		///Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		///</summary>
		///
		///<returns>
		///A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		///</returns>
		///<filterpriority>2</filterpriority>
		public override string ToString()
		{
			return ((Point)this).ToString();
		}

		public static implicit operator Point(POINT other)
		{
			return new Point(other.x, other.y);
		}

		public static implicit operator POINT(Point other)
		{
			return new POINT(other);
		}

		public static explicit operator POINT(IntPtr lParam)
		{
			return new POINT(lParam);
		}

		public static bool operator ==(POINT one, POINT two)
		{
			return (one.x == two.x) && (one.y == two.y);
		}

		public static bool operator !=(POINT one, POINT two)
		{
			return !((one.x == two.x) && (one.y == two.y));
		}

		public static explicit operator IntPtr(POINT point)
		{
			var ix = unchecked((short)point.x);
			var iy = unchecked((short)point.y);

			var ux = unchecked((ushort)ix);
			var uy = unchecked((ushort)iy);

			return (IntPtr)((((long)uy) << 16) | ux);
		}
	}
}
