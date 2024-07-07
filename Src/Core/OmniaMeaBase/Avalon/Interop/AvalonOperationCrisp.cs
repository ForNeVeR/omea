// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;

using JetBrains.Interop.WinApi;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Interop;
using JetBrains.Util.Interop;

namespace JetBrains.UI.Hooks
{
	/// <summary>
	/// Enforses crisp fonts in avalon controls.
	/// Call whenever you're about to use Avalon, before or after doing so.
	/// All the calls but the first one will be ignored.
	/// </summary>
	/// <remarks>
	/// Hooks the MILCORE so that it obtained somehow improved information about the system settings.
	/// </remarks>
	public static unsafe class AvalonOperationCrisp
	{
		#region Data

		private static readonly object myCookie = new object();

		private static bool myExecuted;

		#endregion

		#region Operations

		public static void Execute()
		{
			// On the second time the hook would not find the original function to play with, and will throw
			lock(myCookie)
			{
				if(myExecuted)
					return;
				myExecuted = true;
			}

			// Do it
			try
			{
				Execute_InstallHook();
				myExecuted = true;

				Execute_InvalidateMil();
			}
			catch(Exception ex)
			{
				Core.ReportException(ex, false);
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Injects the hook.
		/// </summary>
		private static void Execute_InstallHook()
		{
			ImportAddressTableHook.Install("USER32.DLL", "SystemParametersInfoW", "MILCORE.DLL", (SystemParametersInfoWDelegate)OnSystemParametersInfoW);
		}

		/// <summary>
		/// A kick for the MILCORE to query for the display settings again, this time with our improved data.
		/// </summary>
		private static void Execute_InvalidateMil()
		{
			// Send an update to all the top-level windows on our thread
			Win32Declarations.EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
			{
				int dwDummy;
				if(Win32Declarations.GetWindowThreadProcessId(hWnd, out dwDummy) == Kernel32Dll.GetCurrentThreadId())
				{
					User32Dll.Helpers.SendMessageW(hWnd, WindowsMessages.WM_SETTINGCHANGE, (IntPtr)Win32Declarations.SPI_SETFONTSMOOTHING, IntPtr.Zero);
					User32Dll.Helpers.SendMessageW(hWnd, WindowsMessages.WM_SETTINGCHANGE, (IntPtr)Win32Declarations.SPI_SETFONTSMOOTHINGTYPE, IntPtr.Zero);
				}

				return true; // Go on
			}, 0);
		}

		/// <summary>
		/// Our replacement function which we hook into the MILCORE's IAT.
		/// </summary>
		private static int OnSystemParametersInfoW(UInt32 uiAction, UInt32 uiParam, void* pvParam, UInt32 fWinIni)
		{
			switch(uiAction)
			{
			case Win32Declarations.SPI_GETCLEARTYPE:
				*((int*)pvParam) = 1;
				return 1;
			case Win32Declarations.SPI_GETFONTSMOOTHING:
				*((int*)pvParam) = 1;
				return 1;
			case Win32Declarations.SPI_GETFONTSMOOTHINGTYPE:
				*((UInt32*)pvParam) = Win32Declarations.FE_FONTSMOOTHINGCLEARTYPE;
				return 1;
			default:
				return SystemParametersInfoW(uiAction, uiParam, pvParam, fWinIni); // As we hook only MILCORE's view on the function, we're still calling the original version (or, maybe, hooked by someone else ;)
			}
		}

		[DllImport("User32.dll", ExactSpelling = true)]
		private static extern int SystemParametersInfoW(UInt32 uiAction, UInt32 uiParam, void* pvParam, UInt32 fWinIni);

		#endregion

		#region SystemParametersInfoWDelegate Type

		public delegate int SystemParametersInfoWDelegate(UInt32 uiAction, UInt32 uiParam, void* pvParam, UInt32 fWinIni);

		#endregion
	}
}
