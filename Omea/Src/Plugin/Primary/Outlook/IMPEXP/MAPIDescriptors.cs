/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class ProcessMailAddNtf
    {
        private ProcessMailAddNtf()
        {}
        public static bool ProcessIMAPMessage( IEFolder folder, string entryID )
        {
            Tracer._Trace( "ProcessIMAPMessage" );
            IETable table = folder.GetEnumTable( DateTime.MinValue );
            if ( table == null )
            {
                return false;
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
                    IERowSet row = table.GetNextRow();
                    if ( row == null )
                    {
                        continue;
                    }
                    using ( row )
                    {
                        if ( row.GetBinProp( 0 ) == entryID )
                        {
                            if ( row.GetLongProp( 6 ) == 1 )
                            {
                                Tracer._Trace( "ProcessIMAPMessage FALSE" );
                                //folder.SetMessageStatus( entryID, 0x1000, 0x1000 );
                                return false;
                            }
                            else
                            {
                                Tracer._Trace( "ProcessIMAPMessage TRUE" );
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        public static void DoJobImpl( MAPINtf ntf, IEMessage message, FolderDescriptor folderDescriptor )
        {
            string entryId = OutlookSession.GetMessageID( message );
            string messageClass = MessageType.GetMessageClass( message );

            if ( MessageType.InterpretAsMail( messageClass ) )
            {
                new MailDescriptor( folderDescriptor, entryId, message ).QueueJob( JobPriority.AboveNormal );
            }
            else if ( MessageType.InterpretAsContact( messageClass ) )
            {
                ContactDescriptorWrapper.Do( JobPriority.AboveNormal, folderDescriptor, entryId, entryId );
            }
            else if ( MessageType.InterpretAsTask( messageClass ) )
            {
                Tracer._Trace( "Task was added" );
                TaskDescriptor.Do( JobPriority.AboveNormal, folderDescriptor, message, entryId );
            }
            else
            {
                Tracer._Trace( "Unknown item of class " + messageClass + " was added" );
            }
        }

        public static void DoJob( MAPINtf ntf, string storeID )
        {
            if ( ntf == null )
            {
                return;
            }

            try
            {
                IEFolder folder = OutlookSession.OpenFolder( ntf.ParentID, storeID );
                if ( folder == null )
                {
                    return;
                }
                FolderDescriptor folderDescriptor = FolderDescriptor.Get( folder );
                if ( folderDescriptor == null )
                {
                    return;
                }
                using ( folder )
                {
                    if ( folderDescriptor.ContainerClass == FolderType.IMAP )
                    {
                        if ( !ProcessIMAPMessage( folder, ntf.EntryID ) )
                        {
                            return;
                        }
                    }
                    IEMessage message = OutlookSession.OpenMessage( ntf.EntryID, storeID );
                    if ( message == null )
                    {
                        return;
                    }
                    using ( message )
                    {
                        DoJobImpl( ntf, message, folderDescriptor );
                    }
                }
            }
            catch ( System.Threading.ThreadAbortException exception )
            {
                Tracer._TraceException( exception );
            }
            catch ( Exception exception )
            {
                Core.ReportException( exception, ExceptionReportFlags.AttachLog );
            }
        }
    }

    internal class MailMovedDescriptor : AbstractNamedJob
    {
        private MAPIFullNtf _ntf;
        private string _storeID;
        public MailMovedDescriptor( MAPIFullNtf ntf, string storeID )
        {
            Guard.NullArgument( ntf, "ntf" );
            Guard.NullArgument( storeID, "storeID" );
            _storeID = storeID;
            _ntf = ntf;
        }
        protected override void Execute()
        {
            if ( OutlookSession.OutlookProcessor.ShuttingDown )
            {
                return;
            }

            IEFolder folder = OutlookSession.OpenFolder( _ntf.ParentID, _storeID );
            if ( folder != null )
            {
                using ( folder )
                {
                    FolderDescriptor folderDescriptor = FolderDescriptor.Get( folder );
                    if ( folderDescriptor == null )
                    {
                        return;
                    }
                    IResource resFolder = Folder.Find( folderDescriptor.FolderIDs.EntryId );
                    if ( resFolder == null )
                    {
                        return;
                    }

                    bool ignoredFolder = Folder.IsIgnored( resFolder );

                    IEMessage message = OutlookSession.OpenMessage( _ntf.EntryID, _storeID );
                    if ( message == null )
                    {
                        return;
                    }
                    using ( message )
                    {
                        string entryId = OutlookSession.GetMessageID( message );
                        IResource mail = Core.ResourceStore.FindUniqueResource( "Email", PROP.EntryID, _ntf.EntryID );
                        if ( mail == null && _ntf.OldEntryID != null )
                        {
                            mail = Core.ResourceStore.FindUniqueResource( "Email", PROP.EntryID, _ntf.OldEntryID );
                        }

                        if ( ignoredFolder && mail != null )
                        {
                            Trace.WriteLine( "Moved mail ID=" + mail.Id + " to ignored folder" );
                            Mail.ForceDelete( mail );
                            return;
                        }
                        if ( mail == null )
                        {
                            ProcessMailAddNtf.DoJob( _ntf, _storeID );
                            return;
                        }
                        mail.SetProp( PROP.EntryID, entryId );
                        Folder.LinkMail( resFolder, mail );
                    }
                }
            }
        }

        public override string Name
        {
            get { return "Process mail moving"; }
            set
            {}
        }
    }

    internal class MailDeletedDescriptor : AbstractNamedJob
    {
        private IResource _resourceToDelete;

        public MailDeletedDescriptor( MAPINtf ntf, string storeId )
        {
            Guard.NullArgument( ntf, "ntf" );
            Guard.NullArgument( storeId, "storeID" );
            IEMessage deletedItem = OutlookSession.OpenMessage( ntf.EntryID, storeId );
            if ( deletedItem != null )
            {
                using ( deletedItem )
                {
                    Trace.WriteLine( "Successfully opened deleted item resource" );
                    string entryId = OutlookSession.GetMessageID( deletedItem );
                    if( String.IsNullOrEmpty( entryId ))
                        throw new ArgumentNullException( "entryId", "MailDeletedDescriptor -- NULL entryId string of the existing IEMessage");

                    FindResourcesByEntryId( entryId );
                }
            }
            else
            {
                FindResourcesByEntryId( ntf.EntryID );
                if ( _resourceToDelete != null )
                {
                    return;
                }

                // we've got a short-term entry ID in the notification; we need to scan the parent
                // folder to find the resources which need to be deleted
                IEFolder parentFolder = OutlookSession.OpenFolder( ntf.ParentID, storeId );
                if ( parentFolder != null )
                {
                    using ( parentFolder )
                    {
                        string parentId = OutlookSession.GetFolderID( parentFolder );
                        IResource parentMAPIFolder = Folder.Find( parentId );
                        if ( parentMAPIFolder != null )
                        {
                            if ( Folder.IsFolderOfType( parentMAPIFolder, FolderType.Contact ) )
                            {
                                RemoveContactFromSync( parentId, storeId );
                                return;
                            }

                            //  Deletion from the folder which we ignore anyway
                            //  must not lead to a great performance loss.
                            if( !parentMAPIFolder.HasProp( PROP.IgnoredFolder ))
                            {
                                FindResourceToDelete( parentFolder, parentMAPIFolder, storeId );
                            }
                        }
                    }
                }
            }
        }

        private void FindResourceToDelete( IEFolder parentFolder, IResource parentMAPIFolder, string storeId )
        {
            // NOTE: In Cached Exchange mode (at least), messages can be opened successfully
            // immediately after they are permanently deleted, but opening them through
            // the short-term entry ID that we got in the notification still does not work.
            // Thus, if the message was permanently deleted in Outlook, the code below will
            // not find the deleted message, and we will have to rely on background enumeration
            // to delete it.
            // When the message is permanently deleted in OmniaMea, we immediately delete
            // the resource as well.
            IResourceList children = parentMAPIFolder.GetLinksOfType( null, PROP.MAPIFolder );
            foreach ( IResource child in children.ValidResources )
            {
                if ( child.HasProp( PROP.EntryID ) )
                {
                    IEMessage deletedItem = OutlookSession.OpenMessage( child.GetStringProp( PROP.EntryID ), storeId );
                    if ( deletedItem != null )
                    {
                        //  Item exists in the folder, the object is not needed anymore
                        deletedItem.Dispose();
                    }
                    else
                    {
                        Trace.WriteLine( "Failed to open resource with ID=" + child.Id + ", queueing for delete" );
                        _resourceToDelete = child;
                        break;
                    }
                }
            }
            if ( _resourceToDelete == null )
            {
                HashSet set = new HashSet( children.Count );
                Tracer._Trace( "Cannot find deleted mail." );
                Tracer._Trace( "Try another algorithm." );
                foreach ( IResource child in children.ValidResources )
                {
                    if ( !child.HasProp( PROP.EntryID ) )
                    {
                        continue;
                    }
                    set.Add( child.GetStringProp( PROP.EntryID ) );
                }

                IEMessages messages = parentFolder.GetMessages();
                if ( messages != null )
                {
                    using ( messages )
                    {
                        for ( int i = 0 ; i < messages.GetCount(); i++ )
                        {
                            IEMessage message = messages.OpenMessage( i );
                            if ( message != null )
                            {
                                using ( message )
                                {
                                    string entryid = message.GetBinProp( MAPIConst.PR_ENTRYID );
                                    if ( entryid != null )
                                    {
                                        set.Remove( entryid );
                                    }
                                }
                            }
                        }
                    }
                }
                if ( set.Count > 0 )
                {
                    foreach ( HashSet.Entry entry in set )
                    {
                        FindResourcesByEntryId( (string)entry.Key );
                        if ( _resourceToDelete != null )
                        {
                            Tracer._Trace( "Resource found for deleting." );
                            break;
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        //  Iterate over all contacts in the AB represented by parentId, find all 
        //  contact resources with corresponding IEMessages not existing in the AB,
        //  and remove them from AB - remove EntryID property AND unlink with AB
        //  resource.
        //---------------------------------------------------------------------
        private void RemoveContactFromSync( string parentId, string storeId )
        {
            IResource AB = Core.ResourceStore.FindUniqueResource( "AddressBook", PROP.EntryID, parentId );
            if ( AB != null )
            {
                IResourceList contacts = AB.GetLinksOfType( "Contact", "InAddressBook" );
                foreach ( IResource contact in contacts.ValidResources )
                {
                    if ( contact.HasProp( PROP.EntryID ) )
                    {
                        IEMessage contactMsg = OutlookSession.OpenMessage( contact.GetStringProp( PROP.EntryID ), storeId );
                        if ( contactMsg != null )
                        {
                            //  Item exists in the Outlook folder, the object is not
                            //  needed anymore.
                            contactMsg.Dispose();
                        }
                        else
                            Contact.RemoveFromSync( contact, true );
                    }
                }
            }
        }

        private void FindResourcesByEntryId( string entryId )
        {
            _resourceToDelete = Core.ResourceStore.FindUniqueResource( STR.Email, PROP.EntryID, entryId );
            if ( _resourceToDelete == null )
            {
                _resourceToDelete = Core.ResourceStore.FindUniqueResource( STR.Task, PROP.EntryID, entryId );
            }
            if ( _resourceToDelete == null )
            {
                _resourceToDelete = Contact.FindByEntryID( entryId );
            }
            if ( _resourceToDelete != null )
            {
                Trace.WriteLine( "Found resource to delete: ID=" + _resourceToDelete.Id );
            }
        }

        protected override void Execute()
        {
            if ( _resourceToDelete != null )
            {
                switch ( _resourceToDelete.Type )
                {
                    case STR.Email:   Mail.ForceDelete( _resourceToDelete ); break;
                    case STR.Task:    _resourceToDelete.Delete();            break;
                    case STR.Contact: Contact.RemoveFromSync( _resourceToDelete, true );  break;
                }
            }
        }

        public override string Name
        {
            get { return "Deleting mail, task or contact"; }
            set {}
        }
    }

    internal class FolderModifiedDescriptor : AbstractNamedJob
    {
        protected FolderDescriptor _folder;
        protected FolderDescriptor _parentFolder;
        private bool _isMovedFolder;

        public FolderModifiedDescriptor( MAPINtf ntf, string storeID, bool isMovedFolder )
        {
            Guard.NullArgument( ntf, "ntf" );
            _isMovedFolder = isMovedFolder;
            IEFolder folder = OutlookSession.OpenFolder( ntf.EntryID, storeID );
            if ( folder != null )
            {
                using ( folder )
                {
                    _folder = FolderDescriptor.Get( folder );
                    if ( ntf.ParentID != null && ntf.ParentID.Length > 0 )
                    {
                        IEFolder parentFolder =
                            OutlookSession.OpenFolder( ntf.ParentID, storeID );
                        if ( parentFolder != null )
                        {
                            using ( parentFolder )
                            {
                                _parentFolder = FolderDescriptor.Get( parentFolder );
                            }
                        }
                    }
                }
            }
        }
        protected override void Execute()
        {
            if ( _folder != null && !String.IsNullOrEmpty( _folder.FolderIDs.EntryId ) )
            {
                IResource resFolder = Folder.Find( _folder.FolderIDs.EntryId );
                if ( resFolder != null )
                {
                    if ( !Folder.IsParentRoot( resFolder ) )
                    {
                        Folder.SetName( resFolder, _folder.Name );
                    }
                    if ( _isMovedFolder && _parentFolder != null )
                    {
                        IResource resParentFolder = Folder.Find( _parentFolder.FolderIDs.EntryId );
                        if ( resParentFolder != null )
                        {
                            Folder.SetParent( resFolder, resParentFolder );
                        }
                    }
                }
                FolderStructureDescriptor.UpdateContactFolder( _folder );
            }
        }

        public override string Name
        {
            get { return "Process folder modified notification"; }
            set
            {}
        }
    }

    internal class FolderAddDescriptor : FolderModifiedDescriptor
    {
        private FolderAddDescriptor( MAPINtf ntf, string storeID ) : base( ntf, storeID, false )
        {}

        public static void Do( JobPriority jobPriority, MAPINtf ntf, string storeID )
        {
            FolderAddDescriptor descriptor = new FolderAddDescriptor( ntf, storeID );
            if ( descriptor.GetParentFolder() != null )
            {
                Core.ResourceAP.QueueJob( jobPriority, descriptor );
            }
        }

        private IResource GetParentFolder()
        {
            if ( _folder != null && _parentFolder != null )
            {
                return Folder.Find( _parentFolder.FolderIDs.EntryId );
            }
            return null;
        }

        protected override void Execute()
        {
            IResource resParentFolder = GetParentFolder();
            if ( resParentFolder != null )
            {
                IResource resFolder = Folder.FindOrCreate( _folder, resParentFolder );
                Folder.SetSeeAll( resFolder, true );
            }
        }
    }

    internal class FolderDeletedDescriptor : AbstractNamedJob
    {
        private string _entryId;
        private MAPINtf _ntf;
        private string _storeID;
        private bool _retry = true;
        public FolderDeletedDescriptor( MAPINtf ntf, string storeID )
        {
            Guard.NullArgument( ntf, "ntf" );
            Guard.EmptyStringArgument( storeID, "storeID" );
            _ntf = ntf;
            _storeID = storeID;

            // The notification contains only the short-term entry ID, and since the
            // folder has already been deleted, it is no longer possible to get the
            // long-term entry ID. Thus, we need to scan the children of the parent of
            // the deleted folder and check if all of them still exist.
            IResource resFolder = Folder.Find( ntf.EntryID );
            if ( resFolder == null )
            {
                IEFolder parentFolder =
                    OutlookSession.OpenFolder( ntf.ParentID, storeID );
                if ( parentFolder != null )
                {
                    using ( parentFolder )
                    {
                        string parentId = OutlookSession.GetFolderID( parentFolder );
                        IResource parentMAPIFolder = Folder.Find( parentId );
                        if ( parentMAPIFolder == null )
                        {
                            parentMAPIFolder = Folder.Find( ntf.ParentID );
                        }

                        if ( parentMAPIFolder != null )
                        {
                            IResourceList childFolders = parentMAPIFolder.GetLinksTo( "MAPIFolder", "Parent" );
                            foreach ( IResource childFolderRes in childFolders )
                            {
                                IEFolder childFolder = OutlookSession.OpenFolder( childFolderRes.GetStringProp( "EntryID" ),
                                                                                  storeID );
                                if ( childFolder != null )
                                {
                                    childFolder.Dispose();
                                }
                                else
                                {
                                    _entryId = childFolderRes.GetStringProp( "EntryID" );
                                }
                            }

                            if ( _entryId == null )
                            {
                                HashSet set = new HashSet( childFolders.Count );
                                foreach ( IResource childFolderRes in childFolders )
                                {
                                    set.Add( childFolderRes.GetStringProp( "EntryID" ) );
                                }

                                IEFolders folders = OutlookSession.GetFolders( parentFolder );
                                if ( folders != null )
                                {
                                    using ( folders )
                                    {
                                        for ( int i = 0; i < folders.GetCount(); ++i )
                                        {
                                            IEFolder folder = OutlookSession.OpenFolder( folders, i );
                                            if ( folder != null )
                                            {
                                                using ( folder )
                                                {
                                                    string entryId = folder.GetBinProp( MAPIConst.PR_ENTRYID );
                                                    if ( entryId != null )
                                                    {
                                                        set.Remove( entryId );
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                foreach ( HashSet.Entry entry in set )
                                {
                                    _entryId = (string)entry.Key;
                                    break;
                                }

                                if ( _entryId == null && Retry )
                                {
                                    OutlookSession.OutlookProcessor.QueueJobAt( DateTime.Now.AddMinutes( 2 ), "Delete folder",
                                        new MethodInvoker( CreateFolderDeletedDescriptor ) );
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                _entryId = ntf.EntryID;
            }
        }
        public bool Retry { set { _retry = value; } get { return _retry; } }

        private void CreateFolderDeletedDescriptor()
        {
            FolderDeletedDescriptor descriptor = new FolderDeletedDescriptor( _ntf, _storeID );
            descriptor.Retry = false;
            Core.ResourceAP.QueueJob( JobPriority.AboveNormal, descriptor );
        }

        protected override void Execute()
        {
            if ( _entryId != null )
            {
                IResource resFolder = Folder.Find( _entryId );
                if ( resFolder != null )
                {
                    Folder.DeleteFolder( resFolder );
                }
            }
        }

        public override string Name
        {
            get { return "Process folder deleted notification"; }
            set
            {}
        }
    }
}