// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using   System;
using   System.IO;
using   System.Collections;
using   NUnit.Framework;
using   CommonTests;
using   JetBrains.Omea.TextIndex;
using   JetBrains.Omea.OpenAPI;
using   JetBrains.Omea.AsyncProcessing;
using   JetBrains.Omea.Diagnostics;

namespace TextIndexTests
{
    [TestFixture]
    public class TextIndexTest: MyPalDBTests
    {
        public const int ThreadsNumber = 2;
        private readonly AsyncProcessor[]  _processors = new AsyncProcessor[ ThreadsNumber ];
        private FullTextIndexer     indexer;
        private int                 UpdatePhaseCounter;

        //---------------------------------------------------------------------
        private static void ExceptionHandler( Exception e )
        {
            if( e is ResourceDeletedException )
                return;

            Exception innerException = e.InnerException;
            if ( innerException != null )
                ExceptionHandler( innerException );
            else
            {
                Tracer._TraceException( e );
                Console.WriteLine( e.Message + e.StackTrace );
            }
        }

        #region SetUp/TearDown
        [SetUp] public void SetUp()
        {
            try
            {
                string[] files = Directory.GetFiles(".", "_*");
                foreach ( string fileName in files )
                {
                    File.Delete( fileName );
                }
            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.Message );
            }

            OMEnv.DataDir = ".\\Data";
            InitStorage();
            for( int i = 0; i < _processors.Length; ++i )
            {
                _processors[ i ] = new AsyncProcessor( new AsyncExceptionHandler( ExceptionHandler ), false );
            }

            MockPluginEnvironment env = new MockPluginEnvironment( _storage );
            env.SetCoreProps( new MockCoreProps() );

            indexer = new FullTextIndexer();
            indexer.Initialize();
            Core.ResourceStore.ResourceTypes.Register( "TestType", "TestType", "Name", ResourceTypeFlags.Normal );
        }

        [TearDown] public void TearDown()
        {
            CloseStorage();
            indexer.CloseIndices();
            indexer.DiscardTextIndexImpl( false );
            Word.DisposeTermTrie();

            try
            {
                string[] files = Directory.GetFiles(".", "_*");
                foreach ( string fileName in files )
                {
                    File.Delete( fileName );
                }
            }
            catch ( Exception exc )
            {
                Console.WriteLine( exc.Message );
                Assert.Fail( exc.Message );
            }
        }
        #endregion SetUp/TearDown

        private static void AssertIfTrue( string message, bool condition )
        {
            Assert.IsTrue( condition, message );
        }

        [Test] public void EmptyIndex()
        {
            indexer.EndBatchUpdate();
            Assert.IsTrue( !indexer.IsDocumentPresentInternal( 100 ) );
        }

        [Test] public void AddNewDocumentWithZeroID()
        {

            indexer.AddDocumentFragment( 0, "test" );
            indexer.EndBatchUpdate();
        }

        [Test] public void AddNewDocument()
        {
            IResource newRes = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes.Id, "one two three fourplay" );
            indexer.EndBatchUpdate();
            AssertIfTrue( "document we have added is found in index", indexer.IsDocumentPresentInternal( newRes.Id ) );

            Entry[]  result = indexer.ProcessQueryInternal( "fourplay" );
            AssertIfTrue( "Size of query result does not coinside with expected one", result.Length == 1 );
            AssertIfTrue( "Result document is not in the list of query result", result[ 0 ].DocIndex == newRes.Id );
        }

        [Test] public void AddDocumentWithEmptyBody()
        {
            IResource newRes = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes.Id, "" );
            indexer.EndBatchUpdate();
            AssertIfTrue( "empty document we have added is found in index !!!", !indexer.IsDocumentPresentInternal( newRes.Id ) );
        }

        [Test] public void DeleteDocument()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            IResource newRes2 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "one two three fourplay" );
            indexer.AddDocumentFragment( newRes2.Id, "fourplay threeplay. " );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "fourplay" );
            AssertIfTrue( "Size of query result does not equal 2 (before removal)", result.Length == 2 );

            indexer.DeleteDocument( newRes1.Id );
            result = indexer.ProcessQueryInternal( "fourplay" );
            AssertIfTrue( "Size of query result does not equal 1 (after removal)", result.Length == 1 );
        }

        [Test] public void DeleteLastDocument()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            IResource newRes2 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "one two three fourplay" );
            indexer.AddDocumentFragment( newRes2.Id, "fourplay threeplay. " );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "fourplay" );
            AssertIfTrue( "Size of query result does not equal 2 (before removal)", result.Length == 2 );

            indexer.DeleteDocument( newRes1.Id );
            indexer.DeleteDocument( newRes2.Id );
            result = indexer.ProcessQueryInternal( "fourplay" );
            AssertIfTrue( "We should not find anything", result == null );
        }

        [Test] public void DeleteDocumentAddDocumentWithSameTerms()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            IResource newRes2 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "one two three fourplay" );
            indexer.AddDocumentFragment( newRes2.Id, "fourplay threeplay. " );
            indexer.EndBatchUpdate();

            //--
            Entry[]  result = indexer.ProcessQueryInternal( "fourplay" );
            AssertIfTrue( "Size of query result does not equal 2 (before removal)", result.Length == 2 );

            //--
            indexer.DeleteDocument( newRes1.Id );
            result = indexer.ProcessQueryInternal( "fourplay" );
            AssertIfTrue( "Size of query result does not equal 1 (after removal)", result.Length == 1 );

            //--
            IResource newRes3 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes3.Id, "one fourplay" );
            indexer.EndBatchUpdate();
            result = indexer.ProcessQueryInternal( "fourplay" );
            AssertIfTrue( "Size of query result does not equal 2 (after removal and addition)", result.Length == 2 );

            //--
            result = indexer.ProcessQueryInternal( "one" );
            AssertIfTrue( "Unexpected error - result list is empty - one elemented is expected", result != null );
            AssertIfTrue( "Size of query result does not equal 1 (after removal double addition)", result.Length == 1 );
        }

        [Test] public void TestMergedSearchableResources()
        {
            //  1. Construct Main chunk. Test that we get necessary initial documents
            //  2. Add 990 documents. Ensure that all of them are fired.
            //  3. Add 11 documents. Ensure that all 11 are in the event parameter
            //     (check cross-body acceptance).
            //  4. Add 10 documents. Ensure that all 10 are fired.
            //  5. Flush index. Ensure no document is fired.
            indexer.NextUpdateFinished += UpdateChunkHandler;

            UpdatePhaseCounter = 1;
            indexer.AddDocumentFragment( 10000, "one two three fourplay" );
            indexer.AddDocumentFragment( 10001, "fourplay threeplay. " );
            indexer.EndBatchUpdate();

            UpdatePhaseCounter = 2;
            for( int i = 1; i < 991; i++ )
                indexer.AddDocumentFragment( i, "one fourplay" );
            indexer.EndBatchUpdate();

            UpdatePhaseCounter = 3;
            for( int i = 991; i < 1002; i++ )
                indexer.AddDocumentFragment( i, "one fourplay" );
            indexer.EndBatchUpdate();
            UpdatePhaseCounter = 4;
            for( int i = 0; i < 10; i++ )
                indexer.AddDocumentFragment( i + 10002, "one fourplay" );
            indexer.EndBatchUpdate();
            UpdatePhaseCounter = 5;
            indexer.CloseIndices();
        }

        [Test] public void AddDocumentWithRepeatingID()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            IResource newRes2 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "token1 token2 token3" );
            indexer.AddDocumentFragment( newRes2.Id, "token8 token9 token7" );
            indexer.AddDocumentFragment( newRes1.Id, "token3 token4 token5" );
            indexer.EndBatchUpdate();
            AssertIfTrue( "document we have added is not found in index", indexer.IsDocumentPresentInternal( newRes1.Id ) );

            Entry[]  result = indexer.ProcessQueryInternal( "token1" );
            AssertIfTrue( "We must find one token from the first instance of the document", result.Length == 1 );
        }

        [Test] public void AddEmptyBatchUpdate()
        {
            indexer.AddDocumentFragment( 100, "token1 token2 token3" );
            indexer.AddDocumentFragment( 200, "token8 token9 token7" );
            indexer.AddDocumentFragment( 100, "token3 token4 token5" );
            indexer.EndBatchUpdate();
            indexer.AddDocumentFragment( 300, "" );
            indexer.EndBatchUpdate();
            indexer.AddDocumentFragment( 400, "" );
            indexer.EndBatchUpdate();
            indexer.CloseIndices();
        }

        [Test] public void SeveralDocumentVersionsInUpdateMode()
        {
            indexer.AddDocumentFragment( 100, "token1 " );
            indexer.AddDocumentFragment( 100, "token1 " );
            indexer.AddDocumentFragment( 200, "token8 " );
            indexer.EndBatchUpdate();

            indexer.AddDocumentFragment( 300, "token10 " );
            indexer.AddDocumentFragment( 400, "token40 " );
            indexer.AddDocumentFragment( 300, "token50 " );
            indexer.EndBatchUpdate();

            indexer.AddDocumentFragment( 500, "token100 token200" );
            indexer.AddDocumentFragment( 600, "token400" );
            indexer.AddDocumentFragment( 500, "token100" );

            indexer.EndBatchUpdate();
        }

        [Test] public void SeveralEmptyDocumentVersionsInUpdateMode()
        {
            indexer.AddDocumentFragment( 100, "token1 " );
            indexer.AddDocumentFragment( 100, "token1 " );
            indexer.AddDocumentFragment( 200, "token8 " );
            indexer.EndBatchUpdate();

            indexer.AddDocumentFragment( 300, " " );
            indexer.AddDocumentFragment( 400, "token40 " );
            indexer.AddDocumentFragment( 300, "token50 " );
            indexer.EndBatchUpdate();
        }

        [Test] public void SeveralEmptyDocumentVersionsInMainMode()
        {
            indexer.AddDocumentFragment( 100, "token1 " );
            indexer.AddDocumentFragment( 200, "token8 " );
            indexer.AddDocumentFragment( 100, "token2 " );
            indexer.AddDocumentFragment( 200, "token9 " );
            indexer.EndBatchUpdate();

            indexer.AddDocumentFragment( 300, " " );
            indexer.AddDocumentFragment( 400, "token40 " );
            indexer.EndBatchUpdate();
        }

        //---------------------------------------------------------------------
        //  !!! for this test tokens buffer size in the FullTextIndexer must
        //  be set to 7 !!!
        //---------------------------------------------------------------------
        [Test] public void SeveralExtraLargeSequentially()
        {
            int[] ids = new int[ 10 ];
            for( int i = 0; i < 10; ++i )
            {
                ids[ i ] = Core.ResourceStore.NewResource( "TestType" ).Id;
            }
            indexer.AddDocumentFragment( ids[ 0 ], "token2 token1 token3" );
            indexer.EndBatchUpdate();

            //-----------------------------------------------------------------
            indexer.AddDocumentFragment( ids[ 1 ], "token2 token3 token4 token5 token6 token999 token998 toekn997" );
            indexer.AddDocumentFragment( ids[ 2 ], "token1 token2 token3 token7 token8 token9 token10" );
            indexer.AddDocumentFragment( ids[ 3 ], "token1 token1 token1 token1 token1 token1 token1 token1 token11" );
            indexer.AddDocumentFragment( ids[ 4 ], "token1 token1 token2 token1 token1 token1 token3 token1 token1 token1 token1 token1 token12" );
            indexer.AddDocumentFragment( ids[ 5 ], "token1 token2 token13" );
            indexer.AddDocumentFragment( ids[ 6 ], "token1 token2 token14" );
            indexer.AddDocumentFragment( ids[ 7 ], "token1 token2 token15" );
            indexer.AddDocumentFragment( ids[ 8 ], "token1 token2 token16" );
            indexer.AddDocumentFragment( ids[ 9 ], "token1 token2 token17" );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "token1" );
            Assertion.AssertEquals( 9, result.Length );
        }

        //---------------------------------------------------------------------
        //  !!! for this test tokens buffer size in the FullTextIndexer must
        //  be set to 7 !!!
        //---------------------------------------------------------------------
        [Test] public void TestEmptyLargeEmptyLargeSequence()
        {
            indexer.AddDocumentFragment( 100, "token1 token2 token3" );
            indexer.EndBatchUpdate();

            //-----------------------------------------------------------------
            indexer.AddDocumentFragment( 200, "token1 token2 token3 token1 token2 token1 token2 token1 token2" );
            indexer.AddDocumentFragment( 300, "token1 token2 token3 token1 token2 token1 token2 token1 token2" );
            indexer.EndBatchUpdate();
            indexer.AddDocumentFragment( 400, "   " );
            indexer.AddDocumentFragment( 500, "token1 token2 token3 token1 token2 token1 token2 token1 token2" );
            indexer.AddDocumentFragment( 600, "   " );
            indexer.AddDocumentFragment( 700, "token1 token2 token3 token1 token2 token1 token2 token1 token2" );
            indexer.EndBatchUpdate();
        }

//        [Test, Ignore("test fails")] public void  DictionaryServer()
        [Test] public void  DictionaryServer()
        {
            string[] dics = OMEnv.DictionaryFileNames;
            Hashtable derivateBases = new Hashtable();

            for( int i = dics.Length - 1; i >= 0; i-- )
            {
                string  dic = dics[ i ];
                string  str;
                int     index, length, counter = 0;

                StreamReader sr = new StreamReader( dic, System.Text.Encoding.Default );
                while(( str = sr.ReadLine() ) != null )
                {
                    str = str.ToLower();
                    if( !IsValidString( str ))
                        continue;
                    if( str.IndexOf( '$' ) == -1 )
                    {
                        if( !derivateBases.ContainsKey( str ) )
                        {
                            OMEnv.DictionaryServer.FindLowerBound( str, out index, out length );
                            AssertIfTrue( "Ordinary lexeme [" + str + "] is not found in dic [" + dic + "]", index >= 0 );
                        }
                    }
                    else
                    {
                        string prefix = str.Substring( 0, str.IndexOf( '$' ));
                        derivateBases[ prefix ] = 1;

                        OMEnv.DictionaryServer.FindLowerBound( prefix, out index, out length );
                        AssertIfTrue( "Mapped lexeme [" + str + "] is not found in dic [" + dic + "]",
                                index < -1 || index == 0 || counter == 1 );

                        string  source = OMEnv.DictionaryServer.GetDicString( -index, length );
                        int     delim = source.IndexOf( '$' );
                        AssertIfTrue( "Returned result is not a map: " + source, delim != -1 );
                        AssertIfTrue( "Mapped lexeme [" + prefix + "] is not found against " +
                                source.Substring( 0, delim), prefix == source.Substring( 0, delim ));
                    }
                    counter++;
                }
                sr.Close();
            }
        }

        //  1. find ordinary lexeme which is in the dictionary
        //  2. find mapped lexeme
        //  3. find wordform after morphoanalysis
        //  4. find mapped wordform after morphoanalysis
        //  4. test several rules in the morphogrammar
        //  Test reverse transformations - from lexeme to wordforms
        [Test] public void  LexemeConstructor()
        {
            Word word = new Word();
            LexemeConstructor constructor = new LexemeConstructor( OMEnv.ScriptMorphoAnalyzer,
                                                                   OMEnv.DictionaryServer );

            word.Token = "paper";
            constructor.NormalizeToken( word );
            AssertIfTrue( "Did not find ordinary lexeme which is in the dictionary", word.Token == "paper" );

            word.Token = "brightly";
            constructor.NormalizeToken( word );
            AssertIfTrue( "Did not find mapped lexeme: " + word.Token, word.Token == "bright" );

            word.Token = "chessboards";
            constructor.NormalizeToken( word );
            AssertIfTrue( "Did not find wordform after morphoanalysis", word.Token == "chessboard" );

            //-----------------------------------------------------------------
            word.Token = "computerizations";
            constructor.NormalizeToken( word );
            Console.WriteLine( word.Token );
            AssertIfTrue( "Did not find mapped wordform after morphoanalysis", word.Token == "computerize" );

            int variant = RetrieveIndexFromBits( word.StartOffset );
            string str = OMEnv.DictionaryServer.GetLexemeMapping( "computerize", variant );
            AssertIfTrue( "Reversed transoformations failed", str == "computerizations" );

            //-----------------------------------------------------------------
            word.Token = "garbage-garbage";
            constructor.NormalizeToken( word );
            Console.WriteLine( word.Token );
            AssertIfTrue( "Did not find mapped wordform after morphoanalysis", word.Token == "garbage-garbage" );

            //-----------------------------------------------------------------
            word.Token = "paid";
            constructor.NormalizeToken( word );
            Console.WriteLine( word.Token );
            AssertIfTrue( "Did not find wordform [paid] after morphoanalysis", word.Token == "pay" );

            word.Token = "crabbing";
            constructor.NormalizeToken( word );
            Console.WriteLine( word.Token );
            AssertIfTrue( "Did not find wordform [crabbing] after morphoanalysis: " + word.Token, word.Token == "crab" );

            word.Token = "swimming";
            constructor.NormalizeToken( word );
            Console.WriteLine( word.Token );
            AssertIfTrue( "Did not find wordform [swimming] after morphoanalysis", word.Token == "swim" );

            word.Token = "prices";
            constructor.NormalizeToken( word );
            Console.WriteLine( word.Token );
            AssertIfTrue( "Did not find wordform [swimming] after morphoanalysis", word.Token == "price" );

            word.Token = "ies";
            constructor.NormalizeToken( word );
            Console.WriteLine( word.Token );
            AssertIfTrue( "Did not find wordform [swimming] after morphoanalysis", word.Token == "ies" );
        }

        private static bool IsValidString( string str )
        {
            foreach( char ch in str )
            {
//                UnicodeCategory ue = Char.GetUnicodeCategory( ch );
//                if(( ch != '$' && ch != '-' && ue != UnicodeCategory.LowercaseLetter ) || (int)ch > 128 )
                if((int)ch > 128 )
                    return false;
            }
            return true;
        }

        private static int  RetrieveIndexFromBits( uint Mask )
        {
            int     Result = 0;
            if(( Mask & 0x01000000 ) > 0 )
                Result += 1;
            if(( Mask & 0x02000000 ) > 0 )
                Result += 2;
            if(( Mask & 0x20000000 ) > 0 )
                Result += 4;
            if(( Mask & 0x40000000 ) > 0 )
                Result += 8;
            if(( Mask & 0x80000000 ) > 0 )
                Result += 16;
            return( Result );
        }

        private void UpdateChunkHandler( object sender, DocsArrayArgs docIDs )
        {
            switch( UpdatePhaseCounter )
            {
                case 1:
                {
                    Console.WriteLine( "Amount of docs in the phase 1 is " + docIDs.GetDocuments().Length );
                    AssertIfTrue( "Amount of documents in the phase 1 must be 2", docIDs.GetDocuments().Length == 2 );
                    break;
                }
                case 2:
                {
                    Console.WriteLine( "Amount of docs in the phase 2 is " + docIDs.GetDocuments().Length );
                    AssertIfTrue( "Amount of documents in the phase 2 must be 990", docIDs.GetDocuments().Length == 990 );
                    break;
                }
                case 3:
                {
                    Console.WriteLine( "Amount of docs in the phase 3 is " + docIDs.GetDocuments().Length );
                    AssertIfTrue( "Amount of documents in the phase 3 must be 11", docIDs.GetDocuments().Length == 11 );
                    break;
                }
                case 4:
                {
                    Console.WriteLine( "Amount of docs in the phase 4 is " + docIDs.GetDocuments().Length );
                    AssertIfTrue( "Amount of documents in the phase 4 must be 10", docIDs.GetDocuments().Length == 10 );
                    break;
                }
                case 5:
                {
                    AssertIfTrue( "We must not get anything on phase 5 ", false );
                    break;
                }
            }
        }
    }
}
