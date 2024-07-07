// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.ContactsPlugin
{
    internal interface IContactBlockContainer
    {
        void AddContactBlock( int col, string caption, AbstractContactViewBlock block );
    }

    internal interface IContactTabBlockContainer
    {
        void AddContactBlock( string tabName, string caption, AbstractContactViewBlock block );
    }

    /**
     * Service for registering contact view panes.
     */

    internal class ContactService: IContactService
	{
        private static ContactService _theService;
        private readonly AnchoredList[] _blockCreators;
        private readonly Dictionary<string, AnchoredList> _BlockCreatorsByTab;

        public static ContactService GetInstance()
        {
            if ( _theService == null )
            {
                _theService = new ContactService();
            }
            return _theService;
        }

        private ContactService()
        {
            _blockCreators = new AnchoredList [2];
            _blockCreators [0] = new AnchoredList();
            _blockCreators [1] = new AnchoredList();

            _BlockCreatorsByTab = new Dictionary<string, AnchoredList>();
        }

	    public void RegisterContactEditBlock( int column, ListAnchor anchor, string blockID,
                                              ContactBlockCreator blockCreator )
	    {
            #region Preconditions
            if (column != 0 && column != 1)
                throw new ArgumentException( "Contact view column index must be either 0 or 1", "column" );
            #endregion Preconditions

            _blockCreators [column].Add( blockID, blockCreator, anchor );
	    }

        public void RegisterContactEditBlock( string tabName, ListAnchor anchor, string blockID,
                                              ContactBlockCreator blockCreator)
        {
            #region Preconditions
            if( String.IsNullOrEmpty( tabName ) )
                throw new ArgumentException( "Contact view Tab name must be non-null and not-empty string", "tabName" );
            #endregion Preconditions

            AnchoredList list = _BlockCreatorsByTab.ContainsKey( tabName )? _BlockCreatorsByTab[ tabName ] : new AnchoredList();;

            list.Add( blockID, blockCreator, anchor );
            _BlockCreatorsByTab[ tabName ] = list;
        }

        internal void CreateContactBlocks( IContactBlockContainer blockContainer )
        {
            for( int col = 0; col < 2; col++ )
            {
                AnchoredList blockList = _blockCreators[ col ];
                for( int i = 0; i < blockList.Count; i++ )
                {
                    ContactBlockCreator creator = (ContactBlockCreator) blockList[ i ];
                    blockContainer.AddContactBlock( col, blockList.GetKey( i ), creator() );
                }
            }
        }

        internal void CreateContactBlocks( IContactTabBlockContainer blockContainer )
        {
            foreach( string tabName in _BlockCreatorsByTab.Keys )
            {
                AnchoredList blockList = _BlockCreatorsByTab[ tabName ];
                for( int i = 0; i < blockList.Count; i++ )
                {
                    ContactBlockCreator creator = (ContactBlockCreator)blockList[ i ];
                    blockContainer.AddContactBlock( tabName, blockList.GetKey( i ), creator() );
                }
            }
        }
    }
}
