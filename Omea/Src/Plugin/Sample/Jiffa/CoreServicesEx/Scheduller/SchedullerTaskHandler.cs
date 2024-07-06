// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.CoreServicesEx.Scheduller
{
	/// <summary>
	/// A object that handles a schedulled task, that is, is created and executed when the task should be performed.
	/// </summary>
	internal class SchedullerTaskHandler : ResourceObject, ISchedullerTaskHandler
	{
		public SchedullerTaskHandler(IResource resource)
			: base(resource)
		{
		}
	}
}
