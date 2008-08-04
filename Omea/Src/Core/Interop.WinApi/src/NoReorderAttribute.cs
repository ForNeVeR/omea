/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Util
{
	/// <summary>
	/// Prevents the Member Reordering feature from tossing members of the marked class.
	/// </summary>
	/// <remarks>
	/// The attribute must be mentioned in your member reordering patterns.
	/// </remarks>
	[AttributeUsage(AttributeTargets.All)]
	public class NoReorder : Attribute
	{
	}
}