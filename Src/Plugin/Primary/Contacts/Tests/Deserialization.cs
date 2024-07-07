// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.IO;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using JetBrains.Omea.ResourceTools;
using NUnit.Framework;

namespace JetBrains.Omea.ContactsPlugin.Tests
{
	[TestFixture]
    public class DeserializationTests
	{
        private TestCore _core;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            Core.ResourceStore.ResourceTypes.Register( "Email", "Name" );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        //  Test covers the situation when a contact is deserialized with some
        //  e-mail accounts existing. In such a case, no new account must be created,
        //  but rather the existing one should be used.
        [Test]
        public void DeserializeExistingAccount()
        {
            string emailAddress = "zhu@intellij.com";

            IContact contact = Core.ContactManager.FindOrCreateContact( emailAddress, "Sergey Zhulin" );
            IResource emailAccount = contact.Resource.GetLinkProp( "EmailAcct" );
            Assert.AreEqual( emailAddress, emailAccount.GetPropText("EmailAddress") );

            Stream strm = ResourceBinarySerialization.Serialize( contact.Resource );

            //  remove the contact but not the account.
            //  deserialize the contact, there must be only one account.
            contact.Resource.Delete();
            ResourceBinarySerialization.Deserialize( strm );
            IResourceList accounts = Core.ResourceStore.GetAllResources( "EmailAccount");
            int accCount = accounts.Count;
            Assert.AreEqual( accCount, 1 );
            Assert.AreEqual( emailAddress, accounts[ 0 ].GetPropText( "EmailAddress" ) );
        }

    }
}
