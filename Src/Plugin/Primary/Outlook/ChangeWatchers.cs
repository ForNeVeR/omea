// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class ChangeWatchers
    {
        private ImportedContactsChangeWatcher _importedContactsChangeWatcher = new ImportedContactsChangeWatcher();
        private AddressBookChangeWatcher _addressBookChangeWatcher = new AddressBookChangeWatcher();
        private UnreadEmailChangeWatcher _unreadWatcher = new UnreadEmailChangeWatcher();
        private IgnoredFoldersChangeWatcher _ignoredFolderWatcher = new IgnoredFoldersChangeWatcher();
        private ImportanceMailChangeWatcher _importanceMailWathcer = new ImportanceMailChangeWatcher();
        private AnnotationMailChangeWatcher _annotationMailWathcer = new AnnotationMailChangeWatcher();
        private TasksChangeWatcher _tasksChangeWatcher = new TasksChangeWatcher();
        private LinksChangeWatcher _linksChangeWatcher = new LinksChangeWatcher();
        //private ContactEntryIDWatcher _contactEntryIDWatcher = new ContactEntryIDWatcher();
        private CategoriesWatcher _categoriesWatcher = new CategoriesWatcher();
        private PhoneChangeWatcher _phoneChangeWatcher = new PhoneChangeWatcher();
        public void Watch()
        {
            //if ( Settings.TraceContactChanges )
        {
            //_contactEntryIDWatcher.Watch();
        }
            _categoriesWatcher.Watch();
            _addressBookChangeWatcher.Watch();
            _unreadWatcher.Watch();
            _importanceMailWathcer.Watch();
            _tasksChangeWatcher.Watch();
            _linksChangeWatcher.Watch();
            _ignoredFolderWatcher.Watch();
            _importedContactsChangeWatcher.Watch();
            _phoneChangeWatcher.Watch();
            if ( Settings.CreateAnnotationFromFollowup )
            {
                _annotationMailWathcer.Watch();
            }
        }
    }

    internal class IgnoredFoldersChangeWatcher
    {
        private IResourceList _ignoredFolderList = null;
        public void Watch()
        {
            _ignoredFolderList = Folder.GetIgnoredFoldersLive();
            _ignoredFolderList.ResourceAdded += new ResourceIndexEventHandler( OnIgnoredFolderChanged );
            _ignoredFolderList.ResourceDeleting += new ResourceIndexEventHandler( OnIgnoredFolderChanged );
        }
        private void OnIgnoredFolderChanged( object sender, ResourceIndexEventArgs e )
        {
            Core.ResourceAP.QueueJob( new FolderIgnoringChanged( e.Resource ) );
        }
    }

    internal class UnreadEmailChangeWatcher
    {
        private IResourceList _unreadList = null;
        public void Watch()
        {
            _unreadList =
                Core.ResourceStore.FindResourcesLive( STR.Email, Core.Props.IsUnread, true );
            _unreadList.ResourceAdded += new ResourceIndexEventHandler( OnUnreadItemChanged );
            _unreadList.ResourceDeleting += new ResourceIndexEventHandler( OnUnreadItemChanged );
        }
        private void OnUnreadItemChanged( object sender, ResourceIndexEventArgs e )
        {
            OutlookSession.OutlookProcessor.QueueJob( JobPriority.AboveNormal, "Processing unread item changed",
                new ResourceDelegate( OnUnreadItemChangedImpl ), e.Resource );
        }
        private void OnUnreadItemChangedImpl( IResource emailResource )
        {
            Guard.NullArgument( emailResource, "emailResource" );
            if ( emailResource.Type != STR.Email )
            {
                return;
            }
            PairIDs messageIDs = PairIDs.Get( emailResource );
            if ( messageIDs == null )
            {
                return;
            }

            IResource folder = Mail.GetParentFolder( emailResource );
            if ( folder != null && Folder.IsIMAPFolder( folder ) )
            {
                PairIDs folderIDs = PairIDs.Get( folder );
                if ( folderIDs != null )
                {
                    IEFolder mapiFolder = OutlookSession.OpenFolder( folderIDs.EntryId, folderIDs.StoreId );
                    if ( mapiFolder != null )
                    {
                        using ( mapiFolder )
                        {
                            try
                            {
                                mapiFolder.SetReadFlags( messageIDs.EntryId, emailResource.HasProp( Core.Props.IsUnread ) );
                                return;
                            }
                            catch ( COMException exception )
                            {
                                if ( exception.ErrorCode == ( unchecked( (int)0x80040604 ) ) )
                                {
                                    StandartJobs.MessageBox( "Unspecified error. Can't change unread flag for email.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                                    return;
                                }
                                Core.ReportException( exception, ExceptionReportFlags.AttachLog );
                            }
                        }
                    }
                }
            }

            IEMessage message = OutlookSession.OpenMessage( messageIDs.EntryId, messageIDs.StoreId );
            if ( message != null )
            {
                using ( message )
                {
                    bool unread = emailResource.HasProp( Core.Props.IsUnread );
                    message.SetUnRead( unread );
                    OutlookSession.SaveChanges( "Export Read/Unread flag" + emailResource.Id, message, messageIDs.EntryId );
                }
            }
        }
    }

    internal class ImportanceMailChangeWatcher
    {
        private IResourceList _importanceMailList = null;
        public void Watch()
        {
            _importanceMailList = Core.ResourceStore.FindResourcesWithPropLive( STR.Email, PROP.Importance );
            _importanceMailList.ResourceChanged += new ResourcePropIndexEventHandler( OnImportanceMailChanged );
            _importanceMailList.ResourceAdded += new ResourceIndexEventHandler( OnImportanceMailChanged );
            _importanceMailList.ResourceDeleting += new ResourceIndexEventHandler( OnImportanceMailChanged );
            _importanceMailList.AddPropertyWatch( PROP.Importance );
        }
        private void OnImportanceMailChanged( object sender, ResourcePropIndexEventArgs e )
        {
            OnImportanceMailChanged( e.Resource );
        }

        private void OnImportanceMailChanged( object sender, ResourceIndexEventArgs e )
        {
            OnImportanceMailChanged( e.Resource );
        }
        private void OnImportanceMailChanged( IResource resource )
        {
            OutlookSession.OutlookProcessor.QueueJob( JobPriority.AboveNormal, "Processing importance item changed",
                new ResourceDelegate( OnImportanceMailChangedImpl ), resource );
        }
        private void OnImportanceMailChangedImpl( IResource emailResource )
        {
            if ( emailResource != null && emailResource.Type == STR.Email )
            {
                if ( Mail.MailInIMAP( emailResource ) )
                {
                    return;
                }

                int importance = emailResource.GetIntProp( PROP.Importance );
                PairIDs messageIDs = PairIDs.Get( emailResource );
                if ( messageIDs == null )
                {
                    return;
                }
                IEMessage message = OutlookSession.OpenMessage( messageIDs.EntryId, messageIDs.StoreId );
                if ( message != null )
                {
                    using ( message )
                    {
                        message.SetLongProp( MAPIConst.PR_IMPORTANCE, importance + 1 );
                        OutlookSession.SaveChanges( "Export importance flag for resource id = " + emailResource.Id, message, messageIDs.EntryId );
                    }
                }
            }
        }
    }

    internal class TasksChangeWatcher
    {
        private IResourceList _tasksList = null;
        public void Watch()
        {
            _tasksList = Core.ResourceStore.GetAllResourcesLive( STR.Task );
            _tasksList.ResourceChanged += new ResourcePropIndexEventHandler( OnTaskChanged );
            _tasksList.AddPropertyWatch( new int[]
                {
                    PROP.Status, PROP.RemindDate, PROP.StartDate, Core.Props.Date, PROP.Description,
                    Core.Props.Subject, PROP.Priority
                } );
            _tasksList.ResourceAdded += new ResourceIndexEventHandler( OnTaskAdded );
            _tasksList.ResourceDeleting += new ResourceIndexEventHandler( OnTaskDeleted );
        }
        private void OnTaskAdded( object sender, ResourceIndexEventArgs e )
        {
            if ( Settings.ExportTasks )
            {
                new ExportTaskDescriptor( e.Resource ).QueueJob( JobPriority.AboveNormal );
            }
        }
        private void OnTaskDeleted( object sender, ResourceIndexEventArgs e )
        {
            if ( Settings.ExportTasks )
            {
                PairIDs IDs = PairIDs.Get( e.Resource );
                if ( IDs != null )
                {
                    OutlookSession.DeleteMessage( IDs.StoreId, IDs.EntryId, true );
                }
            }
        }
        private void OnTaskChanged( object sender, ResourcePropIndexEventArgs e )
        {
            if ( Settings.ExportTasks )
            {
                new ExportTaskDescriptor( e.Resource ).QueueJob( JobPriority.AboveNormal );
            }
        }
    }

    internal class ImportedContactsChangeWatcher
    {
        private ArrayList _importedContacts = new ArrayList();
        private static bool _processingImportFromOutlook = false;
        private static int _exportedContactID = -1;
        private IResourceList _importedABs = null;

        class AddressBookWatcher
        {
            private IResource _importedAB;
            private IResourceList _contacts;
            public AddressBookWatcher( IResource importedAB )
            {
                _importedAB = importedAB;
                _contacts = _importedAB.GetLinksOfTypeLive( "Contact", "InAddressBook" );
                _contacts.ResourceAdded += new ResourceIndexEventHandler( OnImportedContactAdded );
                _contacts.ResourceChanged += new ResourcePropIndexEventHandler( OnImportedContactChanged );
                _contacts.ResourceDeleting += new ResourceIndexEventHandler( OnImportedContactDeleting );
                _contacts.AddPropertyWatch( new int[]
                    {
                        PROP.EntryID, ResourceProps.DisplayName,
                        Core.ContactManager.Props.EmailAddress,
                        Core.ContactManager.Props.LinkEmailAcct,
                        ContactManager._propBirthday,
                        ContactManager._propCompany,
                        ContactManager._propDescription,
                        ContactManager._propFirstName,
                        ContactManager._propHomePage,
                        ContactManager._propImported,
                        ContactManager._propJobTitle,
                        ContactManager._propLastName,
                        ContactManager._propMiddleName,
                        ContactManager._propPhone,
                        ContactManager._propSuffix,
                        ContactManager._propTitle,
                        ContactManager._propPhone,
                        ContactManager._propUserCreated
                    } );
            }

            private void OnImportedContactChanged( object sender, ResourcePropIndexEventArgs e )
            {
                string oldID = (string)e.ChangeSet.GetOldValue( PROP.EntryID );
                object oldUserCreated = e.ChangeSet.GetOldValue( ContactManager._propUserCreated );

                //-------------------------------------------------------------
                //  An Outlook Contact can be removed from the AB via two mechanisms:
                //  - moved to the Deleted Items. In such case the Outlook resource
                //    generally exists, and EntryID still exists (though it can be
                //    changed). For this case MAPI Listener's OnMailMoved is called,
                //    which calls Contact.RemoveFromSynch( IResource, newID ).
                //  - deleted physically. In this case EntryId is physically removed
                //    along with the object itself. Here MAPI Listener's OnMailDeleted
                //    is called, which calls Contact.RemoveFromSynch( IResource, true ).
                //  In both cases MAPI routines set "UserCreated" boolean prop to "true"
                //  which serves as indication for us to unlink the contact and the AB.
                //-------------------------------------------------------------
                if( oldUserCreated == null && e.Resource.HasProp( ContactManager._propUserCreated ) )
                {
                    new ResourceProxy( e.Resource ).DeleteLink( "InAddressBook", _importedAB );
                }
                else
                //-------------------------------------------------------------
                //  Specially go around the case when EntryId is being removed, this
                //  is covered (on the more general level) by previous condition.
                //  LX: this restriction is left "as is" from SergeZhulin.
                //-------------------------------------------------------------
                if ( e.Resource.HasProp( PROP.EntryID ) || oldID == null )
                {
                    if ( !ProcessingImportFromOutlook && !ImportedContactsChangeWatcher.IsExportedContact( e.Resource ) )
                    {
                        OutlookSession.OutlookProcessor.QueueJob( new ExportContactDescriptor( e.Resource ) );
                    }
                    ImportedContactsChangeWatcher.CleanExportedContact( e.Resource );
                }
            }

            private void OnImportedContactAdded( object sender, ResourceIndexEventArgs e )
            {
                ImportedContactAdded( e.Resource, _importedAB );
            }

            /// <summary>
            /// Method handles the deletion of a Contact resource from any supported
            /// AB. Because of the particular definition of watched resource list
            /// we have no need to check given resource (e.Resource) for actual
            /// belonging to the AB.
            /// </summary>
            private void OnImportedContactDeleting( object sender, ResourceIndexEventArgs e )
            {
                string storeID = _importedAB.GetStringProp( PROP.StoreID );
                if ( storeID != null )
                {
                    OutlookSession.DeleteMessage( storeID, e.Resource.GetStringProp( PROP.EntryID ), true );
                    Contact.RemoveFromSync( e.Resource );
                }
            }
        }

        public static void ImportedContactAdded( IResource contact, IResource AB )
        {
            if ( !ProcessingImportFromOutlook )
            {
                ImportedContactsChangeWatcher.SetExportedContact( contact );
                OutlookSession.OutlookProcessor.QueueJob( new ExportContactDescriptor( contact, AB ) );
            }
        }

        public static bool IsExportedContact( IResource contact )
        {
            return _exportedContactID == contact.Id;
        }
        public static void SetExportedContact( IResource contact )
        {
            _exportedContactID = contact.Id;
        }
        public static void CleanExportedContact( IResource contact )
        {
            _exportedContactID = -1;
        }

        public void Watch()
        {
            _importedABs = Core.ResourceStore.FindResourcesLive( "AddressBook", PROP.Imported, 1 );
            _importedABs.ResourceAdded+=new ResourceIndexEventHandler(_importedABs_ResourceAdded);
            foreach ( IResource importedAB in _importedABs )
            {
                _importedContacts.Add( new AddressBookWatcher( importedAB ) );
            }
        }
        public static bool ProcessingImportFromOutlook { set { _processingImportFromOutlook = value; } get { return _processingImportFromOutlook; } }

        private void _importedABs_ResourceAdded(object sender, ResourceIndexEventArgs e)
        {
            _importedContacts.Add( new AddressBookWatcher( e.Resource ) );
        }
    }

    internal class CategoriesWatcher
    {
        private IResourceList _categoriesList = null;
        public void Watch()
        {
            _categoriesList = Core.ResourceStore.GetAllResourcesLive( "Category" );
            _categoriesList.ResourceChanged += new ResourcePropIndexEventHandler( CategoryChanged );
            _categoriesList.AddPropertyWatch( Core.Props.Parent );
            _categoriesList.AddPropertyWatch( Core.Props.Name );
        }

        private void ProcessCategories( IResource category, string resType )
        {
            IResourceList resources = category.GetLinksOfType( resType, "Category" );
            foreach ( IResource resource in resources )
            {
                ExportCategories.Do( JobPriority.AboveNormal, resource );
            }
        }
        private void ProcessCategoriesForContact( IResource category )
        {
            IResourceList resources = category.GetLinksOfType( STR.Contact, "Category" );
            foreach ( IResource resource in resources )
            {
                OutlookSession.OutlookProcessor.QueueJob( JobPriority.AboveNormal, new ExportContactDescriptor( resource ) );
            }
        }

        private void ProcessCategoriesRecursive( IResource category )
        {
            if ( Settings.SyncMailCategory )
            {
                ProcessCategories( category, STR.Email );
            }
            if ( Settings.SyncTaskCategory )
            {
                ProcessCategories( category, STR.Task );
            }
            if ( Settings.SyncContactCategory )
            {
                ProcessCategoriesForContact( category );
            }
            IResourceList subCategories = category.GetLinksTo( "Category", Core.Props.Parent );
            foreach ( IResource resource in subCategories )
            {
                ProcessCategoriesRecursive( resource );
            }
        }

        private void CategoryChanged( object sender, ResourcePropIndexEventArgs e )
        {
            if ( !Settings.SyncTaskCategory && !Settings.SyncMailCategory && !Settings.SyncContactCategory )
            {
                return;
            }
            if ( e.ChangeSet.IsPropertyChanged( Core.Props.Parent ) || e.ChangeSet.IsPropertyChanged( Core.Props.Name ) )
            {
                ProcessCategoriesRecursive( e.Resource );
            }
        }
    }

    internal class LinksChangeWatcher
    {
        public void Watch()
        {
            Core.ResourceStore.LinkAdded += new LinkEventHandler( LinkAdded );
            Core.ResourceStore.LinkDeleted += new LinkEventHandler( LinkDeleted );
        }
        private void LinkAdded( object sender, LinkEventArgs e )
        {
            if ( e.Source.Type == STR.Email && e.Target.Type == STR.Category )
            {
                IResourceList resAttachments = e.Source.GetLinksOfType( null, PROP.Attachment );
                foreach ( IResource attach in resAttachments.ValidResources )
                {
                    Core.CategoryManager.AddResourceCategory( attach, e.Target );
                }
            }
            LinkAddedOrDeleted( sender, e );
        }
        private void LinkDeleted( object sender, LinkEventArgs e )
        {
            if ( e.Source.Type == STR.Email && e.Target.Type == STR.Category )
            {
                IResourceList resAttachments = e.Source.GetLinksOfType( null, PROP.Attachment );
                foreach ( IResource attach in resAttachments.ValidResources )
                {
                    Core.CategoryManager.RemoveResourceCategory( attach, e.Target );
                }
            }
            LinkAddedOrDeleted( sender, e );
        }
        private void LinkAddedOrDeleted( object sender, LinkEventArgs e )
        {
            if ( e.Source.Type == STR.Email )
            {
                if ( e.Target.Type == STR.Category && Settings.SyncMailCategory )
                {
                    ExportCategories.Do( JobPriority.AboveNormal, e.Source );
                }
                else if ( e.Target.Type == STR.Flag )
                {
                    ExportEmailFlag.Do( JobPriority.AboveNormal, e.Source );
                }
            }
            else
            if ( e.Source.Type == STR.Task )
            {
                if ( e.Target.Type == STR.Category && Settings.SyncTaskCategory )
                {
                    ExportCategories.Do( JobPriority.AboveNormal, e.Source );
                }
            }
            else
            if ( e.Source.Type == STR.Contact )
            {
                if( e.Target.Type == STR.Category && Settings.SyncContactCategory )
                {
                    OutlookSession.OutlookProcessor.QueueJob( JobPriority.AboveNormal, new ExportContactCategoryDescriptor( e.Source ) );
                }
            }
        }
    }

    internal class AddressBookChangeWatcher
    {
        private IResourceList _ABList = null;
        public void Watch()
        {
            _ABList = Core.ResourceStore.FindResourcesWithPropLive( "AddressBook", PROP.EntryID );
            _ABList.AddPropertyWatch( Core.Props.Name );
            _ABList.ResourceChanged +=new ResourcePropIndexEventHandler(_ABList_ResourceChanged);
            _ABList.ResourceDeleting += new ResourceIndexEventHandler( _ABList_ResourceDeleting );
        }

        private void _ABList_ResourceDeleting( object sender, ResourceIndexEventArgs e )
        {
            string entryID = e.Resource.GetStringProp( PROP.EntryID );
            IResource container = Folder.Find( entryID );
            if ( container == null )
            {
                container = Core.ResourceStore.FindUniqueResource( STR.OutlookABDescriptor, PROP.EntryID, entryID );
            }
            if ( container != null )
            {
                Folder.SetIgnoreImport( container, true );
            }
        }

        private void _ABList_ResourceChanged(object sender, ResourcePropIndexEventArgs e)
        {
            string entryID = e.Resource.GetStringProp( PROP.EntryID );
            IResource resFolder = Folder.Find( entryID );
            PairIDs pairIDs = PairIDs.Get( resFolder );
            if ( pairIDs == null )
            {
                return;
            }

            IEFolder folder = OutlookSession.OpenFolder( pairIDs.EntryId, pairIDs.StoreId );
            if ( folder != null )
            {
                using ( folder )
                {
                    if ( !e.Resource.GetPropText( Core.Props.Name ).EndsWith( "(Outlook)" ) )
                    {
                        folder.SetStringProp( MAPIConst.PR_DISPLAY_NAME, e.Resource.GetStringProp( Core.Props.Name ) );
                        folder.SaveChanges();
                    }
                }
            }
        }
    }

    internal class PhoneChangeWatcher
    {
        private IResourceList _phonesList = null;
        public void Watch()
        {
            _phonesList = Core.ResourceStore.GetAllResourcesLive( "Phone" );
            _phonesList.ResourceChanged += new ResourcePropIndexEventHandler( OnPhoneChanged );
            _phonesList.AddPropertyWatch( new int[] {ContactManager._propPhoneName, ContactManager._propPhoneNumber} );
        }
        private void OnPhoneChanged( object sender, ResourcePropIndexEventArgs e )
        {
            IResource phone = e.Resource;
            IResource contact = phone.GetLinkProp( ContactManager._propPhone );
            if ( contact != null && contact.HasProp( PROP.Imported ) && contact.HasProp( PROP.EntryID ) )
            {
                OutlookSession.OutlookProcessor.QueueJob( new ExportContactDescriptor( contact ) );
            }
        }
    }

    internal class AnnotationMailChangeWatcher
    {
        private IResourceList _mailList = null;
        public void Watch()
        {
            _mailList = Core.ResourceStore.FindResourcesWithPropLive( STR.Email, Core.Props.Annotation );
            _mailList.ResourceChanged += new ResourcePropIndexEventHandler( OnMailChanged );
            _mailList.ResourceAdded += new ResourceIndexEventHandler( OnMailChanged );
            _mailList.ResourceDeleting += new ResourceIndexEventHandler( OnMailChanged );
            _mailList.AddPropertyWatch( Core.Props.Annotation );
        }
        private void OnMailChanged( object sender, ResourcePropIndexEventArgs e )
        {
            ExportAnnotation( e.Resource );
        }

        private void OnMailChanged( object sender, ResourceIndexEventArgs e )
        {
            ExportAnnotation( e.Resource );
        }
        public static void ExportAnnotation( IResource resource )
        {
            OutlookSession.OutlookProcessor.QueueJob( JobPriority.AboveNormal, "Processing annotation item changed",
                new ResourceDelegate( ExportAnnotationImpl ), resource );
        }
        private static void ExportAnnotationImpl( IResource emailResource )
        {
            Guard.NullArgument( emailResource, "emailResource" );
            if ( emailResource.Type != STR.Email )
            {
                throw new ArgumentException( "Expected 'Email' resource but was " + emailResource.Type );
            }
            if ( Mail.MailInIMAP( emailResource ) )
            {
                return;
            }

            PairIDs messageIDs = PairIDs.Get( emailResource );
            if ( messageIDs == null )
            {
                return;
            }
            IEMessage message = OutlookSession.OpenMessage( messageIDs.EntryId, messageIDs.StoreId );
            if ( message == null )
            {
                return;
            }
            using ( message )
            {
                string annotation = emailResource.GetPropText( Core.Props.Annotation );
                int tag = message.GetIDsFromNames( ref GUID.set1, lID.msgFlagAnnotation, PropType.PT_STRING8 );
                string oldAnnotation = message.GetStringProp( tag );
                if ( oldAnnotation == null )
                {
                    oldAnnotation = string.Empty;
                }
                if ( !oldAnnotation.Equals( annotation ) )
                {
                    if ( annotation.Length == 0 )
                    {
                        message.DeleteProp( tag );
                    }
                    else
                    {
                        message.SetStringProp( tag, annotation );
                    }
                    OutlookSession.SaveChanges( "ExportAnnotation for resource id = " + emailResource.Id, message, messageIDs.EntryId );
                }
            }
        }
    }
}
