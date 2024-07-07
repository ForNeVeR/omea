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
    public class ContactMergeSplitTests
    {
        private TestCore _core;
        private IResourceStore _storage;
        private IContactManager _contactMgr;

        [SetUp]
        public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;

            _storage.PropTypes.Register( "Subject", PropDataType.String );
            _storage.ResourceTypes.Register( "Email", "Subject" );

            _contactMgr = _core.ContactManager;
            _storage.PropTypes.Register( "LinkedSetValue", PropDataType.Link, PropTypeFlags.Internal );
        }

        [TearDown]
        public void TearDown()
        {
            _core.Dispose();
        }

        //  Test catenation of different variants of textual fields.
        [Test] public void MergeStaticDataTest()
        {
            IResource mergedRes;
            IContact  contact1, contact2, mergedContact;
            //  Case 1. Both contact have content, one content is
            //          different from another.
            contact1 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin First"  );
            contact2 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Second"  );
            contact1.Address = "Mask1";
            contact2.Address = "Premier";
            mergedRes = _contactMgr.Merge( "Sergey Zhulin First", contact1.Resource.ToResourceList().Union( contact2.Resource.ToResourceList() ));
            mergedContact = new ContactBO( mergedRes );
            Assert.AreEqual( "Mask1; Premier", mergedContact.Address );

            //  Case 2. Both contact have content, one content is
            //          equal to another.
            contact1 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Fourth"  );
            contact2 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Fifth"  );
            contact1.Address = "Premier";
            contact2.Address = "Premier";
            mergedRes = _contactMgr.Merge( "Sergey Zhulin Fourth", contact1.Resource.ToResourceList().Union( contact2.Resource.ToResourceList() ));
            mergedContact = new ContactBO( mergedRes );
            Assert.AreEqual( "Premier", mergedContact.Address );

            //  Case 3. One contact have content, other does not
            contact1 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Sixth"  );
            contact2 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Seventh"  );
            contact1.Address = "Premier";
            mergedRes = _contactMgr.Merge( "Sergey Zhulin Sixth", contact1.Resource.ToResourceList().Union( contact2.Resource.ToResourceList() ));
            mergedContact = new ContactBO( mergedRes );
            Assert.AreEqual( "Premier", mergedContact.Address );

            //  Case 4. Both contacts have no content.
            contact1 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Ten"  );
            contact2 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Eleven"  );
            mergedRes = _contactMgr.Merge( "Sergey Zhulin Ten", contact1.Resource.ToResourceList().Union( contact2.Resource.ToResourceList() ));
            mergedContact = new ContactBO( mergedRes );
            Assert.AreEqual( "", mergedContact.Address );

            //  Repeat the same tests for Description, since getting/setting it
            //  already does not use chaching.
            //  Case 1. Both contact have content, one content is
            //          different from another.
            contact1 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin First"  );
            contact2 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Second"  );
            contact1.Description = "Mask1";
            contact2.Description = "Premier";
            mergedRes = _contactMgr.Merge( "Sergey Zhulin First", contact1.Resource.ToResourceList().Union( contact2.Resource.ToResourceList() ));
            mergedContact = new ContactBO( mergedRes );
            Assert.AreEqual( "Mask1; Premier", mergedContact.Description );

            //  Case 2. Both contact have content, one content is
            //          equal to another.
            contact1 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Fourth"  );
            contact2 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Fifth"  );
            contact1.Description = "Premier";
            contact2.Description = "Premier";
            mergedRes = _contactMgr.Merge( "Sergey Zhulin Fourth", contact1.Resource.ToResourceList().Union( contact2.Resource.ToResourceList() ));
            mergedContact = new ContactBO( mergedRes );
            Assert.AreEqual( "Premier", mergedContact.Description );

            //  Case 3. One contact have content, other does not
            contact1 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Sixth"  );
            contact2 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Seventh"  );
            contact1.Description = "Premier";
            mergedRes = _contactMgr.Merge( "Sergey Zhulin Sixth", contact1.Resource.ToResourceList().Union( contact2.Resource.ToResourceList() ));
            mergedContact = new ContactBO( mergedRes );
            Assert.AreEqual( "Premier", mergedContact.Description );

            //  Case 4. Both contacts have no content.
            contact1 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Ten"  );
            contact2 = _contactMgr.FindOrCreateContact( null, "Sergey Zhulin Eleven"  );
            mergedRes = _contactMgr.Merge( "Sergey Zhulin Ten", contact1.Resource.ToResourceList().Union( contact2.Resource.ToResourceList() ));
            mergedContact = new ContactBO( mergedRes );
            Assert.AreEqual( "", mergedContact.Description );
        }

        [Test]
        public void ContactsMerge_Test( )
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, "Sergey Zhulin" );
            IResource targetContact = contact1.Resource;
            Assert.AreEqual( "Sergey", contact1.FirstName );
            Assert.AreEqual( "Zhulin", contact1.LastName );

            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, "Serg Zhulin" );
            IResource sourceContact = contact2.Resource;
            Assert.AreEqual( "Serg", contact2.FirstName );
            Assert.AreEqual( "Zhulin", contact2.LastName );

            IResource emailAccount1 = targetContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( targetEmailAddress, emailAccount1.GetPropText("EmailAddress") );
            IResource targetEmail = _storage.NewResource( "email" );
            targetEmail.SetProp( "Subject", "Subject1" );
            targetEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            targetEmail.AddLink( _contactMgr.Props.LinkFrom, targetContact );
            targetEmail.AddLink( _contactMgr.Props.LinkTo, targetContact );
            IResourceList mails = targetContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );

            IResourceList emailAccounts = targetContact.GetLinksOfType( "EmailAccount", "EmailAcct" );
            Assert.AreEqual( 1, emailAccounts.Count );

            IResource emailAccount2 = sourceContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( sourceEmailAddress, emailAccount2.GetPropText("EmailAddress") );
            emailAccounts = sourceContact.GetLinksOfType( "EmailAccount", "EmailAcct" );
            Assert.AreEqual( 1, emailAccounts.Count );

            IResource sourceEmail = _storage.NewResource( "email" );
            sourceEmail.SetProp( "Subject", "Subject2" );
            sourceEmail.AddLink( Core.ContactManager.Props.LinkEmailAcctFrom, emailAccount2 );
            sourceEmail.AddLink( _contactMgr.Props.LinkFrom, sourceContact );
            sourceEmail.AddLink( _contactMgr.Props.LinkTo, sourceContact );
            mails = sourceContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );

            _contactMgr.Merge( "Serge Zhulin",
                                 targetContact.ToResourceList().Union( sourceContact.ToResourceList() ) );
            IResourceList contacts = _storage.GetAllResources( "Contact"  );
            Assert.AreEqual( 1, contacts.Count );
        }

        [Test]
        public void TestContactsMergeWithCN( )
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, "Sergey Zhulin" );
            IResource targetContact = contact1.Resource;
            Assert.AreEqual( "Sergey", contact1.FirstName );
            Assert.AreEqual( "Zhulin", contact1.LastName );
            contact1.Resource.SetProp( "ShowOriginalNames", true );

            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, "Serg Zhulin" );
            IResource sourceContact = contact2.Resource;
            Assert.AreEqual( "Serg", contact2.FirstName );
            Assert.AreEqual( "Zhulin", contact2.LastName );
            contact2.Resource.SetProp( "ShowOriginalNames", true );

            IResource emailAccount1 = targetContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( targetEmailAddress, emailAccount1.GetPropText("EmailAddress") );
            IResource targetEmail = _storage.NewResource( "email" );
            targetEmail.SetProp( "Subject", "Subject1" );
            targetEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            targetEmail.AddLink( _contactMgr.Props.LinkFrom, targetContact );
            targetEmail.AddLink( _contactMgr.Props.LinkTo, targetContact );
            IResourceList mails = targetContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );

            IResourceList emailAccounts = targetContact.GetLinksOfType( "EmailAccount", "EmailAcct" );
            Assert.AreEqual( 1, emailAccounts.Count );

            IResource emailAccount2 = sourceContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( sourceEmailAddress, emailAccount2.GetPropText("EmailAddress") );
            emailAccounts = sourceContact.GetLinksOfType( "EmailAccount", "EmailAcct" );
            Assert.AreEqual( 1, emailAccounts.Count );

            IResource sourceEmail = _storage.NewResource( "email" );
            sourceEmail.SetProp( "Subject", "Subject2" );
            sourceEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount2 );
            sourceEmail.AddLink( _contactMgr.Props.LinkFrom, sourceContact );
            sourceEmail.AddLink( _contactMgr.Props.LinkTo, sourceContact );
            mails = sourceContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );

            _contactMgr.Merge( "Serge Zhulin",
                                 targetContact.ToResourceList().Union( sourceContact.ToResourceList() ) );
            IResourceList contacts = _storage.GetAllResources( "Contact"  );
            Assert.AreEqual( 1, contacts.Count );
        }

        //  One contact has ContactName, second - not; two mails.
        //  Expected results: two contact names, one is new, one from the
        //                    older contact.
        [Test]
        public void TestContactsMergeWithAsymmetricCNames()
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";

            //--  Contacts and Accounts  --------------------------------------
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, "Sergey Zhulin" );
            IResource targetContact = contact1.Resource;
            IResource emailAccount1 = targetContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( targetEmailAddress, emailAccount1.GetPropText( "EmailAddress" ) );
            contact1.Resource.SetProp( "ShowOriginalNames", true );

            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, "Serg Zhulin" );
            IResource sourceContact = contact2.Resource;
            IResource emailAccount2 = sourceContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( sourceEmailAddress, emailAccount2.GetPropText("EmailAddress") );
            contact2.Resource.SetProp( "ShowOriginalNames", true );

            IContact aux = _contactMgr.FindOrCreateContact( "aux@com", "Mediator contact" );

            //--  Mails  ------------------------------------------------------
            IResource targetEmail = _storage.NewResource( "email" );
            targetEmail.SetProp( "Subject", "Subject1" );
            targetEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            targetEmail.AddLink( _contactMgr.Props.LinkFrom, targetContact );
            targetEmail.AddLink( _contactMgr.Props.LinkTo, aux.Resource );

            IResource sourceEmail = _storage.NewResource( "email" );
            sourceEmail.SetProp( "Subject", "Subject2" );
            sourceEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount2 );
            sourceEmail.AddLink( _contactMgr.Props.LinkFrom, sourceContact );
            sourceEmail.AddLink( _contactMgr.Props.LinkTo, aux.Resource );

            IResource cName = _storage.BeginNewResource( "ContactName" );
            cName.SetProp( Core.ContactManager.Props.LinkBaseContact, targetContact );
            cName.SetProp( _contactMgr.Props.LinkEmailAcct, emailAccount1 );
            targetEmail.SetProp( Core.ContactManager.Props.LinkNameFrom, cName );
            cName.EndUpdate();

            //-----------------------------------------------------------------
            _contactMgr.Merge( "Serge Zhulin", targetContact.ToResourceList().Union( sourceContact.ToResourceList() ) );
            //-----------------------------------------------------------------

            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 2, contacts.Count ); // including the mediator one.

            IResourceList names = targetEmail.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkNameFrom );
            Assert.AreEqual( 1, names.Count );
            names = sourceEmail.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkNameFrom );
            Assert.AreEqual( 1, names.Count );
        }

        [Test]
        public void ContactsMergeAndSplit()
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";

            //  create contacts
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, "Sergey Zhulin" );
            IResource targetContact = contact1.Resource;
            Assert.AreEqual( "Sergey", contact1.FirstName );
            Assert.AreEqual( "Zhulin", contact1.LastName );

            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, "Serg Zhulin" );
            IResource sourceContact = contact2.Resource;
            Assert.AreEqual( "Serg", contact2.FirstName );
            Assert.AreEqual( "Zhulin", contact2.LastName );

            IContact contact3 = _contactMgr.FindOrCreateContact( "1@1.1", "Mediator Contact" );
            IResource mediatorContact = contact3.Resource;
            Assert.AreEqual( "Mediator", contact3.FirstName );
            Assert.AreEqual( "Contact", contact3.LastName );

            //  link contacts to their corresponding accounts
            IResource emailAccount1 = targetContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( targetEmailAddress, emailAccount1.GetPropText("EmailAddress") );
            IResourceList emailAccounts = targetContact.GetLinksOfType( "EmailAccount", "EmailAcct" );
            Assert.AreEqual( 1, emailAccounts.Count );

            IResource emailAccount2 = sourceContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( sourceEmailAddress, emailAccount2.GetPropText("EmailAddress") );
            emailAccounts = sourceContact.GetLinksOfType( "EmailAccount", "EmailAcct" );
            Assert.AreEqual( 1, emailAccounts.Count );

            //  link mails to the accounts and correspondents
            IResource targetEmail = _storage.NewResource( "email" );
            targetEmail.SetProp( "Subject", "Subject1" );
            targetEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            targetEmail.AddLink( _contactMgr.Props.LinkFrom, targetContact );
            targetEmail.AddLink( _contactMgr.Props.LinkTo, mediatorContact );
            IResourceList mails = targetContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );
            mails = mediatorContact.GetLinksOfType( "Email", "To" );
            Assert.AreEqual( 1, mails.Count );

            IResource sourceEmail = _storage.NewResource( "email" );
            sourceEmail.SetProp( "Subject", "Subject2" );
            sourceEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount2 );
            sourceEmail.AddLink( _contactMgr.Props.LinkFrom, sourceContact );
            sourceEmail.AddLink( _contactMgr.Props.LinkTo, mediatorContact );
            mails = sourceContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );
            mails = mediatorContact.GetLinksOfType( "Email", "To" );
            Assert.AreEqual( 2, mails.Count );

            //-----------------------------------------------------------------
            IResourceList list = targetContact.ToResourceList().Union( sourceContact.ToResourceList() );
            IResource newContact = _contactMgr.Merge( "Serge Zhulin", list );
            IResourceList contacts = _storage.GetAllResources( "Contact"  );
            Assert.AreEqual( 2, contacts.Count );

            int linksCount = newContact.GetLinksOfType( null, "From" ).Count;
            Assert.AreEqual( 2, linksCount );

            //-----------------------------------------------------------------
            _contactMgr.Split( newContact );
            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( contacts.Count, 3 );
            contacts = contacts.Minus( mediatorContact.ToResourceList() );

            IResource contact = contacts[ 0 ];
            Assert.AreEqual( contact.GetLinksOfType( null, "From" ).Count, 1 );
            contact = contacts[ 1 ];
            Assert.AreEqual( contact.GetLinksOfType( null, "From" ).Count, 1 );
        }

        //  Test the possibility to merge contacts which have "horizontal links"
        //  between them.
        [Test]
        public void ContactsMergeWithDirectedLinkBetweenContactsOrder1()
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";

            //  create contacts
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, "Sergey Zhulin" );
            IResource targetContact = contact1.Resource;
            Assert.AreEqual( "Sergey", contact1.FirstName );
            Assert.AreEqual( "Zhulin", contact1.LastName );

            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, "Serg Zhulin" );
            IResource sourceContact = contact2.Resource;
            Assert.AreEqual( "Serg", contact2.FirstName );
            Assert.AreEqual( "Zhulin", contact2.LastName );

            int linkId = Core.ResourceStore.PropTypes.Register( "Link To", PropDataType.Link, PropTypeFlags.DirectedLink );
            targetContact.SetProp( linkId, sourceContact );

            //-----------------------------------------------------------------
            IResourceList list = targetContact.ToResourceList().Union( sourceContact.ToResourceList() );
            _contactMgr.Merge( "Serge Zhulin", list );
            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 1, contacts.Count );
        }

        //  Test the possibility to merge contacts which have "horizontal links"
        //  between them.
        [Test]
        public void ContactsMergeWithDirectedLinkBetweenContactsOrder2()
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";

            //  create contacts
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, "Sergey Zhulin" );
            IResource targetContact = contact1.Resource;
            Assert.AreEqual( "Sergey", contact1.FirstName );
            Assert.AreEqual( "Zhulin", contact1.LastName );

            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, "Serg Zhulin" );
            IResource sourceContact = contact2.Resource;
            Assert.AreEqual( "Serg", contact2.FirstName );
            Assert.AreEqual( "Zhulin", contact2.LastName );

            int linkId = Core.ResourceStore.PropTypes.Register( "Link To", PropDataType.Link, PropTypeFlags.DirectedLink );
            sourceContact.SetProp( linkId, targetContact );

            //-----------------------------------------------------------------
            IResourceList list = targetContact.ToResourceList().Union( sourceContact.ToResourceList() );
            _contactMgr.Merge( "Serge Zhulin", list );
            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 1, contacts.Count );
        }

        //  Test the possibility to merge contacts which have "horizontal links"
        //  between them.
        [Test]
        public void ContactsMergeWithUndirectedLinkBetweenContacts()
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";

            //  create contacts
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, "Sergey Zhulin" );
            IResource targetContact = contact1.Resource;
            Assert.AreEqual( "Sergey", contact1.FirstName );
            Assert.AreEqual( "Zhulin", contact1.LastName );

            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, "Serg Zhulin" );
            IResource sourceContact = contact2.Resource;
            Assert.AreEqual( "Serg", contact2.FirstName );
            Assert.AreEqual( "Zhulin", contact2.LastName );

            int linkId = Core.ResourceStore.PropTypes.Register( "Link To", PropDataType.Link );
            sourceContact.SetProp( linkId, targetContact );

            //-----------------------------------------------------------------
            IResourceList list = targetContact.ToResourceList().Union( sourceContact.ToResourceList() );
            _contactMgr.Merge( "Serge Zhulin", list );
            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 1, contacts.Count );
        }

        [Test]
        public void ContactsMergeAndSplitWithCN()
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";

            //  create contacts
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, "Sergey Zhulin" );
            IResource targetContact = contact1.Resource;
            Assert.AreEqual( "Sergey", contact1.FirstName );
            Assert.AreEqual( "Zhulin", contact1.LastName );
            targetContact.SetProp( "ShowOriginalNames", true );

            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, "Serg Zhulin" );
            IResource sourceContact = contact2.Resource;
            Assert.AreEqual( "Serg", contact2.FirstName );
            Assert.AreEqual( "Zhulin", contact2.LastName );
            sourceContact.SetProp( "ShowOriginalNames", true );

            IContact contact3 = _contactMgr.FindOrCreateContact( "1@1.1", "Mediator Contact" );
            IResource mediatorContact = contact3.Resource;
            Assert.AreEqual( "Mediator", contact3.FirstName );
            Assert.AreEqual( "Contact", contact3.LastName );

            //  link contacts to their corresponding accounts
            IResource emailAccount1 = targetContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( targetEmailAddress, emailAccount1.GetPropText("EmailAddress") );
            IResourceList emailAccounts = targetContact.GetLinksOfType( "EmailAccount", "EmailAcct" );
            Assert.AreEqual( 1, emailAccounts.Count );

            IResource emailAccount2 = sourceContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( sourceEmailAddress, emailAccount2.GetPropText("EmailAddress") );
            emailAccounts = sourceContact.GetLinksOfType( "EmailAccount", "EmailAcct" );
            Assert.AreEqual( 1, emailAccounts.Count );

            //  link mails to the accounts and correspondents
            IResource targetEmail = _storage.NewResource( "email" );
            targetEmail.SetProp( "Subject", "Subject1" );
            targetEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            targetEmail.AddLink( _contactMgr.Props.LinkFrom, targetContact );
            targetEmail.AddLink( _contactMgr.Props.LinkTo, mediatorContact );
            IResourceList mails = targetContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );
            mails = mediatorContact.GetLinksOfType( "Email", "To" );
            Assert.AreEqual( 1, mails.Count );

            IResource sourceEmail = _storage.NewResource( "email" );
            sourceEmail.SetProp( "Subject", "Subject2" );
            sourceEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount2 );
            sourceEmail.AddLink( _contactMgr.Props.LinkFrom, sourceContact );
            sourceEmail.AddLink( _contactMgr.Props.LinkTo, mediatorContact );
            mails = sourceContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );
            mails = mediatorContact.GetLinksOfType( "Email", "To" );
            Assert.AreEqual( 2, mails.Count );

            //-----------------------------------------------------------------
            IResourceList list = targetContact.ToResourceList().Union( sourceContact.ToResourceList() );
            IResource newContact = _contactMgr.Merge( "Serge Zhulin", list );
            IResourceList contacts = _storage.GetAllResources( "Contact"  );
            Assert.AreEqual( 2, contacts.Count );

            int linksCount = newContact.GetLinksOfType( null, "From" ).Count;
            Assert.AreEqual( 2, linksCount );

            //-----------------------------------------------------------------
            _contactMgr.Split( newContact );
            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( contacts.Count, 3 );
            contacts = contacts.Minus( mediatorContact.ToResourceList() );

            IResource contact = contacts[ 0 ];
            Assert.AreEqual( contact.GetLinksOfType( null, "From" ).Count, 1 );
            contact = contacts[ 1 ];
            Assert.AreEqual( contact.GetLinksOfType( null, "From" ).Count, 1 );
        }

        [Test]
        public void ContactsSplitWithReassignmentOfNewMail()
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";

            //-----------------------------------------------------------------
            //  create contacts
            //-----------------------------------------------------------------
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, "Sergey Zhulin" );
            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, "Serg Zhulin" );

            //-----------------------------------------------------------------
            //  link contacts to their corresponding accounts
            //-----------------------------------------------------------------
            IResource emailAccount1 = contact1.Resource.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( targetEmailAddress, emailAccount1.GetPropText("EmailAddress") );

            IResource emailAccount2 = contact2.Resource.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( sourceEmailAddress, emailAccount2.GetPropText("EmailAddress") );

            //-----------------------------------------------------------------
            //  create mails and link them to the accounts and contacts
            //-----------------------------------------------------------------
            IResource targetEmail = _storage.NewResource( "email" );
            targetEmail.SetProp( "Subject", "Subject1" );
            targetEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            targetEmail.AddLink( _contactMgr.Props.LinkFrom, contact1.Resource );

            IResource sourceEmail = _storage.NewResource( "email" );
            sourceEmail.SetProp( "Subject", "Subject2" );
            sourceEmail.AddLink( _contactMgr.Props.LinkEmailAcctTo, emailAccount2 );
            sourceEmail.AddLink( _contactMgr.Props.LinkTo, contact2.Resource );

            //-----------------------------------------------------------------
            //  Merge contact, get only one
            //-----------------------------------------------------------------
            IResourceList list = contact1.Resource.ToResourceList().Union( contact2.Resource.ToResourceList() );
            IResource newContact = _contactMgr.Merge( "Serge Zhulin", list );
            IResourceList contacts = _storage.GetAllResources( "Contact"  );
            Assert.AreEqual( 1, contacts.Count );

            int linksCount = newContact.GetLinksOfType( null, "From" ).Count;
            Assert.AreEqual( 1, linksCount );
            linksCount = newContact.GetLinksOfType( null, "To" ).Count;
            Assert.AreEqual( 1, linksCount );

            //-----------------------------------------------------------------
            //  Create one new mail connected to the united contact
            //-----------------------------------------------------------------
            IResource newEmail = _storage.NewResource( "email" );
            newEmail.SetProp( "Subject", "Subject3" );
            newEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            newEmail.AddLink( _contactMgr.Props.LinkFrom, newContact );

            linksCount = newContact.GetLinksOfType( null, "From" ).Count;
            Assert.AreEqual( 2, linksCount );

            Console.WriteLine( "--- Dumping data for new contact " + newContact.GetStringProp( "FirstName" ) + "-" + newContact.GetStringProp( "LastName" ) );
            list = newContact.GetLinksOfType( null, "From" );
            foreach( IResource mail in list )
            {
                Console.WriteLine( "Mail FROM from account " + mail.GetLinksFrom( null, _contactMgr.Props.LinkEmailAcctFrom )[ 0 ].DisplayName );
            }
            list = newContact.GetLinksOfType( null, "To" );
            foreach( IResource mail in list )
            {
                Console.WriteLine( "Mail TO   from account " + mail.GetLinksFrom( null, _contactMgr.Props.LinkEmailAcctTo )[ 0 ].DisplayName );
            }

            //-----------------------------------------------------------------
            //  Now split the contact, and new mail (third) must be linked to the
            //  contact with the corresponding account
            //-----------------------------------------------------------------
            IResourceList oldContacts = _contactMgr.Split( newContact );
            IResource oldContact1 = oldContacts[ 0 ].GetStringProp( "FirstName" ) == "Sergey" ? oldContacts[ 0 ] : oldContacts[ 1 ];
            IResource oldContact2 = oldContacts[ 0 ].GetStringProp( "FirstName" ) == "Sergey" ? oldContacts[ 1 ] : oldContacts[ 0 ];

            Console.WriteLine( "\n--- Dumping data for old contact 1 (" + oldContact1.GetStringProp( "FirstName" ) + "-" + oldContact1.GetStringProp( "LastName" ) + ")" );
            list = oldContact1.GetLinksOfType( null, "From" );
            foreach( IResource mail in list )
            {
                Console.WriteLine( "Mail FROM from account " + mail.GetLinksFrom( null, _contactMgr.Props.LinkEmailAcctFrom )[ 0 ].DisplayName );
            }
            list = oldContact1.GetLinksOfType( null, "To" );
            foreach( IResource mail in list )
            {
                Console.WriteLine( "Mail TO   from account " + mail.GetLinksFrom( null, _contactMgr.Props.LinkEmailAcctTo )[ 0 ].DisplayName );
            }
            Console.WriteLine( "\n--- Dumping data for old contact 2 (" + oldContact2.GetStringProp( "FirstName" ) + "-" + oldContact2.GetStringProp( "LastName" ) + ")" );
            list = oldContact2.GetLinksOfType( null, "From" );
            foreach( IResource mail in list )
            {
                Console.WriteLine( "Mail FROM from account " + mail.GetLinksFrom( null, _contactMgr.Props.LinkEmailAcctFrom )[ 0 ].DisplayName );
            }
            list = oldContact2.GetLinksOfType( null, "To" );
            foreach( IResource mail in list )
            {
                Console.WriteLine( "Mail TO   from account " + mail.GetLinksFrom( null, _contactMgr.Props.LinkEmailAcctTo )[ 0 ].DisplayName );
            }

            Console.WriteLine( "Froms at 1: " + oldContact1.GetLinksOfType( null, "From" ).Count );
            Console.WriteLine( "Tos at 1: " + oldContact1.GetLinksOfType( null, "To" ).Count );
            Console.WriteLine( "Froms at 2: " + oldContact2.GetLinksOfType( null, "From" ).Count );
            Console.WriteLine( "Tos at 2: " + oldContact2.GetLinksOfType( null, "To" ).Count );
            Assert.AreEqual( oldContact1.GetLinksOfType( null, "From" ).Count, 2 );
            Assert.AreEqual( oldContact1.GetLinksOfType( null, "To" ).Count, 0 );
            Assert.AreEqual( oldContact2.GetLinksOfType( null, "From" ).Count, 0 );
            Assert.AreEqual( oldContact2.GetLinksOfType( null, "To" ).Count, 1 );
        }

        [Test]
        public void ContactsSplitWithReassignmentOfNewMail2()
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";

            //-----------------------------------------------------------------
            //  create contacts
            //-----------------------------------------------------------------
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, "Sergey Zhulin" );
            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, "Serg Zhulin" );

            //-----------------------------------------------------------------
            //  link contacts to their corresponding accounts
            //-----------------------------------------------------------------
            IResource emailAccount1 = contact1.Resource.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( targetEmailAddress, emailAccount1.GetPropText("EmailAddress") );

            IResource emailAccount2 = contact2.Resource.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( sourceEmailAddress, emailAccount2.GetPropText("EmailAddress") );

            //-----------------------------------------------------------------
            //  link mails to the accounts and correspondents
            //-----------------------------------------------------------------
            IResource targetEmail = _storage.NewResource( "email" );
            targetEmail.SetProp( "Subject", "Subject1" );
            targetEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            targetEmail.AddLink( _contactMgr.Props.LinkFrom, contact1.Resource );

            IResource sourceEmail = _storage.NewResource( "email" );
            sourceEmail.SetProp( "Subject", "Subject2" );
            sourceEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount2 );
            sourceEmail.AddLink( _contactMgr.Props.LinkFrom, contact2.Resource );

            //-----------------------------------------------------------------
            //  Merge contact, get only one
            //-----------------------------------------------------------------
            IResourceList list = contact1.Resource.ToResourceList().Union( contact2.Resource.ToResourceList() );
            IResource newContact = _contactMgr.Merge( "Serge Zhulin", list );
            IResourceList contacts = _storage.GetAllResources( "Contact"  );
            Assert.AreEqual( 1, contacts.Count );

            //-----------------------------------------------------------------
            //  Create one new mail connected to the united contact
            //-----------------------------------------------------------------
            IResource newEmail = _storage.NewResource( "email" );
            newEmail.SetProp( "Subject", "Subject3" );
            newEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            newEmail.AddLink( _contactMgr.Props.LinkFrom, newContact );

            newEmail = _storage.NewResource( "email" );
            newEmail.SetProp( "Subject", "Subject4" );
            newEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount2 );
            newEmail.AddLink( _contactMgr.Props.LinkFrom, newContact );

            //-----------------------------------------------------------------
            //  Now split the contact, and new mail (third) must be linked to the
            //  contact with the corresponding account
            //-----------------------------------------------------------------
            IResourceList oldContacts = _contactMgr.Split( newContact );
            IResource oldContact1 = oldContacts[ 0 ].GetStringProp( "FirstName" ) == "Sergey" ? oldContacts[ 0 ] : oldContacts[ 1 ];
            IResource oldContact2 = oldContacts[ 0 ].GetStringProp( "FirstName" ) == "Sergey" ? oldContacts[ 1 ] : oldContacts[ 0 ];

            Console.WriteLine( "Froms at 1: " + oldContact1.GetLinksOfType( null, "From" ).Count );
            Console.WriteLine( "Froms at 1: " + oldContact2.GetLinksOfType( null, "From" ).Count );
            Assert.AreEqual( oldContact1.GetLinksOfType( null, "From" ).Count, 2 );
            Assert.AreEqual( oldContact1.GetLinksOfType( null, "To" ).Count, 0 );
            Assert.AreEqual( oldContact2.GetLinksOfType( null, "From" ).Count, 2 );
            Assert.AreEqual( oldContact2.GetLinksOfType( null, "To" ).Count, 0 );
        }

        [Test]
        public void ContactsCascadingMergeAndSplit()
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";
            string nextAddress = "1@1.1";

            //-----------------------------------------------------------------
            //  create contacts
            //-----------------------------------------------------------------
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, "Sergey Zhulin" );
            IResource targetContact = contact1.Resource;
            Assert.AreEqual( "Sergey", contact1.FirstName );
            Assert.AreEqual( "Zhulin", contact1.LastName );

            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, "Serg Zhulin" );
            IResource sourceContact = contact2.Resource;
            Assert.AreEqual( "Serg", contact2.FirstName );
            Assert.AreEqual( "Zhulin", contact2.LastName );

            IContact contact3 = _contactMgr.FindOrCreateContact( nextAddress, "Serge Zhulin" );
            IResource nextContact = contact3.Resource;
            Assert.AreEqual( "Serge", contact3.FirstName );
            Assert.AreEqual( "Zhulin", contact3.LastName );

            IContact contactMed = _contactMgr.FindOrCreateContact( "1@1.1", "Mediator Contact" );
            IResource mediatorContact = contactMed.Resource;
            Assert.AreEqual( "Mediator", contactMed.FirstName );
            Assert.AreEqual( "Contact", contactMed.LastName );

            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 4, contacts.Count );

            //-----------------------------------------------------------------
            //  link contacts to their corresponding accounts
            //-----------------------------------------------------------------
            IResource emailAccount1 = targetContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( targetEmailAddress, emailAccount1.GetPropText("EmailAddress") );
            IResourceList emailAccounts = targetContact.GetLinksOfType( "EmailAccount", "EmailAcct" );
            Assert.AreEqual( 1, emailAccounts.Count );

            IResource emailAccount2 = sourceContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( sourceEmailAddress, emailAccount2.GetPropText("EmailAddress") );
            emailAccounts = sourceContact.GetLinksOfType( "EmailAccount", "EmailAcct" );
            Assert.AreEqual( 1, emailAccounts.Count );

            //-----------------------------------------------------------------
            //  link mails to the accounts and correspondents
            //-----------------------------------------------------------------

            //      targetContact -> email -> mediatorContact

            IResource targetEmail = _storage.NewResource( "email" );
            targetEmail.SetProp( "Subject", "Subject1" );
            targetEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            targetEmail.AddLink( _contactMgr.Props.LinkFrom, targetContact );
            targetEmail.AddLink( _contactMgr.Props.LinkTo, mediatorContact );
            IResourceList mails = targetContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );
            mails = mediatorContact.GetLinksOfType( "Email", "To" );
            Assert.AreEqual( 1, mails.Count );

            //      sourceContact -> email -> mediatorContact

            IResource sourceEmail = _storage.NewResource( "email" );
            sourceEmail.SetProp( "Subject", "Subject2" );
            sourceEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount2 );
            sourceEmail.AddLink( _contactMgr.Props.LinkFrom, sourceContact );
            sourceEmail.AddLink( _contactMgr.Props.LinkTo, mediatorContact );
            mails = sourceContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );
            mails = mediatorContact.GetLinksOfType( "Email", "To" );
            Assert.AreEqual( 2, mails.Count );

            //      mediatorContact -> email -> nextContact

            IResource nextEmail = _storage.NewResource( "email" );
            nextEmail.SetProp( "Subject", "Subject3" );
            nextEmail.AddLink( _contactMgr.Props.LinkTo, nextContact );
            nextEmail.AddLink( _contactMgr.Props.LinkFrom, mediatorContact );
            mails = nextContact.GetLinksOfType( "Email", "To" );
            Assert.AreEqual( 1, mails.Count );
            mails = mediatorContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );

            //-----------------------------------------------------------------
            IResourceList list = targetContact.ToResourceList().Union( sourceContact.ToResourceList() );
            IResource newContact = _contactMgr.Merge( "Serega Zhulin", list );

            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 3, contacts.Count ); // newContact, nextContact & mediatorContact

            int linksCount = newContact.GetLinksOfType( null, "From" ).Count;
            Assert.AreEqual( 2, linksCount );

            //-----------------------------------------------------------------
            list = nextContact.ToResourceList().Union( newContact.ToResourceList() );
            IResource newContact2 = _contactMgr.Merge( "X Y", list );
            contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 2, contacts.Count );  // newContact2 & mediatorContact

            linksCount = newContact2.GetLinksOfType( null, "From" ).Count;
            Assert.AreEqual( 2, linksCount );
            linksCount = newContact2.GetLinksOfType( null, "To" ).Count;
            Assert.AreEqual( 1, linksCount );

            //-----------------------------------------------------------------
            _contactMgr.Split( newContact2 );
            contacts = _storage.GetAllResources( "Contact" ).Minus( mediatorContact.ToResourceList());
            foreach( IResource cnt in contacts )
                Console.WriteLine( "Contact after split: " + cnt.DisplayName );

            //  Now we maintain a flat structure of serialized contacts.
            Assert.AreEqual( 3, contacts.Count );

            //-----------------------------------------------------------------
            IResourceList froms = mediatorContact.GetLinksOfType( null, "From" );
            IResourceList tos = mediatorContact.GetLinksOfType( null, "To" );
            Assert.AreEqual( froms.Count, 1 );
            Assert.AreEqual( tos.Count, 2 );
            Assert.AreEqual( froms.Intersect( tos ).Count, 0 );
        }

        [Test]
        public void EmptyContactsMergeAndSplitWithReassignmentOfNewMail4()
        {
            string targetEmailAddress = "zhu@intellij.com";
            string sourceEmailAddress = "zhu@jetBrains.com";

            //-----------------------------------------------------------------
            //  create contacts
            //-----------------------------------------------------------------
            IContact contact1 = _contactMgr.FindOrCreateContact( targetEmailAddress, null );
            IContact contact2 = _contactMgr.FindOrCreateContact( sourceEmailAddress, null );

            //-----------------------------------------------------------------
            //  Check links of contacts with their corresponding accounts
            //-----------------------------------------------------------------
            IResource emailAccount1 = contact1.Resource.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( targetEmailAddress, emailAccount1.GetPropText("EmailAddress") );

            IResource emailAccount2 = contact2.Resource.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( sourceEmailAddress, emailAccount2.GetPropText("EmailAddress") );

            //-----------------------------------------------------------------
            //  link mails to the accounts and correspondents
            //-----------------------------------------------------------------
            IResource targetEmail = _storage.NewResource( "email" );
            targetEmail.SetProp( "Subject", "Subject1" );
            targetEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            targetEmail.AddLink( _contactMgr.Props.LinkFrom, contact1.Resource );

            IResource sourceEmail = _storage.NewResource( "email" );
            sourceEmail.SetProp( "Subject", "Subject2" );
            sourceEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount2 );
            sourceEmail.AddLink( _contactMgr.Props.LinkFrom, contact2.Resource );

            //-----------------------------------------------------------------
            //  Merge contact, get only one
            //-----------------------------------------------------------------
            IResourceList list = contact1.Resource.ToResourceList().Union( contact2.Resource.ToResourceList() );
            IResource newContact = _contactMgr.Merge( "Serge Zhulin", list );
            IResourceList contacts = _storage.GetAllResources( "Contact" );
            Assert.AreEqual( 1, contacts.Count );

            //-----------------------------------------------------------------
            //  Create new mails connected to the united contact
            //-----------------------------------------------------------------
            IResource newEmail = _storage.NewResource( "email" );
            newEmail.SetProp( "Subject", "Subject3" );
            newEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            newEmail.AddLink( _contactMgr.Props.LinkFrom, newContact );

            newEmail = _storage.NewResource( "email" );
            newEmail.SetProp( "Subject", "Subject4" );
            newEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount2 );
            newEmail.AddLink( _contactMgr.Props.LinkFrom, newContact );

            //-----------------------------------------------------------------
            //  Now split the contact, and new mail (third) must be linked to the
            //  contact with the corresponding account
            //-----------------------------------------------------------------
            IResourceList oldContacts = _contactMgr.Split( newContact );
            IResource oldContact1 = oldContacts[ 0 ];
            IResource oldContact2 = oldContacts[ 1 ];

            Assert.AreEqual( oldContact1.GetLinksOfType( null, "From" ).Count, 2 );
            Assert.AreEqual( oldContact1.GetLinksOfType( null, "To" ).Count, 0 );
            Assert.AreEqual( oldContact2.GetLinksOfType( null, "From" ).Count, 2 );
            Assert.AreEqual( oldContact2.GetLinksOfType( null, "To" ).Count, 0 );
        }

        [Test]
        public void TestCorrectSplitWithEmailAccount( )
        {
            #region Contacts
            string emailAcc1 = "zhu@intellij.com";
            string emailAcc2 = "zhu@jetBrains.com";

            IContact contact1 = _contactMgr.FindOrCreateContact( emailAcc1, "Sergey Zhulin" );
            IResource targetContact = contact1.Resource;

            IContact contact2 = _contactMgr.FindOrCreateContact( emailAcc2, "Serg Zhulin" );
            IResource sourceContact = contact2.Resource;
            #endregion Contacts

            #region Emails
            IResource emailAccount1 = targetContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( emailAcc1, emailAccount1.GetPropText("EmailAddress") );
            IResource targetEmail = _storage.NewResource( "email" );
            targetEmail.SetProp( "Subject", "Subject1" );
            targetEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount1 );
            targetEmail.AddLink( _contactMgr.Props.LinkFrom, targetContact );
            targetEmail.AddLink( _contactMgr.Props.LinkTo, targetContact );
            IResourceList mails = targetContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );

            IResource emailAccount2 = sourceContact.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( emailAcc2, emailAccount2.GetPropText("EmailAddress") );
            IResource sourceEmail = _storage.NewResource( "email" );
            sourceEmail.SetProp( "Subject", "Subject2" );
            sourceEmail.AddLink( _contactMgr.Props.LinkEmailAcctFrom, emailAccount2 );
            sourceEmail.AddLink( _contactMgr.Props.LinkFrom, sourceContact );
            sourceEmail.AddLink( _contactMgr.Props.LinkTo, sourceContact );
            mails = sourceContact.GetLinksOfType( "Email", "From" );
            Assert.AreEqual( 1, mails.Count );
            #endregion Emails

            _contactMgr.Merge( "Serge Zhulin",
                                   targetContact.ToResourceList().Union( sourceContact.ToResourceList() ) );
            IResourceList contacts = _storage.GetAllResources( "Contact"  );
            Assert.AreEqual( 1, contacts.Count );

            IResource major = contacts[ 0 ];
            IResourceList contactKeepers = major.GetLinksOfType( "ContactSerializationBlobKeeper",
                                                                 ContactManager._propSerializationBlobLink );
            IResourceList splits = _contactMgr.Split( major, contactKeepers[ 0 ].ToResourceList() );
            IResourceList emailAcc = major.GetLinksOfType( null, "EmailAcct" );
            Assert.AreEqual( 1, emailAcc.Count );
        }
    }
}
