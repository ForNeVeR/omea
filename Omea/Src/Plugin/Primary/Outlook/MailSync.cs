// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class MailInIndex
    {
        private IntHashSet _ids;
        public void AddResources( IResourceList resources )
        {
            IResourceIdCollection ids = resources.ResourceIds;
            _ids = new IntHashSet( ids.Count / 2 );
            foreach ( int id in ids )
            {
                _ids.Add( id );
            }
        }
        public void TestID( int id )
        {
            if ( _ids != null )
            {
                _ids.Remove( id );
            }
        }
        public void Clear()
        {
            _ids = null;
        }
        public int Count { get { return ( _ids == null ) ? 0 : _ids.Count; } }
        public MailInIndexEnum GetEnumerator()
        {
            IntArrayList ids = IntArrayListPool.Alloc();
            try
            {
                if ( _ids != null )
                {
                    foreach ( IntHashSet.Entry entry in _ids )
                    {
                        ids.Add( entry.Key );
                    }
                }
                return new MailInIndexEnum( ids.ToArray() );
            }
            finally
            {
                IntArrayListPool.Dispose( ids );
            }
        }
    }

    internal class MailInIndexEnum
    {
        private int[] _ids;
        public MailInIndexEnum( int[] ids )
        {
            _ids = ids;
        }
        public int Count { get { return _ids.Length; } }
        public IResource GetResource( int index )
        {
            return Core.ResourceStore.TryLoadResource( _ids[ index ] );
        }
    }

    internal class MailSync : IFolderDescriptorEnumeratorEvent
    {
        private static Tracer _tracer = new Tracer( "MailSync" );
        private DateTime _indexStartDate;
        private MailInIndex _mailsInOldIndex;
        private int _processed = 0;
        private int _foldersCount;
        private bool _idle = false;

        public MailSync( bool computeCounts, DateTime indexStartDate, bool idle )
        {
            Init( computeCounts, indexStartDate, idle );
        }
        public MailSync( bool computeCounts, DateTime indexStartDate )
        {
            Init( computeCounts, indexStartDate, false );
        }
        private void Init( bool computeCounts, DateTime indexStartDate, bool idle )
        {
            _idle = idle;
            _indexStartDate = indexStartDate;
            _mailsInOldIndex = new MailInIndex();

            if ( computeCounts )
            {
                Settings.UpdateProgress( 0, "Computing Outlook Folders count...", "" );

                int Start = Environment.TickCount;
                _foldersCount = OutlookSession.ComputeFolders();

                int Finish = Environment.TickCount - Start;

                Trace.WriteLine( "folder enumeration: " + Finish.ToString() );
                Trace.WriteLine( "folders count: " + _foldersCount.ToString() );

                Start = Environment.TickCount;
            }
        }

        public DateTime IndexStartDate { get { return _indexStartDate; } }

        public void AddMailResources( IResourceList resources )
        {
            _mailsInOldIndex.AddResources( resources );
        }

        public void PrepareMailResources()
        {
            _mailsInOldIndex.Clear();
            IResourceList resMails = Core.ResourceStore.GetAllResources( STR.Email );

            _mailsInOldIndex.AddResources( resMails );
            IResourceList resTasks =
                Core.ResourceStore.FindResourcesWithProp( STR.Task, PROP.EntryID );
            _mailsInOldIndex.AddResources( resTasks );
        }

        #region IFolderDescriptorEnumeratorEvent Members

        public bool FolderFetched( FolderDescriptor parent, FolderDescriptor folder,
                                   out FolderDescriptor folderTag )
        {
            folderTag = null;
            _tracer.Trace( folder.Name );

            if ( Settings.IdleModeManager.Interrupted )
            {
                return false;
            }
            if ( !Folder.IsIgnored( folder ) )
            {
                EnumerateMessageItems( folder );
            }
            return true;
        }

        #endregion

        private void OnFolderFetched( string status )
        {
            if ( Core.ProgressWindow == null )
            {
                return;
            }
            _processed++;
            int processed = _processed;
            if ( processed > _foldersCount )
            {
                processed = _foldersCount;
            }
            int percentage = ( _foldersCount == 0 )
                ? 100
                : processed * 100 / _foldersCount;
            string statusText = "Scanning mail in ( " + status + " ) folder...";
            Settings.UpdateProgress( percentage, statusText, string.Empty );
        }

        private void EnumerateTasks( FolderDescriptor folder, IEFolder mapiFolder )
        {
            OnFolderFetched( folder.Name );

            IEMessages messages = mapiFolder.GetMessages();
            if ( messages != null )
            {
                using ( messages )
                {
                    int count = messages.GetCount();
                    for ( int i = 0; i < count; i++ )
                    {
                        IEMessage message = messages.OpenMessage( i );
                        if ( message != null )
                        {
                            using ( message )
                            {
                                string entryID = OutlookSession.GetMessageID( message );
                                TaskDescriptor.Do( folder, message, entryID );
                            }
                        }
                    }
                }
            }
        }

        public void RemoveDeletedMailsFromIndex()
        {
            try
            {
                int count = 0;
                MailInIndexEnum mailEnum = _mailsInOldIndex.GetEnumerator();
                int total = mailEnum.Count;
//                _tracer.Trace( "Start RemoveDeletedMailsFromIndex" );
//                _tracer.Trace( "RemoveDeletedMailsFromIndex : mail to remove " + total.ToString() );
                int curTickCount = Environment.TickCount;
                for ( int i = 0; i < total; i++ )
                {
                    OutlookSession.ProcessJobs();

                    if ( OutlookSession.OutlookProcessor.ShuttingDown )
                    {
                        break;
                    }
                    if ( _idle && Settings.IdleModeManager.CheckInterruptIdle() )
                    {
                        break;
                    }
                    int percentage = ( total == 0 )
                        ? 100
                        : ++count * 100 / total;
                    if ( percentage > 100 )
                    {
                        percentage = 100;
                    }

                    if ( Environment.TickCount - curTickCount > 500 )
                    {
                        string statusText = "Synchronizing mails(" + count + "/" + total + ")...";
                        Settings.UpdateProgress( percentage, statusText, string.Empty );
                        curTickCount = Environment.TickCount;
                    }

                    IResource resMail = mailEnum.GetResource( i );
                    if ( resMail == null || !Guard.IsResourceLive( resMail ) )
                    {
//                        _tracer.Trace( "RemoveDeletedMailsFromIndex : resMail == null" );
                        continue;
                    }
                    PairIDs messageIDs = PairIDs.Get( resMail );
                    if ( messageIDs == null )
                    {
                        if ( !resMail.HasProp( PROP.EmbeddedMessage ) )
                        {
                            new ResourceProxy( resMail ).DeleteAsync();
                        }
                        continue;
                    }

                    string storeID = messageIDs.StoreId;
                    string folderID = null;
                    IEMessage mapiMessage =
                        OutlookSession.OpenMessage( messageIDs.EntryId, storeID );
                    if ( mapiMessage != null )
                    {
                        using ( mapiMessage )
                        {
                            folderID = mapiMessage.GetBinProp( MAPIConst.PR_PARENT_ENTRYID );
                        }
                    }
                    FolderDescriptor folder = null;
                    if ( folderID != null )
                    {
                        folder = FolderDescriptor.Get( folderID, storeID );
                    }
                    MailSyncToFolder.LinkOrDelete( folder, resMail );
                    OutlookSession.ProcessJobs();
                }
            }
            finally
            {
                _mailsInOldIndex.Clear();
            }
            OutlookSession.ProcessJobs();

        }

        public void EnumerateMessageItems( FolderDescriptor folderDescriptor )
        {
            if ( OutlookSession.WereProblemWithOpeningStorage( folderDescriptor.FolderIDs.StoreId ) ||
                OutlookSession.WereProblemWithOpeningFolder( folderDescriptor.FolderIDs.EntryId ) )
            {
                return;
            }

            try
            {
                IEFolder mapiFolder =
                    OutlookSession.OpenFolder( folderDescriptor.FolderIDs.EntryId, folderDescriptor.FolderIDs.StoreId );
                if ( mapiFolder == null )
                {
                    return;
                }
                using ( mapiFolder )
                {
                    string containerClass = mapiFolder.GetStringProp( MAPIConst.PR_CONTAINER_CLASS );
                    bool taskFolder = ( FolderType.Task.Equals( containerClass ) );
                    if ( taskFolder )
                    {
                        EnumerateTasks( folderDescriptor, mapiFolder );
                    }
                    else
                    {
                        EnumerateMail( folderDescriptor, mapiFolder );
                    }
                }
            }
            catch ( COMException exception )
            {
                _tracer.TraceException( exception );
                if ( exception.ErrorCode == MapiError.MAPI_E_NOT_ENOUGH_DISK )
                {
                    StandartJobs.MessageBox( "Outlook reports that there is no enough disk space.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                }
                OutlookSession.ProblemWithOpeningFolder( folderDescriptor.FolderIDs.EntryId );
                return;
            }
        }
        private void EnumerateMail( FolderDescriptor folder, IEFolder mapiFolder )
        {
            try
            {
                OnFolderFetched( folder.Name );
                int indexed = 0;

                IResource resFolder = Folder.Find( folder.FolderIDs.EntryId );
                DateTime dtRestrict = GetRestrictDate( resFolder );

                IETable table = null;
                try
                {
                    table = mapiFolder.GetEnumTable( dtRestrict );
                }
                catch ( System.UnauthorizedAccessException exception )
                {
                    _tracer.TraceException( exception );
                }
                catch ( OutOfMemoryException exception )
                {
                    _tracer.TraceException( exception );
                }
                if ( table == null )
                {
                    return;
                }
                using ( table )
                {
                    int count = table.GetRowCount();
                    if ( count > 0 )
                    {
                        table.Sort( MAPIConst.PR_MESSAGE_DELIVERY_TIME, false );
                    }
                    for ( uint i = 0; i < count; i++ )
                    {
                        if ( OutlookSession.OutlookProcessor.ShuttingDown )
                        {
                            break;
                        }
                        if ( _idle && Settings.IdleModeManager.CheckInterruptIdle() )
                        {
                            break;
                        }
                        IERowSet row = row = table.GetNextRow();
                        if ( row == null )
                        {
                            continue;
                        }
                        using ( row )
                        {
                            if ( row.GetLongProp( 6 ) != 1 )
                            {
                                ProcessRow( row, folder, mapiFolder, ref indexed );
                            }
                        }
                    }
                }
                if ( Settings.IdleModeManager.CompletedIdle )
                {
                    Folder.SetSeeAllAsync( resFolder );
                }

                _tracer.Trace( "Indexed " + indexed + " messages in folder " + folder.Name );
            }
            finally
            {
                OutlookSession.ProcessJobs();
            }
        }

        private string ProcessRow( IERowSet row, FolderDescriptor folder,
                                   IEFolder mapiFolder, ref int indexed )
        {
            string entryID = row.GetBinProp( 1 );
            if ( entryID == null )
            {
                entryID = row.GetBinProp( 0 );
            }
            string messageClass = row.GetStringProp( 3 );
            if ( messageClass == null )
            {
                messageClass = string.Empty;
            }

            IResource email =
                Core.ResourceStore.FindUniqueResource( STR.Email, PROP.EntryID, entryID );
            if ( email != null )
            {
                _mailsInOldIndex.TestID( email.Id );
                UpdateMail( row, email, messageClass, entryID, folder, mapiFolder );
                indexed++;
            }
            else
            {
                AddMail( messageClass, entryID, folder, mapiFolder, row.GetStringProp( 7 ) );
                indexed++;
            }
            OutlookSession.ProcessJobs();
            return entryID;
        }

        private void UpdateMail( IERowSet row, IResource email, string messageClass, string entryId,
                                 FolderDescriptor folder, IEFolder mapiFolder )
        {
            Guard.NullArgument( messageClass, "messageClass" );

            bool checkForDateTimeNeeded = false;
            bool bWereChanges = false;
            bool interpretAsMail = MessageType.InterpretAsMail( messageClass );
            if ( interpretAsMail )
            {
                bWereChanges = WereChanges( row, email, out checkForDateTimeNeeded );
            }

            if ( bWereChanges )
            {
                IEMessage message = OutlookSession.OpenMessage( folder.FolderIDs.EntryId, mapiFolder, entryId );
                if ( message == null )
                {
                    return;
                }

                using ( message )
                {
                    if ( checkForDateTimeNeeded )
                    {
                        DateTime lastModifiedDate = message.GetDateTimeProp( MAPIConst.PR_LAST_MODIFICATION_TIME );
                        lastModifiedDate = lastModifiedDate.ToUniversalTime();
                        if ( lastModifiedDate.Equals( email.GetProp( PROP.LastModifiedTime ) ) )
                        {
                            bWereChanges = false;
                        }
                    }
                    if ( bWereChanges )
                    {
                        Core.ResourceAP.QueueJob( new MailDescriptor( folder, entryId, message, MailDescriptor.UpdateState, row.GetStringProp( 7 ) ) );
                    }
                    else
                    {
                        Core.ResourceAP.QueueJob( new SyncOnlyMailDescriptor( folder, entryId, message ) );
                    }
                }
            }
            else
            {
                MailSyncToFolder.LinkOrDelete( folder, email );
            }
        }

        private bool WereChanges( IERowSet row, IResource email, out bool checkForDateTimeNeeded )
        {
            checkForDateTimeNeeded = false;
            DateTime lastModifiedDate = row.GetDateTimeProp( 2 );
            if ( lastModifiedDate != DateTime.MinValue )
            {
                lastModifiedDate = lastModifiedDate.ToUniversalTime();
            }
            else
            {
                checkForDateTimeNeeded = true;
            }
            bool bWereChanges = lastModifiedDate.CompareTo( email.GetProp( PROP.LastModifiedTime ) ) != 0;
            if ( !bWereChanges )
            {
                long mesFlags = row.GetLongProp( 4 );
                bool unread = ( mesFlags & 1 ) == 0;
                if ( unread != email.HasProp( Core.Props.IsUnread ) )
                {
                    bWereChanges = true;
                }
            }
            return bWereChanges;
        }

        private static void TryAgainToAddMail( FolderDescriptor folderDescriptor, string entryID, string longBody )
        {
            IEMessage message = null;
            try
            {
                message = OutlookSession.OpenMessage( entryID, folderDescriptor.FolderIDs.StoreId );
            }
            catch ( COMException exception )
            {
                if ( exception.ErrorCode == MapiError.MAPI_E_SUBMITTED )
                {
                    Core.NetworkAP.QueueJobAt( DateTime.Now.AddMinutes( 1 ),
                        new DelegateTryAgainToAddMail( TryAgainToAddMail ), folderDescriptor, entryID, longBody );
                }
            }
            using ( message )
            {
                Core.ResourceAP.QueueJob( new MailDescriptor( folderDescriptor, entryID, message, longBody ) );
            }
        }
        private delegate void DelegateTryAgainToAddMail( FolderDescriptor folderDescriptor, string entryID, string longBody );

        private static void AddMail( string messageClass, string entryID,
                                     FolderDescriptor folderDescriptor, IEFolder mapiFolder, string longBody )
        {
            Guard.NullArgument( messageClass, "messageClass" );
            if ( MessageType.InterpretAsMail( messageClass ) )
            {
                IEMessage message = null;
                try
                {
                    message = OutlookSession.OpenMessage( mapiFolder, entryID );
                }
                catch ( COMException exception )
                {
                    if ( exception.ErrorCode == MapiError.MAPI_E_SUBMITTED )
                    {
                        Core.NetworkAP.QueueJobAt( DateTime.Now.AddSeconds( 10 ),
                            new DelegateTryAgainToAddMail( TryAgainToAddMail ), folderDescriptor, entryID, longBody );
                    }
                }
                if ( message == null )
                {
                    return;
                }
                using ( message )
                {
                    Core.ResourceAP.QueueJob( new MailDescriptor( folderDescriptor, entryID, message, longBody ) );
                }
            }
        }

        private bool _refresh = false;
        public bool Refresh { get { return _refresh; } set { _refresh = value; } }


        private DateTime GetRestrictDate( IResource resFolder )
        {
            if ( Refresh )
            {
                return _indexStartDate;
            }
            if ( resFolder != null )
            {
                bool seeAll = Folder.GetSeeAll( resFolder );
                if ( seeAll )
                {
                    return DateTime.MinValue;
                }
                if ( !seeAll && _indexStartDate == DateTime.MinValue )
                {
                    Folder.SetSeeAllAsync( resFolder );
                }
            }
            return _indexStartDate;
        }
    }
}
