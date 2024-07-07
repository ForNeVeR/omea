// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Text;
using JetBrains.Omea.RTF;
using NUnit.Framework;

namespace OmniaMeaBaseTests
{
	/// <summary>
	/// Summary description for RTFParserTests.
	/// </summary>
    [TestFixture]
    public class RTFParserTests
	{
		public RTFParserTests()
		{
		}

        private RTFParser Parse( string rtf )
        {

            RTFParser parser = new RTFParser( );
            parser.Parse( new StreamReader( new MemoryStream( Encoding.Default.GetBytes( rtf ) ) ) );
            return parser;
        }

        [Test]
        public void ReadDefaultCodePage()
        {
            RTFParser parser = Parse( "{\\rtf1\\ansi\\ansicpg866}" );
            Assert.AreEqual( 866, parser.DefaultCodePage );
            parser.Parse( "{\\rtf1\\ansi}" );
            Assert.AreEqual( -1, parser.DefaultCodePage );
        }
        [Test, ExpectedException( typeof( ArgumentNullException ) )]
        public void NullStringParameter()
        {
            string rtf = null;
            new RTFParser( ).Parse( rtf );
        }
        [Test, ExpectedException( typeof( ArgumentNullException ) )]
        public void NullReaderParameter()
        {
            new RTFParser( ).Parse( (StreamReader)null );
        }

        [Test, Ignore( "RFC was broken by MS"), ExpectedException( typeof( ParenthesisMismatching ) )]
        public void BadFormat_NoClosingParenthesis()
        {
            new RTFParser( ).Parse( "{{}" );
        }
        [Test, Ignore( "RFC was broken by MS"), ExpectedException( typeof( ParenthesisMismatching ) )]
        public void BadFormat_NoOpeningParenthesis()
        {
            new RTFParser( ).Parse( "{}}" );
        }
        [Test, Ignore( "RFC was broken by MS"), ExpectedException( typeof( FontMismatching ) )]
        public void ParseFontWasNotIncludedInFontTbl()
        {
            Parse( "{{\\fonttbl{\\f0}{\\f1}}{\\f3}}" );
        }
        [Test, ExpectedException( typeof( NoExpectedParameter ) )]
        public void NoParameterFor_ansicpg()
        {
            Parse( "{\\ansicpg}" );
        }
        [Test, ExpectedException( typeof( NoExpectedParameter ) )]
        public void NoParameterFor_bin()
        {
            Parse( "{\\bin}" );
        }
        [Test, ExpectedException( typeof( NoExpectedParameter ) )]
        public void NoParameterFor_fcharset()
        {
            Parse( "{\\fcharset}" );
        }
        [Test, ExpectedException( typeof( NoExpectedParameter ) )]
        public void ClearHasParameterFlag()
        {
            Parse( "{\\fcharset123\\fcharset}" );
        }
        [Test]
        public void ReInitGroupDeepCount()
        {
            RTFParser parser = new RTFParser();
            try
            {
                parser.Parse( "{{}" );
            }
            catch ( ParenthesisMismatching )
            {
                parser.Parse( "{{}}" );
            }
        }
        [Test]
        public void CheckFontsInfos()
        {
            RTFParser parser = Parse( "{\\fonttbl{\\f100\\fcharset101}{\\f102\\fcharset103}}" );
            FontInfo[] fonts = parser.GetFontTableInfo();
            Assert.AreEqual( 2, fonts.Length );
            FontInfo info = fonts[0];
            Assert.AreEqual( 100, info.FontNum );
            info = fonts[1];
            Assert.AreEqual( 102, info.FontNum );
        }
        [Test]
        public void ReInitParseFontTbl()
        {
            RTFParser parser = Parse( "{\\fonttbl{\\f0\\fcharset1}{\\f1\\fcharset2}}" );
            FontInfo[] fonts = parser.GetFontTableInfo();
            Assert.AreEqual( 2, fonts.Length );
            parser.Parse( "{\\fonttbl{\\f2\\fcharset2}{\\f3\\fcharset1}}" );
            fonts = parser.GetFontTableInfo();
            Assert.AreEqual( 2, fonts.Length );
        }
        [Test]
        public void ParseFontTbl()
        {
            RTFParser parser = Parse( "{\\fonttbl{\\f0\\fcharset12}{\\f1\\fcharset23}}" );
            FontInfo[] fonts = parser.GetFontTableInfo();
            Assert.AreEqual( 2, fonts.Length );
        }
        [Test]
        public void TestForExtraRegisteredFonts()
        {
            RTFParser parser = Parse( "{{\\fonttbl{\\f0\\fcharset12}{\\f1\\fcharset23}}{\\f1\\fcharset45}}" );
            FontInfo[] fonts = parser.GetFontTableInfo();
            Assert.AreEqual( 2, fonts.Length );
        }
        [Test]
        public void GetBodySimple()
        {
            RTFParser parser = Parse( "{\\body hello from rtf parser}" );
            Assert.AreEqual( "hello from rtf parser", parser.PlainText );
        }
        [Test]
        public void GetBodyFromHexAnsicpg1251()
        {
            RTFParser parser = Parse( "{\\ansicpg1251\\insrsid2717722 \\'cf\\'f0\\'e8\\'e2\\'e5\\'f2}" );
            Assert.AreEqual( "Привет", parser.PlainText );
        }
        [Test]
        public void GetBodyFromHexAnsicpg1251WithParagraph()
        {
            RTFParser parser = Parse( "{\\ansicpg1251\\insrsid2717722 \\'cf\\'f0\\'e8\\'e2\\'e5\\'f2\\par\\'cf\\'f0\\'e8\\'e2\\'e5\\'f2-\\'cf\\'f0\\'e8\\'e2\\'e5\\'f2\\par\\'cf\\'f0\\'e8\\'e2\\'e5\\'f2-\\'cf\\'f0\\'e8\\'e2\\'e5\\'f2\\par\\'cf\\'f0\\'e8\\'e2\\'e5\\'f2-\\'cf\\'f0\\'e8\\'e2\\'e5\\'f2\\par\\'cf\\'f0\\'e8\\'e2\\'e5\\'f2}" );
            Assert.AreEqual( "Привет\r\nПривет-Привет\r\nПривет-Привет\r\nПривет-Привет\r\nПривет", parser.PlainText );
        }
        [Test]
        public void GetBodyFromHexAnsicpg1251AndParTag()
        {
            RTFParser parser = Parse( "{\\ansicpg1251\\insrsid2717722 \\'cf\\par\\'e8}" );
            Assert.AreEqual( "П\r\nи", parser.PlainText );
        }
        [Test]
        public void GetBodyFromHexChainAnsicpg1251()
        {
            RTFParser parser = Parse( "{\\ansicpg1251\\insrsid2717722 \\'cf\\'f0\\'e8\\'e2\\'e5\\'f2}" );
            Assert.AreEqual( "Привет", parser.PlainText );
        }
        [Test]
        public void IgnoreHexInIgnoredGroup()
        {
            RTFParser parser = Parse( "{{\\*\\ansicpg1251\\insrsid2717722 \\'cff0e8e2e5f2}{\\body Hello}}" );
            Assert.AreEqual( "Hello", parser.PlainText );
        }
        [Test]
        public void IgnoreHexInIgnoredGroup2()
        {
            RTFParser parser = Parse( "{{\\*\\ansicpg1251\\insrsid2717722 \\'cf\\'f0\\'e8\\'e2\\'e5-\\'f2}{\\body Hello}}" );
            Assert.AreEqual( "Hello", parser.PlainText );
        }
        [Test]
        public void GetBodyFromHexChain5000Symbols()
        {
                const int COUNT = 5000;
                string rtf = "{\\ansicpg1251\\insrsid2717722 ";
                for ( int i = 0; i < COUNT; ++i )
                {
                    rtf += "\\'cf";
                }
                rtf+="}";
                RTFParser parser = Parse( rtf );

                Assert.AreEqual( COUNT, parser.PlainText.Length );
                foreach ( char ch in parser.PlainText.ToCharArray() )
                {
                    Assert.AreEqual( 'П', ch );
                }
        }
        [Test]
        public void GetBodyFromHexChain5000Symbols2()
        {
            const int COUNT = 5000;
            string rtf = "{\\ansicpg1251\\insrsid2717722 ";
            for ( int i = 0; i < COUNT; ++i )
            {
                rtf += "\\'cf";
            }
            rtf+="}\\par";
            rtf += "{\\ansicpg1251\\insrsid2717722 ";
            for ( int i = 0; i < COUNT; ++i )
            {
                rtf += "\\'cf";
            }
            rtf +="}\\par";
            RTFParser parser = Parse( rtf );

            Assert.AreEqual( COUNT*2 + 4, parser.PlainText.Length );
            char[] charArray = parser.PlainText.ToCharArray();
            int j = 0;
            for ( j = 0; j < COUNT; ++j )
            {
                Assert.AreEqual( 'П', charArray[j] );
            }
            Assert.AreEqual( '\r', charArray[j++] );
            Assert.AreEqual( '\n', charArray[j++] );
            int k = 0;
            for ( k = j; k < j+COUNT; ++k )
            {
                Assert.AreEqual( 'П', charArray[k] );
            }

            Assert.AreEqual( '\r', charArray[k++] );
            Assert.AreEqual( '\n', charArray[k++] );
        }
        [Test]
        public void GetBodyFromHexRussianCharset()
        {
            RTFParser parser = Parse( "{\\ansicpg1252{\\fonttbl{\\f0\\fcharset204}}\\f0\\insrsid2717722 \\'cf\\'f0\\'e8\\'e2\\'e5\\'f2}" );
            Assert.AreEqual( "Привет", parser.PlainText );
        }
        [Test]
        public void GetBodyAfterBinProp()
        {
            RTFParser parser = Parse( "{\\bin10 1234567890\\justbody It is just body}" );
            Assert.AreEqual( "It is just body", parser.PlainText );
        }
        [Test]
        public void IgnoreNestedSkipping()
        {
            RTFParser parser = Parse( "{{\\author{\\body 123}}{\\body 234}}" );
            Assert.AreEqual( "234", parser.PlainText );
        }
        [Test]
        public void IgnoreGroupIfAsteriskControlWordIncluded()
        {
            RTFParser parser = Parse( "{\\body 123\\*\\body 234{\\body 345}}" );
            Assert.AreEqual( "123", parser.PlainText );
        }
        [Test]
        public void CharsetWithoutFont()
        {
            RTFParser parser = Parse( "{\\fonttbl{\\fcharset204}}" );
            FontInfo[] fonts = parser.GetFontTableInfo();
            Assert.AreEqual( 0, fonts.Length );
        }
        [Test]
        public void CharsetWithoutFont2()
        {
            RTFParser parser = Parse( "{\\fonttbl{\\f0\\fcharset204}{\\fcharset204}}" );
            FontInfo[] fonts = parser.GetFontTableInfo();
            Assert.AreEqual( 1, fonts.Length );
        }
        [Test]
        public void IgnoreSetFontBeforeFontTable()
        {
            RTFParser parser = Parse( "{{\\f0}{\\fonttbl{\\f0\\fcharset204}{\\fcharset204}}}" );
            FontInfo[] fonts = parser.GetFontTableInfo();
            Assert.AreEqual( 1, fonts.Length );
        }
        [Test]
        public void TestForFnilTag()
        {
            RTFParser parser = Parse( "{\\fonttbl{\\f0\\fnil}}" );
            FontInfo[] fonts = parser.GetFontTableInfo();
            Assert.AreEqual( 1, fonts.Length );
        }
    }
}
