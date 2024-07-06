// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// The action to rename the resource currently selected in the active view.
    /// </summary>
    public class RenameAction: IAction
    {
        public void Execute( IActionContext context )
        {
            if ( context.CommandProcessor is IResourceBrowser )
            {
                (context.CommandProcessor as IResourceBrowser).EditResourceLabel( context.SelectedResources [0] );
            }
            else if ( context.CommandProcessor is IResourceTreePane )
            {
                (context.CommandProcessor as IResourceTreePane).EditResourceLabel( context.SelectedResources [0] );
            }
            else
            {
                context.CommandProcessor.ExecuteCommand( DisplayPaneCommands.RenameSelection );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count != 1 )
            {
                if ( context.Kind == ActionContextKind.MainMenu )
                {
                    presentation.Enabled = false;
                }
                else
                {
                    presentation.Visible = false;
                }
            }
            else
            if ( context.CommandProcessor is IResourceBrowser || Core.ResourceBrowser.NewspaperVisible )
            {
                presentation.Visible = false;
            }
            else
            if ( !context.CommandProcessor.CanExecuteCommand( DisplayPaneCommands.RenameSelection ) )
            {
                presentation.Visible = false;
//                presentation.Enabled = false;
            }
        }
    }
}
