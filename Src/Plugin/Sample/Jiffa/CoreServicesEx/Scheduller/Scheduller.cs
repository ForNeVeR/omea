// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.CoreServicesEx.Scheduller
{
	internal class Scheduller : IScheduller
	{
		#region IScheduller Members

		/// <summary>
		/// Gets the container of Scheduller data constants.
		/// </summary>
		public ISchedullerData Data
		{
			get
			{
				return SchedullerData.Instance;
			}
		}

		/// <summary>
		/// Gets the collection of all the schedulled groups.
		/// A group is a collection of tasks, all schedulled for execution at some moment of time, for example, periodically, or on Omea startup.
		/// </summary>
		public IResourceObjectsListByName<ISchedullerGroup> Groups
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Gets the collection of all the schedulled tasks.
		/// A task is an atomic item of schedulling that does not define any schedulle on itself, but rather belongs to one or more schedulling groups.
		/// </summary>
		public IResourceObjectsListByName<ISchedullerTask> Tasks
		{
			get
			{
				//return new ResourceObjectsListByName<ISchedullerTask>(OpenAPI.Core.ResourceStore.GetAllResources(Data.SchedullerTaskResourceTypeName), )
				throw new InvalidOperationException(string.Format("!!!"));

			}
		}

		/// <summary>
		/// Enumerates all the tasks and drops those for whom there is no plugin available anymore.
		/// Can be executed to clean up the remainders of previously-installed plugins.
		/// </summary>
		public void DropOrphanedTasks()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
