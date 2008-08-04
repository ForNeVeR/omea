/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using OmniaMea.Categories;

namespace JetBrains.Omea.Categories
{
    /**
     * Action to edit the categories of a resource.
     */

    public class EditCategoriesAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            if ( context.SelectedResources.Count > 0 )
            {
                DialogResult dres = Core.UIManager.ShowAssignCategoriesDialog( Core.MainWindow, context.SelectedResources );
                if( dres == DialogResult.OK )
                {
                    foreach( IResource res in context.SelectedResources )
                    {
                        Core.FilterEngine.ExecRules( StandardEvents.CategoryAssigned, res );
                    }
                }
            }
        }
    }

    /**
     * Action to create a new category.
     */

    public class NewCategoryAction: IAction
    {
        public void Execute( IActionContext context )
        {
            IResource defaultParent = null;
            string defaultContentType = null;
            if ( context.SelectedResources.Count > 0 )
            {
                defaultParent = context.SelectedResources [0];
                defaultContentType = defaultParent.GetStringProp( Core.Props.ContentType );
            }

            using( NewCategoryDlg dlg = new NewCategoryDlg() )
            {
                IResource newCategory = dlg.ShowNewCategoryDialog( Core.MainWindow, "", defaultParent, defaultContentType );
                if ( newCategory != null )
                {
                    Core.LeftSidebar.DefaultViewPane.ExpandParents( newCategory );
                    Core.LeftSidebar.DefaultViewPane.SelectResource( newCategory );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.Kind == ActionContextKind.ContextMenu )
            {
                if ( context.SelectedResources.Count == 1 && context.SelectedResources [0].Type == "Category" )
                    return;

                if ( context.SelectedResources.Count == 1 && context.SelectedResources [0].Type == "ResourceTreeRoot" && 
                    context.SelectedResources [0].GetPropText( "RootResourceType" ).StartsWith( "Category" ) )
                {
                    return;
                }

                if ( context.Instance == Core.LeftSidebar.DefaultViewPane && context.SelectedResources.Count == 0 )
                {
                    return;
                }

                presentation.Visible = false;
            }
        }
    }

    /**
     * Action to delete a category.
     */

    public class DeleteCategoryAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            if ( CategoryManager.ConfirmDeleteCategories( Core.MainWindow, context.SelectedResources ) )
            {
                Core.ResourceAP.QueueJob( new ResourceListDelegate( CategoryManager.DeleteCategories ),
                    context.SelectedResources );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( context.LinkTargetResource != null )
            {
                presentation.Visible = false;  // show "Remove from Category" instead
            }
        }
    }

    /**
     * Removes the category clicked in the Links pane from the resource.
     */

    public class RemoveFromCategoryAction: IAction
    {
        public void Execute( IActionContext context )
        {
            if ( context.LinkTargetResource != null )
            {
				Core.CategoryManager.RemoveResourceCategory( context.LinkTargetResource, 
                    context.SelectedResources [0] );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count != 1 || context.LinkTargetResource == null )
            {
                presentation.Visible = false;
            }
        }
    }

    /**
     * Marks all currently visible resources in the category as read.
     */

    public class MarkCategoryAsReadAction: ResourceAction
    {
        public override void Execute( IResourceList selectedResources )
        {
            foreach( IResource category in selectedResources )
            {
                IResourceList categoryResources = category.GetLinksOfType( null, 
                    (Core.CategoryManager as CategoryManager).PropCategory );
                categoryResources = categoryResources.Intersect( Core.TabManager.CurrentTab.GetFilterList( false ), true );
                categoryResources = categoryResources.Intersect( Core.ResourceStore.FindResourcesWithProp( null, "IsUnread" ), true );
                foreach( IResource res in categoryResources )
                {
                    res.SetProp( "IsUnread", false );
                }
            }
        }
    }

    public class ToggleCategoryContentsRecurseAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            CategoryManager cman = (CategoryManager) Core.CategoryManager;
            bool newState = !AllCategoriesShownRecursively( context );
            foreach( IResource res in context.SelectedResources )
            {
                if ( res.GetLinksTo( "Category", Core.Props.Parent ).Count > 0 )
                {
                    new ResourceProxy( res ).SetProp( cman.PropShowContentsRecursively, newState );
                    if ( res == Core.LeftSidebar.DefaultViewPane.SelectedNode )
                    {
                        // ensure the display is updated
                        Core.LeftSidebar.DefaultViewPane.SelectResource( res );
                    }
                }
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( presentation.Visible )
            {
                presentation.Checked = AllCategoriesShownRecursively( context );
                if ( !presentation.Checked )
                {
                    presentation.Enabled = AnyCategoriesHaveChildren( context );
                }
            }
        }

        private static bool AllCategoriesShownRecursively( IActionContext context )
        {
            CategoryManager cman = (CategoryManager) Core.CategoryManager;
            foreach( IResource res in context.SelectedResources )
            {
                if ( !res.HasProp( cman.PropShowContentsRecursively ) )
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AnyCategoriesHaveChildren( IActionContext context )
        {
            foreach( IResource res in context.SelectedResources )
            {
                if ( res.GetLinksTo( "Category", Core.Props.Parent ).Count > 0 )
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class ChangeCategoryIconAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource category = context.SelectedResources[ 0 ];
            ChooseIconForm form = new ChooseIconForm( category );
            if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
            {
                CategoryIconProvider provider = (CategoryIconProvider)Core.ResourceIconManager.GetResourceIconProvider( "Category" );
                provider.UpdateIcon( category );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
        }
    }
}