// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Workspaces
{
    public class WorkspacesDialogAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
        	WorkspaceButtonsManager.GetInstance().ShowWorkspacesDialog( null );
        }
    }

    /**
     * Action to add the selected resources to a workspace.
     */

    public class AddToWorkspaceAction: IAction
    {
        private IResourceList _allWorkspaces;

        public void Execute( IActionContext context )
        {
            IResourceList selection = context.SelectedResources;
            StringBuilder captionBuilder = new StringBuilder( "Add " );
            if ( selection.Count > 1 )
            {
                captionBuilder.Append( selection.Count );
                captionBuilder.Append( " selected resources" );
            }
            else
            {
                captionBuilder.Append( "'" );
                captionBuilder.Append( selection [0].DisplayName );
                captionBuilder.Append( "'" );
            }
            captionBuilder.Append( " to Workspace" );

            IResource workspace = Core.UIManager.SelectResource( "Workspace", captionBuilder.ToString() );
            if ( workspace != null )
            {
                AddResourcesToWorkspace( context.SelectedResources, workspace );
            }
        }

        public static void AddResourcesToWorkspace( IResourceList resList, IResource workspace )
        {
            ArrayList skippedResources = new ArrayList();
            foreach( IResource res in resList )
            {
                if ( Core.WorkspaceManager.IsInWorkspaceRecursive( workspace, res ) )
                {
                    skippedResources.Add( res );
                    continue;
                }
                if ( Core.WorkspaceManager.GetWorkspaceResourceType( res.Type ) == WorkspaceResourceType.Folder )
                {
                    Core.WorkspaceManager.AddResourceToWorkspaceRecursive( workspace, res );
                }
                else
                {
                    Core.WorkspaceManager.AddResourceToWorkspace( workspace, res );
                }
            }

            ReportSkippedResources( Core.MainWindow, skippedResources );
        }

        public static void ReportSkippedResources( IWin32Window ownerWindow, ArrayList skippedResources )
        {
            if ( skippedResources.Count == 1 )
            {
                MessageBox.Show( ownerWindow,
                                 "The resource '" + (IResource) skippedResources [0] +
                                     "' was not added to the workspace because it belongs to a tree of resources recursively added to the workspace.",
                                 "Add to Workspace", MessageBoxButtons.OK, MessageBoxIcon.Information );
            }
            else if ( skippedResources.Count > 1 )
            {
                MessageBox.Show( ownerWindow,
                                 skippedResources.Count +
                                     " resources were not added to the workspace because they belong to a tree of resources recursively added to the workspace.",
                                 "Add to Workspace", MessageBoxButtons.OK, MessageBoxIcon.Information );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            bool enabled = (context.SelectedResources.Count > 0);
            if ( enabled )
            {
                foreach( IResource res in context.SelectedResources )
                {
                    if ( Core.WorkspaceManager.GetWorkspaceResourceType( res.Type ) == WorkspaceResourceType.None &&
                        Core.ResourceStore.ResourceTypes [res.Type].HasFlag( ResourceTypeFlags.Internal) )
                    {
                        enabled = false;
                        break;
                    }
                }
            }

            if ( _allWorkspaces == null )
            {
                _allWorkspaces = Core.WorkspaceManager.GetAllWorkspaces();
            }
            if ( _allWorkspaces.Count == 0 )
            {
                enabled = false;
            }

            if ( !enabled )
            {
                if ( context.Kind == ActionContextKind.MainMenu )
                {
                    presentation.Enabled = false;
                }
                else
                {
                    presentation.Visible = false;
                }
            }
        }
    }

    /// <summary>
    /// Action shown in the link context menu to remove a resource from a workspace.
    /// </summary>
    public class RemoveFromWorkspaceAction: IAction
    {
        public void Execute( IActionContext context )
        {
            if ( context.LinkTargetResource != null )
            {
                Core.WorkspaceManager.RemoveResourceFromWorkspace( context.SelectedResources [0],
                    context.LinkTargetResource );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count != 1 || context.LinkTargetResource == null )
            {
                presentation.Visible = false;
            }
        }
    }

    public class ActivateWorkspaceAction: IAction
    {
        private IResource _workspace;

        public ActivateWorkspaceAction( IResource workspace )
        {
            _workspace = workspace;
        }

        public void Execute( IActionContext context )
        {
            Core.WorkspaceManager.ActiveWorkspace = _workspace;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = (Core.WorkspaceManager.ActiveWorkspace == _workspace );
        }
    }

    public class NextWorkspaceAction: IAction
    {
        public void Execute( IActionContext context )
        {
        	WorkspaceButtonsManager.GetInstance().ActivateNextWorkspace( true );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = WorkspaceButtonsManager.GetInstance().CanActivateNextWorkspace();
        }
    }

    public class PrevWorkspaceAction: IAction
    {
        public void Execute( IActionContext context )
        {
        	WorkspaceButtonsManager.GetInstance().ActivateNextWorkspace( false );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = WorkspaceButtonsManager.GetInstance().CanActivateNextWorkspace();
        }
    }

    public class WorkspaceLinkClickAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Core.WorkspaceManager.ActiveWorkspace = context.SelectedResources [0];
            if ( context.LinkTargetResource != null )
            {
                Core.UIManager.DisplayResourceInContext( context.LinkTargetResource );
            }
        }
    }
}
