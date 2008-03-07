/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

using JetBrains.Omea.CoreServicesEx.Core;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// Implements the Core Extended.
	/// </summary>
	public class CoreEx : ICoreEx
	{
		#region Data

		/// <summary>
		/// The singleton instance field.
		/// </summary>
		private static CoreEx myInstance = null;

		#endregion

		#region Init

		/// <summary>
		/// Private ctor.
		/// </summary>
		private CoreEx()
		{
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the instance of the CoreEx.
		/// </summary>
		public static ICoreEx Instance
		{
			get
			{
				return myInstance ?? (myInstance = new CoreEx());
			}
		}

		#endregion

		#region ICoreEx Members

		/// <summary>
		/// Gets the data object that contains various information, such as resource type and property names and IDs for the commonly-used entities.
		/// </summary>
		ICoreData ICoreEx.Data
		{
			get
			{
				return CoreData.Instance;
			}
		}

		/// <summary>
		/// Gets the data object that contains various information, such as resource type and property names and IDs for the commonly-used entities.
		/// </summary>
		public static ICoreData Data
		{
			get
			{
				return Instance.Data;
			}
		}

		/// <summary>
		/// Gets the Omea Scheduller that executes schedulled tasks.
		/// </summary>
		IScheduller ICoreEx.Scheduller
		{
			get
			{
				return ((ICoreEx)this).GetService<IScheduller>();
			}
		}

		/// <summary>
		/// Gets the Omea Scheduller that executes schedulled tasks.
		/// </summary>
		public static IScheduller Scheduller
		{
			get
			{
				return Instance.Scheduller;
			}
		}

		/// <summary>
		/// Gets the Omea Progress Manager that displays the progress for various lengthy operation and provides the means for controlling the way they run.
		/// </summary>
		IProgressManager ICoreEx.ProgressManager
		{
			get
			{
				return ((ICoreEx)this).GetService<IProgressManager>();
			}
		}

		/// <summary>
		/// Gets the Omea Progress Manager that displays the progress for various lengthy operation and provides the means for controlling the way they run.
		/// </summary>
		public static IProgressManager ProgressManager
		{
			get
			{
				return Instance.ProgressManager;
			}
		}

		/// <summary>
		/// Gets the registered plugin service of the given type.
		/// Throws if there's no such registered.
		/// </summary>
		/// <remarks>To get a service for late-binding calls, or register a new service, use the <see cref="IPluginLoader"/> members.</remarks>
		/// <typeparam name="TService">Type of the service object.</typeparam>
		/// <returns>An instance of the service.</returns>
		TService ICoreEx.GetService<TService>()
		{
			TService service = ((ICoreEx)this).TryGetService<TService>();
			if(service == null)
				throw new ArgumentException(string.Format("There is no service registered of type “{0}”.", typeof(TService).FullName));
			return service;
		}

		/// <summary>
		/// Gets the registered plugin service of the given type.
		/// Throws if there's no such registered.
		/// </summary>
		/// <remarks>To get a service for late-binding calls, or register a new service, use the <see cref="IPluginLoader"/> members.</remarks>
		/// <typeparam name="TService">Type of the service object.</typeparam>
		/// <returns>An instance of the service.</returns>
		public static TService GetService<TService>()
		{
			return Instance.GetService<TService>();
		}

		/// <summary>
		/// Gets the registered plugin service of the given type.
		/// Returns <c>Null</c> if there's no such registered.
		/// </summary>
		/// <remarks>To get a service for late-binding calls, or register a new service, use the <see cref="IPluginLoader"/> members.</remarks>
		/// <typeparam name="TService">Type of the service object.</typeparam>
		/// <returns>An instance of the service, or <c>Null</c>.</returns>
		public static TService TryGetService<TService>()
		{
			return Instance.TryGetService<TService>();
		}

		/// <summary>
		/// Gets the registered plugin service of the given type.
		/// Returns <c>Null</c> if there's no such registered.
		/// </summary>
		/// <remarks>To get a service for late-binding calls, or register a new service, use the <see cref="IPluginLoader"/> members.</remarks>
		/// <typeparam name="TService">Type of the service object.</typeparam>
		/// <returns>An instance of the service, or <c>Null</c>.</returns>
		TService ICoreEx.TryGetService<TService>()
		{
			return (TService)Core.PluginLoader.GetPluginService(typeof(TService));
		}

		/// <summary>
		/// Gets a resource object factory for the given resource object type.
		/// Throws if such is not available.
		/// </summary>
		/// <typeparam name="TResourceObject">Resource object type.</typeparam>
		/// <returns>The factory.</returns>
		public static IResourceObjectFactory<TResourceObject> GetResourceObjectFactory<TResourceObject>() where TResourceObject : IResourceObject
		{
			return Instance.GetResourceObjectFactory<TResourceObject>();
		}

		/// <summary>
		/// Gets a resource object factory for the given resource object type.
		/// Throws if such is not available.
		/// </summary>
		/// <typeparam name="TResourceObject">Resource object type.</typeparam>
		/// <returns>The factory.</returns>
		IResourceObjectFactory<TResourceObject> ICoreEx.GetResourceObjectFactory<TResourceObject>()
		{
			return ((ICoreEx)this).GetService<IResourceObjectFactory<TResourceObject>>();
		}

		/// <summary>
		/// Registers a new resource object factory.
		/// </summary>
		/// <typeparam name="TResourceObject">Type of the resource objects handled by the factory.</typeparam>
		/// <param name="factory">The factory object.</param>
		public static void RegisterResourceObjectFactory<TResourceObject>(IResourceObjectFactory<TResourceObject> factory) where TResourceObject : IResourceObject
		{
			Instance.RegisterResourceObjectFactory(factory);
		}

		/// <summary>
		/// Registers a new resource object factory.
		/// </summary>
		/// <typeparam name="TResourceObject">Type of the resource objects handled by the factory.</typeparam>
		/// <param name="factory">The factory object.</param>
		void ICoreEx.RegisterResourceObjectFactory<TResourceObject>(IResourceObjectFactory<TResourceObject> factory)
		{
			Core.PluginLoader.RegisterPluginService(factory);
		}

		#endregion
	}
}