/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;
using System.Collections;

namespace JetBrains.Omea.InstantMessaging.Trillian
{
    /**
     * Class responsible for importing a Trillian log file.
     */

    public class TrillianLog
    {
        private string _fileName;
        private StreamReader _reader;
        private bool _eof = false;
        private DateTime _curDate;         // Date of the last message. Since the full date is only
                                           // found in Session Start and Session Close markers, we need
                                           // to store it between messages.

        private string _curCorrespondent;  // The correspondent may have been renamed between sessions,
                                           // so in order to distinguish incoming and outgoing messages
                                           // correctly, we need to extract the current name from the
                                           // session start message. (If a correspondent is renamed during
                                           // a session, it is not reflected in the log until the session ends.)
        private string _nextLine;
        private ArrayList _messageBuffer = new ArrayList();

        private static Regex _sessionStartRx = new Regex( @"^Session Start \(.+ - [^:]+:([^\)]+)\): (.+)$" );
        private static Regex _messageStartRx = new Regex( @"^\[(\d\d):(\d\d)\] ([^:]+): (.*)$" );
        private static Regex _serviceMessageRx = new Regex( @"^\[\d\d:\d\d\] \*\*\* " );
        private static Regex _offlineMessageRx = new Regex( @"\[Offline Message \([^ ]+ \[(\d{1,2}):(\d\d)\]\)\]$" );
        private static Regex _linkRx = new Regex( @"\(Link: ([^)]+)\)\1" );

        internal TrillianLog( string fileName )
        {
            _fileName = fileName;    		
        }

        /**
         * Returns the filename-only part of the log.
         */
 
        public string GetName()
        {
        	return Path.GetFileNameWithoutExtension( _fileName );
        }

        /**
         * Returns the size of the log.
         */

        public int Size
        {
        	get 
            { 
                return (int) new FileInfo( _fileName ).Length;
            }
        }

        public string CurCorrespondentName
        {
            get { return _curCorrespondent; }
        }

        /**
         * Seeks to the specified offset in the log file.
         */

        public void Seek( int offset )
        {
        	CheckCreateReader();
            _reader.BaseStream.Seek( offset, SeekOrigin.Begin );
        }

        /**
         * Reads and returns the next message from the Trillian log. Returns
         * null if reached end of file.
         */

    	public TrillianLogMessage ReadNextMessage()
        {
            CheckCreateReader();
            if ( _eof )
                return null;

            TrillianLogMessage curMessage = null;
            while( true )
            {
                DateTime offlineMessageDate = DateTime.MinValue;
                string line = ReadNextLine();
            	if ( line == null )
            	{
            		_eof = true;
                    break;
            	}

                Match m = _sessionStartRx.Match( line );
                if ( m.Success )
                {
                    _curCorrespondent = m.Groups [1].Value;
                    try
                    {
                        _curDate = DateTime.ParseExact( m.Groups [2].Value, "ddd MMM dd HH:mm:ss yyyy", 
                            CultureInfo.InvariantCulture );
                    }
                    catch( Exception )
                    {
                    	Trace.WriteLine( "Failed to parse Trillian date: " + m.Groups [2].Value );
                    }
                    continue;
                }

                if ( line.StartsWith( "Session Close (" ) || _serviceMessageRx.IsMatch( line ) )
                {
                	// Marks the end of a session or a service message (contact logon/logoff). 
                    // If we have a message, return it; otherwise, continue parsing
                    if ( curMessage != null )
                        break;

                    continue;
                }

                m = _offlineMessageRx.Match( line );
                if ( m.Success )
                {
                    // FIXME: extract date from offline message
                    offlineMessageDate = ParseDate( _curDate, m.Groups [1].Value, m.Groups [2].Value );
                    line = line.Substring( 0, m.Index-1 );
                }

                line = _linkRx.Replace( line, "$1" );

                m = _messageStartRx.Match( line );
                if ( m.Success )
                {
                	// The line is the beginning of a new message following the current one.
                    // Save it to the buffer and return the message.
                    if ( curMessage != null )
                	{
                		_nextLine = line;
                        break;
                	}

                    string text = m.Groups [4].Value;
                    DateTime dt = ParseDate( _curDate, m.Groups [1].Value, m.Groups [2].Value );
                    string nick = m.Groups [3].Value;

                	curMessage = new TrillianLogMessage( dt, ( nick == _curCorrespondent ), text );
                    _curDate = dt;
                }
                else
                {
                    // the line is a continuation of the current message
                    if ( curMessage != null )
                    {
                    	curMessage.AddLine( line );
                    }
                }

                if ( curMessage != null && offlineMessageDate != DateTime.MinValue )
                {
                	curMessage.Time = offlineMessageDate;
                }
            }
            return curMessage;
        }

        /**
         * If the reader has not been initialized, creates it.
         */

        private void CheckCreateReader()
        {
            if ( !_eof )
            {
            	if ( _reader == null )
            	{
            		try
            		{
            			// Trillian 2.0 writes the encoding marker in the first 3 bytes
                        // of the file, so setting detectEncodingFromByteOrderMarks ensures
                        // that the encoding is detected correctly. Note that for mixed logs,
                        // where the beginning part was written by Trillian 1.0 or earlier
                        // and the end by Trillian 2.0, this will cause the entire log to be
                        // read as UTF8, so the Trillian 1.0 part will be read as garbage
                        // if it uses international characters. Maybe some smarter detection
                        // is possible...
                        _reader = new StreamReader( _fileName, Encoding.Default, true );
            		}
                    catch( Exception )
                    {
                    	_eof = true;
                        return;
                    }
            	}
            }
        }

        /**
         * Returns the buffered line or reads the next line from the stream.
         */

        private string ReadNextLine()
        {
            if ( _nextLine != null )
        	{
                string result = _nextLine;
        		_nextLine = null;
                return result;
        	}

            return _reader.ReadLine();
        }

        /**
         * Parses the date from the specified base date, hour and minute values.
         */

        private DateTime ParseDate( DateTime baseDate, string hours, string minutes )
        {
            DateTime dt = baseDate.Date;
            dt = dt.AddHours( Int32.Parse( hours ) );
            dt = dt.AddMinutes( Int32.Parse( minutes ) );
            if ( dt < baseDate )  // the conversation continues past midnight
            {
                dt = dt.AddDays( 1 );
            }
            return dt;
        }
    }

    /**
     * A single message in a Trillian log.
     */

    public class TrillianLogMessage
    {
    	private DateTime _dateTime;
        private bool     _incoming;
        private string   _text;

    	internal TrillianLogMessage( DateTime dateTime, bool incoming, string text )
    	{
    		_dateTime = dateTime;
    		_incoming = incoming;
    		_text = text;
    	}

        internal void AddLine( string line )
        {
        	_text = _text + "\n" + line;
        }

    	public DateTime Time
    	{
    		get { return _dateTime; }
            set { _dateTime = value; }
    	}
        
    	public bool Incoming
    	{
    		get { return _incoming; }
    	}

    	public string Text
    	{
    		get { return _text; }
    	}
    }
}
