/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Tasks
{
    internal enum TaskStatuses
    {
        Inactive, InProgress, Completed, Waiting, Deferred
    }

    internal enum TaskPriorities
    {
        Normal, High, Low
    }

    /** 
     * for selected resources, creates new task
     */
    public class NewTaskAction : IAction
    {
        public void Execute( IActionContext context )
        {
            bool      isToolbar = (context.Kind == ActionContextKind.Toolbar);
            IResource task = (IResource) Core.ResourceAP.RunUniqueJob( new CreateTaskDelegate( CreateTask ),
                                         isToolbar ? null : context.SelectedResources, context.SelectedPlainText );
            if( task != null )
            {
                Core.UIManager.OpenResourceEditWindow( new TaskEditPane(), task, true );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if( context.Kind == ActionContextKind.Toolbar || context.Kind == ActionContextKind.MainMenu ||
                context.Kind == ActionContextKind.Keyboard )
            {
                return;
            }

            bool visible = true;
            IResourceList selected = context.SelectedResources;
            if( selected.Count == 0 )
            {
                visible = context.Instance is TasksViewPane;
            }
            else
            {
                for( int i = 0; i < selected.Count; i++ )
                {
                    IResource res = selected[ i ];
                    if( res.Type == "Task" ||
                        Core.ResourceStore.ResourceTypes[ res.Type ].HasFlag( ResourceTypeFlags.Internal ) )
                    {
                        visible = false;
                        break;
                    }
                }
            }

            // links can only be created on non-internal resource types & non-tasks
            presentation.Visible = visible;
        }

        internal delegate IResource CreateTaskDelegate( IResourceList selectedResources, string selection );

        internal static IResource CreateTask( IResourceList selectedResources, string selection )
        {
            IResource task = Core.ResourceStore.NewResourceTransient( "Task" );
            string name = string.Empty;
            if( selectedResources != null )
            {
                foreach( IResource target in selectedResources )
                {
                    if( target.Type != "Task" && 
                        !Core.ResourceStore.ResourceTypes[ target.Type ].HasFlag( ResourceTypeFlags.Internal ) )
                    {
                        target.AddLink( TasksPlugin._linkTarget, task );
                        if( name.Length < target.DisplayName.Length )
                        {
                            name = target.DisplayName;
                        }
                    }
                }
            }
            if( name.Length == 0 )
            {
                name = "Untitled";
            }
            task.SetProp( Core.Props.Subject, name );
            task.SetProp( TasksPlugin._propStatus, (int) TaskStatuses.Inactive );
            task.AddLink( TasksPlugin._linkSuperTask, TasksPlugin.RootTask );
            IResource category = Core.ResourceBrowser.OwnerResource;
            if( category != null && category.Type == "Category" )
            {
				Core.CategoryManager.AddResourceCategory( task, category );
            }
            if( !String.IsNullOrEmpty( selection ))
                task.SetProp( TasksPlugin._propDescription, selection );

            return task;
        }
    }

    public class CloneTaskAction : IAction
    {
        internal delegate void ResourcePairDelegate( IResource selected, IResource target );

        public void Execute( IActionContext context )
        {
            IResource selected = context.SelectedResources[ 0 ];
            IResource task = NewTaskAction.CreateTask(null, null);
            if( task != null )
            {
                Core.ResourceAP.RunUniqueJob( new ResourcePairDelegate( CloneProps ), selected, task );
                Core.UIManager.OpenResourceEditWindow( new TaskEditPane(), task, false );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResourceList selected = context.SelectedResources;
            presentation.Visible = (selected.Count == 1) && (selected[ 0 ].Type == "Task" );
        }

        private void  CloneProps( IResource selected, IResource task )
        {
            SetProp( task, selected, Core.Props.Subject );
            SetProp( task, selected, TasksPlugin._propDescription );
            SetProp( task, selected, TasksPlugin._propPriority );
            SetProp( task, selected, Core.Props.Date );
            SetProp( task, selected, TasksPlugin._propRemindDate );
            SetProp( task, selected, TasksPlugin._propStatus );
            SetProp( task, selected, TasksPlugin._propStartDate );
            SetLinks( task, selected, TasksPlugin._propRemindWorkspace );
            SetLinks( task, selected, Core.ResourceStore.PropTypes[ "Category" ].Id );
            SetLinks( task, selected, TasksPlugin._linkSuperTask );

            IResourceList linked = selected.GetLinksTo( null, TasksPlugin._linkTarget );
            foreach( IResource res in linked )
                res.AddLink( TasksPlugin._linkTarget, task );

            task.EndUpdate();
        }

        private static void SetProp( IResource target, IResource task, int prop )
        {
            if( task.HasProp( prop ) )
                target.SetProp( prop, task.GetProp( prop ));
        }

        private static void SetLinks( IResource target, IResource task, int link )
        {
            IResourceList linked = task.GetLinksOfType( null, link );
            foreach( IResource res in linked )
                target.AddLink( link, res );
        }
    }

    /** 
     * mark selected tasks as completed
     */
    public class MarkTasksCompletedAction : IAction
    {
        public void Execute( IActionContext context )
        {
            IResourceList resources = context.SelectedResources.Minus(
                Core.ResourceStore.FindResources( null, TasksPlugin._propStatus, (int)TaskStatuses.Completed ) );
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( MarkTasksCompleted ), resources );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResourceList resources = context.SelectedResources.Minus(
                Core.ResourceStore.FindResources( null, TasksPlugin._propStatus, (int)TaskStatuses.Completed ) );
            presentation.Visible = resources.Count > 0 && resources.AllResourcesOfType( "Task" );
        }

        private static void MarkTasksCompleted( IResourceList selectedResources )
        {
            foreach( IResource task in selectedResources.ValidResources )
            {
                task.BeginUpdate();
                try
                {
                    task.SetProp( TasksPlugin._propStatus, (int) TaskStatuses.Completed );
                    task.SetProp( TasksPlugin._propCompletedDate, DateTime.Now );
                }
                finally
                {
                    task.EndUpdate();
                }
            }
        }
    }

    /**
     * deattach resources from a task in tasks pane
     */
    public class DeAttachResourcesFromTaskInPane : IAction
    {
        public void Execute( IActionContext context )
        {
            string        question = "Are you sure you want to remove ";
            int           tasksCount = 0;
            IResourceList nodes = context.SelectedResources;
            foreach( IResource node in nodes )
            {
                IResource parent = node.GetLinkProp( TasksPlugin._linkTarget );
                if( parent != null )
                {
                    tasksCount++;
                    if( tasksCount > 1 )
                        break;
                }
            }
            if( tasksCount == 1 )
            {
                if( nodes.Count == 1 )
                {
                    question += "'" + nodes[ 0 ].DisplayName + "'";
                }
                else
                {
                    question += "selected resources";
                }
                question += " from the task?";
            }
            else
            {
                question += "selected resources from their tasks?";
            }

            if( MessageBox.Show( Core.MainWindow, question,
                "Attention", MessageBoxButtons.YesNo, MessageBoxIcon.Question ) == DialogResult.Yes )
            {
                foreach( IResource node in nodes )
                {
                    IResource parent = node.GetLinkProp( TasksPlugin._linkTarget );
                    if( parent != null )
                    {
                        Core.ResourceAP.QueueJob( JobPriority.Immediate,
                            new DeattachDelegate( DeAttachResource ), node, parent );
                    }
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResourceList list = context.SelectedResources;
            presentation.Visible = (context.Instance is TasksViewPane) && (list.Count > 0);
            if( presentation.Visible )
            {
                string[] types = context.SelectedResources.GetAllTypes();
                foreach( string type in types )
                {
                    if( type == "Task" )
                    {
                        presentation.Visible = false;
                        return;
                    }
                }
            }
        }

        private delegate void DeattachDelegate( IResource target, IResource task );

        private static void DeAttachResource( IResource target, IResource task  )
        {
            target.DeleteLink( TasksPlugin._linkTarget, task );
        }
    }

    /**
     * open task action
     */
    public class OpenTaskAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            OpenTask( context.SelectedResources[ 0 ] );
        }

        internal static void OpenTask( IResource task )
        {
            Core.UIManager.OpenResourceEditWindow( new TaskEditPane(), task, false );
        }
    }

    /**
     * Shows or hides the To Do pane.
     */

    public class ShowHideTodoAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ISidebar rightSidebar = Core.RightSidebar;
            bool expand = !rightSidebar.IsPaneExpanded( "ToDo" );
            if ( expand )
            {
                Core.UIManager.RightSidebarExpanded = true;
            }
            rightSidebar.SetPaneExpanded( "ToDo", expand );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = Core.RightSidebar.IsPaneExpanded( "ToDo" );
        }
    }

    public class ViewCompletedTasksActions : IAction
    {
        public void Execute(IActionContext context)
        {
            bool state = Core.SettingStore.ReadBool( "Tasks", "ShowCompletedTasks", true );
            state = !state;
            Core.SettingStore.WriteBool( "Tasks", "ShowCompletedTasks", state );

            ISidebar rightSidebar = Core.RightSidebar;
            TasksViewPane pane = (TasksViewPane) rightSidebar.GetPane( "ToDo" );
            pane.ShowCompletedTaks = state;
        }

        public void Update(IActionContext context, ref ActionPresentation presentation)
        {
            presentation.Enabled = Core.RightSidebar.IsPaneExpanded( "ToDo" );
            presentation.Checked = Core.SettingStore.ReadBool( "Tasks", "ShowCompletedTasks", true );
        }
    }
}
