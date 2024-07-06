// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;

using JetBrains.Build.Omea.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Omea.Util
{
	/// <summary>
	/// Implements a property bag that is marshalled by a reference.
	/// </summary>
	public class Bag : MarshalByRefObject
	{
		#region Data

		private readonly Dictionary<AttributeName, object> myStore = new Dictionary<AttributeName, object>();

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the underlying store.
		/// </summary>
		public Dictionary<AttributeName, object> Store
		{
			get
			{
				return myStore;
			}
		}

		#endregion

		#region Operations

		/// <summary>
		/// Checks whether the value is present in the bag.
		/// The type parameter checks the value type.
		/// </summary>
		public bool Contains<T>(AttributeName name)
		{
#pragma warning disable CompareNonConstrainedGenericWithNull
			return TryGet<T>(name) != null;
#pragma warning restore CompareNonConstrainedGenericWithNull
		}

		/// <summary>
		/// Gets a <typeparamref name="T"/> value from the bag, throws on an error.
		/// </summary>
		public T Get<T>(AttributeName name)
		{
			object value;
			if((!Store.TryGetValue(name, out value)) || (value == null))
				throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be specified.", name));
			return DynamicCast<T>(name, value);
		}

		/// <summary>
		/// Gets a <paramref name="name"/> value from the bag, or default if it's missing.
		/// </summary>
		public T Get<T>(AttributeName name, T defaultvalue)
		{
			object value;
			if((Store.TryGetValue(name, out value)) || (value == null))
				return DynamicCast<T>(name, value);
			return defaultvalue;
		}

		/// <summary>
		/// Gets a string value from the bag, throws on an error.
		/// Has a special treatment for the TaskItem elements.
		/// </summary>
		public string GetString(AttributeName attribute)
		{
			object value;
			if((!Store.TryGetValue(attribute, out value)) || (value == null))
			{
				if(value == null)
					throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be specified.", attribute));
			}
			if(value is ITaskItem)
				return ((ITaskItem)value).ItemSpec;
			if(value is string)
				return (string)value;
			throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be a string.", attribute));
		}

		/// <summary>
		/// Assigns a new value into the property bag.
		/// </summary>
		public void Set<T>(AttributeName name, T value)
		{
			TryGet<T>(name); // This ensures the value type does not change in the cell
			Store[name] = value;
		}

		/// <summary>
		/// Tries to get the value, returns a default value if not available.
		/// Throws only on value type mismatch.
		/// </summary>
		public T TryGet<T>(AttributeName name)
		{
			object value;
			return Store.TryGetValue(name, out value) ? DynamicCast<T>(name, value) : default(T);
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Casts the value to the given type.
		/// Throws a detailed message if that is not possible.
		/// </summary>
		private static T DynamicCast<T>(AttributeName name, object value)
		{
			if(!(value is T))
				throw new InvalidOperationException(string.Format("The “{0}” task input parameter must be of type “{1}”.", name, typeof(T).AssemblyQualifiedName));
			return (T)value;
		}

		#endregion
	}
}
