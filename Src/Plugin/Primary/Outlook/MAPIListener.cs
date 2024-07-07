// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Diagnostics;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class LibManagerBase : ILibManager
    {
        private HashSet _forms = new HashSet();
        private int _nextID = 0;
        private DateTime _lastReadPropAttempt;
        private int _lastReadProp_id = 0;

        public LibManagerBase()
        {
            _lastReadPropAttempt = DateTime.MaxValue;
            if( Core.SettingStore.ReadBool( "Outlook", "EnableStuckWatchDog", true ) )
            {
                Core.NetworkAP.QueueJobAt( DateTime.Now.AddMinutes( 1 ), new MethodInvoker( CheckStuck ) );
            }
        }

        public int FormCount()
        {
            return _forms.Count;
        }
        public int RegisterForm()
        {
            int id = _nextID++;
            _forms.Add( id );
            return id;
        }

        public void UnregisterForm( int id )
        {
            _forms.Remove( id );
        }
        public void BeginReadProp( int prop_id )
        {
            _lastReadProp_id = prop_id;
            _lastReadPropAttempt = DateTime.Now;
        }
        public void EndReadProp()
        {
            _lastReadPropAttempt = DateTime.MaxValue;
        }
        private void CheckStuck()
        {
            if ( !Settings.UseTimeoutForReadingProp ) return;
            if( DateTime.Now.AddMinutes( -3 ) > _lastReadPropAttempt )
            {
                Tracer._Trace( "There is stuck while reading property with TAG = " + _lastReadProp_id );
                OutlookSession.OutlookProcessor.AbortThread();
            }
            else
            {
                Core.NetworkAP.QueueJobAt( DateTime.Now.AddSeconds( 10 ), new MethodInvoker( CheckStuck ) );
            }
        }

        public void DeleteMessage( string entryID )
        {
            IResource mail = Core.ResourceStore.FindUniqueResource( STR.Email, PROP.EntryID, entryID );
            if ( mail != null )
            {
                PairIDs mailIDs = PairIDs.Get( mail );
                if ( mailIDs != null )
                {
                    OutlookSession.DeleteMessage( mailIDs.StoreId, entryID, false );
                }
                else
                {
                    Mail.ForceDelete( mail );
                }
            }
        }

        private static void CopyMessage( string entryID, string folderID, bool move )
        {
            IResource mail = Core.ResourceStore.FindUniqueResource( STR.Email, PROP.EntryID, entryID );
            if ( mail == null )
            {
                return;
            }
            IResource folder = Core.ResourceStore.FindUniqueResource( STR.MAPIFolder, PROP.EntryID, folderID );
            if ( folder == null )
            {
                return;
            }
            new MoveMessageToFolderAction(!move).DoMove( folder, mail.ToResourceList() );
        }

        public void MoveMessage( string entryID, string folderID )
        {
            CopyMessage( entryID, folderID, true );
        }
        public void CopyMessage( string entryID, string folderID )
        {
            CopyMessage( entryID, folderID, false );
        }
    }

    internal class MAPIListenerBase : IMAPIListener
    {
        #region IMAPIListener Members

        public virtual void OnMailAdd(MAPINtf ntf){}
        public virtual void OnNewMail(MAPINtf ntf){}
        public virtual void OnFolderModify(MAPINtf ntf){}
        public virtual void OnMailModify(MAPIFullNtf ntf){}
        public virtual void OnMailMove(MAPIFullNtf ntf){}
        public virtual void OnFolderAdd(MAPINtf ntf){}
        public virtual void OnMailCopy(MAPIFullNtf ntf){}
        public virtual void OnMailDelete(MAPINtf ntf){}
        public virtual void OnFolderCopy(MAPIFullNtf ntf){}
        public virtual void OnFolderDelete(MAPINtf ntf){}
        public virtual void OnFolderMove(MAPIFullNtf ntf){}
        #endregion
    }

    internal class MAPIListenerStub : IMAPIListener
    {
        private IMAPIListener _mapiListener = null;
        public MAPIListenerStub( IMAPIListener mapiListener )
        {
            _mapiListener = mapiListener;
        }
        public void SetListener( IMAPIListener mapiListener )
        {
            _mapiListener = mapiListener;
        }
        #region IMAPIListener Members

        public virtual void OnMailAdd(MAPINtf ntf)
        {
            if ( _mapiListener != null )
                _mapiListener.OnMailAdd( ntf );
        }
        public virtual void OnNewMail(MAPINtf ntf)
        {
            if ( _mapiListener != null )
                _mapiListener.OnNewMail( ntf );
        }
        public virtual void OnFolderModify(MAPINtf ntf)
        {
            if ( _mapiListener != null )
                _mapiListener.OnFolderModify( ntf );
        }
        public virtual void OnMailModify(MAPIFullNtf ntf)
        {
            if ( _mapiListener != null )
                _mapiListener.OnMailModify( ntf );
        }
        public virtual void OnMailMove(MAPIFullNtf ntf)
        {
            if ( _mapiListener != null )
               _mapiListener.OnMailMove( ntf );
        }
        public virtual void OnFolderAdd(MAPINtf ntf)
        {
            if ( _mapiListener != null )
                _mapiListener.OnFolderAdd( ntf );
        }
        public virtual void OnMailCopy(MAPIFullNtf ntf)
        {
            if ( _mapiListener != null )
                _mapiListener.OnMailCopy( ntf );
        }
        public virtual void OnMailDelete(MAPINtf ntf)
        {
            if ( _mapiListener != null )
                _mapiListener.OnMailDelete( ntf );
        }
        public virtual void OnFolderCopy(MAPIFullNtf ntf)
        {
            if ( _mapiListener != null )
                _mapiListener.OnFolderCopy( ntf );
        }
        public virtual void OnFolderDelete(MAPINtf ntf)
        {
            if ( _mapiListener != null )
                _mapiListener.OnFolderDelete( ntf );
        }
        public virtual void OnFolderMove(MAPIFullNtf ntf)
        {
            if ( _mapiListener != null )
                _mapiListener.OnFolderMove( ntf );
        }
        #endregion
    }

    internal class MAPIListener : IMAPIListener
    {
        internal class JobStrategy
        {
            private JobPriority _priority;
            protected JobStrategy(){}
            public JobStrategy( JobPriority priority )
            {
                _priority = priority;
            }
            public virtual void QueueJob( Delegate Method, params object[] args )
            {
                if ( OutlookSession.OutlookProcessor != null )
                {
                    OutlookSession.OutlookProcessor.QueueJob( _priority, Method, args );
                }
            }
            public virtual void QueueJob( AbstractJob job )
            {
                if ( OutlookSession.OutlookProcessor != null )
                {
                    OutlookSession.OutlookProcessor.QueueJob( _priority, job );
                }
            }
            public JobPriority JobPriority { get { return _priority; } }
        }
        internal class JobStrategyTimed : JobStrategy
        {
            private readonly int _seconds;
            public JobStrategyTimed( int seconds )
            {
                _seconds = seconds;
            }
            public override void QueueJob( Delegate Method, params object[] args )
            {
                OutlookSession.OutlookProcessor.QueueJobAt( DateTime.Now.AddSeconds( _seconds ), Method, args );
            }
            public override void QueueJob( AbstractJob job )
            {
                OutlookSession.OutlookProcessor.QueueJobAt( DateTime.Now.AddSeconds( _seconds ), job );
            }
        }
        public MAPIListener( string storeID )
        {
            _storeID = storeID;
            _bTrace = Settings.TraceOutlookListeners;
            string strategy = Settings.ListenersStrategy;
            if ( strategy != null && strategy.ToLower().Equals( "timed" ) )
            {
                int delayInSeconds = Settings.ListenersStrategyTimeInSeconds;
                _jobStrategy = new JobStrategyTimed( delayInSeconds );
            }
            else
            {
                _jobStrategy = new JobStrategy( JobPriority.AboveNormal );
            }
        }
        private bool _bTrace;
        private string _storeID;

        private readonly Tracer _tracer = new Tracer( "MAPIListener" );
        private static JobStrategy _jobStrategy;
        #region IMAPINotification Members

        private void Trace( string str )
        {
            if ( _bTrace )
            {
                _tracer.Trace( str );
            }
        }
        private void Trace( string category, string idName, string id )
        {
            int length = 0;
            int hash = 0;
            if ( id != null )
            {
                length = id.Length;
                hash = id.GetHashCode();
            }
            Trace( category + ": " + idName + " = '" + hash + "' " + length + "/" + id );
        }

        private void Trace( string category, MAPINtf ntf )
        {
            Trace( category, "storeID", _storeID );
            Trace( category, "parentID", ntf.ParentID );
            Trace( category, "entryID", ntf.EntryID );
        }
        private bool CheckArgument( string category, MAPINtf ntf )
        {
            Guard.NullArgument( ntf, "ntf" );
            if ( _bTrace )
            {
                Trace( category, ntf );
            }
            if ( ntf.EntryID != null && ntf.EntryID.Length > 0 )
            {
                bool ret = OutlookSession.IsMailExported( ntf.EntryID );
                if ( ret )
                {
                    Trace( category + ": event is ignored" );
                }
                return !ret;
            }
            return true;
        }

        private bool CheckArgument( string category, MAPIFullNtf ntf )
        {
            bool bRet = CheckArgument( category, (MAPINtf)ntf );
            if ( _bTrace )
            {
                Trace( category, "oldParentID", ntf.OldParentID );
                Trace( category, "oldEntryID", ntf.OldEntryID );
            }
            return bRet;
        }

        private static bool CheckStorageIgnored( string storeId )
        {
            IResource store = Core.ResourceStore.FindUniqueResource( STR.MAPIInfoStore, PROP.EntryID, storeId );
            if ( store != null )
            {
                return store.HasProp( PROP.IgnoredFolder );
            }
            return true;
        }

        #region OnNewMail
        public void OnNewMail(MAPINtf ntf)
        {
            if ( CheckArgument( "OnNewMail", ntf ) && Settings.ProcessMessageNew )
            {
                _jobStrategy.QueueJob( new DelegateMAPINtf( OnNewMailImpl ), ntf );
            }
        }

        private void OnNewMailImpl(MAPINtf ntf)
        {
            Trace( "[New] *Email* - OnNewMailImpl" );
            if ( CheckStorageIgnored( _storeID ) )
            {
                Trace( "OnNewMailImpl: storage is ignored" );
                return;
            }
            FolderDescriptor folderDescriptor = FolderDescriptor.Get( ntf.ParentID, _storeID );
            if ( folderDescriptor == null )
            {
                Trace( "OnNewMailImpl: folderDescriptor == null" );
                return;
            }

            if ( folderDescriptor.ContainerClass == FolderType.IMAP )
            {
                IEFolder folder = OutlookSession.OpenFolder( ntf.ParentID, _storeID );
                if ( folder == null )
                {
                    return;
                }
                using ( folder )
                {
                    if ( !ProcessMailAddNtf.ProcessIMAPMessage( folder, ntf.EntryID ) )
                    {
                        return;
                    }
                }
            }

            IEMessage message = OutlookSession.OpenMessage( ntf.EntryID, _storeID );
            if ( message == null )
            {
                Trace( "OnNewMailImpl: cannot open mapi message" );
                return;
            }
            using ( message )
            {
                try
                {
                    string entryId = OutlookSession.GetMessageID( message );
                    if ( OutlookSession.IsMailExported( entryId ) )
                    {
                        Trace( "OnNewMailImpl: mail is exported at the moment" );
                        return;
                    }
                    new NewMailDescriptor( folderDescriptor, entryId, message ).QueueJob( JobPriority.AboveNormal );
                }
                catch ( System.Threading.ThreadAbortException ex )
                {
                    Tracer._TraceException( ex );
                }
                catch ( Exception exception )
                {
					Core.ReportBackgroundException( exception );
                }
            }
        }
        #endregion OnNewMail

        #region OnMailAdd
        public void OnMailAdd( MAPINtf ntf )
        {
            if ( CheckArgument( "OnMailAdd", ntf ) && Settings.ProcessMessageAdd )
            {
                _jobStrategy.QueueJob( new DelegateMAPINtf( OnMailAddImpl ), ntf );
            }
        }
        private void OnMailAddImpl( MAPINtf ntf )
        {
            if ( CheckStorageIgnored( _storeID ) )
            {
                return;
            }
            ProcessMailAddNtf.DoJob( ntf, _storeID );
        }
        #endregion OnMailAdd

        #region OnMailModify
        public void OnMailModify( MAPIFullNtf ntf )
        {
            if ( CheckArgument( "OnMailModify", ntf ) && Settings.ProcessMessageModify )
            {
                _jobStrategy.QueueJob( new DelegateMAPIFullNtf( OnMailModifyImpl ), ntf );
            }
        }

        private void OnMailModifyImpl( MAPIFullNtf ntf )
        {
            if ( CheckStorageIgnored( _storeID ) )
            {
                return;
            }
            try
            {
                IEFolder folder = OutlookSession.OpenFolder( ntf.ParentID, _storeID );
                if ( folder == null ) return;
                using( folder )
                {
                    FolderDescriptor folderDescriptor = FolderDescriptor.Get( folder );
                    if ( folderDescriptor.ContainerClass == FolderType.IMAP )
                    {
                        if ( !ProcessMailAddNtf.ProcessIMAPMessage( folder, ntf.EntryID ) )
                        {
                            return;
                        }
                    }
                    IEMessage message = OutlookSession.OpenMessage( ntf.EntryID, _storeID );
                    if ( message == null ) return;
                    using ( message )
                    {
                        ProcessMailModifyImpl( ntf, message, folderDescriptor );
                    }
                }
            }
            catch ( System.Threading.ThreadAbortException ex )
            {
                Tracer._TraceException( ex );
            }
            catch ( Exception exception )
            {
                Core.ReportBackgroundException( exception );
            }
        }

        private void ProcessMailModifyImpl( MAPIFullNtf ntf, IEMessage message, FolderDescriptor folderDescriptor )
        {
            string messageClass = MessageType.GetMessageClass( message );
            if ( MessageType.InterpretAsMail( messageClass ) )
            {
                string entryId = OutlookSession.GetMessageID( message );
                if ( OutlookSession.IsMailExported( entryId ) )
                {
                    return;
                }
                new MailDescriptor( folderDescriptor, entryId, message, MailDescriptor.UpdateState ).QueueJob( JobPriority.AboveNormal );
            }
            else if ( MessageType.InterpretAsContact( messageClass ) )
            {
                string realEntryId = message.GetBinProp( MAPIConst.PR_ENTRYID );
                if ( OutlookSession.IsMailExported( realEntryId ) )
                {
                    return;
                }
                ContactDescriptorWrapper.Do( folderDescriptor, realEntryId, realEntryId );
            }
            else if ( MessageType.InterpretAsTask( messageClass ) )
            {
                _tracer.Trace( "Task was modified" );
                string realEntryId = message.GetBinProp( MAPIConst.PR_ENTRYID );
                if ( OutlookSession.IsMailExported( realEntryId ) )
                {
                    return;
                }
                TaskDescriptor.Do( JobPriority.AboveNormal, folderDescriptor, message, realEntryId );
            }
            else
            {
                ntf = ntf;
//                _tracer.Trace( "Unknown item of class " + messageClass + " was modified" );
            }
        }
        #endregion OnMailModify

        #region OnMailMove
        public void OnMailMove(MAPIFullNtf ntf)
        {
            if ( CheckArgument( "OnMailMove", ntf ) && Settings.ProcessMessageMove )
            {
                _jobStrategy.QueueJob( new DelegateMAPIFullNtf( OnMailMoveImpl ), ntf );
            }
        }

        private void OnMailMoveImpl( MAPIFullNtf ntf )
        {
            if ( CheckStorageIgnored( _storeID ) )
            {
                return;
            }
            FolderDescriptor folderDescriptor = FolderDescriptor.Get( ntf.ParentID, _storeID );
            if ( folderDescriptor == null ) return;

            try
            {
                IEMessage message = OutlookSession.OpenMessage( ntf.EntryID, _storeID );
                if ( message == null )
                {
                    return;
                }
                using ( message )
                {
                    string realID = message.GetBinProp( MAPIConst.PR_ENTRYID );
                    if ( realID == null )
                    {
                        realID = ntf.EntryID;
                    }
                    else
                    {
                        if ( OutlookSession.IsMailExported( realID ) )
                        {
                            return;
                        }
                    }
                    string messageClass = MessageType.GetMessageClass( message );
                    if ( MessageType.InterpretAsMail( messageClass ) )
                    {
                        Core.ResourceAP.QueueJob( JobPriority.AboveNormal, new MailMovedDescriptor( ntf, _storeID ) );
                    }
                    else
                    if ( MessageType.InterpretAsContact( messageClass ) )
                    {
                        if ( folderDescriptor.ContainerClass != FolderType.Contact )
                        {
                            _tracer.Trace( "Contact was moved to deleted items" );
                            IResource contact = Core.ResourceStore.FindUniqueResource( STR.Contact, PROP.EntryID, realID );
                            string entryID = realID;
                            if ( contact == null && ntf.OldEntryID != null )
                            {
                                _tracer.Trace( "contact not found" );
                                contact = Core.ResourceStore.FindUniqueResource( STR.Contact, PROP.EntryID, ntf.OldEntryID );
                                entryID = ntf.EntryID;
                            }
                            if ( contact != null )
                            {
                                _tracer.Trace( "contact found" );
                                _tracer.Trace( "RemoveFromSync" );
                                Contact.RemoveFromSync( contact, entryID );
                            }
                            else
                            {
                                _tracer.Trace( "ClearInvalidEntryIDFromContacts" );
                                _jobStrategy.QueueJob( new MethodInvoker( REGISTRY.ClearInvalidEntryIDFromContacts ) );
                            }
                        }
                        else
                        {
                            string searchEntryID = realID;
                            if ( realID != ntf.OldEntryID )
                            {
                                searchEntryID = ntf.OldEntryID;
                            }

                            _tracer.Trace( "ContactDescriptorWrapper process moving" );
                            ContactDescriptorWrapper.Do( _jobStrategy.JobPriority, folderDescriptor, realID, searchEntryID );
                        }
                        //OutlookSession.OutlookProcessor.QueueJob( new ContactDescriptorWrapper( folderDescriptor, ntf.EntryID ) );
                    }
                    else if ( MessageType.InterpretAsTask( messageClass ) )
                    {
                        _tracer.Trace( "Task was moved" );
                        if ( Core.ResourceStore.FindUniqueResource( STR.Task, PROP.EntryID, realID ) != null )
                        {
                            TaskDescriptor.Do( JobPriority.AboveNormal, folderDescriptor, message, realID );
                        }
                        else
                        {
                            RefreshTaskFolder( folderDescriptor );
                            FolderDescriptor oldFolderDescriptor = FolderDescriptor.Get( ntf.OldParentID, _storeID );
                            if ( oldFolderDescriptor != null )
                            {
                                RefreshTaskFolder( oldFolderDescriptor );
                            }
                        }
                    }
                    else
                    {
                        _tracer.Trace( "Unknown item of class " + messageClass + " was moved" );
                    }
                }
            }
            catch ( System.Threading.ThreadAbortException ex )
            {
                Tracer._TraceException( ex );
            }
            catch ( Exception exception )
            {
                Core.ReportBackgroundException( exception );
            }
        }

        private static void RefreshTaskFolder( FolderDescriptor folderDescriptor )
        {
            if ( folderDescriptor.ContainerClass == FolderType.Task )
            {
                RefreshFolderDescriptor.Do( JobPriority.AboveNormal, folderDescriptor.FolderIDs, DateTime.MinValue );
            }
        }
        #endregion OnMailMove

        #region OnMailDelete
        public void OnMailDelete( MAPINtf ntf )
        {
            if ( CheckArgument( "OnMailDelete", ntf ) )
            {
                Trace( "Attempt to ignore OnMailDelete" );
            }

            //if ( CheckArgument( "OnMailDelete", ntf ) )
            {
                _jobStrategy.QueueJob( new DelegateMAPINtf( OnMailDeleteImpl ), ntf );
            }
        }

        private void OnMailDeleteImpl( MAPINtf ntf )
        {
            if( !CheckStorageIgnored( _storeID ) )
            {
                Core.ResourceAP.QueueJob( JobPriority.AboveNormal, new MailDeletedDescriptor( ntf, _storeID ) );
            }
        }
        #endregion OnMailDelete

        #region OnFolderAdd
        public void OnFolderAdd( MAPINtf ntf )
        {
            if ( CheckArgument( "OnFolderAdd", ntf ) )
            {
                _jobStrategy.QueueJob( new DelegateMAPINtf( OnFolderAddImpl ), ntf );
            }
        }

        private void OnFolderAddImpl( MAPINtf ntf )
        {
            if ( CheckStorageIgnored( _storeID ) )
            {
                return;
            }
            FolderAddDescriptor.Do( JobPriority.AboveNormal, ntf, _storeID );
        }
        #endregion OnFolderAdd

        #region OnMailCopy
        public void OnMailCopy( MAPIFullNtf ntf )
        {
            if ( CheckArgument( "OnMailCopy", ntf ) )
            {
                OnMailAdd( ntf );
            }
        }

        #endregion OnMailCopy

        #region OnFolderModify
        public void OnFolderModify( MAPINtf ntf )
        {
            if ( CheckArgument( "OnFolderModify", ntf ) && Settings.ProcessFolderModify )
            {
                _jobStrategy.QueueJob(  new DelegateMAPINtf( OnFolderModifyImpl ), ntf );
            }
        }
        private void OnFolderModifyImpl( MAPINtf ntf )
        {
            Core.ResourceAP.QueueJob( new FolderModifiedDescriptor( ntf, _storeID, false ) );
        }
        #endregion OnFolderModify

        #region OnFolderCopy
        public void OnFolderCopy( MAPIFullNtf ntf )
        {
            if ( CheckArgument( "OnFolderCopy", ntf ) )
            {
                _jobStrategy.QueueJob( new DelegateMAPIFullNtf( OnFolderCopyImpl ), ntf );
            }
        }

        private void OnFolderCopyImpl( MAPIFullNtf ntf )
        {
            if ( CheckStorageIgnored( _storeID ) )
            {
                return;
            }
            FolderAddDescriptor.Do( JobPriority.AboveNormal, ntf, _storeID );
        }
        #endregion OnFolderCopy

        #region OnFolderDelete
        public void OnFolderDelete( MAPINtf ntf )
        {
            if ( CheckArgument( "OnFolderDelete", ntf ) )
            {
                _jobStrategy.QueueJob( new DelegateMAPINtf( OnFolderDeleteImpl ), ntf );
            }
        }

        private void OnFolderDeleteImpl(MAPINtf ntf)
        {
            if ( CheckStorageIgnored( _storeID ) )
            {
                return;
            }
            Core.ResourceAP.QueueJob( JobPriority.AboveNormal, new FolderDeletedDescriptor( ntf, _storeID ) );
        }
        #endregion OnFolderDelete

        #region OnFolderMove
        public void OnFolderMove( MAPIFullNtf ntf )
        {
            if ( CheckArgument( "OnFolderMove", ntf ) )
            {
                _jobStrategy.QueueJob( new DelegateMAPIFullNtf( OnFolderMoveImpl ), ntf );
            }
        }
        private void OnFolderMoveImpl( MAPIFullNtf ntf )
        {
            if ( CheckStorageIgnored( _storeID ) )
            {
                return;
            }
            Core.ResourceAP.QueueJob( JobPriority.AboveNormal, new FolderModifiedDescriptor( ntf, _storeID, true ) );
        }
        #endregion OnFolderMove

        private delegate void DelegateMAPIFullNtf( MAPIFullNtf ntf );
        private delegate void DelegateMAPINtf( MAPINtf ntf );

        #endregion
    }
}
