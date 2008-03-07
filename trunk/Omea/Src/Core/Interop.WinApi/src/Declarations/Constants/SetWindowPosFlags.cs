/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Flags for the <see cref="Win32Declarations.SetWindowPos"/> fucntion.
	/// </summary>
	[Flags]
	public enum SetWindowPosFlags : uint
	{
		/// <summary>
		/// Retains the current size (ignores the cx and cy parameters).
		/// </summary>
		SWP_NOSIZE = 0x0001,
		/// <summary>
		/// Retains the current position (ignores X and Y parameters).
		/// </summary>
		SWP_NOMOVE = 0x0002,
		/// <summary>
		/// Retains the current Z order (ignores the hWndInsertAfter parameter).
		/// </summary>
		SWP_NOZORDER = 0x0004,
		/// <summary>
		/// Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any parts of the window and parent window that need redrawing.
		/// </summary>
		SWP_NOREDRAW = 0x0008,
		/// <summary>
		/// Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
		/// </summary>
		SWP_NOACTIVATE = 0x0010,
		/// <summary>
		/// Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
		/// </summary>
		SWP_FRAMECHANGED = 0x0020, /* The frame changed: send WM_NCCALCSIZE */
		/// <summary>
		/// Displays the window.
		/// </summary>
		SWP_SHOWWINDOW = 0x0040,
		/// <summary>
		/// Hides the window.
		/// </summary>
		SWP_HIDEWINDOW = 0x0080,
		/// <summary>
		/// Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client area are saved and copied back into the client area after the window is sized or repositioned.
		/// </summary>
		SWP_NOCOPYBITS = 0x0100,
		/// <summary>
		/// Does not change the owner window's position in the Z order.
		/// </summary>
		SWP_NOOWNERZORDER = 0x0200, /* Don't do owner Z ordering */
		/// <summary>
		/// Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
		/// </summary>
		SWP_NOSENDCHANGING = 0x0400, /* Don't send WM_WINDOWPOSCHANGING */
		/// <summary>
		/// Prevents generation of the WM_SYNCPAINT message.
		/// </summary>
		SWP_DEFERERASE = 0x2000,
		/// <summary>
		/// If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window. This prevents the calling thread from blocking its execution while other threads process the request. 
		/// </summary>
		SWP_ASYNCWINDOWPOS = 0x4000,
		/// <summary>
		/// Same as the <see cref="SWP_NOOWNERZORDER"/> flag.
		/// </summary>
		SWP_NOREPOSITION = SWP_NOOWNERZORDER,
		/// <summary>
		/// Draws a frame (defined in the window's class description) around the window.
		/// </summary>
		SWP_DRAWFRAME = SWP_FRAMECHANGED,
	}
}