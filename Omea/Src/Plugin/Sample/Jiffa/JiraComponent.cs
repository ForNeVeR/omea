/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.Jiffa.JiraSoap;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.Jiffa
{
	public class JiraComponent : ResourceObject
	{
		private readonly JiraProject _project;

		public JiraComponent(JiraProject project, IResource resource)
			: base(resource)
		{
			_project = project;
		}

		public void Sync(RemoteComponent componentJira)
		{
			ResourceProxy proxy = new ResourceProxy(Resource, AsyncPriority);
			proxy.BeginUpdate();

			proxy.SetProp(Core.Props.Name, componentJira.name);

			if(Async)
				proxy.EndUpdateAsync();
			else
				proxy.EndUpdate();
		}

		public JiraProject Project
		{
			get
			{
				return _project;
			}
		}

		/// <summary>
		/// Gets the item's ID on the JIRA server.
		/// </summary>
		public string JiraId
		{
			get
			{
				return Resource.GetPropText(Props.JiraId);
			}
		}

		/// <summary>
		/// Resource type for the wrappee resources.
		/// </summary>
		public static string Type
		{
			get
			{
				return Types.JiraComponent;
			}
		}
	}
}