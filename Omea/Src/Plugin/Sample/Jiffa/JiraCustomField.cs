/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

using JetBrains.Omea.Jiffa.JiraSoap;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.Jiffa
{
	/// <summary>
	/// Describes a server-global custom field in JIRA.
	/// </summary>
	public class JiraCustomField : ResourceObject, ISyncableTo<RemoteField>
	{
		private readonly JiraServer _server;

		/// <summary>
		/// Creates a new instance upon a resource.
		/// </summary>
		public JiraCustomField(JiraServer server, IResource resource)
			: base(resource)
		{
			_server = server;
		}

		/// <summary>
		/// Looks up a custom field on the given server that has the name specified.
		/// <c>Null</c> if none such found.
		/// </summary>
		public static JiraCustomField FromName(JiraServer server, string name)
		{
			if(server == null)
				throw new ArgumentNullException("server");
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			IResourceList reslist = server.Resource.GetLinksTo(Types.JiraCustomField, Core.Props.Parent);
			reslist = reslist.Intersect(Core.ResourceStore.FindResources(Types.JiraCustomField, Core.Props.Name, name), true);

			return reslist.Count > 0 ? new JiraCustomField(server, reslist[0]) : null;
		}

		/// <summary>
		/// Gets the owning server.
		/// </summary>
		public JiraServer Server
		{
			get
			{
				return _server;
			}
		}

		public void Sync(RemoteField itemJira)
		{
			ResourceProxy proxy = new ResourceProxy(Resource, AsyncPriority);
			proxy.BeginUpdate();
			proxy.SetProp(Core.Props.Name, itemJira.name);
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
		/// Gets the name of the JIRA item.
		/// </summary>
		public string Name
		{
			get
			{
				return Resource.DisplayName;
			}
		}
	}
}