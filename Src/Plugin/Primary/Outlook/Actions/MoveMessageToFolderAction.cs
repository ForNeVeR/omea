// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows.Forms;
using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Diagnostics;

namespace JetBrains.Omea.OutlookPlugin
{

    /**
    * Action which move selected message to specified folder.
    */
    public class MoveMessageToFolderAction : OutlookAction
    {
        protected IResource _selectedFolder;
        bool _copy = false;

        public MoveMessageToFolderAction()
        {}

        public MoveMessageToFolderAction( bool copy )
        {
            _copy = copy;
        }

        public bool IsCopy { get { return _copy; } }

        public void DoMove( IResource selectedFolder, IResourceList selectedResources )
        {
            Tracer._Trace( "Execute DoMove: MoveMessageToFolderAction" );
            Guard.NullArgument( selectedFolder, "selectedFolder" );
            Guard.NullArgument( selectedResources, "selectedResources" );
            SetFolder( selectedFolder );
            OutlookSession.OutlookProcessor.QueueJob( JobPriority.Immediate, "Move message to another folder",
                new ResourceListDelegate( ExecuteActionImpl ), selectedResources );
        }
        protected void SetFolder( IResource selectedFolder )
        {
            _selectedFolder = selectedFolder;
        }
        protected override void ExecuteAction( IResourceList selectedResources )
        {
            Tracer._Trace( "Execute action: MoveMessageToFolderAction" );
            OutlookSession.OutlookProcessor.QueueJob( JobPriority.Immediate, "Move message",
                new ResourceListDelegate( ExecuteActionImpl ), selectedResources );
        }

        protected virtual void ExecuteActionImpl( IResourceList selectedResources )
        {
            Tracer._Trace( "Execute action: SyncMail" );
            PairIDs selectedFolderIDs = PairIDs.Get( _selectedFolder );
            if ( !OutlookSession.FolderExists( selectedFolderIDs ) )
            {
                return;
            }
            foreach ( IResource resMail in selectedResources.ValidResources )
            {
                PairIDs messageIDs = PairIDs.Get( resMail );
                if ( messageIDs == null ) continue;

                IEMessage message =
                    OutlookSession.OpenMessage( messageIDs.EntryId, messageIDs.StoreId );
                if ( message == null ) continue;

                if ( selectedFolderIDs.StoreId.Equals( messageIDs.StoreId ) )
                {
                    DoMoveImpl(message, messageIDs, selectedFolderIDs);
                }
                else
                {
                    DoMoveBetweenStorages( resMail, message, messageIDs, selectedFolderIDs );
                }
            }
        }
        private void DoMoveBetweenStorages( IResource resMail, IEMessage message, PairIDs messageIDs, PairIDs selectedFolderIDs )
        {
            using ( message )
            {
                IEFolder folder =
                    OutlookSession.OpenFolder( selectedFolderIDs.EntryId, selectedFolderIDs.StoreId );
                if ( folder == null ) return;
                using ( folder )
                {
                    IEMessage newMessage = folder.CreateMessage( "IPM.note" );
                    using ( newMessage )
                    {
                        message.CopyTo( newMessage );
                        string entryID = newMessage.GetBinProp( MAPIConst.PR_ENTRYID );
                        OutlookSession.SaveChanges( true, "Save mail for moving between storages resource id = " + resMail.Id, newMessage, entryID );
                        if ( _copy )
                        {
                            return;
                        }

                        if ( !string.IsNullOrEmpty( entryID ) && !resMail.HasProp( -PROP.Attachment ) )
                        {
                            new ResourceProxy( resMail ).SetProp( PROP.EntryID, entryID );
                        }
                        OutlookSession.DeleteMessage( messageIDs.StoreId, messageIDs.EntryId, false );
                    }
                }
            }
        }

        private void DoMoveImpl(IEMessage message, PairIDs messageIDs, PairIDs selectedFolderIDs)
        {
            string parentID = string.Empty;
            using ( message )
            {
                parentID = message.GetBinProp( MAPIConst.PR_PARENT_ENTRYID );
            }
            IEFolder parentFolder =
                OutlookSession.OpenFolder( parentID, messageIDs.StoreId );
            if ( parentFolder == null ) return;

            using ( parentFolder )
            {
                IEFolder folder =
                    OutlookSession.OpenFolder( selectedFolderIDs.EntryId, selectedFolderIDs.StoreId );
                if ( folder == null ) return;
                using ( folder )
                {
                    if ( _copy )
                    {
                        parentFolder.CopyMessage( messageIDs.EntryId, folder );
                    }
                    else
                    {
                        parentFolder.MoveMessage( messageIDs.EntryId, folder );
                    }
                }
            }
        }
    }
    public class MoveFolderToFolderWithDialogAction : MoveMessageToFolderWithDialogAction
    {
        public MoveFolderToFolderWithDialogAction(){}
        public MoveFolderToFolderWithDialogAction( bool copy ) : base( copy ){}
        protected override void ExecuteAction( IResourceList selectedResources )
        {
            OutlookSession.OutlookProcessor.QueueJob( JobPriority.Immediate, "Move subfolder to another parent folder",
                new ResourceListDelegate( ExecuteActionImpl ), selectedResources );
        }

        protected override void ExecuteActionImpl( IResourceList selectedResources )
        {
            foreach ( IResource folder in selectedResources.ValidResources )
            {
                if ( folder.Id == _lastFolder.Id || Folder.IsDefault( folder ) || Folder.IsAncestor( _lastFolder, folder ) )
                {
                    StandartJobs.MessageBox( "Cannot move folder '" + folder.DisplayName +
                        "' to folder '" + _lastFolder.DisplayName + "'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    return;
                }
            }
            new MoveFolderToFolderAction().DoMove( _lastFolder, selectedResources );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count == 0 || context.SelectedResources[ 0 ].Type != STR.MAPIFolder )
            {
                presentation.Visible = false;
            }
        }
    }
    public class MoveMessageToFolderWithDialogAction : MoveMessageToFolderAction
    {
        protected IResource _lastFolder = null;
        public MoveMessageToFolderWithDialogAction(){}
        public MoveMessageToFolderWithDialogAction( bool copy ) : base( copy ){}

        protected override bool NeedProcess( IResourceList selectedResources )
        {
            SelectOutlookFolder dlg = SelectOutlookFolder.GetInstance( IsCopy );
            using ( dlg )
            {
                if ( _lastFolder != null )
                {
                    dlg.SelectFolder( _lastFolder );
                }
                if ( dlg.ShowDialog( Core.MainWindow ) == DialogResult.OK )
                {
                    IResource selectedFolder = dlg.SelectedFolderResource;
                    if ( selectedFolder != null )
                    {
                        _lastFolder = selectedFolder;
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void ExecuteAction( IResourceList selectedResources )
        {
            DoMove( _lastFolder, selectedResources );
        }
    }

}
