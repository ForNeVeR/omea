// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// An action filter which hides or disables the action if all the selected resources
    /// have the internal flag.
    /// </summary>
    public class InternalResourceFilter: IActionStateFilter
    {
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count == 0 ||
                ResourceTypeHelper.AnyResourcesInternal( context.SelectedResources ) )
            {
                if ( context.Kind == ActionContextKind.Toolbar || context.Kind == ActionContextKind.MainMenu )
                {
                    presentation.Enabled = false;
                }
                else
                {
                    presentation.Visible = false;
                }
            }
        }
    }

    public class TextIndexPresentFilter: IActionStateFilter
    {
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = Core.TextIndexManager.IsIndexPresent();
        }
    }

    public class SingleResourceChosen: IActionStateFilter
    {
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = (context.SelectedResources.Count == 1);
        }
    }

    public class SearchViewChosen: IActionStateFilter
    {
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = context.SelectedResources.Count > 0 &&
                                   context.SelectedResources[ 0 ].GetStringProp( "DeepName" ) ==
                                        ICore.Instance.FilterRegistry.ViewNameForSearchResults;
        }
    }

    public class NoSelectedResourceFilter: IActionStateFilter
    {
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count != 0 )
            {
                presentation.Visible = false;
            }
        }
    }

    public class MainWindowFilter: IActionStateFilter
    {
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.OwnerForm != null && context.OwnerForm != Core.MainWindow )
            {
                presentation.Enabled = false;
            }
        }
    }

    public class ActiveTabFilter: IActionStateFilter
    {
        private string _tabID;
        private bool _acceptAll;

        public ActiveTabFilter( string tabID, bool acceptAll )
        {
            _tabID = tabID;
            _acceptAll = acceptAll;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.Kind != ActionContextKind.ContextMenu )
            {
                string tabId = Core.TabManager.CurrentTabId;
                if ( tabId != _tabID && (!_acceptAll || tabId != "All" ) )
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
            }
        }
    }
}
