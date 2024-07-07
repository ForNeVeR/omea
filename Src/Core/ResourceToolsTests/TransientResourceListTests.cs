// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using JetBrains.Omea.ResourceTools;
using NUnit.Framework;

namespace ResourceToolsTests
{
	/**
     * Tests for TransientResourceList.
     */

    [TestFixture]
    public class TransientResourceListTests
	{
        private TestCore _core;
        private IResourceStore _storage;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;
            _storage.ResourceTypes.Register( "Email", "Name" );
            _storage.ResourceTypes.Register( "Person", "Name" );
            _storage.PropTypes.Register( "FirstName", PropDataType.String );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        [Test] public void LiveTransientList()
        {
            IResource email = _storage.NewResource( "Email" );
            IResourceList emails = _storage.GetAllResourcesLive( "Email" );

            TransientResourceList trans = new TransientResourceList();
            IResource person = _storage.NewResourceTransient( "Person" );
            person.SetProp( "FirstName", "Dmitry" );
            trans.Add( person );

            IResource person2 = _storage.NewResourceTransient( "Person" );
            person2.SetProp( "FirstName", "Sergey" );
            trans.Add( person2 );

            IResourceList union = emails.Union( trans );
            union.Sort( "ID" );
            Assert.AreEqual( 3, union.Count );

            person2.Delete();
            Assert.AreEqual( 2, union.Count );

            person.Delete();
            Assert.AreEqual( 1, union.Count );
        }
	}
}
