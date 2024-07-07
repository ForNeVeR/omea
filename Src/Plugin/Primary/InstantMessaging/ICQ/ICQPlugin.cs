// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.InstantMessaging.ICQ.DBImport;
using JetBrains.Omea.GUIControls;
using JetBrains.DataStructures;
using System.Drawing;
using System.Reflection;

namespace JetBrains.Omea.InstantMessaging.ICQ
{
    [PluginDescriptionAttribute("ICQ IM", "JetBrains Inc.", "ICQ IM conversation viewer.\n Extracts ICQ database and converts it into searchable conversations.", PluginDescriptionFormat.PlainText, "Icons/IcqPluginIcon.png")]
    public class ICQPlugin : ReenteringEnumeratorJob, IPlugin, IResourceDisplayer, IResourceTextProvider
    {
        #region System.Object overrides

        public override bool Equals(object obj)
        {
            return obj is ICQPlugin;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region IPlugin Members

        public void Register()
        {
            _thePlugin = this;
            _folderWatchers = new HashMap();
            _dbLocations = new HashSet();
            _icqContacts = new ArrayList();
            _icqMessages = new ArrayList();
            _idResources = new IntHashSet();

            RegisterTypes();

            IUIManager uiMgr = Core.UIManager;
            uiMgr.RegisterOptionsGroup( "Instant Messaging", "The Instant Messaging options enable you to control how [product name] works with supported instant messaging programs." );
            OptionsPaneCreator icqPaneCreator = ICQOptionsPane.ICQOptionsPaneCreator;
            uiMgr.RegisterOptionsPane( "Instant Messaging", "ICQ", icqPaneCreator,
                "The ICQ options enable you to specify which ICQ accounts should be indexed, and how [product name] should build conversations from ICQ messages." );
            uiMgr.AddOptionsChangesListener( "Instant Messaging", "ICQ", ICQOptionsChanged );

            if( UINsCollection.GetUINs().Count > 0 )
            {
                uiMgr.RegisterWizardPane( "ICQ", icqPaneCreator, 10 );
                Core.TabManager.RegisterResourceTypeTab( "IM", "IM", _icqConversationResName, 2 );
                _correspondentPane = new CorrespondentCtrl();
                _correspondentPane.IniSection = "ICQ";
                _correspondentPane.SetCorresponentFilterList( Core.ResourceStore.FindResourcesWithProp( null, "ICQAcct" ) );

                Image img = Utils.TryGetEmbeddedResourceImageFromAssembly( Assembly.GetExecutingAssembly(), "ICQPlugin.Icons.Correspondents24.png" );
                Core.LeftSidebar.RegisterResourceStructurePane( "ICQCorrespondents", "IM", "ICQ Correspondents", img, _correspondentPane );
                Core.LeftSidebar.RegisterViewPaneShortcut( "ICQCorrespondents", Keys.Control | Keys.Alt | Keys.Q );
            }

            IPluginLoader loader = Core.PluginLoader;
            IActionManager actionManager = Core.ActionManager;

            loader.RegisterResourceDisplayer( _icqConversationResName, this );
            actionManager.RegisterLinkClickAction( new ConversationLinkClickAction(),_icqConversationResName, null );
            _conversationManager = new IMConversationsManager( _icqConversationResName, "ICQ Conversation", "Subject",
                                                               GetConversationTimeSpan(), _propICQAcct, _propFromICQ, _propToICQ, this );
            _conversationManager.ReverseMode = GetReverseMode();
            SaveConversationAction saveConvAction = new SaveConversationAction( _conversationManager, _propNickName );
            actionManager.RegisterContextMenuAction( saveConvAction, ActionGroups.ITEM_OPEN_ACTIONS, ListAnchor.Last,
                                                     "Save to File...", null, _icqConversationResName, null );
            actionManager.RegisterActionComponent( saveConvAction, "SaveAs", _icqConversationResName, null );
            EmailConversationAction mailConvAction = new EmailConversationAction( _conversationManager, _propNickName );
            actionManager.RegisterContextMenuAction( mailConvAction, ActionGroups.ITEM_OPEN_ACTIONS, ListAnchor.Last,
                                                    "Send by Email", null, _icqConversationResName, null );
            actionManager.RegisterActionComponent( mailConvAction, "SendByMail", _icqConversationResName, null );
            loader.RegisterResourceSerializer( _icqAccountResName, new ICQAccountSerializer() );
            loader.RegisterResourceTextProvider( _icqConversationResName, _conversationManager );
            loader.RegisterResourceTextProvider( _contactResName, this );

            Core.ResourceBrowser.RegisterLinksPaneFilter( _icqConversationResName, new ItemRecipientsFilter() );
            Core.ResourceBrowser.RegisterLinksGroup( "Accounts", new[] { _propICQAcct }, ListAnchor.First );

            //  Upgrade information about ICQ address book - set its
            //  ContentType property so that it could be filtered out when
            //  this plugin is switched off.
            IResource ab = Core.ResourceStore.FindUniqueResource( "AddessBook", "Name", "ICQ Contacts" );
            if( ab != null )
            {
                ab.SetProp( "ContentType", _icqConversationResName );
            }

            Core.ResourceBrowser.SetDefaultViewSettings( "IM", AutoPreviewMode.Off, true );
        }

        public void Startup()
        {
            IContactService contactService = (IContactService) Core.PluginLoader.GetPluginService( typeof( IContactService ) );
            if ( contactService != null )
            {
                ContactBlockCreator creator = ICQContactBlock.CreateBlock;
                contactService.RegisterContactEditBlock( 0, ListAnchor.Last, "ICQ Accounts", creator );
                contactService.RegisterContactEditBlock( ContactTabNames.GeneralTab, ListAnchor.Last, "ICQ Accounts", creator );
            }
            ISettingStore settings = Core.SettingStore;
            _indexStartDate = settings.ReadDate( "Startup", "IndexStartDate", DateTime.MinValue );
            if( _indexStartDate > DateTime.MinValue )
            {
                _idleIndexing = settings.ReadBool( "Startup", "IdleIndexing", false );
                bool needIdle = ObjectStore.ReadBool( "ICQ", "NeedIdle", true );
                if( _idleIndexing && needIdle )
                {
                    Trace.WriteLine( "Queueing conversations rebuild in idle mode" , "ICQ.Plugin" );
                    Core.ResourceAP.QueueIdleJob( this );
                }
                else
                {
                    Trace.WriteLine( "_idleIndexing = " + _idleIndexing , "ICQ.Plugin" );
                    Trace.WriteLine( "NeedIdle = " + needIdle , "ICQ.Plugin" );
                }
            }
            Core.StateChanged += Core_StateChanged;
        }

        private void Core_StateChanged( object sender, EventArgs e )
        {
            if( Core.State == CoreState.Running )
            {
                Core.ResourceAP.QueueJobAt( DateTime.Now.AddSeconds( 15 ), this );
            }
            else if( Core.State == CoreState.ShuttingDown )
            {
                Interrupted = true;
            }
        }

        public void Shutdown()
        {
            Interrupted = true;
            DisposeDBWatchers();
        }
        #endregion

        #region ReenteringEnumeratorJob Members

        public override void EnumerationStarting()
        {
            if( Core.State == CoreState.ShuttingDown )
            {
                Interrupted = true;
                return;
            }

            _conversationManager.ConversationPeriod = GetConversationTimeSpan();
            _minUpdateDate = GetMinUpdateDate();
            _maxUpdateDate = GetMaxUpdateDate();
            _startedInIdleMode = Core.IsSystemIdle;
            if( _minUpdateDate == DateTime.MaxValue || _maxUpdateDate == DateTime.MinValue )
            {
                DeleteICQConversations();
                _indexStartDate = DateTime.MinValue;
            }
            if( UINsCollection.GetUINs().Count > 0 )
            {
                _icqAB = new AddressBook( "ICQ Contacts", _icqConversationResName );
                _icqAB.IsExportable = false;
                DisposeDBWatchers();
                DoImporting();
            }
        }

        public override void EnumerationFinished()
        {
            if( !Interrupted )
            {
                Trace.WriteLine( _idResources.Count + " conversations updated" , "ICQ.Plugin" );
                // index all conversation resources
                foreach( IntHashSet.Entry E in _idResources )
                {
                    int id = E.Key;
                    IResource convs = Core.ResourceStore.LoadResource( id );
                    Core.FilterEngine.ExecRules( StandardEvents.ResourceReceived, convs );
                    Core.TextIndexManager.QueryIndexing( id );
                }
                SetUpdateDates( _minUpdateDate, _maxUpdateDate );
                // set notifiers for icq db directories
                if( GetBuildConverstionOnline() )
                {
                    foreach( HashSet.Entry E in _dbLocations )
                    {
                        if( !_folderWatchers.Contains( E.Key ) )
                        {
                            FileSystemWatcher watcher = new FileSystemWatcher();
                            watcher.Path = (string) E.Key;
                            watcher.IncludeSubdirectories = false;
                            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                            watcher.Changed += AsyncUpdateHistory;
                            watcher.Filter = "*.d*";
                            watcher.EnableRaisingEvents = true;
                            _folderWatchers.Add( E.Key, watcher );
                            Trace.WriteLine( watcher.Path , "ICQ.Plugin" );
                        }
                    }
                }
                if( _startedInIdleMode )
                {
                    ObjectStore.WriteBool( "ICQ", "NeedIdle", false );
                }
            }
            Core.UIManager.GetStatusWriter( this, StatusPane.UI ).ClearStatus();
        }

        public override AbstractJob GetNextJob()
        {
            if( _contactIndex < _icqContacts.Count )
            {
                ICQContact contact = (ICQContact) _icqContacts[ _contactIndex++ ];
                return new DelegateJob(
                    "Processing ICQ contact [" + contact.UIN + ']',
                    new ProcessICQAccountDelegate( ProcessICQAccount ), new object[] { contact } );
            }
            while( _messageIndex < _icqMessages.Count )
            {
                if( _startedInIdleMode && !Core.IsSystemIdle )
                {
                    Interrupted = true;
                    Core.ResourceAP.QueueIdleJob( this );
                    break;
                }
                ICQMessage message = (ICQMessage) _icqMessages[ _messageIndex++ ];
                DateTime msgDateTime = message.Time;
                if( msgDateTime <= DateTime.Now )
                {
                    _percentage = _messageIndex * 100 / _icqMessages.Count;
                    if( _minUpdateDate > msgDateTime )
                    {
                        _minUpdateDate = msgDateTime;
                    }
                    else if( _maxUpdateDate < msgDateTime )
                    {
                        _maxUpdateDate = msgDateTime;
                    }
                    _percentage = _messageIndex * 100 / _icqMessages.Count;
                    return new DelegateJob( "Processing ICQ message from " + message.From.UIN,
                                            new ProcessICQMessageDelegate( ProcessICQMessage ), new object[] { message } );
                }
            }
            return null;
        }

        public override string Name
        {
            get { return "Building ICQ conversations"; }
        }

        #endregion

        #region IResourceDisplayer Members

        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            if( resourceType != _icqConversationResName )
            {
                return null;
            }
        	BrowserDisplayPane pane = new BrowserDisplayPane( DisplayConversation );
            pane.DisplayResourceEnded += pane_DisplayResourceEnded;
            return pane;
        }

        private void pane_DisplayResourceEnded( object sender, EventArgs e )
        {
            if( _displayedResource != null )
            {
                _displayedResource.Dispose();
                _displayedResource = null;
            }
        }

        #endregion

        #region IResourceTextProvider Members

        public bool ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            if( res.Type == _contactResName )
            {
                IResource icqAcc = res.GetLinkProp( _propICQAcct );
                if( icqAcc != null )
                {
                    consumer.AddDocumentFragment( res.Id, icqAcc.GetPropText( _propNickName ) );
                }
            }
            return true;
        }

        #endregion

        #region implementation details

        private void RegisterTypes()
        {
            IResourceStore store = Core.ResourceStore;

            _propICQAcct = ResourceTypeHelper.UpdatePropTypeRegistration( "ICQAcct", PropDataType.Link, PropTypeFlags.ContactAccount );
            store.PropTypes.RegisterDisplayName( _propICQAcct, "ICQ UIN" );
            _propUIN = store.PropTypes.Register( "UIN", PropDataType.Int );
            _propNickName = store.PropTypes.Register( "NickName", PropDataType.String );

            IResource contactRes = store.FindUniqueResource( "ResourceType", "Name", "Contact" );
            string contactDisplayNameMask = contactRes.GetPropText( "DisplayNameMask" );
            if( contactDisplayNameMask.IndexOf( "ICQAcct" ) < 0 )
            {
                contactRes.SetProp( "DisplayNameMask", contactDisplayNameMask + " | ICQAcct" );
            }
            store.ResourceTypes.Register( _icqAccountResName, "ICQ Account", "NickName UIN",
                ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, this );
            store.ResourceTypes.Register( _icqConversationResName, "ICQ Conversation", "Subject",
                ResourceTypeFlags.Normal, this );

            _propFromICQ = ResourceTypeHelper.UpdatePropTypeRegistration( "FromICQ", PropDataType.Link, PropTypeFlags.Internal );
            _propToICQ = ResourceTypeHelper.UpdatePropTypeRegistration( "ToICQ", PropDataType.Link, PropTypeFlags.Internal );

            IDisplayColumnManager colManager = Core.DisplayColumnManager;
            colManager.RegisterDisplayColumn( _icqConversationResName, 0, new ColumnDescriptor( "From", 120 ) );
            colManager.RegisterDisplayColumn( _icqConversationResName, 1, new ColumnDescriptor( "To", 120 ) );
            colManager.RegisterDisplayColumn( _icqConversationResName, 2,
                new ColumnDescriptor( new[] { "Subject" }, 300, ColumnDescriptorFlags.AutoSize ) );
            colManager.RegisterDisplayColumn(_icqConversationResName, 3, new ColumnDescriptor( "Date", 120 ) );

            Core.PluginLoader.RegisterResourceDeleter( _icqConversationResName, new ICQConversationDeleter() );
        }

        /**
         * ICQ messages comparer
         */
        private class ICQMessageComparer : IComparer
        {
            public int Compare( object x, object y )
            {
                ICQMessage mx = (ICQMessage) x;
                ICQMessage my = (ICQMessage) y;
                if( mx.Time < my.Time )
                {
                    return -1;
                }
                if( mx.Time > my.Time )
                {
                    return 1;
                }
                return 0;
            }
        }

        private void DoImporting()
        {
            _dbLocations.Clear();
            _icqContacts.Clear();
            _icqMessages.Clear();
            _idResources.Clear();

            Importer theImporter = Importer.GetInstance();
            theImporter.Reset();

            Core.UIManager.GetStatusWriter( this, StatusPane.UI ).ShowStatus( "Importing ICQ database" );
            if( Core.ProgressWindow != null )
            {
                Core.ProgressWindow.UpdateProgress( 0, "Importing ICQ database", null );
            }

            AsyncProcessor resourceAP = (AsyncProcessor)Core.ResourceAP;

            bool needIdle = ObjectStore.ReadBool( "ICQ", "NeedIdle", true );

            foreach( IICQDatabase D in theImporter )
            {
                if( needIdle && !_startedInIdleMode && _indexStartDate > DateTime.MinValue )
                {
                    D.SkipUpdate();
                }
                bool hasData = false;
                bool indexed = false;
                int lastUIN = 0;
                foreach( object ICQObject in D )
                {
                    if( lastUIN != D.CurrentUIN )
                    {
                        lastUIN = D.CurrentUIN;
                        indexed = IndexedUIN( lastUIN );
                    }
                    if( indexed )
                    {
                        if( ICQObject is ICQContact )
                        {
                            _icqContacts.Add( ICQObject );
                        }
                        else if( ICQObject is ICQMessage )
                        {
                            ICQMessage msg = (ICQMessage) ICQObject;
                            DateTime msgDateTime = msg.Time;
                            if( ( _startedInIdleMode || _indexStartDate <= msgDateTime ) &&
                                ( _minUpdateDate > msgDateTime || _maxUpdateDate < msgDateTime ) )
                            {
                                _icqMessages.Add( msg );
                            }
                        }
                        hasData = true;
                    }
                    if( Core.State == CoreState.ShuttingDown )
                    {
                        D.SkipUpdate();
                        Interrupted = true;
                        return;
                    }
                    if( resourceAP.OutstandingJobs > 0 )
                    {
                        resourceAP.DoJobs();
                    }
                }
                if( hasData )
                {
                    _dbLocations.Add( D.CurrentLocation );
                }
            }
            // sort messages by date
            _icqMessages.Sort( new ICQMessageComparer() );
            // remove duplicates
            for( int i = 0; i < _icqMessages.Count - 1; )
            {
                ICQMessage m1 = _icqMessages[ i ] as ICQMessage;
                ICQMessage m2 = _icqMessages[ i + 1 ] as ICQMessage;
                if( m1 != null && m2 != null &&
                    m1.From == m2.From && m1.To == m2.To &&
                    m1.Time == m2.Time && m1.Body == m2.Body )
                {
                    _icqMessages.RemoveAt( i + 1 );
                }
                else
                {
                    ++i;
                }
                if( resourceAP.OutstandingJobs > 0 )
                {
                    resourceAP.DoJobs();
                }
            }
            Trace.WriteLine( _icqMessages.Count + " new messages appeared" , "ICQ.Plugin" );
            _lastPercentage = -1;
            _percentage = _contactIndex = _messageIndex = 0;
        }

        private delegate void ProcessICQMessageDelegate( ICQMessage message );

        private void ProcessICQMessage( ICQMessage message )
        {
            ICQContact From = message.From;
            ICQContact To = message.To;

            if( !From.IsIgnored() && !To.IsIgnored() )
            {
                // searching for the "From" contact
                IResource fromAccount = Core.ResourceStore.FindUniqueResource( _icqAccountResName, _propUIN, From.UIN ) ?? NewICQAccount( From );

            	// searching for the "To" contact
                IResource toAccount = Core.ResourceStore.FindUniqueResource( _icqAccountResName, _propUIN, To.UIN ) ?? NewICQAccount( To );

            	// update or create conversation and request its indexing if updated
                IResource convs = _conversationManager.Update( message.Body, message.Time, fromAccount, toAccount );
                if( convs != null )
                {
                    _idResources.Add( convs.Id );
                }
                if( _lastPercentage < _percentage )
                {
                    if( Core.ProgressWindow != null )
                    {
                        Core.ProgressWindow.UpdateProgress( _percentage, "Building ICQ conversations", null );
                    }
                    Core.UIManager.GetStatusWriter( this, StatusPane.UI ).ShowStatus(
                        "Building ICQ conversations (" + _percentage + "%)" );
                    _lastPercentage = _percentage;
                }
            }
        }

        private delegate void ProcessICQAccountDelegate( ICQContact ICQAccount );

        private void ProcessICQAccount( ICQContact contact )
        {
            IResource ICQAccRes =
                Core.ResourceStore.FindUniqueResource( _icqAccountResName, _propUIN, contact.UIN );

            if( ICQAccRes == null )
            {
                NewICQAccount( contact );
            }
            else
            {
                ICQAccRes.SetProp( _propNickName, contact.NickName );
                UpdateContact( contact );
            }
        }

        private IResource NewICQAccount( ICQContact account )
        {
            IResource newICQAccount = Core.ResourceStore.BeginNewResource( _icqAccountResName );
            try
            {
                newICQAccount.SetProp( _propUIN, account.UIN );
                newICQAccount.SetProp( _propNickName, account.NickName );
                IResource aContact = UpdateContact( account );
                aContact.AddLink( _propICQAcct, newICQAccount );
                _icqAB.AddContact( aContact );
            }
            finally
            {
                newICQAccount.EndUpdate();
            }
            return newICQAccount;
        }

        private static IResource UpdateContact( ICQContact account )
        {
            IContact contact;
            if( account.LastName.Length > 0 && account.FirstName.Length > 0 )
            {
                contact = Core.ContactManager.FindOrCreateContact( account.eMail, account.FirstName, account.LastName );
            }
            else
            {
                string senderName = ConstructICQSenderName( account );
                contact = Core.ContactManager.FindOrCreateContact( account.eMail, senderName );
//                Trace.WriteLine( "Updating account with : [" + account.eMail + "] [" + senderName + "]", "ICQ.Plugin" );
            }

            DateTime birthDate = contact.Birthday;
            if( IsDefaultICQDate( birthDate ))
            {
                contact.Birthday = account.BirthDate;
            }

            if( contact.Address == string.Empty )
            {
                contact.Address = account.Address;
            }

            if( contact.Company == string.Empty )
            {
                contact.Company = account.Company;
            }

            if( contact.HomePage == string.Empty )
            {
                contact.HomePage = account.Homepage;
            }

            return contact.Resource;
        }

        private static string ConstructICQSenderName( ICQContact contact )
        {
            string name = ( contact.FirstName + " " + contact.LastName ).Trim();
            string nick = contact.NickName.Trim();
            if( nick.Length > 0 )
            {
                name += " [" + nick + "]";
            }
            name += ' ' + contact.UIN.ToString();
            return name;
        }

        private static bool IsDefaultICQDate( DateTime dt )
        {
            return( dt == DateTime.MinValue || ( dt.Year == 1970 && dt.Month == 1 && dt.Day == 1 ) );
        }

        /**
         * event handler on changes in icq databases
         * starts async update of icq history
         */
        private static void AsyncUpdateHistory( object sender, FileSystemEventArgs e )
        {
            AsyncUpdateHistory();
        }

        internal static void AsyncUpdateHistory()
        {
            Trace.WriteLine( "AsyncUpdateHistory()" , "ICQ.Plugin" );
            Core.ResourceAP.QueueJobAt( DateTime.Now.AddSeconds( 1 ), _thePlugin );
        }

        private static void DeleteICQConversations()
        {
            try
            {
                ObjectStore.DeleteSection( "ICQDbImportTableRecordNumbers" );
                IResourceStore store = Core.ResourceStore;
                IResourceList contacts = store.FindResourcesWithProp( "Contact", _propICQAcct );
                IResourceList conversations =
                    store.GetAllResources( _icqConversationResName );
                if( conversations.Count > 0 )
                {
                    conversations.DeleteAll();
                }
                IResourceList icqAccounts =
                    store.GetAllResources( _icqAccountResName );
                if( icqAccounts.Count > 0 )
                {
                    icqAccounts.DeleteAll();
                }
                Trace.WriteLine( "All ICQConversation resources successfully deleted", "ICQ.Plugin" );
                Core.ContactManager.DeleteUnusedContacts( contacts );
                Trace.WriteLine( "All unused contacts successfully deleted", "ICQ.Plugin" );
            }
            catch( StorageException )
            {
                Trace.WriteLine( "No ICQconversation resources found", "ICQ.Plugin" );
            }
        }

        /**
         * displays conversation as auto-generated html text
         */
        private void DisplayConversation( IResource resource, AbstractWebBrowser browser, WordPtr[] wordsToHighlight )
        {
            try
            {
                browser.ShowHtml( _conversationManager.ToHtmlString( resource, _propNickName ), WebSecurityContext.Restricted, DocumentSection.RestrictResults(wordsToHighlight, DocumentSection.BodySection) );
            }
            catch( Exception e )
            {
                Trace.WriteLine( e.ToString(), "ICQ.Plugin" );
                return;
            }
            _displayedResource = resource.ToResourceListLive();
            _displayedResource.ResourceChanged += ConversationChangedHandler;

        }

        private void ConversationChangedHandler( object sender, ResourcePropIndexEventArgs e )
        {
            if( _displayedResource == sender )
            {
                Core.ResourceBrowser.RedisplaySelectedResource();
            }
        }

        /**
         * getting/setting maximum time span between msgs in a conversation
         */
        internal static TimeSpan GetConversationTimeSpan()
        {
            return new TimeSpan( ((long)
                Core.SettingStore.ReadInt( "ICQ", "ConversationPeriod", 3600 )) * 10000000 );
        }

        internal static void SetConversationTimeSpan( TimeSpan span )
        {
            Core.SettingStore.WriteInt(
                "ICQ", "ConversationPeriod", (int) ( span.Ticks / 10000000 ) );
        }

        internal static bool GetBuildConverstionOnline()
        {
            return Core.SettingStore.ReadBool( "ICQ", "BuildConverstionOnline", true );
        }

        internal static void SetBuildConverstionOnline( bool online )
        {
            Core.SettingStore.WriteBool( "ICQ", "BuildConverstionOnline", online );
        }

        internal static bool GetReverseMode()
        {
            return Core.SettingStore.ReadBool( "ICQ", "ReverseMode", false );
        }

        internal static void SetReverseMode( bool reverse )
        {
            Core.SettingStore.WriteBool( "ICQ", "ReverseMode", reverse );
        }

        /**
         * getting/setting update bound dates of ICQ conversations
         */
        internal static DateTime GetMinUpdateDate()
        {
            return ObjectStore.ReadDate( "ICQ", "MinDate", DateTime.MaxValue );
        }

        internal static DateTime GetMaxUpdateDate()
        {
            return ObjectStore.ReadDate( "ICQ", "MaxDate", DateTime.MinValue );
        }

        internal static void SetUpdateDates( DateTime minDate, DateTime maxDate )
        {
            ObjectStore.WriteDate( "ICQ", "MinDate", minDate );
            ObjectStore.WriteDate( "ICQ", "MaxDate", maxDate );
        }

        /**
         * checks whether specified UIN is to indexed
         */
        internal static bool IndexedUIN( int UIN )
        {
            string[] uins = Core.SettingStore.ReadString( "ICQ", "UINs" ).Split( ';' );
            foreach( string uin in uins )
            {
                if( UIN.ToString() == uin )
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * save list of UINs to be indexed
         */
        internal static void SaveUINs2BeIndexed( IntArrayList uins )
        {
            string strValue = string.Empty;
            for( int i = 0; i < uins.Count; ++i )
            {
                if( strValue.Length > 0 )
                {
                    strValue += ';';
                }
                strValue += uins[ i ].ToString();
            }
            Core.SettingStore.WriteString( "ICQ", "UINs", strValue );
        }

        internal static bool GetImportOnly2003b()
        {
            return Core.SettingStore.ReadBool( "ICQ", "ImportOnly2003b", false );
        }

        internal static void SetImportOnly2003b( bool import )
        {
            Core.SettingStore.WriteBool( "ICQ", "ImportOnly2003b", import );
        }

        private void DisposeDBWatchers()
        {
            foreach( HashMap.Entry E in _folderWatchers )
            {
                ( (FileSystemWatcher) E.Value ).Dispose();
            }
            _folderWatchers.Clear();
        }

        private void ICQOptionsChanged( object sender, EventArgs e )
        {
            bool reverseMode = GetReverseMode();
            if( _conversationManager.ReverseMode != reverseMode )
            {
                _conversationManager.ReverseMode = reverseMode;
                IResourceBrowser rBrowser = Core.ResourceBrowser;
                if( rBrowser != null && rBrowser.SelectedResources != null &&
                    rBrowser.SelectedResources.AllResourcesOfType( _icqConversationResName ) )
                {
                    rBrowser.RedisplaySelectedResource();
                }
            }
        }

        internal const string               _contactResName = "Contact";
        internal const string               _emailAccountResName = "EmailAccount";
        internal const string               _icqAccountResName = "ICQAccount";
        internal const string               _icqConversationResName = "ICQConversation";
        internal static int					_propFromICQ;
        internal static int					_propToICQ;
        internal static int                 _propUIN;
        internal static int                 _propNickName;
        internal static int                 _propICQAcct;
        private IMConversationsManager      _conversationManager;
        private DateTime                    _indexStartDate;
        private bool                        _idleIndexing;
        private bool                        _startedInIdleMode;
        private DateTime                    _minUpdateDate;
        private DateTime                    _maxUpdateDate;
        private HashMap                     _folderWatchers;
        private HashSet                     _dbLocations;
        private ArrayList                   _icqContacts;
        private ArrayList                   _icqMessages;
        private IntHashSet                  _idResources;
        private int                         _lastPercentage;
        private int                         _percentage;
        private int                         _contactIndex;
        private int                         _messageIndex;
        private AddressBook                 _icqAB;
        private IResourceList               _displayedResource;
        internal static CorrespondentCtrl   _correspondentPane;
        internal static ICQPlugin           _thePlugin;

        #endregion
    }
}
