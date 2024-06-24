// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using JetBrains.Annotations;
using JetBrains.UI.Interop;

namespace JetBrains.UI.Avalon
{
	/// <summary>
	/// Avalon-related helper routines.
	/// </summary>
	public static class Helpers
	{
		/// <summary>
		/// Applies the aeroglass effect to the whole window, if possible.
		/// Returns whether the effect is supported in the current environment and was applied successfully.
		/// </summary>
		public static bool Glassify([NotNull] Window window, bool enable, NoWindowHandleAction action)
		{
			if(window == null)
				throw new ArgumentNullException("window");

			IntPtr handle = new WindowInteropHelper(window).Handle;
			if(handle == IntPtr.Zero)
			{
				switch(action)
				{
				case NoWindowHandleAction.Ignore:
					return false;
				case NoWindowHandleAction.Throw:
					throw new InvalidOperationException(string.Format("Cannot glassify a window that does not have a handle."));
				default:
					throw new ArgumentOutOfRangeException("action");
				}
			}

			if(!UI.Interop.Helpers.CanGlassify(handle))
				return false;

			// Prepare the window for glassification
			window.Background = Brushes.Transparent;
			HwndSource.FromHwnd(handle).CompositionTarget.BackgroundColor = Colors.Transparent;

			return Glassify(handle, enable);
		}

		/// <summary>
		/// Applies the aeroglass effect to the whole window, if possible.
		/// Returns whether the effect is supported in the current environment and was applied successfully.
		/// </summary>
		public static bool Glassify(IntPtr handle, bool enable)
		{
			if(handle == IntPtr.Zero)
				throw new ArgumentNullException("handle");

			// Is the glass effect available?
			if(!((Environment.OSVersion.Platform == PlatformID.Win32NT) && (Environment.OSVersion.Version >= new Version(6, 0)) && (Win32Declarations.DwmIsCompositionEnabled())))
				return false;

			// Try glassifying the window
			return Win32Declarations.DwmExtendFrameIntoClientArea(handle, (enable ? Win32Declarations.MARGINS.WholeSurface : Win32Declarations.MARGINS.Null)) >= 0;
		}

		/// <summary>
		/// What to do if the window has no handle.
		/// </summary>
		public enum NoWindowHandleAction
		{
			Ignore,
			Throw
		}
	}
}
