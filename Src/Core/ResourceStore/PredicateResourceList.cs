// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Database;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;
using System.Text;

namespace JetBrains.Omea.ResourceStore
{
    internal enum PredicateMatch { Match, None, Add, Del };

    /**
     * The base interface for a resource list predicate.
     */

    public abstract class ResourceListPredicate
    {
        internal abstract IntArrayList GetMatchingResources( out bool sortedById );

        /// <summary>
        /// Returns a reference to the sorted list of the resource IDs matching the predicate.
        /// The object returned in syncObject must be locked while working with the list.
        /// </summary>
        /// <param name="syncObject">The object for synchronizing access to the list, or null
        /// if synchronization is not required.</param>
        /// <returns>The sorted list of resource IDs matching the predicate.</returns>
        internal virtual IntArrayList GetSortedMatchingResourcesRef( out object syncObject )
        {
            bool sortedById;
            IntArrayList result = GetMatchingResources( out sortedById );
            if ( !sortedById )
            {
                result.Sort();
            }
            syncObject = null;
            return result;
        }

        internal abstract PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs );
        internal abstract int GetSelectionCost();

        internal virtual ResourceListPredicate Optimize( bool isLive )
        {
            ResourceListPredicate predicate = MyPalStorage.Storage.GetCachedPredicate( this );
            if ( predicate != null )
            {
                return predicate;
            }
            return this;
        }

        /**
         * If all elements matching the predicate are known to be of the same type,
         * returns the ID of that type. If not, returns -1.
         */

        internal virtual int GetKnownType()
        {
            return -1;
        }

        /**
         * Checks if the predicate is (or contains in a union) the type predicate with the
         * specified type ID.
         */

        internal virtual bool HasTypePredicate( int typeId )
        {
            return false;
        }

        internal virtual bool HasAnyTypePredicate()
        {
            return false;
        }

        internal void GetMatchCounts( IResource res, IPropertyChangeSet cs, ref int oldMatch, ref int newMatch )
        {
            PredicateMatch match = MatchResource( res, cs );
            if ( match == PredicateMatch.Add || match == PredicateMatch.Match )
                newMatch++;

            if ( match == PredicateMatch.Del || match == PredicateMatch.Match )
                oldMatch++;
        }

        internal PredicateMatch MatchFromCounts( int oldMatch, int newMatch )
        {
            if ( newMatch > 0 && oldMatch > 0 )
                return PredicateMatch.Match;
            if ( newMatch > 0 )
                return PredicateMatch.Add;
            if ( oldMatch > 0 )
                return PredicateMatch.Del;
            return PredicateMatch.None;
        }

        protected IntArrayList GetResourcesFromResultSet( IResultSet rs, int column )
        {
            try
            {
                return DoGetResourcesFromResultSet( rs, column );
            }
            finally
            {
                rs.Dispose();
            }
        }

        protected IntArrayList DoGetResourcesFromResultSet( IResultSet rs, int column )
        {
            IntArrayList result = new IntArrayList();
            using( SafeRecordValueEnumerator enumerator = new SafeRecordValueEnumerator( rs, "PredicateResourceList.DoGetResourcesFromResultSet" ) )
            {
                while( enumerator.MoveNext() )
                {
                    result.Add( enumerator.GetCurrentIntValue( column ) );
                }
            }
            return result;
        }
    }

    /**
     * The predicate which is implemented as a plain list of resource IDs.
     */

    public class PlainListPredicate: ResourceListPredicate
    {
        private IntArrayList _resources;

        public PlainListPredicate( IntArrayList resources )
        {
            _resources = resources;
        }

        internal int Count
        {
            get { return _resources.Count; }
        }

        public void Add( int resourceId )
        {
            _resources.Add( resourceId );
        }

        public void AddRange( IntArrayList list )
        {
            _resources.AddRange( list );
        }

        internal override IntArrayList GetMatchingResources( out bool sortedById )
        {
            sortedById = false;
            return (IntArrayList) _resources.Clone();
        }

        internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
        {
            return ( _resources.IndexOf( res.Id ) >= 0 ) ? PredicateMatch.Match : PredicateMatch.None;
        }

        internal override int GetSelectionCost()
        {
            return 0;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder( "List(" );
            if ( _resources.Count > 0 )
            {
                builder.Append( _resources [0] );
                for( int i=1; i<_resources.Count; i++ )
                {
                    builder.Append( ',' );
                    builder.Append( _resources [i] );
                }
            }
            builder.Append( ")" );
            return builder.ToString();
        }

        public override bool Equals( object obj )
        {
            if ( Object.ReferenceEquals( this, obj ) )
                return true;

            PlainListPredicate rhs = obj as PlainListPredicate;
            if ( rhs == null )
                return false;

            if ( _resources.Count != rhs._resources.Count )
                return false;

            for( int i=0; i<_resources.Count; i++ )
            {
                if ( rhs._resources.IndexOf( _resources [i] ) < 0 )
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hc = 0;
            for( int i=0; i<_resources.Count; i++ )
            {
                hc ^= _resources [i] << (i % 32);
            }
            return hc;
        }
    }
}
