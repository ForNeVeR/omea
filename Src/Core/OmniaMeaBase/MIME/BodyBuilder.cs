// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Text;
using System.IO;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.MIME
{

    /**
     * MultiPartBodyBuilder class builds multi-part mime message
     * or plaintext message with uuencoded insertions
     */
    public class MultiPartBodyBuilder
    {
        private const int _UULinePreferredLen = 45;
        private const int _PlainTextFlowedLinePreferredLen = 72;
        private const int _PlainTextFlowedLineMaxLen = 78;

        public static string BuildPlainTextFlowedBody( string body, string charset )
        {
            StringBuilder bodyBuilder = StringBuilderPool.Alloc();
            try
            {
                string[] lines = body.Split( '\n' );

                foreach( string line in lines )
                {
                    string lineCopy = line;
                    while( lineCopy.Length > _PlainTextFlowedLineMaxLen )
                    {
                        int i;
                        for( i = _PlainTextFlowedLinePreferredLen; i < lineCopy.Length && !Char.IsWhiteSpace( lineCopy[ i ] ); ++i );
                        if( i == lineCopy.Length )
                        {
                            break;
                        }
                        bodyBuilder.Append( lineCopy.Substring( 0, i + 1 ) );
                        lineCopy = lineCopy.Substring( i + 1 );
                        if( lineCopy.Length > 0 )
                        {
                            bodyBuilder.Append( "\r\n" );
                        }

                    }
                    bodyBuilder.Append( lineCopy );
                    bodyBuilder.Append( '\n' );
                }

                return EscapeSinglePeriodsAndTranslateToCharset( bodyBuilder.ToString(), charset );
            }
            finally
            {
                StringBuilderPool.Dispose( bodyBuilder );
            }
        }

        /**
         * builds mail/news body with uuencoded attachments
         */
        public static string BuildBodyWithUUEncodedInsertions( string body, string charset, params string[] fullnames )
        {
            if( fullnames.Length == 0 )
            {
                return EscapeSinglePeriodsAndTranslateToCharset( body, charset );
            }
            StringBuilder bodyBuilder = StringBuilderPool.Alloc();
            try
            {
                bodyBuilder.Append( body );
                foreach( string fullname in fullnames )
                {
                    FileStream fs = OpenAttachment( fullname );
                    if( fs != null )
                    {
                        bodyBuilder.Append( "\r\nbegin 777 " );
                        bodyBuilder.Append( Path.GetFileName( fullname ) );
                        byte[] bytes = new byte[ _UULinePreferredLen ];
                        int read;
                        while( ( read = fs.Read( bytes, 0, bytes.Length ) ) > 0 )
                        {
                            bodyBuilder.Append( "\r\n" );
                            bodyBuilder.Append( UUParser._UUAlphabet[ read ] );
                            bodyBuilder.Append( UUParser.Encode( bytes, read ) );
                        }
                        bodyBuilder.Append( "\r\n`\r\nend" );
                    }
                }
                return EscapeSinglePeriodsAndTranslateToCharset( bodyBuilder.ToString(), charset );
            }
            finally
            {
                StringBuilderPool.Dispose( bodyBuilder );
            }
        }

        /**
         * builds MIME mail/news body for plaintext bodies
         */
        public static string BuildMIMEBody( string body, string bodyCharset, string bodyEncoding,
                                            out string boundary, params string[] fullnames )
        {
            string newBoundary = "----++Omea_Parts_Splitter" + new Random().NextDouble().ToString().Substring( 1 );

            StringBuilder bodyBuilder = StringBuilderPool.Alloc();
            try
            {

                // add MIME help string
                bodyBuilder.Append( "This is a multi-part message in MIME format.\r\n" );

                // encode and add body
                bodyEncoding = bodyEncoding.ToLower();
                bodyBuilder.Append( newBoundary );
                bodyBuilder.Append( "\r\nContent-Type: text/plain" );
                if( bodyCharset.Length > 0 )
                {
                    bodyBuilder.Append( "; charset=" );
                    bodyBuilder.Append( bodyCharset );
                }
                if( bodyEncoding == "quoted-printable" )
                {
                    bodyBuilder.Append( "\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\n" );
                    string[] lines = body.Split( '\n' );
                    foreach( string line in lines )
                    {
                        bodyBuilder.Append( MIMEParser.EncodeQuotedPrintable( bodyCharset, line.TrimEnd( '\r' ) ) );
                        bodyBuilder.Append( "\r\n" );
                    }
                }
                else if( bodyEncoding == "base64" )
                {
                    body = MIMEParser.EncodeBase64( bodyCharset, body );
                    bodyBuilder.Append( "\r\nContent-Transfer-Encoding: base64\r\n" );
                    /**
                     * split base64 body onto several lines
                     */
                    string line;
                    int i = 0;
                    int lineLen = _UULinePreferredLen * 4 / 3;
                    while( i < body.Length )
                    {
                        line = body.Substring( i, ( lineLen <= body.Length - i ) ? lineLen : body.Length - i );
                        bodyBuilder.Append( "\r\n" );
                        bodyBuilder.Append( line );
                        i += line.Length;
                    }
                }
                else
                {
                    bodyBuilder.Append( "\r\nContent-Transfer-Encoding: 8bit\r\n\r\n" );
                    bodyBuilder.Append( EscapeSinglePeriodsAndTranslateToCharset( body, bodyCharset ) );
                }

                // encode and add attachments
                byte[] bytes = new byte[ _UULinePreferredLen ];
                foreach( string fullname in fullnames )
                {
                    FileStream fs = OpenAttachment( fullname );
                    if( fs != null )
                    {
                        string filename = Path.GetFileName( fullname );
                        if( MIMEParser.Has8BitCharacters( filename ) )
                        {
                            filename = MIMEParser.CreateBase64MIMEString( bodyCharset, filename );
                        }
                        bodyBuilder.Append( "\r\n" );
                        bodyBuilder.Append( newBoundary );
                        string contentType = MIMEContentTypes.GetContentType( Path.GetExtension( fullname ) );
                        if( contentType == null )
                        {
                            bodyBuilder.Append( "\r\nContent-Type: application/octet-stream; name=\"" );
                        }
                        else
                        {
                            bodyBuilder.Append( "\r\nContent-Type: " );
                            bodyBuilder.Append( contentType );
                            bodyBuilder.Append( "; name=\"" );
                        }
                        bodyBuilder.Append( filename );
                        bodyBuilder.Append( "\"\r\nContent-Transfer-Encoding: base64\r\nContent-Disposition: attachment; filename=\"" );
                        bodyBuilder.Append( filename );
                        bodyBuilder.Append( "\"\r\n" );
                        int read;
                        while( ( read = fs.Read( bytes, 0, bytes.Length ) ) > 0 )
                        {
                            bodyBuilder.Append( "\r\n" );
                            bodyBuilder.Append( MIMEParser.EncodeBase64( bytes, read ) );
                        }
                    }
                }

                // finish multipart message
                bodyBuilder.Append( "\r\n" );
                bodyBuilder.Append( newBoundary );
                bodyBuilder.Append( "--" );

                // ignore the "--" prefix
                boundary = newBoundary.Substring( 2 );
                return bodyBuilder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( bodyBuilder );
            }
        }

        /**
         * builds MIME mail/news body for plaintext & HTML bodies
         */
        public static string BuildMIMEBody( string body, string HTMLBody, string bodyCharset, string bodyEncoding,
                                            out string boundary, params string[] fullnames )
        {
            return (boundary = string.Empty);
        }

        private static FileStream OpenAttachment( string fullname )
        {
            FileStream fs;
            try
            {
                fs = new FileStream( fullname, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
            }
            catch( Exception )
            {
                fs = null;
            }
            return fs;
        }

        private static string EscapeSinglePeriodsAndTranslateToCharset( string body, string charset )
        {
            string[] lines;
            lines = body.Split( '\n' );
            bool areSinglePeriods = false;
            for( int i = 0; i < lines.Length; ++i )
            {
                if( lines[ i ].StartsWith( "." ) )
                {
                    lines[ i ] = lines[ i ].Insert( 0, "." );
                    areSinglePeriods = true;
                }
            }
            StringBuilder bodyBuilder = StringBuilderPool.Alloc();
            try
            {
                if( areSinglePeriods )
                {
                    for( int i = 0; i < lines.Length; ++i )
                    {
                        bodyBuilder.Append( lines[ i ] );
                        bodyBuilder.Append( '\n' );
                    }
                    body = bodyBuilder.ToString();
                }
                bodyBuilder.Length = 0;
                byte[] bytes = MIMEParser.GetEncodingExceptionSafe( charset ).GetBytes( body );
                for( int i = 0; i < bytes.Length; ++i )
                {
                    bodyBuilder.Append( (char) bytes[ i ] );
                }
                return bodyBuilder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( bodyBuilder );
            }
        }
    }
}
