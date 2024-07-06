// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Web;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
	/// <summary>
	/// Perforce repository type.
	/// </summary>
	public class P4RepositoryType: RepositoryType
	{
        private readonly Dictionary<string, string> _contactNameCache = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _contactEmailCache = new Dictionary<string, string>();

	    public override string Id
	    {
	        get { return "P4"; }
	    }

	    public override string Name
	    {
	        get { return "Perforce"; }
	    }

	    public override bool EditRepository( IWin32Window ownerWindow, IResource repository )
	    {
	        using( var dlg = new P4RepositoryOptions() )
	        {
	            return dlg.EditRepository( ownerWindow, repository ) == DialogResult.OK;
	        }
	    }

	    public override void UpdateRepository( IResource repository )
	    {
            try
            {
                if ( repository.HasProp( Props.LastRevision ) )
                {
                    int lastChangeSet = repository.GetProp( Props.LastRevision );
                    ChangeSetSummary[] summaries = CreateRunner( repository ).GetChangeSetsAfter( lastChangeSet, GetPathsToWatch( repository ) );
                    CreateChangeSetResources( repository, summaries );
                }
                else
                {
                    CreateChangeSetResources( repository, CreateRunner( repository ).GetLastChangeSets( Settings.ChangeSetsToIndex ) );
                }
                ClearLastError( repository );
            }
            catch ( RunnerException ex )
            {
                SetLastError( repository, ex );
            }

            DescribeNewChangeSets( repository );
        }

	    /// <summary>
	    /// Creates an instance of the Perforce command runner for the specified repository.
	    /// </summary>
	    /// <param name="repository">The repository for which the runner is requested.</param>
	    /// <returns>The runner instance.</returns>
	    private P4Runner CreateRunner( IResource repository )
	    {
	        return new P4Runner( repository.GetProp( Props.P4ServerPort ),
	                             repository.GetProp( Props.P4Client ),
	                             repository.GetProp( Props.UserName ),
	                             repository.GetProp( Props.Password ) );
	    }

	    /// <summary>
        /// Creates resources from the specified array of change set descriptors.
        /// </summary>
        /// <param name="changeSets">The change sets to be converted to resources.</param>
        private void CreateChangeSetResources( IResource repository, ChangeSetSummary[] changeSets )
        {
            string[] ignoredClients = repository.GetProp( Props.P4IgnoreChanges ).Split( ';' );

            foreach( ChangeSetSummary changeSet in changeSets )
            {
                if ( Array.IndexOf( ignoredClients, changeSet.Client ) >= 0 )
                {
                    continue;
                }

                if ( FindChangeSet( repository, changeSet.Number ) != null )
                {
                    continue;
                }

                ResourceProxy proxy = ResourceProxy.BeginNewResource( Props.ChangeSetResource );
                proxy.SetProp( Props.ChangeSetNumber, changeSet.Number );
                proxy.SetProp( Core.Props.Date, changeSet.Date );
                proxy.SetProp( Props.P4Client, changeSet.Client );
                proxy.SetProp( Core.Props.IsUnread, true );
                proxy.AddLink( Props.ChangeSetRepository, repository );
                proxy.EndUpdate();
                LinkChangeSetToContact( repository, proxy.Resource, changeSet.User );

                // Execute rules for the new changeset
                Core.FilterEngine.ExecRules( StandardEvents.ResourceReceived, proxy.Resource );

                // Request text indexing of the changeset
                Core.TextIndexManager.QueryIndexing( proxy.Resource.Id );

                if ( changeSet.Number > repository.GetProp( Props.LastRevision ) )
                {
                    new ResourceProxy( repository ).SetPropAsync( Props.LastRevision, changeSet.Number );
                }
            }
        }

	    /// <summary>
        /// Initiates the procedure for fetching the descriptions of all change sets which
        /// don't currently have known descriptions.
        /// </summary>
        private void DescribeNewChangeSets( IResource repository )
        {
            IResourceList changeSets = repository.GetLinksOfType( Props.ChangeSetResource, Props.ChangeSetRepository );
            changeSets = changeSets.Minus( Core.ResourceStore.FindResourcesWithProp( Props.ChangeSetResource, Core.Props.Subject ) );
            if ( changeSets.Count > 0 )
            {
                Core.NetworkAP.QueueJob( "Describing Perforce changeset",
                    () => DescribeChangeSets(repository, changeSets, 0 ));
            }
        }

        /// <summary>
        /// Performs one step of the procedure to fetch the descriptions of change sets
        /// and queues the next step of it.
        /// </summary>
        private void DescribeChangeSets( IResource repository, IResourceList changeSets, int index )
        {
            IResource changeSet = Core.ResourceStore.TryLoadResource( changeSets.ResourceIds [index] );
            if ( changeSet != null )
            {
                DescribeChangeSet( repository, changeSet.GetProp( Props.ChangeSetNumber ) );
            }

            if ( index < changeSets.Count-1 )
            {
                Core.NetworkAP.QueueJob("Describing Perforce changeset",
                                        () => DescribeChangeSets(repository, changeSets, index + 1));
            }
        }

        /// <summary>
        /// Gets, parses and stores the details of a single change set.
        /// </summary>
        /// <param name="repository">The repository for which the change set is described.</param>
        /// <param name="changeSetNumber">The number of the change set to describe.</param>
        private void DescribeChangeSet( IResource repository, int changeSetNumber )
        {
            IResource changeSet = FindChangeSet( repository, changeSetNumber );
            if ( changeSet == null )
            {
                // the changeset may have been deleted when it was detected to belong to an ignored path
                return;
            }

            // avoid duplicate fetching of changeset description
            if ( changeSet.HasProp( Props.Change ) )
            {
                return;
            }

            SccPlugin.StatusWriter.ShowStatus( "Fetching details for changeset " + changeSetNumber + "..." );

            ChangeSetDetails details;
            try
            {
                details = CreateRunner( repository ).DescribeChangeSet( changeSetNumber );
                ClearLastError( repository);
            }
            catch( RunnerException ex )
            {
                SetLastError( repository, ex );
                SccPlugin.StatusWriter.ClearStatus();
                return;
            }

            if ( IsIgnoredChangeSet( repository, details ) )
            {
                new ResourceProxy( changeSet ).DeleteAsync();
                SccPlugin.StatusWriter.ClearStatus();
                return;
            }

            Core.ResourceAP.QueueJob( "Saving changeset", () => SaveChangeSet(repository, changeSet, details ));

            SccPlugin.StatusWriter.ClearStatus();
        }

        private static void SaveChangeSet( IResource repository, IResource changeSet, ChangeSetDetails details )
        {
            if ( changeSet.HasProp( Props.Change ) )
            {
                return;
            }

            ResourceProxy proxy = new ResourceProxy( changeSet );
            proxy.BeginUpdate();

            SetChangeSetDescription( proxy, details.Description );

            foreach( var fileChangeData in details.FileChanges )
            {
                int pos = fileChangeData.Path.LastIndexOf( '/' );
                string folderName = fileChangeData.Path.Substring( 0, pos );
                string fileName = fileChangeData.Path.Substring( pos+1 );

                IResource folder = FindOrCreateFolder( repository, folderName );
                proxy.AddLink( Props.AffectsFolder, folder );

                var fileChange = FileChange.Create();
                fileChange.Name = fileName;
                fileChange.AffectsFolder = folder;
                fileChange.ChangeType = fileChangeData.ChangeType;
                fileChange.Revision = fileChangeData.Revision;
                fileChange.Diff = fileChangeData.Diff;
                fileChange.Binary = fileChangeData.Binary;
                fileChange.Save();

                proxy.AddLink( Props.Change, fileChange.Resource );
            }

            proxy.EndUpdate();
            Core.TextIndexManager.QueryIndexing( proxy.Resource.Id );
        }

	    public override void OnChangesetSelected( IResource repository, IResource changeset )
	    {
	        if ( !changeset.HasProp( Props.Change ) )
	        {
	            Core.NetworkAP.QueueJob( JobPriority.Immediate,
                    "Describing Perforce changeset",
                    () => DescribeChangeSets( repository, changeset.ToResourceList(), 0 ) );
	        }
	    }

	    /// <summary>
        /// Checks if at least one file in the specified changeset belongs to a watched
        /// project.
        /// </summary>
        /// <param name="details">The changeset details.</param>
        /// <returns>true if none of the files in the changeset belongs to a watched project.</returns>
        private static bool IsIgnoredChangeSet( IResource repository, ChangeSetDetails details )
        {
	        string[] pathsToWatch = GetPathsToWatch(repository);
	        if ( pathsToWatch.Length == 0 )
            {
                return false;
            }

            foreach( FileChangeData fileChange in details.FileChanges )
            {
                string fileChangePath = fileChange.Path.ToLower();
                foreach( string path in pathsToWatch )
                {
                    if ( fileChangePath.StartsWith( path.ToLower() ) )
                    {
                        return false;
                    }
                }
            }
            return true;
        }

	    private static string[] GetPathsToWatch(IResource repository)
	    {
	        return repository.GetProp( Props.PathsToWatch ).Split( ';' );
	    }

	    /// <summary>
	    /// Returns the cached details for the specified user, or runs 'p4 user' to
	    /// get the details if no cached details exist.
	    /// </summary>
	    /// <param name="userName">The user name of the user.</param>
	    /// <param name="email">The e-mail address of the user.</param>
	    /// <param name="fullName">The full name of the user.</param>
        protected override void GetUserDetails( IResource repository, string userName, out string email, out string fullName )
        {
            string cacheKey = repository.Id + ":" + userName;
            email = _contactEmailCache [cacheKey];
            fullName = _contactNameCache [cacheKey];
            if ( email == null && fullName == null )
            {
                try
                {
                    CreateRunner( repository ).DescribeUser( userName, out email, out fullName );
                    ClearLastError( repository);
                }
                catch( RunnerException ex )
                {
                    SetLastError( repository, ex );
                }

                _contactEmailCache [cacheKey] = email;
                _contactNameCache [cacheKey] = fullName;
            }
        }

        internal override string BuildLinkToFile(IResource repository, FileChange fileChange)
        {
            string url = repository.GetProp( Props.P4WebUrl );
            if ( url.Length == 0 )
            {
                return null;
            }
            if ( url.IndexOf( "://" ) < 0 )
            {
                url = "http://" + url;
            }
            if ( !url.EndsWith( "/" ) )
            {
                url += "/";
            }

            IResource changeSet = fileChange.Resource.GetReverseLinkProp( Props.Change );
            string fileName = "/" + BuildPath( fileChange.GetProp( Props.AffectsFolder ) ) + "/" +
                              fileChange.Name;
            int csNumber = changeSet.GetProp( Props.ChangeSetNumber );
            int revision = fileChange.GetProp( Props.Revision );
            url += "@md=c&sr=" + csNumber + "@" + HttpUtility.UrlEncode( fileName ) + '#' + revision;
            return url;
        }

        internal override string BuildFileName(IResource repository, FileChange fileChange)
        {
            return "/" + BuildPath( fileChange.AffectsFolder ) + "/" + fileChange.Name;
        }
	}
}
