// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections.Generic;

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// An interface for the list of resource objects, accessibly by index and Resource ID.
	/// </summary>
	/// <typeparam name="T">The resource object type.</typeparam>
	public interface IResourceObjectsList<T> : IList<T> where T : class, IResourceObject
	{
		/// <summary>
		/// Gets the resource list wrapped by this list.
		/// </summary>
		IResourceList Resources { get; }

		/// <summary>
		/// Gets a resource object by the database ID of an Omea resource.
		/// Throws if there is no such resource in the list.
		/// </summary>
		/// <param name="id">The Omea resource database ID.</param>
		/// <returns>The resource object.</returns>
		T GetById(int id);

		/// <summary>
		/// Gets a resource object by the database ID of an Omea resource.
		/// Returns <c>Null</c> if there is no such resource in the list.
		/// </summary>
		/// <param name="id">The Omea resource database ID.</param>
		/// <returns>The resource object, or <c>Null</c> if not found.</returns>
		T TryGetById(int id);
	}
}
