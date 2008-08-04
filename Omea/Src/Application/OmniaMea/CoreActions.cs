/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using GUIControls;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.CustomProperties;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
    /// <summary>
    /// The action for showing the Advanced Search dialog.
    /// </summary>
    public class AdvancedSearchAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            (Core.MainWindow as MainFrame).ShowAdvancedSearchDialog();
        }
    }

    public class BackAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ResourceBrowser browser = (ResourceBrowser) Core.ResourceBrowser;
            browser.GoBack();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            ResourceBrowser browser = (ResourceBrowser) Core.ResourceBrowser;
            presentation.Enabled = browser.CanBack();
        }
    }

    public class ForwardAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ResourceBrowser browser = (ResourceBrowser) ICore.Instance.ResourceBrowser;
            browser.GoForward();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            ResourceBrowser browser = (ResourceBrowser) ICore.Instance.ResourceBrowser;
            presentation.Enabled = browser.CanForward();
        }
    }

    /**
     * Action to execute a display pane action in the resource browser.
     */

    public class DisplayPaneAction: IAction
    {
        private readonly string _command;
        private readonly bool   _hideIfDisabled;

        public DisplayPaneAction( string command )
        {
            _command = command;
            _hideIfDisabled = false;
        }

        public DisplayPaneAction( string command, bool hideIfDisabled )
        {
            _command = command;
            _hideIfDisabled = hideIfDisabled;
        }

        public void Execute( IActionContext context )
        {
            context.CommandProcessor.ExecuteCommand( _command );
        }

        public virtual void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( !context.CommandProcessor.CanExecuteCommand( _command ) )
            {
                if ( _hideIfDisabled || context.Kind == ActionContextKind.ContextMenu )
                {
                    presentation.Visible = false;
                }
                else
                {
                    presentation.Enabled = false;
                }
            }
        }

        public override string ToString()
        {
            return "DisplayPaneAction/" + _command;
        }

        public override bool Equals( object obj )
        {
            DisplayPaneAction rhs = obj as DisplayPaneAction;
            if ( rhs == null )
            {
                return false;
            }
            return _command == rhs._command;
        }

        public override int GetHashCode()
        {
            return _command.GetHashCode();
        }
    }

    public class CopyAction: IAction
    {
        public void Execute( IActionContext context )
        {
            context.CommandProcessor.ExecuteCommand( DisplayPaneCommands.Copy );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.Kind == ActionContextKind.ContextMenu )
            {
                presentation.Visible = context.CommandProcessor.CanExecuteCommand( DisplayPaneCommands.Copy ) &&
                    context.SelectedText != null && context.SelectedText.Length > 0;
            }
            else
            {
                presentation.Enabled = context.CommandProcessor.CanExecuteCommand( DisplayPaneCommands.Copy );
            }
        }
    }

    /**
     * Action to execute a display pane action from the IEBrowser context menu.
     */

    public class IEBrowserAction: DisplayPaneAction
    {
        public IEBrowserAction( string action )
            : base( action ) {}

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.CurrentUrl == null )
            {
                if ( context.Kind == ActionContextKind.MainMenu )
                {
                    presentation.Enabled = false;
                }
                else
                {
                    presentation.Visible = false;
                }
                return;
            }
            base.Update( context, ref presentation );
        }
    }

    /**
     * Action to mark the selected resources as read or unread.
     */

    public class MarkAsReadAction: IAction
    {
        private readonly bool _unreadValue;
        
        public MarkAsReadAction( bool unreadValue )
        {
            _unreadValue = unreadValue;
        }

        public void Execute( IActionContext context )
        {
            Core.ResourceAP.QueueJob(
                JobPriority.Immediate, "Marking as Read/Unread",
                new ResourceListDelegate( ExecuteAction ), context.SelectedResourcesExpanded );
        }

        private void ExecuteAction( IResourceList selectedResources )
        {
            foreach( IResource res in selectedResources.ValidResources )
            {
                if ( Core.ResourceStore.ResourceTypes [res.Type].HasFlag( ResourceTypeFlags.CanBeUnread ) )
                {
                    res.SetProp( Core.Props.IsUnread, _unreadValue );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            bool anyCanBeUnread = false;
            bool allRead        = true;
            foreach( IResource res in context.SelectedResourcesExpanded.ValidResources )
            {
                if ( ICore.Instance.ResourceStore.ResourceTypes [res.Type].HasFlag( ResourceTypeFlags.CanBeUnread ) )
                    anyCanBeUnread = true;

                if ( (bool) res.GetProp( Core.Props.IsUnread ) != _unreadValue )
                    allRead = false;
            }
            if ( !anyCanBeUnread || allRead )
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

    /// <summary>
    /// Default implementation of action to mark all visible resources as read.
    /// </summary>
    public class MarkAllAsReadAction: IAction
    {
        public void Execute( IActionContext context )
        {
            Core.ResourceAP.QueueJob(
                JobPriority.Immediate, "Marking All as Read",
                new ResourceListDelegate( DoMarkAsRead ), Core.ResourceBrowser.VisibleResources );
        }

        private static void DoMarkAsRead( IResourceList resList )
        {
            foreach( IResource res in resList.ValidResources )
            {
                res.DeleteProp( Core.Props.IsUnread );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.Kind == ActionContextKind.ContextMenu )
            {
                presentation.Visible = false;
            }
            else
            {
                presentation.Enabled = Core.ResourceBrowser.ResourceListVisible ||
                    Core.ResourceBrowser.NewspaperVisible;
            }
        }
    }

    /// <summary>
    /// Action to make an entire conversation as read
    /// </summary>
    public class MarkConversationAsRead: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            Core.ResourceAP.QueueJob(
                JobPriority.Immediate, "Marking Conversation as Read",
                new ResourceListDelegate( MarkConversationsRead ), context.SelectedResources );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            bool anyHasThreadHandler = false;
            for( int i=0; i<context.SelectedResources.Count; i++ )
            {
                string resType = context.SelectedResources [i].Type;
                IResourceThreadingHandler threadingHandler = Core.PluginLoader.GetResourceThreadingHandler( resType );
                if ( threadingHandler != null )
                {
                    IResource threadRoot = ConversationBuilder.GetConversationRoot( context.SelectedResources [i] );
                    if ( threadingHandler.CanExpandThread( threadRoot, ThreadExpandReason.Enumerate ) )
                    {
                        anyHasThreadHandler = true;
                        break;
                    }
                }
            }

            if ( !anyHasThreadHandler )
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

        private static void MarkConversationsRead( IResourceList selectedResources )
        {
            foreach( IResource res in selectedResources )
            {
                if ( !res.IsDeleted )
                {
                    ConversationBuilder.MarkConversationRead( res, true );
                }
            }
        }
    }

    /**
     * Action to add a custom link to the selected resource.
     */

    public class AddLinkAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            ResourceClipboardForm.ShowResourceClipboard( context.SelectedResources );
        }
    }

    /**
     * Action to show the Options dialog.
     */

    public class OptionsDialogAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            ICore.Instance.UIManager.ShowOptionsDialog();            
        }
    }

    /**
     * Action to go to the next unread message.
     */

    public class GotoNextUnreadAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ResourceBrowser resourceBrowser = (ResourceBrowser) Core.ResourceBrowser;
            resourceBrowser.GotoNextUnread();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            ResourceBrowser resourceBrowser = (ResourceBrowser) Core.ResourceBrowser;
            if ( !resourceBrowser.CanGotoNextUnread() )
            {
                presentation.Enabled = false;
            }
        }
    }
    
    /**
     * Action to show the correspondence with the selected contact.
     */

    public class ShowCorrespondenceAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            //  ShowCorrespondence can be issued on resources of both "Contact"
            //  and "ContactName" types.
            IResource contact = context.SelectedResources[ 0 ];
            if( contact.Type == "ContactName" )
                contact = contact.GetLinkProp( Core.ContactManager.Props.LinkBaseContact );

            Core.UIManager.BeginUpdateSidebar();

            IResource ws = Core.WorkspaceManager.ActiveWorkspace;
            if ( ws != null && !contact.HasLink( "InWorkspace", ws ) )
            {
                Core.WorkspaceManager.ActiveWorkspace = null;
            }

            Core.TabManager.CurrentTabId = "All";
            Core.LeftSidebar.ActivateViewPane( StandardViewPanes.Correspondents );
            Core.UIManager.EndUpdateSidebar();
            AbstractViewPane viewPane = Core.LeftSidebar.GetPane( StandardViewPanes.Correspondents );
            viewPane.SelectResource( contact, false );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Enabled )
            {
                //  Selection in the Correspondents pane anyway shows the
                //  correspondence from this selection.
                presentation.Visible = !(context.Instance is CorrespondentCtrl);
            }
        }
    }

    /**
     * Action to show the column configuration dialog.
     */

    public class ConfigureColumnsAction: IAction
    {
        public void Execute( IActionContext context )
        {
            (Core.ResourceBrowser as ResourceBrowser).ConfigureColumns();
        }
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = !Core.ResourceBrowser.NewspaperVisible;
        }
    }

    /**
     * Action to exit OmniaMea.
     */

    public class ExitAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Form mainFrame = Core.MainWindow as Form;
            mainFrame.Close();
        }
    }

	/// <summary>
	/// Action to show the currently viewed page in an external browser window.
	/// </summary>
    public class OpenNewBrowserAction: IAction
    {
        public void Execute( IActionContext context )
        {
            if( !string.IsNullOrEmpty( context.CurrentUrl ) )
				Core.UIManager.OpenInNewBrowserWindow(context.CurrentUrl);
#if DEBUG
			else
				throw new InvalidOperationException("[DEBUG] Trying to execute a disabled action.");
#endif
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = !string.IsNullOrEmpty( context.CurrentUrl );
        }
    }

    public class RestoreDefaultViewsAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            if( MessageBox.Show( "This command will recreate all the views created when Omea is installed, even if you modified or deleted some of them. Do you wish to continue?",
                                 "Restore Default Views", MessageBoxButtons.YesNo, MessageBoxIcon.Question ) == DialogResult.Yes )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate, "Restoring Default Views",
                                          new MethodInvoker( ViewsInitializer.InitViewsConstructors ) );
            }
        }
    }

    #region Annotations
    //-------------------------------------------------------------------------
    //  Annotations-related actions
    //-------------------------------------------------------------------------
    public class ViewAnnotationsAction: IAction
    {
        bool    MenuItemState = false;

        public ViewAnnotationsAction()
        {
            MenuItemState = Core.SettingStore.ReadBool( "Annotations", "ViewAnnotations", true );
            Core.ResourceBrowser.ViewAnnotations = MenuItemState;
        }

        public void Execute( IActionContext context )
        {
            MenuItemState = !MenuItemState;
            Core.ResourceBrowser.ViewAnnotations = MenuItemState;
            Core.SettingStore.WriteBool( "Annotations", "ViewAnnotations", MenuItemState );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = MenuItemState;
        }
    }

    public class AnnotateResourceAction: IAction
    {
        public void Execute( IActionContext context )
        {
            Core.ResourceBrowser.EditAnnotation( context.SelectedResources[ 0 ] );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.Kind == ActionContextKind.MainMenu )
            {
                if ( context.SelectedResources.Count != 1 )
                {
                    presentation.Enabled = false;
                }
            }
            else
            {
                presentation.Visible = ( context.SelectedResources.Count == 1 );
            }
        }
    }

    public class DeleteAnnotationAction: IAction
    {
        public void Execute( IActionContext context )
        {
            foreach( IResource res in context.SelectedResources )
            {
                if( res.HasProp( Core.Props.Annotation ))
                {
                    new ResourceProxy( res ).DeleteProp( Core.Props.Annotation );
                }
            }
            //  hack: hide the annotation form for the resource, then after
            //  state is restored, nothing will be shown for empty annotation.
            if( Core.ResourceBrowser.ViewAnnotations )
            {
                Core.ResourceBrowser.ViewAnnotations = false;
                Core.ResourceBrowser.ViewAnnotations = true;
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            //  OM-12037 - 1. Allow to delete multiple annotations at once.
            //             2. For the sake of handiness, do not hide menu item
            //                if some resources do NOT have an annotation.
            bool visible = false;
            foreach( IResource res in context.SelectedResources )
                visible = visible || res.HasProp( Core.Props.Annotation );

            if ( context.Kind == ActionContextKind.MainMenu )
            {
                presentation.Enabled = visible;
            }
            else
            {
                presentation.Visible = visible;
            }
        }
    }
    #endregion Annotations

    public class SendResourcesAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            SendResourcesDialog dlg = new SendResourcesDialog( context.SelectedResources );
            dlg.ShowDialog();
            dlg.Dispose();
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
#if READER
            presentation.Visible = false;
#else
            if ( context.SelectedResources.Count == 1 &&
                 context.SelectedResources[ 0 ].Type == "ContactName" )
            {
                presentation.Visible = false;
                return;
            }

            foreach ( IResource resource in context.SelectedResources )
            {
                if ( Core.PluginLoader.GetResourceSerializer( resource.Type ) != null )
                {
                    return; 
                }
            }
            if ( context.Kind == ActionContextKind.MainMenu )
            {
                presentation.Enabled = false;
            }
            else
            {
                presentation.Visible = false;
            }
#endif
        }
    }

    public class SendResourceNameAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            IResource res = context.SelectedResources[ 0 ];
            res = res.GetLinkProp( Core.ContactManager.Props.LinkBaseContact );

            SendResourcesDialog dlg = new SendResourcesDialog( res.ToResourceList() );
            dlg.ShowDialog();
            dlg.Dispose();
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
#if READER
            presentation.Visible = false;
#else
            presentation.Visible = (context.SelectedResources.Count == 1) &&
                                   (context.SelectedResources[ 0 ].Type == "ContactName");
#endif
        }
    }

    public class ConfigurePropTypesAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            using( CustomPropTypesDlg dlg = new CustomPropTypesDlg() )
            {
                dlg.EditCustomPropertyTypes();
            }
        }
    }

    public class EditCustomPropertiesAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            using( CustomPropertiesDlg dlg = new CustomPropertiesDlg() )
            {
                dlg.EditCustomProperties( context.SelectedResources );            	
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( ResourceTypeHelper.GetCustomProperties().Count == 0 )
            {
                presentation.Visible = false;
            }
        }
    }

    public class ShowHideLinksPaneAction: IAction
    {
        public void Execute( IActionContext context )
        {
            Core.ResourceBrowser.LinksPaneExpanded = !Core.ResourceBrowser.LinksPaneExpanded;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = Core.ResourceBrowser.LinksPaneExpanded;
        }
    }

    public class ShowHideLeftSidebarAction: IAction
    {
        public void Execute( IActionContext context )
        {
            Core.UIManager.LeftSidebarExpanded = !Core.UIManager.LeftSidebarExpanded;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = Core.UIManager.LeftSidebarExpanded;
        }
    }

    public class ShowHideRightSidebarAction: IAction
    {
        public void Execute( IActionContext context )
        {
            Core.UIManager.RightSidebarExpanded = !Core.UIManager.RightSidebarExpanded;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (Core.RightSidebar.PanesCount > 0);
            presentation.Checked = Core.UIManager.RightSidebarExpanded;
        }
    }

    public class ShowHideResourceListAction: IAction
    {
        public void Execute( IActionContext context )
        {
            Core.ResourceBrowser.ResourceListExpanded = !Core.ResourceBrowser.ResourceListExpanded;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = Core.ResourceBrowser.ResourceListExpanded;
        }
    }

    public class ShowHideShortcutBarAction: IAction
    {
        public void Execute( IActionContext context )
        {
            Core.UIManager.ShortcutBarVisible = !Core.UIManager.ShortcutBarVisible;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = Core.UIManager.ShortcutBarVisible;
        }
    }

    public class ShowHideWorkspaceBarAction: IAction
    {
        public void Execute( IActionContext context )
        {
            Core.UIManager.WorkspaceBarVisible = !Core.UIManager.WorkspaceBarVisible;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = Core.UIManager.WorkspaceBarVisible;
        }
    }

    public class ToggleFullMessageViewAction: IAction
    {
        private bool _savedLeftSidebarExpanded = true;
        private bool _savedRightSidebarExpanded = true;
        private bool _savedLinksPaneExpanded = true;
        private bool _savedSplitterVisible = true;
        private bool _savedResourceListExpanded = true;
        private bool _savedIsResourceListFocused = true;
        
        public void Execute( IActionContext context )
        {
            if ( !IsFullView() )
            {
                _savedLeftSidebarExpanded = Core.UIManager.LeftSidebarExpanded;
                _savedRightSidebarExpanded = Core.UIManager.RightSidebarExpanded;
                _savedLinksPaneExpanded = Core.ResourceBrowser.LinksPaneExpanded;
                _savedSplitterVisible = (Core.ResourceBrowser as ResourceBrowser).ResourceListSplitterVisible;
                _savedResourceListExpanded = Core.ResourceBrowser.ResourceListExpanded;
                _savedIsResourceListFocused = Core.ResourceBrowser.ResourceListFocused;

                Core.UIManager.LeftSidebarExpanded = false;
                Core.UIManager.RightSidebarExpanded = false;
                Core.ResourceBrowser.LinksPaneExpanded = false;
                if ( _savedSplitterVisible )
                {
                    Core.ResourceBrowser.ResourceListExpanded = false;
                }

                (Core.TabManager as TabSwitcher).TabChanging += TabManager_TabChanging;
            }
            else
            {
                RestoreNormalWindowMode();
            }
        }

        private void RestoreNormalWindowMode()
        {
            if ( _savedLeftSidebarExpanded )
                Core.UIManager.LeftSidebarExpanded = true;
            if ( _savedRightSidebarExpanded )
                Core.UIManager.RightSidebarExpanded = true;
            if ( _savedLinksPaneExpanded )
                Core.ResourceBrowser.LinksPaneExpanded = true;
            if ( _savedResourceListExpanded && _savedSplitterVisible )
                Core.ResourceBrowser.ResourceListExpanded = true;
            if( _savedIsResourceListFocused )
                Core.ResourceBrowser.FocusResourceList();

            (Core.TabManager as TabSwitcher).TabChanging -= TabManager_TabChanging;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = IsFullView();
        }

        private static bool IsFullView()
        {
            return !Core.UIManager.LeftSidebarExpanded &&
                !Core.UIManager.RightSidebarExpanded && 
                (!(Core.ResourceBrowser as ResourceBrowser).ResourceListSplitterVisible || !Core.ResourceBrowser.ResourceListExpanded ) &&
                !Core.ResourceBrowser.LinksPaneExpanded;
        }

        private void TabManager_TabChanging( object sender, EventArgs e )
        {
            if ( IsFullView() )
            {
                RestoreNormalWindowMode();
            }
        }
    }

    public class MoveNext : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            ResourceBrowser browser = Core.ResourceBrowser as ResourceBrowser;
            bool ctrlPressed = (Control.ModifierKeys == Keys.Control);
            if( ctrlPressed )
                browser.GotoNextUnread();
            else
                browser.GotoNext();
        }
    }

    public class MovePrev : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            ResourceBrowser browser = Core.ResourceBrowser as ResourceBrowser;
            bool ctrlPressed = (Control.ModifierKeys == Keys.Control);
            if( ctrlPressed )
                browser.GotoPrevUnread();
            else
                browser.GotoPrev();
        }
    }

    public class BasicSearchAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            (Core.MainWindow as MainFrame).FocusSearchBox();
        }
    }

    public class DeleteCustomLinkAction: IAction
    {
        public void Execute( IActionContext context )
        {
            if ( context.SelectedResources.Count > 0 && context.LinkTargetResource != null )
            {
                new ResourceProxy( context.SelectedResources [0] ).DeleteLink( context.LinkPropId,
                    context.LinkTargetResource );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count != 1 || context.LinkTargetResource == null )
            {
                presentation.Visible = false;
                return;
            }

            IResource propTypeRes = Core.ResourceStore.FindUniqueResource( "PropType", "ID", context.LinkPropId );
            if ( !propTypeRes.HasProp( "Custom" ) )
            {
                presentation.Visible = false;
            }
        }
    }

    public class FocusResourceBrowserAction: IAction
    {
        public void Execute( IActionContext context )
        {
            Core.ResourceBrowser.FocusResourceList();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( !(context.Instance is IResourceTreePane) && !(context.Instance is CorrespondentCtrl) )
                presentation.Enabled = false;
        }
    }

    public class ShowHelpContentsAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Help.ShowHelp( Core.MainWindow as Control, Core.UIManager.HelpFileName );
        }
    }

    public class ShowKeyboardShortcutsAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Help.ShowHelp( Core.MainWindow as Control, Core.UIManager.HelpFileName, "/reference/keyboard_shortcuts.html" );
        }
    }

    public class ShowAboutBoxAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            using( AboutBox box = new AboutBox() )
            {
                string buildDate = "";
                object[] attrs = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof(AssemblyDescriptionAttribute), false );
                if ( attrs.Length > 0 )
                {
                    buildDate = ((AssemblyDescriptionAttribute) attrs [0]).Description;
                }
                box.ShowAboutBox( Core.ProductVersion, buildDate );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            presentation.Text = "About " + Core.ProductFullName + "...";
        }
    }

    /**
     * Selects the clicked resource in the Views and Categories pane.
     */
    
    public class ShowInDefaultPaneAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {   
            Core.LeftSidebar.ActivateViewPane( StandardViewPanes.ViewsCategories );
            Core.LeftSidebar.DefaultViewPane.SelectResource( context.SelectedResources [0] );
        }
    }

    public class OpenUrlAction: SimpleAction
    {
        private readonly string _url;

        public OpenUrlAction( string url )
        {
            _url = url;
        }

        public override void Execute( IActionContext context )
        {
			Core.UIManager.OpenInNewBrowserWindow(_url);
        }
    }

    /// <summary>
    /// An action for deleting resources through the Semantic Resource Delete API.
    /// </summary>
    public class GenericDeleteAction: IAction
    {
        private const string _cDeletePermanentlyTitle = "Delete Permanently";

        public void Execute( IActionContext context )
        {
            IResourceList resourcesToDelete = null;
            IResourceList selResources = context.SelectedResourcesExpanded;
            string[] selTypes = selResources.GetAllTypes();
            IResourceStore store = Core.ResourceStore;
            IResourceList allDeleted = store.FindResourcesWithProp( null, Core.Props.IsDeleted );
            bool shiftPressed = Control.ModifierKeys == Keys.Shift;
            foreach( string resType in selTypes )
            {
                IResourceList typedResources = selResources.Intersect( store.GetAllResources( resType ) );
                IResourceList deletedTypedResources;
                bool deletePermanent = shiftPressed;
                if( deletePermanent || ResourceDeleterOptions.GetDeleteAlwaysPermanently( resType ) )
                {
                    deletedTypedResources = typedResources;
                    typedResources = store.EmptyResourceList;
                    deletePermanent = true;
                }
                else
                {
                    deletedTypedResources = typedResources.Intersect( allDeleted );
                    typedResources = typedResources.Minus( deletedTypedResources );
                }
                IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( resType );
                DialogResult result = DialogResult.No;
                if( typedResources.Count > 0 )
                {
                    if( !ResourceDeleterOptions.GetConfirmDeleteToRecycleBin( resType ) )
                    {
                        result = DialogResult.Yes;
                    }
                    else
                    {
                        result = deleter.ConfirmDeleteResources( typedResources, deletePermanent, (selTypes.Length > 1) );
                    }
                    if ( result == DialogResult.Cancel )
                    {
                        return;
                    }
                    if ( result == DialogResult.Yes )
                    {
                        resourcesToDelete = typedResources.Union( resourcesToDelete );
                    }
                }
                if( deletedTypedResources.Count > 0 )
                {
                    if( !ResourceDeleterOptions.GetConfirmDeletePermanently( resType ) )
                    {
                        result = DialogResult.Yes;
                    }
                    else
                    {
                        result = deleter.ConfirmDeleteResources( deletedTypedResources, true, (selTypes.Length > 1) );
                    }
                    if ( result == DialogResult.Cancel )
                    {
                        return;
                    }
                    if ( result == DialogResult.Yes )
                    {
                        resourcesToDelete = deletedTypedResources.Union( resourcesToDelete );
                    }
                }
            }

            if ( resourcesToDelete != null )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate, "Deleting resources",
                    new ResourceListDelegate( DoDelete ), resourcesToDelete );
            }
        }

        private static void DoDelete( IResourceList resList )
        {
            foreach( IResource res in resList.ValidResources )
            {
                string type = res.Type;
                IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( type );
                if( ResourceDeleterOptions.GetDeleteAlwaysPermanently( type ) )
                {
                    deleter.DeleteResourcePermanent( res );
                }
                else
                {
                    deleter.DeleteResource( res );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( !AllHaveEnabledDeleters( context.SelectedResourcesExpanded ) )
            {
                if ( context.Kind == ActionContextKind.ContextMenu )
                {
                    presentation.Visible = false;
                }
                else
                {
                    presentation.Enabled = false;
                }
            }

            //  If all resources in the list are to be deleted permanently,
            //  change the name of the action to the more appropriate.
            if( presentation.Visible && presentation.Enabled &&
                ResourceTypeHelper.AllResourcesHaveProp( context.SelectedResources, Core.Props.IsDeleted ))
            {
                presentation.Text = _cDeletePermanentlyTitle;
            }
        }

        private static bool AllHaveEnabledDeleters( IResourceList selectedResources )
        {
            if ( selectedResources.Count == 0 )
            {
                return false;
            }

            foreach( IResource res in selectedResources.ValidResources )
            {
                IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( res.Type );
                if ( deleter == null )
                {
                    return false;
                }
                if ( !deleter.CanDeleteResource( res, res.HasProp( Core.Props.IsDeleted ) ) )
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Action to undelete resources from the Deleted Resources view
    /// </summary>
    public class UndeleteAction: IAction
    {
        public void Execute( IActionContext context )
        {
            Core.ResourceAP.QueueJob(
                JobPriority.Immediate, "Undeleting resources",
                new ResourceListDelegate( DoUndelete ), context.SelectedResourcesExpanded );
        }

        private static void DoUndelete( IResourceList resList )
        {
            for( int i=0; i<resList.Count; i++ )
            {
                IResource res = Core.ResourceStore.TryLoadResource( resList.ResourceIds [i] );
                if ( res != null )
                {
                    IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( res.Type );
                    deleter.UndeleteResource( res );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( !AllCanUndelete( context.SelectedResourcesExpanded ) )
            {
                if ( context.Kind == ActionContextKind.ContextMenu )
                {
                    presentation.Visible = false;
                }
                else
                {
                    presentation.Enabled = false;
                }
            }
        }

        private static bool AllCanUndelete( IResourceList resList )
        {
            if ( resList.Count == 0 )
            {
                return false;
            }

            for( int i = 0; i < resList.Count; i++ )
            {
                IResource res = Core.ResourceStore.TryLoadResource( resList.ResourceIds [i] );
                if ( res != null )
                {
                    if ( !res.HasProp( Core.Props.IsDeleted ) || 
                        Core.PluginLoader.GetResourceDeleter( res.Type ) == null )
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public class ToggleAutoPreviewAction: IAction
    {
        private readonly AutoPreviewMode _mode;

        public ToggleAutoPreviewAction( int modeValue )
        {
            _mode = (AutoPreviewMode) modeValue;
        }

        public void Execute( IActionContext context )
        {
            ResourceBrowser browser = Core.ResourceBrowser as ResourceBrowser;
            browser.AutoPreviewMode = _mode;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            ResourceBrowser browser = Core.ResourceBrowser as ResourceBrowser;
            presentation.Checked = (browser.AutoPreviewMode == _mode);
        }
    }

    public class ToggleVerticalLayoutAction: IAction
    {
        private readonly bool _value;

        public ToggleVerticalLayoutAction( bool value )
        {
            _value = value;
        }

        public void Execute( IActionContext context )
        {
            ResourceBrowser browser = Core.ResourceBrowser as ResourceBrowser;
            browser.VerticalLayout = _value;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            ResourceBrowser browser = Core.ResourceBrowser as ResourceBrowser;
            presentation.Checked = (browser.VerticalLayout == _value);
        }
    }

    public class ToggleVerticalLayoutToolbarAction: IAction
    {
        private readonly bool _value;

        public ToggleVerticalLayoutToolbarAction( bool value )
        {
            _value = value;
        }

        public void Execute( IActionContext context )
        {
            ResourceBrowser browser = Core.ResourceBrowser as ResourceBrowser;
            browser.VerticalLayout = _value;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            ResourceBrowser browser = Core.ResourceBrowser as ResourceBrowser;
            presentation.Checked = (browser.VerticalLayout == _value) && 
                (Core.ResourceBrowser.BrowserPanesMode == BrowserPanesVisibilityMode.Both);
        }

        public override string ToString()
        {
            return base.ToString() + "(" + _value + ")";
        }
    }

    public class ToggleWebPageModeAction: IAction
    {
        public void Execute( IActionContext context )
        {
            BrowserPanesVisibilityMode mode = Core.ResourceBrowser.BrowserPanesMode;
            Core.ResourceBrowser.BrowserPanesMode = (mode == BrowserPanesVisibilityMode.ContentOnly) ? 
                                                     BrowserPanesVisibilityMode.Both : BrowserPanesVisibilityMode.ContentOnly;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = (Core.ResourceBrowser.BrowserPanesMode == BrowserPanesVisibilityMode.ContentOnly);
        }
    }

    public class ToggleListOnlyModeAction : IAction
    {
        public void Execute( IActionContext context )
        {
            BrowserPanesVisibilityMode mode = Core.ResourceBrowser.BrowserPanesMode;
            Core.ResourceBrowser.BrowserPanesMode = (mode == BrowserPanesVisibilityMode.ListOnly) ? 
                                                     BrowserPanesVisibilityMode.Both : BrowserPanesVisibilityMode.ListOnly;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = (Core.ResourceBrowser.BrowserPanesMode == BrowserPanesVisibilityMode.ListOnly);
        }
    }

    public class ToggleGroupsAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ResourceBrowser browser = Core.ResourceBrowser as ResourceBrowser;
            browser.GroupItems = !browser.GroupItems;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            ResourceBrowser browser = Core.ResourceBrowser as ResourceBrowser;
            presentation.Checked = browser.GroupItems;
        }
    }

    public class SetAllGroupsExpandedAction: IAction
    {
        private readonly bool _expanded;

        public SetAllGroupsExpandedAction( bool expanded )
        {
            _expanded = expanded;
        }

        public void Execute( IActionContext context )
        {
            (Core.ResourceBrowser as ResourceBrowser).SetAllGroupsExpanded( _expanded );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = (Core.ResourceBrowser as ResourceBrowser).GroupItems;
        }

        public override string ToString()
        {
            return base.ToString() + "(" + _expanded + ")";
        }
    }

    public class SetAllThreadsExpandedAction: IAction
    {
        private readonly bool _expanded;

        public SetAllThreadsExpandedAction( bool expanded )
        {
            _expanded = expanded;
        }

        public void Execute( IActionContext context )
        {
            (Core.ResourceBrowser as ResourceBrowser).SetAllThreadsExpanded( _expanded );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = (Core.ResourceBrowser as ResourceBrowser).IsThreaded;
        }
    }

    public class SendCurrentUrlAction: IAction
    {
        public void Execute( IActionContext context )
        {
            IEmailService service = (IEmailService) Core.PluginLoader.GetPluginService( typeof(IEmailService) );
            string subject = context.CurrentPageTitle;
            if( string.IsNullOrEmpty( subject ) )
            {
                subject = context.CurrentUrl;
            }
            service.CreateEmail( subject, context.CurrentUrl, EmailBodyFormat.PlainText, (EmailRecipient[]) null, null, true );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( string.IsNullOrEmpty( context.CurrentUrl ) || context.CurrentUrl == "about:blank" )
            {
                presentation.Enabled = false;
            }
        }
    }

    public class PopFocusAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ((ResourceBrowser) Core.ResourceBrowser).PopFocus();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = ((Control) Core.ResourceBrowser).ContainsFocus;
        }
    }

    #region TextIndex Actions
    public class StopResumeTextIndexingAction : IAction
    {
        public void Execute( IActionContext context )
        {
            TextIndexManager timgr = (TextIndexManager) Core.TextIndexManager;
            if( timgr.IsIndexingSuspended )
                timgr.ResumeIndexingByUser();
            else
                timgr.SuspendIndexingByUser();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = Core.TextIndexManager.IsIndexPresent();
            if( presentation.Visible )
            {
                if( ((TextIndexManager) Core.TextIndexManager).IsIndexingSuspended )
                    presentation.Text = "Resume Text Indexing";
                else
                    presentation.Text = "Pause Text Indexing";
            }
        }
    }

    public class IndexUnindexedResourcesAction : IAction
    {
        public void Execute( IActionContext context )
        {
            foreach( IResourceType resType in Core.ResourceStore.ResourceTypes )
            {
                if( TextIndexManager.IsResTypeIndexingConformant( resType ) )
                {
                    int  count = 0;
                    IResourceList resList = Core.ResourceStore.GetAllResources( resType.Name );
                    foreach( int resID in resList.ResourceIds )
                    {
                        //  Fix for OM-12527 - illegal resource Ids can be met
                        //  during iteration (negative).
                        if( resID >= 0 && !Core.TextIndexManager.IsDocumentInIndex( resID ) )
                        {
                            Core.TextIndexManager.QueryIndexing( resID );
                            count++;
                        }
                    }
                    Trace.WriteLine( "-- TextIndexManager -- Finished synchronizing " + count + " of " +
                                     resList.ResourceIds.Count + " <" + resType.Name + "> resources." );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Kind == ActionContextKind.MainMenu);
            presentation.Enabled = Core.TextIndexManager.IsIndexPresent();
        }
    }
    #endregion TextIndex Actions

    public class ExportResourceListAction : SimpleAction
    {
        public override void Execute( IActionContext ctxt )
        {
            ColumnDescriptor[] columns = ((ResourceBrowser)Core.ResourceBrowser).GetDisplayedColumns();
            ExportListForm form = new ExportListForm( columns );
            form.ShowDialog( Core.MainWindow );
        }
    }
}
