/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using Tasks;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class MailIconProvider : IOverlayIconProvider
    {
        private readonly Icon[] _forwarded = new Icon[ 1 ];
        private readonly Icon[] _replied = new Icon[ 1 ];

        internal MailIconProvider()
        {
            _forwarded[ 0 ] = OutlookPlugin.LoadIconFromAssembly( "OutlookPlugin.Icons.forward_arrow.ico" );
            _replied[ 0 ] = OutlookPlugin.LoadIconFromAssembly( "OutlookPlugin.Icons.reply_arrow.ico" );
        }

        public Icon[] GetOverlayIcons( IResource res )
        {
            int  val = res.GetIntProp( "PR_ICON_INDEX" );
            if( val == 261 )
            {
                return _replied;
            }
            else
            if( val == 262 )
            {
                return _forwarded;
            }
            else
            {
                IResource folder = Mail.GetParentFolder( res );
                if ( folder != null )
                {
                    return FolderIconProvider.GetOverlayIcon( folder );
                }
            }
            return null;
        }
    }

    internal class FolderIconProvider : IResourceIconProvider, IOverlayIconProvider
    {
        private Icon _ignored;
        private Icon _opened;
        private Icon _closed;

        private static Icon[] _problem;

        #region IResourceIconProvider Members

        public Icon GetResourceIcon( IResource resource )
        {
            if ( Folder.IsIgnored( resource ) )
            {
                if ( _ignored == null )
                {
                    _ignored = OutlookPlugin.LoadIconFromAssembly( "OutlookPlugin.Icons.IgnoredFolder.ico" );
                }
                return _ignored;
            }

            if ( resource.GetIntProp( Core.Props.Open ) == 1 || 
                resource.GetIntProp( PROP.OpenIgnoreFolder ) == 1 || 
                resource.GetIntProp( PROP.OpenSelectFolder ) == 1 )
            {
                if ( _opened == null )
                {
                    _opened = OutlookPlugin.LoadIconFromAssembly( "OutlookPlugin.Icons.OPENFOLD2.ICO" );
                }
                return _opened;
            }
            if ( _closed == null )
            {
                _closed = OutlookPlugin.LoadIconFromAssembly( "OutlookPlugin.Icons.CLSDFOLD2.ICO" );
            }
            return _closed;
        }

        public Icon GetDefaultIcon(string resType)
        {
            if ( _closed == null )
            {
                _closed = OutlookPlugin.LoadIconFromAssembly( "OutlookPlugin.Icons.CLSDFOLD2.ICO" );
            }
            return _closed;
        }

        #endregion

        #region IOverlayIconProvider Members
        public Icon[] GetOverlayIcons(IResource resource)
        {
            return GetOverlayIcon( resource );
        }

        #endregion
        public static Icon[] GetOverlayIcon(IResource resource)
        {
            PairIDs folderIDs = PairIDs.Get( resource );
            if ( folderIDs != null )
            {
                if ( OutlookSession.WereProblemWithOpeningStorage( folderIDs.StoreId ) || 
                    OutlookSession.WereProblemWithOpeningFolder( folderIDs.EntryId ) )
                {
                    if ( _problem == null )
                    {
                        _problem = new Icon[1];
                        _problem[0] = OutlookPlugin.LoadIconFromAssembly( "OutlookPlugin.Icons.inaccessible.ico" );
                    }
                    return _problem;
                }
            }
            return null;
        }

    }
    internal class StreamProvider: IStreamProvider
    {
        #region IStreamProvider Members

        private static Stream GetResourceStreamImpl( IResource resource, int threadId )
        {
            try
            {
                return new OutlookAttachment( resource ).Stream;
            }
            catch ( ThreadAbortException ex )
            {
                Tracer._TraceException( ex );                    
            }
            catch ( OutlookAttachmentException exception )
            {
                Core.ReportBackgroundException( exception );
            }
            return null;
        }

        public Stream GetResourceStream( IResource resource )
        {
            if ( Settings.SyncAttachments )
            {
                try
                {
                    return (Stream)OutlookSession.OutlookProcessor.RunUniqueJob( "Get resource stream",
                        new Resource2StreamDelegate( GetResourceStreamImpl ), resource, Thread.CurrentThread.GetHashCode() );
                }
                catch ( OutlookThreadTimeoutException ex )
                {
                    Tracer._TraceException( ex );
                }

            }
            return null;
        }

        #endregion
    }

    internal class ResourceDisplayer: IResourceDisplayer
    {
        #region IResourceDisplayer Members

        IDisplayPane IResourceDisplayer.CreateDisplayPane( string resourceType )
        {
            if ( resourceType == STR.Email || resourceType == STR.EmailFile )
            {
                MailBodyView bodyView = new MailBodyView();
                return bodyView;
            }
            return null;
        }

        #endregion
    }

    public class OutlookViewsInitializer: IViewsConstructor
    {
        public const string    AuthorPostedMailName = "Author posted a mail";
        public const string    AuthorPostedMailDeep = "postemail";
        public const string    LocatesInFolderName = "Locates in %specified% outlook folder";
        public const string    LocatesInFolderDeep = "outlookfolder";
        public const string    SentViaAccountName = "Sent/received through %specified% email account";
        public const string    SentViaAccountDeep = "sentviaaccount";
        public const string    SentToMailingListXName = "Sent to the %specified% mailing list(s)";
        public const string    SentToMailingListXDeep = "tomailinglist";
        public const string    ImportantMailName = "Email is of %specified% importance";
        public const string    ImportantMailDeep = "importantmail";
        public const string    HasAttachmentName = "Email has an attachment";
        public const string    HasAttachmentDeep = "hasattachment";
        public const string    SentOnlyToMeName = "Sent only to me";
        public const string    SentOnlyToMeDeep = "senttome";
        public const string    AuthorInABName = "Message's Author is in an Address Book";
        public const string    AuthorInABDeep = "authorinAB";

        public const string    SetImportanceActionName = "Set %specified% importance";
        public const string    SetImportanceActionDeep = "setimportance";
        public const string    CopyEmailToFolderName = "Copy e-mail to %Outlook folder%";
        public const string    CopyEmailToFolderDeep = "copy2folder";
        public const string    MoveEmailToFolderName = "Move e-mail to %Outlook folder%";
        public const string    MoveEmailToFolderDeep = "move2folder";

        #region IViewsConstructor interface
        //---------------------------------------------------------------------
        //  Register mail-dependent conditions, views and rules
        //---------------------------------------------------------------------
        void IViewsConstructor.RegisterViewsFirstRun()
        {
            string[]        applTypes = new string[ 1 ] { STR.Email };
            IFilterManager  fMgr = Core.FilterManager;
            IResource       cond;

            cond = fMgr.CreateConditionTemplate( LocatesInFolderName, LocatesInFolderDeep, applTypes, ConditionOp.In, STR.MAPIFolder, STR.MAPIFolder );
            fMgr.AssociateConditionWithGroup( cond, "Email Conditions" );

            cond = fMgr.CreateConditionTemplate( SentViaAccountName, SentViaAccountDeep, applTypes, ConditionOp.In, "EmailAccount", "EmailAccountTo" );
            fMgr.AssociateConditionWithGroup( cond, "Address and Contact Conditions" );

            cond = fMgr.CreateStandardCondition( HasAttachmentName, HasAttachmentDeep, applTypes, "Attachment", ConditionOp.HasLink );
            fMgr.AssociateConditionWithGroup( cond, "Email Conditions" );

            cond = fMgr.CreateConditionTemplate( SentToMailingListXName, SentToMailingListXDeep, applTypes, ConditionOp.In, "MailingList", "To" );
            fMgr.AssociateConditionWithGroup( cond, "Address and Contact Conditions" );

            IResource myResType = Core.ResourceStore.FindUniqueResource( "ResourceType", Core.Props.Name, STR.Email );
            cond = fMgr.CreateStandardCondition( AuthorPostedMailName, AuthorPostedMailDeep, new string[]{ "Contact" }, 
                                                 "LinkedResourcesOfType", ConditionOp.In, myResType.ToResourceList() );
            fMgr.AssociateConditionWithGroup( cond, "Address and Contact Conditions" );
        }
        void IViewsConstructor.RegisterViewsEachRun()
        {
            IResource res;
            IFilterManager fMgr = Core.FilterManager;
            string[]       applTypes = new string[ 1 ] { STR.Email };
            fMgr.RegisterRuleApplicableResourceType( "Email" );

            //  Actions
            res = fMgr.RegisterRuleActionTemplate( MoveEmailToFolderName, MoveEmailToFolderDeep,
                                                   new MoveToFolderRuleAction(), ConditionOp.In, STR.MAPIFolder );
            fMgr.MarkActionTemplateAsSingleSelection( res );

            res = fMgr.RegisterRuleActionTemplate( CopyEmailToFolderName, CopyEmailToFolderDeep,
                                                   new MoveToFolderRuleAction( true ), ConditionOp.In, STR.MAPIFolder );
            fMgr.MarkActionTemplateAsSingleSelection( res );

            fMgr.RegisterRuleActionTemplateWithUIHandler( SetImportanceActionName, SetImportanceActionDeep, new SetImportanceRuleAction(),
                                                          applTypes, new EmailImportanceUIHandler(), ConditionOp.Eq );

            //  Conditions/Templates
            res = fMgr.CreateConditionTemplateWithUIHandler( ImportantMailName, ImportantMailDeep, applTypes, 
                                                             new EmailImportanceUIHandler(), ConditionOp.Eq, STR.Importance );
            fMgr.AssociateConditionWithGroup( res, "Email Conditions" );

            res = fMgr.RegisterCustomCondition( SentOnlyToMeName, SentOnlyToMeDeep, null, new SentOnly2MeCondition() );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );
            fMgr.MarkConditionOnlyForRule( res );

            res = fMgr.CreateStandardCondition( AuthorInABName, AuthorInABDeep, null, "From>InAddressBook", ConditionOp.HasProp );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );
            fMgr.MarkConditionOnlyForRule( res );

            //  Notifications
            Core.NotificationManager.RegisterNotifyMeCondition( "Email", fMgr.Std.FromContactX, Core.ResourceStore.GetPropId( "From" ) );
        }
        #endregion IViewsConstructor interface
    }

    public class OutlookUpgrade1ViewsInitializer: IViewsConstructor
    {
        #region IViewsConstructor interface
        void IViewsConstructor.RegisterViewsFirstRun()
        {
            //-----------------------------------------------------------------
            //  All conditions, templates and actions must have their deep names
            //-----------------------------------------------------------------
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, OutlookViewsInitializer.LocatesInFolderName, OutlookViewsInitializer.LocatesInFolderDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, OutlookViewsInitializer.SentViaAccountName, OutlookViewsInitializer.SentViaAccountDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, OutlookViewsInitializer.SentToMailingListXName, OutlookViewsInitializer.SentToMailingListXDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, OutlookViewsInitializer.ImportantMailName, OutlookViewsInitializer.ImportantMailDeep );

            AscribeDeepName( FilterManagerProps.ConditionResName, OutlookViewsInitializer.HasAttachmentName, OutlookViewsInitializer.HasAttachmentDeep );
            AscribeDeepName( FilterManagerProps.ConditionResName, OutlookViewsInitializer.SentOnlyToMeName, OutlookViewsInitializer.SentOnlyToMeDeep );
            AscribeDeepName( FilterManagerProps.ConditionResName, OutlookViewsInitializer.AuthorInABName, OutlookViewsInitializer.AuthorInABDeep );

            AscribeDeepName( FilterManagerProps.RuleActionTemplateResName, OutlookViewsInitializer.CopyEmailToFolderName, OutlookViewsInitializer.CopyEmailToFolderDeep );
            AscribeDeepName( FilterManagerProps.RuleActionTemplateResName, OutlookViewsInitializer.MoveEmailToFolderName, OutlookViewsInitializer.MoveEmailToFolderDeep );
            AscribeDeepName( FilterManagerProps.RuleActionTemplateResName, OutlookViewsInitializer.SetImportanceActionName, OutlookViewsInitializer.SetImportanceActionDeep );

            //-----------------------------------------------------------------
            //  Register standard (for the plugin) tray icon rule.
            //-----------------------------------------------------------------
            Core.TrayIconManager.RegisterTrayIconRule( "Unread mail message(s)", new string[ 1 ] { STR.Email },
                                                       new IResource[] { Core.FilterManager.Std.ResourceIsUnread },
                                                       null, OutlookPlugin.LoadIconFromAssembly( "OutlookPlugin.Icons.unread.ico" ) );

            //-----------------------------------------------------------------
            //  If necessary relink all rules from "Delete e-mail" action to
            //  standard and more generic "Delete Resource" action.
            //-----------------------------------------------------------------
            IResource  action = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName, Core.Props.Name, "Delete e-mail" );
            if( action != null )
            {
                IResourceList   linkedRules = action.GetLinksOfType( null, "LinkedAction" );
                for( int i = 0; i < linkedRules.Count; i++ )
                {
                    linkedRules[ i ].SetProp( "LinkedAction", Core.FilterManager.Std.DeleteResourceAction );
                }
                action.Delete();
            }
        }        
        void IViewsConstructor.RegisterViewsEachRun()
        {
            IResource r = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName,
                                                                 "DeepName", OutlookViewsInitializer.SentToMailingListXDeep );
            if( r != null )
                r.SetProp( Core.Props.ContentType, STR.Email );

            r = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName,
                                                       "DeepName", OutlookViewsInitializer.HasAttachmentDeep );
            if( r != null )
                r.SetProp( "ApplicableToProp", STR.AttachmentType );
        }

        private static void AscribeDeepName( string type, string name, string deepName )
        {
            IResource res = Core.ResourceStore.FindUniqueResource( type, Core.Props.Name, name );
            if( res != null )
                res.SetProp( "DeepName", deepName );
        }
        #endregion IViewsConstructor interface
    }

    public class OutlookUpgrade2ViewsInitializer: IViewsConstructor
    {
        #region IViewsConstructor interface
        void IViewsConstructor.RegisterViewsFirstRun()
        {
            IResource res;
            IFilterManager fMgr = Core.FilterManager;

            //  Conditions/Templates
            IResource myResType = Core.ResourceStore.FindUniqueResource( "ResourceType", Core.Props.Name, STR.Email );
            res = fMgr.CreateStandardCondition( OutlookViewsInitializer.AuthorPostedMailName, OutlookViewsInitializer.AuthorPostedMailDeep,
                                                new string[]{ "Contact" }, "LinkedResourcesOfType", ConditionOp.In, myResType.ToResourceList() );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );
        }        
        void IViewsConstructor.RegisterViewsEachRun()
        {
        }
        #endregion IViewsConstructor interface
    }

    internal class ResourceTextProvider: IResourceTextProvider
    {
        static bool ProcessResourceTextImpl( IResource res, IResourceTextConsumer consumer )
        {
            OutlookProcessor.CheckState();
            try
            {
                OutlookProcessor processor = OutlookSession.OutlookProcessor;
                if ( processor != null )
                {
                    MailBodyDescriptorDelegate myDelegate = CreateMailBodyDescriptor;
                    MailBodyDescriptor mailBody = (MailBodyDescriptor)processor.RunUniqueJob( myDelegate, res );
                    if( mailBody != null && Core.State != CoreState.ShuttingDown )
                    {
                        //  Order of sections: Source, Subject, Body.
                        IResource resPerson = res.GetLinkProp( Core.ContactManager.Props.LinkFrom );
                        IResource resAccount = res.GetLinkProp( PROP.EmailAccountFrom );
                        if ( resPerson != null )
                        {
                            //  Construct [From] section out of contact name and its account
                            string fromText = resPerson.DisplayName;
                            if( resAccount != null )
                                fromText += " " + resAccount.DisplayName;
                            consumer.AddDocumentFragment( res.Id, fromText, DocumentSection.SourceSection );
                        }
                        consumer.AddDocumentHeading( res.Id, mailBody.Subject );
                        consumer.RestartOffsetCounting();
                        if ( mailBody.IsHTML )
                        {
                            HtmlIndexer.IndexHtml( res, mailBody.Body, consumer, DocumentSection.BodySection );
                        }
                        else
                        {
                            consumer.AddDocumentFragment( res.Id, mailBody.Body.Replace( "\r\n", "\n" ) );
                        }
                    }
                }
            }
            catch( OutlookThreadTimeoutException )
            {
                if ( consumer.Purpose == TextRequestPurpose.Indexing )
                {
                    // retry indexing of the email later
                    Guard.QueryIndexingWithCheckId( res );
                }
                return false;
            }
            return true;
        }

        #region IResourceTextProvider Members
        bool IResourceTextProvider.ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            #region Preconditions
            Guard.NullArgument( res, "res" );
            Guard.NullArgument( consumer, "consumer" );
            #endregion Preconditions

            if ( res.Type == STR.Email || res.Type == STR.EmailFile )
            {
                if ( !ProcessResourceTextImpl( res, consumer ) )
                {
                    return false;
                }
            }
/*
            IResource mail = res.GetLinkProp( PROP.Attachment );
            if( mail != null && mail.Type == STR.Email )
            {
                consumer.AddDocumentHeading( res.Id, res.GetPropText( Core.Props.Name ) );
                IResource resPerson = res.GetLinkProp(PROP.From);
                if (resPerson != null)
                {
                    consumer.AddDocumentFragment( res.Id, resPerson.DisplayName, DocumentSection.SourceSection );
                }
            }
*/
            return true;
        }

        private static MailBodyDescriptor CreateMailBodyDescriptor( IResource mail )
        {
            return new MailBodyDescriptor( mail );
        }
        #endregion
    }

    internal class OutlookLinksPaneFilter: ILinksPaneFilter
    {
        public bool AcceptLinkType( IResource displayedResource, int propId, ref string displayName )
        {
            if ( propId == PROP.MAPIFolder )
            {
                displayName = "Folder";
            }
            return true;
        }

        public bool AcceptLink( IResource displayedResource, int propId, IResource targetResource,
                                ref string linkTooltip )
        {
            return true;
        }

        public bool AcceptAction( IResource displayedResource, IAction action )
        {
            return true;
        }
    }

    internal class OutlookLinksPaneFilterForTasks : ILinksPaneFilter
    {
        public bool AcceptLinkType(IResource displayedResource, int propId, ref string displayName)
        {
            return propId != PROP.OwnerStore && propId != PROP.MAPIFolder;
        }

        public bool AcceptLink( IResource displayedResource, int propId, IResource targetResource,
            ref string linkTooltip )
        {
            return true;
        }

        public bool AcceptAction(IResource displayedResource, IAction action)
        {
            return true;
        }
    }

    internal class EmailImportanceUIHandler : IStringTemplateParamUIHandler
    {
        string Value = string.Empty, Representation = string.Empty;

        public DialogResult  ShowUI( IWin32Window h )
        {
            EmailImportanceForm form = new EmailImportanceForm( Value );
            DialogResult  result = form.ShowDialog( h );
            form.Dispose();
            Value = form.Value;
            Representation = form.Representation;
            return result;
        }
        public IResource    Template        { set { } }
        public string       CurrentValue    { set { Value = value; } }
        public string       Result          { get {  return Value;           } }
        public string       DisplayString   { get {  return Representation;  } }
    }

    public class SetImportanceRuleAction : IRuleAction
    {
        public void   Exec( IResource res, IActionParameterStore actionStore )
        {
            string  val = actionStore.ParameterAsString().ToLower();
            ResourceProxy proxy = new ResourceProxy( res );
            proxy.BeginUpdate();
            if( val == "1" || val == "-1" )
                proxy.SetProp( STR.Importance, Int32.Parse( val ) );
            else
                proxy.DeleteProp( STR.Importance );
            proxy.EndUpdate();
        }
    }
}