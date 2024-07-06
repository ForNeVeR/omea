// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// WM_ACTIVATE wParam loword value.
	/// </summary>
	public enum WmActivateCodes : uint
	{
		/// <summary>
		/// Deactivated.
		/// </summary>
		Inactive = 0,
		/// <summary>
		/// Activated by some method other than a mouse click (for example, by a call to the <see cref="User32Dll.SetActiveWindow"/> function or by use of the keyboard interface to select the window).
		/// </summary>
		Active = 1,
		/// <summary>
		/// Activated by a mouse click.
		/// </summary>
		ClickActive = 2,
	}
}
