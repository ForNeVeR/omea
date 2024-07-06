// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using CommonTests;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.TextIndex;
using NUnit.Framework;

namespace TextIndexTests
{
    [TestFixture]
    public class MatchQueryTest : MyPalDBTests
    {
        public const int ThreadsNumber = 2;

        private readonly AsyncProcessor[]  _processors = new AsyncProcessor[ ThreadsNumber ];
        private FullTextIndexer     indexer;

        private static int _savedID;

        #region Startup
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

        [SetUp] public void SetUp()
        {
            try
            {
                string[] files = System.IO.Directory.GetFiles(".", "_*");
                foreach ( string fileName in files )
                {
                    System.IO.File.Delete( fileName );
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
                string[] files = System.IO.Directory.GetFiles(".", "_*");
                foreach ( string fileName in files )
                {
                    System.IO.File.Delete( fileName );
                }
            }
            catch ( Exception exc )
            {
                Console.WriteLine( exc.Message );
                Assert.Fail( exc.Message );
            }
        }
        #endregion Startup

        #region SimpleSequence
        [Test] public void TestSimpleSequence()
        {
            //  This is the main goal of this testing.
            indexer.ResourceProcessed += Handler_SimpleSequenceTest;

            IResource newRes = Core.ResourceStore.NewResource( "TestType" );
            _savedID = newRes.Id;
            indexer.AddDocumentFragment( newRes.Id, "token1.token2. token3 token4.token5. " );
            indexer.EndBatchUpdate();

            indexer.ResourceProcessed -= Handler_SimpleSequenceTest;
        }

        void Handler_SimpleSequenceTest(object sender, EventArgs e)
        {
            bool isMatched = indexer.MatchQuery( "token2", _savedID, 10 );
            Assert.IsTrue( isMatched, "Query [token2] must succeed" );

            isMatched = indexer.MatchQuery( "token4", _savedID, 10 );
            Assert.IsTrue( isMatched, "Query [token4] must succeed" );

            isMatched = indexer.MatchQuery( "token7", _savedID, 10 );
            Assert.IsTrue( !isMatched, "Query [token7] must fail" );
        }
        #endregion SimpleSequence

        #region Conjunction
        [Test] public void TestConjunction()
        {
            //  This is the main goal of this testing.
            indexer.ResourceProcessed += Handler_ConjunctionTest;

            IResource newRes = Core.ResourceStore.NewResource( "TestType" );
            _savedID = newRes.Id;
            indexer.AddDocumentFragment( newRes.Id, "token1 token2." );
            indexer.AddDocumentFragment( newRes.Id, " token3 token4. token5. " );
            indexer.EndBatchUpdate();

            indexer.ResourceProcessed -= Handler_ConjunctionTest;
        }

        void Handler_ConjunctionTest(object sender, EventArgs e)
        {
            bool isMatched = indexer.MatchQuery( "token2 token1", _savedID, 10 );
            Assert.IsTrue( isMatched, "Query [token2 token1] must succeed" );

            isMatched = indexer.MatchQuery( "token2 token4", _savedID, 11 );
            Assert.IsTrue( isMatched, "Query [token2 token4] must succeed" );

            isMatched = indexer.MatchQuery( "token4 token6", _savedID, 12 );
            Assert.IsTrue( !isMatched, "Query [token4 token6] must fail" );
        }
        #endregion Conjunction

        #region Disjunction
        [Test] public void TestDisjunction()
        {
            //  This is the main goal of this testing.
            indexer.ResourceProcessed += Handler_DisjunctionTest;

            IResource newRes = Core.ResourceStore.NewResource( "TestType" );
            _savedID = newRes.Id;
            indexer.AddDocumentFragment( newRes.Id, "token1 token2." );
            indexer.AddDocumentFragment( newRes.Id, " token3 token4. token5. " );
            indexer.EndBatchUpdate();

            indexer.ResourceProcessed -= Handler_DisjunctionTest;
        }

        void Handler_DisjunctionTest(object sender, EventArgs e)
        {
            bool isMatched = indexer.MatchQuery( "token2 or token5", _savedID, 10 );
            Assert.IsTrue( isMatched, "Query [token2 or token5] must succeed" );

            isMatched = indexer.MatchQuery( "tokenX or token4", _savedID, 11 );
            Assert.IsTrue( isMatched, "Query [tokenX or token4] must succeed" );

            isMatched = indexer.MatchQuery( "token4 or tokenX", _savedID, 12 );
            Assert.IsTrue( isMatched, "Query [token4 or tokenX] must succeed" );

            isMatched = indexer.MatchQuery( "tokenY or tokenX", _savedID, 13 );
            Assert.IsTrue( !isMatched, "Query [tokenY or tokenX] must fail" );
        }
        #endregion Disjunction

        #region Different Proximities
        [Test] public void TestProximities()
        {
            //  This is the main goal of this testing.
            indexer.ResourceProcessed += Handler_ProximitiesTest;

            IResource newRes = Core.ResourceStore.NewResource( "TestType" );
            _savedID = newRes.Id;
            indexer.AddDocumentHeading( newRes.Id, "severall tokens in the subject" );
            indexer.AddDocumentFragment( newRes.Id, "token1 token20." );
            indexer.AddDocumentFragment( newRes.Id, " token8 token9  token10. " );
            indexer.EndBatchUpdate();

            indexer.ResourceProcessed -= Handler_ProximitiesTest;
        }

        void Handler_ProximitiesTest(object sender, EventArgs e)
        {
            bool isMatched = indexer.MatchQuery( "severall near subject", _savedID, 10 );
            Assert.IsTrue( isMatched, "Query [several near subject] must succeed" );

            isMatched = indexer.MatchQuery( "token8 near token9", _savedID, 11 );
            Assert.IsTrue( isMatched, "Query [token8 near token9] must succeed" );

            isMatched = indexer.MatchQuery( "severall near token20", _savedID, 12 );
            Assert.IsTrue( !isMatched, "Query [several near token20] must fail" );

            isMatched = indexer.MatchQuery( "\"token8 token10\"", _savedID, 13 );
            Assert.IsTrue( !isMatched, "Query [\"token8 token10\"] must fail" );
        }
        #endregion Different Proximities
    }
}
