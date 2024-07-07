// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using GUIControls.CustomViews;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls.CustomViews;
using JetBrains.Omea.FiltersManagement;
using JetBrains.UI.RichText;

namespace JetBrains.Omea.GUIControls
{
    #region Views
    //-------------------------------------------------------------------------
    //  Custom views management:
    //  - list views;
    //  - create new view;
    //  - edit existing view.
    //-------------------------------------------------------------------------
    public class ViewsManagerAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ViewsManagerForm form = new ViewsManagerForm();
            form.ShowDialog( Core.MainWindow );
            form.Dispose();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Kind == ActionContextKind.MainMenu);
        }
    }

    public class NewViewAction: IAction
    {
        public void Execute( IActionContext context )
        {
            //-----------------------------------------------------------------
            //  For several resource types which are considered "exclusive", we
            //  automatically pass this type as the default resource type into
            //  the view constructor.
            //-----------------------------------------------------------------
            string   exclusiveType = null;
            string[] currentTabTypes = Core.TabManager.CurrentTab.GetResourceTypes();
            if( currentTabTypes != null )
            {
                foreach( string type in currentTabTypes )
                {
                    if( Core.ResourceTreeManager.AreViewsExclusive( type ) )
                        exclusiveType = type;
                }
            }

            //-----------------------------------------------------------------
            EditViewForm  form = new EditViewForm( exclusiveType );
            if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
            {
                IResource res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "Name", form.HeadingText );
                if( context.SelectedResources.Count == 1 &&
                    context.SelectedResources[ 0 ].Type == FilterManagerProps.ViewFolderResName )
                {
                    ResourceProxy proxy = new ResourceProxy( res );
                    proxy.BeginUpdate();
                    proxy.SetProp( "Parent", context.SelectedResources[ 0 ] );
                    proxy.EndUpdate();
                }
                else
                    Core.ResourceTreeManager.LinkToResourceRoot( res, 2 );
                Core.LeftSidebar.DefaultViewPane.SelectResource( res );
            }
            form.Dispose();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if( context.Kind == ActionContextKind.ContextMenu )
            {
                presentation.Visible = (context.Instance == Core.LeftSidebar.DefaultViewPane) &&
                                       ((context.SelectedResources.Count == 0) ||
                                        ((context.SelectedResources.Count == 1) &&
                                         (context.SelectedResources[ 0 ].Type == FilterManagerProps.ViewResName ||
                                          context.SelectedResources[ 0 ].Type == FilterManagerProps.ViewFolderResName)));
            }
        }
    }

    public class EditViewOrRefineSearchAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource res = context.SelectedResources[ 0 ];
            if( res.GetStringProp( "DeepName" ) == Core.FilterRegistry.ViewNameForSearchResults )
            {
                SearchCtrl.ShowAdvancedSearchDialog( res );
            }
            else
            {
                EditViewForm form = new EditViewForm( res );
                if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
                {
                    IResource view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, form.HeadingText );
                    Core.UnreadManager.InvalidateUnreadCounter( view );
                    Core.LeftSidebar.DefaultViewPane.SelectResource( view );
                }
                form.Dispose();
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                IResource res = context.SelectedResources[ 0 ];
                if( res.GetStringProp( "DeepName" ) == Core.FilterRegistry.ViewNameForSearchResults )
                {
                    presentation.Text = "Refine this search...";
                }
                else
                {
                    presentation.Text = "Edit View...";
                }
            }
        }
    }

    public class CopyViewAction: IAction
    {
        public void Execute( IActionContext context )
        {
            IResource view = context.SelectedResources[ 0 ];

            //  Construct a name for a new view.
            string newName = "Copy of " + view.DisplayName;
            IResource res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, newName );
            if( res != null )
            {
                for( int i = 2;; i++ )
                {
                    newName = "Copy of " + view.DisplayName + "(" + i + ")";
                    res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, newName );
                    if( res == null )
                        break;
                }
            }

            int sortOrder = view.HasProp( "RootSortOrder" ) ? view.GetIntProp( "RootSortOrder" ) : 0;
            IResource newView = Core.FilterRegistry.CloneView( view, newName );

            Core.ResourceTreeManager.LinkToResourceRoot( newView, sortOrder );
            Core.LeftSidebar.DefaultViewPane.EditResourceLabel( newView );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Instance == Core.LeftSidebar.DefaultViewPane) &&
                                   (context.SelectedResources.Count == 1) &&
                                   (context.SelectedResources[ 0 ].Type == FilterManagerProps.ViewResName);
            if( presentation.Visible )
            {
                string deepName = context.SelectedResources[ 0 ].GetStringProp( "DeepName" );
                presentation.Visible = (deepName != Core.FilterRegistry.ViewNameForSearchResults );
            }
        }
    }

    public class ConvertView2RuleAction: IAction
    {
        public void Execute( IActionContext context )
        {
            IResource view = context.SelectedResources[ 0 ];
            IResource[][] conditions;
            IResource[]   exceptions;
            FilterRegistry.CloneConditionTypeLinks( view, out conditions, out exceptions );

            string[] types = FilterRegistry.CompoundType( view );
            Core.FilteringFormsManager.ShowEditActionRuleForm( view.GetStringProp( Core.Props.Name ), types,
                                                               conditions, exceptions, null );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Instance == Core.LeftSidebar.DefaultViewPane) &&
                                   (context.SelectedResources.Count == 1) &&
                                   (context.SelectedResources[ 0 ].Type == FilterManagerProps.ViewResName);
        }
    }

    public class DeleteViewAction: IAction
    {
        public void Execute( IActionContext context )
        {
            if( context.SelectedResources.Count > 1 )
            {
                if( MessageBox.Show( "Delete all selected views?", "Delete Views",
                                     MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation ) == DialogResult.Yes )
                {
                    for( int i = 0; i < context.SelectedResources.Count; i++ )
                        Core.FilterRegistry.DeleteView( context.SelectedResources[ i ] );
                }
            }
            else
            {
                string name = context.SelectedResources[ 0 ].GetPropText( Core.Props.Name );
                if( MessageBox.Show( "Delete view \"" + name + "\"?", "Delete View",
                                     MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation ) == DialogResult.Yes )
                    Core.FilterRegistry.DeleteView( context.SelectedResources[ 0 ] );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Instance == Core.LeftSidebar.DefaultViewPane);
            if( context.SelectedResources.Count >= 1 )
            {
                foreach( IResource res in context.SelectedResources )
                    presentation.Enabled = presentation.Enabled && res.Type == FilterManagerProps.ViewResName;
            }
            else
                presentation.Enabled = false;
        }
    }
    #endregion Views

    #region CustomViewsFolders
    //-------------------------------------------------------------------------
    //  Custom views folders
    //-------------------------------------------------------------------------

    public class NewViewFolderAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ResourceProxy proxy = ResourceProxy.BeginNewResource( FilterManagerProps.ViewFolderResName );
            proxy.BeginUpdate();

            string  newName = CreateNewName();
            proxy.SetProp( Core.Props.Name, newName );
            proxy.SetProp( "DeepName", newName );

            //  Link as subfolder?
            if( IsSingleViewFolder( context ) )
                proxy.SetProp( Core.Props.Parent, context.SelectedResources[ 0 ] );

            //  Set content type if we are in the tab for "exclusive"
            //  resource type, e.g. Contact or Task
            string[]  currTabResTypes = Core.TabManager.CurrentTab.GetResourceTypes();
            if( currTabResTypes != null )
            {
                bool  isViewExclusive = false;
                foreach( string resType in currTabResTypes )
                    isViewExclusive = isViewExclusive || Core.ResourceTreeManager.AreViewsExclusive( resType );
                if( isViewExclusive )
                    proxy.SetProp( Core.Props.ContentType, currTabResTypes[ 0 ] );
            }
            proxy.EndUpdate();

            //-----------------------------------------------------------------
            if( !IsSingleViewFolder( context ) )
                Core.ResourceTreeManager.LinkToResourceRoot( proxy.Resource, 1 );
            Core.LeftSidebar.ActivateViewPane( StandardViewPanes.ViewsCategories );
            Core.LeftSidebar.DefaultViewPane.EditResourceLabel( proxy.Resource );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if( context.Kind == ActionContextKind.ContextMenu )
            {
                presentation.Visible = (context.Instance == Core.LeftSidebar.DefaultViewPane) &&
                                       ((context.SelectedResources.Count == 0) ||
                                        ((context.SelectedResources.Count == 1) &&
                                         (context.SelectedResources[ 0 ].Type == FilterManagerProps.ViewFolderResName)));
            }
        }
        private static bool IsSingleViewFolder( IActionContext context )
        {
            return context.SelectedResources.Count == 1 &&
                   context.SelectedResources[ 0 ].Type == FilterManagerProps.ViewFolderResName;
        }

        private static string  CreateNewName()
        {
            string newName = "New View Folder";
            IResourceList views = Core.ResourceStore.FindResources( FilterManagerProps.ViewFolderResName, Core.Props.Name, newName );
            if( views.Count > 0 )
            {
                for( int i = 1;;i++ )
                {
                    newName = "New View Folder(" + i + ")";
                    views = Core.ResourceStore.FindResources( FilterManagerProps.ViewFolderResName, Core.Props.Name, newName );
                    if( views.Count == 0 )
                        break;
                }
            }
            return newName;
        }
    }

    public class DeleteViewFolderAction: IAction
    {
        public void Execute( IActionContext context )
        {
            int count = context.SelectedResources.Count;
            if( count == 1 )
            {
                IResource folder = context.SelectedResources[ 0 ];
                string name = folder.GetStringProp( Core.Props.Name );
                if( MessageBox.Show( "Delete View Folder \"" + name + "\" and all Views it contains?", "Views Manager",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation ) == DialogResult.Yes )
                    DeleteFolder( folder );
            }
            else
            {
                if( MessageBox.Show( "Delete " + count + " View Folders and all Views they contain?", "Views Manager",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation ) == DialogResult.Yes )
                {
                    lock( context.SelectedResources )
                    {
                        for( int i = count - 1; i >= 0; i-- )
                            DeleteFolder( context.SelectedResources[ i ] );
                    }
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Instance == Core.LeftSidebar.DefaultViewPane);
            presentation.Enabled = AllAreViewFolders( context );
        }

        private static void  DeleteFolder( IResource folder )
        {
            IResourceList descendants = folder.GetLinksTo( null, Core.Props.Parent );
            foreach( IResource res in descendants )
            {
                if( res.Type == FilterManagerProps.ViewFolderResName )
                    DeleteFolder( res );
                else
                    Core.FilterRegistry.DeleteView( res );
            }
            new ResourceProxy( folder ).Delete();
        }
        private static bool AllAreViewFolders( IActionContext context )
        {
            bool allConform = true;
            foreach( IResource res in context.SelectedResources )
                allConform = allConform && (res.Type == FilterManagerProps.ViewFolderResName);

            return allConform;
        }
    }
    #endregion CustomViewsFolders

    #region Rules
    //-------------------------------------------------------------------------
    //  Custom rules management:
    //  - list rules;
    //  - create new rule;
    //  - edit existing rule.
    //-------------------------------------------------------------------------
    public class EditRulesAction: IAction
    {
        public void Execute( IActionContext context )
        {
            RulesManagerForm form = new RulesManagerForm( "IsActionFilter" );
            form.ShowDialog( Core.MainWindow );
            form.Dispose();
        }
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled =
                (Core.ResourceStore.GetAllResources( FilterRegistry.RuleApplicableResourceTypeResName ).Count > 0);
        }
    }
    public class NewRuleAction: IAction
    {
        public void Execute( IActionContext context )
        {
            EditRuleForm form = new EditRuleForm();
            form.ShowDialog( Core.MainWindow );
            form.Dispose();
        }
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled =
                (Core.ResourceStore.GetAllResources( FilterRegistry.RuleApplicableResourceTypeResName ).Count > 0);
        }
    }

    /// <summary>
    /// Displays a dialog for editing a rule.
    /// <usage>Used as an click action in the Links pane.</usage>
    /// </summary>
    public class EditRuleAction : IAction
    {
        public void Execute( IActionContext context )
        {
            EditRuleForm form = new EditRuleForm( context.SelectedResources[ 0 ].DisplayName );
            form.ShowDialog();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.SelectedResources.Count == 1) &&
                                   (context.SelectedResources[ 0 ].Type == FilterManagerProps.RuleResName );
        }
    }

    public class ApplyRulesToAction: IAction
    {
        private delegate void ApplyRulesDelegate( IResourceList res, IResourceList rules );

        //  - collect a list of resource types corresponding to the selected
        //    resources
        //  - iterate through all avaiable rules, select only those which
        //    application type is equal or wider than elements in that list
        public void Execute( IActionContext context )
        {
            ApplyRulesForm form = new ApplyRulesForm( context.SelectedResourcesExpanded );
            if( form.ShowDialog() == DialogResult.OK )
            {
                IResourceList  resList = Core.ResourceStore.EmptyResourceList;
                switch( form.Order )
                {
                    case 0: resList = context.SelectedResources; break;
                    case 1: resList = Core.ResourceBrowser.VisibleResources; break;
                    case 2: resList = Core.TabManager.CurrentTab.GetFilterList( false ); break;
                    case 3: resList = CollectResources(); break;
                }
                Core.ResourceAP.QueueJob( JobPriority.AboveNormal, "Manually apply rules",
                                          new ApplyRulesDelegate( ApplyRules ), resList, form.SelectedRules );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = true;
        }

        private static void ApplyRules( IResourceList list, IResourceList rules )
        {
            foreach( IResource rule in rules )
            {
                Core.FilterEngine.ExecRule( rule, list );
            }
        }

        private static IResourceList  CollectResources()
        {
            IResourceList  result = Core.ResourceStore.EmptyResourceList;
            IResourceList  resTypes = Core.ResourceStore.GetAllResources( "ResourceType" );
            foreach( IResource resType in resTypes )
            {
                if( resType.GetIntProp( "NoIndex" ) == 0 )
                    result = result.Union( Core.ResourceStore.GetAllResources( resType.GetStringProp( Core.Props.Name ) ) );
            }
            return result;
        }
    }
    #endregion rules

    #region FormattingRules
    public class NewFormattingRuleAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            EditFormattingRuleForm form = new EditFormattingRuleForm();
            form.ShowDialog( Core.MainWindow );
            form.Dispose();
        }
    }

    public class EditFormattingRulesAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            RulesManagerForm form = new RulesManagerForm( "IsFormattingFilter" );
            form.ShowDialog( Core.MainWindow );
            form.Dispose();
        }
    }
    #endregion FormattingRules

    #region TrayIconRules
    public class NewTrayIconRuleAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            EditTrayIconRuleForm form = new EditTrayIconRuleForm();
            form.ShowDialog( Core.MainWindow );
            form.Dispose();
        }
    }

    public class EditTrayIconRulesAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            RulesManagerForm form = new RulesManagerForm( "IsTrayIconFilter" );
            form.ShowDialog( Core.MainWindow );
            form.Dispose();
        }
    }
    #endregion TrayIconRules

    #region Auto Expire Rules
    public class EditExpirationRulesAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            RulesManagerForm form = new RulesManagerForm( "IsExpirationFilter" );
            form.ShowDialog( Core.MainWindow );
            form.Dispose();
        }
    }

    public class EditExpirationRule : IAction
    {
        public void Execute( IActionContext context )
        {
            IResource linkedExpRule = context.SelectedResources[ 0 ].GetLinkProp( "ExpirationRuleLink" );
            for( int i = 1; i < context.SelectedResources.Count; i++ )
            {
                linkedExpRule = (linkedExpRule == null) ?
                                null : context.SelectedResources[ i ].GetLinkProp( "ExpirationRuleLink" );
            }

            Core.FilteringFormsManager.ShowExpirationRuleForm( context.SelectedResources, linkedExpRule );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Kind == ActionContextKind.ContextMenu) &&
                                   (context.SelectedResources.Count > 0);
        }
    }

    public class DeleteExpirationRule : IAction
    {
        public void Execute( IActionContext context )
        {
            //  Just delete links, rules with no linked folders are
            //  deleted automatically.
            for( int i = 0; i < context.SelectedResources.Count; i++ )
            {
                IResource container = context.SelectedResources[ i ];
                new ResourceProxy( container ).DeleteLinks( "ExpirationRuleLink" );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Kind == ActionContextKind.ContextMenu) &&
                                   (context.SelectedResources.Count > 0);
            presentation.Enabled = false;
            foreach( IResource res in context.SelectedResources )
                presentation.Enabled = presentation.Enabled || res.HasProp( "ExpirationRuleLink" );
            if ( context.Kind == ActionContextKind.ContextMenu && !presentation.Enabled )
            {
                presentation.Visible = false;
            }
        }
    }
    #endregion Auto Expire Rules

    #region Search View
    public abstract class SearchQueryAction: ActionOnSingleResource
    {
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            presentation.Visible = presentation.Visible &&
                                   context.SelectedResources[ 0 ].GetStringProp( "DeepName" ) ==
                                   Core.FilterRegistry.ViewNameForSearchResults;
        }
    }

    public class ShowDeletedResourcesAction : SearchQueryAction
    {
        public override void Execute( IActionContext context )
        {
            IResource view = context.SelectedResources[ 0 ];
            ResourceProxy proxy = new ResourceProxy( view );

            proxy.BeginUpdate();
            if( !view.HasProp( Core.Props.ShowDeletedItems ) )
                proxy.SetProp( Core.Props.ShowDeletedItems, true );
            else
                proxy.DeleteProp( Core.Props.ShowDeletedItems );
            Core.SettingStore.WriteBool( "Search", "ShowDeletedItems", !view.HasProp( Core.Props.ShowDeletedItems ) );
            proxy.SetProp( "ForceExec", true );
            proxy.EndUpdate();

            Core.UnreadManager.InvalidateUnreadCounter( view );
            Core.LeftSidebar.DefaultViewPane.SelectResource( view );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation ); // controls presentation.Enabled
            presentation.Checked = presentation.Visible &&
                                   context.SelectedResources[ 0 ].HasProp( Core.Props.ShowDeletedItems );
        }
    }

    public class ConvertSearch2ViewAction: SearchQueryAction
    {
        public override void Execute( IActionContext context )
        {
            IResource view = context.SelectedResources [0];
            string    newName = view.GetStringProp( Core.Props.Name ).Substring( 16 ); // strip "Search results: "
            IResourceList views = Core.ResourceStore.FindResources( FilterManagerProps.ViewResName, Core.Props.Name, newName );
            if( views.Count > 0 )
            {
                string  baseName = newName;
                for( int i = 1;; i++ )
                {
                    newName = baseName + "(" + i + ")";
                    views = Core.ResourceStore.FindResources( FilterManagerProps.ViewResName, Core.Props.Name, newName );
                    if( views.Count == 0 )
                        break;
                }
            }

            ResourceProxy proxy = new ResourceProxy( view );
            proxy.BeginUpdate();
            proxy.SetProp( Core.Props.Name, newName );
            proxy.SetProp( "DeepName", newName );
            proxy.DeleteProp( Core.Props.ShowDeletedItems );
            if( view.HasProp( Core.Props.ContentType ) || view.HasProp( "ContentLinks" ) )
                proxy.DeleteProp( "ShowInAllTabs" );
            proxy.EndUpdate();

            Core.LeftSidebar.DefaultViewPane.EditResourceLabel( view );
        }
    }

    public class SearchSelectedAction: IAction
    {
        public void Execute( IActionContext context )
        {
            SearchCtrl.CreateSearchView( context.SelectedText, false );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            bool hasSelectedText = ( context.SelectedText != null && !String.IsNullOrEmpty( context.SelectedPlainText ) );
            presentation.Visible = hasSelectedText && Core.TextIndexManager.IsIndexPresent() &&
                                   ( context.Kind == ActionContextKind.ContextMenu );
        }
    }
    #endregion Search View

    #region Mark All as Read
    public class MarkAllReadAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Core.ResourceAP.QueueJob(
                JobPriority.Immediate, new MarkAllReadJob( context.SelectedResources ) );
        }
    }

    internal class MarkAllReadJob: AbstractJob
    {
        readonly IResourceList _views;

        internal MarkAllReadJob( IResourceList views )
        {
            _views = views;
        }
        protected override void Execute()
        {
            string[]  currentResTypes = Core.TabManager.CurrentTab.GetResourceTypes();
            IResourceList unreadList = Core.ResourceStore.FindResourcesWithProp( null, Core.Props.IsUnread );
            foreach( IResource view in _views )
            {
                IResourceList  inView = Core.FilterEngine.ExecView( view );

                IResource ws = Core.WorkspaceManager.ActiveWorkspace;
                if ( ws != null )
                {
                    inView = inView.Intersect( Core.WorkspaceManager.GetFilterList( ws ), true );
                }
                inView = inView.Intersect( unreadList, true );
                for( int i = inView.Count - 1; i >= 0; --i )
                {
                    IResource res = inView[ i ];
                    if( currentResTypes == null || Array.IndexOf( currentResTypes, res.Type ) != -1 )
                    {
                        res.DeleteProp( "IsUnread" );
                    }
                }
            }
        }
    }
    #endregion Mark All as Read

    #region Emptying Recycle Bin
    public class ClearDeletedItemsViewAction : IAction
    {
        public void Execute( IActionContext context )
        {
            Core.ResourceAP.QueueJob( new ReenteringPersistentDeleter( context.SelectedResources[ 0 ] ) );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = presentation.Enabled =
                (context.Instance == Core.LeftSidebar.DefaultViewPane) &&
                (context.SelectedResources.Count == 1 ) &&
                (context.SelectedResources[ 0 ].Type == FilterManagerProps.ViewResName) &&
                (context.SelectedResources[ 0 ].GetStringProp( "DeepName" ) == FilterManagerProps.ViewDeletedItemsDeepName);

            //  Resources of some types can not be deleted permanently, e.g.
            //  Contacts and IM conversations. Thus this menu must be forbidden
            //  for the view in the corresponding tabs.
            if( presentation.Visible )
            {
	            string[] types = Core.TabManager.CurrentTab.GetResourceTypes();

                //  "All Resources" tab always returns NULL
                if( types != null )
                {
                    bool     canDelete = false;
                    foreach( string type in types )
                    {
	                    if( type.Length > 0 && Core.ResourceStore.ResourceTypes.Exist( type ) )
	                    {
	                        IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( type );
	                        if( deleter != null )
	                        {
	                            canDelete = canDelete || deleter.CanDeleteResource( null, true );
	                        }
                        }
                    }
                    presentation.Visible = canDelete;
                }
            }
        }
    }

    internal class ReenteringPersistentDeleter : ReenteringEnumeratorJob
    {
        readonly IResource  _savedView;

        IntArrayList    ResourceIds;
        int             Index;

        internal ReenteringPersistentDeleter( IResource view )
        {
            _savedView = view;
        }

        public override string Name
        {
            get { return "Performing emptying the Deleted Resources"; }
        }

        public override void  EnumerationStarting()
        {
            IResourceList  inView = Core.FilterEngine.ExecView( _savedView );
            inView = inView.Intersect( Core.ResourceBrowser.FilterResourceList, true );
            ResourceIds = new IntArrayList( inView.ResourceIds );
            Index = 0;
        }
        public override void EnumerationFinished() {}
        public override AbstractJob GetNextJob()
        {
            //  test anchor.
            if( Index >= ResourceIds.Count )
                return null;

            IResource res = Core.ResourceStore.TryLoadResource( ResourceIds[ Index++ ] );
            if ( res != null )
            {
                IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( res.Type );
                if( deleter != null )
                {
                    return new DelegateJob( new ResourceDelegate( deleter.DeleteResourcePermanent ), new object[] { res } );
                }
            }
            return GetNextJob();
        }
    }
    #endregion Emptying Recycle Bin

    public class RefreshViewAction: IAction
    {
        public void Execute( IActionContext context )
        {
            AbstractViewPane viewPane = Core.LeftSidebar.GetPane( StandardViewPanes.ViewsCategories );
            if ( viewPane != null )
            {
                viewPane.UpdateSelection();
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResource res = Core.ResourceBrowser.OwnerResource;

            if ( context.Kind == ActionContextKind.ContextMenu )
            {
                if ( res == null || res.Type != FilterManagerProps.ViewResName || !context.SelectedResources.Contains( res ) )
                {
                    presentation.Visible = false;
                }
            }
            else
            {
                if ( res == null || res.Type != FilterManagerProps.ViewResName )
                {
                    presentation.Enabled = false;
                }
            }
        }
    }

    public class WatchThreadAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            FormattingForm form = new FormattingForm( context.SelectedResources[ 0 ] );
            form.ShowDialog( Core.MainWindow );
        }
    }
    public class ToggleShowTotalCountAction : IAction
    {
        public void Execute( IActionContext context )
        {
            foreach( IResource res in context.SelectedResources )
            {
                ResourceProxy proxy = new ResourceProxy( res );
                proxy.BeginUpdate();
                if( res.HasProp( Core.Props.ShowTotalCount ) )
                    proxy.DeleteProp( Core.Props.ShowTotalCount );
                else
                    proxy.SetProp( Core.Props.ShowTotalCount, true );
                proxy.EndUpdate();
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            bool allSet = false;
            presentation.Enabled = (context.SelectedResources.Count > 0);
            if( presentation.Enabled )
            {
                bool allEqual = true;
                allSet = context.SelectedResources[ 0 ].HasProp( Core.Props.ShowTotalCount );
                for( int i = 1; i < context.SelectedResources.Count; i++ )
                {
                    allEqual = allEqual && (context.SelectedResources[ i ].HasProp( Core.Props.ShowTotalCount ) == allSet);
                }
                presentation.Enabled = allEqual;
            }

            if( presentation.Enabled )
            {
                presentation.Checked = allSet;
            }
        }
    }

    public class TotalCountDecorator : IResourceNodeDecorator
    {
        private static readonly IResourceList   _allDel = Core.ResourceStore.FindResourcesWithPropLive( null, Core.Props.IsDeleted );

        private TextStyle       _textStyle;
        private IResourceList   _allLiveContainers;
        private readonly string _resourceNodeType;
        private readonly int    _itemLink;
        private readonly int    _folderLink;

        public TotalCountDecorator( string resourceNodeType, int itemLink )
        {
            _resourceNodeType = resourceNodeType;
            _itemLink = itemLink;
            _folderLink = -1;
            Init();
        }

        public TotalCountDecorator( string resourceNodeType, int itemLink, int parentFolderLink )
        {
            _resourceNodeType = resourceNodeType;
            _itemLink = itemLink;
            _folderLink = parentFolderLink;
            Init();
        }

        private void Init()
        {
            _textStyle = new TextStyle( FontStyle.Regular, Color.Green, SystemColors.Window );
            _allLiveContainers = Core.ResourceStore.FindResourcesWithPropLive( null, Core.Props.ShowTotalCount );
            _allLiveContainers.AddPropertyWatch( _itemLink );
            if( _folderLink >= 0 && _folderLink != _itemLink )
            {
                _allLiveContainers.AddPropertyWatch( _itemLink );
            }
            _allLiveContainers.ResourceChanged += OnContainerChanged;
        }

        public event ResourceEventHandler DecorationChanged;
        public string DecorationKey { get{ return "TotalItems"; } }

        public bool DecorateNode( IResource res, RichText nodeText )
        {
            if( res.HasProp( Core.Props.ShowTotalCount ))
            {
                IResource wsp = Core.WorkspaceManager.ActiveWorkspace;
                IResourceList total = GetNodeResourceList( res );
                if( wsp != null )
                {
                    total = total.Intersect( wsp.GetLinksOfType( null, "WorkspaceVisible" ), true );
                }
                if( total.Count != 0 )
                {
                    nodeText.Append( " " );
                    nodeText.Append( "[" + total.Count + "]", _textStyle );
                }
                return true;
            }
            return false;
        }

        private IResourceList GetNodeResourceList( IResource res )
        {
            IResourceList result = Core.ResourceStore.EmptyResourceList;
            if( res.Type == _resourceNodeType )
            {
                result = res.GetLinksOfType( null, _itemLink );
            }
            if( _folderLink >= 0 )
            {
                foreach( IResource child in res.GetLinksTo( null, _folderLink ) )
                {
                    result = result.Union( GetNodeResourceList( child ) );
                }
            }
            if( !res.HasProp( "ShowDeletedItems" ))
            {
                result = result.Minus( _allDel );
            }
            return result;
        }

        private void OnContainerChanged( object sender, ResourcePropIndexEventArgs args )
        {
            IPropertyChangeSet set = args.ChangeSet;
            if( set.IsPropertyChanged( _itemLink ) || ( _folderLink >= 0 && set.IsPropertyChanged( _folderLink ) ) )
            {
                if( DecorationChanged != null )
                {
                    DecorationChanged( this, new ResourceEventArgs( args.Resource ) );
                }
            }
        }
    }
}
