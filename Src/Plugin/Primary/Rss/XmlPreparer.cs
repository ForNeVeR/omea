// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Text;
using System.Xml;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Summary description for XmlPreparer.
	/// </summary>
	internal class XmlPreparer
	{
        private static string _HtmlEntities = null;

        private const int _Enc_Invalid   = -1; //Bad Encoding
        private const int _Enc_UniCodeBE =  0; //Unicode Big Endian
        private const int _Enc_UniCode   =  1; //Unicode Little Endian
        private const int _Enc_UCS4BE    =  2; //UCS4 BigEndian
        private const int _Enc_UCS4BEB   =  3; //UCS4 BigEnding with Byte order mark
        private const int _Enc_UCS4      =  4; //UCS4 Little Endian
        private const int _Enc_UCS4B     =  5; //UCS4 Little Ending with Byte order mark
        private const int _Enc_UCS434    =  6; //UCS4 order 3412
        private const int _Enc_UCS434B   =  7; //UCS4 order 3412 with Byte order mark
        private const int _Enc_UCS421    =  8; //UCS4 order 2143
        private const int _Enc_UCS421B   =  9; //UCS4 order 2143 with Byte order mark
        private const int _Enc_EBCDIC    = 10; //EBCDIC
        private const int _Enc_UTF8      = 11; //UTF8
        private const int _Enc_ASCII     = 12; //ASCII

        private static int[,] _EncodingTable = {
                                                   //           Unknown         0000          feff            fffe            efbb            3c00            003c            3f00            003f            3c3f            786d               4c6f            a794            bf3c
                                                   /*Unknown*/ {_Enc_Invalid   ,_Enc_Invalid ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid      ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid },
                                                   /*0000*/    {_Enc_Invalid   ,_Enc_Invalid ,_Enc_UCS4BEB   ,_Enc_UCS421B   ,_Enc_Invalid   ,_Enc_UCS421    ,_Enc_UCS4BE    ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid      ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid },
                                                   /*feff*/    {_Enc_UniCodeBE ,_Enc_UCS434  ,_Enc_UniCodeBE ,_Enc_UniCodeBE ,_Enc_UniCodeBE ,_Enc_UniCodeBE ,_Enc_UniCodeBE ,_Enc_UniCodeBE ,_Enc_UniCodeBE ,_Enc_UniCodeBE ,_Enc_UniCodeBE    ,_Enc_UniCodeBE ,_Enc_UniCodeBE ,_Enc_Invalid },
                                                   /*fffe*/    {_Enc_UniCode   ,_Enc_UCS4B   ,_Enc_UniCode   ,_Enc_UniCode   ,_Enc_UniCode   ,_Enc_UniCode   ,_Enc_UniCode   ,_Enc_UniCode   ,_Enc_UniCode   ,_Enc_UniCode   ,_Enc_UniCode      ,_Enc_UniCode   ,_Enc_UniCode   ,_Enc_Invalid },
                                                   /*efbb*/    {_Enc_Invalid   ,_Enc_Invalid ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid      ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_UTF8    },
                                                   /*3c00*/    {_Enc_Invalid   ,_Enc_UCS4    ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_UniCode   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid      ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid },
                                                   /*003c*/    {_Enc_Invalid   ,_Enc_UCS434  ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_UniCodeBE ,_Enc_Invalid   ,_Enc_Invalid      ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid },
                                                   /*3f00*/    {_Enc_Invalid   ,_Enc_Invalid ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid      ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid },
                                                   /*003f*/    {_Enc_Invalid   ,_Enc_Invalid ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid      ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid },
                                                   /*3c3f*/    {_Enc_Invalid   ,_Enc_Invalid ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_ASCII        ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid },
                                                   /*786d*/    {_Enc_Invalid   ,_Enc_Invalid ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid      ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid },
                                                   /*4c6f*/    {_Enc_Invalid   ,_Enc_Invalid ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid      ,_Enc_Invalid   ,_Enc_EBCDIC    ,_Enc_Invalid },
                                                   /*a794*/    {_Enc_Invalid   ,_Enc_Invalid ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid      ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid },
                                                   /*bf3c*/    {_Enc_Invalid   ,_Enc_Invalid ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid      ,_Enc_Invalid   ,_Enc_Invalid   ,_Enc_Invalid }
                                               };

        private Stream _stream = null;
        private string _encodingName = null;
        private Encoding _encoding = null;
        private int _skipBytes = 0;

		internal XmlPreparer( Stream stream, string encodingName )
		{
            _stream = stream;
            _encodingName = encodingName;
		}

        internal bool PrepareXML()
        {
            // Read first 256 bytes and try to detect endoding by it.
            byte[] streamStartBytes = new byte[256];
            int cBytes = _stream.Read( streamStartBytes, 0, 256 );
            _stream.Seek( 0, SeekOrigin.Begin );

            _encoding = GetEncoding( streamStartBytes, cBytes );
            if( _encoding == null )
            {
                if( _encodingName == null )
                {
                    return false;
                }
                try
                {
                    _encoding = Encoding.GetEncoding( _encodingName );
                }
                catch( NotSupportedException )
                {
                }
            }
            return _encoding != null;
        }

        internal string GetXML()
        {
            if( _encoding == null )
            {
                new InvalidOperationException( "GetXML() is called without sucessuful PrepareXML()" );
            }

            byte[] buffer = new byte[ 4096 ];
            char[] chars = new char[ buffer.Length ];
            StringBuilder sb = StringBuilderPool.Alloc();
            try
            {
                int read;
                Decoder dec = _encoding.GetDecoder();
                if( _skipBytes > 0 )
                {
                    while( ( _skipBytes -= _stream.Read( buffer, 0, _skipBytes ) ) > 0 );
                }
                while( ( read = _stream.Read( buffer, 0, buffer.Length ) ) > 0 )
                {
                    int cc;
                    try
                    {
                        cc = dec.GetChars( buffer, 0, read, chars, 0 );
                    }
                    catch( ArgumentException )
                    {
                        chars = new char[ dec.GetCharCount( buffer, 0, read ) ] ;
                        cc = dec.GetChars( buffer, 0, read, chars, 0 );
                    }
                    sb.Append( chars, 0, cc );
                }
                _stream.Seek( 0, SeekOrigin.Begin );
                // Cut out restricted chars
                for(int i = 0; i < sb.Length; ++i )
                {
                    int c = sb[i];
                    // XML Spec 1.1, paragraph 2.1 and 2.2
                    //  document	   ::=   	prolog element Misc* - Char* RestrictedChar Char*
                    // 	RestrictedChar	   ::=   	[#x1-#x8] | [#xB-#xC] | [#xE-#x1F] | [#x7F-#x84] | [#x86-#x9F]
                    if(
                        (c >= 0x0001 && c <= 0x0008) ||
                        (c >= 0x000B && c <= 0x000C) ||
                        (c >= 0x000E && c <= 0x001F) ||
                        (c >= 0x007F && c <= 0x0084) ||
                        (c >= 0x0086 && c <= 0x009F)
                        )
                    {
                        sb[i] = '?';
                    }
                }
                return sb.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( sb );
            }
        }

        private Encoding GetEncoding( byte[] bytes, int cbytes )
        {
            int enc = HaveBOM(bytes,cbytes);

            switch( enc )
            {
                case _Enc_ASCII:
                case _Enc_Invalid:
                    // Unknown, or ``<?xml''
                    break;
                case _Enc_UTF8:
                    // Real UTF-8 with BOM
                    _skipBytes = 3;
                    return new UTF8Encoding( true, false );
                case _Enc_UniCodeBE:
                    // UCS-2BE
                    return Encoding.BigEndianUnicode;
                case _Enc_UniCode:
                    // UCS-2LE
                    return Encoding.Unicode;
                case _Enc_UCS4BE:
                case _Enc_UCS4BEB:
                case _Enc_UCS4:
                case _Enc_UCS4B:
                case _Enc_UCS434:
                case _Enc_UCS434B:
                case _Enc_UCS421:
                case _Enc_UCS421B:
                case _Enc_EBCDIC:
                default:
                    // Can prepare XML in these encodings
                    return null;
            }

            // <?xml -- ?
            Encoding utf8 = new UTF8Encoding( false, false );
            string xml = utf8.GetString( bytes, 0, cbytes );
            if( ! xml.StartsWith( "<?xml" ) )
            {
                // Shit happens?
                return null;
            }
            // XML! Try to find `encoding'
            int i;
            i = xml.IndexOf( "?>" );
            if( i > 0 )
            {
                xml = xml.Substring( 0, i );
            }
            // Try to find `` encoding="''
            int encStart = xml.IndexOf( " encoding=" );
            if( encStart > 0 )
            {
                encStart += 10;
                char q = xml[ encStart ];
                encStart += 1;
                int encEnd = xml.IndexOf( q, encStart );
                if( encEnd < encStart )
                {
                    // No ``encoding=""'', it is UTF-8
                    return new UTF8Encoding( false, false );
                }
                encEnd -= 1;
                string encoding = xml.Substring( encStart, encEnd - encStart + 1 );
                if( encoding.ToUpper() == "UTF-8" )
                {
                    return new UTF8Encoding( false, false );
                }
                else
                {
                    try
                    {
                        return Encoding.GetEncoding( encoding );
                    }
                    catch( NotSupportedException )
                    {
                        // Unknown one
                        return null;
                    }
                    catch( ArgumentException )
                    {
                        // Unknown one
                        return null;
                    }
                }
            }
            // No "encoding" attribute in XML declaration, assume UTF-8
            return new UTF8Encoding( false, false );
        }

        private static int GetEncodingIndex(int word)
        {
            switch(word)
            {
                case 0x0000:    return 1;
                case 0xfeff:    return 2;
                case 0xfffe:    return 3;
                case 0xefbb:    return 4;
                case 0x3c00:    return 5;
                case 0x003c:    return 6;
                case 0x3f00:    return 7;
                case 0x003f:    return 8;
                case 0x3c3f:    return 9;
                case 0x786d:    return 10;
                case 0x4c6f:    return 11;
                case 0xa794:    return 12;
                case 0xbf3c:    return 13;
                default:        return 0; //unknown
            }
        }

        private static int HaveBOM( byte[] bytes, int cbytes )
        {
            if( cbytes < 2 )
            {
                return -1;
            }
            int index1 = GetEncodingIndex( bytes[0] << 8 | bytes[1] );
            int index2 = 0;

            if( cbytes >= 4 )
            {
                index2 = GetEncodingIndex( bytes[2] << 8 | bytes[3] );
            }
            return _EncodingTable[ index1, index2 ];
        }

        internal static string HtmlEntites()
        {
            if( _HtmlEntities != null )
            {
                return _HtmlEntities;
            }
            StringBuilder sb = StringBuilderPool.Alloc();
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load( JetBrains.Omea.HTML.HtmlEntityReader.GetHtmlEntitiesStream() );
                foreach( XmlElement e in xml.GetElementsByTagName( "Entity" ) )
                {
                    int val = Int32.Parse( e.GetAttribute( "Value" ) );
                    sb.AppendFormat( "<!ENTITY {0} \"{1}\">\n", e.GetAttribute( "Name" ), (char)val );
                }
                _HtmlEntities = sb.ToString();
            }
            catch
            {
                _HtmlEntities = "";
            }
            finally
            {
                StringBuilderPool.Dispose( sb );
            }
            return _HtmlEntities;
        }
    }
}
