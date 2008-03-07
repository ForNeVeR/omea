/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Text;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.MIME
{
    /** 
     * UUencoder/-decoder
     */
    public class UUParser
    {
        static UUParser()
        {
            _char2Byte = new byte[ 128 ];
            for( byte i = 0; i < _UUAlphabet.Length; ++i )
            {
                _char2Byte[ (int) _UUAlphabet[ i ] ] = i;
            }
        }

        public static byte[] Decode( string str )
        {
            int len = str.Length * 6; // in bits
            len = ( len >> 3 ) + ( ( len & 7 ) > 0 ? 1 : 0 ); // in bytes
            byte[] result = new byte[ len ];

            len = str.Length;

            uint code = 0;
            int bites = 0;
            int j = 0;

            for( int i = 0; i < len; ++i )
            {
                code += _char2Byte[ ( (int) str[ i ] ) & 127 ];
                if( ( bites += 6 ) == 24 )
                {
                    result[ j + 2 ] = (byte) ( code & 255 );
                    code >>= 8;
                    result[ j + 1 ] = (byte) ( code & 255 );
                    code >>= 8;
                    result[ j ] = (byte) ( code & 255 );
                    j += 3;
                    bites = 0;
                    code = 0;
                }
                code <<= 6;
            }

            j = result.Length;
            while( bites > 0 )
            {
                bites -= 8;
                result[ --j ] = (byte) ( code & 255 );
                code >>= 8;
            }
            
            return result;
        }

        /**
         * NOTE: if len is not multiple of 3, then more than len bytes
         * in the bytes array could be used
         */
        public static string Encode( byte[] bytes, int len )
        {
            int i = 0;
            uint code;
            StringBuilder builder = StringBuilderPool.Alloc();
            try 
            {
                while( len > 0 )
                {
                    code = (uint) bytes[ i + 2 ] + ( ((uint) bytes[ i + 1 ] ) << 8 ) + ( ((uint) bytes[ i ] ) << 16 );
                    builder.Append( _UUAlphabet[ (int) ( ( code >> 18 ) & 0x3f ) ] );
                    builder.Append( _UUAlphabet[ (int) ( ( code >> 12 ) & 0x3f ) ] );
                    builder.Append( _UUAlphabet[ (int) ( ( code >> 6 ) & 0x3f ) ] );
                    builder.Append( _UUAlphabet[ (int) ( code & 0x3f ) ] );
                
                    i += 3;
                    len -= 3;
                }

                return builder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( builder ) ;
            }
        }

        public static readonly string _UUAlphabet = "`!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_";
        private static byte[] _char2Byte;
    }
}