/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>


using System;
using System.Collections;
using System.Diagnostics;

namespace JetBrains.Omea.TextIndex
{
/******************************************************************************
    ScriptMorphoAnalyzer internal structures - tree representation
******************************************************************************/

    class   SymBranch
    {
        public  SymBranch( char c, Node node )
        {
            CommonCounter++;
            cSym = c;
            DownLink = node;
        }
        public  char        cSym;
        public  Node        DownLink;
        public  static int  CommonCounter = 0;
    }

    class   Node
    {
        //---------------------------------------------------------------------
        //  NB: we assume that input symbol is already in lowercase, in which
        //      all the grammar rules are written (usually this is true, since
        //      morphological analysis is performed after tokenization, where
        //      all case transformations are done.
        //---------------------------------------------------------------------
        public  Node    findNodeByBranch( char c_ )
        {
            SymBranch sb_;

            int count = (aBranches != null) ? aBranches.Count : 0;
            while( count > 0 )
            {
                sb_ = ( SymBranch )aBranches[ --count ];
                if( sb_.cSym == c_ )
                    return( sb_.DownLink );
            }
            return( null );
        }

        //---------------------------------------------------------------------
        public  void    AddNodeBySymbol( Node NextNode, char c_ )
        {
            Debug.Assert( aBranches == null || findNodeByBranch( c_ ) == null );
            if( aBranches == null )
            {
                aBranches = new ArrayList();
            }
            aBranches.Add( new SymBranch( c_, NextNode ) );
        }

        //---------------------------------------------------------------------
        //  Split string into particular POS variants
        //---------------------------------------------------------------------

        public void    InitVariants( string alternativePOS, WhatToInit eo_ )
        {
            string[]    variants = alternativePOS.Split( '|' );
            string[]    ValuesRef = null;
            if( eo_ == WhatToInit.WordEndList )
                ValuesRef = aWordEnds = new string[ variants.Length ];
            else
                ValuesRef = aWildCards = new string[ variants.Length ];

            for( int i = 0; i < variants.Length; i++ )
            {
                string  variant = variants[ i ].Trim();
                int     separatorIndex = variant.IndexOf( ' ' );

                if( separatorIndex > 0 )
                    ValuesRef[ i ] = variant.Substring( 0, separatorIndex );
                else
                    throw new Exception( "Morphological grammar error -- Illegal format of Variants list: " + variant );
            }
        }

        //---------------------------------------------------------------------
        public  enum    WhatToInit { WhildCardList, WordEndList }

        public    string[]      aWordEnds, aWildCards;
        protected ArrayList     aBranches;
    }
}