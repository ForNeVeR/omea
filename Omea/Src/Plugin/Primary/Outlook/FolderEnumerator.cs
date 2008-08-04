/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Threading;
using EMAPILib;
using JetBrains.DataStructures;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    internal interface IFolderDescriptorEnumeratorEvent
    {
        //Do not forget release COM reference for MAPI.Folders
        bool FolderFetched( FolderDescriptor parent, FolderDescriptor folder, out FolderDescriptor folderTag );
    }

    internal class FolderDescriptorEnumerator
    {
        private IEMsgStore _msgStore;
        private string _storeID;
        private string _storeName;
        private IFolderDescriptorEnumeratorEvent _enumListener = null;

        public static void Do( IEMsgStore msgStore, string storeID, string storeName, IFolderDescriptorEnumeratorEvent enumListener )
        {
            FolderDescriptorEnumerator enumerator =
                new FolderDescriptorEnumerator( msgStore, storeID, storeName, enumListener );
            try
            {
                enumerator.Enumerate();
            }
            catch ( Exception exception )
            {
                if( !(exception is ThreadAbortException) )
                {
                    Core.ReportException( exception, false );
                }
            }
        }
        public FolderDescriptorEnumerator( IEMsgStore msgStore, string storeID, string storeName, IFolderDescriptorEnumeratorEvent enumListener )
        {
            Guard.NullArgument( msgStore, "msgStore" );
            Guard.NullArgument( enumListener, "enumListener" );
            _msgStore = msgStore;
            _storeID = storeID;
            _storeName = storeName;
            _enumListener = enumListener;
        }
        public void Enumerate()
        {
            IEFolder folder = _msgStore.GetRootFolder();
            if ( folder != null )
            {
                using ( folder )
                {
                    FolderDescriptor tag = null;
                    FolderDescriptor folderDescriptor = FolderDescriptor.Get( _storeID, folder );
                    folderDescriptor.Name = _storeName;
                    bool continueEnumerate = _enumListener.FolderFetched( null, folderDescriptor, out tag );
                    if ( continueEnumerate )
                    {
                        OutlookSession.ProcessJobs();
                        EnumerateInternal( folder, tag );
                    }
                }
            }
        }

        private void EnumerateInternal( IEFolder folder, FolderDescriptor parentTag )
        {
            IEFolders folders = OutlookSession.GetFolders( folder );
            if ( folders == null )
            {
                return;
            }
            using ( folders )
            {
                for ( int i = 0; i < folders.GetCount(); ++i )
                {
                    OutlookSession.ProcessJobs();
                    IEFolder subFolder = OutlookSession.OpenFolder( folders, i );
                    if ( subFolder == null )
                    {
                        continue;
                    }
                    using ( subFolder )
                    {
                        FolderDescriptor tag = null;
                        FolderDescriptor folderDescriptor = FolderDescriptor.Get( _storeID, subFolder );
                        bool continueEnumerate = _enumListener.FolderFetched( parentTag, folderDescriptor, out tag );
                        if ( continueEnumerate )
                        {
                            EnumerateInternal( subFolder, tag );
                        }
                    }
                }
            }
        }
    }

    internal class ProcessedFolders
    {
        private static HashSet _processedFolders = new HashSet();
        private static int _objectCount = 0;
        private static bool _started = false;
        public static void RegisterFolder( string entryID )
        {
            lock ( _processedFolders )
            {
                _processedFolders.Add( entryID );
            }
        }
        public static void AddRef()
        {
            lock ( _processedFolders )
            {
                _started = true;
                _objectCount++;
            }
        }
        public static void DecRef()
        {
            lock ( _processedFolders )
            {
                _objectCount--;
                if ( _objectCount == 0 )
                {
                    _processedFolders.Clear();
                }
            }
        }
        public static bool IsFolderProcessed( string folderID )
        {
            if ( folderID == null )
            {
                return true;
            }
            lock ( _processedFolders )
            {
                if ( _objectCount == 0 && _started )
                {
                    return true;
                }
                return _processedFolders.Contains( folderID );
            }
        }
    }

    internal class MailSyncBackground : ReenteringJob, IFolderDescriptorEnumeratorEvent
    {
        private DateTime _dateRestrict;
        public MailSyncBackground( DateTime dateRestrict )
        {
            ProcessedFolders.AddRef();
            _dateRestrict = dateRestrict;
        }
        protected override void Execute()
        {
            if ( OutlookSession.OutlookProcessor.ShuttingDown )
            {
                return;
            }

            IStatusWriter statusWriter = Core.UIManager.GetStatusWriter( this, StatusPane.Network );
            statusWriter.ShowStatus( "Performing background mail synchronization..." );
            Tracer._Trace( "MailSyncBackground is executed" );
            foreach ( IEMsgStore msgStore in OutlookSession.GetMsgStores() )
            {
                if ( OutlookSession.OutlookProcessor.ShuttingDown )
                {
                    break;
                }

                OutlookSession.ProcessJobs();

                if ( msgStore == null )
                {
                    continue;
                }
                if ( OutlookProcessor.IsIgnoredInfoStore( msgStore ) )
                {
                    continue;
                }

                string storeID = msgStore.GetBinProp( MAPIConst.PR_STORE_ENTRYID );
                string name = msgStore.GetStringProp( MAPIConst.PR_DISPLAY_NAME );
                FolderDescriptorEnumerator.Do( msgStore, storeID, name, this );
            }
            statusWriter.ClearStatus();
            ProcessedFolders.DecRef();
        }

        #region IFolderDescriptorEnumeratorEvent Members

        public bool FolderFetched( FolderDescriptor parent, FolderDescriptor folder, out FolderDescriptor folderTag )
        {
            folderTag = null;
            if ( OutlookSession.OutlookProcessor.ShuttingDown )
            {
                return false;
            }
            if ( !Folder.IsIgnored( folder ) )
            {
                ProcessedFolders.RegisterFolder( folder.FolderIDs.EntryId );
                RefreshFolderDescriptor.Do( JobPriority.BelowNormal, folder.FolderIDs, _dateRestrict );
            }
            return true;
        }

        #endregion

        public override string Name
        {
            get { return "Mail sync in background"; }
        }
    }

}