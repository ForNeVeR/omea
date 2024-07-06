// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.ResourceStore
{
	/**
     * The difference of two predicates.
     */

    internal class MinusPredicate: ResourceListPredicate
	{
		private ResourceListPredicate _lhs;
        private ResourceListPredicate _rhs;

        internal MinusPredicate( ResourceListPredicate lhs, ResourceListPredicate rhs )
		{
            _lhs = lhs;
            _rhs = rhs;
		}

        internal override int GetSelectionCost()
        {
            return _lhs.GetSelectionCost();
        }

        internal override int GetKnownType()
        {
            return _lhs.GetKnownType();
        }

        internal override IntArrayList GetMatchingResources( out bool sortedById )
        {
            bool lhsSortedById = false;
            IntArrayList result = _lhs.GetMatchingResources( out lhsSortedById );
            if ( _rhs.GetSelectionCost() >= 3 )
            {
                for( int i=result.Count-1; i >= 0; i-- )
                {
                    IResource resource;

                    try
                    {
                        resource = MyPalStorage.Storage.LoadResource( result [i], true, _lhs.GetKnownType() );
                    }
                    catch( InvalidResourceIdException )
                    {
                        result.RemoveAt( i );
                        continue;
                    }

                    if ( resource.IsDeleted || _rhs.MatchResource( resource, null ) != PredicateMatch.None )
                    {
                        result.RemoveAt( i );
                    }
                }
            }
            else
            {
                if ( !lhsSortedById )
                {
                    result.Sort();
                    lhsSortedById = true;
                }

                object rhsSyncObject;
                IntArrayList minus = _rhs.GetSortedMatchingResourcesRef( out rhsSyncObject );
                if ( rhsSyncObject == null )
                {
                    result.MinusSorted( minus );
                }
                else
                {
                    lock( rhsSyncObject )
                    {
                        result.MinusSorted( minus );
                    }
                }
            }
            sortedById = lhsSortedById;
            return result;
        }

        internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
        {
            int oldMatchL = 0, newMatchL = 0, oldMatchR = 0, newMatchR = 0;

            _lhs.GetMatchCounts( res, cs, ref oldMatchL, ref newMatchL );
            _rhs.GetMatchCounts( res, cs, ref oldMatchR, ref newMatchR );
            return MatchFromCounts( oldMatchL - oldMatchR, newMatchL - newMatchR );
        }

        internal override ResourceListPredicate Optimize( bool isLive )
        {
            _lhs = _lhs.Optimize( isLive );
            _rhs = _rhs.Optimize( isLive );
            return base.Optimize( isLive );
        }

        public override string ToString()
        {
            return "Minus(" + _lhs.ToString() + "," + _rhs.ToString() + ")";
        }

        public override bool Equals( object obj )
        {
            if ( obj == null || !(obj is MinusPredicate) )
                return false;

            MinusPredicate rhs = (MinusPredicate) obj;
            return rhs._lhs.Equals( _lhs ) && rhs._rhs.Equals( _rhs );
        }

        public override int GetHashCode()
        {
            return _lhs.GetHashCode() ^ _rhs.GetHashCode();
        }
	}
}
