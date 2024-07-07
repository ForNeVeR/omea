// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Omea.Jiffa.JiraSoap;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.Jiffa
{
	public class JiraUser : ResourceObject, ISyncableTo<RemoteUser>
	{
		private readonly JiraServer _server;

		public JiraUser(JiraServer server, IResource resource)
			: base(resource)
		{
			_server = server;
		}

		/// <summary>
		/// Looks up a user on the given server that has the name specified.
		/// <c>Null</c> if none such found.
		/// </summary>
		public static JiraUser FromName(JiraServer server, string name)
		{
			if(server == null)
				throw new ArgumentNullException("server");
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			IResourceList reslist = server.Resource.GetLinksTo(Types.JiraUser, Core.Props.Parent);
			reslist = reslist.Intersect(Core.ResourceStore.FindResources(Types.JiraUser, Core.Props.Name, name), true);

			return reslist.Count > 0 ? new JiraUser(server, reslist[0]) : null;
		}

		/*
		/// <summary>
		/// Contacts the server, gets the user data and
		/// </summary>
		/// <param name="server"></param>
		/// <param name="username"></param>
		/// <returns></returns>
		public static JiraUser FromJiraTransient(JiraServer server, string username)
		{ }
		 * */

		public JiraServer Server
		{
			get
			{
				return _server;
			}
		}

		public void Sync(RemoteUser itemJira)
		{
			ResourceProxy proxy = new ResourceProxy(Resource, AsyncPriority);
			proxy.BeginUpdate();
			proxy.SetProp(Core.Props.Name, itemJira.name);
			proxy.SetProp(Props.FullName, itemJira.fullname);
			proxy.SetProp(Props.Email, itemJira.email);
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

		/// <summary>
		/// Gets the user's name.
		/// </summary>
		public string Name
		{
			get
			{
				return Resource.GetPropText(Core.Props.Name);
			}
		}

		/// <summary>
		/// Gets the user's full name.
		/// </summary>
		public string FullName
		{
			get
			{
				return Resource.GetPropText(Props.FullName);
			}
		}

		/// <summary>
		/// Gets the user's email address.
		/// </summary>
		public string Email
		{
			get
			{
				return Resource.GetPropText(Props.Email);
			}
		}
	}
}
