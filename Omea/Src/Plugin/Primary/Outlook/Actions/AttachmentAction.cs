// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    /**
     * Base class for actions working with attachments.
     */

    public abstract class AttachmentAction: IAction
    {
        public virtual void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( !REGISTRY.IsRegistered )
            {
                //Outlook plugin was not load
                presentation.Visible = false;
                presentation.Enabled = false;
                return;
            }
            if ( context.SelectedResources.Count == 1 )
            {
                IResource resource = context.SelectedResources[0];
                IResource maybeMail = resource.GetLinkProp( PROP.Attachment );
                if ( maybeMail != null && maybeMail.Type == STR.Email && !resource.HasProp( PROP.EmbeddedMessage ) )
                {
                    presentation.Visible = true;
                    return;
                }
            }
            presentation.Visible = false;
            return;
        }

        protected OutlookAttachment GetAttachment( IResourceList resourceList )
        {
            if ( resourceList == null || resourceList.Count == 0 ) return null;
            return GetAttachment( resourceList[0] );
        }

        protected OutlookAttachment GetAttachment( IResource resource )
        {
            try
            {
                return new OutlookAttachment( resource );
            }
            catch ( OutlookAttachmentException exception )
            {
                Tracer._TraceException( exception );
                return null;
            }
        }

        public abstract void Execute( IActionContext context );
    }

    public class OpenAttachmentAction: AttachmentAction
    {
        public override void Execute( IActionContext context )
        {
            foreach( IResource attachment in context.SelectedResources )
            {
                Core.FileResourceManager.OpenSourceFile( attachment );
            }
        }
    }
    public class UnpackResourcesAction: AttachmentAction
    {
        public void Unpack( IResource attachment )
        {
            try
            {
                OutlookAttachment att = GetAttachment( attachment );
                string fullFileName =
                    Path.Combine( Path.GetTempPath(), ResourceSerializer.ResourceTransferFileName );
                att.SaveAs( fullFileName );
                IResource mail = (IResource)attachment.GetProp( PROP.InternalAttachment );
                ReceiveResourcesDialog dialog = new ReceiveResourcesDialog( fullFileName, mail );
                File.Delete( fullFileName );
                dialog.ShowDialog();
            }
            catch ( Exception exception )
            {
                Tracer._TraceException( exception );
                MsgBox.Error( "Outlook plugin", "Impossible to unpack resources after transfer\n" + exception.Message );
                return;
            }
        }
        public override void Execute( IActionContext context )
        {
            if ( context.SelectedResources.Count == 0 ) return;
            Unpack( context.SelectedResources[0] );
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count < 1 )
            {
                presentation.Visible = false;
                return;
            }
            IResource res = context.SelectedResources [0];
            presentation.Visible = res.HasProp( PROP.ResourceTransfer );
        }
    }
}
