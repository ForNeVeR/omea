// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceStore
{
    /// <summary>
    /// A predicate which matches a resource of any of the specified types.
    /// </summary>
    internal class MultiResourceTypePredicate: ResourceListPredicate
    {
        private int[] _resTypeIds;

        internal MultiResourceTypePredicate( string[] types )
        {
            _resTypeIds = new int [types.Length];
            for( int i=0; i<types.Length; i++ )
            {
                _resTypeIds [i] = MyPalStorage.Storage.ResourceTypes [types [i]].Id;
            }
        }

        internal override IntArrayList GetMatchingResources( out bool sortedById )
        {
            sortedById = false;

            IntArrayList result = null;
            for( int i=0; i<_resTypeIds.Length; i++ )
            {
                ResourceListPredicate basePredicate = new ResourceTypePredicate( _resTypeIds [i] );
                ResourceListPredicate cachedPredicate = MyPalStorage.Storage.GetCachedPredicate( basePredicate );

                bool tempSortedById = false;
                object syncObject = null;
                IntArrayList tempResult;
                if ( cachedPredicate != null )
                {
                    tempResult = cachedPredicate.GetSortedMatchingResourcesRef( out syncObject );
                }
                else
                {
                    tempResult = basePredicate.GetMatchingResources( out tempSortedById );
                    syncObject = null;
                }
                if( result == null )
                {
                    if( syncObject == null )
                    {
                        result = tempResult;
                    }
                    else
                    {
                        lock( syncObject )
                        {
                            result = (IntArrayList) tempResult.Clone();
                        }
                    }
                }
                else
                {
                    if( syncObject == null )
                    {
                        result.AddRange( tempResult );
                    }
                    else
                    {
                        lock( syncObject )
                        {
                            result.AddRange( tempResult );
                        }
                    }
                }
            }
            if( result == null )
            {
                result = new IntArrayList();
            }
            return result;
        }

        internal override bool HasTypePredicate( int typeId )
        {
            return Array.IndexOf( _resTypeIds, typeId ) >= 0;
        }

        internal override bool HasAnyTypePredicate()
        {
            return true;
        }

        internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
        {
            if ( Array.IndexOf( _resTypeIds, res.TypeId ) >= 0 )
            {
                return (cs != null && ( cs.IsNewResource || cs.IsPropertyChanged( ResourceProps.Type ) ) )
                    ? PredicateMatch.Add
                    : PredicateMatch.Match;
            }
            else if ( cs != null && cs.IsPropertyChanged( ResourceProps.Type ) &&
                Array.IndexOf( _resTypeIds, (int) cs.GetOldValue( ResourceProps.Type ) ) >= 0 )
            {
                return PredicateMatch.Del;
            }
            return PredicateMatch.None;
        }

        public override string ToString()
        {
            string[] typeNames = new string[ _resTypeIds.Length ];
            for( int i=0; i<_resTypeIds.Length; i++ )
            {
                typeNames [i] = MyPalStorage.Storage.ResourceTypes [_resTypeIds [i]].Name;
            }
            return "MultiType(" + String.Join( ",", typeNames ) + ")";
        }

        internal override int GetSelectionCost()
        {
            return 5;
        }

        public override bool Equals( object obj )
        {
            if ( Object.ReferenceEquals( this, obj) )
                return true;

            MultiResourceTypePredicate rhs = obj as MultiResourceTypePredicate;
            if ( rhs == null )
                return false;

            if ( rhs._resTypeIds.Length != _resTypeIds.Length )
                return false;

            for( int i=0; i<_resTypeIds.Length; i++ )
            {
                if ( Array.IndexOf( rhs._resTypeIds, _resTypeIds [i] ) < 0 )
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hc = 0;
            for( int i=0; i<_resTypeIds.Length; i++ )
            {
                hc = hc << 6 + _resTypeIds [i];
            }
            return hc;
        }
    }
}
