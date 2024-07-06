// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OpenApiEx
{
	public interface IResourceObject : IComparable
	{
		/// <summary>
		/// Gets the underlying resource storing the object data.
		/// </summary>
		IResource Resource { get; }

		/// <summary>
		/// Gets the ID of the resource.
		/// </summary>
		int Id { get; }

		/// <summary>
		/// Gets or sets whether all the write-operations to the resource properties should be asynchronous.
		/// </summary>
		bool Async { get; set; }

		/// <summary>
		/// Gets or sets the async operations priority (applies only when <see cref="Async"/> is <c>True</c>).
		/// </summary>
		JobPriority AsyncPriority { get; set; }

		/// <summary>
		/// Marks the resource as deleted.
		/// </summary>
		void Delete();

		/// <summary>
		/// Unmarks the resource as deleted.
		/// </summary>
		void Undelete();
	}
}
