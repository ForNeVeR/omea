// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Pen Styles.
	/// </summary>
	public enum PenStyles // int
	{
		PS_SOLID = 0,
		PS_DASH = 1, /* -------  */
		PS_DOT = 2, /* .......  */
		PS_DASHDOT = 3, /* _._._._  */
		PS_DASHDOTDOT = 4, /* _.._.._  */
		PS_NULL = 5,
		PS_INSIDEFRAME = 6,
		PS_USERSTYLE = 7,
		PS_ALTERNATE = 8,
		PS_STYLE_MASK = 0x0000000F,

		PS_ENDCAP_ROUND = 0x00000000,
		PS_ENDCAP_SQUARE = 0x00000100,
		PS_ENDCAP_FLAT = 0x00000200,
		PS_ENDCAP_MASK = 0x00000F00,

		PS_JOIN_ROUND = 0x00000000,
		PS_JOIN_BEVEL = 0x00001000,
		PS_JOIN_MITER = 0x00002000,
		PS_JOIN_MASK = 0x0000F000,

		PS_COSMETIC = 0x00000000,
		PS_GEOMETRIC = 0x00010000,
		PS_TYPE_MASK = 0x000F0000,
	}
}
