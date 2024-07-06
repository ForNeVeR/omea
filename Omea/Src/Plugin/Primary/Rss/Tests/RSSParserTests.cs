// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Reflection;
using System.Text;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using JetBrains.Omea.RSSPlugin;
using NUnit.Framework;

namespace RSSPlugin.Tests
{
	[TestFixture]
    public class RSSParserTests
	{
        private TestCore _core;
        private IResourceStore _storage;
        private IResource _feed;
        private RSSParser _parser;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;

            Props.Register( null );

            _feed = _storage.NewResource( "RSSFeed" );
            _parser = new RSSParser( _feed );
        }

        [TearDown] public void TearDown()
        {
            _parser.Dispose();
            _core.Dispose();
        }

        private IResourceList ParseFeed( string name )
        {
            foreach( string resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames() )
            {
                if ( resourceName.EndsWith( name ) )
                {
                    using( Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( resourceName ) )
                    {
                        _parser.Parse( stream, Encoding.Default, true );
                        return _feed.GetLinksOfType( "RSSItem", Props.RSSItem );
                    }
                }
            }
            throw new Exception( "Failed to find test data stream " + name );
        }

        [Test] public void TestTitleCData()
        {
            IResourceList items = ParseFeed( "title-cdata.xml" );
            Assert.AreEqual( 1, items.Count );
            IResource item = items [0];
            Assert.AreEqual( "Google Desktop Search - Initial Reactions", item.GetStringProp( "Subject" ) );
        }

        [Test] public void TestTitleXhtml()
        {
            IResourceList items = ParseFeed( "title-xhtml.xml" );
//            Assert.AreEqual( "<p xmlns=\"http://purl.org/atom/ns#\">Google Desktop Search - Initial Reactions</p>",
            //  We do not allow html tags to appear in the subjects.
            Assert.AreEqual( "Google Desktop Search - Initial Reactions",
                items [0].GetStringProp( "Subject" ) );
        }

        [Test] public void TestTitlePlain()
        {
            IResourceList items = ParseFeed( "title-plain.xml" );
            Assert.AreEqual( "Virtual Server 2005 and Windows XP SP2",
                items [0].GetStringProp( "Subject" ) );
        }
        [Test] public void TestImageProps()
        {
            ParseFeed( "image_tag.xml" );
            Assert.AreEqual( "Wired News", _feed.GetStringProp( "ImageTitle" ) );
            Assert.AreEqual( "http://static.wired.com/news/images/netcenterb.gif", _feed.GetStringProp( "ImageURL" ) );
            Assert.AreEqual( "http://www.wired.com/", _feed.GetStringProp( "ImageLink" ) );
        }

        [Test] public void TestSpacesInTags()
        {
            IResourceList items = ParseFeed( "spaces-in-tags.xml" );
            Assert.AreEqual( "humor", items [0].GetStringProp( Props.RSSCategory ) );
            DateTime dt = items [0].GetDateProp( "Date" ).ToUniversalTime();
            Assert.AreEqual( 10, dt.Month );
            Assert.AreEqual( 2, dt.Day );

            IResource sender = items [0].GetLinkProp( "From" );
            Assert.IsNotNull( sender );
            Assert.AreEqual( "liz", sender.DisplayName );
        }

        [Test] public void TestAtomSummary()
        {
            IResourceList items = ParseFeed( "atom-summary.xml" );
            Assert.AreEqual( "<div xmlns=\"http://www.w3.org/1999/xhtml\">I truly love IntelliJ IDEA from JetBrains</div>",
                items [0].GetPropText( Core.Props.LongBody ) );
        }
    }
}

