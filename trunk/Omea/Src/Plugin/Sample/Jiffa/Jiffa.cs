/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using JetBrains.Omea.GUIControls;
using JetBrains.Omea.Jiffa.Res;
using JetBrains.Omea.Nntp;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Jiffa
{
	[PluginDescription("(H) Serge Baltic", "JIRA connectivity.")]
	public class Jiffa : IPlugin, IJiffaService
	{
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
			try
			{
				Core.PluginLoader.RegisterPluginService(this);

				RegisterActions();

				JiraServer.Register(this);
				SubmitterOptionsPane.Register(this);

				Core.TabManager.RegisterResourceTypeTab(Name, Stringtable.Jira, new string[] {Types.JiraServer, Types.JiraProject, Types.JiraComponent, Types.JiraIssueType, Types.JiraPriority, Types.JiraStatus, Types.JiraCustomField, Types.JiraUser}, Core.Props.Parent, 10);
				IResourceTreePane pane = Core.LeftSidebar.RegisterResourceStructureTreePane(Name, Name, Stringtable.Jira, null, JiraServer.Type);
				pane.ParentProperty = Core.Props.Parent;
				pane.WorkspaceFilterTypes = new string[] {Types.JiraServer, Types.JiraProject, Types.JiraComponent, Types.JiraIssueType, Types.JiraPriority, Types.JiraStatus, Types.JiraCustomField, Types.JiraUser};
				pane.AddNodeFilter(new ResourceTreePaneNodeFilter());
			}
			catch(Exception ex)
			{
				Core.ReportException(ex, false);
			}
		}

		/// <summary>
		/// Registers the main menu, context menu, etc actions.
		/// </summary>
		protected void RegisterActions()
		{
			IAction actionSubmit = new SubmitNntpMessageToJiraAction();
			//IAction actionSample = new MethodInvokerAction(OnTestAction, null);
			IAction actionServerProperties = new MethodInvokerAction(OnServerProperties, null);
			IAction actionAddServer = new MethodInvokerAction(OnAddServer, null);
			IAction actionDeleteServer = new MethodInvokerAction(OnDeleteServer, null);

			string sGroupId;
			Core.ActionManager.RegisterContextMenuActionGroup(sGroupId = Name, ListAnchor.Last);
			Core.ActionManager.RegisterContextMenuAction(actionSubmit, sGroupId, ListAnchor.Last, Stringtable.SubmitToJira, null, "Article", null);
			Core.ActionManager.RegisterContextMenuAction(actionServerProperties, sGroupId, ListAnchor.Last, Stringtable.JiraServerProperties, null, JiraServer.Type, null);
			Core.ActionManager.RegisterContextMenuAction(actionAddServer, sGroupId, ListAnchor.Last, Stringtable.AddJiraServer,null, null, new IActionStateFilter[] {new JiraResourceTreePaneFilter()});
			Core.ActionManager.RegisterContextMenuAction(actionDeleteServer, sGroupId, ListAnchor.Last, Stringtable.DeleteJiraServer, null, JiraServer.Type, null);

			/*
			Core.ActionManager.RegisterMainMenu(Stringtable.Jira, ListAnchor.Last);
			Core.ActionManager.RegisterMainMenuActionGroup(sGroupId, Stringtable.Jira, ListAnchor.Last);
			Core.ActionManager.RegisterMainMenuAction(actionSubmit, sGroupId, ListAnchor.Last, Stringtable.SubmitToJira, null, null);
			 * */

			//IActionStateFilter[] filters = new IActionStateFilter[] { new PhoboTabFilter() };

			//Core.ActionManager.RegisterMainMenuAction(actionSample, sGroupId, ListAnchor.Last, "Jiffa Action", null, new IActionStateFilter[] {});

			// Context Menu
			//Core.ActionManager.RegisterContextMenuActionGroup(sGroup = "PhoboContext", ListAnchor.Last);
			//Core.ActionManager.RegisterContextMenuAction(actProperties, sGroup, ListAnchor.Last, "PropertiesЕ", Const.TypeName, null);
			//Core.ActionManager.RegisterContextMenuAction(actionSample, sGroupId, ListAnchor.Last, "Jiffa Action", null, null);
		}

		private class JiraResourceTreePaneFilter : IActionStateFilter
		{
			///<summary>
			///
			///            For the specified context, updates the presentation state of an action.
			///            
			///</summary>
			///
			///<param name="context">
			///              Context, containing information about resources to which the action will be applied.
			///            </param>
			///<param name="presentation">
			///              The state of the UI element which presents the action to the user. For the
			///              first filter in the chain, the presentation is initialized with the default
			///              values. For subsequent filters, it contains the data set by previous filters.
			///            </param>
			public void Update(IActionContext context, ref ActionPresentation presentation)
			{
				if(context.SelectedResources.Count != 0)
				{
					presentation.Visible = false;
					return;
				}
				if(context.Instance is JetResourceTreePane)
				{
					presentation.Visible = true;
					presentation.Enabled = true;
					return;
				}

				presentation.Visible = false;
				return;
			}
		}

		public static readonly string Name = "Jiffa";

		protected void OnTestAction(IActionContext ctx)
		{
			JiraServer server;
			server = new JiraServer(Core.ResourceStore.FindUniqueResource(Types.JiraServer, Core.Props.Name, "TestJira"));

			server.Uri = "http://unit-138:8080/rpc/soap/jirasoapservice-v2";

			/*
				ResourceProxy proxy = ResourceProxy.BeginNewResource(Types.JiraServer);
				proxy.AddLink(Core.Props.Parent, Core.ResourceTreeManager.GetRootForType(Types.JiraServer)); 
				proxy.EndUpdate();

				server = new JiraServer(proxy.Resource);
				server.Uri = "http://unit-138:8080/rpc/soap/jirasoapservice-v2";
				server.Username = "admin";
				server.Password = "password";
				server.Name = "TestJira";
			 */

			//return;
			/*
			IResourceList servers = Core.ResourceStore.GetAllResources(Types.JiraServer);
			if(servers.Count == 0)
			{
				if(MessageBox.Show(Core.MainWindow, "No JIRA servers found. Create one?", "Jiffa", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
					return;

				ResourceProxy proxy = ResourceProxy.BeginNewResource(Types.JiraServer);
				proxy.EndUpdate();

				server = new JiraServer(proxy.Resource);
				server.Uri = "http://www.jetbrains.net/jira/rpc/soap/jirasoapservice-v2";
				server.Username = "baltic";
				server.Name = "JetBrains JIRA";
			}
			else if(servers.Count != 1)
			{
				MessageBox.Show(Core.MainWindow, "Expecting exactly one JIRA server.", "Jiffa");
				return;
			}
			else
				server = new JiraServer(servers[0]);*/

			//new ResourceProxy(server.Resource).AddLink(Core.Props.Parent, Core.ResourceTreeManager.GetRootForType(Types.JiraServer));
			//new ResourceProxy(server.Resource).SetPropAsync(Core.Props.Name, "JetBrains JIRA");

			if(MessageBox.Show(Core.MainWindow, string.Format("Syncing the JIRA server at {0}.", server.Uri), Name, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK)
				return;

			server.Sync();
		}

		protected void OnServerProperties(IActionContext ctx)
		{
			foreach(IResource resource in ctx.SelectedResources)
			{
				if(resource.Type != Types.JiraServer)
					continue;
				new JiraServer(resource).ShowPropertySheet();
			}
		}

		protected void OnAddServer(IActionContext ctx)
		{
			JiraServer server = JiraServer.CreateNew();
			server.ShowPropertySheet();
		}

		protected void OnDeleteServer(IActionContext ctx)
		{
			foreach(IResource resource in ctx.SelectedResources)
			{
				if(resource.Type != Types.JiraServer)
					continue;
				JiraServer server = new JiraServer(resource);
				while(MessageBox.Show(Core.MainWindow, string.Format(Stringtable.ConfirmDeleteServer, server.Name), Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.No)
					Thread.Sleep(500);
			}
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

		public static string GetRandomName()
		{
			Random rand = new Random();
			string wovels = "aeiouy";
			string consonants = "bcdfghjklmnpqrstvwxz";

			StringBuilder sb = new StringBuilder();
			for(int a = 0; a < 7; a++)
			{
				char c;
				string sFrom = a % 2 != 0 ? wovels : consonants;
				c = sFrom[rand.Next(sFrom.Length)];
				if(sb.Length == 0)
					c = char.ToUpper(c);
				sb.Append(c);
			}
			return sb.ToString();
		}
	}

	internal class ResourceTreePaneNodeFilter : IResourceNodeFilter
	{
		public bool AcceptNode(IResource res, int level)
		{
			return (res.Type == JiraServer.Type) || (res.Type == JiraProject.Type) || (res.Type == JiraComponent.Type);
		}
	}

	/// <summary>
	/// An interface that represents the Jiffa services.
	/// </summary>
	public interface IJiffaService
	{
	}

	public class SubmitNntpMessageToJiraAction : SimpleAction
	{
		public override void Execute(IActionContext context)
		{
			try
			{
				if(context.SelectedResources.Count == 0)
				{
					MessageBox.Show(Core.MainWindow, Stringtable.ActionNoSelection);
					return;
				}

				//IResource resProject = Core.ResourceStore.FindUniqueResource(Types.JiraProject, Props.Key, "RSRP");
				/*
			IResource resProject = Core.ResourceStore.FindUniqueResource(Types.JiraProject, Props.Key, "RSP");
			IResource resServer = resProject.GetLinksFrom(Types.JiraServer, Core.Props.Parent)[0];
			JiraProject project = new JiraProject(new JiraServer(resServer), resProject);
			 * */

				foreach(IResource res in context.SelectedResources)
					new Impl(res).Execute();
			}
			catch(Exception ex)
			{
				Core.ReportException(ex, ExceptionReportFlags.AttachLog);
			}
		}

		public override void Update(IActionContext context, ref ActionPresentation presentation)
		{
			presentation.Visible = presentation.Enabled = true;
		}

		public class Impl
		{
			protected readonly IResource _resource;

			protected Submission _submission = null;

			public Impl(IResource resource)
			{
				_resource = resource;
				if(_resource.Type != "Article")
					throw new InvalidOperationException();
			}

			public void Execute()
			{
				// Chose a project
				int nProject = Core.SettingStore.ReadInt("Jiffa.Submission", "SubmitToProject", -1);
				IResource resProject = Core.ResourceStore.TryLoadResource(nProject);
				if(resProject == null)
				{
					MessageBox.Show(Core.MainWindow, Stringtable.SubmitProjectNotSpecified, Jiffa.Name, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}
				JiraProject project = JiraProject.FromResource(resProject);

				// Invoke submission
				_submission = new Submission(project);
				_submission.IssueSubmitted += new EventHandler(OnIssueSubmitted);
				_submission.ReadArticle(_resource);
				_submission.ShowUI(null);
			}

			private void OnIssueSubmitted(object sender, EventArgs e)
			{
				Core.UserInterfaceAP.QueueJob((MethodInvoker)NewsReplyWithIssue);
			}

			protected void NewsReplyWithIssue()
			{
				new ResourceProxy(_resource, JobPriority.Lowest).DeletePropAsync(Core.Props.IsUnread);

				string sSubj = _resource.GetPropText(Core.Props.Subject);
				if(sSubj.StartsWith("Re: ", true, CultureInfo.CurrentUICulture))
					sSubj = sSubj.Substring(3);
				sSubj = "Re: " + sSubj;

				// Construct the issue url: take the project URI and replace the project key with issue key in it
				string sIssueUri = _submission.Project.Uri;
				sIssueUri = sIssueUri.Substring(0, sIssueUri.Length - _submission.Project.Key.Length);
				sIssueUri += _submission.Issue.key;

				string sTemplate = JiffaSettings.Template;
				sTemplate = sTemplate.Replace("<%=IssueUri%>", sIssueUri);

				IResourceList resGroups = _resource.GetLinksFromLive("NewsGroup", "Newsgroups");

				EditMessageForm.EditAndPostMessage(resGroups, sSubj, sTemplate, _resource.GetPropText("ArticleId"), false);
			}
		}
	}

	public class JiffaIconProvider : IResourceIconProvider
	{
		private Icon _iconJiraServer = null;

		private Icon _iconJiraProject = null;

		private Icon _iconJiraComponent = null;

		private Icon _iconOther = null;

		public Icon GetResourceIcon(IResource resource)
		{
			return GetDefaultIcon(resource.Type);
		}

		public Icon GetDefaultIcon(string resType)
		{
			if(resType == JiraServer.Type)
				return _iconJiraServer ?? (_iconJiraServer = LoadIcon("JiraServer.ico"));
			else if(resType == JiraProject.Type)
				return _iconJiraProject ?? (_iconJiraProject = LoadIcon("JiraProject.ico"));
			else if(resType == JiraComponent.Type)
				return _iconJiraComponent ?? (_iconJiraComponent = LoadIcon("JiraComponent.ico"));
			else
				return _iconOther ?? (_iconOther = LoadIcon("JiraComponent.ico"));
		}

		/// <summary>
		/// Loads an icon by its short name.
		/// </summary>
		public static Icon LoadIcon(string sResLocalName)
		{
			Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JetBrains.Omea.Jiffa.Res." + sResLocalName);
			if(stream == null)
				return null;
			return new Icon(stream);
		}
	}

	public interface ISyncableTo<TJira>
	{
		void Sync(TJira itemJira);
	}
}