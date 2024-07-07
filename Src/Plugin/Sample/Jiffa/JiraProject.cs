// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;

using JetBrains.Omea.Jiffa.JiraSoap;
using JetBrains.Omea.Jiffa.Res;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.Jiffa
{
	public class JiraProject : ResourceObject, IResourceObjectFactory<JiraComponent>, ISyncableTo<RemoteProject>
	{
		public JiraServer Server
		{
			get
			{
				return _server;
			}
		}

		private readonly JiraServer _server;

		//public static class Props { public static readonly string JiraId = ""}
		public JiraProject(JiraServer server, IResource resource)
			: base(resource)
		{
			if(resource == null)
				throw new ArgumentNullException("resource");
			if(!resource.GetLinksFrom(Types.JiraServer, Core.Props.Parent).Contains(server.Resource))
				throw new ArgumentException(Stringtable.Error_WrongParent);
			_server = server;
		}

		/// <summary>
		/// Creates a project object by wrapping a given resource and looking up the server automatically.
		/// </summary>
		public static JiraProject FromResource(IResource resource)
		{
			if(resource == null)
				throw new ArgumentNullException("resource");
			IResourceList servers = resource.GetLinksFrom(Types.JiraServer, Core.Props.Parent);
			if(servers.Count == 0)
				throw new InvalidOperationException(Stringtable.Error_NoParent);
			if(servers.Count > 1)
				throw new InvalidOperationException(Stringtable.Error_MultiParent);

			return new JiraProject(new JiraServer(servers[0]), resource);
		}

		/// <summary>
		/// Looks up a project with the given key.
		/// If there's more than one project with such a key (eg, on different servers), the behavior is undefined.
		/// Throws if a project could not be found.
		/// </summary>
		public static JiraProject FromProjectKey(string key)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException("key");
			IResource resource = Core.ResourceStore.FindUniqueResource(Types.JiraProject, Props.Key, key);
			if(resource == null)
				throw new InvalidOperationException(Stringtable.Error_NoProjectByKey);
			return FromResource(resource);
		}

		public void Sync(RemoteProject projectJira)
		{
			// Sync the project itself
			ResourceProxy proxy = new ResourceProxy(Resource, AsyncPriority);
			proxy.BeginUpdate();
			proxy.SetProp(Props.Key, projectJira.key);
			proxy.SetProp(Core.Props.LongBody, projectJira.description);
			proxy.SetProp(Core.Props.Name, projectJira.name);
			proxy.SetProp(Props.ProjectUri, projectJira.projectUrl);
			proxy.SetProp(Props.Uri, projectJira.url);
			if(Async)
				proxy.EndUpdateAsync();
			else
				proxy.EndUpdate();

			// Sync the components
			SyncComponents();
		}

		public void SyncComponents()
		{
			// Create missing items, update existing items
			Dictionary<IResource, bool> mapLiving = new Dictionary<IResource, bool>(); // Hash-set for the items that are still alive in JIRA
			foreach(RemoteComponent componentJira in Server.Service.getComponents(Server.GetSignInToken(), Key))
			{
				JiraComponent componentLocal = FindComponentByJiraId(componentJira.id);
				if(componentLocal == null)
					componentLocal = CreateComponent(componentJira);
				else
					componentLocal.Undelete();
				componentLocal.Async = false;
				componentLocal.Sync(componentJira);
				mapLiving[componentLocal.Resource] = true;
			}

			// Mark those projects no more in JIRA as deleted
			foreach(JiraComponent componentLocal in Components)
			{
				if(!mapLiving.ContainsKey(componentLocal.Resource))
					componentLocal.Delete();
			}
		}

		private JiraComponent CreateComponent(RemoteComponent componentJira)
		{
			ResourceProxy proxy = ResourceProxy.BeginNewResource(Types.JiraComponent);
			proxy.AsyncPriority = JobPriority.Normal;

			proxy.SetProp(Props.JiraId, componentJira.id);
			proxy.AddLink(Core.Props.Parent, Resource);

			proxy.EndUpdate();

			return GetComponent(proxy.Resource);
		}

		/// <summary>
		/// Gets the Jira Project Key of this project.
		/// </summary>
		public string Key
		{
			get
			{
				return Resource.GetPropText(Props.Key);
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
		/// Gets a list of all the JIRA components defined under this JIRA project.
		/// </summary>
		public IList<JiraComponent> Components
		{
			get
			{
				return new ResourceObjectsList<JiraComponent>(Resource.GetLinksTo(Types.JiraComponent, Core.Props.Parent), this);
			}
		}

		/// <summary>
		/// Resource type for the wrappee resources.
		/// </summary>
		public static string Type
		{
			get
			{
				return Types.JiraProject;
			}
		}

		JiraComponent IResourceObjectFactory<JiraComponent>.CreateResourceObject(IResource resource)
		{
			if(resource == null)
				throw new ArgumentNullException("resource");

			return GetComponent(resource);
		}

		private JiraComponent GetComponent(IResource resource)
		{
			return new JiraComponent(this, resource);
		}

		public JiraComponent FindComponentByJiraId(string id)
		{
			IResourceList resources = Core.ResourceStore.FindResources(Types.JiraComponent, Props.JiraId, id);
			resources = resources.Intersect(Resource.GetLinksTo(Types.JiraComponent, Core.Props.Parent), true);
			if(resources.Count > 0)
				return GetComponent(resources[0]);
			else
				return null;
		}

		public string Uri
		{
			get
			{
				return Resource.GetPropText(Props.Uri);
			} /*set
			{
				if (value == null)
					throw new ArgumentNullException();
				if (value == Uri)
					return;
				WriteProp(Props.Uri, value);
			}*/
		}

		public string Description
		{
			get
			{
				return Resource.GetPropText(Core.Props.LongBody);
			} /*set
			{
				if (value == null)
					throw new ArgumentNullException();
				if (value == Description)
					return;
				WriteProp(Core.Props.LongBody, value);
			}*/
		}

		public string ProjectUri
		{
			get
			{
				return Resource.GetPropText(Props.ProjectUri);
			} /*set
			{
				if (value == null)
					throw new ArgumentNullException();
				if (value == ProjectUri)
					return;
				WriteProp(Props.ProjectUri, value);
			}*/
		}
	}
}
