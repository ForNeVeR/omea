// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.Jiffa.JiraSoap;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.Jiffa
{
	public class JiraIssueType : ResourceObject, ISyncableTo<RemoteIssueType>
	{
		private readonly JiraServer _server;

		public JiraIssueType(JiraServer server, IResource resource)
			: base(resource)
		{
			_server = server;
		}

		/// <summary>
		/// String values for the JIRA issue keys. Integral values should not be used.
		/// </summary>
		public enum JiraIssueKeys
		{
			assignee,
			affectsVersions,
			attachmentNames,
			components,
			created,
			customFieldValues,
			description,
			duedate,
			environment,
			fixVersions,
			key,
			priority,
			project,
			reporter,
			resolution,
			status,
			summary,
			type,
			updated,
			votes,
		}

		public JiraServer Server
		{
			get
			{
				return _server;
			}
		}

		public void Sync(RemoteIssueType itemJira)
		{
			ResourceProxy proxy = new ResourceProxy(Resource, AsyncPriority);
			proxy.BeginUpdate();
			proxy.SetProp(Core.Props.Name, itemJira.name);
			proxy.SetProp(Core.Props.LongBody, itemJira.description);
			proxy.SetProp(Props.IconUri, itemJira.icon);
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
