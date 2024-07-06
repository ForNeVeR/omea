// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Runtime.InteropServices;
using EMAPILib;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Diagnostics;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class FolderDescriptor
    {
        private PairIDs _folderIDs;
        private string _name;
        private string _containerClass = string.Empty;
        private int _storeSupportMask = 0;
        private int _contentCount = 0;

        public FolderDescriptor( IResource folder )
        {
            Init( PairIDs.Get( folder ),
                folder.GetPropText( Core.Props.Name ),
                Folder.GetContainerClass( folder ), Folder.GetStoreSupportMask( folder ), Folder.GetConentCount( folder ) );
        }
        public FolderDescriptor( PairIDs IDs, IEFolder folder )
        {
            string name = folder.GetStringProp( MAPIConst.PR_DISPLAY_NAME );
            string containerClass = folder.GetStringProp( MAPIConst.PR_CONTAINER_CLASS );
            int storeSupportMask = folder.GetLongProp( MAPIConst.PR_STORE_SUPPORT_MASK );
            int contentCount = folder.GetLongProp( MAPIConst.PR_CONTENT_COUNT );
            Init( IDs, name, containerClass, storeSupportMask, contentCount );
        }
        private void Init( PairIDs IDs, string name, string containerClass, int storeSupportMask, int contentCount )
        {
            _folderIDs = IDs;
            _name = name;
            if ( containerClass != null )
            {
                _containerClass = containerClass;
            }
            _storeSupportMask = storeSupportMask;
            _contentCount = contentCount;
        }

        public static FolderDescriptor Get( IEFolder folder )
        {
            Guard.NullArgument( folder, "folder" );
            string entryId = OutlookSession.GetFolderID( folder );
            string storeId = folder.GetBinProp( MAPIConst.PR_STORE_ENTRYID );
            return Get( new PairIDs( entryId, storeId ), folder );
        }

        public static FolderDescriptor Get( PairIDs IDs )
        {
            Guard.NullArgument( IDs, "IDs" );
            IEFolder folder =
                OutlookSession.OpenFolder( IDs.EntryId, IDs.StoreId );
            if ( folder != null ) using ( folder )
                                  {
                                      return Get( IDs, folder );
                                  }
            return null;
        }
        public static FolderDescriptor Get( string entryID, string storeID )
        {
            Guard.EmptyStringArgument( entryID, "entryID" );
            Guard.EmptyStringArgument( storeID, "storeID" );
            return Get( new PairIDs( entryID, storeID ) );
        }
        public static FolderDescriptor Get( PairIDs IDs, IEFolder folder )
        {
            Guard.NullArgument( IDs, "IDs" );
            Guard.NullArgument( folder, "folder" );
            return new FolderDescriptor( IDs, folder );
        }
        public static FolderDescriptor Get( string entryID, string storeID, IEFolder folder )
        {
            Guard.EmptyStringArgument( entryID, "entryID" );
            Guard.EmptyStringArgument( storeID, "storeID" );
            Guard.NullArgument( folder, "folder" );
            return Get( new PairIDs( entryID, storeID ), folder );
        }
        public static FolderDescriptor Get( string storeID, IEFolder folder )
        {
            Guard.EmptyStringArgument( storeID, "storeID" );
            Guard.NullArgument( folder, "folder" );
            string entryID = OutlookSession.GetFolderID( folder );
            return Get( entryID, storeID, folder );
        }

        public PairIDs FolderIDs{ get { return _folderIDs; } }
        public string Name{ get { return _name; } set { _name = value; } }
        public string ContainerClass{ get { return _containerClass; } }
        public int StoreSupportMask{ get { return _storeSupportMask; } }
        public int ContentCount{ get { return _contentCount; } }
    }

    internal class MAPIInfoStoreDescriptor : AbstractNamedJob
    {
        private string _name;
        private string _entryId;
        private string _deletedItemsId;
        private string _junkEmailId;
        private int _supportMask;
        private bool _supported;
        private bool _storeTypeChecked = false;
        IResource _resource;
        private ArrayList _defaultFolderEntryIDs = new ArrayList();

        public MAPIInfoStoreDescriptor( IEMsgStore msgStore )
        {
            _name = msgStore.GetStringProp( MAPIConst.PR_DISPLAY_NAME );
            _entryId = msgStore.GetBinProp( MAPIConst.PR_ENTRYID );
            _deletedItemsId = msgStore.GetBinProp( MAPIConst.PR_IPM_WASTEBASKET_ENTRYID );

            MAPIIDs mapiIds = msgStore.GetInboxIDs();
            if ( mapiIds != null )
            {
                AddDefaultFolderEntryID( mapiIds.EntryID );
            }
            AddDefaultFolderEntryID( _deletedItemsId );
            AddDefaultFolderEntryID( msgStore.GetBinProp( MAPIConst.PR_IPM_SENTMAIL_ENTRYID ) );
            AddDefaultFolderEntryID( msgStore.GetBinProp( MAPIConst.PR_IPM_OUTBOX_ENTRYID ) );

            IEFolder rootFolder = msgStore.GetRootFolder();
            if ( rootFolder != null )
            {
                using ( rootFolder )
                {
                    string parentEntryId = rootFolder.GetBinProp( MAPIConst.PR_PARENT_ENTRYID );
                    if ( parentEntryId != null )
                    {
                        IEFolder parentFolder = msgStore.OpenFolder( parentEntryId );
                        if ( parentFolder != null )
                        {
                            using ( parentFolder )
                            {
                                AddDefaultFolderEntryID( parentFolder.GetBinProp( MAPIConst.PR_IPM_APPOINTMENT_ENTRYID ) );
                                AddDefaultFolderEntryID( parentFolder.GetBinProp( MAPIConst.PR_IPM_CONTACT_ENTRYID ) );
                                AddDefaultFolderEntryID( parentFolder.GetBinProp( MAPIConst.PR_IPM_DRAFTS_ENTRYID ) );
                                AddDefaultFolderEntryID( parentFolder.GetBinProp( MAPIConst.PR_IPM_JOURNAL_ENTRYID ) );
                                AddDefaultFolderEntryID( parentFolder.GetBinProp( MAPIConst.PR_IPM_NOTE_ENTRYID ) );
                                AddDefaultFolderEntryID( parentFolder.GetBinProp( MAPIConst.PR_IPM_TASK_ENTRYID ) );
                                ArrayList additionalRenEntryIDs = parentFolder.GetBinArray( MAPIConst.PR_ADDITIONAL_REN_ENTRYIDS );
                                if ( additionalRenEntryIDs != null && additionalRenEntryIDs.Count > 4 )
                                {
                                    _junkEmailId = additionalRenEntryIDs[4] as string;
                                }
                                if ( additionalRenEntryIDs != null )
                                {
                                    foreach ( string id in additionalRenEntryIDs )
                                    {
                                        AddDefaultFolderEntryID( id );
                                    }
                                }
                            }

                        }
                    }
                }
            }
            _supportMask = msgStore.GetLongProp( MAPIConst.PR_STORE_SUPPORT_MASK );

            _resource = Core.ResourceStore.FindUniqueResource( STR.MAPIInfoStore, PROP.EntryID, _entryId );
            _storeTypeChecked = _resource == null?false:_resource.HasProp( PROP.StoreTypeChecked );
            if ( _resource == null || !_storeTypeChecked )
            {
                try
                {
                    _supported = OutlookSession.IsStorageSupported( msgStore );
                    _storeTypeChecked = true;
                }
                catch ( COMException exception )
                {
                    Tracer._TraceException( exception );
                }
            }
            else
            {
                _supported = _resource.HasProp( PROP.StoreSupported );
            }
        }

        private void AddDefaultFolderEntryID ( string id )
        {
            if ( !String.IsNullOrEmpty( id ) )
            {
                _defaultFolderEntryIDs.Add( id );
            }
        }

        protected override void Execute()
        {
            if ( _resource != null && _resource.IsDeleted )
            {
                return;
            }
            if ( _deletedItemsId == null && _entryId == null )
            {
                return;
            }

            if ( _resource == null && _deletedItemsId != null )
            {
                _resource = Core.ResourceStore.FindUniqueResource( STR.MAPIInfoStore,
                    PROP.DeletedItemsEntryID, _deletedItemsId );
            }

            if ( _resource == null )
            {
                _resource = Core.ResourceStore.BeginNewResource( STR.MAPIInfoStore );
            }
            else
            {
                _resource.BeginUpdate();
            }

            IStringList propList = _resource.GetStringListProp( PROP.DefaultFolderEntryIDs );
            propList.Clear();
            foreach ( string entryId in _defaultFolderEntryIDs )
            {
                propList.Add( entryId );
            }
            _resource.SetProp( PROP.EntryID, _entryId );
            _resource.SetProp( PROP.DeletedItemsEntryID, _deletedItemsId );
            _resource.SetProp( PROP.JunkEmailEntryID, _junkEmailId );

            _resource.SetProp( PROP.PR_STORE_SUPPORT_MASK, _supportMask );
            _resource.SetProp( PROP.StoreSupported, _supported );
            if ( !_supported )
            {
                _resource.SetProp( PROP.IgnoredFolder, 1 );
                _name += " (Not supported)";
            }
            else if ( ( _supportMask & STORE_SUPPORT_MASK.STORE_PUBLIC_FOLDERS ) != 0 )
            {
                _resource.SetProp( PROP.IgnoredFolder, 1 );
            }
            _resource.SetProp( Core.Props.Name, _name );
            _resource.SetProp( PROP.StoreTypeChecked, _storeTypeChecked );
            _resource.EndUpdate();
        }

        public override string Name
        {
            get { return "Exporting info store: " + _name; }
        }
    }

    internal class RefreshFolderDescriptor : ReenteringJob
    {
        private FolderDescriptor _folder;
        private DateTime _dateRestriction;

        private RefreshFolderDescriptor( FolderDescriptor folderDescriptor, DateTime dateRestriction )
        {
            _folder = folderDescriptor;
            _dateRestriction = dateRestriction;
        }

        public static void Do( JobPriority jobPriority, FolderDescriptor folderDescriptor, DateTime dateRestriction )
        {
            IResource folder;
            if ( IsDataCorrect( out folder, folderDescriptor ) )
            {
                OutlookSession.OutlookProcessor.QueueJob( jobPriority, new RefreshFolderDescriptor( folderDescriptor, dateRestriction ) );
            }
        }
        public static void Do( JobPriority jobPriority, PairIDs folderIDs, DateTime dateRestriction )
        {
            Do( jobPriority, FolderDescriptor.Get( folderIDs.EntryId, folderIDs.StoreId ), dateRestriction );
        }

        private static bool IsDataCorrect( out IResource resFolder, FolderDescriptor folderDescriptor )
        {
            resFolder = null;
            if ( OutlookSession.OutlookProcessor.ShuttingDown ) return false;

            if ( folderDescriptor == null ) return false;
            resFolder = Folder.Find( folderDescriptor.FolderIDs.EntryId );
            if ( resFolder == null ) return false;
            if ( Folder.IsIgnored( folderDescriptor ) ) return false;
            return true;
        }
        protected override void Execute()
        {
            IResource resFolder;
            if ( !IsDataCorrect( out resFolder, _folder ) )
            {
                return;
            }

            IStatusWriter statusWriter = Core.UIManager.GetStatusWriter( this, StatusPane.Network );
            statusWriter.ShowStatus( "Synchronizing folder " + _folder.Name + "..." );

            OutlookSession.ProcessJobs();
            try
            {
                MailSync mailSync = new MailSync( false, _dateRestriction );
                mailSync.AddMailResources( Folder.GetMessageList( resFolder ) );
                mailSync.EnumerateMessageItems( _folder );
                mailSync.RemoveDeletedMailsFromIndex();
            }
            finally
            {
                statusWriter.ClearStatus();
            }
        }

        public override string Name
        {
            get
            {
                return "Refresh for folder";
            }
        }
    }

    internal class FolderIgnoringChanged : AbstractNamedJob
    {
        private IResource _folder;

        public FolderIgnoringChanged( IResource folder )
        {
            _folder = folder;
        }

        protected override void Execute()
        {
            try
            {
                if ( !Guard.IsResourceLive( _folder ) )
                {
                    return;
                }

                Folder.SetSeeAll( _folder, false );
                if ( Folder.IsIgnored( _folder ) )
                {
                    foreach ( IResource mail in Folder.GetMailList( _folder ) )
                    {
                        Mail.ForceDelete( mail );
                    }
                }
                else
                {
                    RefreshFolderDescriptor.Do( JobPriority.Normal, new FolderDescriptor( _folder ), Settings.IndexStartDate );
                }
            }
            catch ( Exception exception )
            {
                Core.ReportException( exception, ExceptionReportFlags.AttachLog );
                Tracer._TraceException( exception );
            }
        }

        public override string Name
        {
            get { return "Folder ignoring changed for folder: " + _folder.DisplayName; }
        }
    }
}
