// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;

namespace JetBrains.Interop.WinApi
{
	[StructLayout(LayoutKind.Sequential)]
	public struct OPENFILENAME
	{
		public UInt32 lStructSize;
		public IntPtr hwndOwner;
		public IntPtr hInstance;
		public IntPtr lpstrFilter;
		public IntPtr lpstrCustomFilter;
		public UInt32 nMaxCustFilter;
		public UInt32 nFilterIndex;
		public IntPtr lpstrFile;
		public UInt32 nMaxFile;
		public IntPtr lpstrFileTitle;
		public UInt32 nMaxFileTitle;
		public IntPtr lpstrInitialDir;
		public IntPtr lpstrTitle;
		public OFN Flags;
		public UInt16 nFileOffset;
		public UInt16 nFileExtension;
		public IntPtr lpstrDefExt;
		public IntPtr lCustData;
		public CallBack lpfnHook;
		public IntPtr lpTemplateName;
	}

	public delegate UIntPtr CallBack(IntPtr hwnd, WindowsMessages msg, IntPtr wParam, IntPtr lParam);
}
