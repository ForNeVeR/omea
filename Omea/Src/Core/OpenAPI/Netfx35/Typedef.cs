// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using System.Collections.Generic;

namespace System35
{
	public delegate void Action();

	public delegate void Action<T1, T2>(T1 arg1, T2 arg2);

	public delegate void Action<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);

	public delegate void Action<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

	public delegate TResult Func<TResult>();

	public delegate TResult Func<T, TResult>(T arg);

	public delegate TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);

	public delegate TResult Func<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);

	public delegate TResult Func<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
}

namespace System.Collection.Generic
{
	/// <summary>
	/// Mimics the Netfx35 HashSet.
	/// </summary>
	public class HashSet<TKey> : ICollection<TKey>
	{
		#region Data

		private readonly Dictionary<TKey, byte> _map;

		#endregion

		#region Init

		public HashSet(int capacity)
		{
			_map = new Dictionary<TKey, byte>(capacity);
		}

		public HashSet()
		{
			_map = new Dictionary<TKey, byte>();
		}

		#endregion

		#region ICollection<TKey> Members

		///<summary>
		///Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
		///</summary>
		///
		///<param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"></see>.</param>
		///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only.</exception>
		void ICollection<TKey>.Add(TKey item)
		{
			_map.Add(item, 0);
		}

		public bool Add(TKey item)
		{
			if(_map.ContainsKey(item))
				return false;
			_map.Add(item, 0);
			return true;
		}

		///<summary>
		///Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
		///</summary>
		///
		///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only. </exception>
		public void Clear()
		{
			_map.Clear();
		}

		///<summary>
		///Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"></see> contains a specific value.
		///</summary>
		///
		///<returns>
		///true if item is found in the <see cref="T:System.Collections.Generic.ICollection`1"></see>; otherwise, false.
		///</returns>
		///
		///<param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"></see>.</param>
		public bool Contains(TKey item)
		{
			return _map.ContainsKey(item);
		}

		///<summary>
		///Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"></see> to an <see cref="T:System.Array"></see>, starting at a particular <see cref="T:System.Array"></see> index.
		///</summary>
		///
		///<param name="array">The one-dimensional <see cref="T:System.Array"></see> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"></see>. The <see cref="T:System.Array"></see> must have zero-based indexing.</param>
		///<param name="arrayIndex">The zero-based index in array at which copying begins.</param>
		///<exception cref="T:System.ArgumentOutOfRangeException">arrayIndex is less than 0.</exception>
		///<exception cref="T:System.ArgumentNullException">array is null.</exception>
		///<exception cref="T:System.ArgumentException">array is multidimensional.-or-arrayIndex is equal to or greater than the length of array.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"></see> is greater than the available space from arrayIndex to the end of the destination array.-or-Type T cannot be cast automatically to the type of the destination array.</exception>
		public void CopyTo(TKey[] array, int arrayIndex)
		{
			_map.Keys.CopyTo(array, arrayIndex);
		}

		///<summary>
		///Returns an enumerator that iterates through a collection.
		///</summary>
		///
		///<returns>
		///An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
		///</returns>
		///<filterpriority>2</filterpriority>
		public IEnumerator GetEnumerator()
		{
			return _map.Keys.GetEnumerator();
		}

		///<summary>
		///Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
		///</summary>
		///
		///<returns>
		///true if item was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"></see>; otherwise, false. This method also returns false if item is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"></see>.
		///</returns>
		///
		///<param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"></see>.</param>
		///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only.</exception>
		public bool Remove(TKey item)
		{
			return _map.Remove(item);
		}

		///<summary>
		///Returns an enumerator that iterates through the collection.
		///</summary>
		///
		///<returns>
		///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
		///</returns>
		///<filterpriority>1</filterpriority>
		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
		{
			return _map.Keys.GetEnumerator();
		}

		///<summary>
		///Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
		///</summary>
		///
		///<returns>
		///The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
		///</returns>
		///
		public int Count
		{
			get
			{
				return _map.Count;
			}
		}

		///<summary>
		///Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only.
		///</summary>
		///
		///<returns>
		///true if the <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only; otherwise, false.
		///</returns>
		///
		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		#endregion
	}
}

namespace System.Runtime.CompilerServices
{
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
  public sealed class ExtensionAttribute : Attribute { }
}
