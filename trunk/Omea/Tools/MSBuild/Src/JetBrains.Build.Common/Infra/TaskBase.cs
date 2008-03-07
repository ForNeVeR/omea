/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JetBrains.Build.Common.Infra
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
		private readonly Hashtable myBag = new Hashtable();

		#endregion

		#region Operations

		/// <summary>
		/// Checks whether a bag entry is present.
		/// </summary>
		public bool BagContains(AttributeName name)
		{
			return Bag[name] != null;
		}

		/// <summary>
		/// Gets a typed value from the bag. Throws if a value is missing.
		/// </summary>
		public T BagGet<T>(AttributeName name)
		{
			return TaskHelper.GetValue<T>(Bag, name);
		}

		/// <summary>
		/// Gets a typed value from the bag. Returns the <paramref name="defaultvalue"/> if an entry is missing from the bag.
		/// </summary>
		public T BagGet<T>(AttributeName name, T defaultvalue)
		{
			object entry = Bag[name];
			return (T)(entry ?? defaultvalue);
		}

		/// <summary>
		/// Gets a typed value from the bag. <c>Null</c> (a missing value) is OK.
		/// </summary>
		public T BagGetTry<T>(AttributeName name)
		{
			return (T)Bag[name];
		}

		/// <summary>
		/// Puts a typed value to the bag. <c>Null</c> (a missing value) is OK.
		/// </summary>
		public void BagSet<T>(AttributeName name, T value)
		{
			Bag[name] = value;
		}

		#endregion

		#region Implementation

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
		/// The method to be overriden in inheriting tasks.
		/// Throw an exception in case of an errror.
		/// </summary>
		protected abstract void ExecuteTask();

		/// <summary>
		/// Gets a string value from the bag, throws on an error.
		/// </summary>
		protected string GetStringValue(AttributeName attribute)
		{
			return TaskHelper.GetStringValue(Bag, attribute);
		}

		/// <summary>
		/// Gets a <typeparamref name="T"/> value from the bag, throws on an error.
		/// </summary>
		protected T GetValue<T>(AttributeName attribute)
		{
			return TaskHelper.GetValue<T>(Bag, attribute);
		}

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