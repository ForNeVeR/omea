// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;
using JetBrains.Omea.RSSPlugin;

namespace RSSPlugin.Tests
{
	[TestFixture]
    public class OPMLTests
	{
        private TestCore _core;
        private IResourceStore _storage;
        private IResource _rootGroup;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;
            Props.Register( null );
            _rootGroup = _storage.NewResource( "RSSFeedGroup" );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        [Test] public void TestSimpleImport()
        {
            StringReader reader = new StringReader( "<opml version=\"1.0\"><body><outline type=\"rss\" text=\".Avery Blog\" title=\".Avery Blog\" xmlUrl=\"http://dotavery.com/blog/Rss.aspx\"/></body></opml>");
            OPMLProcessor.Import( reader, _rootGroup, false );

            IResourceList feedLinks = _rootGroup.GetLinksTo( "RSSFeed", "Parent" );
            Assert.AreEqual( 1, feedLinks.Count );
            Assert.AreEqual( 0, _rootGroup.GetLinksTo( "RSSFeedGroup", "Parent" ).Count );

            IResource feed = feedLinks [0];
            Assert.AreEqual( ".Avery Blog", feed.GetStringProp( "Name" ) );
        }

        [Test] public void TestEmptyOutlineImport()
        {
            StringReader reader = new StringReader( "<opml version=\"1.0\"><body><outline text=\"Some Text\"/><outline type=\"rss\" text=\".Avery Blog\" title=\".Avery Blog\" xmlUrl=\"http://dotavery.com/blog/Rss.aspx\"/></body></opml>");
            OPMLProcessor.Import( reader, _rootGroup, false );

            IResourceList feedLinks = _rootGroup.GetLinksTo( "RSSFeed", "Parent" );
            IResourceList groupLinks = _rootGroup.GetLinksTo( "RSSFeedGroup", "Parent" );
            Assert.AreEqual( 1, feedLinks.Count );
            Assert.AreEqual( 1, groupLinks.Count );

            IResource feed = feedLinks [0];
            Assert.AreEqual( ".Avery Blog", feed.GetStringProp( "Name" ) );

            IResource group = groupLinks [0];
            Assert.AreEqual( "Some Text", group.GetStringProp( "Name" ) );

            Assert.AreEqual( 0, group.GetLinksTo( "RSSFeedGroup", "Parent" ).Count );
        }

        [Test] public void TestEmptyOutlineImport2()
        {
            StringReader reader = new StringReader( "<opml version=\"1.0\"><body><outline text=\"Some Text\"></outline><outline type=\"rss\" text=\".Avery Blog\" title=\".Avery Blog\" xmlUrl=\"http://dotavery.com/blog/Rss.aspx\"/></body></opml>");
            OPMLProcessor.Import( reader, _rootGroup, false );

            IResourceList feedLinks = _rootGroup.GetLinksTo( "RSSFeed", "Parent" );
            IResourceList groupLinks = _rootGroup.GetLinksTo( "RSSFeedGroup", "Parent" );
            Assert.AreEqual( 1, feedLinks.Count );
            Assert.AreEqual( 1, groupLinks.Count );

            IResource feed = feedLinks [0];
            Assert.AreEqual( ".Avery Blog", feed.GetStringProp( "Name" ) );

            IResource group = groupLinks [0];
            Assert.AreEqual( "Some Text", group.GetStringProp( "Name" ) );

            Assert.AreEqual( 0, group.GetLinksTo( "RSSFeedGroup", "Parent" ).Count );
        }

        [Test] public void TestImportFeedInOutline()
        {
            StringReader reader = new StringReader(
                "<opml version=\"1.0\">" +
                    "<body><outline text=\"Some Text\">" +
                        "<outline type=\"rss\" text=\".Avery Blog\" title=\".Avery Blog\" xmlUrl=\"http://dotavery.com/blog/Rss.aspx\"/>" +
                        "<outline type=\"rss\" text=\"mercola.com blog\" title=\"mercola.com blog\" xmlUrl=\"http://mercola.com/blog/rss.xml\"/>" +
                    "</outline>" +
                    "<outline text=\"Some Text 2\"></outline>" +
                "</body></opml>" );
            OPMLProcessor.Import( reader, _rootGroup, false );

            IResourceList feedLinks = _rootGroup.GetLinksTo( "RSSFeed", "Parent" );
            IResourceList groupLinks = _rootGroup.GetLinksTo( "RSSFeedGroup", "Parent" );
            Assert.AreEqual( 0, feedLinks.Count );
            Assert.AreEqual( 2, groupLinks.Count );

            feedLinks = groupLinks [0].GetLinksTo( "RSSFeed", "Parent" );
            Assert.AreEqual( 2, feedLinks.Count );

            IResource feed = feedLinks [0];
            Assert.AreEqual( ".Avery Blog", feed.GetStringProp( "Name" ) );

            Assert.AreEqual( "Some Text", groupLinks [0].GetStringProp( "Name" ) );
            Assert.AreEqual( "Some Text 2", groupLinks [1].GetStringProp( "Name" ) );
        }

        [Test] public void TestImportFeedInOutline_ExistingFeeds()
        {
            IResource feed = _storage.NewResource( "RSSFeed" );
            feed.SetProp( "URL", "http://dotavery.com/blog/Rss.aspx" );

            StringReader reader = new StringReader(
                "<opml version=\"1.0\">" +
                "<body><outline text=\"Some Text\">" +
                "<outline type=\"rss\" text=\".Avery Blog\" title=\".Avery Blog\" xmlUrl=\"http://dotavery.com/blog/Rss.aspx\"/>" +
                "<outline type=\"rss\" text=\"mercola.com blog\" title=\"mercola.com blog\" xmlUrl=\"http://mercola.com/blog/rss.xml\"/>" +
                "</outline>" +
                "<outline text=\"Some Text 2\"></outline>" +
                "</body></opml>" );
            OPMLProcessor.Import( reader, _rootGroup, false );

            IResourceList feedLinks = _rootGroup.GetLinksTo( "RSSFeed", "Parent" );
            IResourceList groupLinks = _rootGroup.GetLinksTo( "RSSFeedGroup", "Parent" );
            Assert.AreEqual( 0, feedLinks.Count );
            Assert.AreEqual( 2, groupLinks.Count );

            feedLinks = groupLinks [0].GetLinksTo( "RSSFeed", "Parent" );
            Assert.AreEqual( 1, feedLinks.Count );

            feed = feedLinks [0];
            Assert.AreEqual( "mercola.com blog", feed.GetStringProp( "Name" ) );

            Assert.AreEqual( "Some Text", groupLinks [0].GetStringProp( "Name" ) );
            Assert.AreEqual( "Some Text 2", groupLinks [1].GetStringProp( "Name" ) );
        }

        [Test] public void TestImportFeedWithExplicitEndTag()
        {
            StringReader reader = new StringReader( "<opml version=\"1.0\"><body><outline type=\"rss\" text=\".Avery Blog\" title=\".Avery Blog\" xmlUrl=\"http://dotavery.com/blog/Rss.aspx\"></outline></body></opml>");
            OPMLProcessor.Import( reader, _rootGroup, false );

            IResourceList feedLinks = _rootGroup.GetLinksTo( "RSSFeed", "Parent" );
            Assert.AreEqual( 1, feedLinks.Count );
            Assert.AreEqual( 0, _rootGroup.GetLinksTo( "RSSFeedGroup", "Parent" ).Count );

            IResource feed = feedLinks [0];
            Assert.AreEqual( ".Avery Blog", feed.GetStringProp( "Name" ) );
        }

        [Test] public void TestImportFeedInOutlineWithEndTags_ExistingFeeds()
        {
            IResource feed = _storage.NewResource( "RSSFeed" );
            feed.SetProp( "URL", "http://dotavery.com/blog/Rss.aspx" );

            StringReader reader = new StringReader(
                "<opml version=\"1.0\">" +
                "<body><outline text=\"Some Text\">" +
                "<outline type=\"rss\" text=\".Avery Blog\" title=\".Avery Blog\" xmlUrl=\"http://dotavery.com/blog/Rss.aspx\"></outline>" +
                "<outline type=\"rss\" text=\"mercola.com blog\" title=\"mercola.com blog\" xmlUrl=\"http://mercola.com/blog/rss.xml\"></outline>" +
                "</outline>" +
                "<outline text=\"Some Text 2\"></outline>" +
                "</body></opml>" );
            OPMLProcessor.Import( reader, _rootGroup, false );

            IResourceList feedLinks = _rootGroup.GetLinksTo( "RSSFeed", "Parent" );
            IResourceList groupLinks = _rootGroup.GetLinksTo( "RSSFeedGroup", "Parent" );
            Assert.AreEqual( 0, feedLinks.Count );
            Assert.AreEqual( 2, groupLinks.Count );

            feedLinks = groupLinks [0].GetLinksTo( "RSSFeed", "Parent" );
            Assert.AreEqual( 1, feedLinks.Count );

            feed = feedLinks [0];
            Assert.AreEqual( "mercola.com blog", feed.GetStringProp( "Name" ) );

            Assert.AreEqual( "Some Text", groupLinks [0].GetStringProp( "Name" ) );
            Assert.AreEqual( "Some Text 2", groupLinks [1].GetStringProp( "Name" ) );
        }
    }
}
