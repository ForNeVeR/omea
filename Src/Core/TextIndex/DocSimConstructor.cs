// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using   System;
using   System.Diagnostics;
using   System.Collections;
using   JetBrains.Omea.Containers;
using   JetBrains.Omea.OpenAPI;
using   JetBrains.DataStructures;

namespace JetBrains.Omea.TextIndex
{
public  class   DocSimConstructor
{
    public  DocSimConstructor() {}
/*
    //-------------------------------------------------------------------------
    //  Compute similarity for the document (represented as DocID), return
    //  pairs (DocID, SimDistance) one pair by one.
    //  Return empty array if no similar is found or document index is illegal.
    //-------------------------------------------------------------------------

    public  static  ArrayList   Run( DocIndexAccessor docIndex, BatchDocIndexAccessor memDocIndex,
                                     TermIndexAccessor termIndex, TermIndexAccessor memTermIndex,
                                     int docID )
    {
        ArrayList   results = new ArrayList();
        SourceDoc = GetDocument( docID, docIndex, memDocIndex );
        if( SourceDoc != null )
        {
            IntArrayList   termIDs = new IntArrayList();
            IntArrayList    docCandidates = new IntArrayList();
            float[]         distances = new float[ ciNearestDocsLimit + 1 ];
            int[]           docIndices = new int  [ ciNearestDocsLimit + 1 ];

            //---------------------------------------------------------------------
            Array.Sort( SourceDoc.Entries );
            RetrieveTermOffsets( SourceDoc, termIDs );
            CollectDocCandidates( docID, termIndex, memTermIndex, termIDs, docCandidates );
            ComputeSimilarity( docIndex, memDocIndex, docCandidates, distances, docIndices );

            //---------------------------------------------------------------------
            for( int i = 1; i <= ciNearestDocsLimit; i++ )
            {
                if( docIndices[ i ] != -1 && DocExist( docIndices[ i ] ))
                    results.Add( new Pair( docIndices[ i ], distances[ i ] ) );
            }
            results.Reverse();

            docIndices = null;
            distances = null;
        }
        return( results );
    }

    protected static DocIndexRecord GetDocument( int docID, DocIndexAccessor docIndex, BatchDocIndexAccessor memDocIndex )
    {
        DocIndexRecord rec = null;
        if( memDocIndex != null && memDocIndex.isDocumentPresent( docID ))
            rec = memDocIndex.GetRecord( docID );
        else
        if( docIndex.isDocumentPresent( docID ))
            rec = docIndex.GetRecord( docID );
        return rec;
    }

    //-------------------------------------------------------------------------
    //  Retrieve a list of term offsets from the record of the document.
    //-------------------------------------------------------------------------
    protected   static void     RetrieveTermOffsets( DocIndexRecord record, IntArrayList TermIndices )
    {
        TermIndices.Clear();
        for( int i = 0; i < record.TermsNumber; i++ )
            TermIndices.Add( record.Entries[ i ].TermID );

        Debug.Assert( TermIndices.Count > 0 );
    }

    //-------------------------------------------------------------------------
    //  Given the array of term offsets, collect all document indices, in which
    //  these terms occur.
    //-------------------------------------------------------------------------

    private static void  CollectDocCandidates( int sourceDocID,
                                               TermIndexAccessor termIndex, TermIndexAccessor memIndex,
                                               IntArrayList termIDs, IntArrayList DocCandidates )
    {
        DocCandidates.Clear();
        TermIndexRecord   record;
        IntHashTableOfInt docIndicesTemp = new IntHashTableOfInt();

        Trace.WriteLine( "DocSimConstructor -- Total " + termIDs.Count + " term ids is analyzed" );
        for( int i = 0; i < termIDs.Count; i++ )
        {
            if( termIndex.TermExist( termIDs[ i ] ))
            {
                record = termIndex.GetRecordByHC( termIDs[ i ] );
                CollectDocCandidatesFromRecord( sourceDocID, record, docIndicesTemp );
            }

            if( memIndex != null && memIndex.TermExist( termIDs[ i ] ))
            {
                record = memIndex.GetRecordByHC( termIDs[ i ] );
                CollectDocCandidatesFromRecord( sourceDocID, record, docIndicesTemp );
            }
        }

        foreach( IntHashTableOfInt.Entry e in docIndicesTemp )
            DocCandidates.Add( e.Key );
    }

    private static void  CollectDocCandidatesFromRecord( int sourceDocID, TermIndexRecord rec,
                                                         IntHashTableOfInt docIndices )
    {
        for( int i = 0; i < rec.DocsNumber; i++ )
        {
            int docIndex = rec.GetEntryAt( i ).DocIndex;
            if(( docIndex != sourceDocID ) && !docIndices.ContainsKey( docIndex ))
                docIndices.Add( docIndex, 1 );
        }
    }

    //-------------------------------------------------------------------------
    //  For every document in the array, compute its distance to the given
    //  one, and keep only the predefined number of MOST close.
    //  Keeping a set of the most close is performed by maintaining the fixed
    //  array, which state is
    //-------------------------------------------------------------------------

    private static void     ComputeSimilarity( DocIndexAccessor docIndex, BatchDocIndexAccessor memDocIndex,
                                               IntArrayList docCandidates,
                                               float[] afl_Distances, int[] ai_DocIndices )
    {
        InitResultSet( afl_Distances, ai_DocIndices );
        for( int i = 0; i < docCandidates.Count; i++ )
        {
            DocIndexRecord  doc2Record = GetDocument( docCandidates[ i ], docIndex, memDocIndex );
            if( doc2Record != null )
            {
                Array.Sort( doc2Record.Entries );
                float   Distance = ComputeDistance( doc2Record );
                UpdateNearestSet( docCandidates[ i ], Distance, afl_Distances, ai_DocIndices );
            }
        }
    }

    protected   static void     InitResultSet( float[] afl_Distances, int[] ai_DocIndices )
    {
        for( int i = 0; i <= ciNearestDocsLimit; i++ )
        {
            afl_Distances[ i ] = cfInfinity;
            ai_DocIndices[ i ] = -1;
        }
    }

    //-------------------------------------------------------------------------
    //  We keep only "ciNearestDocsLimit" number of values, thus
    //  "ciNearestDocsLimit + 1"-th cell is always undefined. Put new value into
    //  the "ciNearestDocsLimit + 1"-th cell (if there is a reason to) and
    //  sorting will find the proper place for it inside the array.
    //-------------------------------------------------------------------------

    protected   static void     UpdateNearestSet( int docID, float Distance,
                                                  float[] distances, int[] docIDs )
    {
        if( Distance > distances[ 0 ] )
        {
            distances[ 0 ] = Distance;
            docIDs[ 0 ] = docID;

            Array.Sort( distances, docIDs );
        }
    }

    //-------------------------------------------------------------------------
    //  For the given pair of the documents, compute the distance between them
    //  using one of the potential metrics:
    //  - Cosine metric (implemented one)
    //  - Descartes distance (not suitable in our case)
    //  Take into the consideration the normalization coefficient accounting
    //  summary lengths of the documents.
    //-------------------------------------------------------------------------

    protected   static float   ComputeDistance( DocIndexRecord record_2 )
    {
        double  Distance = 0.0;
        DocIndexRecord  record_1 = SourceDoc;

        //---------------------------------------------------------------------
        int     i_Left = 0, i_Right = 0;
        double  leftLength = 0.0, rightLength = 0.0;
        while(( i_Left < record_1.TermsNumber ) && ( i_Right < record_2.TermsNumber ))
        {
            DocEntry   left  = record_1.Entries[ i_Left ];
            DocEntry   right = record_2.Entries[ i_Right ];
            leftLength += (double)(left.TfIdf * left.TfIdf);
            rightLength += (double)(right.TfIdf * right.TfIdf);

            if( left.TermID == right.TermID )
            {
                Distance += (double)(left.TfIdf * right.TfIdf);
                i_Left++; i_Right++;
            }
            else
            if( left.TermID < right.TermID )
                i_Left++;
            else
                i_Right++;
        }

        if( i_Left == record_1.TermsNumber )
        {
            for( int i = i_Right + 1; i < record_2.TermsNumber; i++ )
            {
                DocEntry   right = record_2.Entries[ i ];
                rightLength += (double)(right.TfIdf * right.TfIdf);
            }
        }
        else
        if( i_Right == record_2.TermsNumber )
        {
            for( int i = i_Left + 1; i < record_1.TermsNumber; i++ )
            {
                DocEntry   left = record_1.Entries[ i ];
                leftLength += (double)(left.TfIdf * left.TfIdf);
            }
        }
        Distance = ( Distance / Math.Sqrt( leftLength * rightLength ));
        return( (float)Distance );
    }

    private static bool DocExist( int docID )
    {
        return Core.ResourceStore.TryLoadResource( docID ) != null;
    }

    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    private const   int     ciNearestDocsLimit = 10;
    private const   float   cfInfinity = 10e-10F;
    private static  DocIndexRecord  SourceDoc;
*/
}
}
