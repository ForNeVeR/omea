// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;
using Microsoft.Win32;

namespace JetBrains.Omea.OutlookPlugin
{
    public class DebugPossibility
    {
        class The10SecJob : AbstractNamedJob
        {
            public override string Name
            {
                get { return "Wait for 10 seconds"; }
            }

            /// <summary>
            /// Override this method in order to perform an one-step job or to do
            /// initialization work for a many-steps job.
            /// </summary>
            protected override void Execute()
            {
                Thread.Sleep( 10000 );
            }
        }

        public static void Queue10SecJob()
        {
            OutlookSession.OutlookProcessor.QueueJob( JobPriority.AboveNormal, new The10SecJob() );
        }

    }

    internal class MsgStores : IEnumerable
    {
        private ArrayList _msgStores;
        int _count;
        public MsgStores( ArrayList msgStores )
        {
            _msgStores = msgStores;
            _count = _msgStores.Count;
        }
        public IEMsgStore GetMsgStore( int index )
        {
            if ( index < 0 || index >= _count )
            {
                throw new ArgumentOutOfRangeException( "index" );
            }
            return (IEMsgStore)_msgStores[ index ];
        }
        public int Count { get { return _count; } }
        public IEMsgStore this[ int index ] { get { return GetMsgStore( index ); } }

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return new MsgStoresEnumerator( this );
        }

        #endregion
    }

    internal class MsgStoresEnumerator : IEnumerator
    {
        private MsgStores _msgStores;
        private int _cursor = -1;
        public MsgStoresEnumerator( MsgStores msgStores )
        {
            _msgStores = msgStores;
        }

        #region IEnumerator Members

        public void Reset()
        {
            _cursor = -1;
        }

        public object Current
        {
            get
            {
                if ( ( _cursor < 0 ) || ( _cursor == _msgStores.Count ) )
                {
                    throw new InvalidOperationException();
                }
                return _msgStores[ _cursor ];
            }
        }

        public bool MoveNext()
        {
            return ++_cursor < _msgStores.Count;
        }

        #endregion
    }

    internal class OutlookSession
    {
        private static LibManagerBase _libManager = new LibManagerBase();
        private static uint _outlookVersion = 0;
        private static OutlookProcessor _outlookProcessor;
        private static EMAPISession _eSession;
        private static PairIDs _deletedFolderIDs;
        private static HashMap _msgStores = new HashMap();
        private static IEMsgStore _defaultMsgStore = null;
        private static Tracer _tracer = new Tracer( "OutlookSession" );
        private static HashMap _exportingMail = new HashMap();
        private static IEAddrBook _addressBook;

        private OutlookSession()
        {}

        public static void SaveChanges( string trace, IEMessage message, string entryId )
        {
            SaveChanges( false, trace, message, entryId );
        }
        public static void SaveChanges( bool newCreated, string trace, IEMAPIProp message, string entryId )
        {
            _tracer.Trace( "MAPI.Message.SaveChanges" );
            _tracer.Trace( trace );
            if ( !newCreated )
            {
                BeginExportingMail( entryId );
            }
            message.SaveChanges();
        }
        public static bool IsMailExported( string entryId )
        {
            if ( String.IsNullOrEmpty( entryId ) )
            {
                return false;
            }
            lock ( _exportingMail )
            {
                HashMap.Entry entry = _exportingMail.GetEntry( entryId );
                bool bRet = entry != null;
                if ( bRet )
                {
                    DateTime date = (DateTime)entry.Value;
                    TimeSpan dif = DateTime.Now - date;
                    if ( dif.TotalMilliseconds > 10000 )
                    {
                        bRet = false;
                    }
                }
                EndExportingMail( entryId );
                if ( bRet )
                {
                    _tracer.Trace( "Ignore event for entryId = " + entryId );
                }
                return bRet;
            }
        }
        private static void BeginExportingMail( string entryId )
        {
            if ( String.IsNullOrEmpty( entryId ) )
            {
                return;
            }
            lock ( _exportingMail )
            {
                _exportingMail.Add( entryId, DateTime.Now );
            }
        }
        private static void EndExportingMail( string entryId )
        {
            Guard.EmptyStringArgument( entryId, "entryId" );
            lock ( _exportingMail )
            {
                _exportingMail.Remove( entryId );
            }
        }

        public static LibManagerBase LibManagerBase { get { return _libManager; } }
        public static string GetFolderID( IEFolder folder )
        {
            return folder.GetBinProp( MAPIConst.PR_ENTRYID );
        }
        public static string GetMessageID( IEMessage message )
        {
            return message.GetBinProp( MAPIConst.PR_ENTRYID );
        }
        public static IEMessage OpenEmailFile( IResource resource )
        {
            string strName = resource.GetStringProp( "Name" );
            if ( strName != null && strName.Length > 0 )
            {
                string path = Path.Combine( resource.GetStringProp( "Directory" ), strName );
                return EMAPISession.LoadFromMSG( path );
            }
            return null;
        }

        public static OutlookProcessor OutlookProcessor { get { return _outlookProcessor; } }
        public static void Init( OutlookProcessor outlookProcessor )
        {
            _outlookProcessor = outlookProcessor;
        }
        public static bool WereProblemWithOpeningStorage( string storeId )
        {
            return _problemWithOpening.Contains( storeId );
        }
        public static bool WereProblemWithOpeningFolder( string entryId )
        {
            return _problemWithOpening.Contains( entryId );
        }
        private static HashSet _problemWithOpening = new HashSet();
        private static void ProblemWithOpeningStorage( EMAPILib.ProblemWhenOpenStorage problem )
        {
            _problemWithOpening.Add( problem.StoreId );
        }
        public static void ProblemWithOpeningFolder( string entryId )
        {
            _problemWithOpening.Add( entryId );
        }

        public static void Initialize()
        {
            try
            {
                _outlookVersion = GetOutlookVersionFromRegistry();
            }
            catch ( Exception ex )
            {
                Core.AddExceptionReportData( "\nError getting Outlook version from registry: " + ex.Message );
                Trace.WriteLine( "Error getting Outlook version from registry: " + ex.Message );
                _outlookVersion = 0;
            }
            ReportOutlookAddins();
            ReportOutlookExtensions();
            _eSession = new EMAPISession( 0 );
            _eSession.CheckDependencies();
            try
            {
                if ( !_eSession.Initialize( IsPickLogonProfile(), _libManager ) )
                {
                    throw new Exception( "MAPI logon failed" );
                }
            }
            catch ( COMException exception )
            {
                _tracer.TraceException( exception );
                Core.ReportBackgroundException( exception );
                throw new Exception( "MAPI logon failed: " + exception.Message );
            }
            _addressBook = _eSession.OpenAddrBook();
            IEMsgStores stores = _eSession.GetMsgStores();
            if ( stores != null )
            {
                using ( stores )
                {
                    int count = stores.GetCount();
                    Trace.WriteLine( "*********************************************************");
                    Trace.WriteLine( "* " + count + " MAPI stores detected");
                    for ( int i = 0; i < count; ++i )
                    {
                        IEMsgStore store = null;
                        try
                        {
                            store = stores.GetMsgStore( i );
                        }
                        catch ( EMAPILib.ProblemWhenOpenStorage ex )
                        {
                            Trace.WriteLine( "* " + i + "th store caused problem while getting the IEMsgStore resource");
                            ProblemWithOpeningStorage( ex );
                        }
                        if ( store == null )
                        {
                            continue;
                        }

                        string storeID = stores.GetStorageID( i );
                        _msgStores.Add( storeID, store );
                        Trace.WriteLine( "* " + i + "th store has StoreID [" + storeID + "]" );

                        if ( Settings.UseOutlookListeners )
                        {
                            try
                            {
                                MAPIListenerStub mapiListener = new MAPIListenerStub( new MAPIListener( storeID ) );
                                store.Advise( mapiListener );
                            }
                            catch ( Exception exception )
                            {
                                _tracer.TraceException( exception );
                                //SetLastException( exception );
                            }
                        }

                        if ( stores.IsDefaultStore( i ) )
                        {
                            Trace.WriteLine( "* " + i + "th store is a default store" );
                            _defaultMsgStore = store;
                            string delEntryID = _defaultMsgStore.GetBinProp( MAPIConst.PR_IPM_WASTEBASKET_ENTRYID );
                            _deletedFolderIDs = new PairIDs( delEntryID, storeID );
                        }
                    }
                    Trace.WriteLine( "*********************************************************");
                }
            }
            if ( _defaultMsgStore == null )
            {
                throw new ApplicationException( "There is no default storage" );
            }
        }
        public static IEMsgStore GetDefaultMsgStore()
        {
            return _defaultMsgStore;
        }
        private static IEFolder FindTaskFolderFromResourceStore()
        {
            IResourceList taskFolders =
                Core.ResourceStore.FindResources( STR.MAPIFolder, PROP.ContainerClass, FolderType.Task );
            taskFolders.Sort( new SortSettings( ResourceProps.Id, false ) );
            foreach ( IResource resource in taskFolders.ValidResources )
            {
                PairIDs folderIDs = PairIDs.Get( resource );
                if ( folderIDs != null )
                {
                    IEFolder taskFolder = OpenFolder( folderIDs.EntryId, folderIDs.StoreId );
                    if ( taskFolder != null )
                    {
                        return taskFolder;
                    }
                }
            }
            return null;
        }
        public static IEFolder OpenDefaultTaskFolder()
        {
            IEMsgStore defaultMsgStore = GetDefaultMsgStore();
            if ( defaultMsgStore != null )
            {
                IEFolder folder = defaultMsgStore.OpenTasksFolder();
                if ( folder != null )
                {
                    return folder;
                }
            }
            return FindTaskFolderFromResourceStore();
        }

        private static bool IsPickLogonProfile()
        {
            string pickLogonProfile =
                RegUtil.GetValue( Registry.CurrentUser, @"Software\Microsoft\Exchange\Client\Options", "PickLogonProfile" ) as string;
            if ( pickLogonProfile == "1" )
            {
                return true;
            }
            return false;
        }

        public static int GetOutlookDefaultEncodingOut()
        {
            if ( _outlookVersion == 0 )
            {
                return 0;
            }
            string key = @"Software\Microsoft\Office\" + _outlookVersion + @".0\Outlook\Options\MSHTML\International";
            if ( !RegUtil.IsKeyExists( Registry.CurrentUser, key ) )
            {
                return 0;
            }
            return (int)RegUtil.GetValue( Registry.CurrentUser, key, "Default_CodePageOut", 0 );
        }

        private static void ReportOutlookAddins()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey( @"Software\Microsoft\Office\Outlook\Addins", false );
            if ( regKey == null )
            {
                return;
            }
            try
            {
                string[] subkeys;
                try
                {
                    subkeys = regKey.GetSubKeyNames();
                }
                catch
                {
                    subkeys = new string[] {};
                }
                foreach ( string subkey in subkeys )
                {
                    RegistryKey subRegKey = regKey.OpenSubKey( subkey );
                    if ( subRegKey == null )
                    {
                        continue;
                    }
                    AddExceptionReportData( "\nAddins REGKEY: " + subkey );
                    AddExceptionReportData( "\nAddins name: " + (string)subRegKey.GetValue( "FriendlyName" ) );
                    AddExceptionReportData( "\nAddins description: " + (string)subRegKey.GetValue( "Description" ) );
                }
            }
            finally
            {
                regKey.Close();
            }
        }

        private static void ReportOutlookExtensions()
        {
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey( @"Software\Microsoft\Exchange\Client\Extensions", false );
            if ( regKey == null )
            {
                return;
            }
            try
            {
                string[] values;
                try
                {
                    values = regKey.GetValueNames();
                }
                catch
                {
                    values = new string[] {};
                }
                foreach ( string value in values )
                {
                    AddExceptionReportData( "\nOutlook extension: " + value + " : " + (string)regKey.GetValue( value ) );
                }
            }
            finally
            {
                regKey.Close();
            }
        }

        private static void AddExceptionReportData( string message )
        {
            _tracer.Trace( message );
            Core.AddExceptionReportData( message );
        }

        private static uint GetOutlookVersionFromRegistry()
        {
            if ( RegUtil.IsKeyExists( Registry.ClassesRoot, @"Outlook.Application\CurVer" ) )
            {
                string outlookVersionStr = RegUtil.GetValue( Registry.ClassesRoot, @"Outlook.Application\CurVer", "" ) as string;
                if ( outlookVersionStr != null )
                {
                    AddExceptionReportData( "\nOutlook version is: " + outlookVersionStr );
                    int pos = outlookVersionStr.LastIndexOf( '.' );
                    if ( pos >= 0 )
                    {
                        return UInt32.Parse( outlookVersionStr.Substring( pos + 1 ) );
                    }
                }
            }
            return 0;
        }

        public static PairIDs GetDeletedItemsFolderIDs( string storeID )
        {
            IResource resource = Core.ResourceStore.FindUniqueResource( STR.MAPIInfoStore, PROP.EntryID, storeID );
            if ( resource != null )
            {
                string deletedItems = resource.GetStringProp( PROP.DeletedItemsEntryID );
                if ( deletedItems != null )
                {
                    return new PairIDs( deletedItems, storeID );
                }
            }
            return _deletedFolderIDs;
        }

        public static bool FolderExists( PairIDs IDs )
        {
            return FolderExists( IDs.EntryId, IDs.StoreId );
        }
        public static bool FolderExists( string entryID, string storeID )
        {
            IEFolder mapiFolder = OpenFolder( entryID, storeID );
            if ( mapiFolder == null )
            {
                return false;
            }
            using ( mapiFolder )
            {
                return true;
            }
        }
        public static bool IsDeletedItemsFolder( string entryID )
        {
            IResource resource =
                Core.ResourceStore.FindUniqueResource( STR.MAPIInfoStore, PROP.DeletedItemsEntryID, entryID );
            return ( resource != null );
        }

        public static uint Version { get { return _outlookVersion; } }

        public static MessageBody GetMessageBody( EMAPILib.IEMessage message )
        {
            string messageClass = MessageType.GetMessageClass( message );
            if ( MessageType.IsReportRead( messageClass ) )
            {
                return new MessageBody( GenerateBodyForReportRead( message ), MailBodyFormat.PlainText, 0 );
            }
            if ( MessageType.IsReportDelivered( messageClass ) )
            {
                return new MessageBody( GenerateBodyForReportDelivered( message ), MailBodyFormat.PlainText, 0 );
            }
            return message.GetRawBodyAsRTF();
        }

        private static string GenerateBodyForReportRead( IEMessage message )
        {
            string displayTo = message.GetStringProp( MAPIConst.PR_ORIGINAL_DISPLAY_TO );
            string bodyText = "Your message: \n\n" + "\tTo:\t\t\t" + displayTo + "\n";
            string subject = message.GetStringProp( MAPIConst.PR_ORIGINAL_SUBJECT );
            bodyText += "\tSubject: \t" + subject + "\n";

            DateTime submitTime = message.GetDateTimeProp( MAPIConst.PR_ORIGINAL_SUBMIT_TIME );
            if ( submitTime != DateTime.MinValue )
            {
                bodyText += "\tSent: \t\t" + submitTime.ToString() + "\n\n";
            }

            DateTime reportTime = message.GetDateTimeProp( MAPIConst.PR_REPORT_TIME );
            if ( reportTime != DateTime.MinValue )
            {
                bodyText += "was read on " + reportTime.ToString() + "\n";
            }
            return bodyText;
        }
        private static string GenerateBodyForReportDelivered( IEMessage message )
        {
            string displayTo = message.GetStringProp( MAPIConst.PR_ORIGINAL_DISPLAY_TO );
            string bodyText = "Your message: \n\n" + "\tTo:\t\t\t" + displayTo + "\n";
            string subject = message.GetStringProp( MAPIConst.PR_ORIGINAL_SUBJECT );
            bodyText += "\tSubject: \t" + subject + "\n";

            DateTime submitTime = message.GetDateTimeProp( MAPIConst.PR_ORIGINAL_SUBMIT_TIME );
            if ( submitTime != DateTime.MinValue )
            {
                bodyText += "\tSent: \t\t" + submitTime.ToString() + "\n\n";
            }

            bodyText += "was delivered to the following recipient(s):\n\n";

            string name = message.GetStringProp( MAPIConst.PR_DISPLAY_TO );
            bodyText += "\t" + name;

            DateTime deliveryTime = message.GetDateTimeProp( MAPIConst.PR_MESSAGE_DELIVERY_TIME );
            if ( deliveryTime != DateTime.MinValue )
            {
                bodyText += " on " + deliveryTime.ToString() + "\n";
            }
            return bodyText;
        }

        public static EMAPISession EMAPISession { get { return _eSession; } }
        public static IEAddrBook GetAddrBook()
        {
            return _addressBook;
        }
        private static bool Trapped( COMException exc )
        {
            if ( exc.ErrorCode == ( unchecked( (int)0x80040107 ) ) )
            {
                _tracer.Trace( "Cannot open message store with id: " );
                _tracer.Trace( "MAPI_E_INVALID_ENTRYID was returned" );
            }
            else if ( exc.ErrorCode == ( unchecked( (int)0x80040111 ) ) )
            {
                _tracer.Trace( "Cannot open message store with id: " );
                _tracer.Trace( "0x80040111 was returned" );
            }
            else if ( exc.ErrorCode == ( unchecked( (int)0x8004010F ) ) )
            {
                _tracer.Trace( "Cannot open message store with id: " );
                _tracer.Trace( "MAPI_E_NOT_FOUND was returned" );
            }
            else if ( exc.ErrorCode == ( unchecked( (int)0x8004011D ) ) )
            {
                _tracer.Trace( "Cannot open message store with id: " );
                _tracer.Trace( "MAPI_E_FAILONEPROVIDER was returned" );
                //string message = "Warning: " + GetMAPIInfoStoreName( storeId ) + " storage cannot be open";
                //StandartJobs.MessageBox( message );
            }
            else if ( exc.ErrorCode == ( unchecked( (int)0x8004011C ) ) )
            {
                _tracer.Trace( "Cannot open message store with id: " );
                _tracer.Trace( "MAPI_E_UNCONFIGURED was returned" );
                //string message = "Warning: " + GetMAPIInfoStoreName( storeId ) + " storage cannot be open because it was not configured";
                //StandartJobs.MessageBox( message );
            }
            else if ( exc.ErrorCode == ( unchecked( (int)0x8004011F ) ) )
            {
                _tracer.Trace( "Cannot open message store with id: " );
                _tracer.Trace( "MAPI_E_UNKNOWN_LCID was returned" );
                //string message = "Warning: " + GetMAPIInfoStoreName( storeId ) + " storage cannot be open";
                //StandartJobs.MessageBox( message );
            }
            else
            {
                return false;
            }
            return true;
        }
        public static MAPIIDs GetInboxIDs()
        {
            IEMsgStore msgStore = GetDefaultMsgStore();
            if ( msgStore != null )
            {
                return msgStore.GetInboxIDs();
            }
            return null;
        }
        public static IEMsgStore OpenMsgStore( string storeId )
        {
            if ( OutlookSession.OutlookProcessor != null && OutlookSession.OutlookProcessor.ShuttingDown )
            {
                return null;
            }
            HashMap.Entry entry = _msgStores.GetEntry( storeId );
            if ( entry != null )
            {
                return (IEMsgStore)entry.Value;
            }
            IEMsgStore msgStore = null;
            try
            {
                msgStore = _eSession.OpenMsgStore( storeId );
            }
            catch ( COMException exc )
            {
                if ( !Trapped( exc ) )
                {
                    throw exc;
                }
                _tracer.TraceException( exc );
            }
            finally
            {
                _msgStores[ storeId ] = msgStore;
            }
            return msgStore;
        }
        public static MsgStores GetMsgStores()
        {
            ArrayList msgStores = new ArrayList();
            foreach ( HashMap.Entry entry in _msgStores )
            {
                msgStores.Add( (IEMsgStore)entry.Value );
            }
            return new MsgStores( msgStores );
        }
        public static int ComputeFolders()
        {
            IResourceList ignoredFolders = Core.ResourceStore.FindResourcesWithProp( STR.MAPIFolder, PROP.IgnoredFolder );
            return Core.ResourceStore.GetAllResources( STR.MAPIFolder ).Minus( ignoredFolders ).Count;
        }
        public static IEFolder OpenFolder( IEFolders folders, int index )
        {
            try
            {
                return folders.OpenFolder( index );
            }
            catch ( COMException exception )
            {
                if ( exception.ErrorCode == ( unchecked( (int)0x8004060E ) ) ||
                    exception.ErrorCode == MapiError.MAPI_E_NETWORK_ERROR ||
                    exception.ErrorCode == MapiError.MAPI_E_FAILONEPROVIDER )
                    //Folder is in offline mode or Network error
                {
                    OutlookSession.ProblemWithOpeningFolder( folders.GetEntryId( index ) );
                    return null;
                }
                if ( exception.ErrorCode == MapiError.MAPI_E_EXTENDED_ERROR )
                {
                    OutlookSession.ProblemWithOpeningFolder( folders.GetEntryId( index ) );
                    return null;
                }
                throw exception;
            }
        }
        public static IEMessage OpenMessage( IEFolder folder, string entryId )
        {
            try
            {
                return folder.OpenMessage( entryId );
            }
            catch ( System.UnauthorizedAccessException exception )
            {
                _tracer.TraceException( exception );
            }
            catch ( COMException exception )
            {
                _tracer.TraceException( exception );
                if ( exception.ErrorCode != ( unchecked( (int)0x80004005 ) ) && exception.ErrorCode != ( unchecked( (int)0x80040608 ) ) )
                {
                    throw exception;
                }
            }
            return null;
        }
        public static IEMessage OpenMessage( string parentFolderId, IEFolder folder, string entryId )
        {
            try
            {
                return OpenMessage( folder, entryId );
            }
            catch ( COMException exception )
            {
                if ( exception.ErrorCode == ( unchecked( (int)0x8004060E ) ) ) //Folder is in offline mode
                {
                    OutlookSession.ProblemWithOpeningFolder( parentFolderId );
                    return null;
                }
                if ( exception.ErrorCode == ( unchecked( (int)0x80004005 ) ) || exception.ErrorCode == ( unchecked( (int)0x80040608 ) ) )
                {
                    return null;
                }
                throw exception;
            }
        }
        public static IEFolder OpenFolder( string entryId, string storeId )
        {
            IEMsgStore msgStore = OpenMsgStore( storeId );
            if ( msgStore == null )
            {
                return null;
            }
            try
            {
                return msgStore.OpenFolder( entryId );
            }
            catch ( COMException exc )
            {
                if ( !Trapped( exc ) )
                {
                    throw exc;
                }
                _tracer.TraceException( exc );
            }
            return null;
        }
        public static IEMessage OpenMessage( string entryId, string storeId )
        {
            IEMsgStore msgStore = OpenMsgStore( storeId );
            if ( msgStore == null )
            {
                return null;
            }
            try
            {
                return msgStore.OpenMessage( entryId );
            }
            catch ( COMException exc )
            {
                if ( !Trapped( exc ) )
                {
                    throw exc;
                }
                _tracer.TraceException( exc );
            }
            return null;
        }
        public static void Uninitialize()
        {
            foreach ( HashMap.Entry entry in _msgStores )
            {
                IEMsgStore msgStore = (IEMsgStore)entry.Value;
                if ( msgStore != null )
                {
                    msgStore.Unadvise();
                    msgStore.Dispose();
                }
            }
            if ( _defaultMsgStore != null )
            {
                _defaultMsgStore.Dispose();
            }
            if ( _addressBook != null )
            {
                _addressBook.Dispose();
            }
            _msgStores.Clear();
            _eSession.Uninitialize();
            //_eSession.Dispose();	// TODO:Convert: call dtor!!!
            _eSession = null;
        }

        public static bool IsOutlookRun
        {
            get
            {
                IntPtr ptr = GenericWindow.FindWindow( "rctrl_renwnd32", null );
                return ( ptr.ToInt32() != 0 );
            }
        }

        private static bool MoveMessage( string storeID, string entryID, string folderID, PairIDs delFolderIds )
        {
            if ( folderID == null )
            {
                return false;
            }
            if ( folderID != delFolderIds.EntryId )
            {
                IEFolder delFolder = OutlookSession.OpenFolder( delFolderIds.EntryId, delFolderIds.StoreId );
                if ( delFolder == null )
                {
                    return false;
                }
                using ( delFolder )
                {
                    IEFolder folder = OutlookSession.OpenFolder( folderID, storeID );
                    if ( folder == null )
                    {
                        return false;
                    }
                    using ( folder )
                    {
                        try
                        {
                            folder.MoveMessage( entryID, delFolder );
                            return true;
                        }
                        catch ( COMException exception )
                        {
                            _tracer.TraceException( exception );
                            StandartJobs.MessageBox( "Cannot complete deleting mail. Reason: " + exception.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error );
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        private static bool MoveFolder( string storeID, string entryID, string folderID, PairIDs delFolderIds )
        {
            if ( folderID == null || folderID == delFolderIds.EntryId )
            {
                return false;
            }
            IEFolder delFolder = OutlookSession.OpenFolder( delFolderIds.EntryId, delFolderIds.StoreId );
            if ( delFolder == null )
            {
                return false;
            }
            using ( delFolder )
            {
                IEFolder folder = OutlookSession.OpenFolder( folderID, storeID );
                if ( folder == null )
                {
                    return false;
                }
                using ( folder )
                {
                    try
                    {
                        folder.MoveFolder( entryID, delFolder );
                        return true;
                    }
                    catch ( COMException exception )
                    {
                        OutlookSession.OutlookProcessor.HandleException( exception );
                        return false;
                    }
                }
            }
        }
        private static void DeleteMessageImpl( string storeId, string entryId, bool DeletedItems )
        {
            Guard.EmptyStringArgument( storeId, "storeId" );
            Guard.EmptyStringArgument( entryId, "entryId" );
            IEMsgStore msgStore = OutlookSession.OpenMsgStore( storeId );
            if ( msgStore != null )
            {
                try
                {
                    msgStore.DeleteMessage( entryId, DeletedItems );
                }
                catch ( System.ArgumentException exception )
                {
                    //there is bug in parameteres for IFolder.DeleteMessages. Try to find it
                    _tracer.Trace( "There is problem with parameter while deleting message. EntryId = " + entryId );
                    _tracer.Trace( "StoreId = " + storeId );
                    _tracer.TraceException( exception );
                    Core.ReportException( exception, ExceptionReportFlags.AttachLog );
                }
                catch ( COMException exception )
                {
                    _tracer.TraceException( exception );
                    StandartJobs.MessageBox( "Cannot complete deleting mail. Reason: " + exception.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error );
                }
            }
        }
        public static bool DeleteMessage( string storeId, string entryId, bool DeletedItems )
        {
            if ( !DeletedItems )
            {
                OutlookSession.DeleteMessageImpl( storeId, entryId, false );
                return true;
            }

            PairIDs folderIds = OutlookSession.GetDeletedItemsFolderIDs( storeId );
            if ( folderIds == null )
            {
                OutlookSession.DeleteMessageImpl( storeId, entryId, false );
                return true;
            }

            string folderID = null;
            IEMessage message = OutlookSession.OpenMessage( entryId, storeId );
            if ( message == null )
            {
                return false;
            }
            using ( message )
            {
                folderID = message.GetBinProp( MAPIConst.PR_PARENT_ENTRYID );
            }
            if ( folderID != null )
            {
                if ( folderID != folderIds.EntryId )
                {
                    if ( MoveMessage( storeId, entryId, folderID, folderIds ) )
                    {
                        return true;
                    }
                }
            }
            OutlookSession.DeleteMessageImpl( storeId, entryId, false );
            return true;
        }
        public static ArrayList GetCategories( IEMAPIProp message )
        {
            int catTag = message.GetIDsFromNames( ref GUID.set4, "Keywords", PropType.PT_MV_STRING8 );
            ArrayList storedCategories = message.GetStringArray( catTag );
            if ( storedCategories != null )
            {
                return storedCategories;
            }
            return null;
        }
        public static void SetCategories( IEMAPIProp message, ArrayList categories )
        {
            int catTag = message.GetIDsFromNames( ref GUID.set4, "Keywords", PropType.PT_MV_STRING8 );
            message.SetStringArray( catTag, categories );
        }
        public static void DeleteFolder( PairIDs folderIDs, bool DeletedItems )
        {
            DeleteFolder( folderIDs, DeletedItems, null );
        }

        public static void DeleteFolderWithRename( PairIDs folderIDs, string newName )
        {
            DeleteFolder( folderIDs, true, newName );
        }
        private static void DeleteFolderImpl( PairIDs folderIDs, bool DeletedItems )
        {
            IEMsgStore msgStore = OutlookSession.OpenMsgStore( folderIDs.StoreId );
            if ( msgStore != null )
            {
                msgStore.DeleteFolder( folderIDs.EntryId, DeletedItems );
            }
        }

        public static void DeleteFolder( PairIDs folderIDs, bool DeletedItems, string newName )
        {
            if ( !DeletedItems )
            {
                DeleteFolderImpl( folderIDs, false );
                return;
            }

            PairIDs deletedItems = OutlookSession.GetDeletedItemsFolderIDs( folderIDs.StoreId );
            if ( deletedItems == null )
            {
                DeleteFolderImpl( folderIDs, false );
                return;
            }

            string folderID = null;
            IEFolder folder = OutlookSession.OpenFolder( folderIDs.EntryId, folderIDs.StoreId );
            if ( folder != null )
            {
                using ( folder )
                {
                    folderID = folder.GetFolderID();
                }
            }
            if ( folderID != null )
            {
                if ( folderID != deletedItems.EntryId )
                {
                    if ( newName != null )
                    {
                        IEFolder eFolder = OutlookSession.OpenFolder( folderIDs.EntryId, folderIDs.StoreId );
                        if ( eFolder != null )
                        {
                            using ( eFolder )
                            {
                                eFolder.SetStringProp( MAPIConst.PR_DISPLAY_NAME, newName );
                                eFolder.SaveChanges();
                            }
                        }
                    }

                    if ( MoveFolder( folderIDs.StoreId, folderIDs.EntryId, folderID, deletedItems ) )
                    {
                        return;
                    }
                }
            }
            DeleteFolderImpl( folderIDs, false );
        }

        public static void ProcessJobs()
        {
            if ( _outlookProcessor != null )
            {
                _outlookProcessor.ProcessJobs();
            }
            else
            {
                Application.DoEvents();
            }
        }

        public static IEFolders GetFolders( IEFolder folder )
        {
            return GetFolders( folder, null );
        }

        public static IEFolders GetFolders( IEFolder folder, FolderDescriptor folderDescriptor )
        {
            try
            {
                if ( folderDescriptor == null )
                {
                    folderDescriptor = FolderDescriptor.Get( folder );
                }
                return folder.GetFolders();
            }
            catch ( COMException exception )
            {
                _tracer.TraceException( exception );
                OutlookSession.ProblemWithOpeningFolder( folderDescriptor.FolderIDs.EntryId );
            }
            return null;
        }

        public static bool IsStorageSupported( IEMsgStore msgStore )
        {
            if ( Settings.SupportIMAP )
            {
                return true;
            }

            IEFolder root = msgStore.GetRootFolder();
            if ( root == null )
            {
                return true;
            }
            using ( root )
            {
                IEFolders folders = OutlookSession.GetFolders( root );
                if ( folders == null )
                {
                    return true;
                }
                using ( folders )
                {
                    int count = folders.GetCount();
                    for ( int i = 0; i < count; ++i )
                    {
                        IEFolder folder = OpenFolder( folders, i );
                        if ( folder == null )
                        {
                            continue;
                        }
                        using ( folder )
                        {
                            string containerClass = folder.GetStringProp( MAPIConst.PR_CONTAINER_CLASS );
                            if ( FolderType.IMAP == containerClass )
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        public static int ObjectsCount { get { return EMAPISession.ObjectsCount(); } }

        public static int HeapSize { get { return EMAPISession.HeapSize(); } }
    }
}
