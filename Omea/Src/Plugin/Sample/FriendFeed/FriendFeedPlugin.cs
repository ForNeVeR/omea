// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using FriendFeed;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.FriendFeed
{
    public class FriendFeedPlugin: IPlugin
    {
        public void Register()
        {
            Core.UIManager.RegisterOptionsPane( "Internet", "FriendFeed",
                delegate { return new FriendFeedOptionsPane(); }, "The FriendFeed options pane allows you to specify your FriendFeed login and password");
            Core.ActionManager.RegisterContextMenuActionGroup( "ShareActions", ListAnchor.Last );
            Core.ActionManager.RegisterContextMenuAction( new ShareAction(), "ShareActions", ListAnchor.First, "Share on FriendFeed", null, "RSSItem", null );
        }

        public void Startup()
        {
        }

        public void Shutdown()
        {
        }
    }

    public class ShareAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            string nickName = Core.SettingStore.ReadString( "FriendFeed", "Nickname" );
            string remoteKey = Core.SettingStore.ReadString( "FriendFeed", "RemoteKey" );
            if ( String.IsNullOrEmpty( nickName ) || String.IsNullOrEmpty( remoteKey ) )
            {
                Core.UIManager.ShowOptionsDialog( "Internet", "FriendFeed" );
                return;
            }

            ShareDialog dlg = new ShareDialog();
            dlg.ShowResource( context.SelectedResources[0] );
            if ( dlg.ShowDialog( context.OwnerForm ) != DialogResult.OK ) return;

            IResource res = context.SelectedResources[0];
            string title = dlg.Title;
            string comment = dlg.Comment;
            string link = res.GetStringProp( "Link" );
            Core.NetworkAP.QueueJob( (PostDelegate) DoPost, title, link, comment );
        }

        private delegate void PostDelegate( string title, string link, string comment );

        private static void DoPost( string title, string link, string comment )
        {
            string nickName = Core.SettingStore.ReadString( "FriendFeed", "Nickname" );
            string remoteKey = Core.SettingStore.ReadString( "FriendFeed", "RemoteKey" );
            FriendFeedClient client = new FriendFeedClient( nickName, remoteKey );

            if ( String.IsNullOrEmpty( comment ) )
            {
                client.PublishLink( title, link );
            }
            else
            {
                client.PublishLink( title, link, comment );
            }

        }
    }
}
