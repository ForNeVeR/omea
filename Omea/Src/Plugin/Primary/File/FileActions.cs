// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.FilePlugin
{
    public abstract class FileAction : ActionOnResource
    {
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                IResourceList resources = context.SelectedResources;
                for( int i = 0; i < resources.Count; ++i )
                {
                    IResource res = resources[ i ];
                    if( res.Type != FileProxy._folderResourceType )
                    {
                        IResource folder = res.GetLinkProp( FileProxy._propParentFolder );
                        if( folder == null || folder.Type != FileProxy._folderResourceType )
                        {
                            presentation.Visible = false;
                            break;
                        }
                    }
                }
            }
        }
    }

    /**
     * opens a file
     */
    public class FileOpenAction : FileAction
    {
        public override void Execute( IActionContext context )
        {
            IResourceList resources = context.SelectedResources;
            for( int i = 0; i < resources.Count; ++i )
            {
                Core.FileResourceManager.OpenSourceFile( resources[ i ] );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                IResourceList resources = context.SelectedResources;
                for( int i = 0; i < resources.Count; ++i )
                {
                    if( resources[ i ].Type == FileProxy._folderResourceType )
                    {
                        presentation.Visible = false;
                        break;
                    }
                }
            }
        }
    }

    /**
     * opens file's directory in windows browser
     */
    public class LocateOnDiskAction : FileAction
    {
        public override void Execute( IActionContext context )
        {
            IResourceList resources = context.SelectedResources;
            foreach( IResource res in resources )
            {
                string path = null;
                try
                {
                    if( res.Type == FileProxy._folderResourceType )
                    {
                        path = res.GetPropText( FileProxy._propDirectory );
                        Process.Start( path );
                    }
                    else
                    {
                        path = FoldersCollection.Instance.GetFullName( res );
                        Process.Start( "explorer", "/select, \"" + path + "\"" );
                    }
                }
                catch( Exception e )
                {
                    Utils.DisplayException( e, "Error" );
                }
            }
        }
    }

    /**
     * move file to recycle bin or delete permanently
     */
    public class DeleteAction : IAction
    {
        public void Execute( IActionContext context )
        {
            IResourceList selected = context.SelectedResources;
            ArrayList names = new ArrayList( selected.Count );
            foreach( IResource res in selected.ValidResources )
            {
                string name = res.GetStringProp( FileProxy._propDirectory );
                if( res.Type != FileProxy._folderResourceType )
                {
                    name = Path.Combine( name, res.GetPropText( Core.Props.Name ) );
                }
                names.Add( name );
            }
            if( names.Count > 0 )
            {
                IResourceBrowser rBrowser = Core.ResourceBrowser;
                IResourceList viewedResources = selected.Intersect( rBrowser.SelectedResources );
                IResource res2Select = null;
                foreach( IResource res in viewedResources.ValidResources )
                {
                    res2Select = rBrowser.GetResourceBelow( res );
                }
                try
                {
                    FoldersCollection foldersCollection = FoldersCollection.Instance;
                    if( Control.ModifierKeys == Keys.Shift )
                    {
                        if( MessageBox.Show( Core.MainWindow,
                            "Are you sure you want to delete " +  ( ( names.Count > 1 ) ?
                            ( names.Count + " selected items?" ) : ( "'" + names[ 0 ] + "'?" ) ),
                            "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2 ) == DialogResult.Yes )
                        {
                            foreach( IResource res in selected.ValidResources )
                            {
                                foldersCollection.DeleteResource( res );
                            }
                            foreach( string name in names )
                            {
                                if( File.Exists( name ) )
                                {
                                    File.Delete( name );
                                }
                                else if( Directory.Exists( name ) )
                                {
                                    Directory.Delete( name, true );
                                }
                            }
                        }
                    }
                    else
                    {
                        Shell32.MoveFile2RecycleBin( (string[]) names.ToArray( typeof( string ) ) );
                        FileProxy._filesProcessor.QueueJob(
                            new ResourceListDelegate( foldersCollection.EnumerateParents ),
                            selected );
                    }
                }
                catch( Exception e )
                {
                    Utils.DisplayException( e, "Error" );
                    return;
                }
                if( res2Select != null )
                {
                    rBrowser.SelectResource( res2Select );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources == null || context.SelectedResources.Count == 0 )
            {
            	presentation.Visible = false;
                return;
            }

            foreach( IResource res in context.SelectedResources.ValidResources )
            {
                if( !( res.Type == FileProxy._folderResourceType ))
                {
                    IResource folder = res.GetLinkProp( FileProxy._propParentFolder );
                    if( folder == null || folder.Type != FileProxy._folderResourceType )
                    {
                        presentation.Visible = false;
                        return;
                    }
                }
            }
        }
    }

    /**
     * renames a file
     */
    public class FileRenameAction : FileAction
    {
        public override void Execute( IActionContext context )
        {
            IResourceList resources = context.SelectedResources;
            if ( context.Instance == FileProxy._pane )
            {
                FileProxy._pane.EditResourceLabel( resources[ 0 ] );
            }
            else
            {
                Core.ResourceBrowser.EditResourceLabel( resources[ 0 ] );
            }
        }

        public override void Update(IActionContext context, ref ActionPresentation presentation)
        {
            base.Update( context, ref presentation );
            if( presentation.Visible && context.SelectedResources.Count > 1 )
            {
                presentation.Visible = false;
            }
            if( presentation.Visible )
            {
                presentation.Enabled = !context.SelectedResources[ 0 ].HasProp( FileProxy._propStatus );
            }
        }
    }

    /**
     * invokes File Folders options pane
     */
    public class InvokeFileFoldersOptionsPaneAction: IAction
    {
        public void Execute( IActionContext context )
        {
            Core.UIManager.ShowOptionsDialog( "Folders & Files", "Indexed Folders" );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.Instance != FileProxy._pane && context.Kind != ActionContextKind.Toolbar )
            {
                presentation.Visible = false;
            }
        }
    }

    /**
     * Sends file by email
     */
    public class SendFileAction : FileAction
    {
        private IEmailService GetEmailService()
        {
            return (IEmailService) Core.PluginLoader.GetPluginService( typeof( IEmailService ) );
        }

        public override void Execute( IActionContext context )
        {
            IEmailService service = GetEmailService();

            IResourceList resources = context.SelectedResources;
            ArrayList attachments = new ArrayList();
            for( int i = 0; i < resources.Count; ++i )
            {
                attachments.Add( FoldersCollection.Instance.GetFullName( resources[ i ] ));
            }
            service.CreateEmail( null, null, EmailBodyFormat.PlainText, (EmailRecipient[]) null,
                (string[]) attachments.ToArray( typeof( string ) ), true );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                if( ( presentation.Enabled = GetEmailService() != null ) )
                {
                    IResourceList resources = context.SelectedResources;
                    for( int i = 0; i < resources.Count; ++i )
                    {
                        if( resources[ i ].Type == FileProxy._folderResourceType )
                        {
                            presentation.Visible = false;
                            break;
                        }
                    }
                }
            }
        }
    }

    /**
     * sets selection in the file folders tree to a double-clicked folder
     */
    public class SelectFolderAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            FileProxy._pane.SelectResource( context.SelectedResources[ 0 ] );
        }
    }

    /**
     * create new folder action
     */
    public class CreateNewFolderAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource folder = context.SelectedResources[ 0 ];
            string newPath = Path.Combine( folder.GetPropText( FileProxy._propDirectory ), "New Folder" );
            IOTools.CreateDirectory( newPath );
            IResource newFolder = FoldersCollection.Instance.FindOrCreateDirectory( newPath );
            if( newFolder != null )
            {
                Core.WorkspaceManager.AddToActiveWorkspace( newFolder );
                FileProxy._pane.EditResourceLabel( newFolder );
            }
        }
    }

    /**
     * actions for setting file folders indexing mode
     */
    public class SetIndexModeAction : ActionOnSingleResource
    {
        private int _mode;

        public SetIndexModeAction( int mode )
        {
            _mode = mode;
        }

        public override void Execute( IActionContext context )
        {
            IResource folder = context.SelectedResources[ 0 ];
            if( folder.GetIntProp( FileProxy._propStatus ) != _mode )
            {
                new ResourceProxy( folder ).SetProp( FileProxy._propStatus, _mode );
                FoldersCollection.Instance.Interrupted = true;
                FoldersCollection.Instance.WaitUntilFinished();
                FoldersCollection.LoadFoldersForest();
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                IResource folder = context.SelectedResources[ 0 ];
                if( folder.Type != FileProxy._folderResourceType || !folder.HasProp( FileProxy._propStatus ) )
                {
                    presentation.Visible = false;
                    return;
                }
                int status = folder.GetIntProp( FileProxy._propStatus );
                presentation.Checked = ( status == _mode );
            }
        }
    }
}
