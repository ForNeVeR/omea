/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.TextIndex
{
    /// <summary>
    /// Special fake class used to designate a special case of term - stopword.
    /// An object of this class is pushed to stack instead of an index entry
    /// in order to perform special processing of the subsequent operations.
    /// </summary>
    internal class StopwordTerm {}

    ///<summary>
    ///  Process a query represented in a postfix form, transform query terms
    ///  to their document sets, and perform "anding" and "oring" of these sets.
    ///  Result set is the non-filtered result.
    ///</summary>
    public class QueryProcessor
    {
        /// <summary>
        /// Query processor currently can return two statuses of errors:
        /// <para>1. when input query has illegal syntax;</para>
        /// <para>2. when a document section with a given name does not exist.</para>
        /// </summary>
        public enum ErrorStatus {  NoError, IllegalSectionName, IllegalQuerySyntax };

        //-------------------------------------------------------------------------
        public static ErrorStatus  Status{  get{ return Error; }  }

        public static Entry[]  ProcessQuery( QueryPostfixForm postfixForm, TermIndexAccessor termIndex )
        {
            return ProcessQuery( postfixForm, termIndex, false );
        }

        public static Entry[]  ProcessQuery( QueryPostfixForm postfixForm, TermIndexAccessor termIndex, bool appendIdMappings )
        {
            Stack   opStack = new Stack();
            Entry[] result = null;
            Error = ErrorStatus.NoError;
            MappedInstances.Clear();

            if( !appendIdMappings )
            {
                Lexemes.Clear();
                Stopwords.Clear();
            }

            try
            {
                IteratePostfixExpression( postfixForm, termIndex, opStack );

                //-----------------------------------------------------------------
                //  Now only one Entry[] must remain on the top of the stack. It may
                //  be null if no document correspond to the query
                //-----------------------------------------------------------------
                if( opStack.Count != 1 )
                    throw new ApplicationException( "QueryParser -- Illegal query statement found" );

                if( !( opStack.Peek() is StopwordTerm ) )
                {
                    result = ExtractOperandFromStack( opStack );
                    if( result != null )
                        Array.Sort( result, new CompareByTfIdf() );
                }
            }
            catch( Exception exc )
            {
                Trace.WriteLine( "QueryProcessor -- exception [" + exc.Message + "] occured." );
                //  Exception is raised if the expression was constructed with
                //  the syntactic errors.
                //  Clear the stack and put special marker on the top of it
                Error = ErrorStatus.IllegalQuerySyntax;
                result = null;
            }
            opStack.Clear();
            return( result );
        }

        private static void IteratePostfixExpression( IList<QueryParserNode> postfixForm, TermIndexAccessor termIndex, Stack opStack )
        {
            for( int i = 0; i < postfixForm.Count; i++ )
            {
                QueryParserNode node = postfixForm[ i ];
                if( node.NodeType == QueryParserNode.Type.eoTerm )
                {
                    PushTermOnStack( ((TermNode)node).Term, termIndex, opStack );
                }
                else
                if( node.NodeType == QueryParserNode.Type.eoSection )
                {
                    UnarySectionOp( ((SectionNode)node).SectionName, opStack );
                }
                else
                {
                    BinaryOp( node, opStack );
                }
            }
        }

        private static void PushTermOnStack( string term, TermIndexAccessor index, Stack opStack )
        {
            if( !FullTextIndexer.isValuableToken( term ) )
            {
                opStack.Push( new StopwordTerm() );
                if( Stopwords.IndexOf( term ) == -1 )
                    Stopwords.Add( term );
            }
            else
            {
                TermIndexRecord record = index.GetRecord( term );
                if( record != null )
                {
                    int order = Lexemes.IndexOf( term );
                    if( order == -1 )
                    {
                        Lexemes.Add( term );
                        order = Lexemes.Count - 1;
                    }
                    record.PopulateRecordID( (ushort) order );
                }
                opStack.Push( record );
            }
        }

        private static void  BinaryOp( QueryParserNode node, Stack opStack )
        {
            #region Preconditions
            if ( opStack.Count < 2 )
                throw new ApplicationException( "QueryProcessor -- Insufficient number of operands in the operating stack" );
            #endregion Preconditions
            
            //  First, check One or both arguments to be stopwords.
            object o1 = opStack.Pop(), o2 = opStack.Peek();
            opStack.Push( o1 );

            //  If both arguments are stopwords - push them both, leave the stopword 
            //  sign as the result for subsequent calculations.
            //  If only one argument is stopword - leave its counterpart as the result.
            if( o1 is StopwordTerm )
            {
                opStack.Pop();
            }
            else
            if( o2 is StopwordTerm )
            {
                opStack.Pop(); // o1
                opStack.Pop(); // o2
                opStack.Push( o1 ); // o1 again instead of the couple.
            }
            //  both arguments are normal terms which might be met in the index.
            else
            {
                Entry[] result;
                Entry[] rightIndices = ExtractOperandFromStack( opStack );
                Entry[] leftIndices = ExtractOperandFromStack( opStack );

                if( node.NodeType == QueryParserNode.Type.eoOr )
                    result = Join( leftIndices, rightIndices );
                else
                    result = Intercross( leftIndices, rightIndices, ((OpNode)node).RequiredProximity );

                opStack.Push( result );
            }
        }

        #region Operators
        //-------------------------------------------------------------------------
        //  "SECTION" Op
        //-------------------------------------------------------------------------
        private static void  UnarySectionOp( string sectionName, Stack opStack )
        {
            if( ! ( opStack.Peek() is StopwordTerm ))
            {
                uint sectionId = 0;
                if( DocSectionHelper.IsShortNameExist( sectionName ))
                    sectionId = DocSectionHelper.OrderByShortName( sectionName );
                else
                if( DocSectionHelper.IsFullNameExist( sectionName ))
                    sectionId = DocSectionHelper.OrderByFullName( sectionName );

                if( sectionId > 0 )
                {
                    Entry[] operand = ExtractOperandFromStack( opStack );
                    operand = SelectRestrictedEntries( operand, sectionId );
                    opStack.Push( operand );
                }
                else
                    Error = ErrorStatus.IllegalSectionName;
            }
        }

        ///<summary>
        ///  "And" and "Near" Ops. Particular behavior is determined by optional
        ///  predicate which may perform additional check of [intermediate] document,
        ///  feasible for inclusion into the result set
        /// </summary>
        private static Entry[]  Intercross( Entry[] leftIndices, Entry[] rightIndices,
                                            EntryProximity reqProximity )
        {
            if(( leftIndices == null ) || ( rightIndices == null ))
                return( null );

            Array.Sort( leftIndices );
            Array.Sort( rightIndices );

            //---------------------------------------------------------------------
            ArrayList   array_ = new ArrayList();
            int         i_Left = 0, i_Right = 0;

            while(( i_Left < leftIndices.Length ) && ( i_Right < rightIndices.Length ))
            {
                if( leftIndices[ i_Left ].DocIndex == rightIndices[ i_Right ].DocIndex )
                {
                    EntryProximity  scope = ProximityEstimator.EstimateProximity( leftIndices[ i_Left ], rightIndices[ i_Right ] );
                    if( scope <= reqProximity )
                    {
                        leftIndices[ i_Left ].Proximity = scope;
                        array_.Add( JoinInstancesOfEntries( leftIndices[ i_Left ], rightIndices[ i_Right ], reqProximity ));
                    }
                    i_Left++; i_Right++;
                }
                else
                if( leftIndices[ i_Left ].DocIndex < rightIndices[ i_Right ].DocIndex )
                    i_Left++;
                else
                    i_Right++;
            }

            //---------------------------------------------------------------------
            Entry[]   ai_Result = null;
            if( array_.Count > 0 )
            {
                ai_Result = new Entry[ array_.Count ];
                array_.CopyTo( ai_Result );
            }

            return( ai_Result );
        }

        //-------------------------------------------------------------------------
        //  "Or" Op
        //-------------------------------------------------------------------------

        private static Entry[] Join( Entry[] leftIndices, Entry[] rightIndices )
        {
            //---------------------------------------------------------------------
            ArrayList array_ = new ArrayList();
            if( leftIndices != null )   array_.AddRange( leftIndices );
            if( rightIndices != null )  array_.AddRange( rightIndices );
            array_.Sort();

            //---------------------------------------------------------------------
            int i = 0, count = array_.Count;
            while( i < count - 1 )
            {
                if( ((Entry)array_[ i ]).DocIndex == ((Entry)array_[ i + 1 ]).DocIndex )
                {
                    array_[ i ] = JoinInstancesOfEntries( (Entry)array_[ i ], (Entry)array_[ i + 1 ],
                                                          EntryProximity.Document );
                    array_.RemoveAt( i + 1 );
                    count = array_.Count;
                }
                i++;
            }

            //---------------------------------------------------------------------
            Entry[]   ai_Result = null;
            if( array_.Count > 0 )
            {
                ai_Result = new Entry[ array_.Count ];
                array_.CopyTo( ai_Result );
            }

            return( ai_Result );
        }
        #endregion

        ///<summary>
        ///  Extract operand from the top of the stack and cast it to the necessary
        ///  type (Entry[]) if necessary via extracting data from the record
        ///</summary>
        private static Entry[] ExtractOperandFromStack( Stack opStack )
        {
            Object  Operand = opStack.Pop();
            Debug.Assert(( Operand is TermIndexRecord ) || ( Operand is Entry[] ) || ( Operand == null ));

            if( Operand is TermIndexRecord )
                return( ((TermIndexRecord)Operand).Entries );
            else
                return( (Entry[]) Operand );
        }

        //-------------------------------------------------------------------------
        private static Entry  JoinInstancesOfEntries( Entry left, Entry right,
                                                      EntryProximity requiredProximity )
        {
            Entry   JoinedEntry = new Entry();

            JoinedEntry.DocIndex = left.DocIndex;
            JoinedEntry.TfIdf = left.TfIdf + right.TfIdf;
            JoinedEntry.Proximity = left.Proximity;
            InstanceOffset[]    joinedOffsets;

            //  If required proximity is Phrasal, then we need to highlight
            //  only those terms and show only those contexts which correspond
            //  to seach term instances EXACTLY in phrases found, and not
            //  others located elsewhere in the document.
            if( requiredProximity == EntryProximity.Phrase )
            {
                //  Assumption is made that all offsets in the entries are
                //  sorted in asceding order.

                ArrayList tempOffsets = new ArrayList();
                int leftIndex = 0, rightIndex = 0;
                while( leftIndex < left.Count && rightIndex < right.Count )
                {
                    InstanceOffset leftOff = left.Offsets[ leftIndex ], rightOff = right.Offsets[ rightIndex ];
                    if( ProximityEstimator.isPhraseProximity( leftOff, rightOff ))
                    {
                        tempOffsets.Add( leftOff );
                        tempOffsets.Add( rightOff );
                        AddMappedInstances( tempOffsets, left.DocIndex, rightOff );
                        MappedInstances[ HC( left.DocIndex, leftOff.OffsetNormal ) ] = rightOff;
                    }
                    if( leftOff.OffsetNormal < rightOff.OffsetNormal )
                        leftIndex++;
                    else
                        rightIndex++;
                }
                joinedOffsets = (InstanceOffset[]) tempOffsets.ToArray( typeof( InstanceOffset ) );
            }
            else
            {
                joinedOffsets = new InstanceOffset[ left.Count + right.Count ];
                left.Offsets.CopyTo( joinedOffsets, 0 );
                right.Offsets.CopyTo( joinedOffsets, left.Count );
            }
            JoinedEntry.Offsets = joinedOffsets;

            return( JoinedEntry );
        }

        private static void AddMappedInstances( IList tempOffsets, int docIndex, InstanceOffset inst )
        {
            long  hashCode = HC( docIndex, inst.OffsetNormal );
            object rightInst = MappedInstances[ hashCode ];
            while( rightInst != null )
            {
                tempOffsets.Add( (InstanceOffset) rightInst );
                hashCode = HC( docIndex, ((InstanceOffset) rightInst).OffsetNormal );
                rightInst = MappedInstances[ hashCode ];
            }
        }

        private static Entry[] SelectRestrictedEntries( IEnumerable<Entry> entries, uint sectionId )
        {
            ArrayList result = new ArrayList();
            if( entries != null )
            {
                foreach( Entry e in entries )
                {
                    InstanceOffset[] validOffsets = e.FilterOffsetsBySection( sectionId );
                    if( validOffsets.Length > 0 )
                    {
                        e.Offsets = validOffsets;
                        result.Add( e );
                    }
                }
            }

            return ( result.Count == 0 ) ? null : (Entry[]) result.ToArray( typeof( Entry ));
        }

        public  static string[]  LastStoplist        {  get { return (string[])Stopwords.ToArray( typeof( string )); }  }
        public  static string[]  LastSearchLexemes   {  get { return (string[])Lexemes.ToArray( typeof( string )); }  }
        private static long      HC( int docIndex, int offset ) { return ((long)docIndex) << 32 | (uint)offset;  }

        //-------------------------------------------------------------------------
        private static readonly ArrayList   Lexemes = new ArrayList();
        private static readonly ArrayList   Stopwords = new ArrayList();
        private static readonly Hashtable   MappedInstances = new Hashtable();

        private static ErrorStatus  Error;
    }

    public class MatchProcessor
    {
        /// <summary>
        /// This local cache contains IDs of the terms which have been already queried
        /// in the terms trie.
        /// </summary>
        private static readonly Dictionary<string,int> _termIDs = new Dictionary<string, int>();

        public static bool  MatchQuery( QueryPostfixForm postfixForm, IntHashTable tokens )
        {
            Stack<List<long>> opStack = new Stack<List<long>>();
            bool  result;

            try
            {
                IteratePostfixExpression( postfixForm, tokens, opStack );
                if( opStack.Count != 1 )
                    throw new ApplicationException( "QueryParser -- Illegal query statement found" );

                result = (opStack.Peek() != null);
            }
            catch( Exception exc )
            {
                Trace.WriteLine( "MatchProcessor -- exception [" + exc.Message + "] occured." );
                result = true;
            }

            opStack.Clear();
            return( result );
        }

        private static void IteratePostfixExpression( IList<QueryParserNode> postfixForm, IntHashTable tokens, Stack<List<long>> opStack )
        {
            for( int i = 0; i < postfixForm.Count; i++ )
            {
                QueryParserNode  node = postfixForm[ i ];
                switch( node.NodeType )
                {
                    case QueryParserNode.Type.eoTerm:
                        PushTermOnStack( ((TermNode)node).Term, tokens, opStack ); break;
                    case QueryParserNode.Type.eoSection:
                        UnarySectionOp( ((SectionNode)node).SectionName, opStack ); break;
                    default:
                        BinaryOp( node, opStack ); break;
                }
            }
        }

        private static void PushTermOnStack( string term, IntHashTable tokens, Stack<List<long>> opStack )
        {
            List<long> resultVal = null;
            if( FullTextIndexer.isValuableToken( term ) )
            {
                int HC;

                //  First check Id of the term in the local cache. Since the amount of
                //  query terms over all queries in the system is several tens (in average),
                //  the size of this cache is small enough. This cache allows not to
                //  consult terms trie each time.
                if( !_termIDs.TryGetValue( term, out HC ) )
                {
                    HC = Word.GetTokenIndex( term );
                }

                if( HC != -1 )
                {
                    if( !_termIDs.ContainsKey( term ) )
                        _termIDs.Add( term, HC );

                    Object val = tokens[ HC ];
                    if( val != null )
                    {
                        resultVal = val as List<long>;
                        if( resultVal == null )
                        {
                            resultVal = new List<long>();
                            resultVal.Add( (long)val );
                        }
                    }
                }
            }
            opStack.Push( resultVal );
        }

        private static void  UnarySectionOp( string sectionName, Stack<List<long>> opStack )
        {
            if( opStack.Peek() != null )
            {
                uint sectionId = 0;
                if( DocSectionHelper.IsShortNameExist( sectionName ))
                    sectionId = DocSectionHelper.OrderByShortName( sectionName );
                else
                if( DocSectionHelper.IsFullNameExist( sectionName ))
                    sectionId = DocSectionHelper.OrderByFullName( sectionName );

                if( sectionId > 0 )
                {
                    List<long>  operand = opStack.Pop();
                    operand = SelectRestrictedMatchedEntries( operand, sectionId );
                    opStack.Push( operand );
                }
            }
        }

        private static List<long> SelectRestrictedMatchedEntries( IEnumerable<long> offsets, uint sectionId )
        {
            List<long> result = new List<long>();
            foreach( long offset in offsets )
            {
                if( MaskEncoder.SectionId( offset ) == sectionId )
                    result.Add( offset );
            }

            return (result.Count == 0) ? null : result;
        }

        private static void  BinaryOp( QueryParserNode node, Stack<List<long>> opStack )
        {
            #region Preconditions
            if ( opStack.Count < 2 )
                throw new ApplicationException( "QueryProcessor -- Insufficient number of operands in the operating stack" );
            #endregion Preconditions
            
            List<long> result;
            List<long> right = opStack.Pop(), left = opStack.Pop();

            if( node.NodeType == QueryParserNode.Type.eoOr )
                result = Join( left, right );
            else
                result = Intercross( left, right, ((OpNode)node).RequiredProximity );

            opStack.Push( result );
        }

        private static List<long> Intercross( List<long> leftIndices, List<long> rightIndices,
                                              EntryProximity reqProximity )
        {
            if(( leftIndices == null ) || ( rightIndices == null ))
                return( null );

            leftIndices.Sort();
            rightIndices.Sort();

            List<long> result = new List<long>();

            EntryProximity  scope = ProximityEstimator.EstimateProximity( leftIndices, rightIndices );
            if( scope <= reqProximity )
                result.AddRange( JoinInstancesOfEntries( leftIndices, rightIndices, reqProximity ));

            return (result.Count == 0) ? null : result;
        }
 
        private static List<long> Join( IEnumerable<long> leftIndices, IEnumerable<long> rightIndices )
        {
            List<long> result = new List<long>();

            if( leftIndices != null )  result.AddRange( leftIndices );
            if( rightIndices != null ) result.AddRange( rightIndices );
            result.Sort();

            return (result.Count == 0) ? null : result;
        }

        private static List<long> JoinInstancesOfEntries( List<long> left, List<long> right,
                                                          EntryProximity requiredProximity )
        {
            List<long> joinedList = new List<long>();

            if( requiredProximity == EntryProximity.Phrase )
            {
                //  Assumption is made that all offsets in the entries are
                //  sorted in asceding order.

                int leftIndex = 0, rightIndex = 0;
                while( leftIndex < left.Count && rightIndex < right.Count )
                {
                    int order1 = MaskEncoder.TokenOrder( left[ leftIndex ] ),
                        order2 = MaskEncoder.TokenOrder( right[ rightIndex ] );

                    if( ProximityEstimator.isPhraseProximity( order1, order2 ))
                    {
                        joinedList.Add( left[ leftIndex ] );
                        joinedList.Add( right[ rightIndex ] );
                    }
                    if( order1 < order2 )
                        leftIndex++;
                    else
                        rightIndex++;
                }
            }
            else
            {
                joinedList.AddRange( left );
                joinedList.AddRange( right );
            }

            return( joinedList );
        }
    }

    #region Proximity Estimator
    //-----------------------------------------------------------------------------
    //  NB: It should be noted that this algorithm is almost optimal only with
    //      the assumption that all entry offsets are sorted!!!
    //-----------------------------------------------------------------------------
    internal class ProximityEstimator
    {
        internal static EntryProximity  EstimateProximity( Entry Left, Entry Right )
        {
            Debug.Assert( Left.DocIndex == Right.DocIndex, "Illegal precondition for calling Estimator - doc IDs are different" );

            int               iLeft = 0, iRight = 0;
            EntryProximity    Result = EntryProximity.Document;

            while(( iLeft < Left.Count ) && ( iRight < Right.Count ))
            {
                InstanceOffset leftOff = Left.Instance( iLeft );
                InstanceOffset rightOff = Right.Instance( iRight );

                if( leftOff.Sentence == rightOff.Sentence )
                {
                    Result = EntryProximity.Sentence;
                    if( isPhraseProximity( leftOff, rightOff ) )
                    {
                        Result = EntryProximity.Phrase;
                        break;
                    }
                }
                if( leftOff.OffsetNormal < rightOff.OffsetNormal )
                    iLeft++;
                else
                    iRight++;
            }

            return( Result );
        }

        internal static EntryProximity  EstimateProximity( List<long> Left, List<long> Right )
        {
            int            iLeft = 0, iRight = 0;
            EntryProximity Result = EntryProximity.Document;

            while(( iLeft < Left.Count ) && ( iRight < Right.Count ))
            {
                long leftOff = Left[ iLeft ];
                long rightOff = Right[ iRight ];

                if( MaskEncoder.Sentence( leftOff ) == MaskEncoder.Sentence( rightOff ) )
                {
                    Result = EntryProximity.Sentence;
                    if( isPhraseProximity( MaskEncoder.TokenOrder( leftOff ), MaskEncoder.TokenOrder( rightOff ) ))
                    {
                        Result = EntryProximity.Phrase;
                        break;
                    }
                }
                if( MaskEncoder.OffsetNormal( leftOff ) < MaskEncoder.OffsetNormal( rightOff ) )
                    iLeft++;
                else
                    iRight++;
            }

            return( Result );
        }

        internal static bool  isPhraseProximity( InstanceOffset left, InstanceOffset right )
        {
            return (left.TokenOrder - right.TokenOrder) == -1;
        }

        internal static bool  isPhraseProximity( int left, int right )
        {
            return (left - right) == -1;
        }
    }
    #endregion Proximity Estimator
}