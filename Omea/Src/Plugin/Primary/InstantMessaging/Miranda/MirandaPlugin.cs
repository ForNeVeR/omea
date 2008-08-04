/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.InstantMessaging.Miranda
{
    [PluginDescriptionAttribute("Miranda IM", "JetBrains Inc.", "Miranda IM conversation viewer.\n Extracts Miranda database and converts it into searchable conversations.", PluginDescriptionFormat.PlainText, "Icons/MirandaPluginIcon.png")]
    public class MirandaPlugin: IPlugin, IResourceDisplayer
    {
        private const string _MirandaOptionsDescription = "The Miranda options enable you to specify which Miranda accounts should be indexed, and how [product name] should build conversations from Miranda messages.";

        private IResourceStore _store;
        private string _profileToIndex;
        private string _dbPath;
        private FileSystemWatcher _mirandaWatcher;
        private MirandaImportJob _importJob;
        private AddressBook _mirandaAB;

        private static IMConversationsManager _convManager;
        private static CorrespondentCtrl _correspondentPane;
        private static MirandaPlugin _theInstance;

        void IPlugin.Register()
        {
            _theInstance = this;
            _store = Core.ResourceStore;

            Props.Register();
            ResourceTypes.Register( this );
            RegisterTypes();

            IDisplayColumnManager colMgr = Core.DisplayColumnManager;
            colMgr.RegisterDisplayColumn( ResourceTypes.MirandaConversation, 0, new ColumnDescriptor( "From", 100 ) );
            colMgr.RegisterDisplayColumn( ResourceTypes.MirandaConversation, 1, new ColumnDescriptor( "To", 100 ) );
            colMgr.RegisterDisplayColumn( ResourceTypes.MirandaConversation, 2, 
                new ColumnDescriptor( new[] { Core.ResourceStore.PropTypes [Core.Props.Subject].Name, "DisplayName" }, 300, ColumnDescriptorFlags.AutoSize ) );
            colMgr.RegisterDisplayColumn( ResourceTypes.MirandaConversation, 3, new ColumnDescriptor( "Date", 120 ) );

            if( !_store.PropTypes.Exist( "ConversationList" ) )
            {
                ClearConversations();
            }

            _convManager = new IMConversationsManager( ResourceTypes.MirandaConversation, "Miranda Conversation", "Subject",
                IniSettings.ConversationPeriodTimeSpan, Props.MirandaAcct, Props.FromAccount, Props.ToAccount, this );
            _convManager.ReverseMode = IniSettings.LatestOnTop;

            Core.PluginLoader.RegisterResourceTextProvider( ResourceTypes.MirandaConversation, _convManager );
            Core.PluginLoader.RegisterResourceTextProvider( "Contact", new MirandaContactTextProvider() );
            Core.PluginLoader.RegisterResourceDisplayer( ResourceTypes.MirandaConversation, this );
            Core.ActionManager.RegisterLinkClickAction( new ConversationLinkClickAction(), ResourceTypes.MirandaConversation, null );

            Core.PluginLoader.RegisterResourceSerializer( ResourceTypes.MirandaAIMAccount,
                new MirandaAccountSerializer( Props.ScreenName ) );
            Core.PluginLoader.RegisterResourceSerializer( ResourceTypes.MirandaICQAccount,
                new MirandaAccountSerializer( Props.UIN ) );

            string[] dbNames = ProfileManager.GetProfileList();
            if ( dbNames.Length > 0 )
            {
                IUIManager uiMgr = Core.UIManager;
                uiMgr.RegisterOptionsGroup( "Instant Messaging", "The Instant Messaging options enable you to control how [product name] works with supported instant messaging programs." );
                uiMgr.RegisterWizardPane( "Miranda", CreateMirandaOptions, 11 );
                uiMgr.RegisterOptionsPane( "Instant Messaging", "Miranda", CreateMirandaOptions, _MirandaOptionsDescription );
                uiMgr.AddOptionsChangesListener( "Instant Messaging", "Miranda", OnMirandaOptionsChanged );
                
                Core.TabManager.RegisterResourceTypeTab( "IM", "IM", "MirandaConversation", 2 );
                
                _correspondentPane = new CorrespondentCtrl();
                _correspondentPane.IniSection = "Miranda";
                _correspondentPane.SetCorresponentFilterList( Core.ResourceStore.FindResourcesWithProp( null, Props.MirandaAcct ) );

                Image img = Utils.TryGetEmbeddedResourceImageFromAssembly( Assembly.GetExecutingAssembly(), "OmniaMea.InstantMessaging.Miranda.Icons.correspondents.ico" );
                Core.LeftSidebar.RegisterResourceStructurePane( "MirandaCorrespondents", "IM", "Miranda Correspondents", img, _correspondentPane );
                Core.LeftSidebar.RegisterViewPaneShortcut( "MirandaCorrespondents", Keys.Control | Keys.Alt | Keys.M );

                SaveConversationAction saveConvAction = new SaveConversationAction( _convManager, Props.NickName );
                Core.ActionManager.RegisterContextMenuAction( saveConvAction, ActionGroups.ITEM_OPEN_ACTIONS, ListAnchor.Last, 
                                                              "Save to File...", null, "MirandaConversation", null );
                Core.ActionManager.RegisterActionComponent( saveConvAction, "SaveAs", "MirandaConversation", null );

                EmailConversationAction mailConvAction = new EmailConversationAction( _convManager, Props.NickName );
                Core.ActionManager.RegisterContextMenuAction( mailConvAction, ActionGroups.ITEM_OPEN_ACTIONS, ListAnchor.Last, 
                                                              "Send by Email", null, "MirandaConversation", null );
                Core.ActionManager.RegisterActionComponent( mailConvAction, "SendByMail", "MirandaConversation", null );

                Core.ResourceBrowser.RegisterLinksGroup( "Accounts", new[] { Props.MirandaAcct }, ListAnchor.First );
                Core.ResourceBrowser.RegisterLinksPaneFilter( ResourceTypes.MirandaConversation, new ItemRecipientsFilter() );

                _mirandaAB = new AddressBook( "Miranda Contacts", ResourceTypes.MirandaConversation );
                _mirandaAB.IsExportable = false;

                //  Upgrade information about Miranda address book - set its
                //  ContentType property so that it could be filtered out when
                //  this plugin is switched off.
                _mirandaAB.Resource.SetProp( Core.Props.ContentType, ResourceTypes.MirandaConversation );
                
                Core.ResourceBrowser.SetDefaultViewSettings( "IM", AutoPreviewMode.Off, true );
            }

            Core.PluginLoader.RegisterResourceDeleter( ResourceTypes.MirandaConversation, new MirandaConversationDeleter() );
        }

        private void RegisterTypes()
        {
            _store.RegisterUniqueRestriction( ResourceTypes.MirandaYahooAccount, Props.YahooId );
    
            IResource typeMirandaMessage = _store.FindUniqueResource( "ResourceType", "Name", "MirandaMessage" );
            if ( typeMirandaMessage != null )
            {
                _store.GetAllResources( "MirandaMessage" ).DeleteAll();
                ClearConversations();
            }
        }

        public void Shutdown()
        {
            DisposeMirandaWatcher();
        }

        private static AbstractOptionsPane CreateMirandaOptions()
        {
            return new MirandaOptionsPane();
        }

        internal static CorrespondentCtrl CorresponentPane
        {
            get { return _correspondentPane; }
        }

        internal static IMConversationsManager ConversationManager
        {
            get { return _convManager; }
        }

        internal static MirandaImportJob ImportJob
        {
            get { return _theInstance._importJob; }
        }

        private void ClearConversations()
        {
            _store.GetAllResources( ResourceTypes.MirandaConversation ).DeleteAll();
            ClearTimeBounds( _store.GetAllResources( ResourceTypes.MirandaAIMAccount ) );
            ClearTimeBounds( _store.GetAllResources( ResourceTypes.MirandaICQAccount ) );
            ClearTimeBounds( _store.GetAllResources( ResourceTypes.MirandaJabberAccount ) );
            ClearTimeBounds( _store.GetAllResources( ResourceTypes.MirandaYahooAccount ) );
        }

        void IPlugin.Startup()
        {
            _profileToIndex = Core.SettingStore.ReadString( "Miranda", "ProfileToIndex" );
            if ( string.IsNullOrEmpty( _profileToIndex ) )
                return;

            _dbPath = ProfileManager.GetDatabasePath( _profileToIndex );
            if ( _dbPath == null )
                return;

            IContactService contactService = (IContactService) Core.PluginLoader.GetPluginService( typeof(IContactService) );
            if ( contactService != null )
            {
                ContactBlockCreator creator = MirandaContactBlock.CreateBlock;
                contactService.RegisterContactEditBlock( 0, ListAnchor.Last, "Miranda", creator );
                contactService.RegisterContactEditBlock( ContactTabNames.GeneralTab, ListAnchor.Last, "Miranda", creator );
            }

            ImportDatabase();
            if ( IniSettings.IdleIndexing && !IniSettings.FullIndexingCompleted )
            {
                Core.ResourceAP.QueueIdleJob( new DelegateJob( new MethodInvoker( DoIdleImport ), new object[] {} ) );
            }

            if ( IniSettings.SyncImmediate ) 
            {
                CreateMirandaWatcher();
            }
        }

        /// <summary>
        /// Creates a FileSystemWatcher for monitoring the changes to the Miranda DB.
        /// </summary>
        private void CreateMirandaWatcher()
        {
            if ( _mirandaWatcher != null )
                return;

            if ( _dbPath != null && File.Exists( _dbPath ) )
            {
                _mirandaWatcher = new FileSystemWatcher( Path.GetDirectoryName( _dbPath ), 
                    Path.GetFileName( _dbPath ) );
                _mirandaWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                _mirandaWatcher.Changed += AsyncImportDatabase;
                _mirandaWatcher.EnableRaisingEvents = true;
            }
        }

        /**
         * Disposes the Miranda FileSystemWatcher.
         */

        private void DisposeMirandaWatcher()
        {
            if ( _mirandaWatcher != null )
            {
                _mirandaWatcher.Dispose();
                _mirandaWatcher = null;
            }
        }

        private void OnMirandaOptionsChanged( object sender, EventArgs e )
        {
            if ( IniSettings.SyncImmediate )
            {
                CreateMirandaWatcher();
            }
            else
            {
                DisposeMirandaWatcher();
            }

            _convManager.ReverseMode = IniSettings.LatestOnTop;
            if ( !_convManager.ConversationPeriod.Equals( IniSettings.ConversationPeriodTimeSpan ) )
            {
                _convManager.ConversationPeriod = IniSettings.ConversationPeriodTimeSpan;
                Core.ResourceAP.QueueJob( JobPriority.BelowNormal, new MethodInvoker( RebuildConversations ) );
            }
            else
            {
                IResourceBrowser rBrowser = Core.ResourceBrowser;
                if( rBrowser.SelectedResources.AllResourcesOfType( "MirandaConversation" ) )
                {
                    rBrowser.RedisplaySelectedResource();
                }
            }
        }

        private void RebuildConversations()
        {
            ClearConversations();
            _importJob.ClearEventOffsets();
            ImportDatabase();
        }

        /** 
         * event handler on changes in Miranda databases
         * starts async update of Miranda history
         */

        private void AsyncImportDatabase( object sender, FileSystemEventArgs e )
        {
            Core.ResourceAP.QueueJobAt( DateTime.Now.AddSeconds( 10 ), 
                new MethodInvoker( ImportDatabase ) );
        }

        private static void ClearTimeBounds( IResourceList accounts )
        {
            foreach( IResource account in accounts )
            {
                account.DeleteProp( Props.FirstMirandaImport );
                account.DeleteProp( Props.LastMirandaImport );
            }
        }

        /**
         * Imports the database which was configured for importing by the user.
         */

        private void ImportDatabase()
        {
            if ( _importJob == null )
            {
                _importJob = new MirandaImportJob( _dbPath, _convManager, _mirandaAB );
            }
            Core.ResourceAP.QueueJob( JobPriority.BelowNormal, _importJob );
        }

        private void DoIdleImport()
        {
            Trace.WriteLine( "Miranda Idle Import" );
            _importJob.ResetIndexStartDate();
            _importJob.ExecuteInIdle = true;
            Core.ResourceAP.QueueJob( JobPriority.Immediate, _importJob );
        }

        // -- IResourceDisplayer implementation ------------------------------

        IDisplayPane IResourceDisplayer.CreateDisplayPane( string resourceType )
        {
            if ( resourceType == ResourceTypes.MirandaConversation )
            {
                return new MirandaConversationDisplayPane( _convManager, Props.NickName );
            }
            return null;
        }

        internal delegate void StringCallback( string arg );

        internal static void HandleDatabaseOpenError( string message )
        {
            DialogResult dr = MessageBox.Show( Core.MainWindow,
                message + ". Would you like to turn off synchronization for that database?",
                Core.ProductFullName, MessageBoxButtons.YesNo );
            if ( dr == DialogResult.Yes )
            {
                Core.SettingStore.WriteString( "Miranda", "ProfileToIndex", "" );                
            }
        }

        public static void DoRebuildConversations()
        {
            if ( Core.ProgressWindow != null )
            {
                Core.ProgressWindow.UpdateProgress( 0, "Clearing old conversations...", null );
            }
            Core.ResourceAP.RunJob( new MethodInvoker( _theInstance.ClearConversations ) );
            ImportJob.ResetIndexStartDate();
            ImportJob.ExecuteInIdle = false;
            Core.ResourceAP.RunJob( ImportJob );
            while( !ImportJob.Completed )
            {
                Thread.Sleep( 50 );
            }
        }
    }

    internal class ConversationLinkClickAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            IResource conversation = context.SelectedResources [0];
            IResource fromContact = conversation.GetLinkProp( "From" );
            if ( fromContact.HasProp( "MySelf" ) )
            {
                fromContact = conversation.GetLinkProp( "To" );
            }
            
            Core.UIManager.BeginUpdateSidebar();
            Core.TabManager.CurrentTabId = "IM";
            Core.LeftSidebar.ActivateViewPane( "MirandaCorrespondents" );
            Core.UIManager.EndUpdateSidebar();
            MirandaPlugin.CorresponentPane.SelectResource( fromContact, false );
            Core.ResourceBrowser.SelectResource( context.SelectedResources [0] );
        }
    }

    internal class ResourceTypes
    {
        internal const string MirandaICQAccount    = "MirandaICQAccount";
        internal const string MirandaAIMAccount    = "MirandaAIMAccount";
        internal const string MirandaJabberAccount = "MirandaJabberAccount";
        internal const string MirandaYahooAccount  = "MirandaYahooAccount";
        internal const string MirandaConversation  = "MirandaConversation";

        internal static void Register( IPlugin ownerPlugin )
        {
            IResourceStore store = Core.ResourceStore;
            store.ResourceTypes.Register( MirandaICQAccount, "Miranda ICQ Account", "NickName UIN", 
                                          ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, ownerPlugin );
            store.ResourceTypes.Register( MirandaAIMAccount, "Miranda AIM Account", "ScreenName",
                                          ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, ownerPlugin );
            store.ResourceTypes.Register( MirandaJabberAccount, "Miranda Jabber Account", "JabberID",
                                          ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, ownerPlugin );
            store.ResourceTypes.Register( MirandaYahooAccount, "Miranda Yahoo Account", "YahooID",
                                          ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, ownerPlugin );
            store.ResourceTypes.Register( MirandaConversation, "Miranda Conversation", 
                                          Core.ResourceStore.PropTypes[ Core.Props.Subject ].Name );
        }
    }

    internal class Props
    {
        private static int _propMirandaAcct;
        private static int _propNickName;
        private static int _propScreenName;
        private static int _propJabberID;
        private static int _propYahooID;
        private static int _propUIN;
        private static int _propFirstMirandaImport;
        private static int _propLastMirandaImport;
        private static int _propFromAccount;
        private static int _propToAccount;

        internal static void Register()
        {
            IResourceStore store = Core.ResourceStore;
            IPropTypeCollection propTypes = store.PropTypes;
            
            _propUIN                = propTypes.Register( "UIN", PropDataType.Int );
            _propNickName           = propTypes.Register( "NickName", PropDataType.String );
            _propScreenName         = propTypes.Register( "ScreenName", PropDataType.String );
            _propJabberID           = propTypes.Register( "JabberID", PropDataType.String );
            _propYahooID            = propTypes.Register( "YahooID", PropDataType.String );
            _propFirstMirandaImport = propTypes.Register( "FirstMirandaImport", PropDataType.Date );
            _propLastMirandaImport  = propTypes.Register( "LastMirandaImport", PropDataType.Date );
            _propFromAccount        = propTypes.Register( "FromAccount", PropDataType.Link, PropTypeFlags.Internal );
            _propToAccount          = propTypes.Register( "ToAccount", PropDataType.Link, PropTypeFlags.Internal );

            IResource propMirandaAcct = store.FindUniqueResource( "PropType", "Name", "MirandaAcct" );
            if ( propMirandaAcct != null )
            {
                propMirandaAcct.SetProp( "Flags", (int) PropTypeFlags.ContactAccount );
                _propMirandaAcct = propMirandaAcct.GetIntProp( "ID" );
            }
            else
            {
                _propMirandaAcct = store.PropTypes.Register( "MirandaAcct", PropDataType.Link, PropTypeFlags.ContactAccount );
            }
            store.PropTypes.RegisterDisplayName( _propMirandaAcct, "Miranda Account" );
        }

        public static int MirandaAcct        { get { return _propMirandaAcct; } }
        public static int UIN                { get { return _propUIN; } }
        public static int NickName           { get { return _propNickName; } }
        public static int ScreenName         { get { return _propScreenName; } }
        public static int JabberId           { get { return _propJabberID; } }
        public static int YahooId            { get { return _propYahooID; } }
        public static int FirstMirandaImport { get { return _propFirstMirandaImport; } }
        public static int LastMirandaImport  { get { return _propLastMirandaImport; } }
        public static int FromAccount        { get { return _propFromAccount; } }
        public static int ToAccount          { get { return _propToAccount; } }
    }

    internal class MirandaContactTextProvider: IResourceTextProvider
    {
        public bool ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            foreach( IResource account in res.GetLinksOfType( null, Props.MirandaAcct ) )
            {
                foreach( int propId in new[] { Props.NickName, Props.ScreenName, Props.JabberId, Props.YahooId } )
                {
                    consumer.AddDocumentFragment( res.Id, account.GetStringProp( propId) );
                }
            }
            return true;
        }
    }

    internal class MirandaAccountSerializer: IResourceSerializer
    {
        private readonly int _keyProp;

        public MirandaAccountSerializer( int keyProp )
        {
            _keyProp = keyProp;
        }

        public void AfterSerialize( IResource parentResource, IResource res, XmlNode node )
        {
        }

        public IResource AfterDeserialize( IResource parentResource, IResource res, XmlNode node )
        {
            IResource account = Core.ResourceStore.FindUniqueResource( res.Type, _keyProp, res.GetProp( _keyProp ) );
            return account ?? res;
        }

        public SerializationMode GetSerializationMode( IResource res, string propertyType )
        {
            return SerializationMode.Default;
        }
    }

    internal class MirandaConversationDeleter: DefaultResourceDeleter
    {
        public override bool CanDeleteResource( IResource res, bool permanent )
        {
            return !permanent;
        }

        public override void DeleteResourcePermanent( IResource res )
        {
            // no-op - Miranda conversations may not be deleted permanently
        }
    }

    internal class IniSettings
    {
        internal static bool CreateCategories
        {
            get { return Core.SettingStore.ReadBool( "Miranda", "CreateCategories", true ); }
            set { Core.SettingStore.WriteBool( "Miranda", "CreateCategories", value ); }
        }

        internal static bool LatestOnTop
        {
            get { return Core.SettingStore.ReadBool( "Miranda", "LatestOnTop", false ); }
            set { Core.SettingStore.WriteBool( "Miranda", "LatestOnTop", value ); }
        }

        internal static bool SyncImmediate
        {
            get { return Core.SettingStore.ReadBool( "Miranda", "SyncImmediate", true ); }
            set { Core.SettingStore.WriteBool( "Miranda", "SyncImmediate", value ); }
        }

        internal static int ConversationPeriod
        {
            get { return Core.SettingStore.ReadInt( "Miranda", "ConversationPeriod", 3600 ); }
            set { Core.SettingStore.WriteInt( "Miranda", "ConversationPeriod", value ); }
        }

        internal static TimeSpan ConversationPeriodTimeSpan
        {
            get
            {
                int convPeriod = ConversationPeriod;
                return new TimeSpan( convPeriod / 3600, (convPeriod / 60) % 60, convPeriod % 60 );
            }
        }

        internal static bool TraceImport
        {
            get { return Core.SettingStore.ReadBool( "Miranda", "TraceImport", false ); }
        }

        internal static bool FullIndexingCompleted
        {
            get { return Core.SettingStore.ReadBool( "Miranda", "FullIndexingCompleted", false ); }
            set { Core.SettingStore.WriteBool( "Miranda", "FullIndexingCompleted", value ); }
        }

        internal static DateTime IndexStartDate
        {
            get { return Core.SettingStore.ReadDate( "Startup", "IndexStartDate", DateTime.MinValue ); }
        }

        internal static bool IdleIndexing
        {
            get { return Core.SettingStore.ReadBool( "Startup", "IdleIndexing", false ); }
        }
    }

    public class RebuildMirandaConversationsAction: IAction
    {
    	public void Execute(IActionContext context)
    	{
    		Core.UIManager.RunWithProgressWindow("Rebuilding Miranda conversations…", MirandaPlugin.DoRebuildConversations);
    	}

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = ( Core.TabManager.CurrentTabId == "IM" || Core.TabManager.CurrentTabId == "All" ) &&
                MirandaPlugin.ImportJob != null;
        }
    }

    public class MirandaAccountClickAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            Core.UIManager.BeginUpdateSidebar();
            Core.TabManager.CurrentTabId = "IM";
            Core.UIManager.EndUpdateSidebar();

            IResource account = context.SelectedResources [0];
            IResourceList resList = account.GetLinksOfType( null, Props.FromAccount );
            resList = resList.Union( account.GetLinksOfType( null, Props.ToAccount ) );

            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            options.Caption = "Messages for " + account.DisplayName;
            options.SetTransientContainer( Core.ResourceTreeManager.ResourceTreeRoot,
                StandardViewPanes.ViewsCategories );
            Core.ResourceBrowser.DisplayResourceList( null, resList, options );
        }
    }
}
