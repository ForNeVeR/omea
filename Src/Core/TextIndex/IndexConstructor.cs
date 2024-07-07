// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JetBrains.DataStructures;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.TextIndex
{
    public class   IndexConstructor
    {
        public static string WorkDir
        {
            get { return strWorkDir;  }
            set { strWorkDir = value; }
        }

        public static void  FlushDocument( TermIndexAccessor termIndex, int docId, int maxTermInDoc, IntHashTable tokens )
        {
            foreach( IntHashTable.Entry e in tokens )
            {
                try
                {
                    termIndex.AddRecord( docId, e.Key, e.Value,  maxTermInDoc );
                }
                catch( Exception exc )
                {
                    Trace.WriteLineIf( !FullTextIndexer._suppTrace, "-- IndexConstructor -- Flushing document -- exception occured with key " + e.Key );
                    throw new FormatException( "-- IndexConstructor -- Flushing document -- exception occured with key " + e.Key, exc );
                }
            }
        }

        #region IndexConstruction
        internal static void  WriteEntry(
            BinaryWriter writer, int docId, int termId, object instances, int maxTermInDoc )
        {
            List<long> offsets = instances as List<long>;
            int     instancesOnDoc = ( offsets == null ) ? 1 : offsets.Count;
            float   tfIdf = CalcMetric( instancesOnDoc, maxTermInDoc, 1.0 );
            if( offsets != null )
            {
                WriteEntry( writer, docId, tfIdf, offsets );
            }
            else
            {
                WriteEntry( writer, docId, tfIdf, (long) instances );
            }
        }

        private static float CalcMetric( int instancesNumber, int maxTermFreqInDoc, double extRelevanceRatio )
        {
            double  tf = 0.5 + 0.5 * ((double)instancesNumber) / ((double)maxTermFreqInDoc );
            return( (float)( tf * extRelevanceRatio ) );
        }

        internal static void  WriteSignature( BinaryWriter fileIndex )
        {
            long    dateInTicks = DateTime.Now.Ticks;
            fileIndex.Write( dateInTicks );
            fileIndex.Write( IndexAccessorImpl.Version );  //  Version control signature
            fileIndex.Write( 0x7FFFFFFF );        //  Maximal term/doc ID (to be written later)
        }

        private static void WriteEntry( BinaryWriter writer, int docID, float tfIdf, ICollection<long> offsets )
        {
            WriteCount( writer, docID );
            writer.Write( tfIdf );
            WriteCount( writer, offsets.Count - 1 ); // save count minus 1
            foreach( long offset in offsets )
            {
                //  long value "Offset" consists of 3 fields:
                //  - (ushort) token order
                //  - (ushort) token sentence number
                //  - (int)    token offset;
                writer.Write( (uint) ( offset & 0xffffffff ) );
                writer.Write( (uint) ( offset >> 32 ) );
            }
        }

        private static void WriteEntry( BinaryWriter writer, int docID, float tfIdf, long offset )
        {
            WriteCount( writer, docID );
            writer.Write( tfIdf );
            WriteCount( writer, 0 ); // save count minus 1

            //  long value "Offset" consists of 3 fields:
            //  - (ushort) token order
            //  - (ushort) token sentence number
            //  - (int)    token offset;
            writer.Write( (uint) ( offset & 0xffffffff ) );
            writer.Write( (uint) ( offset >> 32 ) );
        }

        internal static void WriteCount( BinaryWriter writer, int count )
        {
            Debug.Assert( count >= 0 );
            while( count > 0x7f )
            {
                writer.Write( (byte) ( count & 0x7f ) );
                count >>= 7;
            }
            writer.Write( (byte) ( count + 0x80 ) );
        }

        internal static int ReadCount( BinaryReader reader )
        {
            int count = 0;
            int bits = 0;
            byte b;
            do
            {
                b = reader.ReadByte();
                count += ( ( b & 0x7f ) << bits );
                bits += 7;
            } while( ( b & 0x80 ) == 0 );
            return count;
        }
        #endregion IndexConstruction

        #region Attributes
        public  const   int         ciSignatureLength = 28;
        private static  string      strWorkDir;
        #endregion
    }
}
