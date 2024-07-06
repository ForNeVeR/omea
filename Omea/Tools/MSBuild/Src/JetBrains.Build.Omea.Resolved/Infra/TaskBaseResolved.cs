// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Build.Omea.Infra;
using JetBrains.Build.Omea.Util;

using Microsoft.Build.Utilities;

namespace JetBrains.Build.Omea.Resolved.Infra
{
	/// <summary>
	/// The base class for the “Resolved” part of the product-dependent tasks.
	/// These tasks require strong references to the product binaries. They're loaded into a different AppDomain (to unload on completion), and a special resolved is installed to locate them in the binaries folder.
	/// These resolved task parts should be capable of reading the parameters their actual tasks receive, which is achieved thru the use of a property bag.
	/// </summary>
	public abstract class TaskBaseResolved : MarshalByRefObject, ITaskBaseResolved
	{
		#region Data

		/// <summary>
		/// <see cref="Log"/>.
		/// </summary>
		private TaskLoggingHelper myLog;

		#endregion

		#region Attributes

		/// <summary>
		/// Gets or sets the logger into which the progress/status messages should be emitted.
		/// </summary>
		public TaskLoggingHelper Log
		{
			get
			{
				return myLog ?? (myLog = Bag.Get<TaskLoggingHelper>(AttributeName.AfxLog));
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// The method to be overriden in inheriting tasks.
		/// Throw an exception in case of an errror.
		/// </summary>
		protected abstract void ExecuteTaskResolved();

		#endregion

		#region ITaskBaseResolved Members

		///<summary>
		/// Executes the resolved task, catches and reports its exceptions.
		/// Should not throw.
		///</summary>
		public void Execute()
		{
			if(Bag == null)
				throw new InvalidOperationException(string.Format("The Parameter Bag must have been given to the task before executing it."));

			ExecuteTaskResolved();
		}

		/// <summary>
		/// Gets or sets the task parameters bag that comes from the unresolved part of the task.
		/// </summary>
		public Bag Bag { get; set; }

		#endregion
	}
}
