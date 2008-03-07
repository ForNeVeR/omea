/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Text;
using NUnit.Framework;
using JetBrains.Omea.MIME;

namespace OmniaMeaBaseTests
{
    [TestFixture]
    public class MIMEParserTests
    {
        /*[Test]
        public void TestSimpleBase64Header()
        {
            string subject = "=?KOI8-R?B?89XC1sXL1A==?=";
            if( !MIMEParser.IsMIMEString( subject ) )
                throw new Exception( "=?KOI8-R?B?89XC1sXL1A==?= is not recognized as MIME string" );
            string decoded = MIMEParser.DecodeMIMEString( subject );
            if( decoded != "Субжект" )
                throw new Exception( "=?KOI8-R?B?89XC1sXL1A==?= should be equal to Субжект, but it is decoded to " + decoded );
        }

        [Test]
        public void TestMultipartBase64Header()
        {
            string subject = "=?KOI8-R?B?89XC1sXL1A==?=\r\n=?KOI8-R?B?89XC1sXL1A==?=";
            if( !MIMEParser.IsMIMEString( subject ) )
                throw new Exception( "=?KOI8-R?B?89XC1sXL1A==?=\\r\\n=?KOI8-R?B?89XC1sXL1A==?= is not recognized as MIME string" );
            string decoded = MIMEParser.DecodeMIMEString( subject );
            if( decoded != "СубжектСубжект" )
                throw new Exception( "=?KOI8-R?B?89XC1sXL1A==?=\\r\\n=?KOI8-R?B?89XC1sXL1A==?= should be equal to СубжектСубжект, but it is decoded to " + decoded );
        }

        [Test]
        public void TestSimpleQuotedPrintableHeader()
        {
            string subject = "=?us-ascii?q?h=65llo_world?=";
            if( !MIMEParser.IsMIMEString( subject ) )
                throw new Exception( "=?us-ascii?q?h=65llo_world?= is not recognized as MIME string" );
            string decoded = MIMEParser.DecodeMIMEString( subject );
            if( decoded != "hello world" )
                throw new Exception( "=?us-ascii?q?h=65llo_world?= should be equal to \"hello world\", but it is decoded to " + decoded );
        }

        [Test]
        public void TestMixedHeader()
        {
            string subject = "=?KOI8-R?B?89XC1sXL1A==?=\r\n=?us-ascii?q?=3A_h=65llo_world?=";
            if( !MIMEParser.IsMIMEString( subject ) )
                throw new Exception( "=?KOI8-R?B?89XC1sXL1A==?=\r\n=?us-ascii?q?=3A_h=65llo_world?= is not recognized as MIME string" );
            string decoded = MIMEParser.DecodeMIMEString( subject );
            if( decoded != "Субжект: hello world" )
                throw new Exception( "=?KOI8-R?B?89XC1sXL1A==?=\r\n=?us-ascii?q?=3A_h=65llo_world?= should be equal to \"Субжект: hello world\", but it is decoded to " + decoded );
        }*/

        [Test]
        public void TestUUEncodeDecode()
        {
            /**
             * NOTE: the length of test string should be a multiple of 3
             */
            string plainText = "This is plain text";
            byte[] bytes = Encoding.ASCII.GetBytes( plainText );
            string uustr = UUParser.Encode( bytes, bytes.Length );
            bytes = UUParser.Decode( uustr );
            string decodedStr = Encoding.ASCII.GetString( bytes );
            Console.WriteLine( decodedStr );
            Assert.IsTrue( plainText == decodedStr );
        }

        [Test]
        public void TestEmptyBoundary()
        {
            MultiPartBodyParser parser = new MultiPartBodyParser( "", "charset=koi8-r; boundary=", "base64", "koi8-r" );
            parser.GetParts();
            parser = new MultiPartBodyParser( "", " boundary=", "base64", "koi8-r" );
            parser.GetParts();
            parser = new MultiPartBodyParser( "", "boundary=", "base64", "koi8-r" );
            parser.GetParts();
            parser = new MultiPartBodyParser( "", "charset=\"koi8-r\"; boundary= ", "base64", "koi8-r" );
            parser.GetParts();
        }

        [Test]
        public void TestChar2ByteConversion()
        {
            StringBuilder chars = new StringBuilder();
            byte b = 0;
            for( int i = 0; i < 256; ++i )
            {
                chars.Append( (char) b );
                b++;
            }
            string s = chars.ToString();
            byte[] bytes = new byte[ 256 ];
            for( int i = 0; i < 256; ++i )
            {
                bytes[ i ] = (byte) s[ i ];
            }
            for( int i = 0; i < 256; ++i )
            {
                if( bytes[ i ] != (byte) i )
                {
                    throw new Exception( "i != i!" );
                }
            }
        }
    }
}
