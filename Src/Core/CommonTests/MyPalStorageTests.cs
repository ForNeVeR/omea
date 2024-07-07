// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using NUnit.Framework;

using JetBrains.Omea.OpenAPI;

namespace CommonTests
{
    /**
     * Tests for the core ResourceStore functionality.
     */

    [TestFixture]
    public class MyPalStorageTests: MyPalDBTests
    {
        [SetUp]
        public void SetUp()
        {
            InitStorage();
        }

        [TearDown]
        public void TearDown()
        {
            CloseStorage();
        }

        [Test] public void RegisterPropType()
        {
            int ID = _storage.PropTypes.Register( "Author", PropDataType.Link );
            Assert.AreEqual( "Author", _storage.PropTypes [ID].Name );
            Assert.AreEqual( PropDataType.Link, _storage.PropTypes [ID].DataType );
            Assert.AreEqual( ID, _storage.GetPropId( "Author" ) );

            ID = _storage.PropTypes.Register( "Received", PropDataType.Date );
            Assert.AreEqual( "Received", _storage.PropTypes [ID].Name );
            Assert.AreEqual( PropDataType.Date, _storage.PropTypes [ID].DataType );
        }

        [Test] public void PropTypesAsResources()
        {
            IResourceList resList = _storage.GetAllResources( "PropType" );
            resList = resList;

            int ID = _storage.PropTypes.Register( "Author", PropDataType.Link );
            IResource res = _storage.FindUniqueResource( "PropType", "Name", "Author" );
            Assert.IsNotNull( res, "must find matching PropType entry for the resource" );
            Assert.AreEqual( ID, res.GetProp( "ID" ) );
            Assert.AreEqual( (int) PropDataType.Link, res.GetProp( "DataType" ) );
            Assert.AreEqual( 0, res.GetProp( "Flags" ) );

            // ensure that duplicate registration doesn't create a new object
            int count = _storage.GetAllResources( "PropType" ).Count;
            _storage.PropTypes.Register( "Author", PropDataType.Link );
            Assert.AreEqual( count, _storage.GetAllResources( "PropType" ).Count );

            res.SetProp( "Flags", (int) PropTypeFlags.CountUnread );
            Assert.AreEqual( PropTypeFlags.CountUnread, _storage.PropTypes [ID].Flags );

            // check that prop type resources for internal fields also exist
            Assert.IsTrue( _storage.FindUniqueResource( "PropType", "Name", "Name" ) != null );
            Assert.IsTrue( _storage.FindUniqueResource( "PropType", "Name", "ID" ) != null );

            ReopenStorage();
            int propAuthor = _storage.GetPropId( "Author" );
            Assert.AreEqual( PropTypeFlags.CountUnread, _storage.PropTypes [propAuthor].Flags );
        }

        [Test] public void PropTypeFlags_Or()
        {
            _storage.PropTypes.Register( "Test", PropDataType.Int, PropTypeFlags.Internal );
            _storage.PropTypes.Register( "Test", PropDataType.Int, PropTypeFlags.AskSerialize );
            Assert.AreEqual( PropTypeFlags.Internal | PropTypeFlags.AskSerialize, _storage.PropTypes ["Test"].Flags );

            ReopenStorage();
            Assert.AreEqual( PropTypeFlags.Internal | PropTypeFlags.AskSerialize, _storage.PropTypes ["Test"].Flags );

            _storage.PropTypes ["Test"].Flags = PropTypeFlags.Internal;
            Assert.AreEqual( PropTypeFlags.Internal, _storage.PropTypes ["Test"].Flags );

            ReopenStorage();
            Assert.AreEqual( PropTypeFlags.Internal, _storage.PropTypes ["Test"].Flags );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void ChangePropDataType()
        {
            IResource res = _storage.FindUniqueResource( "PropType", "Name", "Name" );
            res.SetProp( "DataType", (int) PropDataType.Int );
        }

        [Test, ExpectedException(typeof(StorageException))]
        public void CheckInvalidPropType()
        {
            IPropType propType = _storage.PropTypes [-255];
            propType = propType;
        }

        [Test] public void RegisterDuplicatePropType()
        {
            int ID1 = _storage.PropTypes.Register( "Author", PropDataType.Link );
            int ID2 = _storage.PropTypes.Register( "Author", PropDataType.Link );
            Assert.AreEqual( ID1, ID2 );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void InconsistentPropTypeRegistration()
        {
            _storage.PropTypes.Register( "From", PropDataType.Link );
            _storage.PropTypes.Register( "From", PropDataType.Int );
        }

        [Test] public void RegisterResourceType()
        {
            _storage.PropTypes.Register( "subject", PropDataType.String );
            int ID = _storage.ResourceTypes.Register( "Email", "E-mail", "subject", ResourceTypeFlags.NoIndex );
            Assert.AreEqual( "Email", _storage.ResourceTypes [ ID ].Name );
            Assert.AreEqual( "subject", _storage.ResourceTypes [ID].ResourceDisplayNameTemplate );
            Assert.AreEqual( ID, _storage.ResourceTypes ["Email"].Id );
            Assert.AreEqual( ResourceTypeFlags.NoIndex, _storage.ResourceTypes[ "Email" ].Flags );
            Assert.AreEqual( "E-mail", _storage.ResourceTypes ["Email"].DisplayName );

            ReopenStorage();
            Assert.AreEqual( ResourceTypeFlags.NoIndex, _storage.ResourceTypes [ "Email" ].Flags );

            _storage.ResourceTypes.Register( "Email", "E-mail", "Subject", ResourceTypeFlags.Internal );
            Assert.AreEqual( ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, _storage.ResourceTypes[ "Email" ].Flags );
        }

        [Test, ExpectedException(typeof(StorageException))]
        public void CheckInvalidResourceType()
        {
            string name = _storage.ResourceTypes[ -255 ].Name;
            name = name;
        }

        [Test, ExpectedException( typeof(StorageException))]
        public void InvalidPropertyInDisplayName()
        {
            _storage.ResourceTypes.Register( "Email", "Email", "SomeShit", ResourceTypeFlags.NoIndex );
        }

        [Test] public void RegisterDuplicateResourceType()
        {
            _storage.PropTypes.Register( "subject", PropDataType.String );
            int ID1 = _storage.ResourceTypes.Register( "Email", "Email", "Subject" );
            int ID2 = _storage.ResourceTypes.Register( "Email", "Email", "Subject" );
            Assert.AreEqual( ID1, ID2 );
        }

        [Test] public void ResourceTypesAsResources()
        {
            _storage.PropTypes.Register( "subject", PropDataType.String );
            int ID = _storage.ResourceTypes.Register( "Email", "E-mail", "Subject" );
            IResource res = _storage.FindUniqueResource( "ResourceType", "Name", "Email" );
            Assert.IsTrue( res != null, "must find matching ResourceType entry for the resource" );

            Assert.AreEqual( "Subject", res.GetStringProp( "DisplayNameMask" ) );
            Assert.AreEqual( "E-mail", res.DisplayName );

            _storage.ResourceTypes [ID].ResourceDisplayNameTemplate = "Name";
            _storage.ResourceTypes [ID].DisplayName = "E!mail";
            _storage.ResourceTypes [ID].Flags = ResourceTypeFlags.NoIndex;
            Assert.AreEqual( "Name", _storage.ResourceTypes [ID].ResourceDisplayNameTemplate );
            Assert.AreEqual( "E!mail", _storage.ResourceTypes [ID].DisplayName );
            Assert.AreEqual( ResourceTypeFlags.NoIndex, _storage.ResourceTypes ["Email"].Flags );

            ReopenStorage();

            ID = _storage.ResourceTypes ["Email"].Id;
            Assert.AreEqual( "Name", _storage.ResourceTypes [ID].ResourceDisplayNameTemplate );
            Assert.AreEqual( "E!mail", _storage.ResourceTypes [ID].DisplayName );
            Assert.AreEqual( ResourceTypeFlags.NoIndex, _storage.ResourceTypes [ "Email" ].Flags );
        }

        [Test] public void UpdateResourceTypeFlags()
        {
            _storage.PropTypes.Register( "subject", PropDataType.String );
            _storage.ResourceTypes.Register( "Email", "Email", "Subject" );
            Assert.AreEqual( ResourceTypeFlags.Normal, _storage.ResourceTypes[ "Email" ].Flags );
            _storage.ResourceTypes.Register( "Email", "Email", "Subject", ResourceTypeFlags.CanBeUnread );
            Assert.AreEqual( ResourceTypeFlags.CanBeUnread, _storage.ResourceTypes ["Email"].Flags );
        }

        [Test] public void DeleteResourceType()
        {
            _storage.ResourceTypes.Register( "Test", "Test", "Name" );
            IResource res = _storage.NewResource( "Test" );
            _storage.ResourceTypes.Delete( "Test" );
            Assert.IsTrue( res.IsDeleted );

            Assert.IsTrue( !_storage.ResourceTypes.Exist( "Test" ) );
            foreach( IResourceType resType in _storage.ResourceTypes )
            {
                Assert.IsFalse( resType.Name == "Test" );
            }

            ReopenStorage();
            Assert.IsTrue( !_storage.ResourceTypes.Exist( "Test" ) );

            IResource restypeRes = _storage.FindUniqueResource( "ResourceType", "Name", "Test" );
            Assert.IsNull( restypeRes );
        }

        [Test] public void PropDisplayName()
        {
            int propAuthor = _storage.PropTypes.Register( "Author", PropDataType.Link );
            Assert.AreEqual( "Author", _storage.PropTypes.GetPropDisplayName( propAuthor ) );

            _storage.PropTypes.RegisterDisplayName( propAuthor, "Creator" );
            Assert.AreEqual( "Creator", _storage.PropTypes.GetPropDisplayName( propAuthor ) );

            ReopenStorage();
            Assert.AreEqual( "Creator", _storage.PropTypes.GetPropDisplayName( propAuthor ) );
        }


        [Test] public void DirectedLinkDisplayName()
        {
            int propReply = _storage.PropTypes.Register( "Reply", PropDataType.Link, PropTypeFlags.DirectedLink );
            Assert.AreEqual( "Reply Source", _storage.PropTypes.GetPropDisplayName( propReply ) );
            Assert.AreEqual( "Reply Target", _storage.PropTypes.GetPropDisplayName( -propReply ) );

            _storage.PropTypes.RegisterDisplayName( propReply, "Reply To", "Replies" );
            Assert.AreEqual( "Reply To", _storage.PropTypes.GetPropDisplayName( propReply ) );
            Assert.AreEqual( "Replies", _storage.PropTypes.GetPropDisplayName( -propReply ) );

            ReopenStorage();
            Assert.AreEqual( "Reply To", _storage.PropTypes.GetPropDisplayName( propReply ) );
            Assert.AreEqual( "Replies", _storage.PropTypes.GetPropDisplayName( -propReply ) );
        }

        [Test] public void PseudoProperties()
        {
            Assert.AreEqual( PropDataType.Int, _storage.PropTypes [ResourceProps.Id].DataType );
            Assert.AreEqual( PropDataType.String, _storage.PropTypes [ResourceProps.Type].DataType );
            Assert.AreEqual( PropDataType.String, _storage.PropTypes [ResourceProps.DisplayName].DataType );
        }
    }
}
