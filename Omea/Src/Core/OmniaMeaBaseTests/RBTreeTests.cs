// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using NUnit.Framework;
using JetBrains.Omea.Containers;
using System.Diagnostics;

namespace OmniaMeaBaseTests
{
    [TestFixture]
    public class RBTreeTests
    {
        public RBTreeTests()
        {
        }

        [Test]
        public void GetMinMaxTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            for ( int i = 0; i < 1000; i++ )
            {
                tree.RB_Insert( i );
            }
            int min = (int)tree.GetMinimum();
            Assert.AreEqual( 0, min );
            int max = (int)tree.GetMaximum();
            Assert.AreEqual( 999, max );
        }
        [Test]
        public void DeletingTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            for ( int i = 0; i < 1000; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = 0; i < 1000; i++ )
            {
                tree.RB_Delete( i );
            }
        }
        /*
        [Test]
        public void SuccessorTest()
        {
            RedBlackTree tree = new RedBlackTree();
            for ( int i = 0; i < 1000; i++ )
            {
                tree.RB_Insert( i );
            }
            RBNode node = tree.GetMinimum();
            for ( int i = 1; i < 1000; i++ )
            {
                node = tree.GetSuccessor( node );
                Assert.AreEqual( i, node.Key );
            }
        }*/

        [Test]
        public void SearchTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            for ( int i = 0; i < 1000; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = 0; i < 1000; i++ )
            {
                int found = tree.SearchForIndex( i );
                Assert.AreEqual( i, found );
            }
        }
        [Test]
        public void SearchAfterDeletingTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            for ( int i = 0; i < 1000; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = 0; i < 1000; i = i + 2 )
            {
                tree.RB_Delete( i );
            }
            for ( int i = 0; i < 499; i++ )
            {
                int found = (int)tree.OS_Select( i );
                Assert.AreEqual( i * 2 + 1, found );
            }
        }
        [Test]
        public void OS_SelectTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            int NUMBER = 40;
            //int TEST_NUM = 8;
            for ( int i = 0; i < NUMBER; i++ )
            {
                tree.RB_Insert( i );
            }

            Assert.AreEqual( NUMBER, tree.Count );
            //Trace.WriteLine( "______________________________" );

            try
            {
                //Trace.WriteLine( "________delete 8______________________" );
                tree.RB_Delete( 8 );
                //Trace.WriteLine( "________delete 9______________________" );
                tree.RB_Delete( 9 );
                //Trace.WriteLine( "________delete 10______________________" );
                tree.RB_Delete( 10 );
                //Trace.WriteLine( "________delete 11______________________" );
                tree.RB_Delete( 11 );
                //Trace.WriteLine( "________delete 12______________________" );
                tree.RB_Delete( 12 );
                //Trace.WriteLine( "________delete 13______________________" );
                tree.RB_Delete( 13 );
                //Trace.WriteLine( "________delete 14______________________" );
                tree.RB_Delete( 14 );
                //Trace.WriteLine( "______________________________" );
                //tree.InorderPrint();
                tree.RB_Delete( 15 );
                for ( int j = 0; j < 32; j++ )
                {
                    tree.OS_Select( j );
                    //Assert.AreEqual( i, found );
                }
            }
            catch ( InvalidCastException ex )
            {
                Trace.WriteLine( ex.Message );
                Trace.WriteLine( "______________________________" );
                tree.InorderPrint();
                Trace.WriteLine( "tree.Count: " + tree.Count );
                throw ex;
            }

        }

        /*
        [Test]
        public void SuccessorAfterDeletingTest()
        {
            RedBlackTree tree = new RedBlackTree();
            for ( int i = 0; i < 1000; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = 0; i < 1000; i = i + 2 )
            {
                tree.RB_Delete( i );
            }
            RBNode testNode = tree.GetMinimum();
            for ( int i = 1; i < 1000; i = i + 2 )
            {
                Assert.AreEqual( i, testNode.Key );
                testNode = tree.GetSuccessor( testNode );
            }
        }
        */
        [Test]
        public void InsertingAfterDeletingTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            int NUMBER = 12;
            int HALF = NUMBER/2;
            for ( int i = 0; i < NUMBER; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = HALF; i < NUMBER; i++ )
            {
                tree.RB_Delete( i );
            }
            for ( int i = HALF; i < NUMBER; i++ )
            {
                tree.RB_Insert( i );
            }

            //RBNode testNode = tree.GetMinimum();
            //Assert.AreEqual( 0, testNode.Key );
            for ( int i = 0; i < NUMBER; i++ )
            {
                Assert.AreEqual( i, tree.OS_Rank( i ) );
                //testNode = tree.GetSuccessor( testNode );
            }
        }
        [Test]
        public void InsertingAfterDeletingTest1000()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            int NUMBER = 1000;
            int HALF = NUMBER/2;
            for ( int i = 0; i < NUMBER; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = HALF; i < NUMBER; i++ )
            {
                tree.RB_Delete( i );
            }
            for ( int i = HALF; i < NUMBER; i++ )
            {
                tree.RB_Insert( i );
            }

            //RBNode testNode = tree.GetMinimum();
            //Assert.AreEqual( 0, testNode.Key );
            for ( int i = 0; i < NUMBER; i++ )
            {
                Assert.AreEqual( i, tree.OS_Rank( i ) );
                //testNode = tree.GetSuccessor( testNode );
            }
        }
        [Test]
        public void InsertingAfterDeletingTest1()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            int NUMBER = 6;
            int HALF = NUMBER/2;
            for ( int i = 0; i < NUMBER; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = HALF; i < NUMBER; i++ )
            {
                tree.RB_Delete( i );
            }

            int test = tree.SearchForIndex( 4 );
            if ( test.Equals( 4 ) )
            {
                Assert.Fail("test.Key == 4");
            }
            for ( int i = HALF; i < NUMBER; i++ )
            {
                tree.RB_Insert( i );
            }

            //RBNode testNode = tree.GetMinimum();
            //Assert.AreEqual( 0, testNode.Key );
            for ( int i = 0; i < NUMBER; i++ )
            {
                Assert.AreEqual( i, tree.OS_Rank( i ) );
                //testNode = tree.GetSuccessor( testNode );
            }
        }
        [Test]
        public void OS_Select_Test()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            for ( int i = 0; i < 1000; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = 0; i < 1000; i++ )
            {
                Assert.AreEqual( i, (int)tree.OS_Select( i ) );
            }
        }
        [Test]
        public void OS_Select_AfterDeletingTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            int NUMBER = 1000;
            int HALF = NUMBER/2;
            for ( int i = 0; i < NUMBER; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = HALF; i < NUMBER; i++ )
            {
                tree.RB_Delete( i );
            }

            for ( int i = 0; i < HALF; i++ )
            {
                Assert.AreEqual( i, (int)tree.OS_Select( i ) );
            }

            for ( int i = HALF; i < NUMBER; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = 0; i < NUMBER; i++ )
            {
                Assert.AreEqual( i, (int)tree.OS_Select( i ) );
            }
        }
        [Test]
        public void OS_Rank_Test()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            for ( int i = 0; i < 1000; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = 0; i < 1000; i++ )
            {
                Assert.AreEqual( i, tree.OS_Rank( i ) );
            }
        }
        [Test]
        public void OS_Rank_AfterDeletingTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            int NUMBER = 1000;
            int HALF = NUMBER/2;
            for ( int i = 0; i < NUMBER; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = HALF; i < NUMBER; i++ )
            {
                tree.RB_Delete( i );
            }

            for ( int i = 0; i < HALF; i++ )
            {
                Assert.AreEqual( i, tree.OS_Rank( i ) );
            }

            for ( int i = HALF; i < NUMBER; i++ )
            {
                tree.RB_Insert( i );
            }
            for ( int i = 0; i < NUMBER; i++ )
            {
                Assert.AreEqual( i, tree.OS_Rank( i ) );
            }
        }
        [Test]
        public void CountTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            tree.RB_Insert( 3 );
            Assert.AreEqual( 0, tree.SearchForIndex( 3 ));
            Assert.AreEqual( 1, tree.Count);
            tree.RB_Delete( 3 );
            Assert.AreEqual( 0, tree.Count);
            Assert.AreEqual( -1, tree.SearchForIndex( 3 ));

            tree.RB_Insert( 2 );
            tree.RB_Insert( 4 );
            tree.RB_Insert( 5 );
            tree.RB_Insert( 6 );
            tree.RB_Insert( 7 );
            tree.RB_Insert( 8 );
            tree.RB_Insert( 9 );
            tree.RB_Insert( 10 );
            tree.RB_Insert( 11 );
            tree.RB_Insert( 12 );
            Assert.AreEqual( 10, tree.Count);
            tree.RB_Delete(8);
            Assert.AreEqual( 9, tree.Count);
            tree.RB_Delete(4);
            Assert.AreEqual( 8, tree.Count);
            tree.RB_Delete(2);
            Assert.AreEqual( 7, tree.Count);
            tree.RB_Delete(10);
            Assert.AreEqual( 6, tree.Count);
            tree.RB_Delete(12);
            Assert.AreEqual( 5, tree.Count);
            tree.RB_Delete(6);
            Assert.AreEqual( 4, tree.Count);
            tree.RB_Delete(7);
            Assert.AreEqual( 3, tree.Count);
            tree.RB_Delete(5);
            Assert.AreEqual( 2, tree.Count);
            tree.RB_Delete(11);
            Assert.AreEqual( 1, tree.Count);
            tree.RB_Delete(9);
            Assert.AreEqual( 0, tree.Count);
        }
        [Test]
        public void SearchForIndexTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            ArrayList list = new ArrayList();
            Assert.AreEqual( list.BinarySearch( 3 ), tree.SearchForIndex( 3 ) );
            tree.RB_Insert( 3 );
            list.Add( 3 );
            Assert.AreEqual( list.BinarySearch( 3 ), tree.SearchForIndex( 3 ) );
            Assert.AreEqual( list.BinarySearch( 4 ), tree.SearchForIndex( 4 ) );
            Assert.AreEqual( list.BinarySearch( 2 ), tree.SearchForIndex( 2 ) );
            Assert.AreEqual( 1, tree.Count );
        }
        [Test]
        public void SearchForIndexTest2()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            ArrayList list = new ArrayList();
            Assert.AreEqual( list.BinarySearch( 3 ), tree.SearchForIndex( 3 ) );
            tree.RB_Insert( 3 );
            list.Add( 3 );
            tree.RB_Insert( 4 );
            list.Add( 4 );
            tree.RB_Insert( 6 );
            list.Add( 6 );
            tree.RB_Insert( 7 );
            list.Add( 7 );
            Assert.AreEqual( list.BinarySearch( 1 ), tree.SearchForIndex( 1 ) );
            Assert.AreEqual( list.BinarySearch( 2 ), tree.SearchForIndex( 2 ) );
            Assert.AreEqual( list.BinarySearch( 3 ), tree.SearchForIndex( 3 ) );
            Assert.AreEqual( list.BinarySearch( 4 ), tree.SearchForIndex( 4 ) );
            Assert.AreEqual( list.BinarySearch( 5 ), tree.SearchForIndex( 5 ) );
            Assert.AreEqual( list.BinarySearch( 6 ), tree.SearchForIndex( 6 ) );
            Assert.AreEqual( list.BinarySearch( 7 ), tree.SearchForIndex( 7 ) );
            Assert.AreEqual( list.BinarySearch( 8 ), tree.SearchForIndex( 8 ) );
            Assert.AreEqual( 4, tree.Count );
        }
        [Test]
        public void SearchForIndexTest3()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            tree.RB_Insert( "zhu0" );
            tree.RB_Insert( "zhu1" );
            tree.RB_Insert( "zhu2" );
            tree.RB_Insert( "zhu3" );
            tree.RB_Insert( "zhu4" );
            tree.RB_Insert( "zhu5" );
            tree.RB_Insert( "zhu6" );
            tree.RB_Insert( "zhu7" );
            tree.RB_Insert( "zhu8" );
            tree.RB_Insert( "zhu9" );
            tree.RB_Delete( "zhu4" );
            tree.RB_Delete( "zhu0" );
            tree.RB_Delete( "zhu1" );
            tree.RB_Delete( "zhu2" );
            tree.RB_Delete( "zhu3" );
            tree.SearchForIndex( "zhu4" );
        }
        [Test]
        public void SearchForIndexTest4()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            tree.RB_Insert( "zhu0" );
            tree.RB_Insert( "zhu1" );
            tree.RB_Insert( "zhu2" );
            tree.RB_Delete( "zhu1" );
            int i = ~tree.SearchForIndex( "zhu1" );
            Assert.AreEqual( 1, i );
        }
        [Test]
        public void SearchForIndexTest5()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            tree.RB_Insert( "zhu0" );
            tree.RB_Insert( "zhu1" );
            tree.RB_Insert( "zhu2" );
            tree.RB_Insert( "zhu3" );
            tree.RB_Insert( "zhu4" );
            tree.RB_Insert( "zhu5" );
            tree.RB_Insert( "zhu6" );
            tree.RB_Insert( "zhu7" );
            tree.RB_Insert( "zhu8" );
            tree.RB_Insert( "zhu9" );
            //Trace.WriteLine("_______before deleting of zhu0______");
            //tree.InorderPrint();
            //Trace.WriteLine("_______deleting of zhu0___");
            tree.RB_Delete( "zhu0" );
            //tree.InorderPrint();
            //Trace.WriteLine("____________________________________");
            //Trace.WriteLine("_______inserting of SergZ___");
            tree.RB_Insert( "SergZ" );
            //tree.InorderPrint();
            //Trace.WriteLine("____________________________________");
            //Trace.WriteLine("_______deleting of zhu1___");
            tree.RB_Delete( "zhu1" );
            //tree.InorderPrint();

            Assert.AreEqual( "zhu2", tree.OS_Select( 1 ) );
        }
        [Test]
        public void GetEqualOrMoreTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            tree.RB_Insert( "zhu0" );
            tree.RB_Insert( "zhu1" );
            tree.RB_Insert( "zhu2" );
            tree.RB_Insert( "zhu3" );
            RBNodeIndexAccess node = tree.GetEqualOrMore( "zhu1", null );
            Assert.AreEqual( "zhu1", (string)node.Key );
            node = tree.GetNext( node );
            Assert.AreEqual( "zhu2", (string)node.Key );
            node = tree.GetNext( node );
            Assert.AreEqual( "zhu3", (string)node.Key );
            node = tree.GetNext( node );
            Assert.AreEqual( null, node );
        }
        [Test]
        public void GetEqualOrMoreTest2()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            tree.RB_Insert( "zhu0" );
            tree.RB_Insert( "zhu4" );
            tree.RB_Insert( "zhu2" );
            tree.RB_Insert( "zhu3" );
            RBNodeIndexAccess node = tree.GetEqualOrMore( "zhu1", null );
            Assert.AreEqual( "zhu2", (string)node.Key );
            node = tree.GetNext( node );
            Assert.AreEqual( "zhu3", (string)node.Key );
            node = tree.GetNext( node );
            Assert.AreEqual( "zhu4", (string)node.Key );
            node = tree.GetNext( node );
            Assert.AreEqual( null, node );
        }

        class TestObjectForEqualOrMore : IComparable
        {
            private string _str;
            private int _num;

            public TestObjectForEqualOrMore( string str, int num )
            {
                _str = str;
                _num = num;
            }
            #region IComparable Members

            public int CompareTo(object obj)
            {
                return _str.CompareTo( ((TestObjectForEqualOrMore)obj)._str );
            }
            public string Str
            {
                get { return _str; }
            }
            public int Num
            {
                get { return _num; }
            }

            #endregion
        }

        private void ValuesTest( RBNodeBase node, string str, int num )
        {
            Assert.AreEqual( str, ((TestObjectForEqualOrMore)node.Key).Str );
            Assert.AreEqual( num, ((TestObjectForEqualOrMore)node.Key).Num );
        }
        [Test]
        public void GetNextAndPreviousTest()
        {
            RedBlackTree tree = new RedBlackTree();
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu2", 2 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu3", 3 ) );

            RBNodeBase node = tree.GetEqualOrMore( new TestObjectForEqualOrMore( "zhu0", 0 ), null );
            ValuesTest( node, "zhu0", 0 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu1", 1 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu2", 2 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu3", 3 );
            RBNodeBase nullNode = tree.GetNext( node );
            Assert.AreEqual( null, nullNode );

            node = tree.GetPrevious( node );
            ValuesTest( node, "zhu2", 2 );

            node = tree.GetPrevious( node );
            ValuesTest( node, "zhu1", 1 );

            node = tree.GetPrevious( node );
            ValuesTest( node, "zhu0", 0 );

            nullNode = tree.GetPrevious( node );
            Assert.AreEqual( null, nullNode );
        }
        [Test]
        public void GetEqualOrLessTest()
        {
            RedBlackTree tree = new RedBlackTree();
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu2", 2 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu3", 3 ) );

            RBNodeBase node = tree.GetEqualOrLess( new TestObjectForEqualOrMore( "zhu1", 0 ) );
            ValuesTest( node, "zhu1", 1 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu2", 2 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu3", 3 );
        }
        [Test]
        public void GetEqualOrLessTest2()
        {
            RedBlackTree tree = new RedBlackTree();
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu2", 2 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu3", 3 ) );

            RBNodeBase node = tree.GetEqualOrLess( new TestObjectForEqualOrMore( "zhu1", 0 ) );
            ValuesTest( node, "zhu0", 0 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu2", 2 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu3", 3 );
        }
        [Test]
        public void GetEqualOrLessTest3()
        {
            RedBlackTree tree = new RedBlackTree();
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );

            RBNodeBase node = tree.GetEqualOrLess( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            ValuesTest( node, "zhu0", 0 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu0", 0 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu0", 0 );
        }
        [Test]
        public void GetEqualOrLessTest4()
        {
            RedBlackTree tree = new RedBlackTree();
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu2", 2 ) );

            RBNodeBase node = tree.GetEqualOrLess( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            ValuesTest( node, "zhu1", 1 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu1", 1 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu1", 1 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu1", 1 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu1", 1 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu1", 1 );
            node = tree.GetNext( node );
            ValuesTest( node, "zhu2", 2 );
        }
        [Test]
        public void GetEqualOrLessTest5()
        {
            RedBlackTree tree = new RedBlackTree();
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu2", 2 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu2", 2 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu2", 2 ) );

            RBNodeBase node = tree.GetEqualOrLess( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            ValuesTest( node, "zhu0", 0 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu2", 2 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu2", 2 );
        }
        [Test]
        public void GetEqualOrLessTest6()
        {
            RedBlackTree tree = new RedBlackTree();
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu2", 2 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu2", 2 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu2", 2 ) );

            tree.GetEqualOrLess( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            //ValuesTest( node, "zhu1", 1 );
            //node = tree.GetNext( node );
/*
            ValuesTest( node, "zhu2", 2 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu2", 2 );
            */
        }

        /*
        [Test]
        public void GetEqualAndMoreWithSameKeysTest()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 1 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 2 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 3 ) );

            RBNodeIndexAccess node = tree.GetEqualOrMore( new TestObjectForEqualOrMore( "zhu0", 0 ), null );
            ValuesTest( node, "zhu0", 0 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu0", 1 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu0", 2 );
            node = tree.GetNext( node );

            ValuesTest( node, "zhu0", 3 );
        }
*/

        [Test]
        public void GetEqualOrMoreTest3()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu0", 0 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu4", 4 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu2", 2 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 5 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu3", 3 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 6 ) );
            tree.RB_Insert( new TestObjectForEqualOrMore( "zhu1", 7 ) );

            RBNodeIndexAccess node = tree.GetEqualOrMore( new TestObjectForEqualOrMore( "zhu1", 0 ), null );
            Assert.AreEqual( "zhu1", ((TestObjectForEqualOrMore)node.Key).Str );
            Assert.AreEqual( 5, ((TestObjectForEqualOrMore)node.Key).Num );

            node = tree.GetNext( node );

            Assert.AreEqual( "zhu1", ((TestObjectForEqualOrMore)node.Key).Str );
            Assert.AreEqual( 6, ((TestObjectForEqualOrMore)node.Key).Num );

            node = tree.GetNext( node );

            Assert.AreEqual( "zhu1", ((TestObjectForEqualOrMore)node.Key).Str );
            Assert.AreEqual( 7, ((TestObjectForEqualOrMore)node.Key).Num );
        }

        [Test]
        public void GetEqualOrMoreTest4()
        {
            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            for ( int i = 0; i < 100000; i++ )
            {
                tree.RB_Insert( i );
            }

            for ( int i = 10000; i < 10010; i++ )
            {
                tree.RB_Delete( i );
                RBNodeIndexAccess node = tree.GetEqualOrMore( i, null );
                Assert.AreEqual( i + 1, (int)node.Key );
                for ( int j = i + 2; j < 100000; j++ )
                {
                    node = tree.GetNext( node );
                    Assert.AreEqual( j, (int)node.Key );
                }
            }
        }
        class TestObject : IComparable
        {
            public int _value;
            public TestObject( int value )
            {
                _value = value;
            }
            #region IComparable Members

            public int CompareTo(object obj)
            {
                //return ( _value - ((TestObject)obj)._value );
                return ( ((TestObject)obj)._value - _value );
            }

            #endregion
        }
        [Test]
        public void StressTestForGetMinimum()
        {

            RedBlackTreeWithIndexAccess tree = new RedBlackTreeWithIndexAccess();
            for ( int i = 0; i < 1000000; i++ )
            {
                tree.RB_Insert( new TestObject( 4 ) );
            }
            tree.RB_Insert( new TestObject( 1 ) );
            RBNodeIndexAccess node = null;
            int key = 0;
            while ( key != 1 )
            {
                node = tree.GetMinimumNode();
                key = ((TestObject)node.Key)._value;
                tree.RB_Delete( node );
            }
        }

        class TestSortObject : IComparable
        {
            public int _index;
            public int _value;
            public TestSortObject( int index, int value )
            {
                _index = index;
                _value = value;
            }
            #region IComparable Members

            public int CompareTo(object obj)
            {
                return ( ((TestSortObject)obj)._value - _value );
            }

            #endregion
        }

        public class Comp : IComparer
        {
            #region IComparer Members

            public int Compare(object x, object y)
            {
                return ((TestSortObject)x)._value - ((TestSortObject)y)._value;
            }

            #endregion
        }

        [Test]
        public void TestForStableSort()
        {

            RedBlackTree tree = new RedBlackTree( new Comp() );
            int count = 0;
            for ( int i = 0; i < 100000; i++ )
            {
                tree.RB_Insert( new TestSortObject( count++, 0 ) );
                tree.RB_Insert( new TestSortObject( count++, 1 ) );
                tree.RB_Insert( new TestSortObject( count++, 2 ) );
            }
            for ( int i = 0; i < 100000; i++ )
            {
                RBNodeBase node = tree.GetMinimumNode();
                Assert.AreEqual( i*3, ((TestSortObject)node.Key)._index );
                Assert.AreEqual( 0, ((TestSortObject)node.Key)._value );
                tree.RB_Delete( node );
            }
            for ( int i = 0; i < 100000; i++ )
            {
                RBNodeBase node = tree.GetMinimumNode();
                Assert.AreEqual( i*3+1, ((TestSortObject)node.Key)._index );
                Assert.AreEqual( 1, ((TestSortObject)node.Key)._value );
                tree.RB_Delete( node );
            }
            for ( int i = 0; i < 100000; i++ )
            {
                RBNodeBase node = tree.GetMinimumNode();
                Assert.AreEqual( i*3+2, ((TestSortObject)node.Key)._index );
                Assert.AreEqual( 2, ((TestSortObject)node.Key)._value );
                tree.RB_Delete( node );
            }
            /*
            for ( int i = 0; i < 20; i++ )
            {
                RBNode node = tree.GetMinimumNode();
                Debug.WriteLine( ((TestSortObject)node.Key)._index );
                //Assert.AreEqual( i, 199999 - ((TestSortObject)node.Key)._index );
                tree.RB_Delete( node );
            }*/
        }
    }
}
