/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
