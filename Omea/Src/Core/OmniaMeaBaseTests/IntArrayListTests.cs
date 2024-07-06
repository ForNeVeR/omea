// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using NUnit.Framework;
using JetBrains.Omea.Containers;

namespace OmniaMeaBaseTests
{
	[TestFixture]
    public class IntArrayListTests
	{
        private void AssertEquals( object expected, object actual )
        {
            Assert.AreEqual( expected, actual );
        }

        [Test] public void BasicTests()
        {
            IntArrayList list = new IntArrayList();
            AssertEquals( 0, list.Count );

            list.Add( 1 );
            AssertEquals( 1, list.Count );
            AssertEquals( 1, list [0] );

            list [0] = 3;
            AssertEquals( 3, list [0] );
        }

        [Test] public void MultiAdd()
        {
            IntArrayList list = new IntArrayList();
            for( int i=0; i<256; i++ )
                list.Add( i );

            AssertEquals( 256, list.Count );
            Assert.IsTrue( list.Capacity >= 256 );
            for( int i=0; i<256; i++)
                AssertEquals( i, list [i] );
        }


        [Test] public void Insert()
        {
            IntArrayList list = new IntArrayList();
            for( int i=0; i<256; i++ )
            {
                list.Insert( 0, i );
            }

            AssertEquals( 256, list.Count );
            for( int i=0; i<256; i++)
            {
                AssertEquals( 255-i, list [i] );
                AssertEquals( i, list.IndexOf( 255-i ) );
            }
        }

        [Test] public void RemoveAt()
        {
            IntArrayList list = new IntArrayList();
            for( int i=0; i<256; i++ )
                list.Add( i );

            for ( int i=0; i<255; i++ )
            {
                list.RemoveAt( 0 );
                AssertEquals( i+1, list [0] );
            }
            AssertEquals( 1, list.Count );
        }

        [Test] public void Enumeration()
        {
            IntArrayList list = new IntArrayList();
            list.Add( 1 );
            list.Add( 654 );

            IEnumerator enumerator = list.GetEnumerator();
            Assert.IsTrue( enumerator.MoveNext() );
            AssertEquals( 1, enumerator.Current );
            Assert.IsTrue( enumerator.MoveNext() );
            AssertEquals( 654, enumerator.Current );
            Assert.IsTrue( !enumerator.MoveNext() );
        }

        [Test] public void MergeSorted()
        {
            IntArrayList list1 = new IntArrayList();
            list1.AddRange( new int[] { 2, 3, 5, 8 } );

            IntArrayList list2 = new IntArrayList();
            list2.AddRange( new int[] { 2, 4, 5, 7 } );

            IntArrayList merged = IntArrayList.MergeSorted( list1, list2 );
            AssertEquals( 6, merged.Count );
            AssertEquals( 2, merged [0] );
            AssertEquals( 3, merged [1] );
            AssertEquals( 4, merged [2] );
            AssertEquals( 5, merged [3] );
            AssertEquals( 7, merged [4] );
            AssertEquals( 8, merged [5] );
        }

        [Test] public void SameValuesInIntersectSorted()
        {
            IntArrayList list1 = new IntArrayList();
            list1.AddRange( new int[] { 0, 1, 2, 3, 3 } );

            IntArrayList list2 = new IntArrayList();
            list2.AddRange( new int[] { 0, 1, 2, 3 } );

            IntArrayList intersected = IntArrayList.IntersectSorted( list1, list2 );
            Assert.AreEqual( 4, intersected.Count );

            AssertEquals( 0, intersected [0] );
            AssertEquals( 1, intersected [1] );
            AssertEquals( 2, intersected [2] );
            AssertEquals( 3, intersected [3] );
        }

        [Test] public void SameValuesInIntersectSortedInplace()
        {
            IntArrayList list1 = new IntArrayList();
            list1.AddRange( new int[] { 0, 1, 2, 3, 3 } );
            IntArrayList list2 = new IntArrayList();
            list2.AddRange( new int[] { 0, 1, 2, 3 } );
            list1.IntersectSorted( list2 );
            Assert.AreEqual( 4, list1.Count );
            AssertEquals( 0, list1[0] );
            AssertEquals( 1, list1[1] );
            AssertEquals( 2, list1[2] );
            AssertEquals( 3, list1[3] );

            list1.Clear(); list2.Clear();
            list1.AddRange( new int[] { 3, 3 } );
            list2.AddRange( new int[] { 0, 1, 2, 3 } );
            list1.IntersectSorted( list2 );
            Assert.AreEqual( 1, list1.Count );
            AssertEquals( 3, list1[0] );

            list1.Clear(); list2.Clear();
            list2.AddRange( new int[] { 3, 3 } );
            list1.AddRange( new int[] { 0, 1, 2, 3 } );
            list1.IntersectSorted( list2 );
            Assert.AreEqual( 1, list1.Count );
            AssertEquals( 3, list1[0] );

            list1.Clear(); list2.Clear();
            list2.AddRange( new int[] { 0, 0, 3, 3 } );
            list1.AddRange( new int[] { 0, 1, 2 } );
            list1.IntersectSorted( list2 );
            Assert.AreEqual( 1, list1.Count );
            AssertEquals( 0, list1[0] );

            list1.Clear(); list2.Clear();
            list2.AddRange( new int[] { 0, 0, 3, 3, 7, 8, 9, 12, 12, 13 } );
            list1.AddRange( new int[] { 0, 1, 2, 3, 4, 5, 8, 8, 12 } );
            list1.IntersectSorted( list2 );
            list1 = (IntArrayList) list1.Clone();
            Assert.AreEqual( 4, list1.Count );
            AssertEquals( 0, list1[0] );
            AssertEquals( 3, list1[1] );
            AssertEquals( 8, list1[2] );
            AssertEquals( 12, list1[3] );
        }

        [Test] public void SortTests()
        {
            IntArrayList list = new IntArrayList();
            int[] unsorted = new int[] { 5, 1, 2, 1, 3, 4, 7, 0, -1, 5, 9, 3, 5, 4 };
            list.AddRange( unsorted  );
            list.Sort();
            int[] array = list.ToArray();
            Array.Sort( unsorted );
            for( int i = 0; i < unsorted.Length; ++i )
            {
                AssertEquals( unsorted[ i ], array[ i ] );
            }
        }

        [Test] public void BinarySearchTests()
        {
            IntArrayList list = new IntArrayList();
            int[] sorted = new int[] { 0, 0, 1, 2, 3, 4, 6, 7, 8, 8, 8, 8, 8, 8, 9, 10 };
            list.AddRange( sorted );
            AssertEquals( Array.BinarySearch( sorted, 5 ), list.BinarySearch( 5 ) );
            AssertEquals( Array.BinarySearch( sorted, 11 ), list.BinarySearch( 11 ) );
            AssertEquals( Array.BinarySearch( sorted, 8 ), list.BinarySearch( 8 ) );
            AssertEquals( Array.BinarySearch( sorted, 0 ), list.BinarySearch( 0 ) );
            AssertEquals( Array.BinarySearch( sorted, 10 ), list.BinarySearch( 10 ) );
            AssertEquals( Array.BinarySearch( sorted, -1 ), list.BinarySearch( -1 ) );
            AssertEquals( Array.BinarySearch( sorted, 100 ), list.BinarySearch( 100 ) );
            list.Clear();
            AssertEquals( Array.BinarySearch( new int[] {}, 5 ), list.BinarySearch( 5 ) );
        }
	}
}
