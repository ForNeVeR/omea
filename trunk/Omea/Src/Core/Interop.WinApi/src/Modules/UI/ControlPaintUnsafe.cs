/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Drawing;

using JetBrains.Annotations;

namespace JetBrains.Interop.WinApi.Modules.UI
{
	/// <summary>
	/// Encapsulates the utility classes for painting the controls.
	/// </summary>
	public static class ControlPaintUnsafe
	{
		#region Operations

		public static unsafe bool PaintStatusBarBackgroundTheme(IntPtr hwnd, [NotNull] Graphics g, Rectangle rectArea, Rectangle rectClip)
		{
			if(UxThemeDll.IsAvailable())
			{
				void* hTheme = UxThemeDll.OpenThemeData((void*)hwnd, "STATUS");
				if(hTheme == null)
					return false; // Probably, the app is not themed

				try
				{
					var hdc = (void*)g.GetHdc();
					try
					{
						// Part and state are both 0 to indicate the default
						RECT rcArea = rectArea;
						RECT rcClip = rectClip;
						int hRet = UxThemeDll.DrawThemeBackground(hTheme, hdc, 0, 0, &rcArea, &rcClip);
						if(hRet < 0)
							throw new Win32Exception(hRet);
					}
					finally
					{
						g.ReleaseHdc((IntPtr)hdc);
					}
				}
				finally
				{
					UxThemeDll.CloseThemeData(hTheme);
				}
			}

			return true;
		}

		#endregion
	}
}