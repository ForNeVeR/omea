/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Key State Masks for Mouse Messages.
	/// </summary>
	public enum KeyStateMasks
	{
		MK_LBUTTON = 0x0001,
		MK_RBUTTON = 0x0002,
		MK_SHIFT = 0x0004,
		MK_CONTROL = 0x0008,
		MK_MBUTTON = 0x0010,
		MK_XBUTTON1 = 0x0020,
		MK_XBUTTON2 = 0x0040,
	}
}