// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Interop.WinApi
{
	[Flags]
	public enum HeaderItemFlag
	{
		HDF_LEFT = 0x0000,
		HDF_RIGHT = 0x0001,
		HDF_CENTER = 0x0002,
		HDF_JUSTIFYMASK = 0x0003,
		HDF_RTLREADING = 0x0004,
		HDF_OWNERDRAW = 0x8000,
		HDF_STRING = 0x4000,
		HDF_BITMAP = 0x2000,
		HDF_BITMAP_ON_RIGHT = 0x1000,
		HDF_IMAGE = 0x0800,
		HDF_SORTUP = 0x0400,
		HDF_SORTDOWN = 0x0200,
	}
}
