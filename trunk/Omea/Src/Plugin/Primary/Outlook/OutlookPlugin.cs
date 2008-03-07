/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;


namespace JetBrains.Omea.OutlookPlugin
{
    [PluginDescriptionAttribute("JetBrains Inc.", "Support for instant integration with Microsoft Outlook - Mails, Folders, Tasks, Categories, Contacts.")]
    public class OutlookPlugin: IPlugin, IEmailService
    {
        private bool _startupStatus = true;
        private readonly Tracer _tracer = new Tracer( "Outlook plugin" );

        public static OutlookPlugin _plugin;

        private OutlookProcessor _outlookProcessor;
        private OutlookUIHandler _outlookUIHandler = null;

        public OutlookUIHandler OutlookUIHandler
        {
            get {  return _outlookUIHandler;  }
        }

        #region IPlugin Members

        static private string GetMAPIFolderToolTip( IResource folder )
        {
            if ( Folder.IsIgnored( folder ) )
            {
                return "This folder is not indexed. Messages are not imported and processed.";
            }
            /*
            int count = folder.GetLinkCount( PROP.MAPIFolder );
            return "This folder contains " + count + " message(s).";
            */
            return null;
        }

        public void Register()
        {
            _plugin = this;
            _tracer.Trace( "Start registering..." );
            Core.AddExceptionReportData( "\nOutlookPlugin is enabled" );
            Settings.LoadSettings();

            REGISTRY.RegisterTypes( this, Core.ContactManager );

            _tracer.Trace( "Start OutlookProcessor..." );
            _outlookProcessor = new OutlookProcessor();

            _outlookUIHandler = new OutlookUIHandler();

            if ( !_outlookProcessor.IsStarted )
            {
                _tracer.Trace( "OutlookProcessor failed to start" );
                Core.AddExceptionReportData( "\nOutlookProcessor failed to start" );
                Core.AddExceptionReportData( "\nOutlook plugin cannot be loaded.\n" +  _outlookProcessor.LastException.Message );
                MsgBox.Error( "Outlook plugin", "Outlook plugin cannot be loaded.\n" +  _outlookProcessor.LastException.Message );

                _startupStatus = false;
                Core.ActionManager.DisableXmlActionConfiguration( Assembly.GetExecutingAssembly() );
                return;
            }
            _tracer.Trace( "Start OutlookProcessor OK" );
            _outlookProcessor.SynchronizeMAPIInfoStores();

            if ( Settings.ExportTasks )
            {
                _tracer.Trace( "prepare ExportTasks" );
                _outlookProcessor.ExportTasks();
            }

			Core.PluginLoader.RegisterResourceUIHandler( STR.MAPIFolder, _outlookUIHandler );
			Core.PluginLoader.RegisterResourceDragDropHandler( STR.MAPIFolder, _outlookUIHandler );
			Core.PluginLoader.RegisterResourceDragDropHandler( Core.ResourceTreeManager.GetRootForType( STR.MAPIFolder ).Type, new OutlookRootDragDropHandler());
                
            IUIManager uiManager = Core.UIManager;

            Core.TabManager.RegisterResourceTypeTab( "Email", "Mail",
                new string[] { STR.Email, STR.MAPIFolder }, PROP.Attachment, 1 );

            IResourceTreePane outlookFolders = 
                Core.LeftSidebar.RegisterResourceStructureTreePane( "MAPIFolders", "Email", "Outlook Folders", 
                LoadIconFromAssembly( "OutlookPlugin.Icons.folders.ico" ), STR.MAPIFolder);
            if ( outlookFolders != null )
            {
                outlookFolders.AddNodeFilter( new OutlookFoldersFilter() );
                ((ResourceTreePaneBase)outlookFolders).AddNodeDecorator( new TotalCountDecorator( STR.MAPIFolder, PROP.MAPIFolder ) );
                outlookFolders.ToolTipCallback = new ResourceToolTipCallback( GetMAPIFolderToolTip );
                Settings.OutlookFolders = outlookFolders;

                Core.LeftSidebar.RegisterViewPaneShortcut( "MAPIFolders", Keys.Control | Keys.Alt | Keys.O );
            }

            uiManager.RegisterResourceLocationLink( STR.Email, PROP.MAPIFolder, 
                STR.MAPIFolder );

            CorrespondentCtrl correspondentPane = new CorrespondentCtrl();
            correspondentPane.IniSection = "Outlook";
            Core.LeftSidebar.RegisterViewPane( "Correspondents", "Email", "Correspondents", 
                LoadIconFromAssembly( "OutlookPlugin.Icons.correspondents.ico" ), correspondentPane );
            
            Core.LeftSidebar.RegisterViewPane( "Attachments", "Email", "Attachments", 
                LoadIconFromAssembly( "OutlookPlugin.Icons.attachments.ico" ), new AttachmentsCtrl() );
            Core.LeftSidebar.RegisterViewPaneShortcut( "Attachments", Keys.Control | Keys.Alt | Keys.T );

            RegisterCustomColumns();
            RegisterOptionsPanes();

            uiManager.RegisterResourceSelectPane( STR.MAPIFolder, typeof( MAPIFoldersTreeSelectPane ) );

            IWorkspaceManager workspaceMgr = Core.WorkspaceManager;
            if ( workspaceMgr != null )
            {
                workspaceMgr.RegisterWorkspaceType( STR.MAPIFolder, 
                    new int[] { PROP.MAPIFolder }, WorkspaceResourceType.Container );
                workspaceMgr.RegisterWorkspaceType( STR.Email,
                    new int[] { -PROP.Attachment }, WorkspaceResourceType.None );
                workspaceMgr.RegisterWorkspaceSelectorFilter( STR.MAPIFolder, new OutlookFoldersFilter() );
            }

            ResourceTextProvider textProvider = new ResourceTextProvider();
            Core.PluginLoader.RegisterResourceTextProvider( STR.Email, textProvider );
            Core.PluginLoader.RegisterResourceTextProvider( STR.EmailFile, textProvider );
//            Core.PluginLoader.RegisterResourceTextProvider( null, textProvider );

            ResourceDisplayer displayer = new ResourceDisplayer();
            Core.PluginLoader.RegisterResourceDisplayer( STR.Email, displayer );
            Core.PluginLoader.RegisterResourceDisplayer( STR.EmailFile, displayer );
            Core.PluginLoader.RegisterStreamProvider( STR.Email, new StreamProvider() );

            Core.PluginLoader.RegisterViewsConstructor( new OutlookUpgrade1ViewsInitializer() );
            Core.PluginLoader.RegisterViewsConstructor( new OutlookViewsInitializer() );
            Core.PluginLoader.RegisterViewsConstructor( new OutlookUpgrade2ViewsInitializer() );

            //-----------------------------------------------------------------
            //  Register Search Extensions to narrow the list of results using
            //  simple phrases in search queries: for restricting the resource
            //  type to emails (three synonyms).
            //-----------------------------------------------------------------
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "mail", STR.Email );
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "mails", STR.Email );
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "email", STR.Email );

            Core.ExpirationRuleManager.RegisterResourceType( PROP.MAPIFolder, STR.MAPIFolder, STR.Email );

            //-----------------------------------------------------------------
            Core.PluginLoader.RegisterPluginService( this );

            Core.ResourceIconManager.RegisterPropTypeIcon( PROP.Attachment, 
                LoadIconFromAssembly( "OutlookPlugin.Icons.attachment.ico" ) );

            Core.ResourceBrowser.RegisterLinksPaneFilter( STR.Email, new OutlookLinksPaneFilter() );
            Core.ResourceBrowser.RegisterLinksPaneFilter( STR.Task, new OutlookLinksPaneFilterForTasks() );

            Core.ContactManager.RegisterContactMergeFilter( new EntryIdMergeFilter() );
            Core.ResourceBrowser.RegisterLinksPaneFilter( "Email", new ItemRecipientsFilter() );

            FolderIconProvider folderIconProvider = new FolderIconProvider();
            Core.ResourceIconManager.RegisterResourceIconProvider( STR.MAPIFolder, folderIconProvider );
            Core.ResourceIconManager.RegisterOverlayIconProvider( STR.MAPIFolder, folderIconProvider );
            Core.ResourceIconManager.RegisterOverlayIconProvider( STR.Email, new MailIconProvider() );

            if( Core.ResourceStore.GetAllResources( "SentItemsEnumSign" ).Count == 0 )
            {
                OutlookSession.OutlookProcessor.RunJob( "Detect owner e-mail", new MethodInvoker( OwnerEmailDetector.Detect ) );
            }
            ResourceDeleters.Register();

            EmailThreadingHandler threadingHandler = new EmailThreadingHandler();
            Core.PluginLoader.RegisterResourceThreadingHandler( "Email", threadingHandler );
            Core.PluginLoader.RegisterResourceThreadingHandler( PROP.Attachment, threadingHandler );
            Core.StateChanged += new EventHandler( Core_StateChanged );

            Core.ResourceBrowser.SetDefaultViewSettings( "Email", AutoPreviewMode.UnreadItems, true );

            _tracer.Trace( "End of Register" );
        }

        private static void RegisterOptionsPanes()
        {
            IUIManager uiManager = Core.UIManager;
            uiManager.RegisterOptionsGroup( "MS Outlook", "This option group contains several pages of setting that control the way [product name] works with Microsoft Outlook's e-mail client." );
            
            OptionsPaneCreator outlookPaneCreator =OutlookOptionsPane.OutlookOptionsPaneCreator;
            uiManager.RegisterOptionsPane( "MS Outlook", STR.OUTLOOK_GENERAL, outlookPaneCreator,
                                           "The Outlook General options enable you to control e-mail delivery, change e-mail addresses that [product name] recognizes as yours, control the display of embedded pictures, control the marking of messages after forwards or replies, and to create [product name] categories from Outlook mailing lists." );

            uiManager.RegisterWizardPane( STR.OUTLOOK_GENERAL, OutlookOptionsPane.OutlookOptionsPaneCreator, 3 );            

            if ( Core.ResourceStore.GetAllResources( STR.MAPIInfoStore ).Count > 1 )
            {
                uiManager.RegisterOptionsPane( "MS Outlook", "Outlook Information Stores", OutlookOptionsPane_InfoStores.CreateOptionsPane,
                    "The Outlook Information Stores options enable you to select which information stores are included in indexing." );

                uiManager.RegisterWizardPane( "Outlook Information Stores", OutlookOptionsPane_InfoStores.CreateOptionsPane, 4 );            
            }
            else
            {
                OutlookSession.OutlookProcessor.SynchronizeFolderStructure();
                OutlookSession.OutlookProcessor.SynchronizeOutlookAddressBooks();
            }
            
            OptionsPaneCreator outlookPaneCreatorIgnoredFolders = OutlookOptionsPane_IgnoredFolders.OptionsPaneCreator;
            uiManager.RegisterOptionsPane( "MS Outlook", STR.OUTLOOK_FOLDERS, outlookPaneCreatorIgnoredFolders,
                                           "The Outlook Folders options enable you specify which Outlook folders are indexed by [product name]." );
            uiManager.RegisterWizardPane( STR.OUTLOOK_FOLDERS, outlookPaneCreatorIgnoredFolders, 5 );
    
            OptionsPaneCreator outlookPaneCreatorAddressBooks = OutlookOptionsPane_AddressBooks.OptionsPaneCreator;
    
            uiManager.RegisterOptionsPane( "MS Outlook", STR.OUTLOOK_ADDRESS_BOOKS, outlookPaneCreatorAddressBooks,
                                           "The Address Books options enable you to select which Outlook address books are accessible from within [product name]." );
            uiManager.RegisterWizardPane( STR.OUTLOOK_ADDRESS_BOOKS, outlookPaneCreatorAddressBooks, 6 );

            OptionsPaneCreator outlookPaneCreatorTasks = OutlookOptionsPane_Tasks.OptionsPaneCreator;
            uiManager.RegisterOptionsPane( "MS Outlook", STR.OUTLOOK_TASKS_PANE, outlookPaneCreatorTasks,
                "The Tasks options enable you to select which Outlook task folders are accessible from within [product name]." );
            uiManager.RegisterWizardPane( STR.OUTLOOK_TASKS_PANE, outlookPaneCreatorTasks, 7 );
            
        }

        private static void RegisterCustomColumns()
        {
            ImageListColumn importanceColumn = new ImageListColumn( PROP.Importance );
            importanceColumn.AddIconValue( LoadIconFromAssembly( "OutlookPlugin.Icons.PriorityHigh.ico" ), 1 );
            importanceColumn.AddIconValue( LoadIconFromAssembly( "OutlookPlugin.Icons.PriorityLow.ico" ), -1 );
            importanceColumn.SetHeaderIcon( LoadIconFromAssembly( "OutlookPlugin.Icons.PriorityHeader.ico" ) );

            Core.DisplayColumnManager.RegisterCustomColumn( PROP.Importance, importanceColumn );

            AttachmentsColumn attachmentColumn = new AttachmentsColumn( LoadIconFromAssembly( "OutlookPlugin.Icons.AttachmentHeader.ico" ),
                                                                        LoadIconFromAssembly( "OutlookPlugin.Icons.AttachmentColumn.ico" ),
                                                                        LoadIconFromAssembly( "OutlookPlugin.Icons.AttachmentResourceColumn.ico" ) );

            Core.DisplayColumnManager.RegisterCustomColumn( -PROP.Attachment, attachmentColumn );
        }

        private static ManualResetEvent _firstIndexingEnd = new ManualResetEvent( false );
        internal static void FinishInitialIndexingJob()
        {
            _firstIndexingEnd.Set();
        }

        public void Startup()
        {
            _tracer.Trace( "Startup()" );

            ContactDragDropHandler.Register();

            if ( !_startupStatus ) return;
            Settings.LoadSettings();

            if ( Settings.DeliverOnStartup )
            {
                _outlookProcessor.RunJob( new PostMan( ) );
            }
            else
            {
                _outlookProcessor.RunJob( new EmptyJob() ); // to make sure thread has been started
            }

            _tracer.Trace( "before SynchronizeFolderStructure" );
            _outlookProcessor.SynchronizeFolderStructure();
            _outlookProcessor.SynchronizeOutlookAddressBooks();
            _tracer.Trace( "after SynchronizeFolderStructure" );
            _outlookProcessor.InitialIndexing();
            _tracer.Trace( "wait for firstIndexingEnd" );
            _outlookProcessor.RunJob( new WaitForSingleObjectJob( _firstIndexingEnd ) );
            _tracer.Trace( "firstIndexingEnd is set" );
            _outlookProcessor.StopMailIndexing();
            _tracer.Trace( "End Startup()" );
        }

        public void Shutdown()
        {
            _outlookProcessor.Dispose();
        }

        #endregion

        internal static Icon LoadIconFromAssembly( string iconName )
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( iconName );
            if( stream != null )
            {
                return new Icon( stream );
            }
            return null;
        }

        #region IEmailService Members

        public void CreateEmail(string subject, string body, EmailBodyFormat bodyFormat, IResourceList recipients, string[] attachments, 
            bool useTemplatesInBody )
        {
            if ( recipients != null && recipients.Count > 0 )
            {
                foreach ( IResource resource in recipients )
                {
                    if ( resource.Type != "Contact" && resource.Type != "ContactName" && resource.Type != STR.EmailAccount )
                    {
                        throw new ApplicationException( "CreateEmail -- wrong invokation with a recipient of inappropriate type [" + resource.Type + "]" );
                    }
                }
            }
            CreateEmailDelegate emailDelegate = new CreateEmailDelegate( OutlookFacadeHelper.CreateNewMessage );
            try
            {
                _outlookProcessor.RunJob( "Creation new mail", emailDelegate, 
                    subject, body, bodyFormat, recipients, attachments, useTemplatesInBody );
            }
            catch ( OutlookThreadTimeoutException ex )
            {
                _tracer.TraceException( ex );
                _outlookProcessor.QueueJob( JobPriority.AboveNormal, "Creation new mail", emailDelegate, 
                    subject, body, bodyFormat, recipients, attachments, useTemplatesInBody );
            }
        }

        public void CreateEmail( string subject, string body, EmailBodyFormat bodyFormat, EmailRecipient[] recipients, string[] attachments, bool addSignature )
        {
            CreateEmailWithRecipDelegate emailDelegate = new CreateEmailWithRecipDelegate( OutlookFacadeHelper.CreateNewMessage );
            try
            {
                _outlookProcessor.RunUniqueJob( "Creation new mail", emailDelegate, subject, body, bodyFormat, recipients, attachments, addSignature );
            }
            catch ( OutlookThreadTimeoutException ex )
            {
                _tracer.TraceException( ex );
                _outlookProcessor.QueueJob( JobPriority.AboveNormal, "Creation new mail", emailDelegate, subject, body, bodyFormat, recipients, attachments, addSignature );
            }
        }

        #endregion

        private class EntryIdMergeFilter: IContactMergeFilter
        {
            public string CheckMergeAllowed( IResourceList contacts )
            {
                int entryIdCount = 0;
                foreach( IResource contact in contacts )
                {
                    REGISTRY.ClearNeeded( contact );
                    if ( contact.HasProp( PROP.EntryID ) && !contact.HasProp( "UserCreated" ) )
                    {
                        ++entryIdCount;
                        if ( entryIdCount > 1 )
                        {
                            return "Please select only one contact which is synchronized with Outlook";
                        }
                    }
                }
                return null;
            }
        }

        private void Core_StateChanged( object sender, EventArgs e )
        {
            if ( Core.State == CoreState.Running )
            {
                _outlookProcessor.ThreadPriority = ThreadPriority.BelowNormal;
                Core.StateChanged -= new EventHandler( Core_StateChanged );
            }
        }
    }

    public class AttachmentComparer: IResourceComparer, IResourceGroupProvider
    {
        public int CompareResources( IResource r1, IResource r2 )
        {
            return r1.HasProp( -PROP.Attachment ).CompareTo( r2.HasProp( -PROP.Attachment ) );
        }

        public string GetGroupName( IResource res )
        {
            return res.HasProp( -PROP.Attachment ) ? "With Attachments" : "No Attachments";
        }
    }

    internal class EmailThreadingHandler: IResourceThreadingHandler
    {
        public IResource GetThreadParent( IResource res )
        {
            IResource parent = res.GetLinkProp( PROP.Attachment );
            if ( parent == null )
            {
                parent = res.GetLinkProp( Core.Props.Reply );
            }
            return parent;
        }

        public IResourceList GetThreadChildren( IResource res )
        {
            return res.GetLinksTo( null, Core.Props.Reply ).Union( res.GetLinksTo( null, PROP.Attachment ) );
        }

        public bool IsThreadChanged( IResource res, IPropertyChangeSet changeSet )
        {
            return changeSet.IsPropertyChanged( Core.Props.Reply ) || changeSet.IsPropertyChanged( PROP.Attachment );
        }

        public bool CanExpandThread( IResource res, ThreadExpandReason reason )
        {
            return res.HasProp( -Core.Props.Reply ) || res.HasProp( PROP.Attachment );
        }

        public bool HandleThreadExpand( IResource res, ThreadExpandReason reason )
        {
            return true;
        }
    }

    public class AttachmentsColumn : ICustomColumn
	{
        private int          _idAttach, _idResAttach;
        private ImageList    _imageList;

        public event ResourceEventHandler ResourceClicked;

	    public AttachmentsColumn( Icon header, Icon attach, Icon resAttach )
	    {
            _idAttach = -Core.ResourceStore.PropTypes[ STR.Attachment ].Id;
	        _idResAttach = Core.ResourceStore.PropTypes[ "InternalAttachment" ].Id;

            _imageList = new ImageList();
            _imageList.ColorDepth = ICore.Instance.ResourceIconManager.IconColorDepth;

            AddIcon( header );
            AddIcon( attach );
            AddIcon( resAttach );
	    }

        public ImageList ImageList { get { return _imageList; } }

        private int AddIcon( Icon icon )
        {
            int iconIndex = _imageList.Images.Count;
            _imageList.Images.Add( icon );
            return iconIndex;
        }

        public virtual void Draw( IResource res, Graphics g, Rectangle rc )
        {
            int x = rc.Left + (rc.Width - _imageList.ImageSize.Width) / 2;
            
            if ( res.HasProp( _idAttach ) )
            {
                _imageList.Draw( g, x, rc.Top, 1 );
            }
            else
            if ( res.HasProp( _idResAttach ) )
            {
                _imageList.Draw( g, x, rc.Top, 2 );
            }
        }

        public void DrawHeader( Graphics g, Rectangle rc )
        {
            int x = rc.Left + (rc.Width - _imageList.ImageSize.Width) / 2;
            _imageList.Draw( g, x, rc.Top, 0 );
        }

        public virtual void MouseClicked( IResource res, Point pt )
        {
            if ( ResourceClicked != null )
            {
                ResourceClicked( this, new ResourceEventArgs( res ) );
            }
        }

        public virtual string GetTooltip( IResource res )
        {
            string text = null;
            if ( res.HasProp( _idAttach ) )
            {
                text = res.GetLinksOfType( null, PROP.Attachment ).Count + " attachment(s)";
            }
            else
            if ( res.HasProp( _idResAttach ) )
            {
                text = res.GetLinksOfType( null, "InternalAttachment" ).Count + " attachment(s)";
            }

            return text;
        }

        public virtual bool ShowContextMenu( IActionContext context, Control ownerControl, Point pt )
        {
            return false;        	
        }
	}
}
