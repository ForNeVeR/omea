/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;

using JetBrains.Build.Util;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JetBrains.Build.HelperTasks
{
	/// <summary>
	/// The base class for deriving tasks from it, defines the attribute bag.
	/// </summary>
	public abstract class TaskBase : Task
	{
		/// <summary>
		/// <see cref="Bag"/>.
		/// </summary>
		private readonly Hashtable myBag = new Hashtable();

		/// <summary>
		/// Gets the task attributes bag.
		/// </summary>
		protected Hashtable Bag
		{
			get
			{
				return myBag;
			}
		}

		/// <summary>
		/// Gets a string value from the bag, throws on an error.
		/// </summary>
		protected string GetStringValue(AttributeName attribute)
		{
			object oValue = Bag[attribute];
			if(oValue == null)
				throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be specified.", attribute));
			if(oValue is ITaskItem)
				return ((ITaskItem)oValue).ItemSpec;
			if(oValue is string)
				return (string)oValue;
			throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be a string.", attribute));
		}

		/// <summary>
		/// Gets a <typeparamref name="T"/> value from the bag, throws on an error.
		/// </summary>
		protected T GetValue<T>(AttributeName attribute)
		{
			object oValue = Bag[attribute];
			if(oValue == null)
				throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be specified.", attribute));
			if(oValue is T)
				return (T)oValue;
			throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be of type {1}.", attribute, typeof(T).FullName));
		}

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

		/// <summary>
		/// The method to be overriden in inheriting tasks.
		/// Throw an exception in case of an errror.
		/// </summary>
		protected abstract void ExecuteTask();
	}
}