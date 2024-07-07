// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace JetBrains.UI.Util
{
	public static class CollectionEx
	{
		#region Operations

		/// <summary>
		/// Returns the value associated with the key, or <c>Null</c> if not found.
		/// </summary>
		[CanBeNull]
		public static TValue TryGetValue<TKey, TValue>([NotNull] this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			if(dictionary == null)
				throw new ArgumentNullException("dictionary");

			TValue value;
			dictionary.TryGetValue(key, out value);
			return value;
		}

		#endregion
	}
}
