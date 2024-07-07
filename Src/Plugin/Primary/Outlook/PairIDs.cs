// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    /// <summary>
    /// Serves for storing entry and store IDs for Outlook Folder or MailItem
    /// </summary>
    [Serializable()]
    public class PairIDs : IComparable
    {
        protected string _entryId = string.Empty;
        protected string _storeId = string.Empty;

        public PairIDs( string entryId, string storeId )
        {
            _entryId = entryId;
            _storeId = storeId;
        }
        public static PairIDs Get( IResource resource )
        {
            if ( resource == null ) return null;
            IResource ownerStore = resource.GetLinkProp( PROP.OwnerStore );
            if ( ownerStore == null ) return null;
            string entryID = resource.GetStringProp( PROP.EntryID );
            if ( entryID == null ) return null;
            string storeID = ownerStore.GetStringProp( PROP.StoreID );
            if ( storeID == null ) return null;
            return new PairIDs( entryID, storeID );
        }

        public string EntryId { get { return _entryId; } }
        public string StoreId { get { return _storeId; } }

        public int CompareTo( Object obj )
        {
            PairIDs pair = (PairIDs)obj;
            if ( pair == null ) return 1;

            int cmp1 = _entryId.CompareTo( pair._entryId );
            if ( cmp1 != 0 ) return cmp1;

            int cmp2 = _storeId.CompareTo( pair._storeId );
            if ( cmp2 != 0 ) return cmp2;

            return 0;
        }

        public override bool Equals( Object obj )
        {
            if ( obj is PairIDs )
            {
                return CompareTo( obj ) == 0;
            }
            return false;
        }

        public override int GetHashCode ()
        {
            return _entryId.GetHashCode() ^ _storeId.GetHashCode();
        }

    }
}
