// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using NUnit.Framework;
using CommonTests;
using JetBrains.Omea.TextIndex;
using System.IO;

namespace StressTests
{
	[TestFixture]
    public class TextIndexStressTests: MyPalDBTests
	{
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
        }

        [TearDown] public void TearDown()
        {
            CloseStorage();

            File.Delete( OMEnv.TermIndexFileName );
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
        }

        [Test] public void ConsistencyAfterUpdates()
        {
            MockPluginEnvironment env = new MockPluginEnvironment( _storage );
            env = env;
            FullTextIndexer indexer = new FullTextIndexer();

            //-----------------------------------------------------------------
            indexer.AddDocumentFragment( 100, "token1 token2 token3" );
            indexer.AddDocumentFragment( 200, "token1 token2 token3" );
            indexer.EndBatchUpdate();
            Entry[]  aentry_Result = indexer.ProcessQueryInternal( "token1" );
            Assert.IsTrue( aentry_Result.Length == 2, "Failed to find all documents" );

            //-----------------------------------------------------------------
            indexer.AddDocumentFragment( 300, "token1 token2 token3" );
            indexer.AddDocumentFragment( 400, "token1 token4 token5" );
            indexer.EndBatchUpdate();
            aentry_Result = indexer.ProcessQueryInternal( "token1" );
            Assert.IsTrue( aentry_Result.Length == 4, "Failed to find all documents" );

            //-----------------------------------------------------------------
            indexer.AddDocumentFragment( 500, "token1 token2 token3" );
            indexer.AddDocumentFragment( 600, "token1 token4 token5" );
            indexer.EndBatchUpdate();
            aentry_Result = indexer.ProcessQueryInternal( "token1" );
            Assert.IsTrue( aentry_Result.Length == 6, "Failed to find all documents" );

            //-----------------------------------------------------------------
            indexer.DeleteDocument( 100 );
            aentry_Result = indexer.ProcessQueryInternal( "token1" );
            Assert.IsTrue( aentry_Result.Length == 5, "Failed to find all documents" );

            indexer.DeleteDocument( 600 );
            aentry_Result = indexer.ProcessQueryInternal( "token1" );
            Assert.IsTrue( aentry_Result.Length == 4, "Failed to find all documents" );

            //-----------------------------------------------------------------
            for( int i = 0; i < 200000; i++ )
            {
                indexer.AddDocumentFragment( i + 1000, "Term" + i );
                if( i % 100000 == 0 )
                {
                    indexer.AddDocumentFragment( i + 1000, "Token1" );
                    Console.Write( "." );
                }
            }
            indexer.EndBatchUpdate();
            aentry_Result = indexer.ProcessQueryInternal( "token1" );
            Assert.IsTrue( aentry_Result.Length == 6, "Failed to find all documents" );

            //-----------------------------------------------------------------
            indexer.AddDocumentFragment( 300, "token1 token2 token3" );
            indexer.AddDocumentFragment( 400, "token1 token4 token5" );
            indexer.EndBatchUpdate();
            aentry_Result = indexer.ProcessQueryInternal( "token1" );
            Assert.IsTrue( aentry_Result.Length == 8, "Failed to find all documents" );

            indexer.DiscardTextIndex();
        }

        [Test] public void  TestMemoryAccessorsMerge()
        {
            MockPluginEnvironment env = new MockPluginEnvironment( _storage );
            env = env;
            FullTextIndexer indexer = new FullTextIndexer();
            indexer.AddDocumentFragment( 1, "one two\n\nthree fourplay" );
            indexer.AddDocumentFragment( 2, "fourplay.\n\nthreeplay. " );
            indexer.EndBatchUpdate();

            for( int i = 3; i < 50000; i++ )
                indexer.AddDocumentFragment( i, "Term" + i );

            Console.WriteLine( "Finished addition" );
            indexer.EndBatchUpdate();
            Console.WriteLine( "Finished Linking" );
            Entry[]  aentry_Result;
            for( int i = 3; i < 50000; i++ )
            {
                aentry_Result = indexer.ProcessQueryInternal( "Term" + i );
                Assert.IsTrue( aentry_Result.Length == 1, "We must find exactly one instance of every term" );
            }
            Console.WriteLine( "Finished quering" );
            indexer.DiscardTextIndex();
        }
    }
}
