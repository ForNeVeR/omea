/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.MailParser
{
	/// <summary>
	/// Type of the parsed paragraph.
	/// </summary>
	public enum ParagraphType
	{
		/// <summary>
		/// Ordinary paragraph.
		/// </summary>
		Plain,

		/// <summary>
		/// Preformatted text, like in the &lt;pre /&gt; HTML tag.
		/// </summary>
		Fixed,
		
		/// <summary>
		/// Contains signature lines.
		/// </summary>
		Sig,
		
		/// <summary>
		/// Something quite special, for example, Outlook information.
		/// </summary>
		Service
	}

	/// <summary>
	/// Type of the plain-text paragraph.
	/// </summary>
    public enum PlainTextParaType
    {
		/// <summary>
		/// Normal paragraph, consists of several lines glued up into one paragraph.
		/// </summary>
        Plain,
		
		/// <summary>
		/// Preformatted text, like in the &lt;pre /&gt; HTML tag.
		/// Happens when the lines are too short to be considered as wrapped.
		/// </summary>
		Fixed,
		
		/// <summary>
		/// The lines are long enough to seem to be paragraphs not split into lines. Each line should be treated as a paragraph.
		/// </summary>
		Unwrapped
    }
	
	/// <summary>
	/// Parses the mail body into a list of paragraphs of different types (text, quote, sig).
	/// </summary>
	public class MailBodyParser
	{
		public class Paragraph
		{
			private readonly string _text;
			private readonly ParagraphType _type;
			private readonly int    _quoteLevel;
			private readonly string _quotePrefix;
			private readonly bool   _outlookQuote;

			/// <summary>
			/// Initializes the instance.
			/// </summary>
			internal Paragraph( string text, ParagraphType type, int quoteLevel, string quotePrefix, bool outlookQuote )
			{
				_text = text;
				_type = type;
				_quoteLevel = quoteLevel;
				_quotePrefix = quotePrefix;
				_outlookQuote = outlookQuote;
			}

			public string Text
			{
				get { return _text; }
			}

			public ParagraphType Type
			{
				get { return _type; }
			}

			public int QuoteLevel
			{
				get { return _quoteLevel; }
			}

			public string QuotePrefix
			{
				get { return _quotePrefix; }
			}

			public bool OutlookQuote
			{
				get { return _outlookQuote; }
			}
		}
		
		private bool           _foundOutlookQuote;
		private int            _lastQuoteLevel = 0;
		private string         _lastQuotePrefix = "";
		private readonly int   _minWrapWidth;
		private readonly ArrayList      _paragraphs = new ArrayList();
        private readonly MailBodyParser _origText;

		public MailBodyParser( string body, int minWrapWidth )
            : this( body, minWrapWidth, null )
        {
        }
        
        public MailBodyParser( string body, int minWrapWidth, MailBodyParser origText )
		{
            _minWrapWidth = minWrapWidth;
            _origText = origText;

            if ( body != null )
            {
                long startTicks = DateTime.Now.Ticks;
                ParseMailBody( body );
                long endTicks = DateTime.Now.Ticks;
                Debug.WriteLine( "MailBodyParser parsing took " + (endTicks - startTicks) / 10000 + " ms" );
            }
		}

		public int ParagraphCount
		{
			get { return _paragraphs.Count; }
		}

		public Paragraph GetParagraph( int index )
		{
			return (Paragraph) _paragraphs[ index ];
		}

        internal Paragraph FindParagraph( string part1, string part2 )
        {
            string rxText = Regex.Escape( part1 ) + "\\s+" + Regex.Escape( part2 );
            Regex rx = new Regex( rxText );
            foreach( Paragraph para in _paragraphs )
            {
                if ( rx.IsMatch( para.Text ) )
                    return para;
            }
            return null;
        }

		/**
		 * Parses the body of the message and fills the paragraphs list.
		 */
		
		private void ParseMailBody( string body )
		{
			body = body.Replace( "\r\n", "\n" );
            string[] lines = body.Split( '\n' );

			ArrayList curParaLines = new ArrayList();
			int prevStartSpaces = -1;
			
            bool foundSig = false;
            bool textAfterSig = false;
            bool emptyLineAfterSig = false;

			_foundOutlookQuote = false;
			bool prevFirstLine = false;
			foreach( string line in lines )
			{
                if ( foundSig )
				{
					if ( line.Trim() == "" )
                    {
                        if ( textAfterSig )
                        {
                            emptyLineAfterSig = true;
                        }
                        AddPara( line, ParagraphType.Sig );
                    }
                    else
                    {
                        if ( emptyLineAfterSig )
                        {
                            foundSig = false;
                        }
                        else
                        {
                            textAfterSig = true;
                            AddPara( line, ParagraphType.Sig );
                        }
                    }
				}
				
                if ( !foundSig )
				{
                    if ( line.StartsWith( "-- " ) )
					{
						AddTextPara( curParaLines, true );
                        curParaLines.Clear();
						AddPara( line, ParagraphType.Sig );
						foundSig = true;
                        textAfterSig = false;
                        emptyLineAfterSig = false;
						continue;
					}
					else if ( IsOutlookQuoteStart( line ) )
					{
						AddTextPara( curParaLines, true );
                        curParaLines.Clear();
						AddPara( line, ParagraphType.Service );
						
						// the text after ----- Original message ----- is formatted as a quote
                        // only if there was some significant text before it
						if( HaveNonquotedTextParagraphs() )
						{
							_foundOutlookQuote = true;
						}
						continue;
					}
	
                    int quoteLevel = GetQuoteLevel( line );

					string strippedLine = ( quoteLevel > 0 ) 
						? StripQuoting( line ) 
						: line;

					if ( strippedLine.Trim() == "" )
					{
                        AddTextPara( curParaLines, true );
						curParaLines.Clear();
						prevStartSpaces = -1;
						continue;
					}

					// check for broken quoting:
                    // if the original message contains the last line of the last
                    // (quoted) paragraph in the same paragraph as the current (unquoted)
                    // line, it means that the current line is broken quoting and needs
                    // to be appended to the last paragraph.
                    if ( quoteLevel == 0 && _lastQuoteLevel == 1 && _origText != null && curParaLines.Count > 0 )
                    {
                        string lastLine = (string) curParaLines [curParaLines.Count-1];
                        if ( _origText.FindParagraph( lastLine.Trim(), strippedLine.Trim() ) != null )
                        {
                            string fixedLine = lastLine + " " + strippedLine.Trim();
                            curParaLines [curParaLines.Count-1] = fixedLine;
                            continue;
                        }
                    }
                    
                    string quotePrefix = (quoteLevel > 0) 
						? GetQuotePrefix( line ) 
						: "";

					if ( quoteLevel != _lastQuoteLevel || quotePrefix != _lastQuotePrefix )
					{
						AddTextPara( curParaLines, false );
						curParaLines.Clear();
						_lastQuoteLevel = quoteLevel;
						_lastQuotePrefix = quotePrefix;
					}

                    int startSpaces = CountStartingSpaces( line );

					// The condition below this line implements the following logic:
					//  - any time the indent changes, we create a fixed paragraph,
					//  - except for the case when the first line of a paragraph is 
					//    indented and the following lines are not

					if ( quoteLevel == 0 && ((startSpaces > 0 && prevStartSpaces >= 0 ) || (prevStartSpaces > 0 && !prevFirstLine) ))
					{
						AddFixedParas( curParaLines, prevFirstLine );
                        AddPara( line, ParagraphType.Fixed );
						curParaLines.Clear();
					}
					else
					{
						curParaLines.Add( strippedLine );
					}
					prevFirstLine = ( prevStartSpaces == -1 );
					prevStartSpaces = startSpaces;
				}
			}
			if ( curParaLines.Count > 0 )
				AddTextPara( curParaLines, true );
		}

		/**
		 * Adds the paragraph with the specified type and text to the paragraph list.
		 */

		private void AddPara( string body, ParagraphType type )
		{
			_paragraphs.Add( new Paragraph( body, type, 0, "", false ) );
		}

		/**
		 * Adds a pending paragraph (possibly quoted) to the paragraph list.
		 */

		private void AddTextPara( ArrayList lines, bool beforeEmptyLine )
		{
			if ( lines.Count > 0 )
			{
                PlainTextParaType paraType = IsPlainTextPara( lines );
                if ( paraType == PlainTextParaType.Plain )
                {
                    StringBuilder bodyBuilder = StringBuilderPool.Alloc();
                    try 
                    {
                        for( int i=0; i<lines.Count-1; i++ )
                        {
                            string line = (string) lines [i];
                            bodyBuilder.Append( line );
                            if ( !line.EndsWith( " " ) )
                            {
                                bodyBuilder.Append( " " );
                            }
                        }
                        bodyBuilder.Append( lines [lines.Count-1] );
                    
                        _paragraphs.Add( new Paragraph( bodyBuilder.ToString(), ParagraphType.Plain, 
                            _lastQuoteLevel, _lastQuotePrefix, _foundOutlookQuote ) );
                    }
                    finally
                    {
                        StringBuilderPool.Dispose( bodyBuilder );
                    }
                }
                else if ( paraType == PlainTextParaType.Fixed )
                {
                    AddFixedParas( lines, beforeEmptyLine );
                }
                else
                {
                    foreach( string line in lines )
                    {
                        _paragraphs.Add( new Paragraph( line, ParagraphType.Plain, _lastQuoteLevel, 
                            _lastQuotePrefix, _foundOutlookQuote ) );
                    }
                }
			}
		}

		/**
		 * Adds a fixed paragraph for each line in the specified array.
		 */

		private void AddFixedParas( ArrayList lines, bool beforeEmptyLine )
		{
            if ( _paragraphs.Count > 0 && beforeEmptyLine )
            {
                Paragraph oldPara = (Paragraph) _paragraphs [_paragraphs.Count-1];
                if ( oldPara.Type == ParagraphType.Fixed )
                {
                    // insert a break paragraph after a sequence of fixed paragraphs
                    _paragraphs.Add( new Paragraph( "", ParagraphType.Fixed, 
                        _lastQuoteLevel, _lastQuotePrefix, _foundOutlookQuote ) );
                }
            }

			foreach( string line in lines )
			{
                _paragraphs.Add( new Paragraph( line, ParagraphType.Fixed, 
					_lastQuoteLevel, _lastQuotePrefix, _foundOutlookQuote ) );
			}
		}

		/**
		 * Determines whether the specified array of lines is a block of plain text 
		 * (which should be displayed with no line breaks) or of formatted text (which
		 * should be displayed with line breaks.
		 */

		private PlainTextParaType IsPlainTextPara( ArrayList lines )
		{
			if ( lines.Count <= 1 )
                return PlainTextParaType.Plain;

            // If the same lines are present in a text to which we are replying, 
            // and were plain text in the original message, they're still plain text now
            if ( _origText != null )
            {
                Paragraph para = _origText.FindParagraph( (string) lines [0], (string) lines [1] );
                if ( para != null && para.Type == ParagraphType.Plain )
                    return PlainTextParaType.Plain;
            }

			int minLineLength = Int32.MaxValue;
			int maxLineLength = 0;
            bool linesEndWithSpace = true;

			// we don't take the last line into account
			for( int i = 0; i < lines.Count-1; i++ )
			{
				string line = (string) lines [i];
				Debug.Assert( line.Length > 0 );
				if ( line.Length < minLineLength )
					minLineLength = line.Length;
				if ( line.Length > maxLineLength )
					maxLineLength = line.Length;
                if ( line.Length > 0 && line [line.Length-1] != ' ' )
                {
                    linesEndWithSpace = false;
                }
            }

            if ( linesEndWithSpace )
                return PlainTextParaType.Plain;

            // If all lines are smaller that some minimum value, show as separate lines
			if ( maxLineLength < _minWrapWidth )
				return PlainTextParaType.Fixed;

            // If the lines are all long, this is probably a list of plain-text
            // paragraphs with no wrapping and no separator lines that needs to be broken
            // into separate line-long paragraphs

            int maxLineLength2 = Math.Max( maxLineLength, ((string) lines [lines.Count-1]).Length );
            if ( maxLineLength2 > _minWrapWidth*2 )
            {
                // maybe it's a table?
                bool hasSpaces = false;
                foreach( string line in lines )
                {
                    if ( line.IndexOf( "   ") >= 0 || line.IndexOf( "\t" ) >= 0 )
                    {
                        hasSpaces = true;
                        break;
                    }
                }
                if ( !hasSpaces )
                    return PlainTextParaType.Unwrapped;
            }

			/*
			 * Try to autodetect if the text was word-wrapped. If wrapping was used, 
			 * then there is a certain margin, and the words are wrapped to the next line
			 * because they exceed that margin. Thus, we add the first word of the next line
			 * to the current line and see if these lengths (unwrapped lengths) for all lines
			 * are greater than the actual wrapped length.
			 * I know that the explanation is a bit unclear...
			 */

            int minWrappedLineLength = Int32.MaxValue;
			for ( int i = 0; i < lines.Count-1; i++ )
			{
				string line = (string) lines [i];
				string nextLine = (string) lines [i+1];
				int wrappedLineLength = line.Length + 1 /* space */ + FirstWordLength( nextLine );
				if ( wrappedLineLength < minWrappedLineLength )
					minWrappedLineLength = wrappedLineLength;
			}

            if ( minWrappedLineLength > maxLineLength )
				return PlainTextParaType.Plain;

			return PlainTextParaType.Fixed;
		}

		/**
		 * Counts the starting spaces in the specified line.
		 */

		private static int CountStartingSpaces( string line )
		{
			int cnt = 0;
            while( cnt < line.Length && Char.IsWhiteSpace( line, cnt ) ) 
            {
                cnt++;
            }
			return cnt;
		}

        private static void ParseQuoting( string line, out int quoteLevel, out string quotePrefix, out string quotedText )
        {
            int spaces = CountStartingSpaces( line );
            quoteLevel = 0;
            StringBuilder quotePrefixBuilder = StringBuilderPool.Alloc();
            try 
            {
                int pos = spaces; 
                bool foundWhitespace = false;
                while( pos < line.Length )
                {
                    if ( line [pos] == '>' )
                    {
                        quoteLevel++;
                    }
                    else if ( Char.IsLetter ( line, pos ) )
                    {
                        // the letters before the first > character are the quote prefix
                        // any other letter stops the quoting
                        if ( quoteLevel > 0 || foundWhitespace )
                            break;

                        quotePrefixBuilder.Append( line [pos] );
                    }
                    else if ( !Char.IsWhiteSpace( line, pos ) )
                        break;
                    else
                        foundWhitespace = true;
                
                    pos++;
                }
                if ( quoteLevel > 0 )
                {
                    quotePrefix = quotePrefixBuilder.ToString();
                    quotedText = line.Substring( pos );
                }
                else
                {
                    quotePrefix = "";
                    quotedText = line;
                }
            }
            finally
            {
                StringBuilderPool.Dispose( quotePrefixBuilder );
            }
        }

		/**
		 * Returns the quoting level (count of > characters) for the specified line.
		 */

		public static int GetQuoteLevel( string line )
		{
            int quoteLevel;
            string quotePrefix, quotedText;
            ParseQuoting( line, out quoteLevel, out quotePrefix, out quotedText );
            return quoteLevel;
		}

		/**
		 * Strips the quote prefix from the specified line.
		 */

		public static string StripQuoting( string line )
		{
            int quoteLevel;
            string quotePrefix, quotedText;
            ParseQuoting( line, out quoteLevel, out quotePrefix, out quotedText );
            return quotedText;
		}

		/**
		 * Returns the quote prefix (the characters before the > character) for the specified line.
		 */

		public static string GetQuotePrefix( string line )
		{
            int quoteLevel;
            string quotePrefix, quotedText;
            ParseQuoting( line, out quoteLevel, out quotePrefix, out quotedText );
            return quotePrefix;
            
		}

		/**
		 * Returns the length of the first word in a line.
		 */

		private static int FirstWordLength( string line )
		{
			int startPos = 0;
			while( startPos < line.Length && Char.IsWhiteSpace( line, startPos ) )
				startPos++;
			if ( startPos == line.Length )
				return 0;

			int endPos = startPos;
            while( endPos < line.Length && !Char.IsWhiteSpace( line, endPos ) )
				endPos++;
			return endPos - startPos;
		}

		/**
		 * Checks if the specified line is the Outlook "original message" line.
		 */

		private static bool IsOutlookQuoteStart( string line )
		{
			line = line.Trim();
			if ( line.Length < 15)  // 5 dashes at start, 5 dashes at end and something in between
				return false;

			int pos = 0;
			int startDashes = 0;
			while (pos < line.Length && line [pos] == '-' )
			{
				pos++;
				startDashes++;
			}
			if ( startDashes != 5 )
				return false;

			pos = line.Length - 1;
			int endDashes = 0;
			while (pos >= 0 && line [pos] == '-')
			{
				pos--;
				endDashes++;
			}
			return (endDashes == 5);
		}

		/**
		 * Checks if non-quoted text paragraphs have already peen parsed.
		 */

		private bool HaveNonquotedTextParagraphs()
		{
			foreach( Paragraph para in _paragraphs )
			{
				if ( (para.Type == ParagraphType.Plain || para.Type == ParagraphType.Fixed ) 
					&& para.QuoteLevel == 0 )
				{
					return true;
				}
			}
			return false;
		}
	}
}
