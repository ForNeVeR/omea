// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// <see cref="WindowsMessages.EM_SETMARGINS"/> constants.
	/// </summary>
	public enum EditBoxControlMargins : short
	{
		/// <summary>
		/// Sets the left margin.
		/// </summary>
		EC_LEFTMARGIN = 0x0001,
		/// <summary>
		/// Sets the right margin.
		/// </summary>
		EC_RIGHTMARGIN = 0x0002,
		/// <summary>
		/// Rich edit controls: Sets the left and right margins to a narrow width calculated using the text metrics of the control's current font. If no font has been set for the control, the margins are set to zero. The lParam parameter is ignored. Edit controls: The EC_USEFONTINFO value cannot be used in the wParam parameter. It can only be used in the lParam parameter.
		/// </summary>
		EC_USEFONTINFO = unchecked((short)0xffff),
	}
}
