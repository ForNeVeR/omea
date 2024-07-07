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
    public class ContactBOTests
    {
        private IContactManager _contactManager;
        private TestCore _core;
        private IResourceStore _storage;

        [SetUp]
        public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;
            _storage.PropTypes.Register( "Subject", PropDataType.String );
            _storage.ResourceTypes.Register( "Email", "Subject" );

            _contactManager = _core.ContactManager;
            _storage.PropTypes.Register( "LinkedSetValue", PropDataType.Link, PropTypeFlags.Internal );
        }

        [TearDown]
        public void TearDown()
        {
            _core.Dispose();
        }

        [Test] public void CheckNormilizedPhoneComparing()
        {
            string email = "zhu@jetBrains.com";
            ContactBO contact = (ContactBO) Core.ContactManager.FindOrCreateContact( email, "Sergey Zhulin"  );
            contact.SetPhoneNumber( "Home", " 123-78-90" );
            IResource phone = contact.GetPhoneByNumber( "_1.2.3.7.8.9.0" );
            Assert.IsNotNull( phone );
            Assert.AreEqual( " 123-78-90", phone.GetStringProp( "PhoneNumber" ) );
        }

        [Test] public void CreateContactBOTest()
        {
            string email = "zhu@jetBrains.com";
            IContact contact = Core.ContactManager.FindOrCreateContact( email, "Sergey Zhulin"  );
            Assert.AreEqual( "Sergey", contact.FirstName );
            Assert.AreEqual( "Zhulin", contact.LastName );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateContactBOWithNULLsTest()
        {
            Core.ContactManager.FindOrCreateContact( null, null );
        }

        [Test] public void CreateContactBOWithoutEmailTest()
        {
            IContact contact = _contactManager.FindOrCreateContact( null, "Sergey Zhulin"  );
            Assert.AreEqual( "Sergey", contact.FirstName );
            Assert.AreEqual( "Zhulin", contact.LastName );
        }

        [Test] public void ChangingContactBOTest()
        {
            string email = "zhu@jetBrains.com";
            IContact contact = _contactManager.FindOrCreateContact( email, "Sergey Zhulin"  );
//            contact.Save();
//            Assert.AreEqual( false, contact.Changed );
            contact.FirstName = "Misha";
//            Assert.AreEqual( true, contact.Changed );
//            contact.Save();
//            Assert.AreEqual( false, contact.Changed );
            contact = _contactManager.FindOrCreateContact( email, "Misha Zhulin"  );
            Assert.AreEqual( "Misha", contact.FirstName );
        }
        [Test] public void ContactBOAllPropertiesTest()
        {
            string email = "zhu@jetBrains.com";
            IContact contact = _contactManager.FindOrCreateContact( email, "Sergey Zhulin"  );
            contact.Company = "jetBrains";
            Assert.AreEqual( "jetBrains", contact.Company );
            contact.HomePage = "www.jetBrains.com";
            Assert.AreEqual( "www.jetBrains.com", contact.HomePage );
            contact.JobTitle = "developer";
            Assert.AreEqual( "developer", contact.JobTitle );
            contact.Address = "sertolovo";
            Assert.AreEqual( "sertolovo", contact.Address );
        }

        [Test]
        public void RequireUniqueContactName()
        {
            string emailAddress1 = "zhu@intellij.com";
            string emailAddress2 = "zhu@jetBrains.com";

            //-----------------------------------------------------------------
            //  create contacts
            //-----------------------------------------------------------------
            IContact contact1 = _contactManager.FindOrCreateContact( emailAddress1, "Sergey Zhulin" );
            IContact contact2 = _contactManager.FindOrCreateContact( emailAddress2, "Sergey Zhulin" );
            Assert.AreEqual( contact1.FirstName, contact2.FirstName );
            Assert.AreEqual( contact1.LastName, contact2.LastName );

            contact1.Resource.SetProp( "ShowOriginalNames", true );

            //-----------------------------------------------------------------
            //  Check links of contacts with their corresponding accounts
            //-----------------------------------------------------------------
            Assert.AreEqual( contact1.Resource.GetLinkCount( _contactManager.Props.LinkEmailAcct ), 2 );
            IResourceList accounts = contact1.Resource.GetLinksOfType( "EmailAccount", _contactManager.Props.LinkEmailAcct );

            IResource emailAccount1 = accounts[ 0 ];
            IResource emailAccount2 = accounts[ 1 ];
            Assert.IsTrue( emailAddress1 == emailAccount1.GetPropText("EmailAddress") || emailAddress2 == emailAccount1.GetPropText("EmailAddress") );
            Assert.IsTrue( emailAddress1 == emailAccount2.GetPropText("EmailAddress") || emailAddress2 == emailAccount2.GetPropText("EmailAddress") );

            //-----------------------------------------------------------------
            //  Create new mails connected to the united contact
            //-----------------------------------------------------------------
            IResource newEmail1 = _storage.NewResource( "email" );
            newEmail1.SetProp( "Subject", "Subject3" );

            IResource newEmail2 = _storage.NewResource( "email" );
            newEmail2.SetProp( "Subject", "Subject3" );

            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, contact1.Resource, newEmail1, emailAccount1, "sender1" );
            Assert.AreEqual( 1, Core.ResourceStore.GetAllResources( "ContactName" ).Count );

            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, contact1.Resource, newEmail2, emailAccount1, "sender1" );
            Assert.AreEqual( 1, Core.ResourceStore.GetAllResources( "ContactName" ).Count );

            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkTo, contact1.Resource, newEmail2, emailAccount2, "sender1" );
            Assert.AreEqual( 2, Core.ResourceStore.GetAllResources( "ContactName" ).Count );
        }

        [Test] public void ApostrophRemovingTest()
        {
            string displayName = "'Sergey Zhulin'";
            IContact contact = _contactManager.FindOrCreateContact( null, displayName );
            Assert.AreEqual( "Sergey", contact.FirstName );
            Assert.AreEqual( "Zhulin", contact.LastName );
        }

        [Test] public void ApostrophRemovingTest2()
        {
            string displayName = "'Sergey Zhulin' (E-mail)";
            IContact contact = _contactManager.FindOrCreateContact( null, displayName );
            Assert.AreEqual( "Sergey", contact.FirstName );
            Assert.AreEqual( "Zhulin", contact.LastName );
        }

        [Test] public void ApostrophRemovingTest3()
        {
            string displayName = "'Sergey Zhulin (E-mail)'";
            IContact contact = _contactManager.FindOrCreateContact( null, displayName );
            Assert.AreEqual( "Sergey", contact.FirstName );
            Assert.AreEqual( "Zhulin", contact.LastName );
        }

        [Test] public void ApostrophRemovingFromAccount()
        {
            string email = "'amy.branton@message.com'";
            string displayName = "'amy.branton@message.com'";
            IContact contact = _contactManager.FindOrCreateContact( email, displayName );
            Assert.AreEqual( "amy", contact.FirstName );
            Assert.AreEqual( "branton", contact.LastName );
        }

        [Test] public void CreationContactBodyTest()
        {
            string displayName = "'Sergey Zhulin'";
            ContactBO contact = (ContactBO) _contactManager.FindOrCreateContact( null, displayName );
            contact.Birthday = new DateTime( 1972, 07, 26 );
            contact.Company = "JetBrains";
            contact.HomePage = "www.jetbrains.ru";
            contact.JobTitle = "Developer";
            contact.Address = "Sertolovo";
            Assert.AreEqual( "Sergey Zhulin Sertolovo JetBrains Developer www.jetbrains.ru", contact.ContactBody);
        }
        [Test] public void CreationContactBodyTest1()
        {
            string displayName = "'Sergey Zhulin'";
            ContactBO contact = (ContactBO) _contactManager.FindOrCreateContact( null, displayName );
            Assert.AreEqual( false, contact.Changed );

            contact.Birthday = new DateTime( 1972, 07, 26 );
            contact.Company = "JetBrains";
            contact.HomePage = "www.jetbrains.ru";
            contact.JobTitle = "Developer";
            contact.Address = "Sertolovo";
            Assert.AreEqual( true, contact.Changed );
//            contact.Save();

            contact = (ContactBO) _contactManager.FindOrCreateContact( null, displayName );
            Assert.AreEqual( false, contact.Changed );
            Assert.AreEqual( "Sergey Zhulin Sertolovo JetBrains Developer www.jetbrains.ru", contact.ContactBody );
        }
        [Test] public void CreationContactBodyTest2()
        {
            string displayName = "'Sergey Zhulin'";
            ContactBO contact = (ContactBO) _contactManager.FindOrCreateContact( null, displayName );
            Assert.AreEqual( false, contact.Changed );

            int id = contact.Resource.Id;
            contact.Birthday = new DateTime( 1972, 07, 26 );
            contact.Company = "JetBrains";
            contact.HomePage = "www.jetbrains.ru";
            contact.JobTitle = "Developer";
            contact.Address = "Sertolovo";
            Assert.AreEqual( true, contact.Changed );
//            contact.Save();
            Assert.AreEqual( id, contact.ID );

            contact = (ContactBO) _contactManager.FindOrCreateContact( null, displayName );
            Assert.AreEqual( false, contact.Changed );
            Assert.AreEqual( "Sergey Zhulin Sertolovo JetBrains Developer www.jetbrains.ru", contact.ContactBody );
            Assert.AreEqual( id, contact.ID );
        }

        [Test] public void CreationContactBodyTest3()
        {
            string displayName = "'Sergey Zhulin'";
            ContactBO contact = (ContactBO) _contactManager.FindOrCreateContact( null, displayName );
            Assert.AreEqual( false, contact.Changed );

            int id = contact.Resource.Id;
            contact.Birthday = new DateTime( 1972, 07, 26 );
            contact.Company = "JetBrains";
            contact.HomePage = "www.jetbrains.ru";
            contact.JobTitle = "Developer";
            contact.Address = "Sertolovo";

            IResource contactName = Core.ResourceStore.BeginNewResource( "ContactName" );
            contactName.SetProp( "Name", "Sergeyy Zhulinn");
            contactName.SetProp( Core.ContactManager.Props.LinkBaseContact, contact.Resource );
            contactName.EndUpdate();

            IResource contactName2 = Core.ResourceStore.BeginNewResource( "ContactName" );
            contactName2.SetProp( "Name", "Sergeyyy Zhulinnn");
            contactName2.SetProp( Core.ContactManager.Props.LinkBaseContact, contact.Resource );
            contactName2.EndUpdate();

            contact = (ContactBO) _contactManager.FindOrCreateContact( null, displayName );
            Assert.AreEqual( false, contact.Changed );
            Assert.AreEqual( "Sergey Zhulin Sertolovo JetBrains Developer www.jetbrains.ru |Sergeyy Zhulinn Sergeyyy Zhulinnn |", contact.ContactBody );
            Assert.AreEqual( id, contact.ID );
        }

        [Test] public void FindExactContact()
        {
            _contactManager.FindOrCreateContact( null, "FirstName LastName" );
            Assert.IsNotNull( _contactManager.FindContact( null, "FirstName", null, "LastName", null ));
        }
        [Test] public void FindNoUnderspecifiedContact()
        {
            _contactManager.FindOrCreateContact( null, "FirstName MiddleName LastName" );
            Assert.IsNull( _contactManager.FindContact( null, "FirstName", "MiddleName", "LastName", "Jr" ));
        }
        [Test] public void FindFullySpecifiedContact()
        {
            _contactManager.FindOrCreateContact( null, "Mr FirstName MiddleName LastName Sr" );
            Assert.IsNotNull( _contactManager.FindContact( "Mr", "FirstName", "MiddleName", "LastName", "Sr" ));
        }
        [Test] public void FindMinimallySpecifiedAmbiguousContact()
        {
            _contactManager.FindOrCreateContact( null, "FirstName LastName Sr" );
            _contactManager.FindOrCreateContact( "Mr", "FirstName MiddleName LastName" );
            IContact result = _contactManager.FindContact( null, "FirstName", null, "LastName", null );
            Assert.IsNotNull( result );
            Assert.IsTrue( result.MiddleName == string.Empty, "Proper contact found" );
        }
        [Test] public void FindOrCreateContactTwice()
        {
            IContact c1 = _contactManager.FindOrCreateContact( null, "FirstName LastName" );
            IContact c2 = _contactManager.FindOrCreateContact( null, "FirstName LastName" );
            Assert.AreEqual( c1.Resource.Id, c2.Resource.Id );
        }
        [Test] public void FindOrCreateSimpleContactTwice()
        {
            IContact c1 = _contactManager.FindOrCreateContact( null, "Brion" );
            IContact c2 = _contactManager.FindOrCreateContact( null, "Brion" );
            Assert.AreEqual( c1.Resource.Id, c2.Resource.Id );
        }
        [Test] public void FindContactTestNullFirstName()
        {
            _contactManager.FindOrCreateContact( null, "FirstName MiddleName LastName" );
            Assert.IsNull( _contactManager.FindContact( null, null, "MiddleName", "LastName", null ));
        }
        [Test] public void FindContactTestNullLastName()
        {
            _contactManager.FindOrCreateContact( null, "FirstName MiddleName LastName" );
            Assert.IsNull( _contactManager.FindContact( null, "FirstName", "MiddleName", null, null ));
        }
        [Test] public void FindContactTestNullBothNames()
        {
            _contactManager.FindOrCreateContact( null, "FirstName MiddleName LastName" );
            Assert.IsNull( _contactManager.FindContact( null, null, "MiddleName", null, null ));
        }
        [Test] public void FindContactTestPartialName()
        {
            IContact c1 = _contactManager.FindOrCreateContact( null, "FirstName MiddleName LastName" );
            IContact c2 = _contactManager.FindOrCreateContact( null, "LastName" );
            Assert.IsTrue( c1.Resource.Id != c2.Resource.Id );
        }
        [Test] public void FindContactTestAlmostEqualsWithNullAccountSearch()
        {
            IContact c1 = _contactManager.FindOrCreateContact( "account1", "FirstName LastName" );
            IContact c2 = _contactManager.CreateContact( "FirstName LastName (JIRA)" );
            Assert.IsTrue( c1.Resource.Id != c2.Resource.Id );

            IContact c = _contactManager.FindOrCreateContact( null, "FirstName LastName" );
            Assert.IsTrue( c.Resource.Id == c1.Resource.Id );

            c = _contactManager.FindOrCreateContact( "account1", "FirstName LastName" );
            Assert.IsTrue( c.Resource.Id == c1.Resource.Id );

            //-----------------------------------------------------------------
            IResource account = _contactManager.FindOrCreateEmailAccount( "account2" );
            account.SetProp( _contactManager.Props.LinkEmailAcct, c2.Resource );

            c = _contactManager.FindOrCreateContact( "account2", "FirstName LastName" );
            Assert.IsTrue( c.Resource.Id == c2.Resource.Id );
        }
        [Test] public void FindContactTestAbsentNames()
        {
            _contactManager.FindOrCreateContact( null, "LastName" );
            Assert.IsNull( _contactManager.FindContact( null, "FirstName", null, "LastName", null ));
        }

        [Test] public void CreateMyselfFindItUsingOtherNameButSamePersonalAccount()
        {
            IResource account = _contactManager.FindOrCreateEmailAccount( "lloix@intellij.com" );
            account.SetProp( _contactManager.Props.PersonalAccount, true );
            IContact myself = _contactManager.FindOrCreateMySelfContact( "lloix@intellij.com", null, "Michael", null, "Gerasimov", null );
            IContact otherMyself = _contactManager.FindOrCreateContact( "lloix@intellij.com", "LloiX" );
            Assert.AreEqual( myself.Resource.Id, otherMyself.Resource.Id );
        }

        [Test] public void CreateMyselfDontFindItUsingOtherNameButSameNonpersonalAccount()
        {
            IContact myself = _contactManager.FindOrCreateMySelfContact( "lloix@intellij.com", null, "Michael", null, "Gerasimov", null );
            IContact otherMyself = _contactManager.FindOrCreateContact( "lloix@intellij.com", "LloiX" );
            Assert.IsFalse( myself.Resource.Id == otherMyself.Resource.Id );
        }

/*
        [Test] public void FindOrCreateMySelfContactByFieldsThenByRicherSenderName()
        {
            _contactManager.FindOrCreateMySelfContact( null, null, "LloiX", null, null, null );
            IContact c = _contactManager.FindOrCreateMySelfContact( null, "Michael Gerasimov" );
            Assert.AreEqual( c.FirstName, "Michael" );
            Assert.AreEqual( c.LastName, "Gerasimov" );
        }
*/
        [Test] public void FindOrCreateMySelfContactByFieldsThenByPoorerSenderName()
        {
            _contactManager.FindOrCreateMySelfContact( null, null, "Michael", null, "Gerasimov", null );
            IContact c = _contactManager.FindOrCreateMySelfContact( null, "LloiX" );
            Assert.AreEqual( c.FirstName, "Michael" );
            Assert.AreEqual( c.LastName, "Gerasimov" );
        }

        //  Config: one mail, two contacts are linked by non-exclusive link
        //          (by CC or To).
        //  Expected: Two links between a mail and email accounts.
        [Test] public void LinkMailWithNonexclusiveLinksTwice()
        {
            IContact main = _contactManager.FindOrCreateContact( "account1", "Michael Gerasimov" );
            IContact extra = _contactManager.FindOrCreateContact( "account2", "FirstName LastName" );
            IResource account1 = main.Resource.GetLinkProp( "EmailAcct" );
            IResource account2 = extra.Resource.GetLinkProp( "EmailAcct" );

            IResource email = _storage.NewResource( "email" );
            email.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkCC, main.Resource, email, account1, "aFrom" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkCC, extra.Resource, email, account2, "bTo" );

            //-----------------------------------------------------------------
            Assert.AreEqual( 2, email.GetLinksOfType( null, Core.ContactManager.Props.LinkEmailAcctCC ).Count );
        }

        [Test] public void DontDeleteMailAfterGoodAddressCame()
        {
            //  1. Create empty contact and some account, and link them to one mail.
            //  2. FindOrCreate new contact with non-empty name, contact must be
            //     linked to the same account.
            //  3. Empty contact must be removed, and all mails must be retargeted
            //     to the new contact.
            IContact main = _contactManager.FindOrCreateContact( "foo@bar.com", null );
            IResource email = _storage.NewResource( "email" );
            email.SetProp( "Subject", "Subject1" );
            Core.ContactManager.LinkContactToResource( _contactManager.Props.LinkFrom, main.Resource, email, "foo@bar.com", null );

            IContact newContact = _contactManager.FindOrCreateContact( "foo@bar.com", "Anonymous User" );

            IResourceList linkedMails = newContact.Resource.GetLinksOfType( "email", _contactManager.Props.LinkFrom );
            Assert.AreEqual( linkedMails.Count, 1 );
        }
    }
}
