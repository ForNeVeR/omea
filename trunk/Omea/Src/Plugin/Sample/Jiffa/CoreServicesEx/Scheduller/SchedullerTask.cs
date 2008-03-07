/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.CoreServicesEx.Scheduller
{
	/// <summary>
	/// A task is an atomic item of schedulling that does not define any schedulle on itself, but rather belongs to one or more schedulling groups.
	/// </summary>
	internal class SchedullerTask : ResourceObject, ISchedullerTask
	{
		public SchedullerTask(IResource resource)
			: base(resource)
		{
		}
	}
}