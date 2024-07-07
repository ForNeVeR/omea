// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceStore;
using NUnit.Framework;

namespace CommonTests
{
	/**
     * Unit tests for the ResourceComparer class.
     */

    [TestFixture]
    public class ResourceComparerTests: MyPalDBTests
	{
        private ResourceList _ownerList;

        [SetUp] public void SetUp()
        {
            InitStorage();
            RegisterResourcesAndProperties();

            _ownerList = (ResourceList) _storage.GetAllResources( "ResourceType" );
        }

        [TearDown] public void TearDown()
        {
            CloseStorage();
        }

        [Test] public void TypeComparer()
        {
            SortSettings settings = SortSettings.Parse( _storage, "Type- FirstName" );
            Assert.AreEqual( 2, settings.SortProps.Length );
            Assert.AreEqual( 2, settings.SortDirections.Length );

            Assert.AreEqual( ResourceProps.Type, settings.SortProps [0] );
            Assert.AreEqual( _propFirstName, settings.SortProps [1] );

            Assert.AreEqual( false, settings.SortDirections [0] );
            Assert.AreEqual( true, settings.SortDirections [1] );

            ResourceComparer comparer = new ResourceComparer( _ownerList, settings, false );

            IResource res1 = _storage.NewResource( "Email" );
            IResource res2 = _storage.NewResource( "Person" );
            Assert.IsTrue( comparer.CompareResources( res1, res2 ) > 0 );
        }
	}
}
