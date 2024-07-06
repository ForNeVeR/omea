// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using Tasks;

namespace JetBrains.Omea.Tasks
{
    [PluginDescription("Tasks", "JetBrains Inc.", "Tasks viewer and editor. Control task completion, alerts and statuses.", PluginDescriptionFormat.PlainText, "Icons/TasksPluginIcon.png")]
	public class TasksPlugin : IPlugin, IResourceDisplayer, IResourceTextProvider, IResourceUIHandler, IResourceDragDropHandler
	{
		#region IPlugin Members : Registration and Startup
		public void Register()
		{
			RegisterTypes();

			Core.ResourceTreeManager.SetViewsExclusive( "Task" );

            Core.TabManager.RegisterResourceTypeTab( "Tasks", "Tasks", new[]{ "Task" }, 99 );
            Core.RightSidebar.RegisterPane( new TasksViewPane(), "ToDo", "To Do",
                                            Utils.TryGetEmbeddedResourceImageFromAssembly( Assembly.GetExecutingAssembly(), "Tasks.Icons.TODO24.png" ) );

			Core.PluginLoader.RegisterResourceTextProvider( "Task", this );
			Core.PluginLoader.RegisterResourceDisplayer( "Task", this );
			Core.PluginLoader.RegisterResourceSerializer( "Task", new TaskSerializer() );
			Core.PluginLoader.RegisterResourceUIHandler( "Task", this );
			Core.PluginLoader.RegisterResourceDragDropHandler( Core.ResourceTreeManager.ResourceTreeRoot.Type, this );	// Root res
			Core.PluginLoader.RegisterResourceDragDropHandler( "Task", new DragDropLinkAdapter( this ));	// Tasks
			Core.PluginLoader.RegisterDefaultThreadingHandler( "Task", _linkSuperTask );

			Core.PluginLoader.RegisterViewsConstructor( new TasksUpgrade1ViewsConstructor() );
			Core.PluginLoader.RegisterViewsConstructor( new TasksViewsConstructor() );
			Core.PluginLoader.RegisterViewsConstructor( new TasksUpgrade2ViewsConstructor() );
			Core.PluginLoader.RegisterViewsConstructor( new TasksUpgrade3ViewsConstructor() );
            Core.PluginLoader.RegisterViewsConstructor( new TasksUpgrade4ViewsConstructor() );

            //-----------------------------------------------------------------
            //  Register Search Extensions to narrow the list of results using
            //  simple phrases in search queries: for restricting the resource
            //  type to Tasks (two synonyms).
            //-----------------------------------------------------------------
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "tasks", "Task" );
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "task", "Task" );

			Core.WorkspaceManager.WorkspaceChanged += WorkspaceManager_WorkspaceChanged;
			Core.WorkspaceManager.RegisterWorkspaceType( "Task", new int[] { }, WorkspaceResourceType.Container );

			_ImageList.Images.Add( LoadIconFromAssembly( "Attached.ico" ));
			Core.ResourceIconManager.RegisterResourceLargeIcon( "Task", LoadIconFromAssembly( "TaskLarge.ico" ));
            Core.ResourceIconManager.RegisterOverlayIconProvider( "Task", new TaskOverlayIconProvider() );

			IDisplayColumnManager colManager = Core.DisplayColumnManager;
			colManager.RegisterPropertyToTextCallback( _propStatus, TaskStatus2String);
			colManager.RegisterPropertyToTextCallback( _propCompletedDate, Date2String);

			ImageListColumn priorityCol = new PriorityColumn();
			ImageListColumn statusCol = new StatusColumn();
			colManager.RegisterCustomColumn( _propPriority, priorityCol );
			colManager.RegisterCustomColumn( _propStatus, statusCol );

			Core.ResourceBrowser.RegisterLinksPaneFilter( "Task", new TasksLinksPaneFilter() );

			Core.PluginLoader.RegisterResourceDeleter( "Task", new DefaultResourceDeleter() );
		}

		private void RegisterTypes()
		{
			IResourceStore store = Core.ResourceStore;
			_propDescription = store.PropTypes.Register("Description", PropDataType.String);
			store.ResourceTypes.Register("Task", "Task", "Subject", ResourceTypeFlags.ResourceContainer, this);
			_propStatus = store.PropTypes.Register("Status", PropDataType.Int);
			_propPriority = store.PropTypes.Register("Priority", PropDataType.Int);
			_propRemindDate = ResourceTypeHelper.UpdatePropTypeRegistration("RemindDate", PropDataType.Date, PropTypeFlags.AskSerialize);
			_propStartDate = ResourceTypeHelper.UpdatePropTypeRegistration("StartDate", PropDataType.Date, PropTypeFlags.Normal);
			store.PropTypes.RegisterDisplayName(_propStartDate, "Start Date");
			_propCompletedDate = store.PropTypes.Register("CompletedDate", PropDataType.Date);
			store.PropTypes[_propCompletedDate].Flags = store.PropTypes[_propCompletedDate].Flags & ~PropTypeFlags.Internal;
			_propIsRoot = store.PropTypes.Register("IsRoot", PropDataType.Int, PropTypeFlags.Internal);
			_linkTarget = ResourceTypeHelper.UpdatePropTypeRegistration("Target", PropDataType.Link, PropTypeFlags.DirectedLink);
			_linkSuperTask = ResourceTypeHelper.UpdatePropTypeRegistration("SuperTask", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal);
			_propRemindWorkspace = ResourceTypeHelper.UpdatePropTypeRegistration("RemindWorkspace", PropDataType.Link, PropTypeFlags.Internal);

			/**
			 * delete obsolete properties
			 */
			if(store.PropTypes.Exist("ReminderActive"))
			{
				IResourceList tasks = store.GetAllResources("Task");
				foreach(IResource task in tasks)
				{
					if(task.HasProp("ReminderActive") && task.GetIntProp("ReminderActive") == 0)
					{
						task.DeleteProp(_propRemindDate);
					}
				}
				store.PropTypes.Delete(store.PropTypes["ReminderActive"].Id);
			}
			if(store.PropTypes.Exist("WorkspaceActivationReminder"))
			{
				store.PropTypes.Delete(store.PropTypes["WorkspaceActivationReminder"].Id);
			}

			store.PropTypes.RegisterDisplayName(_propCompletedDate, "Completed");
			store.PropTypes.RegisterDisplayName(_linkTarget, "Task", "Resources");
		}

		public void Startup()
		{
			CreateRootTask();
			IResourceStore store = Core.ResourceStore;
			_allTasks = store.GetAllResourcesLive("Task").Minus(
				store.FindResourcesLive("Task", _propStatus, (int)TaskStatuses.Completed));
			if(store.PropTypes.Exist("DueDate"))
			{
				foreach(IResource task in _allTasks)
				{
					DateTime dueDate = task.GetDateProp("DueDate");
					if(dueDate > DateTime.MinValue)
					{
						task.SetProp(Core.Props.Date, dueDate);
					}
				}
			}
			_allTasks.ResourceAdded += _allTasks_ResourceAdded;
			_allTasks.ResourceChanged += _allTasks_ResourceUpdated;
			_allTasks.ResourceDeleting += _allTasks_ResourceDeleting;
			Core.StateChanged += Core_StateChanged;
		}

		bool IResourceTextProvider.ProcessResourceText(IResource task, IResourceTextConsumer consumer)
		{
			string subject = task.GetPropText(Core.Props.Subject);
			if(subject.Length > 0)
			{
				consumer.AddDocumentHeading(task.Id, subject);
			}
			string description = task.GetPropText(_propDescription);
			if(description.Length > 0)
			{
				consumer.AddDocumentFragment(task.Id, description);
			}
			return true;
		}

		public void Shutdown()
		{
		}
		#endregion IPlugin Members : Registration and Startup

		#region IResourceDisplayer Members

		public IDisplayPane CreateDisplayPane(string resourceType)
		{
			return new TaskDisplayPane();
		}

		#endregion

		#region IResourceUIHandler Members

		public bool CanDropResources(IResource targetResource, IResourceList dragResources)
		{
			throw new NotImplementedException("Use ResourceDragDropHandler.");
		}

		public bool CanRenameResource(IResource res)
		{
			return false;
		}

		public void ResourcesDropped(IResource targetResource, IResourceList droppedResources)
		{
			throw new NotImplementedException("Use ResourceDragDropHandler.");
		}

		public bool ResourceRenamed(IResource res, string newName)
		{
			return false;
		}

		public void ResourceNodeSelected(IResource res)
		{
		}

		#endregion

		#region IResourceDragDropHandler Members

		/// <summary>
		/// Called to supply data in additional formats when the specified resources are being dragged.
		/// </summary>
		/// <param name="dragResources">The dragged resources.</param>
		/// <param name="dataObject">The drag data object.</param>
		public void AddResourceDragData(IResourceList dragResources, IDataObject dataObject)
		{
			if(!dataObject.GetDataPresent(typeof(string)))
			{
				StringBuilder sb = StringBuilderPool.Alloc();
				try
				{
					foreach(IResource resource in dragResources)
					{
						if(sb.Length != 0)
							sb.Append(", ");
						string text = resource.DisplayName;
						if(text.IndexOf(' ') > 0)
						{
							sb.Append("\"");
							sb.Append(text);
							sb.Append("\"");
						}
						else
							sb.Append(text);
					}
					dataObject.SetData(sb.ToString());
				}
				finally
				{
					StringBuilderPool.Dispose(sb);
				}
			}
		}

		/// <summary>
		/// Called to return the drop effect when the specified data object is dragged over the
		/// specified resource.
		/// </summary>
		/// <param name="targetResource">The resource over which the drag happens.</param>
		/// <param name="data">The <see cref="IDataObject"/> containing the dragged data.</param>
		/// <param name="allowedEffect">The drag-and-drop operations which are allowed by the
		/// originator (or source) of the drag event.</param>
		/// <param name="keyState">The current state of the SHIFT, CTRL, and ALT keys,
		/// as well as the state of the mouse buttons.</param>
		/// <returns>The target drop effect.</returns>
		public DragDropEffects DragOver(IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState)
		{
			if(data.GetDataPresent(typeof(IResourceList))) // Dragging resources over
			{
				// The resources we're dragging
				IResourceList dragResources = (IResourceList)data.GetData(typeof(IResourceList));

				// Restrict the allowed target res-types
				if(!((targetResource.Type == "Task") || (targetResource == Core.ResourceTreeManager.GetRootForType("Task"))))
					return DragDropEffects.None;

				// Collect all the direct and indirect parents of the droptarget; then we'll check to avoid dropping parent on its children
				IntHashSet ancestors = new IntHashSet();
				IResource parent = targetResource;
				while(parent != null)
				{
					ancestors.Add(parent.Id);
					parent = parent.GetLinkProp(TasksPlugin._linkSuperTask);
				}

				// Measure some metrics on the dragged resources, don't allow mixing tasks/resources and prohibit the internal resources
				bool bAllTasks = true;
				bool bNoTasks = true;
				bool bNoInternal = true;
				foreach(IResource res in dragResources)
				{
					bAllTasks = bAllTasks && (res.Type == "Task");
					bNoTasks = bNoTasks && (res.Type != "Task");
					bNoInternal = bNoInternal && (!Core.ResourceStore.ResourceTypes[res.Type].HasFlag(ResourceTypeFlags.Internal));
					if(ancestors.Contains(res.Id))
						return DragDropEffects.None;	// Dropping parent on a child
				}
				if(((!bAllTasks) && (!bNoTasks)) || (!bNoInternal))
					return DragDropEffects.None;

				// Link attachments, move the tasks
				return bAllTasks ? DragDropEffects.Move : DragDropEffects.Link;
			}
			return DragDropEffects.None;
		}

		/// <summary>
		/// Called to handle the drop of the specified data object on the specified resource.
		/// </summary>
		/// <param name="targetResource">The drop target resource.</param>
		/// <param name="data">The <see cref="IDataObject"/> containing the dragged data.</param>
		/// <param name="allowedEffect">The drag-and-drop operations which are allowed by the
		/// originator (or source) of the drag event.</param>
		/// <param name="keyState">The current state of the SHIFT, CTRL, and ALT keys,
		/// as well as the state of the mouse buttons.</param>
		public void Drop(IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState)
		{
			if(data.GetDataPresent(typeof(IResourceList)))
			{
				// The resources we're dragging
				IResourceList dragResources = (IResourceList)data.GetData(typeof(IResourceList));

				if(dragResources.Count > 0)
					Core.ResourceAP.QueueJob(JobPriority.Immediate, "Drop Resources on a Task", new ResourcesDroppedDelegate(ResourcesDroppedImpl), targetResource, dragResources);
			}
		}

		#endregion

		#region implementation details
		private class RemindUOW : AbstractJob
		{
			private IResource _task;

			public RemindUOW(IResource task)
			{
				_task = task;
			}

			#region System.Object overrides

			public override int GetHashCode()
			{
				return _task.Id;
			}

			public override bool Equals(object x)
			{
				RemindUOW remindUOW = x as RemindUOW;
				return (remindUOW != null) && _task.Id == remindUOW._task.Id;
			}

			#endregion

			protected override void Execute()
			{
				if(_task.GetDateProp( _propRemindDate ) > DateTime.Now)
				{
					RemindAboutTask( _task );
				}
				else
                if( !_task.IsDeleted &&
				    _task.GetIntProp( _propStatus ) != (int)TaskStatuses.Completed )
				{
					Core.UIManager.QueueUIJob(new ResourceDelegate(ReminderForm.AddTask), _task);
				}
			}
		}

		internal static void CreateRootTask()
		{
			IResource newRoot = RootTask;
			Core.ResourceTreeManager.SetResourceNodeSort(newRoot, "Subject");
			IResourceStore store = Core.ResourceStore;
			IResource oldRootTask;
			if((oldRootTask = store.FindUniqueResource("Task", _propIsRoot, 1)) != null)
			{
				oldRootTask.Delete();
				IResourceList tasks = store.GetAllResources("Task");
				foreach(IResource task in tasks)
				{
					task.SetProp(_linkTarget, newRoot);
				}
			}
		}

		internal static IResource RootTask
		{
			get { return Core.ResourceTreeManager.GetRootForType("Task"); }
		}


		private static void _allTasks_ResourceAdded(object sender, ResourceIndexEventArgs e)
		{
			RemindAboutTask(e.Resource);
			UpdateToDoCount();
		}

		private static void _allTasks_ResourceUpdated(object sender, ResourcePropIndexEventArgs e)
		{
			RemindAboutTask(e.Resource);
			UpdateToDoCount();
		}

		private static void _allTasks_ResourceDeleting(object sender, ResourceIndexEventArgs e)
		{
			IResource task = e.Resource;
			if(task.GetDateProp(_propRemindDate) != DateTime.MinValue)
			{
				Core.ResourceAP.CancelTimedJobs(new RemindUOW(task));
			}
			UpdateToDoCount();
		}

		private static void RemindAboutTask(IResource task)
		{
			if(task.GetDateProp(_propRemindDate) != DateTime.MinValue)
			{
				AbstractJob uow = new RemindUOW(task);
				Core.ResourceAP.CancelTimedJobs(uow);
				Core.ResourceAP.QueueJobAt(task.GetDateProp(_propRemindDate), uow);
			}
		}

		private static void WorkspaceManager_WorkspaceChanged(object sender, EventArgs e)
		{
			IResource workspace = Core.WorkspaceManager.ActiveWorkspace;
			if(workspace != null)
			{
				IResourceList tasks = workspace.GetLinksOfType( "Task", _propRemindWorkspace );
				foreach(IResource task in tasks)
				{
					ReminderForm.AddTask( task );
				}
			}
			UpdateToDoCount();
		}

		private void Core_StateChanged(object sender, EventArgs e)
		{
			if(Core.State == CoreState.Running)
			{
				foreach(IResource task in _allTasks)
				{
					RemindAboutTask(task);
				}
				UpdateToDoCount();
			}
		}

		private static string TaskStatus2String(IResource task, int propId)
		{
			if(task.Type == "Task")
			{
				int status = task.GetIntProp(propId);
				if(status < 0 || status >= _statuses.Length)
				{
					status = 0;
				}
				return _statuses[status];
			}
			return string.Empty;
		}

		private static string Date2String(IResource res, int propID)
		{
			DateTime dt = res.GetDateProp(propID);
			if(dt == DateTime.MinValue)
			{
				return string.Empty;
			}
			if(dt.Date == DateTime.Today)
			{
				return dt.ToString("HH:mm:ss");
			}

			return dt.ToString();
		}

		internal static Icon LoadIconFromAssembly(string name)
		{
			Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Tasks.Icons." + name);
			return new Icon( stream );
		}

		private static void UpdateToDoCount()
		{
			if(!Core.UserInterfaceAP.IsOwnerThread)
			{
				Core.UserInterfaceAP.QueueJob(new MethodInvoker(UpdateToDoCount));
				return;
			}
			string caption = "To Do";
			int count = 0;
			foreach(IResource res in Core.ResourceStore.GetAllResources("Task").ValidResources)
			{
				if(TasksViewPane._todoFilter.AcceptNode(res, 0))
				{
					++count;
				}
			}
			if(count > 0)
			{
				caption = caption + " (" + count + ")";
			}
			Core.RightSidebar.SetPaneCaption("ToDo", caption);
		}

		#region Supertask parameters recalculation
		internal static void RecalculateSupertaskParameters(ArrayList parentTasks)
		{
			ArrayList doneRoots = new ArrayList();
			foreach(IResource task in parentTasks)
			{
				IResource root = RootOfTask(task);
				if(doneRoots.IndexOf(root) == -1)
				{
					RecalculateTreeParameters(root);
					doneRoots.Add(root);
				}
			}
		}

		internal static IResource RootOfTask(IResource task)
		{
			IResource root = task.GetLinkProp(TasksPlugin._linkSuperTask);
			while(root != null && root.Id != TasksPlugin.RootTask.Id)
			{
				task = root;
				root = task.GetLinkProp(TasksPlugin._linkSuperTask);
			}

			if(root == null)
				throw new ApplicationException("TasksPane -- Illegal structure of the SuperTask/SubTask for task: " + task.DisplayName);

			return task;
		}

		internal static void RecalculateTreeParameters(IResource root)
		{
			DateTime start = DateTime.MaxValue, finish = DateTime.MinValue,
					 dueDate = DateTime.MaxValue;
			bool isInProgress = false, isComplete = true;

			IResourceList subtasks = root.GetLinksTo(null, TasksPlugin._linkSuperTask);
			if(subtasks.Count > 0)
			{
				foreach(IResource task in subtasks)
				{
					RecalculateTreeParameters(task);
					DateTime taskDue = task.GetDateProp(Core.Props.Date);
					DateTime taskStart = task.GetDateProp(TasksPlugin._propStartDate);
					DateTime taskFinish = task.GetDateProp(TasksPlugin._propCompletedDate);
					if(dueDate < taskDue)
						dueDate = taskDue;
					if(taskStart < start)
						start = taskStart;
					if(taskFinish > finish)
						finish = taskFinish;

					int status = task.GetIntProp(TasksPlugin._propStatus);
					isComplete = isComplete && (status == 2);
					isInProgress = isInProgress || (status == 1);
				}

				if(isComplete)
					root.SetProp(TasksPlugin._propStatus, 2);
				else
				{
					root.DeleteProp(TasksPlugin._propCompletedDate);
					if(isInProgress)
						root.SetProp(TasksPlugin._propStatus, 1);
					else
						root.SetProp(TasksPlugin._propStatus, 0);
				}

				if(start != DateTime.MaxValue)
					root.SetProp(TasksPlugin._propStartDate, start);
				if(finish != DateTime.MinValue)
					root.SetProp(TasksPlugin._propCompletedDate, finish);
				if(dueDate != DateTime.MaxValue)
					root.SetProp(Core.Props.Date, dueDate);
			}
		}
		#endregion Supertask parameters recalculation

		/// <summary>
		/// Serializer for sending/receiving tasks.
		/// </summary>
		private class TaskSerializer : IResourceSerializer
		{
			public void AfterSerialize(IResource parentResource, IResource res, System.Xml.XmlNode node)
			{
			}

			public IResource AfterDeserialize(IResource parentResource, IResource res, System.Xml.XmlNode node)
			{
				res.AddLink(TasksPlugin._linkSuperTask, TasksPlugin.RootTask);
				return res;
			}
			public SerializationMode GetSerializationMode(IResource res, string propertyType)
			{
				return SerializationMode.Default;
			}
		}

		private class TasksLinksPaneFilter : ILinksPaneFilter
		{
			public bool AcceptLinkType(IResource displayedResource, int propId, ref string displayName)
			{
				return propId != TasksPlugin._linkTarget;
			}

			public bool AcceptLink(IResource displayedResource, int propId, IResource targetResource,
				ref string linkTooltip)
			{
				return true;
			}

			public bool AcceptAction(IResource displayedResource, IAction action)
			{
				return true;
			}
		}

		/// <summary>
		/// A delegate for queueing the <see cref="ResourcesDroppedImpl"/> function.
		/// </summary>
		protected delegate void ResourcesDroppedDelegate(IResource resTarget, IResourceList resDropped);

		/// <summary>
		/// An implementation-helper for the <see cref="Drop"/> method that handles d'n'd in the Resource thread.
		/// </summary>
		protected static void ResourcesDroppedImpl( IResource resTarget, IResourceList resDropped)
		{
			// Separate the special case — new task creation by dropping on the root
			bool bNoTasks = true;
			foreach(string sResType in resDropped.GetAllTypes())
				bNoTasks = bNoTasks && (sResType != "Task");
			if((bNoTasks) && (resTarget == Core.ResourceTreeManager.GetRootForType("Task")))
			{
				// Create a new task from the root-dropped (or empty-space-dropped) attachments
				Core.UserInterfaceAP.QueueJob(JobPriority.Immediate, "New Task from Dropped Attachments", new ExecuteActionDelegate(new NewTaskAction().Execute), new ActionContext(resDropped));
			}
			else if(bNoTasks)
			{	// Dropping attachments on a normal task
				AddDescendants(resTarget, resDropped, _linkTarget);
			}
			else
			{	// Moving the tasks along the tree, change the parent
				AddDescendants(resTarget, resDropped, _linkSuperTask);
				/*
				foreach(IResource res in resDropped)
				{
					res.SetProp(TasksPlugin._linkTarget, resTarget);
					Core.WorkspaceManager.AddToActiveWorkspace(res);
					Core.WorkspaceManager.CleanWorkspaceLinks(res);
				}
				 */
			}
		}

		/// <summary>
		/// A delegate for the <see cref="IAction.Execute"/> method.
		/// </summary>
		protected delegate void ExecuteActionDelegate(IActionContext context);

		/// <summary>
		/// Processes drop of something on the task. May either link-as-attachment or link-as-supertask.
		/// Must be called on the Resource thread.
		/// </summary>
		internal static void AddDescendants(IResource task, IResourceList descendants, int linkId)
		{
			ArrayList parentTasks = new ArrayList();
			task.BeginUpdate();
			try
			{
				foreach( IResource res in descendants )
				{
					//  Attached resources may be linked to many tasks, while
					//  tasks may have only one supertask.
					if( res.Type != "Task" )
						res.AddLink( linkId, task );
					else
					{
						//  Collect current parents of the tasks in order to
						//  recalculate their stati on completeness and dates
						IResource parent = res.GetLinkProp(linkId);
						if(parent != null && parent.Id != TasksPlugin.RootTask.Id )
							parentTasks.Add( parent );

						res.SetProp( linkId, task );
					}
				}
			}
			finally
			{
				task.EndUpdate();
			}

			if( task.Id != TasksPlugin.RootTask.Id )
				parentTasks.Add( task );

			TasksPlugin.RecalculateSupertaskParameters( parentTasks );
		}
		#endregion

		#region Attributes
		private IResourceList _allTasks;
		internal static int _propDescription;
		internal static int _propStatus;
		internal static int _propPriority;
		internal static int _propRemindDate;
		internal static int _propStartDate;
		internal static int _propCompletedDate;
		internal static int _propIsRoot;
		internal static int _linkTarget;
		internal static int _linkSuperTask;
		internal static int _propRemindWorkspace;
		internal static ImageList _ImageList = new ImageList();
		internal static readonly string[] _statuses = new string[] { "Not Started", "In Progress", "Completed", "Waiting", "Deferred" };
		internal static readonly string[] _priorities = new string[] { "Normal", "High", "Low" };

		#endregion Attributes
	}

	#region Comparers

	/// <summary>
	/// Task priorities comparer.
	/// </summary>
	public class TasksComparerByPriority : IResourceComparer
	{
		public int CompareResources(IResource r1, IResource r2)
		{
			int priority1 = r1.GetIntProp("Priority");
			int priority2 = r2.GetIntProp("Priority");
			if(priority1 == priority2)
			{
				return 0;
			}
			if(priority1 != 0 && priority2 != 0)
			{
				return priority1 - priority2;
			}
			if(priority1 == 0)
			{
				return 3 - (priority2 * 2);
			}
			return (priority1 * 2) - 3;
		}
	}
	#endregion Comparers

	#region UI Handlers
	internal class TaskPriorityUIHandler : IStringTemplateParamUIHandler
	{
		string Value = string.Empty, Representation = string.Empty;

		public DialogResult ShowUI(IWin32Window h)
		{
			TaskPriorityForm form = new TaskPriorityForm(Value);
			DialogResult result = form.ShowDialog(h);
			Value = form.Value;
			Representation = form.Representation;
			form.Dispose();
			return result;
		}
		public IResource Template { set { } }
		public string CurrentValue { set { Value = value; } }
		public string Result { get { return Value; } }
		public string DisplayString { get { return Representation; } }
	}
	#endregion UI Handlers

    internal class TaskOverlayIconProvider : IOverlayIconProvider
    {
        private Icon    _attachmentIcon = null;
        private Icon[]  _retVal;

        public Icon[] GetOverlayIcons( IResource res )
        {
            if( _attachmentIcon == null )
                LoadIcon();

            return (res.GetLinksTo( null, TasksPlugin._linkTarget ).Count > 0) ? _retVal : null;
        }

        private void LoadIcon()
        {
            _attachmentIcon = TasksPlugin.LoadIconFromAssembly( "Attached.ico" );
            _retVal = new Icon[ 1 ] { _attachmentIcon };
        }
    }
}
