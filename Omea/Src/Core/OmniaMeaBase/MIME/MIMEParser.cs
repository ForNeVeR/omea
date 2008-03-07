/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Text;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.MIME
{
    /** 
     * MIMEParser class helps to extract encoded text information from messages headers
     * Multiline and multipart encoded strings are supported
     * Each multipart encoded text is built in a single string returned
     */
    public class MIMEParser
    {
        private const string _mimeBegin = "=?";
        private const char _mimeSeparator = '?';

        public class BadEncodedWordStructure : Exception
        {
            public BadEncodedWordStructure( string str )
                : base( str ) {}
        }

        public static bool IsMIMEString( string str )
        {
            return str.StartsWith( _mimeBegin );
        }

        public static bool ContainsMIMEStrings( string str )
        {
            return str.IndexOf( _mimeBegin ) >= 0;
        }

        public static string DecodeMIMEString( string str )
        {
            StringBuilder output = StringBuilderPool.Alloc();
            try 
            {
                int begin = 0;
                int end;
                while( ( begin = str.IndexOf( _mimeBegin, begin ) ) >= 0 )
                {
                    /** 
                     * do not search mime end separator directly, enumerate mime
                     * header parts instead -- this avoids incorrect determination of
                     * header end in case of quotedprintable header
                     */
                    end = begin + 1;
                    for( int i = 0; i < 3; ++i )
                    {
                        end = str.IndexOf( _mimeSeparator, end + 1 );
                        if( end <= 0 )
                        {
                            return output.ToString();
                        }
                    }

                    string encodedWord = str.Substring( begin + 2, end - begin - 2 );
                    string[] sections = encodedWord.Split( '?' );
                    if( sections.Length != 3 )
                    {
                        throw new BadEncodedWordStructure( "Number of sections in encoded word should be equal to 3" );
                    }

                    if( sections[ 1 ] == "Q" || sections[ 1 ] == "q" ) // quoted-pritable
                    {
                        ParseQuotedPrintable( sections[ 0 ], sections[ 2 ].Replace( '_', ' ' ), output );
                    }
                    else if( sections[ 1 ] == "B" || sections[ 1 ] == "b" ) // base64
                    {
                        ParseBase64( sections[ 0 ], sections[ 2 ], output );
                    }
                    else
                    {
                        throw new BadEncodedWordStructure( "Unknown MIME encoding: " + sections[ 1 ] );
                    }

                    begin = end;
                }
                return output.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( output );
            }
        }

        public static string CreateQuotedPrintableMIMEString( string charset, string text )
        {
            StringBuilder builder = StringBuilderPool.Alloc();
            try 
            {
                builder.Append( _mimeBegin );
                builder.Append( charset );
                builder.Append( "?Q?" );
                EncodeQuotedPrintable( GetEncodingExceptionSafe( charset ).GetBytes( text ), builder );
                builder.Append( "?=" );
                return builder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( builder ) ;
            }
        }

        public static string CreateBase64MIMEString( string charset, string text )
        {
            StringBuilder builder = StringBuilderPool.Alloc();
            try 
            {
                builder.Append( _mimeBegin );
                builder.Append( charset );
                builder.Append( "?B?" );
                builder.Append( EncodeBase64( GetEncodingExceptionSafe( charset ).GetBytes( text ) ) );
                builder.Append( "?=" );
                return builder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( builder ) ;
            }
        }

        public static string DecodeQuotedPrintable( string charset, string text )
        {
            StringBuilder builder = StringBuilderPool.Alloc();
            try 
            {
                ParseQuotedPrintable( charset, text, builder );
                return builder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( builder ) ;
            }
        }

        public static byte[] DecodeQuotedPrintable( string text )
        {
            ArrayList bytes = ArrayListPool.Alloc();
            try 
            {
                string[] lines = text.Split( '\n' );

                for ( int i = 0; i < lines.Length; ++i )
                {
                    if( i > 0 )
                    {
                        bytes.Add( (byte)'\n' );
                    }
                    char[] chars = lines[ i ].ToCharArray();
                    int lineLen = chars.Length;
                    for ( int j = 0; j < lineLen; ++j )
                    {
                        char c = chars[ j ];
                        if( c != '=' )
                        {
                            bytes.Add( (byte) c );
                        }
                        else
                        {
                            if( ++j < lineLen - 1 && chars[ j ] != '\r' )
                            {
                                try
                                {
                                    bytes.Add( Convert.ToByte( new string( chars, j, 2 ), 16 ) );
                                }
                                catch( Exception )
                                {
                                    // just ignore any unparsable garbage
                                }
                            }
                            ++j;
                        }
                    }
                }
                return (byte[]) bytes.ToArray( typeof( byte ) );
            }
            finally
            {
                ArrayListPool.Dispose( bytes );
            }
        }

        public static string DecodeBase64( string charset, string text )
        {
            byte[] bytes = DecodeBase64( text );
            if( bytes == null )
            {
                return string.Empty;
            }
            return GetEncodingExceptionSafe( charset ).GetString( bytes );
        }

        public static byte[] DecodeBase64( string text )
        {
            byte[] bytes = null;

            for( int len = text.Length; ; )
            {
                try
                {
                    bytes = Convert.FromBase64String( text );
                    break;
                }
                catch
                {
                    if( len <= 4 )
                    {
                        break;
                    }
                    text = text.Substring( 0, --len );
                }
            }
            return bytes;
        }

        public static string EncodeQuotedPrintable( string charset, string text )
        {
            StringBuilder builder = StringBuilderPool.Alloc();
            try 
            {
                Encoding encoding = GetEncodingExceptionSafe( charset );
                EncodeQuotedPrintable( encoding.GetBytes( text ), builder );
                return builder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( builder ) ;
            }
        }

        public static string EncodeQuotedPrintable( byte[] bytes )
        {
            StringBuilder builder = StringBuilderPool.Alloc();
            try 
            {
                EncodeQuotedPrintable( bytes, builder );
                return builder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( builder ) ;
            }
        }

        public static string EncodeBase64( string charset, string text )
        {
            Encoding encoding = GetEncodingExceptionSafe( charset );
            return EncodeBase64( encoding.GetBytes( text ) );
        }

        public static string EncodeBase64( byte[] bytes )
        {
            return Convert.ToBase64String( bytes );
        }

        public static string EncodeBase64( byte[] bytes, int count )
        {
            return Convert.ToBase64String( bytes, 0, count );
        }

        public static bool Has8BitCharacters( string str )
        {
            int len = str.Length;
            for( int i = 0; i < len; ++i )
            {
                char c = str[ i ];
                if( c < 0 || c  > 127 )
                {
                    return true;
                }
            }
            return false;
        }

        public static string TranslateRawStringInCharset( string charset, string s )
        {
            if( s.Length == 0 )
            {
                return s;
            }
            Encoding to = GetEncodingExceptionSafe( charset );
            string result;
            try
            {
                byte[] bytes = new byte[ s.Length ];
                for( int i = 0; i < bytes.Length; ++i )
                {
                    bytes[ i ] = (byte) s[ i ];
                }
                result = to.GetString( bytes );
            }
            catch
            {
                result = s;
            }
            return result;
        }

        #region implementation details

        private static void ParseQuotedPrintable( string charset, string text, StringBuilder output )
        {
            Encoding encoding = GetEncodingExceptionSafe( charset );
            string[] lines = text.Split( '\n' );
            bool wrap = true;

            ArrayList bytes = ArrayListPool.Alloc();
            try 
            {
                for( int i = 0; i < lines.Length; ++i )
                {
                    if( !wrap )
                    {
                        bytes.Add( (byte)'\n' );
                    }
                    string line = lines[ i ];
                    int lineLen = line.Length;
                    for ( int j = 0; j < lineLen; ++j )
                    {
                        char c = line[ j ];
                        if( c != '=' )
                        {
                            wrap = false;
                            bytes.Add( (byte) c );
                        }
                        else
                        {
                            wrap = true;
                            if( ++j < lineLen - 1 && line[ j ] != '\r' )
                            {
                                try
                                {
                                    bytes.Add( Convert.ToByte( line.Substring( j, 2 ), 16 ) );
                                    wrap = false;
                                }
                                catch( Exception )
                                {
                                    // just ignore any unparsable garbage
                                }
                            }
                            ++j;
                        }
                    }
                }
                output.Append( encoding.GetString( (byte[]) bytes.ToArray( typeof( byte ) ) ) );
            }
            catch
            {
                output.Append( text );
            }
            finally
            {
                ArrayListPool.Dispose( bytes );
            }
        }

        private static void EncodeQuotedPrintable( byte[] bytes, StringBuilder output )
        {
            int len = bytes.Length;
            for( int i = 0; i < len; ++i )
            {
                byte b = bytes[ i ];
                if( b == '=' || b == '_' || b == '.' || b > 127 || b <= ' ' )
                {
                    output.Append( '=' );
                    output.Append( b.ToString( "x2" ) );
                }
                else
                {
                    output.Append( (char) b );
                }
            }
        }

        private static void ParseBase64( string charset, string text, StringBuilder output )
        {
            Encoding encoding = GetEncodingExceptionSafe( charset );
            output.Append( encoding.GetString( Convert.FromBase64String( text ) ) );
        }

        public static Encoding GetEncodingExceptionSafe( string charset )
        {
            Encoding encoding;
            try
            {
                encoding = Encoding.GetEncoding( charset );
            }
            catch( Exception )
            {
                encoding = Encoding.Default;
            }
            return encoding;
        }

        #endregion
    }
}