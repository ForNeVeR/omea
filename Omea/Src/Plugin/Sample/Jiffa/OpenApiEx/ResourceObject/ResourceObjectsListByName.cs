// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// Represents a list of resource objects, accessibly by index, Resource ID, and name.
	/// </summary>
	/// <typeparam name="T">The resource object type.</typeparam>
	public class ResourceObjectsListByName<T> : ResourceObjectsList<T>, IResourceObjectsListByName<T> where T : class, IResourceObject
	{
		/// <summary>
		/// ID for the property that holds the object name.
		/// </summary>
		protected int _nNamePropId;

		/// <summary>
		/// A handler for creating new objects and adding them to the list.
		/// </summary>
		protected readonly EventHandler<ResourceObjectOutByNameEventArgs<T>> _handlerCreate;

		/// <summary>
		/// Creates a list from a resource list, a factory that wraps the raw resources into resource objects, and the name property ID.
		/// </summary>
		/// <param name="resources">The list of available resources, live or dead.</param>
		/// <param name="factory">The factory for wrapping the raw Omea resources into resource objects.</param>
		/// <param name="nNamePropId">ID of the property that should be looked for when getting resources by name.</param>
		/// <param name="handlerAdd">A handler for adding new resources to the list. May be <c>Null</c>.</param>
		/// <param name="handlerRemove">A handler for adding new resources to the list. May be <c>Null</c>.</param>
		/// <param name="handlerCreate">A handler for creating new resources and including them into the list. May be <c>Null</c>.</param>
		public ResourceObjectsListByName(IResourceList resources, IResourceObjectFactory<T> factory, int nNamePropId, EventHandler<ResourceObjectEventArgs> handlerAdd, EventHandler<ResourceObjectEventArgs> handlerRemove, EventHandler<ResourceObjectOutByNameEventArgs<T>> handlerCreate)
			: base(resources, factory, handlerAdd, handlerRemove)
		{
			_nNamePropId = nNamePropId;
			_handlerCreate = handlerCreate;
		}

		/// <summary>
		/// Creates a list from a resource list, a factory that wraps the raw resources into resource objects, and the name property ID.
		/// </summary>
		/// <param name="resources">The list of available resources, live or dead.</param>
		/// <param name="factory">The factory for wrapping the raw Omea resources into resource objects.</param>
		/// <param name="nNamePropId">ID of the property that should be looked for when getting resources by name.</param>
		public ResourceObjectsListByName(IResourceList resources, IResourceObjectFactory<T> factory, int nNamePropId)
			: this(resources, factory, nNamePropId, null, null, null)
		{
		}

		/// <summary>
		/// Creates a list from a resource list, a factory that wraps the raw resources into resource objects, and the name property name.
		/// </summary>
		/// <param name="resources">The list of available resources, live or dead.</param>
		/// <param name="factory">The factory for wrapping the raw Omea resources into resource objects.</param>
		/// <param name="sNamePropName">Name of the property that should be looked for when getting resources by name.</param>
		public ResourceObjectsListByName(IResourceList resources, IResourceObjectFactory<T> factory, string sNamePropName)
			: this(resources, factory, Core.ResourceStore.PropTypes[sNamePropName].Id)
		{
		}

		/// <summary>
		/// Creates a list from a resource list and a factory that wraps the raw resources into resource objects.
		/// The standard Name property is used when looking for resources by their name.
		/// </summary>
		/// <param name="resources">The list of available resources, live or dead.</param>
		/// <param name="factory">The factory for wrapping the raw Omea resources into resource objects.</param>
		public ResourceObjectsListByName(IResourceList resources, IResourceObjectFactory<T> factory)
			: this(resources, factory, Core.Props.Name)
		{
		}

		#region IResourceObjectsListByName<T> Members

		/// <summary>
		/// Gets a resource object by its name.
		/// Throws if there is no such resource in the list.
		/// If there's more than one, returns any (undefined behavior).
		/// </summary>
		/// <param name="name">Name of the resource to look up.</param>
		/// <returns>The resource object.</returns>
		public T GetByName(string name)
		{
			IResourceList resNamed = Core.ResourceStore.FindResources(null, _nNamePropId, name).Intersect(_resources, true);
			if(resNamed.Count == 0)
				throw new ArgumentOutOfRangeException("name", name, "There is no such a named resource in the list.");
			return _factory.CreateResourceObject(resNamed[0]); // Ignore multiple resources.
		}

		/// <summary>
		/// Gets a resource object by its name.
		/// Returns <c>Null</c> there is no such resource in the list.
		/// If there's more than one, returns any (undefined behavior).
		/// </summary>
		/// <param name="name">Name of the resource to look up.</param>
		/// <returns>The resource object, or <c>Null</c> if not found.</returns>
		public T TryGetByName(string name)
		{
			IResourceList resNamed = Core.ResourceStore.FindResources(null, _nNamePropId, name).Intersect(_resources, true);
			if(resNamed.Count == 0)
				return null;
			return _factory.CreateResourceObject(resNamed[0]); // Ignore multiple resources.
		}

		/// <summary>
		/// Gets a resource object by its name, same as <see cref="GetByName"/>.
		/// Throws if there is no such resource in the list.
		/// If there's more than one, returns any (undefined behavior).
		/// </summary>
		/// <param name="name">Name of the resource to look up.</param>
		/// <returns>The resource object.</returns>
		public T this[string name]
		{
			get
			{
				return GetByName(name);
			}
		}

		/// <summary>
		/// Gets the list of all the resources with such a name in the collection.
		/// If there are none such found, returns an empty list.
		/// </summary>
		/// <param name="name">Name of the resources to look up.</param>
		/// <returns>The list of matching resources.</returns>
		public IResourceObjectsList<T> GetAllByName(string name)
		{
			IResourceList resNamed = Core.ResourceStore.FindResources(null, _nNamePropId, name).Intersect(_resources, true);
			return new ResourceObjectsList<T>(resNamed, _factory);
		}

		/// <summary>
		/// Creates a new item and adds it to the list.
		/// May be unsupported by a particular list, in which case a <see cref="NotSupportedException"/> will be thrown.
		/// </summary>
		/// <param name="name">The name for the new object.</param>
		/// <returns>The newly-created object.</returns>
		public T Create(string name)
		{
			if(name == null)
				throw new ArgumentNullException("name");
			if(_handlerCreate == null)
				throw new NotSupportedException(IsReadOnly ? "The list is read-only." : "This operation is not supported by the list.");

			ResourceObjectOutByNameEventArgs<T> args = new ResourceObjectOutByNameEventArgs<T>(name);
			_handlerCreate(this, args);

			if(args.ResourceObject == null)
				throw new InvalidOperationException(string.Format("The handler has failed to create a resource object named “{0}”.", name));

			return args.ResourceObject;
		}

		#endregion

		///<summary>
		///Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only.
		///</summary>
		///
		///<returns>
		///true if the <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only; otherwise, false.
		///</returns>
		///
		public override bool IsReadOnly
		{
			get
			{
				return (base.IsReadOnly) && (_handlerCreate == null);
			}
		}
	}
}
