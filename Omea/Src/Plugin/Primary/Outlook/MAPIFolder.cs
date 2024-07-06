// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OutlookPlugin;

namespace JetBrains.Omea.OutlookPlugin
{
    internal abstract class Contact
    {
        public static void RemoveFromSync( IResource contact, bool removeEntryID )
        {
            ResourceProxy proxy = new ResourceProxy( contact );
            proxy.AsyncPriority = JobPriority.Immediate;
            proxy.BeginUpdate();
            if ( removeEntryID )
            {
                proxy.DeleteProp( PROP.EntryID );
            }
            proxy.SetProp( "UserCreated", true );
            proxy.EndUpdateAsync();
        }
        public static void RemoveFromSync( IResource contact, string newEntryID )
        {
            ResourceProxy proxy = new ResourceProxy( contact );
            proxy.BeginUpdate();
            proxy.SetProp( PROP.EntryID, newEntryID );
            proxy.SetProp( "UserCreated", true );
            proxy.EndUpdateAsync();
        }
        public static void RemoveFromSync( IResource contact )
        {
            RemoveFromSync( contact, false );
        }
        public static IResource FindByEntryID( string entryID )
        {
            return Core.ResourceStore.FindUniqueResource( STR.Contact, PROP.EntryID, entryID );
        }
    }

    internal abstract class Mail
    {
        public static IResource GetParentFolder( IResource mail )
        {
            return mail.GetLinkProp( PROP.MAPIFolder );
        }

        public static bool MailInIMAP( IResource resource )
        {
            if ( resource.Type == STR.Email )
            {
                IResource folder = GetParentFolder( resource );
                if ( folder != null && Folder.IsIMAPFolder( folder ) )
                {
                    return true;
                }
            }
            return false;
        }
        public static bool CanBeDeleted( IResource mail )
        {
            return !mail.HasProp( PROP.DeletedInIMAP ) && !mail.HasProp( PROP.EmbeddedMessage );
        }
        public static void SetIsDeleted( IResource mail, bool isDeleted )
        {
            mail.SetProp( Core.Props.IsDeleted, isDeleted );
            IResourceList attachments = mail.GetLinksOfType( null, PROP.Attachment );

            foreach ( IResource attachment in attachments.ValidResources )
            {
                attachment.SetProp( Core.Props.IsDeleted, isDeleted );
            }
        }
        public static void Delete( IResource mail )
        {
            if ( mail == null || mail.Id == -1 ) return;
            if ( !Mail.CanBeDeleted( mail ) ) return;
            IResource folder = mail.GetLinkProp( PROP.MAPIFolder );

            if ( folder != null && Folder.IsIMAPFolder( folder ) && !Settings.IgnoreDeletedIMAPMessages )
            {
                mail.SetProp( PROP.DeletedInIMAP, true );
                Folder.LinkMail( folder, mail );
            }
            else
            {
                ForceDelete( mail );
            }
        }
        public static void ForceDelete( IResource mail )
        {
            if ( mail.Id == -1 ) return;
            Trace.WriteLine( "Deleting email resource ID=" + mail.Id );
            IResourceList resAttachments = mail.GetLinksOfType( null, PROP.Attachment );
            resAttachments.DeleteAll();
            IResourceList resInternalAttachments = mail.GetLinksOfType( null, PROP.InternalAttachment );
            resInternalAttachments.DeleteAll();
            IResourceList contactsFrom = mail.GetLinksOfType( "Contact", "From" );
            IResourceList contactsTo = mail.GetLinksOfType( "Contact", "To" );
            mail.Delete();

            Core.ContactManager.DeleteUnusedContacts( contactsFrom );
            Core.ContactManager.DeleteUnusedContacts( contactsTo );
        }
    }
    internal abstract class Folder
    {
        private static IResource _mapiFolderRoot;
        private static IResourceTreeManager _resourceTreeManager;

        static Folder()
        {
            _resourceTreeManager = ICore.Instance.ResourceTreeManager;
            _mapiFolderRoot = _resourceTreeManager.GetRootForType( STR.MAPIFolder );
        }
        public static bool IsPublicFolder( IResource folder )
        {
            return ( GetStoreSupportMask( folder ) & STORE_SUPPORT_MASK.STORE_PUBLIC_FOLDERS ) != 0;
        }
        public static int GetStoreSupportMask( IResource folder )
        {
            return folder.GetIntProp( PROP.PR_STORE_SUPPORT_MASK );
        }
        public static int GetConentCount( IResource folder )
        {
            return folder.GetIntProp( PROP.PR_CONTENT_COUNT );
        }
        public static void LinkMail( IResource folder, IResource mail )
        {
            if ( !Guard.IsResourceLive( folder ) ) return;

            if( mail.Type != STR.Email && mail.Type != STR.Task )
            {
                throw new ArgumentException( "Second parameter must be Email or Task, not " + mail.Type, "mail" );
            }
            mail.SetProp( PROP.MAPIFolder, folder );
            IResource mapiStorage = GetMAPIStorage( folder );
            mail.SetProp( PROP.OwnerStore, mapiStorage );
            DateTime date = mail.GetDateProp( Core.Props.Date );
            if ( folder.GetDateProp( PROP.LastMailDate ) < date )
            {
                folder.SetProp( PROP.LastMailDate, date );
            }

            if ( mail.Type == STR.Task )
            {
                return;
            }
            bool isDeletedItems = Folder.IsDeletedItems( folder ) || mail.HasProp( PROP.DeletedInIMAP );
            Mail.SetIsDeleted( mail, isDeletedItems );
        }
        public static bool IsParentRoot( IResource folder )
        {
            return IsRoot( GetParent( folder ) );
        }
        public static bool IsRoot( IResource folder )
        {
            return folder == _mapiFolderRoot;
        }

        public static void AddSubFolder( FolderDescriptor folder, FolderDescriptor subFolder )
        {
            IResource parentFolder = null;
            if ( folder != null )
            {
                parentFolder = Find( folder.FolderIDs.EntryId );
            }
            IResource resFolder = FindOrCreate( subFolder, parentFolder );
            if ( Folder.IsParentRoot( resFolder ) || Folder.IsParentParentRoot( resFolder ) )
            {
                SetDefault( resFolder, true );
            }
        }
        public static bool IsDeletedItems( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );
            return folder.HasProp( PROP.DeletedItemsFolder );
        }
        public static bool IsDefault( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );

            if ( Folder.IsParentRoot( folder ) )
            {
                return true;
            }
            IResource mapiStore = GetMAPIStorage( folder );
            string storeId = mapiStore.GetStringProp( PROP.StoreID );
            IResource mapiInfoStore = Core.ResourceStore.FindUniqueResource( STR.MAPIInfoStore, PROP.EntryID, storeId );
            if( mapiInfoStore == null )
            {
                throw new ApplicationException( "Folder.IsDefault -- MAPIStore resource result is null." );
            }

            IStringList list = mapiInfoStore.GetStringListProp( PROP.DefaultFolderEntryIDs );
            if( list == null )
            {
                throw new ApplicationException( "Folder.IsDefault -- List of default store folders is NULL." );
            }
            return list.IndexOf( folder.GetStringProp( PROP.EntryID ) ) != -1;
        }
        public static bool HasDeletedItemsAsAncestor( IResource folder )
        {
            IResource current = Folder.GetParent( folder );
            while ( !Folder.IsRoot( current ) )
            {
                if ( Folder.IsDeletedItems( current ) ) return true;
                current = Folder.GetParent( current );
            }
            return false;
        }
        public static void SetParent( IResource folder, IResource parentFolder )
        {
            Guard.NullArgument( folder, "folder" );
            Guard.NullArgument( parentFolder, "parentFolder" );
            folder.SetProp( Core.Props.Parent, parentFolder );
        }
        public static IResource GetParent( IResource folder )
        {
            return folder.GetLinkProp( Core.Props.Parent );
        }
        public static void SetName( IResource folder, string name )
        {
            folder.SetProp( Core.Props.Name, name );
        }
        public static void IgnoreDeletedItemsIfNoIgnoredFolders()
        {
            IResourceList ignoredFolders =
                Core.ResourceStore.FindResources( STR.MAPIFolder, PROP.IgnoredFolder, 1 );
            if ( ignoredFolders.Count == 0 )
            {
                IResourceList folders =
                    Core.ResourceStore.FindResources( STR.MAPIFolder, PROP.DeletedItemsFolder, true );
                foreach ( IResource folder in folders )
                {
                    SetIgnoredRecursive( folder );
                }
                IResourceList stores = Core.ResourceStore.FindResourcesWithProp( STR.MAPIInfoStore, PROP.JunkEmailEntryID );
                foreach ( IResource store in stores )
                {
                    string junkEmailEntryID = store.GetStringProp( PROP.JunkEmailEntryID );
                    IResource junkFolder = Core.ResourceStore.FindUniqueResource( STR.MAPIFolder, PROP.EntryID, junkEmailEntryID );
                    if ( junkFolder != null )
                    {
                        SetIgnoredRecursive( junkFolder );
                    }
                }
            }
        }
        public static bool IsIgnored( IResource folder )
        {
            if ( folder == null ) return true;
            return ( folder.GetIntProp( PROP.IgnoredFolder ) == 1 );
        }
        public static IResourceList GetIgnoredFoldersLive()
        {
            return Core.ResourceStore.FindResourcesLive( STR.MAPIFolder, PROP.IgnoredFolder, 1 );
        }
        public static IResourceList GetFolders( string type )
        {
            return Core.ResourceStore.FindResources( STR.MAPIFolder, PROP.ContainerClass, type );
        }

        public static string GetContainerClass( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );
            return folder.GetPropText( PROP.ContainerClass );
        }
        public static bool IsIMAPFolder( IResource folder )
        {
            return GetContainerClass( folder ) == FolderType.IMAP;
        }
        public static void SetIgnoreImport( IResource folder, bool ignore )
        {
            Guard.NullArgument( folder, "folder" );
            folder.SetProp( PROP.IgnoreContactImport, ignore );
        }
        public static bool IsIgnoreImport( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );
            return folder.HasProp( PROP.IgnoreContactImport );
        }
        public static bool IsFolderOfType( IResource folder, string type )
        {
            return GetContainerClass( folder ) == type;
        }
        public static IResource GetMAPIStorage( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );
            return folder.GetLinkProp( PROP.OwnerStore );
        }
        public static IResourceList GetMailListLive( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );
            return folder.GetLinksOfTypeLive( STR.Email, PROP.MAPIFolder );
        }
        public static IResourceList GetMessageList( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );
            return folder.GetLinksOfType( null, PROP.MAPIFolder );
        }
        public static IResourceList GetMailList( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );
            return folder.GetLinksOfType( STR.Email, PROP.MAPIFolder );
        }
        public static IResourceList GetTaskList( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );
            return folder.GetLinksOfType( STR.Task, PROP.MAPIFolder );
        }
        public static IResource GetSelectedMail( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );
            return folder.GetLinkProp( PROP.SelectedInFolder );
        }
        public static void SetSeeAll( IResource folder, bool seeAll )
        {
            Guard.NullArgument( folder, "folder" );
            folder.SetProp( PROP.SeeAll, seeAll );
        }
        public static bool GetSeeAll( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );
            return folder.HasProp( PROP.SeeAll );
        }
        public static IResourceList GetSubFolders( IResource folder )
        {
            Guard.NullArgument( folder, "folder" );
            return folder.GetLinksTo( STR.MAPIFolder, Core.Props.Parent );
        }

        public static bool IsAncestor( IResource folder, IResource possibleAncestor )
        {
            IResource current = Folder.GetParent( folder );
            while ( !Folder.IsRoot( current ) )
            {
                if ( current == possibleAncestor ) return true;
                current = Folder.GetParent( current );
            }
            return false;
        }
        public static void SetIgnoreImportAsync( IResource folder, string type, bool ignore )
        {
            if ( IsFolderOfType( folder, type ) )
            {
                new ResourceProxy( folder ).SetProp( PROP.IgnoreContactImport, ignore );
            }
            else if ( type == FolderType.Contact && folder.Type == STR.OutlookABDescriptor )
            {
                new ResourceProxy( folder ).SetProp( PROP.IgnoreContactImport, ignore );
            }
        }
        public static void SetSeeAllAsync( IResource folder )
        {
            new ResourceProxy( folder ).SetPropAsync( PROP.SeeAll, true );
        }
        public static void SetSeeAllAndNoIgnoreAsync( IResource folder )
        {
            ResourceProxy proxy = new ResourceProxy( folder );
            proxy.SetProp( PROP.SeeAll, true );
            proxy.SetProp( PROP.IgnoredFolder, 0 );
        }
        public static IResource FindOrCreateMAPIStore( string storeID )
        {
            IResource msgStore = FindMAPIStore( storeID );
            if ( msgStore == null )
            {
                msgStore = Core.ResourceStore.NewResource( STR.MAPIStore );
                msgStore.SetProp( PROP.StoreID, storeID );
            }
            return msgStore;
        }
        public static IResource Find( string entryID )
        {
            return Core.ResourceStore.FindUniqueResource( STR.MAPIFolder, PROP.EntryID, entryID );
        }
        public static IResource FindOrCreate( FolderDescriptor folderDescriptor, IResource parentFolder )
        {
            Guard.NullArgument( folderDescriptor, "folderDescriptor" );
            IResource MAPIStore = FindOrCreateMAPIStore( folderDescriptor.FolderIDs.StoreId );
            IResource resFolder =
                Core.ResourceStore.FindUniqueResource( STR.MAPIFolder, PROP.EntryID, folderDescriptor.FolderIDs.EntryId );
            if ( resFolder != null )
            {
                resFolder.BeginUpdate();
            }
            else
            {
                resFolder = Core.ResourceStore.BeginNewResource( STR.MAPIFolder );
                Core.WorkspaceManager.AddToActiveWorkspaceRecursive( resFolder );
                resFolder.SetProp( "EntryID", folderDescriptor.FolderIDs.EntryId );
                resFolder.SetProp( "OwnerStore", MAPIStore );
                if ( OutlookSession.IsDeletedItemsFolder( folderDescriptor.FolderIDs.EntryId ) )
                {
                    resFolder.SetProp( Core.Props.ShowDeletedItems, true );
                    resFolder.SetProp( PROP.DeletedItemsFolder, true );
                    resFolder.SetProp( PROP.DefaultDeletedItems, true );
                }
                if ( parentFolder != null )
                {
                    SetIgnored( resFolder, IsIgnored( parentFolder ) );
                }
            }
            SetName( resFolder, folderDescriptor.Name );
            string containerClass = folderDescriptor.ContainerClass;
            resFolder.SetProp( PROP.PR_STORE_SUPPORT_MASK, folderDescriptor.StoreSupportMask );
            resFolder.SetProp( PROP.PR_CONTENT_COUNT, folderDescriptor.ContentCount );
            if ( containerClass.Length > 0 )
            {
                resFolder.SetProp( PROP.ContainerClass, containerClass );
            }
            containerClass = resFolder.GetPropText( PROP.ContainerClass );
            bool visible =
                ( containerClass.Length == 0 || containerClass == FolderType.Mail ||
                containerClass == FolderType.Post || containerClass == FolderType.IMAP || containerClass == FolderType.Dav );
            resFolder.SetProp( PROP.MAPIVisible, visible );

            if ( parentFolder != null )
            {
                SetParent( resFolder, parentFolder );
            }
            else
            {
                Folder.SetAsRoot( resFolder );
            }
            resFolder.EndUpdate();
            _resourceTreeManager.SetResourceNodeSort( resFolder, STR.Name );
            return resFolder;
        }
        public static bool IsIgnored( FolderDescriptor folder )
        {
            IResource resFolder = Find( folder.FolderIDs.EntryId );
            if ( resFolder == null )
                return true;

            return IsIgnored( resFolder );
        }

        private static void DeleteFoldersRecursive( IResource folder )
        {
            foreach ( IResource subFolder in Folder.GetSubFolders( folder ).ValidResources )
            {
                DeleteFoldersRecursive( subFolder );
            }
            DeleteFolderAndMail( folder );
        }

        private static void DeleteFolderAndMail( IResource folder )
        {
            IResourceList mails = folder.GetLinksOfType( STR.Email, STR.MAPIFolder );
            Tracer._Trace( "Delete MAPIFolder resource: " + folder.DisplayName + " / res id = " + folder.Id );
            folder.Delete();
            for ( int i = mails.Count - 1; i >= 0; i-- )
            {
                Mail.ForceDelete( mails[i] );
            }
        }

        public static void DeleteFolder( IResource folder )
        {
            if ( !folder.IsDeleted && !folder.IsDeleting )
            {
                DeleteFoldersRecursive( folder );
            }
        }

        public static void DeleteFolderRecursive( IResource folder )
        {
            foreach( IResource childFolder in Folder.GetSubFolders( folder ) )
            {
                DeleteFolderRecursive( childFolder );
            }
            DeleteFolder( folder );
        }

        public static IResourceList GetDefaultDeletedItemsFolder()
        {
            return Core.ResourceStore.FindResources( STR.MAPIFolder,
                PROP.DefaultDeletedItems, true );
        }
        private static bool IsParentParentRoot( IResource folder )
        {
            return IsRoot( GetParent( GetParent( folder ) ) );
        }
        private static void SetAsRoot( IResource folder )
        {
            Folder.SetParent( folder, _mapiFolderRoot );
            if ( !folder.HasProp( Core.Props.Open ) )
            {
                folder.SetProp( Core.Props.Open, 1 );
                folder.SetProp( PROP.OpenIgnoreFolder, 1 );
                folder.SetProp( PROP.OpenSelectFolder, 1 );
            }
        }
        private static void SetDefault( IResource folder, bool defaultFolder )
        {
            folder.SetProp( PROP.DefaultFolder, defaultFolder );
        }
        private static void SetIgnoredRecursive( IResource folder )
        {
            SetIgnored( folder, true );
            foreach ( IResource subFolder in Folder.GetSubFolders( folder ) )
            {
                SetIgnoredRecursive( subFolder );
            }
        }
        private static void SetIgnored( IResource folder, bool ignored )
        {
            if ( ignored )
            {
                folder.SetProp( PROP.IgnoredFolder, 1 );
            }
            else
            {
                folder.DeleteProp( PROP.IgnoredFolder );
            }
        }
        public static IResource FindMAPIStore( string storeID )
        {
            return Core.ResourceStore.FindUniqueResource( STR.MAPIStore, PROP.StoreID, storeID );
        }
    }
}
