// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using NUnit.Framework;
using JetBrains.Omea.HTML;


namespace OmniaMeaBaseTests
{
    [TestFixture]
    public class HTMLToolsTests
    {
        [Test]
        public void TestFixingRelativeLinks()
        {
            string fixedHTML = HtmlTools.FixRelativeLinks(
                "<html><body background='1.gif'></body></html>", "http://www.jetbrains.com");
            if( fixedHTML != "<html><body background=\"http://www.jetbrains.com/1.gif\"></body></html>" )
            {
                Console.WriteLine( fixedHTML );
                throw new Exception("Bad reference value quoted with ''");
            }
            fixedHTML = HtmlTools.FixRelativeLinks(
                "<html><body><a href=index.html></body></html>", "http://www.jetbrains.com");
            if( fixedHTML != "<html><body><a href=\"http://www.jetbrains.com/index.html\"></body></html>" )
            {
                Console.WriteLine( fixedHTML );
                throw new Exception("Bad not quoted reference value");
            }
            fixedHTML = HtmlTools.FixRelativeLinks(
                "<html><body><img src=\"1.jpg\"></body></html>", "ftp://www.jetbrains.com");
            if( fixedHTML != "<html><body><img src=\"ftp://www.jetbrains.com/1.jpg\"></body></html>" )
            {
                Console.WriteLine( fixedHTML );
                throw new Exception("Bad quoted reference value");
            }
            fixedHTML = HtmlTools.FixRelativeLinks(
                "<html><body><a href=/></body></html>", "http://www.jetbrains.com");
            if( fixedHTML != "<html><body><a href=\"http://www.jetbrains.com/\"></body></html>" )
            {
                Console.WriteLine( fixedHTML );
                throw new Exception("Bad trivial reference value");
            }

            fixedHTML = HtmlTools.FixRelativeLinks(
                "<style type=\"text/css\">@import /css/common.css; @import \"/css/hilbert.css\";</style>",
                "http://www.jetbrains.com" );
            if( fixedHTML != "<style type=\"text/css\">@import \"http://www.jetbrains.com/css/common.css\"; @import \"http://www.jetbrains.com/css/hilbert.css\";</style>" )
            {
                Console.WriteLine( fixedHTML );
                throw new Exception("Bad cascading style sheet reference value");
            }

            fixedHTML = HtmlTools.FixRelativeLinks(
                "span { background-image: url(\"images/mybackground.gif\" }",
                "http://www.jetbrains.com" );
            if( fixedHTML != "span { background-image: url(\"http://www.jetbrains.com/images/mybackground.gif\" }" )
            {
                Console.WriteLine( fixedHTML );
                throw new Exception("Bad cascading style sheet reference value");
            }
            fixedHTML = HtmlTools.FixRelativeLinks(
                "<html><body><img src=\"http://banner.jetbrains.com/1.jpg\"></body></html>", "ftp://www.jetbrains.com");
            if( fixedHTML != "<html><body><img src=\"http://banner.jetbrains.com/1.jpg\"></body></html>" )
            {
                Console.WriteLine( fixedHTML );
                throw new Exception( "Bad absolute reference value" );
            }
            fixedHTML = HtmlTools.FixRelativeLinks(
                "span { background-image: url(\"images/mybackground.gif\" }",
                "http://www.jetbrains.com/omea/index.html" );
            if( fixedHTML != "span { background-image: url(\"http://www.jetbrains.com/omea/images/mybackground.gif\" }" )
            {
                Console.WriteLine( fixedHTML );
                throw new Exception( "Retreiving absolute path for regular page failed" );
            }
        }

        [Test] public void TestConvertLinks()
        {
            Assert.AreEqual( "WWW).", HtmlTools.ConvertLinks( "WWW)." ) );
            Assert.AreEqual( "WWW сервере", HtmlTools.ConvertLinks( "WWW сервере" ) );
            Assert.AreEqual( "RSDN@home", HtmlTools.ConvertLinks( "RSDN@home" ) );
            Assert.AreEqual( "<a href=\"news:3004312.1092753494993.JavaMail.itn@is.intellij.net\">news:3004312.1092753494993.JavaMail.itn@is.intellij.net</a>...", HtmlTools.ConvertLinks( "news:3004312.1092753494993.JavaMail.itn@is.intellij.net..." ) );
            Assert.AreEqual( "<a href=\"http://jetbrains.com\">http://jetbrains.com</a>.", HtmlTools.ConvertLinks( "http://jetbrains.com." ) );
            Assert.AreEqual( "<a href=\"http://www.jetbrains.com\">www.jetbrains.com</a>.", HtmlTools.ConvertLinks( "www.jetbrains.com." ) );
            Assert.AreEqual( "<a href=\"http://www.jetbrains.com\">www.jetbrains.com</a>. ", HtmlTools.ConvertLinks( "www.jetbrains.com. " ) );
            Assert.AreEqual( "<a href=\"http://www.jetbrains.com/\">www.jetbrains.com/</a>", HtmlTools.ConvertLinks( "www.jetbrains.com/" ) );
            Assert.AreEqual( "<a href=\"http://www.jetbrains.com/\">http://www.jetbrains.com/</a>", HtmlTools.ConvertLinks( "http://www.jetbrains.com/" ) );
            Assert.AreEqual( "<a href=\"http://www.jetbrains.com\">www.jetbrains.com</a>&nbsp;a", HtmlTools.ConvertLinks( "www.jetbrains.com&nbsp;a" ) );
            Assert.AreEqual( "<a href=\"news://news.intellij.net:119/a@b.com\">news://news.intellij.net:119/a@b.com</a>", HtmlTools.ConvertLinks( "news://news.intellij.net:119/a@b.com" ) );
        }
    }

}
