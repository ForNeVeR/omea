// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OutlookPlugin;
using JetBrains.Omea.PicoCore;
using JetBrains.Omea.ResourceTools;
using NUnit.Framework;

namespace OutlookPlugin.Tests
{
	/// <summary>
	/// Summary description for ContactNamesTests.
	/// </summary>
    [TestFixture]
    public class ContactNamesTests
    {
        private TestCore _core;
        private IResourceStore _storage;
        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;
            _storage.PropTypes.Register( "Subject", PropDataType.String );
            OutlookProcessor.SetSyncVersion( OutlookProcessor.CURRENT_VERSION );

            AddressBook.Initialize( true );
            REGISTRY.RegisterTypes( null, _core.ContactManager );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        [Test] public void CheckIfNamesValidTest()
        {
            ContactNames contactNames = new ContactNames();
            IContact contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNull( contact );
            contactNames.FirstName = "Sergey";
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            contactNames.FirstName = null;
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNull( contact );
            contactNames.LastName = "Zhulin";
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            contactNames.LastName = null;
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNull( contact );
            contactNames.FullName = "Sergey Zhulin";
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
        }
        [Test] public void MySelfWithoutEntryIDTest()
        {
            string ownerEmail = "zhu@intellij.com";
            IContact contact = Core.ContactManager.MySelf;
            int selfId = contact.Resource.Id;
            Assert.IsNotNull( contact );
            contact.AddAccount( ownerEmail );
            ContactNames contactNames = new ContactNames();
            contactNames.EmailAddress = ownerEmail;
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNull( contact );

            contactNames.FullName = "Sergey Zhulin";
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            Assert.AreEqual( selfId, contact.Resource.Id );
            Assert.AreEqual( "Sergey", contact.FirstName );
            Assert.AreEqual( "Zhulin", contact.LastName );
        }
        [Test] public void MySelfWithSameEntryIDTest()
        {
            string ownerEmail = "zhu@intellij.com";
            IContact contact = Core.ContactManager.MySelf;
            int selfId = contact.Resource.Id;
            Assert.IsNotNull( contact );
            contact.AddAccount( ownerEmail );
            contact.Resource.SetProp( PROP.EntryID, "123" );
            ContactNames contactNames = new ContactNames();
            contactNames.EmailAddress = ownerEmail;
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNull( contact );

            contactNames.FullName = "Sergey Zhulin";
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            Assert.AreEqual( selfId, contact.Resource.Id );
            Assert.AreEqual( "Sergey", contact.FirstName );
            Assert.AreEqual( "Zhulin", contact.LastName );
        }
        [Test] public void MySelfWithDiffEntryIDTest()
        {
            string ownerEmail = "zhu@intellij.com";
            IContact contact = Core.ContactManager.MySelf;
            int selfId = contact.Resource.Id;
            Assert.IsNotNull( contact );
            contact.AddAccount( ownerEmail );
            contact.Resource.SetProp( PROP.EntryID, "111" );
            ContactNames contactNames = new ContactNames();
            contactNames.EmailAddress = ownerEmail;
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNull( contact );

            contactNames.FullName = "Sergey Zhulin";
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            Assert.AreEqual( false, selfId == contact.Resource.Id );
            Assert.AreEqual( "Sergey", contact.FirstName );
            Assert.AreEqual( "Zhulin", contact.LastName );
            Assert.AreEqual( ownerEmail, contact.DefaultEmailAddress );
        }
        [Test] public void MySelfWithSameEntryIDAndUpdateFieldsTest()
        {
            string ownerEmail = "zhu@intellij.com";
            IContact contact = Core.ContactManager.MySelf;
            contact.FirstName = "Сергей";
            contact.LastName = "Жулин";
            int selfId = contact.Resource.Id;
            Assert.IsNotNull( contact );
            contact.AddAccount( ownerEmail );
            contact.Resource.SetProp( PROP.EntryID, "123" );
            ContactNames contactNames = new ContactNames();
            contactNames.EmailAddress = ownerEmail;
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNull( contact );

            contactNames.FullName = "Sergey Zhulin";
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            Assert.AreEqual( selfId, contact.Resource.Id );
            Assert.AreEqual( "Sergey", contact.FirstName );
            Assert.AreEqual( "Zhulin", contact.LastName );
        }
        [Test] public void ContactWithSameEntryIDAndUpdateFieldsTest()
        {
            string email = "zhu@intellij.com";
            IContact contact = Core.ContactManager.CreateContact( "Сергей Жулин" );
            int id = contact.Resource.Id;
            Assert.IsNotNull( contact );
            contact.AddAccount( email );
            contact.Resource.SetProp( PROP.EntryID, "123" );
            ContactNames contactNames = new ContactNames();
            contactNames.EmailAddress = "zhu@jetbrains.ru";
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNull( contact );

            contactNames.FullName = "Sergey Zhulin";
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            Assert.AreEqual( id, contact.Resource.Id );
            Assert.AreEqual( "Sergey", contact.FirstName );
            Assert.AreEqual( "Zhulin", contact.LastName );
            IResourceList accounts = contact.Resource.GetLinksOfType( STR.EmailAccount, "EmailAcct" );
            accounts.Sort( new SortSettings( Core.ContactManager.Props.EmailAddress, false ) );
            Assert.AreEqual( 2, accounts.Count );
        }
        [Test] public void ContactWithDiffEntryIDAndCandidatNotExistTest()
        {
            string email = "zhu@intellij.com";
            IContact contact = Core.ContactManager.CreateContact( "Сергей Жулин" );
            int id = contact.Resource.Id;
            Assert.IsNotNull( contact );
            contact.AddAccount( email );
            contact.Resource.SetProp( PROP.EntryID, "111" );
            ContactNames contactNames = new ContactNames();
            contactNames.FullName = "Sergey Zhulin";
            contactNames.EmailAddress = "zhu@jetbrains.ru";
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            Assert.AreEqual( false, id == contact.Resource.Id );
        }

        [Test] public void ContactWithDiffEntryIDAndCandidatExistsTest()
        {
            string email = "zhu@intellij.com";
            IContact contact = Core.ContactManager.CreateContact( "Sergey Zhulin" );
            Assert.IsNotNull( contact );
            contact.AddAccount( email );
            contact.Resource.SetProp( PROP.EntryID, "111" );
            int id = contact.Resource.Id;

            contact = Core.ContactManager.CreateContact( "", "Sergey", "", "", "" );
            Assert.IsNotNull( contact );
            contact.AddAccount( email );

            contact = Core.ContactManager.CreateContact( "Sergey Zhulin" );
            Assert.IsNotNull( contact );
            contact.AddAccount( email );
            id = contact.Resource.Id;

            ContactNames contactNames = new ContactNames();
            contactNames.FullName = "Sergey Zhulin";
            contactNames.EmailAddress = email;
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            Assert.AreEqual( id, contact.Resource.Id );
        }
        [Test] public void BlankContactWithSameAccountAndWithoutEntryIDTest()
        {
            IResource res = _storage.NewResource( "Contact" );

            string email = "zhu@intellij.com";
            IContact contact = Core.ContactManager.GetContact( res );
            Assert.IsNotNull( contact );
            contact.AddAccount( email );
            int id = contact.Resource.Id;

            ContactNames contactNames = new ContactNames();
            contactNames.FullName = "Sergey Zhulin";
            contactNames.EmailAddress = email;
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            Assert.AreEqual( id, contact.Resource.Id );
        }
        [Test] public void BlankContactWithSameAccountAndWithEntryIDTest()
        {
            IResource res = _storage.NewResource( "Contact" );

            string email = "zhu@intellij.com";
            IContact contact = Core.ContactManager.GetContact( res );
            Assert.IsNotNull( contact );
            contact.AddAccount( email );
            contact.Resource.SetProp( PROP.EntryID, "111" );
            int id = contact.Resource.Id;

            ContactNames contactNames = new ContactNames();
            contactNames.FullName = "Sergey Zhulin";
            contactNames.EmailAddress = email;
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            Assert.AreEqual( false, id == contact.Resource.Id );
        }
        [Test] public void BlankContactWithSameAccountAndWithSameEntryIDTest()
        {
            IResource res = _storage.NewResource( "Contact" );

            string email = "zhu@intellij.com";
            IContact contact = Core.ContactManager.GetContact( res );
            Assert.IsNotNull( contact );
            contact.AddAccount( email );
            contact.Resource.SetProp( PROP.EntryID, "123" );
            int id = contact.Resource.Id;

            ContactNames contactNames = new ContactNames();
            contactNames.FullName = "Sergey Zhulin";
            contactNames.EmailAddress = email;
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            Assert.AreEqual( id, contact.Resource.Id );
        }
        [Test] public void MySelfWithOtherEntryIDTest()
        {
            string ownerEmail = "zhu@intellij.com";
            IContact contact = Core.ContactManager.MySelf;
            Assert.IsNotNull( contact );
            contact.AddAccount( ownerEmail );
            contact.Resource.SetProp( PROP.EntryID, "111" );
            contact.FirstName = "Sergey";
            contact.LastName = "Zhulin";

            contact = Core.ContactManager.CreateContact( "Sergey Zhulin" );
            contact.AddAccount( ownerEmail );
            contact.Resource.SetProp( PROP.EntryID, "123" );
            int id = contact.Resource.Id;

            ContactNames contactNames = new ContactNames();
            contactNames.FirstName = "Sergey";
            contactNames.LastName = "Zhulin";
            contactNames.EmailAddress = ownerEmail;
            contact = contactNames.FindOrCreateContact( "123" );
            Assert.IsNotNull( contact );
            Assert.AreEqual( id, contact.Resource.Id );
        }
    }
}
