/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using  System;
using  System.Collections;
using System.Collections.Generic;
using  System.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.TextIndex
{
    #region TreeStructureDescription
    public class QueryParserNode
    {
        public enum  Type  { eoTerm, eoAnd, eoOr, eoNear, eoPhraseNear, eoNot, eoSection }

        public  QueryParserNode( Type eo_ ) { eoType = eo_;  }
        public  Type  NodeType {   get{ return( eoType ); } }

        protected Type  eoType;
    }

    public class OpNode : QueryParserNode
    {
        public OpNode( Type eo_ ) : base( eo_ ) {}

        public int       BranchesNumber  {  get {  return( aOperands.Count );  }  }
        public ArrayList Branches()      { return aOperands;  }

        public QueryParserNode this[ int i_ ]
        {
            get
            {
                Debug.Assert( i_ < aOperands.Count );
                return( (QueryParserNode)aOperands[ i_ ] );
            }
        }

        public void  AddOperand( QueryParserNode node )
        {   aOperands.Add( node );  }

        public void  SetOperands( ArrayList operands )
        {   
            aOperands = new ArrayList();
            aOperands.AddRange( operands );
        }

        public void  RemoveOperand( QueryParserNode node )
        {
            aOperands.Remove( node );
        }

        public EntryProximity RequiredProximity
        {
            get
            {
                if( eoType == Type.eoAnd )
                    return EntryProximity.Document;
                else
                if( eoType == Type.eoNear )
                    return EntryProximity.Sentence;
                else
                if( eoType == Type.eoPhraseNear )
                    return EntryProximity.Phrase;
                else
                    throw new NotSupportedException( "OpNode -- Node of type [" + eoType + "] does not support proximity estimation." );
            }
        }

        #region Attributes
        protected ArrayList  aOperands = new ArrayList();
        #endregion Attributes
    }

    public class TermNode : QueryParserNode
    {
        public  TermNode() : base( Type.eoTerm ) {}
        public  TermNode( string str ) : base( Type.eoTerm )
        {
            Term = str;
        }

        public  string  Term
        {    
            set{ strValue = value; }
            get{ return strValue; }
        }

        protected string strValue;
    }

    public class  SectionNode : OpNode
    {
        public  SectionNode() : base( Type.eoSection ) {}
        public  string  SectionName { get{ return Section; }  set{ Section = value; }}

        private string Section;
    }
    #endregion

    //-------------------------------------------------------------------------
    //  Parse input query, build query tree, merge words corresponding to terms,
    //  optimize number of levels.
    //-------------------------------------------------------------------------
    public static class  QueryParser
    {
        public static QueryPostfixForm ParseQuery( string query )
        {
            Initialize();
            _query = query.Trim().ToLower();

            QueryPostfixForm form;

            try
            {
                QueryParserNode nodeRoot = ParseExpression();
                QueryParserNode newRoot = null;

                PropagateSectionOpInside( ref newRoot, nodeRoot );
                if( newRoot == null )
                    newRoot = nodeRoot;

                form = new QueryPostfixForm();
                Tree2Postfix( newRoot, form );
            }
            catch( Exception )
            {
                form = null;
            }

            return form;
        }

        public static string  Error {  get{ return( _errorMessage ); }  }

        private static void  Initialize()
        {
            _errorMessage = null;
            iCurrentOffset = 0;
            strToken = strPrevToken = "";
            isPhrasalMode = false;
        }

        #region Parser
        private static QueryParserNode ParseExpression()
        {
            OpNode          nodeOp;
            QueryParserNode nodeLeftOperand = ProcessTerm();

            GetNextToken();

            //---------------------------------------------------------------------
            //  if next token == "" => EOS, no more processing, return left operand
            //  as result, 
            //  if next token == ")" => we reached the endo of parenthed expression,
            //  return left operand as result, else process two operands and Op, 
            //  return Op as root.
            //---------------------------------------------------------------------

            if(( strToken == "" ) || ( strToken == ")" ) || ( strToken == "+\"" ))
            {
                BackToken();
                return( nodeLeftOperand );
            }
            else
            {
                QueryParserNode.Type typeNode;
                if( !isPhrasalMode )
                {
                    if( strToken == "and" )
                        typeNode = QueryParserNode.Type.eoAnd;
                    else
                    if( strToken == "or" )
                        typeNode = QueryParserNode.Type.eoOr;
                    else
                    if( strToken == "near" )
                        typeNode = QueryParserNode.Type.eoNear;
                    else
                    {
                        typeNode = QueryParserNode.Type.eoAnd;
                        BackToken();
                    }
                }
                else
                {
                    typeNode = QueryParserNode.Type.eoPhraseNear;
                    BackToken();
                }
                
                nodeOp = new OpNode( typeNode );
                QueryParserNode nodeRightOperand = ParseExpression();
                nodeOp.AddOperand( nodeLeftOperand );
                nodeOp.AddOperand( nodeRightOperand );

                return( nodeOp );
            }
        }

        private static QueryParserNode  ProcessTerm()
        {
            QueryParserNode nodeTerm, result;
            result = nodeTerm = ProcessPrimaryLevel();
            
            GetNextToken();
            if( strToken == "[" )
            {
                SectionNode nodeOp = new SectionNode();

                GetNextToken();
                nodeOp.SectionName = strToken;
                nodeOp.AddOperand( nodeTerm );
                _errorMessage = "Error in the query syntax - expected ']' symbol in document section specifier";
                Expect( "]" );
                _errorMessage = null;

                result = nodeOp;
            }
            else
                BackToken();

            return( result );
        }

        private static QueryParserNode  ProcessPrimaryLevel()
        {
            QueryParserNode    nodePrimaryToken = null;
            
            GetNextToken();
            if( strToken != "" )
            {
                if( strToken == "(" )
                {
                    nodePrimaryToken = ParseExpression();
                    _errorMessage = "Error in the query syntax - expected ')' symbol in expression";
                    Expect( ")" );
                    _errorMessage = null;
                }
                else
                if( strToken == "\"+" )
                {
                    isPhrasalMode = true;
                    nodePrimaryToken = ParseExpression();
                    Expect( "+\"" );
                    isPhrasalMode = false;
                }
                else
                {
                    string normToken = OMEnv.LexemeConstructor.GetNormalizedToken( strToken );

                    if( isDelimitableToken( normToken ))
                        nodePrimaryToken = SplitTokenToTree( normToken );
                    else
                    {
                        nodePrimaryToken = new TermNode( normToken );
                    }
                }
            }

            return( nodePrimaryToken );
        }
        #endregion Parser

        #region TreeConverters
        private static void PropagateSectionOpInside( ref QueryParserNode parent, QueryParserNode subtreeRoot )
        {
            if( subtreeRoot.NodeType == QueryParserNode.Type.eoTerm )
                return;

            if( subtreeRoot.NodeType == QueryParserNode.Type.eoSection )
            {
                Debug.Assert( ((OpNode)subtreeRoot).BranchesNumber == 1, "Illegal parsing of section operator" );
                QueryParserNode child = ((OpNode)subtreeRoot)[ 0 ];
                ArrayList       branches;

                //  if section op is applied to the tree - perform conversion - 
                //  propagate section op into every operand of the tree, and
                //  tree root becomes new local root.
                
                if( child.NodeType != QueryParserNode.Type.eoTerm && 
                    child.NodeType != QueryParserNode.Type.eoSection )
                {
                    //-------------------------------------------------------------
                    if( parent == null )
                        parent = child;
                    else
                    {
                        ((OpNode)parent).RemoveOperand( subtreeRoot );
                        ((OpNode)parent).AddOperand( child );
                    }

                    //-------------------------------------------------------------
                    branches = ((OpNode)child).Branches();
                    for( int i = 0; i < branches.Count; i++ )
                    {
                        SectionNode newSectionOp = new SectionNode();
                        newSectionOp.SectionName = ((SectionNode)subtreeRoot).SectionName;
                        newSectionOp.AddOperand( (QueryParserNode)branches[ i ] );
                        branches[ i ] = newSectionOp;
                    }

                    ((OpNode)child).SetOperands( branches );
                }

                //-----------------------------------------------------------------
                if( child.NodeType != QueryParserNode.Type.eoTerm )
                {
                    branches = ((OpNode)child).Branches();
                    for( int i = 0; i < branches.Count; i++ )
                        PropagateSectionOpInside( ref child, (QueryParserNode)branches[ i ] );
                }
            }
        }

        private static void Tree2Postfix( QueryParserNode root, QueryPostfixForm form )
        {
            if( root == null )
                return;

            if( root.NodeType != QueryParserNode.Type.eoTerm )
            {
                foreach( QueryParserNode node in ((OpNode)root).Branches() )
                    Tree2Postfix( node, form );
            }
            form.Add( root );
        }

        private static QueryParserNode SplitTokenToTree( string token )
        {
            ArrayList       subtokens = SplitTokens( token );
            QueryParserNode node;

            //  1. If there are no tokens - the input string completely consists
            //     of delimiter symbols.
            //  2. If there is only one token - then delimiter symbols either started
            //     or closed the string.
            if( subtokens.Count == 0 )
            {
                node = new TermNode( token );
            }
            else if( subtokens.Count == 1 )
            {
                // check if wildcard
                if( token.EndsWith( "*" ) || token.EndsWith( "?" ) )
                {
                    ArrayList tokens = Word.GetTokensByWildcard( token );
                    if( tokens.Count > 0 )
                    {
                        node = BuildTree( tokens, QueryParserNode.Type.eoOr );
                    }
                    else
                    {
                        node = new TermNode( token );
                    }
                }
                else
                {
                    node = new TermNode( (string)subtokens[ 0 ] );
                }
            }
            else
            {
                node = BuildTree( subtokens, QueryParserNode.Type.eoPhraseNear );
            }
            return node;
        }

        private static QueryParserNode BuildTree( IList subtokens, QueryParserNode.Type opCode )
        {
            QueryParserNode node;

            node = new TermNode( (string)subtokens[ subtokens.Count - 1 ] );
            for( int i = subtokens.Count - 2; i >= 0; i-- )
            {
                TermNode leftOpnd = new TermNode( (string)subtokens[ i ] );
                OpNode op = new OpNode( opCode );
                op.AddOperand( leftOpnd );
                op.AddOperand( node );
                node = op;
            }
            return node;
        }

        private static ArrayList SplitTokens( string token )
        {
            int         delimIndex;
            ArrayList   subtokens = new ArrayList();
            do
            {
                delimIndex = GetDelimiterIndex( token );
                if( delimIndex != -1 )
                {
                    //  Forbid using parentheses '(', ')', '[' and ']' as
                    //  used non-intentionally.
                    char ch = token[ delimIndex ];
                    if(( delimIndex == 0 && isCloseBrace( ch )) ||
                       ( delimIndex != 0 ) && ( isOpenBrace( ch ) || isCloseBrace( ch ) ))
                    {
                        _errorMessage = "Illegal characters met in the token \"" + token + "\"";
                        throw new Exception( "Illegal characters met in the token \"" + token + "\"" );
                    }
                    else
                    if( delimIndex != 0 )
                    {
                        subtokens.Add( token.Substring( 0, delimIndex ) );
                    }
                    token = token.Substring( delimIndex + 1 );
                }
                else
                if( token.Length > 0 )
                    subtokens.Add( token );
            }
            while( delimIndex != -1 );
            return subtokens;
        }
        #endregion TreeConverters

        #region Tokenizer
        private static void  GetNextToken()
        {
            if( strPrevToken != "" )
            {
                strToken = strPrevToken;
                strPrevToken = "";
            }
            else
            {
                if( iCurrentOffset == _query.Length )
                {
                    strToken = "";
                }
                else
                {
                    SkipWhitespace();
                    if( isParenthesisSymbol() )
                    {
                        strToken = _query.Substring( iCurrentOffset++, 1 );

                        //---------------------------------------------------------
                        //  We have somehow to distinguish between opening and closing
                        //  quote sign. Blank before or after it gives a tip.
                        //---------------------------------------------------------

                        if( strToken == "\"" )
                        {
                            if(( iCurrentOffset == _query.Length ) ||
                               ( _query[ iCurrentOffset ] == ' ' ) || ( _query[ iCurrentOffset ] == ')' ))
                                strToken = "+\"";
                            else
                                strToken = "\"+";
                        }
                    }
                    else
                    {
                        int i_StartOffset = iCurrentOffset;
                        while(( iCurrentOffset < _query.Length ) && 
                              !isParenthesisSymbol() && _query[ iCurrentOffset ] != ' ' )
                            iCurrentOffset++;
                        
                        if( iCurrentOffset == i_StartOffset ) //  End Of String
                            strToken = "";
                        else
                            strToken = _query.Substring( i_StartOffset, iCurrentOffset - i_StartOffset );
                    }
                }
            }

            return;
        }
        
        private static void BackToken()
        {
            Debug.Assert( strPrevToken == "", "Attempt to overvrite non-empty BackToken" );
            strPrevToken = strToken;
        }
        
        private static void SkipWhitespace()
        {
            while(( iCurrentOffset < _query.Length ) && ( _query[ iCurrentOffset ] == ' ' ))
                iCurrentOffset++;
        }

        private static void Expect( string str_ )
        {
            GetNextToken();
            if( strToken != str_ )
            {
                throw( new Exception( "Illegal query format - expected token: '" + str_ + "'" ) );
            }
        }

        private static bool isParenthesisSymbol()
        {
            Debug.Assert( iCurrentOffset < _query.Length );

            bool    isStart = iCurrentOffset == 0,
                    isFinish = iCurrentOffset == _query.Length - 1;
            char    ch_ = _query[ iCurrentOffset ];
            char    prevCh = Char.MinValue, nextCh = Char.MinValue;

            if( iCurrentOffset > 0 )
                prevCh = _query[ iCurrentOffset - 1 ];
            if( iCurrentOffset < _query.Length - 1 )
                nextCh = _query[ iCurrentOffset + 1 ];

            return(( isOpenBrace( ch_ )  && ( isStart || ( prevCh == ' ' ) )) ||
                   ( isCloseBrace( ch_ ) && ( isFinish || (nextCh == ' ' ) || (nextCh == ')' ) )) ||
                   ( ch_ == '"' ) && ( isStart || ( prevCh == ' ' ) || ( prevCh == '(' ) ||
                                       isFinish || ( nextCh == ' ' ) || ( nextCh == ')' ))) ;
        }

        private static bool isOpenBrace( char ch )
        {
            return ( ch == '[' ) || ( ch == '(' );
        }

        private static bool isCloseBrace( char ch )
        {
            return ( ch == ']' ) || ( ch == ')' ) || ( ch == '"' );
        }

        private static bool isDelimitableToken( string token )
        {
            return( GetDelimiterIndex( token ) != -1 );
        }

        private static int GetDelimiterIndex( string token )
        {
            for( int i = 0; i < token.Length; i++ )
            {
                if( TextDocParser.isDelimiter( token[ i ] ))
                    return i;
            }
            return -1;
        }
        #endregion

        #region Attributes
        private static int     iCurrentOffset = 0;
        private static string  _query;
        private static string  strToken = "", strPrevToken = "";
        private static bool    isPhrasalMode = false;

        private static string _errorMessage;
        #endregion Attributes

/*
        private void PrintTree( IList form )
        {
            Trace.WriteLine( "QueryParser ========================================================" );
            for( int i = 0; i < form.Count; i++ )
            {
                if( ((QueryParserNode)form[ i ]).NodeType == QueryParserNode.Type.eoTerm )
                    Trace.WriteLine( "QueryParser -- " + ((TermNode)form[ i ]).Term );
                else
                    Trace.WriteLine( "QueryParser -- " + ((QueryParserNode)form[ i ]).NodeType );
            }
            Trace.WriteLine( "QueryParser ========================================================" );
        }
*/
    }

    public class QueryPostfixForm : List<QueryParserNode>
    {
        private int  _termNodesNumber;

        public int  TermNodesCount      {  get {  return( _termNodesNumber );  }  }
        public void IncTermNodesCount() {  _termNodesNumber++;  }
        public int  PostfixCount        {  get {  return( Count );  }  }

        public QueryPostfixForm()
        {
            _termNodesNumber = 0;
        }

        public new void Add( QueryParserNode node )
        {
            if( node is TermNode )
                IncTermNodesCount();

            base.Add( node );
        }
    }
}