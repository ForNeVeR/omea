// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Annotations;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Denotes an exception that occurs during the interop calls.
	/// </summary>
	public class Win32InteropException : ApplicationException
	{
		#region Init

		public Win32InteropException([NotNull] string message, [NotNull] Exception innerException)
			: base(message, innerException)
		{
		}

		public Win32InteropException([NotNull] string message)
			: base(message)
		{
		}

		#endregion
	}
}
