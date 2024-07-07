// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// The portions of the system to be included in the snapshot.
	/// See <see cref="Kernel32Dll.CreateToolhelp32Snapshot"/>.
	/// </summary>
	[Flags]
	public enum TH32CS : uint
	{
		TH32CS_SNAPHEAPLIST = 0x00000001,
		TH32CS_SNAPPROCESS = 0x00000002,
		TH32CS_SNAPTHREAD = 0x00000004,
		TH32CS_SNAPMODULE = 0x00000008,
		TH32CS_SNAPALL = TH32CS_SNAPHEAPLIST | TH32CS_SNAPPROCESS | TH32CS_SNAPTHREAD | TH32CS_SNAPMODULE,
		TH32CS_INHERIT = 0x80000000,
	}
}
