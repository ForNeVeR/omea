// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using System.Text;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.RTF
{
    internal class State : ICloneable
    {
        public bool skipContent = false;
        public Encoding encoding = Encoding.Default;
        #region ICloneable Members

        public object Clone()
        {
            State state = new State();
            state.skipContent = skipContent;
            state.encoding = encoding;
            return state;
        }
        #endregion
    }
    public struct FontInfo
    {
        public int FontNum;
        public object EncodingInfo;
        public FontInfo( int fontNum, object encodingInfo )
        {
            FontNum = fontNum;
            EncodingInfo = encodingInfo;
        }
    }

    internal class EncodingManager
    {
        private HashMap _encoders = new HashMap( 10 );

        public EncodingManager()
        {
            //first column: charset, second codepage
            _encoders.Add( 128, 932 ); // Japanese SHIFTJIS_CHARSET
            _encoders.Add( 129, 949 ); // Korean HANGEUL_CHARSET
            _encoders.Add( 130, 1361 ); // Johab JOHAB_CHARSET
            _encoders.Add( 134, 936 ); // GB2312_CHARSET
            _encoders.Add( 136, 950 ); // Taiwan CHINESEBIG5_CHARSET
            _encoders.Add( 161, 1253 ); // GREEK_CHARSET
            _encoders.Add( 162, 1254 ); // TURKISH_CHARSET
            _encoders.Add( 177, 1255 ); // HEBREW_CHARSET
            _encoders.Add( 178, 1256 ); // ARABIC_CHARSET
            _encoders.Add( 179, 1256 ); // ARABICTRADITIONAL_CHARSET
            _encoders.Add( 180, 1256 ); // ARABICUSER_CHARSET
            _encoders.Add( 181, 1255 ); // HEBREWUSER_CHARSET
            _encoders.Add( 186, 1257 ); // BALTIC_CHARSET
            _encoders.Add( 204, 1251 ); // RUSSIAN_CHARSET
            _encoders.Add( 222, 874 ); // THAI_CHARSET
            _encoders.Add( 238, 1250 ); // EASTEUROPE_CHARSET
            _encoders.Add( 254, 437 ); // PC437_CHARSET
            _encoders.Add( 255, 437 ); // OEM_CHARSET
        }
        public Encoding GetEncoding( int charset )
        {
            HashMap.Entry entry = _encoders.GetEntry( charset );
            if ( entry == null ) return null;
            Encoding encoding = entry.Value as Encoding;
            if ( encoding == null )
            {
                try
                {
                    encoding = Encoding.GetEncoding( (int)entry.Value );
                }
                catch
                {
                    encoding = null;
                }
                entry.Value = encoding;
            }
            return encoding;
        }
    }

    public class RTFParser
    {
        private int _codePage = -1;
        private int _currentPosition = -1;
        private int _groupDeepCount = 0;
        HashSet _expectedParameter = new HashSet();
        private int _parameter = -1;
        private int _fontNum = -1;

        private StreamReader _reader;
        private Stack _states = new Stack();
        private State _curState;
        private StringBuilder _plainText = new StringBuilder();
        private Encoding _defaultEncoding = Encoding.Default;
        HashSet _skipControlWords = new HashSet( 16 );
        EncodingManager _manager = new EncodingManager();
        HashMap _fonts = new HashMap();
        HashSet _defaultFonts = new HashSet();
        private bool _fontTableProcessed = false;
        private const int HEX_BUFFER_SIZE = 4096;

        public RTFParser( )
        {
            _expectedParameter.Add( "ansicpg" );
            _expectedParameter.Add( "bin" );
            _expectedParameter.Add( "f" );
            _expectedParameter.Add( "fcharset" );

            _skipControlWords.Add( "fonttbl");
            _skipControlWords.Add( "author");
            _skipControlWords.Add( "buptim");
            _skipControlWords.Add( "colortbl");
            _skipControlWords.Add( "comment");
            _skipControlWords.Add( "creatim");
            _skipControlWords.Add( "doccomm");
            _skipControlWords.Add( "footer");
            _skipControlWords.Add( "footerf");
            _skipControlWords.Add( "footerl");
            _skipControlWords.Add( "footerr");
            _skipControlWords.Add( "footnote");
            _skipControlWords.Add( "ftncn");
            _skipControlWords.Add( "ftnsep");
            _skipControlWords.Add( "ftnsepc");
            _skipControlWords.Add( "header");
            _skipControlWords.Add( "headerf");
            _skipControlWords.Add( "headerl");
            _skipControlWords.Add( "headerr");
            _skipControlWords.Add( "info");
            _skipControlWords.Add( "keywords");
            _skipControlWords.Add( "operator");
            _skipControlWords.Add( "pict");
            _skipControlWords.Add( "printim");
            _skipControlWords.Add( "private");
            _skipControlWords.Add( "revtim");
            _skipControlWords.Add( "rxe");
            _skipControlWords.Add( "stylesheet");
            _skipControlWords.Add( "subject");
            _skipControlWords.Add( "tc");
            _skipControlWords.Add( "title");
            _skipControlWords.Add( "txe");
            _skipControlWords.Add( "xe");
            _defaultFonts.Add( "fnil" );
            _defaultFonts.Add( "froman" );
            _defaultFonts.Add( "fswiss" );
            _defaultFonts.Add( "fmodern" );
            _defaultFonts.Add( "fscript" );
            _defaultFonts.Add( "fdecor" );
            _defaultFonts.Add( "ftech" );
            _defaultFonts.Add( "fbidi" );
            Init();
        }

        private void Init()
        {
            _codePage = -1;
            _curState = new State();
            _states.Clear();
            _plainText.Length = 0;
            if( _plainText.Capacity > 16384 )
            {
                _plainText.Capacity = 1024;
            }
            _currentPosition = -1;
            _groupDeepCount = 0;
            _fonts.Clear();
            _parameter = -1;
            _fontNum = -1;
            _fontTableProcessed = false;
        }

        public int DefaultCodePage { get { return _codePage; } }
        public int CurrentPosition { get { return _currentPosition; } }
        private State State { get { return _curState; } }
        public string PlainText { get { return _plainText.ToString(); } }
        public FontInfo[] GetFontTableInfo()
        {
            FontInfo[] fontInfos = new FontInfo[ _fonts.Count ];
            int index = -1;
            foreach ( HashMap.Entry entry in _fonts )
            {
                fontInfos[ ++index ] = new FontInfo( (int)entry.Key, entry.Value );
            }
            return fontInfos;
        }

        private void OpenGroup()
        {
            _states.Push( _curState );
            _curState = (State)_curState.Clone();
            ++_groupDeepCount;
        }
        private void CloseGroup()
        {
            if ( --_groupDeepCount < 0 )
            {
                _groupDeepCount = 0;
                return;
                //throw new ParenthesisMismatching( "There is parenthesis mismatching: no open parenthesis.", _currentPosition );
            }
            _curState = (State)_states.Pop();
            if ( _collectFonts && _fonttblGroup > _groupDeepCount )
            {
                StopCollectFonts();
            }
        }

        private void AddFont( int fontNum )
        {
            _fontNum = fontNum;
        }
        private void AddDefaultFont( )
        {
            if ( _fontNum == -1 ) return;
            if ( _fonts.Contains( _fontNum ) )
            {
                return;
            }
            _fonts.Add( _fontNum, _defaultEncoding );
        }

        private void AddCharset( int charset )
        {
            if ( _fontNum == -1 ) return;
            Encoding enc = _manager.GetEncoding( charset );
            if ( enc == null )
            {
                enc = _defaultEncoding;
            }
            _fonts.Add( _fontNum, enc );
        }

        private void SetCurrentFont( int fontNum )
        {
            HashMap.Entry entry = _fonts.GetEntry( fontNum );
            if ( entry == null )
            {
                _fonts.Add( fontNum, _defaultEncoding );
                //throw new FontMismatching( "Cannot set current font. Such fontNum '" + fontNum +
                //    "' was not included in \\fonttbl tag.", _currentPosition );
            }

            if ( entry != null && entry.Value != null )
            {
                _curState.encoding = (Encoding)entry.Value;
            }
            else
            {
                _curState.encoding = _defaultEncoding;
            }
        }

        private void Append( string text )
        {
            if ( !State.skipContent )
            {
                _plainText.Append( text );
            }
        }
        private void Append( char ch )
        {
            if ( !State.skipContent )
            {
                _plainText.Append( ch );
            }
        }

        private void TranslateControlWord( string controlWord )
        {
            if ( controlWord == "f" )
            {
                if ( _collectFonts )
                {
                    AddFont( _parameter );
                    return;
                }
                else if ( _fontTableProcessed )
                {
                    SetCurrentFont( _parameter );
                    return;
                }
            }
            if ( _collectFonts && controlWord == "fcharset" )
            {
                AddCharset( _parameter );
                return;
            }
            if ( _collectFonts && _defaultFonts.Contains( controlWord ) )
            {
                AddDefaultFont();
                return;
            }
            if ( controlWord == "fonttbl" )
            {
                StartCollectFonts();
            }

            if ( State.skipContent || _skipControlWords.Contains( controlWord ) )
            {
                State.skipContent = true;
                return;
            }
            if ( controlWord == "par" )
            {
                Append( "\r\n" );
                return;
            }
            if ( controlWord == "line" )
            {
                Append( "\r\n" );
                return;
            }
            if ( controlWord == "tab" )
            {
                Append( '\t' );
                return;
            }
            if ( controlWord == "rquote" || controlWord == "lquote" )
            {
                Append( '\'' );
                return;
            }
            if ( controlWord == "ldblquote" || controlWord == "rdblquote" )
            {
                Append( '"' );
                return;
            }
            if ( controlWord == "bin" )
            {
                ReadBin();
                return;
            }
            if ( controlWord == "ansicpg" )
            {
                SetCodePage();
                return;
            }
        }
        private bool _collectFonts = false;
        private int _fonttblGroup = -1;

        private void StartCollectFonts()
        {
            _collectFonts = true;
            _fonttblGroup = _groupDeepCount;
        }
        private void StopCollectFonts()
        {
            _collectFonts = false;
            _fontTableProcessed = true;
        }

        private void SetCodePage()
        {
            _codePage = _parameter;
            try
            {
                _defaultEncoding = Encoding.GetEncoding( _codePage );
            }
            catch
            {
                _defaultEncoding = Encoding.Default;
            }
            _curState.encoding = _defaultEncoding;
        }
        private void TranslateControlSymbol( int controlSymbol )
        {
            switch ( controlSymbol )
            {
                case 0xa:
                    Append( '\n' );
                    break;
                case 0xd:
                    Append( '\r' );
                    break;
                case '{':
                    Append( '{' );
                    break;
                case '}':
                    Append( '}' );
                    break;
                case '\\':
                    Append( '\\' );
                    break;
                case '*':
                    State.skipContent = true;
                    break;
                case '\'':
                    ReadHex();
                    break;
            }
        }

        private int Read()
        {
            ++_currentPosition;
            return _reader.Read();
        }

        private void ReadBin()
        {
            for ( int cb = 0; cb < _parameter; ++cb )
            {
                Read();
            }
        }

        private byte[] _bytes = new byte[HEX_BUFFER_SIZE];
        private int _bytesIndex = 0;

        private void ReadHexImpl( byte[] buffer, bool check )
        {
            for ( bool even = true;; even = !even  )
            {
                int readCh = _reader.Peek();
                if ( readCh == -1 )
                {
                    break;
                }
                byte b;
                readCh = Char.ToLower( (char)readCh );
                if ( '0' <= (char)readCh && '9' >= (char)readCh )
                {
                    b = (byte)(readCh - '0');
                }
                else if ( 'a' <= (char)readCh && 'f' >= (char)readCh )
                {
                    b = (byte)(readCh - 'a' + 10);
                }
                else
                {
                    break;
                }
                if ( _bytesIndex < HEX_BUFFER_SIZE || !check )
                {
                    buffer[_bytesIndex] = even ? (byte)( b << 4 ) : (byte)(buffer[_bytesIndex] + b);
                }

                if ( !even )
                {
                    ++_bytesIndex;
                }

                Read();
                if ( !even )
                {
                    break;
                }
            }
        }

        private void ReadHex()
        {
            ReadHex( _bytes, true );
        }

        private void ReadHex( byte[] buffer, bool check )
        {
            _bytesIndex = 0;
            int curPosition = _currentPosition;
            ReadHexImpl( buffer, check );
            if ( _reader.Peek() == '\\' )
            {
                while ( (char)_reader.Peek() == '\\' )
                {
                    Read();
                    if ( (char)_reader.Peek() == '\'' )
                    {
                        Read();
                        ReadHexImpl( buffer, check );
                    }
                    else
                    {
                        _reader.DiscardBufferedData();
                        _reader.BaseStream.Seek( _currentPosition--, SeekOrigin.Begin );
                        break;
                    }
                }
            }
            if ( _bytesIndex == 0 )
            {
                return;
            }

            if ( _bytesIndex < HEX_BUFFER_SIZE || !check )
            {
                Append( _curState.encoding.GetString( buffer, 0, _bytesIndex ) );
            }
            else
            {
                _reader.DiscardBufferedData();
                _currentPosition = curPosition;
                _reader.BaseStream.Seek( _currentPosition + 1, SeekOrigin.Begin );
                ReadHex( new byte[_bytesIndex], false );
            }
        }

        private void ReadControlWord()
        {
            if ( !Char.IsLetter( (char)_reader.Peek() ) ) //is there control symbol?
            {
                TranslateControlSymbol( Read() );
                return;
            }

            string ctrlWord = null;
            for (;;)
            {
                int readCh = _reader.Peek();
                if ( readCh == -1 || !Char.IsLetter( (char)readCh ) )
                {
                    break;
                }
                ctrlWord += (char)readCh;
                Read();
            }

            string param = null;
            if ( _reader.Peek() == '-' ) //parameter can be negative
            {
                param += Read();
            }

            for (;;)
            {
                int readCh = _reader.Peek();
                if ( readCh == -1 || !Char.IsDigit( (char)readCh ) )
                {
                    if ( readCh == ' ' )
                    {
                        Read();
                    }
                    break;
                }
                param += (char)readCh;
                Read();
            }
            if ( param != null )
            {
                if ( _expectedParameter.Contains( ctrlWord ) )
                {
                    _parameter = Int32.Parse( param );
                }
            }
            else
            {
                CheckParameter( ctrlWord );
            }
            TranslateControlWord( ctrlWord );
        }

        private void CheckParameter( string ctrlWord )
        {
            if ( _expectedParameter.Contains( ctrlWord ) )
            {
                throw new NoExpectedParameter( "\\" + ctrlWord + " has no expected parameter", _currentPosition );
            }
        }

        public string Parse( string rtf )
        {
            Guard.NullArgument( rtf, "rtf" );
            return Parse( new StreamReader( new JetMemoryStream( Encoding.Default.GetBytes( rtf ), true ) ) );
        }
        public string Parse( StreamReader reader )
        {
            Guard.NullArgument( reader, "reader" );
            _reader = reader;
            Init();

            int count = 0;
            int readChar;
            while ( (readChar = Read()) != -1 )
            {
                switch ( readChar )
                {
                    case '{':
                        OpenGroup();
                        break;
                    case '}':
                        CloseGroup();
                        break;
                    case '\\':
                        ReadControlWord();
                        break;
                    case 0x0d: //skip
                        break;
                    case 0x0a: //skip
                        break;
                    default:
                        if ( !State.skipContent )
                        {
                            Append( (char)readChar );
                        }
                        break;
                }
                count++;
            }
            if ( _groupDeepCount != 0 )
            {
                //throw new ParenthesisMismatching( "There is parenthesis mismatching: no closing parenthesis.", _currentPosition );
            }
            return _plainText.ToString();
        }
    }
    public class RtfParserException : Exception
    {
        public RtfParserException( string message, int currentPosition ) : base ( message + " Stopped at " + currentPosition ){}
    }
    public class ParenthesisMismatching : RtfParserException
    {
        public ParenthesisMismatching( string message, int currentPosition ) : base ( message, currentPosition ){}
    }
    public class FontMismatching : RtfParserException
    {
        public FontMismatching( string message, int currentPosition ) : base ( message, currentPosition ){}
    }
    public class NoExpectedParameter : RtfParserException
    {
        public NoExpectedParameter( string message, int currentPosition ) : base ( message, currentPosition ){}
    }
}
