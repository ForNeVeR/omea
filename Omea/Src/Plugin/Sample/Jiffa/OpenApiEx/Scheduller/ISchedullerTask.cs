// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// A task is an atomic item of schedulling that does not define any schedulle on itself, but rather belongs to one or more schedulling groups.
	/// </summary>
	public interface ISchedullerTask : IResourceObject
	{
	}
}
