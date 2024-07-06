// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using CommonTests;
using EMAPILib;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.OutlookPlugin;
using NUnit.Framework;

namespace OutlookPlugin.Tests
{
    public class OutlookTests : MyPalDBTests
    {
        private MockPluginEnvironment _core;
        [SetUp]
        public void SetUp()
        {
            SetUpOutlook();
        }

        protected void SetUpOutlook()
        {
            InitStorage();
            _core = new MockPluginEnvironment( _storage );
            _core.RegisterComponentImplementation( typeof(FileResourceManager) );
            OutlookSession.Initialize( );
        }

        [TearDown]
        public void TearDown()
        {
            TearDownOutlook();
        }

        protected void TearDownOutlook()
        {
            OutlookSession.Uninitialize();
            OutlookKiller.KillFatAsses();
            CloseStorage();
        }
    }

    [TestFixture]//, Ignore( "Investigating problems on OMNIAMEA-UNIT")]
    public class PlainFormatTest: OutlookTests
    {
        [Test]//, Ignore( "Investigating problems on OMNIAMEA-UNIT")]
        public void Test()
        {
            FolderDescriptor folderDescriptor = FolderEnum.SearchFolder( "Format" );
            Assert.IsNotNull( folderDescriptor );
            IEFolder folder =
                OutlookSession.OpenFolder( folderDescriptor.FolderIDs.EntryId, folderDescriptor.FolderIDs.StoreId );
            Assert.IsNotNull( folder );
            using ( folder )
            {
                IEMessages messages = folder.GetMessages();
                Assert.IsNotNull( messages );
                using ( messages )
                {

                    Assert.AreEqual( 1, messages.GetCount() );
                    IEMessage mail = messages.OpenMessage( 0 );
                    Assert.IsNotNull( mail );
                    using ( mail )
                    {
                        MessageBody msgBody = mail.GetRawBodyAsRTF();
                        Assert.AreEqual( MailBodyFormat.PlainTextInRTF, msgBody.Format );
                    }
                }
            }
        }
    }

    [TestFixture, Ignore( "Investigating problems on OMNIAMEA-UNIT")]
    public class GetSetPropertiesTests: OutlookTests
	{
        [Test]//, Ignore( "Investigating problems on OMNIAMEA-UNIT")]
        public void GetSetCategories()
        {
            FolderEnum folderEnum = FolderEnum.SearchForFolders( new string[]{ "TasksTest" } );
            FolderDescriptor folderDescriptor = folderEnum.GetFolderDescriptor( "TasksTest" );
            Assert.IsNotNull( folderDescriptor );
            IEFolder folder =
                OutlookSession.OpenFolder( folderDescriptor.FolderIDs.EntryId, folderDescriptor.FolderIDs.StoreId );
            Assert.IsNotNull( folder );
            using ( folder )
            {
                IEMessages messages = folder.GetMessages();
                Assert.IsNotNull( messages );
                using ( messages )
                {

                    Assert.AreEqual( 1, messages.GetCount() );
                    IEMessage task = messages.OpenMessage( 0 );
                    Assert.IsNotNull( task );
                    using ( task )
                    {
                        ArrayList categories = OutlookSession.GetCategories( task );
                        Assert.AreEqual( null, categories );
                        categories = new ArrayList();
                        categories.Add( "category1" );
                        categories.Add( "category2" );
                        categories.Add( "category3" );
                        OutlookSession.SetCategories( task, categories );
                        task.SaveChanges();
                    }
                    task = messages.OpenMessage( 0 );
                    Assert.IsNotNull( task );
                    using ( task )
                    {
                        ArrayList categories = OutlookSession.GetCategories( task );
                        Assert.AreEqual( 3, categories.Count );
                        categories.Remove( "category1" );
                        categories.Remove( "category2" );
                        categories.Remove( "category3" );
                        Assert.AreEqual( 0, categories.Count );
                        OutlookSession.SetCategories( task, null );
                        task.SaveChanges();
                    }
                    task = messages.OpenMessage( 0 );
                    Assert.IsNotNull( task );
                    using ( task )
                    {
                        ArrayList categories = OutlookSession.GetCategories( task );
                        Assert.AreEqual( null, categories );
                    }

                }
            }
        }

        class FreshMailEnum : IFolderDescriptorEnumeratorEvent
        {
            public bool FolderFetched(FolderDescriptor parent, FolderDescriptor folder, out FolderDescriptor folderTag)
            {
                folderTag = folder;
                IEFolder mapiFolder =
                    OutlookSession.OpenFolder( folder.FolderIDs.EntryId, folder.FolderIDs.StoreId );
                if ( mapiFolder != null )
                {
                    using ( mapiFolder )
                    {
                        string name = mapiFolder.GetStringProp( MAPIConst.PR_DISPLAY_NAME );
                        Tracer._Trace( name );
                        string containerClass = mapiFolder.GetStringProp( MAPIConst.PR_CONTAINER_CLASS );
                        containerClass = containerClass;
                        EnumerateMailWithBody( mapiFolder );
                        for ( int i = 0; i < 1; ++i )
                        {
                            EnumerateMail( mapiFolder );
                        }
                    }
                }
                if ( parent == null )
                {
                    return true;
                }
                return true;
            }
            void EnumerateMail( IEFolder mapiFolder )
            {
                IETable table = null;
                try
                {
                    table = mapiFolder.GetEnumTable( DateTime.Now.AddDays( -3 ) );
                }
                catch ( System.UnauthorizedAccessException ){}
                if ( table == null )
                    return;
                using ( table )
                {
                    int count = table.GetRowCount();
                    if ( count > 0 )
                    {
                        table.Sort( MAPIConst.PR_MESSAGE_DELIVERY_TIME, false );
                    }
                    for ( uint i = 0; i < count; i++ )
                    {
                        IERowSet row = table.GetNextRow();
                        if ( row == null ) continue;
                        using ( row )
                        {
                            for ( int j = 0; j < 1; ++j )
                            {
                                ProcessRow( row, mapiFolder );
                            }
                        }
                    }
                }
            }
            void EnumerateMailWithBody( IEFolder mapiFolder )
            {
                IEMessages messages = mapiFolder.GetMessages();
                if ( messages == null ) return;
                using ( messages )
                {
                    int count = messages.GetCount();
                    if ( count > 100 )
                    {
                        count = 100;
                    }
                    for ( int i = 0; i < count; ++i )
                    {
                        IEMessage message = messages.OpenMessage( i );
                        if ( message == null )
                        {
                            continue;
                        }
                        using ( message )
                        {
                            string plainBody = message.GetPlainBody();
                            plainBody = plainBody;
                        }
                    }
                }
            }
            private void ProcessRow( IERowSet row, IEFolder mapiFolder )
            {
                mapiFolder = mapiFolder;
                string entryID = row.GetBinProp( 1 );
                if ( entryID == null )
                {
                    entryID = row.GetBinProp( 0 );
                }
                string messageClass = row.GetStringProp( 3 );
                messageClass = messageClass;
            }


        }
        [Test]//, Ignore( "Investigating problems on OMNIAMEA-UNIT")]
        public void FreshMailTest()
        {
            foreach ( IEMsgStore msgStore in OutlookSession.GetMsgStores() )
            {
                if ( msgStore == null ) continue;
                string storeID = msgStore.GetBinProp( MAPIConst.PR_STORE_ENTRYID );
                string name = msgStore.GetStringProp( MAPIConst.PR_DISPLAY_NAME );
                Tracer._Trace( "name == " + name );
                FolderDescriptorEnumerator.Do( msgStore, storeID, name, new FreshMailEnum() );
            }
        }
    }
}
