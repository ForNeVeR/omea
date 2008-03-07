/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.Net;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.FileTypes;

namespace JetBrains.Omea.Nntp
{
    /** 
     * display subscription dialog
     */
    public class ManageNewsgroupsAction: IAction
    {
        public void Execute( IActionContext context )
        {
            IResourceList resources = context.SelectedResources;
            if( resources.Count == 1 )
            {
                IResource selected = resources[ 0 ];
                if( selected.Type == NntpPlugin._newsServer )
                {
                    SubscribeForm.SubscribeToGroups( selected );
                    return;
                }
                if( selected.Type == NntpPlugin._newsGroup )
                {
                    SubscribeForm.SubscribeToGroups( new NewsgroupResource( selected ).Server );
                    return;
                }
            }
            SubscribeForm.SubscribeToGroups();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            try 
            {
                if ( context.Kind == ActionContextKind.MainMenu || context.Kind == ActionContextKind.Toolbar )
                {
                    return;
                }

                if ( context.SelectedResources.Count == 0 )
                {
                    if ( context.Instance != NntpPlugin._newsgroupsTreePane )
                    {
                        presentation.Visible = false;
                    }
                    return;
                }

                string[] types = context.SelectedResources.GetAllTypes();

                if( types.Length > 2 )
                {
                    presentation.Visible = false;
                    return;
                }
                foreach( string type in types )
                {
                    if( type != NntpPlugin._newsGroup && type != NntpPlugin._newsServer )
                    {
                        presentation.Visible = false;
                        break;
                    }
                }
            }
            finally 
            {
                UpdateActionIfNetworkUnavailable.Update( ref presentation );
            }
        }
    }

    /** 
     * display server properties dialog
     */
    public class ServerPropertiesAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            if ( context.SelectedResources.Count == 0 ) return;

            EditServerForm form = EditServerForm.CreateServerPropertiesForm( context.SelectedResources );
            using( form )
            {
                if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
                {
                    foreach ( IResource server in context.SelectedResources )
                    {
                        NntpConnectionPool.CloseConnections( server );
                        NntpClientHelper.DeliverNewsFromServer( server, null, true, null );
                    }
                }
            }
        }
    }

    /** 
     * displays posting form & posts an article to selected newsgroups
     */
    public class PostAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            IResourceList groups = null;
            IResourceList resources = context.SelectedResources;
            if( resources.AllResourcesOfType( NntpPlugin._newsGroup ) )
            {
                groups = resources;
            }
            else
            {
                IResource owner = Core.ResourceBrowser.OwnerResource;
                if( owner != null ) 
                {
                    string type = owner.Type;
                    if( type == NntpPlugin._newsGroup )
                    {
                        groups = owner.ToResourceListLive();
                    }
                    else if( type == NntpPlugin._newsFolder )
                    {
                        groups = new NewsTreeNode( owner ).Groups;
                    }
                }
            }
            EditMessageForm.EditAndPostMessage( groups, string.Empty, string.Empty, string.Empty, true );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = NntpPlugin._areThereGroups;
        }
    }

    /** 
     * initiates delivering news
     */
    public class DeliverNewsAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            NntpPlugin.DeliverNews( true );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = NntpPlugin._areThereGroups;
            UpdateActionIfNetworkUnavailable.Update( ref presentation );
        }
    }

    /**
     * initiates delivering news from a server
     */
    public class DeliverNewsFromServerAction : ActionOnResource
    {
        private IResourceList _servers = Core.ResourceStore.EmptyResourceList;

        public override void Execute( IActionContext context )
        {
            foreach( IResource server in _servers )
            {
                NntpClientHelper.DeliverNewsFromServer(
                    server, NntpPlugin._newsgroupsTreePane.SelectedNode, true, null );
            }
            _servers = Core.ResourceStore.EmptyResourceList;
        }
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                presentation.Enabled = false;
                IResourceList selected = context.SelectedResources;
                if( selected.AllResourcesOfType( NntpPlugin._newsServer ) )
                {
                    presentation.Enabled = true;
                    _servers = selected;
                }
                else
                {
                    IResource server = Core.ResourceBrowser.OwnerResource;
                    while( server != null )
                    {
                        if( server.Type == NntpPlugin._newsServer )
                        {
                            presentation.Enabled = true;
                            _servers = server.ToResourceList();
                            break;
                        }
                        server = new NewsTreeNode( server ).Parent;
                    }
                }
                UpdateActionIfNetworkUnavailable.Update( ref presentation );
            }
        }
    }

    /** 
     * creates formatted reply message & displays posting form
     */
    public class ReplyAction: IAction
    {
        private readonly EditMessageForm _form;

        public ReplyAction() {}
        internal ReplyAction( EditMessageForm form )
        {
            _form = form;
        }

        public void Execute( IActionContext context )
        {
            IResource article = context.SelectedResources[ 0 ];
            if( Settings.MarkAsReadOnReplyAndFormard && article.HasProp( NntpPlugin._propIsUnread ) )
            {
                new ResourceProxy( article ).SetProp( NntpPlugin._propIsUnread, false );
            }
            string origSubject = article.GetPropText( Core.Props.Subject );
            if( !origSubject.ToLower().StartsWith( "re:" ) )
            {
                origSubject = "Re: " + origSubject;
            }

            IResourceList groupList = article.GetLinksFromLive( NntpPlugin._newsGroup, NntpPlugin._propTo );
            IResource groupRes = null;
            lock( groupList )
            {
            	foreach(IResource resource in groupList)
            	{
            		if (new NewsgroupResource(resource).Server != null)
					{
						groupRes = resource;
						break;
					}
            	}
            }
        	QuoteSettings settings = QuoteSettings.Default;
            if( groupRes != null )
            {
                ServerResource server = new ServerResource( new NewsgroupResource( groupRes ).Server );
                settings.UseSignature = server.UseSignature;
                settings.SignatureInReplies = server.ReplySignaturePosition;
                settings.Signature = server.MailSignature;
            }

            string quote = GetQuotedText( article, context, settings );

            if( article.HasProp( NntpPlugin._propFollowupTo ) )
            {
                string followUpTo = article.GetPropText( NntpPlugin._propFollowupTo );
                string[] groups = followUpTo.Split( ',' );
                groupList = Core.ResourceStore.EmptyResourceList;
                foreach( string group in groups )
                {
                    IResourceList groupResources = Core.ResourceStore.FindResources(
                        NntpPlugin._newsGroup, Core.Props.Name, group.Trim() );
                    if( groupResources.Count > 0 )
                    {
                        groupRes = groupResources[ 0 ];
                        if( groupRes != null )
                        {
                            groupList = groupList.Union( groupRes.ToResourceListLive() );
                        }
                    }
                }
            }
            if( _form != null && Settings.CloseOnReply )
            {
                _form.Close();
            }
            EditMessageForm.EditAndPostMessage(
                groupList, origSubject, quote, article.GetPropText( NntpPlugin._propArticleId ), false );
        }

		public void Update(IActionContext context, ref ActionPresentation presentation)
		{
            if(context.SelectedResources.Count == 1)
            {
                // There's exactly one resource selected; check its type
                // Ensure it's not a local article as we cannot reply to a local article
                presentation.Enabled = presentation.Visible = (context.SelectedResources[0].Type == NntpPlugin._newsArticle);				
            }
            else 
            {
                presentation.Visible = false;	// Can be applied when there is exactly one resource
            }
        }

        internal virtual string  GetQuotedText( IResource article, IActionContext context, QuoteSettings settings )
        {
            string selected = context.SelectedPlainText;
            if( String.IsNullOrEmpty( selected ) )
                return Core.MessageFormatter.QuoteMessage( article, Core.Props.LongBody, settings );
            else
                return Core.MessageFormatter.QuoteMessage( article, selected, settings );
        }
    }

    /** 
     * creates formatted reply message & displays posting form
     */
    public class Reply2SenderWithoutQuotation : ReplyAction
    {
        internal override string  GetQuotedText( IResource article, IActionContext context, QuoteSettings settings )
        {
            return Core.MessageFormatter.QuoteMessage( article, string.Empty, settings );
        }
    }
    
    /**
     * opens news attachment in correspondent application
     */
    public class OpenAttachmentAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            Core.FileResourceManager.OpenSourceFile( context.SelectedResources[ 0 ] );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                IResource article = context.SelectedResources[ 0 ].GetLinkProp( NntpPlugin._propAttachment );
                if( article == null || ( article.Type != NntpPlugin._newsArticle && article.Type != NntpPlugin._newsLocalArticle ) )
                {
                    presentation.Visible = false;
                }
            }
        }
    }

    /** 
     * saves news attachment on disk
     */
    public class SaveAttachmentAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            IResource attachment = context.SelectedResources[ 0 ];
            string    pathToSave = string.Empty;

            bool mayMissDialog = (Control.ModifierKeys == Keys.Control);
            if( mayMissDialog )
                pathToSave = Core.SettingStore.ReadString( "Omea", "LastUsedPath" );
            mayMissDialog = mayMissDialog && (pathToSave.Length > 0);

            if( pathToSave.Length == 0 )
                pathToSave = Environment.CurrentDirectory;

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.FileName = attachment.GetPropText( Core.Props.Name ).Replace( '\\', '-' ).Replace( '/', '-' ).Replace( ':','-' );
            saveDialog.InitialDirectory = pathToSave;

            DialogResult dialogResult;
            if( mayMissDialog )
                dialogResult = DialogResult.OK;
            else
            {
                try
                {
                    dialogResult = saveDialog.ShowDialog( Core.MainWindow );
                }
                catch( Exception e )
                {
                    Utils.DisplayException( e, "Can't save attachment" );
                    dialogResult = DialogResult.Cancel;
                }
            }

            if( dialogResult == DialogResult.OK )
            {
                Stream fileStream;
                if( ( fileStream = saveDialog.OpenFile() ) != null )
                {
                    try 
                    {
                        Stream blob = attachment.GetBlobProp( NntpPlugin._propContent );
                        blob.Position = 0;
                        FileResourceManager.CopyStream( blob, fileStream );
                    }
                    catch( Exception e )
                    {
                        Utils.DisplayException( e, "Can't save attachment" );
                    }
                    finally
                    {
                        fileStream.Close();
                    }
                }
            }
            Core.SettingStore.WriteString( "Omea", "LastUsedPath", pathToSave );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                IResource article = context.SelectedResources[ 0 ].GetLinkProp( NntpPlugin._propAttachment );
                if( article == null || ( article.Type != NntpPlugin._newsArticle && article.Type != NntpPlugin._newsLocalArticle ) )
                {
                    presentation.Visible = false;
                }
            }
        }
    }

    /**
     * Base class for read headers actions
     */
    public abstract class ReadHeadersBaseAction : ActionOnResource
    {
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible && presentation.Enabled )
            {
                bool enabled = false;
                IResourceList resources = context.SelectedResources;
                foreach( IResource group in resources )
                {
                    NewsgroupResource groupResource = new NewsgroupResource( group );
                    enabled = enabled || ( groupResource.IsSubscribed && !group.HasProp( NntpPlugin._propNoMoreHeaders ) );
                    if( enabled )
                    {
                        break;
                    }
                }
                presentation.Enabled = enabled;
                UpdateActionIfNetworkUnavailable.Update( ref presentation );
            }
        }
    }

    /**
     * Reads next N headers from a newsgroup
     * N is got from the Setting Store
     */
    public class ReadNextHeaders: ReadHeadersBaseAction
    {
        public override void Execute( IActionContext context )
        {
            IResourceList resources = context.SelectedResources;
            foreach( IResource group in resources )
            {
                NntpClientHelper.DownloadNextHeadersFromGroup( group );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible && presentation.Enabled )
            {
                int nextHeaders = 0;
                IResourceList resources = context.SelectedResources;
                foreach( IResource group in resources )
                {
                    NewsgroupResource groupResource = new NewsgroupResource( group );
                    if( nextHeaders == 0 )
                    {
                        nextHeaders = groupResource.CountToDownloadAtTime;
                    }
                    else if( nextHeaders != groupResource.CountToDownloadAtTime )
                    {
                        nextHeaders = 0;
                        break;
                    }
                }
                presentation.Text = "Download Next " + ( ( nextHeaders != 0 ) ? nextHeaders.ToString() : "" ) + " Headers";
            }
        }
    }

    /**
     * Reads all headers for a newsgroup
     */
    public class ReadAllHeaders: ReadHeadersBaseAction
    {
        public override void Execute( IActionContext context )
        {
            IResourceList resources = context.SelectedResources;
            if( resources.Count == 1 )
            {
                if( MessageBox.Show( Core.MainWindow,
                    "All headers of the news articles which exist in this newsgroup will be downloaded.\r\nThis may require some time. Do you wish to continue?",
                    "Download All Headers", MessageBoxButtons.YesNo, MessageBoxIcon.Question ) != DialogResult.Yes )
                {
                    return;
                }
            }
            else
            {
                if( MessageBox.Show( Core.MainWindow,
                    "All headers of the news articles which exist in these newsgroups will be downloaded.\r\nThis may require some time. Do you wish to continue?",
                    "Download All Headers", MessageBoxButtons.YesNo, MessageBoxIcon.Question ) != DialogResult.Yes )
                {
                    return;
                }
            }
            foreach( IResource group in resources.ValidResources )
            {
                NntpClientHelper.DownloadAllHeadersFromGroup( group );
            }
        }
    }

    /** 
     * mark all as read
     */
    public class MarkAllAsReadAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            IResourceList resources = context.SelectedResources;
            foreach( string resType in resources.GetAllTypes() )
            {
                if( resType == NntpPlugin._newsServer )
                {
                    if( MessageBox.Show( Core.MainWindow,
                        "This action will mark all messages in all newsgroups on the server as read. Do you wish to continue?",
                        "Mark All as Read", MessageBoxButtons.YesNo, MessageBoxIcon.Question ) != DialogResult.Yes )
                    {
                        return;
                    }
                    break;
                }
            }
            MarkGroupsRead( resources );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                IResourceList selected = context.SelectedResources;
                if( selected.Count == 1 )
                {
                    IResource resource = selected[ 0 ];
                    if( resource.Type == NntpPlugin._newsFolder &&
                        NewsFolders.IsDefaultFolder( resource ) && !NewsFolders.IsSentItems( resource ) )
                    {
                        presentation.Visible = false;
                    }
                    else if( resource.Type == NntpPlugin._newsServer )
                    {
                        presentation.Enabled = new ServerResource( resource ).SubscribedGroupsCount > 0;
                    }
                }
            }
        }

        internal static void MarkGroupsRead( IResourceList groups )
        {
            foreach( IResource group in groups )
            {
                if( group.Type != NntpPlugin._newsGroup && !NewsFolders.IsDefaultFolder( group ) )
                {
                    MarkGroupsRead( new NewsTreeNode( group ).Children );
                }
                else
                {
                    Core.ResourceAP.QueueJob( JobPriority.Immediate,
                        "Mark newsgroup as read", new ResourceDelegate( MarkGroupRead ), group );
                }
            }
        }

        internal static void MarkGroupRead( IResource group )
        {
            IResourceList articles = group.GetLinksTo( null, NntpPlugin._propTo );
            articles = articles.Intersect(
                Core.ResourceStore.FindResourcesWithProp( null, NntpPlugin._propIsUnread ), true );
            foreach( IResource article in articles )
            {
                article.SetProp( NntpPlugin._propIsUnread, false );
            }
        }
    }

    /**
     * subscribe action
     */
    public class SubscribeAction: ActionOnResource
    {
        private IResourceList _groups = null;

        public override void Execute( IActionContext context )
        {
            if( _groups != null )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( SubscribeToGroups ) );
            }
        }

        private void SubscribeToGroups()
        {
            foreach( IResource group in _groups )
            {
                NewsgroupResource groupResource = new NewsgroupResource( group );
                if( !groupResource.IsSubscribed )
                {
                    NntpPlugin.Subscribe2Group( groupResource.Name, groupResource.Server );
                }
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                bool visible = false;
                IResourceList groups = Core.ResourceStore.EmptyResourceList;
                IResourceList resources = context.SelectedResources;
                foreach( IResource res in resources )
                {
                    if( res.Type == NntpPlugin._newsGroup )
                    {
                        groups = groups.Union( res.ToResourceList() );
                    }
                    else if ( res.Type == NntpPlugin._newsFolder || res.Type == NntpPlugin._newsServer )
                    {
                        groups = groups.Union( new NewsTreeNode( res ).Groups );
                    }
                }
                foreach( IResource group in groups )
                {
                    if( !new NewsgroupResource( group ).IsSubscribed )
                    {
                        _groups = groups;
                        visible = true;
                        break;
                    }
                }
                presentation.Visible = visible;
            }
        }
    }

    /** 
     * unsubscribe action
     */
    public class UnsubscribeAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            bool deleteArticles;
            IResourceList resources = context.SelectedResources;
            if( UnsubscribeForm.Unsubscribe( resources, out deleteArticles ) == DialogResult.OK )
            {
                if( resources.Count == 1 )
                {
                    Core.ResourceAP.QueueJob( JobPriority.Immediate,
                        new UnsubscribeDelegate( Unsubscribe ), resources[ 0 ], deleteArticles );
                }
                else
                {
                    foreach( IResource group in resources )
                    {
                        Core.ResourceAP.QueueJob( JobPriority.Immediate,
                            new UnsubscribeDelegate( Unsubscribe ), group, deleteArticles );
                    }
                }
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            presentation.Text =
                ( NewsgroupResource.AllUnsubscribed( context.SelectedResources ) ) ? "Delete" : "Unsubscribe";
        }


        internal delegate void UnsubscribeDelegate( IResource group, bool deleteArticles );

        internal static void Unsubscribe( IResource group, bool deleteArticles )
        {
            NewsgroupResource groupBO = new NewsgroupResource( group );
            string groupName = groupBO.Name;
            IResource server = groupBO.Server;
            if( deleteArticles )
            {
                IResourceList articles = group.GetLinksTo( NntpPlugin._newsArticle, NntpPlugin._propTo );
                group.Delete();
                IntArrayList contactIDs = new IntArrayList();
                foreach( IResource article in articles )
                {
                    // do not delete crossposted articles
                    if( article.GetLinksFrom( NntpPlugin._newsGroup, NntpPlugin._propTo ).Count == 0 )
                    {
                        IResource from = article.GetLinkProp( Core.ContactManager.Props.LinkFrom );
                        if( from != null && contactIDs.IndexOf( from.Id ) < 0 )
                        {
                            contactIDs.Add( from.Id );
                        }
                        article.GetLinksOfType( null, NntpPlugin._propAttachment ).DeleteAll();
                        article.Delete();
                    }
                }
                Core.ContactManager.DeleteUnusedContacts( Core.ResourceStore.ListFromIds( contactIDs, false ) );
            }
            if( server != null )
            {
                ServerResource serverResource = new ServerResource( server );
                serverResource.UnsubscribeFromGroup( groupName );
                if( !deleteArticles )
                {
                    groupBO.InvalidateDisplayName( serverResource.AbbreviateLevel );
                }
            }

            NntpPlugin.CheckGroups();
        }
    }

    /**
     * action for swithing threaded mode
     */
    public class SwitchNewsThreadedModeAction: SwitchThreadedModeAction
    {
        public SwitchNewsThreadedModeAction()
            : base( NntpPlugin._newsGroup, NntpPlugin._newsFolder, NntpPlugin._newsServer ) {}
    }

    /**
     * action for swithing groups' and servers' "Hide Read Messages" mode
     */
    public class SwitchGroupsNServersUnreadModeAction: SwitchUnreadModeAction
    {
        public SwitchGroupsNServersUnreadModeAction() : base( NntpPlugin._newsGroup, NntpPlugin._newsServer ) {}
    }

    /**
     * action for swithing folders' "Hide Read Messages" mode
     */
    public class SwitchFoldersUnreadModeAction: SwitchUnreadModeAction
    {
        public SwitchFoldersUnreadModeAction() : base( NntpPlugin._newsFolder )
        {}

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            IResourceList resources = context.SelectedResources;
            if( presentation.Visible )
            {
                foreach( IResource res in resources )
                {
                    if( NewsFolders.IsDefaultFolder( res ) )
                    {
                        presentation.Visible = false;
                        break;
                    }
                }
            }
        }
    }

    /**
     * Shows the selected news article in context (in the Newsgroups pane).
     */

    public class GoToConversationAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            if ( Core.WorkspaceManager.ActiveWorkspace != null )
            {
                bool anyInCurrentWorkspace = false;
                IResourceList newsgroups = context.SelectedResources [0].GetLinksFrom( NntpPlugin._newsGroup, NntpPlugin._propTo );
                foreach( IResource newsgroup in newsgroups )
                {
                    if ( newsgroup.HasLink( "WorkspaceVisible", Core.WorkspaceManager.ActiveWorkspace ) )
                    {
                        anyInCurrentWorkspace = true;
                        break;
                    }
                }

                if ( !anyInCurrentWorkspace )
                {
                    Core.UIManager.BeginUpdateSidebar();
                    Core.WorkspaceManager.ActiveWorkspace = null;
                    Core.UIManager.EndUpdateSidebar();
                }
            }

            Core.UIManager.DisplayResourceInContext( context.SelectedResources [0], true ); 
            Core.ResourceBrowser.FocusResourceList();
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( presentation.Visible && Core.ResourceBrowser.OwnerResource != null )
            {
                // hide "Go to Conversation" when the newsgroup containing the article
                // is displayed, because it won't have any effect in that case
                IResourceList newsgroups = context.SelectedResources [0].GetLinksFrom( NntpPlugin._newsGroup, NntpPlugin._propTo );
                if ( newsgroups.IndexOf( Core.ResourceBrowser.OwnerResource ) >= 0 )
                {
                    presentation.Visible = false;
                }
            }
        }
    }

    /**
     * base class for the Reply2Sender and the ForwardArticle actions
     */
    public abstract class ReplyForwardAction : IAction
    {
        internal EditMessageForm _form;

        protected void CheckForm()
        {
            if( _form != null && Settings.CloseOnReply )
            {
                _form.Close();
            }
        }

        protected static IEmailService GetEmailService()
        {
            return (IEmailService) Core.PluginLoader.GetPluginService( typeof( IEmailService ) );
        }

        public virtual void Execute( IActionContext context )
        {
            if( _form == null && context.Kind == ActionContextKind.Keyboard )
            {
                _form = context.Instance as EditMessageForm;
            }
            IResource article = context.SelectedResources[ 0 ];
            if( Settings.MarkAsReadOnReplyAndFormard && article.HasProp( NntpPlugin._propIsUnread ) )
            {
                new ResourceProxy( article ).SetProp( NntpPlugin._propIsUnread, false );
            }
        }

        public virtual void Update( IActionContext context, ref ActionPresentation presentation )
        {
			presentation.Visible = presentation.Enabled = true;	// Show and enable by default

			// If on the message editing form, never hide this button so that it won't pop out suddenly when we save the message and the resource appears
			// In other cases, preserve the prekv behavior where the action disapperas if unavailable
			bool	isAvail = ((context.SelectedResources != null) && (context.SelectedResources.Count != 0));
			if(_form != null)
				presentation.Enabled = isAvail;
			else
				presentation.Visible = isAvail;

			// Additional restrictions
            presentation.Enabled = presentation.Enabled && (GetEmailService() != null) && 
                                   (context.SelectedResources.Count == 1) &&
                                    context.SelectedResources[ 0 ].HasProp( Core.Props.LongBody );
        }
    }

    /**
     * Replies to sender of an article
     */
    public class Reply2Sender: ReplyForwardAction
    {
        public Reply2Sender() {}
        internal Reply2Sender( EditMessageForm form )
        {
            _form = form;
        }

        public override void Execute( IActionContext context )
        {
            base.Execute( context );
            IEmailService service = GetEmailService();
            IResource article = context.SelectedResources[ 0 ];
            string subject = article.GetPropText( Core.Props.Subject );
            if( !subject.ToLower().StartsWith( "re:" ) )
            {
                subject = "Re: " + subject;
            }
            string quote = Core.MessageFormatter.QuoteMessage( article, Core.Props.LongBody );
            CheckForm();
            IContactManager cm = Core.ContactManager;
            IResource from = article.GetLinkProp( cm.Props.LinkFrom );
            string name = cm.GetFullName( from );
            IResource emailAcc = article.GetLinkProp( cm.Props.LinkEmailAcctFrom );
            string email = ( emailAcc == null ) ? string.Empty : emailAcc.GetPropText( cm.Props.EmailAddress );
            service.CreateEmail( subject, quote, EmailBodyFormat.PlainText,
                new EmailRecipient[] { new EmailRecipient( name, email ) }, null, false );
        }
		
		public override void Update(IActionContext context, ref ActionPresentation presentation)
		{
			if(context.SelectedResources.Count == 1)
			{
				// There's exactly one resource selected; check its type
				// Ensure it's not a local article as we cannot reply to an author of a local article
				presentation.Enabled = presentation.Visible = (context.SelectedResources[0].Type == NntpPlugin._newsArticle);				
			}
			else
				presentation.Visible = false;	// Can be applied when there is exactly one resource
		}
    }

    /**
     * Forwards an article
     */
    public class ForwardArticle: ReplyForwardAction
    {
        public ForwardArticle() {}
        internal ForwardArticle( EditMessageForm form )
        {
            _form = form;
        }

        public override void Execute( IActionContext context )
        {
            base.Execute( context );
            IEmailService service = GetEmailService();
            StringBuilder forwardedBodyBuilder = StringBuilderPool.Alloc();
            try 
            {
                IResource article = context.SelectedResources[ 0 ];
                string subject = article.GetPropText( Core.Props.Subject );
                forwardedBodyBuilder.AppendFormat(
                    "\r\n\r\nFrom: {0}", NewsContactHelper.RestoreFromField( article ) );
                forwardedBodyBuilder.AppendFormat(
                    "\r\nSent: {0}\r\nNewsgroups: ", article.GetDateProp( Core.Props.Date ) );
                IResourceList groups = article.GetLinksFrom( NntpPlugin._newsGroup, NntpPlugin._propTo );
                if( groups.Count > 0 )
                {
                    forwardedBodyBuilder.Append( groups[ 0 ].GetPropText( Core.Props.Name ) );
                    for( int i = 1; i < groups.Count; ++i )
                    {
                        forwardedBodyBuilder.AppendFormat( ", {0}", groups[ i ].GetPropText( Core.Props.Name ) );
                    }
                }
                forwardedBodyBuilder.AppendFormat(
                    "\r\nSubject: {0}\r\n\r\n{1}", subject, article.GetPropText( Core.Props.LongBody ) );
                CheckForm();
                service.CreateEmail(
                    "Fw: " + subject, forwardedBodyBuilder.ToString(), EmailBodyFormat.PlainText, (EmailRecipient[]) null, null, true );
            }
            finally
            {
                StringBuilderPool.Dispose( forwardedBodyBuilder );
            }
        }
    }


    /**
     * open article in separate window
     */
    public class OpenArticleInSeparateWindow : ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            foreach( IResource selected in context.SelectedResources.ValidResources )
            {
                if( NewsFolders.IsInFolder( selected, NewsFolders.Drafts ) ||
                    NewsFolders.IsInFolder( selected, NewsFolders.Outbox ) )
                {
                    EditMessageForm.EditAndPostMessage( selected );
                }
                else
                {
                    EditMessageForm.OpenMessageInSeparateWindow( selected );
                }
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                foreach( IResource res in context.SelectedResources.ValidResources )
                {
                    if( res.HasProp( Core.Props.IsDeleted ) )
                    {
                        presentation.Visible = false;
                        return;
                    }
                }
            }
        }

    }

    /**
     * toggle formatting for article's preview
     */
    public class ToggleFormattingAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource resource = context.SelectedResources[ 0 ];
            ResourceProxy proxy = new ResourceProxy( resource );
            if( resource.HasProp( "NoFormat" ) )
            {
                proxy.DeleteProp( "NoFormat" );
            }
            else
            {
                proxy.SetProp( "NoFormat", true );
            }
            Core.ResourceBrowser.RedisplaySelectedResource();
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                presentation.Text = ( context.SelectedResources[ 0 ].HasProp( "NoFormat" ) ) ?
                    "Show as Formatted Text" : "Show as Plain Text";
            }
        }
    }

    /**
     * action for viewing headers
     */
    public class ViewHeadersAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            HeadersViewer.ViewHeaders( context.SelectedResources[ 0 ] );
        }
    }

    /**
     * action for folder creation
     */
    public class NewFolderAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource folder = NewsFolders.GetNewsFolderResource( context.SelectedResources[ 0 ], "New Folder", true );
            NntpPlugin._newsgroupsTreePane.EditResourceLabel( folder );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                IResource resource = context.SelectedResources[ 0 ];
                presentation.Visible = resource.Type == NntpPlugin._newsServer ||
                    ( resource.Type == NntpPlugin._newsFolder && !NewsFolders.IsDefaultFolder( resource ) );
            }
        }
    }

    /**
     * action for deleting newsfolder
     */
    public class DeleteFolderAction : ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource folder = context.SelectedResources[ 0 ];
            bool allowDelete = false;
            if( NewsFolders.IsDefaultFolder( folder ) )
            {
                if( folder.GetLinksTo( null, NntpPlugin._propTo ).Count == 0 ||
                    AskDeleteFolder() == DialogResult.Yes )
                {
                    allowDelete = true;
                }
            }
            else
            {
                if( new NewsTreeNode( folder ).Children.Count == 0 || AskDeleteFolder() == DialogResult.Yes )
                {
                    allowDelete = true;
                }
            }
            if( allowDelete )
            {
                Core.ResourceAP.QueueJob(
                    JobPriority.Immediate, new ResourceDelegate( DeleteFolderImpl ), folder );
            }
        }

        private static DialogResult AskDeleteFolder()
        {
            return MessageBox.Show( Core.MainWindow,
                "Do you want to delete the folder and its contents?", "Delete News Folder",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                presentation.Visible = !NewsFolders.IsDefaultFolder( context.SelectedResources[ 0 ] ) ||
                    Core.ResourceStore.GetAllResources( NntpPlugin._newsServer ).Count == 0;
            }
        }

        private static void DeleteFolderImpl( IResource folder )
        {
            NewsTreeNode node = new NewsTreeNode( folder );
            IResourceList childs = node.Children;
            foreach( IResource child in childs )
            {
                if( child.Type == NntpPlugin._newsGroup )
                {
                    UnsubscribeAction.Unsubscribe( child, true );
                }
                else
                {
                    DeleteFolderImpl( child );
                }
            }
            if( NewsFolders.IsDefaultFolder( folder ) )
            {
                folder.GetLinksTo( null, NntpPlugin._propTo ).DeleteAll();
            }
            folder.Delete();
        }
    }

    /**
     * action for refreshing article
     */
    public class RefreshArticleAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            RefreshArticleImpl( context.SelectedResourcesExpanded );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update(context, ref presentation );
            if( presentation.Visible )
            {
                foreach( IResource res in context.SelectedResourcesExpanded.ValidResources )
                {
                    if( res.HasProp( Core.Props.IsDeleted ) )
                    {
                        presentation.Visible = false;
                        return;
                    }
                }
                IResource article = context.SelectedResourcesExpanded[ 0 ];
                presentation.Text = ( article.HasProp( NntpPlugin._propHasNoBody ) ) ? "Download" : "Refresh";
                UpdateActionIfNetworkUnavailable.Update( ref presentation );
            }
        }

        internal static void RefreshArticleImpl( IResourceList articles )
        {
            if( !Core.ResourceAP.IsOwnerThread )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate,
                    new ResourceListDelegate( RefreshArticleImpl ), articles );
            }
            else
            {
                foreach( IResource article in articles )
                {
                    article.SetProp( NntpPlugin._propHasNoBody, true );
                    article.GetLinksOfType( null, NntpPlugin._propAttachment ).DeleteAll();
                    ArticlePreviewPane pane = NntpPlugin._previewPane;
                    if( pane != null && pane.IsArticleDisplayed( article ) )
                    {
                        pane.RedisplayArticle( article );
                    }
                    else
                    {
                        IResourceList groups = article.GetLinksOfType( NntpPlugin._newsGroup, NntpPlugin._propTo );
                        foreach( IResource groupRes in groups.ValidResources )
                        {
                            IResource server = new NewsgroupResource( groupRes ).Server;
                            if( server != null )
                            {
                                NntpConnection articlesConnection =
                                    NntpConnectionPool.GetConnection( server, "foreground" );
                                NntpDownloadArticleUnit downloadUnit =
                                    new NntpDownloadArticleUnit( article, groupRes, JobPriority.Immediate, true );
                                articlesConnection.StartUnit( Int32.MaxValue - 1, downloadUnit );
                            }
                        }
                    }
                }
            }
        }
    }

	/// <summary>
	/// Action for copying article URL.
	/// </summary>
    public class CopyArticleURLAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
        	try
			{
				string sUri = GetArticleUri(context.SelectedResources[0]);
				if(sUri != null)
					Clipboard.SetDataObject(sUri);
				else
					MessageBox.Show(Core.MainWindow, "Could not get the article URL.", Core.ProductName, MessageBoxButtons.OK,
					                MessageBoxIcon.Error);
			}
			catch( ExternalException e )
			{
				Utils.DisplayException( Core.MainWindow, e, "Error" );
			}
		}
    	
    	/// <summary>
    	/// Gets an uri for the news article. May return Null in case of problems.
    	/// </summary>
    	/// <param name="article"></param>
    	/// <returns></returns>
    	public static string GetArticleUri(IResource article)
    	{
            string articleId = ParseTools.UnescapeCaseSensitiveString( article.GetPropText( NntpPlugin._propArticleId ) );
            if( articleId.Length > 0 )
            {
                IResourceList servers = Core.ResourceStore.EmptyResourceList;
                IResourceList groups = article.GetLinksFrom( NntpPlugin._newsGroup, NntpPlugin._propTo );
                foreach( IResource group in groups )
                {
                    IResource server = new NewsgroupResource( group ).Server;
                    if( server != null )
                    {
                        servers = servers.Union( server.ToResourceList(), true );
                    }
                }
                if( servers.Count > 0 )
                {
                    servers.Sort( "LastUpdated", false );
                    return "news://" + new ServerResource(servers[0]).Name + "/" + articleId.Trim('<', '>');
                }
            }
    		return null;
    	}
    }

    /**
     * action for cancelling article
     */
    public class CancelArticleAction: ActionOnSingleResource
    {
        private IResource _controlArticle;

        public override void Execute( IActionContext context )
        {
            if( Settings.ConfirmCancel )
            {
                MessageBoxWithCheckBox.Result result = MessageBoxWithCheckBox.ShowYesNo(
                    Core.MainWindow, "Do you wish to cancel selected article?",
                    "Confirm Cancellation", "Never &ask confirmation", false );
                if( result.Checked )
                {
                    Settings.ConfirmCancel.Save( false );
                }
                if( result.IdPressedButton != (int)DialogResult.Yes )
                {
                    return;
                }
            }
            IResource article = context.SelectedResources[ 0 ];
            string id = article.GetPropText( NntpPlugin._propArticleId );
            if( id.Length > 0 )
            {
                string subj = "cancel " + ParseTools.UnescapeCaseSensitiveString( id );
                string fromField = NewsContactHelper.RestoreFromField( article );
                string nntpText = "From: " + fromField + "\r\nSubject: " + subj + "\r\nControl: " + subj + "\r\nNewsgroups: ";
                IResourceList groups = article.GetLinksFrom( NntpPlugin._newsGroup, NntpPlugin._propTo );
                if ( groups.Count > 0 )
                {
                    nntpText += new NewsgroupResource( groups[ 0 ] ).Name;
                    for( int i = 1; i < groups.Count; ++i )
                    {
                        nntpText += ',';
                        nntpText += new NewsgroupResource( groups[ i ] ).Name;
                    }
                }
                nntpText += "\r\n\r\n.\r\n";
                _controlArticle = NewsFolders.PlaceArticle(
                    null, NewsFolders.Drafts, groups, fromField, string.Empty, string.Empty,
                    string.Empty, string.Empty, nntpText, null );
                NntpClientHelper.PostArticle( _controlArticle, ControlPostFinished, true );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible && Core.SettingStore.ReadBool( "NNTP", "CancelDenied", true ) )
            {
                IContactManager contactManager = Core.ContactManager;
                IResource from = context.SelectedResources[ 0 ].GetLinkProp( contactManager.Props.LinkFrom );
                if( from == null || !contactManager.GetContact( from ).IsMyself )
                {
                    presentation.Enabled = false;
                }
                UpdateActionIfNetworkUnavailable.Update( ref presentation );
            }
        }

        private void ControlPostFinished( AsciiProtocolUnit unit )
        {
            string error = ( (NntpPostArticleUnit) unit ).Error;
            if( error != null )
            {
                Core.UserInterfaceAP.QueueJob( new LineDelegate( DisplayError ), error );
            }
            new ResourceProxy( _controlArticle ).DeleteAsync();
        }

        private static void DisplayError( string error )
        {
            MessageBox.Show( "Cancellation of the article failed.\r\nError details: " + error, "Cancellation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error );
        }
    }

    /**
     * action for deleting single news server
     */
    public class DeleteNewsServerAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            WarnAndDelete( context.SelectedResources[ 0 ], Core.MainWindow );
        }

        public override void Update(IActionContext context, ref ActionPresentation presentation)
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                presentation.Text = "Remove";
            }
        }

        internal static void WarnAndDelete( IResource server, IWin32Window parentWindow )
        {
            IResourceList groups = new NewsTreeNode( server ).Groups;
            if( groups.Count == 0 ||
                MessageBox.Show( parentWindow, "Are you sure you want to remove the '" + server.DisplayName +
                "' news server with all its groups which you are subscribed to?",
                "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question ) == DialogResult.Yes )
            {
                Cursor old = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    Core.ResourceAP.RunUniqueJob( new ResourceDelegate( DoDelete ), server  );
                }
                finally
                {
                    Cursor.Current = old;
                }
            }
        }

        private static void DoDelete( IResource resource )
        {
            IResourceList children = new NewsTreeNode( resource ).Children;
            IAsyncProcessor resourceAP = Core.ResourceAP;
            foreach( IResource child in children )
            {
                if( child.Type == NntpPlugin._newsGroup )
                {
                    resourceAP.QueueJob( JobPriority.Immediate,
                        new UnsubscribeAction.UnsubscribeDelegate( UnsubscribeAction.Unsubscribe ), child, true );
                }
                else
                {
                    DoDelete( child );
                }
            }
            resourceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( resource.Delete ) );
        }
    }

    /**
     * action for searching in a newsgroup
     */
    public class SearchInNewsgroup: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            IResource template = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, Core.Props.Name,
                                                                        "Appeared in the %specified% newsgroup(s)" );
            IResource condition = FilterConvertors.InstantiateTemplate( template, context.SelectedResources, new string[]{ "Article" } );
            Core.FilteringFormsManager.ShowAdvancedSearchForm( "", new string[]{ "Article" }, new IResource[] { condition }, null );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = presentation.Visible = context.SelectedResources.Count > 0;
            if( context.SelectedResources.Count > 1 )
                presentation.Text = "Search in these Newsgroups";
        }
    }

    /**
     * action for searching in a newsgroup
     */
    public class SearchInNewsgroupFolder: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            IResourceList selection = Core.ResourceStore.EmptyResourceList;
            foreach( IResource folder in context.SelectedResources )
            {
                selection = selection.Union( folder.GetLinksTo( "NewsGroup", "Parent" ));
            }
            IResource template = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, Core.Props.Name,
                                                                        "Appeared in the %specified% newsgroup(s)" );
            IResource condition = FilterConvertors.InstantiateTemplate( template, selection, new string[]{ "Article" } );
            Core.FilteringFormsManager.ShowAdvancedSearchForm( "", new string[]{ "Article" }, new IResource[] { condition }, null );
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( presentation.Visible && presentation.Enabled )
            {
                int count = 0;
                foreach( IResource res in context.SelectedResources.ValidResources )
                {
                    count += res.GetLinksTo( "NewsGroup", Core.Props.Parent ).Count;
                    if ( count > 0 )
                    {
                        break;
                    }
                }
                if ( count == 0 )
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
        }
    }

    /**
     * action for saving articles as files
     */
    public class SaveAsAction : ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            IResourceList articles = context.SelectedResources;
            ISettingStore settings = Core.SettingStore;
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.RestoreDirectory = true;
            dlg.InitialDirectory = settings.ReadString( "NNTP", "SaveAsDialog.InitialDirectory",
                Environment.GetFolderPath( Environment.SpecialFolder.Personal ) );
            dlg.Filter = "txt files (*.txt)|*.txt|Outlook Express files (*.nws)|*.nws|All files (*.*)|*.*";
            dlg.FilterIndex = settings.ReadInt( "NNTP", "SaveAsDialog.FilterIndex", 0 );
            if( articles.Count == 1 )
            {
                string name = articles[ 0 ].DisplayName;
                for( int i = 0; i < name.Length; ++i )
                {
                    char c = name[ i ];
                    if( !Char.IsWhiteSpace( c ) && !Char.IsLetterOrDigit( c ) )
                    {
                        name = name.Replace( c, '_' );
                    }
                }
                dlg.FileName = name;
            }
            if( dlg.ShowDialog() == DialogResult.OK )
            {
                try
                {
                    settings.WriteString( "NNTP", "SaveAsDialog.InitialDirectory",
                        IOTools.GetDirectoryName( new FileInfo( dlg.FileName ) ) );
                    settings.WriteInt( "NNTP", "SaveAsDialog.FilterIndex", dlg.FilterIndex );
                    IResourceList groups = Core.ResourceStore.EmptyResourceList;
                    for( int i = 0; i < articles.Count && groups.Count == 0; ++i )
                    {
                        groups = articles[ i ].GetLinksFrom( NntpPlugin._newsGroup, NntpPlugin._propTo );
                    }
                    Encoding encoding = ( groups.Count == 0 ) ? Encoding.Default :
                        Encoding.GetEncoding( new ServerResource( new NewsgroupResource( groups[ 0 ] ).Server ).Charset );
                    Stream stream = dlg.OpenFile();
                    if( stream != null )
                    {
                        using( StreamWriter writer = new StreamWriter( stream, encoding ) )
                        {
                            foreach( IResource article in articles )
                            {
                                if( article.HasProp( NntpPlugin._propArticleHeaders ) )
                                {
                                    writer.WriteLine( article.GetPropText( NntpPlugin._propArticleHeaders ) );
                                }
                                if( article.HasProp( Core.Props.LongBody ) )
                                {
                                    writer.WriteLine( article.GetPropText( Core.Props.LongBody ) );
                                }
                                else if( article.HasProp( NntpPlugin._propHtmlContent ) )
                                {
                                    writer.WriteLine( article.GetPropText( NntpPlugin._propHtmlContent ) );
                                }
                                if( article != articles[ articles.Count - 1 ] )
                                {
                                    writer.WriteLine();
                                    writer.WriteLine( '.' );
                                    writer.WriteLine();
                                }
                            }
                        }
                    }
                }
                catch {}
            }
        }
    }

    /**
     * action for switching the "mark all as read on exit" mode
     */
    public class SwitchMarkReadAction : ActionOnSingleResource
    {
        private readonly string _propName;

        public SwitchMarkReadAction( string propName )
        {
            _propName = propName;
        }

        public override void Execute( IActionContext context )
        {
            IResource resource = context.SelectedResources[ 0 ];
            ResourceProxy proxy = new ResourceProxy( resource );
            if( resource.HasProp( _propName ) )
            {
                proxy.DeleteProp( _propName );
            }
            else
            {
                proxy.SetProp( _propName, true );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                IResource resource = context.SelectedResources[ 0 ];
                if( presentation.Visible == !NewsFolders.IsDefaultFolder( resource ) )
                {
                    presentation.Checked = resource.HasProp( _propName );
                }
            }
        }
    }

    public class StopThreadAction : ActionOnSingleResource
    {
        private const string _cNntpIniSection = "NNTP";
        private const string _cShowConfirmKey = "ShowMarkReadOnStopThread";
        private const string _cMarkReadOnStopKey = "MarkAsReadOnThreadStop";

        private const string _cMessage = "Mark all articles in thread read?";
        private const string _cTitle = "Mark Thread Read";
        private const string _cQuestion = "Do not show this message again";

        private delegate void MarkReadDelegate( IResource res, bool read );

        public override void Execute( IActionContext context )
        {
            IResource selected = context.SelectedResources[ 0 ];
            IResource root;
            bool hasProp = ConversationBuilder.CheckPropOnParents( selected, NntpPlugin._propIsIgnoredThread, out root );
            if( !hasProp )
            {
                bool markAsReadOnStop = Core.SettingStore.ReadBool( _cNntpIniSection, _cMarkReadOnStopKey, true );
                bool showConfirmMarkAsRead = Core.SettingStore.ReadBool( _cNntpIniSection, _cShowConfirmKey, true );
                bool isCtrlKey = (Control.ModifierKeys & Keys.Control ) > 0;
                if( showConfirmMarkAsRead || isCtrlKey )
                {
                    MessageBoxWithCheckBox.Result result = 
                        MessageBoxWithCheckBox.ShowYesNo( Core.MainWindow, _cMessage, _cTitle, _cQuestion, !showConfirmMarkAsRead );
                    if( result.Checked )
                        Core.SettingStore.WriteBool( _cNntpIniSection, _cShowConfirmKey, false );

                    markAsReadOnStop = (result.IdPressedButton == (int)DialogResult.Yes);
                }

                if( markAsReadOnStop )
                {
                    Core.ResourceAP.RunJob( new MarkReadDelegate( ConversationBuilder.MarkConversationRead ), selected, true );
                }

                ResourceProxy proxy = new ResourceProxy( selected );
                proxy.BeginUpdate();
                proxy.SetProp( NntpPlugin._propIsIgnoredThread, true );
                proxy.SetProp( NntpPlugin._propThreadVisibilityToggleDate, DateTime.Now );
                proxy.EndUpdate();
            }
            else
            {
                DateTime    ignoreStartDate = root.GetDateProp( NntpPlugin._propThreadVisibilityToggleDate );
                IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( NntpPlugin._newsArticle );

                //  Collect the thread, take only deleted resources.
                //  If a deleted resource was received after the thread was
                //  marked as "Stop updating" then we can Undelete it.
                IResourceList thread = ConversationBuilder.UnrollConversation( root );
                foreach( IResource article in thread )
                {
                    DateTime dateTime = article.GetDateProp( Core.Props.Date );
                    if( article.HasProp( Core.Props.IsDeleted ) && dateTime > ignoreStartDate )
                    {
                        deleter.UndeleteResource( article );
                    }
                }

                new ResourceProxy( root ).DeleteProp( NntpPlugin._propIsIgnoredThread );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if( presentation.Visible )
            {
                if( ConversationBuilder.CheckPropOnParents( context.SelectedResources[ 0 ],
                                                            NntpPlugin._propIsIgnoredThread ))
                {
                    presentation.Text = "Restore Ignored Articles";
                }
            }
        }
    }

    internal class UpdateActionIfNetworkUnavailable
    {
        public static void Update( ref ActionPresentation presentation )
        {
            if( presentation.Visible && !Utils.IsNetworkConnected() )
            {
                presentation.ToolTip = NntpPlugin._networkUnavailable;
                presentation.Enabled = false;
            }
        }
    }
}