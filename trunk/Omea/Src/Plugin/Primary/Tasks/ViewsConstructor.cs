/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Tasks
{
    public class TasksViewsConstructor : IViewsConstructor
    {
        public const string  TaskNotStartedName = "Task is not started yet";
        public const string  TaskNotStartedDeep = "tasknotstarted";
        public const string  TaskInProgressName = "Task is in progress";
        public const string  TaskInProgressDeep = "taskinprogress";
        public const string  TaskIsCompletedName = "Task is completed";
        public const string  TaskIsCompletedDeep = "taskcompleted";
        public const string  TaskIsNotCompletedName = "Task is not completed";
        public const string  TaskIsNotCompletedDeep = "tasknotcompleted";
        public const string  TaskIsOverdueName = "Overdue task";
        public const string  TaskIsOverdueDeep = "taskoverdue";
        public const string  TaskHasReminderName = "Task has a reminder";
        public const string  TaskHasReminderDeep = "taskhasreminder";
        public const string  TaskOfPriorityName = "Task is of %specified% priority";
        public const string  TaskOfPriorityDeep = "taskhaspriority";
        public const string  TaskReminderDatedName = "Task reminder is dated by %time span%";
        public const string  TaskReminderDatedDeep = "taskreminderspan";

        public const string  CreateTaskName = "Create Task";
        public const string  CreateTaskDeep = "createtask";
        public const string  Attach2TaskName = "Attach to %Task(s)%";
        public const string  Attach2TaskDeep = "attach2task";

        #region IViewsConstructor interface
        void IViewsConstructor.RegisterViewsFirstRun()
        {
            string[] applType = new string[ 1 ] { "Task" };
            IFilterManager fMgr = Core.FilterManager;
            IResourceTreeManager treeMgr = Core.ResourceTreeManager;

            IResource notStarted = fMgr.CreateStandardCondition( TaskNotStartedName, TaskNotStartedDeep, applType, "Status", ConditionOp.In, "0" );
            IResource inProgress = fMgr.CreateStandardCondition( TaskInProgressName, TaskInProgressDeep, applType, "Status", ConditionOp.In, "1" );
            IResource completed  = fMgr.CreateStandardCondition( TaskIsCompletedName, TaskIsCompletedDeep, applType, "Status", ConditionOp.In, "2" );
            IResource notCompleted = fMgr.CreateStandardCondition( TaskIsNotCompletedName, TaskIsNotCompletedDeep, applType, "Status", ConditionOp.Lt, "2" );
            IResource overdue    = fMgr.CreateStandardCondition( TaskIsOverdueName, TaskIsOverdueDeep, applType, "Date", ConditionOp.Lt, "Today" );
            IResource withReminder = fMgr.CreateStandardCondition( TaskHasReminderName, TaskHasReminderDeep, applType, "RemindDate", ConditionOp.HasProp );
            IResource remindDateRes = fMgr.CreateConditionTemplate( TaskReminderDatedName, TaskReminderDatedDeep, applType, ConditionOp.Eq, "RemindDate" );

            IResource dueToday    = FilterConvertors.InstantiateTemplate( fMgr.Std.ReceivedInTheTimeSpanX, "Today", applType );
            IResource dueTomorrow = FilterConvertors.InstantiateTemplate( fMgr.Std.ReceivedInTheTimeSpanX, "Tomorrow", applType );
            IResource dueThisWeek = FilterConvertors.InstantiateTemplate( fMgr.Std.ReceivedInTheTimeSpanX, "This week", applType );

            fMgr.AssociateConditionWithGroup( notStarted, "Task Conditions" );
            fMgr.AssociateConditionWithGroup( inProgress, "Task Conditions" );
            fMgr.AssociateConditionWithGroup( completed, "Task Conditions" );
            fMgr.AssociateConditionWithGroup( notCompleted, "Task Conditions" );
            fMgr.AssociateConditionWithGroup( overdue, "Task Conditions" );
            fMgr.AssociateConditionWithGroup( withReminder, "Task Conditions" );
            fMgr.AssociateConditionWithGroup( remindDateRes, "Task Conditions" );

            IResource viewAll = fMgr.RegisterView( "All Tasks", applType, (IResource[])null, null );
            IResource viewNotStarted = fMgr.RegisterView( "Not Started", applType, new IResource[ 1 ]{ notStarted }, null );
            IResource viewInProgress = fMgr.RegisterView( "In Progress", applType, new IResource[ 1 ]{ inProgress }, null );
            IResource viewCompleted = fMgr.RegisterView( "Completed", applType, new IResource[ 1 ]{ completed }, null );
            IResource viewOverdue = fMgr.RegisterView( "Overdue", applType, new IResource[ 1 ]{ overdue }, new IResource[ 1 ]{ completed } );
            IResource viewDueToday = fMgr.RegisterView( "Due Today", applType, new IResource[ 2 ]{ dueToday, notCompleted }, null );
            IResource viewDueTomorrow = fMgr.RegisterView( "Due Tomorrow", applType, new IResource[ 2 ]{ dueTomorrow, notCompleted }, null );
            IResource viewDueThisWeek = fMgr.RegisterView( "Due This Week", applType, new IResource[ 2 ]{ dueThisWeek, notCompleted }, null );

            treeMgr.LinkToResourceRoot( viewAll, 1 );
            treeMgr.LinkToResourceRoot( viewNotStarted, 2 );
            treeMgr.LinkToResourceRoot( viewInProgress, 3 );
            treeMgr.LinkToResourceRoot( viewCompleted, 4 );
            treeMgr.LinkToResourceRoot( viewOverdue, 5 );
            treeMgr.LinkToResourceRoot( viewDueToday, 6 );
            treeMgr.LinkToResourceRoot( viewDueTomorrow, 7 );
            treeMgr.LinkToResourceRoot( viewDueThisWeek, 8 );

            viewAll.SetProp( "DisableDefaultGroupping", true );
            viewNotStarted.SetProp( "DisableDefaultGroupping", true );
            viewInProgress.SetProp( "DisableDefaultGroupping", true );
            viewCompleted.SetProp( "DisableDefaultGroupping", true );
            viewOverdue.SetProp( "DisableDefaultGroupping", true );
            viewDueToday.SetProp( "DisableDefaultGroupping", true );
            viewDueTomorrow.SetProp( "DisableDefaultGroupping", true );
            viewDueThisWeek.SetProp( "DisableDefaultGroupping", true );
        }

        void IViewsConstructor.RegisterViewsEachRun()
        {
            string[]        applType = new string[ 1 ] { "Task" };
            IResource       res;
            IFilterManager  fMgr = Core.FilterManager;

            //  Conditions/Templates
            res = fMgr.CreateConditionTemplateWithUIHandler( TaskOfPriorityName, TaskOfPriorityDeep, applType,
                                                             new TaskPriorityUIHandler(), ConditionOp.Eq, "Priority" );
            fMgr.AssociateConditionWithGroup( res, "Task Conditions" );

            //  Rule Actions/Templates
            fMgr.RegisterRuleAction( CreateTaskName, CreateTaskDeep, new CreateTaskRuleAction() );
            fMgr.RegisterRuleActionTemplate( Attach2TaskName, Attach2TaskDeep, new AttachToTasksRuleAction(), ConditionOp.In, "Task" );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, "All Tasks" );
            Core.UIManager.RegisterResourceDefaultLocation( "Task", res );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, "Due Today" );
            Core.TabManager.SetDefaultSelectedResource( "Tasks", res );
        }
        #endregion IViewsConstructor interface
    }

    //  NB: TasksUpgrade2ViewsConstructor was already used, use another name.
    public class TasksUpgrade1ViewsConstructor : IViewsConstructor
    {
        #region IViewsConstructor interface
        void IViewsConstructor.RegisterViewsFirstRun()
        {
            IResource res;

            //-----------------------------------------------------------------
            //  All conditions, templates and actions must have their deep names
            //-----------------------------------------------------------------
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", TasksViewsConstructor.TaskNotStartedName );
            if( res != null )
                res.SetProp( "DeepName", TasksViewsConstructor.TaskNotStartedDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", TasksViewsConstructor.TaskInProgressName );
            if( res != null )
                res.SetProp( "DeepName", TasksViewsConstructor.TaskInProgressDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", TasksViewsConstructor.TaskIsCompletedName );
            if( res != null )
                res.SetProp( "DeepName", TasksViewsConstructor.TaskIsCompletedDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", TasksViewsConstructor.TaskIsNotCompletedName );
            if( res != null )
                res.SetProp( "DeepName", TasksViewsConstructor.TaskIsNotCompletedDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", TasksViewsConstructor.TaskIsOverdueName );
            if( res != null )
                res.SetProp( "DeepName", TasksViewsConstructor.TaskIsOverdueDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", TasksViewsConstructor.TaskHasReminderName );
            if( res != null )
                res.SetProp( "DeepName", TasksViewsConstructor.TaskHasReminderDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", TasksViewsConstructor.TaskOfPriorityName );
            if( res != null )
                res.SetProp( "DeepName", TasksViewsConstructor.TaskOfPriorityDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName, "Name", TasksViewsConstructor.CreateTaskName );
            if( res != null )
                res.SetProp( "DeepName", TasksViewsConstructor.CreateTaskDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionTemplateResName, "Name", TasksViewsConstructor.Attach2TaskName );
            if( res != null )
                res.SetProp( "DeepName", TasksViewsConstructor.Attach2TaskDeep );
        }

        void IViewsConstructor.RegisterViewsEachRun()
        {
            IResource res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", TasksViewsConstructor.TaskReminderDatedName );
            if( res != null )
                res.SetProp( "DeepName", TasksViewsConstructor.TaskReminderDatedDeep );

            //  Some conditions were created inproperly
            IResourceList list = Core.ResourceStore.FindResources( FilterManagerProps.ConditionTemplateResName, "DeepName", TasksViewsConstructor.TaskOfPriorityName );
            list.DeleteAll();
        }
        #endregion IViewsConstructor interface
    }

    public class TasksUpgrade2ViewsConstructor : IViewsConstructor
    {
        #region IViewsConstructor Members

        public void RegisterViewsFirstRun()
        {
            IResource view;
            view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", "All Tasks" );
            if( view != null )
                view.SetProp( "DisableDefaultGroupping", true );
            view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", "Not Started" );
            if( view != null )
                view.SetProp( "DisableDefaultGroupping", true );
            view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", "In Progress" );
            if( view != null )
                view.SetProp( "DisableDefaultGroupping", true );
            view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", "Completed" );
            if( view != null )
                view.SetProp( "DisableDefaultGroupping", true );
            view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", "Overdue" );
            if( view != null )
                view.SetProp( "DisableDefaultGroupping", true );
            view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", "Due Today" );
            if( view != null )
                view.SetProp( "DisableDefaultGroupping", true );
            view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", "Due Tomorrow" );
            if( view != null )
                view.SetProp( "DisableDefaultGroupping", true );
            view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", "Due This Week" );
            if( view != null )
                view.SetProp( "DisableDefaultGroupping", true );
        }

        public void RegisterViewsEachRun()
        {
            // TODO:  Add TasksUpgrade2ViewsConstructor.RegisterViewsEachRun implementation
        }

        #endregion
    }

    public class TasksUpgrade3ViewsConstructor : IViewsConstructor
    {
        #region IViewsConstructor Members
        public void RegisterViewsFirstRun()
        {
            //  Link all tasks created so far to the root with the
            //  new predefined link.
            IResourceList list = Core.ResourceStore.GetAllResources( "Task" );
            foreach( IResource res in list )
            {
                if( res.GetLinksOfType( null, TasksPlugin._linkSuperTask ).Count == 0 )
                {
                    IResource rootTask = res.GetLinkProp( TasksPlugin._linkTarget );
                    res.SetProp( TasksPlugin._linkSuperTask, rootTask );
                }
            }

            //  Tasks for "Next Week" view.
            IFilterManager fMgr = Core.FilterManager;
            IStandardConditions std = fMgr.Std;
            string[] applType = new string[ 1 ] { "Task" };

            IResource dateRes = fMgr.CreateConditionTemplate( std.ReceivedInTheTimeSpanXName, std.ReceivedInTheTimeSpanXNameDeep, null, ConditionOp.In, "Date" );
            IResource nextWeekCond = FilterConvertors.InstantiateTemplate( dateRes, "Next Week", null );
            IResource completed  = fMgr.CreateStandardCondition( TasksViewsConstructor.TaskIsCompletedName, TasksViewsConstructor.TaskIsCompletedDeep, applType, "Status", ConditionOp.Eq, "2" );
            IResource tasksNextWeek = fMgr.RegisterView( "Due Next Week", applType, new IResource[ 1 ]{ nextWeekCond }, new IResource[ 1 ]{ completed } );
            Core.ResourceTreeManager.LinkToResourceRoot( tasksNextWeek, 9 );
        }

        public void RegisterViewsEachRun()
        {
            // TODO:  Add TasksUpgrade2ViewsConstructor.RegisterViewsEachRun implementation
        }
        #endregion
    }

    /// <summary>
    /// This class analyzes bad dates set during older versions of Omea for
    /// super tasks. Two cases are possible - date is DateTime.MinValue or
    /// shifted by UTC modifier from DateTime.MinValue. Either cases are covered
    /// by testing the potential conversion.
    /// </summary>
    public class TasksUpgrade4ViewsConstructor : IViewsConstructor
    {
        public void RegisterViewsFirstRun()
        {
            IResourceList list = Core.ResourceStore.GetAllResources( "Task" );
            foreach( IResource res in list )
            {
                AnalyzeDate( res, TasksPlugin._propStartDate );
                AnalyzeDate( res, Core.Props.Date );
            }
        }

        public void RegisterViewsEachRun()
        {}

        private void AnalyzeDate( IResource res, int prop )
        {
            if( res.HasProp( prop ))
            {
                DateTime time = res.GetDateProp( prop );
                try
                {
                    time.ToFileTime();
                }
                catch( ArgumentOutOfRangeException )
                {
                    res.DeleteProp( prop );
                }
            }
        }
    }
}