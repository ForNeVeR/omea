/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Diagnostics;
using JetBrains.Omea.Database;
using System.Windows.Forms;
using JetBrains.Omea.Containers;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.Base;

namespace DBUtil
{
	class Class1
	{
		private static void InsertRecord( ITable testTable, string name, int age, DateTime dateTime )
		{
			IRecord record = testTable.NewRecord();
			record.SetValue( 1, name );
//			testTable.SetValue( "Age", age );
//			testTable.SetValue( "Birthday", dateTime );
			record.Commit();
		}

        class MyObject : IComparable
        {
            private int _i;
            public MyObject( int i )
            {
                _i = i;
            }
            #region IComparable Members

            public int CompareTo(object obj)
            {
                return _i - ((MyObject)obj)._i;
            }

            #endregion

        }

        class MyComparer : IComparer
        {
            #region IComparer Members

            public int Compare(object x, object y)
            {
                return ((MyObject)x).CompareTo(y);
            }

            #endregion
        }

        public static void RadixSort( int[] array )
        {
            int ARRAY_SIZE = array.Length;
            int[] sortedArray1 = array;
            int[] sortedArray2 = new int[ARRAY_SIZE];

            const int BASED = 256;
            const int step = 8;
            int[] counts = new int[BASED];

            for ( int dCount = 0; dCount < 32; dCount += step )
            {
                for ( int i = 0; i < BASED; i++ )
                {
                    counts[i] = 0;
                }

                int[] temp = sortedArray1;
                sortedArray1 = sortedArray2;
                sortedArray2 = temp;
                int mask = ( BASED - 1 ) << dCount;

                for ( int i = 0; i < ARRAY_SIZE; i++ )
                {
                    counts[ (int)((sortedArray2[i] & mask ) >> dCount ) ]++;
                }
                for ( int i = 1; i < BASED; i++ )
                {
                    counts[i] = counts[i] + counts[i-1];
                }
                for ( int i = ARRAY_SIZE - 1; i >= 0; i-- )
                {
                    int number = sortedArray2[i];
                    int index = --counts[ (int)((number & mask) >> dCount ) ];
                    sortedArray1[index] = number;
                }
            }
        }
        public static void RadixSort( uint[] array )
        {
            int ARRAY_SIZE = array.Length;
            uint[] sortedArray1 = array;
            uint[] sortedArray2 = new uint[ARRAY_SIZE];

            const int BASED = 256;
            const int step = 8;
            uint[] counts = new uint[BASED];

            for ( int dCount = 0; dCount < 32; dCount += step )
            {
                for ( int i = 0; i < BASED; i++ )
                {
                    counts[i] = 0;
                }

                uint[] temp = sortedArray1;
                sortedArray1 = sortedArray2;
                sortedArray2 = temp;
                int mask = ( BASED - 1 ) << dCount;

                for ( int i = 0; i < ARRAY_SIZE; i++ )
                {
                    counts[ (uint)((sortedArray2[i] & mask ) >> dCount ) ]++;
                }
                for ( int i = 1; i < BASED; i++ )
                {
                    counts[i] = counts[i] + counts[i-1];
                }
                for ( int i = ARRAY_SIZE - 1; i >= 0; i-- )
                {
                    uint number = sortedArray2[i];
                    int index = (int)--counts[ (uint)((number & mask) >> dCount ) ];
                    sortedArray1[index] = number;
                }
            }
        }

        public interface IRadixIntValue
        {
            int GetValue( int item );
        }

        class RadixIntEntry
        {
            public int _item;
            public int _value;
            public RadixIntEntry _next;

            public RadixIntEntry( int item, int value )
            {
                _item = item;
                _value = value;
            }
        }

        public static void RadixSort( int[] array, IRadixIntValue radixIntValue )
        {
            if ( radixIntValue == null )
            {
                RadixSort( array );
                return;
            }
            int ARRAY_SIZE = array.Length;
            int[] sortedArray1 = array;
            //int[] sortedArray2 = new int[ARRAY_SIZE];

            const int BASED = 256;
            const int step = 8;
            RadixIntEntry[] counts = new RadixIntEntry[BASED];

            for ( int dCount = 0; dCount < 32; dCount += step )
            {
                for ( int i = 0; i < BASED; i++ )
                {
                    counts[i] = null;
                }

                //int[] temp = sortedArray1;
                //sortedArray1 = sortedArray2;
                //sortedArray2 = temp;
                int mask = ( BASED - 1 ) << dCount;

                for ( int i = ARRAY_SIZE - 1; i >= 0; i-- )
                {
                    int value = radixIntValue.GetValue( sortedArray1[i] );
                    int index = (int)((value  & mask ) >> dCount );
                    RadixIntEntry radixEntry = new RadixIntEntry( i, value );
                    object obj = counts[ index ];
                    if ( obj == null )
                    {
                        counts[ index ] = radixEntry;
                    }
                    else
                    {
                        RadixIntEntry prev = (RadixIntEntry)obj;
                        radixEntry._next = prev;
                        counts[ index ] = radixEntry;
                    }
                }

                /*
                temp = sortedArray1;
                sortedArray1 = sortedArray2;
                sortedArray2 = temp;
                */

                int idx = 0;
                for ( int i = 0; i < BASED; i++ )
                {
                    RadixIntEntry entry = (RadixIntEntry)counts[i];
                    while ( entry != null )
                    {
                        sortedArray1[ idx++ ] = entry._item;
                        entry = entry._next;
                    }
                }
                //int k = 1;
            }
        }

        public class MyValue : IRadixIntValue
        {
            #region IRadixIntValue Members

            public int GetValue(int item)
            {
                return 20 - item;
            }

            #endregion
        }

        public class TestKey : IFixedLengthKey, IComparer
        {
            private long _key;

            public TestKey( long key )
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
                _key = reader.ReadInt64();
            }

            public int CompareTo(object obj)
            {
                return _key.CompareTo( ((TestKey)obj)._key );
            }

            public IFixedLengthKey FactoryMethod( BinaryReader reader )
            {
                TestKey testKey = new TestKey();
                testKey.Read( reader );
                return testKey;
            }
            public IFixedLengthKey FactoryMethod( )
            {
                TestKey testKey = new TestKey();
                testKey._key = _key;
                return testKey;
            }

            public IComparable Key
            {
                get { return _key; }
                set { _key = (long)value; }
            }

            public int KeySize { get{ return 8; } }

            public void SetIntKey( int key )
            {
                _key = key;
            }

            #endregion

            #region IComparer Members

            public int Compare(object x, object y)
            {
                TestKey xKey = (TestKey)x;
                TestKey yKey = (TestKey)y;
                return (int)(xKey._key - yKey._key);
            }

            #endregion
        }

        [STAThread]
		static void Main(string[] args)
		{
            Random rand = new Random( Int32.MaxValue );

            int i1 = ("1308554180").GetHashCode();
            int i2 = ("2117097963").GetHashCode();
            if ( i1 == i2 )
            {
                Tracer._Trace("OK");
            }
            return;
        }
	}
}
