// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
    /// <summary>
    /// Action to start the synchronization process for all available repositories
    /// </summary>
    public class SynchronizeRepositoriesAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            SccPlugin.SynchronizeRepositories();
        }
    }

    /// <summary>
    /// Action to start the synchronization process for a single repository
    /// </summary>
    public class SynchronizeRepositoryAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            foreach( IResource res in context.SelectedResources )
            {
                RepositoryType repType = SccPlugin.GetRepositoryType( res );
                if ( repType != null )
                {
                    repType.UpdateRepository( res );
                }
            }
        }
    }

    /// <summary>
    /// Action to edit the properties of the selected repository.
    /// </summary>
    public class RepositoryPropertiesAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource repository = context.SelectedResources[0];
            RepositoryType repType = SccPlugin.GetRepositoryType( repository );
            repType.EditRepository( Core.MainWindow, repository );
        }
    }

    /// <summary>
    /// Action to delete the specified repository
    /// </summary>
    public class DeleteRepositoryAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            DeleteRepository( Core.MainWindow, context.SelectedResources[0], true );
        }

        public static bool DeleteRepository( IWin32Window ownerWindow, IResource repository, bool async )
        {
            DialogResult dr =
                MessageBox.Show( ownerWindow, "Do you want to delete the repository '" + repository.DisplayName +
                "' and all cached content stored for it?",
                "Delete Repository", MessageBoxButtons.YesNo, MessageBoxIcon.Question );
            if ( dr == DialogResult.Yes )
            {
                if ( async )
                {
                    Core.ResourceAP.QueueJob( JobPriority.Immediate, "Deleting Perforce repository",
                                              new ResourceDelegate( DoDeleteRepository ), repository );
                }
                else
                {
                    Core.UIManager.RunWithProgressWindow( "Deleting Repository",
                                                          () => RunDeleteRepository(repository));
                }
                return true;
            }
            return false;
        }

        private static void RunDeleteRepository( IResource res )
        {
            Core.ResourceAP.RunJob("Deleting SCC repository", () => DoDeleteRepository(res));
        }

        private static void DoDeleteRepository( IResource res )
        {
            IResourceList changeSets = res.GetLinksOfType( Props.ChangeSetResource, Props.ChangeSetRepository );
            int count = 0, percent = -1;
            foreach( IResource changeset in changeSets )
            {
                count++;
                int newPercent = count*100/changeSets.Count;
                if ( newPercent != percent && Core.ProgressWindow != null )
                {
                    Core.ProgressWindow.UpdateProgress( newPercent, "Deleting changesets...", "" );
                }
                DeleteChangeSet( changeset );
            }
            if ( Core.ProgressWindow != null )
            {
                Core.ProgressWindow.UpdateProgress( 0, "Deleting folders...", "" );
            }
            foreach( IResource folder in res.GetLinksTo( Props.FolderResource, Core.Props.Parent ) )
            {
                DeleteFolderRecursive( folder );
            }
            if ( Core.ProgressWindow != null )
            {
                Core.ProgressWindow.UpdateProgress( 0, "Deleting users...", "" );
            }
            res.GetLinksOfType( Props.UserToRepositoryMapResource, Props.UserRepository ).DeleteAll();
            res.Delete();
        }

        private static void DeleteFolderRecursive( IResource folder )
        {
            foreach( IResource child in folder.GetLinksTo( Props.FolderResource, Core.Props.Parent ) )
            {
                DeleteFolderRecursive( child );
            }
            folder.Delete();
        }

        internal static void DeleteChangeSet( IResource changeset )
        {
            changeset.GetLinksOfType( Props.FileChangeResource, Props.Change ).DeleteAll();
            changeset.Delete();
        }
    }

    public class ToggleShowSubfolderContentsAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            bool anyChecked = HasAnyCheckedResource(context);
            foreach( IResource res in context.SelectedResources )
            {
                new ResourceProxy( res ).SetProp( Props.ShowSubfolderContents, !anyChecked );
            }
            if ( context.SelectedResources.Contains( Core.ResourceBrowser.OwnerResource ) )
            {
                // redisplay the contents
                SccPlugin.FolderTreePane.SelectResource( Core.ResourceBrowser.OwnerResource );
            }
        }

        private static bool HasAnyCheckedResource(IActionContext context)
        {
            return context.SelectedResources.Find(res => res.HasProp(Props.ShowSubfolderContents)) != null;
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( presentation.Visible )
            {
                presentation.Checked = HasAnyCheckedResource(context);
            }
        }
    }
}
