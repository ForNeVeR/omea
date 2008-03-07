/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.ResourceStore
{
	/**
     * The union of multiple predicates.
     */
    
    internal class UnionPredicate: ResourceListDerivedPredicate
    {
        internal UnionPredicate( params ResourceListPredicate[] predicates )
            : base( predicates )
        {
        }
        
        protected override string GetDerivationName()
        {
            return "Union";
        }

        internal override int GetSelectionCost()
        {
            int cost = 0;
            for( int i=0; i<_sourcePredicates.Length; i++ )
            {
                cost += _sourcePredicates [i].GetSelectionCost();
            }
            return cost;
        }

        internal void AddSource( ResourceListPredicate pred )
        {
            _sourcePredicates = AddPredicate( _sourcePredicates, pred );
        }

        internal override IntArrayList GetMatchingResources( out bool sortedById )
        {
            sortedById = true;
            if ( _sourcePredicates.Length == 0 )
            {
                return new IntArrayList();
            }

            bool srcSortedById;
            IntArrayList result = _sourcePredicates [0].GetMatchingResources( out srcSortedById );
            if ( !srcSortedById )
            {
                result.Sort();
            }
            
            for( int i=1; i<_sourcePredicates.Length; i++ )
            {
                IntArrayList list2 = _sourcePredicates [i].GetMatchingResources( out srcSortedById );
                if ( !srcSortedById )
                {
                    list2.Sort();
                }
                result = IntArrayList.MergeSorted( result, list2 );
            }

            return result;            
        }

        internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
        {
            int oldMatch = 0;
            int newMatch = 0;
            for( int i=0; i<_sourcePredicates.Length; i++ )
            {
                _sourcePredicates [i].GetMatchCounts( res, cs, ref oldMatch, ref newMatch );

                if ( newMatch > 0 && oldMatch > 0 )
                    break;
            }
            return MatchFromCounts( oldMatch, newMatch );
        }

        internal override bool HasTypePredicate( int typeId )
        {
            for( int i=0; i<_sourcePredicates.Length; i++ )
            {
                if ( _sourcePredicates [i].HasTypePredicate( typeId ) )
                {
                    return true;
                }
            }
            return false;
        }

        internal override bool HasAnyTypePredicate()
        {
            for( int i=0; i<_sourcePredicates.Length; i++ )
            {
                if ( _sourcePredicates [i].HasAnyTypePredicate() )
                {
                    return true;
                }
            }
            return false;
        }

        internal override ResourceListPredicate Optimize( bool isLive )
        {
            ResourceListPredicate result = this;
            if ( isLive )
            {
                int plainListCount = 0;
                for( int i=0; i<_sourcePredicates.Length; i++ )
                {
                    PlainListPredicate plainList = _sourcePredicates [i] as PlainListPredicate;
                    if ( plainList != null )
                    {       
                        plainListCount++;
                        if ( plainListCount > 1 || plainList.Count == 0 )
                        {
                            result = OptimizePlainLists();
                        }
                    }
                }
            }

            return OptimizeSourcePredicates( result, isLive );
        }

        private ResourceListPredicate OptimizePlainLists()
        {
            ArrayList predicates = ArrayListPool.Alloc();
            try 
            {
                PlainListPredicate plainList = new PlainListPredicate( new IntArrayList() );
                for( int i=0; i<_sourcePredicates.Length; i++ )
                {
                    PlainListPredicate sourcePlainList = _sourcePredicates [i] as PlainListPredicate;
                    if ( sourcePlainList != null )
                    {
                        if ( sourcePlainList.Count > 0 )
                        {
                            bool sortedById;
                            plainList.AddRange( sourcePlainList.GetMatchingResources( out sortedById ) );
                        }
                    }
                    else
                    {
                        predicates.Add( _sourcePredicates [i] );
                    }
                }

                if ( predicates.Count == 0 )
                {
                    return plainList;
                }

                if ( plainList.Count > 0 )
                {
                    predicates.Add( plainList );
                }

                if ( predicates.Count == 1 )
                {
                    return (ResourceListPredicate) predicates [0];
                }

                _sourcePredicates = (ResourceListPredicate[]) predicates.ToArray( typeof(ResourceListPredicate) );
            }
            finally
            {
                ArrayListPool.Dispose( predicates );
            }
            return this;
        }
    }
}
