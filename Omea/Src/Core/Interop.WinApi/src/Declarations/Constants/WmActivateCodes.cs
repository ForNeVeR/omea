/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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