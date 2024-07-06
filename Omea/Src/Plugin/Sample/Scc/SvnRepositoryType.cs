// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
	/// <summary>
	/// Subversion repository.
	/// </summary>
	public class SvnRepositoryType: RepositoryType
	{
	    public override string Id
	    {
	        get { return "SVN"; }
	    }

	    public override string Name
	    {
	        get { return "Subversion"; }
	    }

        public override bool EditRepository( IWin32Window ownerWindow, IResource repository )
        {
            using( SvnRepositoryOptions dlg = new SvnRepositoryOptions() )
            {
                return dlg.EditRepository( ownerWindow, repository ) == DialogResult.OK;
            }
        }

        public override void UpdateRepository( IResource repository )
        {
            int startRevision, lastRevision;
            if ( repository.HasProp( Props.LastRevision ) )
            {
                startRevision = repository.GetProp( Props.LastRevision );
                lastRevision = -1;
            }
            else
            {
                try
                {
                    lastRevision = GetRunner( repository ).GetLastRevision();
                    ClearLastError( repository );
                }
                catch( RunnerException ex )
                {
                    SetLastError( repository, ex );
                    return;
                }
                if ( lastRevision <= Settings.ChangeSetsToIndex )
                {
                    startRevision = 1;
                }
                else
                {
                    startRevision = lastRevision - Settings.ChangeSetsToIndex;
                }
            }

            ParseSvnLog( repository, startRevision, lastRevision );
        }

	    private static SvnRunner GetRunner( IResource repository )
	    {
	        return new SvnRunner( repository.GetProp( Props.RepositoryUrl ),
	                              repository.GetProp( Props.UserName ),
	                              repository.GetProp( Props.Password ) );
	    }

	    private void ParseSvnLog( IResource repository, int startRevision, int lastRevision )
	    {
	        string log;
	        try
	        {
	            log = GetRunner( repository ).GetXmlLog( startRevision, lastRevision );
	            ClearLastError( repository );
	        }
	        catch( RunnerException ex )
	        {
	            SetLastError( repository, ex  );
	            return;
	        }
            XmlDocument doc = new XmlDocument();
            doc.Load( new StringReader( log ) );
            foreach( XmlElement node in doc.SelectNodes( "//logentry" ) )
            {
                int revision = Int32.Parse( node.GetAttribute( "revision" ) );
                if ( FindChangeSet( repository, revision ) != null )
                {
                    continue;
                }

                string author = "", description = "";
                DateTime date = DateTime.Now;
                XmlNode childNode = node.SelectSingleNode( "author" );
                if ( childNode != null )
                {
                    author = childNode.InnerText;
                }
                childNode = node.SelectSingleNode( "msg" );
                if ( childNode != null )
                {
                    description = childNode.InnerText;
                }
                childNode = node.SelectSingleNode( "date" );
                if ( childNode != null )
                {
                    date = DateTime.Parse( childNode.InnerText );
                }

                ResourceProxy proxy = ResourceProxy.BeginNewResource( Props.ChangeSetResource );
                proxy.SetProp( Props.ChangeSetNumber, revision );
                proxy.SetProp( Core.Props.Date, date );
                proxy.SetProp( Core.Props.IsUnread, true );
                SetChangeSetDescription( proxy, description );
                proxy.AddLink( Props.ChangeSetRepository, repository );
                ProcessFileChanges( repository, proxy, node, revision );
                proxy.EndUpdate();

                LinkChangeSetToContact( repository, proxy.Resource, author );

                // Execute rules for the new changeset
                Core.FilterEngine.ExecRules( StandardEvents.ResourceReceived, proxy.Resource );

                // Request text indexing of the changeset
                Core.TextIndexManager.QueryIndexing( proxy.Resource.Id );

                if ( revision > repository.GetProp( Props.LastRevision ) )
                {
                    new ResourceProxy( repository ).SetPropAsync( Props.LastRevision, revision );
                }
            }
        }

	    private static void ProcessFileChanges( IResource repository, ResourceProxy csProxy, XmlElement node, int revision )
	    {
	        foreach( XmlElement pathNode in node.SelectNodes( "paths/path" ) )
	        {
	            string action = pathNode.GetAttribute( "action" );
	            string path = pathNode.InnerText;

                int pos = path.LastIndexOf( '/' );
                string folderName = path.Substring( 0, pos );
                string fileName = path.Substring( pos+1 );

                IResource folder = FindOrCreateFolder( repository, folderName );
                csProxy.AddLink( Props.AffectsFolder, folder );

	            FileChange fileChange = FileChange.Create();
                fileChange.Name = fileName;
                fileChange.AffectsFolder = folder;
                switch( action )
	            {
                    case "A":
	                    fileChange.ChangeType = "add";
	                    break;
                    case "D":
                        fileChange.ChangeType = "delete";
                        break;
                    case "R":
                        fileChange.ChangeType = "replace";
                        break;
                    default:
                        fileChange.ChangeType = "change";
                        break;
                }
                fileChange.Revision = revision;
                fileChange.Save();

                csProxy.AddLink( Props.Change, fileChange.Resource );

	        }
	    }

	    protected override void GetUserDetails( IResource repository, string userName,
	                                            out string email, out string fullName )
	    {
	        email = null;
	        fullName = userName;
	    }

	    internal override string BuildFileName(IResource repository, FileChange fileChange)
	    {
	        return BuildPath( fileChange.AffectsFolder ) + "/" + fileChange.Name;
	    }

	    internal override string BuildLinkToFile(IResource repository, FileChange fileChange)
	    {
	        string url = repository.GetProp( Props.RepositoryUrl );
	        if ( url.StartsWith( "http://" ) )
	        {
                if ( url.EndsWith( "/" ) )
                {
                    url = url.Substring( 0, url.Length - 1 );
                }
                return url + BuildPath( fileChange.AffectsFolder ) + "/" + fileChange.Name;
            }
	        return null;
	    }

	    internal override string OnFileChangeSelected(IResource repository, FileChange fileChange)
        {
            if ( !fileChange.HasProp( Props.Diff ) )
            {
                Core.NetworkAP.QueueJob( JobPriority.Immediate, "Loading Subversion diff",
                    () => GetDiff(repository, fileChange));
                return "Loading diff...";
            }
	        return null;
        }

	    private void GetDiff( IResource repository, FileChange fileChange )
	    {
	        if ( fileChange.HasProp( Props.Diff ) )
	        {
	            return;
	        }

	        int revision = fileChange.Revision;
	        if ( revision == 1 )
	        {
	            return;
            }

	        string repoRoot = repository.GetProp( Props.RepositoryRoot );
	        if ( repoRoot == null )
	        {
	            try
	            {
                    repoRoot = GetRunner( repository ).GetSvnInfo( "Repository Root:" );
	                ClearLastError( repository );
	            }
	            catch( RunnerException ex )
	            {
	                SetLastError( repository, ex );
	                return;
	            }
	            new ResourceProxy( repository ).SetPropAsync( Props.RepositoryRoot, repoRoot );
	        }

	        string repoPath = BuildFileName( repository, fileChange );
	        SvnRunner svnRunner = GetRunner( repository );
	        svnRunner.RepositoryUrl = repoRoot;
	        try
	        {
                string diff = svnRunner.GetDiff( repoPath, revision - 1, revision );
                fileChange.Diff = diff;
	            fileChange.SaveAsync();
	            if ( diff == "" )
	            {
	                CheckBinaryFile( repository, repoRoot, repoPath, fileChange );
	            }

	            ClearLastError( repository );
	        }
	        catch( RunnerException ex )
	        {
	            SetLastError( repository, ex );
	        }
	    }

	    private static void CheckBinaryFile( IResource repository, string repoRoot, string repoPath, FileChange fileChange )
	    {
	        SvnRunner svnRunner = GetRunner( repository );
	        svnRunner.RepositoryUrl = repoRoot;
	        try
	        {
	            string prop = svnRunner.GetProperty( repoPath, "svn:mime-type" );
	            ClearLastError( repository );
	            // see libsvn_subr/validate.c:svn_mime_type_is_binary() in Subversion source code
	            if ( !prop.StartsWith( "text/" ) && !prop.StartsWith( "image/x-xbitmap" ) &&
	                !prop.StartsWith( "image/x-xpixmap" ) )
	            {
                    fileChange.Binary = true;
	                fileChange.SaveAsync();
	            }
	        }
	        catch( RunnerException ex )
	        {
	            SetLastError( repository, ex );
	        }
	    }
	}
}
