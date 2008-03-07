/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Flags for the <see cref="Win32Declarations.ScrollWindowEx(IntPtr,int,int,ref RECT,ref RECT,IntPtr,IntPtr,ScrollWindowFlags)"/>, <see cref="Win32Declarations.ScrollWindowEx(IntPtr,int,int,ref RECT,ref RECT,IntPtr,ref RECT,UI.Interop.ScrollWindowFlags)"/> functions.
	/// </summary>
	[Flags]
	public enum ScrollWindowFlags : uint
	{
		/// <summary>
		/// Erases the newly invalidated region by sending a WM_ERASEBKGND message to the window when specified with the SW_INVALIDATE flag.
		/// </summary>
		SW_ERASE,
		/// <summary>
		/// Invalidates the region identified by the hrgnUpdate parameter after scrolling.
		/// </summary>
		SW_INVALIDATE,
		/// <summary>
		/// Scrolls all child windows that intersect the rectangle pointed to by the prcScroll parameter. The child windows are scrolled by the number of pixels specified by the dx and dy parameters. The system sends a WM_MOVE message to all child windows that intersect the prcScroll rectangle, even if they do not move.
		/// </summary>
		SW_SCROLLCHILDREN,
		/// <summary>
		/// Windows 98/Me, Windows 2000/XP: Scrolls using smooth scrolling. Use the HIWORD portion of the flags parameter to indicate how much time the smooth-scrolling operation should take.			
		/// </summary>
		SW_SMOOTHSCROLL,
	}
}