// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// A group is a collection of tasks, all schedulled for execution at some moment of time, for example, periodically, or on Omea startup.
	/// </summary>
	public interface ISchedullerGroup : IResourceObject, ISchedullerTaskFolder
	{
	}
}
