/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using   System.IO;

namespace JetBrains.Omea.TextIndex
{
    //-----------------------------------------------------------------------------
    //  Wrapper around binary representation of the document record.
    //-----------------------------------------------------------------------------

    public class   DocIndexRecord
    {
        protected   int     iDocIndex;

        public DocIndexRecord( bool f_ ) {}
        public int   DocIndex    {   get{ return( iDocIndex ); } }

        public void  Init( BinaryReader reader, int len )
        {
            #region Preconditions
            if( len != 4 )
                throw( new System.FormatException( "DocIndexRecord -- Too short term record in initialization (" + len + ")" ));
            #endregion Preconditions

            iDocIndex = reader.ReadInt32();
            if( iDocIndex < -1 )
                throw( new System.FormatException( "DocIndexRecord -- Illegal value for Document Index header while parsing DocIndex record" ));
        }

        //-------------------------------------------------------------------------
        //  Assuming that caller has already set the necessay offset in the binary
        //  stream
        //-------------------------------------------------------------------------
        public void  Save( BinaryWriter writer_ )
        {
            writer_.Write( 4 );
            writer_.Write( iDocIndex );
        }
    }
}