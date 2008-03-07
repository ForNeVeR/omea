/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// Represents a list of resource objects, accessibly by index and Resource ID.
	/// </summary>
	/// <typeparam name="T">The resource object type.</typeparam>
	public class ResourceObjectsList<T> : IResourceObjectsList<T> where T : class, IResourceObject
	{
		/// <summary>
		/// The backing resource list.
		/// </summary>
		protected readonly IResourceList _resources;

		/// <summary>
		/// A factory wrapping the resources into resource objects.
		/// </summary>
		protected readonly IResourceObjectFactory<T> _factory;

		/// <summary>
		/// A handler for adding existing objects to the list.
		/// </summary>
		protected readonly EventHandler<ResourceObjectEventArgs> _handlerAdd;

		/// <summary>
		/// A handler for removing objects from the list.
		/// </summary>
		protected readonly EventHandler<ResourceObjectEventArgs> _handlerRemove;

		/// <summary>
		/// Creates a read-only list from a resource list and a factory that wraps the raw resources into resource objects.
		/// </summary>
		/// <param name="resources">The list of available resources, live or dead.</param>
		/// <param name="factory">The factory for wrapping the raw Omea resources into resource objects.</param>
		public ResourceObjectsList(IResourceList resources, IResourceObjectFactory<T> factory)
			: this(resources, factory, null, null)
		{
		}

		/// <summary>
		/// Creates a modifyable list from a resource list and a factory that wraps the raw resources into resource objects.
		/// The set of modification operations supported on the list is defined by the delegates you provide for adding and removing the items.
		/// </summary>
		/// <param name="resources">The list of available resources, live or dead.</param>
		/// <param name="factory">The factory for wrapping the raw Omea resources into resource objects.</param>
		/// <param name="handlerAdd">A handler for adding new resources to the list. May be <c>Null</c>.</param>
		/// <param name="handlerRemove">A handler for adding new resources to the list. May be <c>Null</c>.</param>
		public ResourceObjectsList(IResourceList resources, IResourceObjectFactory<T> factory, EventHandler<ResourceObjectEventArgs> handlerAdd, EventHandler<ResourceObjectEventArgs> handlerRemove)
		{
			if(resources == null)
				throw new ArgumentNullException("resources");
			if(factory == null)
				throw new ArgumentNullException("factory");
			// Handlers may be Null

			_resources = resources;
			_factory = factory;

			_handlerAdd = handlerAdd;
			_handlerRemove = handlerRemove;
		}

		/// <summary>
		/// Gets the resource list wrapped by this list.
		/// </summary>
		public virtual IResourceList Resources
		{
			get
			{
				return _resources;
			}
		}

		/// <summary>
		/// Gets a resource object by the database ID of an Omea resource.
		/// Throws if no such resource in the list.
		/// </summary>
		/// <param name="id">The Omea resource database ID.</param>
		/// <returns>The resource object.</returns>
		public T GetById(int id)
		{
			IResource resource = Core.ResourceStore.LoadResource(id); // Will throw if no such resource
			if(!_resources.Contains(resource))
				throw new ArgumentOutOfRangeException("id", id, "A resource with the given identifier is not present in the list.");
			return _factory.CreateResourceObject(resource);
		}

		/// <summary>
		/// Gets a resource object by the database ID of an Omea resource.
		/// Returns <c>Null</c> if no such resource in the list.
		/// </summary>
		/// <param name="id">The Omea resource database ID.</param>
		/// <returns>The resource object, or <c>Null</c> if not found.</returns>
		public T TryGetById(int id)
		{
			IResource resource = Core.ResourceStore.TryLoadResource(id);
			if(resource == null)
				return null;
			if(!_resources.Contains(resource))
				return null;
			return _factory.CreateResourceObject(resource);
		}

		///<summary>
		///Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"></see>.
		///</summary>
		///
		///<returns>
		///The index of item if found in the list; otherwise, -1.
		///</returns>
		///
		///<param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"></see>.</param>
		public virtual int IndexOf(T item)
		{
			return _resources.IndexOf(item.Resource);
		}

		///<summary>
		///Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"></see> at the specified index.
		///</summary>
		///
		///<param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"></see>.</param>
		///<param name="index">The zero-based index at which item should be inserted.</param>
		///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"></see> is read-only.</exception>
		///<exception cref="T:System.ArgumentOutOfRangeException">index is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"></see>.</exception>
		public virtual void Insert(int index, T item)
		{
			if(_handlerAdd == null)
				throw new NotSupportedException(IsReadOnly ? "The list is read-only." : "This operation is not supported by the list.");
			_handlerAdd(this, new ResourceObjectEventArgs(item));
		}

		///<summary>
		///Removes the <see cref="T:System.Collections.Generic.IList`1"></see> item at the specified index.
		///</summary>
		///
		///<param name="index">The zero-based index of the item to remove.</param>
		///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"></see> is read-only.</exception>
		///<exception cref="T:System.ArgumentOutOfRangeException">index is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"></see>.</exception>
		public virtual void RemoveAt(int index)
		{
			if(_handlerRemove == null)
				throw new NotSupportedException(IsReadOnly ? "The list is read-only." : "This operation is not supported by the list.");
			_handlerRemove(this, new ResourceObjectEventArgs(this[index]));
		}

		///<summary>
		///Gets or sets the element at the specified index.
		///</summary>
		///
		///<returns>
		///The element at the specified index.
		///</returns>
		///
		///<param name="index">The zero-based index of the element to get or set.</param>
		///<exception cref="T:System.ArgumentOutOfRangeException">index is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"></see>.</exception>
		///<exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1"></see> is read-only.</exception>
		public virtual T this[int index]
		{
			get
			{
				return _factory.CreateResourceObject(_resources[index]);
			}
			set
			{
				throw new NotSupportedException("The items within this list do not have a persistent index.");
			}
		}

		///<summary>
		///Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
		///</summary>
		///
		///<param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"></see>.</param>
		///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only.</exception>
		public virtual void Add(T item)
		{
			if(_handlerAdd == null)
				throw new NotSupportedException(IsReadOnly ? "The list is read-only." : "This operation is not supported by the list.");
			_handlerAdd(this, new ResourceObjectEventArgs(item));
		}

		///<summary>
		///Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
		///</summary>
		///
		///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only. </exception>
		public virtual void Clear()
		{
			if(_handlerRemove == null)
				throw new NotSupportedException(IsReadOnly ? "The list is read-only." : "This operation is not supported by the list.");

			// Static snapshot
			T[] items;
			lock(_resources) // To keep Count and itemset in sync on live lists
			{
				items = new T[Count];
				CopyTo(items, 0);
			}

			// Remove each
			foreach(T item in items)
				_handlerRemove(this, new ResourceObjectEventArgs(item));
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
		public virtual bool Contains(T item)
		{
			return _resources.Contains(item.Resource);
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
		public virtual void CopyTo(T[] array, int arrayIndex)
		{
			if(array == null)
				throw new ArgumentNullException("array");
			if(arrayIndex < 0)
				throw new ArgumentOutOfRangeException("arrayIndex");
			foreach(IResource resource in _resources)
			{
				if(arrayIndex >= array.Length)
					throw new ArgumentException("arrayIndex");
				array[arrayIndex] = _factory.CreateResourceObject(resource);
			}
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
		public virtual bool Remove(T item)
		{
			if(_handlerRemove == null)
				throw new NotSupportedException(IsReadOnly ? "The list is read-only." : "This operation is not supported by the list.");

			if(!Contains(item))
				return false;

			_handlerRemove(this, new ResourceObjectEventArgs(item));

			return true;
		}

		///<summary>
		///Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
		///</summary>
		///
		///<returns>
		///The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
		///</returns>
		///
		public virtual int Count
		{
			get
			{
				return _resources.Count;
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
		public virtual bool IsReadOnly
		{
			get
			{
				return (_handlerAdd != null) && (_handlerRemove != null);
			}
		}

		///<summary>
		///Returns an enumerator that iterates through the collection.
		///</summary>
		///
		///<returns>
		///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
		///</returns>
		///<filterpriority>1</filterpriority>
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return new Enumerator(_resources.GetEnumerator(), _factory);
		}

		///<summary>
		///Returns an enumerator that iterates through a collection.
		///</summary>
		///
		///<returns>
		///An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
		///</returns>
		///<filterpriority>2</filterpriority>
		public virtual IEnumerator GetEnumerator()
		{
			return new Enumerator(_resources.GetEnumerator(), _factory);
		}

		/// <summary>
		/// Implements the enumeration.
		/// </summary>
		public class Enumerator : IEnumerator, IEnumerator<T>
		{
			private readonly IEnumerator _enumResources;

			private readonly IResourceObjectFactory<T> _factory;

			protected T _current = null;

			public Enumerator(IEnumerator enumResources, IResourceObjectFactory<T> factory)
			{
				_enumResources = enumResources;
				_factory = factory;
			}

			///<summary>
			///Advances the enumerator to the next element of the collection.
			///</summary>
			///
			///<returns>
			///true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			///</returns>
			///
			///<exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
			public virtual bool MoveNext()
			{
				if(!_enumResources.MoveNext())
					return false;
				_current = _factory.CreateResourceObject((IResource)_enumResources.Current);
				return true;
			}

			///<summary>
			///Sets the enumerator to its initial position, which is before the first element in the collection.
			///</summary>
			///
			///<exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
			public virtual void Reset()
			{
				_current = null;
				_enumResources.Reset();
			}

			///<summary>
			///Gets the current element in the collection.
			///</summary>
			///
			///<returns>
			///The current element in the collection.
			///</returns>
			///
			///<exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element. </exception><filterpriority>2</filterpriority>
			object IEnumerator.Current
			{
				get
				{
					if(_current == null)
						throw new InvalidOperationException();
					return _current;
				}
			}

			///<summary>
			///Gets the element in the collection at the current position of the enumerator.
			///</summary>
			///
			///<returns>
			///The element in the collection at the current position of the enumerator.
			///</returns>
			///
			public virtual T Current
			{
				get
				{
					if(_current == null)
						throw new InvalidOperationException();
					return _current;
				}
			}

			///<summary>
			///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
			///</summary>
			///<filterpriority>2</filterpriority>
			public virtual void Dispose()
			{
			}
		}
	}
}