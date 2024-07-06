// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Web;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.HTML;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.MailParser
{
    /**
     * Converts a plain-text body parsed by MailBodyParser to an HTML page.
     */

    internal class MailBodyFormatter
    {
        private const string _cDefaultStyleTmpl = "<style>\n" +
                                                  "    body, td { font-family: $FontFace$; font-size: $FontSize$pt; }\n" +
                                                  "    .oddquote { background-color: #f0f0f0; color: #800080; }\n" +
                                                  "    .evenquote { background-color: #e0e0e0; color: #800000; }\n" +
                                                  "    .outlookquote { color: #008080; }\n" +
                                                  "    .sig { font-size: 8pt; font-style: italic; }\n" + "</style>\n";
        private const string _cTableStartTag = "<table border=\"0\" cellpadding=\"5\" cellspacing=\"0\" width=\"95%\">";
        private const string _cTabSubst = "&nbsp;&nbsp;&nbsp;&nbsp;";

        private static readonly StringBuilder _builder = new StringBuilder();
        private static bool _quoteTableStarted, _quoteRowStarted;

    	public static string FormatBody( MailBodyParser parser, bool noWrap, MarkerInjector injector, string fontFace, int fontSize )
        {
            return FormatBody( parser, null, noWrap, injector, fontFace, fontSize );
        }

    	public static string FormatBody( MailBodyParser parser, string subject, bool noWrap, MarkerInjector injector, string fontFace, int fontSize )
        {
            // A link converter that is capable of sweeping out the markers when
            // creating the links out of plain text (by using the MarkedHtmlLinkConverter).
            // If there are no markers injected, HtmlLinkConverter is used.
			HtmlLinkConverter linkconverter = (injector != null) ? new MarkedHtmlLinkConverter( injector ) : new HtmlLinkConverter();
            string style = _cDefaultStyleTmpl.Replace( "$FontFace$", fontFace ).Replace( "$FontSize$", fontSize.ToString() );

            _builder.Length = 0;
            _quoteTableStarted = _quoteRowStarted = false;

            FormatBody( parser, subject, linkconverter, style, noWrap );
            return _builder.ToString();
        }

        private static void FormatBody( MailBodyParser parser, string subject, HtmlLinkConverter converter, string style, bool noWrap )
        {
            _builder.Append( "<html><head>").Append( style ).
                     Append( Core.MessageFormatter.DualMediaSubjectStyle ).Append( "</head><body>" );
            if( !String.IsNullOrEmpty( subject ) )
            {
                _builder.Append( MessageFormatter.FormattedHeader( subject ) );
            }

            int oldQuoteLevel = 0;
            string oldQuotePrefix = "";
            for( int i = 0; i < parser.ParagraphCount; i++ )
            {
                MailBodyParser.Paragraph para = parser.GetParagraph( i );
                if ( para.QuoteLevel > 0 )
                {
                    if (!_quoteTableStarted)
                    {
                        _builder.Append( _cTableStartTag );
                        _quoteTableStarted = true;
                        _quoteRowStarted = false;
                    }

                    if ( _quoteRowStarted )
                    {
                        if ( oldQuoteLevel != para.QuoteLevel || oldQuotePrefix != para.QuotePrefix )
                        {
                            _builder.Append( "</td></tr>" );
                            _quoteRowStarted = false;
                        }
                    }

                    if ( !_quoteRowStarted )
                    {
                        _builder.Append( "<tr class=\"");
                        _builder.Append( (para.QuoteLevel % 2 != 0) ? "oddquote" : "evenquote" );
                        _builder.Append( "\"><td>" );
                        _builder.Append( HTMLEncode( para.QuotePrefix ) );
                        for( int q = 0; q < para.QuoteLevel; q++ )
                        {
                            _builder.Append( "&gt;" );
                        }
                        _builder.Append( "</td><td>" );
                        _quoteRowStarted = true;
                    }
                    else
                        _builder.Append( "<br>" );

                    if ( para.Type == ParagraphType.Fixed )
                    {
                        _builder.Append( converter.ConvertLinks( ReplaceSpaces( HTMLEncode( para.Text ), noWrap ) ) );
                    }
                    else
                    {
                        // show an extra separator line above and below every quoted
                        // plain-text paragraph
                        if ( i > 0 )
                        {
                            MailBodyParser.Paragraph prevPara = parser.GetParagraph( i-1 );
                            if ( prevPara.Type != ParagraphType.Plain && prevPara.QuoteLevel == para.QuoteLevel
                                && prevPara.QuotePrefix == para.QuotePrefix )
                            {
                                _builder.Append( "<br>" );
                            }
                        }
                        _builder.Append( converter.ConvertLinks( HTMLEncode( para.Text ) ) );
                        _builder.Append( "<br>" );
                    }
                }
                else
                {
                    CloseOpenElements();
                    switch( para.Type )
                    {
                        case ParagraphType.Plain:
                            _builder.Append( "<p>" );
                            if ( para.OutlookQuote )
                            {
                                _builder.Append("<span class=\"outlookquote\">");
                                _builder.Append( converter.ConvertLinks( HTMLEncode( para.Text ) ) );
                                _builder.Append("</span>");
                            }
                            else
                            {
                                _builder.Append( converter.ConvertLinks( HTMLEncode( para.Text ) ) );
                            }
                            _builder.Append( "</p>\r\n" );
                            break;

                        case ParagraphType.Fixed:
                        case ParagraphType.Service:
                            if ( para.OutlookQuote )
                            {
                                _builder.Append("<span class=\"outlookquote\">");
                                _builder.Append( converter.ConvertLinks( ReplaceSpaces( HTMLEncode( para.Text ), noWrap ) ) );
                                _builder.Append("</span>");
                            }
                            else
                            {
                                _builder.Append( converter.ConvertLinks( ReplaceSpaces( HTMLEncode( para.Text ), noWrap ) ) );
                            }
                            _builder.Append( "<br>\r\n" );
                            break;

                        case ParagraphType.Sig:
                            _builder.Append( "<span class=\"sig\">" );
                            _builder.Append( converter.ConvertLinks( para.Text ) );
                            _builder.Append( "</span></br>" );
                            break;
                    }
                }
                oldQuoteLevel = para.QuoteLevel;
                oldQuotePrefix = para.QuotePrefix;
            }
            CloseOpenElements();
            _builder.Append( "</body></html> ");
        }

		/// <summary>
		/// If the quoting table or row was started, closes it.
		/// </summary>
        private static void CloseOpenElements()
        {
            if ( _quoteRowStarted )
            {
                _builder.Append( "</td></tr>" );
                _quoteRowStarted = false;
            }
            if ( _quoteTableStarted )
            {
                _builder.Append( "</table>" );
                _quoteTableStarted = false;
            }
        }

		/// <summary>
		/// Encodes the HTML entitites in the specified text.
		/// </summary>
        private static string HTMLEncode( string text )
        {
            text = text.Replace( "&", "&amp;" );
            text = text.Replace( "<", "&lt;" );
            text = text.Replace( ">", "&gt;" );
            return text;
        }

        /// <summary>
        /// Replaces the spaces and tabs in the line with &nbsp;.
        /// </summary>
		private static string ReplaceSpaces( string text, bool noWrap )
        {
            return noWrap ? text.Replace( " ", "&nbsp;" ).Replace( "\t", _cTabSubst ) : text.Replace( "\t", _cTabSubst );
        }
    }

	/// <summary>
	/// Provides HTML representation of a resource body in correspondence with its format and reply properties
	/// </summary>
    public class MessageFormatter: IMessageFormatter
    {
        private const int  _iDefaultMessageWidth = 50;
        private const string _cDefaultDualHeaderStyle =
            "<style type=\"text/css\" media=\"screen\"> .OmeaHeader { display: none; }</style>\n" +
            "<style type=\"text/css\" media=\"print\"> .OmeaHeader { margin: 0; padding: 0; font-size: 130%; color: #000; }</style>\n";

        private readonly MailQuoteProcessor _quoter = new MailQuoteProcessor();
        private readonly HashMap _previewTextProviders = new HashMap();  // Type -> IPreviewTextProvider

        public string DualMediaSubjectStyle
        {
            get {  return _cDefaultDualHeaderStyle;  }
        }

        #region GetFormattedBody
        #region Parameter Set Wrappers
        public string GetFormattedBody( IResource res, int bodyProp, int replyLink )
        {
            return GetFormattedBody( res, bodyProp, replyLink,
                                     Core.UIManager.DefaultFontFace, (int)Core.UIManager.DefaultFontSize );
		}

		public string GetFormattedBody( IResource res, string body, string replyToBody )
		{
            return GetFormattedBody( res, body, replyToBody, ref WordPtr.Empty,
                                     Core.UIManager.DefaultFontFace, (int)Core.UIManager.DefaultFontSize );
		}

		public string GetFormattedBody( IResource res, string body, string replyToBody, string fontFace, int fontSize )
		{
            return GetFormattedBody( res, body, replyToBody, ref WordPtr.Empty, fontFace, fontSize );
		}

		public string GetFormattedBody( IResource res, int bodyProp, int replyLink, ref WordPtr[] offsets )
		{
            return GetFormattedBody( res, bodyProp, replyLink, ref offsets,
                                     Core.UIManager.DefaultFontFace, (int) Core.UIManager.DefaultFontSize );
		}

        public string GetFormattedBody( IResource res, int bodyProp, int replyLink, string fontFace, int fontSize )
        {
            string body, reply;
            GetTexts( res, bodyProp, replyLink, out body, out reply );

            return GetFormattedBody( res, body, reply, ref WordPtr.Empty, fontFace, fontSize );
		}

		public string GetFormattedBody( IResource res, int bodyProp, int replyLink, ref WordPtr[] offsets,
                                        string fontFace, int fontSize )
		{
            string body, reply;
            GetTexts( res, bodyProp, replyLink, out body, out reply );

            return GetFormattedBody( res, body, reply, ref offsets, fontFace, fontSize );
		}

		public string GetFormattedBody( IResource res, string body, string replyToBody, ref WordPtr[] offsets )
		{
            return GetFormattedBody( res, body, replyToBody, ref offsets,
                                     Core.UIManager.DefaultFontFace, (int) Core.UIManager.DefaultFontSize );
		}
        #endregion Parameter Set Wrappers

		public string GetFormattedBody( IResource res, string body, string replyToBody,
                                        ref WordPtr[] offsets, string fontFace, int fontSize )
		{
            string formattedText;
            string subject = res.GetPropText( Core.Props.Subject );
            bool needFormatting = res.HasProp( "NoFormat" );

			// If no offsets passed, default to the simple processing
			if(( offsets == null ) || ( offsets.Length == 0 ))
            {
                formattedText = GetFormattedText( needFormatting, body, replyToBody, subject, null, fontFace, fontSize );
            }
            else
            {
                // Save offsets, format text, retrieve offsets.
			    using( MarkerInjector injector = new MarkerInjector() )
			    {
                    WordPtr[] modOffs = DocumentSection.RestrictResults( offsets, DocumentSection.BodySection );
				    body = injector.InjectMarkers( body, modOffs );
                    formattedText = GetFormattedText( needFormatting, body, replyToBody, subject, injector, fontFace, fontSize );
				    formattedText = injector.CollectMarkers( formattedText, out modOffs );
			    }
            }
            return formattedText;
		}

        private static string GetFormattedText( bool isNoFormat, string body, string replyToBody, string subject,
                                                MarkerInjector injector, string fontFace, int fontSize )
        {
            string text;
            int    minWrapWidth = Core.SettingStore.ReadInt( "Formatting", "MinimumWrapWidth", _iDefaultMessageWidth );
            if( isNoFormat )
		    {
			    text = "<pre>" + HttpUtility.HtmlEncode( body ) + "</pre>";
		    }
		    else
		    {
                MailBodyParser replyParser = new MailBodyParser( replyToBody, minWrapWidth );
			    MailBodyParser parser = new MailBodyParser( body, minWrapWidth, replyParser );
			    text = MailBodyFormatter.FormatBody( parser, subject, false, injector, fontFace, fontSize );
		    }
            return text;
        }

        private static void GetTexts( IResource res, int bodyProp, int replyLink, out string body, out string origBody )
        {
            body = res.GetPropText( bodyProp );
            IResource origRes = res.GetLinkProp( replyLink );
			origBody = (origRes != null) ? origRes.GetPropText( bodyProp ) : null;
        }

        public string GetFormattedHtmlBody( IResource res, string body, ref WordPtr[] offsets )
        {
            string formattedText;
            string subject = res.GetPropText( Core.Props.Subject );

			// If no offsets passed, default to the simple processing
			if(( offsets == null ) || ( offsets.Length == 0 ))
            {
                formattedText = InsertHeaderWithStyle( body, subject );
            }
            else
            {
                // Save offsets, format text, retrieve offsets.
			    using( MarkerInjector injector = new MarkerInjector() )
			    {
                    WordPtr[] modOffs = DocumentSection.RestrictResults( offsets, DocumentSection.BodySection );
				    body = injector.InjectMarkers( body, modOffs );
                    formattedText = InsertHeaderWithStyle( body, subject );
				    formattedText = injector.CollectMarkers( formattedText, out modOffs );
			    }
            }
            return formattedText;
        }

        private static string InsertHeaderWithStyle( string text, string subject )
        {
            string formattedText = text;
            if( !string.IsNullOrEmpty( subject ) )
            {
                string formattedTitle = FormattedHeaderWithStyle( subject );
                int index = text.IndexOf( "<body>", StringComparison.InvariantCultureIgnoreCase );
                if( index != -1 )
                {
                    formattedText = text.Substring( 0, index + 6 ) + formattedTitle + text.Substring( index + 6 );
                }
                else
                {
                    formattedText = formattedTitle + text;
                }
            }
            return formattedText;
        }

        public string StandardStyledHeader( string subject )
        {
            return FormattedHeader( subject );
        }

        internal static string FormattedHeader( string subject )
        {
            return "<h1 class=\"OmeaHeader\">" + HttpUtility.HtmlEncode(subject) + "</h1><hr class=\"OmeaHeader\">\n";
        }

        internal static string FormattedHeaderWithStyle( string subject )
        {
            return _cDefaultDualHeaderStyle + FormattedHeader( subject );
        }
        #endregion GetFormattedBody

        #region QuoteMessage
		public string QuoteMessage( IResource resource, int bodyProp )
        {
            return QuoteMessage( resource, resource.GetPropText( bodyProp ) );
        }

	    public string QuoteMessage( IResource resource, string body )
        {
            MailBodyParser parser = new MailBodyParser( body, 50 );
            return _quoter.Quote( parser, resource, QuoteSettings.Default );
        }

        public string QuoteMessage( IResource resource, int bodyProp, QuoteSettings settings )
        {
            return QuoteMessage( resource, resource.GetPropText( bodyProp ), settings );
        }

        public string QuoteMessage( IResource resource, string body, QuoteSettings settings )
        {
            MailBodyParser parser = new MailBodyParser( body, 50 );
            return _quoter.Quote( parser, resource, settings );
        }

	    public void RegisterPreviewTextProvider( string resourceType, IPreviewTextProvider provider )
	    {
	        _previewTextProviders[ resourceType ] = provider;
	    }

	    public string GetPreviewText( IResource res, int lines )
	    {
            IPreviewTextProvider provider = (IPreviewTextProvider) _previewTextProviders [res.Type];
            if ( provider != null )
            {
                string previewText = provider.GetPreviewText( res, lines );
                if ( previewText == null )
                {
                    return "";
                }
                return previewText;
            }
            if ( res.HasProp( Core.Props.LongBody ) )
            {
                string longBody = res.GetPropText( Core.Props.LongBody ).Trim();
                if ( res.HasProp( Core.Props.LongBodyIsHTML ) )
                {
                    if ( longBody.Length > 1024 )
                    {
                        longBody = longBody.Substring( 0, 1024 ) + "...";  // we won't fit more text in 2 lines, anyway
                    }
                    longBody = HtmlTools.StripHTML( longBody );
                    longBody = HtmlTools.SafeHtmlDecode( longBody ).Trim();
                }
                longBody = longBody.Replace( '\t', ' ' );

                return CleanPreviewText( longBody, lines );
            }
            return "";
        }

        private string CleanPreviewText( string body, int lines )
        {
            body = body.Replace( "\r\n", "\n" );
            return CleanPreviewText( body, lines, true );
        }

        private string CleanPreviewText( string body, int lines, bool skipQuoting )
        {
            StringBuilder resultBuilder = StringBuilderPool.Alloc();
            try
            {
                int pos = 0;
                int foundLines = 0;
                int quotedLines = 0;
                while( foundLines < lines )
                {
                    int nextPos = body.IndexOf( '\n', pos );
                    if ( nextPos == -1 )
                    {
                        if ( foundLines < lines-1 && quotedLines > 0 )
                        {
                            return CleanPreviewText( body, lines, false );
                        }
                        resultBuilder.Append( body.Substring( pos ) );
                        break;
                    }

                    string nextLine = body.Substring( pos, nextPos - pos );
                    pos = nextPos+1;
                    while( pos < body.Length && body [pos] == '\n' )
                    {
                        pos++;
                    }

                    if ( skipQuoting && MailBodyParser.GetQuoteLevel( nextLine ) > 0 )
                    {
                        quotedLines++;
                        continue;
                    }

                    if ( foundLines > 0 )
                    {
                        resultBuilder.Append( "\r\n" );
                    }
                    foundLines++;

                    resultBuilder.Append( nextLine );
                    if ( pos == body.Length )
                    {
                        break;
                    }
                }
                if ( foundLines == 0 && quotedLines > 0 )
                {
                    return CleanPreviewText( body, lines, false );
                }

                return resultBuilder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( resultBuilder );
            }
        }
        #endregion QuoteMessage
    }

	/// <summary>
	/// Injects special marker into the text at specific points determined by the offsets,
	/// so that they could be collected back after formatting the text and the offsets could
	/// be updated to match the corresponding points in the reformatted text.
	/// </summary>
	class MarkerInjector : IDisposable
	{
		/// <summary>
		/// List of the markers (of type <see cref="Marker"/>) injected into the content.
		/// </summary>
		/// <remarks>Valid after the call to <see cref="InjectMarkers"/> and up to end, so that multiple formatted samples could be matched against one source fragment.</remarks>
		protected ArrayList	_markers = null;

		/// <summary>
		/// The minimum (non-inclusive) marker injected into the strings.
		/// </summary>
		protected char	m_cMinMarker = (char)0xFFFF;

		/// <summary>
		/// The maximum (inclusive) marker injected into the strings.
		/// </summary>
		protected char	m_cMaxMarker = (char)0xFFFF;

		/// <summary>
		/// Receives a clean content and injects the markers.
		/// </summary>
		/// <param name="content">Initial unformatted content, for which the offsets are valid.</param>
		/// <param name="offsets">Offsets in the content. From the structure, only the <see cref="WordPtr.StartOffset"/> is used, others may be uninitialzied.</param>
		/// <returns>Content with the markers injected.</returns>
		/// <remarks>After injecting the markers, the offsets in <paramref name="offsets"/> become invalid as the text size changes. This is by design.</remarks>
		public string InjectMarkers(string content, WordPtr[] offsets)
        {
            #region Preconditions
            if (_markers != null)
				throw new InvalidOperationException("The object cannot be reused, you should create a new one.");
            #endregion Preconditions

			if( offsets == null )  // No offsets specified
			{
				_markers = new ArrayList(0);	// Create an empty list of markers
				return content;
			}

			StringBuilder	marked = new StringBuilder(content.Length + offsets.Length );	// The content plus one marker per each offset

			// Process markers
			int	pos = 0;
			int	nCurMarker = (int)m_cMaxMarker;
			_markers = new ArrayList();
			foreach( WordPtr offset in offsets )
			{
				// Some proof checks
				if(offset.StartOffset < pos)	// Offsets are not sorted. That's not a problem to sort them, but it's an indication of internal errors
					throw new ArgumentException("The offsets are not arranged properly.");
				if((offset.Section != DocumentSection.BodySection) && (offset.Section != "") && (offset.Section != null))	// Trying to consume wrong section
					throw new ArgumentException("Cannot process offset information for sections other than Body.");
				if(offset.StartOffset >= content.Length)	// Offsets out of range. Just ignore because it's not a fatal error, maybe the content has changed but was not reindexed yet
				{
					Trace.WriteLine( "Warning: MarkerInjector was supplied with offsets falling ouside the provided content. Such offsets were ignored." );
					break;
				}

				// Break apart
				marked.Append( content.Substring( pos, offset.StartOffset - pos ) );	// Content between markers or beginning of string

				for(; (content.IndexOf( (char)nCurMarker ) != -1) && (nCurMarker > 0); nCurMarker--)	// Ensure there's no such char
					;

				// Inject & store
				marked.Append( (char)nCurMarker );
				_markers.Add( new Marker((char)nCurMarker, offset) );

				// Advance
				pos = offset.StartOffset;
				nCurMarker--;
			}

			// Store the minimum injected marker value (it's non-inclusive)
			m_cMinMarker = (char)nCurMarker;

			// Copy content after the last marker
			marked.Append( content.Substring( pos, content.Length - pos ) );

			return marked.ToString(  );
		}

		/// <summary>
		/// Receives the formatted content with markers injected, collects the markers, updates the offsets and returns the content with markers cleaned up.
		/// </summary>
		/// <param name="content">Content with the markers.</param>
		/// <param name="offsets">Returns the updated offsets from the <see cref="InjectMarkers"/> method. Note that the number of offsets may decrease if some of the markers go out while formatting.</param>
		/// <returns>The cleaned up content without the markers.</returns>
		public string CollectMarkers(string content, out WordPtr[] offsets)
		{
			if(_markers == null)
				throw new InvalidOperationException("The object must be first initialized by the InjectMarkers call.");
			if(_markers.Count == 0)	// No markers injected in the text
			{
				offsets = null;
				return content;
			}

			// Locate markers
			int	pos;
			ArrayList	markers = new ArrayList(_markers.Count);	// Collect here (some may need to be removed)
			foreach(Marker marker in _markers)
			{
				// Locate the marker
				pos = content.IndexOf( marker._markingChar );
				marker._offsetData.StartOffset = pos;
				if(pos != -1)	// If the marker was found in the stream, keep it. Otherwise, drop by not adding to the new storage
					markers.Add( marker );
				else
					Trace.WriteLine("[MFO] Warning! A marker was filtered out due to absense in the formatted stream.");	// Pos == -1

				// TODO: what to do if marker is duplicated?
				// 1) use the first
				// 2) assume it's a bug and dispose of both offsets, leaving the marker chars intact
				// 3) add offsets for both
			}
			_markers = markers;	// Keep the filtered set of markers
			if(_markers.Count == 0)	// No markers left in the text
			{
				Trace.WriteLine("[MFO] Error! No markers were found in the formatted stream.");
				offsets = null;
				return content;
			}

			// Arrange in order of appearance
			try
			{
				_markers.Sort();
			}
			catch(InvalidOperationException)
			{	// This means that some markers had indeed equal positions, which must not happen
				Trace.WriteLine("[MFO] Error! Some of the markers had equal positions in the formatted stream. Cancelling further processing.");
				offsets = null;
				return content;
			}

			// Cut off and refresh offsets, collect the new offsets
			pos = 0;	// Current position
			int	shift = 0;	// Number of cut-out chars to shift the subsequent offsets by
			StringBuilder stripped = new StringBuilder(content.Length);
			offsets = new WordPtr[_markers.Count];
			foreach(Marker marker in _markers)
			{
				// Maintenance
				stripped.Append( content.Substring( pos, marker._offsetData.StartOffset - pos ) );
				pos = marker._offsetData.StartOffset + 1;	// Beyond the marker
				marker._offsetData.StartOffset -= shift;	// Encounter the cut-off markers

				// Collect
				offsets[shift] = marker._offsetData;

				// Advance
				shift++;
			}

			// Copy content after the last marker
			stripped.Append( content.Substring( pos, content.Length - pos ) );

			return stripped.ToString(  );
		}

		/// <summary>
		/// Removes all the injected markers from a given string and returns the sweeped string.
		/// Note that this does not affect the infrastructure of this class, unlike <see cref="CollectMarkers"/>.
		/// Also, this does not make any offset validation, just removes the markers.
		/// </summary>
		/// <param name="marked"></param>
		/// <returns></returns>
		public string SweepMarkers(string marked)
		{
			StringBuilder	sb = new StringBuilder(marked.Length);

			foreach(char ch in marked)
			{
				if(!((ch > m_cMinMarker) && (ch <= m_cMaxMarker)))
					sb.Append( ch );
			}

			return sb.ToString();
		}

		/// <summary>
		/// Stores information about one marker injected into the content.
		/// </summary>
		internal class Marker : IComparable
		{
			/// <summary>
			/// Init instance.
			/// </summary>
			internal Marker(char marker, WordPtr data)
			{
				_markingChar = marker;
				_offsetData = data;
			}

			/// <summary>
			/// The char used to mark the text.
			/// </summary>
			public char	_markingChar;

			/// <summary>
			/// Data associated with the offset, to be returned along with the new offset.
			/// </summary>
			public WordPtr _offsetData;

			/// <summary>
			/// Compare by offsets
			/// </summary>
			public int CompareTo( object obj )
			{
				if(_offsetData.StartOffset == ((Marker)obj)._offsetData.StartOffset)	// Marker offsets in the stream must not be equal
					throw new InvalidOperationException();
				return _offsetData.StartOffset.CompareTo( ((Marker)obj)._offsetData.StartOffset );
			}
		}

		public void Dispose()
		{
			_markers = null;
		}
	}

	/// <summary>
	/// Introduces a HTML link derived from a text representation, correctly handling the possibly-present markers.
	/// </summary>
	internal class MarkedHtmlLinkConverter : HtmlLinkConverter
	{
		/// <summary>
		/// An author of the injected markers in the processed content.
		/// </summary>
		protected MarkerInjector	_injector;

		public MarkedHtmlLinkConverter(MarkerInjector injector)
		{
			_injector = injector;
		}

		protected override string BuildHref( string href, string hrefText )
		{
			return base.BuildHref( _injector.SweepMarkers(href), hrefText );	// Use the base implementation, but remove the markers from the URL
		}
	}
}
