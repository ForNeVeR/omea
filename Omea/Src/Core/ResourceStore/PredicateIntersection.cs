// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;
using System.Diagnostics;

namespace JetBrains.Omea.ResourceStore
{
    /**
     * The intersection of multiple predicates.
     */

    internal class IntersectionPredicate: ResourceListDerivedPredicate
    {
        // For MatchResource(), all predicates are treated the same.
        // For GetMatchingResources(), we divide the predicates in two classes -
        // some (like PropValuePredicate) will return a small result set, others
        // (like TypeResourceListPredicate) - a large one. We call the first type
        // "selective", and the second type "filtering". Thus, we intersect
        // the result sets from all selective predicates with each other and then
        // filter them through the filtering ones.

        private ResourceListPredicate[] _sourcePredicatesFiltering;
        private ResourceListPredicate[] _sourcePredicatesSelective;
        private int _minSelectionCost = 0;
        private int _knownType = -1;

        internal static bool _traceIntersections = false;

        internal IntersectionPredicate( params ResourceListPredicate[] predicates )
            : base( predicates )
        {
            if ( predicates.Length < 2 )
            {
                throw new ArgumentOutOfRangeException( "At least 2 predicates required for intersection" );
            }
        }

        private void BuildSelectionPlan( ResourceListPredicate[] predicates )
        {
            _minSelectionCost = Int32.MaxValue;
            int maxSelectionCost = Int32.MinValue;
            int minSelectionCostCount = 0;
            int zeroCostCount = 0;
            for( int i=0; i<predicates.Length; i++ )
            {
                int cost = predicates [i].GetSelectionCost();

                if ( cost > 0 && cost < _minSelectionCost )
                {
                    _minSelectionCost = cost;
                    minSelectionCostCount = 1;
                }
                else if ( cost == _minSelectionCost )
                {
                    minSelectionCostCount++;
                }
                else if ( cost == 0 )
                {
                    zeroCostCount++;
                }

                maxSelectionCost = Math.Max( cost, maxSelectionCost );

                int knownType = predicates [i].GetKnownType();
                if ( knownType >= 0 )
                {
                    _knownType = knownType;
                }
            }

            if ( zeroCostCount == predicates.Length )
            {
                _minSelectionCost = 0;
            }

            if ( _minSelectionCost == maxSelectionCost && zeroCostCount == 0 )
            {
                // we need to have at least one selective predicate
                _sourcePredicatesSelective = new ResourceListPredicate [1];
                _sourcePredicatesSelective [0] = predicates [0];

                _sourcePredicatesFiltering = new ResourceListPredicate [predicates.Length-1];
                Array.Copy( predicates, 1, _sourcePredicatesFiltering, 0, predicates.Length-1 );
            }
            else
            {
                int selCount = minSelectionCostCount + zeroCostCount;
                int filtCount = predicates.Length - selCount;
                _sourcePredicatesSelective = new ResourceListPredicate [selCount];
                _sourcePredicatesFiltering = new ResourceListPredicate [filtCount];
                int selIndex = 0, filtIndex = 0;

                for( int i=0; i<predicates.Length; i++ )
                {
                    if ( predicates [i].GetSelectionCost() <= _minSelectionCost )
                    {
                        if ( selIndex >= _sourcePredicatesSelective.Length )
                        {
                            throw new Exception( "Index out of bounds: selCount=" + selCount +
                                ", filtCount=" + filtCount + ", selIndex=" + selIndex + ", minSelectionCost=" + _minSelectionCost );
                        }
                        _sourcePredicatesSelective [selIndex++] = predicates [i];
                    }
                    else
                    {
                        if ( filtIndex >= _sourcePredicatesFiltering.Length )
                        {
                            throw new Exception( "Index out of bounds: selCount=" + selCount +
                                ", filtCount=" + filtCount + ", filtIndex=" + filtIndex + ", minSelectionCost=" + _minSelectionCost );
                        }
                        _sourcePredicatesFiltering [filtIndex++] = predicates [i];
                    }
                }
            }
        }

        protected override string GetDerivationName()
        {
            return "Intersection";
        }

        internal override int GetKnownType()
        {
            return _knownType;
        }

        internal void AddSource( ResourceListPredicate pred )
        {
            lock ( this )
            {
                if ( _sourcePredicatesSelective != null )
                {
                    if ( pred.GetSelectionCost() <= _minSelectionCost )
                    {
                        _sourcePredicatesSelective = AddPredicate( _sourcePredicatesSelective, pred );
                    }
                    else
                    {
                        _sourcePredicatesFiltering = AddPredicate( _sourcePredicatesFiltering, pred );
                    }
                }
                _sourcePredicates = AddPredicate( _sourcePredicates, pred );
            }
        }

        internal override int GetSelectionCost()
        {
            return _minSelectionCost;
        }

        internal override IntArrayList GetMatchingResources( out bool sortedById )
        {
            lock ( this )
            {
                if ( _sourcePredicatesSelective == null )
                {
                    BuildSelectionPlan( _sourcePredicates );
                }

                sortedById = true;
                if ( _sourcePredicatesSelective.Length == 0 )
                {
                    return new IntArrayList();
                }

                if ( _traceIntersections )
                {
                    Debug.WriteLine( "Intersection source predicate selective: " + _sourcePredicatesSelective [0] );
                }

                int i = 1;
                object syncObject;
                IntArrayList result = _sourcePredicatesSelective [0].GetSortedMatchingResourcesRef( out syncObject );
                if( syncObject != null )
                {
                    if( _sourcePredicatesSelective.Length == 1 )
                    {
                        lock( syncObject )
                        {
                            result = (IntArrayList) result.Clone();
                        }
                    }
                    else
                    {
                        i = 2;
                        object syncObject2;
                        IntArrayList list2 = _sourcePredicatesSelective [1].GetSortedMatchingResourcesRef( out syncObject2 );
                        using( new MultiLock( syncObject, syncObject2 ) )
                        {
                            result = IntArrayList.IntersectSortedNew( result, list2 );
                        }
                    }
                }

                for( ; result.Count > 0 && i < _sourcePredicatesSelective.Length; i++ )
                {
                    if ( _traceIntersections )
                    {
                        Debug.WriteLine( "Intersection source predicate selective: " + _sourcePredicatesSelective [i] );
                    }
                    IntArrayList list2 = _sourcePredicatesSelective [i].GetSortedMatchingResourcesRef( out syncObject );
                    MTSafeInPlaceIntersectSorted( syncObject, result, list2 );
                }

                int skipIndex = -1;
                int filterCount = _sourcePredicatesFiltering.Length;
                if ( result.Count > 100 && filterCount > 0 )
                {
                    filterCount--;
                    // If the first intersection predicate returned too many results, run the
                    // first filtering predicate as selection and intersect, instead of loading
                    // all the resources and checking filters
                    skipIndex = FindCheapestFilteringPredicate();
                    if ( _traceIntersections )
                    {
                        Debug.WriteLine( "Intersection cheapest filtering source: " + _sourcePredicatesFiltering [skipIndex] );
                    }
                    IntArrayList list2 = _sourcePredicatesFiltering [skipIndex].GetSortedMatchingResourcesRef( out syncObject );
                    MTSafeInPlaceIntersectSorted( syncObject, result, list2 );
                }

                // filter the result by applying non-selective predicates
                if ( result.Count > 0 && filterCount > 0 )
                {
                    if ( _traceIntersections )
                    {
                        for( i=0; i<_sourcePredicatesFiltering.Length; i++ )
                        {
                            Debug.WriteLine( "Intersection source predicate filtering: " + _sourcePredicatesFiltering [i] );
                        }
                    }

                    int srcIndex = 0, destIndex = 0;
                    while( srcIndex < result.Count )
                    {
                        if ( MatchFiltering( result [srcIndex], skipIndex ) )
                        {
                            result [destIndex] = result [srcIndex];
                            destIndex++;
                        }
                        srcIndex++;
                    }
                    result.RemoveRange( destIndex, result.Count-destIndex );
                }
                return result;
            }
        }

        private static void MTSafeInPlaceIntersectSorted( object syncObject, IntArrayList result, IntArrayList list2 )
        {
            if( syncObject == null )
            {
                result.IntersectSorted( list2 );
            }
            else
            {
                lock( syncObject )
                {
                    result.IntersectSorted( list2 );
                }
            }
        }

        private int FindCheapestFilteringPredicate()
        {
            int minCost = Int32.MaxValue;
            int minIndex = 0;
            for( int i=0; i<_sourcePredicatesFiltering.Length; i++ )
            {
                int cost = _sourcePredicatesFiltering [i].GetSelectionCost();
                if ( cost < minCost )
                {
                    minIndex = i;
                    minCost = cost;
                }
            }
            return minIndex;
        }

        private bool MatchFiltering( int resID, int skipIndex )
        {
            IResource res;
            try
            {
                res = MyPalStorage.Storage.LoadResource( resID, true, -1 );
                if ( res.IsDeleted )
                    return false;
            }
            catch( InvalidResourceIdException )
            {
                return false;
            }

            for( int i=0; i<_sourcePredicatesFiltering.Length; i++ )
            {
                if ( i == skipIndex )
                    continue;

                if ( _sourcePredicatesFiltering [i].MatchResource( res, null ) == PredicateMatch.None )
                {
                    return false;
                }
            }
            return true;
        }

        internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
        {
            int oldMatch = 0;
            int newMatch = 0;
            int len = _sourcePredicates.Length;
            for( int i=0; i<len; i++ )
            {
                PredicateMatch match = _sourcePredicates [i].MatchResource( res, cs );
                if ( match == PredicateMatch.Add || match == PredicateMatch.Match )
                    newMatch++;

                if ( match == PredicateMatch.Del || match == PredicateMatch.Match )
                    oldMatch++;

                if ( newMatch == 0 && oldMatch == 0 )
                    break;
            }
            if ( newMatch == len && oldMatch == len )
                return PredicateMatch.Match;
            if ( newMatch == len )
                return PredicateMatch.Add;
            if ( oldMatch == len )
                return PredicateMatch.Del;
            return PredicateMatch.None;
        }

        /**
         * Optimizes the predicate to make its calculation more efficient.
         */

        internal override ResourceListPredicate Optimize( bool isLive )
        {
            bool optimized = false;
            ArrayList newPredicateList = ArrayListPool.Alloc();
            try
            {
                ExpandIntersections( newPredicateList, ref optimized );
                RemoveEqualSources( newPredicateList, ref optimized );
                RemoveIntersectingUnions( newPredicateList, ref optimized );
                RemoveRedundantTypeIntersections( newPredicateList, ref optimized );
                ResourceListPredicate result = this;
                if ( optimized )
                {
                    if ( newPredicateList.Count == 1 )
                    {
                        result = (ResourceListPredicate) newPredicateList [0];
                    }
                    else
                    {
                        _sourcePredicates = (ResourceListPredicate[]) newPredicateList.ToArray(
                            typeof(ResourceListPredicate) );
                    }
                }
                return OptimizeSourcePredicates( result, isLive );
            }
            finally
            {
                ArrayListPool.Dispose( newPredicateList );
            }
        }

        /**
         * Intersect(A,Intersect(B,C)) -> Intersect(A,B,C)
         * If any of the sources of the intersection are also intersections,
         * collapses their sources into the current intersection.
         */

        private void ExpandIntersections( ArrayList newPredicateList, ref bool optimized )
        {
            for( int i=0; i<_sourcePredicates.Length; i++ )
            {
                IntersectionPredicate sourceIntersection = _sourcePredicates [i] as IntersectionPredicate;
                if ( sourceIntersection != null )
                {
                    newPredicateList.AddRange( sourceIntersection._sourcePredicates );
                    optimized = true;
                }
                else
                {
                    newPredicateList.Add( _sourcePredicates [i] );
                }
            }
        }

        /**
         * Intersect(A,A,B) -> Intersect(A,B)
         * Removes equal predicates from the list of source predicates.
         */

        private void RemoveEqualSources( ArrayList newPredicateList, ref bool optimized )
        {
            for( int i=newPredicateList.Count-1; i >= 0; i-- )
            {
                for( int j=0; j<i; j++ )
                {
                    if ( newPredicateList [i].Equals( newPredicateList [j] ) )
                    {
                        newPredicateList.RemoveAt( i );
                        optimized = true;
                        break;
                    }
                }
            }
        }

        /**
         * Intersect(A, Union(A, B)) -> A
         * Removes from the specified array list union predicates which contain
         * members of the current intersection.
         */

        private void RemoveIntersectingUnions( ArrayList newPredicateList, ref bool optimized )
        {
            for( int i=newPredicateList.Count-1; i >= 0; i-- )
            {
                UnionPredicate sourceUnion = newPredicateList [i] as UnionPredicate;
                if ( sourceUnion != null )
                {
                    for( int j=0; j<newPredicateList.Count; j++ )
                    {
                        if ( i != j && sourceUnion.ContainsPredicate( (ResourceListPredicate) newPredicateList [j] ) )
                        {
                            newPredicateList.RemoveAt( i );
                            optimized = true;
                            break;
                        }
                    }
                }
            }
        }

        /**
         * An intersection of a type predicate and a predicate with a known type is
         * equivalent to the known type predicate.
         */

        private void RemoveRedundantTypeIntersections( ArrayList newPredicateList, ref bool optimized )
        {
            int knownTypeId = -1;
            for( int i=0; i<newPredicateList.Count; i++ )
            {
                ResourceListPredicate predicate = (ResourceListPredicate) newPredicateList [i];
                if ( !(predicate.HasAnyTypePredicate() ) )
                {
                    int predKnownTypeId = predicate.GetKnownType();
                    if ( knownTypeId == -1 )
                    {
                        knownTypeId = predKnownTypeId;
                    }
                    else if ( predKnownTypeId != knownTypeId )
                    {
                        return;
                    }
                }
            }

            if ( knownTypeId != -1 )
            {
                for( int i=newPredicateList.Count-1; i >= 0; i-- )
                {
                    if ( ((ResourceListPredicate) newPredicateList [i]).HasTypePredicate( knownTypeId ) )
                    {
                        newPredicateList.RemoveAt( i );
                        optimized = true;
                    }
                }
            }
        }
    }
}
