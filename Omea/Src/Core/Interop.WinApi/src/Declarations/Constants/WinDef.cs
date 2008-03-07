/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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