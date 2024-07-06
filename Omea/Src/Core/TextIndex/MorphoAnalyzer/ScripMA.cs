// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using   System.IO;
using   System.Text.RegularExpressions;
using   System.Collections;
using   System.Diagnostics;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.TextIndex
{
/******************************************************************************
    Class defines major logic for scriptable morphological analysis:
    1. Determine the possible hypothetical lexemes from the wordform given
       and grammar rules.
    2. Filter out those hypotheses which do not appear in the dictionary.
    Comment: the order of the hypotheses which have passed the dictionary test
             is not defined.

    Class determines possible lexeme alternatives and their POS using set of
    rule from the given grammar.
    A rule has the following format:
      <word template> => <modifier> <POS> {| <modifier> <POS>}*

      [word template] is unique across the grammar and has format "*XXXX",
      where "XXXX" defines the word suffix to be analyzed.
      [modifier] is in ['=', '~', '*'] :
        '=' - the word is not changed
        '~' - suffix is stripped out from the word
        '*' - suffix is substituted by the symbols following after the '*'
******************************************************************************/

public class ScriptMorphoAnalyzer
{
    //-------------------------------------------------------------------------
    public  ScriptMorphoAnalyzer( string GrammarFileName )
    {
        Root = new Node();
        listLexemes = new ArrayList();
        reg = new Regex( "^ *(.+?) *=> *(.+) *$" );
        string  content = Utils.ReadEncodedFile( GrammarFileName );
        ParseGrammar( content );
    }

    #region Grammar Parsing
    //-------------------------------------------------------------------------
    //  Each line in the grammar file is a separate rule describing a
    //  {word|ending}, except lines starting with ';'
    //-------------------------------------------------------------------------
    protected   void    ParseGrammar( string GrammarContent )
    {
        string[] all = GrammarContent.Split( '\n' );
        for( int i = 0; i < all.Length; i++ )
        {
            string str = all[ i ].TrimEnd( '\n', '\r' );
            if(( str.Length > 0 ) && ( str[ 0 ] != ';' ))
                ParseLine( str );
        }
    }
    protected   void    ParseGrammar( StreamReader stream_ )
    {
        string  str;
        while(( str = stream_.ReadLine() ) != null )
        {
            if(( str.Length > 0 ) && ( str[ 0 ] != ';' ))
                ParseLine( str );
        }
    }

    //-------------------------------------------------------------------------
    //  Split rules into two parts - wordform pattern and actions with their
    //  corresponding descriptions. Pattern and Action are delimited by "=>"
    //-------------------------------------------------------------------------

    protected   void    ParseLine( string str_ )
    {
        Match   match = reg.Match( str_ );
        if( match.Success )
        {
            //-----------------------------------------------------------------
            string  str_Pattern  = match.Groups[ 1 ].Value,
                    str_Variants = match.Groups[ 2 ].Value;
            int     i_SymIndex = str_Pattern.Length - 1;
            Node    CurrentNode = Root;
            Debug.Assert( i_SymIndex >= 0, "Pattern string has zero length - impossible to build tree" );

            //-----------------------------------------------------------------
            while(( i_SymIndex >= 0 ) && ( str_Pattern[ i_SymIndex ] != '*' ))
            {
                Node    NextNode = CurrentNode.findNodeByBranch( str_Pattern[ i_SymIndex ] );
                if( NextNode == null )
                {
                    NextNode = new Node();
                    CurrentNode.AddNodeBySymbol( NextNode, str_Pattern[ i_SymIndex ] );
                }
                i_SymIndex--;
                CurrentNode = NextNode;
            }
            CurrentNode.InitVariants( str_Variants,
                                     ( i_SymIndex < 0 )? Node.WhatToInit.WordEndList : Node.WhatToInit.WhildCardList );
        }
    }
    #endregion Grammar Parsing

    #region High-level logic
    public  ArrayList  GetLexemes( string wordForm, out bool isPerfectMatch )
    {
        int         suffixLen;
        string[]    hypotheses = FindMatch( wordForm, out suffixLen, out isPerfectMatch );

        listLexemes.Clear();
        if( hypotheses != null )
        {
            //  If we have a perfect match, that is the whole word is
            //  substituted with another one (or there may be two orthographic
            //  variants, but here we do not matter), we set a flag that we
            //  do not need to perform a lookup of this word in the dictionary
            //  later.
            if( isPerfectMatch )
            {
                listLexemes.Add( hypotheses[ 0 ] );
            }
            else
            {
                System.Collections.IEnumerator enum_ = hypotheses.GetEnumerator();
                while ( enum_.MoveNext() )
                {
                    string  str_ = TranslateModifier( wordForm, (string)enum_.Current, suffixLen );
                    if( ! listLexemes.Contains( str_ ))
                        listLexemes.Add( str_ );
                }
            }
        }
        return( listLexemes );
    }
    #endregion High-level logic

    #region Suffix tree traversal
    //-------------------------------------------------------------------------
    //  Walk along the suffix tree in the reversed order of the given wordform
    //-------------------------------------------------------------------------
    public  string[]    FindMatch( string wordForm,
                                   out int matchedSuffixLength,
                                   out bool isPerfectMatch )
    {
        int         i_LastSym = wordForm.Length - 1;
        int         i_SuffixLength = 0, lastVisitedSuffixLength = 0;
        Node        CurrentNode = Root;
        string[]    ResultList = null;

        //---------------------------------------------------------------------
        matchedSuffixLength = 0;
        isPerfectMatch = false;

        while( i_LastSym >= 0 )
        {
            Node    NextNode = CurrentNode.findNodeByBranch( wordForm[ i_LastSym ] );

            //-----------------------------------------------------------------
            //  If there is no continuation for the word suffix - check whether
            //  last node (CurrentNode) has rules for suffix processing,
            //  otherwise return reference to rules which process lesser suffix
            //-----------------------------------------------------------------

            if( NextNode == null )
            {
                if( CurrentNode.aWordEnds != null )
                {
                    matchedSuffixLength = lastVisitedSuffixLength;
                    ResultList = CurrentNode.aWordEnds;
                }
                else
                if( CurrentNode.aWildCards != null )
                {
                    matchedSuffixLength = i_SuffixLength;
                    ResultList = CurrentNode.aWildCards;
                }
                else
                    //  if ResultList == null, then this assignment is just useless.
                    matchedSuffixLength = lastVisitedSuffixLength;

                return( ResultList );
            }

            //-----------------------------------------------------------------
            //  Remember rule variants for the current suffix length so that
            //  return alternatives if no more narrow rule wont be found.
            //-----------------------------------------------------------------

            if( CurrentNode.aWildCards != null )
            {
                ResultList = CurrentNode.aWildCards;
                lastVisitedSuffixLength = i_SuffixLength;
            }

            //-----------------------------------------------------------------
            i_LastSym--;
            i_SuffixLength++;
            CurrentNode = NextNode;
        }

        //---------------------------------------------------------------------
        //  Wordform is shorter than rules which process its suffix
        //---------------------------------------------------------------------

        if( CurrentNode.aWordEnds != null )
        {
            matchedSuffixLength = i_SuffixLength;
            ResultList = CurrentNode.aWordEnds;
            isPerfectMatch = true;
        }
        else
        if( ResultList != null )
        {
            matchedSuffixLength = lastVisitedSuffixLength;
        }

        return( ResultList );
    }
    #endregion Suffix tree traversal

    //-------------------------------------------------------------------------
    //  1. If Modifier == '=' then wordform is not to be changed
    //  2. If Modifier == '~' suffix of specified length must be removed
    //  3. Otherwise, suffix of specified length must be removed, and Modifier
    //     is appended to the rest.
    //-------------------------------------------------------------------------

    private string  TranslateModifier( string wordForm, string action, int suffixLen )
    {
        string  result;
        switch( action )
        {
            case "=":
                result = wordForm;
                break;
            case "~":
                Debug.Assert( suffixLen <= wordForm.Length );
                result = wordForm.Remove( wordForm.Length - suffixLen, suffixLen );
                break;
            default:
                Debug.Assert( suffixLen <= wordForm.Length );
                result = wordForm.Remove( wordForm.Length - suffixLen, suffixLen );
                result += action;
                break;
        }
        return( result );
    }

    //-------------------------------------------------------------------------
    private ArrayList   listLexemes;
    private Node        Root;
    private Regex       reg;
}
}
