/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.DataStructures;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    public class OutlookProcessor : AsyncProcessor
    {
        private Exception _lastException;
        private ChangeWatchers _changeWatchers = new ChangeWatchers();
        private bool _scheduleDeliver = true;
        private static int _syncVersion;
        private IUIManager _UIManager = null;
        public const int CURRENT_VERSION = 11;
        private static bool _inited = false;
        private bool _aborted = false;

        public OutlookProcessor() : base( null, false )
        {
            _tracer = new Tracer( "OutlookProcessor" );
            try
            {
                OutlookSession.Init( this );
                OnSettingsChanged( null, null );
                IdlePeriod = Settings.IdlePeriod * 60000;
                ThreadName = "Outlook AsyncProcessor";
                ThreadPriority = ThreadPriority.Normal;
                ThreadStarted += new EventHandler( _outlookProcessor_ThreadStarted );
                ThreadFinished += new EventHandler( _outlookProcessor_ThreadFinished );
                ProcessMessages = true;
                ExceptionHandler = new AsyncExceptionHandler( HandleException );
                _UIManager = Core.UIManager;
                _UIManager.RegisterIndicatorLight( "Outlook", this, 10,
                    OutlookPlugin.LoadIconFromAssembly( "OutlookPlugin.Icons.outlook_idle.ico" ),
                    OutlookPlugin.LoadIconFromAssembly( "OutlookPlugin.Icons.outlook_busy.ico" ),
                    OutlookPlugin.LoadIconFromAssembly( "OutlookPlugin.Icons.outlook_stuck.ico" ) );
                _UIManager.MainWindowClosing += new System.ComponentModel.CancelEventHandler( _UIManager_MainWindowClosing );
                StartThread();
                _inited = true;
            }
            catch ( Exception exception )
            {
                SetLastException( exception );
            }
        }

        public static bool Inited { get { return _inited; } }

        public void AbortThread()
        {
            if ( _aborted )
            {
                return;
            }
            _finished = _aborted = true;
            this.Thread.Abort();
            MessageBox.Show( "Outlook plugin has encountered fatal error. " + Core.ProductFullName + " will shutdown.", 
                "Error" , MessageBoxButtons.OK, MessageBoxIcon.Error );
            Core.UIManager.CloseMainWindow();
        }

        public static void CheckState()
        {
            if ( !OutlookProcessor.Inited )
            {
                throw new ApplicationException( "Outlook processor is not executed yet" );
            }
        }

        public override void DoJobs()
        {
            if ( !_aborted )
            {
                base.DoJobs();
            }
            else
            {
                _finished = true;
            }
        }

        public void ProcessJobs()
        {
            if ( Core.State == CoreState.Running )
            {
                if ( OutstandingJobs > 0 )
                {
                    DoJobs();
                }
                else
                {
                    Application.DoEvents();
                }
            }
        }

        public bool ShuttingDown { get { return ( Core.State == CoreState.ShuttingDown ) || Finished; } }
        public bool IsStarted { get { return ( _lastException == null ); } }
        public Exception LastException { get { return _lastException; } }

        public static int SyncVersion
        {
            get
            {
                LoadSyncVersion();
                return _syncVersion;
            }
        }

        public bool ScheduleDeliver { get { return _scheduleDeliver; } }
        private void OnSettingsChanged( object obj, EventArgs args )
        {
            Settings.LoadSettings();
            if ( Core.State == CoreState.Running )
            {
                QueueJob( JobPriority.AboveNormal, "Synchronization for address books", 
                    new MethodInvoker( SynchronizeOutlookAddressBooksImpl ) );
                QueueJob( JobPriority.AboveNormal, "Synchronization for contacts", new MethodInvoker( SynchronizeContactsImpl ) );
            }
            _scheduleDeliver = Settings.ScheduleDeliver;
        }

        public void SynchronizeOutlookAddressBooks()
        {
            if ( Settings.SyncContacts )
            {
                if ( IsInitialStart() )
                {
                    QueueJob( "Synchronization of address books", new MethodInvoker( SynchronizeOutlookAddressBooksImpl ) );
                }
            }
        }
        public void RunSynchronizeFolderAndAddressBooks()
        {
            RunJob( new MethodInvoker( SynchronizeFolderStructureImpl ) );
            if ( Settings.SyncContacts )
            {
                if ( IsInitialStart() )
                {
                    RunJob( "Synchronization of address books", new MethodInvoker( SynchronizeOutlookAddressBooksImpl ) );
                }
            }
        }

        public void SynchronizeFolderStructure()
        {
            QueueJob( "Synchronization of folder structure", new MethodInvoker( SynchronizeFolderStructureImpl ) );
        }

        public void SynchronizeMAPIInfoStores()
        {
            MethodInvoker synchronizeMAPIInfoStoresMethod = new MethodInvoker( SynchronizeMAPIInfoStoresImpl );
            if ( IsInitialStart() )
            {
                RunJob( "Synchronization of MAPI info stores", synchronizeMAPIInfoStoresMethod );
            }
            else
            {
                QueueJob( JobPriority.Immediate, "Synchronization of MAPI info stores", synchronizeMAPIInfoStoresMethod );
            }
        }

        public void InitialIndexing()
        {
            if ( Settings.SyncContacts )
            {
                _tracer.Trace( "prepare SynchronizeContacts" );
                SynchronizeContacts();
            }
            _tracer.Trace( "prepare EnumerateMails" );
            EnumerateMails();
        }
        private void ExportTasksImpl()
        {
            IResourceList tasksToExport = Core.ResourceStore.GetAllResources( STR.Task );
            IResourceList tasksWithEntry = Core.ResourceStore.FindResourcesWithProp( null, PROP.EntryID );
            tasksToExport = tasksToExport.Minus( tasksWithEntry );
            foreach ( IResource task in tasksToExport.ValidResources )
            {
                if ( Finished )
                {
                    break;
                }
                new ExportTaskDescriptor( task ).QueueJob( JobPriority.AboveNormal );
            }
        }

        public void ExportTasks()
        {
            QueueJob( JobPriority.Immediate, new MethodInvoker( ExportTasksImpl ) );
        }

        private static void LoadSyncVersion()
        {
            if ( _syncVersion == 0 )
            {
                IResource resSyncVersion = GetSyncVersionResource();
                _syncVersion = resSyncVersion.GetIntProp( PROP.SyncVersion );
            }
        }

        private static IResource GetSyncVersionResource()
        {
            IResourceList syncVersions = Core.ResourceStore.GetAllResources( STR.SyncVersion );
            if ( syncVersions.Count > 0 )
            {
                return syncVersions[ 0 ];
            }
            return Core.ResourceStore.NewResource( STR.SyncVersion );
        }
        public bool IsSyncComplete()
        {
            IResourceList syncVersions = Core.ResourceStore.GetAllResources( STR.SyncVersion );
            if ( syncVersions.Count == 0 )
            {
                return false;
            }
            return syncVersions[ 0 ].HasProp( PROP.SyncComplete );
        }
        public void SetSyncComplete()
        {
            IResource resource = GetSyncVersionResource();
            ResourceProxy proxy = new ResourceProxy( resource );
            proxy.SetPropAsync( PROP.SyncComplete, true );
        }
        internal static void SetSyncVersion( int syncVersion )
        {
            _syncVersion = syncVersion;
            Tracer._Trace( "OutlookProcessor: Forced syncVersion was set = " + _syncVersion );
        }

        private void SetSyncVersion()
        {
            _syncVersion = CURRENT_VERSION;
            IResource resource = GetSyncVersionResource();
            resource.SetProp( PROP.SyncVersion, _syncVersion );
            _tracer.Trace( "syncVersion was set = " + _syncVersion );
        }

        internal void StopMailIndexing()
        {
            SetSyncVersion();
            if ( Settings.IdleIndexing )
            {
                QueueIdleJob( new MailSyncDescriptor( false, DateTime.MinValue, true ) );
            }
            _changeWatchers.Watch();
            Core.UIManager.AddOptionsChangesListener( "MS Outlook", STR.OUTLOOK_GENERAL, new EventHandler( OnSettingsChanged ) );
            Core.UIManager.AddOptionsChangesListener( "MS Outlook", STR.OUTLOOK_FOLDERS, new EventHandler( OnSettingsChanged ) );
            Core.UIManager.AddOptionsChangesListener( "MS Outlook", STR.OUTLOOK_ADDRESS_BOOKS, new EventHandler( OnSettingsChanged ) );

            new PostManRepeatable();
        }

        public void SynchronizeContacts()
        {
            if ( IsInitialStart())
            {
                QueueJob( "Synchronization for contacts", new MethodInvoker( SynchronizeContactsImpl ) );
            }
        }

        public void EnumerateMails()
        {
            QueueJob( "Start for mail enumerating", new MethodInvoker( EnumerateMailsImpl ) );
        }

        private void SynchronizeMAPIInfoStoresImpl()
        {
            bool isInitialStart = IsInitialStart();

            HashMap infoStores = new HashMap();
            foreach ( IResource infoStore in Core.ResourceStore.GetAllResources( STR.MAPIInfoStore ).ValidResources )
            {
                string storeId = infoStore.GetStringProp( PROP.EntryID );
                if ( storeId != null )
                {
                    if ( !OutlookSession.WereProblemWithOpeningStorage( storeId ) )
                    {
                        infoStores.Add( storeId, infoStore );
                    }
                }
                else
                {
                    new ResourceProxy( infoStore ).DeleteAsync();
                }
            }

            foreach ( IEMsgStore msgStore in OutlookSession.GetMsgStores() )
            {
                if ( msgStore == null )
                {
                    continue;
                }
                string storeID = msgStore.GetBinProp( MAPIConst.PR_ENTRYID );
                infoStores.Remove( storeID );
                MAPIInfoStoreDescriptor infoStoreJob = new MAPIInfoStoreDescriptor( msgStore );
                if( isInitialStart ) 
                {
                    Core.ResourceAP.RunJob( infoStoreJob );
                }
                else
                {
                    Core.ResourceAP.QueueJob( JobPriority.Immediate, infoStoreJob );
                }
            }
            foreach ( HashMap.Entry entry in infoStores )
            {
                IResource infoStore = (IResource)entry.Value;
                new ResourceProxy( infoStore ).Delete();
            }
        }

        internal static bool IsIgnoredInfoStore( IEMsgStore msgStore )
        {
            Trace.WriteLine( "IsIgnoredInfoStore -- checking infostore: " );
            string entryId = msgStore.GetBinProp( MAPIConst.PR_ENTRYID );
            if ( entryId != null )
            {
                IResource infoStore = Core.ResourceStore.FindUniqueResource( STR.MAPIInfoStore, PROP.EntryID, entryId );

                if ( infoStore != null )
                {
                    if( infoStore.HasProp( Core.Props.Name ))
                        Trace.WriteLine( "      IsIgnoredInfoStore -- with name [" + infoStore.GetStringProp( Core.Props.Name ) + "]" );

                    int prop = infoStore.GetIntProp( PROP.IgnoredFolder );
                    if( prop != 0 )
                        Trace.WriteLine( "      IsIgnoredInfoStore -- FLAG IgnoredFolder is set!" );

                    return infoStore.GetIntProp( PROP.IgnoredFolder ) != 0;
                }
                else
                {
                    Trace.WriteLine( "      IsIgnoredInfoStore -- No InfoStore resource found." );
                }
            }

            Trace.WriteLine( "IsIgnoredInfoStore -- It has no PR_ENTRYID, trying DeletedItemsEntryID" );
            string deletedItemsEntryId = msgStore.GetBinProp( MAPIConst.PR_IPM_WASTEBASKET_ENTRYID );
            if ( deletedItemsEntryId != null )
            {
                IResource infoStore = Core.ResourceStore.FindUniqueResource( STR.MAPIInfoStore,
                                                                             PROP.DeletedItemsEntryID, deletedItemsEntryId );
                if ( infoStore != null )
                {
                    if( infoStore.HasProp( Core.Props.Name ))
                        Trace.WriteLine( "IsIgnoredInfoStore -- with name [" + infoStore.GetStringProp( Core.Props.Name ) + "]" );

                    int prop = infoStore.GetIntProp( PROP.IgnoredFolder );
                    if( prop != 0 )
                        Trace.WriteLine( "IsIgnoredInfoStore -- FLAG IgnoredFolder is set!" );

                    return infoStore.GetIntProp( PROP.IgnoredFolder ) != 0;
                }
                else
                {
                    Trace.WriteLine( "      IsIgnoredInfoStore -- No InfoStore resource found." );
                }
            }
            Trace.WriteLine( "IsIgnoredInfoStore -- It has no PR_IPM_WASTEBASKET_ENTRYID" );

            return false;
        }

        private void SynchronizeFolderRecursive( IEFolder folder, string name, string storeID, FolderDescriptor parentDescriptor )
        {
            if ( folder == null )
            {
                return;
            }
            using ( folder )
            {
                FolderDescriptor folderDescriptor = FolderDescriptor.Get( folder );
                if ( name != null )
                {
                    folderDescriptor.Name = name;
                    _tracer.Trace( "Folder name = " + name );
                }
                else
                {
                    _tracer.Trace( "Folder name is unknown" );
                }
                _folders.Remove( folderDescriptor.FolderIDs.EntryId );
                FolderStructureDescriptor folderStruct =
                    new FolderStructureDescriptor( parentDescriptor, folderDescriptor );
                Core.ResourceAP.QueueJob( folderStruct );
                IEFolders folders = OutlookSession.GetFolders( folder, folderDescriptor );
                if ( folders == null )
                {
                    return;
                }
                using ( folders )
                {
                    for ( int i = 0; i < folders.GetCount(); ++i )
                    {
                        try
                        {
                            SynchronizeFolderRecursive( folders.OpenFolder( i ), null, storeID, folderDescriptor );
                        }
                        catch ( COMException exception )
                        {
                            _tracer.TraceException( exception );
                            OutlookSession.ProblemWithOpeningFolder( folderDescriptor.FolderIDs.EntryId );
                            break;
                        }
                    }
                }
            }
        }
        private HashSet _folders = new HashSet();
        private void SynchronizeFolderStructureImpl()
        {
            _tracer.Trace( "Start SynchronizeFolderStructureImpl" );
            IResourceList folders = Core.ResourceStore.GetAllResources( STR.MAPIFolder );
            foreach ( IResource folder in folders.ValidResources )
            {
                string entryID = folder.GetStringProp( PROP.EntryID );
                if ( entryID != null )
                {
                    _folders.Add( entryID );
                }
                else
                {
                    new ResourceProxy( folder ).DeleteAsync();
                }
            }
            _tracer.Trace( "Start enumeration for info stores" );

            foreach ( IEMsgStore msgStore in OutlookSession.GetMsgStores() )
            {
                if ( msgStore == null )
                {
                    continue;
                }
                _tracer.Trace( "GetRootFolder" );

                IEFolder rootFolder = msgStore.GetRootFolder();
                string storeID = msgStore.GetBinProp( MAPIConst.PR_STORE_ENTRYID );
                if ( IsIgnoredInfoStore( msgStore ) )
                {
                    _tracer.Trace( "MsgStore is ignored" );
                    if ( rootFolder == null )
                    {
                        continue;
                    }
                    using ( rootFolder )
                    {
                        string entryID = OutlookSession.GetFolderID( rootFolder );
                        IResource rootMAPIFolder = Folder.Find( entryID );
                        if ( rootMAPIFolder != null )
                        {
                            _tracer.Trace( "Delete folder recursive for " + rootMAPIFolder.DisplayName );
                            Core.ResourceAP.QueueJob( "Delete folder recursive", new ResourceDelegate( Folder.DeleteFolderRecursive ),
                                rootMAPIFolder );
                        }
                    }
                }
                else
                {
                    string name = msgStore.GetStringProp( MAPIConst.PR_DISPLAY_NAME );
                    _tracer.Trace( "MsgStore name = " + name );
                    SynchronizeFolderRecursive( rootFolder, name, storeID, null );
                }
            }

            _tracer.Trace( "Delete folders recursively if necessary" );

            foreach ( HashSet.Entry entry in _folders )
            {
                IResource folderToDelete =
                    Core.ResourceStore.FindUniqueResource( STR.MAPIFolder, PROP.EntryID, (string)entry.Key );
                if ( folderToDelete != null )
                {
                    PairIDs folderIDs = PairIDs.Get( folderToDelete );
                    if ( folderIDs != null )
                    {
                        if ( OutlookSession.WereProblemWithOpeningStorage( folderIDs.StoreId ) )
                        {
                            continue;
                        }
                        if ( OutlookSession.WereProblemWithOpeningFolder( folderIDs.EntryId ) )
                        {
                            continue;
                        }
                    }
                    Core.ResourceAP.QueueJob( new ResourceDelegate( Folder.DeleteFolderRecursive ),
                        folderToDelete );
                }
            }
            _folders.Clear();
            _tracer.Trace( "Finish SynchronizeFolderStructureImpl" );
        }

        private void SynchronizeOutlookAddressBooksImpl()
        {
            _tracer.Trace( "Start SynchronizeOutlookAddressBooksImpl" );

            IEAddrBook addrBook = OutlookSession.GetAddrBook();
            if ( addrBook != null )
            {
                int count = addrBook.GetCount();
                for ( int i = 0; i < count; i++ )
                {
                    if ( ShuttingDown )
                    {
                        return;
                    }
                    OutlookSession.ProcessJobs();
                    IEABContainer abCont = addrBook.OpenAB( i );
                    if ( abCont == null )
                    {
                        continue;
                    }
                    using ( abCont )
                    {
                        int displayType = abCont.GetLongProp( MAPIConst.PR_DISPLAY_TYPE );
                        if ( displayType != ABType.DT_GLOBAL )
                        {
                            continue;
                        }
                        string entryID = abCont.GetBinProp( MAPIConst.PR_ENTRYID );
                        Core.ResourceAP.RunJob( new OutlookABDescriptor( abCont.GetStringProp( MAPIConst.PR_DISPLAY_NAME ), entryID ) );
                    }
                }
            }
            _tracer.Trace( "Start SynchronizeOutlookAddressBooksImpl" );
        }

        private void SynchronizeContactsImpl()
        {
            _tracer.Trace( "Start SynchronizeContactsImpl" );

            Settings.UpdateProgress( 0, "Computing Address Books count...", "" );
            int totalABs = Folder.GetFolders( FolderType.Contact ).Count;
            int processedABs = 0;

            IEAddrBook addrBook = OutlookSession.GetAddrBook();
            if ( addrBook != null )
            {
                int count = addrBook.GetCount();
                totalABs += count;
                for ( int i = 0; i < count; ++i )
                {
                    if ( ShuttingDown )
                    {
                        return;
                    }
                    OutlookSession.ProcessJobs();
                    ++processedABs;
                    int percentage = ( processedABs * 100 ) / totalABs;
                    Settings.UpdateProgress( percentage, "Synchronizing Address Books", processedABs.ToString() );
                    IEABContainer abContainer = addrBook.OpenAB( i );
                    if ( abContainer == null )
                    {
                        continue;
                    }
                    using ( abContainer )
                    {
                        ProcessGlobalAddressBook( abContainer );
                    }
                }
            }
            ProcessContactFolders( processedABs, totalABs );
            _tracer.Trace( "Finish SynchronizeContactsImpl" );

            Settings.UpdateProgress( 100, "Synchronizing Address Books", totalABs.ToString() );
        }
        private void ProcessGlobalAddressBook( IEABContainer abContainer )
        {
            IERowSet rowSet = abContainer.GetRowSet();
            if ( rowSet == null )
            {
                return;
            }
            using ( rowSet )
            {
                string entryID = abContainer.GetBinProp( MAPIConst.PR_ENTRYID );
                IResource outlookAB = Core.ResourceStore.FindUniqueResource( STR.OutlookABDescriptor, PROP.EntryID, entryID );
                if ( outlookAB == null )
                {
                    return;
                }
                if ( !Folder.IsIgnoreImport( outlookAB ) )
                {
                    string curName = abContainer.GetStringProp( MAPIConst.PR_DISPLAY_NAME );
                    string abName = OutlookAddressBook.GetProposedName( curName, entryID );
                    OutlookAddressBook AB = new OutlookAddressBook( abName, entryID, false );
                    AB.RunAB();
                    int count = rowSet.GetRowCount();
                    for ( int i = 0; i < count; i++ )
                    {
                        if ( ShuttingDown )
                        {
                            return;
                        }
                        OutlookSession.ProcessJobs();

                        string ABentryID = rowSet.GetBinProp( 0, i /*MAPIConst.PR_ENTRYID*/ );
                        if ( ABentryID == null )
                        {
                            continue;
                        }
                        Core.ResourceAP.QueueJob( new ContactDescriptor( rowSet, i, ABentryID, AB ) );
                    }
                }
                else
                {
                    IResource AB = Core.ResourceStore.FindUniqueResource( STR.AddressBook, PROP.EntryID, entryID );
                    if ( AB != null )
                    {
                        new ResourceProxy( AB ).DeleteAsync();
                    }
                }
            }
        }
        private void ProcessContactFolder( PairIDs folderIDs, string abName )
        {
            IEFolder folder = null;
            try
            {
                folder = OutlookSession.OpenFolder( folderIDs.EntryId, folderIDs.StoreId );
            }
            catch( System.Threading.ThreadAbortException )
            {
            }
            catch ( Exception exception )
            {
                if ( exception is COMException &&
                    ( (COMException)exception ).ErrorCode != ( unchecked( (int)0x80040111 ) ) ) //ClassFactory cannot supply requested class
                {
                    return;
                }
                Core.ReportException( exception, ExceptionReportFlags.AttachLog );
                return;
            }
            if ( folder == null )
            {
                return;
            }
            using ( folder )
            {
                OutlookAddressBook AB = new OutlookAddressBook( abName, folderIDs, true );
                AB.RunAB();
                IEMessages messages = folder.GetMessages();
                if ( messages == null )
                {
                    return;
                }
                using ( messages )
                {
                    int mesCount = messages.GetCount();
                    for ( int i = 0; i < mesCount; i++ )
                    {
                        if( ShuttingDown )
                        {
                            break;
                        }
                        OutlookSession.ProcessJobs();
                        IEMessage message = messages.OpenMessage( i );
                        if ( message == null )
                        {
                            continue;
                        }
                        using ( message )
                        {
                            string mesEntryID = OutlookSession.GetMessageID( message );
                            Core.ResourceAP.QueueJob( new ContactDescriptor( message, mesEntryID, mesEntryID, AB ) );
                        }
                    }
                }
            }
        }
        private void ProcessContactFolders( int processedABs, int totalABs )
        {
            ArrayList toDeleteFolders = new ArrayList();
            foreach ( IResource contactFolder in Folder.GetFolders( FolderType.Contact ).ValidResources )
            {
                if( ShuttingDown )
                {
                    break;
                }
                OutlookSession.ProcessJobs();
                processedABs++;
                int percentage = ( processedABs * 100 ) / totalABs;
                Settings.UpdateProgress( percentage, "Indexing Address Books", processedABs.ToString() );

                PairIDs folderIDs = PairIDs.Get( contactFolder );
                if ( folderIDs == null )
                {
                    toDeleteFolders.Add( contactFolder );
                    continue;
                }
                string abName = OutlookAddressBook.GetProposedName( contactFolder.GetPropText( Core.Props.Name ), folderIDs.EntryId );
                Core.ResourceAP.QueueJob( new OutlookAddressBookReName( folderIDs.EntryId, abName ) );

                if ( !Folder.IsIgnoreImport( contactFolder ) )
                {
                    ProcessContactFolder( folderIDs, abName );
                }
                else
                {
                    IResource AB = Core.ResourceStore.FindUniqueResource( STR.AddressBook, PROP.EntryID, folderIDs.EntryId );
                    if ( AB != null )
                    {
                        new ResourceProxy( AB ).DeleteAsync();
                    }
                }
            }
            foreach ( IResource contactFolder in toDeleteFolders ) // delete corrupted 'MAPIFolder' resources
            {
                new ResourceProxy( contactFolder ).DeleteAsync();
            }
        }
        private void OnCoreStateChanged( object sender, EventArgs e )
        {
            if ( Core.State == CoreState.Running )
            {
                QueueBackgroundWork();
                Core.StateChanged -= new EventHandler( OnCoreStateChanged );
                new PostManRepeatable();
            }
        }
        private void PrepareBackgroundWork()
        {
            if ( Core.State == CoreState.Running )
            {
                QueueBackgroundWork();
            }
            Core.StateChanged += new EventHandler( OnCoreStateChanged );
        }
        private void QueueBackgroundWork()
        {
            if ( Settings.UseBackgroundMailSync )
            {
                _tracer.Trace( "Heavy enumeration is started in background" );
                QueueJobAt( DateTime.Now.AddMinutes( 1 ), "Address Books Synchronization", new MethodInvoker( SynchronizeOutlookAddressBooksImpl ) );
                QueueJobAt( DateTime.Now.AddMinutes( 1.5 ), "Contacts Synchronization", new MethodInvoker( SynchronizeContactsImpl ) );
                QueueJobAt( DateTime.Now.AddMinutes( 2 ), new MailSyncBackground( Settings.IndexStartDate ) );
            }
        }

        private void UpdateProgress( int processed, int total )
        {
            if ( Core.ProgressWindow == null )
            {
                return;
            }

            int percentage = ( total == 0 )
                ? 100
                : processed * 100 / total;
            string statusText = "Updating attachments...";
            Settings.UpdateProgress( percentage, statusText, string.Empty );
        }

        private void UpdateAttachments()
        {
            _tracer.Trace( "prepare UpdateAttachments" );

            IResourceList attachments = Core.ResourceStore.FindResourcesWithProp( null, PROP.AttachmentIndex );
            int count = attachments.Count;
            int processed = 0;
            UpdateProgress( processed, count );
            int ticks = System.Environment.TickCount;
            foreach ( IResource attach in attachments.ValidResources )
            {
                try
                {
                    IEAttach attachment = new OutlookAttachment( attach ).OpenAttach();
                    if ( attachment == null )
                    {
                        continue;
                    }
                    using ( attachment )
                    {
                        string contentID = attachment.GetStringProp( MAPIConst.PR_ATTACH_CONTENT_ID );
                        int attachNum = attachment.GetLongProp( MAPIConst.PR_ATTACH_NUM );
                        int attachMethod = attachment.GetLongProp( MAPIConst.PR_ATTACH_METHOD );
                        ResourceProxy resAttach = new ResourceProxy( attach );
                        resAttach.BeginUpdate();
                        resAttach.SetProp( CommonProps.ContentId, contentID );
                        resAttach.SetProp( PROP.PR_ATTACH_NUM, attachNum );
                        resAttach.SetProp( PROP.AttachMethod, attachMethod );
                        resAttach.EndUpdateAsync();
                    }
                }
                catch
                {}
                ++processed;
                if ( System.Environment.TickCount - ticks > 250 )
                {
                    ticks = System.Environment.TickCount;
                    UpdateProgress( processed, count );
                }
            }
        }

        private void UpdateAnnotation()
        {
            _tracer.Trace( "prepare UpdateAnnotation" );
            IResourceList list = Core.ResourceStore.FindResourcesWithProp( STR.Email, PROP.MessageFlag );
            foreach ( IResource resource in list.ValidResources )
            {
                ResourceProxy mail = new ResourceProxy( resource );
                mail.BeginUpdate();
                string messageFlag = resource.GetStringProp( PROP.MessageFlag );
                string annotation = resource.GetStringProp( Core.Props.Annotation );
                if ( annotation != messageFlag )
                {
                    if ( annotation == null || annotation.Length == 0 )
                    {
                        mail.SetProp( Core.Props.Annotation, messageFlag );
                    }
                    else
                    {
                        AnnotationMailChangeWatcher.ExportAnnotation( resource );
                    }
                }
                mail.DeleteProp( PROP.MessageFlag );
                mail.EndUpdate();
            }
        }

        private void EnumerateMailsImpl()
        {
            _tracer.Trace( "prepare EnumerateMailsImpl" );

            try
            {
                if ( SyncVersion < 10 )
                {
                    UpdateAttachments();
                }
                if ( SyncVersion < 9 && Settings.CreateAnnotationFromFollowup )
                {
                    UpdateAnnotation();
                }

                DateTime dtRestriction = Settings.IndexStartDate;
                if ( !IsInitialStart() )
                {
                    _tracer.Trace( "Light enumeration is started" );
                    if ( Settings.SyncMode == MailSyncMode.None )
                    {
                        PrepareBackgroundWork();
                    }
                    else if ( Settings.SyncMode == MailSyncMode.All )
                    {
                        new MailSyncDescriptor( true, dtRestriction, false ).NextMethod();
                    }
                    else
                    {
                        new FreshMailEnumerator().NextMethod();
                        PrepareBackgroundWork();
                    }
                }
                else
                {
                    ResourceProxy.BeginNewResource( STR.InitialEmailEnum ).EndUpdateAsync();
                    _tracer.Trace( "Heavy enumeration is started" );
                    new MailSyncDescriptor( true, dtRestriction, false ).NextMethod();
                    if ( dtRestriction == DateTime.MinValue )
                    {
                        SetSyncComplete();
                    }
                }
            }
            finally
            {
                _tracer.Trace( "fire FinishInitialIndexingJob" );
                Core.ResourceAP.QueueJob( new MethodInvoker( OutlookPlugin.FinishInitialIndexingJob ) );
            }
        }

        private void _outlookProcessor_ThreadStarted( object sender, EventArgs e )
        {
            try
            {
                OutlookSession.Initialize();
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
                SetLastException( exception );
            }
        }

        private void _outlookProcessor_ThreadFinished( object sender, EventArgs e )
        {
            Settings.LastExecutionTime.Save( DateTime.Now );
            OutlookSession.OutlookProcessor.ProcessJobs();
            try
            {
                OutlookSession.Uninitialize();
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
                SetLastException( exception );
            }
            if ( _aborted )
            {
                _finished = true;
            }
        }

        public bool HandleException( COMException comException )
        {
            _tracer.TraceException( comException );
            if ( comException.ErrorCode == ( unchecked( (int)0x80040600 ) ) )
            {
                StandartJobs.MessageBox( "MAPI reports internal error that your message storage is corrupted", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error );
                return true;
            }
            else if ( comException.ErrorCode == ( unchecked( (int)0x8004011C ) ) )
            {
                StandartJobs.MessageBox( "MAPI reports internal error that your message storage is not configured", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error );
                return true;
            }
            else if ( comException.ErrorCode == ( unchecked( (int)0x80040116 ) ) ) //MAPI_E_DISK_ERROR
            {
                StandartJobs.MessageBox( "MAPI reports internal error that there is hard disk error while reading from message store.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error );
                return true;
            }
            else if ( comException.ErrorCode == ( unchecked( (int)0x8004060C ) ) ) //
            {
                StandartJobs.MessageBox( "Outlook reports that your message store exceeded the available space.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error );
                return true;
            }
            return false;
        }
        private void HandleException( Exception exception )
        {
            if ( exception is System.Threading.ThreadAbortException )
            {
                return;
            }
            bool trapped = false;
            Exception mostInnerException = Utils.GetMostInnerException( exception );
            if ( mostInnerException is COMException )
            {
                trapped = HandleException( (COMException)mostInnerException );
            }
            if ( !trapped )
            {
                Core.ReportException( exception, ExceptionReportFlags.AttachLog );
            }
            SetLastException( exception );
        }
        private void SetLastException( Exception exception )
        {
            _lastException = exception;
        }

        public override void Dispose()
        {
            Core.UIManager.DeRegisterIndicatorLight( "Outlook" );
            base.Dispose();
        }

        private void _UIManager_MainWindowClosing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            if ( OutlookSession.LibManagerBase.FormCount() > 0 && !OutlookSession.EMAPISession.CanClose() )
            {
                DialogResult result =
                    MessageBox.Show( "You have open Outlook message windows.\nYou should save, send or close the messages before closing Omea,\notherwise the messages will be lost. Continue closing?",
                    "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2 );
                if ( result == DialogResult.No )
                {
                    e.Cancel = true;
                }
            }
        }

        private static bool IsInitialStart()
        {
            return Core.ResourceStore.GetAllResources( STR.InitialEmailEnum ).Count == 0;
        }
    }

    internal delegate MailBodyDescriptor PairIDsDelegate( PairIDs pairIDs );

    internal delegate void StringDelegate( string str );

    internal delegate MailBodyDescriptor MailBodyDescriptorDelegate( IResource resource );

    internal delegate void ActionContextDelegate( IActionContext context );

    internal delegate Stream Resource2StreamDelegate( IResource resource, int threadId );

    internal delegate void ResourceList_ResourceDelegate( IResourceList resourceList, IResource resource );

    internal delegate void Resource_StringDelegate( IResource resource, string str );

    internal delegate void CreateEmailDelegate( string subject, string body, EmailBodyFormat bodyFormat, IResourceList recipients,
    string[] attachments, bool useTemplatesInBody );
    internal delegate void CreateEmailWithRecipDelegate( string subject, string body, EmailBodyFormat bodyFormat, EmailRecipient[] recipients,
    string[] attachments, bool useTemplatesInBody );

    internal class OutlookThreadTimeoutException : Exception
    {
        internal OutlookThreadTimeoutException()
            : base( "Timeout when calling Outlook thread" )
        {}
    }

    internal class StandartJobs
    {
        static public void MessageBox( string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon )
        {
            Core.UIManager.QueueUIJob( new DelegateShowMessageBox( ShowMessageBox ), message, caption, buttons, icon );
        }

        delegate void DelegateShowMessageBox( string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon );

        static private void ShowMessageBox( string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon )
        {
            System.Windows.Forms.MessageBox.Show( message, caption, buttons, icon );
        }
    }

    internal class EmptyJob : AbstractNamedJob
    {
        protected override void Execute()
        {
            Tracer._Trace( "Empty job was executed." );
        }

        /// <summary>
        /// Gets or sets the name of the job.
        /// </summary>
        /// <remarks>The name of the last executing job is displayed in the tooltip for
        /// the async processor status indicator in the status bar.</remarks>
        public override string Name { get { return "Empty job to start async processor"; }
        }
    }
}