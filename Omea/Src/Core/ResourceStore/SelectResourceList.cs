// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Database;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.ResourceStore
{
    /**
     * The predicate which supports a list of resources that do not match a predicate
     * but need to remain in the list because it was created in snapshot mode.
     */

    internal abstract class SnapshotPredicate: ResourceListPredicate
    {
        protected bool         _snapshot;
        private IntArrayList _snapshotList = null;

        protected SnapshotPredicate( bool snapshot )
        {
            _snapshot = snapshot;
        }

        /**
         * Returns the necessary result for the situation when the resource changed
         * to match the predicate. It may have been already added to the snapshot
         * list, so the Match result instead of the Add result should be returned.
         */

        protected PredicateMatch CheckSnapshotAdd( int resID )
        {
            if ( _snapshot && _snapshotList != null )
            {
                if ( _snapshotList.IndexOf( resID ) >= 0 )
                {
                    return PredicateMatch.Match;
                }
            }
            return PredicateMatch.Add;
        }

        /**
         * Returns the necessary result for the situation when a resource no longer
         * matches the predicate. In snapshot mode, adds the resource to the snapshot
         * list; otherwise returns the "Removed from list" result.
         */

        protected PredicateMatch CheckSnapshotRemove( int resID )
        {
            if ( _snapshot )
            {
                if ( _snapshotList == null )
                {
                    _snapshotList = new IntArrayList();
                }
                _snapshotList.Add( resID );
                return PredicateMatch.Match;
            }
            return PredicateMatch.Del;
        }

        /**
         * Returns the necessary result for the situation where the resource
         * does not match the predicate.
         */

        protected PredicateMatch CheckSnapshotNoMatch( int resID )
        {
            if ( _snapshotList != null && _snapshotList.IndexOf( resID ) >= 0 )
            {
                return PredicateMatch.Match;
            }
            return PredicateMatch.None;
        }
    }

    /**
     * The predicate which matches resources that have a specified property.
     */

    internal class ResourcesWithPropPredicate: SnapshotPredicate
    {
        private int _propId;

        internal ResourcesWithPropPredicate( int propID, bool snapshot )
            : base( snapshot )
        {
            _propId = propID;
        }

        internal override IntArrayList GetMatchingResources( out bool sortedById )
        {
            sortedById = false;
            return GetResourcesFromResultSet( MyPalStorage.Storage.SelectResourcesWithProp( _propId ), 0 );
        }

        internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
        {
            if ( res.HasProp( _propId ) )
            {
                if ( cs != null && cs.IsPropertyChanged( _propId ) && cs.GetOldValue( _propId ) == null )
                    return CheckSnapshotAdd( res.Id );

                return PredicateMatch.Match;
            }
            else if ( cs != null && cs.GetOldValue( _propId ) != null )
            {
                return CheckSnapshotRemove( res.Id );
            }
            return CheckSnapshotNoMatch( res.Id );
        }

        internal override int GetSelectionCost()
        {
            return 3;
        }

        public override string ToString()
        {
            return "ResourcesWithProp(" + MyPalStorage.Storage.GetPropName( _propId ) + ")";
        }

        public override bool Equals( object obj )
        {
            if ( Object.ReferenceEquals( this, obj ) )
                return true;

            ResourcesWithPropPredicate rhs = obj as ResourcesWithPropPredicate;
            if ( rhs == null )
                return false;

            return _propId == rhs._propId && _snapshot == rhs._snapshot;
        }

        public override int GetHashCode()
        {
            return _propId;
        }
    }

    /**
     * The predicate which matches resources that have a specified link property.
     */

    internal class ResourcesWithLinkPredicate: SnapshotPredicate
    {
        private int _propId;

        internal ResourcesWithLinkPredicate( int propID, bool snapshot )
            : base( snapshot )
        {
            _propId = propID;
        }

        internal override IntArrayList GetMatchingResources( out bool sortedById )
        {
            bool reverseLink = (_propId < 0 && MyPalStorage.Storage.IsLinkDirected( -_propId ) );

            IResultSet rs = MyPalStorage.Storage.SelectLinksOfType( reverseLink ? -_propId : _propId );
            try
            {
                IntArrayList list;
                if ( reverseLink )
                {
                    list = DoGetResourcesFromResultSet( rs, 1 );
                    list.Sort();
                }
                else
                {
                    list = DoGetResourcesFromResultSet( rs, 0 );
                    list.Sort();
                    if ( !MyPalStorage.Storage.IsLinkDirected( _propId ) )
                    {
                        IntArrayList list2 = DoGetResourcesFromResultSet( rs, 1 );
                        list2.Sort();
                        list = IntArrayList.MergeSorted( list, list2 );
                    }
                }

                list.RemoveDuplicatesSorted();
                sortedById = true;
                return list;
            }
            finally
            {
                rs.Dispose();
            }
        }

        internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
        {
            int linkCount = res.GetLinkCount( _propId );
            if ( linkCount > 0 )
            {
                if ( cs != null && GetLinkCountDelta( cs, _propId ) == linkCount )
                    return CheckSnapshotAdd( res.Id );

                return PredicateMatch.Match;
            }
            else if ( cs != null && GetLinkCountDelta( cs, _propId ) < 0 )
            {
                return CheckSnapshotRemove( res.Id );
            }
            return CheckSnapshotNoMatch( res.Id );
        }

        private int GetLinkCountDelta( IPropertyChangeSet cs, int propId )
        {
            LinkChange[] linkChanges = cs.GetLinkChanges( propId );
            int count = 0;
            foreach( LinkChange change in linkChanges )
            {
                if ( change.ChangeType == LinkChangeType.Add )
                {
                    count++;
                }
                else
                {
                    count--;
                }
            }
            return count;

        }

        internal override int GetSelectionCost()
        {
            return 3;
        }

        public override string ToString()
        {
            return "ResourcesWithLink(" + MyPalStorage.Storage.GetPropName( _propId ) + ")";
        }

        public override bool Equals( object obj )
        {
            if ( Object.ReferenceEquals( this, obj ) )
                return true;

            ResourcesWithLinkPredicate rhs = obj as ResourcesWithLinkPredicate;
            if ( rhs == null )
                return false;

            return _propId == rhs._propId && _snapshot == rhs._snapshot;
        }

        public override int GetHashCode()
        {
            return _propId;
        }
    }

    /**
     * The predicate which matches resources having the value of a specific
     * property in a specific range (or equal to a value).
     */

    internal class PropValuePredicate: SnapshotPredicate
    {
        private int    _propId;
        private object _minValue;
        private object _maxValue;
        private string _minValueStr;
        private bool   _isStringList;

        internal PropValuePredicate( int propId, object minValue, object maxValue, bool snapshot )
            : base( snapshot )
        {
            _propId = propId;
            _minValue = minValue;
            _maxValue = maxValue;
            _minValueStr = _minValue as string;
            _isStringList = (MyPalStorage.Storage.PropTypes [propId].DataType == PropDataType.StringList);
        }

        internal override IntArrayList GetMatchingResources( out bool sortedById )
        {
            sortedById = false;
            IResultSet rs;
            if ( _maxValue == null )
            {
                rs = MyPalStorage.Storage.SelectResources( _propId, _minValue );
            }
            else
            {
                rs = MyPalStorage.Storage.SelectResourcesInRange( _propId, _minValue, _maxValue );
            }
            return GetResourcesFromResultSet( rs, 0 );
        }

        internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
        {
            if( MatchValue( res.GetProp( _propId ) ) )
            {
                if ( cs != null && cs.IsPropertyChanged( _propId ) && !MatchValue( cs.GetOldValue( _propId ) ) )
                    return CheckSnapshotAdd( res.Id );

                return PredicateMatch.Match;
            }
            else if ( cs != null && MatchValue( cs.GetOldValue( _propId ) ) )
            {
                return CheckSnapshotRemove( res.Id );
            }
            return CheckSnapshotNoMatch( res.Id );
        }

        internal override int GetSelectionCost()
        {
            return 2;
        }

        private bool MatchValue( object propValue )
        {
            if ( propValue == null )
                return false;

            if ( _maxValue == null )
            {
                if ( _minValueStr != null )
                {
                    if ( _isStringList )
                    {
                        PropertyStringList propValueList = (PropertyStringList) propValue;
                        return propValueList.IndexOfInsensitive( _minValueStr ) >= 0;
                    }
                    return String.Compare( _minValueStr, (string) propValue, true ) == 0;
                }
                return propValue.Equals( _minValue );
            }
            else
            {
                int rc1 = ((IComparable) _minValue).CompareTo( propValue );
                int rc2 = ((IComparable) _maxValue).CompareTo( propValue );
                return rc1 <= 0 && rc2 >= 0;
            }
        }

        public override string ToString()
        {
            return "PropValue(" + MyPalStorage.Storage.GetPropName( _propId ) + "," + _minValue +
                ( (_maxValue == null) ? "" : ( "," + _maxValue ) ) + ")";
        }

        public override bool Equals( object obj )
        {
            if ( obj == null || !(obj is PropValuePredicate) )
                return false;

            PropValuePredicate rhs = (PropValuePredicate) obj;
            return rhs._propId == _propId && rhs._minValue.Equals( _minValue ) &&
                ( ( _maxValue == null && rhs._maxValue == null ) ||
                  ( _maxValue != null && _maxValue.Equals( rhs._maxValue ) ) ) &&
                _snapshot == rhs._snapshot;
        }

        public override int GetHashCode()
        {
            int hc = _minValue.GetHashCode() ^ (_propId << 16);
            if ( _maxValue != null )
                hc ^= _maxValue.GetHashCode();

            return hc;
        }
    }
}
