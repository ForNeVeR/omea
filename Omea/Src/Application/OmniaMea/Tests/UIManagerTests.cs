// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;

namespace OmniaMea.Tests
{
	[TestFixture]
    public class UIManagerTests
    {
        private TestCore _core;
        private UIManager _uiManager;
        private int _propMAPIFolder;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _uiManager = new UIManager( null );

            _core.ResourceStore.ResourceTypes.Register( "Email", "Name" );
            _core.ResourceStore.ResourceTypes.Register( "Folder", "Name" );
            _propMAPIFolder = _core.ResourceStore.PropTypes.Register( "MAPIFolder", PropDataType.Link,
                PropTypeFlags.DirectedLink );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        [Test] public void ResourcesInLocation()
        {
            _uiManager.RegisterResourceLocationLink( "Email", _propMAPIFolder, "Folder" );
            IResource email = _core.ResourceStore.NewResource( "Email" );
            IResource folder = _core.ResourceStore.NewResource( "Folder" );
            email.AddLink( _propMAPIFolder, folder );

            IResourceList emails = _uiManager.GetResourcesInLocation( folder );
            Assert.AreEqual( 1, emails.Count );
            Assert.AreEqual( email, emails [0] );
        }
	}
}
