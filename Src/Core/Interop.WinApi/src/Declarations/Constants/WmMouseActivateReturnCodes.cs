// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// <see cref="WindowsMessages.WM_MOUSEACTIVATE"/> return codes.
	/// </summary>
	public enum WmMouseActivateReturnCodes
	{
		MA_ACTIVATE = 1,
		MA_ACTIVATEANDEAT = 2,
		MA_NOACTIVATE = 3,
		MA_NOACTIVATEANDEAT = 4,
	}
}
