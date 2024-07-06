// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Web;

namespace JetBrains.Omea.RemoteControl
{
	internal class CallRequest
	{
		internal class RequestException : Exception
		{
			private int _code;

			public RequestException( int code, string message )
				: base( message ) { _code = code; }

			public int Code { get { return _code; } }
		}

		private const string _contentType = "application/x-www-form-urlencoded";
		private static char[] _spaces;

		private string _verb = null;
		private string _url = null;
		private string _ver = null;
		private Hashtable _headers = null;
		private Hashtable _params = null;

		internal string Version { get { return _ver; } }

		internal CallRequest()
		{
			_spaces = new char[] { ' ', '\t' };
			_headers = new Hashtable();
			_params = new Hashtable();
		}

		internal void Read(Stream s)
		{
			TextReader tr = new StreamReader( s, Encoding.UTF8 );
			string str;
			int pos;

			// Read head
			str = tr.ReadLine();
			if( null == str )
				throw new RequestException(400, "No data");
			str = str.Trim( _spaces );

			// Parse head

			// Extract
			pos = str.IndexOf( ' ' );
			if( pos < 0 )
				throw new RequestException(400, "No Verb in first line");
			_verb = str.Substring( 0, pos );

			// Trim
			str = str.Remove( 0, pos ).TrimStart( _spaces );

			// Extract URL
			pos = str.LastIndexOf( ' ' );
			if( pos < 0 )
				throw new RequestException(400, "No URL in first line");
			_url = str.Substring( 0, pos );

			// Trim
			str = str.Remove( 0, pos ).TrimStart( _spaces );

			_ver = str;

			// Ok, read all headers.
			string lastVar = "";
			while( true )
			{
				string var, val;

				str = tr.ReadLine();
				if( null == str )
					throw new RequestException(400, "Headers are not complete");
				// Empty line after header?
				if( str == "" )
					break;

				// Check for continuation
				int i;
				for( i = 0; i < _spaces.Length; ++i )
				{
					if( _spaces[i] == str[0] )
						break;
				}
				if( i < _spaces.Length )
				{
					if( "" == lastVar )
						throw new RequestException(400, "Headers are invalid");
					str.Trim( _spaces );
					val = _headers[lastVar] as string;
					val += " " + str;
					_headers[lastVar] = val;
				}
				else
				{
					pos = str.IndexOf( ':' );
					var = str.Substring( 0, pos  ).Trim( _spaces );
					val = str.Substring( pos + 1 ).Trim( _spaces );
					_headers.Add( var, val );
					lastVar = var;
				}
            }

			// We should not getr body if here are not POST request.
			if( _verb != "POST" )
				throw new RequestException(405, "HTTP Verb '" + _verb + " 'is not supported");

			// Ok, read body
			if( !_headers.ContainsKey( "Content-Length" ) )
				throw new RequestException(411, "Need Content-Length");

			int cl;
			try
			{
				cl = Int32.Parse( _headers["Content-Length"] as string );
			}
			catch
			{
				throw new RequestException(411, "Invalid Content-Length '" + ( _headers["Content-Length"] as string ) + "'");
			}
			if( 0 == cl )
			{
				str = null;
			}
			else
			{
				char[] a = new char[ cl ];
				int r = 0;
				int off = 0;
				while( off != cl && 0 != ( r = tr.Read( a, off, cl - off ) ) )
				{
					off += r;
				}
				if( off != cl )
				{
					throw new RequestException(400, "Body is too small");
				}
				str = new string(a);
			}

			// Check request header for different conditions
			if( !_url.StartsWith( "/" ) )
				throw new RequestException(404, "Relative URLs are not supported");
			if( _ver != "HTTP/1.1" && _ver != "HTTP/1.0" )
				throw new RequestException(505, "Protocol version '" + _ver + "'is not supported");
			if( !_headers.ContainsKey( "Content-Type" ) )
				throw new RequestException(415, "Content-Type is not found");
			if( _headers["Content-Type"] as string != _contentType )
				throw new RequestException(415, "Content-Type '" + ( _headers["Content-Type"] as string ) + "' is not supported");

			// All checks was passed, parse body
			// Everything is Ok with empty body
			if(str == null)
				return;

			str = str.Trim( _spaces );
			if( str == "" )
				return;

			string[] pairs = str.Split( '&' );
			foreach ( string pair in pairs )
			{
				string var, val;
				pos = pair.IndexOf( '=' );
				if(pos < 0)
				{
					var = pair;
					val = "";
				}
				else
				{
					var = pair.Substring( 0, pos );
					val = pair.Substring( pos + 1 );
				}
				var = HttpUtility.UrlDecode( var );
				val = HttpUtility.UrlDecode( val );
				_params.Add( var, val );
			}
		}

		internal string URL { get { return _url; } }
		internal Hashtable Headers { get { return _headers; } }
		internal Hashtable Parameters { get { return _params; } }
	}
}
