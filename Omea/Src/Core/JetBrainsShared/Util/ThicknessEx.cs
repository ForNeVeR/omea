// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows;

namespace JetBrains.Util
{
	public static class ThicknessEx
	{
		#region Operations

		public static Thickness Add(this Thickness one, Thickness two)
		{
			return new Thickness(one.Left + two.Left, one.Top + two.Top, one.Right + two.Right, one.Bottom + two.Bottom);
		}

		public static Thickness Add(this Thickness one, double left, double top, double right, double bottom)
		{
			return new Thickness(one.Left + left, one.Top + top, one.Right + right, one.Bottom + bottom);
		}

		#endregion
	}
}
