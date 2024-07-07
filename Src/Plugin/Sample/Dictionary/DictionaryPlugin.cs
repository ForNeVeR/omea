// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Web;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.Dictionary
{
	/// <summary>
	/// Sample plugin for JetBrains Omea which allows to look up the selected word
	/// at dictionary.reference.com.
	/// Copyright (C) 2004 by JetBrains s.r.o. All Rights Reserved.
	/// </summary>
	public class DictionaryPlugin: IPlugin
	{
        /// <summary>
        /// The first method called during plugin startup. Registers the context
        /// menu action.
        /// </summary>
        public void Register()
	    {
            // Register the action group where the action will be placed.
            const string actionGroupName = "DictionaryPlugin.Actions";
            Core.ActionManager.RegisterContextMenuActionGroup( actionGroupName, ListAnchor.Last );

            // Register the action. The action applies to all resource types,
            // and no special filters are required.
            Core.ActionManager.RegisterContextMenuAction( new DictionaryLookupAction(),
                actionGroupName, ListAnchor.Last, "Look Up in Dictionary", null, null );
	    }

	    /// <summary>
	    /// The method which is called after the Register() method has been called
	    /// for all plugins. Nothing to do here.
	    /// </summary>
        public void Startup()
	    {
	    }

	    /// <summary>
	    /// The method called at OmniaMea shutdown. Nothing to do here.
	    /// </summary>
        public void Shutdown()
	    {
	    }
	}

    /// <summary>
    /// The action to perform the dictionary lookup.
    /// </summary>
    public class DictionaryLookupAction : IAction
    {
        /// <summary>
        /// Executes the lookup at the dictionary Web site.
        /// </summary>
        /// <param name="context"></param>
        public void Execute( IActionContext context )
        {
            string query = HttpUtility.UrlEncode( context.SelectedPlainText.Trim() );
            Process.Start( "http://dictionary.reference.com/search?q=" + query );
        }

        /// <summary>
        /// Enable the action only if there is selected text which can be looked up.
        /// </summary>
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = ( context.SelectedPlainText != null &&
                context.SelectedPlainText.Length > 0 );
        }
    }
}
