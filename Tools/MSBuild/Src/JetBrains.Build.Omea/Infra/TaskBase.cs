// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Build.Omea.Util;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JetBrains.Build.Omea.Infra
{
	/// <summary>
	/// The base class for deriving tasks from it, defines the attribute bag.
	/// </summary>
	public abstract class TaskBase : Task
	{
		#region Data

		/// <summary>
		/// <see cref="Bag"/>.
		/// </summary>
		private readonly Bag myBag = new Bag();

		#endregion

		#region Implementation

		/// <summary>
		/// Gets the task attributes bag.
		/// </summary>
		protected Bag Bag
		{
			get
			{
				return myBag;
			}
		}

		/// <summary>
		/// The method to be overriden in inheriting tasks.
		/// Throw an exception in case of an errror.
		/// </summary>
		protected abstract void ExecuteTask();

		#endregion

		#region Overrides

		///<summary>
		///When overridden in a derived class, executes the task.
		///</summary>
		///
		///<returns>
		///true if the task successfully executed; otherwise, false.
		///</returns>
		///
		public override bool Execute()
		{
			try
			{
				ExecuteTask();
				return true;
			}
			catch(Exception ex)
			{
				Log.LogError(ex.Message);
				Log.LogMessage(MessageImportance.Normal, ex.ToString());
				return false;
			}
		}

		#endregion
	}
}
