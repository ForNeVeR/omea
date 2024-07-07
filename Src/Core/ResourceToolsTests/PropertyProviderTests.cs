// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.PicoCore;
using NUnit.Framework;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace ResourceToolsTests
{
	/**
     * Unit tests for SimplePropertyProvider and related functionality in ResourceList.
     */

    [TestFixture]
    public class PropertyProviderTests
	{
        private TestCore _core;
        private IResourceStore _storage;
        private SimplePropertyProvider _provider;
        private ResourcePropIndexEventArgs _lastChangeArgs;
        private int _propSize;
        private int _propSubject;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;
            _propSize = _storage.PropTypes.Register( "Size", PropDataType.Int );
            _propSubject = _storage.PropTypes.Register( "Subject", PropDataType.String );
            _storage.ResourceTypes.Register( "Email", "Subject" );
            _storage.ResourceTypes.Register( "Person", "Name" );

            _provider = new SimplePropertyProvider();
            _lastChangeArgs = null;
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        [Test] public void SimpleTest()
        {
            IResource email = _storage.NewResource( "Email" );
            _provider.SetProp( email.Id, _propSize, 100 );

            IResourceList allEmails = _storage.GetAllResourcesLive( "Email" );
            allEmails.AttachPropertyProvider( _provider );

            Assert.IsTrue( allEmails.HasProp( _propSize ) );
            Assert.IsTrue( !allEmails.HasProp( _propSubject ) );

            Assert.AreEqual( "100", allEmails.GetPropText( 0, _propSize ) );
        }

        [Test] public void UnionTest()
        {
            IResource email = _storage.NewResource( "Email" );
            _provider.SetProp( email.Id, _propSize, 100 );

            IResourceList allEmails = _storage.GetAllResourcesLive( "Email" );
            allEmails.AttachPropertyProvider( _provider );

            IResourceList allPersons = _storage.GetAllResourcesLive( "Person" );

            IResourceList unionList = allPersons.Union( allEmails );
            Assert.IsTrue( unionList.HasProp( _propSize ) );
            Assert.AreEqual( "100", unionList.GetPropText( 0, _propSize ) );
        }

        [Test] public void TestChangeNotification()
        {
            IResource email = _storage.NewResource( "Email" );

            IResourceList allEmails = _storage.GetAllResourcesLive( "Email" );
            allEmails.AttachPropertyProvider( _provider );
            allEmails.ResourceChanged += new ResourcePropIndexEventHandler( OnResourceChanged );

            _provider.SetProp( email.Id, _propSize, 100 );
            Assert.IsTrue( _lastChangeArgs != null, "ResourceChanged event must have been fired" );
            Assert.AreEqual( _lastChangeArgs.Resource, email );
            Assert.IsTrue( _lastChangeArgs.ChangeSet.IsPropertyChanged( _propSize ) );
        }

        [Test] public void ResourceDeletedTest()
        {
            IResource email = _storage.NewResource( "Email" );
            int emailID = email.Id;

            IResourceList allEmails = _storage.GetAllResourcesLive( "Email" );
            allEmails.AttachPropertyProvider( _provider );

            email.Delete();
            _provider.SetProp( emailID, _propSize, 100 );
        }

        private void OnResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            _lastChangeArgs = e;
        }
	}
}
