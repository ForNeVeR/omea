// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using JetBrains.Omea.RSSPlugin;
using NUnit.Framework;

namespace RSSPlugin.Tests
{
	[TestFixture]
    public class FeedsTreeCommiterTests
	{
        private TestCore _core;
        private IResourceStore _storage;
        private IResource _importRoot;
        private IResource _previewRoot;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;

            Props.Register( null );

            _importRoot = _storage.NewResource( "RSSFeedGroup" );
            _previewRoot = _storage.NewResource( "RSSFeedGroup" );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        private IResource NewGroup( string name, IResource parent )
        {
            IResource res = _storage.NewResource( "RSSFeedGroup" );
            res.SetProp( "Name", name );
            res.SetProp( "Parent", parent );
            return res;
        }

        private IResource NewFeed( IResource parent )
        {
            IResource feed = _storage.NewResource( "RSSFeed" );
            feed.SetProp( "Parent", parent );
            return feed;
        }

        [Test] public void ConfirmImport_Simple()
        {
            IResource feed = NewFeed( _previewRoot );

            FeedsTreeCommiter.DoConfirmImport( _previewRoot, _importRoot );
            Assert.AreEqual( _importRoot, feed.GetLinkProp( "Parent" ) );
            Assert.IsTrue( _previewRoot.IsDeleted );
        }

        [Test] public void ConfirmImport_SameGroupName()
        {
            IResource importGroup = NewGroup( "Subscriptions", _importRoot );
            IResource previewGroup = NewGroup( "Subscriptions", _previewRoot );
            IResource feed = NewFeed( previewGroup );

            FeedsTreeCommiter.DoConfirmImport( _previewRoot, _importRoot );

            Assert.AreEqual( importGroup, feed.GetLinkProp( "Parent" ) );
            Assert.IsTrue( previewGroup.IsDeleted );
        }

        [Test] public void ConfirmImport_ComplexStructure()
        {
            IResource previewGroup = NewGroup( "Subscriptions", _previewRoot );
            IResource previewChild = NewGroup( "Child", previewGroup );
            IResource feed = NewFeed( previewChild );
            IResource previewChild2 = NewGroup( "Child2", previewGroup );
            IResource feed2 = NewFeed( previewChild2 );

            IResource importGroup = NewGroup( "Subscriptions", _importRoot );
            IResource importChild = NewGroup( "Child", importGroup );

            FeedsTreeCommiter.DoConfirmImport( _previewRoot, _importRoot );

            Assert.AreEqual( importChild, feed.GetLinkProp( "Parent" ) );
            Assert.AreEqual( previewChild2, feed2.GetLinkProp( "Parent" ) );
            Assert.AreEqual( importGroup, previewChild2.GetLinkProp( "Parent" ) );
        }

        [Test] public void PartiallyExistingStructure()
        {
            IResource importGroup = NewGroup( "Subscriptions", _importRoot );
            IResource previewGroup = NewGroup( "Subscriptions", _previewRoot );
            IResource previewGroup2 = NewGroup( "Subscriptions2", _previewRoot );

            IResource feed = NewFeed( previewGroup );
            IResource feed2 = NewFeed( previewGroup2 );

            FeedsTreeCommiter.DoConfirmImport( _previewRoot, _importRoot );
            Assert.AreEqual( importGroup, feed.GetLinkProp( "Parent" ) );
            Assert.AreEqual( previewGroup2, feed2.GetLinkProp( "Parent" ) );
            Assert.AreEqual( _importRoot, previewGroup2.GetLinkProp( "Parent" ) );
        }
	}
}
