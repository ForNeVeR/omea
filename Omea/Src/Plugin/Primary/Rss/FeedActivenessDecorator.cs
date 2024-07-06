// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.RichText;

namespace JetBrains.Omea.RSSPlugin
{
    internal class FeedActivenessDecorator: IResourceNodeDecorator
    {
        public event ResourceEventHandler DecorationChanged;

        public bool DecorateNode( IResource res, RichText nodeText )
        {
            if ( (res.Type == Props.RSSFeedResource || res.Type == Props.RSSFeedGroupResource) && res.HasProp( Props.IsPaused ) )
            {
                nodeText.SetColors( Color.Gray, SystemColors.Window );
                return true;
            }
            return false;
        }

        public string DecorationKey
        {
            get { return "FeedActiveness"; }
        }
    }
}
