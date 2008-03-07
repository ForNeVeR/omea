/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.Contacts;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class STR
    {
        public const string EmailFile = "EmailFile";
        public const string Email = "Email";
        public const string Contact = "Contact";
        public const string Flag = "Flag";
        public const string MessageFlag = "MessageFlag";
        public const string Importance = "Importance";
        public const string Priority = "Priority";
        public const string AddressBook = "AddressBook";
        public const string Category = "Category";
        public const string Subject = "Subject";
        public const string Name = "Name";
        public const string MAPIStore = "MAPIStore";
        public const string FileTypeMap = "FileTypeMap";
        public const string MAPIFolder = "MAPIFolder";
        public const string EntryID = "EntryID";
        public const string StoreID = "StoreID";
        public const string RecordKey = "RecordKey";
        public const string InternetMsgID = "InternetMsgID";
        public const string ReplyTo = "ReplyTo";
        public const string Date = "Date";
        public const string SentOn = "SentOn";
        public const string Unread = "IsUnread";
        public const string Extension = "Extension";
        public const string SyncVersion = "SyncVersion";
        public const string Description = "Description";
        public const string IgnoredFolder = "IgnoredFolder";
        public const string ContainerClass = "ContainerClass";
        public const string MAPIVisible = "MAPIVisible";
        public const string DefaultFolder = "DefaultFolder";
        public const string EmailAccount = "EmailAccount";
        public const string OutlookABDescriptor = "OutlookABDescriptor";
        public const string IgnoreContactImport = "IgnoreContactImport";
        public const string DeletedItemsFolder = "DeletedItemsFolder";
        public const string DefaultDeletedItems = "DefaultDeletedItems";
        public const string MAPIInfoStore = "MAPIInfoStore";
        public const string DeletedItemsEntryID = "DeletedItemsEntryID";
        public const string JunkEmailEntryID = "JunkEmailEntryID";
        public const string PR_STORE_SUPPORT_MASK = "PR_STORE_SUPPORT_MASK";
        public const string PR_CONTENT_COUNT = "PR_CONTENT_COUNT";
        public const string PR_ICON_INDEX = "PR_ICON_INDEX";
        public const string PR_ATTACH_CONTENT_ID = "PR_ATTACH_CONTENT_ID";
        public const string PR_ATTACH_NUM = "PR_ATTACH_NUM";
        public const string StoreSupported = "StoreSupported";
        public const string StoreTypeChecked = "StoreTypeChecked";
        public const string LastMailDate = "LastMailDate";
        public const string ResourceAttachment = "ResourceAttachment";
        public const string ShowPictures = "ShowPictures";
        public const string EmbeddedMessage = "EmbeddedMessage";
        public const string NoFormat = "NoFormat";
        public const string BodyFormat = "BodyFormat";
        public const string Imported = "Imported";
        public const string InitialEmailEnum = "InitialEmailEnum";
        public const string LastReceiveDate = "LastReceiveDate";    
        public const string OUTLOOK_GENERAL = "Outlook General";
        public const string OUTLOOK_FOLDERS = "Outlook Folders";
        public const string OUTLOOK_ADDRESS_BOOKS = "Outlook Address Books";
        public const string OUTLOOK_TASKS_PANE = "Outlook Tasks";
        public const string OMTaskId = "OMTaskId";
        public const string Task = "Task";
        public const string SuperTaskLink = "SuperTask";
        public const string DefaultFolderEntryIDs = "DefaultFolderEntryIDs";

        public const string AttachmentType = "AttachmentType";
        public const string Attachment = "Attachment";
        public const string AttachmentIndex = "AttachmentIndex";
        public const string AttachMethod = "AttachMethod";
        //  Used to cache the sizes of the pictorial attachments so that we
        //  don't try to reload it many times (this must be used in conjunction
        //  with caching of the picture attachment itself).
        public const string Width = "Width";
        public const string Height = "Height";
    }

    internal class PROP
    {
        public static int EntryID;
        public static int StoreID;
        public static int RecordKey;
        public static int SentOn;
        public static int LastReceiveDate;
        public static int InternetMsgID;
        public static int ReplyTo;
        public static int ConversationIndex;
        public static int ReplyToConversationIndex;
        public static int Extension;
        public static int ResType;

        public static int From;
        public static int To;
        public static int CC;
        public static int EmailAccountFrom;
        public static int EmailAccountTo;
        public static int EmailAccountCC;
        public static int AttType;
        public static int Attachment;
        public static int InternalAttachment;
        public static int TopLevelCategory;
        public static int OwnerStore;
        public static int AttachmentIndex;
        public static int AttachMethod;
        public static int MAPIFolder;
        public static int SelectedInFolder;
        public static int MySelf;
        public static int OpenIgnoreFolder;
        public static int OpenSelectFolder;
        public static int SeeAll;
        public static int ContainerClass;
        public static int MAPIVisible;
        public static int DefaultFolder;
        public static int ResourceTransfer;
        public static int NoFormat;
        public static int BodyFormat;
        public static int LastMailDate;

        public static int Status;
        public static int RemindDate;
        public static int StartDate;
        public static int Description;
        public static int Imported;
        public static int MessageFlag;
        public static int Priority;
        public static int Importance;
        public static int LastModifiedTime;
        public static int IgnoredFolder;
        public static int Target;
        public static int SuperTaskLink;
        public static int SyncVersion;
        public static int SyncComplete;
        public static int IgnoreContactImport;
        public static int DeletedItemsFolder;
        public static int DefaultDeletedItems;
        public static int DeletedItemsEntryID;
        public static int JunkEmailEntryID;
        public static int PR_STORE_SUPPORT_MASK;
        public static int StoreSupported;
        public static int StoreTypeChecked;
        public static int PR_CONTENT_COUNT;
        public static int PR_ICON_INDEX;
        public static int PR_ATTACH_NUM;
        
        public static int ShowPictures;
        public static int OMTaskId;
        public static int DeletedInIMAP;
        public static int EmbeddedMessage;
        public static int DefaultFolderEntryIDs;

        public static int AttachmentPicWidth;
        public static int AttachmentPicHeight;
    }
    internal class REGISTRY
    {
        private static bool _registered = false;

        public static void RegisterTypes( OutlookPlugin ownerPlugin, IContactManager contactManager )
        {
            string exts = Core.SettingStore.ReadString( "FilePlugin", "EmailMessageFileExts" );
            exts = ( exts.Length == 0 ) ? ".msg" : exts + ",.msg";
            string[] extsArray = exts.Split( ',' );
            for( int i = 0; i < extsArray.Length; ++i )
            {
                extsArray[ i ] = extsArray[ i ].Trim();
            }
            Core.FileResourceManager.RegisterFileResourceType(
                STR.EmailFile, "Email File", STR.Name, 0, ownerPlugin, extsArray );
            RegisterMailTypes( ownerPlugin, contactManager );
        }
        public static bool IsRegistered
        {
            get { return _registered; }
        }
        public static IResourceStore RS
        {
            get { return Core.ResourceStore; }
        }
        private static void RegisterMailTypes( OutlookPlugin ownerPlugin, IContactManager contactManager )
        {
            if ( RS.ResourceTypes.Exist( "MAPIFolderRoot" ) )
            {
                IResourceList resourcesToDelete = RS.GetAllResources("MAPIFolderRoot");
                foreach ( IResource resource in resourcesToDelete )
                {
                    resource.Delete();
                }
            }
            RegisterResources( ownerPlugin );
            PROP.SyncVersion  = 
                RS.PropTypes.Register( STR.SyncVersion, PropDataType.Int, PropTypeFlags.Internal | PropTypeFlags.NoSerialize );

            PROP.EntryID = ResourceTypeHelper.UpdatePropTypeRegistration( STR.EntryID, PropDataType.String, 
                                                                          PropTypeFlags.Internal | PropTypeFlags.NoSerialize );
            PROP.StoreID   = RS.PropTypes.Register( STR.StoreID, PropDataType.String, PropTypeFlags.Internal | PropTypeFlags.NoSerialize );
            PROP.RecordKey = ResourceTypeHelper.UpdatePropTypeRegistration( STR.RecordKey, PropDataType.String, 
                                                                            PropTypeFlags.Internal | PropTypeFlags.NoSerialize );
            PROP.InternetMsgID = ResourceTypeHelper.UpdatePropTypeRegistration( STR.InternetMsgID, 
                                                                                PropDataType.String, PropTypeFlags.Internal );
            PROP.ReplyTo   = ResourceTypeHelper.UpdatePropTypeRegistration( STR.ReplyTo, PropDataType.String, PropTypeFlags.Internal );
            PROP.ConversationIndex = RS.PropTypes.Register( "ConversationIndex", PropDataType.String, PropTypeFlags.Internal );
            PROP.ReplyToConversationIndex = RS.PropTypes.Register( "ReplyToConversationIndex", PropDataType.String, PropTypeFlags.Internal );

            PROP.SentOn = RS.PropTypes.Register( STR.SentOn, PropDataType.Date );
            PROP.LastReceiveDate = RS.PropTypes.Register( STR.LastReceiveDate, PropDataType.Date );

            PROP.Extension = RS.PropTypes.Register( STR.Extension, PropDataType.String );
            PROP.ResType   = RS.PropTypes.Register( "ResType", PropDataType.String, PropTypeFlags.Internal );
            PROP.AttachmentIndex = RS.PropTypes.Register( STR.AttachmentIndex, PropDataType.Int, PropTypeFlags.Internal );
            PROP.AttachMethod = RS.PropTypes.Register( STR.AttachMethod, PropDataType.Int, PropTypeFlags.Internal );
            
            PROP.ResourceTransfer = RS.PropTypes.Register( "ResorceTransfer", PropDataType.Bool, PropTypeFlags.Internal );

            CorrectDeletedItemsProperty();

            PROP.DeletedInIMAP = RS.PropTypes.Register( "DeletedInIMAP", PropDataType.Bool, PropTypeFlags.Internal );
            PROP.IgnoredFolder = RS.PropTypes.Register( "IgnoredFolder", PropDataType.Int, PropTypeFlags.Internal );
            PROP.DefaultFolderEntryIDs = RS.PropTypes.Register( STR.DefaultFolderEntryIDs, PropDataType.StringList, PropTypeFlags.Internal );
            PROP.MySelf = Core.ContactManager.Props.Myself;
            PROP.Imported = RS.PropTypes.Register( STR.Imported, PropDataType.Int, PropTypeFlags.Internal );
            PROP.OpenIgnoreFolder = RS.PropTypes.Register( "OpenIgnoreFolder", PropDataType.Int, PropTypeFlags.Internal );
            PROP.OpenSelectFolder = RS.PropTypes.Register( "OpenSelectFolder", PropDataType.Int, PropTypeFlags.Internal );
            PROP.MessageFlag = RS.PropTypes.Register( STR.MessageFlag, PropDataType.String, PropTypeFlags.Internal );
            PROP.LastModifiedTime = ResourceTypeHelper.UpdatePropTypeRegistration( "LastModifiedTime", 
                PropDataType.Date, PropTypeFlags.Internal );
            PROP.Priority = RS.PropTypes.Register( STR.Priority, PropDataType.Int );
            PROP.EmbeddedMessage = RS.PropTypes.Register( STR.EmbeddedMessage, PropDataType.Bool, PropTypeFlags.Internal );
            PROP.Importance = RS.PropTypes.Register( STR.Importance, PropDataType.Int );
            PROP.SeeAll = RS.PropTypes.Register( "SeeAll", PropDataType.Bool, PropTypeFlags.Internal );
            PROP.ContainerClass = RS.PropTypes.Register( STR.ContainerClass, PropDataType.String, PropTypeFlags.Internal );
            PROP.BodyFormat = RS.PropTypes.Register( STR.BodyFormat, PropDataType.String, PropTypeFlags.Internal );
            PROP.DefaultFolder = RS.PropTypes.Register( STR.DefaultFolder, PropDataType.Bool, PropTypeFlags.Internal );
            PROP.MAPIVisible = RS.PropTypes.Register( STR.MAPIVisible, PropDataType.Bool, PropTypeFlags.Internal );
            PROP.ShowPictures = RS.PropTypes.Register( STR.ShowPictures, PropDataType.Bool, PropTypeFlags.Internal );
            PROP.NoFormat = RS.PropTypes.Register( STR.NoFormat, PropDataType.Bool, PropTypeFlags.Internal );

            PROP.SyncComplete = RS.PropTypes.Register( "SyncComplete", PropDataType.Bool, PropTypeFlags.Internal );
            PROP.Target = ResourceTypeHelper.UpdatePropTypeRegistration( "Target", PropDataType.Link, PropTypeFlags.DirectedLink );
            PROP.IgnoreContactImport = RS.PropTypes.Register( STR.IgnoreContactImport, PropDataType.Bool, PropTypeFlags.Internal );
            PROP.DeletedItemsFolder = RS.PropTypes.Register( STR.DeletedItemsFolder, PropDataType.Bool, PropTypeFlags.Internal );
            PROP.DefaultDeletedItems = RS.PropTypes.Register( STR.DefaultDeletedItems, PropDataType.Bool, PropTypeFlags.Internal );
            PROP.DeletedItemsEntryID = RS.PropTypes.Register( STR.DeletedItemsEntryID, PropDataType.String, PropTypeFlags.Internal );
            PROP.JunkEmailEntryID = RS.PropTypes.Register( STR.JunkEmailEntryID, PropDataType.String, PropTypeFlags.Internal );
            PROP.PR_STORE_SUPPORT_MASK = RS.PropTypes.Register( STR.PR_STORE_SUPPORT_MASK, PropDataType.Int, PropTypeFlags.Internal );
            PROP.StoreSupported = RS.PropTypes.Register( STR.StoreSupported, PropDataType.Bool, PropTypeFlags.Internal );
            PROP.StoreTypeChecked = RS.PropTypes.Register( STR.StoreTypeChecked, PropDataType.Bool, PropTypeFlags.Internal );
            PROP.PR_CONTENT_COUNT = RS.PropTypes.Register( STR.PR_CONTENT_COUNT, PropDataType.Int, PropTypeFlags.Internal );
            PROP.PR_ICON_INDEX = RS.PropTypes.Register( STR.PR_ICON_INDEX, PropDataType.Int, PropTypeFlags.Internal );
            CommonProps.Register();

            if ( RS.PropTypes.Exist( STR.PR_ATTACH_CONTENT_ID ) )
            {
                IResourceList list = RS.FindResourcesWithProp( STR.Email, STR.PR_ATTACH_CONTENT_ID );
                foreach ( IResource mail in list )
                {
                    mail.SetProp( CommonProps.ContentId, mail.GetStringProp( STR.PR_ATTACH_CONTENT_ID ) );
                    mail.DeleteProp( STR.PR_ATTACH_CONTENT_ID );
                }
                IPropType propType = RS.PropTypes[STR.PR_ATTACH_CONTENT_ID];
                RS.PropTypes.Delete( propType.Id );
            }
            PROP.PR_ATTACH_NUM = RS.PropTypes.Register( STR.PR_ATTACH_NUM, PropDataType.Int, PropTypeFlags.Internal );
            PROP.LastMailDate = RS.PropTypes.Register( STR.LastMailDate, PropDataType.Date, PropTypeFlags.Internal );
            PROP.OMTaskId = RS.PropTypes.Register( STR.OMTaskId, PropDataType.String, PropTypeFlags.Internal | PropTypeFlags.NoSerialize );

            PROP.AttachmentPicWidth = RS.PropTypes.Register(STR.Width, PropDataType.Int, PropTypeFlags.Internal);
            PROP.AttachmentPicHeight = RS.PropTypes.Register(STR.Height, PropDataType.Int, PropTypeFlags.Internal);

            PROP.From = contactManager.Props.LinkFrom;
            PROP.To = contactManager.Props.LinkTo;
            PROP.CC = contactManager.Props.LinkCC;

            PROP.EmailAccountFrom = Core.ContactManager.Props.LinkEmailAcctFrom;
            PROP.EmailAccountTo = Core.ContactManager.Props.LinkEmailAcctTo;
            PROP.EmailAccountCC = Core.ContactManager.Props.LinkEmailAcctCC;
            RS.RegisterLinkRestriction( STR.Email, PROP.From, "Contact", 0, 1 );

            PROP.SelectedInFolder = RS.PropTypes.Register( "SelectedInFolder", PropDataType.Link , PropTypeFlags.Internal );

            PROP.Attachment = Core.ResourceStore.PropTypes.Register( STR.Attachment, PropDataType.Link, 
                                                                     PropTypeFlags.SourceLink | PropTypeFlags.DirectedLink, ownerPlugin );
            RS.PropTypes.RegisterDisplayName(PROP.Attachment, "Outlook Message", "Attachment");

            PROP.AttType = RS.PropTypes.Register( STR.AttachmentType, PropDataType.Link, PropTypeFlags.Internal );
            PROP.OwnerStore = RS.PropTypes.Register( "OwnerStore",     PropDataType.Link, PropTypeFlags.Internal );
            PROP.TopLevelCategory = RS.PropTypes.Register( "TopLevelCategory",     PropDataType.Link, PropTypeFlags.Internal );
            PROP.InternalAttachment = RS.PropTypes.Register( "InternalAttachment", PropDataType.Link, PropTypeFlags.Internal );

            IResource propMAPIFolderLink = RS.FindUniqueResource( "PropType", "Name", STR.MAPIFolder );
            PropTypeFlags flags = PropTypeFlags.Normal | PropTypeFlags.CountUnread;
            if ( propMAPIFolderLink != null )
            {
                propMAPIFolderLink.SetProp( "Flags", (int)flags );
                PROP.MAPIFolder = propMAPIFolderLink.GetIntProp( "ID" );
            }
            else
            {
                PROP.MAPIFolder = RS.PropTypes.Register( STR.MAPIFolder, PropDataType.Link, flags );
            }
            RS.PropTypes.RegisterDisplayName( PROP.MAPIFolder, "Outlook Folder" );

            RS.ResourceTypes.Register( STR.Task, "Subject" );

            RS.RegisterUniqueRestriction( STR.Email, PROP.EntryID );
            RS.RegisterUniqueRestriction( STR.OutlookABDescriptor, PROP.EntryID );
            RS.RegisterUniqueRestriction( "AddressBook", PROP.EntryID );
            RS.RegisterUniqueRestriction( STR.MAPIInfoStore, PROP.EntryID );
            RS.RegisterUniqueRestriction( STR.MAPIInfoStore, PROP.DeletedItemsEntryID );
            RS.RegisterUniqueRestriction( STR.MAPIInfoStore, PROP.JunkEmailEntryID );
            RS.RegisterUniqueRestriction( "Contact", PROP.EntryID );
            RS.RegisterUniqueRestriction( STR.MAPIStore, PROP.StoreID );

            RS.RegisterLinkRestriction( STR.Email, PROP.MAPIFolder, STR.MAPIFolder, 0, 1 );
            RS.RegisterUniqueRestriction( STR.MAPIFolder, PROP.EntryID );
            RS.RegisterLinkRestriction( STR.MAPIFolder, Core.Props.Parent, null, 1, 1 );
            RS.RegisterLinkRestriction( STR.MAPIFolder, PROP.OwnerStore, STR.MAPIStore, 1, 1 );
            RS.RegisterLinkRestriction( STR.Email, PROP.OwnerStore, STR.MAPIStore, 0, 1 );
            RS.DeleteUniqueRestriction( STR.Task, PROP.OMTaskId );
            RS.RegisterUniqueRestriction( STR.Task, PROP.EntryID );

            RemoveInvalidAttachmentResources();
            RemoveAttachmentResources();
            UpdateOutlookAttachments( );
            ChangeDatePropForMAPIStore();

            PROP.Status = RS.PropTypes.Register( "Status", PropDataType.Int );
            PROP.RemindDate = RS.PropTypes.Register( "RemindDate", PropDataType.Date );
            PROP.StartDate = RS.PropTypes.Register( "StartDate", PropDataType.Date );
            PROP.Description = RS.PropTypes.Register( "Description", PropDataType.String );
            //  NB: this prop format must coinside with that in TasksPlugin.
            PROP.SuperTaskLink = RS.PropTypes.Register( STR.SuperTaskLink, PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );
            RS.ResourceTypes.Register( "SentItemsEnumSign", "", ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );
            UpdateLastMailDateForFolders();
            RemoveWrongGlobalBooks();
            RemoveOMTaskIDs();
            UpdateDeletedItemsFolders();
            DeleteInvalidMAPIFolders();
            UpdateMapiInfoStores();
            _registered = true;
        }
        private static void UpdateMapiInfoStores()
        {
            IResourceList stores = Core.ResourceStore.GetAllResources( STR.MAPIInfoStore );
            foreach ( IResource store in stores.ValidResources )
            {
                if ( !store.HasProp( PROP.StoreSupported ) )
                {
                    store.SetProp( PROP.IgnoredFolder, 1 );
                }
            }
        }
        private static void UpdateDeletedItemsFolders()
        {
            if ( OutlookProcessor.SyncVersion < 7 )
            {
                IResourceList folders = Core.ResourceStore.FindResources( STR.MAPIFolder, PROP.DeletedItemsFolder, true );
                foreach ( IResource folder in folders )
                {
                    folder.SetProp( Core.Props.ShowDeletedItems, true );
                }
            }
        }
        private static void CorrectDeletedItemsProperty()
        {
            if ( OutlookProcessor.SyncVersion < 6 )
            {
                int InDeletedItems = RS.PropTypes.Register( "InDeletedItems", PropDataType.Bool, PropTypeFlags.Internal );
                IResourceList deletedResources = Core.ResourceStore.FindResourcesWithProp( STR.Email, InDeletedItems );
                foreach ( IResource mail in deletedResources )
                {
                    Mail.SetIsDeleted( mail, true );
                }
                RS.PropTypes.Delete( InDeletedItems );
            }
        }

        private static void UpdateOutlookAttachments( )
        {
            if ( OutlookProcessor.SyncVersion < 5 )
            {
                IResourceList mailWithAttachments = 
                    RS.FindResourcesWithProp( STR.Email, PROP.Attachment );
                foreach( IResource mail in mailWithAttachments )
                {
                    IResourceList attachments = mail.GetLinksFrom( null, PROP.Attachment );
                    foreach( IResource attachment in attachments )
                    {
                        if ( !mail.HasProp( PROP.EmbeddedMessage ) )
                        {
                            mail.DeleteLink( PROP.Attachment, attachment );
                            attachment.AddLink( PROP.Attachment, mail );
                        }
                    }
                }
            }
            if ( OutlookProcessor.SyncVersion < 8 )
            {
                IResourceList mailWithAttachments = 
                    RS.FindResourcesWithProp( STR.Email, PROP.Attachment );
                foreach( IResource mail in mailWithAttachments )
                {
                    IResourceList attachments = mail.GetLinksFrom( null, PROP.Attachment );
                    foreach( IResource attachment in attachments )
                    {
                        ContactManager.CloneLinkage( mail, attachment );
                    }
                }
            }
        }

        private static void ChangeDatePropForMAPIStore()
        {
            if ( OutlookProcessor.SyncVersion < 11 )
            {
                IResourceList resources = RS.FindResourcesWithProp( STR.MAPIStore, Core.Props.Date );
                foreach ( IResource resource in resources )
                {
                    resource.SetProp( PROP.LastReceiveDate, resource.GetDateProp( Core.Props.Date ) );
                    resource.DeleteProp( Core.Props.Date );
                }
            }
        }
        private static void RemoveInvalidAttachmentResources()
        {
            if ( OutlookProcessor.SyncVersion < 11 )
            {
                IResourceList attachments = RS.FindResourcesWithProp( null, PROP.AttachmentIndex );
                IResourceList resList1 = RS.FindResourcesWithProp( null, PROP.Attachment );
                IResourceList resList2 = RS.FindResourcesWithProp( null, PROP.InternalAttachment );
                attachments = attachments.Minus( resList1 );
                attachments = attachments.Minus( resList2 );
                attachments.DeleteAll();
            }
        }
        private static void RemoveAttachmentResources()
        {
            //move attachment properties to format file resource
            try
            {
                if ( RS.ResourceTypes.Exist( STR.Attachment ) )
                {
                    IResourceList attachments = Core.ResourceStore.GetAllResources( STR.Attachment );
                    for ( int i = attachments.Count - 1; i >= 0 ; i-- )
                    {
                        IResource attachment = attachments[i];
                        IResource formatFile = attachment.GetLinkProp( "Source" );
                        if ( formatFile != null )
                        {
                            formatFile.SetProp( Core.Props.Name, attachment.GetProp( Core.Props.Name ) );
                            formatFile.SetProp( Core.Props.Date, attachment.GetProp( Core.Props.Date ) );
                            formatFile.SetProp( PROP.AttachmentIndex, attachment.GetProp( PROP.AttachmentIndex ) );
                            if ( attachment.HasProp( PROP.ResourceTransfer ) )
                            {
                                formatFile.SetProp( PROP.ResourceTransfer, true );
                            }
                            IResourceList froms = attachment.GetLinksFrom( "Contact", PROP.From );
                            foreach ( IResource from in froms )
                            {
                                formatFile.AddLink( PROP.From, from );
                            }
                            IResourceList tos = formatFile.GetLinksFrom( "Contact", PROP.To );
                            foreach ( IResource to in tos )
                            {
                                formatFile.AddLink( PROP.To, to );
                            }
                            attachment.Delete();
                        }
                        else
                        {
                            IResource mail = attachment.GetLinkProp( PROP.InternalAttachment );
                            if ( mail != null )
                            {
                                attachment.AddLink( PROP.Attachment, mail );
                            }
                            attachment.ChangeType( "UnknownFile" );
                        }
                    }
                    RS.ResourceTypes.Delete( STR.Attachment );
                }
            }
            catch( StorageException ) {}
        }
        private static HashSet _testedContacts = new HashSet();
        public static bool ClearNeeded( IResource contact )
        {
            lock ( _testedContacts )
            {
                bool tested = _testedContacts.Contains( contact.Id );
                _testedContacts.Add( contact.Id );
                if ( !tested )
                {
                    if ( ExportContactDescriptor.IsClearNeeded( contact ) )
                    {
                        Contact.RemoveFromSync( contact, true );
                        return true;
                    }
                }
            }
            return false;
        }

        public static void ClearInvalidEntryIDFromContacts()
        {
            IResourceList contacts = RS.FindResourcesWithProp( STR.Contact, PROP.EntryID );
            foreach ( IResource contact in contacts )
            {
                ClearNeeded( contact );
            }
        }
        private static void RemoveWrongGlobalBooks()
        {
            IResourceList gals = RS.GetAllResources( STR.OutlookABDescriptor );
            for ( int i = gals.Count - 1; i >= 0 ; i-- )
            {
                IResource gal = gals[i];
                if ( Folder.Find( gal.GetPropText( PROP.EntryID ) ) != null )
                {
                    gal.Delete();
                }
            }
        }
        private static void RemoveOMTaskIDs()
        {
            IResourceList tasks = RS.GetAllResources( STR.Task );
            foreach ( IResource task in tasks )
            {
                task.DeleteProp( PROP.OMTaskId );                
            }
        }

        private static void UpdateLastMailDateForFolders()
        {
            IResourceList folders = RS.FindResourcesWithProp( STR.MAPIFolder, PROP.LastMailDate );
            if ( folders.Count == 0 )
            {
                folders = RS.GetAllResources( STR.MAPIFolder );
                foreach ( IResource folder in folders.ValidResources )
                {
                    IResourceList mail = folder.GetLinksOfType( STR.Email, PROP.MAPIFolder );
                    if ( mail.Count > 0 )
                    {
                        mail.Sort( new SortSettings( Core.Props.Date, false ) );
                        folder.SetProp( PROP.LastMailDate, mail[0].GetDateProp( Core.Props.Date ) );
                    }
                }
            }
        }

        private static void DeleteInvalidMAPIFolders()
        {
            IResourceList list = Core.ResourceStore.GetAllResources( STR.MAPIFolder );
            foreach ( IResource folder in list.ValidResources )
            {
                PairIDs folderIDs = PairIDs.Get( folder );
                if ( folderIDs == null )
                {
                    folder.Delete();
                }
            }
        }

        private static void RegisterResources( IPlugin ownerPlugin )
        {
            OwnerEmailDetector.Initialize();
            AddressBook.Initialize();
            RS.ResourceTypes.Register( STR.Email, STR.Email, STR.Subject, ResourceTypeFlags.CanBeUnread, ownerPlugin );
            RS.ResourceTypes.Register( STR.AttachmentType, STR.AttachmentType, 
                STR.Name, ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, ownerPlugin );
            RS.ResourceTypes.Register( STR.MAPIStore, string.Empty, ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            RS.ResourceTypes.Register( STR.MAPIInfoStore, STR.Name, ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            RS.ResourceTypes.Register( STR.FileTypeMap, string.Empty, ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            RS.ResourceTypes.Register( STR.MAPIFolder, "Outlook Folder", STR.Name, ResourceTypeFlags.ResourceContainer | ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal, ownerPlugin );
            RS.ResourceTypes.Register( STR.OutlookABDescriptor, STR.Name, ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );
            RS.ResourceTypes.Register( STR.SyncVersion, STR.Name, ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            RS.ResourceTypes.Register( STR.ResourceAttachment, STR.Name, ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            RS.ResourceTypes.Register( STR.InitialEmailEnum, string.Empty, ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );

            //  OM-11397. Register owner plugin "Outlook" for this type even if
            //  it is core type. This is necessary to keep condition
            //  "Sent to Mailing list" out of Omea Reader version.
            RS.ResourceTypes.Register( "MailingList", "Mailing List", "EmailAcct", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, ownerPlugin );
        }
    }        
}
