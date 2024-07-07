// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
