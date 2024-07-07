// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Interop.WinApi
{
	[Flags]
	public enum MsgWaitForMultipleObjectsFlags : uint
	{
		MWMO_WAITALL = 0x0001,
		MWMO_ALERTABLE = 0x0002,
		MWMO_INPUTAVAILABLE = 0x0004,
	}
}
