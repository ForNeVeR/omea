// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.InstantMessaging.Trillian
{
	/// <summary>
	/// The plugin for importing Trillian logs into OmniaMea.
	/// </summary>
    [PluginDescriptionAttribute("Trillian IM", "JetBrains Inc.", "Trillian IM conversation viewer.\n Extracts Trillian database and converts it into searchable conversations.", PluginDescriptionFormat.PlainText, "Icons/TrillainPluginIcon.png")]
    public class TrillianPlugin: IPlugin, IResourceDisplayer
	{
        private ICore _environment;
        private TrillianProfileManager _profileManager;

        // Resource types used by the plugin.
        private const string _typeTrillianAccount = "TrillianAccount";
        private const string _typeTrillianConversation = "TrillianConversation";

        // IDs of the properties used by the plugin. Initialized in RegisterTypes().
        private int _propProtocol;
        private int _propIMAddress;
        private int _propNick;
        private int _propTrillianAcct;
        private int _propFromAccount;
        private int _propToAccount;
        private int _propLastImportOffset;

        // The contact describing the current user of OmniaMea.
        private IResource _myselfContact;

        // The class which manages creating IM conversations from individual messages,
        // converting conversations to HTML and so on.
        private IMConversationsManager _convManager;

        // The address book where we will collect the contacts imported from Trillian.
        private AddressBook _trillianAB;

        /**
         * Initializes the plugin, registers the resource and property types
         * and UI elements used by the plugin, returns the array of resource
         * types for which the plugin is responsible.
         */

        public void Register()
        {
            _environment = ICore.Instance;

            _profileManager = new TrillianProfileManager();
            if ( _profileManager.ProfileCount == 0 )
            {
                // Do not show the plugin if Trillian is not installed or no profiles
                // for it are defined.
            	return;
            }

            RegisterTypes();

            // Initialize the conversation manager. It will by itself register some of the
            // resource and property types used by conversations, so we need to supply
            // it with information.
            // The TimeSpan parameter specifies the maximum interval between messages for
            // which they are still considered a single conversation. We could make this
            // configurable, like the ICQ plugin does, but for simplicity we don't.

            _convManager = new IMConversationsManager( _typeTrillianConversation, "Trillian Conversation",
                "Subject", new TimeSpan( 1, 0, 0 ), _propTrillianAcct, _propFromAccount, _propToAccount,
                this);

            // The conversation manager will also take responsibility for passing the text of
            // conversations for text indexing.

            _environment.PluginLoader.RegisterResourceTextProvider( _typeTrillianConversation, _convManager );

            // Register our plugin as the displayer for TrillianConversation resources.

            _environment.PluginLoader.RegisterResourceDisplayer( _typeTrillianConversation, this );

            IUIManager uiMgr = Core.UIManager;
            uiMgr.RegisterOptionsGroup( "Instant Messaging", "The Instant Messaging options enable you to control how [product name] works with supported instant messaging programs." );
            // Registers the options pane for the plugin to be shown in the Options
            // dialog and the Startup Wizard.
            uiMgr.RegisterOptionsPane( "Instant Messaging", "Trillian", CreateTrillianOptionsPane, null );
            uiMgr.RegisterWizardPane( "Trillian", CreateTrillianOptionsPane, 11 );
        }

        /**
         * Registers the resource and property types used by the plugin.
         */

        private void RegisterTypes()
        {
            // Registers the property that will be used to store the protocol of
            // a Trillian account (ICQ, AIM, MSN and so on). Note that all properties
            // used in the display name mask of a resource type need to be registered
            // before the resource type itself.

            _propProtocol = _environment.ResourceStore.PropTypes.Register( "Protocol", PropDataType.String );

            // The property which will be used to store the protocol-specific ID of
            // the account (UIN, screen name and so on).

            _propIMAddress = _environment.ResourceStore.PropTypes.Register( "IMAddress", PropDataType.String );

            // The property for storing the account-specific nickname of the contact.

            _propNick = _environment.ResourceStore.PropTypes.Register( "Nick", PropDataType.String );

            // The properties for linking messages to accounts through which they were
            // sent or received.

            _propFromAccount = _environment.ResourceStore.PropTypes.Register( "FromAccount", PropDataType.Link, PropTypeFlags.Internal );
            _propToAccount   = _environment.ResourceStore.PropTypes.Register( "ToAccount", PropDataType.Link, PropTypeFlags.Internal );

            // The property for storing the last offset in the Trillian log which
            // was imported. This property is set on TrillianAccount instances.
            // The property is registered as Internal because we don't want it to be
            // visible in the resource browser column selector.

            _propLastImportOffset = _environment.ResourceStore.PropTypes.Register( "LastImportOffset",
                                                                                 PropDataType.Int, PropTypeFlags.Internal );

            // The property for linking the TrillianAccount resource to a contact.
            // Since we won't be looking on a TrillianAccount resource directly,
            // there is no need to register the link as directed.
            // Note that the property ID may not contain spaces, but the user-friendly
            // display name has no such restriction.
            // Also note that, for contact merging to work correctly, we need to mark
            // the link type with the ContactAccount flag.

            _propTrillianAcct = _environment.ResourceStore.PropTypes.Register( "TrillianAcct",
                                                                             PropDataType.Link, PropTypeFlags.ContactAccount );
            _environment.ResourceStore.PropTypes.RegisterDisplayName( _propTrillianAcct, "Trillian Account");

            // Registers the resource type which will be used for storing information
            // about the IM accounts of a contact. Each instance of this resource type
            // will have properties Protocol, IMAddress and Nick, and these resources will
            // be linked to Contact resources. We set the Internal flag because the
            // account does not need to appear in any views, and the NoIndex flag because
            // the account has no data that could be added to the full-text index.

            _environment.ResourceStore.ResourceTypes.Register( _typeTrillianAccount, "Protocol IMAddress",
                ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
        }

        /**
         * Creates the options pane for the Trillian plugin. The options panes are
         * created lazily, so this method is called only when the "Trillian" page
         * is actually selected in the Options dialog.
         */

        private AbstractOptionsPane CreateTrillianOptionsPane()
        {
        	TrillianOptionsPane optionsPane = new TrillianOptionsPane();
            optionsPane.ProfileManager = _profileManager;
            return optionsPane;
        }

        /**
         * Performs the startup activities of the plugin and starts any
         * background processes (if needed). Returns true if the startup
         * was successful.
         */

        public void Startup()
        {
            // Read the "profiles to index" setting. Note that the Startup Wizard
            // is invoked after Register(), so it's too early to read settings in
            // Register().

            string profilesToIndex = _environment.SettingStore.ReadString( "Trillian",
                "ProfilesToIndex" );
            ArrayList profileList = new ArrayList( profilesToIndex.Split( ';' ) );
            if ( profileList.Count == 0 )
            {
            	return;
            }

            // Create the address book for Trillian contacts. (If an address
            // book with the same name already exists, it will attach to that instance.)
            _trillianAB = new AddressBook( "Trillian Contacts" );
            _trillianAB.IsExportable = false;

            foreach( TrillianProfile profile in _profileManager.Profiles )
            {
            	if ( profileList.IndexOf( profile.Name ) >= 0 )
            	{
            		ImportProfile( profile );
            	}
            }
        }

        /**
         * Ends the work of the plugin, stops any background processes (if needed),
         * releases the resources.
         */

        public void Shutdown()
        {
        }

        #region IResourceDisplayer Members

        /**
         * This method is called to create an instance of the control which we will
         * use to display our resources. We use a standard helper class that displays
         * resources in an instance of the embedded browser and pass to it the delegate
         * that will format our resources as HTML.
         */

        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            if ( resourceType == _typeTrillianConversation )
            {
                return new BrowserDisplayPane( DisplayConversation );
            }
            return null;
        }

        /**
         * This method is called by IEBrowserDisplayPane to display the specified resource
         * in the browser.
         */

        private void DisplayConversation( IResource resource, AbstractWebBrowser browser, WordPtr[] wordsToHighlight )
        {
            // Ask ConversationManager to format the conversation as HTML and
            // feed it into IEBrowser.
            string htmlString = _convManager.ToHtmlString( resource, _propNick );
            browser.ShowHtml( htmlString, WebSecurityContext.Restricted, DocumentSection.RestrictResults(wordsToHighlight, DocumentSection.BodySection) );
        }

        #endregion

        /**
         * Imports a single Trillian profile.
         */

        private void ImportProfile( TrillianProfile profile )
        {
            _environment.ProgressWindow.UpdateProgress( 0, "Importing Trillian profile " + profile.Name + "...", null );

            ImportBuddyGroup( profile.Buddies );

            string name      = profile.ReadICQSetting( "name" );
            string nick      = profile.ReadICQSetting( "nick name" );
            string firstName = profile.ReadICQSetting( "first name" );
            string lastName  = profile.ReadICQSetting( "last name" );
            string email     = profile.ReadICQSetting( "email" );

            // The ICQ profile contains the most information that we could use to
            // find or create Myself contact, so we use it. If the ICQ profile was
            // not configured, we don't have enough data to create a useful Myself
            // contact anyway if it does not already exist. So simply use the (empty)
            // ICQ data in this case too.

            IContact mySelfContact = Core.ContactManager.FindOrCreateMySelfContact( email, firstName + " " + lastName );
            _myselfContact = mySelfContact.Resource;

            if ( name.Length > 0 )
            {
            	IResource icqAccount = FindTrillianAccount( "ICQ", name );
                if ( icqAccount == null )
                {
                	icqAccount = CreateTrillianAccount( "ICQ", name, nick );
                    _myselfContact.AddLink( _propTrillianAcct, icqAccount );
                }

                TrillianLog[] logs = profile.GetLogs( "ICQ" );
                foreach( TrillianLog log in logs )
                {
                    ImportLog( "ICQ", log, icqAccount );
                }
            }




            /*
            foreach( string protocol in new string[] { "ICQ", "AIM", "IRC", "MSN", "Yahoo" } )
        	{
        		TrillianLog[] logs = profile.GetLogs( protocol );
                foreach( TrillianLog log in logs )
                {
                	ImportLog( protocol, log );
                }
        	}
            */
        }

        /**
         * Imports a single buddy group.
         */

        private void ImportBuddyGroup( TrillianBuddyGroup group )
        {
        	foreach( TrillianBuddy buddy in group.Buddies )
        	{
        		IResource account = FindOrCreateTrillianAccount( buddy.Protocol, buddy.Address, buddy.Nick );
                IResource contact = account.GetLinkProp( _propTrillianAcct );

                _trillianAB.AddContact( contact );
        	}
            foreach( TrillianBuddyGroup childGroup in group.Groups )
        	{
                ImportBuddyGroup( childGroup );
        	}
        }

        /**
         * Finds an existing Trillian account with the specified protocol and UIN,
         * or creates a new one.
         */

        private IResource FindOrCreateTrillianAccount( string protocol, string uin, string nick )
        {
            // Check if we already have an account for that buddy.
            IResource existingAccount = FindTrillianAccount( protocol, uin );
            if ( existingAccount != null )
                return existingAccount;

            IResource account = CreateTrillianAccount( protocol, uin, nick );

            // Now link the account to a contact. Since Trillian, unlike ICQ or Miranda,
            // doesn't store any information about contacts in the contact list, we could
            // identify the contact only by nickname. This means that we'll create bogus
            // contacts most of the time, and the user will need to use the contact merging
            // feature to link the Trillian conversations to the correct contact.

            IContact contact = Core.ContactManager.FindOrCreateContact( null, nick ?? uin );
            contact.Resource.AddLink( _propTrillianAcct, account );
            return account;
        }

        /**
         * Finds an existing Trillian account with the specified protocol and UIN.
         */

        private IResource FindTrillianAccount( string protocol, string uin )
        {
            // The FindResources() method allows to specify only one search condition,
            // so we need to get the buddies with the same UIN and manually loop through
            // them to see if they have the right protocol.

            IResourceList resList = _environment.ResourceStore.FindResources( _typeTrillianAccount,
                _propIMAddress, uin );

            foreach( IResource res in resList )
            {
                if ( res.GetStringProp( _propProtocol ) == protocol )
                {
                    return res;
                }
            }
            return null;
        }

        /**
         * Creates a Trillian account with the specified parameters.
         */

        private IResource CreateTrillianAccount( string protocol, string uin, string nick )
        {
            // Create the Trillian account from the specified data. The import
            // is called from the Startup() method, which is running in the resource
            // thread, so we can work with the resource store directly and need not use
            // ResourceProxy. However, using BeginNewResource() is a good idea in any case.

            IResource account = _environment.ResourceStore.BeginNewResource( _typeTrillianAccount );
            account.SetProp( _propProtocol, protocol );
            account.SetProp( _propIMAddress, uin );
            if ( nick != null )
            {
                account.SetProp( _propNick, nick );
            }
            account.EndUpdate();
            return account;
        }

        /**
         * Imports a single Trillian log.
         */

        private void ImportLog( string protocol, TrillianLog log, IResource selfAccount )
        {
            // For ICQ, the first session does not contain the nickname of the correspondent.
            // Thus, to avoid creating number-only contacts, we buffer messages until we
            // get a real nick, then create the contact and account, process the messages from
            // the buffer and continue parsing normally.
            ArrayList messageBuffer = new ArrayList();

            IResource correspondentAcct = FindTrillianAccount( protocol, log.GetName() );
            if ( correspondentAcct != null )
            {
                // GetIntProp() returns 0 if the property was not defined
                int lastImportOffset = correspondentAcct.GetIntProp( _propLastImportOffset );

                // If the log size is less than the last import offset, it means that the
                // log was truncated. Redo import from start.
                if ( lastImportOffset > log.Size )
                {
                	lastImportOffset = 0;
                }
                if ( lastImportOffset == log.Size )
                {
                	// the log is already completely imported
                    return;
                }
                log.Seek( lastImportOffset );
            }

            while( true )
            {
            	TrillianLogMessage msg = log.ReadNextMessage();
                if ( msg == null )
                    break;

                if ( correspondentAcct == null )
                {
                    int foo;
                	if ( protocol == "ICQ" && Int32.TryParse( log.CurCorrespondentName, out foo ) )
                	{
                        messageBuffer.Add( msg );
                        continue;
                	}
                    correspondentAcct = FindOrCreateTrillianAccount( protocol, log.GetName(),
                        log.CurCorrespondentName );

                    ImportFromBuffer( messageBuffer, selfAccount, correspondentAcct );
                    messageBuffer.Clear();
                }

                IResource fromAccount = msg.Incoming ? correspondentAcct : selfAccount;
                IResource toAccount   = msg.Incoming ? selfAccount : correspondentAcct;

                IResource conversation = _convManager.Update( msg.Text, msg.Time, fromAccount, toAccount );

                // Request text indexing of the conversation. Repeated indexing requests
                // will be merged, so there is no harm in requesting to index the same
                // conversation multiple times.
                if ( conversation != null )
                {
                    _environment.TextIndexManager.QueryIndexing( conversation.Id );
                }
            }

            if ( correspondentAcct == null && messageBuffer.Count > 0 )
            {
                // no nickname until end of log => process messages accumulated in the buffer
                correspondentAcct = FindOrCreateTrillianAccount( protocol, log.GetName(), null );
                ImportFromBuffer( messageBuffer, selfAccount, correspondentAcct );
            }
            correspondentAcct.SetProp( _propLastImportOffset, log.Size );
        }

        /**
         * Imports log messages from the specified buffer.
         */

        private void ImportFromBuffer( ArrayList buffer, IResource selfAccount, IResource correspondentAcct )
        {
            foreach( TrillianLogMessage msg in buffer )
            {
                IResource fromAccount = msg.Incoming ? correspondentAcct : selfAccount;
                IResource toAccount   = msg.Incoming ? selfAccount : correspondentAcct;

                IResource conversation = _convManager.Update( msg.Text, msg.Time, fromAccount, toAccount );
                if ( conversation != null )
                {
                    _environment.TextIndexManager.QueryIndexing( conversation.Id );
                }
            }
        }
    }
}
