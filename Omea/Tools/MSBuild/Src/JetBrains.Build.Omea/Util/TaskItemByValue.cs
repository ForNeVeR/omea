/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JetBrains.Build.Omea.Util
{
	/// <summary>
	/// Mimics an <see cref="ITaskItem"/> in a marshal-by-value class.
	/// The standard implementation would ruin the build if it originates in an unloaded appdomain.
	/// </summary>
	[Serializable]
	public class TaskItemByValue
	{
		#region Data

		private string myItemSpec = "";

		private Dictionary<string, string> myMetadata = new Dictionary<string, string>();

		#endregion

		#region Init

		public TaskItemByValue()
		{
		}

		public TaskItemByValue(string itemSpec)
		{
			myItemSpec = itemSpec;
		}

		#endregion

		#region Attributes

		public string ItemSpec
		{
			get
			{
				return myItemSpec;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");

				myItemSpec = value;
			}
		}

		public Dictionary<string, string> Metadata
		{
			get
			{
				return myMetadata;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");

				myMetadata = value;
			}
		}

		#endregion

		#region Operations

		public static TaskItemByValue[] ArrayFromTaskItems(ITaskItem[] array)
		{
			if(array == null)
				throw new ArgumentNullException("array");

			var retval = new TaskItemByValue[array.Length];
			for(int a = 0; a < array.Length; a++)
			{
				retval[a] = new TaskItemByValue(array[a].ItemSpec);
				foreach(string name in array[a].MetadataNames)
					retval[a].AddMetadata(name, array[a].GetMetadata(name));
			}
			return retval;
		}

		public static ITaskItem[] ArrayToTaskItems(TaskItemByValue[] array)
		{
			if(array == null)
				throw new ArgumentNullException("array");

			var retval = new ITaskItem[array.Length];
			for(int a = 0; a < array.Length; a++)
				retval[a] = (TaskItem)array[a];
			return retval;
		}

		public void AddMetadata(string name, string value)
		{
			if(name == null)
				throw new ArgumentNullException("name");
			if(value == null)
				throw new ArgumentNullException("value");

			Metadata[name] = value;
		}

		#endregion

		#region ERROR

		public static explicit operator TaskItem(TaskItemByValue value)
		{
			if(value == null)
				throw new ArgumentNullException("value");

			return new TaskItem(value.ItemSpec, value.Metadata);
		}

		#endregion
	}
}