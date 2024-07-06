// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Flags for the <see cref="Win32Declarations.GetAncestor"/> function.
	/// </summary>
	public enum GetAncestorFlags : uint
	{
		/// <summary>
		/// Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
		/// </summary>
		GA_PARENT = 1,

		/// <summary>
		/// Retrieves the root window by walking the chain of parent windows.
		/// </summary>
		GA_ROOT = 2,

		/// <summary>
		/// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
		/// </summary>
		GA_ROOTOWNER = 3,
	}
}
