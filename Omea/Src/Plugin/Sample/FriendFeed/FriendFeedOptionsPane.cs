// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.FriendFeed
{
    public partial class FriendFeedOptionsPane : AbstractOptionsPane
    {
        public FriendFeedOptionsPane()
        {
            InitializeComponent();
        }

        public override void ShowPane()
        {
            base.ShowPane();
            _edtNickname.Text = Core.SettingStore.ReadString( "FriendFeed", "Nickname" );
            _edtRemoteKey.Text = Core.SettingStore.ReadString( "FriendFeed", "RemoteKey" );
        }

        public override void OK()
        {
            base.OK();
            Core.SettingStore.WriteString( "FriendFeed", "Nickname", _edtNickname.Text );
            Core.SettingStore.WriteString( "FriendFeed", "RemoteKey", _edtRemoteKey.Text );
        }

        private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            Process.Start( "http://friendfeed.com/remotekey" );
        }
    }
}
