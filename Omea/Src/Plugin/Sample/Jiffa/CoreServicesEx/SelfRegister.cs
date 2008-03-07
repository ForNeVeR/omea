/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.CoreServicesEx.ProgressManager;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.CoreServicesEx
{
	/// <summary>
	/// Performs the self-registration of the Progress Manager.
	/// </summary>
	internal class SelfRegister : IPlugin
	{
		#region IPlugin Members

		///<summary>
		///
		///            Registers the plugin resource types, actions and other services.
		///            
		///</summary>
		///
		///<remarks>
		///
		///<para>
		///This is the first method called after the plugin is loaded. It should
		///            be used to register any resource types or services that could be used by other plugins.
		///</para>
		///
		///<para>
		///To access the services provided by the core, methods of the static class
		///            <see cref="T:JetBrains.Omea.OpenAPI.Core" /> can be used. All core services are already available when this
		///            method is called.
		///</para>
		///
		///</remarks>
		///
		public void Register()
		{
			string sNameName = OpenAPI.Core.ResourceStore.PropTypes[OpenAPI.Core.Props.Name].Name;

			OpenAPI.Core.ResourceStore.ResourceTypes.Register(ProgressManagerData._sProgressItemResourceTypeName, ProgressManagerData._sProgressItemResourceTypeName, sNameName, ResourceTypeFlags.Normal, this);
			OpenAPI.Core.ResourceStore.ResourceTypes.Register(ProgressManagerData._sFolderResourceTypeName, ProgressManagerData._sFolderResourceTypeName, sNameName, ResourceTypeFlags.ResourceContainer);

			OpenAPI.Core.ResourceStore.PropTypes.Register(ProgressManagerData._sMultiParentLinkName, PropDataType.Link, PropTypeFlags.DirectedLink);

			OpenAPI.Core.PluginLoader.RegisterPluginService((IProgressManager)(ProgressManager.ProgressManager.Instance));
		}

		///<summary>
		///
		///            Performs the longer initialization activities of the plugin and starts up
		///            background activities, if any are necessary.
		///            
		///</summary>
		///
		///<remarks>
		///
		///<para>
		///This is the second method called in the plugin startup sequence.
		///            It is called after the <see cref="M:JetBrains.Omea.OpenAPI.IPlugin.Register" /> method has already been called for
		///            all plugins, so the code in this method can use the services provided by other
		///            plugins.
		///</para>
		///
		///<para>
		///To access the services provided by the core, methods of the static class
		///            <see cref="T:JetBrains.Omea.OpenAPI.Core" /> can be used. All core services are already available when this
		///            method is called.
		///</para>
		///
		///</remarks>
		///
		public void Startup()
		{
		}

		///<summary>
		///
		///            Terminates the plugin.
		///            
		///</summary>
		///
		///<remarks>
		///If the plugin needs any shutdown activities (like deleting temporary
		///            files), these should be performed in these method. All <see cref="T:JetBrains.Omea.OpenAPI.Core" /> services 
		///            are still available when the method is called.
		///</remarks>
		///
		public void Shutdown()
		{
		}

		#endregion
	}
}