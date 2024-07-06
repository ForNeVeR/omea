// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// An interface for the list of resource objects, accessibly by index, Resource ID, and name.
	/// </summary>
	/// <typeparam name="T">The resource object type.</typeparam>
	public interface IResourceObjectsListByName<T> : IResourceObjectsList<T> where T : class, IResourceObject
	{
		/// <summary>
		/// Gets a resource object by its name.
		/// Throws if there is no such resource in the list.
		/// If there's more than one, returns any (undefined behavior).
		/// </summary>
		/// <param name="name">Name of the resource to look up.</param>
		/// <returns>The resource object.</returns>
		T GetByName(string name);

		/// <summary>
		/// Gets a resource object by its name.
		/// Returns <c>Null</c> there is no such resource in the list.
		/// If there's more than one, returns any (undefined behavior).
		/// </summary>
		/// <param name="name">Name of the resource to look up.</param>
		/// <returns>The resource object, or <c>Null</c> if not found.</returns>
		T TryGetByName(string name);

		/// <summary>
		/// Gets a resource object by its name, same as <see cref="GetByName"/>.
		/// Throws if there is no such resource in the list.
		/// If there's more than one, returns any (undefined behavior).
		/// </summary>
		/// <param name="name">Name of the resource to look up.</param>
		/// <returns>The resource object.</returns>
		T this[string name] { get; }

		/// <summary>
		/// Gets the list of all the resources with such a name in the collection.
		/// If there are none such found, returns an empty list.
		/// </summary>
		/// <param name="name">Name of the resources to look up.</param>
		/// <returns>The list of matching resources.</returns>
		IResourceObjectsList<T> GetAllByName(string name);

		/// <summary>
		/// Creates a new item and adds it to the list.
		/// May be unsupported by a particular list, in which case a <see cref="NotSupportedException"/> will be thrown.
		/// </summary>
		/// <param name="name">The name for the new object.</param>
		/// <returns>The newly-created object.</returns>
		T Create(string name);
	}
}
