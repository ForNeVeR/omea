// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using System.Text;
using JetBrains.Omea.Containers;
using NUnit.Framework;
using JetBrains.DataStructures;

namespace OmniaMeaBaseTests
{
	[TestFixture]
	public class ContainersTest
	{
		[Test, Ignore("IntArrayList specific")]
		public void TestIntArrayListReducedCapacity()
        {
            IntArrayList    array = new IntArrayList( 2 );
            for( int i = 0; i < 10; i++ )
                array.Add( i );

            //  now artificially reduce amount of memory.
            array.Capacity = 3;
            if( array.Capacity != array.Count )
                throw new ApplicationException( "After reducing capacity Count != Capacity" + array.Capacity + " " + array.Count );

            if( array[ 0 ] != 0 )
                throw new ApplicationException( "Elements in the new storage are not saved" );
            if( array[ 1 ] != 1 )
                throw new ApplicationException( "Elements in the new storage are not saved" );
            if( array.Last != 2 )
                throw new ApplicationException( "Elements in the new storage are not saved" );
        }

		[Test, Ignore("IntArrayList specific")]
		public void TestIntArrayListInsertElementPastCapacity()
        {
            IntArrayList    array = new IntArrayList();
            for( int i = 0; i < array.Capacity; i++ )
                array.Add( i );

            try
            {
                array.Insert( array.Capacity + 3, 0 );
            }
            catch( ArgumentOutOfRangeException exc_ )
            {
                Console.WriteLine( "Caught necessary exception: " + exc_.Message );
            }
        }

        [Test] public void TestRemoveDuplicates()
        {
            IntArrayList array1 = new IntArrayList();
            array1.RemoveDuplicatesSorted();
            Assert.AreEqual( 0, array1.Count );

            IntArrayList array2 = new IntArrayList();
            array2.Add( 1 );
            array2.RemoveDuplicatesSorted();
            Assert.AreEqual( 1, array2.Count );

            IntArrayList array3 = new IntArrayList();
            array3.Add( 2 );
            array3.Add( 2 );
            array3.RemoveDuplicatesSorted();
            Assert.AreEqual( 1, array3.Count );
        }

        [Test]
        public void TestIntArrayListMinusSorted()
        {
            IntArrayList array = new IntArrayList();
            array.AddRange( new int[] { 0, 1, 2, 3, 4, 5, 5, 5, 6, 7, 8, 9 } );
            IntArrayList minus = new IntArrayList();
            minus.Add( 0 );
            minus.Add( 1 );
            array.MinusSorted( minus );
            Assert.AreEqual( 10, array.Count );
            minus.Add( 6 );
            minus.Add( 7 );
            minus.Add( 8 );
            minus.Add( 9 );
            array.MinusSorted( minus );
            Assert.AreEqual( 6, array.Count );
            minus.Clear();
            minus.Add( 5 );
            array.MinusSorted( minus );
            Assert.AreEqual( 3, array.Count );
            array.MinusSorted( array );
            Assert.AreEqual( 0, array.Count );
        }

		[Test]
		public void TestIntHashTable()
		{
			// update hashtable
			IntHashTable T = new IntHashTable();
			Random Rnd = new Random();
			int i = 0;
			for( ; i < 1000; ++i )
				T[ i - 500 ] = Rnd.Next(10000000).ToString();

			// test IEnumerable implementation
			int iCount = 0;
			foreach( IntHashTable.Entry E in T )
			{
				++iCount;
				if( E.Value == null )
					throw new Exception("Null value in IntHashTable");
			}
			if(iCount != 1000)
				throw new Exception("IntHashTable as IEnumerable returns invalid entries");

			// test IDictionary implementation
			if( !T.Contains( 100 ) || !T.Contains( 200 ))
				throw new Exception( "IntHashTable's key resolution error");
			if( T.Count != 1000)
				throw new Exception( "IntHashTable.Count returned invalid value: " + T.Count.ToString() );
		}

		[Test]
		public void TestIntHashTableOfInt()
		{
			// update hashtable
			IntHashTableOfInt T = new IntHashTableOfInt();
			Random Rnd = new Random();
			int i = 0;
			for( ; i < 1000; ++i )
				T[ i - 500 ] = Rnd.Next(10000000);

			// test IEnumerable implementation
			int iCount = 0;
			foreach( IntHashTableOfInt.Entry E in T )
			{
				++iCount;
				if( E.Value == Int32.MaxValue )
					throw new Exception("Null value in IntHashTableOfInt");
			}
			if(iCount != 1000)
				throw new Exception("IntHashTableOfInt as IEnumerable returns invalid entries");

			// test IDictionary implementation
			if( !T.Contains( 100 ) || !T.Contains( 200 ))
				throw new Exception( "IntHashTableOfInt's key resolution error");
			if( T.Count != 1000)
				throw new Exception( "IntHashTableOfInt.Count returned invalid value: " + T.Count.ToString() );
		}

        [Test] public void TestIntSelfCleanedHashTable()
        {
            IntWeakHashTable ht = new IntWeakHashTable();
            ArrayList objects = new ArrayList();
            for( int i=0; i<100; i++ )
            {
                object o = new Object();
                objects.Add( o );
                ht.Add( i, o );
            }
            for( int i=0; i<100; i++ )
            {
                ht.Add( i+100, new Object() );
            }

            GC.Collect();
            ht.Compact();
            Assert.AreEqual( 100, ht.Count );
        }

		[Test]
		public void TestHashMap()
		{
			// update hashtable
			HashMap T = new HashMap();
			Random Rnd = new Random();
			long i = 0;
			for( ; i < 1000; ++i )
				T[ i ] = Rnd.Next(10000000).ToString();

			// test IEnumerable implementation
			int iCount = 0;
			foreach( HashMap.Entry E in T )
			{
				++iCount;
				if( E.Value == null )
					throw new Exception("Null value in HashMap");
			}
			if( iCount != 1000 )
				throw new Exception("HashMap as IEnumerable returns invalid entries");

			// test IDictionary implementation
			if( !T.Contains( 100L ) || !T.Contains( 200L ))
				throw new Exception( "HashMap's key resolution error");
			if( T.Count != 1000 )
				throw new Exception( "HashMap.Count returned invalid value: " + T.Count.ToString() );

		}

/*		[Test]
		public void TestHashMapPerformance()
		{
			int i, j;
			int count = 1000000;
			Random Rnd = new Random();
			DateTime Start;

            System.GC.Collect();

            Start = DateTime.Now;
			Hashtable dotNETTable = new Hashtable();
			for( i = 0; i < count; ++i )
			{
                j = Rnd.Next( 100000 );
                if( dotNETTable.Contains( j ) )
                    dotNETTable[ j ] = "q";
                else
                    dotNETTable[ j ] = string.Empty;

			}
			TimeSpan HashtableTime = DateTime.Now - Start;
			Console.WriteLine( HashtableTime );

            System.GC.Collect();

            Start = DateTime.Now;
            IntHashTable T = new IntHashTable();
            for( i = 0; i < count; ++i )
            {
                j = Rnd.Next( 100000 );
                IntHashTable.Entry E = T.GetEntry( j );
                if( E != null )
                    E.Value = "q";
                else
                    T[ j ] = string.Empty;
            }
            TimeSpan HashMapTime = DateTime.Now - Start;
            Console.WriteLine( HashMapTime );

			if( HashtableTime.Ticks < HashMapTime.Ticks )
				throw new Exception( "Hashtable seems to be more efficient than OmniaMea HashMap!");
		}*/

		[Test]
		public void TestHashSet()
		{
			HashSet _set = new HashSet();

			string[] strings = new string[]
				{ "1000", "1200", "1500", "1800", "2000", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
				  "a", "b", "c", "d", "e", "f", "g", "k", "l", "m", "n", "o", "p", "r", "s", "t", "x", "y", "z"
				};
			foreach( string s in strings )
				_set.Add( s );
			if( _set.Count != strings.Length )
				throw new Exception( "Invalid size of HashSet" );
			foreach( string s in strings )
				if( !_set.Contains( s ))
					throw new Exception( "HashSet doesn't contain earlier inserted key" );
		}

        [Test]
        public void TestCache()
        {
            ObjectCache cache = new ObjectCache( 16 );

            cache.CacheObject( 0, "this is a cached object" );

            if( (( string ) cache.TryKey( 0 )) != "this is a cached object" )
            {
                throw new Exception( "Bad object extracted from cache" );
            }

            if( cache.HitRate() != 1 )
            {
                throw new Exception( "Bad cache hitrate" );
            }

            if( cache.Count != 1 )
            {
                throw new Exception( "Bad cache count" );
            }

            for( int i = 1; i < 100; ++i )
            {
                cache.CacheObject( i, i );
                if( ( i % 7 ) == 0 )
                {
                    cache.TryKey( 0 );
                }
            }

            if( (( string ) cache.TryKey( 0 )) != "this is a cached object" )
            {
                throw new Exception( "Old cached element missed" );
            }

            cache.Remove( 0 );
            if( cache.TryKey( 0 ) != null )
            {
                throw new Exception( "Removed object is present in cache" );
            }

            for( int i = 100; i < 150; ++i )
            {
                cache.CacheObject( i, i );
            }

            for( int i = 149; i > 133; --i )
            {
                if( cache.TryKey( i ) == null )
                {
                    throw new Exception( i.ToString() + "th object is not present in cache" ) ;
                }
            }

            for( int i = 133; i > 0; --i )
            {
                if( cache.TryKey( i ) != null )
                {
                    throw new Exception( i.ToString() + "th object is present in cache" ) ;
                }
            }
        }

        [Test]
        public void TestCacheEnumerator()
        {
            IntObjectCache cache = new IntObjectCache( 16 );

            for( int i = 0; i < 10; ++i )
            {
                cache.CacheObject( i, i );
            }
            int count = 0;
            foreach( int i in cache )
            {
                ++count;
            }
            Assert.AreEqual( 10, count );
            for( int i = 0; i < 10; ++i )
            {
                cache.CacheObject( i + 10, i );
            }
            count = 0;
            foreach( int i in cache )
            {
                ++count;
            }
            Assert.AreEqual( cache.Size, count );
        }

        [Test, Ignore("This is stress test")]
        public void StressCaching()
        {
            const int cacheSize = 1000;
            IntObjectCache cache = new IntObjectCache( cacheSize );
            Random rnd = new Random();
            for( int i = 0; i < 100000000; ++i )
            {
                int next = rnd.Next( cacheSize * 2 );
                if( cache.TryKey( next ) == null )
                {
                    cache.CacheObject( next, i );
                }
            }
            Console.WriteLine( "Cache hit rate = " + cache.HitRate().ToString() );
        }

        [Test]
        public void TestCharTrie()
        {
            CharTrie ct = new CharTrie( null ); // use default char comparer

            ct.Add( "this" );
            ct.Add( "these" );
            ct.Add( "those" );
            ct.Add( "the" );
            ct.Add( "they" );
            ct.Add( "their" );
            ct.Add( "there" );
            ct.Add( "thus" );

            // test strings
            if( ct.GetMatchingLength("this") != "this".Length ||
                ct.GetMatchingLength("these") != "these".Length ||
                ct.GetMatchingLength("those") != "those".Length ||
                ct.GetMatchingLength("the") != "the".Length ||
                ct.GetMatchingLength("they") != "they".Length ||
                ct.GetMatchingLength("their") != "their".Length ||
                ct.GetMatchingLength("there") != "there".Length ||
                ct.GetMatchingLength("thus") != "thus".Length )
                throw new Exception(" earlier added strings are not contained in char trie " );

            // test substrings
            if( ct.GetMatchingLength("thisaaa") != "this".Length ||
                ct.GetMatchingLength("thesebbb") != "these".Length ||
                ct.GetMatchingLength("thoseccc") != "those".Length ||
                ct.GetMatchingLength("theddd") != "the".Length ||
                ct.GetMatchingLength("theyeee") != "they".Length ||
                ct.GetMatchingLength("theirfff") != "their".Length ||
                ct.GetMatchingLength("thereggg") != "there".Length ||
                ct.GetMatchingLength("thushhh") != "thus".Length )
                throw new Exception(" earlier added strings are not contained in char trie " );

            // test enumeration
            int i = 0;
            foreach ( CharTrie.Node node in ct )
            {
                node.GetType();
                ++i;
            }

            if( i != 17 )
                throw new Exception( " char trie enumeration returns invalid number of nodes ");

            // test serialization
            MemoryStream stream  = new MemoryStream();
            ct.Save( new BinaryWriter( stream ));
            stream.Flush();

            CharTrie ct1 = new CharTrie( null );
            stream.Seek( 0, SeekOrigin.Begin );
            ct1.Load( new BinaryReader( stream ));
            if( ct1.GetMatchingLength("this") != "this".Length ||
                ct1.GetMatchingLength("these") != "these".Length ||
                ct1.GetMatchingLength("those") != "those".Length ||
                ct1.GetMatchingLength("the") != "the".Length ||
                ct1.GetMatchingLength("they") != "they".Length ||
                ct1.GetMatchingLength("their") != "their".Length ||
                ct1.GetMatchingLength("there") != "there".Length ||
                ct1.GetMatchingLength("thus") != "thus".Length )
                throw new Exception(" earlier added strings are not contained in char trie " );
            if( ct1.GetMatchingLength("thisaaa") != "this".Length ||
                ct1.GetMatchingLength("thesebbb") != "these".Length ||
                ct1.GetMatchingLength("thoseccc") != "those".Length ||
                ct1.GetMatchingLength("theddd") != "the".Length ||
                ct1.GetMatchingLength("theyeee") != "they".Length ||
                ct1.GetMatchingLength("theirfff") != "their".Length ||
                ct1.GetMatchingLength("thereggg") != "there".Length ||
                ct1.GetMatchingLength("thushhh") != "thus".Length )
                throw new Exception(" earlier added strings are not contained in char trie " );

        }

        [Test]
        public void TestExternalTrie()
        {
            ArrayList random_strings = new ArrayList();
            byte[] rbytes = new byte[ 128 ];
            Random rnd = new Random();
            DateTime start;

            try
            {
                ExternalTrie trie = new ExternalTrie( "test.trie" );
                using( trie )
                {
                    start = DateTime.Now;
                    int dummy;

                    for( int i = 0; i < 1000; ++i )
                    {
                        rnd.NextBytes( rbytes );
                        string rstr = Encoding.UTF8.GetString( rbytes );
                        if( rstr.Length > 0 )
                        {
                            random_strings.Add( rstr );
                            trie.AddString( rstr, out dummy );
                        }
                    }
                }
                Console.WriteLine( "Insertion of 1000 random strings into external trie took " +
                    ( DateTime.Now - start ).ToString() );
                trie = new ExternalTrie( "test.trie" );
                using( trie )
                {
                    start = DateTime.Now;
                    foreach( string rstr in random_strings)
                    {
                        if( trie.GetStringIndex( rstr ) < 0 )
                        {
                            trie.GetStringIndex( rstr );
                            throw new Exception( "One of strings, earlier inserted into the trie, could not be found" );
                        }
                    }
                }
                Console.WriteLine( "Searching for 1000 strings in the trie took " +
                    ( DateTime.Now - start ).ToString() );
            }
            finally
            {
                File.Delete( "test.trie" );
            }
        }
/*
        [Test]
        public void TestDictionaryCharTrie()
        {
            Random Rnd = new Random();
            StreamReader sr = new StreamReader( @"C:\OmniaMea\bin\Debug\Data\oxford.lex" );
            DateTime Start;
            string line;
            while( ( line = sr.ReadLine() ) != null );

            sr.BaseStream.Seek( 0, SeekOrigin.Begin );
            System.GC.Collect();

            Start = DateTime.Now;
            CharTrie ct = new CharTrie( null );
            while( ( line = sr.ReadLine() ) != null )
                ct.Add( line );
            Console.WriteLine( "Time spent on insertion of oxford.lex into char trie: " + (DateTime.Now - Start) );

            sr.BaseStream.Seek( 0, SeekOrigin.Begin );
            while( ( line = sr.ReadLine() ) != null )
            {
                ct.GetMatchingLength( line );
                ct.GetMatchingLength( line + "01" );
            }
            Console.WriteLine( "After searching for each word: " + (DateTime.Now - Start) );

            sr.BaseStream.Seek( 0, SeekOrigin.Begin );
            Start = DateTime.Now;
            ArrayList t = new ArrayList();
            while( ( line = sr.ReadLine() ) != null )
                t.Add(line);
            t.Sort();
            Console.WriteLine( "Time spent on insertion of oxford.lex into arraylist & sorting it: " + (DateTime.Now - Start) );

            sr.BaseStream.Seek( 0, SeekOrigin.Begin );
            while( ( line = sr.ReadLine() ) != null )
            {
                t.BinarySearch( line );
                t.BinarySearch( line + "01" );
            }
            Console.WriteLine( "After searching for each word: " + (DateTime.Now - Start) );

            sr.BaseStream.Seek( 0, SeekOrigin.Begin );
            Start = DateTime.Now;
            HashMap T = new HashMap( new HashMap.hFoo( HashFunctions.ObjectHF ), null );
            while( ( line = sr.ReadLine() ) != null )
                T[ line ] = null;
            Console.WriteLine( "Time spent on insertion of oxford.lex into hashmap: " + (DateTime.Now - Start) );

            sr.BaseStream.Seek( 0, SeekOrigin.Begin );
            while( ( line = sr.ReadLine() ) != null )
            {
                T[ line ] = null;
                T.Contains( line + "01" );
            }
            Console.WriteLine( "After searching for each word: " + (DateTime.Now - Start) );
        }*/
	}
}
