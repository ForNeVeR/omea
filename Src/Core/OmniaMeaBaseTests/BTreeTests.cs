// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using DBIndex;
using JetBrains.Omea.AsyncProcessing;
using NUnit.Framework;
using JetBrains.Omea.Containers;
using System.IO;
using JetBrains.DataStructures;

namespace OmniaMeaBaseTests
{
    public class TestKey : IFixedLengthKey
    {
        private int _key;

        public TestKey( int key )
        {
            _key = key;
        }

        public TestKey( )
        {
        }

        #region IFixedLengthKey Members

        public void Write(BinaryWriter writer)
        {
            writer.Write( _key );
        }

        public void Read(BinaryReader reader)
        {
            _key = reader.ReadInt32();
        }

        public int CompareTo(object obj)
        {
            int result = _key.CompareTo( ((TestKey)obj)._key );
            return result;
        }

        public IFixedLengthKey FactoryMethod( BinaryReader reader )
        {
            TestKey testKey = new TestKey();
            testKey.Read( reader );
            return testKey;
        }
        public IFixedLengthKey FactoryMethod( )
        {
            return new TestKey( _key );
        }

        public IComparable Key
        {
            get { return _key; }
            set { _key = (int)value; }
        }

        public int KeySize { get{ return 4; } }

        public void SetIntKey( int key )
        {
            _key = key;
        }

        #endregion
    }

    public class BTreeTestsBase
    {
        static Random _random = new Random( System.Environment.TickCount );
        static int _i = 0;

        protected static int GetUniqueRand( IntHashSet numbers )
        {
            /*int rand = _random.Next( );
            while ( numbers.Contains( rand ) )
            {
                rand = _random.Next( );
            }
            numbers.Add( rand );
            return rand;**/
            numbers.Add( ++_i );
            return _i;
        }

        protected static string _indexFileName = "btree_test.btree_test";

        private void RemoveFiles()
        {
            try
            {
                string[] files = System.IO.Directory.GetFiles( ".", "*.btree_test*" );
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
            Console.WriteLine(
                "DBIndex objects: " + OmniaMeaBTree.GetObjectsCount() +
                ", memory used: " + OmniaMeaBTree.GetUsedMemory() );
            RemoveFiles();
        }
    }

    [TestFixture]
    public class BTreeTests: BTreeTestsBase
    {
        [Test]
        public void Clearing()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                bTree.InsertKey( new TestKey( 1 ), 1 );
                for( int i = 0; i < 10000; i++ )
                {
                    bTree.InsertKey( new TestKey( 2 ), i );
                }
                bTree.InsertKey( new TestKey( 3 ), 3 );
                IntArrayList offsets = new IntArrayList();
                bTree.SearchForRange( new TestKey( 2 ), new TestKey( 2 ), offsets );
                Assert.AreEqual( 10000, offsets.Count );
                bTree.Clear();
                offsets.Clear();
                bTree.SearchForRange( new TestKey( 2 ), new TestKey( 2 ), offsets );
                Assert.AreEqual( 0, offsets.Count );
                Assert.AreEqual( 0, bTree.Count );
                for( int i = 0; i < 10000; i++ )
                {
                    bTree.InsertKey( new TestKey( 2 ), i );
                }
                Assert.AreEqual( 10000, bTree.Count );
                bTree.Close();
            }
        }

        [Test]
        public void BatchInserting()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                for( int i = 0; i < 50000; i++ )
                {
                    bTree.InsertKey( new TestKey( i ), i + 1 );
                }
                Assert.AreEqual( 50000, bTree.Count );
                for( int i = 100000; i > 50000; i-- )
                {
                    bTree.InsertKey( new TestKey( i ), i + 2 );
                }
                Assert.AreEqual( 100000, bTree.Count );

                bTree.Close();
                bTree.Open();
                for( int i = 100000; i < 150000; i++ )
                {
                    bTree.InsertKey( new TestKey( i ), i + 3 );
                }
                Assert.AreEqual( 150000, bTree.Count );
                bTree.Close();
            }
        }

        [Test]
        public void BatchSearchingBackward()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                IntArrayList offsets = new IntArrayList();
                for( int i = 10000; i > 0; i-- )
                {
                    bTree.InsertKey( new TestKey( i ), i );
                    offsets.Clear();
                    bTree.SearchForRange( new TestKey( i ), new TestKey( i ), offsets );
                    Assert.AreEqual( true, FindOffset( offsets, i ), i.ToString() + " not found" );
                }
                Assert.AreEqual( 10000, bTree.Count );
                for( int i = 10000; i > 0; i-- )
                {
                    bTree.DeleteKey( new TestKey( i ), i );
                    offsets.Clear();
                    bTree.SearchForRange( new TestKey( i ), new TestKey( i ), offsets );
                    Assert.AreEqual( 0, offsets.Count, i.ToString() + " must not be found" );
                    Assert.AreEqual( false, FindOffset( offsets, i ) );
                }
                Assert.AreEqual( 0, bTree.Count );

                bTree.Close();
                bTree.Open();
                Assert.AreEqual( 0, bTree.Count );
                bTree.Close();
            }
        }

        [Test]
        public void BatchInsertingAndReopen()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                for( int i = 0; i < 10000; i++ )
                {
                    bTree.InsertKey( new TestKey( i ), i );
                }
                Assert.AreEqual( 10000, bTree.Count );
                bTree.Close();
                bTree.Open();
                Assert.AreEqual( 10000, bTree.Count );
                for( int i = 10000; i < 20000; i++ )
                {
                    bTree.InsertKey( new TestKey( i ), i );
                }
                Assert.AreEqual( 20000, bTree.Count );
                bTree.Close();
                bTree.Open();
                Assert.AreEqual( 20000, bTree.Count );
                bTree.Close();
            }
        }

        [Test]
        public void BatchInsertingAndDeleting()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                for( int i = 0; i < 10000; i++ )
                {
                    bTree.InsertKey( new TestKey( i ), i );
                }
                Assert.AreEqual( 10000, bTree.Count );
                for( int i = 0; i < 10000; i++ )
                {
                    bTree.DeleteKey( new TestKey( i ), i );
                }
                Assert.AreEqual( 0, bTree.Count );

                bTree.Close();
                bTree.Open();
                Assert.AreEqual( 0, bTree.Count );
                for( int i = 0; i < 10; i++ )
                {
                    bTree.InsertKey( new TestKey( i ), i );
                }
                Assert.AreEqual( 10, bTree.Count );
                IntArrayList offsets = new IntArrayList();
                for( int i = 0; i < 10; i++ )
                {
                    offsets.Clear();
                    bTree.SearchForRange( new TestKey( i ), new TestKey( i ), offsets );
                    Assert.AreEqual( 1, offsets.Count );
                    Assert.AreEqual( i, offsets[0] );
                }
                for( int i = 0; i < 10; i++ )
                {
                    bTree.DeleteKey( new TestKey( i ), i );
                }
                Assert.AreEqual( 0, bTree.Count );
                for( int i = 0; i < 10; i++ )
                {
                    offsets.Clear();
                    bTree.SearchForRange( new TestKey( i ), new TestKey( i ), offsets );
                    Assert.AreEqual( 0, offsets.Count );
                }
                bTree.Close();
            }
        }

        [Test]
        public void RangeSearching()
        {
            IntArrayList offsets = new IntArrayList();

            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                bTree.InsertKey( new TestKey( 1 ), 1 );
                for( int i = 0; i < 10000; i++ )
                {
                    bTree.InsertKey( new TestKey( 2 ), i );
                }
                bTree.InsertKey( new TestKey( 3 ), 3 );
                bTree.SearchForRange( new TestKey( 2 ), new TestKey( 2 ), offsets );
                Assert.AreEqual( 10000, offsets.Count );
                bTree.Close();
            }
        }

        [Test]
        public void RangeSearching1()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                bTree.InsertKey( new TestKey( 1 ), 1 );
                for( int i = 0; i < 1000; i++ )
                {
                    bTree.InsertKey( new TestKey( 2 ), i );
                }
                bTree.InsertKey( new TestKey( 3 ), 3 );
                IntArrayList offsets = new IntArrayList();
                bTree.SearchForRange( new TestKey( 2 ), new TestKey( 2 ), offsets );
                Assert.AreEqual( 1000, offsets.Count );
                bTree.Close();
            }
        }

        [Test]
        public void RangeSearching2()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                bTree.InsertKey( new TestKey( 1 ), 1 );
                for( int i = 0; i < 5000; i++ )
                {
                    bTree.InsertKey( new TestKey( 2 ), i );
                }
                bTree.InsertKey( new TestKey( 3 ), 3 );
                IntArrayList offsets = new IntArrayList();
                bTree.SearchForRange( new TestKey( 2 ), new TestKey( 2 ), offsets );
                Assert.AreEqual( 5000, offsets.Count );
                bTree.Close();
            }
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
        public void RangeSearchingEmpty()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                bTree.InsertKey( new TestKey( 1 ), 1 );
                for( int i = 0; i < 10000; i++ )
                {
                    bTree.InsertKey( new TestKey( 2 ), i );
                }
                bTree.InsertKey( new TestKey( 3 ), 3 );
                for( int i = 0; i < 10000; i++ )
                {
                    bTree.DeleteKey( new TestKey( 2 ), i );
                }
                Assert.AreEqual( 2, bTree.Count );
                IntArrayList offsets = new IntArrayList();
                bTree.SearchForRange( new TestKey( 2 ), keyFactory, offsets );
                Assert.AreEqual( 0, offsets.Count );
                bTree.Close();
            }
        }

        [Test]
        public void SequentialSearchDeleteInsert()
        {
            IntHashSet numbers = new IntHashSet();
            const int queueSize = 100000;
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open( );

                //Random random = new Random( System.Environment.TickCount );
                Queue queue = new Queue( queueSize );
                for ( int i = 0; i < queueSize; i++ )
                {
                    int key = GetUniqueRand( numbers );
                    TestKey testKey = new TestKey( key );
                    queue.Enqueue( testKey );
                    bTree.InsertKey( testKey, key );
                }
                bTree.Close();
                if( !bTree.Open() )
                {
                    throw new Exception( "Can't reopen btree! ");
                }
                int time = System.Environment.TickCount;
                IntArrayList offsets = new IntArrayList();
                for ( int i = 0; i < 20000; i++ )
                {
                    TestKey testKey = (TestKey)queue.Dequeue();
                    offsets.Clear();
                    bTree.SearchForRange( testKey, testKey, offsets );
                    Assert.AreEqual( 1, offsets.Count, testKey.Key.ToString() + " not found. i = " + i.ToString() );
                    Assert.AreEqual( (int)testKey.Key, offsets[0] );
                    bTree.DeleteKey( testKey, (int)testKey.Key );
                    numbers.Remove( (int)testKey.Key );
                    offsets.Clear();
                    bTree.SearchForRange( testKey, testKey, offsets );
                    Assert.AreEqual( 0, offsets.Count );
                    TestKey newKey = new TestKey( GetUniqueRand( numbers ) );
                    queue.Enqueue( newKey );
                    bTree.InsertKey( newKey, (int)newKey.Key );
                    offsets.Clear();
                    bTree.SearchForRange( newKey, newKey, offsets );
                    Assert.AreEqual( 1, offsets.Count );
                    Assert.AreEqual( (int)newKey.Key, offsets[0] );
                }
                time = System.Environment.TickCount - time;
                Console.WriteLine( " work took " + time.ToString() );

                bTree.Close();
            }
        }

        [Test]
        public void Deleting()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                bTree.InsertKey( new TestKey( 1 ), 1 );
                bTree.InsertKey( new TestKey( 2 ), 2 );
                Assert.AreEqual( 2, bTree.Count );
                bTree.DeleteKey( new TestKey( 2 ), 2 );
                Assert.AreEqual( 1, bTree.Count );
                IntArrayList offsets = new IntArrayList();
                bTree.SearchForRange( new TestKey( 1 ), new TestKey( 1 ), offsets );
                Assert.AreEqual( 1, offsets.Count );
                Assert.AreEqual( 1, offsets[0] );

                bTree.Close();
            }
        }

        [Test]
        public void GetAllOffsets()
        {
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                bTree.InsertKey( new TestKey( 1 ), 1 );
                for( int i = 0; i < 10000; i++ )
                {
                    bTree.InsertKey( new TestKey( 2 ), i );
                }
                bTree.InsertKey( new TestKey( 3 ), 3 );
                IntArrayList offsets = new IntArrayList();
                bTree.GetAllKeys( offsets );
                Assert.AreEqual( 10002, offsets.Count );
                bTree.Close();
                bTree.Open();
                offsets.Clear();
                bTree.GetAllKeys( offsets );
                Assert.AreEqual( 10002, offsets.Count );
                for( int i = 5; i < 15; ++i )
                {
                    bTree.InsertKey( new TestKey( i - 5 ), i );
                }
                bTree.Close();
                bTree.Open();
                offsets.Clear();
                bTree.GetAllKeys( offsets );
                Assert.AreEqual( 10012, offsets.Count );
                bTree.Close();
                bTree.Open();
                for( int i = 5; i < 15; ++i )
                {
                    bTree.DeleteKey( new TestKey( i - 5 ), i );
                }
                offsets.Clear();
                bTree.GetAllKeys( offsets );
                Assert.AreEqual( 10002, offsets.Count );
                bTree.Close();
                bTree.Open();
                offsets.Clear();
                bTree.GetAllKeys( offsets );
                Assert.AreEqual( 10002, offsets.Count );
                bTree.Close();
            }
        }

        public class TestCompoundKey : IFixedLengthKey
        {
            private long _key1;
            private long _key2;

            public TestCompoundKey( long key1, long key2 )
            {
                _key1 = key1;
                _key2 = key2;
            }

            public TestCompoundKey( )
            {
            }

            public long Key1
            {
                get { return _key1; }
            }
            public long Key2
            {
                get { return _key2; }
            }
            #region IFixedLengthKey Members

            public void Write(BinaryWriter writer)
            {
                writer.Write( _key1 );
                writer.Write( _key2 );
            }

            public void Read(BinaryReader reader)
            {
                _key1 = reader.ReadInt64();
                _key2 = reader.ReadInt64();
            }

            public int CompareTo(object obj)
            {
                int result = _key1.CompareTo( ((TestCompoundKey)obj)._key1 );
                if ( result != 0 )
                {
                    return result;
                }
                result = _key2.CompareTo( ((TestCompoundKey)obj)._key2 );
                return result;
            }

            public IFixedLengthKey FactoryMethod( BinaryReader reader )
            {
                TestCompoundKey testKey = new TestCompoundKey();
                testKey.Read( reader );
                return testKey;
            }
            public IFixedLengthKey FactoryMethod( )
            {
                TestCompoundKey testKey = new TestCompoundKey();
                testKey._key1 = _key1;
                testKey._key2 = _key2;
                return testKey;
            }

            public IComparable Key
            {
                get { return this; }
                set {}
            }

            public int KeySize { get{ return 16; } }

            public void SetIntKey( int key )
            {
            }

            #endregion

            #region IComparer Members

            public int Compare(object x, object y)
            {
                TestCompoundKey xKey = (TestCompoundKey)x;
                TestCompoundKey yKey = (TestCompoundKey)y;
                return (int)(xKey._key1 - yKey._key1);
            }

            #endregion
        }

        [Test]
        public void CompoundKeyTest()
        {
            TestCompoundKey keyFactory = new TestCompoundKey();
            BTree bTree = new BTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                for( int i = 0; i < 10000; i++ )
                {
                    bTree.InsertKey( new TestCompoundKey(  i , i ), i );
                }
                IntArrayList offsets = new IntArrayList();
                bTree.SearchForRange( new TestCompoundKey(  0, 0 ), new TestCompoundKey(  10000 , 10000 ), offsets );
                Assert.AreEqual( 10000, offsets.Count );
                bTree.Close();
            }
        }

        [Test]
        public void InsertAndDeletePageBenchmark()
        {
            IntHashSet numbers = new IntHashSet();
            const int queueSize = 4000;
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open( );

                Queue queue = new Queue( queueSize );
                for ( int i = 0; i < queueSize; i++ )
                {
                    TestKey testKey = new TestKey( GetUniqueRand( numbers ) );
                    queue.Enqueue( testKey );
                    bTree.InsertKey( testKey, (int)testKey.Key );
                }
                int time = System.Environment.TickCount;
                for ( int i = 0; i < 100000; i++ )
                {
                    TestKey testKey = (TestKey)queue.Dequeue();
                    bTree.DeleteKey( testKey, (int)testKey.Key );
                    numbers.Remove( (int)testKey.Key );
                    TestKey newKey = new TestKey( GetUniqueRand( numbers ) );
                    queue.Enqueue( newKey );
                    bTree.InsertKey( newKey, (int)newKey.Key );
                }
                time = System.Environment.TickCount - time;
                Console.WriteLine( " work took " + time.ToString() );

                bTree.Close();
            }
        }

        [Test]
        public void SearchForRangeOrderTest()
        {
            IntHashSet numbers = new IntHashSet();
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                TestKey testKey = new TestKey( 0 );

                for ( int i = 0; i < 10000; i++ )
                {
                    testKey = new TestKey( GetUniqueRand( numbers ) );
                    bTree.InsertKey( testKey, (int)testKey.Key );
                }

                ArrayList keys_offsets = new ArrayList();
                bTree.SearchForRange( new TestKey( 0 ), testKey, keys_offsets );
                for( int j = 1; j < keys_offsets.Count; ++j )
                {
                    KeyPair pair1 = (KeyPair) keys_offsets[ j - 1 ];
                    KeyPair pair2 = (KeyPair) keys_offsets[ j ];
                    if( pair1._key.CompareTo( pair2._key ) > 0 )
                    {
                        throw new Exception( "Invalid key order, j = " + j );
                    }
                }

                bTree.Close();
            }
        }

        [Test]
        public void SequentialInsertAndDelete()
        {
            IntHashSet numbers = new IntHashSet();
            const int queueSize = 10000;
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                int time = System.Environment.TickCount;
                Queue queue = new Queue( queueSize );
                const int cycles = 2;

                for( int count = 0; count < cycles; ++count )
                {
                    for ( int i = 0; i < queueSize; i++ )
                    {
                        TestKey testKey = new TestKey( GetUniqueRand( numbers ) );
                        queue.Enqueue( testKey );
                        bTree.InsertKey( testKey, (int)testKey.Key );
                    }
                    for ( int i = 0; i < queueSize - 1; i++ )
                    {
                        TestKey testKey = (TestKey)queue.Dequeue();
                        bTree.DeleteKey( testKey, (int)testKey.Key );
                        numbers.Remove( (int)testKey.Key );
                    }
                    queue.Clear();
                }

                Assert.AreEqual( cycles, bTree.Count );

                time = System.Environment.TickCount - time;
                Console.WriteLine( " work took " + time.ToString() );

                bTree.Close();
            }
        }

        [Test]
        public void SequentialInsertDeleteGetAllKeys()
        {
            IntHashSet numbers = new IntHashSet();
            const int queueSize = 3000;
            const int cacheSize = 32;
            TestKey keyFactory = new TestKey();
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            bTree.SetCacheSize( cacheSize );
            using( bTree )
            {
                bTree.Open();
                int time = System.Environment.TickCount;
                Queue queue = new Queue( queueSize );
                const int cycles = 20;

                for ( int i = 0; i < queueSize * cacheSize; i++ )
                {
                    TestKey testKey = new TestKey( GetUniqueRand( numbers ) );
                    bTree.InsertKey( testKey, (int)testKey.Key );
                }

                for( int count = 0; count < cycles; ++count )
                {
                    for ( int i = 0; i < queueSize; i++ )
                    {
                        TestKey testKey = new TestKey( GetUniqueRand( numbers ) );
                        queue.Enqueue( testKey );
                        bTree.InsertKey( testKey, (int)testKey.Key );
                    }
                    int count_ = bTree.Count;
                    bTree.Close();
                    bTree.Open();
                    ArrayList keys = new ArrayList( count_ );
                    IntArrayList ints = new IntArrayList( count_ );
                    bTree.GetAllKeys( keys );
                    if( keys.Count != count_ || bTree.Count != count_ )
                    {
                        throw new Exception(
                            "keys.Count = " +  keys.Count + ", bTree.Count = " + bTree.Count + ", count_ = " + count_ );
                    }
                    foreach( KeyPair pair in keys )
                    {
                        ints.Add( (int) pair._key.Key );
                    }
                    for ( int i = 0; i < queueSize - 1; i++ )
                    {
                        TestKey testKey = (TestKey)queue.Dequeue();
                        int key = (int)testKey.Key;
                        bTree.DeleteKey( testKey, key );
                        numbers.Remove( key );
                        if( ints.BinarySearch( key ) < 0 )
                        {
                            throw new Exception( "The ints array doesn't contain removed key" );
                        }
                    }
                    queue.Clear();
                }

                Assert.AreEqual( cycles + queueSize * cacheSize, bTree.Count );

                time = System.Environment.TickCount - time;
                Console.WriteLine( " work took " + time.ToString() );

                bTree.Close();
            }
        }

        [Test]
        public void EmulateSequentialSplit_ThenDeleteAndGetAllKeys()
        {
            TestKey keyFactory = new TestKey();
            TestKey testKey;
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                for( int i = 0; i < 10; ++i )
                {
                    testKey = new TestKey( 0 );
                    bTree.InsertKey( testKey, i );
                }
                for( int i = 0; i < 10; ++i )
                {
                    testKey = new TestKey( Int32.MaxValue );
                    bTree.InsertKey( testKey, i );
                }
                for( int i = 1; i < 4991; ++i )
                {
                    testKey = new TestKey( i );
                    bTree.InsertKey( testKey, 0 );
                    testKey = new TestKey( Int32.MaxValue - i );
                    bTree.InsertKey( testKey, 0 );
                }
                int count_ = bTree.Count;
                Assert.AreEqual( 10000, count_ );
                bTree.Close();
                bTree.Open();
                ArrayList keys = new ArrayList( count_ );
                bTree.GetAllKeys( keys );
                if( keys.Count != count_ || bTree.Count != count_ )
                {
                    throw new Exception(
                        "keys.Count = " +  keys.Count + ", bTree.Count = " + bTree.Count + ", count_ = " + count_ );
                }
                testKey = new TestKey( 0 );
                bTree.DeleteKey( testKey, 0 );
                bTree.DeleteKey( testKey, 1 );
                bTree.DeleteKey( testKey, 2 );
                testKey = new TestKey( Int32.MaxValue );
                bTree.DeleteKey( testKey, 9 );
                bTree.DeleteKey( testKey, 8 );
                bTree.DeleteKey( testKey, 7 );
                for( int i = 10000; i < 55003; ++i )
                {
                    testKey = new TestKey( i );
                    bTree.InsertKey( testKey, 0 );
                    testKey = new TestKey( Int32.MaxValue - i );
                    bTree.InsertKey( testKey, 0 );
                }
                count_ = bTree.Count;
                Assert.AreEqual( 100000, count_ );
                bTree.Close();
                bTree.Open();
                keys = new ArrayList( count_ );
                bTree.GetAllKeys( keys );
                if( keys.Count != count_ || bTree.Count != count_ )
                {
                    throw new Exception(
                        "keys.Count = " +  keys.Count + ", bTree.Count = " + bTree.Count + ", count_ = " + count_ );
                }

                bTree.Close();
            }
        }

        [Test]
        public void MultipleEqualKeys()
        {
            const int cycles = 10000;
            TestKey keyFactory = new TestKey( 0 );
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                for( int i = 0; i < cycles; ++i )
                {
                    bTree.InsertKey( keyFactory, i );
                }
                IntArrayList offsets = new IntArrayList();
                bTree.SearchForRange( keyFactory, keyFactory, offsets );
                Assert.AreEqual( cycles, bTree.Count );
                Assert.AreEqual( cycles, offsets.Count );
                bTree.Close();
            }
        }

        [Test]
        public void GroupsMultipleEqualKeys()
        {
            const int cycles = 20000;
            TestKey keyFactory = new TestKey( 0 );
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                for( int j = 0; j < 10; ++j )
                {
                    for( int i = 0; i < cycles; ++i )
                    {
                        bTree.InsertKey( new TestKey( j ), i );
                    }
                }
                for( int j = 0; j < 10; ++j )
                {
                    IntArrayList offsets = new IntArrayList();
                    bTree.SearchForRange( new TestKey( j ), new TestKey( j ), offsets );
                    Assert.AreEqual( cycles, offsets.Count );
                    for( int i = 0; i < cycles; ++i )
                    {
                        bTree.DeleteKey( new TestKey( j ), i );
                    }
                }
                bTree.Close();
            }
        }

        [Test]
        public void SmallCacheBTreeReopen()
        {
            TestKey keyFactory = new TestKey( 0 );
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.SetCacheSize(  2 );
                bTree.Open();
                for( int i = 0; i < 50000; ++i )
                {
                    bTree.InsertKey( new TestKey( i ), i );
                }
                bTree.Close();
                bTree.Open();
                for( int i = 0; i < 15000; ++i )
                {
                    IntArrayList offsets = new IntArrayList();
                    bTree.SearchForRange( new TestKey( i ), new TestKey( i ), offsets );
                    Assert.AreEqual( 1, offsets.Count );
                    bTree.DeleteKey( new TestKey( i ), i );
                }
                bTree.Close();
                bTree.Open();
                for( int i = 15000; i < 30000; ++i )
                {
                    IntArrayList offsets = new IntArrayList();
                    bTree.SearchForRange( new TestKey( i ), new TestKey( i ), offsets );
                    Assert.AreEqual( 1, offsets.Count );
                    bTree.DeleteKey( new TestKey( i ), i );
                }
                bTree.Close();
                bTree.Open();
                for( int i = 30000; i < 50000; ++i )
                {
                    IntArrayList offsets = new IntArrayList();
                    bTree.SearchForRange( new TestKey( i ), new TestKey( i ), offsets );
                    Assert.AreEqual( 1, offsets.Count );
                }
                bTree.Close();
            }
        }

        [Test]
        public void MultipleBTrees()
        {
            ArrayList keys = new ArrayList();
            TestKey keyFactory = new TestKey();
            TestKey sameKey = new TestKey( -1 );
            OmniaMeaBTree bTree1 = new OmniaMeaBTree( _indexFileName, keyFactory );
            OmniaMeaBTree bTree2 = new OmniaMeaBTree( _indexFileName + "2", keyFactory );
            OmniaMeaBTree bTree3 = new OmniaMeaBTree( _indexFileName + "3", keyFactory );
            bTree1.Open();
            bTree2.Open();
            bTree3.Open();
            for( int i = 0; i < 50000; i++ )
            {
                bTree1.InsertKey( new TestKey( i ), i + 1 );
                bTree2.InsertKey( new TestKey( i ), i + 1 );
                bTree3.InsertKey( new TestKey( i ), i + 1 );
            }

            bTree1.Close();
            bTree2.Close();
            bTree1.Open();
            bTree3.Close();
            bTree2.Open();
            bTree3.Open();

            Assert.AreEqual( 50000, bTree1.Count );
            Assert.AreEqual( 50000, bTree2.Count );
            Assert.AreEqual( 50000, bTree3.Count );

            keys.Clear();
            bTree1.GetAllKeys( keys );
            Assert.AreEqual( 50000, keys.Count );
            keys.Clear();
            bTree2.GetAllKeys( keys );
            Assert.AreEqual( 50000, keys.Count );
            keys.Clear();
            bTree3.GetAllKeys( keys );
            Assert.AreEqual( 50000, keys.Count );

            for( int i = 0; i < 50000; i++ )
            {
                bTree1.InsertKey( new TestKey( i ), i - 1 );
                bTree2.InsertKey( new TestKey( i ), i - 1 );
                bTree3.InsertKey( new TestKey( i ), i - 1 );
            }

            bTree1.Close();
            bTree2.Close();
            bTree3.Close();
            bTree3.Open();
            bTree2.Open();
            bTree1.Open();

            Assert.AreEqual( 100000, bTree1.Count );
            Assert.AreEqual( 100000, bTree2.Count );
            Assert.AreEqual( 100000, bTree3.Count );

            keys.Clear();
            bTree1.GetAllKeys( keys );
            Assert.AreEqual( 100000, keys.Count );
            keys.Clear();
            bTree2.GetAllKeys( keys );
            Assert.AreEqual( 100000, keys.Count );
            keys.Clear();
            bTree3.GetAllKeys( keys );
            Assert.AreEqual( 100000, keys.Count );

            for( int i = 50000; i > 0; i-- )
            {
                bTree1.InsertKey( new TestKey( i ), i + 2 );
                bTree2.InsertKey( new TestKey( i ), i + 2 );
                bTree3.InsertKey( new TestKey( i ), i + 2 );
            }

            bTree1.Close();
            bTree2.Close();
            bTree3.Close();
            bTree3.Open();
            bTree2.Open();
            bTree1.Open();

            Assert.AreEqual( 150000, bTree1.Count );
            Assert.AreEqual( 150000, bTree2.Count );
            Assert.AreEqual( 150000, bTree3.Count );

            keys.Clear();
            bTree1.GetAllKeys( keys );
            Assert.AreEqual( 150000, keys.Count );
            keys.Clear();
            bTree2.GetAllKeys( keys );
            Assert.AreEqual( 150000, keys.Count );
            keys.Clear();
            bTree3.GetAllKeys( keys );
            Assert.AreEqual( 150000, keys.Count );

            for( int i = 0; i < 1100; i++ )
            {
                bTree1.InsertKey( sameKey, i );
                bTree2.InsertKey( sameKey, i );
                bTree3.InsertKey( sameKey, i );
            }

            keys.Clear();
            bTree1.GetAllKeys( keys );
            Assert.AreEqual( 151100, keys.Count );
            keys.Clear();
            bTree2.GetAllKeys( keys );
            Assert.AreEqual( 151100, keys.Count );
            keys.Clear();
            bTree3.GetAllKeys( keys );
            Assert.AreEqual( 151100, keys.Count );

            keys.Clear();
            bTree1.SearchForRange( sameKey, sameKey, keys );
            Assert.AreEqual( 1100, keys.Count );
            keys.Clear();
            bTree2.SearchForRange( sameKey, sameKey, keys );
            Assert.AreEqual( 1100, keys.Count );
            keys.Clear();
            bTree3.SearchForRange( sameKey, sameKey, keys );
            Assert.AreEqual( 1100, keys.Count );

            bTree1.Close();
            bTree3.Close();
            bTree3.Dispose();
            bTree1.Dispose();
            bTree2.Close();
            bTree1.Dispose();
        }

        [Test]
        public void GetAllKeysEnumerator()
        {
            TestKey keyFactory = new TestKey( 0 );
            OmniaMeaBTree bTree = new OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                for( int i = 0; i < 100000; ++i )
                {
                    bTree.InsertKey( new TestKey( i ), i );
                }
                int count = 0;
                foreach( KeyPair pair in bTree.GetAllKeys() )
                {
                    Assert.AreEqual( count, (int) pair._key.Key );
                    Assert.AreEqual( count, pair._offset );
                    ++count;
                }
                Assert.AreEqual( count, 100000 );

                KeyPair pair1 = bTree.GetMaximum();
                Assert.AreEqual( (int)pair1._key.Key, 99999 );
                pair1 = bTree.GetMinimum();
                Assert.AreEqual( (int)pair1._key.Key, 0 );

                // delete each 15 of 16 keys
                for( int i = 0; i < 100000; ++i )
                {
                    if( ( i & 15 ) != 0 )
                    {
                        bTree.DeleteKey( new TestKey( i ), i );
                    }
                }
                count = 0;
                foreach( KeyPair pair in bTree.GetAllKeys() )
                {
                    Assert.AreEqual( 16 * count, (int) pair._key.Key );
                    Assert.AreEqual( 16 * count, pair._offset );
                    ++count;
                }
                Assert.AreEqual( count, 100000 / 16 );
                pair1 = bTree.GetMaximum();
                Assert.AreEqual( (int)pair1._key.Key, 99984 );
                pair1 = bTree.GetMinimum();
                Assert.AreEqual( (int)pair1._key.Key, 0 );

                bTree.Close();
            }
        }

        [Test]
        public void SearchForRangeEnumerator()
        {
            TestKey keyFactory = new TestKey( 0 );
            IBTree bTree = new OmniaMeaBTree( _indexFileName, keyFactory );
            using( bTree )
            {
                bTree.Open();
                for( int i = 0; i < 100000; ++i )
                {
                    bTree.InsertKey( new TestKey( i ), i );
                }
                int count = 0;
                foreach( KeyPair pair in bTree.SearchForRange( new TestKey( 0 ), new TestKey( 9999 ) ) )
                {
                    Assert.AreEqual( count, (int) pair._key.Key );
                    Assert.AreEqual( count, pair._offset );
                    ++count;
                }
                Assert.AreEqual( count, 10000 );

                count = 0;
                foreach( KeyPair pair in bTree.SearchForRange( new TestKey( 100001 ), new TestKey( 100001 ) ) )
                {
                    Assert.AreEqual( count, (int) pair._key.Key );
                    Assert.AreEqual( count, pair._offset );
                    ++count;
                }
                Assert.AreEqual( count, 0 );

                count = 0;
                foreach( KeyPair pair in bTree.SearchForRange( new TestKey( 49999 ), new TestKey( 49999 ) ) )
                {
                    Assert.AreEqual( (int) pair._key.Key, 49999 );
                    Assert.AreEqual( pair._offset, 49999 );
                    ++count;
                }
                Assert.AreEqual( count, 1 );

                // delete each 15 of 16 keys
                for( int i = 0; i < 100000; ++i )
                {
                    if( ( i & 15 ) != 0 )
                    {
                        bTree.DeleteKey( new TestKey( i ), i );
                    }
                }
                count = 0;
                foreach( KeyPair pair in bTree.SearchForRange( new TestKey( 0 ), new TestKey( 100000 ) ) )
                {
                    Assert.AreEqual( 16 * count, (int) pair._key.Key );
                    Assert.AreEqual( 16 * count, pair._offset );
                    ++count;
                }
                Assert.AreEqual( count, 100000 / 16 );

                bTree.Close();
            }
        }
    }

    [TestFixture]
    public class BTreeStressTests: BTreeTestsBase
    {
        private delegate void DoStressProcessingDelegate( IBTree bTree );

        private void DoStressProcessing( IBTree bTree )
        {
            const int initialSize = 500000;
            const int iterations = 1000000;

            IntHashSet uniqueKeys = new IntHashSet();
            IntArrayList array = new IntArrayList();
            using( bTree )
            {
                bTree.Open();
                for( int i = 0; i < initialSize; ++i )
                {
                    bTree.InsertKey( new TestKey( GetUniqueRand( uniqueKeys ) ), 0 );
                }

                for( int i = 0; i < iterations; ++i )
                {
                    int key = 0;
                    foreach( IntHashSet.Entry e in uniqueKeys )
                    {
                        key = e.Key;
                        break;
                    }
                    array.Clear();
                    bTree.SearchForRange( new TestKey( key ), new TestKey( key ), array );
                    Assert.AreEqual( 1, array.Count );
                    bTree.DeleteKey( new TestKey( key ), 0 );
                    uniqueKeys.Remove( key );
                    if( ( i & 31 ) == 5 )
                    {
                        array.Clear();
                        bTree.GetAllKeys( array );
                        Assert.AreEqual( initialSize + i - 1, array.Count );
                        Assert.AreEqual( uniqueKeys.Count, array.Count );
                    }
                    bTree.InsertKey( new TestKey( GetUniqueRand( uniqueKeys ) ), 0 );
                    bTree.InsertKey( new TestKey( GetUniqueRand( uniqueKeys ) ), 0 );
                    if( ( i & 31 ) == 17 )
                    {
                        array.Clear();
                        bTree.GetAllKeys( array );
                        Assert.AreEqual( initialSize + i + 1, array.Count );
                        Assert.AreEqual( uniqueKeys.Count, array.Count );
                    }
                    Trace.WriteLine( "Passes: " + i );
                }
                bTree.Close();
            }
        }

        [Test, Ignore( "This is stress test" )]
        public void SingleThreadedStress()
        {
            IBTree bTree = new /*BTree*/OmniaMeaBTree( _indexFileName, new TestKey( 0 ) );
            DoStressProcessing( bTree );
        }

        [Test, Ignore( "This is stress test" )]
        public void MultiThreadedStress()
        {
            AsyncProcessor processor1 = new AsyncProcessor();
            AsyncProcessor processor2 = new AsyncProcessor();
            IBTree bTree1 = new /*BTree*/OmniaMeaBTree( _indexFileName, new TestKey( 0 ) );
            IBTree bTree2 = new /*BTree*/OmniaMeaBTree( _indexFileName + "2", new TestKey( 0 ) );
            processor1.QueueJob( new DoStressProcessingDelegate( DoStressProcessing ), bTree1 );
            processor2.QueueJob( new DoStressProcessingDelegate( DoStressProcessing ), bTree2 );
            using( processor1 )
            {
                using( processor2 )
                {
                    Thread.Sleep( 1000 );
                }
            }
        }
    }
}
