/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// Conducts the schedulled actions execution in Omea.
	/// </summary>
	public interface IScheduller
	{
		/// <summary>
		/// Gets the container of Scheduller data constants.
		/// </summary>
		ISchedullerData Data { get; }

		/// <summary>
		/// Gets the collection of all the schedulled groups.
		/// A group is a collection of tasks, all schedulled for execution at some moment of time, for example, periodically, or on Omea startup.
		/// </summary>
		IResourceObjectsListByName<ISchedullerGroup> Groups { get; }

		/// <summary>
		/// Gets the collection of all the schedulled tasks.
		/// A task is an atomic item of schedulling that does not define any schedulle on itself, but rather belongs to one or more schedulling groups.
		/// </summary>
		IResourceObjectsListByName<ISchedullerTask> Tasks { get; }

		/// <summary>
		/// Enumerates all the tasks and drops those for whom there is no plugin available anymore.
		/// Can be executed to clean up the remainders of previously-installed plugins.
		/// </summary>
		void DropOrphanedTasks();
	}
}