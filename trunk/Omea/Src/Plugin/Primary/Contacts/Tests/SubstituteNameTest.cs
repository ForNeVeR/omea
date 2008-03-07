/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;

namespace JetBrains.Omea.ContactsPlugin.Tests
{
	[TestFixture]
    public class SubstituteNameTest
	{
        private IResource _email;
        private IResource _contact;
        private IResource _contact2;
        private IResourceStore _storage;
        private TestCore _core;
        private IContactManager _contactManager;
        
        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;

            Core.ResourceStore.ResourceTypes.Register( "Email", "Name" );

            _contactManager = Core.ContactManager;

            _email = _core.ResourceStore.NewResource( "Email" );
            _contact = _core.ResourceStore.NewResource( "Contact" );
            _contact.SetProp( ContactManager._propFirstName, "Dmitry" );
            _contact.SetProp( ContactManager._propLastName, "Jemerov" );

            _contact2 = _core.ResourceStore.NewResource( "Contact" );
            _contact2.SetProp( ContactManager._propFirstName, "Michael" );
            _contact2.SetProp( ContactManager._propLastName, "Gerasimov" );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        private IResource CreateContactName( IResource contact, string name )
        {
            IResource contactName = _core.ResourceStore.BeginNewResource( "ContactName" );
            contactName.SetProp( "Name", name );
            contactName.AddLink( Core.ContactManager.Props.LinkBaseContact, contact );
            contactName.EndUpdate();
            return contactName;
        }

        [Test] public void NoContactName()
        {
            _email.AddLink( _contactManager.Props.LinkFrom, _contact );
            Assert.AreEqual( "Dmitry Jemerov", ContactsPlugin.SubstituteName( _email, _contactManager.Props.LinkFrom ) );
        }

        [Test] public void NoOriginalName()
        {
            IResource contactName = CreateContactName( _contact, "Dmitry Jemerov (JetBrains)" );

            _email.AddLink( _contactManager.Props.LinkFrom, _contact );
            _email.AddLink( Core.ContactManager.Props.LinkNameFrom, contactName );
            Assert.AreEqual( "Dmitry Jemerov", ContactsPlugin.SubstituteName( _email, _contactManager.Props.LinkFrom ) );
        }

        [Test] public void OriginalName()
        {
            IResource contactName = CreateContactName( _contact, "Dmitry Jemerov (JetBrains)" );
            _contact.SetProp( Core.ContactManager.Props.ShowOriginalNames, true );
            _email.AddLink( _contactManager.Props.LinkFrom, _contact );
            _email.AddLink( Core.ContactManager.Props.LinkNameFrom, contactName );
            Assert.AreEqual( "Dmitry Jemerov (JetBrains)", ContactsPlugin.SubstituteName( _email, _contactManager.Props.LinkFrom ) );
        }

        [Test] public void NoContact()
        {
            _storage.ResourceTypes.Register( "Weblog", "Name" );
            IResource weblog = _storage.NewResource( "Weblog" );
            weblog.SetProp( "Name", "yole's devblog" );
            _email.AddLink( _contactManager.Props.LinkFrom, weblog );
            Assert.AreEqual( "yole's devblog", ContactsPlugin.SubstituteName( _email, _contactManager.Props.LinkFrom ) );
        }

        [Test] public void MultipleNames()
        {
            IResource contactName = CreateContactName( _contact, "Dmitry Jemerov (JetBrains)" );
            IResource contactName2 = CreateContactName( _contact2, "Michael Gerasimov (JetBrains)" );
            _contact.SetProp( Core.ContactManager.Props.ShowOriginalNames, true );
            _email.AddLink( _contactManager.Props.LinkFrom, _contact );
            _email.AddLink( Core.ContactManager.Props.LinkNameFrom, contactName );
            _email.AddLink( _contactManager.Props.LinkFrom, _contact2 );
            _email.AddLink( Core.ContactManager.Props.LinkNameFrom, contactName2 );
            Assert.AreEqual( "Dmitry Jemerov (JetBrains), Michael Gerasimov", 
                ContactsPlugin.SubstituteName( _email, _contactManager.Props.LinkFrom ) );
        }
    }
}
