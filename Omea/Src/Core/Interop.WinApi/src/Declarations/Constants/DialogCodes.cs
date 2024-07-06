// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Dialog Codes.
	/// </summary>
	public enum DialogCodes
	{
		DLGC_WANTARROWS = 0x0001, /* Control wants arrow keys         */
		DLGC_WANTTAB = 0x0002, /* Control wants tab keys           */
		DLGC_WANTALLKEYS = 0x0004, /* Control wants all keys           */
		DLGC_WANTMESSAGE = 0x0004, /* Pass message to control          */
		DLGC_HASSETSEL = 0x0008, /* Understands EM_SETSEL message    */
		DLGC_DEFPUSHBUTTON = 0x0010, /* Default pushbutton               */
		DLGC_UNDEFPUSHBUTTON = 0x0020, /* Non-default pushbutton           */
		DLGC_RADIOBUTTON = 0x0040, /* Radio button                     */
		DLGC_WANTCHARS = 0x0080, /* Want WM_CHAR messages            */
		DLGC_STATIC = 0x0100, /* Static item: don't include       */
		DLGC_BUTTON = 0x2000, /* Button item: can be checked      */
	}
}
