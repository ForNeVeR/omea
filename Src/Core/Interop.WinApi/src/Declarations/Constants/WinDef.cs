// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Assorted constants.
	/// </summary>
	public unsafe class WinDef
	{
		#region Data

		public const int MAX_PATH = 260;

		public static readonly void* INVALID_HANDLE_VALUE = (void*)(IntPtr)(-1);

		/// <summary>
		/// Maximum number of wait objects.
		/// </summary>
		public static readonly int MAXIMUM_WAIT_OBJECTS = 64;

		#endregion
	}
}
