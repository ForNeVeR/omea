/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// A factory interface for wrapping raw resources into resource objects.
	/// </summary>
	/// <typeparam name="T">Resource object type.</typeparam>
	public interface IResourceObjectFactory<T> where T : IResourceObject
	{
		/// <summary>
		/// Wraps the resource into a resource object.
		/// </summary>
		/// <param name="resource">The raw Omea resource.</param>
		/// <returns>The resource object attached to <paramref name="resource"/>.</returns>
		T CreateResourceObject(IResource resource);
	}
}