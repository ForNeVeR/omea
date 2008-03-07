/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using JetBrains.DataStructures;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.InstantMessaging.Miranda
{
	/// <summary>
	/// The class which manages the import of a single Miranda database.
	/// </summary>
	internal class MirandaImportJob: ReenteringEnumeratorJob
	{
        private IMirandaDB _db;
        private string _dbPath;
        private int _contactIndex;
        private int _lastProgressUpdate = 0;
        private long _startTicks;
        private IEnumerator _contactEnumerator;
        private IResource _selfContact;
        private Hashtable _selfAccounts = new Hashtable( new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer() );
        private IntHashTableOfInt _lastEventOffsets = new IntHashTableOfInt();     // contact offset -> last event offset
        private bool _traceImport;
        private DateTime _indexStartDate = DateTime.MinValue;
        private AddressBook _mirandaAB;
        private IMConversationsManager _conversationManager;
        private int _lastFileSize = -1;
        private int _lastSlackSpace = -1;
        private bool _dbErrorReported = false;
        private IntHashSet _updatedConversations;
        private bool _completed = false;

        public MirandaImportJob( string dbPath, IMConversationsManager convManager, AddressBook mirandaAB )
	    {
	        _dbPath = dbPath;
            _conversationManager = convManager;
            _mirandaAB = mirandaAB;
            _traceImport = IniSettings.TraceImport;
            if ( !IniSettings.FullIndexingCompleted )
            {
                _indexStartDate = IniSettings.IndexStartDate;
            }
	    }

        public override string Name
        {
            get { return "Miranda Database Import"; }
            set {  }
        }

        internal void ClearEventOffsets()
        {
            _lastEventOffsets.Clear();
        }

        internal void ResetIndexStartDate()
        {
            _indexStartDate = DateTime.MinValue;
            _lastFileSize = -1;
            ClearEventOffsets();
        }

        public override void EnumerationStarting()
	    {
            _completed = false;
            _startTicks = DateTime.Now.Ticks;

            if ( Core.State == CoreState.ShuttingDown )
            {
                Interrupted = true;
                return;
            }

            if ( !File.Exists( _dbPath ) )
            {
                Trace.WriteLine( "Failed to open Miranda DB " + _dbPath );
                Interrupted = true;
                return;
            }

            MirandaDB db;
            try
            {
                db = new MirandaDB( _dbPath );
            }
            catch( IOException )
            {
                CheckReportDBError( "Failed to open database " + _dbPath );
                return;
            }
            catch( MirandaDatabaseCorruptedException ex )
            {
                CheckReportDBError( "Database " + _dbPath + " is corrupted: " + ex.Message );
                return;
            }
            ImportDB( db );
        }

	    private void CheckReportDBError( string message )
	    {
	        Interrupted = true;
	        if ( !_dbErrorReported )
	        {
	            _dbErrorReported = true;
	            Core.UIManager.QueueUIJob( new MirandaPlugin.StringCallback( MirandaPlugin.HandleDatabaseOpenError ), message );
	        }
	    }

	    public void ImportDB( IMirandaDB db )
        {
            _updatedConversations = new IntHashSet();
            _db = db;

            if ( _db.FileSize == _lastFileSize && _db.SlackSpace == _lastSlackSpace )
            {
                TraceImport( "Skipping Miranda DB import because file size and slack space did not change" );
                _contactEnumerator = null;
                return;
            }
            _lastFileSize = _db.FileSize;
            _lastSlackSpace = db.SlackSpace;

            ImportContact( _db.UserContact, true );
            if ( _selfContact == null )
            {
                _selfContact = Core.ContactManager.MySelf.Resource;
            }

            // for AIM, we cannot create the contact resource from
            // the account, so hook the contact to the account later
            foreach( IResource acct in _selfAccounts.Values )
            {
                if ( !acct.HasLink( Props.MirandaAcct, _selfContact ) )
                    acct.AddLink( Props.MirandaAcct, _selfContact );
            }

            _contactIndex = 0;
            _contactEnumerator = _db.Contacts.GetEnumerator();
        }

        public override AbstractJob GetNextJob()
        {
            if ( Core.State == CoreState.ShuttingDown )
            {
                Interrupted = true;
                return null;
            }

            if ( _contactEnumerator == null || !_contactEnumerator.MoveNext() )
            {
                return null;
            }

            if ( Core.ProgressWindow != null )
            {
                if ( Environment.TickCount - _lastProgressUpdate > 500 && _db.ContactCount > 0 )
                {
                    Core.ProgressWindow.UpdateProgress( _contactIndex * 100 / _db.ContactCount,
                        "Importing Miranda database...", null );
                    _lastProgressUpdate = Environment.TickCount;
                }
            }
            DelegateJob job = new DelegateJob( new ImportContactDelegate( ImportContact ),
                new object[] { (IMirandaContact) _contactEnumerator.Current, false } );
            _contactIndex++;
            return job;
        }

        public override void EnumerationFinished()
	    {
            if ( _db != null )
            {
                _db.Close();
            }
            
            if ( _updatedConversations != null )
            {
                foreach( IntHashSet.Entry e in _updatedConversations )
                {
                    IResource res = Core.ResourceStore.TryLoadResource( e.Key );
                    if ( res != null )
                    {
                        Core.FilterManager.ExecRules( StandardEvents.ResourceReceived, res );
                        Core.TextIndexManager.QueryIndexing( res.Id );
                    }
                }
                _updatedConversations = null;
            }
            
            long endTicks = DateTime.Now.Ticks;
            Trace.WriteLineIf( IniSettings.TraceImport,
                "Miranda import took " + (endTicks - _startTicks) / 10000 + " ms" );

            if ( !Interrupted && _indexStartDate == DateTime.MinValue )
            {
                IniSettings.FullIndexingCompleted = true;
                ExecuteInIdle = false;
            }
            _completed = true;
	    }

	    public bool Completed
	    {
	        get { return _completed; }
	    }

	    private void TraceImport( string str )
        {
            if ( _traceImport )
            {
                Trace.WriteLine( str );
            }
        }

        private delegate void ImportContactDelegate( IMirandaContact contact, bool myself );

        private void ImportContact( IMirandaContact contact, bool myself )
        {
            if ( contact.LastEventOffset == _lastEventOffsets [contact.Offset] )
                return;
            
            // guard for job reentering (we may have restarted the import job with a new database - OM-11022)
            if ( contact.DatabaseClosed )
                return;

            try
            {
                _lastEventOffsets [contact.Offset] = contact.LastEventOffset;

                IMirandaContactSettings icqSettings = null;
                IMirandaContactSettings aimSettings = null;
                IMirandaContactSettings jabberSettings = null;
                IMirandaContactSettings yahooSettings = null;
                string group = null;

                TraceImport( "Importing contact at " + contact.Offset );
                foreach( IMirandaContactSettings settings in contact.ContactSettings )
                {
                    TraceImport( "Found settings for " + settings.ModuleName );
                    if ( String.Compare( settings.ModuleName, "ICQ", true ) == 0 )
                    {
                        icqSettings = settings;                    
                    }
                    else if ( String.Compare( settings.ModuleName, "AIM", true ) == 0 )
                    {
                        aimSettings = settings;                    
                    }
                    else if ( String.Compare( settings.ModuleName, "JABBER", true ) == 0 )
                    {
                        jabberSettings = settings;
                    }
                    else if ( String.Compare( settings.ModuleName, "YAHOO", true ) == 0 )
                    {
                        yahooSettings = settings;
                    }
                    else if ( String.Compare( settings.ModuleName, "CList", true ) == 0 )
                    {
                        group = (string) settings.Settings ["Group"];
                    }
                }
                if ( icqSettings != null )
                    ImportICQContact( contact, icqSettings, group, myself );

                if ( aimSettings != null )
                    ImportAIMContact( contact, aimSettings, group, myself );
            
                if ( jabberSettings != null )
                    ImportJabberContact( contact, jabberSettings, group, myself );

                if ( yahooSettings != null )
                    ImportYahooContact( contact, yahooSettings, group, myself );
            }
            catch( MirandaDatabaseCorruptedException ex )
            {
                CheckReportDBError( "Database " + _dbPath + " is corrupted: " + ex.Message );
            }
        }

        private void ImportICQContact( IMirandaContact contact, IMirandaContactSettings settings,
            string group, bool myself )
        {
            if ( !settings.Settings.Contains( "UIN" ) )
            {
                return;
            }

            int UIN          = (int) settings.Settings ["UIN"];
            string firstName = (string) settings.Settings ["FirstName"];
            string lastName  = (string) settings.Settings ["LastName"];
            string email     = (string) settings.Settings ["e-mail"];
            string nick      = (string) settings.Settings ["Nick"];

            if ( firstName == null )
            {
                firstName = "";
            }
            if ( lastName == null )
            {
                lastName = "";
            }

            IResource contactRes;
            IResource icqAccount = Core.ResourceStore.FindUniqueResource( ResourceTypes.MirandaICQAccount, 
                                                                          Props.UIN, UIN );
            if ( icqAccount == null || !icqAccount.HasProp( Props.MirandaAcct ) )
            {
                if ( icqAccount == null )
                {
                    icqAccount = Core.ResourceStore.BeginNewResource( ResourceTypes.MirandaICQAccount );
                    icqAccount.SetProp( Props.UIN, UIN );
                    icqAccount.SetProp( Props.NickName, nick );
                    icqAccount.EndUpdate();
                }

                if ( firstName == "" && lastName == "" )
                {
                    string contactName;
                    if ( nick != null && nick.Length > 0 )
                        contactName = nick;
                    else
                        contactName = UIN.ToString();

                    IContact ct = Core.ContactManager.FindOrCreateContact( email, contactName );
                    contactRes = ct.Resource;
                }
                else
                {
                    IContact ct = Core.ContactManager.FindOrCreateContact( email, firstName, lastName );
                    contactRes = ct.Resource;
                }

                icqAccount.AddLink( Props.MirandaAcct, contactRes );
            }
            else
            {
                contactRes = icqAccount.GetLinkProp( Props.MirandaAcct );
            }

            AssignCategory( contactRes, group );
            if ( _mirandaAB != null )
            {
                _mirandaAB.AddContact( contactRes );
            }

            else if ( !contactRes.HasLink( Props.MirandaAcct, icqAccount ) )
            {
                contactRes.AddLink( Props.MirandaAcct, icqAccount );
            }                                                  

            if ( myself )
            {
                _selfContact = contactRes;
                _selfAccounts ["ICQ"] = icqAccount;
            }
            else
                ImportContactEvents( contact, icqAccount, "ICQ" );
        }

        private void ImportAIMContact( IMirandaContact contact, IMirandaContactSettings settings,
            string group, bool myself )
        {
            string screenName = (string) settings.Settings ["SN"];
            string nickName = (string) settings.Settings ["Nick"];

            if ( screenName == null )
                return;

            ImportContactGeneric( contact, group, myself, "AIM", ResourceTypes.MirandaAIMAccount,
                new int[] { Props.ScreenName, Props.NickName }, new object[] { screenName, nickName } );
        }

        private void ImportJabberContact( IMirandaContact contact, IMirandaContactSettings settings,
            string group, bool myself )
        {
            string jabberID = (string) settings.Settings ["jid"];
            if ( jabberID == null )
            {
                jabberID = (string) settings.Settings ["LoginName"] + "@" +
                    (string) settings.Settings ["LoginServer"];
            }
            string nickName = (string) settings.Settings ["Nick"];

            ImportContactGeneric( contact, group, myself, "JABBER", ResourceTypes.MirandaJabberAccount,
                new int[] { Props.JabberId, Props.NickName }, new object[] { jabberID, nickName } );
        }

        private void ImportYahooContact( IMirandaContact contact, IMirandaContactSettings settings,
            string group, bool myself )
        {
            string yahooId = (string) settings.Settings ["yahoo_id"];
            string nickName = (string) settings.Settings ["Nick"];

            if ( yahooId == null )
                return;

            ImportContactGeneric( contact, group, myself, "YAHOO", ResourceTypes.MirandaYahooAccount,
                new int[] { Props.YahooId, Props.NickName }, new object[] { yahooId, nickName } );
        }

        private void ImportContactGeneric( IMirandaContact contact, string group, bool myself, 
            string moduleName, string accountResType, int[] propIds, object[] propValues )
        {
            IResource imAccount = Core.ResourceStore.FindUniqueResource( accountResType, 
                propIds [0], propValues [0] );
            if ( imAccount == null )
            {
                imAccount = Core.ResourceStore.BeginNewResource( accountResType );
                for( int i=0; i<propIds.Length; i++ )
                {
                    imAccount.SetProp( propIds [i], propValues [i] );
                }
                imAccount.EndUpdate();
            }

            if ( myself )
            {
                _selfAccounts [moduleName] = imAccount;
            }
            else
            {
                IResource contactRes = imAccount.GetLinkProp( Props.MirandaAcct );
                if ( contactRes == null )
                {
                    string contactName = imAccount.GetPropText( Props.NickName );
                    if ( contactName.Length == 0 )
                    {
                        contactName = imAccount.DisplayName;
                    }
                    IContact contactBO = Core.ContactManager.FindOrCreateContact( null, contactName );
                    contactRes = contactBO.Resource;
                    contactRes.AddLink( Props.MirandaAcct, imAccount );
                }
                AssignCategory( contactRes, group );
                ImportContactEvents( contact, imAccount, moduleName );
            }
        }

        private void ImportContactEvents( IMirandaContact contact, IResource accountRes, string moduleName )
        {
            if ( contact == null )
                throw new ArgumentNullException( "contact" );
            if ( accountRes == null )
                throw new ArgumentNullException( "accountRes" );
            
            TraceImport( "Importing events for " + moduleName );
            DateTime firstImportedTime = accountRes.GetDateProp( Props.FirstMirandaImport );
            DateTime lastImportedTime = accountRes.GetDateProp( Props.LastMirandaImport );
            TraceImport( "First imported time " + firstImportedTime );
            TraceImport( "Last imported time " + lastImportedTime );
            DateTime lastMessageTime = DateTime.MinValue;
            DateTime firstMessageTime = firstImportedTime;

            int eventsImported = 0, eventsSkippedIndexStart = 0, eventsSkippedImportedTime = 0;

            IResource selfAccount = (IResource) _selfAccounts [moduleName];
            if ( selfAccount == null )
            {
                Trace.WriteLine( "Could not find self account for module " + moduleName );
                return;
            }
            if ( selfAccount.GetLinkProp( Props.MirandaAcct ) == null )
            {
                throw new Exception( "Myself account is not linked to a contact" );
            }
            if ( accountRes.GetLinkProp( Props.MirandaAcct ) == null )
            {
                throw new Exception( "Account to import is not linked to a contact" );
            }

            foreach( IMirandaEvent mirandaEvent in contact.Events )
            {
                if ( mirandaEvent.ModuleName == moduleName && mirandaEvent.EventType == 0 )
                {
                    if ( mirandaEvent.Timestamp.ToLocalTime() < _indexStartDate )
                    {
                        eventsSkippedIndexStart++;
                        continue;
                    }
                    
                    if ( mirandaEvent.Timestamp < firstImportedTime || mirandaEvent.Timestamp > lastImportedTime )
                    {
                        if ( mirandaEvent.Timestamp > lastMessageTime )
                            lastMessageTime = mirandaEvent.Timestamp;
                        if ( mirandaEvent.Timestamp < firstMessageTime )
                            firstMessageTime = mirandaEvent.Timestamp;

                        IResource fromAccount, toAccount;
                        
                        if ( (mirandaEvent.Flags & 2) != 0 )  // DBEF_SENT
                        {
                            fromAccount = selfAccount;
                            toAccount   = accountRes;
                        }
                        else
                        {
                            fromAccount = accountRes;
                            toAccount   = selfAccount;
                        }

                        IResource conversation = _conversationManager.Update(
                            mirandaEvent.EventData,
                            mirandaEvent.Timestamp.ToLocalTime(),
                            fromAccount, toAccount );
                        if ( conversation != null )
                        {
                            _updatedConversations.Add( conversation.Id );
                        }
                        eventsImported++;
                    }
                    else
                        eventsSkippedImportedTime++;
                }
            }
            TraceImport( String.Format( "Imported {0} events, skipped because of index start date - {1}, skipped because already imported - {2}",
                eventsImported, eventsSkippedIndexStart, eventsSkippedImportedTime ) );
            if ( lastMessageTime > lastImportedTime )
            {
                accountRes.SetProp( Props.LastMirandaImport, lastMessageTime );
            }
            
            if ( _indexStartDate != DateTime.MinValue )
            {
                accountRes.SetProp( Props.FirstMirandaImport, _indexStartDate );
            }
            else if ( firstMessageTime < firstImportedTime )
            {
                accountRes.SetProp( Props.FirstMirandaImport, firstMessageTime );
            }
        }

        private void AssignCategory( IResource contactRes, string group )
        {
            if ( IniSettings.CreateCategories && group != null )
            {
                IResource groupCategory = Core.CategoryManager.FindOrCreateCategory( 
                    Core.CategoryManager.GetRootForTypedCategory( "Contact" ), group );

                Core.CategoryManager.AddResourceCategory( contactRes, groupCategory );
            }
        }
	}
}
