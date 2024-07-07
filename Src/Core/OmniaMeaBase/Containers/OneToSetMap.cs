// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collection.Generic;
using System.Collections;
using System.Collections.Generic;

namespace JetBrains.Omea.Containers
{
	/// <summary>
	/// A prototype implementation for a one-to-set map.
	/// </summary>
	public class OneToSetMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, ICollection<TValue>>>
	{
		#region Data

		private readonly Dictionary<TKey, ICollection<TValue>> _map;

		#endregion

		#region Init

		public OneToSetMap()
		{
			_map = new Dictionary<TKey, ICollection<TValue>>();
		}

		#endregion

		#region Attributes

		public ICollection<TValue> this[TKey key]
		{
			get
			{
				ICollection<TValue> value;
				value = !_map.TryGetValue(key, out value) ? new TValue[] {} : value;
				return value;
			}
		}

		#endregion

		#region Operations

		public void Add(TKey key, TValue value)
		{
			ICollection<TValue> values = TryGetValues(key);
			if(values == null)
			{
				values = new HashSet<TValue>();
				_map[key] = values;
			}

			values.Add(value);
		}

		public bool ContainsKey(TKey key)
		{
			return _map.ContainsKey(key);
		}

		#endregion

		#region Implementation

		private ICollection<TValue> TryGetValues(TKey key)
		{
			ICollection<TValue> value;
			return _map.TryGetValue(key, out value) ? value : null;
		}

		#endregion

		#region IEnumerable<KeyValuePair<TKey,ICollection<TValue>>> Members

		public IEnumerator<KeyValuePair<TKey, ICollection<TValue>>> GetEnumerator()
		{
			return _map.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _map.GetEnumerator();
		}

		#endregion
	}
}
