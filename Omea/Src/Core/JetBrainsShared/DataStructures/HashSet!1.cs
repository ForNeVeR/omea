// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

/*
using JetBrains.DataStructures;

namespace System.Collections.Generic
{
	/// <summary>
	/// Mimics the Netfx35 HashSet class.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	public class HashSet<TKey> : ICollection<TKey>
	{
		#region Data

		protected internal Bucket[] _buckets;

		protected internal uint _count;

		protected internal uint _firstFree;

		protected internal uint[] _hashTable;

		protected internal uint _initialSize;

		protected internal uint _size;

		protected internal int _version;

		#endregion

		#region Init

		public HashSet()
			: this(0)
		{
		}

		public HashSet(int initialSize)
		{
			_count = 0;
			ReHash(_initialSize = HashtableParams.AdjustHashtableSize((uint)initialSize));
		}


		public HashSet(IEnumerable<TKey> collection)
			: this(0)
		{
			foreach(TKey item in collection)
				Add(item);
		}

		#endregion

		#region Attributes

		public int Count
		{
			get
			{
				return (int)_count;
			}
		}

		#endregion

		#region Operations

		public virtual void Add(TKey key)
		{
			uint tableIndex, prevIndex;
			uint index = SearchCollisions(key, out tableIndex, out prevIndex);
			if(index == 0)
			{
				uint i = _firstFree;
				if(i != 0)
					_firstFree = _buckets[i].Next;
				else
				{
					i = _count + 1;
					if(i == _buckets.Length)
					{
						ReHash(((_size * 13) - 7) >> 3);
						tableIndex = ((uint)key.GetHashCode()) % _size;
					}
				}
				_buckets[i].Key = key;
				_buckets[i].Next = _hashTable[tableIndex];
				_hashTable[tableIndex] = i;
				++_count;
#if DEBUG
				++_version;
#endif
			}
		}

		public void Clear()
		{
			if(_initialSize != _size)
			{
				_count = 0;
				ReHash(0);
			}
			else
			{
				if(_count > 0)
				{
					_count = 0;
					_firstFree = 0;
					Array.Clear(_hashTable, 0, _hashTable.Length);
					Array.Clear(_buckets, 0, _buckets.Length);
				}
			}
		}

		public bool Contains(TKey key)
		{
			if(_count == 0)
				return false;

			if(key == null)
				throw new ArgumentNullException("key");

			uint tableIndex, prevIndex;
			return SearchCollisions(key, out tableIndex, out prevIndex) != 0;
		}

		public IEnumerator GetEnumerator()
		{
			return new HashSetEnumerator(this);
		}

		public object GetKey(object key)
		{
			if(key == null)
				throw new ArgumentNullException("key");

			if(_count == 0)
				return null;

			uint tableIndex, prevIndex;
			uint bucketIndex = SearchCollisions(key, out tableIndex, out prevIndex);
			return (bucketIndex == 0) ? null : _buckets[bucketIndex].Key;
		}

		public void Remove(object key)
		{
			if(key == null)
				throw new ArgumentNullException("key");

			if(_count == 0)
				return;

			uint tableIndex, prevIndex;
			uint index = SearchCollisions(key, out tableIndex, out prevIndex);
			if(index != 0)
			{
				if(prevIndex != 0)
					_buckets[prevIndex].Next = _buckets[index].Next;
				else
					_hashTable[tableIndex] = _buckets[index].Next;
				_buckets[index].Key = null;
				_buckets[index].Next = _firstFree;
				_firstFree = index;
				if(--_count == 0)
					Clear();
#if DEBUG
				++_version;
#endif
			}
		}

		#endregion

		#region Implementation

		protected internal void ReHash(uint desiredSize)
		{
			if(desiredSize < _initialSize)
				desiredSize = _initialSize;
			uint size = HashtableParams.AdjustHashtableSize(desiredSize);
			if(size != _size)
			{
				_firstFree = 0;
				Bucket[] oldBuckets = _buckets;
				_hashTable = new uint[size];
				_buckets = new Bucket[size * HashtableParams.maxBucketsPerIndex];
				if(_count > 0)
				{
					for(uint i = 1, j = 0; i < oldBuckets.Length; ++i)
					{
						object key = oldBuckets[i].Key;
						if(key != null)
						{
							++j;
							_buckets[j].Key = key;
							uint hashValue = ((uint)key.GetHashCode()) % size;
							_buckets[j].Next = _hashTable[hashValue];
							_hashTable[hashValue] = j;
						}
					}
				}
#if DEBUG
				++_version;
#endif
				_size = size;
			}
		}

		/// <summary>
		/// Returns index of bucket where key is found or zero if not found.
		/// </summary>
		protected internal uint SearchCollisions(TKey key, out uint tableIndex, out uint prevIndex)
		{
			prevIndex = 0;
			uint bucketIndex = _hashTable[tableIndex = ((uint)key.GetHashCode()) % _size];
			if(bucketIndex > 0 && !key.Equals(_buckets[bucketIndex].Key))
			{
#if DEBUG
				int savedVersion = _version;
#endif
				_buckets[0].Key = key;
				do
				{
#if DEBUG
					if(savedVersion != _version || !key.Equals(_buckets[0].Key))
						throw new InvalidOperationException("Non-serialized usage detected!");
#endif
					bucketIndex = _buckets[(prevIndex = bucketIndex)].Next;
				} while(!key.Equals(_buckets[bucketIndex].Key));
				_buckets[0].Key = null;
			}
			return bucketIndex;
		}

		#endregion

		#region bucket Type

		protected internal struct Bucket
		{
			#region Data

			public TKey Key;

			public uint Next;

			#endregion
		}

		#endregion

		#region Entry Type

		/// <summary>
		/// Proxy object around a bucket representing key-value pair.
		/// </summary>
		public class Entry
		{
			#region Data

			internal Bucket[] _buckets;

			internal uint _index;

			#endregion

			#region Init

			internal Entry(Bucket[] buckets, uint index)
			{
				_buckets = buckets;
				_index = index;
			}

			#endregion

			#region Attributes

			public object Key
			{
				get
				{
					return _buckets[_index].Key;
				}
				set
				{
					_buckets[_index].Key = value;
				}
			}

			#endregion
		}

		#endregion

		#region HashSetEnumerator Type

		protected internal class HashSetEnumerator : IEnumerator
		{
			#region Data

			private readonly uint _count;

			private uint _counted;

			private readonly Entry _entry;

#if DEBUG
			private int _savedVersion;

			private readonly HashSet _theSet;

			#endregion

			#region Init

			public HashSetEnumerator(HashSet set)
			{
				_entry = new Entry(set._buckets, 0);
				_count = set._count;
				_theSet = set;
				Reset();
			}

			#endregion

			#region IEnumerator Members

			public virtual bool MoveNext()
			{
				if(_counted == _count)
					return false;
				while(_entry._buckets[++_entry._index].Key == null)
					;
				++_counted;
				return true;
			}

			public void Reset()
			{
				_entry._index = _counted = 0;
#if DEBUG
				_savedVersion = _theSet._version;
#endif
			}

			public object Current
			{
				get
				{
#if DEBUG
					if(_counted == 0)
						throw new InvalidOperationException("Enumerator.Current called without calling MoveNext().");
					if(_savedVersion != _theSet._version)
						throw new InvalidOperationException("Collection modified while enumeration.");
#endif
					return _entry;
				}
			}

			#endregion

#endif
		}

		#endregion

#endif

		#region ICollection<TKey> Members

		///<summary>
		///Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
		///</summary>
		///
		///<returns>
		///true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.
		///</returns>
		///
		///<param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		bool ICollection<TKey>.Contains(TKey item)
		{
			throw new NotImplementedException();
		}

		///<summary>
		///Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
		///</summary>
		///
		///<param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
		///<param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
		///<exception cref="T:System.ArgumentNullException"><paramref name="array" /> is null.</exception>
		///<exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is less than 0.</exception>
		///<exception cref="T:System.ArgumentException"><paramref name="array" /> is multidimensional.-or-<paramref name="arrayIndex" /> is equal to or greater than the length of <paramref name="array" />.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.-or-Type <paramref name="T" /> cannot be cast automatically to the type of the destination <paramref name="array" />.</exception>
		void ICollection<TKey>.CopyTo(TKey[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		///<summary>
		///Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
		///</summary>
		///
		///<returns>
		///true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
		///</returns>
		///
		///<param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</exception>
		bool ICollection<TKey>.Remove(TKey item)
		{
			throw new NotImplementedException();
		}

		///<summary>
		///Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
		///</summary>
		///
		///<returns>
		///true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.
		///</returns>
		///
		bool ICollection<TKey>.IsReadOnly
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		#region IEnumerable<TKey> Members

		///<summary>
		///Returns an enumerator that iterates through the collection.
		///</summary>
		///
		///<returns>
		///A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		///</returns>
		///<filterpriority>1</filterpriority>
		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
*/
