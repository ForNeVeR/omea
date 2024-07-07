// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// Represents an extension to the Omea Core interface.
	/// </summary>
	public interface ICoreEx
	{
		/// <summary>
		/// Gets the data object that contains various information, such as resource type and property names and IDs for the commonly-used entities.
		/// </summary>
		ICoreData Data { get; }

		/// <summary>
		/// Gets the Omea Scheduller that executes schedulled tasks.
		/// </summary>
		IScheduller Scheduller { get; }

		/// <summary>
		/// Gets the Omea Progress Manager that displays the progress for various lengthy operation and provides the means for controlling the way they run.
		/// </summary>
		IProgressManager ProgressManager { get; }

		/// <summary>
		/// Gets the registered plugin service of the given type.
		/// Throws if there's no such registered.
		/// </summary>
		/// <remarks>To get a service for late-binding calls, or register a new service, use the <see cref="IPluginLoader"/> members.</remarks>
		/// <typeparam name="TService">Type of the service object.</typeparam>
		/// <returns>An instance of the service.</returns>
		TService GetService<TService>();

		/// <summary>
		/// Gets the registered plugin service of the given type.
		/// Returns <c>Null</c> if there's no such registered.
		/// </summary>
		/// <remarks>To get a service for late-binding calls, or register a new service, use the <see cref="IPluginLoader"/> members.</remarks>
		/// <typeparam name="TService">Type of the service object.</typeparam>
		/// <returns>An instance of the service, or <c>Null</c>.</returns>
		TService TryGetService<TService>();

		/// <summary>
		/// Gets a resource object factory for the given resource object type.
		/// Throws if such is not available.
		/// </summary>
		/// <typeparam name="TResourceObject">Resource object type.</typeparam>
		/// <returns>The factory.</returns>
		IResourceObjectFactory<TResourceObject> GetResourceObjectFactory<TResourceObject>() where TResourceObject : IResourceObject;

		/// <summary>
		/// Registers a new resource object factory.
		/// </summary>
		/// <typeparam name="TResourceObject">Type of the resource objects handled by the factory.</typeparam>
		/// <param name="factory">The factory object.</param>
		void RegisterResourceObjectFactory<TResourceObject>(IResourceObjectFactory<TResourceObject> factory) where TResourceObject : IResourceObject;
	}
}
