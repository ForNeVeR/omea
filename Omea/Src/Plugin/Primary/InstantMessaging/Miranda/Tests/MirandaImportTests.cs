// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.InstantMessaging.Miranda;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;

namespace JetBrains.Omea.InstantMessaging.Miranda.Tests
{
	[TestFixture]
    public class MirandaImportTests
	{
        private TestCore _core;
        private IResourceStore _storage;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;
            Props.Register();
            ResourceTypes.Register( null );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        private void DoImportDB( IMirandaDB db )
        {
            IMConversationsManager convManager = new IMConversationsManager(
                ResourceTypes.MirandaConversation, "Miranda Conversation", "Subject",
                IniSettings.ConversationPeriodTimeSpan,
                Props.MirandaAcct, Props.FromAccount, Props.ToAccount, null );

            MirandaImportJob importJob = new MirandaImportJob( "", convManager, null );
            importJob.ImportDB( db );
            while( true )
            {
                AbstractJob job = importJob.GetNextJob();
                if ( job == null )
                    break;
                job.NextMethod.Invoke();
            }
            importJob.EnumerationFinished();
        }

        [Test] public void TestImportICQAccount()
        {
            MockMirandaDB db = new MockMirandaDB();
            MockMirandaContact ownerContact = (MockMirandaContact) db.UserContact;
            ownerContact.AddSetting( "ICQ", "UIN", 84614327 );
            ownerContact.AddSetting( "ICQ", "FirstName", "Dmitry" );
            ownerContact.AddSetting( "ICQ", "LastName", "Jemerov" );

            DoImportDB( db );

            IContact contact = Core.ContactManager.FindContact( "Dmitry Jemerov" );
            Assert.IsNotNull( contact );
            IResource icqAccount = contact.Resource.GetLinkProp( Props.MirandaAcct );
            Assert.AreEqual( ResourceTypes.MirandaICQAccount, icqAccount.Type );
            Assert.AreEqual( 84614327, icqAccount.GetProp( Props.UIN ) );
        }

        [Test] public void ImportICQWithExistingAccount()
        {
            IContact contact = Core.ContactManager.FindOrCreateContact( null, "Dmitry Jemerov" );
            IResource icqAccount = _storage.NewResource( ResourceTypes.MirandaICQAccount );
            icqAccount.SetProp( Props.UIN, 84614327 );
            icqAccount.AddLink( Props.MirandaAcct, contact.Resource );

            MockMirandaDB db = new MockMirandaDB();
            MockMirandaContact ownerContact = (MockMirandaContact) db.UserContact;
            ownerContact.AddSetting( "ICQ", "UIN", 84614327 );
            ownerContact.AddSetting( "ICQ", "FirstName", "yole@work" );

            MockMirandaContact otherContact = db.AddContact();
            otherContact.AddSetting( "ICQ", "UIN", 1000 );
            otherContact.AddSetting( "ICQ", "FirstName", "Test" );
            otherContact.AddEvent( "ICQ", 0, DateTime.Now, 0, "Hello" );

            DoImportDB( db );

            IResourceList conversations = _storage.GetAllResources( ResourceTypes.MirandaConversation );
            Assert.AreEqual( 1, conversations.Count );
            IResource conv = conversations [0];
            Assert.AreEqual( contact.Resource, conv.GetLinkProp( "To" ) );
        }

        [Test] public void ImportAIMWithExistingAccount()
        {
            Core.ContactManager.FindOrCreateMySelfContact( null, "test" );

            IContact contact = Core.ContactManager.FindOrCreateContact( null, "Dmitry Jemerov" );
            IResource aimAccount = _storage.NewResource( ResourceTypes.MirandaAIMAccount );
            aimAccount.SetProp( Props.ScreenName, "intelliyole" );
            aimAccount.AddLink( Props.MirandaAcct, contact.Resource );

            MockMirandaDB db = new MockMirandaDB();
            MockMirandaContact ownerContact = (MockMirandaContact) db.UserContact;
            ownerContact.AddSetting( "AIM", "SN", "test" );
            ownerContact.AddSetting( "AIM", "Nick", "test" );

            MockMirandaContact otherContact = db.AddContact();
            otherContact.AddSetting( "AIM", "SN", "intelliyole" );
            ownerContact.AddSetting( "AIM", "Nick", "intelliyole" );
            otherContact.AddEvent( "AIM", 0, DateTime.Now, 0, "Hello" );

            DoImportDB( db );

            IResourceList contacts = Core.ResourceStore.GetAllResources( "Contact" );
            contacts.Sort( new SortSettings( ResourceProps.DisplayName, true ) );
            Assert.AreEqual( 2, contacts.Count );
            Assert.AreEqual( "Dmitry Jemerov", contacts [0].DisplayName );
            Assert.AreEqual( "test", contacts [1].DisplayName );
        }
	}
}
