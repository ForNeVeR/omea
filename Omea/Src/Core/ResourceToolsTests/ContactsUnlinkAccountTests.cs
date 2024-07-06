// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;

namespace ContactsTests
{
    [TestFixture]
    public class ContactBOUnlinkAccountTests
    {
        private TestCore        _core;
        private IResourceStore  _storage;
        private ContactManager _contactManager;
        private int             linkAttachment;

        [SetUp]
        public void SetUp()
        {
            _core = new TestCore();
            _storage = Core.ResourceStore;
            _storage.PropTypes.Register( "Subject", PropDataType.String );
            _storage.ResourceTypes.Register( "Email", "Subject" );

            _contactManager = (ContactManager) _core.ContactManager;
            _storage.PropTypes.Register( "LinkedSetValue", PropDataType.Link, PropTypeFlags.Internal );

            linkAttachment = Core.ResourceStore.PropTypes.Register( "Attachment",
                PropDataType.Link, PropTypeFlags.SourceLink | PropTypeFlags.DirectedLink );
            Core.ResourceStore.PropTypes.RegisterDisplayName( linkAttachment, "Outlook Message", "Outlook Attachment" );
        }

        [TearDown]
        public void TearDown()
        {
            _core.Dispose();
        }

        //  Config: two contacts, 2 mails, 0 contact names (both contacts have a single
        //          accounts, no sender names), 3 accounts.
        //  Configuration: (main,account1,null) <-From-< email1 >-To-> (extra,account3,null)
        //                 (main,account2,null) <-To-<   email2 >-From-> (extra,account3,null)
        //  Action: unlink the contact and account1;
        //  Expected: new contact created, new contact name (it has the name of
        //            account), email1's link From points to the new Contact and
        //            the ContactName:
        //                 (new,account1,"From: account1") <-From-< email1 >-To-> (extra,account3,null)
        //                 (main,account2,null) <-To-<   email2 >-From-> (extra,account3,null)
        [Test] public void UnlinkContactAndAccountTestWithoutContactNames()
        {
            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 0, contacts.Count );

            IContact main = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 1, contacts.Count );

            IContact extra = _contactManager.FindOrCreateContact( "account2", "FirstName LastName" );
            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 2, contacts.Count );

            IResource account1 = main.Resource.GetLinkProp( "EmailAcct" );
            IResource account2 = extra.Resource.GetLinkProp( "EmailAcct" );
            IResource account3 = Core.ContactManager.FindOrCreateEmailAccount( "account3" );

            IResource email1 = _storage.NewResource( "email" );
            email1.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email1, account1, "aFrom" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, extra.Resource, email1, account3, "aTo" );

            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 2, contacts.Count );

            IResource email2 = _storage.NewResource( "email" );
            email2.SetProp( "Subject", "Subject2" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, main.Resource, email2, account2, "bTo" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, extra.Resource, email2, account3, "bFrom" );

            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 2, contacts.Count );

            IResourceList names = account3.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkEmailAcct );
            Assert.AreEqual( names.Count, 2 );

            //  emulate the case when there is no ContactName linked to the e-account
            names = account1.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkEmailAcct );
            Assert.AreEqual( names.Count, 1 );
            names.DeleteAll();

            //-----------------------------------------------------------------
            _contactManager.HardRemoveAccountFromContact( main.Resource, account1 );
            //-----------------------------------------------------------------

            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 3, contacts.Count );

            //  DO NOT! expect newly created contact name.
            IResourceList contactNames = _storage.GetAllResources( "ContactName" );
            Assert.AreEqual( 3, contactNames.Count );

            IResourceList newContacts = account1.GetLinksOfType( "Contact", _contactManager.Props.LinkEmailAcct );
            Assert.AreEqual( 1, newContacts.Count );

            IResource newContact = newContacts[ 0 ];
            Assert.AreEqual( "From: account1", newContact.GetStringProp( "LastName" ) );

            Assert.AreEqual( 1, newContact.GetLinksOfType( "email", _contactManager.Props.LinkFrom ).Count );
            Assert.AreEqual( 0, newContact.GetLinksOfType( "email", _contactManager.Props.LinkTo ).Count );
            Assert.AreEqual( 0, main.Resource.GetLinksOfType( "email", _contactManager.Props.LinkFrom ).Count );
            Assert.AreEqual( 1, main.Resource.GetLinksOfType( "email", _contactManager.Props.LinkTo ).Count );
        }

        //  Config: two contacts, 2 mails, 2 contact names (created by artificially setting
        //          2 sender names for main contact), 3 accounts.
        //  Configuration: (main,account1,"A") <-From-< email1 >-To-> (extra,account3,null)
        //                 (main,account2,"B") <-To-<   email2 >-From-> (extra,account3,null)
        //  Action: unlink the contact and account1;
        //  Expected results:  new contact created, contact name is retargeted,
        //          email1's link From points to the new Contact and the old ContactName:
        //                 (new("A"),account1,"A") <-From-< email1 >-To-> (extra,account3,null)
        //                 (main,account2,"B")     <-To-<   email2 >-From-> (extra,account3,null)
        [Test] public void UnlinkContactAndAccountTestWithContactNames()
        {
            IContact main = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            IContact extra = _contactManager.FindOrCreateContact( "account2", "FirstName LastName" );
            IResource account1 = main.Resource.GetLinkProp( "EmailAcct" );
            IResource account2 = extra.Resource.GetLinkProp( "EmailAcct" );
            IResource account3 = Core.ContactManager.FindOrCreateEmailAccount( "account3" );

            IResource email1 = _storage.NewResource( "email" );
            email1.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email1, account1, "A" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, extra.Resource, email1, account3, "aTo" );

            IResource email2 = _storage.NewResource( "email" );
            email2.SetProp( "Subject", "Subject2" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, main.Resource, email2, account2, "B" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, extra.Resource, email2, account3, "bFrom" );

            //-----------------------------------------------------------------
            _contactManager.HardRemoveAccountFromContact( main.Resource, account1 );
            //-----------------------------------------------------------------

            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 3, contacts.Count );

            //  Expect contact names are on their places.
            IResourceList contactNames = _storage.GetAllResources( "ContactName" );
            Assert.AreEqual( 4, contactNames.Count );

            IResourceList newContacts = account1.GetLinksOfType( "Contact", _contactManager.Props.LinkEmailAcct );
            Assert.AreEqual( 1, newContacts.Count );

            IResource newContact = newContacts[ 0 ];
            Assert.AreEqual( "A", newContact.GetStringProp( "LastName" ) );

            Assert.AreEqual( 1, newContact.GetLinksOfType( "email", _contactManager.Props.LinkFrom ).Count );
            Assert.AreEqual( 0, newContact.GetLinksOfType( "email", _contactManager.Props.LinkTo ).Count );
            Assert.AreEqual( 0, main.Resource.GetLinksOfType( "email", _contactManager.Props.LinkFrom ).Count );
            Assert.AreEqual( 1, main.Resource.GetLinksOfType( "email", _contactManager.Props.LinkTo ).Count );
        }

        //  Config: two contacts, 3 mails, 3 contact names (created by artificially setting
        //          2 sender names for main contact), 2 accounts.
        //  Configuration: (main,account1,"A") <-From-< email1 >-To-> (extra,account3,null)
        //                 (main,account1,"B") <-From-< email2 >-To-> (extra,account3,null)
        //                 (main,account2,"C") <-To-<   email3 >-From-> (extra,account3,null)
        //  Action: unlink the contact and account1;
        //  Expected:  2 new contacts created, email1's link From points to the new Contact "A",
        //             email2's link From points to the new Contact "B":
        //                 (new1("A"),account1,"A") <-From-< email1 >-To-> (extra,account3,null)
        //                 (new2("B"),account1,"B") <-From-< email2 >-To-> (extra,account3,null)
        //                 (main,account2,"B")      <-To-<   email2 >-From-> (extra,account3,null)
        [Test] public void UnlinkContactAndAccountTestWithContactNamesNumerousNewContacts()
        {
            IContact main = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            IContact extra = _contactManager.FindOrCreateContact( "account2", "FirstName LastName" );
            IResource account1 = main.Resource.GetLinkProp( "EmailAcct" );
            IResource account2 = extra.Resource.GetLinkProp( "EmailAcct" );
            IResource account3 = Core.ContactManager.FindOrCreateEmailAccount( "account3" );

            IResource email1 = _storage.NewResource( "email" );
            email1.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email1, account1, "A" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, extra.Resource, email1, account3, "aTo" );

            IResource email2 = _storage.NewResource( "email" );
            email2.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email2, account1, "B" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, extra.Resource, email2, account3, "aTo" );

            IResource email3 = _storage.NewResource( "email" );
            email3.SetProp( "Subject", "Subject2" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, main.Resource, email3, account2, "C" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, extra.Resource, email3, account3, "bFrom" );

            //-----------------------------------------------------------------
            _contactManager.HardRemoveAccountFromContact( main.Resource, account1 );
            //-----------------------------------------------------------------

            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 4, contacts.Count );

            //  Expect contact names are on their places.
            IResourceList contactNames = _storage.GetAllResources( "ContactName" );
            Assert.AreEqual( 5, contactNames.Count );

            IResourceList newContacts = account1.GetLinksOfType( "Contact", _contactManager.Props.LinkEmailAcct );
            Assert.AreEqual( 2, newContacts.Count );

            IResource newContact1, newContact2;
            if( newContacts[ 0 ].Id < newContacts[ 1 ].Id )
            { newContact1 = newContacts[ 0 ]; newContact2 = newContacts[ 1 ]; }
            else
            { newContact1 = newContacts[ 1 ]; newContact2 = newContacts[ 0 ]; }

            Assert.AreEqual( "A", newContact1.GetStringProp( "LastName" ) );
            Assert.AreEqual( "B", newContact2.GetStringProp( "LastName" ) );

            Assert.AreEqual( 1, newContact1.GetLinksOfType( "email", _contactManager.Props.LinkFrom ).Count );
            Assert.AreEqual( 0, newContact1.GetLinksOfType( "email", _contactManager.Props.LinkTo ).Count );
            Assert.AreEqual( 1, newContact2.GetLinksOfType( "email", _contactManager.Props.LinkFrom ).Count );
            Assert.AreEqual( 0, newContact2.GetLinksOfType( "email", _contactManager.Props.LinkTo ).Count );

            Assert.AreEqual( 0, main.Resource.GetLinksOfType( "email", _contactManager.Props.LinkFrom ).Count );
            Assert.AreEqual( 1, main.Resource.GetLinksOfType( "email", _contactManager.Props.LinkTo ).Count );
        }

        //  Config: one contact, 1 mail, 2 contact names, 2 accounts.
        //  NB!!!:  mail is linked by two links to the same contact (but different accounts)
        //  Action: unlink the contact and one of the accounts;
        //  Expected:  new contact created,  mails refer both to the old and new contacts.
        [Test] public void UnlinkContactAndAccountTestFromAndToAreSameContact()
        {
            IContact main = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            _contactManager.FindOrCreateContact( "account2", "Michael Gerasimov" );
            IResource account1 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account1" );
            IResource account2 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account2" );
            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 1, contacts.Count );

            IResource email = _storage.NewResource( "email" );
            email.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email, account1, "A" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, main.Resource, email, account2, "B" );

            IResourceList contactNames = _storage.GetAllResources( "ContactName" );
            Assert.AreEqual( 2, contactNames.Count );

            IResourceList list = email.GetLinksOfType( null, _contactManager.Props.LinkFrom );
            Console.WriteLine( list.Count );
            foreach( IResource res in list )
                Console.WriteLine( "Contact is " + res.DisplayName + " type is " + res.Type );

            //-----------------------------------------------------------------
            _contactManager.HardRemoveAccountFromContact( main.Resource, account2 );
            //-----------------------------------------------------------------

            //  2 contact - the old one and the new one.
            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 2, contacts.Count );

            //  Contact name must be preserved.
            contactNames = _storage.GetAllResources( "ContactName" );
            Assert.AreEqual( 2, contactNames.Count );

            list = email.GetLinksOfType( null, _contactManager.Props.LinkFrom );
            Console.WriteLine( list.Count );
            foreach( IResource res in list )
                Console.WriteLine( "Contact is " + res.DisplayName + " type is " + res.Type );

            //  check some links are preserved.
            Assert.AreEqual( email.HasLink( _contactManager.Props.LinkFrom, main.Resource ), true );
            Assert.AreEqual( email.HasLink( _contactManager.Props.LinkTo, main.Resource ), false );
        }

        //  Config: two contacts, 2 mails, 2 contact names (created by artificially setting
        //          2 sender names for main contact), 3 accounts.
        //  Configuration: (main,account1,"A") <-From-< email1 >-To-> (extra,account3,null)
        //                 (main,account2,"A") <-To-<   email2 >-From-> (extra,account3,null)
        //  Action: unlink the contact and account1;
        //  Expected:  new contact created, contact name is retargeted,
        //             email1's link From points to the new Contact and the old ContactName:
        //                 (new("A"),account1,"A") <-From-< email1 >-To-> (extra,account3,null)
        //                 (main,account2,"B")     <-To-<   email2 >-From-> (extra,account3,null)
        //          AND:
        //          initially name "A" is put into the list of sender names of the "mail"
        //          contact. When unlinking, we check whether to remove this name from its list.
        [Test] public void UnlinkContactAndAccountTestWithEqualContactNames()
        {
            IContact main = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            IContact extra = _contactManager.FindOrCreateContact( "account2", "FirstName LastName" );
            IResource account1 = main.Resource.GetLinkProp( "EmailAcct" );
            IResource account2 = extra.Resource.GetLinkProp( "EmailAcct" );
            IResource account3 = Core.ContactManager.FindOrCreateEmailAccount( "account3" );

            //---
            IResource email1 = _storage.NewResource( "email" );
            email1.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email1, account1, "A" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, extra.Resource, email1, account3, "aTo" );

            //---
            IResource email2 = _storage.NewResource( "email" );
            email2.SetProp( "Subject", "Subject2" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, main.Resource, email2, account2, "A" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, extra.Resource, email2, account3, "bFrom" );

            //---
            IResourceList cNames = main.Resource.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
            Assert.AreEqual( 2, cNames.Count );
            foreach( IResource res in cNames )
                Console.WriteLine( res.DisplayName );
            Console.WriteLine( "----------------" );

            //-----------------------------------------------------------------
            _contactManager.HardRemoveAccountFromContact( main.Resource, account1 );
            //-----------------------------------------------------------------

            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 3, contacts.Count );

            //  Expect contact names are on their places.
            IResourceList contactNames = _storage.GetAllResources( "ContactName" );
            Assert.AreEqual( 4, contactNames.Count );

            IResourceList newContacts = account1.GetLinksOfType( "Contact", _contactManager.Props.LinkEmailAcct );
            Assert.AreEqual( 1, newContacts.Count );

            IResource newContact = newContacts[ 0 ];
            Assert.AreEqual( "A", newContact.GetStringProp( "LastName" ) );

            Assert.AreEqual( 1, newContact.GetLinksOfType( "email", _contactManager.Props.LinkFrom ).Count );
            Assert.AreEqual( 0, newContact.GetLinksOfType( "email", _contactManager.Props.LinkTo ).Count );
            Assert.AreEqual( 0, main.Resource.GetLinksOfType( "email", _contactManager.Props.LinkFrom ).Count );
            Assert.AreEqual( 1, main.Resource.GetLinksOfType( "email", _contactManager.Props.LinkTo ).Count );

            cNames = main.Resource.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
            Assert.AreEqual( 1, cNames.Count );
            foreach( IResource res in cNames )
                Console.WriteLine( res.DisplayName );
            Console.WriteLine( "----------------" );
        }

        //  Config: 2 contacts, 2 accounts, 1 mail. Both contacts (and accounts)
        //          are linked with the same type of link (CC in this case).
        //  Action: remove account from the first contact.
        //  Expected: both links CC are present (check the bug OM-7151).
        [Test] public void RelinkAccountWithMailLinkedByNumerousContacts()
        {
            IContact main = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            IContact extra = _contactManager.FindOrCreateContact( "account2", "FirstName LastName" );
            IResource account1 = main.Resource.GetLinkProp( "EmailAcct" );
            IResource account2 = extra.Resource.GetLinkProp( "EmailAcct" );

            //---
            IResource email = _storage.NewResource( "email" );
            email.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkCC, main.Resource, email, account1, "A" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkCC, extra.Resource, email, account2, "aTo" );

            //-----------------------------------------------------------------
            _contactManager.HardRemoveAccountFromContact( main.Resource, account1 );
            //-----------------------------------------------------------------

            Assert.AreEqual( 2, email.GetLinksOfType( "Contact", _contactManager.Props.LinkCC ).Count );
        }

        //  Config: one contact, 1 mail, 2 contact names, 2 accounts.
        //  NB!!!:  mail is linked by two links to the same contact (but different accounts)
        //  Action: unlink the contact and one of the accounts;
        //  Expected:  new contact created,  mails refer both to the old and new contacts.
        [Test] public void UnlinkContactAndAccountTestFromAndToAreSameContactPlusNewMail()
        {
            IContact main = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            _contactManager.FindOrCreateContact( "account2", "Michael Gerasimov" );
            IResource account1 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account1" );
            IResource account2 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account2" );
            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 1, contacts.Count );

            IResource email = _storage.NewResource( "email" );
            email.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email, account1, "Michael Gerasimov" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, main.Resource, email, account2, "Michael Gerasimov (JIRA)" );

            IResourceList contactNames = _storage.GetAllResources( "ContactName" );
            //  Take into account that contact names which coinside with the
            //  name of the contact are not created and resources are linked only
            //  directly.
            Assert.AreEqual( 1, contactNames.Count );

            IResourceList list = email.GetLinksOfType( null, _contactManager.Props.LinkFrom );
            Console.WriteLine( list.Count );
            foreach( IResource res in list )
                Console.WriteLine( "Contact is " + res.DisplayName + " type is " + res.Type );

            //-----------------------------------------------------------------
            _contactManager.HardRemoveAccountFromContact( main.Resource, account2 );
            //-----------------------------------------------------------------

            //  2 contacts - the old one and the new one.
            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 2, contacts.Count );

            //  Contact names must be preserved.
            contactNames = _storage.GetAllResources( "ContactName" );
            Assert.AreEqual( 1, contactNames.Count );

            list = email.GetLinksOfType( null, _contactManager.Props.LinkTo );
            Assert.AreEqual( list.Count, 1 );
            IResource newContactRes = list[ 0 ];
            Assert.AreEqual( newContactRes.GetStringProp( "FirstName" ), "Michael" );
            Assert.AreEqual( newContactRes.GetStringProp( "LastName" ), "Gerasimov" );

            //  Check preserveness of contact names.
            IResourceList names = newContactRes.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
            Assert.AreEqual( names.Count, 1 );

            IResource contactName = names[ 0 ];
            Assert.AreEqual( contactName.GetStringProp( "Name" ), "Michael Gerasimov (JIRA)" );

            //  check some links are preserved.
            Assert.AreEqual( email.HasLink( _contactManager.Props.LinkFrom, main.Resource ), true );
            Assert.AreEqual( email.HasLink( _contactManager.Props.LinkTo, main.Resource ), false );

            //-----------------------------------------------------------------
            IContact what = _contactManager.FindOrCreateContact( "account2", "Michael Gerasimov (JIRA)" );
            //-----------------------------------------------------------------

            Assert.AreEqual( what.Resource.Id, newContactRes.Id );
        }
/*
        //  Config: 1 mail, 1 attachment, 2 contacts, 2 accounts.
        //  Action: Unlink account from the first contact;
        //  Expected: new contact is created, mail is relinked to it, and its
        //            attachment is also relinked to it.
        [Test] public void UnlinkContactAndAccountTestMailWithAttachments()
        {
            IContact main  = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            IContact extra = _contactManager.FindOrCreateContact( "account2", "FirstName LastName" );
            IResource account1 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account1" );
            IResource account2 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account2" );
            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 2, contacts.Count );

            IResource email = _storage.NewResource( "email" );
            email.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email, account1, "A" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, extra.Resource, email, account2, "B" );

            IResource attach = _storage.NewResource( "email" );
            attach.SetProp( "Name", "AttachmentResource" );
            attach.SetProp( _contactManager.Props.LinkFrom, main.Resource );
            attach.SetProp( _contactManager.Props.LinkTo, extra.Resource );
            attach.SetProp( "Attachment", email );

            //-----------------------------------------------------------------
            _contactManager.HardRemoveAccountFromContact( main.Resource, account1 );
            //-----------------------------------------------------------------

            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 3, contacts.Count );

            IResource newContact = email.GetLinkProp( _contactManager.Props.LinkFrom );
            Assert.IsTrue( newContact.Id != main.Resource.Id );
            Assert.IsTrue( attach.GetLinkProp( _contactManager.Props.LinkFrom ).Id == newContact.Id );
        }
*/
        //  Config: 1 mail, 1 attachment, 4 contacts, 4 accounts.
        //  Action: Unlink account from the first contact;
        //  Expected: new contact is created, mail is relinked to it, and its
        //            attachment is also relinked to it.
        [Test] public void UnlinkContactAndAccountTestMailWithAttachmentsLinkedToSeveralContacts()
        {
            IContact to1  = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            IContact to2  = _contactManager.FindOrCreateContact( "account2", "F1 L1" );
            IContact to3  = _contactManager.FindOrCreateContact( "account3", "F2 L2" );
            IContact extra = _contactManager.FindOrCreateContact( "account4", "F3 L3" );
            IResource account1 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account1" );
            IResource account2 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account2" );
            IResource account3 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account3" );
            IResource account4 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account4" );
            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 4, contacts.Count );

            IResource email = _storage.NewResource( "email" );
            email.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, to1.Resource, email, account1, "B1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, to2.Resource, email, account2, "B2" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, to3.Resource, email, account3, "B3" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, extra.Resource, email, account4, "A" );

            IResource attach = _storage.NewResource( "email" );
            attach.SetProp( "Name", "AttachmentResource" );
            attach.SetProp( _contactManager.Props.LinkTo, to1.Resource );
            attach.SetProp( _contactManager.Props.LinkTo, to2.Resource );
            attach.SetProp( _contactManager.Props.LinkTo, to3.Resource );
            attach.SetProp( _contactManager.Props.LinkFrom, extra.Resource );
            attach.SetProp( "Attachment", email );

            //-----------------------------------------------------------------
            _contactManager.HardRemoveAccountFromContact( to1.Resource, account1 );
            //-----------------------------------------------------------------

            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 5, contacts.Count );

            contacts = email.GetLinksOfType( "Contact", _contactManager.Props.LinkTo );
            Assert.IsTrue( contacts.IndexOf( to1.Resource.Id ) == -1 );
            contacts = attach.GetLinksOfType( "Contact", _contactManager.Props.LinkTo );
            Assert.IsTrue( contacts.IndexOf( to1.Resource.Id ) == -1 );
        }

        //  Config: 1 mail, 2 contacts, 2 accounts.
        //  Action: Unlink account from the first contact;
        //  Expected: new contact is created, mail is relinked to it using both
        //            links - From and CC.
        [Test] public void UnlinkContactAndAccountTestContactIsFromAndCC()
        {
            IContact from  = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            IContact to  = _contactManager.FindOrCreateContact( "account2", "F1 L1" );
            IResource account1 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account1" );
            IResource account2 = Core.ResourceStore.FindUniqueResource( "EmailAccount", _contactManager.Props.EmailAddress, "account2" );

            IResource email = _storage.NewResource( "email" );
            email.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, from.Resource, email, account1, "B1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, to.Resource, email, account2, "B2" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkCC, from.Resource, email, account1, "B1" );
            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 2, contacts.Count );

            //-----------------------------------------------------------------
            _contactManager.HardRemoveAccountFromContact( from.Resource, account1 );
            //-----------------------------------------------------------------

            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 4, contacts.Count );

            IResourceList fromContacts = email.GetLinksOfType( "Contact", _contactManager.Props.LinkFrom );
            Assert.AreEqual( 1, fromContacts.Count );
            Assert.IsFalse( fromContacts[ 0 ].Id == from.Resource.Id );

            IResourceList ccContacts = email.GetLinksOfType( "Contact", _contactManager.Props.LinkCC );
            Assert.AreEqual( 1, ccContacts.Count );
            Assert.IsFalse( ccContacts[ 0 ].Id == from.Resource.Id );

//            Assert.AreEqual( ccContacts[ 0 ].Id, fromContacts[ 0 ].Id );
        }

        [Test] public void UnlinkResourceFromContactNames1()
        {
            IContact main = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            IResourceList accounts = main.Resource.GetLinksOfType( null, "EmailAcct" );

            //---
            IResource email = _storage.NewResource( "email" );
            email.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email, accounts[ 0 ], "A" );

            //-----------------------------------------------------------------
            _contactManager.UnlinkContactInformation( email );
            //-----------------------------------------------------------------
            Assert.AreEqual( 0, email.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkNameFrom ).Count );
            Assert.AreEqual( 0, main.Resource.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact ).Count );
        }

        [Test] public void UnlinkResourceFromContactNames2()
        {
            IContact main = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            IResourceList accounts = main.Resource.GetLinksOfType( null, "EmailAcct" );

            //---
            IResource email1 = _storage.NewResource( "email" );
            email1.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email1, accounts[ 0 ], "A" );

            IResource email2 = _storage.NewResource( "email" );
            email2.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email2, accounts[ 0 ], "A" );

            //-----------------------------------------------------------------
            _contactManager.UnlinkContactInformation( email1 );
            //-----------------------------------------------------------------
            Assert.AreEqual( 1, email1.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkNameFrom ).Count );
            Assert.AreEqual( 1, main.Resource.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact ).Count );
        }
    }
}
