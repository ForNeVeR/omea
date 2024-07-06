// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    public abstract class OutlookSimpleAction : SimpleAction
    {
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = ( OutlookSession.OutlookProcessor != null );
        }
    }

    public class SwitchMAPIFolderUnreadModeAction : SwitchUnreadModeAction
    {
        public SwitchMAPIFolderUnreadModeAction()
            : base( STR.MAPIFolder )
        {}
    }

    public class DisplayMailsInFolder : ActionOnResource
    {
        private IResource _folder;

        internal void DisplayResourceList( IResource folder, IResourceList resourceList )
        {
            ResourceListDisplayOptions options = new ResourceListDisplayOptions();

            _folder = folder;
            bool displayUnread = folder.HasProp( Core.Props.DisplayUnread );

            if ( displayUnread )
            {
                options.CaptionTemplate = "Unread messages in %OWNER%";
            }
            else
            {
                options.CaptionTemplate = "Messages in %OWNER%";
            }
            options.SelectedResource = Folder.GetSelectedMail( _folder );

            if ( Folder.IsIgnored( _folder ) )
            {
                options.StatusLine = "This folder is in the ignore list";
                options.StatusLine += ". Click to remove from the ignored folders list";
                options.StatusLineClickHandler = OnClickOnBrowserStatus;
            }
            else
            {
                DateTime dateRestriction = Settings.IndexStartDate;
                if ( dateRestriction.CompareTo( DateTime.MinValue ) != 0 && !Folder.GetSeeAll( _folder ) )
                {
                    options.StatusLine = "Showing messages since " + dateRestriction.ToShortDateString();
                    options.StatusLine += ". Click to see all messages";
                    options.StatusLineClickHandler = OnClickOnBrowserStatus;
                }
            }

            options.SortSettings = new SortSettings( Core.Props.Date, false );
            if ( _folder.HasProp( Core.Props.DisplayThreaded ) )
            {
                options.ThreadingHandler = Core.PluginLoader.CompositeThreadingHandler;
            }
            Core.ResourceBrowser.DisplayResourceList( _folder, resourceList, options );
        }

        private void OnClickOnBrowserStatus( object sender, EventArgs e )
        {
            if ( _folder != null )
            {
                Folder.SetSeeAllAndNoIgnoreAsync( _folder );
                RefreshFolderDescriptor.Do( JobPriority.Normal, PairIDs.Get( _folder ), DateTime.MinValue );
            }
            Core.ResourceBrowser.HideStatusLine();
        }

        public override void Execute( IActionContext context )
        {
            Tracer._Trace( "Execute action: DisplayMailsInFolder" );
            if ( context.SelectedResources.Count == 0 )
            {
                return;
            }
            _folder = context.SelectedResources[ 0 ];
            DisplayResourceList( _folder, Folder.GetMailListLive( _folder ) );
        }
    }

    public class NewMessageAction : OutlookSimpleAction
    {
        private static void ExecuteImpl()
        {
            OutlookFacadeHelper.CreateNewMessage( null, null, EmailBodyFormat.PlainText, (IResourceList)null, null, true );
        }
        public override void Execute( IActionContext context )
        {
            Tracer._Trace( "Execute action: NewMessageAction" );
            OutlookSession.OutlookProcessor.QueueJob( JobPriority.AboveNormal, "New message action",new MethodInvoker( ExecuteImpl ) );
        }
    }

    public class DeliverNowAction : OutlookSimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Tracer._Trace( "Execute action: DeliverNowAction" );
            OutlookSession.OutlookProcessor.QueueJob( new PostMan() );
        }
    }

    public class MarkAsReadOnReply
    {
        public static void Do( IResource mail )
        {
            if ( Settings.MarkAsReadOnReply )
            {
                ResourceProxy mailProxy = new ResourceProxy( mail );
                mailProxy.SetProp( Core.Props.IsUnread, false );
            }
        }
    }

    internal delegate void  AscribeCategoriesDelegate( IResource folder, IResourceList cats );
    public class CreateCategoryAction : IAction
    {
        private static void AssignMailsToCategory( IResource folder, IResourceList categories )
        {
            IResourceList mails = folder.GetLinksOfType( STR.Email, PROP.MAPIFolder );
            foreach( IResource category in categories )
            {
                foreach ( IResource res in mails.ValidResources )
                {
                    try
                    {
                        Core.CategoryManager.AddResourceCategory( res, category );
                    }
                    catch ( System.Threading.ThreadAbortException ex )
                    {
                        Tracer._TraceException( ex );
                    }
                    catch ( Exception ex )
                    {
                        Core.ReportBackgroundException( ex );
                    }
                }

                //  Remove the category from the folder itself so that
                //  next (possible) time we ascribe new categories for
                //  the content. Presence of the category on the folder
                //  may disinform the user on the present categories on the
                //  mails themselves.
                Core.CategoryManager.RemoveResourceCategory( folder, category );
            }
            foreach (IResource res in mails.ValidResources)
            {
                Core.FilterEngine.ExecRules( StandardEvents.CategoryAssigned, res );
            }
        }
        public void Execute( IActionContext context )
        {
            IResourceList folder = context.SelectedResources[ 0 ].ToResourceList();
            IUIManager uiManager = Core.UIManager;
            if( uiManager.ShowAssignCategoriesDialog( Core.MainWindow, folder ) == DialogResult.OK )
            {
                IResourceList categories = folder[ 0 ].GetLinksOfType( "Category", "Category" );
                Core.ResourceAP.QueueJob( JobPriority.AboveNormal,
                                          new AscribeCategoriesDelegate( AssignMailsToCategory ),
                                          folder[ 0 ], categories);
            }
        }
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            // type="MAPIFolder" restriction is put into plugin.xml
            presentation.Visible = ( context.SelectedResources.Count == 1 );
        }
    }

    /**
     * mark all as read
     */

    public class MarkAllAsReadAction : IAction
    {
        #region IAction Members

        public void Execute( IActionContext context )
        {
            Tracer._Trace( "Execute action: MarkAllAsReadAction" );
            if ( context.SelectedResources.Count == 0 )
            {
                return;
            }
            foreach ( IResource resource in context.SelectedResources )
            {
                if ( resource.Type == STR.MAPIFolder )
                {
                    Core.ResourceAP.QueueJob( "Outlook MarkAllAsRead",
                                              new ResourceDelegate( MarkFolderAsRead ), resource );
                }
            }
        }

        private static void MarkFolderAsRead( IResource folder )
        {
            IResourceList mails = folder.GetLinksOfType( STR.Email, PROP.MAPIFolder );
            mails = mails.Intersect(
                Core.ResourceStore.FindResourcesWithProp( STR.Email, Core.Props.IsUnread ), true );
            foreach ( IResource mail in mails )
            {
                mail.SetProp( Core.Props.IsUnread, false );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count == 0 )
            {
                presentation.Visible = false;
                return;
            }

            foreach ( IResource resource in context.SelectedResources )
            {
                if ( resource.Type != STR.MAPIFolder || Folder.IsIgnored( resource ) )
                {
                    presentation.Visible = false;
                    return;
                }
            }
        }

        #endregion
    }

    public class RenameFolderAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            if ( context.SelectedResources.Count == 0 )
            {
                return;
            }
            Settings.OutlookFolders.EditResourceLabel( context.SelectedResources[ 0 ] );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( context.Instance != Settings.OutlookFolders )
            {
                presentation.Visible = false;
                return;
            }
            IResource folder = context.SelectedResources[ 0 ];
            if ( Folder.IsDefault( folder ) )
            {
                presentation.Visible = false;
            }
        }
    }

    internal class RenameFolderProcessor
    {
        public RenameFolderProcessor( IResource resFolder, string name )
        {
            OutlookSession.OutlookProcessor.QueueJob( JobPriority.Immediate, "Renaming folder",
                new Resource_StringDelegate( ExecuteAction ), resFolder, name );
        }

        private static void ExecuteAction( IResource resFolder, string name )
        {
            Trace.WriteLine( ">>> RenameFolderAction.ExecuteAction" );
            PairIDs pairIDs = PairIDs.Get( resFolder );
            if ( pairIDs == null )
            {
                return;
            }

            IEFolder folder = OutlookSession.OpenFolder( pairIDs.EntryId, pairIDs.StoreId );
            if ( folder == null )
            {
                MsgBox.Error( "Outlook plugin", "Cannot rename folder: it was not found in Outlook storage" );
                return;
            }
            using ( folder )
            {
                folder.SetStringProp( MAPIConst.PR_DISPLAY_NAME, name );
                folder.SaveChanges();
            }

            Trace.WriteLine( "<<< RenameFolderAction.ExecuteAction" );
        }
    }

    public class MoveToFolderRuleAction : IRuleAction
    {
        private readonly bool _copy = false;
        public MoveToFolderRuleAction()
        {}
        public MoveToFolderRuleAction( bool copy )
        {
            _copy = copy;
        }
        private void ExecImpl( IResourceList mail, IResource folder )
        {
            new MoveMessageToFolderAction( _copy ).DoMove( folder, mail );
        }
        public void Exec( IResource resource, IActionParameterStore actionStore )
        {
            Tracer._Trace( "Execute rule: MoveToFolderRuleAction" );
            if ( resource == null || resource.Type != STR.Email )
            {
                return;
            }

            IResourceList folders = actionStore.ParametersAsResList();
            if ( folders != null && folders.Count > 0 )
            {
                IResource folder = folders[ 0 ];
                if ( folder.Type == STR.MAPIFolder )
                {
                    OutlookSession.OutlookProcessor.QueueJob( JobPriority.Normal, "Move message to folder rule action",
                        new ResourceList_ResourceDelegate( ExecImpl ), resource.ToResourceList(), folder );
                }
            }
        }
    }

    public class DeleteMessageRuleAction : IRuleAction
    {
        public void Exec( IResource resource, IActionParameterStore actionStore )
        {
            Tracer._Trace( "Execute rule: DeleteMessageRuleAction" );
            if ( resource == null || resource.Type != STR.Email )
            {
                return;
            }
            try
            {
                PairIDs messageIDs = PairIDs.Get( resource );
                OutlookSession.DeleteMessage( messageIDs.StoreId, messageIDs.EntryId, true );
            }
            catch ( Exception exception )
            {
                Tracer._TraceException( exception );
            }
        }
    }

    //-------------------------------------------------------------------------
    //  1. take contact Myself
    //  2. get all mails to Myself
    //  3. get all mails which have CC fiels
    //  4. 4 = (2 \ 3)
    //  5. Check that all in 4 have only one Contact link (myself)
    //-------------------------------------------------------------------------
    internal class SentOnly2MeCondition : ICustomCondition
    {
        public bool MatchResource( IResource res )
        {
            IResourceList toContacts = res.GetLinksOfType( "Contact", "To" );
            return ( res.GetLinksOfType( "Contact", "CC" ).Count == 0 &&
                toContacts.Count == 1 &&
                toContacts[ 0 ].Id == Core.ContactManager.MySelf.Resource.Id );
        }

        public IResourceList Filter( string resType )
        {
            throw new ApplicationException( "Can not call this condition in List context" );
        }

        public IResourceList Filter( IResourceList input )
        {
            return Core.ResourceStore.EmptyResourceList;
        }
    }

    /**
     * toggle formatting for plaintext mail's preview
     */

    public class ToggleFormattingAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            if ( context.SelectedResources.Count > 0 )
            {
                IResource resource = context.SelectedResources[ 0 ];
                ResourceProxy proxy = new ResourceProxy( resource );
                if ( resource.HasProp( "NoFormat" ) )
                {
                    proxy.DeleteProp( "NoFormat" );
                }
                else
                {
                    proxy.SetProp( "NoFormat", true );
                }
            }
            Core.ResourceBrowser.RedisplaySelectedResource();

        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( context.SelectedResources.Count == 0 || context.SelectedResources[ 0 ].GetStringProp( PROP.BodyFormat ) != "PlainText" )
            {
                presentation.Visible = false;
                presentation.Enabled = false;
            }
            if ( presentation.Visible )
            {
                presentation.Text = ( context.SelectedResources[ 0 ].HasProp( "NoFormat" ) ) ?
                    "Show as formatted text" : "Show as plain text";
            }
        }
    }

    public class IndexAllEmailAction : IAction
    {
        #region IAction Members

        public void Execute( IActionContext context )
        {
            Tracer._Trace( "Execute action: IndexAllEmailAction" );
            OutlookSession.OutlookProcessor.QueueJob( new MailSyncBackground( DateTime.MinValue ) );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = OutlookSession.OutlookProcessor.IsStarted && !OutlookSession.OutlookProcessor.IsSyncComplete();
        }

        #endregion
    }

    public class SynchronizeFolderNow : IAction
    {
        public void Execute( IActionContext context )
        {
            IResourceList list = context.SelectedResources;
            foreach ( IResource resource in list.ValidResources )
            {
                if ( resource.Type == STR.MAPIFolder )
                {
                    Folder.SetSeeAllAndNoIgnoreAsync( resource );
                    RefreshFolderDescriptor.Do( JobPriority.Normal, PairIDs.Get( resource ), DateTime.MinValue );
                }
            }
            Core.ResourceBrowser.HideStatusLine();
        }
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResourceList list = context.SelectedResources;
            foreach ( IResource resource in list.ValidResources )
            {
                if ( resource.Type == STR.MAPIFolder )
                {
                    presentation.Enabled = true;
                    return;
                }
            }
            presentation.Enabled = false;
        }
    }

    public class EmptyDeletedItemsFolderAction : IAction
    {
        #region IAction Members

        public void Execute( IActionContext context )
        {
            Tracer._Trace( "Execute action: EmptyDeletedItemsFolderAction" );
            if ( context.SelectedResources.Count == 0 )
            {
                return;
            }
            foreach ( IResource resource in context.SelectedResources )
            {
                if ( resource.Type == STR.MAPIFolder && Folder.IsDeletedItems( resource ) )
                {
                    OutlookSession.OutlookProcessor.QueueJob( JobPriority.AboveNormal, "Empty folder action",
                        new ResourceDelegate( EmptyFolder ), resource );
                }
            }
        }

        private static void EmptyFolder( IResource resource )
        {
            PairIDs IDs = PairIDs.Get( resource );
            IEFolder folder = OutlookSession.OpenFolder( IDs.EntryId, IDs.StoreId );
            if ( folder != null )
            {
                using ( folder )
                {
                    folder.Empty();
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count == 0 )
            {
                presentation.Visible = false;
                return;
            }

            foreach ( IResource resource in context.SelectedResources )
            {
                if ( resource.Type != STR.MAPIFolder || !Folder.IsDeletedItems( resource ) )
                {
                    presentation.Visible = false;
                    return;
                }
            }
        }

        #endregion
    }

    public class SearchInFolder: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            IResourceList selection = context.SelectedResources;
            IResource template = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", "Locates in %specified% outlook folder" );
            IResource condition = FilterConvertors.InstantiateTemplate( template, selection, new string[]{ "Email" } );
            Core.FilteringFormsManager.ShowAdvancedSearchForm( "", new string[]{ "Email" }, new IResource[] { condition }, null );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            int  groupsCount = context.SelectedResources.Count;

            presentation.Enabled = presentation.Visible = groupsCount > 0;
            if( groupsCount > 1 )
            {
                presentation.Text = "Search in these Folders";
            }
        }
    }
}
