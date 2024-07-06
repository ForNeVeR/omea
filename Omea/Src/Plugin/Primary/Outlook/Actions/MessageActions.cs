// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    /**
     * Base class for actions working on the selected Outlook messages.
     */

    public abstract class OutlookAction : IAction
    {
        public OutlookAction()
        {
        }
        public virtual void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if( context.SelectedResources.Count == 0 ||
                context.SelectedResources[ 0 ].Type != STR.Email )
            {
                presentation.Visible = false;
            }
        }
        public virtual void Execute( IActionContext context )
        {
            Trace.WriteLine( ">>> Executing OutlookAction" );
            try
            {
                if ( context.SelectedResources.Count > 0 )
                {
                    if ( NeedProcess( context.SelectedResources ) )
                    {
                        OutlookSession.OutlookProcessor.QueueJob( JobPriority.Immediate, "Execute action with outlook forms",
                            new ResourceListDelegate( ExecuteAction ), context.SelectedResources );
                    }
                }
            }
            catch ( COMException exc )
            {
                MsgBox.Error( "Outlook plugin", "Exception: " + exc.Message );
            }
            Trace.WriteLine( "<<< Executing OutlookAction" );
        }

        protected virtual void ExecuteAction( IResourceList selectedResources )
        {
            try
            {
                foreach ( IResource resource in selectedResources )
                {
                    PairIDs pairIDs = PairIDs.Get( resource );
                    //if ( pairIDs == null ) continue;
                    ExecuteAction( resource, pairIDs );
                }
            }
            catch ( Exception exception )
            {
                StandartJobs.MessageBox( "Operation cannot be completed.\nReason: " + exception.Message, "Outlook Plugin",
                    MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }
        protected virtual void ExecuteAction( IResource resource, PairIDs pairIDs )
        {}
        protected virtual bool NeedProcess( IResourceList selectedResources )
        {
            return true;
        }
        protected bool AskUserNeedProcess( string message, int count )
        {
            return AskUserNeedProcess( message, count, 5 );
        }
        protected bool AskUserNeedProcess( string message, int count, int maxCount )
        {
            if ( count < maxCount )
            {
                return true;
            }
            return DialogResult.Yes == MessageBox.Show( message, "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question );
        }

        protected static string AskSingleNamePath( string title, string fileName )
        {
            string  path = null;
            string  pathToSave = Settings.LastSelectedFileFolder;

            if( (Control.ModifierKeys == Keys.Control) && (pathToSave.Length > 0) )
            {
                path = Path.Combine( pathToSave.Trim( '\\' ), fileName );
            }
            else
            {
                using( SaveFileDialog saveDlg = new SaveFileDialog() )
                {
                    saveDlg.Title = title;
                    saveDlg.AddExtension = true;
                    if ( fileName != null )
                    {
                        string EXT = Path.GetExtension( fileName ).ToUpper().Replace( ".", string.Empty );
                        string ext = Path.GetExtension( fileName );
                        saveDlg.Filter = EXT + " files (*" + ext + ")|*" + Path.GetExtension( fileName ) + "|All files (*.*)|*.*";
                    }

                    string lastSelectedFileFolder = Settings.LastSelectedFileFolder;
                    if ( lastSelectedFileFolder != null && fileName != null )
                    {
                        saveDlg.FileName = Path.Combine( lastSelectedFileFolder, fileName );
                    }

                    if ( saveDlg.ShowDialog() == DialogResult.OK )
                    {
                        path = saveDlg.FileName;
                        Settings.LastSelectedFileFolder.Save( Path.GetDirectoryName( path ) );
                    }
                }
            }
            return path;
        }

        protected static string  AskPath( string title )
        {
            string  path = null;
            string  pathToSave = Settings.LastSelectedFileFolder;

            if( (Control.ModifierKeys == Keys.Control) && (pathToSave.Length > 0) )
            {
                path = pathToSave;
            }
            else
            {
                using( FolderBrowserDialog dlg = new FolderBrowserDialog() )
                {
                    dlg.Description = title;
                    dlg.SelectedPath = pathToSave;
                    if ( dlg.ShowDialog() == DialogResult.OK )
                    {
                        if ( dlg.SelectedPath.Length != 0 )
                        {
                            path = dlg.SelectedPath;
                            Settings.LastSelectedFileFolder.Save( path );
                        }
                    }
                }
            }
            return path;
        }
    }

    /**
     * Action which opens the selected message in a new Outlook window.
     */

    public class DisplayMessageAction : OutlookAction
    {
        protected override void ExecuteAction( IResource resource, PairIDs pairIDs )
        {
            Tracer._Trace( "Execute action: DisplayMessageAction" );
            if ( resource.HasProp( PROP.EmbeddedMessage ) )
            {
                IEMessage message = new OutlookAttachment( resource ).OpenEmbeddedMessage();
                if ( message != null )
                {
                    using ( message )
                    {
                        string path = Core.FileResourceManager.GetUniqueTempDirectory();
                        path = Path.Combine( path, SaveToMSGAction.GetFileName( resource ) + ".msg" );
                        message.SaveToMSG( path );
                        try
                        {
                            Process.Start( path );
                        }
                        catch( Exception e )
                        {
                            Utils.DisplayException( e, "Can't open file" );
                        }
                    }
                }
            }
            else
            {
                if ( pairIDs == null )
                {
                    return;
                }
                new ResourceProxy( resource ).SetPropAsync( Core.Props.IsUnread, false );
                OutlookFacadeHelper.DisplayMessage( pairIDs.EntryId, pairIDs.StoreId );
            }
        }
        protected override bool NeedProcess( IResourceList selectedResources )
        {
            return AskUserNeedProcess( "Are you sure you want to display " + selectedResources.Count + " messages?",
                selectedResources.Count );
        }
    }

    /**
     * Action which creates a reply to the selected message.
     */

    public class ReplyMessageAction : OutlookAction
    {
        protected override void ExecuteAction( IResource resource, PairIDs pairIDs )
        {
            Tracer._Trace( "Execute action: ReplyMessageAction" );
            MarkAsReadOnReply.Do( resource );
            if ( pairIDs == null )
            {
                return;
            }
            OutlookFacadeHelper.ReplyMessage( resource, pairIDs.EntryId, pairIDs.StoreId );
        }
        protected override bool NeedProcess( IResourceList selectedResources )
        {
            return AskUserNeedProcess( "Are you sure you want to reply " + selectedResources.Count + " messages?", selectedResources.Count );
        }
    }

    /**
     * Action which creates a reply to all the recipients of the selected message.
     */

    public class ReplyAllMessageAction : OutlookAction
    {
        protected override void ExecuteAction( IResource resource, PairIDs pairIDs )
        {
            Tracer._Trace( "Execute action: ReplyAllMessageAction" );
            MarkAsReadOnReply.Do( resource );
            if ( pairIDs == null )
            {
                return;
            }
            OutlookFacadeHelper.ReplyAllMessage( resource, pairIDs.EntryId, pairIDs.StoreId );
        }
        protected override bool NeedProcess( IResourceList selectedResources )
        {
            return AskUserNeedProcess( "Are you sure you want to reply " + selectedResources.Count + " messages?", selectedResources.Count );
        }
    }

    public class ForwardMessageAction : OutlookAction
    {
        private string[] SaveMessages( IResourceList selectedResources )
        {
            ArrayList array = new ArrayList( selectedResources.Count );

            foreach ( IResource resource in selectedResources.ValidResources )
            {
                PairIDs msgId = PairIDs.Get( resource );
                IEMessage message = OutlookSession.OpenMessage( msgId.EntryId, msgId.StoreId );
                if ( message != null )
                {
                    using ( message )
                    {
                        string path = Core.FileResourceManager.GetUniqueTempDirectory();
                        path = Path.Combine( path, resource.GetPropText( Core.Props.Name ) + ".msg" );
                        message.SaveToMSG( path );
                        array.Add( path );
                    }
                }
            }
            string[] files = new string[ array.Count ];
            for ( int i = 0; i < array.Count; ++i )
            {
                files[i] = array[i] as string;
            }
            return files;
        }

        protected override void ExecuteAction( IResourceList selectedResources )
        {
            if ( selectedResources.Count == 1 )
            {
                base.ExecuteAction( selectedResources );
            }
            else
            {
                string[] files = SaveMessages( selectedResources );
                OutlookFacadeHelper.CreateNewMessage( "", "", EmailBodyFormat.PlainText, (IResourceList)null, files, true );
            }
        }
        protected override void ExecuteAction( IResource resource, PairIDs pairIDs )
        {
            Tracer._Trace( "Execute action: ForwardMessageAction" );
            MarkAsReadOnReply.Do( resource );
            if ( pairIDs == null )
            {
                return;
            }
            OutlookFacadeHelper.ForwardMessage( pairIDs.EntryId, pairIDs.StoreId );
        }
        protected override bool NeedProcess( IResourceList selectedResources )
        {
            return AskUserNeedProcess( "Are you sure you want to forward " + selectedResources.Count + " messages?", selectedResources.Count );
        }
    }

    public class PrintMessageAction : OutlookAction
    {
        protected override void ExecuteAction( IResource resource, PairIDs pairIDs )
        {
            if ( pairIDs == null )
            {
                return;
            }
            Tracer._Trace( "Execute action: PrintMessageAction" );
            OutlookFacadeHelper.PrintMessage( pairIDs.EntryId, pairIDs.StoreId );
        }
        protected override bool NeedProcess( IResourceList selectedResources )
        {
            return AskUserNeedProcess( "Are you sure you want to print " + selectedResources.Count + " message(s)?", selectedResources.Count, 1 );
        }
    }

    public class SynchronizeFolderNowAction : OutlookAction
    {
        protected override void ExecuteAction( IResource folder, PairIDs pairIDs )
        {
            Trace.WriteLine( ">>> SynchronizeFolderNowAction.ExecuteAction" );
            if ( pairIDs == null )
            {
                return;
            }
            if ( Folder.IsPublicFolder( folder ) )
            {
                RefreshFolderDescriptor.Do( JobPriority.Immediate, pairIDs, Settings.IndexStartDate );
            }
            Trace.WriteLine( "<<< SynchronizeFolderNowAction.ExecuteAction" );
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count == 0 || !context.SelectedResources.AllResourcesOfType( STR.MAPIFolder ) )
            {
                presentation.Visible = false;
                return;
            }
            foreach ( IResource folder in context.SelectedResources )
            {
                if ( Folder.IsPublicFolder( folder ) && !Folder.IsIgnored( folder ) )
                {
                    presentation.Enabled = true;
                    presentation.Visible = true;
                    return;
                }
            }
            presentation.Enabled = false;
            presentation.Visible = false;
            return;
        }
    }

    /**
     * Action which deletes the selected messages.
     */

    public class DeleteMessageAction : OutlookAction
    {
        protected bool _deletedItems = true;

        protected override void ExecuteAction( IResource resource, PairIDs pairIDs )
        {
            Tracer._Trace( "Execute action: DeleteMessageAction" );
            Trace.WriteLine( ">>> DeleteMessageAction.ExecuteAction" );
            MailItemDeleter.DeleteMail( resource, pairIDs, _deletedItems );
            Trace.WriteLine( "<<< DeleteMessageAction.ExecuteAction" );
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( presentation.Visible )
            {
                foreach ( IResource mail in context.SelectedResources )
                {
                    if ( Mail.CanBeDeleted( mail ) )
                    {
                        return;
                    }
                }
                presentation.Visible = false;
            }
        }
    }

    public class DeleteMessagePermanentAction : DeleteMessageAction
    {
        public DeleteMessagePermanentAction()
        {
            _deletedItems = false;
        }
        protected override bool NeedProcess( IResourceList selectedResources )
        {
            Guard.NullArgument( selectedResources, "selectedResources" );
            if ( _deletedItems )
            {
                return true;
            }
            if ( selectedResources.Count == 0 )
            {
                return false;
            }
            IResource mail = selectedResources[ 0 ];
            IResource folder = mail.GetLinkProp( PROP.MAPIFolder );
            Guard.NullLocalVariable( folder, "folder" );

            if ( Folder.IsIMAPFolder( folder ) )
            {
                _deletedItems = true;
                return true;
            }

            string strConfirmation = null;
            if ( selectedResources.Count == 1 )
            {
                strConfirmation = "Are you sure that you want to permanently delete the selected message?";
            }
            else
            {
                strConfirmation = "Are you sure that you want to permanently delete the selected messages?";
            }
            DialogResult result =
                MessageBox.Show( strConfirmation, "Attention", MessageBoxButtons.YesNo, MessageBoxIcon.Warning );
            return ( result == DialogResult.Yes );
        }

        protected override void ExecuteAction( IResourceList selectedResources )
        {
            // Because of problems with delete notifications on Exchange
            // (see comments in MailDeletedDescriptor .ctor), we immediately delete the selected resources.
            base.ExecuteAction( selectedResources );
            foreach ( IResource resource in selectedResources.ValidResources )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceDelegate( Mail.Delete ), resource );
            }
        }
    }

    /**
     * Action which deletes the selected folders.
     */

    public class DeleteFolderAction : OutlookAction
    {
        protected bool _deletedItems = true;

        protected override bool NeedProcess( IResourceList selectedResources )
        {
            if ( selectedResources.Count == 0 )
            {
                return false;
            }
            string strConfirmation = null;
            bool deletedItems = _deletedItems;
            if ( deletedItems )
            {
                bool change = true;
                bool hasDeleted = false;
                foreach (  IResource resource in selectedResources.ValidResources )
                {
                    if ( !Folder.HasDeletedItemsAsAncestor( resource ) )
                    {
                        change = false;
                        break;
                    }
                    else
                    {
                        hasDeleted = true;
                    }
                }
                if ( change && hasDeleted )
                {
                    deletedItems = false;
                }
            }
            if ( deletedItems )
            {
                if ( selectedResources.Count > 1 )
                {
                    strConfirmation =
                        "Are you sure you want to delete the selected folders and move all of their contents into the Deleted Items folder?";
                }
                else
                {
                    string folderName = selectedResources[ 0 ].GetPropText( Core.Props.Name );
                    strConfirmation =
                        "Are you sure you want to delete the folder '"
                        + folderName + "' and move all of its contents into the Deleted Items folder?";
                }
            }
            else
            {
                if ( selectedResources.Count > 1 )
                {
                    strConfirmation =
                        "Are you sure that you want to permanently delete the selected folders?";
                }
                else
                {
                    strConfirmation =
                        "Are you sure that you want to permanently delete the selected folder?";
                }
            }
            DialogResult result =
                MessageBox.Show( strConfirmation, "Attention", MessageBoxButtons.YesNo, MessageBoxIcon.Warning );
            return ( result == DialogResult.Yes );
        }

        public static void DeleteFolder( IResource folder, bool deletedItems )
        {
            if ( Folder.IsDefault( folder ) )
            {
                return;
            }
            IResourceList folders = Folder.GetDefaultDeletedItemsFolder();
            HashSet childrenNames = new HashSet();
            foreach ( IResource resFolder in folders )
            {
                foreach ( IResource child in Folder.GetSubFolders( resFolder ) )
                {
                    childrenNames.Add( child.GetPropText( Core.Props.Name ) );
                }
            }

            string name = folder.GetPropText( Core.Props.Name );
            string oldName = name;
            bool rename = false;
            for ( int j = 1;; j++ )
            {
                if ( !childrenNames.Contains( name ) )
                {
                    break;
                }
                name = oldName + j.ToString();
                rename = true;
            }
            PairIDs pairIds = PairIDs.Get( folder );
            if ( pairIds != null )
            {
                if ( deletedItems && rename )
                {
                    OutlookSession.DeleteFolderWithRename( pairIds, name );
                }
                else
                {
                    OutlookSession.DeleteFolder( pairIds, deletedItems );
                }
            }

            if ( !deletedItems )
            {
                Core.ResourceAP.QueueJob( JobPriority.AboveNormal,
                    new ResourceDelegate( DeleteFolderResource ), folder );
            }
        }
        private static void DeleteFolderResource( IResource folder )
        {
            Folder.DeleteFolder( folder );
        }

        protected override void ExecuteAction( IResourceList selectedResources )
        {
            Trace.WriteLine( ">>> DeleteFolderAction.ExecuteAction" );
            if ( selectedResources.Count == 0 )
            {
                return;
            }
            foreach ( IResource folder in selectedResources.ValidResources )
            {
                DeleteFolder( folder, _deletedItems );
            }
            Trace.WriteLine( "<<< DeleteFolderAction.ExecuteAction" );
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count == 0 || !context.SelectedResources.AllResourcesOfType( STR.MAPIFolder ) )
            {
                presentation.Visible = false;
                return;
            }
            foreach ( IResource folder in context.SelectedResources )
            {
                if ( Folder.IsDefault( folder ) )
                {
                    presentation.Enabled = false;
                    return;
                }
            }
        }
    }

    public class DeleteFolderPermanentAction : DeleteFolderAction
    {
        public DeleteFolderPermanentAction()
        {
            _deletedItems = false;
        }
    }

    public class MoveFolderToFolderAction : OutlookAction
    {
        protected PairIDs _selectedFolder;
        protected IResource _targetFolder;

        public void DoMove( IResource targetFolder, IResourceList selectedResources )
        {
            SetFolder( PairIDs.Get( targetFolder ), targetFolder );
            OutlookSession.OutlookProcessor.QueueJob( JobPriority.Immediate, "Move subfolder to another parent folder",
                new ResourceListDelegate( ExecuteActionImpl ), selectedResources );
        }
        protected void SetFolder( PairIDs selectedFolder, IResource targetFolder )
        {
            _selectedFolder = selectedFolder;
            _targetFolder = targetFolder;
        }

        protected override void ExecuteAction( IResourceList selectedResources )
        {
            ExecuteActionImpl( selectedResources );
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count > 0 && !context.SelectedResources.AllResourcesOfType( STR.MAPIFolder ) )
            {
                presentation.Visible = false;
            }
        }
        protected void ExecuteActionImpl( IResourceList selectedResources )
        {
            Tracer._Trace( "Execute action: MoveFolderToFolderAction" );
            if ( _selectedFolder == null || _targetFolder == null )
            {
                return;
            }

            IEFolder destFolder =
                OutlookSession.OpenFolder( _selectedFolder.EntryId, _selectedFolder.StoreId );
            if ( destFolder == null )
            {
                return;
            }

            using ( destFolder )
            {
                for ( int i = 0; i < selectedResources.Count; i++ )
                {
                    PairIDs folderIDs = PairIDs.Get( selectedResources[ i ] );
                    if ( folderIDs == null )
                    {
                        continue;
                    }
                    IResource parentFolder = Folder.GetParent( selectedResources[ i ] );
                    PairIDs parentFolderIDs = PairIDs.Get( parentFolder );
                    if ( parentFolderIDs == null )
                    {
                        continue;
                    }

                    IEFolder ieFolder =
                        OutlookSession.OpenFolder( parentFolderIDs.EntryId, parentFolderIDs.StoreId );
                    if ( ieFolder == null )
                    {
                        continue;
                    }
                    using ( ieFolder )
                    {
                        Tracer._Trace( "Move folder: " + folderIDs.EntryId.GetHashCode() + "/" + folderIDs.EntryId );
                        try
                        {
                            ieFolder.MoveFolder( folderIDs.EntryId, destFolder );
                        }
                        catch ( COMException exception )
                        {
                            Tracer._TraceException( exception );
                            if ( exception.ErrorCode == ( unchecked( (int)0x8004010F ) ) )
                            {
                                StandartJobs.MessageBox( "Folder is not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                            }
                            else if ( exception.ErrorCode == ( unchecked( (int)0x80040604 ) ) )
                            {
                                StandartJobs.MessageBox( "Collision. Probably target folder has subfolder with the same name", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                            }
                            else if ( exception.ErrorCode == ( unchecked( (int)0x80040119 ) ) || exception.ErrorCode == ( unchecked( (int)0x8004dff2 ) ) )
                            {
                                StandartJobs.MessageBox( "Unspecified error. Can't move or copt folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                            }
                            else
                            {
                                StandartJobs.MessageBox( exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                            }
                        }
                    }
                }
            }
        }
    }

    public class CreateFolderAction : OutlookAction
    {
        private string _folderName;
        protected override void ExecuteAction( IResourceList selectedResources )
        {
            Trace.WriteLine( ">>> CreateFolderAction.ExecuteAction" );
            PairIDs folderIDs = PairIDs.Get( selectedResources[ 0 ] );
            try
            {
                IEFolder folder =
                    OutlookSession.OpenFolder( folderIDs.EntryId, folderIDs.StoreId );
                if ( folder == null )
                {
                    return;
                }
                using ( folder )
                {
                    IEFolder subFolder = folder.CreateSubFolder( _folderName );
                    if ( subFolder != null )
                    {
                        using ( subFolder )
                        {}
                    }
                }
            }
            catch ( COMException exception )
            {
                Tracer._TraceException( exception );
                MsgBox.Error( "Outlook Plugin", "Cannot create new folder.\n" +
                    "Reason is: folder with such name already exists." );
            }
            catch ( System.UnauthorizedAccessException exception )
            {
                Tracer._TraceException( exception );
                MsgBox.Error( "Outlook Plugin", "Cannot create new folder.\n" + exception.Message );
            }
            Trace.WriteLine( "<<< CreateFolderAction.ExecuteAction" );
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count != 1 || context.SelectedResources[ 0 ].Type != STR.MAPIFolder )
            {
                presentation.Visible = false;
            }
        }
        protected override bool NeedProcess( IResourceList selectedResources )
        {
            if ( selectedResources.Count > 0 )
            {
                string input =
                    Core.UIManager.InputString( "Create New Folder", "Please, enter the name for new folder.", "", null, null );
                if ( input != null && input.Length > 0 )
                {
                    _folderName = input;
                    return true;
                }
            }
            return false;
        }

        public override void Execute( IActionContext context )
        {
            Trace.WriteLine( ">>> Executing OutlookAction" );
            try
            {
                base.Execute( context );
            }
            catch ( COMException exc )
            {
                StandartJobs.MessageBox( "Operation cannot be completed.\nReason: " + exc.Message, "Outlook Plugin",
                    MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
            Trace.WriteLine( "<<< Executing OutlookAction" );
        }
    }

    /**
     * Action which undeletes the selected IMAP messages.
     */

    public class UnDeleteIMAPMessageAction : OutlookAction
    {
        protected override void ExecuteAction( IResource resource, PairIDs pairIDs )
        {
            Tracer._Trace( "Execute action: UnDeleteIMAPMessageAction" );
            Trace.WriteLine( ">>> UnDeleteIMAPMessageAction.ExecuteAction" );
            if ( pairIDs == null )
            {
                return;
            }

            IResource folder = resource.GetLinkProp( PROP.MAPIFolder );
            if ( !Folder.IsIMAPFolder( folder ) )
            {
                return;
            }
            IEMessage message = OutlookSession.OpenMessage( pairIDs.EntryId, pairIDs.StoreId );
            if ( message == null )
            {
                return;
            }
            using ( message )
            {
                int tag = message.GetIDsFromNames( ref GUID.set1, lID.msgDeletedInIMAP, PropType.PT_LONG );
                message.DeleteProp( tag );
                OutlookSession.SaveChanges( "Undelete message resource id = " + resource.Id, message, pairIDs.EntryId );
            }
            Trace.WriteLine( "<<< UnDeleteIMAPMessageAction.ExecuteAction" );
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( presentation.Visible )
            {
                foreach ( IResource mail in context.SelectedResources )
                {
                    if ( mail.HasProp( PROP.DeletedInIMAP ) )
                    {
                        return;
                    }
                }
                presentation.Visible = false;
            }
        }
    }

    public class SaveAttachments : OutlookAction
    {
        private string  _path;
        private bool    _single = false;

        protected override bool NeedProcess( IResourceList selectedResources )
        {
            _path = null;
            _single = selectedResources.Count == 1;

            if ( _single )
            {
                string fileName = selectedResources[ 0 ].GetPropText( Core.Props.Name );
                if ( fileName.Length == 0 )
                    fileName = "noname";

                _path = AskSingleNamePath( "Save Attachment", fileName );
            }
            else
            {
                _path = AskPath( "Save Attachment(s) to Folder" );
            }
            return _path != null;
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = false;
            if( context.SelectedResources.Count > 0 )
            {
                //  ALL resources must be a proped outlook attachments linked
                //  to a Mail resource. Otherwise we have an exception during save.
                presentation.Visible = true;
                foreach ( IResource attach in context.SelectedResources.ValidResources )
                {
                    IResource parent = attach.GetLinkProp( PROP.Attachment );
                    presentation.Visible = presentation.Visible &&
                                           (parent != null) && (parent.Type == STR.Email);
                }
            }
        }

        protected override void ExecuteAction( IResource resource, PairIDs pairIDs )
        {
            try
            {
                if ( _single )
                {
                    new OutlookAttachment( resource ).SaveAs( _path );
                }
                else
                {
                    SaveAllAttachments.SaveAttachment( resource, _path );
                }
            }
            catch ( OutlookAttachmentException exception )
            {
                Tracer._TraceException( exception );
            }
        }
    }

    public class SaveAllAttachments : OutlookAction
    {
        protected string _path;

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( presentation.Visible )
            {
                foreach ( IResource mail in context.SelectedResources.ValidResources )
                {
                    if ( mail.GetLinkCount( -PROP.Attachment ) > 0 )
                    {
                        presentation.Visible = true;
                        return;
                    }
                }
                presentation.Visible = false;
            }
        }

        protected override void ExecuteAction( IResource resource, PairIDs pairIDs )
        {
            try
            {
                SaveAttachments( resource.GetLinksTo( null, PROP.Attachment ), _path );
            }
            catch ( OutlookAttachmentException exception )
            {
                Tracer._TraceException( exception );
            }
        }

        protected override bool NeedProcess( IResourceList selectedResources )
        {
            _path = AskPath( "Save Attachment(s) to Folder" );
            return _path != null;
        }

        public static void SaveAttachments( IResourceList attachments, string path )
        {
            foreach ( IResource attach in attachments.ValidResources )
            {
                SaveAttachment( attach, path );
            }
        }

        public static void SaveAttachment( IResource attach, string path )
        {
            string fileName = attach.GetPropText( Core.Props.Name );
            if ( fileName.Length == 0 )
            {
                fileName = "noname";
            }
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension( fileName );
            string extension = Path.GetExtension( fileName );

            string fullName = Path.Combine( path, fileName );
            int count = 0;
            while ( File.Exists( fullName ) )
            {
                ++count;
                fullName = Path.Combine( path, fileNameWithoutExtension + "[" + count + "]" + extension );
            }
            new OutlookAttachment( attach ).SaveAs( fullName );
        }
    }

    public class SaveToMSGAction : OutlookAction
    {
        private string _path;
        bool _single = false;

        protected override bool NeedProcess( IResourceList selectedResources )
        {
            _single = selectedResources.Count == 1;
            _path = null;

            if ( _single )
            {
                string fileName = GetFileName( selectedResources[ 0 ] ) + GetExtension();
                _path = AskSingleNamePath( "Save As", fileName );
            }
            else
            {
                _path = AskPath( "Save As" );
            }
            return (_path != null);
        }

        public static string GetFileName( IResource resource )
        {
            string subject = resource.GetPropText( Core.Props.Subject );
            CorrectFileName( ref subject );

            if ( subject.Length == 0 )
            {
                subject = resource.GetPropText( PROP.EntryID );
            }
            return subject;
        }
        protected virtual string GetExtension()
        {
            return ".msg";
        }
        protected virtual void ExecuteActionImpl( IEMessage message, string fileName )
        {
            message.SaveToMSG( fileName );
        }
        protected override void ExecuteAction( IResource resource, PairIDs pairIDs )
        {
            Tracer._Trace( "Execute action: SaveToMSGAction" );
            Trace.WriteLine( ">>> SaveToMSGAction.ExecuteAction" );
            if ( pairIDs != null )
            {
                IEMessage message = OutlookSession.OpenMessage( pairIDs.EntryId, pairIDs.StoreId );
                if ( message == null )
                {
                    return;
                }
                using ( message )
                {
                    string fileName = null;
                    if ( !_single )
                    {
                        string subject = message.GetStringProp( MAPIConst.PR_SUBJECT );
                        CorrectFileName( ref subject );

                        if ( subject == null || subject.Length == 0 )
                        {
                            subject = message.GetBinProp( MAPIConst.PR_ENTRYID );
                        }

                        fileName = Path.Combine( _path, subject + ".msg" );
                    }
                    else
                    {
                        fileName = _path;
                    }
                    ExecuteActionImpl( message, fileName );
                }
            }
            Trace.WriteLine( "<<< SaveToMSGAction.ExecuteAction" );
        }

        private static void  CorrectFileName( ref string filename )
        {
            if( !String.IsNullOrEmpty( filename ) )
            {
                filename = filename.Replace( ":", "." );
                foreach ( char ch in Path.InvalidPathChars )
                {
                    filename = filename.Replace( new string( ch, 1 ), string.Empty );
                }
                filename = filename.Replace( "/", "." );
                filename = filename.Replace( "?", "." );
                filename = filename.Replace( "*", "." );
                filename = filename.Replace( "\\", "." );
                filename = filename.Trim();
            }
        }
    }

    public class SaveToTxtAction : SaveToMSGAction
    {
        protected override string GetExtension()
        {
            return ".txt";
        }
        protected override void ExecuteActionImpl( IEMessage message, string fileName )
        {
            FileStream file = null;
            TextWriter writer = null;
            try
            {
                file = File.Create( fileName );
                if ( file != null )
                {
                    string body = message.GetPlainBody();
                    byte[] bytes = Encoding.Unicode.GetBytes( body );
                    writer = new StreamWriter( file );
                    writer.Write( Encoding.ASCII.GetString( bytes ) );
                    writer.Flush();
                }
            }
            finally
            {
                if ( file != null )
                {
                    file.Close();
                }
            }
        }
    }

    public class ForwardAllAttachments : OutlookAction
    {
        private string _path;
        protected override bool NeedProcess( IResourceList selectedResources )
        {
            _path = Core.FileResourceManager.GetUniqueTempDirectory();
            return ( selectedResources.Count != 0 );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                foreach ( IResource attach in context.SelectedResources.ValidResources )
                {
                    if ( attach.GetLinkCount( PROP.Attachment ) == 0 )
                    {
                        presentation.Visible = false;
                        return;
                    }
                }
            }
        }
        protected override void ExecuteAction( IResourceList selectedResources )
        {
            try
            {
                SaveAllAttachments.SaveAttachments( selectedResources, _path );

                string[] files =  Directory.GetFiles( _path );
                if ( files == null ) return;
                for ( int i = 0; i < files.Length; ++i )
                {
                    files[ i ] = Path.Combine( _path, files[ i ] );
                }
                OutlookFacadeHelper.CreateNewMessage( "", "", EmailBodyFormat.PlainText, (IResourceList)null, files, true );
            }
            catch ( OutlookAttachmentException exception )
            {
                Tracer._TraceException( exception );
            }
        }
    }
}
