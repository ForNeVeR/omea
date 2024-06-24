// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;

namespace JetBrains.Interop.WinApi.Interfaces
{
	[ComImport]
	[ComVisible(false)]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("00000001-0000-0000-C000-000000000046")]
	public interface IClassFactory
	{
		void CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, [MarshalAs(UnmanagedType.Interface)] [Out] out object ppvObject);

		void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
	}
}
