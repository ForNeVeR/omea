/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Diagnostics;
using EMAPILib;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class MailSyncDescriptor : AbstractNamedJob
    {
        private bool _computeCount;
        private DateTime _indexStartDate;
        private bool _idle;

        public MailSyncDescriptor( bool computeCount, DateTime indexStartDate, bool idle )
            : base()
        {
            Tracer._Trace( "MailSyncDescriptor starts" );
            _computeCount = computeCount;
            _indexStartDate = indexStartDate;
            _idle = idle;
        }
        #region INamedUnitOfWork Members

        public override string Name
        {
            get { return "MailSyncDescriptor"; } 
            set { }
        }

        #endregion

        protected override void Execute()
        {
            Tracer._Trace( "DoEnum()" );
            if ( Settings.IdleModeManager.CheckIdleAndSyncComplete() )
            {
                Tracer._Trace( "IsSyncComplete()" );
                return;
            }
            Settings.IdleModeManager.SetIdleFlag();
            MailSync mailSync = new MailSync( _computeCount, _indexStartDate, _idle );
            mailSync.PrepareMailResources();
            
            foreach ( IEMsgStore msgStore in OutlookSession.GetMsgStores() )
            {
                if ( msgStore == null ) continue;
                if ( OutlookProcessor.IsIgnoredInfoStore( msgStore ) )
                {
                    continue;
                }

                string storeID = msgStore.GetBinProp( MAPIConst.PR_STORE_ENTRYID );
                string name = msgStore.GetStringProp( MAPIConst.PR_DISPLAY_NAME );
                FolderDescriptorEnumerator.Do( msgStore, storeID, name, mailSync );
                if ( Settings.IdleModeManager.Interrupted )
                {
                    if ( Settings.IdleIndexing )
                    {
                        Settings.IdleModeManager.DropInterrupted();
                        OutlookSession.OutlookProcessor.QueueIdleJob( this );
                    }
                    break;
                }
            }
            if ( Settings.IdleModeManager.CompletedIdle )
            {
                OutlookSession.OutlookProcessor.SetSyncComplete();
                mailSync.RemoveDeletedMailsFromIndex();
            }
            else if ( !Settings.IdleModeManager.Idle )
            {
                if ( mailSync.IndexStartDate == DateTime.MinValue )
                {
                    OutlookSession.OutlookProcessor.SetSyncComplete();
                }
                mailSync.RemoveDeletedMailsFromIndex();
            }
            return;
        }
    }

    internal class FreshMailEnumerator : AbstractNamedJob
    {
        private static Tracer _tracer = new Tracer( "FreshMailEnumerator" );
        private DateTime _dtRestrict;
        public FreshMailEnumerator() : base()
        {
            _dtRestrict = Settings.IndexStartDate;
        }
        #region INamedUnitOfWork Members

        public override string Name
        {
            get { return "FreshMailEnumerator"; } 
            set { }
        }

        #endregion

        public static void ProcessFolder( FolderDescriptor folderDescriptor, DateTime dtRestrict )
        {
            _tracer.Trace( "Synchronize folder: " + folderDescriptor.Name );

            IResource resStore = Folder.FindMAPIStore( folderDescriptor.FolderIDs.StoreId );
            if ( resStore == null )
            {
                return;
            }
            DateTime date = resStore.GetDateProp( PROP.LastReceiveDate );
            if ( date == DateTime.MinValue )
            {
                date = dtRestrict;
            }

            MailSync mailSync = new MailSync( false, date );
            mailSync.Refresh = true;
            mailSync.EnumerateMessageItems( folderDescriptor );
        }

        protected override void Execute()
        {
            DateTime minDate = Settings.LastExecutionTime;
            if ( minDate != DateTime.MinValue )
            {
                minDate = minDate.AddDays( -14 );
            }
            IResourceList folders = 
                Core.ResourceStore.FindResourcesInRange( STR.MAPIFolder, PROP.LastMailDate, 
                    minDate, DateTime.Now );
            for ( int i = folders.Count - 1; i >= 0; --i )
            {
                IResource folder = Core.ResourceStore.TryLoadResource( folders.ResourceIds[i] );
                if ( folder == null )
                {
                    continue;
                }
                PairIDs folderIDs = PairIDs.Get( folder );
                if ( folderIDs == null )
                {
                    continue;
                }
                FolderDescriptor folderDescriptor = FolderDescriptor.Get( folderIDs );
                if ( folderDescriptor != null )
                {
                    ProcessFolder( folderDescriptor, _dtRestrict );
                }
            }
        }
    }
    internal class MsgStoresEnumeratorJob : ReenteringEnumeratorJob
    {
        private Tracer _tracer = new Tracer( "MsgStoresEnumeratorJob" );
        private MsgStores _msgStores;
        private int _index = 0;

        public MsgStoresEnumeratorJob()
        {
            _msgStores = OutlookSession.GetMsgStores();
            if ( _msgStores == null ) return;
        }

        public override void EnumerationStarting()
        {
            _tracer.Trace( "EnumerationStarting" );
        }

        public override void EnumerationFinished()
        {
            _tracer.Trace( "EnumerationFinished" );
        }

        public override AbstractJob GetNextJob()
        {
            IEMsgStore msgStore = null;
            while ( (_index < _msgStores.Count ) && msgStore == null )
            {
                msgStore = _msgStores.GetMsgStore( _index++ );
            }
            if ( msgStore == null )
            {
                return null;
            }
            return new FolderEnumeratorJob( msgStore.GetRootFolder() );
        }

        public override string Name
        {
            get { return ""; }
            set {}
        }
    }

    internal class FolderEnumeratorJob : ReenteringEnumeratorJob
    {
        private static Tracer _tracer = new Tracer( "FolderEnumeratorJob" );
        private IEFolder _folder;
        private IEFolders _folders;
        private int _count = 0;
        private int _index = 0;

        public FolderEnumeratorJob( IEFolder folder )
        {
            _folder = folder;
            if ( _folder == null ) return;
            _folders = OutlookSession.GetFolders( _folder );
            if ( _folders != null )
            {
                _count = _folders.GetCount();
            }
        }

        public override void EnumerationStarting()
        {
            _tracer.Trace( "EnumerationStarting" );
        }

        public override void EnumerationFinished()
        {
            if ( _folders != null )
            {
                _folders.Dispose();
            }
            _tracer.Trace( "EnumerationFinished" );
        }

        public override AbstractJob GetNextJob()
        {
            if ( _folders != null )
            {
                IEFolder folder = null;
                while ( (_index < _count ) && folder == null )
                {
                    folder = _folders.OpenFolder( _index++ );
                }
                if ( folder != null )
                {
                    _tracer.Trace( "folder = " + folder.GetStringProp( MAPIConst.PR_DISPLAY_NAME ) );
                    OutlookSession.OutlookProcessor.QueueJob( new MailEnumeratorJob( folder ) );
                    return new FolderEnumeratorJob( folder );
                }
            }
            return null;
        }

        public override string Name
        {
            get { return ""; }
            set {}
        }
    }


    internal class MailEnumeratorJob : ReenteringEnumeratorJob
    {
        private static Tracer _tracer = new Tracer( "MailEnumeratorJob" );
        private IEFolder _folder;
        //private int _count = 0;
        //private int _index = 0;

        public MailEnumeratorJob( IEFolder folder )
        {
            _folder = folder;
            if ( _folder == null ) return;
        }

        public override void EnumerationStarting()
        {
            _tracer.Trace( "EnumerationStarting" );
        }

        public override void EnumerationFinished()
        {
            if ( _folder != null )
            {
                _folder.Dispose();
            }
            _tracer.Trace( "EnumerationFinished" );
        }

        public override AbstractJob GetNextJob()
        {
            return null;
        }

        public override string Name
        {
            get { return "Enumeration for mail"; }
            set {}
        }
    }
}
