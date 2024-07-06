// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.ResourceStore
{
    /**
     * The predicate which matches all the links of a specified resource.
     */

    internal class ResourceLinkPredicate: ResourceListPredicate
    {
        private Resource _baseResource;
        private int      _propType;
        private int      _directPropType;
        private int      _reversePropType;
        private bool     _directed;
        private int      _knownType = -1;

        internal ResourceLinkPredicate( Resource baseResource, int propType, bool directed )
        {
            _baseResource = baseResource;

            _propType = propType;

            // since we're handling the notifications for target resources, the prop ID
            // for directed links will be reversed
            if ( MyPalStorage.Storage.IsLinkDirected( propType ) )
            {
                _directPropType = -propType;
                _reversePropType = directed ? -propType : propType;
            }
            else
            {
                _directPropType = propType;
                _reversePropType = propType;
            }
            _directed = directed;
        }

        internal override IntArrayList GetMatchingResources( out bool sortedById )
        {
            sortedById = true;
            return _baseResource.GetListOfLinks( _propType, _directed, true );
        }

        internal override IntArrayList GetSortedMatchingResourcesRef( out object syncObject )
        {
            syncObject = _baseResource;
            return _baseResource.GetListOfLinks( _propType, _directed, false );
        }

        internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
        {
            PredicateMatch match = MatchResourceByLink( res, cs, _directPropType );
            if ( match != PredicateMatch.Match && _directPropType != _reversePropType )
            {
                PredicateMatch match2 = MatchResourceByLink( res, cs, _reversePropType );
                if ( match2 == PredicateMatch.Match )
                    return match2;
                if ( match == PredicateMatch.Add && match2 == PredicateMatch.Del )
                    return PredicateMatch.Match;
                if ( match == PredicateMatch.Del && match2 == PredicateMatch.Add )
                    return PredicateMatch.Match;
                if ( match != PredicateMatch.None )
                    return match;
                return match2;
            }
            return match;
        }

        private PredicateMatch MatchResourceByLink( IResource res, IPropertyChangeSet cs, int propID )
        {
            LinkChangeType chg = LinkChangeType.None;
            if ( cs != null )
            {
                chg = cs.GetLinkChange( propID, _baseResource.Id );
            }
            switch( chg )
            {
                case LinkChangeType.Add:    return PredicateMatch.Add;
                case LinkChangeType.Delete: return PredicateMatch.Del;
                default:
                    return res.HasLink( propID, _baseResource )
                        ? PredicateMatch.Match
                        : PredicateMatch.None;
            }
        }

        internal override int GetSelectionCost()
        {
            return 3;
        }

        internal override int GetKnownType()
        {
            return _knownType;
        }

        internal void SetKnownType( int knownType )
        {
            _knownType = knownType;
        }

        public override string ToString()
        {
            return "Link(" + MyPalStorage.Storage.GetPropName( _propType ) + "," + _baseResource.Id + ")";
        }

        public override bool Equals( object obj )
        {
            if ( obj == null || !(obj is ResourceLinkPredicate) )
                return false;

            ResourceLinkPredicate rhs = (ResourceLinkPredicate) obj;
            return rhs._baseResource.Id == _baseResource.Id &&
                rhs._propType == _propType &&
                rhs._directed == _directed;
        }

        public override int GetHashCode()
        {
            return _baseResource.Id ^ ( _propType << 16 ) ^ ((_directed ? 1 : 0) << 31);
        }
    }
}
