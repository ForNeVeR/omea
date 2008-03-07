/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    public class AddToShortcutsAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            ShortcutBar.GetInstance().AddShortcutsFromList( context.SelectedResources );
            Core.UIManager.ShortcutBarVisible = true;
        }
    }

    public class OrganizeShortcutsAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ShortcutBar.GetInstance().OrganizeShortcuts();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = ShortcutBar.GetInstance() != null &&
                ShortcutBar.GetInstance().ShortcutCount > 0;
        }
    }
}
