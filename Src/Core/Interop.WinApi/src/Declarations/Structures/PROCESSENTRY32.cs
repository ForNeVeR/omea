// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;

using JetBrains.Util;

namespace JetBrains.Interop.WinApi
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	[NoReorder]
	public struct PROCESSENTRY32
	{
		public UInt32 dwSize;

		public UInt32 cntUsage;

		public UInt32 th32ProcessID; // this process

		public IntPtr th32DefaultHeapID;

		public UInt32 th32ModuleID; // associated exe

		public UInt32 cntThreads;

		public UInt32 th32ParentProcessID; // this process's parent process

		public Int32 pcPriClassBase; // Base priority of process's threads

		public UInt32 dwFlags;

		public unsafe fixed Char szExeFile [WinDef.MAX_PATH]; // Path

		public static PROCESSENTRY32 NewWithSize()
		{
			return new PROCESSENTRY32 {dwSize = ((uint)Marshal.SizeOf(typeof(PROCESSENTRY32)))};
		}
	}
}
