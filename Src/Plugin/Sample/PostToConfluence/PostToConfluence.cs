// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.PostToConfluence
{
	/// <summary>
	/// The plugin for posting the text of the selected resource as a page in
	/// Confluence.
	/// </summary>
	public class PostToConfluencePlugin: IPlugin
    {
	    public void Register()
	    {
            Core.ActionManager.RegisterMainMenuActionGroup( "ConfluenceActions", "Actions", ListAnchor.Last );
            Core.ActionManager.RegisterMainMenuAction( new PostToConfluenceAction(),
                "ConfluenceActions", ListAnchor.Last, "Post to Confluence...", null, null );
	    }

	    public void Startup()
	    {
	    }

	    public void Shutdown()
	    {
	    }
    }

    public class PostToConfluenceAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            IResource resourceToPost = null;
            if ( context.SelectedResources.Count > 0 )
            {
                resourceToPost = context.SelectedResources [0];
            }
            PostDialog.PostToConfluence( resourceToPost, context.SelectedPlainText );
        }
    }
}
