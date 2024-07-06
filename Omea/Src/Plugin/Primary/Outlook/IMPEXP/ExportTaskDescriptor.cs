// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using EMAPILib;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class ExportTaskDescriptor : AbstractNamedJob
    {
        private IResource _task;
        private bool _createNew;
        private string _OMTaskId = null;
        private static HashSet _exportedTasks = new HashSet();
        private bool _abort = false;
        private bool _created = false;

        public ExportTaskDescriptor( IResource task )
        {
            Guard.NullArgument( task, "task" );
            _task = task;
            _createNew = !_task.HasProp( PROP.EntryID );
            if ( _exportedTasks.Contains( _task.Id ) )
            {
                _abort = true;
                return;
            }
            _exportedTasks.Add( _task.Id );
            ResourceProxy proxy = new ResourceProxy( _task );
            if ( !_createNew )
            {
                proxy.DeleteProp( PROP.OMTaskId );
            }
            else
            {
                _OMTaskId = DateTime.Now.Ticks.ToString();
                proxy.SetProp( PROP.OMTaskId, _OMTaskId );
            }
        }

        public void QueueJob( JobPriority priority )
        {
            OutlookSession.OutlookProcessor.QueueJob( priority, this );
        }

        private void SetDateProp( string what, IEMessage message, Guid guid, int id, DateTime dateTime )
        {
            try
            {
                int tag = message.GetIDsFromNames( ref guid, id, PropType.PT_SYSTIME );
                message.SetDateTimeProp( tag, dateTime );
            }
            catch( System.ArgumentOutOfRangeException )
            {
                string str = "ExportTaskDescriptor -- Can not set datetime for " + what + " [" + dateTime.ToString() + "]";
                Trace.WriteLine( str );
            }
        }
        private void ExecuteImpl()
        {
            bool wasChanges = false;
            IEMessage message = OpenMessage( ref wasChanges );
            if ( message == null ) return;

            using ( message )
            {
                SetPriority( message, ref wasChanges );
                SetSubject( message, ref wasChanges );
                SetDescription( message, ref wasChanges );
                SetDueDate( message, ref wasChanges );
                SetStartDate( message, ref wasChanges );
                SetRemindDate( message, ref wasChanges );
                SetRemindActive( message, ref wasChanges );
                SetStatus( message, ref wasChanges );
                if ( Settings.SyncTaskCategory )
                {
                    SetCategories( message, ref wasChanges );
                }
                SetOMTaskID( message, ref wasChanges );
                if ( wasChanges )
                {
                    OutlookSession.SaveChanges( _created, "Export task resource id = " + _task.Id, message, message.GetBinProp( MAPIConst.PR_ENTRYID ) );
                }
            }
        }
        protected override void Execute()
        {
            if ( _abort )
            {
                return;
            }

            try
            {
                ExecuteImpl();
            }
            finally
            {
                _exportedTasks.Remove( _task.Id );
            }
        }

        private void SetCategories( IEMessage message, ref bool wasChanges )
        {
            if ( ExportCategories.ProcessCategories( message, _task ) )
            {
                wasChanges = true;
            }
        }
        private void SetStatus( IEMessage message, ref bool wasChanges )
        {
            int curStatus = _task.GetIntProp( PROP.Status );
            if ( curStatus != Task.GetStatus( message, true ) )
            {
                wasChanges = true;
                int tag = message.GetIDsFromNames( ref GUID.set2, lID.taskStatus, PropType.PT_LONG );
                message.SetLongProp( tag, curStatus );
                bool completed = (curStatus == 2);
                tag = message.GetIDsFromNames( ref GUID.set2, lID.taskCompleted, PropType.PT_BOOLEAN );
                message.SetBoolProp( tag, completed );
            }
        }

        private void SetOMTaskID( IEMessage message, ref bool wasChanges )
        {
            if ( Task.GetOMTaskID( message ) != _OMTaskId )
            {
                int tag = message.GetIDsFromNames( ref GUID.set4, "OMTaskID", PropType.PT_STRING8 );
                message.SetStringProp( tag, _OMTaskId );
                wasChanges = true;
            }
        }

        private void SetRemindActive( IEMessage message, ref bool wasChanges )
        {
            int reminderActive = ( _task.GetDateProp( PROP.RemindDate ) == DateTime.MinValue ) ? 0 : 1;
            if ( reminderActive != Task.GetReminderActive( message ) )
            {
                int tag = message.GetIDsFromNames( ref GUID.set1, lID.taskReminderActive, PropType.PT_BOOLEAN );
                message.SetBoolProp( tag, (reminderActive == 1) ? true : false );
                wasChanges = true;
            }
        }

        private void SetRemindDate( IEMessage message, ref bool wasChanges )
        {
            DateTime remindDate = _task.GetDateProp( PROP.RemindDate );
            if ( remindDate != Task.GetRemindDate( message ) )
            {
                SetDateProp( "Remind",  message, GUID.set1, lID.taskRemindDate, remindDate );
                SetDateProp( "Snooze", message, GUID.set1, lID.taskSnoozeDate, DateTime.MinValue );
                wasChanges = true;
            }
        }

        private void SetStartDate( IEMessage message, ref bool wasChanges )
        {
            DateTime startDate = _task.GetDateProp( PROP.StartDate );
            if ( startDate != Task.GetStartDate( message ).Date )
            {
                SetDateProp( "Start", message, GUID.set2, lID.taskStartDate, startDate );
                wasChanges = true;
            }
        }

        private void SetDueDate( IEMessage message, ref bool wasChanges )
        {
            DateTime dueDate = _task.GetDateProp( Core.Props.Date );
            if ( dueDate.Date != Task.GetDueDate( message ).Date )
            {
                SetDateProp( "Due", message, GUID.set2, lID.taskDueDate, dueDate );
                wasChanges = true;
            }
        }

        private void SetDescription( IEMessage message, ref bool wasChanges )
        {
            string description = _task.GetPropText( PROP.Description );
            if ( description != Task.GetDescription( message ) )
            {
                message.WriteStringStreamProp( MAPIConst.PR_BODY, description );
                wasChanges = true;
            }
        }

        private void SetSubject( IEMessage message, ref bool wasChanges )
        {
            string subject = _task.GetPropText( Core.Props.Subject );
            if ( subject != Task.GetSubject( message ) )
            {
                message.SetStringProp( MAPIConst.PR_SUBJECT, subject );
                wasChanges = true;
            }
        }

        private void SetPriority( IEMessage message, ref bool wasChanges )
        {
            int priorityOld = Task.GetPriority( message );
            int priority = _task.GetIntProp( PROP.Priority );
            if ( _created || priority != priorityOld )
            {
                if ( priority == 1 )
                {
                    message.SetLongProp( MAPIConst.PR_PRIORITY, 1 );
                    message.SetLongProp( MAPIConst.PR_IMPORTANCE, 2 );
                }
                else if ( priority == 2 )
                {
                    message.SetLongProp( MAPIConst.PR_PRIORITY, -1 );
                    message.SetLongProp( MAPIConst.PR_IMPORTANCE, 0 );
                }
                else
                {
                    message.SetLongProp( MAPIConst.PR_PRIORITY, 0 );
                    message.SetLongProp( MAPIConst.PR_IMPORTANCE, 1 );
                }
                wasChanges = true;
            }
        }

        private IEMessage OpenMessage( ref bool wasChanges )
        {
            if ( _createNew && _OMTaskId == null )
            {
                return null;
            }

            wasChanges = false;
            IEMessage message = null;
            if ( !_createNew )
            {
                PairIDs IDs = PairIDs.Get( _task );
                if ( IDs == null )
                {
                    if ( _task.HasProp( PROP.EntryID ) )
                    {
                        IEFolder taskFolder = OutlookSession.OpenDefaultTaskFolder();
                        if ( taskFolder == null )
                        {
                            return null;
                        }

                        using ( taskFolder )
                        {
                            IDs = new PairIDs( _task.GetStringProp( PROP.EntryID ), taskFolder.GetBinProp( MAPIConst.PR_STORE_ENTRYID ) );
                        }
                    }
                    if ( IDs == null )
                    {
                        return null;
                    }
                }

                message = OutlookSession.OpenMessage( IDs.EntryId, IDs.StoreId );
            }
            if ( message == null )
            {
                IEFolder taskFolder = OutlookSession.OpenDefaultTaskFolder();
                if ( taskFolder == null )
                {
                    return null;
                }
                wasChanges = true;

                using ( taskFolder )
                {
                    message = taskFolder.CreateMessage( "IPM.Task" );
                    _created = true;
                }
            }
            return message;
        }

        public override string Name
        {
            get { return "Exporting task"; }
        }
    }
}
