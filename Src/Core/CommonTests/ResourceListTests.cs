// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.OpenAPI;

namespace CommonTests
{
    /**
     * Unit tests for the ResourceList class.
     */

    [TestFixture]
    public class ResourceListTests: MyPalDBTests
    {
        private ArrayList _addedResources;
        private ArrayList _deletedResources;
        private ArrayList _changedResources;

        [SetUp] public void SetUp()
        {
            InitStorage();
            RegisterResourcesAndProperties();
            _addedResources = new ArrayList();
            _deletedResources = new ArrayList();
            _changedResources = new ArrayList();
        }

        [TearDown] public void TearDown()
        {
            CloseStorage();
        }

        private void OnResourceAdded( object sender, ResourceIndexEventArgs e )
        {
            _addedResources.Add( e );
        }

        private void OnResourceDeleting( object sender, ResourceIndexEventArgs e )
        {
            _deletedResources.Add( e );
        }

        private void OnResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            _changedResources.Add( e );
        }

        private void AttachHandlers( IResourceList resList )
        {
            resList.ResourceAdded    += OnResourceAdded;
            resList.ResourceDeleting += OnResourceDeleting;
            resList.ResourceChanged  += OnResourceChanged;
        }

        [Test] public void FindStrResources()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propSubject, "Test" );

            IResourceList resList = _storage.FindResources( null, _propSubject, "Test" );
            Assert.IsTrue( resList != null, "FindResources must not return a NULL list" );
            Assert.AreEqual( 1, resList.Count );
            Assert.AreEqual( res.Id, resList [0].Id );
        }

        [Test, ExpectedException(typeof(StorageException))]
        public void FindLongStrResources()
        {
            int propLongBody = _storage.PropTypes.Register( "LongBody", PropDataType.LongString );
            IResourceList resList = _storage.FindResources( null, propLongBody, "Test" );
            resList = resList;
        }

        [Test] public void FindMultipleResources()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSubject, "Test" );

            IResource person = _storage.NewResource( "Person" );
            person.SetProp( _propSubject, "Test" );

            Assert.IsTrue( email.Id != person.Id );

            IResourceList resList = _storage.FindResources( null, _propSubject, "Test" );
            Assert.AreEqual( 2, resList.Count );
        }

        [Test] public void FindIntResources()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propSize, 654 );

            IResourceList resList = _storage.FindResources( null, _propSize, 654 );
            Assert.AreEqual( 1, resList.Count );
        }

        [Test] public void FindDateResources()
        {
            DateTime dt = DateTime.Now;

            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propReceived, dt );

            IResourceList resList = _storage.FindResources( null, _propReceived, dt );
            Assert.AreEqual( 1, resList.Count );
        }

        [Test, ExpectedException( typeof(StorageException) ) ]
        public void FindBoolFalseResources()
        {
            IResourceList resList = _storage.FindResources( null, _propUnread, false );
            resList = resList;
        }

        [Test] public void FindBoolTrueResources()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propUnread, true );

            IResourceList resList = _storage.FindResourcesLive( null, _propUnread, true );
            AttachHandlers( resList );

            Assert.AreEqual( 1, resList.Count );
            Assert.AreEqual( res.Id, resList [0].Id );

            res.SetProp( _propUnread, false );
            Assert.AreEqual( 0, resList.Count );
            Assert.AreEqual( 1, _deletedResources.Count );

            res.SetProp( _propUnread, true );
            Assert.AreEqual( 1, resList.Count );
            Assert.AreEqual( 1, _addedResources.Count );

            res.SetProp( _propSize, 100 );   // some property that does not affect prop predicate
            Assert.AreEqual( 1, resList.Count );
            Assert.AreEqual( 1, _addedResources.Count );
        }

        [Test] public void FindTypedResources()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSubject, "Test" );

            IResource person = _storage.NewResource( "Person" );
            person.SetProp( _propSubject, "Test" );

            IResourceList resList = _storage.FindResources( "Email", _propSubject, "Test" );
            Assert.AreEqual( 1, resList.Count );
        }

        [Test] public void FindResourcesCaseInsensitive()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSubject, "Test" );

            IResourceList resList = _storage.FindResources( "email", _propSubject, "Test" );
            Assert.AreEqual( 1, resList.Count );
        }

        [Test] public void FindResourcesWithProp()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Subject", "Test" );
            IResource email2 = _storage.NewResource( "Email" );

            IResourceList resList = _storage.FindResourcesWithPropLive( "Email", _propSubject );
            Assert.AreEqual( 1, resList.Count );

            email2.SetProp( "Subject", "Test2" );
            Assert.AreEqual( 2, resList.Count );

            email2.DeleteProp( "Subject" );
            Assert.AreEqual( 1, resList.Count );
        }

        [Test] public void FindResourcesWithPropNoType()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Subject", "Test" );
            email.AddLink( _propAuthor, person );
            IResource email2 = _storage.NewResource( "Email" );

            IResourceList resList = _storage.FindResourcesWithPropLive( null, _propSubject );
            IResourceList resList2 = _storage.FindResourcesWithPropLive( null, _propAuthor );
            Assert.AreEqual( 1, resList.Count );
            Assert.AreEqual( 2, resList2.Count );

            email.Delete();
            Assert.AreEqual( 0, resList.Count );
            Assert.AreEqual( 0, resList2.Count );

            email2.Delete();                    // this used to trigger #1251
        }

        [Test, ExpectedException(typeof(StorageException))]
        public void FindResourcesWithLongStringProp()  // #5472
        {
            int prop = _storage.PropTypes.Register( "LongString", PropDataType.LongString );
            _storage.FindResourcesWithProp( null, prop );
        }

        [Test] public void FindResourcesWithPropSnapshot()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Subject", "Test" );

            IResourceList resList = _storage.FindResourcesWithProp( SelectionType.LiveSnapshot, "Email", _propSubject );
            AttachHandlers( resList );
            Assert.AreEqual( 1, resList.Count );

            email.DeleteProp( "Subject" );
            Assert.AreEqual( 1, resList.Count );
            Assert.AreEqual( 1, _changedResources.Count );

            _changedResources.Clear();
            email.SetProp( "Size", 100 );
            Assert.AreEqual( 1, _changedResources.Count );
        }

        [Test] public void FindResourcesWithBoolProp()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propUnread, true );
            IResource email2 = _storage.NewResource( "Email" );

            IResourceList resList = _storage.FindResourcesWithPropLive( "Email", _propUnread );
            Assert.AreEqual( 1, resList.Count );

            email2.SetProp( _propUnread, true );
            Assert.AreEqual( 2, resList.Count );

            email2.SetProp( _propUnread, false );
            Assert.AreEqual( 1, resList.Count );
        }

        [Test] public void FindResourcesWithLinkProp()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            email.AddLink( _propAuthor, person );

            IResource email2 = _storage.NewResource( "Email" );

            IResourceList resList = _storage.FindResourcesWithPropLive( "Email", _propAuthor );
            Assert.AreEqual( 1, resList.Count );

            email2.AddLink( _propAuthor, person );
            Assert.AreEqual( 2, resList.Count );

            email2.DeleteLink( _propAuthor, person );
            Assert.AreEqual( 1, resList.Count );

            email.Delete();
            Assert.AreEqual( 0, resList.Count );
        }

        [Test] public void FindResourcesWithLinkProp_SeveralLinks()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            IResource person2 = _storage.NewResource( "Person" );
            email.AddLink( _propAuthor, person );
            email.AddLink( _propAuthor, person2 );

            IResourceList resList = _storage.FindResourcesWithProp( "Email", _propAuthor );
            Assert.AreEqual( 1, resList.Count );
        }

        [Test] public void FindResourcesWithLinkPropBidi()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            email.AddLink( _propAuthor, person );

            IResource email2 = _storage.NewResource( "Email" );
            IResource person2 = _storage.NewResource( "Person" );
            person2.AddLink( _propAuthor, email2 );

            IResourceList resList = _storage.FindResourcesWithProp( "Email", _propAuthor );
            Assert.AreEqual( 2, resList.Count );

            resList = _storage.FindResourcesWithProp( null, _propAuthor );
            Assert.AreEqual( 4, resList.Count );
        }

        [Test] public void FindResourcesWithDirectedLinkProp()
        {
            IResource email1 = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            email1.AddLink( "Reply", email2 );

            IResourceList resWithProp = _storage.FindResourcesWithPropLive( null, "Reply" );
            Assert.AreEqual( 1, resWithProp.Count );
            Assert.AreEqual( email1.Id, resWithProp [0].Id );

            email1.DeleteLink( "Reply", email2 );
            Assert.AreEqual( 0, resWithProp.Count );

            email2.AddLink( "Reply", email1 );
            Assert.AreEqual( 1, resWithProp.Count );
        }

        [Test] public void FindResourcesWithReverseLinkProp()
        {
            IResource email1 = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            email1.AddLink( _propReply, email2 );

            IResourceList resWithProp = _storage.FindResourcesWithPropLive( null, -_propReply );
            Assert.AreEqual( 1, resWithProp.Count );
            Assert.AreEqual( email2.Id, resWithProp [0].Id );

            email1.DeleteLink( "Reply", email2 );
            Assert.AreEqual( 0, resWithProp.Count );

            email2.AddLink( "Reply", email1 );
            Assert.AreEqual( 1, resWithProp.Count );
            Assert.AreEqual( email1.Id, resWithProp [0].Id );
        }

        [Test] public void FindResourcesWithLinkPropVsPropChange()
        {
            IResource email1 = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            email1.AddLink( "Reply", email2 );

            IResourceList resWithProp = _storage.FindResourcesWithPropLive( null, "Reply" );
            resWithProp = resWithProp;

            email1.SetProp( "Subject", "Test" );   // this used to cause #1249
        }

        [Test] public void TestLiveLinks()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );

            IResourceList resList = email.GetLinksOfTypeLive( null, _propAuthor );
            AttachHandlers( resList );
            Assert.AreEqual( 0, resList.Count );

            email.AddLink( _propAuthor, person );
            Assert.AreEqual( 1, resList.Count );
            Assert.AreEqual( 1, _addedResources.Count );

            email.DeleteLink( _propAuthor, person );
            Assert.AreEqual( 1, _deletedResources.Count );
            Assert.AreEqual( 0, resList.Count );
        }

        [Test] public void LiveLinksDelete()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            person.AddLink( _propAuthor, email );
            IResourceList links = person.GetLinksOfTypeLive( null, _propAuthor );
            Assert.AreEqual( 1, links.Count );
            email.Delete();
            Assert.AreEqual( 0, links.Count );
        }

        [Test] public void LiveLinksInsensitive()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            person.AddLink( _propAuthor, email );

            IResourceList links = person.GetLinksOfType( "email", _propAuthor );
            Assert.AreEqual( 1, links.Count );
        }

        [Test] public void KnownTypeOptimization()
        {
            _storage.RegisterLinkRestriction( "Person", _propAuthor, "Email", 0, Int32.MaxValue );

            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            person.AddLink( _propAuthor, email );

            ResourceList links = (ResourceList) person.GetLinksOfType( "Email", _propAuthor );
            Assert.AreEqual( "Link(Author," + person.Id + ")", links.ListTypeToString() );

            ResourceList intersectedLinks = (ResourceList) links.Intersect( _storage.GetAllResources( "Email" ) );
            int count = intersectedLinks.Count;  // force optimization and instantiation
            count = count;
            Assert.AreEqual( "Link(Author," + person.Id + ")", intersectedLinks.ListTypeToString() );

            IResourceList twoTypes = _storage.GetAllResources( "Email" ).Union( _storage.GetAllResources( "Person" ) );
            ResourceList unionIntersectedLinks = (ResourceList) links.Intersect( twoTypes );
            count = unionIntersectedLinks.Count;  // force optimization and instantiation
            Assert.AreEqual( "Link(Author," + person.Id + ")", unionIntersectedLinks.ListTypeToString() );
        }

        [Test] public void TestLiveSelect()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSubject, "Test" );

            IResourceList resList = _storage.FindResourcesLive( null, _propSubject, "Test" );
            AttachHandlers( resList );
            Assert.AreEqual( 1, resList.Count );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( _propSubject, "Test" );

            Assert.AreEqual( 2, resList.Count );

            email2.SetProp( _propSize, 654 );
            Assert.AreEqual( 2, resList.Count );
            Assert.AreEqual( 1, _changedResources.Count );

            email2.SetProp( _propSubject, "No test" );
            Assert.AreEqual( 1, resList.Count );
            Assert.AreEqual( 1, _deletedResources.Count );
        }

        [Test] public void LiveSelectDelete()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSubject, "Test" );

            IResourceList resList = _storage.FindResourcesLive( null, _propSubject, "Test" );
            AttachHandlers( resList );
            Assert.AreEqual( 1, resList.Count );

            email.Delete();
            Assert.AreEqual( 0, resList.Count );
            Assert.AreEqual( 1, _deletedResources.Count );
        }

        [Test] public void GetAllResources()
        {
            IResource email = _storage.NewResource( "Email" );
            email = email;

            IResourceList resList = _storage.GetAllResources( "Email" );
            Assert.AreEqual( 1, resList.Count );
        }

        [Test] public void LiveAllResources()
        {
            IResourceList rlist = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( rlist );

            IResource person = _storage.NewResource( "Person" );

            Assert.AreEqual( 1, rlist.Count );

            person.SetProp( "FirstName", "Dmitry" );
            Assert.AreEqual( 1, _changedResources.Count );

            person.Delete();
            Assert.AreEqual( 0, rlist.Count );
        }

        [Test] public void LiveChangeType()
        {
            IResourceList rlist = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( rlist );

            IResource person = _storage.NewResource( "Person" );
            Assert.AreEqual( 1, rlist.Count );

            person.ChangeType( "Email" );
            Assert.AreEqual( 0, rlist.Count );
        }

        [Test] public void MultiTypeResources()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );

            IResourceList emailsAndPersons = _storage.GetAllResources( new string[] { "Email", "Person" } );
            Assert.AreEqual( 2, emailsAndPersons.Count );
        }

        [Test] public void MultiTypeResourcesLive()
        {
            IResourceList emailsAndPersons = _storage.GetAllResourcesLive( new string[] { "Email", "Person" } );

            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );

            Assert.AreEqual( 2, emailsAndPersons.Count );
        }

        [Test] public void AllResourcesTransient()
        {
            IResourceList allPersons = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( allPersons );
            int c = allPersons.Count;  // force instantiation
            c = c;

            IResource person = _storage.NewResourceTransient( "Person" );
            Assert.AreEqual( 0, _addedResources.Count );

            IResourceList allPersons2 = _storage.GetAllResourcesLive( "Person" );
            // NOTE: no assert here (both variants - 0 or 1 resource in allPerson2 - are acceptable)

            person.Delete();
            Assert.AreEqual( 0, _addedResources.Count );
            Assert.AreEqual( 0, _deletedResources.Count );
            Assert.AreEqual( 0, allPersons2.Count );
        }

        [Test] public void AllResourcesInsensitive()
        {
            IResourceList resList = _storage.GetAllResourcesLive( "email" );

            IResource email = _storage.NewResource( "Email" );
            email = email;
            Assert.AreEqual( 1, resList.Count );
        }

        [Test] public void Sort()
        {
            IResource person1 = _storage.NewResource( "Person" );
            person1.SetProp( "FirstName", "Vasya" );

            IResource person2 = _storage.NewResource( "Person" );
            person2.SetProp( "FirstName", "Kolya" );

            IResourceList resList = _storage.GetAllResources( "Person" );

            SortSettings ss = new SortSettings( _storage.PropTypes[ "FirstName" ].Id, true );
            resList.Sort( ss );
            Assert.AreEqual( "Kolya", resList [0].GetStringProp( "FirstName" ) );
            Assert.AreEqual( "Vasya", resList [1].GetStringProp( "FirstName" ) );
        }

        [Test] public void SortWithNull()
        {
            IResource person1 = _storage.NewResource( "Person" );
            person1.SetProp( "FirstName", "Vasya" );

            IResource person2 = _storage.NewResource( "Person" );
            person2 = person2;

            IResourceList resList = _storage.GetAllResources( "Person" );

            SortSettings ss = new SortSettings( _storage.PropTypes[ "FirstName" ].Id, true );
            resList.Sort( ss );
            Assert.IsTrue( resList [0].GetStringProp( "FirstName" ) == null );
            Assert.AreEqual( "Vasya", resList [1].GetStringProp( "FirstName" ) );
        }

        [Test] public void ReverseSort()
        {
            IResource person1 = CreatePerson( "Vasya", null ); person1 = person1;
            IResource person2 = CreatePerson( "Kolya", null ); person2 = person2;

            IResourceList resList = _storage.GetAllResources( "Person" );

            SortSettings ss = new SortSettings( _storage.PropTypes[ "FirstName" ].Id, false );
            resList.Sort( ss );
            Assert.AreEqual( "Vasya", resList [0].GetStringProp( "FirstName" ) );
            Assert.AreEqual( "Kolya", resList [1].GetStringProp( "FirstName" ) );
        }

        [Test] public void SortByType()
        {
            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "FirstName", "Dmitry" );

            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "FirstName", "Dmitry" );

            IResourceList resList = _storage.FindResources( null, "FirstName", "Dmitry" );
            SortSettings ss = new SortSettings( ResourceProps.Type, true );
            resList.Sort( ss );

            Assert.AreEqual( email.Id, resList [0].Id );
            Assert.AreEqual( person.Id, resList [1].Id );

            resList.Sort( "Type-" );

            Assert.AreEqual( person.Id, resList [0].Id );
            Assert.AreEqual( email.Id, resList [1].Id );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( "FirstName", "Alexey" );

            resList = resList.Union( email2.ToResourceList() );
            resList.Sort( "Type- FirstName" );
            Assert.AreEqual( person.Id, resList [0].Id );
            Assert.AreEqual( email2.Id, resList [1].Id );
            Assert.AreEqual( email.Id, resList [2].Id );
        }

        [Test] public void SortByDisplayName()
        {
            IResource person1 = _storage.NewResource( "Person" );
            person1.DisplayName = "Michael Gerasimov";

            IResource person2 = _storage.NewResource( "Person" );
            person2.DisplayName = "Dmitry Jemerov";

            IResourceList resList = _storage.GetAllResources( "Person" );
            resList.Sort( "DisplayName" );
            Assert.AreEqual( person2.Id, resList [0].Id );
            Assert.AreEqual( person1.Id, resList [1].Id );
        }

        [Test] public void SortByLink()
        {
            IResource person1 = _storage.NewResource( "Person" );
            person1.SetProp( "FirstName", "Michael" );
            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( "Author", person1 );

            IResource person2 = _storage.NewResource( "Person" );
            person2.SetProp( "FirstName", "Dmitry" );
            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( "Author", person2 );

            IResourceList emails = _storage.GetAllResources( "Email" );
            SortSettings ss = new SortSettings( _storage.PropTypes[ "Author" ].Id, true );
            emails.Sort( ss );
            Assert.AreEqual( email2.Id, emails [0].Id );
            Assert.AreEqual( email1.Id, emails [1].Id );

            ss = new SortSettings( _storage.PropTypes[ "Author" ].Id, false );
            emails.Sort( ss );
            Assert.AreEqual( email1.Id, emails [0].Id );
            Assert.AreEqual( email2.Id, emails [1].Id );
        }

        [Test] public void MultiDirSort()
        {
            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( _propSubject, "First" );
            email1.SetProp( _propSize, 100 );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( _propSubject, "Second" );
            email2.SetProp( _propSize, 50 );

            IResource email3 = _storage.NewResource( "Email" );
            email3.SetProp( _propSubject, "Second" );
            email3.SetProp( _propSize, 200 );

            IResourceList emails = _storage.GetAllResources( "Email" );
            emails.Sort( "Subject Size-", true );
            Assert.AreEqual( email1.Id, emails [0].Id );
            Assert.AreEqual( email3.Id, emails [1].Id );
            Assert.AreEqual( email2.Id, emails [2].Id );

            emails.Sort( "Subject Size-", false );
            Assert.AreEqual( email1.Id, emails [2].Id );
            Assert.AreEqual( email3.Id, emails [1].Id );
            Assert.AreEqual( email2.Id, emails [0].Id );
        }

        [Test] public void EquivPropSort()
        {
            int propName = _storage.PropTypes.Register( "Name", PropDataType.String );
            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( "Name", "C" );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( "Subject", "B" );

            IResource email3 = _storage.NewResource( "Email" );
            email3.SetProp( "Name", "A" );

            IResourceList emails = _storage.GetAllResources( "Email" );
            emails.Sort( new int[] { _propSubject, propName }, true, true );

            Assert.AreEqual( email3.Id, emails [0].Id );
            Assert.AreEqual( email2.Id, emails [1].Id );
            Assert.AreEqual( email1.Id, emails [2].Id );
        }

        [Test] public void EquivPropSort_NoProp()
        {
            IResource email1 = _storage.NewResource( "Email" ); email1 = email1;
            IResource email2 = _storage.NewResource( "Email" ); email2 = email2;

            IResourceList emails = _storage.GetAllResources( "Email" );
            emails.Sort( new int[] { _propSubject }, true, true );    // this used to crash (#1532)
            Assert.AreEqual( 2, emails.Count );
        }

        [Test] public void Union()
        {
            IResource person1 = _storage.NewResource( "Person" );
            person1.SetProp( "FirstName", "Vasya" );

            IResource person2 = _storage.NewResource( "Person" );
            person2.SetProp( "FirstName", "Vasya" );
            person2.SetProp( "LastName", "Pupkin" );

            IResource person3 = _storage.NewResource( "Person" );
            person3.SetProp( "FirstName", "Kolya" );
            person3.SetProp( "LastName", "Pupkin" );

            IResourceList rlist1 = _storage.FindResourcesLive( null, _propFirstName, "Vasya" );
            Assert.AreEqual( 2, rlist1.Count );
            IResourceList rlist2 = _storage.FindResourcesLive( null, _propLastName, "Pupkin" );
            Assert.AreEqual( 2, rlist2.Count );

            IResourceList rlist = rlist1.Union( rlist2 );
            AttachHandlers( rlist );
            Assert.AreEqual( 3, rlist.Count );

            person1.Delete();
            Assert.AreEqual( 2, rlist.Count );

            person2.SetProp( "LastName", "Vasin" );
            Assert.AreEqual( 1, rlist2.Count );
            Assert.AreEqual( 2, rlist.Count );

            _changedResources.Clear();
            SortSettings ss = new SortSettings( _storage.PropTypes[ "FirstName" ].Id, true );
            rlist.Sort( ss );
            Assert.AreEqual( person3.Id, rlist [0].Id );

            person3.SetProp( "FirstName", "Borya" );
            Assert.AreEqual( 1, _changedResources.Count );
        }

        [Test] public void MultiUnion()
        {
            IResource person1 = CreatePerson( "Vasya", null ); person1 = person1;
            IResource person2 = CreatePerson( "Kolya", null ); person2 = person2;
            IResource person3 = CreatePerson( "Petya", null ); person3 = person3;

            IResourceList list = _storage.FindResources( "Person", "FirstName", "Vasya" );
            list = list.Union( _storage.FindResources( "Person", "FirstName", "Kolya" ) );
            list = list.Union( _storage.FindResourcesLive( "Person", "FirstName", "Petya" ) );

            Assert.AreEqual( 3, list.Count );

            CreatePerson( "Petya", "Pupkin" );
            Assert.AreEqual( 4, list.Count );
        }

        [Test] public void UnionSort()
        {
            IResource person1 = CreatePerson( "Vasya", null );
            IResource person2 = CreatePerson( "Kolya", null );

            IResourceList list1 = _storage.FindResources( "Person", "FirstName", "Vasya" );
            SortSettings ss = new SortSettings( _storage.PropTypes[ "FirstName" ].Id, true );
            list1.Sort( ss );

            IResourceList list2 = _storage.FindResources( "Person", "FirstName", "Kolya" );
            list2.Sort( ss );

            IResourceList ulist1 = list1.Union( list2 );
            Assert.AreEqual( 1, ulist1.SortPropIDs.Length );
            Assert.AreEqual( _propFirstName, ulist1.SortPropIDs [0] );

            IResource person3 = CreatePerson( "Misha", null );
            IResourceList list3 = _storage.FindResources( "Person", "FirstName", "Misha" );

            IResourceList ulist2 = ulist1.Union( list3, true );
            Assert.IsTrue( ulist2 == ulist1 );
            Assert.AreEqual( 1, ulist2.SortPropIDs.Length );
            Assert.AreEqual( _propFirstName, ulist2.SortPropIDs [0] );

            Assert.AreEqual( person2.Id, ulist2 [0].Id );
            Assert.AreEqual( person3.Id, ulist2 [1].Id );
            Assert.AreEqual( person1.Id, ulist2 [2].Id );
        }

        [Test] public void UnionAllowMerge()
        {
            for( int i=0; i<20; i++ )
            {
                IResource res = _storage.NewResource( "Email" );
                res.SetProp( "Size", i );
            }

            IResourceList resList1 = _storage.FindResourcesInRange( null, "Size", 0, 5 );
            resList1 = resList1.Union( _storage.FindResourcesInRange( null, "Size", 5, 10 ));
            // the bug (OM-12195) manifests itself only if the original resource list is instantiated
            Assert.AreEqual( 11, resList1.Count );
            IResourceList resList2 = _storage.FindResourcesInRange( null, "Size", 5, 15 );

            IResourceList intList = resList1.Union( resList2, true );
            Assert.AreEqual( 16, intList.Count );
        }

        [Test] public void Intersect()
        {
            IResource person1 = CreatePerson( "Vasya", null ); person1 = person1;
            IResource person2 = CreatePerson( "Vasya", "Pupkin" ); person2 = person2;
            IResource person3 = CreatePerson( "Kolya", "Pupkin" ); person3 = person3;

            IResourceList rlist1 = _storage.FindResourcesLive( null, _propFirstName, "Vasya" );
            Assert.AreEqual( 2, rlist1.Count );
            IResourceList rlist2 = _storage.FindResourcesLive( null, _propLastName, "Pupkin" );
            Assert.AreEqual( 2, rlist2.Count );

            IResourceList rlist = rlist1.Intersect( rlist2 );
            Assert.AreEqual( 1, rlist.Count );

            IResource person4 = CreatePerson( "Petya", "Pupkin" ); person4 = person4;
            Assert.AreEqual( 1, rlist.Count );

            IResource person5 = CreatePerson( "Vasya", "Pupkin" );
            Assert.AreEqual( 2, rlist.Count );

            person5.Delete();
            Assert.AreEqual( 1, rlist.Count );

            IResourceList rlist_int = rlist.Intersect( _storage.FindResourcesLive( null, "FirstName", "Petya" ) );
            //Assert( Object.ReferenceEquals( rlist_int, rlist ) );
            Assert.AreEqual( 0, rlist_int.Count );
        }

        [Test] public void IntersectHalfLive()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Subject", "Test" );
            email.SetProp( "FirstName", "Dmitry" );

            IResourceList resList1 = _storage.FindResources( SelectionType.LiveSnapshot, "Email", "Subject", "Test" );
            IResourceList resList2 = _storage.FindResourcesLive( "Email", "FirstName", "Dmitry" );

            IResourceList intersectList = resList1.Intersect( resList2 );
            AttachHandlers( intersectList );

            email.SetProp( "Subject", "Not Test" );
            Assert.AreEqual( 0, _deletedResources.Count );

            resList2.Dispose();
            email.Delete();
            Assert.AreEqual( 1, _deletedResources.Count );
        }

        [Test] public void MultiIntersect()
        {
            for( int i=0; i<20; i++ )
            {
                IResource res = _storage.NewResource( "Email" );
                res.SetProp( "Size", i );
            }

            IResourceList resList1 = _storage.FindResourcesInRange( null, "Size", 0, 10 );
            SortSettings ss = new SortSettings( _storage.PropTypes[ "Size" ].Id, false );
            resList1.Sort( ss );

            IResourceList resList2 = _storage.FindResourcesInRange( null, "Size", 5, 15 );
            ss = new SortSettings( _storage.PropTypes[ "Size" ].Id, true );
            resList2.Sort( ss );

            IResourceList intList = resList1.Intersect( resList2 );
            Assert.AreEqual( 6, intList.Count );
        }

        [Test] public void IntersectAllowMerge()
        {
            for( int i=0; i<20; i++ )
            {
                IResource res = _storage.NewResource( "Email" );
                res.SetProp( "Size", i );
            }

            IResourceList resList1 = _storage.FindResourcesInRange( "Email", "Size", 0, 10 );
            // the bug (OM-12195) manifests itself only if the original resource list is instantiated
            Assert.AreEqual( 11, resList1.Count );
            IResourceList resList2 = _storage.FindResourcesInRange( "Email", "Size", 5, 15 );

            IResourceList intList = resList1.Intersect( resList2, true );
            Assert.AreEqual( 6, intList.Count );
        }

        [Test] public void IntersectSort()
        {
            IResource person1 = CreatePerson( "Vasya", null ); person1 = person1;
            IResource person2 = CreatePerson( "Vasya", "Pupkin" ); person2 = person2;
            IResource person3 = CreatePerson( "Kolya", "Pupkin" ); person3 = person3;

            IResourceList rlist1 = _storage.FindResourcesLive( null, _propFirstName, "Vasya" );
            rlist1.Sort( "FirstName LastName" );
            IResourceList rlist2 = _storage.FindResourcesLive( null, _propLastName, "Pupkin" );
            rlist2.Sort( "FirstName LastName" );

            IResourceList rlist = rlist1.Intersect( rlist2 );
            Assert.AreEqual( 2, rlist.SortPropIDs.Length );
        }

        [Test] public void IntersectPlainList()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );

            List<int> list = new List<int>();
            list.Add( person.Id );
            list.Add( email.Id );
            IResourceList plainList = _storage.ListFromIds( list.ToArray(), false );
            IResourceList newList = plainList.Intersect( _storage.GetAllResources( "Person" ) );
            Assert.AreEqual( 1, newList.Count );
            Assert.AreEqual( 2, plainList.Count );
        }

        [Test] public void FindInRange()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Size", 90 );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( "Size", 100 );

            IResource email3 = _storage.NewResource( "Email" );
            email3.SetProp( "Size", 150 );

            IResourceList rlist = _storage.FindResourcesInRange( null, _propSize, 100, 200 );
            Assert.AreEqual( 2, rlist.Count );
        }

        [Test] public void FindInRangeDate()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Received", DateTime.Now );

            DateTime dt1 = DateTime.Now.Date;
            DateTime dt2 = DateTime.Now.Date.AddDays( 1.0 ).AddSeconds( -1.0 );
            IResourceList rlist = _storage.FindResourcesInRange( null, _propReceived, dt1, dt2 );
            Assert.AreEqual( 1, rlist.Count );
        }

        [Test] public void FindInRangeLive()
        {
            DateTime dt1 = DateTime.Now.Date;
            DateTime dt2 = DateTime.Now.Date.AddDays( 1.0 ).AddSeconds( -1.0 );
            IResourceList rlist = _storage.FindResourcesInRangeLive( null, _propReceived, dt1, dt2 );
            AttachHandlers( rlist );

            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Received", DateTime.Now );

            Assert.AreEqual( 1, rlist.Count );
            Assert.AreEqual( 1, _addedResources.Count );
        }

        [Test] public void FindInRangeLiveReverse()
        {
            IResourceList rlist = _storage.FindResourcesInRangeLive( "Email", _propSize, 200, 100 );
            AttachHandlers( rlist );

            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSize, 150 );

            Assert.AreEqual( 1, _addedResources.Count );
        }

        [Test] public void FindInRangeLiveSorting()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Received", DateTime.Now.AddSeconds( -10.0 ) );

            DateTime dt1 = DateTime.Now.Date;
            DateTime dt2 = DateTime.Now.Date.AddDays( 1.0 ).AddSeconds( -1.0 );
            IResourceList rlist = _storage.FindResourcesInRangeLive( null, _propReceived, dt1, dt2 );
            SortSettings ss = new SortSettings( _storage.PropTypes[ "Received" ].Id, false );
            rlist.Sort( ss );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( "Received", DateTime.Now );

            Assert.AreEqual( 2, rlist.Count );
            Assert.AreEqual( email2.Id, rlist [0].Id );
        }

        [Test] public void LiveUnion()
        {
            IResourceList rlist = _storage.FindResourcesInRangeLive( null, _propReceived,
                DateTime.Now.Date, DateTime.Now.AddDays( 1.0 ).AddSeconds( -1.0 ) );
            IResourceList rlist2 = _storage.GetAllResources( "Person" );

            IResourceList rlist3 = rlist.Union( rlist2 );
            rlist3.ResourceAdded += OnResourceAdded;

            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Received", DateTime.Now );

            Assert.AreEqual( 1, rlist3.Count );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void PropTypeMismatch()
        {
            _storage.FindResources( null, _propSize, "Test" );
        }

        [Test] public void ListHasProp()
        {
            IResourceList rlist = _storage.GetAllResourcesLive( "Person" );
            Assert.IsTrue( !rlist.HasProp( "FirstName" ), "empty list should not have property" );

            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "FirstName", "Dmitry" );
            Assert.IsTrue( rlist.HasProp( "FirstName" ), "list should have property" );
        }

        [Test] public void ListPropText()
        {
            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "FirstName", "Dmitry" );

            IResourceList rlist = _storage.GetAllResources( "Person" );
            Assert.AreEqual( "Dmitry", rlist.GetPropText( 0, "FirstName" ) );
            Assert.IsTrue( rlist.HasProp( 0, "FirstName" ) );
            Assert.IsTrue( !rlist.HasProp( 0, "LastName" ) );

            person.SetProp( "LastName", "" );
            Assert.IsTrue( rlist.HasProp( 0, "LastName" ) );
        }

        [Test] public void DeleteAll()
        {
            IResourceList allPersons = _storage.GetAllResourcesLive( "Person" );

            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "FirstName", "Dmitry" );

            IResource person2 = _storage.NewResource( "Person" );
            person2.SetProp( "FirstName", "Ivan" );

            allPersons.DeleteAll();
            Assert.AreEqual( 0, allPersons.Count );
        }

        [Test] public void DeleteAll_IgnoreDeleted()
        {
            IResource person1 = _storage.NewResource( "Person" );
            IResource person2 = _storage.NewResource( "Person" ); person2 = person2;

            IResourceList allPersons = _storage.GetAllResources( "Person" );
            int cnt = allPersons.Count; cnt = cnt;

            person1.Delete();
            allPersons.DeleteAll();

            Assert.AreEqual( 0, _storage.GetAllResources( "Person" ).Count );
        }

        [Test] public void TestLinkChange()
        {
            IResourceList allPersons = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( allPersons );

            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            Assert.AreEqual( 0, _changedResources.Count );

            person.AddLink( _propAuthor, email );
            Assert.AreEqual( 1, _changedResources.Count );
            ResourcePropIndexEventArgs args = (ResourcePropIndexEventArgs) _changedResources [0];
            Assert.AreEqual( LinkChangeType.Add, args.ChangeSet.GetLinkChange( _propAuthor, email.Id ) );

            person.DeleteLink( _propAuthor, email );
            Assert.AreEqual( 2, _changedResources.Count );
            args = (ResourcePropIndexEventArgs) _changedResources [1];
            Assert.AreEqual( LinkChangeType.Delete, args.ChangeSet.GetLinkChange( _propAuthor, email.Id ) );
        }

        [Test] public void BoolPropChangeset()
        {
            IResourceList allEmails = _storage.GetAllResourcesLive( "Email" );
            AttachHandlers( allEmails );

            IResource email = _storage.NewResource( "Email" );
            email.BeginUpdate();
            email.SetProp( _propSubject, "Test" );
            email.SetProp( _propUnread, true );
            email.EndUpdate();

            Assert.AreEqual( 1, _changedResources.Count );
            ResourcePropIndexEventArgs args = (ResourcePropIndexEventArgs) _changedResources [0];
            Assert.IsTrue( args.ChangeSet.IsPropertyChanged( _propUnread ) );

            // setting Unread property again to true should not fire a resource changed event
            email.SetProp( _propUnread, true );
            Assert.AreEqual( 1, _changedResources.Count );
        }

        [Test] public void DeferredUpdates()
        {
            IResourceList allPersons = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( allPersons );

            IResource person = _storage.BeginNewResource( "Person" );
            Assert.AreEqual( 0, _addedResources.Count );
            person.SetProp( "FirstName", "Dmitry" );
            person.SetProp( "LastName", "Jemerov" );
            Assert.AreEqual( 0, _changedResources.Count );

            person.EndUpdate();
            Assert.AreEqual( 1, _addedResources.Count );
            Assert.AreEqual( 0, _changedResources.Count );

            person.BeginUpdate();
            person.SetProp( "FirstName", "Vasya" );
            Assert.IsTrue( person.IsChanged() );
            Assert.AreEqual( 0, _changedResources.Count );
            person.BeginUpdate();  // nested updates
            person.EndUpdate();
            Assert.AreEqual( 0, _changedResources.Count );
            person.EndUpdate();
            Assert.AreEqual( 1, _changedResources.Count );
        }

        [Test] public void EmptyUpdate()
        {
            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "FirstName", "Dmitry" );

            IResourceList allPersons = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( allPersons );

            person.BeginUpdate();
            person.SetProp( "FirstName", "Dmitry" );
            person.SetProp( "IsUnread", false );
            Assert.IsTrue( !person.IsChanged() );
            person.EndUpdate();

            Assert.AreEqual( 0, _changedResources.Count );
        }

        [Test] public void SortPositionChange()
        {
            IResource person1 = _storage.NewResource( "Person" );
            person1.SetProp( "FirstName", "Dmitry" );

            IResource person2 = _storage.NewResource( "Person" );
            person2.SetProp( "FirstName", "Michael" );

            IResourceList allPersons = _storage.GetAllResourcesLive( "Person" );
            allPersons.Sort( "FirstName" );
            Assert.AreEqual( person1.Id, allPersons [0].Id );

            person1.SetProp( "FirstName", "yole" );
            Assert.AreEqual( person2.Id, allPersons [0].Id );
        }

        [Test] public void BatchUpdateProps()
        {
            IResourceList resList = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( resList );

            IResource person = _storage.NewResource( "Person" );
            IResource email1 = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );

            person.BeginUpdate();
            person.SetProp( _propFirstName, "Dmitry" );
            person.SetProp( _propLastName, "Jemerov" );
            person.AddLink( _propAuthor, email1 );
            person.EndUpdate();

            Assert.AreEqual( 1, _changedResources.Count );
            ResourcePropIndexEventArgs args = (ResourcePropIndexEventArgs) _changedResources [0];
            Assert.IsTrue( args.ChangeSet.IsPropertyChanged( _propFirstName ) );
            Assert.IsTrue( args.ChangeSet.IsPropertyChanged( _propLastName ) );
            Assert.IsTrue( !args.ChangeSet.IsPropertyChanged( _propReceived ) );

            _changedResources.Clear();
            person.BeginUpdate();
            person.SetProp( _propFirstName, "Dima" );
            person.SetProp( _propFirstName, "Dimitrij" );
            person.SetProp( _propLastName, "Zhemerov" );
            person.DeleteLink( _propAuthor, email1 );
            person.AddLink( _propAuthor, email2 );
            person.EndUpdate();

            Assert.AreEqual( 1, _changedResources.Count );
            args = (ResourcePropIndexEventArgs) _changedResources [0];
            Assert.AreEqual( "Dmitry", args.ChangeSet.GetOldValue( _propFirstName ) );
            Assert.AreEqual( "Jemerov", args.ChangeSet.GetOldValue( _propLastName ) );
        }

        [Test] public void ChangeSetAfterDeleteProp()
        {
            IResource person = _storage.NewResource( "Person" );
            person.SetProp( _propFirstName, "Dmitry" );
            person.SetProp( _propUnread, true );

            IResourceList resList = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( resList );

            person.DeleteProp( _propFirstName );
            Assert.AreEqual( 1, _changedResources.Count );
            ResourcePropIndexEventArgs args = (ResourcePropIndexEventArgs) _changedResources [0];
            Assert.AreEqual( "Dmitry", args.ChangeSet.GetOldValue( _propFirstName ) );

            _changedResources.Clear();
            person.DeleteProp( _propUnread );
            Assert.AreEqual( 1, _changedResources.Count );
            args = (ResourcePropIndexEventArgs) _changedResources [0];
            Assert.AreEqual( true, args.ChangeSet.GetOldValue( _propUnread ) );
        }

        [Test] public void BatchUpdateLinks()
        {
            IResource email1 = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            IResource person2 = _storage.NewResource( "Person" );

            IResourceList resList = _storage.GetAllResourcesLive( "Email" );
            AttachHandlers( resList );

            email1.BeginUpdate();
            email1.AddLink( _propAuthor, person );
            email1.AddLink( _propReply, person2 );
            email1.EndUpdate();

            Assert.AreEqual( 1, _changedResources.Count );
            ResourcePropIndexEventArgs args = (ResourcePropIndexEventArgs) _changedResources [0];
            Assert.IsTrue( args.ChangeSet.IsPropertyChanged( _propReply ) );
            Assert.IsTrue( args.ChangeSet.IsPropertyChanged( _propAuthor ) );
            Assert.AreEqual( LinkChangeType.Add, args.ChangeSet.GetLinkChange( _propAuthor, person.Id ) );
            Assert.AreEqual( LinkChangeType.Add, args.ChangeSet.GetLinkChange( _propReply, person2.Id ) );

            Assert.AreEqual( LinkChangeType.None, args.ChangeSet.GetLinkChange( _propAuthor, person2.Id ) );
            Assert.IsTrue( !args.ChangeSet.IsPropertyChanged( _propSubject ) );
        }

        [Test] public void DeleteBeforeEndUpdate()
        {
            int propUnread = _storage.PropTypes.Register( "IUnread", PropDataType.Int );

            IResourceList resList = _storage.FindResourcesLive( null, propUnread, 1 );
            AttachHandlers( resList );

            IResourceList resListSnap = _storage.FindResources( SelectionType.LiveSnapshot, null, propUnread, 1 );

            IResource email = _storage.NewResource( "Email" );
            email.SetProp( propUnread, 1 );

            email.BeginUpdate();
            email.SetProp( propUnread, 0 );
            email.Delete();

            Assert.AreEqual( 0, resList.Count );
            Assert.AreEqual( 0, resListSnap.Count );
            Assert.AreEqual( 1, _deletedResources.Count );

            email.EndUpdate();
        }

        [Test] public void DeleteNewBeforeEndUpdate()
        {
            IResourceList resList = _storage.FindResourcesLive( null, _propUnread, true );
            AttachHandlers( resList );

            IResource email = _storage.BeginNewResource( "Email" );
            email.SetProp( _propUnread, true );
            email.Delete();

            Assert.AreEqual( 0, resList.Count );
        }

        [Test, ExpectedException( typeof(ResourceDeletedException) )]
        public void BeginUpdateAfterDelete()
        {
            IResource email = _storage.NewResource( "Email" );
            email.Delete();

            email.BeginUpdate();
            email.EndUpdate();
        }

        [Test] public void Minus()
        {
            IResource person1 = CreatePerson( "Dmitry", "Jemerov" );
            IResource person2 = CreatePerson( "Boris", "Jemerov" );

            IResourceList jemerovs = _storage.FindResourcesLive( "Person", _propLastName, "Jemerov" );
            IResourceList dmitries = _storage.FindResourcesLive( "Person", _propFirstName, "Dmitry" );

            IResourceList minus = jemerovs.Minus( dmitries );
            Assert.AreEqual( 1, minus.Count );
            Assert.AreEqual( person2.Id, minus [0].Id );

            person1.SetProp( "FirstName", "Dima" );
            Assert.AreEqual( 2, minus.Count );
            person1.SetProp( "FirstName", "Dmitry" );
            Assert.AreEqual( 1, minus.Count );

            IResource person3 = CreatePerson( "Nikolai", "Jemerov" );
            Assert.AreEqual( 2, minus.Count );

            person3.SetProp( "LastName", "Zhemerov" );
            Assert.AreEqual( 1, minus.Count );
        }

        [Test] public void Minus2()
        {
            IResourceList allPersons = _storage.GetAllResourcesLive( "Person" );
            IResourceList jemerovs = _storage.FindResourcesLive( "Person", "LastName", "Jemerov" );
            allPersons = allPersons.Minus( jemerovs );
            AttachHandlers( allPersons );

            IResource res = _storage.BeginNewResource( "Person" );
            res.SetProp( "LastName", "Jemerov" );
            res.EndUpdate();

            Assert.AreEqual( 0, _addedResources.Count );
        }

        [Test] public void LiveSingleResourceList()
        {
            IResource person = _storage.NewResource( "Person" );
            IResourceList personList = person.ToResourceListLive();
            AttachHandlers( personList );

            person.SetProp( "FirstName", "Dmitry" );
            Assert.AreEqual( 1, _changedResources.Count );

            person.Delete();
            Assert.AreEqual( 1, _deletedResources.Count );
        }

        [Test] public void LiveSnapshot()
        {
            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "FirstName", "Dmitry" );

            IResourceList personList = _storage.FindResources( SelectionType.LiveSnapshot, "Person", "FirstName", "Dmitry" );
            AttachHandlers( personList );
            Assert.AreEqual( 1, personList.Count );

            person.SetProp( "FirstName", "Dima" );
            Assert.AreEqual( 1, _changedResources.Count );
            Assert.AreEqual( 1, personList.Count );

            person.SetProp( "FirstName", "Dmitry" );
            Assert.AreEqual( 2, _changedResources.Count );
            Assert.AreEqual( 1, personList.Count );

            person.SetProp( "FirstName", "Dima" );

            IResource person2 = _storage.NewResource( "Person" );
            person2.SetProp( "FirstName", "Dmitry" );
            Assert.AreEqual( 1, _addedResources.Count );
            Assert.AreEqual( 2, personList.Count );

            person.Delete();
            Assert.AreEqual( 1, personList.Count );
            Assert.AreEqual( 1, _deletedResources.Count );
        }

        [Test] public void AffectsDisplayName()
        {
            IResource person = _storage.NewResource( "Person" );
            IResourceList personList = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( personList );

            person.SetProp( _propFirstName, "Dmitry" );

            Assert.AreEqual( 1, _changedResources.Count );
            ResourcePropIndexEventArgs args = (ResourcePropIndexEventArgs) _changedResources [0];
            Assert.IsTrue( args.ChangeSet.IsDisplayNameAffected );

            person.SetProp( _propSize, 100 );
            Assert.AreEqual( 2, _changedResources.Count );
            args = (ResourcePropIndexEventArgs) _changedResources [1];
            Assert.IsTrue( !args.ChangeSet.IsDisplayNameAffected );

            person.BeginUpdate();
            person.SetProp( _propSize, 150 );
            person.SetProp( _propFirstName, "Dima" );
            person.EndUpdate();

            Assert.AreEqual( 3, _changedResources.Count );
            args = (ResourcePropIndexEventArgs) _changedResources [2];
            Assert.IsTrue( args.ChangeSet.IsDisplayNameAffected );

            person.DeleteProp( _propFirstName );
            Assert.AreEqual( 4, _changedResources.Count );
            args = (ResourcePropIndexEventArgs) _changedResources [3];
            Assert.IsTrue( args.ChangeSet.IsDisplayNameAffected );

            person.DisplayName = "Test";
            Assert.AreEqual( 5, _changedResources.Count );
            args = (ResourcePropIndexEventArgs) _changedResources [4];
            Assert.IsTrue( args.ChangeSet.IsDisplayNameAffected );

            _changedResources.Clear();
            personList.Dispose();

            _storage.ResourceTypes.Register( "Message", "Author" );
            IResource message = _storage.NewResource( "Message" );

            IResourceList messageList = _storage.GetAllResourcesLive( "Message" );
            AttachHandlers( messageList );

            message.SetProp( _propAuthor, person );
            Assert.AreEqual( 1, _changedResources.Count );
            args = (ResourcePropIndexEventArgs) _changedResources [0];
            Assert.IsTrue( args.ChangeSet.IsDisplayNameAffected );
        }

        [Test] public void AffectsDisplayName_BatchDeleteProp()
        {
            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "FirstName", "Dmitry" );
            person.SetProp( "LastName", "Jemerov" );

            IResourceList personList = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( personList );

            person.BeginUpdate();
            person.SetProp( "Size", 100 );
            person.DeleteProp( "LastName" );
            person.EndUpdate();

            Assert.AreEqual( 1, _changedResources.Count );
            ResourcePropIndexEventArgs args = (ResourcePropIndexEventArgs) _changedResources [0];
            Assert.IsTrue( args.ChangeSet.IsDisplayNameAffected );
        }

        [Test] public void EventsAfterDispose()
        {
            IResource person = _storage.NewResource( "Person" );
            IResourceList personList = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( personList );

            person.SetProp( _propFirstName, "Dmitry" );
            Assert.AreEqual( 1, _changedResources.Count );

            personList.Dispose();
            person.SetProp( _propLastName, "Jemerov" );
            Assert.AreEqual( 1, _changedResources.Count );
        }

        [Test] public void PropertyWatch()
        {
            IResource person = _storage.NewResource( "Person" );
            IResourceList personList = _storage.GetAllResourcesLive( "Person" );
            personList.AddPropertyWatch( _propFirstName );
            AttachHandlers( personList );

            person.SetProp( _propLastName, "Dmitry" );
            Assert.AreEqual( 0, _changedResources.Count );

            person.SetProp( _propFirstName, "Dima" );
            Assert.AreEqual( 1, _changedResources.Count );

            person.BeginUpdate();
            person.SetProp( _propLastName, "Jemerov" );
            person.SetProp( _propFirstName, "Dmitry" );
            person.EndUpdate();

            Assert.AreEqual( 2, _changedResources.Count );

            person.BeginUpdate();
            person.SetProp( _propLastName, "Zhemerov" );
            person.SetProp( _propSize, 100 );
            person.EndUpdate();

            Assert.AreEqual( 2, _changedResources.Count );

            IResource email = _storage.NewResource( "Email" );
            email.AddLink( "Reply", person );   // reverse link => negative propID

            Assert.AreEqual( 2, _changedResources.Count );

            IResource email2 = _storage.NewResource( "Email" );

            person.BeginUpdate();
            email2.AddLink( "Reply", person );   // reverse link => negative propID, multi-prop changeset
            person.SetProp( "LastName", "Iemerov" );
            person.EndUpdate();

            Assert.AreEqual( 2, _changedResources.Count );
        }

        [Test, ExpectedException(typeof(StorageException))]
        public void InvalidPropWatch()
        {
            IResourceList personList = _storage.GetAllResourcesLive( "Person" );
            personList.AddPropertyWatch( 65535 );
        }

        [Test] public void WatchDisplayName()
        {
            IResource person = _storage.NewResource( "Person" );
            IResourceList personList = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( personList );
            personList.AddPropertyWatch( ResourceProps.DisplayName );

            person.SetProp( _propLastName, "Jemerov" );
            Assert.AreEqual( 1, _changedResources.Count );

            person.SetProp( _propSize, 100 );
            Assert.AreEqual( 1, _changedResources.Count );
        }

        [Test] public void HandlerOnlyList()
        {
            IResourceList allPersons = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( allPersons );

            IResource person = _storage.NewResource( "Person" );
            Assert.AreEqual( 1, _addedResources.Count );

            person.SetProp( "Name", "Dmitry" );
            Assert.AreEqual( 1, _changedResources.Count );

            person.Delete();
            Assert.AreEqual( 1, _deletedResources.Count );
        }

        [Test] public void StringListPropChanges()
        {
            IResource res = _storage.NewResource( "Person" );

            IResourceList allPersons = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( allPersons );

            IStringList stringList = res.GetStringListProp( _propValueList );
            Assert.AreEqual( 0, _changedResources.Count );

            stringList.Add( "Test" );
            Assert.AreEqual( 1, _changedResources.Count );

            stringList.RemoveAt( 0 );
            Assert.AreEqual( 2, _changedResources.Count );
        }

        [Test] public void FindResourcesByStringList()
        {
            IResource res = _storage.NewResource( "Person" );
            IStringList propList = res.GetStringListProp( _propValueList );
            propList.Add( "Dmitry" );
            propList.Add( "Michael" );

            IResourceList dmitries = _storage.FindResources( "Person", _propValueList, "Dmitry" );
            Assert.AreEqual( 1, dmitries.Count );

            IResourceList michaels = _storage.FindResources( "Person", _propValueList, "Michael" );
            Assert.AreEqual( 1, michaels.Count );
        }

        [Test] public void FindResourcesByStringListLive()
        {
            IResource res = _storage.NewResource( "Person" );
            IResourceList dmitries = _storage.FindResourcesLive( "Person", _propValueList, "Dmitry" );
            Assert.AreEqual( 0, dmitries.Count );

            IStringList propList = res.GetStringListProp( _propValueList );
            propList.Add( "Dmitry" );
            Assert.AreEqual( 1, dmitries.Count );
        }

        [Test] public void Deinstantiate()
        {
            IResource res = _storage.NewResource( "Person" ); res = res;
            IResourceList resList = _storage.GetAllResourcesLive( "Person" );
            AttachHandlers( resList );
            Assert.AreEqual( 1, resList.Count );

            resList.Deinstantiate();
            IResource res2 = _storage.NewResource( "Person" ); res2 = res2;
            Assert.AreEqual( 0, _addedResources.Count );

            Assert.AreEqual( 2, resList.Count );  // calling Count re-instantiates the list
        }

        [Test] public void AllowReadDeleted()
        {
            IResource res = _storage.NewResource( "Person" );
            IResourceList resList = _storage.GetAllResources( "Person" );
            Assert.IsTrue( resList.Count == 1 );

            res.Delete();
            IResource res1 = resList [0];
            Assert.IsTrue( res1.IsDeleted );

            foreach( IResource res2 in resList )
            {
                Assert.IsTrue( res2.IsDeleted );
            }
        }

        [Test] public void TransientIDUpdate()
        {
            IResource res = _storage.NewResourceTransient( "Person" );
            IResourceList resList = res.ToResourceList();
            Assert.AreEqual( 1, resList.Count );
            res.EndUpdate();
            Assert.AreEqual( resList [0].Id, res.Id );
        }

        [Test] public void GetAllTypesSorted()
        {
            IResource person = _storage.NewResource( "Person" ); person = person;
            IResource email = _storage.NewResource( "Email" ); email = email;

            IResourceList rl = _storage.GetAllResources( "Person" );
            rl = rl.Union( _storage.GetAllResources( "Email" ) );

            string[] allTypes = rl.GetAllTypes();
            Assert.AreEqual( "Email", allTypes [0] );
            Assert.AreEqual( "Person", allTypes [1] );
        }

        [Test] public void CaseInsenstiveSelect()
        {
            IResource song1 = CreateEmail( "A song" ); song1 = song1;
            IResource song2 = CreateEmail( "a song" ); song2 = song2;

            IResourceList songs = _storage.FindResourcesLive( null, _propSubject, "A song" );
            Assert.AreEqual( 2, songs.Count );

            IResource song3 = CreateEmail( "a song" ); song3 = song3;
            Assert.AreEqual( 3, songs.Count );
        }

        [Test] public void OldValueIfChangeDuringUpdate()
        {
            IResource person = _storage.NewResource( "Person" );
            IResourceList persons = _storage.GetAllResourcesLive( "Person" );
            persons.ResourceChanged += HandlePersonChanged ;
            _changeCount = 0;
            person.BeginUpdate();
            person.SetProp( _propUnread, true );
            person.EndUpdate();

            Assert.IsTrue( _changeSet1.IsPropertyChanged( _propUnread ) );
            Assert.IsTrue( !_changeSet2.IsPropertyChanged( _propUnread ) );
        }

        private void HandlePersonChanged( object sender, ResourcePropIndexEventArgs e )
        {
            _changeCount++;
            if ( _changeCount == 1 )
            {
                _changeSet1 = e.ChangeSet;
                e.Resource.BeginUpdate();
                e.Resource.SetProp( "Name", "Dmitry" );
                e.Resource.EndUpdate();
            }
            else
            {
                _changeSet2 = e.ChangeSet;
            }
        }

        private int _changeCount = 0;
        private IPropertyChangeSet _changeSet1 = null;
        private IPropertyChangeSet _changeSet2 = null;

        [Test] public void UnionWithEmptyList()
        {
            ResourceList resList = (ResourceList) _storage.GetAllResourcesLive( "Person" );
            resList = (ResourceList) resList.Union( _storage.ListFromIds( new int[] {}, true ) );

            Assert.AreEqual( 0, resList.Count ); // force instantiate and optimize
            Assert.AreEqual( "Cache(Type(Person))", resList.ListTypeToString() );
        }

        [Test] public void UnionPlainLists()
        {
            IResource res = _storage.NewResource( "Person" );
            IResource res2 = _storage.NewResource( "Person" );
            IResourceList resList = _storage.ListFromIds( new int[] { res.Id }, true );
            resList = resList.Union( _storage.ListFromIds( new int[] { res2.Id }, true ) );

            Assert.AreEqual( 2, resList.Count ); // force instantiate and optimize
            Assert.AreEqual( "List(" + res.Id + "," + res2.Id + ")", (resList as ResourceList).ListTypeToString() );
        }

        [Test] public void PropagateOptimizeToIntersection()
        {
            IResourceList resList = _storage.GetAllResourcesLive( "Person" );
            _storage.CachePredicate( resList );

            IResource email = _storage.NewResource( "Email" );
            IResourceList links = email.GetLinksOfTypeLive( "Person", _propAuthor );
            Assert.AreEqual( 0, links.Count );
            Assert.AreEqual( "Intersection(Cache(Type(Person)),Link(Author," + email.Id + "))", (links as ResourceList).ListTypeToString() );
        }

        [Test] public void SnapshotNotEqualsPlain()
        {
            IResourceList resList = _storage.FindResourcesWithPropLive( null, _propUnread );
            _storage.CachePredicate( resList );

            IResourceList resList2 = _storage.FindResourcesWithProp( SelectionType.LiveSnapshot, null, _propUnread );
            Assert.AreEqual( 0, resList2.Count );
            Assert.AreEqual( "ResourcesWithProp(IsUnread)", (resList2 as ResourceList).ListTypeToString() );
        }

        [Test] public void EventsForCached()
        {
            IResourceList resList = _storage.GetAllResourcesLive( "Person" );
            _storage.CachePredicate( resList );

            IResource person = _storage.NewResource( "Person" );

            IResourceList resList2 = _storage.GetAllResourcesLive( "Person" );
            Assert.AreEqual( 1, resList2.Count );
            Assert.AreEqual( "Cache(Type(Person))", (resList2 as ResourceList).ListTypeToString() );

            person.Delete();
            Assert.AreEqual( 0, resList2.Count );

        }

        [Test] public void ValidResourcesEnumerator()
        {
            IResource person1 = _storage.NewResource( "Person" );
            IResource person2 = _storage.NewResource( "Person" );
            IResourceList allPersons = _storage.GetAllResources( "Person" );
            Assert.AreEqual( 2, allPersons.Count );  // force instantiation
            person1.Delete();

            IEnumerator enumerator = allPersons.ValidResources.GetEnumerator();
            enumerator.MoveNext();
            Assert.AreEqual( person2, enumerator.Current );
            Assert.IsFalse( enumerator.MoveNext() );
        }
    }
}

