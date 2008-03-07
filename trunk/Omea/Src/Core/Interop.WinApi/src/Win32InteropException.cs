/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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