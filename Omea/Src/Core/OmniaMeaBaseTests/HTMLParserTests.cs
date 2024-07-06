// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Text;
using JetBrains.DataStructures;
using JetBrains.Omea.HTML;
using NUnit.Framework;

namespace OmniaMeaBaseTests
{
	[TestFixture]
	public class HTMLParserTests
	{
		private HTMLParser CreateParser( string html )
		{
			return new HTMLParser(
				new StreamReader( new MemoryStream( Encoding.Default.GetBytes( html ) ) ) );
		}

		/// <summary>
		/// Invokes parser repeatedly to read all the fragments.
		/// Writes the fragments to a string, separates them with spaces (trailing space is added too!).
		/// </summary>
		/// <param name="parser"></param>
		/// <returns></returns>
		private string ReadAllFragments( HTMLParser parser )
		{
			StringBuilder sb = new StringBuilder();
			while( !parser.Finished )
				sb.Append( parser.ReadNextFragment() );
			try
			{
				if( parser.ReadNextFragment().Length != 0 )
					throw new InvalidOperationException( "Parser must return an empty fragment having read the whole text (if there's a tag after the last returned meaningful string)." );
				throw new InvalidOperationException( "Parser must throw an exception if reading beyond end of stream." );
			}
			catch( EndOfStreamException ) // It's expected
			{
			}

			return sb.ToString();
		}

		[Test]
		public void NoBody()
		{
			string noBodyHTML = "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.0//EN\"><HTML><HEAD> text to be ignored </HEAD></HTML>";
            using( HTMLParser parser = CreateParser( noBodyHTML ) )
            {
                if( parser.ReadNextFragment().Length > 0 )
                    throw new Exception( "Text outside HTML body is read!" );
            }
		}

		[Test]
		public void NoBodyNoWordBreak()
		{
			string noBodyHTML = "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.0//EN\"><HTML><HEAD> text to be ignored </HEAD></HTML>";
            using( HTMLParser parser = CreateParser( noBodyHTML ) )
            {
                parser.BreakWords = false;
                if( parser.ReadNextFragment().Length > 0 )
                    throw new Exception( "Text outside HTML body is read!" );
            }
		}

		[Test]
		public void SimpleBody()
		{
			string noBodyHTML = "<HTML><HEAD><BODY>text in body</BODY>text to be ignored</HEAD></HTML>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( noBodyHTML ) ) ) ) )
            {
                Assert.AreEqual( "text in body", ReadAllFragments( parser ), "Invalid simple body text!" );
            }
		}

		[Test]
		public void SimpleBodyNoWordBreak()
		{
			string noBodyHTML = "<HTML><HEAD><BODY>text in body</BODY>text to be ignored</HEAD></HTML>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( noBodyHTML ) ) ) ) )
            {
                parser.BreakWords = false;

                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("text in body", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("", parser.ReadNextFragment());
                Assert.AreEqual( true, parser.Finished );
            }
		}

		[Test]
		public void QuotesInTag()
		{
			string HTML = "<HTML><HEAD><BODY>1st frag<P a=\"aaaa\" b=\"bbbb\"> 2nd frag </BODY></HEAD></HTML>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                Assert.AreEqual( "1st frag 2nd frag ", ReadAllFragments( parser ), "Invalid fragments!" );
            }
		}

		[Test]
		public void QuotesInTagNoWordBreak()
		{
			string HTML = "<HTML><HEAD><BODY>1st frag<P a=\"aaaa\" b=\"bbbb\"> 2nd frag </BODY></HEAD></HTML>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                parser.BreakWords = false;
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("1st frag", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual(" 2nd frag ", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("", parser.ReadNextFragment());
                Assert.AreEqual( true, parser.Finished );
            }
		}

		[Test]
		public void Title()
		{
			string HTML = "<HTML><HEAD><Title>The title</tITLe></HEAD><BODY>1st frag<P a=\"aaaa\" b=\"bbbb\"> 2nd frag </BODY></HTML>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                Assert.AreEqual( "The title1st frag 2nd frag ", ReadAllFragments( parser ), "Invalid fragments!" );
            }
		}

		[Test]
		public void TitleNoWordBreak()
		{
			string HTML = "<HTML><HEAD><Title>The title</tITLe></HEAD><BODY>1st frag<P a=\"aaaa\" b=\"bbbb\"> 2nd frag </BODY></HTML>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                parser.BreakWords = false;
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("The title", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("1st frag", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual(" 2nd frag ", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("", parser.ReadNextFragment());
                Assert.AreEqual( true, parser.Finished );
            }
		}

		[Test]
		public void Scripts()
		{
			string HTML = "<HTML><HEAD><Title>The title</tITLe><script>i = 0</script></HEAD><BODY>1st frag<P a=\"aaaa\" b=\"bbbb\"><script>i = 0</script> 2nd frag </BODY></HTML>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                Assert.AreEqual( "The title1st frag 2nd frag ", ReadAllFragments( parser ), "Invalid fragments!" );
            }
		}

		[Test]
		public void ScriptsNoWordBreak()
		{
			string HTML = "<HTML><HEAD><Title>The title</tITLe><script>i = 0</script></HEAD><BODY>1st frag<P a=\"aaaa\" b=\"bbbb\"><script>i = 0</script> 2nd frag </BODY></HTML>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                parser.BreakWords = false;
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("The title", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("1st frag", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual(" 2nd frag ", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("", parser.ReadNextFragment());
                Assert.AreEqual( true, parser.Finished );
            }
		}

		[Test]
		public void CharEntityReferences()
		{
			string HTML = "<body><p>&#x69;&#X6E;&#x63;lude &lt;&#X6C;ist&gt;<p>inclu&#100;&#101; &quot;omniamea.h&quot;<p>#include &laquo;Kama&mdash;Sutra&raquo;</p></body>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                Assert.AreEqual( "include <list>include \"omniamea.h\"#include «Kama—Sutra»", ReadAllFragments( parser ), "Invalid fragments!" );
            }
		}

		[Test]
		public void CharEntityReferencesNoWordBreak()
		{
			string HTML = "<body><p>&#x69;&#X6E;&#x63;lude &lt;&#X6C;ist&gt;<p>inclu&#100;&#101; &quot;omniamea.h&quot;<p>#include &laquo;Kama&mdash;Sutra&raquo;</p></body>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                parser.BreakWords = false;
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("include <list>", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("include \"omniamea.h\"", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("#include «Kama—Sutra»", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("", parser.ReadNextFragment());
                Assert.AreEqual( true, parser.Finished );
            }
		}

		[Test]
		public void Charset()
		{
			string HTML = "<HTML><meTa httP-eQuIv=\"Content-Type\" content=\"text/html; cHaRseT=WinDowS-1251\"><BODY>1st frag</BODY></HTML>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                Assert.AreEqual( "1st frag", ReadAllFragments( parser ), "Invalid fragments" );
                Assert.AreEqual( "windows-1251", parser.CharSet, "Invalid charset!" );
            }
		}

		[Test]
		public void CharsetNoWordBreak()
		{
			string HTML = "<HTML><meTa httP-eQuIv=\"Content-Type\" content=\"text/html; cHaRseT=WinDowS-1251\"><BODY>1st frag</BODY></HTML>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                parser.BreakWords = false;
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("1st frag", parser.ReadNextFragment());
                Assert.AreEqual( false, parser.Finished );
                Assert.AreEqual("", parser.ReadNextFragment());
                Assert.AreEqual( true, parser.Finished );
                Assert.AreEqual( "windows-1251", parser.CharSet, "Invalid charset!" );
            }
		}

		[Test]
		public void Finishing()
		{
			string HTML = "<HTML><HEAD><Title>The title</tITLe></HEAD><BODY>1st frag<P> 2nd frag </BODY></HTML>\n";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                while( !parser.Finished )
                {
                    parser.ReadNextFragment();
                }
            }
		}

		[Test]
		public void FinishingNoWordBreak()
		{
			string HTML = "<HTML><HEAD><Title>The title</tITLe></HEAD><BODY>1st frag<P> 2nd frag </BODY></HTML>\n";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                parser.BreakWords = false;
                while( !parser.Finished )
                {
                    parser.ReadNextFragment();
                }
            }
		}

		[Test]
		public void FinishingOnUnclosed()
		{
			string HTML = "<HTML><HEAD><Title>The title";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                parser.BreakWords = false;
                int	a;
                for(a = 0; (a < 0x1000) && ( !parser.Finished ); a++)
                    parser.ReadNextFragment();
                if(!(a < 1000))
                    Assert.Fail( "The parser has failed to finish." );
            }
		}

		[Test]
		public void FinishingOnUnclosedNoWordBreak()
		{
			string HTML = "<HTML><HEAD><Title>The title";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                parser.BreakWords = false;
                int	a;
                for(a = 0; (a < 0x1000) && ( !parser.Finished ); a++)
                    parser.ReadNextFragment();
                if(!(a < 1000))
                    Assert.Fail( "The parser has failed to finish." );
            }
		}

		[Test]
		public void FinishingOnOverclosed()
		{
			string HTML = "<HTML><HEAD><Title>The title</</</</</</</a></a></html></head></title>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                int	a;
                for(a = 0; (a < 0x1000) && ( !parser.Finished ); a++)
                    parser.ReadNextFragment();
                if(!(a < 1000))
                    Assert.Fail( "The parser has failed to finish." );
            }
		}

		[Test]
		public void FinishingOnOverclosedNoWordBreak()
		{
			string HTML = "<HTML><HEAD><Title>The title</</</</</</</a></a></html></head></title>";
            using( HTMLParser parser = new HTMLParser(
                       new StreamReader( new MemoryStream( Encoding.Default.GetBytes( HTML ) ) ) ) )
            {
                parser.BreakWords = false;

                int	a;
                for(a = 0; (a < 0x1000) && ( !parser.Finished ); a++)
                    parser.ReadNextFragment();
                if(!(a < 1000))
                    Assert.Fail( "The parser has failed to finish." );
            }
		}

		[Test]
		public void Attributes()
		{
            using( HTMLParser parser = CreateParser( "" ) )
            {
                HashMap hashMap = parser.ParseAttributes( "link   rel=\"stylesheet\"   HRef=\"/styles-site.css\" type = 'text/css' /" );
                Assert.AreEqual( 3, hashMap.Count );
                Assert.AreEqual( "stylesheet", hashMap[ "rel" ] );
                Assert.AreEqual( "/styles-site.css", hashMap[ "href" ] );
                Assert.AreEqual( "text/css", hashMap[ "type" ] );
            }
		}

		[Test]
		public void AttributesNoWordBreak()
		{
            using( HTMLParser parser = CreateParser( "" ) )
            {
                parser.BreakWords = false;
                HashMap hashMap = parser.ParseAttributes( "link   rel=\"stylesheet\"   HRef=\"/styles-site.css\" type = 'text/css' /" );
                Assert.AreEqual( 3, hashMap.Count );
                Assert.AreEqual( "stylesheet", hashMap[ "rel" ] );
                Assert.AreEqual( "/styles-site.css", hashMap[ "href" ] );
                Assert.AreEqual( "text/css", hashMap[ "type" ] );
            }
		}
	}

	[TestFixture]
	public class HtmlEntityReaderTests
	{
		private HtmlEntityReader _reader = null;

		[SetUp, TearDown]

		public void Clean()
		{
			_reader = null;
		}

		protected void Seed( string text )
		{
			_reader = new HtmlEntityReader( new StringReader( text ) );
		}

		[Test]
		public void Plain()
		{
			string seed = "Come&#160;and &lt;see&gt;";
			Seed( seed );
			StringBuilder sb = new StringBuilder();

			int len;
			while( !_reader.Eof )
			{
				sb.Append( (char) _reader.Read( false, true, out len ) );
				Assert.AreEqual( len, 1 );
			}

			Assert.AreEqual( sb.ToString(), seed );
		}

		[Test]
		public void Entities()
		{
			string seed = "Come&#160;and &lt;see&gt; &mdash; &copy; &laquo;HornHoof&trade; Inc&raquo;";
			Seed( seed );
			StringBuilder sb = new StringBuilder();

			int len;
			char ch;
			while( !_reader.Eof )
			{
				ch = (char) _reader.Read( true, true, out len );

				sb.Append( ch );
				switch( ch )
				{
				case (char) 160:
					Assert.AreEqual( len, 6 );
					break;
				case '<':
					goto case '>';
				case '>':
					Assert.AreEqual( len, 4 );
					break;
				case '—':
					Assert.AreEqual( len, "&mdash;".Length );
					break;
				case '©':
					Assert.AreEqual( len, "&copy;".Length );
					break;
				case '«':
					Assert.AreEqual( len, "&laquo;".Length );
					break;
				case '»':
					Assert.AreEqual( len, "&raquo;".Length );
					break;
				case '™':
					Assert.AreEqual( len, "&trade;".Length );
					break;
				default:
					Assert.AreEqual( len, 1 );
					break;
				}

			}

			Assert.AreEqual( sb.ToString(), "Come" + (char) 160 + "and <see> — © «HornHoof™ Inc»" );

		}

		[Test]
		public void PeekPlain()
		{
			string seed = "Come&#160;and &lt;see&gt;";
			Seed( seed );
			StringBuilder sb = new StringBuilder();

			int len;
			char chPeek, chRead;
			while( !_reader.Eof )
			{
				chPeek = (char) _reader.Read( false, false, out len );
				Assert.AreEqual( len, 1 );
				chRead = (char) _reader.Read( false, true, out len );
				Assert.AreEqual( len, 1 );

				Assert.AreEqual( chPeek, chRead );

				sb.Append( chRead );
			}

			Assert.AreEqual( sb.ToString(), seed );
		}

		[Test]
		public void PeekEntity()
		{
			string seed = "Come&#160;and &lt;see&gt;";
			Seed( seed );
			StringBuilder sb = new StringBuilder();

			int len;
			int lenTest;
			char chPeek, chRead;
			while( !_reader.Eof )
			{
				chPeek = (char) _reader.Read( true, false, out len );

				switch( chPeek )
				{
				case (char) 160:
					lenTest = 6;
					break;
				case '<':
					goto case '>';
				case '>':
					lenTest = 4;
					break;
				default:
					lenTest = 1;
					break;
				}
				Assert.AreEqual( len, lenTest );

				chRead = (char) _reader.Read( true, true, out len );
				Assert.AreEqual( len, lenTest );

				Assert.AreEqual( chPeek, chRead );

				sb.Append( chRead );
			}

			Assert.AreEqual( sb.ToString(), "Come" + (char) 160 + "and <see>" );
		}

		[Test]
		public void PeekMixed()
		{
			string seed = "Come&#160;and &lt;see&gt;";
			Seed( seed );
			StringBuilder sbPlain = new StringBuilder();
			StringBuilder sbEntity = new StringBuilder();

			int len;
			int lenTest;
			char chPeek, chRead;
			while( !_reader.Eof )
			{
				chPeek = (char) _reader.Read( true, false, out len );

				switch( chPeek )
				{
				case (char) 160:
					lenTest = 6;
					break;
				case '<':
					goto case '>';
				case '>':
					lenTest = 4;
					break;
				default:
					lenTest = 1;
					break;
				}
				Assert.AreEqual( lenTest, len );
				sbEntity.Append( chPeek );

				chRead = (char) _reader.Read( false, true, out len );
				Assert.AreEqual( len, 1 );
				sbPlain.Append( chRead );
			}

			Assert.AreEqual( sbPlain.ToString(), seed );
			Assert.AreEqual( sbEntity.ToString(), "Come" + (char) 160 + "#160;and <lt;see>gt;" );
		}

	}
}
