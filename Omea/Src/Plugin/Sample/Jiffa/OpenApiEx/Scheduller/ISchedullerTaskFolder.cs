/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// A folder contains other folder and tasks.
	/// </summary>
	public interface ISchedullerTaskFolder : IResourceObject
	{
		/// <summary>
		/// Gets the tasks in this folder, non-recursively.
		/// </summary>
		IResourceObjectsListByName<ISchedullerTask> Tasks { get; }

		/// <summary>
		/// Gets the tasks in this folder and in all the folders under this one, recursively.
		/// </summary>
		IResourceObjectsListByName<ISchedullerTask> TasksRecursive { get; }

		/// <summary>
		/// Gets the sub-folders in this folder, non-recursively.
		/// </summary>
		IResourceObjectsListByName<ISchedullerTaskFolder> Folders { get; }
	}
}