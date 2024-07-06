// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using JetBrains.Omea.Jiffa.JiraSoap;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.Jiffa
{
	public class JiraServer : ResourceObject
	{
		/// <summary>
		/// The JIRA SOAP interface.
		/// </summary>
		private JiraSoapServiceService _service = null;

		public static void Register(IPlugin owner)
		{
			string sNameName = Core.ResourceStore.PropTypes[Core.Props.Name].Name;

			Core.ResourceStore.ResourceTypes.Register(Types.JiraServer, Types.JiraServer, sNameName, ResourceTypeFlags.ResourceContainer, owner);
			Core.ResourceStore.ResourceTypes.Register(Types.JiraProject, Types.JiraProject, sNameName, ResourceTypeFlags.ResourceContainer, owner);
			Core.ResourceStore.ResourceTypes.Register(Types.JiraComponent, Types.JiraComponent, sNameName, ResourceTypeFlags.ResourceContainer, owner);
			Core.ResourceStore.ResourceTypes.Register(Types.JiraIssueType, Types.JiraIssueType, sNameName, ResourceTypeFlags.Normal, owner);
			Core.ResourceStore.ResourceTypes.Register(Types.JiraStatus, Types.JiraStatus, sNameName, ResourceTypeFlags.Normal, owner);
			Core.ResourceStore.ResourceTypes.Register(Types.JiraPriority, Types.JiraPriority, sNameName, ResourceTypeFlags.Normal, owner);
			Core.ResourceStore.ResourceTypes.Register(Types.JiraCustomField, Types.JiraCustomField, sNameName, ResourceTypeFlags.Normal, owner);
			Core.ResourceStore.ResourceTypes.Register(Types.JiraUser, Types.JiraUser, sNameName, ResourceTypeFlags.Normal, owner);

			Core.ResourceIconManager.RegisterResourceIconProvider(new string[] {Type, JiraProject.Type, JiraComponent.Type, Types.JiraIssueType, Types.JiraPriority, Types.JiraStatus}, new JiffaIconProvider());

			/*
			workspaceMgr.RegisterWorkspaceType(_newsGroup, new int[] { -_propTo }, WorkspaceResourceType.Container);
			workspaceMgr.RegisterWorkspaceFolderType(_newsServer, _newsGroup, new int[] { -Core.Props.Parent });
			workspaceMgr.RegisterWorkspaceFolderType(_newsFolder, _newsGroup, new int[] { -Core.Props.Parent });
			workspaceMgr.RegisterWorkspaceType(_newsArticle,
				new int[] { -_propAttachment }, WorkspaceResourceType.None);*/

			foreach(string sPropName in Props.StringProps)
				Core.ResourceStore.PropTypes.Register(sPropName, PropDataType.String, PropTypeFlags.Normal, owner);
		}

		/// <summary>
		/// Downloads all the projects, components, etc from the server and creates resources for them.
		/// </summary>
		public void Sync()
		{
			List<KeyValuePair<string, MethodInvoker>> arSyncActions = new List<KeyValuePair<string, MethodInvoker>>();

			// Prepare the list of sync actions
			arSyncActions.Add(new KeyValuePair<string, MethodInvoker>("JIRA Projects", delegate { new ServerChildHelper<JiraProject, RemoteProject>(this, Types.JiraProject).SyncAll(Service.getProjectsNoSchemes); }));
			arSyncActions.Add(new KeyValuePair<string, MethodInvoker>("JIRA Issue Types", delegate { new ServerChildHelper<JiraIssueType, RemoteIssueType>(this, Types.JiraIssueType).SyncAll(Service.getIssueTypes); }));
			arSyncActions.Add(new KeyValuePair<string, MethodInvoker>("JIRA Issue Statuses", delegate { new ServerChildHelper<JiraStatus, RemoteStatus>(this, Types.JiraStatus).SyncAll(Service.getStatuses); }));
			arSyncActions.Add(new KeyValuePair<string, MethodInvoker>("JIRA Issue Priorities", delegate { new ServerChildHelper<JiraPriority, RemotePriority>(this, Types.JiraPriority).SyncAll(Service.getPriorities); }));
			//arSyncActions.Add(new KeyValuePair<string, MethodInvoker>("JIRA Issue Custom Fields", delegate { new ServerChildHelper<JiraCustomField, RemoteField>(this, Types.JiraCustomField).SyncAll(Service.getCustomFields); }));
			//arSyncActions.Add(new KeyValuePair<string, MethodInvoker>("JIRA Users", delegate { new ServerChildHelper<JiraUser, RemoteUser>(this, Types.JiraUser).SyncAll(Service.getUser); }));

			// Invoke the syncers
			StringBuilder sb = new StringBuilder();
			foreach(KeyValuePair<string, MethodInvoker> pair in arSyncActions)
			{
				try
				{
					pair.Value(); // Exec!
				}
				catch(Exception ex)
				{
					sb.AppendFormat("Failed to sync {0}. {1}", pair.Key, ex.Message);
					sb.AppendLine();
				}
			}

			// Show the errors, if any
			if(sb.Length > 0)
				Core.UserInterfaceAP.QueueJob(Jiffa.Name + " Sync Error Message", ((MethodInvoker)delegate { MessageBox.Show(Core.MainWindow, string.Format("There were errors syncing to the “{0}” JIRA server.\n\n{1}", Name, sb), Jiffa.Name, MessageBoxButtons.OK, MessageBoxIcon.Information); }));
		}

		/*
		protected void SyncProjects()
		{
			// Create missing projects, update existing projects
			Dictionary<IResource, bool> mapLiveProjects = new Dictionary<IResource, bool>(); // Hash-set for the projects that are still alive in JIRA
			foreach(RemoteProject projectJira in Service.getProjects(GetSignInToken()))
			{
				JiraProject projectLocal = FindProjectByJiraId(projectJira.id);
				if(projectLocal == null)
					projectLocal = CreateProject(projectJira);
				else
					projectLocal.Undelete();
				projectLocal.Async = false;
				projectLocal.Sync(projectJira);
				mapLiveProjects[projectLocal.Resource] = true;
			}

			// Mark those projects no more in JIRA as deleted
			foreach(JiraProject projectLocal in Projects)
			{
				if(!mapLiveProjects.ContainsKey(projectLocal.Resource))
					projectLocal.Delete();
			}
		}*/

		/*
		protected void SyncChildren<TLocal, TJira>(GetChildrenFromJiraDelegate<TJira> delegateGetChildren, FindChildByJiraId<TLocal> delegateFindChild) where TJira : AbstractNamedRemoteEntity where TLocal : ResourceObject
		{
			// Create missing items, update existing items
			Dictionary<IResource, bool> mapLiving = new Dictionary<IResource, bool>(); // Hash-set for the items that are still alive in JIRA
			foreach(TJira itemJira in delegateGetChildren(GetSignInToken()))
			{
				TLocal itemLocal = delegateFindChild(itemJira.id);
				if(itemLocal == null)
					itemLocal = CreateComponent(componentJira);
				else
					itemLocal.Undelete();
				itemLocal.Async = false;
				itemLocal.Sync(componentJira);
				mapLiving[itemLocal.Resource] = true;
			}

			// Mark those projects no more in JIRA as deleted
			foreach(JiraComponent componentLocal in Components)
			{
				if(!mapLiving.ContainsKey(componentLocal.Resource))
					componentLocal.Delete();
			}
		}*/

		/// <summary>
		/// Gets or sets the username for this server connection.
		/// </summary>
		[Browsable(true)]
		[Category("Credentials")]
		[Description("Username for JIRA sign-in.")]
		public string Username
		{
			get
			{
				return Resource.GetPropText(Props.Username);
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();
				WriteProp(Props.Username, value);
			}
		}

		/// <summary>
		/// Gets or sets the password for this server connection.
		/// Note: due to security reasons, the password returned is always a mock string.
		/// </summary>
		[Browsable(true)]
		[Category("Credentials")]
		[Description("Password for JIRA sign-in.")]
		public string Password
		{
			get
			{
				return "•••••";
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();
				WriteProp(Props.Password, value);
			}
		}

		/// <summary>
		/// Gets the password, as it's stored in the database.
		/// </summary>
		protected string GetPasswordRaw()
		{
			return Resource.GetPropText(Props.Password);
		}

		/// <summary>
		/// Gets or sets the Web Service URI of this JIRA server.
		/// </summary>
		[Browsable(true)]
		[Category("General")]
		[Description("URI of the JIRA server SOAP interface.")]
		public string Uri
		{
			get
			{
				return Resource.GetPropText(Props.Uri);
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();
				if(value == Uri)
					return;
				WriteProp(Props.Uri, value);
				_service = null; // Recreate the service for the new URI
			}
		}

		/// <summary>
		/// Gets or sets the JIRA server user-readable name.
		/// </summary>
		[Browsable(true)]
		[Category("General")]
		[Description("Display name for the server.")]
		public string Name
		{
			get
			{
				return Resource.GetPropText(Core.Props.Name);
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();
				WriteProp(Core.Props.Name, value);
			}
		}

		[Browsable(false)]
		public JiraSoapServiceService Service
		{
			get
			{
				if(_service == null)
				{
					_service = new JiraSoapServiceService();
					_service.Url = Uri;
				}
				return _service ?? (_service = new JiraSoapServiceService());
			}
		}

		public JiraServer(IResource resource)
			: base(resource)
		{
		}

		public JiraProject FindProjectByJiraId(string id)
		{
			IResourceList resources = Core.ResourceStore.FindResources(Types.JiraProject, Props.JiraId, id);
			resources = resources.Intersect(Resource.GetLinksTo(Types.JiraProject, Core.Props.Parent), true);
			if(resources.Count > 0)
				return GetProject(resources[0]);
			else
				return null;
		}

		public JiraProject GetProject(IResource resource)
		{
			return new JiraProject(this, resource);
		}

		/// <summary>
		/// Gets a list of all the JIRA projects under this JIRA server.
		/// </summary>
		[Browsable(false)]
		public IList<JiraProject> Projects
		{
			get
			{
				return new ServerChildHelper<JiraProject, RemoteProject>(this, Types.JiraProject).GetList();
			}
		}

		/// <summary>
		/// Gets a list of all the JIRA projects under this JIRA server.
		/// </summary>
		[Browsable(false)]
		public IList<JiraIssueType> IssueTypes
		{
			get
			{
				return new ServerChildHelper<JiraIssueType, RemoteIssueType>(this, Types.JiraIssueType).GetList();
			}
		}

		/// <summary>
		/// Gets a list of all the JIRA projects under this JIRA server.
		/// </summary>
		[Browsable(false)]
		public IList<JiraStatus> Statuses
		{
			get
			{
				return new ServerChildHelper<JiraStatus, RemoteStatus>(this, Types.JiraStatus).GetList();
			}
		}

		/// <summary>
		/// Gets a list of all the JIRA projects under this JIRA server.
		/// </summary>
		[Browsable(false)]
		public IList<JiraPriority> Priorities
		{
			get
			{
				return new ServerChildHelper<JiraPriority, RemotePriority>(this, Types.JiraPriority).GetList();
			}
		}

		/// <summary>
		/// While syncing to JIRA, creates a new Omea resource for the locally-missing project.
		/// </summary>
		/// <returns></returns>
		protected JiraProject CreateProject(RemoteProject projectJira)
		{
			ResourceProxy proxy = ResourceProxy.BeginNewResource(Types.JiraProject);
			proxy.AsyncPriority = JobPriority.Normal;

			proxy.SetProp(Props.JiraId, projectJira.id);
			proxy.AddLink(Core.Props.Parent, Resource);

			proxy.EndUpdate();

			return GetProject(proxy.Resource);
		}

		/// <summary>
		/// Gets a sign-in token for working with the <see cref="Service"/>.
		/// </summary>
		/// <returns>A string to be passed into various <see cref="Service"/>'s methods.</returns>
		public string GetSignInToken()
		{
			return Service.login(Username, GetPasswordRaw());
		}

		/// <summary>
		/// Creates a new JIRA server resource.
		/// </summary>
		public static JiraServer CreateNew()
		{
			ResourceProxy proxy = ResourceProxy.BeginNewResource(Type);
			proxy.AddLink(Core.Props.Parent, RootResource);
			proxy.EndUpdate();

			JiraServer retval = new JiraServer(proxy.Resource);
			retval.Name = Jiffa.GetRandomName();

			return retval;
		}

		/// <summary>
		/// Resource type for the wrappee resources.
		/// </summary>
		public static string Type
		{
			get
			{
				return Types.JiraServer;
			}
		}

		/// <summary>
		/// Gets the root resource to which all of the <see cref="JiraServer"/> resources should be linked as children with a parent link.
		/// </summary>
		public static IResource RootResource
		{
			get
			{
				return Core.ResourceTreeManager.GetRootForType(Type);
			}
		}

		/// <summary>
		/// Displays a property sheet window for editing the server properties.
		/// </summary>
		public void ShowPropertySheet()
		{
			new ServerPropertiesSheet(this).Show(Core.MainWindow);
		}
	}

	public class ServerChildHelper<TResourceObject, TJira> : IResourceObjectFactory<TResourceObject> where TResourceObject : class, IResourceObject where TJira : AbstractNamedRemoteEntity
	{
		private readonly JiraServer _server;

		private readonly string _restype;

		public ServerChildHelper(JiraServer server, string restype)
		{
			_server = server;
			_restype = restype;
		}

		public IList<TResourceObject> GetList()
		{
			return new ResourceObjectsList<TResourceObject>(_server.Resource.GetLinksTo(_restype, Core.Props.Parent), this);
		}

		public delegate TJira[] GetChildrenFromJiraDelegate(string token);

		public void SyncAll(GetChildrenFromJiraDelegate delegateGetChildren)
		{
			// Create missing items, update existing items
			Dictionary<IResource, bool> mapLiving = new Dictionary<IResource, bool>(); // Hash-set for the items that are still alive in JIRA
			foreach(TJira itemJira in delegateGetChildren(_server.GetSignInToken()))
			{
				TResourceObject itemLocal = FindByJiraId(itemJira.id);
				if(itemLocal == null)
					itemLocal = CreateNewResource(itemJira);
				else
					itemLocal.Undelete();
				itemLocal.Async = false;
				((ISyncableTo<TJira>)itemLocal).Sync(itemJira);

				mapLiving[itemLocal.Resource] = true;
			}

			// Mark those projects no more in JIRA as deleted
			foreach(TResourceObject componentLocal in GetList())
			{
				if(!mapLiving.ContainsKey(componentLocal.Resource))
					componentLocal.Delete();
			}
		}

		private TResourceObject CreateNewResource(TJira itemJira)
		{
			ResourceProxy proxy = ResourceProxy.BeginNewResource(_restype);

			proxy.SetProp(Props.JiraId, itemJira.id);
			proxy.AddLink(Core.Props.Parent, _server.Resource);

			proxy.EndUpdate();

			return CreateResourceObject(proxy.Resource);
		}

		public TResourceObject CreateResourceObject(IResource resource)
		{
			if(resource == null)
				throw new ArgumentNullException("resource");

			ConstructorInfo ctor = typeof(TResourceObject).GetConstructor(new Type[] {typeof(JiraServer), typeof(IResource)});
			return (TResourceObject)ctor.Invoke(new object[] {_server, resource});
		}

		public TResourceObject FindByJiraId(string id)
		{
			IResourceList resources = Core.ResourceStore.FindResources(_restype, Props.JiraId, id);
			resources = resources.Intersect(_server.Resource.GetLinksTo(_restype, Core.Props.Parent), true);
			if(resources.Count > 0)
				return CreateResourceObject(resources[0]);
			else
				return null;
		}
	}
}
