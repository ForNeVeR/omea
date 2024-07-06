// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using NUnit.Framework;
using JetBrains.Omea.MailParser;

namespace JetBrains.Omea.MailParser.Tests
{
    [TestFixture]
    public class BodyParserTest
    {
        private MailBodyParser _parser;

        private void AssertEquals( object expected, object actual )
        {
            Assert.AreEqual( expected, actual );
        }

        private MailBodyParser CreateParser( string body )
        {
            _parser = new MailBodyParser( body, 10 );
            return _parser;
        }

        private void VerifyPara( int index, string body, ParagraphType type )
        {
            Assert.AreEqual( body, _parser.GetParagraph( index ).Text );
            Assert.AreEqual( type, _parser.GetParagraph( index ).Type );
        }

        [Test] public void BasicTest()
        {
            MailBodyParser parser = CreateParser( "This is a test message" );
            Assert.AreEqual( 1, parser.ParagraphCount );

            MailBodyParser.Paragraph para = parser.GetParagraph( 0 );
            Assert.IsNotNull( para );
            Assert.AreEqual( "This is a test message", para.Text );
            Assert.AreEqual( ParagraphType.Plain, para.Type );
        }

        [Test] public void TestMergeLines()
        {
            MailBodyParser parser = CreateParser( "This is a test\r\nmessage" );
            AssertEquals( "This is a test message", parser.GetParagraph( 0 ).Text );
        }

        [Test] public void TestFormatFlowed()
        {
            MailBodyParser parser = CreateParser( "This is a test \r\nmessage" );
            AssertEquals( "This is a test message", parser.GetParagraph( 0 ).Text );
        }

        [Test] public void TestTwoPara()
        {
            MailBodyParser parser = CreateParser( "This is a test\n\nmessage" );
            Assert.AreEqual( 2, parser.ParagraphCount );
            Assert.AreEqual( "This is a test", parser.GetParagraph( 0 ).Text );
            Assert.AreEqual( "message", parser.GetParagraph( 1 ).Text );
        }

        [Test] public void TestWhitespaceLines()
        {
            MailBodyParser parser = CreateParser( "This is a test\n \t\nmessage" );
            Assert.AreEqual( 2, parser.ParagraphCount );
            Assert.AreEqual( "This is a test", parser.GetParagraph( 0 ).Text );
            Assert.AreEqual( "message", parser.GetParagraph( 1 ).Text );
        }

        [Test] public void TestFixedPara()
        {
            MailBodyParser parser = CreateParser( "  This is a test\n  message" );
            Assert.AreEqual( 2, parser.ParagraphCount );
            VerifyPara( 0, "  This is a test", ParagraphType.Fixed );
            VerifyPara( 1, "  message", ParagraphType.Fixed );
        }

        [Test] public void TestFixedParaVariable()
        {
            MailBodyParser parser = CreateParser( "  This is a test\n    message" );
            Assert.AreEqual( 2, parser.ParagraphCount );
            VerifyPara( 0, "  This is a test", ParagraphType.Fixed );
            VerifyPara( 1, "    message", ParagraphType.Fixed );
        }

        [Test] public void TestFixedParaVariable2()
        {
            CreateParser( "This is a\n  test\nmessage" );
            Assert.AreEqual( 3, _parser.ParagraphCount );
            VerifyPara( 0, "This is a", ParagraphType.Fixed );
            VerifyPara( 1, "  test", ParagraphType.Fixed );
            VerifyPara( 2, "message", ParagraphType.Fixed );
        }

        [Test] public void TestFixedParaVariable3()
        {
            CreateParser( "This is a\ntest\n  message" );
            AssertEquals( 3, _parser.ParagraphCount );
            VerifyPara( 0, "This is a", ParagraphType.Fixed );
            VerifyPara( 1, "test", ParagraphType.Fixed );
            VerifyPara( 2, "  message", ParagraphType.Fixed );
        }

        [Test] public void TestFixedEmptyLine()
        {
            CreateParser( " This is\n  a\n\n test\n message" );
            AssertEquals( 5, _parser.ParagraphCount );
        }

        [Test] public void TestFirstLineIndent()
        {
            MailBodyParser parser = CreateParser( "  This is a\ntest message" );
            Assert.AreEqual( 1, parser.ParagraphCount );
            VerifyPara( 0, "  This is a test message", ParagraphType.Plain );
        }

        [Test] public void TestQuoting()
        {
            MailBodyParser parser = CreateParser( "> Quoted text" );
            VerifyPara( 0, "Quoted text", ParagraphType.Plain );
            Assert.AreEqual( 1, parser.GetParagraph( 0 ).QuoteLevel );
            Assert.AreEqual( "", parser.GetParagraph( 0 ).QuotePrefix );
        }

        [Test] public void TestQuotePrefix()
        {
            MailBodyParser parser = CreateParser( "DJ> Quoted text" );
            VerifyPara( 0, "Quoted text", ParagraphType.Plain );
            Assert.AreEqual( "DJ", parser.GetParagraph( 0 ).QuotePrefix );
        }

        [Test] public void TestMultilineQuoting()
        {
            MailBodyParser parser = CreateParser( "> Quoted quoted quoted\n> text" );
            Assert.AreEqual( 1, parser.ParagraphCount );
            VerifyPara( 0, "Quoted quoted quoted text", ParagraphType.Plain );
        }

        [Test] public void TestSig()
        {
            MailBodyParser parser = CreateParser( "-- \nDmitry");
            Assert.AreEqual( 2, parser.ParagraphCount );
            VerifyPara( 0, "-- ", ParagraphType.Sig );
        }

        [Test] public void TestAfterSig()
        {
            MailBodyParser parser = CreateParser( "-- \nDmitry\n\nYour text" );
            AssertEquals( 4, parser.ParagraphCount );
            VerifyPara( 3, "Your text", ParagraphType.Plain );
        }

        [Test] public void TestShortLines()
        {
            MailBodyParser parser = CreateParser( "Short\nlines" );
            Assert.AreEqual( 2, parser.ParagraphCount );
            VerifyPara( 0, "Short", ParagraphType.Fixed );
        }

        [Test] public void TestVariableLines()
        {
            MailBodyParser parser = CreateParser( "Alpha beta gamma delta\nepsilon\nzeta theta iota kappa" );
            Assert.AreEqual( 3, parser.ParagraphCount );
        }

        [Test] public void TestShortLastLine()
        {
            MailBodyParser parser = new MailBodyParser( "Alpha beta gamma delta\nepsilon zeta theta iota\nkappa", 20 );
            Assert.AreEqual( 1, parser.ParagraphCount );
        }

        [Test] public void TestLongQuotePrefix()
        {
            MailBodyParser parser = CreateParser( "----------------> " );
            Assert.AreEqual( 0, parser.GetParagraph( 0 ).QuoteLevel );
        }

        [Test] public void TestQuotePrefixWithSpaces()
        {
            CreateParser( "> > Quoted text" );
            AssertEquals( 2, _parser.GetParagraph( 0 ).QuoteLevel );
        }

        [Test] public void TestQuoteEmptyLine()
        {
            CreateParser( "> Quoted text\n>\n> Quoted text 2" );
            AssertEquals( 2, _parser.ParagraphCount );
        }

        [Test] public void TestOutlookQuoteStart()
        {
            CreateParser( "----- Original message -----\n> Quoted quoted quoted text\nMy text text is here" );
            AssertEquals( 3, _parser.ParagraphCount );
            VerifyPara( 0, "----- Original message -----", ParagraphType.Service );
            VerifyPara( 1, "Quoted quoted quoted text", ParagraphType.Plain );
            VerifyPara( 2, "My text text is here", ParagraphType.Plain );
            AssertEquals( false, _parser.GetParagraph( 2 ).OutlookQuote );
        }

        [Test] public void TestOutlookQuoteAfterText()
        {
            CreateParser( "My text is here\n----- Original message -----\nQuoted quoted quoted text" );
            AssertEquals( 3, _parser.ParagraphCount );
            VerifyPara( 0, "My text is here", ParagraphType.Plain );
            AssertEquals( ParagraphType.Service, _parser.GetParagraph( 1 ).Type );
            AssertEquals( true, _parser.GetParagraph( 2 ).OutlookQuote );
        }

        [Test] public void TestGetQuoteLevel()
        {
            Assert.AreEqual(1, MailBodyParser.GetQuoteLevel( "> Quoted text" ) );
        }

        [Test] public void TestGetQuotePrefix()
        {
            Assert.AreEqual("DJ", MailBodyParser.GetQuotePrefix( "DJ> Quoted text" ) );
        }

        [Test] public void TestStripQuoting()
        {
            AssertEquals( "Quoted text", MailBodyParser.StripQuoting( "> Quoted text" ) );
            AssertEquals( "", MailBodyParser.StripQuoting(">") );
            AssertEquals( "Quoted text", MailBodyParser.StripQuoting( "> > Quoted text" ) );
        }

        [Test] public void SeveralWordsBeforeQuoting()
        {
            string test = "Test Test Test > Test Test";
            AssertEquals( test, MailBodyParser.StripQuoting( test ) );
            AssertEquals( "", MailBodyParser.GetQuotePrefix( test ) );
            AssertEquals( 0, MailBodyParser.GetQuoteLevel( test ) );
        }
    }
}
