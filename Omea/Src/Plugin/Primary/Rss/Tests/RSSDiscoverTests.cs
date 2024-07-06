// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Reflection;
using JetBrains.Omea.RSSPlugin;
using NUnit.Framework;

namespace RSSPlugin.Tests
{
	[TestFixture]
    public class RSSDiscoverTests
	{
        private RSSDiscover _rssDiscover;

        [SetUp] public void SetUp()
        {
            _rssDiscover = new RSSDiscover();
            _rssDiscover.DownloadResults = false;
        }

        private Stream GetResourceStream( string name )
        {
            foreach( string resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames() )
            {
                if ( resourceName.EndsWith( name ) )
                {
                    return Assembly.GetExecutingAssembly().GetManifestResourceStream( resourceName );
                }
            }
            throw new Exception( "Stream " + name + " not found" );
        }

        [Test] public void SimpleDiscoverTest()
        {
            _rssDiscover.StartDiscover( "http://www.livejournal.com/users/yole",
                GetResourceStream( "simple-discover.html" ), "" );
            Assert.AreEqual( 2, _rssDiscover.CandidateURLs.Count );
            Assert.AreEqual( "http://www.livejournal.com/users/yole/data/rss",
                (string) _rssDiscover.CandidateURLs.Pop() );
            Assert.AreEqual( "http://www.livejournal.com/users/yole/data/atom",
                (string) _rssDiscover.CandidateURLs.Pop() );
        }

        [Test] public void BadUrlScheme()
        {
            _rssDiscover.StartDiscover( "someshit://www.livejournal.com/users/yole",
                GetResourceStream( "bad-url-scheme.html" ), "" );
            Assert.AreEqual( 0, _rssDiscover.CandidateURLs.Count );
        }
	}
}
