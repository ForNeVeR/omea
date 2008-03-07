/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// A group is a collection of tasks, all schedulled for execution at some moment of time, for example, periodically, or on Omea startup.
	/// </summary>
	public interface ISchedullerGroup : IResourceObject, ISchedullerTaskFolder
	{
	}
}