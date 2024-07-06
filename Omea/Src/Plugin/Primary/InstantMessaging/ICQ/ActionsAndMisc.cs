// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.InstantMessaging.ICQ
{
    internal class ConversationLinkClickAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            IResource conversation = context.SelectedResources [0];
            IResource fromContact = conversation.GetLinkProp( "From" );
            if ( fromContact.HasProp( "MySelf" ) )
            {
                fromContact = conversation.GetLinkProp( "To" );
            }

            Core.UIManager.BeginUpdateSidebar();
            Core.TabManager.CurrentTabId = "IM";
            Core.LeftSidebar.ActivateViewPane( "ICQCorrespondents" );
            Core.UIManager.EndUpdateSidebar();
            ICQPlugin._correspondentPane.SelectResource( fromContact, false );
            Core.ResourceBrowser.SelectResource( context.SelectedResources [0] );
        }
    }

    public class RebuildConversationsAction : SimpleAction
    {
        public override void Execute( IActionContext context  )
        {
            RebuildForm form = new RebuildForm();
            form.ShowDialog( Core.MainWindow );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation)
        {
            presentation.Visible = context.Kind == ActionContextKind.MainMenu &&
                  ( Core.TabManager.CurrentTabId == "IM" || Core.TabManager.CurrentTabId == "All" );
        }
    }

    public class ICQAccountClickAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            Core.UIManager.BeginUpdateSidebar();
            Core.TabManager.CurrentTabId = "IM";
            Core.UIManager.EndUpdateSidebar();

            IResource account = context.SelectedResources [0];
            IResourceList resList = account.GetLinksOfType( null, ICQPlugin._propFromICQ );
            resList = resList.Union( account.GetLinksOfType( null, ICQPlugin._propToICQ ) );

            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            options.Caption = "Messages for " + account.DisplayName;
            options.SetTransientContainer( Core.ResourceTreeManager.ResourceTreeRoot,
                StandardViewPanes.ViewsCategories );
            Core.ResourceBrowser.DisplayResourceList( null, resList, options );
        }
    }

    internal class ICQConversationDeleter : DefaultResourceDeleter
    {
        public override bool CanDeleteResource( IResource res, bool permanent )
        {
            return !permanent;
        }

        /**
         * permanent deletion of icq conversations is fobidden
         */
        public override void DeleteResourcePermanent( IResource res )
        {
        }
    }
}
