﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Interop.WinApi
{
	[Flags]
	public enum QueueStatusFlags:uint
	{
		QS_KEY = 0x0001,
		QS_MOUSEMOVE = 0x0002,
		QS_MOUSEBUTTON = 0x0004,
		QS_POSTMESSAGE = 0x0008,
		QS_TIMER = 0x0010,
		QS_PAINT = 0x0020,
		QS_SENDMESSAGE = 0x0040,
		QS_HOTKEY = 0x0080,
		QS_ALLPOSTMESSAGE = 0x0100,
		QS_RAWINPUT = 0x0400,

		QS_MOUSE = (QS_MOUSEMOVE | QS_MOUSEBUTTON),
		QS_INPUT = (QS_MOUSE | QS_KEY | QS_RAWINPUT),
		QS_ALLEVENTS = (QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY),
		QS_ALLINPUT = (QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE),
	}
}