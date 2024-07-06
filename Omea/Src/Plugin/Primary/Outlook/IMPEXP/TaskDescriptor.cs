// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class TaskDescriptor : AbstractNamedJob
    {
        private static ResourceTracer _tracer = new ResourceTracer( "TaskDescriptor" );
        private string _entryID;
        private string _subject;
        private DateTime _dueDate;
        private DateTime _remindDate;
        private DateTime _startDate;
        private int _priority;
        private int _reminderActive;
        private int _status;
        private string _description;
        private FolderDescriptor _folder;
        private string _OMTaskId;
        private ArrayList _outlookCategories;

        private TaskDescriptor( FolderDescriptor folder, IEMessage message, string entryID )
        {
            _folder = folder;
            _entryID = entryID;
            if ( message != null )
            {
                _subject = Task.GetSubject( message );
                _description = Task.GetDescription( message );
                _priority = Task.GetPriority( message );
                _dueDate = Task.GetDueDate( message );
                _dueDate = _dueDate.ToUniversalTime();
                _startDate = Task.GetStartDate( message );
                _startDate = _startDate.ToUniversalTime();
                _remindDate = Task.GetRemindDate( message );

                _OMTaskId = Task.GetOMTaskID( message );
                _reminderActive = Task.GetReminderActive( message );
                _status = Task.GetStatus( message, false );
                _outlookCategories = OutlookSession.GetCategories( message );
            }
        }

        public static void Do( JobPriority jobPriority, FolderDescriptor folder, IEMessage message, string entryID )
        {
            if ( folder == null || folder.FolderIDs == null )
            {
                Tracer._Trace( "Cannot locate folder" );
                return;
            }
            Core.ResourceAP.QueueJob( jobPriority, new TaskDescriptor( folder, message, entryID ) );
        }
        public static void Do( FolderDescriptor folder, IEMessage message, string entryID )
        {
            Do( JobPriority.Normal, folder, message, entryID );
        }

        protected override void Execute()
        {
            IResource task = GetTaskResource();
            if ( task == null )
            {
                Tracer._Trace( "TASK IMPORT task not found" );
            }
            if ( task != null )
            {
                if ( _folder.ContainerClass != FolderType.Task )
                {
                    Tracer._Trace( "Delete task: id = " + task.Id );
                    task.Delete();
                    return;
                }
            }

            IResource resFolder = Folder.Find( _folder.FolderIDs.EntryId );
            bool folderIgnored = ( resFolder != null && Folder.IsIgnored( resFolder ) ) || OutlookSession.IsDeletedItemsFolder( _folder.FolderIDs.EntryId );
            if ( folderIgnored )
            {
                if ( task != null )
                {
                    task.Delete();
                }
                return;
            }

            bool import = resFolder != null && !Folder.IsIgnoreImport( resFolder );
            if ( !import && task == null ) return;

            if ( task == null )
            {
                task = Core.ResourceStore.BeginNewResource( STR.Task );
            }
            else
            {
                task.BeginUpdate();
            }

            string oldEntryID = task.GetStringProp( PROP.EntryID );
            if ( oldEntryID == null )
            {
                task.SetProp( PROP.EntryID, _entryID );
            }
            else if ( oldEntryID != _entryID )
            {
                throw new ApplicationException( "Try to change entryID for task" );
            }
            if ( resFolder != null )
            {
                Folder.LinkMail( resFolder, task );
            }

            if ( !import )
            {
                task.EndUpdate();
                if ( Settings.TraceTaskChanges )
                {
                    _tracer.Trace( task );
                }
                return;
            }

            task.SetProp( Core.Props.Subject, _subject );

            SetDateProp( task, Core.Props.Date, _dueDate );
            SetDateProp( task, PROP.StartDate, _startDate );

            if( _reminderActive == 0 )
            {
                SetDateProp( task, PROP.RemindDate, DateTime.MinValue );
            }
            else
            {
                SetDateProp( task, PROP.RemindDate, _remindDate );
            }

            task.SetProp( PROP.Status, _status );
            task.SetProp( PROP.Priority, _priority );
            task.SetProp( PROP.Description, _description );
            task.AddLink( PROP.Target, Core.ResourceTreeManager.GetRootForType( STR.Task ) );
            if( !task.HasProp( PROP.SuperTaskLink ))
                task.SetProp( PROP.SuperTaskLink, Core.ResourceTreeManager.GetRootForType( STR.Task ) );

            if ( Settings.SyncTaskCategory )
            {
                CategorySetter.DoJob( _outlookCategories, task );
            }

            bool wereChanges = task.IsChanged();
            task.EndUpdate();
            if ( wereChanges && Core.TextIndexManager != null )
            {
                Guard.QueryIndexingWithCheckId( task );
            }
            if ( Settings.TraceTaskChanges )
            {
                _tracer.Trace( task );
            }
        }

        private IResource GetTaskResource()
        {
            IResource task = Core.ResourceStore.FindUniqueResource( STR.Task, PROP.EntryID, _entryID );
            if ( task == null )
            {
                if ( _OMTaskId != null && _OMTaskId.Length > 0 )
                {
                    IResourceList taskList = Core.ResourceStore.FindResourcesWithProp( STR.Task, PROP.OMTaskId );
                    foreach ( IResource candidat in taskList )
                    {

                        if ( ( candidat.GetStringProp( PROP.OMTaskId ) == _OMTaskId ) && !candidat.HasProp( PROP.EntryID ) )
                        {
                            return candidat;
                        }
                    }
                }
            }
            return task;
        }

        private void SetDateProp( IResource task, int prop, DateTime value )
        {
            DateTime propDate = task.GetDateProp( prop );
            if ( value == DateTime.MinValue )
            {
                if ( propDate != DateTime.MinValue )
                {
                    task.DeleteProp( prop );
                }
                return;
            }
            task.SetProp( prop, value );
        }

        public override string Name
        {
            get { return "Import task"; }
        }
    }
    internal class Task
    {
        private Task(){}
        public static int GetStatus( IEMessage message, bool retFalse )
        {
            int tag = message.GetIDsFromNames( ref GUID.set2, lID.taskStatus, PropType.PT_LONG );
            int status = message.GetLongProp( tag, retFalse );
            if ( status == -9999 )  return status;
            if ( status < 0 ) status = 0;
            return status;
        }
        public static int GetReminderActive( IEMessage message )
        {
            int tag = message.GetIDsFromNames( ref GUID.set1, lID.taskReminderActive, PropType.PT_BOOLEAN );
            return message.GetBoolProp( tag ) ? 1:0;
        }
        public static string GetOMTaskID( IEMessage message )
        {
            int tag = message.GetIDsFromNames( ref GUID.set4, "OMTaskID", PropType.PT_STRING8 );
            return message.GetStringProp( tag );
        }
        public static string GetSubject( IEMessage message )
        {
            string ret = message.GetStringProp( MAPIConst.PR_SUBJECT );
            if ( ret == null )
            {
                ret = string.Empty;
            }
            return ret;
        }
        public static string GetDescription( IEMessage message )
        {
            string ret = message.GetPlainBody();
            if ( ret == null )
            {
                ret = string.Empty;
            }
            return ret;
        }
        public static DateTime GetDueDate( IEMessage message )
        {
            int tag = message.GetIDsFromNames( ref GUID.set2, lID.taskDueDate, PropType.PT_SYSTIME );
            return message.GetDateTimeProp( tag );
        }
        public static DateTime GetRemindDate( IEMessage message )
        {
            int tag = message.GetIDsFromNames( ref GUID.set1, lID.taskRemindDate, PropType.PT_SYSTIME );
            DateTime remindDate = message.GetDateTimeProp( tag );
            tag = message.GetIDsFromNames( ref GUID.set1, lID.taskSnoozeDate, PropType.PT_SYSTIME );
            DateTime snoozeDate = message.GetDateTimeProp( tag );
            return remindDate > snoozeDate ? remindDate : snoozeDate;
        }
        public static DateTime GetStartDate( IEMessage message )
        {
            int tag = message.GetIDsFromNames( ref GUID.set2, lID.taskStartDate, PropType.PT_SYSTIME );
            return message.GetDateTimeProp( tag );
        }
        public static int GetPriority( IEMessage message )
        {
            int priority = message.GetLongProp( MAPIConst.PR_PRIORITY, true );
            switch ( priority )
            {
                case -1:
                    return 2;
                case 1:
                    return 1;
            }
            return 0;
        }
    }
}
