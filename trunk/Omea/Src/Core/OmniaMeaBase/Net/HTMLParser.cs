/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.TextIndex;

namespace JetBrains.Omea.HTML
{
	/// <summary>
	/// For given stream, HTML parser returns sequence of text fragments.
	/// </summary>
	/// <remarks>
	/// <para>A fragment is returned by <see cref="ReadNextFragment"/> only if it is situated
	/// in body or in title, and not in script or any other place outside the above mentioned ones.</para>
	/// 
	/// <para>The <see cref="InTitle"/> property allows to check which fragment is currently being processed.</para>
	/// 
	/// <para>Each read fragment can be a simple fragment and a heading,
	/// it can be verified by InHeading property.</para>
	/// </remarks>
	public class HTMLParser : IDisposable
	{
		/// <summary>
		/// Determines the behavior in responce to individual HTML tags, fetches the attributes for indexing, and so on.
		/// </summary>
		public delegate void TagHandler( HTMLParser instance, string tag );
        private bool _closeReader = true;

        internal class CaseInsensitiveCharComparer: IComparer
        {
            #region IComparer Members

            public int Compare( object x, object y )
            {
                return Char.ToLower( ( char ) x ) - Char.ToLower( ( char ) y );
            }

            #endregion
        }

        static HTMLParser()
		{
			_tagsTrie = new CharTrie( new CaseInsensitiveCharComparer() );
			_tagsHandlers = new HashMap();
			_tagsHandlers.Add( _tagsTrie.Add( "meta" ), new TagHandler( HandleMeta ) );
			_tagsHandlers.Add( _tagsTrie.Add( "title" ), new TagHandler( OpeningTitle ) );
			_tagsHandlers.Add( _tagsTrie.Add( "/title" ), new TagHandler( ClosingTitle ) );
			_tagsHandlers.Add( _tagsTrie.Add( "body" ), new TagHandler( OpeningBody ) );
			_tagsHandlers.Add( _tagsTrie.Add( "/body" ), new TagHandler( ClosingBody ) );
			_tagsHandlers.Add( _tagsTrie.Add( "script" ), new TagHandler( OpeningScript ) );
			_tagsHandlers.Add( _tagsTrie.Add( "/script" ), new TagHandler( ClosingScript ) );
			_tagsHandlers.Add( _tagsTrie.Add( "h1" ), new TagHandler( OpeningHeading ) );
			_tagsHandlers.Add( _tagsTrie.Add( "h2" ), new TagHandler( OpeningHeading ) );
			_tagsHandlers.Add( _tagsTrie.Add( "h3" ), new TagHandler( OpeningHeading ) );
			_tagsHandlers.Add( _tagsTrie.Add( "h4" ), new TagHandler( OpeningHeading ) );
			_tagsHandlers.Add( _tagsTrie.Add( "h5" ), new TagHandler( OpeningHeading ) );
			_tagsHandlers.Add( _tagsTrie.Add( "h6" ), new TagHandler( OpeningHeading ) );
			_tagsHandlers.Add( _tagsTrie.Add( "/h1" ), new TagHandler( ClosingHeading ) );
			_tagsHandlers.Add( _tagsTrie.Add( "/h2" ), new TagHandler( ClosingHeading ) );
			_tagsHandlers.Add( _tagsTrie.Add( "/h3" ), new TagHandler( ClosingHeading ) );
			_tagsHandlers.Add( _tagsTrie.Add( "/h4" ), new TagHandler( ClosingHeading ) );
			_tagsHandlers.Add( _tagsTrie.Add( "/h5" ), new TagHandler( ClosingHeading ) );
			_tagsHandlers.Add( _tagsTrie.Add( "/h6" ), new TagHandler( ClosingHeading ) );
		}

		/// <summary>
		/// Creates HTML parser over TextReader.
		/// </summary>
		/// <param name="reader">Provides the content to be converted to text.</param>
		public HTMLParser( TextReader reader )
		{
			_reader = new HtmlEntityReader( reader );
			_finished = _reader.Peek() == -1; // Mark as finished if there are no characters in the stream
			_tagBuilder = StringBuilderPool.Alloc();
			_fragmentBuilder = StringBuilderPool.Alloc();
			_charset = string.Empty;
			_title = string.Empty;
			_inBody = _inHeading = _inScript = _inTitle = false;
			_localTagsTrie = null;
			_localTagsHandlers = null;
		}

		/// <summary>
		/// Creates HTML parser over TextReader
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="parseAll">If is set to true, parse all, not only body</param>
        public HTMLParser( TextReader reader, bool parseAll )
            : this( reader )
        {
            _inBody = parseAll;
        }

	    public bool CloseReader
	    {
	        get { return _closeReader; }
	        set { _closeReader = value; }
	    }

	    #region IDisposable Members

		public void Dispose()
		{
            if ( _closeReader )
            {
                _reader.Close();
            }
            StringBuilderPool.Dispose( _tagBuilder );
            StringBuilderPool.Dispose( _fragmentBuilder );
		}

		#endregion

		public void AddTagHandler( string tag, TagHandler handler )
		{
			if( _localTagsTrie == null )
			{
				_localTagsTrie = new CharTrie( new CaseInsensitiveCharComparer() );
				_localTagsHandlers = new HashMap();
			}
			_localTagsHandlers.Add( _localTagsTrie.Add( tag ), handler );
		}

		/// <summary>
		/// Reads a text fragment (text node, or attribute value, etc) form the HTML stream.
		/// </summary>
		/// <returns>Text fragment without any HTML formatting and with the entities substituted.</returns>
		public string ReadNextFragment()
		{
			int	start;
			return ReadNextFragment( out start );
		}

		/// <summary>
		/// Reads a text fragment (text node, or attribute value, etc) form the HTML stream. Provides the information on what was the starting position of the HTML representation of this fragment in the HTML stream.
		/// </summary>
		/// <param name="start">Starting position of the current text fragment in the HTML stream, or -1 if some failure has occured.</param>
		/// <returns>Text fragment without any HTML formatting and with the entities substituted.</returns>
		/// <remarks>
		/// <para>It is an error to read beyond the end of HTML stream. Check the <see cref="Finished"/> property value before calling this function.</para>
		/// </remarks>
		public string ReadNextFragment(out int start)
		{
			if(_finished)
				throw new EndOfStreamException( "Cannot read beyond the end of HTML stream. Please mind the Finished property." );

			_fragmentBuilder.Length = 0;
			do	// This loop avoids returning empty fragments
			{
				start = -1;	// In case of failure, return -1
				char lastReadChar;
				try
				{
					// Read thru any tags preceeding the text node
					string tag;
					while( ((lastReadChar = _reader.PeekChar( false )) == '<') || (!_inBody && !_inTitle) || _inScript )
					{
						lastReadChar = _reader.ReadChar( false );

						if( lastReadChar == '<' )
						{
							tag = ReadTag();
							object handler;
                            lock( _tagsHandlers )
                            {
                                handler = _tagsHandlers[ _tagsTrie.GetMatchingNode( tag ) ];
                            }
							if( handler != null )
							{
								((TagHandler) handler)( this, tag );
							}
							if( _localTagsTrie != null )
							{
								handler = _localTagsHandlers[ _localTagsTrie.GetMatchingNode( tag ) ];
								if( handler != null )
								{
									((TagHandler) handler)( this, tag );
								}
							}
						}
					}

					// We're in between the tags and the text node. Remember this position
					start = _reader.Position;

					if(_doBreakWords)	// As the word ends, stop and return it (along with all the characters following the word)
					{
						// Collect the next token from the text node, up to the first spacing char
						while( _reader.PeekChar( false ) != '<' ) // Do not subst the entities for bracket here
						{
							if(TextDelimitingCategories.IsDelimiter( _reader.PeekChar(true) ))	// Peek with substitution
								break;
							_fragmentBuilder.Append( _reader.ReadChar( true ) );	// Read with substitution and append to the output
						}
						// Collect all the spacing chars following the token, up to the next token or an html tag
						while( _reader.PeekChar( false ) != '<' ) // Do not subst the entities for bracket here
						{
							if(!TextDelimitingCategories.IsDelimiter( _reader.PeekChar(true) ))	// Peek with substitution
								break;
							_fragmentBuilder.Append( _reader.ReadChar( true ) );	// Read with substitution and append to the output
						}
					}
					else	// Do not break the words, return the whole fragment (up to the next tag)
					{
						// Collect the next token from the text node, up to the opening angle bracked of the next tag
						while( _reader.PeekChar( false ) != '<' ) // Do not subst the entities for bracket here
							_fragmentBuilder.Append( _reader.ReadChar( true ) );	// Read with substitution and append to the output
					}
				}
				catch( EndOfStreamException )
				{
					_finished = true;
				}
			}while((!_finished) && (_fragmentBuilder.Length == 0));	// Keep trying until we collect some text. Do not return empty strings

			string result = _fragmentBuilder.ToString();

			// store title in property
			if( _inTitle )
				_title = result;

			return result;
		}

		public HashMap ParseAttributes( string tag )
		{
			HashMap result = new HashMap();
			int pos = 0;

			// tag attr="value1" attr2='value2'
			pos = SkipNonWhitespace( tag, pos );
			pos = SkipWhitespace( tag, pos );

			while( pos < tag.Length )
			{
				int attrNameStart = pos;
				while( pos < tag.Length && Char.IsLetterOrDigit( tag, pos ) )
				{
					pos++;
				}

				int attrNameEnd = pos;
				pos = SkipWhitespace( tag, pos );
				if( pos < tag.Length && tag[ pos ] == '=' )
				{
					pos++;
					pos = SkipWhitespace( tag, pos );
					if( pos < tag.Length && (tag[ pos ] == '\'' || tag[ pos ] == '\"') )
					{
						char startChar = tag[ pos ];
						pos++;
						int attrValueStart = pos;
						while( pos < tag.Length && tag[ pos ] != startChar )
						{
							pos++;
						}
						if( pos < tag.Length && tag[ pos ] == startChar )
						{
							string attrName = tag.Substring( attrNameStart, attrNameEnd - attrNameStart ).ToLower();
							string attrValue = tag.Substring( attrValueStart, pos - attrValueStart );
							result[ attrName ] = attrValue;
						}
					}
				}
				pos = SkipNonWhitespace( tag, pos );
				pos = SkipWhitespace( tag, pos );
			}

			return result;
		}

		#region properties

		public string CharSet
		{
			get { return _charset; }
		}

		public string Title
		{
			get { return _title; }
		}

		public bool InBody
		{
			get { return _inBody; }
		}

		public bool InHeading
		{
			get { return _inHeading; }
		}

		public bool InTitle
		{
			get { return _inTitle; }
		}

		public bool InScript
		{
			get { return _inScript; }
		}

		public bool Finished
		{
			get { return _finished; }
		}

		/// <summary>
		/// Determines whether parser should break its output to individual words and return each word separately, or not.
		/// </summary>
		/// <since>518</since>
		public bool BreakWords
		{
			get
			{
				return _doBreakWords;
			}
			set
			{
				_doBreakWords = value;
			}
		}

		#endregion

		/// <summary>
		/// Returns whole tag including all attributes.
		/// </summary>
		/// <returns></returns>
		protected internal string ReadTag()
		{
			_tagBuilder.Length = 0;
			bool inQuotes = false;
			char lastReadChar;
			while( ((lastReadChar = _reader.ReadChar( false )) != '>') || (inQuotes) )
			{
				if( lastReadChar == '\"' )
					inQuotes = !inQuotes;
				_tagBuilder.Append( lastReadChar );
			}
			return _tagBuilder.ToString();
		}

		private static int SkipNonWhitespace( string tag, int pos )
		{
			while( pos < tag.Length && !Char.IsWhiteSpace( tag, pos ) )
			{
				pos++;
			}
			return pos;
		}

		private static int SkipWhitespace( string tag, int pos )
		{
			while( pos < tag.Length && Char.IsWhiteSpace( tag, pos ) )
			{
				pos++;
			}
			return pos;
		}

		/// <summary>
		/// Position in the input HTML stream, which is the number of characters consumed and converted into text by this moment.
		/// </summary>
		/// <remarks>
		/// These are the characters in HTML representation, as opposed to the plain text characters.
		/// </remarks>
		public int Position
		{
			get { return _reader.Position; }
		}

		#region tag handlers

		/**
         * TODO: the tag should be honestly parsed
         */

		private static void HandleMeta( HTMLParser instance, string tag )
		{
			tag = tag.ToLower();
			int index = tag.IndexOf( "http-equiv" );
			if( index > 0 && tag.IndexOf( "\"content-type\"", index + 10 ) > 0 )
			{
				index = tag.IndexOf( "content", index + 10 );
				if( index > 0 )
				{
					index = tag.IndexOf( "charset=", index + 7 );
					if( index > 0 )
					{
						index += 8; // length of "charset="
						int charsetEnd = tag.IndexOfAny( new char[] {'"', '\'', ' ', ';', ','}, index );
						if( charsetEnd <= index )
						{
							charsetEnd = tag.Length;
						}
						instance._charset = tag.Substring( index, charsetEnd - index ).Trim();
					}
				}
			}
		}

		private static void OpeningTitle( HTMLParser instance, string tag )
		{
			instance._inTitle = true;
		}

		private static void ClosingTitle( HTMLParser instance, string tag )
		{
			instance._inTitle = false;
		}

		private static void OpeningBody( HTMLParser instance, string tag )
		{
			instance._inBody = true;
		}

		private static void ClosingBody( HTMLParser instance, string tag )
		{
			instance._inBody = false;
		}

		private static void OpeningScript( HTMLParser instance, string tag )
		{
			instance._inScript = true;
		}

		private static void ClosingScript( HTMLParser instance, string tag )
		{
			instance._inScript = false;
		}

		private static void OpeningHeading( HTMLParser instance, string tag )
		{
			instance._inHeading = true;
		}

		private static void ClosingHeading( HTMLParser instance, string tag )
		{
			instance._inHeading = false;
		}

		#endregion

		/// <summary>
		/// A reader that provides the unput text.
		/// </summary>
		protected internal HtmlEntityReader _reader;

		protected internal bool _finished;

		protected internal StringBuilder _tagBuilder;

		protected internal StringBuilder _fragmentBuilder;

		protected internal string _charset;

		protected internal string _title;

		protected internal bool _inBody;

		protected internal bool _inHeading;

		protected internal bool _inScript;

		protected internal bool _inTitle;

		protected internal static CharTrie _tagsTrie;

		protected internal static HashMap _tagsHandlers;

		protected internal CharTrie _localTagsTrie;

		protected internal HashMap _localTagsHandlers;

		/// <summary>
		/// Determines whether parser should break its output to individual words and return each word separately, or not.
		/// </summary>
		protected internal bool _doBreakWords = true;
	}

	public class HtmlTools
	{
        private static readonly Regex _rxStripHTML = new Regex( "<[^<>]+>" );
        private static readonly Regex _rxLineBreak = new Regex( "< *br */ *>" );
        private static readonly HtmlLinkConverter _htmlLinkConverter = new HtmlLinkConverter();

		/// <summary>
		/// Tries to detect charset from html stream of resource
		/// if charset is not set returns the name of default encoding.
		/// </summary>
		public static string DetectCharset( TextReader reader )
		{
			string charset = Encoding.Default.HeaderName;
            using( HTMLParser parser = new HTMLParser( reader ) )
            {
                parser.CloseReader = false;
                while( !parser.Finished )
                {
                    parser.ReadNextFragment();
                    if( parser.InBody )
                    {
                        if( parser.CharSet.Length > 0 )
                        {
                            charset = parser.CharSet;
                        }
                        break;
                    }
                }
            }
			return charset.Replace( '_', '-' );
		}

		/// <summary>
		/// Skips scripts from a document.
		/// </summary>
		public static string SkipScripts( string htmlText )
		{
			string text = htmlText.ToLower();
			int scriptOffset = 0;
			int offset;
            StringBuilder skipper = StringBuilderPool.Alloc();
            try
            {
                while( scriptOffset < text.Length &&
                    (offset = text.IndexOf( "<script", scriptOffset )) > 0 )
                {
                    if( offset > scriptOffset )
                    {
                        skipper.Append( htmlText, scriptOffset, offset - scriptOffset );
                    }
                    scriptOffset = text.IndexOf( "/script>", offset );
                    if( scriptOffset < 0 )
                    {
                        scriptOffset = text.Length;
                    }
                    else
                    {
                        scriptOffset += "/script>".Length;
                    }
                }
                if( scriptOffset < text.Length )
                {
                    skipper.Append( htmlText, scriptOffset, text.Length - scriptOffset );
                }
                return skipper.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( skipper );
            }
		}

		/// <summary>
		/// Fixing relative links in tag attributes.
		/// </summary>
		/// <param name="htmlText"></param>
		/// <param name="URL"></param>
		/// <returns></returns>
		public static string FixRelativeLinks( string htmlText, string URL )
		{
			Uri currentUri;
            try
            {
                currentUri = new Uri( URL );                
            }
            catch( UriFormatException )
            {
                return htmlText;
            }
			htmlText = FixAttributeLinks( htmlText, currentUri, "src=" );
			htmlText = FixAttributeLinks( htmlText, currentUri, "href=" );
			htmlText = FixAttributeLinks( htmlText, currentUri, "background=" );
			htmlText = FixAttributeLinks( htmlText, currentUri, "url(" );
			htmlText = FixAttributeLinks( htmlText, currentUri, "@import " );
			return htmlText;
		}

        private static string FixAttributeLinks( string htmlText, Uri currentUri, string tag )
		{
			char[] URLsplitters = {' ', '>', ';'};
			char[] quotes = {'"', '\''};
			StringBuilder textBuilder = StringBuilderPool.Alloc();
            try
            {
                int urlStart, urlEnd, added = 0;

                while( added < htmlText.Length && ( urlStart = Utils.IndexOf( htmlText, tag, added, true ) ) > 0 )
                {
                    urlStart += tag.Length;
                    textBuilder.Append( htmlText, added, urlStart - added );
                    added = urlStart;
                    if( urlStart > htmlText.Length - 2 )
                    {
                        break;
                    }
                    urlEnd = (htmlText[ urlStart ] == '"' || htmlText[ urlStart ] == '\'')
                        ? htmlText.IndexOfAny( quotes, ++urlStart ) : htmlText.IndexOfAny( URLsplitters, urlStart );
                    if( urlEnd < 0 )
                    {
                        break;
                    }
                    string link = htmlText.Substring( urlStart, urlEnd - urlStart );
                    if( link.Length > 0 && link.IndexOf( "://" ) < 0 && !link.StartsWith( "mailto:" ) )
                    {
                        try
                        {
                            // For Web URIs,
                            // compose the new URI
                            // in a proper way
                            if( (currentUri.Scheme == Uri.UriSchemeHttp)
                                || (currentUri.Scheme == Uri.UriSchemeHttps)
                                || (currentUri.Scheme == Uri.UriSchemeFtp) )
                                link = new Uri( currentUri, link ).ToString();
                                // For files,
                                // URI composing makes them seem
                                // like network names,
                                // which causes a hangup.
                                //
                                // So,
                                // do a manual composition here.
                            else if( currentUri.Scheme == Uri.UriSchemeFile )
                                link = new Uri( currentUri.ToString() + link ).ToString();
                            // Otherwise,
                            // don't know what to do
                            // with other proto types.
                        }
                        catch
                        {
                        }
                    }
                    textBuilder.Append( '"' );
                    textBuilder.Append( link.Trim( '"', '\'' ) );
                    textBuilder.Append( '"' );
                    added = urlEnd;
                    if( htmlText[ urlEnd ] == '"' || htmlText[ urlEnd ] == '\'' )
                    {
                        ++added;
                    }
                }
                if( added < htmlText.Length )
                {
                    textBuilder.Append( htmlText, added, htmlText.Length - added );
                }
                return textBuilder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( textBuilder );
            }
		}

		/// <summary>
		/// Converts the HTML and mail links in the specified text to &lt;a href&gt;.
		/// </summary>
		public static string ConvertLinks( string text )
		{
            return _htmlLinkConverter.ConvertLinks( text );
		}

		/// <summary>
		/// Checks if the given text looks like a start of a valid HTML file.
		/// </summary>
		public static bool IsHTML( string text )
		{
			text = text.ToLower();
			int pos = text.IndexOf( "<html" );
			if( pos >= 0 && text.IndexOf( ">", pos ) >= 0 )
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes all HTML tags from the specified string.
		/// </summary>
        public static string StripHTML( string html )
        {
            return _rxStripHTML.Replace( html, "" );
        }
        public static string ReplaceLineBreaks( string html )
        {
            return _rxLineBreak.Replace( html, "\n" );
        }

        public static string SafeHtmlDecode( string text )
        {
            try
            {
                return HttpUtility.HtmlDecode( text );
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "HttpUtility.HtmlDecode failed for '" + text + "': " + ex.ToString() );
                return text;
            }
        }
	}

    /// <summary>
    /// Replaces links in plain text with HTML references and allows derived classes to
    /// customize replacement behavior.
    /// </summary>
    public class HtmlLinkConverter
    {
        private static readonly Regex _rxLink = new Regex( @"(http:|https:|ftp:)\/\/([^\s()](?!&(gt|lt|nbsp)+;))+[^\p{Pe}\p{Pc}\p{Pd}\p{Pf}\p{Pi}\p{Ps}\p{Po}\s]/?" );
        private static readonly Regex _rxWWWLink = new Regex( @"(?<!\/\/)www\.([^\s()](?!&(gt|lt|nbsp);))+[^\p{Pe}\p{Pc}\p{Pd}\p{Pf}\p{Pi}\p{Ps}\p{Po}\s]/?", RegexOptions.IgnoreCase );
        private static readonly Regex _rxMailLink = new Regex( @"((mailto:)?|(news:.+))[\w\.\-]+\@([\w\.\-])+\.([\w\.\-])+\w" );
        private static readonly Regex _rxUncNameLink;

        static HtmlLinkConverter()
        {
            StringBuilder pathChars = new StringBuilder( "[^\\?\\*\\s\\" );
            pathChars.Append( Path.DirectorySeparatorChar );
            pathChars.Append( '\\' );
            pathChars.Append( Path.AltDirectorySeparatorChar );
            pathChars.Append( '\\' );
            pathChars.Append( Path.VolumeSeparatorChar );
            foreach( char c in Path.InvalidPathChars )
            {
                pathChars.Append( '\\' );
                pathChars.Append( c );
            }
            pathChars.Append( "]+" );
            _rxUncNameLink = new Regex( @"\\\\" + pathChars + @"(\\" + pathChars + ")*" );
        }

        public string ConvertLinks( string text )
        {
            text = _rxLink.Replace( text, new MatchEvaluator( ReplaceHTMLLink ) );
            text = _rxWWWLink.Replace( text, new MatchEvaluator( ReplaceWWWLink ) );
            text = _rxMailLink.Replace( text, new MatchEvaluator( ReplaceMailLink ) );
            text = _rxUncNameLink.Replace( text, new MatchEvaluator( ReplaceUncNameLink ) );
            return text;
        }

        /// <summary>
		/// Converts a plain text string to the HTML Anchor element targeting the location identified by the string,
		/// with the anchor label containing the location url.
		/// </summary>
		/// <param name="href">URI of the location in the form which is suitable for the href attribute of anchor HTML element.</param>
		/// <param name="hrefText">Human-readable form of the URI.</param>
		/// <returns>Text representation of the HTML Anchor element.</returns>
		/// <remarks>Override this function to provide special processing of URLs or their text representation.</remarks>
        protected virtual string BuildHref( string href, string hrefText )
        {
            return "<a href=\"" + href + /*"\" title=\"" + href +*/ "\">" + hrefText + "</a>";
        }

        protected virtual string ReplaceHTMLLink( Match m )
        {
            return BuildHref( m.Value, m.Value );
        }

        protected virtual string ReplaceWWWLink( Match m )
        {
            return BuildHref( "http://" + m.Value,  m.Value );
        }
    
        protected virtual string ReplaceMailLink( Match m )
        {
            if( !m.Value.StartsWith( "mailto:" ) && !m.Value.StartsWith( "news:" ) )
            {
                return BuildHref( "mailto:" + m.Value, m.Value );
            }
            return BuildHref( m.Value, m.Value );
        }

        protected string ReplaceUncNameLink( Match m )
        {
            return BuildHref( "file:" + m.Value, m.Value );
        }
    }

	public class HtmlEntityReader : TextReader
	{
		public HtmlEntityReader( TextReader baseReader )
		{
			_baseReader = baseReader;
			_isBaseEof = false;

			// Pre-cache the first chars
			StartLookahead();
			Debug.Assert( _lookahead != null );
			Debug.Assert( _lookaheadPos == 0 );
		}

		static HtmlEntityReader()
		{
			// Fill in the entities dictionary
			_entitiesTrie = new CharTrie( null );
			_entity2CharMap = new HashMap();

			// Find the resource
			Stream	stream = GetHtmlEntitiesStream();
			if(stream == null)
				throw new Exception( "HtmlEntityReader could not locate the HTML entity definitions resource stream." );

			try
			{
				// Load the XML with entities definitions
				XmlDocument	xml = new XmlDocument();
				xml.Load( stream );
				stream.Close();

				// Load each entity
				foreach(XmlElement xmlEntity in xml.SelectNodes( "/Entites/Entity" ))
					_entity2CharMap.Add( _entitiesTrie.Add( xmlEntity.GetAttribute( "Name" ) ), (char)int.Parse( xmlEntity.GetAttribute( "Value" ) ) );
			}
			catch(Exception ex)
			{
				throw new Exception("HtmlEntityReader could not complete loading the entity definitions into the dictionary.", ex);
			}
		}

		/// <summary>
		/// Returns a stream that contains the XML Entities definitions.
		/// The stream is an XML document: <Entites><Entity Name="nbsp" Value="160" /> … </Entites>
		/// </summary>
		/// <returns>The entities stream.</returns>
		public static Stream GetHtmlEntitiesStream()
		{
			foreach(string resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
			{
				if(resourceName.EndsWith("HtmlEntities.xml"))
					return Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
			}
			return null;
		}

		#region Data

		/// <summary>
		/// The reader that supplies us with the raw text.
		/// </summary>
		protected TextReader _baseReader = null;

		/// <summary>
		/// Lookahead buffer that is used when we're reading entities and want to peek some chars forward in the stream without actually reading them.
		/// </summary>
		/// <remarks>
		/// We always maintain the constant queue size, equal to <see cref="_lookaheadSize"/>. If there is less content than required to fill the lookahead queue up, then it's padded with '-1's which represent the end of stream.
		/// The queue stores <see cref="int"/>s not <see cref="char"/>s according to the <see cref="TextReader.Read()"/> method's signature.
		/// </remarks>
		protected int[] _lookahead = null;

		/// <summary>
		/// Size of the lookahead buffer, or the cyclic queue capacity. Must be a power of 2.
		/// </summary>
		protected int _lookaheadSize = 0x10;

		/// <summary>
		/// A mask which wraps the indices into the <see cref="_lookahead"/> array by ANDing with.
		/// </summary>
		protected int _lookaheadMask = 0x0F;

		/// <summary>
		/// Head of the lookahead cyclic queue. It's the tail also, as the length is maintained constant.
		/// </summary>
		protected int _lookaheadPos = 0;

		/// <summary>
		/// Tells whether the base stream is thru.
		/// </summary>
		protected bool _isBaseEof = false;

		/// <summary>
		/// A reusable temporary buffer.
		/// </summary>
		private StringBuilder _fragmentBuilder = new StringBuilder();

		/// <summary>
		/// Default comparer for the trie nodes.
		/// </summary>
		protected static TrieNodeComparer _trieNodeComparer = new TrieNodeComparer();

		/// <summary>
		/// A trie that provides for incremental match of entities.
		/// </summary>
		protected static CharTrie _entitiesTrie;

		/// <summary>
		/// A map that maps trie nodes (ie entity names) to the corresponding characters they represent.
		/// </summary>
		protected static HashMap _entity2CharMap;

		/// <summary>
		/// Defines whether the previously-read character was an entity or not.
		/// </summary>
		protected bool _isLastCharEntity = false;

		/// <summary>
		/// A hash table of the entities that should be ignored and should not be resolved when reading the stream.
		/// </summary>
		protected HashSet _hashIgnoredEntities = null;

		#endregion

		/// <summary>
		/// Gets or sets a <see cref="HashSet">hash set</see> of entities that should be ignored when parsing the stream.
		/// </summary>
		public HashSet IgnoredEntities
		{
			get { return _hashIgnoredEntities; }
			set { _hashIgnoredEntities = value; }
		}

		/// <summary>
		/// On startup, initializes the lookahead queue and reads the first chars into it.
		/// </summary>
		protected void StartLookahead()
		{
			// Check the size
			Debug.Assert( _lookaheadSize != 0 );
			if( (int) Math.Pow( 2, Math.Log( _lookaheadSize ) / Math.Log( 2 ) ) != _lookaheadSize ) // Must be a power of 2
			{
				Debug.Assert( false );
				_lookaheadSize = (int) Math.Pow( 2, Math.Ceiling( Math.Log( _lookaheadSize ) / Math.Log( 2 ) ) ); // Ensure it's a power of 2
			}
			_lookaheadMask = _lookaheadSize - 1; // Mask for wrapping into the scope

			Debug.Assert( _lookahead == null );

			_lookahead = new int[_lookaheadSize];
			_lookaheadPos = 0;

			int a;

			// Read existing chars
			for( a = 0; a < _lookaheadSize; a++ )
			{
				_lookahead[ a ] = _baseReader.Read();
				if( _lookahead[ a ] == -1 )
				{
					_isBaseEof = true;
					break;
				}
			}

			// If there were not enough chars to fill the lookahead buffer, pad with -1
			for(; a < _lookaheadSize; a++ )
				_lookahead[ a ] = -1;
		}

		public override void Close()
		{
			_isBaseEof = true;
			_lookahead[ _lookaheadPos & _lookaheadMask ] = -1; // Mark as over
			_baseReader.Close();
		}

		protected override void Dispose( bool disposing )
		{
			_isBaseEof = true;
			_lookahead[ _lookaheadPos & _lookaheadMask ] = -1; // Mark as over
			_baseReader.Close();
		}

		public override int Peek()
		{
			return _lookahead[ _lookaheadPos & _lookaheadMask ]; // No special checks for EOF needed, as we should return -1 in this case and we do actually have -1 there.
		}

		/// <summary>
		/// Reads a character at a position of <see cref="forth"/> steps ahead the current position.
		/// </summary>
		/// <param name="forth">Number of steps forth away from the current position, <c>0</c> means current (=<see cref="Peek()"/>). Must be not above <see cref="_lookaheadSize">the lookahead buffer size</see>.</param>
		/// <returns>Character, or <c>-1</c> if beyond end of stream.</returns>
		public int Peek( int forth )
		{
			Debug.Assert( forth < _lookaheadSize );
			return _lookahead[ (_lookaheadPos + forth) & _lookaheadMask ];
		}

		public override int Read()
		{
			int nRet = _lookahead[ _lookaheadPos & _lookaheadMask ];
			if( !_isBaseEof ) // Have to read more into cache?
			{
				if( (_lookahead[ _lookaheadPos & _lookaheadMask ] = _baseReader.Read()) == -1 )
					_isBaseEof = true;
			}
			else
				_lookahead[ _lookaheadPos & _lookaheadMask ] = -1;

			_lookaheadPos++;
			return nRet; // Prev char
		}

		/// <summary>
		/// Reads a character from the stream, optionally substituting entities.
		/// </summary>
		/// <param name="substituteEntities">Whether to substitute entities or not.</param>
		/// <returns>Character read from the stream, or <c>-1</c> if an end of stream was encountered.</returns>
		/// <remarks>This function is a subset of <see cref="Read(bool, bool, out int)"/>.</remarks>
		public int Read(bool substituteEntities)
		{
			int	len;
			return Read(substituteEntities, true, out len);
		}

		/// <summary>
		/// Reads a character from the stream, optionally substituting entities. Either removes the matched characters from the stream, or just looks up the next character. Returns the number of characters matching the returned character in the stream.
		/// </summary>
		/// <param name="substituteEntities">Whether to substitute entities or not.</param>
		/// <param name="removeFromStream">If <c>True</c>, the read symbols are removed from the stream and next read attempt will read the next symbol. If <c>False</c>, works as <see cref="Peek()"/>, but is capable of substituting the entites.</param>
		/// <param name="matchedLength">Number of characters substituted by the symbol returned, always equal to <c>1</c> if <paramref name="substituteEntities"/> is <c>False</c>, otherwise, equal to the length of the entity representation.</param>
		/// <returns>Character read from the stream, or <c>-1</c> if an end of stream was encountered.</returns>
		/// <remarks>See <see cref="ReadChar"/> for the safe version of this function which always returns a valid char, or throws an exception in cases when this function would return <c>-1</c>.</remarks>
		public int Read( bool substituteEntities, bool removeFromStream, out int matchedLength )
		{
			Debug.Assert( _lookahead != null );
			Debug.Assert( _lookaheadSize != 0 );
			Debug.Assert( _lookahead.Length == _lookaheadSize );
			Debug.Assert( _lookaheadMask == _lookaheadSize - 1 );

			_isLastCharEntity = false; // Default assumption
			matchedLength = 1;	// This applies to the case when it's the simple char (not an entity), or an entity that cannot be recognized

			if( (!substituteEntities) || (Peek() != '&') )
				return removeFromStream ? Read() : Peek(); // Not an entity, or entities mode off

			int ch = -1;
			int nPos;

			// May be some kind of entity
			if( Peek( 1 ) == '#' )
			{ // Represents a character code

				// Collect the entity body
				_fragmentBuilder.Length = 0;
				for( nPos = 2, ch = -1; nPos < _lookaheadSize - 1; nPos++ )
				{
					ch = Peek( nPos );
					if( (ch == (int) ';') || (ch == -1) )
						break;
					_fragmentBuilder.Append( (char) ch );
				}

				if( ch == -1 ) // Malformed entity
					return removeFromStream ? Read() : Peek();

				try // Try converting string rep to a number
				{
					string sCode = _fragmentBuilder.ToString();
					if( (sCode[ 0 ] == 'x') || (sCode[ 0 ] == 'X') ) // Hexadecimal
						ch = Convert.ToInt32( sCode.Substring( 1 ), 0x10 );
					else
						ch = Convert.ToInt32( sCode, 10 );

					matchedLength = nPos + 1;	// Plus the final char to which nPos currently points
					if(removeFromStream)
						Skip( matchedLength );
					_isLastCharEntity = true;
					return ch;
				}
				catch
				{
					return removeFromStream ? Read() : Peek(); // Malformed entity
				}
			}
			else // Represents some named entity … possibly.
			{
				// Walk the char trie following the path represented by the character stream
				StringBuilder	sb = StringBuilderPool.Alloc();
                try 
                {
                    CharTrie.Node node = _entitiesTrie.Root;
                    for( nPos = 1; nPos < _lookaheadSize - 1; nPos++ )
                    {
                        ch = Peek( nPos );
                        if( ch == -1 )
                            return removeFromStream ? Read() : Peek(); // Not an entity
                        if( ch == (int) ';' )
                            break; // Parsed OK

                        node = node.SubNode( (char) ch, _trieNodeComparer );

                        // If we have fallen of the path (tried to walk into a non-existent node), then it's not a known entity
                        if( node == null )
                            break;

                        sb.Append(node.Value);
                    }

                    // The character that corresponds to the trie node at which the path ended
                    // It may be null in case if the path is partial and we have not reached the ending character of the path (eg had "in" on the path of "infin")
                    object subst = null;
                    if(node != null)	// If we have not fallen off the path
                        subst = _entity2CharMap[ node ];

                    // Return the result: either the resolved char entity (if it was OK), or the next char AS IS (if the entity was not recognized)
                    if(subst != null)
                    {	// A valid, recognized char entity, return its char equivalent and skip its codes
                        bool	bSubstituteEntity = true;

                        // Check if this entity should be ignored
                        if(_hashIgnoredEntities != null)
                        {
                            lock(_hashIgnoredEntities)
                            {
                                if(_hashIgnoredEntities.Contains(sb.ToString()))
                                    bSubstituteEntity = false;	// Do not substitute the entity if it's on the ignore list
                            }
                        }
					
                        // Substitute the entity if it has not been suppressed
                        if(bSubstituteEntity)
                        {
                            matchedLength = nPos + 1;	// Plus the final semicolon to which nPos currently points
                            if(removeFromStream)
                                Skip( matchedLength );
                            _isLastCharEntity = true;
                            return (char) subst; // Yeah! We've substituted the entity successfully!
                        }
                    }
                }
                finally
			    {
			        StringBuilderPool.Dispose( sb );
			    }

				// Not an entity, return the current char without substitution
				return removeFromStream ? Read() : Peek();
			}
		}

		/// <summary>
		/// Skips the specified number of chars ahead the current position from reading.
		/// </summary>
		/// <param name="skip">Number of characters to skip.</param>
		/// <remarks>First we do lookahead, if it shows that we're interested in the content, we return the substitution and skip the looked-ahead chars.</remarks>
		private void Skip( int skip ) // TODO: you may provide a more intelligent implementation …
		{
			for( int a = 0; a < skip; a++ )
				Read();
		}

		public override int Read( char[] buffer, int index, int count )
		{
			throw new NotImplementedException(); // YAGNI
		}

		public override string ReadToEnd()
		{
			throw new NotImplementedException(); // YAGNI
		}

		public override int ReadBlock( char[] buffer, int index, int count )
		{
			throw new NotImplementedException(); // YAGNI
		}

		public override string ReadLine()
		{
			throw new NotImplementedException(); // YAGNI
		}

		/// <summary>
		/// Tells whether the stream is over.
		/// </summary>
		/// <remarks>Note that this applies not to the base stream, but to the instance in the whole, which has its lookahead queue that has to be emptied after the base stream is thru.</remarks>
		public bool Eof
		{
			get { return (_isBaseEof) && (_lookahead[ _lookaheadPos & _lookaheadMask ] == -1); }
		}

		/// <summary>
		/// Position in the stream we're currently reading.
		/// </summary>
		public int Position
		{
			get { return _lookaheadPos; }
		}

		/// <summary>
		/// Whether the previously-read character was an entity or not.
		/// </summary>
		public bool IsLastCharEntity
		{
			get { return _isLastCharEntity; }
		}

		/// <summary>
		/// A node comparer for the trie nodes that is produced from the trie's internal node comparer.
		/// </summary>
		protected class TrieNodeComparer : IComparer
		{
			public int Compare( object x, object y )
			{
				return (int)((CharTrie.Node)x).Value - (int)((CharTrie.Node)y).Value;
			}
		}

		/// <summary>
		/// Does almost the same as <see cref="Read(bool)"/>. The only difference is that the return value of this function is always safe and represents a valid character. <see cref="Read(bool)"/>'s <c>-1</c> value that indicates an end of stream causes an <see cref="EndOfStreamException"/> exception to be thrown.
		/// </summary>
		/// <param name="substituteEntities">Defines whether to substitute entities when reading. This parameter is passed to the <see cref="Read(bool)"/> function.</param>
		/// <returns>Character read from the stream.</returns>
		public char ReadChar( bool substituteEntities )
		{
			int	len;
			int ret = substituteEntities ? Read( substituteEntities, true, out len ) : Read();
			if( ret == -1 )
				throw new EndOfStreamException( "Trying to read beyond the end of an HTML Entity Reader stream." );
			return (char) ret;
		}

		/// <summary>
		/// Does almost the same as <see cref="Peek"/>. The only difference is that the return value of this function is always safe and represents a valid character. <see cref="Peek"/>'s <c>-1</c> value that indicates an end of stream causes an <see cref="EndOfStreamException"/> exception to be thrown.
		/// </summary>
		/// <param name="substituteEntities">Defines whether to substitute entities when reading. Currently not implemented.</param>
		/// <returns>Character looked up in the stream, but not removed from it.</returns>
		public char PeekChar( bool substituteEntities )
		{
			int	len;
			int ret = Read( substituteEntities, false, out len );
			if( ret == -1 )
				throw new EndOfStreamException( "Trying to read beyond the end of an HTML Entity Reader stream." );
			return (char) ret;
		}
	}
}