// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
