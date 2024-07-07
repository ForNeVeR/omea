// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;

namespace JetBrains.Interop.WinApi.Wrappers
{
	/// <summary>
	/// Wraps a window handle into the WinForms <see cref="IWin32Window"/> interface.
	/// </summary>
	public unsafe class Win32Window : IWin32Window
	{
		#region Data

		private readonly IntPtr _handle;

		#endregion

		#region Init

		/// <summary>
		/// Wraps a native window handle.
		/// Can be <c>Null</c>.
		/// </summary>
		public Win32Window(IntPtr handle)
		{
			_handle = handle;
		}

		/// <summary>
		/// Wraps a native window handle.
		/// Can be <c>Null</c>.
		/// </summary>
		public Win32Window(void* handle)
		{
			_handle = (IntPtr)handle;
		}

		#endregion

		#region IWin32Window Members

		///<summary>
		///Gets the handle to the window represented by the implementer.
		///</summary>
		///
		///<returns>
		///A handle to the window represented by the implementer.
		///</returns>
		///<filterpriority>1</filterpriority>
		public IntPtr Handle
		{
			get
			{
				return _handle;
			}
		}

		#endregion
	}
}
