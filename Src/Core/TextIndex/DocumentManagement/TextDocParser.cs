// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using   System;
using   System.Text;
using   System.Globalization;
using   System.Diagnostics;

namespace JetBrains.Omea.TextIndex
{
public class TextDocParser: IDocParser
{
    //-------------------------------------------------------------------------
    static TextDocParser()
    {
        InitASCIIUnicodeCategories();
    }

    protected static void InitASCIIUnicodeCategories()
    {
        for( int i = 0; i < 128; i++ )
            ASCIIUnicodeCategory[ i ] = Char.GetUnicodeCategory( (char)i );
    }

    //-------------------------------------------------------------------------
    public  TextDocParser()
    {
        iSentenceNumber = iTokenNumber = 0;
        iOffset = iEndOffset = 0;
        CurrentWord = new Word();
    }

    public void  Init( string str_ )
    {
        Next( str_ );

        iShiftOffset = 0;
        iSentenceNumber = 0;
        iTokenNumber = 0;
    }

    public void  Next( string str_ )
    {
        iOffset = iEndOffset = 0;
        CurrentWord.Init();

        //  increment by previous buffer length.
        if( strBuffer != null )
            iShiftOffset += BufferLength;

        strBuffer = str_;
        BufferLength = strBuffer.Length;
    }

    #region External Shifting
    public void  IncrementSentence()
    {
        iSentenceNumber++;
    }
    public void  IncrementOffset( int inc )
    {
        iShiftOffset += inc;
    }
    public void  FlushOffset()
    {
        iShiftOffset = 0;

        //  We expect that offsets flushing is made between to chunks of text.
        //  Thus, next call is "TextDocParser.Next", which adds "BufferLength"
        //  making "iShiftOffset" zero.
        if( strBuffer != null )
            iShiftOffset -= BufferLength;
    }
    #endregion External Shifting

    //-------------------------------------------------------------------------
    //  Extract new token from the input stream. Return EndOfStream token again
    //  if this token has already been  reported.
    //-------------------------------------------------------------------------

    public virtual Word    getNextWord()
    {
        int     NumberOfSpecials;
        CurrentWord.ClearMarks();

        bool hasUpperCaseChars = false;
        if( CurrentWord.Tag != Word.TokenType.eoEOS )
        {
            SkipWhitespace();
            if( iOffset < BufferLength )
            {
                iEndOffset = FindRightBorderOfToken( out NumberOfSpecials, out hasUpperCaseChars );
                CleanToken( ref NumberOfSpecials );

                string  token = strBuffer.Substring( iOffset, iEndOffset - iOffset + 1 );
                uint    ValidOffset = (uint)(iOffset + iShiftOffset) % MaximalOffset;
                CurrentWord.Init( token, ValidOffset, iSentenceNumber, iTokenNumber++, Word.TokenType.eoUndef );
                CurrentWord.SelfIdentifyType( NumberOfSpecials );
                iOffset += token.Length;
                iTokenNumber = (ushort)(iTokenNumber % ushort.MaxValue);
            }
            else
            {
                CurrentWord.Tag = Word.TokenType.eoEOS;
                CurrentWord.SentenceNumber = iSentenceNumber;
            }
        }

        if( hasUpperCaseChars )
        {
            CurrentWord.NormalizeCase();
        }
        return( CurrentWord );
    }

    //-------------------------------------------------------------------------
    //  Consider as white space the following symbols - blank, tab (these two
    //  conform to IsWhiteSpace method), newline, slash.
    //-------------------------------------------------------------------------

    protected void  SkipWhitespace()
    {
        while(( iOffset < BufferLength ) && isDelimiterAt( iOffset ))
        {
            //  keep trace of "sentence end" markers. We define them as the sequence -
            //  (one of sentence end punctuation symbols - '.', '!', '?')([WhiteSpace]|EOL)

            char    ch = strBuffer[ iOffset ];
            if( isSentenceEndSymbol( ch ) &&
                (( iOffset + 1 < BufferLength && Char.IsWhiteSpace( strBuffer, iOffset + 1 )) ||
                 ( iOffset + 1 == BufferLength )) )
            {
                iSentenceNumber++;
            }
            else
            if( ( iOffset >= 2 ) && ( ch == '\n' ) && ( strBuffer[ iOffset - 1 ] == '\n' ) &&
                !isSentenceEndSymbol( strBuffer[ iOffset - 2 ] ) &&  // counter has already been incremented
                ( strBuffer[ iOffset - 2 ] != '\n' ))                 // avoid unnecessary increments
            {
                iSentenceNumber++;
            }

            iOffset++;
        }
    }

    protected bool  isDelimiterAt( int i_Offset )
    {
        Debug.Assert( i_Offset < BufferLength );
        UnicodeCategory category = Char.GetUnicodeCategory( strBuffer[ i_Offset ] );
        return TextDelimitingCategories.IsCleaningCat( (int)category );
    }
    public static bool  isDelimiter( char ch )
    {
        UnicodeCategory category = Char.GetUnicodeCategory( ch );
        return TextDelimitingCategories.IsDelimiter( (int)category );
    }

    //-------------------------------------------------------------------------
    //  primary token (it is a subject of possible transformations later) is a smth
    //  delimited by the set of delimiter symbols OR is the rest of string
    //  till EOB
    //-------------------------------------------------------------------------
    protected int   FindRightBorderOfToken( out int NumberOfSpecials, out bool hasUpperCaseChars )
    {
        char            charCode;
        UnicodeCategory charCategory;

        NumberOfSpecials = 0;
        iEndOffset = iOffset;
        hasUpperCaseChars = Char.IsUpper( strBuffer, iEndOffset );
        while( ++iEndOffset < BufferLength )
        {
            charCode = strBuffer[ iEndOffset ];
            if( charCode < 128 )
            {
                charCategory = ASCIIUnicodeCategory[ charCode ];
            }
            else
            {
                charCategory = Char.GetUnicodeCategory( charCode );
            }
            if( TextDelimitingCategories.IsDelimiter( (int)charCategory ) )
            {
                break;
            }
            if( TextDelimitingCategories.IsCleaningCat( (int)charCategory ) )
            {
                NumberOfSpecials++;
            }
            hasUpperCaseChars = hasUpperCaseChars || Char.IsUpper( charCode );
        }

        return( --iEndOffset );
    }

    //-------------------------------------------------------------------------
    //  Here we define primary token-delimiting rules and exceptions:
    //  - if there is a punctuation sign at the end of the token - remove it
    //-------------------------------------------------------------------------
    protected void  CleanToken( ref int NumberOfSpecials )
    {
        Debug.Assert( iEndOffset >= iOffset, "String cleaning is impossible - empty string" );

        while( isEndsWithPunctuation() )
        {
            iEndOffset--;
            NumberOfSpecials--;
        }
    }

    #region Predicates
    //-------------------------------------------------------------------------
    //  Convert the last char from UNICODE representation to UTF8, check that
    //  it is from the ASCII range (0-127) by checking the second byte to be 0
    //  (actually, all 5 successive bytes must be 0, but we constrain this check).
    //-------------------------------------------------------------------------

    protected bool  isEndsWithPunctuation()
    {
        if( iOffset == iEndOffset )
            return( false );

        bEncodedFromUnicode[ 1 ] = 0x00;
        UTFEncoder.GetBytes( strBuffer, iEndOffset, 1, bEncodedFromUnicode, 0 );
        bool    f_ = (bEncodedFromUnicode[ 1 ] == 0x00) && aflagStrictPunctuations[ bEncodedFromUnicode[ 0 ] ];
        return( f_ ); // '!' || '?' || '.' || ';' || ':' || ',' || ')' || ']' || '>' || '|'
    }

    protected bool  isSentenceEndSymbol( char ch_ )
    {
        return(( ch_ == '.' ) || ( ch_ == '!' ) || ( ch_ == '?' ));
    }
    #endregion

    //-------------------------------------------------------------------------
    #region Attributes
    protected  const   int      ciMaxBytesNumberForUTF9Conversion = 6;
    protected  const   int      MaximalOffset = 16777215; //2^24 - 1;

    protected  string           strBuffer;
    protected  int              BufferLength;
    protected  int              iOffset, iEndOffset, iShiftOffset;
    protected  uint             iSentenceNumber;
    protected  ushort           iTokenNumber;
    protected  Word             CurrentWord;
    protected  UTF8Encoding     UTFEncoder = new UTF8Encoding();
    protected  byte[]           bEncodedFromUnicode = new byte[ ciMaxBytesNumberForUTF9Conversion ];

    protected  static UnicodeCategory[]    ASCIIUnicodeCategory = new UnicodeCategory[ 128 ];
    public     static bool[]    aflagStrictPunctuations = {
    //  NUL     SOH     STX     ETX     EOT     ENQ     ACQ     BEL     BS      TAB     LF      VT      FF      CR      SO      SI
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,
    //  DLE     DC1     DC2     DC3     DC4     NAK     SYN     ETB     CAN     EM      SUB     ESC     FS      GS      RS      US
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,
    //  ' '     '!'     '"'     '#'     '$'     '%'     '&'     '''     '('     ')'     '*'     '+'     ','     '-'     '.'     '/'
        false,  true,   true,   false,  false,  false,  false,  true,   false,  true,   false,  false,  true,   false,  true,   true,
    //  '0'     '1'     '2'     '3'     '4'     '5'     '6'     '7'     '8'     '9'     ':'     ';'     '<'     '='     '>'     '?'
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true,   true,   false,  false,  true,   true,
    //  '@'     'A'     'B'     'C'     'D'     'E'     'F'     'G'     'H'     'I'     'J'     'K'     'L'     'M'     'N'     'O'
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,
    //  'P'     'Q'     'R'     'S'     'T'     'U'     'V'     'W'     'X'     'Y'     'Z'     '['     '\'     ']'     '^'     '_'
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true,   true,   false,
    //  '`'     'a'     'b'     'c'     'd'     'e'     'f'     'g'     'h'     'i'     'j'     'k'     'l'     'm'     'n'     'o'
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,
    //  'p'     'q'     'r'     's'     't'     'u'     'v'     'w'     'x'     'y'     'z'     '{'     '|'     '}'     '~'     0x7F
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true,   true,   true,  false,

        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,
        false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false };
    #endregion
}
}
