﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.CoreServicesEx.Scheduller
{
	/// <summary>
	/// A group is a collection of tasks, all schedulled for execution at some moment of time, for example, periodically, or on Omea startup.
	/// </summary>
	internal class SchedullerGroup : ResourceObject, ISchedullerGroup
	{
		public SchedullerGroup(IResource resource)
			: base(resource)
		{
		}

		#region ISchedullerTaskFolder Members

		/// <summary>
		/// Gets the tasks in this folder, non-recursively.
		/// </summary>
		public IResourceObjectsListByName<ISchedullerTask> Tasks
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Gets the tasks in this folder and in all the folders under this one, recursively.
		/// </summary>
		public IResourceObjectsListByName<ISchedullerTask> TasksRecursive
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Gets the sub-folders in this folder, non-recursively.
		/// </summary>
		public IResourceObjectsListByName<ISchedullerTaskFolder> Folders
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		#endregion
	}
}
