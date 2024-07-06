// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Omea.Infra
{
	/// <summary>
	/// Static helper methods, externalized from different tasks.
	/// </summary>
	public static class TaskHelper
	{
		#region Operations

		public static bool BagContains(Hashtable bag, AttributeName name)
		{
			return bag[name] != null;
		}

		/// <summary>
		/// Gets a <typeparamref name="T"/> value from the bag, throws on an error.
		/// </summary>
		public static T BagGet<T>(Hashtable bag, AttributeName attribute)
		{
			object oValue = bag[attribute];
			if(oValue == null)
				throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be specified.", attribute));
			if(oValue is T)
				return (T)oValue;
			throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be of type {1}.", attribute, typeof(T).FullName));
		}

		public static T BagGet<T>(Hashtable bag, AttributeName name, T defaultvalue)
		{
			object entry = bag[name];
			return (T)(entry ?? defaultvalue);
		}

		public static T BagGetTry<T>(Hashtable bag, AttributeName name)
		{
			return (T)bag[name];
		}

		public static void BagSet<T>(Hashtable bag, AttributeName name, T value)
		{
			bag[name] = value;
		}

		/// <summary>
		/// Gets a string value from the bag, throws on an error.
		/// Has a special treatment for the TaskItem elements.
		/// </summary>
		public static string GetStringValue(Hashtable bag, AttributeName attribute)
		{
			object oValue = bag[attribute];
			if(oValue == null)
				throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be specified.", attribute));
			if(oValue is ITaskItem)
				return ((ITaskItem)oValue).ItemSpec;
			if(oValue is string)
				return (string)oValue;
			throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be a string.", attribute));
		}

		#endregion
	}
}
