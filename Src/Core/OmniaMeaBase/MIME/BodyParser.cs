// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Text;
using System.IO;
using System.Collections;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.MIME
{
    public enum MessagePartTypes
    {
        Body,
        HtmlBody,
        Attachment,
        Inline,
        Embedded
    };

    public enum MessagePartEncoding
    {
        None,
        QuotedPrintable,
        Base64
    };

    public class MessagePart
    {
        /**
         * ctor for body part
         */
        internal MessagePart( string body, MessagePartTypes type )
        {
            _type = type;
            _body = body;
        }

        /**
         * ctor for attachment parts
         */
        internal MessagePart( byte[] bytes, string name )
        {
            _type = MessagePartTypes.Attachment;
            _name = name;
            _content = new JetMemoryStream( bytes, true );
        }

        internal MessagePart( byte[] bytes, int count, string name )
        {
            _type = MessagePartTypes.Attachment;
            _name = name;
            _content = new JetMemoryStream( bytes, 0, count );
        }

        public MessagePartTypes PartType
        {
            get { return _type; }
            set { _type = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public Stream Content
        {
            get { return _content; }
        }

        public string Body
        {
            get { return _body; }
        }

        public string ContentId
        {
            get { return _contentId; }
            set { _contentId = value; }
        }

        private MessagePartTypes    _type;
        private string              _name;
        private Stream              _content;
        private string              _body;
        private string              _contentId;
    }

    /**
     * MultiPartBody class parses multi-part mime message or plaintext
     * message with uuencoded insertions
     * GetParts() returns list of parts: body and attachments if any
     */
    public class MultiPartBodyParser
    {
        private static readonly char[] _boundaryDelimiters = new char[] {' ', ';' };

        public MultiPartBodyParser()
        {
            _bodyBuilder = new StringBuilder();
            _htmlBodyBuilder = new StringBuilder();
            _partBuilder = new StringBuilder();
            _parts = new ArrayList( 1 );
        }

        public MultiPartBodyParser( string body, string content_type, string content_transfer_encoding, string defaultCharset )
            : this()
        {
            ProcessBody( body, content_type, content_transfer_encoding, defaultCharset );
        }

        public void ProcessBody( string body, string content_type, string content_transfer_encoding, string defaultCharset )
        {
            Clear();

            _body = body;
            _content_type = content_type;
            _content_transfer_encoding = content_transfer_encoding;

            string lowerContentType = content_type.ToLower();
            _charset = DetectCharset( lowerContentType, content_type, defaultCharset );

            if( lowerContentType.StartsWith( "multipart" ) )
            {
                ProcessMutipartBody();
            }
            else
            {
                ProcessSingleBody();
            }

            /**
             * the last part is always body because it could be combined
             * from several multipart entities, as well as it could be separated
             * with several uuencoded insertions
             */
            if( _bodyBuilder.Length > 0 )
            {
                _parts.Add( new MessagePart( _bodyBuilder.ToString(), MessagePartTypes.Body ) );
            }
            if( _htmlBodyBuilder.Length > 0 )
            {
                string htmlBody = _htmlBodyBuilder.ToString();
                if( htmlBody.IndexOf( '\0' ) > 0 )
                {
                    htmlBody = htmlBody.Substring( 2 ).Replace( "\0", string.Empty );
                }
                _parts.Add( new MessagePart( htmlBody, MessagePartTypes.HtmlBody ) );
            }
        }

        public void Clear()
        {
            _bodyBuilder.Length = 0;
            if( _bodyBuilder.Capacity > 16384 )
            {
                _bodyBuilder.Capacity = 1024;
            }
            _htmlBodyBuilder.Length = 0;
            if( _htmlBodyBuilder.Capacity > 16384 )
            {
                _htmlBodyBuilder.Capacity = 1024;
            }
            _partBuilder.Length = 0;
            if( _partBuilder.Capacity > 16384 )
            {
                _partBuilder.Capacity = 1024;
            }
            _parts.Clear();
        }

        public MessagePart[] GetParts()
        {
            MessagePart[] parts = new MessagePart[ _parts.Count ];
            for( int i = 0; i < _parts.Count; ++i )
            {
                parts[ i ] = (MessagePart) _parts[ i ];
            }
            return parts;
        }

        public string Charset
        {
            get { return _charset; }
        }

        private void ProcessSingleBody()
        {
            string[] lines = _body.Split( '\n' );

            _partBuilder.Length = 0;
            if( _content_type.ToLower().IndexOf( "/html" ) < 0 )
            {
                ProcessSinglePlainBody( lines );
            }
            else
            {
                ProcessSingleHtmlBody( lines );
            }

            if( _bodyBuilder.Length > 0 || _htmlBodyBuilder.Length > 0 )
            {
                string cte = _content_transfer_encoding;
                if( Utils.StartsWith( cte, "quoted-printable",true ) )
                {
                    string body = _bodyBuilder.ToString();
                    _bodyBuilder.Length = 0;
                    _bodyBuilder.Append( MIMEParser.DecodeQuotedPrintable( _charset, body ) );
                    body = _htmlBodyBuilder.ToString();
                    _htmlBodyBuilder.Length = 0;
                    _htmlBodyBuilder.Append( MIMEParser.DecodeQuotedPrintable( _charset, body ) );
                }
                else if( Utils.StartsWith( cte, "base64", true ) )
                {
                    string body = _bodyBuilder.ToString();
                    _bodyBuilder.Length = 0;
                    _bodyBuilder.Append( MIMEParser.DecodeBase64( _charset, body ) );
                    body = _htmlBodyBuilder.ToString();
                    _htmlBodyBuilder.Length = 0;
                    _htmlBodyBuilder.Append( MIMEParser.DecodeBase64( _charset, body ) );
                }
                else
                {
                    string body = _bodyBuilder.ToString();
                    _bodyBuilder.Length = 0;
                    _bodyBuilder.Append( MIMEParser.TranslateRawStringInCharset( _charset, body ) );
                    body = _htmlBodyBuilder.ToString();
                    _htmlBodyBuilder.Length = 0;
                    _htmlBodyBuilder.Append( MIMEParser.TranslateRawStringInCharset( _charset, body ) );
                    for( int i = 0; i < _parts.Count; ++i )
                    {
                        MessagePart attachment =  (MessagePart) _parts[i];
                        attachment.Name = MIMEParser.TranslateRawStringInCharset( _charset, attachment.Name );
                    }
                }
            }
        }

        private void ProcessSinglePlainBody( string[] lines )
        {
            string filename = string.Empty;
            bool inBody = true;
            int partLen = 0;
            string line;

            for( int i = 0; i < lines.Length; ++i )
            {
                line = lines[ i ].TrimEnd( '\r' );
                if( inBody && line.StartsWith( "begin" ) )
                {
                    string[] attachHeaders = line.TrimEnd( ' ' ).Split( ' ' );
                    if( attachHeaders.Length > 2 )
                    {
                        string fattr = attachHeaders[ 1 ];
                        bool fattrOk = fattr.Length >= 3;
                        if( fattrOk )
                        {
                            for( int j = 0; j < fattr.Length; ++j )
                            {
                                if( !( fattrOk = Char.IsDigit( fattr[ j ] ) ) )
                                {
                                    break;
                                }
                            }
                        }
                        if( fattrOk )
                        {
                            filename = attachHeaders[ 2 ];
                            for( int j = 3; j < attachHeaders.Length; ++j )
                            {
                                filename += attachHeaders[ j ];
                            }
                            inBody = false;
                            partLen = 0;
                            continue;
                        }
                    }
                }
                else if( !inBody && line.StartsWith( "end" ) )
                {
                    if( _partBuilder.Length > 0 )
                    {
                        if( partLen > 0 )
                        {
                            byte[] bytes = UUParser.Decode( _partBuilder.ToString() );
                            if( partLen > bytes.Length )
                            {
                                partLen = bytes.Length;
                            }
                            MessagePart attachment = new MessagePart( bytes, partLen, filename );
                            attachment.PartType = MessagePartTypes.Inline;
                            _parts.Add( attachment );
                        }
                        _partBuilder.Length = 0;
                    }
                    inBody = true;
                    continue;
                }
                if( inBody )
                {
                    _bodyBuilder.Append( line );
                    _bodyBuilder.Append( "\r\n" );
                }
                else
                {
                    if( line.Length > 1 && line[ 0 ] != '`' )
                    {
                        partLen += UUParser._UUAlphabet.IndexOf( line[ 0 ] );
                        _partBuilder.Append( line, 1, line.Length - 1 );
                    }
                }
            }
        }

        private void ProcessSingleHtmlBody( string[] lines )
        {
            for( int i = 0; i < lines.Length; ++i )
            {
                _htmlBodyBuilder.Append( lines[ i ] );
            }
        }

        private void ProcessMutipartBody()
        {
            /**
             * extract boundary from content-type
             */
            string boundary = string.Empty;
            int boundaryBegin = _content_type.ToLower().IndexOf( "boundary=" );
            if( boundaryBegin > 0 )
            {
                boundaryBegin += 9;
                int boundaryEnd = _content_type.IndexOfAny( _boundaryDelimiters, boundaryBegin );
                if( boundaryEnd < 0 )
                {
                    boundaryEnd = _content_type.Length;
                }
                if( boundaryEnd > boundaryBegin )
                {
                    boundary = _content_type.Substring(
                        boundaryBegin, boundaryEnd - boundaryBegin ).Replace( "\"", string.Empty ).Trim();
                }
            }
            if( boundary.Length == 0 )
            {
                _bodyBuilder.Append( "Bad multipart message: the \"Content-Type\" header doesn't contain valid boundary. Message source follows as plain text.\r\n\r\n" );
                _bodyBuilder.Append( _body );
                return;
            }
            boundary = "--" + boundary;
            string finishingBoundary = boundary + "--";

            string[] lines = _body.Split( '\n' );
            string line = string.Empty;
            int i = 0;
            bool inHeaders = true;
            bool isHtmlBody = false;
            bool isAttachment = false;
            bool isInline = false;
            bool isAlternative = false;
            MessagePartEncoding MIME_encoding = MessagePartEncoding.None;
            string bodyCharset = string.Empty;
            string filename = string.Empty;
            string altBoundary = string.Empty;
            string contentId = string.Empty;

            _partBuilder.Length = 0;

            /**
             * search for the first boundary
             */
            for( ; i < lines.Length; ++i )
            {
                line = lines[ i ].TrimEnd( '\r' );
                if( line.StartsWith( boundary ) )
                {
                    ++i;
                    break;
                }
            }

            for( ; i < lines.Length && !line.StartsWith( finishingBoundary ); ++i )
            {
                line = lines[ i ].TrimEnd( '\r' );
                /**
                 * if not in headers then process next part
                 */
                if( !inHeaders )
                {
                    /**
                     * if filename not present process body
                     */
                    if( filename.Length == 0 )
                    {
                        if( isHtmlBody )
                        {
                            i = ProcessHTMLBody( lines, i, boundary, MIME_encoding, bodyCharset );
                        }
                        else
                        {
                            i = ProcessBody( lines, i, boundary, MIME_encoding, bodyCharset );
                            /**
                             * if body is nested multipart/alternative then parse it in its turn
                             */
                            if( isAlternative && altBoundary.Length > 0 )
                            {
                                MultiPartBodyParser parser = new MultiPartBodyParser(
                                    _bodyBuilder.ToString(), "multipart/alternative; boundary=" +
                                    altBoundary, _content_transfer_encoding, _charset );
                                _bodyBuilder.Length = 0;
                                _htmlBodyBuilder.Length = 0;
                                _parts.AddRange( parser.GetParts() );
                            }
                        }
                    }
                    else
                    /**
                     * if filename present then we are to extract attacment
                     */
                    {
                        i = ProcessAttachment( lines, i, boundary, MIME_encoding );
                        if( MIMEParser.IsMIMEString( filename ) )
                        {
                            filename = MIMEParser.DecodeMIMEString( filename );
                        }
                        string partStr = _partBuilder.ToString();
                        MessagePart attachment = null;
                        switch( MIME_encoding )
                        {
                            case MessagePartEncoding.QuotedPrintable:
                            {
                                attachment = new MessagePart( MIMEParser.DecodeQuotedPrintable( partStr ), filename );
                                break;
                            }
                            case MessagePartEncoding.Base64:
                            {
                                byte[] bytes = MIMEParser.DecodeBase64( partStr );
                                if( bytes != null )
                                {
                                    attachment = new MessagePart( bytes, filename );
                                }
                                break;
                            }
                            default:
                            {
                                attachment = new MessagePart(
                                    MIMEParser.GetEncodingExceptionSafe( bodyCharset ).GetBytes( partStr ), filename );
                                break;
                            }
                        }
                        if( attachment != null )
                        {
                            if( isInline )
                            {
                                attachment.PartType = MessagePartTypes.Inline;
                            }
                            else
                            {
                                if( contentId.Length > 0 )
                                {
                                    attachment.PartType = MessagePartTypes.Embedded;
                                    attachment.ContentId = contentId;
                                }
                            }
                            _parts.Add( attachment );
                        }
                        _partBuilder.Length = 0;
                    }
                    /**
                     * reset encoding, charset, filename, alternative boundary, content-id and flags
                     */
                    MIME_encoding = MessagePartEncoding.None;
                    bodyCharset = _charset;
                    filename = altBoundary = contentId = string.Empty;
                    inHeaders = true;
                    isHtmlBody = isAttachment = isInline = false;
                }
                else
                {
                    if( line.Length == 0 )
                    {
                        inHeaders = false;
                    }
                    else
                    {
                        string lowerLine = line.ToLower();
                        if( lowerLine.StartsWith( "content-type:" ) )
                        {
                            string content_type = lowerLine.Substring( 14 );
                            if( content_type.StartsWith( "multipart/alternative" ) ||
                                content_type.StartsWith( "multipart/related" ) )
                            {
                                isAlternative = true;
                            }
                            else if( content_type.StartsWith( "application/" ) || content_type.StartsWith( "image/" ) )
                            {
                                isAttachment = true;
                            }
                            else
                            {
                                if( content_type.StartsWith( "text/html" ) )
                                {
                                    isHtmlBody = true;
                                }
                                bodyCharset = DetectCharset( lowerLine, line, _charset );
                            }
                        }
                        else if( lowerLine.StartsWith( "content-transfer-encoding:" ) )
                        {
                            string encoding = lowerLine.Substring( 27 );
                            if( encoding.StartsWith( "quoted-printable" ) )
                            {
                                MIME_encoding = MessagePartEncoding.QuotedPrintable;
                            }
                            else if( encoding.StartsWith( "base64" ) )
                            {
                                MIME_encoding = MessagePartEncoding.Base64;
                            }
                        }
                        else if( lowerLine.StartsWith( "content-disposition:" ) )
                        {
                            string headerValue = lowerLine.Substring( 21 );
                            if( headerValue.StartsWith( "attachment" ) )
                            {
                                isAttachment = true;
                            }
                            else if( headerValue.StartsWith( "inline" ) )
                            {
                                isInline = true;
                            }
                        }
                        else if( lowerLine.StartsWith( "content-id:" ) )
                        {
                            contentId = line.Substring( 12 ).Replace( "<", "" ).Replace( ">", "" );
                        }
                        if( ( isAttachment || isInline ) && filename.Length == 0 )
                        {
                            int nameBegin;
                            if( ( nameBegin = lowerLine.IndexOf( "filename=" ) ) > 0 )
                            {
                                filename = line.Substring( nameBegin + 9 ).Replace( "\"", string.Empty ).Trim();
                            }
                            if( ( nameBegin = lowerLine.IndexOf( "name=" ) ) > 0 )
                            {
                                filename = line.Substring( nameBegin + 5 ).Replace( "\"", string.Empty ).Trim();
                            }
                        }
                        if( isAlternative && altBoundary.Length == 0 )
                        {
                            if( ( boundaryBegin = lowerLine.IndexOf( "boundary=" ) ) > 0 )
                            {
                                altBoundary = line.Substring( boundaryBegin + 9 ).Replace( "\"", string.Empty ).Trim();
                            }
                        }
                    }
                }
            }
        }

        /**
         * returns the index of last processed line in the array
         */
        private int ProcessBody( string[] lines, int fisrtLine, string boundary,
                                 MessagePartEncoding MIME_encoding, string bodyCharset )
        {
            int i = fisrtLine;

            if( MIME_encoding == MessagePartEncoding.Base64 )
            {
                StringBuilder builder = StringBuilderPool.Alloc();
                try
                {
                    for( ; i < lines.Length; ++i )
                    {
                        string line = lines[ i ].TrimEnd( '\r' );
                        if( line.StartsWith( boundary ) )
                        {
                            break;
                        }
                        builder.Append( line );
                    }
                    _bodyBuilder.Append( DecodeLine( builder.ToString(), MIME_encoding, bodyCharset ) );
                }
                finally
                {
                    StringBuilderPool.Dispose( builder ) ;
                }
            }
            else
            {
                for( ; i < lines.Length; ++i )
                {
                    string line = lines[ i ].TrimEnd( '\r' );
                    if( line.StartsWith( boundary ) )
                    {
                        break;
                    }
                    _bodyBuilder.Append( DecodeLine( line, MIME_encoding, bodyCharset ) );
                    if( MIME_encoding != MessagePartEncoding.QuotedPrintable || !line.EndsWith( "=" ) )
                    {
                        _bodyBuilder.Append( "\r\n" );
                    }
                }
            }
            return i;
        }

        /**
         * returns the index of last processed line in the array
         */
        private int ProcessHTMLBody( string[] lines, int fisrtLine, string boundary,
                                     MessagePartEncoding MIME_encoding, string bodyCharset )
        {
            int i = fisrtLine;

            for( ; i < lines.Length; ++i )
            {
                string line = lines[ i ].TrimEnd( '\r' );
                if( line.StartsWith( boundary ) )
                {
                    break;
                }
                line = DecodeLine( line, MIME_encoding, bodyCharset );
                _htmlBodyBuilder.Append( line );
            }
            return i;
        }

        /**
         * returns the index of last processed line in the array
         */
        private int ProcessAttachment( string[] lines, int fisrtLine, string boundary,
                                       MessagePartEncoding MIME_encoding )
        {
            int i = fisrtLine;

            for( ; i < lines.Length; ++i )
            {
                string line = lines[ i ].TrimEnd( '\r' );
                if( line.StartsWith( boundary ) )
                {
                    break;
                }
                _partBuilder.Append( line );
                if( MIME_encoding == MessagePartEncoding.None )
                {
                    _partBuilder.Append( "\r\n" );
                }
            }

            return i;
        }

        private static string DecodeLine( string line, MessagePartEncoding MIME_encoding, string bodyCharset )
        {
            if( MIME_encoding == MessagePartEncoding.QuotedPrintable )
            {
                line = MIMEParser.DecodeQuotedPrintable( bodyCharset, line );
            }
            else if( MIME_encoding == MessagePartEncoding.Base64 )
            {
                line = MIMEParser.DecodeBase64( bodyCharset, line );
            }
            else
            {
                line = MIMEParser.TranslateRawStringInCharset( bodyCharset, line );
            }
            return line;
        }

        private string DetectCharset( string lowerLine, string line, string defaultCharset )
        {
            string bodyCharset = defaultCharset;

            int charsetBegin;
            if( ( charsetBegin = lowerLine.IndexOf( "charset=" ) ) > 0 )
            {
                bodyCharset = line.Substring(
                    charsetBegin + 8 ).Replace( "\"", string.Empty ).Trim().ToLower();
                int charsetEnd = bodyCharset.IndexOf( ';' );
                if( charsetEnd > 0 )
                {
                    bodyCharset = bodyCharset.Substring( 0, charsetEnd );
                }
                bodyCharset = bodyCharset.Replace( '_', '-' );
            }
            return bodyCharset;
        }

        private string          _body;
        private string          _content_type;
        private string          _content_transfer_encoding;
        private string          _charset;
        private StringBuilder   _bodyBuilder;
        private StringBuilder   _htmlBodyBuilder;
        private StringBuilder   _partBuilder;
        private ArrayList       _parts;
    }
}
