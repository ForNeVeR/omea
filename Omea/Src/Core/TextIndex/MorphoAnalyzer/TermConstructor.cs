/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections;
using System.Diagnostics;

namespace JetBrains.Omea.TextIndex
{
    public class LexemeConstructor
    {
        public  LexemeConstructor( ScriptMorphoAnalyzer morphAn, DictionaryServer  dicServer )
        {
            _morphAn = morphAn;
            _dicServer = dicServer;
        }

        public string  GetNormalizedToken( string token )
        {
            Word word = new Word();
            word.Token = token;
            NormalizeToken( word );
            return word.Token;
        }

        public void  NormalizeToken( Word word )
        {
            word.ClearMarks();
            if( !_dicServer.isUnchangeable( word.Token ) )
            {
                int  dicIndex = -1, dicLength, mapIndex = -1, mapLength = -1;

                // NB: ref word.Token ???
                _dicServer.FindLowerBound( word.Token, out dicIndex, out dicLength );

                if( dicIndex != -1 )
                {
                    if( dicIndex < 0 )
                    {
                        GetMapIndex( -dicIndex, dicLength, out mapIndex, out mapLength );
                        AssignStringAndLinguisticInflexion( word, mapIndex, mapLength );
                    }
                    //  do nothing if the token is a dictionary entry
                }
                else
                {
                    string  perfectMatch;
                    GetNormalizedForm( word, out dicIndex, out dicLength, out perfectMatch );
                    if( dicIndex != -1 )
                    {
                        mapIndex = dicIndex; mapLength = dicLength;
                        if( dicIndex < 0 )
                            GetMapIndex( -dicIndex, dicLength, out mapIndex, out mapLength );
                        AssignStringAndLinguisticInflexion( word, mapIndex, mapLength );
                    }
                    else
                    if( perfectMatch != null )
                        AssignStringAndLinguisticInflexion( word, perfectMatch );
                }
            }
        }

        protected void GetMapIndex( int index, int length, out int mapIndex, out int mapLength )
        {
            mapIndex = _dicServer.GetMapIndex( index, length );
            mapLength = length - (mapIndex - index);
            Debug.Assert( mapIndex != -1 );
        }

        //-------------------------------------------------------------------------
        //  Hack #1. If there was just reduction of plural or 3rd person - mark that
        //  by inverting the Offset sign (highest bit)
        //  Hack #2. If there was a reduction of proper verb (-ed, common case, or -d for
        //  verbs on "-e"), invert prehighest bit.
        //  Hack #3. If there was a reduction of continuous form (+ing, or -e+ing)
        //-------------------------------------------------------------------------
        protected void  AssignStringAndLinguisticInflexion( Word word, int index, int length )
        {
            int     lengthsDelta = word.Token.Length - length;
            char    lastNewChar = _dicServer.GetChar( index + length - 1 );
            string  newValue = null;

            if(( lengthsDelta == 1 ) && ( word.Token[ word.Token.Length - 1 ] == 's' ))
                word.MarkPlural();
            else
            if(( lengthsDelta == 2 ) && word.Token.EndsWith( "ies" ) && lastNewChar == 'y' )
                word.MarkPlural();
            else
            if( word.Token.EndsWith( "ed" ) )
            {
                if(( lengthsDelta == 2 ) || (( lengthsDelta == 1 ) && ( lastNewChar == 'e' )))
                    word.MarkProperPast();
            }
            else
            if( word.Token.EndsWith( "ing" ) )
            {
                if(( lengthsDelta == 3 ) || (( lengthsDelta == 2 ) && ( lastNewChar == 'e' )))
                    word.MarkContinuous();
            }
            else
            {
                int wfVariantIndex = _dicServer.GetWordformVariant( word.Token );
                if( wfVariantIndex == -1 )
                {
                    newValue = _dicServer.GetDicString( index, length );
                    wfVariantIndex = _dicServer.StoreMapping( word.Token, newValue );
                }
                word.MarkWordformVariant( wfVariantIndex );
            }
            if( newValue != null )
                word.Token = newValue;
            else
                word.Token = _dicServer.GetDicString( index, length );
        }

        protected void  AssignStringAndLinguisticInflexion( Word word, string match )
        {
            int wfVariantIndex = _dicServer.GetWordformVariant( word.Token );
            if( wfVariantIndex == -1 )
            {
                wfVariantIndex = _dicServer.StoreMapping( word.Token, match );
            }

            word.MarkWordformVariant( wfVariantIndex );
            word.Token = match;
        }

        //-------------------------------------------------------------------------
        //  Find the normalized form (dictionary form) for the given wordform.
        //-------------------------------------------------------------------------

        private void GetNormalizedForm( Word word, out int index, out int length, out string perfectValue )
        {
            length = index = -1;
            perfectValue = null;
            if( word.Tag != Word.TokenType.eoNumber )
            {
                bool isPerfectMatch;
                ArrayList   lexemes = _morphAn.GetLexemes( word.Token, out isPerfectMatch );
                if( isPerfectMatch )
                    perfectValue = (string)lexemes[ 0 ];
                else
                    FilterHypotheses( lexemes, out index, out length );
            }
        }

        private void FilterHypotheses( ArrayList lexemes, out int index, out int length )
        {
            length = index = -1;
            if( lexemes.Count > 0 )
            {
                foreach( string str in lexemes )
                {
                    if(  _dicServer.FindLowerBound( str, out index, out length ) && 
                         !_dicServer.isUnchangeable( str ) )
                        break;
                }
            }
        }

        #region Attributes
        protected   ScriptMorphoAnalyzer  _morphAn;
        protected   DictionaryServer      _dicServer;
        #endregion
    }
}