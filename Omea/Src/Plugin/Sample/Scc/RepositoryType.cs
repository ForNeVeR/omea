/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
	    public abstract string BuildFileName( IResource repository, IResource fileChange );
	    public abstract string BuildLinkToFile( IResource repository, IResource fileChange );

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

        private delegate void LinkChangeSetToContactDelegate( IResource repository, IResource resource, string user );

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
                Core.ResourceAP.RunJob( new LinkChangeSetToContactDelegate( LinkChangeSetToContact ),
                    repository, resource, user );
                return;
            }
            
            IResourceList userRepMapList = Core.ResourceStore.FindResources( Props.UserToRepositoryMapResource,
                Props.UserId, user );
            userRepMapList = userRepMapList.Intersect( repository.GetLinksOfType( Props.UserToRepositoryMapResource,
                Props.UserRepository ) );
            
            if ( userRepMapList.Count == 0 )
            {
                string email, fullName;
                GetUserDetails( repository, user, out email, out fullName );
                IContact contact = Core.ContactManager.FindOrCreateContact( email, fullName );
                IResource contactRes = contact.Resource;
                
                IResource userRepMap = Core.ResourceStore.BeginNewResource( Props.UserToRepositoryMapResource );
                userRepMap.SetProp( Props.UserId, user );
                userRepMap.AddLink( Props.UserRepository, repository );
                userRepMap.AddLink( Props.UserContact, contactRes );
                userRepMap.EndUpdate();

                Core.ContactManager.LinkContactToResource( Core.ContactManager.Props.LinkFrom,
                    contactRes, resource, email, fullName );
            }
            else
            {
                IResource contactRes = userRepMapList [0].GetLinkProp( Props.UserContact );
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
            string folderName;
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
            folderName = dir.Substring( pos+1 );

            foreach( IResource child in parent.GetLinksTo( Props.FolderResource, Core.Props.Parent ) )
            {
                if ( String.Compare( child.GetStringProp( Core.Props.Name ), folderName, true ) == 0 )
                {
                    return child;
                }
            }
            if ( canCreate )
            {
                ResourceProxy proxy = ResourceProxy.BeginNewResource( Props.FolderResource );
                proxy.SetProp( Core.Props.Name, folderName );
                proxy.AddLink( Core.Props.Parent, parent );
                proxy.EndUpdate();
                return proxy.Resource;
            }
	        return null;
        }

        protected string BuildPath( IResource resource )
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
	    public virtual string OnFileChangeSelected( IResource repository, IResource fileChange )
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
