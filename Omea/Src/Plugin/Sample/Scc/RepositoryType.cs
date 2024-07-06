// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
	/// <summary>
	/// Summary description for RepositoryType.
	/// </summary>
	public abstract class RepositoryType
	{
        public abstract string Id { get; }
        public abstract string Name { get; }

	    /// <summary>
        /// Shows the configuration dialog for the specified repository.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="repository">The repository to configure.</param>
        /// <returns>true if the changes were saved or false if the configuration was cancelled</returns>
        public abstract bool EditRepository( IWin32Window ownerWindow, IResource repository );

        /// <summary>
        /// Runs the synchronization process for the specified repository.
        /// </summary>
        /// <param name="repository">The repository to synchronize.</param>
	    public abstract void UpdateRepository( IResource repository );
	    internal abstract string BuildFileName(IResource repository, FileChange fileChange);
	    internal abstract string BuildLinkToFile(IResource repository, FileChange fileChange);

        protected static IResource FindChangeSet( IResource repository, int number )
        {
            IResourceList existingCS = Core.ResourceStore.FindResources( Props.ChangeSetResource,
                Props.ChangeSetNumber,
                number );
            existingCS = existingCS.Intersect(
                repository.GetLinksOfType( Props.ChangeSetResource, Props.ChangeSetRepository ) );
            if ( existingCS.Count > 0 )
            {
                return existingCS [0];
            }
            return null;
        }

        protected static void SetChangeSetDescription( ResourceProxy proxy, string desc )
        {
            proxy.SetProp( Core.Props.LongBody, desc );
            desc = desc.Replace( "\r\n", " " );
            if ( desc.Length < 50 )
            {
                proxy.SetProp( Core.Props.Subject, desc );
            }
            else
            {
                proxy.SetProp( Core.Props.Subject, desc.Substring( 0, 50 ) + "..." );
            }
        }

	    /// <summary>
        /// Locates or, if necessary, creates the Omea contact for the specified Perforce
        /// user, and links the resource to it.
        /// </summary>
        /// <param name="resource">The changeset resource.</param>
        /// <param name="user">The Perforce user ID.</param>
        protected void LinkChangeSetToContact( IResource repository, IResource resource, string user )
        {
            if ( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.RunJob( "Linking changeset to contact",
                    () => LinkChangeSetToContact (repository, resource, user));
                return;
            }

            BusinessObjectList<UserToRepositoryMap> userRepMapList = Core.ResourceStore.FindResources( UserToRepositoryMap.ResourceType,
                Props.UserId, user );
            userRepMapList = userRepMapList.Intersect( repository.GetLinksOfType( UserToRepositoryMap.ResourceType,
                Props.UserRepository ) );

            if ( userRepMapList.Count == 0 )
            {
                string email, fullName;
                GetUserDetails( repository, user, out email, out fullName );
                IContact contact = Core.ContactManager.FindOrCreateContact( email, fullName );
                IResource contactRes = contact.Resource;

                UserToRepositoryMap userRepMap = UserToRepositoryMap.Create();
                userRepMap.UserId = user;
                userRepMap.UserRepository = repository;
                userRepMap.UserContact = contactRes;
                userRepMap.Save();

                Core.ContactManager.LinkContactToResource( Core.ContactManager.Props.LinkFrom,
                    contactRes, resource, email, fullName );
            }
            else
            {
                IResource contactRes = userRepMapList [0].UserContact;
                IContact contact = Core.ContactManager.GetContact( contactRes );
                Core.ContactManager.LinkContactToResource( Core.ContactManager.Props.LinkFrom,
                    contactRes, resource, contact.DefaultEmailAddress, contactRes.DisplayName );
            }
        }

	    protected abstract void GetUserDetails( IResource repository, string user, out string email, out string fullName );

	    /// <summary>
	    /// Returns a resource for the folder with the specified path.
	    /// </summary>
	    /// <param name="repository">The repository for which the folder is returned.</param>
	    /// <param name="dir">The path to the folder.</param>
	    /// <returns>The folder resource.</returns>
	    internal static IResource FindOrCreateFolder( IResource repository, string dir )
	    {
	        return FindOrCreateFolder( repository, dir, true );
	    }

	    /// <summary>
	    /// Returns an existing resource for the folder with the specified path.
	    /// </summary>
        /// <param name="repository">The repository for which the folder is returned.</param>
        /// <param name="dir">The path to the folder.</param>
        /// <returns>The folder resource.</returns>
        internal static IResource FindFolder( IResource repository, string dir )
	    {
	        return FindOrCreateFolder(repository, dir, false);
	    }

	    /// <summary>
        /// Returns a resource for the P4 or Subversion folder with the specified path.
        /// </summary>
        /// <param name="dir">The path to the folder.</param>
        /// <param name="canCreate">If true, the folder is created if it does not exist. If false,
        /// only existing folders are returned.</param>
        /// <returns>The folder resource.</returns>
        private static IResource FindOrCreateFolder( IResource repository, string dir, bool canCreate )
        {
            // Example: //Root/folder/folder2
	        int pos = dir.LastIndexOf( '/' );
            IResource parent;
            if ( pos <= 1 )
            {
                parent = repository;
            }
            else
            {
                parent = FindOrCreateFolder( repository, dir.Substring( 0, pos ), canCreate );
            }
            string folderName = dir.Substring( pos+1 );

            foreach( Folder child in parent.GetLinksTo( Folder.ResourceType, Core.PropIds.Parent) )
            {
                if ( String.Compare( child.Name, folderName, true ) == 0 )
                {
                    return child.Resource;
                }
            }
            if ( canCreate )
            {
                Folder folder = Folder.Create();
                folder.Name = folderName;
                folder.Parent = parent;
                folder.Save();
                return folder.Resource;
            }
	        return null;
        }

        protected static string BuildPath( IResource resource )
        {
            IResource parent = resource.GetLinkProp( Core.Props.Parent );
            if ( parent.Type == Props.FolderResource )
            {
                return BuildPath( parent ) + "/" + resource.GetStringProp( Core.Props.Name );
            }
            return "/" + resource.GetStringProp( Core.Props.Name );
        }

        /// <summary>
        /// Called when a changeset is selected in the repository.
        /// </summary>
        /// <param name="repository">The repository to which the file change belongs.</param>
        /// <param name="changeset">The selected changeset.</param>
        public virtual void OnChangesetSelected( IResource repository, IResource changeset )
        {
        }

        /// <summary>
	    /// Called when a file change is selected in the repository.
	    /// </summary>
	    /// <param name="repository">The repository to which the file change belongs.</param>
	    /// <param name="fileChange">The selected file change.</param>
	    /// <returns>The text to display in the diff preview pane.</returns>
	    internal virtual string OnFileChangeSelected(IResource repository, FileChange fileChange)
	    {
	        return null;
	    }

        /// <summary>
        /// Clears the last error message for the specified repository.
        /// </summary>
        /// <param name="repository">The repository for which the error message is cleaned.</param>
        internal static void ClearLastError( IResource repository )
        {
            if ( repository.HasProp( Props.LastError ) )
            {
                new ResourceProxy( repository ).DeletePropAsync( Props.LastError );
            }
        }

        /// <summary>
        /// Sets the last error message for the specified repository.
        /// </summary>
        /// <param name="repository">The repository for which the error message is set.</param>
        /// <param name="ex">The exception from which the error message text is taken.</param>
        internal static void SetLastError( IResource repository, RunnerException ex )
        {
            string error = ex.Message.Replace( '\n', ' ' ).Replace( '\r', ' ' ).Replace( '\t', ' ' );
            while( error.IndexOf( "  " ) >= 0 )
            {
                error = error.Replace( "  ", " " );
            }
            new ResourceProxy( repository ).SetPropAsync( Props.LastError, error );
        }
	}
}
