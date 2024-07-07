// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
