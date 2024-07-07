// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using OmniaMeaBaseTests;
using JetBrains.Omea.Containers;
using DBIndex;
using NUnit.Framework;

namespace StressTests
{
    [TestFixture]
    public class BTreeStressTests
	{
        private static string _indexFileName = "btree_test.btree_test";
        private void RemoveFiles()
        {
            try
            {
                string[] files = System.IO.Directory.GetFiles( ".", "*.btree_test" );
                foreach ( string fileName in files )
                {
                    System.IO.File.Delete( fileName );
                }
            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.Message );
            }
        }

        [SetUp]
        public void SetUp()
        {
            RemoveFiles();
        }

        [TearDown]
        public void TearDown()
        {
            RemoveFiles();
        }

        private bool FindOffset( IntArrayList offsets, int offset )
        {
            foreach ( int off in offsets )
            {
                if ( off.Equals( offset ) ) return true;
            }
            return false;
        }

        [Test]
        public void BatchSearching()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            bTree.SetCacheSize( 2 );
            bTree.Open();
            for( int i = 0; i < 10000; i++ )
            {
                bTree.InsertKey( new TestKey( i ), i );
            }
            Assert.AreEqual( 10000, bTree.Count );
            IntArrayList offsets = new IntArrayList();
            for( int i = 0; i < 10000; i++ )
            {
                bTree.DeleteKey( new TestKey( i ), i );
                offsets.Clear();
                bTree.SearchForRange( new TestKey( i ), new TestKey( Int32.MaxValue ), offsets );
                Assert.AreEqual( 10000 - i - 1, offsets.Count );
                Assert.AreEqual( false, FindOffset( offsets, i ) );
            }
            Assert.AreEqual( 0, bTree.Count );

            bTree.Close();
            bTree.Open();
            Assert.AreEqual( 0, bTree.Count );
            bTree.Close();
        }

        [Test]
        public void RangeSearching3()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            bTree.SetCacheSize( 2 );
            bTree.Open();
            for( int i = 0; i < 100000; i++ )
            {
                bTree.InsertKey( new TestKey( 2 ), i );
            }
            IntArrayList offsets = new IntArrayList();
            bTree.SearchForRange( new TestKey( 2 ), new TestKey( 2 ), offsets );

            Assert.AreEqual( true, FindOffset( offsets, 9000 ) );
            bTree.DeleteKey( new TestKey( 2 ), 9000 );

            offsets.Clear();
            bTree.SearchForRange( new TestKey( 2 ), new TestKey( 2 ), offsets );
            Assert.AreEqual( false, FindOffset( offsets, 9000 ) );

            Assert.AreEqual( true, FindOffset( offsets, 1 ) );
            bTree.DeleteKey( new TestKey( 2 ), 1 );
            offsets.Clear();
            bTree.SearchForRange( new TestKey( 2 ), new TestKey( 2 ), offsets );
            Assert.AreEqual( false, FindOffset( offsets, 1 ) );

            bTree.Close();
        }

        [Test]
        public void TestRBInsideBTree()
        {
            int test = 0;
            int maxCount = 1;//40
            int attempts = 2;//7
            while ( true )
            {
                test++;
                TestKey keyFactory = new TestKey();
                IBTree bTree = new /*BTree*/OmniaMeaBTree( test.ToString() + ".btree_test", keyFactory );
                bTree.SetCacheSize( 2 );

                IntArrayList offsets = new IntArrayList();
                maxCount++;
                if ( maxCount > 40 )
                {
                    maxCount = 1;
                    attempts++;
                }
                if ( attempts > 3 ) break;
                Console.WriteLine( "Attempts = " + attempts.ToString() + " maxCount = " + maxCount.ToString() );
                for ( int j = 0; j < attempts; j++ )
                {
                    bTree.Open();
                    for ( int i = 0; i < maxCount; i++ )
                    {
                        bTree.InsertKey( new TestKey( i ), i );
                        offsets.Clear();
                        bTree.SearchForRange( new TestKey( i ), new TestKey( i ), offsets );
                        Assert.AreEqual( 1, offsets.Count );
                        Assert.AreEqual( i, offsets[0] );
                        offsets.Clear();
                        bTree.SearchForRange( new TestKey( 0 ), new TestKey( maxCount ), offsets );
                        Assert.AreEqual( i + 1, offsets.Count );
                        int expectedOffset = 0;
                        foreach ( int offset in offsets )
                        {
                            Assert.AreEqual( expectedOffset, offset );
                            expectedOffset++;
                        }
                    }
                    for ( int i = 0; i < maxCount; i++ )
                    {
                        bTree.DeleteKey( new TestKey( i ), i );
                        offsets.Clear();
                        bTree.SearchForRange( new TestKey( i ), new TestKey( i ), offsets );
                        Assert.AreEqual( 0, offsets.Count );
                        offsets.Clear();
                        bTree.SearchForRange( new TestKey( 0 ), new TestKey( maxCount ), offsets );
                        Assert.AreEqual( maxCount - i - 1, offsets.Count );
                        int expectedOffset = 0;
                        foreach ( int offset in offsets )
                        {
                            Assert.AreEqual( expectedOffset + i + 1, offset );
                            expectedOffset++;
                        }
                    }
                    for ( int i = maxCount; i > 0; i-- )
                    {
                        bTree.InsertKey( new TestKey( i ), i );
                        offsets.Clear();
                        bTree.SearchForRange( new TestKey( i ), new TestKey( i ), offsets );
                        Assert.AreEqual( 1, offsets.Count );
                        Assert.AreEqual( i, offsets[0] );
                        offsets.Clear();
                        bTree.SearchForRange( new TestKey( 0 ), new TestKey( maxCount ), offsets );
                        Assert.AreEqual( maxCount - i + 1, offsets.Count );
                        int expectedOffset = i;
                        foreach ( int offset in offsets )
                        {
                            Assert.AreEqual( expectedOffset, offset );
                            expectedOffset++;
                        }
                    }
                    for ( int i = maxCount; i > 0; i-- )
                    {
                        bTree.DeleteKey( new TestKey( i ), i );
                        offsets.Clear();
                        bTree.SearchForRange( new TestKey( i ), new TestKey( i ), offsets );
                        Assert.AreEqual( 0, offsets.Count );
                        offsets.Clear();
                        bTree.SearchForRange( new TestKey( 0 ), new TestKey( maxCount ), offsets );
                        Assert.AreEqual( i - 1, offsets.Count );
                        int expectedOffset = 0;
                        foreach ( int offset in offsets )
                        {
                            Assert.AreEqual( expectedOffset + 1, offset );
                            expectedOffset++;
                        }
                    }
                    bTree.Close();
                }
            }
        }

        [Test]
        public void SuccessiveClosingOpeningTest()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            bTree.SetCacheSize( 16 );
            bTree.Open();
            const int bTreeSize = 200003; // !!!!! PRIME NUMBER !!!!!
            for( int i = 0; i < bTreeSize; ++i )
            {
                bTree.InsertKey( new TestKey( i ), i );
            }
            Random rnd = new Random();
            int insert = rnd.Next( bTreeSize ) + bTreeSize;
            int delete = rnd.Next( bTreeSize );
            for( int i = 0; i < 1000; ++i )
            {
                bTree.Close();
                bTree.Open();
                if( bTreeSize != bTree.Count )
                {
                    throw new Exception( "After Open() bTreeSize != bTree.Count, i = " + i );
                }
                for( int j = 0; j < 50; ++j )
                {
                    bTree.InsertKey( new TestKey( insert ), insert );
                    bTree.DeleteKey( new TestKey( delete ), delete );
                    insert = ( insert + 50000 ) % bTreeSize + bTreeSize;
                    delete = ( delete + 50000 ) % bTreeSize;
                }
                if( bTreeSize != bTree.Count )
                {
                    throw new Exception( "After inserting/deleting bTreeSize != bTree.Count, i = " + i );
                }
            }
            bTree.Close();
        }
    }
}
