// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Specifies the drawing options [for WM_PRINT]. You can combine one or more of the following flags.
	/// </summary>
	[Flags]
	public enum PrintWindowFlags : uint
	{
		/// <summary>
		/// Draw the window only if it is visible.
		/// </summary>
		PRF_CHECKVISIBLE = 0x00000001,

		/// <summary>
		/// Draw the non-client area of the window.
		/// </summary>
		PRF_NONCLIENT = 0x00000002,

		/// <summary>
		/// Draw the client area of the window.
		/// </summary>
		PRF_CLIENT = 0x00000004,

		/// <summary>
		/// Erase the background before drawing the window.
		/// </summary>
		PRF_ERASEBKGND = 0x00000008,

		/// <summary>
		/// Draw all visible child windows.
		/// </summary>
		PRF_CHILDREN = 0x00000010,

		/// <summary>
		/// Draw all owned windows.
		/// </summary>
		PRF_OWNED = 0x00000020,
	}
}
