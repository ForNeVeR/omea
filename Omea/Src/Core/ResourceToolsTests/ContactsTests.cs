/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;

namespace ContactsTests
{
    [TestFixture]
    public class ContactsTests
    {
        private string _name = "Sergey Zhulin";
        private string _email = "zhu@jetBrains.com";
        private string  Title, FirstName, MiddleName, LastName, Suffix, AddSpec;

        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test] public void CheckNameSuffixes()
        {
            string  senderName = "Michael McNaman, Jr.";
            string  mail = "";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName.IndexOf( "Jr." ) == -1 );
            Assert.IsTrue( LastName.IndexOf( "Jr." ) == -1 );
            Console.WriteLine( "1. - [" + senderName + "]: " + FirstName + "|" + LastName + " | " + Suffix );

            senderName = "Michael L. McNaman, Sr.";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName.IndexOf( "Jr." ) == -1 );
            Assert.IsTrue( LastName.IndexOf( "Jr." ) == -1 );
            Console.WriteLine( "2. - [" + senderName + "]: " + FirstName + " | " + LastName + " | " + Suffix );

            senderName = "Michael L. McNaman,  I";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName.IndexOf( " I" ) == -1 );
            Assert.IsTrue( LastName.IndexOf( " I" ) == -1 );
            Console.WriteLine( "3. - [" + senderName + "]: " + FirstName + " | " + LastName + " | " + Suffix );

            senderName = "Michael L. McNaman II ";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName.IndexOf( "II" ) == -1 );
            Assert.IsTrue( LastName.IndexOf( "II" ) == -1 );
            Console.WriteLine( "4. - [" + senderName + "]: " + FirstName + " | " + LastName + " | " + Suffix );

            senderName = "Michael L. McNaman  III  ";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName.IndexOf( "III" ) == -1 );
            Assert.IsTrue( LastName.IndexOf( "III" ) == -1 );
            Console.WriteLine( "5. - [" + senderName + "]: " + FirstName + " | " + LastName + " | " + Suffix );

            senderName = "Prof. Michael L. McNaman  III  ";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName.IndexOf( "Prof" ) == -1 );
            Assert.IsTrue( LastName.IndexOf( "Prof" ) == -1 );
            Console.WriteLine( "5. - [" + senderName + "]: " + Title + " | " + FirstName + " | " + LastName + " | " + Suffix );

            senderName = "Dr   Michael L. McNaman  III  ";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName.IndexOf( "Prof" ) == -1 );
            Assert.IsTrue( LastName.IndexOf( "Prof" ) == -1 );
            Console.WriteLine( "6. - [" + senderName + "]: " + Title + " | " + FirstName + " | " + LastName + " | " + Suffix );
        }

        [Test] public void Parsing2Or3Names()
        {
            string  senderName = "Michael McNaman";
            string  mail = "";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName == "Michael" );
            Assert.IsTrue( LastName == "McNaman" );

            senderName = "Michael A. McNaman";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName == "Michael" );
            Assert.IsTrue( MiddleName == "A." );
            Assert.IsTrue( LastName == "McNaman" );

            senderName = "McNaman, Michael";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName == "Michael" );
            Assert.IsTrue( LastName == "McNaman" );

            senderName = "McNaman, Michael A.";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName == "Michael" );
            Assert.IsTrue( MiddleName == "A." );
            Assert.IsTrue( LastName == "McNaman" );
        }

        [Test] public void ParsingFullName()
        {
            string  senderName = "dr. Michael A. McNaman, Jr (Email)";
            string  mail = "";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( Title == "dr." );
            Assert.IsTrue( FirstName == "Michael" );
            Assert.IsTrue( MiddleName == "A." );
            Assert.IsTrue( LastName == "McNaman" );
            Assert.IsTrue( Suffix == "Jr" );
            Assert.IsTrue( AddSpec == "(Email)" );
        }

        [Test] public void ParsingFnLnWithAddSpec()
        {
            string  senderName = "FirstName LastName (JetBrains)";
            string  mail = "";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName == "FirstName" );
            Assert.IsTrue( MiddleName == string.Empty );
            Assert.IsTrue( LastName == "LastName" );
            Assert.IsTrue( AddSpec == "(JetBrains)" );

            senderName = "FirstName MiddleName LastName (JetBrains)";
            ContactResolver.ResolveName( senderName, mail, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( FirstName == "FirstName" );
            Assert.IsTrue( MiddleName == "MiddleName" );
            Assert.IsTrue( LastName == "LastName" );
            Assert.IsTrue( AddSpec == "(JetBrains)" );
        }

        [Test]
        public void FirstName_LastName_Test( )
        {
            _name = "Sergey Zhulin";
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( bResolved, "'" + _name + "' must be resolved." );
            Assert.AreEqual( "Sergey", FirstName, "Wrong first name" );
            Assert.AreEqual( "Zhulin", LastName, "Wrong last name" );
        }

        [Test]
        public void LastName_FirstName_Test( )
        {
            _name = "Zhulin,   Sergey ";
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( bResolved, "'" + _name + "' resolution" );
            Assert.AreEqual( "Sergey", FirstName, "Wrong first name" );
            Assert.AreEqual( "Zhulin", LastName, "Wrong last name" );
        }

        [Test]
        public void Long_DisplayName_Test( )
        {
            _name = "Vasiliy Pupkin vperedi ";
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( bResolved, "'" + _name + "' resolution" );
            Assert.AreEqual( FirstName, "Vasiliy" );
            Assert.AreEqual( LastName, "vperedi" );
        }

        [Test]
        public void Short_DisplayName_Test( )
        {
            _name = "Pupkin";
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( bResolved, "'" + _name + "' resolution" );
            Assert.AreEqual( string.Empty, FirstName );
            Assert.AreEqual( _name, LastName );
        }

        [Test]
        public void Empty_DisplayName_Test( )
        {
            _name = string.Empty;
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsFalse( bResolved, "'" + _name + "' resolution" );
            Assert.AreEqual( string.Empty, FirstName );
            Assert.AreEqual( _name, LastName );
        }
        [Test]
        public void Null_DisplayName_Test( )
        {
            _name = null;
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsFalse( bResolved, "'" + _name + "' resolution" );
            Assert.AreEqual( string.Empty, FirstName );
            Assert.AreEqual( string.Empty, LastName );
        }

        [Test]
        public void Null_DisplayName_Null_Email_Test( )
        {
            _name = null;
            _email = null;
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsFalse( bResolved, "'" + _name + "' resolution" );
            Assert.AreEqual( string.Empty, FirstName );
            Assert.AreEqual( string.Empty, LastName );
        }

        [Test]
        public void Empty_DisplayName_Empty_Email_Test( )
        {
            _name = string.Empty;
            _email = string.Empty;
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsFalse( bResolved, "'" + _name + "' resolution" );
            Assert.AreEqual( string.Empty, FirstName );
            Assert.AreEqual( string.Empty, LastName );
        }
        [Test]
        public void DisplayNameIsEqualToEmail_Test( )
        {
            _name = "zhu@jetBrains.com";
            _email = _name;
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsFalse( bResolved, "'" + _name + "' resolution" );
            Assert.AreEqual( string.Empty, FirstName );
            Assert.AreEqual( string.Empty, LastName );
        }
        [Test]
        public void DisplayNameIsCleanableGarbageTest()
        {
            _name = "\\";
            ContactResolver.ResolveName( _name, null, out Title, out FirstName, out MiddleName,
                                         out LastName, out Suffix, out AddSpec );
            Assert.AreEqual( _name, LastName, "Wrong last name" );
        }
        [Test]
        public void FirstNameDotLastName_Test( )
        {
            _name = "sergey.zhulin@mail.ru";
            _email = "sergey.zhulin@mail.ru";
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( bResolved, "'" + _name + "' resolution" );
            Assert.AreEqual( "sergey", FirstName );
            Assert.AreEqual("zhulin", LastName );
        }

        [Test]
        public void FirstNameDotLastNameExcludeExtraPrefixTest( )
        {
            _name = "From: sergey.zhulin@mail.ru";
            _email = "From: sergey.zhulin@mail.ru";
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsTrue( !bResolved );
        }

        [Test]
        public void FirstNameDotLastName_DisplayNameEmpty_Test( )
        {
            _name = string.Empty;
            _email = "sergey.zhulin@mail.ru";
            bool bResolved = ContactResolver.ResolveName( _name, _email, out Title, out FirstName, out MiddleName,
                                                          out LastName, out Suffix, out AddSpec );
            Assert.IsFalse( bResolved, "'" + _name + "' resolution" );
            Assert.AreEqual( string.Empty, FirstName );
            Assert.AreEqual( string.Empty, LastName );
        }

	}

    [TestFixture]
	public class ContactTests
	{
        private TestCore _core;
		private IContactManager _contactManager;
        private IResourceStore _storage;

		[SetUp]
		public void SetUp()
		{
            _core = new TestCore();
            _storage = _core.ResourceStore;
		    _contactManager = _core.ContactManager;
		}

		[TearDown]
		public void TearDown()
		{
			_core.Dispose();
		}

		[Test] public void SimpleFindContactTest()
		{
			string email = "zhu@jetBrains.com";
			IContact contact = _contactManager.FindOrCreateContact( email, "Sergey Zhulin"  );
			Assert.AreEqual( "Sergey", contact.FirstName );
			Assert.AreEqual( "Zhulin", contact.LastName );
			IResourceList accounts = contact.Resource.GetLinksOfType( "EmailAccount", "EmailAcct" );
			Assert.AreEqual( 1, accounts.Count );
			IResource account = accounts[0];
			Assert.AreEqual( email, account.GetPropText( "EmailAddress" ) );
		}

		[Test] public void Test1()
		{
			string email = "sergzzz@chat.ru";
			IContact contact = _contactManager.FindOrCreateContact( email, string.Empty  );
			contact = _contactManager.FindOrCreateContact( email, "Sergey Zhulin"  );
			Assert.AreEqual( "Sergey", contact.FirstName );
			Assert.AreEqual( "Zhulin", contact.LastName );

			IResourceList accounts = contact.Resource.GetLinksOfType( "EmailAccount", "EmailAcct" );
			Assert.AreEqual( 1, accounts.Count );
			IResource account = accounts[0];
			Assert.AreEqual( email, account.GetPropText( "EmailAddress" ) );

			account = _storage.FindUniqueResource( "EmailAccount",  "EmailAddress", email );
			IResourceList contacts = account.GetLinksOfType( "Contact", "EmailAcct" );
			Assert.AreEqual( 1, contacts.Count );
			Assert.AreEqual( "Sergey", contacts[0].GetPropText( "FirstName" ) );
			Assert.AreEqual( "Zhulin", contacts[0].GetPropText( "LastName" ) );
		}

		[Test] public void Test2()
		{
			string email = "sergzzz@chat.ru";
			string senderName = "Sergey Zhulin";

			IContact contact = _contactManager.FindOrCreateContact( email, senderName  );
			contact = _contactManager.FindOrCreateContact( email, string.Empty  );

			Assert.AreEqual( "Sergey", contact.FirstName );
			Assert.AreEqual( "Zhulin", contact.LastName );
			IResourceList accounts = contact.Resource.GetLinksOfType( "EmailAccount", "EmailAcct" );
			Assert.AreEqual( 1, accounts.Count );

			IResource account = accounts[0];
			Assert.AreEqual( email, account.GetPropText( "EmailAddress" ) );
			IResource mail = _storage.FindUniqueResource( "EmailAccount",  "EmailAddress", email );
			IResourceList contacts = mail.GetLinksOfType( "Contact", "EmailAcct" );
			Assert.AreEqual( 1, contacts.Count );
			Assert.AreEqual( "Sergey", contacts[0].GetPropText( "FirstName" ) );
			Assert.AreEqual( "Zhulin", contacts[0].GetPropText( "LastName" ) );
		}
		[Test] public void Test3()
		{
			string email1 = "sergzzz@chat.ru";
			string email2 = "sergzzz@mail.ru";
			string senderName = "Sergey Zhulin";

            IContact contactBO = _contactManager.FindOrCreateContact( email1, string.Empty );
			contactBO = _contactManager.FindOrCreateContact( email2, string.Empty  );
			contactBO = _contactManager.FindOrCreateContact( email1, senderName  );
			contactBO = _contactManager.FindOrCreateContact( email2, senderName  );

			Assert.AreEqual( "Sergey", contactBO.FirstName );
			Assert.AreEqual( "Zhulin", contactBO.LastName );

			IResource contact = _storage.FindUniqueResource( "Contact",  "LastName", "Zhulin" );
//-----
			Assert.AreEqual( "Sergey", contact.GetPropText( "FirstName" ) );
			Assert.AreEqual( "Zhulin", contact.GetPropText( "LastName" ) );

			IResourceList accounts = contact.GetLinksOfType( "EmailAccount", "EmailAcct" );
			Assert.AreEqual( 2, accounts.Count );

			IResource mail = _storage.FindUniqueResource( "EmailAccount",  "EmailAddress", email1 );
			Assert.AreEqual( 1, mail.GetLinksOfType( "Contact", "EmailAcct" ).Count );
			mail = _storage.FindUniqueResource( "EmailAccount",  "EmailAddress", email2 );
			Assert.AreEqual( 1, mail.GetLinksOfType( "Contact", "EmailAcct" ).Count );
		}

        [Test] public void Test4()
        {
            IContact contact1 = _contactManager.FindOrCreateContact( "deementhus@yandex.ru", "Dmitry" );
            IContact contact2 = _contactManager.FindOrCreateContact( "yole@yole.ru", "Dmitry Jemerov" );
            Assert.IsTrue( contact1.Resource.Id != contact2.Resource.Id );
        }

        [Test] public void Test5()
        {
            IContact contact1 = _contactManager.FindOrCreateContact( "yole@yole.ru", "" );
            IContact contact2 = _contactManager.FindOrCreateContact( "deementhus@yandex.ru", "Dmitry" );
            IContact contact3 = _contactManager.FindOrCreateContact( "yole@yole.ru", "Dmitry Jemerov" );
            Assert.IsTrue( contact1.Resource.Id != contact2.Resource.Id );
            Assert.AreEqual( contact1.Resource.Id, contact3.Resource.Id );
        }
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void WasCreatedFlagTest1()
        {
            _contactManager.FindOrCreateContact( "", "" );
        }
    }
}
