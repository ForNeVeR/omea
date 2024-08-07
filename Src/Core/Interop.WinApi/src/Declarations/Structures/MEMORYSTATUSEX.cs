﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;

using JetBrains.Util;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Helper structure for the <see cref="Kernel32Dll.GlobalMemoryStatusEx"/> function.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	[NoReorder]
	public struct MEMORYSTATUSEX
	{
		public UInt32 dwLength;

		public UInt32 dwMemoryLoad;

		public UInt64 ullTotalPhys;

		public UInt64 ullAvailPhys;

		public UInt64 ullTotalPageFile;

		public UInt64 ullAvailPageFile;

		public UInt64 ullTotalVirtual;

		public UInt64 ullAvailVirtual;

		public UInt64 ullAvailExtendedVirtual;

		public void SetSize()
		{
			dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
		}
	}
}
