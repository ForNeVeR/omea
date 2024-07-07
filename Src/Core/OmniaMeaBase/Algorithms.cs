// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Omea.Algorithms
{
	public class Sorts
	{
        private Sorts() {}

        public static void RadixSort( int[] array )
        {
            RadixSort( array, array.Length );
        }

        public static void RadixSort( int[] array, int arraySize )
        {
            int[] sortedArray1 = array;
            int[] sortedArray2 = new int[arraySize];

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

                for ( int i = 0; i < arraySize; i++ )
                {
                    counts[ (int)((sortedArray2[i] & mask ) >> dCount ) ]++;
                }
                for ( int i = 1; i < BASED; i++ )
                {
                    counts[i] = counts[i] + counts[i-1];
                }
                for ( int i = arraySize - 1; i >= 0; i-- )
                {
                    int number = sortedArray2[i];
                    int index = --counts[ (int)((number & mask) >> dCount ) ];
                    sortedArray1[index] = number;
                }
            }
        }

        public static void RadixSort( uint[] array )
        {
            RadixSort( array, array.Length );
        }

        public static void RadixSort( uint[] array, int arraySize )
        {
            uint[] sortedArray1 = array;
            uint[] sortedArray2 = new uint[arraySize];

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

                for ( int i = 0; i < arraySize; i++ )
                {
                    counts[ (uint)((sortedArray2[i] & mask ) >> dCount ) ]++;
                }
                for ( int i = 1; i < BASED; i++ )
                {
                    counts[i] = counts[i] + counts[i-1];
                }
                for ( int i = arraySize - 1; i >= 0; i-- )
                {
                    uint number = sortedArray2[i];
                    int index = (int)--counts[ (uint)((number & mask) >> dCount ) ];
                    sortedArray1[index] = number;
                }
            }
        }

    }
}
