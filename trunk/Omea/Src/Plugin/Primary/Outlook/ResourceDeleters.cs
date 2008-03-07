/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class ResourceDeleters
    {
        static public void Register()
        {
            Core.PluginLoader.RegisterResourceDeleter( STR.Email, new MailItemDeleter() );
        }
    }
    internal class MailItemDeleter: DefaultResourceDeleter
    {
        public override void UndeleteResource( IResource res )
        {
            Guard.NullArgument( res, "res" );
            IResourceList attachments = res.GetLinksOfType( null, PROP.Attachment );

            foreach ( IResource attachment in attachments.ValidResources )
            {
                attachment.SetProp( Core.Props.IsDeleted, false );
            }

            IResource folder = null;
            MAPIIDs IDs = OutlookSession.GetInboxIDs();
            if ( IDs != null )
            {
                folder = Folder.Find( IDs.EntryID );
            }

            if ( folder != null )
            {
                MoveMessageToFolderAction action = new MoveMessageToFolderAction( false );
                action.DoMove( folder, res.ToResourceList() );
            }
        }
        public override void DeleteResourcePermanent( IResource res )
        {
            Guard.NullArgument( res, "res" );
            DeleteMail( res, PairIDs.Get( res ), false );
        }
        public override void DeleteResource( IResource res )
        {
            Guard.NullArgument( res, "res" );
            DeleteMail( res, PairIDs.Get( res ), true );
        }
        public static void DeleteMail( IResource mail, PairIDs pairIDs, bool deletedItems )
        {
            if ( deletedItems )
            {
                IResource folder = mail.GetLinkProp( PROP.MAPIFolder );
                if ( folder != null )
                {
                    deletedItems = !Folder.IsIMAPFolder( folder );
                }
            }
            if ( pairIDs != null )
            {
                OutlookSession.DeleteMessage( pairIDs.StoreId, pairIDs.EntryId, deletedItems );
            }
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceDelegate( Mail.Delete ), mail );
        }
    }
}