// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Interop.WinApi
{
	[Flags]
	public enum SetLayeredWindowAttributesFlags : uint
	{
		LWA_COLORKEY = 1,
		LWA_ALPHA = 2,
	}
}
