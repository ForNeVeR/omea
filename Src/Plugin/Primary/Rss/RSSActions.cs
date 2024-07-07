// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.RSSPlugin.SubscribeWizard;

namespace JetBrains.Omea.RSSPlugin
{
    /// <summary>
    /// An action that is available on an RSSFeedGroup resource or on an empty feed pane.
    /// </summary>
    public abstract class RSSPaneAction : IAction
    {
        public abstract void Execute( IActionContext context );

        public virtual void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.Kind == ActionContextKind.MainMenu ||
                 context.Kind == ActionContextKind.Toolbar ||
                 context.Kind == ActionContextKind.Keyboard )
            {
                return;
            }

            if ( context.SelectedResources.Count == 0 )
            {
                if ( context.Instance != RSSPlugin.RSSTreePane )
                    presentation.Visible = false;
            }
            else
            if ( context.SelectedResources.Count > 1 || !context.SelectedResources.AllResourcesOfType( Props.RSSFeedGroupResource ) )
            {

                presentation.Visible = false;
            }
        }
    }

    #region Import/Export

    public class ImportFeedsAction : RSSPaneAction
    {
        private delegate void ImportJob( IResource root, bool addToWorkspace );
        public override void Execute( IActionContext context )
        {
            IResource importRoot;
            if ( context.SelectedResources.Count == 1 &&
                context.SelectedResources[ 0 ].Type == Props.RSSFeedGroupResource )
            {
                importRoot = context.SelectedResources[ 0 ];
            }
            else
            {
                importRoot = RSSPlugin.RootFeedGroup;
            }
            // Import-import
            WizardForm wizard = new WizardForm( "Import OPML Files" );
            Hashtable importers = new Hashtable();
            ImportManager importManager = new ImportManager( wizard, importers );
            OPMLImporter importer = new OPMLImporter( importManager, importRoot );
            importers.Add( "OPML Files", importer );
            importManager.SelectImporter( "OPML Files", true );
            wizard.RegisterPane( 0, new OptionsPaneWizardAdapter( "Import OPML Files", importer.GetSettingsPaneCreator() ) );

            // Ready!
            if( DialogResult.OK == wizard.ShowDialog( context.OwnerForm ) )
            {
                // Call this, if import was done it doesn't break anything.
                if( ! importManager.FeedsImported )
                {
                    Core.ResourceAP.QueueJob( new ImportJob( importManager.DoImport ), importRoot, true );
                }
            }
        }

    }

    public class ExportFeedsAction : IAction
    {
        public void Execute( IActionContext context )
        {
            ExportFeedsForm form = new ExportFeedsForm();
            if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
            {
                try
                {
                    OPMLProcessor.Export( RSSPlugin.RootFeedGroup, form.CheckedFeeds, form.FileName );
                }
                catch( IOException ex )
                {
                    MessageBox.Show( Core.MainWindow,
                        "Failed to export OPML file: " + ex.Message, "Export Feed Subscriptions",
                        MessageBoxButtons.OK, MessageBoxIcon.Error );
                }
                catch( UnauthorizedAccessException ex )
                {
                    MessageBox.Show( Core.MainWindow,
                        "Failed to export OPML file: " + ex.Message, "Export Feed Subscriptions",
                        MessageBoxButtons.OK, MessageBoxIcon.Error );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            int feedsCount = Core.ResourceStore.GetAllResources( Props.RSSFeedResource ).Count;
            presentation.Enabled = feedsCount > 0;
        }
    }

    public class ImportWizardAction : RSSPaneAction
    {
        private delegate void ImportJob( IResource root, bool addToWorkspace );
        public override void Execute(IActionContext context)
        {
            WizardForm wizard = new WizardForm( ImportManager.ImportPaneName );
            wizard.ShowInTaskbar = false;
            ImportManager importManager = new ImportManager( wizard, RSSPlugin.GetInstance().FeedImporters );
            wizard.RegisterPane( 0,
                new OptionsPaneWizardAdapter( ImportManager.ImportPaneName, importManager.GetImportWizardPane )
                );
            if( DialogResult.OK == wizard.ShowDialog( context.OwnerForm ) )
            {
                // Call this, if import was done it doesn't break anything.
                if( ! importManager.FeedsImported )
                {
                    Core.UIManager.RunWithProgressWindow( ImportManager.ImportPaneName, delegate { importManager.DoImport(RSSPlugin.RootFeedGroup, true); } );
                }
                Core.ResourceAP.QueueJob( new MethodInvoker(importManager.DoImportCache) );
            }
        }
    }
    #endregion Import/Export

    public class AddFeedAction : RSSPaneAction
    {
        public override void Execute( IActionContext context )
        {
            string defaultURL = "";
            IResource defaultGroup = null;

            if ( context.CurrentUrl != null )
            {
                defaultURL = context.CurrentUrl;
            }
            IResourceList selectedResources = context.SelectedResources;
            IResource selectedResource = ( selectedResources.Count > 0 ) ?
                selectedResources[ 0 ] : RSSPlugin.RSSTreePane.SelectedNode;
            if( selectedResource != null )
            {
                if ( selectedResource.Type == Props.RSSFeedGroupResource )
                {
                    defaultGroup = selectedResource;
                }
                else if ( selectedResource.Type == Props.RSSFeedResource )
                {
                    defaultGroup = selectedResource.GetLinkProp( Core.Props.Parent );
                }
                else if ( selectedResource.Type == "RSSItem" )
                {
                    IResource feed = selectedResource.GetLinkProp( -Props.RSSItem );
                    if( feed != null )
                    {
                        defaultGroup = feed.GetLinkProp( Core.Props.Parent );
                    }
                }
            }

            new SubscribeForm().ShowAddFeedWizard( defaultURL, defaultGroup );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( context.Instance == RSSPlugin.RSSTreePane &&
                context.SelectedResources.AllResourcesOfType( Props.RSSFeedResource ) )
            {
                presentation.Visible = true;
            }
            if( !Utils.IsNetworkConnected() )
            {
                presentation.Enabled = false;
                presentation.ToolTip = RSSPlugin._NetworkUnavailable;
            }
        }
    }

    public class AddSearchFeedAction : AddFeedAction
    {
        public override void Execute( IActionContext context )
        {
            IResource defaultGroup = null;
            if ( context.SelectedResources.Count > 0 )
            {
                IResource selectedResource = context.SelectedResources[ 0 ];
                if ( selectedResource.Type == Props.RSSFeedGroupResource )
                {
                    defaultGroup = selectedResource;
                }
                else if ( selectedResource.Type == Props.RSSFeedResource )
                {
                    defaultGroup = selectedResource.GetLinkProp( Core.Props.Parent );
                }
            }

            new SubscribeForm().ShowSearchFeedWizard( defaultGroup );
        }
    }

    public class MarkAsReadAction : IAction
    {
        public void Execute( IActionContext context )
        {
            DoMarkAsRead( context.SelectedResources );
        }

        public static void DoMarkAsRead( IResourceList resList )
        {
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( MarkListAsRead ),
                resList);
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.Kind == ActionContextKind.ContextMenu )
            {
                if ( !RSSPlugin.IsFeedsOrGroups( context.SelectedResources ) )
                {
                    presentation.Visible = false;
                }
            }
        }

        private static void MarkListAsRead( IResourceList selectedResources )
        {
            foreach ( IResource feed in selectedResources.ValidResources )
            {
                IResourceList itemList;
                bool haveComments = false;
                if ( feed.Type == Props.RSSFeedResource )
                {
                    itemList = RSSPlugin.ItemsInFeed( feed, true, ref haveComments );
                }
                else
                {
                    itemList = RSSPlugin.ItemsInGroupRecursive( feed, true, ref haveComments );
                }
                foreach ( IResource item in itemList )
                {
                    item.SetProp( Core.Props.IsUnread, false );
                }
            }
        }
    }

    public class RemoveFeedsAndGroupsAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            int feedCount = 0, groupCount = 0;
            foreach( IResource res in context.SelectedResources )
            {
                if ( res.Type == Props.RSSFeedResource )
                {
                    feedCount++;
                }
                else
                {
                    groupCount++;
                }
            }

            string msg;
            string title = "Remove Subscription";
            if ( groupCount == 0 )
            {
                if ( context.SelectedResources.Count > 1 )
                {
                    msg = context.SelectedResources.Count + " selected feeds";
                }
                else
                {
                    msg = "'" + context.SelectedResources[ 0 ].GetStringProp( "Name" ) + "'";
                }
                msg = "Do you wish to unsubscribe from " + msg + "?";
            }
            else if ( feedCount == 0 )
            {
                msg = (groupCount == 1) ? "Do you wish to delete the folder and all feeds it contains?" :
                                          "Do you wish to delete the folders and all feeds they contain?";
                title = "Delete Feed Folder";
            }
            else
            {
                msg = "Do you wish to delete the selected feeds and feed folders?";
            }

            DialogResult dr = MessageBox.Show( Core.MainWindow, msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question );

            if ( dr == DialogResult.Yes )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate,
                    new ResourceListDelegate( DeleteFeedsAndGroups ), context.SelectedResources );

                //  Save RSS subscription in a "shadow" place where we can
                //  recover it from later.
                //  Todo: augment crash recovery with a list of subscriptions (in a worst variant).
                Core.ResourceAP.QueueJob( JobPriority.BelowNormal, new MethodInvoker( RSSPlugin.SaveSubscription ) );
            }
        }

        internal static void DeleteFeedsAndGroups( IResourceList selectedResources )
        {
            foreach( IResource res in selectedResources.ValidResources )
            {
                if ( res.Type == Props.RSSFeedResource )
                {
                    IResourceList items = res.GetLinksOfType( "RSSItem", "RSSItem" );
                    foreach ( IResource item in items )
                    {
                        IResource feed = item.GetLinkProp( -Props.ItemCommentFeed );
                        if ( feed != null )
                        {
                            feed.GetLinksOfType( "RSSItem", "RSSItem" ).DeleteAll();
                            feed.Delete();
                        }
                    }
                    items.DeleteAll();
                    res.Delete();
                }
                else if ( res.Type == Props.RSSFeedGroupResource )
                {
                    DeleteFeedGroup( res );
                }
            }
        }

        internal static void DeleteFeedGroup( IResource group )
        {
            DeleteFeedsAndGroups( group.GetLinksTo( null, Core.Props.Parent ) );
            group.Delete();
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( presentation.Visible && presentation.Enabled )
            {
                string[] types = context.SelectedResources.GetAllTypes();
                bool haveWrongTypes = false;
                for( int i=0; i<types.Length; i++ )
                {
                    if ( types [i] != Props.RSSFeedResource && types [i] != Props.RSSFeedGroupResource )
                    {
                        haveWrongTypes = true;
                        break;
                    }
                }
                if ( haveWrongTypes )
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
                if ( types.Length == 1 && types [0] == Props.RSSFeedResource )
                {
                    presentation.Text = "Unsubscribe";
                }
            }
        }
    }

    public class OpenItemAction : IAction
    {
        private readonly string _propName;

        public OpenItemAction( string propName )
        {
            _propName = propName;
        }

        public void Execute( IActionContext context )
        {
            //  Fix OM-12773. Seems to be occasional mistiming of different
            //  events, since generally we can do not deal with Resource Store
            //  in manually-activated actions in ShuttingDown state.
            if( Core.State != CoreState.ShuttingDown )
            {
                IResource res = context.SelectedResources[ 0 ];
                string homepage = res.GetStringProp( _propName );
                if ( homepage != null )
				    Core.UIManager.OpenInNewBrowserWindow(homepage);
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = context.SelectedResources.Count == 1;
            if ( presentation.Visible )
            {
                presentation.Enabled = ( context.SelectedResources[ 0 ].HasProp( _propName ) );
            }
        }
    }

    /// <summary>
    /// Action that initiates an update (download) of feeds and feed-folders.
    /// NOTE: somewhen was combined with UpdateAllFeeds action, but at some point that started to have little sense.
    /// </summary>
    public class UpdateFeedAction : IAction
    {
    	public void Execute( IActionContext context )
    	{
    		IResourceList feedsToUpdate;
    		IResourceList feedsPaused = Core.ResourceStore.FindResourcesWithProp( null, Props.IsPaused );

    		if( RSSPlugin.IsFeedsOrGroups( context.SelectedResources ) )
    		{
    			feedsToUpdate = context.SelectedResources;
    		}
    		else if( RSSPlugin.IsFeedOrGroup( context.ListOwnerResource ) )
    		{
    			feedsToUpdate = context.ListOwnerResource.ToResourceList();
    		}
    		else
    		{
    			return;
    		}

    		feedsToUpdate = feedsToUpdate.Minus( feedsPaused );
    		foreach( IResource res in feedsToUpdate )
    		{
    			if( res.Type == Props.RSSFeedResource )
    				RSSPlugin.GetInstance().QueueFeedUpdate( res, JobPriority.AboveNormal );
    			else if( res.Type == Props.RSSFeedGroupResource )
    				QueueFeedsInGroup( res );
    		}
    	}

    	private static void QueueFeedsInGroup( IResource feedGroup )
    	{
    		foreach( IResource res in feedGroup.GetLinksTo( null, Core.Props.Parent ) )
    		{
    			if( res.Type == Props.RSSFeedResource )
    			{
    				RSSPlugin.GetInstance().QueueFeedUpdate( res );
    			}
    			else if( res.Type == Props.RSSFeedGroupResource )
    			{
    				QueueFeedsInGroup( res );
    			}
    		}
    	}

    	public void Update( IActionContext context, ref ActionPresentation presentation )
    	{
            if( !Utils.IsNetworkConnected() )
            {
                presentation.Enabled = false;
                presentation.ToolTip = RSSPlugin._NetworkUnavailable;
            }
            else
    		if( context.CurrentUrl != null && context.Kind == ActionContextKind.Toolbar )
    		{
    			// in this case, the standard Web page Refresh action should be used
    			presentation.Visible = false;
    		}
            else
    		if( !RSSPlugin.IsFeedsOrGroups( context.SelectedResources ) &&
    			!RSSPlugin.IsFeedOrGroup( context.ListOwnerResource ) )
    		{
    			presentation.Visible = false;
    		}
    		else
            if( context.Kind != ActionContextKind.Toolbar )
    		{
    			IResourceList feedsToUpdate;
    			IResourceList feedsPaused = Core.ResourceStore.FindResourcesWithProp( null, Props.IsPaused );
    			if( RSSPlugin.IsFeedsOrGroups( context.SelectedResources ) )
    			{
    				feedsToUpdate = context.SelectedResources;
    			}
    			else
    			{
    				feedsToUpdate = context.ListOwnerResource.ToResourceList();
    			}

    			bool anythingToUpdate = false;
    			feedsToUpdate = feedsToUpdate.Minus( feedsPaused );
    			foreach( IResource res in feedsToUpdate )
    			{
    				if( res.Type == Props.RSSFeedResource )
    				{
    					anythingToUpdate = anythingToUpdate || !res.HasProp( Props.IsPaused );
    				}
    				else if( res.Type == Props.RSSFeedGroupResource )
    				{
    					anythingToUpdate = true;
    				}
    			}
    			presentation.Enabled = anythingToUpdate;
    			presentation.Text = "Update";
    		}
    	}
    }

	/// <summary>
	/// Implements the “Update All Feeds” action.
	/// </summary>
	public class UpdateAllFeedsAction : IAction
	{
		/// <summary>Executes the action.</summary>
		/// <param name="context">
		/// The context for executing the action. Describes the objects to which the action
		/// is applied, like the list of selected resources.
		/// </param>
		public void Execute( IActionContext context )
		{
			RSSPlugin.GetInstance().UpdateAll();
		}

		public void Update( IActionContext context, ref ActionPresentation presentation )
		{
			//  Even when the UpdateAll action is already running and execution
            //  won't result in anything, the action is not prevented from being executed
            presentation.Enabled = Utils.IsNetworkConnected();
            if( !presentation.Enabled )
                presentation.ToolTip = RSSPlugin._NetworkUnavailable;
			return;
		}
	}

	/// <summary>
    /// Action for showing the feed view pane.
    /// </summary>
    public class FeedPropertiesAction : SimpleAction
    {
        private static IResourceList GetFeedsRecursive( IResourceList selectedResources,
                                                        IResourceList allFeeds, IResourceList allFeedGroups )
        {
            IResourceList feeds = selectedResources.Intersect( allFeeds, true );
            IResourceList feedGroups = selectedResources.Intersect( allFeedGroups, true );

            foreach ( IResource feedGroup in feedGroups.ValidResources )
            {
                feeds = feeds.Union( GetFeedsRecursive( feedGroup.GetLinksTo( null, Core.Props.Parent ), allFeeds, allFeedGroups ), true );
            }
            return feeds;
        }

        private static IResourceList GetFeeds( IResourceList selectedResources )
        {
            IResourceList allFeeds = Core.ResourceStore.GetAllResources( Props.RSSFeedResource );
            IResourceList allFeedGroups = Core.ResourceStore.GetAllResources( Props.RSSFeedGroupResource );

            return GetFeedsRecursive( selectedResources, allFeeds, allFeedGroups );
        }

        public override void Execute( IActionContext context )
        {
            if ( context.SelectedResources.Count > 0 )
            {
                IResourceList feeds = GetFeeds( context.SelectedResources );
                if ( feeds.Count == 0 )
                {
                    return;
                }
                using ( RSSFeedView dlg = new RSSFeedView() )
                {
                    dlg.DisplayRSSFeeds( feeds );
                    dlg.ShowDialog( Core.MainWindow );
                }
            }
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = false;
            if ( context.SelectedResources.Count != 0 )
            {
                foreach ( IResource resource in context.SelectedResources )
                {
                    if ( resource.Type != Props.RSSFeedResource && resource.Type != Props.RSSFeedGroupResource )
                    {
                        return;
                    }
                }
                presentation.Visible = true;
            }
        }
    }

    #region Feed Groups

    /// <summary>
    /// Action for creating a new feed group.
    /// </summary>
    public class NewFeedGroupAction : RSSPaneAction
    {
        public override void Execute( IActionContext context )
        {
            if ( !Core.TabManager.ActivateTab( "Feeds" ) )
            {
                return;
            }
            Core.LeftSidebar.ActivateViewPane( "Feeds" );
            IResource parent = RSSPlugin.RootFeedGroup;
            if ( context.SelectedResources.Count > 0 )
            {
                IResource selRes = context.SelectedResources[ 0 ];
                if ( selRes.Type == Props.RSSFeedGroupResource )
                {
                    parent = selRes;
                }
                else
                if ( selRes.HasProp( Core.Props.Parent ) )
                {
                    IResource resParent = selRes.GetLinkProp( Core.Props.Parent );
                    if ( resParent.Type == Props.RSSFeedGroupResource )
                    {
                        parent = resParent;
                    }
                }
            }

            IResource newGroup = RSSPlugin.CreateFeedGroup( parent, "<New Feed Folder>" );
            RSSPlugin.RSSTreePane.EditResourceLabel( newGroup );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if( context.SelectedResources.Count != 1 ||
                context.SelectedResources[ 0 ].Type != Props.RSSFeedResource )
            {
                base.Update( context, ref presentation );
            }
        }
    }

    public class MoveFeed2FolderAction : IAction
    {
        public void Execute( IActionContext context )
        {
            IResourceList list = context.SelectedResources;
            SelectFeedFolderForm form = new SelectFeedFolderForm( list );
            if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
            {
                IResource folder = form.SelectedFolder;
                if( folder != null )
                {
                    foreach( IResource feedOrFolder in list )
                    {
                        new ResourceProxy( feedOrFolder ).SetProp( Core.Props.Parent, folder );
                    }
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.SelectedResources.Count > 0);
        }
    }
    #endregion Feed Groups

    /// <summary>
    /// Action for sending an RSS item by email.
    /// </summary>
    public class SendEmailAction : SimpleAction
    {
        private bool _haveEmailService;
        private IEmailService _emailService;

        public override void Execute( IActionContext context )
        {
            if ( context.SelectedResources.Count == 0 )
            {
                return;
            }
            InitializeEmailService();
            if ( _emailService != null )
            {
                IResource firstItem = context.SelectedResources[ 0 ];
                string subject = firstItem.GetPropText( Core.Props.Subject );
                StringBuilder html = StringBuilderPool.Alloc();
                try
                {
                    foreach ( IResource selItem in context.SelectedResources )
                    {
                        html.Append( selItem.GetPropText( Core.Props.LongBody ) );
                        if ( selItem.HasProp( Props.Link ) )
                        {
                            html.Append( "<p><a href=\"" + selItem.GetStringProp( Props.Link ) + "\">" +
                                selItem.GetStringProp( Props.Link ) + "</a><p>" );
                        }
                    }
                    if ( context.SelectedResources.Count > 1 )
                    {
                        subject += " ( and " + ( context.SelectedResources.Count - 1 ) + " more items ) ";
                    }
                    _emailService.CreateEmail( "FW: " + subject, html.ToString(),
                        EmailBodyFormat.Html, Core.ResourceStore.EmptyResourceList, new string[] {}, false );
                }
                finally
                {
                    StringBuilderPool.Dispose( html );
                }
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            InitializeEmailService();
            base.Update( context, ref presentation );
            presentation.Enabled = ( _emailService != null );
        }

        private void InitializeEmailService()
        {
            if ( !_haveEmailService )
            {
                _emailService = (IEmailService)Core.PluginLoader.GetPluginService( typeof (IEmailService) );
                _haveEmailService = true;
            }
        }
    }

    public class FilterUnreadFeedsAction : IAction
    {

        public void Execute( IActionContext context )
        {
            bool val = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.FilterUnreadFeeds, false );
            Core.SettingStore.WriteBool( IniKeys.Section, IniKeys.FilterUnreadFeeds, !val );
            RSSPlugin.UpdateUnreadPaneFilter( !val );
        }
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Kind == ActionContextKind.MainMenu);
            presentation.Checked = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.FilterUnreadFeeds, false );
        }
    }

    public class FilterErrorFeedsAction : IAction
    {
        public void Execute( IActionContext context )
        {
            bool val = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.FilterErrorFeeds, false );
            Core.SettingStore.WriteBool( IniKeys.Section, IniKeys.FilterErrorFeeds, !val );
            RSSPlugin.UpdateErrorPaneFilter( !val );
        }
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Kind == ActionContextKind.MainMenu);
            presentation.Checked = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.FilterErrorFeeds, false );
        }
    }

    public class SortByLastPostAction : IAction
    {
        public void Execute( IActionContext context )
        {
            bool val = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.ShowPlaneList, false );
            Core.SettingStore.WriteBool( IniKeys.Section, IniKeys.ShowPlaneList, !val );
            RSSPlugin.UpdateSortFilter( !val );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Kind == ActionContextKind.MainMenu);
            presentation.Checked = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.ShowPlaneList, false );
        }
    }

    /// <summary>
    /// Action for hiding the read messages in an RSS post.
    /// </summary>
    public class HideReadRSSAction : IAction
    {
        public void Execute( IActionContext context )
        {
            IResource rssFeed = GetFeedFromContext( context );
            if ( rssFeed != null )
            {
                new ResourceProxy( rssFeed ).SetProp( Core.Props.DisplayUnread,
                                                      !rssFeed.HasProp( Core.Props.DisplayUnread ) );
                RSSPlugin.GetInstance().ResourceNodeSelected( rssFeed );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResource rssFeed = GetFeedFromContext( context );
            if ( rssFeed != null )
            {
                presentation.Checked = rssFeed.HasProp( Core.Props.DisplayUnread );
            }
            else
            {
                presentation.Visible = false;
            }
        }

        private static IResource GetFeedFromContext( IActionContext context )
        {
            IResource rssFeed = null;
            if ( context.Kind == ActionContextKind.ContextMenu )
            {
                if ( context.Instance == RSSPlugin.RSSTreePane &&
                     context.SelectedResources.Count == 1 &&
                     ( context.SelectedResources[ 0 ].Type == Props.RSSFeedResource ||
                       context.SelectedResources[ 0 ].Type == Props.RSSFeedGroupResource ) )
                {
                    rssFeed = context.SelectedResources[ 0 ];
                }
            }
            else if ( context.Kind == ActionContextKind.MainMenu )
            {
                if ( context.ListOwnerResource != null &&
                     ( context.ListOwnerResource.Type == Props.RSSFeedResource || context.ListOwnerResource.Type == Props.RSSFeedGroupResource ) )
                {
                    rssFeed = context.ListOwnerResource;
                }
                else if ( context.SelectedResources.Count == 1 &&
                          ( context.SelectedResources[ 0 ].Type == Props.RSSFeedResource ||
                            context.SelectedResources[ 0 ].Type == Props.RSSFeedGroupResource ) )
                {
                    rssFeed = context.SelectedResources[ 0 ];
                }
            }
            return rssFeed;
        }
    }

    public class ReadCommentsAction : ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            foreach ( IResource rssItem in context.SelectedResources )
            {
                if ( RSSPlugin.HasComments( rssItem ) )
                {
                    DownloadComments( rssItem );
                    if ( !Core.ResourceBrowser.NewspaperVisible )
                    {
                        Core.ResourceBrowser.ExpandConversation( rssItem );
                    }
                    else
                    {
                        ResourceListDisplayOptions rldo = new ResourceListDisplayOptions();
                        IResource commentFeed = rssItem.GetLinkProp( -Props.ItemCommentFeed );
                        IResource parentFeed = rssItem.GetLinkProp( -Props.RSSItem );
                        rldo.SetTransientContainer( parentFeed, "Feeds" );
                        rldo.Caption = "Comments to '" + rssItem.DisplayName + "'";
                        rldo.ShowNewspaper = true;
                        Core.ResourceBrowser.DisplayResourceList( null,
                                                                  commentFeed.GetLinksOfType( "RSSItem", Props.RSSItem ), rldo );
                    }
                }
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( Core.ResourceBrowser.NewspaperVisible && context.SelectedResources.Count > 1 )
            {
                presentation.Enabled = false;
                return;
            }
            if ( presentation.Visible )
            {
                bool anyComments = false;
                foreach ( IResource rssItem in context.SelectedResources )
                {
                    if ( RSSPlugin.HasComments( rssItem ) )
                    {
                        anyComments = true;
                        break;
                    }
                }
                if ( !anyComments )
                {
                    if ( context.Kind == ActionContextKind.Toolbar )
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

        internal static void DownloadComments( IResource rssItem )
        {
            IResource parentFeed = rssItem.GetLinkProp( -Props.RSSItem );
            string commentUrl = rssItem.GetStringProp( Props.CommentRSS );
            if ( parentFeed != null )
            {
                try
                {
                    commentUrl = new Uri( new Uri( parentFeed.GetStringProp( Props.URL ) ), commentUrl ).ToString();
                }
                catch( Exception ex )
                {
                    Trace.WriteLine( "Error building absolute comment feed URI: " + ex.Message );
                }
            }
            IResource commentFeed = rssItem.GetLinkProp( -Props.ItemCommentFeed );
            if ( commentFeed == null )
            {
                ResourceProxy commentFeedProxy = ResourceProxy.BeginNewResource( Props.RSSFeedResource );
                commentFeedProxy.AddLink( Props.ItemCommentFeed, rssItem );

                if ( parentFeed != null )
                {
                    commentFeedProxy.SetProp( Props.FeedComment2Feed, parentFeed );
                }

                commentFeedProxy.SetProp( Props.URL, commentUrl );
                commentFeedProxy.EndUpdate();
                commentFeed = commentFeedProxy.Resource;
            }
            else if ( !commentFeed.HasProp( Props.URL ) )
            {
                new ResourceProxy( commentFeed ).SetProp( Props.URL, commentUrl );
            }

            CreateDownloadingCommentsItem( rssItem );

            RSSUnitOfWork uow = new RSSUnitOfWork( commentFeed, true, false );
            uow.ParseDone += OnFeedParseDone;
            Core.NetworkAP.QueueJob( JobPriority.Immediate, uow );
        }

        private static void OnFeedParseDone( object sender, EventArgs e )
        {
            RSSUnitOfWork uow = (RSSUnitOfWork)sender;
            if ( uow.Status == RSSWorkStatus.Success )
            {
                FeedUpdateQueue.CleanupCommentFeed( uow );
            }
            else if ( uow.Status == RSSWorkStatus.XMLError )
            {
                IResource rssItem = uow.Feed.GetLinkProp( Props.ItemCommentFeed );
                IResource res = FindDownloadingCommentsItem( rssItem );
                if ( res != null )
                {
                    res.SetProp( Core.Props.Subject, "Comment download failed: " + uow.LastException.Message );
                }
            }
        }

        private static void CreateDownloadingCommentsItem( IResource rssItem )
        {
            if ( FindDownloadingCommentsItem( rssItem ) != null )
            {
                return;
            }

            ResourceProxy transientItem = ResourceProxy.BeginNewResource( "RSSItem" );
            transientItem.SetProp( Core.Props.Subject, "Downloading comments..." );

            // make sure the stub item is visible when downloading comments is activated from a view
            transientItem.SetProp( Core.Props.Date, rssItem.GetDateProp( Core.Props.Date ) );

            transientItem.SetProp( Props.ItemComment, rssItem );
            transientItem.SetProp( Props.FeedComment, rssItem.GetLinkProp( -Props.RSSItem ) );
            transientItem.SetProp( Props.Transient, 1 );
            transientItem.EndUpdateAsync();
        }

        internal static IResource FindDownloadingCommentsItem( IResource rssItem )
        {
            foreach ( IResource transientRes in Core.ResourceStore.FindResources( "RSSItem", Core.Props.Subject,
                                                                                  "Downloading comments..." ) )
            {
                if ( transientRes.HasLink( Props.ItemComment, rssItem ) )
                {
                    return transientRes;
                }
            }
            return null;
        }
    }

    public class SetAutoUpdateFeedAction : ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            foreach ( IResource feed in context.SelectedResources )
            {
                new ResourceProxy( feed ).SetProp( Props.AutoUpdateComments, !feed.HasProp( Props.AutoUpdateComments ) );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            presentation.Visible = false;
            if ( context.SelectedResources.Count != 1 )
            {
                return;
            }
            IResource feed = context.SelectedResources[ 0 ];
            if ( feed == null || feed.Type != Props.RSSFeedResource )
            {
                return;
            }
            presentation.Visible = true;
            presentation.Checked = feed.HasProp( Props.AutoUpdateComments );
        }
    }

    public class PostNewComment : ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            foreach ( IResource item in context.SelectedResources )
            {
                PostCommentForm.CreateNewComment( item );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            presentation.Visible = false;
            if ( context.SelectedResources.Count != 1 )
            {
                return;
            }
            IResource item = context.SelectedResources[ 0 ];
            if ( item == null || item.Type != "RSSItem" || !item.HasProp( Props.WfwComment ) )
            {
                return;
            }
            presentation.Visible = true;
        }
    }

    public class PlanToDownloadAction : IAction
    {
        public void Execute( IActionContext context )
        {
            IResourceList selectedResources = context.SelectedResources;
            foreach ( IResource resource in selectedResources.ValidResources )
            {
                int state = resource.GetIntProp( Props.EnclosureDownloadingState );
                if ( state == DownloadState.Failed || state == DownloadState.NotDownloaded )
                {
                    EnclosureDownloadManager.PlanToDownload( resource );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResourceList selectedResources = context.SelectedResources;
            foreach ( IResource resource in selectedResources.ValidResources )
            {
                if ( resource.HasProp( Props.EnclosureURL ) )
                {
                    int state = resource.GetIntProp( Props.EnclosureDownloadingState );
                    if ( state == DownloadState.Failed || state == DownloadState.NotDownloaded )
                    {
                        presentation.Visible = true;
                        return;
                    }
                }
            }
            presentation.Visible = false;
        }
    }

    public class InterruptDownloadAction : IAction
    {
        public void Execute( IActionContext context )
        {
            IResourceList selectedResources = context.SelectedResources;
            foreach ( IResource resource in selectedResources.ValidResources )
            {
                if ( resource.HasProp( Props.EnclosureURL ) )
                {
                    int state = resource.GetIntProp( Props.EnclosureDownloadingState );
                    if ( state == DownloadState.InProgress || state == DownloadState.Planned )
                    {
                        EnclosureDownloadManager.CancelDownload( resource );
                    }
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResourceList selectedResources = context.SelectedResources;
            foreach ( IResource resource in selectedResources.ValidResources )
            {
                int state = resource.GetIntProp( Props.EnclosureDownloadingState );
                if ( state == DownloadState.InProgress || state == DownloadState.Planned )
                {
                    presentation.Visible = true;
                    return;
                }
            }
            presentation.Visible = false;
        }
    }

    public class LocateOnDiskAction : ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            IResourceList resources = context.SelectedResources;
            foreach ( IResource res in resources )
            {
                try
                {
                    if ( res.Type == "RSSItem" )
                    {
                        string path = res.GetPropText( Props.EnclosureTempFile );
                        if ( path.Length == 0 )
                        {
                            IResource feed = res.GetLinkProp( -Props.RSSItem );
                            path = feed.GetPropText( Props.EnclosurePath );
                            if ( path.Length == 0 )
                            {
                                path = Settings.EnclosurePath;
                            }
                        }
                        if ( path != null && path.Length > 0 )
                        {
                            //Process.Start( Path.GetDirectoryName( path ) );
                            Process.Start( "explorer", "/select, \"" + path + "\"" );
                        }
                    }
                }
                catch ( Exception e )
                {
                    Utils.DisplayException( e, "Error" );
                }
            }
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResourceList selectedResources = context.SelectedResources;
            foreach ( IResource resource in selectedResources.ValidResources )
            {
                int state = resource.GetIntProp( Props.EnclosureDownloadingState );
                if ( state == DownloadState.Completed )
                {
                    presentation.Visible = true;
                    return;
                }
            }
            presentation.Visible = false;
        }
    }

    public class RunEnclosureAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource res = context.SelectedResources[ 0 ];
            try
            {
                string path = res.GetPropText( Props.EnclosureTempFile );
                if ( path.Length == 0 )
                {
                    IResource feed = res.GetLinkProp( -Props.RSSItem );
                    path = feed.GetPropText( Props.EnclosurePath );
                    if ( path.Length == 0 )
                    {
                        path = Settings.EnclosurePath;
                    }
                }
                if ( path != null && path.Length > 0 )
                {
                    Process.Start( path );
                }
            }
            catch ( Exception e )
            {
                Utils.DisplayException( e, "Error" );
            }
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                IResource res = context.SelectedResources[ 0 ];
                int state = res.GetIntProp( Props.EnclosureDownloadingState );
                presentation.Visible = (res.Type == "RSSItem") && (state == DownloadState.Completed);
            }
        }
    }

    public class DeleteEnclosureAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource res = context.SelectedResources[ 0 ];
            try
            {
                string path = res.GetPropText( Props.EnclosureTempFile );
                if ( path.Length == 0 )
                {
                    IResource feed = res.GetLinkProp( -Props.RSSItem );
                    path = feed.GetPropText( Props.EnclosurePath );
                    if ( path.Length == 0 )
                    {
                        path = Settings.EnclosurePath;
                    }
                }
                if ( path != null && path.Length > 0 )
                {
                    File.Delete( path );
                }
            }
            catch ( Exception e )
            {
                Utils.DisplayException( e, "Error" );
            }
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                IResource res = context.SelectedResources[ 0 ];
                int state = res.GetIntProp( Props.EnclosureDownloadingState );
                presentation.Visible = (res.Type == "RSSItem") && (state == DownloadState.Completed);
            }
        }
    }

    public class SearchInFeed: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            IResource template = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName,
                                                                        Core.Props.Name, "Post is in %feed%" );
            IResource condition = FilterConvertors.InstantiateTemplate( template, context.SelectedResources, new string[]{ "RSSItem" } );
            Core.FilteringFormsManager.ShowAdvancedSearchForm( "", new string[]{ "RSSItem" }, new IResource[] { condition }, null );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = presentation.Visible = context.SelectedResources.Count > 0;

            if( context.SelectedResources.Count > 1 )
                presentation.Text = "Search in these Feeds";
        }
    }

    public class SearchInFeedGroupAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            IResourceList feeds = Core.ResourceStore.EmptyResourceList;
            foreach( IResource folder in context.SelectedResources )
                feeds = feeds.Union( GetFeedsByFolder( folder ) );

            IResource template = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName,
                                                                        Core.Props.Name, "Post is in %feed%" );
            IResource condition = FilterConvertors.InstantiateTemplate( template, feeds, new string[]{ "RSSItem" } );
            Core.FilteringFormsManager.ShowAdvancedSearchForm( "", new string[]{ "RSSItem" }, new IResource[] { condition }, null );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResourceList feeds = Core.ResourceStore.EmptyResourceList;
            foreach( IResource folder in context.SelectedResources )
            {
                if( folder.Type != Props.RSSFeedGroupResource )
                    throw new ArgumentException( "Search Action -- Action can be applied only to Feed Folder resources." );

                feeds = feeds.Union( GetFeedsByFolder( folder ) );
            }

            presentation.Enabled = feeds.Count > 0;
        }

        private static IResourceList  GetFeedsByFolder( IResource folder )
        {
            IResourceList feeds = Core.ResourceStore.EmptyResourceList;
            IResourceList linked = folder.GetLinksTo( null, Core.Props.Parent );

            foreach( IResource res in linked.ValidResources )
            {
                if( res.Type == Props.RSSFeedResource )
                    feeds = feeds.Union( res.ToResourceList(), true );
                else
                if( res.Type == Props.RSSFeedGroupResource )
                    feeds = feeds.Union( GetFeedsByFolder( res ) );
            }

            return feeds;
        }
    }

    public class SaveItemAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            IResource rssItem = context.SelectedResources [0];
            string fileName = rssItem.GetPropText( Core.Props.Subject ).Trim();
            IOTools.MakeValidFileName( ref fileName );
            fileName = fileName.TrimEnd( '.' ) + ".html";

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "HTML files (*.html)|*.html|All files|*.*";
            using( dlg )
            {
                dlg.FileName = fileName;
                if ( dlg.ShowDialog( Core.MainWindow ) == DialogResult.OK )
                {
                    FileStream fs = new FileStream( dlg.FileName, FileMode.Create );
                    TextWriter fsWriter = new StreamWriter( fs, Encoding.UTF8 );
                    try
                    {
                        RssNewspaperProvider provider = new RssNewspaperProvider();
                        fsWriter.Write( "<html><head><title>" );
                        fsWriter.Write( HttpUtility.HtmlEncode( rssItem.GetStringProp( Core.Props.Subject ) ) );
                        fsWriter.Write( "</title><style type=\"text/css\">" );
                        provider.GetHeaderStyles( "RSSItem", fsWriter );
                        fsWriter.Write( "</style></head><body>" );

                        foreach( IResource res in context.SelectedResources.ValidResources )
                        {
                            provider.GetItemHtml( res, fsWriter );
                            if ( context.SelectedResources.Count > 1 )
                            {
                                fsWriter.Write( "<hr />" );
                            }
                        }
                        fsWriter.Write( "</body>" );
                    }
                    finally
                    {
                        fsWriter.Close();
                    }
                }
            }
        }
    }

    public class StartStopUpdateFeedAction : IAction
    {
        public void Execute( IActionContext context )
        {
            bool  startAction = true;
            IResourceList selectedResources = context.SelectedResources;

            foreach( IResource res in selectedResources.ValidResources )
            {
                bool  state = res.HasProp( Props.IsPaused );
                startAction = startAction && state;
            }
            Core.ResourceAP.QueueJob( JobPriority.Immediate,
                new UpdateStartStopStatusesDelegate( UpdateStartStopStatuses ), selectedResources, startAction );
        }

        private delegate void UpdateStartStopStatusesDelegate( IResourceList selectedResources, bool startAction );

        private static void UpdateStartStopStatuses( IResourceList selectedResources, bool startAction )
        {
            IRssService service = (IRssService) Core.PluginLoader.GetPluginService( typeof( IRssService ) );
            foreach( IResource res in selectedResources.ValidResources )
            {
                if( startAction )
                {
                    if( res.Type == Props.RSSFeedResource )
                    {
                        service.ScheduleFeedUpdate( res );
                    }
                    IResource temp = res;
                    do
                    {
                        temp.DeleteProp( Props.IsPaused );
                        temp = temp.GetLinkProp( Core.Props.Parent );
                    }
                    while( temp != null );
                }
                else
                {
                    res.SetProp( Props.IsPaused, true );
                }
                if( res.Type == Props.RSSFeedGroupResource )
                {
                    UpdateStartStopStatuses( res.GetLinksTo( null, Core.Props.Parent ), startAction );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            bool  startAction = true, pauseAction = true;
            IResourceList selectedResources = context.SelectedResources;

            presentation.Visible = false;
            if( selectedResources.Count > 0 )
            {
                bool hasFolders = false;
                foreach( IResource res in selectedResources.ValidResources )
                {
                    bool  state = res.HasProp( Props.IsPaused );
                    startAction = startAction && state;
                    pauseAction = pauseAction && !state;
                    hasFolders = hasFolders || res.Type == Props.RSSFeedGroupResource;
                }

                presentation.Visible = true;
                presentation.Enabled = startAction || pauseAction;
                if( startAction )
                {
                    presentation.Text = "Resume Updating Feed";
                    if( hasFolders || selectedResources.Count > 1 )
                        presentation.Text += "(s)";
                }
                else
                if( pauseAction )
                {
                    presentation.Text = "Pause Updating Feed";
                    if( hasFolders || selectedResources.Count > 1 )
                        presentation.Text += "(s)";
                }
            }
        }
    }

    /**
     * action for copying feed post URL
     */
    public class CopyPostURLAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource post = context.SelectedResources[ 0 ];
            try
            {
                Clipboard.SetDataObject( post.GetPropText( Props.Link ) );
            }
            catch( ExternalException e )
            {
                Utils.DisplayException( Core.MainWindow, e, "Error" );
            }
        }
    }
}
