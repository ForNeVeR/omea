/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Runtime.InteropServices;

using JetBrains.Util;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// The RECT structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
	/// By convention, the right and bottom edges of the rectangle are normally considered exclusive. In other words, the pixel whose coordinates are (right, bottom) lies immediately outside of the the rectangle. For example, when RECT is passed to the FillRect function, the rectangle is filled up to, but not including, the right column and bottom row of pixels. This structure is identical to the RECTL structure.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	[NoReorder]
	public struct RECT
	{
		public Int32 left;

		public Int32 top;

		public Int32 right;

		public Int32 bottom;

		public RECT(int left, int top, int right, int bottom)
		{
			this.left = left;
			this.top = top;
			this.right = right;
			this.bottom = bottom;
		}

		public RECT(Rectangle rect)
			: this(rect.Left, rect.Top, rect.Right, rect.Bottom)
		{
		}

		public int Width
		{
			get
			{
				return right - left;
			}
		}

		public int Height
		{
			get
			{
				return bottom - top;
			}
		}

		public static RECT Empty = new RECT(0, 0, 0, 0);

		public static implicit operator Rectangle(RECT rc)
		{
			return new Rectangle(new Point(rc.left, rc.top), new Size(rc.Width, rc.Height));
		}

		public static implicit operator RECT(Rectangle rect)
		{
			return new RECT(rect);
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
			return ((Rectangle)this).ToString();
		}
	}
}