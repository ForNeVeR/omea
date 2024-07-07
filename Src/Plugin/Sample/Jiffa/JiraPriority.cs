// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.Jiffa.JiraSoap;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.Jiffa
{
	public class JiraPriority : ResourceObject, ISyncableTo<RemotePriority>
	{
		private readonly JiraServer _server;

		public JiraPriority(JiraServer server, IResource resource)
			: base(resource)
		{
			_server = server;
		}

		public JiraServer Server
		{
			get
			{
				return _server;
			}
		}

		public void Sync(RemotePriority itemJira)
		{
			ResourceProxy proxy = new ResourceProxy(Resource, AsyncPriority);
			proxy.BeginUpdate();
			proxy.SetProp(Core.Props.Name, itemJira.name);
			proxy.SetProp(Core.Props.LongBody, itemJira.description);
			proxy.SetProp(Props.IconUri, itemJira.icon);
			proxy.SetProp(Props.Color, itemJira.color);
			if(Async)
				proxy.EndUpdateAsync();
			else
				proxy.EndUpdate();
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
	}
}
