/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Common.Infra
{
	/// <summary>
	/// Static helper methods, externalized from different tasks.
	/// </summary>
	public static class TaskHelper
	{
		#region Operations

		/// <summary>
		/// Gets a string value from the bag, throws on an error.
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

		/// <summary>
		/// Gets a <typeparamref name="T"/> value from the bag, throws on an error.
		/// </summary>
		public static T GetValue<T>(Hashtable bag, AttributeName attribute)
		{
			object oValue = bag[attribute];
			if(oValue == null)
				throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be specified.", attribute));
			if(oValue is T)
				return (T)oValue;
			throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be of type {1}.", attribute, typeof(T).FullName));
		}

		#endregion
	}
}