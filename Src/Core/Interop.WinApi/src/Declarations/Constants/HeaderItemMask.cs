// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Interop.WinApi
{
	[Flags]
	public enum HeaderItemMask : uint
	{
		HDI_WIDTH = 0x0001,
		HDI_HEIGHT = HDI_WIDTH,
		HDI_TEXT = 0x0002,
		HDI_FORMAT = 0x0004,
		HDI_LPARAM = 0x0008,
		HDI_BITMAP = 0x0010,
		HDI_IMAGE = 0x0020,
		HDI_DI_SETITEM = 0x0040,
		HDI_ORDER = 0x0080,
		HDI_FILTER = 0x0100,
	}
}
