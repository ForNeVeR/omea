// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.TextIndex
{
//-----------------------------------------------------------------------------
//  Class Word is the basic container for all token-dependent information
//-----------------------------------------------------------------------------

    public class Word
    {
        //-------------------------------------------------------------------------
        public enum  TokenType  { eoUndef, eoString, eoNumber, eoUnusable, eoWebAddress, eoEOS  }
        public enum  TokenPlace { eoUndef, eoTitle, eoBody, eoReference, eoSubject  }

        //-------------------------------------------------------------------------
        public  Word()  {   Init( "", 0, 0, 0, TokenType.eoUndef );    }

        public void  Init() {  Init( "", 0, 0, 0, TokenType.eoUndef );  }
        public void  Init( string str, uint start, uint i_Sentence, ushort order, TokenType type )
        {
            Token = str;
            iStartPos = start;
            iSentenceNumber = i_Sentence % MaximalSentenceNumber;
            usTokenOrder = order;
            eoType = type;
            termId = -1;
        }

        public void  NormalizeCase()
        {
            if( strToken != null )
            {
                strToken = strToken.ToLower();
                //  do not recalculate hash code for the token since our
                //  hash function is case insensitive and computes its value
                //  for lowercase representation by default
            }
        }

        public void  SelfIdentifyType( int NumberOfSpecialSymbols )
        {
            int TokenLength = strToken.Length;
            if(( TokenLength > ciMaxUsableTokenLength ) ||
                ( NumberOfSpecialSymbols > 1 ))
                eoType = TokenType.eoUnusable;
            else
            if( TokenLength >= 4 )
            {
                if( strToken.StartsWith( "www." ) )
                    eoType = TokenType.eoWebAddress;
                else
                if( TokenLength >= 7 && strToken.StartsWith( "http://" ) )
                {
                    eoType = TokenType.eoWebAddress;
                    strToken = strToken.Remove( 0, 7 );
                    termId = -1;
                }
            }
            else
            {
                //  This is an AWFUL method for checking for the particular types
                //  but (!) it IS faster than checking by "Convert" or "ToInt"
                //   parsing.
                for ( int i = 0; i < TokenLength; i++ )
                {
                    char    ch = strToken[ i ];
                    if(( ch != '.' ) && !Char.IsDigit( ch ) )
                        return;
                }
                eoType = TokenType.eoNumber;
            }
        }

        //-------------------------------------------------------------------------
        //  Grammatical hacking - store the information on wordform changes in the
        //  highest bits of the starting offsets
        //-------------------------------------------------------------------------

        #region Grammatical Masks
        // clear only grammatical bits, leave section bits unchanged
        public void  ClearMarks()
        {
            iStartPos &= 0x1CFFFFFF;
        }
        public void  MarkSection( int sectionId )
        {
        }
        public void  MarkPlural()
        {
            Debug.Assert((iStartPos & 0x80000000) == 0, "Can not mark plural twice" );
            Debug.Assert((iStartPos & 0x40000000) == 0, "Can not mark plural and proper past simultaneously" );
            Debug.Assert((iStartPos & 0x20000000) == 0, "Can not mark plural and Continuous simultaneously" );
            iStartPos |= 0x80000000; // 1<<32
        }
        public void  MarkProperPast()
        {
            Debug.Assert((iStartPos & 0x40000000) == 0, "Can not mark proper past twice" );
            Debug.Assert((iStartPos & 0x80000000) == 0, "Can not assign plural and proper past simultaneously" );
            iStartPos |= 0x40000000; // 1<<31
        }
        public void  MarkContinuous()
        {
            Debug.Assert((iStartPos & 0x20000000) == 0, "Can not mark Continuous twice" );
            Debug.Assert((iStartPos & 0x40000000) == 0, "Can not assign Continuous and proper past simultaneously" );
            Debug.Assert((iStartPos & 0x80000000) == 0, "Can not assign plural and Continuous simultaneously" );
            iStartPos |= 0x20000000; // 1<<30
        }
        public void  MarkWordformVariant( int var )
        {
            Debug.Assert( var < 16, "Amount of wordform variants exceed storable amount in the index" );
            Debug.Assert((iStartPos & 0x20000000) == 0, "Can not assign WordformVar and Continuous simultaneously" );
            Debug.Assert((iStartPos & 0x40000000) == 0, "Can not assign WordformVar and proper past simultaneously" );
            Debug.Assert((iStartPos & 0x80000000) == 0, "Can not assign plural and WordformVar simultaneously" );

            if( (var & 0x00000010) > 0 )
                iStartPos |= 0x80000000; // 1<<32
            if( (var & 0x00000008) > 0 )
                iStartPos |= 0x40000000; // 1<<31
            if( (var & 0x00000004) > 0 )
                iStartPos |= 0x20000000; // 1<<30
            if( (var & 0x00000002) > 0 )
                iStartPos |= 0x02000000; // 1<<26
            if( (var & 0x00000001) > 0 )
                iStartPos |= 0x01000000; // 1<<25
        }
        #endregion Grammatical Masks

        #region Trie Support
        public static int GetTermId( string token )
        {
            if( _tokenTrie == null )
            {
                _tokenTrie = new ExternalTrie( OMEnv.TokenTreeFileName, OMEnv.CachingStrategy );
                _tokenTrie.NodesCacheSize = 4095;
            }
            int index;
            try
            {
                _tokenTrie.AddString( token, out index );
            }
            catch( ArgumentOutOfRangeException e ) // bad index, can't seek in file
            {
                throw new FormatException( "Token trie is corrupted. " + e.Message );
            }
            catch( IOException e ) // other IO problems
            {
                throw new FormatException( "Can't operate token trie. " + e.Message );
            }
            return index;
        }

        public static int GetTokenIndex( string token )
        {
            return _tokenTrie.GetStringIndex( token );
        }

        public static string GetTokensById( int id )
        {
            return _tokenTrie.GetStringByIndex( id );
        }

        public static ArrayList GetTokensByWildcard( string wildcard )
        {
            return _tokenTrie.GetMatchingStrings( wildcard, true );
        }

        public static void FlushTermTrie()
        {
            //  Null is possible at least in tests, where we check the
            //  behavior of FullTextIndex component on empty resources.
            if( _tokenTrie != null )
            {
                _tokenTrie.Flush();
            }
        }

        public static void DisposeTermTrie()
        {
            //  Null is possible at least in tests, where we check the
            //  behavior of FullTextIndex component on empty resources.
            if( _tokenTrie != null )
            {
                _tokenTrie.Dispose();
            }
            _tokenTrie = null;
        }
        #endregion Trie Support

        #region  Accessors
        public  string      Token
        {
            get {  return strToken;  }
            set {  strToken = value; }
        }
        public  TokenType   Tag             {  get{ return eoType;   }      set{ eoType = value;   }  }
        public  uint        SentenceNumber  {  get{ return iSentenceNumber; } set{ iSentenceNumber = value; } }
        public  ushort      TokenOrder      {  get{ return usTokenOrder; }  set{ usTokenOrder = value; } }
        public  uint        StartOffset     {  get{ return iStartPos;       } }
        public  void        SetId()         { termId = GetTermId( strToken ); }
        public  int         HC              {  get{ Debug.Assert( termId != -1 ); return termId;   }     }
        public  uint        SectionId
        {
            get { return( (iStartPos & 0x1CFFFFFF) >> 26 );  }

            set
            {
                Debug.Assert( value <= 7, "Section ID exceeds maximal number 7" );

                uint mask = ((value > 7)? 0:value) << 26; //  set bits 28, 27, 26
                iStartPos |= mask;
            }
        }
        #endregion

        #region  Attributes
        protected   const   int     ciMaxUsableTokenLength = 56;
        protected   const   double  ServiceFeatureThreshold = 0.34;
        protected   const   int     MaximalSentenceNumber = 32600;
        protected   const   int     UnicodeCategoriesNumber = 30;

        protected   string          strToken;
        protected   uint            iStartPos;
        protected   uint            iSentenceNumber;
        protected   ushort          usTokenOrder;
        protected   TokenType       eoType;
        protected   int             termId;
        #endregion

        private static ExternalTrie _tokenTrie;
    }

    internal class MaskEncoder
    {
        public static uint SectionId( long mask )
        {
            uint offset = (uint)(mask & 0x00000000FFFFFFFF);
            return (offset & 0x1CFFFFFF) >> 26;
        }

        public static uint Offset( long mask )
        {
            uint offset = (uint)(mask & 0x00000000FFFFFFFF);
            return (offset & 0x00FFFFFF);
        }

        public static uint OffsetNormal( long mask )
        {
            uint offset = (uint)(mask & 0x00000000FFFFFFFF);
            return offset;
        }

        public static int TokenOrder( long mask )
        {
            int order = (int)(mask >> 48);
            return order;
        }

        public static int Sentence( long mask )
        {
            int upperMask = (int)(mask >> 32);
            return upperMask & 0x0000FFFF;
        }

        public static long Mask( uint order, uint sentence, uint offset )
        {
            return (((long) order) << 48) + (((long) sentence) << 32) + offset;
        }
    }
}
