// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Web.Mail;
using System.Windows.Forms;
using CommonTests;
using EMAPILib;
using JetBrains.DataStructures;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OutlookPlugin;
using NUnit.Framework;

namespace OutlookPlugin.Tests
{
    [TestFixture, Ignore( "Investigating problems on OMNIAMEA-UNIT")]
    public class ListenersTests: MyPalDBTests
    {
        private MockPluginEnvironment _core;
        private Tracer _tracer = new Tracer( "ListenersTests" );
        private MAPIListenerStub _listener;

        public ListenersTests()
        {
            _tracer = _tracer;
            _core = _core;
        }
        [SetUp] public void SetUp()
        {
            _listener = new MAPIListenerStub( null );
            InitStorage();
            _core = new MockPluginEnvironment( _storage );
            OutlookSession.Initialize( );
        }

        [TearDown] public void TearDown()
        {
            OutlookSession.Uninitialize();
            OutlookKiller.KillFatAsses();
            CloseStorage();
        }

        [Test, Ignore( "Investigating problems on OMNIAMEA-UNIT")]
        public void TestNewMail()
        {
            TestNewMessage newMailListener = new TestNewMessage();
            _listener.SetListener( newMailListener );
            newMailListener.Test();
            newMailListener.CheckCompletion();
        }
        [Test] public void TestMovingMail()
        {
            TestMovingMessage testListener = new TestMovingMessage();
            _listener.SetListener( testListener );
            testListener.Init();
            testListener.Test1();
            testListener.CheckCompletion();
            testListener.Test2();
            testListener.CheckCompletion();
        }
        class TestNewMessage : MAPIListenerBase
        {
            private bool _complete = false;
            private string _subject;
            //private string to = "om-test@labs.intellij.net";
            private string to = "zhu@intellij.com";
            //private string to = "Sergey.Zhulin@labs.intellij.net";
            private FolderEnum _folderEnum;

            public void Test()
            {
                _folderEnum = FolderEnum.SearchForFolders( new string[]{ "RuleTest" } );
                _folderEnum = _folderEnum;

                _subject = Environment.TickCount.ToString();
                Console.WriteLine( "init subject = " + _subject );

                JetBrains.Util.MailUtil.SendEMail( "zhu@intellij.com", to, "mail.intellij.net", "RuleTest",
                    MailFormat.Text, _subject, false, new MailAttachment[0], false );
                int ticks = Environment.TickCount;
                OutlookMailDeliver.DeliverNow();
                while ( ( Environment.TickCount - ticks ) < 30000 )
                {
                    System.Threading.Thread.Sleep( 1 );
                    Application.DoEvents();
                }
                OutlookMailDeliver.DeliverNow();
            }
            public void CheckCompletion()
            {
                Tracer._Trace( "CheckCompletion" );

                int ticks = Environment.TickCount;
                while ( ( Environment.TickCount - ticks ) < 60000 )
                {
                    System.Threading.Thread.Sleep( 1 );
                    Application.DoEvents();
                }

                Assert.AreEqual( true, _complete );
                _complete = false;
            }
/*
            public override void OnNewMail( MAPINtf ntf )
            {
                Tracer._Trace( "OnNewMail" );
                Console.WriteLine( "OnNewMail" );

                IEMessage message = OutlookSession.OpenMessage( ntf.EntryID, ntf.StoreID );
                if ( message == null ) return;
                using ( message )
                {
                    string subject = message.GetStringProp( MAPIConst.PR_SUBJECT );
                    if ( subject == _subject )
                    {
                        IEFolder folder = OutlookSession.OpenFolder( ntf.ParentID, ntf.StoreID );
                        AssertNotNull( folder );
                        using( folder )
                        {
                            string entryID = OutlookSession.GetFolderID( folder );
                            Tracer._Trace( _folderEnum.GetFolderDescriptor( "RuleTest" ).FolderIDs.EntryId);
                            Tracer._Trace( entryID );
                            //AssertEquals( _folderEnum.GetFolderDescriptor( "RuleTest" ).FolderIDs.EntryId, entryID );
                        }
                        _complete = true;
                    }
                    else
                    {
                        Console.WriteLine( "Expected subject = " + _subject );
                        Console.WriteLine( "Received subject = " + subject );
                    }
                }
            }

            */
        }

        class TestMovingMessage : MAPIListenerBase
        {
            private FolderDescriptor _folderFirst;
            private FolderDescriptor _folderSecond;
            private string _storeID;
            private string _messageID;
            private string _recordKey;
            private bool _complete = false;
            private int _count = 0;

            public TestMovingMessage()
            {
            }

            public void CheckCompletion()
            {
                int ticks = Environment.TickCount;
                while ( ( Environment.TickCount - ticks ) < 2000 )
                {
                    Application.DoEvents();
                }
                Assert.AreEqual( true, Complete );
                Complete = false;
            }

            public bool Complete { get { return _complete; } set { _complete = value; } }

            public override void OnMailMove( MAPIFullNtf ntf )
            {
                Console.WriteLine( "OnMailMove: " + _count );
                Console.WriteLine( "ntf.EntryID: " + ntf.EntryID );
                Console.WriteLine( "ntf.OntryID: " + ntf.OldEntryID );
                IEMessage message = null;
                string msgId = null;
                string recordKey = null;
                switch ( _count )
                {
                    case 0:
                        message = OutlookSession.OpenMessage( ntf.EntryID, _storeID );
                        if ( message == null )
                        {
                            return;
                        }
                        Console.WriteLine( "Subject + " +  message.GetStringProp( MAPIConst.PR_SUBJECT ) );

                        Assert.IsNotNull( message );
                        using( message )
                        {
                            msgId = OutlookSession.GetMessageID( message );
                            recordKey = message.GetBinProp( MAPIConst.PR_RECORD_KEY );
                        }
                        Console.WriteLine( "Id1: " + msgId );
                        Console.WriteLine( "Id2: " + _messageID );
                        if ( msgId != _messageID )
                        {
                            if ( recordKey == _recordKey )
                            {
                                _messageID = msgId;
                            }
                            else
                            {
                                return;
                            }
                        }
                        Console.WriteLine( "PID = " +  ntf.ParentID );
                        Console.WriteLine( "OID = " +  ntf.OldParentID );
                        Console.WriteLine( "FID = " +  _folderFirst.FolderIDs.EntryId );
                        Console.WriteLine( "SID = " +  _folderSecond.FolderIDs.EntryId );
                        Assert.AreEqual( GetFolderID( ntf.ParentID ), _folderSecond.FolderIDs.EntryId );
                        Assert.AreEqual( GetFolderID( ntf.OldParentID ), _folderFirst.FolderIDs.EntryId );
                        ++_count;
                        _complete = true;
                        break;
                    case 1:
                        message = OutlookSession.OpenMessage( ntf.EntryID, _storeID );
                        if ( message == null )
                        {
                            return;
                        }
                        Console.WriteLine( message.GetStringProp( MAPIConst.PR_SUBJECT ) );

                        Assert.IsNotNull( message );
                        using( message )
                        {
                            msgId = OutlookSession.GetMessageID( message );
                            recordKey = message.GetBinProp( MAPIConst.PR_RECORD_KEY );
                        }
                        Console.WriteLine( "Id1: " + msgId );
                        Console.WriteLine( "Id2: " + _messageID );
                        if ( msgId != _messageID )
                        {
                            if ( recordKey == _recordKey )
                            {
                                _messageID = msgId;
                            }
                            else
                            {
                                return;
                            }
                        }
                        Assert.AreEqual( GetFolderID( ntf.ParentID ), _folderFirst.FolderIDs.EntryId );
                        Assert.AreEqual( GetFolderID( ntf.OldParentID ), _folderSecond.FolderIDs.EntryId );
                        ++_count;
                        _complete = true;
                        break;
                }
            }

            private string GetFolderID( string entryID )
            {
                IEFolder folder = OutlookSession.OpenFolder( entryID, _storeID );
                Assert.IsNotNull( folder );
                using ( folder )
                {
                    return OutlookSession.GetFolderID( folder );
                }
            }

            private void SearchForFolders()
            {
                FolderEnum folderEnum = FolderEnum.SearchForFolders( new string[] { "MoveFirst", "MoveSecond" } );
                _folderFirst = folderEnum.GetFolderDescriptor( "MoveFirst" );
                _folderSecond = folderEnum.GetFolderDescriptor( "MoveSecond" );
                _storeID = _folderFirst.FolderIDs.StoreId;
            }

            private void LoadMessageID()
            {
                IEFolder folderFirst =
                    OutlookSession.OpenFolder( _folderFirst.FolderIDs.EntryId, _folderFirst.FolderIDs.StoreId );
                Assert.IsNotNull( folderFirst );
                using ( folderFirst )
                {
                    IEMessages messages = folderFirst.GetMessages();
                    Assert.IsNotNull( messages );
                    using ( messages )
                    {
                        if ( messages.GetCount() == 0 )
                        {
                            Assert.Fail( "Source folder should have at least one message" );
                        }
                        if ( messages.GetCount() != 1 )
                        {
                            Assert.Fail( "Source folder should have one message" );
                        }
                        IEMessage message = messages.OpenMessage( 0 );
                        Assert.IsNotNull( message );
                        using( message )
                        {
                            _messageID = OutlookSession.GetMessageID( message );
                            _recordKey = message.GetBinProp( MAPIConst.PR_RECORD_KEY );
                        }
                    }
                }
                Assert.IsNotNull( _messageID );
            }
            private void MoveMessage( FolderDescriptor source, FolderDescriptor  destination )
            {
                IEFolder srcFolder =
                    OutlookSession.OpenFolder( source.FolderIDs.EntryId, source.FolderIDs.StoreId );
                Assert.IsNotNull( srcFolder );
                using ( srcFolder )
                {
                    IEFolder destFolder =
                        OutlookSession.OpenFolder( destination.FolderIDs.EntryId, destination.FolderIDs.StoreId );
                    Assert.IsNotNull( destFolder );
                    using ( destFolder )
                    {
                        srcFolder.MoveMessage( _messageID, destFolder );
                    }
                }
            }
            public void Init()
            {
                SearchForFolders();
                LoadMessageID();
            }

            public void Test1()
            {
                MoveMessage( _folderFirst, _folderSecond );
            }
            public void Test2()
            {
                MoveMessage( _folderSecond, _folderFirst );
            }
        }
    }
    internal class FolderEnum : IFolderDescriptorEnumeratorEvent
    {
        private HashMap _folders = new HashMap();

        #region IFolderDescriptorEnumeratorEvent Members

        public bool FolderFetched( FolderDescriptor parent, FolderDescriptor folder, out FolderDescriptor folderTag )
        {
            folderTag = folder;
            Console.WriteLine( "Folder name = " + folder.Name );
            HashMap.Entry entry = _folders.GetEntry( folder.Name );
            if ( entry != null )
            {
                entry.Value = folder;
            }
            return true;
        }
        public FolderEnum( string[] names )
        {
            foreach ( string name in names )
            {
                _folders.Add( name, null );
            }
        }
        public FolderDescriptor GetFolderDescriptor( string name )
        {
            return (FolderDescriptor)_folders[name];
        }
        public void AssertSearching()
        {
            foreach ( HashMap.Entry entry in _folders )
            {
                Assert.IsNotNull( entry.Value, (string)entry.Key );
            }
        }
        public static FolderDescriptor SearchFolder( string name )
        {
            return SearchForFolders( new string[]{name}, false ).GetFolderDescriptor( name );
        }
        public static FolderDescriptor SearchFolder( string name, bool debug )
        {
            return SearchForFolders( new string[]{name}, debug ).GetFolderDescriptor( name );
        }
        public static FolderEnum SearchForFolders( string[] names )
        {
            return SearchForFolders( names, false );
        }

        public static FolderEnum SearchForFolders( string[] names, bool debug )
        {
            FolderEnum folderEnum = new FolderEnum( names );
            if ( debug )
            {
                return folderEnum;
            }
            foreach ( IEMsgStore msgStore in OutlookSession.GetMsgStores() )
            {
                FolderDescriptorEnumerator.Do( msgStore, msgStore.GetBinProp( MAPIConst.PR_STORE_ENTRYID ), string.Empty, folderEnum );
            }
            folderEnum.AssertSearching();
            return folderEnum;
        }

        #endregion

    }
}
