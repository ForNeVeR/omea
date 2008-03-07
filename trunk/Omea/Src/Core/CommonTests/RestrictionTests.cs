/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceStore;
using NUnit.Framework;

namespace CommonTests
{
    [TestFixture]
    public class RestrictionTests: MyPalDBTests
	{
        [SetUp]
        public void SetUp()
        {
            InitStorage();
            RegisterResourcesAndProperties();
        }

        [TearDown]
        public void TearDown()
        {
            CloseStorage();
        }

        [Test]
        public void LinkRestrictionsAreOK()
        {
            int fromLink  = _storage.PropTypes.Register( "From", PropDataType.Link );
            int toLink  = _storage.PropTypes.Register( "To", PropDataType.Link );
            int aLink  = _storage.PropTypes.Register( "aLink", PropDataType.Link );
            
            Assert.AreEqual( 0, _storage.GetMinLinkCountRestriction( "Email", fromLink ) );
            Assert.AreEqual( Int32.MaxValue, _storage.GetMaxLinkCountRestriction( "Email", fromLink ) );

            _storage.RegisterLinkRestriction( "Email", fromLink, null, 1, 1 );
            _storage.RegisterLinkRestriction( "Email", toLink, null, 1, 1 );
            _storage.RegisterLinkRestriction( "Email", aLink, null, 0, 1 );

            Assert.AreEqual( 1, _storage.GetMinLinkCountRestriction( "Email", fromLink ) );
            Assert.AreEqual( 1, _storage.GetMaxLinkCountRestriction( "Email", fromLink ) );

            IResource res = _storage.BeginNewResource( "Email" );
            IResource from = _storage.NewResource( "Person" );
            IResource to = _storage.NewResource( "Person" );
            res.AddLink( fromLink, from );
            res.AddLink( toLink, to );
            res.EndUpdate();
        }

        [Test, ExpectedException( typeof(ResourceRestrictionException) )]
        public void NotEnoughLinks()
        {
            int fromLink  = _storage.PropTypes.Register( "From", PropDataType.Link );
            int toLink  = _storage.PropTypes.Register( "To", PropDataType.Link );
                        _storage.RegisterLinkRestriction( "Email", toLink, null, 1, 1 );
            IResource res = _storage.BeginNewResource( "Email" );
            IResource from = _storage.NewResource( "Person" );
            res.AddLink( fromLink, from );
            // the following is necessary link, so exception should be thrown
            //res.AddLink( toLink, to );
            res.EndUpdate();
        }

        [Test, ExpectedException( typeof(ResourceRestrictionException) )]
        public void TooManyLinks()
        {
            _storage.ResourceTypes.Register( "Email", "Email", "Subject" );
            _storage.ResourceTypes.Register( "Contact", "Contact", "" );
            int fromLink  = _storage.PropTypes.Register( "From", PropDataType.Link );
            int toLink  = _storage.PropTypes.Register( "To", PropDataType.Link );
            _storage.RegisterLinkRestriction( "Email", fromLink, null, 1, 1 );
            _storage.RegisterLinkRestriction( "Email", toLink, null, 1, 1 );
            IResource res = _storage.BeginNewResource( "Email" );
            IResource from = _storage.NewResource( "Contact" );
            IResource to = _storage.NewResource( "Contact" );
            IResource to2 = _storage.NewResource( "Contact" );
            res.AddLink( fromLink, from );
            res.AddLink( toLink, to );
            // the following is excess link, so exception should be thrown
            res.AddLink( toLink, to2 );
            res.EndUpdate();
        }

        [Test, ExpectedException( typeof(ResourceRestrictionException)) ]
        public void LinkRestrictionReverse()
        {
            int toLink  = _storage.PropTypes.Register( "ToLink", PropDataType.Link );
            _storage.RegisterLinkRestriction( "Email", toLink, null, 0, 1 );

            IResource res = _storage.NewResource( "Email" );
            IResource to = _storage.NewResource( "Person" );
            IResource to2 = _storage.NewResource( "Person" );

            to.AddLink( toLink, res );
            to2.AddLink( toLink, res );
        }

        [Test, ExpectedException( typeof(ResourceRestrictionException) )]
        public void NotEnoughLinksAfterReopen()
        {
            int fromLink  = _storage.PropTypes.Register( "From", PropDataType.Link );
            _storage.RegisterLinkRestriction( "Email", fromLink, null, 1, 1 );
            ReopenStorage();

            IResource res = _storage.NewResource( "Email" ); res = res;
        }

        [Test] public void SetPropWithRestriction()
        {
            int fromLink  = _storage.PropTypes.Register( "From", PropDataType.Link );
            _storage.RegisterLinkRestriction( "Email", fromLink, null, 1, 1 );

            IResource from1 = _storage.NewResource( "Person" );
            IResource from2 = _storage.NewResource( "Person" );
            IResource res = _storage.BeginNewResource( "Email" );
            res.AddLink( fromLink, from1 );
            res.EndUpdate();

            res.SetProp( fromLink, from2 );  // shouldn't cause any exceptions
        }

        [Test] public void DirectedLinkRestriction()
        {
            int parentLink = _storage.PropTypes.Register( "Parent", PropDataType.Link, PropTypeFlags.DirectedLink );
            _storage.RegisterLinkRestriction( "Email", parentLink, "Email", 0, 1 );

            IResource email0 = _storage.NewResource( "Email" );
            IResource email1 = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            IResource email3 = _storage.NewResource( "Email" );
            email2.AddLink( parentLink, email1 );
            email3.AddLink( parentLink, email1 );
            email1.AddLink( parentLink, email0 );
        }

        [Test, ExpectedException( typeof(ResourceRestrictionException ) )]
        public void DirectedLinkBrokenRestriction()
        {
            int parentLink = _storage.PropTypes.Register( "Parent", PropDataType.Link, PropTypeFlags.DirectedLink );
            _storage.RegisterLinkRestriction( "Email", parentLink, null, 0, 1 );

            IResource email0 = _storage.NewResource( "Email" );
            IResource email1 = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            email0.AddLink( parentLink, email1 );
            email0.AddLink( parentLink, email2 );
        }

        [Test, ExpectedException( typeof(ResourceRestrictionException ) )]
        public void DirectedLinkRestrictionDelete()
        {
            _storage.ResourceTypes.Register( "Root", "Name" );
            int parentLink = _storage.PropTypes.Register( "Parent", PropDataType.Link, PropTypeFlags.DirectedLink );
            _storage.RegisterLinkRestriction( "Email", parentLink, null, 1, 1 );

            IResource root = _storage.NewResource( "Root" );
            IResource email = _storage.BeginNewResource( "Email" );
            email.AddLink( parentLink, root );
            email.EndUpdate();

            root.DeleteLink( parentLink, email );
        }

        [Test, ExpectedException( typeof(ResourceRestrictionException ) )]
        public void UniqueRestriction()
        {
            _storage.RegisterUniqueRestriction( "Email", _propSubject );

            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( "Subject", "A" );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( "Subject", "A" );
        }

        [Test, ExpectedException( typeof(ResourceRestrictionException ) )]
        public void UniqueRestrictionAfterReopen()
        {
            _storage.RegisterUniqueRestriction( "Email", _propSubject );
            ReopenStorage();

            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( "Subject", "A" );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( "Subject", "A" );
        }

        [Test] public void DuplicateRegistration()
        {
            int initialRestrictionCount = _storage.GetAllResources( "UniqueRestriction" ).Count;

            _storage.RegisterUniqueRestriction( "Email", _propSubject );
            ReopenStorage();
            _storage.RegisterUniqueRestriction( "Email", _propSubject );

            Assert.AreEqual( initialRestrictionCount+1, _storage.GetAllResources( "UniqueRestriction" ).Count );
        }

        [Test] public void DeleteRestriction()
        {
            _storage.RegisterUniqueRestriction( "Email", _propSubject );

            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( "Subject", "A" );

            _storage.DeleteUniqueRestriction( "Email", _propSubject );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( "Subject", "A" );   // no exception here
        }

        [Test] public void RepairRequired()
        {
            _storage.RegisterUniqueRestriction( "Email", _propSubject );

            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( "Subject", "A" );

            IResource email2 = _storage.NewResource( "Email" );
            try
            {
                email2.SetProp( "Subject", "A" );
            }
            catch( ResourceRestrictionException )
            {
                // ignore
            }

            Assert.IsTrue( _storage.RepairRequired );
            ReopenStorage();
            Assert.IsTrue( !_storage.RepairRequired );

            IResource email = _storage.FindUniqueResource( "Email", "Subject", "A" ); email = email;
            Assert.IsTrue( _storage.RepairRequired );

            ResourceStoreRepair repair = new ResourceStoreRepair( null );
            repair.FixErrors = true;
            repair.RepairRestrictions();

            IResourceList resultList = _storage.FindResources( "Email", "Subject", "A" );
            Assert.AreEqual( 1, resultList.Count );
            Assert.AreEqual( email1.Id, resultList [0].Id );
        }

        [Test] public void TooManyRestrictionsRepair()  // see bug #5482
        {
            IResource email = _storage.NewResource( "Email" );
            IResource from1 = _storage.NewResource( "Person" );
            IResource from2 = _storage.NewResource( "Person" );
            IResource from3 = _storage.NewResource( "Person" );

            email.AddLink( _propAuthor, from1 );
            email.AddLink( _propAuthor, from2 );
            email.AddLink( _propAuthor, from3 );

            _storage.RegisterLinkRestriction( "Email", _propAuthor, null, 0, 1 );

            ResourceStoreRepair repair = new ResourceStoreRepair( null );
            repair.FixErrors = true;
            repair.RepairRestrictions();

            IResourceList links = email.GetLinksOfType( null, _propAuthor );
            Assert.AreEqual( 1, links.Count );
            Assert.AreEqual( from1.Id, links [0].Id );
        }

        [Test] public void NotEnoughRestrictionsRepair()  // #5603
        {
            int testLink = _storage.PropTypes.Register( "TestLink", PropDataType.Link );
            IResource email = _storage.NewResource( "Email" );
            IResource from1 = _storage.NewResource( "Person" );

            email.AddLink( _propAuthor, from1 );

            _storage.RegisterLinkRestriction( "Email", _propAuthor, null, 1, Int32.MaxValue );
            _storage.RegisterLinkRestriction( "Person", testLink, null, 1, Int32.MaxValue );

            ResourceStoreRepair repair = new ResourceStoreRepair( null );
            repair.FixErrors = true;
            repair.RepairRestrictions();

            Assert.IsTrue( !email.IsDeleted );
            Assert.IsTrue( !from1.IsDeleted );
        }

        [Test] public void RepairUniqueRestrictionWithLinkRestrictions()
        {
            int testLink = _storage.PropTypes.Register( "TestLink", PropDataType.Link ); testLink = testLink;
            IResource email1 = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            IResource from1 = _storage.NewResource( "Person" );
            IResource from2 = _storage.NewResource( "Person" );

            email1.AddLink( _propAuthor, from1 );
            email2.AddLink( _propAuthor, from2 );

            from1.SetProp( "Name", "Dmitry" );
            from2.SetProp( "Name", "Dmitry" );

            _storage.RegisterLinkRestriction( "Email", _propAuthor, null, 1, Int32.MaxValue );
            _storage.RegisterUniqueRestriction( "Person", _storage.PropTypes ["Name"].Id );

            ResourceStoreRepair repair = new ResourceStoreRepair( null );
            repair.FixErrors = true;
            repair.RepairRestrictions();

            Assert.IsTrue( !from1.IsDeleted );
            Assert.IsTrue( !from2.IsDeleted );
        }

        [Test] public void DirectedLinkRestrictionRepair()
        {
            int parentLink = _storage.PropTypes.Register( "ParentLink", PropDataType.Link, PropTypeFlags.DirectedLink );
            
            IResource email = _storage.NewResource( "Email" );
            IResource from1 = _storage.NewResource( "Person" );
            IResource from2 = _storage.NewResource( "Person" );
            IResource from3 = _storage.NewResource( "Person" );

            from1.AddLink( parentLink, email );
            from2.AddLink( parentLink, email );
            from3.AddLink( parentLink, email );

            _storage.RegisterLinkRestriction( "Email", parentLink, null, 0, 1 );

            ResourceStoreRepair repair = new ResourceStoreRepair( null );
            repair.FixErrors = true;
            repair.RepairRestrictions();

            IResourceList links = email.GetLinksOfType( null, parentLink );
            Assert.AreEqual( 3, links.Count );
        }

        [Test] public void TypedLinkRestrictionRepair()
        {
            int replyLink = _storage.PropTypes.Register( "Reply", PropDataType.Link, PropTypeFlags.DirectedLink );
            IResource email = _storage.NewResource( "Email" );
            IResource from1 = _storage.NewResource( "Person" );
            email.AddLink( replyLink, from1 );

            _storage.RegisterLinkRestriction( "Email", replyLink, "Email", 1, 1 );

            ResourceStoreRepair repair = new ResourceStoreRepair( null );
            repair.FixErrors = true;
            repair.RepairRestrictions();
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void RegisterLinkRestriction_BadType()
        {
            _storage.RegisterLinkRestriction( "Someshit", _propAuthor, null, 0, 1 );
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void RegisterUniqueRestriction_NoType()
        {
            _storage.RegisterUniqueRestriction( "Someshit", _propSubject );
        }

        [Test, ExpectedException(typeof(ResourceRestrictionException))] 
        public void CustomRestriction()
        {
            _storage.RegisterCustomRestriction( "Email", _propSize, new CustomResourceRestriction() );
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSize, 200 );
        }

        [Test, ExpectedException(typeof(ResourceRestrictionException))] 
        public void CustomRestrictionAfterReopen()
        {
            _storage.RegisterCustomRestriction( "Email", _propSize, new CustomResourceRestriction() );
            ReopenStorage();
            _storage.RegisterCustomRestriction( "Email", _propSize, new CustomResourceRestriction() );
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSize, 200 );
        }

        [Test, ExpectedException(typeof(ResourceRestrictionException))]
        public void MissingRestrictionAfterReopen()
        {
            _storage.RegisterCustomRestriction( "Email", _propSize, new CustomResourceRestriction() );
            ReopenStorage();
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSize, 50 );
        }

        [Test] public void DeleteCustomRestriction()
        {
            _storage.RegisterCustomRestriction( "Email", _propSize, new CustomResourceRestriction() );
            _storage.DeleteCustomRestriction( "Email", _propSize );
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSize, 200 );
        }

        [Test, ExpectedException( typeof(ResourceRestrictionException))] 
        public void RestrictionOnDelete()
        {
            _storage.RegisterRestrictionOnDelete( "Email", new CustomResourceRestriction() );
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSize, 200 );
            email.Delete();
        }

        [Test] public void RestrictionOnDelete_Pass()
        {
            _storage.RegisterRestrictionOnDelete( "Email", new CustomResourceRestriction() );
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSize, 50 );
            email.Delete();
        }

        [Test] public void RestrictionOnDelete_NotDelete()
        {
            _storage.RegisterRestrictionOnDelete( "Email", new CustomResourceRestriction() );
            IResource email = _storage.BeginNewResource( "Email" );
            email.SetProp( _propSize, 200 );
            email.EndUpdate();
        }

        [Test] public void DeleteUniqueRestrictionWithPropType()
        {
            int initialRestrictionCount = _storage.GetAllResources( "UniqueRestriction" ).Count;
            int propId = _storage.PropTypes.Register( "Test", PropDataType.String );
            _storage.RegisterUniqueRestriction( "Email", propId );
            _storage.PropTypes.Delete( propId );
            Assert.AreEqual( initialRestrictionCount, _storage.GetAllResources( "UniqueRestriction" ).Count );
            
            // verify exception OM-8941 does not happen
            _storage.NewResource( "Email" );
        }

        [Test] public void DeleteLinkRestrictionWithPropType()
        {
            int initialRestrictionCount = _storage.GetAllResources( "LinkRestriction" ).Count;
            int propId = _storage.PropTypes.Register( "Test", PropDataType.Link );
            _storage.RegisterLinkRestriction( "Email", propId, null, 1, 1 );
            _storage.PropTypes.Delete( propId );
            Assert.AreEqual( initialRestrictionCount, _storage.GetAllResources( "LinkRestriction" ).Count );
            
            // verify exception OM-8941 does not happen
            _storage.NewResource( "Email" );
        }

        [Test] public void InvalidPropTypeInUniqueRestriction()
        {
            int initialRestrictionCount = _storage.GetAllResources( "UniqueRestriction" ).Count;

            IResource uniqueRestriction = _storage.NewResource( "UniqueRestriction" );
            uniqueRestriction.SetProp( "fromResourceType", "Email" );
            uniqueRestriction.SetProp( "UniquePropId", 4096 );

            ResourceStoreRepair repair = new ResourceStoreRepair( null );
            repair.FixErrors = true;
            repair.RepairRestrictions();

            Assert.IsTrue( uniqueRestriction.IsDeleted );
            Assert.AreEqual( initialRestrictionCount, _storage.GetAllResources( "UniqueRestriction" ).Count );
        }

        [Test] public void InvalidPropTypeInLinkRestriction()
        {
            int initialRestrictionCount = _storage.GetAllResources( "LinkRestriction" ).Count;

            IResource linkRestriction = _storage.NewResource( "LinkRestriction" );
            linkRestriction.SetProp( "fromResourceType", "Email" );
            linkRestriction.SetProp( "LinkType", 4096 );

            ResourceStoreRepair repair = new ResourceStoreRepair( null );
            repair.FixErrors = true;
            repair.RepairRestrictions();

            Assert.IsTrue( linkRestriction.IsDeleted );
            Assert.AreEqual( initialRestrictionCount, _storage.GetAllResources( "LinkRestriction" ).Count );
        }

        [Test] public void LinkCountAndDeleteLink()
        {
            int fromLink  = _storage.PropTypes.Register( "From", PropDataType.Link );
            IResource res = _storage.NewResource( "Person" );
            IResource email1 = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            IResource email3 = _storage.NewResource( "Email" );
            res.AddLink( fromLink, email1 );
            res.AddLink( fromLink, email2 );
            res.AddLink( fromLink, email3 );

            _storage.RegisterLinkRestriction( "Person", fromLink, "Email", 0, 1 );            
            res.DeleteLink( fromLink, email3 );
        }

        private class CustomResourceRestriction: IResourceRestriction
        {
            public void CheckResource( IResource res )
            {
                if ( res.GetIntProp( "Size" ) > 100 )
                    throw new ResourceRestrictionException( "Resource is too big" );
            }
        }
    }
}
