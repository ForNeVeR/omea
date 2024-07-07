// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.JetListViewLibrary;

namespace JetBrains.Omea.GUIControls
{
    public class ResourceNameJetFilter: IJetListViewNodeFilter
    {
        private string _filterString;
        private string[] _filterStringWords;

        public ResourceNameJetFilter( string filterString )
        {
            SetFilterString( filterString );
        }

        private void SetFilterString( string s )
        {
            _filterString = s;
            _filterStringWords = s.Split( ' ', '.', '@' );
            for( int i=0; i<_filterStringWords.Length; i++ )
            {
                _filterStringWords [i] = _filterStringWords [i].ToLower();
            }
        }

        public event EventHandler FilterChanged;

        public bool AcceptNode( JetListViewNode node )
        {
            return _filterString.Length == 0 || NameMatches( node.Data.ToString() );
        }

        public string FilterString
        {
            get { return _filterString; }
            set
            {
                if ( _filterString != value )
                {
                    SetFilterString( value );
                    if ( FilterChanged != null )
                    {
                        FilterChanged( this, EventArgs.Empty );
                    }
                }
            }
        }

        /**
         * Checks if one of the words in the name begins with the specified
         * search string.
         */

        internal bool NameMatches( string str )
        {
            string[] words = str.Split( ' ', '.', '@', '\'', ',' );
            if ( _filterStringWords.Length == 1 )
            {
                foreach( string word in words )
                {
                    if ( word.ToLower().StartsWith( _filterStringWords [0] ) )
                        return true;
                }
            }
            else
            {
                if ( words.Length >= _filterStringWords.Length )
                {
                    for( int i=0; i<_filterStringWords.Length; i++ )
                    {
                        if ( !words [i].ToLower().StartsWith( _filterStringWords [i] ) )
                            return false;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
